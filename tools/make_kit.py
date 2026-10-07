"""
make_kit.py - Low-poly 1931 detective game asset kit generator for Blender 5.2.
Generates: kit.blend, manifest.json, preview.png, Models/*.fbx
"""

import os
import sys
import math
import json
import argparse
from pathlib import Path

import bpy
import bmesh
from mathutils import Matrix, Vector

PALETTE = {
    "wood": "#8C5835",
    "darkwood": "#382218",
    "brass": "#C89E4B",
    "iron": "#2B2E33",
    "glass_glow": "#FFAA33",
    "skin": "#D49D78",
    "coat": "#2B363F",
    "trousers": "#1D2228",
    "cream": "#E8DFC8",
    "jade": "#467A65",
}

def hex_to_linear(hex_str):
    h = hex_str.lstrip("#")
    r, g, b = int(h[0:2], 16) / 255.0, int(h[2:4], 16) / 255.0, int(h[4:6], 16) / 255.0
    def to_lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (to_lin(r), to_lin(g), to_lin(b), 1.0)

def create_materials():
    materials = {}
    for name, hex_code in PALETTE.items():
        mat = bpy.data.materials.new(name=name)
        rgba = hex_to_linear(hex_code)
        mat.diffuse_color = rgba
        bsdf = mat.node_tree.nodes.get("Principled BSDF") if mat.node_tree else None
        if bsdf:
            bsdf.inputs["Base Color"].default_value = rgba
            if name == "brass":
                bsdf.inputs["Metallic"].default_value = 0.85
                bsdf.inputs["Roughness"].default_value = 0.32
            elif name == "iron":
                bsdf.inputs["Metallic"].default_value = 0.65
                bsdf.inputs["Roughness"].default_value = 0.58
            elif name == "glass_glow":
                bsdf.inputs["Roughness"].default_value = 0.15
                if "Emission Color" in bsdf.inputs:
                    bsdf.inputs["Emission Color"].default_value = rgba
                if "Emission Strength" in bsdf.inputs:
                    bsdf.inputs["Emission Strength"].default_value = 6.0
            else:
                bsdf.inputs["Roughness"].default_value = 0.70
        materials[name] = mat
    return materials

def add_box(bm, center, size, mat_idx=0):
    mat = Matrix.Translation(Vector(center)) @ Matrix.Diagonal(Vector((size[0], size[1], size[2], 1.0)))
    res = bmesh.ops.create_cube(bm, size=1.0, matrix=mat)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat_idx
    return res

def add_cone_z(bm, center, r1, r2, depth, segments=8, mat_idx=0, rot_euler=None):
    mat = Matrix.Translation(Vector(center))
    if rot_euler:
        mat = mat @ rot_euler.to_matrix().to_4x4()
    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=r1, radius2=r2, depth=depth, matrix=mat)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat_idx
    return res

def add_cylinder_x(bm, center, r, depth, segments=12, mat_idx=0):
    rot = Matrix.Rotation(math.radians(90), 4, "Y")
    mat = Matrix.Translation(Vector(center)) @ rot
    res = bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=r, radius2=r, depth=depth, matrix=mat)
    for f in {f for v in res["verts"] for f in v.link_faces}:
        f.material_index = mat_idx
    return res

def add_creased_leaf(bm, base_pos, tip_pos, side1_pos, side2_pos, crease_up=0.03, mat_idx=0):
    # Two-triangle creased lowpoly leaf for faceted silhouette
    b = bm.verts.new(base_pos)
    t = bm.verts.new(tip_pos)
    s1 = bm.verts.new(side1_pos)
    s2 = bm.verts.new(side2_pos)
    mid_pos = (base_pos + tip_pos) * 0.5 + Vector((0, 0, crease_up))
    m = bm.verts.new(mid_pos)
    f1 = bm.faces.new((b, s1, m))
    f2 = bm.faces.new((m, s1, t))
    f3 = bm.faces.new((b, m, s2))
    f4 = bm.faces.new((m, t, s2))
    for f in [f1, f2, f3, f4]:
        f.material_index = mat_idx

def setup_root(name, mat_names, materials, populate_fn):
    root = bpy.data.objects.new(name, None)
    root.empty_display_type = "PLAIN_AXES"
    root.location = (0.0, 0.0, 0.0)

    mesh = bpy.data.meshes.new(f"{name}_Mesh")
    for m in mat_names:
        mesh.materials.append(materials[m])

    bm = bmesh.new()
    mat_map = {m: i for i, m in enumerate(mat_names)}
    populate_fn(bm, mat_map)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()

    mesh_obj = bpy.data.objects.new(f"{name}_Mesh", mesh)
    mesh_obj.parent = root
    mesh_obj.location = (0.0, 0.0, 0.0)

    col = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(col)
    col.objects.link(root)
    col.objects.link(mesh_obj)

    return root, mesh_obj

def build_lantern(materials):
    mat_names = ["iron", "brass", "glass_glow"]
    def populate(bm, m):
        # Base plate & collar at Z=0..0.24
        add_box(bm, (0.0, 0.0, 0.04), (0.44, 0.44, 0.08), m["iron"])
        add_box(bm, (0.0, 0.0, 0.16), (0.32, 0.32, 0.16), m["iron"])
        # Iron post from 0.24 to 2.84
        add_box(bm, (0.0, 0.0, 1.54), (0.14, 0.14, 2.60), m["iron"])
        # Brass decorative bands on post
        add_box(bm, (0.0, 0.0, 0.90), (0.18, 0.18, 0.04), m["brass"])
        add_box(bm, (0.0, 0.0, 1.80), (0.18, 0.18, 0.04), m["brass"])
        add_box(bm, (0.0, 0.0, 2.70), (0.18, 0.18, 0.04), m["brass"])
        # Bracket collar support leading up to 3.0m
        add_box(bm, (0.0, 0.0, 2.92), (0.24, 0.24, 0.16), m["iron"])
        # 4-sided warm lantern housing at 3.0m with brass trim
        add_box(bm, (0.0, 0.0, 3.02), (0.40, 0.40, 0.04), m["brass"])
        # Emissive warm amber lantern glass
        add_box(bm, (0.0, 0.0, 3.17), (0.34, 0.34, 0.26), m["glass_glow"])
        # Iron corner frame struts
        for cx in [-0.17, 0.17]:
            for cy in [-0.17, 0.17]:
                add_box(bm, (cx, cy, 3.17), (0.03, 0.03, 0.26), m["iron"])
        # Brass top cornice
        add_box(bm, (0.0, 0.0, 3.32), (0.42, 0.42, 0.04), m["brass"])
        # Pyramidal cap
        rot45 = Matrix.Rotation(math.radians(45), 4, "Z").to_euler()
        add_cone_z(bm, (0.0, 0.0, 3.40), 0.42 * math.sqrt(2)/2, 0.0, 0.16, segments=4, mat_idx=m["iron"], rot_euler=rot45)
        # Brass finial apex to exactly 3.5m
        add_box(bm, (0.0, 0.0, 3.49), (0.04, 0.04, 0.02), m["brass"])
    return setup_root("Lantern", mat_names, materials, populate)

def build_bench(materials):
    mat_names = ["darkwood", "iron"]
    def populate(bm, m):
        # Iron legs and armrests at X = +/- 0.75m
        for x in [-0.75, 0.75]:
            add_box(bm, (x, -0.18, 0.2275), (0.05, 0.05, 0.455), m["iron"])
            add_box(bm, (x,  0.18, 0.2275), (0.05, 0.05, 0.455), m["iron"])
            add_box(bm, (x,  0.00, 0.44),   (0.05, 0.41, 0.03),  m["iron"])
            add_box(bm, (x,  0.22, 0.655),  (0.04, 0.04, 0.41),  m["iron"])
            add_box(bm, (x,  0.00, 0.60),   (0.04, 0.40, 0.03),  m["iron"])
        add_box(bm, (0.0, 0.0, 0.12), (1.55, 0.03, 0.03), m["iron"])

        # Dark wood slats (width 1.9m along X)
        # Seat slats: top surface at Z=0.48m (thickness 0.025, center 0.4675)
        for y in [-0.15, -0.05, 0.05, 0.15]:
            add_box(bm, (0.0, y, 0.4675), (1.90, 0.07, 0.025), m["darkwood"])
        # Back slats
        for z, y in [(0.58, 0.20), (0.70, 0.22), (0.82, 0.24)]:
            add_box(bm, (0.0, y, z), (1.90, 0.025, 0.07), m["darkwood"])
    return setup_root("Bench", mat_names, materials, populate)

def build_milk_cart(materials):
    mat_names = ["wood", "darkwood", "iron", "cream"]
    def populate(bm, m):
        wheel_r = 0.34
        wheel_z = 0.34
        wheel_y = 0.00
        # Two wheels: outer faces at +/-0.675 -> width 1.35m
        add_cylinder_x(bm, (-0.65, wheel_y, wheel_z), wheel_r, 0.05, segments=12, mat_idx=m["iron"])
        add_cylinder_x(bm, ( 0.65, wheel_y, wheel_z), wheel_r, 0.05, segments=12, mat_idx=m["iron"])
        add_cylinder_x(bm, ( 0.00, wheel_y, wheel_z), 0.03, 1.25, segments=8, mat_idx=m["iron"])
        add_cylinder_x(bm, (-0.65, wheel_y, wheel_z), 0.07, 0.05, segments=8, mat_idx=m["darkwood"])
        add_cylinder_x(bm, ( 0.65, wheel_y, wheel_z), 0.07, 0.05, segments=8, mat_idx=m["darkwood"])

        # Chassis underframe
        add_box(bm, (0.0, wheel_y, 0.37), (1.05, 0.10, 0.06), m["darkwood"])
        # Cart floor & walls (depth from -0.375 to +0.375 -> 0.75m)
        add_box(bm, (0.0, 0.00, 0.41), (0.95, 0.60, 0.04), m["wood"])
        add_box(bm, (0.0, -0.28, 0.52), (0.95, 0.04, 0.18), m["wood"])
        add_box(bm, (0.0,  0.30, 0.52), (0.95, 0.04, 0.18), m["wood"])
        add_box(bm, (-0.455, 0.01, 0.52), (0.04, 0.62, 0.18), m["wood"])
        add_box(bm, ( 0.455, 0.01, 0.52), (0.04, 0.62, 0.18), m["wood"])

        # Rest legs to ground Z=0
        add_box(bm, (-0.35, -0.26, 0.19), (0.05, 0.05, 0.38), m["darkwood"])
        add_box(bm, ( 0.35, -0.26, 0.19), (0.05, 0.05, 0.38), m["darkwood"])

        # Front handle shafts extending to Y = -0.375m
        add_box(bm, (-0.28, -0.32, 0.58), (0.04, 0.11, 0.04), m["darkwood"])
        add_box(bm, ( 0.28, -0.32, 0.58), (0.04, 0.11, 0.04), m["darkwood"])
        add_box(bm, ( 0.00, -0.36, 0.64), (0.68, 0.03, 0.04), m["darkwood"])
        # Rear lip extending to Y = +0.375m
        add_box(bm, ( 0.00, 0.36, 0.52),  (0.95, 0.03, 0.18), m["darkwood"])

        # Four faceted milk cans (height reaching 1.00m)
        for cx, cy in [(-0.22, -0.13), (0.22, -0.13), (-0.22, 0.13), (0.22, 0.13)]:
            add_cone_z(bm, (cx, cy, 0.58), 0.12, 0.12, 0.30, segments=10, mat_idx=m["iron"])
            add_cone_z(bm, (cx, cy, 0.79), 0.12, 0.07, 0.12, segments=10, mat_idx=m["iron"])
            add_cone_z(bm, (cx, cy, 0.88), 0.07, 0.07, 0.06, segments=10, mat_idx=m["iron"])
            add_cone_z(bm, (cx, cy, 0.93), 0.08, 0.08, 0.04, segments=10, mat_idx=m["cream"])
            add_box(bm, (cx, cy, 0.975), (0.08, 0.02, 0.05), m["iron"])
    return setup_root("MilkCart", mat_names, materials, populate)

def build_clockmaker(materials):
    mat_names = ["trousers", "darkwood", "cream", "coat", "skin"]
    def populate(bm, m):
        # Shoes at Z=0..0.08, pointing -Y
        add_box(bm, (-0.11, -0.04, 0.04), (0.10, 0.20, 0.08), m["darkwood"])
        add_box(bm, ( 0.11, -0.04, 0.04), (0.10, 0.20, 0.08), m["darkwood"])

        # Trousers legs & hips
        add_cone_z(bm, (-0.11, 0.0, 0.46), 0.08, 0.07, 0.76, segments=8, mat_idx=m["trousers"])
        add_cone_z(bm, ( 0.11, 0.0, 0.46), 0.08, 0.07, 0.76, segments=8, mat_idx=m["trousers"])
        add_box(bm, (0.0, 0.0, 0.88), (0.34, 0.22, 0.12), m["trousers"])

        # Cream shirt torso
        add_box(bm, (0.0, 0.0, 1.16), (0.36, 0.22, 0.48), m["cream"])

        # Dark apron: bib on chest, hanging skirt, waistband, shoulder straps
        add_box(bm, (0.0, -0.115, 1.18), (0.24, 0.02, 0.32), m["coat"])
        add_box(bm, (0.0, -0.115, 0.74), (0.32, 0.02, 0.36), m["coat"])
        add_box(bm, (0.0,  0.000, 0.93), (0.37, 0.23, 0.04), m["coat"])
        add_box(bm, (-0.08, -0.08, 1.34), (0.03, 0.07, 0.06), m["coat"])
        add_box(bm, ( 0.08, -0.08, 1.34), (0.03, 0.07, 0.06), m["coat"])

        # Arms hanging down (rolled cream sleeves, bare forearms, hands)
        for ax in [-0.23, 0.23]:
            add_cone_z(bm, (ax, 0.0, 1.27), 0.05, 0.055, 0.22, segments=8, mat_idx=m["cream"])
            add_cone_z(bm, (ax, 0.0, 1.14), 0.065, 0.065, 0.04, segments=8, mat_idx=m["cream"])
            add_cone_z(bm, (ax, 0.0, 1.00), 0.045, 0.045, 0.24, segments=8, mat_idx=m["skin"])
            add_box(bm, (ax, 0.0, 0.80), (0.06, 0.07, 0.16), m["skin"])

        # Neck & faceless stylized head
        add_cone_z(bm, (0.0, 0.0, 1.43), 0.05, 0.05, 0.06, segments=8, mat_idx=m["skin"])
        add_cone_z(bm, (0.0, 0.0, 1.54), 0.09, 0.085, 0.16, segments=8, mat_idx=m["skin"])

        # Flat hair (back/sides) & 1931 cap reaching Z=1.72m
        add_box(bm, (0.0, 0.05, 1.56), (0.19, 0.10, 0.12), m["darkwood"])
        add_box(bm, (0.0, -0.10, 1.63), (0.18, 0.08, 0.025), m["coat"])
        add_cone_z(bm, (0.0, -0.02, 1.66), 0.13, 0.11, 0.08, segments=8, mat_idx=m["coat"])
        add_box(bm, (0.0, -0.02, 1.71), (0.04, 0.04, 0.02), m["coat"])
    return setup_root("Clockmaker", mat_names, materials, populate)

def build_desk(materials):
    mat_names = ["wood", "darkwood", "brass"]
    def populate(bm, m):
        # Workbench top: 1.8m wide, 0.8m deep, top at 0.86m (thickness 0.06m)
        add_box(bm, (0.0, 0.0, 0.83), (1.80, 0.80, 0.06), m["wood"])

        # Legs & frame
        for lx in [-0.84, 0.84]:
            for ly in [-0.34, 0.34]:
                add_box(bm, (lx, ly, 0.40), (0.08, 0.08, 0.80), m["darkwood"])
            add_box(bm, (lx, 0.00, 0.12), (0.06, 0.60, 0.05), m["darkwood"])
        add_box(bm, (0.0, 0.34, 0.12), (1.60, 0.06, 0.05), m["darkwood"])

        # Drawer pedestals & center drawer
        add_box(bm, (-0.52, 0.0, 0.50), (0.48, 0.68, 0.58), m["wood"])
        add_box(bm, ( 0.52, 0.0, 0.50), (0.48, 0.68, 0.58), m["wood"])
        add_box(bm, ( 0.00, 0.0, 0.74), (0.54, 0.68, 0.10), m["wood"])

        # Brass drawer handles (facing -Y)
        for hz in [0.30, 0.48, 0.66]:
            add_box(bm, (-0.52, -0.355, hz), (0.12, 0.03, 0.03), m["brass"])
            add_box(bm, ( 0.52, -0.355, hz), (0.12, 0.03, 0.03), m["brass"])
        add_box(bm, (0.00, -0.355, 0.74), (0.12, 0.03, 0.03), m["brass"])
    return setup_root("Desk", mat_names, materials, populate)

def build_plant(materials):
    mat_names = ["wood", "darkwood", "jade"]
    def populate(bm, m):
        # 10-sided terracotta pot (0..0.34m)
        add_cone_z(bm, (0.0, 0.0, 0.14), 0.14, 0.21, 0.28, segments=10, mat_idx=m["wood"])
        add_cone_z(bm, (0.0, 0.0, 0.31), 0.22, 0.22, 0.06, segments=10, mat_idx=m["wood"])
        add_cone_z(bm, (0.0, 0.0, 0.30), 0.19, 0.19, 0.02, segments=10, mat_idx=m["darkwood"])

        # Central stem & tall upright leaf reaching 0.70m
        add_cone_z(bm, (0.0, 0.0, 0.44), 0.015, 0.012, 0.26, segments=6, mat_idx=m["darkwood"])
        add_creased_leaf(
            bm,
            Vector((0.0, -0.01, 0.54)),
            Vector((0.0, 0.01, 0.70)),
            Vector((-0.07, 0.0, 0.62)),
            Vector((0.07, 0.0, 0.62)),
            crease_up=0.02,
            mat_idx=m["jade"]
        )

        # Spreading lowpoly leaves on stems
        for deg in [0, 50, 110, 160, 220, 280, 330]:
            rad = math.radians(deg)
            dx, dy = math.cos(rad), math.sin(rad)
            sx, sy, sz = dx * 0.14, dy * 0.14, 0.44
            add_box(bm, (sx * 0.6, sy * 0.6, 0.37), (0.02, 0.02, 0.14), m["darkwood"])
            base = Vector((sx, sy, sz))
            tip = Vector((dx * 0.26, dy * 0.26, 0.42))
            side1 = Vector((dx * 0.18 - dy * 0.07, dy * 0.18 + dx * 0.07, 0.49))
            side2 = Vector((dx * 0.18 + dy * 0.07, dy * 0.18 - dx * 0.07, 0.49))
            add_creased_leaf(bm, base, tip, side1, side2, crease_up=0.025, mat_idx=m["jade"])
    return setup_root("Plant", mat_names, materials, populate)

def render_preview(roots, preview_path):
    # Preview layout: all six roots neatly arranged
    preview_offsets = {
        "Lantern":    (-3.6,  0.2, 0.0),
        "Clockmaker": (-2.0, -1.2, 0.0),
        "Desk":       ( 0.4, -1.0, 0.0),
        "Plant":      ( 2.4, -1.2, 0.0),
        "MilkCart":   (-0.8,  1.4, 0.0),
        "Bench":      ( 2.5,  1.4, 0.0),
    }

    # Offset roots for preview display
    for name, root_obj in roots.items():
        if name in preview_offsets:
            root_obj.location = preview_offsets[name]

    # Neutral slate floor
    slate_mat = bpy.data.materials.new("slate_floor")
    slate_mat.diffuse_color = hex_to_linear("#2E353C")
    if slate_mat.node_tree:
        bsdf = slate_mat.node_tree.nodes.get("Principled BSDF")
        if bsdf:
            bsdf.inputs["Base Color"].default_value = hex_to_linear("#2E353C")
            bsdf.inputs["Roughness"].default_value = 0.85

    floor_mesh = bpy.data.meshes.new("Floor")
    floor_mesh.materials.append(slate_mat)
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=24.0)
    bm.to_mesh(floor_mesh)
    bm.free()
    floor_obj = bpy.data.objects.new("Floor", floor_mesh)
    bpy.context.scene.collection.objects.link(floor_obj)
    floor_obj.location = (0.0, 0.0, -0.001)

    # Area lights (muted blue-gray twilight key + warm amber accent)
    def make_light(name, pos, target, color, energy, size):
        ldata = bpy.data.lights.new(name, "AREA")
        ldata.color = color
        ldata.energy = energy
        ldata.size = size
        lobj = bpy.data.objects.new(name, ldata)
        bpy.context.scene.collection.objects.link(lobj)
        lobj.location = Vector(pos)
        lobj.rotation_euler = (Vector(target) - lobj.location).to_track_quat("-Z", "Y").to_euler()
        return lobj

    key_light = make_light("KeyLight", (7.0, -9.0, 8.5), (0.0, 0.0, 1.2), (0.78, 0.86, 1.0), 750, 6.0)
    fill_light = make_light("FillLight", (-7.0, -5.0, 6.5), (0.0, 0.0, 1.2), (1.0, 0.78, 0.52), 450, 5.0)
    rim_light = make_light("RimLight", (0.0, 7.0, 7.0), (0.0, 0.0, 1.2), (0.75, 0.82, 0.95), 250, 6.0)

    # Orthographic camera framed to capture all assets including the 3.5m lantern
    cdata = bpy.data.cameras.new("PreviewCamera")
    cdata.type = "ORTHO"
    cdata.ortho_scale = 13.0
    cam = bpy.data.objects.new("PreviewCamera", cdata)
    bpy.context.scene.collection.objects.link(cam)
    cam.location = Vector((9.0, -12.5, 9.0))
    cam.rotation_euler = (Vector((0.0, 0.0, 1.4)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = cam

    # Render settings
    scene = bpy.context.scene
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.filepath = str(preview_path.resolve())
    scene.render.image_settings.file_format = "PNG"

    bpy.ops.render.render(write_still=True)

    # Clean up preview helpers
    for obj in [floor_obj, key_light, fill_light, rim_light, cam]:
        bpy.data.objects.remove(obj, do_unlink=True)

    # Restore root positions before saving / exporting
    for root_obj in roots.values():
        root_obj.location = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()

def main():
    args = sys.argv
    script_args = args[args.index("--") + 1:] if "--" in args else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default="kit-out")
    parsed = parser.parse_args(script_args)

    out_dir = Path(parsed.out).resolve()
    models_dir = out_dir / "Models"
    models_dir.mkdir(parents=True, exist_ok=True)

    # Clean factory scene
    bpy.ops.wm.read_factory_settings(use_empty=True)

    materials = create_materials()

    builders = [
        ("Lantern", build_lantern),
        ("Bench", build_bench),
        ("MilkCart", build_milk_cart),
        ("Clockmaker", build_clockmaker),
        ("Desk", build_desk),
        ("Plant", build_plant),
    ]

    roots = {}
    mesh_objs = {}
    manifest_assets = []

    for name, builder_fn in builders:
        root, mesh_obj = builder_fn(materials)
        roots[name] = root
        mesh_objs[name] = mesh_obj

    # Render preview
    preview_path = out_dir / "preview.png"
    render_preview(roots, preview_path)

    # Verify roots are restored to (0,0,0) and collect dimensions
    verification = {}
    for name, mesh_obj in mesh_objs.items():
        root = roots[name]
        dims = mesh_obj.dimensions
        verification[name] = {
            "root_at_origin": list(root.location) == [0.0, 0.0, 0.0],
            "dimensions": [round(dims.x, 3), round(dims.y, 3), round(dims.z, 3)],
            "positive_dimensions": dims.x > 0 and dims.y > 0 and dims.z > 0,
            "vertices": len(mesh_obj.data.vertices),
            "polygons": len(mesh_obj.data.polygons),
        }

    # Save source kit .blend retaining named collections
    blend_path = out_dir / "kit.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))

    # Export individual FBX assets
    fbx_files = []
    for name, root in roots.items():
        mesh_obj = mesh_objs[name]
        bpy.ops.object.select_all(action="DESELECT")
        root.select_set(True)
        mesh_obj.select_set(True)
        bpy.context.view_layer.objects.active = root

        fbx_path = models_dir / f"{name}.fbx"
        bpy.ops.export_scene.fbx(
            filepath=str(fbx_path),
            use_selection=True,
            axis_forward="-Z",
            axis_up="Y",
            add_leaf_bones=False,
        )
        fbx_files.append(str(fbx_path.relative_to(out_dir)))
        manifest_assets.append({
            "name": name,
            "root": root.name,
            "mesh": mesh_obj.name,
            "file": f"Models/{name}.fbx",
            "materials": [slot.material.name for slot in mesh_obj.material_slots if slot.material],
            "dimensions_m": verification[name]["dimensions"],
            "vertices": verification[name]["vertices"],
            "polygons": verification[name]["polygons"],
        })

    # Generate manifest.json
    manifest = {
        "palette": PALETTE,
        "export_settings": {
            "axis_forward": "-Z",
            "axis_up": "Y",
            "use_selection": True,
            "add_leaf_bones": False,
        },
        "assets": manifest_assets,
        "files": [
            "kit.blend",
            "preview.png",
            "manifest.json",
        ] + [f"Models/{name}.fbx" for name in roots],
        "verification": verification,
    }

    manifest_path = out_dir / "manifest.json"
    with open(manifest_path, "w", encoding="utf-8") as f:
        json.dump(manifest, f, indent=2)

    # Verification checks
    all_fbx_ok = all((models_dir / f"{name}.fbx").is_file() and (models_dir / f"{name}.fbx").stat().st_size > 0 for name in roots)
    blend_ok = blend_path.is_file() and blend_path.stat().st_size > 0
    preview_ok = preview_path.is_file() and preview_path.stat().st_size > 0
    dims_ok = all(v["positive_dimensions"] for v in verification.values())

    print(json.dumps({
        "status": "success",
        "output_directory": str(out_dir),
        "blend_file": str(blend_path.name),
        "manifest": str(manifest_path.name),
        "preview": str(preview_path.name),
        "fbx_models": [f"{name}.fbx" for name in roots],
        "verification": {
            "fbx_files_exported": all_fbx_ok,
            "blend_saved": blend_ok,
            "preview_rendered": preview_ok,
            "positive_dimensions": dims_ok,
            "asset_details": verification,
        }
    }, indent=2))

if __name__ == "__main__":
    main()
