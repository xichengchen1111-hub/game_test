using UnityEngine;
// Stable roles separate the opening from the exploration duty officer.
public sealed class OpeningActor : Interactable {
    public string role;
    bool walking,poseApplied;
    Quaternion leftLegPose,rightLegPose,leftArmPose,rightArmPose;
    float stride;
    Transform leftLeg,rightLeg,leftArm,rightArm;
    PeriodCharacterPerformance performance;
    // Walking like a person (user, 2026-10-15: the guard glided sideways and backwards round corners
    // and swivelled on the spot like a statue on a turntable). The body turns at a limited rate and
    // only moves the way it faces; a sharp turn slows the walk, and past about 70° he turns where he
    // stands, stepping round; the walk eases in and out instead of starting and stopping dead.
    public float Speed{get;private set;}
    bool arriving,turningOnSpot;
    public static Vector3 Facing(Transform t){var f=-t.forward;f.y=0;return f.sqrMagnitude>1e-6f?f.normalized:Vector3.forward;}
    void Awake(){performance=GetComponentInChildren<PeriodCharacterPerformance>();}
    // arrive: this is the end of the walk, so he slows down onto the point instead of passing it.
    public void WalkTowards(Vector3 point,float speed,bool followFloor=false,bool arrive=false){arriving=arrive;StepTowards(point,speed,Time.deltaTime);arriving=false;if(followFloor)SnapToFloor();}
    // Feet on the floor under the actor (the court lies a little lower than the cloister), ignoring its own body box.
    void SnapToFloor() {
        var p=transform.position;float best=float.NegativeInfinity;
        foreach(var h in Physics.RaycastAll(p+Vector3.up*.6f,Vector3.down,1.4f,~(1<<2),QueryTriggerInteraction.Ignore))
            if(!h.transform.IsChildOf(transform)&&h.point.y>best&&h.point.y<p.y+.45f)best=h.point.y;
        if(!float.IsNegativeInfinity(best)){p.y=Mathf.MoveTowards(p.y,best,Time.deltaTime*1.5f);transform.position=p;}
    }
    void StepTowards(Vector3 point,float speed,float deltaTime) {
        if(DemoApp.Instance&&DemoApp.Instance.GameTimePaused)return;
        Vector3 to=point-transform.position;to.y=0;float distance=to.magnitude;
        if(distance<.0005f){var p=transform.position;p.y=point.y;transform.position=p;}
        if(distance<.0005f||speed<=0||deltaTime<=0){StopWalking();return;}
        walking=true;var direction=to/distance;
        // Brisk turning while slow, a gentler arc at full pace.
        float rate=Mathf.Lerp(250,150,Mathf.Clamp01(Speed/1.3f));
        if(distance>.015f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-direction),rate*deltaTime);
        float error=Vector3.Angle(Facing(transform),direction);
        float wanted=speed*Mathf.Clamp01((Mathf.Cos(error*Mathf.Deg2Rad)-.35f)/.55f);
        if(arriving)wanted=Mathf.Min(wanted,Mathf.Max(.22f,Mathf.Sqrt(2*1.5f*distance)));
        Speed=Mathf.MoveTowards(Speed,wanted,(wanted>Speed?2.4f:3.4f)*deltaTime);
        // Along the way he faces; only the last few centimetres go straight onto the point.
        var move=distance<.2f?direction:Facing(transform);
        var position=transform.position+move*Mathf.Min(Speed*deltaTime,distance);
        // Height follows the point as before (routes given at kerb or floor height); feet-on-floor walks snap afterwards.
        position.y=Mathf.MoveTowards(position.y,point.y,Mathf.Max(Speed,.5f)*deltaTime);
        transform.position=position;
        // On the spot the feet step round with the turn; walking, the stride matches the ground speed.
        turningOnSpot=Speed<.06f&&error>10;
        if(performance)performance.SetLocomotion(turningOnSpot?.4f:Speed);
        stride+=deltaTime*8;
    }
    // Turning where he stands, stepping round rather than swivelling stiffly; true once he faces that way.
    public bool TurnToward(Vector3 direction,float degreesPerSecond=160) {
        direction.y=0;if(direction.sqrMagnitude<1e-6f)return true;
        if(DemoApp.Instance&&DemoApp.Instance.GameTimePaused)return false;
        float error=Vector3.Angle(Facing(transform),direction);
        if(error<1.5f){if(walking)StopWalking();return true;}
        Speed=0;
        transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-direction),degreesPerSecond*Time.deltaTime);
        // A small adjustment is only the body; a real turn moves the feet too, until it is done.
        if(error>12||turningOnSpot){turningOnSpot=true;walking=true;if(performance)performance.SetLocomotion(.4f);}
        return false;
    }
    // A short step back or to one side keeping the way he faces (pulling a door toward himself).
    public bool Shuffle(Vector3 point,float metresPerSecond=.45f) {
        if(DemoApp.Instance&&DemoApp.Instance.GameTimePaused)return false;
        var to=point-transform.position;to.y=0;float distance=to.magnitude;
        if(distance<.01f){if(walking)StopWalking();return true;}
        walking=true;Speed=0;turningOnSpot=false;
        transform.position+=to/distance*Mathf.Min(distance,metresPerSecond*Time.deltaTime);
        if(performance)performance.SetLocomotion(.4f);
        return false;
    }
    public void StopWalking(){walking=false;turningOnSpot=false;Speed=0;if(performance)performance.SetWalking(false);ResetWalkPose();}
    void ResetWalkPose() {
        if(!poseApplied)return;
        if(leftLeg)leftLeg.localRotation=leftLegPose;if(rightLeg)rightLeg.localRotation=rightLegPose;
        if(leftArm)leftArm.localRotation=leftArmPose;if(rightArm)rightArm.localRotation=rightArmPose;poseApplied=false;
    }
    void LateUpdate() {
        // New characters use their authored full-body Walk clip exclusively.
        if(performance)return;
        if(!walking||!DemoApp.Instance||DemoApp.Instance.GameTimePaused)return;
        if(!leftLeg)foreach(var t in GetComponentsInChildren<Transform>()) {
            if(t.name=="Left_UpperLeg")leftLeg=t;if(t.name=="Right_UpperLeg")rightLeg=t;
            if(t.name=="Left_UpperArm")leftArm=t;if(t.name=="Right_UpperArm")rightArm=t;
        }
        if(!poseApplied){leftLegPose=leftLeg?leftLeg.localRotation:Quaternion.identity;rightLegPose=rightLeg?rightLeg.localRotation:Quaternion.identity;leftArmPose=leftArm?leftArm.localRotation:Quaternion.identity;rightArmPose=rightArm?rightArm.localRotation:Quaternion.identity;poseApplied=true;}
        if(leftLeg)leftLeg.localRotation=leftLegPose;if(rightLeg)rightLeg.localRotation=rightLegPose;
        if(leftArm)leftArm.localRotation=leftArmPose;if(rightArm)rightArm.localRotation=rightArmPose;
        float swing=Mathf.Sin(stride)*18;
        if(leftLeg)leftLeg.Rotate(Vector3.right,swing,Space.Self);if(rightLeg)rightLeg.Rotate(Vector3.right,-swing,Space.Self);
        if(leftArm)leftArm.Rotate(Vector3.right,-swing*.4f,Space.Self);if(rightArm)rightArm.Rotate(Vector3.right,swing*.4f,Space.Self);
    }
    void OnDisable(){StopWalking();}
    public override string ActionText => "交谈 · "+title;
    public override void Use() { DemoApp.Instance.OpenOpeningActor(this); }
}
