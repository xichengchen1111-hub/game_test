"""Import the user-supplied watchmaker, preserving one mesh and all three actions.

blender --background --python-exit-code 1 --python import_meshy_watchmaker.py --
    --archive <supplied.zip> --root <repository_or_staged_root>
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct
import sys
import tempfile
import zipfile

import bpy
from mathutils import Vector
import numpy as np


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def file_digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def glb_images(data: bytes) -> dict:
    length, kind = struct.unpack_from('<II', data, 12)
    assert kind == 0x4E4F534A
    document = json.loads(data[20:20 + length])
    offset = 20 + length
    bin_length, kind = struct.unpack_from('<II', data, offset)
    assert kind == 0x004E4942
    buffer = data[offset + 8:offset + 8 + bin_length]
    result = {}
    for image in document.get('images', []):
        view = document['bufferViews'][image['bufferView']]
        start = view.get('byteOffset', 0)
        result[image['name']] = buffer[start:start + view['byteLength']]
    return result


def set_action(rig, action):
    rig.animation_data_create()
    rig.animation_data.action = action
    if hasattr(action, 'slots') and action.slots:
        rig.animation_data.action_slot = action.slots[0]


def bounds(mesh):
    evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    points = [evaluated.matrix_world @ Vector(p) for p in evaluated.bound_box]
    return Vector([min(p[i] for p in points) for i in range(3)]), Vector([max(p[i] for p in points) for i in range(3)])


def inspect_seated_animation(rig, mesh, action):
    """Deep inspection of the seated animation over all frames and sample frames."""
    bones_to_track = [
        'mixamorig:Hips',
        'mixamorig:Head',
        'mixamorig:LeftUpLeg',
        'mixamorig:RightUpLeg',
        'mixamorig:LeftFoot',
        'mixamorig:RightFoot',
    ]

    # 1. Rest bounds & bone positions
    rig.data.pose_position = 'REST'
    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_mesh = mesh.evaluated_get(depsgraph)
    rest_pts = [eval_mesh.matrix_world @ Vector(pt) for pt in eval_mesh.bound_box]
    rest_min = Vector([min(p[i] for p in rest_pts) for i in range(3)])
    rest_max = Vector([max(p[i] for p in rest_pts) for i in range(3)])

    rest_bone_pos = {}
    for bname in bones_to_track:
        b = rig.pose.bones[bname]
        rest_bone_pos[bname] = list(rig.matrix_world @ b.matrix.translation)

    # 2. Seated pose inspection
    rig.data.pose_position = 'POSE'
    bpy.context.view_layer.update()
    set_action(rig, action)

    start_frame, end_frame = (float(f) for f in action.frame_range)
    sample_frames = [start_frame + (end_frame-start_frame)*i/9 for i in range(10)]
    all_frames = list(range(math.ceil(start_frame), math.floor(end_frame)+1)) + sample_frames
    all_frames = sorted(set(all_frames))
    def frame_set(frame):
        scene.frame_set(math.floor(frame), subframe=frame-math.floor(frame))

    frame_samples = []
    all_hip_pos = []
    all_head_pos = []
    all_eval_min = []
    all_eval_max = []

    scene = bpy.context.scene
    support_samples = []
    for f in all_frames:
        frame_set(f)
        depsgraph = bpy.context.evaluated_depsgraph_get()
        eval_mesh = mesh.evaluated_get(depsgraph)
        pts = [eval_mesh.matrix_world @ Vector(pt) for pt in eval_mesh.bound_box]
        cur_min = Vector([min(p[i] for p in pts) for i in range(3)])
        cur_max = Vector([max(p[i] for p in pts) for i in range(3)])

        all_eval_min.append(cur_min)
        all_eval_max.append(cur_max)

        hip_wpos = rig.matrix_world @ rig.pose.bones['mixamorig:Hips'].matrix.translation
        head_wpos = rig.matrix_world @ rig.pose.bones['mixamorig:Head'].matrix.translation
        all_hip_pos.append(hip_wpos)
        all_head_pos.append(head_wpos)

        if f in sample_frames:
            posed = eval_mesh.to_mesh()
            vertices = [eval_mesh.matrix_world @ v.co for v in posed.vertices]
            contact = [v.z for v in vertices if abs(v.x-hip_wpos.x)<.20 and -.06<v.y-hip_wpos.y<.22]
            support_samples.append({'frame': f, 'supportZ': min(contact), 'hipZ': hip_wpos.z,
                                    'soleZ': min(v.z for v in vertices)})
            eval_mesh.to_mesh_clear()
            sample_data = {
                'frame': f,
                'boundsMin': list(cur_min),
                'boundsMax': list(cur_max),
                'bones': {bname: list(rig.matrix_world @ rig.pose.bones[bname].matrix.translation) for bname in bones_to_track}
            }
            frame_samples.append(sample_data)

    hip_min = Vector([min(p[i] for p in all_hip_pos) for i in range(3)])
    hip_max = Vector([max(p[i] for p in all_hip_pos) for i in range(3)])
    head_min = Vector([min(p[i] for p in all_head_pos) for i in range(3)])
    head_max = Vector([max(p[i] for p in all_head_pos) for i in range(3)])
    eval_overall_min = Vector([min(p[i] for p in all_eval_min) for i in range(3)])
    eval_overall_max = Vector([max(p[i] for p in all_eval_max) for i in range(3)])

    # Initial vs final frame pose distance
    frame_set(start_frame)
    f0_bones = {bname: rig.matrix_world @ rig.pose.bones[bname].matrix.translation for bname in bones_to_track}
    frame_set(end_frame)
    f1_bones = {bname: rig.matrix_world @ rig.pose.bones[bname].matrix.translation for bname in bones_to_track}
    diffs = {bname: float((f1_bones[bname] - f0_bones[bname]).length) for bname in bones_to_track}

    # Evaluate pelvic contact surface at frame 0
    frame_set(start_frame)
    depsgraph = bpy.context.evaluated_depsgraph_get()
    eval_mesh = mesh.evaluated_get(depsgraph)
    temp_mesh = eval_mesh.to_mesh()
    verts_world = [eval_mesh.matrix_world @ v.co for v in temp_mesh.vertices]
    hip_f0 = f0_bones['mixamorig:Hips']
    pelvis_verts = [v for v in verts_world if abs(v.x - hip_f0.x) < .20 and -.06 < v.y - hip_f0.y < .22]
    assert pelvis_verts, 'No pelvis contact mesh found'
    pelvis_lowest_z = min(v.z for v in pelvis_verts)
    eval_mesh.to_mesh_clear()

    # Did animation stand or transition?
    stands_or_transitions = bool(hip_max.z-hip_min.z>.10 or hip_max.z>rest_bone_pos['mixamorig:Hips'][2]-.15)
    assert not stands_or_transitions, 'Supplied clip must remain seated'

    return {
        'actionName': action.name,
        'evaluatedSamples': len(all_frames),
        'seconds': (end_frame-start_frame)/scene.render.fps,
        'frameRange': [start_frame, end_frame],
        'restBoundsMin': list(rest_min),
        'restBoundsMax': list(rest_max),
        'restBoundsSize': list(rest_max - rest_min),
        'restBonePositions': rest_bone_pos,
        'overallEvaluatedBoundsMin': list(eval_overall_min),
        'overallEvaluatedBoundsMax': list(eval_overall_max),
        'rootMotionHipsRange': {
            'min': list(hip_min),
            'max': list(hip_max),
            'delta': list(hip_max - hip_min),
        },
        'headRange': {
            'min': list(head_min),
            'max': list(head_max),
            'delta': list(head_max - head_min),
        },
        'initialVsFinalPoseBoneDistance': diffs,
        'standsOrTransitions': stands_or_transitions,
        'trackedBoneNames': bones_to_track,
        'pelvisLowestZ': pelvis_lowest_z,
        'seatSupportSamples': support_samples,
        'sampleFrames': frame_samples,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--root', type=Path, required=True)
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    root = args.root.resolve()

    source = root / 'assets/source/watchmaker/meshy'
    unity = root / 'game/Assets/Art/WatchmakerMeshy'
    review = root / 'output/demo/watchmaker-meshy'
    for folder in (source, unity, review):
        folder.mkdir(parents=True, exist_ok=True)

    archive_data = args.archive.read_bytes()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.preferences.filepaths.save_version = 0
    scene = bpy.context.scene
    scene.render.fps = 30
    actions = []
    entries = []
    rig = mesh = None

    with zipfile.ZipFile(args.archive) as archive, tempfile.TemporaryDirectory(prefix='watchmaker-') as temp:
        names = sorted(n for n in archive.namelist() if n.lower().endswith('.glb'))
        assert len(names) == 3, f'Expected three supplied GLBs, got {len(names)}'
        # Prioritize Sit_Cross_Legged as the primary default action and model base
        names.sort(key=lambda n: 'Sit_Cross_Legged' not in n)

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
            assert len(imported_meshes) == 1, f'Expected 1 skinned mesh, found {len(imported_meshes)}'
            action = next(iter(set(bpy.data.actions) - old_actions))
            action.name = Path(name).stem.split('_Animation_')[1].removesuffix('_withSkin')
            action.use_fake_user = True
            actions.append(action)
            entries.append({
                'entry': name,
                'sha256': digest(data),
                'action': action.name,
                'frames': list(action.frame_range)
            })

            if index == 0:
                rig, mesh = imported_rig, imported_meshes[0]
                # Save original seated GLB
                (source / 'WatchmakerSeated.glb').write_bytes(data)

                # Extract textures
                maps = glb_images(data)
                for image_name, filename in [
                    ('texture_0', 'BaseColor.png'),
                    ('normal', 'Normal.png'),
                    ('texture_0_metallic_roughness', 'MetallicRoughness.png')
                ]:
                    (source / filename).write_bytes(maps[image_name])

                for filename in ('BaseColor.png', 'Normal.png'):
                    (unity / filename).write_bytes((source / filename).read_bytes())

                # glTF G=roughness/B=metallic -> URP RGB=metallic/A=smoothness
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

                # Remove non-character objects (such as Icosphere helper)
                for obj in objects - {rig, mesh}:
                    bpy.data.objects.remove(obj, do_unlink=True)
            else:
                assert list(imported_rig.data.bones.keys()) == list(rig.data.bones.keys()), 'Different animation rig'
                for obj in objects:
                    bpy.data.objects.remove(obj, do_unlink=True)

    # Clean unreferenced data blocks
    for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)

    assert len(rig.data.bones) == 37 and len(bpy.data.actions) == 3

    # Named collections for character/review
    for obj in (rig, mesh):
        if obj.name in scene.collection.objects:
            scene.collection.objects.unlink(obj)

    char_coll = bpy.data.collections.new('Character')
    scene.collection.children.link(char_coll)
    char_coll.objects.link(rig)
    char_coll.objects.link(mesh)

    rev_coll = bpy.data.collections.new('Review')
    scene.collection.children.link(rev_coll)

    # Inspect the seated animation
    seated_analysis = inspect_seated_animation(rig, mesh, actions[0])

    # Set seated action active and frame 0
    set_action(rig, actions[0])
    scene.frame_start, scene.frame_end = (int(f) for f in actions[0].frame_range)
    scene.frame_set(scene.frame_start)

    # Ensure textures are packed and references are relative (not absolute scratch paths)
    for image in list(bpy.data.images):
        if image.name in {'texture_0', 'normal', 'texture_0_metallic_roughness'}:
            filename = {
                'texture_0': 'BaseColor.png',
                'normal': 'Normal.png',
                'texture_0_metallic_roughness': 'MetallicRoughness.png'
            }[image.name]
            image.filepath = str(source / filename)
            image.pack()
            image.filepath = '//' + filename
        elif image.type != 'IMAGE':
            bpy.data.images.remove(image)

    # Export FBX using standard glTF-to-FBX axis pattern (-Z forward, Y up)
    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=str(unity / 'Watchmaker.fbx'),
        use_selection=True,
        object_types={'ARMATURE', 'MESH'},
        axis_forward='-Z',
        axis_up='Y',
        add_leaf_bones=False,
        use_armature_deform_only=False,
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_step=1,
        bake_anim_simplify_factor=0,
        path_mode='STRIP'
    )

    # Setup Review lighting and camera
    low, high = bounds(mesh)
    centre = (low + high) / 2
    height = high.z - low.z

    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 16
    scene.cycles.use_denoising = True
    scene.render.resolution_x, scene.render.resolution_y = 800, 1000
    scene.render.resolution_percentage = 100

    scene.world = bpy.data.worlds.new('ReviewWorld')
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.14, 0.16, 0.20, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.5

    for name, position, power, size in [
        ('Key', (2, -3, 4), 350, 3),
        ('Fill', (-2, -1, 2.5), 180, 3),
        ('Rim', (0, 2, 3), 250, 2)
    ]:
        light = bpy.data.lights.new(name, 'AREA')
        light.energy, light.shape, light.size = power, 'DISK', size
        obj = bpy.data.objects.new(name, light)
        rev_coll.objects.link(obj)
        obj.location = position
        obj.rotation_euler = (centre - obj.location).to_track_quat('-Z', 'Y').to_euler()

    camera = bpy.data.objects.new('ReviewCamera', bpy.data.cameras.new('ReviewCamera'))
    rev_coll.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'

    renders = [
        ('full', Vector((1.2, -3.2, centre.z + 0.2)), centre, height * 1.25),
        ('side', Vector((3.2, centre.y, centre.z)), centre, height * 1.25),
        ('seated', Vector((0.8, -2.2, 0.45)), Vector((centre.x, centre.y, 0.35)), 0.95),
    ]

    for name, pos, target, scale in renders:
        camera.location = pos
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera.data.ortho_scale = scale
        scene.render.filepath = str(review / f'model-{name}.png')
        bpy.ops.render.render(write_still=True)

    # Clean non-image images before saving blend
    for image in list(bpy.data.images):
        if image.type != 'IMAGE':
            bpy.data.images.remove(image)

    blend_path = source / 'Watchmaker.blend'
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path), compress=True)

    # Reopen to verify source files and packed state
    bpy.ops.wm.open_mainfile(filepath=str(blend_path))

    reopened_char = [o for o in bpy.context.scene.objects if o.type == 'MESH' and any(m.type == 'ARMATURE' for m in o.modifiers)]
    reopened_rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    assert len(reopened_char) == 1, 'Expected 1 skinned mesh on reopen'
    assert len(reopened_rig.data.bones) == 37, 'Expected 37 bones on reopen'
    assert len(bpy.data.actions) == 3, 'Expected 3 actions on reopen'
    assert len(bpy.data.images) == 3, 'Expected 3 images on reopen'
    assert all(img.packed_file is not None for img in bpy.data.images), 'Expected all images packed'
    assert all(img.filepath.startswith('//') for img in bpy.data.images), 'Expected relative image paths'
    assert 'Character' in bpy.data.collections and 'Review' in bpy.data.collections, 'Expected named collections'

    report = {
        'blender': bpy.app.version_string,
        'archive': args.archive.name,
        'archiveSha256': digest(archive_data),
        'provenance': 'User-supplied Meshy asset; no license document in archive',
        'animations': entries,
        'skinnedMeshes': len(reopened_char),
        'vertices': len(reopened_char[0].data.vertices),
        'packedMaps': len(bpy.data.images),
        'boundsMin': list(low),
        'boundsMax': list(high),
        'fps': bpy.context.scene.render.fps,
        'texturePacking': 'URP RGB=glTF B (metallic), alpha=1-glTF G (roughness)',
        'unityRuntimeChecked': False,
        'bones': len(reopened_rig.data.bones),
        'sourceBlendSha256': file_digest(blend_path),
        'seatedGlbSha256': file_digest(source / 'WatchmakerSeated.glb'),
        'unityFbxSha256': file_digest(unity / 'Watchmaker.fbx'),
        'seatedAnalysis': seated_analysis,
    }

    report_text = json.dumps(report, indent=2) + '\n'
    for out_path in (source / 'import-results.json', review / 'import-results.json'):
        out_path.write_text(report_text, encoding='utf-8')

    print('MESHY_WATCHMAKER_IMPORT_PASS', json.dumps({
        'archive': report['archive'],
        'bones': report['bones'],
        'vertices': report['vertices'],
        'actions': len(report['animations']),
        'packedMaps': report['packedMaps'],
        'seatedLowestPelvisZ': seated_analysis['pelvisLowestZ'],
        'standsOrTransitions': seated_analysis['standsOrTransitions'],
    }))


if __name__ == '__main__':
    main()
