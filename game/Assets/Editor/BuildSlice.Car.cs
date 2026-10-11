using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The hotel car prefab is assembled from the editable Blender FBX and the layout
// contract written by tools/car/build_hotel_car.py: one source for pivots,
// colliders, seats, exits, wheels and every interaction point.
public static partial class BuildSlice {
    const string CarModelPath = "Assets/Art/Car/HotelCar.fbx";
    const string CarLayoutPath = "Assets/Resources/HotelCarLayout.json";
    public const string CarPrefabPath = "Assets/Generated/HotelCar.prefab";
    // Hotel B08 front carriage court, beside the guest path, facing the gate onto Station Road.
    public static readonly Vector3 HotelCarParking = new Vector3(169.5f, .02f, 2f);
    public const float HotelCarParkingYaw = 0;
    // The suspension rests at targetPosition (.5) of its .18m travel, so the hub sits .09m below the collider centre.
    const float WheelSuspensionLift = .09f;

    [Serializable] public class CarPartDef { public string id, parent, kind, fixture; public float[] pivot, axis, offset; public float angle, speed; }
    [Serializable] public class CarBoxDef { public string part; public float[] center, size; }
    [Serializable] public class CarControlDef { public string id, kind, part, label; public float[] center, size; }
    [Serializable] public class CarSeatDef { public string id, label, row; public int side; public bool driver; public float[] sit, eye, exitNear, exitFar; public string[] doors; }
    [Serializable] public class CarWheelDef { public string id; public float[] center; public bool steer, drive; }
    [Serializable] public class CarShelterDef { public float[] centre, footprint; public float eaves, rise; }
    [Serializable] public class CarRainDef { public float[] centre, size; }
    [Serializable] public class CarLayoutDef {
        public CarPartDef[] parts; public CarControlDef[] controls; public CarBoxDef[] body, partColliders; public CarSeatDef[] seats; public CarWheelDef[] wheels;
        public float wheelRadius, mass; public float[] centreOfMass, headlampLeft, headlampRight, tailLamp, domeLamp, enginePoint;
        public CarShelterDef shelter; public CarRainDef windshieldRain;
    }
    public static CarLayoutDef LoadCarLayout() => JsonUtility.FromJson<CarLayoutDef>(File.ReadAllText(CarLayoutPath));
    static Vector3 V3(float[] a) => new Vector3(a[0], a[1], a[2]);

    static Material CarMaterial(string name) {
        name = name.Split('.')[0].Replace(' ', '_');
        if (name == "Car_Glass") {
            const string path = "Assets/Generated/Materials/CarGlass.mat";
            var glass = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!glass) { glass = new Material(Shader.Find("WrongDate/ClearGlass")); AssetDatabase.CreateAsset(glass, path); }
            glass.SetColor("_Tint", new Color(.62f, .72f, .78f, .07f)); glass.renderQueue = 3000; EditorUtility.SetDirty(glass); return glass;
        }
        (string hex, float metal, float smooth, float emission) spec = name switch {
            "Car_Paint" => ("#6B2C2C", 0, .62f, 0), "Car_Black" => ("#1E1C1A", 0, .5f, 0), "Car_Chrome" => ("#C9C6BE", .9f, .82f, 0),
            "Car_Tire" => ("#232220", 0, .12f, 0), "Car_Spoke" => ("#E8DCC0", .2f, .55f, 0), "Car_Stripe" => ("#D9C9A3", 0, .5f, 0),
            "Car_Leather" => ("#4A3328", 0, .38f, 0), "Car_Trim" => ("#6A5A48", 0, .2f, 0), "Car_Headliner" => ("#8A7A66", 0, .1f, 0),
            "Car_Dash" => ("#4A3B30", 0, .48f, 0), "Car_Floor" => ("#2A2826", 0, .1f, 0), "Car_Engine" => ("#3E463F", .2f, .45f, 0),
            "Car_Brass" => ("#B38A49", .8f, .62f, 0), "Car_Lamp" => ("#FFE2A8", 0, .8f, 1), "Car_TailLamp" => ("#A32D2D", 0, .75f, 1),
            "Car_DomeLamp" => ("#F3DDB2", 0, .7f, 1), "Car_Gauge" => ("#E8DCC0", 0, .6f, 1), "Car_Needle" => ("#B5482A", 0, .5f, 0),
            "Car_Canvas" => ("#6E6A55", 0, .05f, 0), "Car_Rubber" => ("#2A2826", 0, .08f, 0), "Car_Wood" => ("#7A5236", 0, .3f, 0),
            "Car_Tan" => ("#8A6A3E", 0, .3f, 0), "Car_Mirror" => ("#3F4A52", .9f, .95f, 0), "Car_InnerTube" => ("#7A3A30", 0, .15f, 0),
            _ => throw new Exception("Unmapped car material " + name)
        };
        var m = Mat(name.Replace("_", ""), spec.hex, spec.emission, spec.smooth);
        m.SetFloat("_Metallic", spec.metal);
        if (spec.emission > 0) m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(m);
        return m;
    }
    static CarPart.Kind CarPartKind(string kind) => kind switch {
        "hinge" => CarPart.Kind.Hinge, "slide" => CarPart.Kind.Slide, "screw" => CarPart.Kind.Screw, "spin" => CarPart.Kind.Spin,
        "steps" => CarPart.Kind.Steps, "gear" => CarPart.Kind.Gear, "gauge" => CarPart.Kind.Gauge, "sweep" => CarPart.Kind.Sweep,
        "wheel" => CarPart.Kind.Wheel, "static" => CarPart.Kind.Static, _ => throw new Exception("Unknown car part kind " + kind)
    };
    static CarControlKind CarControlKindFor(string kind) {
        if (!Enum.TryParse<CarControlKind>(kind, true, out var parsed)) {
            parsed = kind switch { "lights" => CarControlKind.Lights, "wiper" => CarControlKind.Wiper, _ => throw new Exception("Unknown car control " + kind) };
        }
        return parsed;
    }

    // Fast iteration entry: compile, import and regenerate only the car prefab.
    public static void CarPrefabOnly() {
        Directory.CreateDirectory("Assets/Generated/Materials"); AssetDatabase.Refresh();
        CarPrefab(); AssetDatabase.SaveAssets();
    }
    static void CarPrefab() {
        var importer = AssetImporter.GetAtPath(CarModelPath) as ModelImporter;
        if (!importer) throw new Exception("Missing editable car FBX " + CarModelPath);
        if (!importer.isReadable || importer.importAnimation || importer.materialImportMode != ModelImporterMaterialImportMode.ImportStandard || importer.importLights || importer.importCameras) {
            importer.isReadable = true; importer.importAnimation = false; importer.importLights = false; importer.importCameras = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; importer.SaveAndReimport();
        }
        var layout = LoadCarLayout();
        var root = new GameObject("旅馆用车 · 1931");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CarModelPath));
        PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        visual.name = "Visual"; visual.transform.SetParent(root.transform, false);
        var markers = visual.GetComponentsInChildren<Transform>(true);
        var front = markers.Single(t => t.name == "FrontMarker"); var east = markers.Single(t => t.name == "EastMarker");
        var direction = front.position - visual.transform.position; direction.y = 0;
        visual.transform.localRotation = Quaternion.AngleAxis(Vector3.SignedAngle(direction, Vector3.forward, Vector3.up), Vector3.up) * visual.transform.localRotation;
        if (Vector3.Distance(root.transform.InverseTransformPoint(front.position), new Vector3(0, 0, 3)) > .01f ||
            Vector3.Distance(root.transform.InverseTransformPoint(east.position), new Vector3(3, 0, 0)) > .01f)
            throw new Exception("Car import axis/scale mismatch: front=" + front.position + " east=" + east.position);

        // Rebuild every hinge as a clean car-aligned pivot (unit scale), moving the imported meshes under it.
        var partObjects = new Dictionary<string, CarPart>();
        foreach (var def in layout.parts) {
            var parent = string.IsNullOrEmpty(def.parent) || def.parent == "Body" ? root.transform : partObjects[def.parent].transform;
            var go = new GameObject(def.id);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(root.transform.TransformPoint(V3(def.pivot)), root.transform.rotation);
            var source = root.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == "Pivot_" + def.id);
            if (!source) throw new Exception("Car pivot missing in FBX: " + def.id);
            if (Vector3.Distance(source.position, go.transform.position) > .002f) throw new Exception("Car pivot moved on import: " + def.id);
            foreach (var child in source.Cast<Transform>().ToArray()) child.SetParent(go.transform, true);
            var part = go.AddComponent<CarPart>();
            part.partId = def.id; part.fixtureId = def.fixture; part.kind = CarPartKind(def.kind);
            part.axis = V3(def.axis); part.angle = def.angle; part.offset = V3(def.offset); part.speed = def.speed;
            partObjects[def.id] = part;
        }
        foreach (var leftover in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Pivot_")).ToArray())
            if (leftover.childCount == 0) UnityEngine.Object.DestroyImmediate(leftover.gameObject);

        var lampRenderers = new List<Renderer>(); var lampSlots = new List<int>(); var lampKinds = new List<int>();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) {
            var materials = renderer.sharedMaterials.Select(m => CarMaterial(m.name)).ToArray();
            renderer.sharedMaterials = materials;
            for (int i = 0; i < materials.Length; i++) {
                int kind = materials[i].name == "CarLamp" ? 0 : materials[i].name == "CarTailLamp" ? 1 : materials[i].name == "CarDomeLamp" ? 2 : materials[i].name == "CarGauge" ? 3 : -1;
                if (kind < 0) continue;
                lampRenderers.Add(renderer); lampSlots.Add(i); lampKinds.Add(kind);
            }
            if (materials.All(m => m.shader.name == "WrongDate/ClearGlass")) renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.gameObject.isStatic = false;
        }
        foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) UnityEngine.Object.DestroyImmediate(collider);

        // Simple box colliders only: body shell, then each moving leaf under its own pivot.
        var collision = Group("Body collision", Vector3.zero, root.transform).transform;
        int index = 0;
        foreach (var box in layout.body) {
            var g = Group("Body box " + index++, V3(box.center), collision);
            g.AddComponent<BoxCollider>().size = V3(box.size);
        }
        foreach (var box in layout.partColliders) {
            var part = partObjects[box.part];
            var g = new GameObject(box.part + " leaf");
            g.transform.SetParent(part.transform, false);
            g.transform.SetPositionAndRotation(root.transform.TransformPoint(V3(box.center)), root.transform.rotation);
            g.AddComponent<BoxCollider>().size = V3(box.size);
        }
        foreach (var part in partObjects.Values)
            part.leaves = part.GetComponentsInChildren<BoxCollider>(true).Where(c => c.GetComponentInParent<CarPart>() == part).ToArray();

        var car = root.AddComponent<HotelCar>();
        var body = root.AddComponent<Rigidbody>();
        body.mass = layout.mass; body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        car.body = body; car.mass = layout.mass; car.centreOfMass = V3(layout.centreOfMass);
        foreach (var def in layout.controls) {
            var kind = CarControlKindFor(def.kind);
            // Fixed scenery (trunk contents) has no pivot; its controls ride on the body.
            var owner = partObjects.TryGetValue(def.part, out var movablePart) ? movablePart : null;
            CarControl control;
            if (kind == CarControlKind.Body) control = root.AddComponent<CarControl>();
            else if (kind == CarControlKind.Door || kind == CarControlKind.Hood || kind == CarControlKind.TrunkLid) control = owner.gameObject.AddComponent<CarControl>();
            else {
                var g = new GameObject("Control " + def.id);
                g.transform.SetParent(owner ? owner.transform : root.transform, false);
                g.transform.SetPositionAndRotation(root.transform.TransformPoint(V3(def.center)), root.transform.rotation);
                // Interaction target only: a trigger, so it never blocks exits, doors or the body's mass.
                var target = g.AddComponent<BoxCollider>(); target.size = V3(def.size); target.isTrigger = true;
                control = g.AddComponent<CarControl>();
            }
            control.car = car; control.controlId = def.id; control.kind = kind; control.title = def.label; control.source = "旅馆用车";
            control.part = kind == CarControlKind.Window ? partObjects["Glass_" + def.id.Substring("window-".Length)] : owner;
        }
        var seats = new List<CarSeatPoint>();
        foreach (var def in layout.seats) {
            var g = Group("Seat " + def.id, V3(def.sit), root.transform);
            var seat = g.AddComponent<CarSeatPoint>();
            seat.seatId = def.id; seat.label = def.label; seat.driver = def.driver; seat.row = def.row; seat.side = def.side;
            seat.eyeLocal = V3(def.eye); seat.exitNear = V3(def.exitNear); seat.exitFar = V3(def.exitFar);
            seat.nearDoor = partObjects[def.doors[0]]; seat.farDoor = partObjects[def.doors[1]];
            seats.Add(seat);
        }
        foreach (var control in root.GetComponentsInChildren<CarControl>(true))
            if (control.kind == CarControlKind.Seat) control.seat = seats.Single(s => "seat-" + s.seatId == control.controlId);

        string[] order = { "FL", "FR", "RL", "RR" };
        for (int i = 0; i < 4; i++) {
            var def = layout.wheels.Single(w => w.id == order[i]);
            // Ignore Raycast keeps interaction rays off the wheels; physics still collides.
            var g = Group("WheelCollider " + def.id, V3(def.center) + Vector3.up * WheelSuspensionLift, root.transform); g.layer = 2;
            var wheel = g.AddComponent<WheelCollider>();
            wheel.radius = layout.wheelRadius; wheel.mass = 25; wheel.suspensionDistance = .18f; wheel.forceAppPointDistance = .12f; wheel.wheelDampingRate = .6f;
            wheel.suspensionSpring = new JointSpring { spring = 32000, damper = 3800, targetPosition = .5f };
            car.wheels[i] = wheel; car.wheelVisuals[i] = partObjects["Wheel_" + def.id].transform;
        }
        var lamps = new List<Light>();
        foreach (var point in new[] { layout.headlampLeft, layout.headlampRight }) {
            var g = Group("Headlamp beam", V3(point) + Vector3.forward * .06f, root.transform); g.transform.localRotation = Quaternion.Euler(4, 0, 0);
            var light = g.AddComponent<Light>(); light.type = LightType.Spot; light.range = 34; light.spotAngle = 58; light.innerSpotAngle = 24;
            light.intensity = 24; light.color = C("#FFE2B0"); light.shadows = LightShadows.None; light.enabled = false; lamps.Add(light);
        }
        var dome = Group("Dome lamp", V3(layout.domeLamp), root.transform).AddComponent<Light>();
        dome.type = LightType.Point; dome.range = 2.4f; dome.intensity = .9f; dome.color = C("#FFDDAE"); dome.shadows = LightShadows.None; dome.enabled = false;
        car.headlamps = lamps.ToArray(); car.domeLight = dome;
        car.lampRenderers = lampRenderers.ToArray(); car.lampSlots = lampSlots.ToArray(); car.lampKinds = lampKinds.ToArray();
        car.parts = partObjects.Values.ToArray(); car.seats = seats.ToArray();
        car.enginePoint = V3(layout.enginePoint);
        car.wiperPivot = V3(layout.parts.Single(p => p.id == "WiperArm").pivot);

        var shelterRoot = Group("Car rain shelter", Vector3.zero, root.transform);
        var s = layout.shelter;
        WeatherAuthoring.Shelter(shelterRoot, new Vector2(s.footprint[0], s.footprint[1]), s.eaves, s.rise, new Vector2(s.centre[0], s.centre[1]));
        car.shelter = shelterRoot.GetComponent<WeatherShelter>();

        const string rainPath = "Assets/Generated/Materials/CarWindshieldRain.mat";
        var rain = AssetDatabase.LoadAssetAtPath<Material>(rainPath);
        if (!rain) { rain = new Material(Shader.Find("WrongDate/WindshieldRain")); AssetDatabase.CreateAsset(rain, rainPath); }
        rain.shader = Shader.Find("WrongDate/WindshieldRain"); EditorUtility.SetDirty(rain);
        var rainCentre = V3(layout.windshieldRain.centre);
        var quad = Quad("Windscreen rain", Vector3.zero, new Vector2(layout.windshieldRain.size[0], layout.windshieldRain.size[1]), rain, partObjects["Windshield"].transform);
        quad.transform.SetPositionAndRotation(root.transform.TransformPoint(rainCentre), root.transform.rotation);
        var quadRenderer = quad.GetComponent<MeshRenderer>(); quadRenderer.shadowCastingMode = ShadowCastingMode.Off; quadRenderer.receiveShadows = false;
        car.windshieldRain = quadRenderer; car.windshieldPoint = rainCentre; car.rainQuadCentre = rainCentre;
        car.rainQuadSize = new Vector2(layout.windshieldRain.size[0], layout.windshieldRain.size[1]);
        car.sounds = root.AddComponent<CarSounds>();

        CarChecks(root, layout, car);
        Directory.CreateDirectory("Assets/Generated");
        PrefabUtility.SaveAsPrefabAsset(root, CarPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("HOTEL_CAR_PREFAB_PASS: parts=" + layout.parts.Length + " controls=" + layout.controls.Length + " seats=" + layout.seats.Length);
    }

    static void CarChecks(GameObject root, CarLayoutDef layout, HotelCar car) {
        var parts = root.GetComponentsInChildren<CarPart>(true);
        if (parts.Length != layout.parts.Length) throw new Exception("Car part count mismatch");
        if (root.GetComponentsInChildren<CarControl>(true).Length != layout.controls.Length) throw new Exception("Car control count mismatch");
        if (car.wheels.Any(w => !w) || car.wheelVisuals.Any(w => !w)) throw new Exception("Car wheels incomplete");
        for (int i = 0; i < 4; i++) {
            var visual = car.wheelVisuals[i].GetComponentsInChildren<Renderer>().Aggregate(new Bounds(car.wheelVisuals[i].position, Vector3.zero), (b, r) => { b.Encapsulate(r.bounds); return b; });
            if (Mathf.Abs(visual.size.y - layout.wheelRadius * 2) > .03f) throw new Exception("Wheel visual size mismatch " + visual.size);
            if (Vector3.Distance(visual.center, car.wheelVisuals[i].position) > .08f) throw new Exception("Wheel pivot not at hub " + i);
        }
        foreach (var part in parts) {
            if (part.kind == CarPart.Kind.Hinge && part.partId.StartsWith("Door_") && part.leaves.Length < 3) throw new Exception("Door leaf colliders missing " + part.partId);
            if (part.GetComponentsInChildren<Renderer>(true).Length == 0) throw new Exception("Car part without geometry " + part.partId);
        }
        if (root.GetComponentsInChildren<MeshCollider>(true).Length > 0) throw new Exception("Car must use box colliders only");
    }

    static void PlaceHotelCar() {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarPrefabPath);
        if (!prefab) throw new Exception("Hotel car prefab missing");
        var car = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        car.transform.SetPositionAndRotation(HotelCarParking, Quaternion.Euler(0, HotelCarParkingYaw, 0));
        var hotel = car.GetComponent<HotelCar>(); hotel.parkingPosition = HotelCarParking; hotel.parkingYaw = HotelCarParkingYaw;
        // A dynamic vehicle: not part of Town geometry, the navigation bake or window copies.
        Debug.Log("HOTEL_CAR_PLACED: B08 front court " + HotelCarParking);
    }
}
