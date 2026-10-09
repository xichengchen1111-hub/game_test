"""Original seamless cloud noise. Pillow only; fixed seed, no downloaded assets."""
from pathlib import Path
import random
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SIZE = 256
rng = random.Random(1931)
fields = {n: [[rng.random() for _ in range(n)] for _ in range(n)] for n in (4, 8, 16, 32, 64)}
cells = [[(rng.random(), rng.random()) for _ in range(16)] for _ in range(16)]

def billow(x, y):
    x, y = x * 16 / SIZE, y * 16 / SIZE
    ix, iy = int(x), int(y)
    distance = 4.0
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            px, py = cells[(iy + oy) % 16][(ix + ox) % 16]
            distance = min(distance, (ix + ox + px - x) ** 2 + (iy + oy + py - y) ** 2)
    return max(0, 1 - distance ** .5 / .95)

def noise(x, y, n):
    x, y = x * n / SIZE, y * n / SIZE
    ix, iy = int(x), int(y)
    fx, fy = x - ix, y - iy
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    grid = fields[n]
    a = grid[iy % n][ix % n] * (1 - fx) + grid[iy % n][(ix + 1) % n] * fx
    b = grid[(iy + 1) % n][ix % n] * (1 - fx) + grid[(iy + 1) % n][(ix + 1) % n] * fx
    return a * (1 - fy) + b * fy

pixels = []
for y in range(SIZE):
    for x in range(SIZE):
        broad = sum(noise(x, y, n) * w for n, w in ((8, .55), (16, .27), (32, .13), (64, .05)))
        warp = noise(x, y, 4)
        pixels.append(tuple(round(v * 255) for v in (broad, warp, billow(x, y))))
image = Image.new('RGB', (SIZE, SIZE))
image.putdata(pixels)
for path in ('assets/source/sky/weather-noise.png', 'game/Assets/Art/Sky/WeatherNoise.png'):
    image.save(ROOT / path)
print('WEATHER_NOISE_PASS: 256px seamless seeded RGB fields')
