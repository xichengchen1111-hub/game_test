"""Editable, metre-scale wooden side chair for the watchmaker's supplied seated loop.

blender --background --python-exit-code 1 --python tools/build_watchmaker_chair.py -- --root <repo>
"""
import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def material(name, hex_color):
    values = [int(hex_color[i:i+2], 16) / 255 for i in (0, 2, 4)]
    values = [v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in values]
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*values, 1)
    m.use_nodes = True
    shader = m.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = m.diffuse_color
    shader.inputs['Roughness'].default_value = .8
    return m


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--root', type=Path, required=True)
    args = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
    root = args.root.resolve()
    source = root / 'assets/source/watchmaker/chair'
    unity = root / 'game/Assets/Art/WatchmakerChair'
    review = root / 'output/demo/watchmaker-meshy'
    for folder in (source, unity, review):
        folder.mkdir(parents=True, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    scene.unit_settings.system = 'METRIC'
    kit = bpy.data.collections.new('WatchmakerChair')
    scene.collection.children.link(kit)
    wood = material('ChairWood', '69503B')
    leather = material('ChairLeather', '473C32')
    parts = []

    def box(name, location, size, mat=wood, bevel=.008):
        bpy.ops.mesh.primitive_cube_add(size=1, location=location)
        obj = bpy.context.object
        obj.name = name
        obj.dimensions = size
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        for col in list(obj.users_collection):
            col.objects.unlink(obj)
        kit.objects.link(obj)
        obj.data.materials.append(mat)
        if bevel:
            modifier = obj.modifiers.new('Small worn edge', 'BEVEL')
            modifier.width = bevel
            modifier.segments = 1
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        parts.append(obj)
        return obj

    # Local front is -Y in Blender. Unity derives its yaw from the imported back rail.
    box('Chair timber seat', (0, 0, .345), (.60, .59, .05), bevel=.015)
    box('ChairSeat', (0, -.008, .385), (.57, .55, .03), leather, .012)  # top=.40
    for x in (-.255, .255):
        for y in (-.245, .245):
            box(('Front' if y < 0 else 'Rear') + (' left leg' if x < 0 else ' right leg'),
                (x, y, .165), (.055, .055, .33))
        box('Side apron', (x, 0, .295), (.045, .49, .10))
        box('Side stretcher', (x, 0, .14), (.035, .49, .035))
        box('Back upright', (x, .285, .64), (.055, .055, .58))
    for y in (-.245, .245):
        box('Seat apron', (0, y, .295), (.51, .045, .10))
    box('Lower cross stretcher', (0, 0, .14), (.51, .035, .035))
    box('Back top rail', (0, .285, .945), (.54, .065, .07), bevel=.015)
    box('Back lower rail', (0, .285, .56), (.51, .04, .05))
    for x in (-.16, 0, .16):
        box('Back slat', (x, .285, .75), (.065, .035, .34), bevel=.008)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.export_scene.fbx(filepath=str(unity / 'WatchmakerChair.fbx'), use_selection=True,
        object_types={'MESH'}, axis_forward='-Z', axis_up='Y', bake_anim=False, path_mode='STRIP')

    preview = bpy.data.collections.new('ReviewOnly')
    scene.collection.children.link(preview)
    camera = bpy.data.objects.new('Chair review camera', bpy.data.cameras.new('Chair review camera'))
    preview.objects.link(camera)
    camera.location = (1.5, -2.3, 1.7)
    camera.rotation_euler = (Vector((0, 0, .51)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = 1.4
    scene.camera = camera
    scene.world = bpy.data.worlds.new('Chair review world')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.13, .15, .20, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .6
    light = bpy.data.lights.new('Chair review light', 'AREA')
    light.energy, light.size = 300, 3
    obj = bpy.data.objects.new('Chair review light', light)
    preview.objects.link(obj)
    obj.location = (2, -3, 4)
    obj.rotation_euler = (Vector((0, 0, .5)) - obj.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.resolution_x = scene.render.resolution_y = 700
    scene.render.resolution_percentage = 100
    scene.render.filepath = str(review / 'model-chair.png')
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(source / 'WatchmakerChair.blend'), compress=True)
    bpy.ops.wm.open_mainfile(filepath=str(source / 'WatchmakerChair.blend'))
    parts = list(bpy.data.collections['WatchmakerChair'].objects)
    points = [obj.matrix_world @ Vector(p) for obj in parts for p in obj.bound_box]
    low = [min(p[i] for p in points) for i in range(3)]
    high = [max(p[i] for p in points) for i in range(3)]
    report = {'blender': bpy.app.version_string, 'units': 'metres', 'front': 'Blender -Y; Unity yaw derived from imported back rail',
        'parts': len(parts), 'vertices': sum(len(o.data.vertices) for o in parts),
        'boundsMin': low, 'boundsMax': high, 'seatTop': .40,
        'materials': sorted({m.name for o in parts for m in o.data.materials}),
        'provenance': 'Original procedural low-poly wooden side chair; no external textures',
        'sourceReopened': True, 'unityRuntimeChecked': False}
    assert abs(low[2]) < .001 and len(parts) == 20
    (source / 'chair-manifest.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('WATCHMAKER_CHAIR_PASS', json.dumps(report))


if __name__ == '__main__':
    main()
