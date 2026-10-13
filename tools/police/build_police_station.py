"""Blender generator for the D courtyard-loggia police station (B10).

Reads the shared layout contract (tools/police/police_layout.py output) and
builds one editable scene: shell with real door/window apertures, floors,
ceilings, stairs, roofs, loggias, joinery, movable door leaves and drawers on
their pivots, lamp fittings and furniture. Exports the FBX used by Unity's
BuildSlice.Police and saves the packed .blend.

    blender --background --factory-startup --python-exit-code 1 \
        --python tools/police/build_police_station.py -- \
        --layout assets/source/police/police-layout.json \
        --output assets/source/police --previews output/demo/police-station/blender

Coordinates in this script are Unity building-local metres (+X east, +Y up,
+Z north); u() converts to Blender for an FBX exported with -Z forward/Y up
(same convention as tools/car/build_hotel_car.py). Object names are the
contract with Unity: Ext_*/Int_* shells, Glass, Leaf_<opening>_<n> (origin on
the hinge), Drawer_<n>, Furn_<id>, Lamp_<id>, Bulb_<id>, EastMarker/NorthMarker.
"""
import argparse, json, math, random, sys
from pathlib import Path

import bpy, bmesh
from mathutils import Vector

# name: (hex, metallic, roughness, metres per texture repeat or 0 for flat)
MATERIALS = {
    'PExt_Stucco': ('c2bdb0', 0, .9, 3.0), 'PExt_Ashlar': ('8a8c88', 0, .85, 1.2), 'PExt_Slate': ('4a5058', 0, .6, 1.5),
    'PExt_Flags': ('928d84', 0, .8, 3.0), 'PExt_Timber': ('5e4632', 0, .7, 1.0), 'PExt_Paint': ('ddd5c3', 0, .5, 0),
    'PExt_DoorOak': ('4f3a29', 0, .45, 1.0), 'PExt_Iron': ('2c3133', .6, .45, 0), 'PExt_Gutter': ('33393b', .5, .5, 0),
    'PExt_Sign': ('2f3b38', 0, .5, 0),
    'P_Glass': ('9fb3be', 0, .05, 0), 'P_LampGlass': ('ffd9a0', 0, .2, 0), 'P_Bulb': ('fff0cf', 0, .2, 0),
    'PInt_Plaster': ('d9d0bd', 0, .9, 3.0), 'PInt_Ceiling': ('e6e0d2', 0, .95, 3.0), 'PInt_Oak': ('9a805f', 0, .55, 2.0),
    'PInt_Tiles': ('a07a62', 0, .5, 1.0), 'PInt_Stone': ('7c7d79', 0, .85, 1.2), 'PInt_Panel': ('5e4632', 0, .5, 1.0),
    'PInt_Furniture': ('6a4f37', 0, .5, 1.0), 'PInt_FurnLight': ('9a7c58', 0, .45, 1.0), 'PInt_Green': ('3f5a47', 0, .6, 0),
    'PInt_Brass': ('b38a49', .8, .35, 0), 'PInt_Iron': ('2b2f31', .5, .5, 0), 'PInt_Paper': ('e8dfc8', 0, .9, 0),
    'PInt_Folder': ('7a6543', 0, .8, 0), 'PInt_Fabric': ('8a8170', 0, .95, 0), 'PInt_Oilcloth': ('3b4639', 0, .4, 0),
    'PInt_Enamel': ('e6e2d6', 0, .25, 0), 'PInt_Cork': ('a37c55', 0, .9, 0), 'PInt_Black': ('1e1e1e', .2, .4, 0),
    'PInt_Photo': ('5b4a3a', 0, .4, 0), 'PInt_BookRed': ('6a3a2c', 0, .8, 0), 'PInt_BookGreen': ('3e4a3a', 0, .8, 0),
    'PInt_BookBrown': ('6a5a3a', 0, .8, 0), 'PInt_BookBlue': ('2e3a4a', 0, .8, 0), 'PInt_Box': ('8a7656', 0, .9, 0),
    'PInt_Leather': ('4a3328', 0, .6, 0),
}
RNG = random.Random(19310120)


def u(p):
    return Vector((-float(p[0]), -float(p[2]), float(p[1])))


class Part:
    def __init__(self, name, pivot=None):
        self.name, self.pivot = name, pivot
        self.faces = []  # (verts unity, normal unity, material, uvs)

    def quad(self, verts, normal, mat, uvs=None):
        if mat not in MATERIALS:
            raise ValueError('Unknown material ' + mat)
        self.faces.append(([tuple(v) for v in verts], tuple(normal), mat, uvs))


PARTS, ORDER = {}, []


def part(name, pivot=None):
    if name not in PARTS:
        PARTS[name] = Part(name, pivot)
        ORDER.append(name)
    return PARTS[name]


def box(p, c, s, mat, faces='all', mats=None):
    """Axis-aligned box: c centre, s size (Unity). faces: 'all' or subset of '+x-x+y-y+z-z'."""
    cx, cy, cz = c; hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    spec = {
        '+x': ([(cx + hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz + hz), (cx + hx, cy + hy, cz + hz), (cx + hx, cy + hy, cz - hz)], (1, 0, 0)),
        '-x': ([(cx - hx, cy - hy, cz + hz), (cx - hx, cy - hy, cz - hz), (cx - hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz + hz)], (-1, 0, 0)),
        '+y': ([(cx - hx, cy + hy, cz - hz), (cx + hx, cy + hy, cz - hz), (cx + hx, cy + hy, cz + hz), (cx - hx, cy + hy, cz + hz)], (0, 1, 0)),
        '-y': ([(cx - hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz - hz), (cx - hx, cy - hy, cz - hz)], (0, -1, 0)),
        '+z': ([(cx + hx, cy - hy, cz + hz), (cx - hx, cy - hy, cz + hz), (cx - hx, cy + hy, cz + hz), (cx + hx, cy + hy, cz + hz)], (0, 0, 1)),
        '-z': ([(cx - hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz - hz), (cx + hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz - hz)], (0, 0, -1)),
    }
    for key, (verts, n) in spec.items():
        if faces != 'all' and key not in faces:
            continue
        p.quad(verts, n, (mats or {}).get(key, mat))


def bbox(p, x0, y0, z0, x1, y1, z1, mat, faces='all', mats=None):
    box(p, ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0)), mat, faces, mats)


def tri(p, verts, normal, mat, uvs=None):
    p.faces.append(([tuple(v) for v in verts], tuple(normal), mat, uvs))


def prism(p, base, y0, y1, mat, top=True, bottom=False):
    """Vertical prism over a convex XZ polygon (CCW seen from above)."""
    n = len(base)
    for i in range(n):
        a, b = base[i], base[(i + 1) % n]
        nx, nz = (b[1] - a[1]), -(b[0] - a[0])
        ln = math.hypot(nx, nz) or 1
        p.quad([(a[0], y0, a[1]), (b[0], y0, b[1]), (b[0], y1, b[1]), (a[0], y1, a[1])], (nx / ln, 0, nz / ln), mat)
    if top:
        for i in range(1, n - 1):
            tri(p, [(base[0][0], y1, base[0][1]), (base[i][0], y1, base[i][1]), (base[i + 1][0], y1, base[i + 1][1])], (0, 1, 0), mat)
    if bottom:
        for i in range(1, n - 1):
            tri(p, [(base[0][0], y0, base[0][1]), (base[i + 1][0], y0, base[i + 1][1]), (base[i][0], y0, base[i][1])], (0, -1, 0), mat)


def cylinder(p, cx, cz, r, y0, y1, mat, seg=10, top=True):
    prism(p, [(cx + r * math.cos(2 * math.pi * i / seg), cz + r * math.sin(2 * math.pi * i / seg)) for i in range(seg)], y0, y1, mat, top)


def frustum(p, cx, cz, r0, r1, y0, y1, mat, seg=12, cap=False):
    for i in range(seg):
        a0, a1 = -2 * math.pi * i / seg, -2 * math.pi * (i + 1) / seg
        pa0 = (cx + r0 * math.cos(a0), y0, cz + r0 * math.sin(a0)); pa1 = (cx + r0 * math.cos(a1), y0, cz + r0 * math.sin(a1))
        pb0 = (cx + r1 * math.cos(a0), y1, cz + r1 * math.sin(a0)); pb1 = (cx + r1 * math.cos(a1), y1, cz + r1 * math.sin(a1))
        mid = (a0 + a1) / 2; slope = (r0 - r1) / max(1e-6, (y1 - y0))
        n = (math.cos(mid), slope, math.sin(mid))
        p.quad([pa0, pa1, pb1, pb0], n, mat)
        if cap:
            tri(p, [(cx, y1, cz), pb1, pb0], (0, 1, 0), mat)


# ---------------------------------------------------------------------------
# Local frames for furniture: build in (x right, z front) then rotate by yaw.
# ---------------------------------------------------------------------------
class Frame:
    def __init__(self, p, x, y, z, yaw):
        self.p, self.o, self.yaw = p, (x, y, z), math.radians(yaw)

    def pt(self, lx, ly, lz):
        c, s = math.cos(self.yaw), math.sin(self.yaw)
        # local +z (front) -> (sin yaw, cos yaw); local +x -> (cos yaw, -sin yaw)
        return (self.o[0] + lx * c + lz * s, self.o[1] + ly, self.o[2] - lx * s + lz * c)

    def dir(self, dx, dy, dz):
        c, s = math.cos(self.yaw), math.sin(self.yaw)
        return (dx * c + dz * s, dy, -dx * s + dz * c)

    def box(self, c, s, mat, faces='all'):
        cx, cy, cz = c; hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        spec = {
            '+x': ([(cx + hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz + hz), (cx + hx, cy + hy, cz + hz), (cx + hx, cy + hy, cz - hz)], (1, 0, 0)),
            '-x': ([(cx - hx, cy - hy, cz + hz), (cx - hx, cy - hy, cz - hz), (cx - hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz + hz)], (-1, 0, 0)),
            '+y': ([(cx - hx, cy + hy, cz - hz), (cx + hx, cy + hy, cz - hz), (cx + hx, cy + hy, cz + hz), (cx - hx, cy + hy, cz + hz)], (0, 1, 0)),
            '-y': ([(cx - hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz + hz), (cx + hx, cy - hy, cz - hz), (cx - hx, cy - hy, cz - hz)], (0, -1, 0)),
            '+z': ([(cx + hx, cy - hy, cz + hz), (cx - hx, cy - hy, cz + hz), (cx - hx, cy + hy, cz + hz), (cx + hx, cy + hy, cz + hz)], (0, 0, 1)),
            '-z': ([(cx - hx, cy - hy, cz - hz), (cx + hx, cy - hy, cz - hz), (cx + hx, cy + hy, cz - hz), (cx - hx, cy + hy, cz - hz)], (0, 0, -1)),
        }
        for key, (verts, n) in spec.items():
            if faces != 'all' and key not in faces:
                continue
            self.p.quad([self.pt(*v) for v in verts], self.dir(*n), mat)

    def bb(self, x0, y0, z0, x1, y1, z1, mat, faces='all'):
        self.box(((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), (abs(x1 - x0), abs(y1 - y0), abs(z1 - z0)), mat, faces)


# ---------------------------------------------------------------------------
# Shell
# ---------------------------------------------------------------------------
def wall_pieces(w, own):
    """Solid pieces (a0,a1,y0,y1): columns split at every opening edge, minus every
    opening covering that column (stacked windows and door/window pairs included).
    Unity's BuildSlice.Police uses the same algorithm for its box colliders."""
    xs = sorted({w['a0'], w['a1']} | {v for o in own for v in (o['at'] - o['width'] / 2, o['at'] + o['width'] / 2)})
    cols = []
    for s0, s1 in zip(xs, xs[1:]):
        if s1 - s0 < .001:
            continue
        mid = (s0 + s1) / 2
        holes = sorted((o['bottom'], o['top']) for o in own if o['at'] - o['width'] / 2 < mid < o['at'] + o['width'] / 2)
        spans, y = [], w['y0']
        for b, t in holes:
            if b > y + .001:
                spans.append((y, b))
            y = max(y, t)
        if w['y1'] > y + .001:
            spans.append((y, w['y1']))
        if cols and cols[-1][1] == s0 and cols[-1][2] == spans:
            cols[-1] = (cols[-1][0], s1, spans)
        else:
            cols.append((s0, s1, spans))
    return [(a0, a1, y0, y1) for a0, a1, spans in cols for y0, y1 in spans]


def wall_box(w, a0, a1, y0, y1, grow=0.0):
    if w['axis'] == 'x':
        return (a0, y0, w['c'] - w['t'] / 2 - grow, a1, y1, w['c'] + w['t'] / 2 + grow)
    return (w['c'] - w['t'] / 2 - grow, y0, a0, w['c'] + w['t'] / 2 + grow, y1, a1)


def face_part(finish, level):
    return part('Ext_Facade' if finish == 'ext' else 'Int_Walls')


def build_walls(L):
    W = {w['id']: w for w in L['walls']}
    for w in L['walls']:
        own = [o for o in L['openings'] if o['wall'] == w['id']]
        ext_mat = 'PExt_Stucco'; int_mat = 'PInt_Panel' if w['id'] == 'S_rail' else 'PInt_Plaster'
        for (a0, a1, y0, y1) in wall_pieces(w, own):
            x0, yy0, z0, x1, yy1, z1 = wall_box(w, a0, a1, y0, y1)
            normal_pos = '+z' if w['axis'] == 'x' else '+x'
            normal_neg = '-z' if w['axis'] == 'x' else '-x'
            along = ('+x', '-x') if w['axis'] == 'x' else ('+z', '-z')
            for key, finish in ((normal_pos, w['pos']), (normal_neg, w['neg'])):
                if finish == 'none':
                    continue
                bbox(face_part(finish, w['level']), x0, yy0, z0, x1, yy1, z1, ext_mat if finish == 'ext' else int_mat, faces=key)
            # Reveal/top/end faces: exterior walls carry stucco, partitions plaster. End faces that
            # abut a solid neighbouring piece over their whole height are buried: skip them so no
            # hidden coplanar faces remain (z-fight audit, 2026-10-12).
            reveal_finish = 'ext' if 'ext' in (w['pos'], w['neg']) else 'int'
            rp = face_part(reveal_finish, w['level']); rm = ext_mat if reveal_finish == 'ext' else int_mat
            pieces_all = wall_pieces(w, own)
            ends = []
            if not (a0 > w['a0'] + 1e-6 and any(abs(q[1] - a0) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces_all)):
                ends.append(along[1])
            if not (a1 < w['a1'] - 1e-6 and any(abs(q[0] - a1) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces_all)):
                ends.append(along[0])
            # The court paving runs under a free-standing court wall, so the sill top below its gate
            # would lie in the paving plane. The threshold covers it: leave it out.
            sill = w['pos'] == 'ext' and w['neg'] == 'ext' and any(
                o['kind'] in ('door', 'arch') and abs(o['bottom'] - y1) < 1e-6 and o['at'] - o['width'] / 2 <= a0 + 1e-6 and a1 <= o['at'] + o['width'] / 2 + 1e-6 for o in own)
            bbox(rp, x0, yy0, z0, x1, yy1, z1, rm, faces=ends + ([] if sill else ['+y']) + ['-y'])


def interior_face_segments(L):
    """Yield (wall, side sign, a0, a1, openings) for interior plaster faces."""
    for w in L['walls']:
        own = [o for o in L['openings'] if o['wall'] == w['id']]
        for sign, finish in ((1, w['pos']), (-1, w['neg'])):
            if finish == 'int':
                yield w, sign, own


def room_at(L, x, z, y):
    level = 0 if y < 3.4 else 1
    for r in L['rooms']:
        if r['level'] == level and r['x0'] - .01 <= x <= r['x1'] + .01 and r['z0'] - .01 <= z <= r['z1'] + .01:
            return r
    return None


def build_wall_trims(L):
    """Skirting on interior faces; panelled dado in public ground-floor rooms."""
    g = L['levels']['ground']; up = L['levels']['upper']
    trim = part('Int_Trim')
    for w, sign, own in interior_face_segments(L):
        if w['id'] in ('S_rail',):
            continue
        face = w['c'] + sign * w['t'] / 2
        for base in (g, up):
            if base < w['y0'] - .01 or base + .5 > w['y1']:
                continue
            # Split the face by openings that reach this level.
            cuts = sorted((o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o) for o in own if o['bottom'] < base + 1.0 and o['top'] > base)
            cursor = w['a0'] + (0 if w['id'].startswith('F_') or w['id'][:2] in ('WW', 'EW') else 0)
            spans = []
            for lo, hi, o in cuts:
                spans.append((cursor, lo, o if o['kind'] == 'window' else None))
                if o['kind'] == 'window':
                    spans.append((lo, hi, o))
                cursor = hi
            spans.append((cursor, w['a1'], None))
            for a0, a1, win in spans:
                if a1 - a0 < .05:
                    continue
                mid = (a0 + a1) / 2
                px, pz = ((mid, face + sign * .15) if w['axis'] == 'x' else (face + sign * .15, mid))
                r = room_at(L, px, pz, base + .5)
                if not r:
                    continue
                a0c, a1c = max(a0, r['x0'] if w['axis'] == 'x' else r['z0']), min(a1, r['x1'] if w['axis'] == 'x' else r['z1'])
                if a1c - a0c < .05:
                    continue
                dado = r['public'] and r['level'] == 0 and r['id'] not in ('stair', 'passage')
                top = base + (.85 if dado else .12)
                if win is not None and top > win['bottom'] - .02:
                    top = min(top, win['bottom'] - .02)
                    if top <= base + .05:
                        continue
                depth = .025
                if w['axis'] == 'x':
                    z0, z1 = sorted((face, face + sign * depth))
                    bbox(trim, a0c, base, z0, a1c, top, z1, 'PInt_Panel', faces=['+z' if sign > 0 else '-z', '+y', '+x', '-x'])
                else:
                    x0, x1 = sorted((face, face + sign * depth))
                    bbox(trim, x0, base, a0c, x1, top, a1c, 'PInt_Panel', faces=['+x' if sign > 0 else '-x', '+y', '+z', '-z'])


def build_slabs(L):
    for s in L['slabs']:
        k = s['kind']
        if k == 'foundation':
            continue
        if k == 'finish':
            mat = {'oak': 'PInt_Oak', 'tiles': 'PInt_Tiles', 'stone': 'PInt_Stone'}[s['top']]
            bbox(part('Int_Floors'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], mat, faces=['+y'])
        elif k == 'structure':
            bbox(part('Int_Ceilings'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'PInt_Ceiling', faces=['-y'] + (['+z'] if s['id'] == 'upper-east' else []))
        elif k == 'ceiling':
            bbox(part('Int_Ceilings'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'PInt_Ceiling', faces=['-y'])
        elif k in ('loggia', 'court'):
            # Only the open edges are visible; sides against the range/wing walls are buried.
            faces = {'loggia-west': ['+y', '+x', '+z'], 'loggia-east': ['+y', '-x', '+z'], 'court': ['+y', '+z']}[s['id']]
            bbox(part('Ext_Court'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'PExt_Flags', faces=faces)
        elif k == 'step':
            bbox(part('Ext_Court' if s['id'] == 'court-step' else 'Ext_Facade'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'PExt_Ashlar', faces=['+y', '+x', '-x', '+z', '-z'])


def build_thresholds(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        if o['kind'] not in ('door', 'arch'):
            continue
        w = W[o['wall']]
        lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
        grow = .05 if o['exterior'] else .02
        x0, y0, z0, x1, y1, z1 = wall_box(w, lo + .001, hi - .001, o['bottom'] - .03, o['bottom'] + .006, grow)
        bbox(part('Int_Trim' if not o['exterior'] else 'Ext_Facade'), x0, y0, z0, x1, y1, z1, 'PExt_Ashlar' if o['exterior'] else 'PInt_Panel', faces=['+y', '+x', '-x', '+z', '-z'])


def build_stairs(L):
    st = part('Int_Stairs'); g = L['levels']['ground']
    for s in L['stairs']:
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'], s['z1'], 'PInt_Oak', faces=['+y'])
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'] - .02, s['z1'], 'PInt_Panel', faces=['+x', '-x', '+z', '-z'])
        # Nosing lip on the riser edge facing down-flight.
        if s['id'].startswith('A'):
            bbox(st, s['x0'], s['top'] - .04, s['z0'] - .025, s['x1'], s['top'], s['z0'], 'PInt_Oak', faces=['+y', '-z', '-y'])
        elif s['id'].startswith('B'):
            bbox(st, s['x0'], s['top'] - .04, s['z1'], s['x1'], s['top'], s['z1'] + .025, 'PInt_Oak', faces=['+y', '+z', '-y'])
    # Brass handrails on the spine wall (both flights): one sloped rail per flight, 0.9m above the pitch line.
    rail = part('Int_Trim'); A = [s for s in L['stairs'] if s['id'].startswith('A')]; B = [s for s in L['stairs'] if s['id'].startswith('B')]
    for flight, x in ((A, 20.88 + .045), (B, 20.72 - .045)):
        first, last = flight[0], flight[-1]
        za, zb = (first['z0'], last['z1']) if flight is A else (first['z1'], last['z0'])
        ya, yb = first['top'] + .9, last['top'] + .9
        r = .025
        corners = lambda z, y: [(x - r, y - r, z), (x + r, y - r, z), (x + r, y + r, z), (x - r, y + r, z)]
        a, b = corners(za, ya), corners(zb, yb)
        dz, dy = zb - za, yb - ya; ln = math.hypot(dz, dy)
        up = (0, dz / ln, -dy / ln) if dz > 0 else (0, -dz / ln, dy / ln)
        rail.quad([a[0], a[1], b[1], b[0]], (0, -up[1], -up[2]), 'PInt_Brass')
        rail.quad([a[3], b[3], b[2], a[2]], up, 'PInt_Brass')
        rail.quad([a[1], a[2], b[2], b[1]], (1, 0, 0), 'PInt_Brass')
        rail.quad([a[0], b[0], b[3], a[3]], (-1, 0, 0), 'PInt_Brass')
        # Brackets into the spine wall at both ends.
        for z, y in ((za + (zb - za) * .1, ya + (yb - ya) * .1), (za + (zb - za) * .9, ya + (yb - ya) * .9)):
            wall_x = 20.88 if flight is A else 20.72
            bbox(rail, min(x, wall_x), y - .1, z - .012, max(x, wall_x), y - .02, z + .012, 'PInt_Brass')


def roof_uv(p_eave_a, along, up_dir):
    def f(v):
        d = Vector(v) - Vector(p_eave_a)
        return (d.dot(Vector(along)), d.dot(Vector(up_dir)))
    return f


def slope_quad(p, verts, normal, mat, along, up_dir, tile):
    uvf = roof_uv(verts[0], along, up_dir)
    uvs = [tuple(c / tile for c in uvf(v)) for v in verts]
    p.faces.append(([tuple(v) for v in verts], tuple(normal), mat, uvs))


def build_roofs(L):
    roof = part('Ext_Roof'); tile = 1.5
    for r in L['roofs']:
        x0, z0, x1, z1, e, h = r['x0'], r['z0'], r['x1'], r['z1'], r['eaves'], r['ridge']
        if r['kind'] == 'hip':
            zm = (z0 + z1) / 2; half = (z1 - z0) / 2; xa, xb = x0 + half, x1 - half
            slope = (h - e) / half; ln = math.hypot(1, slope)
            def nrm(dx, dz):
                return (dx * slope / ln, 1 / ln, dz * slope / ln)
            up_s = (0, slope / ln, 1 / ln); up_n = (0, slope / ln, -1 / ln)
            slope_quad(roof, [(x0, e, z0), (x1, e, z0), (xb, h, zm), (xa, h, zm)], nrm(0, -1), 'PExt_Slate', (1, 0, 0), up_s, tile)
            slope_quad(roof, [(x1, e, z1), (x0, e, z1), (xa, h, zm), (xb, h, zm)], nrm(0, 1), 'PExt_Slate', (-1, 0, 0), up_n, tile)
            slope_quad(roof, [(x0, e, z1), (x0, e, z0), (xa, h, zm), (xa, h, zm)], nrm(-1, 0), 'PExt_Slate', (0, 0, -1), (slope / ln, 1 / ln, 0), tile)
            slope_quad(roof, [(x1, e, z0), (x1, e, z1), (xb, h, zm), (xb, h, zm)], nrm(1, 0), 'PExt_Slate', (0, 0, 1), (-slope / ln, 1 / ln, 0), tile)
            # Fascia and soffit around the overhang
            for (a, b, c, d, n) in (((x0, z0), (x1, z0), None, None, (0, 0, -1)), ((x1, z1), (x0, z1), None, None, (0, 0, 1)),
                                    ((x0, z1), (x0, z0), None, None, (-1, 0, 0)), ((x1, z0), (x1, z1), None, None, (1, 0, 0))):
                roof.quad([(a[0], e - .2, a[1]), (b[0], e - .2, b[1]), (b[0], e, b[1]), (a[0], e, a[1])], n, 'PExt_Paint')
            for (sx0, sz0, sx1, sz1) in ((x0, z0, x1, z0 + .35), (x0, z1 - .35, x1, z1), (x0, z0 + .35, x0 + .35, z1 - .35), (x1 - .35, z0 + .35, x1, z1 - .35)):
                bbox(roof, sx0, e - .2, sz0, sx1, e - .2, sz1, 'PExt_Paint', faces=['-y'])
            # Moulded cornice band on the wall head below the soffit.
            for (cx0, cz0, cx1, cz1, n) in ((-.18, -.18, 23.18, 0, '-z'), (-.18, 12, 23.18, 12.18, '+z'), (-.18, 0, 0, 12, '-x'), (23, 0, 23.18, 12, '+x')):
                bbox(part('Ext_Facade'), cx0, e - .45, cz0, cx1, e - .2, cz1, 'PExt_Ashlar', faces=[n, '-y', '+x', '-x', '+z', '-z'])
        elif r['kind'] == 'gable':
            xm = (0 + 5.5) / 2 if x0 < 10 else (17.5 + 23) / 2
            wx0, wx1 = (0, 5.5) if x0 < 10 else (17.5, 23)
            run = xm - wx0; slope = (h - e) / run; ln = math.hypot(1, slope)
            yl = e - slope * (wx0 - x0); yr = e - slope * (x1 - wx1)
            slope_quad(roof, [(x0, yl, z1), (x0, yl, z0), (xm, h, z0), (xm, h, z1)], (-slope / ln, 1 / ln, 0), 'PExt_Slate', (0, 0, -1), (slope / ln, 1 / ln, 0), tile)
            slope_quad(roof, [(x1, yr, z0), (x1, yr, z1), (xm, h, z1), (xm, h, z0)], (slope / ln, 1 / ln, 0), 'PExt_Slate', (0, 0, 1), (-slope / ln, 1 / ln, 0), tile)
            # Undersides of the overhangs and the north verge.
            vx0 = x0 - (.025 if abs(x0 - wx0) < .01 else 0); vx1 = x1 + (.025 if abs(x1 - wx1) < .01 else 0)
            roof.quad([(vx0, yl - .12, z0), (vx0, yl - .12, z1), (vx0, yl, z1), (vx0, yl, z0)], (-1, 0, 0), 'PExt_Paint')
            roof.quad([(vx1, yr - .12, z1), (vx1, yr - .12, z0), (vx1, yr, z0), (vx1, yr, z1)], (1, 0, 0), 'PExt_Paint')
            tri(roof, [(x0, yl, z1), (xm, h, z1), (x1, yr, z1)], (0, 0, 1), 'PExt_Paint')
            # Gable infill wall above the north wall head (z 23.6-24) and timber verge.
            gz0, gz1 = 23.6, 24.0
            for z, n in ((gz1, (0, 0, 1)), (gz0, (0, 0, -1))):
                verts = [(wx0, e, z), (wx1, e, z), (xm, h - .05, z)] if n[2] > 0 else [(wx1, e, z), (wx0, e, z), (xm, h - .05, z)]
                tri(part('Ext_Facade') if n[2] > 0 else part('Int_Walls'), verts, n, 'PExt_Stucco' if n[2] > 0 else 'PInt_Plaster')
        elif r['kind'] == 'leanto':
            hi_x, lo_x = (x0, x1) if r['low'] > 0 else (x1, x0)
            slope = (h - e) / abs(x1 - x0); ln = math.hypot(1, slope); sx = 1 if lo_x > hi_x else -1
            n = (sx * slope / ln, 1 / ln, 0)
            verts = [(hi_x, h, z0), (lo_x, e, z0), (lo_x, e, z1), (hi_x, h, z1)] if sx > 0 else [(hi_x, h, z1), (lo_x, e, z1), (lo_x, e, z0), (hi_x, h, z0)]
            slope_quad(roof, verts, n, 'PExt_Slate', (0, 0, 1) if sx > 0 else (0, 0, -1), (-sx / ln, slope / ln, 0), tile)
            # Timber boarded soffit underneath, and the eave fascia.
            under = [(hi_x, h - .1, z1), (lo_x, e - .1, z1), (lo_x, e - .1, z0), (hi_x, h - .1, z0)] if sx > 0 else [(hi_x, h - .1, z0), (lo_x, e - .1, z0), (lo_x, e - .1, z1), (hi_x, h - .1, z1)]
            part('Ext_Loggia').quad(under, (-n[0], -n[1], 0), 'PExt_Timber')
            part('Ext_Loggia').quad([(lo_x, e - .1, z0), (lo_x, e - .1, z1), (lo_x, e, z1), (lo_x, e, z0)] if sx > 0 else [(lo_x, e - .1, z1), (lo_x, e - .1, z0), (lo_x, e, z0), (lo_x, e, z1)], (sx, 0, 0), 'PExt_Paint')
            part('Ext_Loggia').quad([(lo_x, e - .1, z1), (hi_x, h - .1, z1), (hi_x, h, z1), (lo_x, e, z1)] if sx > 0 else [(hi_x, h - .1, z1), (lo_x, e - .1, z1), (lo_x, e, z1), (hi_x, h, z1)], (0, 0, 1), 'PExt_Paint')
    # Chimney stacks with caps.
    for (cx, cz, y0, y1) in ((4.0, 9.4, 7.4, 9.7), (19.2, 2.6, 7.4, 9.7), (20.25, 15.3, 4.6, 5.9)):
        bbox(roof, cx - .38, y0, cz - .28, cx + .38, y1, cz + .28, 'PExt_Ashlar', faces=['+x', '-x', '+z', '-z'])
        bbox(roof, cx - .46, y1, cz - .36, cx + .46, y1 + .12, cz + .36, 'PExt_Ashlar')
        for dx in (-.16, .16):
            cylinder(roof, cx + dx, cz, .07, y1 + .12, y1 + .42, 'PExt_Gutter', seg=8)


def build_loggias(L):
    lg = part('Ext_Loggia')
    for c in L['columns']:
        x, z, w = c['x'], c['z'], c['w']
        bbox(lg, x - .17, c['y0'], z - .17, x + .17, c['y0'] + .32, z + .17, 'PExt_Ashlar', faces=['+x', '-x', '+z', '-z', '+y'])
        bbox(lg, x - w / 2 + .03, c['y0'] + .32, z - w / 2 + .03, x + w / 2 - .03, c['y1'] - .12, z + w / 2 - .03, 'PExt_Paint', faces=['+x', '-x', '+z', '-z'])
        bbox(lg, x - .15, c['y1'] - .12, z - .15, x + .15, c['y1'], z + .15, 'PExt_Paint', faces=['+x', '-x', '+z', '-z', '-y'])
    for b in L['beams']:
        bbox(lg, b['x0'], b['y0'], b['z0'], b['x1'], b['y1'], b['z1'], 'PExt_Timber', faces=['+x', '-x', '-y', '+z', '-z'])


def build_exterior_details(L):
    fac = part('Ext_Facade'); g = L['levels']['ground']
    W = {w['id']: w for w in L['walls']}
    ext_walls = [w for w in L['walls'] if 'ext' in (w['pos'], w['neg']) and w['y0'] < 0 and w['id'] != 'F_W']
    doors = [o for o in L['openings'] if o['kind'] == 'door' and o['exterior']]
    # Ashlar plinth band (cut at doors).
    for w in ext_walls:
        # A free-standing court wall is exterior on both faces: it carries the band on each, and
        # the band stops against the wing plinths (.05 proud of the wing walls) instead of
        # running into them.
        yard = w['pos'] == 'ext' and w['neg'] == 'ext'
        for sign in ((1, -1) if yard else ((1,) if w['pos'] == 'ext' else (-1,))):
            face = w['c'] + sign * w['t'] / 2
            cuts = sorted((o['at'] - o['width'] / 2, o['at'] + o['width'] / 2) for o in doors if o['wall'] == w['id'])
            # Axis-z walls stop at the through walls; extend to the outer corner (+/-thickness) when that end is exterior.
            # (Pieces meet exactly: F_E stops at z 12 where the east wing's plinth begins.)
            ext0 = -.05 if yard else .04 if w['axis'] == 'x' else (.44 if w['id'] == 'F_E' else 0)
            ext1 = -.05 if yard else .04 if w['axis'] == 'x' else (.4 if w['id'] == 'F_E' else .44 if w['id'] in ('WW_W', 'WW_E', 'EW_W', 'EW_E') else 0)
            a, spans = w['a0'] - ext0, []
            for lo, hi in cuts:
                spans.append((a, lo)); a = hi
            spans.append((a, w['a1'] + ext1))
            for a0, a1 in spans:
                if a1 - a0 < .02:
                    continue
                # Span ends that start at an inside corner (ext0/ext1 <= 0 at the wall ends) are buried.
                lo_end = not (abs(a0 - (w['a0'] - ext0)) < 1e-6 and ext0 <= 0)
                hi_end = not (abs(a1 - (w['a1'] + ext1)) < 1e-6 and ext1 <= 0)
                if w['axis'] == 'x':
                    z0, z1 = sorted((face, face + sign * .05))
                    bbox(fac, a0, -.1, z0, a1, .62, z1, 'PExt_Ashlar', faces=['+z' if sign > 0 else '-z', '+y'] + (['-x'] if lo_end else []) + (['+x'] if hi_end else []))
                else:
                    x0, x1 = sorted((face, face + sign * .05))
                    bbox(fac, x0, -.1, a0, x1, .62, a1, 'PExt_Ashlar', faces=['+x' if sign > 0 else '-x', '+y'] + (['-z'] if lo_end else []) + (['+z'] if hi_end else []))
        if yard:
            # Stone coping; its ends die into the wing quoins (.04 proud of the wing corners).
            c0, c1 = w['c'] - w['t'] / 2 - .04, w['c'] + w['t'] / 2 + .04
            if w['axis'] == 'x':
                bbox(fac, w['a0'] + .04, w['y1'], c0, w['a1'] - .04, w['y1'] + .08, c1, 'PExt_Ashlar', faces=['+y', '-y', '+z', '-z'])
            else:
                bbox(fac, c0, w['y1'], w['a0'] + .04, c1, w['y1'] + .08, w['a1'] - .04, 'PExt_Ashlar', faces=['+y', '-y', '+x', '-x'])
    # String course at the first floor on the front range (S, E, W, N upper).
    for wid, sign in (('F_S', -1), ('F_E', 1)):
        w = W[wid]; face = w['c'] + sign * w['t'] / 2
        cuts = sorted((o['at'] - o['width'] / 2 - .02, o['at'] + o['width'] / 2 + .02) for o in L['openings'] if o['wall'] == wid and o['bottom'] < 3.6 and o['top'] > 3.3)
        a, spans = w['a0'] - (.06 if w['axis'] == 'x' else 0), []
        for lo, hi in cuts:
            spans.append((a, lo)); a = hi
        spans.append((a, w['a1'] + (.06 if w['axis'] == 'x' else 0)))
        for a0, a1 in spans:
            if w['axis'] == 'x':
                z0, z1 = sorted((face, face + sign * .07))
                bbox(fac, a0, 3.36, z0, a1, 3.56, z1, 'PExt_Ashlar', faces=['+z' if sign > 0 else '-z', '+y', '-y', '+x', '-x'])
            else:
                x0, x1 = sorted((face, face + sign * .07))
                bbox(fac, x0, 3.36, a0, x1, 3.56, a1, 'PExt_Ashlar', faces=['+x' if sign > 0 else '-x', '+y', '-y', '+z', '-z'])
    # Quoins on free corners (not the party wall shared with B03 at x=0, z<12).
    corners = [(23, 0, 1, -1, .62, 6.4), (23, 12, 1, 1, 3.62, 6.4), (0, 12, -1, 1, 3.62, 6.4),
               (0, 24, -1, 1, .62, 3.3), (5.5, 24, 1, 1, .62, 3.3), (17.5, 24, -1, 1, .62, 3.3), (23, 24, 1, 1, .62, 3.3)]
    for cx, cz, sx, sz, y, top in corners:
        k = 0
        while y < top:
            lx, lz = (.5, .28) if k % 2 == 0 else (.28, .5)
            x0, x1 = sorted((cx + sx * .04, cx - sx * lx))
            z0, z1 = sorted((cz + sz * .04, cz - sz * lz))
            bbox(fac, x0, y, z0, x1, y + .3, z1, 'PExt_Ashlar', faces=['+x' if sx > 0 else '-x', '+z' if sz > 0 else '-z', '+y', '-y'])
            y += .32; k += 1
    # Gutters on eaves and downpipes on the street corners.
    gut = part('Ext_Roof')
    for (x0, z0, x1, z1) in ((-0.35, -0.5, 23.35, -0.35), (-0.35, 12.35, 23.35, 12.5)):
        bbox(gut, x0, 6.48, z0, x1, 6.58, z1, 'PExt_Gutter')
    for x, z in ((0.25, -.12), (22.75, -.12), (23.12, 11.75)):
        cylinder(gut, x, z, .045, .0, 6.5, 'PExt_Gutter', seg=8)
    # Front door surround, pilasters and the lettered sign board (lettering in Unity).
    fd = next(o for o in L['openings'] if o['id'] == 'front-door')
    x = fd['at']; half = fd['width'] / 2
    for side in (-1, 1):
        px = x + side * (half + .2)
        bbox(fac, px - .16, .62, -.1, px + .16, 3.25, 0, 'PExt_Ashlar', faces=['-z', '+x', '-x', '+y'])
    bbox(fac, x - half - .45, 3.25, -.14, x + half + .45, 3.36, 0, 'PExt_Ashlar', faces=['-z', '+x', '-x', '+y', '-y'])
    # The lettered sign board itself is built in Unity (BuildSlice.RailBoard) above the entablature.
    # Window surrounds and sills.
    for o in L['openings']:
        if o['kind'] != 'window' or not o['exterior']:
            continue
        w = W[o['wall']]; sign = 1 if w['pos'] == 'ext' else -1; face = w['c'] + sign * w['t'] / 2
        lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
        if w['axis'] == 'x':
            z0, z1 = sorted((face, face + sign * .09))
            bbox(fac, lo - .1, o['bottom'] - .1, z0, hi + .1, o['bottom'] - .004, z1, 'PExt_Ashlar', faces=['+y', '-y', '+z' if sign > 0 else '-z', '+x', '-x'])
            if o['wall'] in ('F_S', 'F_N_U') and o['style'] == 'sash':
                s0, s1 = sorted((face, face + sign * .03))
                for a0, a1 in ((lo - .12, lo), (hi, hi + .12)):
                    bbox(fac, a0, o['bottom'], s0, a1, o['top'] + .12, s1, 'PExt_Ashlar', faces=['+z' if sign > 0 else '-z', '+x', '-x'])
                bbox(fac, lo, o['top'], s0, hi, o['top'] + .12, s1, 'PExt_Ashlar', faces=['+z' if sign > 0 else '-z', '+y', '-y'])
        else:
            x0, x1 = sorted((face, face + sign * .09))
            bbox(fac, x0, o['bottom'] - .1, lo - .1, x1, o['bottom'] - .004, hi + .1, 'PExt_Ashlar', faces=['+y', '-y', '+x' if sign > 0 else '-x', '+z', '-z'])


# ---------------------------------------------------------------------------
# Joinery: windows, door frames, leaves, hatch.
# ---------------------------------------------------------------------------
def build_window(L, o, w):
    jo = part('Ext_Windows') if o['exterior'] else part('Int_Trim')
    glass = part('Glass')
    sign = 1 if w['pos'] == 'ext' else -1
    # Frame plane near the exterior face, inset 0.1m.
    plane = w['c'] + sign * (w['t'] / 2 - .1) if o['exterior'] else w['c']
    lo, hi, b, t = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom'], o['top']
    fw, fd = .06, .1

    def bar(a0, a1, y0, y1, depth=fd, offset=0.0, mat='PExt_Paint'):
        c = plane + sign * offset
        if w['axis'] == 'x':
            bbox(jo, a0, y0, c - depth / 2, a1, y1, c + depth / 2, mat)
        else:
            bbox(jo, c - depth / 2, y0, a0, c + depth / 2, y1, a1, mat)

    bar(lo, lo + fw, b, t); bar(hi - fw, hi, b, t); bar(lo + fw, hi - fw, b, b + fw); bar(lo + fw, hi - fw, t - fw, t)
    style = o['style']
    if style == 'barred':
        for i in range(1, 5):
            a = lo + (hi - lo) * i / 5
            bar(a - .012, a + .012, b + fw, t - fw, depth=.024, offset=-sign * .08, mat='PExt_Iron')
    if style in ('sash', 'high'):
        mid = (b + t) / 2 if style == 'sash' else None
        if mid is not None:
            bar(lo + fw, hi - fw, mid - .03, mid + .03, depth=.07)
        cols = 2 if (hi - lo) > .8 else 1
        for i in range(1, cols):
            a = lo + (hi - lo) * i / cols
            bar(a - .015, a + .015, b + fw, t - fw, depth=.05)
        rows = [(b + t) / 2 - (t - b) / 4, (b + t) / 2 + (t - b) / 4] if style == 'sash' and (t - b) > 1.3 else []
        for y in rows:
            bar(lo + fw, hi - fw, y - .012, y + .012, depth=.04)
    # Glass pane in the frame plane.
    if w['axis'] == 'x':
        for n in ((0, 0, 1), (0, 0, -1)):
            verts = [(lo + fw, b + fw, plane), (hi - fw, b + fw, plane), (hi - fw, t - fw, plane), (lo + fw, t - fw, plane)]
            if n[2] < 0:
                verts = verts[::-1]
            glass.quad(verts, n, 'P_Glass')
    else:
        for n in ((1, 0, 0), (-1, 0, 0)):
            verts = [(plane, b + fw, hi - fw), (plane, b + fw, lo + fw), (plane, t - fw, lo + fw), (plane, t - fw, hi - fw)]
            if n[0] < 0:
                verts = verts[::-1]
            glass.quad(verts, n, 'P_Glass')
    # Interior window board.
    if o['exterior']:
        inner = w['c'] - sign * w['t'] / 2
        if w['axis'] == 'x':
            z0, z1 = sorted((inner - sign * .05, plane - sign * .05))
            bbox(part('Int_Trim'), lo - .05, b, z0, hi + .05, b + .025, z1, 'PInt_Panel', faces=['+y', '+x', '-x', '+z', '-z'])
        else:
            x0, x1 = sorted((inner - sign * .05, plane - sign * .05))
            bbox(part('Int_Trim'), x0, b, lo - .05, x1, b + .025, hi + .05, 'PInt_Panel', faces=['+y', '+x', '-x', '+z', '-z'])


def casing(L, o, w):
    """Architraves on both faces of door/arch/hatch openings."""
    trim_int = part('Int_Trim'); trim_ext = part('Ext_Facade')
    lo, hi, b, t = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom'], o['top']
    for sign, finish in ((1, w['pos']), (-1, w['neg'])):
        if finish == 'none':
            continue
        p = trim_ext if finish == 'ext' else trim_int
        mat = 'PExt_DoorOak' if finish == 'ext' else 'PInt_Panel'
        if finish == 'ext' and o['id'] == 'front-door':
            continue
        face = w['c'] + sign * w['t'] / 2
        d0, d1 = sorted((face, face + sign * .025))
        bw = .08
        # Exterior casings stand on the stone plinth instead of sharing its end faces; hatch
        # casings start on the counter top (the counter sits on the sill).
        cb = max(b, .62) if finish == 'ext' and w['y0'] < 0 else (b + .045 if o['kind'] == 'hatch' else b)
        if w['axis'] == 'x':
            bbox(p, lo - bw, cb, d0, lo, t + bw, d1, mat); bbox(p, hi, cb, d0, hi + bw, t + bw, d1, mat); bbox(p, lo, t, d0, hi, t + bw, d1, mat)
        else:
            bbox(p, d0, cb, lo - bw, d1, t + bw, lo, mat); bbox(p, d0, cb, hi, d1, t + bw, hi + bw, mat); bbox(p, d0, t, lo, d1, t + bw, hi, mat)


def jamb_lining(o, w):
    p = part('Ext_Windows') if o['exterior'] else part('Int_Trim')
    mat = 'PExt_DoorOak' if o['exterior'] else 'PInt_Panel'
    lo, hi, b, t = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom'], o['top']
    c0, c1 = w['c'] - w['t'] / 2, w['c'] + w['t'] / 2
    k = .03
    if w['axis'] == 'x':
        bbox(p, lo, b, c0, lo + k, t, c1, mat, faces=['+x']); bbox(p, hi - k, b, c0, hi, t, c1, mat, faces=['-x'])
        bbox(p, lo, t - k, c0, hi, t, c1, mat, faces=['-y'])
    else:
        bbox(p, c0, b, lo, c1, t, lo + k, mat, faces=['+z']); bbox(p, c0, b, hi - k, c1, t, hi, mat, faces=['-z'])
        bbox(p, c0, t - k, lo, c1, t, hi, mat, faces=['-y'])


LEAVES = []


def build_leaf(o, w, index, count):
    """Leaf geometry built closed, relative to its hinge pivot."""
    lo, hi, b, t = o['at'] - o['width'] / 2 + .03, o['at'] + o['width'] / 2 - .03, o['bottom'] + .01, o['top'] - .03
    if o['style'] == 'front':
        t = b + 2.45
    span = (hi - lo) / count
    a0 = lo + index * span; a1 = a0 + span
    hinge_at_low = (index == 0) if count == 2 else (o['hinge'] == 'a0')
    hinge_a = (a0 + .002) if hinge_at_low else (a1 - .002)
    plane = w['c'] - (w['t'] / 2 - .06) * o['swing'] * -1 if o['exterior'] else w['c']
    plane = w['c'] + o['swing'] * (w['t'] / 2 - .05) if o['exterior'] else w['c']
    pivot = (hinge_a, b, plane) if w['axis'] == 'x' else (plane, b, hinge_a)
    name = f"Leaf_{o['id']}_{index}"
    p = part(name, pivot)
    th = .045
    mat = {'front': 'PExt_DoorOak', 'glazed': 'PExt_DoorOak', 'panel': 'PInt_Panel', 'iron': 'PInt_Iron'}[o['style']]
    if o['exterior'] and o['style'] == 'panel':
        mat = 'PExt_DoorOak'

    def leaf_box(s0, s1, y0, y1, depth, offset=0.0, m=None):
        if w['axis'] == 'x':
            bbox(p, s0, y0, plane + offset - depth / 2, s1, y1, plane + offset + depth / 2, m or mat)
        else:
            bbox(p, plane + offset - depth / 2, y0, s0, plane + offset + depth / 2, y1, s1, m or mat)

    s0, s1 = a0 + .004, a1 - .004
    if o['style'] == 'iron':
        bar = .04
        leaf_box(s0, s1, b, b + bar, .04); leaf_box(s0, s1, t - bar, t, .04); leaf_box(s0, s0 + bar, b, t, .04); leaf_box(s1 - bar, s1, b, t, .04)
        leaf_box(s0, s1, b + 1.0, b + 1.04, .03)
        n = 6
        for i in range(1, n):
            a = s0 + (s1 - s0) * i / n
            leaf_box(a - .011, a + .011, b + bar, t - bar, .022)
        lock = s1 - .12 if hinge_at_low else s0 + .02
        leaf_box(lock, lock + .1, b + .95, b + 1.15, .07)
    else:
        glazed = o['style'] in ('front', 'glazed')
        rail = .12
        leaf_box(s0, s1, b, b + .2, th); leaf_box(s0, s1, t - rail, t, th)
        leaf_box(s0, s0 + rail, b, t, th); leaf_box(s1 - rail, s1, b, t, th)
        mid = b + 1.0
        leaf_box(s0, s1, mid - .06, mid + .06, th)
        # Lower raised panel (both faces) and upper glass or panel.
        leaf_box(s0 + rail, s1 - rail, b + .2, mid - .06, th * .6)
        for off in (-th * .45, th * .45):
            leaf_box(s0 + rail + .04, s1 - rail - .04, b + .26, mid - .12, .012, offset=off)
        if glazed:
            # Glass moves with the leaf: second material on the same object.
            gl = (s0 + rail, s1 - rail, mid + .06, t - rail)
            if w['axis'] == 'x':
                for nz in (1, -1):
                    verts = [(gl[0], gl[2], plane), (gl[1], gl[2], plane), (gl[1], gl[3], plane), (gl[0], gl[3], plane)]
                    p.quad(verts if nz > 0 else verts[::-1], (0, 0, nz), 'P_Glass')
            else:
                for nx in (1, -1):
                    verts = [(plane, gl[2], gl[1]), (plane, gl[2], gl[0]), (plane, gl[3], gl[0]), (plane, gl[3], gl[1])]
                    p.quad(verts if nx > 0 else verts[::-1], (nx, 0, 0), 'P_Glass')
            mid2 = (s0 + s1) / 2
            leaf_box(mid2 - .02, mid2 + .02, mid + .06, t - rail, th * .7)
        else:
            leaf_box(s0 + rail, s1 - rail, mid + .06, t - rail, th * .6)
            for off in (-th * .45, th * .45):
                leaf_box(s0 + rail + .04, s1 - rail - .04, mid + .12, t - rail - .06, .012, offset=off)
        # Brass knob and backplate on both faces at the closing edge.
        knob = (s1 - .09) if hinge_at_low else (s0 + .09)
        for off in (-1, 1):
            leaf_box(knob - .025, knob + .025, b + .95, b + 1.13, .012, offset=off * (th / 2 + .006), m='PInt_Brass')
            leaf_box(knob - .02, knob + .02, b + 1.0, b + 1.04, .06, offset=off * (th / 2 + .03), m='PInt_Brass')
    LEAVES.append(dict(object=name, opening=o['id'], fixture=o['fixture'], index=index, count=count,
                       hinge=list(pivot), lowEnd=bool(hinge_at_low), axis=w['axis'], plane=plane,
                       leafCentre=[(a0 + a1) / 2, (b + t) / 2, plane], width=span, height=t - b))


def build_front_transom(o, w):
    jo = part('Ext_Windows'); glass = part('Glass')
    lo, hi = o['at'] - o['width'] / 2 + .03, o['at'] + o['width'] / 2 - .03
    b = o['bottom'] + .01 + 2.45; t = o['top'] - .03
    plane = w['c'] - (w['t'] / 2 - .05)
    bbox(jo, lo, b, plane - .05, hi, b + .08, plane + .05, 'PExt_DoorOak')
    for i in (1, 2):
        a = lo + (hi - lo) * i / 3
        bbox(jo, a - .02, b + .08, plane - .04, a + .02, t, plane + .04, 'PExt_DoorOak')
    for nz in (1, -1):
        verts = [(lo, b + .08, plane), (hi, b + .08, plane), (hi, t, plane), (lo, t, plane)]
        glass.quad(verts if nz > 0 else verts[::-1], (0, 0, nz), 'P_Glass')


def build_hatch(o, w):
    p = part('Int_Trim')
    lo, hi, b = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom']
    bbox(p, lo - .05, b, w['c'] - w['t'] / 2 - .22, hi + .05, b + .045, w['c'] + w['t'] / 2 + .12, 'PInt_FurnLight')
    bbox(p, lo - .02, b - .9, w['c'] + w['t'] / 2, hi + .02, b - .05, w['c'] + w['t'] / 2 + .05, 'PInt_Panel')


def build_joinery(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        w = W[o['wall']]
        if o['kind'] == 'window':
            build_window(L, o, w)
        elif o['kind'] in ('door', 'arch'):
            casing(L, o, w); jamb_lining(o, w)
            if o['leaves']:
                for i in range(o['leaves']):
                    build_leaf(o, w, i, o['leaves'])
            if o['style'] == 'front':
                build_front_transom(o, w)
        elif o['kind'] == 'hatch':
            casing(L, o, w); build_hatch(o, w)


# ---------------------------------------------------------------------------
# Lamps
# ---------------------------------------------------------------------------
def build_lamps(L):
    lv = L['levels']
    for l in L['lamps']:
        fit = part('Lamp_' + l['id']); bulb = part('Bulb_' + l['id'])
        x, y, z = l['x'], l['y'], l['z']
        if l['kind'] == 'pendant':
            ceil = lv['upperCeiling'] if y > 4 else lv['groundCeiling']
            if l['room'] in ('annex', 'archive', 'duty', 'evidence', 'holding'):
                ceil = lv['wingCeiling']
            if l['room'] == 'stair':
                ceil = lv['upperCeiling']
            cylinder(fit, x, z, .06, ceil - .03, ceil, 'PInt_Brass', seg=10)
            bbox(fit, x - .006, y + .12, z - .006, x + .006, ceil - .03, z + .006, 'PInt_Black')
            frustum(fit, x, z, .25, .07, y - .02, y + .12, 'PInt_Enamel', seg=12, cap=True)
            frustum(bulb, x, z, .045, .045, y - .07, y + .0, 'P_Bulb', seg=8, cap=True)
        elif l['kind'] == 'desk':
            base_y = y - .37
            cylinder(fit, x, z, .085, base_y, base_y + .025, 'PInt_Brass', seg=12)
            bbox(fit, x - .012, base_y + .02, z - .012, x + .012, y + .02, z + .012, 'PInt_Brass')
            # Green cased shade (half-cylinder approximated) tilted over the desk.
            for i in range(8):
                a0, a1 = math.pi * i / 8, math.pi * (i + 1) / 8
                p0 = (x - .17, y + .05 + .07 * math.sin(a0), z - .07 * math.cos(a0)); p1 = (x + .17, y + .05 + .07 * math.sin(a0), z - .07 * math.cos(a0))
                p2 = (x + .17, y + .05 + .07 * math.sin(a1), z - .07 * math.cos(a1)); p3 = (x - .17, y + .05 + .07 * math.sin(a1), z - .07 * math.cos(a1))
                mid = (a0 + a1) / 2
                fit.quad([p0, p1, p2, p3], (0, math.sin(mid), -math.cos(mid)), 'PInt_Green')
                fit.quad([p3, p2, p1, p0], (0, -math.sin(mid), math.cos(mid)), 'PInt_Green')
            frustum(bulb, x, z, .028, .028, y - .01, y + .04, 'P_Bulb', seg=8, cap=True)
        elif l['kind'] == 'wall':
            # Lantern on an iron bracket; the wall face is behind (determine from position).
            dz = 1 if z < 1 else 0
            if l['room'] == 'court':
                dx = 1 if x < 11.5 else -1; dz = 0
            else:
                dx = 0; dz = -1
            bx, bz = x - dx * .22, z - dz * .22
            bbox(fit, min(bx, x), y + .24, min(bz, z) - .015, max(bx, x) + .015, y + .27, max(bz, z) + .015, 'PExt_Iron')
            bbox(fit, x - .11, y + .2, z - .11, x + .11, y + .24, z + .11, 'PExt_Iron')
            frustum(fit, x, z, .14, .02, y + .24, y + .36, 'PExt_Iron', seg=4, cap=True)
            bbox(fit, x - .07, y - .16, z - .07, x + .07, y - .13, z + .07, 'PExt_Iron')
            for cx, cz in ((-.085, -.085), (.085, -.085), (.085, .085), (-.085, .085)):
                bbox(fit, x + cx - .01, y - .14, z + cz - .01, x + cx + .01, y + .2, z + cz + .01, 'PExt_Iron')
            bbox(bulb, x - .075, y - .13, z - .075, x + .075, y + .2, z + .075, 'P_LampGlass')


# ---------------------------------------------------------------------------
# Furniture kit
# ---------------------------------------------------------------------------
BOOKS = ['PInt_BookRed', 'PInt_BookGreen', 'PInt_BookBrown', 'PInt_BookBlue']
DRAWERS = []


def legs(f, w, d, h, t=.05, inset=.04, mat='PInt_Furniture'):
    for sx in (-1, 1):
        for sz in (-1, 1):
            cx, cz = sx * (w / 2 - inset - t / 2), sz * (d / 2 - inset - t / 2)
            f.bb(cx - t / 2, 0, cz - t / 2, cx + t / 2, h, cz + t / 2, mat)


def fill_books(f, x0, x1, y, z0, z1, depth_front, boxes=False):
    x = x0 + .02
    while x < x1 - .05:
        if boxes:
            wv = RNG.uniform(.24, .3); hv = .26
            if x + wv > x1 - .02:
                break
            f.bb(x, y, z1 - .3, x + wv - .01, y + hv, z1 - .02, 'PInt_Box', faces=['+z', '+y', '+x', '-x'])
            x += wv
            continue
        wv = RNG.uniform(.025, .06); hv = RNG.uniform(.2, .3)
        if RNG.random() < .08:
            x += RNG.uniform(.05, .15); continue
        if x + wv > x1 - .02:
            break
        f.bb(x, y, z0 + .03, x + wv - .003, y + hv, z1 - .02, RNG.choice(BOOKS), faces=['+z', '+y', '+x', '-x'])
        x += wv


def build_furniture_piece(F, L):
    kind = F['kind']
    if kind == 'desklamp':
        return  # visuals come from the lamp list
    name = 'Furn_' + F['id']
    p = part(name, (F['x'], F['y'], F['z']))
    f = Frame(p, F['x'], F['y'], F['z'], F['yaw'])
    w, d, h = F['w'], F['d'], F['h']
    wood, light = 'PInt_Furniture', 'PInt_FurnLight'
    if kind in ('longtable', 'table', 'desk', 'writingdesk'):
        f.bb(-w / 2, h - .04, -d / 2, w / 2, h, d / 2, light)
        legs(f, w, d, h - .04, t=.06)
        f.bb(-w / 2 + .05, h - .14, -d / 2 + .05, w / 2 - .05, h - .04, d / 2 - .05, wood, faces=['+x', '-x', '+z', '-z', '-y'])
        if kind in ('desk', 'writingdesk'):
            px = w / 2 - .25
            f.bb(px - .2, .05, -d / 2 + .04, px + .2, h - .04, d / 2 - .04, wood)
            for i in range(3):
                y0 = .1 + i * (h - .2) / 3
                f.bb(px - .17, y0, d / 2 - .04, px + .17, y0 + (h - .2) / 3 - .03, d / 2 - .02, light, faces=['+z', '+y', '-y', '+x', '-x'])
                f.bb(px - .04, y0 + .12, d / 2 - .02, px + .04, y0 + .14, d / 2 + .01, 'PInt_Brass')
        if kind == 'writingdesk':
            f.bb(-w / 2, h, -d / 2, w / 2, h + .28, -d / 2 + .1, wood)
            f.bb(-.18, h, -d / 2 + .1, .18, h + .02, -d / 2 + .38, 'PInt_Paper', faces=['+y'])
            cylinder(p, *f.pt(.3, h, -d / 2 + .2)[0:1], f.pt(.3, h, -d / 2 + .2)[2], .03, F['y'] + h, F['y'] + h + .06, 'PInt_Black', seg=8)
    elif kind == 'chair':
        sh = .46
        f.bb(-w / 2 + .02, sh - .04, -d / 2 + .02, w / 2 - .02, sh, d / 2 - .02, light)
        legs(f, w - .04, d - .04, sh - .04, t=.04, inset=.0)
        f.bb(-w / 2 + .02, sh, -d / 2 + .02, -w / 2 + .06, h, -d / 2 + .06, wood)
        f.bb(w / 2 - .06, sh, -d / 2 + .02, w / 2 - .02, h, -d / 2 + .06, wood)
        f.bb(-w / 2 + .06, h - .14, -d / 2 + .025, w / 2 - .06, h - .02, -d / 2 + .055, wood)
        f.bb(-w / 2 + .06, sh + .18, -d / 2 + .03, w / 2 - .06, sh + .24, -d / 2 + .05, wood)
    elif kind == 'bench':
        sh = .45
        f.bb(-w / 2, sh - .05, -d / 2, w / 2, sh, d / 2, light)
        for sx in (-1, 1):
            f.bb(sx * (w / 2 - .12) - .03, 0, -d / 2 + .04, sx * (w / 2 - .12) + .03, sh - .05, d / 2 - .04, wood)
        f.bb(-w / 2 + .1, .12, -.03, w / 2 - .1, .18, .03, wood)
        f.bb(-w / 2, sh, -d / 2, w / 2, sh + .42, -d / 2 + .05, wood)
    elif kind in ('bookshelf', 'doubleshelf'):
        t = .025
        f.bb(-w / 2, 0, -d / 2, -w / 2 + t, h, d / 2, wood); f.bb(w / 2 - t, 0, -d / 2, w / 2, h, d / 2, wood)
        f.bb(-w / 2, h - t, -d / 2, w / 2, h, d / 2, wood); f.bb(-w / 2 + t, 0, -d / 2, w / 2 - t, .08, d / 2, wood)
        if kind == 'bookshelf':
            f.bb(-w / 2 + t, 0, -d / 2, w / 2 - t, h, -d / 2 + .015, wood, faces=['+z'])
        else:
            f.bb(-w / 2 + t, 0, -.008, w / 2 - t, h, .008, wood)
        shelves = 5
        bays = max(1, round(w / .9))
        for i in range(1, bays):
            xx = -w / 2 + w * i / bays
            f.bb(xx - .012, .08, -d / 2, xx + .012, h - t, d / 2, wood)
        for k in range(shelves):
            y = .08 + k * (h - .1) / shelves
            if k > 0:
                f.bb(-w / 2 + t, y - .02, -d / 2, w / 2 - t, y, d / 2, wood)
            for i in range(bays):
                bx0, bx1 = -w / 2 + w * i / bays + .015, -w / 2 + w * (i + 1) / bays - .015
                if kind == 'bookshelf':
                    fill_books(f, bx0, bx1, y, -d / 2 + .015, d / 2, d, boxes=F['extra'] == 'boxes' or F['room'] in ('archive', 'archive2', 'evidence'))
                else:
                    fill_books(f, bx0, bx1, y, 0.01, d / 2, d / 2, boxes=True)
                    sub = Frame(p, *f.pt(0, 0, 0), F['yaw'] + 180)
                    fill_books(sub, -bx1, -bx0, y, 0.01, d / 2, d / 2, boxes=True)
    elif kind == 'lowcabinet':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, wood)
        f.bb(-w / 2 - .02, h, -d / 2 - .02, w / 2 + .02, h + .03, d / 2 + .02, light)
        for i in range(4):
            x0 = -w / 2 + .03 + i * (w - .06) / 4
            f.bb(x0 + .01, .08, d / 2, x0 + (w - .06) / 4 - .01, h - .05, d / 2 + .015, light, faces=['+z', '+x', '-x', '+y', '-y'])
            f.bb(x0 + (w - .06) / 8 - .02, h - .2, d / 2 + .015, x0 + (w - .06) / 8 + .02, h - .17, d / 2 + .035, 'PInt_Brass')
    elif kind == 'cardcabinet':
        labels = F['extra'].split('|')
        movable = F['id'] == 'read-cabinet'
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2 - .02, wood)
        f.bb(-w / 2 - .02, h, -d / 2 - .02, w / 2 + .02, h + .03, d / 2 + .02, light)
        n = len(labels) if movable else 4
        for i in range(n):
            x0 = -w / 2 + .04 + i * (w - .08) / n; x1 = x0 + (w - .08) / n - .02
            if movable:
                # Separate drawer object: origin at the drawer front centre.
                cx = (x0 + x1) / 2; fy0, fy1 = .3, h - .12
                front = f.pt(cx, (fy0 + fy1) / 2, d / 2)
                dn = f'Drawer_{i}'
                dp = part(dn, front)
                df = Frame(dp, F['x'], F['y'], F['z'], F['yaw'])
                df.bb(x0 + .01, fy0, d / 2 - .02, x1 - .01, fy1, d / 2 + .01, light)
                df.bb(x0 + .03, fy0 + .02, -d / 2 + .06, x1 - .03, fy0 + .2, d / 2 - .02, wood, faces=['+x', '-x', '-z', '+y'])
                for k in range(14):
                    zz = -d / 2 + .1 + k * (d - .2) / 14
                    df.bb(x0 + .06, fy0 + .03, zz, x1 - .06, fy0 + .19, zz + .008, 'PInt_Paper')
                df.bb((x0 + x1) / 2 - .07, fy1 - .16, d / 2 + .01, (x0 + x1) / 2 + .07, fy1 - .11, d / 2 + .03, 'PInt_Brass')
                df.bb((x0 + x1) / 2 - .09, fy1 - .3, d / 2 + .01, (x0 + x1) / 2 + .09, fy1 - .22, d / 2 + .015, 'PInt_Paper')
                DRAWERS.append(dict(object=dn, index=i, label=labels[i], front=list(front), axis=list(f.dir(0, 0, 1)), travel=.45))
                f.bb(x0 + .01, .05, d / 2 - .02, x1 - .01, .28, d / 2, wood, faces=['+z'])
            else:
                for r in range(3):
                    y0 = .08 + r * (h - .16) / 3
                    f.bb(x0, y0 + .01, d / 2 - .02, x1, y0 + (h - .16) / 3 - .01, d / 2, light, faces=['+z', '+x', '-x', '+y', '-y'])
                    f.bb((x0 + x1) / 2 - .03, y0 + .18, d / 2, (x0 + x1) / 2 + .03, y0 + .2, d / 2 + .02, 'PInt_Brass')
    elif kind == 'filingcabinet':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, wood)
        for i in range(4):
            y0 = .05 + i * (h - .1) / 4
            f.bb(-w / 2 + .03, y0 + .01, d / 2, w / 2 - .03, y0 + (h - .1) / 4 - .01, d / 2 + .015, light, faces=['+z', '+x', '-x', '+y', '-y'])
            f.bb(-.06, y0 + .2, d / 2 + .015, .06, y0 + .225, d / 2 + .035, 'PInt_Brass')
    elif kind in ('coatstand', 'caperack'):
        f.bb(-.03, 0, -.03, .03, h, .03, wood)
        for a in range(4):
            ang = a * math.pi / 2
            f.bb(-.02 + math.cos(ang) * .12, 0, -.02 + math.sin(ang) * .12, .02 + math.cos(ang) * .12, .04, .02 + math.sin(ang) * .12, wood)
        for a in range(4):
            ang = a * math.pi / 2 + .4
            f.bb(math.cos(ang) * .1 - .015, h - .2, math.sin(ang) * .1 - .015, math.cos(ang) * .1 + .015, h - .14, math.sin(ang) * .1 + .015, 'PInt_Brass')
        if kind == 'caperack':
            for sx in (-1, 1):
                prism(p, [f.pt(sx * .1 - .18, 0, .12)[0::2], f.pt(sx * .1 + .18, 0, .12)[0::2], f.pt(sx * .1 + .12, 0, .22)[0::2], f.pt(sx * .1 - .12, 0, .22)[0::2]][::-1] if False else
                      [tuple(v) for v in (f.pt(sx * .1 - .2, 0, .08)[0::2], f.pt(sx * .1 + .2, 0, .08)[0::2], f.pt(sx * .1 + .14, 0, .24)[0::2], f.pt(sx * .1 - .14, 0, .24)[0::2])],
                      F['y'] + .55, F['y'] + h - .2, 'PInt_Oilcloth')
        else:
            frustum(p, *f.pt(.1, 0, .05)[0::2], .1, .06, F['y'] + h - .13, F['y'] + h - .02, 'PInt_Black', seg=10, cap=True)
    elif kind == 'umbrella':
        cylinder(p, F['x'], F['z'], .14, F['y'], F['y'] + h, 'PInt_Iron', seg=12, top=False)
        for i in range(3):
            a = i * 2.1
            bbox(p, F['x'] + math.cos(a) * .05 - .012, F['y'], F['z'] + math.sin(a) * .05 - .012, F['x'] + math.cos(a) * .05 + .012, F['y'] + h + .3, F['z'] + math.sin(a) * .05 + .012, 'PInt_Black')
    elif kind in ('noticeboard', 'photoboard'):
        f.bb(-w / 2, 0, -.015, w / 2, h, .015, 'PInt_Panel')
        f.bb(-w / 2 + .04, .04, .015, w / 2 - .04, h - .04, .02, 'PInt_Cork', faces=['+z'])
        n = 4 if kind == 'photoboard' else 3
        for i in range(n):
            x0 = -w / 2 + .1 + i * (w - .2) / n
            mat = 'PInt_Photo' if kind == 'photoboard' else 'PInt_Paper'
            ph = .26 if kind == 'photoboard' else .42
            f.bb(x0 + .02, h / 2 - ph / 2 + RNG.uniform(-.05, .05), .02, x0 + (w - .2) / n - .02, h / 2 + ph / 2, .026, mat, faces=['+z'])
    elif kind == 'stove':
        f.bb(-w / 2, .12, -d / 2, w / 2, h, d / 2, 'PInt_Iron')
        legs(f, w, d, .12, t=.05, inset=.02, mat='PInt_Iron')
        f.bb(-w / 2 - .03, h, -d / 2 - .03, w / 2 + .03, h + .04, d / 2 + .03, 'PInt_Iron')
        f.bb(-.15, .3, d / 2, .15, .55, d / 2 + .02, 'PInt_Black')
        ceil = (L['levels']['upperCeiling'] if F['y'] > 3 else (L['levels']['wingCeiling'] if F['room'] in ('duty',) else L['levels']['groundCeiling']))
        cx, _, cz = f.pt(0, 0, -.05)
        cylinder(p, cx, cz, .07, F['y'] + h + .04, ceil, 'PInt_Iron', seg=10, top=False)
    elif kind == 'telephone':
        f.bb(-w / 2, 0, -.07, w / 2, h, 0, 'PInt_Furniture')
        f.bb(-.09, h - .12, 0, .09, h - .04, .05, 'PInt_Black')
        f.bb(-.05, .08, 0, .05, .2, .04, 'PInt_Black')
        f.bb(w / 2, .18, -.02, w / 2 + .03, .36, .03, 'PInt_Black')
    elif kind == 'deskphone':
        cylinder(p, F['x'], F['z'], .06, F['y'], F['y'] + .02, 'PInt_Black', seg=10)
        cylinder(p, F['x'], F['z'], .015, F['y'], F['y'] + .22, 'PInt_Black', seg=6)
        frustum(p, F['x'], F['z'], .02, .04, F['y'] + .22, F['y'] + .27, 'PInt_Black', seg=8, cap=True)
    elif kind == 'hookrail':
        f.bb(-w / 2, 0, -.02, w / 2, h, .02, 'PInt_Panel')
        for i in range(6):
            x = -w / 2 + .12 + i * (w - .24) / 5
            f.bb(x - .015, h / 2 - .015, .02, x + .015, h / 2 + .015, .1, 'PInt_Brass')
    elif kind == 'typewriter':
        f.bb(-w / 2, 0, -d / 2, w / 2, .07, d / 2, 'PInt_Black')
        f.bb(-w / 2 + .03, .07, -d / 2 + .02, w / 2 - .03, .12, 0, 'PInt_Black')
        cylinder(p, *f.pt(-w / 2 - .02, 0, -d / 2 + .05)[0::2], .03, F['y'] + .12, F['y'] + .14, 'PInt_Black', seg=6)
        f.bb(-w / 2 - .02, .12, -d / 2 + .02, w / 2 + .02, .16, -d / 2 + .08, 'PInt_Black')
        f.bb(-w / 2 + .05, .07, .02, w / 2 - .05, .085, d / 2 - .02, 'PInt_Enamel', faces=['+y'])
        f.bb(-.08, .125, -d / 2 + .062, .08, .26, -d / 2 + .068, 'PInt_Paper')
    elif kind in ('ledger',):
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'PInt_Leather')
        f.bb(-w / 2 + .01, .005, -d / 2 + .01, w / 2 - .005, h - .005, d / 2 - .01, 'PInt_Paper', faces=['+x', '+z', '-z'])
    elif kind == 'folder':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'PInt_Folder')
        f.bb(-w / 2 + .02, h, -d / 2 + .02, w / 2 - .02, h + .004, d / 2 - .02, 'PInt_Paper', faces=['+y'])
    elif kind == 'paper':
        f.bb(-w / 2, 0, -d / 2, w / 2, .004, d / 2, 'PInt_Paper', faces=['+y', '+x', '-x', '+z', '-z'])
    elif kind == 'photo':
        f.bb(-w / 2, 0, -d / 2, w / 2, .003, d / 2, 'PInt_Paper', faces=['+y'])
        f.bb(-w / 2 + .015, .003, -d / 2 + .015, w / 2 - .015, .007, d / 2 - .015, 'PInt_Photo', faces=['+y'])
    elif kind == 'docstand':
        # Slanted reading stand with a sheet.
        base = [(-w / 2, 0, -d / 2), (w / 2, 0, -d / 2), (w / 2, 0, d / 2), (-w / 2, 0, d / 2)]
        f.bb(-w / 2, 0, -d / 2, w / 2, .02, d / 2, wood)
        f.bb(-w / 2, .02, -d / 2, w / 2, h, -d / 2 + .03, wood)
        slope_top = [f.pt(-w / 2, .04, d / 2), f.pt(w / 2, .04, d / 2), f.pt(w / 2, h, -d / 2 + .03), f.pt(-w / 2, h, -d / 2 + .03)]
        nrm = f.dir(0, math.cos(math.atan2(h, d)), math.sin(math.atan2(h, d)))
        p.quad(slope_top, nrm, wood)
        sheet = [f.pt(-w / 2 + .04, .07, d / 2 - .03), f.pt(w / 2 - .04, .07, d / 2 - .03), f.pt(w / 2 - .04, h - .02, -d / 2 + .06), f.pt(-w / 2 + .04, h - .02, -d / 2 + .06)]
        sheet = [(v[0] + nrm[0] * .004, v[1] + nrm[1] * .004, v[2] + nrm[2] * .004) for v in sheet]
        p.quad(sheet, nrm, 'PInt_Paper')
        f.bb(-w / 2, .02, d / 2 - .03, w / 2, .07, d / 2, wood)
    elif kind == 'washstand' or kind == 'basinstand':
        if kind == 'washstand':
            f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, wood, faces=['+x', '-x', '+z', '-z'])
            f.bb(-w / 2 - .02, h, -d / 2 - .02, w / 2 + .02, h + .03, d / 2 + .02, 'PInt_Stone')
            cx, _, cz = f.pt(0, 0, 0)
            frustum(p, cx, cz, .17, .2, F['y'] + h + .03, F['y'] + h + .11, 'PInt_Enamel', seg=14)
            cx2, _, cz2 = f.pt(w / 2 - .1, 0, -d / 2 + .1)
            frustum(p, cx2, cz2, .06, .045, F['y'] + h + .03, F['y'] + h + .25, 'PInt_Enamel', seg=10, cap=True)
            f.bb(-w / 2, h + .03, -d / 2, w / 2, h + .55, -d / 2 + .02, 'PInt_Enamel', faces=['+z'])
        else:
            for a in range(3):
                ang = a * 2.094
                bbox(p, F['x'] + math.cos(ang) * .15 - .012, F['y'], F['z'] + math.sin(ang) * .15 - .012, F['x'] + math.cos(ang) * .15 + .012, F['y'] + h, F['z'] + math.sin(ang) * .15 + .012, 'PInt_Iron')
            frustum(p, F['x'], F['z'], .15, .2, F['y'] + h - .04, F['y'] + h + .06, 'PInt_Enamel', seg=14)
    elif kind == 'bucket':
        frustum(p, F['x'], F['z'], .12, .15, F['y'], F['y'] + h, 'PInt_Enamel', seg=12)
    elif kind in ('ironbed', 'cot'):
        mat = 'PInt_Iron' if kind == 'ironbed' else wood
        top = .42 if kind == 'ironbed' else .38
        for sx in (-1, 1):
            f.bb(sx * w / 2 - .02, 0, -d / 2, sx * w / 2 + .02, .75 if kind == 'ironbed' else .55, d / 2, mat)
        for sz in (-1, 1):
            f.bb(-w / 2, top - .04, sz * d / 2 - .02, w / 2, top, sz * d / 2 + .02, mat)
        f.bb(-w / 2 + .02, top, -d / 2 + .02, w / 2 - .02, top + .1, d / 2 - .02, 'PInt_Fabric')
        f.bb(-w / 2 + .5, top + .1, -d / 2 + .04, w / 2 - .04, top + .14, d / 2 - .04, 'PInt_Oilcloth' if kind == 'ironbed' else 'PInt_Green')
        f.bb(-w / 2 + .05, top + .1, -d / 2 + .1, -w / 2 + .4, top + .2, d / 2 - .1, 'PInt_Enamel')
    elif kind == 'wardrobe':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, wood)
        f.bb(-w / 2 - .03, h, -d / 2 - .03, w / 2 + .03, h + .06, d / 2 + .03, wood)
        f.bb(-.005, .1, d / 2, .005, h - .1, d / 2 + .01, 'PInt_Black')
        for sx in (-1, 1):
            f.bb(sx * .05 - .012, h / 2, d / 2, sx * .05 + .012, h / 2 + .1, d / 2 + .025, 'PInt_Brass')
    elif kind == 'cage':
        f.bb(-w / 2, 0, -d / 2, w / 2, .06, d / 2, 'PInt_Iron'); f.bb(-w / 2, h - .06, -d / 2, w / 2, h, d / 2, 'PInt_Iron')
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * w / 2 - .025 * (sx + 1), 0, sz * d / 2 - .025 * (sz + 1), sx * w / 2 - .025 * (sx - 1), h, sz * d / 2 - .025 * (sz - 1), 'PInt_Iron')
        for i in range(1, 16):
            x = -w / 2 + w * i / 16
            f.bb(x - .006, .06, d / 2 - .012, x + .006, h - .06, d / 2, 'PInt_Iron')
        for k in range(1, 6):
            y = h * k / 6
            f.bb(-w / 2, y - .006, d / 2 - .012, w / 2, y + .006, d / 2, 'PInt_Iron')
        for i in range(3):
            for k in range(2):
                f.bb(-w / 2 + .1 + i * .48, .06 + k * .7, -d / 2 + .06, -w / 2 + .5 + i * .48, .36 + k * .7, d / 2 - .1, 'PInt_Box')
        f.bb(-.06, h / 2 - .1, d / 2, .06, h / 2 + .06, d / 2 + .05, 'PInt_Brass')
    elif kind == 'boottray':
        f.bb(-w / 2, 0, -d / 2, w / 2, .03, d / 2, 'PInt_Iron')
        for sx in (-.2, .12):
            f.bb(sx - .05, .03, -.12, sx + .05, .3, .0, 'PInt_Leather'); f.bb(sx - .05, .03, .0, sx + .05, .1, .13, 'PInt_Leather')
    elif kind == 'planter':
        t = .22
        for (x0, z0, x1, z1) in ((-w / 2, -d / 2, w / 2, -d / 2 + t), (-w / 2, d / 2 - t, w / 2, d / 2), (-w / 2, -d / 2 + t, -w / 2 + t, d / 2 - t), (w / 2 - t, -d / 2 + t, w / 2, d / 2 - t)):
            f.bb(x0, 0, z0, x1, h, z1, 'PExt_Ashlar')
        f.bb(-w / 2 + t, 0, -d / 2 + t, w / 2 - t, h - .08, d / 2 - t, 'PInt_Leather', faces=['+y'])
    elif kind == 'drain':
        f.bb(-w / 2, -.002, -d / 2, w / 2, .004, d / 2, 'PExt_Iron', faces=['+y'])
    else:
        raise ValueError('Unknown furniture kind ' + kind)


# ---------------------------------------------------------------------------
# Mesh/object creation
# ---------------------------------------------------------------------------
def newell(vs):
    n = Vector((0, 0, 0))
    for i in range(len(vs)):
        a, b = vs[i], vs[(i + 1) % len(vs)]
        n.x += (a.y - b.y) * (a.z + b.z); n.y += (a.z - b.z) * (a.x + b.x); n.z += (a.x - b.x) * (a.y + b.y)
    return n


def make_materials():
    mats = {}
    for name, (hexc, metal, rough, tile) in MATERIALS.items():
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        rgb = tuple(int(hexc[i:i + 2], 16) / 255 for i in (0, 2, 4))
        lin = tuple(c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4 for c in rgb)
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*lin, 1)
        bsdf.inputs['Metallic'].default_value = metal
        bsdf.inputs['Roughness'].default_value = rough
        m.diffuse_color = (*lin, .35 if name == 'P_Glass' else 1)
        mats[name] = m
    return mats


def build_objects(collection, mats):
    root = bpy.data.objects.new('PoliceStationVisual', None)
    collection.objects.link(root)
    for name, pos in (('EastMarker', (3, 0, 0)), ('NorthMarker', (0, 0, 3))):
        e = bpy.data.objects.new(name, None); e.location = u(pos); e.parent = root; collection.objects.link(e)
    stats = {}
    for name in ORDER:
        p = PARTS[name]
        if not p.faces:
            continue
        pivot = Vector(p.pivot) if p.pivot is not None else Vector((0, 0, 0))
        mesh = bpy.data.meshes.new(name + '_Mesh')
        bm = bmesh.new()
        uvl = bm.loops.layers.uv.new('UVMap')
        used = []
        for verts, normal, mat, uvs in p.faces:
            bverts = [u(v) - u(pivot) for v in verts]
            want = u(normal) - u((0, 0, 0))
            got = newell(bverts)
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
            tile = MATERIALS[mat][3] or 1.0
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
        collection.objects.link(obj)
        obj.parent = root
        obj.location = u(pivot)
        stats[name] = len(mesh.polygons)
    bpy.context.view_layer.update()
    return root, stats


def render_previews(out_dir, collection, L):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True; scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    scene.world = bpy.data.worlds.new('PolicePreviewWorld')
    cam_data = bpy.data.cameras.new('PreviewCam'); cam = bpy.data.objects.new('PreviewCam', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    hidden_for_plan = [o for o in collection.objects if o.name.startswith(('Ext_Roof', 'Int_Ceilings', 'Ext_Loggia'))]
    shots = {
        'street-front': ((4.0, 1.7, -14.0), (12.0, 4.0, 4.0), 55, []),
        'street-east': ((32.0, 2.0, -6.0), (18.0, 3.5, 10.0), 55, []),
        'court': ((11.5, 1.7, 30.0), (11.5, 2.2, 14.0), 62, []),
        'plan-cutaway': ((11.5, 42.0, 9.0), (11.5, 0.0, 12.0), 50, hidden_for_plan),
        'reading-room': ((9.3, 1.66, 5.6), (1.5, 1.0, 1.5), 70, []),
        'hall': ((11.5, 1.66, .8), (11.8, 1.4, 9.5), 70, []),
    }
    for name, (eye, target, fov, hide) in shots.items():
        for o in hide:
            o.hide_render = True
        cam.location = u(eye)
        cam.rotation_euler = (u(target) - u(eye)).to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(fov); cam_data.clip_end = 300
        scene.render.filepath = str(out_dir / ('preview-' + name + '.png'))
        bpy.ops.render.render(write_still=True)
        for o in hide:
            o.hide_render = False
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument('--layout', default='assets/source/police/police-layout.json')
    ap.add_argument('--output', default='assets/source/police')
    ap.add_argument('--unity', default='game/Assets/Art/Police')
    ap.add_argument('--previews', default='')
    args = ap.parse_args(argv)
    L = json.loads(Path(args.layout).read_text(encoding='utf-8'))
    out = Path(args.output).resolve(); out.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'

    build_walls(L); build_wall_trims(L); build_slabs(L); build_thresholds(L); build_stairs(L)
    build_roofs(L); build_loggias(L); build_exterior_details(L); build_joinery(L); build_lamps(L)
    for F in L['furniture']:
        build_furniture_piece(F, L)

    collection = bpy.data.collections.new('PoliceStation')
    bpy.context.scene.collection.children.link(collection)
    mats = make_materials()
    root, stats = build_objects(collection, mats)

    manifest = dict(version=1, layoutVersion=L['version'], objects=stats, triangles=sum(stats.values()),
                    leaves=LEAVES, drawers=DRAWERS, materials=[dict(name=k, hex=v[0], metallic=v[1], roughness=v[2], tile=v[3]) for k, v in MATERIALS.items()],
                    blender=bpy.app.version_string)
    (out / 'police-build-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out / 'PoliceStation.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    if args.unity:
        dest = Path(args.unity).resolve(); dest.mkdir(parents=True, exist_ok=True)
        (dest / 'PoliceStation.fbx').write_bytes((out / 'PoliceStation.fbx').read_bytes())
        (dest / 'PoliceBuildManifest.json').write_bytes((out / 'police-build-manifest.json').read_bytes())
    if args.previews:
        prev = Path(args.previews).resolve(); prev.mkdir(parents=True, exist_ok=True)
        render_previews(prev, collection, L)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'PoliceStation.blend'), compress=True)
    print('POLICE_BLENDER_PASS', json.dumps(dict(objects=len(stats), triangles=manifest['triangles'], leaves=len(LEAVES), drawers=len(DRAWERS))))


if __name__ == '__main__':
    main()
