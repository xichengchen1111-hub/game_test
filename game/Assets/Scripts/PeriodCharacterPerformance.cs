using UnityEngine;

// Three authored, in-place clips. OpeningActor owns world movement; this
// component never edits bones or invents a walk/talk from the idle clip.
public sealed class PeriodCharacterPerformance : MonoBehaviour {
    public Animator animator;
    public string speaker;
    // Zero means the source was in-place and has no measured stride calibration.
    public float walkMetresPerSecond;
    static readonly int Speaking=Animator.StringToHash("Speaking"),Walking=Animator.StringToHash("Walking"),WalkRate=Animator.StringToHash("WalkRate");
    static PeriodCharacterPerformance currentSpeaker;
    float remaining;
    public bool IsSpeaking=>animator&&animator.GetBool(Speaking);
    public bool IsWalking=>animator&&animator.GetBool(Walking);
    public bool WalkCalibrated=>walkMetresPerSecond>.05f;
    void Awake() {
        if(!animator)animator=GetComponent<Animator>();
        if(animator)animator.applyRootMotion=false;
    }
    public void SetWalking(bool moving) {if(animator)animator.SetBool(Walking,moving);}
    public void SetLocomotion(float metresPerSecond) {
        SetWalking(metresPerSecond>.001f);
        if(animator)animator.SetFloat(WalkRate,WalkCalibrated?Mathf.Max(.05f,metresPerSecond/walkMetresPerSecond):1);
    }
    public void Speak(string text) {
        if(!animator)return;
        if(currentSpeaker&&currentSpeaker!=this)currentSpeaker.Listen();
        currentSpeaker=this;
        remaining=Mathf.Clamp((text??string.Empty).Length/5f+1f,2.5f,12f);
        bool restart=IsSpeaking;animator.SetBool(Speaking,true);
        // A moving actor keeps the full-body walk. Restarting Talk while walking
        // would momentarily put both feet in the standing pose.
        if(restart&&!IsWalking)animator.CrossFadeInFixedTime("Talk",.25f,0,0);
    }
    public void Listen() {
        remaining=0;if(animator)animator.SetBool(Speaking,false);
        if(currentSpeaker==this)currentSpeaker=null;
    }
    public static void Silence() {if(currentSpeaker)currentSpeaker.Listen();}
    void OnDisable(){Listen();SetWalking(false);}
    void Tick(float deltaTime,bool frozen) {
        if(!animator)return;
        animator.speed=frozen?0:1;
        if(!frozen&&remaining>0){remaining-=deltaTime;if(remaining<=0)Listen();}
    }
    void Update()=>Tick(Time.deltaTime,DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused);
}
