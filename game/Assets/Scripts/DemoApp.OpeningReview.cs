using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
// Focused native review: real controller/rays plus the shared dialogue/inspection handlers.
public partial class DemoApp {
    void ONeed(bool ok,string why){if(!ok)throw new Exception("OPENING_REVIEW_FAIL: "+why+" stage="+state.openingStage+" escort="+state.openingEscort+" player="+player.transform.position);Debug.Log("OPENING_CHECK: "+why);}
    IEnumerator OUse(Interactable item) {
        var collider=item.GetComponent<Collider>();
        player.Aim(item is OpeningActor?item.transform.position+Vector3.up*1.3f:collider?collider.bounds.center:item.transform.position);yield return null;Physics.SyncTransforms();
        ONeed(TryInteract(),"normal E ray reaches "+item.title);yield return null;
    }
    IEnumerator OUntil(Func<bool> done,float timeout,string why){float until=Time.unscaledTime+timeout;while(!done()&&Time.unscaledTime<until)yield return null;ONeed(done(),why);}
    // Walks behind the guard along the trail he actually took (real controller, no teleport).
    IEnumerator FollowGuard(OpeningActor guard,Func<bool> done,float timeout,string doorShot=null) {
        var trail=new Queue<Vector3>();Vector3 last=guard.transform.position;trail.Enqueue(last);
        float until=Time.unscaledTime+timeout;float shotAt=-1;
        while(!done()&&Time.unscaledTime<until) {
            // His hand on the handle, seen from where the player stands.
            if(doorShot!=null&&guardDoorStep==DoorStep.Open&&shotAt<0)shotAt=Time.unscaledTime+.55f;
            if(shotAt>0&&Time.unscaledTime>=shotAt){shotAt=float.MaxValue;var hands=guard.GetComponent<NpcHands>();player.Aim(hands?hands.Palm(true):guard.transform.position+Vector3.up);yield return Capture(doorShot);}
            if(Vector3.Distance(Flat(guard.transform.position),Flat(last))>.3f){last=guard.transform.position;trail.Enqueue(last);}
            if(DialogueOpen){player.scriptedDirection=Vector3.zero;yield return null;continue;}
            while(trail.Count>1&&Vector3.Distance(Flat(player.transform.position),Flat(trail.Peek()))<.35f)trail.Dequeue();
            // A point the player cannot walk straight to (the guard set off from behind the table) is passed over.
            if(TrailBlocked(trail))continue;
            var target=trail.Peek();var d=Flat(target)-Flat(player.transform.position);
            bool close=Vector3.Distance(Flat(player.transform.position),Flat(guard.transform.position))<1.9f;
            player.scriptedDirection=close||d.sqrMagnitude<.01f?Vector3.zero:d.normalized*.8f;
            yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        ONeed(done(),"followed the guard's own route");
    }
    float trailBest=float.MaxValue,trailSince;Vector3 trailPoint;
    bool TrailBlocked(Queue<Vector3> trail) {
        var point=trail.Peek();float distance=Vector3.Distance(Flat(player.transform.position),Flat(point));
        if(point!=trailPoint){trailPoint=point;trailBest=distance;trailSince=Time.unscaledTime;return false;}
        if(distance<trailBest-.05f){trailBest=distance;trailSince=Time.unscaledTime;return false;}
        if(player.scriptedDirection.sqrMagnitude>.01f&&Time.unscaledTime-trailSince>.8f&&trail.Count>1){trail.Dequeue();return true;}
        return false;
    }
    IEnumerator ReviewOpening() {
        yield return new WaitForSeconds(1);
        ONeed(AudioListener.volume==0,"automated verification is silent");
        ONeed(state.openingStage==0&&state.openingEscort==0&&!state.belongingsCollected,"fresh story starts asleep in the holding cell without belongings");
        ONeed(PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="holding","spawn inside cell");
        var guard=FindOpeningActor("guard");ONeed(guard,"guard exists");
        // ---- Cinematic ----
        double clock=ElapsedGameMinutes;
        StartCoroutine(CellCinematic());yield return null;
        ONeed(cellCinematic&&MovementLocked&&!LookAllowed,"cinematic locks movement and look");
        yield return OUntil(()=>cineLids>.95f&&cineBars>.99f,4,"letterbox in, eyes closed");
        ONeed(cineBlur>.9f&&cineVolume&&cineVolume.enabled,"waking up starts out of focus");yield return Capture("opening-01-eyes-closed");
        yield return OUntil(()=>cineLids>.3f&&cineLids<.7f,8,"a heavy blink half opens the eyes");yield return Capture("opening-02-blink");
        yield return OUntil(()=>cineLine=="……头好痛。",12,"his head hurts");yield return new WaitForSeconds(.6f);yield return Capture("opening-03-head-hurts");
        // The guard is nowhere in sight while he takes in the cell: he waits behind its wall.
        yield return OUntil(()=>cineLine!=null&&cineLine.Contains("老照片"),20,"he looks round a room from another time");
        ONeed(cineBlur<.01f&&(!cineVolume||!cineVolume.enabled),"the room comes into focus once he sits up");
        ONeed(OpeningLocal(guard.transform.position).z>22.4f,"nobody stands at the bars before the knock");yield return new WaitForSeconds(.8f);yield return Capture("opening-04-old-room");
        yield return OUntil(()=>cineLine!=null&&cineLine.Contains("穿越"),20,"he wonders whether he has crossed into another time");
        yield return new WaitForSeconds(1.2f);yield return Capture("opening-05-thought");
        // The knock cuts the thought off before it is finished.
        yield return OUntil(()=>cineShake>.5f,8,"knocking shakes the view");
        ONeed(cineLine==null,"the knock interrupts the thought");
        yield return OUntil(()=>cineBlows>=3,4,"three blows on the door");
        ONeed(cineBlowReach>.9f&&cineBlowError<.08f,"each blow is the guard's fist on the bars, not a sound alone (miss "+cineBlowError.ToString("F3")+" m)");
        yield return OUntil(()=>cineSpeaker=="狱警"&&cineLine!=null,6,"the guard calls through the bars");
        ONeed(Vector3.Distance(Flat(guard.transform.position),Flat(cineBarsStand))<.1f&&OpeningLocal(guard.transform.position).x>17.3f,"the guard is close up at the bars when he calls");
        yield return new WaitForSeconds(.8f);yield return Capture("opening-06-knock-guard");
        yield return OUntil(()=>cineLine!=null&&cineLine.Contains("证据不够"),16,"released for want of evidence");
        ONeed(PoliceStation.Current.Door("police-holding-door").IsOpen,"the guard has unlocked the cell");yield return new WaitForSeconds(1);yield return Capture("opening-07-released");
        yield return OUntil(()=>cineLine!=null&&cineLine.Contains("拿你的东西"),14,"brought out into the cloister");yield return new WaitForSeconds(.8f);yield return Capture("opening-08-out");
        yield return OUntil(()=>!cellCinematic,10,"cinematic ends with the player out in the cloister");
        StoreGameClock();ONeed(state.elapsedGameMinutes>clock,"the shared clock keeps running through the cinematic");
        ONeed(state.openingEscort==1&&PoliceStation.Current.Door("police-holding-door").IsOpen&&!Blocked,"control returns outside the open cell; the guard leads off");
        ONeed(!PoliceStation.Current.RoomAt(player.transform.position,out var after)||after.id!="holding","the player stands outside the cell");
        // ---- No belongings yet ----
        SetWatchOpen(true);SetMapOpen(true);ONeed(!watch&&!mapOpen,"no watch and no map before the property room");
        yield return Capture("opening-09-follow");
        yield return FollowGuard(guard,()=>guardTask==GuardTask.Await||guardTask==GuardTask.Fetch,60,"opening-09b-door");
        ONeed(PoliceStation.Current.Door("police-evidence-door").IsOpen,"the guard unlocks the property room");
        ONeed(guardDoorsByHand>=1,"he opened the property-room door with his hand on the handle");
        ONeed(PoliceStation.Current.RoomAt(guard.transform.position,out var store)&&store.id=="evidence"&&store.name=="保管室","the guard is in the property room");
        ONeed(briefcase&&briefcase.gameObject.activeSelf&&propertyCage&&Vector3.Distance(briefcase.transform.position,propertyCage.caseHome.position+Vector3.up*.15f)<.02f&&!briefcase.GetComponent<Collider>().enabled,"the case stands in the locked cage, out of reach");
        // He takes the case out of the cage and lays it on the table, watched from in front of the table.
        yield return WalkTo(PoliceStation.Current.transform.TransformPoint(PlayerTableSpot));player.Aim(guard.transform.position+Vector3.up*1.2f);
        var guardHands=guard.GetComponent<NpcHands>();
        yield return OUntil(()=>propertyCage.Open>.3f,20,"he unlocks the cage and pulls its door open");
        ONeed(guardHands.Weight(true)>.5f,"his hand is on the cage door as it opens");
        player.Aim(Vector3.Lerp(guard.transform.position+Vector3.up*1.3f,guardHands.Palm(true),.5f));yield return Capture("opening-10-cage");
        yield return OUntil(()=>carryingCase&&caseBlend>=1,20,"he lifts the case out by its handle");
        ONeed(guardHands.Weight(true)>.9f&&Vector3.Distance(briefcase.HandlePoint,guardHands.Palm(true))<.03f,"the handle is in his hand");
        player.Aim(briefcase.transform.position+Vector3.up*.2f);yield return Capture("opening-11-carry");
        yield return OUntil(()=>DialogueOpen&&dialogueNode.id=="handover",25,"the guard hands over at the table");
        ONeed(briefcase&&briefcase.gameObject.activeSelf&&Mathf.Abs(OpeningLocal(briefcase.transform.position).y-(PropertyTableTop.y+.15f))<.01f&&briefcase.GetComponent<Collider>().enabled,"the briefcase stands on the table");
        yield return Capture("opening-12-handover");AdvanceDialogue();AdvanceDialogue();ONeed(!DialogueOpen&&guardHandoverDone,"hand-over finished");
        player.Aim(briefcase.transform.position);yield return Capture("opening-13-briefcase");
        yield return OUse(briefcase);
        ONeed(state.belongingsCollected&&!briefcase.gameObject.activeSelf,"E takes the briefcase back");
        yield return new WaitForSeconds(.5f);ONeed(!Blocked&&pickupBannerLeft>0,"the hand-over banner shows before anyone speaks");yield return Capture("opening-14-pickup");
        yield return OUntil(()=>DialogueOpen&&dialogueNode.id=="collected",4,"the guard offers to take him to the lawyer");
        // Closing the line early must not leave the guard standing in the property room.
        CloseDialogue();
        yield return OUntil(()=>state.openingEscort==3,2,"escort to the lawyer begins however the line ends");
        SetWatchOpen(true);ONeed(watch,"watch available after collecting belongings");yield return new WaitForSeconds(.5f);yield return Capture("opening-15-watch");SetWatchOpen(false);
        yield return FollowGuard(guard,()=>state.openingEscort==4,90);
        ONeed(state.openingStage==1,"delivered to the lawyer");
        ONeed(guardDoorsByHand>=2,"he opened the court door by hand too");
        ONeed(Vector3.Distance(Flat(guard.transform.position),Flat(PoliceStation.Current.transform.TransformPoint(GuardHallSpot)))<.1f,"he waits beside the meeting-room door, clear of the way to it");
        if(DialogueOpen){yield return Capture("opening-16-delivered");CloseDialogue();}
        // ---- The lawyer sits; the player takes the chair opposite ----
        yield return WalkTo(OpeningPoint(11.5f,4.6f));yield return WalkTo(OpeningPoint(14,4.6f));yield return WalkTo(OpeningPoint(15.4f,4.42f));
        var lawyerActor=FindOpeningActor("lawyer");player.Aim(lawyerActor.transform.position+Vector3.up*1.2f);
        // Round the side of his chair, never through its back.
        yield return OUntil(()=>lawyerSeat==LawyerSeat.Sitting&&lawyerSitBlend>.45f,10,"the lawyer lowers himself onto the chair from its side");
        float sideOffset=OpeningLocal(lawyerActor.transform.position).x;
        ONeed(sideOffset>LawyerChairSeat.x+.05f&&sideOffset<LawyerChairSide.x+.01f,"he slides onto the seat from the side ("+sideOffset.ToString("F2")+")");
        yield return Capture("opening-17-lawyer-sitting");
        yield return OUntil(()=>lawyerSitting&&lawyerSitBlend>=1,6,"the lawyer sits down at the table");
        yield return new WaitForSeconds(.8f);yield return Capture("opening-18-lawyer-seated");
        FurnitureSeat chair=null;foreach(var seat in FindObjectsByType<FurnitureSeat>(FindObjectsSortMode.None))if(seat.seatId=="police-seat-int")chair=seat;
        ONeed(chair,"meeting chair exists");yield return OUse(chair);
        ONeed(FurnitureSeated&&restingSeat==chair,"E sits on the chair opposite");
        yield return OUntil(()=>DialogueOpen&&dialogueNode.id=="greeting",3,"sitting begins the meeting");
        var lawyerVoice=lawyerActor.GetComponentInChildren<PeriodCharacterPerformance>();
        ONeed(lawyerVoice&&!lawyerVoice.gestures&&lawyerVoice.IsSpeaking,"seated, he speaks with his face and voice, hands at rest");
        yield return new WaitForSeconds(1.2f);yield return Capture("opening-19-meeting");
        // Found before it is picked up: the table copy is hidden while it is held.
        var paper=FindFirstObjectByType<OpeningNewspaper>();ONeed(paper,"the newspaper lies on the meeting table");
        for(int i=0;i<10&&DialogueOpen&&state.openingStage<2;i++)AdvanceDialogue();
        ONeed(state.openingStage==2,"lawyer offers newspaper");
        ONeed(inspected==paper&&ModelEvidence&&!evidenceDetails,"seated, the paper opens in the hands as a physical inspection");
        CloseInspect();ONeed(state.openingStage==2,"unread paper does not complete reading");
        RiseFromFurnitureSeat();yield return null;yield return OUse(chair);
        yield return OUntil(()=>inspected==paper,3,"sitting again brings the paper back");
        evidenceDetails=true;double before=state.elapsedGameMinutes;yield return new WaitForSeconds(1);StoreGameClock();
        ONeed(state.elapsedGameMinutes>before,"reading keeps shared time moving");yield return Capture("opening-20-newspaper");CloseInspect();
        ONeed(state.openingStage==3,"read paper advances stage");
        yield return OUntil(()=>DialogueOpen&&dialogueNode.id=="after",3,"the conversation continues at the table");AdvanceDialogue();
        ONeed(dialogueChoices.Count==4,"lawyer questions available");yield return Capture("opening-21-questions");
        SelectDialogue(0);AdvanceDialogue();SelectDialogue(1);AdvanceDialogue();SelectDialogue(2);ONeed(dialogueNode.id=="child","he answers about the child");yield return Capture("opening-22-child");AdvanceDialogue();SelectDialogue(3);AdvanceDialogue();
        ONeed(state.openingStage==4&&!Blocked,"first act releases free exploration");
        ONeed(RiseFromFurnitureSeat(),"rise from the meeting chair");
        yield return ReviewOpeningLawyerExit();
        var routine=guard.GetComponent<PoliceStaffRoutine>();ONeed(routine&&routine.enabled,"the guard returns to his own round");
        CreatorTeleport(OpeningPoint(16,4.4f));var safe=player.transform.position;ToggleCreatorFlight();
        player.scriptedDirection=Vector3.up;yield return new WaitForSeconds(1);player.scriptedDirection=Vector3.zero;
        ONeed(player.CreatorFlying&&player.transform.position.y>safe.y+5,"creator flight ascends without recovery");Save();
        var saved=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));ONeed(Mathf.Abs(saved.y-safe.y)<.01f,"flight saves safe ground point");
        yield return Capture("opening-23-flight");ToggleCreatorFlight();ONeed(Vector3.Distance(player.transform.position,safe)<.01f,"flight exit returns to launch point");
        creatorMenu=true;ONeed(MovementLocked&&!LookAllowed,"creator menu captures movement and cursor");yield return Capture("opening-24-tools");creatorMenu=false;CursorMode();
        Save();File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"nativeControllerAndRays\":true,\"physicalInputs\":false,\"stage\":4,\"cinematic\":true,\"belongings\":true,\"seatedMeeting\":true}");
        Debug.Log("OPENING_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyOpeningSave() {
        yield return new WaitForSeconds(1);
        ONeed(state.openingStage==4&&state.openingRevision==OpeningRevision&&state.openingEscort==4&&state.belongingsCollected,"opening progress and belongings persist in a second process");
        ONeed(!player.CreatorFlying,"flight disabled on load");
        ONeed(state.entries.FindAll(e=>e.id=="opening-newspaper").Count==1,"newspaper survives without duplicates");
        ONeed(PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="interview","safe ground position restored");
        var briefcaseItem=FindFirstObjectByType<OpeningBriefcase>();ONeed(!briefcaseItem,"collected briefcase does not reappear");
        SetWatchOpen(true);ONeed(watch,"watch still available after reload");SetWatchOpen(false);
        opening=false;yield return Capture("opening-25-reload");Debug.Log("OPENING_SAVE_PASS");Application.Quit();
    }
    // The officers' rounds, sped up: each walks the navigation mesh between its places without
    // passing through walls or furniture, sits down where it has a seat, and is seen doing so.
    IEnumerator ReviewPoliceNpc() {
        yield return new WaitForSeconds(1);
        ONeed(AudioListener.volume==0,"automated verification is silent");
        PoliceStaffRoutine.DurationScale=.12f;
        var routines=new List<PoliceStaffRoutine>(FindObjectsByType<PoliceStaffRoutine>(FindObjectsSortMode.None));
        var guardRoutine=FindOpeningActor("guard")?.GetComponent<PoliceStaffRoutine>();if(guardRoutine&&!guardRoutine.enabled){guardRoutine.enabled=true;}
        if(guardRoutine&&!routines.Contains(guardRoutine))routines.Add(guardRoutine);
        ONeed(routines.Count>=4,"four officers have a daily round ("+routines.Count+")");
        // The observer waits in the reading room, out of every officer's way and away from the duty window.
        var parking=OpeningPoint(4.8f,5.2f);player.Place(parking,OpeningYaw(1,0));
        var overlaps=new List<string>();int samples=0;float until=Time.unscaledTime+95,next=0,nextShot=Time.unscaledTime+10;int shot=0;var seated=new HashSet<PoliceStaffRoutine>();
        var filmed=new HashSet<string>();
        while(Time.unscaledTime<until) {
            if(Time.unscaledTime>=next) {
                next=Time.unscaledTime+.2f;Physics.SyncTransforms();
                foreach(var r in routines) {
                    if(!r.IsWalking)continue;samples++;var feet=r.transform.position;
                    foreach(var hit in Physics.OverlapCapsule(feet+Vector3.up*.4f,feet+Vector3.up*1.45f,.18f,~(1<<2),QueryTriggerInteraction.Ignore)) {
                        if(hit.transform.IsChildOf(r.transform)||hit.transform.IsChildOf(player.transform)||hit.bounds.max.y<=feet.y+.15f)continue;
                        if(hit.GetComponentInParent<ShopMechanism>())continue;
                        if(hit.GetComponentInParent<PoliceStaffRoutine>()||hit.GetComponentInParent<OpeningActor>())continue;
                        overlaps.Add(r.name+"@"+r.CurrentSpot+" "+ORPath(hit.transform)+" feet="+feet.ToString("F2"));
                    }
                }
                foreach(var r in routines)if(r.MaxSit>.95f)seated.Add(r);
            }
            // Each officer filmed once at each kind of work once settled into it, from a free spot
            // beside or in front with a clear view (never from inside a wall or a desk).
            if(Time.unscaledTime>=nextShot&&shot<16) {
                nextShot=Time.unscaledTime+2.5f;
                foreach(var r in routines) {
                    var pose=r.GetComponent<NpcPose>();
                    if(r.IsWalking||!pose||!pose.Settled||r.CurrentSpot.Length==0)continue;
                    string key=r.name+"/"+pose.activity;if(filmed.Contains(key))continue;
                    if(!NpcCamera(r,out var eye))continue;
                    filmed.Add(key);shot++;
                    player.Place(new Vector3(eye.x,r.transform.position.y,eye.z),0);player.Aim(r.transform.position+Vector3.up*(pose.Sit>.5f?.95f:1.3f));yield return null;
                    yield return Capture("police-npc-"+shot.ToString("00")+"-"+r.CurrentSpot);
                    player.Place(parking,OpeningYaw(1,0));
                    break;
                }
            }
            yield return null;
        }
        var report=new List<string>();
        foreach(var r in routines) {
            report.Add(r.name+": visits="+r.Visits+" walked="+r.WalkedMetres.ToString("F1")+" corners="+r.PathCorners+" doors="+r.DoorsOpened+" sidesteps="+r.Sidesteps+" maxSit="+r.MaxSit.ToString("F2")+(seated.Contains(r)?" sat-on-own-round":""));
            ONeed(r.Visits>=3&&r.WalkedMetres>4,"officer keeps moving between places: "+r.name+" visits="+r.Visits+" walked="+r.WalkedMetres);
        }
        // Which place comes up is chance; anyone with a seat who did not happen to choose it in this
        // short window is sent to it now, so walking there and sitting down are always seen.
        foreach(var r in routines) {
            if(!r.HasSeat||seated.Contains(r))continue;
            ONeed(r.GoTo(r.SeatSpot),"officer can be sent to his seat: "+r.name);
            yield return OUntil(()=>r.MaxSit>.95f,30,"officer walks to his seat and sits down: "+r.name+" ("+r.SeatSpot+")");
            seated.Add(r);report.Add(r.name+": sat when sent to "+r.SeatSpot);
        }
        ONeed(samples>50,"walking sampled "+samples+" times");
        ONeed(overlaps.Count==0,"no officer walks through walls or furniture: "+string.Join(" | ",overlaps.GetRange(0,Mathf.Min(6,overlaps.Count))));
        ONeed(shot>=6,"officers filmed at "+shot+" kinds of work: "+string.Join(", ",filmed));
        // Somebody waiting at the duty window brings the duty officer back to it.
        PoliceStaffRoutine duty=null;foreach(var r in routines)if(Array.Exists(r.spots,s=>s.post))duty=r;
        ONeed(duty,"the duty officer has a post");
        player.Place(OpeningPoint(11.9f,8.1f),OpeningYaw(0,1));
        yield return OUntil(()=>duty.CurrentSpot=="值班窗口"&&!duty.IsWalking,25,"the duty officer comes to the window for a visitor");
        yield return new WaitForSeconds(1.5f);player.Aim(duty.transform.position+Vector3.up*1.5f);yield return Capture("police-npc-window");
        ONeed(duty.CurrentSpot=="值班窗口","he stays at the window while the visitor waits");
        // Two people meeting head-on in the cross hall keep to their right and pass without touching.
        PoliceStaffRoutine reception=null;foreach(var r in routines)if(r.name.StartsWith("接待女警"))reception=r;
        ONeed(reception,"the reception officer has a round");
        player.Place(parking,OpeningYaw(1,0));yield return null;
        int sidesteps=duty.Sidesteps+reception.Sidesteps;
        duty.StageWalk(new Vector3(2.6f,.15f,7.95f),new Vector3(12.6f,.15f,7.95f));reception.StageWalk(new Vector3(12.6f,.15f,7.95f),new Vector3(2.6f,.15f,7.95f));
        Debug.Log("POLICE_NPC_STAGED: duty "+duty.LastPlan+" | reception "+reception.LastPlan);
        float closest=99,meet=Time.unscaledTime+25;bool filmedMeeting=false;
        while(Time.unscaledTime<meet&&!(duty.CurrentSpot=="staged"&&reception.CurrentSpot=="staged")) {
            float gap=Vector3.Distance(Flat(duty.transform.position),Flat(reception.transform.position));closest=Mathf.Min(closest,gap);
            if(!filmedMeeting&&gap<1.3f){filmedMeeting=true;player.Place(OpeningPoint(7.4f,10.6f),0);player.Aim((duty.transform.position+reception.transform.position)/2+Vector3.up*1.3f);yield return null;yield return Capture("police-npc-passing");player.Place(parking,OpeningYaw(1,0));}
            yield return null;
        }
        ONeed(duty.CurrentSpot=="staged"&&reception.CurrentSpot=="staged","both reach the far end of the cross hall (duty: "+duty.State+" | reception: "+reception.State+" | closest "+closest.ToString("F2")+")");
        ONeed(closest>.5f,"they pass without walking through each other (closest "+closest.ToString("F2")+" m)");
        ONeed(duty.Sidesteps+reception.Sidesteps>sidesteps,"they step aside for each other");
        report.Add("cross-hall meeting: closest="+closest.ToString("F2")+"m sidesteps="+(duty.Sidesteps+reception.Sidesteps-sidesteps));
        File.WriteAllText(Path.Combine(captures,"acceptance.json"),JsonUtility.ToJson(new NpcReviewResult{passed=true,officers=routines.Count,samples=samples,report=report.ToArray()},true));
        Debug.Log("POLICE_NPC_REVIEW_PASS: "+string.Join(" ; ",report));Application.Quit();
    }
    [Serializable] class NpcReviewResult{public bool passed;public int officers,samples;public string[] report;}
    // A free place to film an officer from: front-side first, then the sides, then from behind.
    bool NpcCamera(PoliceStaffRoutine r,out Vector3 eye) {
        var head=r.transform.position+Vector3.up*1.35f;var face=-r.transform.forward;face.y=0;face.Normalize();
        foreach(float angle in new[]{35f,-35f,70f,-70f,0f,110f,-110f,150f,-150f})foreach(float distance in new[]{1.8f,1.4f,2.3f}) {
            eye=head+Quaternion.AngleAxis(angle,Vector3.up)*face*distance+Vector3.up*.25f;
            var feet=new Vector3(eye.x,r.transform.position.y,eye.z);
            if(Physics.CheckCapsule(feet+Vector3.up*.35f,eye,.28f,~(1<<2),QueryTriggerInteraction.Ignore))continue;
            if(!PoliceStation.Current.RoomAt(eye,out _))continue;
            if(Physics.Linecast(eye,head,out var hit,~(1<<2),QueryTriggerInteraction.Ignore)&&!hit.transform.IsChildOf(r.transform))continue;
            return true;
        }
        eye=default;return false;
    }
}
