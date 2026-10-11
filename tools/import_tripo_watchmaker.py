"""Preserve a Tripo export, prepare Unity textures and run the authored seated-rig pipeline."""
from pathlib import Path
import argparse, hashlib, json, shutil, subprocess, zipfile
from PIL import Image, ImageOps

p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--archive',type=Path,required=True)
p.add_argument('--blender',type=Path,required=True)
a=p.parse_args()
root=Path(__file__).resolve().parents[1]
source=root/'assets/source/watchmaker/tripo'
original=source/'original'
art=root/'game/Assets/Art/WatchmakerTripo'
original.mkdir(parents=True,exist_ok=True)
art.mkdir(parents=True,exist_ok=True)
archive=original/a.archive.name
if a.archive.resolve()!=archive.resolve():shutil.copy2(a.archive,archive)
with zipfile.ZipFile(archive) as z:
    for info in z.infolist():
        target=(original/info.filename).resolve()
        if not target.is_relative_to(original.resolve()):raise ValueError('Archive path escapes source folder')
    z.extractall(original)
texture=next(original.glob('*.fbm'))
for pattern,name in [('*BaseColor*','BaseColor.png'),('*Normal_Bake*','Normal.png')]:
    image=Image.open(next(texture.glob(pattern))).convert('RGB')
    image.thumbnail((2048,2048));image.save(art/name)
metal=Image.open(next(texture.glob('*metallic*'))).convert('L')
rough=Image.open(next(texture.glob('*roughness*'))).convert('L')
metal.thumbnail((2048,2048));rough.thumbnail((2048,2048))
Image.merge('RGBA',(metal,metal,metal,ImageOps.invert(rough))).save(art/'MetallicSmoothness.png')
for tool in ['build_tripo_watchmaker.py','verify_tripo_watchmaker.py']:
    subprocess.run([str(a.blender),'-b','--python-exit-code','1','--python',str(root/'tools'/tool)],check=True)
report={'archive':archive.name,'archiveSHA256':hashlib.sha256(archive.read_bytes()).hexdigest(),
        'tripoAsset':'373ab2a6-bf3d-43a4-9c4a-a20d63e69697','originalPreserved':True,
        'license':'No license file supplied in export; no inherited Meshy or Toon Boy license claim.',
        'source':'Tripo image-to-model and 10000-quad retopology. Automatic humanoid rig rejected; locally authored rig, skinning, repair props and three clips.',
        'unity':'Generic playback preserves authored contact; TripoWatchmakerAsset also validates and saves a reusable Humanoid Avatar.'}
(source/'import-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
