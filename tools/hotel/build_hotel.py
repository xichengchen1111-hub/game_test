"""Blender generator for the A gabled old inn (B08, 山墙老客栈).

Reads the shared layout contract (tools/hotel/hotel_layout.py output) and builds one editable
scene: roughstone ground storey and half-timbered upper storey with real door/window apertures,
two street gables, the steep slate roofs, the timber entrance porch and the open carport, the
service wing, floors with the stair well, beamed ceilings, the stair, joinery with movable
leaves on their hinges, the register desk's tilting lamp head, lamps and every furnishing.
Exports the FBX used by Unity's BuildSlice.Hotel and saves the packed .blend.

    blender --background --factory-startup --python-exit-code 1 \
        --python tools/hotel/build_hotel.py -- \
        --layout assets/source/hotel/hotel-layout.json \
        --output assets/source/hotel --unity game/Assets/Art/Hotel \
        --previews output/demo/hotel/blender

Coordinates are Unity building-local metres (+X east, +Y up, +Z north). Primitives, the opening
column cut, the police furniture/leaf kit and the garage helpers are shared (same object-name
contract): Ext_*/Int_* shells, Glass, Leaf_<opening>_<n> (origin on the hinge),
LampHead_<lamp> (origin on the tilt joint), Furn_<id>, ExtFurn_<id>, Lamp_<id>, Bulb_<id>,
EastMarker/NorthMarker.
"""
import argparse, json, math, random, sys, zlib
from pathlib import Path

import bpy, bmesh
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'police'))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'garage'))
import build_police_station as K  # noqa: E402
import build_garage as GK  # noqa: E402  (garage helpers; importing it only registers its materials)
from build_police_station import part, box, bbox, tri, prism, cylinder, frustum, Frame, u  # noqa: E402
from build_garage import obox, beam, tube, poly_face, wall_point, wall_normal, along, norm  # noqa: E402

# name: (hex, metallic, roughness, metres per texture repeat or 0 for flat). Textured families carry a
# representative preview colour; Unity maps them to their textures (BuildSlice.Hotel.HotelMaterial).
HOTEL_MATERIALS = {
    'HExt_Rubble': ('9a9282', 0, .9, 1.6), 'HExt_Ashlar': ('a8a08e', 0, .85, 1.2), 'HExt_Plaster': ('ddd1b2', 0, .9, 3.0),
    'HExt_Timber': ('3e2e22', 0, .7, 1.0), 'HExt_Slate': ('46505a', 0, .6, 1.5), 'HExt_Ridge': ('3a4148', 0, .55, 1.5),
    'HExt_Iron': ('262a2b', .6, .5, 0), 'HExt_Gutter': ('30363a', .5, .5, 0), 'HExt_Shutter': ('3b3a2c', 0, .6, 1.0),
    'HExt_Chimney': ('7a6656', 0, .85, 1.6), 'HExt_Pot': ('8a4f35', 0, .7, 0),
    'HInt_PaperA': ('b09460', 0, .85, 1.2), 'HInt_PaperB': ('d6c8a8', 0, .85, 1.0), 'HInt_Lime': ('d8d0bb', 0, .92, 1.6),
    'HInt_Ceiling': ('e4dccb', 0, .95, 3.0), 'HInt_Beam': ('4a3626', 0, .7, 1.0), 'HInt_Panel': ('4e3a28', 0, .5, 1.0),
    'HInt_Oak': ('7a5e40', 0, .55, 2.0), 'HInt_Flags': ('8e8a80', 0, .8, 3.0), 'HInt_Tiles': ('cfc8b8', 0, .4, 1.0),
    'HInt_Rug': ('7e3026', 0, .95, 0), 'HInt_Runner': ('60342c', 0, .95, 0), 'HInt_Cloth': ('e8e2d2', 0, .9, 0),
    'HInt_Velvet': ('6e2a2a', 0, .85, 0), 'HInt_Quilt': ('7d5a46', 0, .95, 0), 'HInt_Linen': ('ece6d8', 0, .95, 0),
    'HInt_Porcelain': ('efece4', 0, .2, 0), 'HInt_Copper': ('a8653a', .85, .38, 0), 'HInt_Firebrick': ('4a2a22', 0, .9, 0),
    'HInt_Soot': ('1c1a18', 0, .95, 0), 'HInt_Leaf': ('4f6b3e', 0, .8, 0), 'HInt_Pot': ('8a4f35', 0, .8, 0),
    'HInt_Picture': ('5d6b62', 0, .6, 0), 'HInt_Gilt': ('9a7a3c', .8, .4, 0), 'HInt_Suitcase': ('5a3a26', 0, .6, 0),
    'HInt_Coal': ('2a2826', 0, .9, 0), 'HInt_Enamel': ('2f3d36', .2, .35, 0),
}
K.MATERIALS.update(HOTEL_MATERIALS)
MATS = K.MATERIALS
RNG = random.Random(19310120)
EXTERIOR_ROOMS = ('', 'street', 'yard')
LAMP_HEADS = []


def proud_box(w, side, a0, a1, y0, y1, depth, mat, p, faces=None, offset=0.0):
    """Box standing proud of a wall face (side +1/-1) by depth."""
    face = w['c'] + side * w['t'] / 2 + side * offset
    if w['axis'] == 'x':
        z0, z1 = sorted((face, face + side * depth))
        bbox(p, a0, y0, z0, a1, y1, z1, mat, faces=faces or ['+z' if side > 0 else '-z', '+y', '-y', '+x', '-x'])
    else:
        x0, x1 = sorted((face, face + side * depth))
        bbox(p, x0, y0, a0, x1, y1, a1, mat, faces=faces or ['+x' if side > 0 else '-x', '+y', '-y', '+z', '-z'])


def ext_side(w):
    return 1 if w['pos'] == 'ext' else -1


# ---------------------------------------------------------------------------
# Walls
# ---------------------------------------------------------------------------
def build_walls(L):
    for w in L['walls']:
        if w['kind'] == 'rail':
            build_rail(w)
            continue
        own = [o for o in L['openings'] if o['wall'] == w['id']]
        pieces = K.wall_pieces(w, own)
        normal_pos = '+z' if w['axis'] == 'x' else '+x'
        normal_neg = '-z' if w['axis'] == 'x' else '-x'
        ends_keys = ('+x', '-x') if w['axis'] == 'x' else ('+z', '-z')
        ext = 'ext' in (w['pos'], w['neg'])
        upper = w['level'] == 'upper'
        lowwall = w['kind'] == 'lowwall'
        mats = w['mats']
        rmat = ('HExt_Timber' if upper else 'HExt_Ashlar') if ext else mats['pos']
        for (a0, a1, y0, y1) in pieces:
            x0, yy0, z0, x1, yy1, z1 = K.wall_box(w, a0, a1, y0, y1)
            for key, finish, side in ((normal_pos, w['pos'], 'pos'), (normal_neg, w['neg'], 'neg')):
                if finish == 'none':
                    continue
                bbox(part('Ext_Facade' if finish == 'ext' else 'Int_Walls'), x0, yy0, z0, x1, yy1, z1, mats[side], faces=[key])
            rp = part('Ext_Facade') if ext else part('Int_Walls')
            inner0 = a0 > w['a0'] + 1e-6 and any(abs(q[1] - a0) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces)
            inner1 = a1 < w['a1'] - 1e-6 and any(abs(q[0] - a1) < 1e-6 and q[2] <= y0 + 1e-6 and q[3] >= y1 - 1e-6 for q in pieces)
            for key, at_wall_end, inner, end in ((ends_keys[1], abs(a0 - w['a0']) < 1e-6, inner0, 'a0'), (ends_keys[0], abs(a1 - w['a1']) < 1e-6, inner1, 'a1')):
                if inner or (at_wall_end and end in w['buried']):
                    continue
                m = mats.get('end', mats['pos'] if not ext else ('HExt_Plaster' if upper else 'HExt_Rubble')) if at_wall_end else rmat
                bbox(rp, x0, yy0, z0, x1, yy1, z1, m, faces=[key])
            hidden_top = abs(y1 - w['y1']) < 1e-6 and not lowwall
            bbox(rp, x0, yy0, z0, x1, yy1, z1, rmat, faces=([] if hidden_top else ['+y']) + (['-y'] if y0 > w['y0'] + 1e-6 else []))
        if lowwall:
            c0, c1 = w['c'] - w['t'] / 2 - .05, w['c'] + w['t'] / 2 + .05
            bbox(part('Ext_Yard'), w['a0'] + .02, w['y1'], c0, w['a1'] - .02, w['y1'] + .09, c1, 'HExt_Ashlar', faces=['+y', '-y', '+z', '-z', '+x', '-x'])


def build_rail(w):
    p = part('Int_Trim'); x_axis = w['axis'] == 'x'
    y0, y1 = w['y0'], w['y1']

    def rbox(a0, a1, b0, b1, mat='HInt_Panel', d=.05):
        if x_axis:
            bbox(p, a0, b0, w['c'] - d / 2, a1, b1, w['c'] + d / 2, mat)
        else:
            bbox(p, w['c'] - d / 2, b0, a0, w['c'] + d / 2, b1, a1, mat)
    rbox(w['a0'], w['a1'], y1 - .07, y1, d=.08)
    rbox(w['a0'], w['a1'], y0, y0 + .08, d=.07)
    rbox(w['a0'], w['a0'] + .1, y0, y1 + .12, d=.1)
    k = w['a0'] + .2
    while k < w['a1'] - .05:
        rbox(k - .018, k + .018, y0 + .08, y1 - .07, d=.036)
        k += .13


def build_half_timber(L):
    """Exposed oak framing on the upper storey's plastered outer faces: sole and wall plates,
    corner and window posts, studs, rails at sill and head height, and corner braces."""
    lv = L['levels']; U, E = lv['upper'], lv['eaves']
    p = part('Ext_Timber')
    D = .045
    for w in L['walls']:
        if w['level'] != 'upper' or w['kind'] != 'wall' or 'ext' not in (w['pos'], w['neg']):
            continue
        side = ext_side(w)
        own = sorted((o for o in L['openings'] if o['wall'] == w['id']), key=lambda o: o['at'])
        # Outer extent of the face: the axis-z walls stop inside the axis-x walls' thickness.
        a0, a1 = (w['a0'] - (.3 if w['axis'] == 'z' else 0), w['a1'] + (.3 if w['axis'] == 'z' else 0))
        proud_box(w, side, a0, a1, U - .02, U + .2, D, 'HExt_Timber', p)
        proud_box(w, side, a0, a1, E - .24, E, D, 'HExt_Timber', p)
        posts = {a0 + .1, a1 - .1}
        for o in own:
            posts.add(o['at'] - o['width'] / 2 - .09); posts.add(o['at'] + o['width'] / 2 + .09)
        ps = sorted(posts)
        studs = []
        for s0, s1 in zip(ps, ps[1:]):
            n = int((s1 - s0) / 1.15)
            studs += [s0 + (s1 - s0) * k / (n + 1) for k in range(1, n + 1)]
        win = lambda a: any(o['at'] - o['width'] / 2 - .02 < a < o['at'] + o['width'] / 2 + .02 for o in own)
        for a in ps + studs:
            if win(a):
                continue
            proud_box(w, side, a - .09, a + .09, U + .2, E - .24, D, 'HExt_Timber', p)
        rails = (own[0]['bottom'] - .1 if own else U + .8, own[0]['top'] if own else U + 2.3)
        for y in rails:
            cuts = sorted((o['at'] - o['width'] / 2, o['at'] + o['width'] / 2) for o in own if o['bottom'] - .12 < y < o['top'] + .05)
            s = a0 + .19
            for lo, hi in cuts + [(a1 - .19, a1)]:
                if lo - s > .05:
                    proud_box(w, side, s, lo, y, y + .14, D * .9, 'HExt_Timber', p)
                s = hi
        # Braces in the end panels, rising from the sole plate to the corner post.
        for corner, sgn in ((a0 + .19, 1), (a1 - .19, -1)):
            far = corner + sgn * 1.0
            if any(min(corner, far) - .1 < o['at'] + o['width'] / 2 and o['at'] - o['width'] / 2 < max(corner, far) + .1 for o in own):
                continue
            face = w['c'] + side * (w['t'] / 2 + D / 2)
            pa = (far, U + .2, face) if w['axis'] == 'x' else (face, U + .2, far)
            pb = (corner, rails[0], face) if w['axis'] == 'x' else (face, rails[0], corner)
            beam(p, pa, pb, .16, D, 'HExt_Timber', up=wall_normal(w, side))
        # Window sills: a projecting oak board.
        for o in own:
            if o['kind'] == 'window':
                proud_box(w, side, o['at'] - o['width'] / 2 - .12, o['at'] + o['width'] / 2 + .12, o['bottom'] - .08, o['bottom'], .1, 'HExt_Timber', p)


def build_ground_facade(L):
    """Stone storey details: quoins, dressed sills and lintels, shutters, the bressummer under the
    timber storey, and the stone service wing's quoins."""
    lv = L['levels']; fr = L['front']; sv = L['service']
    fac = part('Ext_Facade')
    for cx, cz, sx, sz, top in ((fr['x0'], fr['z0'], -1, -1, lv['upper']), (fr['x0'], fr['z1'], -1, 1, lv['upper']),
                                (fr['x1'], fr['z1'], 1, 1, lv['upper']), (sv['x1'], sv['z0'], 1, -1, lv['serviceEaves']),
                                (sv['x1'], sv['z1'], 1, 1, lv['serviceEaves']), (sv['x0'], sv['z0'], -1, -1, lv['serviceEaves'])):
        y, k = -.1, 0
        while y < top - .25:
            hh = RNG.uniform(.26, .34)
            lx, lz = (.46, .24) if k % 2 == 0 else (.24, .46)
            x0, x1 = sorted((cx + sx * .03, cx - sx * lx)); z0, z1 = sorted((cz + sz * .03, cz - sz * lz))
            bbox(fac, x0, y, z0, x1, min(y + hh, top - .3) - .006, z1, 'HExt_Ashlar', faces=['+x' if sx > 0 else '-x', '+z' if sz > 0 else '-z', '+y', '-y'])
            y += hh; k += 1
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        w = W[o['wall']]
        if not o['exterior'] or w['level'] == 'upper':
            continue
        side = ext_side(w)
        lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
        proud_box(w, side, lo - .14, hi + .14, o['top'], o['top'] + .26, .05, 'HExt_Ashlar', fac)
        if o['kind'] == 'window':
            proud_box(w, side, lo - .1, hi + .1, o['bottom'] - .1, o['bottom'] - .004, .08, 'HExt_Ashlar', fac)
            if o['wall'] == 'F_N' or o['wall'] == 'F_W':
                for s_ in (-1, 1):
                    a = lo - .58 if s_ < 0 else hi + .06
                    sh = part('Ext_Shutters')
                    proud_box(w, side, a, a + .52, o['bottom'] + .02, o['top'] - .02, .04, 'HExt_Shutter', sh)
                    for k in range(1, 6):
                        y = o['bottom'] + (o['top'] - o['bottom']) * k / 6
                        proud_box(w, side, a + .04, a + .48, y - .015, y + .015, .015, 'HExt_Timber', sh, offset=.04)
                    proud_box(w, side, a + (.44 if s_ < 0 else .02), a + (.5 if s_ < 0 else .08), o['bottom'] + .3, o['bottom'] + .36, .03, 'HExt_Iron', sh, offset=.04)
    # Bressummer: the heavy oak beam between the stone and the timber storey, round three sides.
    U = lv['upper']
    bp = part('Ext_Timber')
    bbox(bp, fr['x0'] - .06, U - .3, fr['z1'], fr['x1'] + .06, U + .02, fr['z1'] + .06, 'HExt_Timber', faces=['+z', '-y', '+y', '+x', '-x'])
    bbox(bp, fr['x0'] - .06, U - .3, fr['z0'] + .06, fr['x0'], U + .02, fr['z1'], 'HExt_Timber', faces=['-x', '-y', '+y', '-z'])
    bbox(bp, fr['x1'], U - .3, fr['z0'] + .06, fr['x1'] + .06, U + .02, fr['z1'], 'HExt_Timber', faces=['+x', '-y', '+y', '-z'])
    bbox(bp, fr['x0'] - .06, U - .3, fr['z0'] - .06, sv['x0'], U + .02, fr['z0'], 'HExt_Timber', faces=['-z', '-y', '+y', '-x'])
    bbox(bp, fr['x1'] - .5, U - .3, fr['z0'] - .06, fr['x1'] + .06, U + .02, fr['z0'], 'HExt_Timber', faces=['-z', '-y', '+y', '+x'])
    bbox(bp, sv['x0'], U - .3, fr['z0'] - .06, fr['x1'] - .5, U + .02, fr['z0'], 'HExt_Timber', faces=['-z', '-y', '+y'])


# ---------------------------------------------------------------------------
# Gable ends and street gables
# ---------------------------------------------------------------------------
def roof_y(L, z):
    lv = L['levels']
    return lv['ridge'] - lv['slope'] * abs(z - lv['ridgeZ'])


def sw_roof_y(L, z):
    lv = L['levels']
    return lv['serviceRidge'] - (lv['serviceRidge'] - lv['serviceEaves']) / 4.0 * abs(z - lv['serviceRidgeZ'])


def build_gable_ends(L):
    """Half-timbered gable triangles at the west and east ends of the front range, and the stone
    gables of the service wing."""
    lv = L['levels']; fr = L['front']; E = lv['eaves']; T = lv['roofThickness']; rz = lv['ridgeZ']
    lift = .02
    apex = lv['ridge'] - T + lift
    for x_out, x_in, sgn in ((fr['x0'], fr['x0'] + .3, -1), (fr['x1'], fr['x1'] - .3, 1)):
        za = fr['z0'] + (T - lift) / lv['slope'] * 0
        out = [(x_out, E, fr['z1']), (x_out, E, fr['z0']), (x_out, apex, rz)] if sgn < 0 else [(x_out, E, fr['z0']), (x_out, E, fr['z1']), (x_out, apex, rz)]
        poly_face(part('Ext_Facade'), out, (sgn, 0, 0), 'HExt_Plaster')
        inn = [(x_in, E, fr['z0']), (x_in, E, fr['z1']), (x_in, apex, rz)] if sgn < 0 else [(x_in, E, fr['z1']), (x_in, E, fr['z0']), (x_in, apex, rz)]
        poly_face(part('Int_Attic'), inn, (-sgn, 0, 0), 'HInt_Lime')
        # Timber: tie beam on the wall plate, king post, two struts and the collar.
        p = part('Ext_Timber'); D = .045; f = x_out + sgn * D / 2
        bbox(p, min(x_out, x_out + sgn * D), E - .02, fr['z0'] + .3, max(x_out, x_out + sgn * D), E + .18, fr['z1'] - .3, 'HExt_Timber')
        bbox(p, min(x_out, x_out + sgn * D), E + .18, rz - .09, max(x_out, x_out + sgn * D), apex - .25, rz + .09, 'HExt_Timber')
        for zz in (rz - 2.6, rz + 2.6):
            beam(p, (f, E + .18, zz), (f, E + 1.9 - (abs(zz - rz) - 2.6) * .0, rz + (zz - rz) * .25), .14, D, 'HExt_Timber', up=(sgn, 0, 0))
        yc = E + 2.3
        half = (apex - yc) / lv['slope']
        bbox(p, min(x_out, x_out + sgn * D), yc, rz - half + .2, max(x_out, x_out + sgn * D), yc + .16, rz + half - .2, 'HExt_Timber')
        # Barge boards along both verges.
        for zz in (fr['z0'] - .45, fr['z1'] + .45):
            beam(p, (x_out + sgn * .47, roof_y(L, zz) - .2, zz), (x_out + sgn * .47, lv['ridge'] - .2, rz), .05, .26, 'HExt_Timber', up=(sgn, 0, 0))
    sv = L['service']; se = lv['serviceEaves']; sr = lv['serviceRidge'] - T + lift; srz = lv['serviceRidgeZ']
    for x_out, x_in, sgn in ((sv['x0'], sv['x0'] + .45, -1), (sv['x1'], sv['x1'] - .45, 1)):
        z_top = fr['z0'] if sgn < 0 else sv['z1']
        if sgn < 0:
            # The west gable stops against the front range's south wall.
            out = [(x_out, se, z_top), (x_out, se, sv['z0']), (x_out, sr, srz), (x_out, sw_roof_y(L, z_top) - T + lift, z_top)]
            poly_face(part('Ext_Facade'), out[:3], (sgn, 0, 0), 'HExt_Rubble')
        else:
            out = [(x_out, se, sv['z0']), (x_out, se, z_top), (x_out, sr, srz)]
            poly_face(part('Ext_Facade'), out, (sgn, 0, 0), 'HExt_Rubble')
        inn = [(x_in, se, sv['z0']), (x_in, se, z_top), (x_in, sr, srz)]
        poly_face(part('Int_Attic'), inn if sgn < 0 else inn[::-1], (-sgn, 0, 0), 'HInt_Lime')
        p = part('Ext_Timber')
        for zz in (sv['z0'] - .35, (z_top + .35) if sgn > 0 else z_top):
            beam(p, (x_out + sgn * .37, sw_roof_y(L, zz) - .16, zz), (x_out + sgn * .37, lv['serviceRidge'] - .16, srz), .05, .22, 'HExt_Timber', up=(sgn, 0, 0))


def build_street_gables(L):
    lv = L['levels']; E = lv['eaves']; T = lv['roofThickness']; S = lv['slope']; fr = L['front']
    rp = part('Ext_Roof'); fac = part('Ext_Facade'); tp = part('Ext_Timber')
    for g in L['gables']:
        cx, h, apex, face, front = g['x'], g['half'], g['apex'], g['face'], g['front']
        zv = lv['ridgeZ'] + (lv['ridge'] - apex) / S
        sd = (apex - E) / h
        for sgn in (-1, 1):
            pts = [(cx + sgn * h, E, front), (cx, apex, front), (cx, apex, zv), (cx + sgn * h, E, face)]
            n = norm((sgn * sd, 1, 0))
            poly_face(rp, pts if sgn > 0 else pts[::-1], n, 'HExt_Slate')
            under = [(x, y - T, z) for x, y, z in pts]
            poly_face(rp, under[::-1] if sgn > 0 else under, (-n[0], -n[1], 0), 'HExt_Timber')
            rp.quad([(cx + sgn * h, E - T, front), (cx, apex - T, front), (cx, apex, front), (cx + sgn * h, E, front)][::(1 if sgn < 0 else -1)], (0, 0, 1), 'HExt_Timber')
            beam(tp, (cx + sgn * (h + .02), E - .12, front + .02), (cx, apex - .12, front + .02), .05, .26, 'HExt_Timber', up=(0, 0, 1))
        beam(rp, (cx, apex + .04, front + .03), (cx, apex + .04, zv - .1), .16, .08, 'HExt_Ridge')
        # Plastered gable triangle on the wall face, framed in oak, with a louvred vent.
        a = (cx - h, E, face); b = (cx + h, E, face); c = (cx, apex - T + .02, face)
        poly_face(fac, [b, a, c][::-1], (0, 0, 1), 'HExt_Plaster')
        poly_face(part('Int_Attic'), [(cx - h, E, face - .3), (cx + h, E, face - .3), (cx, apex - T + .02, face - .3)][::-1][::-1], (0, 0, -1), 'HInt_Lime')
        D = .045
        bbox(tp, cx - h, E - .02, face, cx + h, E + .18, face + D, 'HExt_Timber')
        bbox(tp, cx - .09, E + .18, face, cx + .09, apex - .45, face + D, 'HExt_Timber')
        for sgn in (-1, 1):
            beam(tp, (cx + sgn * (h - .25), E + .18, face + D / 2), (cx + sgn * .1, E + 1.6, face + D / 2), .14, D, 'HExt_Timber', up=(0, 0, 1))
        for k in range(5):
            y = E + 1.75 + k * .1
            bbox(tp, cx - .32, y, face + D, cx + .32, y + .05, face + D + .03, 'HExt_Timber')
        bbox(tp, cx - .36, E + 1.7, face + D, cx + .36, E + 1.72, face + D + .04, 'HExt_Timber')


# ---------------------------------------------------------------------------
# Roofs
# ---------------------------------------------------------------------------
def slope_poly(L, pts, side, under=True, kind='front'):
    """Convex polygon (x,z) on one slope of the front range (side -1 south, +1 north) or of the
    service wing (kind 'service'): slate top, boarded underside."""
    lv = L['levels']
    if kind == 'front':
        S, rz, yf = lv['slope'], lv['ridgeZ'], lambda z: roof_y(L, z)
    else:
        S, rz, yf = (lv['serviceRidge'] - lv['serviceEaves']) / 4.0, lv['serviceRidgeZ'], lambda z: sw_roof_y(L, z)
    n = norm((0, 1, side * S))
    top = [(x, yf(z), z) for x, z in pts]
    poly_face(part('Ext_Roof'), top, n, 'HExt_Slate')
    if under:
        poly_face(part('Int_Roof'), [(x, y - lv['roofThickness'], z) for x, y, z in top][::-1], (0, -n[1], -n[2]), 'HExt_Timber')


def build_roofs(L):
    lv = L['levels']; T = lv['roofThickness']
    r = next(r for r in L['roofs'] if r['id'] == 'roof-front')
    x0, x1, z0, z1, rz = r['x0'], r['x1'], r['z0'], r['z1'], lv['ridgeZ']
    face = L['front']['z1']
    slope_poly(L, [(x0, z0), (x1, z0), (x1, rz), (x0, rz)], -1)
    edges = x0
    S = lv['slope']
    for g in sorted(L['gables'], key=lambda g: g['x']):
        cx, h = g['x'], g['half']
        zv = rz + (lv['ridge'] - g['apex']) / S
        slope_poly(L, [(edges, rz), (cx - h, rz), (cx - h, z1), (edges, z1)], 1)
        slope_poly(L, [(cx - h, rz), (cx, rz), (cx, zv), (cx - h, face)], 1)
        slope_poly(L, [(cx, rz), (cx + h, rz), (cx + h, face), (cx, zv)], 1)
        edges = cx + h
    slope_poly(L, [(edges, rz), (x1, rz), (x1, z1), (edges, z1)], 1)
    rp = part('Ext_Roof')
    beam(rp, (x0 - .02, lv['ridge'] + .04, rz), (x1 + .02, lv['ridge'] + .04, rz), .2, .1, 'HExt_Ridge', up=(0, 1, 0))
    north_spans, a = [], x0
    for g in sorted(L['gables'], key=lambda g: g['x']):
        north_spans.append((a, g['x'] - g['half'])); a = g['x'] + g['half']
    north_spans.append((a, x1))
    for z, nz, spans in ((z0, -1, [(x0, x1)]), (z1, 1, north_spans)):
        y = roof_y(L, z)
        for sa, sb in spans:
            rp.quad([(sa, y - T - .06, z), (sb, y - T - .06, z), (sb, y + .02, z), (sa, y + .02, z)][::(1 if nz < 0 else -1)], (0, 0, nz), 'HExt_Timber')
            bbox(rp, sa, y - .26, min(z, z + nz * .15), sb, y - .12, max(z, z + nz * .15), 'HExt_Gutter')
    for g in L['gables']:
        # The street gables' own eaves gutters.
        for sgn in (-1, 1):
            xe = g['x'] + sgn * g['half']
            bbox(rp, min(xe, xe + sgn * .14), L['levels']['eaves'] - .26, g['face'], max(xe, xe + sgn * .14), L['levels']['eaves'] - .12, g['front'], 'HExt_Gutter')
    for x, z in ((x0 + .3, z0 - .07), (x1 - .3, z0 - .07), (x0 + .3, z1 + .07), (x1 - .3, z1 + .07)):
        cylinder(rp, x, z, .045, 0.0, roof_y(L, z0) - .2, 'HExt_Gutter', seg=8)
    for x, nx in ((x0, -1), (x1, 1)):
        for za, zb in ((z0, rz), (rz, z1)):
            rp.quad([(x, roof_y(L, za) - T, za), (x, roof_y(L, zb) - T, zb), (x, roof_y(L, zb) + .01, zb), (x, roof_y(L, za) + .01, za)][::(1 if nx > 0 else -1)], (nx, 0, 0), 'HExt_Timber')
    # Service wing roof (ridge along x); the north slope runs up to the front range for x < 23.
    r = next(r for r in L['roofs'] if r['id'] == 'roof-service')
    sx0, sx1, sz0, srz = r['x0'], r['x1'], r['z0'], lv['serviceRidgeZ']
    fz0 = L['front']['z0']; fx1 = L['front']['x1']
    slope_poly(L, [(sx0, sz0), (sx1, sz0), (sx1, srz), (sx0, srz)], -1, kind='service')
    slope_poly(L, [(sx0, srz), (fx1, srz), (fx1, fz0), (sx0, fz0)], 1, kind='service')
    slope_poly(L, [(fx1, srz), (sx1, srz), (sx1, fz0 + .35), (fx1, fz0 + .35)], 1, kind='service')
    yr = lv['serviceRidge']
    beam(rp, (sx0, yr + .04, srz), (sx1 + .02, yr + .04, srz), .18, .09, 'HExt_Ridge')
    y = sw_roof_y(L, sz0)
    rp.quad([(sx1, y - T - .06, sz0), (sx0, y - T - .06, sz0), (sx0, y + .02, sz0), (sx1, y + .02, sz0)], (0, 0, -1), 'HExt_Timber')
    bbox(rp, sx0, y - .24, sz0 - .14, sx1, y - .1, sz0, 'HExt_Gutter')
    y2 = sw_roof_y(L, fz0 + .35)
    rp.quad([(fx1, y2 - T - .06, fz0 + .35), (sx1, y2 - T - .06, fz0 + .35), (sx1, y2 + .02, fz0 + .35), (fx1, y2 + .02, fz0 + .35)], (0, 0, 1), 'HExt_Timber')
    bbox(rp, fx1, y2 - .24, fz0 + .35, sx1, y2 - .1, fz0 + .49, 'HExt_Gutter')
    for x in (sx0 + .3, sx1 - .3):
        cylinder(rp, x, sz0 - .07, .045, 0.0, y - .2, 'HExt_Gutter', seg=8)
    cylinder(rp, sx1 - .3, fz0 + .42, .045, 0.0, y2 - .2, 'HExt_Gutter', seg=8)
    # Flashing strip where the service roof meets the front range wall.
    bbox(rp, sx0, sw_roof_y(L, fz0) - .02, fz0 - .08, fx1, sw_roof_y(L, fz0) + .14, fz0, 'HExt_Gutter', faces=['+y', '-z', '+x', '-x'])
    build_porch(L); build_carport(L); build_chimneys(L)


def gable_roof_z(p, x0, x1, z0, z1, eaves, apex, front_board=True, back_board=True, mat='HExt_Slate', T=.1):
    """Small gable roof with its ridge along z at the middle of x0..x1."""
    xm = (x0 + x1) / 2; s = (apex - eaves) / (xm - x0)
    for sgn, xe in ((-1, x0), (1, x1)):
        n = norm((sgn * s, 1, 0))
        top = [(xe, eaves, z0), (xm, apex, z0), (xm, apex, z1), (xe, eaves, z1)]
        poly_face(p, top if sgn > 0 else top[::-1], n, mat)
        bot = [(x, y - T, z) for x, y, z in top]
        poly_face(p, bot[::-1] if sgn > 0 else bot, (-n[0], -n[1], 0), 'HExt_Timber')
        p.quad([(xe, eaves - T, z0), (xe, eaves - T, z1), (xe, eaves, z1), (xe, eaves, z0)][::(1 if sgn > 0 else -1)], (sgn, 0, 0), 'HExt_Timber')
    for z, nz, show in ((z0, -1, back_board), (z1, 1, front_board)):
        if not show:
            continue
        for sgn, xe in ((-1, x0), (1, x1)):
            beam(p, (xe, eaves - .06, z + nz * .02), (xm, apex - .06, z + nz * .02), .05, .2, 'HExt_Timber', up=(0, 0, nz))
    beam(p, (xm, apex + .03, z0), (xm, apex + .03, z1), .12, .06, 'HExt_Ridge')


def build_porch(L):
    pr = L['porch']; p = part('Ext_Porch')
    x0, x1, z0, z1, e, a = pr['x0'], pr['x1'], pr['z0'], pr['z1'], pr['eaves'], pr['apex']
    gable_roof_z(p, x0, x1, z0, z1, e, a)
    xm = (x0 + x1) / 2
    # Boarded gable front (the lettered sign board is added in Unity over it), plates, posts, brackets.
    poly_face(p, [(x0 + .15, e + .02, z1 - .05), (x1 - .15, e + .02, z1 - .05), (xm, a - .14, z1 - .05)], (0, 0, 1), 'HExt_Timber')
    poly_face(p, [(x1 - .15, e + .02, z1 - .07), (x0 + .15, e + .02, z1 - .07), (xm, a - .14, z1 - .07)], (0, 0, -1), 'HExt_Timber')
    for x in (x0 + .3, x1 - .3):
        bbox(p, x - .08, e - .22, z0, x + .08, e, z1 - .05, 'HExt_Timber')
    bbox(p, x0 + .2, e - .2, z1 - .25, x1 - .2, e + .02, z1 - .08, 'HExt_Timber')
    for post in pr['posts']:
        x, z = post
        bbox(p, x - .09, L['levels']['ground'], z - .09, x + .09, e - .22, z + .09, 'HExt_Timber')
        bbox(p, x - .14, -.15, z - .14, x + .14, L['levels']['ground'] + .08, z + .14, 'HExt_Ashlar')
        for dz in (-1,):
            beam(p, (x, e - .9, z), (x, e - .22, z + dz * .7), .08, .08, 'HExt_Timber', up=(1, 0, 0))
        sgn = 1 if x < xm else -1
        beam(p, (x, e - .8, z), (x + sgn * .6, e - .22, z), .08, .08, 'HExt_Timber', up=(0, 0, 1))


def build_carport(L):
    c = L['carport']; p = part('Ext_Carport')
    gable_roof_z(p, c['x0'], c['x1'], c['z0'], c['z1'], c['eaves'], c['ridge'], mat='HExt_Slate')
    e = c['eaves']
    for post in [q for q in L['posts'] if q['id'].startswith('carport')]:
        x, z, w = post['x'], post['z'], post['w']
        bbox(p, x - w / 2, post['y0'], z - w / 2, x + w / 2, post['y1'], z + w / 2, 'HExt_Timber')
        bbox(p, x - .17, post['y0'] - .1, z - .17, x + .17, .12, z + .17, 'HExt_Ashlar')
        for dz in (-1, 1):
            zz = z + dz * .55
            if c['z0'] < zz < c['z1']:
                beam(p, (x, post['y1'] - .7, z), (x, post['y1'] - .05, zz), .08, .08, 'HExt_Timber', up=(1, 0, 0))
    for x in (23.45, 28.55):
        bbox(p, x - .1, e - .26, c['z0'] + .05, x + .1, e - .05, c['z1'] - .05, 'HExt_Timber')
    for z in (25.0, 32.6):
        bbox(p, c['x0'] + .1, e - .24, z - .08, c['x1'] - .1, e - .08, z + .08, 'HExt_Timber')
        bbox(p, 25.95, e - .08, z - .06, 26.05, c['ridge'] - .12, z + .06, 'HExt_Timber')


def build_chimneys(L):
    p = part('Ext_Roof')
    for ch in L['chimneys']:
        x, z, w, d, top = ch['x'], ch['z'], ch['w'], ch['d'], ch['top']
        base = (roof_y(L, z) if ch['id'] != 'chimney-kitchen' else sw_roof_y(L, z)) - .6
        if ch['id'] == 'chimney-dining':
            base = L['levels']['ridge'] - .5
        bbox(p, x - w / 2, base, z - d / 2, x + w / 2, top, z + d / 2, 'HExt_Chimney', faces=['+x', '-x', '+z', '-z'])
        bbox(p, x - w / 2 - .07, top, z - d / 2 - .07, x + w / 2 + .07, top + .12, z + d / 2 + .07, 'HExt_Ashlar')
        for k in range(2 if d > .8 else 1):
            dz = (k - (.5 if d > .8 else 0)) * .35
            frustum(p, x, z + dz, .1, .08, top + .12, top + .45, 'HExt_Pot', seg=10, cap=True)


# ---------------------------------------------------------------------------
# Floors, ceilings, stair, posts
# ---------------------------------------------------------------------------
FLOOR_MAT = {'oak': 'HInt_Oak', 'flags': 'HInt_Flags', 'tiles': 'HInt_Tiles', 'stone': 'HExt_Ashlar'}


def build_slabs(L):
    lv = L['levels']; well = L['stairWell']
    lamp_x = [l['x'] for l in L['lamps'] if l['kind'] == 'pendant' and l['anchor'] and abs(l['anchor'] - lv['groundCeiling']) < .01]
    for s in L['slabs']:
        k = s['kind']
        if k == 'finish':
            bbox(part('Int_Floors'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], FLOOR_MAT[s['top']], faces=['+y'])
        elif k == 'structure':
            ip = part('Int_Ceilings')
            bbox(ip, s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'HInt_Ceiling', faces=['-y'])
            # The stair well's edges.
            if abs(s['x1'] - well['x0']) < .01:
                bbox(ip, s['x0'], s['y0'], well['z0'], s['x1'], s['y1'] + .02, well['z1'], 'HInt_Panel', faces=['+x'])
            if abs(s['x0'] - well['x1']) < .01:
                bbox(ip, s['x0'], s['y0'], well['z0'], s['x1'], s['y1'] + .02, well['z1'], 'HInt_Panel', faces=['-x'])
            if abs(s['z0'] - well['z1']) < .01:
                bbox(ip, s['x0'], s['y0'], s['z0'], s['x1'], s['y1'] + .02, s['z1'], 'HInt_Panel', faces=['-z'])
            # Exposed oak beams under the ceiling of the ground rooms (clear of lamp canopies).
            x = math.ceil((s['x0'] + .4) / 1.25) * 1.25
            while x < s['x1'] - .3:
                if not any(abs(x - lx) < .3 for lx in lamp_x) and not any(abs(x - px) < .2 for px in (6.48, 16.5)):
                    z0 = s['z0']
                    bbox(part('Int_Beams'), x - .09, s['y0'] - .18, z0, x + .09, s['y0'], s['z1'], 'HInt_Beam', faces=['-y', '+x', '-x'])
                x += 1.25
        elif k == 'ceiling':
            bbox(part('Int_Ceilings'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'HInt_Ceiling', faces=['-y'])
            if s['id'] == 'ceil-service':
                x = s['x0'] + 1.1
                while x < s['x1'] - .3:
                    if abs(x - 23.08) > .3 and abs(x - 19.9) > .35 and abs(x - 26.4) > .35:
                        bbox(part('Int_Beams'), x - .08, s['y0'] - .16, s['z0'], x + .08, s['y0'], s['z1'], 'HInt_Beam', faces=['-y', '+x', '-x'])
                    x += 1.3
        elif k == 'step':
            bbox(part('Ext_Facade'), s['x0'], s['y0'], s['z0'], s['x1'], s['y1'], s['z1'], 'HExt_Ashlar', faces=['+y', '+x', '-x', '+z', '-z'])


def build_thresholds(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        if o['kind'] != 'door':
            continue
        w = W[o['wall']]
        lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
        grow = .04 if o['exterior'] else .02
        x0, y0, z0, x1, y1, z1 = K.wall_box(w, lo + .001, hi - .001, o['bottom'] - .03, o['bottom'] + .006, grow)
        bbox(part('Ext_Facade' if o['exterior'] else 'Int_Trim'), x0, y0, z0, x1, y1, z1,
             'HExt_Ashlar' if o['exterior'] else 'HInt_Panel', faces=['+y', '+x', '-x', '+z', '-z'])


def build_stair(L):
    st = part('Int_Stairs'); g = L['levels']['ground']
    steps = L['stairs']
    for s in steps:
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'], s['z1'], 'HInt_Oak', faces=['+y'])
        bbox(st, s['x0'], g - .02, s['z0'], s['x1'], s['top'] - .02, s['z1'], 'HInt_Panel', faces=['+x', '+z'])
        bbox(st, s['x1'], s['top'] - .04, s['z0'], s['x1'] + .025, s['top'], s['z1'], 'HInt_Oak', faces=['+y', '+x', '-y', '+z'])
    # Open string, handrail and balusters on the north side; a turned newel at the foot.
    z = steps[0]['z1'] + .03
    first, last = steps[0], steps[-1]
    beam(st, (first['x1'], first['top'] - .12, z), (last['x0'], last['top'] - .12, z), .05, .24, 'HInt_Panel')
    beam(st, (first['x1'], first['top'] + .92, z), (last['x0'], last['top'] + .92, z), .07, .07, 'HInt_Panel')
    for s in steps:
        xc = (s['x0'] + s['x1']) / 2
        bbox(st, xc - .02, s['top'], z - .02, xc + .02, s['top'] + .9, z + .02, 'HInt_Panel')
    x = first['x1'] + .02
    bbox(st, x - .07, g, z - .07, x + .07, first['top'] + 1.1, z + .07, 'HInt_Panel')
    frustum(st, x, z, .07, .05, first['top'] + 1.1, first['top'] + 1.22, 'HInt_Panel', seg=10, cap=True)
    # Wall-side handrail on the south wall.
    zw = steps[0]['z0'] + .05
    beam(st, (first['x1'], first['top'] + .9, zw), (last['x0'], last['top'] + .9, zw), .05, .05, 'PInt_Brass')
    # Stair runner carpet down the middle of the treads.
    for s in steps:
        bbox(part('Int_Runner'), s['x0'], s['top'], s['z0'] + .22, s['x1'], s['top'] + .006, s['z1'] - .22, 'HInt_Runner', faces=['+y'])


def build_posts(L):
    for q in L['posts']:
        if q['id'].startswith('pier'):
            p = part('Ext_Yard')
            bbox(p, q['x'] - q['w'] / 2, q['y0'], q['z'] - q['w'] / 2, q['x'] + q['w'] / 2, q['y1'], q['z'] + q['w'] / 2, 'HExt_Rubble', faces=['+x', '-x', '+z', '-z'])
            bbox(p, q['x'] - q['w'] / 2 - .06, q['y1'], q['z'] - q['w'] / 2 - .06, q['x'] + q['w'] / 2 + .06, q['y1'] + .1, q['z'] + q['w'] / 2 + .06, 'HExt_Ashlar')


# ---------------------------------------------------------------------------
# Wall trims: skirting everywhere, a panelled dado in the public ground-floor rooms.
# ---------------------------------------------------------------------------
def room_at(L, x, z, level):
    for r in L['rooms']:
        if r['level'] == level and r['x0'] - .01 <= x <= r['x1'] + .01 and r['z0'] - .01 <= z <= r['z1'] + .01:
            return r
    return None


def build_trims(L):
    lv = L['levels']
    trim = part('Int_Trim')
    for w in L['walls']:
        if w['kind'] != 'wall':
            continue
        own = [o for o in L['openings'] if o['wall'] == w['id']]
        level = 1 if w['level'] == 'upper' else 0
        base = lv['upper'] if level else lv['ground']
        for sign, finish in ((1, w['pos']), (-1, w['neg'])):
            if finish != 'int':
                continue
            face = w['c'] + sign * w['t'] / 2
            cuts = sorted((o['at'] - o['width'] / 2, o['at'] + o['width'] / 2, o) for o in own if o['bottom'] < base + 1.0)
            spans, cursor = [], w['a0']
            for lo, hi, o in cuts:
                spans.append((cursor, lo, None))
                if o['kind'] == 'window':
                    spans.append((lo, hi, o))
                cursor = hi
            spans.append((cursor, w['a1'], None))
            for a0, a1, win in spans:
                mid = (a0 + a1) / 2
                px, pz = ((mid, face + sign * .15) if w['axis'] == 'x' else (face + sign * .15, mid))
                r = room_at(L, px, pz, level)
                if not r or a1 - a0 < .05:
                    continue
                a0c, a1c = max(a0, r['x0'] if w['axis'] == 'x' else r['z0']), min(a1, r['x1'] if w['axis'] == 'x' else r['z1'])
                if a1c - a0c < .05:
                    continue
                dado = level == 0 and r['id'] in ('lobby', 'dining', 'lodge', 'office')
                top = base + (.82 if dado else .14)
                if win is not None:
                    top = min(top, win['bottom'] - .03)
                    if top <= base + .05:
                        continue
                depth = .025 if not dado else .02
                if w['axis'] == 'x':
                    z0, z1 = sorted((face, face + sign * depth))
                    bbox(trim, a0c, base, z0, a1c, top, z1, 'HInt_Panel', faces=['+z' if sign > 0 else '-z', '+y', '+x', '-x'])
                    if dado:
                        z0, z1 = sorted((face + sign * depth, face + sign * (depth + .025)))
                        bbox(trim, a0c, top - .05, z0, a1c, top, z1, 'HInt_Panel', faces=['+z' if sign > 0 else '-z', '+y', '-y', '+x', '-x'])
                else:
                    x0, x1 = sorted((face, face + sign * depth))
                    bbox(trim, x0, base, a0c, x1, top, a1c, 'HInt_Panel', faces=['+x' if sign > 0 else '-x', '+y', '+z', '-z'])
                    if dado:
                        x0, x1 = sorted((face + sign * depth, face + sign * (depth + .025)))
                        bbox(trim, x0, top - .05, a0c, x1, top, a1c, 'HInt_Panel', faces=['+x' if sign > 0 else '-x', '+y', '-y', '+z', '-z'])


# ---------------------------------------------------------------------------
# Joinery
# ---------------------------------------------------------------------------
def build_joinery(L):
    W = {w['id']: w for w in L['walls']}
    for o in L['openings']:
        w = W[o['wall']]
        if o['kind'] == 'window':
            style = o['style']
            K.build_window(L, dict(o, style='sash' if style == 'sash' else 'high'), w)
            if style == 'casement':
                # Small-paned casement: a transom bar at two thirds and two glazing bars.
                side = ext_side(w); plane = w['c'] + side * (w['t'] / 2 - .1)
                y = o['bottom'] + (o['top'] - o['bottom']) * .68
                lo, hi = o['at'] - o['width'] / 2 + .06, o['at'] + o['width'] / 2 - .06
                for b0, b1, a0, a1 in ((y - .025, y + .025, lo, hi), ((o['bottom'] + y) / 2 - .01, (o['bottom'] + y) / 2 + .01, lo, hi)):
                    if w['axis'] == 'x':
                        bbox(part('Ext_Windows'), a0, b0, plane - .03, a1, b1, plane + .03, 'PExt_Paint')
                    else:
                        bbox(part('Ext_Windows'), plane - .03, b0, a0, plane + .03, b1, a1, 'PExt_Paint')
        elif o['kind'] == 'door':
            if o['exterior'] and o['style'] == 'plank':
                K.jamb_lining(o, w)
            else:
                K.casing(L, o, w); K.jamb_lining(o, w)
            for i in range(o['leaves']):
                if o['style'] == 'plank':
                    GK.build_plank_leaf(o, w, i, o['leaves'], L)
                else:
                    K.build_leaf(o, w, i, o['leaves'])
            if o['style'] == 'front':
                K.build_front_transom(o, w)


# ---------------------------------------------------------------------------
# Lamps
# ---------------------------------------------------------------------------
def build_lamps(L):
    for l in L['lamps']:
        fit = part('Lamp_' + l['id']); bulb = part('Bulb_' + l['id'])
        x, y, z = l['x'], l['y'], l['z']
        k = l['kind']
        if k == 'pendant':
            anchor = l['anchor']
            cylinder(fit, x, z, .055, anchor - .03, anchor, 'PInt_Brass', seg=10)
            tube(fit, (x, y + .16, z), (x, anchor - .03, z), .006, 'PInt_Black', seg=5, caps=False)
            cylinder(fit, x, z, .03, y + .12, y + .17, 'PInt_Brass', seg=8)
            frustum(fit, x, z, .2, .1, y - .02, y + .12, 'G_LampGlass', seg=14, cap=True)
            frustum(bulb, x, z, .05, .05, y - .06, y + .0, 'G_Bulb', seg=8, cap=True)
        elif k == 'bulb':
            tube(fit, (x, y + .1, z), (x, l['anchor'], z), .006, 'PInt_Black', seg=5, caps=False)
            cylinder(fit, x, z, .022, y + .05, y + .1, 'PInt_Brass', seg=8)
            frustum(bulb, x, z, .04, .03, y - .04, y + .05, 'G_Bulb', seg=8, cap=True)
        elif k == 'desk':
            base_y = y - .37
            cylinder(fit, x, z, .08, base_y, base_y + .025, 'PInt_Brass', seg=12)
            bbox(fit, x - .012, base_y + .02, z - .012, x + .012, y + .02, z + .012, 'PInt_Brass')
            frustum(fit, x, z, .13, .05, y - .03, y + .07, 'PInt_Green', seg=12, cap=True)
            frustum(bulb, x, z, .028, .028, y - .02, y + .03, 'G_Bulb', seg=8, cap=True)
        elif k == 'angle':
            # Brass base and upright on the desk; the shade swings down about the joint at the top
            # of the upright (LampHead_<id>, origin on the joint) to rake across the register.
            base_y = L['levels']['ground'] + .76
            cylinder(fit, x, z, .085, base_y, base_y + .03, 'PInt_Brass', seg=12)
            bbox(fit, x - .013, base_y + .03, z - .013, x + .013, y + .02, z + .013, 'PInt_Brass')
            joint = (x, y + .02, z)
            head = part('LampHead_' + l['id'], joint)
            tube(head, (x - .03, y + .02, z), (x + .03, y + .02, z), .02, 'PInt_Brass', seg=8)
            arm_end = (x + .2, y + .02, z + .16)
            tube(head, joint, arm_end, .011, 'PInt_Brass', seg=6)
            # Shade: green cone pointing down-forward from the arm end.
            cx, cy, cz = arm_end
            frustum(head, cx, cz, .12, .045, cy - .1, cy + .02, 'PInt_Green', seg=12, cap=True)
            frustum(bulb, cx, cz, .03, .03, cy - .08, cy - .02, 'G_Bulb', seg=8, cap=True)
            LAMP_HEADS.append(dict(object='LampHead_' + l['id'], bulb='Bulb_' + l['id'], lamp=l['id'], joint=list(joint),
                                   shade=[cx, cy - .06, cz]))
        elif k == 'hang':
            top = y + .55 if l['room'] == 'street' else y + .25
            tube(fit, (x, y + .2, z), (x, top, z), .008, 'PExt_Iron', seg=5, caps=False)
            bbox(fit, x - .12, y + .16, z - .12, x + .12, y + .2, z + .12, 'PExt_Iron')
            frustum(fit, x, z, .16, .03, y + .2, y + .32, 'PExt_Iron', seg=4, cap=True)
            bbox(fit, x - .08, y - .18, z - .08, x + .08, y - .15, z + .08, 'PExt_Iron')
            for cx_, cz_ in ((-.1, -.1), (.1, -.1), (.1, .1), (-.1, .1)):
                bbox(fit, x + cx_ - .01, y - .16, z + cz_ - .01, x + cx_ + .01, y + .17, z + cz_ + .01, 'PExt_Iron')
            bbox(bulb, x - .085, y - .15, z - .085, x + .085, y + .16, z + .085, 'G_LampGlass')
        elif k == 'post':
            bbox(fit, x - .1, y - .3, z - .1, x + .1, y - .26, z + .1, 'PExt_Iron')
            for cx_, cz_ in ((-.08, -.08), (.08, -.08), (.08, .08), (-.08, .08)):
                bbox(fit, x + cx_ - .01, y - .26, z + cz_ - .01, x + cx_ + .01, y + .16, z + cz_ + .01, 'PExt_Iron')
            frustum(fit, x, z, .15, .03, y + .16, y + .3, 'PExt_Iron', seg=4, cap=True)
            bbox(bulb, x - .075, y - .25, z - .075, x + .075, y + .15, z + .075, 'G_LampGlass')
        elif k == 'wall':
            wz = z + .22
            bbox(fit, x - .015, y + .24, min(wz, z) - .015, x + .015, y + .27, max(wz, z) + .015, 'PExt_Iron')
            bbox(fit, x - .11, y + .2, z - .11, x + .11, y + .24, z + .11, 'PExt_Iron')
            frustum(fit, x, z, .14, .02, y + .24, y + .36, 'PExt_Iron', seg=4, cap=True)
            bbox(fit, x - .07, y - .16, z - .07, x + .07, y - .13, z + .07, 'PExt_Iron')
            for cx_, cz_ in ((-.085, -.085), (.085, -.085), (.085, .085), (-.085, .085)):
                bbox(fit, x + cx_ - .01, y - .14, z + cz_ - .01, x + cx_ + .01, y + .2, z + cz_ + .01, 'PExt_Iron')
            bbox(bulb, x - .075, y - .13, z - .075, x + .075, y + .2, z + .075, 'G_LampGlass')


# ---------------------------------------------------------------------------
# Furniture
# ---------------------------------------------------------------------------
POLICE_KINDS = {'desk', 'chair', 'bench', 'coatstand', 'noticeboard', 'ledger', 'filingcabinet', 'bookshelf', 'stove',
                'wardrobe', 'washstand', 'umbrella', 'telephone', 'paper', 'table'}
GARAGE_KINDS = {'counter', 'pigeonhole', 'bell', 'crate', 'drum', 'barrel', 'tray'}
LEVELS = None


def fb(f, x0, y0, z0, x1, y1, z1, mat, faces='all'):
    f.bb(x0, y0, z0, x1, y1, z1, mat, faces)


def fcyl(f, a, b, r, mat, seg=10, caps=True):
    tube(f.p, f.pt(*a), f.pt(*b), r, mat, seg, caps)


def rename_exterior(F):
    if F['room'] in EXTERIOR_ROOMS:
        old, new = 'Furn_' + F['id'], 'ExtFurn_' + F['id']
        if old in K.PARTS:
            K.PARTS[new] = K.PARTS.pop(old); K.PARTS[new].name = new
            K.ORDER[K.ORDER.index(old)] = new


def build_furniture_piece(F, L):
    kind = F['kind']
    if kind in ('desklamp', 'anglelamp'):
        if kind == 'anglelamp':
            return
        return
    if kind == 'sideboard':
        F = dict(F, kind='lowcabinet'); kind = 'lowcabinet'
    if kind == 'kitchentable':
        F = dict(F, kind='table'); kind = 'table'
    if kind in POLICE_KINDS or kind == 'lowcabinet':
        K.build_furniture_piece(F, LEVELS); rename_exterior(F); return
    if kind in GARAGE_KINDS:
        GK.build_furniture_piece(F, L); return
    exterior = F['room'] in EXTERIOR_ROOMS
    name = ('ExtFurn_' if exterior else 'Furn_') + F['id']
    p = part(name, (F['x'], F['y'], F['z']))
    f = Frame(p, F['x'], F['y'], F['z'], F['yaw'])
    w, d, h = F['w'], F['d'], F['h']
    wood, light = 'PInt_Furniture', 'PInt_FurnLight'
    if kind == 'keyrack':
        fb(f, -w / 2, 0, -.04, w / 2, h, 0, 'HInt_Panel')
        fb(f, -w / 2 - .03, h, -.06, w / 2 + .03, h + .05, .02, 'HInt_Panel')
        for r in range(2):
            for c in range(3):
                x = -w / 2 + .25 + c * (w - .5) / 2; y = h - .3 - r * .42
                fb(f, x - .006, y, 0, x + .006, y + .012, .05, 'PInt_Brass')
                fb(f, x - .022, y - .16, .03, x + .022, y - .02, .036, 'PInt_Brass')
                fb(f, x - .004, y - .02, .03, x + .004, y + .01, .04, 'PInt_Brass')
                fb(f, x - .05, y + .1, 0, x + .05, y + .14, .006, 'PInt_Paper')
    elif kind == 'openledger':
        # A thick register lying open: two leaves of pages either side of the spine, with the
        # visible pages left to the Unity paper sheets (I01 / the next page).
        fb(f, -w / 2, 0, -d / 2, w / 2, .012, d / 2, 'PInt_Leather')
        for sx in (-1, 1):
            fb(f, sx * .012 if sx > 0 else -w / 2 + .01, .012, -d / 2 + .01, w / 2 - .01 if sx > 0 else -.012, .034, d / 2 - .01, 'PInt_Paper', faces=['+x', '-x', '+z', '-z'])
        fb(f, -.012, .012, -d / 2 + .01, .012, .028, d / 2 - .01, 'PInt_Leather')
        fb(f, -w / 2 - .01, 0, d / 2 - .03, w / 2 + .01, .01, d / 2 + .01, 'PInt_Leather')
    elif kind == 'armchair':
        sh = .44
        fb(f, -w / 2, .1, -d / 2, w / 2, sh, d / 2, 'HInt_Velvet')
        fb(f, -w / 2, sh, -d / 2, w / 2, h, -d / 2 + .16, 'HInt_Velvet')
        for sx in (-1, 1):
            fb(f, sx * w / 2 - (.12 if sx > 0 else 0), sh, -d / 2, sx * w / 2 + (0 if sx > 0 else .12), sh + .22, d / 2, 'HInt_Velvet')
        for sx in (-1, 1):
            for sz in (-1, 1):
                fb(f, sx * (w / 2 - .06) - .03, 0, sz * (d / 2 - .06) - .03, sx * (w / 2 - .06) + .03, .1, sz * (d / 2 - .06) + .03, wood)
        fb(f, -w / 2 + .12, sh, -d / 2 + .16, w / 2 - .12, sh + .08, d / 2 - .02, 'HInt_Velvet')
    elif kind == 'sidetable':
        fcyl(f, (0, h - .03, 0), (0, h, 0), w / 2, wood, seg=14)
        fcyl(f, (0, .02, 0), (0, h - .03, 0), .03, wood, seg=8)
        for k in range(3):
            a = k * 2.094
            fb(f, math.cos(a) * .14 - .02, 0, math.sin(a) * .14 - .02, math.cos(a) * .14 + .02, .04, math.sin(a) * .14 + .02, wood)
        fb(f, -.12, h, -.08, .1, h + .03, .1, 'PInt_BookRed')
    elif kind == 'luggage':
        y = 0
        for k, (lw, ld, lh, m) in enumerate(((w, d, .32, 'HInt_Suitcase'), (w * .78, d * .82, .26, 'PInt_Leather'), (w * .5, d * .6, .18, 'HInt_Suitcase'))):
            if y + lh > h + .01:
                break
            off = (.03 * k, -.02 * k)
            fb(f, -lw / 2 + off[0], y, -ld / 2 + off[1], lw / 2 + off[0], y + lh, ld / 2 + off[1], m)
            fb(f, -lw / 2 + off[0] - .005, y + .04, -ld / 2 + off[1] - .005, lw / 2 + off[0] + .005, y + .08, ld / 2 + off[1] + .005, 'PInt_Brass', faces=['+x', '-x', '+z', '-z'])
            fb(f, -.08 + off[0], y + lh, -.015 + off[1], .08 + off[0], y + lh + .04, .015 + off[1], 'PInt_Black')
            y += lh
    elif kind == 'rug':
        mat = 'HInt_Rug' if F['extra'] == 'persian' else 'HInt_Runner'
        y = .004
        pts = [f.pt(-w / 2, y, -d / 2), f.pt(w / 2, y, -d / 2), f.pt(w / 2, y, d / 2), f.pt(-w / 2, y, d / 2)]
        if mat == 'HInt_Rug':
            uvs = [(0, 0), (1, 0), (1, 1), (0, 1)]
        else:
            rep = max(1.0, (d if d > w else w) / 1.8)
            uvs = [(0, 0), (1, 0), (1, rep), (0, rep)] if d >= w else [(0, 1), (0, 0), (rep, 0), (rep, 1)]
        p.faces.append((pts, (0, 1, 0), mat, uvs))
    elif kind == 'plant':
        frustum(p, F['x'], F['z'], .17, .21, F['y'], F['y'] + .32, 'HInt_Pot', seg=12, cap=False)
        cylinder(p, F['x'], F['z'], .19, F['y'] + .27, F['y'] + .29, 'HInt_Coal', seg=12)
        rng = random.Random(zlib.crc32(F['id'].encode()) & 0xffff)  # str hash() changes every run; crc32 does not
        for k in range(9):
            a = k * 2 * math.pi / 9 + rng.uniform(-.2, .2)
            ln = rng.uniform(.35, h - .3)
            tip = (F['x'] + math.cos(a) * rng.uniform(.25, .4), F['y'] + .3 + ln, F['z'] + math.sin(a) * rng.uniform(.25, .4))
            beam(p, (F['x'], F['y'] + .3, F['z']), tip, .09, .015, 'HInt_Leaf', up=(math.cos(a), 0, math.sin(a)))
    elif kind == 'picture':
        fb(f, -w / 2, 0, -.015, w / 2, h, .015, 'HInt_Gilt')
        fb(f, -w / 2 + .05, .05, .015, w / 2 - .05, h - .05, .02, 'HInt_Picture', faces=['+z'])
    elif kind == 'handlamp':
        fcyl(f, (0, 0, 0), (0, .05, 0), .06, 'PInt_Brass', seg=10)
        frustum(p, F['x'], F['z'], .035, .05, F['y'] + .05, F['y'] + .22, 'G_Glass', seg=10, cap=True)
        fcyl(f, (0, .22, 0), (0, .26, 0), .02, 'PInt_Black', seg=8)
    elif kind == 'safe':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_Enamel')
        fb(f, -w / 2 + .05, .05, d / 2, w / 2 - .05, h - .05, d / 2 + .01, 'HInt_Enamel', faces=['+z', '+x', '-x', '+y', '-y'])
        fcyl(f, (0, h * .6, d / 2 + .01), (0, h * .6, d / 2 + .04), .06, 'PInt_Brass', seg=14)
        fb(f, .15, h * .45, d / 2 + .01, .19, h * .55, d / 2 + .05, 'PInt_Brass')
        fb(f, -w / 2 + .1, h - .14, d / 2 + .01, w / 2 - .1, h - .1, d / 2 + .015, 'HInt_Gilt')
    elif kind == 'fireplace':
        stone = 'HExt_Ashlar'
        fb(f, -w / 2, 0, -d / 2, -w / 2 + .35, h, d / 2, stone)
        fb(f, w / 2 - .35, 0, -d / 2, w / 2, h, d / 2, stone)
        fb(f, -w / 2, h - .3, -d / 2, w / 2, h, d / 2, stone)
        fb(f, -w / 2 - .08, h, -d / 2, w / 2 + .08, h + .08, d / 2 + .08, 'HInt_Beam')
        fb(f, -w / 2 + .35, 0, -d / 2, w / 2 - .35, h - .3, -d / 2 + .05, 'HInt_Firebrick', faces=['+z'])
        fb(f, -w / 2 + .35, 0, -d / 2, -w / 2 + .36, h - .3, d / 2, 'HInt_Soot', faces=['+x'])
        fb(f, w / 2 - .36, 0, -d / 2, w / 2 - .35, h - .3, d / 2, 'HInt_Soot', faces=['-x'])
        fb(f, -w / 2 + .35, h - .31, -d / 2, w / 2 - .35, h - .3, d / 2, 'HInt_Soot', faces=['-y'])
        fb(f, -w / 2 - .1, -.02, d / 2, w / 2 + .1, .03, d / 2 + .4, stone)
        # Grate with a few cold logs; brass candlesticks and a mirror above.
        fb(f, -.3, .05, -.05, .3, .09, .2, 'PInt_Iron')
        for k in range(3):
            fcyl(f, (-.28, .14 + (k == 2) * .09, -.0 + k * .07), (.28, .14 + (k == 2) * .09, .03 + k * .06), .05, 'PInt_FurnLight', seg=8)
        for sx in (-1, 1):
            fcyl(f, (sx * .55, h + .08, .1), (sx * .55, h + .36, .1), .02, 'PInt_Brass', seg=8)
        fb(f, -.45, h + .3, -d / 2, .45, h + .95, -d / 2 + .04, 'HInt_Gilt')
        fb(f, -.4, h + .35, -d / 2 + .04, .4, h + .9, -d / 2 + .045, 'P_Glass', faces=['+z'])
    elif kind == 'roundtable':
        r = w / 2
        fcyl(f, (0, h - .03, 0), (0, h, 0), r, light, seg=18)
        frustum(p, F['x'], F['z'], r + .03, r + .01, F['y'] + h - .2, F['y'] + h + .005, 'HInt_Cloth', seg=18)
        cylinder(p, F['x'], F['z'], r + .01, F['y'] + h, F['y'] + h + .006, 'HInt_Cloth', seg=18)
        fcyl(f, (0, .05, 0), (0, h - .03, 0), .05, wood, seg=8)
        for k in range(4):
            a = k * math.pi / 2 + .785
            fb(f, math.cos(a) * .2 - .03, 0, math.sin(a) * .2 - .03, math.cos(a) * .2 + .03, .05, math.sin(a) * .2 + .03, wood)
        # Laid places: two plates and a small vase.
        for sz in (-1, 1):
            fcyl(f, (0, h + .006, sz * .24), (0, h + .02, sz * .24), .11, 'HInt_Porcelain', seg=14)
        fcyl(f, (0, h, 0), (0, h + .14, 0), .03, 'HInt_Porcelain', seg=10)
    elif kind == 'cashbox':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2, 'HInt_Enamel')
        fb(f, -.03, h * .5, d / 2, .03, h * .7, d / 2 + .01, 'PInt_Brass')
    elif kind == 'range':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2, 'PInt_Iron')
        fb(f, -w / 2 - .02, h, -d / 2 - .02, w / 2 + .02, h + .03, d / 2 + .02, 'PInt_Black')
        for k in range(3):
            fcyl(f, (-.4 + k * .4, h + .03, 0), (-.4 + k * .4, h + .04, 0), .11, 'PInt_Black', seg=12)
        for sx in (-.35, .3):
            fb(f, sx - .22, .15, d / 2, sx + .22, .6, d / 2 + .02, 'PInt_Black')
            fb(f, sx - .1, .5, d / 2 + .02, sx + .1, .52, d / 2 + .05, 'PInt_Brass')
        fb(f, -w / 2, h + .5, -d / 2, w / 2, h + .55, -d / 2 + .25, 'PInt_Iron')
        cx, _, cz = f.pt(0, 0, -d / 2 + .15)
        cylinder(p, cx, cz, .08, F['y'] + h + .55, L['levels']['serviceCeiling'], 'PInt_Iron', seg=10, top=False)
        for k in range(3):
            fcyl(f, (-.4 + k * .4, h + .04, 0), (-.4 + k * .4, h + .2, 0), .09 + .02 * (k % 2), 'HInt_Copper', seg=12)
    elif kind == 'dresser':
        fb(f, -w / 2, 0, -d / 2, w / 2, .9, d / 2, wood)
        fb(f, -w / 2 - .02, .9, -d / 2 - .02, w / 2 + .02, .93, d / 2 + .02, light)
        fb(f, -w / 2, .93, -d / 2, w / 2, h, -d / 2 + .25, wood, faces=['+z', '+x', '-x', '+y'])
        for k in range(3):
            y = 1.1 + k * .3
            fb(f, -w / 2, y, -d / 2, w / 2, y + .02, -d / 2 + .25, light)
            for i in range(6):
                x = -w / 2 + .14 + i * (w - .28) / 5
                tube(p, f.pt(x, y + .13, -d / 2 + .16), f.pt(x, y + .13, -d / 2 + .175), .11, 'HInt_Porcelain', seg=12)
        for i in range(3):
            x0 = -w / 2 + .04 + i * (w - .08) / 3
            fb(f, x0 + .02, .1, d / 2, x0 + (w - .08) / 3 - .02, .8, d / 2 + .015, light, faces=['+z', '+x', '-x', '+y', '-y'])
    elif kind == 'sink':
        fb(f, -w / 2, 0, -d / 2, w / 2, h - .2, d / 2, wood)
        fb(f, -w / 2, h - .2, -d / 2, w / 2, h, -d / 2 + .06, 'HInt_Porcelain')
        fb(f, -w / 2, h - .2, d / 2 - .06, w / 2, h, d / 2, 'HInt_Porcelain')
        fb(f, -w / 2, h - .2, -d / 2 + .06, -w / 2 + .06, h, d / 2 - .06, 'HInt_Porcelain')
        fb(f, w / 2 - .06, h - .2, -d / 2 + .06, w / 2, h, d / 2 - .06, 'HInt_Porcelain')
        fb(f, -w / 2 + .06, h - .2, -d / 2 + .06, w / 2 - .06, h - .18, d / 2 - .06, 'HInt_Porcelain', faces=['+y'])
        fcyl(f, (0, h, -d / 2 + .03), (0, h + .25, -d / 2 + .03), .015, 'PInt_Brass', seg=6)
        fcyl(f, (0, h + .25, -d / 2 + .03), (0, h + .25, -d / 2 + .18), .015, 'PInt_Brass', seg=6)
    elif kind == 'spike':
        fb(f, -.05, 0, -.05, .05, .02, .05, 'PInt_Black')
        fcyl(f, (0, .02, 0), (0, .2, 0), .004, 'PInt_Iron', seg=5)
        for k in range(6):
            fb(f, -.05, .03 + k * .012, -.06, .05, .034 + k * .012, .06, 'PInt_Paper')
    elif kind == 'washtub':
        frustum(p, F['x'], F['z'], w / 2 - .06, w / 2, F['y'] + .25, F['y'] + h, 'PInt_FurnLight', seg=16)
        for k in range(4):
            a = k * math.pi / 2
            fb(f, math.cos(a) * .3 - .03, 0, math.sin(a) * .3 - .03, math.cos(a) * .3 + .03, .27, math.sin(a) * .3 + .03, wood)
        cylinder(p, F['x'], F['z'], w / 2 - .08, F['y'] + .3, F['y'] + h - .12, 'GInt_Water', seg=16)
        for y in (.32, h - .06):
            fcyl(f, (0, y, 0), (0, y + .03, 0), w / 2 + .005, 'PInt_Iron', seg=16, caps=False)
    elif kind == 'mangle':
        for sx in (-1, 1):
            fb(f, sx * (w / 2 - .04) - .04, 0, -d / 2, sx * (w / 2 - .04) + .04, h, d / 2, 'PInt_Iron')
        for y in (.8, .95):
            fcyl(f, (-w / 2 + .08, y, 0), (w / 2 - .08, y, 0), .07, 'PInt_FurnLight', seg=12)
        fcyl(f, (w / 2, .95, 0), (w / 2 + .05, .95, 0), .2, 'PInt_Iron', seg=12)
        fb(f, -w / 2 + .05, .5, -d / 2, w / 2 - .05, .54, d / 2, wood)
    elif kind == 'bed':
        top = .5
        fb(f, -w / 2, 0, -d / 2, w / 2, top - .14, d / 2, wood)
        fb(f, -w / 2 - .03, 0, -d / 2 - .06, w / 2 + .03, h, -d / 2, 'HInt_Panel')
        fb(f, -w / 2 - .03, 0, d / 2, w / 2 + .03, .7, d / 2 + .05, 'HInt_Panel')
        fb(f, -w / 2 + .02, top - .14, -d / 2 + .02, w / 2 - .02, top, d / 2 - .02, 'HInt_Linen')
        fb(f, -w / 2, top - .2, -d / 2 + .55, w / 2, top + .04, d / 2 + .02, 'HInt_Quilt')
        for k in range(1 if w < 1.2 else 2):
            px = 0 if w < 1.2 else (-.32 + .64 * k)
            fb(f, px - .27, top, -d / 2 + .08, px + .27, top + .14, -d / 2 + .45, 'HInt_Linen')
        for sx in (-1, 1):
            fb(f, sx * (w / 2 + .03) - .04, h - .05, -d / 2 - .07, sx * (w / 2 + .03) + .04, h + .06, -d / 2 + .01, 'HInt_Panel')
    elif kind == 'bathtub':
        fb(f, -w / 2, .12, -d / 2, w / 2, h, -d / 2 + .06, 'HInt_Porcelain')
        fb(f, -w / 2, .12, d / 2 - .06, w / 2, h, d / 2, 'HInt_Porcelain')
        fb(f, -w / 2, .12, -d / 2 + .06, -w / 2 + .06, h, d / 2 - .06, 'HInt_Porcelain')
        fb(f, w / 2 - .06, .12, -d / 2 + .06, w / 2, h, d / 2 - .06, 'HInt_Porcelain')
        fb(f, -w / 2 + .06, .12, -d / 2 + .06, w / 2 - .06, .2, d / 2 - .06, 'HInt_Porcelain', faces=['+y'])
        for sx in (-1, 1):
            for sz in (-1, 1):
                fcyl(f, (sx * (w / 2 - .12), 0, sz * (d / 2 - .1)), (sx * (w / 2 - .12), .12, sz * (d / 2 - .1)), .035, 'PInt_Brass', seg=8)
        fcyl(f, (w / 2 - .1, h, 0), (w / 2 - .1, h + .2, 0), .015, 'PInt_Brass', seg=6)
    elif kind == 'wc':
        frustum(p, *f.pt(0, 0, .05)[0::2], .14, .19, F['y'], F['y'] + .4, 'HInt_Porcelain', seg=14)
        fb(f, -.2, .4, -.15, .2, .43, .27, 'PInt_FurnLight')
        fb(f, -.22, .55, -d / 2, .22, .85, -d / 2 + .18, 'HInt_Porcelain')
        fcyl(f, (.18, .78, -d / 2 + .1), (.26, .78, -d / 2 + .1), .01, 'PInt_Brass', seg=5)
    elif kind == 'basin':
        fcyl(f, (0, 0, 0), (0, h - .12, 0), .08, 'HInt_Porcelain', seg=12)
        fb(f, -w / 2, h - .12, -d / 2, w / 2, h, d / 2, 'HInt_Porcelain')
        fb(f, -w / 2 + .03, h - .2, -d / 2, w / 2 - .03, h + .5, -d / 2 + .03, 'HInt_Tiles', faces=['+z'])
        fcyl(f, (0, h, -d / 2 + .06), (0, h + .15, -d / 2 + .06), .012, 'PInt_Brass', seg=6)
        fb(f, -.18, h + .55, -d / 2, .18, h + 1.0, -d / 2 + .02, 'HInt_Gilt')
        fb(f, -.15, h + .58, -d / 2 + .02, .15, h + .97, -d / 2 + .025, 'P_Glass', faces=['+z'])
    elif kind == 'planterbox':
        fb(f, -w / 2, 0, -d / 2, w / 2, h, d / 2, 'HExt_Ashlar')
        fb(f, -w / 2 + .05, h - .05, -d / 2 + .05, w / 2 - .05, h - .04, d / 2 - .05, 'HInt_Coal', faces=['+y'])
        for k in range(7):
            a = k * .9
            beam(p, f.pt(0, h - .05, 0), f.pt(math.cos(a) * .22, h + .35 + (k % 3) * .08, math.sin(a) * .22), .08, .015, 'HInt_Leaf')
    elif kind == 'luggagecart':
        for sx in (-1, 1):
            GK.ftyre(f, (sx * .42, .22, -.4), (1, 0, 0), .22, .17, .05, spokes=8, spoke_mat='PInt_FurnLight', tread='PInt_Iron')
        fb(f, -.38, .3, -.6, .38, .35, .6, light)
        fb(f, -.38, .35, .55, .38, 1.0, .6, light)
        GK.fbeam(f, (-.3, .35, .6), (-.3, .95, .85), .04, .04, 'PInt_Iron')
        GK.fbeam(f, (.3, .35, .6), (.3, .95, .85), .04, .04, 'PInt_Iron')
        fb(f, -.32, .35, -.5, .3, .62, .1, 'HInt_Suitcase')
        fb(f, -.25, .62, -.4, .22, .82, .0, 'PInt_Leather')
        fb(f, -.4, .0, -.6, -.34, .3, -.54, 'PInt_Iron'); fb(f, .34, .0, -.6, .4, .3, -.54, 'PInt_Iron')
    else:
        raise ValueError('Unknown furniture kind ' + kind)


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
        m.diffuse_color = (*lin, .35 if name in ('G_Glass', 'P_Glass') else 1)
        mats[name] = m
    return mats


def build_objects(collection, mats):
    root = bpy.data.objects.new('HotelVisual', None)
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
    scene.world = bpy.data.worlds.new('HotelPreviewWorld')
    cam_data = bpy.data.cameras.new('PreviewCam'); cam = bpy.data.objects.new('PreviewCam', cam_data)
    scene.collection.objects.link(cam); scene.camera = cam
    over = [o for o in collection.objects if o.name.startswith(('Ext_Roof', 'Int_Roof', 'Int_Ceilings', 'Int_Beams', 'Ext_Timber', 'Int_Attic', 'Ext_Porch', 'Ext_Carport', 'Lamp_', 'Bulb_'))]
    upper = [o for o in collection.objects if o.name.startswith(('Ext_Roof', 'Int_Roof', 'Int_Attic', 'Ext_Carport'))]
    shots = {
        'street-front': ((4.0, 3.0, 52.0), (14.0, 5.0, 28.0), 52, []),
        'street-east': ((40.0, 3.0, 44.0), (18.0, 5.0, 24.0), 55, []),
        'rear-yard': ((30.0, 2.5, 2.0), (18.0, 4.0, 18.0), 60, []),
        'plan-ground': ((12.0, 44.0, 26.0), (12.0, 0.0, 26.5), 48, over + [o for o in collection.objects if o.name.startswith(('Int_Floors',)) and False]),
        'plan-upper': ((12.0, 46.0, 26.5), (12.0, 3.0, 27.0), 46, upper + [o for o in collection.objects if o.name.startswith(('Int_Ceilings', 'Lamp_', 'Bulb_'))]),
        'lobby': ((15.6, 1.66, 31.6), (8.0, 1.2, 24.0), 72, []),
        'dining': ((17.2, 1.66, 22.3), (21.0, 1.0, 31.0), 72, []),
        'room6': ((21.8, U_EYE, 25.0), (19.0, 4.2, 21.6), 72, []),
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


U_EYE = 3.55 + 1.62


def main():
    global LEVELS
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument('--layout', default='assets/source/hotel/hotel-layout.json')
    ap.add_argument('--output', default='assets/source/hotel')
    ap.add_argument('--unity', default='game/Assets/Art/Hotel')
    ap.add_argument('--previews', default='')
    args = ap.parse_args(argv)
    L = json.loads(Path(args.layout).read_text(encoding='utf-8'))
    out = Path(args.output).resolve(); out.mkdir(parents=True, exist_ok=True)
    lv = L['levels']
    # The police furniture kit reads its ceiling heights (stove flue) from these names.
    LEVELS = dict(L, levels=dict(lv, wingCeiling=lv['groundCeiling']))
    GK.POLICE_LEVELS = LEVELS

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'

    build_walls(L); build_half_timber(L); build_ground_facade(L); build_gable_ends(L); build_street_gables(L)
    build_roofs(L); build_slabs(L); build_thresholds(L); build_stair(L); build_posts(L); build_trims(L)
    build_joinery(L); build_lamps(L)
    for F in L['furniture']:
        build_furniture_piece(F, L)

    collection = bpy.data.collections.new('Hotel')
    bpy.context.scene.collection.children.link(collection)
    mats = make_materials()
    root, stats = build_objects(collection, mats)

    manifest = dict(version=1, layoutVersion=L['version'], objects=stats, triangles=sum(stats.values()),
                    leaves=K.LEAVES, lampHeads=LAMP_HEADS,
                    materials=[dict(name=k, hex=v[0], metallic=v[1], roughness=v[2], tile=v[3]) for k, v in MATS.items() if k in mats],
                    blender=bpy.app.version_string)
    (out / 'hotel-build-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out / 'Hotel.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
    if args.unity:
        dest = Path(args.unity).resolve(); dest.mkdir(parents=True, exist_ok=True)
        (dest / 'Hotel.fbx').write_bytes((out / 'Hotel.fbx').read_bytes())
        (dest / 'HotelBuildManifest.json').write_bytes((out / 'hotel-build-manifest.json').read_bytes())
    if args.previews:
        prev = Path(args.previews).resolve(); prev.mkdir(parents=True, exist_ok=True)
        render_previews(prev, collection)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'Hotel.blend'), compress=True)
    print('HOTEL_BLENDER_PASS', json.dumps(dict(objects=len(stats), triangles=manifest['triangles'], leaves=len(K.LEAVES), lampHeads=len(LAMP_HEADS))))


if __name__ == '__main__':
    main()
