using UnityEngine;
public class ScenePortal : Interactable {
    public string destination;
    public Vector3 entryPosition;
    public float entryYaw;
    public override void Use() { DemoApp.Instance.Travel(this); }
}
