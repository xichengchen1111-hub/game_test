"""Shared layout contract for the Qingchuan cottage hospital (B11).

One source for the Blender shell/furniture generator (tools/hospital/build_hospital.py)
and the Unity assembler (BuildSlice.Hospital). Same schema as the police contract
(tools/police/police_layout.py), so Unity reads it with the shared PoliceLayout classes.
Coordinates are Unity building-local metres: +X east, +Y up, +Z north; the prefab root
sits at world (80, 0, -65) with no rotation, so plan-data world X = 80 + x and Z = -65 + z.

Single storey with 3.8 m rooms (period hospitals ran 12-14 ft ceilings): a 42 x 12 m front
range (entrance hall and enquiry desk, waiting room, consulting room, dispensary, office,
matron, theatre, and a 3 m corridor along the court side), a west pavilion ward for
children and an east pavilion ward for adults, each 8 x 18 m with beds between the
windows of both long walls, around a court open to the north.

Run from the repository root (stdlib only):
    python3 tools/hospital/hospital_layout.py
Writes assets/source/hospital/hospital-layout.json and its Unity copy
game/Assets/Resources/HospitalLayout.json, after validating the geometry and checking
that docs/maps/town-current/plan-data.json B11 matches.
"""
import json, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ORIGIN = (80.0, 0.0, -65.0)

G = 0.15            # floor finish (town walk is .10)
G_CEIL = 3.95       # ceiling underside
EAVES = 4.35
RIDGE = 6.40        # every ridge: the pavilions, the range between them and the entrance gable
COURT = -0.025      # the court is the town's own yard surface
EXT_T = 0.4
INT_T = 0.16
FOOT = -0.25
NONE = 99.0         # no upper floor

walls, openings, rooms, slabs, roofs = [], [], [], [], []
furniture, lamps, items, shelters, stairs, columns, beams = [], [], [], [], [], [], []


def wall(wid, axis, c, a0, a1, t, y0, y1, pos, neg, level):
    """axis 'x': runs along X at constant z=c; axis 'z': along Z at constant x=c.
    pos/neg: finish of the face whose normal points to +c / -c: 'ext', 'int' or 'none'."""
    walls.append(dict(id=wid, axis=axis, c=c, a0=a0, a1=a1, t=t, y0=y0, y1=y1, pos=pos, neg=neg, level=level))


def opening(oid, wid, at, width, bottom, top, kind, style, leaves=0, hinge='a0', swing=1, fixture='', label='',
            exterior=False, startsOpen=False, enclosure=''):
    """at: centre along the wall axis. swing: +1/-1, side of the wall the leaf turns into."""
    openings.append(dict(id=oid, wall=wid, at=at, width=width, bottom=bottom, top=top, kind=kind, style=style,
                         leaves=leaves, hinge=hinge, swing=swing, fixture=fixture, label=label,
                         exterior=exterior, startsOpen=startsOpen, enclosure=enclosure))


def room(rid, name, x0, z0, x1, z1, floor, access, enclosure, public=True, dado='green'):
    rooms.append(dict(id=rid, name=name, level=0, x0=x0, z0=z0, x1=x1, z1=z1, floor=floor,
                      access=access, enclosure=enclosure, public=public, dado=dado))


def furn(fid, kind, x, z, yaw, w, d, h, y=None, room='', collide=True, extra=''):
    furniture.append(dict(id=fid, kind=kind, x=x, y=G if y is None else y, z=z, yaw=yaw, w=w, d=d, h=h,
                          room=room, collide=collide, extra=extra))


def lamp(lid, kind, x, y, z, room, role='Interior', intensity=3.0, rng=5.0, access=.8, switch='', shade=''):
    lamps.append(dict(id=lid, kind=kind, x=x, y=y, z=z, room=room, role=role, intensity=intensity,
                      range=rng, access=access, switchId=switch, shade=shade))


def item(iid, kind, x, y, z, w, h, d, doc='', evidence='', title='', target='', yaw=0, seat=None):
    entry = dict(id=iid, kind=kind, x=x, y=y, z=z, w=w, h=h, d=d, doc=doc, evidence=evidence, title=title,
                 target=target, yaw=yaw, sitX=0, sitZ=0, standX=0, standZ=0, eye=1.15, sitY=0)
    if seat:
        entry.update(seat)
    items.append(entry)


def seat(sid, caption, x, z, yaw, depth=.45, eye=1.12, height=.46, standOff=.55, w=.5, behind=False):
    """behind: a chair drawn up to a table; the player stands up behind it."""
    f = (math.sin(math.radians(yaw)), math.cos(math.radians(yaw)))
    k = -1 if behind else 1
    stand = (x + k * f[0] * (depth / 2 + standOff), z + k * f[1] * (depth / 2 + standOff))
    item(sid, 'seat', x, G + height, z, w, .17, depth, title=caption, yaw=yaw,
         seat=dict(sitX=x, sitZ=z, sitY=G, standX=round(stand[0], 3), standZ=round(stand[1], 3), eye=eye))


# ---------------------------------------------------------------------------
# Exterior walls. Through walls run the full span; abutting walls stop at faces.
# ---------------------------------------------------------------------------
wall('F_S', 'x', 0.2, 0, 42, EXT_T, FOOT, EAVES, 'int', 'ext', 'front')
wall('F_W', 'z', 0.2, 0.4, 11.6, EXT_T, FOOT, EAVES, 'int', 'ext', 'front')
wall('F_E', 'z', 41.8, 0.4, 11.6, EXT_T, FOOT, EAVES, 'ext', 'int', 'front')
# North wall of the front range: the wards stand behind its west and east ends.
wall('F_N_W', 'x', 11.8, 0, 8, EXT_T, FOOT, EAVES, 'int', 'int', 'front')
wall('F_N_C', 'x', 11.8, 8, 34, EXT_T, FOOT, EAVES, 'ext', 'int', 'front')
wall('F_N_E', 'x', 11.8, 34, 42, EXT_T, FOOT, EAVES, 'int', 'int', 'front')
wall('WW_W', 'z', 0.2, 12, 29.6, EXT_T, FOOT, EAVES, 'int', 'ext', 'west')
wall('WW_E', 'z', 7.8, 12, 29.6, EXT_T, FOOT, EAVES, 'ext', 'int', 'west')
wall('WW_N', 'x', 29.8, 0, 8, EXT_T, FOOT, EAVES, 'ext', 'int', 'west')
wall('EW_W', 'z', 34.2, 12, 29.6, EXT_T, FOOT, EAVES, 'int', 'ext', 'east')
wall('EW_E', 'z', 41.8, 12, 29.6, EXT_T, FOOT, EAVES, 'ext', 'int', 'east')
wall('EW_N', 'x', 29.8, 34, 42, EXT_T, FOOT, EAVES, 'ext', 'int', 'east')

# Tall sashes: 12-14 ft rooms were lit and aired by windows reaching close to the ceiling.
WIN = (G + .95, G + 3.05)
WARD_Z = (15.05, 17.8, 20.6, 23.4, 26.2)      # a window between every pair of beds
opening('front-door', 'F_S', 21.0, 1.8, G, G + 3.0, 'door', 'front', leaves=2, hinge='both', swing=1,
        fixture='hospital-front-door', label='医院正门', exterior=True, enclosure='front')
FRONT_WINDOWS = ((3.2, '诊室'), (8.5, '药房'), (12.6, '候诊室'), (15.4, '候诊室'), (18.4, '门厅'), (23.6, '门厅'),
                 (27.5, '事务室'), (32.5, '护士长室'), (36.8, '手术室'), (39.9, '手术室'))
for i, (x, _) in enumerate(FRONT_WINDOWS):
    opening(f'win-front-{i}', 'F_S', x, 1.2, *WIN, 'window', 'sash', exterior=True)
opening('win-west-consult', 'F_W', 4.4, 1.2, *WIN, 'window', 'sash', exterior=True)
opening('win-west-corridor', 'F_W', 10.1, 1.1, *WIN, 'window', 'sash', exterior=True)
opening('win-east-theatre', 'F_E', 4.4, 1.2, *WIN, 'window', 'sash', exterior=True)
opening('win-east-corridor', 'F_E', 10.1, 1.1, *WIN, 'window', 'sash', exterior=True)
opening('court-door', 'F_N_C', 21.0, 1.1, G, G + 2.3, 'door', 'glazed', leaves=1, hinge='a0', swing=-1,
        fixture='hospital-court-door', label='通往庭院的门', exterior=True, enclosure='front')
COURT_WINDOWS = (10.5, 13.5, 16.5, 25.5, 28.5, 31.5)
for i, x in enumerate(COURT_WINDOWS):
    opening(f'win-court-{i}', 'F_N_C', x, 1.2, *WIN, 'window', 'sash', exterior=True)
for i, z in enumerate(WARD_Z):
    opening(f'win-cw-west-{i}', 'WW_W', z, 1.2, *WIN, 'window', 'sash', exterior=True)
    opening(f'win-cw-court-{i}', 'WW_E', z, 1.2, *WIN, 'window', 'sash', exterior=True)
    opening(f'win-gw-court-{i}', 'EW_W', z, 1.2, *WIN, 'window', 'sash', exterior=True)
    opening(f'win-gw-east-{i}', 'EW_E', z, 1.2, *WIN, 'window', 'sash', exterior=True)
opening('win-cw-north', 'WW_N', 4.0, 1.6, *WIN, 'window', 'sash', exterior=True)
opening('win-gw-north', 'EW_N', 38.0, 1.6, *WIN, 'window', 'sash', exterior=True)
# Garden doors: beds were wheeled out to the court for air and sun.
opening('cw-garden-door', 'WW_E', 28.2, 1.0, G, G + 2.3, 'door', 'glazed', leaves=1, hinge='a0', swing=-1,
        fixture='hospital-children-garden-door', label='儿童病房庭院门', exterior=True, enclosure='children')
opening('gw-garden-door', 'EW_W', 28.2, 1.0, G, G + 2.3, 'door', 'glazed', leaves=1, hinge='a0', swing=1,
        fixture='hospital-general-garden-door', label='普通病房庭院门', exterior=True, enclosure='general')

# ---------------------------------------------------------------------------
# Partitions (floor to ceiling).
# ---------------------------------------------------------------------------
GY = (G, G_CEIL)
wall('G_corr', 'x', 8.5, 0.4, 41.6, INT_T, *GY, 'int', 'int', 'ground')
for wid, x in (('P_x6', 6.0), ('P_x11', 11.0), ('P_x17', 17.0), ('P_x25', 25.0), ('P_x30', 30.0), ('P_x35', 35.0)):
    wall(wid, 'z', x, 0.4, 8.42, INT_T, *GY, 'int', 'int', 'ground')

opening('consult-door', 'G_corr', 3.2, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='hospital-consult-door', label='诊室门')
opening('dispense-hatch', 'G_corr', 8.5, 1.4, G + 1.0, G + 2.1, 'hatch', 'hatch', label='取药窗口')
opening('dispensary-door', 'G_corr', 10.2, .85, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a1', swing=-1,
        fixture='hospital-dispensary-door', label='药房门')
opening('wait-corr-arch', 'G_corr', 14.0, 1.6, G, G + 2.5, 'arch', 'interior', label='候诊室入口')
opening('hall-arch', 'G_corr', 21.0, 4.0, G, G + 3.0, 'arch', 'interior', label='门厅通往走廊')
opening('office-door', 'G_corr', 27.5, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='hospital-office-door', label='事务室门')
opening('matron-door', 'G_corr', 32.5, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a1', swing=-1,
        fixture='hospital-matron-door', label='护士长室门')
opening('theatre-door', 'G_corr', 38.3, 1.5, G, G + 2.3, 'door', 'panel', leaves=2, hinge='both', swing=-1,
        fixture='hospital-theatre-door', label='手术室门')
opening('wait-arch', 'P_x17', 4.4, 2.0, G, G + 2.5, 'arch', 'interior', label='候诊室')
opening('pay-hatch', 'P_x25', 4.2, 1.6, G + 1.0, G + 2.1, 'hatch', 'hatch', label='缴费窗口')
# Ward doors: wide pairs, standing open by day so the sister's desk commands the corridor.
opening('children-door', 'F_N_W', 4.0, 1.5, G, G + 2.3, 'door', 'glazed', leaves=2, hinge='both', swing=1,
        fixture='hospital-children-door', label='儿童病房门', startsOpen=True)
opening('general-door', 'F_N_E', 38.0, 1.5, G, G + 2.3, 'door', 'glazed', leaves=2, hinge='both', swing=1,
        fixture='hospital-general-door', label='普通病房门', startsOpen=True)

# ---------------------------------------------------------------------------
# Rooms (clear interiors). dado: colour of the gloss-painted lower wall.
# ---------------------------------------------------------------------------
room('consult', '门诊诊室', 0.4, 0.4, 5.92, 8.42, 'lino', .70, 'front', public=False)
room('dispensary', '药房', 6.08, 0.4, 10.92, 8.42, 'tiles', .60, 'front', public=False, dado='tile')
room('waiting', '候诊室', 11.08, 0.4, 16.92, 8.42, 'tiles', .70, 'front')
room('hall', '门厅 · 问讯处', 17.08, 0.4, 24.92, 8.42, 'tiles', .70, 'front')
room('office', '事务室 · 缴费处', 25.08, 0.4, 29.92, 8.42, 'wood', .65, 'front', public=False, dado='brown')
room('matron', '护士长室', 30.08, 0.4, 34.92, 8.42, 'wood', .65, 'front', public=False, dado='brown')
room('theatre', '手术室', 35.08, 0.4, 41.6, 8.42, 'tiles', .75, 'front', public=False, dado='tile')
room('corridor', '走廊', 0.4, 8.58, 41.6, 11.6, 'lino', .55, 'front')
room('children', '儿童病房', 0.4, 12.0, 7.6, 29.6, 'wood', .85, 'children', dado='blue')
room('general', '普通病房', 34.4, 12.0, 41.6, 29.6, 'wood', .85, 'general')


def slab(sid, x0, z0, x1, z1, y0, y1, top, bottom, kind):
    slabs.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, y0=y0, y1=y1, top=top, bottom=bottom, kind=kind))


slab('found-front', 0.4, 0.4, 41.6, 11.6, FOOT, G - .02, 'none', 'none', 'foundation')
slab('found-west', 0.4, 12.0, 7.6, 29.6, FOOT, G - .02, 'none', 'none', 'foundation')
slab('found-east', 34.4, 12.0, 41.6, 29.6, FOOT, G - .02, 'none', 'none', 'foundation')
for r in rooms:
    slab('floor-' + r['id'], r['x0'], r['z0'], r['x1'], r['z1'], G - .02, G, r['floor'], 'none', 'finish')
slab('ceil-front', 0.4, 0.4, 41.6, 11.6, G_CEIL, G_CEIL + .12, 'none', 'plaster', 'ceiling')
slab('ceil-west', 0.4, 12.0, 7.6, 29.6, G_CEIL, G_CEIL + .12, 'none', 'plaster', 'ceiling')
slab('ceil-east', 34.4, 12.0, 41.6, 29.6, G_CEIL, G_CEIL + .12, 'none', 'plaster', 'ceiling')
# Door stones. Each stays inside the town map's door clearance, so no planting grows through it.
slab('front-step', 19.9, -0.55, 22.1, 0.0, -.05, G, 'stone', 'none', 'step')
slab('court-step', 20.35, 12.0, 21.65, 12.5, -.05, G, 'stone', 'none', 'step')
slab('cw-garden-step', 8.0, 27.6, 8.5, 28.8, -.05, G, 'stone', 'none', 'step')
slab('gw-garden-step', 33.5, 27.6, 34.0, 28.8, -.05, G, 'stone', 'none', 'step')

# ---------------------------------------------------------------------------
# Roofs: two pavilions gabled to the street and to the north, the range between them,
# and the entrance gable. One ridge height, so the valleys meet without steps.
# ---------------------------------------------------------------------------
roofs.append(dict(id='roof-west', kind='gable', axis='z', x0=-.35, z0=-.35, x1=8.35, z1=30.35, eaves=EAVES, ridge=RIDGE, gables='both'))
roofs.append(dict(id='roof-east', kind='gable', axis='z', x0=33.65, z0=-.35, x1=42.35, z1=30.35, eaves=EAVES, ridge=RIDGE, gables='both'))
roofs.append(dict(id='roof-range', kind='gable', axis='x', x0=4.0, z0=-.35, x1=38.0, z1=12.35, eaves=EAVES, ridge=RIDGE, gables='none'))
roofs.append(dict(id='roof-entrance', kind='gable', axis='z', x0=16.65, z0=-.35, x1=25.35, z1=6.0, eaves=EAVES, ridge=RIDGE, gables='low'))
# Door hood on two posts, below the eaves.
roofs.append(dict(id='roof-hood', kind='gable', axis='z', x0=19.2, z0=-1.75, x1=22.8, z1=-.02, eaves=3.25, ridge=3.95, gables='low'))
for i, x in enumerate((19.5, 22.5)):
    columns.append(dict(id=f'hood-post-{i}', x=x, z=-1.5, w=.16, y0=.10, y1=3.2))
beams.append(dict(id='hood-beam', x0=19.42, x1=22.58, z0=-1.57, z1=-1.43, y0=3.06, y1=3.2))

# ---------------------------------------------------------------------------
# Furniture (front of a piece faces f=(sin yaw, cos yaw): user/seat/drawer/foot side).
# ---------------------------------------------------------------------------
T = G + .76
# Entrance hall and enquiry desk
furn('hall-counter', 'counter', 23.35, 4.2, -90, 3.4, .6, 1.05, room='hall', extra='问讯处')
furn('hall-ledger', 'ledger', 23.35, 3.5, -90, .34, .26, .05, y=G + 1.05, room='hall', collide=False)
furn('hall-bell', 'handbell', 23.25, 4.75, 0, .1, .1, .13, y=G + 1.05, room='hall', collide=False)
furn('hall-phone', 'deskphone', 23.4, 5.5, -90, .22, .2, .22, y=G + 1.05, room='hall', collide=False)
furn('hall-keys', 'keyboard', 24.905, 6.6, -90, .9, .04, .7, y=G + 1.25, room='hall', collide=False)
furn('hall-stool', 'stool', 24.3, 3.3, 0, .36, .36, .62, room='hall')
furn('hall-bench-1', 'bench', 17.33, 1.9, 90, 1.8, .45, .46, room='hall')
furn('hall-bench-2', 'bench', 17.33, 7.1, 90, 1.8, .45, .46, room='hall')
furn('hall-palm-1', 'palm', 17.45, .72, 0, .5, .5, 1.5, room='hall')
furn('hall-palm-2', 'palm', 24.5, 7.95, 0, .5, .5, 1.5, room='hall')
furn('hall-umbrella', 'umbrella', 19.55, .72, 0, .3, .3, .62, room='hall')
furn('hall-coat', 'coatstand', 22.6, .8, 0, .45, .45, 1.85, room='hall')
furn('hall-notice', 'noticeboard', 18.05, 8.405, 180, 1.2, .03, .9, y=G + 1.2, room='hall', collide=False)
furn('hall-donors', 'donorboard', 24.0, 8.405, 180, 1.3, .04, 1.5, y=G + .95, room='hall', collide=False)
furn('hall-sign', 'signboard', 24.9, 4.2, -90, 1.5, .03, .28, y=G + 2.35, room='hall', collide=False, extra='问 讯 · 缴 费')
furn('hall-radiator-1', 'radiator', 18.4, .53, 0, 1.0, .18, .75, room='hall')
furn('hall-radiator-2', 'radiator', 23.6, .53, 0, 1.0, .18, .75, room='hall')
# Waiting room
for i, z in enumerate((2.4, 4.0, 5.6)):
    furn(f'wait-bench-{i}', 'bench', 13.2, z, 0, 2.8, .45, .46, room='waiting')
furn('wait-bench-wall', 'bench', 11.33, 4.4, 90, 3.0, .45, .46, room='waiting')
furn('wait-stove', 'stove', 16.5, 7.95, 180, .55, .55, .9, room='waiting')
furn('wait-table', 'table', 15.9, 1.2, 0, .9, .6, .7, room='waiting')
furn('wait-papers', 'paper', 15.9, 1.2, 12, .3, .42, .01, y=G + .7, room='waiting', collide=False)
furn('wait-notice', 'noticeboard', 11.095, 7.4, 90, 1.0, .03, .8, y=G + 1.25, room='waiting', collide=False)
furn('wait-radiator-1', 'radiator', 12.6, .53, 0, 1.0, .18, .75, room='waiting')
furn('wait-radiator-2', 'radiator', 15.4, .53, 0, 1.0, .18, .75, room='waiting')
# Consulting room
furn('consult-desk', 'desk', 2.6, 2.4, 180, 1.4, .7, .76, room='consult')
furn('consult-chair', 'chair', 2.6, 1.65, 0, .46, .46, .9, room='consult')
furn('consult-patient', 'chair', 3.3, 3.3, 180, .46, .46, .9, room='consult')
furn('consult-book', 'ledger', 2.25, 2.45, 180, .32, .24, .05, y=T, room='consult', collide=False)
furn('consult-couch', 'examcouch', .8, 6.0, 0, .7, 1.9, .7, room='consult')
furn('consult-screen', 'screen', 1.75, 6.0, 90, 1.8, .06, 1.7, room='consult')
furn('consult-cabinet', 'medcabinet', 5.72, 3.0, -90, 1.0, .4, 1.8, room='consult')
furn('consult-sink', 'sink', 5.69, 5.4, -90, .55, .45, .85, room='consult')
furn('consult-scale', 'scale', 5.3, 7.2, -90, .4, .55, 1.4, room='consult')
furn('consult-chart', 'eyechart', 5.905, 1.3, -90, .5, .03, .9, y=G + 1.15, room='consult', collide=False)
furn('consult-radiator', 'radiator', 3.2, .53, 0, 1.0, .18, .75, room='consult')
# Dispensary
furn('disp-shelf-1', 'bottleshelf', 6.255, 3.0, 90, 3.0, .35, 2.2, room='dispensary')
furn('disp-shelf-2', 'bottleshelf', 10.745, 3.0, -90, 3.0, .35, 2.2, room='dispensary')
furn('disp-table', 'table', 8.5, 3.4, 0, 1.6, .8, .86, room='dispensary')
furn('disp-scales', 'pharmscale', 8.2, 3.4, 0, .4, .2, .35, y=G + .86, room='dispensary', collide=False)
furn('disp-mortar', 'mortar', 8.95, 3.3, 0, .16, .16, .12, y=G + .86, room='dispensary', collide=False)
furn('disp-counter', 'lowcabinet', 8.5, 8.17, 180, 1.6, .5, .85, room='dispensary')
furn('disp-sink', 'sink', 6.31, 6.6, 90, .55, .45, .85, room='dispensary')
# Office (almoner and secretary; the pay hatch opens onto the hall behind the enquiry desk)
furn('off-desk', 'desk', 29.55, 2.2, -90, 1.4, .7, .76, room='office')
furn('off-chair', 'chair', 28.85, 2.2, 90, .46, .46, .9, room='office')
furn('off-typewriter', 'typewriter', 29.55, 2.0, -90, .4, .32, .18, y=T, room='office', collide=False)
furn('off-stool', 'stool', 25.75, 4.2, 0, .36, .36, .62, room='office')
furn('off-ledger', 'ledger', 25.3, 4.55, 90, .36, .28, .06, y=G + 1.045, room='office', collide=False)
furn('off-cashbox', 'cashbox', 25.3, 3.8, 90, .3, .2, .12, y=G + 1.045, room='office', collide=False)
furn('off-files-1', 'filingcabinet', 26.0, 8.1, 180, .5, .6, 1.3, room='office')
furn('off-files-2', 'filingcabinet', 26.55, 8.1, 180, .5, .6, 1.3, room='office')
furn('off-safe', 'safe', 29.55, 7.95, 180, .6, .6, 1.0, room='office')
furn('off-phone', 'deskphone', 29.6, 2.75, -90, .22, .2, .22, y=T, room='office', collide=False)
furn('off-radiator', 'radiator', 27.5, .53, 0, 1.0, .18, .75, room='office')
# Matron
furn('matron-desk', 'desk', 32.5, 2.7, 180, 1.5, .8, .76, room='matron')
furn('matron-chair', 'chair', 32.5, 1.9, 0, .5, .5, .95, room='matron')
furn('matron-visitor-1', 'chair', 31.9, 4.0, 180, .46, .46, .9, room='matron')
furn('matron-visitor-2', 'chair', 33.1, 4.0, 180, .46, .46, .9, room='matron')
furn('matron-shelf', 'bookshelf', 30.255, 5.0, 90, 2.4, .35, 2.2, room='matron')
furn('matron-wardrobe', 'wardrobe', 34.625, 6.2, -90, 1.0, .55, 1.9, room='matron')
furn('matron-lamp', 'desklamp', 33.0, 2.6, 0, .3, .3, .48, y=T, room='matron', collide=False)
furn('matron-roster', 'noticeboard', 34.905, 3.4, -90, 1.0, .03, .8, y=G + 1.25, room='matron', collide=False)
furn('matron-radiator', 'radiator', 32.5, .53, 0, 1.0, .18, .75, room='matron')
# Theatre
furn('th-table', 'optable', 38.3, 4.0, 0, .6, 1.9, .85, room='theatre')
furn('th-cabinet', 'medcabinet', 35.3, 2.2, 90, 1.0, .4, 1.8, room='theatre', extra='instruments')
furn('th-sterilizer', 'sterilizer', 35.4, 6.6, 90, .7, .5, 1.1, room='theatre')
furn('th-sink-1', 'sink', 41.37, 6.3, -90, .55, .45, .85, room='theatre')
furn('th-sink-2', 'sink', 41.37, 7.3, -90, .55, .45, .85, room='theatre')
furn('th-trolley', 'trolley', 39.5, 3.7, 0, .45, .75, .85, room='theatre', extra='instruments')
furn('th-stool', 'stool', 37.5, 3.4, 0, .36, .36, .55, room='theatre')
furn('th-cylinder', 'gascylinder', 37.3, 5.4, 0, .3, .3, 1.3, room='theatre')
# Corridor
furn('corr-linen', 'wardrobe', 1.05, 8.86, 0, 1.2, .55, 2.0, room='corridor')
furn('corr-buckets', 'firebuckets', 5.6, 8.66, 0, 1.3, .16, .5, y=G + .9, room='corridor', collide=False)
furn('corr-bench-1', 'bench', 13.5, 11.37, 180, 1.8, .45, .46, room='corridor')
furn('corr-bench-2', 'bench', 28.5, 11.37, 180, 1.8, .45, .46, room='corridor')
for i, x in enumerate((10.5, 16.5, 25.5, 31.5)):
    furn(f'corr-radiator-{i}', 'radiator', x, 11.5, 180, 1.0, .18, .75, room='corridor')
furn('corr-stretcher', 'stretcher', 30.0, 8.96, 90, .65, 1.95, .85, room='corridor')
furn('corr-wheelchair', 'wheelchair', 34.6, 9.1, 180, .62, .95, 1.0, room='corridor')
furn('corr-sign-children', 'signboard', 18.7, 11.585, 180, 1.5, .03, .28, y=G + 2.3, room='corridor', collide=False, extra='←  儿 童 病 房')
furn('corr-sign-general', 'signboard', 23.3, 11.585, 180, 1.5, .03, .28, y=G + 2.3, room='corridor', collide=False, extra='普 通 病 房  →')
furn('corr-notice', 'noticeboard', 24.0, 8.595, 0, 1.2, .03, .9, y=G + 1.2, room='corridor', collide=False)
furn('corr-palm', 'palm', 22.55, 11.25, 0, .5, .5, 1.5, room='corridor')

# Wards. Bed 'front' is the foot; heads stand a hand's breadth off the wall.
CW_BED = (.8, 1.7)          # children's iron beds
GW_BED = (.9, 1.95)
BED_Z = (13.7, 16.4, 19.2, 22.0, 24.8, 27.6)
CHILD_BED = 2               # third bed on the court side
BOY_BED = 1                 # a small boy sits up across the ward
MAN_BED, WOMAN_BED = 1, 3   # general ward: an old man asleep by the east wall, a woman sitting up on the court side
QUILTS = ('rose', 'blue', 'sage', 'cream', 'blue', 'rose')
for i, z in enumerate(BED_Z):
    furn(f'cw-bed-w{i}', 'wardbed', .42 + CW_BED[1] / 2, z, 90, CW_BED[0], CW_BED[1], 1.0, room='children', extra=QUILTS[i] + (':propped' if i == BOY_BED else ''))
    furn(f'cw-locker-w{i}', 'locker', .62, z - .68, 90, .4, .4, .75, room='children')
for i, z in enumerate(BED_Z[:5]):
    furn(f'cw-bed-e{i}', 'wardbed', 7.58 - CW_BED[1] / 2, z, -90, CW_BED[0], CW_BED[1], 1.0, room='children',
         extra='child' if i == CHILD_BED else QUILTS[(i + 2) % 6])
    furn(f'cw-locker-e{i}', 'locker', 7.38, z - .68, -90, .4, .4, .75, room='children')
CHILD_Z = BED_Z[CHILD_BED]
furn('cw-apples', 'applebasket', 7.38, CHILD_Z - .68, 0, .3, .3, .2, y=G + .75, room='children', collide=False)
furn('cw-overbed', 'overbed', 6.38, CHILD_Z, -90, 1.0, .4, .98, room='children')
furn('cw-drawing', 'drawing', 6.38, CHILD_Z + .08, -78, .3, .22, .004, y=G + .98, room='children', collide=False)
furn('cw-crayons', 'crayons', 6.42, CHILD_Z - .28, -90, .16, .1, .02, y=G + .98, room='children', collide=False)
furn('cw-chair', 'chair', 6.3, CHILD_Z - .9, 20, .46, .46, .9, room='children')
furn('cw-flowers', 'flowers', 7.38, BED_Z[0] - .68, 0, .14, .14, .3, y=G + .75, room='children', collide=False)
furn('cw-centre', 'table', 4.0, 16.6, 0, .9, .6, .62, room='children')
furn('cw-centre-flowers', 'flowers', 4.0, 16.6, 0, .14, .14, .3, y=G + .62, room='children', collide=False)
furn('cw-desk', 'desk', 4.0, 22.2, 0, 1.2, .6, .76, room='children')
furn('cw-desk-chair', 'chair', 4.9, 23.0, 200, .46, .46, .9, room='children')
furn('cw-desk-book', 'ledger', 3.75, 22.2, 0, .32, .24, .05, y=T, room='children', collide=False)
furn('cw-desk-lamp', 'desklamp', 4.4, 22.15, 0, .3, .3, .48, y=T, room='children', collide=False)
furn('cw-cabinet', 'medcabinet', 1.2, 12.2, 0, 1.0, .4, 1.8, room='children')
furn('cw-sink', 'sink', 6.9, 12.23, 0, .55, .45, .85, room='children')
furn('cw-screen', 'screen', 1.3, 25.55, 0, 1.6, .06, 1.7, room='children')
furn('cw-toybox', 'toybox', 2.5, 29.3, 180, .9, .5, .45, room='children')
furn('cw-horse', 'rockinghorse', 4.7, 28.7, -30, .35, 1.0, .9, room='children')
furn('cw-smalltable', 'smalltable', 3.5, 27.2, 0, .7, .7, .5, room='children')
furn('cw-smallchair-1', 'smallchair', 3.5, 27.85, 180, .3, .3, .55, room='children')
furn('cw-smallchair-2', 'smallchair', 2.85, 27.2, 90, .3, .3, .55, room='children')
furn('cw-blocks', 'blocks', 3.5, 27.2, 20, .3, .2, .1, y=G + .5, room='children', collide=False)
furn('cw-frieze', 'frieze', 4.0, 29.585, 180, 4.6, .02, .5, y=G + 3.2, room='children', collide=False)
for i, z in enumerate(BED_Z[:5]):
    furn(f'gw-bed-w{i}', 'wardbed', 34.42 + GW_BED[1] / 2, z, 90, GW_BED[0], GW_BED[1], 1.1, room='general', extra=('cream', 'sage')[i % 2] + (':propped' if i == WOMAN_BED else ''))
    furn(f'gw-locker-w{i}', 'locker', 34.62, z - .73, 90, .4, .4, .75, room='general')
for i, z in enumerate(BED_Z):
    furn(f'gw-bed-e{i}', 'wardbed', 41.58 - GW_BED[1] / 2, z, -90, GW_BED[0], GW_BED[1], 1.1, room='general', extra=('sage', 'cream')[i % 2] + (':lying' if i == MAN_BED else ''))
    furn(f'gw-locker-e{i}', 'locker', 41.38, z - .73, -90, .4, .4, .75, room='general')
furn('gw-centre', 'table', 38.0, 16.6, 0, 1.0, .7, .76, room='general')
furn('gw-centre-palm', 'palm', 38.0, 16.6, 0, .3, .3, .8, y=T, room='general', collide=False)
furn('gw-desk', 'desk', 38.0, 22.2, 0, 1.0, .6, .76, room='general')
furn('gw-desk-chair', 'chair', 38.0, 22.9, 180, .46, .46, .9, room='general')
furn('gw-cabinet', 'medcabinet', 40.8, 12.2, 0, 1.0, .4, 1.8, room='general')
furn('gw-sink', 'sink', 35.1, 12.23, 0, .55, .45, .85, room='general')
furn('gw-screen-1', 'screen', 40.5, 20.6, 0, 1.6, .06, 1.7, room='general')
furn('gw-screen-2', 'screen', 35.5, 15.05, 0, 1.6, .06, 1.7, room='general')
# Hot-water radiators under the ward windows (the wards' stoves went when the boiler came).
for i, z in enumerate(WARD_Z):
    furn(f'cw-radiator-w{i}', 'radiator', .5, z, 90, .8, .18, .75, room='children')
    furn(f'cw-radiator-e{i}', 'radiator', 7.5, z, -90, .8, .18, .75, room='children')
    furn(f'gw-radiator-w{i}', 'radiator', 34.5, z, 90, .8, .18, .75, room='general')
    furn(f'gw-radiator-e{i}', 'radiator', 41.5, z, -90, .8, .18, .75, room='general')
furn('gw-trolley', 'trolley', 38.0, 13.6, 90, .45, .75, .85, room='general')
furn('gw-table', 'table', 38.0, 26.6, 0, 1.0, .7, .76, room='general')
furn('gw-flowers', 'flowers', 38.0, 26.6, 0, .14, .14, .3, y=T, room='general', collide=False)
furn('gw-wheelchair', 'wheelchair', 37.2, 28.8, 20, .62, .95, 1.0, room='general')

# ---------------------------------------------------------------------------
# Lamps: opal globes on long drops. Interior lights reach only the interior light layer.
# ---------------------------------------------------------------------------
def pend(lid, x, z, room_id, intensity=3.4, rng=6.5, access=.75):
    lamp(lid, 'pendant', x, G_CEIL - .8, z, room_id, intensity=intensity, rng=rng, access=access)


pend('lamp-hall-1', 21.0, 2.6, 'hall'); pend('lamp-hall-2', 21.0, 6.3, 'hall')
pend('lamp-wait', 14.0, 4.4, 'waiting'); pend('lamp-consult', 3.2, 4.2, 'consult')
pend('lamp-disp', 8.5, 4.6, 'dispensary', access=.6); pend('lamp-office', 27.5, 4.4, 'office')
pend('lamp-matron', 32.5, 4.4, 'matron')
pend('lamp-theatre-1', 36.3, 6.2, 'theatre'); pend('lamp-theatre-2', 40.3, 6.2, 'theatre')
lamp('lamp-theatre-op', 'oplamp', 38.3, G + 2.25, 4.0, 'theatre', intensity=4.0, rng=4.5, access=.4)
for i, x in enumerate((4.0, 10.0, 16.0, 21.0, 26.0, 32.0, 38.0)):
    pend(f'lamp-corr-{i}', x, 10.1, 'corridor', intensity=2.8, rng=6.0, access=.55)
for i, z in enumerate((14.4, 19.0, 23.6, 28.2)):
    # Softer and wider than the other rooms (user, 2026-10-14: the night ward should feel warm and soft).
    pend(f'lamp-cw-{i}', 4.0, z, 'children', intensity=2.7, rng=7.8, access=.85)
    pend(f'lamp-gw-{i}', 38.0, z, 'general', intensity=2.7, rng=7.8, access=.85)
lamp('lamp-cw-desk', 'desk', 4.4, T + .37, 22.15, 'children', intensity=.9, rng=2.4, access=.6, switch='hospital-children-desk-lamp-off', shade='green')
lamp('lamp-matron-desk', 'desk', 33.0, T + .37, 2.6, 'matron', intensity=.9, rng=2.4, access=.6, switch='hospital-matron-desk-lamp-off', shade='green')
# Exterior lanterns (photocell; share the twelve street pools).
lamp('lantern-front-w', 'wall', 19.55, G + 2.3, -0.2, 'street', role='Exterior', intensity=3.2, rng=7.0, access=.85)
lamp('lantern-front-e', 'wall', 22.45, G + 2.3, -0.2, 'street', role='Exterior', intensity=3.2, rng=7.0, access=.85)
lamp('lantern-court', 'wall', 22.0, G + 2.45, 12.2, 'court', role='Exterior', intensity=2.2, rng=6.0, access=.85)

# ---------------------------------------------------------------------------
# Interaction points. People stand or lie where the story needs them.
# ---------------------------------------------------------------------------
# The hall porter keeps the enquiry desk (period practice; a hospital this size has no almoner).
item('porter', 'npc', 24.3, G, 4.2, .7, 1.8, .5, title='问讯处门房', yaw=-90)
# The child sits propped on the pillows of the third court-side bed, facing its foot (west).
# Half lying against a slope of pillows (user, 2026-10-14: she is weak and should lie lower), so her
# hips are further from the head rail than a child sitting up.
item('child', 'npc', 7.58 - .52, G + .58, CHILD_Z, .7, 1.0, .7, title='病床上的孩子', yaw=-90)
item('ward-sister', 'npc', 4.0, G, 22.95, .7, 1.8, .5, title='病房护士', yaw=180)
# The rest of the hospital's people: doc names the cast figure, target how the figure is placed
# (stand, propped = sitting up in bed, lying, sit = on a bench). They only pass the time of day:
# their lines live in Resources/HospitalPeople.json and touch neither the case nor the child's health.
def person(pid, role, title, x, z, yaw, pose='stand', y=None):
    item(pid, 'person', x, G if y is None else y, z, .6, 1.7, .5, doc=role, title=title, target=pose, yaw=yaw)


BED_TOP = G + .58
person('nurse-corridor', 'StaffNurse', '护士', 8.5, 9.35, 180)
person('probationer', 'Probationer', '见习护士', 38.0, 14.45, 180)
person('matron', 'Matron', '护士长', 36.2, 10.1, -90)
person('house-surgeon', 'HouseSurgeon', '住院医生', 36.95, BED_Z[2], -90)
person('visiting-doctor', 'VisitingDoctor', '出诊医生', 3.65, 2.0, 0)
person('patient-boy', 'PatientBoy', '小病人', .42 + .36, BED_Z[BOY_BED], 90, 'propped', BED_TOP)
person('patient-woman', 'PatientWoman', '女病人', 34.42 + .4, BED_Z[WOMAN_BED], 90, 'propped', BED_TOP)
person('patient-man', 'PatientMan', '老病人', 41.58 - .95, BED_Z[MAN_BED], -90, 'lying', BED_TOP)
person('visitor-woman', 'VisitorWoman', '探视的母亲', 1.45, BED_Z[BOY_BED] - .85, 0)
person('visitor-man', 'VisitorMan', '探视的工人', 18.45, 5.9, 90)
person('visitor-granny', 'VisitorGranny', '老妇人', 13.0, 11.3, 180, 'sit')
# Hospital records (tools/hospital/evidence.py; docs/research/mystery-evidence-design.md). Each lies on
# the prop that carries it in the shell: the open register on the enquiry desk, the receipt stub book
# inside the pay hatch, the child's drawing on her overbed table. doc = record id in HospitalEvidence.json.
item('rec-register', 'evidence', 23.35, G + 1.05, 3.5, .42, .12, .34, doc='Y01', evidence='Y01', title='探视登记簿', yaw=-90)
item('rec-receipts', 'evidence', 25.3, G + 1.045, 4.55, .42, .12, .34, doc='Y02', evidence='Y02', title='缴费收据存根簿', yaw=90)
item('rec-drawing', 'evidence', 6.38, G + .98, CHILD_Z + .08, .34, .08, .26, doc='Y03', evidence='Y03', title='孩子的画', yaw=-78)
item('switch-cw-desk', 'lampswitch', 4.4, T + .12, 22.15, .26, .26, .26, title='病房值班台灯', target='lamp-cw-desk')
item('switch-matron', 'lampswitch', 33.0, T + .12, 2.6, .26, .26, .26, title='护士长台灯', target='lamp-matron-desk')
seat('hospital-seat-hall-1', '门厅长凳', 17.33, 1.9, 90, eye=1.1, w=1.6)
seat('hospital-seat-hall-2', '门厅长凳', 17.33, 7.1, 90, eye=1.1, w=1.6)
for i, z in enumerate((2.4, 4.0, 5.6)):
    seat(f'hospital-seat-wait-{i}', '候诊长凳', 13.2, z, 0, eye=1.1, w=2.6)
# (the west corridor bench is taken by an old woman who sits there)
seat('hospital-seat-corr-2', '走廊长凳', 28.5, 11.37, 180, eye=1.1, w=1.6)
seat('hospital-seat-bedside', '床边的椅子', 6.3, CHILD_Z - .9, 20, standOff=.5, behind=True)

# ---------------------------------------------------------------------------
# Weather shelters: rain clipping + acoustic enclosures (exterior door fixtures).
# ---------------------------------------------------------------------------
def shelter(sid, x0, z0, x1, z1, eaves, rise, ridgeAlongX, doors, open_=False):
    shelters.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, eaves=eaves, rise=rise, ridgeAlongX=ridgeAlongX,
                         doors=doors, open=open_))


shelter('front', -.35, -.35, 42.35, 12.0, EAVES, RIDGE - EAVES, True, ['hospital-front-door', 'hospital-court-door'])
shelter('children', -.35, 12.0, 8.35, 30.35, EAVES, RIDGE - EAVES, False, ['hospital-children-garden-door'])
shelter('general', 33.65, 12.0, 42.35, 30.35, EAVES, RIDGE - EAVES, False, ['hospital-general-garden-door'])
shelter('hood', 19.2, -1.75, 22.8, -.35, 3.25, 0, False, [], True)

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
    # A partition crossing must not land inside an opening of the wall it meets.
    for other in walls:
        if other['axis'] == w['axis'] or not (w['a0'] - .01 <= other['c'] <= w['a1'] + .01):
            continue
        if not (other['a0'] - .3 <= w['c'] <= other['a1'] + .3):
            continue
        for o in own:
            if abs(o['at'] - other['c']) < o['width'] / 2 + other['t'] / 2 + .04 and o['bottom'] < other['y1'] and o['top'] > other['y0']:
                errors.append(f"wall {other['id']} runs into opening {o['id']}")
fixtures = [o['fixture'] for o in openings if o['fixture']]
if len(fixtures) != len(set(fixtures)):
    errors.append('duplicate fixture ids')
for name, group in (('furniture', furniture), ('items', items), ('lamps', lamps), ('walls', walls), ('openings', openings)):
    ids = [x['id'] for x in group]
    if len(ids) != len(set(ids)):
        errors.append('duplicate ids in ' + name)
for i, a in enumerate(rooms):
    for b in rooms[i + 1:]:
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
        errors.append(f"furniture {f['id']} names no room")
        continue
    x0, z0, x1, z1 = footprint(f)
    if x0 < r['x0'] - .025 or x1 > r['x1'] + .025 or z0 < r['z0'] - .025 or z1 > r['z1'] + .025:
        errors.append(f"furniture {f['id']} outside {r['id']}: {x0:.2f},{z0:.2f}..{x1:.2f},{z1:.2f}")
# Standing furniture must not interpenetrate, except pieces meant to stand over a bed.
solid = [f for f in furniture if f['collide'] and f['kind'] not in ('overbed',)]
for i, a in enumerate(solid):
    ax0, az0, ax1, az1 = footprint(a)
    for b in solid[i + 1:]:
        bx0, bz0, bx1, bz1 = footprint(b)
        if min(ax1, bx1) - max(ax0, bx0) > .02 and min(az1, bz1) - max(az0, bz0) > .02:
            errors.append(f"furniture overlaps: {a['id']} {b['id']}")
# Door swing and approach clearances must stay free of colliding furniture.
for o in openings:
    if o['kind'] not in ('door', 'arch'):
        continue
    w = W[o['wall']]
    reach = min(max(o['width'] / max(o['leaves'], 1), .7) + .1, 1.4)
    for side in (-1, 1):
        if w['axis'] == 'x':
            box = (o['at'] - o['width'] / 2, w['c'] + side * (w['t'] / 2), o['at'] + o['width'] / 2, w['c'] + side * (w['t'] / 2 + reach))
        else:
            box = (w['c'] + side * (w['t'] / 2), o['at'] - o['width'] / 2, w['c'] + side * (w['t'] / 2 + reach), o['at'] + o['width'] / 2)
        bx0, bz0 = min(box[0], box[2]), min(box[1], box[3])
        bx1, bz1 = max(box[0], box[2]), max(box[1], box[3])
        for f in furniture:
            if not f['collide']:
                continue
            x0, z0, x1, z1 = footprint(f)
            if min(x1, bx1) - max(x0, bx0) > .02 and min(z1, bz1) - max(z0, bz0) > .02:
                errors.append(f"furniture {f['id']} blocks {o['id']}")
# A clear way at least 1.1 m wide down each ward between the bed feet.
for rid in ('children', 'general'):
    r = R[rid]
    feet = [footprint(f) for f in furniture if f['room'] == rid and f['kind'] == 'wardbed']
    west = max(b[2] for b in feet if b[0] < (r['x0'] + r['x1']) / 2 - 1)
    east = min(b[0] for b in feet if b[2] > (r['x0'] + r['x1']) / 2 + 1)
    for f in furniture:
        if f['room'] != rid or not f['collide'] or f['kind'] in ('wardbed', 'locker', 'overbed'):
            continue
        x0, z0, x1, z1 = footprint(f)
        if x0 < west or x1 > east:
            continue
        if max(x0 - west, east - x1) < 1.1:
            errors.append(f"{f['id']} leaves no way past in the {rid} ward")

# Agreement with the town map (B11 parts, doors, windows) in world coordinates.
plan = json.loads((ROOT / 'docs/maps/town-current/plan-data.json').read_text(encoding='utf-8'))
b11 = next(b for b in plan['buildings'] if b['id'] == 'B11')
parts = sorted(tuple(round(v, 3) for v in p['rect']) for p in b11['parts'])
expect = sorted([(101.0, -59.0, 42.0, 12.0), (84.0, -44.0, 8.0, 18.0), (118.0, -44.0, 8.0, 18.0)])
if parts != expect:
    errors.append(f'plan-data B11 parts differ: {parts}')
fd = next(o for o in openings if o['id'] == 'front-door')
cd = next(o for o in openings if o['id'] == 'court-door')
if [round(fd['x'] + ORIGIN[0], 3), round(0 + ORIGIN[2], 3)] != [round(v, 3) for v in b11['door']]:
    errors.append('front door differs from plan-data door')
if [round(cd['x'] + ORIGIN[0], 3), round(12 + ORIGIN[2], 3)] != [round(v, 3) for v in b11['rearDoor']]:
    errors.append('court door differs from plan-data rearDoor')
side_of = {'F_S': 'S', 'F_W': 'W', 'F_E': 'E', 'F_N_C': 'N', 'WW_W': 'W', 'WW_E': 'E', 'WW_N': 'N', 'EW_W': 'W', 'EW_E': 'E', 'EW_N': 'N'}


def outer(o):
    """Centre of an exterior window on the outer wall face, in world metres, as plan-data lists it."""
    w = W[o['wall']]
    side = side_of[o['wall']]
    off = .2 if side in ('E', 'N') else -.2
    if w['axis'] == 'x':
        return round(o['at'] + ORIGIN[0], 2), round(w['c'] + off + ORIGIN[2], 2), o['width'], side
    return round(w['c'] + off + ORIGIN[0], 2), round(o['at'] + ORIGIN[2], 2), o['width'], side


ext_windows = sorted(outer(o) for o in openings if o['kind'] == 'window' and o['exterior'])
plan_windows = sorted((round(w[0], 2), round(w[1], 2), w[2], w[4] if len(w) > 4 else 'S') for w in b11['windows'])
if '--print-plan-windows' in sys.argv:
    print(json.dumps([[x, z, wd, '窗', s] for x, z, wd, s in ext_windows], ensure_ascii=False))
elif ext_windows != plan_windows:
    errors.append(f'exterior windows differ from plan-data ({len(ext_windows)} here, {len(plan_windows)} there): {sorted(set(ext_windows) ^ set(plan_windows))[:6]}')
if b11['storeys'] != 1 and '--print-plan-windows' not in sys.argv:
    errors.append('plan-data B11 must be single-storey')

fin = [s for s in slabs if s['kind'] == 'finish']
for i, a in enumerate(fin):
    for b in fin[i + 1:]:
        if abs(a['y1'] - b['y1']) < .01 and min(a['x1'], b['x1']) - max(a['x0'], b['x0']) > .001 and min(a['z1'], b['z1']) - max(a['z0'], b['z0']) > .001:
            errors.append(f"floor finishes overlap (z-fighting): {a['id']} {b['id']}")
if errors:
    sys.exit('HOSPITAL_LAYOUT_FAIL:\n' + '\n'.join(errors))

data = dict(version=1, id='B11', name='青川医院', scheme='单层双翼庭院式小镇医院', origin=list(ORIGIN),
            levels=dict(ground=G, groundCeiling=G_CEIL, upper=NONE, upperCeiling=NONE, frontEaves=EAVES,
                        frontRidge=RIDGE, wingCeiling=G_CEIL, wingEaves=EAVES, wingRidge=RIDGE, court=COURT),
            walls=walls, openings=openings, rooms=rooms, slabs=slabs, stairs=stairs, roofs=roofs, columns=columns,
            beams=beams, furniture=furniture, lamps=lamps, items=items, shelters=shelters,
            notes='Unity building-local metres (+X east, +Y up, +Z north); root at world origin above; yaw degrees, front faces (sin yaw, cos yaw).')
if '--print-plan-windows' not in sys.argv:
    text = json.dumps(data, ensure_ascii=False, indent=1) + '\n'
    out = ROOT / 'assets/source/hospital'
    out.mkdir(parents=True, exist_ok=True)
    for path in (out / 'hospital-layout.json', ROOT / 'game/Assets/Resources/HospitalLayout.json'):
        path.write_bytes(text.encode('utf-8'))
    print(json.dumps(dict(status='HOSPITAL_LAYOUT_PASS', walls=len(walls), openings=len(openings),
                          doors=sum(o['leaves'] > 0 for o in openings), leaves=sum(o['leaves'] for o in openings),
                          windows=sum(o['kind'] == 'window' for o in openings), rooms=len(rooms),
                          furniture=len(furniture), lamps=len(lamps), items=len(items), shelters=len(shelters),
                          beds=sum(f['kind'] == 'wardbed' for f in furniture)), ensure_ascii=False))
