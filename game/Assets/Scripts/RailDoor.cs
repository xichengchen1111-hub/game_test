using UnityEngine;
// Each real leaf can be opened and closed from either side while the train is parked.
public sealed class RailDoor : Interactable {
    public RailTrain train;
    public BoxCollider leafCollider;
    public override string ActionText=>train.Departing?"列车正在发车":train.DoorOpen(transform)?"关闭车门":"打开车门";
    public override void Use(){train.ToggleDoor(transform);}
    public bool OccupiedBy(CharacterController body) {
        if(!body||!leafCollider)return false;
        var centre=leafCollider.transform.TransformPoint(leafCollider.center);
        var lower=body.transform.TransformPoint(body.center-Vector3.up*(body.height*.5f-body.radius));
        var upper=body.transform.TransformPoint(body.center+Vector3.up*(body.height*.5f-body.radius));
        var segment=upper-lower;
        var nearest=lower+segment*Mathf.Clamp01(Vector3.Dot(centre-lower,segment)/segment.sqrMagnitude);
        var point=Quaternion.Inverse(leafCollider.transform.rotation)*(nearest-centre);
        var scale=leafCollider.transform.lossyScale;
        var half=Vector3.Scale(leafCollider.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)))*.5f;
        var closest=new Vector3(Mathf.Clamp(point.x,-half.x,half.x),Mathf.Clamp(point.y,-half.y,half.y),Mathf.Clamp(point.z,-half.z,half.z));
        float radius=body.radius*Mathf.Abs(body.transform.lossyScale.x)+body.skinWidth+.015f;
        // Leaves stay vertical and only rotate around world Y, so this is the exact
        // capsule/OBB distance, including skin clearance. Use the realised shapes
        // directly rather than depending on a penetration query for the controller.
        return (point-closest).sqrMagnitude<radius*radius;
    }
}
