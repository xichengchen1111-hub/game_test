using UnityEngine;

// Runtime anchor of the shared D police-station prefab (B10). Reads the same layout
// contract as the generators, for room names and acoustic/door lookups. No timers.
public sealed class PoliceStation : MonoBehaviour {
    public static PoliceStation Current { get; private set; }
    public TextAsset layoutJson;
    PoliceLayout layout;
    public PoliceLayout Layout => layout ??= PoliceLayout.Parse(layoutJson.text);
    void OnEnable() { Current = this; }
    void OnDisable() { if (Current == this) Current = null; }
    public bool RoomAt(Vector3 world, out PoliceRoom room) {
        room = null;
        var p = transform.InverseTransformPoint(world);
        if (p.x < -.05f || p.x > 23.05f || p.z < -.05f || p.z > 24.05f || p.y < -1 || p.y > 8) return false;
        room = Layout.RoomAt(p);
        if (room != null) return true;
        // Court and loggias are part of the station grounds without a room name.
        if (p.z > 12 && p.x > 5.5f && p.x < 17.5f && p.y < 3) { room = new PoliceRoom { id = "court", name = "回廊内院" }; return true; }
        return false;
    }
    public ShopMechanism Door(string fixtureId) {
        foreach (var door in GetComponentsInChildren<ShopMechanism>()) if (door.fixtureId == fixtureId) return door;
        return null;
    }
}
