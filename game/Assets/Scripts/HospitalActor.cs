using UnityEngine;
// People of the hospital's story line. Roles are stable: porter, child, sister.
public sealed class HospitalActor : Interactable {
    public string role;
    public override string ActionText => "交谈 · "+title;
    public override void Use() { DemoApp.Instance.OpenHospitalActor(this); }
}
