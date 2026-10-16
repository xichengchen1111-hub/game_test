using UnityEngine;

// A figure posed without its Animator (the lawyer at the wheel) turning to the person he is talking
// to (user, 2026-10-15: at the hospital he said goodbye staring ahead through the windscreen). The
// posed rotations stay as authored and the turn is added on top every frame, shared between the
// chest, the neck and the head, with the eyes taking up what the head cannot; it eases in and out.
public sealed class SeatedLook : MonoBehaviour {
    public Transform target;
    public bool look;
    public float maxYaw=78,maxPitch=24,maxEyes=22;
    Transform chest,neck,head,leftEye,rightEye;
    Quaternion chestRest,neckRest,headRest,leftEyeRest,rightEyeRest;
    float weight,yaw,pitch;bool ready;
    public float Weight=>weight;public float Yaw=>yaw;
    // Degrees between where the face points now (head turn included) and a point.
    public float Facing(Vector3 point){Resolve();if(!head)return 180;var up=transform.up;var f=Quaternion.AngleAxis(Mathf.Clamp(yaw,-maxYaw,maxYaw)*Mathf.SmoothStep(0,1,weight),up)*-transform.forward;return Vector3.Angle(Vector3.ProjectOnPlane(f,up),Vector3.ProjectOnPlane(point-head.position,up))-Mathf.Min(maxEyes,Mathf.Abs(yaw)-Mathf.Min(Mathf.Abs(yaw),maxYaw));}
    void Resolve() {
        if(ready)return;
        foreach(var t in GetComponentsInChildren<Transform>(true))
            switch(t.name){case "Chest":chest=t;break;case "Neck":neck=t;break;case "Head":head=t;break;case "Left_Eye":leftEye=t;break;case "Right_Eye":rightEye=t;break;}
        if(!head)return;
        if(chest)chestRest=chest.localRotation;if(neck)neckRest=neck.localRotation;headRest=head.localRotation;
        if(leftEye)leftEyeRest=leftEye.localRotation;if(rightEye)rightEyeRest=rightEye.localRotation;
        ready=true;
    }
    void Start()=>Resolve();
    void LateUpdate() {
        Resolve();if(!ready)return;
        if(chest)chest.localRotation=chestRest;if(neck)neck.localRotation=neckRest;head.localRotation=headRest;
        if(leftEye)leftEye.localRotation=leftEyeRest;if(rightEye)rightEye.localRotation=rightEyeRest;
        var app=DemoApp.Instance;float dt=app&&app.NPCAnimationPaused?0:Time.deltaTime;
        weight=Mathf.MoveTowards(weight,look&&target?1:0,dt/.7f);
        if(weight<=0)return;
        // The authored front of the cast is the root's -Z.
        var up=transform.up;var facing=-transform.forward;var right=Vector3.Cross(up,facing);
        if(target) {
            var to=target.position-head.position;var flat=Vector3.ProjectOnPlane(to,up);
            float wantYaw=Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(facing,up),flat,up),-maxYaw-maxEyes,maxYaw+maxEyes);
            float wantPitch=Mathf.Clamp(Mathf.Atan2(Vector3.Dot(to,up),flat.magnitude)*Mathf.Rad2Deg,-maxPitch,maxPitch);
            float k=1-Mathf.Exp(-dt*4.5f);yaw=Mathf.Lerp(yaw,wantYaw,k);pitch=Mathf.Lerp(pitch,wantPitch,k);
        }
        float w=Mathf.SmoothStep(0,1,weight);
        float body=Mathf.Clamp(yaw,-maxYaw,maxYaw)*w,eyes=Mathf.Clamp(yaw*w-body,-maxEyes,maxEyes),p=pitch*w;
        // Positive pitch raises the face: about -right, which turns the facing toward up.
        void Turn(Transform bone,float y,float x){if(bone)bone.rotation=Quaternion.AngleAxis(y,up)*Quaternion.AngleAxis(-x,right)*bone.rotation;}
        Turn(chest,body*.22f,0);Turn(neck,body*.33f,p*.4f);Turn(head,body*.45f,p*.6f);
        Turn(leftEye,eyes,0);Turn(rightEye,eyes,0);
    }
}
