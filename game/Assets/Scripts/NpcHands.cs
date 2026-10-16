using UnityEngine;

// Hands that take hold of things (user, 2026-10-15: the guard knocked on the cell door, unlocked it
// and fetched the briefcase without ever moving a hand). After the Animator and NpcPose have posed
// the body, each arm is solved as a two-bone chain that puts the palm where it is wanted. A weight
// eases every reach in and out, so it grows out of whatever the body was doing; the fingers close
// into a grip, a pinch or a fist, and the back can bend a little to reach low or far. Callers set
// the target every frame while they hold it. Explicit pause freezes the easing with the cast.
[DefaultExecutionOrder(150)]
public sealed class NpcHands : MonoBehaviour {
    public enum Grip { Open, Hold, Pinch, Fist }
    sealed class Arm {
        public bool right;
        public Transform upper,lower,hand;
        public Transform[][] fingers=new Transform[4][];public Transform[] thumb;
        // Hand-local directions measured from the rig: along the middle finger, out of the palm.
        public Vector3 fingerLocal,palmLocal,palmOffset;
        // The shoulder joint in the body's frame (right, up, forward), measured from the rig at rest.
        public Vector3 shoulder;
        public Vector3 target,hint;public bool hasHint;
        public Vector3 fingerWorld,palmWorld;public bool orient;
        public float weight,goal,rate=1/.35f,curl,error;public Grip grip;
        public float Reach=>upper&&lower&&hand?Vector3.Distance(upper.position,lower.position)+Vector3.Distance(lower.position,hand.position):.55f;
    }
    readonly Arm left=new Arm{right=false},right=new Arm{right=true};
    Transform spine,chest;
    float lean,leanGoal;
    bool ready;
    // Called after the arms are solved each frame: something held moves with the palm.
    public System.Action AfterSolve;
    Arm Of(bool rightHand)=>rightHand?right:left;
    // The way the body faces (the cast's chest faces the root's -Z) and its right-hand side.
    public Vector3 Facing{get{var f=-transform.forward;f.y=0;return f.sqrMagnitude>1e-6f?f.normalized:Vector3.forward;}}
    public Vector3 Side=>Vector3.Cross(Vector3.up,Facing);
    void Awake()=>Resolve();
    void Resolve() {
        if(ready)return;
        Transform Find(string name){foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name==name)return t;return null;}
        foreach(var arm in new[]{left,right}) {
            string s=arm.right?"Right_":"Left_";
            arm.upper=Find(s+"UpperArm");arm.lower=Find(s+"LowerArm");arm.hand=Find(s+"Hand");
            string[] names={"Index","Middle","Ring","Pinky"};
            for(int i=0;i<4;i++)arm.fingers[i]=new[]{Find(s+names[i]+"Proximal"),Find(s+names[i]+"Intermediate"),Find(s+names[i]+"Distal")};
            arm.thumb=new[]{Find(s+"ThumbProximal"),Find(s+"ThumbIntermediate"),Find(s+"ThumbDistal")};
            if(!arm.hand)continue;
            var middle=arm.fingers[1][0];var index=arm.fingers[0][0];var pinky=arm.fingers[3][0];
            Vector3 finger=middle?(middle.position-arm.hand.position).normalized:(arm.hand.position-arm.lower.position).normalized;
            Vector3 across=index&&pinky?(index.position-pinky.position):Vector3.Cross(finger,Vector3.up);
            // Right hand: along the fingers × index-to-pinky points out of the palm; mirrored on the left.
            var palm=Vector3.Cross(finger,across).normalized*(arm.right?1:-1);
            arm.fingerLocal=Quaternion.Inverse(arm.hand.rotation)*finger;
            arm.palmLocal=Quaternion.Inverse(arm.hand.rotation)*palm;
            float length=middle?Vector3.Distance(middle.position,arm.hand.position):.09f;
            arm.palmOffset=Quaternion.Inverse(arm.hand.rotation)*(finger*length*.62f+palm*.022f);
            if(arm.upper){var d=arm.upper.position-transform.position;arm.shoulder=new Vector3(Vector3.Dot(d,Side),d.y,Vector3.Dot(d,Facing));}
        }
        spine=Find("Spine");chest=Find("Chest");
        ready=left.hand&&right.hand;
    }
    // ---- What the hands are asked to do ----
    public void Reach(bool rightHand,Vector3 palmTarget,float blendSeconds=.35f){var a=Of(rightHand);a.target=palmTarget;a.goal=1;a.rate=1/Mathf.Max(.01f,blendSeconds);}
    public void Hint(bool rightHand,Vector3 elbowToward){var a=Of(rightHand);a.hint=elbowToward;a.hasHint=true;}
    public void Orient(bool rightHand,Vector3 fingers,Vector3 palm){var a=Of(rightHand);a.fingerWorld=fingers.normalized;a.palmWorld=Vector3.ProjectOnPlane(palm,fingers).normalized;a.orient=a.palmWorld.sqrMagnitude>.5f;}
    public void SetGrip(bool rightHand,Grip grip)=>Of(rightHand).grip=grip;
    public void Release(bool rightHand,float seconds=.4f){var a=Of(rightHand);a.goal=0;a.rate=1/Mathf.Max(.01f,seconds);}
    public void ReleaseAll(float seconds=.4f){Release(true,seconds);Release(false,seconds);leanGoal=0;}
    public void Stop(){foreach(var a in new[]{left,right}){a.goal=a.weight=0;a.curl=0;a.orient=a.hasHint=false;}leanGoal=lean=0;}
    public void Lean(float degrees)=>leanGoal=degrees;
    public float Weight(bool rightHand)=>Of(rightHand).weight;
    // How far the palm ended up from where it was asked to be, after the last solve (out of reach, or blending).
    public float Error(bool rightHand)=>Of(rightHand).error;
    public float ArmLength(bool rightHand){Resolve();return Of(rightHand).Reach;}
    // From the shoulder to the middle of the palm with the arm straight.
    public float PalmReach(bool rightHand){Resolve();var a=Of(rightHand);return a.Reach+a.palmOffset.magnitude;}
    public Vector3 ShoulderOffset(bool rightHand){Resolve();return Of(rightHand).shoulder;}
    // Where to stand (on the floor) facing `facing` so the shoulder of that hand is `ahead` metres
    // short of the point, level with it sideways; `inward` moves the body further from it sideways
    // (to keep clear of a door jamb). People this size have short arms: they stand close.
    public Vector3 StandFor(bool rightHand,Vector3 point,Vector3 facing,float ahead,float inward=0) {
        Resolve();facing.y=0;facing.Normalize();var side=Vector3.Cross(Vector3.up,facing);var s=Of(rightHand).shoulder;
        var shoulder=new Vector3(point.x,0,point.z)-facing*ahead;
        var feet=shoulder-side*s.x-facing*s.z-(rightHand?side:-side)*inward;feet.y=transform.position.y;return feet;
    }
    public bool Busy=>left.weight>0||right.weight>0||left.goal>0||right.goal>0;
    public Transform Hand(bool rightHand){Resolve();return Of(rightHand).hand;}
    // The middle of the palm, where a handle or a key sits.
    public Vector3 Palm(bool rightHand){Resolve();var a=Of(rightHand);return a.hand?a.hand.position+a.hand.rotation*a.palmOffset:transform.position+Vector3.up;}
    public Vector3 Shoulder(bool rightHand){Resolve();var a=Of(rightHand);return a.upper?a.upper.position:transform.position+Vector3.up*1.45f;}
    // Whether a point lies within comfortable reach of a shoulder (fraction of the arm's length).
    public bool Within(bool rightHand,Vector3 point,float fraction=.95f){Resolve();var a=Of(rightHand);return Vector3.Distance(Shoulder(rightHand),point)<=a.Reach*fraction+.09f;}
    // The hand on the side of the body the point lies on.
    public bool RightOf(Vector3 point)=>Vector3.Dot(point-transform.position,Side)>=0;
    void LateUpdate() {
        Resolve();if(!ready)return;
        var app=DemoApp.Instance;bool frozen=app&&app.NPCAnimationPaused;
        float dt=frozen?0:Time.deltaTime;
        // Bending forward as far as a reach needs (beyond the arm's length), never more than the waist allows.
        float needed=0;
        foreach(var a in new[]{right,left}) {
            if(a.goal<=0&&a.weight<=0)continue;
            var wrist=a.target-(a.orient?Quaternion.LookRotation(a.fingerWorld,Vector3.ProjectOnPlane(a.palmWorld,a.fingerWorld))*Quaternion.Inverse(Quaternion.LookRotation(a.fingerLocal,a.palmLocal)):a.hand.rotation)*a.palmOffset;
            float over=Vector3.Distance(a.upper.position,wrist)-a.Reach*.96f;
            if(over>0)needed=Mathf.Max(needed,Mathf.Min(24,over/.42f*Mathf.Rad2Deg*1.15f)*Mathf.Max(a.weight,.2f));
        }
        lean=Mathf.MoveTowards(lean,Mathf.Max(leanGoal,needed),dt*35);
        if(Mathf.Abs(lean)>.01f) {
            // Bending forward at the waist and the chest: up tips toward the way the body faces.
            var axis=Side;
            if(spine)spine.rotation=Quaternion.AngleAxis(lean*.55f,axis)*spine.rotation;
            if(chest)chest.rotation=Quaternion.AngleAxis(lean*.45f,axis)*chest.rotation;
        }
        foreach(var a in new[]{right,left}) {
            a.weight=Mathf.MoveTowards(a.weight,a.goal,dt*a.rate);
            float curlGoal=a.goal>0?1:0;a.curl=Mathf.MoveTowards(a.curl,curlGoal,dt*6);
            if(a.weight<=0){if(a.goal<=0){a.orient=false;a.hasHint=false;}continue;}
            Solve(a,Mathf.SmoothStep(0,1,a.weight));
        }
        AfterSolve?.Invoke();
    }
    void Solve(Arm a,float w) {
        Vector3 wrist0=a.hand.position,elbow0=a.lower.position;Quaternion hand0=a.hand.rotation;
        var handGoal=hand0;
        if(a.orient) {
            var fingers=a.fingerWorld;var palm=Vector3.ProjectOnPlane(a.palmWorld,fingers).normalized;
            if(palm.sqrMagnitude>.5f)handGoal=Quaternion.LookRotation(fingers,palm)*Quaternion.Inverse(Quaternion.LookRotation(a.fingerLocal,a.palmLocal));
        }
        var wristGoal=a.target-handGoal*a.palmOffset;
        // The elbow bends down and out, a little behind the hand, unless told otherwise.
        var side=a.right?Side:-Side;
        var hint=a.hasHint?a.hint:a.upper.position-Vector3.up*.35f+side*.28f-Facing*.12f;
        TwoBone(a.upper,a.lower,a.hand,Vector3.Lerp(wrist0,wristGoal,w),Vector3.Lerp(elbow0,hint,w));
        a.hand.rotation=Quaternion.Slerp(hand0,handGoal,w);
        a.error=Vector3.Distance(a.hand.position+a.hand.rotation*a.palmOffset,a.target);
        Curl(a,w*Mathf.SmoothStep(0,1,a.curl));
    }
    static void TwoBone(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint) {
        Vector3 root=upper.position;
        float a=Vector3.Distance(root,lower.position),b=Vector3.Distance(lower.position,end.position);
        var to=target-root;if(to.sqrMagnitude<1e-8f)return;
        float d=Mathf.Clamp(to.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);var forward=to.normalized;
        var bend=Vector3.ProjectOnPlane(hint-root,forward);
        if(bend.sqrMagnitude<1e-6f)bend=Vector3.ProjectOnPlane(lower.position-root,forward);
        if(bend.sqrMagnitude<1e-6f)return;
        bend.Normalize();
        float along=(a*a+d*d-b*b)/(2*d);
        var joint=root+forward*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-root,joint-root)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,root+forward*d-lower.position)*lower.rotation;
    }
    // Degrees for the three joints of each finger, and of the thumb.
    static readonly float[][] FingerCurl={new[]{0f,0,0},new[]{50f,62,40},new[]{40f,55,40},new[]{82f,96,64}};
    static readonly float[][] ThumbCurl={new[]{0f,0,0},new[]{18f,22,16},new[]{26f,32,26},new[]{34f,40,30}};
    void Curl(Arm a,float amount) {
        if(amount<=.001f||a.grip==Grip.Open)return;
        var palm=a.hand.rotation*a.palmLocal;var along=a.hand.rotation*a.fingerLocal;
        var axis=Vector3.Cross(along,palm).normalized;
        var degrees=FingerCurl[(int)a.grip];
        for(int f=0;f<4;f++) {
            // A pinch holds the key between the thumb and the side of the bent index; the rest close more.
            float extra=a.grip==Grip.Pinch&&f>0?1.25f:1;
            for(int j=0;j<3;j++)if(a.fingers[f][j])a.fingers[f][j].rotation=Quaternion.AngleAxis(degrees[j]*amount*extra,axis)*a.fingers[f][j].rotation;
        }
        var thumb=ThumbCurl[(int)a.grip];
        if(a.thumb[0]) {
            var thumbAlong=(a.thumb[1]?a.thumb[1].position:a.thumb[0].position+along*.03f)-a.thumb[0].position;
            var thumbAxis=Vector3.Cross(thumbAlong,palm).normalized;
            if(thumbAxis.sqrMagnitude>.5f)for(int j=0;j<3;j++)if(a.thumb[j])a.thumb[j].rotation=Quaternion.AngleAxis(thumb[j]*amount,thumbAxis)*a.thumb[j].rotation;
        }
    }
}
