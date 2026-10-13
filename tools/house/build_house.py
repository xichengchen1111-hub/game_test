import bpy,bmesh,math,json,sys,random
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'game/Assets/Art/House1931';SRC=ROOT/'assets/source/house'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
random.seed(1931); mats={}; coll=[]; openings=[]; doors=[]; cabinets=[]; lights=[]
colors={'Walnut':(.35,.22,.12,1),'Floor':(.4,.3,.18,1),'Wallpaper':(.64,.6,.49,1),'Plaster':(.65,.62,.54,1),'Fabric':(.36,.42,.28,1),'Rug':(.4,.18,.13,1),'Tile':(.45,.51,.42,1),'Slate':(.2,.24,.26,1),'Cream':(.76,.73,.61,1),'Green':(.16,.24,.19,1),'Iron':(.075,.085,.08,1),'Brass':(.48,.34,.14,1),'Enamel':(.72,.76,.69,1),'Paper':(.8,.74,.58,1),'Glass':(.4,.55,.6,.15),'Mirror':(.55,.58,.58,1),'Chalk':(.8,.8,.73,1),'Blood':(.29,.018,.027,1),'Bulb':(1,.78,.4,1)}
for n,c in colors.items():
 m=bpy.data.materials.new('House_'+n);m.diffuse_color=c;m.use_nodes=True;bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=c;bs.inputs['Roughness'].default_value=.06 if n=='Mirror' else .35 if n in ['Walnut','Brass','Enamel'] else .75
 if n in ['Brass','Iron']:bs.inputs['Metallic'].default_value=.7
 if n=='Mirror':bs.inputs['Metallic'].default_value=1
 if (ART/(n+'.png')).exists():
  im=bpy.data.images.load(str(ART/(n+'.png')));im.pack();t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=im;m.node_tree.links.new(t.outputs['Color'],bs.inputs['Base Color'])
 mats[n]=m
base=(0,0,0);yaw=0;parent=None

def pos(p):
 x,y,z=p;a=math.radians(yaw);return (base[0]+x*math.cos(a)+z*math.sin(a),base[1]+y,base[2]-x*math.sin(a)+z*math.cos(a))
def cv(p):return (p[0],-p[2],p[1])
def empty(name,p=(0,0,0)):
 o=bpy.data.objects.new(name,None);bpy.context.collection.objects.link(o);o.location=cv(p);return o
def world(o):
 bpy.context.view_layer.update();return o.matrix_world.copy()
def assign(o,n,material):
 o.name=n;o.data.materials.append(mats[material]);o.rotation_euler[2]=-math.radians(yaw)
 if parent:
  bpy.context.view_layer.update()
  mw=o.matrix_world.copy();o.parent=parent;o.matrix_world=mw
 return o
def box(n,p,s,mat='Walnut',bevel=.015,solid=False):
 pp=pos(p);bpy.ops.mesh.primitive_cube_add(size=1,location=cv(pp));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);assign(o,n,mat)
 if mat in ['Wallpaper','Floor','Walnut','Plaster','Fabric','Tile','Slate','Rug']:
  uv=o.data.uv_layers.active.data
  for face in o.data.polygons:
   axis=max(range(3),key=lambda k:abs(face.normal[k]));a,b=[k for k in range(3) if k!=axis]
   for li in face.loop_indices:
    co=o.data.vertices[o.data.loops[li].vertex_index].co;uv[li].uv=((co[a]/(s[0],s[2],s[1])[a]+.5,co[b]/(s[0],s[2],s[1])[b]+.5) if mat=='Rug' else (co[a],co[b]))
 if bevel:
  m=o.modifiers.new('Soft worn edges','BEVEL');m.width=min(bevel,min(s)*.22);m.segments=2
  o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL')
 if solid:coll.append(dict(name=n,centre=list(pp),size=list(s),yaw=yaw))
 return o
def cyl(n,p,r,depth,mat='Brass',axis='y'):
 pp=pos(p);bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=r,depth=depth,location=cv(pp));o=bpy.context.object;assign(o,n,mat)
 if axis=='z':o.rotation_euler[0]=math.pi/2
 if axis=='x':o.rotation_euler[1]=math.pi/2
 for poly in o.data.polygons:poly.use_smooth=len(poly.vertices)==4
 return o
def sphere(n,p,s,mat):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,location=cv(pos(p)));o=bpy.context.object;o.scale=(s[0],s[2],s[1]);bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);assign(o,n,mat)
 for poly in o.data.polygons:poly.use_smooth=True
 return o
def rod(n,a,b,r,mat):
 a,b=Vector(cv(pos(a))),Vector(cv(pos(b)));v=b-a;bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=v.length,location=(a+b)/2);o=bpy.context.object;assign(o,n,mat);o.rotation_euler=v.to_track_quat('Z','Y').to_euler();return o
def prism(n,pts,z0,z1,mat):
 # Closed extrusion of a game-space XY outline between two Z planes; outward normals, metric UVs.
 verts=[cv((x,y,z)) for z in [z0,z1] for x,y in pts];k=len(pts)
 faces=[tuple(range(k)),tuple(range(2*k-1,k-1,-1))]+[(i,(i+1)%k,(i+1)%k+k,i+k) for i in range(k)]
 mesh=bpy.data.meshes.new(n);mesh.from_pydata(verts,[],faces);bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.recalc_face_normals(bm,faces=bm.faces);bm.to_mesh(mesh);bm.free()
 uv=mesh.uv_layers.new().data
 for f in mesh.polygons:
  axis=max(range(3),key=lambda q:abs(f.normal[q]));a,b=[q for q in range(3) if q!=axis]
  for li in f.loop_indices:co=mesh.vertices[mesh.loops[li].vertex_index].co;uv[li].uv=(co[a],co[b])
 mesh.materials.append(mats[mat]);o=bpy.data.objects.new(n,mesh);bpy.context.collection.objects.link(o);return o
def carve(o,cutter):
 # Bake an editable hollow (tub, basin, sink) instead of laying a dark plate on a solid block.
 bpy.context.view_layer.objects.active=o
 for m in list(o.modifiers):bpy.ops.object.modifier_apply(modifier=m.name)
 m=o.modifiers.new('Carved hollow','BOOLEAN');m.operation='DIFFERENCE';m.object=cutter;m.solver='EXACT';bpy.ops.object.modifier_apply(modifier=m.name)
 bpy.data.objects.remove(cutter,do_unlink=True);o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');return o
def furniture(n,p,a=0):
 global base,yaw,parent
 base=p;yaw=a;parent=empty(n)
def reset():
 global base,yaw,parent
 base=(0,0,0);yaw=0;parent=None
# Thick exterior walls built by rectangular partitions around real apertures.
def wall(n,axis,fixed,lo,hi,bottom,height,holes,material='Plaster',thick=.24,solid=True):
 reset();us=sorted(set([lo,hi]+[v for h in holes for v in (h[0]-h[1]/2,h[0]+h[1]/2)]));vs=sorted(set([bottom,bottom+height]+[v for h in holes for v in (h[2],h[2]+h[3])]))
 for u0,u1 in zip(us,us[1:]):
  for v0,v1 in zip(vs,vs[1:]):
   if u0<lo or u1>hi or v0<bottom or v1>bottom+height:continue
   u=(u0+u1)/2;v=(v0+v1)/2
   if any(abs(u-h[0])<h[1]/2-.001 and h[2]<v<h[2]+h[3] for h in holes):continue
   p=(u,v,fixed) if axis=='x' else (fixed,v,u);s=(u1-u0,v1-v0,thick) if axis=='x' else (thick,v1-v0,u1-u0)
   box(n,p,s,material,0,solid)
def window(n,axis,fixed,u,bottom,w=1.4,h=1.5,side=None):
 reset();a=0 if axis=='x' else 90;p=(u,bottom,fixed) if axis=='x' else (fixed,bottom,u);furniture(n,p,a)
 for xx in [-w/2,w/2]:box(n+' jamb',(xx,h/2,0),(.095,h+.15,.27),'Cream')
 for yy in [0,h]:box(n+' sill',(0,yy,0),(w+.18,.085,.31),'Cream')
 box(n+' mullion',(0,h/2,0),(.045,h,.055),'Green');box(n+' transom',(0,h*.6,0),(w,.04,.06),'Green')
 box(n+' glazing',(0,h/2,0),(w-.07,h-.07,.014),'Glass',0,True)
 openings.append(dict(name=n,centre=list(pos((0,h/2,0))),size=[w,h,.24],yaw=a))
 # Curtains have folds and a rail on the interior face; walls whose room lies on their positive side pass it explicitly.
 if side is None:side=1 if fixed<0 else -1
 rod(n+' curtain rail',(-w*.65,h+.13,side*.24),(w*.65,h+.13,side*.24),.017,'Walnut')
 for x0 in [-w*.52,w*.52]:
  for j in range(5):box(n+' curtain fold',(x0+(j-2)*.056,h*.46,side*(.25+(.025 if j%2 else 0))),(.061,h*.95,.035),'Fabric',.012)
 reset()
def doorway(n,p,a,inside=False):
 reset();furniture('DoorFrame_'+n,p,a);w=1.3;h=2.4
 for xx in [-w/2,w/2]:box('Door casing',(xx,(h-.065)/2,0),(.11,h-.065,.3),'Walnut')
 box('Door casing lintel',(0,h,0),(w+.18,.12,.3),'Walnut')
 pivot=pos((-w/2,0,0));mover=empty('Door_'+n,pivot)
 leaf=box('Panelled door',(0,h/2,0),(w-.12,h-.07,.07),'Green' if not inside else 'Walnut');mw=world(leaf);leaf.parent=mover;leaf.matrix_world=mw
 for yy,hh in [(.65,.86),(1.75,.83)]:
  for sign in [-1,1]:
   pane=box('Raised door panel',(0,yy,sign*.05),(w-.32,hh,.035),'Walnut');mw=world(pane);pane.parent=mover;pane.matrix_world=mw
 for sign in [-1,1]:
  knob=sphere('Brass door knob',(.44,1.05,sign*.11),(.04,.04,.04),'Brass');mw=world(knob);knob.parent=mover;knob.matrix_world=mw
 doors.append(dict(id=n,node=mover.name,pivot=list(pivot),yaw=a,width=w,height=h,inside=inside))
 reset()
# Ground plinth and upper floor with actual stairwell opening.
box('Ground timber floor',(0,.08,0),(16,.16,12),'Floor',.005,True)
box('Upper floor west',(-2.5,3.28,0),(11,.16,12),'Floor',.005,True)
box('Upper floor south',(5.5,3.28,-2.5),(5,.16,7),'Floor',.005,True)
box('Upper ceiling',(0,6.52,0),(16,.16,12),'Cream',.005,True)
box('Utility stone floor',(-5.36,.08,9.5),(5.28,.16,7),'Tile',.005,True)
box('Utility ceiling',(-5.36,3.1,9.5),(5.28,.14,7),'Cream',.005,True)
# Outer holes u,width,bottom,height.
south=[(-4.8,1.4,1.05,1.5),(4.8,1.4,1.05,1.5)]+[(u,1.4,4.25,1.5) for u in [-4.8,-.8,4.8]]
north=[(-5.6,1.3,.16,2.4),(-.1,1.4,1.05,1.5),(-5.2,1.4,4.25,1.5),(-.2,1.2,4.45,1.2)]
side=[(0,1.3,.16,2.4),(3.6,1.4,1.05,1.5),(-3.4,1.4,4.25,1.5)]
east=[(0,1.3,.16,2.4),(3.4,1.4,4.25,1.5)]
# Gable walls run through the corners and every outer wall rises to the roof line (6.65), leaving no corner notch or eaves slit.
wall('South wall','x',-6,-8.12,8.12,.16,6.49,south);wall('North wall','x',6,-8.12,8.12,.16,6.49,north)
wall('West wall','z',-8,-6,6,.16,6.49,side);wall('East wall','z',8,-6,6,.16,6.49,east)
for axis,fixed,holes in [('x',-6,south),('x',6,north),('z',-8,side),('z',8,[(3.4,1.4,4.25,1.5)])]:
 for i,(u,w,b,h) in enumerate(holes):
  if b>.2:window('Window_'+str(len(openings)+1),axis,fixed,u,b,w,h)
wall('Utility west','z',-8,-0+6,13,.16,3.08,[(10,1.4,1,1.5)])
wall('Utility east','z',-2.72,6,13,.16,3.08,[(10,1.4,1,1.5)])
wall('Utility north','x',13,-8.12,-2.6,.16,3.08,[])
# The utility room lies west of its east wall, so that window's curtains hang on the negative side.
window('Window_utility_w','z',-8,10,1);window('Window_utility_e','z',-2.72,10,1,side=-1)
# Every main-block room is papered on all walls; the outer walls get a thin non-colliding lining with the same apertures.
# South/north linings stop at the east/west linings' faces and the ground band stops under the plaster ceiling,
# so no two lining or ceiling faces share a plane.
for bottom,top in [(.16,3.18),(3.36,6.44)]:
 for axis,fixed,lo,hi,holes in [('x',-5.874,-7.868,7.868,south),('x',5.874,-7.868,7.868,north),('z',-7.874,-5.88,5.88,side),('z',7.874,-5.88,5.88,east)]:wall('Outer wall paper',axis,fixed,lo,hi,bottom,top-bottom,holes,'Wallpaper',.012,False)
# The stairwell is open through both storeys: carry the paper across the floor band on its two outer walls,
# otherwise an 18 cm plaster stripe shows between the ground and upper linings.
for axis,fixed,lo,hi in [('x',5.874,3.08,7.868),('z',7.874,1.08,5.88)]:wall('Outer wall paper',axis,fixed,lo,hi,3.18,.18,[],'Wallpaper',.012,False)
# Ground-floor rooms get a plaster ceiling under the timber upper floor; the stairwell stays open.
reset();box('Plaster ceiling',(-2.44,3.19,0),(10.88,.02,11.76),'Cream',0);box('Plaster ceiling',(5.44,3.19,-2.44),(4.88,.02,6.88),'Cream',0)
# Hall walls; eight room openings have timber architraves.
# Stairwell partitions continue through the slab band; no exposed horizontal slit.
wall('North corridor partition','x',1,-8,8,.16,6.24,[(u,1.3,.16+level,2.4) for level in [0,3.2] for u in [-5.2,-.2]]+[(4,1.4,.16,2.4),(6.4,1.4,3.36,2.4)],'Wallpaper',.16)
wall('South corridor partition','x',-1,-8,8,.16,6.24,[(-2.5,1.4,.16+level,2.4) for level in [0,3.2]]+[(4,1.3,3.36,2.4)],'Wallpaper',.16)
for xx in [-2,3]:wall('North room partition','z',xx,1,6,.16,6.24,[],'Wallpaper',.16)
wall('Bedroom partition','z',1,-6,-1,3.36,3.04,[],'Wallpaper',.16)
for level in [0,3.2]:
 for xx in [-5.2,-.2]:doorway(('upper-' if level else 'lower-')+str(xx),(xx,.16+level,1),0,True)
 doorway(('upper-' if level else 'lower-')+'south',(-2.5,.16+level,-1),180,True)
 if level:doorway('storage',(4,3.36,-1),180,True)
doorway('east',(8,.16,0),90);doorway('west',(-8,.16,0),90);doorway('utility',(-5.6,.16,6),0,True)
# Skirting on every room wall face: (axis, face, room side, span, door/fireplace cuts). The upper stairwell void has none.
# Runs along z stop one board thickness short of their corners and partition runs stop at the end walls' boards,
# so crossing boards never share a top face.
reset();SK=.036;W,E,S,N=-7.868,7.868,-5.868,5.868;door=.71
skirting={.16:[('x',S,1,W,E,[(0,1.26)]),('x',N,-1,W,E,[(-5.6,door)]),('z',W,1,S,N,[(0,door)]),('z',E,-1,S,N,[(0,door)]),
 ('x',-1.08,-1,W,E,[(-2.5,door)]),('x',-.92,1,W,E,[(-2.5,door)]),('x',.92,-1,W,E,[(-5.2,door),(-.2,door),(4,.7)]),('x',1.08,1,W,E,[(-5.2,door),(-.2,door),(4,.7)]),
 ('z',-2.08,-1,1.08,N,[]),('z',-1.92,1,1.08,N,[]),('z',2.92,-1,1.08,N,[]),('z',3.08,1,1.08,N,[])],
 3.36:[('x',S,1,W,E,[]),('x',N,-1,W,2.92,[]),('z',W,1,S,N,[]),('z',E,-1,S,.92,[]),
 ('x',-1.08,-1,W,E,[(-2.5,door),(4,door)]),('x',-.92,1,W,E,[(-2.5,door),(4,door)]),('x',.92,-1,W,E,[(-5.2,door),(-.2,door),(6.4,.7)]),('x',1.08,1,W,2.92,[(-5.2,door),(-.2,door)]),
 ('z',-2.08,-1,1.08,N,[]),('z',-1.92,1,1.08,N,[]),('z',2.92,-1,1.08,N,[]),('z',.92,-1,S,-1.08,[]),('z',1.08,1,S,-1.08,[])]}
for lev,faces in skirting.items():
 for axis,face,sign,lo,hi,cuts in faces:
  if axis=='z':lo,hi=lo+SK,hi-SK
  elif face not in (S,N):lo,hi=lo+SK*(lo==W),hi-SK*(hi==E)
  edges=[lo]+[v for c,h in sorted(cuts) for v in (c-h,c+h)]+[hi]
  for a,b in zip(edges[::2],edges[1::2]):
   a,b=max(a,lo),min(b,hi)
   if b-a<.02:continue
   mid=face+sign*SK/2
   # 2 mm above the floor datum so the board's underside never shares a plane with slab or partition faces.
   box('Wall-attached skirting',((a+b)/2,lev+.077,mid) if axis=='x' else (mid,lev+.077,(a+b)/2),(b-a,.15,SK) if axis=='x' else (SK,.15,b-a),'Walnut',.004)
# Two closed flights share exact tread/landing/rail datum points. All joinery lives in source.
stair=empty('Staircase');parent=stair
for i in range(9):
 for xx,z,top in [(4,1.28+i*.35,.16+(i+1)*3.2/18),(6.4,4.08-i*.35,1.76+(i+1)*3.2/18)]:
  box('Stair tread',(xx,top-.025,z),(1.32,.05,.36),'Walnut',.006,True)
  front=z+(.155 if xx>5 else -.155)
  box('Stair riser',(xx,top-.115,front),(1.28,.18,.04),'Walnut',.004,True)
# The landing runs back to the papered north wall; no open slot is left behind it.
box('Stair landing',(5.2,1.71,5.058),(3.72,.10,1.616),'Walnut',.008,True)
# The top tread previously stopped 10 cm short of the upper floor. The infill now ends where that tread begins,
# so the floor, infill and tread tops meet edge to edge instead of overlapping in one plane.
box('Stair upper threshold',(6.4,3.28,1.05),(1.4,.16,.10),'Walnut',.003,True)
# Continuous closed strings cover exposed riser ends. The cap follows the rise/run exactly.
def stair_string(x,reverse):
 z0,z1=1.105,4.255
 def upper(z):return ((3.36 if reverse else .16+3.2/18)+(-1 if reverse else 1)*(z-1.28)*3.2/18/.35)+.085
 y0,y1=upper(z0),upper(z1);bottom=.16 if not reverse else 1.76
 profile=[(z0,max(bottom,y0-.34)),(z1,max(bottom,y1-.34)),(z1,y1),(z0,y0)]
 verts=[cv((x+side*.05,y,z)) for side in [-1,1] for z,y in profile]
 faces=[(0,3,2,1),(4,5,6,7)]+[(i,(i+1)%4,(i+1)%4+4,i+4) for i in range(4)]
 mesh=bpy.data.meshes.new('Closed string profile');mesh.from_pydata(verts,[],[tuple(reversed(f)) for f in faces]);mesh.uv_layers.new()
 o=bpy.data.objects.new('Stair closed string',mesh);bpy.context.collection.objects.link(o);assign(o,o.name,'Walnut')
 for f in mesh.polygons:
  for li in f.loop_indices:
   v=mesh.vertices[mesh.loops[li].vertex_index].co;mesh.uv_layers.active.data[li].uv=(-v.y,v.z)
 # Mesh is a solid panel; its convex sloping collider is imported without a large AABB.
 return upper

def newel(x,y,z,height=1.04):
 box('Stair newel',(x,y+height/2,z),(.10,height,.10),'Walnut',.008,True)
 box('Stair newel cap',(x,y+height+.018,z),(.135,.036,.135),'Walnut',.008)

def rail(a,b):rod('Stair handrail',a,b,.04,'Walnut')
for xx,rev in [(3.38,False),(4.62,False),(5.78,True),(7.02,True)]:
 upper=stair_string(xx,rev)
 for i in range(1,8):
  z=1.28+i*.35;top=.16+(i+1)*3.2/18 if not rev else 1.76+(9-i)*3.2/18
  rod('Stair seated baluster',(xx,upper(z)-.018,z),(xx,top+.94,z),.021,'Walnut')
 foot=3.36 if rev else .16+3.2/18;end=1.76+3.2/18 if rev else 1.76
 newel(xx,foot-.05,1.28);newel(xx,1.71,4.43,1.20)
 rail((xx,foot+.94,1.28),(xx,end+.94,4.08))
 rail((xx,end+.94,4.08),(xx,2.85,4.43))
 # A short baluster at the last tread supports the bend before its landing newel.
 rod('Stair seated baluster',(xx,upper(4.08)-.018,4.08),(xx,end+.94,4.08),.021,'Walnut')
# Side guards run to wall newels; the old rail across the back of the landing guarded a slot that no longer exists.
for xx in [3.38,7.02]:
 rail((xx,2.85,4.43),(xx,2.85,5.80));newel(xx,1.71,5.80,1.20)
 for zz in [4.76,5.10,5.44]:rod('Landing baluster',(xx,1.76,zz),(xx,2.85,zz),.022,'Walnut')
# Guard the open centre well without crossing the walkable turn behind it.
rail((4.62,2.85,4.43),(5.78,2.85,4.43))
for xx in [4.91,5.20,5.49]:rod('Landing baluster',(xx,1.76,4.43),(xx,2.85,4.43),.022,'Walnut')
box('Landing front fascia',(5.2,1.60,4.29),(3.72,.20,.08),'Walnut',.005)
reset()
# Roof slabs and decorative coursing, solid closed upper ceiling below.
def roof(name,cx,cz,width,depth,eaves,rise):
 reset();slope=math.atan2(rise,width/2)
 for sign in [-1,1]:
  ob=box(name,(cx+sign*width/4,eaves+rise/2,cz+(.18 if name=='Rear wing roof' else 0)),(math.hypot(width/2,rise)+.35,.16,depth+(.09 if name=='Rear wing roof' else .45)),'Slate',.01)
  ob.rotation_euler[1]=sign*slope
  for j in range(15):
   z=cz-depth/2+(.14 if name=='Rear wing roof' else 0)+j*(depth-(.14 if name=='Rear wing roof' else 0))/14
   rod('Roof tile course',(cx-width/2,eaves+.1,z),(cx,eaves+rise+.1,z),.017,'Slate');rod('Roof tile course',(cx,eaves+rise+.1,z),(cx+width/2,eaves+.1,z),.017,'Slate')
 rod('Ridge cap',(cx,eaves+rise+.14,cz-depth/2+(.14 if name=='Rear wing roof' else -.2)),(cx,eaves+rise+.14,cz+depth/2+.2),.11,'Slate')
# The rear wing is pitched low enough that its ridge cap stays clear of the child's-room window above it.
roof('Main pitched roof',0,0,16,12,6.65,1.8);roof('Rear wing roof',-5.36,9.5,5.28,7,3.24,.55)
reset()
# Solid gables flush with the walls below (both main ends and the rear wing's north end); their slopes sit inside the roof slabs.
for z in [-6,6]:prism('Closed gable',[(-8.12,6.65),(8.12,6.65),(0,8.45)],z-.12,z+.12,'Plaster')
prism('Closed gable',[(-8.12,3.24),(-2.6,3.24),(-5.36,3.79)],12.88,13.12,'Plaster')
# Chimney rises on the ridge above the parlour fireplace, from the attic floor through the south gable.
box('Chimney',(0,7.9,-5.72),(.72,2.6,.78),'Plaster',.025);box('Chimney coping',(0,9.24,-5.72),(.89,.14,.95),'Cream')
# Gutters hang under the actual eaves (east/west on the main roof and the rear wing); downpipes swan-neck into them and end in shoes.
def downpipe(face,out,z,top,gutter_x,gutter_y):
 # face: outer wall plane; the pipe clears the stone plinth and is held by brackets fixed into the wall.
 x=face+out*.085;rod('Rain pipe',(x,.14,z),(x,top,z),.04,'Iron');rod('Rain pipe swan neck',(x,top,z),(gutter_x,gutter_y,z),.04,'Iron')
 rod('Rain pipe shoe',(x,.14,z),(x+out*.13,.06,z),.04,'Iron')
 for y in [1.2,top*.5+.6,top-.3]:box('Rain pipe bracket',(face+out*.045,y,z),(.09,.03,.1),'Iron',0)
for out in [-1,1]:rod('Eaves gutter',(out*8.27,6.5,-6.22),(out*8.27,6.5,6.22),.065,'Iron');downpipe(out*8.12,out,-5.9,6.3,out*8.27,6.46)
for xx,face,out in [(-8.24,-8.12,-1),(-2.48,-2.6,1)]:rod('Eaves gutter',(xx,3.12,6.12),(xx,3.12,13.2),.055,'Iron');downpipe(face,out,12.95,2.96,xx,3.09)
box('East porch canopy',(8.86,2.8,0),(1.48,.15,2.8),'Slate',.012)
for zz in [-1.15,1.15]:rod('Porch canopy bracket',(8.13,2.2,zz),(9.3,2.72,zz),.03,'Walnut');rod('Porch canopy bracket',(8.13,2.72,zz),(9.3,2.72,zz),.025,'Walnut')
reset()
# Stone base course over the floor-slab recess under every outer wall, broken only by the two door steps.
def plinth(x0,x1,z0,z1):
 if x1-x0>.01 and z1-z0>.01:box('Stone plinth',((x0+x1)/2,.13,(z0+z1)/2),(x1-x0,.34,z1-z0),'Slate',.006)
plinth(-8.16,8.16,-6.16,-6.11);plinth(-2.56,8.16,6.11,6.16);plinth(-8.16,-2.56,13.11,13.16);plinth(-2.61,-2.56,6.11,13.11)
for x0,x1 in [(8.11,8.16),(-8.16,-8.11)]:
 top=6.11 if x0>0 else 13.11;plinth(x0,x1,-6.11,-.8);plinth(x0,x1,.8,top)
for sign in [-1,1]:box('Door step',(sign*8.3,.06,0),(.6,.2,1.6),'Slate',.01,True)
# Furniture tools, turned legs, framed door panels and drawer trays.
def table(n,p,w=1.5,d=.75,h=.8,a=0):
 furniture(n,p,a);box(n+' top',(0,h,0),(w,.065,d),'Walnut',.02)
 for xx in [-w*.4,w*.4]:
  for zz in [-d*.35,d*.35]:
   cyl('Turned leg',(xx,(h-.0325)/2,zz),.037,h-.0325,'Walnut');sphere('Leg collar',(xx,h*.72,zz),(.05,.065,.05),'Walnut')
 box('Table apron',(0,h-.12,0),(w-.1,.15,d-.1),'Walnut',.012)
 coll.append(dict(name=n,centre=list(pos((0,h/2,0))),size=[w,h,d],yaw=a))
def cabinet(n,p,ids,w=1.3,h=1.25,d=.5,a=0,safe=False):
 furniture('Cabinet_'+n,p,a);mat='Iron' if safe else 'Walnut';cabparent=parent
 def attach(o,pivot):
  mw=world(o);o.parent=pivot;o.matrix_world=mw;return o
 for xx in [-w/2,w/2]:box('Cabinet stile',(xx,h/2,0),(.06,h,d),mat,solid=True)
 box('Cabinet back',(0,h/2,d/2),(w,h,.04),mat,solid=True)
 box('Cabinet top',(0,h+.04,0),(w+.10,.08,d+.08),mat,.012,True)
 box('Cabinet base',(0,.12,0),(w,.10,d),mat,solid=True)
 if n!='bath-mirror':
  for xx in [-w*.38,w*.38]:
   for zz in [-d*.3,d*.3]:sphere('Cabinet foot',(xx,.08,zz),(.055,.08,.055),mat)
 else:
  for xx in [-w*.32,w*.32]:box('Mirror cabinet wall cleat',(xx,h*.75,d/2+.037),( .08,.22,.074),mat,.004)
 moving=[];hinged=n in ['wardrobe','kitchen-larder','kitchen-crockery','bath-mirror','linen'];surfaces=[]
 if hinged or safe:
  for side in ([-1] if safe else [-1,1]):
   pivot=empty('Drawer_'+n+'_'+str(side),pos((side*w/2,0,-d/2)));moving.append(pivot.name)
   # Leaves fill the carcass front between base and top: 4 mm meeting gap, 4 mm top/bottom reveal.
   x=0 if safe else side*(w/4-.005);ww=w-.024 if safe else w/2-.014;lh=h-.178
   attach(box('Cupboard framed leaf',(x,h/2+.085,-d/2-.025),(ww,lh,.055),mat),pivot)
   attach(box('Inset door panel',(x,h/2+.085,-d/2-.061),(ww-.12,lh-.16,.025),'Mirror' if n=='bath-mirror' else mat),pivot)
   attach(sphere('Attached brass pull',(.28 if safe else side*.09,h*.53,-d/2-.10),(.025,.025,.025),'Brass'),pivot)
   if safe:
    for k in range(4):attach(cyl('Mechanical number wheel',(-.24+k*.13,h*.62,-d/2-.09),.047,.035,'Brass','z'),pivot)
  for yy in [.19,h*.48,h*.73]:
   box('Cupboard supported shelf',(0,yy-.018,0),(w-.08,.036,d-.06),mat,.003);surfaces.append((yy,None))
 else:
  for j in range(3):
   hh=(h-.20)/3;bottom=.19+j*hh;yy=bottom+hh*.48
   pivot=empty('Drawer_'+n+'_'+str(j),pos((0,0,0)));moving.append(pivot.name)
   attach(box('Drawer front',(0,yy,-d/2-.025),(w-.07,hh-.022,.055),mat),pivot)
   attach(box('Recessed drawer panel',(0,yy,-d/2-.06),(w-.20,max(.035,hh-.09),.022),mat),pivot)
   for xx in [-w*.27,w*.27]:attach(rod('Attached drawer pull',(xx-.055,yy,-d/2-.097),(xx+.055,yy,-d/2-.097),.012,'Brass'),pivot)
   attach(box('Drawer bottom',(0,bottom,0),(w-.12,.028,d-.06),mat,.003),pivot)
   for xx in [-w/2+.066,w/2-.066]:attach(box('Drawer side',(xx,bottom+hh*.35,0),(.024,hh*.66,d-.08),mat,.003),pivot)
   attach(box('Drawer rear',(0,bottom+hh*.35,d/2-.05),(w-.12,hh*.66,.024),mat,.003),pivot)
   surfaces.append((bottom+.014,pivot))
 # Every storage tier contains supported household objects. Evidence has its own free area.
 for j,(surface,pivot) in enumerate(surfaces):
  for k in range(2):
   xx=(-.27 if k==0 else .27)*w;zz=-d*.29 if n=='kitchen-crockery' else d*.27
   height=min(.055,(h-.20)/12);width=min(.26,w*.28);depth=min(.14,d*.28)
   material='Cream' if n in ['wardrobe','linen','bedside-a','bedside-b','old-trunk'] else 'Paper' if k==0 else 'Fabric'
   for layer in range(2 if hinged and n!='bath-mirror' else 1):
    ob=box('Stored folded linen' if material in ['Cream','Fabric'] else 'Stored tied parcel',(xx,surface+height/2+layer*height,zz),(width,height,depth),material,.012)
    if pivot:attach(ob,pivot)
   if material=='Paper':
    ob=box('Parcel cotton tie',(xx,surface+height+.001,zz),(.014,.002,depth+.001),'Cream',0)
    if pivot:attach(ob,pivot)
  if n in ['kitchen-larder','kitchen-crockery','bath-mirror']:
   cyl('Stored ceramic jar',(0,surface+.055,-d*.29 if n=='kitchen-crockery' else .015),min(.055,d*.22),.11,'Enamel');cyl('Jar lid',(0,surface+.116,-d*.29 if n=='kitchen-crockery' else .015),min(.059,d*.24),.013,'Brass')
 # Paper lies flat on a support, follows that drawer, and is independently selectable.
 evidence=[]
 for i,id in enumerate(ids):
  si=1 if hinged else len(surfaces)-1;surface,pivot=surfaces[si]
  width=min(.28,(w-.15)/max(1,len(ids))*.82)
  pp=((i-(len(ids)-1)/2)*(w-.15)/max(1,len(ids)),surface+(.115 if safe else .006),-.10 if safe else -d*.24)
  evidence.append(dict(id=id,position=list(pos(pp)),node=pivot.name if pivot else '',width=width,depth=.22 if safe else min(.18,d*.32),upright=safe))
 travel=min(.32,d*.62)
 cabinets.append(dict(id=n,title={'safe':'旧铁保险柜'}.get(n,'抽屉与柜格'),position=list(pos((0,h*.62,-d/2-.10))),size=[w,.75,.10],yaw=a,nodes=moving,offset=list((-travel*math.sin(math.radians(a)),0,-travel*math.cos(math.radians(a)))),items=ids,evidence=evidence,safe=safe,hinged=hinged,height=h))
 reset()
def chair(n,p,a=0):
 furniture(n,p,a);box('Chair seat',(0,.48,0),(.49,.065,.46),'Walnut',.025)
 for x in [-.19,.19]:
  for z in [-.17,.17]:rod('Chair leg',(x,0,z),(x,.47,z),.024,'Walnut')
 for x in [-.2,.2]:rod('Back post',(x,.45,.18),(x,1.0,.23),.026,'Walnut')
 for x in [-.1,0,.1]:rod('Back spindle',(x,.55,.2),(x,.94,.22),.013,'Walnut')
 box('Chair crest',(0,.99,.23),(.46,.08,.06),'Walnut',.02)
 coll.append(dict(name=n,centre=list(pos((0,.45,0))),size=[.48,.9,.48],yaw=a));reset()
def sofa(n,p,w=2.4,a=0):
 furniture(n,p,a);box('Sofa frame',(0,.29,0),(w,.25,.87),'Walnut',.06)
 box('Upholstered back',(0,.88,.32),(w-.1,.88,.28),'Fabric',.11)
 for x in [-w/2+.16,w/2-.16]:box('Rolled padded arm',(x,.66,-.02),(.34,.62,.93),'Fabric',.12)
 for i in range(3 if w>1.5 else 1):
  xx=(i-(1 if w>1.5 else 0))*(w-.62)/3;box('Seat cushion',(xx,.51,-.08),((w-.66)/(3 if w>1.5 else 1),.18,.68),'Fabric',.08)
 for x in [-w*.4,w*.4]:
  for z in [-.3,.3]:sphere('Sofa turned foot',(x,.12,z),(.065,.12,.065),'Walnut')
 coll.append(dict(name=n,centre=list(pos((0,.6,0))),size=[w,1.15,1],yaw=a));reset()
def bed(n,p,w=1.55,a=0):
 furniture(n,p,a);box('Bed frame',(0,.37,0),(w,.24,2.1),'Walnut',.03);box('Mattress',(0,.55,0),(w-.07,.23,2.02),'Cream',.07)
 # The blanket is tucked between the posts and drapes over the mattress sides instead of a slab hitting the finials.
 cover=w-.13;box('Wool cover',(0,.685,-.31),(cover,.05,1.28),'Fabric',.02)
 for xx in [-1,1]:box('Wool cover drape',(xx*(w/2-.03),.6,-.31),(.025,.19,1.28),'Fabric',.01)
 box('Wool cover fold',(0,.715,.31),(cover,.035,.14),'Fabric',.015)
 for xx in [-w/2,w/2]:
  for zz in [-1.02,1.02]:
   height=1.30 if zz>0 else .95
   cyl('Bedpost',(xx,height/2,zz),.055,height,'Walnut');sphere('Bed finial',(xx,height+.045,zz),(.072,.065,.072),'Walnut')
 box('Headboard',(0,.95,1.03),(w,.6,.065),'Walnut',.03);box('Footboard',(0,.6,-1.03),(w,.5,.06),'Walnut',.025)
 for xx in ([-w*.22,w*.22] if w>1.3 else [0]):box('Pillow',(xx,.725,.66),(w*.38 if w>1.3 else w*.68,.13,.42),'Cream',.06)
 coll.append(dict(name=n,centre=list(pos((0,.52,0))),size=[w+.15,1.05,2.2],yaw=a));reset()
def lamp(n,p):
 furniture(n,p);cyl('Lamp foot',(0,.04,0),.12,.05);rod('Lamp stem',(0,.07,0),(0,.42,0),.018,'Brass')
 bpy.ops.mesh.primitive_cone_add(vertices=24,radius1=.2,radius2=.10,depth=.22,location=cv(pos((0,.49,0))));assign(bpy.context.object,'Pleated lampshade','Cream');sphere('Lamp bulb',(0,.43,0),(.045,.065,.045),'Bulb');reset()
def bowl(n,p):
 # Teacup on a saucer at real scale (8.4 cm cup); the base point sits 4 mm below the supporting surface datum.
 furniture(n,p);cyl('Cup saucer',(0,.0085,0),.075,.009,'Enamel');cyl('Cup foot',(0,.017,0),.03,.008,'Enamel');cyl('Cup body',(0,.052,0),.042,.062,'Enamel');cyl('Tea surface',(0,.078,0),.037,.003,'Walnut');rod('Cup handle',(.04,.035,0),(.058,.068,0),.006,'Brass');reset()
# Furniture and its tabletop objects share anchors; rear clearances are measured to wall/skirting.
def at(p,dx=0,dy=0,dz=0):return (p[0]+dx,p[1]+dy,p[2]+dz)
SIDEBOARD=(7.48,.16,-3.7);DESK=(.2,.16,5.38);LAUNDRY=(-4.5,.16,12.44)
MASTER_BED=(-2.9,3.36,-4.70);BEDSIDE_A=(-4.28,3.36,-5.54);BEDSIDE_B=(-1.52,3.36,-5.54)
DRESSING=(-7.52,3.36,-5.08);CHILD_DESK=(-3.55,3.36,5.48);TOYS=(-3,3.36,1.41);SEWING=(3.3,3.36,-5.46)
# Main room furniture.
sofa('Three seat sofa',(-7.30,.16,-3.1),2.5,270);sofa('Club armchair',(-3.9,.16,-5.15),1.05,180)
reset();box('Woven sitting room rug',(-5.5,.171,-3.25),(4.6,.018,3.25),'Rug',0)
cabinet('sideboard',SIDEBOARD,['H05','H06'],2.4,1.1,.6,90);cabinet('coffee',(-5.6,.16,-3.2),[],1.25,.44,.7)
# Fireplace surround, tiled hearth and empty grate.
furniture('Fireplace',(0,.16,-5.82),0);box('Hearth',(0,.04,.336),(2.5,.08,.768),'Tile')
for xx in [-.85,.85]:box('Fire surround',(xx,.73,.08),(.3,1.46,.25),'Cream')
box('Mantel',(0,1.48,.11),(2.2,.12,.48),'Walnut');box('Fireback',(0,.64,.02),(1.4,1.2,.08),'Iron')
for xx in [-.55,-.3,-.05,.2,.45]:rod('Fire grate',(xx,.18,.38),(xx,.65,.38),.015,'Iron')
reset();lamp('Sitting room lamp',at(SIDEBOARD,dy=1.165,dz=-.45))
# Bookcase and desk.
table('Writing desk',DESK,2,.8,.8,180);chair('Desk chair',(.2,.16,4.48),180)
cabinet('desk',(-1.5,.16,5.45),['E09'],.7,.75,.6,0);cabinet('medical',(2.25,.16,5.49),['H09','H10'],1,1.12,.5,0)
cabinet('safe',(1.9,.16,1.53),['H11','H12','H13','H14'],.95,1.25,.7,180,True)
# Bookcase stands on the floor with a back board, cornice and plinth; each shelf is packed with volumes of varied size.
furniture('Bookcase',(-1.69,.16,3.5),90)
for yy in [.2,.7,1.2,1.7,2.2]:box('Shelf',(0,yy,0),(1.7,.06,.3),'Walnut')
for xx in [-.85,.85]:box('Bookcase stile',(xx,1.15,0),(.06,2.3,.34),'Walnut')
box('Bookcase back board',(0,1.15,-.155),(1.70,2.26,.02),'Walnut',.004);box('Bookcase cornice',(0,2.33,.01),(1.84,.06,.38),'Walnut',.012);box('Bookcase plinth',(0,.085,.02),(1.64,.17,.26),'Walnut',.006)
for row in range(4):
 x=-.81
 while True:
  wide=.026+random.random()*.03;height=.29+random.random()*.12
  if x+wide>.81:break
  box('Bound volume',(x+wide/2,.23+row*.5+height/2,.005+random.random()*.012),(wide,height,.19+random.random()*.04),random.choice(['Green','Green','Fabric','Slate','Walnut']),.003)
  x+=wide+.0015+(.06 if random.random()<.04 else 0)
reset();lamp('Desk light',at(DESK,dx=.7,dy=.8175,dz=-.05))
# Kitchen: freestanding range, sink and larder, rather than modern fitted kitchen.
table('Kitchen table',(-3.9,.16,3),1.6,.85,.78);chair('Kitchen chair A',(-3.9,.16,2.2),180);chair('Kitchen chair B',(-3.9,.16,3.85),0)
cabinet('kitchen-bills',(-4.16,.16,5.48),['H15','H16'],1.35,1.25,.5,0);cabinet('kitchen-larder',(-2.72,.16,5.48),[],1.05,1.8,.5,0);cabinet('kitchen-crockery',(-7.55,.16,3.6),[],1.2,.72,.5,270)
furniture('Cast iron cooker',(-3.0,.16,1.47),180);box('Range body',(0,.49,0),(.9,.98,.65),'Enamel',.04,True);box('Oven door',(0,.48,-.343),(.68,.48,.045),'Iron',.03)
for xx in [-.22,.22]:
 for zz in [-.15,.15]:cyl('Hotplate',(xx,.98,zz),.13,.012,'Iron')
# Flue rises, elbows back and enters the wall behind the range through a collar instead of stopping in mid-air.
rod('Stove flue',(.33,1,0),(.33,2.62,0),.07,'Iron');sphere('Stove flue elbow',(.33,2.62,0),(.07,.07,.07),'Iron');rod('Stove flue',(.33,2.62,0),(.33,2.62,.47),.07,'Iron');cyl('Flue wall collar',(.33,2.62,.385),.115,.02,'Iron','z');reset()
furniture('Deep sink',(-7.51,.16,1.72),270);box('Sink cupboard',(0,.38,0),(.9,.76,.6),'Green',.025,True)
carve(box('Enamel sink',(0,.86,0),(1,.2,.65),'Enamel',.035),box('Sink basin cutter',(0,.895,-.04),(.84,.21,.47),'Enamel',.03));cyl('Sink drain',(0,.792,-.04),.035,.006,'Iron')
rod('Tap',(0,.95,.25),(0,1.23,.25),.023,'Brass');rod('Spout',(0,1.23,.25),(0,1.23,.05),.023,'Brass');reset()
for p in [(-3.6,.9685,3),(-4.2,.9685,3),at(SIDEBOARD,dy=1.176)]:bowl('Tea cup',p)
cabinet('stationery',(2.62,.16,3.65),[],.9,.9,.45,90)
# Hall and rear wing.
cabinet('hall',(6.3,.16,-.65),['H01','H03'],1.6,.83,.38,180)
cabinet('utility',(-7.55,.16,11.85),['H19'],1.2,1.4,.5,270);table('Laundry washstand',LAUNDRY,1.8,.8,.82)
furniture('Wash tub',at(LAUNDRY,dy=.8525));carve(cyl('Galvanised tub',(0,.19,0),.38,.38,'Enamel'),cyl('Wash tub cutter',(0,.27,0),.35,.38,'Enamel'));reset()
for i in range(3):reset();box('Stacked linen',at(LAUNDRY,dx=-.6,dy=.875+i*.045),(.43,.045,.36),'Cream',.015)
# Upstairs bedrooms.
bed('Parents bed',MASTER_BED,1.8,180)
cabinet('bedside-a',BEDSIDE_A,['H22'],.6,.65,.45,180);cabinet('bedside-b',BEDSIDE_B,[],.6,.65,.45,180)
cabinet('wardrobe',(.48,3.36,-2.5),['H21'],1.8,2.15,.65,90);cabinet('dressing',DRESSING,['H20','H23'],1.4,.85,.48,270)
furniture('Dressing mirror',at(DRESSING,dx=-.18,dy=.92),270);box('Mirror frame',(0,.48,0),(.95,1,.08),'Walnut',.04);box('Mirror silver',(0,.48,-.05),(.82,.84,.012),'Mirror',.035)
for xx in [-.5,.5]:rod('Mirror swing post',(xx,-.02,0),(xx,.74,0),.02,'Walnut');sphere('Mirror pivot knob',(xx,.48,0),(.03,.03,.03),'Brass')
reset();lamp('Bedside light',at(BEDSIDE_A,dy=.715,dz=.075))
bed('Child bed',(-6.85,3.36,4.70),1.05,0);table('Child desk',CHILD_DESK,1.25,.62,.68,180);chair('Child chair',(-3.55,3.36,4.58),180)
cabinet('toys',TOYS,['H24'],1.3,.85,.45,180);cabinet('child-bedside',(-5.75,3.36,5.54),['H26'],.65,.65,.45,0)
# Original wooden horse toy.
# Original wooden pull-along horse on a wheeled board, its wheels resting on the toy cupboard top.
furniture('Toy horse',at(TOYS,dx=-.2,dy=.905));box('Horse wheel board',(0,.06,0),(.26,.02,.09),'Walnut',.004)
for xx in [-.09,.09]:
 for zz in [-.052,.052]:cyl('Horse wheel',(xx,.05,zz),.025,.012,'Iron','z')
sphere('Horse body',(0,.165,0),(.105,.045,.038),'Walnut');rod('Horse neck',(.075,.18,0),(.12,.255,0),.022,'Walnut');sphere('Horse head',(.145,.258,0),(.052,.026,.024),'Walnut')
rod('Horse tail',(-.1,.175,0),(-.135,.13,0),.008,'Fabric')
for xx in [-.065,.065]:
 for zz in [-.022,.022]:rod('Horse leg',(xx,.14,zz),(xx,.07,zz),.011,'Walnut')
reset()
# Bathroom with rolled rim tub, pedestal basin, mirror cabinet.
# Roll-top tub is a real hollow shell raised on four visible iron feet, with drain and end taps.
furniture('Clawfoot bath',(.1,3.36,5.38),0)
carve(box('Bath shell',(0,.45,0),(1.8,.56,.76),'Enamel',.12),box('Bath hollow cutter',(0,.51,0),(1.62,.56,.58),'Enamel',.1))
cyl('Bath drain',(-.62,.232,0),.03,.006,'Iron')
for zz in [-.09,.09]:rod('Bath tap',(-.84,.72,zz),(-.84,.84,zz),.017,'Brass');rod('Bath tap spout',(-.84,.84,zz),(-.74,.82,zz),.014,'Brass')
for xx in [-.65,.65]:
 for zz in [-.25,.25]:sphere('Bath foot',(xx,.095,zz),(.07,.095,.07),'Iron')
coll.append(dict(name='Clawfoot bath',centre=list(pos((0,.39,0))),size=[1.8,.78,.76],yaw=0));reset();cabinet('bath-mirror',(2.1,4.6,5.72),[],.8,.75,.18,0)
furniture('Pedestal basin',(2.1,3.36,5.53));cyl('Basin pedestal',(0,.40,0),.11,.8,'Enamel')
carve(box('Basin bowl',(0,.86,.02),(.58,.15,.48),'Enamel',.05),sphere('Basin bowl cutter',(0,.96,-.01),(.23,.15,.16),'Enamel'))
for xx in [-.13,.13]:rod('Basin tap',(xx,.935,.19),(xx,1.0,.19),.014,'Brass');rod('Basin tap spout',(xx,1.0,.19),(xx,.985,.11),.011,'Brass')
reset()
cabinet('linen',(6.9,3.36,-5.46),[],1.5,1.6,.6,180);cabinet('old-trunk',(7.47,3.36,-2.65),[],1.4,.55,.65,90)
table('Treadle sewing table',SEWING,1.25,.62,.79,180);chair('Sewing chair',at(SEWING,dz=.88),0)
furniture('Sewing machine',at(SEWING,dy=.8175));box('Sewing base',(0,.03,0),(.58,.05,.24),'Iron');box('Sewing upright',(.17,.2,0),(.12,.34,.17),'Iron',.03);box('Sewing arm',(-.02,.36,0),(.44,.12,.17),'Iron',.045);rod('Sewing needle',(-.22,.13,0),(-.22,.35,0),.008,'Iron');cyl('Hand wheel',(.28,.3,0),.15,.055,'Iron','x');reset()
# Stopped kitchen clock; not a historical timing reference.
# Wall clock: hollow case with the dial above and the stopped pendulum hanging, off-centre, behind a glazed lower door.
furniture('Kitchen clock',(-4.2,2.1,5.79))
box('Clock case',(0,0,.06),(.38,.59,.02),'Walnut',.006)
for xx in [-.2025,.2025]:box('Clock case',(xx,0,0),(.025,.65,.14),'Walnut',.006)
for yy in [-.31,.31]:box('Clock case',(0,yy,0),(.38,.03,.14),'Walnut',.006)
box('Clock dial board',(0,.145,-.06),(.38,.29,.02),'Walnut',.006);box('Clock door rail',(0,-.01,-.062),(.38,.03,.024),'Walnut',.004)
box('Clock door glass',(0,-.16,-.064),(.36,.27,.006),'Glass',0)
cyl('Clock face',(0,.145,-.077),.135,.014,'Cream','z');rod('Clock hand',(0,.145,-.088),(.055,.215,-.088),.006,'Iron');rod('Clock hand',(0,.145,-.091),(-.085,.12,-.091),.005,'Iron')
rod('Clock pendulum',(0,-.02,0),(.035,-.21,0),.008,'Brass');sphere('Pendulum bob',(.038,-.225,0),(.045,.045,.016),'Brass');reset()
# Table-top paperwork, family pictures, lamps and ceiling roses.
loose=[('E10',at(DESK,dx=-.15,dy=.839,dz=-.07)),('H04',at(SIDEBOARD,dy=1.1865,dz=.9)),('H17',(-7.82,1.25,.65)),('H18',(-4.2,2.1,5.83)),('H25',at(CHILD_DESK,dy=.719,dz=-.05))]
for i,p in enumerate([(-4,2.85,-3),(4,2.85,-3),(-4.9,2.85,3),(.5,2.85,3),(5.1,6.05,3),(-4.5,6.05,-3),(4.5,6.05,-3),(-4.9,6.05,3),(.3,6.05,3),(-5.2,2.8,10),(4.5,2.85,0)]):
 # Rose sits flush on the actual ceiling above it (plaster ceiling, upper ceiling or utility ceiling); the cord spans to the shade.
 ceiling=(3.03 if p[2]>6.2 else 3.18) if p[1]<3 else 6.44;top=ceiling-p[1]
 furniture('Pendant_'+str(i),p);cyl('Ceiling rose',(0,top-.0175,0),.16,.035,'Cream');rod('Pendant cord',(0,top-.03,0),(0,-.03,0),.013,'Iron');sphere('Milk glass shade',(0,-.16,0),(.22,.14,.22),'Cream');sphere('Warm bulb',(0,-.25,0),(.055,.065,.055),'Bulb');lights.append(list(pos((0,-.3,0))));reset()
# Adult female, 1.68 m head-to-heel; rounded head, neck, shoulders, waist, hips and relaxed limbs.
outline=[(-.07,.84),(.065,.84),(.11,.78),(.105,.65),(.065,.60),(.07,.55),(.18,.51),(.25,.34),(.34,.20),(.42,.14),(.43,.09),(.38,.065),(.32,.12),(.20,.22),(.14,.35),(.13,.12),(.17,-.06),(.17,-.23),(.12,-.47),(.17,-.72),(.23,-.79),(.21,-.84),(.105,-.83),(.08,-.76),(.02,-.48),(-.015,-.25),(-.055,-.48),(-.18,-.72),(-.18,-.80),(-.26,-.82),(-.30,-.77),(-.25,-.69),(-.14,-.43),(-.17,-.19),(-.16,-.05),(-.12,.12),(-.14,.34),(-.24,.25),(-.35,.30),(-.44,.34),(-.46,.39),(-.42,.42),(-.35,.37),(-.25,.34),(-.19,.50),(-.075,.55),(-.06,.60),(-.11,.67),(-.115,.77)]
furniture('Chalk outline',(.25,.166,-2.85),-25)
# Catmull-Rom curve sampled as a flat ribbon; no raised white pipes.
points=[]
for i,p in enumerate(outline):
 p0=Vector(outline[(i-1)%len(outline)]);p1=Vector(p);p2=Vector(outline[(i+1)%len(outline)]);p3=Vector(outline[(i+2)%len(outline)])
 for j in range(5):
  t=j/5;points.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
verts=[];faces=[]
for i,p in enumerate(points):
 tangent=(points[(i+1)%len(points)]-points[i-1]).normalized();normal=Vector((-tangent.y,tangent.x));width=.007*(.85+random.random()*.3)
 for side in [-1,1]:q=p+normal*width*side;verts.append(cv(pos((q.x,0,q.y))))
for i in range(len(points)):k=(i+1)%len(points);faces.extend([(i*2,k*2,k*2+1,i*2+1),(i*2+1,k*2+1,k*2,i*2)])
mesh=bpy.data.meshes.new('Chalk ribbon');mesh.from_pydata(verts,[],faces);mesh.materials.append(mats['Chalk']);mesh.uv_layers.new();o=bpy.data.objects.new('Adult woman chalk outline',mesh);bpy.context.collection.objects.link(o)
# Thin irregular dried pools and small drops, visibly burgundy without a body.
for i,(x,z,rx,rz) in enumerate([(.06,.62,.20,.23),(.12,.39,.16,.20),(.24,.50,.08,.12),(-.16,.73,.025,.035),(.31,.69,.018,.026)]):
 vv=[cv(pos((x,-.004,z)))]+[cv(pos((x+math.cos(k*math.tau/32)*rx*(.9+random.random()*.2),-.004,z+math.sin(k*math.tau/32)*rz*(.9+random.random()*.2)))) for k in range(32)]
 ff=[(0,k+1,(k+1)%32+1) for k in range(32)];ff+=[tuple(reversed(f)) for f in ff]
 me=bpy.data.meshes.new('Blood stain');me.from_pydata(vv,[],ff);me.materials.append(mats['Blood']);me.uv_layers.new();ob=bpy.data.objects.new('Dried burgundy blood '+str(i),me);bpy.context.collection.objects.link(ob)
reset()
# Sagging cloth cordon tied to actual stakes, with a 2.8 m entrance gap.
posts=[(10,-7),(10,-1.4),(10,1.4),(10,7),(-10,-7),(-10,7)]
for x,z in posts:
 cyl('Cordon stake',(x,.63,z),.043,1.22,'Walnut');sphere('Cordon post finial',(x,1.25,z),(.056,.056,.056),'Walnut')
for aa,bb in [(posts[0],posts[1]),(posts[2],posts[3]),(posts[4],posts[5]),(posts[4],posts[0])]:
 vv=[]
 for i in range(41):
  t=i/40;x=aa[0]*(1-t)+bb[0]*t;z=aa[1]*(1-t)+bb[1]*t;y=1.08-.14*math.sin(math.pi*t)
  vv.extend([cv((x,y-.037,z)),cv((x,y+.037,z))])
 ff=[]
 for i in range(40):ff.extend([(i*2,i*2+1,i*2+3,i*2+2),(i*2+2,i*2+3,i*2+1,i*2)])
 me=bpy.data.meshes.new('Tied cloth ribbon');me.from_pydata(vv,[],ff);me.materials.append(mats['Cream']);me.uv_layers.new();o=bpy.data.objects.new('Police cloth cordon',me);bpy.context.collection.objects.link(o)
 for x,z in [aa,bb]:sphere('Cordon knot',(x,1.08,z),(.055,.05,.05),'Cream')
# Join static meshes by material while keeping moveable leaves separate. No loss of editable objects in source.
reset()
for name,p in [('FrontMarker',(0,0,-1)),('RightMarker',(1,0,0)),('UpMarker',(0,1,0))]:empty(name,p)
bpy.ops.wm.save_as_mainfile(filepath=str(SRC/'House1931.blend'))
# Apply bevels for a predictable exported silhouette; source retains editable modifiers.
for o in list(bpy.context.scene.objects):
 if o.type=='MESH':
  bpy.context.view_layer.objects.active=o
  for m in list(o.modifiers):
   try:bpy.ops.object.modifier_apply(modifier=m.name)
   except:pass
bpy.ops.export_scene.fbx(filepath=str(ART/'House1931.fbx'),use_selection=False,object_types={'MESH','EMPTY'},apply_unit_scale=True,axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,use_mesh_modifiers=True)
manifest=dict(origin=[-24.5,0,-57],colliders=coll,openings=openings,doors=doors,cabinets=cabinets,lights=lights,loose=[dict(id=i,position=list(p)) for i,p in loose],rooms=8,storeys=2,triangles=sum(len(o.data.polygons) for o in bpy.context.scene.objects if o.type=='MESH'),objects=len(bpy.context.scene.objects))
(ART/'Layout.json').write_text(json.dumps(manifest,indent=2));(SRC/'layout.json').write_text(json.dumps(manifest,indent=2))
print('HOUSE_SOURCE_PASS',len(coll),len(openings),len(cabinets),manifest['objects'])
