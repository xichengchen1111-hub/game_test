using System;
using System.Linq;
using UnityEngine;

// Shared E stone-barn garage layout contract (tools/garage/garage_layout.py -> Resources/GarageLayout.json).
// Building-local metres: +X east, +Y up, +Z north; the prefab root has no rotation. Walls, openings,
// rooms, slabs, stairs, furniture, lamps, items and shelters reuse the police contract's record types
// (same fields), so the shared opening column cut (PoliceLayout.WallPieces) cuts both buildings.
[Serializable] public class GarageLevels { public float ground, eaves, ridge, ridgeX, slope, roofThickness, officeCeiling, officeRoofHigh, officeRoofLow, loft, loftUnder, yard, foot; }
[Serializable] public class GarageBlock { public string id; public float x0, y0, z0, x1, y1, z1; }
[Serializable] public class GaragePost { public string id; public float x, z, w, y0, y1; }
[Serializable] public class GarageExtent { public float x0, x1, z0, z1; }
[Serializable] public class GarageGableWindow { public string id, wall; public float at, width, bottom, top, rise; }

[Serializable] public class GarageLayout {
    public int version;
    public string id, name, scheme;
    public float[] origin;
    public GarageLevels levels;
    public GarageExtent hall, office;
    public PoliceWall[] walls;
    public PoliceOpening[] openings;
    public GarageGableWindow gableWindow;
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
    public static GarageLayout Parse(string json) {
        var layout = JsonUtility.FromJson<GarageLayout>(json);
        if (layout == null || layout.walls == null || layout.openings == null || layout.rooms == null || layout.levels == null)
            throw new InvalidOperationException("Garage layout contract is incomplete");
        return layout;
    }
    public static GarageLayout Load() {
        var asset = Resources.Load<TextAsset>("GarageLayout");
        if (!asset) throw new InvalidOperationException("Missing Resources/GarageLayout.json");
        return Parse(asset.text);
    }
    public PoliceWall Wall(string id) => walls.Single(w => w.id == id);
    public PoliceOpening Opening(string id) => openings.Single(o => o.id == id);
    public PoliceLamp Lamp(string id) => lamps.Single(l => l.id == id);
    public PoliceItem Item(string id) => items.Single(i => i.id == id);
    // The loft (level 1) shares its plan with the tool store below; anything else is level 0.
    public PoliceRoom RoomAt(Vector3 local) {
        if (local.y > levels.loft - .3f)
            foreach (var r in rooms)
                if (r.level == 1 && local.x >= r.x0 - .1f && local.x <= r.x1 + .1f && local.z >= r.z0 - .1f && local.z <= r.z1 + .1f) return r;
        foreach (var r in rooms)
            if (r.level == 0 && local.x >= r.x0 - .1f && local.x <= r.x1 + .1f && local.z >= r.z0 - .1f && local.z <= r.z1 + .1f) return r;
        return null;
    }
}
