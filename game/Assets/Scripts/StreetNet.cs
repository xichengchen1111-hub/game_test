using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// The town's streets as traffic and passers-by use them (user, 2026-10-15: more people and cars,
// cars keeping the rules of the road). Built at load from the same metre map as the town
// (TownPlan roads) and the 1930 street dressing (PeriodStreets: STOP signs and crossings), so it
// cannot drift from the geometry. Traffic keeps to the right. The east–west streets are the
// through roads; the north–south streets stop at their STOP signs, except at the one signalled
// corner (Station Road × Middle Street), which follows PeriodTrafficSignal. People keep to the
// footways and alleys and cross at corners, on the marked crossings where there are any.
public sealed class StreetNet {
    // ---- Carriageways and junctions (the town grid is axis-aligned) ----
    public sealed class Carriage {
        public TownPlanRoad road;public bool eastWest;public float centre,from,to,half,laneWidth;
        public Vector3 Dir=>eastWest?Vector3.right:Vector3.forward;
    }
    public sealed class Junction {
        public Vector3 centre;public Carriage ew,ns;public bool signal,crossings;
        public readonly List<Lane> inbound=new List<Lane>(),outbound=new List<Lane>();
        // Half extents of the box where the two carriageways overlap.
        public float HalfX=>ns.half;public float HalfZ=>ew.half;
        public bool Inside(Vector3 p,float margin=0)=>Mathf.Abs(p.x-centre.x)<=HalfX+margin&&Mathf.Abs(p.z-centre.z)<=HalfZ+margin;
    }
    // Anything a vehicle drives along: a lane between junctions, or a turn across one.
    public abstract class Path {
        public float length;
        public abstract Vector3 Point(float s);
        public abstract Vector3 Tangent(float s);
        public readonly List<Path> next=new List<Path>();
    }
    public sealed class Lane:Path {
        public Carriage carriage;public Vector3 start,end,dir;
        public Junction fromJunction,toJunction;
        // Arriving at a STOP sign (north–south streets at unsignalled corners).
        public bool stopSign;
        // Signal axis at the signalled corner: 0 north–south, 1 east–west (PeriodTrafficSignal faces N,E,S,W).
        public int signalAxis=-1;
        // How far its stop line stands back from the usual place, a metre behind the crossing (SetBackLines).
        public float setback;
        public bool Entry=>fromJunction==null;public bool Exit=>toJunction==null;
        public override Vector3 Point(float s)=>start+dir*Mathf.Clamp(s,0,length);
        public override Vector3 Tangent(float s)=>dir;
        public override string ToString()=>carriage.road.id+Compass(dir)+" "+Name(fromJunction)+"→"+Name(toJunction);
    }
    public sealed class Turn:Path {
        public Lane from,to;public Junction junction;public Vector3 p0,p1,p2;public int kind; // 0 straight, 1 right, 2 left
        // Turns through the same junction, from other lanes, that cannot be taken at the same time as this
        // one (BuildConflicts).
        public readonly HashSet<Turn> conflicts=new HashSet<Turn>();
        readonly float[] table=new float[17];
        public void Build(){float l=0;var prev=p0;table[0]=0;for(int i=1;i<17;i++){var p=Bezier(i/16f);l+=Vector3.Distance(prev,p);prev=p;table[i]=l;}length=l;}
        Vector3 Bezier(float t){float u=1-t;return u*u*p0+2*u*t*p1+t*t*p2;}
        float T(float s){s=Mathf.Clamp(s,0,length);for(int i=1;i<17;i++)if(table[i]>=s){float k=(s-table[i-1])/Mathf.Max(1e-5f,table[i]-table[i-1]);return (i-1+k)/16f;}return 1;}
        public override Vector3 Point(float s)=>Bezier(T(s));
        public override Vector3 Tangent(float s){float t=T(s),u=1-t;var d=2*u*(p1-p0)+2*t*(p2-p1);return d.sqrMagnitude>1e-6f?d.normalized:(p2-p0).normalized;}
        // Tightest radius of the turn (for the speed it is taken at).
        public float Radius=>kind==0?1000:Mathf.Max(3,Vector3.Distance(p0,p1));
        // A car's body at small steps from its line (front bumper on it) round the corner and a little
        // way out along the street beyond: everywhere it stands while it crosses (BuildSweeps).
        Vector3[] sweep=new Vector3[0],sweepForward=new Vector3[0];float[] slack=new float[0];Vector3 low,high;
        // Every fifth pose is looked at first, then the poses round any that come near (Clearance). A pose
        // is at most two steps (10 cm of path) from one of those, and the measure moves less than its
        // slack over that: 10 cm, and 8.5 m for each radian the body turns (its box axes swinging round
        // the other body; the tightest turn here has a 2.9 m radius, 0.4 m in all).
        const int Coarse=5;
        public void SetSweep(Vector3[] points,Vector3[] forwards) {
            sweep=points;sweepForward=forwards;low=high=points[0];slack=new float[points.Length];
            foreach(var p in points){low=Vector3.Min(low,p);high=Vector3.Max(high,p);}
            for(int c=0;c<points.Length;c++) {
                float turn=0;
                for(int i=Mathf.Max(0,c-2);i<=Mathf.Min(points.Length-1,c+2);i++)turn=Mathf.Max(turn,Vector3.Angle(forwards[c],forwards[i])*Mathf.Deg2Rad);
                slack[c]=.1f+8.5f*turn+.005f;
            }
        }
        // The least clearance between a body standing here and this sweep (a large number when far off):
        // exact whenever it is under `within`, otherwise no less than that (EditMode test against every pose).
        public float Clearance(Vector3 origin,Vector3 forward,float within=ClearPlan) {
            const float reach=AmbientCar.Apart;
            if(origin.x<low.x-reach||origin.x>high.x+reach||origin.z<low.z-reach||origin.z>high.z+reach)return float.MaxValue;
            float best=float.MaxValue;int n=sweep.Length;
            for(int c=0;;c=Mathf.Min(c+Coarse,n-1)) {
                float g=Gap(c,origin,forward);best=Mathf.Min(best,g);
                if(g<within+slack[c])for(int i=Mathf.Max(0,c-Coarse+1);i<Mathf.Min(n,c+Coarse);i++)best=Mathf.Min(best,Gap(i,origin,forward));
                if(c==n-1)break;
            }
            return best;
        }
        float Gap(int i,Vector3 origin,Vector3 forward) {
            float dx=sweep[i].x-origin.x,dz=sweep[i].z-origin.z;
            return dx*dx+dz*dz>AmbientCar.Apart*AmbientCar.Apart?float.MaxValue:AmbientCar.BoxGap(sweep[i],sweepForward[i],origin,forward);
        }
        // The least clearance between the bodies sweeping two turns, the same two steps along this one:
        // exact whenever it is under `within`.
        public float Clearance(Turn other,float within=ClearPlan) {
            float best=float.MaxValue;int n=sweep.Length;
            for(int c=0;;c=Mathf.Min(c+Coarse,n-1)) {
                float near=within+slack[c],g=other.Clearance(sweep[c],sweepForward[c],near);best=Mathf.Min(best,g);
                if(g<near)for(int i=Mathf.Max(0,c-Coarse+1);i<Mathf.Min(n,c+Coarse);i++)best=Mathf.Min(best,other.Clearance(sweep[i],sweepForward[i],within));
                if(c==n-1)break;
            }
            return best;
        }
        public override string ToString()=>(kind==0?"straight ":kind==1?"right ":"left ")+from.carriage.road.id+Compass(from.dir)+"→"+to.carriage.road.id+Compass(to.dir)+" at "+Name(junction);
    }
    static string Compass(Vector3 d)=>Mathf.Abs(d.x)>Mathf.Abs(d.z)?(d.x>0?"E":"W"):(d.z>0?"N":"S");
    static string Name(Junction j)=>j==null?"edge":"("+j.centre.x.ToString("0",CultureInfo.InvariantCulture)+","+j.centre.z.ToString("0",CultureInfo.InvariantCulture)+")";
    // ---- Walking ----
    public sealed class WalkNode {
        public Vector3 point;public readonly List<WalkEdge> edges=new List<WalkEdge>();public int id;
        // A building door people come out of and go into.
        public string door;
    }
    public sealed class WalkEdge {
        public WalkNode a,b;public float length,width;
        // Crossing a carriageway: look both ways first; on a marked crossing at a signal, wait for it.
        public Carriage crosses;public Junction junction;public bool marked;
        // Lateral offsets (metres right of the line, walking a→b) found clear of posts and walls at startup.
        public float[] clear={0};
        public WalkNode Other(WalkNode n)=>n==a?b:a;
    }

    public readonly List<Carriage> carriages=new List<Carriage>();
    public readonly List<Junction> junctions=new List<Junction>();
    public readonly List<Lane> lanes=new List<Lane>();
    public readonly List<Turn> turns=new List<Turn>();
    public readonly List<WalkNode> walkNodes=new List<WalkNode>();
    public readonly List<WalkEdge> walkEdges=new List<WalkEdge>();
    public float Ground=.1f;
    // Room kept between car bodies in a junction (StreetLife.JunctionBusy). The lines and the conflicts
    // are laid out with a little more, for the 5 cm steps the sweeps are taken at; both stay under the
    // 0.22 m two cars have side by side on a 4 m street, and over AmbientCar.Guard.
    public const float Clear=.16f,ClearPlan=.19f;
    const float SweepStep=.05f,SweepOut=2.5f;
    // Where Build spent its time (for the load log).
    public string BuildTimes{get;private set;}
    static string layoutKey;static float[] layoutSetbacks=new float[0];static int[] layoutConflicts=new int[0];

    public static StreetNet Build(TownPlan plan,string periodJson) {
        var net=new StreetNet();
        var crossingCorners=new List<Vector2>();
        if(!string.IsNullOrEmpty(periodJson)) {
            // "crossings" holds [x, z] pairs; JsonUtility cannot read nested arrays, so take them by hand.
            int at=periodJson.IndexOf("\"crossings\"");
            if(at>=0) {
                int open=periodJson.IndexOf('[',at),depth=0,close=open;
                for(int i=open;i<periodJson.Length;i++){if(periodJson[i]=='[')depth++;else if(periodJson[i]==']'&&--depth==0){close=i;break;}}
                var numbers=periodJson.Substring(open,close-open+1).Split(new[]{'[',']',',',' ','\n','\r','\t'},StringSplitOptions.RemoveEmptyEntries);
                var culture=System.Globalization.CultureInfo.InvariantCulture;
                for(int i=0;i+1<numbers.Length;i+=2)crossingCorners.Add(new Vector2(float.Parse(numbers[i],culture),float.Parse(numbers[i+1],culture)));
            }
        }
        foreach(var road in plan.roads) {
            var p=road.points;bool ew=Mathf.Abs(p[1]-p[3])<.01f,ns=Mathf.Abs(p[0]-p[2])<.01f;
            if(road.lane<3.5f||!(ew||ns))continue; // alleys and the narrow garden lane carry no motor traffic
            net.carriages.Add(new Carriage{road=road,eastWest=ew,centre=ew?p[1]:p[0],from=ew?Mathf.Min(p[0],p[2]):Mathf.Min(p[1],p[3]),to=ew?Mathf.Max(p[0],p[2]):Mathf.Max(p[1],p[3]),half=road.lane/2,laneWidth=road.lane/2});
        }
        foreach(var ew in net.carriages)if(ew.eastWest)foreach(var ns in net.carriages)if(!ns.eastWest) {
            if(ns.centre<ew.from||ns.centre>ew.to||ew.centre<ns.from||ew.centre>ns.to)continue;
            var j=new Junction{centre=new Vector3(ns.centre,0,ew.centre),ew=ew,ns=ns};
            foreach(var c in crossingCorners)if(Mathf.Abs(c.x-j.centre.x)<.5f&&Mathf.Abs(c.y-j.centre.z)<.5f)j.crossings=true;
            // The one signal stands at Station Road × Middle Street (StreetSurfaces.Period: (30, 20)).
            j.signal=Mathf.Abs(j.centre.x-30)<.5f&&Mathf.Abs(j.centre.z-20)<.5f;
            net.junctions.Add(j);
        }
        var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(var c in net.carriages)net.BuildLanes(c);
        foreach(var j in net.junctions)net.BuildTurns(j);
        // The set-back lines and the conflicts depend only on the map and the street dressing: worked out
        // once a run (the town is loaded again after every interior) and reused.
        string key=plan.sourceSha256+":"+(periodJson==null?0:periodJson.Length)+":"+(periodJson==null?0:periodJson.GetHashCode());
        bool cached=key==layoutKey&&layoutSetbacks.Length==net.lanes.Count;
        long lines=watch.ElapsedMilliseconds;
        if(cached)net.ApplySetbacks(layoutSetbacks);else net.SetBackLines();
        long conflicts=watch.ElapsedMilliseconds;
        if(cached)for(int i=0;i+1<layoutConflicts.Length;i+=2){var a=net.turns[layoutConflicts[i]];var b=net.turns[layoutConflicts[i+1]];a.conflicts.Add(b);b.conflicts.Add(a);}
        else net.BuildConflicts();
        if(!cached) {
            layoutKey=key;layoutSetbacks=new float[net.lanes.Count];for(int i=0;i<net.lanes.Count;i++)layoutSetbacks[i]=net.lanes[i].setback;
            var pairs=new List<int>();
            for(int i=0;i<net.turns.Count;i++)for(int k=i+1;k<net.turns.Count;k++)if(net.turns[i].conflicts.Contains(net.turns[k])){pairs.Add(i);pairs.Add(k);}
            layoutConflicts=pairs.ToArray();
        }
        long footways=watch.ElapsedMilliseconds;net.BuildWalks(plan);
        net.BuildTimes=(cached?"reused: ":"")+"lines "+(conflicts-lines)+" ms, conflicts "+(footways-conflicts)+" ms, footways "+(watch.ElapsedMilliseconds-footways)+" ms";
        return net;
    }
    // Lanes between consecutive junctions, one each way, on the right-hand half of the carriageway.
    void BuildLanes(Carriage c) {
        var stations=new List<(float s,Junction j)>{(c.from,null),(c.to,null)};
        foreach(var j in junctions)if(j.ew==c||j.ns==c)stations.Add((c.eastWest?j.centre.x:j.centre.z,j));
        stations.Sort((a,b)=>a.s.CompareTo(b.s));
        for(int i=0;i+1<stations.Count;i++) {
            var (s0,j0)=stations[i];var (s1,j1)=stations[i+1];
            // Back from the line where people cross, so a waiting car never stands on it.
            float m0=j0==null?0:StopOffset(j0,c);
            float m1=j1==null?0:StopOffset(j1,c);
            if(s1-m1-(s0+m0)<4)continue;
            foreach(int sign in new[]{1,-1}) {
                var dir=c.Dir*sign;var right=Vector3.Cross(Vector3.up,dir);
                float a=sign>0?s0+m0:s1-m1,b=sign>0?s1-m1:s0+m0;
                Vector3 P(float s)=>(c.eastWest?new Vector3(s,0,c.centre):new Vector3(c.centre,0,s))+right*(c.laneWidth/2);
                var lane=new Lane{carriage=c,start=P(a),end=P(b),dir=dir,fromJunction=sign>0?j0:j1,toJunction=sign>0?j1:j0};
                lane.length=Vector3.Distance(lane.start,lane.end);
                if(lane.fromJunction!=null)lane.fromJunction.outbound.Add(lane);
                if(lane.toJunction!=null) {
                    lane.toJunction.inbound.Add(lane);
                    if(lane.toJunction.signal)lane.signalAxis=c.eastWest?1:0;
                    else lane.stopSign=!c.eastWest;
                }
                lanes.Add(lane);
            }
        }
    }
    // The stop line: a metre behind the far side of the footway line people cross on (the painted bars at
    // Station Road × Middle Street lie there too: x 25/35, and z 12.05/27.95 where SetBackLines moves it).
    static float StopOffset(Junction j,Carriage c){var other=c.eastWest?j.ns:j.ew;return other.half+other.road.strip+other.road.walk/2+1.1f;}
    // A car turning into one of the 4 m streets swings its front corner over the middle of it as it
    // straightens up (two cars side by side there have 0.22 m between them) and would catch a car
    // waiting at the line opposite. As stop lines are painted back for the swept path of turning
    // vehicles, each line is moved back until a car standing at it, or anywhere behind it, is ClearPlan
    // clear of every other approach's way through the corner. On this map that is the north–south
    // approaches, by about 2 m (STREET-03).
    void SetBackLines() {
        // Only the ways out of the lines that moved change from one pass to the next.
        var changed=new HashSet<Lane>(lanes);
        for(int pass=0;pass<4;pass++) {
            BuildSweeps();var moved=new HashSet<Lane>();
            foreach(var lane in lanes) {
                if(lane.toJunction==null)continue;
                float need=0;
                foreach(var t in turns)if(t.junction==lane.toJunction&&t.from!=lane&&changed.Contains(t.from))need=Mathf.Max(need,Behind(lane,t));
                need=Mathf.Min(need,Mathf.Max(0,lane.length-8));
                if(need>0){lane.end-=lane.dir*need;lane.length-=need;lane.setback+=need;moved.Add(lane);}
            }
            changed=moved;
            if(moved.Count==0)return;
            turns.Clear();foreach(var l in lanes)l.next.Clear();
            foreach(var j in junctions)BuildTurns(j);
        }
        BuildSweeps();
    }
    void ApplySetbacks(float[] setbacks) {
        for(int i=0;i<lanes.Count;i++){var lane=lanes[i];float need=setbacks[i];if(need<=0)continue;lane.end-=lane.dir*need;lane.length-=need;lane.setback=need;}
        turns.Clear();foreach(var l in lanes)l.next.Clear();
        foreach(var j in junctions)BuildTurns(j);
        BuildSweeps();
    }
    // How far back from the line a car standing on the lane can still come within ClearPlan of the turn's
    // sweep (0 when it never does), in 5 cm. From the line back until a metre in a row is clear, every
    // quarter metre and then every 5 cm round any that come within ClearPlan and an eighth of a metre
    // (moving straight back a body moves no more than that from the nearest of them).
    float Behind(Lane lane,Turn t) {
        float need=0;
        for(float back=0,clearRun=0;back<6&&clearRun<1;back+=.25f) {
            float s=lane.length-AmbientCar.Front-back;if(s<0)break;
            bool caught=false;
            if(t.Clearance(lane.Point(s),lane.dir,ClearPlan+.15f)<ClearPlan+.15f)
                for(float fine=Mathf.Max(0,back-.2f);fine<=back+.2f;fine+=SweepStep) {
                    float f=lane.length-AmbientCar.Front-fine;
                    if(f>=0&&t.Clearance(lane.Point(f),lane.dir)<ClearPlan){need=Mathf.Max(need,fine+SweepStep);caught=true;}
                }
            clearRun=caught?0:clearRun+.25f;
        }
        return need;
    }
    // Each way through a corner as the body sweeps it: from the line to the end of the approach lane,
    // round the turn and a little way out along the street beyond.
    void BuildSweeps() {
        var points=new List<Vector3>();var forwards=new List<Vector3>();
        foreach(var t in turns) {
            points.Clear();forwards.Clear();
            for(float s=Mathf.Max(0,t.from.length-AmbientCar.Front);s<t.from.length;s+=SweepStep){points.Add(t.from.Point(s));forwards.Add(t.from.dir);}
            int n=Mathf.CeilToInt(t.length/SweepStep);
            for(int i=0;i<=n;i++){float s=t.length*i/n;points.Add(t.Point(s));forwards.Add(t.Tangent(s));}
            for(float s=SweepStep;s<=SweepOut;s+=SweepStep){points.Add(t.to.Point(s));forwards.Add(t.to.dir);}
            t.SetSweep(points.ToArray(),forwards.ToArray());
        }
    }
    // Two ways through the same junction from different lanes conflict when their centre lines come
    // within 2.3 m, or the bodies sweeping them come within ClearPlan (a right turn into a 4 m street
    // and a right turn out of it, opposite, clear the first test and still touch).
    void BuildConflicts() {
        var samples=new Dictionary<Turn,Vector3[]>();
        foreach(var t in turns){var p=new Vector3[13];for(int i=0;i<13;i++)p[i]=t.Point(t.length*i/12f);samples[t]=p;}
        for(int i=0;i<turns.Count;i++)for(int k=i+1;k<turns.Count;k++) {
            var a=turns[i];var b=turns[k];
            if(a.junction!=b.junction||a.from==b.from)continue;
            float best=float.MaxValue;foreach(var p in samples[a])foreach(var q in samples[b])best=Mathf.Min(best,(p-q).sqrMagnitude);
            if(best<2.3f*2.3f||a.Clearance(b)<ClearPlan){a.conflicts.Add(b);b.conflicts.Add(a);}
        }
    }
    // The tightest places left, for the startup log and the EditMode test: a car standing at or behind
    // a line against the other approaches' ways through the corner, and two ways through a corner that
    // may be taken at once.
    public float WaitingClearance(out string where) {
        float best=float.MaxValue;where="";
        foreach(var lane in lanes) {
            if(lane.toJunction==null)continue;
            foreach(var t in turns) {
                if(t.junction!=lane.toJunction||t.from==lane)continue;
                for(float back=0;back<6;back+=SweepStep*.5f) {
                    float s=lane.length-AmbientCar.Front-back;if(s<0)break;
                    float c=t.Clearance(lane.Point(s),lane.dir,.5f);
                    if(c<best){best=c;where=lane+" front "+back.ToString("0.00",CultureInfo.InvariantCulture)+" m behind its line, against "+t;}
                }
            }
        }
        return best;
    }
    public float OpenTurnClearance(out string where) {
        float best=float.MaxValue;where="";
        for(int i=0;i<turns.Count;i++)for(int k=i+1;k<turns.Count;k++) {
            var a=turns[i];var b=turns[k];
            if(a.junction!=b.junction||a.from==b.from||a.conflicts.Contains(b))continue;
            float c=a.Clearance(b,.5f);if(c<best){best=c;where=a+" and "+b;}
        }
        return best;
    }
    void BuildTurns(Junction j) {
        foreach(var from in lanes)if(from.toJunction==j)foreach(var to in lanes)if(to.fromJunction==j) {
            if(Vector3.Dot(from.dir,to.dir)<-.9f)continue; // no U-turns
            float cross=Vector3.Cross(from.dir,to.dir).y;int kind=Mathf.Abs(cross)<.1f?0:cross>0?1:2;
            Vector3 p1;
            if(kind==0)p1=(from.end+to.start)/2;
            else {
                // Where the two lane lines meet: the corner the turn is cut around.
                p1=from.end+from.dir*Vector3.Dot(to.start-from.end,from.dir);
                if(Vector3.Dot(to.start-p1,to.dir)<0)p1=(from.end+to.start)/2;
            }
            var turn=new Turn{from=from,to=to,junction=j,p0=from.end,p1=p1,p2=to.start,kind=kind};turn.Build();
            from.next.Add(turn);turn.next.Add(to);turns.Add(turn);
        }
    }
    // ---- Footways: lines along both sides of each carriageway and down each alley; where two lines
    // meet there is a corner, and a stretch of footway line over a carriageway is a crossing. ----
    void BuildWalks(TownPlan plan) {
        var lines=new List<(Vector3 a,Vector3 b,float width,bool alley)>();
        foreach(var road in plan.roads) {
            var p=road.points;var a=new Vector3(p[0],0,p[1]);var b=new Vector3(p[2],0,p[3]);var dir=(b-a).normalized;var right=Vector3.Cross(Vector3.up,dir);
            if(road.lane>0)foreach(int side in new[]{-1,1}){float off=road.lane/2+road.strip+road.walk/2;lines.Add((a+right*off*side,b+right*off*side,road.walk,false));}
            else lines.Add((a,b,road.walk,true));
        }
        // The square round the tower: the footpath from the square's south side (A04) round the west of
        // the tower to Station Road's south footway. Kept only where it proves clear (CheckClearance).
        lines.Add((new Vector3(0,0,-13.5f),new Vector3(-9,0,-12),2.4f,true));
        lines.Add((new Vector3(-9,0,-12),new Vector3(-9,0,15),2.4f,true));
        var cuts=new List<float>[lines.Count];for(int i=0;i<lines.Count;i++)cuts[i]=new List<float>{0,1};
        for(int i=0;i<lines.Count;i++)for(int k=i+1;k<lines.Count;k++)
            if(Intersect(lines[i].a,lines[i].b,lines[k].a,lines[k].b,out float t,out float u)){cuts[i].Add(t);cuts[k].Add(u);}
        var nodes=new Dictionary<Vector2Int,WalkNode>();
        WalkNode Node(Vector3 p){var key=new Vector2Int(Mathf.RoundToInt(p.x*10),Mathf.RoundToInt(p.z*10));if(!nodes.TryGetValue(key,out var n)){n=new WalkNode{point=new Vector3(p.x,Ground,p.z),id=walkNodes.Count};nodes[key]=n;walkNodes.Add(n);}return n;}
        Carriage Over(Vector3 p){foreach(var c in carriages)if(c.eastWest?Mathf.Abs(p.z-c.centre)<c.half&&p.x>=c.from&&p.x<=c.to:Mathf.Abs(p.x-c.centre)<c.half&&p.z>=c.from&&p.z<=c.to)return c;return null;}
        for(int i=0;i<lines.Count;i++) {
            var list=cuts[i];list.Sort();
            for(int k=0;k+1<list.Count;k++) {
                if(list[k+1]-list[k]<1e-4f)continue;
                var a=Vector3.Lerp(lines[i].a,lines[i].b,list[k]);var b=Vector3.Lerp(lines[i].a,lines[i].b,list[k+1]);
                // A line drawn on into a street ends at the footway it meets there. The back alleys A01 and A02
                // run into Station West Road (to x 134, and to its centre line); the stretch past its west
                // footway would end in the road, a dead end StreetLife would send people to as a way out of
                // town, along the carriageway (WALK-01).
                if(Over(a)!=null||Over(b)!=null)continue;
                var e=new WalkEdge{a=Node(a),b=Node(b),length=Vector3.Distance(a,b),width=lines[i].width};
                var mid=(a+b)/2;e.crosses=Over(mid);
                if(e.crosses!=null)foreach(var j in junctions)if(j.Inside(mid,6)){e.junction=j;e.marked=j.crossings;break;}
                // Footways cross only at corners; an alley's mouth crosses where it meets the street (legal in
                // 1931 if the walker gives way to the cars: no marking, gap acceptance).
                if(e.crosses!=null&&e.junction==null&&!lines[i].alley)continue;
                e.a.edges.Add(e);e.b.edges.Add(e);walkEdges.Add(e);
            }
        }
        // Each street door joins the nearest footway, so people come out of shops and go into them.
        foreach(var building in plan.buildings) {
            if(building.door==null||building.door.Length<2)continue;
            // The step in front of the door (as the paper map's door point), never the door leaf itself.
            var door=new Vector3(building.door[0],Ground,building.door[1])+Quaternion.Euler(0,TownPlan.Yaw(building.doorFacing),0)*Vector3.back*.85f;
            WalkEdge best=null;float bestD=6;Vector3 bestP=default;
            foreach(var e in walkEdges){if(e.crosses!=null)continue;var p=Closest(e.a.point,e.b.point,door);float d=Vector3.Distance(p,door);if(d<bestD){bestD=d;best=e;bestP=p;}}
            if(best==null)continue;
            var dn=new WalkNode{point=door,id=walkNodes.Count,door=building.id};walkNodes.Add(dn);
            var mid=Split(best,bestP);
            var link=new WalkEdge{a=dn,b=mid,length=Vector3.Distance(dn.point,mid.point),width=1.2f};dn.edges.Add(link);mid.edges.Add(link);walkEdges.Add(link);
        }
    }
    WalkNode Split(WalkEdge e,Vector3 p) {
        p.y=Ground;
        if(Vector3.Distance(p,e.a.point)<.3f)return e.a;if(Vector3.Distance(p,e.b.point)<.3f)return e.b;
        var n=new WalkNode{point=p,id=walkNodes.Count};walkNodes.Add(n);
        var second=new WalkEdge{a=n,b=e.b,length=Vector3.Distance(n.point,e.b.point),width=e.width};
        e.b.edges.Remove(e);e.b.edges.Add(second);e.b=n;e.length=Vector3.Distance(e.a.point,n.point);
        n.edges.Add(e);n.edges.Add(second);walkEdges.Add(second);return n;
    }
    static Vector3 Closest(Vector3 a,Vector3 b,Vector3 p){var ab=b-a;float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(1e-6f,ab.sqrMagnitude));return a+ab*t;}
    static bool Intersect(Vector3 a,Vector3 b,Vector3 c,Vector3 d,out float t,out float u) {
        t=u=0;float rx=b.x-a.x,rz=b.z-a.z,sx=d.x-c.x,sz=d.z-c.z;float den=rx*sz-rz*sx;if(Mathf.Abs(den)<1e-6f)return false;
        float qx=c.x-a.x,qz=c.z-a.z;t=(qx*sz-qz*sx)/den;u=(qx*rz-qz*rx)/den;return t>-1e-4f&&t<1+1e-4f&&u>-1e-4f&&u<1+1e-4f;
    }
    // Posts, poles, benches and walls the line was drawn without: sweep a walker's capsule along each
    // edge at the offsets people use and keep only the clear ones; an edge with none is dropped.
    public void CheckClearance(int ignoreLayers) {
        var removed=new List<WalkEdge>();
        foreach(var e in walkEdges) {
            var dir=e.b.point-e.a.point;dir.y=0;float length=dir.magnitude;if(length<.05f)continue;dir/=length;
            var right=Vector3.Cross(Vector3.up,dir);float o=Mathf.Clamp(e.width*.22f,.25f,.6f);
            var clear=new List<float>();
            foreach(float offset in new[]{o,0,-o}) {
                var start=e.a.point+right*offset;
                if(!Physics.CheckCapsule(start+Vector3.up*.45f,start+Vector3.up*1.55f,.26f,~ignoreLayers,QueryTriggerInteraction.Ignore)&&
                   !Physics.CapsuleCast(start+Vector3.up*.45f,start+Vector3.up*1.55f,.26f,dir,length,~ignoreLayers,QueryTriggerInteraction.Ignore))clear.Add(offset);
            }
            if(clear.Count==0)removed.Add(e);else e.clear=clear.ToArray();
        }
        foreach(var e in removed){e.a.edges.Remove(e);e.b.edges.Remove(e);walkEdges.Remove(e);}
        Debug.Log("STREET_NET_CLEARANCE: kept "+walkEdges.Count+" walk edges, dropped "+removed.Count);
    }
    // ---- Queries ----
    public Lane NearestLane(Vector3 p,out float s) {
        Lane best=null;float bestD=float.MaxValue;s=0;
        foreach(var l in lanes){var q=Closest(l.start,l.end,p);float d=(new Vector3(q.x,0,q.z)-new Vector3(p.x,0,p.z)).sqrMagnitude;if(d<bestD){bestD=d;best=l;s=Vector3.Distance(l.start,q);}}
        return best;
    }
    public WalkNode NearestWalk(Vector3 p){WalkNode best=null;float bestD=float.MaxValue;foreach(var n in walkNodes){if(n.door!=null)continue;float d=(n.point-p).sqrMagnitude;if(d<bestD){bestD=d;best=n;}}return best;}
}
