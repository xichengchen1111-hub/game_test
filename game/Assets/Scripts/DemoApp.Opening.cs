using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// First act, revision 2 (user, 2026-10-15): a first-person cinematic wakes the player in the cell;
// the guard knocks, releases him for want of evidence and leads him to the property room, where
// the briefcase with his belongings (watch, map, ticket, notebook) is handed back. Only then does
// the guard take him to the lawyer, who sits down at the meeting table; the player can take the
// chair opposite with E. The lawyer, newspaper and release that follow are unchanged.
public partial class DemoApp {
    public const int OpeningRevision=2;
    DialogueScript openingScript;
    bool openingNoticePlayed;
    void InitializeOpeningState(string[] args,bool existing) {
        bool actReview=Review&&(Array.IndexOf(args,"--review-opening")>=0||Array.IndexOf(args,"--verify-opening-save")>=0||Array.IndexOf(args,"--review-police-cast")>=0||Array.IndexOf(args,"--showcase-opening")>=0);
        bool legacyInvestigation=state.openingRevision==0&&(existing||Review&&!actReview||previewWindow||previewStreet||previewMilkboy||previewWatchmaker);
        if(state.openingRevision==0) {
            bool skip=existing||Review&&!actReview||previewWindow||previewStreet||previewMilkboy||previewWatchmaker;
            state.openingStage=skip?4:0;state.openingEscort=skip?4:0;state.belongingsCollected=skip;
        } else if(state.openingRevision==1) {
            // The first revision had no property room: a save past the cell keeps its belongings,
            // one still waiting in the cell starts the new wake-up.
            state.openingEscort=state.openingStage>=1?4:0;state.belongingsCollected=state.openingStage>=1;
        }
        state.openingRevision=OpeningRevision;
        if(state.openingStage>=1){state.openingEscort=4;state.belongingsCollected=true;}
        InitializeOpeningRideState(legacyInvestigation);
    }
    Vector3 OpeningPoint(float x,float z) => PoliceStation.Current.transform.TransformPoint(new Vector3(x,PoliceStation.Current.Layout.levels.ground+.025f,z));
    Vector3 OpeningLocal(Vector3 world) => PoliceStation.Current.transform.InverseTransformPoint(world);
    float OpeningYaw(float dx,float dz) => Quaternion.LookRotation(PoliceStation.Current.transform.TransformDirection(new Vector3(dx,0,dz))).eulerAngles.y;
    void InitializeOpeningScene() {
        if(CurrentScene!="Town"||!PoliceStation.Current)return;
        if(state.openingStage==0&&state.openingEscort==0)player.Place(OpeningPoint(19.2f,20.6f),OpeningYaw(-1,0));
        RestoreOpeningEscort();
        RestoreOpeningRideScene();
        // Door state is restored by ShopMechanism, including a player closing it later.
        location="青川 · "+(state.openingStage==0&&state.openingEscort==0?"羁押室":state.openingStage==0?"警署":state.openingStage<4?"律师来访":"警署");
    }
    void TickOpening() {
        if(state.openingStage==0&&state.openingEscort==0&&!openingNoticePlayed&&!Blocked&&CurrentScene=="Town"&&PoliceStation.Current) {
            openingNoticePlayed=true;StartCoroutine(CellCinematic());
        }
    }
    OpeningActor FindOpeningActor(string role) {
        foreach(var actor in FindObjectsByType<OpeningActor>(FindObjectsSortMode.None))if(actor.role==role)return actor;
        return null;
    }
    public void CallOpeningGuard(){var guard=FindOpeningActor("guard");if(guard)OpenOpeningActor(guard);}
    public void OpeningReminder(string line){remark=line;remarkStart=Time.time;remarkUntil=Time.time+5;}
    public void OpenOpeningActor(OpeningActor actor) {
        if(Blocked||OpeningRideActive)return;
        if(actor.role=="lawyer"&&state.openingStage>=4&&state.openingRideStage<3){if(state.openingRideStage==1&&lawyerWalk.Count>0){OpeningReminder("律师：我们到门外再说。");return;}OpenRideDialogue(state.openingRideStage==2?"ready":"offer");return;}
        string node;
        if(actor.role=="guard")node=state.openingStage>=1||state.openingEscort>=4?(state.openingStage>=4?"idle":"directions"):
            state.openingEscort<=1?"follow":state.openingEscort==2?(state.belongingsCollected?"collected":guardHandoverDone?"take":"handover"):"to-lawyer";
        else node=state.openingStage==0?"wait":state.openingStage==1?"greeting":state.openingStage==2?"read":state.openingStage==3?"after":"revisit";
        OpenOpeningNode(actor,node);
    }
    void OpenOpeningNode(OpeningActor actor,string node) {
        openingScript=DialogueScript.Load(actor.role=="guard"?"OpeningGuardDialogue":"OpeningLawyerDialogue");
        milkboyScript=openingScript;BindDialoguePerformance(actor);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        // Once the hand-over has been offered it is not forced on the player again.
        if(node=="handover")guardHandoverDone=true;
        if(node=="collected")collectedSpoken=true;
        VisitDialogue(node);CursorMode();
    }
    bool TryOpeningDialogueAction() {
        if(openingScript==null||milkboyScript!=openingScript)return false;
        string id=dialogueNode.id;
        if(id=="paper") {
            state.openingStage=Mathf.Max(2,state.openingStage);CloseDialogue();
            // Seated at the table, the lawyer pushes the paper across: it opens in the hands.
            var paper=FindFirstObjectByType<OpeningNewspaper>();if(paper&&FurnitureSeated)Inspect(paper);
            return true;
        }
        if(id=="release") {state.openingStage=4;state.openingRideStage=1;CloseDialogue();StartLawyerExit();return true;}
        return false;
    }
    void EnsureOpeningDoor() {
        var door=PoliceStation.Current.Door("police-holding-door");
        if(door&&!door.IsOpen&&!door.Moving)door.Use();
    }
    void OpeningInspectionClosed() {
        if(inspected is OpeningNewspaper&&evidenceDetails&&state.openingStage==2) {
            state.openingStage=3;
            // Still at the table: the conversation simply continues.
            if(FurnitureSeated){var lawyer=FindOpeningActor("lawyer");if(lawyer)pendingLawyerAfter=true;}
        }
    }
    bool pendingLawyerAfter;

    // ---- Escort: cell -> property room -> briefcase -> meeting room ----
    bool guardHandoverDone,collectedSpoken;
    readonly List<Vector3> guardRoute=new List<Vector3>();
    readonly Dictionary<int,string> guardRouteDoors=new Dictionary<int,string>();
    int guardRouteIndex;
    float guardWaitRemind;
    enum GuardTask { None, Route, Fetch, Await, Deliver }
    GuardTask guardTask;
    float guardTaskTime;
    // The guard's "these are yours" comes a moment after the case is taken, so the hand-over banner is seen.
    float collectedTalkAt=-1;
    OpeningBriefcase briefcase;OpeningCage propertyCage;
    // Building-local. He opens the cage standing left of its door, reaches in from in front of the
    // opening and sets the case down standing on the table from its far side, across from the player.
    // The guard's arms are short (0.39 m shoulder to wrist): he stands close to what he handles.
    static readonly Vector3 PropertyTableTop=new Vector3(20.42f,.925f,17.88f),GuardCageSpot=new Vector3(20.15f,.15f,18.42f),GuardTableSpot=new Vector3(20.58f,.15f,18.14f);
    const float DoorHandleHeight=1.02f;
    // In the hall he waits south of the meeting-room door, out of the way of anyone walking to it.
    static readonly Vector3 PlayerTableSpot=new Vector3(20.4f,.15f,16.75f),GuardHallSpot=new Vector3(12.4f,.15f,3.7f);
    public bool OpeningEscortActive=>state.openingStage==0&&state.openingEscort>=1&&state.openingEscort<=3;
    void SetGuardRoute(params Vector3[] local) {
        guardRoute.Clear();guardRouteDoors.Clear();guardRouteIndex=0;guardDoorStep=DoorStep.Approach;
        foreach(var p in local)guardRoute.Add(p);
    }
    void BeginEscortToProperty() {
        state.openingEscort=1;guardTask=GuardTask.Route;
        // From where he waited by the cell straight down the cloister to the property-room door.
        SetGuardRoute(new Vector3(16.95f,0,17.0f),new Vector3(19.4f,0,18.35f),GuardCageSpot);
        guardRouteDoors[0]="police-evidence-door";
        Save();
    }
    void BeginEscortToLawyer() {
        state.openingEscort=3;guardTask=GuardTask.Route;collectedTalkAt=-1;
        SetGuardRoute(new Vector3(19.4f,0,18.35f),new Vector3(18.3f,0,17.0f),new Vector3(16.95f,0,17.0f),new Vector3(15.6f,0,17.6f),
            new Vector3(8.3f,0,13.3f),new Vector3(7.4f,0,12.7f),new Vector3(7.4f,0,10.0f),new Vector3(7.4f,0,8.0f),new Vector3(11.5f,0,8.0f),GuardHallSpot);
        guardRouteDoors[5]="police-court-door";
        Save();
    }
    void RestoreOpeningEscort() {
        var guard=FindOpeningActor("guard");if(!guard)return;
        var station=PoliceStation.Current.transform;
        briefcase=FindFirstObjectByType<OpeningBriefcase>(FindObjectsInactive.Include);
        propertyCage=FindFirstObjectByType<OpeningCage>(FindObjectsInactive.Include);
        var routine=guard.GetComponent<PoliceStaffRoutine>();
        if(routine)routine.enabled=state.openingStage>=1||state.openingEscort>=4;
        guard.GetComponent<NpcHands>()?.Stop();
        // The case stands in the cage until the guard has taken it out, then on the table until it is collected.
        bool outOfCage=state.openingEscort>=2;
        if(propertyCage)propertyCage.Open=outOfCage&&state.openingEscort<4?1:0;
        if(briefcase) {
            briefcase.gameObject.SetActive(!state.belongingsCollected&&state.openingEscort<4);
            if(outOfCage)briefcase.Stand(station.TransformPoint(PropertyTableTop),-station.forward);
            else if(propertyCage)briefcase.Stand(propertyCage.caseHome.position,-station.forward);
            var c=briefcase.GetComponent<Collider>();if(c)c.enabled=outOfCage;
        }
        guardTask=GuardTask.None;collectedTalkAt=-1;
        // A save taken between picking the case up and the guard's next words carries on to the lawyer.
        if(state.openingEscort==2&&state.belongingsCollected)state.openingEscort=3;
        // Before the knock nobody stands at the bars (the title screen looks into the cell).
        if(state.openingStage==0&&state.openingEscort==0){guard.transform.position=OpeningPoint(CineGuardHidden.x,CineGuardHidden.z);FaceGuard(guard,new Vector3(0,0,-1));}
        else if(state.openingEscort==1){guard.transform.position=OpeningPoint(16.95f,20.0f);BeginEscortToProperty();}
        else if(state.openingEscort==2){guard.transform.position=station.TransformPoint(GuardTableSpot);FaceGuard(guard,new Vector3(0,0,-1));guardTask=GuardTask.Await;guardHandoverDone=true;}
        else if(state.openingEscort==3){guard.transform.position=OpeningPoint(16.95f,17.0f);BeginEscortToLawyer();guardRouteIndex=3;}
        // Released and with no round to walk (focused reviews keep everyone still): he waits north of
        // the cell door, clear of the doorway, instead of standing in it.
        else if(state.openingEscort>=4&&!PoliceStaffRoutine.Enabled){guard.transform.position=OpeningPoint(16.95f,21.85f);FaceGuard(guard,new Vector3(.75f,0,-.65f));}
        LayLawyer();
    }
    // Placing someone outright (restoring a save): no turn to watch.
    void FaceGuard(OpeningActor guard,Vector3 localDirection) {
        var d=PoliceStation.Current.transform.TransformDirection(localDirection);d.y=0;
        if(d.sqrMagnitude>.0001f)guard.transform.rotation=Quaternion.LookRotation(-d);
    }
    // Runs in reviews too: the escort is part of the world, not of the desktop input handling.
    void TickOpeningEscort() {
        if(!sceneReady||CurrentScene!="Town"||!PoliceStation.Current||GameTimePaused)return;
        if(pickupBannerLeft>0&&!Blocked)pickupBannerLeft-=Time.deltaTime;
        TickOpeningLawyerSeat();
        if(!OpeningEscortActive)return;
        var guard=FindOpeningActor("guard");if(!guard)return;
        var station=PoliceStation.Current.transform;
        if(IsSpeakingWith(guard)||cellCinematic){if(guardTask!=GuardTask.Fetch&&guardDoorStep!=DoorStep.Open)guard.StopWalking();return;}
        float playerDistance=Vector3.Distance(Flat(player.transform.position),Flat(guard.transform.position));
        // Reaching the meeting room by oneself, belongings in hand, completes the escort.
        if(state.openingEscort==3&&PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="interview"){FinishEscort(guard);return;}
        switch(guardTask) {
            case GuardTask.Route: {
                if(guardRouteIndex>=guardRoute.Count){ArriveGuard(guard);break;}
                // Waits for a player who falls behind, turning round to look for him.
                bool behind=playerDistance>(state.openingEscort==1?4.2f:5.5f);
                bool atDoor=guardRouteDoors.TryGetValue(guardRouteIndex,out var doorId);
                if(behind&&!(atDoor&&guardDoorStep==DoorStep.Open)){guard.StopWalking();guard.TurnToward(player.transform.position-guard.transform.position);
                    guardWaitRemind+=Time.deltaTime;if(guardWaitRemind>9){guardWaitRemind=0;OpeningReminder("狱警：磨蹭什么？跟上。");}break;}
                guardWaitRemind=0;
                if(atDoor){if(TickGuardDoor(guard,doorId))guardRouteIndex++;break;}
                bool last=guardRouteIndex==guardRoute.Count-1;
                var target=station.TransformPoint(new Vector3(guardRoute[guardRouteIndex].x,0,guardRoute[guardRouteIndex].z));target.y=guard.transform.position.y;
                // Never walk into the player standing in the way.
                var ahead=Flat(target)-Flat(guard.transform.position);var rel=Flat(player.transform.position)-Flat(guard.transform.position);
                if(rel.magnitude<.95f&&ahead.sqrMagnitude>.0001f&&Vector3.Dot(rel.normalized,ahead.normalized)>.35f){guard.StopWalking();break;}
                guard.WalkTowards(target,1.25f,true,last);
                // Corners are cut a little, as people do; the end of the walk is reached exactly.
                if(Vector3.Distance(Flat(guard.transform.position),Flat(target))<(last?.03f:.3f))guardRouteIndex++;
                break;
            }
            case GuardTask.Fetch:break;
            case GuardTask.Await: {
                if(playerDistance<6)guard.TurnToward(player.transform.position-guard.transform.position);
                if(!guardHandoverDone&&!state.belongingsCollected&&playerDistance<3.6f&&!Blocked){OpenOpeningNode(guard,"handover");break;}
                if(state.belongingsCollected) {
                    if(!collectedSpoken&&collectedTalkAt>=0&&Time.time>=collectedTalkAt&&!Blocked){collectedTalkAt=-1;OpenOpeningNode(guard,"collected");}
                    // However that line ends (read through or closed), he then leads on to the lawyer.
                    else if(collectedSpoken&&!Blocked)BeginEscortToLawyer();
                }
                break;
            }
            case GuardTask.Deliver:break;
        }
    }
    // ---- Doors opened by hand on the way (user, 2026-10-15: the doors used to open by themselves) ----
    // He stops beside the handle, turns to the door, takes hold, pushes the leaf open following it
    // a little way, lets go and walks through the middle of the doorway.
    enum DoorStep { Approach, Front, Turn, Open, Through }
    DoorStep guardDoorStep;
    Vector3 guardDoorToward,guardDoorStand;bool guardDoorRight;
    // Doors he has opened with his hand on the handle (reviews check the hand, not only the door).
    int guardDoorsByHand;
    bool TickGuardDoor(OpeningActor guard,string doorId) {
        var door=PoliceStation.Current.Door(doorId);
        if(!door){guardDoorStep=DoorStep.Approach;return true;}
        float y=guard.transform.position.y;
        if(guardDoorStep==DoorStep.Approach&&guardDoorToward==Vector3.zero) {
            DoorFrame(door,guard.transform.position,out _,out _,out guardDoorToward);
            // The hand on the side of the free edge, where the handle is; the body in front of the
            // opening, a little in from the jamb, close enough for his short arm.
            var facing=-guardDoorToward;guardDoorRight=Vector3.Dot(door.ClosedAlong,Vector3.Cross(Vector3.up,facing))>0;
            var hands=guard.GetComponent<NpcHands>();var handle=door.EdgePoint(y+DoorHandleHeight,.08f)+guardDoorToward*.055f;
            guardDoorStand=hands?hands.StandFor(guardDoorRight,handle,facing,.2f,.06f):handle+guardDoorToward*.45f-door.ClosedAlong*.25f;guardDoorStand.y=y;
        }
        var toward=guardDoorToward;
        switch(guardDoorStep) {
            case DoorStep.Approach: {
                if(door.IsOpen&&door.OpenFraction>.85f){guardDoorStep=DoorStep.Through;break;}
                // First out in the passage straight in front of the doorway, never brushing the wall beside it.
                var front=guardDoorStand+toward*.55f;front.y=y;
                if(Vector3.Distance(Flat(guard.transform.position),Flat(front))>.05f){guard.WalkTowards(front,1.2f,true,true);break;}
                guardDoorStep=DoorStep.Front;break;
            }
            case DoorStep.Front:
                if(Vector3.Distance(Flat(guard.transform.position),Flat(guardDoorStand))>.02f){guard.WalkTowards(guardDoorStand,.8f,true,true);break;}
                guard.StopWalking();guardDoorStep=DoorStep.Turn;break;
            case DoorStep.Turn:
                if(guard.TurnToward(-toward)){guardDoorStep=DoorStep.Open;StartCoroutine(GuardOpensDoor(guard,door,toward,guardDoorRight));}
                break;
            case DoorStep.Open:break;
            case DoorStep.Through: {
                var beyond=door.DoorwayCentre-toward*.6f;beyond.y=y;
                if(Vector3.Distance(Flat(guard.transform.position),Flat(beyond))>.3f){guard.WalkTowards(beyond,1.15f,true);break;}
                guardDoorStep=DoorStep.Approach;guardDoorToward=Vector3.zero;return true;
            }
        }
        return false;
    }
    IEnumerator GuardOpensDoor(OpeningActor guard,ShopMechanism door,Vector3 toward,bool right) {
        var hands=guard.GetComponent<NpcHands>();var pose=guard.GetComponent<NpcPose>();
        float y=guard.transform.position.y;
        Vector3 Handle()=>door.EdgePoint(y+DoorHandleHeight,.08f)+toward*.055f;
        if(hands) {
            if(pose){pose.hasFocus=true;pose.focus=Handle();}
            // A lever handle: taken from above, the fingers round it, pressed down, then the push.
            hands.SetGrip(right,NpcHands.Grip.Hold);
            for(float t=0;t<.45f;){if(!GameTimePaused)t+=Time.deltaTime;hands.Reach(right,Handle(),.42f);hands.Orient(right,-toward,Vector3.down);yield return null;}
            for(float t=0;t<.18f;){if(!GameTimePaused)t+=Time.deltaTime;hands.Reach(right,Handle()-Vector3.up*.035f*Mathf.Clamp01(t/.18f));hands.Orient(right,-toward,Vector3.down);yield return null;}
            float miss=Vector3.Distance(hands.Palm(right),Handle()-Vector3.up*.035f);
            if(hands.Weight(right)>.9f&&miss<.06f)guardDoorsByHand++;
            ShowMark("door-"+door.fixtureId);
            Debug.Log("GUARD_DOOR "+door.fixtureId+" right="+right+" weight="+hands.Weight(right).ToString("F2")+" miss="+miss.ToString("F3")+" guard="+OpeningLocal(guard.transform.position).ToString("F2")+" handle="+OpeningLocal(Handle()).ToString("F2"));
        }
        for(int attempt=0;attempt<40&&!door.IsOpen;attempt++){door.Use();if(door.IsOpen)break;yield return new WaitForSeconds(.25f);}
        if(hands) {
            for(float t=0;t<1.1f;){
                if(!GameTimePaused)t+=Time.deltaTime;
                var p=door.EdgePoint(y+DoorHandleHeight-.035f,.08f);var from=hands.Shoulder(right);from.y=p.y;p-=(p-from).normalized*.05f;
                if(!hands.Within(right,p,.93f))break;
                hands.Reach(right,p);hands.Orient(right,(p-from).normalized,Vector3.down);if(pose)pose.focus=p;yield return null;
            }
            hands.Release(right,.4f);if(pose)pose.hasFocus=false;
        }
        while(door.OpenFraction<.85f)yield return null;
        guardDoorStep=DoorStep.Through;
    }
    void ArriveGuard(OpeningActor guard) {
        guard.StopWalking();
        if(state.openingEscort==1){StartCoroutine(GuardFetchesBriefcase(guard));}
        else if(state.openingEscort==3){
            guardTask=GuardTask.Deliver;
            if(!Blocked)OpenOpeningNode(guard,"delivered");
            FinishEscort(guard);
        }
    }
    // ---- The briefcase (user, 2026-10-15: it used to slide out of the cage and across to the table by itself) ----
    // He turns to the cage, unlocks its door and pulls it open, reaches in for the case by its handle,
    // lifts it off the boxes and draws it out, turns round with it hanging at his side, steps up to
    // the table and sets it down standing, its flap toward the player; then he lets go.
    bool carryingCase;Quaternion caseFrom,caseTo;float caseBlend=-1;
    IEnumerator GuardFetchesBriefcase(OpeningActor guard) {
        guardTask=GuardTask.Fetch;guardTaskTime=0;
        var station=PoliceStation.Current.transform;
        var hands=guard.GetComponent<NpcHands>();var pose=guard.GetComponent<NpcPose>();var cage=propertyCage;
        Vector3 intoCage=station.forward,toPlayer=-station.forward;intoCage.y=0;toPlayer.y=0;
        var tableTop=station.TransformPoint(PropertyTableTop);float floor=guard.transform.position.y;
        // Seconds of unpaused time, for each step of the work.
        IEnumerator Step(float seconds,Action<float> act){for(float t=0;;){act(Mathf.Clamp01(t/seconds));if(t>=seconds)yield break;yield return null;if(!GameTimePaused){t+=Time.deltaTime;guardTaskTime+=Time.deltaTime;}}}
        IEnumerator Turn(Vector3 to,Action each=null){for(float t=0;t<3;){each?.Invoke();if(guard.TurnToward(to))yield break;yield return null;if(!GameTimePaused){t+=Time.deltaTime;guardTaskTime+=Time.deltaTime;}}}
        IEnumerator Walk(Vector3 to,Action each=null){to.y=floor;for(float t=0;t<6&&Vector3.Distance(Flat(guard.transform.position),Flat(to))>.02f;){each?.Invoke();guard.WalkTowards(to,.9f,true,true);yield return null;if(!GameTimePaused){t+=Time.deltaTime;guardTaskTime+=Time.deltaTime;}}guard.StopWalking();}
        IEnumerator Back(Vector3 to,Action each=null){to.y=floor;for(float t=0;t<3&&!guard.Shuffle(to);){each?.Invoke();yield return null;if(!GameTimePaused){t+=Time.deltaTime;guardTaskTime+=Time.deltaTime;}}guard.StopWalking();}
        if(!hands||!cage||!briefcase) {
            // Without hands or cage (a stand-in cast): the case is simply set out on the table.
            yield return Step(1.5f,k=>{});
            if(briefcase){briefcase.Stand(tableTop,toPlayer);var c=briefcase.GetComponent<Collider>();if(c)c.enabled=true;}
            state.openingEscort=2;guardTask=GuardTask.Await;Save();yield break;
        }
        const bool right=true;
        void Look(Vector3 at){if(pose){pose.hasFocus=true;pose.focus=at;}}
        void Done(){hands.Stop();hands.AfterSolve=null;carryingCase=false;caseBlend=-1;if(pose)pose.hasFocus=false;}
        bool Collected()=>state.belongingsCollected||state.openingEscort!=1;
        var back=guard.transform.position;
        yield return Turn(intoCage);
        // Up to the padlock with a short step, keeping his face to the cage (user, 2026-10-15: no
        // wide loops for a half-metre move): the key between thumb and finger, a turn of the wrist.
        var lockAt=cage.Lock-intoCage*.045f;
        yield return Back(hands.StandFor(right,lockAt,intoCage,.15f));
        Look(lockAt);
        Vector3 keyFingers=intoCage,keyPalm=-hands.Side;
        hands.SetGrip(right,NpcHands.Grip.Pinch);
        yield return Step(.5f,k=>{hands.Reach(right,lockAt,.45f);hands.Orient(right,keyFingers,keyPalm);});
        cage.Play(cage.unlockClip);ShowMark("cage");
        yield return Step(.5f,k=>{float turn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.6f,k))*70;hands.Reach(right,lockAt);hands.Orient(right,keyFingers,Quaternion.AngleAxis(turn,keyFingers)*keyPalm);});
        // The door, taken by its edge and drawn open as he steps back out of its way; then he lets
        // go when it is beyond his reach and it swings on to stand open.
        hands.SetGrip(right,NpcHands.Grip.Hold);cage.Play(cage.openClip);
        float grip=floor+1.25f;Vector3 Edge()=>cage.Edge(grip)-intoCage*.025f;
        yield return Back(back,()=>{cage.Open=Mathf.MoveTowards(cage.Open,.14f,Time.deltaTime*.3f);hands.Reach(right,Edge(),.3f);hands.Orient(right,intoCage,hands.Side);Look(Edge());});
        yield return Step(.8f,k=>{cage.Open=Mathf.Lerp(cage.Open,.5f,k);if(hands.Within(right,Edge(),.95f)){hands.Reach(right,Edge());hands.Orient(right,intoCage,hands.Side);}else hands.Release(right,.35f);Look(Edge());});
        hands.Release(right,.35f);
        yield return Step(.45f,k=>{cage.Open=Mathf.Lerp(.5f,1,Mathf.SmoothStep(0,1,k));});
        if(Collected()){Done();yield break;}
        // Across to the opening with a side-step, then in for the handle: taken from above, the palm
        // down and the fingers closing round it.
        var handle=briefcase.HandlePoint;
        var reach=hands.StandFor(right,handle,intoCage,.37f);
        // His body stays out of the cage, the front of it short of the wire.
        float inside=Vector3.Dot(reach-cage.transform.position,intoCage)+.17f;if(inside>0)reach-=intoCage*inside;
        yield return Back(reach);yield return Turn(intoCage);
        Look(handle);hands.SetGrip(right,NpcHands.Grip.Hold);
        Vector3 over=(intoCage*.94f+Vector3.down*.34f).normalized;
        yield return Step(.7f,k=>{hands.Reach(right,handle+Vector3.up*.01f,.6f);hands.Orient(right,over,Vector3.down);});
        yield return Step(.15f,k=>{hands.Reach(right,handle+Vector3.up*.01f);hands.Orient(right,over,Vector3.down);});
        // Lifted off the boxes and drawn straight out through the door, square to it; only once clear
        // of the wire does it turn to hang at his side.
        caseFrom=briefcase.transform.rotation;caseTo=Quaternion.LookRotation(hands.Side,Vector3.up);caseBlend=0;carryingCase=true;
        hands.AfterSolve=()=>{if(!carryingCase||!briefcase)return;var palm=hands.Palm(right);if(caseBlend>=0&&caseBlend<1)briefcase.Blend(palm,caseFrom,caseTo,caseBlend);else briefcase.Hang(palm,hands.Side,GameTimePaused?0:Time.deltaTime);};
        var lifted=handle+Vector3.up*.06f;ShowMark("carry");
        yield return Step(.3f,k=>{hands.Reach(right,Vector3.Lerp(handle,lifted,Mathf.SmoothStep(0,1,k)));hands.Orient(right,over,Vector3.down);});
        var outside=lifted-intoCage*.42f;
        yield return Step(.45f,k=>{hands.Reach(right,Vector3.Lerp(lifted,outside,Mathf.SmoothStep(0,1,k)));hands.Orient(right,over,Vector3.down);Look(briefcase.transform.position);});
        // Hanging at his side, held a little out from the leg so it swings clear of it.
        Vector3 Carry()=>guard.transform.position+hands.Side*.3f+Vector3.up*1.0f+hands.Facing*.04f;
        Vector3 Hang()=>(hands.Facing*.25f+Vector3.down).normalized;
        yield return Step(.6f,k=>{float e=Mathf.SmoothStep(0,1,k);caseBlend=e;hands.Reach(right,Vector3.Lerp(outside,Carry(),e));hands.Orient(right,Vector3.Slerp(over,Hang(),e),Vector3.Slerp(Vector3.down,-hands.Side,e));});
        caseBlend=1;if(pose)pose.hasFocus=false;
        // Round to the table with it, and up to its edge.
        void Carrying(){hands.Reach(right,Carry());hands.Orient(right,Hang(),-hands.Side);}
        yield return Turn(toPlayer,Carrying);
        OpeningBriefcase.StandingPose(tableTop,toPlayer,out var restPosition,out var restRotation);
        var restHandle=restPosition+restRotation*new Vector3(0,.2f,0);
        var atTable=hands.StandFor(right,restHandle,toPlayer,.3f);
        // Never into the table: at least a body's half-width from its edge.
        float overTable=Vector3.Dot(station.TransformPoint(GuardTableSpot)-atTable,toPlayer);if(overTable<0)atTable+=toPlayer*overTable;
        yield return Walk(atTable,Carrying);yield return Turn(toPlayer,Carrying);
        // Onto the table: raised at his side until its base clears the table top, carried in over the
        // edge (turning to face the player on the way), then set down on its base. It never passes
        // through the edge of the table.
        float clear=tableTop.y+.35f+.07f;
        var raised=guard.transform.position+hands.Side*.32f+hands.Facing*.06f;raised.y=clear;
        var above=restHandle;above.y=Mathf.Max(clear,restHandle.y+.06f);
        var carried=hands.Palm(right);Look(restPosition);ShowMark("place");
        Vector3 hold=(hands.Facing*.6f+Vector3.down*.8f).normalized;
        yield return Step(.5f,k=>{float e=Mathf.SmoothStep(0,1,k);hands.Reach(right,Vector3.Lerp(carried,raised,e));hands.Orient(right,Vector3.Slerp(Hang(),hold,e),Vector3.Slerp(-hands.Side,Vector3.down,e));});
        caseFrom=briefcase.transform.rotation;caseTo=restRotation;caseBlend=0;
        yield return Step(.5f,k=>{float e=Mathf.SmoothStep(0,1,k);caseBlend=e;hands.Reach(right,Vector3.Lerp(raised,above,e));hands.Orient(right,hold,Vector3.down);});
        yield return Step(.3f,k=>{caseBlend=1;hands.Reach(right,Vector3.Lerp(above,restHandle,Mathf.SmoothStep(0,1,k)));hands.Orient(right,hold,Vector3.down);});
        carryingCase=false;caseBlend=-1;hands.AfterSolve=null;
        briefcase.Stand(tableTop,toPlayer);var col=briefcase.GetComponent<Collider>();if(col)col.enabled=true;
        hands.SetGrip(right,NpcHands.Grip.Open);hands.Release(right,.45f);
        yield return Step(.45f,k=>{});
        if(pose)pose.hasFocus=false;
        if(state.openingEscort==1){state.openingEscort=2;guardTask=GuardTask.Await;Save();}
    }
    // Turning on the spot where a person stays put (the lawyer settling onto his chair).
    void RotateGuard(OpeningActor guard,Vector3 localDirection) {
        var d=PoliceStation.Current.transform.TransformDirection(localDirection);d.y=0;
        guard.transform.rotation=Quaternion.RotateTowards(guard.transform.rotation,Quaternion.LookRotation(-d),Time.deltaTime*200);
    }
    void FinishEscort(OpeningActor guard) {
        state.openingEscort=4;state.openingStage=Mathf.Max(1,state.openingStage);guardTask=GuardTask.None;
        // He goes back to his own round (PoliceStaffRoutine waits until any conversation with him ends).
        var routine=guard.GetComponent<PoliceStaffRoutine>();if(routine)routine.enabled=true;
        Save();
    }
    public void CollectBelongings(OpeningBriefcase item) {
        if(state.belongingsCollected||Blocked)return;
        state.belongingsCollected=true;item.gameObject.SetActive(false);pickupBannerLeft=4.5f;
        GetComponent<TownSound>()?.PlayHeldItem(false,true);
        // The guard expects the briefcase now: an early grab still counts as the hand-over.
        if(state.openingEscort<2){state.openingEscort=2;guardTask=GuardTask.Await;}
        guardHandoverDone=true;collectedSpoken=false;collectedTalkAt=Time.time+1.6f;Save();
        Debug.Log("OPENING_BELONGINGS_COLLECTED");
    }
    float pickupBannerLeft;
    void DrawPickupBanner() {
        if(pickupBannerLeft<=0||Blocked)return;
        float a=Mathf.Clamp01(pickupBannerLeft/.8f)*Mathf.Clamp01((4.5f-pickupBannerLeft)/.3f+.2f);
        var r=new Rect(W/2-330,H*.2f,660,104);Box(r,new Color(.03f,.05f,.07f,.86f*a));
        GUI.color=new Color(1,1,1,a);
        Label(new Rect(r.x+24,r.y+14,r.width-48,36),"取回私人物品 · 公文包",text);
        Label(new Rect(r.x+24,r.y+56,r.width-48,32),"怀表 Q    纸地图 M    车票 T    公文包 B    档案 Tab",small);
        GUI.color=Color.white;
    }

    // ---- The lawyer sits down when the player comes in; E on the chair opposite starts the talk ----
    // He greets the player, walks round to the side of his chair (never through its back), turns to the
    // table and lowers himself sideways onto the seat; getting up is the same movement in reverse.
    NpcPose lawyerPose;
    static readonly Vector3 LawyerChairSeat=new Vector3(16.5f,.15f,2.12f),LawyerChairSide=new Vector3(17.08f,.15f,2.12f),LawyerBehindChairs=new Vector3(17.08f,.15f,1.3f);
    enum LawyerSeat { Standing, Greeting, Walking, Sitting, Seated }
    LawyerSeat lawyerSeat;
    float lawyerSitBlend,lawyerSeatClock;
    bool lawyerSitting=>lawyerSeat==LawyerSeat.Sitting||lawyerSeat==LawyerSeat.Seated;
    void LayLawyer() {
        var lawyer=FindOpeningActor("lawyer");if(!lawyer)return;
        lawyerPose=lawyer.GetComponent<NpcPose>();lawyerSeat=LawyerSeat.Standing;lawyerSitBlend=0;
        // Saved in the meeting: he is already seated.
        if(state.openingStage>=1&&state.openingStage<4&&lawyerPose) {
            lawyer.transform.position=PoliceStation.Current.transform.TransformPoint(LawyerChairSeat);FaceGuard(lawyer,new Vector3(0,0,1));
            lawyerPose.seatHeight=LawyerSeatHeight;lawyerPose.activity=NpcPose.Activity.Sit;lawyerSeat=LawyerSeat.Seated;lawyerSitBlend=1;
            var voice=lawyer.GetComponentInChildren<PeriodCharacterPerformance>();if(voice)voice.gestures=false;
        }
    }
    float LawyerSeatHeight=>OpeningPoint(0,0).y+.46f-.025f;
    void TickOpeningLawyerSeat() {
        if(state.openingStage<1||state.openingStage>=4||!lawyerPose)return;
        var lawyer=lawyerPose.GetComponent<OpeningActor>();var station=PoliceStation.Current.transform;
        switch(lawyerSeat) {
            case LawyerSeat.Standing: {
                bool inRoom=PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="interview";
                if(!inRoom&&Vector3.Distance(player.transform.position,lawyer.transform.position)>3.5f)break;
                lawyerSeat=LawyerSeat.Greeting;lawyerSeatClock=0;
                // The first thing he says is about how the man looks, not the case (user, 2026-10-15).
                const string greeting="你出来了……脸色这么差。先坐下歇歇吧。";
                OpeningReminder("律师："+greeting);lawyer.GetComponentInChildren<PeriodCharacterPerformance>()?.Speak(greeting);
                break;
            }
            case LawyerSeat.Greeting: {
                // He looks up at the player for a moment before going round to his chair.
                lawyerSeatClock+=Time.deltaTime;
                lawyer.TurnToward(player.transform.position-lawyer.transform.position);
                if(lawyerSeatClock>1.6f){lawyerSeat=LawyerSeat.Walking;lawyerSeatClock=0;}
                break;
            }
            case LawyerSeat.Walking: {
                var behind=station.TransformPoint(LawyerBehindChairs);var side=station.TransformPoint(LawyerChairSide);
                // Behind the chairs first, then up to the side of his own.
                var local=OpeningLocal(lawyer.transform.position);
                var target=local.z<1.9f&&Vector3.Distance(Flat(lawyer.transform.position),Flat(behind))>.04f&&local.x<LawyerChairSide.x-.04f?behind:side;
                if(Vector3.Distance(Flat(lawyer.transform.position),Flat(side))>.03f){
                    // He waits rather than walk into the player standing in his way.
                    var ahead=Flat(target)-Flat(lawyer.transform.position);var rel=Flat(player.transform.position)-Flat(lawyer.transform.position);
                    if(rel.magnitude<.9f&&ahead.sqrMagnitude>.0001f&&Vector3.Dot(rel.normalized,ahead.normalized)>.35f){lawyer.StopWalking();break;}
                    lawyer.WalkTowards(target,1f,false,target==side);break;
                }
                // At the side of his chair he turns to the table, stepping round, before he sits.
                lawyerSeatClock+=Time.deltaTime;
                bool facing=lawyer.TurnToward(station.TransformDirection(new Vector3(0,0,1)));
                if(facing&&lawyerSeatClock>.35f){lawyer.StopWalking();lawyerSeat=LawyerSeat.Sitting;lawyerSitBlend=0;lawyerPose.seatHeight=LawyerSeatHeight;}
                break;
            }
            case LawyerSeat.Sitting: {
                // Seated he talks with his face and voice, his hands at rest (user, 2026-10-15: they kept
                // flying up in front of him); standing again, a short gesture opens each line.
                var voice=lawyer.GetComponentInChildren<PeriodCharacterPerformance>();if(voice)voice.gestures=false;
                lawyerSitBlend=Mathf.Min(1,lawyerSitBlend+Time.deltaTime/.95f);
                RotateGuard(lawyer,new Vector3(0,0,1));
                if(lawyerSitBlend>.2f)lawyerPose.activity=NpcPose.Activity.Sit;
                lawyer.transform.position=Vector3.Lerp(station.TransformPoint(LawyerChairSide),station.TransformPoint(LawyerChairSeat),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.85f,lawyerSitBlend)));
                if(lawyerSitBlend>=1)lawyerSeat=LawyerSeat.Seated;
                break;
            }
        }
        // Sitting down across from him begins (or picks up) the meeting.
        bool seated=FurnitureSeated&&restingSeat&&restingSeat.seatId=="police-seat-int";
        if(seated&&!Blocked&&lawyerSeat==LawyerSeat.Seated) {
            if(!openedFromSeat) {
                openedFromSeat=true;
                if(state.openingStage==1)OpenOpeningNode(lawyer,"greeting");
                else if(state.openingStage==2){var paper=FindFirstObjectByType<OpeningNewspaper>();if(paper)Inspect(paper);}
                else if(state.openingStage==3){pendingLawyerAfter=false;OpenOpeningNode(lawyer,"after");}
            } else if(pendingLawyerAfter&&state.openingStage==3){pendingLawyerAfter=false;OpenOpeningNode(lawyer,"after");}
        }
        if(!seated)openedFromSeat=false;
    }
    bool openedFromSeat;
    // The lawyer gets up again before he leaves the room: up and out sideways, back to the side of the chair.
    System.Collections.IEnumerator LawyerStandUp() {
        if(!lawyerPose||!lawyerSitting)yield break;
        var lawyer=lawyerPose.GetComponent<OpeningActor>();var station=PoliceStation.Current.transform;
        lawyerPose.activity=NpcPose.Activity.Stand;lawyerSeat=LawyerSeat.Sitting;
        var voice=lawyer.GetComponentInChildren<PeriodCharacterPerformance>();
        for(float t=0;t<1;){
            if(!GameTimePaused){t=Mathf.Min(1,t+Time.deltaTime/.9f);lawyer.transform.position=Vector3.Lerp(station.TransformPoint(LawyerChairSeat),station.TransformPoint(LawyerChairSide),Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,t)));}
            yield return null;
        }
        lawyerSeat=LawyerSeat.Standing;if(voice)voice.gestures=true;
    }
    void DrawOpeningObjective() {
        if(opening||creatorMenu||OpeningRideActive||cellCinematic)return;
        DrawPickupBanner();
        if(state.openingStage>=4){if(state.openingRideStage==1||state.openingRideStage==2)Label(new Rect(36,102,720,65),state.openingRideStage==1?"可选 · 律师在警署门外等你，靠近按 E 交谈。":"去看孩子 · 车停在警署东侧中街，靠近右侧前门按 E 上车。",small);else DrawHospitalObjective();return;}
        string goal;
        if(state.openingStage==0)goal=state.openingEscort<=1?"跟着狱警去保管室，领回你的私人物品。":
            state.openingEscort==2&&!state.belongingsCollected?"拿起保管室桌上的公文包：靠近按 E。":"跟着狱警去会见律师。";
        else {
            string[] goals={"","会见律师：在门厅东侧的会见室。对着桌前的椅子按 E 坐下。","查看会见桌上的报纸：E 拿起，再按 E 阅读正文。","放回报纸，和律师继续谈话。"};
            goal=goals[Mathf.Clamp(state.openingStage,1,3)];
            // Already sitting across from him: the hint about the chair has done its work.
            if(state.openingStage==1&&FurnitureSeated&&restingSeat&&restingSeat.seatId=="police-seat-int")goal="和律师谈谈。";
        }
        Label(new Rect(36,102,720,65),goal,small);
    }
}
