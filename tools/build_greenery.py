"""Editable, deterministic botanical kit. Blender --background --python-exit-code 1 --python."""
import bpy,math,random,json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'assets/source/greenery';ART=ROOT/'game/Assets/Art/Greenery'
OUT.mkdir(parents=True,exist_ok=True);ART.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
for d in list(bpy.data.materials):bpy.data.materials.remove(d)
mat=bpy.data.materials.new('GardenPalette');mat.use_nodes=True;bs=mat.node_tree.nodes.get('Principled BSDF');bs.inputs['Roughness'].default_value=.79
for filename,socket in [('GreeneryPalette.png','Base Color'),('GreeneryNormal.png','Normal')]:
 image=bpy.data.images.load(str(ART/filename));image.pack();tex=mat.node_tree.nodes.new('ShaderNodeTexImage');tex.image=image
 if socket=='Normal':
  image.colorspace_settings.name='Non-Color';normal=mat.node_tree.nodes.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.8;mat.node_tree.links.new(tex.outputs['Color'],normal.inputs['Color']);mat.node_tree.links.new(normal.outputs['Normal'],bs.inputs[socket])
 else:mat.node_tree.links.new(tex.outputs['Color'],bs.inputs[socket])
class Mesh:
 def __init__(self):self.v=[];self.f=[];self.c=[];self.uv=[]
 def face(self,points,color,uv=None):
  start=len(self.v);self.v.extend([tuple(p) for p in points]);self.f.append(tuple(range(start,start+len(points))));self.c.append(color)
  self.uv.append(uv or [(0,0),(1,0),(1,1),(0,1)][:len(points)])
 def tube(self,points,radii,rng,sides=7,color=0):
  rows=[]
  for k,point in enumerate(points):
   axis=(Vector(points[min(k+1,len(points)-1)])-Vector(points[max(0,k-1)])).normalized();other=Vector((0,1,0)) if abs(axis.y)<.95 else Vector((1,0,0));u=axis.cross(other).normalized();v=axis.cross(u).normalized()
   rows.append([Vector(point)+(u*math.cos(i*math.tau/sides)+v*math.sin(i*math.tau/sides))*radii[k] for i in range(sides)])
  for k in range(len(rows)-1):
   for i in range(sides):self.face([rows[k][i],rows[k][(i+1)%sides],rows[k+1][(i+1)%sides],rows[k+1][i]],color)
 def leaf(self,p,d,length,width,color,rng,detail=True):
  p=Vector(p);d=Vector(d).normalized();n=Vector((rng.uniform(-.22,.22),rng.uniform(-.22,.22),1)).normalized();w=n.cross(d).normalized()*width
  mid=p+d*length*.48+n*length*.075
  pts=[p,p+d*length*.25-w*.78,p+d*length*.66-w, p+d*length,p+d*length*.66+w,p+d*length*.25+w*.78]
  uv=[(.5,0),(.1,.25),(0,.66),(.5,1),(1,.66),(.9,.25)]
  if detail:
   for i in range(6):self.face([pts[i],pts[(i+1)%6],mid],color,[uv[i],uv[(i+1)%6],(.5,.48)])
  else:self.face([pts[0],pts[2],pts[3],pts[4]],color,[uv[0],uv[2],uv[3],uv[4]])
 def orb(self,p,scale,color,rng,rings=3,sides=7):
  p=Vector(p);rows=[]
  for j in range(rings+1):
   a=-math.pi/2+math.pi*j/rings;rows.append([p+Vector((math.cos(a)*math.cos(i*math.tau/sides)*scale[0],math.cos(a)*math.sin(i*math.tau/sides)*scale[1],math.sin(a)*scale[2])) for i in range(sides)])
  for j in range(rings):
   for i in range(sides):
    if j==0:self.face([rows[j][i],rows[j+1][(i+1)%sides],rows[j+1][i]],color,[(.5,0),(1,1),(0,1)])
    elif j==rings-1:self.face([rows[j][i],rows[j][(i+1)%sides],rows[j+1][i]],color,[(0,0),(1,0),(.5,1)])
    else:self.face([rows[j][i],rows[j][(i+1)%sides],rows[j+1][(i+1)%sides],rows[j+1][i]],color)
 def blossom(self,p,size,color,rng,petals=8,detail=True):
  p=Vector(p)
  for j in range(petals):
   a=j*math.tau/petals;d=Vector((math.cos(a),math.sin(a),rng.uniform(.05,.27)))
   self.leaf(p-d*size*.05,d,size,size*(.20 if petals>6 else .38),color,rng,detail)
  self.orb(p+Vector((0,0,size*.06)),(size*.16,size*.16,size*.10),24,rng,2,6)
 def object(self,name):
  mesh=bpy.data.meshes.new(name);mesh.from_pydata(self.v,[],self.f);mesh.materials.append(mat);mesh.update();uv=mesh.uv_layers.new(name='UVMap')
  for poly,color,coords in zip(mesh.polygons,self.c,self.uv):
   for li,(u,v) in zip(poly.loop_indices,coords):uv.data[li].uv=((color%8+.045+.91*u)/8,(3-color//8+.045+.91*v)/4)
  obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj);return obj

def tree(variant,lod):
 rng=random.Random(1931+variant*97);m=Mesh();height=[7,8.4,6.6,7.5,7.2,5.9,8.2][variant];bark=[0,2,0,1,0,1,3][variant];leafcol=[6,10,8,9,12,7,15][variant]
 trunk=[(0,0,0),(.025,-.02,.5),(-.05,.01,2),(.03,.05,3.5),(.12,.06,height-.7)]
 if variant==2:trunk[-1]=(.2,.1,height-1.6)
 m.tube(trunk,[.17,.14,.105,.075,.018],rng,12 if lod==0 else 8,bark)
 if lod<2:
  for k in range(6):
   a=k*math.tau/6;m.tube([(math.cos(a)*.33,math.sin(a)*.33,.006),(0,0,.38)],[.028,.085],rng,5,bark)
 clusters=[]
 for k in range(22 if variant!=6 else 27):
  a=k*2.399+variant*.7;level=k/(21 if variant!=6 else 26)
  radius=([1.22,.78,1.65,1.13,1.38,1.36,1.4][variant])*(.68+math.sin(level*math.pi)*.48-level*.4)
  z=(3.45 if variant not in [2,5,6] else 2.65)+level*(height-(3.8 if variant not in [2,5,6] else 3.0))
  if variant==6:radius=1.65*(1-level*.83);z=2.6+level*(height-2.9)
  end=Vector((math.cos(a)*radius,math.sin(a)*radius,z));base=Vector((.03,0,2.05+level*(height-3.3)))
  bend=base.lerp(end,.57)-Vector((0,0,.10));m.tube([base,bend,end],[.058*(1-level*.65),.027,.007],rng,7 if lod==0 else 5,bark)
  sc=Vector(([.91,.74,1.10,.85,1.03,.83,.79][variant],[.87,.69,.95,.85,1.01,.82,.75][variant],[.86,1.02,.62,.94,.85,.72,.28][variant]))
  clusters.append((end,sc))
  if variant==4:
   for side in [-1,1]:
    tip=end+Vector((math.cos(a)*.36,math.sin(a)*.36,-.75));m.tube([end,end.lerp(tip,.5)+Vector((0,0,.10)),tip],[.01,.005,.002],rng,4,bark);clusters.append((tip,Vector((.44,.44,.7))))
 clusters.append((Vector((.1,0,height-.45)),Vector((.55,.55,.55))))
 # Geometry leaves, no opaque crown balls. Coarser leaves retain volume at distance.
 for p,sc in clusters:
  count=[125,49,18][lod] if variant!=6 else [90,38,16][lod]
  for j in range(count):
   a=rng.uniform(0,math.tau);c=rng.uniform(-1,1);s=math.sqrt(1-c*c);rad=rng.random()**.23;v=Vector((math.cos(a)*s,math.sin(a)*s,c))
   origin=p+Vector((v.x*sc.x,v.y*sc.y,v.z*sc.z))*rad
   direction=Vector((math.cos(a+rng.uniform(-1.8,1.8)),math.sin(a+rng.uniform(-1.8,1.8)),rng.uniform(-.2,.65)))
   length=rng.uniform(.22,.35)*[1,1.43,2.02][lod];width=length*([.41,.34,.45,.42,.17,.41,.20][variant])
   m.leaf(origin,direction,length,width,min(15,leafcol+rng.choice([0,0,1])),rng,lod==0)
 return m

def shrub(variant,lod):
 rng=random.Random(441+variant);m=Mesh();height=[.65,.83,.55][variant]
 for k in range(7):
  a=k*2.399;p=Vector((math.cos(a)*.25,math.sin(a)*.25,height*.67));m.tube([(0,0,0),p],[.017,.005],rng,5,0)
  for i in range(45 if lod==0 else 20):
   a=rng.uniform(0,math.tau);c=rng.uniform(-.6,1);d=Vector((math.cos(a)*math.sqrt(1-c*c),math.sin(a)*math.sqrt(1-c*c),c));pos=p+Vector((d.x*.31,d.y*.31,d.z*height*.45))
   m.leaf(pos,(d.x,d.y,.45),rng.uniform(.13,.21)*(1 if lod==0 else 1.4),.063, [8,10,12][variant]+i%2,rng,lod==0)
 return m

def groundplant(variant,lod,kind):
 rng=random.Random(190+variant+{'grass':0,'flower':880,'ornamental':370}[kind]);m=Mesh()
 if kind in ['grass','ornamental']:
  count=65 if lod==0 else 25
  for k in range(count):
   a=rng.uniform(0,math.tau);r=math.sqrt(rng.random())*.34;p=Vector((math.cos(a)*r,math.sin(a)*r,0));h=rng.uniform(.14,.33) if kind=='grass' else rng.uniform(.60,1.03)
   d=Vector((math.cos(a)*.5,math.sin(a)*.5,1));m.leaf(p,d,h,.010 if kind=='grass' else .012, [6,10,12][variant%3]+k%2,rng,lod==0)
   if kind=='ornamental' and k%(3 if lod==0 else 2)==0:
    top=p+d.normalized()*h;m.tube([p,top+Vector((0,0,.25))],[.0025,.001],rng,3,25)
    for j in range(7 if lod==0 else 4):m.orb(top+Vector((0,0,j*.035)),(.034*(1-j*.07),.032*(1-j*.07),.045),23 if variant==0 else 22,rng,2,5)
  return m
 # Dense basal leaves bridge the spaces between flowers and conceal bare bed soil.
 for j in range(85 if lod==0 else 35):
  a=rng.uniform(0,math.tau);r=math.sqrt(rng.random())*.37;p=Vector((math.cos(a)*r,math.sin(a)*r,rng.uniform(.06,.26)))
  m.leaf(p,(math.cos(a),math.sin(a),rng.uniform(.35,1)),rng.uniform(.16,.30)*(1 if lod==0 else 1.35),.045 if variant!=4 else .10,8+j%4,rng,lod==0)
 count=[38,22,29,24,9,32,28][variant];count=count if lod==0 else max(5,int(count*.44))
 for k in range(count):
  a=rng.uniform(0,math.tau);r=math.sqrt(rng.random())*.37;p=Vector((math.cos(a)*r,math.sin(a)*r,0));h=rng.uniform(*[(.27,.45),(.58,.91),(.55,.82),(.42,.66),(.52,.73),(.24,.42),(.38,.61)][variant])
  top=p+Vector((rng.uniform(-.07,.07),rng.uniform(-.07,.07),h));m.tube([p,top],[.0038,.0018],rng,4,25)
  for j in range(5 if lod==0 else 3):
   b=a+j*2.399;m.leaf(p.lerp(top,(j+1)/(7 if lod==0 else 5)),(math.cos(b),math.sin(b),.32),rng.uniform(.18,.24),.055 if variant!=4 else .085,9+j%2,rng,lod==0)
  if variant==1:
   # Upright salvia/lavender spikes, with several whorls of little flowers.
   for ring in range(7 if lod==0 else 4):
    z=ring*.025;rad=.034*(1-ring*.055)
    for j in range(3):
     b=j*math.tau/3+ring*.8;m.orb(top+Vector((math.cos(b)*rad,math.sin(b)*rad,z)),(.023,.022,.026),18 if ring%2 else 19,rng,2,5)
  elif variant==4:
   # Hydrangea-like mounded inflorescence made of individual florets.
   for j in range(23 if lod==0 else 11):
    b=j*2.399;c=rng.uniform(.1,1);d=Vector((math.cos(b)*math.sqrt(1-c*c),math.sin(b)*math.sqrt(1-c*c),c))
    m.blossom(top+d*.12,.037 if lod==0 else .046,21 if j%3 else 19,rng,4,False)
  else:m.blossom(top,[.061,.04,.078,.068,.04,.043,.049][variant],[16,18,17,20,21,16,22][variant],rng,[10,5,7,10,4,5,5][variant],lod==0)
 return m

specs=[(f'Tree{i+1}','tree',i,3) for i in range(7)]+[(f'Shrub{i+1}','shrub',i,2) for i in range(3)]+[(f'Grass{i+1}','grass',i,2) for i in range(3)]+[(f'Flowers{i+1}','flower',i,2) for i in range(7)]+[(f'Ornamental{i+1}','ornamental',i,2) for i in range(2)]
assets=[];report=[]
for name,kind,variant,lods in specs:
 objects=[]
 for lod in range(lods):
  mesh=tree(variant,lod) if kind=='tree' else shrub(variant,lod) if kind=='shrub' else groundplant(variant,lod,kind)
  obj=mesh.object(name+'_LOD'+str(lod));objects.append(obj);bpy.context.view_layer.update()
  report.append(dict(name=obj.name,vertices=len(obj.data.vertices),triangles=sum(len(p.vertices)-2 for p in obj.data.polygons),dimensions=list(obj.dimensions),materials=len(obj.data.materials)))
 bpy.ops.object.select_all(action='DESELECT')
 for obj in objects:obj.select_set(True)
 bpy.context.view_layer.objects.active=objects[0]
 bpy.ops.export_scene.fbx(filepath=str(ART/(name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
 assets.append((name,kind,objects));print('EXPORT',name,flush=True)
for i,(name,kind,objects) in enumerate(assets):
 base=Vector(((i%7)*5.2,(i//7)*-3.2,0))
 for j,obj in enumerate(objects):obj.location=base;obj.hide_render=j>0;obj.hide_viewport=j>0
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs[0].default_value=(.46,.47,.48,1);scene.world.node_tree.nodes.get('Background').inputs[1].default_value=.65;scene.view_settings.view_transform='AgX'
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.012));floor=bpy.context.object;floor.name='Preview ground (not exported)';floorMat=bpy.data.materials.new('Preview ground');floorMat.diffuse_color=(.25,.25,.22,1);floor.data.materials.append(floorMat)
target=Vector((15,-2,2.4));bpy.ops.object.camera_add(location=(28,-34,19));cam=bpy.context.object;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=39;scene.camera=cam
bpy.ops.object.light_add(type='AREA',location=(6,-9,18));light=bpy.context.object;light.data.energy=3200;light.data.shape='DISK';light.data.size=14;light.rotation_euler=(target-light.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=2000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100;scene.render.filepath=str(OUT/'kit-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'GreeneryKit.blend'),compress=True);bpy.ops.render.render(write_still=True)
(OUT/'manifest.json').write_text(json.dumps({'source':'Original deterministic geometry and painted texture atlas; user photographs only guide planting design.','version':2,'seed':1931,'families':['Linden','Birch','Oak','Maple','Willow','Orchard apple','Pine'],'assets':report},indent=2));print('GREENERY_KIT_PASS',len(assets),len(report),flush=True)
