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
    float remaining;bool speaking;
    // False for people posed in a bed or on a bench (HospitalBedPose): the pose bends the arms
    // on top of the clip, so the talking gestures would carry the hands into the face. They
    // speak with the mouth and the head only.
    public bool gestures=true;
    // Playback speed of the clips: below 1 for someone weak and tired (the child in bed).
    public float pace=1;
    // Seconds of hand gestures at the start of a line, after which the hands come to rest while the
    // mouth carries on (user, 2026-10-15: the lawyer's hands never stopped moving). Zero: the whole line.
    public float gestureSeconds;
    float gestureLeft;
    // A figure posed without its Animator (the lawyer at the wheel) still speaks with the mouth.
    bool Live=>animator&&animator.isActiveAndEnabled;
    public bool IsSpeaking=>speaking;
    // The line being spoken, for the mouth (PeriodCharacterMouthMotion).
    public string SpokenLine{get;private set;}
    public int SpokenCount{get;private set;}
    public bool IsWalking=>animator&&animator.GetBool(Walking);
    public bool WalkCalibrated=>walkMetresPerSecond>.05f;
    void Awake() {
        if(!animator)animator=GetComponent<Animator>();
        if(animator)animator.applyRootMotion=false;
    }
    public void SetWalking(bool moving) {if(Live)animator.SetBool(Walking,moving);}
    public void SetLocomotion(float metresPerSecond) {
        SetWalking(metresPerSecond>.001f);
        if(Live)animator.SetFloat(WalkRate,WalkCalibrated?Mathf.Max(.05f,metresPerSecond/walkMetresPerSecond):1);
    }
    public void Speak(string text) {
        if(!animator)return;
        if(currentSpeaker&&currentSpeaker!=this)currentSpeaker.Listen();
        currentSpeaker=this;SpokenLine=text;SpokenCount++;speaking=true;
        remaining=Mathf.Clamp((text??string.Empty).Length/5f+1f,2.5f,12f);
        if(!gestures||!Live)return;
        gestureLeft=gestureSeconds>0?gestureSeconds:remaining;
        bool restart=animator.GetBool(Speaking);animator.SetBool(Speaking,true);
        // A moving actor keeps the full-body walk. Restarting Talk while walking
        // would momentarily put both feet in the standing pose.
        if(restart&&!IsWalking)animator.CrossFadeInFixedTime("Talk",.25f,0,0);
    }
    public void Listen() {
        remaining=0;gestureLeft=0;speaking=false;if(Live)animator.SetBool(Speaking,false);
        if(currentSpeaker==this)currentSpeaker=null;
    }
    public static void Silence() {if(currentSpeaker)currentSpeaker.Listen();}
    void OnDisable(){Listen();SetWalking(false);}
    void Tick(float deltaTime,bool frozen) {
        if(!animator)return;
        animator.speed=frozen?0:pace;
        if(!frozen&&gestureLeft>0){gestureLeft-=deltaTime;if(gestureLeft<=0&&Live)animator.SetBool(Speaking,false);}
        if(!frozen&&remaining>0){remaining-=deltaTime;if(remaining<=0)Listen();}
    }
    void Update()=>Tick(Time.deltaTime,DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused);
}
