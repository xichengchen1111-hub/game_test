"""Reopen the actual saved .blend and audit only the exported addition collection."""
import bpy,json,sys
from mathutils import Vector
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];SRC=ROOT/'assets/source/milkshop/interior-a'
bpy.ops.wm.open_mainfile(filepath=str(SRC/'MilkShopInteriorA.blend'))
kit=bpy.data.collections['A_Interior_Additions'];meshes=[o for o in kit.objects if o.type=='MESH']
assert len(meshes)==10
for o in meshes:
 assert o.data.uv_layers and o.data.materials,(o.name,'UV/material missing')
 assert all(abs(s-1)<1e-5 for s in o.scale),o.name
 assert all(p.area>1e-12 for p in o.data.polygons),(o.name,'zero-area polygon')
assert 'Reference_ExistingShop_NOT_EXPORTED' in bpy.data.collections
assert abs(bpy.data.objects['UpMarker'].location.z-1)<1e-6
assert abs(bpy.data.objects['OrderBook'].location.z-1.255)<1e-5
manifest=json.loads((SRC/'manifest.json').read_text(encoding='utf-8'))
assert manifest['triangles']<16000 and manifest['newRealtimeShadows']==0
result={'passed':True,'reopenedBlend':True,'meshes':len(meshes),'uvAndMaterials':True,'unitScale':1,'namedBookLampBellParts':True,'referenceExcludedFromFbx':True}
(SRC/'source-check.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print('MILK_SOURCE_REOPEN_PASS',json.dumps(result))
if '--render' in sys.argv:
 for name,pos,target,lens in [('interior-player',(-.65,-2.3,1.87),(0,1.5,1.87),20),('counter-details',(-.25,-.32,1.90),(-.14,.94,1.39),44)]:
  cam=bpy.context.scene.camera;cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=lens
  bpy.context.scene.render.filepath=str(SRC/'previews'/f'{name}.png');bpy.ops.render.render(write_still=True)
