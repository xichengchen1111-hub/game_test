using UnityEngine;

// A single physical building in Town. Interior, door and facade share this root.
public class ClockTowerInstance : MonoBehaviour {
    public const float OuterWidth=ClockTowerInteriorLayout.ExteriorWidth, InnerWidth=ClockTowerInteriorLayout.InteriorWidth, Height=ClockTowerInteriorLayout.ExteriorHeight;
    public const float DoorWidth=ClockTowerInteriorLayout.DoorWidth, DoorHeight=ClockTowerInteriorLayout.DoorHeight, DoorFloor=ClockTowerInteriorLayout.GroundFloor;
    public bool referenceExteriorImported;
    public Vector3 LocalToWorld(Vector3 point)=>transform.TransformPoint(point);
    // Geometry changed from 9.5m to 20m. Use known stair/landing points rather
    // than scaling a saved height into the empty centre of another spiral turn.
    public bool TryMigrateLegacyPosition(Vector3 oldWorld,out Vector3 migrated) {
        migrated=oldWorld;
        var p=transform.InverseTransformPoint(oldWorld);
        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))return false;
        if(Mathf.Abs(p.x)>=InnerWidth/2||Mathf.Abs(p.z)>=InnerWidth/2||p.y<=.30f||p.y>5.90f)return false;
        var layout=GetComponent<ClockTowerInteriorLayout>();
        Vector3 local;
        if(p.y>=5.50f)local=layout.machineStandingPoint;
        else {
            float radius=new Vector2(p.x,p.z).magnitude;
            int step=Mathf.Clamp(Mathf.RoundToInt((p.y-.12f)/.17125f),1,31);
            local=radius>=.55f&&radius<=1.22f
                ? ClockTowerInteriorLayout.Polar(ClockTowerInteriorLayout.StartAngle+(step-.5f)*ClockTowerInteriorLayout.StepAngle,
                    ClockTowerInteriorLayout.WalkingRadius,ClockTowerInteriorLayout.StepTop(step-1))
                : new Vector3(0,ClockTowerInteriorLayout.GroundFloor,-1.40f);
        }
        migrated=LocalToWorld(local);return true;
    }
    public bool ContainsUpperWalkPosition(Vector3 world) {
        var p=transform.InverseTransformPoint(world);
        float width=p.y>=ClockTowerInteriorLayout.MachineFloor-.05f ? ClockTowerInteriorLayout.ChamberInteriorWidth : InnerWidth;
        return Mathf.Abs(p.x)<width/2 && Mathf.Abs(p.z)<width/2 && p.y>=-.3f && p.y<=ClockTowerInteriorLayout.MachineFloor+.30f;
    }
}
