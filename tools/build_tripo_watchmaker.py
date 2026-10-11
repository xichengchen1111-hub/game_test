"""Author a Unity-named rig and three fitted seated repair actions for the Tripo mesh.
Original downloaded model/textures are retained. Blender 4.5 LTS command-line tool.
"""
import bpy, math, json, sys
from pathlib import Path
from mathutils import Vector, Quaternion, Matrix
R=Path(__file__).resolve().parents[1]; S=R/'assets/source/watchmaker/tripo'; A=R/'game/Assets/Art/WatchmakerTripo'; W=R/'builds/macos-current/watchmaker-work'
A.mkdir(exist_ok=True); W.mkdir(exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.context.preferences.filepaths.save_version=0
bpy.ops.import_scene.fbx(filepath=str(next((S/'original').glob('*.fbx'))))
mesh=next(o for o in bpy.data.objects if o.type=='MESH');mesh.name='WatchmakerBody';bpy.context.view_layer.objects.active=mesh;mesh.select_set(True);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
zmin=min(v.co.z for v in mesh.data.vertices);scale=1.72/(max(v.co.z for v in mesh.data.vertices)-zmin)
for v in mesh.data.vertices:v.co=(v.co-Vector((0,0,zmin)))*scale
mesh.data.materials[0].name='WatchmakerBody'
for image in bpy.data.images:
 if image.source=='FILE': image.pack()
rest=[v.co.copy() for v in mesh.data.vertices]
# Anatomical rig follows the model's measured elbow, wrist, knee and ankle positions.
bpy.ops.object.select_all(action='DESELECT');data=bpy.data.armatures.new('WatchmakerUnitySkeleton');arm=bpy.data.objects.new('WatchmakerRig',data);bpy.context.collection.objects.link(arm);bpy.context.view_layer.objects.active=arm;arm.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(n,h,t,parent=None):
 b=data.edit_bones.new(n);b.head=h;b.tail=t
 if parent:b.parent=data.edit_bones[parent]
 return b
bone('Hips',(0,.055,.93),(0,.045,1.0));bone('Spine',(0,.045,1.0),(0,.045,1.14),'Hips');bone('Chest',(0,.045,1.14),(0,.025,1.28),'Spine');bone('UpperChest',(0,.025,1.28),(0,.015,1.34),'Chest');bone('Neck',(0,.015,1.34),(0,.015,1.42),'UpperChest');bone('Head',(0,.015,1.42),(0,.015,1.66),'Neck');bone('headfront',(0,-.105,1.42),(0,-.18,1.42),'Head')
for side,sg in [('Left',1),('Right',-1)]:
 def B(n,h,t,p):return bone(side+'_'+n,(sg*h[0],h[1],h[2]),(sg*t[0],t[1],t[2]),side+'_'+p if p not in ['Hips','UpperChest'] else p)
 B('Shoulder',(.04,.025,1.30),(.195,.015,1.30),'UpperChest');B('UpperArm',(.195,.015,1.30),(.40,.010,1.29),'Shoulder');B('LowerArm',(.40,.010,1.29),(.565,-.022,1.307),'UpperArm');B('Hand',(.565,-.022,1.307),(.629,-.027,1.307),'LowerArm')
 for name,y,length in [('Index',-.073,.077),('Middle',-.049,.087),('Ring',-.021,.087),('Little',.009,.064)]:
  starts=[.631,.631+length*.43,.631+length*.74,.631+length]
  for i,label in enumerate(['Proximal','Intermediate','Distal']):B(name+label,(starts[i],y,1.305),(starts[i+1],y,1.305),'Hand' if i==0 else name+['Proximal','Intermediate'][i-1])
 pts=[(.571,-.046,1.302),(.590,-.084,1.292),(.611,-.108,1.287),(.621,-.120,1.286)]
 for i,label in enumerate(['Proximal','Intermediate','Distal']):B('Thumb'+label,pts[i],pts[i+1],'Hand' if i==0 else 'Thumb'+['Proximal','Intermediate'][i-1])
 B('UpperLeg',(.09,.055,.93),(.12,.055,.49),'Hips');B('LowerLeg',(.12,.055,.49),(.12,.075,.12),'UpperLeg');B('Foot',(.12,.075,.12),(.12,-.08,.045),'LowerLeg');B('Toes',(.12,-.08,.045),(.12,-.14,.045),'Foot')
bone('Apron',(0,-.11,.98),(0,-.11,.61),'Hips')
bpy.ops.object.mode_set(mode='OBJECT')
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'
vg={b.name:mesh.vertex_groups.new(name=b.name) for b in data.bones if b.name!='headfront'}
def weights(i,pairs):
 total=sum(w for _,w in pairs)
 for n,w in pairs:
  if w>1e-6:vg[n].add([i],w/total,'REPLACE')
def blend(i,a,b,t):
 t=max(0,min(1,t));weights(i,[(a,1-t),(b,t)])
def segdist(v,b):
 d=b.tail_local-b.head_local;t=max(0,min(1,(v-b.head_local).dot(d)/d.length_squared));return (v-(b.head_local+d*t)).length
apron=[];backcloth=[];bodycount=len(rest);leftWatchOffset=None;rightTipOffset=None
for i,v in enumerate(rest):
 x,y,z=v;side='Left' if x>0 else 'Right';ax=abs(x)
 if z>1.415:weights(i,[('Head',1)])
 elif z>1.335 and ax<.16:blend(i,'Neck','Head',(z-1.355)/.075)
 elif ax>.205 and z>1.16:
  if ax>.61 or y<-.065:
   candidates=[b for b in data.bones if b.name.startswith(side+'_') and any(k in b.name for k in ['Proximal','Intermediate','Distal','Hand'])]
   candidates=sorted(candidates,key=lambda b:segdist(v,b))[:2];pairs=[(b.name,1/max(.002,segdist(v,b))**4) for b in candidates];weights(i,pairs)
  elif ax>.545:blend(i,side+'_LowerArm',side+'_Hand',(ax-.545)/.054)
  elif ax>.365:blend(i,side+'_UpperArm',side+'_LowerArm',(ax-.365)/.07)
  else:blend(i,side+'_Shoulder',side+'_UpperArm',(ax-.21)/.055)
 elif z>1.25:blend(i,'Chest','UpperChest',(z-1.25)/.1)
 elif z>1.06:blend(i,'Spine','Chest',(z-1.08)/.16)
 elif z>.97:blend(i,'Hips','Spine',(z-.97)/.1)
 elif z>.575:
  apronWeight=max(0,min(1,(-.025-y)/.065))*max(0,min(1,(1.0-z)/.12))
  hipWeight=max(0,min(1,(z-.82)/.15))
  weights(i,[('Apron',apronWeight),('Hips',(1-apronWeight)*hipWeight),(side+'_UpperLeg',(1-apronWeight)*(1-hipWeight))])
  if apronWeight>.5:apron.append(i)
 elif z>.53:weights(i,[(side+'_UpperLeg',1)])
 elif z>.43:blend(i,side+'_LowerLeg',side+'_UpperLeg',(z-.43)/.1)
 elif z>.15:weights(i,[(side+'_LowerLeg',1)])
 elif z>.075:blend(i,side+'_Foot',side+'_LowerLeg',(z-.075)/.08)
 else:weights(i,[(side+'_Foot',1)])
mod=mesh.modifiers.new('Unity skeleton','ARMATURE');mod.object=arm;mesh.parent=arm
s=bpy.context.scene;s.render.fps=30;s.frame_start=1;s.frame_end=241

def orient(pb,direction):
 m=pb.matrix.copy();q=m.to_3x3().col[1].normalized().rotation_difference(direction.normalized());out=(q.to_matrix()@m.to_3x3()).to_4x4();out.translation=m.translation;pb.matrix=out;bpy.context.view_layer.update()
def absolute_rotation(pb,rot):
 m=rot.to_4x4();m.translation=pb.head.copy();pb.matrix=m;bpy.context.view_layer.update()
def ik(upper,lower,end,target,pole):
 up=arm.pose.bones[upper];lo=arm.pose.bones[lower];tip=arm.pose.bones[end];h=up.head.copy();l1=(lo.bone.head_local-up.bone.head_local).length;l2=(tip.bone.head_local-lo.bone.head_local).length
 d=Vector(target)-h;dist=min(d.length,l1+l2-.0005);u=d.normalized();a=(l1*l1-l2*l2+dist*dist)/(2*dist);v=Vector(pole)-h;v=(v-u*v.dot(u)).normalized();elbow=h+u*a+v*math.sqrt(max(0,l1*l1-a*a));orient(up,elbow-h);orient(lo,Vector(target)-lo.head)
def base_pose(t=0,variant=0):
 for pb in arm.pose.bones:pb.matrix_basis=Matrix.Identity(4)
 hips=arm.pose.bones['Hips'];m=hips.matrix.copy();m.translation=Vector((0,.02,.535));hips.matrix=m;bpy.context.view_layer.update()
 arm.pose.bones['Spine'].rotation_quaternion=Quaternion((1,0,0),math.radians(10));arm.pose.bones['Chest'].rotation_quaternion=Quaternion((1,0,0),math.radians(3));arm.pose.bones['Neck'].rotation_quaternion=Quaternion((1,0,0),math.radians(17));arm.pose.bones['Head'].rotation_quaternion=Quaternion((1,0,0),math.radians(12))
 arm.pose.bones['Apron'].rotation_quaternion=Quaternion((1,0,0),math.radians(-84));bpy.context.view_layer.update()
 apronMatrix=arm.pose.bones['Apron'].matrix.copy();apronMatrix.translation.z+=.04;arm.pose.bones['Apron'].matrix=apronMatrix;bpy.context.view_layer.update()
 for side,sg in [('Left',1),('Right',-1)]:
  ik(side+'_UpperLeg',side+'_LowerLeg',side+'_Foot',(sg*.15,-.40,.12),(sg*.16,-.70,.48));absolute_rotation(arm.pose.bones[side+'_Foot'],data.bones[side+'_Foot'].matrix_local.to_3x3())
 # Shared beginning/end pose: support the watch over the rear edge of the bench.
 envelope=math.sin(math.pi*t)**2;wave=math.sin(t*math.tau*3)*envelope
 lift=(.052*envelope if variant==1 else 0)
 arm.pose.bones['Head'].rotation_quaternion=Quaternion((1,0,0),math.radians(12-(3*envelope if variant==1 else .7*wave)));bpy.context.view_layer.update()
 left=Vector((.075,-.43+(.015*envelope if variant==1 else 0),.885+lift));right=Vector((-.10,-.445,.955+lift))
 if variant==2:right+=Vector((.015*wave,.010*wave,.010*envelope))
 if variant==0:right+=Vector((.0015*wave,0,.002*wave))
 for side,sg,target,pole in [('Left',1,left,(.31,-.27,.91)),('Right',-1,right,(-.32,-.26,.90))]:
  ik(side+'_UpperArm',side+'_LowerArm',side+'_Hand',target,pole)
  direction=Vector((-sg*.78,-.57,-.12 if side=='Left' else -.28)).normalized()
  b=arm.pose.bones[side+'_Hand'];restrot=b.bone.matrix_local.to_3x3();q=restrot.col[1].normalized().rotation_difference(direction);rot=q.to_matrix()@restrot
  # Rest palm points down. Left palm supports the watch; right pinches the screwdriver.
  palm=q@Vector((0,0,-1));desired=Vector((0,0,1 if side=='Left' else -1));desired=(desired-direction*desired.dot(direction)).normalized();palm=(palm-direction*palm.dot(direction)).normalized();angle=math.atan2(direction.dot(palm.cross(desired)),palm.dot(desired))
  if side=='Right' and variant==0:angle+=math.radians(15)*wave
  rot=Quaternion(direction,angle).to_matrix()@rot;absolute_rotation(b,rot)
  for f in ['Thumb','Index','Middle','Ring','Little']:
   for j,suffix in enumerate(['Proximal','Intermediate','Distal']):
    pb=arm.pose.bones[side+'_'+f+suffix];d=pb.bone.matrix_local.to_3x3().col[1];axis=pb.bone.matrix_local.to_3x3().inverted()@(d.cross(Vector((0,0,-1))));axis.normalize()
    curl=([16,20,10] if side=='Left' else ([48,60,25] if f=='Index' else [55,70,25]))[j]
    if f=='Thumb':curl=[15,12,5][j]
    pb.rotation_quaternion=Quaternion(axis,math.radians(curl))
 bpy.context.view_layer.update()
 # Pinch the screwdriver between index and thumb; keep the other fingers curled.
 index=arm.pose.bones['Right_IndexDistal'];target=index.head+Vector((0,-.004,0));ik('Right_ThumbProximal','Right_ThumbIntermediate','Right_ThumbDistal',target,arm.pose.bones['Right_ThumbProximal'].head+Vector((0,-.08,0)))
 if leftWatchOffset is not None:
  desired=arm.pose.bones['Left_Hand'].matrix@leftWatchOffset+Vector((0,0,.015))
  if variant==2:desired+=Vector((.006*wave,.004*wave,.004*envelope))
  hand=arm.pose.bones['Right_Hand'];rot=hand.matrix.to_3x3().copy();target=desired-rot@rightTipOffset
  ik('Right_UpperArm','Right_LowerArm','Right_Hand',target,(-.32,-.26,.90));absolute_rotation(hand,rot)
 bpy.context.view_layer.update()
base_pose()
# Correct the concealed trouser surface where the seated thigh meets the apron.
# Keep the outer apron and its UVs intact instead of hiding visible clipping with lighting.
from mathutils.bvhtree import BVHTree
bpy.context.view_layer.update();evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());posedMesh=evaluated.to_mesh();posed=[v.co.copy() for v in posedMesh.vertices]
def apron_weight(i):return sum(g.weight for g in mesh.data.vertices[i].groups if mesh.vertex_groups[g.group].name=='Apron')
apronFaces=[list(f.vertices) for f in mesh.data.polygons if min(apron_weight(i) for i in f.vertices)>.90]
bvh=BVHTree.FromPolygons(posed,apronFaces);corrected=0
for i,original in enumerate(rest):
 if not (.575<original.z<.89 and apron_weight(i)<.15):continue
 point=posed[i];hit,normal,index,distance=bvh.ray_cast(Vector((point.x,point.y,1.3)),Vector((0,0,-1)),1.2)
 if hit is not None and .48<hit.z<.78 and point.z>hit.z-.012:
  skin=Matrix(((0,0,0,0),)*4)
  for g in mesh.data.vertices[i].groups:
   name=mesh.vertex_groups[g.group].name;transform=arm.pose.bones[name].matrix@data.bones[name].matrix_local.inverted()
   for row in range(4):
    for col in range(4):skin[row][col]+=transform[row][col]*g.weight
  point.z=hit.z-.012;mesh.data.vertices[i].co=skin.inverted()@point;corrected+=1
evaluated.to_mesh_clear();print('SEATED_TROUSER_CLEARANCE',corrected,flush=True)
# Tools are skinned to hands in the exact fitted pose, with all detail meshes retained editable.
def material(name,color,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Metallic'].default_value=metal;p.inputs['Roughness'].default_value=.4;return m
brass=material('RepairBrass',(.40,.25,.09),.72);steel=material('RepairSteel',(.35,.39,.40),.8);wood=material('RepairWood',(.055,.036,.018))
props=[]
def cylinder(name,center,radius,depth,mat,bind,axis=(0,0,1),vertices=24):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=center);o=bpy.context.object;o.name=name;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();o.data.materials.append(mat);bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 inv=(arm.pose.bones[bind].matrix@data.bones[bind].matrix_local.inverted()).inverted()
 for v in o.data.vertices:v.co=inv@v.co
 vg2=o.vertex_groups.new(name=bind);vg2.add(list(range(len(o.data.vertices))),1,'REPLACE');mo=o.modifiers.new('Hand contact','ARMATURE');mo.object=arm;o.parent=arm;props.append(o);return o
# Hand support is palm-up; watch sits in the palm near the finger bases.
left=arm.pose.bones['Left_Hand'];watch=left.head+left.matrix.to_3x3().col[1]*.058+Vector((0,0,.012))
cylinder('Pocket watch case',watch,.031,.013,brass,'Left_Hand')
cylinder('Exposed watch movement',watch+Vector((0,0,.008)),.026,.003,steel,'Left_Hand')
for dx,dy,r in [(-.009,.005,.007),(.010,-.005,.009),(.005,.012,.004)]:cylinder('Movement wheel',watch+Vector((dx,dy,.011)),r,.003,brass,'Left_Hand',vertices=14)
# Tip is over the movement, grip lies between curled right fingers.
right=arm.pose.bones['Right_Hand'];grip=(arm.pose.bones['Right_IndexDistal'].head+arm.pose.bones['Right_ThumbDistal'].head)*.5;tip=watch+Vector((0,0,.015));axis=(grip-tip).normalized();shaftlen=(grip-tip).length
leftWatchOffset=left.matrix.inverted()@watch;rightTipOffset=right.matrix.inverted()@tip
cylinder('Watch screwdriver shaft',(tip+grip)*.5,.002,shaftlen,steel,'Right_Hand',axis,12);cylinder('Watch screwdriver handle',grip+axis*.018,.006,.043,wood,'Right_Hand',axis,12)
# Export explicit contact markers for native runtime verification.
bpy.context.view_layer.objects.active=arm;bpy.ops.object.mode_set(mode='EDIT')
for name,parent,point in [('RepairWatchContact','Left_Hand',watch+Vector((0,0,.015))),('RepairToolTip','Right_Hand',tip)]:
 transform=arm.pose.bones[parent].matrix@data.bones[parent].matrix_local.inverted();restpoint=transform.inverted()@point
 bone(name,restpoint,restpoint+Vector((0,0,.01)),parent)
bpy.ops.object.mode_set(mode='OBJECT')
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'
# One skinned mesh keeps material assignments and the common exported armature simple.
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True)
for o in props:o.select_set(True)
bpy.context.view_layer.objects.active=mesh;bpy.ops.object.join()
# Actual object transform must remain identity after the join.
actions=[];report={'source':'Tripo 373ab2a6-bf3d-43a4-9c4a-a20d63e69697','rigging':'Tripo automatic humanoid rejected model; locally authored Unity-named skeleton and tailored seated skinning','height':1.72,'bones':len(data.bones),'bodyVertices':bodycount,'sourcePolygons':10060,'seatHeight':.4,'clips':[],'contact':'Rigid hand-skinned pocket watch and screwdriver; no cloth hand/finger weights','originalPreserved':True}
for name,variant,frames in [('RepairScrew',0,241),('InspectMovement',1,271),('AdjustParts',2,241)]:
 ac=bpy.data.actions.new(name);ac.use_fake_user=True;arm.animation_data_create();arm.animation_data.action=ac;s.frame_end=frames
 for fr in range(1,frames+1):
  s.frame_set(fr);base_pose((fr-1)/(frames-1),variant)
  for pb in arm.pose.bones:
   pb.keyframe_insert('rotation_quaternion',frame=fr);pb.keyframe_insert('location',frame=fr)
 actions.append(ac);report['clips'].append({'name':name,'seconds':(frames-1)/30,'frames':frames})
# Retain editable source in the repair pose, with packed original textures.
arm.animation_data.action=actions[0];s.frame_set(1);s.frame_end=241
for im in bpy.data.images:
 if im.source=='FILE' and not im.packed_file:im.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(S/'Watchmaker.blend'))
# Export separate action strips instead of stacking them into one timeline.
arm.animation_data.action=None
for ac in actions:
 track=arm.animation_data.nla_tracks.new();strip=track.strips.new(ac.name,1,ac);strip.action_slot=ac.slots[0]
bpy.ops.object.select_all(action='DESELECT');mesh.select_set(True);arm.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(A/'Watchmaker.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,use_mesh_modifiers=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
arm.animation_data.action=actions[0];arm.animation_data.action_slot=actions[0].slots[0];s.frame_set(90)
# Neutral studio review only: actual shop views are captured from the native Unity player.
s.render.engine='CYCLES';s.cycles.samples=16;s.cycles.use_denoising=True;s.render.resolution_x=1000;s.render.resolution_y=850;s.render.resolution_percentage=100;s.view_settings.view_transform='AgX';s.world=bpy.data.worlds.new('Review world');s.world.use_nodes=True;s.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.6
for loc,power,size in [((-2,-3,4),450,4),((2,0,3),300,3)]:
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.data.energy=power;l.data.size=size;l.rotation_euler=(Vector((0,0,.8))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();cam=bpy.context.object;s.camera=cam;cam.data.type='ORTHO'
for name,loc,target,ortho in [('seated-front',(1.6,-3,1.65),(0,-.20,.68),1.65),('seated-side',(3,0,1.0),(0,-.2,.65),1.6),('hands',(1,-2,2),(0,-.58,.90),.65)]:
 cam.location=loc;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=ortho;s.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
(S/'authoring-results.json').write_text(json.dumps(report,indent=2)+'\n');print('WATCHMAKER_AUTHORED',json.dumps(report),flush=True)
