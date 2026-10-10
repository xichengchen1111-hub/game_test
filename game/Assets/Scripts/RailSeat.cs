using UnityEngine;
public sealed class RailSeat : Interactable {
    public RailTrain train;
    public override string ActionText=>title;
    public override void Use(){DemoApp.Instance.SitAndDepart(train);}
}
