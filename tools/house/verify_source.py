import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[2];src=r/'assets/source/house';bpy.ops.wm.open_mainfile(filepath=str(src/'House1931.blend'))
p=json.loads((src/'layout.json').read_text());assert p['rooms']==8 and p['storeys']==2 and len(p['cabinets'])==20
for d in p['doors']:assert bpy.data.objects.get(d['node'])
for c in p['cabinets']:
 for n in c['nodes']:assert bpy.data.objects.get(n)
for name,want in [('RightMarker',(1,0,0)),('FrontMarker',(0,1,0)),('UpMarker',(0,0,1))]:assert (bpy.data.objects[name].location-Vector(want)).length<.001
images=[im for im in bpy.data.images if im.source=='FILE'];assert all(im.packed_file for im in images)
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];assert all(o.data.uv_layers and len(o.data.materials) for o in meshes)
bpy.context.view_layer.update()
for o in meshes:
 if o.name.startswith('Brass door knob'): assert max(o.dimensions)<.1, (o.name,tuple(o.dimensions))
 if o.name.startswith('Warm bulb'): assert max(o.dimensions)<.3, (o.name,tuple(o.dimensions))
for o in meshes:
 if o.name.startswith('Rear wing roof'):assert max((o.matrix_world@v.co).y for v in o.data.vertices)<-6.12,'Rear roof crosses into upper bedroom'
report={'passed':True,'blender':bpy.app.version_string,'rooms':8,'storeys':2,'searchGroups':20,'openings':len(p['openings']),'doors':len(p['doors']),'editableMeshes':len(meshes),'packedImages':len(images),'sourceSHA256':hashlib.sha256((src/'House1931.blend').read_bytes()).hexdigest(),'scope':'Reopened editable source, metre markers, moving part IDs, UV/materials, packed images. Runtime interaction and appearance tested separately.'}
(src/'source-verification.json').write_text(json.dumps(report,indent=2));print(report)
