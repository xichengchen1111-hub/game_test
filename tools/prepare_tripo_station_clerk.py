"""Keep the Tripo skinning and motions, fit a planted stance, and add a speaking jaw."""
import bpy, bmesh, json, math
from pathlib import Path
from mathutils import Vector, Quaternion

R=Path(__file__).resolve().parents[1]
S=R/'assets/source/station-clerk/tripo'
A=R/'game/Assets/Art/StationClerkTripo'
W=R/'builds/macos-current/station-clerk-work'
A.mkdir(parents=True,exist_ok=True);W.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.import_scene.fbx(filepath=str(next((S/'original').glob('*.fbx'))))
scene=bpy.context.scene
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
body=next(o for o in bpy.data.objects if o.type=='MESH')
arm.name='StationClerkRig';body.name='ClerkBody';body.data.materials[0].name='ClerkBody'
original=list(bpy.data.actions)
idle=next(a for a in original if 'idle' in a.name.lower())
talk=next(a for a in original if a!=idle)
assert len(original)==2
arm.animation_data.action=None
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
for pb in arm.pose.bones:pb.matrix_basis.identity()
for pb in arm.pose.bones:pb.rotation_mode='QUATERNION'

# A small lip seam opens during speech; the rest of the original face stays intact.
bpy.context.view_layer.objects.active=arm;arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
jaw=arm.data.edit_bones.new('Jaw');jaw.head=(.001909,.0,.822);jaw.tail=(.001909,-.059,.803);jaw.parent=arm.data.edit_bones['Head']
front=arm.data.edit_bones.new('headfront');front.head=(.001909,-.058,.834431);front.tail=(.001909,-.11,.834431);front.parent=arm.data.edit_bones['Head'];front.use_deform=False
bpy.ops.object.mode_set(mode='OBJECT')
jawgroup=body.vertex_groups.new(name='Jaw')
mouth_z=.8160;mouth_x=.001909;halfwidth=.034
bm=bmesh.new();bm.from_mesh(body.data);deform=bm.verts.layers.deform.verify()
region=[f for f in bm.faces if all(abs(v.co.x-mouth_x)<.048 and -.09<v.co.y<-.044 and .796<v.co.z<.831 for v in f.verts)]
geometry=set(region)
for f in region:geometry.update(f.edges);geometry.update(f.verts)
cut=bmesh.ops.bisect_plane(bm,geom=list(geometry),dist=1e-6,plane_co=(0,0,mouth_z),plane_no=(0,0,1))
seam=[e for e in cut['geom_cut'] if isinstance(e,bmesh.types.BMEdge) and all(abs(v.co.x-mouth_x)<halfwidth and v.co.y<-.058 for v in e.verts)]
assert len(seam)>0,'No mouth seam at the measured lip line'
bmesh.ops.split_edges(bm,edges=seam)
def smooth(t):
    t=max(0,min(1,t));return t*t*(3-2*t)
jaw_vertices=0
for v in bm.verts:
    x,y,z=v.co;dx=abs(x-mouth_x)
    if dx>.065 or y>-.025 or z<.781 or z>mouth_z+1e-5:continue
    if abs(z-mouth_z)<1e-5 and any(f.calc_center_median().z>mouth_z+1e-5 for f in v.link_faces):continue
    width=smooth((.060-dx)/.023);lower=smooth((z-.781)/.012);forward=smooth((-.025-y)/.034)
    weight=.85*width*lower*forward
    if weight<.001:continue
    for k in list(v[deform].keys()):v[deform][k]*=(1-weight)
    v[deform][jawgroup.index]=weight;jaw_vertices+=1
bm.to_mesh(body.data);bm.free();body.data.update()
assert jaw_vertices>10
material=bpy.data.materials.new('MouthInterior');material.diffuse_color=(.04,.006,.008,1);material.use_nodes=True
material.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(.04,.006,.008,1)
material.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=8,radius=1,location=(mouth_x,-.008,.809))
cavity=bpy.context.object;cavity.name='Mouth cavity';cavity.scale=(.029,.043,.016)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
cavity.data.materials.append(material);cavity.parent=arm
headgroup=cavity.vertex_groups.new(name='Head');headgroup.add(list(range(len(cavity.data.vertices))),1,'REPLACE')
modifier=cavity.modifiers.new('Humanoid skin','ARMATURE');modifier.object=arm
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);cavity.select_set(True);bpy.context.view_layer.objects.active=body;bpy.ops.object.join()

# Both clips share exactly the same lower-body pose. Tripo upper-body motion is retained.
def source_pose(action,frame):
    arm.animation_data.action=action;arm.animation_data.action_slot=action.slots[0]
    scene.frame_set(frame);bpy.context.view_layer.update()
source_pose(idle,1)
lower_names=[b.name for b in arm.pose.bones if b.name=='Hips' or any(k in b.name for k in ['UpperLeg','LowerLeg','Foot','Toes'])]
fixed_lower={n:arm.pose.bones[n].matrix_basis.copy() for n in lower_names}
factor=1.78/(max(v.co.z for v in body.data.vertices)-min(v.co.z for v in body.data.vertices))
model_root=bpy.data.objects.new('StationClerkModelRoot',None);bpy.context.collection.objects.link(model_root)
arm.parent=model_root;model_root.scale=(factor,)*3
for name,matrix in fixed_lower.items():arm.pose.bones[name].matrix_basis=matrix
bpy.context.view_layer.update()
evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get())
lowest=min((evaluated.matrix_world@v.co).z for v in evaluated.data.vertices)
model_root.location.z-=lowest
jaw_axis=arm.data.bones['Jaw'].matrix_local.to_quaternion().inverted()@Vector((1,0,0))
actions=[];report={'tripoAsset':'7ef008b1-0a11-4338-a813-c65ba9636e73','heightRest':1.78,'sourceSkinningPreserved':True,'lowerBodyFixedToTripoIdleFrame1':True,'mouthSeamEdges':len(seam),'jawVertices':jaw_vertices,'clips':[]}
for source,name in [(idle,'01_Idle'),(talk,'02_Talk_WithMouth')]:
    frames=int(source.frame_range[1]);samples=[]
    source_pose(source,1)
    first={p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in arm.pose.bones}
    for frame in range(1,frames+1):
        source_pose(source,frame)
        for n,matrix in fixed_lower.items():arm.pose.bones[n].matrix_basis=matrix
        phase=(frame-1)/(frames-1)
        blend=smooth((frame-(frames-18))/18)
        if blend>0:
            for pb in arm.pose.bones:
                if pb.name in lower_names:continue
                loc,rot,scale=first[pb.name]
                pb.location=pb.location.lerp(loc,blend);pb.rotation_quaternion=pb.rotation_quaternion.slerp(rot,blend);pb.scale=pb.scale.lerp(scale,blend)
        angle=0 if source==idle else math.radians(7)*(.65*math.sin(phase*8*math.pi)**2+.35*math.sin(phase*14*math.pi)**2)
        arm.pose.bones['Jaw'].rotation_mode='QUATERNION';arm.pose.bones['Jaw'].rotation_quaternion=Quaternion(jaw_axis,angle)
        samples.append({p.name:(p.location.copy(),p.rotation_quaternion.copy(),p.scale.copy()) for p in arm.pose.bones})
    result=bpy.data.actions.new(name);result.use_fake_user=True;arm.animation_data.action=result
    for frame,sample in enumerate(samples,1):
        for pb in arm.pose.bones:
            pb.location,pb.rotation_quaternion,pb.scale=sample[pb.name]
            for prop in ['location','rotation_quaternion','scale']:pb.keyframe_insert(prop,frame=frame)
    actions.append(result);report['clips'].append({'name':name,'frames':frames,'seconds':(frames-1)/24,'origin':'Tripo preset idle' if source==idle else 'Tripo AI Clerk Talking to Passenger, with local jaw/lip opening'})
for old in original:bpy.data.actions.remove(old)
scene.render.fps=24;scene.frame_start=1;scene.frame_end=int(actions[0].frame_range[1])
arm.animation_data.action=actions[0];arm.animation_data.action_slot=actions[0].slots[0];scene.frame_set(1)
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file:image.pack()
bpy.ops.wm.save_as_mainfile(filepath=str(S/'StationClerk.blend'))
arm.animation_data.action=None
for action in actions:
    track=arm.animation_data.nla_tracks.new();strip=track.strips.new(action.name,1,action);strip.action_slot=action.slots[0]
bpy.ops.object.select_all(action='DESELECT');body.select_set(True);arm.select_set(True);model_root.select_set(True);bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=str(A/'StationClerk.fbx'),use_selection=True,object_types={'ARMATURE','MESH','EMPTY'},add_leaf_bones=False,use_mesh_modifiers=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',path_mode='STRIP')
for track in list(arm.animation_data.nla_tracks):arm.animation_data.nla_tracks.remove(track)
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=1100;scene.render.resolution_percentage=100
scene.world=bpy.data.worlds.new('Review world');scene.world.use_nodes=True;scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value=.7
for loc,power in [((-2,-3,4),350),((2,1,3),200)]:
    bpy.ops.object.light_add(type='AREA',location=loc);light=bpy.context.object;light.data.energy=power;light.data.size=4;light.rotation_euler=(Vector((0,0,1))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add();camera=bpy.context.object;scene.camera=camera;camera.data.type='ORTHO'
for name,action,frame,loc,target,size in [
    ('model-idle',actions[0],80,(2,-5,2),(0,0,.9),2.10),
    ('model-talk',actions[1],48,(2,-5,2),(0,0,.9),2.10),
    ('face-idle',actions[0],80,(0,-4,1.53),(0,0,1.53),.44),
    ('face-talk',actions[1],48,(0,-4,1.53),(0,0,1.53),.44),
    ('model-side',actions[1],48,(4,-.3,1.7),(0,0,.9),2.10)]:
    arm.animation_data.action=action;arm.animation_data.action_slot=action.slots[0];scene.frame_set(frame)
    camera.location=loc;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.ortho_scale=size
    scene.render.filepath=str(W/(name+'.png'));bpy.ops.render.render(write_still=True)
(S/'authoring-results.json').write_text(json.dumps(report,indent=2)+'\n');print('STATION_CLERK_AUTHORED',json.dumps(report),flush=True)
