using UnityEngine;

// Runtime anchor of the shared E stone-barn garage prefab (B07). Reads the same layout contract
// as the generators for room names and door lookups. No timers.
public sealed class RepairGarage : MonoBehaviour {
    public static RepairGarage Current { get; private set; }
    public TextAsset layoutJson;
    GarageLayout layout;
    public GarageLayout Layout => layout ??= GarageLayout.Parse(layoutJson.text);
    void OnEnable() { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }
    public bool RoomAt(Vector3 world, out PoliceRoom room) {
        room = null;
        var p = transform.InverseTransformPoint(world);
        if (p.x < -.05f || p.x > 18.05f || p.z < -.8f || p.z > 30.05f || p.y < -1 || p.y > 9) return false;
        if (p.z < 19.05f) {
            room = Layout.RoomAt(p);
            if (room != null) return true;
        }
        // The fenced rear yard and the side yard behind the office belong to the garage grounds.
        if (p.z > 11.9f && p.y < 3.2f) { room = new PoliceRoom { id = "yard", name = "后院" }; return true; }
        return false;
    }
    public ShopMechanism Door(string fixtureId) {
        foreach (var door in GetComponentsInChildren<ShopMechanism>()) if (door.fixtureId == fixtureId) return door;
        return null;
    }
}
