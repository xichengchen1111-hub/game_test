using System.Collections.Generic;
using UnityEngine;

// The hotel's 1928-31 style four-door sedan: real WheelCollider physics,
// automatic three-speed gearbox, realistic ignition/starter/choke/crank start
// and ordinary E interactions for every door, cover and control.
[DefaultExecutionOrder(40)]
public sealed class HotelCar : MonoBehaviour {
    public static HotelCar Current { get; private set; }
    public Rigidbody body;
    public WheelCollider[] wheels = new WheelCollider[4];      // FL, FR, RL, RR
    public Transform[] wheelVisuals = new Transform[4];
    public CarPart[] parts = new CarPart[0];
    public CarSeatPoint[] seats = new CarSeatPoint[0];
    public Light[] headlamps = new Light[0];
    public Light domeLight;
    public Renderer[] lampRenderers = new Renderer[0];
    public int[] lampSlots = new int[0], lampKinds = new int[0];  // 0 head lens, 1 tail, 2 dome, 3 gauge faces
    public Renderer windshieldRain;
    public WeatherShelter shelter;
    public CarSounds sounds;
    public Vector3 parkingPosition;
    public float parkingYaw;
    public Vector3 centreOfMass = new Vector3(0, .55f, .10f);
    public float mass = 1100;
    public Vector3 enginePoint, windshieldPoint, wiperPivot;
    public Vector2 rainQuadSize = new Vector2(1.12f, .38f);
    public Vector3 rainQuadCentre;

    static readonly float[] Ratios = { 3.11f, 1.77f, 1.00f };
    const float ReverseRatio = 3.75f, FinalDrive = 3.77f, Efficiency = .85f, TorqueScale = .82f;
    public const float IdleRpm = 550, MaxRpm = 2900, UpshiftRpm = 1950, DownshiftRpm = 850;
    const float FootBrake = 650, HandBrake = 1200, MaxSteer = 36, HighSpeedSteer = 13, AntiRoll = 5500;
    public const float StopSpeed = .5f;

    readonly Dictionary<string, CarPart> byId = new Dictionary<string, CarPart>();
    public int Gear { get; private set; }               // -1 reverse, 0 neutral, 1..3
    public bool EngineRunning { get; private set; }
    public bool Cranking { get; private set; }
    public bool HandCranking => crankPulse > 0;
    public float EngineRpm { get; private set; }
    public float SignedSpeed { get; private set; }
    public float Steer { get; private set; }
    public int LightsMode { get; private set; }
    public bool DomeOn { get; private set; }
    public bool CrankMounted { get; private set; }
    public int Shifts { get; private set; }
    public bool HornSounding { get; private set; }
    public float RainOnGlass { get; private set; }
    public float WipeClear { get; private set; }
    public bool FrozenByUi => frozenByUi;
    public string Feedback => Time.unscaledTime < feedbackUntil ? feedback : null;
    public bool Flipped => Vector3.Dot(transform.up, Vector3.up) < .35f;
    public bool Overturned => Flipped && flipTimer > 1.2f;
    public bool Parked => body && body.isKinematic && !frozenByUi;

    // Scripted review input uses the same paths as the keyboard.
    [System.NonSerialized] public float scriptedThrottle, scriptedBrake, scriptedSteer;
    [System.NonSerialized] public bool scriptedStarter, scriptedHorn;

    float throttleIn, brakeIn, steerIn;
    float shiftTimer, stoppedTimer, crankTime, starterPulse, crankPulse, hornPulse, settled, flipTimer, chokeRunning;
    bool frozenByUi, chokeReminded;
    string crankReport;
    Vector3 frozenVelocity, frozenAngular;
    string feedback; float feedbackUntil;
    float wiperPhase, wiperAngle = -40, gripTimer;
    Vector3 shelterPosition; Quaternion shelterRotation;
    Material rainMaterial;
    MaterialPropertyBlock lampBlock;
    Texture2D dropTexture;

    CarPart Part(string id) => byId.TryGetValue(id, out var p) ? p : null;
    bool PartOpen(string id) { var p = Part(id); return p && p.Open; }
    public bool Ignition => PartOpen("IgnitionKey");
    public bool Choke => PartOpen("ChokeKnob");
    public bool SparkAdvanced => PartOpen("SparkLever");
    public bool HandThrottleUp => PartOpen("HandThrottle");
    public bool Wipers => PartOpen("WiperSwitch");
    public bool HandbrakeOn => PartOpen("Handbrake");
    public bool TrunkOpen => PartOpen("TrunkLid");
    public bool StrapsLoose => PartOpen("Strap_L") && PartOpen("Strap_R");
    public bool Cold => DemoApp.Instance && DemoApp.Instance.ElapsedGameMinutes >= DemoApp.Instance.state.carWarmUntil;
    public CarPart DoorPart(string row, int side) => Part("Door_" + row + (side < 0 ? "L" : "R"));
    public bool CabinOpen {
        get {
            foreach (var p in parts) {
                if (!p) continue;
                if (p.partId.StartsWith("Door_") && p.Fraction > .001f) return true;
                if (p.partId.StartsWith("Glass_") && p.Fraction > .02f) return true;
                if (p.partId == "Windshield" && p.Fraction > .001f) return true;
            }
            return false;
        }
    }

    void Awake() {
        Current = this;
        foreach (var p in parts) if (p) byId[p.partId] = p;
        body.mass = mass;
        body.centerOfMass = centreOfMass;
        body.inertiaTensor = new Vector3(1750, 1900, 420) * (mass / 1100f);
        body.inertiaTensorRotation = Quaternion.identity;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearDamping = .02f; body.angularDamping = .25f;
        if (wheels[0]) wheels[0].ConfigureVehicleSubsteps(5, 12, 15);
        ApplyGrip(1);
        lampBlock = new MaterialPropertyBlock();
        if (sounds) sounds.Initialize(enginePoint);
        if (shelter) shelter.vehicle = this;
        if (windshieldRain) {
            rainMaterial = windshieldRain.material;
            dropTexture = MakeDrops();
            rainMaterial.SetTexture("_Drops", dropTexture);
            rainMaterial.SetVector("_Size", new Vector4(rainQuadSize.x, rainQuadSize.y, 0, 0));
            var pivot = wiperPivot - rainQuadCentre;
            rainMaterial.SetVector("_Pivot", new Vector4(pivot.x / rainQuadSize.x + .5f, pivot.y / rainQuadSize.y + .5f, 0, 0));
            rainMaterial.SetVector("_Radii", new Vector4(.05f, .33f, Mathf.Deg2Rad * 42, 0));
        }
        RestoreState();
    }
    void OnDestroy() {
        if (Current == this) Current = null;
        TownLightBudget.Reserved = 0;
        if (rainMaterial) Destroy(rainMaterial);
        if (dropTexture) Destroy(dropTexture);
    }

    // ------------------------------------------------------------------ state
    void RestoreState() {
        var app = DemoApp.Instance;
        var state = app ? app.state : null;
        foreach (var p in parts) {
            if (!p) continue;
            bool open = state != null && !string.IsNullOrEmpty(p.fixtureId) && state.openFixtures.Contains(p.fixtureId);
            p.Snap(open ? 1 : 0);
        }
        Part("Handbrake")?.Snap(1);                          // reload always parks with the brake applied
        LightsMode = state != null && state.openFixtures.Contains("car-lights-2") ? 2 : state != null && state.openFixtures.Contains("car-lights-1") ? 1 : 0;
        Part("LightSwitch")?.Snap(LightsMode / 2f);
        DomeOn = state != null && state.openFixtures.Contains("car-dome");
        CrankMounted = state != null && state.openFixtures.Contains("car-crank-mounted");
        Part("CrankMounted")?.SetVisible(CrankMounted);
        Part("CrankStowed")?.SetVisible(!CrankMounted);
        Vector3 position = parkingPosition; float yaw = parkingYaw;
        if (state != null && state.carSaved && ValidPose(new Vector3(state.carX, state.carY, state.carZ))) {
            position = new Vector3(state.carX, state.carY + .04f, state.carZ); yaw = state.carYaw;
        }
        Place(position, yaw);
        ApplyLamps();
    }
    bool ValidPose(Vector3 p) {
        if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || p.y < -2 || p.y > 6) return false;
        var plan = TownPlan.Current;
        if (plan == null || plan.bounds == null || plan.bounds.Length < 4) return true;
        return p.x > plan.bounds[0] + 2 && p.x < plan.bounds[1] - 2 && p.z > plan.bounds[2] + 2 && p.z < plan.bounds[3] - 2;
    }
    public void StoreState(SliceSave state) {
        state.carSaved = true;
        var p = transform.position;
        state.carX = p.x; state.carY = p.y; state.carZ = p.z; state.carYaw = transform.eulerAngles.y;
    }
    void SetFixture(string id, bool on) {
        var app = DemoApp.Instance; if (!app || string.IsNullOrEmpty(id)) return;
        var list = app.state.openFixtures;
        if (on) { if (!list.Contains(id)) list.Add(id); } else list.Remove(id);
    }
    public void Place(Vector3 position, float yaw) {
        bool kinematic = body.isKinematic;
        body.isKinematic = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
        body.position = position; body.rotation = transform.rotation;
        body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
        settled = 0; flipTimer = 0; frozenByUi = false;
        Physics.SyncTransforms();
        if (kinematic) body.WakeUp();
    }
    public void ResetToParking() { StopEngine(); Place(parkingPosition, parkingYaw); Say("车回到了旅馆前庭的停车位。"); }
    public void RightUpright() {
        var forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        float yaw = forward.sqrMagnitude > .01f ? Quaternion.LookRotation(forward).eulerAngles.y : transform.eulerAngles.y;
        Place(transform.position + Vector3.up * .7f, yaw);
        Say("你把车推正了。");
    }
    public void Say(string text, float seconds = 3.2f) { feedback = text; feedbackUntil = Time.unscaledTime + seconds; }

    // ------------------------------------------------------------------ physics
    void FixedUpdate() {
        var app = DemoApp.Instance;
        if (!app) return;
        bool occupied = app.SeatedCar == this;
        bool driver = occupied && app.CarSeatIsDriver;
        if (occupied && app.Blocked) {
            if (!body.isKinematic) { frozenVelocity = body.linearVelocity; frozenAngular = body.angularVelocity; body.isKinematic = true; frozenByUi = true; }
            return;
        }
        if (frozenByUi) {
            body.isKinematic = false; body.linearVelocity = frozenVelocity; body.angularVelocity = frozenAngular; frozenByUi = false;
        }
        if (!occupied && !EngineRunning) {
            if (!body.isKinematic) {
                bool still = body.linearVelocity.sqrMagnitude < .0036f && body.angularVelocity.sqrMagnitude < .0036f;
                settled = still ? settled + Time.fixedDeltaTime : 0;
                if (settled > 1.2f && !Flipped) { body.isKinematic = true; return; }
            } else return;
        } else if (body.isKinematic) { body.isKinematic = false; settled = 0; body.WakeUp(); }

        float dt = Time.fixedDeltaTime;
        float speed = Vector3.Dot(body.linearVelocity, transform.forward);
        SignedSpeed = speed;
        float throttle = driver ? throttleIn : 0, brake = driver ? brakeIn : 0;
        stoppedTimer = Mathf.Abs(speed) < .3f ? stoppedTimer + dt : 0;
        if (!EngineRunning) Gear = 0;
        else if (Gear == 0 && throttle > .05f) { Gear = 1; sounds?.Clunk(); }
        else if (Gear >= 0 && brake > .05f && throttle <= .05f && stoppedTimer > .25f) { Gear = -1; sounds?.Clunk(); Shifts++; }
        else if (Gear < 0 && throttle > .05f && stoppedTimer > .25f) { Gear = 1; sounds?.Clunk(); Shifts++; }
        float drive = Gear < 0 ? brake : throttle;
        float braking = Gear < 0 ? throttle : brake;
        float wheelRpm = Mathf.Abs((wheels[2].rpm + wheels[3].rpm) * .5f);
        float ratio = Gear > 0 ? Ratios[Gear - 1] : Gear < 0 ? ReverseRatio : 0;
        float coupled = wheelRpm * ratio * FinalDrive;
        if (Gear > 0 && shiftTimer <= 0) {
            if (Gear < 3 && coupled > UpshiftRpm && drive > .1f) { Gear++; shiftTimer = .35f; Shifts++; sounds?.Clunk(); }
            else if (Gear > 1 && coupled < DownshiftRpm) { Gear--; shiftTimer = .25f; Shifts++; }
            ratio = Ratios[Gear - 1]; coupled = wheelRpm * ratio * FinalDrive;
        }
        shiftTimer -= dt;
        bool shifting = shiftTimer > 0;
        float idle = IdleRpm + (Choke ? 300 : 0) + (HandThrottleUp ? 350 : 0);
        float target = !EngineRunning ? (Cranking ? 170 : 0) : Gear == 0 || shifting ? idle + drive * 1700 : Mathf.Max(coupled, idle + drive * 650);
        EngineRpm = Mathf.MoveTowards(EngineRpm, Mathf.Min(target, MaxRpm), dt * 5000);
        float torque = 0;
        if (EngineRunning && Gear != 0 && !shifting && drive > 0 && EngineRpm < MaxRpm - 15)
            torque = EngineTorque(EngineRpm) * drive * TorqueScale * (SparkAdvanced ? 1 : .82f) * ratio * FinalDrive * Efficiency * (Gear < 0 ? -1 : 1);
        float engineBrake = EngineRunning && Gear != 0 && drive < .05f && Mathf.Abs(speed) > 1 ? 32 * ratio * FinalDrive * .5f : 0;
        float hold = (throttle <= .02f && brake <= .02f && Mathf.Abs(speed) < .25f && (Gear == 0 || !EngineRunning || !driver)) ? 320 : 0;
        float maxAngle = Mathf.Lerp(MaxSteer, HighSpeedSteer, Mathf.InverseLerp(5, 25, Mathf.Abs(speed)));
        for (int i = 0; i < 4; i++) {
            var w = wheels[i];
            bool rear = i >= 2;
            w.motorTorque = rear ? torque * .5f : 0;
            float b = braking * FootBrake + hold + (rear ? engineBrake + (HandbrakeOn ? HandBrake : 0) : 0);
            w.brakeTorque = b;
            if (!rear) w.steerAngle = Steer * maxAngle;
        }
        AntiRollBar(wheels[0], wheels[1]);
        AntiRollBar(wheels[2], wheels[3]);
        var v = body.linearVelocity;
        body.AddForce(-v * v.magnitude * .9f);
        gripTimer -= dt;
        if (gripTimer <= 0) {
            gripTimer = .5f;
            var cycle = DayNightCycle.Instance;
            ApplyGrip(cycle ? Mathf.Lerp(1, .72f, cycle.Weather.rain) : 1);
        }
    }
    static float EngineTorque(float rpm) {
        // Newton-metres for an L-head four of about 40 hp.
        if (rpm < 500) return 120;
        if (rpm < 1000) return Mathf.Lerp(120, 174, (rpm - 500) / 500);
        if (rpm < 1800) return Mathf.Lerp(174, 165, (rpm - 1000) / 800);
        if (rpm < 2200) return Mathf.Lerp(165, 150, (rpm - 1800) / 400);
        return Mathf.Lerp(150, 118, (rpm - 2200) / 600);
    }
    void AntiRollBar(WheelCollider left, WheelCollider right) {
        float travelLeft = 1, travelRight = 1;
        bool groundLeft = left.GetGroundHit(out var hitLeft), groundRight = right.GetGroundHit(out var hitRight);
        if (groundLeft) travelLeft = (-left.transform.InverseTransformPoint(hitLeft.point).y - left.radius) / left.suspensionDistance;
        if (groundRight) travelRight = (-right.transform.InverseTransformPoint(hitRight.point).y - right.radius) / right.suspensionDistance;
        float force = (travelLeft - travelRight) * AntiRoll;
        if (groundLeft) body.AddForceAtPosition(left.transform.up * -force, left.transform.position);
        if (groundRight) body.AddForceAtPosition(right.transform.up * force, right.transform.position);
    }
    void ApplyGrip(float grip) {
        foreach (var w in wheels) {
            if (!w) continue;
            var f = w.forwardFriction; f.extremumSlip = .4f; f.extremumValue = 1; f.asymptoteSlip = .8f; f.asymptoteValue = .5f; f.stiffness = 1.2f * grip; w.forwardFriction = f;
            var s = w.sidewaysFriction; s.extremumSlip = .25f; s.extremumValue = 1; s.asymptoteSlip = .5f; s.asymptoteValue = .75f; s.stiffness = 1.1f * grip; w.sidewaysFriction = s;
        }
    }
    float Grip => wheels[0] ? wheels[0].sidewaysFriction.stiffness / 1.1f : 1;
    public float CurrentGrip => Grip;

    // ------------------------------------------------------------------ frame
    void Update() {
        var app = DemoApp.Instance;
        if (!app) return;
        float dt = Time.deltaTime;
        bool occupied = app.SeatedCar == this, driver = occupied && app.CarSeatIsDriver;
        bool controls = driver && !app.Blocked;
        if (controls) ReadDriverInput(app.Review); else { throttleIn = brakeIn = steerIn = 0; }
        Steer = Mathf.MoveTowards(Steer, steerIn, dt * (Mathf.Abs(steerIn) > .01f ? 1.6f : 2.4f));
        starterPulse -= dt; crankPulse -= dt; hornPulse -= dt;
        bool starterHeld = controls && ((!app.Review && Input.GetKey(KeyCode.R)) || scriptedStarter);
        bool cranking = !EngineRunning && (starterHeld || starterPulse > 0 || crankPulse > 0);
        if (cranking && !Cranking) { crankTime = 0; crankReport = null; }
        Cranking = cranking;
        if (Cranking) TickCranking(dt); else crankTime = 0;
        if (EngineRunning) {
            app.state.carWarmUntil = app.ElapsedGameMinutes + 40;
            if (!Ignition) StopEngine();
            chokeRunning = Choke ? chokeRunning + dt : 0;
            if (chokeRunning > 18 && !chokeReminded) { chokeReminded = true; Say("阻风门还拉着，发动机转得有点急。"); }
        }
        HornSounding = !app.Blocked && ((controls && ((!app.Review && Input.GetKey(KeyCode.H)) || scriptedHorn)) || hornPulse > 0);
        foreach (var p in parts) if (p && p.kind != CarPart.Kind.Wheel && p.kind != CarPart.Kind.Gauge && p.kind != CarPart.Kind.Sweep && p.kind != CarPart.Kind.Gear) p.Tick(dt);
        UpdateVisuals(dt, app);
        UpdateRain(dt);
        bool inside = occupied && !CabinOpen;
        sounds?.Tick(EngineRpm, Mathf.Clamp01(Gear < 0 ? brakeIn : throttleIn), EngineRunning, Cranking && !HandCranking, HornSounding,
            Mathf.Abs(SignedSpeed), Wipers && (EngineRunning || Ignition), inside, frozenByUi || app.GameTimePaused && !app.Review);
        flipTimer = Flipped ? flipTimer + dt : 0;
        if (!ValidPose(transform.position)) { if (occupied) app.CarRecovered(); ResetToParking(); }
    }
    void ReadDriverInput(bool review) {
        if (review) { throttleIn = scriptedThrottle; brakeIn = scriptedBrake; steerIn = scriptedSteer; return; }
        throttleIn = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
        brakeIn = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0;
        steerIn = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
        if (Input.GetKeyDown(KeyCode.Space)) ToggleHandbrake();
        if (Input.GetKeyDown(KeyCode.I)) ToggleIgnition();
        if (Input.GetKeyDown(KeyCode.C)) TogglePart("ChokeKnob", "拉出阻风门", "推回阻风门");
        if (Input.GetKeyDown(KeyCode.L)) CycleLights();
        if (Input.GetKeyDown(KeyCode.J)) TogglePart("WiperSwitch", "雨刷开了", "雨刷关了");
        if (Input.GetKeyDown(KeyCode.R) && EngineRunning) Say("发动机已经在转了。");
        if (Input.GetKeyDown(KeyCode.W) && !EngineRunning && !Cranking) Say("发动机没有发动：先打开点火开关（I），再按住 R 踩起动钮。");
    }
    void TickCranking(float dt) {
        crankTime += dt;
        // Report each distinct reason once per cranking session (ignition can change mid-crank).
        if (!Ignition) { if (crankTime > 1f && crankReport != "ignition") { crankReport = "ignition"; Say("点火开关没开，只听见" + (HandCranking ? "摇把" : "起动机") + "在转。"); } return; }
        bool cold = Cold;
        if (cold && !Choke) { if (crankTime > 2.2f && crankReport != "choke") { crankReport = "choke"; Say("冷车打不着——拉出阻风门（C）再试。"); } return; }
        if (crankTime >= (cold ? 1.2f : .6f)) StartEngine();
    }
    void StartEngine() {
        EngineRunning = true; Cranking = false; starterPulse = crankPulse = 0; chokeReminded = false; chokeRunning = 0;
        EngineRpm = IdleRpm + 300;
        Say(Choke ? "发动机着了。阻风门还拉着，热车后推回去。" : "发动机着了。");
    }
    void StopEngine() {
        if (!EngineRunning) return;
        EngineRunning = false; Gear = 0; Say("发动机熄火了。");
    }
    public void StopEngineForReview() => StopEngine();
    public void PressStarter(float seconds = 1.6f) {
        if (EngineRunning) { Say("发动机已经在转了。"); return; }
        starterPulse = seconds; sounds?.Click();
    }

    // ------------------------------------------------------------------ visuals
    void UpdateVisuals(float dt, DemoApp app) {
        for (int i = 0; i < 4; i++) {
            if (!wheels[i] || !wheelVisuals[i]) continue;
            wheels[i].GetWorldPose(out var position, out var rotation);
            wheelVisuals[i].SetPositionAndRotation(position, rotation);
        }
        Part("SteeringWheel")?.Pose(Steer * 450, Vector3.zero);
        Part("Pedal_Throttle")?.Pose((Gear < 0 ? brakeIn : throttleIn) * 18, Vector3.zero);
        Part("Pedal_Brake")?.Pose((Gear < 0 ? throttleIn : brakeIn) * 18, Vector3.zero);
        Part("Pedal_Clutch")?.Pose(shiftTimer > 0 || (EngineRunning && Gear == 0) ? 18 : 0, Vector3.zero);
        var gear = Part("GearLever");
        if (gear) {
            var e = Gear switch { 1 => new Vector3(12, 0, 9), 2 => new Vector3(-12, 0, 9), 3 => new Vector3(12, 0, -9), -1 => new Vector3(-12, 0, -9), _ => Vector3.zero };
            gear.PoseEuler(e);
        }
        Part("StarterButton")?.Pose(0, Cranking && !HandCranking ? Part("StarterButton").offset : Vector3.zero);
        Part("HornButton")?.Pose(0, HornSounding ? Part("HornButton").offset : Vector3.zero);
        Part("CrankMounted")?.Pose(crankPulse > 0 ? (1.6f - crankPulse) * 900 : 0, Vector3.zero);
        float speedAngle = Mathf.Clamp(Mathf.Abs(SignedSpeed) * 3.6f, 0, 100) / 100 * 270 - 135;
        Part("Needle_Speed")?.Pose(speedAngle, Vector3.zero);
        Part("Needle_Fuel")?.Pose(-135 + 270 * .72f, Vector3.zero);
        float amps = EngineRunning && EngineRpm > 750 ? 25 : Ignition || LightsMode > 0 ? -18 - LightsMode * 6 : 0;
        Part("Needle_Amp")?.Pose(amps * 1.6f, Vector3.zero);
        bool wiping = Wipers && (EngineRunning || Ignition);
        if (wiping || wiperAngle > -39.5f) {
            wiperPhase += dt / 1.4f;
            float sweep = -40 + 80 * (.5f - .5f * Mathf.Cos(wiperPhase * Mathf.PI * 2));
            if (!wiping) { sweep = Mathf.MoveTowards(wiperAngle, -40, dt * 120); if (sweep <= -39.5f) wiperPhase = 0; }
            wiperAngle = sweep;
            Part("WiperArm")?.Pose(wiperAngle, Vector3.zero);
        }
        ApplyLamps();
    }
    void ApplyLamps() {
        if (lampBlock == null) lampBlock = new MaterialPropertyBlock();
        for (int i = 0; i < lampRenderers.Length; i++) {
            var r = lampRenderers[i]; if (!r) continue;
            Color c = lampKinds[i] switch {
                0 => LightsMode == 2 ? new Color(1, .86f, .62f) * 3.2f : LightsMode == 1 ? new Color(1, .82f, .56f) * .9f : Color.black,
                1 => LightsMode > 0 ? new Color(1, .12f, .08f) * 2.2f : Color.black,
                // Period dash lamp: the dial faces glow softly whenever the lights are on.
                3 => LightsMode > 0 ? new Color(1, .82f, .55f) * .55f : Color.black,
                _ => DomeOn ? new Color(1, .86f, .62f) * 2.0f : Color.black };
            r.GetPropertyBlock(lampBlock, lampSlots[i]);
            lampBlock.SetColor("_EmissionColor", c);
            r.SetPropertyBlock(lampBlock, lampSlots[i]);
        }
        foreach (var l in headlamps) if (l) l.enabled = LightsMode == 2;
        if (domeLight) domeLight.enabled = DomeOn;
        TownLightBudget.Reserved = (LightsMode == 2 ? 2 : 0) + (DomeOn ? 1 : 0);
    }
    void UpdateRain(float dt) {
        if (!rainMaterial) return;
        var cycle = DayNightCycle.Instance;
        float rain = cycle ? cycle.Weather.rain : 0;
        var glass = transform.TransformPoint(windshieldPoint);
        bool covered = WeatherShelter.RoofHeight(glass + transform.forward * .06f) > glass.y + .05f;
        bool exposed = rain > .02f && !covered && !(Part("Windshield") && Part("Windshield").Fraction > .5f);
        RainOnGlass = Mathf.Clamp01(RainOnGlass + (exposed ? rain * .35f : -.04f) * dt);
        bool wiping = Wipers && (EngineRunning || Ignition);
        WipeClear = Mathf.MoveTowards(WipeClear, wiping ? 1 : 0, dt * (wiping ? 3 : .25f));
        rainMaterial.SetFloat("_Amount", RainOnGlass);
        rainMaterial.SetFloat("_Clear", WipeClear);
        windshieldRain.enabled = RainOnGlass > .005f;
    }
    void LateUpdate() {
        if (!shelter) return;
        if ((shelterPosition - transform.position).sqrMagnitude < .0025f && Quaternion.Angle(shelterRotation, transform.rotation) < 1) return;
        shelter.enabled = false; shelter.enabled = true;
        shelterPosition = transform.position; shelterRotation = transform.rotation;
    }
    // Small translucent water beads (about 3-7 mm on the 1.12 m glass) with a darker rim and a tiny highlight.
    Texture2D MakeDrops() {
        const int width = 288, height = 96;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Original windscreen droplets" };
        var pixels = new Color32[width * height];
        var random = new System.Random(1931);
        for (int d = 0; d < 720; d++) {
            float cx = (float)random.NextDouble() * width, cy = (float)random.NextDouble() * height;
            float r = .7f + (float)random.NextDouble() * (d % 11 == 0 ? 1.9f : 1.0f);
            float stretch = d % 9 == 0 ? 1.9f : 1;
            for (int y = Mathf.Max(0, (int)(cy - r * stretch - 2)); y < Mathf.Min(height, (int)(cy + r * stretch + 2)); y++)
                for (int x = Mathf.Max(0, (int)(cx - r - 2)); x < Mathf.Min(width, (int)(cx + r + 2)); x++) {
                    float dx = (x - cx) / r, dy = (y - cy) / (r * stretch), q = dx * dx + dy * dy;
                    if (q > 1) continue;
                    float hx = dx + .35f, hy = dy - .35f;
                    bool highlight = hx * hx + hy * hy < .09f;
                    byte a = highlight ? (byte)120 : (byte)(22 + 70 * q * q);
                    var c = highlight ? new Color32(235, 240, 245, a) : new Color32(150, 165, 178, a);
                    int i = y * width + x;
                    if (a > pixels[i].a) pixels[i] = c;
                }
        }
        texture.SetPixels32(pixels); texture.Apply(false, true);
        return texture;
    }

    // ------------------------------------------------------------------ sweeps and exits
    bool OwnCollider(Collider c) => c.attachedRigidbody == body;
    public bool SweepBlocked(CarPart part, float target, out bool player) {
        player = false;
        if (!part || part.leaves.Length == 0) return false;
        Physics.SyncTransforms();
        for (int i = 1; i <= 8; i++) {
            float t = Mathf.Lerp(part.Fraction, target, i / 8f);
            foreach (var leaf in part.leaves) {
                if (!leaf) continue;
                part.LeafPose(leaf, t, out var centre, out var rotation, out var half);
                foreach (var hit in Physics.OverlapBox(centre, half * .97f, rotation, ~0, QueryTriggerInteraction.Ignore)) {
                    if (OwnCollider(hit)) continue;
                    player = hit.GetComponentInParent<PlayerMotor>();
                    return true;
                }
            }
        }
        return false;
    }
    bool GroundAt(Vector3 p, out float ground) {
        ground = 0;
        var hits = Physics.RaycastAll(p + Vector3.up * 1.4f, Vector3.down, 3.4f, ~((1 << 2) | (1 << StreetLife.Layer)), QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (var h in hits) { if (OwnCollider(h.collider)) continue; if (h.distance < best) { best = h.distance; ground = h.point.y; } }
        return best < float.MaxValue;
    }
    // The walking capsule climbs anything under its 0.24m step offset, so kerbs and
    // low ledges beside the car never refuse an exit; walls, posts and the car still do.
    public static bool StandingBlocked(Vector3 feet) =>
        Physics.CheckCapsule(feet + Vector3.up * .45f, feet + Vector3.up * 1.53f, .265f, ~(1 << 2), QueryTriggerInteraction.Ignore);
    public bool FindExit(CarSeatPoint seat, out Vector3 feet, out CarPart door) {
        feet = transform.position; door = null;
        if (!Overturned) {
            var options = new[] { (seat.exitNear, seat.nearDoor), (seat.exitFar, seat.farDoor) };
            foreach (var (local, candidateDoor) in options) {
                if (candidateDoor && !candidateDoor.Open && SweepBlocked(candidateDoor, 1, out _)) continue;
                // The authored spot first, then small nudges (car resting on a kerb, a post close by).
                float outward = Mathf.Sign(local.x);
                foreach (var nudge in new[] { Vector3.zero, new Vector3(outward * .18f, 0, 0), new Vector3(0, 0, -.15f), new Vector3(outward * .18f, 0, -.2f), new Vector3(0, 0, .12f) }) {
                    var p = transform.TransformPoint(local + nudge);
                    if (!GroundAt(p, out var ground)) continue;
                    p.y = ground + .025f;
                    if (StandingBlocked(p)) continue;
                    feet = p; door = candidateDoor; return true;
                }
            }
        }
        var flat = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        var ahead = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        foreach (var offset in new[] { -flat * 2.0f, flat * 2.0f, ahead * 3.3f, -ahead * 3.5f }) {
            var p = transform.position + offset;
            if (!GroundAt(p, out var ground)) continue;
            p.y = ground + .025f;
            if (StandingBlocked(p)) continue;
            feet = p; return true;
        }
        return false;
    }
    public CarSeatPoint Seat(string id) { foreach (var s in seats) if (s && s.seatId == id) return s; return null; }
    public bool RowDoorOpen(CarSeatPoint seat) => (seat.nearDoor && seat.nearDoor.Fraction > .6f) || (seat.farDoor && seat.farDoor.Fraction > .6f);
    public void CloseDoorAfterBoarding(CarSeatPoint seat) {
        var door = seat.nearDoor && seat.nearDoor.Open ? seat.nearDoor : seat.farDoor && seat.farDoor.Open ? seat.farDoor : null;
        if (!door) return;
        door.Target = 0; SetFixture(door.fixtureId, false); sounds?.Door(false);
    }
    public void OpenDoorForExit(CarPart door) {
        if (!door || door.Open) return;
        door.Target = 1; SetFixture(door.fixtureId, true); sounds?.Door(true);
    }
    public void ApplyHandbrakeForExit() {
        var hb = Part("Handbrake");
        if (hb && !hb.Open) { hb.Target = 1; sounds?.Ratchet(); }
    }

    // ------------------------------------------------------------------ controls
    void ToggleHandbrake() {
        var hb = Part("Handbrake"); if (!hb) return;
        hb.Target = hb.Open ? 0 : 1; sounds?.Ratchet();
        Say(hb.Open ? "手刹拉起。" : "手刹放下。", 1.8f);
    }
    void ToggleIgnition() {
        var key = Part("IgnitionKey"); if (!key) return;
        key.Target = key.Open ? 0 : 1; SetFixture(key.fixtureId, key.Open); sounds?.Click();
        Say(key.Open ? "点火开关打开。" : "点火开关关上。", 1.8f);
        if (!key.Open) StopEngine();
    }
    void CycleLights() {
        LightsMode = (LightsMode + 1) % 3;
        Part("LightSwitch").Target = LightsMode / 2f;
        SetFixture("car-lights-1", LightsMode == 1); SetFixture("car-lights-2", LightsMode == 2);
        sounds?.Click(); ApplyLamps();
        Say(LightsMode == 0 ? "车灯关了。" : LightsMode == 1 ? "小灯亮了。" : "大灯亮了。", 1.8f);
    }
    void TogglePart(string id, string onText, string offText) {
        var p = Part(id); if (!p) return;
        p.Target = p.Open ? 0 : 1; SetFixture(p.fixtureId, p.Open); sounds?.Click();
        Say(p.Open ? onText : offText, 1.8f);
    }
    bool Moving => Mathf.Abs(SignedSpeed) > 1.5f && !body.isKinematic;
    bool DriverOnly(DemoApp app) {
        if (app.SeatedCar == this && app.CarSeatIsDriver) return true;
        Say("得坐在驾驶座上才够得着。"); return false;
    }

    public string ActionText(CarControl c) {
        var app = DemoApp.Instance;
        bool seatedHere = app && app.SeatedCar == this;
        var p = c.part;
        switch (c.kind) {
            case CarControlKind.Body:
                if (seatedHere) return Mathf.Abs(SignedSpeed) > StopSpeed ? "停稳后才能下车" : "下车";
                return Overturned ? "把车推正" : "查看 · 旅馆用车";
            case CarControlKind.Door: return p.Open ? "关上车门" : "打开车门";
            case CarControlKind.Seat:
                if (seatedHere) return app.CarSeat == c.seat ? (Mathf.Abs(SignedSpeed) > StopSpeed ? "停稳后才能下车" : "下车") : "挪到" + c.seat.label;
                return RowDoorOpen(c.seat) ? (c.seat.driver ? "坐上驾驶座" : "坐到" + c.seat.label) : "先打开车门再上车";
            case CarControlKind.Window: return p.Open ? "摇上车窗" : "摇下车窗";
            case CarControlKind.Hood: return p.Open ? "放下发动机罩侧板" : "掀开发动机罩侧板";
            case CarControlKind.TrunkLid: return p.Open ? "盖上行李箱" : StrapsLoose ? "打开行李箱" : "行李箱扣着皮带";
            case CarControlKind.Strap: return p.Open ? "扣上皮带" : "解开皮带";
            case CarControlKind.RadiatorCap: return p.Open ? "拧上水箱盖" : "拧开水箱盖";
            case CarControlKind.FuelCap: return p.Open ? "拧上油箱盖" : "拧开油箱盖";
            case CarControlKind.CrankHole: return CrankMounted ? "摇动摇把" : "摇把孔（摇把收在行李箱里）";
            case CarControlKind.CrankStowed: return CrankMounted ? "把摇把放回行李箱" : "取出摇把装到车头";
            case CarControlKind.Spare: return "查看备胎";
            case CarControlKind.Engine: return "查看发动机";
            case CarControlKind.ItemTools: return "查看工具卷";
            case CarControlKind.ItemTube: return "查看备用内胎";
            case CarControlKind.ItemOil: return "查看机油壶";
            case CarControlKind.ItemTarp: return "查看雨布";
            case CarControlKind.Gauges: return "查看仪表";
            case CarControlKind.Ignition: return Ignition ? "关掉点火开关" : "打开点火开关";
            case CarControlKind.Starter: return EngineRunning ? "发动机在转" : "踩起动钮";
            case CarControlKind.Choke: return Choke ? "推回阻风门" : "拉出阻风门";
            case CarControlKind.Lights: return "转动灯光开关（" + (LightsMode == 0 ? "关" : LightsMode == 1 ? "小灯" : "大灯") + "）";
            case CarControlKind.Horn: return "按喇叭";
            case CarControlKind.Spark: return SparkAdvanced ? "推迟点火" : "提前点火";
            case CarControlKind.HandThrottle: return HandThrottleUp ? "调回手油门" : "调高手油门";
            case CarControlKind.Gear: return "变速杆（" + GearName + "，自动换档）";
            case CarControlKind.Handbrake: return HandbrakeOn ? "放下手刹" : "拉起手刹";
            case CarControlKind.Wiper: return Wipers ? "关掉雨刷" : "打开雨刷";
            case CarControlKind.WindshieldCrank: return PartOpen("Windshield") ? "关上前风挡" : "推开前风挡";
            case CarControlKind.Visor: return PartOpen("SunVisor") ? "收起遮阳板" : "放下遮阳板";
            case CarControlKind.Dome: return DomeOn ? "关掉车顶灯" : "打开车顶灯";
        }
        return c.title;
    }
    public string GearName => Gear switch { -1 => "倒档", 1 => "一档", 2 => "二档", 3 => "三档", _ => "空档" };

    public void Operate(CarControl c) {
        var app = DemoApp.Instance; if (!app) return;
        var p = c.part;
        switch (c.kind) {
            case CarControlKind.Body:
                if (app.SeatedCar == this) { app.ExitCar(); return; }
                if (Overturned) { RightUpright(); return; }
                Inspect(c, "旅馆用车", "青川旅馆接送客人用的四门轿车：暗栗红车身、黑色翼子板和钢丝轮，左侧挂着备胎，车尾行李架上绑着一只行李箱。车门没上锁，钥匙就插在仪表板上。");
                return;
            case CarControlKind.Door:
                if (Moving) { Say("车还在走，不能开门。"); return; }
                ToggleWithSweep(p, "车门", true);
                return;
            case CarControlKind.Seat:
                if (app.SeatedCar == this && app.CarSeat == c.seat) { app.ExitCar(); return; }
                if (app.SeatedCar == this) { if (app.CarSeat != c.seat) { if (Mathf.Abs(SignedSpeed) > StopSpeed) Say("车还在走，坐稳别动。"); else app.MoveCarSeat(c.seat); } return; }
                if (!RowDoorOpen(c.seat)) { Say("先打开这一排的车门。"); return; }
                app.EnterCar(this, c.seat);
                return;
            case CarControlKind.Window:
                // The control sits on the crank; its part is the sliding glass of the same door.
                p.Target = p.Open ? 0 : 1; SetFixture(p.fixtureId, p.Open);
                var crank = Part("Crank_" + p.partId.Substring("Glass_".Length)); if (crank) crank.Target = p.Target;
                sounds?.Crank(); app.Save(); return;
            case CarControlKind.Hood:
                ToggleWithSweep(p, "发动机罩", false); return;
            case CarControlKind.TrunkLid:
                if (!p.Open && !StrapsLoose) { Say("行李箱还用两条皮带扣着，先解开皮带。"); return; }
                ToggleWithSweep(p, "行李箱盖", false); return;
            case CarControlKind.Strap:
                if (p.Open && TrunkOpen) { Say("先盖上行李箱，再扣皮带。"); return; }
                p.Target = p.Open ? 0 : 1; SetFixture(p.fixtureId, p.Open); sounds?.Latch(); app.Save(); return;
            case CarControlKind.RadiatorCap:
                p.Target = p.Open ? 0 : 1; SetFixture(p.fixtureId, p.Open); sounds?.Latch();
                if (p.Open) Say(EngineRunning ? "水箱有点烫，冷却水是满的。" : "水箱里的冷却水是满的。");
                app.Save(); return;
            case CarControlKind.FuelCap:
                p.Target = p.Open ? 0 : 1; SetFixture(p.fixtureId, p.Open); sounds?.Latch();
                if (p.Open) Say("油箱就在风挡前面，从加油口看得到油面：还有大半箱。");
                app.Save(); return;
            case CarControlKind.CrankHole:
                if (!CrankMounted) { Say("摇把收在车尾的行李箱里。"); return; }
                if (EngineRunning) { Say("发动机已经在转了。"); return; }
                if (SparkAdvanced) { Say("摇把猛地反弹了一下——先把火花推迟（方向盘下的拨杆）再摇。"); sounds?.Clunk(); return; }
                if (app.SeatedCar == this) { Say("得下车站到车头前面才能摇。"); return; }
                crankPulse = 1.6f; sounds?.Crank(); return;
            case CarControlKind.CrankStowed:
                if (!TrunkOpen) { Say("行李箱还盖着。"); return; }
                CrankMounted = !CrankMounted; SetFixture("car-crank-mounted", CrankMounted);
                Part("CrankMounted")?.SetVisible(CrankMounted); Part("CrankStowed")?.SetVisible(!CrankMounted);
                sounds?.Latch(); Say(CrankMounted ? "你取出摇把，插进了车头下方的摇把孔。" : "你把摇把放回了行李箱。"); app.Save(); return;
            case CarControlKind.Spare:
                Inspect(c, "备胎", "侧挂在左前翼子板里的备用钢丝轮，胎面干净，气打得很足。固定扣带上还装着一面小小的后视镜。"); return;
            case CarControlKind.Engine:
                Inspect(c, "发动机", EngineRunning ? "直列四缸正在运转，机体微微发烫，四根火花塞线随着节奏轻轻颤动。" : "直列四缸侧气门发动机，机体刷着灰绿漆。火花塞、分电器和化油器都擦得很干净，旅馆的人显然常常保养它。"); return;
            case CarControlKind.ItemTools:
                Inspect(c, "工具卷", "帆布工具卷里插着扳手、钳子、螺丝刀和一把补胎用的小锉。"); return;
            case CarControlKind.ItemTube:
                Inspect(c, "备用内胎", "叠好的备用内胎，旁边有一小盒补胎胶片。"); return;
            case CarControlKind.ItemOil:
                Inspect(c, "机油壶", "铁皮机油壶，晃一晃还剩半壶。"); return;
            case CarControlKind.ItemTarp:
                Inspect(c, "雨布", "折好的油布。下雨天接客人时，用来盖住行李架上的箱子。"); return;
            case CarControlKind.Gauges:
                Inspect(c, "仪表", "左边是电流表，" + (EngineRunning ? "指针偏向充电一侧" : Ignition ? "指针偏向放电一侧" : "指针停在中间") + "；中间是车速表，现在约 " + Mathf.RoundToInt(Mathf.Abs(SignedSpeed) * 3.6f) + " 公里；右边是油量表，还有七成左右。"); return;
        }
        if (!DriverOnlyKinds(c.kind)) {
            switch (c.kind) {
                case CarControlKind.WindshieldCrank:
                    TogglePart("Windshield", "前风挡下沿推开了，风从缝里吹进来。", "前风挡关上了。"); app.Save(); return;
                case CarControlKind.Visor:
                    TogglePart("SunVisor", "遮阳板放下了。", "遮阳板收起来了。"); app.Save(); return;
                case CarControlKind.Dome:
                    DomeOn = !DomeOn; SetFixture("car-dome", DomeOn); sounds?.Click(); ApplyLamps(); app.Save(); return;
            }
            return;
        }
        if (!DriverOnly(app)) return;
        switch (c.kind) {
            case CarControlKind.Ignition: ToggleIgnition(); break;
            case CarControlKind.Starter: PressStarter(); break;
            case CarControlKind.Choke: TogglePart("ChokeKnob", "拉出阻风门。", "推回阻风门。"); break;
            case CarControlKind.Lights: CycleLights(); break;
            case CarControlKind.Horn: hornPulse = .5f; break;
            case CarControlKind.Spark: TogglePart("SparkLever", "火花提前：发动机更有劲。", "火花推迟：手摇启动前要推到这里。"); break;
            case CarControlKind.HandThrottle: TogglePart("HandThrottle", "手油门调高，怠速快了一些。", "手油门调回。"); break;
            case CarControlKind.Gear: Say("这辆车替你换档：踩油门从一档开始，自动升到三档；停稳后按住 S 会挂倒档。", 4); break;
            case CarControlKind.Handbrake: ToggleHandbrake(); break;
            case CarControlKind.Wiper: TogglePart("WiperSwitch", "雨刷开了。", "雨刷关了。"); break;
        }
        app.Save();
    }
    static bool DriverOnlyKinds(CarControlKind kind) => kind == CarControlKind.Ignition || kind == CarControlKind.Starter || kind == CarControlKind.Choke ||
        kind == CarControlKind.Lights || kind == CarControlKind.Horn || kind == CarControlKind.Spark || kind == CarControlKind.HandThrottle ||
        kind == CarControlKind.Gear || kind == CarControlKind.Handbrake || kind == CarControlKind.Wiper;
    void ToggleWithSweep(CarPart p, string label, bool door) {
        float target = p.Open ? 0 : 1;
        if (SweepBlocked(p, target, out bool player)) {
            Say(player ? "先让开" + label + "的转动范围。" : label + "外面有东西挡着，打不开。");
            return;
        }
        p.Target = target; SetFixture(p.fixtureId, p.Open);
        if (door) sounds?.Door(p.Open); else sounds?.Latch();
        DemoApp.Instance.Save();
    }
    void Inspect(CarControl c, string title, string text) {
        c.title = title; c.body = text; c.source = "旅馆用车"; c.evidenceId = "";
        DemoApp.Instance.Inspect(c);
    }
}
