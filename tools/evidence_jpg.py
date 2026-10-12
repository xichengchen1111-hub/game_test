#!/usr/bin/env python3
"""Convert evidence PNG frames to JPEG before committing them (REPO-01, 2026-10-12).

    python3 tools/evidence_jpg.py output/demo/<feature> [more dirs or .png files]
        [--quality 88] [--keep-png] [--refs docs/agent-memory.md ...]

Each PNG becomes a same-named .jpg (quality 88 is about 12% of the PNG size for game frames)
and the PNG is deleted unless --keep-png is given. References to the converted file names in
.md/.json/.txt files under the given folders, and in any --refs files, are rewritten. Any other
tracked file that still names a converted PNG is listed at the end so it can be fixed by hand.
Uses Pillow; on macOS without Pillow it falls back to the built-in `sips`.
"""
import argparse
import pathlib
import subprocess
import sys


def convert(png, jpg, quality):
    try:
        from PIL import Image
    except ImportError:
        if sys.platform != 'darwin':
            raise SystemExit('Pillow is required (python3 -m pip install pillow)')
        subprocess.run(['sips', '-s', 'format', 'jpeg', '-s', 'formatOptions', str(quality), str(png), '--out', str(jpg)],
                       check=True, capture_output=True)
        return
    with Image.open(png) as image:
        image.convert('RGB').save(jpg, 'JPEG', quality=quality, optimize=True)


def main():
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    except AttributeError:
        pass
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('paths', nargs='+')
    parser.add_argument('--quality', type=int, default=88)
    parser.add_argument('--keep-png', action='store_true')
    parser.add_argument('--refs', nargs='*', default=[])
    args = parser.parse_args()

    pngs, text_files = [], set(pathlib.Path(r) for r in args.refs)
    for raw in args.paths:
        path = pathlib.Path(raw)
        if path.is_dir():
            pngs += sorted(p for p in path.rglob('*') if p.suffix.lower() == '.png')
            text_files |= {p for p in path.rglob('*') if p.suffix.lower() in ('.md', '.json', '.txt')}
        elif path.suffix.lower() == '.png' and path.exists():
            pngs.append(path)
            text_files |= {p for p in path.parent.iterdir() if p.suffix.lower() in ('.md', '.json', '.txt')}
        else:
            raise SystemExit('Not a folder or PNG file: ' + raw)
    if not pngs:
        print('No PNG files found.')
        return 0

    before = after = 0
    renamed = {}
    for png in pngs:
        jpg = png.with_suffix('.jpg')
        convert(png, jpg, args.quality)
        before += png.stat().st_size
        after += jpg.stat().st_size
        renamed[png.name] = jpg.name
        if not args.keep_png:
            png.unlink()
    print('%d frames: %.1f MB PNG -> %.1f MB JPG' % (len(pngs), before / 1048576, after / 1048576))

    for text in sorted(text_files):
        if not text.exists():
            continue
        data = text.read_bytes()
        try:
            content = data.decode('utf-8')
        except UnicodeDecodeError:
            continue
        updated = content
        for old, new in renamed.items():
            updated = updated.replace(old, new)
        if updated != content:
            text.write_bytes(updated.encode('utf-8'))
            print('updated references: ' + str(text))

    if not args.keep_png:
        command = ['git', 'grep', '-l', '-F']
        for old in renamed:
            command += ['-e', old]
        found = subprocess.run(command, capture_output=True).stdout.decode('utf-8', 'replace').split()
        if found:
            print('Still naming a converted PNG (check whether these refer to the same files):')
            for name in found:
                print('  ' + name)
    return 0


if __name__ == '__main__':
    sys.exit(main())
