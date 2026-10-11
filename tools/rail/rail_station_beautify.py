"""Station B beautification pass (2026-10-10, user request: refine station per story/design, keep style).

Unity metres, station local origin. Every element is attached to a supporting face; no opening
in the shared shell changes (door/window counts, sizes and positions stay those of the map).
"""
import math

from rail_shapes import archivolt, frustum, globe, hanging_lantern, prism, pyramid, ring_band, wall_lantern

WINDOWS = (-12., -8., -4., 4., 8., 12.)
FACADES = ((-1, -6.12), (1, 6.12))  # outward sign, outer wall face z


def roof_under(z):
    """Underside of the hall's pitched slate slab."""
    return 6.717 + (3.125 - abs(z)) * .256


def lining(z):
    return roof_under(z) - .025


def _facade(b):
    b.set_module('Module_Station_Masonry')
    for side, fz in FACADES:
        name = 'N' if side > 0 else 'S'
        # Continuous sill course: the arched windows sit on one projecting stone line.
        for sx in (-1, 1):
            b.box(f'SillCourse_{name}{sx}', (sx * 8.47, 1.26, fz + side * .04), (13.18, .09, .08), 'RailStone', bevel=.006)
        # Impost band on the piers, aligned with the arch springing.
        spans = [(-15.0, -13.4), (-10.6, -9.4), (-6.6, -5.4), (-2.6, -1.92), (1.92, 2.6), (5.4, 6.6), (9.4, 10.6), (13.4, 15.0)]
        for i, (x0, x1) in enumerate(spans):
            b.box(f'ImpostBand_{name}{i}', ((x0 + x1) / 2, 3.0, fz + side * .025), (x1 - x0, .08, .05), 'RailStone', bevel=.004)
        for wx in WINDOWS:
            archivolt(b, f'Archivolt_{name}_{wx:.0f}', wx, 3.0, fz, side, 1.105, 1.40, .05, blocks=9)
            # Projecting sill block under each glazed opening.
            b.box(f'WindowSill_{name}_{wx:.0f}', (wx, 1.315, fz + side * .055), (2.42, .05, .11), 'RailStone', bevel=.006)
            for sx in (-1, 1):
                b.box(f'SillBracket_{name}_{wx:.0f}_{sx}', (wx + sx * 1.0, 1.17, fz + side * .035), (.12, .16, .07), 'RailStone', bevel=.006)
        # Door architrave (outer face only; the opening itself is unchanged).
        for sx in (-1, 1):
            b.box(f'DoorArchitrave_{name}{sx}', (sx * 1.81, 1.86, fz + side * .03), (.22, 3.52, .06), 'RailStone', bevel=.006)
        b.box(f'DoorArchitraveHead_{name}', (0, 3.61, fz + side * .035), (3.84, .22, .07), 'RailStone', bevel=.006)
        b.box(f'DoorArchitraveKey_{name}', (0, 3.64, fz + side * .06), (.34, .30, .08), 'RailStone', bevel=.006)
        # Rose window surround with four keystones.
        ring_band(b, f'RoseSurround_{name}', 0, 4.95, fz, side, .86, 1.04, .045, 'RailStone', segments=32)
        for k in (0, 2, 3):  # no top key: it would cut the cornice bed above
            a = k * math.pi / 2
            prism(b, f'RoseKey_{name}_{k}', [(math.cos(a) * .84 + math.cos(a + math.pi / 2) * .08, 4.95 + math.sin(a) * .84 + math.sin(a + math.pi / 2) * .08, fz),
                                             (math.cos(a) * 1.10 + math.cos(a + math.pi / 2) * .11, 4.95 + math.sin(a) * 1.10 + math.sin(a + math.pi / 2) * .11, fz),
                                             (math.cos(a) * 1.10 - math.cos(a + math.pi / 2) * .11, 4.95 + math.sin(a) * 1.10 - math.sin(a + math.pi / 2) * .11, fz),
                                             (math.cos(a) * .84 - math.cos(a + math.pi / 2) * .08, 4.95 + math.sin(a) * .84 - math.sin(a + math.pi / 2) * .08, fz)],
                  (0, 0, side * .07), 'RailStone')
        # Bed moulding and modillions carried by the wall under the main cornice.
        for sx in (-1, 1):
            b.box(f'CorniceBed_{name}{sx}', (sx * 8.06, 5.82, fz + side * .03), (13.88, .05, .06), 'RailStone', bevel=.004)
        for i in range(50):
            x = -14.7 + i * .6
            b.box(f'Modillion_{name}_{i}', (x, 5.905, fz + side * .06), (.10, .12, .12), 'RailStone', bevel=.004)
    # Corner quoins wrap the hall corners and close the original 12cm corner notch.
    def quoins(prefix, corner_x, corner_z, xs, zs, top, return_len):
        y = .46
        course = 0
        while y + .42 <= top + .01:
            long = course % 2 == 0
            a = .62 if long else .38
            c = return_len[0] if long else return_len[1]
            b.box(f'{prefix}_F{course}', (corner_x + xs * (.04 - a / 2), y + .2, corner_z + zs * .02), (a, .40, .04), 'RailStone', bevel=.006)
            b.box(f'{prefix}_R{course}', (corner_x + xs * .02, y + .2, corner_z + zs * (.04 - c / 2)), (.04, .40, c), 'RailStone', bevel=.006)
            y += .43
            course += 1
    for xs in (-1, 1):
        for zs in (-1, 1):
            quoins(f'HallQuoin_{xs}{zs}', xs * 15.12, zs * 6.12, xs, zs, 5.78, (.55, .32))
            quoins(f'AnnexQuoin_{xs}{zs}', xs * 23.12, zs * 4.12, xs, zs, 3.9, (.55, .32))
    # Annex window architraves and sills (rectangular openings, unchanged sizes).
    for xs in (-1, 1):
        cx = xs * 19.0
        ox = xs * 23.12
        for zw in (-2.0, 2.0):
            for sz in (-1, 1):
                b.box(f'AnnexOuterJamb_{xs}_{zw:.0f}_{sz}', (ox + xs * .025, 2.25, zw + sz * .78), (.05, 2.02, .16), 'RailStone', bevel=.005)
            b.box(f'AnnexOuterHead_{xs}_{zw:.0f}', (ox + xs * .03, 3.31, zw), (.06, .16, 1.88), 'RailStone', bevel=.005)
            b.box(f'AnnexOuterSill_{xs}_{zw:.0f}', (ox + xs * .05, 1.265, zw), (.10, .07, 1.74), 'RailStone', bevel=.005)
        for zs in (-1, 1):
            fz = zs * 4.12
            for sx in (-1, 1):
                b.box(f'AnnexSideJamb_{xs}_{zs}_{sx}', (cx + sx * .78, 2.25, fz + zs * .025), (.16, 2.02, .05), 'RailStone', bevel=.005)
            b.box(f'AnnexSideHead_{xs}_{zs}', (cx, 3.31, fz + zs * .03), (1.88, .16, .06), 'RailStone', bevel=.005)
            b.box(f'AnnexSideSill_{xs}_{zs}', (cx, 1.265, fz + zs * .05), (1.74, .07, .10), 'RailStone', bevel=.005)
        # Eaves fascia on the annex.
        for zs in (-1, 1):
            b.box(f'AnnexEavesBand_{xs}_{zs}', (xs * 19.135, 3.93, zs * 4.15), (8.03, .12, .07), 'RailStone', bevel=.004)
    # Granite base on the exposed hall end walls (the facade plinths themselves become granite).
    b.set_module('Module_Station_Base')
    for xs in (-1, 1):
        for zs in (-1, 1):
            b.box(f'GraniteHallEnd_{xs}_{zs}', (xs * 15.15, .27, zs * 5.06), (.06, .34, 2.12), 'RailGranite', bevel=.006)


def _roofscape(b):
    b.set_module('Module_Station_Roofscape')
    # Cast-iron ridge cresting and gable finials.
    b.beam('RidgeCrestRail', (-15.3, 7.735, 0), (15.3, 7.735, 0), .014, 'RailIron', vertices=6)
    for i in range(52):
        x = -15.3 + i * .6
        b.box(f'RidgeCrest_{i}', (x, 7.84, 0), (.024, .17, .024), 'RailIron', bevel=0)
        b.ring(f'RidgeCrestLoop_{i}', (x, 7.86, 0), .045, .006, 'RailIron', axis=(0, 0, 1), segments=10, ring_segs=4)
        b.box(f'RidgeCrestTip_{i}', (x, 7.95, 0), (.05, .05, .05), 'RailIron', bevel=.01)
    for xs in (-1, 1):
        b.cylinder(f'GableFinialBase_{xs}', (xs * 15.18, 7.82, 0), .07, .16, 'RailIron', axis=(0, 1, 0), vertices=10)
        b.cylinder(f'GableFinialSpire_{xs}', (xs * 15.18, 8.12, 0), .022, .46, 'RailIron', axis=(0, 1, 0), vertices=8)
        globe(b, f'GableFinialBall_{xs}', (xs * 15.18, 7.98, 0), .07, 'RailIron', lat=5, lon=10)
        b.box(f'AnnexRidgeFinial_{xs}', (xs * 23.2, 5.42, 0), (.06, .30, .06), 'RailIron', bevel=.01)
    # Stone and brick stacks for the two real stoves below.
    def stack(name, x, z, bottom, top, w):
        b.box(name + '_Shaft', (x, (bottom + top) / 2, z), (w, top - bottom, w), 'RailBrick', bevel=.008)
        b.box(name + '_Band', (x, top - .26, z), (w + .08, .07, w + .08), 'RailStone', bevel=.006)
        b.box(name + '_Cap', (x, top + .045, z), (w + .12, .09, w + .12), 'RailStone', bevel=.008)
        for dx in (-w * .22, w * .22):
            frustum(b, f'{name}_Pot{dx:.2f}', (x + dx, top + .09, z), .11, .085, .34, 'RailClay', segments=12)
    stack('HallStoveChimney', -13.1, 4.35, 6.47, 8.35, .64)
    stack('DutyStoveChimney', 22.3, 0, 5.0, 6.15, .56)
    # Rain-water goods: eaves gutters and iron downpipes at the hall corners.
    for side in (-1, 1):
        # The front gutter stops either side of the pediment so it never hides the QINGCHUAN board.
        for sx, (x0, x1) in ((-1, (-15.45, -3.75)), (1, (3.75, 15.45))) if side < 0 else ((0, (-15.45, 15.45)),):
            b.box(f'HallGutter_{side}_{sx}', ((x0 + x1) / 2, 6.03, side * 6.36), (x1 - x0, .10, .12), 'RailIron', bevel=.01)
        for xs in (-1, 1):
            b.beam(f'HallDownpipe_{side}_{xs}', (xs * 14.55, 6.0, side * 6.36), (xs * 14.55, .12, side * 6.36), .045, 'RailIron', vertices=10)
            b.cylinder(f'HallDownpipeShoe_{side}_{xs}', (xs * 14.55, .16, side * 6.42), .05, .14, 'RailIron', axis=(0, 0, side), vertices=10)
            for y in (1.6, 3.2, 4.8):
                b.box(f'HallDownpipeClip_{side}_{xs}_{y}', (xs * 14.55, y, side * 6.24), (.11, .05, .18), 'RailIron', bevel=.004)


def _lamps(b):
    """Replaces the cylinder lamp heads with period lanterns; bulbs stay in their bound modules."""
    b.set_module('Module_Facade_Lamps')
    centres = {}
    for x in (-10, -6, 6, 10):
        centres[x] = wall_lantern(b, f'FacadeLantern{x}', (x, 3.36, -6.12), (0, -1), size=.24, height=.32)
    for sx in (-1, 1):
        # Hung from actual glazing ribs of the front porch and rear covered way.
        hanging_lantern(b, f'PorchLantern{sx}', (sx * 1.33, 3.24, -6.67), 3.738, size=.20, height=.26)
        hanging_lantern(b, f'RearWayLantern{sx}', (sx * 1.32, 3.24, 7.45), 3.768, size=.20, height=.26)
    b.set_module('Module_Hall_Lamps')
    for x, y in ((-8., 4.5), (-3., 4.8), (3., 4.8), (8., 4.5)):
        top = lining(0) - .145  # underside of the ridge beam
        b.cylinder(f'HallPendantRose_{x:.0f}', (x, top - .02, 0), .16, .04, 'RailBrass', axis=(0, 1, 0), vertices=16)
        b.beam(f'HallPendantRod_{x:.0f}', (x, top - .04, 0), (x, y + .34, 0), .014, 'RailBrass', vertices=8)
        b.cylinder(f'HallPendantGallery_{x:.0f}', (x, y + .30, 0), .13, .08, 'RailBrass', axis=(0, 1, 0), vertices=16)
        b.ring(f'HallPendantCollar_{x:.0f}', (x, y + .25, 0), .15, .012, 'RailBrass', axis=(0, 1, 0), segments=16, ring_segs=5)
        globe(b, f'HallPendantGlobe_{x:.0f}', (x, y + .03, 0), .22, 'RailGlow', lat=7, lon=14, squash=.92)
        b.cylinder(f'HallPendantFinial_{x:.0f}', (x, y - .21, 0), .03, .06, 'RailBrass', axis=(0, 1, 0), vertices=8)
    # Interior wall sconces on the solid piers (glow bound with the hall lamps).
    for x, z, out in ((-10, -5.88, 1), (-6, -5.88, 1), (6, -5.88, 1), (10, -5.88, 1), (-10, 5.88, -1), (-6, 5.88, -1), (6, 5.88, -1)):
        wall_lantern(b, f'HallSconce_{x}_{out}', (x, 3.12, z), (0, out), size=.17, height=.22)
    return centres


def _hall_interior(b):
    b.set_module('Module_Hall_Ceiling')
    # Boarded ceiling lining under each roof slope; real faces between trusses, no black void.
    for side in (-1, 1):
        pts = []
        for x in (-14.88, 14.88):
            for z in (0.0, side * 5.88):
                pts.append((x, lining(z), z))
                pts.append((x, lining(z) + .02, z))
        # Box corner order: (x0,z0,lo),(x0,z0,hi),(x0,z1,lo),(x0,z1,hi),(x1,...)
        faces = [(0, 2, 6, 4), (1, 5, 7, 3), (0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6)]
        b.authored_mesh(f'HallCeilingLining_{side}', pts, faces, 'RailCeiling')
        for zz in (1.95, 3.95):
            z = side * zz
            b.box(f'HallPurlin_{side}_{zz}', (0, lining(z) - .065, z), (29.74, .12, .12), 'RailWalnut', bevel=.006)
    b.box('HallRidgeBeam', (0, lining(0) - .075, 0), (29.74, .14, .16), 'RailWalnut', bevel=.006)
    for side in (-1, 1):
        b.box(f'HallCorniceLong_{side}', (0, 5.86, side * 5.82), (29.76, .10, .12), 'RailWalnut', bevel=.008)
        b.box(f'HallCorniceLongCap_{side}', (0, 5.925, side * 5.80), (29.76, .03, .16), 'RailWalnut', bevel=.004)
    for xs in (-1, 1):
        b.box(f'HallCorniceEnd_{xs}', (xs * 14.82, 5.86, 0), (.12, .10, 11.52), 'RailWalnut', bevel=.008)
    # King-post struts, wall corbels with knee braces and turned pendant drops.
    for tx in (-10., -5., 0., 5., 10.):
        for side in (-1, 1):
            z_mid = side * 2.9
            b.beam(f'TrussStrut_{tx:.0f}_{side}', (tx, 6.05, 0), (tx, lining(z_mid) - .10, z_mid), .055, 'RailWalnut', vertices=8)
        b.cylinder(f'TrussDropCollar_{tx:.0f}', (tx, 5.74, 0), .075, .08, 'RailWalnut', axis=(0, 1, 0), vertices=10)
        globe(b, f'TrussDrop_{tx:.0f}', (tx, 5.64, 0), .07, 'RailWalnut', lat=5, lon=10, squash=1.2)
        if tx == 0:
            continue  # The rose window occupies this bay of wall.
        for side in (-1, 1):
            b.box(f'TrussCorbel_{tx:.0f}_{side}', (tx, 5.50, side * 5.80), (.30, .34, .16), 'RailStone', bevel=.01)
            b.beam(f'TrussKnee_{tx:.0f}_{side}', (tx, 5.40, side * 5.72), (tx, 5.88, side * 4.75), .05, 'RailWalnut', vertices=8)


def _stove(b, name, base, flue_top, facing):
    """Cast-iron waiting-room stove on a granite hearth; ember window is constant, not a lamp."""
    x, y, z = base
    fx, fz = facing
    b.box(name + '_Hearth', (x, y + .025, z), (.96, .05, .96), 'RailGranite', bevel=.008)
    for sx in (-1, 1):
        for sz in (-1, 1):
            b.box(f'{name}_Leg{sx}{sz}', (x + sx * .19, y + .11, z + sz * .19), (.06, .12, .06), 'RailIron', bevel=.006)
    b.cylinder(name + '_Plinth', (x, y + .20, z), .29, .06, 'RailIron', axis=(0, 1, 0), vertices=8)
    b.cylinder(name + '_Body', (x, y + .58, z), .27, .70, 'RailIron', axis=(0, 1, 0), vertices=8)
    for yy in (.30, .86):
        b.ring(f'{name}_Band{yy}', (x, y + yy, z), .275, .012, 'RailBrass', axis=(0, 1, 0), segments=8, ring_segs=4)
    frustum(b, name + '_Shoulder', (x, y + .93, z), .30, .17, .14, 'RailIron', segments=8)
    b.cylinder(name + '_Collar', (x, y + 1.10, z), .11, .06, 'RailIron', axis=(0, 1, 0), vertices=10)
    globe(b, name + '_Urn', (x + fx * .19, y + 1.08, z + fz * .19), .055, 'RailBrass', lat=4, lon=8)
    # Fire door with a glowing mica window facing the room.
    door = (x + fx * .272, y + .55, z + fz * .272)
    b.box(name + '_DoorFrame', door, (.30 if fz else .03, .26, .03 if fz else .30), 'RailIron', bevel=.004)
    b.box(name + '_Embers', (door[0] + fx * .012, door[1], door[2] + fz * .012), (.20 if fz else .012, .15, .012 if fz else .20), 'RailEmber', bevel=0)
    b.box(name + '_DoorHandle', (door[0] + fx * .03 + fz * .11, door[1], door[2] + fz * .03 - fx * .11), (.05, .02, .05), 'RailBrass', bevel=.004)
    b.beam(name + '_Flue', (x, y + 1.10, z), (x, flue_top, z), .075, 'RailIron', vertices=10)
    for yy in (1.9, 3.1):
        b.ring(f'{name}_FlueJoint{yy}', (x, yy, z), .078, .008, 'RailIron', axis=(0, 1, 0), segments=10, ring_segs=4)
    b.cylinder(name + '_CeilingCollar', (x, flue_top - .02, z), .13, .04, 'RailIron', axis=(0, 1, 0), vertices=10)
    # Coal scuttle and poker stand beside it.
    sx, sz = x - fz * .55, z + fx * .55
    frustum(b, name + '_Scuttle', (sx, y + .0, sz), .12, .16, .30, 'RailIron', segments=10)
    b.cylinder(name + '_ScuttleCoal', (sx, y + .27, sz), .14, .03, 'RailCoal', axis=(0, 1, 0), vertices=10)
    b.ring(name + '_ScuttleBail', (sx, y + .34, sz), .14, .007, 'RailBrass', axis=(fx, 0, fz) if (fx or fz) else (1, 0, 0), segments=10, ring_segs=4)


def _rear_covered_way(b):
    """Glazed covered way from the rear hall door to the train shed (x +-2, z 6.12..8.95)."""
    b.set_module('Module_Rear_CoveredWay')
    y = 3.72
    for sx in (-1, 1):
        x = sx * 2.0
        b.box(f'RearWayPlinth_{sx}', (x, .17, 8.85), (.26, .14, .26), 'RailGranite', bevel=.006)
        b.cylinder(f'RearWayColumn_{sx}', (x, 1.93, 8.85), .075, 3.40, 'RailIron', axis=(0, 1, 0), vertices=12)
        b.ring(f'RearWayColumnRing_{sx}', (x, .45, 8.85), .09, .012, 'RailIron', axis=(0, 1, 0), segments=12, ring_segs=4)
        frustum(b, f'RearWayCapital_{sx}', (x, 3.58, 8.85), .08, .14, .10, 'RailIron', segments=12)
        b.beam(f'RearWaySideBeam_{sx}', (x, y, 6.10), (x, y, 8.92), .04, 'RailIron', vertices=8)
        b.beam(f'RearWayBracket_{sx}', (x, 3.05, 6.12), (x, y - .03, 6.9), .022, 'RailIron', vertices=6)
        b.ring(f'RearWayScroll_{sx}', (x, 3.42, 6.42), .2, .011, 'RailIron', axis=(1, 0, 0), segments=16, ring_segs=5)
        b.box(f'RearWayWallPlate_{sx}', (x, 3.35, 6.135), (.10, .90, .03), 'RailIron', bevel=.003)
    b.beam('RearWayFrontBeam', (-2.04, y, 8.92), (2.04, y, 8.92), .045, 'RailIron', vertices=8)
    b.box('RearWayWallBeam', (0, y + .07, 6.15), (4.04, .07, .06), 'RailIron', bevel=.003)
    b.box('RearWayGlass', (0, y + .055, 7.53), (3.96, .015, 2.76), 'RailShedGlass', bevel=0)
    for x in (-1.32, -.66, 0, .66, 1.32):
        b.beam(f'RearWayRib_{x:.2f}', (x, y + .08, 6.15), (x, y + .04, 8.9), .012, 'RailIron', vertices=6)
    for i in range(18):
        b.box(f'RearWayValance_{i}', (-1.92 + i * .226, y - .09, 8.94), (.15, .14, .02), 'RailGreen', bevel=.002)


def _shed(b):
    R, yc, zc = 17.125, -7.725, 20.0

    def arc_y(z):
        return yc + math.sqrt(max(0, R * R - (z - zc) ** 2))

    b.set_module('Module_Shed_Detail2')
    # Transverse glazing bars give the glass a readable pane grid.
    trusses = [-34, -30, -20, -10, 0, 10, 20, 30, 34]
    segs = 16
    edges = [9 + 22 * i / segs for i in range(segs + 1)]
    for k in range(1, 40):
        x = -34 + 1.7 * k
        if min(abs(x - t) for t in trusses) < .35:
            continue
        for i in range(2, segs - 2):
            z0, z1 = edges[i], edges[i + 1]
            b.beam(f'GlazingBar_{k}_{i}', (x, arc_y(z0) + .085, z0), (x, arc_y(z1) + .085, z1), .016, 'RailIron', vertices=4)
    # Ridge ventilator lantern with louvred sides.
    for zs in (-1, 1):
        z = 20 + zs * 1.1
        base = arc_y(z) + .04
        for yy in (0.0, .46):
            b.box(f'RidgeVentRail_{zs}_{yy}', (0, base + yy + .03, z), (66.0, .06, .06), 'RailZinc', bevel=.004)
        for j, yy in enumerate((.13, .24, .35)):
            b.box(f'RidgeVentLouvre_{zs}_{j}', (0, base + yy + .03, z + zs * .02), (66.0, .05, .12), 'RailZinc', bevel=.004)
        for x in range(-33, 34, 3):
            b.box(f'RidgeVentPost_{zs}_{x}', (x, base + .26, z), (.06, .52, .07), 'RailIron', bevel=.003)
    top = arc_y(18.9) + .04 + .52 - .055
    for zs in (-1, 1):
        pts = []
        for x in (-33.2, 33.2):
            for z, y in ((20.0, top + .30), (20 + zs * 1.35, top)):
                pts += [(x, y, z), (x, y + .05, z)]
        faces = [(0, 2, 6, 4), (1, 5, 7, 3), (0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6)]
        b.authored_mesh(f'RidgeVentRoof_{zs}', pts, faces, 'RailZinc')
    for xs in (-1, 1):
        prism(b, f'RidgeVentEnd_{xs}', [(xs * 33.2, arc_y(18.9) + .04, 18.9), (xs * 33.2, top + .02, 18.9), (xs * 33.2, top + .32, 20.0),
                                        (xs * 33.2, top + .02, 21.1), (xs * 33.2, arc_y(18.9) + .04, 21.1)], (xs * .05, 0, 0), 'RailZinc')
    # Glazed end lunettes ("sunburst" screens) above the tie rod; train clearance below 5.4m.
    for xs in (-1, 1):
        x = xs * 34.12
        lower = [(x, arc_y(9 + 22 * i / 32) - .45, 9 + 22 * i / 32) for i in range(33)]
        lower = [(x, 5.42, 9.2)] + [p for p in lower if p[1] > 5.44] + [(x, 5.42, 30.8)]
        prism(b, f'EndLunetteGlass_{xs}', lower, (xs * .02, 0, 0), 'RailShedGlass')
        hub = (x + xs * .04, 5.44, 20.0)
        for k in range(1, 12):
            a = math.pi * k / 12
            z = 20 - math.cos(a) * 10.8
            y = max(5.42, arc_y(z) - .45)
            b.beam(f'EndLunetteSpoke_{xs}_{k}', hub, (x + xs * .04, y, z), .022, 'RailIron', vertices=6)
        ring_pts = []
        for k in range(13):
            a = math.pi * k / 12
            ring_pts.append((x + xs * .04, 5.44 + math.sin(a) * 1.6, 20 - math.cos(a) * 1.6))
        for k in range(12):
            b.beam(f'EndLunetteHub_{xs}_{k}', ring_pts[k], ring_pts[k + 1], .03, 'RailIron', vertices=6)
        b.beam(f'EndLunetteSill_{xs}', (x + xs * .04, 5.44, 9.2), (x + xs * .04, 5.44, 30.8), .035, 'RailIron', vertices=8)
        # Painted timber valance boards with pointed ends hanging below the tie rod.
        z = 9.3
        i = 0
        while z < 30.7:
            prism(b, f'EndValance_{xs}_{i}', [(x, 5.40, z - .075), (x, 5.40, z + .075), (x, 4.98, z + .075), (x, 4.86, z), (x, 4.98, z - .075)],
                  (xs * .025, 0, 0), 'RailGreen')
            z += .18
            i += 1
    # Eaves gutters along both long sides, carried on the column capitals.
    for z in (9.0, 31.0):
        b.box(f'ShedGutter_{z:.0f}', (0, 5.52, z), (68.4, .16, .30), 'RailIron', bevel=.01)
    # Bar from which the platform sign hangs (the old hangers reached no structure).
    b.beam('Plat1HangerBar', (-1.48, 5.40, 18.0), (1.48, 5.40, 18.0), .02, 'RailIron', vertices=8)


def _luggage_room(b):
    """West annex (map: 行李房). Shelving keeps both windows and the hall passage clear."""
    b.set_module('Module_Annex_Luggage')
    cx = -19.0
    b.box('LuggageCeiling', (cx, 3.94, 0), (7.76, .04, 7.76), 'RailCeiling', bevel=0)
    for zs in (-1, 1):
        b.box(f'LuggageCornice_{zs}', (cx, 3.87, zs * 3.84), (7.76, .08, .08), 'RailWalnut', bevel=.004)
    b.box('LuggageCorniceEnd', (-22.84, 3.87, 0), (.08, .08, 7.6), 'RailWalnut', bevel=.004)
    for i, (x0, x1) in enumerate(((-22.82, -20.05), (-17.95, -15.18))):
        w = x1 - x0
        xm = (x0 + x1) / 2
        for ux in (x0 + .03, xm, x1 - .03):
            b.box(f'LuggageShelfUpright_{i}_{ux:.2f}', (ux, 1.25, 3.60), (.05, 2.30, .52), 'RailWalnut', bevel=.004)
        for j, y in enumerate((.16, .74, 1.32, 1.90, 2.38)):
            b.box(f'LuggageShelf_{i}_{j}', (xm, y, 3.60), (w, .035, .52), 'RailWalnut', bevel=.003)
        b.box(f'LuggageShelfBack_{i}', (xm, 1.27, 3.865), (w, 2.26, .02), 'RailWalnut', bevel=0)
        # Parcels and cases packed within each bay of each shelf (deterministic layout).
        pattern = [((.42, .30, .34), 'RailParcel'), ((.36, .22, .30), 'RailParcel'), ((.52, .32, .40), 'RailLeather'), ((.30, .36, .28), 'RailParcel'),
                   ((.44, .20, .32), 'RailRed'), ((.34, .24, .30), 'RailParcel'), ((.38, .16, .26), 'RailParcel'), ((.30, .30, .30), 'RailGreen')]
        k = i * 3
        for j, y in enumerate((.16, .74, 1.32, 1.90)):
            for bay0, bay1 in ((x0 + .06, xm - .03), (xm + .03, x1 - .06)):
                x = bay0 + .03
                while True:
                    size, mat = pattern[k % len(pattern)]
                    if x + size[0] > bay1:
                        break
                    centre = (x + size[0] / 2, y + .0175 + size[1] / 2, 3.60 - (k % 3) * .02)
                    b.box(f'LuggageShelfItem_{i}_{j}_{k}', centre, size, mat, bevel=.008)
                    if mat == 'RailParcel':
                        b.box(f'LuggageTwine_{i}_{j}_{k}', centre, (.012, size[1] + .006, size[2] + .006), 'RailIvory', bevel=0)
                    x += size[0] + .05
                    k += 1
                k += 1
    # Low staging bench with trunks under the south-west window side.
    b.box('LuggageStagingTop', (-21.5, .58, -3.50), (2.6, .06, .66), 'RailWalnut', bevel=.006)
    for x in (-22.7, -21.5, -20.3):
        for z in (-3.75, -3.25):
            b.box(f'LuggageStagingLeg_{x}_{z}', (x, .33, z), (.07, .46, .07), 'RailWalnut', bevel=.004)
    b.box('LuggageStagingTrunk', (-22.05, .82, -3.50), (.90, .42, .56), 'RailLeather', bevel=.01)
    for dx in (-.30, .30):
        b.box(f'LuggageStagingTrunkStrap_{dx}', (-22.05 + dx, .82, -3.50), (.04, .43, .57), 'RailWalnut', bevel=.002)
    b.box('LuggageStagingCase', (-21.0, .74, -3.50), (.62, .26, .40), 'RailRed', bevel=.01)
    b.box('LuggageStagingHatBox', (-20.45, .78, -3.48), (.32, .34, .32), 'RailParcel', bevel=.03)
    # Crates and a sack barrow in the south-east corner, clear of the passage (z +-1.4).
    for i, (x, y, z, s) in enumerate(((-16.0, .36, -3.40, (.62, .52, .62)), (-16.7, .30, -3.45, (.56, .40, .56)), (-16.05, .86, -3.42, (.48, .48, .48)))):
        b.box(f'LuggageCrate_{i}', (x, y, z), s, 'RailWalnut', bevel=.01)
        b.box(f'LuggageCrateBand_{i}', (x, y, z), (s[0] + .01, .05, s[2] + .01), 'RailIron', bevel=0)
    bx, bz = -17.55, -3.25
    for sz in (-1, 1):
        b.beam(f'SackBarrowRail_{sz}', (bx, .22, bz + sz * .2), (bx + .30, 1.30, bz + sz * .2), .018, 'RailIron', vertices=6)
        b.cylinder(f'SackBarrowWheel_{sz}', (bx - .02, .22, bz + sz * .26), .12, .04, 'RailIron', axis=(0, 0, 1), vertices=12)
    b.box('SackBarrowNose', (bx - .02, .13, bz), (.18, .02, .44), 'RailIron', bevel=.003)
    b.beam('SackBarrowAxle', (bx - .02, .22, bz - .28), (bx - .02, .22, bz + .28), .012, 'RailIron', vertices=6)
    # Parcel desk between the outer windows: ledger, bell and luggage labels.
    b.box('ParcelDeskTop', (-22.45, .88, 0), (.70, .05, 1.70), 'RailWalnut', bevel=.006)
    b.box('ParcelDeskBody', (-22.45, .48, 0), (.62, .76, 1.60), 'RailWalnut', bevel=.006)
    b.box('ParcelDeskLedger', (-22.40, .925, -.35), (.30, .04, .42), 'RailGreen', bevel=.004)
    b.cylinder('ParcelDeskBell', (-22.38, .93, .35), .045, .04, 'RailBrass', axis=(0, 1, 0), vertices=12)
    for k in range(4):
        b.box(f'ParcelLabel_{k}', (-22.48, .908, .02 + k * .07), (.09, .004, .05), 'RailIvory', bevel=0)
    b.set_module('Module_Annex_Lamps_W')
    hanging_lantern(b, 'LuggageLamp', (cx, 3.10, 0), 3.92, size=.24, height=.30)


def _duty_room(b):
    """East annex (map: 值班室). Standing desk, forms cabinet, stove, coat stand, flags."""
    b.set_module('Module_Annex_Duty')
    cx = 19.0
    b.box('DutyCeiling', (cx, 3.94, 0), (7.76, .04, 7.76), 'RailCeiling', bevel=0)
    for zs in (-1, 1):
        b.box(f'DutyCornice_{zs}', (cx, 3.87, zs * 3.84), (7.76, .08, .08), 'RailWalnut', bevel=.004)
    b.box('DutyCorniceEnd', (22.84, 3.87, 0), (.08, .08, 7.6), 'RailWalnut', bevel=.004)
    _stove(b, 'DutyStove', (22.3, .10, 0.0), 3.92, (-1, 0))
    # Standing clerk's desk with a sloped top, closed blank ledger and inkstand.
    b.box('DutyDeskBody', (21.0, .52, 3.55), (1.70, .84, .56), 'RailWalnut', bevel=.006)
    pts = [(20.13, .94, 3.27), (21.87, .94, 3.27), (21.87, 1.10, 3.84), (20.13, 1.10, 3.84)]
    prism(b, 'DutyDeskSlope', pts, (0, .04, 0), 'RailWalnut')
    b.box('DutyDeskRail', (21.0, 1.165, 3.81), (1.74, .05, .06), 'RailWalnut', bevel=.004)
    slope = lambda z: .98 + .16 * (z - 3.27) / .57  # top face of the sloped lid
    prism(b, 'DutyDeskLedger', [(20.54, slope(3.38), 3.38), (20.96, slope(3.38), 3.38), (20.96, slope(3.68), 3.68), (20.54, slope(3.68), 3.68)], (0, .035, 0), 'RailGreen')
    b.cylinder('DutyInkstand', (21.45, 1.215, 3.81), .04, .05, 'RailBrass', axis=(0, 1, 0), vertices=10)
    b.box('DutyDeskKick', (21.0, .14, 3.30), (1.70, .08, .06), 'RailIron', bevel=.004)
    # Forms cabinet with pigeonholes.
    b.box('DutyCabinetCarcass', (16.75, .95, 3.66), (2.20, 1.70, .40), 'RailWalnut', bevel=.006)
    for r in range(4):
        for c in range(6):
            b.box(f'DutyPigeonhole_{r}_{c}', (15.83 + c * .37, .78 + r * .25, 3.455), (.31, .20, .01), 'RailCoal', bevel=0)
            if (r + c) % 3 == 0:
                b.box(f'DutyForms_{r}_{c}', (15.83 + c * .37, .72 + r * .25, 3.447), (.26, .05, .006), 'RailIvory', bevel=0)
    b.box('DutyCabinetCornice', (16.75, 1.83, 3.64), (2.30, .06, .46), 'RailWalnut', bevel=.006)
    # Coat stand with a hung uniform coat and peaked cap.
    sx, sz = 22.30, -3.30
    b.cylinder('CoatStandPole', (sx, 1.0, sz), .03, 1.80, 'RailWalnut', axis=(0, 1, 0), vertices=8)
    for a in range(3):
        ang = a * math.tau / 3
        b.beam(f'CoatStandFoot_{a}', (sx, .16, sz), (sx + math.cos(ang) * .30, .11, sz + math.sin(ang) * .30), .02, 'RailWalnut', vertices=6)
        b.beam(f'CoatStandHook_{a}', (sx, 1.78, sz), (sx + math.cos(ang) * .16, 1.86, sz + math.sin(ang) * .16), .012, 'RailBrass', vertices=6)
    b.box('CoatStandUniform', (sx - .14, 1.30, sz + .02), (.20, .90, .42), 'RailGreen', bevel=.02)
    b.box('CoatStandCollar', (sx - .13, 1.74, sz + .02), (.16, .06, .22), 'RailGreen', bevel=.01)
    for k in range(3):
        b.box(f'CoatStandButton_{k}', (sx - .245, 1.48 - k * .18, sz + .02), (.012, .03, .03), 'RailBrass', bevel=0)
    b.cylinder('CoatStandCap', (sx + .02, 1.95, sz), .13, .09, 'RailGreen', axis=(0, 1, 0), vertices=12)
    b.box('CoatStandCapPeak', (sx - .10, 1.915, sz), (.12, .015, .20), 'RailCoal', bevel=.004)
    # Flag holder (rolled red and green hand flags) and hand lamp on a wall shelf.
    b.cylinder('FlagHolder', (15.55, .40, -3.70), .06, .60, 'RailIron', axis=(0, 1, 0), vertices=10)
    for k, mat in enumerate(('RailRed', 'RailGreen')):
        b.cylinder(f'FlagStaff_{k}', (15.53 + k * .05, 1.10, -3.70), .012, 1.10, 'RailWalnut', axis=(0, 1, 0), vertices=6)
        b.cylinder(f'FlagRoll_{k}', (15.53 + k * .05, 1.38, -3.70), .03, .42, mat, axis=(0, 1, 0), vertices=8)
    b.box('DutyWallShelf', (16.55, 1.62, -3.74), (.80, .03, .24), 'RailWalnut', bevel=.004)
    b.box('DutyShelfBracket', (16.55, 1.53, -3.82), (.04, .16, .08), 'RailWalnut', bevel=.004)
    b.cylinder('HandLampBody', (16.40, 1.735, -3.74), .07, .20, 'RailIron', axis=(0, 1, 0), vertices=10)
    b.cylinder('HandLampLens', (16.40, 1.75, -3.665), .045, .02, 'RailRed', axis=(0, 0, 1), vertices=10)
    b.ring('HandLampBail', (16.40, 1.89, -3.74), .06, .006, 'RailIron', axis=(0, 0, 1), segments=10, ring_segs=4)
    # Key board with brass hooks.
    b.box('KeyBoard', (17.35, 1.62, -3.865), (.80, .42, .03), 'RailWalnut', bevel=.004)
    for k in range(6):
        x = 17.05 + (k % 3) * .3
        y = 1.72 - (k // 3) * .2
        b.box(f'KeyHook_{k}', (x, y, -3.84), (.016, .016, .04), 'RailBrass', bevel=0)
        b.box(f'KeyTag_{k}', (x, y - .06, -3.835), (.035, .07, .006), 'RailBrass', bevel=0)
    b.set_module('Module_Annex_Lamps_E')
    hanging_lantern(b, 'DutyLamp', (cx, 3.10, 0), 3.92, size=.24, height=.30)


def add_station_beautify(b):
    print('>>> Station beautification pass...')
    _facade(b)
    _roofscape(b)
    lanterns = _lamps(b)
    _hall_interior(b)
    b.set_module('Module_Hall_Stove')
    _stove(b, 'HallStove', (-13.1, .10, 4.35), lining(4.35), (0, -1))
    _rear_covered_way(b)
    _shed(b)
    _luggage_room(b)
    _duty_room(b)
    return lanterns
