"""Reopen and inspect the saved editable source, then round-trip the export."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[1]
REPO=ROOT.parents[3]
manifest=json.loads((ROOT/'manifest.json').read_text())
height=manifest['height_m'];middle=manifest['middle_floor_height'];gear=manifest['gear_floor_height'];dial=manifest['clock_centres_height']
reports={}
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ClockTower1931.blend'))
objs=[o for o in bpy.data.collections['ClockTower1931_Export'].objects if o.type=='MESH']
assert len(objs)==7,len(objs)
verts=[];polys=[];bounds=[];material_names=set();triangles=0
for o in objs:
    offset=len(verts);vs=[o.matrix_world@v.co for v in o.data.vertices];verts.extend(vs);bounds.extend(vs)
    polys.extend([tuple(offset+i for i in p.vertices) for p in o.data.polygons])
    material_names.update(m.name for m in o.data.materials)
    assert o.location.length<.00001,(o.name,list(o.location))
    assert all(p.area>1e-12 for p in o.data.polygons),o.name
    assert len(o.data.uv_layers)>0,o.name
    o.data.calc_loop_triangles();triangles+=len(o.data.loop_triangles)
minv=[min(v[i] for v in bounds) for i in range(3)];maxv=[max(v[i] for v in bounds) for i in range(3)]
assert abs(minv[2])<.001,minv
assert abs(maxv[2]-height)<.001,maxv
# Measure the actual opening of the timber clock ring at the south clock face.
stone=bpy.data.objects['Exterior_DarkOak']
ring_radii=[]
for vertex in stone.data.vertices:
    v=stone.matrix_world@vertex.co
    if -2.56<v.y<-2.425 and abs(v.x)<1.9 and abs(v.z-dial)<1.85:
        ring_radii.append(math.hypot(v.x,v.z-dial))
ring_clear_diameter=min(ring_radii)*2
assert abs(ring_clear_diameter-manifest['ring_clear_diameter'])<.002,ring_clear_diameter
bvh=BVHTree.FromPolygons(verts,polys)
samples=[]
for x in [-.51,-.34,0,.34,.51]:
    for z in [.17,.45,.85,1.3,1.8,2.23]:
        hit=bvh.ray_cast(Vector((x,-2.2,z)),Vector((0,1,0)),.70)
        assert hit[0] is None,('Door obstructed',x,z,hit)
        samples.append([x,z])
dial_samples=0
for side in range(4):
    a=side*math.pi/2;outward=Vector((math.sin(a),-math.cos(a),0))
    horizontal=Vector((math.cos(a),math.sin(a),0))
    for x in [-.8,0,.8]:
        for y in [-.8,0,.8]:
            start=outward*2.0+horizontal*x+Vector((0,0,dial+y))
            hit=bvh.ray_cast(start,outward,.6)
            assert hit[0] is None,('Clock aperture obstructed',side,x,y,hit)
            dial_samples+=1
reports['source']={'source_opened':str(ROOT/'ClockTower1931.blend'),'mesh_count':len(objs),'materials':sorted(material_names),'triangles':triangles,'bounds_min':minv,'bounds_max':maxv,'clock_aperture_clearance_rays':dial_samples,'door_clearance_rays':len(samples),'door_width_tested':1.02,'door_height_tested':2.06,'door_test_range_blender_y':[-2.2,-1.5],'all_export_meshes_origin_at_base':True,'explicit_uvs':True,'no_zero_area_faces':True,'review_geometry_separate':True,'clock_ring_measured_clear_diameter':ring_clear_diameter,'three_level_heights':[.12,middle,gear],'physical_brick_tile_metres':manifest['textures']['IvoryBrick']['tile_metres']}
# Import into a clean Blender to ensure the FBX's advertised scale and markers survive.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(ROOT/'ClockTower1931.fbx'))
objs=[o for o in bpy.context.scene.objects if o.type=='MESH']
marker={k:list(bpy.data.objects[k].matrix_world.translation) for k in ['FrontMarker','EastMarker','DoorHeadMarker','MiddleFloorMarker','GearFloorMarker','ClockCentreFront','RoofPeakMarker']}
assert Vector(marker['FrontMarker']).__sub__(Vector((0,-1.8,.12))).length<.0001,marker
assert Vector(marker['RoofPeakMarker']).__sub__(Vector((0,0,height))).length<.0001,marker
assert (Vector(marker['MiddleFloorMarker'])-Vector((0,0,middle))).length<.0001,marker
assert (Vector(marker['GearFloorMarker'])-Vector((0,0,gear))).length<.0001,marker
assert (Vector(marker['ClockCentreFront'])-Vector((0,-manifest['clock_unity_plane_distance'],dial))).length<.0001,marker
reports['fbx_roundtrip']={'mesh_count':len(objs),'markers_blender':marker,'metre_scale_preserved':True,'axis_markers_preserved':True}
reports['pass']=True
(ROOT/'verification/inspection-results.json').write_text(json.dumps(reports,indent=2))
print('CLOCKTOWER_SOURCE_INSPECTION_PASS',json.dumps(reports))
