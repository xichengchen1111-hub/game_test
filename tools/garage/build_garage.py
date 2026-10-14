"""Blender generator for the E stone-barn repair garage (B07, 旧石仓修车院).

Reads the shared layout contract (tools/garage/garage_layout.py output) and builds one
editable scene: roughstone hall with real segmental-arch apertures, three timber cross
gables, open timber trusses and sarking, the loft over the tool store with its stair,
the cream office range, the rear yard fence and lean-to shed, joinery with movable
leaves and a drawer on their pivots, lamps and every furnishing and prop (work truck,
motorcycle, chain hoist, benches, vice...). Exports the FBX used by Unity's
BuildSlice.Garage and saves the packed .blend.

    blender --background --factory-startup --python-exit-code 1 \
        --python tools/garage/build_garage.py -- \
        --layout assets/source/garage/garage-layout.json \
        --output assets/source/garage --unity game/Assets/Art/Garage \
        --previews output/demo/garage/blender

Coordinates are Unity building-local metres (+X east, +Y up, +Z north). Geometry
primitives, the opening column cut, the police furniture kit and the panel/glazed leaf
builder are shared with tools/police/build_police_station.py (same conventions and
object-name contract): Ext_*/Int_* shells, Glass, Leaf_<opening>_<n> (origin on the
hinge), Drawer_<n>, Furn_<id> (interior), ExtFurn_<id> (outside the rooms),
Lamp_<id>, Bulb_<id>, EastMarker/NorthMarker.
"""
import argparse, json, math, random, sys
from pathlib import Path

import bpy, bmesh
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'police'))
import build_police_station as K  # noqa: E402  (shared primitives and furniture kit)
from build_police_station import part, box, bbox, tri, prism, cylinder, frustum, Frame, u  # noqa: E402

# Garage materials join the police kit's (furniture reused from that kit keeps its names).
# name: (hex, metallic, roughness, metres per texture repeat or 0 for flat). Textured families carry a
# representative preview colour; Unity tints those textures white (BuildSlice.Garage.GarageMaterial).
GARAGE_MATERIALS = {
    'GExt_Rubble': ('948d7d', 0, .9, 1.6), 'GExt_Ashlar': ('a39b8a', 0, .85, 1.2), 'GExt_Cream': ('ddd2b4', 0, .9, 3.0),
    'GExt_Slate': ('4b5157', 0, .6, 1.5), 'GExt_Tin': ('4a4c4c', .35, .55, 2.0), 'GExt_Boards': ('6f5f4c', 0, .85, 1.2),
    'GExt_Timber': ('5a4331', 0, .75, 1.0), 'GExt_Walnut': ('4a3526', 0, .5, 1.0), 'GExt_Iron': ('2a2d2e', .6, .5, 0),
    'GExt_Gutter': ('34393b', .5, .5, 0), 'GExt_Green': ('2f4339', 0, .55, 0), 'GExt_Cobble': ('6f6b62', 0, .85, 2.0),
    'GExt_Flags': ('9a948a', 0, .8, 3.0), 'GExt_Sign': ('2f3f37', 0, .6, 0), 'GExt_Slate2': ('3e4348', 0, .55, 1.5),
    'GInt_Lime': ('d8d0bb', 0, .92, 1.6), 'GInt_Plaster': ('e3d8bd', 0, .9, 3.0), 'GInt_Ceiling': ('e8e1cf', 0, .95, 3.0),
    'GInt_Cement': ('8a8880', 0, .7, 3.0), 'GInt_Plank': ('8e7254', 0, .7, 2.0), 'GInt_Copper': ('a8653a', .85, .38, 0),
    'GInt_Steel': ('6e7376', .7, .4, 0), 'GInt_Rubber': ('1d1d1c', 0, .85, 0), 'GInt_Oil': ('17140f', .1, .3, 0),
    'GInt_Rag': ('8b877c', 0, .95, 0), 'GInt_Truck': ('2f4a3c', .3, .45, 0), 'GInt_Red': ('7c2e24', .3, .5, 0),
    'GInt_Blue': ('2c4560', .3, .5, 0), 'GInt_Chrome': ('c9c6bd', .9, .2, 0), 'GInt_Canvas': ('6f6a55', 0, .9, 0),
    'GInt_Chain': ('3a3b3a', .7, .45, 0), 'GInt_Grind': ('8d877b', 0, .95, 0), 'GInt_Water': ('3a4a4f', .1, .1, 0),
    'G_Glass': ('9fb3be', 0, .05, 0), 'G_LampGlass': ('ffd9a0', 0, .2, 0), 'G_Bulb': ('fff0cf', 0, .2, 0),
}
K.MATERIALS.update(GARAGE_MATERIALS)
MATS = K.MATERIALS
RNG = random.Random(19310114)
EXTERIOR_ROOMS = ('', 'street', 'yard')


# ---------------------------------------------------------------------------
# Small geometry helpers on top of the police primitives
# ---------------------------------------------------------------------------
def add(a, b):
    return tuple(x + y for x, y in zip(a, b))


def mul(a, s):
    return tuple(x * s for x in a)


def norm(a):
    ln = math.sqrt(sum(x * x for x in a)) or 1
    return tuple(x / ln for x in a)


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def obox(p, c, ax, ay, az, s, mat, faces='all'):
    """Oriented box: centre c, orthonormal axes ax/ay/az (Unity), size s along them."""
    hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    def P(i, j, k):
        return add(add(add(c, mul(ax, i * hx)), mul(ay, j * hy)), mul(az, k * hz))
    spec = {'+x': ([P(1, -1, -1), P(1, -1, 1), P(1, 1, 1), P(1, 1, -1)], ax),
            '-x': ([P(-1, -1, 1), P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1)], mul(ax, -1)),
            '+y': ([P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1)], ay),
            '-y': ([P(-1, -1, 1), P(1, -1, 1), P(1, -1, -1), P(-1, -1, -1)], mul(ay, -1)),
            '+z': ([P(1, -1, 1), P(-1, -1, 1), P(-1, 1, 1), P(1, 1, 1)], az),
            '-z': ([P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1)], mul(az, -1))}
    for key, (verts, n) in spec.items():
        if faces == 'all' or key in faces:
            p.quad(verts, n, mat)


def beam(p, a, b, w, h, mat, up=(0, 1, 0)):
    """Rectangular member from point a to b: width w across, height h along 'up' (projected)."""
    d = tuple(y - x for x, y in zip(a, b)); ln = math.sqrt(sum(x * x for x in d))
    az = norm(d); ax = norm(cross(up, az))
    if sum(x * x for x in ax) < 1e-9:
        ax = (1, 0, 0)
    ay = norm(cross(az, ax))
    obox(p, mul(add(a, b), .5), ax, ay, az, (w, h, ln), mat)


def tube(p, a, b, r, mat, seg=8, caps=True):
    """Cylinder from a to b (any direction)."""
    d = tuple(y - x for x, y in zip(a, b)); az = norm(d)
    ref = (0, 1, 0) if abs(az[1]) < .9 else (1, 0, 0)
    ax = norm(cross(ref, az)); ay = cross(az, ax)
    ring = lambda c, k: add(c, add(mul(ax, r * math.cos(2 * math.pi * k / seg)), mul(ay, r * math.sin(2 * math.pi * k / seg))))
    for k in range(seg):
        a0, a1, b0, b1 = ring(a, k), ring(a, k + 1), ring(b, k), ring(b, k + 1)
        mid = 2 * math.pi * (k + .5) / seg
        n = add(mul(ax, math.cos(mid)), mul(ay, math.sin(mid)))
        p.quad([a0, a1, b1, b0], n, mat)
        if caps:
            tri(p, [a, a1, a0], mul(az, -1), mat)
            tri(p, [b, b0, b1], az, mat)


def ring_disc(p, centre, axis, r0, r1, mat, seg=16):
    """Flat annulus (r0 inner, r1 outer) facing +axis."""
    az = norm(axis); ref = (0, 1, 0) if abs(az[1]) < .9 else (1, 0, 0)
    ax = norm(cross(ref, az)); ay = cross(az, ax)
    for k in range(seg):
        t0, t1 = 2 * math.pi * k / seg, 2 * math.pi * (k + 1) / seg
        pts = [add(centre, add(mul(ax, r * math.cos(t)), mul(ay, r * math.sin(t)))) for r, t in ((r0, t0), (r1, t0), (r1, t1), (r0, t1))]
        p.quad(pts, az, mat)


def tyre(p, centre, axis, r_out, r_in, width, tread='GInt_Rubber', rim='GInt_Steel', spokes=0, spoke_mat='PInt_FurnLight', seg=18):
    """Wheel: tyre band, both sidewalls, rim and optional spokes; axis = axle direction."""
    az = norm(axis); half = mul(az, width / 2)
    tube(p, add(centre, mul(half, -1)), add(centre, half), r_out, tread, seg=seg, caps=False)
    ring_disc(p, add(centre, half), az, r_in, r_out, tread, seg)
    ring_disc(p, add(centre, mul(half, -1)), mul(az, -1), r_in, r_out, tread, seg)
    tube(p, add(centre, mul(half, -.8)), add(centre, mul(half, .8)), r_in, rim, seg=seg, caps=False)
    ring_disc(p, add(centre, mul(half, .6)), az, 0, r_in, rim if not spokes else 'GInt_Oil', seg)
    ring_disc(p, add(centre, mul(half, -.6)), mul(az, -1), 0, r_in, rim if not spokes else 'GInt_Oil', seg)
    tube(p, add(centre, mul(half, -1.05)), add(centre, mul(half, 1.05)), r_in * .22, rim, seg=10)
    if spokes:
        ref = (0, 1, 0) if abs(az[1]) < .9 else (1, 0, 0)
        ax = norm(cross(ref, az)); ay = cross(az, ax)
        for k in range(spokes):
            t = 2 * math.pi * k / spokes
            d = add(mul(ax, math.cos(t)), mul(ay, math.sin(t)))
            beam(p, add(centre, mul(d, r_in * .2)), add(centre, mul(d, r_in * .98)), .03, .025, spoke_mat, up=az)


def poly_face(p, pts, n, mat, uvs=None):
    """Convex polygon fan."""
    for i in range(1, len(pts) - 1):
        p.faces.append(([tuple(pts[0]), tuple(pts[i]), tuple(pts[i + 1])], tuple(n), mat,
                        None if uvs is None else [uvs[0], uvs[i], uvs[i + 1]]))


def wall_point(w, a, y, side):
    """Point on a wall face: along-axis a, height y, side +1/-1 (face toward +c / -c)."""
    c = w['c'] + side * w['t'] / 2
    return (a, y, c) if w['axis'] == 'x' else (c, y, a)


def wall_normal(w, side):
    return (0, 0, side) if w['axis'] == 'x' else (side, 0, 0)


def along(w):
    return (1, 0, 0) if w['axis'] == 'x' else (0, 0, 1)


# ---------------------------------------------------------------------------
# Materials by wall group
# ---------------------------------------------------------------------------
def finish_mat(w, finish, side=0):
    g = w.get('faceGroups', {}).get('pos' if side > 0 else 'neg', w['group']) if side else w['group']
    if g == 'hall':
        return 'GExt_Rubble' if finish == 'ext' else 'GInt_Lime'
    if g == 'office':
        return 'GExt_Cream' if finish == 'ext' else 'GInt_Plaster'
    if g == 'store':
        return 'GInt_Plank'
    return 'GExt_Boards'


def finish_part(w, finish):
    return part('Ext_Facade') if finish == 'ext' else part('Int_Walls')


# ---------------------------------------------------------------------------
# Shell walls (column cut shared with the Unity colliders) and segmental arches
# ---------------------------------------------------------------------------
def arch_geometry(o):
    w = o['width']; r = o['rise']
    R = (w * w / 4 + r * r) / (2 * r)
    yc = o['top'] - R
    return R, yc


def arch_y(o, a):
    R, yc = arch_geometry(o)
    return yc + math.sqrt(max(0.0, R * R - (a - o['at']) ** 2))


def build_walls(L):
    for w in L['walls']:
        if w['kind'] != 'wall':
            continue
        own = [o for o in L['openings'] if o['wall'] == w['id']]
        pieces = K.wall_pieces(w, own)
        normal_pos = '+z' if w['axis'] == 'x' else '+x'
        normal_neg = '-z' if w['axis'] == 'x' else '-x'
        ends_keys = ('+x', '-x') if w['axis'] == 'x' else ('+z', '-z')
        ext = 'ext' in (w['pos'], w['neg'])
        for (a0, a1, y0, y1) in pieces:
            x0, yy0, z0, x1, yy1, z1 = K.wall_box(w, a0, a1, y0, y1)
            for key, finish, side in ((normal_pos, w['pos'], 1), (normal_neg, w['neg'], -1)):
                if finish == 'none':
                    continue
                bbox(finish_part(w, finish), x0, yy0, z0, x1, yy1, z1, finish_mat(w, finish, side), faces=key)
            # Reveals: heavy dressed stone in the hall, plaster/boards elsewhere.
            rmat = 'GExt_Ashlar' if (w['group'] == 'hall' and ext) else finish_mat(w, 'ext' if ext else 'int')
            rp = part('Ext_Facade') if ext else part('Int_Walls')
            ends = []
            inner0 = a0 > w['a0'] + 1e-6 and any(abs(q[1] - a0) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces)
            inner1 = a1 < w['a1'] - 1e-6 and any(abs(q[0] - a1) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces)
            if not inner0 and not (abs(a0 - w['a0']) < 1e-6 and 'a0' in w['buried']):
                ends.append(ends_keys[1])
            if not inner1 and not (abs(a1 - w['a1']) < 1e-6 and 'a1' in w['buried']):
                ends.append(ends_keys[0])
            # The reveal at a wall end uses that wall's own finish (wall corners), openings use rmat.
            for key in ends:
                at_end = (key == ends_keys[1] and abs(a0 - w['a0']) < 1e-6) or (key == ends_keys[0] and abs(a1 - w['a1']) < 1e-6)
                m = finish_mat(w, 'ext' if ext else 'int') if at_end else rmat
                bbox(rp, x0, yy0, z0, x1, yy1, z1, m, faces=[key])
            # Sill tops and lintel soffits of openings; wall heads under the roofs are hidden.
            hidden_top = abs(y1 - w['y1']) < 1e-6
            bbox(rp, x0, yy0, z0, x1, yy1, z1, rmat, faces=([] if hidden_top else ['+y']) + (['-y'] if y0 > w['y0'] + 1e-6 else []))
        for o in own:
            if o.get('rise'):
                arch_fill(w, o)


def arch_fill(w, o, seg=10):
    """Spandrels between the segmental arch and the rectangular cut, on both faces, plus the
    curved soffit through the wall. Collision keeps the rectangle (spandrels are overhead)."""
    lo, hi, top = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['top']
    pts = [(lo + (hi - lo) * i / seg, None) for i in range(seg + 1)]
    pts = [(a, arch_y(o, a)) for a, _ in pts]
    ext = 'ext' in (w['pos'], w['neg'])
    for side, finish in ((1, w['pos']), (-1, w['neg'])):
        if finish == 'none':
            continue
        m = finish_mat(w, finish, side); p = finish_part(w, finish); n = wall_normal(w, side)
        for (a0, y0), (a1, y1) in zip(pts, pts[1:]):
            p.quad([wall_point(w, a0, y0, side), wall_point(w, a1, y1, side), wall_point(w, a1, top, side), wall_point(w, a0, top, side)], n, m)
    smat = 'GExt_Ashlar' if (w['group'] == 'hall' and ext) else finish_mat(w, 'int')
    sp = part('Ext_Facade') if ext else part('Int_Walls')
    R, yc = arch_geometry(o)
    for (a0, y0), (a1, y1) in zip(pts, pts[1:]):
        mid = (a0 + a1) / 2; ny = -(arch_y(o, mid) - yc) / R; na = -(mid - o['at']) / R
        n = tuple(x * na for x in along(w)); n = (n[0], ny, n[2])
        sp.quad([wall_point(w, a0, y0, -1), wall_point(w, a1, y1, -1), wall_point(w, a1, y1, 1), wall_point(w, a0, y0, 1)], n, smat)


def stone_surround(w, o, side, ring=.32, proud=.04):
    """Voussoirs around a segmental head, a keystone and alternating jamb blocks (hall exterior)."""
    p = part('Ext_Facade'); n = wall_normal(w, side)
    face = w['c'] + side * w['t'] / 2
    def P(a, y, d):
        return (a, y, face + side * d) if w['axis'] == 'x' else (face + side * d, y, a)
    R, yc = arch_geometry(o)
    half = math.asin(min(1, (o['width'] / 2) / R))
    count = max(7, int(o['width'] / .22) | 1)
    for i in range(count):
        t0 = -half + 2 * half * i / count; t1 = -half + 2 * half * (i + 1) / count
        key = i == count // 2
        r1 = R + ring + (.08 if key else 0)
        q = lambda t, r: (o['at'] + r * math.sin(t), yc + r * math.cos(t))
        g = .006
        a, b, c2, d = q(t0 + g / R, R), q(t1 - g / R, R), q(t1 - g / R, r1), q(t0 + g / R, r1)
        dep = proud + (.03 if key else 0)
        p.quad([P(*a, dep), P(*b, dep), P(*c2, dep), P(*d, dep)], n, 'GExt_Ashlar')
        # Outer edge, inner (soffit) edge and the two joint sides of the stone.
        mid_t = (t0 + t1) / 2; radial = (math.sin(mid_t), math.cos(mid_t))
        def edge(e0, e1, outward):
            v = [P(*e0, 0), P(*e1, 0), P(*e1, dep), P(*e0, dep)]
            ax = along(w)
            nn = (ax[0] * outward[0], outward[1], ax[2] * outward[0])
            p.quad(v, nn, 'GExt_Ashlar')
        edge(d, c2, radial)
        edge(a, b, (-radial[0], -radial[1]))
        edge(a, d, (-math.cos(t0), math.sin(t0)))
        edge(c2, b, (math.cos(t1), -math.sin(t1)))
    # Jamb blocks from the sill (or floor) up to the spring line.
    spring = o['top'] - o['rise']
    y = o['bottom'] if o['kind'] == 'window' else max(o['bottom'], 0)
    k = 0
    while y < spring - .05:
        h = min(.3, spring - y)
        long_ = .34 if k % 2 == 0 else .22
        for s in (-1, 1):
            edge_a = o['at'] + s * o['width'] / 2
            a0, a1 = sorted((edge_a, edge_a + s * long_))
            x0, yy0, z0, x1, yy1, z1 = (a0, y + .005, 0, a1, y + h - .005, 0)
            if w['axis'] == 'x':
                zz0, zz1 = sorted((face, face + side * proud))
                bbox(p, a0, y + .005, zz0, a1, y + h - .005, zz1, 'GExt_Ashlar', faces=['+z' if side > 0 else '-z', '+y', '-y', '+x' if s > 0 else '-x'])
            else:
                xx0, xx1 = sorted((face, face + side * proud))
                bbox(p, xx0, y + .005, a0, xx1, y + h - .005, a1, 'GExt_Ashlar', faces=['+x' if side > 0 else '-x', '+y', '-y', '+z' if s > 0 else '-z'])
        y += h; k += 1
    if o['kind'] == 'window':
        lo, hi = o['at'] - o['width'] / 2 - .1, o['at'] + o['width'] / 2 + .1
        if w['axis'] == 'x':
            zz0, zz1 = sorted((face, face + side * .09))
            bbox(p, lo, o['bottom'] - .1, zz0, hi, o['bottom'] - .004, zz1, 'GExt_Ashlar', faces=['+y', '-y', '+z' if side > 0 else '-z', '+x', '-x'])
        else:
            xx0, xx1 = sorted((face, face + side * .09))
            bbox(p, xx0, o['bottom'] - .1, lo, xx1, o['bottom'] - .004, hi, 'GExt_Ashlar', faces=['+y', '-y', '+x' if side > 0 else '-x', '+z', '-z'])


def build_gables(L):
    """Stone gable triangles above the hall's end walls, up under the roof sarking; the front
    gable carries the high arched window that lights the open roof space."""
    lv = L['levels']; h = L['hall']; e = lv['eaves']; S = lv['slope']; rx = lv['ridgeX']
    lift = .03
    def top_y(x):
        return lv['ridge'] - S * abs(x - rx) - lv['roofThickness'] + lift
    xa = h['x0'] + (lv['roofThickness'] - lift) / S; xb = h['x1'] - (lv['roofThickness'] - lift) / S
    apex = top_y(rx)
    def half_at(y):
        return (apex - y) / S
    gw = L['gableWindow']
    for wid, c, ext_side, hole in (('gable-S', h['z0'] + .275, -1, gw), ('gable-N', h['z1'] - .275, 1, None)):
        w = dict(id=wid, axis='x', c=c, t=.55, a0=h['x0'], a1=h['x1'], y0=e, y1=apex, pos='int' if ext_side < 0 else 'ext',
                 neg='ext' if ext_side < 0 else 'int', group='hall', kind='wall', buried=[])
        bands = []
        if hole:
            lo, hi = hole['at'] - hole['width'] / 2, hole['at'] + hole['width'] / 2
            b, t = hole['bottom'], hole['top']
            bands.append([(xa, e), (xb, e), (rx + half_at(b), b), (rx - half_at(b), b)])
            bands.append([(rx - half_at(b), b), (lo, b), (lo, t), (rx - half_at(t), t)])
            bands.append([(hi, b), (rx + half_at(b), b), (rx + half_at(t), t), (hi, t)])
            bands.append([(rx - half_at(t), t), (rx + half_at(t), t), (rx, apex)])
        else:
            bands.append([(xa, e), (xb, e), (rx, apex)])
        for side, finish in ((1, w['pos']), (-1, w['neg'])):
            m = finish_mat(w, finish); p = finish_part(w, finish); n = wall_normal(w, side)
            for band in bands:
                pts = [wall_point(w, a, y, side) for a, y in band]
                poly_face(p, pts, n, m)
        if hole:
            o = dict(hole, kind='window', bottom=hole['bottom'])
            lo, hi, b, t = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom'], o['top']
            rp = part('Ext_Facade')
            z0, z1 = c - .275, c + .275
            bbox(rp, lo, b, z0, lo + .001, t, z1, 'GExt_Ashlar', faces=['+x'])
            bbox(rp, hi - .001, b, z0, hi, t, z1, 'GExt_Ashlar', faces=['-x'])
            bbox(rp, lo, b - .001, z0, hi, b, z1, 'GExt_Ashlar', faces=['+y'])
            arch_fill(w, o)
            stone_surround(w, o, ext_side)
            build_iron_window(w, o)


# ---------------------------------------------------------------------------
# Fences, balustrades
# ---------------------------------------------------------------------------
def build_fences_and_rails(L):
    for w in L['walls']:
        if w['kind'] == 'fence':
            own = [o for o in L['openings'] if o['wall'] == w['id']]
            p = part('Ext_Yard'); x_axis = w['axis'] == 'x'
            a = w['a0']; width = .15
            cuts = [(o['at'] - o['width'] / 2, o['at'] + o['width'] / 2) for o in own]
            while a < w['a1'] - .02:
                b = min(a + width - .012, w['a1'])
                if not any(lo - .01 < (a + b) / 2 < hi + .01 for lo, hi in cuts):
                    top = w['y1'] - (.04 if int(a / width) % 2 else 0)
                    if x_axis:
                        bbox(p, a, -.05, w['c'] - .012, b, top, w['c'] + .012, 'GExt_Boards')
                    else:
                        bbox(p, w['c'] - .012, -.05, a, w['c'] + .012, top, b, 'GExt_Boards')
                a += width
            # Rails on the yard side and posts every 2.4 m (rails skip the gate).
            spans = []; s = w['a0']
            for lo, hi in sorted(cuts):
                spans.append((s, lo)); s = hi
            spans.append((s, w['a1']))
            side = -1 if w['id'] in ('Y_N', 'Y_E') else 1
            for s0, s1 in spans:
                for y in (.35, 1.45):
                    if x_axis:
                        z0, z1 = sorted((w['c'] + side * .012, w['c'] + side * .07))
                        bbox(p, s0, y, z0, s1, y + .09, z1, 'GExt_Timber')
                    else:
                        x0, x1 = sorted((w['c'] + side * .012, w['c'] + side * .07))
                        bbox(p, x0, y, s0, x1, y + .09, s1, 'GExt_Timber')
                k = s0
                while k <= s1 + .01:
                    kk = min(max(k, s0 + .05), s1 - .05)
                    if x_axis:
                        bbox(p, kk - .05, -.05, w['c'] + side * .012 - .05, kk + .05, w['y1'] + .05, w['c'] + side * .012 + .05, 'GExt_Timber')
                    else:
                        bbox(p, w['c'] + side * .012 - .05, -.05, kk - .05, w['c'] + side * .012 + .05, w['y1'] + .05, kk + .05, 'GExt_Timber')
                    k += 2.4
        elif w['kind'] == 'rail':
            p = part('Int_Loft'); x_axis = w['axis'] == 'x'
            y0, y1 = w['y0'], w['y1']
            def rbox(a0, a1, b0, b1, mat='PInt_FurnLight', d=.05):
                if x_axis:
                    bbox(p, a0, b0, w['c'] - d / 2, a1, b1, w['c'] + d / 2, mat)
                else:
                    bbox(p, w['c'] - d / 2, b0, a0, w['c'] + d / 2, b1, a1, mat)
            rbox(w['a0'], w['a1'], y1 - .06, y1, d=.07)
            rbox(w['a0'], w['a1'], y0 + .45, y0 + .5)
            n = max(2, int((w['a1'] - w['a0']) / 1.1) + 1)
            for i in range(n + 1):
                a = w['a0'] + (w['a1'] - w['a0']) * i / n
                rbox(max(w['a0'], a - .04), min(w['a1'], a + .04), y0, y1, d=.08)
            k = w['a0'] + .12
            while k < w['a1'] - .1:
                rbox(k - .015, k + .015, y0 + .5, y1 - .06, d=.03)
                k += .14


# ---------------------------------------------------------------------------
# Floors, ceilings, loft, exterior paving
# ---------------------------------------------------------------------------
FLOOR_MAT = {'oak': 'PInt_Oak', 'cement': 'GInt_Cement', 'boards': 'GInt_Plank'}


def build_slabs(L):
    for s in L['slabs']:
        k = s['kind']
        if k == 'finish':
            bbox(part('Int_Floors'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], FLOOR_MAT[s['top']], faces=['+y'])
        elif k == 'ceiling':
            bbox(part('Int_Ceilings'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'GInt_Ceiling', faces=['-y'])
        elif k == 'loft':
            p = part('Int_Loft')
            bbox(p, s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'GInt_Plank', faces=['+y'])
            bbox(p, s['x0'], s['y0'], s['z0'], s['x1'], s['y0'] + .03, s['z1'], 'GInt_Plank', faces=['-y'])
            bbox(p, s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'PInt_FurnLight', faces=['-z', '+x'])
            # Joists under the boards (seen from the tool store), running across the store.
            z = s['z0'] + .25
            while z < s['z1'] - .1:
                bbox(p, s['x0'], s['y0'] - .16, z - .04, s['x1'] - .1, s['y0'], z + .04, 'PInt_FurnLight', faces=['-y', '+z', '-z', '+x'])
                z += .6
        elif k in ('paving', 'apron', 'step', 'yard'):
            mat = {'paving': 'GExt_Flags', 'apron': 'GExt_Ashlar', 'step': 'GExt_Ashlar', 'yard': 'GExt_Cobble'}[k]
            faces = ['+y', '-z', '+x', '-x', '+z']
            if s['id'] == 'yard-side':
                faces = ['+y']
            elif s['id'] == 'yard-back':
                faces = ['+y', '+z', '+x']
            elif k == 'paving':
                faces = ['+y', '-z', '+x']
            bbox(part('Ext_Ground'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], mat, faces=faces)


def build_thresholds(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        if o['kind'] != 'door' or W[o['wall']]['kind'] != 'wall':
            continue
        w = W[o['wall']]
        lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
        grow = .05 if o['exterior'] else .02
        x0, y0, z0, x1, y1, z1 = K.wall_box(w, lo + .001, hi - .001, o['bottom'] - .03, o['bottom'] + .006, grow)
        bbox(part('Ext_Facade' if o['exterior'] else 'Int_Trim'), x0, y0, z0, x1, y1, z1,
             'GExt_Ashlar' if o['exterior'] else 'PInt_Panel', faces=['+y', '+x', '-x', '+z', '-z'])


def build_stairs(L):
    st = part('Int_Stairs'); g = L['levels']['ground']
    for s in L['stairs']:
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'], s['z1'], 'GInt_Plank', faces=['+y'])
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'] - .02, s['z1'], 'PInt_FurnLight', faces=['+x', '-z'])
        bbox(st, s['x0'], s['top'] - .04, s['z0'] - .025, s['x1'], s['top'], s['z0'], 'GInt_Plank', faces=['+y', '-z', '-y', '+x'])
    # Sloped string, handrail and balusters on the open east side.
    first, last = L['stairs'][0], L['stairs'][-1]
    x = first['x1'] + .03
    a = (x, first['top'] + .9, first['z0']); b = (x, last['top'] + .9, last['z1'])
    beam(st, a, b, .06, .06, 'PInt_FurnLight', up=(0, 1, 0))
    beam(st, (x, first['top'] - .1, first['z0']), (x, last['top'] - .1, last['z1']), .05, .22, 'PInt_FurnLight')
    for s in L['stairs']:
        zc = (s['z0'] + s['z1']) / 2
        bbox(st, x - .015, s['top'], zc - .015, x + .015, s['top'] + .9, zc + .015, 'PInt_FurnLight')
    bbox(st, x - .04, first['top'] - .2, first['z0'], x + .04, first['top'] + 1.0, first['z0'] + .08, 'PInt_FurnLight')


# ---------------------------------------------------------------------------
# Roofs: hall with three timber cross gables, office lean-to, shed and awning
# ---------------------------------------------------------------------------
def slope_y(L, x):
    lv = L['levels']
    return lv['ridge'] - lv['slope'] * abs(x - lv['ridgeX'])


def roof_poly(L, pts, underside=False, part_top='Ext_Roof', part_under='Int_Roof'):
    """Convex polygon on the hall roof plane (x,z list): slate top and boarded sarking below."""
    lv = L['levels']; S = lv['slope']; rx = lv['ridgeX']
    west = all(x <= rx + 1e-6 for x, _ in pts)
    n = (-S, 1, 0) if west else (S, 1, 0); n = norm(n)
    top = [(x, slope_y(L, x), z) for x, z in pts]
    alongv = (0, 0, 1) if west else (0, 0, -1)
    up_dir = norm((S, 1, 0) if west else (-S, 1, 0))
    K.slope_quad(part(part_top), top, n, 'GExt_Slate', alongv, up_dir, 1.5) if len(top) == 4 else poly_face(part(part_top), top, n, 'GExt_Slate')
    if underside:
        bottom = [(x, y - lv['roofThickness'], z) for x, y, z in top][::-1]
        poly_face(part(part_under), bottom, (-n[0], -n[1], -n[2]), 'GInt_Plank')


def build_hall_roof(L):
    lv = L['levels']; r = next(r for r in L['roofs'] if r['id'] == 'roof-hall')
    rx, S, T = lv['ridgeX'], lv['slope'], lv['roofThickness']
    x0, x1, z0, z1 = r['x0'], r['x1'], r['z0'], r['z1']
    face = L['hall']['x1']
    # West slope in one piece.
    roof_poly(L, [(x0, z0), (rx, z0), (rx, z1), (x0, z1)], True)
    # East slope: continuous eave strip beyond the wall face, then bands with the dormer holes.
    roof_poly(L, [(face, z0), (x1, z0), (x1, z1), (face, z1)], True)
    edges = [z0]
    for d in sorted(L['dormers'], key=lambda d: d['z']):
        za, zb, vx = d['z'] - d['half'], d['z'] + d['half'], d['valleyX']
        roof_poly(L, [(rx, edges[-1]), (face, edges[-1]), (face, za), (rx, za)], True)
        roof_poly(L, [(rx, za), (face, za), (vx, d['z']), (rx, d['z'])], True)
        roof_poly(L, [(rx, d['z']), (vx, d['z']), (face, zb), (rx, zb)], True)
        edges.append(zb)
    roof_poly(L, [(rx, edges[-1]), (face, edges[-1]), (face, z1), (rx, z1)], True)
    rp = part('Ext_Roof')
    # Edges: eaves fascias, gable verges and a ridge roll.
    for x in (x0, x1):
        y = slope_y(L, x); n = (-1 if x < rx else 1, 0, 0)
        rp.quad([(x, y - T, z0), (x, y - T, z1), (x, y, z1), (x, y, z0)], n, 'GExt_Timber')
    for z, nz in ((z0, -1), (z1, 1)):
        for xa, xb in ((x0, rx), (rx, x1)):
            ya, yb = slope_y(L, xa), slope_y(L, xb)
            rp.quad([(xa, ya - T - .06, z), (xb, yb - T - .06, z), (xb, yb + .02, z), (xa, ya + .02, z)], (0, 0, nz), 'GExt_Timber')
    beam(rp, (rx, lv['ridge'] + .03, z0 - .02), (rx, lv['ridge'] + .03, z1 + .02), .2, .1, 'GExt_Slate2')
    # Gutters and downpipes on both eaves.
    for x, sx in ((x0, -1), (x1, 1)):
        y = slope_y(L, x)
        bbox(rp, min(x, x + sx * .14), y - .22, z0, max(x, x + sx * .14), y - .1, z1, 'GExt_Gutter')
        for z in (z0 + .5, z1 - .5):
            cylinder(rp, x + sx * .07, z, .045, 0.0, y - .15, 'GExt_Gutter', seg=8)


def build_dormers(L):
    lv = L['levels']; T = lv['roofThickness']; e = lv['eaves']
    for d in L['dormers']:
        zc, hw, apex, face, vx, front = d['z'], d['half'], d['apex'], d['face'], d['valleyX'], d['front']
        sd = (apex - e) / hw
        rp = part('Ext_Roof'); ip = part('Int_Roof')
        for sgn in (-1, 1):
            ze = zc + sgn * (hw + .15)
            ye = apex - sd * (hw + .15)
            pts = [(front, ye, ze), (front, apex, zc), (vx, apex, zc), (face, e, zc + sgn * hw)]
            n = norm((0, 1, sgn * sd))   # plane y = apex - sd*|z - zc|: tilts toward its own eave
            poly_face(rp, pts, n, 'GExt_Slate')
            poly_face(ip, [(x, y - T, z) for x, y, z in pts][::-1], (0, -n[1], -n[2]), 'GInt_Plank')
            rp.quad([(front, ye - T, ze), (front, apex - T, zc), (front, apex, zc), (front, ye, ze)], (1, 0, 0), 'GExt_Timber')
            # Barge board along the verge.
            beam(rp, (front + .03, ye - .05, ze), (front + .03, apex - .05, zc), .04, .22, 'GExt_Green', up=(1, 0, 0))
        beam(rp, (vx - .05, apex + .03, zc), (front + .03, apex + .03, zc), .16, .08, 'GExt_Slate2')
        # Boarded gable on the wall head: outer boards with a louvred vent, plain boards inside.
        gt = .12; xo, xi = face + .005, face - gt
        tri_pts = lambda x: [(x, e, zc - hw), (x, e, zc + hw), (x, apex - T + .02, zc)]
        a, b, c = tri_pts(xo); poly_face(part('Ext_Facade'), [a, b, c], (1, 0, 0), 'GExt_Boards')
        a, b, c = tri_pts(xi); poly_face(part('Int_Walls'), [c, b, a], (-1, 0, 0), 'GExt_Boards')
        fp = part('Ext_Facade')
        for k in range(5):
            y = 5.05 + k * .09
            bbox(fp, xo, y, zc - .22, xo + .035, y + .05, zc + .22, 'GExt_Timber', faces=['+x', '+y', '-y', '+z', '-z'])
        bbox(fp, xo, 5.0, zc - .27, xo + .045, 5.52, zc - .22, 'GExt_Timber', faces=['+x', '+z', '-z'])
        bbox(fp, xo, 5.0, zc + .22, xo + .045, 5.52, zc + .27, 'GExt_Timber', faces=['+x', '+z', '-z'])
        bbox(fp, xo, e - .02, zc - hw, xo + .06, e + .1, zc + hw, 'GExt_Timber', faces=['+x', '+y', '+z', '-z'])


def build_other_roofs(L):
    for r in L['roofs']:
        if r['id'] == 'roof-office':
            p = part('Ext_Roof'); x0, x1, z0, z1, lo, hi = r['x0'], r['x1'], r['z0'], r['z1'], r['eaves'], r['ridge']
            slope = (hi - lo) / (z1 - z0); n = norm((0, 1, slope))
            K.slope_quad(p, [(x0, hi, z0), (x1, hi, z0), (x1, lo, z1), (x0, lo, z1)][::-1][::-1], n, 'GExt_Tin', (1, 0, 0), norm((0, slope, -1)), 2.0)
            p.quad([(x0, lo - .08, z1), (x1, lo - .08, z1), (x1, lo, z1), (x0, lo, z1)], (0, 0, 1), 'GExt_Timber')
            zin = 11.65
            yin = hi - slope * (zin - z0)
            p.quad([(x0, yin - .08, zin), (x1, yin - .08, zin), (x1, lo - .08, z1), (x0, lo - .08, z1)][::-1], (0, -1, 0), 'GExt_Boards')
            bbox(p, x0, lo - .2, z1, x1, lo - .08, z1 + .14, 'GExt_Gutter')
            cylinder(p, x0 + .25, z1 + .07, .045, 0.0, lo - .12, 'GExt_Gutter', seg=8)
        elif r['id'] in ('roof-shed', 'roof-awning'):
            p = part('Ext_Yard' if r['id'] == 'roof-shed' else 'Ext_Facade')
            x0, x1, z0, z1, lo, hi = r['x0'], r['x1'], r['z0'], r['z1'], r['eaves'], r['ridge']
            if r['low'] > 0:   # high at z0
                za, zb = z0, z1
            else:
                za, zb = z1, z0
            s = (hi - lo) / abs(zb - za); dz = 1 if zb > za else -1
            n = norm((0, 1, s * dz))
            top = [(x0, hi, za), (x1, hi, za), (x1, lo, zb), (x0, lo, zb)]
            poly_face(p, top if dz > 0 else top[::-1], n, 'GExt_Tin')
            under = [(x, y - .06, z) for x, y, z in top]
            poly_face(p, under[::-1] if dz > 0 else under, (0, -n[1], -n[2]), 'GExt_Boards')
            p.quad([(x0, lo - .06, zb), (x1, lo - .06, zb), (x1, lo, zb), (x0, lo, zb)], (0, 0, dz), 'GExt_Timber')
            for x, nx in ((x0, -1), (x1, 1)):
                p.quad([(x, hi - .06, za), (x, lo - .06, zb), (x, lo, zb), (x, hi, za)], (nx, 0, 0), 'GExt_Timber')
            if r['id'] == 'roof-awning':
                for x in (x0 + .25, (x0 + x1) / 2, x1 - .25):
                    beam(p, (x, hi - .55, 0.0), (x, lo - .03, zb + .12), .07, .07, 'GExt_Timber', up=(1, 0, 0))
                    bbox(p, x - .04, hi - .65, -.08, x + .04, hi - .06, 0.0, 'GExt_Timber', faces=['-z', '+x', '-x', '-y'])
            else:
                bbox(p, x0, lo - .26, 22.68, x1, lo - .06, 22.82, 'GExt_Timber')
                for post in L['posts']:
                    bbox(p, post['x'] - post['w'] / 2, post['y0'], post['z'] - post['w'] / 2, post['x'] + post['w'] / 2, post['y1'], post['z'] + post['w'] / 2, 'GExt_Timber')
                    bbox(p, post['x'] - .14, post['y0'], post['z'] - .14, post['x'] + .14, post['y0'] + .12, post['z'] + .14, 'GExt_Ashlar')
                # Ledger against the hall wall carrying the rafters.
                bbox(p, x0, hi - .3, 19.0, x1, hi - .08, 19.12, 'GExt_Timber', faces=['+z', '-y', '+x', '-x'])


def build_trusses(L):
    lv = L['levels']; h = L['hall']; T = lv['roofThickness']; rx = lv['ridgeX']; S = lv['slope']
    tp = part('Int_Trusses')
    xin0, xin1 = h['x0'] + .55, h['x1'] - .55
    under = lambda x: slope_y(L, x) - T
    for t in L['trusses']:
        z = t['z']; w = t['w']
        bbox(tp, xin0 - .2, t['tieY0'], z - w / 2, xin1 + .2, t['tieY1'], z + w / 2, 'GExt_Timber')
        for xs in (xin0 - .1, xin1 + .1):
            sgn = 1 if xs < rx else -1
            a = (xs, t['tieY1'] - .02, z); b = (rx - sgn * .1, under(rx) - .14, z)
            beam(tp, a, b, w * .9, .22, 'GExt_Timber', up=(0, 1, 0))
            # Raking strut from the king post foot to mid-rafter.
            mx = (xs + rx) / 2
            beam(tp, (rx - sgn * .1, t['tieY1'] + .3, z), (mx, under(mx) - .22, z), w * .7, .14, 'GExt_Timber')
        bbox(tp, rx - .1, t['tieY1'], z - w / 2, rx + .1, under(rx) - .02, z + w / 2, 'GExt_Timber')
        for xs in (xin0 + .35, xin1 - .35):
            bbox(tp, xs - .02, t['tieY0'] - .06, z - w / 2 - .01, xs + .02, t['tieY1'] + .06, z + w / 2 + .01, 'GExt_Iron')
    # Purlins, ridge beam and the wedge wall plates under the sarking.
    z0, z1 = h['z0'] + .55, h['z1'] - .55
    for x in (8.4, 14.2):
        top = min(under(x - .09), under(x + .09)) - .002
        bbox(tp, x - .09, top - .22, z0, x + .09, top, z1, 'GExt_Timber', faces=['-y', '+x', '-x', '+z', '-z'])
    bbox(tp, rx - .1, under(rx) - .26, z0, rx + .1, under(rx + .1) - .002, z1, 'GExt_Timber', faces=['-y', '+x', '-x'])
    for xo, xi in ((h['x0'], h['x0'] + .55), (h['x1'], h['x1'] - .55)):
        # Wall plate wedge closing the slot between the wall head and the sarking:
        # cross-section (xa, eaves) -> (xi, eaves) -> (xi, under(xi)); the slope face is buried.
        sgn = 1 if xi > xo else -1
        xa = xo + sgn * T / S
        ya = lv['eaves']; yi = under(xi)
        tp.quad([(xi, ya, z0), (xi, ya, z1), (xi, yi, z1), (xi, yi, z0)], (sgn, 0, 0), 'GExt_Timber')
        for z, nz in ((z0, -1), (z1, 1)):
            tri(tp, [(xa, ya, z), (xi, ya, z), (xi, yi, z)], (0, 0, nz), 'GExt_Timber')


# ---------------------------------------------------------------------------
# Exterior details
# ---------------------------------------------------------------------------
def build_exterior_details(L):
    fac = part('Ext_Facade'); h = L['hall']; o_ = L['office']; lv = L['levels']
    W = {w['id']: w for w in L['walls']}
    # Quoins on the free hall corners.
    for cx, cz, sx, sz in ((h['x1'], h['z0'], 1, -1), (h['x1'], h['z1'], 1, 1), (h['x0'], h['z1'], -1, 1)):
        y, k = 0.0, 0
        while y < lv['eaves'] - .1:
            hh = RNG.uniform(.26, .34)
            lx, lz = (.48, .26) if k % 2 == 0 else (.26, .48)
            x0, x1 = sorted((cx + sx * .03, cx - sx * lx)); z0, z1 = sorted((cz + sz * .03, cz - sz * lz))
            bbox(fac, x0, y, z0, x1, min(y + hh, lv['eaves']) - .006, z1, 'GExt_Ashlar', faces=['+x' if sx > 0 else '-x', '+z' if sz > 0 else '-z', '+y', '-y'])
            y += hh; k += 1
    # Stone surrounds of the hall's arched openings (outside faces).
    for o in L['openings']:
        w = W[o['wall']]
        if w['group'] == 'hall' and o['exterior'] and o.get('rise'):
            side = 1 if w['pos'] == 'ext' else -1
            stone_surround(w, o, side)
    # Office range: parapet coping, painted fascia board, plinth, window sill and door hood.
    os_ = W['O_S']; face = 0.0
    bbox(fac, o_['x0'], 4.0, -.06, o_['x1'], 4.08, .62, 'GExt_Ashlar', faces=['+y', '-z', '-x', '+z'])
    bbox(fac, o_['x0'], 3.15, -.05, o_['x1'], 3.5, 0.0, 'GExt_Green', faces=['-z', '+y', '-y', '-x'])
    bbox(fac, o_['x0'], -.1, -.05, 3.2, .45, 0.0, 'GExt_Ashlar', faces=['-z', '+y', '-x', '+x'])
    bbox(fac, 4.2, -.1, -.05, o_['x1'], .45, 0.0, 'GExt_Ashlar', faces=['-z', '+y', '-x'])
    bbox(fac, -.0, 3.85, 0.62, .35, 3.92, o_['z1'] - .35, 'GExt_Ashlar', faces=['+y', '+x'])
    win = next(o for o in L['openings'] if o['id'] == 'win-office')
    bbox(fac, win['at'] - win['width'] / 2 - .08, win['bottom'] - .08, -.08, win['at'] + win['width'] / 2 + .08, win['bottom'] - .004, 0.0, 'GExt_Ashlar', faces=['+y', '-y', '-z', '+x', '-x'])
    dr = next(o for o in L['openings'] if o['id'] == 'office-door')
    for s in (-1, 1):
        a = dr['at'] + s * (dr['width'] / 2 + .06)
        bbox(fac, a - .06, .45, -.05, a + .06, dr['top'] + .1, 0.0, 'GExt_Green', faces=['-z', '+x', '-x', '+y'])
    bbox(fac, dr['at'] - dr['width'] / 2 - .14, dr['top'] + .1, -.07, dr['at'] + dr['width'] / 2 + .14, dr['top'] + .22, 0.0, 'GExt_Green', faces=['-z', '+y', '-y', '+x', '-x'])
    # Records window sill on the north wall.
    rw = next(o for o in L['openings'] if o['id'] == 'win-records')
    bbox(fac, rw['at'] - rw['width'] / 2 - .08, rw['bottom'] - .08, 12.0, rw['at'] + rw['width'] / 2 + .08, rw['bottom'] - .004, 12.08, 'GExt_Ashlar', faces=['+y', '-y', '+z', '+x', '-x'])


# ---------------------------------------------------------------------------
# Joinery
# ---------------------------------------------------------------------------
def build_iron_window(w, o):
    """Iron-framed window with small panes; segmental head following the arch."""
    jo = part('Ext_Windows'); glass = part('Glass')
    side = 1 if w['pos'] == 'ext' else -1
    plane = w['c'] + side * (w['t'] / 2 - .12)
    lo, hi, b = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom']
    spring = o['top'] - o['rise']
    fw = .045
    def P(a, y, d=0.0):
        return (a, y, plane + d) if w['axis'] == 'x' else (plane + d, y, a)
    def bar(a0, a1, y0, y1, depth=.06):
        if w['axis'] == 'x':
            bbox(jo, a0, y0, plane - depth / 2, a1, y1, plane + depth / 2, 'GExt_Iron')
        else:
            bbox(jo, plane - depth / 2, y0, a0, plane + depth / 2, y1, a1, 'GExt_Iron')
    bar(lo, lo + fw, b, spring); bar(hi - fw, hi, b, spring); bar(lo, hi, b, b + fw)
    # Segmental head bar from short straight pieces.
    seg = 8
    pts = [(lo + (hi - lo) * i / seg, arch_y(o, lo + (hi - lo) * i / seg) - .005) for i in range(seg + 1)]
    for (a0, y0), (a1, y1) in zip(pts, pts[1:]):
        beam(jo, P(a0, y0 - fw / 2), P(a1, y1 - fw / 2), .06, fw, 'GExt_Iron', up=(0, 1, 0))
    cols = 3 if o['width'] > .85 else 2
    for i in range(1, cols):
        a = lo + (hi - lo) * i / cols
        bar(a - .012, a + .012, b + fw, arch_y(o, a) - fw, depth=.035)
    y = b + .32
    while y < spring - .05:
        bar(lo + fw, hi - fw, y - .012, y + .012, depth=.035)
        y += .32
    # Opening casement in the middle: a slightly proud frame.
    for nsgn in (1, -1):
        n = wall_normal(w, nsgn)
        rect = [P(lo + fw, b + fw), P(hi - fw, b + fw), P(hi - fw, spring), P(lo + fw, spring)]
        glass.quad(rect if nsgn > 0 else rect[::-1], n, 'G_Glass')
        fan = [P(lo + fw, spring)] + [P(a, max(spring, yv - fw)) for a, yv in pts[1:-1]] + [P(hi - fw, spring)]
        for i in range(1, len(fan) - 1):
            tri_v = [fan[0], fan[i], fan[i + 1]]
            glass.faces.append(([tuple(v) for v in (tri_v if nsgn > 0 else tri_v[::-1])], n, 'G_Glass', None))
    if o.get('exterior'):
        inner = w['c'] - side * w['t'] / 2
        if w['axis'] == 'x':
            z0, z1 = sorted((inner - side * .04, plane - side * .04))
            bbox(part('Int_Trim'), lo - .04, b, z0, hi + .04, b + .03, z1, 'GExt_Ashlar', faces=['+y', '+x', '-x', '+z', '-z'])
        else:
            x0, x1 = sorted((inner - side * .04, plane - side * .04))
            bbox(part('Int_Trim'), x0, b, lo - .04, x1, b + .03, hi + .04, 'GExt_Ashlar', faces=['+y', '+x', '-x', '+z', '-z'])


def build_plank_leaf(o, w, index, count, L):
    """Ledged plank leaf (carriage/rear/store doors, yard gate), built closed about its hinge."""
    arch = bool(o.get('rise'))
    lo, hi, b = o['at'] - o['width'] / 2 + .02, o['at'] + o['width'] / 2 - .02, o['bottom'] + .012
    span = (hi - lo) / count
    a0 = lo + index * span; a1 = a0 + span
    hinge_low = (index == 0) if count == 2 else (o['hinge'] == 'a0')
    hinge_a = (a0 + .002) if hinge_low else (a1 - .002)
    if w['kind'] == 'fence':
        plane = w['c'] + o['swing'] * .04
    else:
        plane = w['c'] + o['swing'] * (w['t'] / 2 - .07) if o['exterior'] else w['c']
    pivot = (hinge_a, b, plane) if w['axis'] == 'x' else (plane, b, hinge_a)
    name = f"Leaf_{o['id']}_{index}"
    p = part(name, pivot)
    def top_at(a):
        return (arch_y(o, a) - .03) if arch else (o['top'] - .02)
    th = {'carriage': .07, 'plank': .05, 'boards': .04}[o['style']]
    mat = {'carriage': 'GExt_Walnut', 'plank': 'GExt_Walnut' if o['exterior'] else 'PInt_Panel', 'boards': 'GExt_Boards'}[o['style']]
    def P(a, y, d):
        return (a, y, plane + d) if w['axis'] == 'x' else (plane + d, y, a)
    n_planks = max(3, round(span / .16))
    for i in range(n_planks):
        s0 = a0 + .004 + (span - .008) * i / n_planks; s1 = a0 + .004 + (span - .008) * (i + 1) / n_planks - .006
        t0, t1 = top_at(s0), top_at(s1)
        for d, nsgn in ((th / 2, 1), (-th / 2, -1)):
            pts = [P(s0, b, d), P(s1, b, d), P(s1, t1, d), P(s0, t0, d)]
            p.quad(pts if nsgn > 0 else pts[::-1], wall_normal(w, nsgn), mat)
        ax = along(w)
        p.quad([P(s0, b, -th / 2), P(s0, b, th / 2), P(s0, t0, th / 2), P(s0, t0, -th / 2)], tuple(-x for x in ax), mat)
        p.quad([P(s1, b, th / 2), P(s1, b, -th / 2), P(s1, t1, -th / 2), P(s1, t1, th / 2)], ax, mat)
        p.quad([P(s0, t0, th / 2), P(s1, t1, th / 2), P(s1, t1, -th / 2), P(s0, t0, -th / 2)], (0, 1, 0), mat)
    # Ledges and a diagonal brace on the inside face; strap hinges outside.
    inside = -o['swing'] if o['exterior'] else 1
    inside = 1 if w['kind'] == 'fence' else inside
    s0, s1 = a0 + .03, a1 - .03
    top_low = min(top_at(s0), top_at(s1))
    ys = [b + .25, (b + top_low) / 2, top_low - .3]
    for y in ys:
        a_, b_ = P(s0, y - .07, inside * (th / 2)), P(s1, y + .07, inside * (th / 2 + .04))
        bbox(p, min(a_[0], b_[0]), y - .07, min(a_[2], b_[2]), max(a_[0], b_[0]), y + .07, max(a_[2], b_[2]), mat)
    for (ya, yb) in zip(ys, ys[1:]):
        lowa, higha = (s1, s0) if hinge_low else (s0, s1)
        beam(p, P(lowa - (.04 if hinge_low else -.04), ya + .08, inside * (th / 2 + .02)), P(higha + (.04 if hinge_low else -.04), yb - .08, inside * (th / 2 + .02)), .1, .04, mat, up=wall_normal(w, inside))
    outside = -inside
    for y in ys:
        far = (s1 - .2) if hinge_low else (s0 + .2)
        a_, b_ = P(hinge_a, y - .025, outside * (th / 2)), P(far, y + .025, outside * (th / 2 + .012))
        bbox(p, min(a_[0], b_[0]), y - .025, min(a_[2], b_[2]), max(a_[0], b_[0]), y + .025, max(a_[2], b_[2]), 'GExt_Iron')
    knob = (s1 - .12) if hinge_low else (s0 + .12)
    for side_ in (-1, 1):
        c = P(knob, b + 1.05, side_ * (th / 2 + .03))
        tube(p, P(knob, b + 1.05, side_ * th / 2), c, .012, 'GExt_Iron', seg=6)
        tube(p, add(c, (0, -.05, 0)), add(c, (0, .05, 0)), .03 if o['style'] == 'carriage' else .015, 'GExt_Iron', seg=8)
    height = (o['top'] - (o['rise'] or 0) - b) if arch else (o['top'] - .02 - b)
    K.LEAVES.append(dict(object=name, opening=o['id'], fixture=o['fixture'], index=index, count=count,
                         hinge=list(pivot), lowEnd=bool(hinge_low), axis=w['axis'], plane=plane,
                         leafCentre=[(a0 + a1) / 2, b + height / 2, plane], width=span, height=height))


def build_joinery(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        w = W[o['wall']]
        if o['kind'] == 'window':
            if o['style'] == 'iron':
                build_iron_window(w, o)
            else:
                K.build_window(L, o, w)
        elif o['kind'] == 'door':
            if w['group'] in ('office', 'store') or (w['group'] == 'hall' and not o['exterior']):
                if w['group'] != 'store' and o['style'] != 'glazed':
                    K.casing(L, o, w)
                elif o['style'] == 'glazed':
                    K.casing(L, dict(o, id='office-door-casing'), dict(w, pos='int', neg='none'))
                K.jamb_lining(o, w)
            elif w['kind'] == 'wall':
                if not o.get('rise'):
                    K.jamb_lining(o, w)
            for i in range(o['leaves']):
                if o['style'] in ('carriage', 'plank', 'boards'):
                    build_plank_leaf(o, w, i, o['leaves'], L)
                else:
                    K.build_leaf(o, w, i, o['leaves'])
            if w['kind'] == 'fence':
                for s in (-1, 1):
                    a = o['at'] + s * (o['width'] / 2 + .06)
                    bbox(part('Ext_Yard'), a - .06, -.05, w['c'] - .06, a + .06, 2.0, w['c'] + .06, 'GExt_Timber')
    # Interior window boards for the office sashes come from the police window builder.


# ---------------------------------------------------------------------------
# Lamps
# ---------------------------------------------------------------------------
def build_lamps(L):
    for l in L['lamps']:
        fit = part('Lamp_' + l['id']); bulb = part('Bulb_' + l['id'])
        x, y, z = l['x'], l['y'], l['z']
        if l['kind'] == 'pendant':
            anchor = l['anchor']
            cylinder(fit, x, z, .05, anchor - .03, anchor, 'GInt_Copper', seg=10)
            tube(fit, (x, y + .14, z), (x, anchor - .03, z), .006, 'PInt_Black', seg=5, caps=False)
            frustum(fit, x, z, .24, .06, y - .02, y + .14, 'GInt_Copper', seg=14, cap=True)
            frustum(bulb, x, z, .045, .045, y - .07, y, 'G_Bulb', seg=8, cap=True)
        elif l['kind'] == 'bulb':
            tube(fit, (x, y + .1, z), (x, l['anchor'], z), .006, 'PInt_Black', seg=5, caps=False)
            cylinder(fit, x, z, .022, y + .05, y + .1, 'PInt_Brass', seg=8)
            frustum(bulb, x, z, .04, .03, y - .04, y + .05, 'G_Bulb', seg=8, cap=True)
        elif l['kind'] == 'desk':
            base_y = y - .37
            cylinder(fit, x, z, .08, base_y, base_y + .025, 'PInt_Brass', seg=12)
            bbox(fit, x - .012, base_y + .02, z - .012, x + .012, y + .02, z + .012, 'PInt_Brass')
            frustum(fit, x, z, .13, .05, y - .03, y + .07, 'PInt_Green', seg=12, cap=True)
            frustum(bulb, x, z, .028, .028, y - .02, y + .03, 'G_Bulb', seg=8, cap=True)
        elif l['kind'] == 'bracket':
            # Arm out of the east wall over the bench.
            beam(fit, (17.05, y + .25, z), (x - .25, y + .25, z), .03, .03, 'PInt_Iron')
            tube(fit, (x - .25, y + .25, z), (x - .25, y + .08, z), .01, 'PInt_Iron', seg=6)
            frustum(fit, x - .25, z, .14, .04, y - .02, y + .08, 'GInt_Copper', seg=12, cap=True)
            frustum(bulb, x - .25, z, .03, .03, y - .05, y, 'G_Bulb', seg=8, cap=True)
        elif l['kind'] == 'wall':
            if l['room'] == 'yard':
                dz = 1
            else:
                dz = -1
            wz = z - dz * .22
            bbox(fit, x - .015, y + .24, min(wz, z) - .015, x + .015, y + .27, max(wz, z) + .015, 'GExt_Iron')
            bbox(fit, x - .11, y + .2, z - .11, x + .11, y + .24, z + .11, 'GExt_Iron')
            frustum(fit, x, z, .14, .02, y + .24, y + .36, 'GExt_Iron', seg=4, cap=True)
            bbox(fit, x - .07, y - .16, z - .07, x + .07, y - .13, z + .07, 'GExt_Iron')
            for cx, cz in ((-.085, -.085), (.085, -.085), (.085, .085), (-.085, .085)):
                bbox(fit, x + cx - .01, y - .14, z + cz - .01, x + cx + .01, y + .2, z + cz + .01, 'GExt_Iron')
            bbox(bulb, x - .075, y - .13, z - .075, x + .075, y + .2, z + .075, 'G_LampGlass')


# ---------------------------------------------------------------------------
# Furniture and props
# ---------------------------------------------------------------------------
POLICE_KINDS = {'desk', 'chair', 'bench', 'coatstand', 'noticeboard', 'ledger', 'deskphone', 'filingcabinet',
                'bookshelf', 'lowcabinet', 'stove', 'desklamp'}
DRAWERS = []


def fb(f, x0, y0, z0, x1, y1, z1, mat, faces='all'):
    f.bb(x0, y0, z0, x1, y1, z1, mat, faces)


def fcyl(f, a, b, r, mat, seg=10, caps=True):
    tube(f.p, f.pt(*a), f.pt(*b), r, mat, seg, caps)


def fbeam(f, a, b, w, h, mat, up=(0, 1, 0)):
    beam(f.p, f.pt(*a), f.pt(*b), w, h, mat, up=f.dir(*up))


def ftyre(f, c, axis, r_out, r_in, width, **kw):
    tyre(f.p, f.pt(*c), f.dir(*axis), r_out, r_in, width, **kw)


def build_truck(f, F):
    """Original 1930-style light pickup (the mechanic's work truck), front toward local +z."""
    body, black, wood = 'GInt_Truck', 'PInt_Black', 'PInt_FurnLight'
    R, track, fz, rz = .36, .72, 1.3, -1.35
    for zc in (fz, rz):
        for sx in (-1, 1):
            ftyre(f, (sx * track, R, zc), (1, 0, 0), R, .25, .14, spokes=12, spoke_mat='PInt_FurnLight')
    fb(f, -.44, .42, -2.05, -.36, .56, 2.0, black); fb(f, .36, .42, -2.05, .44, .56, 2.0, black)
    fcyl(f, (-.6, R, fz), (.6, R, fz), .035, black); fcyl(f, (-.6, R, rz), (.6, R, rz), .045, black)
    # Front fenders sweeping down into the running boards.
    for sx in (-1, 1):
        x0, x1 = sorted((sx * .58, sx * .88))
        pts = []
        for k in range(9):
            t = math.radians(-15 + 165 * k / 8)
            pts.append((fz + .46 * math.cos(t), R + .46 * math.sin(t) * .9))
        for (z0, y0), (z1, y1) in zip(pts, pts[1:]):
            fbeam(f, ((x0 + x1) / 2, y0, z0), ((x0 + x1) / 2, y1, z1), x1 - x0, .03, black)
        fb(f, x0, .44, -.85, x1, .48, fz - .45, black)
        pts = []
        for k in range(9):
            t = math.radians(0 + 180 * k / 8)
            pts.append((rz + .45 * math.cos(t), R + .45 * math.sin(t) * .95))
        for (z0, y0), (z1, y1) in zip(pts, pts[1:]):
            fbeam(f, ((x0 + x1) / 2 - sx * .02, y0, z0), ((x0 + x1) / 2 - sx * .02, y1, z1), x1 - x0, .03, black)
    # Bonnet with louvres, radiator, lamps, bumper.
    fb(f, -.4, .62, .55, .4, 1.18, 1.72, body)
    for k in range(6):
        zz = .75 + k * .13
        for sx in (-1, 1):
            fb(f, sx * .4 - .006, .82, zz, sx * .4 + .006, 1.0, zz + .05, black)
    fb(f, -.43, .58, 1.72, .43, 1.26, 1.82, 'GInt_Chrome')
    fb(f, -.36, .64, 1.82, .36, 1.2, 1.84, black)
    fcyl(f, (0, 1.26, 1.77), (0, 1.33, 1.77), .035, 'GInt_Chrome')
    for sx in (-1, 1):
        fcyl(f, (sx * .58, 1.0, 1.82), (sx * .58, 1.0, 1.98), .1, 'GInt_Chrome', seg=12)
        # Unlit lens (the truck is parked; an emissive lamp glass would read as switched-on headlights).
        tube(f.p, f.pt(sx * .58, 1.0, 1.981), f.pt(sx * .58, 1.0, 1.99), .085, 'PInt_Enamel', seg=12)
        fbeam(f, (sx * .58, .92, 1.88), (sx * .2, .7, 1.8), .025, .025, black)
    fb(f, -.8, .48, 2.02, .8, .58, 2.1, black)
    # Cab: body, roof, windscreen frame and glass, side windows, seat and wheel.
    fb(f, -.68, .58, -.55, .68, 1.2, .55, body)
    fb(f, -.66, 1.2, -.55, -.62, 1.86, .55, body); fb(f, .62, 1.2, -.55, .66, 1.86, .55, body)
    fb(f, -.66, 1.2, -.55, .66, 1.86, -.5, body)
    fb(f, -.72, 1.86, -.62, .72, 1.94, .66, black)
    fb(f, -.66, 1.2, .5, .66, 1.25, .56, body); fb(f, -.66, 1.8, .5, .66, 1.86, .56, body)
    for sx in (-1, 0, 1):
        fb(f, sx * .62 - .025, 1.25, .5, sx * .62 + .025, 1.8, .56, body)
    for nz in (1, -1):
        pts = [f.pt(-.6, 1.26, .53), f.pt(.6, 1.26, .53), f.pt(.6, 1.79, .53), f.pt(-.6, 1.79, .53)]
        f.p.quad(pts if nz > 0 else pts[::-1], f.dir(0, 0, nz), 'G_Glass')
    for sx in (-1, 1):
        for nx in (1, -1):
            pts = [f.pt(sx * .64, 1.26, -.42), f.pt(sx * .64, 1.26, .42), f.pt(sx * .64, 1.78, .42), f.pt(sx * .64, 1.78, -.42)]
            f.p.quad(pts if nx * sx > 0 else pts[::-1], f.dir(nx, 0, 0), 'G_Glass')
        fb(f, sx * .68 - .01, .7, -.35, sx * .68 + .01, .72, .35, 'GInt_Chrome')
    fb(f, -.6, .58, -.5, .6, .9, -.15, 'PInt_Leather'); fb(f, -.6, .9, -.52, .6, 1.4, -.42, 'PInt_Leather')
    fbeam(f, (-.3, 1.0, .35), (-.3, 1.25, .2), .03, .03, black)
    tyre(f.p, f.pt(-.3, 1.27, .19), f.dir(0, .5, -.866), .17, .15, .025, tread=black, rim=black)
    # Bed: boarded floor and sides, tailgate, a tool box and a coil of rope.
    fb(f, -.72, .62, -2.15, .72, .7, -.6, wood)
    for sx in (-1, 1):
        fb(f, sx * .72 - .03, .7, -2.15, sx * .72 + .03, 1.1, -.6, wood)
        for k in range(3):
            fb(f, sx * .72 - .04, .72 + k * .13, -2.15, sx * .72 + .04, .74 + k * .13, -.6, black)
    fb(f, -.72, .7, -2.15, .72, 1.08, -2.1, wood)
    fb(f, -.72, .7, -.62, .72, 1.1, -.58, body)
    fb(f, -.55, .7, -1.2, .05, .98, -.85, 'GInt_Red'); fb(f, -.56, .97, -1.21, .06, 1.0, -.84, 'PInt_Black')
    tyre(f.p, f.pt(.35, .74, -1.65), f.dir(0, 1, 0), .2, .12, .06, tread='GInt_Canvas', rim='GInt_Canvas')
    fb(f, -.62, .68, -2.18, -.48, .76, -2.14, 'GInt_Red'); fb(f, .48, .68, -2.18, .62, .76, -2.14, 'GInt_Red')


def build_motorcycle(f, F):
    black, steel = 'PInt_Black', 'GInt_Steel'
    R = .33
    for zc in (.7, -.68):
        ftyre(f, (0, R, zc), (1, 0, 0), R, .27, .09, spokes=16, spoke_mat='GInt_Chrome')
        pts = [(zc + (R + .05) * math.cos(math.radians(a)), R + (R + .05) * math.sin(math.radians(a))) for a in range(10, 175, 20)]
        for (z0, y0), (z1, y1) in zip(pts, pts[1:]):
            fbeam(f, (0, y0, z0), (0, y1, z1), .13, .012, black)
    head = (0, .88, .45)
    fbeam(f, head, (0, .78, -.25), .04, .04, black)
    fbeam(f, head, (0, .38, .12), .045, .045, black)
    fbeam(f, (0, .38, .12), (0, .36, -.35), .04, .04, black)
    fbeam(f, (0, .78, -.25), (0, R, -.68), .03, .03, black)
    fbeam(f, (0, .36, -.35), (0, R, -.68), .03, .03, black)
    for sx in (-1, 1):
        fbeam(f, (sx * .07, .92, .48), (sx * .07, R, .7), .03, .03, 'GInt_Chrome')
    fb(f, -.13, .74, -.06, .13, .94, .46, 'GInt_Red')
    fb(f, -.12, .3, -.18, .12, .62, .22, steel)
    for k in range(5):
        fb(f, -.15, .46 + k * .04, -.05, .15, .475 + k * .04, .1, steel)
    fcyl(f, (.14, .32, .0), (.14, .26, -.9), .035, 'GInt_Chrome')
    fb(f, -.13, .84, -.52, .13, .92, -.2, 'PInt_Leather')
    fcyl(f, (-.36, 1.05, .55), (.36, 1.05, .55), .014, 'GInt_Chrome')
    fbeam(f, (0, .88, .45), (0, 1.05, .55), .03, .03, black)
    fcyl(f, (0, .98, .62), (0, .98, .72), .07, 'GInt_Chrome')
    # Centre stand on the floor.
    for sx in (-1, 1):
        fbeam(f, (sx * .1, .3, -.25), (sx * .14, .0, -.35), .025, .025, black)


def build_hoist(F):
    """Manual chain hoist hanging from the truss tie beam above the truck's engine bay."""
    p = part('Furn_' + F['id'], (F['x'], F['y'], F['z']))
    x, z, top = F['x'], F['z'], F['y'] + F['h']
    bbox(p, x - .06, top - .1, z - .14, x + .06, top, z + .14, 'PInt_Iron')
    tube(p, (x, top - .1, z), (x, top - .22, z), .03, 'GInt_Chain', seg=6)
    tube(p, (x - .07, top - .42, z), (x + .07, top - .42, z), .16, 'GInt_Red', seg=14)
    bbox(p, x - .1, top - .62, z - .1, x + .1, top - .5, z + .1, 'PInt_Iron')
    y = top - .62
    k = 0
    while y > F['y'] + .25:
        if k % 2:
            bbox(p, x - .025, y - .07, z - .006, x + .025, y, z + .006, 'GInt_Chain')
        else:
            bbox(p, x - .006, y - .07, z - .025, x + .006, y, z + .025, 'GInt_Chain')
        y -= .06; k += 1
    bbox(p, x - .05, y - .1, z - .02, x + .05, y, z + .02, 'PInt_Iron')
    for k in range(6):
        a0, a1 = math.pi * k / 6, math.pi * (k + 1) / 6
        beam(p, (x + .06 * math.cos(a0), y - .1 - .06 * math.sin(a0), z), (x + .06 * math.cos(a1), y - .1 - .06 * math.sin(a1), z), .02, .02, 'PInt_Iron')
    # Hand chain loop hanging beside the block.
    for side in (-1, 1):
        yy = top - .42
        while yy > F['y'] - .4:
            bbox(p, x + .16 - .004, yy - .05, z + side * .05 - .004, x + .16 + .004, yy, z + side * .05 + .004, 'GInt_Chain')
            yy -= .05


def build_furniture_piece(F, L):
    kind = F['kind']
    if kind in POLICE_KINDS:
        if kind == 'desklamp':
            return
        K.build_furniture_piece(F, POLICE_LEVELS)
        if F['room'] in EXTERIOR_ROOMS:
            # Outside the rooms: keep it off the interior light layer.
            old, new = 'Furn_' + F['id'], 'ExtFurn_' + F['id']
            K.PARTS[new] = K.PARTS.pop(old); K.PARTS[new].name = new
            K.ORDER[K.ORDER.index(old)] = new
        return
    if kind == 'hoist':
        build_hoist(F)
        return
    exterior = F['room'] in EXTERIOR_ROOMS
    name = ('ExtFurn_' if exterior else 'Furn_') + F['id']
    p = part(name, (F['x'], F['y'], F['z']))
    f = Frame(p, F['x'], F['y'], F['z'], F['yaw'])
    w, d, h = F['w'], F['d'], F['h']
    wood, light, dark = 'PInt_Furniture', 'PInt_FurnLight', 'GExt_Walnut'
    if kind == 'counter':
        fb(f, -w / 2, 0, -d / 2 + .05, w / 2, h - .04, d / 2, wood)
        fb(f, -w / 2 - .03, h - .04, -d / 2, w / 2 + .03, h, d / 2 + .05, light)
        for i in range(5):
            x0 = -w / 2 + .1 + i * (w - .2) / 5
            fb(f, x0 + .03, .15, d / 2, x0 + (w - .2) / 5 - .03, h - .15, d / 2 + .012, light, faces=['+z', '+x', '-x', '+y', '-y'])
        fb(f, -w / 2, 0, d / 2, w / 2, .1, d / 2 + .02, 'PInt_Black')
        fb(f, -w / 2 + .1, .35, -d / 2 + .06, w / 2 - .1, .38, d / 2 - .02, light, faces=['+y'])
    elif kind == 'pigeonhole':
        t = .02
        fb(f, -w / 2, 0, -d / 2, w / 2, h, -d / 2 + .02, wood)
        rows, cols = 7, max(3, round(w / .3))
        for i in range(cols + 1):
            x = -w / 2 + w * i / cols
            fb(f, x - t / 2, 0, -d / 2, x + t / 2, h, d / 2, wood)
        for k in range(rows + 1):
            y = h * k / rows
            fb(f, -w / 2, y - t / 2, -d / 2, w / 2, y + t / 2, d / 2, wood)
        for i in range(cols):
            for k in range(rows):
                if RNG.random() < .7:
                    x0 = -w / 2 + w * i / cols + .03; x1 = -w / 2 + w * (i + 1) / cols - .03
                    y0 = h * k / rows + .015; hh = RNG.uniform(.08, h / rows - .04)
                    m = RNG.choice(['PInt_Box', 'PInt_Box', 'GInt_Steel', 'PInt_Paper', 'GInt_Red', 'GInt_Copper'])
                    fb(f, x0 + RNG.uniform(0, .04), y0, -d / 2 + .04, x1 - RNG.uniform(0, .04), y0 + hh, d / 2 - .03, m)
    elif kind == 'workbench':
        fb(f, -w / 2, h - .07, -d / 2, w / 2, h, d / 2, 'PInt_FurnLight')
        for sx in (-1, 1):
            for sz in (-1, 1):
                fb(f, sx * (w / 2 - .08) - .05, 0, sz * (d / 2 - .07) - .05, sx * (w / 2 - .08) + .05, h - .07, sz * (d / 2 - .07) + .05, wood)
        fb(f, -w / 2 + .05, .18, -d / 2 + .05, w / 2 - .05, .22, d / 2 - .05, wood)
        fb(f, -w / 2, h, -d / 2, w / 2, h + .3, -d / 2 + .03, wood)
        for k in range(3):
            fb(f, -w / 2 + .2 + k * .55, .22, -d / 2 + .1, -w / 2 + .6 + k * .55, .42, d / 2 - .12, RNG.choice(['PInt_Box', 'GInt_Steel', 'GInt_Red']))
        fb(f, -.3, h - .2, d / 2 - .02, .3, h - .08, d / 2 + .01, light)
        fb(f, -.04, h - .15, d / 2 + .01, .04, h - .13, d / 2 + .03, 'PInt_Brass')
        # Loose tools on the top: a spanner, a hammer, a file and an oily rag.
        fb(f, -w / 2 + .3, h, -.05, -w / 2 + .55, h + .012, -.02, 'GInt_Steel')
        fb(f, .2, h, .05, .5, h + .025, .08, 'PInt_FurnLight'); fb(f, .45, h, .02, .5, h + .05, .12, 'PInt_Iron')
        fb(f, w / 2 - .5, h, -.12, w / 2 - .25, h + .01, -.1, 'GInt_Steel')
        fb(f, w / 2 - .4, h, .1, w / 2 - .18, h + .025, .22, 'GInt_Rag')
    elif kind == 'vise':
        fb(f, -.07, 0, -.12, .07, .04, .12, 'GInt_Steel')
        fb(f, -.05, .04, -.04, .05, .17, .04, 'GInt_Steel')
        fb(f, -.06, .1, -.15, .06, .2, -.05, 'GInt_Steel')
        fb(f, -.06, .1, -.03, .06, .2, .08, 'GInt_Steel')
        fb(f, -.065, .19, -.06, .065, .22, -.045, 'PInt_Iron'); fb(f, -.065, .19, -.032, .065, .22, -.017, 'PInt_Iron')
        fcyl(f, (0, .15, .08), (0, .15, .22), .012, 'GInt_Chrome')
        fcyl(f, (-.12, .15, .22), (.12, .15, .22), .008, 'GInt_Chrome')
        for sx in (-1, 1):
            fcyl(f, (sx * .12, .15, .22), (sx * .125, .15, .22), .018, 'GInt_Chrome')
    elif kind == 'toolboard':
        fb(f, -w / 2, 0, -.02, w / 2, h, .02, 'PInt_Panel')
        x = -w / 2 + .12
        while x < w / 2 - .1:
            kindt = RNG.random()
            ln = RNG.uniform(.18, .36)
            m = 'GInt_Steel' if kindt < .6 else 'PInt_FurnLight'
            fb(f, x - .025, h / 2 - ln / 2 + RNG.uniform(-.2, .2), .02, x + .025, h / 2 + ln / 2, .035, m)
            fb(f, x - .006, h - .12, .02, x + .006, h - .1, .07, 'PInt_Iron')
            x += RNG.uniform(.1, .17)
        fb(f, -w / 2 + .3, .08, .02, -w / 2 + .9, .18, .03, 'GInt_Steel')
    elif kind == 'tagboard':
        fb(f, -w / 2, 0, -.015, w / 2, h, .015, 'PInt_Panel')
        rows, cols = 3, 6
        for r in range(rows):
            for c in range(cols):
                x = -w / 2 + .1 + c * (w - .2) / (cols - 1); y = h - .15 - r * (h - .25) / (rows - 1)
                fb(f, x - .004, y + .04, .015, x + .004, y + .05, .05, 'PInt_Brass')
                if (r, c) == (1, 2):
                    # The empty hook: a clean, unfaded patch where a tag used to hang.
                    fb(f, x - .032, y - .03, .015, x + .032, y + .035, .017, 'PInt_FurnLight', faces=['+z'])
                    continue
                tube(p, f.pt(x, y, .03), f.pt(x, y, .036), .028, 'PInt_Brass', seg=10)
    elif kind == 'drawercabinet':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2 - .02, wood)
        fb(f, -w / 2 - .02, h, -d / 2 - .02, w / 2 + .02, h + .03, d / 2 + .02, light)
        rows, cols = 5, 4
        for r in range(rows):
            for c in range(cols):
                x0 = -w / 2 + .04 + c * (w - .08) / cols; x1 = x0 + (w - .08) / cols - .015
                y0 = .06 + r * (h - .1) / rows; y1 = y0 + (h - .1) / rows - .015
                if (r, c) == (0, cols - 1):
                    front = f.pt((x0 + x1) / 2, (y0 + y1) / 2, d / 2)
                    dp = part('Drawer_0', front)
                    df = Frame(dp, F['x'], F['y'], F['z'], F['yaw'])
                    df.bb(x0, y0, d / 2 - .02, x1, y1, d / 2 + .01, light)
                    df.bb(x0 + .02, y0 + .01, -d / 2 + .06, x1 - .02, y0 + .12, d / 2 - .02, wood, faces=['+x', '-x', '-z', '+y'])
                    for k in range(4):
                        xx = x0 + .05 + k * (x1 - x0 - .1) / 3
                        df.bb(xx - .025, y0 + .015, -.1 + k * .03, xx + .025, y0 + .022, -.06 + k * .03, 'PInt_Brass')
                    df.bb((x0 + x1) / 2 - .03, (y0 + y1) / 2 - .01, d / 2 + .01, (x0 + x1) / 2 + .03, (y0 + y1) / 2 + .01, d / 2 + .03, 'PInt_Brass')
                    DRAWERS.append(dict(object='Drawer_0', index=0, label='零件抽屉', front=list(front), axis=list(f.dir(0, 0, 1)), travel=.3))
                    f.bb(x0, y0, d / 2 - .02, x1, y1, d / 2 - .015, 'PInt_Black', faces=['+z'])
                    continue
                fb(f, x0, y0, d / 2 - .02, x1, y1, d / 2 + .005, light, faces=['+z', '+x', '-x', '+y', '-y'])
                fb(f, (x0 + x1) / 2 - .03, (y0 + y1) / 2 - .01, d / 2 + .005, (x0 + x1) / 2 + .03, (y0 + y1) / 2 + .01, d / 2 + .025, 'PInt_Brass')
                fb(f, (x0 + x1) / 2 - .045, y1 - .045, d / 2 + .005, (x0 + x1) / 2 + .045, y1 - .02, d / 2 + .01, 'PInt_Paper')
    elif kind == 'toolshelf':
        t = .025
        fb(f, -w / 2, 0, -d / 2, -w / 2 + t, h, d / 2, wood); fb(f, w / 2 - t, 0, -d / 2, w / 2, h, d / 2, wood)
        for k in range(4):
            y = .05 + k * (h - .1) / 3
            fb(f, -w / 2 + t, y, -d / 2, w / 2 - t, y + .025, d / 2, wood)
            x = -w / 2 + .1
            while x < w / 2 - .2 and k < 3:
                ww = RNG.uniform(.15, .4)
                m = RNG.choice(['PInt_Box', 'GInt_Steel', 'GInt_Red', 'GInt_Blue', 'GInt_Copper', 'GInt_Rag'])
                fb(f, x, y + .025, -d / 2 + .05, x + ww, y + .025 + RNG.uniform(.08, .26), d / 2 - .04, m)
                x += ww + RNG.uniform(.04, .12)
    elif kind == 'anvil':
        fcyl(f, (0, 0, 0), (0, .45, 0), .2, 'PInt_FurnLight', seg=12)
        fb(f, -.12, .45, -.09, .12, .5, .09, 'PInt_Iron')
        fb(f, -.07, .5, -.06, .07, .62, .06, 'PInt_Iron')
        fb(f, -.22, .62, -.08, .17, .75, .08, 'PInt_Iron')
        fbeam(f, (-.22, .7, 0), (-.34, .74, 0), .1, .06, 'PInt_Iron')
    elif kind == 'grinder':
        fcyl(f, (0, 0, 0), (0, .05, 0), .2, 'PInt_Iron', seg=12)
        fcyl(f, (0, .05, 0), (0, .85, 0), .06, 'PInt_Iron', seg=10)
        fb(f, -.12, .85, -.1, .12, 1.02, .1, 'GInt_Truck')
        for sx in (-1, 1):
            fcyl(f, (sx * .13, .93, 0), (sx * .2, .93, 0), .1, 'GInt_Grind', seg=16)
            fb(f, sx * .17 - .04, .93, -.12, sx * .17 + .04, 1.05, .0, 'PInt_Iron')
    elif kind == 'jack':
        fb(f, -.14, .03, -.35, .14, .16, .35, 'GInt_Red')
        for sx in (-1, 1):
            for sz in (-1, 1):
                fcyl(f, (sx * .16, .05, sz * .28), (sx * .12, .05, sz * .28), .05, 'PInt_Iron')
        fb(f, -.06, .16, .2, .06, .22, .34, 'GInt_Steel')
        fbeam(f, (0, .12, -.33), (0, .7, -.85), .035, .035, 'PInt_Iron')
    elif kind == 'ragbin':
        fcyl(f, (0, 0, 0), (0, h - .1, 0), w / 2, 'GInt_Steel', seg=14, caps=False)
        f.bb(-w / 2 + .03, 0, -w / 2 + .03, w / 2 - .03, .02, w / 2 - .03, 'GInt_Steel')
        for k in range(6):
            a = k * 1.1
            fb(f, math.cos(a) * .1 - .09, h - .2 + (k % 3) * .04, math.sin(a) * .1 - .07, math.cos(a) * .1 + .09, h - .08 + (k % 3) * .04, math.sin(a) * .1 + .07, 'GInt_Rag')
        fb(f, .05, h - .12, -.05, .2, h - .04, .08, 'PInt_Leather')
    elif kind in ('crate', 'chest'):
        mat = 'PInt_FurnLight' if kind == 'crate' else 'GInt_Red'
        if kind == 'crate' and h > .65:
            fb(f, -w / 2, 0, -d / 2, w / 2, h / 2, d / 2, mat)
            fb(f, -w / 2 + .1, h / 2, -d / 2 + .05, w / 2 - .05, h, d / 2 - .1, mat)
        else:
            fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2, mat)
        for y in (.04, h - .08):
            fb(f, -w / 2 - .01, y, -d / 2 - .01, w / 2 + .01, y + .05, d / 2 + .01, 'PInt_Black' if kind == 'chest' else 'PInt_Furniture', faces=['+x', '-x', '+z', '-z'])
        if kind == 'chest':
            for sx in (-1, 1):
                fb(f, sx * w / 2 - .02, h * .6, -.06, sx * w / 2 + .02, h * .6 + .03, .06, 'PInt_Iron')
    elif kind == 'oilcans':
        for k, (x, z, r, hh, m) in enumerate(((-.15, 0, .05, .22, 'GInt_Red'), (.0, .05, .06, .16, 'GInt_Copper'), (.15, -.02, .04, .25, 'GInt_Blue'))):
            fcyl(f, (x, 0, z), (x, hh, z), r, m, seg=10)
        fbeam(f, (0, .16, .05), (.12, .22, .05), .015, .015, 'GInt_Copper')
    elif kind in ('drum', 'barrel'):
        if kind == 'drum':
            m = RNG.choice(['GInt_Red', 'GInt_Blue', 'GInt_Truck'])
            fcyl(f, (0, 0, 0), (0, h, 0), w / 2, m, seg=16)
            for y in (h * .33, h * .66):
                fcyl(f, (0, y - .015, 0), (0, y + .015, 0), w / 2 + .012, m, seg=16, caps=False)
            fcyl(f, (.15, h, .05), (.15, h + .02, .05), .03, 'GInt_Steel')
        else:
            frustum(p, F['x'], F['z'], w / 2 - .05, w / 2, F['y'], F['y'] + h / 2, 'PInt_FurnLight', seg=16)
            frustum(p, F['x'], F['z'], w / 2, w / 2 - .05, F['y'] + h / 2, F['y'] + h, 'PInt_FurnLight', seg=16, cap=True)
            for y in (.1, h - .12):
                fcyl(f, (0, y, 0), (0, y + .04, 0), w / 2 - .02, 'PInt_Iron', seg=16, caps=False)
    elif kind == 'tyres':
        for k in range(4):
            tyre(p, f.pt(RNG.uniform(-.03, .03), .1 + k * .2, RNG.uniform(-.03, .03)), (0, 1, 0), .36, .22, .19, rim='GInt_Rubber')
    elif kind == 'wheels':
        for k in range(2):
            tyre(p, f.pt(-.15 + k * .3, .37, 0), f.dir(1, 0, .15 * (1 - 2 * k)), .36, .25, .13, spokes=12, spoke_mat='PInt_FurnLight')
    elif kind == 'carseat':
        fb(f, -w / 2, 0, -d / 2, w / 2, .3, d / 2, 'PInt_Leather')
        fb(f, -w / 2, .3, -d / 2, w / 2, h, -d / 2 + .14, 'PInt_Leather')
        for k in range(1, 4):
            x = -w / 2 + w * k / 4
            fb(f, x - .006, .3, -d / 2 + .14, x + .006, h - .05, -d / 2 + .145, 'PInt_Black')
    elif kind == 'chalkboard':
        for sz in (-1, 1):
            fbeam(f, (-w / 2 + .03, 0, sz * .16), (-w / 2 + .03, h, 0), .04, .04, wood)
            fbeam(f, (w / 2 - .03, 0, sz * .16), (w / 2 - .03, h, 0), .04, .04, wood)
        for sz in (-1, 1):
            a = f.pt(-w / 2 + .06, .1, sz * .145); b = f.pt(w / 2 - .06, .1, sz * .145)
            c = f.pt(w / 2 - .06, h - .06, sz * .02); dd = f.pt(-w / 2 + .06, h - .06, sz * .02)
            nrm = f.dir(0, .16, sz * h)
            p.quad([a, b, c, dd], norm(nrm), 'PInt_Black')
    elif kind == 'hangsign':
        # Iron bracket out of the office front, the board hangs below on two rings.
        y = F['y']
        beam(p, (F['x'], y + .55, .0), (F['x'], y + .55, -.95), .03, .04, 'GExt_Iron')
        beam(p, (F['x'], y + .2, .0), (F['x'], y + .53, -.6), .025, .025, 'GExt_Iron')
        for zz in (-.25, -.8):
            tube(p, (F['x'], y + .55, zz), (F['x'], y + .44, zz), .006, 'GExt_Iron', seg=5)
        bx = F['x']; z0, z1 = -.88, -.17; y0, y1 = y - .25, y + .44
        bbox(p, bx - .025, y0, z0, bx + .025, y1, z1, 'GExt_Green', faces=['+y', '-y', '+z', '-z'])
        for nx in (1, -1):
            xs = bx + nx * .026
            verts = [(xs, y0, z1), (xs, y0, z0), (xs, y1, z0), (xs, y1, z1)] if nx > 0 else [(xs, y0, z0), (xs, y0, z1), (xs, y1, z1), (xs, y1, z0)]
            p.faces.append((verts, (nx, 0, 0), 'GExt_Sign', [(0, 0), (1, 0), (1, 1), (0, 1)]))
    elif kind == 'tyrerack':
        for sx in (-1, 1):
            fb(f, sx * (w / 2 - .05) - .04, 0, -d / 2, sx * (w / 2 - .05) + .04, h, -d / 2 + .08, wood)
            fb(f, sx * (w / 2 - .05) - .04, 0, d / 2 - .08, sx * (w / 2 - .05) + .04, h, d / 2, wood)
        for y in (.15, .9):
            fb(f, -w / 2, y, -d / 2, w / 2, y + .06, d / 2, wood)
        for k in range(4):
            tyre(p, f.pt(-w / 2 + .3 + k * .45, .55, 0), f.dir(1, 0, 0), .34, .22, .17, rim='GInt_Rubber')
            tyre(p, f.pt(-w / 2 + .3 + k * .45, 1.3, 0), f.dir(1, 0, 0), .3, .2, .15, rim='GInt_Steel')
    elif kind == 'timber':
        for k in range(6):
            fb(f, -w / 2 + RNG.uniform(0, .2), (k // 2) * .1, -d / 2 + (k % 2) * .25, w / 2 - RNG.uniform(0, .2), (k // 2) * .1 + .09, -d / 2 + (k % 2) * .25 + .22, 'PInt_FurnLight')
    elif kind == 'trough':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, -d / 2 + .08, 'GExt_Ashlar'); fb(f, -w / 2, 0, d / 2 - .08, w / 2, h, d / 2, 'GExt_Ashlar')
        fb(f, -w / 2, 0, -d / 2 + .08, -w / 2 + .08, h, d / 2 - .08, 'GExt_Ashlar'); fb(f, w / 2 - .08, 0, -d / 2 + .08, w / 2, h, d / 2 - .08, 'GExt_Ashlar')
        fb(f, -w / 2 + .08, 0, -d / 2 + .08, w / 2 - .08, h - .12, d / 2 - .08, 'GInt_Water', faces=['+y'])
        fcyl(f, (-w / 2 + .2, h, 0), (-w / 2 + .2, h + .7, 0), .04, 'GExt_Iron')
        fbeam(f, (-w / 2 + .2, h + .65, 0), (-w / 2 + .45, h + .6, 0), .03, .03, 'GExt_Iron')
    elif kind == 'handcart':
        for sx in (-1, 1):
            ftyre(f, (sx * .42, .3, -.2), (1, 0, 0), .3, .24, .05, spokes=10, spoke_mat='PInt_FurnLight', tread='PInt_Iron')
        fb(f, -.38, .32, -.6, .38, .38, .6, 'PInt_FurnLight')
        for sx in (-1, 1):
            fb(f, sx * .38 - .02, .38, -.6, sx * .38 + .02, .6, .6, 'PInt_FurnLight')
            fbeam(f, (sx * .3, .35, .6), (sx * .3, .75, 1.1), .04, .04, 'PInt_FurnLight')
        fbeam(f, (-.38, .1, .45), (-.38, .32, .45), .03, .03, 'PInt_FurnLight'); fbeam(f, (.38, .1, .45), (.38, .32, .45), .03, .03, 'PInt_FurnLight')
    elif kind == 'tray':
        fb(f, -w / 2, 0, -d / 2, w / 2, .04, d / 2, 'PInt_Furniture')
        fb(f, -w / 2 + .02, .02, -d / 2 + .02, w / 2 - .02, .03, d / 2 - .02, 'PInt_Paper', faces=['+y'])
    elif kind == 'bell':
        fcyl(f, (0, 0, 0), (0, .012, 0), .05, 'PInt_Black', seg=12)
        frustum(p, F['x'], F['z'], .045, .015, F['y'] + .012, F['y'] + .06, 'PInt_Brass', seg=12, cap=True)
    elif kind == 'motorcycle':
        build_motorcycle(f, F)
    elif kind == 'truck':
        build_truck(f, F)
    else:
        raise ValueError('Unknown furniture kind ' + kind)


POLICE_LEVELS = None


# ---------------------------------------------------------------------------
# Mesh/object creation
# ---------------------------------------------------------------------------
def make_materials():
    mats = {}
    used = {f[2] for p in K.PARTS.values() for f in p.faces}
    for name in sorted(used):
        hexc, metal, rough, tile = MATS[name]
        m = bpy.data.materials.new(name); m.use_nodes = True
        rgb = tuple(int(hexc[i:i + 2], 16) / 255 for i in (0, 2, 4))
        lin = tuple(c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in rgb)
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*lin, 1)
        bsdf.inputs['Metallic'].default_value = metal
        bsdf.inputs['Roughness'].default_value = rough
        m.diffuse_color = (*lin, .35 if name == 'G_Glass' else 1)
        mats[name] = m
    return mats


def build_objects(collection, mats):
    root = bpy.data.objects.new('GarageVisual', None)
    collection.objects.link(root)
    for name, pos in (('EastMarker', (3, 0, 0)), ('NorthMarker', (0, 0, 3))):
        e = bpy.data.objects.new(name, None); e.location = u(pos); e.parent = root; collection.objects.link(e)
    stats = {}
    for name in K.ORDER:
        p = K.PARTS[name]
        if not p.faces:
            continue
        pivot = Vector(p.pivot) if p.pivot is not None else Vector((0, 0, 0))
        mesh = bpy.data.meshes.new(name + '_Mesh')
        bm = bmesh.new(); uvl = bm.loops.layers.uv.new('UVMap')
        used = []
        for verts, normal, mat, uvs in p.faces:
            bverts = [u(v) - u(pivot) for v in verts]
            want = u(normal) - u((0, 0, 0))
            got = K.newell(bverts)
            order = list(range(len(verts)))
            if got.length > 1e-12 and got.dot(want) < 0:
                order.reverse()
            try:
                face = bm.faces.new([bm.verts.new(bverts[i]) for i in order])
            except ValueError:
                continue
            if mat not in used:
                used.append(mat)
            face.material_index = used.index(mat)
            tile = MATS[mat][3] or 1.0
            ax = max(range(3), key=lambda k: abs(normal[k]))
            for loop, i in zip(face.loops, order):
                if uvs is not None:
                    loop[uvl].uv = uvs[i]
                else:
                    v = verts[i]
                    uvc = (v[2], v[1]) if ax == 0 else (v[0], v[2]) if ax == 1 else (v[0], v[1])
                    loop[uvl].uv = (uvc[0] / tile, uvc[1] / tile)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bmesh.ops.triangulate(bm, faces=bm.faces, quad_method='BEAUTY', ngon_method='BEAUTY')
        bm.to_mesh(mesh); bm.free()
        for m in used:
            mesh.materials.append(mats[m])
        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj); obj.parent = root; obj.location = u(pivot)
        stats[name] = len(mesh.polygons)
    bpy.context.view_layer.update()
    return root, stats


def render_previews(out_dir, collection):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True; scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    scene.world = bpy.data.worlds.new('GaragePreviewWorld')
    cam_data = bpy.data.cameras.new('PreviewCam'); cam = bpy.data.objects.new('PreviewCam', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    roofs = [o for o in collection.objects if o.name.startswith(('Ext_Roof', 'Int_Roof', 'Int_Trusses', 'Int_Ceilings', 'Lamp_', 'Bulb_'))]
    shots = {
        'street-front': ((2.0, 1.7, -14.0), (10.5, 3.5, 4.0), 58, []),
        'lane-east': ((27.0, 1.8, -6.0), (14.0, 4.0, 9.0), 55, []),
        'yard': ((12.0, 1.7, 29.0), (9.0, 2.5, 18.0), 62, []),
        'plan-cutaway': ((9.0, 40.0, 10.0), (9.0, 0.0, 12.5), 50, roofs),
        'hall-from-door': ((11.3, 1.66, .8), (11.0, 2.2, 14.0), 72, []),
        'hall-to-door': ((10.0, 1.66, 13.5), (12.0, 2.0, 0.0), 72, []),
        'loft': ((6.2, LOFT_EYE, 15.2), (9.5, 3.4, 18.4), 72, []),
        'office': ((4.4, 1.66, 1.2), (1.2, 1.2, 6.5), 72, []),
    }
    for name, (eye, target, fov, hide) in shots.items():
        for o in hide:
            o.hide_render = True
        cam.location = u(eye)
        cam.rotation_euler = (u(target) - u(eye)).to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(fov); cam_data.clip_end = 300; cam_data.clip_start = .05
        scene.render.filepath = str(out_dir / ('preview-' + name + '.png'))
        bpy.ops.render.render(write_still=True)
        for o in hide:
            o.hide_render = False
    bpy.data.objects.remove(cam)


LOFT_EYE = 4.4


def main():
    global POLICE_LEVELS
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument('--layout', default='assets/source/garage/garage-layout.json')
    ap.add_argument('--output', default='assets/source/garage')
    ap.add_argument('--unity', default='game/Assets/Art/Garage')
    ap.add_argument('--previews', default='')
    args = ap.parse_args(argv)
    L = json.loads(Path(args.layout).read_text(encoding='utf-8'))
    out = Path(args.output).resolve(); out.mkdir(parents=True, exist_ok=True)
    lv = L['levels']
    # The police furniture kit reads its ceiling heights (stove flue) from these names.
    POLICE_LEVELS = dict(L, levels=dict(lv, groundCeiling=lv['officeCeiling'], upperCeiling=lv['officeCeiling'], wingCeiling=lv['officeCeiling']))

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'

    build_walls(L); build_gables(L); build_fences_and_rails(L); build_slabs(L); build_thresholds(L); build_stairs(L)
    build_hall_roof(L); build_dormers(L); build_other_roofs(L); build_trusses(L); build_exterior_details(L)
    build_joinery(L); build_lamps(L)
    for F in L['furniture']:
        build_furniture_piece(F, L)

    collection = bpy.data.collections.new('RepairGarage')
    bpy.context.scene.collection.children.link(collection)
    mats = make_materials()
    root, stats = build_objects(collection, mats)

    manifest = dict(version=1, layoutVersion=L['version'], objects=stats, triangles=sum(stats.values()),
                    leaves=K.LEAVES, drawers=DRAWERS,
                    materials=[dict(name=k, hex=v[0], metallic=v[1], roughness=v[2], tile=v[3]) for k, v in MATS.items() if k in mats],
                    blender=bpy.app.version_string)
    (out / 'garage-build-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out / 'RepairGarage.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    if args.unity:
        dest = Path(args.unity).resolve(); dest.mkdir(parents=True, exist_ok=True)
        (dest / 'RepairGarage.fbx').write_bytes((out / 'RepairGarage.fbx').read_bytes())
        (dest / 'GarageBuildManifest.json').write_bytes((out / 'garage-build-manifest.json').read_bytes())
    if args.previews:
        prev = Path(args.previews).resolve(); prev.mkdir(parents=True, exist_ok=True)
        render_previews(prev, collection)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'RepairGarage.blend'), compress=True)
    print('GARAGE_BLENDER_PASS', json.dumps(dict(objects=len(stats), triangles=manifest['triangles'], leaves=len(K.LEAVES), drawers=len(DRAWERS))))


if __name__ == '__main__':
    main()
