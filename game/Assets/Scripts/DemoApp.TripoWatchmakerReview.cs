using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class TripoWatchmakerResult {
        public bool passed,dialogueRay,threeActions,noAdjacentRepeat,rootStationary,chairSupported,pauseFreezes,readingKeepsMotion;
        public string[] sequence;public float maxRootTravel,maxHipTravel,headDownMinimum,seatHeight,maxToolWatchDistance;public int completedActions,samples;
        public string verification;
    }
    IEnumerator ReviewTripoWatchmaker() {
        yield return new WaitForSeconds(.8f);
        var layout=seamlessShop.layout;Vector3 W(float x,float y,float z)=>layout.ToTown(new Vector3(x,y,z));
        var owner=GameObject.Find("Clockmaker");var perf=owner.GetComponentInChildren<WatchmakerPerformance>();
        NeedShop(perf&&perf.Ready&&!perf.animator.applyRootMotion,"Tripo repair performance missing");
        var animator=perf.animator;var bones=owner.GetComponentsInChildren<Transform>();
        var hips=bones.Single(t=>t.name=="Hips");var head=bones.Single(t=>t.name=="Head");var front=bones.Single(t=>t.name=="headfront");
        var watchContact=bones.Single(t=>t.name=="RepairWatchContact");var toolTip=bones.Single(t=>t.name=="RepairToolTip");
        var seat=bones.Single(t=>t.name=="ChairSeat").GetComponent<Renderer>();
        var result=new TripoWatchmakerResult{headDownMinimum=1,seatHeight=seat.bounds.max.y-owner.transform.position.y};
        NeedShop(Mathf.Abs(result.seatHeight-.4f)<.01f,"Chair must retain .4m seat");result.chairSupported=true;
        player.Place(W(0,.1f,-5.5f),0);var door=Mechanism("shop-front-door");if(!door.IsOpen){door.Use();yield return new WaitForSeconds(1);}
        player.Aim(W(2,1.15f,2.7f));yield return WalkTo(W(0,0,-2.15f));yield return WalkTo(W(0,0,-1));yield return WalkTo(W(.65f,0,.8f));
        player.Aim(head.position);yield return Capture("player-approach");
        NeedShop(TryInteract()&&DialogueOpen,"Watchmaker dialogue must remain reachable");result.dialogueRay=true;
        int completed=perf.CompletedActions;float time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSeconds(1.5f);yield return Capture("original-dialogue");
        NeedShop(perf.CompletedActions>completed||Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-time)>.05f,"Dialogue must keep repair animation running");result.readingKeepsMotion=true;CloseDialogue();
        player.enabled=false;reviewPortrait=true;var eye=player.eye;float fov=eye.fieldOfView;var origin=owner.transform.position;
        Vector3 P(float x,float y,float z)=>owner.transform.TransformPoint(new Vector3(x,y,z));
        void Frame(Vector3 from,Vector3 to,float field){eye.transform.position=from;eye.transform.LookAt(to);eye.fieldOfView=field;}
        Frame(P(-1.15f,1.22f,-.90f),P(0,.91f,-.22f),45);
        var recorder=eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,"01-repair-variants"));
        var sequence=new List<string>();var hipStart=hips.position;int last=-1;float until=Time.time+48;
        while(Time.time<until) {
            if(perf.CompletedActions!=last){sequence.Add(perf.CurrentState);last=perf.CompletedActions;yield return Capture("action-"+last.ToString("D2")+"-"+perf.CurrentState);}
            result.maxRootTravel=Mathf.Max(result.maxRootTravel,Vector3.Distance(origin,owner.transform.position));
            result.maxHipTravel=Mathf.Max(result.maxHipTravel,Vector3.Distance(hipStart,hips.position));
            result.headDownMinimum=Mathf.Min(result.headDownMinimum,-(front.position-head.position).normalized.y);
            result.maxToolWatchDistance=Mathf.Max(result.maxToolWatchDistance,Vector3.Distance(watchContact.position,toolTip.position));
            result.samples++;yield return null;
        }
        recorder.End();result.sequence=sequence.ToArray();result.completedActions=perf.CompletedActions;
        result.threeActions=sequence.Distinct().Count()==3;result.noAdjacentRepeat=sequence.Zip(sequence.Skip(1),(a,b)=>a!=b).All(x=>x);
        NeedShop(result.threeActions&&result.noAdjacentRepeat,"Three repair variants must play without adjacent repeats");
        NeedShop(result.maxRootTravel<.001f&&result.maxHipTravel<.035f,"Seated character must not drift or rise");result.rootStationary=true;
        NeedShop(result.maxToolWatchDistance<.014f,"Tool tip must remain close to the movement throughout actions and blends");
        NeedShop(result.headDownMinimum>.12f,"All repair actions must keep head lowered toward work");
        Frame(P(-.42f,1.27f,-1.05f),P(0,.97f,-.38f),34);recorder.Begin(Path.Combine(captures,"02-hands-watch-detail"));
        yield return new WaitForSeconds(8);yield return Capture("hands-watch-detail");recorder.End();
        Frame(P(-.85f,.56f,.12f),P(0,.40f,-.20f),60);yield return Capture("chair-feet-side");
        Frame(P(.68f,1.20f,.90f),P(0,.67f,0),45);yield return Capture("seat-apron-back");
        // Exercise the real pause state; ordinary reading above remains live.
        paused=true;yield return null;yield return new WaitForEndOfFrame();time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;var pausedHead=head.rotation;
        yield return new WaitForSecondsRealtime(.6f);
        NeedShop(Mathf.Abs(time-animator.GetCurrentAnimatorStateInfo(0).normalizedTime)<.001f&&Quaternion.Angle(pausedHead,head.rotation)<.01f,"Explicit pause must freeze repair");
        result.pauseFreezes=true;paused=false;
        eye.fieldOfView=fov;reviewPortrait=false;player.enabled=true;player.Place(W(.65f,.03f,.8f),0);player.Aim(head.position);
        result.passed=true;result.verification=Application.platform+" native rendered player; actual controller entry and dialogue ray; recorded live three-clip sequence and close hands/seat/feet views. Detail camera is staged; not physical input or blind playtesting. Source mesh checks supplement visual contact review.";
        File.WriteAllText(Path.Combine(captures,"watchmaker-tripo-results.json"),JsonUtility.ToJson(result,true));Debug.Log("WATCHMAKER_TRIPO_REVIEW_PASS");Application.Quit();
    }
}
