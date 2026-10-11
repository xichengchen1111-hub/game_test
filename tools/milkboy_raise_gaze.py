"""Blender: raise both approved milkboy actions, retaining hands, jaw and source assets."""
import bpy, math, json, pathlib, sys
from mathutils import Quaternion

root = pathlib.Path(sys.argv[sys.argv.index('--') + 1]).resolve()
source = root / 'assets/source/milkboy/tripo'
bpy.ops.wm.open_mainfile(filepath=str(source / 'Milkboy.blend'))
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
meshes = [o for o in bpy.data.objects if o.type == 'MESH']
scene = bpy.context.scene
actions = list(bpy.data.actions)
for track in list(arm.animation_data.nla_tracks):
    arm.animation_data.nla_tracks.remove(track)
report = {'revision': 'v5 head-up', 'actions': [], 'changedBones': ['Neck', 'Head'], 'jawAndHandCurvesPreserved': True}
for action in actions:
    arm.animation_data.action = action
    arm.animation_data.action_slot = action.slots[0]
    start, end = [round(n) for n in action.frame_range]
    for frame in range(start, end + 1):
        scene.frame_set(frame)
        phase = 2 * math.pi * (frame - start) / max(1, end - start)
        # Small, periodic glances; the head stays upright with an upward resting gaze.
        yaw = math.radians(3.0 * math.sin(phase))
        roll = math.radians(1.0 * math.sin(phase + .3))
        for name, pitch in [('Neck', 4.0), ('Head', 14.0)]:
            bone = arm.pose.bones[name]
            rotation = Quaternion((0, 0, 1), yaw) @ Quaternion((0, 1, 0), roll) @ Quaternion((1, 0, 0), math.radians(-pitch))
            matrix = (rotation.to_matrix() @ bone.bone.matrix_local.to_3x3()).to_4x4()
            matrix.translation = bone.head.copy()
            bone.matrix = matrix
            bpy.context.view_layer.update()
            bone.rotation_mode = 'QUATERNION'
            bone.keyframe_insert('rotation_quaternion', frame=frame)
    report['actions'].append({'name': action.name, 'frames': [start, end], 'restGazeUpDegrees': 14, 'glanceDegrees': 3, 'rollDegrees': 1})
scene.frame_set(1)
bpy.ops.wm.save_as_mainfile(filepath=str(source / 'Milkboy.blend'))
bpy.ops.object.select_all(action='DESELECT')
arm.select_set(True)
for mesh in meshes: mesh.select_set(True)
bpy.context.view_layer.objects.active = arm
arm.animation_data.action = None
for action in actions:
    track = arm.animation_data.nla_tracks.new()
    strip = track.strips.new(action.name, 1, action)
    strip.action_slot = action.slots[0]
bpy.ops.export_scene.fbx(filepath=str(source / 'Milkboy.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH'}, use_mesh_modifiers=False, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=True, bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='AUTO', embed_textures=False)
(root / 'game/Assets/Art/MilkboyTripo/Milkboy.fbx').write_bytes((source / 'Milkboy.fbx').read_bytes())
(source / 'head-up-refinement.json').write_text(json.dumps(report, indent=2))
print('MILKBOY_HEAD_UP_SOURCE_PASS', json.dumps(report), flush=True)
