"""Original procedural textures for the E stone-barn garage (B07).

numpy + Pillow, deterministic (fixed seeds), every texture tiles seamlessly.

    python -X utf8 tools/garage/make_garage_assets.py [--size 1024]

Writes game/Assets/Art/Garage/Textures/*_Base.png and *_Normal.png:
  GarageRubble   grey-buff random-coursed roughstone (hall exterior), 1.6 m per tile
  GarageLime     the same coursing under a worn limewash (hall interior), 1.6 m per tile
  GarageCement   worn cement flags with oil stains (hall floor), 3 m per tile
  GarageBoards   weathered vertical boarding (timber gables, fence, shed), 1.2 m per tile
  GarageCobble   rounded setts for the rear yard, 2 m per tile
  GarageTin      tarred standing-seam sheet (office lean-to roof), 2 m per tile
plus GarageSign.png, the hanging sign's crossed spanner and hammer (no lettering).
Door and sounds reuse the police kit (game/Assets/Art/Police).
"""
import argparse
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'game/Assets/Art/Garage/Textures'


def tile_noise(n, scale, seed):
    """Tileable band-limited noise in [-1, 1]: white noise filtered in the frequency domain."""
    rng = np.random.default_rng(seed)
    white = rng.normal(size=(n, n))
    fy = np.fft.fftfreq(n)[:, None]; fx = np.fft.fftfreq(n)[None, :]
    f = np.sqrt(fx * fx + fy * fy)
    mask = np.exp(-(f * scale) ** 2)
    out = np.real(np.fft.ifft2(np.fft.fft2(white) * mask))
    out -= out.mean(); out /= np.abs(out).max() + 1e-9
    return out


def normal_map(height, strength):
    dx = np.roll(height, -1, 1) - np.roll(height, 1, 1)
    dy = np.roll(height, -1, 0) - np.roll(height, 1, 0)
    nx, ny, nz = -dx * strength, dy * strength, np.ones_like(height)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    return np.stack([nx / ln, ny / ln, nz / ln], -1) * .5 + .5


def save(name, rgb, height, strength):
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.uint8(np.clip(rgb, 0, 255))).save(OUT / f'{name}_Base.png', optimize=True)
    Image.fromarray(np.uint8(np.clip(normal_map(height, strength) * 255, 0, 255))).save(OUT / f'{name}_Normal.png', optimize=True)


def coursed_stones(n, seed, rows=(9, 12), mortar=(5, 9)):
    """Random-coursed rubble: rows of uneven height, stones of uneven length; wraps both ways.
    Returns stone id per pixel (-1 = mortar), inner distance and the per-stone random table."""
    rng = np.random.default_rng(seed)
    count = int(rng.integers(*rows))
    h = rng.uniform(.55, 1.5, count); h = np.round(h / h.sum() * n).astype(int); h[-1] += n - h.sum()
    ids = np.full((n, n), -1, int); dist = np.zeros((n, n))
    ywarp = (tile_noise(n, 55, seed + 1) * 14).astype(int)
    xwarp = (tile_noise(n, 55, seed + 2) * 14).astype(int)
    yy, xx = np.mgrid[0:n, 0:n]
    wy = (yy + ywarp) % n; wx = (xx + xwarp) % n
    edges = np.concatenate([[0], np.cumsum(h)])
    row = np.searchsorted(edges, wy, side='right') - 1
    jitter = tile_noise(n, 6, seed + 3)
    stone = 0
    for r in range(count):
        widths = []
        total = 0
        while total < n:
            wv = int(rng.uniform(.7, 2.9) * h[r])
            widths.append(wv); total += wv
        widths[-1] -= total - n
        if widths[-1] < h[r] * .5 and len(widths) > 1:
            widths[-2] += widths[-1]; widths.pop()
        offset = int(rng.integers(0, n))
        starts = (offset + np.concatenate([[0], np.cumsum(widths)[:-1]])) % n
        sel = row == r
        px = wx[sel]; py = wy[sel]
        rel = (px[:, None] - starts[None, :]) % n
        k = np.argmin(np.where(rel < np.array(widths)[None, :], rel, n * 4), axis=1)
        x_in = rel[np.arange(len(k)), k]; w_k = np.array(widths)[k]
        y_in = py - edges[r]
        m = rng.uniform(*mortar)
        # Rounded, uneven stone outlines: corner rounding plus a per-pixel jitter of the joint width.
        d = np.minimum.reduce([x_in, w_k - 1 - x_in, y_in, h[r] - 1 - y_in]).astype(float)
        cx = np.minimum(x_in, w_k - 1 - x_in).astype(float); cy = np.minimum(y_in, h[r] - 1 - y_in).astype(float)
        rr = .16 * h[r]
        corner = (cx < rr) & (cy < rr)
        d = np.where(corner, rr - np.sqrt((rr - cx) ** 2 + (rr - cy) ** 2), d)
        d = d + jitter[sel] * m * .35
        ids[sel] = np.where(d >= m / 2, stone + k, -1)
        dist[sel] = np.maximum(d - m / 2, 0)
        stone += len(widths)
    table = rng.random((stone + 1, 4))
    return ids, dist, table


def rubble(n, lime=False):
    ids, dist, table = coursed_stones(n, 1931)
    fine = tile_noise(n, 3, 11); mid = tile_noise(n, 22, 12); broad = tile_noise(n, 140, 13)
    palette = np.array([[150, 143, 128], [128, 124, 115], [164, 152, 130], [116, 111, 100], [141, 134, 119], [136, 121, 102], [122, 118, 112], [155, 146, 124]], float)
    stone_rgb = palette[(table[np.maximum(ids, 0), 0] * len(palette)).astype(int) % len(palette)]
    stone_rgb *= (1 + (table[np.maximum(ids, 0), 1] - .5) * .18)[..., None]
    bulge = 1 - np.exp(-dist / (6 + 10 * table[np.maximum(ids, 0), 2]))
    height = np.where(ids >= 0, .35 + .55 * bulge + .08 * mid + .05 * fine, .05 + .04 * fine)
    rgb = np.where((ids >= 0)[..., None], stone_rgb * (1 + .1 * mid[..., None] + .05 * fine[..., None]),
                   np.array([104, 98, 88]) * (1 + .1 * fine[..., None]))
    rgb *= (1 + .07 * broad)[..., None]
    if lime:
        # Worn limewash: pale cream, thick in the joints, rubbed thin on the stone faces.
        wash = np.clip(.78 + .25 * broad + .12 * mid - .25 * bulge * (table[np.maximum(ids, 0), 3] > .7), .35, 1)
        cream = np.array([226, 219, 200]) * (1 + .03 * fine[..., None])
        rgb = rgb * (1 - wash[..., None]) + cream * wash[..., None]
        height = height * .45 + .05 * fine
        # Grime band: darker at the foot of the wall (bottom of the texture, v = 0).
        v = np.linspace(1, 0, n)[:, None]
        rgb *= (1 - .10 * np.clip((v - .82) / .18, 0, 1))[..., None]
    return rgb, height


def cement(n):
    rng = np.random.default_rng(7)
    fine = tile_noise(n, 2.5, 21); mid = tile_noise(n, 30, 22); broad = tile_noise(n, 180, 23)
    yy, xx = np.mgrid[0:n, 0:n]
    cell = n // 5
    jx = (xx % cell); jy = (yy % cell)
    joint = np.minimum.reduce([jx, cell - 1 - jx, jy, cell - 1 - jy]) < 3
    shade = rng.uniform(.9, 1.08, (5, 5))[yy // cell % 5, xx // cell % 5]
    rgb = np.array([138, 136, 128], float) * shade[..., None] * (1 + .06 * mid[..., None] + .04 * fine[..., None] + .05 * broad[..., None])
    # Oil stains: soft dark blots, a few drip trails.
    stain = np.zeros((n, n))
    for _ in range(9):
        cx, cy, r = rng.integers(0, n), rng.integers(0, n), rng.uniform(.03, .1) * n
        dx = (xx - cx + n // 2) % n - n // 2; dy = (yy - cy + n // 2) % n - n // 2
        stain = np.maximum(stain, np.clip(1 - np.sqrt(dx * dx + dy * dy) / r + .3 * mid, 0, 1) * rng.uniform(.4, .8))
    rgb *= (1 - .45 * stain)[..., None]
    rgb[joint] *= .72
    height = .6 + .12 * fine + .1 * mid - .5 * joint
    return rgb, height


def boards(n):
    rng = np.random.default_rng(5)
    fine = tile_noise(n, 2, 31); grain = tile_noise(n, 8, 32)
    yy, xx = np.mgrid[0:n, 0:n]
    count = 8
    w = n // count
    k = xx // w; local = xx % w
    tone = rng.uniform(.82, 1.12, count)[k % count]
    streak = np.sin((yy * .02 + grain * 3 + k * 1.7)) * .5 + .5
    rgb = np.array([108, 92, 74], float) * tone[..., None] * (.86 + .14 * streak[..., None] + .05 * fine[..., None])
    gap = (local < 3) | (local > w - 2)
    rgb[gap] *= .45
    weather = np.clip(tile_noise(n, 90, 33) * .5 + .5, 0, 1)
    grey = np.array([128, 124, 116], float)
    rgb = rgb * (1 - .35 * weather[..., None]) + grey * .35 * weather[..., None]
    height = .7 + .1 * streak + .05 * fine - .6 * gap
    return rgb, height


def cobble(n):
    rng = np.random.default_rng(9)
    ids, dist, table = coursed_stones(n, 77, rows=(10, 12), mortar=(7, 11))
    fine = tile_noise(n, 2.5, 41); mid = tile_noise(n, 25, 42)
    bulge = 1 - np.exp(-dist / 9)
    base = np.array([116, 112, 104], float) * (1 + (table[np.maximum(ids, 0), 1] - .5) * .25)[..., None]
    rgb = np.where((ids >= 0)[..., None], base * (1 + .08 * mid[..., None] + .05 * fine[..., None]),
                   np.array([84, 80, 70]) * (1 + .1 * fine[..., None]))
    height = np.where(ids >= 0, .3 + .65 * bulge + .05 * fine, .02)
    return rgb, height


def tin(n):
    fine = tile_noise(n, 2.5, 51); broad = tile_noise(n, 120, 52)
    yy, xx = np.mgrid[0:n, 0:n]
    seam_w = n // 4
    s = xx % seam_w
    seam = np.exp(-((s - seam_w / 2) / 3.0) ** 2)
    rgb = np.array([70, 72, 72], float) * (1 + .08 * broad[..., None] + .04 * fine[..., None]) * (1 + .25 * seam[..., None])
    rust = np.clip(tile_noise(n, 40, 53) - .45, 0, 1) * 1.6
    rgb = rgb * (1 - rust[..., None] * .4) + np.array([110, 74, 50]) * rust[..., None] * .4
    height = .5 + .5 * seam + .04 * fine
    return rgb, height


def sign(n=512):
    """Painted crossed spanner and hammer on a dark green board; no lettering."""
    img = Image.new('RGB', (n, n), (38, 52, 46))
    d = ImageDraw.Draw(img)
    d.rectangle((14, 14, n - 15, n - 15), outline=(176, 150, 98), width=10)
    c = n / 2
    def bar(angle, length, width, colour):
        import math
        a = math.radians(angle); ux, uy = math.cos(a), math.sin(a); px, py = -uy, ux
        pts = [(c - ux * length / 2 + px * width / 2, c - uy * length / 2 + py * width / 2),
               (c + ux * length / 2 + px * width / 2, c + uy * length / 2 + py * width / 2),
               (c + ux * length / 2 - px * width / 2, c + uy * length / 2 - py * width / 2),
               (c - ux * length / 2 - px * width / 2, c - uy * length / 2 - py * width / 2)]
        d.polygon(pts, fill=colour)
        return ux, uy, px, py
    metal = (214, 206, 186)
    # Spanner: shaft with an open jaw at one end and a ring at the other.
    ux, uy, px, py = bar(45, n * .62, n * .07, metal)
    ex, ey = c + ux * n * .33, c + uy * n * .33
    d.ellipse((ex - n * .085, ey - n * .085, ex + n * .085, ey + n * .085), fill=metal)
    d.ellipse((ex - n * .04 + ux * n * .04, ey - n * .04 + uy * n * .04, ex + n * .04 + ux * n * .04, ey + n * .04 + uy * n * .04), fill=(38, 52, 46))
    rx, ry = c - ux * n * .33, c - uy * n * .33
    d.ellipse((rx - n * .075, ry - n * .075, rx + n * .075, ry + n * .075), fill=metal)
    d.ellipse((rx - n * .035, ry - n * .035, rx + n * .035, ry + n * .035), fill=(38, 52, 46))
    # Hammer: wooden handle and an iron head.
    ux, uy, px, py = bar(-45, n * .6, n * .055, (150, 108, 66))
    hx, hy = c - ux * n * .3, c - uy * n * .3
    import math
    a = math.radians(45)
    head = [(hx + math.cos(a) * n * .14 + math.sin(a) * n * .05, hy + math.sin(a) * n * .14 - math.cos(a) * n * .05),
            (hx - math.cos(a) * n * .14 + math.sin(a) * n * .05, hy - math.sin(a) * n * .14 - math.cos(a) * n * .05),
            (hx - math.cos(a) * n * .14 - math.sin(a) * n * .05, hy - math.sin(a) * n * .14 + math.cos(a) * n * .05),
            (hx + math.cos(a) * n * .14 - math.sin(a) * n * .05, hy + math.sin(a) * n * .14 + math.cos(a) * n * .05)]
    d.polygon(head, fill=(120, 122, 124))
    img = img.filter(ImageFilter.GaussianBlur(.6))
    rng = np.random.default_rng(3)
    a = np.asarray(img).astype(float) * (1 + rng.normal(0, .03, (n, n, 1)))
    OUT.mkdir(parents=True, exist_ok=True)
    Image.fromarray(np.uint8(np.clip(a, 0, 255))).save(OUT / 'GarageSign.png', optimize=True)


def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--size', type=int, default=1024); args = ap.parse_args()
    n = args.size
    for name, (rgb, height), strength in (('GarageRubble', rubble(n), 6.0), ('GarageLime', rubble(n, lime=True), 4.0),
                                          ('GarageCement', cement(n), 3.0), ('GarageBoards', boards(n), 3.0),
                                          ('GarageCobble', cobble(n), 5.0), ('GarageTin', tin(n), 2.5)):
        save(name, rgb, height, strength)
    sign()
    print('GARAGE_TEXTURES_PASS', sorted(p.name for p in OUT.glob('*.png')))


if __name__ == '__main__':
    main()
