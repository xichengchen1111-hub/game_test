using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// The opening cinematic (user, 2026-10-15): first person, lying on the cell's iron bed. The eyes
// open with a few heavy blinks through a blur, "my head hurts", a slow look round a place that does
// not belong to his time. He came here on purpose: Dr Fang Ming's shuttle device, built in his own
// time, was to send him back to a murder that was never solved, and he had not quite believed it
// would work. While he is still taking that in, the guard bangs on the iron door with his fist and
// cuts him off, unlocks it with his key, shoves it open, tells him curtly that he is free to go
// for want of evidence and brings him out into the cloister; the cinematic ends there and the walk
// to the property room is played. The guard stays out of sight behind the cell wall until he
// knocks. The shared clock keeps running; Esc skips to the end state.
public partial class DemoApp {
    bool cellCinematic,cineSkip,cineGuardWalking;
    public bool CellCinematicActive=>cellCinematic;
    // Eyelids: 1 closed, 0 open. Letterbox 0..1. Blur 0..1 (depth of field while the eyes focus).
    float cineLids=1,cineBars,cinePain,cineShake,cineRoll,cineYaw,cinePitch,cineBlur;
    Vector3 cineEye;
    string cineLine,cineSpeaker;bool cineInner;
    // Characters of the current line shown so far (it is written out as it is thought or said).
    float cineReveal;
    // Blows of the guard's fist that have landed (reviews check the hand reached the door each time).
    int cineBlows;float cineBlowReach,cineBlowError;
    AudioSource cineAudio;
    Texture2D cineLidTexture,cineLidBottom;
    Volume cineVolume;DepthOfField cineFocus;
    Transform Station=>PoliceStation.Current.transform;
    Vector3 CellPoint(float x,float y,float z)=>Station.TransformPoint(new Vector3(x,y,z));
    float CellYaw(Vector3 from,Vector3 to){var d=to-from;d.y=0;return Quaternion.LookRotation(d).eulerAngles.y;}
    float CellPitch(Vector3 from,Vector3 to){var d=to-from;return -Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;}
    // Building-local places of the scene. At the bars he stands a little toward the lock, close
    // enough to reach the door; aside, he leaves the doorway and the cloister path clear.
    // Aside he waits in the cloister walk south of the cell door, clear of the loggia columns
    // (x 16.18, every 2.4 m) and a little more than an arm's length from the player coming out.
    static readonly Vector3 CineGuardHidden=new Vector3(16.95f,0,22.9f),CineGuardAside=new Vector3(16.95f,0,19.2f);
    static readonly Vector3 CineGuardAsideFacing=new Vector3(.15f,0,1);
    // Where he stands to knock (world): worked out from the door and the length of his arm.
    Vector3 cineBarsStand;
    Vector3 KnockPoint(ShopMechanism door,float floor){DoorFrame(door,OpeningPoint(16.95f,20.6f),out var centre,out var along,out var toward);var contact=centre-along*.1f+toward*.05f;contact.y=floor+1.48f;return contact;}
    static readonly Vector3 CinePlayerOut=new Vector3(17.05f,0,20.55f);
    IEnumerator CineWait(float seconds){for(float t=0;t<seconds&&!cineSkip;t+=Time.deltaTime)yield return null;}
    // Eases the eye, view direction and lids together.
    IEnumerator CineMove(Vector3 eye,float yaw,float pitch,float seconds,float lids=-1) {
        Vector3 e0=cineEye;float y0=cineYaw,p0=cinePitch,l0=cineLids;
        for(float t=0;t<seconds&&!cineSkip;t+=Time.deltaTime) {
            float k=Mathf.SmoothStep(0,1,t/seconds);
            cineEye=Vector3.Lerp(e0,eye,k);cineYaw=Mathf.LerpAngle(y0,yaw,k);cinePitch=Mathf.Lerp(p0,pitch,k);
            if(lids>=0)cineLids=Mathf.Lerp(l0,lids,k);
            yield return null;
        }
        if(!cineSkip){cineEye=eye;cineYaw=yaw;cinePitch=pitch;if(lids>=0)cineLids=lids;}
    }
    IEnumerator CineLook(Vector3 eye,Vector3 target,float seconds)=>CineMove(eye,CellYaw(eye,target),CellPitch(eye,target),seconds);
    IEnumerator CineLids(float to,float seconds){float from=cineLids;for(float t=0;t<seconds&&!cineSkip;t+=Time.deltaTime){cineLids=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t/seconds));yield return null;}if(!cineSkip)cineLids=to;}
    IEnumerator CineBlurTo(float to,float seconds){float from=cineBlur;for(float t=0;t<seconds&&!cineSkip;t+=Time.deltaTime){SetCineBlur(Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,t/seconds)));yield return null;}if(!cineSkip)SetCineBlur(to);}
    // A line is written out at reading pace and then held; a spoken one also moves the speaker's mouth.
    IEnumerator CineSay(string speaker,string line,float seconds,bool inner=false,PeriodCharacterPerformance voice=null) {
        cineSpeaker=speaker;cineLine=line;cineInner=inner;cineReveal=0;if(voice)voice.Speak(line);
        float perSecond=inner?15:13;
        for(float t=0;t<Mathf.Max(seconds,line.Length/perSecond+1)&&!cineSkip;t+=Time.deltaTime){cineReveal=t*perSecond;yield return null;}
        cineLine=null;if(voice)voice.Listen();
    }
    // A thought that is cut off: it is written out and stays on screen until something interrupts it.
    IEnumerator CineThinkUntilCut(string line) {
        cineSpeaker="你";cineLine=line;cineInner=true;cineReveal=0;
        for(float t=0;t<line.Length/15f+.25f&&!cineSkip;t+=Time.deltaTime){cineReveal=t*15;yield return null;}
    }
    IEnumerator CellCinematic() {
        cellCinematic=true;cineSkip=false;cineBlows=0;cineBlowReach=0;cineBlowError=0;SetMapOpen(false);SetWatchOpen(false);CursorMode();
        Debug.Log("OPENING_CINEMATIC_BEGIN");
        var guard=FindOpeningActor("guard");var voice=guard?guard.GetComponentInChildren<PeriodCharacterPerformance>():null;
        var hands=guard?guard.GetComponent<NpcHands>():null;
        var door=PoliceStation.Current.Door("police-holding-door");
        // The guard waits further along the cloister, behind the cell wall: nobody is at the bars yet.
        if(guard){guard.StopWalking();guard.transform.position=OpeningPoint(CineGuardHidden.x,CineGuardHidden.z);FaceGuard(guard,new Vector3(0,0,-1));}
        player.Place(OpeningPoint(21.2f,21.9f),0);
        // Lying on the bed: head on the north end, looking at the ceiling.
        Vector3 lying=CellPoint(22.16f,.9f,22.3f),sitting=CellPoint(21.98f,1.44f,21.8f),doorPoint=CellPoint(17.7f,1.55f,20.6f);
        cineEye=lying;cineYaw=CellYaw(lying,CellPoint(22.16f,.9f,20f));cinePitch=-78;cineLids=1;cineBars=0;cinePain=0;cineRoll=0;cineLine=null;
        SetCineBlur(1);EnsureCineAudio();
        for(float t=0;t<.8f&&!cineSkip;t+=Time.deltaTime){cineBars=t/.8f;yield return null;}
        cineBars=1;PlayCine(CineTinnitus(),.5f);
        yield return CineWait(1.4f);
        // Heavy blinks, the first ones failing, the room swimming into focus.
        yield return CineLids(.62f,.9f);yield return CineWait(.45f);yield return CineLids(1,.22f);yield return CineWait(.5f);
        StartCoroutine(CineBlurTo(.7f,1.6f));
        yield return CineLids(.38f,.8f);yield return CineWait(.35f);yield return CineLids(.92f,.18f);yield return CineWait(.25f);
        StartCoroutine(CineBlurTo(.35f,1.4f));
        yield return CineLids(0,1.1f);
        cinePain=1;StartCoroutine(CineBlurTo(.12f,2.4f));
        ShowMark("awake");yield return CineSay("你","……头好痛。",2.4f);
        cinePain=.4f;
        // Sit up on the edge of the bed, facing the cell door: the cloister beyond the bars is empty.
        StartCoroutine(CineBlurTo(0,1.6f));
        yield return CineMove(sitting,CellYaw(sitting,CellPoint(19.5f,1.4f,21.0f)),12,1.9f);
        cinePain=.2f;yield return CineWait(.4f);
        ShowMark("look");yield return CineLook(sitting,CellPoint(20.25f,2.45f,23.6f),1.4f);
        yield return CineSay("你","（石墙，铁栏，高处一扇小窗……）",2.4f,true);
        yield return CineLook(sitting,CellPoint(20.25f,2.7f,21.6f),1.2f);
        yield return CineSay("你","（这种吊灯，我只在老照片里见过。）",2.6f,true);
        // While he looks at the basin and the bucket, the guard walks up to the bars, unseen.
        yield return CineLook(sitting,CellPoint(20.1f,.6f,23.2f),1.2f);
        if(guard) {
            // Close up to the bars, his right fist level with his shoulder.
            float floor=OpeningPoint(17,20.6f).y;var contact=door?KnockPoint(door,floor):OpeningPoint(17.8f,20.5f)+Vector3.up*1.48f;
            var toward=Station.TransformDirection(Vector3.left);if(door)DoorFrame(door,OpeningPoint(16.95f,20.6f),out _,out _,out toward);
            cineBarsStand=hands?hands.StandFor(true,contact,-toward,.26f):contact+toward*.45f;cineBarsStand.y=floor;
            StartCoroutine(CineGuardWalk(guard,OpeningLocal(cineBarsStand),1f,new Vector3(1,0,0)));
        }
        yield return CineSay("你","（搪瓷盆，木桶……全是几十年前的东西。）",2.8f,true);
        // Staring at the floor by his feet, turned away from the door (the guard there stays out of frame).
        yield return CineLook(sitting,CellPoint(21.6f,.15f,20.6f),1.0f);cinePain=0;
        ShowMark("realize");yield return CineSay("你","（我……真的穿越了？）",2.2f,true);
        yield return CineSay("你","（没想到，方明博士的穿梭装置……竟然是真的。）",3.0f,true);
        // Still taking it in when the knocking cuts him off.
        while(cineGuardWalking&&!cineSkip)yield return null;
        yield return CineThinkUntilCut("（这么说，现在是一九三一年。那桩到我们那时候都没破的案子——）");
        cineLine=null;
        float yawDoor=CellYaw(sitting,doorPoint),pitchDoor=CellPitch(sitting,doorPoint);
        // Three blows of his fist on the bars; the first one turns the head toward the door.
        if(guard&&hands&&door)yield return CineGuardKnocks(guard,hands,door,blow=>{cineShake=1;if(blow==0)StartCoroutine(CineMove(sitting,yawDoor,pitchDoor,.35f));});
        else{for(int k=0;k<3&&!cineSkip;k++){cineShake=1;cineBlows++;PlayCine(Resources.Load<AudioClip>("OpeningSounds/CellKnock"),1);if(k==0)StartCoroutine(CineMove(sitting,yawDoor,pitchDoor,.35f));yield return CineWait(.42f);}}
        yield return CineWait(.25f);
        yield return CineSay("狱警","喂！醒了没有？起来，到门口来。",2.4f,false,voice);
        // Stand up and go to the bars; he unlocks the door, shoves it open and speaks from the doorway.
        Vector3 stand=CellPoint(21.0f,1.81f,21.4f),atDoor=CellPoint(19.4f,1.81f,20.72f);
        yield return CineMove(stand,CellYaw(stand,doorPoint),4,1.1f);
        StartCoroutine(CineWalkBob(1.8f));
        yield return CineMove(atDoor,CellYaw(atDoor,doorPoint+Vector3.up*.1f),2,1.8f);
        if(guard&&hands&&door)yield return CineGuardUnlocks(guard,hands,door);
        else{PlayCine(Resources.Load<AudioClip>("OpeningSounds/CellUnlock"),.8f);yield return CineWait(.5f);if(door&&!door.IsOpen&&!door.Moving)door.Use();}
        yield return CineWait(.6f);
        yield return CineLook(atDoor,guard?guard.transform.position+Vector3.up*1.6f:doorPoint,.6f);
        ShowMark("released");yield return CineSay("狱警","算你走运。证据不够，上头不扣你了——律师给你办了保释。",3.6f,false,voice);
        if(guard)StartCoroutine(CineGuardWalk(guard,CineGuardAside,.9f,CineGuardAsideFacing));
        yield return CineSay("狱警","出来。",1.3f,false,voice);
        // Out through the open door into the cloister, the guard waiting to one side.
        Vector3 sill=CellPoint(18.1f,1.81f,20.62f),outside=CellPoint(CinePlayerOut.x,1.81f,CinePlayerOut.z);
        StartCoroutine(CineWalkBob(2.1f));
        yield return CineMove(sill,CellYaw(atDoor,sill),2,1.0f);
        Vector3 guardHead=guard?OpeningPoint(CineGuardAside.x,CineGuardAside.z)+Vector3.up*1.55f:outside+Vector3.left;
        yield return CineMove(outside,CellYaw(outside,guardHead),CellPitch(outside,guardHead),1.1f);
        while(cineGuardWalking&&!cineSkip)yield return null;
        yield return CineSay("狱警","跟我过来，拿你的东西。",2.4f,false,voice);
        FinishCellCinematic(guard,door);
    }
    // The guard walks to a building-local place, slowing onto it, and turns there to face a building-local direction.
    IEnumerator CineGuardWalk(OpeningActor guard,Vector3 local,float speed,Vector3 face) {
        cineGuardWalking=true;
        var to=OpeningPoint(local.x,local.z);
        while(cellCinematic&&!cineSkip&&Vector3.Distance(Flat(guard.transform.position),Flat(to))>.02f){guard.WalkTowards(to,speed,true,true);yield return null;}
        guard.StopWalking();
        var direction=Station.TransformDirection(face);
        for(float t=0;t<3&&cellCinematic&&!cineSkip;t+=Time.deltaTime){if(guard.TurnToward(direction))break;yield return null;}
        cineGuardWalking=false;
    }
    // Where a door's leaf is when shut (world): the middle of the doorway, the way the leaf runs from
    // hinge to free edge, and the side of it that faces `from`.
    static void DoorFrame(ShopMechanism door,Vector3 from,out Vector3 centre,out Vector3 along,out Vector3 toward) {
        centre=door.DoorwayCentre;along=door.ClosedAlong;
        var n=Vector3.Cross(Vector3.up,along);if(Vector3.Dot(from-centre,n)<0)n=-n;toward=n;
    }
    // Three blows of the fist on the bars (user, 2026-10-15: there was knocking but no hand moved).
    // The fist is drawn back, driven in fast, lands with the sound, and comes back slower.
    IEnumerator CineGuardKnocks(OpeningActor guard,NpcHands hands,ShopMechanism door,System.Action<int> blow) {
        DoorFrame(door,OpeningPoint(16.95f,20.6f),out _,out _,out var toward);
        var contact=KnockPoint(door,guard.transform.position.y);
        var cocked=contact+toward*.2f+Vector3.up*.07f;
        // Knuckles to the bars: the fingers, before they close, point at the door and up; the palm turns in.
        Vector3 fingers=(-toward*.75f+Vector3.up*.65f).normalized,palm=-hands.Side;
        var knock=Resources.Load<AudioClip>("OpeningSounds/CellKnock");
        var pose=guard.GetComponent<NpcPose>();if(pose){pose.hasFocus=true;pose.focus=contact;}
        hands.SetGrip(true,NpcHands.Grip.Fist);
        for(float t=0;t<.34f&&!cineSkip;t+=Time.deltaTime){hands.Reach(true,cocked,.3f);hands.Orient(true,fingers,palm);yield return null;}
        for(int k=0;k<3&&!cineSkip;k++) {
            for(float t=0;t<.075f&&!cineSkip;t+=Time.deltaTime){float e=t/.075f;hands.Reach(true,Vector3.Lerp(cocked,contact,e*e));hands.Orient(true,fingers,palm);yield return null;}
            hands.Reach(true,contact);yield return null;
            cineBlows++;cineBlowReach=Mathf.Max(cineBlowReach,hands.Weight(true));cineBlowError=Mathf.Max(cineBlowError,Vector3.Distance(hands.Palm(true),contact));
            Debug.Log("GUARD_KNOCK "+k+" weight="+hands.Weight(true).ToString("F2")+" error="+hands.Error(true).ToString("F3")+" palmToContact="+Vector3.Distance(hands.Palm(true),contact).ToString("F3")+" shoulderToContact="+Vector3.Distance(hands.Shoulder(true),contact).ToString("F3")+" arm="+hands.ArmLength(true).ToString("F3"));
            PlayAt(knock,contact,1f,1+(k-1)*.025f);if(k==0)ShowMark("knock");blow?.Invoke(k);
            for(float t=0;t<.2f&&!cineSkip;t+=Time.deltaTime){hands.Reach(true,Vector3.Lerp(contact,cocked,Mathf.SmoothStep(0,1,t/.2f)));hands.Orient(true,fingers,palm);yield return null;}
            for(float t=0;t<.12f&&!cineSkip;t+=Time.deltaTime){hands.Reach(true,cocked);hands.Orient(true,fingers,palm);yield return null;}
        }
        hands.Release(true,.45f);if(pose)pose.hasFocus=false;
    }
    // The key in the lock with the hand on that side, a turn of the wrist, then the flat of the hand
    // on the bars and a shove: the heavy leaf swings into the cell and the hand goes a little way with it.
    IEnumerator CineGuardUnlocks(OpeningActor guard,NpcHands hands,ShopMechanism door) {
        float feet=guard.transform.position.y;
        DoorFrame(door,guard.transform.position,out _,out _,out var toward);
        Vector3 Lock()=>door.EdgePoint(feet+1.05f,.07f)+toward*.05f;
        bool right=hands.RightOf(Lock());
        var side=right?hands.Side:-hands.Side;
        Vector3 fingers=-toward,palm=-side;
        var pose=guard.GetComponent<NpcPose>();if(pose){pose.hasFocus=true;pose.focus=Lock();}
        hands.SetGrip(right,NpcHands.Grip.Pinch);
        for(float t=0;t<.5f&&!cineSkip;t+=Time.deltaTime){hands.Reach(right,Lock(),.45f);hands.Orient(right,fingers,palm);yield return null;}
        PlayAt(Resources.Load<AudioClip>("OpeningSounds/CellUnlock"),Lock(),.95f);ShowMark("unlock");
        for(float t=0;t<.62f&&!cineSkip;t+=Time.deltaTime) {
            float turn=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,.52f,t))*75*(right?1:-1);
            hands.Reach(right,Lock());hands.Orient(right,fingers,Quaternion.AngleAxis(turn,fingers)*palm);yield return null;
        }
        hands.SetGrip(right,NpcHands.Grip.Open);
        Vector3 Bars(){var edge=door.EdgePoint(feet+1.12f,.14f);var from=hands.Shoulder(right);from.y=edge.y;return edge-(edge-from).normalized*.04f;}
        for(float t=0;t<.28f&&!cineSkip;t+=Time.deltaTime){var p=Bars();hands.Reach(right,p);hands.Orient(right,Vector3.up,p-hands.Shoulder(right));yield return null;}
        hands.Lean(7);
        if(!door.IsOpen&&!door.Moving)door.Use();
        for(float t=0;t<1.2f&&!cineSkip;t+=Time.deltaTime){var p=Bars();if(!hands.Within(right,p,.97f))break;hands.Reach(right,p);hands.Orient(right,Vector3.up,p-hands.Shoulder(right));yield return null;}
        hands.Lean(0);hands.Release(right,.4f);if(pose)pose.hasFocus=false;
    }
    IEnumerator CineWalkBob(float seconds){for(float t=0;t<seconds&&!cineSkip;t+=Time.deltaTime){cineRoll=Mathf.Sin(t*7.5f)*.6f;yield return null;}cineRoll=0;}
    void FinishCellCinematic(OpeningActor guard,ShopMechanism door) {
        if(door&&!door.IsOpen&&!door.Moving)door.Use();
        if(guard) {
            guard.StopWalking();guard.transform.position=OpeningPoint(CineGuardAside.x,CineGuardAside.z);FaceGuard(guard,CineGuardAsideFacing);
            guard.GetComponentInChildren<PeriodCharacterPerformance>()?.Listen();
            guard.GetComponent<NpcHands>()?.Stop();var pose=guard.GetComponent<NpcPose>();if(pose)pose.hasFocus=false;
        }
        cineLine=null;cineLids=0;cinePain=0;cineShake=0;cineRoll=0;SetCineBlur(0);cineGuardWalking=false;
        var feet=OpeningPoint(CinePlayerOut.x,CinePlayerOut.z);
        player.Place(feet,guard?CellYaw(feet,guard.transform.position):OpeningYaw(-1,0));player.eye.transform.localPosition=new Vector3(0,1.66f,0);
        cellCinematic=false;cineBars=0;CursorMode();
        BeginEscortToProperty();
        Debug.Log("OPENING_CINEMATIC_END skipped="+cineSkip+" blows="+cineBlows);
    }
    void SkipCellCinematic(){if(cellCinematic)cineSkip=true;}
    // While the eyes find their focus: a depth-of-field blur over everything beyond arm's length.
    void SetCineBlur(float amount) {
        cineBlur=Mathf.Clamp01(amount);
        if(!cineVolume) {
            if(cineBlur<=.001f)return;
            var holder=new GameObject("Cell cinematic focus");holder.transform.SetParent(transform,false);
            cineVolume=holder.AddComponent<Volume>();cineVolume.isGlobal=true;cineVolume.priority=200;cineVolume.weight=1;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            cineFocus=profile.Add<DepthOfField>(true);
            cineFocus.mode.Override(DepthOfFieldMode.Gaussian);cineFocus.gaussianStart.Override(0);cineFocus.highQualitySampling.Override(true);
            cineVolume.sharedProfile=profile;
        }
        cineFocus.gaussianEnd.Override(Mathf.Lerp(14f,.35f,cineBlur));
        cineFocus.gaussianMaxRadius.Override(Mathf.Lerp(.5f,1.5f,cineBlur));
        cineVolume.enabled=cineBlur>.01f;
    }
    // After the motor: the eye is placed in the world and may roll a little (lying, knocking).
    void ApplyCellCinematicEye() {
        if(!cellCinematic||!player)return;
        cineShake=Mathf.MoveTowards(cineShake,0,Time.deltaTime*4);
        float shake=cineShake*cineShake;
        player.transform.rotation=Quaternion.Euler(0,cineYaw+shake*Mathf.Sin(Time.time*61)*1.2f,0);
        player.pitch=cinePitch;
        player.eye.transform.localPosition=player.transform.InverseTransformPoint(cineEye+Vector3.up*shake*.012f*Mathf.Sin(Time.time*47));
        float roll=cineRoll;
        player.eye.transform.localRotation=Quaternion.Euler(cinePitch+shake*Mathf.Sin(Time.time*53)*1.5f,0,roll);
    }
    void DrawCellCinematic() {
        if(!cellCinematic)return;
        if(!cineLidTexture) {
            // Two soft edges: opaque toward the lid, clear toward the opening.
            cineLidTexture=new Texture2D(1,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            cineLidBottom=new Texture2D(1,64,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp};
            for(int i=0;i<64;i++){float a=Mathf.SmoothStep(0,1,i/63f);cineLidTexture.SetPixel(0,i,new Color(0,0,0,a));cineLidBottom.SetPixel(0,i,new Color(0,0,0,1-a));}
            cineLidTexture.Apply();cineLidBottom.Apply();
        }
        // Pain: a dull pulse darkening the view.
        if(cinePain>0){float pulse=.5f+.5f*Mathf.Sin(Time.time*3.1f);Box(new Rect(0,0,W,H),new Color(.18f,.02f,.02f,cinePain*(.08f+.09f*pulse)));}
        float lid=Mathf.Clamp01(cineLids);
        if(lid>0) {
            float edge=H*.2f,h=lid*(H*.5f+edge);
            GUI.color=Color.black;GUI.DrawTexture(new Rect(0,0,W,Mathf.Max(0,h-edge)),solid);GUI.color=Color.white;
            GUI.DrawTexture(new Rect(0,h-edge,W,edge),cineLidTexture);
            GUI.DrawTexture(new Rect(0,H-h,W,edge),cineLidBottom);
            GUI.color=Color.black;GUI.DrawTexture(new Rect(0,H-h+edge,W,Mathf.Max(0,h-edge)),solid);GUI.color=Color.white;
        }
        float bar=H*.11f*cineBars;
        Box(new Rect(0,0,W,bar),Color.black);Box(new Rect(0,H-bar,W,bar),Color.black);
        if(!string.IsNullOrEmpty(cineLine)) {
            var style=new GUIStyle(cineInner?italic:text){alignment=TextAnchor.MiddleCenter,wordWrap=true};
            // Broken where the whole line will break, so words do not jump between lines as it is written out.
            string prefix=cineInner?"":cineSpeaker+"：";
            string shown=Reveal(CjkWrap(prefix+cineLine,style,W*.76f),prefix.Length+Mathf.Clamp(Mathf.CeilToInt(cineReveal),0,cineLine.Length));
            GUI.color=new Color(0,0,0,.6f);Label(new Rect(W*.12f+2,H-bar+2,W*.76f,bar-12),shown,style);
            GUI.color=cineInner?new Color(.82f,.82f,.84f):new Color(.96f,.93f,.86f);Label(new Rect(W*.12f,H-bar,W*.76f,bar-14),shown,style);GUI.color=Color.white;
        }
        var hint=new GUIStyle(small){alignment=TextAnchor.LowerRight};GUI.color=new Color(1,1,1,.45f);
        Label(new Rect(W-300,H-38,270,28),"Esc 跳过",hint);GUI.color=Color.white;
    }
    // The first `visible` characters of a wrapped line; the inserted breaks do not count.
    static string Reveal(string wrapped,int visible){int count=0;for(int i=0;i<wrapped.Length;i++){if(wrapped[i]=='\n')continue;if(count==visible)return wrapped.Substring(0,i);count++;}return wrapped;}
    // ---- Sound: a faint ringing in the ears; the knocks, the key and the door are recorded-style clips ----
    void EnsureCineAudio(){if(!cineAudio){cineAudio=gameObject.AddComponent<AudioSource>();cineAudio.playOnAwake=false;cineAudio.spatialBlend=0;}}
    void PlayCine(AudioClip clip,float volume){if(!clip)return;EnsureCineAudio();cineAudio.PlayOneShot(clip,volume);}
    // A sound where it happens (the guard's fist on the bars, his key in the lock).
    void PlayAt(AudioClip clip,Vector3 at,float volume,float pitch=1) {
        if(!clip)return;
        var holder=new GameObject("Sound · "+clip.name);holder.transform.position=at;
        var source=holder.AddComponent<AudioSource>();source.clip=clip;source.spatialBlend=.8f;source.volume=volume;source.pitch=pitch;
        source.minDistance=1.2f;source.maxDistance=18;source.rolloffMode=AudioRolloffMode.Linear;source.Play();
        Destroy(holder,clip.length/Mathf.Max(.5f,pitch)+.25f);
    }
    static AudioClip tinnitus;
    static AudioClip CineTinnitus() {
        if(tinnitus)return tinnitus;const int rate=44100;var s=new float[rate*4];
        for(int i=0;i<s.Length;i++){float t=i/(float)rate,env=Mathf.Sin(Mathf.PI*Mathf.Clamp01(t/4f));s[i]=(Mathf.Sin(2*Mathf.PI*3150*t)*.06f+Mathf.Sin(2*Mathf.PI*2840*t)*.03f)*env*env;}
        tinnitus=AudioClip.Create("Ringing ears",s.Length,1,rate,false);tinnitus.SetData(s,0);return tinnitus;
    }
}
