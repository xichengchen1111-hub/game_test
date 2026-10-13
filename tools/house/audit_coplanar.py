"""Find same-facing coplanar overlaps in the authored house before Unity's ZFightRepair has to patch them.

Mirrors the Unity detector's rule: two triangles of different objects, facing the same way, whose planes are within
0.6 mm and whose overlap is at least 1 cm2. Reports object pairs in game metres (x east, y up, z north).
Run: Blender -b --factory-startup --python tools/house/audit_coplanar.py [-- <blend> <out.json>]
"""
import bpy, json, sys, math
from pathlib import Path
from mathutils import Vector

r = Path(__file__).resolve().parents[2]
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
blend = argv[0] if argv else str(r / 'assets/source/house/House1931.blend')
out = argv[1] if len(argv) > 1 else str(r / 'assets/source/house/coplanar-audit.json')
bpy.ops.wm.open_mainfile(filepath=blend)
dg = bpy.context.evaluated_depsgraph_get()
TOL, MIN_AREA = .0006, 1e-4


def game(v):
    return Vector((v.x, v.z, -v.y))


tris = []
for o in bpy.context.scene.objects:
    if o.type != 'MESH':
        continue
    e = o.evaluated_get(dg); me = e.to_mesh(); me.calc_loop_triangles(); mw = o.matrix_world
    vs = [game(mw @ v.co) for v in me.vertices]
    for t in me.loop_triangles:
        a, b, c = (vs[i] for i in t.vertices)
        n = (b - a).cross(c - a)
        if n.length < 1e-10:
            continue
        area = n.length / 2; n.normalize()
        tris.append((o.name, n, n.dot(a), (a, b, c), area))
    e.to_mesh_clear()

buckets = {}
for i, (name, n, d, pts, area) in enumerate(tris):
    key = (round(n.x, 3), round(n.y, 3), round(n.z, 3))
    buckets.setdefault(key, []).append(i)


def clip(poly, a, b):
    # Keep the part of poly left of edge a->b (2D Sutherland-Hodgman).
    def side(p):
        return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])
    res = []
    for i in range(len(poly)):
        p, q = poly[i], poly[(i + 1) % len(poly)]
        sp, sq = side(p), side(q)
        if sp >= 0:
            res.append(p)
        if (sp >= 0) != (sq >= 0):
            t = sp / (sp - sq); res.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
    return res


def area2(poly):
    return abs(sum(poly[i][0] * poly[(i + 1) % len(poly)][1] - poly[(i + 1) % len(poly)][0] * poly[i][1] for i in range(len(poly)))) / 2


pairs = {}
for key, ids in buckets.items():
    n = Vector(key).normalized()
    u = n.cross(Vector((0, 1, 0)) if abs(n.y) < .9 else Vector((1, 0, 0))).normalized(); w = n.cross(u)
    ids.sort(key=lambda i: tris[i][2])
    for x in range(len(ids)):
        ia = ids[x]; na, _, da, pa, _ = tris[ia]
        A = [(p.dot(u), p.dot(w)) for p in pa]
        if (A[1][0] - A[0][0]) * (A[2][1] - A[0][1]) - (A[1][1] - A[0][1]) * (A[2][0] - A[0][0]) < 0:
            A = A[::-1]
        for y in range(x + 1, len(ids)):
            ib = ids[y]; nb, _, db, pb, _ = tris[ib]
            if db - da > TOL:
                break
            if nb == na:
                continue
            poly = [(p.dot(u), p.dot(w)) for p in pb]
            for k in range(3):
                if len(poly) < 3:
                    break
                poly = clip(poly, A[k], A[(k + 1) % 3])
            if len(poly) < 3:
                continue
            ov = area2(poly)
            if ov < 1e-6:
                continue
            pk = tuple(sorted((na.split('.')[0], nb.split('.')[0])))
            rec = pairs.setdefault(pk, dict(a=pk[0], b=pk[1], area=0.0, count=0, at=None))
            rec['area'] += ov; rec['count'] += 1
            if rec['at'] is None:
                c = sum(pa, Vector()) / 3; rec['at'] = [round(c.x, 3), round(c.y, 3), round(c.z, 3)]
report = sorted([p for p in pairs.values() if p['area'] >= MIN_AREA], key=lambda p: -p['area'])
for p in report:
    p['area'] = round(p['area'] * 1e4, 1)
Path(out).write_text(json.dumps(dict(source=blend, triangles=len(tris), pairsOver1cm2=len(report), pairs=report, units='area cm2; object base names'), indent=1))
print('COPLANAR_AUDIT', len(tris), 'triangles', len(report), 'object pairs')
