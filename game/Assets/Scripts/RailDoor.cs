using UnityEngine;
// Doors open automatically at each station; this handle lets passengers inspect boarding.
public sealed class RailDoor : Interactable {
    public RailTrain train;
    public override string ActionText=>title;
    public override void Use(){if(!train.Departing)train.SetDoors(true);}
}
