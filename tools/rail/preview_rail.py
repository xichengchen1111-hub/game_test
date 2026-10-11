"""Render quick Blender review views of the generated rail sources.

These are modelling checks only, not game captures.
    blender --background --python-exit-code 1 --python preview_rail.py -- --blend StationB.blend --out dir --set station
"""
import argparse
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def u(p):
    return Vector((-float(p[0]), -float(p[2]), float(p[1])))


VIEWS = {
    'station': [
        ('front-wide', (-14, 5.5, -30), (0, 4.2, -6), 30),
        ('front-oblique', (-26, 3.2, -16), (-6, 4.0, -6), 28),
        ('entrance-close', (0, 1.7, -14), (0, 4.4, -6), 30),
        ('window-close', (-8, 1.8, -10.5), (-8, 3.4, -6), 32),
        ('roofline', (22, 12, -26), (4, 6.5, 2), 30),
        ('rear-concourse', (-9, 1.7, 15), (2, 3.6, 4), 24),
        ('shed-inside', (-26, 1.9, 21), (20, 6.5, 20), 22),
        ('shed-end', (-52, 4.0, 4), (-34, 6.5, 20), 30),
        ('shed-aerial', (-60, 26, -8), (0, 4, 18), 30),
        ('hall-west', (6, 1.7, -3.5), (-12, 3.2, 2), 22),
        ('hall-east', (-6, 1.7, -3.5), (12, 3.6, 2), 22),
        ('hall-ceiling', (0, 1.7, -4.5), (0, 6.6, 3), 20),
        ('stove-corner', (-8.5, 1.6, 0.8), (-13.2, 1.4, 4.4), 28),
        ('luggage-room', (-15.6, 1.7, -0.6), (-21, 1.4, 1.5), 22),
        ('duty-room', (15.6, 1.7, -0.6), (21, 1.4, 1.5), 22),
        ('hall-endwall', (2, 1.7, 0), (15, 5.3, 0), 30),
    ],
    'train': [
        ('loco-front', (36, 2.4, -10), (20, 1.9, 0), 32),
        ('loco-side', (19, 1.8, -14), (19, 1.8, 0), 34),
        ('loco-rear', (8, 3.2, -9), (16, 2.2, 0), 30),
        ('coach-side', (-1, 2.0, -13), (-1, 2.0, 0), 30),
        ('coach-end', (-8, 2.4, -5.5), (-8, 2.0, 0), 26),
        ('van', (-36, 2.4, -9), (-29, 2.0, 0), 30),
        ('consist', (40, 9, -26), (0, 1.5, 0), 30),
        ('coach-aisle', (6.3, 2.65, 0), (-5, 2.25, 0), 20),
        ('coach-window', (0.0, 2.2, 0.3), (0.9, 2.2, -1.45), 22),
        ('coach-seat', (2.9, 2.0, 0.25), (-1.0, 1.8, -1.1), 24),
        ('underframe', (-1, 0.55, -6), (-1, 0.6, 0), 30),
    ],
}


def main():
    args = argparse.ArgumentParser()
    args.add_argument('--blend', required=True)
    args.add_argument('--out', required=True)
    args.add_argument('--set', required=True, choices=sorted(VIEWS))
    args.add_argument('--only', default='')
    a = args.parse_args(sys.argv[sys.argv.index('--') + 1:])
    bpy.ops.wm.open_mainfile(filepath=str(Path(a.blend).resolve()))
    out = Path(a.out).resolve()
    out.mkdir(parents=True, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    shading = scene.display.shading
    shading.light = 'STUDIO'
    shading.color_type = 'MATERIAL'
    shading.show_shadows = True
    shading.show_cavity = True
    shading.cavity_type = 'BOTH'
    shading.background_type = 'WORLD'
    scene.world.color = (.20, .22, .27)
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.display.render_aa = '8'
    camera_data = bpy.data.cameras.new('PreviewCamera')
    camera = bpy.data.objects.new('PreviewCamera', camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    wanted = {w for w in a.only.split(',') if w}
    for name, eye, target, lens in VIEWS[a.set]:
        if wanted and name not in wanted:
            continue
        camera_data.lens = lens
        camera_data.clip_start = .05
        camera_data.clip_end = 400
        location = u(eye)
        direction = (u(target) - location).normalized()
        camera.matrix_world = direction.to_track_quat('-Z', 'Y').to_matrix().to_4x4()
        camera.location = location
        scene.render.filepath = str(out / f'{a.set}-{name}.png')
        bpy.ops.render.render(write_still=True)
        print('PREVIEW', scene.render.filepath)


main()
