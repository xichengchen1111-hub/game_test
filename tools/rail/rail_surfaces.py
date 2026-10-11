"""Original seamless base/normal/metallic-smoothness maps, authored at metre scale.

All maps are procedural (numpy) and tile seamlessly. Base maps stay light and low in contrast:
Unity multiplies them by each material's flat colour, so the broad-colour art style is kept and
only fine surface character (joints, grain, weave, wear) is added.
"""
import numpy as np
from PIL import Image

N = 512


def _value_noise(n, cells, rng):
    grid = rng.random((cells, cells))
    t = np.arange(n) * cells / n
    i0 = np.floor(t).astype(int)
    f = t - i0
    f = f * f * (3 - 2 * f)
    i1 = (i0 + 1) % cells
    a = grid[np.ix_(i0, i0)]
    b = grid[np.ix_(i0, i1)]
    c = grid[np.ix_(i1, i0)]
    d = grid[np.ix_(i1, i1)]
    fy = f[:, None]
    fx = f[None, :]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(n, seed, cells=4, octaves=5, gain=.5):
    rng = np.random.default_rng(seed)
    total = np.zeros((n, n))
    amp = 1.0
    norm = 0.0
    for o in range(octaves):
        c = cells * (2 ** o)
        if c > n:
            break
        total += amp * _value_noise(n, c, rng)
        norm += amp
        amp *= gain
    return total / norm * 2 - 1


def save(out, name, base, height, metal, smooth, strength=2.4):
    rgb = np.clip(base, 0, 255).astype('uint8')
    if rgb.ndim == 2:
        rgb = np.repeat(rgb[..., None], 3, -1)
    Image.fromarray(rgb).save(out / (name + 'Base.png'))
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * strength
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * strength
    normal = np.stack([-dx, -dy, np.ones_like(dx)], -1)
    normal /= np.linalg.norm(normal, axis=2, keepdims=True)
    Image.fromarray(np.clip((normal * .5 + .5) * 255, 0, 255).astype('uint8')).save(out / (name + 'Normal.png'))
    mask = np.zeros((N, N, 4), dtype='uint8')
    mask[:, :, 0] = np.clip(metal, 0, 255)
    mask[:, :, 3] = np.clip(smooth, 0, 255)
    Image.fromarray(mask).save(out / (name + 'Mask.png'))


def _cells(rows, cols, stagger, seed):
    """Per-cell random value for a (staggered) block layout."""
    yy, xx = np.mgrid[:N, :N]
    row = yy * rows // N
    shift = (row % 2) * (N // cols // 2) if stagger else 0
    col = ((xx + shift) % N) * cols // N
    rng = np.random.default_rng(seed)
    table = rng.random((rows, cols))
    ry = (yy * rows) % N / N
    rx = (((xx + shift) % N) * cols) % N / N
    return table[row, col], ry, rx, row, col


def build(out):
    yy, xx = np.mgrid[:N, :N]
    px = xx * 2 * np.pi / N
    py = yy * 2 * np.pi / N
    grain = fbm(N, 11, cells=8, octaves=6)
    broad = fbm(N, 12, cells=2, octaves=4)

    # Limestone ashlar: 4 courses per tile, per-block tone, recessed joints and soft chisel speckle.
    tone, ry, rx, row, col = _cells(4, 2, True, 3101)
    joint = (ry < .045) | (rx < .022)
    edge = np.minimum(np.minimum(ry, 1 - ry) * 4, np.minimum(rx, 1 - rx) * 2)
    edge = np.clip(edge * 6, 0, 1)
    h = .16 + grain * .010 + broad * .01 - (1 - edge) * .03
    h[joint] = 0
    v = (tone - .5) * 10 + grain * 4 + broad * 3 - (1 - edge) * 6
    base = np.stack([228 + v, 223 + v, 207 + v * .9], -1)
    base[joint] = [178, 172, 160]
    save(out, 'Limestone', base, h, 0, 40 + grain * 6)

    # Floor: cream/grey-green checker with per-tile variation, grout and mild foot wear.
    tone, ry, rx, row, col = _cells(4, 4, False, 3102)
    checker = ((row + col) % 2).astype(bool)
    grout = (ry < .03) | (rx < .03)
    wear = np.clip(broad * .5 + .5, 0, 1)
    v = (tone - .5) * 8 + grain * 3
    colors = np.where(checker[..., None], np.stack([138 + v, 150 + v, 147 + v], -1), np.stack([224 + v, 219 + v, 199 + v], -1))
    colors = colors - wear[..., None] * 6
    colors[grout] = [118, 115, 106]
    height = np.where(grout, 0, .12 + grain * .004)
    save(out, 'Floor', colors, height, 0, 110 + wear * 30 + grain * 10)

    # Walnut: quarter-cut grain with flecks; brighter varnish highlights.
    wave = np.sin(px * 16 + np.sin(py * 2) * 1.4 + np.sin(px * 3) * 2 + grain * 1.5)
    fine = np.sin(px * 70 + grain * 6) * .8
    pores = (fbm(N, 13, cells=64, octaves=2) > .55) * -8
    v = wave * 9 + fine + grain * 4 + pores
    wood = np.stack([176 + v, 132 + v * .8, 88 + v * .5], -1)
    save(out, 'Walnut', wood, wave * .016 + grain * .004, 0, 120 + wave * 10)

    # Moquette: woven olive diamonds within a herringbone.
    diamond = (np.abs((xx + yy) % 64 - 32) < 3) | (np.abs((xx - yy) % 64 - 32) < 3)
    weave = ((xx % 4 == 0) * 3 + (yy % 4 == 0) * 2) + fbm(N, 14, cells=64, octaves=2) * 3
    felt = np.stack([168 + weave, 178 + weave, 140 + weave], -1)
    felt[diamond] *= .78
    save(out, 'Moquette', felt, (xx % 4 == 0) * .035 + (yy % 4 == 0) * .035 + grain * .004, 0, 16)

    # Painted steel/timber: near-flat satin with faint brush lay and soft grime toward darker patches.
    brush = np.sin(px * 90 + grain * 3) * .6
    paint = 226 + grain * 2.5 + broad * 4 + brush
    save(out, 'Paint', paint, grain * .002 + brush * .0006, 10, 150 + broad * 18 + grain * 6)

    # Cast/wrought iron: hammered mottling and darker pitting.
    pits = (fbm(N, 15, cells=32, octaves=3) > .5) * -10
    iron = 182 + grain * 8 + broad * 6 + pits
    save(out, 'Iron', iron, grain * .012 + pits * .0008, 170, 92 + grain * 14)

    # Brass: soft polish streaks, slightly patinated recesses.
    streak = np.sin(py * 40 + grain * 2) * 3
    brass = np.stack([224 + streak + grain * 3, 216 + streak + grain * 3, 190 + grain * 4], -1)
    save(out, 'Brass', brass, grain * .002, 230, 168 + streak * 4 + broad * 12)

    # Leather: pebbled grain with creased patches.
    pebble = fbm(N, 16, cells=48, octaves=3)
    leather = 186 + pebble * 9 + broad * 6
    save(out, 'Leather', leather, pebble * .02, 0, 70 + broad * 12)

    # Slate: overlapping courses with staggered joints, per-slate tone.
    tone, ry, rx, row, col = _cells(8, 4, True, 3103)
    joint = (ry < .05) | (rx < .015)
    v = (tone - .5) * 18 + grain * 4
    slate = 204 + v
    slate[joint] = 150
    save(out, 'Slate', slate, np.where(joint, 0, .09 + ry * .03) + grain * .006, 0, 46 + tone * 18)

    # --- New sets (2026-10-10 beautification) -------------------------------------------
    # Granite base course: dark speckle with rare large courses.
    speck = fbm(N, 21, cells=96, octaves=2)
    tone, ry, rx, row, col = _cells(2, 1, True, 3104)
    joint = (ry < .02) | (rx < .01)
    v = speck * 22 + (tone - .5) * 10 + broad * 5
    granite = np.stack([210 + v, 208 + v, 206 + v], -1)
    granite[joint] = [140, 138, 136]
    save(out, 'Granite', granite, np.where(joint, 0, .1) + speck * .01, 0, 70 + speck * 25)

    # Brick: running bond, 20 courses per tile, per-brick colour and recessed lime mortar.
    tone, ry, rx, row, col = _cells(20, 6, True, 3105)
    mortar = (ry < .16) | (rx < .04)
    v = (tone - .5) * 34 + grain * 6
    brick = np.stack([200 + v, 160 + v * .8, 140 + v * .6], -1)
    brick[mortar] = [222, 214, 196]
    save(out, 'Brick', brick, np.where(mortar, 0, .14) + grain * .01, 0, 30 + grain * 8)

    # Painted tongue-and-groove ceiling boards (joints parallel to the slope).
    boards = 8
    bx = (xx * boards) % N / N
    groove = bx < .05
    tone = np.random.default_rng(3106).random(boards)[(xx * boards // N)]
    v = (tone - .5) * 6 + grain * 2
    ceiling = 230 + v
    ceiling = np.where(groove, 182, ceiling)
    save(out, 'Boards', ceiling, np.where(groove, 0, .08) + grain * .002, 0, 70 + grain * 6)

    # Zinc sheet roofing: standing seams and soft oxidised blooms.
    seams = 4
    sx = (xx * seams) % N / N
    seam = sx < .04
    bloom = fbm(N, 22, cells=6, octaves=4)
    zinc = 214 + bloom * 12 + grain * 3
    zinc = np.where(seam, 238, zinc)
    save(out, 'Zinc', zinc, np.where(seam, .2, .05) + bloom * .004, 90, 96 + bloom * 30)

    # Carpet runner: medallions and diamond lattice in faded wine and gold.
    d = np.abs((xx % 128) - 64) + np.abs((yy % 128) - 64)
    medallion = d < 22
    ring = (d > 38) & (d < 44)
    lattice = (np.abs((xx + yy) % 128 - 64) < 3) | (np.abs((xx - yy) % 128 - 64) < 3)
    tuft = fbm(N, 23, cells=128, octaves=2) * 6
    carpet = np.stack([196 + tuft, 172 + tuft, 160 + tuft], -1)
    carpet[lattice] = [226, 196, 140]
    carpet[ring] = [226, 196, 140]
    carpet[medallion] = [150, 120, 110]
    save(out, 'Carpet', carpet, tuft * .003, 0, 10)

    # Velvet curtain: vertical folds with sheen on the crests.
    fold = np.sin(px * 10 + np.sin(py * 2) * .4 + grain * .6)
    velvet = 196 + fold * 26 + grain * 4
    save(out, 'Velvet', velvet, fold * .05, 0, 60 + (fold > .6) * 40)

    # Gangway bellows: horizontal pleats in coated canvas.
    pleat = np.sin(py * 24)
    bellows = 196 + pleat * 24 + grain * 5
    save(out, 'Bellows', bellows, pleat * .06, 0, 40 + (pleat > .7) * 30)

    # Kraft parcel paper with fibres.
    fibre = fbm(N, 24, cells=128, octaves=2) * 8 + np.sin(px * 3 + grain * 4) * 3
    paper = np.stack([226 + fibre, 214 + fibre, 196 + fibre], -1)
    save(out, 'Paper', paper, fibre * .002, 0, 20)

    # Platform flagstones: 4 courses of irregular-length flags, per-flag tone and worn edges.
    tone, ry, rx, row, col = _cells(4, 3, True, 3107)
    joint = (ry < .035) | (rx < .02)
    edge = np.clip(np.minimum(np.minimum(ry, 1 - ry) * 3, np.minimum(rx, 1 - rx) * 2) * 8, 0, 1)
    v = (tone - .5) * 16 + grain * 6 + broad * 5 - (1 - edge) * 8
    flags = np.stack([206 + v, 203 + v, 196 + v], -1)
    flags[joint] = [128, 126, 122]
    save(out, 'Flagstone', flags, np.where(joint, 0, .12) + grain * .008 - (1 - edge) * .02, 0, 60 + broad * 20 + (1 - edge) * 10)

    # Coping stone: long dressed slabs (joints every 0.9m at the platform tiling).
    tone, ry, rx, row, col = _cells(1, 2, False, 3108)
    joint = rx < .01
    v = (tone - .5) * 12 + grain * 5
    coping = np.stack([214 + v, 210 + v, 200 + v], -1)
    coping[joint] = [130, 128, 124]
    save(out, 'Coping', coping, np.where(joint, 0, .1) + grain * .006, 0, 70 + grain * 10)
    print('RAIL_SURFACES_PASS: twenty original seamless surface sets')
