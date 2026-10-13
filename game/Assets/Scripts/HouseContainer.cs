using UnityEngine;
// Opening storage leaves the room visible. Paper objects have their own interaction rays.
public sealed class HouseContainer : Interactable {
    public string fixtureId;
    public Transform[] drawers;
    public Vector3[] closedPositions;
    public Vector3 offset;
    public Interactable[] contents;
    public bool safe,hinged;
    public bool IsOpen {get;private set;}
    public bool Unlocked => !safe || DemoApp.Instance.state.openFixtures.Contains("house-safe-unlocked");
    float progress,blockedUntil;Quaternion[] closedRotations;BoxCollider[][] movingColliders;
    public override string ActionText=>Time.time<blockedUntil?"先让开柜门和抽屉":safe&&!Unlocked?"拨动保险柜密码":IsOpen?"合上 · "+title:"打开 · "+title;
    void Start(){closedRotations=System.Array.ConvertAll(drawers,d=>d.localRotation);movingColliders=System.Array.ConvertAll(drawers,d=>System.Array.FindAll(d.GetComponentsInChildren<BoxCollider>(),c=>!c.isTrigger));IsOpen=DemoApp.Instance.state.openFixtures.Contains(fixtureId)&&Unlocked;progress=IsOpen?1:0;Apply();}
    Quaternion Rotation(int i,float t)=>Quaternion.AngleAxis((hinged&&i==1?-85:85)*t,Vector3.up)*closedRotations[i];
    bool Occupied(float t){
        if(movingColliders==null)return false;Physics.SyncTransforms();
        for(int i=0;i<drawers.Length;i++)foreach(var b in movingColliders[i]){
            var d=drawers[i];var local=d.InverseTransformPoint(b.transform.TransformPoint(b.center));var rot=safe||hinged?Rotation(i,t):closedRotations[i];var position=closedPositions[i]+(safe||hinged?Vector3.zero:offset*t);
            var centre=d.parent.TransformPoint(position+rot*local);var orient=d.parent.rotation*rot;
            foreach(var hit in Physics.OverlapBox(centre,b.size*.5f,orient,~0,QueryTriggerInteraction.Ignore))if(hit.GetComponentInParent<PlayerMotor>())return true;
        }return false;
    }
    void Update(){if(DemoApp.Instance&&DemoApp.Instance.NPCAnimationPaused)return;float next=Mathf.MoveTowards(progress,IsOpen?1:0,Time.deltaTime*2);if(Mathf.Abs(next-progress)<.0001f)return;if(Occupied(Mathf.SmoothStep(0,1,next)))return;progress=next;Apply();}
    void Apply(){foreach(var paper in contents){var col=paper.GetComponent<Collider>();if(col)col.enabled=progress>.8f;}for(int i=0;i<drawers.Length;i++)if(drawers[i]){if(safe||hinged)drawers[i].localRotation=Rotation(i,Mathf.SmoothStep(0,1,progress));else drawers[i].localPosition=closedPositions[i]+offset*Mathf.SmoothStep(0,1,progress);}Physics.SyncTransforms();}
    public override void Use(){if(safe&&!Unlocked){DemoApp.Instance.Inspect(this);return;}SetOpen(!IsOpen);}
    public bool TryCode(string code){if(!safe||code!="0614")return false;var s=DemoApp.Instance.state;if(!s.openFixtures.Contains("house-safe-unlocked"))s.openFixtures.Add("house-safe-unlocked");SetOpen(true);return true;}
    public void SetOpen(bool value){if(value&&!Unlocked)return;for(int i=0;i<=16;i++)if(Occupied(Mathf.Lerp(progress,value?1:0,i/16f))){blockedUntil=Time.time+2;return;}IsOpen=value;var list=DemoApp.Instance.state.openFixtures;if(value){if(!list.Contains(fixtureId))list.Add(fixtureId);}else list.Remove(fixtureId);DemoApp.Instance.Save();}
}
