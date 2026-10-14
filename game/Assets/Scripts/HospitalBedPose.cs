using UnityEngine;

// A person in a hospital bed or on a bench, made from a standing character's own idle and
// talk clips. After the Animator has posed the body each frame, the legs are folded away
// (under the covers) or bent to sit, the arms are brought to rest, and the head turns to a
// visitor who stands near. Rotations are applied in the body's frame (up runs from hips to
// head, the chest faces -Z), so they do not depend on how a particular rig orients its
// bones. Explicit pause freezes it with the rest of the cast.
public sealed class HospitalBedPose : MonoBehaviour {
    public enum Mode { SittingUpInBed, LyingInBed, SittingOnSeat }
    public Mode mode;
    public Transform frame;
    // When set (the cinematic), the head turns to this point instead of the player's eye.
    public Transform lookTarget;
    // Speaking without gestures: the head nods a little with the words instead.
    public PeriodCharacterPerformance performance;
    // The idle sways the hips; in a bed or on a bench that reads as the body sliding over the mattress
    // (user, 2026-10-14), so the hips keep the place they were authored at and only the turns remain.
    public Transform hips;public Vector3 hipsRest;
    public Transform leftLeg,rightLeg,leftShin,rightShin,leftUpperArm,rightUpperArm,leftLowerArm,rightLowerArm,neck,head;
    // armsIn brings the idle's slightly spread arms to the body (about the frame's forward axis) before any other bend.
    public float shoulderForward=16,shoulderIn=8,elbowBend=76,armsIn=10,watchDistance=5.5f,maxYaw=62,maxPitchUp=22,maxPitchDown=14;
    // How fast the head follows a visitor, how far it rests tilted on the pillow (degrees, about the
    // front-back axis) and how much it nods while speaking (1 = about two degrees).
    public float turnRate=3.2f,headTilt,talkNod=1;
    // Chin down (degrees) for someone half lying: the reclined body would otherwise face the ceiling.
    public float headForward;
    float yaw,pitch,talk,talkClock;
    void LateUpdate()=>Apply();
    // Public so the editor pose review (HospitalChildReview) can pose a sampled frame.
    public void Apply() {
        if(!frame)return;
        if(hips)hips.localPosition=hipsRest;
        var side=frame.right;
        // The chest faces -Z, so the left arm is on +X: a negative turn about forward lowers it toward the body.
        Bend(leftUpperArm,frame.forward,-armsIn);Bend(rightUpperArm,frame.forward,armsIn);
        if(mode==Mode.SittingOnSeat) {
            // Thighs forward, shins down again: the hips stay where the seat puts them.
            Bend(leftLeg,side,88);Bend(rightLeg,side,88);Bend(leftShin,side,-86);Bend(rightShin,side,-86);
            Bend(leftUpperArm,side,shoulderForward);Bend(rightUpperArm,side,shoulderForward);Bend(leftLowerArm,side,elbowBend);Bend(rightLowerArm,side,elbowBend);
        } else {
            // Hidden by the covers: without this the standing legs would hang through the mattress.
            // Lying, the arms stay along the sides under the covers (armsIn only).
            if(leftLeg)leftLeg.localScale=Vector3.one*.001f;if(rightLeg)rightLeg.localScale=Vector3.one*.001f;
            if(mode==Mode.SittingUpInBed) {
                Bend(leftUpperArm,side,shoulderForward);Bend(rightUpperArm,side,shoulderForward);
                // The chest faces -Z, so the left arm is on +X: a positive turn about up brings it inward.
                Bend(leftUpperArm,frame.up,shoulderIn);Bend(rightUpperArm,frame.up,-shoulderIn);
                Bend(leftLowerArm,side,elbowBend);Bend(rightLowerArm,side,elbowBend);
            }
        }
        // A positive turn about the side axis lifts the face (it carries the hanging arm forward), so chin down is negative.
        if(headForward!=0){Bend(neck,side,-headForward*.4f);Bend(head,side,-headForward*.6f);}
        float targetYaw=0,targetPitch=0;
        var app=DemoApp.Instance;
        if(app&&app.player&&head) {
            var to=frame.InverseTransformPoint(lookTarget?lookTarget.position:app.player.eye.transform.position)-frame.InverseTransformPoint(head.position);
            // Only a visitor in front of or beside the person is looked at.
            if(to.magnitude<watchDistance&&to.z<.6f) {
                targetYaw=Mathf.Clamp(Mathf.Atan2(-to.x,-to.z)*Mathf.Rad2Deg,-maxYaw,maxYaw);
                targetPitch=Mathf.Clamp(Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg,-maxPitchDown,maxPitchUp);
            }
        }
        if(!(app&&app.NPCAnimationPaused)) {
            float k=1-Mathf.Exp(-Time.deltaTime*turnRate);
            yaw=Mathf.Lerp(yaw,targetYaw,k);pitch=Mathf.Lerp(pitch,targetPitch,k);
            talk=Mathf.MoveTowards(talk,performance&&performance.IsSpeaking?1:0,Time.deltaTime*2.5f);talkClock+=Time.deltaTime;
        }
        // While speaking: a slow, small nod and sway (about two degrees), never a gesture.
        float nod=talkNod*talk*(1.8f*Mathf.Sin(talkClock*2.3f)+.8f*Mathf.Sin(talkClock*5.1f+1.3f)),sway=talkNod*talk*1.2f*Mathf.Sin(talkClock*1.1f);
        // The neck carries a third of the turn, the head the rest.
        Turn(neck,(yaw+sway)*.34f,(pitch+nod)*.34f);Turn(head,(yaw+sway)*.66f,(pitch+nod)*.66f);
        if(head&&headTilt!=0)head.rotation=Quaternion.AngleAxis(headTilt,frame.forward)*head.rotation;
    }
    static void Bend(Transform bone,Vector3 axis,float degrees) {
        if(bone)bone.rotation=Quaternion.AngleAxis(degrees,axis)*bone.rotation;
    }
    void Turn(Transform bone,float y,float p) {
        if(!bone)return;
        // Positive yaw turns the face (-Z) toward -X: the same sense as Atan2(-x,-z) above.
        bone.rotation=Quaternion.AngleAxis(y,frame.up)*Quaternion.AngleAxis(p,frame.right)*bone.rotation;
    }
}
