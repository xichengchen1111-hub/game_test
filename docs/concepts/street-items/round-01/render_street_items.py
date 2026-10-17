"""Concept renders for ten 1931 street items (round 01). Concept only, not a game asset.

blender.exe --background --factory-startup --python-exit-code 1 --python render_street_items.py -- <tex_dir> <out_dir> [samples] [only_id]

Builds every item procedurally on the same street vignette (the game's own concrete,
kerb and asphalt textures, overcast key light, cream shopfront) and renders one hero
view per item plus a same-scale lineup. Nothing is exported to assets/ or Unity.
"""
import bpy, bmesh, math, sys, random
from mathutils import Vector, Matrix, Euler
from pathlib import Path

argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
TEX = Path(argv[0]); OUT = Path(argv[1]); OUT.mkdir(parents=True, exist_ok=True)
SAMPLES = int(argv[2]) if len(argv) > 2 else 160
ONLY = argv[3] if len(argv) > 3 else None
REPO = Path(__file__).resolve().parents[4]
STREET = REPO / 'game/Assets/Art/Street'
FONT = 'C:/Windows/Fonts/georgiab.ttf'
rnd = random.Random(1931)

# ---------------------------------------------------------------- scene reset
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene

def link(obj, coll=None):
    (coll or scene.collection).objects.link(obj); return obj

def new_coll(name):
    c = bpy.data.collections.new(name); scene.collection.children.link(c); return c

# ---------------------------------------------------------------- materials
def _mat(name):
    m = bpy.data.materials.new(name)
    try: m.use_nodes = True
    except Exception: pass
    nt = m.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputMaterial'); bsdf = nt.nodes.new('ShaderNodeBsdfPrincipled')
    nt.links.new(bsdf.outputs['BSDF'], out.inputs['Surface'])
    return m, nt, bsdf

def _mix(nt, fac, a, b):
    n = nt.nodes.new('ShaderNodeMix'); n.data_type = 'RGBA'
    ins = {s.identifier: s for s in n.inputs}
    for key, val in (('A_Color', a), ('B_Color', b)):
        if isinstance(val, tuple): ins[key].default_value = (*val, 1)
        else: nt.links.new(val, ins[key])
    if isinstance(fac, float): ins['Factor_Float'].default_value = fac
    else: nt.links.new(fac, ins['Factor_Float'])
    return {s.identifier: s for s in n.outputs}['Result_Color']

def _noise(nt, scale, detail=6, rough=.55, coord='Object'):
    tc = nt.nodes.new('ShaderNodeTexCoord'); n = nt.nodes.new('ShaderNodeTexNoise')
    n.inputs['Scale'].default_value = scale; n.inputs['Detail'].default_value = detail; n.inputs['Roughness'].default_value = rough
    nt.links.new(tc.outputs[coord], n.inputs['Vector']); return n.outputs['Fac']

def _ramp(nt, src, a, b):
    r = nt.nodes.new('ShaderNodeValToRGB'); r.color_ramp.elements[0].position = a; r.color_ramp.elements[1].position = b
    nt.links.new(src, r.inputs['Fac']); return r.outputs['Color']

def _bump(nt, bsdf, src, strength):
    b = nt.nodes.new('ShaderNodeBump'); b.inputs['Strength'].default_value = strength
    nt.links.new(src, b.inputs['Height']); nt.links.new(b.outputs['Normal'], bsdf.inputs['Normal'])

def _grime(nt, color_socket, height=.45, amount=.45):
    """Darken toward the pavement: splash and soot band common on street iron."""
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ')
    nt.links.new(tc.outputs['Object'], sep.inputs['Vector'])
    mr = nt.nodes.new('ShaderNodeMapRange'); mr.inputs['From Min'].default_value = 0; mr.inputs['From Max'].default_value = height
    mr.inputs['To Min'].default_value = amount; mr.inputs['To Max'].default_value = 0
    nt.links.new(sep.outputs['Z'], mr.inputs['Value'])
    n = _noise(nt, 6, 4)
    mul = nt.nodes.new('ShaderNodeMath'); mul.operation = 'MULTIPLY'
    nt.links.new(mr.outputs['Result'], mul.inputs[0]); nt.links.new(n, mul.inputs[1])
    return _mix(nt, mul.outputs[0], color_socket, (.05, .045, .04))

MATS = {}
def paint(name, base, chip=(.06, .055, .05), chips=.62, rough=.5, metal_chip=.5, grime=True, bump=.12):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    n = _noise(nt, 48, 10, .66)
    mask = _ramp(nt, n, chips, chips + .025)
    col = _mix(nt, mask, base, chip)
    tone = _noise(nt, 2.5, 3)
    col = _mix(nt, _ramp(nt, tone, .3, .8), col, tuple(min(1, c * 1.18) for c in base)) if False else col
    if grime: col = _grime(nt, col)
    nt.links.new(col, bsdf.inputs['Base Color'])
    rr = nt.nodes.new('ShaderNodeMapRange'); rr.inputs['To Min'].default_value = rough; rr.inputs['To Max'].default_value = min(1, rough + .3)
    nt.links.new(mask, rr.inputs['Value']); nt.links.new(rr.outputs['Result'], bsdf.inputs['Roughness'])
    mm = nt.nodes.new('ShaderNodeMapRange'); mm.inputs['To Min'].default_value = 0; mm.inputs['To Max'].default_value = metal_chip
    nt.links.new(mask, mm.inputs['Value']); nt.links.new(mm.outputs['Result'], bsdf.inputs['Metallic'])
    _bump(nt, bsdf, _noise(nt, 60, 6), bump)
    MATS[name] = m; return m

def metal(name, base, rough=.35, tarnish=(.12, .1, .06)):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    col = _mix(nt, _ramp(nt, _noise(nt, 14, 8), .45, .7), base, tarnish)
    nt.links.new(col, bsdf.inputs['Base Color']); bsdf.inputs['Metallic'].default_value = 1
    bsdf.inputs['Roughness'].default_value = rough
    _bump(nt, bsdf, _noise(nt, 80, 4), .05)
    MATS[name] = m; return m

def stone(name, base, speck=.25, rough=.62, scale=90):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    tc = nt.nodes.new('ShaderNodeTexCoord'); v = nt.nodes.new('ShaderNodeTexVoronoi'); v.inputs['Scale'].default_value = scale
    nt.links.new(tc.outputs['Object'], v.inputs['Vector'])
    sp = _ramp(nt, v.outputs['Distance'], .02, .09)
    col = _mix(nt, sp, tuple(c * (1 - speck) for c in base), base)
    fl = _ramp(nt, _noise(nt, scale * 1.7, 2, .5), .62, .66)
    col = _mix(nt, fl, col, tuple(min(1, c * 1.5) for c in base))
    col = _mix(nt, _ramp(nt, _noise(nt, 3, 4), .35, .75), col, tuple(c * .78 for c in base))
    col = _grime(nt, col, .35, .35)
    nt.links.new(col, bsdf.inputs['Base Color']); bsdf.inputs['Roughness'].default_value = rough
    _bump(nt, bsdf, _noise(nt, 40, 8), .35)
    MATS[name] = m; return m

def wood(name, base, painted=None, rough=.6):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    tc = nt.nodes.new('ShaderNodeTexCoord'); mp = nt.nodes.new('ShaderNodeMapping'); mp.inputs['Scale'].default_value = (1, 1, 14)
    nt.links.new(tc.outputs['Object'], mp.inputs['Vector'])
    wv = nt.nodes.new('ShaderNodeTexWave'); wv.wave_type = 'RINGS'; wv.inputs['Scale'].default_value = 3.2
    wv.inputs['Distortion'].default_value = 6; wv.inputs['Detail'].default_value = 4
    nt.links.new(mp.outputs['Vector'], wv.inputs['Vector'])
    col = _mix(nt, _ramp(nt, wv.outputs['Fac'], .3, .9), tuple(c * .7 for c in base), base)
    if painted:
        mask = _ramp(nt, _noise(nt, 9, 8, .6), .58, .62)
        col = _mix(nt, mask, painted, col)
    col = _grime(nt, col, .35, .3)
    nt.links.new(col, bsdf.inputs['Base Color']); bsdf.inputs['Roughness'].default_value = rough
    _bump(nt, bsdf, wv.outputs['Fac'], .08)
    MATS[name] = m; return m

def plain(name, base, rough=.5, metallic=0., emit=None, emit_strength=0., transmission=0., alpha=1.):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    bsdf.inputs['Base Color'].default_value = (*base, 1); bsdf.inputs['Roughness'].default_value = rough
    bsdf.inputs['Metallic'].default_value = metallic
    if transmission: bsdf.inputs['Transmission Weight'].default_value = transmission; bsdf.inputs['IOR'].default_value = 1.45
    if emit: bsdf.inputs['Emission Color'].default_value = (*emit, 1); bsdf.inputs['Emission Strength'].default_value = emit_strength
    MATS[name] = m; return m

def image_mat(name, path, rough=.6, uv_scale=None, normal=None, normal_strength=.6, coat=0., tint=None, dark=0.):
    if name in MATS: return MATS[name]
    m, nt, bsdf = _mat(name)
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(str(path)); t.extension = 'REPEAT'
    col = t.outputs['Color']
    if tint: col = _mix(nt, tint[1], col, tint[0])
    if dark: col = _mix(nt, dark, col, (0., 0., 0.))
    nt.links.new(col, bsdf.inputs['Base Color']); bsdf.inputs['Roughness'].default_value = rough
    if coat: bsdf.inputs['Coat Weight'].default_value = coat
    if normal:
        tn = nt.nodes.new('ShaderNodeTexImage'); tn.image = bpy.data.images.load(str(normal)); tn.image.colorspace_settings.name = 'Non-Color'
        nm = nt.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = normal_strength
        nt.links.new(tn.outputs['Color'], nm.inputs['Color']); nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
    MATS[name] = m; return m

# ---------------------------------------------------------------- mesh helpers
def finish(bm, name, mat, coll, loc=(0, 0, 0), rot=(0, 0, 0), smooth=35, parent=None):
    me = bpy.data.meshes.new(name); bm.normal_update(); bm.to_mesh(me); bm.free()
    if smooth:
        for p in me.polygons: p.use_smooth = True
        try: me.set_sharp_from_angle(angle=math.radians(smooth))
        except Exception: pass
    if mat: me.materials.append(mat)
    o = bpy.data.objects.new(name, me); link(o, coll)
    o.location = loc; o.rotation_euler = rot
    if parent: o.parent = parent
    return o

def lathe(name, prof, mat, coll, seg=48, loc=(0, 0, 0), rot=(0, 0, 0), smooth=40, phase=0., parent=None, uv=False):
    bm = bmesh.new(); rings = []
    for r, z in prof:
        if r <= 1e-6: rings.append([bm.verts.new((0, 0, z))])
        else: rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / seg + phase), r * math.sin(2 * math.pi * i / seg + phase), z)) for i in range(seg)])
    for a, b in zip(rings, rings[1:]):
        if len(a) == 1 and len(b) == 1: continue
        for i in range(seg):
            j = (i + 1) % seg
            if len(a) == 1: bm.faces.new((a[0], b[i], b[j]))
            elif len(b) == 1: bm.faces.new((a[i], b[0], a[j]))
            else: bm.faces.new((a[i], b[i], b[j], a[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, coll, loc, rot, smooth, parent)

def box(name, size, loc, mat, coll, bevel=.006, seg=2, rot=(0, 0, 0), parent=None, smooth=30):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    if bevel > 0: bmesh.ops.bevel(bm, geom=bm.edges[:], offset=min(bevel, min(size) * .45), segments=seg, profile=.5, affect='EDGES')
    return finish(bm, name, mat, coll, loc, rot, smooth, parent)

def cyl(name, r, h, loc, mat, coll, seg=32, rot=(0, 0, 0), r2=None, bevel=0., parent=None, smooth=40):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=seg, radius1=r, radius2=r if r2 is None else r2, depth=h)
    if bevel > 0:
        rim = [e for e in bm.edges if len(e.link_faces) == 2 and abs(e.link_faces[0].normal.dot(e.link_faces[1].normal)) < .5]
        bmesh.ops.bevel(bm, geom=rim, offset=bevel, segments=2, profile=.5, affect='EDGES')
    return finish(bm, name, mat, coll, loc, rot, smooth, parent)

def prism(name, pts, depth, loc, mat, coll, axis='Y', bevel=.005, rot=(0, 0, 0), parent=None, smooth=30):
    """Extrude a 2D outline (u,v) by depth; axis Y means outline in XZ."""
    bm = bmesh.new()
    def P(u, v, w):
        return {'Y': (u, w, v), 'Z': (u, v, w), 'X': (w, u, v)}[axis]
    a = [bm.verts.new(P(u, v, -depth / 2)) for u, v in pts]; b = [bm.verts.new(P(u, v, depth / 2)) for u, v in pts]
    bm.faces.new(a); bm.faces.new(list(reversed(b)))
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n; bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    if bevel > 0: bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel, segments=2, profile=.5, affect='EDGES', clamp_overlap=True)
    return finish(bm, name, mat, coll, loc, rot, smooth, parent)

def torus(name, R, r, loc, mat, coll, rot=(0, 0, 0), su=24, sv=10, parent=None, scale=(1, 1, 1)):
    bm = bmesh.new(); grid = []
    for i in range(su):
        a = 2 * math.pi * i / su; row = []
        for j in range(sv):
            b = 2 * math.pi * j / sv; rr = R + r * math.cos(b)
            row.append(bm.verts.new((rr * math.cos(a) * scale[0], rr * math.sin(a) * scale[1], r * math.sin(b) * scale[2])))
        grid.append(row)
    for i in range(su):
        for j in range(sv):
            bm.faces.new((grid[i][j], grid[(i + 1) % su][j], grid[(i + 1) % su][(j + 1) % sv], grid[i][(j + 1) % sv]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return finish(bm, name, mat, coll, loc, rot, 60, parent)

def tube(name, pts, radius, mat, coll, res=8, parent=None, cyclic=False):
    cu = bpy.data.curves.new(name, 'CURVE'); cu.dimensions = '3D'; cu.bevel_depth = radius; cu.bevel_resolution = 3
    sp = cu.splines.new('POLY' if len(pts) < 3 else 'NURBS'); sp.points.add(len(pts) - 1)
    for p, c in zip(sp.points, pts): p.co = (*c, 1)
    if sp.type == 'NURBS': sp.order_u = min(4, len(pts)); sp.use_endpoint_u = not cyclic; sp.resolution_u = res
    sp.use_cyclic_u = cyclic
    cu.use_fill_caps = True
    o = bpy.data.objects.new(name, cu); o.data.materials.append(mat); link(o, coll)
    if parent: o.parent = parent
    return o

def text(name, body, size, loc, mat, coll, rot=(math.pi / 2, 0, 0), extrude=.003, parent=None, align='CENTER', font=FONT):
    cu = bpy.data.curves.new(name, 'FONT'); cu.body = body; cu.size = size; cu.extrude = extrude
    cu.font = bpy.data.fonts.load(font, check_existing=True); cu.align_x = align; cu.align_y = 'CENTER'
    cu.bevel_depth = extrude * .3 if extrude else 0
    o = bpy.data.objects.new(name, cu); o.data.materials.append(mat); link(o, coll)
    o.location = loc; o.rotation_euler = rot
    if parent: o.parent = parent
    return o

def plate(name, img, w, h, loc, coll, rot=(math.pi / 2, 0, 0), parent=None, thick=.004):
    m = image_mat('Enamel_' + name, TEX / img, rough=.22, coat=.4)
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1); bmesh.ops.scale(bm, vec=(w, h, thick), verts=bm.verts)
    uv = bm.loops.layers.uv.new()
    for f in bm.faces:
        for l in f.loops:
            x, y, _ = l.vert.co; l[uv].uv = (x / w + .5, y / h + .5)
    return finish(bm, name, m, coll, loc, rot, 0, parent)

def chain(name, a, b, sag, links, mat, coll, R=.011, r=.0028, parent=None):
    a, b = Vector(a), Vector(b)
    for k in range(links):
        t = (k + .5) / links
        p = a.lerp(b, t); p.z -= sag * 4 * t * (1 - t)
        q = a.lerp(b, t + .01); q.z -= sag * 4 * (t + .01) * (1 - t - .01)
        d = (q - p).normalized()
        rot = d.to_track_quat('X', 'Z').to_euler(); rot.rotate_axis('X', (math.pi / 2) * (k % 2))
        torus(f'{name}_{k}', R, r, p, mat, coll, rot=rot, su=14, sv=6, parent=parent, scale=(1.55, 1, 1))

# ---------------------------------------------------------------- common palette
IRON = paint('IronBlack', (.035, .036, .034), chip=(.12, .07, .04), chips=.66, rough=.45)
BRASS = metal('Brass', (.56, .42, .2), .32)
BRONZE = metal('Bronze', (.32, .22, .12), .45, (.1, .16, .12))
GALV = metal('Galvanised', (.42, .42, .4), .5, (.2, .2, .19))
GRANITE = stone('Granite', (.23, .23, .225), .55, .55, 70)
SANDSTONE = stone('Limestone', (.56, .53, .47), .15, .7, 60)
GLASS = plain('Glass', (.85, .9, .9), .02, transmission=1.)

# ---------------------------------------------------------------- street vignette
def vignette():
    c = new_coll('Vignette')
    def slab(name, x0, x1, y0, y1, z0, z1, mat, tile):
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1)
        bmesh.ops.scale(bm, vec=(x1 - x0, y1 - y0, z1 - z0), verts=bm.verts)
        bmesh.ops.translate(bm, vec=((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2), verts=bm.verts)
        uv = bm.loops.layers.uv.new()
        for f in bm.faces:
            n = f.normal
            for l in f.loops:
                x, y, z = l.vert.co
                l[uv].uv = (x / tile, y / tile) if abs(n.z) > .5 else ((x + y) / tile, z / tile)
        return finish(bm, name, mat, c, smooth=0)
    conc = image_mat('Concrete', STREET / 'PeriodConcrete_Base.png', .5, normal=STREET / 'PeriodConcrete_Normal.png', normal_strength=.8, dark=.38)
    kerb = image_mat('Kerb', STREET / 'KerbStone_Base.png', .6, normal=STREET / 'KerbStone_Normal.png', dark=.3)
    asph = image_mat('Asphalt', STREET / 'PeriodAsphalt_Base.png', .42, normal=STREET / 'PeriodAsphalt_Normal.png', normal_strength=.7)
    slab('Footway', -30, 30, -1.0, 4.2, -.2, 0, conc, 4)
    slab('Kerb', -30, 30, -1.32, -1.0, -.36, 0, kerb, 1)
    slab('Road', -30, 30, -16, -1.32, -.6, -.15, asph, 4)
    slab('FarKerb', -30, 30, -16.3, -16, -.6, 0, kerb, 1)
    # Cream plaster shopfront with dark green stall riser, 4.2 m behind the kerb edge.
    plaster = paint('Plaster', (.5, .47, .4), chip=(.4, .37, .31), chips=.72, rough=.85, metal_chip=0, bump=.3)
    green = paint('ShopGreen', (.07, .11, .085), chip=(.16, .12, .08), chips=.74, rough=.5, metal_chip=0)
    stone_base = stone('Plinth', (.34, .34, .33), scale=60)
    box('Facade', (60, .4, 7), (0, 4.4, 3.5), plaster, c, .0)
    box('Plinth', (60, .5, .35), (0, 4.25, .17), stone_base, c, .01)
    for x0 in range(-24, 25, 8):
        box(f'Riser_{x0}', (3.2, .12, .55), (x0, 4.15, .6), green, c, .01)
        box(f'Sill_{x0}', (3.4, .2, .06), (x0, 4.12, .9), green, c, .01)
        box(f'Glass_{x0}', (3.1, .05, 1.7), (x0, 4.2, 1.8), plain('DarkGlass', (.03, .035, .04), .08, metallic=.2), c, 0)
        for dx in (-1.6, 0, 1.6): box(f'Mullion_{x0}_{dx}', (.08, .1, 1.8), (x0 + dx, 4.14, 1.8), green, c, .01)
        box(f'Transom_{x0}', (3.4, .1, .1), (x0, 4.14, 2.7), green, c, .01)
        box(f'Fascia_{x0}', (3.6, .14, .5), (x0, 4.12, 3.1), green, c, .01)
        box(f'Door_{x0}', (1.1, .1, 2.3), (x0 + 3.6, 4.16, 1.15), green, c, .01)
    return c

# ---------------------------------------------------------------- the ten items
def item_hydrant(c):
    red = paint('HydrantRed', (.30, .075, .055), chip=(.07, .05, .04))
    silver = paint('HydrantSilver', (.34, .34, .32), chip=(.08, .06, .05), chips=.6, rough=.45, metal_chip=.6)
    prof = [(0, 0), (.17, 0), (.17, .028), (.155, .035), (.13, .04), (.118, .065), (.108, .085), (.102, .5), (.112, .512), (.142, .518), (.142, .556), (.1, .56)]
    lathe('Barrel', prof, red, c, 48)
    bon = [(.13, .556), (.132, .565), (.127, .6), (.11, .635), (.084, .662), (.058, .678), (.045, .684), (0, .686)]
    lathe('Bonnet', bon, silver, c, 48)
    cyl('Nut', .034, .045, (0, 0, .706), silver, c, seg=5, bevel=.003)
    cyl('NutWasher', .05, .008, (0, 0, .688), silver, c, seg=32)
    for k in range(8):
        a = 2 * math.pi * (k + .5) / 8
        cyl(f'FlangeBolt{k}', .011, .022, (.128 * math.cos(a), .128 * math.sin(a), .566), silver, c, seg=6)
        cyl(f'BaseBolt{k}', .012, .02, (.152 * math.cos(a), .152 * math.sin(a), .036), red, c, seg=6)
    # Two hose outlets at the sides, one large steamer outlet toward the road (-Y).
    for s in (-1, 1):
        cyl(f'Outlet{s}', .036, .08, (s * .13, 0, .40), red, c, rot=(0, math.pi / 2, 0), seg=24)
        cyl(f'OutletCap{s}', .046, .036, (s * .186, 0, .40), silver, c, rot=(0, math.pi / 2, 0), seg=6, bevel=.004)
        torus(f'CapLug{s}', .009, .003, (s * .205, 0, .38), silver, c, rot=(0, math.pi / 2, 0), su=12, sv=6)
        chain(f'Chain{s}', (s * .205, 0, .372), (s * .107, -.02, .47), .07, 7, silver, c)
    cyl('Steamer', .055, .07, (0, -.135, .33), red, c, rot=(math.pi / 2, 0, 0), seg=28)
    cyl('SteamerCap', .066, .045, (0, -.19, .33), silver, c, rot=(math.pi / 2, 0, 0), seg=6, bevel=.004)
    cyl('SteamerNut', .02, .02, (0, -.222, .33), silver, c, rot=(math.pi / 2, 0, 0), seg=5)
    chain('ChainS', (0, -.224, .305), (.06, -.09, .46), .09, 9, silver, c)
    for z in (.18, .26):
        torus(f'Band{z}', .104, .006, (0, 0, z), red, c, su=40, sv=6)
    return 'Hydrant', .75

def item_mailbox(c):
    olive = paint('PostOlive', (.13, .15, .085), chip=(.05, .045, .035), chips=.64, rough=.5)
    w, d = .46, .40
    pts = [(-w / 2, 0), (w / 2, 0), (w / 2, .62)] + [(w / 2 * math.cos(t), .62 + .2 * math.sin(t)) for t in [math.pi * k / 16 for k in range(1, 16)]] + [(-w / 2, .62)]
    prism('Body', pts, d, (0, 0, .46), olive, c, 'Y', .01)
    # Pull-down hopper chute on the front, slightly proud and tilted.
    box('Hopper', (.36, .05, .15), (0, -.215, 1.0), olive, c, .012, rot=(math.radians(-8), 0, 0))
    cyl('HopperHandle', .011, .3, (0, -.252, .925), BRASS, c, rot=(0, math.pi / 2, 0), seg=16)
    for s in (-1, 1): box(f'HandleArm{s}', (.016, .05, .016), (s * .15, -.232, .925), olive, c, .004)
    # Lower collection door with raised frame, lock and cast lettering.
    for x, z, sx, sz in [(0, .505, .38, .02), (0, .82, .38, .02), (-.18, .66, .02, .33), (.18, .66, .02, .33)]:
        box(f'DoorFrame{x}{z}', (sx, .02, sz), (x, -.207, z), olive, c, .004)
    text('MailText', 'MAIL', .085, (0, -.205, .705), olive, c, extrude=.004)
    cyl('Lock', .018, .012, (0, -.205, .58), BRASS, c, rot=(math.pi / 2, 0, 0), seg=20)
    box('Keyhole', (.006, .004, .016), (0, -.212, .578), plain('Dark', (.01, .01, .01)), c, 0)
    # Side relief panel with the same word and a star rosette: no plates for times.
    text('SideText', 'LETTERS', .05, (.232, 0, .72), olive, c, rot=(math.pi / 2, 0, math.pi / 2), extrude=.003)
    for s in (-1, 1):
        lathe(f'Rosette{s}', [(0, .004), (.03, .0), (.034, -.004)], olive, c, 12, loc=(0, -.204, 1.12 + 0 * s), rot=(math.pi / 2, 0, 0)) if s > 0 else None
    # Four splayed legs with brace and bolted feet.
    for sx in (-1, 1):
        for sy in (-1, 1):
            box(f'Leg{sx}{sy}', (.035, .035, .5), (sx * .2, sy * .17, .23), olive, c, .006, rot=(sy * math.radians(4), -sx * math.radians(5), 0))
            box(f'Foot{sx}{sy}', (.09, .09, .012), (sx * .222, sy * .188, .006), olive, c, .004)
            for bx in (-1, 1): cyl(f'FootBolt{sx}{sy}{bx}', .008, .012, (sx * .222 + bx * .028, sy * .188 + bx * .028, .016), IRON, c, seg=6)
    for sy in (-1, 1): box(f'Brace{sy}', (.42, .02, .03), (0, sy * .175, .2), olive, c, .004)
    box('Cap', (w + .02, d + .03, .02), (0, 0, .46), olive, c, .006)
    return 'Mailbox', 1.3

def item_alarm(c):
    red = paint('AlarmRed', (.33, .065, .045), chip=(.06, .05, .04))
    gold = metal('GoldLeaf', (.6, .46, .2), .3, (.3, .2, .08))
    lathe('Plinth', [(0, 0), (.2, 0), (.2, .05), (.17, .07), (.17, .11), (.13, .13), (.1, .17), (0, .17)], red, c, 8, phase=math.pi / 8)
    # Fluted shaft: 16 flutes as a star outline extruded upward.
    flutes = []
    for k in range(32):
        a = 2 * math.pi * k / 32; r = .068 if k % 2 == 0 else .058
        flutes.append((r * math.cos(a), r * math.sin(a)))
    prism('Shaft', flutes, 1.16, (0, 0, .75), red, c, 'Z', .004, smooth=70)
    lathe('Collar', [(0, 1.32), (.085, 1.32), (.095, 1.335), (.095, 1.355), (.115, 1.37), (.115, 1.39), (0, 1.39)], red, c, 32)
    torus('GoldRing', .086, .006, (0, 0, 1.33), gold, c, su=40, sv=6)
    # Signal box with arched hood.
    bw, bd, bh = .30, .2, .40
    pts = [(-bw / 2, 0), (bw / 2, 0), (bw / 2, bh)] + [(bw / 2 * math.cos(t), bh + .09 * math.sin(t)) for t in [math.pi * k / 12 for k in range(1, 12)]] + [(-bw / 2, bh)]
    prism('Box', pts, bd, (0, 0, 1.39), red, c, 'Y', .008)
    prism('Hood', [(x * 1.12, (y - bh) * 1.25 + bh + .005) for x, y in pts[2:]] + [(-bw * .56, bh - .01), (bw * .56, bh - .01)], bd + .05, (0, 0, 1.39), red, c, 'Y', .006)
    # Door with small window over the pull hook, lettering and hinge.
    box('Door', (.24, .02, .3), (0, -.108, 1.56), red, c, .008)
    box('WindowFrame', (.13, .015, .09), (0, -.12, 1.64), gold, c, .004)
    box('Window', (.11, .006, .07), (0, -.127, 1.64), GLASS, c, 0)
    box('HookPlate', (.09, .004, .06), (0, -.112, 1.64), plain('HookBack', (.6, .58, .52), .5), c, 0)
    box('Hook', (.012, .01, .04), (0, -.116, 1.636), BRASS, c, .002, rot=(0, 0, math.radians(-20)))
    plate('AlarmPlate', 'plate_alarm.png', .22, .08, (0, -.122, 1.5), c)
    for z in (1.46, 1.66): cyl(f'Hinge{z}', .01, .04, (.122, -.112, z), red, c, seg=12)
    cyl('Handle', .012, .03, (-.1, -.124, 1.56), BRASS, c, rot=(math.pi / 2, 0, 0), seg=16)
    # Red globe lamp: unlit in daytime, would share the photocell curve and light-pool budget.
    lathe('LampStem', [(0, 1.86), (.03, 1.86), (.02, 1.9), (.02, 1.95), (.045, 1.96), (.045, 1.975), (0, 1.975)], red, c, 24)
    globe = plain('RedGlobe', (.5, .08, .06), .12, transmission=.6, emit=(.9, .2, .12), emit_strength=.6)
    lathe('Globe', [(0, 1.975), (.06, 1.985), (.088, 2.03), (.085, 2.08), (.055, 2.115), (0, 2.12)], globe, c, 32)
    lathe('GlobeCap', [(0, 2.11), (.06, 2.112), (.05, 2.135), (.012, 2.15), (.012, 2.19), (0, 2.2)], red, c, 24)
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        tube(f'Cage{k}', [(.05 * math.cos(a), .05 * math.sin(a), 1.975), (.095 * math.cos(a), .095 * math.sin(a), 2.04), (.065 * math.cos(a), .065 * math.sin(a), 2.11)], .004, red, c)
    return 'FireAlarm', 2.2

def item_trough(c):
    # Granite stadium trough facing the road (-Y); bubbler and dog basins on the footway side.
    L, R = 1.0, .36
    def stadium(l, r, n=14):
        pts = [(l / 2 + r * math.cos(t), r * math.sin(t)) for t in [-math.pi / 2 + math.pi * k / n for k in range(n + 1)]]
        pts += [(-l / 2 + r * math.cos(t), r * math.sin(t)) for t in [math.pi / 2 + math.pi * k / n for k in range(n + 1)]]
        return pts
    bm = bmesh.new(); outer = [bm.verts.new((x, y, .06)) for x, y in stadium(L, R)]
    f = bm.faces.new(outer)
    ext = bmesh.ops.extrude_face_region(bm, geom=[f]); top = [e for e in ext['geom'] if isinstance(e, bmesh.types.BMFace)][0]
    bmesh.ops.translate(bm, vec=(0, 0, .58), verts=top.verts)
    ins = bmesh.ops.inset_individual(bm, faces=[top], thickness=.085, depth=0)
    ext2 = bmesh.ops.extrude_face_region(bm, geom=[top]); inner = [e for e in ext2['geom'] if isinstance(e, bmesh.types.BMFace)][0]
    bmesh.ops.translate(bm, vec=(0, 0, -.44), verts=inner.verts)
    bmesh.ops.scale(bm, vec=(.96, .9, 1), verts=inner.verts, space=Matrix.Translation((0, 0, -.2)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    rim_edges = [e for e in bm.edges if all(v.co.z > .6 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=rim_edges, offset=.018, segments=3, profile=.5, affect='EDGES', clamp_overlap=True)
    finish(bm, 'Trough', GRANITE, c, smooth=40)
    prism('TroughPlinth', stadium(L + .04, R + .05), .06, (0, 0, .03), stone('PlinthDark', (.34, .34, .33)), c, 'Z', .01)
    water = MATS.get('Water') or plain('Water', (.03, .04, .045), .03, transmission=.0)
    prism('Water', stadium(L * .96, R - .1), .004, (0, 0, .56), water, c, 'Z', 0)
    for k, (x, y) in enumerate([(-.3, -.08), (.25, .1)]):
        leaf = plain(f'Leaf{k}', (.28, .19, .09), .7)
        prism(f'Leaf{k}', [(0, 0), (.025, .012), (.05, 0), (.025, -.012)], .002, (x, y, .565), leaf, c, 'Z', 0, rot=(0, 0, k * 1.3))
    # Upstand stele at the back carrying the bubbler and an inscription (no date).
    box('Stele', (.7, .2, .66), (0, R + .02, .9), GRANITE, c, .02)
    lathe('SteleCap', [(0, 1.23), (.0, 1.23)], GRANITE, c) if False else None
    prism('SteleTop', [(-.38, 0), (.38, 0), (.3, .07), (0, .14), (-.3, .07)], .24, (0, R + .02, 1.23), GRANITE, c, 'Y', .01)
    box('Panel', (.5, .01, .26), (0, R - .085, .98), stone('PanelStone', (.38, .38, .37), .2, .7), c, .004)
    text('Inscription', 'WATER', .1, (0, R - .093, .98), plain('Carved', (.12, .12, .115), .9), c, extrude=.0015)
    # Bronze lion-mask spout feeding the basin.
    cyl('SpoutBoss', .06, .03, (0, R - .07, .76), BRONZE, c, rot=(math.pi / 2, 0, 0), seg=24, bevel=.006)
    tube('Spout', [(0, R - .08, .76), (0, R - .17, .76), (0, R - .23, .71)], .016, BRONZE, c)
    # Drinking bubbler for people on the footway side, and a dog basin at the foot.
    lathe('BubblerBowl', [(0, .94), (.03, .94), (.12, .99), (.14, 1.03), (.13, 1.035), (.11, 1.0), (0, .995)], BRONZE, c, 32, loc=(-.48, R + .2, 0))
    lathe('BubblerPost', [(0, .06), (.09, .06), (.08, .12), (.06, .14), (.055, .9), (.07, .94), (0, .94)], GRANITE, c, 24, loc=(-.48, R + .2, 0))
    cyl('Bubbler', .012, .05, (-.48, R + .2, 1.02), BRONZE, c, seg=12)
    lathe('DogBasin', [(0, .02), (.16, .02), (.18, .06), (.18, .12), (.15, .12), (.14, .05), (0, .05)], GRANITE, c, 32, loc=(.62, R + .22, 0))
    prism('DogWater', [(math.cos(t) * .14, math.sin(t) * .14) for t in [2 * math.pi * k / 24 for k in range(24)]], .003, (.62, R + .22, .1), water, c, 'Z', 0)
    return 'Trough', 1.4

def item_hitching(c):
    # Ball-and-ring cast-iron hitching post with a two-step granite carriage block.
    prof = [(0, 0), (.09, 0), (.09, .03), (.075, .04), (.07, .07), (.055, .1), (.05, .14), (.044, .84), (.052, .86), (.065, .875), (.065, .9), (.05, .91), (.045, .94),
            (.06, .96), (.068, .99), (.06, 1.02), (.04, 1.04), (.03, 1.06), (0, 1.065)]
    lathe('Post', prof, IRON, c, 32)
    for k in range(12):
        a = 2 * math.pi * k / 12
        box(f'Flute{k}', (.008, .008, .66), (.045 * math.cos(a), .045 * math.sin(a), .48), IRON, c, .003)
    lathe('Acorn', [(0, 1.06), (.022, 1.07), (.03, 1.1), (.022, 1.13), (0, 1.14)], IRON, c, 24)
    cyl('RingBoss', .02, .03, (0, -.06, .93), IRON, c, rot=(math.pi / 2, 0, 0), seg=16)
    torus('TieRing', .045, .007, (0, -.085, .885), BRASS, c, rot=(math.radians(80), 0, 0), su=32, sv=8)
    # Granite carriage block, two steps up from the gutter side.
    box('StepLow', (.75, .42, .2), (.75, -.05, .1), GRANITE, c, .025)
    box('StepHigh', (.75, .22, .2), (.75, .05, .3), GRANITE, c, .025)
    box('StepIron', (.6, .02, .01), (.75, -.25, .201), IRON, c, .003)
    return 'Hitching', 1.15

def item_column(c):
    dark = paint('ColumnGreen', (.045, .065, .055), chip=(.1, .07, .05), chips=.66, rough=.45)
    zinc = paint('ZincCap', (.3, .34, .32), chip=(.15, .17, .15), chips=.6, rough=.55, metal_chip=.7)
    lathe('Base', [(0, 0), (.64, 0), (.64, .06), (.6, .08), (.6, .16), (.56, .2), (.53, .24), (.51, .26), (0, .26)], dark, c, 64)
    # Poster shaft with wrapped UVs (u around, v up).
    bm = bmesh.new(); seg, H, r = 96, 2.2, .503
    ring0 = [bm.verts.new((r * math.cos(2 * math.pi * i / seg), r * math.sin(2 * math.pi * i / seg), .26)) for i in range(seg)]
    ring1 = [bm.verts.new((v.co.x, v.co.y, .26 + H)) for v in ring0]
    uv = bm.loops.layers.uv.new()
    for i in range(seg):
        j = (i + 1) % seg; f = bm.faces.new((ring0[i], ring0[j], ring1[j], ring1[i]))
        for l, (u, v) in zip(f.loops, [(i / seg, 0), ((i + 1) / seg, 0), ((i + 1) / seg, 1), (i / seg, 1)]): l[uv].uv = (u, v)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    finish(bm, 'Posters', image_mat('Posters', TEX / 'column_posters.png', .75, coat=.15), c, rot=(0, 0, math.radians(-170)), smooth=80)
    cyl('ShaftCore', .5, H, (0, 0, .26 + H / 2), dark, c, seg=64)
    lathe('Cornice', [(0, 2.44), (.52, 2.44), (.55, 2.47), (.55, 2.5), (.6, 2.54), (.64, 2.58), (.64, 2.62), (.56, 2.64), (.56, 2.7), (0, 2.7)], dark, c, 64)
    for k in range(32):
        a = 2 * math.pi * k / 32
        box(f'Dentil{k}', (.035, .03, .04), (.565 * math.cos(a), .565 * math.sin(a), 2.52), dark, c, .004, rot=(0, 0, a))
    lathe('Dome', [(.56, 2.7), (.58, 2.72), (.55, 2.8), (.47, 2.9), (.34, 2.98), (.18, 3.03), (.06, 3.05), (0, 3.05)], zinc, c, 64)
    for k in range(8):
        a = 2 * math.pi * k / 8
        tube(f'Rib{k}', [(.575 * math.cos(a), .575 * math.sin(a), 2.72), (.5 * math.cos(a), .5 * math.sin(a), 2.87), (.3 * math.cos(a), .3 * math.sin(a), 2.99), (.07 * math.cos(a), .07 * math.sin(a), 3.05)], .012, zinc, c)
    lathe('Finial', [(0, 3.04), (.06, 3.05), (.06, 3.08), (.035, 3.1), (.055, 3.15), (.04, 3.2), (.012, 3.24), (.006, 3.36), (0, 3.37)], dark, c, 24)
    return 'Column', 3.4

def item_litter(c):
    band = paint('LitterGreen', (.06, .085, .07), chip=(.1, .08, .06), chips=.62, rough=.45)
    wire = GALV
    R, H, z0, n = .23, .6, .2, 22
    for s in (-1, 1):  # crossed helical wires make the diamond basket weave
        for k in range(n):
            a0 = 2 * math.pi * k / n
            pts = [(R * (1 - .06 * (1 - t)) * math.cos(a0 + s * t * math.pi * .7), R * (1 - .06 * (1 - t)) * math.sin(a0 + s * t * math.pi * .7), z0 + H * t) for t in [i / 10 for i in range(11)]]
            tube(f'Wire{s}{k}', pts, .0032, wire, c)
    for z, rr in [(z0, R * .94), (z0 + H * .5, R * .97), (z0 + H, R)]:
        torus(f'Hoop{z}', rr, .007, (0, 0, z), wire, c, su=48, sv=8)
    lathe('Rim', [(R - .01, z0 + H - .02), (R + .012, z0 + H - .02), (R + .016, z0 + H + .01), (R - .004, z0 + H + .015)], band, c, 48)
    lathe('Bottom', [(0, z0 - .01), (R * .95, z0 - .01), (R * .95, z0 + .02), (0, z0 + .02)], band, c, 32)
    lathe('Band', [(R * .965, z0 + .3), (R * .99, z0 + .3), (R * .99, z0 + .42), (R * .965, z0 + .42)], band, c, 48)
    plate('LitterPlate', 'plate_litter.png', .2, .066, (0, -R * .99 - .006, z0 + .36), c)
    for k in range(3):
        a = 2 * math.pi * k / 3 + math.pi / 6
        tube(f'LegL{k}', [(R * .9 * math.cos(a), R * .9 * math.sin(a), z0 + .02), (R * 1.08 * math.cos(a), R * 1.08 * math.sin(a), .0)], .009, band, c)
        cyl(f'LegFoot{k}', .022, .01, (R * 1.08 * math.cos(a), R * 1.08 * math.sin(a), .005), band, c, seg=12)
    for k in range(7):  # a few dry winter leaves, no paper
        leaf = plain(f'DryLeaf{k % 3}', [(.3, .2, .1), (.22, .15, .08), (.35, .27, .14)][k % 3], .8)
        a, rr = rnd.uniform(0, 6.28), rnd.uniform(0, .17)
        prism(f'Leaf{k}', [(0, 0), (.03, .014), (.06, 0), (.03, -.014)], .002, (rr * math.cos(a), rr * math.sin(a), z0 + .03 + k * .012), leaf, c, 'Z', 0, rot=(rnd.uniform(-.4, .4), rnd.uniform(-.4, .4), a))
    return 'Litter', .85

def item_sandbin(c):
    painted = wood('BinWood', (.25, .2, .14), painted=(.23, .25, .2), rough=.75)
    W, D, Hf, Hb = 1.0, .56, .58, .74
    # Plank walls: horizontal boards with small gaps on front and sides.
    for i in range(4):
        z = .1 + i * .122
        box(f'FrontBoard{i}', (W, .03, .115), (0, -D / 2, z + .058), painted, c, .006)
        hb = min(.115, Hb - z) if i < 4 else 0
        box(f'BackBoard{i}', (W, .03, .115), (0, D / 2, z + .058), painted, c, .006)
    box('BackTop', (W, .03, .14), (0, D / 2, .66), painted, c, .006)
    for s in (-1, 1):
        prism(f'Side{s}', [(-D / 2, .1), (D / 2, .1), (D / 2, Hb), (-D / 2, Hf)], .03, (s * (W / 2 - .015), 0, 0), painted, c, 'X', .006)
    # Sloped lid with battens, iron straps, hinges and hasp.
    ang = math.atan2(Hb - Hf, D)
    lid = Euler((ang, 0, 0))
    box('Lid', (W + .06, D + .08, .035), (0, 0, (Hf + Hb) / 2 + .03), painted, c, .008, rot=lid)
    for x in (-.35, 0, .35): box(f'Batten{x}', (.06, D + .02, .02), (x, 0, (Hf + Hb) / 2 + .058), painted, c, .005, rot=lid)
    strap = paint('BinIron', (.07, .07, .065), chip=(.2, .1, .05), chips=.55, rough=.6)
    for x in (-.4, .4):
        box(f'Strap{x}', (.04, D + .085, .006), (x, 0, (Hf + Hb) / 2 + .05), strap, c, .002, rot=lid)
        cyl(f'Hinge{x}', .012, .09, (x, D / 2 + .03, Hb + .04), strap, c, rot=(0, math.pi / 2, 0), seg=12)
    box('Hasp', (.05, .008, .12), (0, -D / 2 - .03, Hf - .02), strap, c, .002)
    torus('Staple', .014, .004, (0, -D / 2 - .035, Hf - .08), strap, c, rot=(math.pi / 2, 0, 0), su=12, sv=6)
    for s in (-1, 1):
        for z in (.13, .55): box(f'Corner{s}{z}', (.06, .006, .06), (s * (W / 2 - .03), -D / 2 - .018, z), strap, c, .002)
    box('Runner0', (.08, D + .04, .1), (-.4, 0, .05), painted, c, .006); box('Runner1', (.08, D + .04, .1), (.4, 0, .05), painted, c, .006)
    stencil = plain('Stencil', (.62, .58, .48), .8)
    text('Stencil', 'SAND', .17, (0, -D / 2 - .016, .38), stencil, c, extrude=0)
    text('Stencil2', 'FOR ICY WALKS', .05, (0, -D / 2 - .016, .22), stencil, c, extrude=0)
    # Shovel leaning on the side, and a little spilled grit.
    handle = wood('ShovelHandle', (.42, .3, .18), rough=.55)
    sx = W / 2 + .1
    sx = W / 2 + .035
    tube('ShovelShaft', [(sx, -.16, .27), (sx, .06, .93)], .016, handle, c)
    torus('ShovelGrip', .045, .011, (sx, .075, .985), handle, c, rot=(0, math.pi / 2, 0), su=16, sv=6)
    blade = paint('ShovelBlade', (.18, .18, .17), chip=(.3, .15, .06), chips=.5, rough=.55, metal_chip=.7)
    prism('ShovelBlade', [(-.12, .3), (.12, .3), (.11, .06), (0, .0), (-.11, .06)], .004, (sx, -.2, .005), blade, c, 'X', .002, rot=(math.radians(-14), 0, 0))
    grit = stone('Grit', (.27, .24, .2), .3, .9, 300)
    for k in range(40):
        a, rr = rnd.uniform(0, 6.28), abs(rnd.gauss(0, .1))
        cyl(f'Grit{k}', rnd.uniform(.004, .009), .005, (rnd.uniform(-.3, .3), -D / 2 - .08 - rr, .003), grit, c, seg=6)
    return 'SandBin', .85

def item_shoeshine(c):
    walnut = wood('Walnut', (.2, .12, .07), rough=.45)
    green = wood('StandGreen', (.1, .1, .08), painted=(.11, .17, .13), rough=.5)
    leather = paint('Leather', (.12, .05, .035), chip=(.2, .12, .08), chips=.7, rough=.55, metal_chip=0, grime=False)
    W = .78
    # Raised platform with panelled front and brass edge.
    box('Platform', (W, .95, .5), (0, .1, .25), green, c, .012)
    for x in (-.2, .2): box(f'Panel{x}', (.3, .02, .32), (x, -.38, .26), walnut, c, .01)
    box('BrassEdge', (W + .01, .02, .02), (0, -.37, .5), BRASS, c, .004)
    box('Step', (W, .3, .2), (0, -.52, .1), green, c, .01)
    box('StepTread', (W + .02, .32, .025), (0, -.52, .21), walnut, c, .006)
    box('Top', (W + .03, .98, .03), (0, .1, .515), walnut, c, .008)
    # Chair: seat, slatted back, arms, leather cushion.
    box('Seat', (.62, .48, .05), (0, .2, .76), walnut, c, .01)
    box('Cushion', (.56, .42, .06), (0, .2, .81), leather, c, .025)
    for x in (-.29, .29):
        box(f'FrontLeg{x}', (.05, .05, .23), (x, -.02, .64), walnut, c, .008)
        box(f'BackPost{x}', (.05, .05, .78), (x, .42, .91), walnut, c, .008, rot=(math.radians(-6), 0, 0))
        box(f'Arm{x}', (.07, .5, .04), (x, .18, 1.0), walnut, c, .012)
        box(f'ArmPost{x}', (.04, .04, .2), (x, -.02, .89), walnut, c, .006)
    for k in range(5): box(f'Slat{k}', (.07, .025, .5), (-.2 + k * .1, .45, 1.06), walnut, c, .006, rot=(math.radians(-6), 0, 0))
    box('BackRail', (.62, .05, .08), (0, .47, 1.32), walnut, c, .01, rot=(math.radians(-6), 0, 0))
    box('BackCushion', (.5, .04, .3), (0, .41, 1.08), leather, c, .015, rot=(math.radians(-6), 0, 0))
    # Brass foot rests (shoe-shaped) on short posts at the front edge.
    for x in (-.14, .14):
        cyl(f'RestPost{x}', .015, .2, (x, -.32, .6), BRASS, c, seg=12)
        sole = [(-.04, -.13), (.04, -.13), (.05, -.02), (.035, .1), (0, .13), (-.035, .1), (-.05, -.02)]
        prism(f'Rest{x}', sole, .02, (x, -.32, .71), BRASS, c, 'Z', .006)
        box(f'Heel{x}', (.07, .03, .05), (x, -.43, .73), BRASS, c, .005)
    # Polish box and kit on the platform; price board on a post (5 cents, no hours).
    box('KitBox', (.36, .2, .14), (.26, -.6, .29), walnut, c, .008)
    box('KitHandle', (.3, .02, .1), (.26, -.6, .4), walnut, c, .005)
    for k, x in enumerate((.14, .22)): lathe(f'Tin{k}', [(0, 0), (.035, 0), (.035, .025), (0, .025)], metal('Tin', (.5, .48, .44), .4), c, 24, loc=(x, -.58, .37))
    bristle = plain('Bristle', (.08, .06, .05), .9)
    box('Brush', (.12, .04, .025), (.34, -.6, .375), walnut, c, .006); box('Bristles', (.11, .035, .02), (.34, -.6, .355), bristle, c, .003)
    box('SignPost', (.04, .04, 1.1), (-.46, -.5, .55), walnut, c, .006)
    plate('ShinePlate', 'plate_shine.png', .26, .2, (-.46, -.528, 1.0), c)
    return 'Shoeshine', 1.4

def item_treeguard(c):
    # Cast-iron tree grate (two halves, concentric slots) and a tapered guard with scroll band.
    pit = plain('Soil', (.06, .045, .035), .95)
    box('Pit', (1.2, 1.2, .02), (0, 0, -.009), pit, c, 0)
    box('GrateFrameN', (1.22, .05, .02), (0, .585, 0), IRON, c, .004); box('GrateFrameS', (1.22, .05, .02), (0, -.585, 0), IRON, c, .004)
    box('GrateFrameE', (.05, 1.22, .02), (.585, 0, 0), IRON, c, .004); box('GrateFrameW', (.05, 1.22, .02), (-.585, 0, 0), IRON, c, .004)
    box('GrateSplit', (1.2, .02, .02), (0, 0, 0), IRON, c, .003) if False else None
    for k, rr in enumerate([.2, .26, .32, .38, .44, .5]):
        torus(f'GrateRing{k}', rr, .012, (0, 0, .0), IRON, c, su=64, sv=6, scale=(1, 1, .7))
    for k in range(24):
        a = 2 * math.pi * k / 24
        box(f'GrateRib{k}', (.34, .022, .018), (.36 * math.cos(a), .36 * math.sin(a), 0), IRON, c, .003, rot=(0, 0, a))
    for (sx, sy) in [(1, 1), (1, -1), (-1, 1), (-1, -1)]:
        for k in range(3):
            box(f'Corner{sx}{sy}{k}', (.022, .14 - k * .03, .018), (sx * (.5 + k * .025), sy * (.5 + k * .025), 0), IRON, c, .003, rot=(0, 0, math.pi / 4 * sx * sy))
    bark = stone('Bark', (.09, .075, .06), .4, .95, 25)
    cyl('Trunk', .1, 3.2, (0, 0, 1.6), bark, c, seg=20, r2=.07)
    tube('Branch0', [(0, 0, 2.6), (.3, .1, 3.0), (.55, .15, 3.3)], .03, bark, c)
    tube('Branch1', [(0, 0, 2.9), (-.25, -.1, 3.2), (-.45, -.05, 3.45)], .025, bark, c)
    # Guard: 20 bars tapering, three hoops, top scroll band, ball finials on alternate bars.
    n, r0, r1, H = 20, .3, .26, 1.5
    for k in range(n):
        a = 2 * math.pi * k / n
        tube(f'Bar{k}', [(r0 * math.cos(a), r0 * math.sin(a), .0), (r1 * math.cos(a), r1 * math.sin(a), H)], .008, IRON, c)
        if k % 2 == 0: lathe(f'Finial{k}', [(0, H), (.016, H + .01), (.018, H + .03), (.008, H + .05), (0, H + .055)], IRON, c, 12, loc=(r1 * math.cos(a), r1 * math.sin(a), 0))
    for z in (.08, .55, 1.15, 1.42):
        rr = r0 + (r1 - r0) * z / H
        torus(f'GuardHoop{z}', rr, .01, (0, 0, z), IRON, c, su=48, sv=6, scale=(1, 1, 1.4))
    for k in range(n):  # S-scrolls between the two upper hoops
        a = 2 * math.pi * (k + .5) / n; rr = r0 + (r1 - r0) * 1.28 / H
        ca, sa = math.cos(a), math.sin(a); tx, ty = -sa, ca
        pts = []
        for i in range(13):
            t = i / 12; ph = t * 2 * math.pi
            off = .032 * math.sin(ph) * (1 - abs(.5 - t))
            pts.append((rr * ca + tx * off, rr * sa + ty * off, 1.16 + .25 * t))
        tube(f'Scroll{k}', pts, .005, IRON, c)
    return 'TreeGuard', 1.7

ITEMS = [
    ('01-hydrant', item_hydrant), ('02-mailbox', item_mailbox), ('03-fire-alarm', item_alarm), ('04-horse-trough', item_trough),
    ('05-hitching-post', item_hitching), ('06-poster-column', item_column), ('07-litter-basket', item_litter), ('08-sand-bin', item_sandbin),
    ('09-shoeshine-stand', item_shoeshine), ('10-tree-guard', item_treeguard),
]

# ---------------------------------------------------------------- render setup
def setup_render(w, h):
    scene.render.engine = 'CYCLES'
    try:
        prefs = bpy.context.preferences.addons['cycles'].preferences
        for t in ('OPTIX', 'CUDA'):
            try:
                prefs.compute_device_type = t; prefs.get_devices()
                if any(d.type == t for d in prefs.devices): break
            except Exception: continue
        for d in prefs.devices: d.use = d.type != 'CPU'
        scene.cycles.device = 'GPU'
    except Exception as e: print('GPU setup failed, using CPU', e)
    scene.cycles.samples = SAMPLES; scene.cycles.use_denoising = True
    scene.render.resolution_x = w; scene.render.resolution_y = h; scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = 'AgX'
    try: scene.view_settings.look = 'AgX - Medium Low Contrast'
    except Exception: pass
    scene.view_settings.exposure = -1.05
    scene.render.image_settings.file_format = 'PNG'

def lights():
    world = bpy.data.worlds.new('Overcast'); scene.world = world
    try: world.use_nodes = True
    except Exception: pass
    nt = world.node_tree; nt.nodes.clear()
    out = nt.nodes.new('ShaderNodeOutputWorld'); bg = nt.nodes.new('ShaderNodeBackground')
    # Vertical gradient: brighter cloud deck overhead, cool grey at the horizon.
    tc = nt.nodes.new('ShaderNodeTexCoord'); sep = nt.nodes.new('ShaderNodeSeparateXYZ'); ramp = nt.nodes.new('ShaderNodeValToRGB')
    nt.links.new(tc.outputs['Generated'], sep.inputs['Vector']); nt.links.new(sep.outputs['Z'], ramp.inputs['Fac'])
    ramp.color_ramp.elements[0].position = .45; ramp.color_ramp.elements[0].color = (.34, .36, .40, 1)
    ramp.color_ramp.elements[1].position = 1; ramp.color_ramp.elements[1].color = (.62, .64, .66, 1)
    nt.links.new(ramp.outputs['Color'], bg.inputs['Color']); bg.inputs['Strength'].default_value = 1.0
    nt.links.new(bg.outputs['Background'], out.inputs['Surface'])
    sun = bpy.data.lights.new('SkyKey', 'SUN'); sun.energy = 1.3; sun.angle = math.radians(22); sun.color = (1, .97, .93)
    o = bpy.data.objects.new('SkyKey', sun); link(o); o.rotation_euler = Euler((math.radians(52), 0, math.radians(-38)))
    # A faint warm bounce off-camera, standing in for the shop window and a lit lamp.
    a = bpy.data.lights.new('Warm', 'AREA'); a.energy = 60; a.size = 3; a.color = (1, .78, .5)
    o = bpy.data.objects.new('Warm', a); link(o); o.location = (4, 2.5, 3); o.rotation_euler = Euler((math.radians(70), 0, math.radians(120)))

def camera(target_h, focus=(0, 0), wide=False, name='Cam'):
    cam = bpy.data.cameras.new(name); cam.lens = 50 if not wide else 40
    cam.dof.use_dof = True; cam.dof.aperture_fstop = 2.8
    o = bpy.data.objects.new(name, cam); link(o); scene.camera = o
    tgt = Vector((focus[0], focus[1], target_h * .46))
    dist = max(1.5, target_h * 2.05 + .5) if target_h < 2 else target_h * 1.45 + .9
    ang = math.radians(-32)
    eye_h = min(1.55, target_h * .78 + .35)
    o.location = (tgt.x + dist * math.sin(ang), tgt.y - dist * math.cos(ang), eye_h)
    d = tgt - Vector(o.location); o.rotation_euler = d.to_track_quat('-Z', 'Y').to_euler()
    cam.dof.focus_distance = d.length
    return o

def render(path):
    scene.render.filepath = str(path); bpy.ops.render.render(write_still=True); print('RENDERED', path)

setup_render(1080, 1350)
lights(); vignette()
haze = bpy.data.materials.new('Haze'); nt = haze.node_tree; nt.nodes.clear()
vo = nt.nodes.new('ShaderNodeVolumePrincipled'); vo.inputs['Density'].default_value = .018; vo.inputs['Color'].default_value = (.62, .64, .67, 1); vo.inputs['Anisotropy'].default_value = .2
mo = nt.nodes.new('ShaderNodeOutputMaterial'); nt.links.new(vo.outputs['Volume'], mo.inputs['Volume'])
hz = box('HazeVolume', (80, 50, 14), (0, 0, 5), haze, scene.collection, 0); hz.visible_shadow = False
built = []
for key, fn in ITEMS:
    c = new_coll(key); name, h = fn(c); built.append((key, c, h))

def show_only(keep):
    for key, c, _ in built:
        c.hide_render = key not in keep
        for o in c.all_objects: o.hide_render = key not in keep

def place(key, x):
    for k, c, _ in built:
        if k == key:
            for o in c.all_objects:
                if o.parent is None: o.location.x += x

for key, c, h in built:
    if ONLY and ONLY not in key and ONLY != 'lineup': continue
    if ONLY == 'lineup': break
    show_only({key}); cam = camera(h)
    render(OUT / f'{key}.png')
    bpy.data.objects.remove(cam)

if not ONLY or ONLY == 'lineup':
    # Same-scale lineup along the facility band, 1.8 m scale pole with 0.5 m bands at the left.
    show_only({k for k, _, _ in built})
    hz.hide_render = True  # the ortho lineup is a scale sheet, no aerial haze
    widths = {'04-horse-trough': 2.4, '06-poster-column': 1.6, '09-shoeshine-stand': 1.3, '10-tree-guard': 1.4, '08-sand-bin': 1.5, '05-hitching-post': 1.6}
    x = 0; positions = {}
    for key, c, h in built:
        wv = widths.get(key, .9); x += wv / 2; place(key, x); positions[key] = x; x += wv / 2 + .25
    poleA = plain('PoleA', (.75, .73, .68), .6); poleB = plain('PoleB', (.08, .08, .08), .6)
    for k in range(4): box(f'Scale{k}', (.04, .04, .45), (-.5, 0, .225 + k * .45), poleA if k % 2 == 0 else poleB, scene.collection, 0)
    for k, c in enumerate(['Facade']):
        pass
    setup_render(3000, 880)
    cam = bpy.data.cameras.new('Lineup'); cam.type = 'ORTHO'; cam.ortho_scale = x + 1.4
    o = bpy.data.objects.new('Lineup', cam); link(o); scene.camera = o
    o.location = ((x - .5) / 2, -14, 1.85); o.rotation_euler = Euler((math.radians(90), 0, 0))
    import json
    (OUT / 'lineup-positions.json').write_text(json.dumps({'ortho_scale': cam.ortho_scale, 'center_x': o.location.x, 'center_z': o.location.z, 'width_px': 3000, 'height_px': 880, 'items': positions}, indent=1))
    render(OUT / 'lineup.png')
print('DONE')
