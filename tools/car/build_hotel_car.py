"""Original 1928-31 style four-door hotel sedan for Wrong Date Testimony.

Generates the editable Blender source, the Unity FBX and one layout contract that
the Unity prefab builder reads for pivots, colliders, seats and controls.

    blender --background --python-exit-code 1 --python tools/car/build_hotel_car.py -- --output assets/source/car

Authoring coordinates are Unity car-local metres: +X right, +Y up, +Z forward.
The origin is on the ground, midway between the axles. The car is an original
period design: no maker badge, model name or copied body is used.
"""

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

# ---------------------------------------------------------------------------
# Dimensions (metres). Proportions follow common 1928-31 American/European
# four-door sedans: about 2.6 m wheelbase, 21 inch wire wheels, upright cab.
# ---------------------------------------------------------------------------
WHEELBASE = 2.63
FRONT_AXLE = WHEELBASE / 2
REAR_AXLE = -WHEELBASE / 2
TRACK_HALF = 0.71
WHEEL_RADIUS = 0.37
TIRE_SECTION = 0.13
BODY_HALF = 0.65
SILL = 0.60
BELT = 1.20
ROOF_TOP = 1.785
GLASS_LOW, GLASS_HIGH = 1.22, 1.62
FIREWALL = 0.80
RADIATOR_FRONT = 1.76
DOOR_SPANS = {  # (front z, rear z, hinge z)
    'F': (0.28, -0.48, 0.28),
    'R': (-0.56, -1.30, -1.30),
}
REAR_DOOR_NOTCH_Z = -0.92   # rear door lower edge clears the rear fender
REAR_DOOR_NOTCH_Y = 0.98
STEER_CENTRE = Vector((-0.30, 1.13, -0.09))
STEER_AXIS = Vector((0.0, -0.749, 0.663)).normalized()
DRIVER_X = -0.30
VISOR_X = -0.35          # visor spans x -0.50..-0.20
WIPER_MOTOR_X = -0.14    # between the visor and the interior mirror (x +-0.08)

MATERIALS = {
    'Car_Paint': ('6B2C2C', 0.0, 0.42),
    'Car_Black': ('1E1C1A', 0.0, 0.50),
    'Car_Chrome': ('C9C6BE', 0.9, 0.18),
    'Car_Glass': ('A9BFC9', 0.0, 0.05),
    'Car_Tire': ('232220', 0.0, 0.85),
    'Car_Spoke': ('E8DCC0', 0.2, 0.40),
    'Car_Stripe': ('D9C9A3', 0.0, 0.45),
    'Car_Leather': ('4A3328', 0.0, 0.60),
    'Car_Trim': ('6A5A48', 0.0, 0.80),
    'Car_Headliner': ('8A7A66', 0.0, 0.90),
    'Car_Dash': ('4A3B30', 0.0, 0.55),
    'Car_Floor': ('2A2826', 0.0, 0.90),
    'Car_Engine': ('3E463F', 0.2, 0.55),
    'Car_Brass': ('B38A49', 0.8, 0.35),
    'Car_Lamp': ('FFE2A8', 0.0, 0.20),
    'Car_TailLamp': ('A32D2D', 0.0, 0.25),
    'Car_DomeLamp': ('F3DDB2', 0.0, 0.30),
    'Car_Gauge': ('E8DCC0', 0.0, 0.40),
    'Car_Needle': ('B5482A', 0.0, 0.50),
    'Car_Canvas': ('6E6A55', 0.0, 0.95),
    'Car_Rubber': ('2A2826', 0.0, 0.95),
    'Car_Wood': ('7A5236', 0.0, 0.70),
    'Car_Tan': ('8A6A3E', 0.0, 0.70),
    'Car_Mirror': ('3F4A52', 0.9, 0.05),
    'Car_InnerTube': ('7A3A30', 0.0, 0.85),
}


def u(p):
    """Unity car-local (x right, y up, z forward) to Blender for FBX -Z forward / Y up."""
    return Vector((-float(p[0]), -float(p[2]), float(p[1])))


# ---------------------------------------------------------------------------
# Geometry collection. Each part becomes one Blender object; movable parts
# are parented to a named pivot empty so Unity can rebuild exact hinges.
# ---------------------------------------------------------------------------
class Part:
    def __init__(self, name, pivot=None, parent=None):
        self.name, self.pivot, self.parent = name, pivot, parent
        self.verts, self.faces, self.mats = [], [], []

    def add(self, verts, faces, mat):
        if mat not in MATERIALS:
            raise ValueError('Unknown material ' + mat)
        base = len(self.verts)
        self.verts.extend(Vector(v) for v in verts)
        for f in faces:
            self.faces.append(tuple(base + i for i in f))
            self.mats.append(mat)


PARTS = {}
PART_ORDER = []
LAYOUT = {'parts': [], 'controls': [], 'body': [], 'seats': [], 'wheels': []}


def part(name, pivot=None, parent='Body'):
    if name not in PARTS:
        PARTS[name] = Part(name, pivot, None if name == 'Body' else parent)
        PART_ORDER.append(name)
    return PARTS[name]


def rot_x(deg):
    return Matrix.Rotation(math.radians(deg), 3, 'X')


def rot_y(deg):
    return Matrix.Rotation(math.radians(deg), 3, 'Y')


def rot_z(deg):
    return Matrix.Rotation(math.radians(deg), 3, 'Z')


def box(p, c, s, mat, r=None):
    c = Vector(c)
    hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    corners = [Vector((x, y, z)) for x in (-hx, hx) for y in (-hy, hy) for z in (-hz, hz)]
    if r is not None:
        corners = [r @ v for v in corners]
    verts = [c + v for v in corners]
    # index = xi*4 + yi*2 + zi
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    p.add(verts, faces, mat)


def hexa(p, pts, mat):
    """Arbitrary hexahedron from 8 corners ordered like box(): index = xi*4 + yi*2 + zi."""
    faces = [(0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6), (0, 2, 6, 4), (1, 5, 7, 3)]
    p.add([Vector(v) for v in pts], faces, mat)


def frame(axis):
    a = Vector(axis).normalized()
    t = Vector((0, 1, 0)) if abs(a.y) < 0.9 else Vector((1, 0, 0))
    b1 = a.cross(t).normalized()
    b2 = a.cross(b1).normalized()
    return a, b1, b2


def cyl(p, a, b, r, mat, seg=12, r2=None):
    a, b = Vector(a), Vector(b)
    axis = b - a
    _, e1, e2 = frame(axis)
    r2 = r if r2 is None else r2
    verts = []
    for i in range(seg):
        t = 2 * math.pi * i / seg
        d = e1 * math.cos(t) + e2 * math.sin(t)
        verts.append(a + d * r)
    for i in range(seg):
        t = 2 * math.pi * i / seg
        d = e1 * math.cos(t) + e2 * math.sin(t)
        verts.append(b + d * r2)
    faces = [(i, (i + 1) % seg, seg + (i + 1) % seg, seg + i) for i in range(seg)]
    faces.append(tuple(range(seg - 1, -1, -1)))
    faces.append(tuple(range(seg, 2 * seg)))
    p.add(verts, faces, mat)


def disc(p, c, normal, rx, ry, depth, mat, seg=24, up=None):
    """Elliptical plate centred at c, extruded `depth` along normal (both directions halved)."""
    n = Vector(normal).normalized()
    up = Vector(up) if up is not None else (Vector((0, 1, 0)) if abs(n.y) < 0.9 else Vector((0, 0, 1)))
    e1 = up.cross(n).normalized()
    e2 = n.cross(e1).normalized()
    c = Vector(c)
    verts = []
    for side in (-0.5, 0.5):
        for i in range(seg):
            t = 2 * math.pi * i / seg
            verts.append(c + n * depth * side + e1 * math.cos(t) * rx + e2 * math.sin(t) * ry)
    faces = [(i, (i + 1) % seg, seg + (i + 1) % seg, seg + i) for i in range(seg)]
    faces.append(tuple(range(seg - 1, -1, -1)))
    faces.append(tuple(range(seg, 2 * seg)))
    p.add(verts, faces, mat)


def lathe(p, origin, axis, profile, mat, seg=16):
    """Revolve (radius, distance-along-axis) points. Radii of zero become poles."""
    origin = Vector(origin)
    a, e1, e2 = frame(axis)
    rings = []
    verts = []
    for radius, h in profile:
        if radius <= 1e-6:
            rings.append([len(verts)])
            verts.append(origin + a * h)
        else:
            ring = []
            for i in range(seg):
                t = 2 * math.pi * i / seg
                ring.append(len(verts))
                verts.append(origin + a * h + (e1 * math.cos(t) + e2 * math.sin(t)) * radius)
            rings.append(ring)
    faces = []
    for r0, r1 in zip(rings, rings[1:]):
        if len(r0) == 1 and len(r1) == 1:
            continue
        if len(r0) == 1:
            faces += [(r0[0], r1[(i + 1) % seg], r1[i]) for i in range(seg)]
        elif len(r1) == 1:
            faces += [(r0[i], r0[(i + 1) % seg], r1[0]) for i in range(seg)]
        else:
            faces += [(r0[i], r0[(i + 1) % seg], r1[(i + 1) % seg], r1[i]) for i in range(seg)]
    if len(rings[0]) > 1:
        faces.append(tuple(reversed(rings[0])))
    if len(rings[-1]) > 1:
        faces.append(tuple(rings[-1]))
    p.add(verts, faces, mat)


def torus(p, c, axis, R, r, mat, seg=24, rseg=8):
    c = Vector(c)
    a, e1, e2 = frame(axis)
    verts = []
    for i in range(seg):
        t = 2 * math.pi * i / seg
        radial = e1 * math.cos(t) + e2 * math.sin(t)
        for j in range(rseg):
            s = 2 * math.pi * j / rseg
            verts.append(c + radial * (R + r * math.cos(s)) + a * r * math.sin(s))
    faces = []
    for i in range(seg):
        for j in range(rseg):
            i2, j2 = (i + 1) % seg, (j + 1) % rseg
            faces.append((i * rseg + j, i2 * rseg + j, i2 * rseg + j2, i * rseg + j2))
    p.add(verts, faces, mat)


def loft(p, sections, mat):
    """Connect equal-length closed cross-sections and cap the ends."""
    n = len(sections[0])
    verts = [Vector(v) for s in sections for v in s]
    faces = []
    for k in range(len(sections) - 1):
        for i in range(n):
            a, b = k * n + i, k * n + (i + 1) % n
            faces.append((a, b, b + n, a + n))
    faces.append(tuple(range(n - 1, -1, -1)))
    last = (len(sections) - 1) * n
    faces.append(tuple(last + i for i in range(n)))
    p.add(verts, faces, mat)


def extrude_xz(p, poly_zy, x0, x1, mat):
    """Side-profile polygon given as (z, y) pairs, extruded across x."""
    loft(p, [[Vector((x0, y, z)) for z, y in poly_zy], [Vector((x1, y, z)) for z, y in poly_zy]], mat)


def extrude_z(p, poly_xy, z0, z1, mat):
    """Front-profile polygon given as (x, y) pairs, extruded along z."""
    loft(p, [[Vector((x, y, z0)) for x, y in poly_xy], [Vector((x, y, z1)) for x, y in poly_xy]], mat)


def rounded_rect(x0, x1, y0, y1, r_top, seg=4):
    pts = [(x0, y0), (x1, y0)]
    for i in range(seg + 1):
        t = math.pi / 2 * i / seg
        pts.append((x1 - r_top + math.cos(t) * r_top, y1 - r_top + math.sin(t) * r_top))
    for i in range(seg + 1):
        t = math.pi / 2 + math.pi / 2 * i / seg
        pts.append((x0 + r_top + math.cos(t) * r_top, y1 - r_top + math.sin(t) * r_top))
    return pts


def fender(p, path_zy, x_inner, x_outer, mat):
    """Crowned sheet-metal fender swept along a side-view path."""
    sections = []
    for i, (z, y) in enumerate(path_zy):
        z0, y0 = path_zy[max(i - 1, 0)]
        z1, y1 = path_zy[min(i + 1, len(path_zy) - 1)]
        tz, ty = z1 - z0, y1 - y0
        length = math.hypot(tz, ty) or 1
        nz, ny = -ty / length, tz / length          # normal in the side plane
        if ny < 0:
            nz, ny = -nz, -ny
        width = x_outer - x_inner
        top = []
        for k in range(7):
            f = k / 6
            x = x_inner + width * f
            crown = 0.035 * math.sin(math.pi * f) - (0.05 if k in (0, 6) else 0)
            top.append((x, crown))
        ring = [Vector((x, y + ny * h, z + nz * h)) for x, h in top]
        ring += [Vector((x, y + ny * (h - 0.028), z + nz * (h - 0.028))) for x, h in reversed(top)]
        sections.append(ring)
    loft(p, sections, mat)


def control(cid, kind, part_name, center, size, label=''):
    LAYOUT['controls'].append({'id': cid, 'kind': kind, 'part': part_name,
                               'center': list(map(float, center)), 'size': list(map(float, size)), 'label': label})


def collider(owner, center, size):
    entry = {'center': list(map(float, center)), 'size': list(map(float, size))}
    if owner == 'Body':
        LAYOUT['body'].append(entry)
    else:
        LAYOUT.setdefault('partColliders', []).append(dict(entry, part=owner))


def movable(name, pivot, kind, axis, angle=0.0, offset=(0, 0, 0), speed=1.6, parent='Body', fixture=''):
    part(name, Vector(pivot), parent)
    LAYOUT['parts'].append({'id': name, 'parent': parent, 'pivot': list(map(float, pivot)), 'kind': kind,
                            'axis': list(map(float, Vector(axis).normalized())), 'angle': float(angle),
                            'offset': list(map(float, offset)), 'speed': float(speed), 'fixture': fixture})
    return PARTS[name]


# ---------------------------------------------------------------------------
# Body shell, chassis and engine bay
# ---------------------------------------------------------------------------
def build_body():
    b = part('Body')
    # Floor, toe board and firewall.
    box(b, (0, 0.47, -0.70), (1.26, 0.06, 2.00), 'Car_Floor')
    box(b, (0, 0.62, 0.55), (1.20, 0.035, 0.56), 'Car_Floor', rot_x(-32))
    box(b, (0, 0.82, FIREWALL - 0.01), (0.88, 0.72, 0.03), 'Car_Black')
    box(b, (0, 0.505, -0.02), (1.00, 0.012, 0.52), 'Car_Rubber')
    box(b, (0, 0.505, -1.18), (1.00, 0.012, 0.36), 'Car_Rubber')
    # Tapered cowl: hood width at the firewall, full body width at the windscreen.
    # hexa() corner order: [surface A: y0z0, y0z1, y1z0, y1z1] + [surface B: same].
    for s in (-1, 1):
        hexa(b, [
            (s * 0.465, 0.60, FIREWALL), (s * BODY_HALF, 0.60, 0.28), (s * 0.465, 1.18, FIREWALL), (s * BODY_HALF, 1.18, 0.28),
            (s * 0.44, 0.60, FIREWALL), (s * (BODY_HALF - 0.025), 0.60, 0.28), (s * 0.44, 1.18, FIREWALL), (s * (BODY_HALF - 0.025), 1.18, 0.28),
        ], 'Car_Paint')
    hexa(b, [
        (-0.44, 1.165, FIREWALL), (-BODY_HALF, 1.165, 0.28), (-0.44, 1.20, FIREWALL), (-BODY_HALF, 1.20, 0.28),
        (0.44, 1.165, FIREWALL), (BODY_HALF, 1.165, 0.28), (0.44, 1.20, FIREWALL), (BODY_HALF, 1.20, 0.28),
    ], 'Car_Paint')
    collider('Body', (0, 1.18, 0.54), (1.30, 0.05, 0.52))
    for s in (-1, 1):
        collider('Body', (s * 0.555, 0.89, 0.54), (0.20, 0.58, 0.52))
    # Dashboard (painted walnut-brown) with instrument oval and switches.
    box(b, (0, 1.05, 0.29), (1.26, 0.26, 0.05), 'Car_Dash')
    collider('Body', (0, 1.05, 0.29), (1.26, 0.26, 0.05))
    collider('Body', (0, 0.82, FIREWALL - 0.01), (0.88, 0.72, 0.04))
    collider('Body', (0, 0.47, -0.70), (1.30, 0.07, 2.02))
    disc(b, (0, 1.06, 0.262), (0, 0, -1), 0.145, 0.062, 0.008, 'Car_Chrome')
    disc(b, (0, 1.06, 0.257), (0, 0, -1), 0.132, 0.052, 0.004, 'Car_Black')
    for x in (-0.07, 0.0, 0.07):
        disc(b, (x, 1.06, 0.2545), (0, 0, -1), 0.026, 0.026, 0.003, 'Car_Gauge', seg=20)
        for k in range(7):
            t = math.radians(-135 + 45 * k)
            box(b, (x + math.sin(t) * 0.021, 1.06 + math.cos(t) * 0.021, 0.2528), (0.002, 0.006, 0.0012), 'Car_Black', rot_z(-math.degrees(t)))
    disc(b, (0, 0.975, 0.262), (0, 0, -1), 0.022, 0.022, 0.006, 'Car_Chrome')
    disc(b, (0.20, 0.975, 0.264), (0, 0, -1), 0.016, 0.016, 0.004, 'Car_Chrome')
    control('gauges', 'gauges', 'Body', (0, 1.062, 0.25), (0.30, 0.10, 0.03), '查看仪表')
    # Sills, pillars, quarters and rear wall.
    for s in (-1, 1):
        box(b, (s * 0.625, 0.54, -0.51), (0.05, 0.12, 1.58), 'Car_Paint')
        box(b, (s * 0.625, 1.16, -0.52), (0.05, 1.12, 0.08), 'Car_Paint')
        box(b, (s * 0.62, 1.45, 0.285), (0.05, 0.52, 0.05), 'Car_Paint')
        box(b, (s * 0.625, 1.11, -1.50), (0.05, 0.26, 0.40), 'Car_Paint')
        box(b, (s * 0.625, 1.42, -1.34), (0.05, 0.44, 0.08), 'Car_Paint')
        box(b, (s * 0.625, 1.42, -1.66), (0.05, 0.44, 0.08), 'Car_Paint')
        box(b, (s * 0.625, 1.67, -1.50), (0.05, 0.10, 0.40), 'Car_Paint')
        box(b, (s * 0.632, 1.42, -1.50), (0.006, 0.40, 0.24), 'Car_Glass')
        box(b, (s * 0.6525, 1.21, -1.50), (0.006, 0.012, 0.40), 'Car_Stripe')
        # Inner wheelhouse below the rear quarter, behind the rear door notch.
        box(b, (s * 0.62, 0.75, -1.31), (0.06, 0.50, 0.78), 'Car_Paint')
        collider('Body', (s * 0.625, 1.16, -0.52), (0.05, 1.12, 0.08))
        collider('Body', (s * 0.625, 1.11, -1.50), (0.05, 1.22, 0.40))
        collider('Body', (s * 0.62, 0.75, -1.31), (0.06, 0.50, 0.78))
        collider('Body', (s * 0.62, 1.45, 0.285), (0.05, 0.52, 0.05))
    box(b, (0, 0.90, -1.70), (1.30, 0.80, 0.04), 'Car_Paint')
    for s in (-1, 1):
        box(b, (s * 0.465, 1.44, -1.70), (0.37, 0.28, 0.04), 'Car_Paint')
    box(b, (0, 1.66, -1.70), (1.30, 0.16, 0.04), 'Car_Paint')
    box(b, (0, 1.44, -1.70), (0.56, 0.28, 0.006), 'Car_Glass')
    collider('Body', (0, 1.11, -1.70), (1.30, 1.22, 0.05))
    # Roof with rolled edges, visor, leatherette insert and headliner.
    section = [(-0.65, 1.71), (-0.64, 1.748), (-0.60, 1.772), (-0.50, ROOF_TOP), (0.50, ROOF_TOP),
               (0.60, 1.772), (0.64, 1.748), (0.65, 1.71)]
    loft(b, [[Vector((x, y, z)) for x, y in section] for z in (0.34, -1.72)], 'Car_Paint')
    box(b, (0, 1.79, -0.65), (0.96, 0.01, 1.70), 'Car_Black')
    box(b, (0, 1.715, 0.37), (1.28, 0.03, 0.10), 'Car_Paint')
    box(b, (0, 1.695, -0.69), (1.24, 0.02, 2.02), 'Car_Headliner')
    collider('Body', (0, 1.745, -0.69), (1.30, 0.09, 2.10))
    # Dome lamp, interior mirror, wiper motor.
    cyl(b, (0, 1.684, -0.62), (0, 1.672, -0.62), 0.05, 'Car_DomeLamp', seg=16)
    torus(b, (0, 1.678, -0.62), (0, 1, 0), 0.052, 0.006, 'Car_Chrome', seg=16, rseg=6)
    control('dome', 'dome', 'Body', (0, 1.675, -0.62), (0.12, 0.03, 0.12), '车顶灯')
    cyl(b, (0, 1.70, 0.24), (0, 1.635, 0.24), 0.006, 'Car_Chrome', seg=8)
    box(b, (0, 1.612, 0.24), (0.16, 0.05, 0.012), 'Car_Chrome')
    box(b, (0, 1.612, 0.2335), (0.15, 0.042, 0.002), 'Car_Mirror')
    box(b, (WIPER_MOTOR_X, 1.635, 0.27), (0.07, 0.05, 0.05), 'Car_Chrome')
    # Windscreen crank knob in the header centre.
    cyl(b, (0.12, 1.66, 0.27), (0.12, 1.635, 0.27), 0.012, 'Car_Black', seg=10)
    control('windshieldCrank', 'windshieldCrank', 'Body', (0.12, 1.645, 0.27), (0.05, 0.05, 0.05), '风挡摇柄')

    # Hood top halves (fixed) with centre hinge; aprons; radiator shell.
    for s in (-1, 1):
        box(b, (s * 0.21, 1.115, 1.24), (0.42, 0.025, 0.88), 'Car_Paint')
        box(b, (s * 0.53, 0.72, 1.24), (0.22, 0.04, 0.88), 'Car_Black')
    cyl(b, (0, 1.13, FIREWALL), (0, 1.13, 1.68), 0.012, 'Car_Chrome', seg=8)
    collider('Body', (0, 1.115, 1.24), (0.86, 0.03, 0.88))
    shell = rounded_rect(-0.31, 0.31, 0.60, 1.14, 0.09)
    extrude_z(b, shell, 1.66, RADIATOR_FRONT, 'Car_Chrome')
    box(b, (0, 0.87, 1.765), (0.50, 0.44, 0.012), 'Car_Black')
    for i in range(9):
        box(b, (-0.22 + 0.055 * i, 0.87, 1.772), (0.012, 0.42, 0.01), 'Car_Chrome')
    box(b, (0, 0.87, 1.645), (0.56, 0.46, 0.06), 'Car_Black')
    collider('Body', (0, 0.87, 1.71), (0.64, 0.56, 0.12))
    torus(b, (0, 0.50, 1.762), (0, 0, 1), 0.022, 0.006, 'Car_Chrome', seg=12, rseg=6)
    control('crankHole', 'crankHole', 'Body', (0, 0.50, 1.78), (0.12, 0.10, 0.08), '摇把孔')

    # Chassis, axles, springs, exhaust and steering column.
    for s in (-1, 1):
        box(b, (s * 0.36, 0.43, -0.15), (0.06, 0.10, 3.90), 'Car_Black')
    box(b, (0, 0.46, 1.72), (0.80, 0.08, 0.08), 'Car_Black')
    box(b, (0, 0.46, -2.05), (0.80, 0.08, 0.08), 'Car_Black')
    collider('Body', (0, 0.42, -0.20), (0.82, 0.16, 3.92))
    box(b, (0, 0.36, FRONT_AXLE), (1.30, 0.05, 0.05), 'Car_Black')
    box(b, (0, 0.47, FRONT_AXLE), (1.00, 0.05, 0.06), 'Car_Black')
    cyl(b, (-0.70, 0.37, REAR_AXLE), (0.70, 0.37, REAR_AXLE), 0.045, 'Car_Black')
    lathe(b, (0, 0.37, REAR_AXLE - 0.13), (0, 0, 1), [(0, 0), (0.08, 0.04), (0.13, 0.13), (0.08, 0.22), (0, 0.26)], 'Car_Black', seg=12)
    box(b, (0, 0.50, REAR_AXLE), (1.00, 0.05, 0.06), 'Car_Black')
    cyl(b, (0, 0.40, 0.70), (0, 0.37, REAR_AXLE + 0.12), 0.04, 'Car_Black')
    cyl(b, (0.26, 0.36, 0.95), (0.26, 0.33, -2.05), 0.03, 'Car_Black', seg=8)
    cyl(b, (0.26, 0.33, -1.20), (0.26, 0.33, -1.70), 0.07, 'Car_Black')
    col_a = STEER_CENTRE + STEER_AXIS * 0.035
    col_b = STEER_CENTRE + STEER_AXIS * 0.72
    cyl(b, col_a, col_b, 0.024, 'Car_Black')
    disc(b, STEER_CENTRE + STEER_AXIS * 0.012, STEER_AXIS, 0.048, 0.048, 0.03, 'Car_Black', seg=16)

    # Engine bay: inline four, head, plugs, distributor, fan, generator, carburettor.
    box(b, (0, 0.66, 1.24), (0.30, 0.42, 0.62), 'Car_Engine')
    box(b, (0, 0.91, 1.24), (0.28, 0.08, 0.58), 'Car_Engine')
    control('engine', 'engine', 'Body', (0, 0.70, 1.24), (0.34, 0.52, 0.66), '发动机')
    for z in (1.03, 1.17, 1.31, 1.45):
        cyl(b, (0.05, 0.95, z), (0.05, 1.00, z), 0.011, 'Car_Spoke', seg=8)
        cyl(b, (0.05, 1.00, z), (0.01, 0.995, 1.58), 0.004, 'Car_Black', seg=6)
    cyl(b, (0, 0.93, 1.58), (0, 1.01, 1.58), 0.035, 'Car_Black')
    cyl(b, (0, 0.97, 1.50), (0, 1.04, 1.63), 0.03, 'Car_Black')
    cyl(b, (0, 0.82, 1.585), (0, 0.82, 1.62), 0.04, 'Car_Black')
    for k in range(4):
        box(b, (0, 0.82, 1.61), (0.05, 0.24, 0.01), 'Car_Black', rot_z(45 + 90 * k))
    cyl(b, (-0.20, 0.80, 1.30), (-0.20, 0.80, 1.52), 0.055, 'Car_Black')
    box(b, (0.20, 0.62, 1.24), (0.08, 0.10, 0.10), 'Car_Brass')
    box(b, (0.17, 0.70, 1.24), (0.05, 0.06, 0.40), 'Car_Engine')
    box(b, (0.17, 0.58, 1.24), (0.05, 0.06, 0.50), 'Car_Black')
    cyl(b, (-0.13, 0.875, 1.35), (-0.13, 0.92, 1.35), 0.025, 'Car_Chrome', seg=10)
    cyl(b, (-0.16, 0.62, 1.05), (-0.18, 0.84, 1.05), 0.006, 'Car_Chrome', seg=6)
    torus(b, (-0.18, 0.855, 1.05), (1, 0, 0), 0.016, 0.004, 'Car_Chrome', seg=10, rseg=5)
    cyl(b, (0.15, 1.00, 0.775), (0.15, 1.00, 0.70), 0.03, 'Car_Black')
    collider('Body', (0, 0.66, 1.24), (0.30, 0.42, 0.62))

    # Fenders, running boards, bumpers.
    front_path = [(1.86, 0.58), (1.76, 0.76), (1.62, 0.88), (1.47, 0.935), (FRONT_AXLE, 0.95), (1.15, 0.935),
                  (1.00, 0.88), (0.88, 0.78), (0.76, 0.62), (0.64, 0.49), (0.55, 0.445)]
    rear_path = [(-0.80, 0.445), (-0.90, 0.50), (-1.02, 0.68), (-1.15, 0.84), (-1.315, 0.935), (-1.48, 0.93),
                 (-1.64, 0.84), (-1.76, 0.70), (-1.86, 0.55)]
    for s in (-1, 1):
        x_in, x_out = (0.58, 0.88)
        fender(b, front_path if s > 0 else front_path, s * x_in if s > 0 else -x_out, s * x_out if s > 0 else -x_in, 'Car_Black')
        fender(b, rear_path, s * x_in if s > 0 else -x_out, s * x_out if s > 0 else -x_in, 'Car_Black')
        box(b, (s * 0.76, 0.42, -0.13), (0.24, 0.04, 1.36), 'Car_Black')
        box(b, (s * 0.882, 0.42, -0.13), (0.008, 0.045, 1.36), 'Car_Chrome')
        for z in (-0.6, -0.3, 0.0, 0.3):
            box(b, (s * 0.76, 0.4415, z), (0.22, 0.004, 0.012), 'Car_Rubber')
        collider('Body', (s * 0.73, 0.86, 1.30), (0.30, 0.20, 0.84))
        collider('Body', (s * 0.73, 0.86, -1.33), (0.30, 0.20, 0.80))
        collider('Body', (s * 0.76, 0.42, -0.13), (0.24, 0.05, 1.36))
    for z in (1.95, -2.30):
        for y in (0.43, 0.53):
            box(b, (0, y, z), (1.64, 0.05, 0.02), 'Car_Chrome')
        for s in (-1, 1):
            box(b, (s * 0.82, 0.48, z), (0.03, 0.15, 0.03), 'Car_Chrome')
            box(b, (s * 0.36, 0.46, z - 0.12 if z > 0 else z + 0.10), (0.03, 0.04, 0.24 if z > 0 else 0.20), 'Car_Black')
        collider('Body', (0, 0.48, z), (1.68, 0.16, 0.06))

    # Headlamps on a cross bar, horn and the left tail lamp.
    cyl(b, (-0.66, 0.86, 1.70), (0.66, 0.86, 1.70), 0.016, 'Car_Chrome', seg=8)
    for s in (-1, 1):
        c = Vector((s * 0.50, 0.98, 1.66))
        lathe(b, c, (0, 0, 1), [(0, -0.10), (0.06, -0.09), (0.10, -0.05), (0.115, 0.0), (0.118, 0.018)], 'Car_Chrome', seg=18)
        cyl(b, c + Vector((0, 0, 0.012)), c + Vector((0, 0, 0.024)), 0.108, 'Car_Lamp', seg=18)
        torus(b, c + Vector((0, 0, 0.022)), (0, 0, 1), 0.113, 0.008, 'Car_Chrome', seg=18, rseg=6)
        cyl(b, (s * 0.50, 0.86, 1.70), (s * 0.50, 0.87, 1.66), 0.014, 'Car_Chrome', seg=8)
    lathe(b, (-0.40, 0.84, 1.60), (0, 0, 1), [(0.0, 0.0), (0.022, 0.0), (0.026, 0.06), (0.048, 0.11), (0.055, 0.12)], 'Car_Black', seg=12)
    box(b, (-0.74, 0.88, -1.80), (0.02, 0.14, 0.02), 'Car_Black')
    tail = Vector((-0.74, 0.98, -1.80))
    lathe(b, tail, (0, 0, -1), [(0, -0.05), (0.035, -0.04), (0.045, 0.0)], 'Car_Chrome', seg=12)
    cyl(b, tail + Vector((0, 0, -0.004)), tail + Vector((0, 0, -0.014)), 0.04, 'Car_TailLamp', seg=12)

    # Trunk rack and the hollow trunk (lid is a separate part).
    for s in (-1, 1):
        box(b, (s * 0.50, 0.64, -1.98), (0.04, 0.04, 0.56), 'Car_Black')
        box(b, (s * 0.36, 0.55, -1.90), (0.03, 0.18, 0.03), 'Car_Black')
    for z in (-1.80, -1.95, -2.10, -2.22):
        box(b, (0, 0.665, z), (1.04, 0.012, 0.05), 'Car_Wood')
    walls = [((0, 0.685, -1.98), (1.10, 0.03, 0.48)), ((0, 0.875, -1.75), (1.10, 0.38, 0.02)),
             ((0, 0.875, -2.21), (1.10, 0.38, 0.02)), ((-0.54, 0.875, -1.98), (0.02, 0.38, 0.44)),
             ((0.54, 0.875, -1.98), (0.02, 0.38, 0.44))]
    for c, s in walls:
        box(b, c, s, 'Car_Black')
        collider('Body', c, s)
    box(b, (0, 0.71, -1.98), (1.06, 0.02, 0.44), 'Car_Trim')
    for x in (-0.55, 0.55):
        for z in (-1.74, -2.22):
            box(b, (x, 0.875, z), (0.025, 0.40, 0.025), 'Car_Chrome')
    for x in (-0.30, 0.30):
        box(b, (x, 0.76, -2.228), (0.06, 0.03, 0.012), 'Car_Chrome')
    box(b, (0, 1.05, -2.226), (0.05, 0.03, 0.012), 'Car_Chrome')

    # Seats: front bench and rear bench in dark leather.
    box(b, (0, 0.53, -0.48), (1.20, 0.10, 0.50), 'Car_Black')
    box(b, (0, 0.69, -0.48), (1.20, 0.22, 0.52), 'Car_Leather')
    box(b, (0, 1.08, -0.80), (1.20, 0.56, 0.10), 'Car_Leather', rot_x(8))
    cyl(b, (-0.60, 1.36, -0.84), (0.60, 1.36, -0.84), 0.05, 'Car_Leather')
    box(b, (0, 0.52, -1.20), (1.16, 0.10, 0.46), 'Car_Black')
    box(b, (0, 0.67, -1.20), (1.16, 0.20, 0.48), 'Car_Leather')
    box(b, (0, 1.06, -1.52), (1.16, 0.56, 0.10), 'Car_Leather', rot_x(8))
    for z in (-0.30, -0.48, -0.66):
        box(b, (0, 0.8015, z), (1.16, 0.004, 0.012), 'Car_Black')
    for x in (-0.30, 0.30):
        for y in (0.95, 1.15):
            box(b, (x, y, -0.758), (0.012, 0.012, 0.004), 'Car_Black')
    collider('Body', (0, 0.66, -0.48), (1.20, 0.28, 0.52))
    collider('Body', (0, 0.64, -1.20), (1.16, 0.26, 0.48))
    collider('Body', (0, 1.08, -0.80), (1.20, 0.56, 0.10))
    collider('Body', (0, 1.06, -1.52), (1.16, 0.56, 0.10))

    # Pedals and starter live in their own parts; the gear and brake levers too.
    control('body', 'body', 'Body', (0, 0.9, -0.2), (0.01, 0.01, 0.01), '旅馆用车')


# ---------------------------------------------------------------------------
# Movable parts
# ---------------------------------------------------------------------------
def build_doors():
    for row, (zf, zr, hinge_z) in DOOR_SPANS.items():
        for side, s in (('L', -1), ('R', 1)):
            name = 'Door_' + row + side
            front = row == 'F'
            # Unity left-handed AngleAxis around +Y: front doors swing the rear edge out,
            # rear doors swing the front edge out.
            angle = (70 if s < 0 else -70) if front else (-70 if s < 0 else 70)
            d = movable(name, (s * BODY_HALF, 0.0, hinge_z), 'hinge', (0, 1, 0), angle, speed=1.8,
                        fixture='car-door-' + (row + side).lower())
            length, mid = zf - zr, (zf + zr) / 2
            lower = [(SILL, BELT, zf, zr)] if front else [(SILL, REAR_DOOR_NOTCH_Y, zf, REAR_DOOR_NOTCH_Z),
                                                          (REAR_DOOR_NOTCH_Y, BELT, zf, zr)]
            for y0, y1, a, c in lower:
                cz, lz, cy, ly = (a + c) / 2, a - c, (y0 + y1) / 2, y1 - y0
                box(d, (s * 0.6325, cy, cz), (0.035, ly, lz), 'Car_Paint')
                box(d, (s * 0.6075, cy, cz), (0.015, ly, lz - 0.01), 'Car_Trim')
                collider(name, (s * 0.625, cy, cz), (0.05, ly, lz))
            box(d, (s * 0.632, 1.42, zf - 0.02), (0.03, 0.44, 0.04), 'Car_Paint')
            box(d, (s * 0.632, 1.42, zr + 0.02), (0.03, 0.44, 0.04), 'Car_Paint')
            box(d, (s * 0.632, 1.66, mid), (0.03, 0.04, length), 'Car_Paint')
            box(d, (s * 0.6525, 1.21, mid), (0.006, 0.012, length), 'Car_Stripe')
            collider(name, (s * 0.632, 1.42, zf - 0.02), (0.03, 0.44, 0.04))
            collider(name, (s * 0.632, 1.42, zr + 0.02), (0.03, 0.44, 0.04))
            collider(name, (s * 0.632, 1.66, mid), (0.03, 0.04, length))
            handle_z = zr + 0.10 if front else zf - 0.10
            box(d, (s * 0.665, 1.10, handle_z), (0.02, 0.015, 0.10), 'Car_Chrome')
            box(d, (s * 0.656, 1.10, handle_z + (0.04 if front else -0.04)), (0.012, 0.02, 0.02), 'Car_Chrome')
            box(d, (s * 0.588, 1.05, handle_z), (0.02, 0.015, 0.07), 'Car_Chrome')
            # Window glass slides into the door; the crank on the trim panel turns it.
            g = movable('Glass_' + row + side, (s * 0.632, GLASS_LOW, mid), 'slide', (0, -1, 0), offset=(0, -0.40, 0),
                        speed=0.9, parent=name, fixture='car-window-' + (row + side).lower())
            box(g, (s * 0.632, 1.42, mid), (0.006, 0.40, length - 0.10), 'Car_Glass')
            collider('Glass_' + row + side, (s * 0.632, 1.42, mid), (0.03, 0.40, length - 0.10))
            crank_z = zr + 0.20 if front else zf - 0.20
            k = movable('Crank_' + row + side, (s * 0.596, 0.95, crank_z), 'spin', (1, 0, 0), angle=720, speed=0.9, parent=name)
            disc(k, (s * 0.596, 0.95, crank_z), (1, 0, 0), 0.014, 0.014, 0.012, 'Car_Chrome', seg=10)
            box(k, (s * 0.588, 0.95, crank_z - 0.035), (0.008, 0.012, 0.07), 'Car_Chrome')
            cyl(k, (s * 0.588, 0.95, crank_z - 0.07), (s * 0.565, 0.95, crank_z - 0.07), 0.010, 'Car_Black', seg=8)
            control('window-' + row + side, 'window', 'Crank_' + row + side, (s * 0.585, 0.95, crank_z - 0.03), (0.05, 0.08, 0.12), '车窗摇柄')
            control('door-' + row + side, 'door', name, (s * 0.625, 0.90, mid), (0.05, 0.60, length - 0.02), '车门')


def build_front_parts():
    for side, s in (('L', -1), ('R', 1)):
        # Period side panel: lifts out, up and folds right over to lie on top of the hood.
        h = movable('Hood_' + side, (s * 0.43, 1.14, 1.24), 'hinge', (0, 0, 1), -270 if s < 0 else 270, speed=0.9,
                    fixture='car-hood-' + side.lower())
        box(h, (s * 0.425, 0.92, 1.24), (0.02, 0.38, 0.88), 'Car_Paint')
        for k in range(12):
            z = 1.52 - k * 0.045
            box(h, (s * 0.437, 0.97, z), (0.006, 0.16, 0.012), 'Car_Paint')
        box(h, (s * 0.437, 1.03, 1.60), (0.004, 0.012, 0.10), 'Car_Stripe')
        collider('Hood_' + side, (s * 0.425, 0.92, 1.24), (0.04, 0.38, 0.88))
        control('hood-' + side, 'hood', 'Hood_' + side, (s * 0.43, 0.92, 1.24), (0.05, 0.38, 0.86), '发动机罩')
    rc = movable('RadiatorCap', (0, 1.14, 1.71), 'screw', (0, 1, 0), 150, (0, 0.035, 0), speed=2.2, fixture='car-radiator-cap')
    lathe(rc, (0, 1.14, 1.71), (0, 1, 0), [(0, 0), (0.035, 0), (0.035, 0.02), (0.022, 0.03), (0, 0.035)], 'Car_Chrome', seg=14)
    cyl(rc, (0, 1.175, 1.71), (0, 1.20, 1.71), 0.012, 'Car_Chrome', seg=8)
    disc(rc, (0, 1.215, 1.71), (0, 0, 1), 0.024, 0.024, 0.008, 'Car_Chrome', seg=14)
    disc(rc, (0, 1.215, 1.7125), (0, 0, 1), 0.018, 0.018, 0.002, 'Car_Gauge', seg=14)
    control('radiatorCap', 'radiatorCap', 'RadiatorCap', (0, 1.18, 1.71), (0.08, 0.08, 0.08), '水箱盖')
    fc = movable('FuelCap', (0, 1.205, 0.62), 'screw', (0, 1, 0), 160, (0, 0.03, 0), speed=2.2, fixture='car-fuel-cap')
    lathe(fc, (0, 1.20, 0.62), (0, 1, 0), [(0, 0), (0.04, 0), (0.04, 0.018), (0.03, 0.026), (0, 0.028)], 'Car_Chrome', seg=14)
    box(fc, (0, 1.232, 0.62), (0.06, 0.008, 0.012), 'Car_Chrome')
    control('fuelCap', 'fuelCap', 'FuelCap', (0, 1.22, 0.62), (0.10, 0.06, 0.10), '油箱盖')
    # Windscreen hinged at the header so the lower edge pushes out for air.
    w = movable('Windshield', (0, 1.665, 0.31), 'hinge', (1, 0, 0), -12, speed=1.2, fixture='car-windshield')
    box(w, (0, 1.44, 0.31), (1.14, 0.40, 0.006), 'Car_Glass')
    box(w, (0, 1.65, 0.31), (1.20, 0.03, 0.03), 'Car_Paint')
    box(w, (0, 1.235, 0.31), (1.20, 0.03, 0.03), 'Car_Paint')
    for s in (-1, 1):
        box(w, (s * 0.585, 1.44, 0.31), (0.03, 0.44, 0.03), 'Car_Paint')
    collider('Windshield', (0, 1.44, 0.31), (1.20, 0.44, 0.03))
    wiper = movable('WiperArm', (DRIVER_X, 1.665, 0.345), 'sweep', (0, 0, 1), 40, speed=1.0)
    cyl(wiper, (DRIVER_X, 1.665, 0.335), (DRIVER_X, 1.665, 0.36), 0.012, 'Car_Chrome', seg=8)
    box(wiper, (DRIVER_X, 1.53, 0.345), (0.008, 0.26, 0.006), 'Car_Chrome')
    box(wiper, (DRIVER_X, 1.47, 0.338), (0.012, 0.24, 0.010), 'Car_Rubber')
    ws = movable('WiperSwitch', (WIPER_MOTOR_X, 1.612, 0.245), 'hinge', (1, 0, 0), 35, speed=4.0, fixture='car-wipers')
    box(ws, (WIPER_MOTOR_X, 1.60, 0.245), (0.006, 0.025, 0.006), 'Car_Black')
    control('wiperSwitch', 'wiper', 'WiperSwitch', (WIPER_MOTOR_X, 1.605, 0.25), (0.05, 0.05, 0.05), '雨刷开关')
    v = movable('SunVisor', (VISOR_X, 1.682, 0.26), 'hinge', (1, 0, 0), -80, speed=2.0, fixture='car-visor')
    box(v, (VISOR_X, 1.676, 0.16), (0.30, 0.01, 0.20), 'Car_Headliner')
    control('visor', 'visor', 'SunVisor', (VISOR_X, 1.676, 0.16), (0.30, 0.02, 0.20), '遮阳板')
    # Front crank (mounted only after it is taken from the trunk).
    cr = movable('CrankMounted', (0, 0.50, 1.79), 'spin', (0, 0, 1), 1080, speed=0.7)
    cyl(cr, (0, 0.50, 1.76), (0, 0.50, 1.86), 0.010, 'Car_Black', seg=8)
    box(cr, (0, 0.56, 1.86), (0.014, 0.13, 0.014), 'Car_Black')
    cyl(cr, (0, 0.62, 1.86), (0, 0.62, 1.95), 0.012, 'Car_Wood', seg=8)


def build_interior_parts():
    # Steering wheel rotates on the column; hub switches stay with the column.
    sw = movable('SteeringWheel', STEER_CENTRE, 'spin', STEER_AXIS, 0, speed=1.0)
    torus(sw, STEER_CENTRE, STEER_AXIS, 0.20, 0.013, 'Car_Black', seg=28, rseg=8)
    _, e1, e2 = frame(STEER_AXIS)
    for k in range(3):
        t = math.radians(90 + 120 * k)
        d = e1 * math.cos(t) + e2 * math.sin(t)
        cyl(sw, STEER_CENTRE + d * 0.045, STEER_CENTRE + d * 0.195, 0.008, 'Car_Black', seg=6)
    hub = STEER_CENTRE - STEER_AXIS * 0.012
    horn = movable('HornButton', hub, 'slide', STEER_AXIS, offset=tuple(STEER_AXIS * 0.006), speed=12)
    disc(horn, hub - STEER_AXIS * 0.012, STEER_AXIS, 0.022, 0.022, 0.012, 'Car_Black', seg=16)
    control('horn', 'horn', 'HornButton', hub - STEER_AXIS * 0.012, (0.04, 0.04, 0.04), '喇叭')
    ls = movable('LightSwitch', hub, 'steps', STEER_AXIS, 70, speed=4)
    box(ls, hub - STEER_AXIS * 0.02 + e1 * 0.062, (0.014, 0.014, 0.014), 'Car_Chrome')
    cyl(ls, hub - STEER_AXIS * 0.02, hub - STEER_AXIS * 0.02 + e1 * 0.064, 0.005, 'Car_Chrome', seg=6)
    control('lightSwitch', 'lights', 'LightSwitch', hub - STEER_AXIS * 0.02 + e1 * 0.062, (0.03, 0.03, 0.03), '灯光开关')
    lever_base = STEER_CENTRE + STEER_AXIS * 0.06
    for name, sign, label, fixture in (('SparkLever', -1, '火花提前拨杆', 'car-spark-advanced'), ('HandThrottle', 1, '手油门拨杆', 'car-hand-throttle')):
        lv = movable(name, lever_base, 'hinge', STEER_AXIS, 30 * sign, speed=3, fixture=fixture)
        tip = lever_base + (e1 * sign * 0.10) + e2 * 0.04
        cyl(lv, lever_base, tip, 0.005, 'Car_Chrome', seg=6)
        disc(lv, tip, STEER_AXIS, 0.010, 0.010, 0.008, 'Car_Black', seg=10)
        control(name, 'spark' if sign < 0 else 'handThrottle', name, tip, (0.04, 0.04, 0.04), label)
    # Ignition key and choke on the dash; starter button on the toe board.
    key = movable('IgnitionKey', (0, 0.975, 0.256), 'hinge', (0, 0, -1), 60, speed=5, fixture='car-ignition')
    box(key, (0, 0.975, 0.250), (0.004, 0.004, 0.014), 'Car_Brass')
    box(key, (0, 0.99, 0.243), (0.014, 0.024, 0.004), 'Car_Brass')
    control('ignition', 'ignition', 'IgnitionKey', (0, 0.972, 0.248), (0.045, 0.04, 0.03), '点火开关')
    ch = movable('ChokeKnob', (0.20, 0.975, 0.262), 'slide', (0, 0, -1), offset=(0, 0, -0.045), speed=4, fixture='car-choke')
    cyl(ch, (0.20, 0.975, 0.262), (0.20, 0.975, 0.236), 0.004, 'Car_Chrome', seg=6)
    lathe(ch, (0.20, 0.975, 0.236), (0, 0, -1), [(0, 0), (0.012, 0), (0.013, 0.010), (0.009, 0.016), (0, 0.017)], 'Car_Black', seg=12)
    control('choke', 'choke', 'ChokeKnob', (0.20, 0.975, 0.235), (0.04, 0.04, 0.04), '阻风门拉钮')
    toe_n = rot_x(-32) @ Vector((0, 1, 0))
    st_c = Vector((-0.02, 0.585, 0.42))
    st = movable('StarterButton', st_c, 'slide', -toe_n, offset=tuple(-toe_n * 0.010), speed=10)
    cyl(st, st_c - toe_n * 0.005, st_c + toe_n * 0.016, 0.018, 'Car_Chrome', seg=12)
    control('starter', 'starter', 'StarterButton', st_c + toe_n * 0.01, (0.06, 0.05, 0.06), '起动钮')
    # Pedals pivot under the toe board.
    for name, x, pad in (('Pedal_Clutch', -0.42, (0.07, 0.015, 0.09)), ('Pedal_Brake', -0.20, (0.07, 0.015, 0.09)),
                         ('Pedal_Throttle', -0.08, (0.04, 0.012, 0.08))):
        pv = movable(name, (x, 0.46, 0.50), 'hinge', (1, 0, 0), 18, speed=6)
        box(pv, (x, 0.55, 0.46), (0.018, 0.20, 0.018), 'Car_Black', rot_x(-20))
        box(pv, (x, 0.645, 0.425), pad, 'Car_Rubber', rot_x(-55))
    # Gear lever and hand brake rise from the floor.
    gl = movable('GearLever', (0.0, 0.50, 0.06), 'gear', (1, 0, 0), 0, speed=5)
    cyl(gl, (0.0, 0.50, 0.06), (-0.03, 1.02, -0.12), 0.011, 'Car_Chrome', seg=8)
    lathe(gl, (-0.03, 1.02, -0.12), (0, 1, 0), [(0, -0.026), (0.02, -0.018), (0.026, 0), (0.02, 0.018), (0, 0.026)], 'Car_Black', seg=12)
    box(gl, (0, 0.512, 0.06), (0.10, 0.025, 0.10), 'Car_Rubber')
    control('gear', 'gear', 'GearLever', (-0.03, 1.02, -0.12), (0.06, 0.06, 0.06), '变速杆')
    # Keep the grip clear of the gear knob in reverse with the brake raised.
    # Move the whole assembly (pivot, mesh and target) together, 14 cm right.
    hb = movable('Handbrake', (0.26, 0.50, 0.08), 'hinge', (1, 0, 0), -24, speed=5)
    cyl(hb, (0.26, 0.50, 0.08), (0.26, 0.93, -0.04), 0.011, 'Car_Chrome', seg=8)
    cyl(hb, (0.26, 0.88, -0.026), (0.26, 0.95, -0.046), 0.017, 'Car_Black', seg=10)
    control('handbrake', 'handbrake', 'Handbrake', (0.26, 0.91, -0.035), (0.05, 0.08, 0.05), '手刹杆')
    # Gauge needles turn about the dash normal.
    for name, x in (('Needle_Amp', -0.07), ('Needle_Speed', 0.0), ('Needle_Fuel', 0.07)):
        nd = movable(name, (x, 1.06, 0.2525), 'gauge', (0, 0, -1), 0, speed=6)
        box(nd, (x, 1.071, 0.2525), (0.003, 0.022, 0.002), 'Car_Needle')
        disc(nd, (x, 1.06, 0.2522), (0, 0, -1), 0.004, 0.004, 0.003, 'Car_Black', seg=8)


def build_rear_parts():
    lid = movable('TrunkLid', (0, 1.17, -1.73), 'hinge', (1, 0, 0), 85, speed=1.3, fixture='car-trunk-lid')
    box(lid, (0, 1.12, -1.98), (1.12, 0.10, 0.50), 'Car_Black')
    box(lid, (0, 1.071, -1.98), (1.06, 0.004, 0.44), 'Car_Trim')
    for x in (-0.56, 0.56):
        box(lid, (x, 1.12, -1.98), (0.012, 0.105, 0.50), 'Car_Chrome')
    collider('TrunkLid', (0, 1.12, -1.98), (1.12, 0.10, 0.50))
    control('trunkLid', 'trunkLid', 'TrunkLid', (0, 1.13, -2.10), (1.0, 0.12, 0.26), '行李箱盖')
    for side, x in (('L', -0.30), ('R', 0.30)):
        st = movable('Strap_' + side, (x, 1.17, -2.235), 'hinge', (1, 0, 0), 25, speed=3, parent='TrunkLid',
                     fixture='car-strap-' + side.lower())
        box(st, (x, 1.176, -1.98), (0.05, 0.008, 0.50), 'Car_Tan')
        box(st, (x, 0.955, -2.239), (0.05, 0.43, 0.008), 'Car_Tan')
        box(st, (x, 0.765, -2.243), (0.06, 0.03, 0.006), 'Car_Chrome')
        control('strap-' + side, 'strap', 'Strap_' + side, (x, 0.80, -2.245), (0.08, 0.12, 0.03), '行李箱皮带')
    # Trunk contents.
    items = part('TrunkItems')
    cyl(items, (-0.42, 0.75, -1.84), (-0.08, 0.75, -1.84), 0.045, 'Car_Canvas', seg=12)
    box(items, (-0.25, 0.75, -1.84), (0.02, 0.095, 0.095), 'Car_Tan')
    # Laid out so a person beside the trunk can see each item over the 0.38 m walls.
    torus(items, (0.25, 0.735, -2.04), (0, 1, 0), 0.12, 0.032, 'Car_InnerTube', seg=20, rseg=8)
    cyl(items, (0.30, 0.70, -1.82), (0.30, 0.86, -1.82), 0.055, 'Car_Engine', seg=12)
    cyl(items, (0.30, 0.86, -1.82), (0.30, 0.90, -1.86), 0.008, 'Car_Engine', seg=6)
    box(items, (-0.25, 0.73, -2.10), (0.40, 0.05, 0.16), 'Car_Canvas')
    box(items, (-0.25, 0.763, -2.10), (0.34, 0.016, 0.14), 'Car_Canvas')
    control('item-tools', 'itemTools', 'TrunkItems', (-0.25, 0.75, -1.84), (0.40, 0.10, 0.11), '工具卷')
    control('item-tube', 'itemTube', 'TrunkItems', (0.25, 0.765, -2.04), (0.26, 0.05, 0.26), '备用内胎')
    control('item-oil', 'itemOil', 'TrunkItems', (0.30, 0.86, -1.82), (0.12, 0.08, 0.11), '机油壶')
    control('item-tarp', 'itemTarp', 'TrunkItems', (-0.25, 0.74, -2.10), (0.40, 0.08, 0.17), '雨布')
    # The crank lies along the trunk between the left bundles and the inner tube.
    cs = movable('CrankStowed', (0.015, 0.715, -1.99), 'static', (0, 1, 0))
    cyl(cs, (0.0, 0.715, -1.86), (0.0, 0.715, -2.02), 0.010, 'Car_Black', seg=8)
    box(cs, (0.0, 0.715, -2.02), (0.014, 0.014, 0.03), 'Car_Black')
    box(cs, (0.025, 0.715, -2.035), (0.05, 0.014, 0.014), 'Car_Black')
    cyl(cs, (0.045, 0.715, -2.04), (0.045, 0.715, -2.13), 0.012, 'Car_Wood', seg=8)
    control('crankStowed', 'crankStowed', 'Body', (0.015, 0.73, -1.99), (0.07, 0.06, 0.30), '摇把')


def build_spare_and_wheels():
    sp = part('Spare')
    # Side-mounted spare sits in the left fender well, ahead of the driver's door hinge.
    c = Vector((-0.875, 0.80, 0.70))
    wire_wheel(sp, c, -1)
    box(sp, (-0.75, 0.80, 0.70), (0.20, 0.04, 0.04), 'Car_Black')
    box(sp, (-0.875, 1.175, 0.70), (0.15, 0.02, 0.05), 'Car_Black')
    cyl(sp, (-0.875, 1.18, 0.70), (-0.875, 1.30, 0.70), 0.008, 'Car_Chrome', seg=8)
    box(sp, (-0.875, 1.34, 0.70), (0.02, 0.08, 0.12), 'Car_Chrome')
    box(sp, (-0.862, 1.34, 0.70), (0.002, 0.07, 0.11), 'Car_Mirror')
    collider('Body', (-0.87, 0.80, 0.70), (0.14, 0.74, 0.74))
    control('spare', 'spare', 'Body', (-0.88, 0.80, 0.70), (0.15, 0.74, 0.74), '备胎')
    for name, x, z in (('Wheel_FL', -TRACK_HALF, FRONT_AXLE), ('Wheel_FR', TRACK_HALF, FRONT_AXLE),
                       ('Wheel_RL', -TRACK_HALF, REAR_AXLE), ('Wheel_RR', TRACK_HALF, REAR_AXLE)):
        w = movable(name, (x, WHEEL_RADIUS, z), 'wheel', (1, 0, 0), 0)
        wire_wheel(w, Vector((x, WHEEL_RADIUS, z)), -1 if x < 0 else 1)
        LAYOUT['wheels'].append({'id': name[-2:], 'center': [x, WHEEL_RADIUS, z], 'steer': name[-2] == 'F',
                                 'drive': name[-2] == 'R'})


def wire_wheel(p, c, outward):
    ax = Vector((1, 0, 0))
    torus(p, c, ax, WHEEL_RADIUS - TIRE_SECTION / 2, TIRE_SECTION / 2, 'Car_Tire', seg=28, rseg=10)
    torus(p, c, ax, 0.255, 0.014, 'Car_Spoke', seg=28, rseg=6)
    hub_out = c + ax * outward * 0.07
    cyl(p, c - ax * outward * 0.045, hub_out, 0.065, 'Car_Spoke', seg=14)
    lathe(p, hub_out, ax * outward, [(0.05, 0.0), (0.045, 0.02), (0.025, 0.035), (0, 0.04)], 'Car_Chrome', seg=14)
    _, e1, e2 = frame(ax)
    for k in range(16):
        t = 2 * math.pi * k / 16
        d = e1 * math.cos(t) + e2 * math.sin(t)
        lace = ax * (0.03 if k % 2 else -0.03)
        cyl(p, c + lace + d * 0.06, c + d * 0.245, 0.0045, 'Car_Spoke', seg=5)


def seats():
    # The eye sits straight above the seat pivot, so turning the head never moves the eye.
    rows = [('driver', '驾驶座', -0.30, -0.50, 'F', -1), ('passenger', '副驾驶座', 0.30, -0.50, 'F', 1),
            ('rearLeft', '后排左座', -0.30, -1.20, 'R', -1), ('rearRight', '后排右座', 0.30, -1.20, 'R', 1)]
    for sid, label, x, z, row, side in rows:
        # Outside the running board (x 0.88) and >=0.3 m from the open door leaf.
        exit_z = -0.36 if row == 'F' else -0.66
        other = 1 if side < 0 else -1
        LAYOUT['seats'].append({
            'id': sid, 'label': label, 'sit': [x, 0.80, z], 'eye': [0.0, 0.72, 0.0],
            'row': row, 'side': side, 'driver': sid == 'driver',
            'exitNear': [side * 1.30, 0.0, exit_z], 'exitFar': [other * 1.30, 0.0, exit_z],
            'doors': ['Door_' + row + ('L' if side < 0 else 'R'), 'Door_' + row + ('R' if side < 0 else 'L')],
        })
        control('seat-' + sid, 'seat', 'Body', (x, 0.83, z - 0.03), (0.46, 0.12, 0.40), label)


# ---------------------------------------------------------------------------
# Blender scene assembly and export
# ---------------------------------------------------------------------------
def make_materials():
    mats = {}
    for name, (hex_color, metal, rough) in MATERIALS.items():
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        rgb = tuple(int(hex_color[i:i + 2], 16) / 255 for i in (0, 2, 4))
        lin = tuple(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in rgb)
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*lin, 1)
        bsdf.inputs['Metallic'].default_value = metal
        bsdf.inputs['Roughness'].default_value = rough
        if name == 'Car_Glass':
            bsdf.inputs['Alpha'].default_value = 0.25
        m.diffuse_color = (*lin, 0.35 if name == 'Car_Glass' else 1)
        mats[name] = m
    return mats


def build_objects(collection, mats):
    root = bpy.data.objects.new('HotelCarVisual', None)
    collection.objects.link(root)
    for name, pos in (('FrontMarker', (0, 0, 3)), ('EastMarker', (3, 0, 0))):
        e = bpy.data.objects.new(name, None)
        e.location = u(pos)
        e.parent = root
        collection.objects.link(e)
    # Only translations are used, so local location = world pivot - world parent pivot.
    holders = {'Body': (root, Vector((0, 0, 0)))}
    for name in PART_ORDER:
        p = PARTS[name]
        holder, holder_pos = root, Vector((0, 0, 0))
        if p.pivot is not None:
            parent_obj, parent_pos = holders.get(p.parent or 'Body', holders['Body'])
            e = bpy.data.objects.new('Pivot_' + name, None)
            e.empty_display_size = 0.05
            collection.objects.link(e)
            e.parent = parent_obj
            world = u(p.pivot)
            e.location = world - parent_pos
            holders[name] = (e, world)
            holder, holder_pos = e, world
        mesh = bpy.data.meshes.new(name + '_Mesh')
        bm = bmesh.new()
        vmap = [bm.verts.new(u(v)) for v in p.verts]
        bm.verts.ensure_lookup_table()
        used = []
        for f, m in zip(p.faces, p.mats):
            try:
                face = bm.faces.new([vmap[i] for i in f])
            except ValueError:
                continue
            if m not in used:
                used.append(m)
            face.material_index = used.index(m)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bmesh.ops.triangulate(bm, faces=bm.faces, ngon_method='BEAUTY', quad_method='BEAUTY')
        bm.to_mesh(mesh)
        bm.free()
        for m in used:
            mesh.materials.append(mats[m])
        obj = bpy.data.objects.new(name, mesh)
        collection.objects.link(obj)
        obj.parent = holder
        obj.location = -holder_pos
    bpy.context.view_layer.update()
    return root


def render_previews(out_dir, collection):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.display.shading.light = 'STUDIO'
    scene.display.shading.color_type = 'MATERIAL'
    scene.display.shading.show_shadows = True
    scene.render.resolution_x, scene.render.resolution_y = 1400, 900
    scene.render.film_transparent = False
    world = bpy.data.worlds.new('CarPreviewWorld')
    scene.world = world
    cam_data = bpy.data.cameras.new('PreviewCam')
    cam = bpy.data.objects.new('PreviewCam', cam_data)
    bpy.context.scene.collection.objects.link(cam)
    scene.camera = cam
    shots = {
        'front-three-quarter': ((-4.2, 1.6, 4.6), (0, 0.8, 0.2), 40),
        'side-left': ((-6.5, 1.0, -0.15), (0, 0.85, -0.15), 38),
        'rear-three-quarter': ((3.6, 1.8, -5.0), (0, 0.8, -0.6), 40),
        'driver-view': ((-0.30, 1.52, -0.50), (-0.20, 1.15, 1.2), 70),
        'top': ((0, 9.5, -0.2), (0, 0, -0.2), 30),
    }
    for name, (eye, target, lens_fov) in shots.items():
        cam.location = u(eye)
        direction = u(target) - u(eye)
        cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        cam_data.angle = math.radians(lens_fov)
        scene.render.filepath = str(out_dir / ('preview-' + name + '.png'))
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(cam)


def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument('--output', default='assets/source/car')
    ap.add_argument('--previews', default='')
    args = ap.parse_args(argv)
    out = Path(args.output).resolve()
    out.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1.0

    build_body()
    build_doors()
    build_front_parts()
    build_interior_parts()
    build_rear_parts()
    build_spare_and_wheels()
    seats()

    collection = bpy.data.collections.new('HotelCar')
    bpy.context.scene.collection.children.link(collection)
    mats = make_materials()
    build_objects(collection, mats)

    tri_count = sum(len(o.data.polygons) for o in collection.objects if o.type == 'MESH')
    LAYOUT.update({
        'name': '旅馆用车', 'version': 1, 'units': 'metres, Unity car-local (+X right, +Y up, +Z forward)',
        'wheelRadius': WHEEL_RADIUS, 'wheelbase': WHEELBASE, 'trackHalf': TRACK_HALF,
        'centreOfMass': [0.0, 0.55, 0.10], 'mass': 1100.0,
        'steerCentre': list(STEER_CENTRE), 'steerAxis': list(STEER_AXIS),
        'headlampLeft': [-0.50, 0.98, 1.70], 'headlampRight': [0.50, 0.98, 1.70], 'tailLamp': [-0.74, 0.98, -1.82],
        'domeLamp': [0.0, 1.64, -0.62], 'enginePoint': [0.0, 0.80, 1.30],
        'shelter': {'centre': [0.0, -0.69], 'footprint': [1.30, 2.06], 'eaves': 1.72, 'rise': 0.06},
        'windshieldRain': {'centre': [0.0, 1.44, 0.302], 'size': [1.12, 0.38]},
        'triangles': tri_count,
    })
    (out / 'hotel-car-layout.json').write_text(json.dumps(LAYOUT, ensure_ascii=False, indent=1), encoding='utf-8')

    bpy.ops.object.select_all(action='DESELECT')
    for obj in collection.objects:
        obj.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(out / 'HotelCar.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                             axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP')
    if args.previews:
        prev = Path(args.previews).resolve()
        prev.mkdir(parents=True, exist_ok=True)
        render_previews(prev, collection)
    bpy.ops.wm.save_as_mainfile(filepath=str(out / 'HotelCar.blend'), compress=True)
    print('HOTEL_CAR_SOURCE_PASS parts=%d controls=%d triangles=%d' % (len(PART_ORDER), len(LAYOUT['controls']), tri_count))


main()
