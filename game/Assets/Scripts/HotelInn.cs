using UnityEngine;

// Runtime anchor of the shared A gabled old inn prefab (B08). Reads the same layout contract as the
// generators for room names and door lookups. No timers.
public sealed class HotelInn : MonoBehaviour {
    public static HotelInn Current { get; private set; }
    public TextAsset layoutJson;
    HotelLayout layout;
    public HotelLayout Layout => layout ??= HotelLayout.Parse(layoutJson.text);
    void OnEnable() { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }
    public Vector3 Point(float x, float y, float z) => transform.TransformPoint(new Vector3(x, y, z));
    public bool RoomAt(Vector3 world, out PoliceRoom room) {
        room = null;
        var p = transform.InverseTransformPoint(world);
        if (p.x < -.05f || p.x > 41.05f || p.z < -.05f || p.z > 40.05f || p.y < -1 || p.y > 12) return false;
        room = Layout.RoomAt(p);
        if (room != null) return true;
        var f = Layout.front;
        if (p.y > 3) return false;
        if (p.z > f.z1) { room = new PoliceRoom { id = "forecourt", name = "前庭车马院" }; return true; }
        if (p.x > f.x1 && p.z > Layout.service.z1) { room = new PoliceRoom { id = "carport", name = "车棚" }; return true; }
        room = new PoliceRoom { id = "yard", name = "后院" }; return true;
    }
    public ShopMechanism Door(string fixtureId) {
        foreach (var door in GetComponentsInChildren<ShopMechanism>()) if (door.fixtureId == fixtureId) return door;
        return null;
    }
    public RakingLamp RegisterLamp => GetComponentInChildren<RakingLamp>(true);
}
