"""Local-only Blender integration checks; every generated asset stays under builds.

The Meshy look-around channel is an export-mechanics fixture, NOT a speaking
animation or a replacement for any new PoliceCast character.
"""
import argparse
import json
import math
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREPARE = ROOT / 'tools/cast/prepare_period_character.py'
MESHY = ROOT / 'game/Assets/Art/MilkboyMeshy/Messenger.fbx'
CLERK = next((ROOT / 'assets/source/station-clerk/tripo/original').glob('*.fbx'))
PREFIX = 'target_character|target_character|'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--blender', type=Path, required=True)
    parser.add_argument('--output', type=Path, default=ROOT / 'builds/cast-motion-tests')
    parser.add_argument('--skip-renders', action='store_true')
    args = parser.parse_args(); output = args.output.resolve()
    if not output.is_relative_to(ROOT / 'builds'):
        raise ValueError('Fixture outputs must stay inside this repository builds directory.')
    output.mkdir(parents=True, exist_ok=True); results = []
    base = [str(args.blender), '--background', '--factory-startup', '--python-exit-code', '1']

    def invoke(name, command, expect_error=None):
        process = subprocess.run(base + command, cwd=ROOT, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        (output / (name + '.log')).write_text(process.stdout)
        success = process.returncode == 1 and expect_error in process.stdout if expect_error else process.returncode == 0
        results.append({'name': name, 'passed': success, 'exitCode': process.returncode, 'expectedError': expect_error})
        print(name, 'PASS' if success else 'FAIL', flush=True)
        if not success: raise AssertionError(process.stdout[-6000:])
        return process.stdout

    def command(name, source, extra, render=False):
        folder = output / name
        return ['--python', str(PREPARE), '--', '--model', str(source), '--source', str(folder / 'source'),
                '--art', str(folder / 'art'), '--review', str(folder / 'review'), '--role', 'LocalFixture', '--height', '1.7',
                '--fixture-label', 'LOCAL-PIPELINE-ONLY-lookaround-is-not-speaking', *extra,
                *([] if render else ['--skip-renders'])]

    idle = ['--idle-action', PREFIX + 'Short_Breathe_and_Look_Around']
    walk = ['--walk-action', PREFIX + 'Walking']
    talk = ['--talk-action', PREFIX + 'Look_Around_Dumbfounded']
    invoke('missing-walk', command('missing-walk', CLERK, []), 'A real supplied WALK action is required')
    invoke('missing-talk', command('missing-talk', MESHY, idle + walk), 'A real supplied TALK action is required')
    invoke('duplicate-actions', command('duplicate-actions', MESHY, idle + walk + ['--talk-action', PREFIX + 'Short_Breathe_and_Look_Around']),
           'must be three distinct supplied actions')
    invoke('invalid-range', command('invalid-range', MESHY, idle + walk + talk + ['--walk-range', '0', '99999']), 'Invalid range')

    code = f'''import importlib.util
from mathutils import Matrix
spec=importlib.util.spec_from_file_location('cast_prepare',{str(PREPARE)!r}); m=importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
parents={{name:None if name=='Hips' else 'Hips' for name in m.REQUIRED}}
poses=[{{name:Matrix.Translation((i,0,0)) for name in m.REQUIRED}} for i in (0,1)]
ranges=m.articulation({{'poses':poses}},parents)
assert max(ranges.values())==0
rates=m.walk_rates(1.1,1.2083333333333333,1.2)
assert abs(rates['walkMetresPerSecond']-1.1/1.2)<1e-12
assert abs(rates['sourceWalkMetresPerSecond']-1.1/1.2083333333333333)<1e-12
assert rates['walkMetresPerSecond']!=rates['sourceWalkMetresPerSecond']
assert m.walk_rates(.002,1.2083333333333333,1.2)=={{'walkMetresPerSecond':0,'sourceWalkMetresPerSecond':0,'calibrationRequired':True}}
valid={{'rotationRangeDegrees':{{name:8 for name in m.REQUIRED}}}}
try: m.validate_motion({{'idle':valid,'walk':{{'rotationRangeDegrees':ranges}},'talk':valid}})
except ValueError as e: assert 'WALK' in str(e) and 'static pose' in str(e)
else: raise AssertionError('Translated static pose was accepted as Walk')
print('STATIC_WALK_REJECTION_PASS')'''
    invoke('translated-static-walk', ['--python-expr', code])
    invoke('in-place', command('in-place', MESHY, idle + walk + talk, render=not args.skip_renders))
    invoke('forward', command('forward', MESHY, idle + ['--walk-action', PREFIX + 'walking_2'] + talk))
    for name in ('in-place', 'forward'):
        folder = output / name
        report = json.loads((folder / 'source/authoring-results.json').read_text())
        motion = json.loads((folder / 'art/Motion.json').read_text())
        assert [c['name'] for c in report['clips']] == ['01_Idle', '02_Walk', '03_Talk']
        assert report['sourceSkinningPreserved'] and report['sourceFaceGeometryPreserved'] and not report['lowerBodyFixed']
        assert not report['runtimeVerified'] and not report['appearanceAccepted'] and not report['meshSelfIntersectionVerified']
        assert report['fixtureLabel'] and not report['productionAsset']
        assert all(c['bakedHorizontalNetTravelMetres'] < 1e-5 for c in report['clips'])
        if name == 'in-place': assert motion['calibrationRequired'] and motion['walkMetresPerSecond'] == 0
        else:
            assert not motion['calibrationRequired'] and motion['walkMetresPerSecond'] > .5
            assert math.isclose(motion['walkMetresPerSecond'], motion['sourceHorizontalTravelMetres'] / motion['bakedSeconds'])
            assert math.isclose(motion['sourceWalkMetresPerSecond'], motion['sourceHorizontalTravelMetres'] / motion['sourceSeconds'])
        if name == 'in-place' and not args.skip_renders:
            assert len(report['reviewImages']) == 30
            assert all((folder / 'review' / image).is_file() for image in report['reviewImages'])
        code = f'''import bpy,json
bpy.ops.import_scene.fbx(filepath={str(folder / 'art/Character.fbx')!r})
names=[a.name.rsplit('|',1)[-1] for a in bpy.data.actions if any(c.data_path.startswith('pose.bones[') for l in a.layers for s in l.strips for b in s.channelbags for c in b.fcurves)]
assert sorted(names)==['01_Idle','02_Walk','03_Talk'],names
assert all(a.frame_range[1]-a.frame_range[0]>2 for a in bpy.data.actions)
print('THREE_CLIPS_REIMPORT_PASS',json.dumps(names))'''
        invoke(name + '-fbx-reimport', ['--python-expr', code])
    (output / 'results.json').write_text(json.dumps({'passed': True, 'scope': 'Local old Meshy mechanics fixture; no new NPC or valid Talk acceptance',
                                                   'checks': results}, indent=2) + '\n')
    print('CAST_PIPELINE_TESTS_PASS', str(output / 'results.json'), flush=True)


if __name__ == '__main__':
    main()
