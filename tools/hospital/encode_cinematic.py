"""Encode the recording of the bedside cinematic into one video for review.

    python3 tools/hospital/encode_cinematic.py <capture folder> <destination video>

The capture folder is what `--review --fresh --showcase-hospital-cg` writes (ShowcaseRecorder:
numbered JPEG frames with wall-clock times, the listener mix, named marks). The video runs from
the walk down the ward to the bedside after the card, at recorded speed and with the game's own
sound raised back from the quiet recording level. Nothing is retimed, regraded or cropped.
"""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'opening'))
from encode_showcase import render  # noqa: E402


def main():
    folder = Path(sys.argv[1]).resolve()
    target = Path(sys.argv[2]).resolve()
    target.parent.mkdir(parents=True, exist_ok=True)
    manifest = json.loads((folder / 'capture.json').read_text())
    marks = {m['name']: m['time'] for m in manifest['marks']}
    audio = folder / 'game-audio.wav'
    mix = np.fromfile(audio, dtype='<f4', offset=44)
    peak = float(np.abs(mix).max()) if mix.size else 0.0
    gain = min(1 / manifest['listenerVolume'], .95 / peak) if peak > 0 else 1.0
    start, end = marks['ward'], marks['end'] + .6
    seconds = render(folder, manifest, audio, gain, [(start, end)], target, folder.parent)
    report = {'source': str(folder), 'video': target.name, 'seconds': round(seconds, 2), 'nativeFrames': len(manifest['times']),
              'size': [manifest['width'], manifest['height']], 'audioGain': round(gain, 3), 'marks': manifest['marks'],
              'method': 'Native game window and listener mix of a scripted run with the real controller and the ordinary E ray; '
                        'recorded speed. Scripted inputs are not physical keyboard play.'}
    target.with_suffix('.json').write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print('HOSPITAL_CINEMATIC_VIDEO', target, round(seconds, 2))


if __name__ == '__main__':
    main()
