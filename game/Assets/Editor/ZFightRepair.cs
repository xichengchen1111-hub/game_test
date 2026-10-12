using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Build-time z-fighting repair (2026-10-12 audit). Exactly coplanar, same-facing surfaces
// that would shade differently flicker. Each planar patch (one renderer + submesh + plane)
// that overlaps others gets the smallest step (k x 2mm along the shared normal) not used by
// an overlapping neighbour; larger patches are placed first, so the main surface stays and
// trims, decals and details move. Duplicate triangles inside one patch step 1mm. A step can
// land on another surface, so the pass repeats until nothing editable is left.
// Render meshes only: copies are saved under Assets/Generated/ZFightRepair and colliders keep
// their original meshes. Same precedent as ClockTowerSurfaceChecks.Repair.
public static class ZFightRepair {
    public const float Step = .002f;
    const int MaxPasses = 6;
    const string Folder = "Assets/Generated/ZFightRepair/";
    static readonly string[] Trace = (Environment.GetEnvironmentVariable("ZFIGHT_TRACE") ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    static readonly string[] MovingTypes = { "ShopMechanism", "ClockDial", "RailDoor", "RailWheel", "RailRod", "CarPart", "MilkShopFixture", "ClockTowerGearMotion", "StationClerkPerformance", "MilkboyPerformance", "WatchmakerPerformance", "AmbientMotion" };

    // Parts that move relative to their prefab (doors, drawers, hands, wheels, actors). The repair includes them at
    // their authored pose: a step along the normal in their own frame stays correct at every pose. The scan reports them apart.
    public static bool Moving(Transform t, Transform root) {
        for (var p = t; p && p != root.parent; p = p.parent) {
            if (p.name.StartsWith("Pivot_") || p.name.StartsWith("Wheel_") || p.name.StartsWith("Rod_") || p.name.StartsWith("Leaf_") || p.name.StartsWith("Drawer_")) return true;
            foreach (var c in p.GetComponents<MonoBehaviour>()) if (c && MovingTypes.Contains(c.GetType().Name)) return true;
            if (p.GetComponent<Animator>()) return true;
        }
        return false;
    }

    static CoplanarDetector Detect(Transform[] roots) {
        var det = new CoplanarDetector(.0006f);
        foreach (var root in roots)
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
                if (r.GetComponent<MeshFilter>() && !(r.sharedMaterials.Any(m => m && m.shader && m.shader.name.Contains("Halo")))) det.Add(r);
        return det;
    }

    public static int Repair(Transform root, string label, Func<MeshRenderer, bool> editable = null) => Repair(new[] { root }, label, editable);

    // Scene passes give every root at once, so pairs between roots (a train on a platform) are seen.
    public static int Repair(Transform[] roots, string label, Func<MeshRenderer, bool> editable = null) {
        var original = new Dictionary<MeshFilter, Mesh>();
        var total = new Dictionary<MeshFilter, Dictionary<(int s, int i), (Vector3 n, float offset)>>();
        var temporary = new HashSet<Mesh>();
        int first = 0, patches = 0, singles = 0, pass = 0;
        List<CoplanarDetector.Pair> pairs, stuck; CoplanarDetector det;
        for (; ; pass++) {
            det = Detect(roots);
            pairs = det.Detect().Where(p => p.kind != "identical").ToList();
            if (pass == 0) first = pairs.Count;
            var push = Plan(det, pairs, editable, label, pass, ref patches, ref singles, out stuck);
            if (push.Count == 0 || pass == MaxPasses) break;
            var touched = new HashSet<MeshFilter>();
            foreach (var kv in push) {
                var tri = det.tris[kv.Key]; var mf = det.renderers[tri.renderer].GetComponent<MeshFilter>();
                if (!original.ContainsKey(mf)) original[mf] = mf.sharedMesh;
                if (!total.TryGetValue(mf, out var edits)) total[mf] = edits = new Dictionary<(int s, int i), (Vector3 n, float offset)>();
                var key = (tri.submesh, tri.index);
                edits[key] = edits.TryGetValue(key, out var e) ? (e.n, e.offset + kv.Value) : (tri.n, kv.Value);
                touched.Add(mf);
            }
            foreach (var mf in touched) {
                var old = mf.sharedMesh;
                mf.sharedMesh = Build(original[mf], Local(mf.transform, total[mf]), out _);
                temporary.Add(mf.sharedMesh);
                if (temporary.Contains(old)) { temporary.Remove(old); UnityEngine.Object.DestroyImmediate(old); }
            }
        }
        int remaining = pairs.Count - stuck.Count;
        foreach (var mf in total.Keys) { var tmp = mf.sharedMesh; mf.sharedMesh = original[mf]; if (temporary.Contains(tmp)) UnityEngine.Object.DestroyImmediate(tmp); }
        var kept = new HashSet<string>();
        int edited = total.Count == 0 ? 0 : Save(total, original, label, kept);
        // Each label is regenerated whole by its pass: drop copies this build no longer uses.
        if (Directory.Exists(Folder + label))
            foreach (var file in Directory.GetFiles(Folder + label, "*.asset"))
                if (!kept.Contains(file.Replace('\\', '/'))) AssetDatabase.DeleteAsset(file.Replace('\\', '/'));
        foreach (var p in stuck.OrderByDescending(p => p.area).Take(8))
            Debug.Log("ZFIGHT_REPAIR " + label + " fixed pair: " + Name(det, p.a) + " <> " + Name(det, p.b) + " " + (p.area * 10000).ToString("0.0") + "cm2 at " + p.centre);
        Debug.Log("ZFIGHT_REPAIR " + label + ": " + first + " visible coplanar pairs, " + patches + " patch steps and " + singles + " triangle steps in " + pass + " passes, " + edited + " meshes edited, " +
            stuck.Count + " pairs between fixed geometry, " + remaining + " editable pairs left");
        if (remaining > 0) throw new InvalidOperationException("Z-fight repair " + label + " did not converge: " + remaining + " editable coplanar pairs left");
        // Pairs between two fixed (already saved) prefabs cannot be moved here: fix the source or the earlier prefab pass.
        if (stuck.Count > 0) throw new InvalidOperationException("Z-fight repair " + label + ": " + stuck.Count + " coplanar pairs between fixed geometry (see ZFIGHT_REPAIR fixed pair lines)");
        return edited;
    }

    static string Name(CoplanarDetector det, int tri) => det.renderers[det.tris[tri].renderer].name + "(" + det.tris[tri].material + ")";

    // Greedy colouring of overlapping patches: largest first, each takes the smallest step
    // not used by an already placed neighbour. Fixed (non-editable) patches stay at 0.
    static Dictionary<int, float> Plan(CoplanarDetector det, List<CoplanarDetector.Pair> pairs, Func<MeshRenderer, bool> editable, string label, int pass,
        ref int patches, ref int singles, out List<CoplanarDetector.Pair> stuck) {
        stuck = new List<CoplanarDetector.Pair>();
        var push = new Dictionary<int, float>();
        if (pairs.Count == 0) return push;
        bool Editable(int r) => editable == null || editable(det.renderers[r]);
        // Patch = renderer + submesh + plane. Planes are found by chaining sorted plane distances
        // (gaps > 0.3mm split), so one flat face is never cut in two by a rounding boundary.
        var patchOf = new int[det.tris.Count];
        int nextPatch = 0;
        foreach (var g in Enumerable.Range(0, det.tris.Count).GroupBy(i => { var q = det.tris[i]; return (q.renderer, q.submesh, Mathf.RoundToInt(q.n.x * 500), Mathf.RoundToInt(q.n.y * 500), Mathf.RoundToInt(q.n.z * 500)); })) {
            float last = float.NegativeInfinity;
            foreach (int i in g.OrderBy(i => det.tris[i].d)) { if (det.tris[i].d - last > .0003f) nextPatch++; last = det.tris[i].d; patchOf[i] = nextPatch; }
        }
        var area = new Dictionary<int, float>(); var members = new Dictionary<int, List<int>>(); var owner = new Dictionary<int, int>();
        for (int i = 0; i < det.tris.Count; i++) {
            int k = patchOf[i]; area[k] = area.TryGetValue(k, out var a) ? a + det.tris[i].area : det.tris[i].area;
            if (!members.TryGetValue(k, out var l)) { members[k] = l = new List<int>(); owner[k] = det.tris[i].renderer; }
            l.Add(i);
        }
        var neighbours = new Dictionary<int, HashSet<int>>();
        var single = new Dictionary<int, float>();
        foreach (var p in pairs) {
            var A = det.tris[p.a]; var B = det.tris[p.b]; int ka = patchOf[p.a], kb = patchOf[p.b];
            if (ka == kb) {
                int smaller = A.area < B.area || (A.area == B.area && p.a > p.b) ? p.a : p.b;
                if (Editable(det.tris[smaller].renderer)) single[smaller] = Step / 2; else stuck.Add(p);
                continue;
            }
            if (!Editable(A.renderer) && !Editable(B.renderer)) { stuck.Add(p); continue; }
            if (!neighbours.TryGetValue(ka, out var na)) neighbours[ka] = na = new HashSet<int>(); na.Add(kb);
            if (!neighbours.TryGetValue(kb, out var nb)) neighbours[kb] = nb = new HashSet<int>(); nb.Add(ka);
        }
        var rank = new Dictionary<int, int>();
        foreach (int k in neighbours.Keys) if (!Editable(owner[k])) rank[k] = 0;
        foreach (int k in neighbours.Keys.Where(k => Editable(owner[k])).OrderByDescending(k => area[k]).ThenBy(k => det.tris[members[k][0]].material, StringComparer.Ordinal).ThenBy(k => k).ToList()) {
            var used = new HashSet<int>(neighbours[k].Where(rank.ContainsKey).Select(q => rank[q]));
            int r = 0; while (used.Contains(r)) r++;
            rank[k] = r;
            if (r > 0) { foreach (int t in members[k]) push[t] = r * Step; patches++; }
        }
        // Duplicates inside one patch step half a unit beyond wherever their patch went.
        foreach (var kv in single) { push[kv.Key] = (push.TryGetValue(kv.Key, out var o) ? o : 0) + kv.Value; singles++; }
        if (Trace.Length > 0)
            foreach (var p in pairs) {
                string an = det.renderers[det.tris[p.a].renderer].name, bn = det.renderers[det.tris[p.b].renderer].name;
                if (!Trace.Any(t => an.Contains(t) || bn.Contains(t))) continue;
                Debug.Log("ZTRACE " + label + " pass " + pass + " " + Info(p.a) + " <> " + Info(p.b) + " n=" + det.tris[p.a].n + " d " + det.tris[p.a].d.ToString("0.00000") + "/" + det.tris[p.b].d.ToString("0.00000") + " " + p.kind);
            }
        return push;

        string Info(int t) {
            int k = patchOf[t];
            return det.renderers[det.tris[t].renderer].name + "[" + det.tris[t].submesh + ":" + det.tris[t].index + " patch " + k + " area " + area[k].ToString("0.0000") +
                " editable " + Editable(det.tris[t].renderer) + " rank " + (rank.TryGetValue(k, out var rr) ? rr : -1) + " push " + (push.TryGetValue(t, out var pp) ? pp : 0) + "]";
        }
    }

    static List<(int s, int i, Vector3 v)> Local(Transform t, Dictionary<(int s, int i), (Vector3 n, float offset)> edits) =>
        edits.OrderBy(e => e.Key.s).ThenBy(e => e.Key.i).Select(e => (e.Key.s, e.Key.i, t.InverseTransformVector(e.Value.n * e.Value.offset))).ToList();

    static Mesh Build(Mesh source, List<(int s, int i, Vector3 v)> edits, out float maxStep) {
        var verts = new List<Vector3>(); source.GetVertices(verts);
        var normals = new List<Vector3>(); source.GetNormals(normals);
        var tangents = new List<Vector4>(); source.GetTangents(tangents);
        var colors = new List<Color>(); source.GetColors(colors);
        var uvSets = new List<List<Vector4>>();
        for (int ch = 0; ch < 4; ch++) { var l = new List<Vector4>(); source.GetUVs(ch, l); uvSets.Add(l); }
        var subs = new List<List<int>>();
        for (int s = 0; s < source.subMeshCount; s++) subs.Add(new List<int>(source.GetTriangles(s)));
        maxStep = 0;
        foreach (var (s, i, v) in edits) {
            var list = subs[s]; maxStep = Mathf.Max(maxStep, v.magnitude);
            for (int k = 0; k < 3; k++) {
                int old = list[i + k]; int fresh = verts.Count;
                verts.Add(verts[old] + v);
                if (normals.Count > old) normals.Add(normals[old]);
                if (tangents.Count > old) tangents.Add(tangents[old]);
                if (colors.Count > old) colors.Add(colors[old]);
                foreach (var l in uvSets) if (l.Count > old) l.Add(l[old]);
                list[i + k] = fresh;
            }
        }
        var mesh = new Mesh { name = source.name.Replace(" (z-fight repaired)", "") + " (z-fight repaired)", indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : source.indexFormat };
        mesh.SetVertices(verts);
        if (normals.Count == verts.Count) mesh.SetNormals(normals);
        if (tangents.Count == verts.Count) mesh.SetTangents(tangents);
        if (colors.Count == verts.Count) mesh.SetColors(colors);
        for (int ch = 0; ch < 4; ch++) if (uvSets[ch].Count == verts.Count) mesh.SetUVs(ch, uvSets[ch]);
        mesh.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s, false);
        mesh.RecalculateBounds();
        return mesh;
    }

    static uint Fnv(string s) { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return h; }
    static string Path(Transform t) { var parts = new List<string>(); for (; t; t = t.parent) parts.Add(t.name + "#" + t.GetSiblingIndex()); parts.Reverse(); return string.Join("/", parts); }
    static string Sanitize(string s) => new string(s.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
    // Source name without our suffixes, so a copy of a repaired copy does not grow "-hash-hash" names.
    static string BaseName(string name) {
        var s = Sanitize(name.Replace(" (z-fight repaired)", ""));
        while (s.Length > 9 && s[s.Length - 9] == '-' && s.Substring(s.Length - 8).All(c => "0123456789abcdef".IndexOf(c) >= 0)) s = s.Substring(0, s.Length - 9);
        return s;
    }
    static string V(Vector3 v) => v.x.ToString("0.#####") + "," + v.y.ToString("0.#####") + "," + v.z.ToString("0.#####");

    static int Save(Dictionary<MeshFilter, Dictionary<(int s, int i), (Vector3 n, float offset)>> total, Dictionary<MeshFilter, Mesh> original, string label, HashSet<string> kept) {
        Directory.CreateDirectory(Folder + label);
        var cache = new Dictionary<string, Mesh>();
        int edited = 0;
        foreach (var mf in total.Keys.OrderBy(m => Path(m.transform), StringComparer.Ordinal).ToList()) {
            var source = original[mf]; var edits = Local(mf.transform, total[mf]);
            // Identify the exact source mesh (FBX parts often share a mesh name): asset GUID + local file id,
            // or the instance id for scene-built meshes. Same source + same edits => one shared copy.
            string identity = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long fileId) && !string.IsNullOrEmpty(guid)
                ? guid + ":" + fileId : "scene:" + source.GetInstanceID();
            string body = "|" + source.name + "|" + source.vertexCount + "|" + string.Join(";", edits.Select(e => e.s + ":" + e.i + ":" + V(e.v)));
            if (cache.TryGetValue(identity + body, out var shared)) { mf.sharedMesh = shared; edited++; continue; }
            var mesh = Build(source, edits, out float maxStep);
            // Safety: a repaired copy may only grow by its largest step; anything else means a wrong source.
            if (Vector3.Distance(mesh.bounds.min, source.bounds.min) > maxStep * 1.8f + .001f || Vector3.Distance(mesh.bounds.max, source.bounds.max) > maxStep * 1.8f + .001f)
                throw new InvalidOperationException("Z-fight repair changed " + source.name + " beyond its step: " + source.bounds + " -> " + mesh.bounds);
            string stable = identity.StartsWith("scene:") ? Path(mf.transform) + body : identity + body;
            string stem = BaseName(source.name) + "-" + Fnv(stable).ToString("x8");
            string path = Folder + label + "/" + stem + ".asset";
            // CreateAsset names a new asset after its file; updates must carry the same name, or the
            // serialized meshes (and downstream file names) alternate between builds.
            mesh.name = stem;
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved) { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; EditorUtility.SetDirty(mesh); }
            else AssetDatabase.CreateAsset(mesh, path);
            cache[identity + body] = mesh; mf.sharedMesh = mesh; kept.Add(path); edited++;
        }
        return edited;
    }
}
