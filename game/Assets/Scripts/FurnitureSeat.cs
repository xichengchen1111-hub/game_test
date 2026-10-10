using UnityEngine;

// Resting seats have their own stable identity; the departure seat remains a RailSeat.
public sealed class FurnitureSeat : Interactable {
    public string seatId;
    public Transform sittingPoint,standingPoint;
    public float eyeHeight=1.15f;
    public override string ActionText=>title;
    public override void Use(){DemoApp.Instance.SitOnFurniture(this);}
    public bool FindStandingPosition(out Vector3 position) {
        Physics.SyncTransforms();
        position=standingPoint.position;
        var offsets=new[]{Vector3.zero,standingPoint.right*.42f,-standingPoint.right*.42f,standingPoint.forward*.35f};
        foreach(var offset in offsets) {
            var candidate=standingPoint.position+offset;
            if(!Physics.Raycast(candidate+Vector3.up*.45f,Vector3.down,out var floor,1.1f,~(1<<2),QueryTriggerInteraction.Ignore))continue;
            candidate.y=floor.point.y+.025f;
            if(Physics.CheckCapsule(candidate+Vector3.up*.29f,candidate+Vector3.up*1.53f,.265f,~(1<<2),QueryTriggerInteraction.Ignore))continue;
            position=candidate;return true;
        }
        return false;
    }
}
