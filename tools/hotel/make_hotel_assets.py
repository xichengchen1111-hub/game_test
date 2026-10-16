"""Original procedural textures for the A gabled old inn (B08).

numpy + Pillow, deterministic, every texture tiles seamlessly.

    python -X utf8 tools/hotel/make_hotel_assets.py [--size 1024]

Writes game/Assets/Art/Hotel/Textures/*_Base.png (and *_Normal.png where relief matters):
  HotelPaperA    lobby/dining wallpaper: faded ochre ground, deep green damask stripes, 1.2 m per tile
  HotelPaperB    guest-room wallpaper: cream ground, small sprigs and pale stripes, 1.0 m per tile
  HotelRug       Persian-style medallion rug (whole rug, clamped), madder red, indigo, ivory
  HotelRunner    corridor/room runner, banded border repeating along its length, 1.8 m per tile
Stone, plaster, oak, slate, flags and tiles reuse the police/garage kits.
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'game/Assets/Art/Hotel/Textures'


def tile_noise(n, scale, seed, shape=None):
    rng = np.random.default_rng(seed)
    h, w = shape or (n, n)
    white = rng.normal(size=(h, w))
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    f = np.sqrt(fx * fx + fy * fy)
    out = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(f * scale) ** 2)))
    out -= out.mean(); out /= np.abs(out).max() + 1e-9
    return out


def normal_map(height, strength):
    dx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
    dy = np.roll(height, -1, 0) - np.roll(height, 1, 0)
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(height)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln, ny / ln, nz / ln], -1) * .5 + .5


def save(name, rgb, height=None, strength=1.0):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.uint8(np.clip(rgb, 0, 255))).save(OUT / f'{name}_Base.png', optimize=True)
    if height is not None:
        Image.fromarray(np.uint8(np.clip(normal_map(height, strength) * 255, 0, 255))).save(OUT / f'{name}_Normal.png', optimize=True)


def mix(a, b, t):
    return a * (1 - t[..., None]) + b * t[..., None]


def damask_motif(u, v):
    """Periodic damask-like lobed motif on the unit cell (u, v in [0,1))."""
    x = (u - .5) * 2; y = (v - .5) * 2
    r = np.sqrt(x * x * 1.6 + y * y)
    a = np.arctan2(y, x)
    petals = .55 + .22 * np.cos(4 * a) + .08 * np.cos(8 * a)
    body = np.clip((petals - r) * 9, 0, 1)
    stem = np.clip((.06 - np.abs(x)) * 30, 0, 1) * (np.abs(y) < .95)
    ring = np.clip((.05 - np.abs(r - .3)) * 25, 0, 1)
    return np.clip(body - ring * .7 + stem * .6, 0, 1)


def paper_a(n):
    yy, xx = np.mgrid[0:n, 0:n] / n
    ground = np.array([176, 150, 96], np.float32)
    stripe_col = np.array([70, 86, 62], np.float32)
    rgb = np.broadcast_to(ground, (n, n, 3)).astype(np.float32).copy()
    # Four stripes per tile; the damask sits in alternate stripes, half a cell offset.
    s = (xx * 4) % 1
    band = np.clip((.11 - np.abs(s - .5)) * 40, 0, 1)
    fine = (np.clip((.012 - np.abs(np.abs(s - .5) - .34)) * 200, 0, 1))
    cellu = (xx * 4) % 1; cellv = (yy * 3 + (np.floor(xx * 4) % 2) * .5) % 1
    motif = damask_motif(cellu, cellv) * (1 - band)
    rgb = mix(rgb, stripe_col, band * .6 + fine * .5)
    rgb = mix(rgb, np.array([150, 122, 72], np.float32), motif * .55)
    age = tile_noise(n, 40, 11) * 10 + tile_noise(n, 6, 12) * 5
    rgb += age[..., None]
    return rgb, motif * .3 + band * .2 + tile_noise(n, 3, 13) * .08


def paper_b(n):
    yy, xx = np.mgrid[0:n, 0:n] / n
    rgb = np.broadcast_to(np.array([214, 200, 168], np.float32), (n, n, 3)).astype(np.float32).copy()
    s = (xx * 5) % 1
    stripe = np.clip((.04 - np.abs(s - .5)) * 60, 0, 1)
    rgb = mix(rgb, np.array([190, 168, 132], np.float32), stripe * .7)
    # Small sprigs: a rose-coloured dot with two green leaves on a staggered grid.
    cu = (xx * 5) % 1; cv = (yy * 6 + (np.floor(xx * 5) % 2) * .5) % 1
    for du, dv, col, rad in ((0, 0, (150, 82, 80), .07), (-.07, .06, (104, 120, 84), .05), (.07, .05, (104, 120, 84), .05)):
        d = np.sqrt(((cu - .25 - du) * 1.6) ** 2 + (cv - .5 - dv) ** 2)
        t = np.clip((rad - d) * 60, 0, 1)
        rgb = mix(rgb, np.array(col, np.float32), t * .8)
    rgb += (tile_noise(n, 30, 21) * 8 + tile_noise(n, 5, 22) * 4)[..., None]
    return rgb, tile_noise(n, 3, 23) * .08


def rug(n):
    yy, xx = np.mgrid[0:n, 0:n] / n
    madder = np.array([128, 40, 34], np.float32); indigo = np.array([38, 46, 78], np.float32)
    ivory = np.array([206, 186, 150], np.float32); gold = np.array([176, 128, 60], np.float32)
    rgb = np.broadcast_to(madder, (n, n, 3)).astype(np.float32).copy()
    ex = np.minimum(xx, 1 - xx); ey = np.minimum(yy, 1 - yy); e = np.minimum(ex, ey)
    rgb = mix(rgb, indigo, ((e > .03) & (e < .11)).astype(np.float32))
    rgb = mix(rgb, ivory, ((e > .045) & (e < .055)).astype(np.float32) * .9)
    rgb = mix(rgb, gold, ((e > .085) & (e < .095)).astype(np.float32) * .9)
    # Border meander
    t = (np.where(ex < ey, yy, xx) * 24) % 1
    mean = ((e > .06) & (e < .08) & (np.abs(t - .5) < .25)).astype(np.float32)
    rgb = mix(rgb, madder * 1.1, mean)
    # Central medallion and corner spandrels
    x = (xx - .5) * 2; y = (yy - .5) * 2
    r = np.sqrt(x * x + (y * .8) ** 2); a = np.arctan2(y, x)
    med = np.clip((.38 + .06 * np.cos(8 * a) - r) * 30, 0, 1)
    rgb = mix(rgb, indigo, med)
    rgb = mix(rgb, ivory, np.clip((.18 + .04 * np.cos(8 * a) - r) * 30, 0, 1))
    rgb = mix(rgb, madder, np.clip((.08 - r) * 40, 0, 1))
    for cx in (-1, 1):
        for cy in (-1, 1):
            d = np.sqrt((x - cx * .78) ** 2 + (y - cy * .78) ** 2)
            rgb = mix(rgb, indigo, np.clip((.22 - d) * 25, 0, 1) * (e > .11))
    # Small field motifs
    fu = (xx * 14) % 1; fv = (yy * 14) % 1
    dots = np.clip((.12 - np.sqrt((fu - .5) ** 2 + (fv - .5) ** 2)) * 30, 0, 1) * (e > .12) * (1 - med)
    rgb = mix(rgb, gold * .9, dots * .7)
    wear = np.clip(tile_noise(n, 25, 31) * .5 + .1, 0, 1) * .25
    rgb = mix(rgb, np.array([150, 120, 100], np.float32), wear)
    pile = tile_noise(n, 1.5, 32) * 10
    rgb += pile[..., None]
    return rgb


def runner(n):
    yy, xx = np.mgrid[0:n, 0:n] / n
    base = np.array([96, 52, 44], np.float32)
    rgb = np.broadcast_to(base, (n, n, 3)).astype(np.float32).copy()
    ex = np.minimum(xx, 1 - xx)
    rgb = mix(rgb, np.array([46, 50, 68], np.float32), ((ex > .02) & (ex < .14)).astype(np.float32))
    rgb = mix(rgb, np.array([196, 170, 128], np.float32), ((ex > .065) & (ex < .08)).astype(np.float32))
    v = (yy * 6) % 1
    diamond = np.clip((.18 - (np.abs(xx - .5) * 1.2 + np.abs(v - .5))) * 20, 0, 1)
    rgb = mix(rgb, np.array([170, 120, 62], np.float32), diamond)
    rgb += (tile_noise(n, 1.5, 41) * 9 + tile_noise(n, 30, 42) * 8)[..., None]
    return rgb


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--size', type=int, default=1024)
    n = ap.parse_args().size
    rgb, h = paper_a(n); save('HotelPaperA', rgb, h, 1.2)
    rgb, h = paper_b(n); save('HotelPaperB', rgb, h, 1.0)
    save('HotelRug', rug(n))
    save('HotelRunner', runner(n))
    print('HOTEL_TEXTURES_PASS', n)


if __name__ == '__main__':
    main()
