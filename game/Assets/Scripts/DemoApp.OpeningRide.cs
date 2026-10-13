using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The optional first-act lift owns separate story state and a visual car.
// It never takes over the player's independently saved, drivable hotel car.
public partial class DemoApp {
    OpeningRideCar rideCar;
    OpeningRideBoarding rideBoarding;
    DialogueScript rideScript;
    bool openingRideActive,openingRideCameraMoving,rideOfferShown;
    readonly Queue<Vector3> lawyerWalk=new Queue<Vector3>();
    public bool OpeningRideActive=>openingRideActive;
    public bool OpeningRideCameraMoving=>openingRideCameraMoving;
    public static Vector3 PoliceRideParking=>new Vector3(29.1f,.025f,30);
    public static Vector3 PoliceRideWaiting=>new Vector3(12.5f,.12f,25.4f);
    public static Vector3 HospitalRideParking=>new Vector3(101.4f,.025f,-73.6f);
    public static Vector3 HospitalDropoff=>new Vector3(101.4f,.12f,-69.5f);
    public static Vector3 OpeningRideSafePoint(int stage)=>stage>=4?HospitalDropoff:new Vector3(26.5f,.12f,30.36f);

    void InitializeOpeningRideState(bool legacyInvestigation) {
        if(state.openingRideRevision==0){state.openingRideRevision=1;state.openingRideStage=legacyInvestigation?5:state.openingStage>=4?1:0;}
        // An interrupted journey resumes at the car, with the invitation accepted.
        // Never load a disabled controller into empty air or into a missing vehicle.
        if(state.openingRideStage==3){state.openingRideStage=2;var p=OpeningRideSafePoint(2);state.x=p.x;state.y=p.y;state.z=p.z;state.yaw=90;state.scene="Town";}
    }
    void EnsureOpeningRideScene() {
        if(CurrentScene!="Town"||state.openingRideStage==0||state.openingRideStage>=5)return;
        if(!rideCar) {
            var prefab=Resources.Load<GameObject>("OpeningRideCar");
            if(!prefab){Debug.LogError("Missing OpeningRideCar resource");return;}
            rideCar=Instantiate(prefab).GetComponent<OpeningRideCar>();
            rideCar.transform.SetPositionAndRotation(state.openingRideStage>=4?HospitalRideParking:PoliceRideParking,Quaternion.Euler(0,state.openingRideStage>=4?-90:180,0));
            rideBoarding=rideCar.gameObject.AddComponent<OpeningRideBoarding>();rideBoarding.title="律师的汽车";
            rideCar.SetDriverVisible(state.openingRideStage>=4);
            rideCar.SetPassengerDoor(state.openingRideStage>=4,true);
        }
    }
    void RestoreOpeningRideScene() {
        lawyerWalk.Clear();EnsureOpeningRideScene();
        if(state.openingStage<4)return;
        OpeningActor lawyer=null;foreach(var actor in FindObjectsByType<OpeningActor>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(actor.role=="lawyer")lawyer=actor;
        if(lawyer){lawyer.StopWalking();lawyer.gameObject.SetActive(state.openingRideStage<3);lawyer.transform.position=state.openingRideStage==2?LawyerDriverStanding:PoliceRideWaiting;lawyer.transform.rotation=Quaternion.Euler(0,0,0);}
    }
    Vector3 LawyerDriverStanding=>rideCar?rideCar.transform.TransformPoint(new Vector3(-1.55f,.02f,-.36f)):new Vector3(30.55f,.12f,29.64f);
    void StartLawyerExit() {
        EnsureOpeningRideScene();lawyerWalk.Clear();
        foreach(var p in new[]{OpeningPoint(17.6f,1.3f),OpeningPoint(17.6f,4.6f),OpeningPoint(11.3f,4.6f),OpeningPoint(11.3f,1.2f),OpeningPoint(11.3f,-2.1f),PoliceRideWaiting})lawyerWalk.Enqueue(p);
        OpeningReminder("律师：我先到门外等你，还有件事想问你。");
    }
    void StartLawyerToCar() {
        lawyerWalk.Clear();
        foreach(var p in new[]{new Vector3(24.8f,.12f,25.4f),new Vector3(31.1f,.12f,25.4f),LawyerDriverStanding})lawyerWalk.Enqueue(p);
    }
    void TickOpeningRide() {
        if(!sceneReady||CurrentScene!="Town"||GameTimePaused)return;
        if(lawyerWalk.Count>0) {
            var actor=FindOpeningActor("lawyer");
            if(actor&&DialogueOpen)actor.StopWalking();
            else if(actor) {
                // Use the same physical door mechanism; wait for a blocked swing.
                bool doorsReady=true;
                if(PoliceStation.Current&&actor.transform.position.z>27)foreach(string id in new[]{"police-interview-door","police-front-door-left","police-front-door-right"}) {
                    var door=PoliceStation.Current.Door(id);if(!door)continue;
                    if(!door.IsOpen&&!door.Moving)door.Use();if(door.OpenFraction<.9f)doorsReady=false;
                }
                if(doorsReady){var target=lawyerWalk.Peek();actor.WalkTowards(target,1.7f);if(Vector3.Distance(actor.transform.position,target)<.035f){lawyerWalk.Dequeue();if(lawyerWalk.Count==0)actor.StopWalking();}}
                else actor.StopWalking();
            }
        }
        if(Review||Blocked||openingRideActive||player.CreatorFlying)return;
        if(state.openingRideStage==1&&!rideOfferShown&&lawyerWalk.Count==0&&Vector3.Distance(player.transform.position,PoliceRideWaiting)<3) {
            rideOfferShown=true;var actor=FindOpeningActor("lawyer");if(actor)player.Aim(actor.transform.position+Vector3.up*1.45f);OpenRideDialogue("offer");
        } else if(state.openingRideStage==4&&Vector3.Distance(player.transform.position,HospitalDropoff)<5)OpenRideDialogue("goodbye");
    }
    void OpenRideDialogue(string node) {
        if(Blocked)return;
        rideScript=DialogueScript.Load("OpeningRideDialogue");milkboyScript=rideScript;
        var actor=FindOpeningActor("lawyer");BindDialoguePerformance(actor);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        VisitDialogue(node);CursorMode();
    }
    bool TryOpeningRideDialogueAction() {
        if(rideScript==null||milkboyScript!=rideScript)return false;
        if(dialogueNode.id=="accept") {state.openingRideStage=2;CloseDialogue();StartLawyerToCar();OpeningReminder("去警署东侧中街的汽车旁，靠近右侧前门按 E 上车。");return true;}
        if(dialogueNode.id=="later") {rideOfferShown=true;CloseDialogue();return true;}
        return false;
    }
    void OpeningRideDialogueClosed() {
        if(milkboyScript==rideScript&&rideScript!=null&&state.openingRideStage==4) {
            state.openingRideStage=5;StartCoroutine(LawyerDriveAway());
        }
    }
    public void UseOpeningRideCar() {
        if(Blocked||OpeningRideActive)return;
        if(state.openingRideStage==4){OpenRideDialogue("goodbye");return;}
        if(state.openingRideStage==1){OpeningReminder("律师还在警署门外等你。先和他谈谈。");return;}
        if(state.openingRideStage==2)BeginOpeningRide();
    }
    void BeginOpeningRide() {
        if(!rideCar||Blocked||OpeningRideActive||state.openingRideStage!=2||CarSeated||RailSeated||FurnitureSeated)return;
        if(player.CreatorFlying){OpeningReminder("先按 F3 返回地面，再上车。");return;}
        if(Vector3.Distance(player.eye.transform.position,rideCar.PassengerSeat.position+Vector3.up*.7f)>3.4f)return;
        var side=rideCar.transform.InverseTransformPoint(player.transform.position);
        if(side.x<.95f||side.z< -1.1f||side.z>.55f){OpeningReminder("请从靠近警署的右侧前门上车。");return;}
        StartCoroutine(OpeningHospitalRide());
    }
    IEnumerator RideWait(float seconds) {
        for(float t=0;t<seconds;){if(!GameTimePaused)t+=Time.deltaTime;yield return null;}
    }
    IEnumerator RideEyeTo(Vector3 target,float seconds) {
        Vector3 start=player.eye.transform.localPosition;
        for(float t=0;t<seconds;){if(!GameTimePaused){t+=Time.deltaTime;player.eye.transform.localPosition=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,t/seconds));}yield return null;}
        player.eye.transform.localPosition=target;
    }
    IEnumerator OpeningHospitalRide() {
        openingRideActive=true;openingRideCameraMoving=true;state.openingRideStage=3;
        SetMapOpen(false);SetWatchOpen(false);player.scriptedDirection=Vector3.zero;focus=null;Save();
        // Keep the eye continuous through the open passenger aperture before sitting.
        var worldEye=player.eye.transform.position;player.Mount(rideCar.PassengerSeat);
        player.eye.transform.localPosition=player.transform.InverseTransformPoint(worldEye);
        rideCar.Occupied=true;rideCar.SetSolid(false);rideCar.SetPassengerDoor(true);CursorMode();
        yield return RideWait(.75f);
        yield return RideEyeTo(new Vector3(1.1f,.95f,.1f),.65f);
        yield return RideEyeTo(rideCar.PassengerEyeLocal,.85f);
        // Let the lawyer finish his actual route before taking the driver seat.
        while(lawyerWalk.Count>0)yield return null;
        // The lawyer has walked to his side of the car while we approach ours.
        var lawyer=FindOpeningActor("lawyer");lawyerWalk.Clear();if(lawyer)lawyer.gameObject.SetActive(false);
        rideCar.SetDriverVisible(true);rideCar.SetPassengerDoor(false);yield return RideWait(.85f);
        rideCar.EngineRunning=true;openingRideCameraMoving=false;rideLine="律师：坐稳了。我送你去医院。";
        yield return RideWait(1.2f);
        yield return DriveOpeningRoute(OpeningHospitalRoute(),7.5f);
        rideCar.Speed=0;rideLine="律师：到了。医院的大门就在右边。";yield return RideWait(1.5f);
        openingRideCameraMoving=true;rideCar.SetPassengerDoor(true);yield return RideWait(.75f);
        // Turn toward the open door, rise from the seat and step onto the approach.
        player.transform.localRotation=Quaternion.Euler(0,90,0);player.pitch=0;
        yield return RideEyeTo(player.transform.InverseTransformPoint(rideCar.PassengerStandingPoint+Vector3.up*1.66f),1f);
        player.Dismount(transform,rideCar.PassengerStandingPoint,0);
        // A short visible step from the gutter onto the clear hospital approach.
        var from=player.transform.position;var cc=player.GetComponent<CharacterController>();cc.enabled=false;
        for(float t=0;t<1;){if(!GameTimePaused){t+=Time.deltaTime;player.transform.position=Vector3.Lerp(from,HospitalDropoff,Mathf.SmoothStep(0,1,t));}yield return null;}
        player.Dismount(transform,HospitalDropoff,180);rideCar.Occupied=false;rideCar.SetSolid(true);
        state.openingRideStage=4;openingRideActive=false;openingRideCameraMoving=false;rideLine=null;
        player.Aim(rideCar.transform.position+Vector3.up*1.3f);CursorMode();Save();OpenRideDialogue("goodbye");
        Debug.Log("OPENING_RIDE_ARRIVED");
    }
    // Samples of tangent arcs stay on the existing intersections. No world warp or time skip.
    static Vector3 P(float x,float z)=>new Vector3(x,.025f,z);
    static void Arc(List<Vector3> points,Vector3 centre,float radius,float from,float to) {
        for(int i=1;i<=18;i++){float a=Mathf.Lerp(from,to,i/18f)*Mathf.Deg2Rad;points.Add(centre+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}
    }
    static Vector3[] OpeningHospitalRoute() {
        var p=new List<Vector3>{PoliceRideParking};
        for(int i=1;i<=28;i++){float t=i/28f;p.Add(P(Mathf.Lerp(29.1f,30,Mathf.SmoothStep(0,1,t)),Mathf.Lerp(30,16,t)));}
        p.Add(P(30,-117));Arc(p,P(35,-117),5,180,270);p.Add(P(130,-122));Arc(p,P(130,-117),5,270,360);
        p.Add(P(135,-80));Arc(p,P(130,-80),5,0,90);p.Add(P(115,-75));
        for(int i=1;i<=28;i++){float t=i/28f;p.Add(P(Mathf.Lerp(115,101.4f,t),Mathf.Lerp(-75,-73.6f,Mathf.SmoothStep(0,1,t))));}
        return p.ToArray();
    }
    IEnumerator DriveOpeningRoute(Vector3[] points,float cruise) {
        float speed=0,total=0;for(int i=1;i<points.Length;i++)total+=Vector3.Distance(points[i-1],points[i]);
        float remaining=total;
        for(int i=1;i<points.Length&&rideCar;i++) {
            var start=rideCar.transform.position;var end=points[i];float length=Vector3.Distance(start,end),travelled=0;
            while(travelled<length&&rideCar) {
                if(GameTimePaused){yield return null;continue;}
                var forward=(end-start).normalized;
                float turning=Vector3.Angle(rideCar.transform.forward,forward);
                float desired=Mathf.Min(cruise,Mathf.Sqrt(Mathf.Max(.16f,remaining)*3f));
                if(turning>3)desired=Mathf.Min(desired,3.5f);
                // The departing lawyer yields if the player stands in his path.
                if(!openingRideActive) {var relative=rideCar.transform.InverseTransformPoint(player.transform.position);if(relative.z>0&&relative.z<5&&Mathf.Abs(relative.x)<1.7f)desired=0;}
                speed=Mathf.MoveTowards(speed,desired,Time.deltaTime*2.5f);
                float step=Mathf.Min(length-travelled,speed*Time.deltaTime);travelled+=step;remaining-=step;
                rideCar.transform.position=Vector3.Lerp(start,end,travelled/Mathf.Max(length,.001f));
                rideCar.transform.rotation=Quaternion.RotateTowards(rideCar.transform.rotation,Quaternion.LookRotation(forward),Time.deltaTime*90);
                rideCar.Steering=Mathf.Clamp(Vector3.SignedAngle(rideCar.transform.forward,forward,Vector3.up),-30,30)/30;
                rideCar.Speed=speed;yield return null;
            }
        }
        if(rideCar){rideCar.Speed=0;rideCar.Steering=0;}
    }
    IEnumerator LawyerDriveAway() {
        if(!rideCar)yield break;
        rideCar.SetPassengerDoor(false);yield return RideWait(1);
        rideCar.SetSolid(false);rideCar.EngineRunning=true;
        yield return DriveOpeningRoute(new[]{rideCar.transform.position,P(80,-73.6f),P(68,-75),P(-10,-75)},7);
        if(rideCar)Destroy(rideCar.gameObject);
    }
    string rideLine;
    void DrawOpeningRide() {
        if(!openingRideActive||paused||opening)return;
        string line=rideLine??"上车 · 律师送你去医院";
        Box(new Rect(0,H-112,W,112),new Color(.015f,.023f,.03f,.62f));
        Label(new Rect(60,H-98,W-120,48),line,text);
        Label(new Rect(60,H-43,W-120,28),openingRideCameraMoving?"Esc 暂停":"鼠标 环顾车窗与街道  ·  Esc 暂停",small);
    }
}
