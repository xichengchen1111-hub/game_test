using UnityEngine;

// Runtime anchor of the shared Qingchuan hospital prefab (B11). Reads the same layout
// contract as the generators (tools/hospital/hospital_layout.py -> Resources/HospitalLayout.json),
// which uses the police contract's schema, so the shared PoliceLayout classes parse it.
public sealed class HospitalBuilding : MonoBehaviour {
    public static HospitalBuilding Current { get; private set; }
    public TextAsset layoutJson;
    PoliceLayout layout;
    public PoliceLayout Layout => layout ??= PoliceLayout.Parse(layoutJson.text);
    void OnEnable() { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }
    // A standing point on the floor, from building-local metres.
    public Vector3 Point(float x, float z) => transform.TransformPoint(new Vector3(x, Layout.levels.ground + .025f, z));
    public bool RoomAt(Vector3 world, out PoliceRoom room) {
        room = null;
        var p = transform.InverseTransformPoint(world);
        if (p.x < -.05f || p.x > 42.05f || p.z < -.05f || p.z > 30.05f || p.y < -1 || p.y > 5) return false;
        room = Layout.RoomAt(p);
        if (room != null) return true;
        // The court between the wards belongs to the hospital grounds without a room name.
        if (p.z > 12 && p.x > 8 && p.x < 34) { room = new PoliceRoom { id = "court", name = "医院庭院" }; return true; }
        return false;
    }
    public ShopMechanism Door(string fixtureId) {
        foreach (var door in GetComponentsInChildren<ShopMechanism>()) if (door.fixtureId == fixtureId) return door;
        return null;
    }
    public PoliceItem Spot(string id) => Layout.Item(id);
}
