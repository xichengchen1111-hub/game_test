using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shared z-fighting detector (ZFightScan reports, ZFightRepair fixes). Finds pairs of
// rendered triangles in the same plane (offset <= maxOffset), facing the same way, that
// overlap by >= minArea. A pair is "visible" when the two surfaces would shade
// differently (material, texture coordinates or light layers differ). Undersides lying on
// the ground and buried triangles are skipped.
public sealed class CoplanarDetector {
    public struct Tri {
        public Vector3 a, b, c, n; public Vector2 ua, ub, uc; public float d, area;
        public int renderer, submesh, index; public string material; public uint layers;
    }
    public struct Pair { public int a, b; public float area, offset; public string kind; public Vector3 centre; }
    public readonly List<Tri> tris = new List<Tri>();
    public readonly List<MeshRenderer> renderers = new List<MeshRenderer>();
    readonly float maxOffset;
    public CoplanarDetector(float maxOffset = .002f) { this.maxOffset = maxOffset; }

    public void Add(MeshRenderer r) {
        if (!r || !r.enabled || !r.gameObject.activeInHierarchy) return;
        var lod = r.GetComponentInParent<LODGroup>();
        if (lod) { var lods = lod.GetLODs(); if (lods.Length > 0 && !lods[0].renderers.Contains(r)) return; }
        var mf = r.GetComponent<MeshFilter>(); if (!mf || !mf.sharedMesh) return;
        var mesh = mf.sharedMesh; var v = mesh.vertices; var uvs = mesh.uv; bool hasUv = uvs != null && uvs.Length == v.Length;
        var m = r.transform.localToWorldMatrix; int owner = renderers.Count; renderers.Add(r);
        for (int s = 0; s < mesh.subMeshCount; s++) {
            if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
            var mat = s < r.sharedMaterials.Length && r.sharedMaterials[s] ? r.sharedMaterials[s].name : "none";
            var idx = mesh.GetTriangles(s);
            for (int i = 0; i + 2 < idx.Length; i += 3) {
                var a = m.MultiplyPoint3x4(v[idx[i]]); var b = m.MultiplyPoint3x4(v[idx[i + 1]]); var c = m.MultiplyPoint3x4(v[idx[i + 2]]);
                var cross = Vector3.Cross(b - a, c - a); float area2 = cross.magnitude;
                if (area2 < 2e-5f) continue;
                var n = cross / area2;
                float top = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
                if (n.y < -.7f && top < .16f) continue;   // underside lying on the ground
                if (top < -.05f) continue;                 // buried below the town ground
                tris.Add(new Tri { a = a, b = b, c = c, n = n, d = Vector3.Dot(n, a), area = area2 / 2, renderer = owner, submesh = s, index = i, material = mat, layers = r.renderingLayerMask,
                    ua = hasUv ? uvs[idx[i]] : Vector2.zero, ub = hasUv ? uvs[idx[i + 1]] : Vector2.zero, uc = hasUv ? uvs[idx[i + 2]] : Vector2.zero });
            }
        }
    }

    (int, int, int, long) Key(Tri t) => (Mathf.RoundToInt(t.n.x * 500), Mathf.RoundToInt(t.n.y * 500), Mathf.RoundToInt(t.n.z * 500), (long)Mathf.Floor(t.d / maxOffset));

    public List<Pair> Detect(float minArea = .0001f) {
        var buckets = new Dictionary<(int, int, int, long), List<int>>();
        for (int i = 0; i < tris.Count; i++) {
            var key = Key(tris[i]);
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
            list.Add(i);
        }
        var pairs = new List<Pair>();
        foreach (var pair in buckets) {
            var (nx, ny, nz, dk) = pair.Key;
            var own = pair.Value; buckets.TryGetValue((nx, ny, nz, dk + 1), out var next);
            if (own.Count + (next?.Count ?? 0) < 2) continue;
            var n = tris[own[0]].n;
            var u = Vector3.Cross(n, Mathf.Abs(n.y) < .9f ? Vector3.up : Vector3.right).normalized; var w = Vector3.Cross(n, u);
            var items = new List<(int tri, bool nextBucket, Vector2 min, Vector2 max)>();
            void Item(int i, bool nb) {
                var t = tris[i]; Vector2 p0 = P(t.a, u, w), p1 = P(t.b, u, w), p2 = P(t.c, u, w);
                items.Add((i, nb, Vector2.Min(p0, Vector2.Min(p1, p2)), Vector2.Max(p0, Vector2.Max(p1, p2))));
            }
            foreach (int i in own) Item(i, false);
            if (next != null) foreach (int i in next) Item(i, true);
            items.Sort((x, y) => x.min.x.CompareTo(y.min.x));
            var active = new List<int>();
            for (int k = 0; k < items.Count; k++) {
                var cur = items[k];
                active.RemoveAll(q => items[q].max.x < cur.min.x - 1e-4f);
                foreach (int q in active) {
                    var other = items[q];
                    if (other.nextBucket && cur.nextBucket) continue;
                    if (other.max.y < cur.min.y - 1e-4f || other.min.y > cur.max.y + 1e-4f) continue;
                    var A = tris[other.tri]; var B = tris[cur.tri];
                    if (Vector3.Dot(A.n, B.n) < .9995f) continue;
                    float off = PlaneGap(A, B); if (off > maxOffset) continue;
                    float area = (float)OverlapArea(A, B, u, w, out var centre);
                    if (area < minArea) continue;
                    bool uvSame = Vector2.Distance(UvAt(A, centre), UvAt(B, centre)) < .002f;
                    string kind = A.material != B.material ? "material" : !uvSame ? "uv" : A.layers != B.layers ? "lightlayers" : "identical";
                    pairs.Add(new Pair { a = other.tri, b = cur.tri, area = area, offset = off, kind = kind, centre = centre });
                }
                active.Add(k);
            }
        }
        return pairs;
    }

    static Vector2 P(Vector3 p, Vector3 u, Vector3 w) => new Vector2(Vector3.Dot(p, u), Vector3.Dot(p, w));
    // Distance of the smaller triangle's corners from the larger triangle's plane. A sliver's own
    // normal is poorly determined, and |dA - dB| far from the origin then fakes coplanarity.
    static float PlaneGap(Tri A, Tri B) {
        var big = A.area >= B.area ? A : B; var small = A.area >= B.area ? B : A;
        return Mathf.Max(Mathf.Abs(Vector3.Dot(big.n, small.a - big.a)), Mathf.Max(Mathf.Abs(Vector3.Dot(big.n, small.b - big.a)), Mathf.Abs(Vector3.Dot(big.n, small.c - big.a))));
    }
    static Vector2 UvAt(Tri t, Vector3 p) {
        Vector3 v0 = t.b - t.a, v1 = t.c - t.a, v2 = p - t.a;
        float d00 = Vector3.Dot(v0, v0), d01 = Vector3.Dot(v0, v1), d11 = Vector3.Dot(v1, v1), d20 = Vector3.Dot(v2, v0), d21 = Vector3.Dot(v2, v1);
        float den = d00 * d11 - d01 * d01; if (Mathf.Abs(den) < 1e-12f) return t.ua;
        float bv = (d11 * d20 - d01 * d21) / den, bw = (d00 * d21 - d01 * d20) / den;
        return t.ua * (1 - bv - bw) + t.ub * bv + t.uc * bw;
    }
    // Overlap is clipped in double precision around A's first corner: in float at town
    // coordinates (~200m) the two halves of one long 2mm strip "overlapped" by about 1cm2.
    struct D2 { public double x, y; public D2(double x, double y) { this.x = x; this.y = y; } }
    static D2 Pd(Vector3 p, Vector3 o, Vector3 u, Vector3 w) {
        double px = (double)p.x - o.x, py = (double)p.y - o.y, pz = (double)p.z - o.z;
        return new D2(px * u.x + py * u.y + pz * u.z, px * w.x + py * w.y + pz * w.z);
    }
    static double Side(D2 a, D2 b, D2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
    static double Signed(IList<D2> poly) { double s = 0; for (int i = 0; i < poly.Count; i++) { var a = poly[i]; var b = poly[(i + 1) % poly.Count]; s += a.x * b.y - b.x * a.y; } return s / 2; }
    static D2 Intersect(D2 p, D2 q, D2 a, D2 b) { double s1 = Side(a, b, p), s2 = Side(a, b, q), t = s1 / (s1 - s2); return new D2(p.x + (q.x - p.x) * t, p.y + (q.y - p.y) * t); }
    static double OverlapArea(Tri A, Tri B, Vector3 u, Vector3 w, out Vector3 centre) {
        var o = A.a; centre = o;
        var subject = new List<D2> { Pd(A.a, o, u, w), Pd(A.b, o, u, w), Pd(A.c, o, u, w) };
        var clip = new[] { Pd(B.a, o, u, w), Pd(B.b, o, u, w), Pd(B.c, o, u, w) };
        if (Signed(subject) < 0) subject.Reverse();
        if (Signed(clip) < 0) Array.Reverse(clip);
        for (int e = 0; e < 3 && subject.Count > 0; e++) {
            D2 c0 = clip[e], c1 = clip[(e + 1) % 3]; var input = subject; subject = new List<D2>();
            for (int k = 0; k < input.Count; k++) {
                D2 cur = input[k], prev = input[(k + input.Count - 1) % input.Count];
                bool cin = Side(c0, c1, cur) >= 0, pin = Side(c0, c1, prev) >= 0;
                if (cin) { if (!pin) subject.Add(Intersect(prev, cur, c0, c1)); subject.Add(cur); }
                else if (pin) subject.Add(Intersect(prev, cur, c0, c1));
            }
        }
        if (subject.Count < 3) return 0;
        double cx = 0, cy = 0; foreach (var q in subject) { cx += q.x; cy += q.y; }
        centre = o + u * (float)(cx / subject.Count) + w * (float)(cy / subject.Count);
        return Math.Abs(Signed(subject));
    }
}
