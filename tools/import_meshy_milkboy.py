"""Import the user-supplied messenger, preserving one mesh and all six actions.

blender --background --python-exit-code 1 --python tools/import_meshy_milkboy.py --
    --archive <supplied.zip> --root <repository>
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import tempfile
import zipfile

import bpy
from mathutils import Vector
import numpy as np


def digest(data):
    return hashlib.sha256(data).hexdigest()


def glb_images(data):
    length, kind = struct.unpack_from('<II', data, 12)
    assert kind == 0x4E4F534A
    document = json.loads(data[20:20 + length])
    offset = 20 + length
    bin_length, kind = struct.unpack_from('<II', data, offset)
    assert kind == 0x004E4942
    buffer = data[offset + 8:offset + 8 + bin_length]
    result = {}
    for image in document['images']:
        view = document['bufferViews'][image['bufferView']]
        start = view.get('byteOffset', 0)
        result[image['name']] = buffer[start:start + view['byteLength']]
    return result


def set_action(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]


def bounds(mesh):
    evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [evaluated.matrix_world @ Vector(p) for p in evaluated.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--root', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    root = args.root.resolve()
    source = root / 'assets/source/milkboy/meshy'
    unity = root / 'game/Assets/Art/MilkboyMeshy'
    review = root / 'output/demo/milkboy-meshy'
    for folder in (source, unity, review):
        folder.mkdir(parents=True, exist_ok=True)
    archive_data = args.archive.read_bytes()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.fps = 30
    actions = []
    entries = []
    rig = mesh = None
    with zipfile.ZipFile(args.archive) as archive, tempfile.TemporaryDirectory(prefix='messenger-') as temp:
        names = sorted(n for n in archive.namelist() if n.lower().endswith('.glb'))
        assert len(names) == 6, 'Expected six supplied GLBs'
        names.sort(key=lambda n: 'Short_Breathe_and_Look_Around' not in n)
        for index, name in enumerate(names):
            data = archive.read(name)
            path = Path(temp) / Path(name).name
            path.write_bytes(data)
            old_objects = set(bpy.data.objects)
            old_actions = set(bpy.data.actions)
            bpy.ops.import_scene.gltf(filepath=str(path))
            objects = set(bpy.data.objects) - old_objects
            imported_rig = next(o for o in objects if o.type == 'ARMATURE')
            imported_meshes = [o for o in objects if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
            assert len(imported_meshes) == 1
            action = next(iter(set(bpy.data.actions) - old_actions))
            action.name = Path(name).stem.split('_Animation_')[1].removesuffix('_withSkin')
            action.use_fake_user = True
            actions.append(action)
            entries.append({'entry': name, 'sha256': digest(data), 'action': action.name, 'frames': list(action.frame_range)})
            if index == 0:
                rig, mesh = imported_rig, imported_meshes[0]
                (source / 'MessengerIdle.glb').write_bytes(data)
                maps = glb_images(data)
                for image_name, filename in [('texture_0', 'BaseColor.png'), ('normal', 'Normal.png'), ('texture_0_metallic_roughness', 'MetallicRoughness.png')]:
                    (source / filename).write_bytes(maps[image_name])
                for filename in ('BaseColor.png', 'Normal.png'):
                    (unity / filename).write_bytes((source / filename).read_bytes())
                # glTF G=roughness/B=metallic -> URP RGB=metallic/A=smoothness.
                packed_source = bpy.data.images.load(str(source / 'MetallicRoughness.png'), check_existing=False)
                packed_source.colorspace_settings.name = 'Non-Color'
                pixels = np.empty(len(packed_source.pixels), dtype=np.float32)
                packed_source.pixels.foreach_get(pixels)
                pixels = pixels.reshape(-1, 4)
                output = np.empty_like(pixels)
                output[:, :3] = pixels[:, 2:3]
                output[:, 3] = 1 - pixels[:, 1]
                packed = bpy.data.images.new('UnityMetallicSmoothness', *packed_source.size, alpha=True)
                packed.colorspace_settings.name = 'Non-Color'
                packed.pixels.foreach_set(output.ravel())
                packed.filepath_raw = str(unity / 'MetallicSmoothness.png')
                packed.file_format = 'PNG'
                packed.save()
                bpy.data.images.remove(packed)
                bpy.data.images.remove(packed_source)
                for obj in objects - {rig, mesh}:
                    bpy.data.objects.remove(obj, do_unlink=True)
            else:
                assert list(imported_rig.data.bones.keys()) == list(rig.data.bones.keys()), 'Different animation rig'
                for obj in objects:
                    bpy.data.objects.remove(obj, do_unlink=True)
    # Remove duplicate unused imported data; keep only the primary material's images.
    for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)
    assert len(rig.data.bones) == 66 and len(bpy.data.actions) == 6
    set_action(rig, actions[0])
    scene.frame_start, scene.frame_end = (int(f) for f in actions[0].frame_range)
    scene.frame_set(scene.frame_start)
    for image in bpy.data.images:
        filename = {'texture_0': 'BaseColor.png', 'normal': 'Normal.png', 'texture_0_metallic_roughness': 'MetallicRoughness.png'}[image.name]
        image.filepath = str(source / filename)
        image.pack()
        image.filepath = '//' + filename
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(unity / 'Messenger.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'},
                             axis_forward='-Z', axis_up='Y', add_leaf_bones=False, use_armature_deform_only=False,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_step=1, bake_anim_simplify_factor=0, path_mode='STRIP')
    low, high = bounds(mesh)
    centre = (low + high) / 2
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 800, 1000
    scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('ReviewWorld')
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.14, .16, .20, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .5
    for name, position, power, size in [('Key', (2, -3, 4), 350, 3), ('Fill', (-2, -1, 2.5), 180, 3), ('Rim', (0, 2, 3), 250, 2)]:
        light = bpy.data.lights.new(name, 'AREA')
        light.energy, light.shape, light.size = power, 'DISK', size
        obj = bpy.data.objects.new(name, light)
        scene.collection.objects.link(obj)
        obj.location = position
        obj.rotation_euler = (centre - obj.location).to_track_quat('-Z', 'Y').to_euler()
    camera = bpy.data.objects.new('ReviewCamera', bpy.data.cameras.new('ReviewCamera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'
    for name, position, target, size in [('full', (1, -3.5, 1.3), centre, (high.z-low.z)*1.16),
                                         ('face', (.35, -2, 1.4), Vector((0, -.05, 1.35)), .62),
                                         ('side', (3, -.15, 1.2), centre, (high.z-low.z)*1.16)]:
        camera.location = position
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera.data.ortho_scale = size
        scene.render.filepath = str(review / ('model-' + name + '.png'))
        bpy.ops.render.render(write_still=True)
    for image in list(bpy.data.images):
        if image.type != 'IMAGE':
            bpy.data.images.remove(image)
    bpy.ops.wm.save_as_mainfile(filepath=str(source / 'Messenger.blend'), compress=True)
    bpy.ops.wm.open_mainfile(filepath=str(source / 'Messenger.blend'))
    character = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
    assert len(character) == 1 and len(bpy.data.actions) == 6 and len(bpy.data.images) == 3
    assert all(image.packed_file for image in bpy.data.images)
    report = {'blender': bpy.app.version_string, 'archive': args.archive.name, 'archiveSha256': digest(archive_data),
              'provenance': 'User-supplied Meshy asset; no license document in archive', 'animations': entries,
              'skinnedMeshes': len(character),
              'vertices': len(character[0].data.vertices), 'packedMaps': len(bpy.data.images),
              'boundsMin': list(low), 'boundsMax': list(high), 'fps': bpy.context.scene.render.fps,
              'texturePacking': 'URP RGB=glTF B (metallic), alpha=1-glTF G (roughness)', 'unityRuntimeChecked': False}
    # Reopening invalidates prior bpy references.
    report['bones'] = len(next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE').data.bones)
    for path in (source / 'import-results.json', review / 'import-results.json'):
        path.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('MESHY_IMPORT_PASS', json.dumps(report))


if __name__ == '__main__':
    main()
