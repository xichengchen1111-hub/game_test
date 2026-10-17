using System.Collections.Generic;
using UnityEngine;

// A passer-by (user, 2026-10-15: more people on the streets, old and young, men and women, period
// dress; the old walk differently). Walks the footway graph (StreetNet) keeping to the right, turns
// like a person rather than sliding, gives way to people and to the player (who never gives way),
// waits at the kerb for a gap in the cars — or for the signal at Station Road × Middle Street — and
// crosses a little quicker; held up halfway by a car standing on the crossing, it goes back to the
// kerb and waits there again. Older people walk slower with a bent back, rest now and then, and the
// old gentleman plants a walking stick. In rain most carry an open umbrella. Kinematic, with a
// capsule the player cannot walk through; it checks each step before taking it, never pushing
// anyone, since nothing can push the player's controller. Not saved, gives no testimony.
[DefaultExecutionOrder(120)]
public sealed class StreetWalker : MonoBehaviour {
    public StreetDensity.Folk kind;
    public float speed=1.3f,stoop;
    public PeriodCharacterPerformance performance;
    public NpcHands hands;
    public Transform spine,neck;
    public bool Gone{get;private set;}
    public Vector3 Velocity{get;private set;}
    public string Activity{get;private set;}="walk";
    public bool Crossing=>crossingEdge!=null&&onCrossing;
    public StreetWalker partner;
    // The dog walker's terrier (StreetDog), on the lead in her left hand.
    public StreetDog dog;
    StreetLife life;System.Random rng;
    readonly List<Vector3> points=new List<Vector3>();
    readonly List<StreetNet.WalkEdge> kerb=new List<StreetNet.WalkEdge>();
    int next;StreetNet.WalkNode goal;
    float current,blocked,shift,pauseLeft,chatLeft,speakIn,restIn,lookAround;int doorTries;
    StreetNet.WalkEdge crossingEdge;bool onCrossing,waitingAtKerb,turnedBack;
    // What holds it up, for the logs, and where the hold began.
    string heldKind;Object heldBy;int heldNext;float heldDistance;
    // What it did on its last step and why, for the logs.
    string stepKind="";float wantedNow,errorNow;bool makingWay;
    // Times it has gone back from the crossing it is on (two, and it goes another way instead).
    int backsHere;
    // Held up this long on a crossing by a car standing in the way, a walker goes back to the kerb it set
    // out from (STREET-05), until it is this far off the carriageway, out of the cars' way; held up this
    // long by anybody while a car stands waiting for it, it makes way for the car.
    public const float TurnBackAfter=2f,BackOffRoad=.7f,MakeWayAfter=1.5f;
    public bool TurnedBack=>turnedBack;
    Vector3 chatFacing;
    // Walking stick: the tip stays planted while the body passes it, then swings forward.
    Transform cane;Vector3 caneTip;float caneSwing=-1;Vector3 caneFrom;
    Transform umbrella;
    Renderer[] renderers;bool shown=true,shadows=true;SkinnedMeshRenderer skin;
    public StreetNet.WalkNode Goal=>goal;
    // Most people out in the rain carry an umbrella (decided once per outing).
    public bool CarriesUmbrella{get;private set;}
    public void Begin(StreetLife owner,System.Random random,Vector3 at,Vector3 facing) {
        life=owner;rng=random;Gone=false;points.Clear();kerb.Clear();next=0;current=0;blocked=0;shift=0;pauseLeft=0;chatLeft=0;doorTries=0;crossingEdge=null;onCrossing=waitingAtKerb=turnedBack=false;partner=null;
        transform.SetPositionAndRotation(at,Quaternion.LookRotation(-Flat(facing)));
        restIn=Elderly?25+20*(float)rng.NextDouble():float.MaxValue;Activity="walk";CarriesUmbrella=rng.NextDouble()<.8;
        if(dog){bool fresh=dog.Lead==null;dog.Begin(owner,random,at,Facing);if(fresh)renderers=null;}
        if(renderers==null)renderers=GetComponentsInChildren<Renderer>(true);
        if(!skin)skin=GetComponentInChildren<SkinnedMeshRenderer>(true);
        if(kind==StreetDensity.Folk.OldMan&&!cane)MakeCane();
        if(cane){caneTip=Ground(at+Facing*.3f+Side*.3f);caneSwing=-1;}
        SetUmbrella(false);
    }
    bool Elderly=>kind==StreetDensity.Folk.OldMan||kind==StreetDensity.Folk.OldWoman;
    public Vector3 Facing{get{var f=-transform.forward;f.y=0;return f.sqrMagnitude>1e-6f?f.normalized:Vector3.forward;}}
    Vector3 Side=>Vector3.Cross(Vector3.up,Facing);
    static Vector3 Flat(Vector3 v){v.y=0;return v.sqrMagnitude>1e-6f?v.normalized:Vector3.forward;}
    // A route of nodes becomes waypoints on the walker's right of each stretch, using the offsets
    // found clear at startup; the start of each crossing is a kerb where the walker looks first.
    public void Follow(List<StreetNet.WalkNode> route,StreetNet.WalkNode destination) {
        points.Clear();kerb.Clear();next=0;goal=destination;turnedBack=false;backsHere=0;
        for(int i=0;i+1<route.Count;i++) {
            var a=route[i];var b=route[i+1];StreetNet.WalkEdge edge=null;
            foreach(var e in a.edges)if(e.Other(a)==b){edge=e;break;}
            if(edge==null)continue;
            var dir=Flat(b.point-a.point);var right=Vector3.Cross(Vector3.up,dir);
            // Offsets are stored along a→b; walking b→a, right is the other side.
            float sign=edge.a==a?1:-1,wanted=Mathf.Clamp(edge.width*.22f,.25f,.6f),best=0,bestGap=float.MaxValue;
            foreach(var o in edge.clear){float mine=o*sign,g=Mathf.Abs(mine-wanted);if(g<bestGap){bestGap=g;best=mine;}}
            points.Add(a.point+right*best);kerb.Add(edge.crosses!=null?edge:null);
            points.Add(b.point+right*best);kerb.Add(null);
        }
    }
    public void Chat(StreetWalker other,float seconds,Vector3 facing) {
        partner=other;chatLeft=seconds;chatFacing=Flat(facing);speakIn=.5f+(float)rng.NextDouble()*2;Activity="chat";points.Clear();kerb.Clear();
    }
    public bool Idle=>points.Count==0||next>=points.Count;
    public void Tick(float dt) {
        if(Gone||dt<=0)return;
        if(chatLeft>0){TickChat(dt);return;}
        if(pauseLeft>0) {
            pauseLeft-=dt;Stand(dt);
            if(lookAround!=0)Turn(Quaternion.AngleAxis(lookAround,Vector3.up)*Facing,dt,60);
            // At a doorstep: in, if nobody is watching; otherwise wait a little longer or go elsewhere.
            if(pauseLeft<=0){Activity="walk";if(Idle){if(++doorTries>2){doorTries=0;life.Replan(this);}else life.Arrived(this,goal);}}
            return;
        }
        if(Idle){Stand(dt);return;}
        // At a kerb: look along the road and go when it is safe.
        if(kerb[next]!=null&&!onCrossing&&Vector3.Distance(Flat2(transform.position),Flat2(points[next]))<.3f) {
            crossingEdge=kerb[next];
            if(!life.CanCross(crossingEdge,points[next],points[next+1],this)){if(!waitingAtKerb)life.Stats.kerbWaits++;waitingAtKerb=true;Activity="wait-kerb";heldKind=life.CrossRefusal;heldBy=life.CrossRefusalCar;Stand(dt);Turn(points[next+1]-points[next],dt,120);return;}
            waitingAtKerb=false;onCrossing=true;Activity="cross";next++;blocked=0;life.Stats.crossings++;
        }
        var target=points[next];var to=target-transform.position;to.y=0;float distance=to.magnitude;
        // Gone back off the carriageway, out of the cars' way: on to the kerb as ever, to wait there for the
        // way to clear (somebody may be standing on the kerb point itself).
        if(turnedBack&&!life.OnCarriage(transform.position,BackOffRoad)){turnedBack=false;onCrossing=false;crossingEdge=null;Activity="walk";BackOnKerb();return;}
        // Over and onto the footway at the far side: the crossing is done there, whoever stands about the
        // corner. Stopping short of somebody waiting at the kerb for the next crossing, people finishing
        // theirs queued back into the road, and the cars waited for the last of them (STREET-05: a minute
        // and more at West Lane × Residential Street).
        if(onCrossing&&!turnedBack&&kerb[next]==null&&distance<2.5f&&!life.OnCarriage(transform.position,BackOffRoad)) {
            onCrossing=false;crossingEdge=null;Activity="walk";next++;backsHere=0;
            if(Idle)Arrived();
            return;
        }
        if(distance<.25f) {
            // Back on the kerb it set out from: it waits there for the way to clear, as before setting out.
            if(turnedBack){turnedBack=false;onCrossing=false;crossingEdge=null;Activity="walk";Stand(dt);BackOnKerb();return;}
            if(onCrossing&&kerb[next]==null&&crossingEdge!=null){onCrossing=false;crossingEdge=null;Activity="walk";backsHere=0;}
            next++;
            if(Idle){Arrived();return;}
            return;
        }
        var direction=to/distance;
        // Old people stop for a rest now and then.
        restIn-=dt;
        if(restIn<=0&&!onCrossing){restIn=30+25*(float)rng.NextDouble();pauseLeft=3+5*(float)rng.NextDouble();lookAround=0;Activity="rest";return;}
        float wanted=speed*(onCrossing?1.12f:1);
        // Give way: slow behind someone slower, step aside for someone coming the other way, stop short of anyone.
        var right=Vector3.Cross(Vector3.up,direction);float shiftWanted=0,away=0;bool stop=false,backing=false;
        foreach(var o in life.Neighbours(transform.position,2.6f,this)) {
            var rel=o.position-transform.position;rel.y=0;float ahead=Vector3.Dot(rel,direction),lateral=Vector3.Dot(rel,right);
            if(ahead<=0||ahead>2.4f||Mathf.Abs(lateral)>.75f)continue;
            bool sameWay=Vector3.Dot(o.velocity,direction)>.4f*Mathf.Max(.2f,o.velocity.magnitude)&&o.velocity.magnitude>.2f;
            if(sameWay&&!o.player){if(ahead<1.4f)wanted=Mathf.Min(wanted,o.velocity.magnitude*.95f);}
            else shiftWanted=lateral>.05f?-.55f:.55f;
            if(ahead<.75f&&Mathf.Abs(lateral)<.55f) {
                stop=true;if(o.walker&&o.walker.TurnedBack)backing=true;heldKind=o.player?"the player ahead":o.walker?"somebody ahead":"a dog ahead";heldBy=o.walker;
                // Facing this way (coming the other way, or stopped face to face), or the player standing there;
                // not somebody stopped ahead going the same way, who is waited for.
                away=o.player||o.walker&&Vector3.Dot(o.facing,direction)<-.3f?(lateral>.05f?-1:1):0;
            }
        }
        shift=Mathf.MoveTowards(shift,shiftWanted,dt*.9f);
        if(stop) {
            // On a crossing, face to face with somebody (or up against the player): step aside (to the right,
            // unless they are on that side), and the two pass. Stopped face to face, two people on a crossing
            // whose ways each side lie close together stood there, and the cars waited for them (STREET-05).
            // Somebody stopped ahead going the same way is waited for: stepping round them too, two people
            // going the same way stepped round each other by turns. On the footway people wait behind each
            // other, or go round another way, as before.
            if(onCrossing&&away!=0&&!backing) {
                var aside=right*(away*Mathf.Min(speed*.6f*dt,.05f));var q=transform.position+aside;
                if(life.Free(q,this)){q.y=Mathf.MoveTowards(transform.position.y,life.GroundAt(q,transform.position.y),dt*1.2f);Velocity=(q-transform.position)/dt;transform.position=q;current=Mathf.MoveTowards(current,0,4*dt);if(performance)performance.SetLocomotion(speed*.6f);stepKind="stepping aside";Hold(dt,null,false);return;}
            }
            stepKind="stopped short";Held(dt,null,backing);return;
        }
        var aim=target+right*shift-transform.position;aim.y=0;
        // Turn at a person's rate and walk the way the body faces, slower through a sharp turn.
        Turn(aim,dt,current>.6f?210:300);
        float error=Vector3.Angle(Facing,aim);
        wanted*=Mathf.Clamp01((Mathf.Cos(error*Mathf.Deg2Rad)-.3f)/.6f);wantedNow=wanted;errorNow=error;
        current=Mathf.MoveTowards(current,wanted,(wanted>current?1.6f:3f)*dt);
        var step=Facing*Mathf.Min(current*dt,distance+.2f);
        var p=transform.position+step;
        // (Turning on the spot moves nobody into anything.)
        if(step.sqrMagnitude>1e-10f&&!life.Free(p,this,out var car)){current=0;heldKind=life.FreeBlockerKind;heldBy=life.FreeBlocker;stepKind="blocked";Held(dt,car);return;}
        stepKind=step.sqrMagnitude>1e-10f?"walking":"turning on the spot";makingWay=false;
        // Only a step taken ends a hold (STREET-05: this was cleared before the step was tried, so a
        // walker held by something solid or a car's body never counted the time).
        Moved();
        p.y=Mathf.MoveTowards(transform.position.y,life.GroundAt(p,transform.position.y),dt*1.2f);
        Velocity=(p-transform.position)/dt;transform.position=p;
        if(performance)performance.SetLocomotion(current);
    }
    static Vector2 Flat2(Vector3 v)=>new Vector2(v.x,v.z);
    // For the logs: who, where and doing what; and where they stand by a car's body, given one.
    public string Describe(Transform car=null) {
        var c=System.Globalization.CultureInfo.InvariantCulture;var p=transform.position;
        string text=name+" ("+p.x.ToString("0.00",c)+", "+p.z.ToString("0.00",c)+") "+Activity+(Crossing?" on a crossing":"")+(blocked>0?" held "+blocked.ToString("0.0",c)+"s by "+heldKind+Who(heldBy):"");
        var f=next<points.Count?points[next]:p;
        text+=" ["+stepKind+", "+current.ToString("0.00",c)+" m/s (wants "+wantedNow.ToString("0.00",c)+"), facing "+errorNow.ToString("0",c)+"° off, heading for ("+f.x.ToString("0.00",c)+", "+f.z.ToString("0.00",c)+")"+(stepKind=="stepping aside"||stepKind=="stopped short"?" by "+heldKind+Who(heldBy):"")+"]";
        if(!car)return text;
        var local=car.InverseTransformPoint(p);local.z+=AmbientCar.CentreBack;
        return text+", "+local.x.ToString("0.00",c)+" m across and "+local.z.ToString("0.00",c)+" m along from the middle of its body, "+AmbientCar.PointGap(car.position,car.forward,p).ToString("0.00",c)+" m off it";
    }
    static string Who(Object what) {
        if(!what)return "";var c=System.Globalization.CultureInfo.InvariantCulture;
        var t=what is Component k?k.transform:null;var at=t?" at ("+t.position.x.ToString("0.00",c)+", "+t.position.z.ToString("0.00",c)+")":"";
        string state="";
        if(what is StreetWalker w)state=" "+w.Activity+(w.Crossing?" on a crossing":"")+(w.Idle?", going nowhere":"")+
            (w.Activity=="wait-kerb"?" for "+w.heldKind:w.blocked>0?", held "+w.blocked.ToString("0.0",c)+"s by "+w.heldKind:"");
        return " ("+what.name+at+state+")";
    }
    void Stand(float dt) {
        current=Mathf.MoveTowards(current,0,4*dt);Velocity=Vector3.zero;
        if(performance)performance.SetLocomotion(0);
    }
    void Turn(Vector3 toward,float dt,float rate) {
        toward.y=0;if(toward.sqrMagnitude<1e-6f)return;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-toward.normalized),rate*dt);
    }
    // Held up by somebody, something solid or a car's body (null: not a car). On the footway, after a
    // while, go round another way (often the player stands there). On a crossing, where the cars wait
    // for whoever is on it, a car standing in the way sends the walker back to the kerb, to set out
    // again when the way is clear, and anyone coming back from one is made way for (STREET-05: held
    // beside a car standing on the crossing, which waited for them in turn, a walker stood until one of
    // the two was recycled out of sight). Held up by people while a car stands waiting for this walker,
    // it gets out of the car's way: back to the kerb from a crossing, a step aside on the footway.
    // Otherwise people on a crossing are waited for, or stepped round. A car going by clears the way itself.
    void Held(float dt,AmbientCar car,bool backing=false){Stand(dt);Hold(dt,car,backing);}
    void Hold(float dt,AmbientCar car,bool backing) {
        if(car&&car.speed>.3f)return;
        if(blocked==0){heldNext=next;heldDistance=Flat2(points[next]-transform.position).magnitude;}
        blocked+=dt;
        // Held up by somebody while a car stands waiting for this walker: out of the car's way (STREET-05:
        // people bunched where a turning car cuts the corner, and the car waited for them a minute).
        var waiting=!car&&blocked>MakeWayAfter?life.CarWaitingFor(this):null;
        if(onCrossing) {
            if(car){if(blocked>TurnBackAfter){TurnBack(car,"held by");blocked=0;}}
            else if(backing&&!turnedBack&&blocked>.6f){TurnBack(null,"making way for somebody coming back");blocked=0;}
            else if(waiting&&!turnedBack){TurnBack(waiting,"making way for");blocked=0;}
        }
        else if(waiting&&StepAway(dt,waiting)){}
        else if(blocked>3.5f){blocked=0;life.Replan(this);}
    }
    // Off the carriageway, a step away from the way a car standing for this walker is about to drive, and
    // never onto a carriageway.
    bool StepAway(float dt,AmbientCar car) {
        var me=transform.position;var away=me-car.NearestOnWay(me,25);away.y=0;
        if(away.sqrMagnitude<1e-4f)return false;
        var q=me+away.normalized*Mathf.Min(speed*.6f*dt,.05f);
        if(life.OnCarriage(q,.3f)||!life.Free(q,this))return false;
        if(!makingWay){makingWay=true;life.MadeWay(this,car);}
        q.y=Mathf.MoveTowards(me.y,life.GroundAt(q,me.y),dt*1.2f);Velocity=(q-me)/dt;transform.position=q;
        if(performance)performance.SetLocomotion(speed*.6f);stepKind="making way for a car";
        return true;
    }
    // A hold ends once the walker has got on: 15 cm nearer the point it makes for, or past it. Creeping a
    // hair at a time up to somebody standing in front (stopping and starting on the 0.75 m line), or
    // stepping aside and drifting back, is still being held (STREET-05: a boy at a corner did the one, and
    // an old man at a kerb the other, for minutes; neither went round, nor made way for the car waiting).
    void Moved(){if(blocked>0&&(next!=heldNext||Flat2(points[next]-transform.position).magnitude<heldDistance-.15f))blocked=0;}
    // Back the way it came along the crossing (or on across again, if that way is blocked by a car as well).
    void TurnBack(AmbientCar car,string why) {
        turnedBack=!turnedBack;next+=turnedBack?-1:1;Activity=turnedBack?"turn-back":"cross";
        if(turnedBack){backsHere++;life.TurnedBack(this,car,why);}
    }
    // Back on the kerb from a crossing: a moment before trying again, and after going back twice from the
    // same crossing, another way instead (one man crossed back and forth five times in front of a busy
    // corner, STREET-05).
    void BackOnKerb() {
        if(backsHere>=2){backsHere=0;life.Replan(this);return;}
        pauseLeft=2+(float)rng.NextDouble();lookAround=0;Activity="wait-kerb";
    }
    void Arrived() {
        current=0;if(performance)performance.SetLocomotion(0);
        life.Arrived(this,goal);
    }
    // A shop window, a doorstep: stand a moment, then go on.
    public void Pause(float seconds,Vector3 face,string what){pauseLeft=seconds;lookAround=0;Activity=what;if(face.sqrMagnitude>0)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-Flat(face)),30);}
    public void Despawn(){Gone=true;Activity="gone";}
    void TickChat(float dt) {
        chatLeft-=dt;Stand(dt);Turn(chatFacing,dt,140);
        speakIn-=dt;
        if(speakIn<=0&&performance) {
            // Taking turns: speak a few seconds, then listen while the other speaks.
            if(performance.IsSpeaking){performance.Listen();speakIn=2.5f+2*(float)rng.NextDouble();}
            else if(!partner||!partner.performance||!partner.performance.IsSpeaking){performance.Speak(life.ChatLine(rng));speakIn=2.5f+3*(float)rng.NextDouble();}
            else speakIn=1;
        }
        if(chatLeft<=0){if(performance)performance.Listen();partner=null;Activity="walk";life.Replan(this);}
    }
    // ---- The walking stick ----
    void MakeCane() {
        var stick=new GameObject("Walking stick");stick.transform.SetParent(transform,false);stick.layer=gameObject.layer;
        var shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shaft.name="Stick shaft";Object.Destroy(shaft.GetComponent<Collider>());
        shaft.transform.SetParent(stick.transform,false);shaft.transform.localScale=new Vector3(.024f,.43f,.024f);shaft.transform.localPosition=new Vector3(0,-.43f,0);
        var crook=GameObject.CreatePrimitive(PrimitiveType.Cylinder);crook.name="Stick crook";Object.Destroy(crook.GetComponent<Collider>());
        crook.transform.SetParent(stick.transform,false);crook.transform.localScale=new Vector3(.026f,.06f,.026f);crook.transform.localPosition=new Vector3(0,.0f,.05f);crook.transform.localRotation=Quaternion.Euler(90,0,0);
        var mat=life.Material(new Color(.22f,.14f,.08f));
        foreach(var r in stick.GetComponentsInChildren<Renderer>()){r.sharedMaterial=mat;r.renderingLayerMask=1;}
        foreach(var t in stick.GetComponentsInChildren<Transform>())t.gameObject.layer=gameObject.layer;
        cane=stick.transform;renderers=null;
    }
    void UpdateCane(float dt) {
        if(!cane||!hands)return;
        var body=transform.position;var side=Side;var forward=Facing;
        float along=Vector3.Dot(caneTip-body,forward);
        // Planted until the body is past it, then swung to a little ahead for the next step.
        if(caneSwing<0&&(along<-.22f||Vector3.Distance(Flat2(caneTip),Flat2(body+side*.3f+forward*along))>.25f)&&current>.05f){caneSwing=0;caneFrom=caneTip;}
        if(caneSwing>=0) {
            caneSwing+=dt/.42f;var land=Ground(body+forward*(.32f+current*.2f)+side*.3f);
            caneTip=Vector3.Lerp(caneFrom,land,Mathf.SmoothStep(0,1,caneSwing))+Vector3.up*.07f*Mathf.Sin(Mathf.Clamp01(caneSwing)*Mathf.PI);
            if(caneSwing>=1){caneSwing=-1;caneTip=land;}
        }
        // The handle sits where the hand rests by the hip, the stick from there to the tip.
        var ideal=body+side*.27f+forward*.08f+Vector3.up*(.86f-stoop*.004f);
        var stick=(ideal-caneTip).normalized;var handle=caneTip+stick*.86f;
        cane.position=handle;cane.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(forward,stick).normalized,stick);
        hands.Reach(true,handle,.3f);hands.SetGrip(true,NpcHands.Grip.Hold);
    }
    Vector3 Ground(Vector3 p){p.y=life.GroundAt(p,transform.position.y);return p;}
    // ---- Umbrella in the rain ----
    public void SetUmbrella(bool open) {
        if(open==(umbrella&&umbrella.gameObject.activeSelf))return;
        if(open&&!umbrella){umbrella=life.MakeUmbrella(transform);renderers=null;shown=true;}
        if(umbrella)umbrella.gameObject.SetActive(open);
        if(!open&&hands&&!cane)hands.Release(true,.4f);
    }
    void UpdateUmbrella() {
        if(!umbrella||!umbrella.gameObject.activeSelf||!hands||cane)return;
        // Held at the chest a little in front, the canopy just clear of the hat.
        var handle=transform.position+Facing*.26f+Side*.08f+Vector3.up*1.3f;
        umbrella.position=handle;umbrella.rotation=Quaternion.LookRotation(Facing,Vector3.up);
        hands.Reach(true,handle,.4f);hands.SetGrip(true,NpcHands.Grip.Hold);
    }
    // ---- After the clip: the bent back of the old, the stick and the umbrella ----
    void LateUpdate() {
        if(Gone)return;
        float dt=DemoApp.Instance&&DemoApp.Instance.GameTimePaused?0:Time.deltaTime;
        // Only on a pose the Animator has just written (off screen it stops writing, and the bend would build up).
        if(stoop>0&&spine&&(skin?skin.isVisible:true)) {
            spine.rotation=Quaternion.AngleAxis(stoop,Side)*spine.rotation;
            if(neck)neck.rotation=Quaternion.AngleAxis(-stoop*.55f,Side)*neck.rotation;
        }
        UpdateCane(dt);UpdateUmbrella();
    }
    // Far ones are hidden and lose their shadows (fog hides them anyway).
    public void SetDetail(bool visible,bool castShadows) {
        if(renderers==null)renderers=GetComponentsInChildren<Renderer>(true);
        if(visible!=shown){shown=visible;foreach(var r in renderers)if(r)r.enabled=visible;}
        if(castShadows!=shadows){shadows=castShadows;foreach(var r in renderers)if(r)r.shadowCastingMode=castShadows?UnityEngine.Rendering.ShadowCastingMode.On:UnityEngine.Rendering.ShadowCastingMode.Off;}
    }
}
