"""Prepare the Tripo terrier of the street cast: its own mesh, skin and rig, scaled and placed.

Tripo rigs a four-legged animal on its humanoid template: the forelegs are the "arms" (Clavicle,
Upperarm, Forearm, Hand, with the toes as fingers), the hind legs the "legs" (Thigh, Calf, Foot,
ToeBase), then Waist, Spine01/02, NeckTwist01/02 and Head; there are no tail bones. Its motion
library and text-to-motion are humanoid only (a generated walk stood the dog upright), so no clip
is exported: StreetDog (game/Assets/Scripts/StreetDog.cs) walks and idles it from these bones.

Scales the rig so the back behind the withers stands at --withers metres (a wire fox terrier is
about 0.39 m at the shoulder), faces it along Blender -Y (Unity -Z, like the period characters),
puts the lowest paw on the ground and the middle of the body over the origin. Saves Character.blend
(textures packed), Dog.fbx (mesh and armature, no animation) and DogRig.json (bone roles and the
measured legs), and renders rest-pose review images.

    blender --background --factory-startup --python tools/street/prepare_street_dog.py -- \
        --model <fbx> --source <dir> --art <dir> --review <dir> [--withers 0.39] [--skip-renders]
"""
import argparse
import hashlib
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector

ROLES = {
    'hip': 'Hip', 'pelvis': 'Pelvis', 'waist': 'Waist', 'spine1': 'Spine01', 'spine2': 'Spine02',
    'neck1': 'NeckTwist01', 'neck2': 'NeckTwist02', 'head': 'Head',
    'foreLeftUpper': 'L_Upperarm', 'foreLeftLower': 'L_Forearm', 'foreLeftPaw': 'L_Hand',
    'foreRightUpper': 'R_Upperarm', 'foreRightLower': 'R_Forearm', 'foreRightPaw': 'R_Hand',
    'hindLeftUpper': 'L_Thigh', 'hindLeftLower': 'L_Calf', 'hindLeftPaw': 'L_Foot',
    'hindRightUpper': 'R_Thigh', 'hindRightLower': 'R_Calf', 'hindRightPaw': 'R_Foot',
}


def world_vertices(meshes):
    graph = bpy.context.evaluated_depsgraph_get()
    points = []
    for obj in meshes:
        evaluated = obj.evaluated_get(graph)
        points += [evaluated.matrix_world @ v.co for v in evaluated.data.vertices]
    return points


def signature(meshes):
    digest = hashlib.sha256()
    for obj in sorted(meshes, key=lambda o: o.name):
        for v in obj.data.vertices:
            digest.update(('%.6f %.6f %.6f' % tuple(v.co)).encode())
            for g in v.groups:
                digest.update(('%d %.5f' % (g.group, g.weight)).encode())
    return digest.hexdigest()


def back_height(points, spine):
    """Top of the back just behind the withers: the highest skin over the front of the ribcage."""
    y0 = spine.y
    band = [p.z for p in points if abs(p.x) < .06 and y0 - .02 <= p.y <= y0 + .09]
    if len(band) < 20:
        raise ValueError('Too few vertices over the ribcage to measure the back height.')
    return max(band)


def render_review(scene, height, length, review):
    scene.render.engine = 'CYCLES'; scene.cycles.samples = 8; scene.cycles.use_denoising = True
    scene.render.resolution_x = 640; scene.render.resolution_y = 480; scene.render.resolution_percentage = 100
    scene.world = bpy.data.worlds.new('Dog review'); scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (.8, .8, .8, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = .7
    target = Vector((0, 0, height * .5))
    for location, power in (((-1.4, -2, 2.4), 160), ((1.4, .8, 1.8), 110)):
        bpy.ops.object.light_add(type='AREA', location=location); lamp = bpy.context.object
        lamp.data.energy = power; lamp.data.size = 2.5
        lamp.rotation_euler = (target - lamp.location).to_track_quat('-Z', 'Y').to_euler()
    bpy.ops.mesh.primitive_plane_add(size=50); bpy.context.object.name = 'Review ground, not exported'
    bpy.ops.object.camera_add(); camera = bpy.context.object; scene.camera = camera
    camera.data.type = 'ORTHO'; camera.data.ortho_scale = max(length, height) * 1.25
    scene.render.image_settings.file_format = 'JPEG'; scene.render.image_settings.quality = 85
    names = []
    for view, location in (('front', (0, -4, height * .5)), ('side', (4, 0, height * .5)), ('back', (0, 4, height * .5)),
                           ('three-quarter', (2.6, -2.6, height * 1.1))):
        camera.location = location
        camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
        name = 'Rest-' + view + '.jpg'
        scene.render.filepath = str(review / name); bpy.ops.render.render(write_still=True); names.append(name)
    return names


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('model', 'source', 'art', 'review'):
        parser.add_argument('--' + name, type=Path, required=True)
    parser.add_argument('--withers', type=float, default=.39)
    parser.add_argument('--skip-renders', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--') + 1:])
    if not .2 <= args.withers <= .8:
        raise ValueError('--withers is the shoulder height in metres of a dog, 0.2-0.8.')
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.import_scene.fbx(filepath=str(args.model))
    rigs = [o for o in bpy.data.objects if o.type == 'ARMATURE']; meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    if len(rigs) != 1 or not meshes:
        raise ValueError('Expected one supplied armature with skinned meshes.')
    arm = rigs[0]
    if not all(any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers) for o in meshes):
        raise ValueError('Each mesh must keep its supplied armature skin; nothing is re-skinned here.')
    missing = sorted(b for b in ROLES.values() if b not in arm.data.bones)
    if missing:
        raise ValueError('The four-legged rig lacks bones: ' + str(missing))
    original = signature(meshes)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    for obj in bpy.data.objects:
        if obj.animation_data:
            obj.animation_data_clear()
    bones = {role: arm.data.bones[name] for role, name in ROLES.items()}
    head = arm.matrix_world @ bones['head'].head_local; pelvis = arm.matrix_world @ bones['pelvis'].head_local
    forward = head - pelvis; forward.z = 0
    if forward.length < .05:
        raise ValueError('Cannot tell the head end of the dog from the rig.')
    root = bpy.data.objects.new('TerrierModelRoot', None); bpy.context.collection.objects.link(root)
    for obj in list(bpy.data.objects):
        if obj != root and obj.parent is None and obj.type in ('ARMATURE', 'MESH', 'EMPTY'):
            obj.parent = root
    root.rotation_mode = 'QUATERNION'; root.rotation_quaternion = forward.normalized().rotation_difference(Vector((0, -1, 0)))
    bpy.context.view_layer.update()
    spine = arm.matrix_world @ bones['spine2'].head_local
    measured = back_height(world_vertices(meshes), spine)
    floor = min(p.z for p in world_vertices(meshes))
    scale = args.withers / (measured - floor)
    root.scale = Vector((scale,) * 3); bpy.context.view_layer.update()
    points = world_vertices(meshes)
    fore = (arm.matrix_world @ bones['foreLeftPaw'].head_local + arm.matrix_world @ bones['foreRightPaw'].head_local) / 2
    hind = (arm.matrix_world @ bones['hindLeftPaw'].head_local + arm.matrix_world @ bones['hindRightPaw'].head_local) / 2
    middle = (fore + hind) / 2
    root.location = Vector((-middle.x, -middle.y, -min(p.z for p in points))); bpy.context.view_layer.update()
    if signature(meshes) != original:
        raise ValueError('Unexpected change to the supplied geometry or skin weights.')
    points = world_vertices(meshes)
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))

    def at(role, end='head'):
        b = bones[role]
        return arm.matrix_world @ (b.head_local if end == 'head' else b.tail_local)

    def leg(prefix):
        upper, lower, paw = (at(prefix + part) for part in ('Upper', 'Lower', 'Paw'))
        return {'upper': ROLES[prefix + 'Upper'], 'lower': ROLES[prefix + 'Lower'], 'paw': ROLES[prefix + 'Paw'],
                'jointHeights': [round(upper.z, 4), round(lower.z, 4), round(paw.z, 4)],
                'upperLength': round((lower - upper).length, 4), 'lowerLength': round((paw - lower).length, 4),
                'pawForward': round(-paw.y, 4)}
    rig = {'note': 'Four-legged Tripo rig on the humanoid template; roles for StreetDog. Unity faces local -Z.',
           'bones': ROLES, 'withersMetres': args.withers, 'scaleApplied': round(scale, 5),
           'lengthMetres': round(hi.y - lo.y, 4), 'widthMetres': round(hi.x - lo.x, 4), 'heightMetres': round(hi.z - lo.z, 4),
           'legs': {name: leg(name) for name in ('foreLeft', 'foreRight', 'hindLeft', 'hindRight')},
           'neckHeight': round(at('neck1').z, 4), 'headHeight': round(at('head').z, 4), 'headForward': round(-at('head').y, 4)}
    for folder in (args.source, args.art, args.review):
        folder.mkdir(parents=True, exist_ok=True)
    for image in bpy.data.images:
        if image.source == 'FILE' and not image.packed_file:
            image.pack()
    scene = bpy.context.scene; scene.unit_settings.system = 'METRIC'; scene.unit_settings.scale_length = 1
    source_path = args.source / 'Character.blend'; bpy.ops.wm.save_as_mainfile(filepath=str(source_path), compress=True)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in [root, arm] + meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=str(args.art / 'Dog.fbx'), use_selection=True, object_types={'ARMATURE', 'MESH', 'EMPTY'},
                             add_leaf_bones=False, use_mesh_modifiers=False, bake_anim=False,
                             axis_forward='-Z', axis_up='Y', path_mode='STRIP')
    (args.art / 'DogRig.json').write_text(json.dumps(rig, indent=2) + '\n')
    images = [] if args.skip_renders else render_review(scene, hi.z - lo.z, hi.y - lo.y, args.review)
    report = {'role': 'Terrier', 'blenderVersion': bpy.app.version_string, 'sourceModel': str(args.model.resolve()),
              'sourceModelSHA256': hashlib.sha256(args.model.read_bytes()).hexdigest(), 'skinGeometrySHA256': original,
              'sourceSkinningPreserved': True, 'clips': [], 'motion': 'none exported; runtime gait in StreetDog',
              'rig': rig, 'reviewImages': images, 'runtimeVerified': False, 'appearanceAccepted': False}
    (args.source / 'authoring-results.json').write_text(json.dumps(report, indent=2) + '\n')
    print('STREET_DOG_AUTHORED', json.dumps({'withers': args.withers, 'scale': scale, 'length': rig['lengthMetres'],
                                            'height': rig['heightMetres']}), flush=True)


if __name__ == '__main__':
    main()
