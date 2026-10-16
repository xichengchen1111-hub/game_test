"""Preserve the Tripo terrier export and prepare it for the street (see prepare_street_dog.py).

The archive is kept unchanged under assets/source/street-cast/Terrier/tripo/original/ (ignored by
Git, like the other casts); textures are prepared exactly as for the period characters.

    python3 tools/street/import_street_dog.py --archive builds/street/downloads/Terrier/Terrier-Tripo-rig.zip \
        --blender ~/Applications/Blender-4.5.9/Blender.app/Contents/MacOS/Blender --asset-id <tripo id>
"""
import argparse
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/cast'))
import import_period_character as cast  # noqa: E402  (shared archive and texture handling)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--archive', type=Path, required=True)
    parser.add_argument('--blender', type=Path, required=True)
    parser.add_argument('--asset-id', default='')
    parser.add_argument('--withers', type=float, default=.39)
    parser.add_argument('--skip-renders', action='store_true')
    args = parser.parse_args()
    source = ROOT / 'assets/source/street-cast/Terrier/tripo'
    art = ROOT / 'game/Assets/Art/StreetCast/Terrier'
    review = ROOT / 'builds/cast-review/Terrier'
    saved, unpacked, signature = cast.preserve_archive(args.archive.resolve(), source / 'original')
    models = sorted(unpacked.rglob('*.fbx')) + sorted(unpacked.rglob('*.FBX'))
    if len(models) != 1:
        raise ValueError('Expected one FBX in the terrier export: ' + str(models))
    textures = cast.prepare_textures(unpacked, art)
    command = [str(args.blender.resolve()), '--background', '--factory-startup', '--python-exit-code', '1',
               '--python', str(Path(__file__).with_name('prepare_street_dog.py')), '--',
               '--model', str(models[0]), '--source', str(source), '--art', str(art), '--review', str(review),
               '--withers', str(args.withers)]
    if args.skip_renders:
        command.append('--skip-renders')
    subprocess.run(command, cwd=ROOT, check=True)
    report = {'role': 'Terrier', 'tripoAsset': args.asset_id, 'archive': str(saved.relative_to(ROOT)),
              'archiveSHA256': signature, 'originalPreserved': True, 'withersMetres': args.withers, 'textures': textures,
              'source': str((source / 'Character.blend').relative_to(ROOT)), 'unity': str((art / 'Dog.fbx').relative_to(ROOT)),
              'rig': str((art / 'DogRig.json').relative_to(ROOT)),
              'sourceSHA256': cast.digest(source / 'Character.blend'), 'fbxSHA256': cast.digest(art / 'Dog.fbx'),
              'license': 'No third-party license inherited; retain any license in the original export.',
              'runtimeVerified': False, 'appearanceAccepted': False}
    (source / 'import-results.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
    print('STREET_DOG_IMPORT_PASS', flush=True)


if __name__ == '__main__':
    main()
