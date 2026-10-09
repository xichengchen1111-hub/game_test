"""One-time normalization; also present in the canonical build_exterior.py."""
import bpy,json,shutil
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1]
REPO=ROOT.parents[3]
OUT=REPO/'game/Assets/Art/ClockTower1931'
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ClockTower1931.blend'))
asset=bpy.data.collections['ClockTower1931_Export']
for o in asset.objects:
    if o.type!='MESH':continue
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
bpy.ops.object.select_all(action='DESELECT')
for o in asset.objects:o.select_set(True)
bpy.context.view_layer.objects.active=bpy.data.objects['ClockTower1931']
bpy.ops.export_scene.fbx(filepath=str(OUT/'ClockTower1931.fbx'),use_selection=True,object_types={'MESH','EMPTY'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,path_mode='STRIP',embed_textures=False,use_mesh_modifiers=True)
shutil.copy2(OUT/'ClockTower1931.fbx',ROOT/'ClockTower1931.fbx')
bounds=[o.matrix_world@Vector(v) for o in asset.objects if o.type=='MESH' for v in o.bound_box]
m=json.loads((ROOT/'manifest.json').read_text());m['bounds_blender_min']=[min(v[i] for v in bounds) for i in range(3)];m['bounds_blender_max']=[max(v[i] for v in bounds) for i in range(3)]
m['merged_mesh_transforms']='Identity rotation and scale; origin at base centre.'
(ROOT/'manifest.json').write_text(json.dumps(m,indent=2));(OUT/'manifest.json').write_text(json.dumps(m,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'ClockTower1931.blend'))
print('EXPORT_TRANSFORM_NORMALIZED',m['bounds_blender_min'],m['bounds_blender_max'])
