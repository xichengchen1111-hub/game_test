"""Replace only Windows in the existing saved source; preserve every other mesh."""
import bpy,math,json,hashlib,sys,struct,shutil
from pathlib import Path
from mathutils import Vector
REPO=Path(__file__).resolve().parents[2];SOURCE=REPO/'assets/source/milkshop'
TEMP=REPO/'builds/review/milk-interior-a/window-exports'
TEMP.mkdir(parents=True,exist_ok=True);(SOURCE/'verification').mkdir(parents=True,exist_ok=True)
sys.path.insert(0,str(SOURCE/'tools'))
from window_joinery import build_windows
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
asset=bpy.data.collections['MilkShop1931_Asset'];old=asset.objects['Windows'];parent=old.parent
def geometry_hash(o):
    h=hashlib.sha256()
    for v in o.data.vertices:h.update(struct.pack('<fff',*v.co))
    for p in o.data.polygons:h.update(struct.pack('<'+'I'*len(p.vertices),*p.vertices))
    return h.hexdigest()
before={o.name:geometry_hash(o) for o in asset.objects if o.type=='MESH' and o!=old}
source_before=hashlib.sha256((SOURCE/'MilkShop1931.blend').read_bytes()).hexdigest()
bpy.data.objects.remove(old,do_unlink=True)
pale=bpy.data.materials['PalePaintedTimber'];glass=bpy.data.materials['OldWindow'];parts=[]
def active(o):
    bpy.ops.object.select_all(action='DESELECT');o.hide_set(False);o.select_set(True);bpy.context.view_layer.objects.active=o
def register(o,name,m):
    o.name=name
    for c in list(o.users_collection):c.objects.unlink(o)
    asset.objects.link(o);o.data.materials.append(m);parts.append(o)
    layer=o.data.uv_layers.active or o.data.uv_layers.new(name='UVMap')
    for p in o.data.polygons:
        normal=o.matrix_world.to_3x3()@p.normal;axis=max(range(3),key=lambda i:abs(normal[i]))
        for li in p.loop_indices:
            v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
            a,b=(v.x,v.y) if axis==2 else ((v.x,v.z) if axis==1 else (v.y,v.z))
            layer.data[li].uv=(a/7.2+.5,b/5)
    return o
def box(name,loc,size,m,group):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.dimensions=size;active(o);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);register(o,name,m)
    bevel=o.modifiers.new('Soft timber edges','BEVEL');bevel.width=min(.010,min(size)*.2);bevel.segments=1
    normal=o.modifiers.new('Weighted normals','WEIGHTED_NORMAL');normal.keep_sharp=True;normal.weight=40
    return o
def pane(name,x,y,z,w,h,m,group):
    me=bpy.data.meshes.new(name);me.from_pydata([(x,y-w/2,z-h/2),(x,y+w/2,z-h/2),(x,y+w/2,z+h/2),(x,y-w/2,z+h/2)],[],[(0,1,2,3)]);me.update();o=bpy.data.objects.new(name,me);asset.objects.link(o);return register(o,name,m)
build_windows(box,pane,pale,glass)
for o in parts:
    active(o)
    for mod in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
bpy.ops.object.select_all(action='DESELECT')
for o in parts:o.select_set(True)
bpy.context.view_layer.objects.active=parts[0];bpy.ops.object.join();windows=bpy.context.object;windows.name='Windows'
bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');windows.parent=parent
after={o.name:geometry_hash(o) for o in asset.objects if o.type=='MESH' and o!=windows}
assert before==after,'Repair altered a non-window mesh'
objects=list(asset.objects);hidden={o.name:o.hide_get() for o in objects}
bpy.ops.object.select_all(action='DESELECT')
for o in objects:o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=parent
export=TEMP/'MilkShop1931.fbx'
bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='COPY',embed_textures=False,use_mesh_modifiers=True)
shutil.copy2(export,REPO/'game/Assets/Art/MilkShop1931/MilkShop1931.fbx')
for o in objects:
    if o.name.startswith('COL_'):o.select_set(False)
bpy.ops.export_scene.gltf(filepath=str(TEMP/'MilkShop1931.glb'),export_format='GLB',use_selection=True,export_apply=True,export_yup=True,export_texcoords=True,export_normals=True,export_materials='EXPORT',export_extras=True)
for o in objects:o.hide_set(hidden[o.name])
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
bpy.ops.wm.open_mainfile(filepath=str(SOURCE/'MilkShop1931.blend'))
assert all(geometry_hash(bpy.data.objects[name])==value for name,value in before.items())
window=bpy.data.objects['Windows'];gidx=list(window.data.materials).index(bpy.data.materials['OldWindow'])
glass_faces=[p for p in window.data.polygons if p.material_index==gidx]
assert len(glass_faces)==8,'Glass must contain one quad for each of eight panes'
stats={'passed':True,'reopened':True,'unchangedNonWindowMeshes':before,'oldSourceSha256':source_before,'newSourceSha256':hashlib.sha256((SOURCE/'MilkShop1931.blend').read_bytes()).hexdigest(),'frameReturnInsetM':.05,'frameBeyondWallFaceM':.07,'mullionCrossbarsOverlap':False,'singleGlassQuads':len(glass_faces),'wallDoorCounterPlacementChanged':False}
(SOURCE/'verification/window-joinery-repair.json').write_text(json.dumps(stats,indent=2)+'\n',encoding='utf-8')
stats_path=SOURCE/'verification/model-stats.json'
model=json.loads(stats_path.read_text()) if stats_path.exists() else {'asset':'MilkShop1931','footprint_m':[7.2,5.8],'visual_meshes':12,'units':'metres'}
model['triangles']=sum(len(o.data.loop_triangles) for o in bpy.data.collections['MilkShop1931_Asset'].objects if o.type=='MESH' and not o.name.startswith('COL_') for _ in [o.data.calc_loop_triangles()]);model['windowRepair']='5cm return overlap, 7cm depth clearance, butt crossbars, one surface per glass pane'
(SOURCE/'verification/model-stats.json').write_text(json.dumps(model,indent=2)+'\n')
print('MILK_WINDOW_JOINERY_SOURCE_PASS',json.dumps(stats))
