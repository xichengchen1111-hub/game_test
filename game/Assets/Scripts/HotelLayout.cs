using System;
using System.Linq;
using UnityEngine;

// Shared A gabled old inn layout contract (tools/hotel/hotel_layout.py -> Resources/HotelLayout.json).
// Building-local metres: +X east, +Y up, +Z north; the prefab root has no rotation. Walls, openings,
// rooms, slabs, stairs, furniture, lamps, items and shelters reuse the police contract's record types
// (same fields), so the shared opening column cut (PoliceLayout.WallPieces) cuts this building too.
[Serializable] public class HotelLevels { public float ground, groundCeiling, upper, upperCeiling, eaves, ridge, ridgeZ, slope, roofThickness, yard, foot, serviceEaves, serviceRidge, serviceRidgeZ, serviceCeiling, streetWall; }
[Serializable] public class HotelExtent { public float x0, x1, z0, z1; }
[Serializable] public class HotelCarStand { public float x, z, yaw, halfW, back, front; }

[Serializable] public class HotelLayout {
    public int version;
    public string id, name, scheme;
    public float[] origin;
    public HotelLevels levels;
    public HotelExtent front, service, stairWell;
    public HotelCarStand car;
    public PoliceWall[] walls;
    public PoliceOpening[] openings;
    public PoliceRoom[] rooms;
    public PoliceSlab[] slabs;
    public PoliceStep[] stairs;
    public GarageBlock[] blocks;
    public GaragePost[] posts;
    public PoliceFurniture[] furniture;
    public PoliceLamp[] lamps;
    public PoliceItem[] items;
    public PoliceShelter[] shelters;

    public Vector3 Origin => new Vector3(origin[0], origin[1], origin[2]);
    public static HotelLayout Parse(string json) {
        var layout = JsonUtility.FromJson<HotelLayout>(json);
        if (layout == null || layout.walls == null || layout.openings == null || layout.rooms == null || layout.levels == null)
            throw new InvalidOperationException("Hotel layout contract is incomplete");
        return layout;
    }
    public static HotelLayout Load() {
        var asset = Resources.Load<TextAsset>("HotelLayout");
        if (!asset) throw new InvalidOperationException("Missing Resources/HotelLayout.json");
        return Parse(asset.text);
    }
    public PoliceWall Wall(string id) => walls.Single(w => w.id == id);
    public PoliceOpening Opening(string id) => openings.Single(o => o.id == id);
    public PoliceLamp Lamp(string id) => lamps.Single(l => l.id == id);
    public PoliceItem Item(string id) => items.Single(i => i.id == id);
    // The upper floor (level 1) lies over the front range; anything lower is level 0.
    public PoliceRoom RoomAt(Vector3 local) {
        int level = local.y > levels.upper - .3f ? 1 : 0;
        foreach (var r in rooms)
            if (r.level == level && local.x >= r.x0 - .1f && local.x <= r.x1 + .1f && local.z >= r.z0 - .1f && local.z <= r.z1 + .1f) return r;
        return null;
    }
}
