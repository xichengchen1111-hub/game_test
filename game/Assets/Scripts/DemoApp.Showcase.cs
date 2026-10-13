using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Presentation run of the first act: the real controller, doors, dialogue, inspection and car
// ride at a pace a viewer can follow, recorded by ShowcaseRecorder. Started with
// --review --fresh --showcase-opening; it uses its own save. Nothing here is a test.
public partial class DemoApp {
    ShowcaseRecorder showcase;
    bool showcaseTitle;
    const float ShowcaseListenerVolume=.05f;
    void ShowLook(float yaw,float pitch){player.transform.localRotation=Quaternion.Euler(0,yaw,0);player.pitch=pitch;player.eye.transform.localRotation=Quaternion.Euler(pitch,0,0);}
    IEnumerator ShowLookAt(Vector3 point,float seconds) {
        float yaw0=player.transform.localEulerAngles.y,pitch0=player.pitch;
        player.Aim(point);float yaw1=player.transform.localEulerAngles.y,pitch1=player.pitch;ShowLook(yaw0,pitch0);
        for(float t=0;t<seconds;t+=Time.deltaTime){float k=Mathf.SmoothStep(0,1,t/seconds);ShowLook(Mathf.LerpAngle(yaw0,yaw1,k),Mathf.Lerp(pitch0,pitch1,k));yield return null;}
        ShowLook(yaw1,pitch1);
    }
    // Walk with the view easing toward the direction of travel, like a player steering with the mouse.
    IEnumerator ShowWalk(Vector3 target,float speedFactor=1,float pitch=3) {
        float until=Time.time+30;
        while(Time.time<until) {
            Vector3 d=target-player.transform.position;d.y=0;if(d.magnitude<.12f)break;
            player.scriptedDirection=d.normalized*speedFactor;
            float yaw=Mathf.MoveTowardsAngle(player.transform.localEulerAngles.y,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,Time.deltaTime*150);
            ShowLook(yaw,Mathf.MoveTowards(player.pitch,pitch,Time.deltaTime*40));yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        if(Time.time>=until)ShowAbort("walk blocked before "+target+" at "+player.transform.position);
    }
    bool showcaseFailed;
    // A nested coroutine that throws would strand the recorder; stop cleanly and keep what was filmed.
    bool ShowAbort(string why) {
        if(showcaseFailed)return true;showcaseFailed=true;Debug.LogError("SHOWCASE_FAIL: "+why);
        player.scriptedDirection=Vector3.zero;if(showcase)showcase.End();AudioListener.volume=0;Application.Quit();return true;
    }
    IEnumerator ShowHold(float seconds){for(float t=0;t<seconds;t+=Time.deltaTime)yield return null;}
    // Reading time grows with the line, as a player would take.
    IEnumerator ShowLine(float extra=0){yield return ShowHold(Mathf.Clamp(1.4f+(dialogueNode!=null?dialogueNode.text.Length:0)*.085f,2.4f,6.5f)+extra);}
    IEnumerator ShowUse(Interactable item,Vector3 point,float turn=.7f) {
        yield return ShowLookAt(point,turn);yield return ShowHold(.35f);Physics.SyncTransforms();
        if(!TryInteract())ShowAbort("E does not reach "+item.title+" from "+player.transform.position);
        yield return null;
    }
    IEnumerator ShowDoor(ShopMechanism door) {
        if(!door||door.IsOpen)yield break;
        yield return ShowLookAt(door.GetComponentInChildren<BoxCollider>().bounds.center,.7f);yield return ShowHold(.35f);Physics.SyncTransforms();
        if(!TryInteract())door.Use();
        yield return ShowHold(1.25f);
    }
    void ShowChoose(string target) {
        int index=dialogueChoices.FindIndex(c=>c.target==target);
        if(index<0){ShowAbort("no dialogue choice "+target);return;}
        SelectDialogue(index);
    }
    IEnumerator ShowcaseOpening() {
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;dayNightReviewFrozen=false;
        yield return new WaitForSeconds(1.5f);
        if(state.openingStage!=0||!PoliceStation.Current){ShowAbort("needs a fresh first act");yield break;}
        Silent=false; // the lawyer's on-screen reminders are part of the act
        // The recorder taps the listener, so the mix has to be above zero; it stays very quiet in the room.
        AudioListener.volume=ShowcaseListenerVolume;
        showcase=player.eye.gameObject.AddComponent<ShowcaseRecorder>();
        string folder=Path.Combine(captures,"showcase-opening");
        showcaseTitle=true;opening=true;CursorMode();yield return null;
        showcase.Begin(folder);showcase.Mark("title");
        yield return ShowHold(4.2f);
        opening=false;showcaseTitle=false;CursorMode();showcase.Mark("cell");
        var guard=FindOpeningActor("guard");var lawyer=FindOpeningActor("lawyer");
        yield return ShowHold(.8f);
        yield return ShowLookAt(OpeningPoint(20.25f,23.6f)+Vector3.up*2.1f,1.6f);yield return ShowHold(.9f);
        yield return ShowLookAt(guard.transform.position+Vector3.up*1.55f,1.5f);yield return ShowHold(.5f);
        showcase.Mark("guard");OpenOpeningActor(guard);yield return ShowLine();
        AdvanceDialogue();yield return ShowLine();
        AdvanceDialogue();showcase.Mark("cell-door");
        if(state.openingStage!=1){ShowAbort("guard did not release the cell");yield break;}
        yield return ShowHold(1.9f);
        yield return ShowWalk(OpeningPoint(18.25f,20.6f),.8f);yield return ShowWalk(OpeningPoint(16.3f,20.5f),.8f);
        yield return ShowLookAt(guard.transform.position+Vector3.up*1.55f,.7f);yield return ShowHold(.5f);
        showcase.Mark("court");
        yield return ShowWalk(OpeningPoint(15,17));
        // Glance across the court to its closed north end before turning to the main range.
        yield return ShowLookAt(OpeningPoint(11.5f,23.8f)+Vector3.up*1.4f,1.2f);yield return ShowHold(1.1f);
        // Q: the pocket watch.
        showcase.Mark("watch");yield return ShowLookAt(OpeningPoint(9.5f,14.5f)+Vector3.up*1.5f,1.0f);
        SetWatchOpen(true);yield return ShowHold(3.4f);SetWatchOpen(false);yield return ShowHold(.5f);
        yield return ShowWalk(OpeningPoint(7.5f,13.3f));
        yield return ShowDoor(PoliceStation.Current.Door("police-court-door"));
        showcase.Mark("front-range");
        yield return ShowWalk(OpeningPoint(7.4f,10));yield return ShowWalk(OpeningPoint(7.4f,8));yield return ShowWalk(OpeningPoint(11.5f,8));
        yield return ShowDoor(PoliceStation.Current.Door("police-hall-door"));
        // Stand west of the table, clear of the route the lawyer later takes to the door.
        yield return ShowWalk(OpeningPoint(11.5f,4.6f));yield return ShowWalk(OpeningPoint(14,4.6f));yield return ShowWalk(OpeningPoint(14.7f,3.7f),.8f);
        showcase.Mark("lawyer");
        yield return ShowUse(lawyer,lawyer.transform.position+Vector3.up*1.5f);
        for(int i=0;i<4;i++){yield return ShowLine();AdvanceDialogue();}
        if(state.openingStage!=2){ShowAbort("lawyer did not offer the newspaper");yield break;}
        showcase.Mark("newspaper");
        var paper=FindFirstObjectByType<OpeningNewspaper>();
        yield return ShowUse(paper,paper.GetComponent<Collider>().bounds.center,.9f);
        yield return ShowHold(2.6f);ToggleEvidenceDetails();showcase.Mark("newspaper-text");yield return ShowHold(6.5f);
        CloseInspect();yield return ShowHold(.6f);
        if(state.openingStage!=3){ShowAbort("reading did not advance the act");yield break;}
        showcase.Mark("questions");
        yield return ShowUse(lawyer,lawyer.transform.position+Vector3.up*1.5f);
        yield return ShowLine();AdvanceDialogue();yield return ShowHold(2.2f);
        ShowChoose("suspect");yield return ShowLine(.4f);AdvanceDialogue();yield return ShowHold(1.4f);
        ShowChoose("release");showcase.Mark("release");yield return ShowLine(.4f);AdvanceDialogue();
        if(state.openingStage!=4||state.openingRideStage!=1){ShowAbort("release did not open the investigation");yield break;}
        // Watch the lawyer round the table and leave, then follow him through the hall.
        showcase.Mark("follow-out");
        float patience=Time.time+12;
        while(Time.time<patience&&PoliceStation.Current.transform.InverseTransformPoint(lawyer.transform.position).x>13.4f)yield return ShowLookAt(lawyer.transform.position+Vector3.up*1.45f,.25f);
        yield return ShowWalk(OpeningPoint(14,4.6f),.68f);yield return ShowWalk(OpeningPoint(11.5f,4.6f),.68f);
        yield return ShowWalk(OpeningPoint(11.5f,1.4f),.68f);
        patience=Time.time+40;while(lawyerWalk.Count>0&&Time.time<patience)yield return ShowLookAt(lawyer.transform.position+Vector3.up*1.45f,.3f);
        yield return ShowWalk(OpeningPoint(11.5f,-.9f),.8f);yield return ShowWalk(OpeningPoint(10.0f,-2.3f),.8f);
        yield return ShowWalk(OpeningPoint(9.9f,-3.9f),.8f);
        // M: the paper map, unfolded on the pavement outside the station.
        showcase.Mark("map");yield return ShowHold(.4f);
        SetMapOpen(true);yield return ShowHold(4.2f);SetMapOpen(false);yield return ShowHold(.6f);
        yield return ShowWalk(OpeningPoint(11.4f,-4.3f),.8f);
        showcase.Mark("offer");
        yield return ShowUse(lawyer,lawyer.transform.position+Vector3.up*1.5f,1.0f);
        yield return ShowLine(1.2f);ShowChoose("accept");showcase.Mark("accept");yield return ShowLine();AdvanceDialogue();
        if(state.openingRideStage!=2||!rideCar){ShowAbort("lift was not accepted");yield break;}
        showcase.Mark("to-car");
        yield return ShowHold(1.2f);
        yield return ShowWalk(new Vector3(26.5f,.12f,25.4f),.8f);yield return ShowWalk(rideCar.PassengerStandingPoint,.8f);
        var boarding=FindFirstObjectByType<OpeningRideBoarding>();
        yield return ShowLookAt(rideCar.PassengerSeat.position+Vector3.up*.75f,.8f);yield return ShowHold(.6f);
        showcase.Mark("boarding");Physics.SyncTransforms();
        if(!TryInteract()||!OpeningRideActive){boarding.Use();if(!OpeningRideActive){ShowAbort("could not board the car");yield break;}}
        float limit=Time.time+40;while((OpeningRideCameraMoving||rideCar.Speed<.5f)&&Time.time<limit)yield return null;
        showcase.Mark("driving");
        Transform head=rideCar.Driver.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Head");
        yield return ShowHold(3.5f);
        if(head){yield return ShowLookAt(head.position,1.2f);yield return ShowHold(2.2f);}
        yield return ShowLookTo(0,0,1.4f);yield return ShowHold(5f);
        showcase.Mark("street-right");yield return ShowLookTo(62,2,1.6f);yield return ShowHold(4f);
        yield return ShowLookTo(0,0,1.6f);
        limit=Time.time+120;float glance=Time.time+9;
        while(state.openingRideStage<4&&Time.time<limit) {
            if(!OpeningRideCameraMoving&&rideCar&&rideCar.Speed>5&&Time.time>glance){glance=Time.time+11;yield return ShowLookTo(34,1,1.5f);yield return ShowHold(2.4f);yield return ShowLookTo(0,0,1.5f);}
            yield return null;
        }
        if(state.openingRideStage<4){ShowAbort("car did not reach the hospital");yield break;}
        showcase.Mark("goodbye");
        yield return ShowLine(1.2f);AdvanceDialogue();
        showcase.Mark("departure");
        limit=Time.time+7;while(Time.time<limit&&rideCar){yield return ShowLookAt(rideCar.transform.position+Vector3.up*1.1f,.25f);}
        showcase.Mark("end");yield return ShowHold(1.5f);
        showcase.End();AudioListener.volume=0;
        Debug.Log("SHOWCASE_OPENING_DONE");Application.Quit();
    }
    IEnumerator ShowLookTo(float yaw,float pitch,float seconds) {
        float yaw0=player.transform.localEulerAngles.y,pitch0=player.pitch;
        for(float t=0;t<seconds;t+=Time.deltaTime) {
            // The ride owns the view while it seats or unseats the passenger.
            if(OpeningRideCameraMoving)yield break;
            float k=Mathf.SmoothStep(0,1,t/seconds);ShowLook(Mathf.LerpAngle(yaw0,yaw,k),Mathf.Lerp(pitch0,pitch,k));yield return null;
        }
        ShowLook(yaw,pitch);
    }
}
