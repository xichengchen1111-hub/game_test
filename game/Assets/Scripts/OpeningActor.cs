using UnityEngine;
// Stable roles separate the opening from the exploration duty officer.
public sealed class OpeningActor : Interactable {
    public string role;
    bool walking,poseApplied;
    Quaternion leftLegPose,rightLegPose,leftArmPose,rightArmPose;
    float stride;
    Transform leftLeg,rightLeg,leftArm,rightArm;
    PeriodCharacterPerformance performance;
    void Awake(){performance=GetComponentInChildren<PeriodCharacterPerformance>();}
    public void WalkTowards(Vector3 point,float speed)=>StepTowards(point,speed,Time.deltaTime);
    void StepTowards(Vector3 point,float speed,float deltaTime) {
        if(DemoApp.Instance&&DemoApp.Instance.GameTimePaused)return;
        Vector3 direction=point-transform.position;direction.y=0;
        if(direction.sqrMagnitude<.000001f||speed<=0||deltaTime<=0){StopWalking();return;}
        walking=true;Vector3 previous=transform.position;
        if(direction.sqrMagnitude>.001f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(-direction),deltaTime*180);
        transform.position=Vector3.MoveTowards(transform.position,point,speed*deltaTime);
        if(performance)performance.SetLocomotion(Vector3.Distance(previous,transform.position)/deltaTime);
        stride+=deltaTime*8;
    }
    public void StopWalking(){walking=false;if(performance)performance.SetWalking(false);ResetWalkPose();}
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
    void Update(){
        if(role!="guard"||!DemoApp.Instance||DemoApp.Instance.state.openingStage<1||DemoApp.Instance.GameTimePaused)return;
        if(DemoApp.Instance.IsSpeakingWith(this)){StopWalking();return;}
        var local=new Vector3(16.95f,.15f,21.8f);
        var target=transform.parent?transform.parent.TransformPoint(local):local;
        if(Vector3.Distance(transform.position,target)<.001f){StopWalking();return;}
        WalkTowards(target,1);
    }
    void OnDisable(){StopWalking();}
    public override string ActionText => "交谈 · "+title;
    public override void Use() { DemoApp.Instance.OpenOpeningActor(this); }
}
