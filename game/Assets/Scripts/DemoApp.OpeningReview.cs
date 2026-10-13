using System;
using System.Collections;
using System.IO;
using UnityEngine;
// Focused native review: real controller/rays plus the shared dialogue/inspection handlers.
public partial class DemoApp {
    void ONeed(bool ok,string why){if(!ok)throw new Exception("OPENING_REVIEW_FAIL: "+why);Debug.Log("OPENING_CHECK: "+why);}
    IEnumerator OUse(Interactable item) {
        player.Aim(item.transform.position+Vector3.up*(item is OpeningActor?1.3f:0));yield return null;Physics.SyncTransforms();
        ONeed(TryInteract(),"normal E ray reaches "+item.title);yield return null;
    }
    IEnumerator ReviewOpening() {
        yield return new WaitForSeconds(1);
        ONeed(state.openingStage==0,"fresh story starts in holding cell");
        ONeed(PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="holding","spawn inside cell");
        yield return Capture("opening-01-cell");
        var guard=FindOpeningActor("guard");yield return OUse(guard);
        ONeed(DialogueOpen&&MovementLocked,"guard dialogue blocks movement");yield return Capture("opening-02-guard");
        // Closing early must be recoverable through the same actor.
        CloseDialogue();yield return OUse(guard);AdvanceDialogue();AdvanceDialogue();
        ONeed(state.openingStage==1&&!DialogueOpen,"guard notification unlocks the physical cell door");
        yield return new WaitForSeconds(1);ONeed(PoliceStation.Current.Door("police-holding-door").IsOpen,"cell door open");
        yield return WalkTo(OpeningPoint(18.25f,20.6f));yield return WalkTo(OpeningPoint(16.1f,20.6f));
        yield return WalkTo(OpeningPoint(15,17));yield return WalkTo(OpeningPoint(7.4f,13));
        var court=PoliceStation.Current.Door("police-court-door");
        if(court&&!court.IsOpen){player.Aim(court.transform.position+Vector3.up);if(!TryInteract())court.Use();yield return new WaitForSeconds(1);}
        yield return WalkTo(OpeningPoint(7.4f,10));yield return WalkTo(OpeningPoint(7.4f,8));
        yield return WalkTo(OpeningPoint(11.5f,8));
        var hall=PoliceStation.Current.Door("police-hall-door");if(hall&&!hall.IsOpen){hall.Use();yield return new WaitForSeconds(1);}
        yield return WalkTo(OpeningPoint(11.5f,4.6f));yield return WalkTo(OpeningPoint(14,4.6f));yield return WalkTo(OpeningPoint(17.5f,3.7f));
        var lawyer=FindOpeningActor("lawyer");yield return OUse(lawyer);
        for(int i=0;i<4;i++)AdvanceDialogue();
        ONeed(state.openingStage==2,"lawyer offers newspaper");
        var paper=FindFirstObjectByType<OpeningNewspaper>();yield return OUse(paper);
        ONeed(ModelEvidence&&!evidenceDetails&&MovementLocked,"paper starts as physical inspection");
        CloseInspect();ONeed(state.openingStage==2,"unread paper does not complete reading");yield return OUse(paper);
        evidenceDetails=true;double before=state.elapsedGameMinutes;yield return new WaitForSeconds(1);StoreGameClock();
        ONeed(state.elapsedGameMinutes>before,"reading keeps shared time moving");yield return Capture("opening-03-newspaper");CloseInspect();
        ONeed(state.openingStage==3,"read paper advances stage");yield return OUse(lawyer);AdvanceDialogue();
        ONeed(dialogueChoices.Count==3,"lawyer questions available");yield return Capture("opening-04-lawyer");SelectDialogue(0);AdvanceDialogue();SelectDialogue(1);AdvanceDialogue();SelectDialogue(2);AdvanceDialogue();
        ONeed(state.openingStage==4&&!Blocked,"first act releases free exploration");
        yield return ReviewOpeningLawyerExit();
        CreatorTeleport(OpeningPoint(16,4.4f));var safe=player.transform.position;ToggleCreatorFlight();
        player.scriptedDirection=Vector3.up;yield return new WaitForSeconds(1);player.scriptedDirection=Vector3.zero;
        ONeed(player.CreatorFlying&&player.transform.position.y>safe.y+5,"creator flight ascends without recovery");Save();
        var saved=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));ONeed(Mathf.Abs(saved.y-safe.y)<.01f,"flight saves safe ground point");
        yield return Capture("opening-05-flight");ToggleCreatorFlight();ONeed(Vector3.Distance(player.transform.position,safe)<.01f,"flight exit returns to launch point");
        creatorMenu=true;ONeed(MovementLocked&&!LookAllowed,"creator menu captures movement and cursor");yield return Capture("opening-06-tools");creatorMenu=false;CursorMode();
        Save();File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"nativeControllerAndRays\":true,\"physicalInputs\":false,\"stage\":4}");
        Debug.Log("OPENING_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyOpeningSave() {
        yield return new WaitForSeconds(1);
        ONeed(state.openingStage==4&&state.openingRevision==1,"opening progress persists in a second process");
        ONeed(!player.CreatorFlying,"flight disabled on load");
        ONeed(state.entries.FindAll(e=>e.id=="opening-newspaper").Count==1,"newspaper survives without duplicates");
        ONeed(PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="interview","safe ground position restored");
        opening=false;yield return Capture("opening-07-reload");Debug.Log("OPENING_SAVE_PASS");Application.Quit();
    }
}
