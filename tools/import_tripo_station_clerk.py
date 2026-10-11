"""Preserve the Tripo download, prepare Unity PBR textures, and refine the two clips."""
from pathlib import Path
import argparse, hashlib, json, shutil, subprocess, zipfile
from PIL import Image, ImageOps

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('--archive',type=Path,required=True)
parser.add_argument('--blender',type=Path,required=True)
args=parser.parse_args();root=Path(__file__).resolve().parents[1]
source=root/'assets/source/station-clerk/tripo';original=source/'original';art=root/'game/Assets/Art/StationClerkTripo'
original.mkdir(parents=True,exist_ok=True);art.mkdir(parents=True,exist_ok=True)
archive=original/args.archive.name
if args.archive.resolve()!=archive.resolve():shutil.copy2(args.archive,archive)
with zipfile.ZipFile(archive) as z:
    for item in z.infolist():
        if not (original/item.filename).resolve().is_relative_to(original.resolve()):raise ValueError('Archive path escapes source folder')
    z.extractall(original)
textures=next(original.glob('*.fbm'))
for pattern,name in [('*BaseColor*','BaseColor.png'),('*Normal_Bake*','Normal.png')]:
    image=Image.open(next(textures.glob(pattern))).convert('RGB');image.thumbnail((2048,2048));image.save(art/name)
metal=Image.open(next(textures.glob('*metallic*'))).convert('L');rough=Image.open(next(textures.glob('*roughness*'))).convert('L')
metal.thumbnail((2048,2048));rough.thumbnail((2048,2048));Image.merge('RGBA',(metal,metal,metal,ImageOps.invert(rough))).save(art/'MetallicSmoothness.png')
subprocess.run([str(args.blender),'-b','--python-exit-code','1','--python',str(root/'tools/prepare_tripo_station_clerk.py')],cwd=root,check=True)
report={'tripoAsset':'7ef008b1-0a11-4338-a813-c65ba9636e73','archive':archive.name,'archiveSHA256':hashlib.sha256(archive.read_bytes()).hexdigest(),
        'model':'Tripo H3.1 image-to-model, 9821 quad retopology, successful Unity Humanoid Compatible rig',
        'motion':'Tripo idle and AI Clerk Talking to Passenger, planted lower-body refinement and local generic jaw opening',
        'originalPreserved':True,'license':'No license file supplied in export; no inherited third-party model license claim.'}
(source/'import-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
