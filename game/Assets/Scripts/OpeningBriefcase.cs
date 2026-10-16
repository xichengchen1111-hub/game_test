using UnityEngine;

// The player's own briefcase in the property room. It stands in the wire-mesh cage until the guard
// unlocks the cage, takes it out by the handle and sets it on the table; E takes it back, with the
// watch, map, ticket and notebook inside. While it is carried it hangs from the guard's palm by the
// handle and swings a little with the hand (user, 2026-10-15: it used to slide out by itself).
public sealed class OpeningBriefcase : Interactable {
    // Case-local: the top of the handle grip, and half the body's height (it stands on its base).
    static readonly Vector3 Grip=new Vector3(0,.2f,0);
    const float HalfHeight=.15f;
    Vector3 swing,swingVelocity,lastGrip;bool hanging;
    public override string ActionText=>"拿起 · "+title;
    public override void Use(){DemoApp.Instance.CollectBelongings(this);}
    public Vector3 HandlePoint=>transform.TransformPoint(Grip);
    // Standing upright on a surface, its front (the flap) facing the given way.
    public static void StandingPose(Vector3 surface,Vector3 front,out Vector3 position,out Quaternion rotation) {
        front.y=0;rotation=Quaternion.LookRotation(front.sqrMagnitude>1e-6f?front.normalized:Vector3.forward,Vector3.up);
        position=surface+Vector3.up*HalfHeight;
    }
    public void Stand(Vector3 surface,Vector3 front){StandingPose(surface,front,out var p,out var r);transform.SetPositionAndRotation(p,r);hanging=false;}
    // Held by the handle: the grip in the palm, the case hanging below it, its broad sides facing
    // `side`; it lags a little behind the hand's movement and settles back under it.
    public void Hang(Vector3 palm,Vector3 side,float dt) {
        if(!hanging){hanging=true;lastGrip=palm;swing=Vector3.zero;swingVelocity=Vector3.zero;}
        if(dt>0) {
            var velocity=(palm-lastGrip)/dt;lastGrip=palm;
            // A damped pendulum driven by the hand's horizontal acceleration.
            var push=-new Vector3(velocity.x,0,velocity.z)*.06f;
            swingVelocity+=((push-swing)*38f-swingVelocity*7f)*dt;swing+=swingVelocity*dt;
            swing=Vector3.ClampMagnitude(swing,.35f);
        }
        side.y=0;var down=(Vector3.down+swing).normalized;
        var face=Vector3.ProjectOnPlane(side.sqrMagnitude>1e-6f?side.normalized:Vector3.forward,down).normalized;
        var rotation=Quaternion.LookRotation(face,-down);
        transform.SetPositionAndRotation(palm-rotation*Grip,rotation);
    }
    // Between being held and standing: the handle stays in the palm while the case turns upright.
    public void Blend(Vector3 palm,Quaternion from,Quaternion to,float k) {
        var rotation=Quaternion.Slerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(k)));
        transform.SetPositionAndRotation(palm-rotation*Grip,rotation);hanging=false;
    }
}
