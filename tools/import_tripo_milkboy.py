"""Import the approved v4 Tripo model; retain original textures and editable source."""
from pathlib import Path
import argparse,json,hashlib,shutil,subprocess
from PIL import Image,ImageOps
p=argparse.ArgumentParser();p.add_argument('--source',type=Path,required=True);p.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);p.add_argument('--blender',type=Path,required=True);args=p.parse_args()
r=args.root.resolve();src=args.source.resolve();original=r/'assets/source/milkboy/tripo';art=r/'game/Assets/Art/MilkboyTripo';original.mkdir(parents=True,exist_ok=True);art.mkdir(parents=True,exist_ok=True)
shutil.copy2(src/'export/Milkboy_v4.fbx',original/'Milkboy.fbx');shutil.copy2(src/'export/Milkboy_v4.fbx',art/'Milkboy.fbx')
tex=src/'export/Milkboy_v4.fbm';shutil.copytree(tex,original/'textures',dirs_exist_ok=True)
base=next(tex.glob('*BaseColor*'));normal=next(tex.glob('*Normal_Bake*'));metal=next(tex.glob('*metallic*'));rough=next(tex.glob('*roughness*'))
Image.open(base).convert('RGB').save(art/'BaseColor.png');Image.open(normal).convert('RGB').save(art/'Normal.png')
m=Image.open(metal).convert('L');roughness=Image.open(rough).convert('L');assert m.size==roughness.size
Image.merge('RGBA',(m,m,m,ImageOps.invert(roughness))).save(art/'MetallicSmoothness.png')
# Save the editable master with packed textures; no dependency on the previous output directory.
script=original/'pack_source.py';script.write_text('import bpy,pathlib\nbpy.ops.wm.open_mainfile(filepath='+repr(str(src/'Milkboy_v4_Master.blend'))+')\nfor im in bpy.data.images:\n p=pathlib.Path('+repr(str(original/'textures'))+')/pathlib.Path(im.filepath).name\n if p.exists():im.filepath=str(p);im.pack()\nbpy.ops.wm.save_as_mainfile(filepath='+repr(str(original/'Milkboy.blend'))+')\n')
subprocess.run([str(args.blender),'-b','--python-exit-code','1','--python',str(script)],check=True)
shutil.copy2(src/'verification/grip_strap.py',original/'v4-refinement-reference.py');shutil.copy2(src/'verification/strap-refinement.json',original/'strap-refinement.json')
subprocess.run([str(args.blender),'-b','--python-exit-code','1','--python',str(r/'tools/milkboy_raise_gaze.py'),'--',str(r)],check=True)
report={'source':'User-approved Tripo asset a7950cfb-8a98-4db5-8dd9-8fb85e2bc3a5, with v4 shoulder-strap/jaw refinement and user-requested v5 head-up poses','modelSHA256':hashlib.sha256((original/'Milkboy.fbx').read_bytes()).hexdigest(),'animations':['01_Talk_WithMouth','02_Nervous_HoldShoulderStrap'],'mouth':'Generic jaw/lip opening; no phoneme or audio synchronization','originalAssetsPreserved':True,'license':'No license file supplied; do not inherit prior Toon Boy or Meshy license claims.'}
(original/'import-results.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print('TRIPO_SOURCE_IMPORT_PASS',flush=True)
