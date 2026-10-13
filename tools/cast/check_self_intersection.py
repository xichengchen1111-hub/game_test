"""Measure hand/forearm penetration into the torso, hips and legs of a prepared period character.

Run on the prepared source, which holds the three baked 30 fps actions:
    blender --background assets/source/police-cast/<Role>/tripo/Character.blend \
        --python tools/cast/check_self_intersection.py -- <output folder> [frame step]

Every sampled frame of 01_Idle, 02_Walk and 03_Talk is evaluated with the real skin. A probe
vertex (dominant weight on a hand, finger or forearm bone) counts as penetrating when it lies
behind the nearest body face (dominant weights on hips, spine, chest or legs) and squarely
behind it rather than past an open edge. The report gives depth and frame per clip and the
script renders close-ups of the worst frames. It measures arms against the body only: cloth
stretching, skirts and shoulder creases still need the rendered sheets to be looked at.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

BODY = {'Hips', 'Spine', 'Chest', 'UpperChest', 'Left_UpperLeg', 'Right_UpperLeg', 'Left_LowerLeg', 'Right_LowerLeg'}
FINGERS = ('Hand', 'Thumb', 'Index', 'Middle', 'Ring', 'Pinky')
REACH = .10          # only faces within 10 cm of a probe are considered
SQUARE = -.6         # cosine: the probe must be behind the face, not beyond its edge
REPORT_DEPTH = .005  # 5 mm


def probe_class(bone):
    if not bone.startswith(('Left_', 'Right_')):
        return None
    side, part = bone.split('_', 1)
    if part.startswith(FINGERS):
        return side + ' hand'
    if part == 'LowerArm':
        return side + ' forearm'
    return None


def main():
    argv = sys.argv[sys.argv.index('--') + 1:]
    out = Path(argv[0]); out.mkdir(parents=True, exist_ok=True)
    step = int(argv[1]) if len(argv) > 1 else 2
    scene = bpy.context.scene
    arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
    mesh = next(o for o in bpy.data.objects if o.type == 'MESH' and o.find_armature() == arm)
    names = {g.index: g.name for g in mesh.vertex_groups}
    dominant = []
    for v in mesh.data.vertices:
        best = max(v.groups, key=lambda g: g.weight, default=None)
        dominant.append(names[best.group] if best else '')
    probes = [(i, probe_class(b)) for i, b in enumerate(dominant) if probe_class(b)]
    body_polys = [tuple(p.vertices) for p in mesh.data.polygons if all(dominant[i] in BODY for i in p.vertices)]
    if not probes or not body_polys:
        raise ValueError('Skin groups do not separate arms from the body; cannot measure.')
    report = {'role': out.name, 'probeVertices': len(probes), 'bodyFaces': len(body_polys), 'frameStep': step,
              'rule': 'probe behind nearest body face within 10 cm, cosine below -0.6; depth in metres', 'clips': []}
    worst_frames = []
    for clip in ('01_Idle', '02_Walk', '03_Talk'):
        action = bpy.data.actions[clip]
        arm.animation_data.action = action
        if hasattr(arm.animation_data, 'action_slot') and action.slots:
            arm.animation_data.action_slot = action.slots[0]
        start, end = int(action.frame_range[0]), int(action.frame_range[1])
        frames = list(range(start, end + 1, step))
        worst = dict(depth=0.0, frame=start, part='', bodyBone='', at=[0, 0, 0])
        over = {.005: 0, .01: 0, .02: 0}
        per_part = {}
        for f in frames:
            scene.frame_set(f)
            evaluated = mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
            data = evaluated.to_mesh()
            world = evaluated.matrix_world
            co = [world @ v.co for v in data.vertices]
            tree = BVHTree.FromPolygons(co, body_polys, all_triangles=False)
            deepest = 0.0
            for index, part in probes:
                p = co[index]
                hit, normal, face, distance = tree.find_nearest(p, REACH)
                if hit is None or distance < 1e-6:
                    continue
                offset = p - hit
                signed = offset.dot(normal)
                if signed >= 0 or signed / offset.length > SQUARE:
                    continue
                depth = -signed
                deepest = max(deepest, depth)
                per_part[part] = max(per_part.get(part, 0.0), depth)
                if depth > worst['depth']:
                    worst = dict(depth=depth, frame=f, part=part, bodyBone=dominant[body_polys[face][0]], at=[round(c, 4) for c in p])
            for limit in over:
                over[limit] += deepest > limit
            evaluated.to_mesh_clear()
        entry = {'clip': clip, 'framesSampled': len(frames), 'maxDepthMetres': round(worst['depth'], 4), 'worstFrame': worst['frame'],
                 'worstPart': worst['part'], 'againstBodyBone': worst['bodyBone'], 'worstPoint': worst['at'],
                 'framesDeeperThan5mm': over[.005], 'framesDeeperThan1cm': over[.01], 'framesDeeperThan2cm': over[.02],
                 'maxDepthByPartMetres': {k: round(v, 4) for k, v in sorted(per_part.items())}}
        report['clips'].append(entry)
        if worst['depth'] > REPORT_DEPTH:
            worst_frames.append((clip, worst))
        else:
            scene.frame_set(start); worst_frames.append((clip, dict(worst, frame=start, at=[round(c, 4) for c in (arm.matrix_world @ arm.pose.bones['Right_Hand'].head)])))
    report['maxDepthMetres'] = max(c['maxDepthMetres'] for c in report['clips'])
    hands = max((d for c in report['clips'] for part, d in c['maxDepthByPartMetres'].items() if part.endswith('hand')), default=0.0)
    sleeves = max((d for c in report['clips'] for part, d in c['maxDepthByPartMetres'].items() if part.endswith('forearm')), default=0.0)
    report['maxHandDepthMetres'] = hands; report['maxSleeveDepthMetres'] = sleeves
    report['limits'] = {'handMetres': .003, 'sleeveMetres': .025,
                        'reason': 'A bare hand inside cloth is visible clipping; a sleeve pressed up to 2.5 cm into the coat reads as cloth on cloth.'}
    report['withinLimits'] = hands <= .003 and sleeves <= .025
    report['closeUps'] = render_close_ups(scene, arm, worst_frames, out)
    (out / 'self-intersection.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('SELF_INTERSECTION_REPORT', json.dumps({'withinLimits': report['withinLimits'], 'hand': hands, 'sleeve': sleeves, **{c['clip']: c['maxDepthByPartMetres'] for c in report['clips']}}))


def render_close_ups(scene, arm, worst_frames, out):
    if not worst_frames:
        return []
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 16; scene.cycles.use_denoising = True
    scene.render.resolution_x = scene.render.resolution_y = 720; scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('Clipping review'); scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.8, .8, .8, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .9
    for location, power in (((-2, -3, 3), 350), ((2, -3, 3), 350), ((0, 3, 3), 250)):
        bpy.ops.object.light_add(type='AREA', location=location); lamp = bpy.context.object
        lamp.data.energy = power; lamp.data.size = 3
        lamp.rotation_euler = (Vector((0, 0, 1)) - lamp.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.object.camera_add(); camera = bpy.context.object; scene.camera = camera
    camera.data.type = 'ORTHO'; camera.data.ortho_scale = .7
    scene.render.image_settings.file_format = 'JPEG'; scene.render.image_settings.quality = 90
    paths = []
    for clip, worst in worst_frames:
        action = bpy.data.actions[clip]
        arm.animation_data.action = action
        if hasattr(arm.animation_data, 'action_slot') and action.slots:
            arm.animation_data.action_slot = action.slots[0]
        scene.frame_set(worst['frame'])
        target = Vector(worst['at'])
        side = 1 if target.x >= 0 else -1
        for view, direction in (('front', Vector((0, -1, 0))), ('outer', Vector((side, -.35, 0))), ('back', Vector((side * .4, 1, 0)))):
            camera.location = target + direction.normalized() * 4
            camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
            name = f"clip-{clip}-f{worst['frame']:03d}-{view}.jpg"
            scene.render.filepath = str(out / name); bpy.ops.render.render(write_still=True); paths.append(name)
    return paths


if __name__ == '__main__':
    main()
