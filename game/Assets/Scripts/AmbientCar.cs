using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// A car in the town's traffic (StreetLife spawns, recycles and steps them). It drives its lane and
// turn paths on the right-hand side with the Intelligent Driver Model (Treiber 2000) behind whatever
// is nearest ahead — another car, a stop line, a person in the road, the player — so it eases off,
// queues and pulls away instead of stopping dead. The rules are the 1926 Uniform Vehicle Code's:
// a full stop at the STOP signs and a gap in the through traffic before going on (the car on the
// right first when two arrive together), the signal at Station Road × Middle Street (stop on amber
// when it can be done comfortably), left turns giving way to oncoming cars, people on the corners
// given way to, and hand signals from the driver's window before turning (no flashing indicators
// until 1939). Kinematic: it never pushes anyone, so it must stop before touching them.
public sealed class AmbientCar : MonoBehaviour {
    public StreetNet.Path path;public float s,speed;
    public readonly List<StreetNet.Path> plan=new List<StreetNet.Path>();
    public float length=4.3f,width=1.78f;
    public const float Front=1.98f;
    // The body in plan: 4.3 × 1.78 m, its centre 0.17 m behind the origin. Two bodies whose origins are
    // further apart than Apart cannot come within 0.6 m of each other (each corner is at most 2.49 m
    // from its origin).
    public const float HalfLength=2.15f,HalfWidth=.89f,CentreBack=.17f,Apart=5.6f;
    // The last resort in Tick: never move to within this of another car's body.
    public const float Guard=.1f;
    // How far apart two bodies are along the box sides' directions (separating axes, in plan): positive
    // is clear by that much, negative is how deep they overlap. The review's audit measures the same.
    public static float BoxGap(Vector3 originA,Vector3 forwardA,Vector3 originB,Vector3 forwardB) {
        float ax=forwardA.x,az=forwardA.z,bx=forwardB.x,bz=forwardB.z;
        float la=1/Mathf.Sqrt(ax*ax+az*az),lb=1/Mathf.Sqrt(bx*bx+bz*bz);ax*=la;az*=la;bx*=lb;bz*=lb;
        // Centre to centre; the axes are each box's forward (x,z) and right (z,-x).
        float dx=originB.x-bx*CentreBack-originA.x+ax*CentreBack,dz=originB.z-bz*CentreBack-originA.z+az*CentreBack;
        float same=Mathf.Abs(ax*bx+az*bz),cross=Mathf.Abs(ax*bz-az*bx);
        float alongA=Mathf.Abs(dx*ax+dz*az)-HalfLength-HalfLength*same-HalfWidth*cross;
        float acrossA=Mathf.Abs(dx*az-dz*ax)-HalfWidth-HalfLength*cross-HalfWidth*same;
        float alongB=Mathf.Abs(dx*bx+dz*bz)-HalfLength-HalfLength*same-HalfWidth*cross;
        float acrossB=Mathf.Abs(dx*bz-dz*bx)-HalfWidth-HalfLength*cross-HalfWidth*same;
        return Mathf.Max(Mathf.Max(alongA,acrossA),Mathf.Max(alongB,acrossB));
    }
    // Period values (1931 cars accelerate slowly; design brief, section 1): desired speed from the
    // street's limit and the driver, gentle acceleration, comfortable braking, time gap, standstill gap.
    public float limitScale=1,accel=.9f,comfortable=1.5f,headway=1.8f,standstill=2.5f;
    public CarPart[] parts=new CarPart[0];
    public Renderer[] lampRenderers=new Renderer[0];public int[] lampSlots=new int[0],lampKinds=new int[0];
    // The driver's left arm: on the wheel, out level (left turn), up (right turn), down (stopping).
    public Transform[] armBones=new Transform[0];public Quaternion[] armPoses=new Quaternion[0];
    public AudioSource engine,road,horn;
    public bool Gone{get;private set;}
    public float StoppedFor{get;private set;}
    public string Waiting{get;private set;}
    public bool StopDone=>stopDone;public float StopDoneAt{get;private set;}
    public StreetNet.Turn NextTurn=>plan.Count>0?plan[0] as StreetNet.Turn:path as StreetNet.Turn;
    // Its front bumper over the stop line, still on the approach: as good as in the junction.
    public bool PastLine=>path is StreetNet.Lane l&&l.toJunction!=null&&l.length-s-Front<0;
    public int Signal{get;private set;}
    float stopTimer;bool stopDone,amberGo,held;string waitedFor;float blockedByPlayer,honkLeft,wheelAngle,steer,armBlend;int armShown,armFrom;
    System.Random rng;MaterialPropertyBlock lampBlock;StreetLife life;
    public void Place(StreetLife owner,StreetNet.Path start,float at,System.Random random,float initialSpeed) {
        life=owner;path=start;s=at;speed=initialSpeed;plan.Clear();rng=random;Gone=false;stopDone=false;stopTimer=0;StoppedFor=0;Waiting=null;held=false;
        limitScale=.92f+.18f*(float)rng.NextDouble();headway=1.6f+.4f*(float)rng.NextDouble();blockedByPlayer=0;honkLeft=0;Signal=0;armShown=0;armBlend=1;
        Pose();
    }
    StreetNet.Path NextAfter(StreetNet.Path p) {
        if(p.next.Count==0)return null;
        if(p is StreetNet.Turn)return p.next[0];
        // Most traffic stays on the east–west streets; the narrow north–south ones carry less.
        float pick=(float)rng.NextDouble(),total=0;var weights=new float[p.next.Count];
        for(int i=0;i<p.next.Count;i++) {
            var t=(StreetNet.Turn)p.next[i];bool ontoNarrow=!t.to.carriage.eastWest;
            weights[i]=t.kind==0?.6f:t.kind==1?.22f:.18f;
            if(ontoNarrow&&t.kind!=0)weights[i]*=.55f;
            if(!t.from.carriage.eastWest&&t.to.carriage.eastWest)weights[i]*=1.6f;
            // Leaving town near the player would make it vanish in view.
            if(t.to.Exit&&life.NearPlayer(t.to.end,70))weights[i]*=.02f;
            total+=weights[i];
        }
        pick*=total;for(int i=0;i<weights.Length;i++){pick-=weights[i];if(pick<=0)return p.next[i];}
        return p.next[p.next.Count-1];
    }
    // Paths ahead, chosen early enough to look down them.
    void EnsurePlan(float ahead) {
        float covered=path.length-s;var last=plan.Count>0?plan[plan.Count-1]:path;
        foreach(var p in plan)covered+=p.length;
        while(covered<ahead){var n=NextAfter(last);if(n==null)break;plan.Add(n);covered+=n.length;last=n;}
    }
    public void Tick(float dt) {
        if(Gone||dt<=0)return;
        EnsurePlan(70);
        // gap drives the IDM; hard is the real free distance, never driven into.
        float gap=float.MaxValue,hard=float.MaxValue,leaderSpeed=0;Waiting=null;
        var paths=new List<StreetNet.Path>(plan.Count+1){path};paths.AddRange(plan);
        // 1. The car ahead on this path and the next ones, 70 m on from here (STREET-03: counted from the
        // start of this path, a lane over 70 m long hid the car that had just turned off it ahead).
        float offset=0;
        for(int k=0;k<paths.Count&&offset-s<70;k++) {
            var p=paths[k];
            foreach(var other in life.CarsOn(p)) {
                if(other==this||other.Gone)continue;
                if(k==0&&other.s<=s)continue;
                float along=(k==0?other.s-s:offset+other.s-s)-length;
                if(along<gap){gap=along;leaderSpeed=other.speed;Waiting="car";}
                hard=Mathf.Min(hard,along);
            }
            offset+=p.length;
        }
        // The car ahead may have left this lane by another turn: still ahead of us until it is clear.
        var fromLane=path as StreetNet.Lane??(path as StreetNet.Turn)?.from;
        if(fromLane!=null)foreach(var sibling in fromLane.next) {
            if(sibling==path||plan.Count>0&&sibling==plan[0])continue;
            foreach(var other in life.CarsOn(sibling)) {
                if(other==this)continue;
                float along=path is StreetNet.Lane?fromLane.length-s+other.s-length:other.s-s-length;
                if(along>-length&&along<gap){gap=Mathf.Max(0,along);leaderSpeed=other.speed;Waiting="car";hard=Mathf.Min(hard,gap);}
            }
        }
        // 2. The rules at the end of this lane.
        if(path is StreetNet.Lane lane&&lane.toJunction!=null) {
            // From the front bumper (1.98 m ahead of the car's origin) to the stop line.
            float toLine=lane.length-s-Front;
            // A line is not a car: stop with the bumper just short of it, not a standstill gap back.
            if(MustStop(lane,toLine,dt)) {
                hard=Mathf.Min(hard,Mathf.Max(0,toLine));
                if(toLine+standstill-.35f<gap){gap=Mathf.Max(0,toLine)+standstill-.35f;leaderSpeed=0;Waiting="rule";}
            }
        }
        // 3. People, the player and other solid things in the road ahead.
        float look=Mathf.Max(9,speed*speed/(2*comfortable)+12);
        float obstacle=life.ObstacleAhead(this,paths,s,look,out bool isPlayer);
        if(obstacle<gap){gap=obstacle;leaderSpeed=0;Waiting=isPlayer?"player":"person";}
        hard=Mathf.Min(hard,obstacle);
        if(Waiting=="person"&&waitedFor!="person")life.Stats.yields++;
        waitedFor=Waiting;
        // One short toot if the player stands in the way for a while, never a harsh blare.
        if(isPlayer&&obstacle<7&&speed<.3f){blockedByPlayer+=dt;if(blockedByPlayer>2.5f&&blockedByPlayer<100){blockedByPlayer=100;honkLeft=.35f;life.Honked(this);}}
        else if(!isPlayer||obstacle>9)blockedByPlayer=0;
        // IDM.
        float rain=life.Rain;
        float v0=StreetDensity.SpeedLimit(Carriage)*limitScale*(1-.1f*rain);
        // Slow on the narrow streets while meeting another car (two barely pass on 4 m).
        if(Carriage!=null&&!Carriage.eastWest&&life.Meeting(this,24))v0=Mathf.Min(v0,3.2f);
        if(path is StreetNet.Turn turn)v0=Mathf.Min(v0,Mathf.Sqrt(2.2f*turn.Radius));
        else if(plan.Count>0&&plan[0] is StreetNet.Turn coming){float toTurn=path.length-s;float vt=Mathf.Sqrt(2.2f*coming.Radius);v0=Mathf.Min(v0,Mathf.Sqrt(vt*vt+2*comfortable*.7f*Mathf.Max(0,toTurn)));}
        float T=headway*(1+.25f*rain);
        float sStar=standstill+Mathf.Max(0,speed*T+speed*(speed-leaderSpeed)/(2*Mathf.Sqrt(accel*comfortable)));
        float a=accel*(1-Mathf.Pow(speed/Mathf.Max(.1f,v0),4)-(gap<float.MaxValue?Mathf.Pow(sStar/Mathf.Max(.05f,gap),2):0));
        a=Mathf.Clamp(a,-7f,accel);
        speed=Mathf.Max(0,speed+a*dt);
        if(hard<.05f)speed=0;
        // Held at a line: a dead stop, not a creep (a full stop is what the STOP sign asks).
        if(Waiting=="rule"&&hard<.6f)speed=0;
        // Likewise short of someone standing in the road. Their distance is found in 0.75 m steps, so
        // near the standstill gap the IDM would inch up at walking pace until the next step (STREET-03:
        // 0.14 m/s for over 5 s, 1 cm short of braking, when the review checked).
        if((Waiting=="person"||Waiting=="player")&&gap<standstill+.75f&&speed<.3f)speed=0;
        // Never step into what is there: the real free distance is a hard limit for this frame.
        float move=Mathf.Min(speed*dt,Mathf.Max(0,hard-.05f));
        if(hard<float.MaxValue&&move<speed*dt)speed=move/Mathf.Max(dt,1e-4f);
        // The last resort, whichever way another car came (the rules above should never leave one
        // there; StreetLife counts and logs each time this holds a car): the body never moves to
        // within Guard of another's, unless the move opens the gap.
        if(move>0) {
            if(Blocked(move,out var by,out float now,out float then)){move=0;speed=0;Waiting="car";if(!held){held=true;life.Guarded(this,by,now,then);}}
            else held=false;
        }
        s+=move;
        StoppedFor=speed<.05f?StoppedFor+dt:0;
        while(s>path.length) {
            s-=path.length;
            if(path is StreetNet.Lane l&&l.Exit){Gone=true;return;}
            if(plan.Count==0){var n=NextAfter(path);if(n==null){Gone=true;return;}plan.Add(n);}
            if(path is StreetNet.Lane crossed&&plan[0] is StreetNet.Turn)life.Entered(this,crossed,stopDone,amberGo);
            life.Move(this,path,plan[0]);path=plan[0];plan.RemoveAt(0);stopDone=false;stopTimer=0;amberGo=false;
        }
        Pose();
        Animate(dt,a);
    }
    // Whether moving on by `move` would bring the body within Guard of another car's and nearer than now.
    bool Blocked(float move,out AmbientCar by,out float now,out float then) {
        var p=path;float at=s+move;int index=0;
        while(at>p.length&&index<plan.Count){at-=p.length;p=plan[index++];}
        var to=p.Point(at);var facing=p.Tangent(at);var here=transform.position;var ahead=transform.forward;
        by=null;now=then=0;var cars=life.AllCars;
        for(int i=0;i<cars.Count;i++) {
            var other=cars[i];if(other==this||other.Gone)continue;var o=other.transform;
            float dx=o.position.x-here.x,dz=o.position.z-here.z;if(dx*dx+dz*dz>8*8)continue;
            float next=BoxGap(to,facing,o.position,o.forward);if(next>=Guard)continue;
            float current=BoxGap(here,ahead,o.position,o.forward);
            if(next<current){by=other;now=current;then=next;return true;}
        }
        return false;
    }
    // For the logs: where it is, on which way, how fast, and what it waits for.
    public string Describe() {
        var p=transform.position;var c=CultureInfo.InvariantCulture;
        return name+" ("+p.x.ToString("0.00",c)+", "+p.z.ToString("0.00",c)+") heading "+transform.eulerAngles.y.ToString("0",c)+" on "+path+" s="+s.ToString("0.00",c)+"/"+path.length.ToString("0.00",c)+
            " speed="+speed.ToString("0.00",c)+" waiting="+(Waiting??"-")+" stopped="+StoppedFor.ToString("0.0",c)+"s"+(PastLine?" past its line":"")+(plan.Count>0?" then "+plan[0]:"");
    }
    // How far along its way this car first comes within 1.6 m of the segment a–b (a crossing), or -1.
    public float RouteDistanceTo(Vector3 a,Vector3 b,float maxAhead) {
        float along=0,sOn=s;int index=0;StreetNet.Path p=path;
        while(along<=maxAhead&&p!=null) {
            if(sOn>p.length){sOn-=p.length;p=index<plan.Count?plan[index]:null;index++;continue;}
            var q=p.Point(sOn);var ab=b-a;ab.y=0;var aq=q-a;aq.y=0;
            float t=Mathf.Clamp01(Vector3.Dot(aq,ab)/Mathf.Max(1e-4f,ab.sqrMagnitude));var near=a+ab*t-q;near.y=0;
            if(near.magnitude<1.6f)return along;
            sOn+=.75f;along+=.75f;
        }
        return -1;
    }
    public StreetNet.Carriage Carriage=>path is StreetNet.Lane l?l.carriage:path is StreetNet.Turn t?t.to.carriage:null;
    // Whether the car must hold at the stop line now.
    bool MustStop(StreetNet.Lane lane,float toLine,float dt) {
        var j=lane.toJunction;var next=plan.Count>0?plan[0] as StreetNet.Turn:null;
        // Over the line already (the way was clear when it went, and others now wait for it): on round
        // the corner unless someone is actually in the way. The right of way and the light were settled
        // at the line (it went on green or amber); stopping dead over the line would leave it where the
        // cars turning past swing through.
        if(toLine<0){if(lane.signalAxis>=0)amberGo=true;return life.JunctionBusy(j,this,next);}
        if(lane.signalAxis>=0) {
            int state=PeriodTrafficSignal.State(life.SignalSeconds,lane.signalAxis);
            if(state==0)return toLine>-.5f;
            // Amber: stop when no more than 3 m/s² of braking does it; otherwise carry on through (and keep going).
            if(amberGo)return false;
            if(state==1){bool stop=toLine>.5f&&speed*speed/(2*Mathf.Max(.1f,toLine))<=3f;if(!stop&&speed>1)amberGo=true;return stop;}
            // Green: a left turn waits for oncoming traffic; nobody enters a junction still occupied.
            if(next!=null&&next.kind==2&&life.OncomingWithin(lane,32))return true;
            return life.JunctionBusy(j,this,next);
        }
        if(lane.stopSign) {
            // A full stop at the line for a second, then go when the through street is clear.
            if(!stopDone) {
                if(toLine<2.4f&&speed<.05f){stopTimer+=dt;if(stopTimer>1.1f){stopDone=true;StopDoneAt=Time.time;}}
                return true;
            }
            return life.JunctionBusy(j,this,next)||life.ThroughTrafficWithin(j,this,5.5f)||life.YieldToRight(j,this);
        }
        // The through street: carry on unless the junction is occupied or turning left across oncoming cars.
        if(next!=null&&next.kind==2&&life.OncomingWithin(lane,26))return true;
        return life.JunctionBusy(j,this,next);
    }
    void Pose() {
        var p=path.Point(s);var t=path.Tangent(s);
        transform.SetPositionAndRotation(new Vector3(p.x,life.RoadHeight,p.z),Quaternion.LookRotation(t,Vector3.up));
    }
    void Animate(float dt,float acceleration) {
        // Steering from the curvature of the path just ahead (wheelbase 2.63 m).
        var t0=path.Tangent(s);var t1=path.Tangent(Mathf.Min(path.length,s+.6f));
        if(path is StreetNet.Lane&&plan.Count>0&&s+.6f>path.length)t1=plan[0].Tangent(s+.6f-path.length);
        float curvature=Vector3.SignedAngle(t0,t1,Vector3.up)*Mathf.Deg2Rad/.6f;
        steer=Mathf.MoveTowards(steer,Mathf.Clamp(Mathf.Atan(2.63f*curvature)*Mathf.Rad2Deg,-34,34),dt*70);
        wheelAngle=Mathf.Repeat(wheelAngle+speed/.37f*Mathf.Rad2Deg*dt,360);
        foreach(var part in parts) {
            if(!part)continue;
            if(part.partId.StartsWith("Wheel_"))part.PoseEuler(new Vector3(wheelAngle,part.partId.StartsWith("Wheel_F")?steer:0,0));
            else if(part.partId=="SteeringWheel")part.Pose(steer/34*180,Vector3.zero);
        }
        // Hand signals: a few seconds before a turn, and while braking to a stop.
        int wanted=0;var turn=NextTurn;float toEnd=path is StreetNet.Lane?path.length-s:0;
        if(turn!=null&&turn.kind!=0&&(path is StreetNet.Turn||toEnd<26))wanted=turn.kind==2?1:2;
        else if(acceleration<-.8f&&speed>1.2f)wanted=3;
        if(wanted!=armShown&&armBlend>=1){armFrom=armShown;armShown=wanted;armBlend=0;}
        if(armBones.Length>0&&armPoses.Length>=armBones.Length*4) {
            armBlend=Mathf.MoveTowards(armBlend,1,dt*2.2f);float k=Mathf.SmoothStep(0,1,armBlend);
            for(int i=0;i<armBones.Length;i++)if(armBones[i])armBones[i].localRotation=Quaternion.Slerp(armPoses[armFrom*armBones.Length+i],armPoses[armShown*armBones.Length+i],k);
        }
        Signal=armShown;
        // Lamps lit at dusk (emissive only: the twelve-light budget is the street lamps').
        if(lampBlock==null)lampBlock=new MaterialPropertyBlock();
        bool lit=life.LampsLit;
        for(int i=0;i<lampRenderers.Length;i++) {
            var r=lampRenderers[i];if(!r)continue;
            var colour=lampKinds[i]==0?(lit?new Color(1,.86f,.62f)*3.2f:Color.black):lampKinds[i]==1?(lit||speed<.05f&&Waiting!=null&&acceleration<0?new Color(1,.12f,.08f)*2.2f:Color.black):Color.black;
            r.GetPropertyBlock(lampBlock,lampSlots[i]);lampBlock.SetColor("_EmissionColor",colour);r.SetPropertyBlock(lampBlock,lampSlots[i]);
        }
        // Engine note rising with road speed, tyres on the setts, the horn.
        if(engine){engine.pitch=Mathf.Clamp((650+speed*230)/CarSounds.EngineLoopRpm,.5f,2.6f);engine.volume=.32f+.25f*Mathf.Clamp01(Mathf.Max(0,acceleration)/accel);}
        if(road){road.pitch=.7f+Mathf.Clamp01(speed/14f)*.6f;road.volume=Mathf.Clamp01(speed/9f)*.4f;}
        if(horn){honkLeft-=dt;horn.volume=honkLeft>0?.8f:0;}
    }
    public void Freeze(bool frozen) {
        foreach(var a in new[]{engine,road,horn})if(a){if(frozen&&a.isPlaying)a.Pause();else if(!frozen&&!a.isPlaying&&a.clip)a.UnPause();}
    }
}
