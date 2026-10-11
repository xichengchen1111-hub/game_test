"""Train beautification pass (2026-10-10): locomotive, tender, coaches and van.

Unity metres in train-local coordinates (faces +X). Moving parts (wheels, side rods, doors,
lamp switch knobs) keep their own pivots; nothing here enters a coach window aperture, the
0.8m aisle, the boarding doorways or the departure seat.
"""
import math

from rail_shapes import frustum, globe, prism

COACHES = ((0.0, 'C1'), (-16.0, 'C2'))
L = .09  # WHEEL_LIFT in build_rail_assets: running gear stands on the rail heads
WINDOWS = (-4.5, -2.7, -.9, .9, 2.7, 4.5)
BAYS = (-4.5, -2.7, -.9, .9, 2.7)


def _lining_rect(b, name, x0, x1, y0, y1, plane, mat='RailBrass', w=.016, t=.006, axis='z'):
    """Thin painted lining rectangle on a flat side; plane is the outer face coordinate."""
    if axis == 'z':
        s = 1 if plane > 0 else -1
        z = plane + s * t / 2
        b.box(name + '_T', ((x0 + x1) / 2, y1, z), (x1 - x0, w, t), mat, bevel=0)
        b.box(name + '_B', ((x0 + x1) / 2, y0, z), (x1 - x0, w, t), mat, bevel=0)
        b.box(name + '_L', (x0, (y0 + y1) / 2, z), (w, y1 - y0, t), mat, bevel=0)
        b.box(name + '_R', (x1, (y0 + y1) / 2, z), (w, y1 - y0, t), mat, bevel=0)


def _locomotive(b):
    b.set_module('Module_Loco_Beauty')
    # Running plates either side of the boiler, broken where the driving wheels rise through.
    pieces = [(16.80, 16.80), (17.90, 18.50), (19.70, 20.30), (21.50, 25.66)]
    for side in (-1, 1):
        for i, (x0, x1) in enumerate(pieces):
            if x1 - x0 < .1:
                continue
            b.box(f'RunningPlate_{side}_{i}', ((x0 + x1) / 2, 1.28, side * 1.01), (x1 - x0, .04, .62), 'RailIron', bevel=.004)
            b.box(f'Valance_{side}_{i}', ((x0 + x1) / 2, 1.18, side * 1.33), (x1 - x0, .20, .025), 'RailIron', bevel=.003)
            b.box(f'ValanceLining_{side}_{i}', ((x0 + x1) / 2, 1.13, side * 1.345), (x1 - x0 - .04, .012, .005), 'RailRed', bevel=0)
        # Splashers over the three driving axles, with brass beading.
        for ax in (17.3, 19.1, 20.9):
            a0, a1 = math.radians(38), math.radians(142)
            a0 = math.asin((1.26 - .73 - L) / .82)
            a1 = math.pi - a0
            arc = [(ax + .82 * math.cos(a0 + (a1 - a0) * k / 10), .73 + L + .82 * math.sin(a0 + (a1 - a0) * k / 10), side * 1.07) for k in range(11)]
            prism(b, f'Splasher_{side}_{ax}', arc, (0, 0, -side * .24), 'RailIron')
            for k in range(10):
                p, q = arc[k], arc[k + 1]
                b.beam(f'SplasherBead_{side}_{ax}_{k}', (p[0], p[1], side * 1.075), (q[0], q[1], side * 1.075), .011, 'RailBrass', vertices=6)
        # Sandboxes ahead of the leading splasher and lubricator-side steps.
        b.box(f'Sandbox_{side}', (21.65, 1.43, side * 1.02), (.30, .26, .26), 'RailIron', bevel=.02)
        b.cylinder(f'SandboxLid_{side}', (21.65, 1.58, side * 1.02), .07, .04, 'RailBrass', axis=(0, 1, 0), vertices=10)
        for x in (15.05, 25.35):
            for k, y in enumerate((.62, .90)):
                b.box(f'Step_{side}_{x}_{k}', (x, y, side * 1.40), (.32, .03, .20), 'RailIron', bevel=.003)
            b.box(f'StepHanger_{side}_{x}', (x, .90, side * 1.48), (.30, .60, .02), 'RailIron', bevel=.002)
    # Smokebox saddle, cylinder covers and crosshead blocks.
    b.box('SmokeboxSaddle', (23.3, 1.03, 0), (1.25, .32, 1.10), 'RailIron', bevel=.02)
    for side in (-1, 1):
        b.cylinder(f'CylinderCover_{side}', (23.07, .75 + L, side * 1.15), .37, .04, 'RailIron', axis=(1, 0, 0), vertices=20)
        b.ring(f'CylinderCoverRim_{side}', (23.09, .75 + L, side * 1.15), .33, .014, 'RailBrass', axis=(1, 0, 0), segments=20, ring_segs=5)
        b.cylinder(f'CylinderBackCover_{side}', (21.93, .75 + L, side * 1.15), .30, .04, 'RailIron', axis=(1, 0, 0), vertices=16)
        b.box(f'Crosshead_{side}', (21.75, .75 + L, side * 1.15), (.20, .16, .10), 'RailIron', bevel=.01)
        b.beam(f'PistonRod_{side}', (21.85, .75 + L, side * 1.15), (21.93, .75 + L, side * 1.15), .03, 'RailIron', vertices=8)
    # Belpaire firebox shoulders, safety valves and a copper-capped chimney.
    b.box('FireboxCasing', (17.45, 2.12, 0), (1.30, 1.36, 1.64), 'RailIron', bevel=.06)
    b.box('FireboxBand', (18.11, 2.12, 0), (.03, 1.38, 1.66), 'RailBrass', bevel=.01)
    b.cylinder('SafetyValveBase', (17.45, 2.86, 0), .13, .08, 'RailBrass', axis=(0, 1, 0), vertices=14)
    for dz in (-.05, .05):
        b.cylinder(f'SafetyValve_{dz}', (17.45, 3.00, dz), .035, .22, 'RailBrass', axis=(0, 1, 0), vertices=10)
    frustum(b, 'ChimneyCopperCap', (23.0, 3.46, 0), .25, .31, .16, 'RailCopper', segments=20)
    b.ring('ChimneyRoll', (23.0, 3.62, 0), .31, .02, 'RailCopper', axis=(0, 1, 0), segments=20, ring_segs=6)
    # Arched cab roof with overhang (replaces the flat plate), gutter strips and lookout rims.
    n = 9
    verts = []
    for x in (14.62, 16.98):
        for i in range(n):
            z = -1.40 + 2.80 * i / (n - 1)
            y = 3.46 + .16 * math.cos(z / 1.40 * math.pi / 2)
            verts += [(x, y, z), (x, y + .045, z)]
    v = lambda xi, i, hi: (xi * n + i) * 2 + hi
    faces = []
    for i in range(n - 1):
        faces += [(v(0, i, 0), v(0, i + 1, 0), v(1, i + 1, 0), v(1, i, 0)), (v(0, i, 1), v(1, i, 1), v(1, i + 1, 1), v(0, i + 1, 1)),
                  (v(0, i, 0), v(0, i, 1), v(0, i + 1, 1), v(0, i + 1, 0)), (v(1, i, 0), v(1, i + 1, 0), v(1, i + 1, 1), v(1, i, 1))]
    faces += [(v(0, 0, 0), v(1, 0, 0), v(1, 0, 1), v(0, 0, 1)), (v(0, n - 1, 0), v(0, n - 1, 1), v(1, n - 1, 1), v(1, n - 1, 0))]
    b.authored_mesh('CabArchedRoof', verts, faces, 'RailIron')
    for side in (-1, 1):
        b.box(f'CabSideTop_{side}', (15.8, 3.355, side * 1.30), (2.0, .21, .06), 'RailGreen', bevel=.004)
    for side in (-1, 1):
        b.beam(f'CabGutter_{side}', (14.62, 3.47, side * 1.38), (16.98, 3.47, side * 1.38), .014, 'RailIron', vertices=6)
        b.ring(f'SpectacleRim_{side}', (16.84, 2.75, side * .85), .235, .014, 'RailBrass', axis=(1, 0, 0), segments=16, ring_segs=5)
        _lining_rect(b, f'CabLining_{side}', 15.0, 16.6, 1.18, 2.12, side * 1.33)
    # Cab-side brass beading around the window opening.
    for side in (-1, 1):
        _lining_rect(b, f'CabWindowBead_{side}', 15.18, 16.42, 2.28, 3.22, side * 1.33, w=.025)
    # Front running plate across the frames, with lamp irons standing on it.
    b.box('FrontRunningPlate', (24.98, 1.28, 0), (1.36, .04, 1.40), 'RailIron', bevel=.004)
    for z in (-.7, 0, .7):
        b.box(f'LampIron_{z}', (25.55, 1.36, z), (.04, .12, .03), 'RailIron', bevel=.002)
    b.box('BufferBeamLining', (25.93, .85, 0), (.005, .40, 2.30), 'RailIvory', bevel=0)
    b.box('BufferBeamLiningFill', (25.935, .85, 0), (.005, .36, 2.26), 'RailRed', bevel=0)


def _tender(b):
    b.set_module('Module_Tender_Beauty')
    x0, x1 = 8.10, 14.90
    for side in (-1, 1):
        # Flared coping along the tank top.
        pts = [(x0, 2.82, side * 1.35), (x1, 2.82, side * 1.35), (x1, 3.02, side * 1.47), (x0, 3.02, side * 1.47)]
        prism(b, f'TenderCoping_{side}', pts, (0, .03, -side * .03), 'RailGreen')
        b.beam(f'TenderCopingBead_{side}', (x0, 3.04, side * 1.47), (x1, 3.04, side * 1.47), .014, 'RailBrass', vertices=6)
        # Two lined panels per side, in place of the plain iron straps.
        _lining_rect(b, f'TenderPanelA_{side}', 8.35, 11.40, 1.10, 2.62, side * 1.35)
        _lining_rect(b, f'TenderPanelB_{side}', 11.60, 14.65, 1.10, 2.62, side * 1.35)
        _lining_rect(b, f'TenderPanelInnerA_{side}', 8.43, 11.32, 1.18, 2.54, side * 1.35, mat='RailRed', w=.008)
        _lining_rect(b, f'TenderPanelInnerB_{side}', 11.68, 14.57, 1.18, 2.54, side * 1.35, mat='RailRed', w=.008)
        # Axleboxes, horn guides and leaf springs outside each wheel.
        for ax in (9.3, 11.5, 13.7):
            b.box(f'TenderAxlebox_{side}_{ax}', (ax, .48 + L, side * 1.31), (.24, .24, .12), 'RailIron', bevel=.01)
            for dx in (-.15, .15):
                b.box(f'TenderHorn_{side}_{ax}_{dx}', (ax + dx, .62, side * 1.28), (.05, .40, .06), 'RailIron', bevel=.004)
            b.box(f'TenderSpring_{side}_{ax}', (ax, .71, side * 1.31), (.80, .05, .10), 'RailIron', bevel=.006)
            b.box(f'TenderSpringBuckle_{side}_{ax}', (ax, .67, side * 1.31), (.10, .10, .11), 'RailIron', bevel=.004)
    # Rear coping, ladder and tool box.
    prism(b, 'TenderCopingRear', [(x0, 2.82, -1.35), (x0, 2.82, 1.35), (x0 - .12, 3.02, 1.47), (x0 - .12, 3.02, -1.47)], (.03, .03, 0), 'RailGreen')
    for z in (.62, .98):
        b.beam(f'TenderLadderRail_{z}', (8.075, 1.08, z), (8.075, 2.95, z), .016, 'RailIron', vertices=6)
    for k in range(6):
        b.beam(f'TenderLadderRung_{k}', (8.075, 1.20 + k * .30, .62), (8.075, 1.20 + k * .30, .98), .012, 'RailIron', vertices=6)
    b.box('TenderToolBox', (9.0, 2.93, -.75), (.50, .22, .80), 'RailIron', bevel=.01)
    b.box('TenderCoalBoard', (14.75, 3.05, 0), (.06, .45, 2.40), 'RailIron', bevel=.004)


def _coach(b, cx, short):
    b.set_module(f'Module_{short}_Beauty')
    for side in (-1, 1):
        face = side * 1.49
        # Lined lower panels between the doors and beaded window openings on the ivory band.
        for i, (a, c) in enumerate(((-5.20, -1.80), (-1.70, 1.70), (1.80, 5.20))):
            _lining_rect(b, f'{short}_LowerLining_{side}_{i}', cx + a + .08, cx + c - .08, 1.13, 1.72, face)
        for wx in WINDOWS:
            _lining_rect(b, f'{short}_WindowBead_{side}_{wx}', cx + wx - .75, cx + wx + .75, 1.84, 3.225, face, mat='RailGreen', w=.03, t=.008)
        for ex in (-6.92, 6.92):
            _lining_rect(b, f'{short}_EndLining_{side}_{ex}', cx + ex - .38, cx + ex + .38, 1.13, 1.72, face)
        # Grab rails on the latch side of every door (clear of each swinging leaf).
        for gx in (cx + 6.50, cx - 5.30):
            b.beam(f'{short}_GrabRail_{side}_{gx:.2f}', (gx, 1.35, side * 1.535), (gx, 2.95, side * 1.535), .013, 'RailBrass', vertices=8)
            for y in (1.38, 2.92):
                b.beam(f'{short}_GrabStandoff_{side}_{gx:.2f}_{y}', (gx, y, face), (gx, y, side * 1.535), .009, 'RailBrass', vertices=6)
        # Solebar with rivets, and queen-post truss rods under the body.
        b.box(f'{short}_Solebar_{side}', (cx, .90, side * 1.325), (14.80, .16, .05), 'RailIron', bevel=.004)
        for k in range(30):
            b.cylinder(f'{short}_Rivet_{side}_{k}', (cx - 7.1 + k * .49, .90, side * 1.352), .012, .01, 'RailIron', axis=(0, 0, side), vertices=6)
        tz = side * .80
        anchors = [(cx - 3.55, .84, tz), (cx - 1.4, .42, tz), (cx + 1.4, .42, tz), (cx + 3.55, .84, tz)]
        for k in range(3):
            b.beam(f'{short}_TrussRod_{side}_{k}', anchors[k], anchors[k + 1], .014, 'RailIron', vertices=6)
        for qx in (cx - 1.4, cx + 1.4):
            b.beam(f'{short}_QueenPost_{side}_{qx:.1f}', (qx, .85, tz), (qx, .42, tz), .022, 'RailIron', vertices=6)
    b.box(f'{short}_BatteryBox', (cx - 2.4, .67, -.35), (.80, .36, .50), 'RailIron', bevel=.01)
    # Bogie side frames, bolsters and transoms (replacing the old solid bogie block).
    for bx in (cx - 4.9, cx + 4.9):
        for side in (-1, 1):
            pts = [(bx - 1.25, .74, side * 1.17), (bx + 1.25, .74, side * 1.17), (bx + 1.05, .48, side * 1.17), (bx + .55, .38, side * 1.17),
                   (bx - .55, .38, side * 1.17), (bx - 1.05, .48, side * 1.17)]
            prism(b, f'{short}_BogieSide_{bx:.1f}_{side}', pts, (0, 0, -side * .05), 'RailIron')
        b.box(f'{short}_Bolster_{bx:.1f}', (bx, .68, 0), (.42, .16, 2.30), 'RailIron', bevel=.01)
        for dx in (-.55, .55):
            b.box(f'{short}_Transom_{bx:.1f}_{dx}', (bx + dx, .64, 0), (.10, .10, 2.30), 'RailIron', bevel=.006)
    # Roof ventilators and rain strips.
    for k, vx in enumerate((-5.0, -2.5, 0.0, 2.5, 5.0)):
        x = cx + vx
        b.cylinder(f'{short}_RoofVentNeck_{k}', (x, 3.83, 0), .05, .08, 'RailIron', axis=(0, 1, 0), vertices=8)
        b.cylinder(f'{short}_RoofVentBody_{k}', (x, 3.92, 0), .085, .34, 'RailIron', axis=(1, 0, 0), vertices=10)
        for e in (-1, 1):
            globe(b, f'{short}_RoofVentCap_{k}_{e}', (x + e * .17, 3.92, 0), .085, 'RailIron', lat=4, lon=10)
    for side in (-1, 1):
        z = side * 1.18
        y = 3.46 + .34 * math.sqrt(max(0, 1 - (z / 1.515) ** 2)) + .02
        b.beam(f'{short}_RainStrip_{side}', (cx - 7.2, y, z), (cx + 7.2, y, z), .012, 'RailIron', vertices=6)
    # ---- Saloon interior --------------------------------------------------------------
    # Walnut wall lining on the inner faces (posts and lower wall), never inside an aperture.
    for side in (-1, 1):
        z = side * 1.4025
        b.box(f'{short}_DadoLining_{side}', (cx, 1.43, z), (10.36, .74, .015), 'RailWalnut', bevel=0)
        b.box(f'{short}_ChairRail_{side}', (cx, 1.815, side * 1.385), (10.36, .05, .04), 'RailWalnut', bevel=.006)
        for px in (-3.6, -1.8, 0.0, 1.8, 3.6):
            b.box(f'{short}_PostLining_{side}_{px}', (cx + px, 2.50, z), (.35, 1.40, .015), 'RailWalnut', bevel=0)
            b.box(f'{short}_CoatHook_{side}_{px}', (cx + px, 3.02, side * 1.37), (.03, .05, .05), 'RailBrass', bevel=.004)
        for k in range(6):
            x = cx - 4.85 + k * 1.94
            b.box(f'{short}_DadoStile_{side}_{k}', (x, 1.43, side * 1.39), (.05, .70, .012), 'RailWalnut', bevel=.002)
        for wx in WINDOWS:
            x = cx + wx
            b.beam(f'{short}_CurtainRod_{side}_{wx}', (x - .72, 3.17, side * 1.36), (x + .72, 3.17, side * 1.36), .011, 'RailBrass', vertices=6)
            for e in (-1, 1):
                # Slim tied-back drapes hugging each post; the glazed width stays open.
                dx = x + e * .645
                upper = [(dx - .055, 3.16, side * 1.39), (dx + .055, 3.16, side * 1.39), (dx + .032, 2.38, side * 1.39), (dx - .032, 2.38, side * 1.39)]
                lower = [(dx - .032, 2.38, side * 1.39), (dx + .032, 2.38, side * 1.39), (dx + .06, 1.90, side * 1.39), (dx - .06, 1.90, side * 1.39)]
                prism(b, f'{short}_CurtainUpper_{side}_{wx}_{e}', upper, (0, 0, -side * .055), 'RailCurtain')
                prism(b, f'{short}_CurtainLower_{side}_{wx}_{e}', lower, (0, 0, -side * .065), 'RailCurtain')
                b.box(f'{short}_CurtainTie_{side}_{wx}_{e}', (dx, 2.38, side * 1.36), (.08, .025, .075), 'RailBrass', bevel=.004)
        for bx in BAYS:
            x = cx + bx
            b.box(f'{short}_WindowTable_{side}_{bx}', (x, 1.76, side * 1.265), (.46, .03, .27), 'RailWalnut', bevel=.006)
            b.beam(f'{short}_TableBracket_{side}_{bx}', (x, 1.50, side * 1.395), (x, 1.745, side * 1.16), .014, 'RailBrass', vertices=6)
    b.box(f'{short}_CarpetRunner', (cx, 1.054, 0), (10.30, .008, .76), 'RailCarpet', bevel=0)
    # Upholstered head rolls on every seat back.
    backs = [(bx - .68, 1) for bx in BAYS] + [(bx + .68, -1) for bx in BAYS] + [(3.55, 1)]
    for bxx, facing in backs:
        for side in (-1, 1):
            b.cylinder(f'{short}_HeadRoll_{bxx}_{side}', (cx + bxx + facing * .05, 2.21, side * .92), .045, .76, 'RailFelt', axis=(0, 0, 1), vertices=10)
    # Raised panels on the saloon faces of both bulkheads.
    for ex in (-5.2, 5.2):
        s = -1 if ex > 0 else 1
        for side in (-1, 1):
            x = cx + ex + s * .035
            for y0, y1 in ((1.15, 1.95), (2.10, 3.30)):
                b.box(f'{short}_BulkPanel_{ex}_{side}_{y0}', (x, (y0 + y1) / 2, side * .93), (.012, y1 - y0, .80), 'RailWalnut', bevel=.004)


def _gangways(b):
    b.set_module('Module_Gangways')
    # Bellows between the two coaches; walking width inside stays 0.92m.
    x0, x1 = -8.61, -7.39
    for k in range(6):
        x = x0 + (x1 - x0) * (k + .5) / 6
        grow = .02 if k % 2 else 0
        for side in (-1, 1):
            b.box(f'BellowsSide_{k}_{side}', (x, 2.18, side * (.51 + grow / 2)), (.17, 2.30, .10 + grow), 'RailRubber', bevel=.01)
        b.box(f'BellowsTop_{k}', (x, 3.38 + grow / 2, 0), (.17, .10 + grow, 1.12 + grow * 2), 'RailRubber', bevel=.01)
    # Closed end doors at the outer coach ends (tender / van ends had an open 0.8m drop).
    for ex, s in ((7.33, 1), (-23.33, -1)):
        b.box(f'EndDoorPanelLower_{ex}', (ex, 1.62, 0), (.05, 1.10, .80), 'RailWalnut', bevel=.006)
        b.box(f'EndDoorRailTop_{ex}', (ex, 3.19, 0), (.05, .12, .80), 'RailWalnut', bevel=.006)
        for z in (-.36, .36):
            b.box(f'EndDoorStile_{ex}_{z}', (ex, 2.66, z), (.05, 1.06, .08), 'RailWalnut', bevel=.004)
        b.box(f'EndDoorMuntin_{ex}', (ex, 2.20, 0), (.05, .06, .80), 'RailWalnut', bevel=.004)
        b.box(f'EndDoorGlass_{ex}', (ex, 2.68, 0), (.012, .90, .64), 'RailGlass', bevel=0)
        b.box(f'EndDoorHandle_{ex}', (ex - s * .035, 1.95, .30), (.02, .14, .03), 'RailBrass', bevel=.003)


def _van(b):
    b.set_module('Module_Luggage_Beauty')
    cx = -29.0
    for side in (-1, 1):
        face = side * 1.49
        for ax in (cx - 2.4, cx + 2.4):
            b.box(f'VanAxlebox_{side}_{ax}', (ax, .60, side * 1.36), (.24, .24, .12), 'RailIron', bevel=.01)
            pts = [(ax - .45, .92, side * 1.33), (ax + .45, .92, side * 1.33), (ax + .16, .40, side * 1.33), (ax - .16, .40, side * 1.33)]
            prism(b, f'VanWIron_{side}_{ax}', pts, (0, 0, -side * .03), 'RailIron')
            b.box(f'VanSpring_{side}_{ax}', (ax, .82, side * 1.36), (.90, .05, .10), 'RailIron', bevel=.006)
        b.box(f'VanSolebar_{side}', (cx, .90, side * 1.325), (8.80, .16, .05), 'RailIron', bevel=.004)
    for k, vx in enumerate((-3.0, 0.0, 3.0)):
        b.cylinder(f'VanRoofVent_{k}', (cx + vx, 3.86, 0), .07, .12, 'RailIron', axis=(0, 1, 0), vertices=8)
        b.cylinder(f'VanRoofVentHood_{k}', (cx + vx, 3.94, 0), .11, .04, 'RailIron', axis=(0, 1, 0), vertices=10)
    b.cylinder('VanTailLampBody', (cx - 4.50, 2.55, .85), .10, .16, 'RailIron', axis=(-1, 0, 0), vertices=12)
    b.cylinder('VanTailLampLens', (cx - 4.59, 2.55, .85), .07, .02, 'RailRed', axis=(-1, 0, 0), vertices=12)


def add_train_beautify(b):
    print('>>> Train beautification pass...')
    _locomotive(b)
    _tender(b)
    for cx, short in COACHES:
        _coach(b, cx, short)
    _gangways(b)
    _van(b)
