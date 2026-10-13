"""Bake supplied Idle/Walk/Talk actions, preserving the original skin and face.

No retargeting or invented actions. Remove only horizontal net travel from the
sampled world pose; retain hip bob and all real leg/foot/facial-bone motion.
Report loop seams and foot heights instead of silently morphing the loop shut.
"""
import argparse
import hashlib
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

REQUIRED = ['Hips', 'Spine', 'Chest', 'Neck', 'Head'] + [s + '_' + b for s in ('Left', 'Right')
            for b in ('UpperArm', 'LowerArm', 'Hand', 'UpperLeg', 'LowerLeg', 'Foot')]
ALIASES = {'Pelvis': 'Hips', 'pelvis': 'Hips', 'Spine1': 'Chest', 'Spine2': 'UpperChest'}
for side in ('Left', 'Right'):
    for old, new in (('Arm', 'UpperArm'), ('ForeArm', 'LowerArm'), ('UpLeg', 'UpperLeg'), ('Leg', 'LowerLeg'),
                     ('Shoulder', 'Shoulder'), ('Hand', 'Hand'), ('Foot', 'Foot'), ('ToeBase', 'Toes')):
        ALIASES[side + old] = side + '_' + new
CLIPS = (('idle', '01_Idle'), ('walk', '02_Walk'), ('talk', '03_Talk'))


def canonical(name):
    name = name.rsplit(':', 1)[-1]
    return ALIASES.get(name, name).replace('Little', 'Pinky')


def assign_action(arm, action):
    arm.animation_data_create(); arm.animation_data.action = action
    if action and len(action.slots):
        slots = [s for s in action.slots if s.target_id_type == 'OBJECT']
        if len(slots) != 1:
            raise ValueError(f'Action {action.name!r} needs one armature OBJECT slot; found {len(slots)}.')
        arm.animation_data.action_slot = slots[0]


def curves(action):
    return [curve for layer in action.layers for strip in layer.strips for bag in strip.channelbags for curve in bag.fcurves]


def select_actions(actions, args):
    result = {}
    for kind, _ in CLIPS:
        explicit = getattr(args, kind + '_action')
        matches = [a for a in actions if a.name == explicit] if explicit else [a for a in actions if kind in a.name.lower()]
        if len(matches) != 1:
            raise ValueError(f'A real supplied {kind.upper()} action is required; specify --{kind}-action exactly. '
                             f'Available: {[a.name for a in actions]}. No action is synthesized from Idle.')
        result[kind] = matches[0]
    if len({a.as_pointer() for a in result.values()}) != 3:
        raise ValueError('Idle, Walk and Talk must be three distinct supplied actions.')
    return result


def frame(scene, value):
    whole = math.floor(value); scene.frame_set(whole, subframe=value - whole)
    bpy.context.view_layer.update()


def angle(a, b):
    radians = a.rotation_difference(b).angle
    return math.degrees(min(radians, math.tau - radians))


def walk_rates(distance, source_seconds, baked_seconds):
    needs_calibration = distance < .05
    return {'walkMetresPerSecond': 0 if needs_calibration else distance / baked_seconds,
            'sourceWalkMetresPerSecond': 0 if needs_calibration else distance / source_seconds,
            'calibrationRequired': needs_calibration}


def vertices(meshes):
    graph = bpy.context.evaluated_depsgraph_get()
    return {o.name: [o.evaluated_get(graph).matrix_world @ v.co for v in o.evaluated_get(graph).data.vertices] for o in meshes}


def signature(meshes):
    data = []
    for obj in sorted(meshes, key=lambda o: o.name):
        groups = {g.index: canonical(g.name) for g in obj.vertex_groups}
        data.append((obj.name, [tuple(v.co) for v in obj.data.vertices], [tuple(p.vertices) for p in obj.data.polygons],
                     [[(groups[g.group], g.weight) for g in v.groups] for v in obj.data.vertices]))
    return hashlib.sha256(json.dumps(data, separators=(',', ':')).encode()).hexdigest()


def sample(scene, arm, action, requested, fps, names):
    start, end = tuple(requested or action.frame_range)
    if not action.frame_range[0] <= start < end <= action.frame_range[1]:
        raise ValueError(f'Invalid range {start, end} for {action.name!r}: {tuple(action.frame_range)}.')
    seconds = (end - start) / fps
    if seconds < .1 or seconds > 60:
        raise ValueError(f'Select a 0.1–60 second cycle; {action.name!r} is {seconds:.4f}s.')
    count = max(3, round(seconds * 30) + 1)
    assign_action(arm, action)
    # FBX may create separate actions for an animated ancestor. Select the
    # matching take on that ancestor before sampling the evaluated world pose.
    suffix = action.name.split('|', 1)[-1]
    ancestor = arm.parent
    while ancestor:
        matches = [a for a in bpy.data.actions if a.name == ancestor.name + '|' + suffix]
        if matches: assign_action(ancestor, matches[0])
        elif ancestor.animation_data and ancestor.animation_data.action:
            raise ValueError(f'No matching ancestor take for {ancestor.name!r} / {action.name!r}; cannot safely bake root motion.')
        ancestor = ancestor.parent
    poses = []
    for index in range(count):
        frame(scene, start + (end - start) * index / (count - 1))
        evaluated = arm.evaluated_get(bpy.context.evaluated_depsgraph_get())
        poses.append({names[p.name]: evaluated.matrix_world @ p.matrix for p in evaluated.pose.bones})
    return {'sourceAction': action.name, 'sourceFrames': [start, end], 'sourceFps': fps,
            'sourceSeconds': seconds, 'frames': count, 'seconds': (count - 1) / 30, 'poses': poses}


def articulation(clip, parents):
    result = {}
    for name in clip['poses'][0]:
        parent = parents[name]
        def local(p): return p[parent].inverted_safe() @ p[name] if parent else p[name]
        initial = local(clip['poses'][0]).to_quaternion()
        result[name] = max(angle(initial, local(p).to_quaternion()) for p in clip['poses'])
    return result


def validate_motion(clips):
    for kind, clip in clips.items():
        if max(clip['rotationRangeDegrees'].values()) < .1:
            raise ValueError(f'Supplied {kind.upper()} action is a static pose.')
    for side in ('Left', 'Right'):
        if max(clips['walk']['rotationRangeDegrees'][side + '_' + bone] for bone in ('UpperLeg', 'LowerLeg', 'Foot')) < 4:
            raise ValueError(f'Supplied WALK lacks genuine {side} leg/foot articulation; a translated idle is not walking.')
    if max(clips['talk']['rotationRangeDegrees'][name] for name in REQUIRED if 'Leg' not in name and 'Foot' not in name) < .5:
        raise ValueError('Supplied TALK lacks measurable upper-body motion; choose an actual speaking action.')


def bake_pose(arm, pose, inverse_arm, offset):
    targets = {name: inverse_arm @ Matrix.Translation(-offset) @ matrix for name, matrix in pose.items()}
    for pb in arm.pose.bones:
        if pb.name not in targets:
            pb.matrix_basis.identity(); continue
        kwargs = {'parent_matrix': targets[pb.parent.name], 'parent_matrix_local': pb.parent.bone.matrix_local} if pb.parent else {}
        pb.matrix_basis = pb.bone.convert_local_to_pose(targets[pb.name], pb.bone.matrix_local, invert=True, **kwargs)


def sole_regions(meshes):
    world = vertices(meshes); result = {}
    for side in ('Left', 'Right'):
        candidates = []
        for obj in meshes:
            groups = {g.index for g in obj.vertex_groups if g.name in (side + '_Foot', side + '_Toes')}
            candidates.extend((obj.name, v.index, world[obj.name][v.index].z) for v in obj.data.vertices
                              if sum(g.weight for g in v.groups if g.group in groups) >= .5)
        if not candidates: raise ValueError(f'No {side} shoe/sole region from supplied foot/toe weights; inspect the skin.')
        bottom = min(p[2] for p in candidates)
        result[side] = [(obj, index) for obj, index, z in candidates if z <= bottom + .018]
    return result


def measure(scene, arm, meshes, action, clip, regions, travel, calibrated):
    assign_action(arm, action); poses = []; soles = {side: [] for side in regions}
    for number in range(1, clip['frames'] + 1):
        frame(scene, number); world = vertices(meshes)
        poses.append({p.name: (arm.matrix_world @ p.matrix).copy() for p in arm.pose.bones})
        for side, selected in regions.items():
            points = [world[obj][index] for obj, index in selected]
            soles[side].append((min(p.z for p in points), sum(points, Vector()) / len(points)))
    first, last = poses[0], poses[-1]
    position = max((first[n].translation - last[n].translation).length for n in first)
    rotation = max(angle(first[n].to_quaternion(), last[n].to_quaternion()) for n in first)
    first_velocity = (poses[1]['Hips'].translation - first['Hips'].translation) * 30
    last_velocity = (last['Hips'].translation - poses[-2]['Hips'].translation) * 30
    feet = {}
    for side, records in soles.items():
        support = [i for i, (height, _) in enumerate(records) if -.01 <= height <= .025]
        velocities = [(records[i][1] - records[i - 1][1]) * 30 + travel / clip['seconds']
                      for i in support if i > 0 and i - 1 in support]
        feet[side] = {'soleVertices': len(regions[side]), 'lowestSurfaceMinMetres': min(p[0] for p in records),
                      'lowestSurfaceMaxMetres': max(p[0] for p in records), 'nearGroundFrameFraction': len(support) / len(records),
                      'horizontalExcursionMetres': max((Vector((p[1].x, p[1].y)) - Vector((records[0][1].x, records[0][1].y))).length for p in records),
                      'nearGroundHorizontalSpeedMaxMps': max(Vector((v.x, v.y)).length for v in velocities) if calibrated and velocities else None}
    both_air = sum(all(soles[side][i][0] > .025 for side in regions) for i in range(clip['frames'])) / clip['frames']
    hip_delta = last['Hips'].translation - first['Hips'].translation
    return {'loopPositionErrorMetres': position, 'loopRotationErrorDegrees': rotation,
            'bakedHorizontalNetTravelMetres': Vector((hip_delta.x, hip_delta.y)).length,
            'hipLoopVelocityErrorMps': (last_velocity - first_velocity).length,
            'loopWithinReviewTolerance': position <= .005 and rotation <= 1, 'loopRepairApplied': False,
            'feet': feet, 'bothFeetAbove25mmFraction': both_air,
            'footContactNote': '30fps shoe-surface samples. Near-ground is only a contact candidate. '
                               'In-place Walk needs external speed calibration before world-space sliding can be measured.',
            'groundPenetrationWithin10mm': min(p['lowestSurfaceMinMetres'] for p in feet.values()) >= -.01}


CLEAR_BODY = {'Hips', 'Spine', 'Chest', 'UpperChest', 'Left_UpperLeg', 'Right_UpperLeg', 'Left_LowerLeg', 'Right_LowerLeg'}
CLEAR_FINGERS = ('Hand', 'Thumb', 'Index', 'Middle', 'Ring', 'Pinky')
CLEAR_HAND = .004                 # air kept between bare hands/fingers and the clothed body, metres
CLEAR_SLEEVE = -.02               # a sleeve may press this far into the coat: cloth on cloth, not a hand through cloth
CLEAR_REACH = .12
CLEAR_LIMIT = math.radians(40)    # largest upper-arm correction ever applied


def arm_clearance(scene, arm, meshes, actions):
    """Keep hands and forearms out of the clothed torso, hips and legs.

    Tripo retargets its motions to a bare standard body, so on a character in a thick coat the
    hanging forearms sink into the coat skirt and gesturing hands pass through its front. For
    every baked frame the upper arm is turned about the shoulder, away from the body axis, by
    just enough that bare hands stay outside the cloth and sleeves press no more than 2 cm into
    it; the corrections are widened and smoothed over time and wrapped across the loop seam.
    Clearing the sleeves completely would hold the arms spread on every frame, because the coat
    skirt is wider than a hanging arm: that stance was tried and rejected. Arms that stay within
    these limits are left exactly as supplied. Mesh, weights and every other bone are unchanged.
    """
    if len(meshes) != 1:
        return {'applied': False, 'reason': 'needs one skinned mesh', 'clips': []}
    mesh = meshes[0]
    names = {g.index: g.name for g in mesh.vertex_groups}
    dominant = [names[max(v.groups, key=lambda g: g.weight).group] if v.groups else '' for v in mesh.data.vertices]
    probes = {side: [(i, CLEAR_SLEEVE if b.endswith('_LowerArm') else CLEAR_HAND) for i, b in enumerate(dominant)
                     if b.startswith(side + '_') and (b.split('_', 1)[1].startswith(CLEAR_FINGERS) or b.endswith('_LowerArm'))]
              for side in ('Left', 'Right')}
    body = [tuple(p.vertices) for p in mesh.data.polygons if all(dominant[i] in CLEAR_BODY for i in p.vertices)]
    if not body or not all(probes.values()):
        return {'applied': False, 'reason': 'skin groups do not separate arms from the body', 'clips': []}
    to_arm = arm.matrix_world.to_3x3().inverted_safe()

    def worst(side, co, tree):
        need, point = 0.0, None
        for index, margin in probes[side]:
            p = co[index]
            hit, normal, _, distance = tree.find_nearest(p, CLEAR_REACH)
            if hit is None or distance < 1e-7:
                continue
            offset = p - hit; signed = offset.dot(normal)
            # Ignore faces seen edge-on: past an open hem the nearest point says nothing about depth.
            if abs(signed) < .6 * offset.length:
                continue
            if margin - signed > need:
                need, point = margin - signed, p.copy()
        return need, point

    def turn(pb, world_axis, radians):
        head = pb.matrix.translation.copy()
        rotation = Matrix.Rotation(radians, 4, (to_arm @ world_axis).normalized())
        target = Matrix.Translation(head) @ rotation @ Matrix.Translation(-head) @ pb.matrix
        pb.matrix_basis = pb.bone.convert_local_to_pose(target, pb.bone.matrix_local, parent_matrix=pb.parent.matrix,
                                                        parent_matrix_local=pb.parent.bone.matrix_local, invert=True)

    def solve(limit):
        """Turn each upper arm until its hand and forearm clear the body; returns residual need."""
        residual = {}
        for side in ('Left', 'Right'):
            pb = arm.pose.bones[side + '_UpperArm']; start = pb.matrix.to_quaternion()
            need = 0.0
            for _ in range(24):
                bpy.context.view_layer.update()
                co = vertices(meshes)[mesh.name]
                need, point = worst(side, co, BVHTree.FromPolygons(co, body))
                if need <= .0005:
                    break
                shoulder = arm.matrix_world @ pb.matrix.translation
                axis_point = arm.matrix_world @ arm.pose.bones['Hips'].matrix.translation
                radial = Vector((point.x - axis_point.x, point.y - axis_point.y, 0))
                reach = point - shoulder
                if radial.length < 1e-4 or reach.length < .05:
                    break
                radial.normalize(); axis = reach.cross(radial)
                if axis.length < .05 * reach.length:
                    break
                used = pb.matrix.to_quaternion().rotation_difference(start).angle
                step = min(max(need, .002) / reach.length, math.radians(4), limit - min(used, math.tau - used))
                if step <= 1e-5:
                    break
                turn(pb, axis.normalized(), step)
            residual[side] = max(need, 0.0)
        bpy.context.view_layer.update()
        return residual

    report = {'applied': True, 'handClearanceMetres': CLEAR_HAND, 'sleevePressLimitMetres': -CLEAR_SLEEVE, 'limitDegrees': math.degrees(CLEAR_LIMIT),
              'method': 'Per-frame upper-arm turn about the shoulder, away from the body axis; widened 4 frames, '
                        'Gaussian-smoothed (sigma 3 frames) and wrapped across the loop seam; residual solved per frame.',
              'clips': []}
    for action in actions:
        assign_action(arm, action)
        count = int(action.frame_range[1]); period = count - 1
        raw = {side: [] for side in probes}
        for number in range(1, count + 1):
            frame(scene, number)
            before = {side: arm.pose.bones[side + '_UpperArm'].matrix.to_quaternion() for side in probes}
            solve(CLEAR_LIMIT)
            for side in probes:
                delta = arm.pose.bones[side + '_UpperArm'].matrix.to_quaternion() @ before[side].inverted()
                axis, radians = delta.to_axis_angle()
                if radians > math.pi: radians -= math.tau
                raw[side].append(Vector(axis) * radians)
        smooth = {}
        kernel = [math.exp(-.5 * (k / 3) ** 2) for k in range(-9, 10)]; total = sum(kernel)
        for side, vectors in raw.items():
            wide = [max((vectors[(i + k) % period] for k in range(-4, 5)), key=lambda v: v.length) for i in range(period)]
            blurred = [sum((wide[(i + k) % period] * w for k, w in zip(range(-9, 10), kernel)), Vector()) / total for i in range(period)]
            smooth[side] = blurred + [blurred[0]]
        previous = {}; largest = 0.0; changed = 0; residual = 0.0
        for number in range(1, count + 1):
            frame(scene, number); touched = False
            for side in probes:
                vector = smooth[side][number - 1]
                if vector.length > 1e-5:
                    turn(arm.pose.bones[side + '_UpperArm'], (arm.matrix_world.to_3x3() @ vector).normalized(), min(vector.length, CLEAR_LIMIT))
                    touched = True
            left = solve(CLEAR_LIMIT + math.radians(5))
            residual = max(residual, *left.values())
            for side in probes:
                pb = arm.pose.bones[side + '_UpperArm']
                if side in previous and pb.rotation_quaternion.dot(previous[side]) < 0:
                    pb.rotation_quaternion = -pb.rotation_quaternion
                previous[side] = pb.rotation_quaternion.copy()
                pb.keyframe_insert('rotation_quaternion', frame=number)
                largest = max(largest, raw[side][number - 1].length, smooth[side][number - 1].length)
            changed += touched
        for curve in curves(action):
            for key in curve.keyframe_points: key.interpolation = 'LINEAR'
        report['clips'].append({'name': action.name, 'frames': count, 'framesAdjusted': changed,
                                'largestCorrectionDegrees': round(math.degrees(largest), 2),
                                'residualBelowMarginMetres': round(residual, 4)})
    return report


def render_review(scene, arm, clips, review, height):
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 8; scene.cycles.use_denoising = True
    scene.render.resolution_x = 480; scene.render.resolution_y = 640; scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('Cast review'); scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.8, .8, .8, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
    for location, power in (((-2, -3, 4), 450), ((2, 1, 3), 300)):
        bpy.ops.object.light_add(type='AREA', location=location); lamp = bpy.context.object
        lamp.data.energy = power; lamp.data.size = 4
        lamp.rotation_euler = (Vector((0, 0, height * .5)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=200); bpy.context.object.name = 'Review ground, not exported'
    bpy.ops.object.camera_add(); camera = bpy.context.object; scene.camera = camera
    camera.data.type = 'ORTHO'; camera.data.ortho_scale = height * 1.18
    scene.render.image_settings.file_format = 'JPEG'; scene.render.image_settings.quality = 85
    paths = []
    for clip in clips:
        assign_action(arm, bpy.data.actions[clip['name']])
        for view, location in (('front', (0, -5, height * .52)), ('side', (5, 0, height * .52)), ('back', (0, 5, height * .52))):
            camera.location = location
            camera.rotation_euler = (Vector((0, 0, height * .5)) - camera.location).to_track_quat('-Z', 'Y').to_euler()
            for phase in (0, .25, .5, .75, 1):
                frame(scene, 1 + (clip['frames'] - 1) * phase)
                name = f"{clip['name']}-{view}-{round(phase * 100):03d}.jpg"
                scene.render.filepath = str(review / name); bpy.ops.render.render(write_still=True); paths.append(name)
    return paths


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('model', 'source', 'art', 'review'): parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--role', required=True); parser.add_argument('--height', type=float, required=True)
    for kind, _ in CLIPS:
        parser.add_argument('--' + kind + '-action', help='Exact supplied action; no retargeting or synthesized replacement')
        parser.add_argument('--' + kind + '-range', type=float, nargs=2, metavar=('START', 'END'), help='Cycle endpoints in source frames')
    parser.add_argument('--skip-renders', action='store_true')
    parser.add_argument('--fixture-label', default='', help='Mark isolated pipeline experiments as non-production fixtures')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.import_scene.fbx(filepath=str(args.model))
    scene = bpy.context.scene; source_fps = scene.render.fps / scene.render.fps_base
    rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']; meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    if len(rigs) != 1 or not meshes: raise ValueError('Expected one supplied humanoid armature with skinned meshes.')
    arm = rigs[0]; source_actions = list(bpy.data.actions)
    if not all(any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers) for o in meshes):
        raise ValueError('Each mesh must have a supplied armature skin. No automatic re-skinning is performed.')
    if any(o.data.shape_keys and o.data.shape_keys.animation_data for o in meshes):
        raise ValueError('Animated shape keys require explicitly matched facial clips; this importer will not silently discard them.')
    original_signature = signature(meshes)
    names = {p.name: canonical(p.name) for p in arm.pose.bones}
    if len(set(names.values())) != len(names): raise ValueError('Canonical bone names collide: ' + str(names))
    missing = sorted(set(REQUIRED) - set(names.values()))
    if missing: raise ValueError('Supplied rig is missing required bones: ' + str(missing))
    selected = select_actions([a for a in source_actions if any(c.data_path.startswith('pose.bones[') for c in curves(a))], args)
    for obj in bpy.data.objects:
        if obj.animation_data:
            for track in list(obj.animation_data.nla_tracks): obj.animation_data.nla_tracks.remove(track)
    clips = {kind: sample(scene, arm, selected[kind], getattr(args, kind + '_range'), source_fps, names) for kind, _ in CLIPS}
    parents = {names[p.name]: names[p.parent.name] if p.parent else None for p in arm.pose.bones}
    for kind, clip in clips.items():
        clip['rotationRangeDegrees'] = articulation(clip, parents)
    validate_motion(clips)
    assign_action(arm, selected['idle']); frame(scene, clips['idle']['sourceFrames'][0]); fixed_arm = arm.matrix_world.copy()
    for obj in list(bpy.data.objects):
        matrix = obj.matrix_world.copy()
        if obj.animation_data: obj.animation_data_clear()
        obj.matrix_world = matrix
    for old, new in names.items():
        if old != new:
            arm.data.bones[old].name = new
            for mesh in meshes:
                if old in mesh.vertex_groups: mesh.vertex_groups[old].name = new
    for pb in arm.pose.bones: pb.rotation_mode = 'QUATERNION'
    inverse_arm = fixed_arm.inverted_safe(); anchor = clips['idle']['poses'][0]['Hips'].translation.copy(); anchor.z = 0
    actions = []
    for kind, output in CLIPS:
        clip = clips[kind]; clip['name'] = output
        start = clip['poses'][0]['Hips'].translation.copy(); end = clip['poses'][-1]['Hips'].translation.copy()
        start.z = 0; end.z = 0; travel = end - start; clip['rawTravel'] = travel.copy()
        action = bpy.data.actions.new(output); action.use_fake_user = True; assign_action(arm, action)
        for index, pose in enumerate(clip['poses']):
            bake_pose(arm, pose, inverse_arm, start - anchor + travel * (index / (clip['frames'] - 1)))
            for pb in arm.pose.bones:
                for prop in ('location', 'rotation_quaternion', 'scale'): pb.keyframe_insert(prop, frame=index + 1)
        for layer in action.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for curve in bag.fcurves:
                        for key in curve.keyframe_points: key.interpolation = 'LINEAR'
        actions.append(action)
    for action in source_actions: bpy.data.actions.remove(action)
    for action, (_, name) in zip(actions, CLIPS): action.name = name
    scene.render.fps = 30; scene.render.fps_base = 1
    assign_action(arm, actions[0]); frame(scene, 1)
    left = arm.matrix_world @ arm.pose.bones['Left_UpperLeg'].head; right = arm.matrix_world @ arm.pose.bones['Right_UpperLeg'].head
    lateral = left - right; lateral.z = 0; forward = lateral.cross(Vector((0, 0, 1))).normalized()
    if forward.length < .1: raise ValueError('Cannot determine actor forward from supplied humanoid hips.')
    root = bpy.data.objects.new(args.role + 'ModelRoot', None); bpy.context.collection.objects.link(root)
    for obj in list(bpy.data.objects):
        if obj != root and obj.parent is None and obj.type in ('ARMATURE', 'MESH', 'EMPTY'): obj.parent = root
    root.rotation_mode = 'QUATERNION'; root.rotation_quaternion = forward.rotation_difference(Vector((0, -1, 0)))
    bpy.context.view_layer.update(); points = [v for group in vertices(meshes).values() for v in group]
    actual_height = max(v.z for v in points) - min(v.z for v in points)
    if actual_height < .01: raise ValueError('Invalid imported body dimensions.')
    root.scale = Vector((args.height / actual_height,) * 3); bpy.context.view_layer.update()
    hips = arm.matrix_world @ arm.pose.bones['Hips'].head; points = [v for group in vertices(meshes).values() for v in group]
    root.location = Vector((-hips.x, -hips.y, -min(v.z for v in points))); bpy.context.view_layer.update()
    if 'headfront' not in arm.data.bones:
        bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active = arm
        bpy.ops.object.mode_set(mode='EDIT'); head = arm.data.edit_bones['Head']; inverse = arm.matrix_world.to_3x3().inverted()
        marker = arm.data.edit_bones.new('headfront'); marker.parent = head; marker.use_deform = False
        marker.head = head.head + inverse @ Vector((0, -.07, 0)); marker.tail = marker.head + inverse @ Vector((0, -.04, 0))
        bpy.ops.object.mode_set(mode='OBJECT')
    if signature(meshes) != original_signature: raise ValueError('Unexpected source geometry or skin-weight change.')
    clearance = arm_clearance(scene, arm, meshes, actions)
    if signature(meshes) != original_signature: raise ValueError('Arm clearance must not change geometry or weights.')
    regions = sole_regions(meshes); travel = root.matrix_world.to_3x3() @ clips['walk']['rawTravel']; distance = travel.length
    rates = walk_rates(distance, clips['walk']['sourceSeconds'], clips['walk']['seconds'])
    needs_calibration = rates['calibrationRequired']
    report = {'role': args.role, 'fixtureLabel': args.fixture_label, 'productionAsset': not bool(args.fixture_label),
              'blenderVersion': bpy.app.version_string, 'sourceModel': str(args.model.resolve()),
              'sourceModelSHA256': hashlib.sha256(args.model.read_bytes()).hexdigest(), 'heightMetres': args.height,
              'sourceSkinningPreserved': True, 'sourceFaceGeometryPreserved': True, 'skinGeometrySHA256': original_signature,
              'lowerBodyFixed': False, 'horizontalTravelRemoval': 'First-to-last Hips world XY drift; no leg freezing or loop morphing',
              'forward': 'Blender -Y / Unity -Z via headfront marker', 'bones': len(arm.data.bones),
              'canonicalBoneRenames': {old: new for old, new in names.items() if old != new},
              'meshVertices': sum(len(o.data.vertices) for o in meshes), 'clips': [], 'runtimeVerified': False,
              'meshSelfIntersectionVerified': False, 'appearanceAccepted': False, 'armClearance': clearance,
              'visualReviewScope': 'Five front/side/back phases per action. Full loops, transitions, other angles and runtime still require review.'}
    for action, (kind, _) in zip(actions, CLIPS):
        clip = clips[kind]; clip_travel = root.matrix_world.to_3x3() @ clip['rawTravel']
        public = {k: v for k, v in clip.items() if k not in ('poses', 'rawTravel')}
        public.update({'sourceHorizontalTravelMetres': clip_travel.length, 'sourceHorizontalTravelVector': list(clip_travel),
                       'horizontalNetTravelRemoved': True, 'sourceSelection': 'explicit' if getattr(args, kind + '_action') else 'unique matching source name'})
        public.update(measure(scene, arm, meshes, action, clip, regions, clip_travel, kind == 'walk' and not needs_calibration))
        report['clips'].append(public)
    motion = {**rates, 'sourceAction': clips['walk']['sourceAction'],
              'sourceFrames': clips['walk']['sourceFrames'], 'sourceSeconds': clips['walk']['sourceSeconds'], 'bakedSeconds': clips['walk']['seconds'],
              'sourceHorizontalTravelMetres': distance, 'minimumTravelForCalibrationMetres': .05,
              'horizontalNetTravelRemoved': True, 'fixtureLabel': args.fixture_label, 'runtimeVerified': False,
              'clips': [{k: c[k] for k in ('name', 'sourceAction', 'sourceFrames', 'sourceSeconds', 'loopPositionErrorMetres', 'loopRotationErrorDegrees', 'feet')} for c in report['clips']]}
    for folder in (args.source, args.art, args.review): folder.mkdir(parents=True, exist_ok=True)
    for image in bpy.data.images:
        if image.source == 'FILE' and not image.packed_file: image.pack()
    assign_action(arm, actions[0]); frame(scene, 1); scene.frame_start = 1; scene.frame_end = clips['idle']['frames']
    scene.unit_settings.system = 'METRIC'; scene.unit_settings.scale_length = 1
    source_path = args.source / 'Character.blend'; bpy.ops.wm.save_as_mainfile(filepath=str(source_path), compress=True)
    assign_action(arm, None)
    for action in actions:
        track = arm.animation_data.nla_tracks.new(); strip = track.strips.new(action.name, 1, action)
        if len(action.slots): strip.action_slot = action.slots[0]
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root, arm] + meshes: obj.select_set(True)
    for obj in bpy.data.objects:
        if obj.type == 'EMPTY' and obj != root and obj.parent: obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=str(args.art / 'Character.fbx'), use_selection=True,
                            object_types={'ARMATURE', 'MESH', 'EMPTY'}, add_leaf_bones=False, use_mesh_modifiers=False,
                            bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=True,
                            bake_anim_simplify_factor=0, axis_forward='-Z', axis_up='Y', path_mode='STRIP')
    bpy.ops.wm.open_mainfile(filepath=str(source_path)); scene = bpy.context.scene
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    report['reviewImages'] = [] if args.skip_renders else render_review(scene, arm, report['clips'], args.review, args.height)
    report['sourceReopened'] = True; report['rendered'] = not args.skip_renders
    (args.art / 'Motion.json').write_text(json.dumps(motion, ensure_ascii=False, indent=2) + '\n')
    (args.source / 'authoring-results.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('PERIOD_CHARACTER_AUTHORED', json.dumps({'role': args.role, 'source': str(source_path),
          'clips': [c['name'] for c in report['clips']], 'calibrationRequired': needs_calibration,
          'runtimeVerified': False, 'fixtureLabel': args.fixture_label}), flush=True)


if __name__ == '__main__':
    main()
