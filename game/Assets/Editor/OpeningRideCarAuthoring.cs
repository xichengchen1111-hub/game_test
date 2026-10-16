using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Keep the playable hotel car as the single source of car geometry and pivots.
// Strip its stateful controls before this separate story resource enters play.
public static class OpeningRideCarAuthoring {
    public const string PrefabPath = "Assets/Resources/OpeningRideCar.prefab";
    public static void BuildPrefab() {
        var root = PrefabUtility.LoadPrefabContents(BuildSlice.CarPrefabPath);
        try {
            root.name = "律师的汽车";
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var original = root.GetComponent<HotelCar>();
            if (!original) throw new Exception("Opening car requires the existing hotel car prefab.");
            var car = root.AddComponent<OpeningRideCar>();
            car.parts = original.parts;
            car.passengerDoor = original.parts.Single(p => p.partId == "Door_FR");
            var passenger = original.seats.Single(s => s.seatId == "passenger");
            car.passengerSeat = passenger.transform; car.passengerEyeLocal = passenger.eyeLocal;
            car.sounds = original.sounds;
            car.shelter = original.shelter; car.shelter.vehicle = null;
            car.headlamps = original.headlamps; car.domeLight = original.domeLight;
            car.lampRenderers = original.lampRenderers; car.lampSlots = original.lampSlots; car.lampKinds = original.lampKinds;
            if (original.windshieldRain) UnityEngine.Object.DestroyImmediate(original.windshieldRain.gameObject);
            foreach (var part in car.parts) { part.fixtureId = ""; part.leaves = new BoxCollider[0]; }
            foreach (var interaction in root.GetComponentsInChildren<Interactable>(true)) UnityEngine.Object.DestroyImmediate(interaction);
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            foreach (var seat in root.GetComponentsInChildren<CarSeatPoint>(true)) UnityEngine.Object.DestroyImmediate(seat);
            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Rigidbody>());
            car.solid = root.AddComponent<BoxCollider>();
            car.solid.center = new Vector3(0, .88f, -.17f); car.solid.size = new Vector3(1.78f, 1.76f, 4.3f);
            car.driver = AddDriver(root.transform);
            car.SetDriverVisible(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("OPENING_RIDE_CAR_PREFAB_PASS: existing car meshes, independent state, seated driver and passenger door");
        } finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static Transform AddDriver(Transform parent) {
        bool newCast=PeriodCharacterAsset.AllPrepared;
        var actor = newCast?PeriodCharacterAsset.Create(parent, Vector3.zero, "Lawyer", "律师"):StationClerkAsset.Create(parent,Vector3.zero);
        actor.name = "律师 · 驾驶坐姿";
        foreach (var collider in actor.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
        foreach (var interaction in actor.GetComponentsInChildren<Interactable>(true)) UnityEngine.Object.DestroyImmediate(interaction);
        foreach (var performance in actor.GetComponentsInChildren<StationClerkPerformance>(true)) UnityEngine.Object.DestroyImmediate(performance);
        // The posed driver still speaks (mouth only) and turns his head to the player at the hospital.
        foreach (var performance in actor.GetComponentsInChildren<PeriodCharacterPerformance>(true)) { performance.gestures = false; performance.speaker = "律师"; }
        foreach (var animator in actor.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        if (!actor.GetComponent<SeatedLook>()) actor.AddComponent<SeatedLook>();
        if(!newCast)actor.transform.localScale=Vector3.one*.86f;
        actor.transform.localRotation = Quaternion.Euler(0, 180, 0); // authored front is -Z
        var bones = actor.GetComponentsInChildren<Transform>(true);
        Transform Bone(string name) => bones.Single(t => t.name == name);
        Vector3 Point(float x, float y, float z) => parent.TransformPoint(new Vector3(x, y, z));
        var hips = Bone("Hips");
        actor.transform.position += Point(-.30f, .82f, -.52f) - hips.position;
        // Two-bone reach preserves every limb's length instead of scaling limbs
        // or leaving the standing model's feet through the floor of the car.
        PoseLimb(Bone("Left_UpperLeg"), Bone("Left_LowerLeg"), Bone("Left_Foot"), Point(-.43f, .47f, .32f), Point(-.43f, .78f, .12f));
        PoseLimb(Bone("Right_UpperLeg"), Bone("Right_LowerLeg"), Bone("Right_Foot"), Point(-.18f, .47f, .32f), Point(-.18f, .78f, .12f));
        PoseLimb(Bone("Left_UpperArm"), Bone("Left_LowerArm"), Bone("Left_Hand"), Point(-.48f, 1.10f, -.085f), Point(-.58f, 1.06f, -.43f));
        PoseLimb(Bone("Right_UpperArm"), Bone("Right_LowerArm"), Bone("Right_Hand"), Point(-.12f, 1.10f, -.085f), Point(-.04f, 1.06f, -.43f));
        foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true)) renderer.renderingLayerMask = 1;
        return actor.transform;
    }
    static void PoseLimb(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 hint) {
        float a = Vector3.Distance(upper.position, lower.position), b = Vector3.Distance(lower.position, end.position);
        var forward = (target - upper.position).normalized;
        float distance = Mathf.Clamp(Vector3.Distance(upper.position, target), Mathf.Abs(a - b) + .001f, a + b - .001f);
        var bend = Vector3.ProjectOnPlane(hint - upper.position, forward).normalized;
        if (bend.sqrMagnitude < .01f) bend = Vector3.ProjectOnPlane(Vector3.up, forward).normalized;
        float along = (a * a + distance * distance - b * b) / (2 * distance);
        var joint = upper.position + forward * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        upper.rotation = Quaternion.FromToRotation(lower.position - upper.position, joint - upper.position) * upper.rotation;
        lower.rotation = Quaternion.FromToRotation(end.position - lower.position, target - lower.position) * lower.rotation;
    }
}
