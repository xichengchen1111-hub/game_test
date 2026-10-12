"""Duty-officer placeholder look: recolour the approved Tripo station clerk's green
uniform/cap to police navy. Skin, leather and metal keep their colours.

    python tools/police/make_officer_texture.py
Reads game/Assets/Art/StationClerkTripo/BaseColor.png, writes
game/Assets/Art/PoliceOfficer/BaseColor.png. Placeholder until a dedicated
officer model is generated; the mesh and face are the clerk's.
"""
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
src = np.asarray(Image.open(ROOT / 'game/Assets/Art/StationClerkTripo/BaseColor.png').convert('RGB')).astype(np.float32) / 255
hsv = np.asarray(Image.fromarray((src * 255).astype(np.uint8)).convert('HSV')).astype(np.float32)
h, s, v = hsv[..., 0] * 360 / 255, hsv[..., 1] / 255, hsv[..., 2] / 255
green = (h > 95) & (h < 185) & (s > .10)
weight = np.clip((s - .10) / .12, 0, 1) * green
nh = np.where(green, 222.0, h)
ns = np.where(green, np.clip(s * 1.35 + .08, 0, .75), s)
nv = np.where(green, v * .92, v)
out = np.stack([nh / 360 * 255, ns * 255, nv * 255], -1).astype(np.uint8)
rgb = np.asarray(Image.fromarray(out, 'HSV').convert('RGB')).astype(np.float32) / 255
mix = src * (1 - weight[..., None]) + rgb * weight[..., None]
dest = ROOT / 'game/Assets/Art/PoliceOfficer'
dest.mkdir(parents=True, exist_ok=True)
Image.fromarray((mix * 255 + .5).astype(np.uint8)).save(dest / 'BaseColor.png', optimize=True)
print('OFFICER_TEXTURE_PASS', round(float(green.mean()) * 100, 1), '% texels recoloured')
