using UnityEngine;

// This actor owns only the station clerk's two in-place animations.
public sealed class StationClerkPerformance : MonoBehaviour {
    public Animator animator;
    // Dialogue speaker whose lines drive the talk clip (the police duty officer reuses this actor rig).
    public string speaker = "售票员";
    float remaining;
    static readonly int Speaking=Animator.StringToHash("Speaking");
    public bool IsSpeaking => animator&&animator.GetBool(Speaking);
    public void Speak(string text) {
        if(!animator)return;
        remaining=Mathf.Clamp((text??string.Empty).Length/5f+1f,2.5f,12f);
        bool restart=IsSpeaking;animator.SetBool(Speaking,true);
        if(restart)animator.CrossFadeInFixedTime("Talk",.25f,0,0);
    }
    public void Listen() { remaining=0;if(animator)animator.SetBool(Speaking,false); }
    void OnDisable() { Listen(); }
    void Update() {
        if(!animator)return;
        bool frozen=DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused;
        animator.speed=frozen?0:1;
        if(!frozen&&remaining>0) { remaining-=Time.deltaTime;if(remaining<=0)Listen(); }
    }
}
