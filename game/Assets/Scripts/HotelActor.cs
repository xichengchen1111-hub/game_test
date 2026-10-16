using UnityEngine;

// The inn's people: the porter at the counter and the landlady at the dining cash desk. Their
// figures are placeholders (user, 2026-10-16: a teammate makes the people); the conversations,
// the porter's testimony and the save state are complete. Roles are stable: porter, keeper.
public sealed class HotelActor : Interactable {
    public string role;
    public override string ActionText => "交谈 · " + title;
    public override void Use() { DemoApp.Instance.OpenHotelActor(this); }
}
