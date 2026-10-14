"""Blender generator for the Qingchuan cottage hospital (B11).

Reads the shared layout contract (tools/hospital/hospital_layout.py output) and builds one
editable scene: shell with real door/window apertures, floors, ceilings, the pavilion roofs,
joinery, movable door leaves on their hinges, lamp fittings and the hospital furniture.
Exports the FBX used by Unity's BuildSlice.Hospital and saves the packed .blend.

    blender --background --factory-startup --python-exit-code 1 \
        --python tools/hospital/build_hospital.py -- \
        --layout assets/source/hospital/hospital-layout.json \
        --output assets/source/hospital --previews builds/hospital/blender

The low-level kit (parts, boxes, the shared wall column cut, sash windows, casings, door
leaves, the police furniture pieces reused here) comes from tools/police/build_police_station.py,
imported as a library; its material names are mapped onto the hospital palette at the end.
Coordinates are Unity building-local metres (+X east, +Y up, +Z north). Object names are the
contract with Unity: Ext_*/Int_* shells, Glass, Leaf_<opening>_<n> (origin on the hinge),
Furn_<id>, Lamp_<id>, Bulb_<id>, EastMarker/NorthMarker.
"""
import argparse, json, math, sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'police'))
import bpy
import build_police_station as P
from build_police_station import part, box, bbox, tri, prism, cylinder, frustum, Frame

# name: (hex, metallic, roughness, metres per texture repeat or 0 for flat)
MATERIALS = {
    'HExt_Render': ('cfc9ba', 0, .92, 3.0), 'HExt_Stone': ('8f908b', 0, .85, 1.2), 'HExt_Slate': ('474d55', 0, .6, 1.5),
    'HExt_Paint': ('e2dccd', 0, .5, 0), 'HExt_Door': ('2f4a3f', 0, .4, 0), 'HExt_Iron': ('2c3133', .6, .45, 0),
    'HExt_Gutter': ('33393b', .5, .5, 0), 'HExt_Timber': ('4f4034', 0, .7, 1.0), 'HExt_Louvre': ('3a3d3b', 0, .7, 0),
    'H_Glass': ('9fb3be', 0, .05, 0), 'H_LampGlass': ('ffd9a0', 0, .2, 0), 'H_Bulb': ('fff3d9', 0, .2, 0),
    'HInt_Wall': ('e1dac4', 0, .6, 3.0), 'HInt_Ceiling': ('ebe6d8', 0, .95, 3.0),
    'HInt_DadoGreen': ('5f7765', 0, .32, 0), 'HInt_DadoBrown': ('735a44', 0, .38, 0), 'HInt_DadoBlue': ('86a0ad', 0, .32, 0),
    'HInt_DadoTile': ('e3e6e0', 0, .14, 0), 'HInt_DadoBand': ('2e3d35', 0, .4, 0),
    'HInt_Wood': ('8f7350', 0, .38, 2.0), 'HInt_Lino': ('7c5d47', 0, .33, 0), 'HInt_Tiles': ('ad9d86', 0, .42, 1.0),
    'HInt_Joinery': ('6b5038', 0, .45, 1.0), 'HInt_Furniture': ('6a4f37', 0, .5, 1.0), 'HInt_FurnLight': ('9a7c58', 0, .45, 1.0),
    'HInt_Enamel': ('edece5', 0, .2, 0), 'HInt_Iron': ('2b2f31', .5, .5, 0), 'HInt_Brass': ('b38a49', .8, .35, 0),
    'HInt_Steel': ('aeb4b6', .8, .3, 0), 'HInt_Black': ('1e1e1e', .2, .4, 0), 'HInt_Rubber': ('35383a', 0, .8, 0),
    'HInt_Linen': ('ebe8de', 0, .9, 0), 'HInt_Ticking': ('c9c6b8', 0, .9, 0), 'HInt_QuiltRose': ('b88c8a', 0, .92, 0),
    'HInt_QuiltBlue': ('8496ab', 0, .92, 0), 'HInt_QuiltSage': ('95a68d', 0, .92, 0), 'HInt_QuiltCream': ('d3c8aa', 0, .92, 0),
    'HInt_BlanketRed': ('8a3a31', 0, .95, 0), 'HInt_Cloth': ('cbd5c6', 0, .9, 0),
    'HInt_Paper': ('e8dfc8', 0, .9, 0), 'HInt_Cork': ('a37c55', 0, .9, 0), 'HInt_Leather': ('4a3328', 0, .6, 0),
    'HInt_LampGreen': ('3f5a47', 0, .6, 0), 'HInt_SignGreen': ('2c4a3d', 0, .25, 0), 'HInt_Gold': ('c0a25a', .6, .4, 0),
    'HInt_BottleBrown': ('5a3a1e', 0, .18, 0), 'HInt_BottleBlue': ('27406b', 0, .18, 0), 'HInt_BottleGreen': ('2f5a40', 0, .18, 0),
    'HInt_BottleClear': ('cfd8d6', 0, .12, 0), 'HInt_FireRed': ('9a2d25', 0, .4, 0),
    'HInt_Leaf': ('47613f', 0, .7, 0), 'HInt_Pot': ('8a5a3c', 0, .8, 0), 'HInt_Wicker': ('b08c58', 0, .8, 0),
    'HInt_Apple': ('a8342c', 0, .35, 0), 'HInt_AppleGreen': ('93a54c', 0, .4, 0),
    'HInt_ToyRed': ('b0483a', 0, .5, 0), 'HInt_ToyBlue': ('3f6a9a', 0, .5, 0), 'HInt_ToyYellow': ('d2aa3c', 0, .5, 0),
    'HInt_HorseGrey': ('c9c4b8', 0, .6, 0), 'HInt_Marble': ('d8d6cf', 0, .3, 0),
    'HInt_BookRed': ('6a3a2c', 0, .8, 0), 'HInt_BookGreen': ('3e4a3a', 0, .8, 0), 'HInt_BookBrown': ('6a5a3a', 0, .8, 0),
    'HInt_BookBlue': ('2e3a4a', 0, .8, 0), 'HInt_Box': ('8a7656', 0, .9, 0), 'HInt_Oilcloth': ('3b4639', 0, .4, 0),
}
# The shared kit writes police material names; these are their hospital equivalents.
REMAP = {
    'PExt_Stucco': 'HExt_Render', 'PExt_Ashlar': 'HExt_Stone', 'PExt_Slate': 'HExt_Slate', 'PExt_Paint': 'HExt_Paint',
    'PExt_DoorOak': 'HExt_Door', 'PExt_Iron': 'HExt_Iron', 'PExt_Gutter': 'HExt_Gutter', 'PExt_Timber': 'HExt_Timber',
    'P_Glass': 'H_Glass', 'P_LampGlass': 'H_LampGlass', 'P_Bulb': 'H_Bulb',
    'PInt_Plaster': 'HInt_Wall', 'PInt_Ceiling': 'HInt_Ceiling', 'PInt_Panel': 'HInt_Joinery', 'PInt_Furniture': 'HInt_Furniture',
    'PInt_FurnLight': 'HInt_FurnLight', 'PInt_Green': 'HInt_LampGreen', 'PInt_Brass': 'HInt_Brass', 'PInt_Iron': 'HInt_Iron',
    'PInt_Paper': 'HInt_Paper', 'PInt_Fabric': 'HInt_Linen', 'PInt_Enamel': 'HInt_Enamel', 'PInt_Cork': 'HInt_Cork',
    'PInt_Black': 'HInt_Black', 'PInt_Leather': 'HInt_Leather', 'PInt_Stone': 'HInt_Marble', 'PInt_Oilcloth': 'HInt_Oilcloth',
    'PInt_BookRed': 'HInt_BookRed', 'PInt_BookGreen': 'HInt_BookGreen', 'PInt_BookBrown': 'HInt_BookBrown',
    'PInt_BookBlue': 'HInt_BookBlue', 'PInt_Box': 'HInt_Box', 'PInt_Folder': 'HInt_Box', 'PInt_Photo': 'HInt_Leather',
}
P.MATERIALS.update(MATERIALS)
PLINTH = .62
SOFFIT = .16


# ---------------------------------------------------------------------------
# Frame helpers for round and tilted pieces.
# ---------------------------------------------------------------------------
def cyl(f, lx, lz, r, y0, y1, mat, seg=10, top=True):
    x, _, z = f.pt(lx, 0, lz)
    cylinder(f.p, x, z, r, f.o[1] + y0, f.o[1] + y1, mat, seg=seg, top=top)


def cone(f, lx, lz, r0, r1, y0, y1, mat, seg=12, cap=False):
    x, _, z = f.pt(lx, 0, lz)
    frustum(f.p, x, z, r0, r1, f.o[1] + y0, f.o[1] + y1, mat, seg=seg, cap=cap)


def ball(f, lx, ly, lz, r, mat, seg=8):
    cone(f, lx, lz, r * .55, r, ly - r, ly - r * .25, mat, seg=seg)
    cone(f, lx, lz, r, r * .5, ly - r * .25, ly + r * .7, mat, seg=seg, cap=True)


def apple(f, lx, ly, lz, r, mat, seg=16):
    """A rounded apple centred at ly (it is seen close in the bedside cinematic), with a stem."""
    prof = [(.2, -.9), (.6, -.78), (.86, -.52), (.99, -.16), (.98, .2), (.86, .52), (.62, .74), (.3, .8)]
    for i, ((r0, y0), (r1, y1)) in enumerate(zip(prof, prof[1:])):
        cone(f, lx, lz, r * r0, r * r1, ly + r * y0, ly + r * y1, mat, seg=seg, cap=i == len(prof) - 2)
    cyl(f, lx, lz, r * .07, ly + r * .72, ly + r * 1.08, 'HInt_Wood', seg=6)


def quad(f, pts, normal, mat):
    f.p.quad([f.pt(*v) for v in pts], f.dir(*normal), mat)


def slab2(f, pts, normal, mat):
    """Thin sheet visible from both sides."""
    quad(f, pts, normal, mat)
    quad(f, pts[::-1], tuple(-c for c in normal), mat)


def skin(f, pts, out, mat):
    """A slanted quad facing roughly `out`, wound like the box faces."""
    a, b, c = pts[0], pts[1], pts[2]
    e1 = [b[i] - a[i] for i in range(3)]; e2 = [c[i] - b[i] for i in range(3)]
    n = (e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0])
    if sum(n[i] * out[i] for i in range(3)) > 0:
        pts = pts[::-1]
    else:
        n = tuple(-v for v in n)
    length = math.sqrt(sum(v * v for v in n))
    quad(f, pts, tuple(v / length for v in n), mat)


def covers(f, base, stations, mats):
    """Bedclothes raised over a body. stations: (z, half width, rise) from the head end to the foot;
    mats: one material per span. Open underneath, closed at both ends."""
    def ring(z, half, rise):
        return [(-half, base, z), (-half * .78, base + rise * .7, z), (-half * .3, base + rise, z),
                (half * .3, base + rise, z), (half * .78, base + rise * .7, z), (half, base, z)]
    rings = [ring(*s) for s in stations]
    for a, b, mat in zip(rings, rings[1:], mats):
        for i in range(5):
            skin(f, [a[i], a[i + 1], b[i + 1], b[i]], ((a[i][0] + a[i + 1][0]) / 2, .1, 0), mat)
    for r, out, mat in ((rings[0], (0, 0, -1), mats[0]), (rings[-1], (0, 0, 1), mats[-1])):
        skin(f, [r[0], r[1], r[4], r[5]], out, mat)
        skin(f, [r[1], r[2], r[3], r[4]], out, mat)


def wheel(f, lx, ly, lz, r, width, mat, seg=12, hub=None):
    """Disc standing on its rim, axle along the piece's local X."""
    ring = [(r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg)) for i in range(seg)]
    for side in (-1, 1):
        x = lx + side * width / 2
        for i in range(1, seg - 1):
            verts = [(x, ly + ring[0][1], lz + ring[0][0]), (x, ly + ring[i][1], lz + ring[i][0]), (x, ly + ring[i + 1][1], lz + ring[i + 1][0])]
            tri(f.p, [f.pt(*v) for v in verts], f.dir(side, 0, 0), hub or mat)
    for i in range(seg):
        a, b = ring[i], ring[(i + 1) % seg]
        mid = math.atan2(a[1] + b[1], a[0] + b[0])
        quad(f, [(lx - width / 2, ly + a[1], lz + a[0]), (lx + width / 2, ly + a[1], lz + a[0]),
                 (lx + width / 2, ly + b[1], lz + b[0]), (lx - width / 2, ly + b[1], lz + b[0])], (0, math.sin(mid), math.cos(mid)), mat)


# ---------------------------------------------------------------------------
# Shell: dado, floors and ceilings, roofs, exterior dressings.
# ---------------------------------------------------------------------------
def room_sides(r):
    """(axis, face coordinate, sign of the inward normal, a0, a1) for the four walls of a room."""
    return (('x', r['z0'], 1, r['x0'], r['x1']), ('x', r['z1'], -1, r['x0'], r['x1']),
            ('z', r['x0'], 1, r['z0'], r['z1']), ('z', r['x1'], -1, r['z0'], r['z1']))


def build_dado(L):
    """Gloss-painted lower walls with a dark capping band and skirting: the period hospital wall."""
    g = L['levels']['ground']
    trim = part('Int_Trim')
    for r in L['rooms']:
        tall = r['dado'] == 'tile'
        colour = {'green': 'HInt_DadoGreen', 'brown': 'HInt_DadoBrown', 'blue': 'HInt_DadoBlue', 'tile': 'HInt_DadoTile'}[r['dado']]
        top = g + (1.8 if tall else 1.35)
        for axis, face, sign, a0, a1 in room_sides(r):
            cuts = []
            for w in L['walls']:
                if w['axis'] != axis or abs(w['c'] + sign * w['t'] / 2 - face) > .03:
                    continue
                for o in L['openings']:
                    if o['wall'] != w['id'] or o['bottom'] > top:
                        continue
                    lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
                    if hi > a0 and lo < a1:
                        # Casings stand 8 cm around doors, arches and hatches; a window's board caps its own span.
                        grow = 0 if o['kind'] == 'window' else .085
                        cuts.append((lo - grow, hi + grow, o))
            cuts.sort(key=lambda c: c[0])
            # Side strips stop short of the corners so the tops of two walls' strips never share a patch.
            inset = .022 if axis == 'z' else 0
            spans, cursor = [], a0 + inset
            for lo, hi, o in cuts:
                spans.append((cursor, lo, top, True))
                below = o['bottom'] - (.03 if o['kind'] == 'window' else .09)
                if o['kind'] in ('window', 'hatch') and below > g + .2:
                    spans.append((lo, hi, below, False))
                cursor = hi
            spans.append((cursor, a1 - inset, top, True))
            for s0, s1, y1, capped in spans:
                if s1 - s0 < .04:
                    continue
                for y0, yt, depth, mat in ((g, g + .12, .02, 'HInt_DadoBand'), (g + .12, y1 - (.05 if capped else 0), .008, colour)) + \
                        (((y1 - .05, y1, .02, 'HInt_DadoBand'),) if capped else ()):
                    d0, d1 = sorted((face, face + sign * depth))
                    if axis == 'x':
                        bbox(trim, s0, y0, d0, s1, yt, d1, mat, faces=['+z' if sign > 0 else '-z', '+y', '+x', '-x'])
                    else:
                        bbox(trim, d0, y0, s0, d1, yt, s1, mat, faces=['+x' if sign > 0 else '-x', '+y', '+z', '-z'])


def build_slabs(L):
    for s in L['slabs']:
        k = s['kind']
        if k == 'finish':
            mat = {'wood': 'HInt_Wood', 'tiles': 'HInt_Tiles', 'lino': 'HInt_Lino'}[s['top']]
            bbox(part('Int_Floors'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], mat, faces=['+y'])
        elif k == 'ceiling':
            bbox(part('Int_Ceilings'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'HInt_Ceiling', faces=['-y'])
        elif k == 'step':
            bbox(part('Ext_Facade'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'HExt_Stone', faces=['+y', '+x', '-x', '+z', '-z'])


def slope(p, verts, normal, along, up_dir):
    P.slope_quad(p, verts, normal, 'HExt_Slate', along, up_dir, 1.5)


def build_roofs(L):
    """Two pavilions gabled north and south, the range between them and the entrance gable.
    Every ridge is at one height and every roof rises the same amount over its half span, so
    the valleys run from an eaves corner straight to a ridge junction: each slope is one exact
    polygon and no roof passes hidden through another."""
    roof = part('Ext_Roof'); fac = part('Ext_Facade')
    R = {r['id']: r for r in L['roofs']}
    e, h = L['levels']['frontEaves'], L['levels']['frontRidge']
    W, E, C, N = R['roof-west'], R['roof-east'], R['roof-range'], R['roof-entrance']
    zs, zn = W['z0'], W['z1']                 # street and north verges
    cs, cn = C['z0'], C['z1']                 # range eaves
    zr = (cs + cn) / 2                        # range ridge
    xw, xe = (W['x0'] + W['x1']) / 2, (E['x0'] + E['x1']) / 2
    xg = (N['x0'] + N['x1']) / 2
    sp = (h - e) / (xw - W['x0']); lp = math.hypot(1, sp)
    sr = (h - e) / (zr - cs); lr = math.hypot(1, sr)
    up_w, up_e = (sp / lp, 1 / lp, 0), (-sp / lp, 1 / lp, 0)
    nw, ne = (-sp / lp, 1 / lp, 0), (sp / lp, 1 / lp, 0)
    for r, inner_west in ((W, False), (E, True)):
        x0, x1, xm = r['x0'], r['x1'], (r['x0'] + r['x1']) / 2
        # Outer slope: one rectangle. Court-side slope: the range cuts a triangle out of it.
        outer = [(x0, e, zn), (x0, e, zs), (xm, h, zs), (xm, h, zn)] if not inner_west else [(x1, e, zs), (x1, e, zn), (xm, h, zn), (xm, h, zs)]
        slope(roof, outer, nw if not inner_west else ne, (0, 0, -1) if not inner_west else (0, 0, 1), up_w if not inner_west else up_e)
        xi = x1 if not inner_west else x0
        n, up = (ne, up_e) if not inner_west else (nw, up_w)
        slope(roof, [(xm, h, zs), (xi, e, zs), (xm, h, zr)], n, (0, 0, 1), up)
        slope(roof, [(xm, h, zr), (xi, e, cn), (xi, e, zn), (xm, h, zn)], n, (0, 0, 1), up)
    # Range: the south slope is cut by both pavilions and the entrance gable, the north slope by the pavilions.
    ns, nn = (0, 1 / lr, -sr / lr), (0, 1 / lr, sr / lr)
    up_s, up_n = (0, sr / lr, 1 / lr), (0, sr / lr, -1 / lr)
    slope(roof, [(W['x1'], e, cs), (N['x0'], e, cs), (xg, h, zr), (xw, h, zr)], ns, (1, 0, 0), up_s)
    slope(roof, [(N['x1'], e, cs), (E['x0'], e, cs), (xe, h, zr), (xg, h, zr)], ns, (1, 0, 0), up_s)
    slope(roof, [(E['x0'], e, cn), (W['x1'], e, cn), (xw, h, zr), (xe, h, zr)], nn, (-1, 0, 0), up_n)
    slope(roof, [(N['x0'], e, cs), (xg, h, cs), (xg, h, zr)], nw, (0, 0, 1), up_w)
    slope(roof, [(xg, h, cs), (N['x1'], e, cs), (xg, h, zr)], ne, (0, 0, 1), up_e)

    # Boxed eaves: fascia board and flat soffit back to the wall face.
    def eave(a, b, inward):
        (ax, az), (bx, bz) = a, b
        dx, dz = inward
        n = (-dx, 0, -dz)
        roof.quad([(ax, e - SOFFIT, az), (bx, e - SOFFIT, bz), (bx, e, bz), (ax, e, az)], n, 'HExt_Paint')
        roof.quad([(ax, e - SOFFIT, az), (bx, e - SOFFIT, bz), (bx + dx * .35, e - SOFFIT, bz + dz * .35), (ax + dx * .35, e - SOFFIT, az + dz * .35)], (0, -1, 0), 'HExt_Paint')
        # Gutter on the fascia.
        g0 = (min(ax, bx) - (0 if dx else 0) - (abs(dx) * 0), e - .1, min(az, bz))
        if dz:
            bbox(roof, min(ax, bx), e - .11, az - dz * .1, max(ax, bx), e - .02, az - dz * .001, 'HExt_Gutter', faces=['+x', '-x', '-y', '-z' if dz > 0 else '+z'])
        else:
            bbox(roof, ax - dx * .1, e - .11, min(az, bz), ax - dx * .001, e - .02, max(az, bz), 'HExt_Gutter', faces=['+z', '-z', '-y', '-x' if dx > 0 else '+x'])

    eave((W['x0'], zs), (W['x0'], zn), (1, 0)); eave((E['x1'], zs), (E['x1'], zn), (-1, 0))
    eave((W['x1'], cn), (W['x1'], zn), (-1, 0)); eave((E['x0'], cn), (E['x0'], zn), (1, 0))
    eave((W['x1'], cs), (N['x0'], cs), (0, 1)); eave((N['x1'], cs), (E['x0'], cs), (0, 1))
    eave((W['x1'], cn), (E['x0'], cn), (0, -1))

    # Gables: bargeboards on the verge, a sloped soffit behind them and the wall infill up to the roof.
    def gable(x0, x1, zv, zw, wall0, wall1):
        xm = (x0 + x1) / 2; out = -1 if zv < zw else 1
        for xa, xb in ((x0, xm), (x1, xm)):
            roof.quad([(xa, e - SOFFIT, zv), (xb, h - SOFFIT, zv), (xb, h, zv), (xa, e, zv)], (0, 0, out), 'HExt_Paint')
            s = 1 if xa < xb else -1
            roof.quad([(xa, e - SOFFIT, zv), (xb, h - SOFFIT, zv), (xb, h - SOFFIT, zw), (xa, e - SOFFIT, zw)], (s * sp / lp, -1 / lp, 0), 'HExt_Paint')
        y_side = e + (wall0 - x0) * sp - SOFFIT
        fac.quad([(wall0, e, zw), (wall1, e, zw), (wall1, y_side, zw), (xm, h - SOFFIT, zw), (wall0, y_side, zw)], (0, 0, out), 'HExt_Render')
        # Louvred roof vent in the apex, in a stone frame.
        y = e + (h - e) * .42
        d0, d1 = sorted((zw, zw + out * .05))
        bbox(fac, xm - .32, y - .1, d0, xm + .32, y + .62, d1, 'HExt_Stone', faces=['-z' if out < 0 else '+z', '+x', '-x', '+y', '-y'])
        d0, d1 = sorted((zw + out * .05, zw + out * .06))
        bbox(fac, xm - .22, y, d0, xm + .22, y + .52, d1, 'HExt_Louvre', faces=['-z' if out < 0 else '+z'])
        for k in range(1, 5):
            d0, d1 = sorted((zw + out * .06, zw + out * .09))
            bbox(fac, xm - .22, y + k * .104 - .02, d0, xm + .22, y + k * .104 + .012, d1, 'HExt_Paint', faces=['-z' if out < 0 else '+z', '+y', '-y'])

    for r in (W, E):
        gable(r['x0'], r['x1'], zs, zs + .35, r['x0'] + .35, r['x1'] - .35)
        gable(r['x0'], r['x1'], zn, zn - .35, r['x0'] + .35, r['x1'] - .35)
    gable(N['x0'], N['x1'], cs, cs + .35, N['x0'] + .35, N['x1'] - .35)

    # Door hood: a small tiled gable on two posts.
    H = R['roof-hood']; hx0, hx1, hz0, hz1, he, hh = H['x0'], H['x1'], H['z0'], H['z1'], H['eaves'], H['ridge']
    hm = (hx0 + hx1) / 2; hs = (hh - he) / (hm - hx0); hl = math.hypot(1, hs)
    slope(roof, [(hx0, he, hz1), (hx0, he, hz0), (hm, hh, hz0), (hm, hh, hz1)], (-hs / hl, 1 / hl, 0), (0, 0, -1), (hs / hl, 1 / hl, 0))
    slope(roof, [(hx1, he, hz0), (hx1, he, hz1), (hm, hh, hz1), (hm, hh, hz0)], (hs / hl, 1 / hl, 0), (0, 0, 1), (-hs / hl, 1 / hl, 0))
    lg = part('Ext_Loggia')
    for xa, s in ((hx0, 1), (hx1, -1)):
        lg.quad([(xa, he - .09, hz0), (hm, hh - .09, hz0), (hm, hh - .09, hz1), (xa, he - .09, hz1)], (s * hs / hl, -1 / hl, 0), 'HExt_Timber')
        lg.quad([(xa, he - .09, hz0), (hm, hh - .09, hz0), (hm, hh, hz0), (xa, he, hz0)], (0, 0, -1), 'HExt_Paint')
        lg.quad([(xa, he - .09, hz0), (xa, he - .09, hz1), (xa, he, hz1), (xa, he, hz0)], (-s, 0, 0), 'HExt_Paint')
    tri(lg, [(hx0 + .1, he, hz0 + .03), (hx1 - .1, he, hz0 + .03), (hm, hh - .1, hz0 + .03)], (0, 0, -1), 'HExt_Render')
    for c in L['columns']:
        x, z, w = c['x'], c['z'], c['w']
        bbox(lg, x - w / 2 - .04, c['y0'], z - w / 2 - .04, x + w / 2 + .04, c['y0'] + .22, z + w / 2 + .04, 'HExt_Stone', faces=['+x', '-x', '+z', '-z', '+y'])
        bbox(lg, x - w / 2, c['y0'] + .22, z - w / 2, x + w / 2, c['y1'], z + w / 2, 'HExt_Timber', faces=['+x', '-x', '+z', '-z'])
    for b in L['beams']:
        bbox(lg, b['x0'], b['y0'], b['z0'], b['x1'], b['y1'], b['z1'], 'HExt_Timber', faces=['+x', '-x', '-y', '+z', '-z'])
        for c in L['columns']:      # plates from each post back to the wall
            bbox(lg, c['x'] - .06, b['y0'], b['z1'], c['x'] + .06, b['y1'], -.001, 'HExt_Timber', faces=['+x', '-x', '-y'])

    # Chimney stacks over the three stoves.
    for cx, cz in ((16.5, 7.75), (4.0, 16.6), (38.0, 16.6)):
        top = h + .95
        bbox(roof, cx - .36, h - 1.2, cz - .28, cx + .36, top, cz + .28, 'HExt_Render', faces=['+x', '-x', '+z', '-z'])
        bbox(roof, cx - .44, top, cz - .36, cx + .44, top + .12, cz + .36, 'HExt_Stone')
        for dx in (-.16, .16):
            cylinder(roof, cx + dx, cz, .075, top + .12, top + .42, 'HExt_Gutter', seg=8)


def exterior_faces():
    """Outline of the three ranges as (axis, coordinate, outward sign, a0, a1, convex at a0, convex at a1)."""
    return (('x', 0, -1, 0, 42, True, True), ('z', 42, 1, 0, 30, True, True), ('x', 30, 1, 34, 42, True, True),
            ('z', 34, -1, 12, 30, False, True), ('x', 12, 1, 8, 34, False, False), ('z', 8, 1, 12, 30, False, True),
            ('x', 30, 1, 0, 8, True, True), ('z', 0, -1, 0, 30, True, True))


def build_exterior_details(L):
    fac = part('Ext_Facade')
    W = {w['id']: w for w in L['walls']}
    outer = {}
    for o in L['openings']:
        if not o['exterior']:
            continue
        w = W[o['wall']]; sign = 1 if w['pos'] == 'ext' else -1
        outer.setdefault((w['axis'], round(w['c'] + sign * w['t'] / 2, 3)), []).append((o, sign))
    # Stone plinth, broken at the doors and returned round the outer corners.
    for axis, c, sign, a0, a1, cv0, cv1 in exterior_faces():
        doors = sorted((o['at'] - o['width'] / 2, o['at'] + o['width'] / 2) for o, _ in outer.get((axis, c), []) if o['kind'] == 'door' and a0 <= o['at'] <= a1)
        a, spans = a0 - (.05 if cv0 else -.05), []
        for lo, hi in doors:
            spans.append((a, lo)); a = hi
        spans.append((a, a1 + (.05 if cv1 else -.05)))
        for s0, s1 in spans:
            if s1 - s0 < .02:
                continue
            d0, d1 = sorted((c, c + sign * .05))
            if axis == 'x':
                bbox(fac, s0, -.1, d0, s1, PLINTH, d1, 'HExt_Stone', faces=['+z' if sign > 0 else '-z', '+y', '+x', '-x'])
            else:
                bbox(fac, d0, -.1, s0, d1, PLINTH, s1, 'HExt_Stone', faces=['+x' if sign > 0 else '-x', '+y', '+z', '-z'])
    # Stone sills and flat surrounds on every outside window.
    for (axis, c), group in outer.items():
        for o, sign in group:
            if o['kind'] != 'window':
                continue
            lo, hi, b, t = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom'], o['top']
            for (s0, s1, y0, y1, depth, faces_x, faces_z) in (
                    (lo - .12, hi + .12, b - .1, b - .004, .09, ['+y', '-y', '+x', '-x'], ['+y', '-y', '+z', '-z']),
                    (lo - .12, lo, b, t + .12, .03, ['+x', '-x'], ['+z', '-z']), (hi, hi + .12, b, t + .12, .03, ['+x', '-x'], ['+z', '-z']),
                    (lo, hi, t, t + .12, .03, ['+y', '-y'], ['+y', '-y'])):
                d0, d1 = sorted((c, c + sign * depth))
                if axis == 'x':
                    bbox(fac, s0, y0, d0, s1, y1, d1, 'HExt_Stone', faces=['+z' if sign > 0 else '-z'] + faces_x)
                else:
                    bbox(fac, d0, y0, s0, d1, y1, s1, 'HExt_Stone', faces=['+x' if sign > 0 else '-x'] + faces_z)
    # Quoins on the six free corners.
    e = L['levels']['frontEaves']
    for cx, cz, sx, sz in ((0, 0, -1, -1), (42, 0, 1, -1), (42, 30, 1, 1), (34, 30, -1, 1), (8, 30, 1, 1), (0, 30, -1, 1)):
        y, k = PLINTH, 0
        while y < e - .5:
            lx, lz = (.5, .28) if k % 2 == 0 else (.28, .5)
            x0, x1 = sorted((cx + sx * .04, cx - sx * lx)); z0, z1 = sorted((cz + sz * .04, cz - sz * lz))
            bbox(fac, x0, y, z0, x1, y + .3, z1, 'HExt_Stone', faces=['+x' if sx > 0 else '-x', '+z' if sz > 0 else '-z', '+y', '-y'])
            y += .32; k += 1
    # Entrance: stone pilasters and lintel round the door; the name board above the hood is lettered in Unity.
    fd = next(o for o in L['openings'] if o['id'] == 'front-door'); x = fd['at']; half = fd['width'] / 2
    for side in (-1, 1):
        px = x + side * (half + .2)
        bbox(fac, px - .16, PLINTH, -.1, px + .16, fd['top'] + .1, 0, 'HExt_Stone', faces=['-z', '+x', '-x', '+y'])
    bbox(fac, x - half - .04, fd['top'], -.1, x + half + .04, fd['top'] + .1, 0, 'HExt_Stone', faces=['-z', '-y'])
    # Downpipes at the street corners of the pavilions and in the court corners.
    for px, pz in ((-.1, .3), (42.1, .3), (8.1, 12.3), (33.9, 12.3), (8.1, 29.7), (33.9, 29.7), (-.1, 29.7), (42.1, 29.7)):
        cylinder(part('Ext_Roof'), px, pz, .045, 0, e - .1, 'HExt_Gutter', seg=8)


def build_hatch(o, w):
    """Serving hatch: a counter board through the wall and a panelled apron on both faces."""
    p = part('Int_Trim')
    lo, hi, b = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o['bottom']
    c0, c1 = w['c'] - w['t'] / 2, w['c'] + w['t'] / 2
    if w['axis'] == 'x':
        bbox(p, lo - .05, b, c0 - .2, hi + .05, b + .045, c1 + .2, 'HInt_FurnLight')
    else:
        bbox(p, c0 - .2, b, lo - .05, c1 + .2, b + .045, hi + .05, 'HInt_FurnLight')


def build_joinery(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        w = W[o['wall']]
        if o['kind'] == 'window':
            P.build_window(L, o, w)
        elif o['kind'] in ('door', 'arch'):
            P.casing(L, o, w); P.jamb_lining(o, w)
            for i in range(o['leaves']):
                P.build_leaf(o, w, i, o['leaves'])
            if o['style'] == 'front':
                P.build_front_transom(o, w)
        elif o['kind'] == 'hatch':
            P.casing(L, o, w); build_hatch(o, w)


def build_lamps(L):
    ceil = L['levels']['groundCeiling']
    for l in L['lamps']:
        fit = part('Lamp_' + l['id']); bulb = part('Bulb_' + l['id'])
        x, y, z = l['x'], l['y'], l['z']
        if l['kind'] == 'pendant':
            # Opal globe on a chain: the period ward and corridor fitting.
            cylinder(fit, x, z, .07, ceil - .04, ceil, 'HInt_Brass', seg=10)
            bbox(fit, x - .007, y + .2, z - .007, x + .007, ceil - .04, z + .007, 'HInt_Brass')
            frustum(fit, x, z, .07, .05, y + .14, y + .2, 'HInt_Brass', seg=10, cap=True)
            for r0, r1, y0, y1 in ((.06, .135, -.16, -.1), (.135, .17, -.1, -.01), (.17, .135, -.01, .08), (.135, .055, .08, .14)):
                frustum(bulb, x, z, r0, r1, y + y0, y + y1, 'H_Bulb', seg=12)
            cylinder(bulb, x, z, .06, y - .162, y - .16, 'H_Bulb', seg=12)
        elif l['kind'] == 'oplamp':
            # Theatre lamp: a wide enamelled reflector on a ceiling stem.
            cylinder(fit, x, z, .09, ceil - .05, ceil, 'HInt_Steel', seg=10)
            bbox(fit, x - .02, y + .22, z - .02, x + .02, ceil - .05, z + .02, 'HInt_Steel')
            frustum(fit, x, z, .46, .1, y + .02, y + .22, 'HInt_Enamel', seg=14, cap=True)
            cylinder(bulb, x, z, .2, y, y + .02, 'H_Bulb', seg=12)
        elif l['kind'] == 'desk':
            base_y = y - .37
            cylinder(fit, x, z, .085, base_y, base_y + .025, 'HInt_Brass', seg=12)
            bbox(fit, x - .012, base_y + .02, z - .012, x + .012, y + .02, z + .012, 'HInt_Brass')
            for i in range(8):
                a0, a1 = math.pi * i / 8, math.pi * (i + 1) / 8
                p0 = (x - .17, y + .05 + .07 * math.sin(a0), z - .07 * math.cos(a0)); p1 = (x + .17, y + .05 + .07 * math.sin(a0), z - .07 * math.cos(a0))
                p2 = (x + .17, y + .05 + .07 * math.sin(a1), z - .07 * math.cos(a1)); p3 = (x - .17, y + .05 + .07 * math.sin(a1), z - .07 * math.cos(a1))
                mid = (a0 + a1) / 2
                fit.quad([p0, p1, p2, p3], (0, math.sin(mid), -math.cos(mid)), 'HInt_LampGreen')
                fit.quad([p3, p2, p1, p0], (0, -math.sin(mid), math.cos(mid)), 'HInt_LampGreen')
            frustum(bulb, x, z, .028, .028, y - .01, y + .04, 'H_Bulb', seg=8, cap=True)
        elif l['kind'] == 'wall':
            # Lantern on an iron bracket; the street lanterns hang south of the front wall, the court one north of it.
            dz = -1 if l['room'] == 'street' else 1
            bz = z - dz * .2
            bbox(fit, x - .015, y + .24, min(bz, z), x + .015, y + .27, max(bz, z), 'HExt_Iron')
            bbox(fit, x - .11, y + .2, z - .11, x + .11, y + .24, z + .11, 'HExt_Iron')
            frustum(fit, x, z, .14, .02, y + .24, y + .36, 'HExt_Iron', seg=4, cap=True)
            bbox(fit, x - .07, y - .16, z - .07, x + .07, y - .13, z + .07, 'HExt_Iron')
            for cx, cz in ((-.085, -.085), (.085, -.085), (.085, .085), (-.085, .085)):
                bbox(fit, x + cx - .01, y - .14, z + cz - .01, x + cx + .01, y + .2, z + cz + .01, 'HExt_Iron')
            bbox(bulb, x - .075, y - .13, z - .075, x + .075, y + .2, z + .075, 'H_LampGlass')
        else:
            raise ValueError('Unknown lamp kind ' + l['kind'])


# ---------------------------------------------------------------------------
# Hospital furniture. Local frame: +z is the piece's front, y up from its base.
# ---------------------------------------------------------------------------
QUILT = {'rose': 'HInt_QuiltRose', 'blue': 'HInt_QuiltBlue', 'sage': 'HInt_QuiltSage', 'cream': 'HInt_QuiltCream'}
BOTTLES = ('HInt_BottleBrown', 'HInt_BottleBlue', 'HInt_BottleGreen', 'HInt_BottleClear', 'HInt_BottleBrown')


def bottles(f, x0, x1, y, z, seed, step=.085, tall=.16):
    rng = P.RNG
    x = x0 + .05
    while x < x1 - .04:
        r = rng.uniform(.022, .034); hgt = tall * rng.uniform(.7, 1.15)
        mat = BOTTLES[(seed + int(x * 97)) % len(BOTTLES)]
        cyl(f, x, z, r, y, y + hgt, mat, seg=6)
        cyl(f, x, z, r * .45, y + hgt, y + hgt + .035, mat, seg=5)
        x += step + rng.uniform(-.01, .02)


def castors(f, w, d, r=.04, inset=.04):
    for sx in (-1, 1):
        for sz in (-1, 1):
            wheel(f, sx * (w / 2 - inset), r, sz * (d / 2 - inset), r, .025, 'HInt_Rubber', seg=8)


def bedstead(f, w, d, head, foot, mat='HInt_Enamel'):
    """Iron bedstead: head at -z, foot at +z, mattress frame at .42."""
    t = .034
    for sz, top in ((-1, head), (1, foot)):
        z = sz * (d / 2 - t / 2)
        for sx in (-1, 1):
            x = sx * (w / 2 - t / 2)
            f.bb(x - t / 2, .06, z - t / 2, x + t / 2, top, z + t / 2, mat)
            ball(f, x, top + .02, z, .028, 'HInt_Brass', seg=6)
        f.bb(-w / 2 + t, top - .07, z - t / 2 + .004, w / 2 - t, top - .04, z + t / 2 - .004, mat)
        f.bb(-w / 2 + t, .44, z - t / 2 + .004, w / 2 - t, .47, z + t / 2 - .004, mat)
        n = 6
        for i in range(1, n):
            x = -w / 2 + w * i / n
            f.bb(x - .008, .47, z - .008, x + .008, top - .07, z + .008, mat)
    for sx in (-1, 1):
        x = sx * (w / 2 - t / 2)
        f.bb(x - .012, .385, -d / 2 + t, x + .012, .42, d / 2 - t, 'HInt_Iron')
    castors(f, w, d, r=.03, inset=.017)


def hospital_piece(F, L):
    kind = F['kind']
    p = part('Furn_' + F['id'], (F['x'], F['y'], F['z']))
    f = Frame(p, F['x'], F['y'], F['z'], F['yaw'])
    w, d, h = F['w'], F['d'], F['h']
    wood, light, en = 'HInt_Furniture', 'HInt_FurnLight', 'HInt_Enamel'
    if kind == 'wardbed':
        # extra: '<quilt>', '<quilt>:propped' (someone sits up against the pillows), '<quilt>:lying', or 'child'.
        child = F['extra'] == 'child'
        colour, _, state = ('rose:propped' if child else F['extra']).partition(':')
        quilt = QUILT[colour]; propped = state == 'propped'; lying = state == 'lying'
        bedstead(f, w, d, h, h - .28)
        f.bb(-w / 2 + .03, .42, -d / 2 + .04, w / 2 - .03, .56, d / 2 - .04, 'HInt_Ticking')
        top = .56
        # Where the sheet is turned down: at the waist of someone sitting up (the child half lies further down the bed).
        fold = -d / 2 + (.6 if child else .44 if propped else .5)
        f.bb(-w / 2 + .01, top, fold + .14, w / 2 - .01, top + .05, d / 2 - .05, quilt, faces=['+y', '+z', '-z'])
        f.bb(-w / 2 + .01, top, fold, w / 2 - .01, top + .055, fold + .14, 'HInt_Linen', faces=['+y', '+z', '-z'])
        for sx in (-1, 1):      # the covers hang over both sides
            x0, x1 = sorted((sx * (w / 2 - .01), sx * (w / 2 + .012)))
            f.bb(x0, .36, fold + .14, x1, top + .05, d / 2 - .05, quilt, faces=['+x' if sx > 0 else '-x', '+y', '-y', '+z', '-z'])
            f.bb(x0, .38, fold, x1, top + .055, fold + .14, 'HInt_Linen', faces=['+x' if sx > 0 else '-x', '+y', '-y', '+z', '-z'])
        if propped:
            # Propped up for the day: two pillows leaning on the head rail and the covers raised over the legs;
            # on the child's bed also the red hospital blanket folded at the foot.
            # (z0, y0, z1, y1, th): the front slopes from the mattress at z1 up to z0 + th; the top is th deep.
            # The child half lies (about 30 degrees from upright): a long wedge for her back and a pillow for her head.
            pillows = (((-d / 2 + .04, top + .02, -d / 2 + .46, top + .40, .2), (-d / 2 + .03, top + .30, -d / 2 + .29, top + .58, .1)) if child
                       else ((-d / 2 + .08, top + .02, -d / 2 + .3, top + .52, .1), (-d / 2 + .16, top + .0, -d / 2 + .4, top + .3, .1)))
            for k, (z0, y0, z1, y1, th) in enumerate(pillows):
                x0, x1 = -w / 2 + .1, w / 2 - .1
                slope = (0, z1 - z0 - th, y1 - y0)
                n = math.hypot(*slope)
                quad(f, [(x0, y0, z1), (x1, y0, z1), (x1, y1, z0 + th), (x0, y1, z0 + th)], (0, slope[1] / n, slope[2] / n), 'HInt_Linen')
                quad(f, [(x0, y1, z0 + th), (x1, y1, z0 + th), (x1, y1, z0), (x0, y1, z0)], (0, 1, 0), 'HInt_Linen')
                for sx, xx in ((-1, x0), (1, x1)):
                    quad(f, [(xx, y0, z1), (xx, y1, z0 + th), (xx, y1, z0), (xx, y0, z0)], (sx, 0, 0), 'HInt_Linen')
            if child:
                f.bb(-w / 2 + .03, top + .05, d / 2 - .33, w / 2 - .03, top + .13, d / 2 - .07, 'HInt_BlanketRed', faces=['+y', '+x', '-x', '+z', '-z'])
        else:
            f.bb(-w / 2 + .1, top, -d / 2 + .08, w / 2 - .1, top + .11, fold - .04, 'HInt_Linen', faces=['+y', '+x', '-x', '+z', '-z'])
        if propped or lying:
            # The covers raised over whoever is in the bed. Its own object: Unity shows it only while
            # that person's figure is in the game, so a bed never has a body-shaped quilt and no body.
            o = Frame(part('Furn_' + F['id'] + '-occupied', (F['x'], F['y'], F['z'])), F['x'], F['y'], F['z'], F['yaw'])
            # Full width of the mattress and down to the foot rail, so it reads as bedding drawn over
            # a body rather than a separate box; only the height drops a little toward the feet.
            half = w / 2 - .01
            rise = ((.13 if d < 1.8 else .16) if propped else .24)
            end = d / 2 - (.36 if child else .06)
            covers(o, top + .045, [(fold - .006, half, rise), (fold + .14, half, rise), (end - .35, half, rise * .8), (end, half, rise * .45)],
                   ['HInt_Linen', quilt, quilt])
        # Chart board hooked on the foot rail.
        f.bb(-.11, h - .28 - .36, d / 2 + .002, .11, h - .28 - .06, d / 2 + .012, wood)
        f.bb(-.095, h - .28 - .345, d / 2 + .012, .095, h - .28 - .09, d / 2 + .015, 'HInt_Paper', faces=['+z'])
    elif kind == 'locker':
        f.bb(-w / 2, .06, -d / 2, w / 2, h, d / 2, light)
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .04) - .02, 0, sz * (d / 2 - .04) - .02, sx * (w / 2 - .04) + .02, .06, sz * (d / 2 - .04) + .02, wood)
        f.bb(-w / 2 - .01, h, -d / 2 - .01, w / 2 + .01, h + .02, d / 2 + .01, wood)
        f.bb(-w / 2 + .03, .1, d / 2, w / 2 - .03, h - .2, d / 2 + .012, wood, faces=['+z', '+x', '-x', '+y', '-y'])
        f.bb(-w / 2 + .03, h - .17, d / 2, w / 2 - .03, h - .03, d / 2 + .012, wood, faces=['+z', '+x', '-x', '+y', '-y'])
        f.bb(-.02, h - .11, d / 2 + .012, .02, h - .09, d / 2 + .03, 'HInt_Brass')
        cone(f, -.09, -.06, .035, .04, h + .02, h + .1, en, seg=8)         # enamel mug
        f.bb(-.05, h + .05, -.065, -.035, h + .085, -.055, en)
    elif kind == 'overbed':
        f.bb(-w / 2, h - .03, -d / 2, w / 2, h, d / 2, light)
        for sx in (-1, 1):
            x = sx * (w / 2 - .03)
            f.bb(x - .02, .05, -.02, x + .02, h - .03, .02, en)
            f.bb(x - .025, .03, -d / 2, x + .025, .06, d / 2, en)
            for sz in (-1, 1):
                wheel(f, x, .03, sz * (d / 2 - .03), .03, .02, 'HInt_Rubber', seg=8)
    elif kind == 'applebasket':
        cone(f, 0, 0, .09, .15, 0, .1, 'HInt_Wicker', seg=18)
        cyl(f, 0, 0, .09, 0, .008, 'HInt_Wicker', seg=18)
        for i, (ax, az, ay) in enumerate(((0, 0, .15), (.07, .02, .11), (-.06, .05, .11), (.01, -.075, .11), (-.05, -.045, .12), (.06, -.05, .13))):
            apple(f, ax, ay, az, .042, 'HInt_AppleGreen' if i == 3 else 'HInt_Apple')
    elif kind == 'drawing':
        f.bb(-w / 2, 0, -d / 2, w / 2, .002, d / 2, 'HInt_Paper', faces=['+y', '+x', '-x', '+z', '-z'])
        y = .0045

        def mark(x0, z0, x1, z1, mat, layer=0):
            f.bb(x0, y, z0, x1, y + .0004 + layer * .0025, z1, mat, faces=['+y'])
        # A house, three figures and a car, in wax crayon.
        mark(-.11, -.02, -.03, .05, 'HInt_ToyRed'); mark(-.085, -.02, -.06, .015, 'HInt_ToyBlue', 1)
        quad(f, [(-.125, y + .0004, .0505), (-.015, y + .0004, .0505), (-.07, y + .0004, .095)], (0, 1, 0), 'HInt_ToyBlue')
        for x, tall, mat in ((.0, .06, 'HInt_ToyBlue'), (.035, .05, 'HInt_ToyRed'), (.062, .032, 'HInt_ToyYellow')):
            mark(x - .006, -.02, x + .006, -.02 + tall, mat); mark(x - .011, -.02 + tall, x + .011, -.02 + tall + .02, mat)
        mark(.085, -.02, .135, .0, 'HInt_ToyBlue'); mark(.097, .0, .125, .014, 'HInt_ToyBlue')
        mark(.09, -.03, .102, -.02, 'HInt_Black'); mark(.118, -.03, .13, -.02, 'HInt_Black')
        mark(-.13, -.045, .14, -.038, 'HInt_AppleGreen')
    elif kind == 'crayons':
        for i, mat in enumerate(('HInt_ToyRed', 'HInt_ToyBlue', 'HInt_ToyYellow', 'HInt_AppleGreen')):
            f.bb(-w / 2 + i * .035, 0, -.04 + i * .012, -w / 2 + i * .035 + .012, .012, .04 + i * .012, mat)
    elif kind == 'flowers':
        cone(f, 0, 0, .035, .05, 0, .16, 'HInt_BottleClear', seg=8)
        for i in range(5):
            a = i * 1.26; r = .05
            f.bb(math.cos(a) * r * .5 - .004, .12, math.sin(a) * r * .5 - .004, math.cos(a) * r * .5 + .004, .26 + .02 * (i % 2), math.sin(a) * r * .5 + .004, 'HInt_Leaf')
            ball(f, math.cos(a) * r, .28 + .02 * (i % 2), math.sin(a) * r, .028, ('HInt_Paper', 'HInt_ToyYellow', 'HInt_QuiltRose')[i % 3], seg=6)
    elif kind == 'counter':
        f.bb(-w / 2, .08, -d / 2 + .04, w / 2, h - .04, d / 2 - .03, wood)
        f.bb(-w / 2 + .02, 0, -d / 2 + .06, w / 2 - .02, .08, d / 2 - .07, 'HInt_Black')
        f.bb(-w / 2 - .03, h - .04, -d / 2, w / 2 + .03, h, d / 2 + .04, light)
        n = max(2, round(w / .8))
        for i in range(n):
            x0 = -w / 2 + .06 + i * (w - .12) / n; x1 = x0 + (w - .12) / n - .06
            f.bb(x0, .2, d / 2 - .03, x1, h - .16, d / 2 - .012, light, faces=['+z', '+x', '-x', '+y', '-y'])
        f.bb(-w / 2 + .1, .34, d / 2 + .02, w / 2 - .1, .37, d / 2 + .05, 'HInt_Brass')        # foot rail
        for i in range(3):
            x = -w / 2 + .12 + i * (w - .24) / 2
            f.bb(x - .012, .1, d / 2 - .03, x + .012, .36, d / 2 + .035, 'HInt_Brass')
    elif kind == 'handbell':
        cone(f, 0, 0, .05, .02, 0, .06, 'HInt_Brass', seg=10, cap=True)
        cyl(f, 0, 0, .01, .06, .12, wood, seg=6)
    elif kind == 'keyboard':
        f.bb(-w / 2, 0, -.012, w / 2, h, .012, wood)
        for r in range(3):
            for c in range(6):
                x = -w / 2 + .1 + c * (w - .2) / 5; y = .12 + r * .2
                f.bb(x - .006, y + .06, .012, x + .006, y + .075, .03, 'HInt_Brass')
                if (r * 6 + c) % 4 != 1:
                    f.bb(x - .012, y - .03, .016, x + .012, y + .062, .02, 'HInt_Steel')
    elif kind == 'stool':
        cyl(f, 0, 0, w / 2, h - .035, h, light, seg=12)
        for i in range(4):
            a = math.pi / 4 + i * math.pi / 2
            f.bb(math.cos(a) * (w / 2 - .05) - .017, 0, math.sin(a) * (w / 2 - .05) - .017, math.cos(a) * (w / 2 - .05) + .017, h - .035, math.sin(a) * (w / 2 - .05) + .017, wood)
        cyl(f, 0, 0, w / 2 - .06, .2, .215, wood, seg=10, top=True)
    elif kind == 'palm':
        cone(f, 0, 0, .14, .2, 0, .34, 'HInt_Pot', seg=10)
        cyl(f, 0, 0, .19, .3, .31, 'HInt_Leather', seg=10)
        f.bb(-.02, .3, -.02, .02, .62, .02, 'HInt_Leather')
        for i in range(9):
            a = i * 2 * math.pi / 9 + .2; lift = .55 + .28 * (i % 3) / 2
            ca, sa = math.cos(a), math.sin(a); wd = .07
            base = (0, .55, 0); mid = (ca * .3, .55 + lift * .9, sa * .3); tip = (ca * .62, .55 + lift * .55, sa * .62)
            slab2(f, [(base[0] - sa * .02, base[1], base[2] + ca * .02), (base[0] + sa * .02, base[1], base[2] - ca * .02),
                      (mid[0] + sa * wd, mid[1], mid[2] - ca * wd), (mid[0] - sa * wd, mid[1], mid[2] + ca * wd)], (-ca * .6, .8, -sa * .6), 'HInt_Leaf')
            slab2(f, [(mid[0] - sa * wd, mid[1], mid[2] + ca * wd), (mid[0] + sa * wd, mid[1], mid[2] - ca * wd), (tip[0], tip[1], tip[2])], (ca * .5, .85, sa * .5), 'HInt_Leaf')
    elif kind == 'donorboard':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, wood)
        f.bb(-w / 2 - .04, h, -d / 2 - .01, w / 2 + .04, h + .07, d / 2 + .02, wood)
        f.bb(-w / 2 + .06, h - .2, d / 2, w / 2 - .06, h - .1, d / 2 + .004, 'HInt_Gold', faces=['+z'])
        for col in (-1, 1):
            for r in range(11):
                y = h - .32 - r * .1
                ln = (w / 2 - .14) * (.6 + .4 * ((r * 7 + col) % 5) / 4)
                x0 = col * .04 if col > 0 else -.04 - ln
                f.bb(x0, y, d / 2, x0 + ln, y + .022, d / 2 + .004, 'HInt_Gold', faces=['+z'])
    elif kind == 'signboard':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_SignGreen')
        for (x0, y0, x1, y1) in ((-w / 2 + .02, .02, w / 2 - .02, .03), (-w / 2 + .02, h - .03, w / 2 - .02, h - .02)):
            f.bb(x0, y0, d / 2, x1, y1, d / 2 + .002, 'HInt_Paper', faces=['+z'])
    elif kind == 'radiator':
        n = max(4, round(w / .075))
        for i in range(n):
            x = -w / 2 + (i + .5) * w / n
            f.bb(x - w / n * .36, .09, -d / 2 + .02, x + w / n * .36, h, d / 2, 'HExt_Paint')
        f.bb(-w / 2, .12, -d / 2 + .05, w / 2, .16, d / 2 - .04, 'HExt_Paint'); f.bb(-w / 2, h - .1, -d / 2 + .05, w / 2, h - .06, d / 2 - .04, 'HExt_Paint')
        for sx in (-1, 1):
            f.bb(sx * (w / 2 - .06) - .025, 0, -d / 2 + .03, sx * (w / 2 - .06) + .025, .09, d / 2 - .01, 'HInt_Iron')
        f.bb(w / 2, .12, -.02, w / 2 + .05, .16, .02, 'HInt_Brass')
    elif kind == 'examcouch':
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .05) - .02, 0, sz * (d / 2 - .06) - .02, sx * (w / 2 - .05) + .02, h - .08, sz * (d / 2 - .06) + .02, en)
        f.bb(-w / 2, h - .12, -d / 2, w / 2, h - .08, d / 2, en)
        f.bb(-w / 2 + .01, h - .08, -d / 2 + .5, w / 2 - .01, h, d / 2 - .01, 'HInt_Leather')
        quad(f, [(-w / 2 + .01, h, -d / 2 + .5), (w / 2 - .01, h, -d / 2 + .5), (w / 2 - .01, h + .18, -d / 2 + .02), (-w / 2 + .01, h + .18, -d / 2 + .02)], (0, .93, .36), 'HInt_Leather')
        for sx in (-1, 1):
            quad(f, [(sx * (w / 2 - .01), h - .08, -d / 2 + .5), (sx * (w / 2 - .01), h, -d / 2 + .5), (sx * (w / 2 - .01), h + .18, -d / 2 + .02), (sx * (w / 2 - .01), h - .08, -d / 2 + .02)], (sx, 0, 0), 'HInt_Leather')
        f.bb(-w / 2 + .01, h - .08, -d / 2 + .01, w / 2 - .01, h + .18, -d / 2 + .02, 'HInt_Leather', faces=['-z'])
        f.bb(-w / 2 + .04, h, .1, w / 2 - .04, h + .012, d / 2 - .04, 'HInt_Linen', faces=['+y', '+x', '-x', '+z', '-z'])
    elif kind == 'screen':
        n = 3; pw = w / n
        for i in range(n):
            x0 = -w / 2 + i * pw; zz = (.0 if i % 2 == 0 else .0)
            f.bb(x0 + .01, .08, -.012, x0 + .03, h, .012, en); f.bb(x0 + pw - .03, .08, -.012, x0 + pw - .01, h, .012, en)
            f.bb(x0 + .03, h - .03, -.01, x0 + pw - .03, h, .01, en); f.bb(x0 + .03, .3, -.01, x0 + pw - .03, .33, .01, en)
            slab2(f, [(x0 + .03, .34, 0), (x0 + pw - .03, .34, 0), (x0 + pw - .03, h - .035, 0), (x0 + .03, h - .035, 0)], (0, 0, 1), 'HInt_Cloth')
        for x in (-w / 2 + .02, 0, w / 2 - .02):
            f.bb(x - .015, .02, -.16, x + .015, .08, .16, en)
            for sz in (-1, 1):
                wheel(f, x, .028, sz * .15, .028, .02, 'HInt_Rubber', seg=8)
    elif kind in ('medcabinet', 'bottleshelf'):
        glazed = kind == 'medcabinet'
        body = en if glazed else wood
        t = .025
        f.bb(-w / 2, 0, -d / 2, -w / 2 + t, h, d / 2, body); f.bb(w / 2 - t, 0, -d / 2, w / 2, h, d / 2, body)
        f.bb(-w / 2 + t, h - t, -d / 2, w / 2 - t, h, d / 2, body); f.bb(-w / 2 + t, 0, -d / 2, w / 2 - t, .1, d / 2, body)
        f.bb(-w / 2 + t, .1, -d / 2, w / 2 - t, h - t, -d / 2 + .015, body, faces=['+z'])
        low = .75 if glazed else .1
        if glazed:      # cupboard below, glass doors above
            f.bb(-w / 2 + t, .1, d / 2 - .02, w / 2 - t, low, d / 2, body, faces=['+z'])
            f.bb(-.006, .14, d / 2, .006, low - .04, d / 2 + .004, 'HInt_Iron', faces=['+z'])
            for sx in (-1, 1):
                f.bb(sx * .05 - .01, low - .2, d / 2, sx * .05 + .01, low - .1, d / 2 + .02, 'HInt_Brass')
            f.bb(-w / 2 + t, low, -d / 2 + .015, w / 2 - t, low + .02, d / 2, body)
        shelves = 3 if glazed else 5
        for k in range(shelves):
            y = low + .02 + k * (h - t - low - .02) / shelves
            if k:
                f.bb(-w / 2 + t, y - .018, -d / 2 + .015, w / 2 - t, y, d / 2 - .03, body)
            if F['extra'] == 'instruments':
                f.bb(-w / 2 + .08, y, -d / 2 + .08, -w / 2 + .38, y + .025, d / 2 - .08, 'HInt_Steel')
                f.bb(.04, y, -d / 2 + .08, w / 2 - .08, y + .06, d / 2 - .1, en)
            else:
                bottles(f, -w / 2 + t, w / 2 - t, y, -.01, k + len(F['id']), tall=.17 if glazed else .2)
                if not glazed:
                    bottles(f, -w / 2 + t, w / 2 - t, y, -d / 2 + .08, k + 3, step=.11, tall=.24)
        if glazed:
            for sx in (-1, 1):
                x0, x1 = sorted((sx * .008, sx * (w / 2 - t)))
                for (a0, b0, a1, b1) in ((x0, low + .02, x0 + .03, h - t), (x1 - .03, low + .02, x1, h - t), (x0, low + .02, x1, low + .05), (x0, h - t - .03, x1, h - t)):
                    f.bb(a0, b0, d / 2 - .02, a1, b1, d / 2, body)
                slab2(f, [(x0 + .03, low + .05, d / 2 - .01), (x1 - .03, low + .05, d / 2 - .01), (x1 - .03, h - t - .03, d / 2 - .01), (x0 + .03, h - t - .03, d / 2 - .01)], (0, 0, 1), 'H_Glass')
    elif kind == 'sink':
        # Wall basin on iron brackets, taps and a tiled splashback (the wall is behind, at -z).
        f.bb(-w / 2 - .1, h - .25, -d / 2, w / 2 + .1, h + .46, -d / 2 + .012, 'HInt_DadoTile', faces=['+z', '+x', '-x', '+y'])
        f.bb(-w / 2, h - .16, -d / 2 + .012, w / 2, h, d / 2, en, faces=['+x', '-x', '+z', '-y'])
        for (x0, z0, x1, z1) in ((-w / 2, -d / 2 + .012, w / 2, -d / 2 + .07), (-w / 2, d / 2 - .04, w / 2, d / 2), (-w / 2, -d / 2 + .07, -w / 2 + .04, d / 2 - .04), (w / 2 - .04, -d / 2 + .07, w / 2, d / 2 - .04)):
            f.bb(x0, h - .02, z0, x1, h, z1, en, faces=['+y'])
        f.bb(-w / 2 + .04, h - .13, -d / 2 + .07, w / 2 - .04, h - .125, d / 2 - .04, en, faces=['+y'])
        for (x0, z0, x1, z1, n) in ((-w / 2 + .04, -d / 2 + .07, w / 2 - .04, -d / 2 + .07, (0, 0, 1)), (w / 2 - .04, d / 2 - .04, -w / 2 + .04, d / 2 - .04, (0, 0, -1))):
            quad(f, [(x0, h - .125, z0), (x1, h - .125, z1), (x1, h - .02, z1), (x0, h - .02, z0)], n, en)
        for sx in (-1, 1):
            quad(f, [(sx * (w / 2 - .04), h - .125, -d / 2 + .07), (sx * (w / 2 - .04), h - .125, d / 2 - .04), (sx * (w / 2 - .04), h - .02, d / 2 - .04), (sx * (w / 2 - .04), h - .02, -d / 2 + .07)], (-sx, 0, 0), en)
            f.bb(sx * .13 - .012, h, -d / 2 + .03, sx * .13 + .012, h + .1, -d / 2 + .054, 'HInt_Brass')
            f.bb(sx * .13 - .012, h + .08, -d / 2 + .054, sx * .13 + .012, h + .1, -d / 2 + .14, 'HInt_Brass')
            f.bb(sx * (w / 2 - .06) - .012, h - .5, -d / 2 + .012, sx * (w / 2 - .06) + .012, h - .16, -d / 2 + .04, 'HInt_Iron')
        cyl(f, 0, .0, .02, h - .62, h - .16, 'HInt_Steel', seg=6, top=False)
    elif kind == 'scale':
        f.bb(-w / 2, 0, -d / 2 + .1, w / 2, .08, d / 2, 'HInt_Iron'); f.bb(-w / 2 + .03, .08, -d / 2 + .13, w / 2 - .03, .1, d / 2 - .03, 'HInt_Steel', faces=['+y', '+x', '-x', '+z', '-z'])
        f.bb(-.03, 0, -d / 2, .03, h, -d / 2 + .08, en)
        f.bb(-w / 2 + .02, h - .12, -d / 2 + .01, w / 2 - .02, h - .08, -d / 2 + .07, 'HInt_Brass')
        f.bb(.05, h - .2, -d / 2 + .03, .08, h - .12, -d / 2 + .05, 'HInt_Iron')
    elif kind == 'eyechart':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_Paper')
        f.bb(-w / 2 - .015, -.015, -d / 2, w / 2 + .015, 0, d / 2 + .004, 'HInt_Black'); f.bb(-w / 2 - .015, h, -d / 2, w / 2 + .015, h + .015, d / 2 + .004, 'HInt_Black')
        y = h - .14
        for r, size in enumerate((.09, .065, .05, .04, .03, .024, .018)):
            n = r + 1; gap = (w - .1) / n
            for c in range(n):
                x = -w / 2 + .05 + (c + .5) * gap
                f.bb(x - size / 2, y, d / 2, x + size / 2, y + size, d / 2 + .002, 'HInt_Black', faces=['+z'])
            y -= size + .04
    elif kind == 'pharmscale':
        f.bb(-w / 2, 0, -d / 2, w / 2, .03, d / 2, wood)
        f.bb(-.01, .03, -.01, .01, h, .01, 'HInt_Brass'); f.bb(-w / 2 + .03, h - .02, -.006, w / 2 - .03, h - .008, .006, 'HInt_Brass')
        for sx in (-1, 1):
            cyl(f, sx * (w / 2 - .05), 0, .055, .1, .108, 'HInt_Brass', seg=10)
            f.bb(sx * (w / 2 - .05) - .002, .108, -.002, sx * (w / 2 - .05) + .002, h - .02, .002, 'HInt_Brass')
    elif kind == 'mortar':
        cone(f, 0, 0, .05, .08, 0, h, en, seg=10)
        f.bb(.02, h - .02, -.012, .1, h + .05, .012, en)
    elif kind == 'cashbox':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_Black')
        f.bb(-.05, h, -.012, .05, h + .02, .012, 'HInt_Brass'); f.bb(-.015, h * .45, d / 2, .015, h * .7, d / 2 + .01, 'HInt_Brass')
    elif kind == 'safe':
        f.bb(-w / 2, .06, -d / 2, w / 2, h, d / 2, 'HInt_LampGreen')
        f.bb(-w / 2 + .04, .12, d / 2, w / 2 - .04, h - .06, d / 2 + .02, 'HInt_LampGreen', faces=['+z', '+x', '-x', '+y', '-y'])
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .07) - .04, 0, sz * (d / 2 - .07) - .04, sx * (w / 2 - .07) + .04, .06, sz * (d / 2 - .07) + .04, 'HInt_Iron')
        f.bb(-.05, h * .5, d / 2 + .02, .05, h * .5 + .1, d / 2 + .035, 'HInt_Brass'); f.bb(.1, h * .45, d / 2 + .02, .13, h * .7, d / 2 + .05, 'HInt_Brass')
    elif kind == 'optable':
        f.bb(-.24, 0, -.34, .24, .06, .34, 'HInt_Iron')
        cyl(f, 0, 0, .09, .06, h - .1, en, seg=10)
        f.bb(-w / 2, h - .1, -d / 2, w / 2, h - .06, d / 2, 'HInt_Steel')
        f.bb(-w / 2 + .01, h - .06, -d / 2 + .5, w / 2 - .01, h, d / 2 - .01, 'HInt_Leather')
        f.bb(-w / 2 + .01, h - .06, -d / 2 + .01, w / 2 - .01, h, -d / 2 + .46, 'HInt_Leather')
        f.bb(-w / 2 + .03, h, -d / 2 + .06, w / 2 - .03, h + .07, -d / 2 + .36, 'HInt_Linen', faces=['+y', '+x', '-x', '+z', '-z'])
        f.bb(w / 2, h - .3, -.03, w / 2 + .05, h - .26, .03, 'HInt_Steel'); f.bb(w / 2 + .03, h - .34, -.012, w / 2 + .05, h - .12, .012, 'HInt_Steel')
    elif kind == 'sterilizer':
        f.bb(-w / 2, .5, -d / 2, w / 2, h, d / 2, 'HInt_Steel')
        f.bb(-w / 2 - .01, h, -d / 2 - .01, w / 2 + .01, h + .03, d / 2 + .01, 'HInt_Steel')
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .04) - .015, 0, sz * (d / 2 - .04) - .015, sx * (w / 2 - .04) + .015, .5, sz * (d / 2 - .04) + .015, en)
        f.bb(-w / 2 + .04, .14, -d / 2 + .04, w / 2 - .04, .16, d / 2 - .04, en)
        f.bb(-.06, h + .03, -.012, .06, h + .06, .012, 'HInt_Black'); f.bb(-w / 2 - .04, .62, -.02, -w / 2, .66, .02, 'HInt_Brass')
    elif kind == 'trolley':
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .015) - .012, .08, sz * (d / 2 - .015) - .012, sx * (w / 2 - .015) + .012, h, sz * (d / 2 - .015) + .012, en)
        for y in (.28, h - .03):
            f.bb(-w / 2, y, -d / 2, w / 2, y + .02, d / 2, en)
            for (x0, z0, x1, z1) in ((-w / 2, -d / 2, w / 2, -d / 2 + .012), (-w / 2, d / 2 - .012, w / 2, d / 2), (-w / 2, -d / 2, -w / 2 + .012, d / 2), (w / 2 - .012, -d / 2, w / 2, d / 2)):
                f.bb(x0, y + .02, z0, x1, y + .05, z1, en)
        castors(f, w, d, r=.04, inset=.015)
        if F['extra'] == 'instruments':
            f.bb(-w / 2 + .05, h - .01, -d / 2 + .08, w / 2 - .05, h + .02, .08, 'HInt_Steel')
            cone(f, 0, d / 2 - .16, .07, .1, h - .01, h + .06, en, seg=10)
        else:
            cone(f, -.05, -d / 2 + .18, .09, .13, h - .01, h + .07, en, seg=10)        # basin
            cone(f, .08, .14, .05, .06, h - .01, h + .14, en, seg=8)                    # jug
            bottles(f, -w / 2 + .02, w / 2 - .02, h - .01, d / 2 - .1, 2, tall=.13)
            f.bb(-w / 2 + .06, .3, -d / 2 + .1, w / 2 - .06, .36, d / 2 - .1, 'HInt_Linen')
    elif kind == 'gascylinder':
        cyl(f, 0, 0, .19, 0, .03, 'HInt_Iron', seg=10)
        cyl(f, 0, 0, .1, .03, h - .22, 'HInt_Black', seg=12)
        cone(f, 0, 0, .1, .035, h - .22, h - .12, 'HInt_Black', seg=12, cap=True)
        cyl(f, 0, 0, .025, h - .12, h - .04, 'HInt_Brass', seg=6)
        f.bb(-.07, h - .08, -.015, .07, h - .05, .015, 'HInt_Brass')
        f.bb(.1, .2, -.012, .13, h - .3, .012, 'HInt_Iron'); f.bb(-.02, .9, .1, .02, .93, .13, 'HInt_Iron')
    elif kind == 'firebuckets':
        f.bb(-w / 2, h - .06, -d / 2, w / 2, h, -d / 2 + .025, wood)
        for i in range(3):
            x = -w / 2 + .2 + i * (w - .4) / 2
            f.bb(x - .012, h - .1, -d / 2 + .025, x + .012, h - .04, -d / 2 + .09, 'HInt_Iron')
            cone(f, x, -d / 2 + .09 + .11, .085, .125, h - .4, h - .12, 'HInt_FireRed', seg=10)
            cyl(f, x, -d / 2 + .09 + .11, .085, h - .4, h - .392, 'HInt_FireRed', seg=10)
            slab2(f, [(x - .11, h - .12, -d / 2 + .2), (x + .11, h - .12, -d / 2 + .2), (x + .06, h - .03, -d / 2 + .09), (x - .06, h - .03, -d / 2 + .09)], (0, .5, .86), 'HInt_Iron')
    elif kind == 'stretcher':
        for sx in (-1, 1):
            for sz in (-1, 1):
                f.bb(sx * (w / 2 - .03) - .015, .12, sz * (d / 2 - .25) - .015, sx * (w / 2 - .03) + .015, h - .08, sz * (d / 2 - .25) + .015, en)
                wheel(f, sx * (w / 2 - .03), .1, sz * (d / 2 - .25), .1, .035, 'HInt_Rubber', seg=12, hub='HInt_Enamel')
        f.bb(-w / 2, h - .08, -d / 2, w / 2, h - .05, d / 2, en)
        f.bb(-w / 2 + .03, .32, -d / 2 + .22, w / 2 - .03, .34, d / 2 - .22, en)
        f.bb(-w / 2 + .02, h - .05, -d / 2 + .03, w / 2 - .02, h + .02, d / 2 - .03, 'HInt_Ticking')
        f.bb(-w / 2 + .08, h + .02, -d / 2 + .08, w / 2 - .08, h + .1, -d / 2 + .42, 'HInt_Linen', faces=['+y', '+x', '-x', '+z', '-z'])
        f.bb(-w / 2 + .03, h + .02, -.1, w / 2 - .03, h + .06, d / 2 - .1, 'HInt_BlanketRed', faces=['+y', '+x', '-x', '+z', '-z'])
        for sx in (-1, 1):
            f.bb(sx * (w / 2 - .03) - .012, h - .08, -d / 2 - .14, sx * (w / 2 - .03) + .012, h - .05, -d / 2, en)
    elif kind == 'wheelchair':
        seat_y = .5
        f.bb(-w / 2 + .09, seat_y - .03, -d / 2 + .22, w / 2 - .09, seat_y, d / 2 - .25, light)
        slab2(f, [(-w / 2 + .1, seat_y, -d / 2 + .24), (w / 2 - .1, seat_y, -d / 2 + .24), (w / 2 - .1, h, -d / 2 + .16), (-w / 2 + .1, h, -d / 2 + .16)], (0, .1, 1), 'HInt_Wicker')
        for sx in (-1, 1):
            x = sx * (w / 2 - .09)
            f.bb(x - .017, .1, -d / 2 + .2, x + .017, h + .03, -d / 2 + .235, wood)
            f.bb(x - .017, .28, d / 2 - .29, x + .017, seat_y + .2, d / 2 - .255, wood)
            f.bb(x - .017, seat_y + .18, -d / 2 + .22, x + .017, seat_y + .21, d / 2 - .26, wood)
            f.bb(x - .012, h, -d / 2 + .06, x + .012, h + .025, -d / 2 + .22, wood)
            wheel(f, sx * (w / 2 - .02), .3, -d / 2 + .3, .3, .03, 'HInt_Rubber', seg=16, hub='HInt_FurnLight')
        f.bb(-w / 2 + .12, .14, d / 2 - .22, w / 2 - .12, .16, d / 2, light)
        f.bb(-.012, .16, d / 2 - .29, .012, seat_y - .03, d / 2 - .26, wood)
        wheel(f, 0, .07, d / 2 - .08, .07, .03, 'HInt_Rubber', seg=10)
    elif kind == 'toybox':
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_ToyBlue')
        f.bb(-w / 2 - .01, h, -d / 2 - .01, w / 2 + .01, h + .025, d / 2 + .01, light)
        for i, mat in enumerate(('HInt_ToyRed', 'HInt_ToyYellow', 'HInt_AppleGreen')):
            f.bb(-w / 2 + .1 + i * .22, h + .025, -.06, -w / 2 + .2 + i * .22, h + .125, .04, mat)
        ball(f, w / 2 - .14, h + .11, .02, .085, 'HInt_ToyRed', seg=8)
    elif kind == 'rockinghorse':
        grey = 'HInt_HorseGrey'
        for sx in (-1, 1):       # rockers: three straight pieces along a shallow curve
            x = sx * (w / 2 - .03)
            f.bb(x - .02, .0, -.22, x + .02, .04, .22, 'HInt_ToyRed')
            for sz in (-1, 1):
                quad(f, [(x - .02, .0, sz * .22), (x + .02, .0, sz * .22), (x + .02, .1, sz * (d / 2)), (x - .02, .1, sz * (d / 2))], (0, -.93, sz * .37), 'HInt_ToyRed')
                quad(f, [(x - .02, .04, sz * .22), (x - .02, .14, sz * (d / 2)), (x + .02, .14, sz * (d / 2)), (x + .02, .04, sz * .22)], (0, .93, -sz * .37), 'HInt_ToyRed')
                quad(f, [(x + sx * .02, .0, sz * .22), (x + sx * .02, .04, sz * .22), (x + sx * .02, .14, sz * (d / 2)), (x + sx * .02, .1, sz * (d / 2))], (sx, 0, 0), 'HInt_ToyRed')
                quad(f, [(x - sx * .02, .0, sz * .22), (x - sx * .02, .1, sz * (d / 2)), (x - sx * .02, .14, sz * (d / 2)), (x - sx * .02, .04, sz * .22)], (-sx, 0, 0), 'HInt_ToyRed')
                f.bb(x - .025, .04, sz * .2 - .025, x + .025, .5, sz * .2 + .025, grey)
        f.bb(-.09, .45, -.3, .09, .68, .3, grey)
        f.bb(-.06, .62, .22, .06, .92, .36, grey); f.bb(-.055, .8, .3, .055, .93, .52, grey)
        f.bb(-.012, .7, .2, .012, .96, .26, 'HInt_Leather'); f.bb(-.02, .5, -.4, .02, .7, -.3, 'HInt_Leather')
        f.bb(-.1, .68, -.12, .1, .71, .12, 'HInt_ToyRed')
    elif kind == 'smalltable':
        f.bb(-w / 2, h - .03, -d / 2, w / 2, h, d / 2, 'HInt_ToyYellow')
        P.legs(f, w, d, h - .03, t=.04, inset=.03, mat='PInt_FurnLight')
    elif kind == 'smallchair':
        sh = .3
        f.bb(-w / 2, sh - .025, -d / 2, w / 2, sh, d / 2, 'HInt_ToyBlue')
        P.legs(f, w, d, sh - .025, t=.03, inset=.0, mat='PInt_FurnLight')
        f.bb(-w / 2, sh, -d / 2, -w / 2 + .03, h, -d / 2 + .03, light); f.bb(w / 2 - .03, sh, -d / 2, w / 2, h, -d / 2 + .03, light)
        f.bb(-w / 2 + .03, h - .1, -d / 2 + .004, w / 2 - .03, h - .01, -d / 2 + .026, 'HInt_ToyBlue')
    elif kind == 'blocks':
        for (x, z, s, mat, y) in ((-.08, 0, .06, 'HInt_ToyRed', 0), (0, .03, .06, 'HInt_ToyBlue', 0), (.08, -.02, .06, 'HInt_ToyYellow', 0), (-.04, .01, .06, 'HInt_AppleGreen', .06)):
            f.bb(x - s / 2, y, z - s / 2, x + s / 2, y + s, z + s / 2, mat)
    elif kind == 'frieze':
        # Painted nursery frieze: a row of ducks, boats and balls on a pale band.
        f.bb(-w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_Paper', faces=['+z', '+y', '-y', '+x', '-x'])
        n = int(w / .46)
        for i in range(n):
            x = -w / 2 + (i + .5) * w / n; mat = ('HInt_ToyYellow', 'HInt_ToyBlue', 'HInt_ToyRed')[i % 3]
            z = d / 2 + .003
            if i % 3 == 0:      # duck
                f.bb(x - .09, .14, d / 2, x + .07, .25, z, mat, faces=['+z']); f.bb(x + .03, .25, d / 2, x + .1, .33, z, mat, faces=['+z'])
                f.bb(x + .1, .27, d / 2, x + .14, .3, z, 'HInt_ToyRed', faces=['+z'])
            elif i % 3 == 1:    # boat
                f.bb(x - .11, .13, d / 2, x + .11, .19, z, mat, faces=['+z']); f.bb(x - .008, .19, d / 2, x + .008, .38, z, 'HInt_Leather', faces=['+z'])
                quad(f, [(x + .008, .2, z), (x + .11, .2, z), (x + .008, .37, z)], (0, 0, 1), 'HInt_Linen')
            else:               # ball
                for k in range(3):
                    f.bb(x - .07 + k * .012, .15 + k * .03, d / 2, x + .07 - k * .012, .3 - k * .03 + .03, z + k * .003, ('HInt_ToyRed', 'HInt_Paper', 'HInt_ToyBlue')[k], faces=['+z'])
        f.bb(-w / 2, .04, d / 2, w / 2, .06, d / 2 + .003, 'HInt_SignGreen', faces=['+z']); f.bb(-w / 2, h - .06, d / 2, w / 2, h - .04, d / 2 + .003, 'HInt_SignGreen', faces=['+z'])
    else:
        raise ValueError('Unknown hospital furniture kind ' + kind)


SHARED = {'bench', 'chair', 'table', 'desk', 'bookshelf', 'filingcabinet', 'coatstand', 'umbrella', 'noticeboard', 'stove',
          'deskphone', 'typewriter', 'ledger', 'paper', 'wardrobe', 'lowcabinet', 'desklamp'}


def render_previews(out_dir, collection):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True; scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1600, 1000
    scene.render.image_settings.file_format = 'JPEG'; scene.render.image_settings.quality = 88
    scene.world = bpy.data.worlds.new('HospitalPreviewWorld')
    cam_data = bpy.data.cameras.new('PreviewCam'); cam = bpy.data.objects.new('PreviewCam', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    lids = [o for o in collection.objects if o.name.startswith(('Ext_Roof', 'Int_Ceilings', 'Ext_Loggia'))]
    shots = {
        'street-front': ((21.0, 1.7, -22.0), (21.0, 3.2, 0.0), 62, []),
        'street-west': ((-12.0, 1.7, -14.0), (12.0, 3.0, 6.0), 58, []),
        'court': ((21.0, 1.7, 34.0), (21.0, 2.4, 12.0), 66, []),
        'aerial': ((-10.0, 26.0, -24.0), (21.0, 2.0, 14.0), 52, []),
        'plan-cutaway': ((21.0, 62.0, 13.0), (21.0, 0.0, 15.0), 44, lids),
        'hall-from-door': ((21.0, 1.66, .9), (22.6, 1.3, 6.0), 74, []),
        'enquiry-desk': ((20.2, 1.66, 4.0), (24.0, 1.2, 4.3), 66, []),
        'corridor-west': ((22.0, 1.66, 10.1), (2.0, 1.4, 10.1), 62, []),
        'children-ward': ((4.0, 1.66, 12.6), (4.3, 1.1, 24.0), 76, []),
        'child-bed': ((4.4, 1.66, 20.3), (7.2, .9, 19.1), 58, []),
        'general-ward': ((38.0, 1.66, 12.6), (38.0, 1.1, 24.0), 76, []),
        'theatre': ((38.3, 1.66, 8.0), (38.3, 1.0, 3.0), 72, []),
    }
    for name, (eye, target, fov, hide) in shots.items():
        for o in hide:
            o.hide_render = True
        cam.location = P.u(eye)
        cam.rotation_euler = (P.u(target) - P.u(eye)).to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(fov); cam_data.clip_start = .05; cam_data.clip_end = 300
        scene.render.filepath = str(out_dir / ('preview-' + name + '.jpg'))
        bpy.ops.render.render(write_still=True)
        for o in hide:
            o.hide_render = False
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument('--layout', default='assets/source/hospital/hospital-layout.json')
    ap.add_argument('--output', default='assets/source/hospital')
    ap.add_argument('--unity', default='game/Assets/Art/Hospital')
    ap.add_argument('--previews', default='')
    args = ap.parse_args(argv)
    L = json.loads(Path(args.layout).read_text(encoding='utf-8'))
    out = Path(args.output).resolve(); out.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'

    P.build_walls(L); build_dado(L); build_slabs(L); P.build_thresholds(L)
    build_roofs(L); build_exterior_details(L); build_joinery(L); build_lamps(L)
    for F in L['furniture']:
        if F['kind'] in SHARED:
            P.build_furniture_piece(F, L)
        else:
            hospital_piece(F, L)
    # Hospital palette for everything the shared kit wrote in police materials.
    for p in P.PARTS.values():
        p.faces = [(v, n, REMAP.get(m, m), uv) for v, n, m, uv in p.faces]
        stray = sorted({m for _, _, m, _ in p.faces if m not in MATERIALS})
        if stray:
            raise ValueError(f'{p.name} keeps materials outside the hospital palette: {stray}')

    collection = bpy.data.collections.new('Hospital')
    bpy.context.scene.collection.children.link(collection)
    mats = P.make_materials()
    root, stats = P.build_objects(collection, mats)
    root.name = 'HospitalVisual'
    used = sorted({m for p in P.PARTS.values() for _, _, m, _ in p.faces})
    manifest = dict(version=1, layoutVersion=L['version'], objects=stats, triangles=sum(stats.values()),
                    leaves=P.LEAVES, drawers=[], materials=[dict(name=k, hex=MATERIALS[k][0], metallic=MATERIALS[k][1], roughness=MATERIALS[k][2], tile=MATERIALS[k][3]) for k in used],
                    blender=bpy.app.version_string)
    (out / 'hospital-build-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    for m in list(bpy.data.materials):
        if m.users == 0:
            bpy.data.materials.remove(m)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out / 'Hospital.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    if args.unity:
        dest = Path(args.unity).resolve(); dest.mkdir(parents=True, exist_ok=True)
        (dest / 'Hospital.fbx').write_bytes((out / 'Hospital.fbx').read_bytes())
        (dest / 'HospitalBuildManifest.json').write_bytes((out / 'hospital-build-manifest.json').read_bytes())
    if args.previews:
        prev = Path(args.previews).resolve(); prev.mkdir(parents=True, exist_ok=True)
        render_previews(prev, collection)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'Hospital.blend'), compress=True)
    print('HOSPITAL_BLENDER_PASS', json.dumps(dict(objects=len(stats), triangles=manifest['triangles'], leaves=len(P.LEAVES), materials=len(used))))


if __name__ == '__main__':
    main()
