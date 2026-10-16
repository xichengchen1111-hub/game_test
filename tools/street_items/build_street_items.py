"""1931 street-furniture kit: ten game-ready kinds for the town (user-approved 2026-10-16).

blender.exe --background --factory-startup --python-exit-code 1 --python tools/street_items/build_street_items.py -- \
    --output assets/source/street-items --unity game/Assets/Art/StreetItems [--previews output/demo/street-items/blender]

Designs follow docs/concepts/street-items/round-01 (concept renders) at game density. Each kind is one
joined mesh object Item_<kind> with its origin at the footprint centre on the ground and its front
(the street side) towards Blender -Y; a child empty Front_<kind> sits 1 m in front and Top_<kind>
1 m above, so the Unity assembler can verify axes. Footprints and heights are asserted against the
kinds in docs/maps/town-current/plan-data.json (streetItems). Material names (SI_*) are the Unity
contract (BuildSlice.StreetItems.cs). Nothing here carries a clock, a date, a time or a name.
"""
import bpy, bmesh, math, sys, json, random, argparse
from mathutils import Vector, Matrix, Euler
from pathlib import Path

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
ap = argparse.ArgumentParser(); ap.add_argument('--output', required=True); ap.add_argument('--unity'); ap.add_argument('--previews')
args = ap.parse_args(argv)
ROOT = Path(__file__).resolve().parents[2]
PLAN = json.loads((ROOT / 'docs/maps/town-current/plan-data.json').read_text(encoding='utf-8'))
KINDS = {}
for it in PLAN['streetItems']: KINDS.setdefault(it['kind'], dict(w=it['width'], d=it['depth'], h=it['height']))
FONT = next((p for p in ['C:/Windows/Fonts/georgiab.ttf', '/System/Library/Fonts/Supplemental/Georgia Bold.ttf'] if Path(p).exists()), None)
rnd = random.Random(1931)

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

# ---------------------------------------------------------------- materials (names are the Unity contract)
PALETTE = {
    'SI_IronBlack': (.035, .036, .034), 'SI_HydrantRed': (.30, .075, .055), 'SI_HydrantSilver': (.34, .34, .32),
    'SI_PostOlive': (.13, .15, .085), 'SI_AlarmRed': (.33, .065, .045), 'SI_GoldLeaf': (.6, .46, .2), 'SI_Brass': (.56, .42, .2),
    'SI_Bronze': (.32, .22, .12), 'SI_Galvanised': (.42, .42, .4), 'SI_Granite': (.23, .23, .225), 'SI_GraniteDark': (.17, .17, .165),
    'SI_Carved': (.09, .09, .085), 'SI_Glass': (.6, .65, .65), 'SI_RedGlobe': (.5, .08, .06), 'SI_Water': (.03, .04, .045),
    'SI_Leaf': (.28, .19, .09), 'SI_ColumnGreen': (.045, .065, .055), 'SI_ZincCap': (.3, .34, .32), 'SI_Posters': (1, 1, 1),
    'SI_PlateLitter': (1, 1, 1), 'SI_PlateShine': (1, 1, 1), 'SI_PlateAlarm': (1, 1, 1), 'SI_LitterGreen': (.06, .085, .07),
    'SI_BinWood': (.23, .25, .2), 'SI_BinIron': (.07, .07, .065), 'SI_Stencil': (.62, .58, .48), 'SI_Handle': (.42, .3, .18),
    'SI_Blade': (.18, .18, .17), 'SI_Walnut': (.2, .12, .07), 'SI_StandGreen': (.11, .17, .13), 'SI_Leather': (.12, .05, .035),
    'SI_Tin': (.5, .48, .44), 'SI_Bristle': (.08, .06, .05), 'SI_Soil': (.06, .045, .035), 'SI_HookBack': (.6, .58, .52), 'SI_Dark': (.01, .01, .01),
}
MATS = {}
def M(name):
    if name in MATS: return MATS[name]
    m = bpy.data.materials.new(name); m.diffuse_color = (*PALETTE[name], 1)
    nt = m.node_tree; b = nt.nodes.get('Principled BSDF')
    if b:
        b.inputs['Base Color'].default_value = (*PALETTE[name], 1)
        if name in ('SI_Brass', 'SI_Bronze', 'SI_Galvanised', 'SI_GoldLeaf', 'SI_Tin'): b.inputs['Metallic'].default_value = 1; b.inputs['Roughness'].default_value = .35
        if name == 'SI_Glass': b.inputs['Transmission Weight'].default_value = 1; b.inputs['Roughness'].default_value = .02
        if name in ('SI_Posters', 'SI_PlateLitter', 'SI_PlateShine', 'SI_PlateAlarm') and args.unity:
            t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(str(ROOT / args.unity / (name + '.png')), check_existing=True)
            nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    MATS[name] = m; return m

# ---------------------------------------------------------------- mesh helpers
UV_TILE = .5  # metres per detail-map repeat (box projection)
current = []  # objects of the kind being built

def finish(bm, name, mat, loc=(0, 0, 0), rot=(0, 0, 0), smooth=35, uv=True):
    if uv and not bm.loops.layers.uv:
        layer = bm.loops.layers.uv.new()
        for f in bm.faces:
            n = f.normal; ax = max(range(3), key=lambda i: abs(n[i]))
            for l in f.loops:
                co = l.vert.co
                u, v = {0: (co.y, co.z), 1: (co.x, co.z), 2: (co.x, co.y)}[ax]
                l[layer].uv = (u / UV_TILE, v / UV_TILE)
    me = bpy.data.meshes.new(name); bm.normal_update(); bm.to_mesh(me); bm.free()
    if smooth:
        for p in me.polygons: p.use_smooth = True
        try: me.set_sharp_from_angle(angle=math.radians(smooth))
        except Exception: pass
    me.materials.append(M(mat))
    o = bpy.data.objects.new(name, me); scene.collection.objects.link(o)
    o.location = loc; o.rotation_euler = rot; current.append(o); return o

def lathe(name, prof, mat, seg=24, loc=(0, 0, 0), rot=(0, 0, 0), smooth=40, phase=0.):
    bm = bmesh.new(); rings = []
    for r, z in prof:
        rings.append([bm.verts.new((0, 0, z))] if r <= 1e-6 else [bm.verts.new((r * math.cos(2 * math.pi * i / seg + phase), r * math.sin(2 * math.pi * i / seg + phase), z)) for i in range(seg)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1: continue
        for i in range(seg):
            j = (i + 1) % seg
            if len(a) == 1: bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1: bm.faces.new((a[i], b[0], a[j]))
            else: bm.faces.new((a[i], b[i], b[j], a[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, loc, rot, smooth)

def box(name, size, loc, mat, bevel=.006, seg=1, rot=(0, 0, 0), smooth=30):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1); bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    if bevel > 0: bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(size) * .45), segments=seg, profile=.5, affect='EDGES')
    return finish(bm, name, mat, loc, rot, smooth)

def cyl(name, r, h, loc, mat, seg=16, rot=(0, 0, 0), r2=None, smooth=40):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    return finish(bm, name, mat, loc, rot, smooth)

def prism(name, pts, depth, loc, mat, axis='Y', bevel=.004, rot=(0, 0, 0), smooth=30):
    bm = bmesh.new()
    def P(u, v, w): return {'Y': (u, w, v), 'Z': (u, v, w), 'X': (w, u, v)}[axis]
    a = [bm.verts.new(P(u, v, -depth / 2)) for u, v in pts]; b = [bm.verts.new(P(u, v, depth / 2)) for u, v in pts]
    bm.faces.new(a); bm.faces.new(list(reversed(b)))
    for i in range(len(pts)):
        j = (i + 1) % len(pts); bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bevel > 0: bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=1, profile=.5, affect='EDGES', clamp_overlap=True)
    return finish(bm, name, mat, loc, rot, smooth)

def torus(name, R, r, loc, mat, rot=(0, 0, 0), su=16, sv=6, scale=(1, 1, 1)):
    bm = bmesh.new(); grid = []
    for i in range(su):
        a = 2 * math.pi * i / su; row = []
        for j in range(sv):
            b = 2 * math.pi * j / sv; rr = R + r * math.cos(b)
            row.append(bm.verts.new((rr * math.cos(a) * scale[0], rr * math.sin(a) * scale[1], r * math.sin(b) * scale[2])))
        grid.append(row)
    for i in range(su):
        for j in range(sv): bm.faces.new((grid[i][j], grid[(i + 1) % su][j], grid[(i + 1) % su][(j + 1) % sv], grid[i][(j + 1) % sv]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, loc, rot, 60)

def tube(name, pts, radius, mat, sides=6):
    """Swept circle along a polyline (smooth via Catmull-Rom subdivision), as real mesh."""
    P = [Vector(p) for p in pts]
    if len(P) > 2:
        dense = []
        for i in range(len(P) - 1):
            p0, p1, p2, p3 = P[max(i - 1, 0)], P[i], P[i + 1], P[min(i + 2, len(P) - 1)]
            for k in range(4):
                t = k / 4
                dense.append(.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
        dense.append(P[-1]); P = dense
    bm = bmesh.new(); rings = []
    for i, p in enumerate(P):
        d = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        a = d.orthogonal().normalized(); b = d.cross(a)
        rings.append([bm.verts.new(p + (a * math.cos(2 * math.pi * k / sides) + b * math.sin(2 * math.pi * k / sides)) * radius) for k in range(sides)])
    for r0, r1 in zip(rings, rings[1:]):
        for k in range(sides): bm.faces.new((r0[k], r0[(k + 1) % sides], r1[(k + 1) % sides], r1[k]))
    bm.faces.new(list(reversed(rings[0]))); bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, smooth=70)

def text(name, body, size, loc, mat, rot=(math.pi / 2, 0, 0), extrude=.003):
    cu = bpy.data.curves.new(name, 'FONT'); cu.body = body; cu.size = size; cu.extrude = extrude
    if FONT: cu.font = bpy.data.fonts.load(FONT, check_existing=True)
    cu.align_x = 'CENTER'; cu.align_y = 'CENTER'; cu.resolution_u = 3
    o = bpy.data.objects.new(name, cu); scene.collection.objects.link(o); o.location = loc; o.rotation_euler = rot
    dg = bpy.context.evaluated_depsgraph_get(); me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))
    bpy.data.objects.remove(o); bpy.data.curves.remove(cu)
    bm = bmesh.new(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    bmesh.ops.transform(bm, matrix=Matrix.LocRotScale(Vector(loc), Euler(rot), None), verts=bm.verts)
    return finish(bm, name, mat, smooth=0)

def plate(name, mat, w, h, loc, rot=(math.pi / 2, 0, 0), thick=.004):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1); bmesh.ops.scale(bm, vec=(w, h, thick), verts=bm.verts)
    uv = bm.loops.layers.uv.new()
    for f in bm.faces:
        for l in f.loops:
            x, y, _ = l.vert.co; l[uv].uv = (x / w + .5, y / h + .5)
    return finish(bm, name, mat, loc, rot, 0, uv=False)

def chain(name, a, b, sag, links, mat):
    a, b = Vector(a), Vector(b)
    for k in range(links):
        t = (k + .5) / links; p = a.lerp(b, t); p.z -= sag * 4 * t * (1 - t)
        q = a.lerp(b, t + .01); q.z -= sag * 4 * (t + .01) * (1 - t - .01)
        rot = (q - p).normalized().to_track_quat('X', 'Z').to_euler(); rot.rotate_axis('X', (math.pi / 2) * (k % 2))
        torus(f'{name}_{k}', .011, .0028, p, mat, rot=rot, su=8, sv=4, scale=(1.55, 1, 1))

# ---------------------------------------------------------------- the ten kinds (front = -Y)
def k_hydrant():
    lathe('Barrel', [(0, 0), (.17, 0), (.17, .028), (.155, .035), (.13, .04), (.118, .065), (.108, .085), (.102, .5), (.112, .512), (.142, .518), (.142, .556), (.1, .56)], 'SI_HydrantRed', 32)
    lathe('Bonnet', [(.13, .556), (.132, .565), (.127, .6), (.11, .635), (.084, .662), (.058, .678), (.045, .684), (0, .686)], 'SI_HydrantSilver', 32)
    cyl('Nut', .034, .045, (0, 0, .711), 'SI_HydrantSilver', seg=5)
    cyl('NutWasher', .05, .008, (0, 0, .69), 'SI_HydrantSilver', seg=24)
    for k in range(8):
        a = 2 * math.pi * (k + .5) / 8
        cyl(f'FlangeBolt{k}', .011, .022, (.128 * math.cos(a), .128 * math.sin(a), .567), 'SI_HydrantSilver', seg=6)
        cyl(f'BaseBolt{k}', .012, .02, (.152 * math.cos(a), .152 * math.sin(a), .038), 'SI_HydrantRed', seg=6)
    for s in (-1, 1):
        cyl(f'Outlet{s}', .036, .08, (s * .13, 0, .40), 'SI_HydrantRed', rot=(0, math.pi / 2, 0), seg=16)
        cyl(f'OutletCap{s}', .046, .036, (s * .186, 0, .40), 'SI_HydrantSilver', rot=(0, math.pi / 2, 0), seg=6)
        chain(f'Chain{s}', (s * .2, 0, .372), (s * .107, -.02, .47), .07, 6, 'SI_HydrantSilver')
    cyl('Steamer', .055, .07, (0, -.135, .33), 'SI_HydrantRed', rot=(math.pi / 2, 0, 0), seg=20)
    cyl('SteamerCap', .066, .045, (0, -.188, .33), 'SI_HydrantSilver', rot=(math.pi / 2, 0, 0), seg=6)
    cyl('SteamerNut', .02, .02, (0, -.219, .33), 'SI_HydrantSilver', rot=(math.pi / 2, 0, 0), seg=5)
    chain('ChainS', (0, -.2, .305), (.06, -.09, .46), .09, 7, 'SI_HydrantSilver')
    for z in (.18, .26): torus(f'Band{z}', .104, .006, (0, 0, z), 'SI_HydrantRed', su=32, sv=4)

def k_mailbox():
    w, d = .46, .40
    pts = [(-w / 2, 0), (w / 2, 0), (w / 2, .62)] + [(w / 2 * math.cos(math.pi * k / 12), .62 + .2 * math.sin(math.pi * k / 12)) for k in range(1, 12)] + [(-w / 2, .62)]
    prism('Body', pts, d, (0, 0, .46), 'SI_PostOlive', 'Y', .008)
    box('Hopper', (.36, .05, .15), (0, -.215, 1.0), 'SI_PostOlive', .01, rot=(math.radians(-8), 0, 0))
    cyl('HopperHandle', .011, .3, (0, -.252, .925), 'SI_Brass', rot=(0, math.pi / 2, 0), seg=10)
    for s in (-1, 1): box(f'HandleArm{s}', (.016, .05, .016), (s * .15, -.232, .925), 'SI_PostOlive', .003)
    for i, (x, z, sx, sz) in enumerate([(0, .505, .38, .02), (0, .82, .38, .02), (-.18, .6625, .02, .295), (.18, .6625, .02, .295)]):
        box(f'DoorFrame{i}', (sx, .02, sz), (x, -.207, z), 'SI_PostOlive', .004)
    text('MailText', 'MAIL', .085, (0, -.2045, .705), 'SI_PostOlive', extrude=.004)
    cyl('Lock', .018, .012, (0, -.205, .58), 'SI_Brass', rot=(math.pi / 2, 0, 0), seg=12)
    box('Keyhole', (.006, .004, .016), (0, -.213, .578), 'SI_Dark', 0)
    text('SideText', 'LETTERS', .05, (.2335, 0, .72), 'SI_PostOlive', rot=(math.pi / 2, 0, math.pi / 2), extrude=.003)
    lathe('Rosette', [(0, .004), (.03, .0), (.034, -.004)], 'SI_PostOlive', 12, loc=(0, -.2, 1.12), rot=(math.pi / 2, 0, 0))
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(f'Leg{sx}{sy}', (.035, .035, .48), (sx * .2, sy * .165, .253), 'SI_PostOlive', .005, rot=(sy * math.radians(3), -sx * math.radians(4), 0))
            box(f'Foot{sx}{sy}', (.08, .08, .012), (sx * .215, sy * .18, .006), 'SI_PostOlive', .003)
            for bx in (-1, 1): cyl(f'FootBolt{sx}{sy}{bx}', .008, .012, (sx * .215 + bx * .025, sy * .18 + bx * .025, .016), 'SI_IronBlack', seg=6)
    for sy in (-1, 1): box(f'Brace{sy}', (.42, .02, .03), (0, sy * .172, .2), 'SI_PostOlive', .004)
    box('Cap', (w + .02, d + .03, .02), (0, 0, .46), 'SI_PostOlive', .005)

def k_alarm():
    lathe('Plinth', [(0, 0), (.2, 0), (.2, .05), (.17, .07), (.17, .11), (.13, .13), (.1, .17), (0, .17)], 'SI_AlarmRed', 8, phase=math.pi / 8)
    flutes = [((.068 if k % 2 == 0 else .058) * math.cos(2 * math.pi * k / 32), (.068 if k % 2 == 0 else .058) * math.sin(2 * math.pi * k / 32)) for k in range(32)]
    prism('Shaft', flutes, 1.16, (0, 0, .75), 'SI_AlarmRed', 'Z', 0, smooth=70)
    lathe('Collar', [(0, 1.32), (.085, 1.32), (.095, 1.335), (.095, 1.355), (.115, 1.37), (.115, 1.39), (0, 1.39)], 'SI_AlarmRed', 24)
    torus('GoldRing', .087, .006, (0, 0, 1.33), 'SI_GoldLeaf', su=32, sv=4)
    bw, bd, bh = .30, .2, .40
    pts = [(-bw / 2, 0), (bw / 2, 0), (bw / 2, bh)] + [(bw / 2 * math.cos(math.pi * k / 10), bh + .09 * math.sin(math.pi * k / 10)) for k in range(1, 10)] + [(-bw / 2, bh)]
    prism('Box', pts, bd, (0, 0, 1.39), 'SI_AlarmRed', 'Y', .006)
    hood = [(x * 1.12, (y - bh) * 1.25 + bh + .005) for x, y in pts[2:]] + [(-bw * .56, bh - .01), (bw * .56, bh - .01)]
    prism('Hood', hood, bd + .05, (0, 0, 1.39), 'SI_AlarmRed', 'Y', .005)
    box('Door', (.24, .02, .3), (0, -.108, 1.56), 'SI_AlarmRed', .006)
    box('WindowFrame', (.13, .015, .09), (0, -.121, 1.64), 'SI_GoldLeaf', .003)
    box('Window', (.11, .004, .07), (0, -.1305, 1.64), 'SI_Glass', 0)
    box('HookPlate', (.09, .004, .06), (0, -.1135, 1.64), 'SI_HookBack', 0)
    box('Hook', (.012, .01, .04), (0, -.1215, 1.636), 'SI_Brass', .002, rot=(0, 0, math.radians(-20)))
    plate('AlarmPlate', 'SI_PlateAlarm', .22, .08, (0, -.1205, 1.5))
    for z in (1.46, 1.66): cyl(f'Hinge{z}', .01, .04, (.122, -.112, z), 'SI_AlarmRed', seg=8)
    cyl('Handle', .012, .03, (-.1, -.127, 1.56), 'SI_Brass', rot=(math.pi / 2, 0, 0), seg=10)
    lathe('LampStem', [(0, 1.86), (.03, 1.86), (.02, 1.9), (.02, 1.95), (.045, 1.96), (.045, 1.975), (0, 1.975)], 'SI_AlarmRed', 16)
    lathe('Globe', [(0, 1.975), (.06, 1.985), (.088, 2.03), (.085, 2.08), (.055, 2.115), (0, 2.12)], 'SI_RedGlobe', 20)
    lathe('GlobeCap', [(0, 2.11), (.06, 2.112), (.05, 2.135), (.012, 2.15), (.012, 2.19), (0, 2.2)], 'SI_AlarmRed', 16)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        tube(f'Cage{k}', [(.05 * math.cos(a), .05 * math.sin(a), 1.975), (.095 * math.cos(a), .095 * math.sin(a), 2.04), (.065 * math.cos(a), .065 * math.sin(a), 2.11)], .004, 'SI_AlarmRed', 4)

def k_trough():
    # Fits a 1 m facilities band: bubbler and dog basin at the ends, stele on the back rim.
    L, R, Y = 1.0, .36, -.03
    def stadium(l, r, n=10):
        pts = [(l / 2 + r * math.cos(t), r * math.sin(t)) for t in [-math.pi / 2 + math.pi * k / n for k in range(n + 1)]]
        return pts + [(-l / 2 + r * math.cos(t), r * math.sin(t)) for t in [math.pi / 2 + math.pi * k / n for k in range(n + 1)]]
    bm = bmesh.new(); f = bm.faces.new([bm.verts.new((x, y + Y, .06)) for x, y in stadium(L, R)])
    top = [e for e in bmesh.ops.extrude_face_region(bm, geom=[f])['geom'] if isinstance(e, bmesh.types.BMFace)][0]
    bmesh.ops.translate(bm, vec=(0, 0, .58), verts=top.verts)
    bmesh.ops.inset_individual(bm, faces=[top], thickness=.085, depth=0)
    inner = [e for e in bmesh.ops.extrude_face_region(bm, geom=[top])['geom'] if isinstance(e, bmesh.types.BMFace)][0]
    bmesh.ops.translate(bm, vec=(0, 0, -.44), verts=inner.verts)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    rim = [e for e in bm.edges if all(v.co.z > .6 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=rim, offset=.016, segments=2, profile=.5, affect='EDGES', clamp_overlap=True)
    finish(bm, 'Trough', 'SI_Granite', smooth=40)
    prism('TroughPlinth', [(x, y + Y) for x, y in stadium(L + .04, R + .05)], .06, (0, 0, .03), 'SI_GraniteDark', 'Z', .008)
    prism('Water', [(x, y + Y) for x, y in stadium(L * .96, R - .1)], .004, (0, 0, .562), 'SI_Water', 'Z', 0)
    for k, (x, y) in enumerate([(-.3, -.1), (.25, .07)]):
        prism(f'Leaf{k}', [(0, 0), (.025, .012), (.05, 0), (.025, -.012)], .002, (x, y, .5665), 'SI_Leaf', 'Z', 0, rot=(0, 0, k * 1.3))
    box('Stele', (.7, .16, .62), (0, .33, .95), 'SI_Granite', .015)
    prism('SteleTop', [(-.38, 0), (.38, 0), (.3, .07), (0, .14), (-.3, .07)], .2, (0, .33, 1.26), 'SI_Granite', 'Y', .008)
    box('Panel', (.5, .01, .24), (0, .246, 1.0), 'SI_GraniteDark', .003)
    text('Inscription', 'WATER', .1, (0, .2395, 1.0), 'SI_Carved', extrude=.0015)
    cyl('SpoutBoss', .06, .03, (0, .235, .78), 'SI_Bronze', rot=(math.pi / 2, 0, 0), seg=16)
    tube('Spout', [(0, .23, .78), (0, .15, .78), (0, .1, .72)], .016, 'SI_Bronze')
    lathe('BubblerPost', [(0, .06), (.09, .06), (.08, .12), (.06, .14), (.055, .9), (.07, .94), (0, .94)], 'SI_Granite', 16, loc=(-.96, .1, 0))
    lathe('BubblerBowl', [(0, .94), (.03, .94), (.12, .99), (.135, 1.03), (.125, 1.035), (.105, 1.0), (0, .995)], 'SI_Bronze', 20, loc=(-.96, .1, 0))
    cyl('Bubbler', .012, .05, (-.96, .1, 1.02), 'SI_Bronze', seg=8)
    lathe('DogBasin', [(0, .003), (.16, .003), (.175, .04), (.175, .12), (.15, .12), (.14, .05), (0, .05)], 'SI_Granite', 20, loc=(.93, .12, 0))
    prism('DogWater', [(math.cos(t) * .14, math.sin(t) * .14) for t in [2 * math.pi * k / 16 for k in range(16)]], .003, (.93, .12, .1), 'SI_Water', 'Z', 0)

def k_hitch():
    px = -.6
    prof = [(0, 0), (.09, 0), (.09, .03), (.075, .04), (.07, .07), (.055, .1), (.05, .14), (.044, .84), (.052, .86), (.065, .875), (.065, .9), (.05, .91), (.045, .94),
            (.06, .96), (.068, .99), (.06, 1.02), (.04, 1.04), (.03, 1.06), (0, 1.065)]
    lathe('Post', prof, 'SI_IronBlack', 20, loc=(px, 0, 0))
    for k in range(10):
        a = 2 * math.pi * k / 10; box(f'Flute{k}', (.008, .008, .66), (px + .045 * math.cos(a), .045 * math.sin(a), .48), 'SI_IronBlack', .002)
    lathe('Acorn', [(0, 1.06), (.022, 1.07), (.03, 1.1), (.022, 1.13), (0, 1.14)], 'SI_IronBlack', 12, loc=(px, 0, 0))
    cyl('RingBoss', .02, .03, (px, -.06, .93), 'SI_IronBlack', rot=(math.pi / 2, 0, 0), seg=10)
    torus('TieRing', .045, .007, (px, -.085, .885), 'SI_Brass', rot=(math.radians(80), 0, 0), su=20, sv=6)
    box('StepLow', (.75, .42, .2), (.3, -.02, .1), 'SI_Granite', .02)
    box('StepHigh', (.75, .22, .2), (.3, .08, .3), 'SI_Granite', .02)
    box('StepIron', (.6, .02, .01), (.3, -.22, .2045), 'SI_IronBlack', .003)

def k_column():
    lathe('Base', [(0, 0), (.64, 0), (.64, .06), (.6, .08), (.6, .16), (.56, .2), (.53, .24), (.51, .26), (0, .26)], 'SI_ColumnGreen', 40)
    bm = bmesh.new(); seg, H, r = 48, 2.2, .503
    ring0 = [bm.verts.new((r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg), .26)) for i in range(seg)]
    ring1 = [bm.verts.new((v.co.x, v.co.y, .26 + H)) for v in ring0]
    uv = bm.loops.layers.uv.new()
    for i in range(seg):
        j = (i + 1) % seg; f = bm.faces.new((ring0[i], ring0[j], ring1[j], ring1[i]))
        for l, (u, v) in zip(f.loops, [(i / seg, 0), ((i + 1) / seg, 0), ((i + 1) / seg, 1), (i / seg, 1)]): l[uv].uv = (u, v)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(-170), 3, 'Z'))
    finish(bm, 'Posters', 'SI_Posters', smooth=80, uv=False)
    lathe('Cornice', [(.49, 2.44), (.52, 2.44), (.55, 2.47), (.55, 2.5), (.6, 2.54), (.64, 2.58), (.64, 2.62), (.56, 2.64), (.56, 2.7), (0, 2.7)], 'SI_ColumnGreen', 40)
    for k in range(32):
        a = 2 * math.pi * k / 32; box(f'Dentil{k}', (.035, .03, .04), (.565 * math.cos(a), .565 * math.sin(a), 2.52), 'SI_ColumnGreen', .003, rot=(0, 0, a))
    lathe('Dome', [(.56, 2.7), (.58, 2.72), (.55, 2.8), (.47, 2.9), (.34, 2.98), (.18, 3.03), (.06, 3.05), (0, 3.05)], 'SI_ZincCap', 40)
    for k in range(8):
        a = 2 * math.pi * k / 8
        tube(f'Rib{k}', [(.575 * math.cos(a), .575 * math.sin(a), 2.72), (.5 * math.cos(a), .5 * math.sin(a), 2.875), (.3 * math.cos(a), .3 * math.sin(a), 2.995), (.07 * math.cos(a), .07 * math.sin(a), 3.055)], .012, 'SI_ZincCap')
    lathe('Finial', [(0, 3.04), (.06, 3.05), (.06, 3.08), (.035, 3.1), (.055, 3.15), (.04, 3.2), (.012, 3.24), (.006, 3.36), (0, 3.37)], 'SI_ColumnGreen', 16)

def k_litter():
    R, H, z0, n = .23, .6, .2, 18
    for s in (-1, 1):
        for k in range(n):
            a0 = 2 * math.pi * k / n
            tube(f'Wire{s}{k}', [(R * (1 - .06 * (1 - t)) * math.cos(a0 + s * t * math.pi * .7), R * (1 - .06 * (1 - t)) * math.sin(a0 + s * t * math.pi * .7), z0 + H * t) for t in [i / 4 for i in range(5)]], .0032, 'SI_Galvanised', 4)
    for z, rr in [(z0, R * .94), (z0 + H * .5, R * .97), (z0 + H, R)]: torus(f'Hoop{z}', rr, .007, (0, 0, z), 'SI_Galvanised', su=32, sv=5)
    lathe('Rim', [(R - .01, z0 + H - .02), (R + .012, z0 + H - .02), (R + .016, z0 + H + .01), (R - .004, z0 + H + .015)], 'SI_LitterGreen', 32)
    lathe('Bottom', [(0, z0 - .01), (R * .95, z0 - .01), (R * .95, z0 + .02), (0, z0 + .02)], 'SI_LitterGreen', 24)
    lathe('Band', [(R * .965, z0 + .3), (R * .99, z0 + .3), (R * .99, z0 + .42), (R * .965, z0 + .42)], 'SI_LitterGreen', 32)
    plate('LitterPlate', 'SI_PlateLitter', .2, .066, (0, -R * .99 - .0045, z0 + .36))
    for k in range(3):
        a = 2 * math.pi * k / 3 + math.pi / 6
        tube(f'Leg{k}', [(R * .9 * math.cos(a), R * .9 * math.sin(a), z0 + .02), (R * 1.08 * math.cos(a), R * 1.08 * math.sin(a), .006)], .009, 'SI_LitterGreen')
        cyl(f'LegFoot{k}', .022, .01, (R * 1.08 * math.cos(a), R * 1.08 * math.sin(a), .005), 'SI_LitterGreen', seg=10)
    for k in range(6):  # dry leaves only, never paper
        a, rr = rnd.uniform(0, 6.28), rnd.uniform(0, .16)
        prism(f'DryLeaf{k}', [(0, 0), (.03, .014), (.06, 0), (.03, -.014)], .002, (rr * math.cos(a), rr * math.sin(a), z0 + .025 + k * .012), 'SI_Leaf', 'Z', 0, rot=(rnd.uniform(-.4, .4), rnd.uniform(-.4, .4), a))

def k_sandbin():
    W, D, Hf, Hb = 1.0, .56, .58, .74
    for i in range(4):
        z = .1 + i * .122
        box(f'FrontBoard{i}', (W - .06, .03, .115), (0, -D / 2, z + .058), 'SI_BinWood', .005)
        box(f'BackBoard{i}', (W - .06, .03, .115), (0, D / 2, z + .058), 'SI_BinWood', .005)
    box('BackTop', (W - .06, .03, .14), (0, D / 2, .66), 'SI_BinWood', .005)
    for s in (-1, 1): prism(f'Side{s}', [(-D / 2, .1), (D / 2, .1), (D / 2, Hb), (-D / 2, Hf)], .03, (s * (W / 2 - .015), 0, 0), 'SI_BinWood', 'X', .005)
    ang = math.atan2(Hb - Hf, D); lid = Euler((ang, 0, 0)); zl = (Hf + Hb) / 2
    box('Lid', (W + .06, D + .08, .035), (0, 0, zl + .03), 'SI_BinWood', .006, rot=lid)
    for x in (-.35, 0, .35): box(f'Batten{x}', (.06, D + .02, .02), (x, 0, zl + .058), 'SI_BinWood', .004, rot=lid)
    for x in (-.4, .4):
        box(f'Strap{x}', (.04, D + .085, .006), (x, 0, zl + .0505), 'SI_BinIron', .002, rot=lid)
        cyl(f'Hinge{x}', .012, .09, (x, D / 2 + .03, Hb + .04), 'SI_BinIron', rot=(0, math.pi / 2, 0), seg=8)
    box('Hasp', (.05, .008, .12), (0, -D / 2 - .03, Hf - .02), 'SI_BinIron', .002)
    torus('Staple', .014, .004, (0, -D / 2 - .035, Hf - .08), 'SI_BinIron', rot=(math.pi / 2, 0, 0), su=10, sv=4)
    for s in (-1, 1):
        for z in (.13, .55): box(f'Corner{s}{z}', (.06, .006, .06), (s * (W / 2 - .03), -D / 2 - .018, z), 'SI_BinIron', .002)
        box(f'Runner{s}', (.08, D + .04, .1), (s * .4, 0, .05), 'SI_BinWood', .005)
    text('Stencil', 'SAND', .17, (0, -D / 2 - .0175, .38), 'SI_Stencil', extrude=0)
    text('Stencil2', 'FOR ICY WALKS', .05, (0, -D / 2 - .0175, .22), 'SI_Stencil', extrude=0)
    sx = W / 2 + .025  # shovel leaning on the right side
    tube('ShovelShaft', [(sx, -.17, .27), (sx, .06, .93)], .016, 'SI_Handle')
    torus('ShovelGrip', .045, .011, (sx, .075, .975), 'SI_Handle', rot=(0, math.pi / 2, 0), su=12, sv=5)
    prism('ShovelBlade', [(-.12, .3), (.12, .3), (.11, .06), (0, .0), (-.11, .06)], .004, (sx, -.2, .005), 'SI_Blade', 'X', .002, rot=(math.radians(-14), 0, 0))

def k_shoeshine():
    W, Y = .78, .03
    box('Platform', (W, .95, .5), (0, .1 + Y, .25), 'SI_StandGreen', .01)
    for x in (-.2, .2): box(f'Panel{x}', (.3, .02, .32), (x, -.383 + Y, .26), 'SI_Walnut', .008)
    box('BrassEdge', (W + .01, .02, .02), (0, -.375 + Y, .5), 'SI_Brass', .003)
    box('Step', (W, .3, .2), (0, -.52 + Y, .1), 'SI_StandGreen', .008)
    box('StepTread', (W + .02, .32, .025), (0, -.52 + Y, .2125), 'SI_Walnut', .005)
    box('Top', (W + .03, .98, .03), (0, .1 + Y, .515), 'SI_Walnut', .006)
    box('Seat', (.62, .48, .05), (0, .2 + Y, .76), 'SI_Walnut', .008)
    box('Cushion', (.56, .42, .06), (0, .2 + Y, .81), 'SI_Leather', .02)
    for x in (-.29, .29):
        box(f'FrontLeg{x}', (.05, .05, .23), (x, -.02 + Y, .645), 'SI_Walnut', .006)
        box(f'BackPost{x}', (.05, .05, .78), (x, .42 + Y, .91), 'SI_Walnut', .006, rot=(math.radians(-6), 0, 0))
        box(f'Arm{x}', (.07, .5, .04), (x, .18 + Y, 1.0), 'SI_Walnut', .01)
        box(f'ArmPost{x}', (.04, .04, .2), (x, -.02 + Y, .89), 'SI_Walnut', .005)
    for k in range(5): box(f'Slat{k}', (.07, .025, .5), (-.2 + k * .1, .45 + Y, 1.06), 'SI_Walnut', .005, rot=(math.radians(-6), 0, 0))
    box('BackRail', (.62, .05, .08), (0, .47 + Y, 1.32), 'SI_Walnut', .008, rot=(math.radians(-6), 0, 0))
    box('BackCushion', (.5, .04, .3), (0, .41 + Y, 1.08), 'SI_Leather', .012, rot=(math.radians(-6), 0, 0))
    for x in (-.14, .14):
        cyl(f'RestPost{x}', .015, .19, (x, -.32 + Y, .615), 'SI_Brass', seg=8)
        prism(f'Rest{x}', [(-.04, -.13), (.04, -.13), (.05, -.02), (.035, .1), (0, .13), (-.035, .1), (-.05, -.02)], .02, (x, -.32 + Y, .71), 'SI_Brass', 'Z', .005)
        box(f'Heel{x}', (.07, .03, .05), (x, -.43 + Y, .73), 'SI_Brass', .004)
    box('KitBox', (.32, .18, .14), (.22, -.58 + Y, .295), 'SI_Walnut', .006)
    box('KitHandle', (.26, .02, .1), (.22, -.58 + Y, .405), 'SI_Walnut', .004)
    for k, x in enumerate((.12, .2)): lathe(f'Tin{k}', [(0, 0), (.032, 0), (.032, .025), (0, .025)], 'SI_Tin', 16, loc=(x, -.56 + Y, .3675))
    box('Brush', (.12, .04, .025), (.3, -.6 + Y, .3775), 'SI_Walnut', .005); box('Bristles', (.11, .035, .02), (.3, -.6 + Y, .358), 'SI_Bristle', .003)
    box('SignPost', (.035, .035, .62), (-.33, -.3 + Y, .84), 'SI_Walnut', .005)
    plate('ShinePlate', 'SI_PlateShine', .22, .17, (-.33, -.3205 + Y, 1.02))

def k_treeguard():
    # Wraps an existing street tree: no trunk here (the greenery kit owns it and its collision).
    box('Pit', (1.18, 1.18, .004), (0, 0, .002), 'SI_Soil', 0)
    for nm, sz, loc in [('N', (1.22, .05, .02), (0, .585, .016)), ('S', (1.22, .05, .02), (0, -.585, .016)), ('E', (.05, 1.12, .02), (.585, 0, .016)), ('W', (.05, 1.12, .02), (-.585, 0, .016))]:
        box('GrateFrame' + nm, sz, loc, 'SI_IronBlack', .003)
    for k, rr in enumerate([.26, .32, .38, .44, .5]): torus(f'GrateRing{k}', rr, .012, (0, 0, .011), 'SI_IronBlack', su=40, sv=4, scale=(1, 1, .7))
    for k in range(20):
        a = 2 * math.pi * k / 20; box(f'GrateRib{k}', (.3, .022, .018), (.38 * math.cos(a), .38 * math.sin(a), .015), 'SI_IronBlack', .002, rot=(0, 0, a))
    for sx, sy in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        for k in range(3): box(f'Corner{sx}{sy}{k}', (.022, .14 - k * .03, .018), (sx * (.5 + k * .025), sy * (.5 + k * .025), .0135), 'SI_IronBlack', .002, rot=(0, 0, math.pi / 4 * sx * sy))
    n, r0, r1, H = 20, .3, .26, 1.5
    for k in range(n):
        a = 2 * math.pi * k / n
        tube(f'Bar{k}', [(r0 * math.cos(a), r0 * math.sin(a), .0), (r1 * math.cos(a), r1 * math.sin(a), H)], .008, 'SI_IronBlack', 5)
        if k % 2 == 0: lathe(f'Finial{k}', [(0, H), (.016, H + .01), (.018, H + .03), (.008, H + .05), (0, H + .055)], 'SI_IronBlack', 8, loc=(r1 * math.cos(a), r1 * math.sin(a), 0))
    for z in (.08, .55, 1.15, 1.42): torus(f'GuardHoop{z}', r0 + (r1 - r0) * z / H, .01, (0, 0, z), 'SI_IronBlack', su=36, sv=4, scale=(1, 1, 1.4))
    for k in range(n):
        a = 2 * math.pi * (k + .5) / n; rr = r0 + (r1 - r0) * 1.28 / H; ca, sa = math.cos(a), math.sin(a)
        pts = [(rr * ca - sa * .032 * math.sin(t * 2 * math.pi) * (1 - abs(.5 - t)), rr * sa + ca * .032 * math.sin(t * 2 * math.pi) * (1 - abs(.5 - t)), 1.16 + .25 * t) for t in [i / 6 for i in range(7)]]
        tube(f'Scroll{k}', pts, .005, 'SI_IronBlack', 4)

BUILDERS = dict(hydrant=k_hydrant, mailbox=k_mailbox, alarm=k_alarm, trough=k_trough, hitch=k_hitch, column=k_column,
                litter=k_litter, sandbin=k_sandbin, shoeshine=k_shoeshine, treeguard=k_treeguard)
assert set(BUILDERS) == set(KINDS), (set(BUILDERS) ^ set(KINDS))

# ---------------------------------------------------------------- assemble, verify, export
COPLANAR = []
def _clip_area(subject, clip):
    """Overlap area of two convex 2D polygons (Sutherland-Hodgman)."""
    def area(poly): return abs(sum(poly[i][0] * poly[i - 1][1] - poly[i - 1][0] * poly[i][1] for i in range(len(poly)))) / 2
    def signed(poly): return sum(poly[i - 1][0] * poly[i][1] - poly[i][0] * poly[i - 1][1] for i in range(len(poly)))  # >0: counter-clockwise
    if signed(clip) < 0: clip = clip[::-1]
    out = subject
    for i in range(len(clip)):
        a, b = clip[i - 1], clip[i]; inp, out = out, []
        if not inp: break
        def inside(p): return (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0]) >= -1e-12
        def cross(p, q):
            x1, y1, x2, y2 = p[0], p[1], q[0], q[1]; dx, dy = b[0] - a[0], b[1] - a[1]
            den = (x2 - x1) * dy - (y2 - y1) * dx
            t = ((a[0] - x1) * dy - (a[1] - y1) * dx) / den if abs(den) > 1e-15 else 0
            return (x1 + t * (x2 - x1), y1 + t * (y2 - y1))
        for j in range(len(inp)):
            p, q = inp[j - 1], inp[j]
            if inside(q):
                if not inside(p): out.append(cross(p, q))
                out.append(q)
            elif inside(p): out.append(cross(p, q))
    return area(out) if len(out) > 2 else 0
def coplanar_audit(kind, objs):
    """Same-direction faces of different parts within 0.6 mm of one plane overlapping >= 1 cm2 (z-fight).
    Any material: each part has its own box-projected UVs, so even same-material overlaps flicker."""
    bpy.context.view_layer.update()
    faces = []
    for o in objs:
        mw = o.matrix_world
        for p in o.data.polygons:
            if p.area < 1e-6: continue
            n = (mw.to_3x3() @ p.normal).normalized(); vs = [mw @ o.data.vertices[i].co for i in p.vertices]
            faces.append((o.name, n, n.dot(vs[0]), vs))
    buckets = {}
    for f in faces: buckets.setdefault((round(f[1].x, 2), round(f[1].y, 2), round(f[1].z, 2), round(f[2] / .002)), []).append(f)
    seen = set()
    for key, group in buckets.items():
        near = group + [g for dk in (-1, 1) for g in buckets.get((*key[:3], key[3] + dk), [])]
        for a in group:
            u = a[1].orthogonal().normalized(); v = a[1].cross(u)
            pa = [(q.dot(u), q.dot(v)) for q in a[3]]
            for b in near:
                if a[0] >= b[0] or abs(a[2] - b[2]) > .0006 or a[1].dot(b[1]) < .999: continue
                if (kind, a[0], b[0]) in seen: continue
                if _clip_area(pa, [(q.dot(u), q.dot(v)) for q in b[3]]) >= 1e-4:
                    seen.add((kind, a[0], b[0])); COPLANAR.append((kind, a[0], b[0]))

manifest = dict(version=1, blender=bpy.app.version_string, kinds=[])
roots = []
for kind, build in BUILDERS.items():
    current.clear(); build()
    coplanar_audit(kind, current)
    for o in current: o.select_set(False)
    bpy.ops.object.select_all(action='DESELECT')
    for o in current: o.select_set(True)
    bpy.context.view_layer.objects.active = current[0]
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    bpy.ops.object.join()
    item = bpy.context.view_layer.objects.active; item.name = item.data.name = 'Item_' + kind
    me = item.data
    # Merge duplicate material slots created by the join, keeping every face's material.
    target = [me.materials[p.material_index].name for p in me.polygons]
    for i in reversed(range(len(me.materials))):
        if [m.name for m in me.materials].index(me.materials[i].name) != i: me.materials.pop(index=i)
    names = [m.name for m in me.materials]
    me.polygons.foreach_set('material_index', [names.index(n) for n in target]); me.update()
    assert [me.materials[p.material_index].name for p in me.polygons] == target, kind
    assert len(set(target)) == len(names), (kind, 'unused material slot')
    xs = [v.co.x for v in me.vertices]; ys = [v.co.y for v in me.vertices]; zs = [v.co.z for v in me.vertices]
    spec = KINDS[kind]; tol = .06
    ext = dict(x=[min(xs), max(xs)], y=[min(ys), max(ys)], z=[min(zs), max(zs)])
    if kind != 'treeguard':  # the flat grate is wider than the guard footprint by design
        assert max(abs(min(xs)), max(xs)) <= spec['w'] / 2 + tol, (kind, 'width', ext)
        assert max(abs(min(ys)), max(ys)) <= spec['d'] / 2 + tol, (kind, 'depth', ext)
    assert abs(max(zs) - spec['h']) <= .08 and min(zs) > -.002, (kind, 'height', ext, spec['h'])
    for marker, loc in (('Front_', (0, -1, 0)), ('Top_', (0, 0, 1))):
        e = bpy.data.objects.new(marker + kind, None); scene.collection.objects.link(e); e.parent = item; e.location = loc
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    manifest['kinds'].append(dict(kind=kind, object=item.name, triangles=tris, materials=names, extents={k: [round(a, 4), round(b, 4)] for k, (a, b) in ext.items()}, footprint=[spec['w'], spec['d']], height=spec['h']))
    roots.append(item)
manifest['triangles'] = sum(k['triangles'] for k in manifest['kinds'])
manifest['coplanarPairs'] = [list(p) for p in COPLANAR]
print('COPLANAR', len(COPLANAR), COPLANAR)
assert not COPLANAR, 'coplanar faces between kit parts: fix at the source, do not rely on ZFightRepair'

manifest['materials'] = [dict(name=n, rgb=[round(c, 4) for c in PALETTE[n]]) for n in sorted(MATS)]
out = (ROOT / args.output).resolve(); out.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='DESELECT')
for r in roots:
    r.select_set(True)
    for c in r.children: c.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(out / 'StreetItems.fbx'), use_selection=True, object_types={'MESH', 'EMPTY'},
                         axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP', mesh_smooth_type='FACE')
(out / 'street-items-manifest.json').write_text(json.dumps(manifest, indent=1) + '\n')
if args.unity:
    dest = (ROOT / args.unity).resolve(); dest.mkdir(parents=True, exist_ok=True)
    (dest / 'StreetItems.fbx').write_bytes((out / 'StreetItems.fbx').read_bytes())
    (ROOT / 'game/Assets/Resources/StreetItemsManifest.json').write_text(json.dumps(manifest, indent=1) + '\n')

if args.previews:
    # Lineup preview of the game kit (not a runtime capture).
    x = 0
    for r in roots:
        k = r.name[5:]; w = KINDS[k]['w'] if k != 'treeguard' else 1.25; x += w / 2; r.location.x = x; x += w / 2 + .3
    world = bpy.data.worlds.new('W'); scene.world = world; world.node_tree.nodes['Background'].inputs['Color'].default_value = (.5, .52, .55, 1)
    sun = bpy.data.lights.new('Sun', 'SUN'); sun.energy = 2.5; sun.angle = .4; so = bpy.data.objects.new('Sun', sun); scene.collection.objects.link(so); so.rotation_euler = (math.radians(50), 0, math.radians(-30))
    bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=30); g = bpy.data.meshes.new('G'); bm.to_mesh(g)
    go = bpy.data.objects.new('Ground', g); scene.collection.objects.link(go); go.location = (x / 2, 0, 0)
    gm = bpy.data.materials.new('G'); gm.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value = (.35, .35, .33, 1); g.materials.append(gm)
    cam = bpy.data.cameras.new('C'); cam.type = 'ORTHO'; cam.ortho_scale = x + 1; co = bpy.data.objects.new('C', cam); scene.collection.objects.link(co)
    co.location = (x / 2, -12, 1.8); co.rotation_euler = (math.radians(90), 0, 0); scene.camera = co
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 48; scene.render.resolution_x = 2400; scene.render.resolution_y = 720
    scene.view_settings.view_transform = 'AgX'; scene.render.image_settings.file_format = 'JPEG'; scene.render.image_settings.quality = 88
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences; prefs.compute_device_type = 'OPTIX'; prefs.get_devices()
        for d in prefs.devices: d.use = d.type != 'CPU'
        scene.cycles.device = 'GPU'
    except Exception: pass
    pv = (ROOT / args.previews).resolve(); pv.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(pv / 'kit-lineup.jpg'); bpy.ops.render.render(write_still=True)
    for r in roots: r.location.x = 0
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=str(out / 'StreetItems.blend'), compress=True)
print('STREET_ITEMS_KIT_PASS', json.dumps({k['kind']: k['triangles'] for k in manifest['kinds']}), 'total', manifest['triangles'])
