"""Package actual native-map captures with exact, phased verification provenance."""
from pathlib import Path
import hashlib
import json
import subprocess
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output/demo/full-map'
def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))
def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()
def write(path, data):
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')

checks = {}
for filename in ('baseline-results.json', 'pre-cap-results.json', 'cap-fix-results.json', 'door-fix-results.json'):
    rows = read(ROOT / 'builds/review/full-map' / filename)
    if isinstance(rows, dict): rows = [rows]
    for row in rows:
        assert row['passed'], row['name']
        checks[row['name']] = row
assert len(checks) == 22, f'Incomplete native suite: {len(checks)}/22'
assembly = ROOT / 'builds/windows/TownDemo_Data/Managed/Assembly-CSharp.dll'
final_hash = sha(assembly)
for name, row in checks.items():
    log = Path(row['log'])
    text = log.read_text(encoding='utf-8-sig')
    assert 'PASS' in text and 'REVIEW_FAIL' not in text and 'Exception:' not in text, name
    row['logSha256'] = sha(log)
    row['log'] = str(log.relative_to(ROOT)).replace('\\', '/')
    row['captures'] = str(Path(row['captures']).relative_to(ROOT)).replace('\\', '/')
assert all(checks[n]['assemblySha256'] == final_hash for n in (
    'milk-720','milk-relaunch','milk-1080','milk-1080-relaunch','map-relaunch'))
normal = Path.home() / 'AppData/LocalLow/Qingchuan Studio/错日证词 · 暮色小镇/slice-save.json'
normal_hash = sha(normal)
assert normal_hash == 'FDE6BCE28F10294A989DC5052A88111F08855E624BC756BEFD3A1BB326B58839'
caps = read(OUT / 'tower-surface-separation.json')
assert caps['passed'] and caps['remainingTriangles'] == 0 and caps['collisionUnchanged']
door = read(ROOT / 'assets/source/milkshop/verification/door-joinery-repair.json')
assert door['passed'] and door['reopened'] and door['originalOpeningAndCollisionUnchanged']
assert sha(ROOT / 'assets/source/milkshop/MilkShop1931.blend') == door['newSourceSha256'].upper()

panels = [
    ('map-1080/full-town-overview.png', '整图布局', '410×277米 · 70处宅地 · 未完成建筑保留占位'),
    ('map-1080/station-road-night.png', '沿街照明', '既有铺装与灯型扩展 · 附近最多12盏路灯照明'),
    ('shop-720/unity-exterior.png', '钟表铺新点位', '保留原尺寸、实体门、透明窗和店内老板'),
    ('milk-1080/milk-a-interior.png', '牛奶屋内部', '灯具、柜台订单与交互保留 · 无转场进出'),
    ('tower-night/tower-upper-stone-floor.png', '钟楼机房修复', '去除24个与地板重合的墙顶面 · 碰撞不变'),
    ('dialogue-720/dialogue-greeting.png', '送奶少年新点位', '实际模型正面朝向街道 · 正常交互射线复验'),
]
font = lambda size: ImageFont.truetype('C:/Windows/Fonts/msyh.ttc', size)
canvas = Image.new('RGB', (1920, 920), '#e9e2d5')
draw = ImageDraw.Draw(canvas)
draw.text((22, 12), '青川 · 全镇地图接入', font=font(30), fill='#3e454e')
draw.text((1410, 20), 'Unity 实际运行画面 / 720p · 1080p', font=font(16), fill='#6b7075')
for index, (name, title, caption) in enumerate(panels):
    x, y = (index % 3)*640, 64+(index//3)*418
    source = Image.open(OUT / name).convert('RGB')
    canvas.paste(source.resize((628,353), Image.Resampling.LANCZOS), (x+6,y))
    draw.text((x+12,y+358), title, font=font(21), fill='#3e454e')
    draw.text((x+12,y+387), caption, font=font(15), fill='#6b7075')
canvas.save(OUT / 'full-map-comparison.jpg', quality=93)

motion = []
for case, pattern in [('milk-720','window-motion-*'),('tower-night','tower-motion-*'),('tower-720','tower-motion-*'),
                      ('milk-720','door-motion-*'),('milk-1080','door-motion-*')]:
    for folder in sorted((OUT / case).glob(pattern)):
        data = read(folder / 'motion.json')
        values = data.get('framePatchLuminance', data.get('luminance'))
        assert len(values) >= 10 and data.get('movementMetres',data.get('metres')) > .7
        clip = folder.parent / (folder.name+'.mp4')
        subprocess.run(['ffmpeg','-v','error','-y','-framerate','10','-i',str(folder/'%03d.png'),
                        '-frames:v',str(len(values)),'-c:v','libx264','-pix_fmt','yuv420p','-movflags','+faststart',str(clip)],check=True)
        motion.append(dict(case=case,surface=folder.name,frames=len(values),metres=data.get('movementMetres',data.get('metres')),
                           luminanceRange=max(values)-min(values),clip=str(clip.relative_to(ROOT)).replace('\\','/')))
        contact = Image.new('RGB',(1536,936),'#161b22')
        contact_draw = ImageDraw.Draw(contact)
        for index in range(len(values)):
            source = Image.open(folder / f'{index:03d}.png').convert('RGB')
            x, y = (index%4)*384, (index//4)*234
            contact.paste(source.resize((384,216),Image.Resampling.LANCZOS),(x,y))
            contact_draw.text((x+5,y+216),f'{case} / {folder.name} / {index:03d}',font=font(11),fill='white')
        contact.save(folder.parent / (folder.name+'-contact.jpg'),quality=93)
write(OUT/'motion-results.json',motion)
write(OUT/'acceptance.json',dict(passed=True,unity='6000.3.18f1',urp='17.0.1',hardware='RTX 5060 Laptop / Windows D3D11',
    finalAssemblySha256=final_hash,buildBytes=sum(p.stat().st_size for p in (ROOT/'builds/windows').rglob('*') if p.is_file()),
    source=read(OUT/'map-compile.json')['source'],checks=list(checks.values()),normalSaveUnchanged=True,normalSaveSha256=normal_hash,
    phases='Map/environment checked before focused test-staging repairs. Day/shop/time/dairy repeated after staging fix. Tower visual-cap removal and imported NPC-facing checks repeated on cap-fix assembly. The final dairy door-joinery fix repeated both resolutions, window motion, doorway/interactions and separate milk/full-map reloads on the final assembly. Earlier unchanged tests retain their exact assembly hashes.',
    scope='Six exterior visit orders on real clear footways; station/home interiors, historical NPC schedules and vehicle turns unimplemented. Scripted native controller and normal interaction rays; no physical keyboard, blind playtest, Mac validation or low-end FPS measurement.',
    imageHandling='Actual full frames resized and labeled without recoloring or AI editing. Motion clips sampled at 10 Hz, not game FPS.',
    comparisonSources=[p[0] for p in panels],towerSurfaceSeparation=caps,milkDoorJoinery=door))
print(f'FULL_MAP_ARTIFACTS_PASS: {len(checks)} native cases, {len(motion)} motion clips, exact phased hashes and unchanged normal save')
