"""Reopen the editable source and check both clips, planted feet and jaw isolation."""
import bpy,json,math
from pathlib import Path
R=Path(__file__).resolve().parents[1];S=R/'assets/source/station-clerk/tripo'
bpy.ops.wm.open_mainfile(filepath=str(S/'StationClerk.blend'))
scene=bpy.context.scene;arm=next(o for o in bpy.data.objects if o.type=='ARMATURE');body=next(o for o in bpy.data.objects if o.type=='MESH')
assert set(a.name for a in bpy.data.actions)=={'01_Idle','02_Talk_WithMouth'}
assert len(body.data.materials)==2
assert len([i for i in bpy.data.images if i.source=='FILE' and i.packed_file])==4
group=body.vertex_groups['Jaw'].index
assert all(v.co.z>.775 for v in body.data.vertices if any(g.group==group and g.weight>.01 for g in v.groups)),'Jaw deforms clothing'
report={'passed':False,'bones':len(arm.data.bones),'meshVertices':len(body.data.vertices),'rootScale':list(arm.parent.scale),'sourcePackedImages':4,'clips':[]}
foot_reference=None
for action in bpy.data.actions:
    arm.animation_data.action=action;arm.animation_data.action_slot=action.slots[0]
    maximum=0;minimum=10;feet_drift=0;jaw_angles=[]
    for frame in range(1,int(action.frame_range[1])+1,3):
        scene.frame_set(frame);bpy.context.view_layer.update()
        feet=[arm.matrix_world@arm.pose.bones[n].matrix.translation for n in ['Left_Foot','Right_Foot']]
        if foot_reference is None:foot_reference=[v.copy() for v in feet]
        feet_drift=max(feet_drift,*(float((v-ref).length) for v,ref in zip(feet,foot_reference)))
        evaluated=body.evaluated_get(bpy.context.evaluated_depsgraph_get());verts=evaluated.data.vertices
        soles=[(evaluated.matrix_world@verts[i].co).z for i,v in enumerate(body.data.vertices) if v.co.z<.04]
        minimum=min(minimum,min(soles));maximum=max(maximum,min(soles))
        jaw_angles.append(math.degrees(arm.pose.bones['Jaw'].rotation_quaternion.angle))
    assert feet_drift<1e-5,(action.name,feet_drift)
    assert abs(minimum)<.002 and abs(maximum)<.002,(action.name,minimum,maximum)
    if action.name=='01_Idle':assert max(jaw_angles)<.01
    else:assert max(jaw_angles)>5
    scene.frame_set(1);first=[p.matrix.copy() for p in arm.pose.bones]
    scene.frame_set(int(action.frame_range[1]));last=[p.matrix.copy() for p in arm.pose.bones]
    loop_error=max(abs(a[i][j]-b[i][j]) for a,b in zip(first,last) for i in range(4) for j in range(4))
    assert loop_error<1e-4,(action.name,loop_error)
    report['clips'].append({'name':action.name,'feetDriftMetres':feet_drift,'soleMinMetres':minimum,'soleMaxMetres':maximum,'jawMaxDegrees':max(jaw_angles),'loopMatrixError':loop_error})
report['passed']=True;(S/'source-verification.json').write_text(json.dumps(report,indent=2)+'\n');print('STATION_CLERK_SOURCE_PASS',json.dumps(report),flush=True)
