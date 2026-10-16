using System;
using System.Collections;
using System.IO;
using UnityEngine;

// Focused native checkpoint review. Dialogue choices are programmatic, while
// interaction uses the normal first-hit E ray and walking uses the real motor.
public partial class DemoApp {
    float openingRideAuditAt;
    int openingRideAuditSamples;
    void ORNeed(bool ok,string why) {
        if(!ok)throw new Exception("OPENING_RIDE_REVIEW_FAIL: "+why+" stage="+state.openingRideStage+" player="+player.transform.position);
        Debug.Log("OPENING_RIDE_CHECK: "+why);
    }
    IEnumerator ORUntil(Func<bool> done,float timeout,string why) {
        float deadline=Time.unscaledTime+timeout;
        while(!done()&&Time.unscaledTime<deadline){ORAuditRoute();yield return null;}
        ORAuditRoute();
        ORNeed(done(),why);
    }
    static string ORPath(Transform item) {
        string path=item.name;
        while(item.parent){item=item.parent;path=item.name+"/"+path;}
        return path;
    }
    static float ORRoadClearance(Vector3 point,out string closestRoad) {
        float best=float.NegativeInfinity;closestRoad="none";
        foreach(var road in TownPlan.Current.roads) {
            // Pedestrian lanes, strips and footways are deliberately excluded.
            if(road.lane<=0)continue;
            for(int i=2;i<road.points.Length;i+=2) {
                var a=new Vector2(road.points[i-2],road.points[i-1]);var b=new Vector2(road.points[i],road.points[i+1]);
                var p=new Vector2(point.x,point.z);var ab=b-a;
                float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(ab.sqrMagnitude,.0001f));
                float clearance=road.lane*.5f-Vector2.Distance(p,a+ab*t);
                if(clearance>best){best=clearance;closestRoad=road.id;}
            }
        }
        return best;
    }
    void ORAuditRoute() {
        if(!OpeningRideActive||!rideCar||rideCar.Speed<=.1f||GameTimePaused||Time.unscaledTime<openingRideAuditAt)return;
        openingRideAuditAt=Time.unscaledTime+.2f;openingRideAuditSamples++;
        // Sample the conservative body footprint, including the rotated corners,
        // edges and interior. At junctions every point may belong to either lane.
        for(int x=0;x<=6;x++)for(int z=0;z<=14;z++) {
            var local=new Vector3(Mathf.Lerp(-.95f,.95f,x/6f),0,Mathf.Lerp(-2.33f,1.98f,z/14f));
            var point=rideCar.transform.TransformPoint(local);
            float clearance=ORRoadClearance(point,out var road);
            if(clearance<-.03f)ORNeed(false,"vehicle footprint leaves road lane: world="+point.ToString("F3")+" local="+local.ToString("F3")+" nearest="+road+" clearance="+clearance.ToString("F3")+" car="+rideCar.transform.position.ToString("F3")+" yaw="+rideCar.transform.eulerAngles.y.ToString("F2"));
        }
        Physics.SyncTransforms();
        var centre=rideCar.transform.TransformPoint(new Vector3(0,1.05f,-.175f));
        foreach(var hit in Physics.OverlapBox(centre,new Vector3(.95f,.65f,2.155f),rideCar.transform.rotation,~0,QueryTriggerInteraction.Ignore)) {
            if(hit.transform.IsChildOf(rideCar.transform)||hit.transform.IsChildOf(player.transform))continue;
            if(hit.bounds.max.y<=rideCar.transform.position.y+.32f)continue;
            ORNeed(false,"vehicle body intersects collider: "+ORPath(hit.transform)+" bounds="+hit.bounds+" car="+rideCar.transform.position.ToString("F3")+" yaw="+rideCar.transform.eulerAngles.y.ToString("F2"));
        }
    }
    // Heading rate of the moving story car, smoothed over a quarter second (frame jitter is not a jolt).
    float turnRate,maxTurnRate,maxTurnAccel,maxRoll,maxPitch;int turnSamples;
    IEnumerator ORSampleTurning() {
        float lastYaw=rideCar?rideCar.transform.eulerAngles.y:0,clock=0,windowTurn=0,lastRate=0;bool primed=false;
        while(rideCar&&state.openingRideStage<4) {
            if(!GameTimePaused&&rideCar.Speed>.3f) {
                float dt=Time.deltaTime,yaw=rideCar.transform.eulerAngles.y;
                windowTurn+=Mathf.DeltaAngle(lastYaw,yaw);clock+=dt;
                if(clock>=.25f){
                    turnRate=windowTurn/clock;turnSamples++;
                    if(primed)maxTurnAccel=Mathf.Max(maxTurnAccel,Mathf.Abs(turnRate-lastRate)/clock);
                    maxTurnRate=Mathf.Max(maxTurnRate,Mathf.Abs(turnRate));lastRate=turnRate;primed=true;windowTurn=0;clock=0;
                }
                maxRoll=Mathf.Max(maxRoll,Mathf.Abs(rideCar.BodyRoll));maxPitch=Mathf.Max(maxPitch,Mathf.Abs(rideCar.BodyPitch));
            }
            if(rideCar)lastYaw=rideCar.transform.eulerAngles.y;
            yield return null;
        }
    }
    // Called by the original prison/newspaper review immediately after release.
    // This observes the real lawyer route started by dialogue; it does not place
    // the actor outside or invoke the departure routine itself.
    IEnumerator ReviewOpeningLawyerExit() {
        ORNeed(state.openingStage==4&&state.openingRideStage==1,"release starts lawyer exit");
        var actor=FindOpeningActor("lawyer");ORNeed(actor,"released lawyer exists");
        int queued=lawyerWalk.Count;ORNeed(queued>0,"release queued the actual indoor-to-outside walk");
        OpenOpeningActor(actor);
        ORNeed(state.openingRideStage==1&&!DialogueOpen&&lawyerWalk.Count==queued,"speaking before lawyer exits cannot replace the indoor route with car boarding");
        // He gets up from the meeting table before the walk; the chair and table are no obstruction then.
        float standing=Time.unscaledTime+5;while(lawyerSitting&&Time.unscaledTime<standing)yield return null;
        ORNeed(!lawyerSitting,"lawyer stands up before leaving");
        float deadline=Time.unscaledTime+35,next=0;int samples=0;
        while(Vector3.Distance(actor.transform.position,PoliceRideWaiting)>.1f&&Time.unscaledTime<deadline) {
            if(!GameTimePaused&&Time.unscaledTime>=next) {
                next=Time.unscaledTime+.15f;samples++;Physics.SyncTransforms();var feet=actor.transform.position;
                foreach(var hit in Physics.OverlapCapsule(feet+Vector3.up*.35f,feet+Vector3.up*1.45f,.22f,~0,QueryTriggerInteraction.Ignore)) {
                    if(hit.transform.IsChildOf(actor.transform)||hit.transform.IsChildOf(player.transform)||hit.bounds.max.y<=feet.y+.12f)continue;
                    ORNeed(false,"lawyer walk intersects collider: "+ORPath(hit.transform)+" feet="+feet.ToString("F3")+" bounds="+hit.bounds);
                }
            }
            yield return null;
        }
        ORNeed(Vector3.Distance(actor.transform.position,PoliceRideWaiting)<.1f,"lawyer walks from existing meeting room through police front door to outside invitation");
        ORNeed(samples>10,"lawyer exit observed over multiple native movement samples");
        Debug.Log("OPENING_LAWYER_EXIT_PASS: capsuleSamples="+samples);
    }
    IEnumerator ORUse(Interactable item) {
        ORNeed(item,"interaction target exists");
        var collider=item.GetComponent<Collider>();
        var point=item is OpeningActor?item.transform.position+Vector3.up*1.3f:collider?collider.bounds.center:item.transform.position;
        player.Aim(point);yield return null;Physics.SyncTransforms();
        ORNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f),"normal E ray hits "+item.title);
        ORNeed(hit.collider.GetComponentInParent<Interactable>()==item,"first E target is "+item.title+", hit="+hit.collider.name);
        ORNeed(TryInteract(),"normal E handler uses "+item.title);yield return null;
    }
    void ORChoose(string target) {
        for(int i=0;i<8&&DialogueOpen&&dialogueChoices.Count==0;i++)AdvanceDialogue();
        int index=dialogueChoices.FindIndex(c=>c.target==target);
        ORNeed(index>=0,"lawyer offers choice "+target);SelectDialogue(index);
        for(int i=0;i<8&&DialogueOpen&&dialogueChoices.Count==0;i++)AdvanceDialogue();
    }
    IEnumerator ReviewOpeningRide(bool interrupt=false) {
        yield return new WaitForSeconds(1);
        opening=false;paused=false;dayNightReviewFrozen=false;
        ORNeed(AudioListener.volume==0,"automated verification is silent");
        // The prison/newspaper flow has its own review. Start this test at its
        // saved release checkpoint; no claim of walking the prior act here.
        state.openingRevision=1;state.openingStage=4;state.openingRideRevision=1;state.openingRideStage=1;
        InitializeOpeningScene();EnsureOpeningRideScene();
        ORNeed(rideCar,"story car exists beside existing police building");
        int entries=state.entries.Count;
        var hotelCar=HotelCar.Current;var hotelPosition=hotelCar?hotelCar.transform.position:Vector3.zero;
        player.Place(PoliceRideWaiting+Vector3.back*1.8f,0);yield return null;
        var lawyer=FindOpeningActor("lawyer");yield return ORUse(lawyer);
        ORNeed(DialogueOpen&&MovementLocked,"outside lawyer invitation uses shared dialogue");
        yield return Capture("opening-ride-01-invitation");
        ORChoose("later");
        ORNeed(state.openingRideStage==1&&!Blocked&&!OpeningRideActive,"deferring leaves free walking available");
        var before=player.transform.position;player.scriptedDirection=Vector3.back;
        yield return new WaitForSeconds(.35f);player.scriptedDirection=Vector3.zero;
        ORNeed(Vector3.Distance(before,player.transform.position)>.25f,"player can leave after deferring");
        yield return WalkTo(PoliceRideWaiting+Vector3.back*1.8f);
        yield return ORUse(lawyer);ORChoose("accept");
        ORNeed(state.openingRideStage==2&&!OpeningRideActive&&!Blocked,"accepting requests boarding without starting the car immediately");
        // Walk around the police front corner, never through its south wall.
        yield return WalkTo(new Vector3(26.5f,.12f,25.4f));
        yield return WalkTo(rideCar.PassengerStandingPoint);
        var boarding=FindFirstObjectByType<OpeningRideBoarding>();
        var passengerPosition=player.transform.position;
        // Wrong-side staging checks only interaction rejection. The approach
        // above and the journey below retain their real controller movement.
        player.Place(rideCar.transform.TransformPoint(new Vector3(-1.65f,.10f,-.36f)),0);
        yield return ORUse(boarding);
        ORNeed(state.openingRideStage==2&&!OpeningRideActive&&!MovementLocked,"driver-side E refuses boarding through the car body");
        player.Place(passengerPosition,0);yield return ORUse(boarding);
        ORNeed(OpeningRideActive&&MovementLocked,"boarding locks locomotion");
        // Frames through getting in: the turn to the door, the step to the sill, ducking under the
        // roof line, sliding round into the seat, and the glance as the door is pulled shut.
        float boarded=Time.unscaledTime;int frame=0;
        foreach(float at in new[]{.55f,1.25f,1.75f,2.25f,2.75f,3.35f,4.0f}) {
            while(Time.unscaledTime-boarded<at)yield return null;
            yield return Capture("opening-ride-02-boarding-"+(++frame));
        }
        StartCoroutine(ORSampleTurning());
        yield return ORUntil(()=>rideCar&&rideCar.Occupied&&rideCar.Speed>.5f,20,"passenger boards and lawyer starts driving");
        ORNeed(player.transform.IsChildOf(rideCar.PassengerSeat),"player is seated relative to the moving car");
        ORNeed(!player.GetComponent<CharacterController>().enabled,"passenger capsule is disabled while mounted");
        ORNeed(!OpeningRideCameraMoving&&LookAllowed,"passenger can look around during the drive");
        double clock=ElapsedGameMinutes,real=Time.unscaledTimeAsDouble;var carBefore=rideCar.transform.position;
        yield return new WaitForSecondsRealtime(1);
        ORNeed(Vector3.Distance(carBefore,rideCar.transform.position)>.5f,"car moves through the existing town");
        ORNeed(Math.Abs((ElapsedGameMinutes-clock)-(Time.unscaledTimeAsDouble-real)*GameClock.GameMinutesPerRealSecond)<.035,"driving keeps the shared twelve-times clock");
        yield return Capture("opening-ride-03-driving");
        paused=true;CursorMode();yield return null;
        float preference=PlayerPrefs.GetFloat(MasterVolumeKey,-1),originalVolume=masterVolume;
        SetMasterVolume(.7f);ORNeed(AudioListener.volume==0&&PlayerPrefs.GetFloat(MasterVolumeKey,-1)==preference,"review stays muted even if sound control changes, without writing user preference");
        SetMasterVolume(0);yield return Capture("opening-ride-audio-settings");SetMasterVolume(originalVolume);
        var pausedCar=rideCar.transform.position;var pausedEye=player.eye.transform.position;double pausedClock=ElapsedGameMinutes;
        yield return new WaitForSecondsRealtime(.5f);
        ORNeed(Vector3.Distance(pausedCar,rideCar.transform.position)<.001f&&Vector3.Distance(pausedEye,player.eye.transform.position)<.001f,"explicit pause stops car and passenger");
        ORNeed(Math.Abs(ElapsedGameMinutes-pausedClock)<1e-9,"explicit pause stops shared clock");
        paused=false;CursorMode();yield return null;
        if(interrupt) {
            ORNeed(state.openingRideStage==3,"interrupt checkpoint is taken during actual transit");Save();
            var saved=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
            ORNeed(saved.openingRideStage==3,"in-transit stage is stored for recovery");
            ORNeed(Vector3.Distance(new Vector3(saved.x,saved.y,saved.z),OpeningRideSafePoint(3))<.05f,"in-transit save uses safe boarding checkpoint");
            ORNeed(saved.entries.Count==entries,"interrupt save preserves evidence");
            File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"checkpoint\":\"interrupted-during-drive\",\"normalInteractionRays\":true,\"physicalInputs\":false}");
            Debug.Log("OPENING_RIDE_INTERRUPTED_PASS");Application.Quit();yield break;
        }
        yield return ORUntil(()=>rideCar&&rideCar.transform.position.z< -58,35,"passenger reaches middle of town along existing middle-street lane");
        yield return Capture("opening-ride-03b-middle-street");
        ORNeed(rideCar.Driver&&rideCar.Driver.gameObject.activeInHierarchy,"lawyer driver remains visible during journey");
        Transform head=null;foreach(var bone in rideCar.Driver.GetComponentsInChildren<Transform>())if(bone.name=="Head")head=bone;
        ORNeed(head,"driver head is available for passenger viewpoint");
        player.Aim(head.position);yield return Capture("opening-ride-03c-driver");
        player.transform.localRotation=Quaternion.identity;player.pitch=0;player.eye.transform.localRotation=Quaternion.identity;
        // A frame in the first real bend, and the frames of getting out at the hospital.
        yield return ORUntil(()=>Mathf.Abs(turnRate)>18||state.openingRideStage>=4,90,"the car takes a bend");
        if(state.openingRideStage<4)yield return Capture("opening-ride-03d-bend");
        yield return ORUntil(()=>OpeningRideCameraMoving&&rideCar&&rideCar.Speed<.01f||state.openingRideStage>=4,90,"the passenger starts getting out");
        float leaving=Time.unscaledTime;int exitFrame=0;
        foreach(float at in new[]{.5f,1.2f,1.8f,2.5f,3.2f}) {
            while(Time.unscaledTime-leaving<at&&OpeningRideCameraMoving)yield return null;
            if(!OpeningRideCameraMoving)break;
            yield return Capture("opening-ride-04-leaving-"+(++exitFrame));
        }
        yield return ORUntil(()=>state.openingRideStage==4&&!OpeningRideCameraMoving,110,"ride arrives and passenger gets out at the existing hospital");
        ORNeed(openingRideAuditSamples>100,"route footprint and body checked throughout actual drive; samples="+openingRideAuditSamples);
        // Smooth bends: the heading changes without jolts and the body leans a little on its springs.
        Debug.Log("OPENING_RIDE_TURNING: maxTurnRate="+maxTurnRate.ToString("F1")+"deg/s maxTurnAccel="+maxTurnAccel.ToString("F1")+"deg/s2 maxRoll="+maxRoll.ToString("F2")+"deg maxPitch="+maxPitch.ToString("F2")+"deg samples="+turnSamples);
        ORNeed(turnSamples>200&&maxTurnRate>15,"the drive includes real bends");
        ORNeed(maxTurnAccel<90,"the car winds into and out of each bend without a jolt (max "+maxTurnAccel.ToString("F1")+" deg/s2)");
        ORNeed(maxRoll>.3f&&maxRoll<2f,"the body leans a little out of the bends (max "+maxRoll.ToString("F2")+" deg)");
        ORNeed(!OpeningRideActive&&player.GetComponent<CharacterController>().enabled,"hospital dropoff restores walking capsule");
        ORNeed(Vector3.Distance(player.transform.position,HospitalDropoff)<.6f,"dropoff is on the hospital entrance pavement");
        ORNeed(DialogueOpen,"lawyer delivers hospital farewell");
        // He turns from the wheel to look at the player while he speaks, his mouth moving with the words.
        var driverLook=rideCar.driver?rideCar.driver.GetComponent<SeatedLook>():null;var driverVoice=rideCar.driver?rideCar.driver.GetComponentInChildren<PeriodCharacterPerformance>():null;
        yield return ORUntil(()=>driverLook&&driverLook.Weight>.95f,3,"the lawyer turns his head toward the player");
        ORNeed(Mathf.Abs(driverLook.Yaw)>30&&driverLook.Facing(player.eye.transform.position)<28,"he faces the player (yaw "+driverLook.Yaw.ToString("F0")+", off "+driverLook.Facing(player.eye.transform.position).ToString("F0")+" deg)");
        ORNeed(driverVoice&&driverVoice.IsSpeaking&&!driverVoice.gestures,"he says goodbye from the seat, speaking without gestures");
        yield return Capture("opening-ride-04-goodbye");
        CloseDialogue();
        ORNeed(state.openingRideStage==5&&!Blocked&&!MovementLocked,"closing farewell finishes trip without trapping controls");
        var parked=rideCar.transform.position;
        yield return ORUntil(()=>!rideCar||Vector3.Distance(rideCar.transform.position,parked)>12,18,"lawyer drives away visibly after goodbye");
        player.Aim(rideCar?rideCar.transform.position+Vector3.up:parked);yield return Capture("opening-ride-05-departure");
        var hospital=TownPlan.Current.Building("B11");
        yield return WalkTo(TownPlan.Point(hospital.gate,.12f));
        yield return WalkTo(TownPlan.Point(hospital.door,.12f)+Vector3.back*1.2f);
        player.Aim(TownPlan.Point(hospital.door,1.7f));
        ORNeed(state.entries.Count==entries,"trip does not invent or grant investigation evidence");
        ORNeed(!hotelCar||Vector3.Distance(hotelCar.transform.position,hotelPosition)<.7f,"existing drivable hotel car remains in place");
        Save();yield return Capture("opening-ride-06-hospital-door");
        File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"stage\":5,\"checkpoint\":\"post-release\",\"normalInteractionRays\":true,\"realControllerWalking\":true,\"physicalInputs\":false,\"routeAuditSamples\":"+openingRideAuditSamples+"}");
        Debug.Log("OPENING_RIDE_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyOpeningRideSave() {
        yield return new WaitForSeconds(1);opening=false;paused=false;dayNightReviewFrozen=false;
        ORNeed(AudioListener.volume==0,"automated verification is silent");
        ORNeed(state.openingRideRevision==1&&state.openingRideStage==5,"completed ride survives separate-process load");
        ORNeed(!OpeningRideActive&&!OpeningRideCameraMoving&&!Blocked,"completed save restores ordinary movement");
        ORNeed(!rideCar&&!FindOpeningActor("lawyer"),"completed trip does not respawn car or lawyer");
        ORNeed(Vector3.Distance(player.transform.position,TownPlan.Point(TownPlan.Current.Building("B11").door,.12f))<2,"hospital doorway position survives load");
        var before=player.transform.position;player.scriptedDirection=Vector3.back;
        yield return new WaitForSeconds(.35f);player.scriptedDirection=Vector3.zero;
        ORNeed(Vector3.Distance(before,player.transform.position)>.25f,"completed save can walk away");
        yield return Capture("opening-ride-07-reload");Debug.Log("OPENING_RIDE_SAVE_PASS");Application.Quit();
    }
    IEnumerator VerifyOpeningRideInterrupted() {
        yield return new WaitForSeconds(1);opening=false;paused=false;dayNightReviewFrozen=false;
        ORNeed(AudioListener.volume==0,"automated verification is silent");
        ORNeed(state.openingRideStage==2,"interrupted transit loads at accepted boarding stage");
        ORNeed(!OpeningRideActive&&!Blocked&&rideCar,"interruption restores an available stationary car");
        ORNeed(Vector3.Distance(player.transform.position,OpeningRideSafePoint(3))<.1f,"interrupted player returns to safe police boarding point");
        ORNeed(Vector3.Distance(rideCar.transform.position,PoliceRideParking)<.1f,"interrupted car returns to police parking");
        yield return ORUse(FindFirstObjectByType<OpeningRideBoarding>());
        yield return ORUntil(()=>rideCar&&rideCar.Occupied&&rideCar.Speed>.5f,20,"ordinary E boards and restarts interrupted journey");
        yield return Capture("opening-ride-08-interrupt-reload");
        Debug.Log("OPENING_RIDE_INTERRUPTED_SAVE_PASS");Application.Quit();
    }
}
