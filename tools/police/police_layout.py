"""Shared layout contract for the D courtyard-loggia police station (B10).

One source for the Blender shell/furniture generator and the Unity assembler
(BuildSlice.Police). Coordinates are Unity building-local metres: +X east,
+Y up, +Z north; the prefab root sits at world (1, 0, 27.7) with no rotation,
so plan-data world X = 1 + x and Z = 27.7 + z.

Run from the repository root (stdlib only):
    python tools/police/police_layout.py
Writes assets/source/police/police-layout.json and its Unity copy
game/Assets/Resources/PoliceLayout.json, after validating the geometry and
checking that docs/maps/town-current/plan-data.json B10 matches.
"""
import json, math, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ORIGIN = (1.0, 0.0, 27.7)

# Levels (metres above the town datum).
G = 0.15            # ground floor finish (town walk is .10)
G_CEIL = 3.35       # ground ceiling underside
U = 3.55            # upper floor finish
U_CEIL = 6.55
FRONT_EAVES = 6.75
FRONT_RIDGE = 8.8
WING_CEIL = 3.35
WING_EAVES = 3.6
WING_RIDGE = 5.0
COURT = 0.06
EXT_T = 0.4
INT_T = 0.16
FOOT = -0.25

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


def room(rid, name, level, x0, z0, x1, z1, floor, access, enclosure, public=True):
    rooms.append(dict(id=rid, name=name, level=level, x0=x0, z0=z0, x1=x1, z1=z1, floor=floor,
                      access=access, enclosure=enclosure, public=public))


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
    """behind: a chair drawn up to a table; the player stands up behind it, not onto the table."""
    f = (math.sin(math.radians(yaw)), math.cos(math.radians(yaw)))
    k = -1 if behind else 1
    stand = (x + k * f[0] * (depth / 2 + standOff), z + k * f[1] * (depth / 2 + standOff))
    item(sid, 'seat', x, G + height, z, w, .17, depth, title=caption, yaw=yaw,
         seat=dict(sitX=x, sitZ=z, sitY=G, standX=round(stand[0], 3), standZ=round(stand[1], 3), eye=eye))


# ---------------------------------------------------------------------------
# Exterior walls. Through walls run the full span; abutting walls stop at faces.
# ---------------------------------------------------------------------------
wall('F_S', 'x', 0.2, 0, 23, EXT_T, FOOT, FRONT_EAVES, 'int', 'ext', 'front')
wall('F_W', 'z', 0.2, 0.4, 11.6, EXT_T, FOOT, FRONT_EAVES, 'int', 'ext', 'front')
wall('F_E', 'z', 22.8, 0.4, 11.6, EXT_T, FOOT, FRONT_EAVES, 'ext', 'int', 'front')
# Rear wall of the front range: wing rooms sit behind its west/east ground thirds.
wall('F_N_W', 'x', 11.8, 0, 5.5, EXT_T, FOOT, G_CEIL + .2, 'int', 'int', 'front')
wall('F_N_C', 'x', 11.8, 5.5, 17.5, EXT_T, FOOT, G_CEIL + .2, 'ext', 'int', 'front')
wall('F_N_E', 'x', 11.8, 17.5, 23, EXT_T, FOOT, G_CEIL + .2, 'int', 'int', 'front')
wall('F_N_U', 'x', 11.8, 0, 23, EXT_T, G_CEIL + .2, FRONT_EAVES, 'ext', 'int', 'front')
# West wing
wall('WW_W', 'z', 0.2, 12, 23.6, EXT_T, FOOT, WING_EAVES, 'int', 'ext', 'west')
wall('WW_E', 'z', 5.3, 12, 23.6, EXT_T, FOOT, WING_EAVES, 'ext', 'int', 'west')
wall('WW_N', 'x', 23.8, 0, 5.5, EXT_T, FOOT, WING_EAVES, 'ext', 'int', 'west')
# East wing
wall('EW_W', 'z', 17.7, 12, 23.6, EXT_T, FOOT, WING_EAVES, 'int', 'ext', 'east')
wall('EW_E', 'z', 22.8, 12, 23.6, EXT_T, FOOT, WING_EAVES, 'ext', 'int', 'east')
wall('EW_N', 'x', 23.8, 17.5, 23, EXT_T, FOOT, WING_EAVES, 'ext', 'int', 'east')
# Court wall closing the north end between the wings (user request 2026-10-13: the court
# must not stand open to the back land). Thinner than the house walls and set back 8 cm
# from the wing gables, so the wing quoins and plinths stay proud of it; its court face
# (z 23.72) clears the last loggia column shafts and beam ends. The top stays below the
# column caps (3.0) and the loggia roofs sail over it.
YARD_T = 0.2
YARD_TOP = 2.9
wall('C_N', 'x', 23.82, 5.5, 17.5, YARD_T, FOOT, YARD_TOP, 'ext', 'ext', 'court')

# Exterior openings -----------------------------------------------------------
WIN_G = (G + .9, G + 2.8)      # ground sash windows
WIN_U = (U + .9, U + 2.6)      # upper sash windows
opening('front-door', 'F_S', 11.5, 1.4, G, G + 3.0, 'door', 'front', leaves=2, hinge='both', swing=1,
        fixture='police-front-door', label='警署正门', exterior=True, enclosure='front')
for i, x in enumerate((2.4, 5.2, 8.0, 15.0, 17.8, 20.6)):
    opening(f'win-front-g{i}', 'F_S', x, 1.2, *WIN_G, 'window', 'sash', exterior=True)
for i, x in enumerate((2.4, 5.2, 9.3, 11.5, 13.7, 17.8, 20.6)):
    opening(f'win-front-u{i}', 'F_S', x, 1.1, *WIN_U, 'window', 'sash', exterior=True)
opening('win-east-wait', 'F_E', 3.2, 1.2, *WIN_G, 'window', 'sash', exterior=True)
opening('win-east-stair', 'F_E', 10.5, 1.0, 2.7, 3.9, 'window', 'sash', exterior=True)
opening('win-east-rest', 'F_E', 3.2, 1.1, *WIN_U, 'window', 'sash', exterior=True)
opening('court-door', 'F_N_C', 7.36, 1.05, G, G + 2.3, 'door', 'glazed', leaves=1, hinge='a0', swing=-1,
        fixture='police-court-door', label='横厅后门', exterior=True, enclosure='front')
opening('win-cat-loggia', 'F_N_C', 6.0, .7, G + .9, G + 2.5, 'window', 'sash', exterior=True)
opening('win-wc-court', 'F_N_C', 9.35, .7, G + 1.75, G + 2.45, 'window', 'high', exterior=True)
opening('win-office-1', 'F_N_C', 12.4, 1.2, *WIN_G, 'window', 'sash', exterior=True)
opening('win-office-2', 'F_N_C', 14.8, 1.2, *WIN_G, 'window', 'sash', exterior=True)
for i, x in enumerate((8.6, 11.0, 13.4)):
    opening(f'win-rear-u{i}', 'F_N_U', x, 1.1, *WIN_U, 'window', 'sash', exterior=True)
opening('win-rear-wc2', 'F_N_U', 16.9, .7, U + 1.2, U + 2.4, 'window', 'sash', exterior=True)
# West wing (inner wall faces the west loggia)
opening('annex-door', 'WW_E', 13.4, 1.05, G, G + 2.2, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='police-annex-door', label='资料分馆门', exterior=True, enclosure='annex')
opening('win-annex', 'WW_E', 15.8, 1.4, *WIN_G, 'window', 'sash', exterior=True)
opening('archive-door', 'WW_E', 19.2, 1.05, G, G + 2.2, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='police-archive-door', label='档案库门', exterior=True, enclosure='archive')
opening('win-archive', 'WW_E', 21.8, 1.0, *WIN_G, 'window', 'sash', exterior=True)
opening('win-archive-n', 'WW_N', 2.75, 1.2, G + 1.9, G + 2.7, 'window', 'high', exterior=True)
# East wing (inner wall faces the east loggia)
opening('duty-door', 'EW_W', 13.0, 1.05, G, G + 2.2, 'door', 'panel', leaves=1, hinge='a0', swing=1,
        fixture='police-duty-door', label='巡警值班室门', exterior=True, enclosure='duty')
opening('win-duty-court', 'EW_W', 14.8, 1.2, *WIN_G, 'window', 'sash', exterior=True)
opening('evidence-door', 'EW_W', 17.0, 1.05, G, G + 2.2, 'door', 'panel', leaves=1, hinge='a0', swing=1,
        fixture='police-evidence-door', label='物证室门', exterior=True, enclosure='evidence')
opening('holding-door', 'EW_W', 20.6, .9, G, G + 2.1, 'door', 'iron', leaves=1, hinge='a0', swing=1,
        fixture='police-holding-door', label='旧羁押室铁栅门', exterior=True, enclosure='holding')
opening('win-duty-east', 'EW_E', 14.0, 1.2, *WIN_G, 'window', 'sash', exterior=True)
opening('win-evidence-e', 'EW_E', 17.8, 1.0, G + 1.9, G + 2.6, 'window', 'high', exterior=True)
opening('win-holding-n', 'EW_N', 20.25, .8, G + 2.0, G + 2.5, 'window', 'barred', exterior=True)
# Court wall: the barred yard gate on the court axis keeps the way north to the service yard
# (plan-data B10 backgate). Both leaves start closed and open into the court.
opening('yard-gate', 'C_N', 11.5, 1.8, COURT, COURT + 2.3, 'door', 'iron', leaves=2, hinge='both', swing=-1,
        fixture='police-yard-gate', label='内院铁栅后门', exterior=True)

# ---------------------------------------------------------------------------
# Ground-floor partitions (floor to ceiling).
# ---------------------------------------------------------------------------
GY = (G, G_CEIL)
wall('G_read_N', 'x', 7.0, 0.4, 10.08, INT_T, *GY, 'int', 'int', 'ground')
wall('G_read_hall', 'z', 10.0, 0.4, 6.92, INT_T, *GY, 'int', 'int', 'ground')
wall('G_z6', 'x', 6.0, 12.92, 22.6, INT_T, *GY, 'int', 'int', 'ground')
wall('G_hall_int', 'z', 13.0, 0.4, 5.92, INT_T, *GY, 'int', 'int', 'ground')
wall('G_int_wait', 'z', 19.0, 0.4, 5.92, INT_T, *GY, 'int', 'int', 'ground')
wall('G_stair_W', 'z', 19.0, 7.0, 11.6, INT_T, G, U_CEIL, 'int', 'int', 'ground')
wall('G_cross_N1', 'x', 9.0, 0.4, 6.58, INT_T, *GY, 'int', 'int', 'ground')
wall('G_cross_N2', 'x', 9.0, 8.12, 18.92, INT_T, *GY, 'int', 'int', 'ground')
wall('G_cat_pass', 'z', 6.5, 9.08, 11.6, INT_T, *GY, 'int', 'int', 'ground')
wall('G_pass_wc', 'z', 8.2, 9.08, 11.6, INT_T, *GY, 'int', 'int', 'ground')
wall('G_wc_off', 'z', 10.5, 9.08, 11.6, INT_T, *GY, 'int', 'int', 'ground')
wall('W_split', 'x', 18.0, 0.4, 5.1, INT_T, G, WING_CEIL, 'int', 'int', 'west')
wall('E_split1', 'x', 16.0, 17.9, 22.6, INT_T, G, WING_CEIL, 'int', 'int', 'east')
wall('E_split2', 'x', 19.6, 17.9, 22.6, INT_T, G, WING_CEIL, 'int', 'int', 'east')
# Stair spine between the two flights; railing on the upper landing over flight A.
wall('S_spine', 'z', 20.8, 7.0, 9.52, INT_T, G, U_CEIL, 'int', 'int', 'stair')
wall('S_rail', 'x', 7.0, 20.88, 22.6, .08, U, U + 1.0, 'int', 'int', 'stair')

opening('read-arch', 'G_read_hall', 4.0, 1.6, G, G + 2.4, 'arch', 'interior', label='阅览室入口')
opening('read-north', 'G_read_N', 4.5, 1.2, G, G + 2.3, 'arch', 'interior', label='阅览室北门')
opening('interview-door', 'G_hall_int', 4.6, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a1', swing=1,
        fixture='police-interview-door', label='问询室门', startsOpen=True)
opening('wait-door', 'G_z6', 19.9, 1.0, G, G + 2.2, 'arch', 'interior', label='候问室门')
opening('catalog-door', 'G_cross_N1', 3.5, .9, G, G + 2.15, 'arch', 'interior', label='誊录室门')
opening('wc-door', 'G_cross_N2', 9.0, .8, G, G + 2.1, 'door', 'panel', leaves=1, hinge='a0', swing=1,
        fixture='police-wc-door', label='盥洗间门', startsOpen=True)
opening('duty-hatch', 'G_cross_N2', 11.9, 2.2, G + .95, G + 2.25, 'hatch', 'hatch', label='值班接待窗口')
opening('office-door', 'G_cross_N2', 17.2, .95, G, G + 2.15, 'door', 'panel', leaves=1, hinge='a1', swing=1,
        fixture='police-office-door', label='值班室门', startsOpen=True)

# ---------------------------------------------------------------------------
# Upper floor partitions.
# ---------------------------------------------------------------------------
UY = (U, U_CEIL)
wall('U_z6', 'x', 6.0, 0.4, 22.6, INT_T, *UY, 'int', 'int', 'upper')
wall('U_chief_work', 'z', 7.5, 0.4, 5.92, INT_T, *UY, 'int', 'int', 'upper')
wall('U_work_rest', 'z', 15.9, 0.4, 5.92, INT_T, *UY, 'int', 'int', 'upper')
wall('U_z9', 'x', 9.0, 0.4, 18.92, INT_T, *UY, 'int', 'int', 'upper')
wall('U_arch_wc', 'z', 15.9, 9.08, 11.6, INT_T, *UY, 'int', 'int', 'upper')
opening('chief-door', 'U_z6', 6.6, .95, U, U + 2.15, 'door', 'panel', leaves=1, hinge='a1', swing=-1,
        fixture='police-chief-door', label='负责人办公室门', startsOpen=True)
opening('work-door', 'U_z6', 11.5, 1.1, U, U + 2.2, 'arch', 'interior', label='调查办公室门')
opening('rest-door', 'U_z6', 17.0, .95, U, U + 2.15, 'door', 'panel', leaves=1, hinge='a0', swing=-1,
        fixture='police-rest-door', label='值班休息室门', startsOpen=True)
opening('archive2-door-w', 'U_z9', 4.0, 1.1, U, U + 2.2, 'arch', 'interior', label='档案储存')
opening('archive2-door-e', 'U_z9', 12.0, 1.1, U, U + 2.2, 'arch', 'interior', label='档案储存')
opening('wc2-door', 'U_z9', 17.4, .8, U, U + 2.1, 'door', 'panel', leaves=1, hinge='a1', swing=1,
        fixture='police-wc2-door', label='二层盥洗门', startsOpen=True)

# ---------------------------------------------------------------------------
# Rooms (clear interiors) and enclosures.
# ---------------------------------------------------------------------------
room('read', '案卷阅览室', 0, 0.4, 0.4, 9.92, 6.92, 'oak', .70, 'front')
room('hall', '接待门厅', 0, 10.08, 0.4, 12.92, 6.92, 'tiles', .55, 'front')
room('interview', '问询室', 0, 13.08, 0.4, 18.92, 5.92, 'oak', .65, 'front')
room('waiting', '候问侧席', 0, 19.08, 0.4, 22.6, 5.92, 'oak', .65, 'front')
room('cross', '贯通横厅', 0, 0.4, 6.92, 18.92, 8.92, 'tiles', .30, 'front')
room('lobby', '横厅候问区', 0, 13.08, 6.08, 19.0, 6.92, 'tiles', .30, 'front')
room('stair', '双跑楼梯', 0, 19.08, 6.0, 22.6, 11.6, 'oak', .40, 'front')
room('catalog', '档案誊录室', 0, 0.4, 9.08, 6.42, 11.6, 'oak', .35, 'front', public=False)
room('passage', '院门通道', 0, 6.58, 8.92, 8.12, 11.6, 'tiles', .35, 'front')
room('wc', '盥洗间', 0, 8.28, 9.08, 10.42, 11.6, 'tiles', .25, 'front', public=False)
room('office', '值班接待室', 0, 10.58, 9.08, 18.92, 11.6, 'oak', .55, 'front', public=False)
room('annex', '资料分馆', 0, 0.4, 12.0, 5.1, 17.92, 'oak', .55, 'annex')
room('archive', '装订档案库', 0, 0.4, 18.08, 5.1, 23.6, 'oak', .35, 'archive')
room('duty', '巡警值班室', 0, 17.9, 12.0, 22.6, 15.92, 'oak', .60, 'duty', public=False)
room('evidence', '物证室', 0, 17.9, 16.08, 22.6, 19.52, 'tiles', .20, 'evidence', public=False)
room('holding', '旧羁押室', 0, 17.9, 19.68, 22.6, 23.6, 'stone', .15, 'holding')
room('chief', '负责人办公室', 1, 0.4, 0.4, 7.42, 5.92, 'oak', .70, 'front', public=False)
room('work', '调查办公／誊录', 1, 7.58, 0.4, 15.82, 5.92, 'oak', .70, 'front', public=False)
room('rest', '值班休息室', 1, 15.98, 0.4, 22.6, 5.92, 'oak', .65, 'front', public=False)
room('corridor', '二层走廊', 1, 0.4, 6.08, 19.0, 8.92, 'oak', .25, 'front')
room('landing', '楼梯平台', 1, 19.0, 6.08, 22.6, 7.0, 'oak', .40, 'front')
room('archive2', '档案储存', 1, 0.4, 9.08, 15.82, 11.6, 'oak', .45, 'front', public=False)
room('wc2', '盥洗／储藏', 1, 15.98, 9.08, 18.92, 11.6, 'tiles', .40, 'front', public=False)

# ---------------------------------------------------------------------------
# Slabs: foundations, floor finishes, the upper floor and ceilings.
# ---------------------------------------------------------------------------
def slab(sid, x0, z0, x1, z1, y0, y1, top, bottom, kind):
    slabs.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, y0=y0, y1=y1, top=top, bottom=bottom, kind=kind))

slab('found-front', 0.4, 0.4, 22.6, 11.6, FOOT, G - .02, 'none', 'none', 'foundation')
slab('found-west', 0.4, 12.0, 5.1, 23.6, FOOT, G - .02, 'none', 'none', 'foundation')
slab('found-east', 17.9, 12.0, 22.6, 23.6, FOOT, G - .02, 'none', 'none', 'foundation')
for r in rooms:
    if r['level'] == 0 and r['id'] != 'stair':
        slab('floor-' + r['id'], r['x0'], r['z0'], r['x1'], r['z1'], G - .02, G, r['floor'], 'none', 'finish')
slab('floor-stairstrip', 19.0, 6.08, 22.6, 7.0, G - .02, G, 'oak', 'none', 'finish')
# Upper floor slab: whole front range except the stair void (x 19.08-22.6, z 7.0-11.6).
slab('upper-main', 0.4, 0.4, 19.0, 11.6, G_CEIL, U - .02, 'none', 'plaster', 'structure')
slab('upper-east', 19.0, 0.4, 22.6, 7.0, G_CEIL, U - .02, 'none', 'plaster', 'structure')
for r in rooms:
    if r['level'] == 1:
        slab('floor-' + r['id'], r['x0'], r['z0'], r['x1'], r['z1'], U - .02, U, r['floor'], 'none', 'finish')
slab('ceil-front', 0.4, 0.4, 22.6, 11.6, U_CEIL, U_CEIL + .12, 'none', 'plaster', 'ceiling')
slab('ceil-west', 0.4, 12.0, 5.1, 23.6, WING_CEIL, WING_CEIL + .12, 'none', 'plaster', 'ceiling')
slab('ceil-east', 17.9, 12.0, 22.6, 23.6, WING_CEIL, WING_CEIL + .12, 'none', 'plaster', 'ceiling')
# Loggias and court (exterior surfaces).
slab('loggia-west', 5.5, 12.0, 7.0, 24.0, FOOT, G, 'flag', 'none', 'loggia')
slab('loggia-east', 16.0, 12.0, 17.5, 24.0, FOOT, G, 'flag', 'none', 'loggia')
slab('court', 7.0, 12.0, 16.0, 24.0, -.12, COURT, 'court', 'none', 'court')
slab('front-step', 10.5, -0.5, 12.5, 0.0, -.05, G, 'stone', 'none', 'step')
slab('court-step', 7.0, 12.0, 7.95, 12.55, COURT, G, 'stone', 'none', 'step')

# ---------------------------------------------------------------------------
# Stairs: two flights of ten 0.17m risers (0.28m treads) and a mid landing.
# Solid masses down to the floor, so nothing can be walked under.
# ---------------------------------------------------------------------------
RISE, TREAD = .17, .28
for k in range(1, 10):
    stairs.append(dict(id=f'A{k}', x0=20.88, x1=22.6, z0=7.0 + TREAD * (k - 1), z1=7.0 + TREAD * k, top=round(G + RISE * k, 3)))
stairs.append(dict(id='landing-mid', x0=19.08, x1=22.6, z0=9.52, z1=11.6, top=round(G + RISE * 10, 3)))
for j in range(1, 10):
    stairs.append(dict(id=f'B{j}', x0=19.08, x1=20.72, z0=round(9.52 - TREAD * j, 3), z1=round(9.52 - TREAD * (j - 1), 3), top=round(G + RISE * (10 + j), 3)))

# ---------------------------------------------------------------------------
# Roofs, loggia columns and beams.
# ---------------------------------------------------------------------------
roofs.append(dict(id='roof-front', kind='hip', x0=-0.35, z0=-0.35, x1=23.35, z1=12.35, eaves=FRONT_EAVES, ridge=FRONT_RIDGE, axis='x', low=0))
roofs.append(dict(id='roof-west', kind='gable', x0=-0.3, z0=12.0, x1=5.5, z1=24.3, eaves=WING_EAVES, ridge=WING_RIDGE, axis='z', low=0))
roofs.append(dict(id='roof-east', kind='gable', x0=17.5, z0=12.0, x1=23.3, z1=24.3, eaves=WING_EAVES, ridge=WING_RIDGE, axis='z', low=0))
roofs.append(dict(id='roof-loggia-west', kind='leanto', x0=5.5, z0=12.0, x1=7.2, z1=24.15, eaves=3.22, ridge=3.52, axis='z', low=1))
roofs.append(dict(id='roof-loggia-east', kind='leanto', x0=15.8, z0=12.0, x1=17.5, z1=24.15, eaves=3.22, ridge=3.52, axis='z', low=-1))
for side, x in (('w', 6.82), ('e', 16.18)):
    for i, z in enumerate((14.0, 16.4, 18.8, 21.2, 23.6)):
        columns.append(dict(id=f'col-{side}{i}', x=x, z=z, w=.24, y0=G, y1=3.12))
    beams.append(dict(id=f'beam-{side}', x0=x - .11, x1=x + .11, z0=12.0, z1=23.72, y0=3.12, y1=3.3))

# ---------------------------------------------------------------------------
# Furniture (front of a piece faces f=(sin yaw, cos yaw): user/seat/drawer side).
# ---------------------------------------------------------------------------
# Entrance hall
furn('hall-coat', 'coatstand', 10.45, 1.0, 90, .45, .45, 1.85, room='hall')
furn('report-desk', 'writingdesk', 10.38, 2.3, 90, .95, .55, .78, room='hall')
furn('hall-bench', 'bench', 12.69, 2.6, -90, 1.6, .45, .46, room='hall')
furn('hall-umbrella', 'umbrella', 12.62, .78, 0, .3, .3, .62, room='hall')
furn('hall-notice', 'noticeboard', 10.095, 5.4, 90, 1.0, .03, .8, y=G + 1.2, room='hall', collide=False)
# Reading room: public material table, comparison table, classification cabinet, shelves
furn('read-t1', 'longtable', 2.6, 2.3, 0, 3.0, 1.0, .76, room='read')
furn('read-t2', 'longtable', 7.2, 2.3, 0, 2.4, 1.0, .76, room='read')
furn('read-stand-1', 'docstand', 6.75, 2.2, 0, .45, .32, .32, y=G + .76, room='read', collide=False)
furn('read-stand-2', 'docstand', 7.65, 2.2, 0, .45, .32, .32, y=G + .76, room='read', collide=False)
furn('read-folder-1', 'folder', 1.75, 2.25, 0, .34, .26, .03, y=G + .76, room='read', collide=False)
furn('read-page-2', 'paper', 2.5, 2.3, 8, .24, .32, .01, y=G + .76, room='read', collide=False)
furn('read-photo-3', 'photo', 3.15, 2.28, -6, .22, .17, .01, y=G + .76, room='read', collide=False)
furn('read-lamp-1', 'desklamp', 3.9, 2.55, 0, .3, .3, .48, y=G + .76, room='read', collide=False)
furn('read-lamp-2', 'desklamp', 8.2, 2.55, 0, .3, .3, .48, y=G + .76, room='read', collide=False)
furn('read-cabinet', 'cardcabinet', 0.65, 4.9, 90, 2.4, .5, 1.15, room='read', extra='证人|日期|物证')
furn('read-shelf-1', 'bookshelf', 2.15, 6.745, 180, 3.2, .35, 2.2, room='read')
furn('read-shelf-2', 'bookshelf', 7.5, 6.745, 180, 4.4, .35, 2.2, room='read')
for i, x in enumerate((1.8, 3.4)):
    furn(f'read-chair-{i}', 'chair', x, 3.15, 180, .46, .46, .9, room='read')
furn('read-chair-2', 'chair', 7.2, 3.15, 180, .46, .46, .9, room='read')
# Interview room
furn('int-table', 'table', 16.0, 2.9, 0, 1.6, .9, .76, room='interview')
furn('int-chair-0', 'chair', 16.0, 3.65, 180, .46, .46, .9, room='interview')
furn('int-chair-1', 'chair', 15.5, 2.15, 0, .46, .46, .9, room='interview')
furn('int-chair-2', 'chair', 16.5, 2.15, 0, .46, .46, .9, room='interview')
furn('int-pad', 'paper', 16.0, 2.95, 0, .24, .32, .01, y=G + .76, room='interview', collide=False)
furn('int-coat', 'coatstand', 18.6, .8, -90, .45, .45, 1.85, room='interview')
# Waiting room
furn('wait-bench-1', 'bench', 19.31, 3.0, 90, 1.8, .45, .46, room='waiting')
furn('wait-bench-2', 'bench', 21.8, 5.69, 180, 1.4, .45, .46, room='waiting')
furn('wait-stove', 'stove', 22.2, 1.05, -90, .55, .55, .9, room='waiting')
furn('wait-capes', 'caperack', 22.3, 4.3, -90, .5, .3, 1.8, room='waiting')
furn('wait-boots', 'boottray', 20.9, .7, 0, .8, .35, .08, room='waiting', collide=False)
# Cross hall
furn('cross-phone', 'telephone', 15.2, 8.85, 180, .28, .14, .4, y=G + 1.25, room='cross', collide=False)
furn('cross-notice', 'noticeboard', 2.5, 7.085, 0, 1.0, .03, .8, y=G + 1.2, room='cross', collide=False)
furn('cross-hooks', 'hookrail', 15.8, 6.11, 0, 1.4, .06, .2, y=G + 1.55, room='lobby', collide=False)
furn('cross-bench', 'bench', 15.9, 6.31, 0, 1.6, .45, .46, room='lobby')
# Catalog room
furn('cat-desk', 'desk', 5.55, 11.3, 180, 1.2, .6, .76, room='catalog')
furn('cat-chair', 'chair', 5.55, 10.55, 0, .46, .46, .9, room='catalog')
furn('cat-typewriter', 'typewriter', 5.3, 11.32, 180, .4, .32, .18, y=G + .76, room='catalog', collide=False)
furn('cat-book', 'ledger', 5.95, 11.25, 180, .32, .24, .05, y=G + .76, room='catalog', collide=False)
furn('cat-shelf', 'bookshelf', 0.575, 10.35, 90, 2.3, .35, 2.2, room='catalog')
furn('cat-files', 'filingcabinet', 2.4, 11.3, 180, .5, .6, 1.3, room='catalog')
# WC
furn('wc-basin', 'washstand', 9.9, 11.33, 180, .6, .45, .85, room='wc')
# Duty office (behind the hatch)
furn('off-desk-1', 'desk', 15.0, 11.25, 180, 1.4, .7, .76, room='office')
furn('off-chair-1', 'chair', 15.0, 10.5, 0, .46, .46, .9, room='office')
furn('off-desk-2', 'desk', 18.0, 11.25, 180, 1.4, .7, .76, room='office')
furn('off-files', 'filingcabinet', 18.6, 9.75, -90, .5, .6, 1.3, room='office')
furn('off-stove', 'stove', 13.4, 11.2, 180, .55, .55, .9, room='office')
furn('off-phone', 'deskphone', 14.6, 11.3, 180, .22, .2, .22, y=G + .76, room='office', collide=False)
furn('off-roster', 'noticeboard', 18.915, 10.6, -90, .9, .03, .7, y=G + 1.3, room='office', collide=False)
# West wing
furn('annex-table', 'table', 2.75, 15.0, 0, 1.8, .9, .76, room='annex')
furn('annex-chair-0', 'chair', 2.3, 15.75, 180, .46, .46, .9, room='annex')
furn('annex-chair-1', 'chair', 3.2, 14.25, 0, .46, .46, .9, room='annex')
furn('annex-lamp', 'desklamp', 3.45, 15.2, 0, .3, .3, .48, y=G + .76, room='annex', collide=False)
furn('annex-shelf-1', 'bookshelf', 0.575, 13.8, 90, 2.4, .35, 2.2, room='annex')
furn('annex-shelf-2', 'bookshelf', 0.575, 16.3, 90, 2.4, .35, 2.2, room='annex')
furn('arch-shelf-1', 'bookshelf', 0.575, 19.6, 90, 2.4, .35, 2.2, room='archive')
furn('arch-shelf-2', 'bookshelf', 0.575, 22.05, 90, 2.4, .35, 2.2, room='archive')
furn('arch-island', 'doubleshelf', 2.75, 21.2, 90, 3.2, .7, 2.0, room='archive')
# East wing
furn('duty-desk', 'desk', 22.25, 14.0, -90, 1.4, .7, .76, room='duty')
furn('duty-chair', 'chair', 21.55, 14.0, 90, .46, .46, .9, room='duty')
furn('duty-book', 'ledger', 22.3, 13.8, -90, .32, .24, .05, y=G + .76, room='duty', collide=False)
furn('duty-stove', 'stove', 18.45, 15.35, 90, .55, .55, .9, room='duty')
furn('duty-capes', 'caperack', 22.3, 12.45, -90, .5, .3, 1.8, room='duty')
furn('duty-bench', 'bench', 20.6, 15.69, 180, 1.4, .45, .46, room='duty')
furn('ev-shelf', 'bookshelf', 22.425, 17.8, -90, 2.8, .35, 2.2, room='evidence', extra='boxes')
furn('ev-cage', 'cage', 20.4, 19.2, 180, 1.6, .6, 2.0, room='evidence')
furn('ev-board', 'photoboard', 20.2, 16.1, 0, 1.4, .03, .9, y=G + 1.05, room='evidence', collide=False)
furn('ev-table', 'table', 20.4, 17.6, 0, 1.2, .7, .76, room='evidence')
furn('hold-bed', 'ironbed', 22.18, 21.7, -90, 1.9, .8, .55, room='holding')
furn('hold-basin', 'basinstand', 18.35, 23.25, 180, .45, .4, .75, room='holding')
furn('hold-bucket', 'bucket', 21.9, 23.25, 0, .3, .3, .32, room='holding', collide=False)
# Upper floor
UF = U
furn('chief-desk', 'desk', 3.8, 1.55, 0, 1.5, .8, .76, y=UF, room='chief')
furn('chief-chair', 'chair', 3.8, 2.35, 180, .5, .5, .95, y=UF, room='chief')
furn('chief-visitor', 'chair', 2.3, 3.6, 180, .46, .46, .9, y=UF, room='chief')
furn('chief-shelf', 'bookshelf', 0.575, 2.6, 90, 2.4, .35, 2.2, y=UF, room='chief')
furn('chief-coat', 'coatstand', 0.8, 5.5, 90, .45, .45, 1.85, y=UF, room='chief')
furn('chief-lamp', 'desklamp', 4.35, 1.4, 0, .3, .3, .48, y=UF + .76, room='chief', collide=False)
furn('chief-papers', 'paper', 3.6, 1.5, -10, .24, .32, .01, y=UF + .76, room='chief', collide=False)
for i, x in enumerate((9.3, 11.5, 13.7)):
    furn(f'work-desk-{i}', 'desk', x, 1.0, 0, 1.3, .7, .76, y=UF, room='work')
    furn(f'work-chair-{i}', 'chair', x, 1.75, 180, .46, .46, .9, y=UF, room='work')
furn('work-typewriter', 'typewriter', 11.5, 0.95, 0, .4, .32, .18, y=UF + .76, room='work', collide=False)
furn('work-files', 'filingcabinet', 15.45, 3.5, -90, .5, .6, 1.3, y=UF, room='work')
furn('work-files-2', 'filingcabinet', 7.95, 3.5, 90, .5, .6, 1.3, y=UF, room='work')
furn('rest-bed-1', 'cot', 17.3, 1.0, 0, 1.9, .8, .5, y=UF, room='rest')
furn('rest-bed-2', 'cot', 20.6, 1.0, 0, 1.9, .8, .5, y=UF, room='rest')
furn('rest-wardrobe', 'wardrobe', 22.3, 4.5, -90, 1.0, .55, 1.9, y=UF, room='rest')
furn('rest-table', 'table', 18.9, 3.6, 0, .8, .6, .74, y=UF, room='rest')
furn('rest-chair', 'chair', 18.9, 4.2, 180, .46, .46, .9, y=UF, room='rest')
furn('rest-wash', 'washstand', 16.33, 3.0, 90, .6, .45, .85, y=UF, room='rest')
for i, x in enumerate((1.8, 4.2, 6.6)):
    furn(f'a2-shelf-{i}', 'bookshelf', x, 11.425, 180, 2.4, .35, 2.2, y=UF, room='archive2')
for i, x in enumerate((8.6, 11.0, 13.4)):
    furn(f'a2-low-{i}', 'lowcabinet', x, 11.35, 180, 2.2, .5, .85, y=UF, room='archive2')
furn('a2-index', 'cardcabinet', 15.2, 10.0, -90, 1.1, .5, 1.15, y=UF, room='archive2', extra='索引')
furn('wc2-basin', 'washstand', 16.5, 11.33, 180, .6, .45, .85, y=UF, room='wc2')
furn('wc2-cabinet', 'wardrobe', 18.5, 11.3, 180, .7, .5, 1.8, y=UF, room='wc2')
furn('corr-bench', 'bench', 9.0, 6.31, 0, 1.6, .45, .46, y=UF, room='corridor')
# Court (planter kerb; tree and benches come from the shared town kit in Unity)
furn('court-planter', 'planter', 11.5, 18.4, 0, 2.4, 2.4, .32, y=COURT, room='court')
furn('court-drain', 'drain', 11.5, 14.6, 0, .5, .5, .01, y=COURT, room='court', collide=False)

# ---------------------------------------------------------------------------
# Lamps. Interior lights affect only the police interior rendering layer.
# ---------------------------------------------------------------------------
def pend(lid, x, z, room_id, level=0, intensity=3.2, rng=5.5, access=.75):
    y = (G_CEIL if level == 0 else U_CEIL) - .55
    if room_id in ('annex', 'archive', 'duty', 'evidence', 'holding'):
        y = WING_CEIL - .55
    lamp(lid, 'pendant', x, y, z, room_id, intensity=intensity, rng=rng, access=access)

pend('lamp-hall-1', 11.5, 2.2, 'hall'); pend('lamp-hall-2', 11.5, 5.4, 'hall')
pend('lamp-read-1', 2.6, 3.6, 'read', access=.70); pend('lamp-read-2', 7.2, 3.6, 'read', access=.70)
lamp('lamp-read-desk-1', 'desk', 3.9, G + 1.13, 2.55, 'read', intensity=.9, rng=2.4, access=.6, switch='police-read-lamp-1-off', shade='green')
lamp('lamp-read-desk-2', 'desk', 8.2, G + 1.13, 2.55, 'read', intensity=.9, rng=2.4, access=.6, switch='police-read-lamp-2-off', shade='green')
pend('lamp-int', 16.0, 3.0, 'interview', access=.65)
pend('lamp-wait', 20.8, 3.2, 'waiting', intensity=2.6, access=.65)
pend('lamp-cross-1', 3.5, 8.0, 'cross', intensity=2.6, access=.3); pend('lamp-cross-2', 9.5, 8.0, 'cross', intensity=2.6, access=.3)
pend('lamp-cross-3', 16.0, 7.5, 'cross', intensity=2.6, access=.3)
pend('lamp-cat', 3.5, 10.3, 'catalog', intensity=2.4, rng=4.5, access=.35)
lamp('lamp-cat-desk', 'desk', 6.1, G + 1.13, 11.4, 'catalog', intensity=.8, rng=2.2, access=.5, switch='police-catalog-lamp-off', shade='green')
pend('lamp-wc', 9.35, 10.3, 'wc', intensity=1.6, rng=3.5, access=.25)
pend('lamp-office-1', 13.5, 10.3, 'office', intensity=2.6, access=.55); pend('lamp-office-2', 17.0, 10.3, 'office', intensity=2.6, access=.55)
lamp('lamp-stair', 'pendant', 20.8, U_CEIL - .7, 10.4, 'stair', intensity=3.0, rng=6.5, access=.4)
pend('lamp-annex', 2.75, 15.0, 'annex', access=.55)
lamp('lamp-annex-desk', 'desk', 3.45, G + 1.13, 15.2, 'annex', intensity=.8, rng=2.2, access=.5, switch='police-annex-lamp-off', shade='green')
pend('lamp-archive', 2.75, 20.8, 'archive', intensity=2.6, access=.35)
pend('lamp-duty', 20.25, 14.0, 'duty', access=.6)
pend('lamp-evidence', 20.25, 17.8, 'evidence', intensity=2.2, rng=4.5, access=.2)
pend('lamp-holding', 20.25, 21.6, 'holding', intensity=1.3, rng=4.0, access=.15)
pend('lamp-chief', 4.0, 3.2, 'chief', level=1, access=.7)
lamp('lamp-chief-desk', 'desk', 4.35, U + 1.13, 1.4, 'chief', intensity=.8, rng=2.2, access=.6, switch='police-chief-lamp-off', shade='green')
pend('lamp-work-1', 9.8, 3.2, 'work', level=1, access=.7); pend('lamp-work-2', 13.6, 3.2, 'work', level=1, access=.7)
pend('lamp-rest', 19.3, 3.2, 'rest', level=1, intensity=2.4, access=.65)
pend('lamp-corr-1', 5.0, 7.5, 'corridor', level=1, intensity=2.4, access=.25); pend('lamp-corr-2', 13.0, 7.5, 'corridor', level=1, intensity=2.4, access=.25)
pend('lamp-a2-1', 4.0, 10.3, 'archive2', level=1, intensity=2.4, access=.45); pend('lamp-a2-2', 12.0, 10.3, 'archive2', level=1, intensity=2.4, access=.45)
pend('lamp-wc2', 17.4, 10.3, 'wc2', level=1, intensity=1.6, rng=3.5, access=.4)
# Exterior lanterns (photocell; share the twelve street pools).
lamp('lantern-front-w', 'wall', 10.15, G + 2.55, -0.2, 'street', role='Exterior', intensity=3.2, rng=7.0, access=.85)
lamp('lantern-front-e', 'wall', 12.85, G + 2.55, -0.2, 'street', role='Exterior', intensity=3.2, rng=7.0, access=.85)
lamp('lantern-loggia-w1', 'wall', 5.78, G + 2.45, 15.0, 'court', role='Exterior', intensity=2.0, rng=6.0, access=.85)
lamp('lantern-loggia-w2', 'wall', 5.78, G + 2.45, 21.2, 'court', role='Exterior', intensity=2.0, rng=6.0, access=.85)
lamp('lantern-loggia-e1', 'wall', 17.22, G + 2.45, 15.0, 'court', role='Exterior', intensity=2.0, rng=6.0, access=.85)
lamp('lantern-loggia-e2', 'wall', 17.22, G + 2.45, 21.2, 'court', role='Exterior', intensity=2.0, rng=6.0, access=.85)

# ---------------------------------------------------------------------------
# Interaction points (Unity trigger boxes). Texts live in Resources/PoliceDocuments.json.
# ---------------------------------------------------------------------------
T = G + .76
item('doc-e01', 'evidence', 1.75, T + .03, 2.25, .38, .08, .3, doc='e01-copy', evidence='E01')
item('doc-statement', 'document', 2.5, T + .03, 2.3, .3, .08, .36, doc='husband-statement')
item('doc-letter-photo', 'document', 3.15, T + .03, 2.28, .28, .08, .24, doc='letter-photo')
item('doc-compare-e01', 'evidence', 6.75, T + .2, 2.2, .46, .36, .3, doc='e01-compare', evidence='E01')
item('doc-compare-e08', 'evidence', 7.65, T + .2, 2.2, .46, .36, .3, doc='e08-copy', evidence='E08')
item('report-desk', 'report', 10.38, G + .9, 2.3, .55, .3, .9, title='整理阶段报告')
item('notice-hall', 'document', 10.12, G + 1.6, 5.4, .08, .8, 1.0, doc='hall-notice')
item('notice-cross', 'document', 2.5, G + 1.6, 7.12, 1.0, .8, .08, doc='patrol-notice')
item('cross-phone', 'document', 15.2, G + 1.45, 8.86, .3, .42, .16, doc='telephone')
item('cat-book', 'document', 5.95, T + .05, 11.25, .34, .1, .28, doc='catalog')
item('int-pad', 'document', 16.0, T + .03, 2.95, .3, .08, .36, doc='interview-pad')
item('off-phone', 'document', 14.6, T + .12, 11.3, .26, .24, .24, doc='office-phone')
item('duty-book', 'document', 22.3, T + .05, 13.8, .28, .1, .34, doc='duty-book')
item('duty-capes', 'document', 22.3, G + 1.2, 12.45, .5, 1.2, .35, doc='rain-capes')
item('ev-board', 'document', 20.2, G + 1.5, 16.2, 1.4, .9, .08, doc='photo-board')
item('ev-cage', 'document', 20.4, G + 1.0, 18.88, 1.6, 1.8, .08, doc='sealed-cage')
item('hold-bed', 'document', 22.18, G + .75, 21.7, .8, .3, 1.9, doc='holding-bed')
item('hold-basin', 'document', 18.35, G + .8, 23.25, .45, .2, .4, doc='holding-basin')
item('annex-gazette', 'document', 0.8, G + 1.3, 13.8, .3, .5, 2.2, doc='gazette')
item('arch-boxes', 'document', 2.75, G + 1.2, 21.2, .7, .6, 3.0, doc='archive-boxes')
item('chief-papers', 'document', 3.6, U + .79, 1.5, .3, .08, .36, doc='permit-stub')
item('a2-index', 'document', 14.98, U + .9, 10.0, .1, .5, 1.1, doc='archive-index')
item('wait-stove', 'document', 22.2, G + .6, 1.05, .55, .8, .55, doc='stove')
# Classification drawers (doc is the card inside, readable when pulled open).
for i, (label, doc) in enumerate((('证人', 'index-witness'), ('日期', 'index-date'), ('物证', 'index-evidence'))):
    z = 4.9 + (i - 1) * .8
    item(f'drawer-{i}', 'drawer', 0.9, G + .78, z, .1, .3, .72, doc=doc, title=label, target=f'police-index-drawer-{i}')
# Lamp switches
item('switch-read-1', 'lampswitch', 3.9, T + .12, 2.55, .26, .26, .26, title='阅览桌台灯', target='lamp-read-desk-1')
item('switch-read-2', 'lampswitch', 8.2, T + .12, 2.55, .26, .26, .26, title='对照桌台灯', target='lamp-read-desk-2')
item('switch-cat', 'lampswitch', 6.1, T + .12, 11.4, .26, .26, .26, title='誊录桌台灯', target='lamp-cat-desk')
item('switch-annex', 'lampswitch', 3.45, T + .12, 15.2, .26, .26, .26, title='分馆台灯', target='lamp-annex-desk')
item('switch-chief', 'lampswitch', 4.35, U + .88, 1.4, .26, .26, .26, title='办公桌台灯', target='lamp-chief-desk')
# Duty officer behind the reception hatch (talk target) and the report.
item('duty-officer', 'npc', 11.9, G, 9.72, .7, 1.8, .5, title='值班警员', yaw=180)
# Seats
seat('police-seat-read-0', '阅览椅', 1.8, 3.15, 180, behind=True)
seat('police-seat-read-1', '阅览椅', 3.4, 3.15, 180, behind=True)
seat('police-seat-read-2', '对照桌椅', 7.2, 3.15, 180, behind=True)
seat('police-seat-hall-bench', '门厅长凳', 12.69, 2.6, -90, eye=1.1, w=1.4)
seat('police-seat-wait-1', '候问长凳', 19.31, 3.0, 90, eye=1.1, w=1.6)
seat('police-seat-wait-2', '候问长凳', 21.8, 5.69, 180, eye=1.1, w=1.2, standOff=.5)
seat('police-seat-cross', '横厅长凳', 15.9, 6.31, 0, eye=1.1, w=1.4)
seat('police-seat-int', '问询椅', 16.0, 3.65, 180, behind=True)
seat('police-seat-annex', '分馆阅读椅', 2.3, 15.75, 180, behind=True)
seat('police-seat-catalog', '誊录椅', 5.55, 10.55, 0, standOff=.4, behind=True)

# ---------------------------------------------------------------------------
# Weather shelters: rain clipping + acoustic enclosures (door fixtures).
# ---------------------------------------------------------------------------
def shelter(sid, x0, z0, x1, z1, eaves, rise, ridgeAlongX, doors, open_=False):
    shelters.append(dict(id=sid, x0=x0, z0=z0, x1=x1, z1=z1, eaves=eaves, rise=rise, ridgeAlongX=ridgeAlongX,
                         doors=doors, open=open_))

shelter('front', -0.35, -0.35, 23.35, 12.0, FRONT_EAVES, FRONT_RIDGE - FRONT_EAVES, True, ['police-front-door', 'police-court-door'])
shelter('annex', -0.3, 12.0, 5.5, 18.0, WING_EAVES, WING_RIDGE - WING_EAVES, False, ['police-annex-door'])
shelter('archive', -0.3, 18.0, 5.5, 24.3, WING_EAVES, WING_RIDGE - WING_EAVES, False, ['police-archive-door'])
shelter('duty', 17.5, 12.0, 23.3, 16.0, WING_EAVES, WING_RIDGE - WING_EAVES, False, ['police-duty-door'])
shelter('evidence', 17.5, 16.0, 23.3, 19.6, WING_EAVES, WING_RIDGE - WING_EAVES, False, ['police-evidence-door'])
shelter('holding', 17.5, 19.6, 23.3, 24.3, WING_EAVES, WING_RIDGE - WING_EAVES, False, ['police-holding-door'])
shelter('loggia-west', 5.5, 12.0, 7.2, 24.15, 3.22, 0, False, [], True)
shelter('loggia-east', 15.8, 12.0, 17.5, 24.15, 3.22, 0, False, [], True)

# ---------------------------------------------------------------------------
# Derived opening data and validation.
# ---------------------------------------------------------------------------
W = {w['id']: w for w in walls}


def opening_centre(o):
    w = W[o['wall']]
    return (o['at'], w['c']) if w['axis'] == 'x' else (w['c'], o['at'])


errors = []
for o in openings:
    w = W[o['wall']]
    x, z = opening_centre(o)
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
for name, group in (('furniture', furniture), ('items', items), ('lamps', lamps), ('walls', walls), ('openings', openings)):
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
# Door swing and approach clearances must stay free of colliding furniture.
for o in openings:
    if o['kind'] not in ('door', 'arch'):
        continue
    w = W[o['wall']]
    level_y = G if o['bottom'] < 1 else U
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

# Agreement with the town map (B10 parts, doors, windows) in world coordinates.
plan = json.loads((ROOT / 'docs/maps/town-current/plan-data.json').read_text(encoding='utf-8'))
b10 = next(b for b in plan['buildings'] if b['id'] == 'B10')
parts = sorted(tuple(round(v, 3) for v in p['rect']) for p in b10['parts'])
expect = sorted([(12.5, 33.7, 23.0, 12.0), (3.75, 45.7, 5.5, 12.0), (21.25, 45.7, 5.5, 12.0)])
if parts != expect:
    errors.append(f'plan-data B10 parts differ: {parts}')
fd = next(o for o in openings if o['id'] == 'front-door')
cd = next(o for o in openings if o['id'] == 'court-door')
if [round(fd['x'] + ORIGIN[0], 3), round(0 + ORIGIN[2], 3)] != [round(v, 3) for v in b10['door']]:
    errors.append('front door differs from plan-data door')
if [round(cd['x'] + ORIGIN[0], 3), round(12 + ORIGIN[2], 3)] != [round(v, 3) for v in b10['rearDoor']]:
    errors.append('court door differs from plan-data rearDoor')
side_of = {'F_S': 'S', 'F_E': 'E', 'F_N_C': 'N', 'F_N_U': 'N', 'WW_E': 'E', 'WW_N': 'N', 'EW_W': 'W', 'EW_E': 'E', 'EW_N': 'N'}
ext_windows = sorted((round(o['x'] + ORIGIN[0] if W[o['wall']]['axis'] == 'x' else W[o['wall']]['c'] + (.2 if side_of[o['wall']] == 'E' else -.2) + ORIGIN[0], 2),
                      round((W[o['wall']]['c'] + (.2 if side_of[o['wall']] == 'N' else -.2) if W[o['wall']]['axis'] == 'x' else o['z']) + ORIGIN[2], 2),
                      o['width'], side_of[o['wall']]) for o in openings if o['kind'] == 'window' and o['exterior'])
plan_windows = sorted((round(w[0], 2), round(w[1], 2), w[2], w[4]) for w in b10['windows'])
if ext_windows != plan_windows:
    missing = set(ext_windows) ^ set(plan_windows)
    errors.append(f'exterior windows differ from plan-data: {sorted(missing)}')

fin = [s for s in slabs if s['kind'] == 'finish']
for i, a in enumerate(fin):
    for b in fin[i + 1:]:
        if abs(a['y1'] - b['y1']) < .01 and min(a['x1'], b['x1']) - max(a['x0'], b['x0']) > .001 and min(a['z1'], b['z1']) - max(a['z0'], b['z0']) > .001:
            errors.append(f"floor finishes overlap (z-fighting): {a['id']} {b['id']}")
if errors:
    sys.exit('POLICE_LAYOUT_FAIL:\n' + '\n'.join(errors))

data = dict(version=1, id='B10', name='青川警署', scheme='D 内院回廊警署', origin=list(ORIGIN),
            levels=dict(ground=G, groundCeiling=G_CEIL, upper=U, upperCeiling=U_CEIL, frontEaves=FRONT_EAVES,
                        frontRidge=FRONT_RIDGE, wingCeiling=WING_CEIL, wingEaves=WING_EAVES, wingRidge=WING_RIDGE, court=COURT),
            walls=walls, openings=openings, rooms=rooms, slabs=slabs, stairs=stairs, roofs=roofs, columns=columns,
            beams=beams, furniture=furniture, lamps=lamps, items=items, shelters=shelters,
            notes='Unity building-local metres (+X east, +Y up, +Z north); root at world origin above; yaw degrees, front faces (sin yaw, cos yaw).')
text = json.dumps(data, ensure_ascii=False, indent=1) + '\n'
out = ROOT / 'assets/source/police'
out.mkdir(parents=True, exist_ok=True)
for path in (out / 'police-layout.json', ROOT / 'game/Assets/Resources/PoliceLayout.json'):
    path.write_bytes(text.encode('utf-8'))
print(json.dumps(dict(status='POLICE_LAYOUT_PASS', walls=len(walls), openings=len(openings),
                      doors=sum(o['leaves'] > 0 for o in openings), windows=sum(o['kind'] == 'window' for o in openings),
                      rooms=len(rooms), furniture=len(furniture), lamps=len(lamps), items=len(items),
                      shelters=len(shelters), stairSteps=len(stairs)), ensure_ascii=False))
