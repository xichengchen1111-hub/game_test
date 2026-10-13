"""Reopen actual source and check the reported stairwell intrusions and physical seams."""
import bpy,bmesh,json,hashlib
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[2];src=r/'assets/source/house';bpy.ops.wm.open_mainfile(filepath=str(src/'House1931.blend'));bpy.context.view_layer.update()
p=json.loads((src/'layout.json').read_text());fail=[]
def need(ok,why):
 if not ok:fail.append(why)
def bounds(o):
 vs=[o.matrix_world@Vector(v) for v in o.bound_box];q=[(v.x,v.z,-v.y) for v in vs]
 return ([min(v[k] for v in q) for k in range(3)],[max(v[k] for v in q) for k in range(3)])
def exact(n):return bounds(bpy.data.objects[n])
lo,hi=exact('East porch canopy');need(lo[0]>=8.115,'Exterior porch roof intrudes through east wall')
# Upper outlet: existing floor -> infill -> last tread, no negative overlap / height jump.
fl,fh=exact('Upper floor south');tl,th=exact('Stair upper threshold')
steps=[bounds(o) for o in bpy.data.objects if o.name.startswith('Stair tread')]
last=max((b for b in steps if b[0][0]>5),key=lambda b:b[1][1])
need(th[2]>=last[0][2]-.001 and tl[2]<=fh[2]+.001,'Upper outlet has an open floor gap')
# Floor, infill and top tread meet edge to edge: no two of them may overlap in the walking plane (z-fighting).
need(tl[2]>=fh[2]-.001 and th[2]<=last[0][2]+.001,'Upper outlet infill overlaps floor or tread in one plane')
need(max(abs(th[1]-fh[1]),abs(th[1]-last[1][1]))<.002,'Upper outlet floor levels differ')
ll,lh=exact('Stair landing');lower=max((b for b in steps if b[0][0]<5),key=lambda b:b[1][1])
need(lower[1][2]>=ll[2] and abs(lower[1][1]-lh[1])<.002,'Lower flight does not join landing')
need(lh[2]>=5.86,'Landing leaves an open slot before the north wall')
# Sample the wall/slab junction specifically where the old mesh had a 16 cm slit.
def covered(v,prefix):
 return any(b['name']==prefix and all(abs(v[i]-b['centre'][i])<=b['size'][i]/2+.0001 for i in range(3)) for b in p['colliders'])
for z in [1.5,2.5,3.5,4.5,5.5]:need(covered((3,3.28,z),'North room partition'),'Stair west partition has storey slit')
for x in [3.2,4.2,5.2,6.4,7.6]:need(covered((x,3.28,1),'North corridor partition'),'Stair south partition has storey slit')
# The double-height stairwell must show paper, not a plaster stripe, across the floor band of its outer walls.
paper=[bounds(o) for o in bpy.data.objects if o.name.startswith('Outer wall paper')]
for pt in [(x,y,5.874) for x in [3.5,5.2,7.5] for y in [3.19,3.27,3.35]]+[(7.874,y,z) for z in [1.5,3.4,5.5] for y in [3.19,3.27,3.35]]:
 need(any(all(lo[k]-.001<=pt[k]<=hi[k]+.001 for k in range(3)) for lo,hi in paper),'Stairwell wall paper gap at '+str(pt))
need(not any(o.name.startswith('Continuous stair string') or o.name=='Landing handrail transition' for o in bpy.data.objects),'Legacy floating rail/string remains')
for o in bpy.data.objects:
 if o.name.startswith('Stair closed string'):
  bm=bmesh.new();bm.from_mesh(o.data);need(bm.calc_volume(signed=True)>0,'Inward-facing closed string: '+o.name);bm.free()
for frame in [o for o in bpy.data.objects if o.name.startswith('DoorFrame_')]:
 header=next(o for o in frame.children if o.name.startswith('Door casing lintel'));hl,hh=bounds(header)
 for jamb in [o for o in frame.children if o.name.startswith('Door casing.') or o.name=='Door casing']:
  jl,jh=bounds(jamb);need(.003<=hl[1]-jh[1]<=.008,'Overlapping door casing corner '+jamb.name)
# Every handrail endpoint must terminate at another rail or inside a newel body.
rails=[o for o in bpy.data.objects if o.name.startswith('Stair handrail')]
posts=[bounds(o) for o in bpy.data.objects if o.name.startswith('Stair newel')]
ends=[]
for o in rails:
 zs=[v.co.z for v in o.data.vertices]
 for zz in [min(zs),max(zs)]:
  v=o.matrix_world@Vector((0,0,zz));ends.append((o.name,Vector((v.x,v.z,-v.y))))
for idx,(name,v) in enumerate(ends):
 joined=any(j!=idx and (v-w).length<.002 for j,(_,w) in enumerate(ends))
 inside=any(all(lo[k]-.005<=v[k]<=hi[k]+.005 for k in range(3)) for lo,hi in posts)
 need(joined or inside,'Unsupported handrail end: '+name+' '+str(tuple(v)))
report={'passed':not fail,'failures':fail,'porchInteriorClearance':round(lo[0]-7.88,3),'treadCount':len(steps),'closedStringCount':len([o for o in bpy.data.objects if o.name.startswith('Stair closed string')]),'railEndsChecked':len(ends),'upperThresholdHeight':round(th[1],3),'sourceSHA256':hashlib.sha256((src/'House1931.blend').read_bytes()).hexdigest(),'scope':'Reopened geometry, canopy boundary, actual floor/landing joins, wall band coverage and rail endpoint support. Continuous native walking checked separately.'}
(src/'stairs-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2));assert not fail
