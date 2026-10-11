using UnityEngine;

// Every action begins/ends in the same supported seated pose. A shuffled bag
// gives each repair gesture a turn without repeating the last gesture.
public sealed class WatchmakerPerformance : MonoBehaviour {
    public Animator animator;
    public string[] states={"RepairScrew","InspectMovement","AdjustParts"};
    public float blendSeconds=.6f;
    public int CompletedActions {get; private set;}
    public int CurrentAction {get; private set;}
    public bool Ready => started;
    public string CurrentState => states[CurrentAction];
    int[] bag;
    int cursor;
    bool started;

    void Start() {
        if(!animator||states.Length<2)return;
        bag=new int[states.Length];
        for(int i=0;i<bag.Length;i++)bag[i]=i;
        CurrentAction=0;
        // The prefab already contains this seated sample: no T-pose frame.
        animator.Play(states[CurrentAction],0,0);
        Refill();started=true;
    }
    void Refill() {
        for(int i=bag.Length-1;i>0;i--) {
            int j=Random.Range(0,i+1);int value=bag[i];bag[i]=bag[j];bag[j]=value;
        }
        if(bag[0]==CurrentAction){int value=bag[0];bag[0]=bag[1];bag[1]=value;}
        cursor=0;
    }
    void Update() {
        if(!started)return;
        bool frozen=DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused;
        animator.speed=frozen?0:1;
        if(frozen||animator.IsInTransition(0))return;
        var state=animator.GetCurrentAnimatorStateInfo(0);
        if(!state.IsName(states[CurrentAction])||state.normalizedTime<1)return;
        CompletedActions++;
        if(cursor>=bag.Length)Refill();
        CurrentAction=bag[cursor++];
        animator.CrossFadeInFixedTime(states[CurrentAction],blendSeconds,0,0);
    }
}
