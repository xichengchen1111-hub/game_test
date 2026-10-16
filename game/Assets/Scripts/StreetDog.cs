using UnityEngine;

// A wire fox terrier on a lead beside its owner (user, 2026-10-15: "有遛狗的"; the dog made in
// Tripo, approved 2026-10-16). Tripo's motions are humanoid only, so the dog is moved here from its
// own bones once its owner has moved: a trot on diagonal pairs whose stride follows its ground speed,
// the back rising and falling a little, the head carried up and turning to look about; at a stop it
// stands, breathes, looks round and now and then up at its owner. It keeps to the owner's left,
// falls in behind where the side is not clear (or the player stands there), never strays more than
// the lead allows, and the lead runs from the owner's left hand to the collar, sagging when slack.
// Kinematic with a small body the player cannot walk through. Not saved; it never leaves the owner.
[DefaultExecutionOrder(160)]
public sealed class StreetDog : MonoBehaviour {
    public StreetWalker owner;
    public Transform visual,hip,spine1,spine2,neck1,neck2,head;
    // Fore left, fore right, hind left, hind right: upper, lower and paw of each.
    public Transform[] legs=new Transform[12];
    public float length=.72f,stride=.62f;
    public float legSwing=24,foreFold=62,hindFold=48;
    const float LeadLength=1.5f,Side=.5f;
    Quaternion[] rest;Transform[] posed;Vector3 visualRest;
    Vector3 position,heading=Vector3.forward;float phase,speed,walkWeight,breath;
    float lookIn,lookYaw,lookPitch,yawGoal,pitchGoal;
    LineRenderer lead;Transform ownerHand,ownerForearm;
    readonly Vector3[] leadPoints=new Vector3[12];
    StreetLife life;System.Random rng;
    public Vector3 Position=>position;
    public float Speed=>speed;
    public bool LeadTaut{get;private set;}
    public LineRenderer Lead=>lead;

    void Awake() {
        var body=new[]{hip,spine1,spine2,neck1,neck2,head};
        posed=new Transform[body.Length+legs.Length];body.CopyTo(posed,0);legs.CopyTo(posed,body.Length);
        rest=new Quaternion[posed.Length];for(int i=0;i<posed.Length;i++)if(posed[i])rest[i]=posed[i].localRotation;
        if(visual)visualRest=visual.localPosition;
    }
    public void Begin(StreetLife streets,System.Random random,Vector3 at,Vector3 facing) {
        life=streets;rng=random;
        if(!owner)owner=GetComponentInParent<StreetWalker>();
        heading=Flat(facing);position=Clear(at+Left(heading)*Side+heading*.15f,at)?at+Left(heading)*Side+heading*.15f:at-heading*.7f;
        position.y=life.GroundAt(position,at.y+.2f);
        phase=(float)rng.NextDouble();speed=0;walkWeight=0;lookIn=1+2*(float)rng.NextDouble();lookYaw=lookPitch=yawGoal=pitchGoal=0;
        if(!ownerHand&&owner) {
            foreach(var t in owner.GetComponentsInChildren<Transform>(true)){if(t.name=="Left_Hand")ownerHand=t;if(t.name=="Left_LowerArm")ownerForearm=t;}
        }
        if(!lead)MakeLead();
        Place();
    }
    static Vector3 Flat(Vector3 v){v.y=0;return v.sqrMagnitude>1e-6f?v.normalized:Vector3.forward;}
    static Vector3 Left(Vector3 facing)=>-Vector3.Cross(Vector3.up,facing);
    // Static world only (not people, cars or rain triggers): a dog-sized body standing at p.
    bool Clear(Vector3 p,Vector3 owner) {
        p.y=owner.y;
        return !Physics.CheckCapsule(p+Vector3.up*.2f,p+Vector3.up*.42f,.13f,~((1<<StreetLife.Layer)|(1<<2)),QueryTriggerInteraction.Ignore);
    }
    void LateUpdate() {
        if(!owner||owner.Gone||life==null)return;
        float dt=DemoApp.Instance&&DemoApp.Instance.GameTimePaused?0:Time.deltaTime;
        if(dt>0)Move(dt);
        Place();Pose(dt);UpdateLead();
    }
    // Where to be: on the owner's left and a little ahead while walking, beside the knee when standing;
    // closer in, or behind the owner on the walked line, when the side is blocked by a wall, somebody
    // else, another dog or the player; it never steps into anyone.
    void Move(float dt) {
        var o=owner.transform.position;var facing=owner.Facing;bool walking=owner.Velocity.sqrMagnitude>.04f;
        bool Spot(Vector3 p)=>Clear(p,o)&&life.DogRoom(p,owner);
        var want=o+Left(facing)*Side+facing*(walking?.22f:.05f);
        if(!Spot(want)) {
            var close=o+Left(facing)*.3f+facing*.1f;var behind=o-facing*.75f;
            want=Spot(close)?close:Spot(behind)?behind:position;
        }
        var to=want-position;to.y=0;float distance=to.magnitude;
        // Back on its spot at once if anything (pooling, a teleport) put it far away.
        if(distance>LeadLength+1){position=want;speed=0;return;}
        float top=Mathf.Max(owner.speed*1.4f,1.9f);
        float wanted=Mathf.Clamp(distance*2.6f-.05f,0,top);
        speed=Mathf.MoveTowards(speed,wanted,(wanted>speed?3.5f:6f)*dt);
        if(distance>.02f) {
            var dir=to/distance;var step=dir*Mathf.Min(speed*dt,distance);
            var next=position+step;
            if(life.DogRoom(next,owner))position=next;
            else {
                // Round whoever is in the way, on the owner's side of them.
                var round=Vector3.Cross(Vector3.up,dir)*Mathf.Sign(Vector3.Dot(o-position,Vector3.Cross(Vector3.up,dir))+1e-4f);
                var aside=position+round*Mathf.Min(speed*dt,.05f);
                if(life.DogRoom(aside,owner)&&Clear(aside,o))position=aside;else speed=0;
            }
            if(speed>.12f)heading=Vector3.Slerp(heading,dir,Mathf.Clamp01(dt*8f)).normalized;
        }
        if(speed<.12f)heading=Vector3.Slerp(heading,facing,Mathf.Clamp01(dt*2.2f)).normalized;
        position.y=Mathf.MoveTowards(position.y,life.GroundAt(position,o.y+.3f),dt*1.5f);
        phase=Mathf.Repeat(phase+dt*speed/stride,1);
        walkWeight=Mathf.MoveTowards(walkWeight,Mathf.Clamp01((speed-.05f)/.35f),dt*4f);
    }
    void Place() {
        // The model faces the root's -Z, like the period characters.
        transform.SetPositionAndRotation(position,Quaternion.LookRotation(-heading,Vector3.up));
    }
    // A trot: fore left with hind right, fore right with hind left. Each foot is on the ground for a
    // little over half the cycle, moving back under the body, then lifts, folds and swings forward.
    void Pose(float dt) {
        for(int i=0;i<posed.Length;i++)if(posed[i])posed[i].localRotation=rest[i];
        var side=Vector3.Cross(Vector3.up,heading);var up=Vector3.up;
        float[] offsets={0,.5f,.5f,0};
        for(int leg=0;leg<4;leg++) {
            var upper=legs[leg*3];var lower=legs[leg*3+1];var paw=legs[leg*3+2];if(!upper||!lower)continue;
            float t=Mathf.Repeat(phase+offsets[leg],1),swing,fold=0;const float stance=.56f;
            if(t<stance)swing=Mathf.Cos(Mathf.PI*t/stance);
            else{float s=(t-stance)/(1-stance);swing=-Mathf.Cos(Mathf.PI*s);fold=Mathf.Sin(Mathf.PI*s);}
            swing*=walkWeight;fold*=walkWeight;
            bool fore=leg<2;
            // Positive swing carries the paw forward.
            upper.rotation=Quaternion.AngleAxis(-legSwing*swing,side)*upper.rotation;
            float bend=(fore?foreFold:hindFold)*fold;
            lower.rotation=Quaternion.AngleAxis(bend,side)*lower.rotation;
            if(paw)paw.rotation=Quaternion.AngleAxis(-bend*(fore?.55f:.35f),side)*paw.rotation;
        }
        // The back rises twice a cycle and rolls a little toward the leg pair in the air.
        if(visual)visual.localPosition=visualRest+Vector3.up*(.007f*walkWeight*Mathf.Cos(phase*Mathf.PI*4));
        if(spine1)spine1.rotation=Quaternion.AngleAxis(2.2f*walkWeight*Mathf.Sin(phase*Mathf.PI*2),heading)*spine1.rotation;
        // Breathing at rest, quicker after walking.
        breath+=dt*(1/2.2f+walkWeight*.6f);
        if(spine2)spine2.rotation=Quaternion.AngleAxis(.9f*(1-walkWeight)*Mathf.Sin(breath*Mathf.PI*2),side)*spine2.rotation;
        // Looking about: at a stop now here, now there, now up at the owner; walking, mostly ahead.
        lookIn-=dt;
        if(lookIn<=0&&rng!=null) {
            bool atOwner=walkWeight<.5f&&rng.NextDouble()<.3;
            if(atOwner&&owner) {
                var toOwner=Flat(owner.transform.position-position);
                yawGoal=Mathf.Clamp(Vector3.SignedAngle(heading,toOwner,Vector3.up),-60,60);pitchGoal=24;
            } else {
                float range=walkWeight>.5f?14:48;
                yawGoal=(float)(rng.NextDouble()*2-1)*range;pitchGoal=walkWeight>.5f?2:(float)(rng.NextDouble()*16-6);
            }
            lookIn=walkWeight>.5f?2+2*(float)rng.NextDouble():1.6f+3.4f*(float)rng.NextDouble();
        }
        lookYaw=Mathf.MoveTowards(lookYaw,yawGoal,dt*70);lookPitch=Mathf.MoveTowards(lookPitch,pitchGoal,dt*45);
        float nod=1.6f*walkWeight*Mathf.Sin(phase*Mathf.PI*4+.6f);
        var neck=neck1?neck1:neck2;
        if(neck)neck.rotation=Quaternion.AngleAxis(lookYaw*.45f,up)*Quaternion.AngleAxis(-(lookPitch*.4f),side)*neck.rotation;
        if(head)head.rotation=Quaternion.AngleAxis(lookYaw*.55f,up)*Quaternion.AngleAxis(-(lookPitch*.6f+nod),side)*head.rotation;
    }
    // ---- The lead ----
    void MakeLead() {
        var go=new GameObject("Lead");go.transform.SetParent(transform.parent?transform.parent:transform,false);go.layer=gameObject.layer;
        lead=go.AddComponent<LineRenderer>();lead.positionCount=leadPoints.Length;lead.useWorldSpace=true;
        lead.widthMultiplier=.011f;lead.numCapVertices=2;lead.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        lead.sharedMaterial=life.Material(new Color(.2f,.12f,.07f));lead.renderingLayerMask=1;
    }
    void UpdateLead() {
        if(!lead||!owner)return;
        // The handle in the owner's left hand at the hip; the hand reaches for it every frame.
        var o=owner.transform.position;var facing=owner.Facing;
        var grip=o+Left(facing)*.22f+facing*.16f+Vector3.up*.84f;
        if(owner.hands){owner.hands.Reach(false,grip,.4f);owner.hands.SetGrip(false,NpcHands.Grip.Hold);}
        var a=grip;
        if(ownerHand&&ownerForearm&&owner.hands&&owner.hands.Weight(false)>.5f)a=ownerHand.position+(ownerHand.position-ownerForearm.position).normalized*.06f;
        var b=(neck1?neck1.position:transform.position+Vector3.up*.38f)+heading*.03f-Vector3.up*.02f;
        float span=Vector3.Distance(a,b),slack=Mathf.Max(0,LeadLength-span);LeadTaut=slack<.05f;
        float sag=Mathf.Clamp(slack*.55f,.015f,.5f);
        for(int i=0;i<leadPoints.Length;i++) {
            float t=i/(float)(leadPoints.Length-1);
            leadPoints[i]=Vector3.Lerp(a,b,t)-Vector3.up*(sag*4*t*(1-t));
            // A lead that sags to the paving lies on it.
            float floor=Mathf.Min(a.y,b.y)-.5f;if(leadPoints[i].y<position.y+.01f)leadPoints[i].y=Mathf.Max(floor,position.y+.01f);
        }
        lead.SetPositions(leadPoints);
    }
    public void SetDetail(bool visible){if(lead)lead.enabled=visible;}
}
