"""Original procedural textures and short sounds for the D police station.

Standard library only (zlib/struct/wave/multiprocessing), so it runs on any
host including the Mac mini's system Python 3.9. Deterministic: fixed seeds.

    python3 tools/police/make_police_assets.py OUTPUT_DIR [--size 1024] [--jobs N]

Writes OUTPUT_DIR/Textures/*.png (tileable base colour + normal maps and one
illustration) and OUTPUT_DIR/Sounds/*.wav. Copy into game/Assets/Art/Police/.
"""
import argparse, math, os, random, struct, sys, wave, zlib
from multiprocessing import Pool

# ---------------------------------------------------------------------------
# PNG / noise helpers
# ---------------------------------------------------------------------------
def write_png(path, width, height, rows):
    """rows: list of bytearrays (RGB)."""
    raw = b''.join(b'\x00' + bytes(r) for r in rows)
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
    png += chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b'')
    with open(path, 'wb') as f:
        f.write(png)


class Noise:
    """Tileable value noise on an integer lattice of the given period."""
    def __init__(self, seed):
        rnd = random.Random(seed)
        self.table = [rnd.random() for _ in range(4096)]
        self.perm = list(range(4096)); rnd.shuffle(self.perm)

    def lattice(self, x, y):
        return self.table[self.perm[(x * 73 + self.perm[y & 4095]) & 4095]]

    def value(self, u, v, period):
        x, y = u * period, v * period
        x0, y0 = int(math.floor(x)), int(math.floor(y))
        fx, fy = x - x0, y - y0
        sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        x0 %= period; y0 %= period; x1 = (x0 + 1) % period; y1 = (y0 + 1) % period
        a, b = self.lattice(x0, y0), self.lattice(x1, y0)
        c, d = self.lattice(x0, y1), self.lattice(x1, y1)
        return (a + (b - a) * sx) + ((c + (d - c) * sx) - (a + (b - a) * sx)) * sy

    def fbm(self, u, v, period, octaves=4, gain=.5):
        total, amp, norm = 0.0, 1.0, 0.0
        for o in range(octaves):
            total += amp * self.value(u, v, period << o)
            norm += amp; amp *= gain
        return total / norm


def clamp(v):
    return 0 if v < 0 else 255 if v > 255 else int(v)


def hexrgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def normal_rows(height, size, strength):
    rows = []
    for y in range(size):
        row = bytearray(size * 3)
        up, down = height[(y - 1) % size], height[(y + 1) % size]
        mid = height[y]
        for x in range(size):
            dx = (mid[(x + 1) % size] - mid[(x - 1) % size]) * strength
            dy = (down[x] - up[x]) * strength
            nz = 1.0 / math.sqrt(dx * dx + dy * dy + 1.0)
            row[x * 3] = clamp((-dx * nz * .5 + .5) * 255)
            row[x * 3 + 1] = clamp((dy * nz * .5 + .5) * 255)
            row[x * 3 + 2] = clamp((nz * .5 + .5) * 255)
        rows.append(row)
    return rows


# ---------------------------------------------------------------------------
# Material generators: each returns (colour rows, height rows)
# ---------------------------------------------------------------------------
def plaster(size, seed=11):
    n, m = Noise(seed), Noise(seed + 1)
    col, hei = [], []
    for y in range(size):
        v = y / size; row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size
            low = n.fbm(u, v, 3, 3)
            grain = m.fbm(u, v, 48, 3)
            streak = n.value(u * .25 + v * .06, v * 2.5, 8)
            l = .86 + (low - .5) * .10 + (grain - .5) * .07 + (streak - .5) * .04
            row[x * 3:x * 3 + 3] = bytes((clamp(l * 250), clamp(l * 247), clamp(l * 240)))
            h[x] = grain * .6 + low * .4
        col.append(row); hei.append(h)
    return col, hei


def ashlar(size, seed=21):
    """1.2m tile: courses 0.3m, blocks 0.6m, staggered."""
    n, m = Noise(seed), Noise(seed + 5)
    base = hexrgb('777a78'); rnd = random.Random(seed)
    tint = {}
    col, hei = [], []
    for y in range(size):
        v = y / size; course = int(v * 4); row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size; shift = .25 if course % 2 else 0
            bu = (u + shift) % 1.0; block = int(bu * 2)
            key = (course, block)
            if key not in tint:
                tint[key] = (rnd.uniform(-.07, .07), rnd.uniform(-.02, .02))
            ju = min(bu * 2 % 1.0, 1 - bu * 2 % 1.0) * .6; jv = min(v * 4 % 1.0, 1 - v * 4 % 1.0) * .3
            joint = min(ju, jv)
            g = m.fbm(u, v, 32, 4); s = n.fbm(u, v, 6, 3)
            l = 1 + tint[key][0] + (g - .5) * .16 + (s - .5) * .08
            if joint < .006:
                l *= .62; hh = .0
            else:
                hh = .55 + g * .35 - max(0, .02 - joint) * 8
            row[x * 3:x * 3 + 3] = bytes((clamp(base[0] * l * (1 + tint[key][1])), clamp(base[1] * l), clamp(base[2] * l * 1.02)))
            h[x] = hh
        col.append(row); hei.append(h)
    return col, hei


def slate(size, seed=31):
    """1.5m tile: 0.1875m exposed courses (8 rows), slates 0.25m wide."""
    n = Noise(seed); rnd = random.Random(seed); tint = {}
    col, hei = [], []
    for y in range(size):
        v = y / size; c = int(v * 8); f = v * 8 % 1.0
        row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size; shift = .5 / 6 if c % 2 else 0
            su = (u + shift) % 1.0; s = int(su * 6); fu = su * 6 % 1.0
            key = (c, s)
            if key not in tint:
                tint[key] = rnd.uniform(-.12, .12)
            g = n.fbm(u, v, 40, 3)
            edge = fu < .025 or fu > .975
            shade = .78 + .22 * f          # each course darker at its lower edge
            l = (1 + tint[key] + (g - .5) * .14) * shade * (.55 if edge else 1)
            row[x * 3:x * 3 + 3] = bytes((clamp(58 * l), clamp(64 * l), clamp(72 * l)))
            h[x] = f * .7 + g * .2 - (.4 if edge else 0)
        col.append(row); hei.append(h)
    return col, hei


def oak(size, seed=41, hexcol='9a805f', board=.07, length=(.3, .6, 1.0)):
    """Boards run along U; 'board' is the board width as a fraction of the tile."""
    n, m = Noise(seed), Noise(seed + 9); rnd = random.Random(seed)
    base = hexrgb(hexcol); rows_n = int(round(1 / board)); joints = []
    for r in range(rows_n):
        cuts, pos = [], rnd.uniform(0, .5)
        while pos < 1:
            cuts.append(pos); pos += rnd.choice(length)
        joints.append(sorted(c % 1.0 for c in cuts))
    tints = {}
    col, hei = [], []
    for y in range(size):
        v = y / size; r = int(v * rows_n); fv = v * rows_n % 1.0
        row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size
            cuts = joints[r]; seg = sum(1 for cc in cuts if cc <= u)
            key = (r, seg % max(len(cuts), 1))
            if key not in tints:
                tints[key] = (rnd.uniform(-.10, .10), rnd.uniform(0, 1))
            grain = m.value(u * 1.0 + tints[key][1], v * rows_n * 3.0 + n.value(u, v, 6) * 1.5, 64)
            fine = n.fbm(u, v, 96, 2)
            l = 1 + tints[key][0] + (grain - .5) * .22 + (fine - .5) * .06
            gap = fv < .03 or fv > .97 or any(abs(u - cc) < .0015 for cc in cuts)
            if gap:
                l *= .5
            row[x * 3:x * 3 + 3] = bytes((clamp(base[0] * l), clamp(base[1] * l), clamp(base[2] * l)))
            h[x] = (.2 if gap else .7) + grain * .15
        col.append(row); hei.append(h)
    return col, hei


def quarry_tiles(size, seed=51):
    """1.0m tile: 4x4 checker of 0.25m red/buff quarry tiles with grout."""
    n = Noise(seed); rnd = random.Random(seed); tint = {}
    red, buff = hexrgb('8a4a36'), hexrgb('c9b994')
    col, hei = [], []
    for y in range(size):
        v = y / size; ty = int(v * 4); fv = v * 4 % 1.0
        row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size; tx = int(u * 4); fu = u * 4 % 1.0
            key = (tx, ty)
            if key not in tint:
                tint[key] = rnd.uniform(-.08, .08)
            base = red if (tx + ty) % 2 else buff
            g = n.fbm(u, v, 24, 3)
            grout = fu < .02 or fu > .98 or fv < .02 or fv > .98
            wear = n.fbm(u, v, 4, 2)
            l = (1 + tint[key] + (g - .5) * .12) * (.95 + wear * .1)
            if grout:
                c = (96, 90, 82)
            else:
                c = (base[0] * l, base[1] * l, base[2] * l)
            row[x * 3:x * 3 + 3] = bytes(clamp(k) for k in c)
            h[x] = .2 if grout else .7 + g * .1
        col.append(row); hei.append(h)
    return col, hei


def flagstones(size, seed=61):
    """3.0m tile: courses of 0.45-0.6m, stones 0.5-0.95m, buff grey."""
    n, m = Noise(seed), Noise(seed + 3); rnd = random.Random(seed)
    rows_h, total = [], 0.0
    while total < 3.0:
        hgt = rnd.choice((.45, .5, .55, .6)); rows_h.append(hgt); total += hgt
    scale = 3.0 / total; rows_h = [r * scale / 3.0 for r in rows_h]
    edges = [0.0]
    for r in rows_h:
        edges.append(edges[-1] + r)
    cuts = []
    for _ in rows_h:
        pos, cc = rnd.uniform(0, .2), []
        while pos < 1:
            cc.append(pos); pos += rnd.uniform(.17, .32)
        cuts.append(sorted(c % 1.0 for c in cc))
    tint = {}
    col, hei = [], []
    for y in range(size):
        v = y / size; r = max(i for i in range(len(rows_h)) if edges[i] <= v)
        fv0, fv1 = v - edges[r], edges[r + 1] - v
        row = bytearray(size * 3); h = [0.0] * size
        for x in range(size):
            u = x / size; seg = sum(1 for cc in cuts[r] if cc <= u)
            key = (r, seg % max(len(cuts[r]), 1))
            if key not in tint:
                tint[key] = (rnd.uniform(-.1, .1), rnd.uniform(-.03, .03))
            g = m.fbm(u, v, 40, 4); s = n.fbm(u, v, 8, 3)
            joint = min(fv0, fv1, min(abs(u - cc) for cc in cuts[r]))
            l = 1 + tint[key][0] + (g - .5) * .18 + (s - .5) * .1
            if joint < .0022:
                l *= .55
            row[x * 3:x * 3 + 3] = bytes((clamp(146 * l * (1 + tint[key][1])), clamp(140 * l), clamp(129 * l)))
            h[x] = (.15 if joint < .0022 else .6) + g * .3
        col.append(row); hei.append(h)
    return col, hei


TEXTURES = {
    'PolicePlaster': (plaster, 3.0),
    'PoliceAshlar': (ashlar, 6.0),
    'PoliceSlate': (slate, 5.0),
    'PoliceOak': (lambda s: oak(s, 41, '9a805f', 1 / 14, (.3, .6, 1.0)), 4.0),
    'PoliceDarkOak': (lambda s: oak(s, 43, '5e4632', 1 / 6, (1.0,)), 3.0),
    'PoliceTiles': (quarry_tiles, 3.0),
    'PoliceFlags': (flagstones, 5.0),
}


def build_texture(job):
    name, out, size = job
    fn, strength = TEXTURES[name]
    col, hei = fn(size)
    write_png(os.path.join(out, name + '_Base.png'), size, size, col)
    write_png(os.path.join(out, name + '_Normal.png'), size, size, normal_rows(hei, size, strength))
    return name


def letter_photo(out):
    """A sepia print of a sheet whose top and bottom were trimmed: illegible handwriting only."""
    w, h = 768, 576; rnd = random.Random(7); n = Noise(77)
    rows = []
    for y in range(h):
        row = bytearray(w * 3)
        for x in range(w):
            u, v = x / w, y / h
            g = n.fbm(u, v, 16, 3)
            border = x < 28 or x > w - 29 or y < 28 or y > h - 29
            if border:
                c = (226, 214, 188)
            else:
                c = (92 + g * 24, 78 + g * 20, 60 + g * 16)      # dark tabletop
                if 170 < x < 600 and 120 < y < 470:              # trimmed sheet
                    c = (196 + g * 20, 180 + g * 18, 150 + g * 14)
            row[x * 3:x * 3 + 3] = bytes(clamp(k) for k in c)
        rows.append(row)
    # Pen strokes: wavy illegible lines inside the sheet, no letterforms.
    for line in range(8):
        y0 = 150 + line * 38 + rnd.uniform(-3, 3); x = 196.0
        end = 575 - rnd.uniform(0, 90) if line != 7 else 400
        while x < end:
            width = rnd.uniform(14, 46); amp = rnd.uniform(3, 7); freq = rnd.uniform(.25, .5)
            for k in range(int(width)):
                yy = int(y0 + math.sin((x + k) * freq) * amp * (.5 + .5 * math.sin(k * .2)))
                xx = int(x + k)
                for t in (0, 1):
                    if 0 <= yy + t < h and 0 <= xx < w:
                        rows[yy + t][xx * 3:xx * 3 + 3] = bytes((70, 56, 44))
            x += width + rnd.uniform(6, 14)
    # Clean straight trimmed edges with a faint cast shadow.
    for x in range(170, 600):
        for yy in (470, 471, 472):
            rows[yy][x * 3:x * 3 + 3] = bytes((60, 50, 40))
    write_png(os.path.join(out, 'PoliceLetterPhoto.png'), w, h, rows)


# ---------------------------------------------------------------------------
# Sounds
# ---------------------------------------------------------------------------
RATE = 44100


def write_wav(path, samples):
    peak = max(1e-6, max(abs(s) for s in samples))
    scale = .85 / peak
    with wave.open(path, 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(RATE)
        w.writeframes(b''.join(struct.pack('<h', int(max(-1, min(1, s * scale)) * 32767)) for s in samples))


def lowpass(samples, cutoff):
    a = math.exp(-2 * math.pi * cutoff / RATE); y, out = 0.0, []
    for s in samples:
        y = (1 - a) * s + a * y; out.append(y)
    return out


def highpass(samples, cutoff):
    low = lowpass(samples, cutoff)
    return [s - l for s, l in zip(samples, low)]


def door_wood(opening, seed):
    rnd = random.Random(seed); n = int(RATE * (.95 if opening else .55)); out = [0.0] * n
    if opening:
        # Latch click, then a slow hinge creak (stick-slip pulses through a resonance).
        for i in range(int(RATE * .012)):
            out[i] += rnd.uniform(-1, 1) * math.exp(-i / (RATE * .002))
        t0 = int(RATE * .08); phase = 0.0
        for i in range(t0, n):
            t = (i - t0) / RATE; f = 340 + 160 * math.sin(t * 3.1) + 60 * t
            phase += f / RATE
            env = min(1, t / .08) * math.exp(-t * 2.4)
            pulse = 1.0 if (phase % 1.0) < .18 else 0.0
            out[i] += pulse * env * .5 + rnd.uniform(-1, 1) * env * .05
        out = lowpass(highpass(out, 180), 3800)
    else:
        # Wooden thud (low body mode) plus latch tick.
        for i in range(n):
            t = i / RATE
            out[i] = (math.sin(2 * math.pi * 92 * t) * .9 + math.sin(2 * math.pi * 178 * t) * .4) * math.exp(-t * 22)
            out[i] += rnd.uniform(-1, 1) * math.exp(-t * 60) * .4
        k = int(RATE * .06)
        for i in range(int(RATE * .01)):
            out[k + i] += rnd.uniform(-1, 1) * .5 * math.exp(-i / (RATE * .0015))
        out = lowpass(out, 2600)
    return out


def iron_gate(seed):
    rnd = random.Random(seed); n = int(RATE * 1.3); out = [0.0] * n
    partials = [(523, 1.0, 5.0), (1187, .6, 7.0), (1730, .45, 9.0), (2650, .3, 12.0), (311, .5, 4.0)]
    for start, gain in ((0, 1.0), (int(RATE * .09), .45), (int(RATE * .16), .25)):
        for i in range(start, n):
            t = (i - start) / RATE
            out[i] += gain * sum(a * math.sin(2 * math.pi * f * t) * math.exp(-t * d) for f, a, d in partials) * .3
            if t < .01:
                out[i] += gain * rnd.uniform(-1, 1) * (1 - t / .01)
    rattle = lowpass([rnd.uniform(-1, 1) for _ in range(n)], 1800)
    for i in range(n):
        t = i / RATE
        out[i] += rattle[i] * .25 * math.exp(-t * 6) * (.5 + .5 * math.sin(t * 90))
    return highpass(out, 120)


def drawer(seed):
    rnd = random.Random(seed); n = int(RATE * .55)
    noise = [rnd.uniform(-1, 1) for _ in range(n)]
    body = lowpass(highpass(noise, 300), 1600)
    out = []
    for i, s in enumerate(body):
        t = i / RATE; env = min(1, t / .04) * (1 if t < .38 else max(0, 1 - (t - .38) / .1))
        out.append(s * env * (.7 + .3 * math.sin(t * 140)))
    k = int(RATE * .44)
    for i in range(int(RATE * .09)):
        t = i / RATE
        out[k + i] += math.sin(2 * math.pi * 140 * t) * math.exp(-t * 40) * .8
    return out


def paper(seed):
    rnd = random.Random(seed); n = int(RATE * .5); out = [0.0] * n
    noise = highpass([rnd.uniform(-1, 1) for _ in range(n)], 1500)
    for burst in range(5):
        start = int(RATE * (.02 + burst * .08 + rnd.uniform(0, .03))); length = int(RATE * rnd.uniform(.04, .09))
        for i in range(length):
            if start + i < n:
                t = i / length
                out[start + i] += noise[start + i] * math.sin(math.pi * t) ** 2 * rnd.uniform(.6, 1.0)
    return lowpass(out, 7500)


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('output'); ap.add_argument('--size', type=int, default=1024)
    ap.add_argument('--jobs', type=int, default=os.cpu_count() or 4)
    a = ap.parse_args()
    tex = os.path.join(a.output, 'Textures'); snd = os.path.join(a.output, 'Sounds')
    os.makedirs(tex, exist_ok=True); os.makedirs(snd, exist_ok=True)
    with Pool(min(a.jobs, len(TEXTURES))) as pool:
        for name in pool.imap_unordered(build_texture, [(k, tex, a.size) for k in TEXTURES]):
            print('texture', name, flush=True)
    letter_photo(tex); print('texture PoliceLetterPhoto', flush=True)
    write_wav(os.path.join(snd, 'PoliceDoorOpen.wav'), door_wood(True, 3))
    write_wav(os.path.join(snd, 'PoliceDoorClose.wav'), door_wood(False, 4))
    write_wav(os.path.join(snd, 'PoliceIronDoor.wav'), iron_gate(5))
    write_wav(os.path.join(snd, 'PoliceDrawer.wav'), drawer(6))
    write_wav(os.path.join(snd, 'PolicePaper.wav'), paper(8))
    print('POLICE_ASSETS_PASS', a.size, flush=True)


if __name__ == '__main__':
    main()
