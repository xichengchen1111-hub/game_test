"""Check rendered watch hands against shared-clock readings, not just UI math."""
import argparse
import json
import math
from pathlib import Path

from PIL import Image


def check(results_path, image_path=None):
    data = json.loads(results_path.read_text(encoding='utf-8'))
    image_path = image_path or results_path.parent / 'watch-later.png'
    image = Image.open(image_path).convert('RGB')
    assert image.size == (data['width'], data['height']), 'Screenshot dimensions differ'
    scale = data['hudScale']
    cx, cy = data['centerX'], data['centerY']
    samples = {}
    for name, angle, length in [('hour', data['minutes'] % 720 * .5, 56),
                                ('minute', data['minutes'] % 60 * 6, 80)]:
        angle = math.radians(angle)
        ink = 0
        count = 20
        # Sample blank inner-face portions along each expected hand. A pivot dot,
        # one Roman numeral or a hand drawn outside the face cannot satisfy this.
        for i in range(count):
            distance = length * scale * (.30 + .58 * i / (count - 1))
            x = round(cx + math.sin(angle) * distance)
            y = round(cy - math.cos(angle) * distance)
            pixels = [image.getpixel((x + dx, y + dy)) for dx in (-1, 0, 1) for dy in (-1, 0, 1)]
            ink += any(max(pixel) < 105 for pixel in pixels)
        samples[name] = {'darkSamples': ink, 'samples': count, 'inkFraction': ink / count}
    # The same native toggle must remove the actual face, not merely change a bool.
    closed_path = results_path.parent / 'watch-closed.png'
    toggle_pixels = None
    if image_path.name == 'watch-later.png' and closed_path.exists():
        closed = Image.open(closed_path).convert('RGB')
        assert closed.size == image.size
        deltas = []
        for dx, dy in [(20, -18), (-20, -18), (25, 20), (-25, 20)]:
            p = (round(cx + dx * scale), round(cy + dy * scale))
            deltas.append(sum(abs(a-b) for a, b in zip(image.getpixel(p), closed.getpixel(p))) / 3)
        toggle_pixels = sum(deltas) / len(deltas)
    passed = all(item['inkFraction'] >= .80 for item in samples.values())
    if toggle_pixels is not None:
        passed = passed and toggle_pixels > 40
    return {'passed': passed, 'resolution': list(image.size), 'image': image_path.name,
            'hands': samples, 'toggleMeanRgbChange8bit': toggle_pixels,
            'verification': 'Samples ink in actual native-rendered PNGs along expected shared-time hand directions; no screenshot editing.'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('results', type=Path)
    parser.add_argument('--image', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    result = check(args.results, args.image)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(result))
    if not result['passed']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
