"""Shared layout contract for the E stone-barn repair garage (B07, 旧石仓修车院).

One source for the Blender shell/furniture generator (tools/garage/build_garage.py)
and the Unity assembler (BuildSlice.Garage). Coordinates are Unity building-local
metres: +X east, +Y up, +Z north; the prefab root sits at world (-144, 0, 27.7)
with no rotation, so plan-data world X = -144 + x and Z = 27.7 + z.

User-selected concept: docs/concepts/repair-shop/round-01/e-stone-barn-garage.png
(2026-10-14). A tall single-volume roughstone hall with three narrow transverse
timber gables on its long (lane) side, a low segmental-arch vehicle doorway with
dark walnut leaves, tall narrow iron-framed windows in heavy stone reveals, a low
cream-plaster customer office at the street end, a real storage loft over the
back tool store reached by a straight stair, and a timber lean-to parts shed
closing a modest rear yard.

Run from the repository root (stdlib only):
    python -X utf8 tools/garage/garage_layout.py
Writes assets/source/garage/garage-layout.json and its Unity copy
game/Assets/Resources/GarageLayout.json, after validating the geometry and
checking that docs/maps/town-current/plan-data.json B07 matches.
"""
import json, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ORIGIN = (-144.0, 0.0, 27.7)

# Levels (metres above the town datum).
G = 0.15             # floor finish (town walk is .10)
FOOT = -0.25
EAVES = 4.6          # hall wall head
RIDGE = 8.4          # hall ridge (roof top surface)
RIDGE_X = 11.3
HALL = (5.0, 17.6, 0.0, 19.0)        # outer x0, x1, z0, z1
OFFICE = (0.0, 5.0, 0.0, 12.0)
OFFICE_CEIL = 2.95
OFFICE_ROOF_HI, OFFICE_ROOF_LO = 3.75, 3.1
LOFT = 2.75          # loft floor top
LOFT_UNDER = 2.55
STONE_T = 0.55
OFFICE_T = 0.35
INT_T = 0.16
SLOPE = (RIDGE - EAVES) / (RIDGE_X - HALL[0])
ROOF_T = 0.14        # sarking + slate: underside is this far below the top surface
YARD = 0.06          # rear yard paving top

walls, openings, rooms, slabs, roofs = [], [], [], [], []
furniture, lamps, items, shelters, stairs, blocks, trusses, dormers, posts = [], [], [], [], [], [], [], [], []


def wall(wid, axis, c, a0, a1, t, y0, y1, pos, neg, group, kind='wall', buried=(), faceGroups=None):
    """axis 'x': runs along X at constant z=c; axis 'z': along Z at constant x=c.
    pos/neg: finish of the face whose normal points to +c / -c: 'ext', 'int' or 'none'.
    group picks the material family; kind 'rail'/'fence' are built as open timber work
    with the same solid collision; buried lists ends that butt a neighbouring wall."""
    walls.append(dict(id=wid, axis=axis, c=c, a0=a0, a1=a1, t=t, y0=y0, y1=y1, pos=pos, neg=neg,
                      group=group, kind=kind, buried=list(buried), level='ground', faceGroups=faceGroups or {}))


def opening(oid, wid, at, width, bottom, top, kind, style, leaves=0, hinge='a0', swing=1, fixture='', label='',
            exterior=False, startsOpen=False, rise=0.0):
    """at: centre along the wall axis; rise: segmental arch rise below top (0 = square head).
    swing: +1/-1, side of the wall the leaf turns into."""
    openings.append(dict(id=oid, wall=wid, at=at, width=width, bottom=bottom, top=top, kind=kind, style=style,
                         leaves=leaves, hinge=hinge, swing=swing, fixture=fixture, label=label,
                         exterior=exterior, startsOpen=startsOpen, rise=rise, enclosure=''))


def room(rid, name, level, x0, z0, x1, z1, floor, access, public=True):
    rooms.append(dict(id=rid, name=name, level=level, x0=x0, z0=z0, x1=x1, z1=z1, floor=floor,
                      access=access, enclosure='garage', public=public))


def furn(fid, kind, x, z, yaw, w, d, h, y=None, room='', collide=True, extra=''):
    furniture.append(dict(id=fid, kind=kind, x=x, y=G if y is None else y, z=z, yaw=yaw, w=w, d=d, h=h,
                          room=room, collide=collide, extra=extra))


def lamp(lid, kind, x, y, z, room, role='Interior', intensity=3.0, rng=5.0, access=.8, switch='', anchor=0.0):
    """anchor: height of the beam/ceiling the pendant hangs from."""
    lamps.append(dict(id=lid, kind=kind, x=x, y=y, z=z, room=room, role=role, intensity=intensity,
                      range=rng, access=access, switchId=switch, shade='copper', anchor=anchor))


def item(iid, kind, x, y, z, w, h, d, doc='', evidence='', title='', target='', yaw=0, seat=None):
    entry = dict(id=iid, kind=kind, x=x, y=y, z=z, w=w, h=h, d=d, doc=doc, evidence=evidence, title=title,
                 target=target, yaw=yaw, sitX=0, sitZ=0, standX=0, standZ=0, eye=1.15, sitY=0)
    if seat:
        entry.update(seat)
    items.append(entry)


def seat(sid, caption, x, z, yaw, depth=.45, eye=1.12, height=.46, standOff=.55, w=.5, behind=False):
    f = (math.sin(math.radians(yaw)), math.cos(math.radians(yaw)))
    k = -1 if behind else 1
    stand = (x + k * f[0] * (depth / 2 + standOff), z + k * f[1] * (depth / 2 + standOff))
    item(sid, 'seat', x, G + height, z, w, .17, depth, title=caption, yaw=yaw,
         seat=dict(sitX=x, sitZ=z, sitY=G, standX=round(stand[0], 3), standZ=round(stand[1], 3), eye=eye))


def block(bid, x0, y0, z0, x1, y1, z1):
    """Extra collision boxes that are not walls/furniture (stair balustrade, loft roof)."""
    blocks.append(dict(id=bid, x0=x0, y0=y0, z0=z0, x1=x1, y1=y1, z1=z1))


def roof_y(x):
    """Top surface of the hall roof above building-local x (both slopes)."""
    return RIDGE - SLOPE * abs(x - RIDGE_X)


# ---------------------------------------------------------------------------
# Hall: roughstone walls. The gable walls run through; the long walls butt them.
# ---------------------------------------------------------------------------
hx0, hx1, hz0, hz1 = HALL
wall('H_S', 'x', hz0 + STONE_T / 2, hx0, hx1, STONE_T, FOOT, EAVES, 'int', 'ext', 'hall', buried=['a0'])
wall('H_N', 'x', hz1 - STONE_T / 2, hx0, hx1, STONE_T, FOOT, EAVES, 'ext', 'int', 'hall')
wall('H_E', 'z', hx1 - STONE_T / 2, hz0 + STONE_T, hz1 - STONE_T, STONE_T, FOOT, EAVES, 'ext', 'int', 'hall', buried=['a0', 'a1'])
# West wall: the office range leans on its lower part. Below the office roof void its west
# face is the office's cream plaster; above it, and behind the office, it is weathered stone.
wall('H_W_lo', 'z', hx0 + STONE_T / 2, hz0 + STONE_T, OFFICE[3], STONE_T, FOOT, 3.0, 'int', 'int', 'hall', buried=['a0', 'a1'],
     faceGroups={'neg': 'office'})
wall('H_W_hi', 'z', hx0 + STONE_T / 2, hz0 + STONE_T, OFFICE[3], STONE_T, 3.0, EAVES, 'int', 'ext', 'hall', buried=['a0', 'a1'])
wall('H_W_back', 'z', hx0 + STONE_T / 2, OFFICE[3], hz1 - STONE_T, STONE_T, FOOT, EAVES, 'int', 'ext', 'hall', buried=['a0', 'a1'])

# Office range (cream plaster; the west wall is the party wall against H01, raised as a parapet).
ox0, ox1, oz0, oz1 = OFFICE
wall('O_S', 'x', oz0 + STONE_T / 2, ox0, ox1, STONE_T, FOOT, 4.0, 'int', 'ext', 'office', buried=['a1'])
wall('O_N', 'x', oz1 - OFFICE_T / 2, ox0, ox1, OFFICE_T, FOOT, OFFICE_ROOF_LO, 'ext', 'int', 'office', buried=['a1'])
wall('O_W', 'z', ox0 + OFFICE_T / 2, oz0 + STONE_T, oz1 - OFFICE_T, OFFICE_T, FOOT, 3.85, 'int', 'none', 'office', buried=['a0', 'a1'])
wall('O_mid', 'x', 7.2, ox0 + OFFICE_T, ox1, INT_T, G, OFFICE_CEIL, 'int', 'int', 'office', buried=['a0', 'a1'])

# Back tool store under the loft; open timber balustrades on the loft edges.
wall('S_store_S', 'x', 14.8, hx0 + STONE_T, 11.08, INT_T, G, LOFT_UNDER, 'int', 'int', 'store', buried=['a0'])
wall('S_store_E', 'z', 11.0, 14.88, hz1 - STONE_T, INT_T, G, LOFT_UNDER, 'int', 'int', 'store', buried=['a0', 'a1'])
wall('L_rail_S', 'x', 14.76, 6.45, 11.08, .06, LOFT, LOFT + 1.0, 'int', 'int', 'rail', kind='rail')
wall('L_rail_E', 'z', 11.05, 14.79, hz1 - STONE_T, .06, LOFT, LOFT + 1.0, 'int', 'int', 'rail', kind='rail')

# Rear yard fence (boarded, 1.9 m) on the plot edges; the hall and H01's wing close the rest.
YF = 1.9
wall('Y_W', 'z', .13, hz1, 30.0, .06, -.1, YF, 'ext', 'ext', 'fence', kind='fence')
wall('Y_N', 'x', 29.97, .1, 17.75, .06, -.1, YF, 'ext', 'ext', 'fence', kind='fence')
wall('Y_E', 'z', 17.72, hz1, 29.94, .06, -.1, YF, 'ext', 'ext', 'fence', kind='fence')

# ---------------------------------------------------------------------------
# Openings
# ---------------------------------------------------------------------------
DORMERS_Z = (3.0, 8.6, 14.2)
opening('carriage', 'H_S', RIDGE_X, 3.6, G, G + 3.55, 'door', 'carriage', leaves=2, hinge='both', swing=-1,
        fixture='garage-carriage-door', label='车间拱门', exterior=True, startsOpen=True, rise=.55)
for i, z in enumerate(DORMERS_Z):
    opening(f'win-east-{i}', 'H_E', z, 1.0, G + 1.15, 4.25, 'window', 'iron', exterior=True, rise=.3)
opening('rear-door', 'H_N', 14.5, 1.0, G, G + 2.2, 'door', 'plank', leaves=1, hinge='a1', swing=1,
        fixture='garage-rear-door', label='车间后门', exterior=True)
opening('win-rear', 'H_N', 16.3, .9, G + 1.1, G + 2.4, 'window', 'iron', exterior=True, rise=.18)
opening('win-loft', 'H_N', 8.3, .9, 3.45, 4.35, 'window', 'iron', exterior=True, rise=.15)
opening('win-store', 'H_W_back', 16.6, .8, G + 1.1, G + 2.1, 'window', 'iron', exterior=True, rise=.12)
opening('office-door', 'O_S', 3.7, 1.0, G, G + 2.25, 'door', 'glazed', leaves=1, hinge='a1', swing=1,
        fixture='garage-office-door', label='接待室门', exterior=True)
opening('win-office', 'O_S', 1.6, 1.4, G + .85, G + 2.35, 'window', 'sash', exterior=True)
opening('win-records', 'O_N', 2.5, 1.0, G + 1.0, G + 2.1, 'window', 'sash', exterior=True)
opening('hall-door', 'H_W_lo', 2.0, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a0', swing=1,
        fixture='garage-hall-door', label='接待室侧门', startsOpen=True)
opening('viewing-window', 'H_W_lo', 5.0, 1.8, G + 1.0, G + 2.2, 'window', 'interior')
opening('records-door', 'O_mid', 2.6, .85, G, G + 2.1, 'door', 'panel', leaves=1, hinge='a0', swing=1,
        fixture='garage-records-door', label='账房门', startsOpen=True)
opening('store-door', 'S_store_S', 9.4, .9, G, G + 2.1, 'door', 'plank', leaves=1, hinge='a1', swing=1,
        fixture='garage-store-door', label='工具库门', startsOpen=True)
# The gate opening runs to the fence top: a lintel at 1.8 m would stop a 1.78 m capsule standing on the paving.
opening('yard-gate', 'Y_N', 14.5, 1.3, YARD, YF, 'door', 'boards', leaves=1, hinge='a0', swing=-1,
        fixture='garage-yard-gate', label='后院木栅门', exterior=True)
# The front gable's high window lights the open roof space of the hall (no extra storey).
GABLE_WINDOW = dict(id='win-gable', at=RIDGE_X, width=1.0, bottom=5.0, top=6.5, rise=.25, wall='gable-S')

# ---------------------------------------------------------------------------
# Rooms
# ---------------------------------------------------------------------------
room('office', '接待室', 0, ox0 + OFFICE_T, oz0 + STONE_T, ox1, 7.12, 'oak', .60)
room('records', '账房', 0, ox0 + OFFICE_T, 7.28, ox1, oz1 - OFFICE_T, 'oak', .35)
room('hall', '修理车间', 0, hx0 + STONE_T, hz0 + STONE_T, hx1 - STONE_T, 14.72, 'cement', .55)
room('back', '车间后场', 0, 11.08, 14.72, hx1 - STONE_T, hz1 - STONE_T, 'cement', .35)
room('store', '工具库', 0, hx0 + STONE_T, 14.88, 10.92, hz1 - STONE_T, 'boards', .25)
room('loft', '储物阁楼', 1, hx0 + STONE_T, 14.72, 11.08, hz1 - STONE_T, 'boards', .30)

# ---------------------------------------------------------------------------
# Slabs: floor finishes, ceilings, loft and the exterior aprons/yard owned by the prefab.
# ---------------------------------------------------------------------------
def slab(sid, x0, z0, x1, z1, y0, y1, top, bottom, kind):
    slabs.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, y0=y0, y1=y1, top=top, bottom=bottom, kind=kind))


for r in rooms:
    if r['level'] == 0:
        slab('floor-' + r['id'], r['x0'], r['z0'], r['x1'], r['z1'], G - .02, G, r['floor'], 'none', 'finish')
slab('ceil-office', ox0 + OFFICE_T, oz0 + STONE_T, ox1, oz1 - OFFICE_T, OFFICE_CEIL, OFFICE_CEIL + .1, 'none', 'plaster', 'ceiling')
slab('loft', hx0 + STONE_T, 14.72, 11.08, hz1 - STONE_T, LOFT_UNDER, LOFT, 'boards', 'joists', 'loft')
# Front flags along the whole street face (the town surfaces stop at this strip), and a raised
# stone apron before the vehicle arch.
slab('front-paving', ox0, -0.7, hx1, 0.0, -.12, .10, 'flags', 'none', 'paving')
slab('apron', 9.3, -0.7, 13.3, 0.0, -.12, .13, 'stone', 'none', 'apron')
slab('rear-step', 13.9, hz1, 15.1, hz1 + .45, -.12, G, 'stone', 'none', 'step')
slab('yard-side', .1, oz1, hx0, hz1, -.12, YARD, 'cobble', 'none', 'yard')
slab('yard-back', .1, hz1, 17.75, 30.0, -.12, YARD, 'cobble', 'none', 'yard')

# ---------------------------------------------------------------------------
# Stair: fifteen 0.173 m risers along the west wall up to the loft edge (solid to the floor).
# ---------------------------------------------------------------------------
RISE = (LOFT - G) / 15
TREAD = .25
STAIR_X0, STAIR_X1, STAIR_Z0 = hx0 + STONE_T, 6.45, 14.72 - TREAD * 14
for k in range(1, 15):
    stairs.append(dict(id=f'S{k}', x0=STAIR_X0, x1=STAIR_X1, z0=round(STAIR_Z0 + TREAD * (k - 1), 3),
                       z1=round(STAIR_Z0 + TREAD * k, 3), top=round(G + RISE * k, 3)))
# Balustrade on the open (east) side of the flight, stepped boxes so the treads stay free.
for s in stairs:
    block('stair-rail-' + s['id'], STAIR_X1, s['top'], s['z0'], STAIR_X1 + .06, s['top'] + .95, s['z1'])

# ---------------------------------------------------------------------------
# Roofs, trusses, dormers and shed posts.
# ---------------------------------------------------------------------------
roofs.append(dict(id='roof-hall', kind='gable', x0=hx0 - .35, z0=hz0 - .3, x1=hx1 + .35, z1=hz1 + .3,
                  eaves=EAVES, ridge=RIDGE, axis='z', low=0, ridgeX=RIDGE_X))
roofs.append(dict(id='roof-office', kind='flatlean', x0=ox0, z0=oz0 + STONE_T, x1=ox1, z1=oz1 + .3,
                  eaves=OFFICE_ROOF_LO, ridge=OFFICE_ROOF_HI, axis='z', low=1, ridgeX=0))
roofs.append(dict(id='roof-shed', kind='leanto', x0=hx0, z0=hz1, x1=10.7, z1=23.0, eaves=2.45, ridge=3.15,
                  axis='z', low=1, ridgeX=0))
roofs.append(dict(id='roof-awning', kind='leanto', x0=.55, z0=-1.0, x1=4.65, z1=0.0, eaves=2.62, ridge=2.85,
                  axis='z', low=-1, ridgeX=0))
DORMER_HALF, DORMER_APEX = 1.2, 6.2
for i, z in enumerate(DORMERS_Z):
    valley_x = RIDGE_X + (RIDGE - DORMER_APEX) / SLOPE
    dormers.append(dict(id=f'dormer-{i}', z=z, half=DORMER_HALF, apex=DORMER_APEX, face=hx1, valleyX=round(valley_x, 4),
                        front=hx1 + .3))
for i, z in enumerate((5.9, 11.3)):
    trusses.append(dict(id=f'truss-{i}', z=z, tieY0=4.3, tieY1=4.55, w=.24))
for i, x in enumerate((5.25, 7.95, 10.5)):
    posts.append(dict(id=f'shed-post-{i}', x=x, z=22.75, w=.16, y0=YARD, y1=2.39))

# ---------------------------------------------------------------------------
# Furniture (front of a piece faces f=(sin yaw, cos yaw): user/seat/drawer side).
# ---------------------------------------------------------------------------
# Customer office
furn('counter', 'counter', 1.75, 3.4, 180, 2.8, .6, 1.0, room='office')
furn('office-desk', 'desk', 4.65, 5.0, -90, 1.3, .62, .76, room='office')
furn('office-chair', 'chair', 3.95, 5.0, 90, .46, .46, .9, room='office')
furn('office-pigeon', 'pigeonhole', .53, 5.4, 90, 2.0, .35, 2.0, room='office')
furn('office-stove', 'stove', .82, 6.7, 90, .5, .5, .85, room='office')
furn('office-bench', 'bench', .6, 1.75, 90, 1.5, .45, .46, room='office')
furn('office-coat', 'coatstand', 4.6, 6.75, -90, .42, .42, 1.8, room='office')
furn('office-board', 'noticeboard', .37, 3.4, 90, .9, .03, .7, y=G + 1.25, room='office', collide=False)
furn('office-register', 'ledger', 1.2, 3.38, 180, .34, .26, .05, y=G + 1.0, room='office', collide=False)
furn('office-phone', 'deskphone', 2.75, 3.45, 180, .22, .2, .22, y=G + 1.0, room='office', collide=False)
furn('office-bell', 'bell', 2.2, 3.28, 0, .1, .1, .08, y=G + 1.0, room='office', collide=False)
furn('office-desklamp', 'desklamp', 4.8, 4.55, 0, .3, .3, .48, y=G + .76, room='office', collide=False)
# Records room (账房)
furn('rec-desk', 'desk', 2.6, 11.3, 180, 1.4, .65, .76, room='records')
furn('rec-chair', 'chair', 2.6, 10.55, 0, .46, .46, .9, room='records')
furn('rec-files', 'filingcabinet', .65, 9.0, 90, .5, .6, 1.3, room='records')
furn('rec-shelf', 'bookshelf', 4.82, 9.4, -90, 2.2, .35, 2.0, room='records')
furn('rec-tray', 'tray', 2.1, 11.32, 180, .34, .26, .05, y=G + .76, room='records', collide=False)
furn('rec-desklamp', 'desklamp', 3.15, 11.4, 0, .3, .3, .48, y=G + .76, room='records', collide=False)
# Repair hall
furn('truck', 'truck', 13.4, 7.2, 180, 1.75, 4.3, 1.95, room='hall')
furn('hoist', 'hoist', 13.4, 5.9, 0, .4, .4, 2.3, y=2.0, room='hall', collide=False)
furn('bench-a', 'workbench', 16.7, 3.0, -90, 2.4, .7, .9, room='hall')
furn('bench-b', 'workbench', 16.7, 8.6, -90, 2.4, .7, .9, room='hall')
furn('bench-c', 'workbench', 16.7, 13.75, -90, 1.5, .7, .9, room='hall')
furn('vise', 'vise', 16.55, 8.05, -90, .2, .4, .22, y=G + .9, room='hall', collide=False)
furn('toolboard', 'toolboard', 17.03, 5.8, -90, 2.3, .04, 1.1, y=G + 1.15, room='hall', collide=False)
furn('tool-cabinet', 'lowcabinet', 16.8, 5.8, -90, 2.0, .48, .85, room='hall')
furn('motorcycle', 'motorcycle', 15.95, 11.25, 0, .7, 2.0, 1.1, room='hall')
furn('hall-pigeon', 'pigeonhole', 5.76, 8.7, 90, 3.0, .4, 2.2, room='hall')
furn('anvil', 'anvil', 7.3, 3.6, 0, .5, .35, .8, room='hall')
furn('grinder', 'grinder', 7.0, 6.2, 90, .4, .4, 1.1, room='hall')
furn('jack', 'jack', 10.9, 9.8, 30, .35, .9, .25, room='hall', collide=False)
furn('rag-bin', 'ragbin', 12.0, 13.9, 0, .5, .5, .62, room='hall')
furn('crate-hall', 'crate', 7.6, 13.6, 10, .8, .6, .6, room='hall')
furn('tag-board', 'tagboard', 7.6, 14.71, 180, .9, .03, .6, y=G + 1.3, room='hall', collide=False)
furn('oilcans', 'oilcans', 16.75, 2.5, 0, .5, .3, .3, y=G + .9, room='hall', collide=False)
# Back of the hall
furn('drum-1', 'drum', 15.95, 16.95, 0, .6, .6, .9, room='back')
furn('drum-2', 'drum', 16.55, 17.85, 0, .6, .6, .9, room='back')
furn('tyres', 'tyres', 16.55, 15.6, 0, .8, .8, .9, room='back')
furn('parts-cabinet', 'drawercabinet', 12.3, 18.18, 180, 1.2, .5, 1.25, room='back')
# Tool store under the loft
furn('store-shelf', 'toolshelf', 8.2, 18.25, 180, 3.0, .4, 2.1, room='store')
furn('store-crate', 'crate', 6.2, 15.4, 0, .7, .6, .5, room='store')
furn('store-chest', 'chest', 10.4, 16.6, -90, .9, .5, .55, room='store')
# Loft
furn('loft-crates', 'crate', 7.0, 17.7, 0, 1.0, .8, .7, y=LOFT, room='loft')
furn('loft-seat', 'carseat', 9.7, 17.85, 180, 1.1, .5, .8, y=LOFT, room='loft')
furn('loft-wheels', 'wheels', 6.3, 16.0, 90, .7, .7, .75, y=LOFT, room='loft')
# Front of the office and the rear yard (exterior props on the prefab's own paving)
furn('front-bench', 'bench', 1.6, -0.36, 180, 1.3, .4, .46, y=.10)
furn('front-barrel', 'barrel', .45, -0.38, 0, .5, .5, .8, y=.10)
furn('front-board', 'chalkboard', 4.75, -0.45, 160, .55, .3, 1.0, y=.10)
furn('hang-sign', 'hangsign', .45, 0.0, 0, .7, 1.0, .6, y=2.55, collide=False)
furn('shed-rack', 'tyrerack', 6.3, 20.4, 90, 2.0, .7, 1.6, y=YARD)
furn('shed-timber', 'timber', 9.0, 19.6, 0, 2.6, .5, .5, y=YARD)
furn('yard-barrels', 'drum', 2.0, 22.4, 0, .6, .6, .9, y=YARD)
furn('yard-trough', 'trough', 1.1, 26.0, 90, 1.6, .55, .6, y=YARD)
furn('yard-cart', 'handcart', 4.6, 27.8, 70, .9, 1.6, .9, y=YARD)
furn('yard-crates', 'crate', 16.6, 24.0, 0, .9, .7, .7, y=YARD)

# ---------------------------------------------------------------------------
# Lamps. Interior lights affect only the garage interior rendering layer.
# ---------------------------------------------------------------------------
lamp('lamp-office', 'pendant', 2.6, OFFICE_CEIL - .55, 3.9, 'office', intensity=2.8, rng=5.0, access=.6, anchor=OFFICE_CEIL)
lamp('lamp-office-desk', 'desk', 4.8, G + 1.13, 4.55, 'office', intensity=.8, rng=2.2, access=.55, switch='garage-office-lamp-off')
lamp('lamp-records', 'pendant', 2.6, OFFICE_CEIL - .55, 9.4, 'records', intensity=2.4, rng=4.5, access=.35, anchor=OFFICE_CEIL)
lamp('lamp-records-desk', 'desk', 3.15, G + 1.13, 11.4, 'records', intensity=.8, rng=2.2, access=.4, switch='garage-records-lamp-off')
for i, (x, z) in enumerate(((8.4, 5.9), (14.2, 5.9), (8.4, 11.3), (14.2, 11.3))):
    lamp(f'lamp-hall-{i}', 'pendant', x, 3.35, z, 'hall', intensity=3.4, rng=7.0, access=.5, anchor=4.3)
lamp('lamp-bench-a', 'bracket', 16.85, G + 1.95, 2.1, 'hall', intensity=1.1, rng=2.8, access=.45)
lamp('lamp-bench-b', 'bracket', 16.85, G + 1.95, 10.15, 'hall', intensity=1.1, rng=2.8, access=.45)
lamp('lamp-back', 'pendant', 14.2, 3.3, 16.6, 'back', intensity=2.4, rng=5.5, access=.35, anchor=round(roof_y(14.2) - ROOF_T, 3))
lamp('lamp-store', 'bulb', 8.2, LOFT_UNDER - .35, 16.6, 'store', intensity=1.4, rng=4.0, access=.25, anchor=LOFT_UNDER)
lamp('lamp-loft', 'bulb', 8.6, LOFT + 1.75, 16.8, 'loft', intensity=1.4, rng=4.0, access=.3, anchor=round(roof_y(8.6) - ROOF_T, 3))
# Exterior lanterns (photocell; share the twelve street pools).
lamp('lantern-office', 'wall', 4.72, G + 2.15, -0.2, 'street', role='Exterior', intensity=2.8, rng=6.5, access=.85)
lamp('lantern-arch', 'wall', RIDGE_X, 4.05, -0.22, 'street', role='Exterior', intensity=3.0, rng=7.5, access=.85)
lamp('lantern-rear', 'wall', 15.45, G + 2.35, hz1 + .2, 'yard', role='Exterior', intensity=2.0, rng=6.0, access=.85)

# ---------------------------------------------------------------------------
# Interaction points (Unity trigger boxes). Texts live in Resources/GarageDocuments.json.
# ---------------------------------------------------------------------------
CT = G + 1.0
DT = G + .76
item('register', 'document', 1.2, CT + .04, 3.38, .38, .1, .3, doc='register')
item('office-phone', 'document', 2.75, CT + .12, 3.45, .26, .26, .24, doc='office-phone')
item('office-board', 'document', .4, G + 1.6, 3.4, .08, .7, .9, doc='price-board')
item('office-pigeon', 'document', .62, G + 1.3, 5.4, .3, 1.1, 1.8, doc='office-pigeonholes')
item('kane-envelope', 'document', 2.1, DT + .04, 11.32, .38, .08, .3, doc='kane-envelope')
item('w01', 'evidence', 2.68, DT + .02, 11.3, .36, .06, .25, doc='w01', evidence='W01')
item('w02', 'evidence', 3.05, DT + .02, 11.12, .36, .06, .25, doc='w02', evidence='W02')
item('rec-ledgers', 'document', 4.68, G + 1.2, 9.4, .3, 1.2, 2.0, doc='old-registers')
item('truck', 'document', 13.4, G + 1.0, 7.2, 1.8, 1.8, 4.3, doc='work-truck')
item('hoist', 'document', 13.4, 2.9, 5.9, .5, 1.6, .5, doc='chain-hoist')
item('motorcycle', 'document', 15.95, G + .6, 11.25, .8, 1.0, 2.1, doc='motorcycle')
item('vise', 'document', 16.55, G + 1.02, 8.05, .3, .26, .45, doc='bench-vise')
item('toolboard', 'document', 16.98, G + 1.7, 5.8, .1, 1.1, 2.3, doc='tool-wall')
item('tag-board', 'document', 7.6, G + 1.6, 14.66, .9, .6, .1, doc='tag-board')
item('rag-bin', 'document', 12.0, G + .45, 13.9, .55, .5, .55, doc='oil-rags')
item('tyres', 'document', 16.55, G + .5, 15.6, .9, .9, .9, doc='tyres')
item('store-shelf', 'document', 8.2, G + 1.2, 18.0, 3.0, 1.8, .2, doc='tool-store')
item('loft-crates', 'document', 7.0, LOFT + .45, 17.7, 1.1, .8, .9, doc='loft-crates')
item('shed-rack', 'document', 6.3, YARD + .8, 20.4, .8, 1.5, 2.1, doc='shed-rack')
# Inspection triggers must stand proud of the furniture collider they label: a coincident face
# makes the E ray hit the solid box first on some frames (seen at 720p on the shed rack).
# Parts cabinet: the bottom-right drawer pulls out; its card is readable only when open.
item('drawer-0', 'drawer', 12.3, G + .2, 17.9, .3, .2, .3, doc='tag-blanks', title='零件抽屉', target='garage-parts-drawer')
# Lamp switches
item('switch-office', 'lampswitch', 4.8, DT + .12, 4.55, .26, .26, .26, title='接待桌台灯', target='lamp-office-desk')
item('switch-records', 'lampswitch', 3.15, DT + .12, 11.4, .26, .26, .26, title='账房台灯', target='lamp-records-desk')
# The mechanic stands between his truck and the bench, facing the arch (talk target).
item('mechanic', 'npc', 15.35, G, 7.9, .7, 1.8, .5, title='修车匠', yaw=200)
seat('garage-seat-bench', '接待室长凳', .6, 1.75, 90, eye=1.1, w=1.3)
seat('garage-seat-office', '接待桌椅', 3.95, 5.0, 90, behind=True)
seat('garage-seat-records', '账房椅', 2.6, 10.55, 0, behind=True)

# ---------------------------------------------------------------------------
# Weather shelters: rain clipping + one acoustic enclosure for the whole building.
# ---------------------------------------------------------------------------
def shelter(sid, x0, z0, x1, z1, eaves, rise, ridgeAlongX, doors, open_=False):
    shelters.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, eaves=eaves, rise=rise, ridgeAlongX=ridgeAlongX,
                         doors=doors, open=open_))


GARAGE_DOORS = ['garage-carriage-door', 'garage-office-door', 'garage-rear-door']
shelter('hall', hx0 - .35, hz0 - .3, hx1 + .35, hz1 + .3, EAVES, RIDGE - EAVES, False, GARAGE_DOORS)
shelter('office', ox0, oz0, ox1, oz1 + .3, OFFICE_ROOF_LO, 0, True, GARAGE_DOORS)
shelter('shed', hx0, hz1, 10.7, 23.0, 2.45, 0, True, [], True)
shelter('awning', .55, -1.0, 4.65, 0.0, 2.62, 0, True, [], True)

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
    if o['rise'] and (o['rise'] * 2 > o['width'] or o['rise'] >= o['top'] - o['bottom']):
        errors.append(f"arch rise of {o['id']} does not fit")
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
R = {r['id']: r for r in rooms}


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
# Colliding furniture must not overlap each other on the same level.
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
    if o['kind'] not in ('door', 'arch'):
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
        for s in stairs:
            if min(s['x1'], bx1) - max(s['x0'], bx0) > .02 and min(s['z1'], bz1) - max(s['z0'], bz0) > .02:
                errors.append(f"stair {s['id']} blocks {o['id']}")
for o in openings:
    if o['kind'] == 'door' and o['top'] - o['bottom'] < 1.95 and o['top'] < W[o['wall']]['y1'] - 1e-6:
        errors.append(f"door {o['id']} head too low for the 1.78 m player capsule")
for it in items:
    if it['kind'] != 'document':
        continue
    for f in furniture:
        if not f['collide'] or min(f['y'] + f['h'], it['y'] + it['h'] / 2) - max(f['y'], it['y'] - it['h'] / 2) < .05:
            continue
        fx0, fz0, fx1, fz1 = footprint(f)
        ix0, ix1, iz0, iz1 = it['x'] - it['w'] / 2, it['x'] + it['w'] / 2, it['z'] - it['d'] / 2, it['z'] + it['d'] / 2
        if min(fx1, ix1) - max(fx0, ix0) > .05 and min(fz1, iz1) - max(fz0, iz0) > .05:
            if any(abs(a_ - b_) < .02 for a_, b_ in ((fx0, ix0), (fx1, ix1), (fz0, iz0), (fz1, iz1))) and not (ix0 < fx0 - .02 or ix1 > fx1 + .02 or iz0 < fz0 - .02 or iz1 > fz1 + .02):
                errors.append(f"trigger {it['id']} shares a face with collider {f['id']}")
# Stair: the flight must arrive exactly at the loft edge, clear of furniture.
if abs(stairs[-1]['z1'] - 14.72) > .01 or abs(G + RISE * 15 - LOFT) > .002:
    errors.append('stair does not meet the loft edge')
for s in stairs:
    for f in furniture:
        if not f['collide'] or f['y'] > 1:
            continue
        x0, z0, x1, z1 = footprint(f)
        if min(x1, s['x1'] + .5) - max(x0, s['x0']) > .02 and min(z1, s['z1']) - max(z0, s['z0'] - .9) > .02:
            errors.append(f"furniture {f['id']} crowds the stair at {s['id']}")
# Loft headroom under the roof sarking and under the stair-side truss for a 1.78 m capsule.
if roof_y(hx0 + STONE_T) - ROOF_T - LOFT < 1.95:
    errors.append('loft headroom under the low west roof is below 1.95 m')
for t in trusses:
    for s in stairs:
        if s['z0'] - .3 < t['z'] < s['z1'] + .3 and s['top'] + 1.95 > t['tieY0']:
            errors.append(f"truss {t['id']} tie beam too low over the stair")
    if 14.72 - .4 < t['z'] < hz1:
        errors.append(f"truss {t['id']} over the loft")
# Dormer holes must clear the trusses and purlins (east purlin at x 14.2).
for d in dormers:
    for t in trusses:
        if abs(t['z'] - d['z']) < d['half'] + .3:
            errors.append(f"truss {t['id']} crosses {d['id']}")
    if d['valleyX'] < 14.2 + .25:
        errors.append(f"{d['id']} valley cuts the east purlin")
    win = next(o for o in openings if o['wall'] == 'H_E' and abs(o['at'] - d['z']) < .01)
    if win['top'] > EAVES - .25:
        errors.append(f"window under {d['id']} rises into the wall head")

# Agreement with the town map (B07 parts, doors, windows) in world coordinates.
plan = json.loads((ROOT / 'docs/maps/town-current/plan-data.json').read_text(encoding='utf-8'))
b07 = next(b for b in plan['buildings'] if b['id'] == 'B07')
parts = sorted(tuple(round(v, 3) for v in p['rect']) for p in b07['parts'])


def world_rect(x0, x1, z0, z1):
    return (round(ORIGIN[0] + (x0 + x1) / 2, 3), round(ORIGIN[2] + (z0 + z1) / 2, 3), round(x1 - x0, 3), round(z1 - z0, 3))


expect = sorted([world_rect(ox0, ox1, oz0, oz1), world_rect(hx0, hx1, hz0, hz1)])
if parts != expect:
    errors.append(f'plan-data B07 parts differ: {parts} != {expect}')
od = next(o for o in openings if o['id'] == 'office-door')
rd = next(o for o in openings if o['id'] == 'rear-door')
if [round(od['x'] + ORIGIN[0], 3), round(oz0 + ORIGIN[2], 3)] != [round(v, 3) for v in b07['door']]:
    errors.append(f"office door differs from plan-data door {b07['door']}")
if [round(rd['x'] + ORIGIN[0], 3), round(hz1 + ORIGIN[2], 3)] != [round(v, 3) for v in b07['rearDoor']]:
    errors.append(f"rear door differs from plan-data rearDoor {b07['rearDoor']}")
side_of = {'H_S': 'S', 'H_N': 'N', 'H_E': 'E', 'H_W_back': 'W', 'O_S': 'S', 'O_N': 'N'}


def outer(w):
    side = side_of[w['id']]
    return w['c'] + (w['t'] / 2 if side in ('E', 'N') else -w['t'] / 2)


ext_windows = []
for o in openings:
    if o['kind'] != 'window' or not o['exterior']:
        continue
    w = W[o['wall']]
    if w['axis'] == 'x':
        ext_windows.append((round(o['x'] + ORIGIN[0], 2), round(outer(w) + ORIGIN[2], 2), o['width'], side_of[w['id']]))
    else:
        ext_windows.append((round(outer(w) + ORIGIN[0], 2), round(o['z'] + ORIGIN[2], 2), o['width'], side_of[w['id']]))
ext_windows.append((round(GABLE_WINDOW['at'] + ORIGIN[0], 2), round(hz0 + ORIGIN[2], 2), GABLE_WINDOW['width'], 'S'))
plan_windows = sorted((round(w[0], 2), round(w[1], 2), w[2], w[4]) for w in b07['windows'])
if sorted(ext_windows) != plan_windows:
    errors.append(f'exterior windows differ from plan-data: {sorted(set(ext_windows) ^ set(plan_windows))}')
if b07['storeys'] != 1:
    errors.append('plan-data B07 must be a single storey (the loft lies under the hall roof)')

fin = [s for s in slabs if s['kind'] == 'finish']
for i, a in enumerate(fin):
    for b in fin[i + 1:]:
        if abs(a['y1'] - b['y1']) < .01 and min(a['x1'], b['x1']) - max(a['x0'], b['x0']) > .001 and min(a['z1'], b['z1']) - max(a['z0'], b['z0']) > .001:
            errors.append(f"floor finishes overlap (z-fighting): {a['id']} {b['id']}")
if errors:
    sys.exit('GARAGE_LAYOUT_FAIL:\n' + '\n'.join(errors))

data = dict(version=1, id='B07', name='青川修车铺', scheme='E 旧石仓修车院', origin=list(ORIGIN),
            levels=dict(ground=G, eaves=EAVES, ridge=RIDGE, ridgeX=RIDGE_X, slope=SLOPE, roofThickness=ROOF_T,
                        officeCeiling=OFFICE_CEIL, officeRoofHigh=OFFICE_ROOF_HI, officeRoofLow=OFFICE_ROOF_LO,
                        loft=LOFT, loftUnder=LOFT_UNDER, yard=YARD, foot=FOOT),
            hall=dict(x0=hx0, x1=hx1, z0=hz0, z1=hz1), office=dict(x0=ox0, x1=ox1, z0=oz0, z1=oz1),
            walls=walls, openings=openings, gableWindow=GABLE_WINDOW, rooms=rooms, slabs=slabs, stairs=stairs,
            blocks=blocks, roofs=roofs, dormers=dormers, trusses=trusses, posts=posts, furniture=furniture,
            lamps=lamps, items=items, shelters=shelters,
            notes='Unity building-local metres (+X east, +Y up, +Z north); root at world origin above; yaw degrees, front faces (sin yaw, cos yaw).')
text = json.dumps(data, ensure_ascii=False, indent=1) + '\n'
out = ROOT / 'assets/source/garage'
out.mkdir(parents=True, exist_ok=True)
for path in (out / 'garage-layout.json', ROOT / 'game/Assets/Resources/GarageLayout.json'):
    path.write_bytes(text.encode('utf-8'))
print(json.dumps(dict(status='GARAGE_LAYOUT_PASS', walls=len(walls), openings=len(openings),
                      doors=sum(o['leaves'] > 0 for o in openings), windows=sum(o['kind'] == 'window' for o in openings) + 1,
                      rooms=len(rooms), furniture=len(furniture), lamps=len(lamps), items=len(items),
                      shelters=len(shelters), stairSteps=len(stairs), dormers=len(dormers)), ensure_ascii=False))
