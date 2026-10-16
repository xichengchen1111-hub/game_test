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
        // He gets up from the table first; the walk waits for that (TickOpeningRide). From the side of
        // his chair he goes up the east side of the room and along its north wall, clear of the chair
        // opposite and of a player just standing up from it, then south of the open door leaf and out.
        StartCoroutine(LawyerStandUp());
        foreach(var p in new[]{OpeningPoint(17.35f,2.3f),OpeningPoint(17.35f,5.3f),OpeningPoint(14.6f,5.3f),OpeningPoint(14.3f,4.55f),OpeningPoint(13.6f,4.55f),OpeningPoint(12.4f,4.6f),OpeningPoint(11.3f,4.6f),OpeningPoint(11.3f,1.2f),OpeningPoint(11.3f,-2.1f),PoliceRideWaiting})lawyerWalk.Enqueue(p);
        OpeningReminder("律师：我先到门外等你，还有件事想问你。");
    }
    void StartLawyerToCar() {
        lawyerWalk.Clear();
        foreach(var p in new[]{new Vector3(24.8f,.12f,25.4f),new Vector3(31.1f,.12f,25.4f),LawyerDriverStanding})lawyerWalk.Enqueue(p);
    }
    void TickOpeningRide() {
        if(!sceneReady||CurrentScene!="Town"||GameTimePaused)return;
        if(lawyerWalk.Count>0&&!lawyerSitting) {
            var actor=FindOpeningActor("lawyer");
            if(actor&&DialogueOpen)actor.StopWalking();
            else if(actor) {
                // Use the same physical door mechanism; wait for a blocked swing.
                bool doorsReady=true;
                if(PoliceStation.Current&&actor.transform.position.z>27)foreach(string id in new[]{"police-interview-door","police-front-door-left","police-front-door-right"}) {
                    var door=PoliceStation.Current.Door(id);if(!door)continue;
                    if(!door.IsOpen&&!door.Moving)door.Use();if(door.OpenFraction<.9f)doorsReady=false;
                }
                // He waits rather than walk into the player standing in his way.
                var next=lawyerWalk.Peek();var ahead=Flat(next)-Flat(actor.transform.position);
                var rel=player?Flat(player.transform.position)-Flat(actor.transform.position):Vector3.one*100;
                bool blocked=rel.magnitude<.9f&&ahead.sqrMagnitude>.0001f&&Vector3.Dot(rel.normalized,ahead.normalized)>.35f;
                if(doorsReady&&!blocked){var target=next;actor.WalkTowards(target,1.7f,false,lawyerWalk.Count==1);if(Vector3.Distance(actor.transform.position,target)<.035f){lawyerWalk.Dequeue();if(lawyerWalk.Count==0)actor.StopWalking();}}
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
        Component actor=FindOpeningActor("lawyer");
        // At the hospital he says goodbye from behind the wheel, turned toward the player (user, 2026-10-15).
        if(node=="goodbye"&&rideCar&&rideCar.driver&&rideCar.driver.gameObject.activeInHierarchy) {
            actor=rideCar.driver;var look=rideCar.driver.GetComponent<SeatedLook>();if(look){look.target=player.eye.transform;look.look=true;}
        }
        BindDialoguePerformance(actor);
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
    // ---- Getting in (user, 2026-10-15: make it natural) ----
    // The passenger door swings open first; the view turns to the seat, steps to the sill, ducks under
    // the roof line and slides in while turning to face the road, settles into the cushion, and
    // glances at the door as it is pulled shut. The eye never jumps between positions or directions.
    Vector3 CarEye(Vector3 local)=>rideCar.transform.TransformPoint(local);
    static Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t){float u=1-t;return u*u*u*a+3*u*u*t*b+3*u*t*t*c+t*t*t*d;}
    // Car-local eye path through the right front door. The opening runs from the B-pillar (z -.48, right
    // beside the seat) to the A-pillar (z .26) and the open leaf swings forward of it, so the sill and bend
    // points keep to the middle of the opening; getting out first leans forward off the backrest.
    static readonly Vector3 RideStandEye=new Vector3(1.45f,1.68f,-.36f),RideBendEye=new Vector3(1.04f,1.42f,-.22f),RideSillEye=new Vector3(.62f,1.29f,-.18f),RideLeanEye=new Vector3(.38f,1.47f,-.34f);
    // Yaw of a world direction in the player's parent frame (the seat), and its pitch.
    void RideLook(Vector3 worldForward,out float yaw,out float pitch) {
        var local=player.transform.parent?player.transform.parent.InverseTransformDirection(worldForward):worldForward;
        yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;pitch=-Mathf.Asin(Mathf.Clamp(local.normalized.y,-1,1))*Mathf.Rad2Deg;
    }
    void RideSetView(Vector3 worldEye,float yaw,float pitch) {
        player.transform.localRotation=Quaternion.Euler(0,yaw,0);player.pitch=pitch;
        player.eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
        player.eye.transform.localPosition=player.transform.InverseTransformPoint(worldEye);
    }
    IEnumerator OpeningHospitalRide() {
        openingRideActive=true;openingRideCameraMoving=true;state.openingRideStage=3;
        SetMapOpen(false);SetWatchOpen(false);player.scriptedDirection=Vector3.zero;focus=null;Save();
        rideCar.SetPassengerDoor(true);CursorMode();
        // Turn toward the opening door and step up to the sill, still standing.
        var cc=player.GetComponent<CharacterController>();cc.enabled=false;
        Vector3 feet0=player.transform.position,feet1=rideCar.PassengerStandingPoint;
        float yaw0=player.transform.eulerAngles.y,pitch0=player.pitch;
        var seatEye=CarEye(RideSillEye);
        for(float t=0;t<1.1f;){
            if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,t/1.1f);
                var look=(seatEye-player.eye.transform.position);float yaw=Quaternion.LookRotation(new Vector3(look.x,0,look.z)).eulerAngles.y;
                float pitch=-Mathf.Atan2(look.y,new Vector2(look.x,look.z).magnitude)*Mathf.Rad2Deg;
                player.transform.position=Vector3.Lerp(feet0,feet1,k);player.transform.rotation=Quaternion.Euler(0,Mathf.LerpAngle(yaw0,yaw,Mathf.SmoothStep(0,1,Mathf.Min(1,t/.7f))),0);
                player.pitch=Mathf.Lerp(pitch0,pitch,Mathf.SmoothStep(0,1,Mathf.Min(1,t/.7f)));
                player.eye.transform.localPosition=new Vector3(0,1.66f+Mathf.Sin(k*Mathf.PI*2)*.012f,0);player.eye.transform.localRotation=Quaternion.Euler(player.pitch,0,0);}
            yield return null;
        }
        // Into the seat's frame, keeping exactly what the eye sees.
        var worldEye=player.eye.transform.position;var forward=player.eye.transform.forward;
        player.Mount(rideCar.PassengerSeat);RideLook(forward,out float y0,out float p0);RideSetView(worldEye,y0,p0);
        rideCar.Occupied=true;rideCar.SetSolid(false);
        Vector3 start=rideCar.transform.InverseTransformPoint(worldEye),end=rideCar.transform.InverseTransformPoint(rideCar.PassengerSeat.TransformPoint(rideCar.PassengerEyeLocal));
        for(float t=0;t<1.6f;){
            if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.Clamp01(t/1.6f),e=Mathf.SmoothStep(0,1,k);
                var eye=Bezier(start,RideBendEye,RideSillEye,end,e);
                // Head down under the roof line in the middle, turning to the road as the body comes round.
                float yaw=Mathf.LerpAngle(y0,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,k)));
                float pitch=Mathf.Lerp(p0,0,e)+16*Mathf.Sin(Mathf.PI*Mathf.Clamp01(k*1.15f));
                RideSetView(CarEye(eye),yaw,pitch);}
            yield return null;
        }
        // Settling into the cushion.
        for(float t=0;t<.45f;){if(!GameTimePaused){t+=Time.deltaTime;player.eye.transform.localPosition=rideCar.PassengerEyeLocal+Vector3.down*(.04f*Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/.45f)));}yield return null;}
        player.transform.localRotation=Quaternion.identity;player.eye.transform.localPosition=rideCar.PassengerEyeLocal;
        // A glance at the door while it is pulled shut.
        for(float t=0;t<1.3f;){if(!GameTimePaused){t+=Time.deltaTime;float g=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/1.3f));player.transform.localRotation=Quaternion.Euler(0,38*g,0);player.pitch=6*g;if(t>.45f&&rideCar.PassengerDoorOpen&&rideCar.passengerDoor.Open)rideCar.SetPassengerDoor(false);}yield return null;}
        player.transform.localRotation=Quaternion.identity;player.pitch=0;
        // Let the lawyer finish his actual route, then he gets in on his side.
        while(lawyerWalk.Count>0)yield return null;
        yield return LawyerGetsIn();
        rideCar.EngineRunning=true;openingRideCameraMoving=false;rideLine="律师：坐稳了。我送你去医院。";
        yield return RideWait(1.2f);
        yield return DriveOpeningRoute(OpeningHospitalRoute(),7.5f);
        rideCar.Speed=0;rideLine="律师：到了。医院的大门就在右边。";yield return RideWait(1.5f);
        yield return LeaveRideCar();
        state.openingRideStage=4;openingRideActive=false;openingRideCameraMoving=false;rideLine=null;
        // Facing the lawyer through the open door, at the height of his face.
        player.Aim(DriverHead(rideCar.transform.position+Vector3.up*1.3f));CursorMode();Save();OpenRideDialogue("goodbye");
        Debug.Log("OPENING_RIDE_ARRIVED");
    }
    // The lawyer opens his door, lowers himself in and swings round to the wheel; then the seated driver takes over.
    IEnumerator LawyerGetsIn() {
        var lawyer=FindOpeningActor("lawyer");lawyerWalk.Clear();
        if(lawyer&&lawyer.gameObject.activeSelf) {
            rideCar.SetDriverDoor(true);yield return RideWait(.6f);
            var pose=lawyer.GetComponent<NpcPose>();lawyer.StopWalking();
            Vector3 from=lawyer.transform.position,sill=rideCar.transform.TransformPoint(new Vector3(-.95f,.02f,-.4f)),seat=rideCar.transform.TransformPoint(new Vector3(-.3f,.02f,-.56f));
            Quaternion r0=lawyer.transform.rotation,r1=Quaternion.LookRotation(-rideCar.transform.forward);
            if(pose)pose.seatHeight=rideCar.transform.TransformPoint(new Vector3(0,.8f,0)).y;
            for(float t=0;t<1.7f;){
                if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.Clamp01(t/1.7f);
                    lawyer.transform.position=k<.4f?Vector3.Lerp(from,sill,Mathf.SmoothStep(0,1,k/.4f)):Vector3.Lerp(sill,seat,Mathf.SmoothStep(0,1,(k-.4f)/.6f));
                    lawyer.transform.rotation=Quaternion.Slerp(r0,r1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,.85f,k)));
                    if(pose&&k>.35f)pose.activity=NpcPose.Activity.Sit;}
                yield return null;
            }
            rideCar.SetDriverVisible(true);lawyer.gameObject.SetActive(false);
            if(pose){pose.activity=NpcPose.Activity.Stand;}
            yield return RideWait(.3f);rideCar.SetDriverDoor(false);yield return RideWait(.7f);
        } else {rideCar.SetDriverVisible(true);yield return RideWait(.5f);}
    }
    // Getting out: the door opens, he leans forward off the backrest turning to the middle of the
    // opening (not to the pillar beside the seat), ducks out past the sill and stands up on the kerb.
    IEnumerator LeaveRideCar() {
        openingRideCameraMoving=true;rideCar.SetPassengerDoor(true);
        float yawStart=Mathf.DeltaAngle(0,player.transform.localEulerAngles.y),pitchStart=player.pitch;
        Vector3 seated=rideCar.transform.InverseTransformPoint(player.eye.transform.position);
        for(float t=0;t<.8f;){
            if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,t/.8f);
                RideSetView(CarEye(Vector3.Lerp(seated,RideLeanEye,k)),Mathf.LerpAngle(yawStart,42,k),Mathf.Lerp(pitchStart,10,k));}
            yield return null;
        }
        for(float t=0;t<1.5f;){
            if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.Clamp01(t/1.5f),e=Mathf.SmoothStep(0,1,k);
                var eye=Bezier(RideLeanEye,RideSillEye,RideBendEye,RideStandEye,e);
                RideSetView(CarEye(eye),Mathf.Lerp(42,90,e),Mathf.Lerp(10,0,e)+12*Mathf.Sin(Mathf.PI*Mathf.Clamp01(k*1.1f)));}
            yield return null;
        }
        // Standing on the kerb: keep the view while the body leaves the seat.
        float worldYaw=player.eye.transform.eulerAngles.y;
        player.Dismount(transform,rideCar.PassengerStandingPoint,worldYaw);
        var cc=player.GetComponent<CharacterController>();cc.enabled=false;
        var from=player.transform.position;var face=rideCar.transform.position-HospitalDropoff;face.y=0;float endYaw=Quaternion.LookRotation(face).eulerAngles.y;
        for(float t=0;t<1.2f;){if(!GameTimePaused){t+=Time.deltaTime;float k=Mathf.SmoothStep(0,1,t/1.2f);player.transform.position=Vector3.Lerp(from,HospitalDropoff,k);player.transform.rotation=Quaternion.Euler(0,Mathf.LerpAngle(worldYaw,endYaw,k),0);player.eye.transform.localPosition=new Vector3(0,1.66f+Mathf.Sin(k*Mathf.PI*3)*.012f,0);}yield return null;}
        player.Dismount(transform,HospitalDropoff,endYaw);rideCar.Occupied=false;rideCar.SetSolid(true);
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
    // A route sampled every 20 cm by distance, optionally rounded (Chaikin) where it has corners.
    static List<Vector3> RideResample(Vector3[] points,bool round) {
        var src=new List<Vector3>(points);
        if(round)for(int pass=0;pass<4;pass++){var next=new List<Vector3>{src[0]};for(int i=0;i<src.Count-1;i++){next.Add(Vector3.Lerp(src[i],src[i+1],.25f));next.Add(Vector3.Lerp(src[i],src[i+1],.75f));}next.Add(src[src.Count-1]);src=next;}
        var path=new List<Vector3>{src[0]};float carry=0;
        for(int i=1;i<src.Count;i++){
            var a=src[i-1];var b=src[i];float len=Vector3.Distance(a,b);float d=.2f-carry;
            while(d<=len){path.Add(Vector3.Lerp(a,b,d/len));d+=.2f;}
            carry=len-(d-.2f);
        }
        if(Vector3.Distance(path[path.Count-1],src[src.Count-1])>.01f)path.Add(src[src.Count-1]);
        return path;
    }
    // The corners of the route are eased like a driver winding the wheel on and off: the positions
    // are averaged over a sliding window (twice, about a Gaussian), so the curvature rises and falls
    // smoothly into and out of each bend instead of jumping at the start of the arc. Ends stay put.
    static List<Vector3> RideEase(List<Vector3> path,int half) {
        var a=path;
        for(int pass=0;pass<2;pass++){
            var b=new List<Vector3>(a.Count);
            for(int i=0;i<a.Count;i++){
                int w=Mathf.Min(half,Mathf.Min(i,a.Count-1-i));
                var sum=Vector3.zero;for(int j=i-w;j<=i+w;j++)sum+=a[j];
                b.Add(sum/(2*w+1));
            }
            a=b;
        }
        return a;
    }
    // Follows the route by distance: the body always lies along the road's tangent (no sideways
    // slide), the front wheels and the steering wheel take the angle the curve needs, and the speed
    // comes from a profile that slows for each bend before reaching it and pulls away gently after.
    // The body leans out of the bend and dips under braking on its springs (OpeningRideCar.SetLean).
    IEnumerator DriveOpeningRoute(Vector3[] points,float cruise,bool round=false) {
        if(!rideCar)yield break;
        var path=RideResample(RideEase(RideResample(points,round),7).ToArray(),false);int n=path.Count;const float ds=.2f,lateral=1.8f,accel=1.3f,brake=1.7f,wheelbase=2.63f;
        var curve=new float[n];var limit=new float[n];
        for(int i=0;i<n;i++){
            int a=Mathf.Max(0,i-4),b=Mathf.Min(n-1,i+4),c=Mathf.Min(n-1,b+1),z=Mathf.Max(0,a-1);
            var t0=path[a]-path[z];var t1=path[c]-path[b];
            curve[i]=t0.sqrMagnitude>1e-6f&&t1.sqrMagnitude>1e-6f&&b>a?Vector3.SignedAngle(t0,t1,Vector3.up)*Mathf.Deg2Rad/((b-a)*ds):0;
            limit[i]=Mathf.Min(cruise,Mathf.Sqrt(lateral/Mathf.Max(Mathf.Abs(curve[i]),.0005f)));
        }
        limit[0]=0;for(int i=1;i<n;i++)limit[i]=Mathf.Min(limit[i],Mathf.Sqrt(limit[i-1]*limit[i-1]+2*accel*ds)+.15f);
        limit[n-1]=0;for(int i=n-2;i>=0;i--)limit[i]=Mathf.Min(limit[i],Mathf.Sqrt(limit[i+1]*limit[i+1]+2*brake*ds));
        float total=(n-1)*ds,s=0,speed=0,steer=rideCar.Steering;
        Vector3 At(float d){float f=Mathf.Clamp(d/ds,0,n-1);int i=Mathf.Min((int)f,n-2);return Vector3.Lerp(path[i],path[i+1],f-i);}
        float Sample(float[] values,float d){float f=Mathf.Clamp(d/ds,0,n-1);int i=Mathf.Min((int)f,n-2);return Mathf.Lerp(values[i],values[i+1],f-i);}
        while(s<total-.02f&&rideCar) {
            if(GameTimePaused){yield return null;continue;}
            float dt=Time.deltaTime;
            float desired=Mathf.Max(Sample(limit,s+speed*.4f),Mathf.Min(.35f,total-s));
            // The departing lawyer yields if the player stands in his path.
            if(!openingRideActive){var relative=rideCar.transform.InverseTransformPoint(player.transform.position);if(relative.z>0&&relative.z<5&&Mathf.Abs(relative.x)<1.7f)desired=0;}
            float before=speed;
            speed=Mathf.MoveTowards(speed,desired,(desired>speed?accel:brake*1.4f)*dt);
            s=Mathf.Min(total,s+speed*dt);
            rideCar.SetLean(speed*speed*Sample(curve,s),dt>0?(speed-before)/dt:0);
            var tangent=At(s+.6f)-At(s-.6f);tangent.y=0;
            rideCar.transform.position=At(s);
            if(tangent.sqrMagnitude>1e-6f)rideCar.transform.rotation=Quaternion.LookRotation(tangent.normalized);
            float k=Sample(curve,s);
            // Front-wheel angle for this curvature (bicycle model), eased like a hand on the wheel.
            float wheel=Mathf.Atan(wheelbase*k)*Mathf.Rad2Deg;
            steer=Mathf.MoveTowards(steer,Mathf.Clamp(wheel/25f,-1,1),dt*1.8f);
            rideCar.Steering=steer;rideCar.Speed=speed;
            // The passenger's head gives a little to the side in a bend.
            if(openingRideActive&&!openingRideCameraMoving)player.eye.transform.localPosition=rideCar.PassengerEyeLocal+Vector3.right*Mathf.Clamp(-speed*speed*k*.012f,-.035f,.035f);
            yield return null;
        }
        if(rideCar){rideCar.Speed=0;rideCar.Steering=0;}
        if(openingRideActive&&!openingRideCameraMoving)player.eye.transform.localPosition=rideCar.PassengerEyeLocal;
    }
    Vector3 DriverHead(Vector3 fallback) {
        if(!rideCar||!rideCar.driver)return fallback;
        foreach(var t in rideCar.driver.GetComponentsInChildren<Transform>())if(t.name=="Head")return t.position;
        return fallback;
    }
    IEnumerator LawyerDriveAway() {
        if(!rideCar)yield break;
        // A last look, then eyes back to the road as the door shuts.
        yield return RideWait(.6f);
        var look=rideCar.driver?rideCar.driver.GetComponent<SeatedLook>():null;if(look)look.look=false;
        rideCar.SetPassengerDoor(false);yield return RideWait(1);
        rideCar.SetSolid(false);rideCar.EngineRunning=true;
        yield return DriveOpeningRoute(new[]{rideCar.transform.position,P(80,-73.6f),P(68,-75),P(-10,-75)},7,true);
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
