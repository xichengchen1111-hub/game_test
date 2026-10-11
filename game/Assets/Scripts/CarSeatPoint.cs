using UnityEngine;

public sealed class CarSeatPoint : MonoBehaviour {
    public string seatId, label;
    public bool driver;
    public string row;
    public int side;
    public Vector3 eyeLocal = new Vector3(0, .72f, .05f);
    public Vector3 exitNear, exitFar;      // car-local standing points
    public CarPart nearDoor, farDoor;
}
