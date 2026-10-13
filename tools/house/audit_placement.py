"""Reopen the authored house and measure real furniture bounds, supports and curtain clearance."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[2];src=r/'assets/source/house';bpy.ops.wm.open_mainfile(filepath=str(src/'House1931.blend'));bpy.context.view_layer.update()
def bounds(objects):
 points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
 pts=[(p.x,p.z,-p.y) for p in points]
 return ([min(p[a] for p in pts) for a in range(3)],[max(p[a] for p in pts) for a in range(3)])
def meshes(name):
 # The range's flue is fixed into the wall behind it by design; it is not part of the free-standing body.
 o=bpy.data.objects[name]
 return [c for c in o.children_recursive if c.type=='MESH' and not c.name.startswith(('Stove flue','Flue wall collar'))]
def b(name):return bounds(meshes(name))
fail=[];rear=[];contacts=[];curtain_pairs=[]
def need(v,msg):
 if not v:fail.append(msg)
# Axis, min/max side, actual unobstructed plane: papered outer-wall face (±5.868/±7.868), its skirting front (±5.832),
# or a partition face. Gaps must be 0-12 cm. Skirting/paper clearance is also checked mesh by mesh below.
backs=[('Parents bed',2,0,-5.832),('Child bed',2,1,5.832),('Cabinet_bedside-a',2,0,-5.832),('Cabinet_bedside-b',2,0,-5.832),('Cabinet_child-bedside',2,1,5.832),('Cabinet_dressing',0,0,-7.868),('Cabinet_wardrobe',0,1,.92),('Cabinet_sideboard',0,1,7.868),('Three seat sofa',0,0,-7.868),('Writing desk',2,1,5.868),('Cabinet_desk',2,1,5.832),('Cabinet_medical',2,1,5.832),('Bookcase',0,0,-1.92),('Cabinet_stationery',0,1,2.92),('Cabinet_safe',2,0,1.08),('Cabinet_kitchen-bills',2,1,5.832),('Cabinet_kitchen-larder',2,1,5.832),('Cabinet_kitchen-crockery',0,0,-7.868),('Deep sink',0,0,-7.868),('Cast iron cooker',2,0,1.08),('Cabinet_utility',0,0,-7.88),('Laundry washstand',2,1,12.88),('Child desk',2,1,5.832),('Cabinet_toys',2,0,1.08),('Clawfoot bath',2,1,5.868),('Pedestal basin',2,1,5.868),('Cabinet_linen',2,0,-5.832),('Cabinet_old-trunk',0,1,7.868),('Treadle sewing table',2,0,-5.832),('Cabinet_hall',2,0,-.92)]
for name,axis,side,plane in backs:
 lo,hi=b(name);gap=plane-hi[axis] if side else lo[axis]-plane
 rear.append({'object':name,'gapMetres':round(gap,4)});need(-.002<=gap<=.125,f'{name}: rear gap {gap:.3f} m')
# Furniture feet must meet the floor. Mirrors and tabletop items are excluded by explicit ownership.
floor_names=[x[0] for x in backs]+['Club armchair','Cabinet_coffee','Desk chair','Kitchen table','Kitchen chair A','Kitchen chair B','Child chair','Sewing chair']
furniture_overlaps=[]
for index,name in enumerate(floor_names):
 lo,hi=b(name)
 for other in floor_names[index+1:]:
  ol,oh=b(other);depth=[min(hi[a],oh[a])-max(lo[a],ol[a]) for a in range(3)]
  if min(depth)>.012:furniture_overlaps.append([name,other,[round(v,4) for v in depth]])
need(not furniture_overlaps,'Fixed furniture overlaps: '+str(furniture_overlaps))
for name in floor_names:
 lo,hi=b(name);floor=3.36 if lo[1]>3 else .16;gap=lo[1]-floor
 contacts.append({'object':name,'floorGapMetres':round(gap,4)});need(abs(gap)<.012,f'{name}: floor gap {gap:.3f} m')
# Solid furniture may sit under a window, but must never intersect actual curtain folds.
curtains=[o for o in bpy.data.objects if o.type=='MESH' and 'curtain fold' in o.name]
critical=floor_names+['Dressing mirror','Bedside light','Desk light','Sitting room lamp','Toy horse','Sewing machine','Cabinet_bath-mirror']
for name in critical:
 for ob in meshes(name):
  lo,hi=bounds([ob])
  for curtain in curtains:
   cl,ch=bounds([curtain]);depth=[min(hi[a],ch[a])-max(lo[a],cl[a]) for a in range(3)]
   if min(depth)>.003:curtain_pairs.append([name,ob.name,curtain.name,[round(v,4) for v in depth]])
need(not curtain_pairs,'Furniture intersects curtain folds: '+str(curtain_pairs[:8]))
# No furniture part may push into the new skirting boards or outer-wall paper (AABB depth over 3 mm).
trim=[o for o in bpy.data.objects if o.type=='MESH' and (o.name.startswith('Wall-attached skirting') or o.name.startswith('Outer wall paper'))]
trim_bounds=[(o.name,bounds([o])) for o in trim];trim_hits=[]
for name in floor_names+['Dressing mirror','Kitchen clock','Bookcase']:
 for ob in meshes(name):
  lo,hi=bounds([ob])
  for tn,(tl,th) in trim_bounds:
   depth=[min(hi[a],th[a])-max(lo[a],tl[a]) for a in range(3)]
   if min(depth)>.003:trim_hits.append([name,ob.name,tn,[round(v,4) for v in depth]])
need(not trim_hits,'Furniture intersects skirting or wall paper: '+str(trim_hits[:8]))
# Named tabletop props: their lowest point and footprint must be supported by the visible top.
supports=[('Bedside light','Cabinet_bedside-a','Cabinet top'),('Desk light','Writing desk','Writing desk top'),('Sitting room lamp','Cabinet_sideboard','Cabinet top'),('Wash tub','Laundry washstand','Laundry washstand top'),('Toy horse','Cabinet_toys','Cabinet top'),('Sewing machine','Treadle sewing table','Treadle sewing table top')]
top_contacts=[]
for prop,table,prefix in supports:
 top=next(o for o in meshes(table) if o.name.startswith(prefix));lo,hi=bounds([top]);pl,ph=b(prop);gap=pl[1]-hi[1]
 top_contacts.append({'object':prop,'support':table,'gapMetres':round(gap,4)});need(abs(gap)<.012,f'{prop}: support gap {gap:.3f} m')
# The bedside cabinets must remain near the pillow ends and clear of the bed frame.
for bed,stands in [('Parents bed',['Cabinet_bedside-a','Cabinet_bedside-b']),('Child bed',['Cabinet_child-bedside'])]:
 bl,bh=b(bed)
 for name in stands:
  lo,hi=b(name);gap=max(lo[0]-bh[0],bl[0]-hi[0]);need(.04<=gap<=.22,f'{name}: bedside lateral gap {gap:.3f} m')
report={'passed':not fail,'rearClearances':rear,'groundContacts':contacts,'tabletopContacts':top_contacts,'fixedFurnitureIntersections':furniture_overlaps,'curtainIntersections':curtain_pairs,'skirtingOrPaperIntersections':trim_hits,'failures':fail,'sourceSHA256':hashlib.sha256((src/'House1931.blend').read_bytes()).hexdigest(),'scope':'Authored source bounds; wall relations, ground and tabletop contacts, bedsides, fixed furniture pairs, and actual curtain-fold overlaps. Native visual and opening checks are separate.'}
(src/'placement-audit.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2));assert not fail
