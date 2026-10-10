"""Editable Blender source from the exact same punched geometry used by Unity."""
import json,sys
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,str(Path(__file__).resolve().parent))
from build_rail_assets import RailSceneBuilder,u

root=Path(__file__).resolve().parents[2]
source=root/'assets/source/rail';art=root/'game/Assets/Art/Rail'
spec=json.loads((source/'ticket-geometry.json').read_text(encoding='utf-8'))
b=RailSceneBuilder('TicketStock_1931')
report=[]
for punched,key,shift in [(False,'intact',-.07),(True,'punched',.07)]:
    b.set_module('Module_Ticket_'+key)
    data=spec[key]
    points=[tuple(data['vertices'][i:i+3]) for i in range(0,len(data['vertices']),3)]
    faces=[tuple(data['triangles'][i:i+3]) for i in range(0,len(data['triangles']),3)]
    mesh=b.authored_mesh('Ticket_'+key,points,faces,'RailIvory')
    for loop in mesh.data.loops:mesh.data.uv_layers.active.data[loop.index].uv=data['uv'][loop.vertex_index*2:loop.vertex_index*2+2]
    mat=bpy.data.materials.new('TicketPaper_'+key);mat.use_nodes=True
    bsdf=mat.node_tree.nodes.get('Principled BSDF');bsdf.inputs['Roughness'].default_value=.85
    tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(art/('E03.png' if punched else 'E02.png')))
    tex.image.pack();mat.node_tree.links.new(tex.outputs['Color'],bsdf.inputs['Base Color'])
    mesh.data.materials.clear();mesh.data.materials.append(mat)
    for hole in spec['holes']:
        p=((hole['xMm']-spec['widthMm']/2)/1000,.05,(spec['heightMm']/2-hole['yMm'])/1000)
        hit=mesh.ray_cast(u(p),u((0,-1,0)).normalized())[0]
        assert hit!=punched,(key,p,hit)
    assert mesh.ray_cast(u((0,.05,0)),u((0,-1,0)).normalized())[0]
    mesh.location.x=-shift
    report.append({'stock':key,'vertices':len(mesh.data.vertices),'triangles':len(mesh.data.polygons),'actualPunchVoids':3 if punched else 0})
b.export_fbx_and_blend(source/'TicketProps.fbx',source/'TicketProps.blend')
bpy.ops.wm.open_mainfile(filepath=str(source/'TicketProps.blend'))
assert len([o for o in bpy.data.objects if o.type=='MESH'])==2
(source/'ticket-source-checks.json').write_text(json.dumps({'passed':True,'sharedUnityJson':'ticket-geometry.json','models':report},indent=2)+'\n',encoding='utf-8')
print('TICKET_SOURCE_PASS: source reopened; physical three-hole rays and intact stock verified')
