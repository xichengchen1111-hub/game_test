using UnityEngine;

public enum CarControlKind {
    Body, Door, Seat, Window, Hood, TrunkLid, Strap, RadiatorCap, FuelCap, CrankHole, CrankStowed, Spare, Engine,
    ItemTools, ItemTube, ItemOil, ItemTarp, Gauges, Ignition, Starter, Choke, Lights, Horn, Spark, HandThrottle,
    Gear, Handbrake, Wiper, WindshieldCrank, Visor, Dome
}

// Every reachable car feature uses the ordinary 3m E ray and prompt.
public sealed class CarControl : Interactable {
    public HotelCar car;
    public string controlId;
    public CarControlKind kind;
    public CarPart part;
    public CarSeatPoint seat;
    public override string ActionText => car ? car.ActionText(this) : title;
    public override void Use() { if (car) car.Operate(this); }
}
