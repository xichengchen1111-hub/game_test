"""Small reusable low-poly shapes for the rail kit, in Unity metres (X east, Y up, Z north)."""
import math


def prism(b, name, front, depth_vec, mat):
    """Closed prism from a convex front polygon (list of (x,y,z)) extruded by depth_vec."""
    n = len(front)
    back = [(p[0] + depth_vec[0], p[1] + depth_vec[1], p[2] + depth_vec[2]) for p in front]
    faces = [tuple(range(n)), tuple(range(2 * n - 1, n - 1, -1))]
    for i in range(n):
        j = (i + 1) % n
        faces.append((i, j, n + j, n + i))
    return b.authored_mesh(name, list(front) + back, faces, mat)


def archivolt(b, name, cx, cy, face_z, outward, r_in, r_out, proud, blocks=9, mat='RailStone', key_rise=.16, key_proud=.03, start=0.0, end=math.pi):
    """Radial voussoir blocks on a wall face; the centre block is a projecting keystone."""
    gap = .012
    span = (end - start) / blocks
    for k in range(blocks):
        a0 = start + k * span + gap / 2
        a1 = start + (k + 1) * span - gap / 2
        key = k == blocks // 2 and blocks % 2 == 1
        ro = r_out + (key_rise if key else 0)
        pr = proud + (key_proud if key else 0)
        pts = [(cx + r_in * math.cos(a0), cy + r_in * math.sin(a0), face_z),
               (cx + ro * math.cos(a0), cy + ro * math.sin(a0), face_z),
               (cx + ro * math.cos(a1), cy + ro * math.sin(a1), face_z),
               (cx + r_in * math.cos(a1), cy + r_in * math.sin(a1), face_z)]
        if key:
            # Keystones have parallel vertical sides rather than a fan.
            w = (r_in * math.sin(span / 2)) * 1.25
            pts = [(cx - w, cy + r_in - .02, face_z), (cx - w * 1.35, cy + ro, face_z),
                   (cx + w * 1.35, cy + ro, face_z), (cx + w, cy + r_in - .02, face_z)]
        prism(b, f'{name}_{k}', pts, (0, 0, outward * pr), mat)


def ring_band(b, name, cx, cy, face_z, outward, r_in, r_out, proud, mat, segments=24, start=0.0, end=math.tau):
    """Continuous flat annulus (or arc) moulding on a wall face, built as a closed solid."""
    pts = []
    faces = []
    for i in range(segments + 1):
        a = start + (end - start) * i / segments
        for r in (r_in, r_out):
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a), face_z))
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a), face_z + outward * proud))
    for i in range(segments):
        k = i * 4
        n = k + 4
        faces += [(k + 1, k + 3, n + 3, n + 1), (k, n, n + 2, k + 2), (k + 2, n + 2, n + 3, k + 3), (k, k + 1, n + 1, n)]
    if end - start < math.tau - 1e-6:
        faces += [(0, 2, 3, 1), (segments * 4, segments * 4 + 1, segments * 4 + 3, segments * 4 + 2)]
    return b.authored_mesh(name, pts, faces, mat)


def pyramid(b, name, centre, half_w, half_d, height, mat, apex_half=0.0):
    x, y, z = centre
    base = [(x - half_w, y, z - half_d), (x + half_w, y, z - half_d), (x + half_w, y, z + half_d), (x - half_w, y, z + half_d)]
    if apex_half > 0:
        top = [(x - apex_half, y + height, z - apex_half), (x + apex_half, y + height, z - apex_half),
               (x + apex_half, y + height, z + apex_half), (x - apex_half, y + height, z + apex_half)]
        faces = [(0, 1, 2, 3), (7, 6, 5, 4), (0, 4, 5, 1), (1, 5, 6, 2), (2, 6, 7, 3), (3, 7, 4, 0)]
        return b.authored_mesh(name, base + top, faces, mat)
    faces = [(0, 1, 2, 3), (0, 4, 1), (1, 4, 2), (2, 4, 3), (3, 4, 0)]
    return b.authored_mesh(name, base + [(x, y + height, z)], faces, mat)


def globe(b, name, centre, radius, mat, lat=6, lon=12, squash=1.0):
    x, y, z = centre
    pts = [(x, y - radius * squash, z)]
    for i in range(1, lat):
        t = -math.pi / 2 + math.pi * i / lat
        for j in range(lon):
            a = math.tau * j / lon
            pts.append((x + radius * math.cos(t) * math.cos(a), y + radius * squash * math.sin(t), z + radius * math.cos(t) * math.sin(a)))
    pts.append((x, y + radius * squash, z))
    faces = []
    top = len(pts) - 1
    for j in range(lon):
        faces.append((0, 1 + (j + 1) % lon, 1 + j))
    for i in range(lat - 2):
        r0 = 1 + i * lon
        r1 = r0 + lon
        for j in range(lon):
            k = (j + 1) % lon
            faces.append((r0 + j, r0 + k, r1 + k, r1 + j))
    last = 1 + (lat - 2) * lon
    for j in range(lon):
        faces.append((last + j, last + (j + 1) % lon, top))
    return b.authored_mesh(name, pts, faces, mat)


def frustum(b, name, base_centre, r0, r1, height, mat, segments=16):
    """Vertical truncated cone (capital bells, chimney pots, stove tops)."""
    x, y, z = base_centre
    pts = []
    for yy, r in ((y, r0), (y + height, r1)):
        for i in range(segments):
            a = math.tau * i / segments
            pts.append((x + r * math.cos(a), yy, z + r * math.sin(a)))
    faces = [tuple(range(segments - 1, -1, -1)), tuple(range(segments, 2 * segments))]
    for i in range(segments):
        j = (i + 1) % segments
        faces.append((i, j, segments + j, segments + i))
    return b.authored_mesh(name, pts, faces, mat)


def lantern(b, name, base, size=.24, height=.34, glow_mat='RailGlow', frame='RailIron', cap='RailIron', finial=True):
    """Square period lantern standing on `base` (centre of its foot)."""
    x, y, z = base
    h = size / 2
    b.box(name + '_Foot', (x, y + .02, z), (size * .62, .04, size * .62), frame, bevel=.004)
    b.box(name + '_Glass', (x, y + .04 + height / 2, z), (size * .80, height * .92, size * .80), glow_mat, bevel=0)
    for sx in (-1, 1):
        for sz in (-1, 1):
            b.box(f'{name}_Post{sx}{sz}', (x + sx * h * .86, y + .04 + height / 2, z + sz * h * .86), (.022, height, .022), frame, bevel=.002)
    b.box(name + '_Rim', (x, y + .04 + height + .015, z), (size * 1.05, .03, size * 1.05), frame, bevel=.004)
    pyramid(b, name + '_Cap', (x, y + .07 + height, z), h * 1.12, h * 1.12, size * .55, cap, apex_half=size * .08)
    if finial:
        b.cylinder(name + '_Finial', (x, y + .07 + height + size * .62, z), .018, size * .22, cap, axis=(0, 1, 0), vertices=8)
    return y + .07 + height + size * .55


def wall_lantern(b, name, wall_point, outward, size=.24, height=.34):
    """Scrolled iron wall bracket carrying a hanging square lantern."""
    x, y, z = wall_point
    ox, oz = outward
    reach = .40
    b.box(name + '_Plate', (x + ox * .015, y, z + oz * .015), (.14 if oz else .03, .42, .03 if oz else .14), 'RailIron', bevel=.004)
    arm_end = (x + ox * reach, y + .14, z + oz * reach)
    b.beam(name + '_Arm', (x + ox * .03, y + .14, z + oz * .03), arm_end, .016, 'RailIron', vertices=8)
    b.beam(name + '_Strut', (x + ox * .03, y - .17, z + oz * .03), (x + ox * reach * .7, y + .14, z + oz * reach * .7), .011, 'RailIron', vertices=6)
    b.ring(name + '_Scroll', (x + ox * reach * .42, y + .02, z + oz * reach * .42), .085, .007, 'RailIron', axis=(oz, 0, -ox) if oz or ox else (1, 0, 0), segments=12, ring_segs=5)
    b.beam(name + '_Drop', arm_end, (arm_end[0], y + .02, arm_end[2]), .009, 'RailIron', vertices=6)
    lantern_top = y + .02
    foot = lantern_top - (.07 + height + size * .55) - .03
    lantern(b, name, (arm_end[0], foot, arm_end[2]), size, height, finial=False)
    return (arm_end[0], foot + .04 + height / 2, arm_end[2])


def hanging_lantern(b, name, centre_glass, ceiling_y, size=.30, height=.42):
    x, y, z = centre_glass
    foot = y - .04 - height / 2
    top = lantern(b, name, (x, foot, z), size, height, finial=False)
    b.ring(name + '_Hook', (x, top + .05, z), .035, .007, 'RailIron', axis=(1, 0, 0), segments=10, ring_segs=5)
    b.beam(name + '_Cable', (x, top + .08, z), (x, ceiling_y, z), .010, 'RailIron', vertices=6)
    b.cylinder(name + '_Rose', (x, ceiling_y - .015, z), .07, .03, 'RailIron', axis=(0, 1, 0), vertices=12)
