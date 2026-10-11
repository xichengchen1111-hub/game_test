using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Native review of the hotel car. Uses the real WheelCollider car, the ordinary
// 3m E ray and the same E-without-target exit path as the keyboard. Scripted
// throttle/steer replace physical keys only; it is not a blind playtest.
public partial class DemoApp {
    [Serializable] class CarReviewResult {
        public bool passed; public int width, height, controlsUsed, shifts;
        public float topSpeedKmh, stopFromKmh, stopDistance, stopDecel, zeroTo30Seconds, collisionSpeed, collisionStopZ, reverseMetres;
        public float averageDriveFrameMs, worstDriveFrameMs, restingWheelHeightError, rainOnGlass, wipeClear;
        public bool coldNeedsChoke, ignitionRequired, mufflesClosed, openWindowOutdoor, headlampsReserved, pauseFreezes, exitBlockedUsesFarSide, crankKickback, crankStart, flipRecovered;
        public string verification;
    }
    readonly List<float> carFrames = new List<float>();
    void NeedCar(bool ok, string why) {
        if (ok) return;
        var car = HotelCar.Current;
        if (car) Debug.Log("CAR_STATE: pos=" + car.transform.position + " yaw=" + car.transform.eulerAngles.y.ToString("0.0") + " speed=" + car.SignedSpeed.ToString("0.00") +
            " gear=" + car.Gear + " engine=" + car.EngineRunning + " rpm=" + car.EngineRpm.ToString("0") + " handbrake=" + car.HandbrakeOn + " kinematic=" + car.body.isKinematic +
            " frozen=" + car.FrozenByUi + " input=" + car.scriptedThrottle + "/" + car.scriptedBrake + "/" + car.scriptedSteer + " blocked=" + Blocked + " grounded=" + string.Join(",", car.wheels.Select(w => w.isGrounded ? "1" : "0")) +
            " contacts=" + string.Join(";", Physics.OverlapBox(car.transform.TransformPoint(new Vector3(0, .9f, 0)), new Vector3(1.1f, .8f, 2.6f), car.transform.rotation, ~(1 << 2), QueryTriggerInteraction.Ignore).Where(c => c.attachedRigidbody != car.body).Select(c => c.name).Distinct().Take(8)));
        Debug.LogError("CAR_REVIEW_FAIL: " + why);
        throw new InvalidOperationException("CAR_REVIEW_FAIL: " + why);
    }
    CarControl CarControlById(string id) => FindObjectsByType<CarControl>(FindObjectsSortMode.None).Single(c => c.controlId == id);
    Vector3 CarControlPoint(CarControl c) {
        var own = c.GetComponent<Collider>();
        if (own) return own.bounds.center;
        if (c.part && c.part.leaves.Length > 0) return c.part.leaves[0].bounds.center;
        return c.transform.position;
    }
    // Aim the eye at a control and press E exactly as Update does.
    IEnumerator CarUse(string id, int retries = 0) {
        var c = CarControlById(id);
        player.Aim(CarControlPoint(c)); yield return null;
        Physics.SyncTransforms();
        NeedCar(Physics.Raycast(player.eye.transform.position, player.eye.transform.forward, out var hit, 3f), "E ray missed " + id + " from " + player.eye.transform.position);
        NeedCar(hit.collider.GetComponentInParent<Interactable>() == c, "E ray reached " + hit.collider.name + " instead of " + id + " hit=" + hit.point + " target=" + CarControlPoint(c) + " eye=" + player.eye.transform.position);
        NeedCar(focus == c || focus == null, "focus prompt mismatch for " + id);
        NeedCar(TryInteract(), "E did nothing on " + id);
        carControlsUsed.Add(id);
    }
    readonly HashSet<string> carControlsUsed = new HashSet<string>();
    void PressE() { if (!TryInteract() && CarSeated) ExitCar(); }
    IEnumerator CarWait(float seconds) { float until = Time.time + seconds; while (Time.time < until) yield return null; }
    IEnumerator CarWalk(Vector3 target) {
        float deadline = Time.unscaledTime + Vector3.Distance(player.transform.position, target) / 2.2f + 8;
        while (Time.unscaledTime < deadline) {
            var delta = target - player.transform.position; delta.y = 0; if (delta.magnitude < .09f) break;
            player.scriptedDirection = delta.normalized * Mathf.Clamp(delta.magnitude * 2, .1f, 1); yield return null;
        }
        player.scriptedDirection = Vector3.zero;
        NeedCar(Time.unscaledTime < deadline, "walk blocked toward " + target + " at " + player.transform.position);
    }
    // Stage the player at a clear standing point beside the car (car-local), on the real ground.
    void PlaceBesideCar(HotelCar car, Vector3 local, float faceYaw = float.NaN) {
        var p = car.transform.TransformPoint(local);
        var hits = Physics.RaycastAll(p + Vector3.up * 1.5f, Vector3.down, 3.5f, ~(1 << 2), QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (var h in hits) if (h.collider.attachedRigidbody != car.body && h.distance < best) { best = h.distance; p.y = h.point.y + .03f; }
        NeedCar(best < float.MaxValue, "no ground beside car at " + local);
        Physics.SyncTransforms();
        NeedCar(!HotelCar.StandingBlocked(p), "staged standing point blocked at car-local " + local + " by " + string.Join(",",
            Physics.OverlapCapsule(p + Vector3.up * .45f, p + Vector3.up * 1.53f, .265f, ~(1 << 2), QueryTriggerInteraction.Ignore).Select(c => c.name)));
        player.Place(p, float.IsNaN(faceYaw) ? car.transform.eulerAngles.y : faceYaw);
    }
    // Stand where the game itself would set an occupant of this seat down (door side first).
    void PlaceAtDoorway(HotelCar car, string seatId) {
        NeedCar(car.FindExit(car.Seat(seatId), out var doorway, out _), "no doorway for " + seatId);
        player.Place(doorway, car.transform.eulerAngles.y + (car.Seat(seatId).side < 0 ? 90 : -90));
    }
    IEnumerator DriveTo(HotelCar car, Vector3 target, float cruise, float arrive = 1.6f, float timeout = 45) {
        float until = Time.time + timeout;
        while (true) {
            var p = car.transform.position; var to = target - p; to.y = 0;
            if (to.magnitude < arrive) break;
            var forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            float angle = Vector3.SignedAngle(forward, to.normalized, Vector3.up);
            car.scriptedSteer = Mathf.Clamp(angle / 22f, -1, 1);
            float want = Mathf.Lerp(cruise, 3.2f, Mathf.InverseLerp(10, 45, Mathf.Abs(angle)));
            want = Mathf.Min(want, Mathf.Max(2.6f, to.magnitude * .8f));
            car.scriptedThrottle = car.SignedSpeed < want - .3f ? 1 : 0;
            car.scriptedBrake = car.SignedSpeed > want + 1.2f ? .7f : 0;
            carFrames.Add(Time.unscaledDeltaTime);
            NeedCar(Time.time < until, "drive timed out toward " + target + " at " + p);
            NeedCar(!car.Flipped, "car rolled over while driving at " + p);
            yield return null;
        }
    }
    IEnumerator StopCar(HotelCar car, float timeout = 12) {
        float until = Time.time + timeout;
        car.scriptedSteer = 0;
        // In reverse the forward pedal brakes; release at a standstill so neither pedal re-engages a gear.
        while (Mathf.Abs(car.SignedSpeed) > .15f) {
            bool reverse = car.Gear < 0;
            car.scriptedThrottle = reverse ? 1 : 0; car.scriptedBrake = reverse ? 0 : 1;
            NeedCar(Time.time < until, "car did not stop"); yield return null;
        }
        car.scriptedBrake = 0; car.scriptedThrottle = 0;
        if (!car.HandbrakeOn) yield return CarUse("handbrake");
        yield return CarWait(.4f);
    }

    IEnumerator ReviewCar() {
        yield return new WaitForSeconds(.8f);
        var result = new CarReviewResult { width = Screen.width, height = Screen.height };
        var car = HotelCar.Current;
        NeedCar(car, "hotel car missing from Town");
        DayReviewTime(1075); dayNightReviewFrozen = false; DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);
        NeedCar(Vector3.Distance(Flat(car.transform.position), Flat(car.parkingPosition)) < .3f, "car not at hotel parking " + car.transform.position);
        float settleUntil = Time.time + 4;
        while (!car.Parked && Time.time < settleUntil) yield return null;
        NeedCar(car.Parked && !car.Flipped, "parked car did not settle onto its wheels");
        float worst = 0;
        foreach (var wheel in car.wheels) {
            NeedCar(wheel.GetGroundHit(out var hit), "wheel not on the ground " + wheel.name);
            worst = Mathf.Max(worst, Mathf.Abs(car.transform.InverseTransformPoint(hit.point).y));
        }
        result.restingWheelHeightError = worst;
        NeedCar(worst < .07f, "car body not resting at authored ride height, wheel contact offset " + worst);

        // Exterior views.
        player.Place(car.transform.TransformPoint(new Vector3(-3.2f, 0, 4.4f)) + Vector3.up * .05f, 0);
        player.Aim(car.transform.TransformPoint(new Vector3(0, .9f, .2f))); yield return Capture("car-parked-front");
        player.Place(car.transform.TransformPoint(new Vector3(2.6f, 0, -4.6f)) + Vector3.up * .05f, 0);
        player.Aim(car.transform.TransformPoint(new Vector3(0, .9f, -1.2f))); yield return Capture("car-parked-rear");

        // Open the driver's door with E, board, and confirm the door shuts behind.
        var driver = car.Seat("driver");
        // Stand clear of the door's swing, open it, then step into the doorway.
        PlaceBesideCar(car, new Vector3(-1.3f, 0, -.75f), car.transform.eulerAngles.y + 90);
        yield return null;
        var seatControl = CarControlById("seat-driver");
        NeedCar(seatControl.ActionText == "先打开车门再上车", "closed door must prevent boarding");
        yield return CarUse("door-FL");
        yield return CarWait(.9f);
        NeedCar(driver.nearDoor.Fraction > .99f && driver.nearDoor.leaves[0].bounds.center.x < car.transform.TransformPoint(new Vector3(-.65f, 0, 0)).x - .1f, "driver door did not swing outward");
        yield return Capture("car-driver-door-open");
        PlaceBesideCar(car, driver.exitNear, car.transform.eulerAngles.y + 90); yield return null;
        yield return CarUse("seat-driver");
        NeedCar(CarSeated && CarSeatIsDriver && MovementLocked && LookAllowed && !player.GetComponent<CharacterController>().enabled, "boarding did not seat the driver");
        yield return CarWait(1.0f);
        NeedCar(driver.nearDoor.Fraction < .01f, "driver door did not close after boarding");
        player.Aim(car.transform.TransformPoint(new Vector3(-.2f, 1.2f, 3))); yield return Capture("car-driver-view");

        // Cold start: starter without ignition, then without choke, then with choke.
        yield return CarUse("starter"); yield return CarWait(1.4f);
        NeedCar(!car.EngineRunning && car.Feedback != null && car.Feedback.Contains("点火开关没开"), "starter without ignition must not start");
        result.ignitionRequired = true;
        yield return CarUse("ignition"); NeedCar(car.Ignition, "ignition switch did not turn on");
        car.scriptedStarter = true; yield return CarWait(2.6f); car.scriptedStarter = false;
        NeedCar(!car.EngineRunning && car.Cold && car.Feedback != null && car.Feedback.Contains("阻风门"), "cold engine started without the choke");
        result.coldNeedsChoke = true;
        yield return CarUse("choke"); NeedCar(car.Choke, "choke did not pull out");
        yield return CarUse("starter"); yield return CarWait(1.6f);
        NeedCar(car.EngineRunning, "choked cold engine failed to start");
        yield return CarWait(.4f);
        NeedCar(car.sounds.EnginePlaying && car.EngineRpm > HotelCar.IdleRpm, "engine sound/rpm missing");
        yield return CarUse("choke"); NeedCar(!car.Choke, "choke did not push in");
        player.Aim(car.transform.TransformPoint(new Vector3(0, 1.06f, .25f))); yield return Capture("car-gauges-running");

        // Other driver controls through the ordinary ray.
        yield return CarUse("wiperSwitch"); NeedCar(car.Wipers, "wipers did not switch on");
        yield return CarUse("lightSwitch"); NeedCar(car.LightsMode == 1, "light switch first step");
        yield return CarUse("lightSwitch"); NeedCar(car.LightsMode == 2 && car.headlamps.All(l => l.enabled) && TownLightBudget.Reserved == 2, "headlamps not lit or budget not reserved");
        yield return CarUse("lightSwitch"); NeedCar(car.LightsMode == 0 && TownLightBudget.Reserved == 0, "lights did not switch off");
        yield return CarUse("dome"); NeedCar(car.DomeOn && car.domeLight.enabled, "dome lamp");
        yield return CarUse("dome"); NeedCar(!car.DomeOn, "dome lamp off");
        yield return CarUse("visor"); yield return CarWait(.6f); yield return Capture("car-visor-down"); yield return CarUse("visor");
        yield return CarUse("SparkLever"); NeedCar(car.SparkAdvanced, "spark advance");
        yield return CarUse("HandThrottle"); NeedCar(car.HandThrottleUp, "hand throttle"); yield return CarUse("HandThrottle");
        yield return CarUse("horn"); yield return CarWait(.15f); NeedCar(car.HornSounding && car.sounds.HornVolume > .1f, "horn silent");
        yield return CarUse("gear"); NeedCar(car.Feedback != null && car.Feedback.Contains("自动"), "gear explanation");
        yield return CarUse("gauges"); NeedCar(inspected && Blocked, "gauge inspection"); CloseInspect();
        yield return CarUse("windshieldCrank"); yield return CarWait(1f); NeedCar(car.CabinOpen, "windscreen push-out should open the cabin");
        yield return CarUse("windshieldCrank"); yield return CarWait(1f);
        NeedCar(!car.CabinOpen && WeatherShelter.Covered(player.eye.transform.position) && WeatherShelter.MufflesRain(player.eye.transform.position), "closed cabin must keep rain off and muffle it");
        result.mufflesClosed = true;
        yield return CarUse("window-FL"); yield return CarWait(.8f);
        NeedCar(car.CabinOpen && !WeatherShelter.MufflesRain(player.eye.transform.position), "open window must let outdoor rain sound in");
        result.openWindowOutdoor = true;
        yield return CarUse("window-FL"); yield return CarWait(1.2f);
        NeedCar(!HeldItemBlockedByDriving(), "watch/map should be usable when stopped with hand brake");

        // Drive out of the hotel court onto Station Road (north lane, westbound).
        yield return CarUse("handbrake"); NeedCar(!car.HandbrakeOn, "hand brake release");
        NeedCar(HeldItemBlockedByDriving(), "driver's hands must be busy with the brake off");
        int maxGear = 0;
        foreach (var point in new[] { new Vector3(169.5f, 0, 13), new Vector3(168.8f, 0, 18.4f), new Vector3(160, 0, 21.5f) }) {
            var drive = StartCoroutine(DriveTo(car, point, 6));
            while (Vector3.Distance(Flat(car.transform.position), Flat(point)) >= 1.6f) { maxGear = Mathf.Max(maxGear, car.Gear); yield return null; }
            StopCoroutine(drive);
        }
        // Straight full-throttle run west: 0-30 km/h, automatic upshifts, a pause freeze, then full braking.
        var runStart = car.transform.position;
        float launch = Time.time, zeroTo30 = -1, top = 0; bool pauseTested = false;
        float runUntil = Time.time + 16;
        while ((Vector3.Distance(Flat(runStart), Flat(car.transform.position)) < 30 && car.SignedSpeed < 13) || car.SignedSpeed < 6) {
            var forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            var aim = new Vector3(car.transform.position.x - 25, 0, 21.5f) - Flat(car.transform.position);
            car.scriptedSteer = Mathf.Clamp(Vector3.SignedAngle(forward, aim.normalized, Vector3.up) / 12f, -1, 1);
            car.scriptedThrottle = 1; car.scriptedBrake = 0;
            top = Mathf.Max(top, car.SignedSpeed); maxGear = Mathf.Max(maxGear, car.Gear);
            if (zeroTo30 < 0 && car.SignedSpeed * 3.6f >= 30) zeroTo30 = Time.time - launch;
            carFrames.Add(Time.unscaledDeltaTime);
            if (!pauseTested && car.SignedSpeed > 6) {
                player.Aim(car.transform.TransformPoint(new Vector3(-.3f, 1.25f, 9))); yield return null;
                yield return Capture("car-driving-rain");
                var before = car.transform.position; float speedBefore = car.SignedSpeed;
                paused = true; yield return CarWait(.8f);
                NeedCar(car.FrozenByUi && Vector3.Distance(before, car.transform.position) < .9f, "pause must freeze the moving car");
                paused = false; yield return CarWait(.15f);
                NeedCar(!car.FrozenByUi && car.SignedSpeed > speedBefore * .6f, "unpause must restore motion");
                pauseTested = result.pauseFreezes = true;
            }
            NeedCar(Time.time < runUntil && car.transform.position.x > 118 && !car.Flipped, "acceleration run failed at " + car.transform.position + " speed " + car.SignedSpeed);
            yield return null;
        }
        result.topSpeedKmh = top * 3.6f; result.zeroTo30Seconds = zeroTo30; result.shifts = car.Shifts;
        NeedCar(maxGear >= 2 && car.Shifts >= 2, "automatic gearbox did not shift up: max gear " + maxGear);
        NeedCar(car.RainOnGlass > .1f && car.WipeClear > .8f, "rain/wiper state on glass");
        result.rainOnGlass = car.RainOnGlass; result.wipeClear = car.WipeClear;
        car.scriptedThrottle = 0; car.scriptedSteer = 0;
        float v0 = car.SignedSpeed; var brakeStart = car.transform.position;
        car.scriptedBrake = 1;
        while (car.SignedSpeed > .15f) yield return null;
        car.scriptedBrake = 0;
        result.stopFromKmh = v0 * 3.6f; result.stopDistance = Vector3.Distance(Flat(brakeStart), Flat(car.transform.position));
        result.stopDecel = v0 * v0 / (2 * Mathf.Max(.01f, result.stopDistance));
        NeedCar(v0 > 6 && result.stopDecel > 2.2f && result.stopDecel < 9.5f, "implausible braking: from " + v0 + "m/s over " + result.stopDistance + "m");
        // Reverse: hold S at a standstill engages reverse and backs the car east along the lane.
        var reverseStart = car.transform.position;
        car.scriptedBrake = 1; yield return CarWait(2.2f); car.scriptedBrake = 0;
        result.reverseMetres = Vector3.Distance(Flat(reverseStart), Flat(car.transform.position));
        NeedCar(car.Gear == -1 && Vector3.Dot(car.transform.position - reverseStart, car.transform.forward) < -1.0f, "reverse gear did not back the car up");
        yield return StopCar(car);
        yield return CarUse("handbrake");
        // Low-speed collision: turn north into the H16/H17 shopfront (facade z 27.7), clear of the verge trees.
        // Creep forward or back along the lane to x=118, midway between the verge trees at 110 and 124.
        float lineUntil = Time.time + 20;
        var laneMark = new Vector3(118, 0, 21.5f);
        while (Mathf.Abs(car.transform.position.x - 118) > .6f || Mathf.Abs(car.SignedSpeed) > .2f) {
            bool ahead = car.transform.position.x > 118, near = Mathf.Abs(car.transform.position.x - 118) <= .6f;
            // Steer back to the lane centre (z 21.5); reversing inverts the steering effect.
            var flatForward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            var toward = new Vector3(car.transform.position.x + (ahead ? -6 : 6), 0, laneMark.z) - Flat(car.transform.position);
            float laneAngle = Vector3.SignedAngle(ahead ? flatForward : -flatForward, toward.normalized, Vector3.up);
            car.scriptedSteer = Mathf.Clamp(laneAngle / 18f, -1, 1) * (ahead ? 1 : -1);
            float v = car.SignedSpeed;
            if (near) { car.scriptedThrottle = car.Gear < 0 && v < -.1f ? 1 : 0; car.scriptedBrake = car.Gear >= 0 && v > .1f ? 1 : 0; }
            else if (ahead) {
                // Need to go forward (west): stop any backward roll first, then creep.
                if (v < -.1f) { car.scriptedThrottle = car.Gear < 0 ? 1 : 0; car.scriptedBrake = car.Gear >= 0 ? 1 : 0; }
                else { car.scriptedThrottle = v < 1.6f ? 1 : 0; car.scriptedBrake = 0; }
            } else {
                // Need to go back: brake any forward motion; holding S at a standstill engages reverse.
                car.scriptedThrottle = 0;
                car.scriptedBrake = v > .1f ? 1 : v > -1.6f ? 1 : 0;
            }
            NeedCar(Time.time < lineUntil, "could not line up at x=118: " + car.transform.position);
            yield return null;
        }
        car.scriptedThrottle = car.scriptedBrake = 0;
        NeedCar(car.transform.position.x > 116 && car.transform.position.x < 120, "car not lined up between the verge trees: " + car.transform.position);
        float collisionSpeed = 0; float crashUntil = Time.time + 14;
        while (Time.time < crashUntil) {
            var forward = Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized;
            car.scriptedSteer = Mathf.Clamp(Vector3.SignedAngle(forward, Vector3.forward, Vector3.up) / 20, -1, 1);
            car.scriptedThrottle = car.SignedSpeed < 4.5f ? 1 : 0; car.scriptedBrake = 0;
            collisionSpeed = Mathf.Max(collisionSpeed, car.SignedSpeed);
            if (car.transform.position.z > 24 && car.SignedSpeed < .3f) break;
            NeedCar(!car.Flipped, "car rolled over approaching the facade");
            yield return null;
        }
        car.scriptedThrottle = 0; car.scriptedSteer = 0;
        result.collisionSpeed = collisionSpeed * 3.6f; result.collisionStopZ = car.transform.position.z;
        NeedCar(car.transform.position.x > 93 && car.transform.position.x < 129, "collision missed the shopfronts: " + car.transform.position);
        NeedCar(car.transform.position.z > 24 && car.transform.position.z < 27.7f - 1.2f && !car.Flipped && CarSeated, "car passed through or bounced wrongly from the facade at z=" + car.transform.position.z);
        player.Aim(car.transform.TransformPoint(new Vector3(0, 1.3f, 3))); yield return Capture("car-facade-collision");
        yield return StopCar(car);
        yield return CarUse("handbrake");
        car.scriptedBrake = 1; yield return CarWait(2.6f); car.scriptedBrake = 0;
        yield return StopCar(car);
        NeedCar(27.7f - (car.transform.position.z + 1.95f) > 1.6f, "did not back away from the facade: " + car.transform.position);
        // Night with headlamps (staged time, isolated save).
        DayReviewTime(1260); DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast); dayNightReviewFrozen = true;
        yield return CarUse("lightSwitch"); yield return CarUse("lightSwitch");
        NeedCar(car.LightsMode == 2 && car.headlamps.All(l => l.enabled), "night headlamps");
        result.headlampsReserved = TownLightBudget.Reserved == 2;
        player.Aim(car.transform.TransformPoint(new Vector3(0, .6f, 9))); yield return CarWait(.3f); yield return Capture("car-night-headlamps-driver");

        // Exit with E on no particular control; the hand brake is applied and the door opens.
        yield return CarUse("ignition"); NeedCar(!car.EngineRunning, "ignition off should stop the engine");
        yield return CarUse("ignition"); NeedCar(car.Ignition && !car.EngineRunning, "ignition back on, engine still stopped");
        player.Aim(car.transform.TransformPoint(new Vector3(-.3f, 1.55f, 8))); yield return null;
        NeedCar(focus == null || focus is CarControl cc && cc.kind == CarControlKind.Body, "exit aim should not target a control");
        PressE(); yield return null;
        NeedCar(!CarSeated && player.GetComponent<CharacterController>().enabled && car.HandbrakeOn, "E did not exit the car");
        NeedCar(car.transform.InverseTransformPoint(player.transform.position).x < -.9f, "driver should exit on the driver's side");
        NeedCar(!HotelCar.StandingBlocked(player.transform.position), "exit placed the player inside an obstacle");
        yield return CarWait(.8f);
        player.Aim(car.transform.TransformPoint(new Vector3(0, 1.0f, 0))); yield return Capture("car-night-exit");
        // Outside view of the lit headlamps and their pool on the shopfront ahead.
        PlaceBesideCar(car, new Vector3(-1.9f, 0, 1.2f)); player.Aim(car.transform.TransformPoint(new Vector3(.2f, .8f, 3.0f))); yield return CarWait(.3f);
        yield return Capture("car-night-headlamps-outside");
        DayReviewTime(1075); DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast); dayNightReviewFrozen = false;

        // Blocked driver side: exit uses the passenger side instead.
        // Step back into the doorway exactly where the game itself would set the driver down.
        NeedCar(car.FindExit(car.Seat("driver"), out var doorway, out _), "no driver doorway");
        player.Place(doorway, car.transform.eulerAngles.y + 90);
        yield return CarUse("seat-driver"); yield return CarWait(.6f);
        NeedCar(CarSeated, "re-boarding through the open door");
        // A post where the driver would stand, clear of the open door leaf itself.
        // Covers the doorway spot and every nudge the exit search tries, without touching the open door leaf.
        var blockPoint = car.transform.TransformPoint(new Vector3(-1.45f, .9f, -.34f));
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.name = "Temporary exit obstruction";
        blocker.transform.SetPositionAndRotation(blockPoint, car.transform.rotation); blocker.transform.localScale = new Vector3(.6f, 1.8f, .64f);
        Physics.SyncTransforms();
        NeedCar(ExitCar(), "exit with driver side blocked failed");
        result.exitBlockedUsesFarSide = car.transform.InverseTransformPoint(player.transform.position).x > .9f;
        NeedCar(result.exitBlockedUsesFarSide, "blocked exit did not use the passenger side");
        Destroy(blocker); yield return null;

        // Exterior features: hood sides, engine, caps, spare, trunk straps/lid/items, crank.
        PlaceBesideCar(car, new Vector3(1.28f, 0, 1.3f));
        yield return CarUse("hood-R"); yield return CarWait(1.6f);
        NeedCar(CarControlById("hood-R").part.Fraction > .99f, "right hood side did not lift: fraction " + CarControlById("hood-R").part.Fraction + " feedback " + car.Feedback);
        yield return CarUse("engine"); NeedCar(inspected, "engine inspection through the open hood"); CloseInspect();
        player.Aim(car.transform.TransformPoint(new Vector3(0, .8f, 1.24f))); yield return Capture("car-hood-open-engine");
        yield return CarUse("hood-R"); yield return CarWait(1.6f);
        PlaceBesideCar(car, new Vector3(.5f, 0, 2.6f));
        yield return CarUse("radiatorCap"); yield return CarWait(.5f); yield return CarUse("radiatorCap");
        PlaceBesideCar(car, new Vector3(-1.32f, 0, 1.45f));
        yield return CarUse("hood-L"); yield return CarWait(1.6f); NeedCar(CarControlById("hood-L").part.Fraction > .99f, "left hood side did not lift");
        yield return CarUse("hood-L"); yield return CarWait(1.6f);
        yield return CarUse("spare"); NeedCar(inspected, "spare inspection"); CloseInspect();
        yield return CarUse("fuelCap"); yield return CarWait(.5f); NeedCar(car.Feedback != null && car.Feedback.Contains("油"), "fuel cap feedback"); yield return CarUse("fuelCap");
        PlaceBesideCar(car, new Vector3(.35f, 0, -2.75f));
        yield return CarUse("trunkLid"); NeedCar(!car.TrunkOpen && car.Feedback.Contains("皮带"), "buckled trunk must refuse to open");
        yield return CarUse("strap-L"); yield return CarUse("strap-R"); yield return CarWait(.5f);
        yield return CarUse("trunkLid"); yield return CarWait(1.4f); NeedCar(car.TrunkOpen && CarControlById("trunkLid").part.Fraction > .99f, "trunk lid did not open");
        PlaceBesideCar(car, new Vector3(1.0f, 0, -2.0f));
        foreach (var item in new[] { "item-tools", "item-tube", "item-oil", "item-tarp" }) { yield return CarUse(item); NeedCar(inspected, "trunk item " + item); CloseInspect(); }
        player.Aim(car.transform.TransformPoint(new Vector3(0, .75f, -1.98f))); yield return Capture("car-trunk-open");
        yield return CarUse("crankStowed"); NeedCar(car.CrankMounted, "crank not taken");
        // Spark advanced from the seat earlier: the crank kicks back, then retard and crank again.
        PlaceBesideCar(car, new Vector3(.45f, 0, 2.65f));
        yield return CarUse("crankHole"); NeedCar(!car.EngineRunning && car.Feedback.Contains("反弹"), "advanced spark must kick back"); result.crankKickback = true;
        PlaceAtDoorway(car, "passenger");
        yield return CarUse("seat-passenger"); yield return CarWait(.5f); NeedCar(CarSeated && !CarSeatIsDriver, "passenger seat");
        yield return CarUse("SparkLever"); NeedCar(car.SparkAdvanced, "passenger must not reach driver levers");
        yield return CarUse("seat-driver"); yield return CarWait(.5f); NeedCar(CarSeatIsDriver, "slide to driver seat");
        yield return CarUse("SparkLever"); NeedCar(!car.SparkAdvanced, "spark retard");
        NeedCar(ExitCar(), "exit to crank");
        PlaceBesideCar(car, new Vector3(.45f, 0, 2.65f));
        yield return CarUse("crankHole"); yield return CarWait(1.8f);
        NeedCar(car.EngineRunning, "hand crank start failed"); result.crankStart = true;
        player.Aim(car.transform.TransformPoint(new Vector3(0, .6f, 1.85f))); yield return Capture("car-crank-start");

        // Overturned car can be pushed upright from outside.
        var keep = car.transform.position;
        PlaceBesideCar(car, new Vector3(-3.4f, 0, -.3f));
        car.StopEngineForReview();
        // Shut the doors first (same handler as E), so no open leaf is driven into the ground,
        // then turn the car over onto its roof and let it settle.
        foreach (var id in new[] { "door-FL", "door-FR", "door-RL", "door-RR" }) { var door = CarControlById(id); if (door.part.Open) door.Use(); }
        yield return CarWait(1.0f);
        NeedCar(car.parts.Where(p => p.partId.StartsWith("Door_")).All(p => p.Fraction < .01f), "doors did not shut before the tip test");
        float tipYaw = car.transform.eulerAngles.y;
        // Set it down on its roof just clear of the ground (roof top is 1.79 m above the car origin).
        car.Place(keep + Vector3.up * 1.84f, tipYaw);
        car.transform.rotation = Quaternion.Euler(0, tipYaw, 180); car.body.rotation = car.transform.rotation;
        float flipUntil = Time.time + 7; while (!car.Overturned && Time.time < flipUntil) yield return null;
        NeedCar(car.Overturned, "flip detection");
        yield return CarWait(1.2f); yield return Capture("car-overturned");
        var bodyControl = car.GetComponent<CarControl>();
        NeedCar(bodyControl.ActionText == "把车推正", "overturned prompt");
        bodyControl.Use(); yield return CarWait(2.5f);
        NeedCar(!car.Flipped, "car was not righted"); result.flipRecovered = true;

        // Checkpoint for a separate process: seated driver, trunk open, crank mounted, window down, lights on.
        DayReviewTime(1075); dayNightReviewFrozen = false;
        var seat = car.Seat("driver");
        if (!car.RowDoorOpen(seat)) { PlaceBesideCar(car, new Vector3(-1.3f, 0, -.75f)); yield return CarUse("door-FL"); yield return CarWait(.9f); }
        PlaceAtDoorway(car, "driver");
        yield return CarUse("seat-driver"); yield return CarWait(.8f);
        NeedCar(CarSeatIsDriver, "checkpoint seat");
        yield return CarUse("window-FL");
        for (int i = 0; i < 3 && car.LightsMode != 1; i++) yield return CarUse("lightSwitch");
        yield return CarWait(1.2f);
        Save(); File.Copy(savePath, Path.Combine(captures, "car-checkpoint.json"), true);
        var saved = JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
        NeedCar(saved.carSaved && saved.carSeatId == "driver" && saved.openFixtures.Contains("car-trunk-lid") && saved.openFixtures.Contains("car-crank-mounted") &&
                saved.openFixtures.Contains("car-window-fl") && saved.openFixtures.Contains("car-lights-1") && !HotelCar.StandingBlocked(new Vector3(saved.x, saved.y, saved.z)), "checkpoint save incomplete");
        result.controlsUsed = carControlsUsed.Count;
        result.averageDriveFrameMs = carFrames.Count > 0 ? carFrames.Average() * 1000 : 0;
        result.worstDriveFrameMs = carFrames.Count > 0 ? carFrames.Max() * 1000 : 0;
        result.passed = true;
        result.verification = "Native player: real WheelCollider physics and Rigidbody collisions, ordinary 3m E ray on every listed control, scripted throttle/steer/brake values in place of physical keys; staged rain/night in an isolated save. Not a blind playtest or a low-end frame-rate benchmark.";
        File.WriteAllText(Path.Combine(captures, "car-review-results.json"), JsonUtility.ToJson(result, true));
        Debug.Log("CAR_REVIEW_PASS controls=" + result.controlsUsed + " top=" + result.topSpeedKmh + "km/h stop=" + result.stopDistance + "m");
        Application.Quit();
    }
    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0, v.z);

    IEnumerator VerifyCarSave() {
        yield return new WaitForSeconds(1.0f);
        var car = HotelCar.Current;
        NeedCar(car && CarSeated && CarSeatIsDriver && SeatedCar == car, "driver seat not restored in a new process");
        NeedCar(Vector3.Distance(Flat(car.transform.position), Flat(new Vector3(state.carX, 0, state.carZ))) < .25f, "car pose not restored");
        NeedCar(car.TrunkOpen && car.CrankMounted && car.LightsMode == 1 && car.CabinOpen && car.HandbrakeOn && !car.EngineRunning, "car fixtures not restored");
        NeedCar(MovementLocked && LookAllowed, "seated controls after reload");
        player.Aim(car.transform.TransformPoint(new Vector3(-.2f, 1.2f, 3))); yield return Capture("car-reloaded-driver");
        if (!car.Ignition) yield return CarUse("ignition");
        if (car.Cold && !car.Choke) yield return CarUse("choke");
        yield return CarUse("starter"); yield return CarWait(1.8f);
        NeedCar(car.EngineRunning, "engine start after reload");
        if (car.Choke) yield return CarUse("choke");
        yield return CarUse("handbrake");
        var start = car.transform.position;
        car.scriptedThrottle = 1; car.scriptedSteer = 0; yield return CarWait(1.5f); car.scriptedThrottle = 0;
        yield return StopCar(car);
        NeedCar(Vector3.Distance(Flat(start), Flat(car.transform.position)) > 1.5f, "reloaded car could not drive");
        NeedCar(ExitCar() && !CarSeated, "exit after reload");
        File.WriteAllText(Path.Combine(captures, "car-save-results.json"), "{\"passed\":true,\"separateProcess\":true,\"seatRestored\":true,\"fixturesRestored\":true,\"drivesAfterReload\":true}");
        Debug.Log("CAR_SAVE_PASS");
        Application.Quit();
    }
}
