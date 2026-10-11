using UnityEngine;

// Only this actor's dialogue changes its animation; other NPCs remain independent.
public sealed class MilkboyPerformance : MonoBehaviour {
    public Animator animator;
    public Transform head,headFront;
    public Vector3 headUpLocal=Vector3.up;
    Quaternion gaze;
    bool gazeReady;
    float remaining;
    static readonly int Speaking=Animator.StringToHash("Speaking");
    public bool IsSpeaking => animator&&animator.GetBool(Speaking);
    public void Speak(string text) {
        remaining=Mathf.Clamp(text.Length/5f+1f,2.5f,8f);
        bool restart=IsSpeaking;animator.SetBool(Speaking,true);
        if(restart)animator.CrossFadeInFixedTime("Talk",.25f,0,0);
    }
    public void Listen() {remaining=0;if(animator)animator.SetBool(Speaking,false);}
    void Update() {
        if(!animator)return;
        bool frozen=DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused;animator.speed=frozen?0:1;
        if(!frozen&&remaining>0){remaining-=Time.deltaTime;if(remaining<=0)Listen();}
    }
    void LateUpdate() {
        if(!head||!headFront||!DemoApp.Instance||DemoApp.Instance.NPCAnimationPaused)return;
        var player=DemoApp.Instance.player;
        Vector3 forward=-transform.parent.forward;
        Vector3 desired=(forward+Vector3.up*.25f).normalized;
        var offset=player.eye.transform.position-head.position;
        var flat=Vector3.ProjectOnPlane(offset,Vector3.up);
        if(offset.sqrMagnitude<36&&Vector3.Dot(flat.normalized,forward)>.05f) {
            float yaw=Mathf.Clamp(Vector3.SignedAngle(forward,flat,Vector3.up),-55,55);
            float pitch=Mathf.Clamp(Mathf.Atan2(offset.y,flat.magnitude)*Mathf.Rad2Deg,5,38);
            // Keep eye contact dominant; only a small, brief sideways hesitation remains.
            float glance=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Time.time*.85f)),8)*2.5f;
            desired=Quaternion.AngleAxis(yaw+glance,Vector3.up)*forward*Mathf.Cos(pitch*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(pitch*Mathf.Deg2Rad);
        }
        var frame=Quaternion.LookRotation(headFront.position-head.position,head.TransformDirection(headUpLocal));
        if(!gazeReady){gaze=frame;gazeReady=true;}
        gaze=Quaternion.Slerp(gaze,Quaternion.LookRotation(desired,Vector3.up),1-Mathf.Exp(-5*Time.deltaTime));
        head.rotation=gaze*Quaternion.Inverse(frame)*head.rotation;
    }
}
