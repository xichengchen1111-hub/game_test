using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Native cast acceptance with isolated saves. Legal creator teleports stage
// close views; E rays, dialogue, animation and the selected door walk are real
// runtime handlers. This is neither physical input nor an unfamiliar playtest.
public partial class DemoApp {
    [Serializable] class PoliceCastActorResult {
        public string title,mesh,material,texture;
        public float height,groundGap,viewAngle;
        public Vector3 feet,viewpoint;
    }
    [Serializable] class PoliceCastReviewResult {
        public bool passed,muted,distinctFromClerk,environmentRemarks,environmentIdle,pauseFrozen,guardWalk,guardWalkPauseFrozen,noUnexpectedEvidence,cellWalk,backgroundRead;
        public int guardRetreatSamples;
        public List<PoliceCastActorResult> actors=new List<PoliceCastActorResult>();
        public string verification;
    }
    void PCastNeed(bool ok,string why) {
        if(!ok)throw new Exception("POLICE_CAST_REVIEW_FAIL: "+why);
        Debug.Log("POLICE_CAST_CHECK: "+why);
    }
    void PCastMute(){PCastNeedQuiet(AudioListener.volume==0,"native review must remain silent");}
    void PCastNeedQuiet(bool ok,string why){if(!ok)throw new Exception("POLICE_CAST_REVIEW_FAIL: "+why);}
    IEnumerator PCastWait(float seconds) {
        float until=Time.unscaledTime+seconds;
        while(Time.unscaledTime<until){PCastMute();yield return null;}
    }
    string[] PCastEvidence()=>state.entries.Select(e=>e.id).OrderBy(id=>id).ToArray();
    static Transform PCastBone(Interactable actor,string name)=>actor.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
    static SkinnedMeshRenderer PCastBody(Interactable actor)=>actor.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(r=>r.sharedMesh.vertexCount).First();
    static string PCastPath(Transform item){var path=item.name;while(item.parent){item=item.parent;path=item.name+"/"+path;}return path;}
    static Bounds PCastBounds(Interactable actor) {
        bool any=false;var bounds=new Bounds();
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            var baked=new Mesh();skin.BakeMesh(baked,true);
            foreach(var vertex in baked.vertices) {
                var p=skin.transform.TransformPoint(vertex);
                if(any)bounds.Encapsulate(p);else{bounds=new Bounds(p,Vector3.zero);any=true;}
            }
            Destroy(baked);
        }
        if(!any)throw new Exception("POLICE_CAST_REVIEW_FAIL: no skinned vertices on "+actor.title);
        return bounds;
    }
    bool PCastGround(Vector3 candidate,Transform actor,out Vector3 feet) {
        feet=candidate;
        foreach(var hit in Physics.RaycastAll(candidate+Vector3.up*.3f,Vector3.down,.9f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) {
            if(hit.transform.IsChildOf(player.transform)||actor&&hit.transform.IsChildOf(actor)||hit.normal.y<.7f)continue;
            feet.y=hit.point.y+.025f;return true;
        }
        return false;
    }
    bool PCastCanStand(Vector3 feet) {
        var cc=player.GetComponent<CharacterController>();float radius=cc.radius;
        var centre=feet+cc.center;
        foreach(var hit in Physics.OverlapCapsule(centre+Vector3.up*(cc.height*.5f-radius),centre-Vector3.up*(cc.height*.5f-radius),radius,~0,QueryTriggerInteraction.Ignore)) {
            if(hit.transform.IsChildOf(player.transform)||hit.bounds.max.y<=feet.y+.025f)continue;
            return false;
        }
        return true;
    }
    bool PCastFaceVisible(Vector3 feet,Interactable actor,Transform head) {
        var eye=feet+Vector3.up*1.66f;var delta=head.position-eye;
        foreach(var hit in Physics.RaycastAll(eye,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance)) {
            if(hit.transform.IsChildOf(player.transform))continue;
            return hit.transform.IsChildOf(actor.transform);
        }
        return true;
    }
    IEnumerator PCastFace(Interactable actor,PoliceCastActorResult result,int index) {
        var head=PCastBone(actor,"Head");var front=PCastBone(actor,"headfront");
        var forward=Vector3.ProjectOnPlane(front.position-head.position,Vector3.up).normalized;
        bool found=false;Vector3 chosen=Vector3.zero;float angle=0;
        foreach(float distance in new[]{1.25f,1.6f,1.05f,2f}) {
            foreach(float turn in new[]{0f,-20,20,-40,40,-60,60,-70,70,-80,80}) {
                var candidate=actor.transform.position+Quaternion.Euler(0,turn,0)*forward*distance;
                if(!PCastGround(candidate,null,out var feet)||!PCastCanStand(feet)||!PCastFaceVisible(feet,actor,head))continue;
                chosen=feet;angle=turn;found=true;break;
            }
            if(found)break;
        }
        PCastNeed(found,"unobstructed legal player-height view exists for "+actor.title);
        CreatorTeleport(chosen);player.Aim(head.position);yield return PCastWait(.3f);
        PCastNeed(!player.CreatorFlying&&player.GetComponent<CharacterController>().enabled&&PCastCanStand(player.transform.position),"grounded creator close view has clear player capsule: "+actor.title);
        PCastNeed(PCastFaceVisible(player.transform.position,actor,head),"face line of sight is clear: "+actor.title);
        result.viewpoint=player.transform.position;result.viewAngle=angle;
        yield return Capture("police-cast-"+index.ToString("00")+"-face");PCastMute();
    }
    IEnumerator PCastUse(Interactable actor) {
        player.Aim(PCastBone(actor,"Head").position);yield return null;Physics.SyncTransforms();
        PCastNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)&&hit.collider.GetComponentInParent<Interactable>()==actor,"normal first-hit E ray reaches "+actor.title);
        PCastNeed(TryInteract(),"normal E handler uses "+actor.title);yield return null;PCastMute();
    }
    IEnumerator PCastEnvironment(PoliceStaff actor,int index) {
        var evidence=PCastEvidence();var speech=actor.GetComponentInChildren<PeriodCharacterPerformance>();
        PCastNeed(speech&&speech.animator,"environment actor has its own animation: "+actor.title);
        yield return PCastUse(actor);yield return PCastWait(.4f);
        PCastNeed(!DialogueOpen&&!inspected&&!Blocked&&remark.StartsWith(actor.title+"：")&&Time.time<remarkUntil,"environment E displays a readable optional remark: "+actor.title);
        PCastNeed(speech.IsSpeaking&&speech.animator.GetCurrentAnimatorStateInfo(0).IsName("Talk"),"environment remark uses Talk: "+actor.title);
        double clock=ElapsedGameMinutes;yield return Capture("police-cast-"+index.ToString("00")+"-remark");
        PCastNeed(ElapsedGameMinutes>clock,"remark keeps shared time moving: "+actor.title);
        paused=true;CursorMode();yield return null;yield return new WaitForEndOfFrame();
        var animator=speech.animator;float pose=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;double pausedClock=ElapsedGameMinutes;
        var head=PCastBone(actor,"Head");var rotation=head.localRotation;
        yield return PCastWait(.35f);
        PCastNeed(animator.speed==0&&Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-pose)<.002f&&Quaternion.Angle(rotation,head.localRotation)<.01f,"explicit pause freezes environmental Talk pose: "+actor.title);
        PCastNeed(Math.Abs(ElapsedGameMinutes-pausedClock)<1e-9,"pause freezes shared clock: "+actor.title);
        paused=false;CursorMode();float until=Time.unscaledTime+13;
        while((speech.IsSpeaking||!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"))&&Time.unscaledTime<until){PCastMute();yield return null;}
        PCastNeed(!speech.IsSpeaking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"environmental Talk returns to Idle without further input: "+actor.title);
        PCastNeed(evidence.SequenceEqual(PCastEvidence()),"environment conversation grants no evidence: "+actor.title);
    }
    IEnumerator PCastGuardRetreat(OpeningActor guard,PoliceCastReviewResult result) {
        PCastNeed(state.openingStage==0,"cast review begins with the real guard notification available");
        CreatorTeleport(OpeningPoint(19.2f,20.6f),-90);player.Aim(PCastBone(guard,"Head").position);yield return null;
        PCastNeed(TryInteract()&&DialogueOpen&&dialogueNode.speaker=="狱警","cell interaction reaches the existing guard notification");
        AdvanceDialogue();AdvanceDialogue();PCastNeed(state.openingStage==1&&!DialogueOpen,"guard notification opens the real cell door");
        var box=guard.GetComponent<BoxCollider>();PCastNeed(box,"guard has an actual body collider");
        var performance=guard.GetComponentInChildren<PeriodCharacterPerformance>();PCastNeed(performance&&performance.animator,"guard has the new three-clip performance");
        var animator=performance.animator;float started=Time.unscaledTime;
        float until=Time.unscaledTime+4,next=0;
        while(Time.unscaledTime<until) {
            // The review advances both lines in one frame, so the scripted Talk restart (0.25 s)
            // is still blending when the guard steps off; sample once Talk->Walk has had time to finish.
            if(!result.guardWalk&&Time.unscaledTime-started>.7f) {
                PCastNeed(performance.IsWalking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Walk"),"guard retreat plays the authored Walk clip (walking="+performance.IsWalking+" speaking="+performance.IsSpeaking+" inTransition="+animator.IsInTransition(0)+" idle="+animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")+" talk="+animator.GetCurrentAnimatorStateInfo(0).IsName("Talk")+" nextWalk="+animator.GetNextAnimatorStateInfo(0).IsName("Walk")+" local="+guard.transform.localPosition+")");result.guardWalk=true;
                paused=true;CursorMode();yield return null;yield return new WaitForEndOfFrame();
                var position=guard.transform.position;var facing=guard.transform.rotation;float phase=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return PCastWait(.35f);
                PCastNeed(animator.speed==0&&guard.transform.position==position&&Quaternion.Angle(facing,guard.transform.rotation)<.001f&&Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-phase)<.002f,"pause freezes guard movement and Walk pose together");
                result.guardWalkPauseFrozen=true;paused=false;CursorMode();until+=.4f;
            }
            if(Time.unscaledTime>=next) {
                next=Time.unscaledTime+.08f;result.guardRetreatSamples++;Physics.SyncTransforms();
                var half=Vector3.Scale(box.size*.5f,box.transform.lossyScale);half.y-=.035f;
                foreach(var hit in Physics.OverlapBox(box.transform.TransformPoint(box.center),half,box.transform.rotation,~0,QueryTriggerInteraction.Ignore)) {
                    if(hit.transform.IsChildOf(guard.transform)||hit.transform.IsChildOf(player.transform)||hit.bounds.max.y<=guard.transform.position.y+.025f)continue;
                    PCastNeed(false,"retreating guard intersects "+PCastPath(hit.transform)+" at local="+guard.transform.localPosition+" bounds="+hit.bounds);
                }
                PCastNeedQuiet(Mathf.Abs(guard.transform.localPosition.x-16.95f)<.01f,"guard keeps the clear east-loggia x=16.95 line");
            }
            if(Mathf.Abs(guard.transform.localPosition.z-21.8f)<.01f)break;
            PCastMute();yield return null;
        }
        PCastNeed(Mathf.Abs(guard.transform.localPosition.z-21.8f)<.01f&&result.guardRetreatSamples>5,"guard visibly steps aside past the loggia pillar without intersection");
        yield return PCastWait(.4f);
        PCastNeed(result.guardWalk&&result.guardWalkPauseFrozen&&!performance.IsWalking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"guard completes retreat and returns to Idle");
        PCastNeed(PoliceStation.Current.Door("police-holding-door").IsOpen,"holding gate remains open after guard retreat");
        yield return WalkTo(OpeningPoint(18.25f,20.6f));yield return WalkTo(OpeningPoint(16.1f,20.6f));
        PCastNeed(!PoliceStation.Current.RoomAt(player.transform.position,out var room)||room.id!="holding","real player walks out of cell past new guard body");result.cellWalk=true;
    }
    IEnumerator ReviewPoliceCast() {
        yield return PCastWait(1);opening=false;paused=false;dayNightReviewFrozen=false;
        PCastNeed(CurrentScene=="Town"&&PoliceStation.Current,"cast is in the existing Town police building");
        PCastNeed(Review&&AudioListener.volume==0,"native cast run is muted before any interaction");
        var result=new PoliceCastReviewResult();var evidence=PCastEvidence();
        string[] titles={"狱警","律师","值班警员","巡警","接待女警"};float[] expected={1.83f,1.80f,1.65f,1.76f,1.60f};
        var items=PoliceStation.Current.GetComponentsInChildren<Interactable>().Where(i=>titles.Contains(i.title)).ToArray();
        PCastNeed(items.Length==5&&titles.All(t=>items.Count(i=>i.title==t)==1),"exactly four police staff and one lawyer are active in police station");
        var cast=titles.Select(t=>items.Single(i=>i.title==t)).ToArray();
        var clerk=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title=="售票员");
        var clerkMesh=PCastBody(clerk).sharedMesh;
        PCastNeed(cast.Select(a=>PCastBody(a).sharedMesh).Distinct().Count()==5&&cast.All(a=>PCastBody(a).sharedMesh!=clerkMesh),"five new cast meshes are distinct and none reuse station clerk mesh");
        PCastNeed(cast.All(a=>PCastBody(a).sharedMaterials.Length>0&&PCastBody(a).sharedMaterials[0]&&PCastBody(a).sharedMaterials[0].GetTexture("_BaseMap")),"every new body has a material and base-color texture");
        PCastNeed(cast.Select(a=>PCastBody(a).sharedMaterials[0]).Distinct().Count()==5,"five new cast members have distinct body materials");
        PCastNeed(cast.Select(a=>PCastBody(a).sharedMaterials[0].GetTexture("_BaseMap")).Distinct().Count()==5,"five independent base-color maps are bound");
        PCastNeed(cast.All(a=>a.GetComponentInChildren<Animator>()&&a.GetComponentInChildren<Animator>().runtimeAnimatorController),"all five actors have an assigned animation controller");
        PCastNeed(cast.Select(a=>a.GetComponentInChildren<Animator>().runtimeAnimatorController).Distinct().Count()==5,"five independent idle/walk/talk controllers are assigned");
        foreach(var actor in cast) {
            var performance=actor.GetComponentInChildren<PeriodCharacterPerformance>();
            PCastNeed(performance&&!actor.GetComponentInChildren<StationClerkPerformance>()&&!performance.animator.applyRootMotion,"new character has one performance owner and no animator root movement: "+actor.title);
            var clips=performance.animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
            PCastNeed(clips.Length==3&&new[]{"Idle","Walk","Talk"}.All(token=>clips.Count(c=>c.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0&&!c.empty&&c.length>0)==1),"three distinct non-empty source clips are assigned: "+actor.title);
        }
        for(int i=0;i<cast.Length;i++) {
            var actor=cast[i];var body=PCastBody(actor);var bounds=PCastBounds(actor);
            PCastNeed(PCastGround(actor.transform.position,actor.transform,out var ground),"floor exists beneath "+actor.title);
            float gap=bounds.min.y-(ground.y-.025f);
            PCastNeed(Mathf.Abs(bounds.size.y-expected[i])<.12f,"native rendered body retains authored height: "+actor.title+" = "+bounds.size.y.ToString("F3")+"m");
            PCastNeed(gap>=-.035f&&gap<=.06f,"feet meet real floor: "+actor.title+" gap="+gap.ToString("F3"));
            result.actors.Add(new PoliceCastActorResult{title=actor.title,mesh=body.sharedMesh.name,material=body.sharedMaterials[0].name,texture=body.sharedMaterials[0].GetTexture("_BaseMap").name,height=bounds.size.y,groundGap=gap,feet=actor.transform.position});
        }
        PCastNeed(result.actors[0].height-result.actors[4].height>.15f&&result.actors[1].height-result.actors[2].height>.08f,"cast stature differences remain visible at real scale");
        var evidenceItems=PoliceStation.Current.GetComponentsInChildren<Interactable>().Where(i=>i.evidenceId=="E01"||i.evidenceId=="E08").ToArray();
        PCastNeed(evidenceItems.Count(i=>i.evidenceId=="E01")==2&&evidenceItems.Count(i=>i.evidenceId=="E08")==1&&evidenceItems.All(i=>!string.IsNullOrEmpty(i.body)&&!string.IsNullOrEmpty(i.source)),"police E01 copies and E08 document keep their existing IDs and readable contents");
        yield return PCastGuardRetreat((OpeningActor)cast[0],result);
        for(int i=0;i<cast.Length;i++) {
            yield return PCastFace(cast[i],result.actors[i],i+1);
            if(cast[i] is PoliceStaff environment)yield return PCastEnvironment(environment,i+1);
            else {yield return PCastUse(cast[i]);PCastNeed(DialogueOpen,"principal character keeps shared dialogue: "+cast[i].title);CloseDialogue();}
        }
        // Read a background object through the regular ray; no invented card.
        CreatorTeleport(OpeningPoint(16,4.4f),180);var pad=PoliceStation.Current.GetComponentsInChildren<PoliceDocumentItem>().Single(i=>i.documentId=="interview-pad");
        player.Aim(pad.transform.position);yield return null;
        PCastNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var paperHit,3f)&&paperHit.collider.GetComponentInParent<Interactable>()==pad&&TryInteract()&&inspected==pad,"existing meeting-room pad remains inspectable");
        double readingClock=ElapsedGameMinutes;yield return PCastWait(.4f);PCastNeed(ElapsedGameMinutes>readingClock&&MovementLocked,"background reading retains shared clock and inspection controls");CloseInspect();result.backgroundRead=true;
        PCastNeed(evidence.SequenceEqual(PCastEvidence()),"cast interactions and background reading do not add or remove investigation evidence");
        // The principal corridor remains traversable with the new bodies in place.
        yield return WalkTo(OpeningPoint(17.5f,4.6f));yield return WalkTo(OpeningPoint(14,4.6f));yield return WalkTo(OpeningPoint(11.5f,4.6f));
        yield return WalkTo(OpeningPoint(8.6f,4));yield return WalkTo(OpeningPoint(2.6f,3.95f));
        player.Aim(evidenceItems.First(i=>i.evidenceId=="E01").transform.position);yield return Capture("police-cast-06-reading-room-path");
        PCastNeed(evidenceItems.All(i=>i&&i.gameObject.activeInHierarchy),"police evidence objects remain available after cast review");
        result.passed=result.muted=result.distinctFromClerk=result.environmentRemarks=result.environmentIdle=result.pauseFrozen=result.noUnexpectedEvidence=true;
        result.verification=Application.platform+" "+SystemInfo.graphicsDeviceType+" "+Screen.width+"x"+Screen.height+"; muted native render, grounded creator viewpoint staging, real controller/E rays and animation. No physical-input, blind, audio-listening or complete police quest claim.";
        Save();PCastMute();File.WriteAllText(Path.Combine(captures,"police-cast-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("POLICE_CAST_REVIEW_PASS");Application.Quit();
    }
}
