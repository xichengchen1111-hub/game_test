"""Shared layout contract for the A gabled old inn (B08, 山墙老客栈).

One source for the Blender shell/furniture generator (tools/hotel/build_hotel.py) and the
Unity assembler (BuildSlice.Hotel). Coordinates are Unity building-local metres: +X east,
+Y up, +Z north; the prefab root sits at world (144, 0, -27) with no rotation, so
plan-data world X = 144 + x and Z = -27 + z. The plot is local x 0..41, z 0..40 and the
street (Station Road) lies north of z 40.

User-selected concept: docs/concepts/hotel/round-01/a-gabled-timber-inn.png (2026-10-16).
A two-storey front range with a roughstone ground storey and a half-timbered upper storey
under a steep slate roof with two street gables, a timber entrance porch, a single-storey
stone service wing (kitchen, store) behind, and an open timber carport for the hotel car,
which keeps its confirmed stand (world 169.5, 2.0, facing north). Ground floor: porter's
lodge, landlady's office, registration hall with counter, key rack, the side-lit register
desk and the stair; dining room with fireplace and the dining cash desk. Upper floor: six
guest rooms, a bathroom, a corridor and the stair landing. Characters are placeholders only
(user, 2026-10-16: a teammate makes the people).

Run from the repository root (stdlib only):
    python -X utf8 tools/hotel/hotel_layout.py
Writes assets/source/hotel/hotel-layout.json and its Unity copy
game/Assets/Resources/HotelLayout.json, after validating the geometry and checking that
docs/maps/town-current/plan-data.json B08 matches.
"""
import json, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ORIGIN = (144.0, 0.0, -27.0)

# Levels (metres above the town datum; the town yard surface is at -0.025).
G = 0.15             # ground floor finish
FOOT = -0.25
GC = 3.25            # ground-floor ceiling (underside of the upper floor)
U = 3.55             # upper floor finish
UC = 6.2             # upper ceiling
EAVES = 6.45         # front range wall head
SLOPE = .75
RIDGE_Z = 27.0
RIDGE = EAVES + SLOPE * 6.0     # 10.95
ROOF_T = .16
YARD = -0.025
FR = (2.0, 23.0, 21.0, 33.0)    # front range outer x0, x1, z0, z1
SW = (16.0, 30.0, 13.0, 21.0)   # service wing outer
SW_EAVES, SW_RIDGE, SW_RIDGE_Z, SW_CEIL = 3.3, 5.5, 17.0, 3.0
ST, UT, IT = .5, .3, .16
CAR = dict(x=25.5, z=29.0, yaw=0, halfW=.9, back=2.35, front=2.0)   # world (169.5, 2.0)

walls, openings, rooms, slabs, roofs = [], [], [], [], []
furniture, lamps, items, shelters, stairs, blocks, gables, posts, chimneys = [], [], [], [], [], [], [], [], []

PAPER_A, PAPER_B, LIME, RUBBLE, PLASTER = 'HInt_PaperA', 'HInt_PaperB', 'HInt_Lime', 'HExt_Rubble', 'HExt_Plaster'


def wall(wid, axis, c, a0, a1, t, y0, y1, pos, neg, mats, level='ground', kind='wall', buried=()):
    """axis 'x': runs along X at constant z=c; axis 'z': along Z at constant x=c.
    pos/neg: 'ext', 'int' or 'none' for the face whose normal points to +c / -c.
    mats: material per face {'pos','neg'} and optionally 'end' for exposed wall ends.
    kind 'rail' is open timber balustrade, 'lowwall' a free-standing garden wall."""
    walls.append(dict(id=wid, axis=axis, c=round(c, 4), a0=a0, a1=a1, t=t, y0=y0, y1=y1, pos=pos, neg=neg,
                      mats=mats, level=level, kind=kind, buried=list(buried), group=level))


def opening(oid, wid, at, width, bottom, top, kind, style, leaves=0, hinge='a0', swing=1, fixture='', label='',
            exterior=False, startsOpen=False):
    openings.append(dict(id=oid, wall=wid, at=at, width=width, bottom=round(bottom, 3), top=round(top, 3), kind=kind,
                         style=style, leaves=leaves, hinge=hinge, swing=swing, fixture=fixture, label=label,
                         exterior=exterior, startsOpen=startsOpen, rise=0.0, enclosure=''))


def room(rid, name, level, x0, z0, x1, z1, floor, access, public=True, wallpaper=''):
    rooms.append(dict(id=rid, name=name, level=level, x0=x0, z0=z0, x1=x1, z1=z1, floor=floor,
                      access=access, enclosure='hotel', public=public))


def furn(fid, kind, x, z, yaw, w, d, h, y=None, room='', collide=True, extra=''):
    furniture.append(dict(id=fid, kind=kind, x=x, y=G if y is None else y, z=z, yaw=yaw, w=w, d=d, h=h,
                          room=room, collide=collide, extra=extra))


def lamp(lid, kind, x, y, z, room, role='Interior', intensity=2.6, rng=5.0, access=.6, switch='', anchor=0.0):
    lamps.append(dict(id=lid, kind=kind, x=x, y=round(y, 3), z=z, room=room, role=role, intensity=intensity,
                      range=rng, access=access, switchId=switch, shade='brass', anchor=anchor))


def item(iid, kind, x, y, z, w, h, d, doc='', evidence='', title='', target='', yaw=0, seat=None):
    entry = dict(id=iid, kind=kind, x=x, y=round(y, 3), z=z, w=w, h=h, d=d, doc=doc, evidence=evidence, title=title,
                 target=target, yaw=yaw, sitX=0, sitZ=0, standX=0, standZ=0, eye=1.15, sitY=0)
    if seat:
        entry.update(seat)
    items.append(entry)


def seat(sid, caption, x, z, yaw, depth=.45, eye=1.12, height=.46, standOff=.55, w=.5, behind=False, base=G):
    f = (math.sin(math.radians(yaw)), math.cos(math.radians(yaw)))
    k = -1 if behind else 1
    stand = (x + k * f[0] * (depth / 2 + standOff), z + k * f[1] * (depth / 2 + standOff))
    item(sid, 'seat', x, base + height, z, w, .17, depth, title=caption, yaw=yaw,
         seat=dict(sitX=x, sitZ=z, sitY=base, standX=round(stand[0], 3), standZ=round(stand[1], 3), eye=eye))


def block(bid, x0, y0, z0, x1, y1, z1):
    blocks.append(dict(id=bid, x0=x0, y0=y0, z0=z0, x1=x1, y1=y1, z1=z1))


def slab(sid, x0, z0, x1, z1, y0, y1, top, bottom, kind, level=0):
    slabs.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, y0=round(y0, 3), y1=round(y1, 3), top=top, bottom=bottom,
                      kind=kind, level=level))


def roof_y(z):
    """Top surface of the front range roof above building-local z (both slopes)."""
    return RIDGE - SLOPE * abs(z - RIDGE_Z)


def sw_roof_y(z):
    return SW_RIDGE - (SW_RIDGE - SW_EAVES) / 4.0 * abs(z - SW_RIDGE_Z)


# ---------------------------------------------------------------------------
# Front range: roughstone ground storey; the north and west walls run through.
# ---------------------------------------------------------------------------
fx0, fx1, fz0, fz1 = FR
wall('F_N', 'x', fz1 - ST / 2, fx0, fx1, ST, FOOT, U, 'ext', 'int', dict(pos=RUBBLE, neg=PAPER_A))
wall('F_S_W', 'x', fz0 + ST / 2, fx0, 16.45, ST, FOOT, U, 'int', 'ext', dict(pos=PAPER_A, neg=RUBBLE), buried=['a1'])
# Behind the dining room the south wall is the party wall with the kitchen; its east end is
# outside, beside the carport, and shows stone.
wall('F_S_E', 'x', fz0 + ST / 2, 16.45, fx1, ST, FOOT, U, 'int', 'int', dict(pos=PAPER_A, neg=LIME, end=RUBBLE), buried=['a0'])
wall('F_W', 'z', fx0 + ST / 2, fz0 + ST, fz1 - ST, ST, FOOT, U, 'int', 'ext', dict(pos=PAPER_B, neg=RUBBLE), buried=['a0', 'a1'])
wall('F_E', 'z', fx1 - ST / 2, fz0 + ST, fz1 - ST, ST, FOOT, U, 'ext', 'int', dict(pos=RUBBLE, neg=PAPER_A), buried=['a0', 'a1'])
# Ground partitions (to the ceiling).
wall('P_W', 'z', 6.48, fz0 + ST, fz1 - ST, IT, G - .02, GC, 'int', 'int', dict(pos=PAPER_A, neg=PAPER_B), buried=['a0', 'a1'])
wall('P_LS', 'x', 28.5, fx0 + ST, 6.4, IT, G - .02, GC, 'int', 'int', dict(pos=PAPER_B, neg=PAPER_B), buried=['a0', 'a1'])
wall('P_E', 'z', 16.5, fz0 + ST, fz1 - ST, IT, G - .02, GC, 'int', 'int', dict(pos=PAPER_A, neg=PAPER_A), buried=['a0', 'a1'])

# Upper storey: half-timbered walls flush with the stone below (outer faces aligned).
wall('U_N', 'x', fz1 - UT / 2, fx0, fx1, UT, U, EAVES, 'ext', 'int', dict(pos=PLASTER, neg=PAPER_B), level='upper')
wall('U_S', 'x', fz0 + UT / 2, fx0, fx1, UT, U, EAVES, 'int', 'ext', dict(pos=PAPER_B, neg=PLASTER), level='upper')
wall('U_W', 'z', fx0 + UT / 2, fz0 + UT, fz1 - UT, UT, U, EAVES, 'int', 'ext', dict(pos=PAPER_B, neg=PLASTER), level='upper', buried=['a0', 'a1'])
wall('U_E', 'z', fx1 - UT / 2, fz0 + UT, fz1 - UT, UT, U, EAVES, 'ext', 'int', dict(pos=PLASTER, neg=PAPER_B), level='upper', buried=['a0', 'a1'])
ux0, ux1, uz0, uz1 = fx0 + UT, fx1 - UT, fz0 + UT, fz1 - UT        # 2.3, 22.7, 21.3, 32.7
CN, CS = 27.28, 25.52     # corridor walls (corridor z 25.6..27.2)
wall('C_N', 'x', CN, ux0, ux1, IT, U, UC, 'int', 'int', dict(pos=PAPER_B, neg=PAPER_A), level='upper', buried=['a0', 'a1'])
wall('C_S_W', 'x', CS, ux0, 9.08, IT, U, UC, 'int', 'int', dict(pos=PAPER_A, neg=PAPER_B), level='upper', buried=['a0'])
wall('C_S_E', 'x', CS, 15.0, ux1, IT, U, UC, 'int', 'int', dict(pos=PAPER_A, neg=PAPER_B), level='upper', buried=['a1'])
for wid, x in (('P_N1', 7.5), ('P_N2', 12.6), ('P_N3', 17.7)):
    wall(wid, 'z', x, CN + IT / 2, uz1, IT, U, UC, 'int', 'int', dict(pos=PAPER_B, neg=PAPER_B), level='upper', buried=['a0', 'a1'])
wall('P_U5', 'z', 9.0, uz0, CS - IT / 2, IT, U, UC, 'int', 'int', dict(pos=PAPER_A, neg=PAPER_B), level='upper', buried=['a0', 'a1'])
wall('P_UB', 'z', 15.08, uz0, CS - IT / 2, IT, U, UC, 'int', 'int', dict(pos='HInt_Tiles', neg=PAPER_A), level='upper', buried=['a0', 'a1'])
wall('P_U6', 'z', 18.2, uz0, CS - IT / 2, IT, U, UC, 'int', 'int', dict(pos=PAPER_B, neg='HInt_Tiles'), level='upper', buried=['a0', 'a1'])
# Balustrade along the stair well on the landing (the well is open from x 10.2 to the bath wall).
HOLE = (10.2, 15.0, uz0, 22.6)
wall('L_rail', 'x', HOLE[3] + .03, HOLE[0], HOLE[1], .06, U, U + 1.0, 'int', 'int', dict(pos='PInt_Panel', neg='PInt_Panel'), level='upper', kind='rail')

# ---------------------------------------------------------------------------
# Service wing (single storey stone, kitchen and store).
# ---------------------------------------------------------------------------
sx0, sx1, sz0, sz1 = SW
SWT = .45
wall('S_S', 'x', sz0 + SWT / 2, sx0, sx1, SWT, FOOT, SW_EAVES, 'int', 'ext', dict(pos=LIME, neg=RUBBLE))
wall('S_W', 'z', sx0 + SWT / 2, sz0 + SWT, fz0, SWT, FOOT, SW_EAVES, 'int', 'ext', dict(pos=LIME, neg=RUBBLE), buried=['a0', 'a1'])
wall('S_E', 'z', sx1 - SWT / 2, sz0 + SWT, sz1 - SWT, SWT, FOOT, SW_EAVES, 'ext', 'int', dict(pos=RUBBLE, neg=LIME), buried=['a0', 'a1'])
wall('S_N', 'x', sz1 - SWT / 2, 23.0, sx1, SWT, FOOT, SW_EAVES, 'ext', 'int', dict(pos=RUBBLE, neg=LIME, end=LIME), buried=['a1'])
wall('P_K', 'z', 23.08, sz0 + SWT, sz1 - SWT, IT, G - .02, SW_CEIL, 'int', 'int', dict(pos=LIME, neg=LIME), buried=['a0', 'a1'])

# Street wall: low roughstone with piers, open at the walk gate and the carriage entry.
LW = 39.6
for wid, a0, a1 in (('LW_1', .3, 10.3), ('LW_2', 12.9, 22.4), ('LW_3', 29.8, 40.7)):
    wall(wid, 'x', LW, a0, a1, .4, -.15, .95, 'ext', 'ext', dict(pos=RUBBLE, neg=RUBBLE), level='ground', kind='lowwall')

# ---------------------------------------------------------------------------
# Openings
# ---------------------------------------------------------------------------
SILL, HEAD = G + .85, G + 2.35
for oid, at, w_ in (('win-lodge-n', 4.45, 1.2), ('win-lobby-nw', 8.6, 1.2), ('win-lobby-ne', 14.6, 1.2),
                    ('win-dining-n1', 18.2, 1.2), ('win-dining-n2', 20.9, 1.2)):
    opening(oid, 'F_N', at, w_, SILL, HEAD, 'window', 'sash', exterior=True)
opening('front-door', 'F_N', 11.6, 1.6, G, G + 2.95, 'door', 'front', leaves=2, hinge='both', swing=-1,
        fixture='hotel-front-door', label='旅馆正门', exterior=True, startsOpen=True)
opening('win-lodge-w', 'F_W', 30.5, 1.0, SILL, HEAD, 'window', 'sash', exterior=True)
opening('win-office-w', 'F_W', 24.9, 1.0, SILL, HEAD, 'window', 'sash', exterior=True)
opening('win-office-s', 'F_S_W', 4.45, 1.0, SILL, HEAD, 'window', 'sash', exterior=True)
opening('win-lobby-s', 'F_S_W', 8.1, 1.0, SILL, HEAD, 'window', 'sash', exterior=True)
opening('win-dining-e1', 'F_E', 23.8, 1.1, SILL, HEAD, 'window', 'sash', exterior=True)
opening('win-dining-e2', 'F_E', 31.0, 1.1, SILL, HEAD, 'window', 'sash', exterior=True)
opening('lodge-door', 'P_W', 31.6, .85, G, G + 2.1, 'door', 'panel', leaves=1, hinge='a1', swing=-1,
        fixture='hotel-lodge-door', label='门房门', startsOpen=True)
opening('lodge-hatch', 'P_W', 29.6, 1.0, G + .95, G + 1.9, 'window', 'interior')
opening('office-door', 'P_W', 22.6, .9, G, G + 2.1, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='hotel-office-door', label='账房门')
opening('dining-door', 'P_E', 29.2, 1.0, G, G + 2.2, 'door', 'glazed', leaves=1, hinge='a1', swing=1,
        fixture='hotel-dining-door', label='餐厅门', startsOpen=True)
opening('kitchen-door', 'F_S_E', 19.4, .9, G, G + 2.1, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='hotel-kitchen-door', label='厨房门', startsOpen=True)
# Service wing
opening('back-door', 'S_S', 20.5, 1.0, G, G + 2.15, 'door', 'plank', leaves=1, hinge='a1', swing=1,
        fixture='hotel-back-door', label='厨房后门', exterior=True)
opening('win-kitchen-s', 'S_S', 18.2, 1.2, G + 1.0, G + 2.2, 'window', 'sash', exterior=True)
opening('win-store-s', 'S_S', 26.6, 1.0, G + 1.0, G + 2.2, 'window', 'sash', exterior=True)
opening('win-kitchen-w', 'S_W', 17.2, 1.0, G + 1.0, G + 2.2, 'window', 'sash', exterior=True)
opening('win-store-e', 'S_E', 17.0, 1.0, G + 1.0, G + 2.2, 'window', 'sash', exterior=True)
opening('win-store-n', 'S_N', 26.6, 1.0, G + 1.0, G + 2.2, 'window', 'sash', exterior=True)
opening('store-door', 'P_K', 15.2, .9, G, G + 2.1, 'door', 'plank', leaves=1, hinge='a0', swing=1,
        fixture='hotel-store-door', label='储藏间门', startsOpen=True)
# Upper storey
USILL, UHEAD = U + .85, U + 2.3
for oid, wid, at, w_ in (('win-r1-n', 'U_N', 4.85, 1.1), ('win-r2-n', 'U_N', 10.05, 1.1), ('win-r3-n', 'U_N', 15.15, 1.1),
                         ('win-r4-n', 'U_N', 20.25, 1.1), ('win-r1-w', 'U_W', 30.0, 1.0), ('win-r5-w', 'U_W', 23.4, 1.0),
                         ('win-r4-e', 'U_E', 30.0, 1.0), ('win-r6-e', 'U_E', 23.4, 1.0), ('win-r5-s', 'U_S', 5.6, 1.1),
                         ('win-landing-s', 'U_S', 12.6, 1.1), ('win-r6-s', 'U_S', 20.45, 1.0)):
    opening(oid, wid, at, w_, USILL, UHEAD, 'window', 'casement', exterior=True)
opening('win-corr-w', 'U_W', 26.4, .7, USILL, UHEAD, 'window', 'casement', exterior=True)
opening('win-corr-e', 'U_E', 26.4, .7, USILL, UHEAD, 'window', 'casement', exterior=True)
opening('win-bath-s', 'U_S', 16.65, .7, U + 1.25, UHEAD, 'window', 'casement', exterior=True)
DOOR_H = U + 2.1
for n, wid, at, sw_ in ((1, 'C_N', 5.0, 1), (2, 'C_N', 10.0, 1), (3, 'C_N', 15.2, 1), (4, 'C_N', 20.2, 1),
                        (5, 'C_S_W', 6.0, -1), (6, 'C_S_E', 20.4, -1)):
    opening(f'door-r{n}', wid, at, .85, U, DOOR_H, 'door', 'panel', leaves=1, hinge='a0', swing=sw_,
            fixture=f'hotel-room-{n}-door', label=f'{"一二三四五六"[n - 1]}号房门', startsOpen=n == 6)
opening('door-bath', 'C_S_E', 16.6, .75, U, DOOR_H, 'door', 'panel', leaves=1, hinge='a1', swing=-1,
        fixture='hotel-bath-door', label='盥洗室门')

# Street gables over rooms 1 and 4 (front-facing gablets; a louvred vent, no attic window).
for gid, cx in (('gable-w', 4.85), ('gable-e', 20.25)):
    gables.append(dict(id=gid, x=cx, half=2.35, apex=9.15, face=fz1, front=fz1 + .35))

# ---------------------------------------------------------------------------
# Rooms
# ---------------------------------------------------------------------------
ix0, ix1, iz0, iz1 = fx0 + ST, fx1 - ST, fz0 + ST, fz1 - ST    # 2.5, 22.5, 21.5, 32.5
room('lodge', '门房', 0, ix0, 28.58, 6.4, iz1, 'oak', .5)
room('office', '账房', 0, ix0, iz0, 6.4, 28.42, 'oak', .45)
room('lobby', '登记大堂', 0, 6.56, iz0, 16.42, iz1, 'oak', .6)
room('dining', '餐厅', 0, 16.58, iz0, ix1, iz1, 'oak', .55)
room('kitchen', '厨房', 0, sx0 + SWT, sz0 + SWT, 23.0, fz0, 'flags', .5, public=False)
room('store', '储藏洗衣间', 0, 23.16, sz0 + SWT, sx1 - SWT, sz1 - SWT, 'flags', .35, public=False)
room('corridor', '二楼走廊', 1, ux0, CS + IT / 2, ux1, CN - IT / 2, 'oak', .35)
room('landing', '楼梯平台', 1, 9.08, uz0, 15.0, CS - IT / 2, 'oak', .4)
for n, x0, x1 in ((1, ux0, 7.42), (2, 7.58, 12.52), (3, 12.68, 17.62), (4, 17.78, ux1)):
    room(f'room{n}', f'{"一二三四"[n - 1]}号房', 1, x0, CN + IT / 2, x1, uz1, 'oak', .5)
room('room5', '五号房', 1, ux0, uz0, 8.92, CS - IT / 2, 'oak', .5)
room('bath', '盥洗室', 1, 15.16, uz0, 18.12, CS - IT / 2, 'tiles', .4)
room('room6', '六号房', 1, 18.28, uz0, ux1, CS - IT / 2, 'oak', .5)
R = {r['id']: r for r in rooms}

# ---------------------------------------------------------------------------
# Slabs: floor finishes, the upper floor structure (with the stair well), ceilings, steps.
# ---------------------------------------------------------------------------
for r in rooms:
    y = G if r['level'] == 0 else U
    if r['id'] == 'landing':
        slab('floor-landing-a', 9.08, HOLE[3], 15.0, r['z1'], y - .02, y, 'oak', 'none', 'finish', 1)
        slab('floor-landing-b', 9.08, uz0, HOLE[0], HOLE[3], y - .02, y, 'oak', 'none', 'finish', 1)
        continue
    slab('floor-' + r['id'], r['x0'], r['z0'], r['x1'], r['z1'], y - .02, y, r['floor'], 'none', 'finish', r['level'])
# Upper floor structure over the ground rooms: plaster ceiling below, finishes above; the well
# over the flight stays open.
slab('upper-a', ix0, iz0, HOLE[0], iz1, GC, U - .02, 'none', 'plaster', 'structure')
slab('upper-b', HOLE[0], HOLE[3], HOLE[1], iz1, GC, U - .02, 'none', 'plaster', 'structure')
slab('upper-c', HOLE[1], iz0, ix1, iz1, GC, U - .02, 'none', 'plaster', 'structure')
slab('ceil-upper', ux0, uz0, ux1, uz1, UC, UC + .1, 'none', 'plaster', 'ceiling', 1)
slab('ceil-service', sx0 + SWT, sz0 + SWT, sx1 - SWT, fz0, SW_CEIL, SW_CEIL + .1, 'none', 'plaster', 'ceiling')
# Porch landing and steps, rear door step (stone on the yard).
slab('porch-floor', 10.0, fz1, 13.2, 35.0, -.15, G, 'stone', 'none', 'step')
slab('porch-step', 10.3, 35.0, 12.9, 35.45, -.15, .06, 'stone', 'none', 'step')
slab('rear-step', 19.85, 12.5, 21.15, sz0, -.15, .06, 'stone', 'none', 'step')

# ---------------------------------------------------------------------------
# Stair: twenty 0.17 m risers along the lobby's back wall, rising west to the landing.
# ---------------------------------------------------------------------------
RISE = (U - G) / 20
TREAD = .25
STAIR_Z0, STAIR_Z1, STAIR_FOOT = iz0, HOLE[3], 14.95
for k in range(1, 20):
    x0 = round(STAIR_FOOT - TREAD * k, 3)
    stairs.append(dict(id=f'S{k}', x0=x0, x1=round(x0 + TREAD, 3), z0=STAIR_Z0, z1=STAIR_Z1, top=round(G + RISE * k, 3)))
for s in stairs:
    block('stair-rail-' + s['id'], s['x0'], s['top'], STAIR_Z1, s['x1'], s['top'] + .95, STAIR_Z1 + .06)

# ---------------------------------------------------------------------------
# Roofs, porch, carport, chimneys
# ---------------------------------------------------------------------------
roofs.append(dict(id='roof-front', kind='gable', x0=fx0 - .45, z0=fz0 - .45, x1=fx1 + .45, z1=fz1 + .45,
                  eaves=EAVES, ridge=RIDGE, axis='x', ridgeZ=RIDGE_Z))
roofs.append(dict(id='roof-service', kind='gable', x0=sx0 - .35, z0=sz0 - .35, x1=sx1 + .35, z1=fz0,
                  eaves=SW_EAVES, ridge=SW_RIDGE, axis='x', ridgeZ=SW_RIDGE_Z))
PORCH = dict(x0=9.85, x1=13.35, z0=fz1, z1=35.4, eaves=2.85, apex=4.05, posts=[[10.15, 35.1], [13.05, 35.1]])
roofs.append(dict(id='roof-porch', kind='porch', **{k: v for k, v in PORCH.items() if k != 'posts'}))
CARPORT = dict(x0=23.15, x1=28.85, z0=24.6, z1=33.0, eaves=2.7, ridge=3.55)
roofs.append(dict(id='roof-carport', kind='carport', **CARPORT))
for i, (x, z) in enumerate(PORCH['posts']):
    posts.append(dict(id=f'porch-post-{i}', x=x, z=z, w=.18, y0=G, y1=PORCH['eaves'] - .05))
for i, (x, z) in enumerate(((23.45, 25.0), (23.45, 32.6), (28.55, 25.0), (28.55, 32.6))):
    posts.append(dict(id=f'carport-post-{i}', x=x, z=z, w=.18, y0=YARD, y1=CARPORT['eaves'] - .05))
# Gate piers in the street wall (lantern posts at the walk gate).
for i, x in enumerate((10.3, 12.9, 22.4, 29.8)):
    posts.append(dict(id=f'pier-{i}', x=x, z=LW, w=.55, y0=-.15, y1=1.35))
chimneys.append(dict(id='chimney-lodge', x=3.0, z=31.9, w=.7, d=.6, top=round(roof_y(31.9) + 1.15, 3)))
chimneys.append(dict(id='chimney-dining', x=22.75, z=27.4, w=.75, d=1.0, top=round(RIDGE + .9, 3)))
chimneys.append(dict(id='chimney-kitchen', x=16.9, z=19.3, w=.6, d=.6, top=round(sw_roof_y(19.3) + 1.1, 3)))
chimneys.append(dict(id='chimney-room6', x=22.3, z=21.75, w=.5, d=.5, top=round(roof_y(21.75) + 1.0, 3)))

# ---------------------------------------------------------------------------
# Furniture (front faces f=(sin yaw, cos yaw): user/seat/drawer side).
# ---------------------------------------------------------------------------
UP = U
# Registration hall
furn('counter', 'counter', 14.55, 25.6, -90, 2.4, .6, 1.05, room='lobby')
furn('keyrack', 'keyrack', 16.36, 25.6, -90, 1.5, .1, .95, y=G + 1.15, room='lobby', collide=False)
furn('counter-bell', 'bell', 14.42, 24.95, 0, .1, .1, .08, y=G + 1.05, room='lobby', collide=False)
furn('counter-ledger', 'ledger', 14.45, 26.25, -90, .34, .26, .05, y=G + 1.05, room='lobby', collide=False)
furn('register-desk', 'desk', 7.0, 25.3, 90, 1.3, .62, .76, room='lobby')
furn('register-chair', 'chair', 7.78, 25.3, -90, .46, .46, .9, room='lobby')
furn('register-book', 'openledger', 7.08, 25.35, 90, .82, .44, .04, y=G + .76, room='lobby', collide=False)
furn('register-lamp', 'anglelamp', 6.86, 24.82, 90, .2, .2, .5, y=G + .76, room='lobby', collide=False)
furn('lobby-armchair-1', 'armchair', 8.3, 31.55, 180, .72, .72, .95, room='lobby')
furn('lobby-armchair-2', 'armchair', 9.75, 31.55, 180, .72, .72, .95, room='lobby')
furn('lobby-sidetable', 'sidetable', 9.02, 32.1, 180, .45, .45, .6, room='lobby')
furn('lobby-bench', 'bench', 14.9, 32.05, 180, 1.4, .45, .46, room='lobby')
furn('lobby-coat', 'coatstand', 13.1, 32.1, 180, .42, .42, 1.8, room='lobby')
furn('lobby-umbrella', 'umbrella', 10.3, 32.2, 0, .3, .3, .5, room='lobby')
furn('lobby-luggage', 'luggage', 8.2, 27.6, 30, 1.0, .6, .7, room='lobby')
furn('lobby-rug', 'rug', 11.4, 28.8, 0, 3.6, 4.4, .01, room='lobby', collide=False, extra='persian')
furn('lobby-phone', 'telephone', 6.62, 27.6, 90, .25, .1, .4, y=G + 1.25, room='lobby', collide=False)
furn('lobby-board', 'noticeboard', 16.39, 31.3, -90, .9, .03, .7, y=G + 1.3, room='lobby', collide=False)
furn('lobby-plant-1', 'plant', 15.95, 32.05, 0, .5, .5, 1.3, room='lobby')
furn('lobby-plant-2', 'plant', 9.0, 21.9, 0, .5, .5, 1.2, room='lobby')
furn('lobby-picture-1', 'picture', 8.2, 21.53, 0, .7, .03, .5, y=G + 1.6, room='lobby', collide=False)
furn('lobby-picture-2', 'picture', 6.59, 28.8, 90, .5, .03, .6, y=G + 1.45, room='lobby', collide=False)
# Porter's lodge
furn('lodge-desk', 'desk', 6.07, 29.6, -90, 1.0, .62, .76, room='lodge')
furn('lodge-chair', 'chair', 5.35, 29.6, 90, .46, .46, .9, room='lodge')
furn('lodge-stove', 'stove', 2.95, 31.95, 90, .5, .5, .85, room='lodge')
furn('lodge-pigeon', 'pigeonhole', 4.1, 28.79, 0, 1.6, .35, 1.9, room='lodge')
furn('lodge-coat', 'coatstand', 2.85, 29.2, 90, .42, .42, 1.8, room='lodge')
furn('lodge-lantern', 'handlamp', 6.05, 29.25, 0, .16, .16, .3, y=G + .76, room='lodge', collide=False)
# Landlady's office
furn('office-desk', 'desk', 4.4, 26.6, 180, 1.3, .65, .76, room='office')
furn('office-chair', 'chair', 4.4, 25.85, 0, .46, .46, .9, room='office')
furn('office-safe', 'safe', 2.95, 22.1, 90, .6, .6, .9, room='office')
furn('office-files', 'filingcabinet', 2.85, 23.75, 90, .5, .6, 1.3, room='office')
furn('office-shelf', 'bookshelf', 4.4, 28.22, 180, 2.0, .35, 2.0, room='office')
furn('office-desklamp', 'desklamp', 5.0, 26.75, 0, .3, .3, .48, y=G + .76, room='office', collide=False)
furn('office-letter', 'paper', 4.0, 26.55, 180, .21, .28, .004, y=G + .76, room='office', collide=False)
furn('office-rug', 'rug', 4.4, 25.2, 0, 2.2, 2.6, .01, room='office', collide=False, extra='runner')
# Dining room
furn('dining-fireplace', 'fireplace', 22.24, 27.4, -90, 1.7, .52, 1.45, room='dining')
for tid, x, z in (('t1', 18.4, 23.6), ('t2', 21.0, 23.6), ('t3', 18.2, 26.9), ('t4', 20.8, 26.9), ('t5', 20.6, 30.5)):
    furn('dining-' + tid, 'roundtable', x, z, 0, .9, .9, .76, room='dining')
for cid, x, z, yaw in (('t1a', 18.4, 22.88, 0), ('t1b', 18.4, 24.32, 180), ('t2a', 21.0, 22.88, 0), ('t2b', 21.0, 24.32, 180),
                       ('t3a', 18.2, 26.18, 0), ('t3b', 18.2, 27.62, 180), ('t4a', 20.08, 26.9, 90), ('t4b', 21.52, 26.9, -90),
                       ('t5a', 20.6, 29.78, 0), ('t5b', 20.6, 31.22, 180), ('t5c', 21.32, 30.5, -90)):
    furn('chair-' + cid, 'chair', x, z, yaw, .46, .46, .9, room='dining')
furn('dining-sideboard', 'sideboard', 21.5, 21.8, 0, 1.4, .45, .9, room='dining')
furn('cash-desk', 'desk', 17.25, 31.75, 90, 1.1, .6, .76, room='dining')
furn('cash-ledger-book', 'ledger', 17.25, 31.35, 90, .3, .4, .03, y=G + .76, room='dining', collide=False)
furn('cash-box', 'cashbox', 17.2, 32.0, 90, .22, .3, .12, y=G + .76, room='dining', collide=False)
furn('cash-desklamp', 'desklamp', 17.05, 32.15, 0, .3, .3, .48, y=G + .76, room='dining', collide=False)
furn('dining-rug', 'rug', 19.6, 26.9, 0, 4.6, 9.0, .01, room='dining', collide=False, extra='persian')
furn('dining-picture', 'picture', 22.47, 30.4, -90, .7, .03, .5, y=G + 1.55, room='dining', collide=False)
furn('dining-plant', 'plant', 17.1, 22.05, 0, .5, .5, 1.2, room='dining')
# Kitchen
furn('kitchen-range', 'range', 16.82, 19.3, 90, 1.4, .72, .9, room='kitchen')
furn('kitchen-table', 'kitchentable', 19.9, 17.3, 0, 1.8, .8, .85, room='kitchen')
furn('kitchen-dresser', 'dresser', 22.76, 18.2, -90, 1.5, .45, 2.0, room='kitchen')
furn('kitchen-sink', 'sink', 18.2, 13.76, 0, 1.0, .58, .85, room='kitchen')
furn('kitchen-spike', 'spike', 22.6, 20.3, -90, .1, .1, .2, y=G + .9, room='kitchen', collide=False)
furn('kitchen-bin', 'drum', 16.85, 14.0, 0, .45, .45, .6, room='kitchen')
furn('kitchen-crate', 'crate', 22.5, 14.0, 0, .7, .55, .5, room='kitchen')
# Store and laundry
furn('store-shelf', 'bookshelf', 24.75, 20.33, 180, 2.4, .35, 2.0, room='store', extra='boxes')
furn('store-luggage', 'luggage', 28.6, 19.6, 0, 1.2, .7, .8, room='store')
furn('store-tub', 'washtub', 28.7, 14.2, 0, .8, .8, .7, room='store')
furn('store-linen', 'crate', 25.6, 14.0, 0, .9, .6, .6, room='store')
furn('store-mangle', 'mangle', 27.1, 14.1, 0, .9, .5, 1.2, room='store')


def bedroom(rid, bx, bz, byaw, bw, wardrobe, washstand, chair, extra=()):
    furn(rid + '-bed', 'bed', bx, bz, byaw, bw, 2.0, 1.0, y=UP, room=rid)
    furn(rid + '-wardrobe', 'wardrobe', *wardrobe, 1.0, .55, 2.0, y=UP, room=rid)
    furn(rid + '-washstand', 'washstand', *washstand, .8, .45, .8, y=UP, room=rid)
    furn(rid + '-chair', 'chair', *chair, .46, .46, .9, y=UP, room=rid)
    for e in extra:
        furn(*e[:8], y=UP, room=rid, **(e[8] if len(e) > 8 else {}))


bedroom('room1', 6.42, 30.6, -90, 1.4, (2.62, 28.2, 90), (3.4, 32.45, 180), (6.6, 28.25, 0),
        extra=[('room1-rug', 'rug', 4.9, 30.0, 0, 2.0, 2.6, .01, dict(collide=False, extra='runner'))])
bedroom('room2', 8.58, 30.6, 90, 1.4, (12.22, 28.2, -90), (11.4, 32.45, 180), (8.4, 28.25, 0))
bedroom('room3', 16.62, 30.6, -90, 1.4, (12.98, 28.2, 90), (13.6, 32.45, 180), (16.8, 28.25, 0))
bedroom('room4', 18.78, 30.6, 90, 1.4, (22.42, 28.2, -90), (21.8, 32.45, 180), (18.6, 28.25, 0))
bedroom('room5', 7.92, 23.0, -90, 1.4, (2.62, 24.9, 90), (4.0, 21.55, 0), (4.6, 24.6, 180))
bedroom('room6', 19.28, 22.4, 90, 1.0, (22.42, 24.85, -90), (21.0, 21.55, 0), (21.6, 23.9, -90),
        extra=[('room6-stove', 'stove', 22.36, 21.72, -90, .45, .45, .8), ('room6-tray', 'tray', 21.0, 21.55, 0, .34, .26, .05,
                                                                              dict(collide=False))])
# The room-6 tray stands on the washstand top.
furniture[-1]['y'] = UP + .8
furn('bath-tub', 'bathtub', 16.6, 22.05, 0, 1.6, .74, .6, y=UP, room='bath')
furn('bath-wc', 'wc', 17.75, 24.2, -90, .45, .65, .8, y=UP, room='bath')
furn('bath-basin', 'basin', 15.45, 24.4, 90, .5, .42, .85, y=UP, room='bath')
furn('landing-bench', 'bench', 12.0, 25.18, 180, 1.4, .4, .46, y=UP, room='landing')
furn('landing-linen', 'wardrobe', 14.62, 24.2, -90, 1.0, .6, 2.0, y=UP, room='landing')
furn('corridor-runner', 'rug', 12.5, 26.4, 0, 19.6, .9, .01, y=UP, room='corridor', collide=False, extra='runner')
# Exterior props (on the yard surface).
furn('front-bench', 'bench', 7.3, 33.6, 0, 1.4, .45, .46, y=YARD)
furn('front-planter-w', 'planterbox', 9.55, 33.45, 0, .5, .5, .55, y=YARD)
furn('front-planter-e', 'planterbox', 13.65, 33.45, 0, .5, .5, .55, y=YARD)
furn('front-barrels', 'barrel', 2.6, 33.7, 0, .55, .55, .8, y=YARD)
furn('front-cart', 'luggagecart', 17.4, 34.6, 30, .9, 1.4, .9, y=YARD)
furn('carport-trunks', 'luggage', 27.9, 23.8, 0, 1.2, .7, .8, y=YARD)
furn('yard-barrels', 'barrel', 23.9, 12.2, 0, .55, .55, .8, y=YARD)
furn('yard-crates', 'crate', 17.3, 11.8, 20, .9, .7, .7, y=YARD)
furn('yard-bench', 'bench', 9.0, 20.3, 180, 1.4, .45, .46, y=YARD)

# ---------------------------------------------------------------------------
# Lamps. Interior lights affect only the interior rendering layer (Light Layer 4).
# ---------------------------------------------------------------------------
P = GC - .62
lamp('lamp-lobby-1', 'pendant', 9.4, P, 28.4, 'lobby', intensity=2.6, rng=6.0, access=.6, anchor=GC)
lamp('lamp-lobby-2', 'pendant', 13.4, P, 29.6, 'lobby', intensity=2.6, rng=6.0, access=.6, anchor=GC)
# The porter's counter (z 24.4-26.8), the key rack and the stair foot (x 14.95) lie in the hall's back half,
# out of reach of the two pendants by the door; this one hangs between them, clear of the ceiling beams
# (user, 2026-10-16: the counter and the stair foot were too dark to see at night).
lamp('lamp-lobby-3', 'pendant', 14.4, P, 23.8, 'lobby', intensity=2.2, rng=5.0, access=.6, anchor=GC)
lamp('lamp-register', 'angle', 6.86, G + 1.18, 24.82, 'lobby', intensity=.7, rng=2.2, access=.55)
lamp('lamp-lodge', 'pendant', 4.4, P, 30.7, 'lodge', intensity=1.8, rng=4.0, access=.5, anchor=GC)
lamp('lamp-office', 'pendant', 4.4, P, 24.6, 'office', intensity=1.9, rng=4.5, access=.45, anchor=GC)
lamp('lamp-office-desk', 'desk', 5.0, G + 1.13, 26.75, 'office', intensity=.75, rng=2.2, access=.45, switch='hotel-office-lamp-off')
lamp('lamp-dining-1', 'pendant', 19.6, P, 24.6, 'dining', intensity=2.4, rng=5.5, access=.55, anchor=GC)
lamp('lamp-dining-2', 'pendant', 19.6, P, 29.4, 'dining', intensity=2.4, rng=5.5, access=.55, anchor=GC)
lamp('lamp-cash', 'desk', 17.05, G + 1.13, 32.15, 'dining', intensity=.75, rng=2.2, access=.45, switch='hotel-cash-lamp-off')
lamp('lamp-kitchen', 'pendant', 19.9, SW_CEIL - .55, 17.3, 'kitchen', intensity=2.4, rng=5.5, access=.5, anchor=SW_CEIL)
lamp('lamp-store', 'bulb', 26.4, SW_CEIL - .4, 17.0, 'store', intensity=1.4, rng=4.0, access=.3, anchor=SW_CEIL)
UPL = UC - .48
lamp('lamp-corridor-1', 'pendant', 6.0, UPL, 26.4, 'corridor', intensity=1.4, rng=4.5, access=.3, anchor=UC)
lamp('lamp-corridor-2', 'pendant', 18.6, UPL, 26.4, 'corridor', intensity=1.4, rng=4.5, access=.3, anchor=UC)
lamp('lamp-landing', 'pendant', 12.4, UPL, 23.9, 'landing', intensity=2.2, rng=5.5, access=.4, anchor=UC)  # also reaches down the stair
for rid, x, z in (('room1', 4.9, 30.2), ('room2', 10.0, 30.2), ('room3', 15.15, 30.2), ('room4', 20.25, 30.2),
                  ('room5', 5.6, 23.4), ('room6', 20.5, 23.3), ('bath', 16.6, 23.4)):
    lamp('lamp-' + rid, 'pendant', x, UPL, z, rid, intensity=1.5, rng=4.2, access=.4, anchor=UC)
# Exterior lanterns (photocell; share the twelve street pools).
lamp('lantern-porch', 'hang', 11.6, 2.4, 34.3, 'street', role='Exterior', intensity=2.6, rng=7.0, access=.85)
lamp('lantern-gate-w', 'post', 10.3, 1.62, LW, 'street', role='Exterior', intensity=2.2, rng=6.0, access=.85)
lamp('lantern-gate-e', 'post', 12.9, 1.62, LW, 'street', role='Exterior', intensity=2.2, rng=6.0, access=.85)
lamp('lantern-carport', 'hang', 26.0, 2.55, 28.8, 'yard', role='Exterior', intensity=1.8, rng=6.0, access=.85)
lamp('lantern-back', 'wall', 20.5, G + 2.35, 12.78, 'yard', role='Exterior', intensity=1.8, rng=5.5, access=.85)

# ---------------------------------------------------------------------------
# Interaction points (Unity trigger boxes). Texts live in Resources/HotelDocuments.json.
# ---------------------------------------------------------------------------
DT = G + .76
CT = G + 1.05
# The register lies open on the side-lit desk: the 20 January page (I01) on the left, the
# next page on the right. The next page's pressed writing (I02) can be read only while the
# desk lamp's shade is tilted down to rake across the paper ('hotel-register-lamp').
PAGE_W, PAGE_H = .3, .4
item('register-left', 'evidence', 7.08, DT + .045, 25.15, PAGE_W, .04, PAGE_H, doc='i01', evidence='I01', yaw=90)
item('register-right', 'page', 7.08, DT + .045, 25.55, PAGE_W, .04, PAGE_H, doc='register-next', target='hotel-register-lamp', yaw=90)
item('register-raking', 'evidence', 7.08, DT + .045, 25.55, PAGE_W, .04, PAGE_H, doc='i02', evidence='I02', target='hotel-register-lamp', yaw=90)
item('register-lamp', 'rakinglamp', 6.86, DT + .32, 24.82, .26, .34, .26, title='查阅桌台灯', target='hotel-register-lamp')
item('dining-ledger', 'evidence', 17.25, DT + .035, 31.35, .3, .03, .4, doc='i03', evidence='I03', yaw=90)
item('keyrack', 'document', 16.3, G + 1.6, 25.6, .1, 1.0, 1.5, doc='key-rack')
item('counter-ledger', 'document', 14.45, CT + .04, 26.25, .3, .08, .36, doc='counter-book')
item('lobby-board', 'document', 16.34, G + 1.65, 31.3, .08, .7, .9, doc='house-rules')
item('lobby-phone', 'document', 6.66, G + 1.45, 27.6, .14, .42, .3, doc='lobby-phone')
item('lobby-luggage', 'document', 8.2, G + .45, 27.6, 1.05, .8, .75, doc='lobby-luggage')
item('lodge-pigeon', 'document', 4.1, G + 1.1, 28.98, 1.6, 1.8, .1, doc='lodge-pigeonholes')
item('lodge-log', 'document', 6.0, DT + .03, 29.85, .3, .06, .24, doc='lodge-log')
item('office-letter', 'document', 4.0, DT + .02, 26.55, .26, .04, .32, doc='police-letter')
item('office-shelf', 'document', 4.4, G + 1.2, 28.0, 2.0, 1.8, .12, doc='old-registers')
item('office-safe', 'document', 3.3, G + .45, 22.1, .1, .8, .6, doc='office-safe')
item('dining-fireplace', 'document', 21.92, G + .7, 27.4, .14, 1.2, 1.6, doc='fireplace')
item('dining-sideboard', 'document', 21.5, G + .95, 22.08, 1.4, .2, .12, doc='sideboard')
item('kitchen-spike', 'document', 22.6, G + 1.05, 20.3, .16, .3, .16, doc='order-spike')
item('kitchen-dresser', 'document', 22.5, G + 1.4, 18.2, .1, 1.0, 1.5, doc='dresser')
item('store-luggage', 'document', 28.6, G + .45, 19.2, 1.25, .85, .1, doc='left-luggage')
item('room6-bed', 'document', 19.28, UP + .62, 22.4, 2.05, .3, 1.05, doc='room6-bed')
item('room6-stove', 'document', 22.0, UP + .55, 21.72, .12, .7, .5, doc='room6-stove')
item('room6-tray', 'document', 21.0, UP + .86, 21.55, .36, .1, .28, doc='room6-tray')
item('room1-bed', 'document', 6.42, UP + .62, 30.6, 2.05, .3, 1.45, doc='guest-room')
item('bath-tub', 'document', 16.6, UP + .66, 22.05, 1.65, .1, .78, doc='bath')
# Lamp switches
item('switch-office', 'lampswitch', 5.0, DT + .12, 26.75, .26, .26, .26, title='账房台灯', target='lamp-office-desk')
item('switch-cash', 'lampswitch', 17.05, DT + .12, 32.15, .26, .26, .26, title='账桌台灯', target='lamp-cash')
# Character placeholders (user, 2026-10-16: a teammate makes the people). Talk targets only.
item('porter', 'npc', 15.6, G, 25.6, .7, 1.8, .5, title='门房', target='porter', yaw=-90)
item('keeper', 'npc', 18.05, G, 31.75, .7, 1.8, .5, title='老板娘', target='keeper', yaw=-90)
seat('hotel-seat-register', '查阅桌椅', 7.78, 25.3, -90, behind=True)
seat('hotel-seat-armchair-1', '大堂扶手椅', 8.3, 31.55, 180, depth=.6, w=.72, eye=1.08)
seat('hotel-seat-armchair-2', '大堂扶手椅', 9.75, 31.55, 180, depth=.6, w=.72, eye=1.08)
seat('hotel-seat-bench', '大堂长凳', 14.9, 32.05, 180, eye=1.1, w=1.4)
seat('hotel-seat-lodge', '门房的椅子', 5.35, 29.6, 90, behind=True)
seat('hotel-seat-office', '账房椅', 4.4, 25.85, 0, behind=True)
seat('hotel-seat-dining', '餐桌椅', 18.2, 27.62, 180)
seat('hotel-seat-front', '门前长凳', 7.3, 33.6, 0, eye=1.1, w=1.4, base=YARD)

# ---------------------------------------------------------------------------
# Weather shelters: rain clipping + one acoustic enclosure for the whole building.
# ---------------------------------------------------------------------------
def shelter(sid, x0, z0, x1, z1, eaves, rise, ridgeAlongX, doors, open_=False):
    shelters.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, eaves=eaves, rise=rise, ridgeAlongX=ridgeAlongX,
                         doors=doors, open=open_))


HOTEL_DOORS = ['hotel-front-door', 'hotel-back-door']
shelter('front', fx0 - .45, fz0 - .45, fx1 + .45, fz1 + .45, EAVES, RIDGE - EAVES, True, HOTEL_DOORS)
shelter('service', sx0 - .35, sz0 - .35, sx1 + .35, fz0 - .45, SW_EAVES, SW_RIDGE - SW_EAVES, True, HOTEL_DOORS)
shelter('porch', PORCH['x0'], PORCH['z0'] + .45, PORCH['x1'], PORCH['z1'], PORCH['eaves'], 0, False, [], True)
shelter('carport', CARPORT['x0'], CARPORT['z0'], CARPORT['x1'], CARPORT['z1'], CARPORT['eaves'], 0, False, [], True)

# ---------------------------------------------------------------------------
# Derived opening data and validation.
# ---------------------------------------------------------------------------
W = {w['id']: w for w in walls}
errors = []
for o in openings:
    w = W[o['wall']]
    x, z = (o['at'], w['c']) if w['axis'] == 'x' else (w['c'], o['at'])
    o['x'], o['z'] = round(x, 3), round(z, 3)
    o['normal'] = 'z' if w['axis'] == 'x' else 'x'
    lo, hi = o['at'] - o['width'] / 2, o['at'] + o['width'] / 2
    if lo < w['a0'] + .05 or hi > w['a1'] - .05:
        errors.append(f"opening {o['id']} outside wall {w['id']}")
    if o['bottom'] < w['y0'] - 1e-6 or o['top'] > w['y1'] + 1e-6:
        errors.append(f"opening {o['id']} height outside wall")
    if o['kind'] == 'door' and o['leaves'] < 1:
        errors.append(f"door {o['id']} without leaf")
    if o['leaves'] and not o['fixture']:
        errors.append(f"leaf without fixture id {o['id']}")
for w in walls:
    own = sorted((o for o in openings if o['wall'] == w['id']), key=lambda o: o['at'])
    for a, b in zip(own, own[1:]):
        if a['at'] + a['width'] / 2 > b['at'] - b['width'] / 2 - .1 and not (a['top'] <= b['bottom'] or b['top'] <= a['bottom']):
            errors.append(f"openings overlap {a['id']} {b['id']}")
fixtures = [o['fixture'] for o in openings if o['fixture']]
if len(fixtures) != len(set(fixtures)):
    errors.append('duplicate fixture ids')
for name, group in (('furniture', furniture), ('items', items), ('lamps', lamps), ('walls', walls), ('openings', openings), ('blocks', blocks)):
    ids = [x['id'] for x in group]
    if len(ids) != len(set(ids)):
        errors.append('duplicate ids in ' + name)
for level in (0, 1):
    rs = [r for r in rooms if r['level'] == level]
    for i, a in enumerate(rs):
        for b in rs[i + 1:]:
            if min(a['x1'], b['x1']) - max(a['x0'], b['x0']) > .2 and min(a['z1'], b['z1']) - max(a['z0'], b['z0']) > .2:
                errors.append(f"rooms overlap {a['id']} {b['id']}")


def footprint(f):
    yaw = math.radians(f['yaw'])
    c, s = abs(math.cos(yaw)), abs(math.sin(yaw))
    hx = (f['w'] * c + f['d'] * s) / 2
    hz = (f['w'] * s + f['d'] * c) / 2
    return f['x'] - hx, f['z'] - hz, f['x'] + hx, f['z'] + hz


for f in furniture:
    r = R.get(f['room'])
    if not r:
        continue
    x0, z0, x1, z1 = footprint(f)
    if x0 < r['x0'] - .02 or x1 > r['x1'] + .02 or z0 < r['z0'] - .02 or z1 > r['z1'] + .02:
        errors.append(f"furniture {f['id']} outside {r['id']}: {x0:.2f},{z0:.2f}..{x1:.2f},{z1:.2f}")
solid = [f for f in furniture if f['collide']]
for i, a in enumerate(solid):
    for b in solid[i + 1:]:
        if abs(a['y'] - b['y']) > .5:
            continue
        ax0, az0, ax1, az1 = footprint(a); bx0, bz0, bx1, bz1 = footprint(b)
        if min(ax1, bx1) - max(ax0, bx0) > .02 and min(az1, bz1) - max(az0, bz0) > .02:
            errors.append(f"furniture overlap {a['id']} {b['id']}")
# Door swing and approach clearances must stay free of colliding furniture and stairs.
for o in openings:
    if o['kind'] != 'door':
        continue
    w = W[o['wall']]
    level_y = o['bottom'] if o['bottom'] > 1 else G
    reach = max(o['width'] / max(o['leaves'], 1), .7) + .1
    for side in (-1, 1):
        if w['axis'] == 'x':
            box = (o['at'] - o['width'] / 2, w['c'] + side * (w['t'] / 2), o['at'] + o['width'] / 2, w['c'] + side * (w['t'] / 2 + reach))
        else:
            box = (w['c'] + side * (w['t'] / 2), o['at'] - o['width'] / 2, w['c'] + side * (w['t'] / 2 + reach), o['at'] + o['width'] / 2)
        bx0, bz0 = min(box[0], box[2]), min(box[1], box[3])
        bx1, bz1 = max(box[0], box[2]), max(box[1], box[3])
        for f in furniture:
            if not f['collide'] or abs(f['y'] - level_y) > .5:
                continue
            x0, z0, x1, z1 = footprint(f)
            if min(x1, bx1) - max(x0, bx0) > .02 and min(z1, bz1) - max(z0, bz0) > .02:
                errors.append(f"furniture {f['id']} blocks {o['id']}")
        if level_y < 1:
            for s in stairs:
                if min(s['x1'], bx1) - max(s['x0'], bx0) > .02 and min(s['z1'], bz1) - max(s['z0'], bz0) > .02:
                    errors.append(f"stair {s['id']} blocks {o['id']}")
        for p_ in posts:
            if min(p_['x'] + p_['w'] / 2, bx1) - max(p_['x'] - p_['w'] / 2, bx0) > .02 and min(p_['z'] + p_['w'] / 2, bz1) - max(p_['z'] - p_['w'] / 2, bz0) > .02:
                errors.append(f"post {p_['id']} blocks {o['id']}")
for o in openings:
    if o['kind'] == 'door' and o['top'] - o['bottom'] < 1.95:
        errors.append(f"door {o['id']} head too low for the 1.78 m player capsule")
for it in items:
    if it['kind'] != 'document':
        continue
    for f in furniture:
        if not f['collide'] or min(f['y'] + f['h'], it['y'] + it['h'] / 2) - max(f['y'], it['y'] - it['h'] / 2) < .05:
            continue
        fx0_, fz0_, fx1_, fz1_ = footprint(f)
        ix0_, ix1_, iz0_, iz1_ = it['x'] - it['w'] / 2, it['x'] + it['w'] / 2, it['z'] - it['d'] / 2, it['z'] + it['d'] / 2
        if min(fx1_, ix1_) - max(fx0_, ix0_) > .05 and min(fz1_, iz1_) - max(fz0_, iz0_) > .05:
            if any(abs(a_ - b_) < .02 for a_, b_ in ((fx0_, ix0_), (fx1_, ix1_), (fz0_, iz0_), (fz1_, iz1_))) and not (ix0_ < fx0_ - .02 or ix1_ > fx1_ + .02 or iz0_ < fz0_ - .02 or iz1_ > fz1_ + .02):
                errors.append(f"trigger {it['id']} shares a face with collider {f['id']}")
# Stair: the flight arrives exactly at the landing level and edge, clear of furniture; headroom
# under the upper floor holds wherever the flight passes beneath it.
if abs(stairs[-1]['x0'] - HOLE[0]) > .01 or abs(G + RISE * 20 - U) > .002:
    errors.append('stair does not meet the landing')
for s in stairs:
    if s['top'] + 1.95 > GC and s['x0'] >= HOLE[1] - .01:
        errors.append(f"stair {s['id']} has no headroom under the upper floor")
    for f in furniture:
        if not f['collide'] or f['y'] > 1:
            continue
        x0, z0, x1, z1 = footprint(f)
        if min(x1, s['x1']) - max(x0, s['x0'] - .6) > .02 and min(z1, s['z1'] + .6) - max(z0, s['z0']) > .02:
            errors.append(f"furniture {f['id']} crowds the stair at {s['id']}")
# The approach at the foot of the stair (east of the first tread) stays clear.
foot = (STAIR_FOOT, STAIR_Z0, 16.42, STAIR_Z1 + .2)
for f in furniture:
    if f['collide'] and f['y'] < 1:
        x0, z0, x1, z1 = footprint(f)
        if min(x1, foot[2]) - max(x0, foot[0]) > .02 and min(z1, foot[3]) - max(z0, foot[1]) > .02:
            errors.append(f"furniture {f['id']} blocks the foot of the stair")
if UC - U < 2.4 or GC - G < 2.9:
    errors.append('storey heights')
# The hotel car's stand (world 169.5, 2.0) and its run to the street stay clear, doors included.
car = (CAR['x'] - CAR['halfW'] - .95, CAR['z'] - CAR['back'], CAR['x'] + CAR['halfW'] + .95, CAR['z'] + CAR['front'])
lane = (CAR['x'] - 1.3, CAR['z'], CAR['x'] + 1.3, 40.0)
for name, (bx0, bz0, bx1, bz1) in (('car stand', car), ('car lane', lane)):
    for p_ in posts:
        if min(p_['x'] + p_['w'] / 2, bx1) - max(p_['x'] - p_['w'] / 2, bx0) > 0 and min(p_['z'] + p_['w'] / 2, bz1) - max(p_['z'] - p_['w'] / 2, bz0) > 0:
            errors.append(f"post {p_['id']} intrudes on the {name}")
    for f in furniture:
        if f['collide'] and not f['room']:
            x0, z0, x1, z1 = footprint(f)
            if min(x1, bx1) - max(x0, bx0) > 0 and min(z1, bz1) - max(z0, bz0) > 0:
                errors.append(f"{f['id']} intrudes on the {name}")
    for w in walls:
        if w['axis'] == 'x':
            wx0, wx1, wz0, wz1 = w['a0'], w['a1'], w['c'] - w['t'] / 2, w['c'] + w['t'] / 2
        else:
            wx0, wx1, wz0, wz1 = w['c'] - w['t'] / 2, w['c'] + w['t'] / 2, w['a0'], w['a1']
        if min(wx1, bx1) - max(wx0, bx0) > 0 and min(wz1, bz1) - max(wz0, bz0) > 0:
            errors.append(f"wall {w['id']} intrudes on the {name}")
if FR[1] > car[0] or CARPORT['eaves'] - .3 < 1.95:
    errors.append('front range or carport too close to the hotel car')

# Agreement with the town map (B08 parts, doors, windows) in world coordinates.
plan = json.loads((ROOT / 'docs/maps/town-current/plan-data.json').read_text(encoding='utf-8'))
b08 = next(b for b in plan['buildings'] if b['id'] == 'B08')
parts = sorted(tuple(round(v, 3) for v in p['rect']) for p in b08['parts'])


def world_rect(x0, x1, z0, z1):
    return (round(ORIGIN[0] + (x0 + x1) / 2, 3), round(ORIGIN[2] + (z0 + z1) / 2, 3), round(x1 - x0, 3), round(z1 - z0, 3))


expect = sorted([world_rect(fx0, fx1, fz0, fz1), world_rect(sx0, sx1, sz0, sz1)])
if parts != expect:
    errors.append(f'plan-data B08 parts differ: {parts} != {expect}')
fd = next(o for o in openings if o['id'] == 'front-door')
bd = next(o for o in openings if o['id'] == 'back-door')
if [round(fd['x'] + ORIGIN[0], 3), round(fz1 + ORIGIN[2], 3)] != [round(v, 3) for v in b08['door']]:
    errors.append(f"front door differs from plan-data door {b08['door']}")
if [round(bd['x'] + ORIGIN[0], 3), round(sz0 + ORIGIN[2], 3)] != [round(v, 3) for v in b08['rearDoor']]:
    errors.append(f"back door differs from plan-data rearDoor {b08['rearDoor']}")
side_of = {'F_N': 'N', 'F_S_W': 'S', 'F_W': 'W', 'F_E': 'E', 'U_N': 'N', 'U_S': 'S', 'U_W': 'W', 'U_E': 'E',
           'S_S': 'S', 'S_W': 'W', 'S_E': 'E', 'S_N': 'N'}


def outer(w):
    side = side_of[w['id']]
    return w['c'] + (w['t'] / 2 if side in ('E', 'N') else -w['t'] / 2)


ext_windows = []
for o in openings:
    if o['kind'] != 'window' or not o['exterior']:
        continue
    w = W[o['wall']]
    tag = 'U' if w['level'] == 'upper' else 'G'
    if w['axis'] == 'x':
        ext_windows.append((round(o['x'] + ORIGIN[0], 2), round(outer(w) + ORIGIN[2], 2), o['width'], side_of[w['id']], tag))
    else:
        ext_windows.append((round(outer(w) + ORIGIN[0], 2), round(o['z'] + ORIGIN[2], 2), o['width'], side_of[w['id']], tag))
if '--print-windows' in sys.argv:
    print(json.dumps(dict(windows=sorted(ext_windows), labels={o['id']: o['x'] for o in openings}), ensure_ascii=False))
    print(chr(10).join(errors))
    sys.exit(0)
plan_windows = sorted((round(w[0], 2), round(w[1], 2), w[2], w[4], 'U' if w[3].startswith('二层') else 'G') for w in b08['windows'] if len(w) > 4)
if sorted(ext_windows) != plan_windows:
    errors.append(f'exterior windows differ from plan-data: {sorted(set(ext_windows) ^ set(plan_windows))}')
if b08['storeys'] != 2:
    errors.append('plan-data B08 must have two storeys (the front range)')

fin = [s for s in slabs if s['kind'] == 'finish']
for i, a in enumerate(fin):
    for b in fin[i + 1:]:
        if abs(a['y1'] - b['y1']) < .01 and min(a['x1'], b['x1']) - max(a['x0'], b['x0']) > .001 and min(a['z1'], b['z1']) - max(a['z0'], b['z0']) > .001:
            errors.append(f"floor finishes overlap (z-fighting): {a['id']} {b['id']}")
if errors:
    sys.exit('HOTEL_LAYOUT_FAIL:\n' + '\n'.join(errors))

data = dict(version=1, id='B08', name='青川旅馆', scheme='A 山墙老客栈', origin=list(ORIGIN),
            levels=dict(ground=G, groundCeiling=GC, upper=U, upperCeiling=UC, eaves=EAVES, ridge=RIDGE, ridgeZ=RIDGE_Z,
                        slope=SLOPE, roofThickness=ROOF_T, yard=YARD, foot=FOOT, serviceEaves=SW_EAVES,
                        serviceRidge=SW_RIDGE, serviceRidgeZ=SW_RIDGE_Z, serviceCeiling=SW_CEIL, streetWall=LW),
            front=dict(x0=fx0, x1=fx1, z0=fz0, z1=fz1), service=dict(x0=sx0, x1=sx1, z0=sz0, z1=sz1),
            stairWell=dict(x0=HOLE[0], x1=HOLE[1], z0=HOLE[2], z1=HOLE[3]), car=CAR, porch=PORCH, carport=CARPORT,
            walls=walls, openings=openings, rooms=rooms, slabs=slabs, stairs=stairs, blocks=blocks, roofs=roofs,
            gables=gables, posts=posts, chimneys=chimneys, furniture=furniture, lamps=lamps, items=items, shelters=shelters,
            notes='Unity building-local metres (+X east, +Y up, +Z north); root at world origin above; yaw degrees, front faces (sin yaw, cos yaw).')
text = json.dumps(data, ensure_ascii=False, indent=1) + '\n'
out = ROOT / 'assets/source/hotel'
out.mkdir(parents=True, exist_ok=True)
for path in (out / 'hotel-layout.json', ROOT / 'game/Assets/Resources/HotelLayout.json'):
    path.write_bytes(text.encode('utf-8'))
print(json.dumps(dict(status='HOTEL_LAYOUT_PASS', walls=len(walls), openings=len(openings),
                      doors=sum(o['leaves'] > 0 for o in openings), windows=sum(o['kind'] == 'window' for o in openings),
                      exteriorWindows=len(ext_windows), rooms=len(rooms), furniture=len(furniture), lamps=len(lamps),
                      items=len(items), shelters=len(shelters), stairSteps=len(stairs)), ensure_ascii=False))
