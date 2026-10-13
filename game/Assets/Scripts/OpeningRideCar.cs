using UnityEngine;

// A visual, scripted copy of the existing sedan. It never registers as HotelCar
// and never writes the player's drivable car, its fixture IDs or its saved seat.
public sealed class OpeningRideCar : MonoBehaviour {
    public static int ReservedLights { get; private set; }
    public Transform passengerSeat, driver;
    public Vector3 passengerEyeLocal = new Vector3(0, .72f, 0);
    public CarPart passengerDoor;
    public CarPart[] parts = new CarPart[0];
    public CarSounds sounds;
    public BoxCollider solid;
    public WeatherShelter shelter;
    public Renderer[] lampRenderers = new Renderer[0];
    public int[] lampSlots = new int[0], lampKinds = new int[0];
    public Light[] headlamps = new Light[0];
    public Light domeLight;
    public float Speed, Steering;
    public bool EngineRunning, Occupied, Cranking;
    public Transform PassengerSeat => passengerSeat;
    public Transform Driver => driver;
    public Vector3 PassengerEyeLocal => passengerEyeLocal;
    public Vector3 PassengerStandingPoint => transform.TransformPoint(new Vector3(1.45f, .02f, -.36f));
    public bool PassengerDoorOpen => passengerDoor && passengerDoor.Fraction > .001f;
    float wheelAngle, wipeTime;
    Vector3 shelterPosition;
    Quaternion shelterRotation;
    MaterialPropertyBlock lampBlock;
    void Awake() {
        lampBlock = new MaterialPropertyBlock();
        sounds?.Initialize(new Vector3(0, .8f, 1.3f));
        foreach (var part in parts) if (part) part.Snap(0);
        SetPassengerDoor(false, true);
        RefreshShelter();
    }
    public void SetDriverVisible(bool visible) { if (driver) driver.gameObject.SetActive(visible); }
    public void SetSolid(bool enabled) { if (solid) solid.enabled = enabled; }
    public void SetPassengerDoor(bool open, bool instant = false) {
        if (!passengerDoor) return;
        bool changed = passengerDoor.Open != open;
        if (instant) passengerDoor.Snap(open ? 1 : 0);
        else { passengerDoor.Target = open ? 1 : 0; if (changed) sounds?.Door(open); }
    }
    void Update() {
        var app = DemoApp.Instance;
        bool frozen = app && app.GameTimePaused;
        float dt = frozen ? 0 : Time.deltaTime;
        foreach (var part in parts) if (part) part.Tick(dt);
        wheelAngle += Speed / .37f * Mathf.Rad2Deg * dt;
        if (EngineRunning) wipeTime += dt;
        bool rain = DayNightCycle.Instance && DayNightCycle.Instance.Weather.rain > .05f;
        foreach (var part in parts) {
            if (!part) continue;
            if (part.partId.StartsWith("Wheel_"))
                part.PoseEuler(new Vector3(wheelAngle, part.partId.StartsWith("Wheel_F") ? Steering * 25 : 0, 0));
            else if (part.partId == "SteeringWheel") part.Pose(Steering * 180, Vector3.zero);
            else if (part.partId == "WiperArm") part.Pose(EngineRunning && rain ? -40 + 40 * (1 - Mathf.Cos(wipeTime / 1.4f * Mathf.PI * 2)) : -40, Vector3.zero);
            else if (part.partId == "Needle_Speed") part.Pose(Mathf.Clamp(Mathf.Abs(Speed) * 3.6f, 0, 100) / 100 * 270 - 135, Vector3.zero);
        }
        sounds?.Tick(EngineRunning ? 600 + Mathf.Abs(Speed) * 115 : 0, Mathf.Clamp01(Mathf.Abs(Speed) / 8), EngineRunning,
            Cranking, false, Mathf.Abs(Speed), EngineRunning && rain, Occupied && !PassengerDoorOpen, frozen);
        if (shelter) shelter.acousticallyOpen = PassengerDoorOpen;
        UpdateLamps();
    }
    void UpdateLamps() {
        if (lampBlock == null) lampBlock = new MaterialPropertyBlock();
        for (int i = 0; i < lampRenderers.Length; i++) {
            var r = lampRenderers[i]; if (!r) continue;
            var colour = lampKinds[i] switch {
                0 => EngineRunning ? new Color(1, .86f, .62f) * 3.2f : Color.black,
                1 => EngineRunning ? new Color(1, .12f, .08f) * 2.2f : Color.black,
                3 => EngineRunning ? new Color(1, .82f, .55f) * .55f : Color.black,
                _ => PassengerDoorOpen ? new Color(1, .86f, .62f) * 2 : Color.black
            };
            r.GetPropertyBlock(lampBlock, lampSlots[i]); lampBlock.SetColor("_EmissionColor", colour); r.SetPropertyBlock(lampBlock, lampSlots[i]);
        }
        // Borrow pools independently, so HotelCar's own reservation remains intact.
        foreach (var light in headlamps) if (light) light.enabled = EngineRunning;
        if (domeLight) domeLight.enabled = PassengerDoorOpen;
        ReservedLights = (EngineRunning ? headlamps.Length : 0) + (PassengerDoorOpen && domeLight ? 1 : 0);
    }
    void LateUpdate() {
        if ((shelterPosition - transform.position).sqrMagnitude > .0025f || Quaternion.Angle(shelterRotation, transform.rotation) > 1)
            RefreshShelter();
    }
    void RefreshShelter() {
        if (!shelter) return;
        shelter.enabled = false; shelter.enabled = true;
        shelterPosition = transform.position; shelterRotation = transform.rotation;
    }
    void OnDisable() { ReservedLights = 0; }
}
