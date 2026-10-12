using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Geometry contract of the generated hotel car prefab. Runtime types are read by
// reflection because editor test assemblies cannot reference Assembly-CSharp.
public class HotelCarPrefabTests {
    GameObject car;
    static readonly Vector3 Far = new Vector3(0, 400, 0);
    [SetUp] public void Load() {
        car = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/HotelCar.prefab"));
        car.transform.position = Far;
        Physics.SyncTransforms();
    }
    [TearDown] public void Clean() { if (car) UnityEngine.Object.DestroyImmediate(car); }
    static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(o);
    static object Prop(object o, string name) => o.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance).GetValue(o);
    Component[] Parts => car.GetComponentsInChildren<Component>(true).Where(c => c && c.GetType().Name == "CarPart").ToArray();
    Component Part(string id) => Parts.Single(p => (string)Field(p, "partId") == id);
    static void Snap(Component part, float t) => part.GetType().GetMethod("Snap").Invoke(part, new object[] { t });
    Component[] Seats => car.GetComponentsInChildren<Component>(true).Where(c => c && c.GetType().Name == "CarSeatPoint").ToArray();
    bool OwnColliderOverlap(Vector3 lower, Vector3 upper, float radius, Collider ignore = null) =>
        Physics.OverlapCapsule(lower, upper, radius, ~0, QueryTriggerInteraction.Ignore).Any(c => c.transform.IsChildOf(car.transform) && c != ignore);

    [Test] public void PrefabHasEveryPartControlSeatAndWheel() {
        Assert.AreEqual(45, Parts.Length);
        Assert.AreEqual(42, car.GetComponentsInChildren<Component>(true).Count(c => c && c.GetType().Name == "CarControl"));
        Assert.AreEqual(4, Seats.Length);
        Assert.AreEqual(4, car.GetComponentsInChildren<WheelCollider>(true).Length);
        Assert.AreEqual(0, car.GetComponentsInChildren<MeshCollider>(true).Length, "box colliders only");
        Assert.IsTrue(car.GetComponentsInChildren<WheelCollider>(true).All(w => w.gameObject.layer == 2), "wheels stay off interaction rays");
    }

    [TestCase("Door_FL", -1)][TestCase("Door_FR", 1)][TestCase("Door_RL", -1)][TestCase("Door_RR", 1)]
    public void DoorsSwingOutward(string id, int side) {
        var door = Part(id);
        var leaves = (BoxCollider[])Field(door, "leaves");
        Assert.GreaterOrEqual(leaves.Length, 3);
        Snap(door, 0); Physics.SyncTransforms();
        float closed = leaves.Max(l => Mathf.Abs(car.transform.InverseTransformPoint(l.bounds.center).x));
        Snap(door, 1); Physics.SyncTransforms();
        float open = leaves.Max(l => Mathf.Abs(car.transform.InverseTransformPoint(l.bounds.center).x));
        Assert.Greater(open, closed + .25f, "door must swing out, not into the cabin");
        Assert.IsTrue(leaves.All(l => Mathf.Sign(car.transform.InverseTransformPoint(l.bounds.center).x) == side));
    }

    [Test] public void ExitPointsClearOfOpenDoorsAndBody() {
        foreach (var door in Parts.Where(p => ((string)Field(p, "partId")).StartsWith("Door_"))) Snap(door, 1);
        Physics.SyncTransforms();
        foreach (var seat in Seats) {
            foreach (var name in new[] { "exitNear", "exitFar" }) {
                var feet = car.transform.TransformPoint((Vector3)Field(seat, name)) + Vector3.up * .03f;
                var hits = Physics.OverlapCapsule(feet + Vector3.up * .29f, feet + Vector3.up * 1.53f, .28f, ~0, QueryTriggerInteraction.Ignore)
                    .Where(c => c.transform.IsChildOf(car.transform)).Select(c => c.name + "@" + car.transform.InverseTransformPoint(c.bounds.center)).ToArray();
                Assert.IsEmpty(hits, Field(seat, "seatId") + " " + name + " overlaps the car or an open door");
            }
        }
    }

    [Test] public void SeatedEyesAreInsideTheCabin() {
        foreach (var seat in Seats) {
            var eye = ((Component)seat).transform.TransformPoint((Vector3)Field(seat, "eyeLocal"));
            var local = car.transform.InverseTransformPoint(eye);
            Assert.That(local.y, Is.InRange(1.35f, 1.62f), "eye height");
            Assert.That(Mathf.Abs(local.x), Is.LessThan(.55f));
            Assert.IsFalse(Physics.CheckSphere(eye, .09f, ~0, QueryTriggerInteraction.Ignore), "eye inside a collider for " + Field(seat, "seatId"));
        }
    }

    [Test] public void WheelVisualsSitOnTheirColliders() {
        var hotel = car.GetComponents<Component>().Single(c => c.GetType().Name == "HotelCar");
        var wheels = (WheelCollider[])Field(hotel, "wheels");
        var visuals = (Transform[])Field(hotel, "wheelVisuals");
        for (int i = 0; i < 4; i++) {
            var colliderCentre = car.transform.InverseTransformPoint(wheels[i].transform.position);
            var visual = car.transform.InverseTransformPoint(visuals[i].position);
            Assert.AreEqual(colliderCentre.x, visual.x, .001f);
            Assert.AreEqual(colliderCentre.z, visual.z, .001f);
            Assert.AreEqual(wheels[i].suspensionDistance * wheels[i].suspensionSpring.targetPosition, colliderCentre.y - visual.y, .005f, "rest position must land on the authored hub");
            Assert.AreEqual(.37f, wheels[i].radius, .001f);
        }
    }

    [Test] public void WindowGlassDropsInsideTheDoor() {
        var glass = Part("Glass_FL");
        var box = ((BoxCollider[])Field(glass, "leaves"))[0];
        Snap(glass, 0); Physics.SyncTransforms(); float up = car.transform.InverseTransformPoint(box.bounds.max).y;
        Snap(glass, 1); Physics.SyncTransforms(); float down = car.transform.InverseTransformPoint(box.bounds.max).y;
        Assert.Greater(up, 1.6f); Assert.Less(down, 1.23f, "lowered glass must sit below the belt line");
    }

    [Test] public void TrunkLidAndHoodSidesLiftClear() {
        var lid = Part("TrunkLid"); var lidBox = ((BoxCollider[])Field(lid, "leaves"))[0];
        Snap(lid, 1); Physics.SyncTransforms();
        Assert.Greater(car.transform.InverseTransformPoint(lidBox.bounds.center).y, 1.35f, "open lid stands up");
        foreach (var id in new[] { "Hood_L", "Hood_R" }) {
            var hood = Part(id); var box = ((BoxCollider[])Field(hood, "leaves"))[0];
            Snap(hood, 1); Physics.SyncTransforms(); var open = car.transform.InverseTransformPoint(box.bounds.center);
            Assert.Greater(open.y, 1.12f, id + " must fold up on top of the hood");
            Assert.Less(Mathf.Abs(open.x), .43f, id + " must lie over the hood, leaving the side of the engine bay open");
        }
    }
    // Interaction targets must not overlap, closed or with every part opened (doors, visor, lid...).
    [TestCase(0f)][TestCase(1f)]
    public void ControlTargetsDoNotOverlap(float pose) {
        foreach (var part in Parts) Snap(part, pose);
        Physics.SyncTransforms();
        var targets = car.GetComponentsInChildren<BoxCollider>(true).Where(b => b.isTrigger && b.GetComponent<Component>() && b.GetComponents<Component>().Any(c => c && c.GetType().Name == "CarControl")).ToArray();
        Assert.Greater(targets.Length, 30);
        var clashes = new System.Collections.Generic.List<string>();
        for (int i = 0; i < targets.Length; i++)
            for (int j = i + 1; j < targets.Length; j++)
                if (Physics.ComputePenetration(targets[i], targets[i].transform.position, targets[i].transform.rotation, targets[j], targets[j].transform.position, targets[j].transform.rotation, out _, out float depth) && depth > .002f)
                    clashes.Add(targets[i].name + " x " + targets[j].name + " " + depth.ToString("0.000"));
        Assert.IsEmpty(clashes);
    }
    // The driver's ordinary 3m eye ray must reach every in-car control first, at each light-switch step.
    [TestCase(0f)][TestCase(.5f)][TestCase(1f)]
    public void DriverEyeReachesEveryControl(float lightStep) {
        Snap(Part("LightSwitch"), lightStep); Physics.SyncTransforms();
        var driver = Seats.Single(s => (string)Field(s, "seatId") == "driver");
        var eye = ((Component)driver).transform.TransformPoint((Vector3)Field(driver, "eyeLocal"));
        var failures = new System.Collections.Generic.List<string>();
        foreach (var id in new[] { "ignition", "starter", "choke", "lightSwitch", "horn", "SparkLever", "HandThrottle", "gear", "handbrake", "wiperSwitch", "windshieldCrank", "visor", "dome", "gauges", "window-FL" }) {
            var control = car.GetComponentsInChildren<Component>(true).Single(c => c && c.GetType().Name == "CarControl" && (string)Field(c, "controlId") == id);
            var target = control.GetComponent<Collider>().bounds.center;
            if (!Physics.Raycast(eye, (target - eye).normalized, out var hit, 3f)) { failures.Add(id + ": no hit"); continue; }
            if (hit.collider.gameObject != control.gameObject) failures.Add(id + ": hit " + hit.collider.name + " at " + car.transform.InverseTransformPoint(hit.point) + " target " + car.transform.InverseTransformPoint(target));
        }
        Assert.IsEmpty(failures);
    }
    // From each standing spot used beside the car, a 1.66m eye ray reaches every outside control first.
    [TestCase(-1, 0f)]
    [TestCase(-1, 0.5f)]
    [TestCase(-1, 1f)]
    [TestCase(0, 0f)]
    [TestCase(0, 0.5f)]
    [TestCase(0, 1f)]
    [TestCase(1, 0f)]
    [TestCase(1, 0.5f)]
    [TestCase(1, 1f)]
    [TestCase(2, 0f)]
    [TestCase(2, 0.5f)]
    [TestCase(2, 1f)]
    [TestCase(3, 0f)]
    [TestCase(3, 0.5f)]
    [TestCase(3, 1f)]
    public void DriverCanReachBrakeAndGearThroughoutTheirTravel(int gear, float brake) {
        var lever = Part("GearLever");
        var pose = gear switch { 1 => new Vector3(12,0,9), 2 => new Vector3(-12,0,9), 3 => new Vector3(12,0,-9), -1 => new Vector3(-12,0,-9), _ => Vector3.zero };
        lever.GetType().GetMethod("PoseEuler").Invoke(lever, new object[] { pose });Snap(Part("Handbrake"), brake);
        var driver = Seats.Single(s => (string)Field(s, "seatId") == "driver");
        var body = new GameObject("Seated control ray test");body.transform.SetParent(driver.transform, false);
        try {
            body.AddComponent<CharacterController>().enabled = false;
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("PlayerMotor")).First(t => t != null);
            var motor = body.AddComponent(type);
            var cameraObject = new GameObject("Seated test eye");cameraObject.transform.SetParent(body.transform, false);
            var eye = cameraObject.AddComponent<Camera>();eye.enabled = false;eye.transform.localPosition = (Vector3)Field(driver,"eyeLocal");
            type.GetField("eye").SetValue(motor,eye);Physics.SyncTransforms();
            foreach(var id in new[]{"handbrake","gear"}) {
                var control = car.GetComponentsInChildren<Component>(true).Single(c => c && c.GetType().Name == "CarControl" && (string)Field(c,"controlId") == id);
                type.GetMethod("Aim").Invoke(motor,new object[]{control.GetComponent<Collider>().bounds.center});Physics.SyncTransforms();
                Assert.IsTrue(Physics.Raycast(eye.transform.position,eye.transform.forward,out var hit,3f), id+" missed");
                Assert.AreEqual(control.gameObject,hit.collider.gameObject,id+" obstructed in gear "+gear+" at brake "+brake);
            }
        } finally { UnityEngine.Object.DestroyImmediate(body); }
    }
    [TestCase(1.28f, 1.3f, "Hood_R", "engine")]
    [TestCase(.5f, 2.6f, "", "radiatorCap")]
    [TestCase(-1.32f, 1.45f, "", "spare,fuelCap")]
    [TestCase(.35f, -2.75f, "", "strap-L,strap-R")]
    [TestCase(1.0f, -2.0f, "TrunkLid,Strap_L,Strap_R", "item-tools,item-tube,item-oil,item-tarp,crankStowed")]
    [TestCase(.45f, 2.65f, "", "crankHole")]
    public void StandingEyeReachesOutsideControls(float x, float z, string open, string ids) {
        foreach (var id in open.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) Snap(Part(id), 1);
        Physics.SyncTransforms();
        var eye = car.transform.TransformPoint(new Vector3(x, 1.66f, z));
        var failures = new System.Collections.Generic.List<string>();
        foreach (var id in ids.Split(',')) {
            var control = car.GetComponentsInChildren<Component>(true).Single(c => c && c.GetType().Name == "CarControl" && (string)Field(c, "controlId") == id);
            var target = control.GetComponent<Collider>().bounds.center;
            if (!Physics.Raycast(eye, (target - eye).normalized, out var hit, 3f)) { failures.Add(id + ": no hit"); continue; }
            if (hit.collider.gameObject != control.gameObject) failures.Add(id + ": hit " + hit.collider.name + " at " + car.transform.InverseTransformPoint(hit.point));
        }
        Assert.IsEmpty(failures);
    }
}
