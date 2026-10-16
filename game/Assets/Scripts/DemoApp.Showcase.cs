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
    // Moments inside the act itself (the knock, the key, the cage), for cutting the short version.
    void ShowMark(string name){if(showcase)showcase.Mark(name);}
    // Walk behind the guard along the way he actually took, looking where he goes.
    IEnumerator ShowFollow(OpeningActor guard,Func<bool> done,float timeout) {
        var trail=new System.Collections.Generic.Queue<Vector3>();Vector3 last=guard.transform.position;trail.Enqueue(last);
        float until=Time.time+timeout;
        while(!done()&&Time.time<until) {
            if(Vector3.Distance(Flat(guard.transform.position),Flat(last))>.3f){last=guard.transform.position;trail.Enqueue(last);}
            if(DialogueOpen){player.scriptedDirection=Vector3.zero;yield return null;continue;}
            // While he opens a door, step out to one side of it and watch his hand on the handle,
            // instead of following straight behind him and seeing the wall beside the doorway.
            if(guardTask==GuardTask.Route&&(guardDoorStep==DoorStep.Turn||guardDoorStep==DoorStep.Open)&&guardRouteDoors.TryGetValue(guardRouteIndex,out var doorId)&&PoliceStation.Current.Door(doorId)) {
                var door=PoliceStation.Current.Door(doorId);var hands=guard.GetComponent<NpcHands>();
                // Just behind him on the side of the hand that takes the handle: the line of sight runs in
                // through the doorway (the leaf sits deep in the wall), past his shoulder to the handle.
                var view=guardDoorStand+guardDoorToward*1.1f+door.ClosedAlong*.35f;var dv=Flat(view)-Flat(player.transform.position);
                player.scriptedDirection=dv.magnitude>.12f?dv.normalized*.6f:Vector3.zero;
                var watch=Vector3.Lerp(guard.transform.position+Vector3.up*1.2f,hands?hands.Palm(guardDoorRight):door.DoorwayCentre+Vector3.up,.8f)-player.eye.transform.position;
                float watchYaw=Mathf.MoveTowardsAngle(player.transform.localEulerAngles.y,Mathf.Atan2(watch.x,watch.z)*Mathf.Rad2Deg,Time.deltaTime*120);
                ShowLook(watchYaw,Mathf.MoveTowards(player.pitch,-Mathf.Atan2(watch.y,new Vector2(watch.x,watch.z).magnitude)*Mathf.Rad2Deg,Time.deltaTime*40));
                yield return null;continue;
            }
            while(trail.Count>1&&Vector3.Distance(Flat(player.transform.position),Flat(trail.Peek()))<.35f)trail.Dequeue();
            if(TrailBlocked(trail)){yield return null;continue;}
            var d=Flat(trail.Peek())-Flat(player.transform.position);
            bool close=Vector3.Distance(Flat(player.transform.position),Flat(guard.transform.position))<2.0f;
            player.scriptedDirection=close||d.sqrMagnitude<.01f?Vector3.zero:d.normalized*.75f;
            var to=guard.transform.position+Vector3.up*1.4f-player.eye.transform.position;
            float yaw=Mathf.MoveTowardsAngle(player.transform.localEulerAngles.y,Mathf.Atan2(to.x,to.z)*Mathf.Rad2Deg,Time.deltaTime*120);
            ShowLook(yaw,Mathf.MoveTowards(player.pitch,4,Time.deltaTime*40));
            yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        if(!done())ShowAbort("lost the guard at "+player.transform.position);
    }
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
        // The cinematic plays itself: waking up, the knock, the release and out into the cloister.
        StartCoroutine(CellCinematic());yield return null;
        float wait=Time.time+80;while(cellCinematic&&Time.time<wait)yield return null;
        if(cellCinematic||state.openingEscort!=1){ShowAbort("the cell cinematic did not hand over to the escort");yield break;}
        showcase.Mark("escort");yield return ShowHold(.5f);
        yield return ShowFollow(guard,()=>guardTask==GuardTask.Fetch||guardTask==GuardTask.Await,60);
        if(showcaseFailed)yield break;
        // At the property-room table, watching the guard take the case out of the cage.
        showcase.Mark("property-room");
        yield return ShowWalk(PoliceStation.Current.transform.TransformPoint(PlayerTableSpot),.7f);
        // Watching his hands: the padlock, the cage door, the case lifted out and carried to the table.
        var guardHands=guard.GetComponent<NpcHands>();
        Vector3 Watch()=>guardHands&&guardHands.Busy?Vector3.Lerp(guard.transform.position+Vector3.up*1.3f,guardHands.Palm(true),.55f):guard.transform.position+Vector3.up*1.35f;
        yield return ShowLookAt(Watch(),.8f);
        wait=Time.time+40;while(!(DialogueOpen&&dialogueNode.id=="handover")&&Time.time<wait)yield return ShowLookAt(Watch(),.3f);
        if(!DialogueOpen){ShowAbort("the guard did not hand the case over");yield break;}
        yield return ShowLine();AdvanceDialogue();yield return ShowLine();AdvanceDialogue();
        showcase.Mark("briefcase");
        yield return ShowUse(briefcase,briefcase.GetComponent<Collider>().bounds.center,.9f);
        if(!state.belongingsCollected){ShowAbort("E did not take the briefcase");yield break;}
        yield return ShowHold(2.0f);
        wait=Time.time+5;while(!(DialogueOpen&&dialogueNode.id=="collected")&&Time.time<wait)yield return null;
        if(DialogueOpen){yield return ShowLine();AdvanceDialogue();}
        // Q: the pocket watch, now that it is back.
        showcase.Mark("watch");yield return ShowHold(.5f);SetWatchOpen(true);yield return ShowHold(3.2f);SetWatchOpen(false);yield return ShowHold(.4f);
        // The briefcase stays with him: B opens it, the ticket and the rest inside.
        showcase.Mark("pack");OpenBackpack();yield return ShowHold(2.2f);MovePackSelection(1,0);yield return ShowHold(.8f);MovePackSelection(1,0);yield return ShowHold(1.4f);CloseBackpack();yield return ShowHold(.5f);
        showcase.Mark("to-lawyer");
        yield return ShowFollow(guard,()=>state.openingEscort==4,90);
        if(showcaseFailed)yield break;
        if(DialogueOpen){yield return ShowLine();AdvanceDialogue();}
        // The lawyer greets him, goes round to his chair and sits; the player takes the chair opposite.
        yield return ShowWalk(OpeningPoint(11.5f,4.6f),.8f);yield return ShowWalk(OpeningPoint(14,4.6f),.8f);
        showcase.Mark("lawyer");
        wait=Time.time+12;while(!(lawyerSitting&&lawyerSitBlend>=1)&&Time.time<wait)yield return ShowLookAt(lawyer.transform.position+Vector3.up*1.3f,.25f);
        yield return ShowHold(.6f);yield return ShowWalk(OpeningPoint(15.4f,4.42f),.7f);
        FurnitureSeat chair=null;foreach(var seat in FindObjectsByType<FurnitureSeat>(FindObjectsSortMode.None))if(seat.seatId=="police-seat-int")chair=seat;
        if(!chair){ShowAbort("no meeting chair");yield break;}
        yield return ShowUse(chair,chair.GetComponent<Collider>().bounds.center,.8f);
        wait=Time.time+4;while(!(DialogueOpen&&dialogueNode.id=="greeting")&&Time.time<wait)yield return null;
        if(!DialogueOpen){ShowAbort("sitting down did not begin the meeting");yield break;}
        for(int i=0;i<10&&DialogueOpen&&state.openingStage<2;i++){yield return ShowLine();AdvanceDialogue();}
        if(state.openingStage!=2||!(inspected is OpeningNewspaper)){ShowAbort("the lawyer did not hand over the newspaper");yield break;}
        showcase.Mark("newspaper");
        yield return ShowHold(2.6f);ToggleEvidenceDetails();showcase.Mark("newspaper-text");yield return ShowHold(6.5f);
        CloseInspect();yield return ShowHold(.6f);
        if(state.openingStage!=3){ShowAbort("reading did not advance the act");yield break;}
        showcase.Mark("questions");
        wait=Time.time+4;while(!(DialogueOpen&&dialogueNode.id=="after")&&Time.time<wait)yield return null;
        yield return ShowLine();AdvanceDialogue();yield return ShowHold(2.2f);
        ShowChoose("suspect");yield return ShowLine(.4f);AdvanceDialogue();yield return ShowHold(1.4f);
        ShowChoose("child");showcase.Mark("child");yield return ShowLine(.6f);AdvanceDialogue();yield return ShowHold(1.4f);
        ShowChoose("release");showcase.Mark("release");yield return ShowLine(.4f);AdvanceDialogue();
        if(state.openingStage!=4||state.openingRideStage!=1){ShowAbort("release did not open the investigation");yield break;}
        yield return ShowHold(.8f);RiseFromFurnitureSeat();
        // Watch the lawyer get up and leave, then follow him through the hall.
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
