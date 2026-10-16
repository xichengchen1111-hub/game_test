"""Original textures for the 1931 street-furniture kit (concept round docs/concepts/street-items/round-01).

python -X utf8 tools/street_items/street_item_art.py   -> game/Assets/Art/StreetItems/
Posters are fictional and carry no dates, times, places or route facts; small-bill body type is
deliberately unreadable. Detail maps (painted-metal wear, granite, wood) are tiling greyscale
albedo multipliers plus tangent-space normals; Unity tints them per material. Deterministic.
"""
import math, random, sys, zlib
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont, ImageChops

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / 'game/Assets/Art/StreetItems'
OUT.mkdir(parents=True, exist_ok=True)
F = 'C:/Windows/Fonts/' if Path('C:/Windows/Fonts/georgiab.ttf').exists() else '/System/Library/Fonts/Supplemental/'
def font(name, size):
    alias = {'georgiab.ttf': 'Georgia Bold.ttf', 'georgia.ttf': 'Georgia.ttf', 'georgiai.ttf': 'Georgia Italic.ttf', 'timesbd.ttf': 'Times New Roman Bold.ttf'}
    path = Path(F + name)
    return ImageFont.truetype(str(path if path.exists() else Path(F + alias[name])), size)
rng = random.Random(1931)

PAPER = (214, 202, 176)
INK = (52, 44, 38)
RED = (138, 52, 38)
GREEN = (58, 78, 62)
OCHRE = (176, 132, 62)
BLUE = (56, 70, 92)

def grain(im, amount=14, seed=0):
    g = np.random.default_rng(seed).normal(128, amount * 2.2, (im.size[1], im.size[0])).clip(0, 255).astype('uint8')
    noise = Image.fromarray(g, 'L').filter(ImageFilter.GaussianBlur(.6))
    base = im.convert('RGB')
    mixed = Image.merge('RGB', [ImageChops.add(c, noise, 2, -64) for c in base.split()])
    return mixed

def age(im, seed):
    """Fade, rain streaks, foxing and creases: an outdoor poster after a damp winter."""
    r = random.Random(seed)
    w, h = im.size
    im = Image.blend(im, Image.new('RGB', im.size, (200, 190, 168)), .1)
    d = ImageDraw.Draw(im, 'RGBA')
    for _ in range(int(w * .08)):  # vertical rain streaks
        x = r.randrange(w); y0 = r.randrange(-h // 4, h // 2); ln = r.randrange(h // 6, h)
        d.line([(x, y0), (x + r.randint(-3, 3), y0 + ln)], fill=(90, 80, 66, r.randint(8, 22)), width=r.randint(1, 3))
    for _ in range(40):  # foxing spots
        x, y, s = r.randrange(w), r.randrange(h), r.randint(3, 16)
        d.ellipse([x - s, y - s, x + s, y + s], fill=(120, 96, 60, r.randint(10, 30)))
    for _ in range(5):  # paste creases
        y = r.randrange(h); d.line([(0, y), (w, y + r.randint(-30, 30))], fill=(255, 250, 235, 26), width=2)
        d.line([(0, y + 3), (w, y + 3 + r.randint(-30, 30))], fill=(60, 50, 40, 30), width=2)
    return grain(im, 18, seed)

def centered(d, y, text, f, fill, w):
    bb = d.textbbox((0, 0), text, font=f)
    d.text(((w - (bb[2] - bb[0])) / 2 - bb[0], y), text, font=f, fill=fill)
    return y + (bb[3] - bb[1])

def border(d, w, h, col, inset=26, width=10):
    d.rectangle([inset, inset, w - inset, h - inset], outline=col, width=width)
    d.rectangle([inset + 18, inset + 18, w - inset - 18, h - inset - 18], outline=col, width=3)

def poster_cocoa(w, h):
    im = Image.new('RGB', (w, h), PAPER); d = ImageDraw.Draw(im)
    border(d, w, h, RED)
    centered(d, 90, 'MERIDIAN', font('georgiab.ttf', 120), RED, w)
    centered(d, 230, 'COCOA', font('georgiab.ttf', 190), INK, w)
    cx, cy = w // 2, 760  # cup with saucer and steam
    d.ellipse([cx - 260, cy + 150, cx + 260, cy + 230], fill=(170, 150, 120), outline=INK, width=6)
    d.pieslice([cx - 190, cy - 190, cx + 190, cy + 190], 0, 180, fill=(232, 222, 198), outline=INK, width=8)
    d.rectangle([cx - 190, cy - 30, cx + 190, cy], fill=(232, 222, 198))
    d.line([(cx - 190, cy - 30), (cx - 190, cy)], fill=INK, width=8); d.line([(cx + 190, cy - 30), (cx + 190, cy)], fill=INK, width=8)
    d.ellipse([cx - 190, cy - 62, cx + 190, cy + 2], fill=(92, 56, 36), outline=INK, width=8)
    d.arc([cx + 150, cy - 10, cx + 280, cy + 120], 270, 100, fill=INK, width=14)
    for k in (-80, 0, 80):
        pts = [(cx + k + 26 * math.sin(t / 9), cy - 90 - t * 3) for t in range(0, 70)]
        d.line(pts, fill=(120, 110, 96), width=9)
    centered(d, 1040, 'warms the whole family', font('georgiai.ttf', 64), INK, w)
    centered(d, 1130, 'SOLD BY GOOD GROCERS', font('georgia.ttf', 46), RED, w)
    return im

def poster_soap(w, h):
    im = Image.new('RGB', (w, h), (198, 208, 200)); d = ImageDraw.Draw(im)
    border(d, w, h, BLUE)
    centered(d, 100, 'HALCYON', font('georgiab.ttf', 124), BLUE, w)
    centered(d, 270, 'SOAP', font('timesbd.ttf', 170), INK, w)
    cx, cy = w // 2, 720
    d.rounded_rectangle([cx - 230, cy - 110, cx + 230, cy + 110], 70, fill=(236, 232, 214), outline=INK, width=8)
    d.rounded_rectangle([cx - 170, cy - 60, cx + 170, cy + 60], 40, outline=(150, 150, 140), width=5)
    f = font('georgiab.ttf', 60); bb = d.textbbox((0, 0), 'PURE', font=f); d.text((cx - (bb[2] - bb[0]) / 2, cy - 38), 'PURE', font=f, fill=BLUE)
    for _ in range(26):
        x, y, s = rng.randint(80, w - 80), rng.randint(440, 980), rng.randint(14, 46)
        if abs(x - cx) < 250 and abs(y - cy) < 130: continue
        d.ellipse([x - s, y - s, x + s, y + s], outline=(250, 250, 246), width=4)
    centered(d, 1010, 'mild  ·  gentle  ·  it floats', font('georgiai.ttf', 62), INK, w)
    centered(d, 1110, 'FOR BATH, NURSERY & LAUNDRY', font('georgia.ttf', 42), BLUE, w)
    return im

def poster_syrup(w, h):
    im = Image.new('RGB', (w, h), (222, 198, 142)); d = ImageDraw.Draw(im)
    border(d, w, h, GREEN)
    centered(d, 90, "DR. LINDEN'S", font('georgiab.ttf', 96), GREEN, w)
    centered(d, 210, 'COUGH', font('timesbd.ttf', 190), INK, w)
    centered(d, 400, 'SYRUP', font('timesbd.ttf', 150), RED, w)
    cx, cy = w // 2, 820  # bottle
    d.rectangle([cx - 34, cy - 270, cx + 34, cy - 200], fill=(70, 50, 40), outline=INK, width=6)
    d.polygon([(cx - 40, cy - 200), (cx + 40, cy - 200), (cx + 120, cy - 130), (cx - 120, cy - 130)], fill=(96, 62, 40), outline=INK)
    d.rounded_rectangle([cx - 130, cy - 135, cx + 130, cy + 200], 26, fill=(110, 70, 42), outline=INK, width=8)
    d.rectangle([cx - 100, cy - 60, cx + 100, cy + 110], fill=(236, 226, 200), outline=INK, width=5)
    for k in range(4): d.line([(cx - 76, cy - 30 + k * 34), (cx + 76, cy - 30 + k * 34)], fill=(120, 110, 96), width=8)
    centered(d, 1080, 'for winter chests', font('georgiai.ttf', 66), INK, w)
    centered(d, 1170, 'AT ALL DRUGGISTS', font('georgia.ttf', 46), GREEN, w)
    return im

def poster_flannel(w, h):
    im = Image.new('RGB', (w, h), (190, 170, 150)); d = ImageDraw.Draw(im)
    d.rectangle([0, 0, w, 360], fill=INK)
    centered(d, 70, 'WOOLCOTT', font('georgiab.ttf', 116), (226, 214, 186), w)
    centered(d, 230, 'FLANNELS', font('georgia.ttf', 96), OCHRE, w)
    cx, cy = w // 2, 760  # folded shirts in a stack
    for k, col in enumerate([(120, 70, 58), (86, 96, 110), (150, 130, 96)]):
        y = cy + 150 - k * 120
        d.rounded_rectangle([cx - 250, y - 70, cx + 250, y + 50], 18, fill=col, outline=INK, width=6)
        for s in range(-230, 240, 40): d.line([(cx + s, y - 66), (cx + s, y + 46)], fill=tuple(max(0, c - 22) for c in col), width=4)
    d.polygon([(cx - 90, cy - 150), (cx, cy - 90), (cx + 90, cy - 150), (cx + 60, cy - 90), (cx - 60, cy - 90)], fill=(226, 214, 186), outline=INK)
    centered(d, 1040, 'warm wear for cold mornings', font('georgiai.ttf', 58), INK, w)
    centered(d, 1130, 'MEN · WOMEN · CHILDREN', font('georgia.ttf', 46), INK, w)
    return im

def small_bill(w, h, title, seed):
    r = random.Random(seed)
    im = Image.new('RGB', (w, h), (222, 214, 192)); d = ImageDraw.Draw(im)
    d.rectangle([10, 10, w - 10, h - 10], outline=INK, width=4)
    centered(d, 40, title, font('timesbd.ttf', 64 if len(title) < 9 else 44), INK, w)
    for k in range(9):  # deliberately unreadable body type: no names, dates or times
        y = 150 + k * 34; x0 = 50
        while x0 < w - 60:
            ln = r.randint(18, 70); d.rectangle([x0, y, min(w - 50, x0 + ln), y + 12], fill=(96, 86, 74)); x0 += ln + 14
    return im

def torn(im, seed, edge=26):
    """Alpha-composite onto older paper with ragged torn margins."""
    r = random.Random(seed)
    w, h = im.size
    mask = Image.new('L', (w, h), 0); d = ImageDraw.Draw(mask)
    pts = []
    for x in range(0, w + 1, 18): pts.append((x, r.randint(0, edge)))
    for y in range(0, h + 1, 18): pts.append((w - r.randint(0, edge), y))
    for x in range(w, -1, -18): pts.append((x, h - r.randint(0, edge)))
    for y in range(h, -1, -18): pts.append((r.randint(0, edge), y))
    d.polygon(pts, fill=255)
    return mask.filter(ImageFilter.GaussianBlur(1.2))

def column_atlas():
    # Shaft circumference 2*pi*0.503m ~= 3.16m, height 2.2m: 4096 x 2850 keeps texels square.
    W, H = 4096, 2850
    atlas = Image.new('RGB', (W, H), (168, 158, 136))
    d = ImageDraw.Draw(atlas, 'RGBA')
    # Layers of older, pasted-over bills underneath.
    for k in range(60):
        x, y = rng.randrange(-200, W), rng.randrange(-200, H)
        sw, sh = rng.randint(300, 800), rng.randint(300, 900)
        tone = rng.randint(150, 205)
        d.rectangle([x, y, x + sw, y + sh], fill=(tone, tone - 8, tone - 26, 255))
        for j in range(rng.randint(2, 8)):
            yy = y + 40 + j * 46; d.rectangle([x + 30, yy, x + sw - 30, yy + 16], fill=(90, 80, 66, 90))
    posters = [poster_cocoa, poster_soap, poster_syrup, poster_flannel]
    pw, ph = 860, 1300
    for k, make in enumerate(posters):
        im = age(make(pw, ph), 10 + k)
        x = 80 + k * 1024; y = 560
        atlas.paste(im, (x, y), torn(im, 20 + k))
    for k, title in enumerate(['TO LET', 'PIANO LESSONS', 'NOTICE', 'FOR SALE', 'WANTED', 'LODGINGS']):
        im = age(small_bill(460, 480, title, 30 + k), 40 + k)
        x = 60 + k * 680 + rng.randint(-30, 30); y = 1960 + rng.randint(-20, 60)
        atlas.paste(im, (x, y), torn(im, 50 + k, 18))
    # Upper band: plain weathered strip under the cornice; lower band: splash grime.
    d = ImageDraw.Draw(atlas, 'RGBA')
    for y in range(H - 260, H):
        a = int(120 * (y - (H - 260)) / 260); d.line([(0, y), (W, y)], fill=(60, 54, 46, a))
    atlas = atlas.filter(ImageFilter.GaussianBlur(.5))
    atlas.resize((2048, 1425), Image.LANCZOS).save(OUT / 'SI_Posters.png')

def plate(text_lines, size, bg, fg, name, rim=(40, 40, 38)):
    w, h = size
    im = Image.new('RGB', (w, h), rim); d = ImageDraw.Draw(im)
    d.rounded_rectangle([8, 8, w - 8, h - 8], 26, fill=bg)
    d.rounded_rectangle([22, 22, w - 22, h - 22], 18, outline=fg, width=5)
    total = sum(f.size + 12 for _, f in text_lines)
    y = (h - total) / 2
    for t, f in text_lines:
        bb = d.textbbox((0, 0), t, font=f)
        d.text(((w - (bb[2] - bb[0])) / 2 - bb[0], y - bb[1]), t, font=f, fill=fg)
        y += f.size + 12
    # Enamel chips at the rim and screw holes.
    r = random.Random(zlib.crc32(name.encode()))
    for _ in range(14):
        x, yy = r.choice([r.randint(10, w - 10)]), r.choice([r.randint(8, 30), r.randint(h - 30, h - 8)])
        s = r.randint(4, 12); d.ellipse([x - s, yy - s, x + s, yy + s], fill=(34, 30, 28))
    for sx in (44, w - 44):
        d.ellipse([sx - 9, h / 2 - 9, sx + 9, h / 2 + 9], fill=(60, 58, 54), outline=(20, 20, 20), width=2)
    grain(im, 10, 7).save(OUT / name)

# ---- tiling detail maps --------------------------------------------------------------------
N = 512
def fbm(seed, scale, octaves=5):
    g = np.random.default_rng(seed); out = np.zeros((N, N)); amp = 1; tot = 0
    for o in range(octaves):
        cells = int(scale * 2 ** o)
        base = g.random((cells, cells))
        img = Image.fromarray((base * 255).astype('uint8')).resize((N, N), Image.BICUBIC) if cells < N else Image.fromarray((base * 255).astype('uint8'))
        # wrap: tile the low-res grid by resizing a 3x3 mosaic and cropping the centre
        mosaic = np.tile(base, (3, 3)); big = Image.fromarray((mosaic * 255).astype('uint8')).resize((N * 3, N * 3), Image.BICUBIC)
        layer = np.asarray(big, dtype='float32')[N:2 * N, N:2 * N] / 255
        out += layer * amp; tot += amp; amp *= .5
    return out / tot
def normal_from(height, strength):
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * strength
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * strength
    n = np.dstack([-dx, -dy, np.ones_like(height)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
    return Image.fromarray(((n * .5 + .5) * 255).astype('uint8'))
def save_pair(name, albedo, height, strength):
    Image.fromarray((albedo.clip(0, 1) * 255).astype('uint8')).convert('RGB').save(OUT / f'{name}_Base.png')
    normal_from(height, strength).save(OUT / f'{name}_Normal.png')
def detail_maps():
    # Painted metal: soft mottling, scattered paint chips showing dark iron, fine pitting.
    mottle = fbm(11, 4); chip = fbm(12, 24, 4); pit = fbm(13, 160, 2)
    chips = np.clip((chip - .66) / .03, 0, 1)
    albedo = (.86 + .14 * mottle - .1 * (pit - .5)) * (1 - chips * .78)
    save_pair('SI_Paint', albedo, mottle * .15 - chips * .5 + pit * .1, 6)
    # Granite: speckled feldspar/mica, broad clouding, tool-dressed surface.
    speck = fbm(21, 220, 2); cloud = fbm(22, 3); grainy = fbm(23, 60, 3)
    albedo = .78 + .22 * cloud - .35 * (speck < .36) + .3 * (speck > .7) - .06 * grainy
    save_pair('SI_Granite', albedo, grainy * .6 + speck * .3, 4)
    # Wood: stretched grain rings (U along the board), with weathered greying.
    g = fbm(31, 6, 4); y = np.linspace(0, 1, N, endpoint=False)[:, None]
    rings = .5 + .5 * np.sin((y * 38 + g * 3.5) * 2 * math.pi)
    albedo = .74 + .2 * rings * (.6 + .4 * fbm(32, 3)) - .08 * fbm(33, 90, 2)
    save_pair('SI_Wood', np.repeat(albedo, 1, 1), rings * .5 + fbm(34, 120, 2) * .2, 5)

if __name__ == '__main__':
    column_atlas()
    plate([('KEEP OUR', font('georgiab.ttf', 54)), ('STREETS CLEAN', font('georgiab.ttf', 54))], (720, 240), (214, 206, 182), (44, 62, 52), 'SI_PlateLitter.png')
    plate([('SHINE', font('georgiab.ttf', 110)), ('5¢', font('georgiab.ttf', 96))], (520, 400), (40, 54, 46), (222, 206, 160), 'SI_PlateShine.png', rim=(26, 26, 24))
    plate([('FIRE ALARM', font('georgiab.ttf', 58)), ('OPEN DOOR · PULL HOOK', font('georgia.ttf', 34))], (720, 260), (214, 206, 182), (130, 40, 30), 'SI_PlateAlarm.png')
    detail_maps()
    print('street item textures written to', OUT)
