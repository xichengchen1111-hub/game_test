using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// The station's people keep to their own daily round instead of standing still: each has a
// few places in the building (a post, a desk, a cabinet, a stove, a bench) with something to
// do there. When one task is done the officer picks another place by weight (never the one
// just left), finds a way there on the town navigation mesh, which knows the walls,
// furniture and doorways, opens a closed door in the way and waits while the player stands
// in the path. Two people meeting in a passage each keep to their right and pass, or the one
// that cannot step aside waits. Sitting down and getting up again are visible movements
// (NpcPose). Talking to an officer stops the round; the officer turns to the player and resumes
// afterwards. Spots are building-local metres, so the round follows the shared police prefab.
public sealed class PoliceStaffRoutine : MonoBehaviour {
    public static bool Enabled=true;
    // Focused reviews shorten every task to watch several rounds in a minute.
    public static float DurationScale=1;
    [Serializable] public class Spot {
        public string name;
        public Vector3 position,approach,face;
        public NpcPose.Activity activity;
        public float minSeconds=8,maxSeconds=16,weight=1,seatHeight;
        // Returning here when a visitor comes near (the duty window).
        public bool post;
    }
    public Spot[] spots=new Spot[0];
    public float walkSpeed=1.15f;
    // Optional visitor area around the post (building-local centre and radius).
    public Vector3 postWatch;public float postRadius;
    enum Phase { Idle, Rising, Walking, Settling }
    Phase phase=Phase.Idle;
    int current=-1,target=-1;
    float timer,blend,blocked;
    Vector3 slideFrom,slideTo;Quaternion turnFrom,turnTo;
    readonly List<Vector3> path=new List<Vector3>();int corner;
    NavMeshPath navPath;
    NpcPose pose;PeriodCharacterPerformance performance;Transform building;
    ShopMechanism[] doors=new ShopMechanism[0];
    System.Random random;
    public string CurrentSpot=>current>=0?spots[current].name:"";
    public bool IsWalking=>phase==Phase.Walking;
    public int Visits{get;private set;}
    public float WalkedMetres{get;private set;}
    public int PathCorners{get;private set;}
    public int DoorsOpened{get;private set;}
    public float MaxSit{get;private set;}
    public bool HasSeat=>Array.Exists(spots,s=>s.activity==NpcPose.Activity.Sit||s.activity==NpcPose.Activity.SitRead||s.activity==NpcPose.Activity.SitWrite);
    public int Sidesteps{get;private set;}
    // For reviews: the last route asked for, and where the officer stands in the round now.
    public string LastPlan{get;private set;}="";
    public string State=>phase+" at "+(building?building.InverseTransformPoint(transform.position).ToString("F2"):"")+" spot="+CurrentSpot+" target="+(target>=0&&target<spots.Length?spots[target].name:"-")+" corner="+corner+"/"+path.Count+" blocked="+blocked.ToString("F1")+" dodge="+dodge.ToString("F2")+" plan="+LastPlan;
    // Everybody else who walks or stands about in the building (the other officers, the guard and
    // the lawyer while they play their parts).
    Transform[] others=new Transform[0];
    float dodge;
    void Start() {
        pose=GetComponent<NpcPose>();performance=GetComponentInChildren<PeriodCharacterPerformance>();
        var station=GetComponentInParent<PoliceStation>();building=station?station.transform:transform.parent;
        if(station)doors=station.GetComponentsInChildren<ShopMechanism>(true);
        var people=new List<Transform>();
        if(station) {
            foreach(var r in station.GetComponentsInChildren<PoliceStaffRoutine>(true))if(r!=this)people.Add(r.transform);
            foreach(var a in station.GetComponentsInChildren<OpeningActor>(true))if(a.transform!=transform&&!people.Contains(a.transform))people.Add(a.transform);
        }
        others=people.ToArray();
        random=new System.Random(name.GetHashCode()^Environment.TickCount);
        // Begin at the authored place doing its task for a moment.
        float best=float.MaxValue;
        for(int i=0;i<spots.Length;i++){float d=Vector3.Distance(World(spots[i].position),transform.position);if(d<best){best=d;current=i;}}
        if(current>=0&&best<.6f)Arrive(current,true);else if(spots.Length>0)Plan(0);
    }
    Vector3 World(Vector3 local)=>building?building.TransformPoint(local):local;
    float Range(float a,float b)=>a+(float)random.NextDouble()*(b-a);
    void Arrive(int index,bool instant) {
        // Remember where he came from before taking up the new place.
        if(current>=0&&current!=index)previous=current;
        current=index;var spot=spots[index];
        if(pose){pose.seatHeight=World(new Vector3(0,spot.seatHeight,0)).y;pose.activity=spot.activity;}
        transform.position=Floor(spot);
        FaceInstant(World(spot.face)-transform.position);
        timer=Range(spot.minSeconds,spot.maxSeconds)*(instant?.5f:1)*DurationScale;phase=Phase.Idle;Visits++;Visited(index);
    }
    void Plan(int index) {
        target=index;var spot=spots[index];
        path.Clear();corner=0;
        var from=transform.position;var to=World(spot.approach==Vector3.zero?spot.position:spot.approach);
        navPath??=new NavMeshPath();
        bool fromOk=NavMesh.SamplePosition(from,out var a,1f,NavMesh.AllAreas),toOk=NavMesh.SamplePosition(to,out var b,1f,NavMesh.AllAreas);
        bool found=fromOk&&toOk&&NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,navPath);
        LastPlan=spot.name+": "+(found?navPath.status+" corners="+navPath.corners.Length:"no path (from "+fromOk+", to "+toOk+")");
        if(found&&navPath.status==NavMeshPathStatus.PathComplete) {
            for(int i=1;i<navPath.corners.Length;i++)path.Add(navPath.corners[i]);
            PathCorners=navPath.corners.Length;
        }
        // Without navigation data (an editor preview) only a short way inside one room is walked directly.
        else if(Vector3.Distance(from,to)<4.5f)path.Add(to);
        else {timer=Range(4,8);phase=Phase.Idle;target=-1;return;}
        path.Add(to);phase=Phase.Walking;blocked=0;
    }
    // How many tasks have been done since each place was last visited.
    int[] waited=new int[0];
    int previous=-1;
    // Utility choice: each place's appeal is its weight, growing the longer it has been left, so over a
    // shift everyone gets round all of their places instead of shuttling between two of them. Neither
    // the place just left nor the one before it is picked again straight away (with three or more).
    float Appeal(int i)=>spots[i].weight*(1+(i<waited.Length?waited[i]:0));
    bool Recent(int i)=>i==current||spots.Length>2&&i==previous;
    int Choose() {
        float total=0;for(int i=0;i<spots.Length;i++)if(!Recent(i))total+=Appeal(i);
        float pick=(float)random.NextDouble()*total;int last=-1;
        for(int i=0;i<spots.Length;i++){if(Recent(i))continue;last=i;pick-=Appeal(i);if(pick<=0)return i;}
        return last>=0?last:current==0&&spots.Length>1?1:0;
    }
    void Visited(int index) {
        if(waited.Length!=spots.Length)Array.Resize(ref waited,spots.Length);
        for(int i=0;i<waited.Length;i++)waited[i]++;
        waited[index]=0;
    }
    // Reviews only: go now to one of the officer's own places and do what is done there.
    public bool GoTo(string spotName) {
        int index=Array.FindIndex(spots,s=>s.name==spotName);if(index<0)return false;
        if(phase==Phase.Walking)Halt();
        if(pose&&pose.activity!=NpcPose.Activity.Stand&&current>=0&&phase==Phase.Idle){target=index;pose.activity=NpcPose.Activity.Stand;var spot=spots[current];slideFrom=transform.position;slideTo=Ground(World(spot.approach==Vector3.zero?spot.position:spot.approach));blend=0;phase=Phase.Rising;}
        else if(phase==Phase.Rising)target=index;
        else{if(pose)pose.activity=NpcPose.Activity.Stand;Plan(index);}
        return true;
    }
    public string SeatSpot{get{var seat=Array.Find(spots,s=>s.activity==NpcPose.Activity.Sit||s.activity==NpcPose.Activity.SitRead||s.activity==NpcPose.Activity.SitWrite);return seat!=null?seat.name:null;}}
    bool PlayerNear(Vector3 local,float radius) {
        var app=DemoApp.Instance;if(!app||!app.player||radius<=0)return false;
        var p=building.InverseTransformPoint(app.player.transform.position);
        return new Vector2(p.x-local.x,p.z-local.z).magnitude<radius&&Mathf.Abs(p.y-local.y)<1.5f;
    }
    void Update() {
        var app=DemoApp.Instance;
        if(!app||!Enabled||spots.Length==0||app.NPCAnimationPaused||!building)return;
        float dt=Time.deltaTime;
        if(pose)MaxSit=Mathf.Max(MaxSit,pose.Sit);
        // In a conversation: stop, and turn toward the player unless seated.
        if(app.IsInDialogueWith(this)||performance&&performance.IsSpeaking) {
            if(phase==Phase.Walking)Halt();
            if(phase!=Phase.Settling&&(!pose||pose.Sit<.05f)){var to=app.player.transform.position-transform.position;to.y=0;if(to.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-to),dt*150);}
            return;
        }
        switch(phase) {
            case Phase.Idle:
                timer-=dt;
                // The duty officer comes back to the window when somebody waits there.
                int post=Array.FindIndex(spots,s=>s.post);
                if(post>=0&&current!=post&&target<0&&PlayerNear(postWatch,postRadius))timer=0;
                if(timer>0)break;
                if(post>=0&&current==post&&PlayerNear(postWatch,postRadius)){timer=2;break;}
                int next=post>=0&&current!=post&&PlayerNear(postWatch,postRadius)?post:Choose();
                target=next;
                if(pose&&pose.activity!=NpcPose.Activity.Stand) {
                    pose.activity=NpcPose.Activity.Stand;
                    var spot=spots[current];slideFrom=transform.position;slideTo=Ground(World(spot.approach==Vector3.zero?spot.position:spot.approach));
                    blend=0;phase=Phase.Rising;
                } else Plan(next);
                break;
            case Phase.Rising:
                blend=Mathf.Min(1,blend+dt/.8f);
                transform.position=Vector3.Lerp(slideFrom,slideTo,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.25f,1,blend)));
                if(blend>=1)Plan(target);
                break;
            case Phase.Walking:Walk(dt);break;
            case Phase.Settling: {
                blend=Mathf.Min(1,blend+dt/.85f);float e=Mathf.SmoothStep(0,1,blend);
                transform.rotation=Quaternion.Slerp(turnFrom,turnTo,Mathf.SmoothStep(0,1,Mathf.Min(1,blend*2.2f)));
                transform.position=Vector3.Lerp(slideFrom,slideTo,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.3f,1,e)));
                if(blend>=.3f&&pose)pose.activity=spots[target].activity;
                if(blend>=1)Arrive(target,false);
                break;
            }
        }
    }
    void Walk(float dt) {
        if(corner>=path.Count){StartSettling();return;}
        var goal=path[corner];var flat=goal-transform.position;flat.y=0;
        // A closed door on the way is opened and the officer waits for the leaf.
        foreach(var door in doors) {
            if(!door||!door.movingPart||!door.protectPlayer)continue;
            var d=door.movingPart.position-transform.position;d.y=0;
            if(d.magnitude>1.5f||flat.sqrMagnitude<.0001f||Vector3.Dot(d.normalized,flat.normalized)<.2f)continue;
            if(!door.IsOpen&&!door.Moving){door.Use();if(door.IsOpen)DoorsOpened++;}
            if(door.IsOpen&&door.OpenFraction<.85f){Pause();return;}
        }
        // Wait for the player standing in the way instead of walking through them.
        var app=DemoApp.Instance;
        if(app&&app.player) {
            var rel=app.player.transform.position-transform.position;rel.y=0;
            if(rel.magnitude<1.0f&&flat.sqrMagnitude>.0001f&&Vector3.Dot(rel.normalized,flat.normalized)>.3f){blocked+=dt;Pause();
                // After a while, pick another task rather than standing there forever.
                if(blocked>6){Halt();timer=Range(2,4);phase=Phase.Idle;}
                return;}
        }
        // Someone else in the way: keep to the right and pass; if a wall or desk leaves no room on the
        // right, carry straight on and wait only when nearly touching. If both have to wait, the one
        // with the lower id gives way and does something else, so two people never stand there forever.
        var dir=flat.sqrMagnitude>.0001f?flat.normalized:-transform.forward;
        float closest=float.MaxValue;Transform blocker=null;
        foreach(var other in others) {
            if(!other||!other.gameObject.activeInHierarchy)continue;
            var rel=other.position-transform.position;rel.y=0;float distance=rel.magnitude;
            if(distance>1.6f||Mathf.Abs(other.position.y-transform.position.y)>1f||distance<.001f)continue;
            float along=Vector3.Dot(rel,dir);if(along<=0)continue;
            float across=Vector3.Dot(rel,Vector3.Cross(Vector3.up,dir));
            // Only someone close to the line ahead matters.
            if(Mathf.Abs(across)<.7f&&along<closest){closest=along;blocker=other;}
        }
        float wanted=closest<1.6f?Mathf.Clamp01((1.6f-closest)/.9f)*.55f:0;
        if(wanted>dodge+.01f&&dodge<.02f)Sidesteps++;
        dodge=Mathf.MoveTowards(dodge,wanted,dt*1.2f);
        if(dodge>.01f) {
            var right=Vector3.Cross(Vector3.up,dir);var aside=(dir+right*dodge*1.6f).normalized;
            if(Clear(transform.position+aside*.25f))dir=aside;
            else if(blocker&&closest<.6f){
                blocked+=dt;Pause();
                if(blocked>(GetInstanceID()<blocker.GetInstanceID()?1.5f:6f)){Halt();timer=Range(2,4);phase=Phase.Idle;}
                return;
            }
        }
        blocked=0;
        // Turning like a person (user, 2026-10-15): a limited rate, a slower walk while the body comes
        // round (past about 60° he stops and steps round where he stands), easing into and out of the
        // walk, and the body already turning toward the next leg as he nears a corner.
        var faceDir=dir;
        if(dodge<.01f&&corner+1<path.Count&&flat.magnitude<.6f){var next=path[corner+1]-goal;next.y=0;if(next.sqrMagnitude>.0001f)faceDir=Vector3.Slerp(dir,next.normalized,(.6f-flat.magnitude)/.6f*.45f).normalized;}
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-faceDir),dt*Mathf.Lerp(240,160,Mathf.Clamp01(pace/Mathf.Max(.1f,walkSpeed))));
        float error=Vector3.Angle(-transform.forward,dir);
        float wantedPace=walkSpeed*Mathf.Clamp01((Mathf.Cos(error*Mathf.Deg2Rad)-.5f)/.5f);
        pace=Mathf.MoveTowards(pace,wantedPace,(wantedPace>pace?2.2f:3.4f)*dt);
        float step=pace*dt;
        if(dodge<.01f&&flat.magnitude<=Mathf.Max(step,.005f)){transform.position=Ground(new Vector3(goal.x,transform.position.y,goal.z));WalkedMetres+=flat.magnitude;corner++;return;}
        // After stepping aside, the corner is reached when close enough rather than exactly.
        if(dodge>=.01f&&flat.magnitude<=.25f){corner++;return;}
        transform.position=Ground(transform.position+dir*step);WalkedMetres+=step;
        if(performance)performance.SetLocomotion(pace>.05f?pace:error>12?.4f:0);
    }
    float pace;
    void StartSettling() {
        pace=0;if(performance)performance.SetWalking(false);
        var spot=spots[target];
        slideFrom=transform.position;slideTo=Floor(spot);
        var face=World(spot.face)-slideTo;face.y=0;if(face.sqrMagnitude<.0001f)face=-transform.forward;
        turnFrom=transform.rotation;turnTo=Quaternion.LookRotation(-face);
        blend=0;phase=Phase.Settling;
    }
    void Pause(){pace=0;if(performance)performance.SetWalking(false);}
    void Halt(){Pause();if(phase==Phase.Walking){phase=Phase.Idle;timer=1.5f;current=-1;target=-1;}}
    void FaceInstant(Vector3 direction){direction.y=0;if(direction.sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(-direction);}
    // Room for the body at a point: nothing solid but people (who step aside themselves) and the player.
    bool Clear(Vector3 feet) {
        foreach(var hit in Physics.OverlapCapsule(feet+Vector3.up*.35f,feet+Vector3.up*1.5f,.22f,~(1<<2),QueryTriggerInteraction.Ignore)) {
            if(hit.transform.IsChildOf(transform)||hit.bounds.max.y<=feet.y+.2f)continue;
            var app=DemoApp.Instance;if(app&&app.player&&hit.transform.IsChildOf(app.player.transform))continue;
            bool person=false;foreach(var other in others)if(other&&hit.transform.IsChildOf(other)){person=true;break;}
            if(!person)return false;
        }
        return true;
    }
    // A seat's own collider must not lift the feet: seated places take the floor height of their approach.
    Vector3 Floor(Spot spot) {
        var p=World(spot.position);
        if(spot.approach==Vector3.zero)return Ground(p);
        p.y=Ground(World(spot.approach)).y;return p;
    }
    // Feet on the floor below, ignoring the officer's own body box.
    Vector3 Ground(Vector3 p) {
        var hits=Physics.RaycastAll(p+Vector3.up*.6f,Vector3.down,1.4f,~(1<<2),QueryTriggerInteraction.Ignore);
        float best=float.NegativeInfinity;
        foreach(var h in hits){if(h.transform.IsChildOf(transform))continue;if(h.point.y>best&&h.point.y<p.y+.45f)best=h.point.y;}
        if(!float.IsNegativeInfinity(best))p.y=best;
        return p;
    }
    void OnDisable(){Pause();}
    // Reviews only: walk at once from one building-local place to another (to stage two people meeting).
    public void StageWalk(Vector3 fromLocal,Vector3 toLocal) {
        var staged=new Spot{name="staged",position=toLocal,face=toLocal+(toLocal-fromLocal).normalized,activity=NpcPose.Activity.Stand,minSeconds=60,maxSeconds=60};
        Array.Resize(ref spots,spots.Length+1);spots[spots.Length-1]=staged;
        if(pose)pose.activity=NpcPose.Activity.Stand;
        transform.position=Ground(World(fromLocal));FaceInstant(World(toLocal)-transform.position);current=-1;Plan(spots.Length-1);
    }
}
