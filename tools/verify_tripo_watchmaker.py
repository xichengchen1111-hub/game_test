import bpy,json,math
from pathlib import Path
from mathutils import Vector
r=Path(__file__).resolve().parents[1];src=r/'assets/source/watchmaker/tripo';bpy.ops.wm.open_mainfile(filepath=str(src/'Watchmaker.blend'));a=next(o for o in bpy.data.objects if o.type=='ARMATURE');m=next(o for o in bpy.data.objects if o.type=='MESH');s=bpy.context.scene
print('MATERIALS',[x.name for x in m.data.materials],flush=True)
bodycount=9505;low=[v.index for v in m.data.vertices[:bodycount] if v.co.z<.025];cloth=[v.index for v in m.data.vertices[:bodycount] if .6<v.co.z<1.05 and abs(v.co.x)<.24];groups={g.index:g.name for g in m.vertex_groups};maxleak=max((sum(g.weight for g in m.data.vertices[i].groups if any(k in groups[g.group] for k in ['Hand','Thumb','Index','Middle','Ring','Little'])) for i in cloth),default=0)
report={'reopened':True,'blender':bpy.app.version_string,'materials':[x.name for x in m.data.materials],'packedImages':sum(bool(x.packed_file) for x in bpy.data.images),'clothHandWeightMax':maxleak,'actions':[]};assert maxleak==0
for ac in bpy.data.actions:
 a.animation_data.action=ac;a.animation_data.action_slot=ac.slots[0];s.frame_set(1);hip0=a.pose.bones['Hips'].head.copy();root0=a.location.copy();maxcontact=0;maxhip=0;minsole=10;maxsole=-10;headDown=1;samples=0;seatMin=10;seatMax=-10
 for fr in range(1,int(ac.frame_range[1])+1,3):
  s.frame_set(fr);bpy.context.view_layer.update();maxhip=max(maxhip,(a.pose.bones['Hips'].head-hip0).length);maxcontact=max(maxcontact,(a.pose.bones['RepairWatchContact'].head-a.pose.bones['RepairToolTip'].head).length);headDown=min(headDown,-(a.pose.bones['Head'].matrix.to_3x3()@a.data.bones['Head'].matrix_local.to_3x3().inverted()@Vector((0,-1,0))).z)
  ev=m.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();minsole=min(minsole,min(me.vertices[i].co.z for i in low));maxsole=max(maxsole,max(me.vertices[i].co.z for i in low));support=min(v.co.z for v in me.vertices[:bodycount] if abs(v.co.x)<.19 and -.22<v.co.y<.20 and .3<v.co.z<.65);seatMin=min(seatMin,support);seatMax=max(seatMax,support);ev.to_mesh_clear();samples+=1
 print(ac.name,'contact',maxcontact,'hips',maxhip,'soles',minsole,maxsole,'headDown',headDown,flush=True)
 assert maxhip<.001 and minsole>-.015 and minsole<.015 and maxcontact<.012 and headDown>.12 and .385<seatMin<=seatMax<.415
 report['actions'].append({'name':ac.name,'samples':samples,'maxToolWatchDistance':maxcontact,'maxHipTravel':maxhip,'soleMin':minsole,'soleMax':maxsole,'headDownMinimum':headDown,'supportSurfaceMin':seatMin,'supportSurfaceMax':seatMax})
(src/'source-verification.json').write_text(json.dumps(report,indent=2));print('WATCHMAKER_SOURCE_PASS',flush=True)
