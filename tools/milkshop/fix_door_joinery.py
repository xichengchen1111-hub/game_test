"""Repair only three doorway components inside existing joined Trim mesh."""
import bpy,bmesh,json,hashlib,struct,shutil,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];SOURCE=ROOT/'assets/source/milkshop'
TEMP=ROOT/'builds/review/full-map/milk-door-exports';TEMP.mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(SOURCE/'tools'))
from door_joinery import build_door_frame
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
asset=bpy.data.collections['MilkShop1931_Asset'];trim=asset.objects['Trim']
def geometry_hash(o):
    h=hashlib.sha256()
    for v in o.data.vertices:h.update(struct.pack('<fff',*v.co))
    for p in o.data.polygons:h.update(struct.pack('<'+'I'*len(p.vertices),*p.vertices))
    return h.hexdigest()
def components(o):
    links={v.index:set() for v in o.data.vertices}
    for edge in o.data.edges:
        a,b=edge.vertices;links[a].add(b);links[b].add(a)
    remaining=set(links);out=[]
    while remaining:
        group={remaining.pop()};todo=list(group)
        while todo:
            for v in links[todo.pop()]&remaining:remaining.remove(v);group.add(v);todo.append(v)
        out.append(group)
    return out
def bounds(o,ids):
    points=[o.matrix_world@o.data.vertices[i].co for i in ids]
    lo=Vector(tuple(min(v[k] for v in points) for k in range(3)));hi=Vector(tuple(max(v[k] for v in points) for k in range(3)))
    return (lo+hi)/2,hi-lo
def is_frame(o,ids):
    centre,size=bounds(o,ids)
    return ((abs(abs(centre.x)-1.31)<.001 and abs(centre.y+2.96)<.001 and abs(size.x-.16)<.001 and size.z>2.5)
        or (abs(centre.x)<.001 and abs(centre.y+2.96)<.001 and abs(centre.z-2.82)<.001 and abs(size.x-2.8)<.001 and abs(size.z-.18)<.001))
def face_signature(o,excluded):
    faces=[];uv=o.data.uv_layers.active
    for p in o.data.polygons:
        if any(v in excluded for v in p.vertices):continue
        corners=[]
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            corners.append(tuple(round(x,6) for x in (*v,*uv.data[li].uv)))
        faces.append((o.data.materials[p.material_index].name,tuple(sorted(corners))))
    return hashlib.sha256(repr(sorted(faces)).encode()).hexdigest()
old_groups=[g for g in components(trim) if is_frame(trim,g)]
assert len(old_groups)==3, f'Expected exactly three original frame components, found {len(old_groups)}'
old_ids=set().union(*old_groups)
unchanged={o.name:geometry_hash(o) for o in asset.objects if o.type=='MESH' and o!=trim}
other_trim=face_signature(trim,old_ids)
old_source=hashlib.sha256((SOURCE/'MilkShop1931.blend').read_bytes()).hexdigest()
bm=bmesh.new();bm.from_mesh(trim.data);bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[bm.verts[i] for i in old_ids],context='VERTS');bm.to_mesh(trim.data);bm.free();trim.data.update()
assert face_signature(trim,set())==other_trim
parts=[];pale=bpy.data.materials['PalePaintedTimber']
def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
def box(name,loc,size,material,group):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size
    active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    asset.objects.link(o);o.data.materials.append(material);parts.append(o)
    uv=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        axis=max(range(3),key=lambda k:abs(p.normal[k]))
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            a,b=(v.x,v.y) if axis==2 else ((v.x,v.z) if axis==1 else (v.y,v.z))
            uv.data[li].uv=(a/7.2+.5,b/5)
    bevel=o.modifiers.new('Soft timber edges','BEVEL');bevel.width=.01;bevel.segments=1
    normals=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');normals.keep_sharp=True;normals.weight=40
    for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
settings=build_door_frame(box,pale)
new_faces=face_signature(trim,set());assert new_faces==other_trim
# Active original Trim retains its transform and all remaining original vertices/UVs.
active(trim)
for o in parts:o.select_set(True)
bpy.ops.object.join();trim=bpy.context.object
new_groups=[]
for g in components(trim):
    centre,size=bounds(trim,g)
    if abs(centre.y+2.92)<.001 and ((abs(abs(centre.x)-1.31)<.001 and size.z>2.5) or (abs(centre.x)<.001 and abs(size.x-2.8)<.001)):new_groups.append(g)
assert len(new_groups)==3
new_ids=set().union(*new_groups);assert face_signature(trim,new_ids)==other_trim
assert {o.name:geometry_hash(o) for o in asset.objects if o.type=='MESH' and o!=trim}==unchanged
objects=list(asset.objects);hidden={o.name:o.hide_get() for o in objects}
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['MilkShop1931']
export=TEMP/'MilkShop1931.fbx'
bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False,use_mesh_modifiers=True)
shutil.copy2(export,ROOT/'game/Assets/Art/MilkShop1931/MilkShop1931.fbx')
for o in objects:o.hide_set(hidden[o.name])
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
assert all(geometry_hash(bpy.data.objects[name])==value for name,value in unchanged.items())
model_stats=SOURCE/'verification/model-stats.json';stats=json.loads(model_stats.read_text())
stats['triangles']=sum(len(o.data.loop_triangles) for o in bpy.data.collections['MilkShop1931_Asset'].objects if o.type=='MESH' and not o.name.startswith('COL_') for _ in [o.data.calc_loop_triangles()])
stats['doorFrameRepair']=settings;model_stats.write_text(json.dumps(stats,indent=2)+'\n')
proof=dict(passed=True,reopened=True,settings=settings,unchangedNonTrimMeshes=unchanged,unchangedOtherTrimFaces=other_trim,oldSourceSha256=old_source,newSourceSha256=hashlib.sha256((SOURCE/'MilkShop1931.blend').read_bytes()).hexdigest(),originalOpeningAndCollisionUnchanged=True)
(SOURCE/'verification/door-joinery-repair.json').write_text(json.dumps(proof,indent=2)+'\n')
# Controlled source render; add camera/lights only after saving/reopening the source.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.render.resolution_x=1280;scene.render.resolution_y=720;scene.render.resolution_percentage=100
camera=bpy.data.objects.new('Door joinery review camera',bpy.data.cameras.new('Door joinery review camera'));scene.collection.objects.link(camera)
camera.location=(0,-1.6,1.8);camera.rotation_euler=(Vector((1.30,-2.74,2.76))-camera.location).to_track_quat('-Z','Y').to_euler();scene.camera=camera;camera.data.lens=28
light=bpy.data.objects.new('Door review fill',bpy.data.lights.new('Door review fill','AREA'));scene.collection.objects.link(light);light.location=(0,-1.4,2.7);light.data.energy=250;light.data.size=3
light.rotation_euler=(Vector((0,-2.9,2))-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(TEMP/'door-source-inside.png');bpy.ops.render.render(write_still=True)
print('MILK_DOOR_JOINERY_SOURCE_PASS',json.dumps(proof))
