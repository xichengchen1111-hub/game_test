using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Passers-by and traffic for the town (user, 2026-10-15): many people and cars by day, few at
// night; people of every age on the footways, period dress; cars that keep the rules of the road;
// a beggar and a street musician on their pitches. Everything lives in a bubble round the player:
// cars within about 140 m, people within about 90 m, spawned where the player cannot see them
// (behind the camera, behind buildings, or in the fog) and recycled when far. How many there are
// follows the hour and the rain (StreetDensity). Nothing here is saved, nothing gives testimony or
// evidence, and none of it comes out during the first act (it starts once the lawyer has dropped
// the player at the hospital) or in the isolated reviews that measure the empty streets.
public sealed class StreetLife : MonoBehaviour {
    public static StreetLife Current;
    // What the review counts: cars through the signal and the STOP corners, and whether they kept the rules.
    public sealed class TrafficStats {
        public int signalEntries,redRuns,stopEntries,stopViolations,crossings,kerbWaits,yields,honks;
        public void Reset(){signalEntries=redRuns=stopEntries=stopViolations=crossings=kerbWaits=yields=honks=0;}
    }
    public readonly TrafficStats Stats=new TrafficStats();
    public void Entered(AmbientCar car,StreetNet.Lane lane,bool stopped,bool amberGo) {
        if(lane.signalAxis>=0) {
            Stats.signalEntries++;
            if(PeriodTrafficSignal.State(SignalSeconds,lane.signalAxis)==0&&!amberGo){Stats.redRuns++;Debug.LogWarning("STREET_CAR_RED: "+car.name+" at "+car.transform.position);}
        } else if(lane.stopSign) {
            Stats.stopEntries++;
            if(!stopped){Stats.stopViolations++;Debug.LogWarning("STREET_CAR_NO_STOP: "+car.name+" at "+car.transform.position);}
        }
    }
    // Normal play, the street-life review and the showcase recording (DemoApp.Awake).
    public static bool Enabled=true;
    // An unnamed user layer: the people and cars' own colliders, left out of the rain's ground rays
    // and of the startup clearance sweep.
    public const int Layer=9;
    public const int PeopleCap=36,CarCap=10;
    const float PeopleRadius=92,CarRadius=145,MaxStep=.1f;
    // Times AmbientCar's last-resort guard has had to hold a car (each logged as STREET_CAR_GUARD).
    public int GuardHolds{get;private set;}
    public float RoadHeight=>.025f;
    public StreetNet Net{get;private set;}
    readonly List<StreetWalker> walkers=new List<StreetWalker>();
    readonly List<AmbientCar> cars=new List<AmbientCar>();
    readonly Dictionary<StreetNet.Path,List<AmbientCar>> onPath=new Dictionary<StreetNet.Path,List<AmbientCar>>();
    static readonly List<AmbientCar> none=new List<AmbientCar>();
    readonly Dictionary<StreetDensity.Folk,List<GameObject>> folk=new Dictionary<StreetDensity.Folk,List<GameObject>>();
    readonly Dictionary<StreetDensity.Folk,Stack<StreetWalker>> walkerPool=new Dictionary<StreetDensity.Folk,Stack<StreetWalker>>();
    GameObject[] carPrefabs=new GameObject[0];
    readonly Stack<AmbientCar> carPool=new Stack<AmbientCar>();
    GameObject props;
    readonly List<StreetNet.WalkNode> doors=new List<StreetNet.WalkNode>(),ends=new List<StreetNet.WalkNode>();
    readonly List<StreetNet.WalkEdge> spawnEdges=new List<StreetNet.WalkEdge>();float spawnLength;
    System.Random rng;
    float nextCensus,peopleTarget,carTarget;
    Camera eye;Transform player;
    readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
    // Buildings with a real interior: nobody vanishes into those doors.
    static readonly HashSet<string> Interiors=new HashSet<string>{"B01","B02","B04","B07","B10","B11","B12","B17"};
    public int People=>walkers.Count;public int Cars=>cars.Count;
    // Where the player stands, or far away while seated in a car (the dogs keep clear of it).
    public Vector3 PlayerPosition=>player&&DemoApp.Instance&&!DemoApp.Instance.CarSeated?player.position:new Vector3(1e6f,0,1e6f);
    public float PeopleTarget=>peopleTarget;public float CarTarget=>carTarget;
    public IReadOnlyList<StreetWalker> Walkers=>walkers;public IReadOnlyList<AmbientCar> AllCars=>cars;
    public StreetBeggar Beggar{get;private set;}
    public StreetOrgan Organ{get;private set;}
    // For the review: a fixed seed and the counts it asks for.
    public static int Seed=0;
    public float? ForcedPeople,ForcedCars;

    static T Ensure<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();return c?c:go.AddComponent<T>();}
    public static StreetLife Create(Camera camera,Transform playerBody) {
        var go=new GameObject("Street life");
        var life=go.AddComponent<StreetLife>();life.eye=camera;life.player=playerBody;
        life.Initialize();Current=life;return life;
    }
    void OnDestroy(){if(Current==this)Current=null;}
    void Initialize() {
        rng=new System.Random(Seed!=0?Seed:System.Environment.TickCount);
        var period=Resources.Load<TextAsset>("PeriodStreets");
        var watch=System.Diagnostics.Stopwatch.StartNew();
        Net=StreetNet.Build(TownPlan.Current,period?period.text:null);Net.Ground=.1f;
        long buildMs=watch.ElapsedMilliseconds;
        Physics.SyncTransforms();
        Net.CheckClearance((1<<Layer)|(1<<2));
        foreach(var n in Net.walkNodes) {
            if(n.door!=null&&!Interiors.Contains(n.door)&&n.edges.Count>0)doors.Add(n);
            else if(n.door==null&&n.edges.Count==1)ends.Add(n);
        }
        foreach(var e in Net.walkEdges)if(e.crosses==null&&e.a.door==null&&e.b.door==null&&e.length>2){spawnEdges.Add(e);spawnLength+=e.length;}
        foreach(var prefab in Resources.LoadAll<GameObject>("StreetLife/Folk")) {
            var name=prefab.name;int cut=name.IndexOf("__");
            if(cut<0||!System.Enum.TryParse<StreetDensity.Folk>(name.Substring(0,cut),out var kind))continue;
            if(!folk.TryGetValue(kind,out var list))folk[kind]=list=new List<GameObject>();list.Add(prefab);
        }
        carPrefabs=Resources.LoadAll<GameObject>("StreetLife/Cars");
        props=Resources.Load<GameObject>("StreetLife/StreetProps");
        Debug.Log("STREET_LIFE_READY: lanes="+Net.lanes.Count+" turns="+Net.turns.Count+" junctions="+Net.junctions.Count+" walkNodes="+Net.walkNodes.Count+" doors="+doors.Count+" folk="+string.Join(",",folk.Keys)+" cars="+carPrefabs.Length+
            " linesSetBack="+Net.lanes.Count(l=>l.setback>0)+" netBuildMs="+buildMs+" ("+Net.BuildTimes+")");
    }

    // ---- The census: how many there should be now ----
    float Hour=>DemoApp.Instance?DemoApp.Instance.Minutes/60f:12;
    public float Rain=>DayNightCycle.Instance?DayNightCycle.Instance.Weather.rain:0;
    public bool LampsLit=>DayNightCycle.Instance&&DayNightCycle.Instance.Daylight*DayNightCycle.Instance.Weather.naturalLight<.5f;
    public double SignalSeconds=>DemoApp.Instance?DemoApp.Instance.ElapsedGameMinutes/GameClock.GameMinutesPerRealSecond:0;
    // The story car owns the road from the police station to the hospital until it has gone.
    bool CarsAllowed=>DemoApp.Instance&&DemoApp.Instance.StreetCarsAllowed;
    void Update() {
        var app=DemoApp.Instance;
        if(!app||!eye||!player)return;
        if(!app.StreetLifeAllowed){Clear();return;}
        bool frozen=app.GameTimePaused;
        foreach(var c in cars)c.Freeze(frozen);
        if(frozen)return;
        // A long frame (the window resized, a capture, a stall) is taken as a short step, so nobody
        // moves metres at once past what their rules looked at.
        float dt=Mathf.Min(Time.deltaTime,MaxStep);
        if(Time.time>=nextCensus){nextCensus=Time.time+.5f;Census();}
        for(int i=cars.Count-1;i>=0;i--){var c=cars[i];c.Tick(dt);if(c.Gone)RemoveCar(c);}
        for(int i=walkers.Count-1;i>=0;i--){var w=walkers[i];w.Tick(dt);if(w.Gone)RemoveWalker(w);}
        float rain=Rain;
        foreach(var w in walkers) {
            float d=Vector3.Distance(w.transform.position,player.position);
            w.SetDetail(d<78,d<30);
            w.SetUmbrella(rain>.25f&&w.CarriesUmbrella);
        }
        if(Beggar)Beggar.Tick(dt,player.position);
        if(Organ)Organ.Tick(dt);
    }
    void Census() {
        float hour=Hour,rain=Rain;
        peopleTarget=ForcedPeople??Mathf.Round(PeopleCap*StreetDensity.PeopleShare(hour,rain));
        carTarget=CarsAllowed?(ForcedCars??Mathf.Round(CarCap*StreetDensity.CarShare(hour,rain))):0;
        // Recycle what is far, then fill up a few at a time where nobody is looking.
        for(int i=walkers.Count-1;i>=0;i--){var w=walkers[i];float d=Distance(w.transform.position);if(d>PeopleRadius+8||walkers.Count>peopleTarget+2&&d>40&&Unseen(w.transform.position))RemoveWalker(w);}
        for(int i=cars.Count-1;i>=0;i--){var c=cars[i];float d=Distance(c.transform.position);if(d>CarRadius+10||c.StoppedFor>45&&Unseen(c.transform.position)||cars.Count>carTarget+1&&d>60&&Unseen(c.transform.position))RemoveCar(c);}
        if(!CarsAllowed)for(int i=cars.Count-1;i>=0;i--)RemoveCar(cars[i]);
        for(int k=0;k<3&&walkers.Count<peopleTarget;k++)if(!SpawnWalker())break;
        for(int k=0;k<2&&cars.Count<carTarget;k++)if(!SpawnCar())break;
        UpdatePitches(hour,rain);
    }
    void Clear() {
        for(int i=walkers.Count-1;i>=0;i--)RemoveWalker(walkers[i]);
        for(int i=cars.Count-1;i>=0;i--)RemoveCar(cars[i]);
        if(Beggar){Destroy(Beggar.gameObject);Beggar=null;}if(Organ){Destroy(Organ.gameObject);Organ=null;}
    }
    float Distance(Vector3 p){var a=p-player.position;a.y=0;return a.magnitude;}
    public bool NearPlayer(Vector3 p,float metres)=>player&&Distance(p)<metres;
    // Out of the camera's view, behind something solid, or deep in the fog.
    readonly Plane[] planes=new Plane[6];int planesFrame=-1;
    public bool Unseen(Vector3 p) {
        float d=Distance(p);if(d>85)return true;
        if(planesFrame!=Time.frameCount){GeometryUtility.CalculateFrustumPlanes(eye,planes);planesFrame=Time.frameCount;}
        if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(p+Vector3.up*1,new Vector3(5,2.4f,5))))return true;
        var from=eye.transform.position;var to=p+Vector3.up*1.4f;
        return Physics.Linecast(from,to,~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore);
    }

    // ---- People ----
    StreetDensity.Folk PickKind(float hour,float rain) {
        float total=0;var weights=new List<(StreetDensity.Folk,float)>();
        foreach(var kind in folk.Keys){if(kind==StreetDensity.Folk.Beggar||kind==StreetDensity.Folk.Musician)continue;float w=StreetDensity.Weight(kind,hour,rain);if(w>0){weights.Add((kind,w));total+=w;}}
        if(total<=0)return StreetDensity.Folk.Man;
        float pick=(float)rng.NextDouble()*total;foreach(var (k,w) in weights){pick-=w;if(pick<=0)return k;}
        return weights[weights.Count-1].Item1;
    }
    static bool Grown(StreetDensity.Folk kind)=>kind!=StreetDensity.Folk.Boy&&kind!=StreetDensity.Folk.Girl;
    StreetWalker TakeWalker(StreetDensity.Folk kind) {
        if(!folk.TryGetValue(kind,out var prefabs)||prefabs.Count==0)return null;
        if(walkerPool.TryGetValue(kind,out var pool)&&pool.Count>0){var w=pool.Pop();w.gameObject.SetActive(true);return w;}
        var prefab=prefabs[rng.Next(prefabs.Count)];
        var go=Instantiate(prefab,transform);go.name=prefab.name;
        foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        var walker=go.GetComponent<StreetWalker>();if(!walker)walker=go.AddComponent<StreetWalker>();
        walker.kind=kind;
        if(!walker.performance)walker.performance=go.GetComponentInChildren<PeriodCharacterPerformance>();
        if(!walker.hands)walker.hands=Ensure<NpcHands>(go);
        foreach(var t in go.GetComponentsInChildren<Transform>(true)){if(t.name=="Spine"&&!walker.spine)walker.spine=t;if(t.name=="Neck"&&!walker.neck)walker.neck=t;}
        var body=Ensure<CapsuleCollider>(go);body.radius=.25f;body.height=1.7f;body.center=new Vector3(0,.85f,0);
        var rb=Ensure<Rigidbody>(go);rb.isKinematic=true;rb.useGravity=false;rb.interpolation=RigidbodyInterpolation.None;
        return walker;
    }
    bool SpawnWalker() {
        if(spawnEdges.Count==0||folk.Count==0)return false;
        float hour=Hour,rain=Rain;var kind=PickKind(hour,rain);
        // Out of a door now and then (where nobody is close), otherwise somewhere along a footway out of sight.
        Vector3 at=default,facing=Vector3.forward;StreetNet.WalkNode start=null;bool found=false;
        if(rng.NextDouble()<.3&&doors.Count>0) {
            for(int tries=0;tries<6&&!found;tries++){var d=doors[rng.Next(doors.Count)];float dist=Distance(d.point);if(dist>14&&dist<PeopleRadius-10&&(dist>26||Unseen(d.point))&&Free(d.point,null)){start=d;at=d.point;facing=d.edges[0].Other(d).point-d.point;found=true;}}
        }
        for(int tries=0;tries<10&&!found;tries++) {
            var e=PickEdge();float t=(float)rng.NextDouble();var p=Vector3.Lerp(e.a.point,e.b.point,t);float dist=Distance(p);
            if(dist<30||dist>PeopleRadius-6||!Unseen(p))continue;
            bool forward=rng.NextDouble()<.5;var dir=(forward?e.b.point-e.a.point:e.a.point-e.b.point).normalized;
            var right=Vector3.Cross(Vector3.up,dir);float off=e.clear.Length>0?e.clear[0]*(forward?1:-1):0;p+=right*off;
            if(!Free(p,null))continue;
            at=p;facing=dir;start=forward?e.b:e.a;found=true;
        }
        if(!found)return false;
        var walker=TakeWalker(kind);if(!walker)return false;
        StreetDensity.Gait(kind,rng,out walker.speed,out walker.stoop);
        at.y=GroundAt(at,at.y+.2f);
        walker.Begin(this,rng,at,facing);walkers.Add(walker);
        // Two acquaintances stopped for a word, in the day (grown-ups; the children run their errands).
        if(hour>8&&hour<18&&rain<.3f&&start.door==null&&Grown(kind)&&rng.NextDouble()<.12&&walkers.Count+1<peopleTarget) {
            var partnerKind=PickKind(hour,rain);for(int tries=0;tries<4&&!Grown(partnerKind);tries++)partnerKind=PickKind(hour,rain);
            var other=Grown(partnerKind)?TakeWalker(partnerKind):null;
            if(other) {
                StreetDensity.Gait(other.kind,rng,out other.speed,out other.stoop);
                var side=Vector3.Cross(Vector3.up,facing.normalized);var q=at+facing.normalized*1.1f;q.y=GroundAt(q,at.y+.2f);
                if(Free(q,walker)){other.Begin(this,rng,q,-facing);walkers.Add(other);float talk=15+30*(float)rng.NextDouble();walker.Chat(other,talk,facing);other.Chat(walker,talk+1,-facing);return true;}
                ReturnWalker(other);
            }
        }
        Route(walker,start);
        return true;
    }
    // For the review and the showcase: one walker of a kind on the footway near a point (at least
    // `away` metres from it), walking on from there.
    public StreetWalker SpawnKind(StreetDensity.Folk kind,Vector3 near,float away=4,float within=16) {
        if(!folk.ContainsKey(kind))return null;
        var options=new List<(StreetNet.WalkEdge e,float t,float d)>();
        foreach(var e in spawnEdges) {
            var ab=e.b.point-e.a.point;float len2=Mathf.Max(1e-4f,ab.sqrMagnitude);
            float t=Mathf.Clamp01(Vector3.Dot(near-e.a.point,ab)/len2);
            for(int k=-2;k<=2;k++) {
                float u=Mathf.Clamp01(t+k*1.5f/Mathf.Sqrt(len2));var q=Vector3.Lerp(e.a.point,e.b.point,u);var d=q-near;d.y=0;float dist=d.magnitude;
                if(dist>=away&&dist<=within)options.Add((e,u,dist));
            }
        }
        foreach(var (e,u,_) in options.OrderBy(o=>o.d)) {
            bool forward=rng.NextDouble()<.5;var dir=(forward?e.b.point-e.a.point:e.a.point-e.b.point).normalized;
            var right=Vector3.Cross(Vector3.up,dir);float off=e.clear.Length>0?e.clear[0]*(forward?1:-1):0;
            var p=Vector3.Lerp(e.a.point,e.b.point,u)+right*off;
            if(!Free(p,null))continue;
            var walker=TakeWalker(kind);if(!walker)return null;
            StreetDensity.Gait(kind,rng,out walker.speed,out walker.stoop);
            p.y=GroundAt(p,p.y+.2f);walker.Begin(this,rng,p,dir);walkers.Add(walker);Route(walker,forward?e.b:e.a);
            return walker;
        }
        return null;
    }
    StreetNet.WalkEdge PickEdge() {
        float pick=(float)rng.NextDouble()*spawnLength;foreach(var e in spawnEdges){pick-=e.length;if(pick<=0)return e;}
        return spawnEdges[spawnEdges.Count-1];
    }
    // Somewhere to go: a door (home, a shop), or out of town along a street.
    void Route(StreetWalker walker,StreetNet.WalkNode from) {
        for(int tries=0;tries<6;tries++) {
            StreetNet.WalkNode goal;
            if(rng.NextDouble()<.68&&doors.Count>0)goal=doors[rng.Next(doors.Count)];
            else goal=ends.Count>0?ends[rng.Next(ends.Count)]:Net.walkNodes[rng.Next(Net.walkNodes.Count)];
            if(goal==from||Vector3.Distance(goal.point,from.point)<25)continue;
            var route=Path(from,goal);
            if(route==null)continue;
            route.Insert(0,new StreetNet.WalkNode{point=walker.transform.position});
            // The first stretch runs from where the walker stands to the route's first node.
            walker.Follow(Stitch(route),goal);return;
        }
        walker.Despawn();
    }
    // Walkers start mid-edge: make the first leg a plain stretch to the first node.
    List<StreetNet.WalkNode> Stitch(List<StreetNet.WalkNode> route) {
        var first=route[0];var second=route[1];
        if(first.edges.Count==0){var link=new StreetNet.WalkEdge{a=first,b=second,length=Vector3.Distance(first.point,second.point),width=1.4f,clear=new[]{0f}};first.edges.Add(link);}
        return route;
    }
    public void Replan(StreetWalker walker) {
        var at=walker.transform.position;var node=Nearest(at,walker);
        if(node==null){if(Unseen(at))walker.Despawn();return;}
        Route(walker,node);
    }
    // The nearest node reachable in a straight line without walking across a street or through anything.
    StreetNet.WalkNode Nearest(Vector3 p,StreetWalker walker) {
        StreetNet.WalkNode best=null;float bestD=40*40;
        foreach(var n in Net.walkNodes) {
            if(n.edges.Count==0||n.door!=null)continue;float d=(n.point-p).sqrMagnitude;
            if(d>=bestD||CrossesCarriage(p,n.point))continue;
            if(Physics.Linecast(p+Vector3.up*1,n.point+Vector3.up*1,~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore))continue;
            bestD=d;best=n;
        }
        return best;
    }
    public bool OnCarriage(Vector3 p) {
        foreach(var c in Net.carriages) {
            bool over=c.eastWest?Mathf.Abs(p.z-c.centre)<c.half&&p.x>=c.from&&p.x<=c.to:Mathf.Abs(p.x-c.centre)<c.half&&p.z>=c.from&&p.z<=c.to;
            if(over)return true;
        }
        return false;
    }
    bool CrossesCarriage(Vector3 a,Vector3 b) {
        float length=Vector3.Distance(a,b);int steps=Mathf.CeilToInt(length/.5f);
        for(int i=0;i<=steps;i++)if(OnCarriage(Vector3.Lerp(a,b,i/(float)Mathf.Max(1,steps))))return true;
        return false;
    }
    public void Arrived(StreetWalker walker,StreetNet.WalkNode goal) {
        // Into the house or shop, or away out of town, where nobody sees them go.
        float d=Distance(walker.transform.position);
        if(d>14&&Unseen(walker.transform.position)||d>32){walker.Despawn();return;}
        if(goal!=null&&goal.door!=null){walker.Pause(2.5f,goal.point-walker.transform.position,"doorstep");return;}
        Replan(walker);
    }
    void RemoveWalker(StreetWalker w){walkers.Remove(w);if(w.partner&&w.partner.partner==w)w.partner.partner=null;ReturnWalker(w);}
    void ReturnWalker(StreetWalker w) {
        w.Despawn();w.gameObject.SetActive(false);
        if(!walkerPool.TryGetValue(w.kind,out var pool))walkerPool[w.kind]=pool=new Stack<StreetWalker>();
        pool.Push(w);
    }
    // A*, on the footway graph.
    readonly Dictionary<StreetNet.WalkNode,float> gScore=new Dictionary<StreetNet.WalkNode,float>();
    readonly Dictionary<StreetNet.WalkNode,StreetNet.WalkNode> cameFrom=new Dictionary<StreetNet.WalkNode,StreetNet.WalkNode>();
    public List<StreetNet.WalkNode> Path(StreetNet.WalkNode from,StreetNet.WalkNode to) {
        gScore.Clear();cameFrom.Clear();
        var open=new List<(StreetNet.WalkNode node,float f)>{(from,0)};gScore[from]=0;
        int guard=0;
        while(open.Count>0&&guard++<4000) {
            int best=0;for(int i=1;i<open.Count;i++)if(open[i].f<open[best].f)best=i;
            var current=open[best].node;open.RemoveAt(best);
            if(current==to) {
                var route=new List<StreetNet.WalkNode>{to};
                while(cameFrom.TryGetValue(route[0],out var prev))route.Insert(0,prev);
                return route;
            }
            foreach(var e in current.edges) {
                var n=e.Other(current);
                // Door links are only for the house it belongs to.
                if(n.door!=null&&n!=to)continue;
                float g=gScore[current]+e.length*(e.crosses!=null?1.4f:1);
                if(gScore.TryGetValue(n,out var known)&&g>=known)continue;
                gScore[n]=g;cameFrom[n]=current;open.Add((n,g+Vector3.Distance(n.point,to.point)));
            }
        }
        return null;
    }
    // ---- Shared queries for the walkers ----
    public struct Neighbour { public Vector3 position,velocity;public bool player; }
    readonly List<Neighbour> neighbours=new List<Neighbour>();
    public List<Neighbour> Neighbours(Vector3 p,float radius,StreetWalker me) {
        neighbours.Clear();
        foreach(var w in walkers) {
            if(w==me||w.Gone)continue;var d=w.transform.position-p;d.y=0;if(d.sqrMagnitude<radius*radius)neighbours.Add(new Neighbour{position=w.transform.position,velocity=w.Velocity});
            // Somebody's dog is in the way too.
            if(w.dog){d=w.dog.Position-p;d.y=0;if(d.sqrMagnitude<radius*radius)neighbours.Add(new Neighbour{position=w.dog.Position,velocity=w.Velocity});}
        }
        var app=DemoApp.Instance;
        if(app&&player&&!app.CarSeated){var d=player.position-p;d.y=0;if(d.sqrMagnitude<radius*radius)neighbours.Add(new Neighbour{position=player.position,velocity=Vector3.zero,player=true});}
        return neighbours;
    }
    readonly Collider[] overlap=new Collider[16];
    // Whether a walker may stand at p: clear of the player, the other walkers and anything solid.
    public bool Free(Vector3 p,StreetWalker me) {
        var app=DemoApp.Instance;
        if(player&&app&&!app.CarSeated){var d=player.position-p;d.y=0;if(d.sqrMagnitude<.58f*.58f)return false;}
        foreach(var w in walkers) {
            if(w==me||w.Gone)continue;
            if(w.dog){var g=w.dog.Position-p;g.y=0;if(g.sqrMagnitude<.42f*.42f)return false;}
            if(me&&w==me.partner)continue;var d=w.transform.position-p;d.y=0;if(d.sqrMagnitude<.5f*.5f)return false;
        }
        // A body's width (0.25 m) and a hand's breadth from any car.
        foreach(var c in cars) {
            var rel=p-c.transform.position;rel.y=0;if(rel.sqrMagnitude>16)continue;
            var local=c.transform.InverseTransformPoint(p);local.z+=.17f;
            float dx=Mathf.Max(0,Mathf.Abs(local.x)-.89f),dz=Mathf.Max(0,Mathf.Abs(local.z)-2.15f);
            if(dx*dx+dz*dz<.33f*.33f)return false;
        }
        int n=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.45f,p+Vector3.up*1.55f,.2f,overlap,~(1<<2),QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++){var c=overlap[i];if(me&&c.transform.IsChildOf(me.transform))continue;if(c.GetComponentInParent<StreetWalker>())continue;return false;}
        return true;
    }
    // Whether a dog may stand at p: clear of everybody but its owner, of other dogs and of the player.
    public bool DogRoom(Vector3 p,StreetWalker owner) {
        var pl=PlayerPosition-p;pl.y=0;if(pl.sqrMagnitude<.5f*.5f)return false;
        foreach(var w in walkers) {
            if(w==owner||w.Gone)continue;
            var d=w.transform.position-p;d.y=0;if(d.sqrMagnitude<.48f*.48f)return false;
            if(w.dog){var g=w.dog.Position-p;g.y=0;if(g.sqrMagnitude<.45f*.45f)return false;}
        }
        return true;
    }
    readonly RaycastHit[] ground=new RaycastHit[8];
    public float GroundAt(Vector3 p,float fallback) {
        int n=Physics.RaycastNonAlloc(new Vector3(p.x,fallback+.6f,p.z),Vector3.down,ground,1.6f,~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore);
        float best=float.NegativeInfinity;
        for(int i=0;i<n;i++)if(ground[i].point.y>best&&ground[i].point.y<fallback+.35f)best=ground[i].point.y;
        return float.IsNegativeInfinity(best)?fallback:best;
    }
    // At the kerb: at the signal, with the parallel green and time to get over; elsewhere a gap in the
    // cars long enough to cross at walking pace, with a margin.
    public bool CanCross(StreetNet.WalkEdge edge,Vector3 from,Vector3 to,StreetWalker walker) {
        float width=Vector3.Distance(from,to),needed=width/Mathf.Max(.5f,walker.speed*1.12f);
        if(edge.junction!=null&&edge.junction.signal) {
            // Crossing the east–west street goes with the north–south green, and the other way round.
            int axis=edge.crosses.eastWest?0:1;double t=(SignalSeconds%58+58)%58;
            double left=axis==0?24-t:53-t;
            if(PeriodTrafficSignal.State(SignalSeconds,axis)!=2||left<needed+1.5)return false;
        }
        foreach(var c in cars) {
            // A car standing at its line (the red, a STOP sign, giving way) is no danger: it waits, and it
            // gives way to whoever is on the crossing when it moves off.
            if(c.speed<.5f)continue;
            // Only a car whose way actually passes over this crossing (straight on, or turning across it)
            // matters; one going straight along the other street never comes near.
            float along=c.RouteDistanceTo(from,to,48);
            if(along<0)continue;
            if(along<4.5f)return false;
            if(along/Mathf.Max(.6f,c.speed)<needed+2.5f)return false;
        }
        return true;
    }
    public string ChatLine(System.Random r)=>Chat[r.Next(Chat.Length)];
    // Lines only for the mouth (never shown): ordinary small talk.
    static readonly string[] Chat={"是啊，是啊，可不是嘛。","您家里都还好吧？","我正要去铺子里看看。","回头再细说，我先走了。","这阵子活儿不好找啊。"};

    // ---- Cars ----
    AmbientCar TakeCar() {
        if(carPool.Count>0){var c=carPool.Pop();c.gameObject.SetActive(true);return c;}
        if(carPrefabs.Length==0)return null;
        var prefab=carPrefabs[rng.Next(carPrefabs.Length)];
        var go=Instantiate(prefab,transform);go.name=prefab.name;
        foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        var car=Ensure<AmbientCar>(go);
        var rb=Ensure<Rigidbody>(go);rb.isKinematic=true;rb.useGravity=false;
        car.engine=Source(go.transform,"Ambient engine",CarSounds.SharedEngine,new Vector3(0,.8f,1.5f),.32f);
        car.road=Source(go.transform,"Ambient tyres",CarSounds.SharedRoad,new Vector3(0,.3f,0),0);
        car.horn=Source(go.transform,"Ambient horn",CarSounds.SharedHorn,new Vector3(0,.8f,1.9f),0);
        return car;
    }
    AudioSource Source(Transform parent,string name,AudioClip clip,Vector3 local,float volume) {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=local;
        var a=g.AddComponent<AudioSource>();a.clip=clip;a.loop=true;a.playOnAwake=false;a.volume=volume;a.spatialBlend=1;
        a.rolloffMode=AudioRolloffMode.Linear;a.minDistance=3;a.maxDistance=42;a.dopplerLevel=0;a.priority=150;
        if(clip)a.Play();return a;
    }
    // A paint each car keeps (1931 cars were mostly black; dark blue, green, maroon and grey besides).
    static readonly Color[] Paints={new Color(.09f,.09f,.085f),new Color(.09f,.09f,.085f),new Color(.12f,.16f,.24f),new Color(.14f,.2f,.14f),new Color(.3f,.12f,.1f),new Color(.36f,.35f,.32f)};
    readonly Dictionary<int,Material> paints=new Dictionary<int,Material>();
    void Paint(AmbientCar car) {
        int index=rng.Next(Paints.Length);
        foreach(var r in car.GetComponentsInChildren<MeshRenderer>(true)) {
            var mats=r.sharedMaterials;bool changed=false;
            for(int i=0;i<mats.Length;i++) {
                if(!mats[i]||!mats[i].name.StartsWith("CarPaint"))continue;
                if(!paints.TryGetValue(index,out var m)){m=new Material(mats[i]){name="CarPaint street "+index};m.SetColor("_BaseColor",Paints[index]);paints[index]=m;}
                mats[i]=m;changed=true;
            }
            if(changed)r.sharedMaterials=mats;
        }
    }
    bool SpawnCar() {
        if(Net.lanes.Count==0)return false;
        for(int tries=0;tries<12;tries++) {
            var lane=Net.lanes[rng.Next(Net.lanes.Count)];
            // Most of the traffic on the east–west streets.
            if(!lane.carriage.eastWest&&rng.NextDouble()<.55)continue;
            float s=(float)rng.NextDouble()*Mathf.Max(0,lane.length-12)+2;
            var p=lane.Point(s);float d=Distance(p);
            if(d<62||d>CarRadius-10||!Unseen(p))continue;
            bool clear=true;foreach(var c in CarsOn(lane))if(Mathf.Abs(c.s-s)<16)clear=false;
            // Nor on top of a car on another way (one turning out of the corner beside this lane).
            foreach(var c in cars)if(AmbientCar.BoxGap(p,lane.dir,c.transform.position,c.transform.forward)<1)clear=false;
            foreach(var w in walkers){var near=w.transform.position-p;near.y=0;if(near.sqrMagnitude<9)clear=false;}
            if(player){var near=player.position-p;near.y=0;if(near.sqrMagnitude<16)clear=false;}
            if(!clear||Physics.CheckBox(p+Vector3.up*.95f,new Vector3(1,.75f,2.4f),Quaternion.LookRotation(lane.dir),~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore))continue;
            var car=TakeCar();if(!car)return false;
            Paint(car);
            car.Place(this,lane,s,rng,StreetDensity.SpeedLimit(lane.carriage)*.75f);
            cars.Add(car);Add(car,lane);
            return true;
        }
        return false;
    }
    // For the review: a car at a given place on a lane.
    public AmbientCar SpawnCarOn(StreetNet.Lane lane,float s,float speed) {
        var car=TakeCar();if(!car)return null;Paint(car);
        car.Place(this,lane,s,rng,speed);cars.Add(car);Add(car,lane);return car;
    }
    void RemoveCar(AmbientCar c) {
        cars.Remove(c);if(c.path!=null&&onPath.TryGetValue(c.path,out var list))list.Remove(c);
        c.gameObject.SetActive(false);carPool.Push(c);
    }
    void Add(AmbientCar c,StreetNet.Path p){if(!onPath.TryGetValue(p,out var list))onPath[p]=list=new List<AmbientCar>();list.Add(c);}
    public List<AmbientCar> CarsOn(StreetNet.Path p)=>onPath.TryGetValue(p,out var list)?list:none;
    public void Move(AmbientCar c,StreetNet.Path from,StreetNet.Path to){if(onPath.TryGetValue(from,out var list))list.Remove(c);Add(c,to);}
    public void Honked(AmbientCar c){Stats.honks++;Debug.Log("STREET_CAR_HONK: "+c.name);}
    public void Guarded(AmbientCar c,AmbientCar by,float now,float then) {
        GuardHolds++;var f=System.Globalization.CultureInfo.InvariantCulture;
        Debug.LogWarning("STREET_CAR_GUARD: "+c.Describe()+" | held clear of "+by.Describe()+" | gap "+now.ToString("0.000",f)+" -> "+then.ToString("0.000",f));
    }
    // Cars coming the other way into the same junction (or already turning across it).
    public bool OncomingWithin(StreetNet.Lane lane,float metres) {
        var j=lane.toJunction;if(j==null)return false;
        foreach(var other in j.inbound) {
            if(Vector3.Dot(other.dir,lane.dir)>-.9f)continue;
            foreach(var c in CarsOn(other)) {
                float toLine=other.length-c.s-AmbientCar.Front;
                if(toLine>metres)continue;
                // Waiting to turn left as well: the one that came first goes.
                var t=c.NextTurn;if(c.speed<.3f&&t!=null&&t.kind==2)continue;
                if(c.speed>.3f||toLine<4)return true;
            }
        }
        foreach(var t in Net.turns)if(t.junction==j&&Vector3.Dot(t.from.dir,lane.dir)<-.9f&&CarsOn(t).Count>0)return true;
        return false;
    }
    // Someone is in the junction on a path that crosses mine, or has pulled over their line onto one (as
    // good as in it), or stands where my way through would touch them (just come out of it along another
    // street, say); or my way out of it is full.
    public bool JunctionBusy(StreetNet.Junction j,AmbientCar me,StreetNet.Turn next) {
        foreach(var t in Net.turns) {
            if(t.junction!=j)continue;
            foreach(var c in CarsOn(t)){if(c==me)continue;if(next==null||next.conflicts.Contains(t))return true;}
        }
        foreach(var lane in j.inbound)foreach(var c in CarsOn(lane)) {
            if(c==me||!c.PastLine)continue;var theirs=c.NextTurn;
            if(theirs!=null&&(next==null||next.conflicts.Contains(theirs)))return true;
        }
        if(next==null)return false;
        foreach(var c in CarsOn(next.to))if(c.s<6&&c.speed<1)return true;
        // The ones going my way out I follow instead; those waiting behind their lines stand clear of
        // every way through by construction (StreetNet.SetBackLines).
        foreach(var lane in j.outbound) {
            if(lane==next.to)continue;
            foreach(var c in CarsOn(lane))if(next.Clearance(c.transform.position,c.transform.forward)<StreetNet.Clear)return true;
        }
        return false;
    }
    // Through traffic arriving within the given seconds on a path that crosses mine.
    public bool ThroughTrafficWithin(StreetNet.Junction j,AmbientCar me,float seconds) {
        var mine=me.NextTurn;
        foreach(var lane in j.inbound) {
            if(lane.stopSign)continue;
            foreach(var c in CarsOn(lane)) {
                float toLine=lane.length-c.s-AmbientCar.Front;if(toLine>55)continue;
                var theirs=c.NextTurn as StreetNet.Turn;if(mine!=null&&theirs!=null&&!mine.conflicts.Contains(theirs))continue;
                if(toLine/Mathf.Max(.5f,c.speed)<seconds||toLine<8)return true;
            }
        }
        return false;
    }
    // Two cars stopped at the STOP signs: the one that stopped first goes; arriving together, the one on the right.
    public bool YieldToRight(StreetNet.Junction j,AmbientCar me) {
        var mine=me.NextTurn;
        foreach(var lane in j.inbound) {
            if(!lane.stopSign)continue;
            foreach(var c in CarsOn(lane)) {
                if(c==me||!c.StopDone||lane.length-c.s-AmbientCar.Front>3)continue;
                var theirs=c.NextTurn;if(mine!=null&&theirs!=null&&!mine.conflicts.Contains(theirs))continue;
                if(c.StopDoneAt<me.StopDoneAt-.8f)return true;
                if(Mathf.Abs(c.StopDoneAt-me.StopDoneAt)<=.8f&&Vector3.Cross(me.transform.forward,c.transform.position-me.transform.position).y>0)return true;
            }
        }
        return false;
    }
    // A car coming the other way close ahead on the same narrow street.
    public bool Meeting(AmbientCar me,float metres) {
        var mine=me.Carriage;if(mine==null)return false;
        foreach(var c in cars) {
            if(c==me||c.Carriage!=mine)continue;
            var rel=c.transform.position-me.transform.position;
            if(Vector3.Dot(c.transform.forward,me.transform.forward)<-.5f&&Vector3.Dot(rel,me.transform.forward)>0&&rel.magnitude<metres)return true;
        }
        return false;
    }
    // Free distance from the car's front to whoever stands in its way along the planned paths.
    readonly List<(Vector3 p,float r,bool player)> obstacles=new List<(Vector3,float,bool)>();
    public float ObstacleAhead(AmbientCar car,List<StreetNet.Path> paths,float s,float look,out bool isPlayer) {
        isPlayer=false;obstacles.Clear();var me=car.transform.position;float reach=look+6;
        foreach(var w in walkers){var d=w.transform.position-me;d.y=0;if(d.sqrMagnitude<reach*reach)obstacles.Add((w.transform.position,.32f,false));}
        var app=DemoApp.Instance;
        if(app&&player&&!app.CarSeated){var d=player.position-me;d.y=0;if(d.sqrMagnitude<reach*reach)obstacles.Add((player.position,.35f,true));}
        foreach(var other in OtherVehicles()){var d=other.position-me;d.y=0;if(d.sqrMagnitude<(reach+3)*(reach+3)){var f=other.forward;foreach(float k in new[]{-1.6f,0,1.6f})obstacles.Add((other.position+f*k,1.05f,other==PlayerCar));}}
        if(obstacles.Count==0)return float.MaxValue;
        float best=float.MaxValue;
        // Walk the corridor in steps from the car's middle forward.
        int index=0;float sOn=s,along=0;
        while(along<=look+2&&index<paths.Count) {
            var p=paths[index];
            if(sOn>p.length){sOn-=p.length;index++;continue;}
            var point=p.Point(sOn);
            foreach(var (q,r,isP) in obstacles) {
                var d=q-point;d.y=0;
                if(d.magnitude<car.width/2+r+.35f) {
                    float gap=along-1.98f-r-.9f;
                    if(along>=.6f&&gap<best){best=gap;isPlayer=isP;}
                }
            }
            if(best<float.MaxValue)break;
            sOn+=.75f;along+=.75f;
        }
        return best;
    }
    Transform PlayerCar=>HotelCar.Current?HotelCar.Current.transform:null;
    IEnumerable<Transform> OtherVehicles() {
        if(HotelCar.Current)yield return HotelCar.Current.transform;
        var ride=DemoApp.Instance?DemoApp.Instance.RideCarTransform:null;if(ride)yield return ride;
    }

    // ---- Props (built in the editor: StreetLifeAuthoring) ----
    public Transform MakeUmbrella(Transform owner) {
        Transform u=null;
        var source=props?props.transform.Find("Umbrella"):null;
        if(source)u=Instantiate(source.gameObject,owner).transform;
        else {
            var root=new GameObject("Umbrella");root.transform.SetParent(owner,false);
            var canopy=GameObject.CreatePrimitive(PrimitiveType.Sphere);canopy.transform.SetParent(root.transform,false);canopy.transform.localPosition=new Vector3(0,.64f,0);canopy.transform.localScale=new Vector3(1.05f,.32f,1.05f);
            var stick=GameObject.CreatePrimitive(PrimitiveType.Cylinder);stick.transform.SetParent(root.transform,false);stick.transform.localPosition=new Vector3(0,.235f,0);stick.transform.localScale=new Vector3(.02f,.49f,.02f);
            foreach(var c in root.GetComponentsInChildren<Collider>())Destroy(c);
            canopy.GetComponent<Renderer>().sharedMaterial=Material(new Color(.06f,.06f,.065f));stick.GetComponent<Renderer>().sharedMaterial=Material(new Color(.2f,.14f,.09f));
            u=root.transform;
        }
        u.name="Umbrella";foreach(var t in u.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
        foreach(var r in u.GetComponentsInChildren<Renderer>(true))r.renderingLayerMask=1;
        return u;
    }
    public GameObject Prop(string name){var source=props?props.transform.Find(name):null;return source?Instantiate(source.gameObject):null;}
    public Material Material(Color colour) {
        if(materials.TryGetValue(colour,out var m)&&m)return m;
        m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Street prop "+colour};m.SetColor("_BaseColor",colour);m.SetFloat("_Smoothness",.25f);
        materials[colour]=m;return m;
    }

    // ---- Pitches: the beggar and the organ grinder keep their hours and places ----
    void UpdatePitches(float hour,float rain) {
        bool beggarHours=hour>=7.5f&&hour<18.5f&&rain<.55f;
        if(beggarHours&&!Beggar&&folk.ContainsKey(StreetDensity.Folk.Beggar))TryPlaceBeggar();
        else if(!beggarHours&&Beggar&&Unseen(Beggar.transform.position)){Destroy(Beggar.gameObject);Beggar=null;}
        bool organHours=hour>=9.5f&&hour<17.5f&&rain<.15f;
        if(organHours&&!Organ&&folk.ContainsKey(StreetDensity.Folk.Musician))TryPlaceOrgan();
        else if(!organHours&&Organ&&Unseen(Organ.transform.position)){Destroy(Organ.gameObject);Organ=null;}
    }
    // By the station, where people come and go off the trains; then the square.
    static readonly (Vector3 at,float yaw)[] BeggarPitches={(new Vector3(181.6f,.1f,31.6f),150),(new Vector3(-12.4f,.1f,-3f),90),(new Vector3(12.4f,.1f,-3f),-90)};
    static readonly (Vector3 at,float yaw)[] OrganPitches={(new Vector3(5.5f,.1f,11.3f),0),(new Vector3(-5.5f,.1f,11.3f),0),(new Vector3(0,.1f,-11.6f),180)};
    void TryPlaceBeggar() {
        foreach(var (at,yaw) in BeggarPitches) {
            if(!Unseen(at)&&Distance(at)<45)continue;
            // The capsule's lowest point stays above the paving, or the ground itself counts as in the way.
            if(Physics.CheckCapsule(at+Vector3.up*.62f,at+Vector3.up*1.2f,.45f,~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore))continue;
            var prefab=folk[StreetDensity.Folk.Beggar][0];var go=Instantiate(prefab,transform);go.name="乞讨的人";
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            go.transform.SetPositionAndRotation(new Vector3(at.x,GroundAt(at,at.y+.2f),at.z),Quaternion.Euler(0,yaw+180,0));
            Beggar=Ensure<StreetBeggar>(go);Beggar.Setup(this);
            Debug.Log("STREET_BEGGAR_PLACED: "+at);return;
        }
    }
    // For the presentation recording: set up on the first clear pitch now, watched or not.
    public void PlaceOrganNow(){if(!Organ&&folk.ContainsKey(StreetDensity.Folk.Musician))TryPlaceOrgan(true);}
    void TryPlaceOrgan(bool watched=false) {
        foreach(var (at,yaw) in OrganPitches) {
            if(!watched&&!Unseen(at)&&Distance(at)<45)continue;
            if(Physics.CheckCapsule(at+Vector3.up*.77f,at+Vector3.up*1.5f,.6f,~((1<<Layer)|(1<<2)),QueryTriggerInteraction.Ignore))continue;
            var prefab=folk[StreetDensity.Folk.Musician][0];var go=Instantiate(prefab,transform);go.name="摇风琴的艺人";
            foreach(var t in go.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            go.transform.SetPositionAndRotation(new Vector3(at.x,GroundAt(at,at.y+.2f),at.z),Quaternion.Euler(0,yaw+180,0));
            Organ=Ensure<StreetOrgan>(go);Organ.Setup(this);
            Debug.Log("STREET_ORGAN_PLACED: "+at);return;
        }
    }
}
