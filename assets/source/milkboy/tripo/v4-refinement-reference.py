import bpy,pathlib,math,json,shutil
from mathutils import Vector,Quaternion
R=pathlib.Path('/Users/wulizhi/.codex/.chatgpt-projects/g-p-6ac33302cd888191873099003d5164f4/output/milkboy-unity-v4')
bpy.ops.wm.open_mainfile(filepath=str(R.parent/'milkboy-unity-v3/Milkboy_v3_Master.blend'))
s=bpy.context.scene;s.render.fps=24
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');mesh=next(o for o in bpy.data.objects if o.type=='MESH')
for im in bpy.data.images:
 source=pathlib.Path(bpy.path.abspath(im.filepath))
 if source.is_file():
  dst=R/'export/Milkboy_v4.fbm'/source.name;dst.parent.mkdir(exist_ok=True);shutil.copy2(source,dst);im.filepath=str(dst)
hold=next(ac for ac in bpy.data.actions if ac.name.startswith('02'));hold.name='02_Nervous_HoldShoulderStrap'
arm.animation_data.action=hold;arm.animation_data.action_slot=hold.slots[0]
ids=json.loads((R/'verification/strap-vertices.json').read_text())
# Real strap surface anchors, one at the upper chest and one below it.
anchors={side:[i for i in ids if abs(mesh.data.vertices[i].co.z-z)<.008 and abs(mesh.data.vertices[i].co.x-x)<.016 and not any(mesh.vertex_groups[g.group].name.endswith(('UpperArm','LowerArm','Hand')) and g.weight>0 for g in mesh.data.vertices[i].groups)] for side,x,z in [('Right',-.031,.680),('Left',.012,.600)]}
print('ANCHORS',anchors,flush=True)
rest=arm.data.bones['Hips'].matrix_local
report={'actions':[a.name for a in bpy.data.actions],'anchors':anchors,'contact_max_error':{},'anchor_weights':{side:[{mesh.vertex_groups[g.group].name:round(g.weight,5) for g in mesh.data.vertices[i].groups} for i in indices] for side,indices in anchors.items()}}
errors={'Left':[],'Right':[]}
def orient(pb,direction):
 old=pb.matrix.copy();q=old.to_3x3().col[1].normalized().rotation_difference(direction.normalized());mat=(q.to_matrix()@old.to_3x3()).to_4x4();mat.translation=old.translation;pb.matrix=mat;bpy.context.view_layer.update()
for fr in range(1,121):
 s.frame_set(fr);bpy.context.view_layer.update()
 ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());em=ev.to_mesh();centers={side:sum((em.vertices[i].co for i in indices),Vector())/len(indices) for side,indices in anchors.items()};ev.to_mesh_clear()
 torso=arm.pose.bones['Hips'].matrix@rest.inverted();rot=torso.to_3x3()
 for side,dirv,pole in [('Right',Vector((.78,0,.625)),Vector((-.17,-.085,.50))),('Left',Vector((-.78,0,.625)),Vector((.17,-.085,.46)))]:
  direction=dirv.normalized();target=centers[side]+rot@(-direction*.054+Vector((0,-.025,-.002)))
  up=arm.pose.bones[side+'_UpperArm'];lo=arm.pose.bones[side+'_LowerArm'];hand=arm.pose.bones[side+'_Hand'];shoulder=up.head.copy()
  L1=up.bone.length;L2=(hand.bone.head_local-lo.bone.head_local).length
  delta=target-shoulder;dist=delta.length;u=delta.normalized();c=(L1*L1-L2*L2+dist*dist)/(2*dist);c=max(-L1,min(L1,c));v=(pole-shoulder)-u*(pole-shoulder).dot(u);v.normalize();elbow=shoulder+u*c+v*math.sqrt(max(0,L1*L1-c*c))
  orient(up,elbow-shoulder);orient(lo,target-lo.head)
  desire=rot@direction;handrest=hand.bone.matrix_local.to_3x3();q=handrest.col[1].normalized().rotation_difference(desire.normalized());matrot=q.to_matrix()@handrest
  normal=q@Vector((0,0,-1));desiredn=rot@Vector((0,1,0));d=desire.normalized();desiredn=(desiredn-d*desiredn.dot(d)).normalized();normal=(normal-d*normal.dot(d)).normalized();angle=math.atan2(d.dot(normal.cross(desiredn)),normal.dot(desiredn));matrot=Quaternion(d,angle).to_matrix()@matrot
  hm=matrot.to_4x4();hm.translation=hand.head.copy();hand.matrix=hm;bpy.context.view_layer.update()
  for b in [up,lo,hand]:b.keyframe_insert('rotation_quaternion',frame=fr);b.keyframe_insert('location',frame=fr)
  for b in arm.pose.bones:
   if not b.name.startswith(side+'_') or not any(part in b.name for part in ['Proximal','Intermediate','Distal']):continue
   fd=b.bone.matrix_local.to_3x3().col[1].normalized();axis=b.bone.matrix_local.to_3x3().inverted()@(fd.cross(Vector((0,0,-1))));axis.normalize()
   curl=(35 if 'Proximal' in b.name else 45 if 'Intermediate' in b.name else 25) if 'Thumb' in b.name else (55 if 'Proximal' in b.name else 65 if 'Intermediate' in b.name else 30)
   b.rotation_mode='QUATERNION';b.rotation_quaternion=Quaternion(axis,math.radians(curl));b.keyframe_insert('rotation_quaternion',frame=fr)
  errors[side].append((hand.head-target).length)
report['contact_max_error']={k:max(v) for k,v in errors.items()}
handgroups={g.index for g in mesh.vertex_groups if any(t in g.name for t in ['Hand','Thumb','Index','Middle','Ring','Pinky'])};report['strap_max_hand_finger_weight']=max(sum(g.weight for g in mesh.data.vertices[i].groups if g.group in handgroups) for i in ids)
(R/'verification/strap-refinement.json').write_text(json.dumps(report,indent=2))
s.frame_set(1);bpy.ops.wm.save_as_mainfile(filepath=str(R/'Milkboy_v4_Master.blend'))
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True);bpy.context.view_layer.objects.active=arm;arm.animation_data.action=None
for ac in bpy.data.actions:
 track=arm.animation_data.nla_tracks.new();strip=track.strips.new(ac.name,1,ac);strip.action_slot=ac.slots[0]
bpy.ops.export_scene.fbx(filepath=str(R/'export/Milkboy_v4.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},use_mesh_modifiers=False,add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=False)
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
arm.animation_data.action=hold;arm.animation_data.action_slot=hold.slots[0];s.frame_set(55)
s.render.engine='CYCLES';s.cycles.samples=12;s.cycles.use_denoising=True;s.render.use_persistent_data=True;s.render.resolution_x=960;s.render.resolution_y=720;s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('World');s.world.color=(.08,.08,.08);s.view_settings.view_transform='Standard'
for m in bpy.data.materials:
 if m.name.startswith('Mouth'):continue
 if m.use_nodes:
  p=m.node_tree.nodes.get('Principled BSDF')
  if p:
   p.inputs['Emission Strength'].default_value=.22
   if p.inputs['Base Color'].is_linked:m.node_tree.links.new(p.inputs['Base Color'].links[0].from_socket,p.inputs['Emission Color'])
target=Vector((0,0,.635));bpy.ops.object.camera_add(location=(0,-3,.68));cam=bpy.context.object;cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=.79;s.camera=cam
bpy.ops.object.light_add(type='AREA',location=(-1,-2,2));li=bpy.context.object;li.data.energy=120;li.data.size=3;li.rotation_euler=(Vector((0,0,.65))-li.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath=str(R/'verification/strap-preview.png');bpy.ops.render.render(write_still=True)
print('STRAP_DONE',json.dumps(report),flush=True)
