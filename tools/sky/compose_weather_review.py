"""Compose unaltered runtime views, encode review-window motion, record provenance."""
from pathlib import Path
import hashlib
import json
import subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/demo/weather'
FONT = 'C:/Windows/Fonts/msyh.ttc'
panels = [
    ('town-noon-scattered', '晴空流云', '高低云层 · 阳光随云遮蔽变化'),
    ('town-sunset-scattered', '暮色日落', '暖色地平线 · 冷暖天空层次'),
    ('town-night-clear', '星月夜空', '同一时钟控制月亮 · 星光穿过云隙'),
    ('town-noon-rain', '阴雨小镇', '先积云再落雨 · 厚云柔化直射光'),
    ('town-noon-mist', '薄雾远景', '空气透视 · 远处建筑融入雾色'),
    ('milk-room-noon-rain', '雨天室内', '屋内遮雨 · 光线不足自动补灯'),
]
canvas = Image.new('RGB', (1920, 920), '#e9e2d5')
draw = ImageDraw.Draw(canvas)
font = lambda n: ImageFont.truetype(FONT, n)
draw.text((22, 12), '青川 · 天空与天气', font=font(30), fill='#3e454e')
draw.text((1430, 20), 'Unity 实际运行画面 / 1080p', font=font(18), fill='#6b7075')
for i, (name, label, caption) in enumerate(panels):
    x, y = (i % 3) * 640, 64 + (i // 3) * 418
    image = Image.open(OUT / '1080' / (name + '.png')).convert('RGB')
    image = image.resize((628, 353), Image.Resampling.LANCZOS)
    canvas.paste(image, (x + 6, y))
    draw.text((x + 12, y + 358), label, font=font(21), fill='#3e454e')
    draw.text((x + 12, y + 387), caption, font=font(16), fill='#6b7075')
canvas.save(OUT / 'sky-weather-comparison.jpg', quality=93)

motion = []
for resolution in ('1080', '720'):
    for side in ('north', 'south'):
        folder = OUT / resolution / ('window-motion-' + side)
        data = json.loads((folder / 'motion.json').read_text(encoding='utf-8-sig'))
        count = len(data['framePatchLuminance'])
        clip = OUT / resolution / ('rain-window-' + side + '.mp4')
        subprocess.run(['ffmpeg', '-v', 'error', '-y', '-framerate', '10', '-i', str(folder / '%03d.png'),
                        '-frames:v', str(count), '-c:v', 'libx264', '-pix_fmt', 'yuv420p', '-movflags', '+faststart', str(clip)], check=True)
        values = data['framePatchLuminance']
        motion.append(dict(resolution=resolution, side=side, frames=count, metres=data['movementMetres'],
                           luminanceRange=max(values)-min(values), clip=str(clip.relative_to(ROOT))))
(OUT / 'window-motion-results.json').write_text(json.dumps(motion, ensure_ascii=False, indent=2), encoding='utf-8')

checks = []
for name in ('acceptance', 'regression'):
    checks += json.loads((ROOT / 'builds/review/weather' / (name + '-results.json')).read_text(encoding='utf-8-sig'))
assert len(checks) == 12 and all(c['passed'] for c in checks)
assembly = ROOT / 'builds/windows/TownDemo_Data/Managed/Assembly-CSharp.dll'
assert all(Path(c['log']).stat().st_mtime >= assembly.stat().st_mtime for c in checks), 'A check log predates this player assembly'
normal = Path('C:/Users/jason/AppData/LocalLow/Qingchuan Studio/错日证词 · 暮色小镇/slice-save.json')
save_hash = hashlib.sha256(normal.read_bytes()).hexdigest().upper()
assert save_hash == 'FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839'
report = dict(passed=True, unity='6000.3.18f1', urp='17.0.1',
              hardware='NVIDIA GeForce RTX 5060 Laptop GPU / Windows D3D11',
              assemblySha256=hashlib.sha256(assembly.read_bytes()).hexdigest(), checks=checks,
              normalSaveUnchanged=True, normalSaveSha256=save_hash,
              scope='Rendered native Windows player, ordinary interaction rays and scripted CharacterController; isolated time/weather staging. No physical keyboard/mouse input or low-end FPS benchmark.',
              imageHandling='Comparison resizes full actual frames and adds labels; no recoloring or AI editing. Motion clips use 10 Hz capture sampling, not game FPS.')
(OUT / 'acceptance.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
print('WEATHER_ARTIFACTS_PASS: actual-frame comparison, four motion clips, twelve acceptance cases, normal save preserved')
