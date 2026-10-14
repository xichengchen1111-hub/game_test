using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The first conversation with the child is played as a cinematic: black bars, authored shots
// instead of the first-person view, centred subtitles. It is the same dialogue (same nodes,
// choices, progress and save) drawn differently; every shot keeps the camera on the father's
// side of the bed, so the player's invisible body is never seen. It ends on the player's own
// resolve, a black card, and the bedside, where the everyday talk continues as usual.
// docs/research/mystery-evidence-design.md (section four).
public partial class DemoApp {
    static readonly string[] ChildCinematicNodes={"greeting","waited","late","look","mother","mother-apples","mother-ask","who","heard","police","police-fear",
        "stay-promise","stay-honest","ask","ask-find","promise","promise-truth","promise-mother","promise-end","resolve"};
    public bool ChildCinematicActive{get;private set;}
    public string ChildCinematicShot{get;private set;}
    float cinematicBars,cinematicShotTime;string cinematicNode;
    bool cinematicEnding;string cinematicCard;float cinematicCardAlpha;
    Vector3 cinematicEyePos,cinematicStand;Quaternion cinematicEyeRot;float cinematicFov;
    Transform cinematicFather;HospitalBedPose cinematicPose;Light cinematicKey;HospitalActor cinematicChild;

    void BeginChildCinematic(HospitalActor child) {
        ChildCinematicActive=true;cinematicChild=child;cinematicEnding=false;cinematicCard=null;cinematicNode=null;cinematicBars=0;
        var eye=player.eye.transform;cinematicEyePos=eye.localPosition;cinematicEyeRot=eye.localRotation;cinematicFov=player.eye.fieldOfView;cinematicStand=player.transform.position;
        player.enabled=false;
        // Where the father sits: the bedside chair, at a seated eye height. The child looks there, not at the camera.
        var chair=HospitalBuilding.Current.Layout.furniture.First(f=>f.id=="cw-chair");float floor=HospitalBuilding.Current.Layout.levels.ground;
        cinematicFather=new GameObject("Cinematic father's head").transform;cinematicFather.position=HospitalLocal(chair.x,floor+1.15f,chair.z);
        cinematicPose=child.GetComponent<HospitalBedPose>();if(cinematicPose)cinematicPose.lookTarget=cinematicFather;
        // A soft warm light on her face for the length of the scene, from above the chair side like the
        // ward's night lamps (interior layer, no shadows; user, 2026-10-14: warmer and softer).
        cinematicKey=new GameObject("Cinematic lamp light").AddComponent<Light>();cinematicKey.type=LightType.Point;cinematicKey.shadows=LightShadows.None;
        cinematicKey.color=new Color(1f,.79f,.56f);cinematicKey.range=3.4f;cinematicKey.intensity=0;
        cinematicKey.transform.position=ChildFrame(.75f,.75f,1.15f);cinematicKey.renderingLayerMask=(int)HospitalInteriorLayer;cinematicKey.GetUniversalAdditionalLightData().renderingLayers=HospitalInteriorLayer;
        StartCoroutine(ChildCinematicLoop());
        Debug.Log("HOSPITAL_CINEMATIC_BEGIN");
    }
    // Interior light layer shared with the hospital lamps (BuildSlice.HospitalInteriorLayer).
    const uint HospitalInteriorLayer=1u<<4;
    // A point in the child's frame: forward toward the foot of the bed, left toward the chair side, up.
    Vector3 ChildFrame(float forward,float left,float up) {
        var t=cinematicChild.transform;var f=-t.forward;f.y=0;f.Normalize();var right=Vector3.Cross(Vector3.up,f);
        return t.position+f*forward-right*left+Vector3.up*up;
    }
    Vector3 ChildFace() {
        var head=cinematicChild.GetComponentsInChildren<Transform>().FirstOrDefault(b=>b.name=="Head");
        return head?head.position+Vector3.up*.05f:ChildFrame(0,0,.62f);
    }
    Vector3 HospitalLocal(float x,float y,float z)=>HospitalBuilding.Current.transform.TransformPoint(new Vector3(x,y,z));

    // The shot for a line: from/to camera positions and targets, interpolated over the line.
    (Vector3 from,Vector3 to,Vector3 lookFrom,Vector3 lookTo,float fov,string name) ChildShot(string node) {
        var face=ChildFace();var father=cinematicFather.position;var toFather=(father-face).normalized;
        float g=HospitalBuilding.Current.Layout.levels.ground;var bed=HospitalBuilding.Current.Layout.furniture.First(f=>f.kind=="wardbed"&&f.extra=="child");
        var apples=HospitalLocal(7.38f,g+.83f,bed.z-.68f);
        (Vector3,Vector3,Vector3,Vector3,float,string) Close(float from,float to,float fov,string name){var side=Vector3.Cross(Vector3.up,toFather).normalized*.06f;return(face+toFather*from+side+Vector3.down*.03f,face+toFather*to+side+Vector3.down*.03f,face,face,fov,name);}
        switch(node) {
            case "greeting": {var a=ChildFrame(2.7f,1.25f,1.0f);var look=ChildFrame(0,0,.38f);return(a,Vector3.Lerp(a,look,.14f),look,look,46,"wide");}
            case "waited": case "who": case "heard": case "stay-promise": case "stay-honest": case "promise": case "promise-truth": {
                var back=father+(father-face).normalized*.18f+Vector3.up*.03f;return(back,back+(face-back).normalized*.06f,face+Vector3.down*.12f,face,42,"father");}
            // The basket on the cabinet, then a slow tilt that brings her back into the frame.
            case "mother-apples": case "promise-mother": {var from=apples+(father-apples).normalized*.9f+Vector3.up*.34f;return(from,Vector3.Lerp(from,apples,.1f),apples,Vector3.Lerp(apples,face,.32f),38,"apples");}
            case "mother-ask": case "police-fear": return Close(.82f,.7f,30,"close");
            // Her request: a slow push from head and shoulders to her face.
            case "ask-find": return Close(1.05f,.74f,30,"push");
            case "resolve": {
                // From the father's chair the view turns from her to the window and the town beyond.
                var window=HospitalLocal(.42f,g+2.05f,bed.z+.3f);return(father,father,face,window,44,"window");}
            default: return Close(.9f,.78f,32,"close");
        }
    }
    // Starts on the next frame: the dialogue opens right after the cinematic begins.
    // Her face through the scene (user, 2026-10-14): glad to see her father, a small smile; her eyes fill
    // when her mother comes up; a tear runs at her request; a smile through the tears at the promise.
    static readonly System.Collections.Generic.Dictionary<string,(float smile,float tears)> ChildMoods=new System.Collections.Generic.Dictionary<string,(float,float)> {
        {"greeting",(.8f,.05f)},{"waited",(.65f,.15f)},{"late",(.6f,.15f)},{"look",(.6f,.15f)},
        {"mother",(.1f,.45f)},{"mother-apples",(.15f,.55f)},{"mother-ask",(0,.6f)},{"who",(0,.6f)},{"heard",(0,.6f)},{"police",(0,.65f)},{"police-fear",(0,.7f)},
        {"stay-promise",(.3f,.7f)},{"stay-honest",(.25f,.7f)},{"ask",(0,.75f)},{"ask-find",(0,.95f)},
        {"promise",(.4f,.95f)},{"promise-truth",(.35f,.95f)},{"promise-mother",(.2f,.95f)},{"promise-end",(0,.95f)},{"resolve",(0,.9f)},
    };
    void ChildMood(float smile,float tears){var expression=cinematicChild?cinematicChild.GetComponentInChildren<PeriodCharacterExpression>():null;if(expression)expression.SetMood(smile,tears);}
    IEnumerator ChildCinematicLoop(){yield return null;while(ChildCinematicActive){UpdateChildCinematic();yield return null;}}
    void UpdateChildCinematic() {
        if(!ChildCinematicActive)return;
        if(!DialogueOpen&&!cinematicEnding){EndChildCinematic(true);return;}
        float dt=Time.unscaledDeltaTime;
        cinematicBars=Mathf.MoveTowards(cinematicBars,cinematicEnding?0:1,dt*1.6f);
        if(cinematicKey)cinematicKey.intensity=Mathf.MoveTowards(cinematicKey.intensity,cinematicEnding?0:.65f,dt*.6f);
        if(DialogueOpen&&dialogueNode.id!=cinematicNode) {
            cinematicNode=dialogueNode.id;cinematicShotTime=0;
            if(ChildMoods.TryGetValue(cinematicNode,out var mood))ChildMood(mood.smile,mood.tears);
            if(!ChildCinematicNodes.Contains(cinematicNode)&&!cinematicEnding){StartCoroutine(ChildCinematicCard());return;}
        }
        if(cinematicEnding||cinematicNode==null)return;
        if(!paused)cinematicShotTime+=dt;
        var shot=ChildShot(cinematicNode);ChildCinematicShot=shot.name;
        float k=Mathf.SmoothStep(0,1,Mathf.Clamp01(cinematicShotTime/(cinematicNode=="resolve"?4.5f:7f)));
        var eye=player.eye.transform;eye.position=Vector3.Lerp(shot.from,shot.to,k);
        eye.rotation=Quaternion.LookRotation(Vector3.Lerp(shot.lookFrom,shot.lookTo,cinematicNode=="resolve"?Mathf.SmoothStep(0,1,Mathf.Clamp01((cinematicShotTime-1.2f)/2.6f)):k)-eye.position);
        player.eye.fieldOfView=shot.fov;
    }
    // After the last line: black, the card, then back to the bedside where the talk continues.
    IEnumerator ChildCinematicCard() {
        cinematicEnding=true;var keep=dialogueNode;dialogueNode=null;
        yield return FadeTo(1,.7f);
        cinematicCard="你答应了她。";
        for(float t=0;t<1;t+=Time.unscaledDeltaTime/.8f){cinematicCardAlpha=t;yield return null;}
        cinematicCardAlpha=1;yield return new WaitForSecondsRealtime(Review?1.2f:2.2f);
        for(float t=1;t>0;t-=Time.unscaledDeltaTime/.8f){cinematicCardAlpha=t;yield return null;}
        cinematicCard=null;cinematicCardAlpha=0;
        RestoreChildCinematicCamera();dialogueNode=keep;
        yield return FadeTo(0,.8f);
        ChildCinematicActive=false;Debug.Log("HOSPITAL_CINEMATIC_END");
    }
    void EndChildCinematic(bool immediate) {
        RestoreChildCinematicCamera();ChildCinematicActive=false;cinematicEnding=false;cinematicCard=null;
        if(immediate)fade=0;
        Debug.Log("HOSPITAL_CINEMATIC_END: closed early");
    }
    void RestoreChildCinematicCamera() {
        var eye=player.eye.transform;eye.localPosition=cinematicEyePos;eye.localRotation=cinematicEyeRot;player.eye.fieldOfView=cinematicFov;
        // Back where the player stood when the talk began, looking at her.
        if(cinematicChild){player.enabled=true;player.Place(cinematicStand,0);player.Aim(ChildFace());}
        else player.enabled=true;
        if(cinematicPose)cinematicPose.lookTarget=null;
        // Back at the bedside her eyes are still wet and the smile is small.
        ChildMood(.3f,.4f);
        if(cinematicFather)Destroy(cinematicFather.gameObject);if(cinematicKey)Destroy(cinematicKey.gameObject);
    }
    // Drawn after the fade overlay, so it shows on black.
    void DrawChildCinematicCard() {
        if(!ChildCinematicActive||cinematicCard==null)return;
        var card=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontSize=34};GUI.color=new Color(1,.96f,.88f,cinematicCardAlpha);
        Label(new Rect(0,H/2-40,W,80),cinematicCard,card);GUI.color=Color.white;
    }
    // Letterboxed like a film: the subtitle and, when she waits for an answer, the choices sit in
    // the bottom bar, never over the picture. The bottom bar deepens while a choice is open.
    // Same controls as every other conversation: number keys or a click on a choice, Space /
    // Enter / E or a click in the lower-right quarter to go on, H for the log, Esc to leave.
    // The choice bar opens on its own (even while the black bars are still easing in), and the
    // choices fade in with it, so they are never drawn over the picture.
    float CinematicBottomBar=>Mathf.Max(H*.12f*cinematicBars,H*.22f*cinematicChoiceBar);
    float cinematicChoiceBar;
    public bool ChildCinematicChoicesShown=>cinematicChoiceBar>=1;
    public Rect ChildCinematicChoiceRect(int i,int count) {
        float margin=DialogueMargin,gap=28,width=Mathf.Min(520,(W-2*margin-(count-1)*gap)/count);
        float left=(W-(count*width+(count-1)*gap))/2;
        return new Rect(left+i*(width+gap),H-92,width,42);
    }
    public float ChildCinematicPictureBottom=>H-CinematicBottomBar;
    void DrawChildCinematic() {
        DialogueStyles();GUIUtility.keyboardControl=0;
        bool choosing=DialogueOpen&&!cinematicEnding&&dialogueChoices.Count>0;
        if(Event.current.type==EventType.Repaint)cinematicChoiceBar=Mathf.MoveTowards(cinematicChoiceBar,choosing?1:0,Time.unscaledDeltaTime*4f);
        float top=H*.12f*cinematicBars,bottom=CinematicBottomBar;
        Box(new Rect(0,0,W,top),Color.black);Box(new Rect(0,H-bottom,W,bottom),Color.black);
        if(!DialogueOpen||cinematicEnding)return;
        if(dialogueHistoryOpen){DrawDialogueHistory(DialogueMargin);return;}
        float alpha=cinematicBars;GUI.color=new Color(1,1,1,alpha);
        bool inner=dialogueNode.speaker=="你";
        float y=H-bottom+12;
        if(!inner)DialogueLabel(new Rect(0,y,W,24),dialogueNode.speaker,new GUIStyle(dialogueName){alignment=TextAnchor.UpperCenter});
        var line=new GUIStyle(dialogueSpeech){alignment=TextAnchor.UpperCenter,wordWrap=true,fontStyle=inner?FontStyle.Italic:FontStyle.Normal};
        DialogueLabel(new Rect(W/2-620,y+(inner?8:26),1240,66),inner?"（"+dialogueNode.text.Trim('（','）')+"）":dialogueNode.text,line);
        if(choosing) {
            // Render against a snapshot: a click changes the active node immediately.
            var choices=dialogueChoices;var option=new GUIStyle(dialogueOption){alignment=TextAnchor.MiddleCenter};
            GUI.color=new Color(1,1,1,cinematicChoiceBar);
            for(int i=0;i<choices.Count;i++) {
                var r=ChildCinematicChoiceRect(i,choices.Count);
                Box(r,r.Contains(Event.current.mousePosition)?new Color(.55f,.43f,.27f,.32f):new Color(1,1,1,.05f));
                Box(new Rect(r.x,r.yMax-1,r.width,1),new Color(.86f,.7f,.44f,.55f));
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)){SelectDialogue(i);break;}
                DialogueLabel(new Rect(r.x+12,r.y+(r.height-25)/2,26,25),(i+1).ToString(),dialogueName);
                DialogueLabel(new Rect(r.x+34,r.y,r.width-46,r.height),choices[i].text,option);
            }
        } else {
            GUI.color=new Color(1,1,1,.7f*alpha);DialogueArrow(new Rect(W-DialogueMargin-38,H-56,38,32));
            if(Event.current.type==EventType.MouseUp&&Event.current.button==0&&AdvanceDialogueAt(Event.current.mousePosition))Event.current.Use();
        }
        GUI.color=Color.white;
    }
}
