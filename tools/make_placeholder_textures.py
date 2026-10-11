#!/usr/bin/env python3
"""Deterministic weathered textures for the 67 placeholder town buildings.

FacadeAtlas.png / FacadeAtlas-Normal.png (1024x1024): one 4.5 m wide wall strip that
tiles horizontally. Bottom half = ground storey (plinth, rising damp, string course),
top half = upper storey (cornice and eave shadow). One opaque painted window per tile:
dressed surround, sill, keystone, louvred shutters, dull glass with a curtain band and
rain streaks below the sill. Stucco is near-neutral; Unity tints it per building.

Slate.png / Slate-Normal.png (512x512, 4 m): courses parallel to the ridge (lines of
constant x), slates staggered in alternate courses, symmetric edges for both slopes.

Restrained low-frequency detail for a low-poly game: no photo noise, no baked sun.
Usage: python tools/make_placeholder_textures.py [--out game/Assets/Art/TownPlaceholders]
"""
import argparse, json
from pathlib import Path
import numpy as np
from PIL import Image

SEED = 1931


def periodic_noise(h, w, cells_y, cells_x, rng):
    """Smooth value noise that tiles in both directions."""
    grid = rng.random((cells_y, cells_x))
    y = np.arange(h) / h * cells_y; x = np.arange(w) / w * cells_x
    y0 = np.floor(y).astype(int); x0 = np.floor(x).astype(int)
    fy = (y - y0)[:, None]; fx = (x - x0)[None, :]
    fy = fy * fy * (3 - 2 * fy); fx = fx * fx * (3 - 2 * fx)
    y1 = (y0 + 1) % cells_y; x1 = (x0 + 1) % cells_x; y0 %= cells_y; x0 %= cells_x
    a = grid[y0][:, x0]; b = grid[y0][:, x1]; c = grid[y1][:, x0]; d = grid[y1][:, x1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def normal_from_height(height, strength, wrap_y):
    gx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * .5
    if wrap_y:
        gy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * .5
    else:
        p = np.pad(height, ((1, 1), (0, 0)), mode='edge'); gy = (p[2:] - p[:-2]) * .5
    # Image rows run downward; +Y (green) points up the texture.
    nx = -gx * strength; ny = gy * strength; nz = np.ones_like(height)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    rgb = np.stack([nx / length, ny / length, nz / length], -1) * .5 + .5
    return (np.clip(rgb, 0, 1) * 255 + .5).astype(np.uint8)


def box(mask_x, mask_y, x0, x1, y0, y1):
    return ((mask_x >= x0) & (mask_x < x1))[None, :] & ((mask_y >= y0) & (mask_y < y1))[:, None]


def soft(v, edge):
    return np.clip(v / max(edge, 1e-6), 0, 1)


def facade_storey(rng, ground, W=1024, H=512, width_m=4.5, height_m=3.05):
    xm = (np.arange(W) + .5) / W * width_m           # metres, left to right
    ym = height_m - (np.arange(H) + .5) / H * height_m  # metres above the storey floor (row 0 = top)
    X = np.broadcast_to(xm[None, :], (H, W)); Y = np.broadcast_to(ym[:, None], (H, W))
    colour = np.empty((H, W, 3)); colour[:] = (214, 208, 196)
    height = np.zeros((H, W))
    # Lime stucco: broad mottling and faint ashlar lines.
    mottle = periodic_noise(H, W, 6, 9, rng) * .6 + periodic_noise(H, W, 14, 21, rng) * .4
    colour *= (1 + (mottle - .5) * .08)[..., None]
    course = np.abs(((Y + .02) % .45) - .225); joint_h = course > .214
    stagger = ((Y // .45) % 2) * .45
    joint_v = np.abs((((X + stagger) % .9) - .45)) > .445
    joints = joint_h | joint_v
    colour[joints] *= .955; height[joints] -= .35
    # Window geometry.
    cx = width_m / 2; ww = 1.15
    sill, head = (0.95, 2.55) if ground else (0.90, 2.45)
    gx0, gx1 = cx - ww / 2, cx + ww / 2
    sx0, sx1 = gx0 - .12, gx1 + .12
    surround = box(xm, ym, sx0, sx1, sill, head + .12)
    colour[surround] = (230, 224, 210); height[surround] = 1.0
    # Shutters outside the surround.
    for a, b in ((sx0 - .50, sx0), (sx1, sx1 + .50)):
        sh = box(xm, ym, a, b, sill, head)
        colour[sh] = (79, 94, 85)
        slat = (((Y - sill) % .07) < .012) & sh
        colour[slat] *= .82; height[sh] = .55; height[slat] = .25
        rim = sh & ~box(xm, ym, a + .03, b - .03, sill + .03, head - .03)
        colour[rim] *= .80; height[rim] = .70
    # Recessed opaque glass with timber frame and curtain band.
    glass = box(xm, ym, gx0, gx1, sill, head)
    t = np.clip((Y - sill) / (head - sill), 0, 1)
    glass_col = np.stack([46 + 22 * t, 56 + 24 * t, 67 + 26 * t], -1)
    colour[glass] = glass_col[glass]
    curtain = glass & (Y > head - (head - sill) * .18)
    colour[curtain] = (150, 145, 135)
    frame = glass & ((np.abs(X - cx) < .025) | (np.abs(Y - (sill + (head - sill) * .60)) < .025) |
                     ~box(xm, ym, gx0 + .05, gx1 - .05, sill + .05, head - .05))
    colour[frame] = (62, 70, 64)
    height[glass] = -1.2; height[frame] = -.6
    # Reveal ambient occlusion just inside the surround.
    reveal = glass & ~box(xm, ym, gx0 + .07, gx1 - .07, sill + .07, head - .07)
    colour[reveal] *= .82
    # Keystone and projecting sill.
    key = box(xm, ym, cx - .09, cx + .09, head, head + .22)
    colour[key] = (234, 228, 214); height[key] = 1.4
    sill_band = box(xm, ym, sx0 - .15, sx1 + .15, sill - .08, sill)
    colour[sill_band] = (226, 220, 206); height[sill_band] = 1.5
    under_sill = box(xm, ym, sx0 - .15, sx1 + .15, sill - .11, sill - .08)
    colour[under_sill] *= .78
    # Rain streaks under both sill ends, fading downward.
    streak_noise = periodic_noise(1, W, 1, 64, rng)[0]
    for end in (sx0 - .10, sx1 + .10):
        for k, offset in enumerate((-.05, .0, .06)):
            centre = end + offset
            width = .035 + .02 * streak_noise[int((centre % width_m) / width_m * W) % W]
            band = np.exp(-((X - centre) / width) ** 2)
            fall = np.clip((sill - .08 - Y) / .7, 0, 1)
            fade = np.where((Y < sill - .08) & (Y > sill - .78), 1 - fall, 0)
            colour *= (1 - .12 * (band * fade * (.7 + .3 * k / 2)))[..., None]
    if ground:
        plinth = Y < .45
        colour[plinth] = (142, 136, 126)
        colour[plinth] *= (1 + (mottle[plinth] - .5) * .10)[:, None]
        pj = plinth & ((np.abs((X % .6) - .3) > .292) | (np.abs(Y - .44) < .008))
        colour[pj] *= .80; height[plinth] = .8; height[pj] = .3
        chamfer = (Y >= .43) & (Y < .45); colour[chamfer] *= 1.10
        damp = periodic_noise(H, W, 4, 14, rng)
        rise = np.clip(1 - (Y - .45) / .75, 0, 1) * (Y >= .45)
        colour *= (1 - .12 * rise * (.55 + .45 * damp))[..., None]
        band = Y > 2.85
        colour[band] = (228, 222, 208); height[band] = 1.2
        colour[(Y > 2.82) & (Y <= 2.85)] *= .80
    else:
        band = Y > 2.80
        colour[band] = (228, 222, 208); height[band] = 1.4
        under = np.clip(1 - (2.80 - Y) / .25, 0, 1) * (Y <= 2.80)
        colour *= (1 - .18 * under)[..., None]
        colour[(Y > 2.92) & (Y < 2.94)] *= .86
    return np.clip(colour, 0, 255), height


def slate(rng, S=512, size_m=4.0):
    xm = (np.arange(S) + .5) / S * size_m; ym = (np.arange(S) + .5) / S * size_m
    X = np.broadcast_to(xm[None, :], (S, S)); Y = np.broadcast_to(ym[:, None], (S, S))
    course = np.floor(X / .20).astype(int); cu = (X % .20) / .20
    offset = (course % 2) * .15
    piece = np.floor((Y + offset) / .30).astype(int); pv = ((Y + offset) % .30) / .30
    courses = int(round(size_m / .20)); pieces = int(round(size_m / .30)) + 1
    value = rng.normal(0, .06, (courses, pieces + 2)); value = np.clip(value, -.10, .10)
    kind = rng.random((courses, pieces + 2))
    v = value[course % courses, piece % (pieces + 2)]
    k = kind[course % courses, piece % (pieces + 2)]
    colour = np.empty((S, S, 3)); colour[:] = (78, 84, 92)
    colour *= (1 + v)[..., None]
    warm = k > .955; moss = (k > .93) & (k <= .955)
    colour[warm] = colour[warm] * .9 + np.array([96, 86, 78]) * .1 * 1.0 + 4
    colour[moss] = colour[moss] * .85 + np.array([84, 96, 78]) * .15
    gap = (cu < .05) | (cu > .95); joint = (pv < .035) | (pv > .965)
    colour[gap] *= .55; colour[joint & ~gap] *= .72
    dome = np.sin(np.clip(cu, 0, 1) * np.pi) * .6 + np.sin(np.clip(pv, 0, 1) * np.pi) * .4
    height = dome.copy(); height[gap] = -.4; height[joint] -= .3
    grain = periodic_noise(S, S, 20, 20, rng)
    colour *= (1 + (grain - .5) * .05)[..., None]
    return np.clip(colour, 0, 255), height


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--out', default=str(Path(__file__).resolve().parents[1] / 'game/Assets/Art/TownPlaceholders'))
    out = Path(ap.parse_args().out); out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(SEED)
    upper_c, upper_h = facade_storey(rng, ground=False)
    ground_c, ground_h = facade_storey(rng, ground=True)
    atlas = np.concatenate([upper_c, ground_c], 0); atlas_h = np.concatenate([upper_h, ground_h], 0)
    Image.fromarray((atlas + .5).astype(np.uint8), 'RGB').save(out / 'FacadeAtlas.png', optimize=True)
    Image.fromarray(normal_from_height(atlas_h, 1.6, wrap_y=False), 'RGB').save(out / 'FacadeAtlas-Normal.png', optimize=True)
    sc, sh = slate(rng)
    Image.fromarray((sc + .5).astype(np.uint8), 'RGB').save(out / 'Slate.png', optimize=True)
    Image.fromarray(normal_from_height(sh, 2.2, wrap_y=True), 'RGB').save(out / 'Slate-Normal.png', optimize=True)
    manifest = dict(generator='tools/make_placeholder_textures.py', seed=SEED,
                    facadeAtlas=dict(size=[1024, 1024], tileMetres=[4.5, 6.1], rows='top half upper storey, bottom half ground storey (3.05 m each)',
                                     uv='u = metres/4.5; v = 0..0.5 one storey, 0..1 two storeys', colourSpace='sRGB albedo; normal OpenGL +Y'),
                    slate=dict(size=[512, 512], tileMetres=[4, 4], courses='0.20 m parallel to ridge (constant x), slates 0.30 m staggered'),
                    note='Opaque painted windows on placeholders; no interior is implied.')
    (out / 'placeholder-textures.json').write_text(json.dumps(manifest, indent=1), encoding='utf-8')
    print('written', out)


if __name__ == '__main__':
    main()
