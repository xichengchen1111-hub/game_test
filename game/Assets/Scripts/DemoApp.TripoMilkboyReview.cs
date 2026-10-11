using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class TripoMilkboyResult {
        public bool passed,visibleOnEntry,idleLoops,inPlace,dialogueRay,playerLineIdle,closeRestoresIdle,humanoid,mouthMoves;
        public Vector3 localPosition;public float jawMaxDegrees,idleLoopsObserved,idleGazeMaxDegrees,talkGazeDegrees;public string verification;
    }
    IEnumerator ReviewTripoMilkboy() {
        yield return new WaitForSeconds(.8f);
        var boy=GameObject.Find("Milkboy");var actor=boy.GetComponentInChildren<MilkboyPerformance>();var animator=actor.animator;
        var marker=milkShopRoot.GetComponent<MilkShopPlacement>();var result=new TripoMilkboyResult();
        result.localPosition=milkShopRoot.InverseTransformPoint(boy.transform.position);
        RequireDialogue(Vector3.Distance(result.localPosition,new Vector3(.15f,.18f,.15f))<.01f,"Child must stand in front of counter");
        RequireDialogue(animator.avatar&&animator.avatar.isHuman&&animator.avatar.isValid&&!animator.applyRootMotion,"Child requires valid in-place Humanoid");result.humanoid=true;
        player.Place(marker.outside,0);
        player.Aim(marker.inside+milkShopRoot.TransformDirection(Vector3.forward)*3+Vector3.up*1.25f);
        var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,"01-entry-idle-dialogue"));
        yield return new WaitForSeconds(1.2f);yield return WalkTo(marker.inside);
        var view=player.eye.WorldToViewportPoint(actor.head.position);
        Debug.Log("TRIPO_ENTRY_VIEW: viewport="+view+" head="+actor.head.position+" eye="+player.eye.transform.position+" renderer="+boy.GetComponentInChildren<SkinnedMeshRenderer>().bounds);
        yield return Capture("entry-visible");
        RequireDialogue(view.z>0&&view.x>.06f&&view.x<.94f&&view.y>.06f&&view.y<.94f,"Child must be visible immediately on entry without aiming at him");
        var headRay=actor.head.position-player.eye.transform.position;
        RequireDialogue(Physics.Raycast(player.eye.transform.position,headRay.normalized,out var hit,headRay.magnitude+.2f)&&hit.collider.GetComponentInParent<Interactable>()?.evidenceId=="E04","Entry view to child must be unobstructed");
        result.visibleOnEntry=true;yield return Capture("entry-visible");
        var front=boy.transform.position-boy.transform.forward*1.20f;front.y=marker.inside.y;yield return WalkTo(front);
        player.Aim(actor.head.position);yield return new WaitForSeconds(.8f);yield return Capture("idle-player-view");
        var origin=boy.transform.position;float start=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        RequireDialogue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Default state must be grip idle");
        float idleUntil=Time.time+6;
        while(Time.time<idleUntil) {
            yield return new WaitForEndOfFrame();
            result.idleGazeMaxDegrees=Mathf.Max(result.idleGazeMaxDegrees,Vector3.Angle(actor.headFront.position-actor.head.position,player.eye.transform.position-actor.head.position));
        }
        RequireDialogue(result.idleGazeMaxDegrees<12,"Idle must keep head raised toward player");
        result.idleLoopsObserved=animator.GetCurrentAnimatorStateInfo(0).normalizedTime-start;
        RequireDialogue(result.idleLoopsObserved>1&&animator.GetCurrentAnimatorStateInfo(0).loop&&Vector3.Distance(origin,boy.transform.position)<.001f,"Idle must loop without moving actor root");result.idleLoops=result.inPlace=true;
        RequireDialogue(TryInteract()&&DialogueOpen,"Ordinary E interaction ray must open child dialogue");result.dialogueRay=true;
        yield return new WaitForSeconds(.5f);RequireDialogue(animator.GetCurrentAnimatorStateInfo(0).IsName("Talk")&&actor.IsSpeaking,"NPC line must use Talk");yield return Capture("talk-greeting");
        result.talkGazeDegrees=Vector3.Angle(actor.headFront.position-actor.head.position,player.eye.transform.position-actor.head.position);
        RequireDialogue(result.talkGazeDegrees<12,"Talking must keep head raised toward player");
        yield return new WaitForSeconds(3.0f);AdvanceDialogue();yield return new WaitForSeconds(1.7f);
        SelectDialogue(0);yield return new WaitForSeconds(2.5f);AdvanceDialogue();
        RequireDialogue(dialogueNode.speaker=="你"&&!actor.IsSpeaking,"Player line must stop child speech");yield return new WaitForSeconds(.4f);
        RequireDialogue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"Player question must restore grip idle");result.playerLineIdle=true;yield return Capture("player-question-idle");
        AdvanceDialogue();yield return new WaitForSeconds(1.2f);yield return Capture("talk-reply");
        CloseDialogue();yield return new WaitForSeconds(.4f);RequireDialogue(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")&&!Blocked,"Closing conversation must restore idle and movement");result.closeRestoresIdle=true;
        yield return WalkTo(front+boy.transform.right*.6f);yield return new WaitForSeconds(.6f);recorder.End();
        // Detail inspection uses the same live actor and scene lighting, with a controlled camera.
        player.Place(front,0);player.Aim(actor.head.position);RequireDialogue(TryInteract()&&DialogueOpen,"Detail inspection opens through real dialogue ray");
        player.enabled=false;reviewPortrait=true;var eye=player.eye;float fov=eye.fieldOfView;
        var face=-boy.transform.forward;eye.transform.position=origin+face*1.25f+boy.transform.right*.55f+Vector3.up*1.14f;
        eye.transform.LookAt(origin+Vector3.up*.97f);eye.fieldOfView=34;
        recorder.Begin(Path.Combine(captures,"02-hands-mouth-detail"));
        var jaw=animator.GetBoneTransform(HumanBodyBones.Jaw);RequireDialogue(jaw,"Jaw must be mapped");var restRotation=jaw.localRotation;
        float until=Time.time+5.4f,detailAt=Time.time+1.2f;bool mouthCaptured=false;
        while(Time.time<until) {
            result.jawMaxDegrees=Mathf.Max(result.jawMaxDegrees,Quaternion.Angle(restRotation,jaw.localRotation));
            if(!mouthCaptured&&Time.time>=detailAt){RequireDialogue(actor.IsSpeaking,"Mouth detail must show active speech");yield return Capture("mouth-detail");mouthCaptured=true;}
            yield return null;
        }
        RequireDialogue(result.jawMaxDegrees>1,"Actual Unity Talk must move mouth");result.mouthMoves=true;
        CloseDialogue();yield return new WaitForSeconds(3);yield return Capture("hands-detail");recorder.End();
        eye.transform.position=origin+face*.8f+boy.transform.right*1.0f+Vector3.up*.99f;eye.transform.LookAt(origin+Vector3.up*.81f);eye.fieldOfView=40;
        yield return Capture("three-quarter-contact");
        eye.transform.position=origin+boy.transform.right*1.5f+Vector3.up*.90f;eye.transform.LookAt(origin+Vector3.up*.79f);yield return Capture("side-contact");
        eye.transform.position=origin+face*1.4f+Vector3.up*1.14f;eye.transform.LookAt(origin+Vector3.up*.97f);eye.fieldOfView=34;
        foreach(var phase in new[]{"clear-noon","rain-noon","night"}) {
            DayReviewTime(phase=="night"?0:720);DayNightCycle.Instance.SetReviewWeather(phase=="rain-noon"?TownWeather.Kind.Rain:TownWeather.Kind.Clear);
            yield return new WaitForSeconds(.8f);yield return Capture("face-light-"+phase);
        }
        eye.fieldOfView=fov;reviewPortrait=false;player.enabled=true;player.Place(front,0);player.Aim(actor.head.position);
        result.passed=true;result.verification=Application.platform+" native rendered player; real controller entry and interaction ray. Conversation uses shared handlers; detail views use controlled camera. No phoneme/audio lip sync, physical-input or blind-playtest claim.";
        File.WriteAllText(Path.Combine(captures,"milkboy-tripo-results.json"),JsonUtility.ToJson(result,true));Debug.Log("MILKBOY_TRIPO_REVIEW_PASS");Application.Quit();
    }
}
