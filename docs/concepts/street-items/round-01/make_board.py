"""Compose the review board and JPG copies from the Blender renders.

python make_board.py <render_dir> <round_dir>
"""
import json, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

SRC, DST = Path(sys.argv[1]), Path(sys.argv[2])
F = 'C:/Windows/Fonts/'
def cn(size, bold=False): return ImageFont.truetype(F + ('msyhbd.ttc' if bold else 'msyh.ttc'), size)
BG, INK, GOLD, MUTED = (24, 34, 30), (236, 230, 216), (214, 186, 132), (170, 176, 162)
ITEMS = json.loads((DST / 'design-plan.json').read_text(encoding='utf-8'))['items']

for it in ITEMS:  # clean per-item JPGs for the repo
    Image.open(SRC / f"{it['id']}.png").convert('RGB').save(DST / f"{it['id']}.jpg", quality=90, optimize=True)

# Same-scale lineup: crop the strip below the kerb, label each item by number.
pos = json.loads((SRC / 'lineup-positions.json').read_text())
lu = Image.open(SRC / 'lineup.png').convert('RGB')
px_per_m = pos['width_px'] / pos['ortho_scale']
ground_y = int(pos['height_px'] / 2 + pos['center_z'] * px_per_m)
lu = lu.crop((0, 0, lu.width, min(lu.height, ground_y + 70)))
d = ImageDraw.Draw(lu)
for it in ITEMS:
    x = (pos['items'][it['id']] - pos['center_x']) * px_per_m + pos['width_px'] / 2
    label = it['id'][:2]
    d.rounded_rectangle([x - 26, ground_y + 14, x + 26, ground_y + 60], 8, fill=BG)
    bb = d.textbbox((0, 0), label, font=cn(30, True)); d.text((x - (bb[2] - bb[0]) / 2, ground_y + 16), label, font=cn(30, True), fill=GOLD)
sx = (-0.5 - pos['center_x']) * px_per_m + pos['width_px'] / 2
d.text((sx + 14, ground_y - 250), '1.8 m\n标尺', font=cn(24), fill=INK)
lu.save(DST / 'lineup-same-scale.jpg', quality=90, optimize=True)

# Contact board: title, 5x2 tiles with number and name, lineup underneath.
tw, th, cap, gap, margin = 520, 650, 96, 18, 48
W = margin * 2 + tw * 5 + gap * 4
lh = int(lu.height * (W - margin * 2) / lu.width)
H = 150 + (th + cap + gap) * 2 + lh + 120
board = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(board)
d.text((margin, 40), '《错日证词》· 街道物件第一轮', font=cn(52, True), fill=INK)
d.text((margin, 108), '1931 · 阴天灰调 · 十件原创设计，Blender 程序建模概念渲染（非游戏资产）', font=cn(26), fill=MUTED)
for k, it in enumerate(ITEMS):
    col, row = k % 5, k // 5
    x = margin + col * (tw + gap); y = 160 + row * (th + cap + gap)
    im = Image.open(SRC / f"{it['id']}.png").convert('RGB').resize((tw, th), Image.LANCZOS)
    board.paste(im, (x, y))
    d.text((x + 4, y + th + 10), f"{it['id'][:2]}  {it['name']}", font=cn(30, True), fill=GOLD)
    d.text((x + 4, y + th + 52), it['tagline'], font=cn(22), fill=MUTED)
ly = 160 + (th + cap + gap) * 2 + 10
d.text((margin, ly), '同比例并排（正交视图，左侧 1.8m 标尺）', font=cn(28, True), fill=INK)
board.paste(lu.resize((W - margin * 2, lh), Image.LANCZOS), (margin, ly + 50))
board.save(DST / 'board.jpg', quality=88, optimize=True)
print('board', board.size, 'lineup', lu.size)
