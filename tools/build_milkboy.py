"""Build an editable, rigged milk-delivery child from the CC-BY Toon Boy asset.
Run with Blender 4.5 LTS --background --python-exit-code 1 --python tools/build_milkboy.py.
Original: silentsheri, https://sketchfab.com/3d-models/toon-boy-bffaf3a444e64ed0aa012e0cf43cf8c3
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector, Matrix, Quaternion
ROOT=Path(__file__).resolve().parents[1]
VENDOR=ROOT/'assets/source/milkboy/vendor'
OUT=ROOT/'game/Assets/Art/Milkboy'
REVIEW=ROOT/'output/demo/milkboy'
for p in (OUT,REVIEW):p.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(VENDOR/'source/Glassy Toon Boy.fbx'))
# Bake the FBX's centimetre/delta transforms into metre-scale Blender coordinates.
rot=Matrix.Rotation(-math.pi/2,4,'Z'); scale=6.12
original=[o for o in bpy.data.objects if o.type=='MESH' and o.name!='GLASSES']
transforms={o.name:rot@o.matrix_world for o in original}
for o in list(bpy.data.objects):
 if o not in original:bpy.data.objects.remove(o,do_unlink=True)
for o in original:
 for v in o.data.vertices:v.co=(transforms[o.name]@v.co)*scale
 o.parent=None;o.delta_scale=(1,1,1);o.delta_rotation_euler=(0,0,0);o.delta_location=(0,0,0);o.rotation_mode='XYZ';o.rotation_euler=(0,0,0);o.scale=(1,1,1);o.location=(0,0,0)
 for p in o.data.polygons:p.use_smooth=True
bpy.context.view_layer.update()

def material(name,color,roughness=.6,metallic=0,texture=None):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=roughness;p.inputs['Metallic'].default_value=metallic
 if texture:
  im=bpy.data.images.load(str(texture),check_existing=True);n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;m.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
 return m
cotton=material('Milkboy_Cotton',(.80,.77,.68),.83)
linen=material('Milkboy_Collar',(.91,.89,.80),.8)
skin=material('Milkboy_Hands',(.63,.36,.24),.56,texture=VENDOR/'textures/el.jpeg')
face=material('Milkboy_Face',(.64,.38,.25),.58,texture=VENDOR/'textures/Body3_1_TXTR.jpeg')
hairmat=material('Milkboy_Blond',(.48,.26,.066),.48)
browmat=material('Milkboy_Brows',(.25,.13,.042),.68)
leather=material('Milkboy_Leather',(.125,.052,.022),.57)
leatheredge=material('Milkboy_LeatherEdge',(.24,.13,.057),.65)
navy=material('Milkboy_Navy',(.018,.023,.031),.5)
shoe=material('Milkboy_Shoes',(.022,.019,.016),.34)
thread=material('Milkboy_Stitch',(.48,.40,.27),.8)
brass=material('Milkboy_Brass',(.47,.29,.085),.32,.75)
white=material('Milkboy_Sclera',(.82,.85,.83),.25)
blue=material('Milkboy_Iris',(.035,.26,.46),.28)
blueedge=material('Milkboy_IrisRim',(.01,.049,.085),.3)
pupil=material('Milkboy_Pupil',(.003,.006,.01),.18)
# Native procedural fabric, with UVs retained for Unity's woven normal texture.
for m in (cotton,linen):
 nt=m.node_tree;p=nt.nodes.get('Principled BSDF');tex=nt.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=330;tex.inputs['Detail'].default_value=2
 bump=nt.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.30;bump.inputs['Distance'].default_value=.0008;nt.links.new(tex.outputs['Fac'],bump.inputs['Height']);nt.links.new(bump.outputs['Normal'],p.inputs['Normal'])

def active(o):
 bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o

def separate_part(source,name,mi,side=0):
 o=bpy.data.objects.new(name,source.data.copy());bpy.context.collection.objects.link(o)
 bm=bmesh.new();bm.from_mesh(o.data)
 remove=[f for f in bm.faces if f.material_index!=mi or (side and sum(v.co.x for v in f.verts)/len(f.verts)*side<0)]
 bmesh.ops.delete(bm,geom=remove,context='FACES');bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS');bm.to_mesh(o.data);bm.free()
 o.data.materials.clear()
 for p in o.data.polygons:p.material_index=0;p.use_smooth=True
 return o
body=bpy.data.objects['BODY']
pants=separate_part(body,'Milkboy_Trousers',1);pants.data.materials.append(cotton)
shoes=separate_part(body,'Milkboy_LaceShoes',3);shoes.data.materials.append(shoe)
hands={s:separate_part(body,'Milkboy_Hand_'+s,2,sgn) for s,sgn in [('L',1),('R',-1)]}
for o in hands.values():o.data.materials.append(skin)
bpy.data.objects.remove(body,do_unlink=True)
head=bpy.data.objects['HEAD'];head.name='Milkboy_Head'
for i,m in enumerate(head.data.materials):
 if i==0:head.data.materials[i]=face
 elif i==1:head.data.materials[i]=browmat
 else:
  # Interior mouth uses matte, physically opaque materials.
  if m.use_nodes:
   p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Roughness'].default_value=.6;p.inputs['Alpha'].default_value=1
# The source includes a flared neck base; tuck it inside the new collar.
for v in head.data.vertices:
 if v.co.z<1.112:
  v.co.x=max(-.047,min(.047,v.co.x));v.co.y=max(-.015,min(.076,v.co.y))
hair=bpy.data.objects['hair'];hair.name='Milkboy_Hair';hair.data.materials.clear();hair.data.materials.append(hairmat)
# Tuck the crown under the cap while preserving the authored fringe and ear locks.
for v in hair.data.vertices:
 if v.co.z>1.365:v.co.z=1.365+(v.co.z-1.365)*.44
 if v.co.y>.14:v.co.y=.14+(v.co.y-.14)*.6
# Subtle inner-brow lift, without changing the original facial topology.
for v in head.data.vertices:
 if v.co.y<-.05 and 1.306<v.co.z<1.344 and abs(v.co.x)<.068:
  v.co.z+=.005*max(0,1-abs(v.co.x)/.068)

# A small complete hierarchy with individual finger phalanges.
armdata=bpy.data.armatures.new('MilkboySkeleton');rig=bpy.data.objects.new('MilkboyRig',armdata);bpy.context.collection.objects.link(rig);active(rig);bpy.ops.object.mode_set(mode='EDIT')
bones={}
def bone(name,h,t,parent=None):
 b=armdata.edit_bones.new(name);b.head=h;b.tail=t
 if parent:b.parent=armdata.edit_bones[parent]
 bones[name]=(Vector(h),Vector(t));return b
bone('Root',(0,0,0),(0,0,.12));bone('Hips',(0,.04,.61),(0,.035,.76),'Root');bone('Spine',(0,.035,.76),(0,.025,.91),'Hips');bone('Chest',(0,.025,.91),(0,.035,1.055),'Spine');bone('Head',(0,.035,1.055),(0,.03,1.36),'Chest')
for s,sgn in [('L',1),('R',-1)]:
 bone('Thigh.'+s,(sgn*.09,.05,.64),(sgn*.097,.046,.34),'Hips');bone('Shin.'+s,(sgn*.097,.046,.34),(sgn*.10,.04,.10),'Thigh.'+s);bone('Foot.'+s,(sgn*.10,.04,.10),(sgn*.10,-.11,.05),'Shin.'+s)
 W=Vector((sgn*.354,.067,.765));D=Vector((sgn*.7071,0,-.7071));Y=Vector((0,1,0));N=D.cross(Y).normalized()
 bone('UpperArm.'+s,(sgn*.134,.045,1.027),(sgn*.254,.056,.90),'Chest');bone('ForeArm.'+s,(sgn*.254,.056,.90),W,'UpperArm.'+s);bone('Hand.'+s,W,W+D*.105,'ForeArm.'+s)
 for fi,(finger,y,end) in enumerate([('Index',.033,.227),('Middle',.055,.234),('Ring',.078,.219),('Pinky',.095,.189)]):
  ss=[.105,.105+(end-.105)*.43,.105+(end-.105)*.75,end]
  pts=[W+D*a+Y*(y-W.y)+N*(-.003) for a in ss]
  for j in range(3):bone(finger+str(j+1)+'.'+s,pts[j],pts[j+1],'Hand.'+s if j==0 else finger+str(j)+'.'+s)
 pts=[W+D*.066+Y*(-.038),W+D*.096+Y*(-.066),W+D*.13+Y*(-.082),W+D*.15+Y*(-.082)]
 for j in range(3):bone('Thumb'+str(j+1)+'.'+s,pts[j],pts[j+1],'Hand.'+s if j==0 else 'Thumb'+str(j)+'.'+s)
bpy.ops.object.mode_set(mode='OBJECT')

def bind(o,weights):
 # weights(v.co) returns a map of bone name to scalar weight.
 groups={}
 for v in o.data.vertices:
  ww=weights(v.co);total=sum(ww.values())
  for name,w in ww.items():
   if w<=0:continue
   if name not in groups:groups[name]=o.vertex_groups.get(name) or o.vertex_groups.new(name=name)
   groups[name].add([v.index],w/total,'REPLACE')
 mod=o.modifiers.new('Milkboy skin','ARMATURE');mod.object=rig;mod.use_deform_preserve_volume=True;o.parent=rig

def segdist(p,a,b):
 t=max(0,min(1,(p-a).dot(b-a)/(b-a).length_squared));return (p-(a+(b-a)*t)).length
for s,sgn in [('L',1),('R',-1)]:
 W=bones['Hand.'+s][0];D=Vector((sgn*.7071,0,-.7071))
 def weights(p,s=s,W=W,D=D):
  dist=(p-W).dot(D)
  if dist<.077:return {'Hand.'+s:1}
  finger=('Thumb' if p.y<.014 else 'Index' if p.y<.044 else 'Middle' if p.y<.066 else 'Ring' if p.y<.086 else 'Pinky')
  names=['Hand.'+s]+[finger+str(j)+'.'+s for j in range(1,4)]
  scores=sorted([(segdist(p,*bones[n]),n) for n in names])[:2]
  return {n:1/max(.008,d)**4 for d,n in scores}
 bind(hands[s],weights)

# Pose both authored hands around the diagonal strap. Local finger joints curl into the palm.
final={
 'L':{'shoulder':(.137,.035,1.037),'elbow':(.223,-.045,.812),'wrist':(.055,-.160,.927),'direction':(-.87,0,.493)},
 'R':{'shoulder':(-.137,.035,1.040),'elbow':(-.215,-.04,.808),'wrist':(-.089,-.16,.851),'direction':(.91,0,.415)}}
def setpose(name,headpos,Q,scale_y=1):
 rest=rig.data.bones[name].matrix_local.to_3x3();m=(Q@rest).to_4x4();m.col[1]*=scale_y;m.translation=headpos;rig.pose.bones[name].matrix=m;bpy.context.view_layer.update()
# Deform the dense authored fingers with a continuous bend field. This avoids
# hard spatial weight boundaries tearing the webbing between the fingers.
hand_maps={}
for s,sgn in [('L',1),('R',-1)]:
 f=final[s];W=Vector(f['wrist']);Wr=bones['Hand.'+s][0]
 D=Vector((sgn*.7071,0,-.7071));Y=Vector((0,1,0));N=D.cross(Y).normalized()
 Df=Vector(f['direction']).normalized();Nf=Vector((0,-1,0));Yf=Nf.cross(Df).normalized()
 def deform(p,Wr=Wr,W=W,D=D,Y=Y,N=N,Df=Df,Yf=Yf,Nf=Nf):
  delta=p-Wr;u=delta.dot(D);v=delta.dot(Y);n=delta.dot(N)
  thumb=max(0,min(1,(.022-p.y)/.035))
  angle=.70*thumb;du=u-.070;dv=v+.028
  u=.070+math.cos(angle)*du-math.sin(angle)*dv
  v=-.028+math.sin(angle)*du+math.cos(angle)*dv
  start=.112-.010*thumb;radius=.032+.006*thumb
  if u>start:
   a=(u-start)/radius;u=start+radius*math.sin(a);n-=radius*(1-math.cos(a))
  return W+.85*(Df*u+Yf*v+Nf*n)
 hand_maps[s]=deform
 for v in hands[s].data.vertices:v.co=deform(v.co)
active(rig);bpy.ops.object.mode_set(mode='EDIT')
for s in ('L','R'):
 f=final[s]
 for name,h,t in [('UpperArm.'+s,f['shoulder'],f['elbow']),('ForeArm.'+s,f['elbow'],f['wrist'])]:
  b=armdata.edit_bones[name];b.head=h;b.tail=t
 for name in ['Hand.'+s]+[f+str(j)+'.'+s for f in ['Index','Middle','Ring','Pinky','Thumb'] for j in range(1,4)]:
  b=armdata.edit_bones[name];h,t=bones[name];b.head=hand_maps[s](h);b.tail=hand_maps[s](t)
bpy.ops.object.mode_set(mode='OBJECT')

created=[]
def mesh(name,verts,faces,mat,rigid='Chest'):
 me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);me.materials.append(mat)
 for p in me.polygons:p.use_smooth=True
 created.append(o)
 if rigid:bind(o,lambda p:{rigid:1})
 return o

def tube(name,points,radii,mat,sides=32,rigid='Chest'):
 pts=[Vector(p) for p in points];vs=[];fs=[]
 for i,c in enumerate(pts):
  tangent=(pts[min(i+1,len(pts)-1)]-pts[max(0,i-1)]).normalized();ref=Vector((0,1,0))
  if abs(tangent.dot(ref))>.95:ref=Vector((1,0,0))
  u=tangent.cross(ref).normalized();v=tangent.cross(u).normalized();rx,ry=radii[i] if isinstance(radii[i],tuple) else (radii[i],radii[i])
  for j in range(sides):
   a=j*2*math.pi/sides;vs.append(c+u*math.cos(a)*rx+v*math.sin(a)*ry)
 for i in range(len(pts)-1):
  for j in range(sides):a=i*sides+j;b=i*sides+(j+1)%sides;fs.append((a,b,b+sides,a+sides))
 fs.extend([tuple(reversed(range(sides))),tuple((len(pts)-1)*sides+j for j in range(sides))]);return mesh(name,vs,fs,mat,rigid)

def rounded_box(name,loc,dim,mat,bevel=.008,rigid='Chest'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mat);m=o.modifiers.new('Soft sewn edges','BEVEL');m.width=bevel;m.segments=4;bpy.ops.object.modifier_apply(modifier=m.name)
 for p in o.data.polygons:p.use_smooth=True
 m=o.modifiers.new('Weighted corner normals','WEIGHTED_NORMAL');bpy.ops.object.modifier_apply(modifier=m.name)
 created.append(o)
 if rigid:bind(o,lambda p:{rigid:1})
 return o

def seam(name,points,mat=thread,r=.0008,rigid='Chest'):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.resolution_u=12;sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
 for b,co in zip(sp.bezier_points,points):b.co=co;b.handle_left_type='AUTO';b.handle_right_type='AUTO'
 cu.bevel_depth=r;cu.bevel_resolution=3;o=bpy.data.objects.new(name,cu);bpy.context.collection.objects.link(o);cu.materials.append(mat);active(o);bpy.ops.object.convert(target='MESH');o=bpy.context.object
 if rigid:bind(o,lambda p:{rigid:1})
 created.append(o);return o

def ellipsoid(name,loc,scale,mat,rigid='Chest',segments=32,rings=16):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings,radius=1,location=loc);o=bpy.context.object;o.name=name;o.scale=scale;active(o);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.data.materials.append(mat)
 for p in o.data.polygons:p.use_smooth=True
 if rigid:bind(o,lambda p:{rigid:1})
 created.append(o);return o
# Tailored jacket with continuous elliptical rings and small fabric drape.
vs=[];fs=[];nr=28;ns=64
for i in range(nr):
 t=i/(nr-1);z=.636+t*.443
 rx=.125+.019*math.sin(t*math.pi)+.004*math.cos(t*6);ry=.073+.012*math.sin(t*math.pi);cy=.037
 if t>.83:rx*=1-(t-.83)*3.25;ry*=1-(t-.83)*2
 for j in range(ns):
  a=j*2*math.pi/ns;wr=.0018*math.sin(a*9+t*18)*(math.sin(math.pi*t)**2);vs.append((math.cos(a)*(rx+wr),cy+math.sin(a)*(ry+wr),z+.0015*math.cos(a*5)*math.sin(t*math.pi)))
for i in range(nr-1):
 for j in range(ns):a=i*ns+j;b=i*ns+(j+1)%ns;fs.append((a,b,b+ns,a+ns))
jacket=mesh('Milkboy_TailoredJacket',vs,fs,cotton)
# Smooth sleeves following the bent shoulder-elbow-wrist path; creases wrap the elbow.
for s in ('L','R'):
 S,E,W=[Vector(final[s][k]) for k in ('shoulder','elbow','wrist')];S.x-=.035 if s=='L' else -.035;S.z-=.015;points=[];radii=[]
 for i in range(25):
  t=i/24
  # Interpolating a softened corner keeps a readable elbow without cube intersections.
  if t<.46:c=S.lerp(E,t/.46)
  else:c=E.lerp(W,(t-.46)/.54)
  c=c+Vector((0,-.009*math.sin(t*math.pi),.009*math.exp(-((t-.46)/.15)**2)))
  r=(.056*(1-t)+.022*t)+.003*math.sin(t*math.pi*16)*math.exp(-((t-.5)/.2)**2)
  points.append(c);radii.append((r,r*.9))
 o=tube('Milkboy_Sleeve_'+s,points,radii,cotton,40,rigid=None)
 def sw(p,s=s,E=E):
  b=max(0,min(1,(p.z-E.z)/.10));return {'ForeArm.'+s:1} if (p-Vector(final[s]['wrist'])).length<.15 else {'UpperArm.'+s:1}
 bind(o,sw)
 direction=(W-E).normalized();cuff=tube('Milkboy_Cuff_'+s,[W-direction*.027,W+direction*.003],[.024,.023],linen,40,'Hand.'+s)
 for k in [-.024,-.004]:
  # Cuff edge follows its circular rim.
  pts=[];u=direction.cross(Vector((0,1,0))).normalized();v=direction.cross(u).normalized()
  for j in range(33):a=j*2*math.pi/32;pts.append(W+direction*k+.0235*(math.cos(a)*u+math.sin(a)*v))
  seam('Cuff stitch '+s+str(k),pts,thread,.00065,'Hand.'+s)
# Collar and shaped lapels, with thickness and rounded edges.
tube('Milkboy_StandingCollar',[(0,.034,1.025),(0,.032,1.098)],[(.086,.070),(.068,.060)],linen,48)
for sgn in (-1,1):
 verts=[(sgn*.043,-.016,1.08),(sgn*.108,-.041,1.031),(sgn*.072,-.064,.960),(sgn*.011,-.063,1.028)]
 o=mesh('Milkboy_Lapel_'+str(sgn),verts,[(0,1,2,3)],linen);active(o);m=o.modifiers.new('Collar thickness','SOLIDIFY');m.thickness=.004;bpy.ops.object.modifier_apply(modifier=m.name);m=o.modifiers.new('Collar soft edge','BEVEL');m.width=.003;m.segments=3;bpy.ops.object.modifier_apply(modifier=m.name)
 seam('Lapel topstitch'+str(sgn),[(sgn*.043,-.021,1.072),(sgn*.098,-.048,1.027),(sgn*.070,-.070,.972)],cotton,.0007)
# Bow tie, placket, fabric breast pockets and buttons.
for sgn in (-1,1):
 o=ellipsoid('Bow tie wing'+str(sgn),(sgn*.027,-.073,1.04),(.034,.014,.019),navy)
ellipsoid('Bow tie knot',(0,-.079,1.041),(.012,.017,.015),navy)
seam('Jacket front seam',[(0,-.046,.66),(0,-.052,.78),(0,-.055,.91),(0,-.058,1.018)],linen,.0025)
for z in [.99,.925,.86,.795,.73]:
 ellipsoid('Jacket brass button '+str(z),(.012,-.06,z),(.006,.0035,.006),brass)
for sgn in (-1,1):
 rounded_box('Breast pocket'+str(sgn),(sgn*.088,-.038,.938),(.069,.011,.069),cotton,.007)
 seam('Pocket seam'+str(sgn),[(sgn*.054,-.046,.968),(sgn*.12,-.038,.968),(sgn*.119,-.040,.912),(sgn*.088,-.050,.904),(sgn*.055,-.048,.912)],linen,.0009)
# Soft oval delivery cap. Crown, band and curved crescent visor are independent meshes.
capcenter=Vector((0,.045,0));vs=[];fs=[];caprings=[(1.383,.181,.189),(1.391,.187,.195),(1.41,.195,.203),(1.444,.201,.207),(1.471,.186,.193),(1.495,.146,.157),(1.507,.070,.085),(1.509,.004,.004)];ns=80
for ri,(z,rx,ry) in enumerate(caprings):
 for j in range(ns):
  a=j*math.tau/ns;fold=.0018*math.cos(a*12)*(1 if 1<ri<5 else .1);vs.append((math.cos(a)*(rx+fold),.015+math.sin(a)*(ry+fold),z+.002*math.cos(a)))
for i in range(len(caprings)-1):
 for j in range(ns):a=i*ns+j;b=i*ns+(j+1)%ns;fs.append((a,b,b+ns,a+ns))
o=mesh('Milkboy_SoftCap',vs,fs,linen,'Head');active(o);m=o.modifiers.new('Soft cap subdivision','SUBSURF');m.levels=2;bpy.ops.object.modifier_apply(modifier=m.name)
# Explicit oval band, no square primitives.
vs=[];fs=[]
for z in [1.376,1.402]:
 for j in range(81):a=j*math.tau/80;vs.append((.184*math.cos(a),.015+.192*math.sin(a),z))
for j in range(80):fs.append((j,j+1,j+82,j+81))
mesh('Milkboy_CapBand',vs,fs,navy,'Head')
vs=[];fs=[]
for k in range(6):
 t=k/5
 for j in range(49):
  a=-math.pi/2+j*math.pi/48
  x=.181*math.sin(a);y=.015-.19*math.cos(a)-.070*t*math.cos(a);z=1.378-.032*t*t*math.cos(a)+.007*math.sin(a)**2;vs.append((x,y,z))
for k in range(5):
 for j in range(48):a=k*49+j;fs.append((a,a+1,a+50,a+49))
o=mesh('Milkboy_CurvedVisor',vs,fs,navy,'Head');active(o);m=o.modifiers.new('Visor thickness','SOLIDIFY');m.thickness=.005;bpy.ops.object.modifier_apply(modifier=m.name)
ellipsoid('Cap brass badge',(0,-.191,1.414),(.020,.005,.014),brass,'Head')
# Eyeballs retain the source sockets. Iris patches lie on their spherical surfaces.
for name in ['R_EYE','R_EYE_1']:
 o=bpy.data.objects[name];o.name='Milkboy_'+name;o.data.materials.clear();o.data.materials.append(white);active(o);sub=o.modifiers.new('Round eyeball','SUBSURF');sub.levels=2;bpy.ops.object.modifier_apply(modifier=sub.name)
 c=Vector([(min(v.co[i] for v in o.data.vertices)+max(v.co[i] for v in o.data.vertices))/2 for i in range(3)]);r=.0355
 oldname=o.name;bpy.data.objects.remove(o,do_unlink=True);o=ellipsoid(oldname,c,(.035,.035,.035),white,None,48,32)
 for label,rad,mat in [('Limbus',.0165,blueedge),('Blue iris',.0145,blue),('Pupil',.0065,pupil)]:
  verts=[];faces=[]
  for k in range(6):
   rr=rad*k/5
   for j in range(49):
    a=j*math.tau/48;x=rr*math.cos(a);z=rr*math.sin(a);y=-math.sqrt(max(0,r*r-rr*rr))-.0005*(3 if label=='Pupil' else 2 if label=='Blue iris' else 1);verts.append(c+Vector((x,y,z)))
  for k in range(5):
   for j in range(48):a=k*49+j;faces.append((a,a+1,a+50,a+49))
  mesh(o.name+' '+label,verts,[tuple(reversed(f)) for f in faces],mat,'Head')
 ellipsoid(o.name+' catchlight',c+Vector((-.006,-.0352,.007)),(.002,.0007,.002),white,'Head',16,8)
 bind(o,lambda p:{'Head':1})
bind(head,lambda p:{'Head':1});bind(hair,lambda p:{'Head':1})
bind(pants,lambda p:{'Thigh.'+('L' if p.x>0 else 'R'):1} if p.z>.3 else {'Shin.'+('L' if p.x>0 else 'R'):1})
bind(shoes,lambda p:{'Foot.'+('L' if p.x>0 else 'R'):1})
# Small structured leather satchel, a soft flap and complete shoulder loop.
rounded_box('Milkboy_Satchel',(.208,.018,.664),(.184,.098,.206),leather,.018,'Hips')
rounded_box('Milkboy_SatchelFlap',(.208,-.039,.710),(.193,.018,.110),leather,.018,'Hips')
seam('Satchel flap stitching',[(.124,-.05,.748),(.122,-.051,.674),(.208,-.052,.663),(.294,-.051,.674),(.292,-.05,.748)],thread,.0009,'Hips')
rounded_box('Satchel clasp',(.208,-.054,.679),(.024,.008,.036),brass,.005,'Hips')
strap_points=[(-.105,.03,1.058),(-.111,-.048,1.035),(-.09,-.089,1.004),(-.056,-.108,.963),(-.015,-.114,.913),(.041,-.105,.849),(.119,-.077,.778),(.203,-.035,.745)]
def ribbon(name,points,width,mat):
 # Catmull-Rom sampling with a continuous narrow ribbon following the torso.
 pts=[Vector(p) for p in points];samples=[]
 for i in range(len(pts)-1):
  a=pts[max(0,i-1)];b=pts[i];c=pts[i+1];d=pts[min(len(pts)-1,i+2)]
  for j in range(10):t=j/10;samples.append(.5*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t))
 samples.append(pts[-1]);vs=[];faces=[]
 for i,p in enumerate(samples):
  tangent=(samples[min(i+1,len(samples)-1)]-samples[max(0,i-1)]).normalized();u=Vector((tangent.z,0,-tangent.x)).normalized()
  for side in [-1,1]:vs.append(p+u*width*.5*side)
 for i in range(len(samples)-1):a=i*2;faces.append((a,a+1,a+3,a+2))
 o=mesh(name,vs,faces,mat);active(o);m=o.modifiers.new('Leather strap thickness','SOLIDIFY');m.thickness=.006;bpy.ops.object.modifier_apply(modifier=m.name)
 for side in [-1,1]:seam(name+' edge '+str(side),[p+Vector((.01*side,-.004,.008*side)) for p in samples[::5]],thread,.00055)
 return o
ribbon('Milkboy_FrontShoulderStrap',strap_points,.034,leather)
ribbon('Milkboy_BackShoulderStrap',[(-.105,.03,1.058),(-.12,.116,1.0),(-.02,.135,.87),(.13,.125,.75),(.215,.05,.744)],.034,leather)
# Small hesitant head tilt; both the cap and eyes share the same head bone.
pb=rig.pose.bones['Head'];pb.rotation_mode='XYZ';pb.rotation_euler=(math.radians(3),math.radians(-3),math.radians(2))
bpy.context.view_layer.update()
# UVs for newly authored clothing and props.
for o in created:
 if not o.data.uv_layers:
  active(o);bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(island_margin=.015);bpy.ops.object.mode_set(mode='OBJECT')
# Keep source materials editable and copy the original skin textures into the Unity asset folder.
import shutil
for src,name in [('el.jpeg','Hands.jpeg'),('Body3_1_TXTR.jpeg','Face.jpeg')]:shutil.copy2(VENDOR/'textures'/src,OUT/name)
for m in [skin,face]:
 for n in m.node_tree.nodes:
  if n.type=='TEX_IMAGE' and n.image:n.image.filepath=str(OUT/('Hands.jpeg' if m==skin else 'Face.jpeg'))
# Render real geometry for artistic review.
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.render.engine='CYCLES';scene.cycles.samples=32;scene.render.resolution_x=1000;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs[0].default_value=(.20,.23,.28,1);scene.world.node_tree.nodes['Background'].inputs[1].default_value=.5
for loc,power,size in [((2,-3,4),450,3),((-3,-1,2.3),260,3),((1,2,3.5),450,2.5)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.size=size;l.rotation_euler=(Vector((0,0,.9))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(1.8,-4,1.8));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.81))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=1.80;scene.camera=cam
scene.view_settings.view_transform='AgX';scene.render.image_settings.file_format='PNG'
# FBX stores a real rig, weights and the current neutral pose.
active(rig)
for o in bpy.data.objects:
 if o.type=='MESH' and o.parent==rig:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'Milkboy.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True,path_mode='RELATIVE')
bpy.data.orphans_purge(do_recursive=True)
for im in bpy.data.images:
 if im.source=='FILE' and Path(bpy.path.abspath(im.filepath)).is_file():im.pack()
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(ROOT/'assets/source/milkboy/Milkboy.blend'))
for name,pos,target,zoom in [('model-full',(1.8,-4,1.8),(0,0,.81),1.80),('model-face',(1.1,-3,1.6),(0,0,1.285),.57),('model-hands',(.5,-3,1.1),(0,-.10,.94),.44),('model-side',(3,-.7,1.5),(0,0,.9),1.7)]:
 cam.location=pos;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=zoom;scene.render.filepath=str(REVIEW/(name+'.png'));bpy.ops.render.render(write_still=True)
manifest={'source':'Toon Boy','author':'silentsheri','url':'https://sketchfab.com/3d-models/toon-boy-bffaf3a444e64ed0aa012e0cf43cf8c3','license':'CC-BY-4.0','bones':len(rig.data.bones),'meshes':len([o for o in rig.children if o.type=='MESH']),'vertices':sum(len(o.data.vertices) for o in rig.children if o.type=='MESH'),'height_m':1.52,'changes':['removed glasses and hoodie','new tailored uniform, delivery cap, satchel','blond hair, blue irises','new articulated skeleton, curled fingers and guarded pose']}
(REVIEW/'model-manifest.json').write_text(json.dumps(manifest,indent=2));print('MILKBOY_ASSET_EXPORTED',manifest)
