"""Build 1931 Railway Station B and Steam Train Assets for Unity.

Generates original, low-poly, historically faithful visual assets:
- StationB.blend / StationB.fbx
- TrainSet.blend / TrainSet.fbx
- rail-layout.json
- source-manifest.json
- station-front.png / train-threequarter.png previews

Usage:
    blender --background --python-exit-code 1 --python build_rail_assets.py -- --output .
or:
    python build_rail_assets.py --output .
"""

import argparse
import json
import math
import os
import subprocess
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parent))

# If run directly from standard Python, relaunch with Blender
if 'bpy' not in sys.modules:
    blender_candidates = [
        Path(r"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"),
        Path(r"C:\Program Files\Blender Foundation\Blender 5.0\blender.exe"),
        Path(r"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe"),
        Path("blender"),
    ]
    blender_exe = None
    for cand in blender_candidates:
        if cand.is_file() or cand.name == 'blender':
            blender_exe = cand
            break
    if not blender_exe:
        print("ERROR: Blender executable not found.", file=sys.stderr)
        sys.exit(1)

    cmd = [str(blender_exe), "--background", "--python-exit-code", "1", "--python", str(Path(__file__).resolve()), "--"] + sys.argv[1:]
    print("Launching Blender:", " ".join(cmd))
    sys.exit(subprocess.call(cmd))

# Running inside Blender's embedded Python
import bpy
import bmesh
from mathutils import Vector, Matrix, Euler, Quaternion


# ---------------------------------------------------------------------------
# Coordinate transformation & Utilities
# ---------------------------------------------------------------------------

def u(p):
    """Convert Unity coords (x east, y up, z north) to Blender coords.
    Helper u(p)=(-p[0], -p[2], p[1]) confirmed for FBX axis_forward=-Z, axis_up=Y.
    """
    return Vector((-float(p[0]), -float(p[2]), float(p[1])))


def u_dir(v):
    """Convert a direction vector from Unity to Blender."""
    return Vector((-float(v[0]), -float(v[2]), float(v[1]))).normalized()


# ---------------------------------------------------------------------------
# Materials
# ---------------------------------------------------------------------------

MATERIAL_DEFS = {
    'RailIron':   {'color': '2C3033', 'metallic': 0.75, 'roughness': 0.45},
    'RailGreen':  {'color': '1E3B2C', 'metallic': 0.10, 'roughness': 0.45},
    'RailIvory':  {'color': 'EAE2CE', 'metallic': 0.00, 'roughness': 0.60},
    'RailBrass':  {'color': 'C6A152', 'metallic': 0.85, 'roughness': 0.28},
    'RailOfficeWalnut': {'color': '5F4030', 'metallic': 0.00, 'roughness': 0.40},
    'RailWalnut': {'color': '5A3A22', 'metallic': 0.00, 'roughness': 0.52},
    'RailFelt':   {'color': '284D37', 'metallic': 0.00, 'roughness': 0.85},
    'RailGlass':  {'color': 'C2E0E5', 'metallic': 0.00, 'roughness': 0.04, 'transmission': 0.95, 'alpha': 0.35, 'ior': 1.45},
    'RailGlow':   {'color': 'FFD68A', 'metallic': 0.00, 'roughness': 0.30, 'emission': 3.5},
    'RailStone':  {'color': 'C4B69E', 'metallic': 0.00, 'roughness': 0.85},
    'RailSlate':  {'color': '3B434C', 'metallic': 0.00, 'roughness': 0.70},
    'RailRed':    {'color': '831E1E', 'metallic': 0.10, 'roughness': 0.50},
    'RailCoal':   {'color': '161719', 'metallic': 0.10, 'roughness': 0.90},
    'RailSkin':   {'color': 'B99075', 'metallic': 0, 'roughness': .85},
    'RailFloor':  {'color': 'DDD8C3', 'metallic': 0, 'roughness': .6},
    'RailLeather': {'color': '8C5C3D', 'metallic': 0, 'roughness': .65},
    'RailGranite': {'color': '77767A', 'metallic': 0, 'roughness': .78},
    'RailBrick':  {'color': '8E5644', 'metallic': 0, 'roughness': .85},
    'RailClay':   {'color': 'A0613F', 'metallic': 0, 'roughness': .75},
    'RailCeiling': {'color': 'E6DCC4', 'metallic': 0, 'roughness': .70},
    'RailZinc':   {'color': '5D6870', 'metallic': .35, 'roughness': .55},
    'RailShedGlass': {'color': 'A9BEBC', 'metallic': 0, 'roughness': .08, 'transmission': .85, 'alpha': .45, 'ior': 1.45},
    'RailEmber':  {'color': 'FF8A3D', 'metallic': 0, 'roughness': .5, 'emission': 4.0},
    'RailParcel': {'color': 'A8875E', 'metallic': 0, 'roughness': .9},
    'RailCarpet': {'color': '6E2B29', 'metallic': 0, 'roughness': .95},
    'RailCurtain': {'color': '6A2C2A', 'metallic': 0, 'roughness': .9},
    'RailRubber': {'color': '25272A', 'metallic': 0, 'roughness': .9},
    'RailCopper': {'color': 'B06A43', 'metallic': .8, 'roughness': .35},
}
WORLD_UV = ('RailStone', 'RailFloor', 'RailGranite', 'RailBrick', 'RailCeiling', 'RailZinc')


def hex_to_linear(hex_str):
    rgb = [int(hex_str[i:i+2], 16) / 255.0 for i in (0, 2, 4)]
    return [v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4 for v in rgb]


def setup_materials():
    mats = {}
    for name, spec in MATERIAL_DEFS.items():
        m = bpy.data.materials.new(name)
        m.use_nodes = True
        linear = hex_to_linear(spec['color'])
        m.diffuse_color = (*linear, spec.get('alpha', 1.0))
        bsdf = m.node_tree.nodes.get('Principled BSDF')
        if bsdf:
            bsdf.inputs['Base Color'].default_value = (*linear, 1.0)
            bsdf.inputs['Roughness'].default_value = spec.get('roughness', 0.5)
            bsdf.inputs['Metallic'].default_value = spec.get('metallic', 0.0)
            if 'transmission' in spec:
                bsdf.inputs['Transmission Weight'].default_value = spec['transmission']
                bsdf.inputs['Alpha'].default_value = spec['alpha']
                bsdf.inputs['IOR'].default_value = spec.get('ior', 1.45)
            if 'emission' in spec:
                bsdf.inputs['Emission Color'].default_value = (*linear, 1.0)
                bsdf.inputs['Emission Strength'].default_value = spec['emission']
        surface={'RailStone':'Limestone','RailFloor':'Floor','RailOfficeWalnut':'Walnut','RailWalnut':'Walnut','RailFelt':'Moquette',
                 'RailIron':'Iron','RailBrass':'Brass','RailSlate':'Slate','RailLeather':'Leather',
                 'RailGreen':'Paint','RailIvory':'Paint','RailRed':'Paint','RailGranite':'Granite','RailBrick':'Brick',
                 'RailCeiling':'Boards','RailZinc':'Zinc','RailCarpet':'Carpet','RailCurtain':'Velvet','RailRubber':'Bellows',
                 'RailParcel':'Paper'}.get(name)
        art=Path(__file__).resolve().parents[2]/'game/Assets/Art/Rail'
        if surface and (art/(surface+'Base.png')).exists():
            tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(art/(surface+'Base.png')));tex.image.pack()
            multiply=m.node_tree.nodes.new('ShaderNodeMixRGB');multiply.blend_type='MULTIPLY';multiply.inputs[0].default_value=1
            multiply.inputs[2].default_value=(*linear,1);m.node_tree.links.new(tex.outputs['Color'],multiply.inputs[1]);m.node_tree.links.new(multiply.outputs[0],bsdf.inputs['Base Color'])
            normal=m.node_tree.nodes.new('ShaderNodeTexImage');normal.image=bpy.data.images.load(str(art/(surface+'Normal.png')));normal.image.colorspace_settings.name='Non-Color';normal.image.pack()
            mapping=m.node_tree.nodes.new('ShaderNodeNormalMap');mapping.inputs['Strength'].default_value=.22
            m.node_tree.links.new(normal.outputs['Color'],mapping.inputs['Color']);m.node_tree.links.new(mapping.outputs['Normal'],bsdf.inputs['Normal'])
        mats[name] = m
    return mats


# ---------------------------------------------------------------------------
# Scene Context Class
# ---------------------------------------------------------------------------

class RailSceneBuilder:
    def __init__(self, collection_name):
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.context.preferences.filepaths.save_version = 0
        self.scene = bpy.context.scene
        self.scene.world = bpy.data.worlds.new('Rail studio world')
        self.scene.unit_settings.system = 'METRIC'
        self.collection = bpy.data.collections.new(collection_name)
        self.scene.collection.children.link(self.collection)
        self.materials = setup_materials()
        self.active_module = None
        self.meshes = []

    def set_module(self, name):
        # Re-entering a module reuses it; a second empty would become "<name>.001" and its
        # glowing parts would silently escape the light binding done by module name in Unity.
        obj = self.collection.objects.get(name)
        if obj is None or obj.type != 'EMPTY':
            obj = bpy.data.objects.new(name, None)
            self.collection.objects.link(obj)
        self.active_module = obj
        return obj

    def empty(self, name, pos=(0, 0, 0), parent=None):
        obj = bpy.data.objects.new(name, None)
        self.collection.objects.link(obj)
        obj.matrix_world = Matrix.Translation(u(pos))
        if parent:
            obj.parent = parent
        bpy.context.view_layer.update()
        return obj

    def finish_mesh(self, obj, name, material, bevel=0.0, parent=None, uv_scale=1.4):
        obj.name = name
        if obj.name not in self.collection.objects:
            self.collection.objects.link(obj)
        if bevel > 0.001:
            bm = bmesh.new()
            bm.from_mesh(obj.data)
            bmesh.ops.bevel(bm, geom=list(bm.edges), offset=bevel, segments=2, affect='EDGES', clamp_overlap=True)
            bm.to_mesh(obj.data)
            bm.free()
        obj.data.materials.append(self.materials[material])
        self.meshes.append(obj)

        # Explicit per-surface planar box-projected UVs
        uv = obj.data.uv_layers.new(name='SurfaceUV')
        coords = [v.co for v in obj.data.vertices]
        if coords:
            for face in obj.data.polygons:
                axis = max(range(3), key=lambda i: abs(face.normal[i]))
                axes = [i for i in range(3) if i != axis]
                for loop in face.loop_indices:
                    point = coords[obj.data.loops[loop].vertex_index]
                    if material in WORLD_UV:point=obj.matrix_world @ point
                    uv.data[loop].uv = tuple(point[i] * uv_scale for i in axes)

        world = obj.matrix_world.copy()
        obj.parent = parent or self.active_module
        obj.matrix_world = world
        return obj

    def box(self, name, pos, size, mat='RailWalnut', bevel=0.005, parent=None, uv_scale=1.4):
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co.x *= size[0]
            v.co.y *= size[2]
            v.co.z *= size[1]
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name, mesh)
        self.collection.objects.link(obj)
        obj.matrix_world = Matrix.Translation(u(pos))
        return self.finish_mesh(obj, name, mat, bevel, parent, uv_scale)

    def frame(self, name, pos, size, mat='RailGreen', bevel=.003, parent=None):
        # Four actual rails with a clear aperture; never an opaque rectangular window backing.
        x,y,z=pos; w,h,d=size; t=.07
        if d<w:
            for sign in (-1,1):
                self.box(name+'_Post'+str(sign),(x+sign*(w-t)/2,y,z),(t,h,d),mat,bevel,parent)
                self.box(name+'_Rail'+str(sign),(x,y+sign*(h-t)/2,z),(w-2*t,t,d),mat,bevel,parent)
        else:
            for sign in (-1,1):
                self.box(name+'_Post'+str(sign),(x,y,z+sign*(d-t)/2),(w,h,t),mat,bevel,parent)
                self.box(name+'_Rail'+str(sign),(x,y+sign*(h-t)/2,z),(w,t,d-2*t),mat,bevel,parent)

    def authored_mesh(self,name,points,faces,material,parent=None):
        mesh=bpy.data.meshes.new(name);mesh.from_pydata([u(p) for p in points],[],faces);mesh.update()
        bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(mesh);bm.free()
        obj=bpy.data.objects.new(name,mesh);self.collection.objects.link(obj)
        return self.finish_mesh(obj,name,material,parent=parent)

    def triangle_prism(self,name,points,extrude,material):
        v=[tuple(p) for p in points]+[tuple(Vector(p)+Vector(extrude)) for p in points]
        return self.authored_mesh(name,v,[(0,1,2),(5,4,3),(0,3,4,1),(1,4,5,2),(2,5,3,0)],material)

    def rose_spandrel(self,name,z):
        verts=[];faces=[];n=32
        for i in range(n):
            a=math.tau*i/n;dx,dy=math.cos(a),math.sin(a);r=min(1/max(abs(dx),.0001),.90/max(abs(dy),.0001))
            for zz in (z-.12,z+.12):
                verts.extend([(dx*.82,4.95+dy*.82,zz),(dx*r,4.95+dy*r,zz)])
        for i in range(n):
            j=(i+1)%n
            for a,b,c,d in ((0,1,1,0),(2,3,3,2),(0,2,2,0),(1,3,3,1)):
                faces.append((i*4+a,i*4+b,j*4+c,j*4+d))
        return self.authored_mesh(name,verts,faces,'RailStone')

    def coach_roof(self,cx,short):
        for name,base,mat in (('Outer',3.46,'RailSlate'),('Inner',3.39,'RailIvory')):
            vertices=[];faces=[];n=16
            for i in range(n):
                a=-math.pi/2+i*math.pi/n;b=a+math.pi/n
                z0,z1=1.48*math.sin(a),1.48*math.sin(b)
                y0,y1=base+.34*math.cos(a),base+.34*math.cos(b)
                k=len(vertices)
                vertices.extend([(cx-7.45,y0,z0),(cx+7.45,y0,z0),(cx+7.45,y1,z1),(cx-7.45,y1,z1),
                    (cx-7.45,y0-.025,z0),(cx+7.45,y0-.025,z0),(cx+7.45,y1-.025,z1),(cx-7.45,y1-.025,z1)])
                for f in ((0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)):
                    faces.append(tuple(k+j for j in f))
            self.authored_mesh(short+'_'+name+'ArchedRoof',vertices,faces,mat)

    def cylinder(self, name, pos, radius, depth, mat='RailIron', axis=(0, 1, 0), parent=None, vertices=16, bevel=0.002):
        bm = bmesh.new()
        bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=vertices, radius1=radius, radius2=radius, depth=depth)
        mesh = bpy.data.meshes.new(name)
        bm.to_mesh(mesh)
        bm.free()
        obj = bpy.data.objects.new(name, mesh)
        self.collection.objects.link(obj)
        direction = u_dir(axis)
        rot_matrix = direction.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        obj.matrix_world = Matrix.Translation(u(pos)) @ rot_matrix
        return self.finish_mesh(obj, name, mat, bevel, parent)

    def beam(self, name, a, b, radius=0.015, mat='RailIron', parent=None, vertices=12):
        a_vec, b_vec = Vector(a), Vector(b)
        center = (a_vec + b_vec) / 2.0
        diff = b_vec - a_vec
        length = diff.length
        if length < 1e-4:
            return None
        return self.cylinder(name, center, radius, length, mat, diff.normalized(), parent, vertices)

    def ring(self, name, pos, radius, thickness=0.015, mat='RailBrass', axis=(0, 0, 1), parent=None, segments=24, ring_segs=8):
        vertices = []
        faces = []
        for i in range(segments):
            a = i * math.tau / segments
            for j in range(ring_segs):
                b = j * math.tau / ring_segs
                r = radius + thickness * math.cos(b)
                vertices.append((r * math.cos(a), r * math.sin(a), thickness * math.sin(b)))
        for i in range(segments):
            next_i = (i + 1) % segments
            for j in range(ring_segs):
                next_j = (j + 1) % ring_segs
                faces.append((i * ring_segs + j, next_i * ring_segs + j, next_i * ring_segs + next_j, i * ring_segs + next_j))
        mesh = bpy.data.meshes.new(name)
        mesh.from_pydata(vertices, [], faces)
        mesh.update()
        obj = bpy.data.objects.new(name, mesh)
        self.collection.objects.link(obj)
        direction = u_dir(axis)
        rot_matrix = direction.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        obj.matrix_world = Matrix.Translation(u(pos)) @ rot_matrix
        return self.finish_mesh(obj, name, mat, 0.0, parent)

    def sign_text(self, name, text, pos, height=0.35, depth=0.03, mat='RailBrass', face_dir=(0, 0, -1), parent=None):
        bpy.ops.object.text_add()
        obj = bpy.context.active_object
        obj.name = name
        obj.data.body = text
        obj.data.size = height
        obj.data.extrude = depth / 2.0
        obj.data.resolution_u = 1
        bpy.ops.object.convert(target='MESH')
        bpy.ops.object.origin_set(type='ORIGIN_GEOMETRY', center='BOUNDS')

        # Orient text: default text is in Blender X-Y plane, extrude along Z.
        # In Unity coords, facing face_dir.
        dir_b = u_dir(face_dir)
        up_b = u_dir((0, 1, 0))
        rot_matrix = dir_b.to_track_quat('Z', 'Y').to_matrix().to_4x4()
        obj.matrix_world = Matrix.Translation(u(pos)) @ rot_matrix

        # Move to correct collection
        for c in list(obj.users_collection):
            c.objects.unlink(obj)
        self.collection.objects.link(obj)
        return self.finish_mesh(obj, name, mat, 0.0, parent)

    def join_static_modules_by_material(self):
        """Merges static meshes per module and material to minimize draw calls."""
        bpy.context.view_layer.update()
        for mod_empty in [o for o in list(self.collection.objects) if o.type == 'EMPTY' and o.name.startswith('Module_')]:
            members = [o for o in list(mod_empty.children) if o.type == 'MESH' and not o.get('ticket_support_fixture')]
            groups = {m_name: [o for o in members if o.data.materials and o.data.materials[0].name == m_name] for m_name in self.materials}
            for m_name, group in groups.items():
                if len(group) < 2:
                    continue
                bpy.ops.object.select_all(action='DESELECT')
                for o in group:
                    o.select_set(True)
                bpy.context.view_layer.objects.active = group[0]
                bpy.ops.object.join()
                group[0].name = f"{mod_empty.name}_{m_name}"
                bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)

    def export_fbx_and_blend(self, fbx_path, blend_path):
        bpy.ops.object.select_all(action='DESELECT')
        for obj in self.collection.objects:
            obj.select_set(True)
        # Choose active mesh
        active_mesh = next((o for o in self.collection.objects if o.type == 'MESH'), None)
        if active_mesh:
            bpy.context.view_layer.objects.active = active_mesh

        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
            use_selection=True,
            object_types={'MESH', 'EMPTY'},
            axis_forward='-Z',
            axis_up='Y',
            bake_anim=False,
            path_mode='STRIP'
        )

        self.scene.world = bpy.data.worlds.new('RailWorld')
        self.scene.world.use_nodes = True
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)


# ---------------------------------------------------------------------------
# Station B Construction
# ---------------------------------------------------------------------------

def build_station(out_dir):
    print(">>> Building StationB...")
    b = RailSceneBuilder('StationVisual')

    # Calibration & Inspection Markers
    b.empty('EastMarker', (3.0, 0.0, 0.0))
    b.empty('FrontMarker', (0.0, 0.0, -3.0))
    b.empty('FloorMarker', (0.0, 1.05, 0.0))

    # 1. Main Hall Shell & Foundation
    b.set_module('Module_Hall_Structure')
    # Floor: top at y=0.10, thickness 0.10, x [-15, 15], z [-6, 6]
    b.box('HallFloor', (0.0, 0.05, 0.0), (30.0, 0.10, 12.0), 'RailFloor', bevel=0.01)

    # South Facade Wall (z = -6.0) with centered door x[-1.7, 1.7] and 6 windows x = [-12, -8, -4, 4, 8, 12]
    # Window openings: x width 2.2, y [1.3, 4.1]
    # Door opening: x width 3.4, y [0.10, 3.50]
    z_front = -6.0
    wall_t = 0.24

    # Front Wall sections below windows (sills): y in [0.10, 1.30]
    # Left of door: x in [-15, -1.7]
    b.box('FrontSill_West', (-8.35, 0.70, z_front), (13.3, 1.20, wall_t), 'RailStone', bevel=0.005)
    # Right of door: x in [1.7, 15]
    b.box('FrontSill_East', (8.35, 0.70, z_front), (13.3, 1.20, wall_t), 'RailStone', bevel=0.005)

    # Front Wall piers between windows (y in [1.30, 4.10]):
    # Window centers: -12, -8, -4, 4, 8, 12. Width 2.2 => openings:
    # [-13.1, -10.9], [-9.1, -6.9], [-5.1, -2.9], [2.9, 5.1], [6.9, 9.1], [10.9, 13.1]
    front_piers = [
        (-14.05, 1.9),   # x in [-15.0, -13.1]
        (-10.0, 1.8),    # x in [-10.9, -9.1]
        (-6.0, 1.8),     # x in [-6.9, -5.1]
        (-2.3, 1.2),     # x in [-2.9, -1.7] (door jamb west)
        (2.3, 1.2),      # x in [1.7, 2.9] (door jamb east)
        (6.0, 1.8),      # x in [5.1, 6.9]
        (10.0, 1.8),     # x in [9.1, 10.9]
        (14.05, 1.9),    # x in [13.1, 15.0]
    ]
    for px, pw in front_piers:
        b.box(f'FrontPier_{px:.1f}', (px, 2.70, z_front), (pw, 2.80, wall_t), 'RailStone', bevel=0.008)
        # Former pilaster/capital boxes were buried in, or coplanar with, this face; the
        # beautification pass adds real projecting impost/sill/archivolt courses instead.

    # Above door lintel: x in [-1.7, 1.7], y in [3.50, 4.10]
    b.box('FrontDoorLintel', (0.0, 3.80, z_front), (3.4, 0.60, wall_t), 'RailStone', bevel=0.005)

    # Upper front wall fascia / cornice: y in [4.10, 6.00], x [-15, 15]
    for sign in (-1,1):
        b.box('FrontUpperPier'+str(sign),(sign*8,5.05,z_front),(14,1.9,wall_t),'RailStone')
    b.box('FrontRoseBottom',(0,4.12,z_front),(2,.04,wall_t),'RailStone')
    b.box('FrontRoseTop',(0,5.92,z_front),(2,.16,wall_t),'RailStone')
    b.box('FrontCorniceMain', (0.0, 6.05, z_front - 0.08), (30.4, 0.16, 0.38), 'RailStone', bevel=0.008)

    # Decorative stone pediment over front entrance: x in [-3.5, 3.5], y in [6.1, 7.8]
    b.box('PedimentBase', (0.0, 6.25, z_front - 0.06), (7.0, 0.24, 0.32), 'RailStone', bevel=0.008)
    # Triangular pediment tympanum
    b.triangle_prism('PedimentTympanum',[(-3.5,6.36,z_front-.2),(3.5,6.36,z_front-.2),(0,8,z_front-.2)],(0,0,.26),'RailStone')

    # Pediment Clock (with NO historical clue time): centered at (0, 7.0, z_front - 0.20)
    b.cylinder('PedimentClockBezel', (0.0, 7.0, z_front - 0.18), 0.62, 0.10, 'RailBrass', axis=(0, 0, -1), vertices=24)
    b.cylinder('PedimentClockDial', (0.0, 7.0, z_front - 0.23), 0.55, 0.02, 'RailIvory', axis=(0, 0, -1), vertices=24)
    b.empty('StationClockCentre',(0,7,z_front-.26))
    # Unity replaces these two reference hands with transforms driven by its shared ClockDial.

    # Decorative Rose Window above entry: centered at (0, 4.95, z_front)
    b.ring('FrontRoseRim', (0.0, 4.95, z_front), 0.85, 0.04, 'RailStone', axis=(0, 0, -1), segments=24)
    b.cylinder('FrontRoseGlass', (0.0, 4.95, z_front), 0.82, 0.015, 'RailGlass', axis=(0, 0, -1), vertices=24)
    for spk in range(8):
        ang = spk * math.pi / 4.0
        b.beam(f'FrontRoseSpoke_{spk}', (0.0, 4.95, z_front), (0.80 * math.cos(ang), 4.95 + 0.80 * math.sin(ang), z_front), 0.016, 'RailIron')

    # North Rear Facade Wall (z = +6.0) mirroring layout
    z_rear = 6.0
    b.box('RearSill_West', (-8.35, 0.70, z_rear), (13.3, 1.20, wall_t), 'RailStone', bevel=0.005)
    b.box('RearSill_East', (8.35, 0.70, z_rear), (13.3, 1.20, wall_t), 'RailStone', bevel=0.005)
    for px, pw in front_piers:
        b.box(f'RearPier_{px:.1f}', (px, 2.70, z_rear), (pw, 2.80, wall_t), 'RailStone', bevel=0.008)
    b.box('RearDoorLintel', (0.0, 3.80, z_rear), (3.4, 0.60, wall_t), 'RailStone', bevel=0.005)
    for sign in (-1,1):
        b.box('RearUpperPier'+str(sign),(sign*8,5.05,z_rear),(14,1.9,wall_t),'RailStone')
    b.box('RearRoseBottom',(0,4.12,z_rear),(2,.04,wall_t),'RailStone')
    b.box('RearRoseTop',(0,5.92,z_rear),(2,.16,wall_t),'RailStone')
    b.box('RearCorniceMain', (0.0, 6.05, z_rear + 0.08), (30.4, 0.16, 0.38), 'RailStone', bevel=0.008)
    b.ring('RearRoseRim', (0.0, 4.95, z_rear), 0.85, 0.04, 'RailStone', axis=(0, 0, 1), segments=24)
    b.cylinder('RearRoseGlass', (0.0, 4.95, z_rear), 0.82, 0.015, 'RailGlass', axis=(0, 0, 1), vertices=24)
    for spk in range(8):
        ang = spk * math.pi / 4.0
        b.beam(f'RearRoseSpoke_{spk}', (0.0, 4.95, z_rear), (0.80 * math.cos(ang), 4.95 + 0.80 * math.sin(ang), z_rear), 0.016, 'RailIron')

    # West (x = -15) and East (x = 15) Hall End Walls:
    # Open connection between hall and annex at z in [-1.4, 1.4], height 3.2 (y in [0.10, 3.30])
    for end_x in (-15.0, 15.0):
        name_prefix = 'West' if end_x < 0 else 'East'
        # Wall south of passage: z in [-6.0, -1.4] (length 4.6, center -3.7)
        b.box(f'{name_prefix}Wall_South', (end_x, 3.05, -3.7), (wall_t, 5.90, 4.6), 'RailStone', bevel=0.005)
        # Wall north of passage: z in [1.4, 6.0] (length 4.6, center 3.7)
        b.box(f'{name_prefix}Wall_North', (end_x, 3.05, 3.7), (wall_t, 5.90, 4.6), 'RailStone', bevel=0.005)
        # Arch lintel above connection passage: z in [-1.4, 1.4], y in [3.30, 6.00]
        b.box(f'{name_prefix}PassageLintel', (end_x, 4.65, 0.0), (wall_t, 2.70, 2.8), 'RailStone', bevel=0.005)
        # Triangular gable wall end: y in [6.0, 9.0], z in [-6.0, 6.0]
        b.triangle_prism(f'{name_prefix}GableWall',[(end_x-wall_t/2,6,-6),(end_x-wall_t/2,6,6),(end_x-wall_t/2,7.6,0)],(wall_t,0,0),'RailStone')

    # 2. Main Hall Slate Gabled Roof
    b.set_module('Module_Hall_Roof')
    # Slate roof pitches: ridge at y=9.0 along z=0, eaves at y=6.0, z = +-6.25
    # Two pitched roof planes: South (z from -6.25 to 0) and North (z from 0 to 6.25)
    # Slope length sqrt(6.25^2 + 3.0^2) = 6.93m
    for sign in (-1,1):
        pos=(0,6.8,sign*3.125)
        panel=b.box('HallRoof_'+str(sign),pos,(30.8,.16,6.46),'RailSlate')
        panel.matrix_world=Matrix.Translation(u(pos)) @ Matrix.Rotation(-sign*math.atan(1.6/6.25),4,u_dir((1,0,0)))
    # Ridge cap
    b.box('HallRoofRidgeCap', (0.0, 7.65, 0.0), (31.0, 0.14, 0.40), 'RailSlate', bevel=0.005)
    for sign in (-1,1):
        xx=sign*15
        b.triangle_prism('HallEndGable'+str(sign),[(xx,6,-6.2),(xx,6,6.2),(xx,7.6,0)],(-sign*.24,0,0),'RailStone')

    # Internal timber roof trusses across the hall (at x = -10, -5, 0, 5, 10). Rafters run
    # parallel to, and just under, the boarded ceiling lining of rail_station_beautify.
    from rail_station_beautify import lining
    for tx in (-10.0, -5.0, 0.0, 5.0, 10.0):
        # Tie beam at eaves level y = 6.0
        b.box(f'TrussTie_{tx:.0f}', (tx, 5.90, 0.0), (0.22, 0.24, 11.8), 'RailWalnut', bevel=0.005)
        # King post up to the ridge beam
        b.beam(f'TrussKing_{tx:.0f}', (tx, 5.90, 0.0), (tx, lining(0) - .14, 0.0), 0.10, 'RailWalnut')
        # Rafters
        b.beam(f'TrussRafterS_{tx:.0f}', (tx, lining(5.8) - .10, -5.8), (tx, lining(0) - .10, 0.0), 0.09, 'RailWalnut')
        b.beam(f'TrussRafterN_{tx:.0f}', (tx, lining(5.8) - .10, 5.8), (tx, lining(0) - .10, 0.0), 0.09, 'RailWalnut')

    # 3. Windows & Glazing (Front and Rear)
    b.set_module('Module_Hall_Glazing')
    window_centers = [-12.0, -8.0, -4.0, 4.0, 8.0, 12.0]
    for side, z_w, norm_z in (('Front', z_front, -1), ('Rear', z_rear, 1)):
        b.rose_spandrel(side+'RoseStoneReturn',z_w)
        for wx in window_centers:
            # Outer iron frame: width 2.2, height 2.8, depth 0.14
            b.frame(f'{side}WinFrame_{wx:.0f}', (wx, 2.70, z_w), (2.20, 2.80, 0.12), 'RailGreen', bevel=0.004)
            # Glazing pane: width 2.06, height 2.66
            b.box(f'{side}WinGlass_{wx:.0f}', (wx, 2.70, z_w), (2.06, 2.66, 0.015), 'RailGlass', bevel=0.0)
            # Vertical mullions
            b.beam(f'{side}WinMullion1_{wx:.0f}', (wx - 0.35, 1.35, z_w), (wx - 0.35, 4.05, z_w), 0.016, 'RailIron')
            b.beam(f'{side}WinMullion2_{wx:.0f}', (wx + 0.35, 1.35, z_w), (wx + 0.35, 4.05, z_w), 0.016, 'RailIron')
            # Transom bars
            b.beam(f'{side}WinTransom_{wx:.0f}', (wx - 1.0, 3.20, z_w), (wx + 1.0, 3.20, z_w), 0.016, 'RailIron')

    # 4. Side Annexes: West [-23, -15], East [15, 23], z [-4, 4], eaves y=4.0
    b.set_module('Module_Annexes')
    for annex_name, cx, x_wall_outer in (('WestAnnex', -19.0, -23.0), ('EastAnnex', 19.0, 23.0)):
        # Floor
        b.box(f'{annex_name}_Floor', (cx, 0.05, 0.0), (8.0, 0.10, 8.0), 'RailFloor', bevel=0.005)
        # End wall at x_wall_outer: length 8.0, height 4.0, with 2 side windows at z = -2.0, 2.0 (width 1.4, y [1.3, 3.2])
        b.box(f'{annex_name}_OuterSill', (x_wall_outer, 0.70, 0.0), (wall_t, 1.20, 8.0), 'RailStone', bevel=0.005)
        b.box(f'{annex_name}_OuterUpper', (x_wall_outer, 3.60, 0.0), (wall_t, 0.80, 8.0), 'RailStone', bevel=0.005)
        b.box(f'{annex_name}_OuterPierMid', (x_wall_outer, 2.25, 0.0), (wall_t, 1.90, 2.2), 'RailStone', bevel=0.005)
        b.box(f'{annex_name}_OuterPierS', (x_wall_outer, 2.25, -3.35), (wall_t, 1.90, 1.3), 'RailStone', bevel=0.005)
        b.box(f'{annex_name}_OuterPierN', (x_wall_outer, 2.25, 3.35), (wall_t, 1.90, 1.3), 'RailStone', bevel=0.005)
        # Windows on outer wall
        for zw in (-2.0, 2.0):
            b.frame(f'{annex_name}_WinFrame_{zw:.0f}', (x_wall_outer, 2.25, zw), (0.10, 1.90, 1.40), 'RailGreen', bevel=0.003)
            b.box(f'{annex_name}_WinGlass_{zw:.0f}', (x_wall_outer, 2.25, zw), (0.015, 1.80, 1.30), 'RailGlass', bevel=0.0)
            # The pier openings were 1.6m for a 1.4m window: close the 20cm daylight slot.
            b.box(f'{annex_name}_WinGapFill_{zw:.0f}', (x_wall_outer, 2.25, zw - math.copysign(.8, zw)), (wall_t, 1.90, .20), 'RailStone', bevel=0.003)
        # Outer gable triangle under the pitched annex roof (was open to the sky).
        b.triangle_prism(f'{annex_name}_OuterGable', [(x_wall_outer - wall_t / 2, 4.0, -4.0), (x_wall_outer - wall_t / 2, 4.0, 4.0), (x_wall_outer - wall_t / 2, 5.2, 0.0)], (wall_t, 0, 0), 'RailStone')

        # South (z = -4) and North (z = +4) Annex Walls
        for side, zw in (('S', -4.0), ('N', 4.0)):
            for sign in (-1,1):
                b.box(f'{annex_name}_Wall_{side}_{sign}',(cx+sign*2.35,2.05,zw),(3.3,3.9,wall_t),'RailStone')
            b.box(f'{annex_name}_Sill_{side}',(cx,.70,zw),(1.4,1.2,wall_t),'RailStone')
            b.box(f'{annex_name}_Header_{side}',(cx,3.60,zw),(1.4,.8,wall_t),'RailStone')
            # Center window on side wall at cx
            b.frame(f'{annex_name}_SideWinFrame_{side}', (cx, 2.25, zw), (1.40, 1.90, 0.10), 'RailGreen', bevel=0.003)
            b.box(f'{annex_name}_SideWinGlass_{side}', (cx, 2.25, zw), (1.30, 1.80, 0.015), 'RailGlass', bevel=0.0)

        # Annex Slate Roof: eaves 4.0, ridge y = 5.2 at z = 0
        # Roof and ridge stop at the hall's outer wall face (x=+-15.12); they used to pierce
        # the end wall and show as a dark chevron inside the waiting hall.
        for sign in (-1,1):
            pos=(cx+math.copysign(.16,cx),4.6,sign*2.1)
            panel=b.box(f'{annex_name}_Roof{sign}',pos,(8.08,.14,4.4),'RailSlate')
            panel.matrix_world=Matrix.Translation(u(pos)) @ Matrix.Rotation(-sign*math.atan(1.2/4.2),4,u_dir((1,0,0)))
        b.box(f'{annex_name}_RoofRidge', (cx+math.copysign(.185,cx), 5.25, 0.0), (8.13, 0.12, 0.35), 'RailSlate', bevel=0.004)

    # 5. Hall Interior Furnishings
    # Clear central aisle: x in [-2, 2], z in [-6, 6]
    b.set_module('Module_Hall_Interior')

    # Timetable & Paper Desk near local (+9, 0, 1):
    # Writing desk at x in [7.5, 10.5], z = 1.0, height y = 1.05
    b.box('PaperDesk', (9.0, 0.55, 1.0), (3.0, 0.90, 0.85), 'RailWalnut', bevel=0.008)
    b.box('PaperDeskTop', (9.0, 1.02, 1.0), (3.15, 0.05, 0.95), 'RailWalnut', bevel=0.005)
    b.box('PaperBlotter', (9.0, 1.05, 1.0), (1.40, 0.012, 0.60), 'RailFelt', bevel=0.002)
    b.cylinder('Inkstand', (8.0, 1.07, 1.15), 0.05, 0.04, 'RailBrass', axis=(0, 1, 0))
    # Wall Timetable notice board: North wall near (+9, 2.8, 5.8)
    # Luggage Scale near (+11.5, 0, -2.0)
    b.box('LuggageScalePlatform', (11.5, 0.18, -2.0), (1.20, 0.16, 1.20), 'RailIron', bevel=0.006)
    b.cylinder('LuggageScalePillar', (11.5, 1.0, -1.45), 0.06, 1.60, 'RailIron', axis=(0, 1, 0))
    b.ring('LuggageScaleDial', (11.5, 1.80, -1.45), 0.28, 0.03, 'RailBrass', axis=(0, 0, -1))
    b.cylinder('LuggageScaleFace', (11.5, 1.80, -1.45), 0.25, 0.01, 'RailIvory', axis=(0, 0, -1))

    # Passenger Waiting Benches in West and East wings (outside central aisle x[-2, 2]):
    # West bay: benches at z = -3.2 and z = -1.8, x in [-11, -5]
    for bx_start in (-10.5, 5.5):
        for bz in (-3.4, -1.8):
            # Bench seat cushion: height y = 0.52 (top y = 0.52 above floor y=0.10)
            b.box(f'BenchCushion_{bx_start:.1f}_{bz:.1f}', (bx_start + 2.5, 0.46, bz), (5.0, 0.10, 0.60), 'RailFelt', bevel=0.005)
            # Bench frame & legs
            b.box(f'BenchFrame_{bx_start:.1f}_{bz:.1f}', (bx_start + 2.5, 0.38, bz), (5.1, 0.06, 0.64), 'RailWalnut', bevel=0.004)
            # Turned wooden legs
            for leg_x in (bx_start + 0.3, bx_start + 2.5, bx_start + 4.7):
                for leg_z in (bz - 0.24, bz + 0.24):
                    b.cylinder(f'BenchLeg_{leg_x:.1f}_{leg_z:.1f}', (leg_x, 0.24, leg_z), 0.035, 0.28, 'RailWalnut', axis=(0, 1, 0))
            # Slatted backrest
            back_z = bz - 0.28 if bz < -2.5 else bz + 0.28
            b.box(f'BenchBack_{bx_start:.1f}_{bz:.1f}', (bx_start + 2.5, 0.82, back_z), (5.0, 0.65, 0.05), 'RailWalnut', bevel=0.004)

    # 6. Shed: Canopy x [-34, 34], z [9, 31], barrel axis X, arch in Z spanning 22m, ridge y 9.4, spring y 5.4
    # Columns ONLY at z = 9 and 31 at x = -30, -20, -10, 0, 10, 20, 30
    b.set_module('Module_TicketClerk')
    b.empty('TicketClerkMarker',(-8,0,1.9))
    for side in (-1,1):
        b.box('Clerk boot'+str(side),(-8+side*.13,.16,1.9),(.17,.12,.30),'RailIron')
        b.box('Clerk leg'+str(side),(-8+side*.13,.56,1.95),(.18,.68,.21),'RailSlate')
        b.box('Clerk sleeve'+str(side),(-8+side*.27,1.15,1.85),(.17,.50,.22),'RailGreen')
        b.cylinder('Clerk hand'+str(side),(-8+side*.27,.89,1.82),.065,.12,'RailSkin')
    b.box('Clerk uniform',(-8,1.10,1.95),(.42,.64,.28),'RailGreen')
    b.cylinder('Clerk neck',(-8,1.47,1.95),.08,.12,'RailSkin')
    b.cylinder('Clerk head',(-8,1.64,1.95),.13,.25,'RailSkin',vertices=10)
    b.cylinder('Clerk cap',(-8,1.79,1.95),.145,.07,'RailGreen',vertices=12)
    b.box('Clerk cap brim',(-8,1.76,1.84),(.25,.028,.18),'RailIron')
    for side in (-1,1):b.box('Clerk eye'+str(side),(-8+side*.045,1.66,1.82),(.018,.015,.008),'RailIron')
    b.box('Clerk nose',(-8,1.61,1.82),(.04,.05,.025),'RailSkin')
    b.set_module('Module_Shed_Structure')
    col_xs = [-30.0, -20.0, -10.0, 0.0, 10.0, 20.0, 30.0]
    for cx in col_xs:
        for cz in (9.0, 31.0):
            if cx==0 and cz==9:continue  # Preserve the existing rear-door access axis.
            # Cast-iron column: square plinth, moulded base, round shaft, bell capital and abacus.
            from rail_shapes import frustum
            b.box(f'ColPlinth_{cx:.0f}_{cz:.0f}', (cx, 0.20, cz), (0.50, 0.40, 0.50), 'RailIron', bevel=0.012)
            b.cylinder(f'ColBaseTorus_{cx:.0f}_{cz:.0f}', (cx, 0.475, cz), 0.25, 0.08, 'RailIron', axis=(0, 1, 0), vertices=20)
            frustum(b, f'ColBaseScotia_{cx:.0f}_{cz:.0f}', (cx, 0.515, cz), 0.22, 0.175, 0.08, 'RailIron', segments=20)
            b.cylinder(f'ColShaft_{cx:.0f}_{cz:.0f}', (cx, 2.745, cz), 0.17, 4.39, 'RailIron', axis=(0, 1, 0), vertices=20)
            for ry in (1.7, 4.72):
                b.ring(f'ColCollar_{cx:.0f}_{cz:.0f}_{ry}', (cx, ry, cz), 0.175, 0.016, 'RailIron', axis=(0, 1, 0), segments=20, ring_segs=6)
            frustum(b, f'ColBell_{cx:.0f}_{cz:.0f}', (cx, 4.94, cz), 0.18, 0.30, 0.30, 'RailIron', segments=20)
            b.box(f'ColCapital_{cx:.0f}_{cz:.0f}', (cx, 5.32, cz), (0.62, 0.16, 0.62), 'RailIron', bevel=0.012)
            # Cast iron corner bracket reaching towards roof
            bracket_dir_z = 1 if cz < 20 else -1
            b.beam(f'ColBracket_{cx:.0f}_{cz:.0f}', (cx, 4.4, cz), (cx, 5.4, cz + bracket_dir_z * 0.8), 0.045, 'RailIron')

    # Transverse Arched Iron Lattice Trusses at each column x plus gable ends x = -34, +34
    # Arch formula: span z in [9, 31], center z_c = 20, R = 17.125, y_c = -7.725
    # y(z) = y_c + sqrt(R^2 - (z - 20)^2). At z=9 & 31: y=5.4. At z=20: y=9.4.
    b.set_module('Module_Shed_Trusses')
    R_arch = 17.125
    y_c_arch = -7.725
    z_c_arch = 20.0
    truss_xs = [-34.0] + col_xs + [34.0]

    for tx in truss_xs:
        # Tie rod at spring line y = 5.40 connecting z=9 to z=31
        b.beam(f'TrussTieRod_{tx:.0f}', (tx, 5.40, 9.2), (tx, 5.40, 30.8), 0.030, 'RailIron')

        # Upper and lower chords sampled across arc
        num_arc_segs = 16
        pts_upper = []
        pts_lower = []
        for i in range(num_arc_segs + 1):
            t_frac = i / float(num_arc_segs)
            z_val = 9.0 + t_frac * 22.0
            dz = z_val - z_c_arch
            y_val = y_c_arch + math.sqrt(max(0.0, R_arch * R_arch - dz * dz))
            pts_upper.append((tx, y_val, z_val))
            # Lower chord offset 0.45m down
            pts_lower.append((tx, max(5.42, y_val - 0.45), z_val))

        for i in range(num_arc_segs):
            # Upper chord segment
            b.beam(f'ChordUp_{tx:.0f}_{i}', pts_upper[i], pts_upper[i+1], 0.045, 'RailIron')
            # Lower chord segment
            b.beam(f'ChordLo_{tx:.0f}_{i}', pts_lower[i], pts_lower[i+1], 0.040, 'RailIron')
            # Vertical web member
            b.beam(f'WebVert_{tx:.0f}_{i}', pts_lower[i], pts_upper[i], 0.025, 'RailIron')
            # Diagonal web member
            b.beam(f'WebDiag_{tx:.0f}_{i}', pts_lower[i], pts_upper[i+1], 0.022, 'RailIron')
        b.beam(f'WebVert_{tx:.0f}_end', pts_lower[-1], pts_upper[-1], 0.025, 'RailIron')

    # Longitudinal Purlins connecting trusses along X (from x=-34 to +34)
    purlin_zs = [9.1, 12.0, 15.0, 18.0, 20.0, 22.0, 25.0, 28.0, 30.9]
    for pz in purlin_zs:
        dz = pz - z_c_arch
        py = y_c_arch + math.sqrt(max(0.0, R_arch * R_arch - dz * dz))
        b.beam(f'Purlin_{pz:.1f}', (-34.0, py, pz), (34.0, py, pz), 0.050, 'RailIron')

    # Glazed Barrel Vault Panels: x [-34, 34], z [9, 31]
    b.set_module('Module_Shed_Roof')
    def vault_y(z):
        return y_c_arch + math.sqrt(max(0.0, R_arch * R_arch - (z - z_c_arch) ** 2))
    for i in range(num_arc_segs):
        z0 = 9.0 + 22.0 * i / num_arc_segs
        z1 = 9.0 + 22.0 * (i + 1) / num_arc_segs
        # Sloped panel on the chord of each arc segment (the old flat slabs stepped like stairs).
        # The two lowest bays each side are zinc sheeting; the rest is lightly frosted glass.
        mat = 'RailZinc' if i in (0, 1, num_arc_segs - 2, num_arc_segs - 1) else 'RailShedGlass'
        thick = .02 if mat == 'RailZinc' else .012
        pts = []
        for x in (-34.0, 34.0):
            for z in (z0, z1):
                pts += [(x, vault_y(z) + .055, z), (x, vault_y(z) + .055 + thick, z)]
        b.authored_mesh(f'VaultPanel_{i}', pts, [(0, 2, 6, 4), (1, 5, 7, 3), (0, 1, 3, 2), (4, 6, 7, 5), (0, 4, 5, 1), (2, 3, 7, 6)], mat)
        # Longitudinal iron glazing bar on every panel joint
        b.beam(f'GlazingRib_{i}', (-34.0, vault_y(z1) + .06, z1), (34.0, vault_y(z1) + .06, z1), 0.022, 'RailIron', vertices=8)

    # 7. Signage & Lanterns
    b.set_module('Module_Signs_Lighting')

    # Exterior Station Sign: QINGCHUAN on front facade pediment
    b.box('SignBacking_Qingchuan', (0.0, 6.25, z_front - 0.22), (4.5, 0.50, 0.05), 'RailWalnut', bevel=0.005)

    # Interior Sign: TICKETS above ticket window
    # Ticket header is part of the supported shared ticket-office kit.

    # Canopy Directional Sign: PLATFORM 1 suspended under truss at x=0, z=18, y=5.0
    b.box('SignBacking_Plat1', (0.0, 4.95, 18.0), (3.4, 0.45, 0.06), 'RailIron', bevel=0.005)
    b.beam('Plat1SignHangerL', (-1.4, 5.4, 18.0), (-1.4, 4.95, 18.0), 0.015, 'RailIron')
    b.beam('Plat1SignHangerR', (1.4, 5.4, 18.0), (1.4, 4.95, 18.0), 0.015, 'RailIron')

    b.set_module("Module_Hall_Lamps")
    # Hall Pendant Lanterns: 4 chandeliers
    hall_lamp_locs = [
        ('Lamp_Hall_01', (-8.0, 4.5, 0.0)),
        ('Lamp_Hall_02', (-3.0, 4.8, 0.0)),
        ('Lamp_Hall_03', (3.0, 4.8, 0.0)),
        ('Lamp_Hall_04', (8.0, 4.5, 0.0)),
    ]
    for lamp_name, lpos in hall_lamp_locs:
        b.empty(lamp_name, lpos)  # Pendant bodies hang from the ridge beam (rail_station_beautify).

    b.set_module("Module_Platform_Lamps")
    # Platform Hanging Lanterns: 6 brass lanterns under canopy along z = 20
    platform_lamp_locs = [
        ('Lamp_Platform_01', (-25.0, 5.8, 20.0)),
        ('Lamp_Platform_02', (-15.0, 5.8, 20.0)),
        ('Lamp_Platform_03', (-5.0, 5.8, 20.0)),
        ('Lamp_Platform_04', (5.0, 5.8, 20.0)),
        ('Lamp_Platform_05', (15.0, 5.8, 20.0)),
        ('Lamp_Platform_06', (25.0, 5.8, 20.0)),
    ]
    from rail_shapes import hanging_lantern
    for lamp_name, lpos in platform_lamp_locs:
        b.empty(lamp_name, lpos)
        # Hung from the ridge purlin (the old cable stopped 0.9m short of any structure).
        hanging_lantern(b, lamp_name, lpos, 9.35, size=.36, height=.50)

    from rail_detail_kit import add_station_details
    add_station_details(b)
    from rail_detail_refinements import add_station_refinements
    add_station_refinements(b)
    from ticket_office import add_ticket_office
    office = add_ticket_office(b)
    (out_dir / 'ticket-office-geometry.json').write_text(json.dumps(office, indent=2)+'\n', encoding='utf-8')
    from rail_station_beautify import add_station_beautify
    lanterns = add_station_beautify(b)
    print('FACADE_LANTERN_GLASS_CENTRES', {k: [round(v, 3) for v in p] for k, p in lanterns.items()})
    # Join static modules by material
    b.join_static_modules_by_material()

    # Render Preview: station-front.png
    cam_data = bpy.data.cameras.new('TempCamStation')
    cam_data.lens = 32
    cam_obj = bpy.data.objects.new('TempCamStation', cam_data)
    b.collection.objects.link(cam_obj)
    b.scene.camera = cam_obj
    # Looking at front facade from South
    cam_obj.location = u((0.0, 4.5, -28.0))
    # Point camera at facade center (0, 4.5, -6)
    dir_to_target = (u((0.0, 4.5, -6.0)) - cam_obj.location).normalized()
    cam_obj.matrix_world = Matrix.Translation(cam_obj.location) @ dir_to_target.to_track_quat('-Z', 'Y').to_matrix().to_4x4()

    preview_path = str(out_dir / 'station-front.png')
    b.scene.render.engine = 'BLENDER_WORKBENCH'
    b.scene.display.shading.light='STUDIO'
    b.scene.display.shading.color_type='MATERIAL'
    b.scene.display.shading.show_shadows=True
    b.scene.display.shading.show_cavity=True
    b.scene.display.shading.background_type='WORLD'
    b.scene.world.color=(.22,.24,.28)
    b.scene.render.resolution_x = 1280
    b.scene.render.resolution_y = 720
    b.scene.render.filepath = preview_path
    try:
        bpy.ops.render.render(write_still=True)
        print(f"Station preview rendered to {preview_path}")
    except Exception as e:
        print(f"EEVEE render failed ({e}), falling back to workbench...")
        b.scene.render.engine = 'BLENDER_WORKBENCH'
        b.scene.display.shading.light = 'STUDIO'
        b.scene.display.shading.color_type = 'MATERIAL'
        b.scene.display.shading.show_shadows = True
        bpy.ops.render.render(write_still=True)

    # Remove temporary camera
    bpy.data.objects.remove(cam_obj, do_unlink=True)
    bpy.data.cameras.remove(cam_data, do_unlink=True)

    # Export FBX & save Blend
    fbx_file = out_dir / 'StationB.fbx'
    blend_file = out_dir / 'StationB.blend'
    b.export_fbx_and_blend(fbx_file, blend_file)
    print(f"StationB saved to {blend_file} and {fbx_file}")
    return blend_file, fbx_file


# ---------------------------------------------------------------------------
# Train Set Construction
# ---------------------------------------------------------------------------

WHEEL_LIFT = .09  # wheel treads rest on the 0.095m rail heads laid by Unity's RailwayGround


def create_wheel(builder, name, pos, radius, width=0.14, is_driver=False, num_spokes=12, parent_empty=None):
    """Creates an independent spoked wheel mesh parented to parent_empty."""
    bm = bmesh.new()
    # Tire / Rim: cylinder along Y in Blender coords (axle along Unity Z)
    # Circle in Blender X-Z plane, thickness along Blender Y
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=24, radius1=radius, radius2=radius, depth=width)
    # Hollow center for spokes
    mesh = bpy.data.meshes.new(name + '_Mesh')
    bm.to_mesh(mesh)
    bm.free()

    # Build detailed spoked wheel with hub & spokes in python mesh data
    verts = []
    faces = []
    # Hub
    r_hub = radius * 0.22 if is_driver else radius * 0.28
    r_rim_in = radius * 0.82
    # Hub cylinder
    segs = 16
    for i in range(segs):
        ang = i * math.tau / segs
        ca, sa = math.cos(ang), math.sin(ang)
        verts.append((ca * r_hub, -width * 0.55, sa * r_hub))
        verts.append((ca * r_hub, width * 0.55, sa * r_hub))
    for i in range(segs):
        i2 = (i + 1) % segs
        faces.append((i * 2, i2 * 2, i2 * 2 + 1, i * 2 + 1))

    # Spokes
    base_idx = len(verts)
    spk_w = 0.020
    for s in range(num_spokes):
        ang = s * math.tau / num_spokes
        ca, sa = math.cos(ang), math.sin(ang)
        perp_x, perp_z = -sa * spk_w * 0.5, ca * spk_w * 0.5
        v_start = len(verts)
        verts.append((ca * r_hub + perp_x, -width * 0.2, sa * r_hub + perp_z))
        verts.append((ca * r_hub - perp_x, -width * 0.2, sa * r_hub - perp_z))
        verts.append((ca * r_rim_in - perp_x, -width * 0.2, sa * r_rim_in - perp_z))
        verts.append((ca * r_rim_in + perp_x, -width * 0.2, sa * r_rim_in + perp_z))
        faces.append((v_start, v_start + 1, v_start + 2, v_start + 3))

    # Rim tire ring
    base_idx = len(verts)
    for i in range(24):
        ang = i * math.tau / 24
        ca, sa = math.cos(ang), math.sin(ang)
        verts.append((ca * r_rim_in, -width * 0.5, sa * r_rim_in))
        verts.append((ca * r_rim_in, width * 0.5, sa * r_rim_in))
        verts.append((ca * radius, width * 0.5, sa * radius))
        verts.append((ca * radius, -width * 0.5, sa * radius))
    for i in range(24):
        i2 = (i + 1) % 24
        for f in range(4):
            f2 = (f + 1) % 4
            faces.append((base_idx + i * 4 + f, base_idx + i2 * 4 + f, base_idx + i2 * 4 + f2, base_idx + i * 4 + f2))

    # Driving wheel counterweight wedge
    if is_driver:
        cw_start = len(verts)
        for i in range(7):
            ang = math.pi * 0.6 + (i / 6.0) * math.pi * 0.8
            ca, sa = math.cos(ang), math.sin(ang)
            verts.append((ca * r_hub * 1.5, -width * 0.35, sa * r_hub * 1.5))
            verts.append((ca * r_rim_in * 0.95, -width * 0.35, sa * r_rim_in * 0.95))
        for i in range(6):
            faces.append((cw_start + i * 2, cw_start + (i + 1) * 2, cw_start + (i + 1) * 2 + 1, cw_start + i * 2 + 1))

    wheel_mesh = bpy.data.meshes.new(name + '_Mesh')
    wheel_mesh.from_pydata(verts, [], faces)
    wheel_mesh.update()
    obj = bpy.data.objects.new(name + '_Obj', wheel_mesh)
    builder.collection.objects.link(obj)

    # Attach to parent empty pivot
    obj.parent = parent_empty
    obj.matrix_local = Matrix.Identity(4)
    bpy.context.view_layer.update()  # Resolve child world transform before finish_mesh preserves it.
    builder.finish_mesh(obj, name + '_Obj', 'RailIron', bevel=0.0, parent=parent_empty)
    return obj


def build_train(out_dir):
    print(">>> Building TrainSet...")
    b = RailSceneBuilder('TrainVisual')

    # Calibration & Inspection Markers
    b.empty('EastMarker', (3.0, 0.0, 0.0))
    b.empty('FrontMarker', (0.0, 0.0, -3.0))
    b.empty('FloorMarker', (0.0, 1.05, 0.0))

    # -----------------------------------------------------------------------
    # 1. LOCOMOTIVE (2-6-0 Mogul): center x=20, length 10.5, front buffer at x=26
    # -----------------------------------------------------------------------
    b.set_module('Module_Loco_Static')

    # Locomotive Chassis Frame (x in [14.8, 25.8])
    b.box('LocoFrame', (20.3, 0.68, 0.0), (10.6, 0.40, 1.40), 'RailIron', bevel=0.008)

    # Buffer Beam Deep Red at front (x = 25.8)
    b.box('LocoFrontBufferBeam', (25.8, 0.85, 0.0), (0.24, 0.48, 2.40), 'RailRed', bevel=0.008)
    # Side spring buffers at z = +-0.85
    for bz in (-0.85, 0.85):
        b.cylinder(f'LocoBufferStem_{bz:.1f}', (26.05, 0.85, bz), 0.065, 0.35, 'RailIron', axis=(1, 0, 0))
        b.cylinder(f'LocoBufferPlate_{bz:.1f}', (26.22, 0.85, bz), 0.18, 0.03, 'RailIron', axis=(1, 0, 0))
    # Screw link coupler hook at center (26.0, 0.85, 0)
    b.ring('LocoCouplerHook', (26.05, 0.85, 0.0), 0.08, 0.022, 'RailIron', axis=(0, 1, 0))

    # Steam Cylinders at x = 22.5, y = 0.75, z = +-1.15
    for side_name, cz in (('L', 1.15), ('R', -1.15)):
        b.cylinder(f'LocoCylinder_{side_name}', (22.5, 0.75 + WHEEL_LIFT, cz), 0.35, 1.10, 'RailIron', axis=(1, 0, 0), vertices=20)
        b.box(f'LocoCrossheadGuide_{side_name}', (21.3, 0.75 + WHEEL_LIFT, cz), (1.30, 0.10, 0.08), 'RailIron', bevel=0.004)

    # Boiler: cylinder along X from x = 16.5 to x = 24.2, radius 0.78, center y = 1.95
    b.cylinder('LocoBoiler', (20.35, 1.95, 0.0), 0.78, 7.70, 'RailIron', axis=(1, 0, 0), vertices=24)
    # Smokebox at front (x in [22.4, 24.2])
    b.cylinder('LocoSmokebox', (23.3, 1.98, 0.0), 0.80, 1.80, 'RailIron', axis=(1, 0, 0), vertices=24)
    # Smokebox front dish door with central dart handle
    b.cylinder('LocoSmokeboxDoor', (24.22, 1.98, 0.0), 0.74, 0.08, 'RailIron', axis=(1, 0, 0), vertices=24)
    b.ring('LocoSmokeboxDart', (24.28, 1.98, 0.0), 0.12, 0.018, 'RailBrass', axis=(1, 0, 0))

    # Chimney / Smokestack at x = 23.0, top at y = 3.6
    b.cylinder('LocoChimney', (23.0, 3.15, 0.0), 0.24, 0.90, 'RailIron', axis=(0, 1, 0), vertices=20)
    b.ring('LocoChimneyLip', (23.0, 3.58, 0.0), 0.26, 0.025, 'RailIron', axis=(0, 1, 0))
    # Smokestack marker
    b.empty('SmokestackMarker', (23.0, 3.60, 0.0))

    # Steam Domes:
    # Main Steam Dome at x = 21.0, y = 2.95
    b.cylinder('LocoSteamDome', (21.0, 2.92, 0.0), 0.30, 0.45, 'RailBrass', axis=(0, 1, 0), vertices=18)
    b.cylinder('LocoSteamDomeCap', (21.0, 3.16, 0.0), 0.32, 0.08, 'RailBrass', axis=(0, 1, 0), vertices=18)
    # Sand Dome at x = 18.2, y = 2.92
    b.cylinder('LocoSandDome', (18.2, 2.90, 0.0), 0.26, 0.40, 'RailIron', axis=(0, 1, 0), vertices=18)

    # Brass / Copper piping along boiler flanks
    for pz in (-0.80, 0.80):
        b.beam(f'LocoBrassPipeTop_{pz:.1f}', (16.8, 2.45, pz), (23.8, 2.45, pz), 0.018, 'RailBrass')
        b.beam(f'LocoBrassPipeMid_{pz:.1f}', (17.0, 2.05, pz * 1.02), (22.5, 2.05, pz * 1.02), 0.014, 'RailBrass')

    # Front Headlamp at x = 24.8, y = 2.15: brass casing with warm glowing lens
    b.cylinder('LocoHeadlampCase', (24.75, 2.15, 0.0), 0.22, 0.32, 'RailBrass', axis=(1, 0, 0), vertices=18)
    b.cylinder('LocoHeadlampLens', (24.92, 2.15, 0.0), 0.19, 0.02, 'RailGlow', axis=(1, 0, 0), vertices=18)
    b.cylinder('LocoHeadlampBracket', (24.55, 1.65, 0.0), 0.04, 0.70, 'RailIron', axis=(0, 1, 0))  # stands on the front running plate

    # Steam Marker at (20, 0.8, -1.1)
    b.empty('SteamMarker', (20.0, 0.8, -1.1))

    # Locomotive Cab: x in [14.8, 16.8], width 2.6 (z in [-1.3, 1.3]), floor y=1.05, roof to y=3.55
    b.box('LocoCabFloor', (15.8, 1.02, 0.0), (2.0, 0.06, 2.50), 'RailIron', bevel=0.005)
    # Cab side walls
    for cz in (-1.30, 1.30):
        # Lower side wall
        b.box(f'LocoCabSideLo_{cz:.1f}', (15.8, 1.65, cz), (2.0, 1.20, 0.06), 'RailGreen', bevel=0.005)
        # Upper window post / cutout
        b.box(f'LocoCabSidePostF_{cz:.1f}', (16.6, 2.75, cz), (0.40, 1.00, 0.06), 'RailGreen', bevel=0.004)
        b.box(f'LocoCabSidePostR_{cz:.1f}', (15.0, 2.75, cz), (0.40, 1.00, 0.06), 'RailGreen', bevel=0.004)
        b.box(f'LocoCabSideGlass_{cz:.1f}', (15.8, 2.75, cz), (1.20, 0.90, 0.012), 'RailGlass', bevel=0.0)
    # Cab front spectacle plate
    b.box('LocoCabFrontPlate', (16.8, 2.25, 0.0), (0.06, 2.40, 2.60), 'RailGreen', bevel=0.005)
    # Spectacle oval windows on front plate
    for sz in (-0.85, 0.85):
        b.cylinder(f'LocoSpectacleWin_{sz:.1f}', (16.82, 2.75, sz), 0.22, 0.02, 'RailGlass', axis=(1, 0, 0), vertices=16)
    # Arched cab roof is authored by rail_train_beautify.

    # Cab Interior Backhead: firebox door, brass gauges, levers
    b.box('LocoFireboxBackhead', (16.7, 1.85, 0.0), (0.22, 1.50, 1.20), 'RailIron', bevel=0.006)
    b.ring('LocoFireboxDoor', (16.57, 1.50, 0.0), 0.25, 0.035, 'RailIron', axis=(1, 0, 0))
    for gz in (-0.35, 0.35):
        b.cylinder(f'LocoGauge_{gz:.1f}', (16.57, 2.30, gz), 0.09, 0.04, 'RailBrass', axis=(1, 0, 0))

    # Locomotive Wheels (Dynamic Empties with Wheel Meshes):
    # 2-6-0: Leading axle at x=23.6, radius 0.40; Driving axles at x=17.3, 19.1, 20.9, radius 0.73
    loco_wheels = [
        ('Wheel_L_Lead', (23.6, 0.40, 0.95), 0.40, False, 10),
        ('Wheel_R_Lead', (23.6, 0.40, -0.95), 0.40, False, 10),
        ('Wheel_L_Drive1', (20.9, 0.73, 0.95), 0.73, True, 16),
        ('Wheel_R_Drive1', (20.9, 0.73, -0.95), 0.73, True, 16),
        ('Wheel_L_Drive2', (19.1, 0.73, 0.95), 0.73, True, 16),
        ('Wheel_R_Drive2', (19.1, 0.73, -0.95), 0.73, True, 16),
        ('Wheel_L_Drive3', (17.3, 0.73, 0.95), 0.73, True, 16),
        ('Wheel_R_Drive3', (17.3, 0.73, -0.95), 0.73, True, 16),
    ]
    for w_name, w_pos, w_rad, is_drv, n_spk in loco_wheels:
        w_pos = (w_pos[0], w_pos[1] + WHEEL_LIFT, w_pos[2])
        w_empty = b.empty(w_name, w_pos)
        create_wheel(b, w_name, w_pos, w_rad, is_driver=is_drv, num_spokes=n_spk, parent_empty=w_empty)

    # Side Rod Pivots Rod_Left / Rod_Right near driving wheels (horizontal along X, x in [17.3, 20.9])
    # Neutral crankpin position at y = 0.73 - 0.38 = 0.35
    for rod_name, rz in (('Rod_Left', 1.05), ('Rod_Right', -1.05)):
        r_empty = b.empty(rod_name, (19.1, 0.73 + WHEEL_LIFT, rz))  # side rods centred on the driving axles
        # Side rod child mesh: horizontal bar connecting driving crankpins from x = 17.3 to x = 20.9
        # Local coords relative to (19.1, 0.40, rz): local X in [-1.8, 1.8]
        rod_bm = bmesh.new()
        bmesh.ops.create_cube(rod_bm, size=1.0)
        for v in rod_bm.verts:
            v.co.x *= 3.90   # length in local X (Unity X)
            v.co.y *= 0.04   # thickness in local Y (Unity Z)
            v.co.z *= 0.10   # height in local Z (Unity Y)
        rod_mesh = bpy.data.meshes.new(rod_name + '_Mesh')
        rod_bm.to_mesh(rod_mesh)
        rod_bm.free()
        rod_obj = bpy.data.objects.new(rod_name + '_Obj', rod_mesh)
        b.collection.objects.link(rod_obj)
        rod_obj.parent = r_empty
        rod_obj.matrix_local = Matrix.Identity(4)
        bpy.context.view_layer.update()
        b.finish_mesh(rod_obj, rod_name + '_Obj', 'RailIron', bevel=0.005, parent=r_empty)

    # -----------------------------------------------------------------------
    # 2. TENDER: center x=11.5, length 7, width 2.7 (x in [8.0, 15.0])
    # -----------------------------------------------------------------------
    b.set_module('Module_Tender_Static')
    # Tender Chassis
    b.box('TenderChassis', (11.5, 0.65, 0.0), (6.9, 0.35, 2.50), 'RailIron', bevel=0.008)
    # Rear buffer beam at x = 8.05
    b.box('TenderBufferBeam', (8.05, 0.85, 0.0), (0.22, 0.45, 2.40), 'RailRed', bevel=0.008)
    for bz in (-0.85, 0.85):
        b.cylinder(f'TenderBufferStem_{bz:.1f}', (7.85, 0.85, bz), 0.065, 0.35, 'RailIron', axis=(-1, 0, 0))
        b.cylinder(f'TenderBufferPlate_{bz:.1f}', (7.68, 0.85, bz), 0.18, 0.03, 'RailIron', axis=(-1, 0, 0))
    b.ring('TenderCouplerHook', (7.85, 0.85, 0.0), 0.08, 0.022, 'RailIron', axis=(0, 1, 0))

    # Tender Body: Heritage green steel sides, water tank rear, coal bunker front
    b.box('TenderBody', (11.5, 1.85, 0.0), (6.8, 1.95, 2.70), 'RailGreen', bevel=0.01)
    b.box('TenderWaterTankHatch', (9.2, 2.88, 0.0), (0.80, 0.14, 0.80), 'RailIron', bevel=0.008)
    # Sloped coal bunker heap: x in [11.2, 14.8], rough coal material
    b.box('TenderCoalHeap', (13.0, 2.35, 0.0), (3.4, 0.95, 2.45), 'RailCoal', bevel=0.02)

    # Tender Wheels (3 axles = 6 wheels at x = 9.3, 11.5, 13.7, radius 0.48)
    tender_axle_xs = [9.3, 11.5, 13.7]
    for idx, tx in enumerate(tender_axle_xs):
        for side, tz in (('L', 0.95), ('R', -0.95)):
            w_name = f'Wheel_Tender_{side}{idx+1}'
            w_empty = b.empty(w_name, (tx, 0.48 + WHEEL_LIFT, tz))
            create_wheel(b, w_name, (tx, 0.48 + WHEEL_LIFT, tz), 0.48, is_driver=False, num_spokes=10, parent_empty=w_empty)

    # -----------------------------------------------------------------------
    # 3. COACHES (Coach 01 center x=0, Coach 02 center x=-16)
    # Length 15, width 2.9, floor y=1.05, roof inner 3.5, outer 3.8, walls z=+-1.45
    # -----------------------------------------------------------------------
    coaches_def = [
        ('Coach1', 'C1', 0.0),
        ('Coach2', 'C2', -16.0),
    ]

    for c_long_name, c_short, c_cx in coaches_def:
        b.set_module(f'Module_{c_long_name}_Static')

        # Chassis & Underframe
        # Shallow underframe so the bogies, truss rods and brake gear show below the body.
        b.box(f'{c_short}_Chassis', (c_cx, 0.915, 0.0), (14.9, 0.13, 2.60), 'RailIron', bevel=0.01)
        # End Buffer Beams (at c_cx +- 7.45)
        for end_sign, ex in ((-1, c_cx - 7.45), (1, c_cx + 7.45)):
            b.box(f'{c_short}_BufferBeam_{end_sign}', (ex, 0.85, 0.0), (0.22, 0.45, 2.40), 'RailRed', bevel=0.008)
            for bz in (-0.85, 0.85):
                b.cylinder(f'{c_short}_BufferStem_{end_sign}_{bz:.1f}', (ex + end_sign * 0.20, 0.85, bz), 0.065, 0.35, 'RailIron', axis=(end_sign, 0, 0))
                b.cylinder(f'{c_short}_BufferPlate_{end_sign}_{bz:.1f}', (ex + end_sign * 0.38, 0.85, bz), 0.18, 0.03, 'RailIron', axis=(end_sign, 0, 0))
            b.ring(f'{c_short}_CouplerHook_{end_sign}', (ex + end_sign * 0.20, 0.85, 0.0), 0.08, 0.022, 'RailIron', axis=(0, 1, 0))

        # Floor: top exactly y = 1.05
        b.box(f'{c_short}_Floor', (c_cx, 1.00, 0.0), (14.8, 0.10, 2.80), 'RailWalnut', bevel=0.005)

        # Exterior Walls: at z = +-1.45
        # Bottom panel: y in [1.05, 1.80], deep green
        # Top fascia / letterboard: y in [3.20, 3.55], ivory / green
        # Window posts only in gaps between windows
        # Window centers within each coach: c_cx + [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5], width 1.45, y [1.8, 3.2]
        # Door openings at local x = +-5.9 center: width 1.05 along X, y in [1.05, 3.35]
        # Door openings span: [5.375, 6.425] and [-6.425, -5.375] relative to coach center
        for side_name, wz, norm_z in (('S', -1.45, -1), ('N', 1.45, 1)):
            # Bottom panel (y in [1.05, 1.80]):
            # Needs cutouts for doors! Door openings y in [1.05, 3.35]
            # So bottom panel exists only where there are NO doors:
            # End west: [c_cx - 7.4, c_cx - 6.425] (length 0.975)
            # Mid passenger bay: [c_cx - 5.375, c_cx + 5.375] (length 10.75)
            # End east: [c_cx + 6.425, c_cx + 7.4] (length 0.975)
            b.box(f'{c_short}_{side_name}_Bottom_Mid', (c_cx, 1.425, wz), (10.75, 0.75, 0.08), 'RailGreen', bevel=0.005)
            b.box(f'{c_short}_{side_name}_Bottom_W', (c_cx - 6.91, 1.425, wz), (0.98, 0.75, 0.08), 'RailGreen', bevel=0.005)
            b.box(f'{c_short}_{side_name}_Bottom_E', (c_cx + 6.91, 1.425, wz), (0.98, 0.75, 0.08), 'RailGreen', bevel=0.005)

            # Top fascia (y in [3.20, 3.55]): above windows & doors across entire coach
            b.box(f'{c_short}_{side_name}_TopFascia', (c_cx, 3.375, wz), (14.8, 0.35, 0.08), 'RailIvory', bevel=0.005)
            b.box(f'{c_short}_{side_name}_TopFasciaTrim', (c_cx, 3.56, wz), (14.9, 0.04, 0.10), 'RailGreen', bevel=0.002)

            # Window Posts (y in [1.80, 3.20]):
            # Window centers at c_cx + [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5] with width 1.45
            # Openings are: [cx-5.225, cx-3.775], [cx-3.425, cx-1.975], [cx-1.625, cx-0.175], [cx+0.175, cx+1.625], [cx+1.975, cx+3.425], [cx+3.775, cx+5.225]
            # Posts between windows:
            post_centers_x = [
                c_cx - 3.60,  # post 1-2: width 0.35
                c_cx - 1.80,  # post 2-3: width 0.35
                c_cx,         # post 3-4: width 0.35
                c_cx + 1.80,  # post 4-5: width 0.35
                c_cx + 3.60,  # post 5-6: width 0.35
            ]
            for p_idx, px in enumerate(post_centers_x):
                b.box(f'{c_short}_{side_name}_Post_{p_idx}', (px, 2.50, wz), (0.35, 1.40, 0.08), 'RailIvory', bevel=0.003)

            # Vestibule wall posts around doors (y in [1.80, 3.20]):
            # Between window 1 and rear door: [c_cx - 5.375, c_cx - 5.225] (width 0.15)
            b.box(f'{c_short}_{side_name}_Post_W_Door', (c_cx - 5.30, 2.50, wz), (0.15, 1.40, 0.08), 'RailIvory', bevel=0.003)
            # Rear corner wall: [c_cx - 7.4, c_cx - 6.425] (width 0.975)
            b.box(f'{c_short}_{side_name}_Wall_RearCorner', (c_cx - 6.91, 2.50, wz), (0.98, 1.40, 0.08), 'RailIvory', bevel=0.003)
            # Between window 6 and front door: [c_cx + 5.225, c_cx + 5.375] (width 0.15)
            b.box(f'{c_short}_{side_name}_Post_E_Door', (c_cx + 5.30, 2.50, wz), (0.15, 1.40, 0.08), 'RailIvory', bevel=0.003)
            # Front corner wall: [c_cx + 6.425, c_cx + 7.4] (width 0.975)
            b.box(f'{c_short}_{side_name}_Wall_FrontCorner', (c_cx + 6.91, 2.50, wz), (0.98, 1.40, 0.08), 'RailIvory', bevel=0.003)

            # Windows in openings:
            win_offsets = [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5]
            for w_idx, w_off in enumerate(win_offsets):
                wx = c_cx + w_off
                # Walnut window frame inside aperture
                b.frame(f'{c_short}_{side_name}_WinFrame_{w_idx}', (wx, 2.50, wz), (1.45, 1.40, 0.06), 'RailWalnut', bevel=0.003)
                # Clear Glass Pane
                b.box(f'{c_short}_{side_name}_WinGlass_{w_idx}', (wx, 2.50, wz), (1.35, 1.30, 0.015), 'RailGlass', bevel=0.0)

        # End Walls & Vestibules:
        # Vestibules at x in [c_cx + 5.2, c_cx + 7.3] and [c_cx - 7.3, c_cx - 5.2]
        # Internal passenger opening width 0.8 along Z at x = c_cx +- 5.2 (opening z in [-0.40, 0.40], y [1.05, 3.25])
        # Bulkhead walls at x = c_cx +- 5.2:
        for b_sign, bx in ((-1, c_cx - 5.2), (1, c_cx + 5.2)):
            # Bulkhead south side: z in [-1.45, -0.40] (width 1.05)
            b.box(f'{c_short}_BulkheadS_{b_sign}', (bx, 2.25, -0.925), (0.06, 2.40, 1.05), 'RailWalnut', bevel=0.004)
            # Bulkhead north side: z in [0.40, 1.45] (width 1.05)
            b.box(f'{c_short}_BulkheadN_{b_sign}', (bx, 2.25, 0.925), (0.06, 2.40, 1.05), 'RailWalnut', bevel=0.004)
            # Arch top above passage opening: z in [-0.40, 0.40], y in [3.25, 3.50]
            b.box(f'{c_short}_BulkheadTop_{b_sign}', (bx, 3.375, 0.0), (0.06, 0.25, 0.80), 'RailWalnut', bevel=0.003)

        # Coach Outer End Walls at c_cx +- 7.3 with gangway opening width 0.8, height 2.2 (y in [1.05, 3.25])
        for e_sign, ex in ((-1, c_cx - 7.35), (1, c_cx + 7.35)):
            b.box(f'{c_short}_EndWallS_{e_sign}', (ex, 2.25, -0.925), (0.08, 2.40, 1.05), 'RailGreen', bevel=0.005)
            b.box(f'{c_short}_EndWallN_{e_sign}', (ex, 2.25, 0.925), (0.08, 2.40, 1.05), 'RailGreen', bevel=0.005)
            b.box(f'{c_short}_EndWallTop_{e_sign}', (ex, 3.375, 0.0), (0.08, 0.25, 0.80), 'RailGreen', bevel=0.003)

        # Arched Roof: inner y = 3.5, outer y = 3.8
        # Outer roof slate/canvas
        b.coach_roof(c_cx,c_short)
        # Inner ceiling ivory boarding

        # Interior Seating & Layout:
        # Central aisle: z in [-0.40, 0.40] completely clear
        # Seat benches ONLY |z| > 0.45, paired facing longitudinally
        # Cushion top y = 1.52 (0.47 above floor), back tops y = 2.30
        seat_bays = [-4.5, -2.7, -0.9, 0.9, 2.7]
        for bay_idx, bx_off in enumerate(seat_bays):
            bx = c_cx + bx_off
            # Paired benches: one facing +X at bx - 0.45, one facing -X at bx + 0.45
            for bench_side, bz in (('S', -0.92), ('N', 0.92)):
                # Facing +X bench
                b.box(f'{c_short}_Bench_F_{bay_idx}_{bench_side}', (bx - 0.45, 1.30, bz), (0.48, 0.44, 0.85), 'RailWalnut', bevel=0.004)
                b.box(f'{c_short}_Cushion_F_{bay_idx}_{bench_side}', (bx - 0.45, 1.50, bz), (0.44, 0.06, 0.80), 'RailFelt', bevel=0.003)
                b.box(f'{c_short}_Back_F_{bay_idx}_{bench_side}', (bx - 0.68, 1.90, bz), (0.06, 0.80, 0.80), 'RailFelt', bevel=0.004)
                # Facing -X bench
                b.box(f'{c_short}_Bench_R_{bay_idx}_{bench_side}', (bx + 0.45, 1.30, bz), (0.48, 0.44, 0.85), 'RailWalnut', bevel=0.004)
                b.box(f'{c_short}_Cushion_R_{bay_idx}_{bench_side}', (bx + 0.45, 1.50, bz), (0.44, 0.06, 0.80), 'RailFelt', bevel=0.003)
                b.box(f'{c_short}_Back_R_{bay_idx}_{bench_side}', (bx + 0.68, 1.90, bz), (0.06, 0.80, 0.80), 'RailFelt', bevel=0.004)

        # Dedicated seating row at x = c_cx + 3.8:
        # "Free seating row at x3.8 should have seat oriented to face+X and allow viewpoint near x3.8, z-.92."
        # South bench facing +X: seat cushion at x = 3.8, z = -0.92, cushion top y = 1.52, back at x = 3.55
        b.box(f'{c_short}_DedicatedSeatSouth', (c_cx + 3.80, 1.30, -0.92), (0.50, 0.44, 0.85), 'RailWalnut', bevel=0.004)
        b.box(f'{c_short}_DedicatedCushionSouth', (c_cx + 3.80, 1.50, -0.92), (0.46, 0.06, 0.80), 'RailFelt', bevel=0.003)
        b.box(f'{c_short}_DedicatedBackSouth', (c_cx + 3.55, 1.90, -0.92), (0.06, 0.80, 0.80), 'RailFelt', bevel=0.004)
        # Matching North bench at x = 3.8
        b.box(f'{c_short}_DedicatedSeatNorth', (c_cx + 3.80, 1.30, 0.92), (0.50, 0.44, 0.85), 'RailWalnut', bevel=0.004)
        b.box(f'{c_short}_DedicatedCushionNorth', (c_cx + 3.80, 1.50, 0.92), (0.46, 0.06, 0.80), 'RailFelt', bevel=0.003)
        b.box(f'{c_short}_DedicatedBackNorth', (c_cx + 3.55, 1.90, 0.92), (0.06, 0.80, 0.80), 'RailFelt', bevel=0.004)

        # Brass luggage racks along walls above windows (y = 2.65, z = +-1.25)
        # Open lattice racks are authored by rail_detail_kit; no opaque shelf behind them.

        # Ceiling Pendant Diffusers along centerline (z = 0, y = 3.25)
        for lx in (-4.0, -1.5, 1.5, 4.0):
            b.cylinder(f'{c_short}_PendantStem_{lx:.1f}', (c_cx + lx, 3.38, 0.0), 0.015, 0.20, 'RailBrass', axis=(0, 1, 0))
            b.cylinder(f'{c_short}_PendantGlow_{lx:.1f}', (c_cx + lx, 3.25, 0.0), 0.12, 0.08, 'RailGlow', axis=(0, 1, 0))

        # Coach Bogies & Wheels (2 bogies per coach at c_cx +- 4.9)
        for b_idx, bx in enumerate((c_cx - 4.9, c_cx + 4.9)):
            # Bogie side frames / bolster are authored by rail_train_beautify.
            # 4 wheels per bogie (axles at bx +- 0.85)
            for ax_idx, ax_x in enumerate((bx - 0.85, bx + 0.85)):
                for side, wz in (('L', 0.95), ('R', -0.95)):
                    w_name = f'Wheel_{c_short}_B{b_idx+1}_{side}{ax_idx+1}'
                    w_empty = b.empty(w_name, (ax_x, 0.48 + WHEEL_LIFT, wz))
                    create_wheel(b, w_name, (ax_x, 0.48 + WHEEL_LIFT, wz), 0.48, is_driver=False, num_spokes=10, parent_empty=w_empty)

        # Dynamic Doors (8 Doors Total: 4 on Coach 1, 4 on Coach 2)
        # Side doors at local x = +-5.9 center. Width 1.05 along X, y in [1.05, 3.35]
        # Door hinge at x = center - 0.525, y = 1.05, z = +-1.49
        # Names: Door_C1_S_F, Door_C1_N_F, Door_C1_S_R, Door_C1_N_R; C2 similarly
        doors_spec = [
            (f'Door_{c_short}_S_F', c_cx + 5.9, -1.49, 'Front', 'South'),
            (f'Door_{c_short}_N_F', c_cx + 5.9, 1.49, 'Front', 'North'),
            (f'Door_{c_short}_S_R', c_cx - 5.9, -1.49, 'Rear', 'South'),
            (f'Door_{c_short}_N_R', c_cx - 5.9, 1.49, 'Rear', 'North'),
        ]
        for d_name, d_center_x, d_hinge_z, d_end, d_side in doors_spec:
            hinge_x = d_center_x - 0.525
            d_empty = b.empty(d_name, (hinge_x, 1.05, d_hinge_z))

            b.box(d_name+'_LowerPanel',(d_center_x,1.60,d_hinge_z),(1.05,1.10,.04),'RailGreen',parent=d_empty)
            b.frame(d_name+'_UpperFrame',(d_center_x,2.775,d_hinge_z),(1.05,1.15,.04),'RailIvory',parent=d_empty)
            b.box(d_name+'_ClearGlass',(d_center_x,2.775,d_hinge_z),(.91,1.01,.015),'RailGlass',parent=d_empty)
            b.box(d_name+'_Handle',(d_center_x+.38,2.10,d_hinge_z-.04),(.06,.16,.03),'RailBrass',parent=d_empty)

    # Dedicated Markers in Coach 01
    b.empty('RideSeat', (3.8, 1.05, -0.92))
    b.empty('StandMarker', (3.8, 1.05, 0.0))
    b.empty('EntryMarker', (5.9, 1.05, -1.9))

    # -----------------------------------------------------------------------
    # 4. GUARD / LUGGAGE VAN: center x=-29, length 9, width 2.9 (x in [-33.5, -24.5])
    # -----------------------------------------------------------------------
    b.set_module('Module_Luggage_Static')
    van_cx = -29.0
    # Chassis
    b.box('LuggageChassis', (van_cx, 0.915, 0.0), (8.9, 0.13, 2.60), 'RailIron', bevel=0.01)
    # End Buffer Beams
    for end_sign, ex in ((-1, van_cx - 4.45), (1, van_cx + 4.45)):
        b.box(f'LuggageBufferBeam_{end_sign}', (ex, 0.85, 0.0), (0.22, 0.45, 2.40), 'RailRed', bevel=0.008)
        for bz in (-0.85, 0.85):
            b.cylinder(f'LuggageBufferStem_{end_sign}_{bz:.1f}', (ex + end_sign * 0.20, 0.85, bz), 0.065, 0.35, 'RailIron', axis=(end_sign, 0, 0))
            b.cylinder(f'LuggageBufferPlate_{end_sign}_{bz:.1f}', (ex + end_sign * 0.38, 0.85, bz), 0.18, 0.03, 'RailIron', axis=(end_sign, 0, 0))
        b.ring(f'LuggageCouplerHook_{end_sign}', (ex + end_sign * 0.20, 0.85, 0.0), 0.08, 0.022, 'RailIron', axis=(0, 1, 0))

    # Floor: top y = 1.05
    b.box('LuggageFloor', (van_cx, 1.00, 0.0), (8.8, 0.10, 2.80), 'RailWalnut', bevel=0.005)
    # Body walls: x in [-33.4, -24.6], y in [1.05, 3.55], z = +-1.45
    for side_name, wz in (('S', -1.45), ('N', 1.45)):
        # Sliding freight cargo doors at van_cx: width 1.8, y in [1.05, 3.20]
        # Wall left of door
        # (The walls previously stopped 0.9m short of both end walls, leaving open slots.)
        b.box(f'LuggageWallW_{side_name}', (van_cx - 2.67, 2.30, wz), (3.54, 2.50, 0.08), 'RailGreen', bevel=0.005)
        # Wall right of door
        b.box(f'LuggageWallE_{side_name}', (van_cx + 2.67, 2.30, wz), (3.54, 2.50, 0.08), 'RailGreen', bevel=0.005)
        # Lintel above freight door
        b.box(f'LuggageDoorLintel_{side_name}', (van_cx, 3.375, wz), (1.8, 0.35, 0.08), 'RailGreen', bevel=0.004)
        # Sliding freight door leaf (recessed / framed)
        b.box(f'LuggageCargoDoor_{side_name}', (van_cx, 2.125, wz - 0.02 if wz < 0 else wz + 0.02), (1.76, 2.15, 0.05), 'RailWalnut', bevel=0.005)

    # End walls
    b.box('LuggageEndWall_W', (van_cx - 4.4, 2.30, 0.0), (0.08, 2.50, 2.80), 'RailGreen', bevel=0.006)
    b.box('LuggageEndWall_E', (van_cx + 4.4, 2.30, 0.0), (0.08, 2.50, 2.80), 'RailGreen', bevel=0.006)

    # Roof: inner 3.5, outer 3.8
    b.box('LuggageRoof', (van_cx, 3.65, 0.0), (8.9, 0.28, 2.95), 'RailSlate', bevel=0.015)

    # Guard Lookout Ducket / Cupola: projecting at x = van_cx + 3.0 (x = -26.0)
    for dz in (-1.58, 1.58):
        b.box(f'GuardDucket_{dz:.1f}', (-26.0, 2.50, dz), (1.20, 1.20, 0.28), 'RailGreen', bevel=0.005)
        b.box(f'GuardDucketWin_{dz:.1f}', (-26.0, 2.50, dz), (0.75, 0.75, 0.30), 'RailGlass', bevel=0.0)

    # Interior Furniture: Wooden Crates, Weigh Scale, Guard Desk
    b.box('LuggageCrate1', (van_cx - 2.5, 1.50, -0.7), (0.90, 0.80, 0.90), 'RailWalnut', bevel=0.005)
    b.box('LuggageCrate2', (van_cx - 2.5, 1.40, 0.6), (0.80, 0.70, 0.80), 'RailWalnut', bevel=0.005)
    b.box('LuggageTrunk', (van_cx - 1.2, 1.35, -0.8), (1.10, 0.60, 0.60), 'RailWalnut', bevel=0.006)
    b.box('GuardDesk', (van_cx + 2.5, 1.50, 0.0), (1.20, 0.90, 0.70), 'RailWalnut', bevel=0.005)

    # Guard Van Wheels (4 heavy wheels at van_cx +- 2.4, radius 0.50)
    for a_idx, ax in enumerate((van_cx - 2.4, van_cx + 2.4)):
        for side, wz in (('L', 0.95), ('R', -0.95)):
            w_name = f'Wheel_Luggage_{side}{a_idx+1}'
            w_empty = b.empty(w_name, (ax, 0.50 + WHEEL_LIFT, wz))
            create_wheel(b, w_name, (ax, 0.50 + WHEEL_LIFT, wz), 0.50, is_driver=False, num_spokes=10, parent_empty=w_empty)

    # Join static modules by material (dynamic empties Wheel_*, Rod_*, Door_* untouched)
    from rail_detail_kit import add_train_details
    add_train_details(b)
    from rail_train_beautify import add_train_beautify
    add_train_beautify(b)
    b.join_static_modules_by_material()

    # Render Preview: train-threequarter.png
    cam_data = bpy.data.cameras.new('TempCamTrain')
    cam_data.lens = 35
    cam_obj = bpy.data.objects.new('TempCamTrain', cam_data)
    b.collection.objects.link(cam_obj)
    b.scene.camera = cam_obj
    # Three-quarter perspective looking down train consist from front-side
    cam_obj.location = u((32.0, 5.5, -12.0))
    dir_to_target = (u((15.0, 2.0, 0.0)) - cam_obj.location).normalized()
    cam_obj.matrix_world = Matrix.Translation(cam_obj.location) @ dir_to_target.to_track_quat('-Z', 'Y').to_matrix().to_4x4()

    preview_path = str(out_dir / 'train-threequarter.png')
    b.scene.render.engine = 'BLENDER_WORKBENCH'
    b.scene.display.shading.light='STUDIO'
    b.scene.display.shading.color_type='MATERIAL'
    b.scene.display.shading.show_shadows=True
    b.scene.display.shading.show_cavity=True
    b.scene.display.shading.background_type='WORLD'
    b.scene.world.color=(.22,.24,.28)
    b.scene.render.resolution_x = 1280
    b.scene.render.resolution_y = 720
    b.scene.render.filepath = preview_path
    try:
        bpy.ops.render.render(write_still=True)
        print(f"Train preview rendered to {preview_path}")
    except Exception as e:
        print(f"EEVEE render failed ({e}), falling back to workbench...")
        b.scene.render.engine = 'BLENDER_WORKBENCH'
        b.scene.display.shading.light = 'STUDIO'
        b.scene.display.shading.color_type = 'MATERIAL'
        b.scene.display.shading.show_shadows = True
        bpy.ops.render.render(write_still=True)

    # Remove temporary camera
    bpy.data.objects.remove(cam_obj, do_unlink=True)
    bpy.data.cameras.remove(cam_data, do_unlink=True)

    # Export FBX & save Blend
    fbx_file = out_dir / 'TrainSet.fbx'
    blend_file = out_dir / 'TrainSet.blend'
    b.export_fbx_and_blend(fbx_file, blend_file)
    print(f"TrainSet saved to {blend_file} and {fbx_file}")
    return blend_file, fbx_file


# ---------------------------------------------------------------------------
# Reopen, Inspect & Write Manifest / Layout
# ---------------------------------------------------------------------------

def reopen_and_inspect(blend_path):
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))
    mesh_objs = [o for o in bpy.data.objects if o.type == 'MESH']
    empty_objs = [o for o in bpy.data.objects if o.type == 'EMPTY']
    mats_used = sorted(list({m.name for o in mesh_objs for m in o.data.materials if m}))

    all_verts = sum(len(o.data.vertices) for o in mesh_objs)
    all_faces = sum(len(o.data.polygons) for o in mesh_objs)

    points = [o.matrix_world @ v.co for o in mesh_objs for v in o.data.vertices]
    b_min = [min(p[i] for p in points) for i in range(3)] if points else [0, 0, 0]
    b_max = [max(p[i] for p in points) for i in range(3)] if points else [0, 0, 0]

    # Convert bounds back to Unity coords: Unity x = -bx, y = bz, z = -by
    # Note: b_min in Blender X maps to b_max in Unity X, etc.
    unity_min = [-b_max[0], b_min[2], -b_max[1]]
    unity_max = [-b_min[0], b_max[2], -b_min[1]]

    return {
        'reopenedFile': blend_path.name,
        'meshObjectsCount': len(mesh_objs),
        'emptyObjectsCount': len(empty_objs),
        'totalVertices': all_verts,
        'totalFaces': all_faces,
        'materials': mats_used,
        'blenderBoundsMin': [round(v, 3) for v in b_min],
        'blenderBoundsMax': [round(v, 3) for v in b_max],
        'unityBoundsMin': [round(v, 3) for v in unity_min],
        'unityBoundsMax': [round(v, 3) for v in unity_max],
        'meshNames': sorted([o.name for o in mesh_objs]),
        'emptyNames': sorted([o.name for o in empty_objs]),
    }


def write_layout_json(out_dir):
    layout = {
        "metadata": {
            "title": "1931 Railway Station & Train Visual Specification",
            "era": 1931,
            "version": "1.0",
            "coordinateSystem": "Unity (X East, Y Up, Z North)"
        },
        "station": {
            "root": [188.0, 0.0, 46.0],
            "mainHall": {
                "boundsX": [-15.0, 15.0],
                "boundsZ": [-6.0, 6.0],
                "floorTopY": 0.10,
                "eavesY": 6.00,
                "ridgeY": 7.60,
                "wallThickness": 0.24,
                "centeredDoors": {
                    "front": {"x": [-1.7, 1.7], "z": -6.0, "height": 3.4, "status": "always_open_no_leaf"},
                    "rear": {"x": [-1.7, 1.7], "z": 6.0, "height": 3.4, "status": "always_open_no_leaf"}
                },
                "facadeWindows": {
                    "centersX": [-12.0, -8.0, -4.0, 4.0, 8.0, 12.0],
                    "width": 2.20,
                    "yRange": [1.30, 4.10],
                    "facades": ["South (z=-6.0)", "North (z=6.0)"],
                    "apertures": "real_round_arch_with_translucent_glazing",
                    "archSpringY": 3.0,
                    "archRadius": 1.1
                },
                "annexPassages": {
                    "west": {"x": -15.0, "z": [-1.4, 1.4], "yRange": [0.10, 3.30]},
                    "east": {"x": 15.0, "z": [-1.4, 1.4], "yRange": [0.10, 3.30]}
                },
                "centralWalkingRoute": {"x": [-2.0, 2.0], "z": [-6.0, 6.0], "status": "completely_unobstructed"}
            },
            "annexes": {
                "west": {"x": [-23.0, -15.0], "z": [-4.0, 4.0], "eavesY": 4.0, "ridgeY": 5.2},
                "east": {"x": [15.0, 23.0], "z": [-4.0, 4.0], "eavesY": 4.0, "ridgeY": 5.2},
                "windows": [
                    {"wall": "WestOuter (x=-23)", "centersZ": [-2.0, 2.0], "width": 1.4, "yRange": [1.3, 3.2]},
                    {"wall": "EastOuter (x=23)", "centersZ": [-2.0, 2.0], "width": 1.4, "yRange": [1.3, 3.2]}
                ]
            },
            "shedCanopy": {
                "spanX": [-34.0, 34.0],
                "spanZ": [9.0, 31.0],
                "springHeightY": 5.40,
                "archRise": 4.00,
                "ridgeHeightY": 9.40,
                "barrelRadius": 17.125,
                "columns": {
                    "rowsZ": [9.0, 31.0],
                    "stationsX": [-30.0, -20.0, -10.0, 0.0, 10.0, 20.0, 30.0],
                    "count": 14
                },
                "platformBoardingClearArea": {
                    "boundsX": [-30.0, 30.0],
                    "boundsZ": [17.0, 25.45],
                    "topY": 0.42,
                    "status": "clear_unobstructed_no_structure_collision"
                }
            },
            "interiorFeatures": [
                {"name": "TicketDesk", "local": [-8.0, 0.0, 1.0], "material": "RailWalnut/RailBrass"},
                {"name": "TimetableDesk", "local": [9.0, 0.0, 1.0], "material": "RailWalnut/RailIvory"},
                {"name": "LuggageScale", "local": [11.5, 0.0, -2.0], "material": "RailIron/RailBrass"},
                {"name": "WaitingBenches", "wings": ["West (x in [-11, -5])", "East (x in [5, 11])"], "material": "RailWalnut/RailFelt"}
            ],
            "signage": [
                {"text": "QINGCHUAN", "location": "Front Facade Pediment (0, 6.25, -6.25)", "material": "RailBrass"},
                {"text": "售票 · 检票", "location": "Supported ticket-office header (-8.0, 2.70, 0.955)", "material": "RailGreen backing / ivory fitted lettering"},
                {"text": "PLATFORM 1", "location": "Canopy Truss Suspended (0.0, 4.95, 18.0)", "material": "RailIvory"}
            ],
            "markers": {
                "EastMarker": [3.0, 0.0, 0.0],
                "FrontMarker": [0.0, 0.0, -3.0],
                "FloorMarker": [0.0, 1.05, 0.0],
                "Lamp_Hall": [[-8.0, 4.5, 0.0], [-3.0, 4.8, 0.0], [3.0, 4.8, 0.0], [8.0, 4.5, 0.0]],
                "Lamp_Platform": [
                    [-25.0, 5.8, 20.0], [-15.0, 5.8, 20.0], [-5.0, 5.8, 20.0],
                    [5.0, 5.8, 20.0], [15.0, 5.8, 20.0], [25.0, 5.8, 20.0]
                ]
            }
        },
        "train": {
            "root": [181.0, 0.0, 73.0],
            "orientation": "Faces +X (East)",
            "consist": [
                {"unit": "GuardLuggageVan", "centerX": -29.0, "length": 9.0, "width": 2.9},
                {"unit": "Coach02", "centerX": -16.0, "length": 15.0, "width": 2.9},
                {"unit": "Coach01", "centerX": 0.0, "length": 15.0, "width": 2.9},
                {"unit": "Tender", "centerX": 11.5, "length": 7.0, "width": 2.7},
                {"unit": "Locomotive_260", "centerX": 20.0, "length": 10.5, "frontBufferX": 26.0}
            ],
            "locomotive": {
                "wheelArrangement": "2-6-0",
                "leadingAxle": {"x": 23.6, "radius": 0.40, "centreY": 0.49, "gaugeZ": 0.95, "empties": ["Wheel_L_Lead", "Wheel_R_Lead"]},
                "drivingAxles": [
                    {"x": 20.9, "radius": 0.73, "empties": ["Wheel_L_Drive1", "Wheel_R_Drive1"]},
                    {"x": 19.1, "radius": 0.73, "empties": ["Wheel_L_Drive2", "Wheel_R_Drive2"]},
                    {"x": 17.3, "radius": 0.73, "empties": ["Wheel_L_Drive3", "Wheel_R_Drive3"]}
                ],
                "sideRods": {
                    "empties": ["Rod_Left", "Rod_Right"],
                    "pivotNeutral": [19.1, 0.82, "+-1.05"], "crankRadius": 0.30,
                    "crankpinSpan": [17.3, 20.9]
                },
                "markers": {
                    "SmokestackMarker": [23.0, 3.6, 0.0],
                    "SteamMarker": [20.0, 0.8, -1.1]
                }
            },
            "tender": {
                "axles": [9.3, 11.5, 13.7],
                "wheelRadius": 0.48,
                "wheelCentreY": 0.57,
                "railHeadTopY": 0.095,
                "empties": ["Wheel_Tender_L1", "Wheel_Tender_R1", "Wheel_Tender_L2", "Wheel_Tender_R2", "Wheel_Tender_L3", "Wheel_Tender_R3"]
            },
            "coaches": {
                "floorTopY": 1.05,
                "roofInnerYRange": [3.365, 3.705],
                "roofOuterYRange": [3.46, 3.80],
                "wallsZ": [-1.45, 1.45],
                "doors": {
                    "localCenterOffsetsX": [-5.9, 5.9],
                    "openingWidth": 1.05,
                    "openingYRange": [1.05, 3.35],
                    "hingeOffset": "center - 0.525",
                    "hingeY": 1.05,
                    "hingeZ": [-1.49, 1.49],
                    "defaultState": "CLOSED",
                    "rotationAxis": "Unity Y",
                    "empties": [
                        "Door_C1_S_F", "Door_C1_N_F", "Door_C1_S_R", "Door_C1_N_R",
                        "Door_C2_S_F", "Door_C2_N_F", "Door_C2_S_R", "Door_C2_N_R"
                    ]
                },
                "windows": {
                    "localCentersX": [-4.5, -2.7, -0.9, 0.9, 2.7, 4.5],
                    "width": 1.45,
                    "yRange": [1.80, 3.20],
                    "bottomPanel": [1.05, 1.80],
                    "topFascia": [3.20, 3.55],
                    "posts": "only_in_gaps"
                },
                "vestibules": {
                    "frontX": [5.2, 7.3],
                    "rearX": [-7.3, -5.2],
                    "internalPassengerOpeningWidthZ": 0.80,
                    "gangwayEndOpeningWidthZ": 0.80,
                    "gangwayClearHeight": 2.20
                },
                "seating": {
                    "aisleZ": [-0.40, 0.40],
                    "benchesZ": "|z| > 0.45",
                    "cushionTopY": 1.52,
                    "backTopY": 2.30,
                    "dedicatedRow": {
                        "x": 3.8,
                        "orientation": "Faces +X",
                        "seatCenter": [3.8, 1.05, -0.92]
                    }
                },
                "markersCoach1": {
                    "RideSeat": [3.8, 1.05, -0.92],
                    "StandMarker": [3.8, 1.05, 0.0],
                    "EntryMarker": [5.9, 1.05, -1.9]
                }
            }
        }
    }
    office_file = out_dir / 'ticket-office-geometry.json'
    if office_file.exists():
        layout['station']['ticketOffice'] = json.loads(office_file.read_text(encoding='utf-8'))
    target_file = out_dir / 'rail-layout.json'
    target_file.write_text(json.dumps(layout, indent=2) + '\n', encoding='utf-8')
    print(f"rail-layout.json written to {target_file}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=Path(__file__).parent.resolve(), help="Output directory")
    parser.add_argument('--station-only', action='store_true', help='Preserve train and ticket assets; rebuild Station B only.')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:])
    out_dir = args.output.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    print(f"Building railway visual assets in: {out_dir}")

    if args.station_only:
        build_station(out_dir)
        inspection = reopen_and_inspect(out_dir / 'StationB.blend')
        manifest_path = out_dir / 'source-manifest.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
        manifest['station'] = inspection
        manifest['ticketOffice'] = json.loads((out_dir / 'ticket-office-geometry.json').read_text(encoding='utf-8'))
        manifest['unityRuntimeChecked'] = False
        manifest_path.write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
        layout_path = out_dir / 'rail-layout.json'
        layout = json.loads(layout_path.read_text(encoding='utf-8'))
        layout['station']['ticketOffice'] = manifest['ticketOffice']
        layout_path.write_text(json.dumps(layout, indent=2)+'\n', encoding='utf-8')
        print('STATION_ONLY_GENERATION_PASS: existing train and ticket sources preserved')
        return

    # Build assets
    station_blend, station_fbx = build_station(out_dir)
    train_blend, train_fbx = build_train(out_dir)

    # Reopen and inspect both source .blend files
    station_inspection = reopen_and_inspect(station_blend)
    train_inspection = reopen_and_inspect(train_blend)

    # Manifest
    manifest = {
        "generator": "build_rail_assets.py",
        "blenderVersion": bpy.app.version_string,
        "unityCoordinateConvention": "X East, Y Up, Z North",
        "fbxExportHelper": "u(p)=(-p[0], -p[2], p[1]) with axis_forward=-Z, axis_up=Y",
        "station": station_inspection,
        "train": train_inspection,
        "previews": {
            "station": "station-front.png",
            "train": "train-threequarter.png"
        },
        "provenance": "Original procedural geometry for a fictional 1931-inspired railway. Antigravity first draft reviewed and refined locally: real apertures, wheel pivots, pitched and arched roofs, clock and signage integration. No downloaded third-party models.",
        "unityRuntimeChecked": False
    }

    manifest['ticketOffice'] = json.loads((out_dir / 'ticket-office-geometry.json').read_text(encoding='utf-8'))
    manifest_file = out_dir / 'source-manifest.json'
    manifest_file.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(f"source-manifest.json written to {manifest_file}")

    # Write Layout JSON
    write_layout_json(out_dir)

    print(">>> All railway visual assets built and verified successfully!")


if __name__ == '__main__':
    main()
