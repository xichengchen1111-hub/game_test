using UnityEngine;

// Shared physical mechanism for the entrance, service door, cabinet leaves and drawer.
// The leaf stays in the Town scene. No portal, fade, teleport or scene swap is involved.
public class ShopMechanism : Interactable {
    public string fixtureId;
    public Transform movingPart;
    public Vector3 openEuler,openOffset;
    public bool protectPlayer;
    public bool startsOpen;
    // Optional (police station): explicit noun for the prompt and open/close sounds.
    public string label;
    public AudioSource sound;
    public AudioClip openClip, closeClip;
    public float OpenFraction => Mathf.SmoothStep(0,1,progress);
    // Swing rate (full turns per second); the heavy cell door is slower than a panelled one.
    public float swingSpeed=1.6f;
    public Vector3 leafCentre,leafSize;
    public bool IsOpen { get; private set; }
    public bool Moving => Mathf.Abs(progress-(IsOpen?1:0))>.001f;
    public bool LastUseBlocked { get; private set; }
    Quaternion closedRotation;
    Vector3 closedPosition;
    float progress;
    float blockedUntil;
    void Start() {
        closedRotation=movingPart.localRotation;closedPosition=movingPart.localPosition;
        var state=DemoApp.Instance.state;
        // Initialize once, so closing a door authored open survives old-save migration and relaunch.
        if(!state.initializedFixtures.Contains(fixtureId)) {
            state.initializedFixtures.Add(fixtureId);
            if(startsOpen&&!state.openFixtures.Contains(fixtureId))state.openFixtures.Add(fixtureId);
        }
        IsOpen=state.openFixtures.Contains(fixtureId);
        progress=IsOpen?1:0;Apply(progress);SetTitle();
    }
    void SetTitle() { title=(IsOpen?"关闭":"打开")+(!string.IsNullOrEmpty(label)?label:fixtureId.Contains("drawer")?"零件抽屉":fixtureId.Contains("vitrine")?"展柜":"木门"); }
    public override void Use() {
        // Before the release the cell door answers with the guard; the cinematic itself unlocks it.
        if(fixtureId=="police-holding-door"&&DemoApp.Instance.state.openingStage==0&&DemoApp.Instance.state.openingEscort==0&&!DemoApp.Instance.CellCinematicActive){DemoApp.Instance.CallOpeningGuard();return;}
        if(Moving)return;
        LastUseBlocked=false;
        float target=IsOpen?0:1;
        if(protectPlayer) {
            Physics.SyncTransforms();
            // Reject a swing whose swept leaf intersects the actual player capsule,
            // including somebody standing in the doorway when closing from either side.
            for(int i=0;i<=24;i++)if(Occupied(Mathf.Lerp(progress,target,i/24f))){LastUseBlocked=true;title="先让开门扇的转动范围";blockedUntil=Time.time+2;return;}
        }
        IsOpen=!IsOpen;SetTitle();
        if(sound&&(IsOpen?openClip:closeClip)){sound.clip=IsOpen?openClip:closeClip;sound.Play();}
        var opened=DemoApp.Instance.state.openFixtures;
        if(IsOpen){if(!opened.Contains(fixtureId))opened.Add(fixtureId);}else opened.Remove(fixtureId);
        DemoApp.Instance.Save();
    }
    bool Occupied(float t) {
        Quaternion rotation=Quaternion.AngleAxis(openEuler.y*t,movingPart.parent.InverseTransformDirection(Vector3.up))*closedRotation;
        var parent=movingPart.parent;
        var centre=parent.TransformPoint(closedPosition+openOffset*t+rotation*leafCentre);
        var orientation=parent.rotation*rotation;
        var scale=parent.lossyScale;scale=new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z));
        var hits=Physics.OverlapBox(centre,Vector3.Scale(leafSize*.5f,scale),orientation,~0,QueryTriggerInteraction.Ignore);
        foreach(var collider in hits)
            if(collider.GetComponentInParent<PlayerMotor>())return true;
        return false;
    }
    // For people who open it by hand (user, 2026-10-15), in world space. The leaf runs from the hinge
    // (the moving part's origin) to its free edge, where the handle or the lock is.
    Vector3 LeafRun{get{var c=leafCentre;c.y=0;return c*2;}}
    public Vector3 EdgePoint(float worldHeight,float inset=.08f){var run=LeafRun;var p=movingPart.TransformPoint(run.normalized*Mathf.Max(0,run.magnitude-inset));p.y=worldHeight;return p;}
    public Vector3 DoorwayCentre=>movingPart.parent.TransformPoint(closedPosition+closedRotation*(LeafRun*.5f));
    public Vector3 ClosedAlong=>movingPart.parent.TransformDirection(closedRotation*LeafRun.normalized);
    public float LeafWidth=>LeafRun.magnitude;
    void Apply(float t) {
        movingPart.localPosition=closedPosition+openOffset*t;
        movingPart.localRotation=Quaternion.AngleAxis(openEuler.y*t,movingPart.parent.InverseTransformDirection(Vector3.up))*closedRotation;
        Physics.SyncTransforms();
    }
    void Update() {
        if(blockedUntil>0&&Time.time>=blockedUntil){blockedUntil=0;SetTitle();}
        float target=IsOpen?1:0;
        if(Mathf.Abs(target-progress)<.001f)return;
        float next=Mathf.MoveTowards(progress,target,Time.deltaTime*swingSpeed);
        // A player entering the sweep while the leaf is moving pauses it safely.
        if(protectPlayer&&Occupied(Mathf.SmoothStep(0,1,next)))return;
        progress=next;Apply(Mathf.SmoothStep(0,1,progress));
    }
}
