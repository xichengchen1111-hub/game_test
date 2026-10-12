using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shared D police-station layout contract (tools/police/police_layout.py -> Resources/PoliceLayout.json).
// Building-local metres: +X east, +Y up, +Z north; the prefab root has no rotation.
[Serializable] public class PoliceLevels { public float ground, groundCeiling, upper, upperCeiling, frontEaves, frontRidge, wingCeiling, wingEaves, wingRidge, court; }
[Serializable] public class PoliceWall { public string id, axis, pos, neg, level; public float c, a0, a1, t, y0, y1; }
[Serializable] public class PoliceOpening {
    public string id, wall, kind, style, hinge, fixture, label, enclosure, normal;
    public float at, width, bottom, top, x, z;
    public int leaves, swing;
    public bool exterior, startsOpen;
}
[Serializable] public class PoliceRoom { public string id, name, floor, enclosure; public int level; public float x0, z0, x1, z1, access; public bool @public; }
[Serializable] public class PoliceSlab { public string id, top, bottom, kind; public float x0, z0, x1, z1, y0, y1; }
[Serializable] public class PoliceStep { public string id; public float x0, x1, z0, z1, top; }
[Serializable] public class PoliceColumn { public string id; public float x, z, w, y0, y1; }
[Serializable] public class PoliceFurniture { public string id, kind, room, extra; public float x, y, z, yaw, w, d, h; public bool collide; }
[Serializable] public class PoliceLamp { public string id, kind, room, role, switchId, shade; public float x, y, z, intensity, range, access; }
[Serializable] public class PoliceItem {
    public string id, kind, doc, evidence, title, target;
    public float x, y, z, w, h, d, yaw, sitX, sitZ, sitY, standX, standZ, eye;
}
[Serializable] public class PoliceShelter { public string id; public float x0, z0, x1, z1, eaves, rise; public bool ridgeAlongX, open; public string[] doors; }

[Serializable] public class PoliceLayout {
    public int version;
    public string id, name, scheme;
    public float[] origin;
    public PoliceLevels levels;
    public PoliceWall[] walls;
    public PoliceOpening[] openings;
    public PoliceRoom[] rooms;
    public PoliceSlab[] slabs;
    public PoliceStep[] stairs;
    public PoliceColumn[] columns;
    public PoliceFurniture[] furniture;
    public PoliceLamp[] lamps;
    public PoliceItem[] items;
    public PoliceShelter[] shelters;

    public Vector3 Origin => new Vector3(origin[0], origin[1], origin[2]);
    public static PoliceLayout Parse(string json) {
        var layout = JsonUtility.FromJson<PoliceLayout>(json);
        if (layout == null || layout.walls == null || layout.openings == null || layout.rooms == null)
            throw new InvalidOperationException("Police layout contract is incomplete");
        return layout;
    }
    public static PoliceLayout Load() {
        var asset = Resources.Load<TextAsset>("PoliceLayout");
        if (!asset) throw new InvalidOperationException("Missing Resources/PoliceLayout.json");
        return Parse(asset.text);
    }
    public PoliceWall Wall(string id) => walls.Single(w => w.id == id);
    public PoliceOpening Opening(string id) => openings.Single(o => o.id == id);
    public PoliceLamp Lamp(string id) => lamps.Single(l => l.id == id);
    public PoliceItem Item(string id) => items.Single(i => i.id == id);

    // Same column cut as tools/police/build_police_station.py wall_pieces(): every
    // opening edge splits the wall; each column loses all openings covering it.
    public static List<Vector4> WallPieces(PoliceWall w, IEnumerable<PoliceOpening> own) {
        var openings = own.ToList();
        var xs = new SortedSet<float> { w.a0, w.a1 };
        foreach (var o in openings) { xs.Add(o.at - o.width / 2); xs.Add(o.at + o.width / 2); }
        var list = xs.ToList(); var pieces = new List<Vector4>();
        for (int i = 0; i + 1 < list.Count; i++) {
            float s0 = list[i], s1 = list[i + 1];
            if (s1 - s0 < .001f) continue;
            float mid = (s0 + s1) / 2, y = w.y0;
            foreach (var o in openings.Where(o => o.at - o.width / 2 < mid && mid < o.at + o.width / 2).OrderBy(o => o.bottom)) {
                if (o.bottom > y + .001f) pieces.Add(new Vector4(s0, s1, y, o.bottom));
                y = Mathf.Max(y, o.top);
            }
            if (w.y1 > y + .001f) pieces.Add(new Vector4(s0, s1, y, w.y1));
        }
        return pieces;
    }
    public static Bounds PieceBounds(PoliceWall w, Vector4 piece) {
        var centre = w.axis == "x" ? new Vector3((piece.x + piece.y) / 2, (piece.z + piece.w) / 2, w.c) : new Vector3(w.c, (piece.z + piece.w) / 2, (piece.x + piece.y) / 2);
        var size = w.axis == "x" ? new Vector3(piece.y - piece.x, piece.w - piece.z, w.t) : new Vector3(w.t, piece.w - piece.z, piece.y - piece.x);
        return new Bounds(centre, size);
    }
    public PoliceRoom RoomAt(Vector3 local) {
        int level = local.y > levels.upper - .4f ? 1 : 0;
        foreach (var r in rooms)
            if (r.level == level && local.x >= r.x0 - .1f && local.x <= r.x1 + .1f && local.z >= r.z0 - .1f && local.z <= r.z1 + .1f) return r;
        return null;
    }
}

[Serializable] public class PoliceLeafInfo { public string @object, opening, fixture, axis; public int index, count; public float[] hinge, leafCentre; public bool lowEnd; public float plane, width, height; }
[Serializable] public class PoliceDrawerInfo { public string @object, label; public int index; public float[] front, axis; public float travel; }
[Serializable] public class PoliceBuildManifest { public int version, layoutVersion, triangles; public PoliceLeafInfo[] leaves; public PoliceDrawerInfo[] drawers; public string blender; }

[Serializable] public class PoliceDocument { public string id, title, source, body, image; }
[Serializable] public class PoliceDocumentSet {
    public PoliceDocument[] documents;
    public static PoliceDocumentSet Load() {
        var asset = Resources.Load<TextAsset>("PoliceDocuments");
        if (!asset) throw new InvalidOperationException("Missing Resources/PoliceDocuments.json");
        var set = JsonUtility.FromJson<PoliceDocumentSet>(asset.text);
        if (set.documents == null || set.documents.Select(d => d.id).Distinct().Count() != set.documents.Length) throw new InvalidOperationException("Police documents need unique IDs");
        return set;
    }
    public PoliceDocument Get(string id) {
        var doc = documents.FirstOrDefault(d => d.id == id);
        if (doc == null) throw new InvalidOperationException("Unknown police document " + id);
        return doc;
    }
}
