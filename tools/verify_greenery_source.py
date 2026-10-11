"""Reopen the packed source to verify export invariants; Blender background."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[1];folder=r/'assets/source/greenery';bpy.ops.wm.open_mainfile(filepath=str(folder/'GreeneryKit.blend'))
manifest=json.loads((folder/'manifest.json').read_text());checks=[]
for a in manifest['assets']:
 o=bpy.data.objects.get(a['name']);assert o and o.type=='MESH';m=o.data
 assert len(m.vertices)==a['vertices'] and len(m.materials)==1 and len(m.uv_layers)==1
 assert all(0<=uv.uv.x<=1 and 0<=uv.uv.y<=1 for uv in m.uv_layers[0].data)
 assert min(v.co.z for v in m.vertices)>-.03
 assert sum(len(p.vertices)-2 for p in m.polygons)==a['triangles']
 dims=[max(v.co[i] for v in m.vertices)-min(v.co[i] for v in m.vertices) for i in range(3)]
 if a['name'].startswith('Tree'):assert 5<dims[2]<10
 else:assert .10<dims[2]<1.6 and max(dims[:2])<1.7
 assert (r/'game/Assets/Art/Greenery'/(a['name'].split('_LOD')[0]+'.fbx')).exists()
 checks.append(dict(name=o.name,dimensions=dims,vertices=len(m.vertices),triangles=a['triangles']))
images=[i for i in bpy.data.images if i.name.startswith('Greenery')];assert len(images)==2 and all(i.packed_file for i in images)
assert len(checks)==51
result={'passed':True,'meshes':len(checks),'models':22,'treeFamilies':7,'packedImages':[i.name for i in images],'checks':checks,'scope':'Reopened Blender source geometry/UV/material/ground/orientation/packed texture checks; native rendering separate.'}
(folder/'source-verification.json').write_text(json.dumps(result,indent=2));print('GREENERY_SOURCE_PASS',len(checks))
