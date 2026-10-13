using UnityEngine;
public sealed class HouseInstance : MonoBehaviour {
    public static HouseInstance Current;
    public void OnEnable(){Current=this;}
    public void OnDisable(){if(Current==this)Current=null;}
    public Vector3 Point(float x,float y,float z)=>transform.TransformPoint(new Vector3(x,y,z));
    public bool Contains(Vector3 world){var p=transform.InverseTransformPoint(world);return p.y>=-.2f&&p.y<6.5f&&((Mathf.Abs(p.x)<8&&Mathf.Abs(p.z)<6)||(p.x>=-8&&p.x<=-2.72f&&p.z>=6&&p.z<=13&&p.y<3.1f));}
    public bool ClearStandingPosition(Vector3 feet){
        foreach(var c in Physics.OverlapCapsule(feet+Vector3.up*.28f,feet+Vector3.up*1.52f,.27f,~0,QueryTriggerInteraction.Ignore))
            if(c.transform.IsChildOf(transform))return false;
        return true;
    }
    // Only recover a loaded, obstructed position, on the same storey and in the same room.
    // Ordinary movement and normal unobstructed saves are never repositioned.
    public Vector3 ResolveLoadedFurnitureOverlap(Vector3 saved){
        if(!Contains(saved))return saved;
        var p=transform.InverseTransformPoint(saved);float floor=p.y>3?3.36f:.16f;
        if(Mathf.Abs(p.y-floor)>.15f||p.x>3&&p.z>1)return saved; // Preserve the staircase.
        Physics.SyncTransforms();if(ClearStandingPosition(saved))return saved;
        int Room(Vector3 q){
            if(q.z>6)return 8;
            if(q.z>-1&&q.z<1)return 0;
            if(q.z<0)return floor>3?(q.x<1?1:2):1;
            return q.x<-2?3:q.x<3?4:5;
        }
        int room=Room(p);
        for(float radius=.1f;radius<=2;radius+=.1f)for(int step=0;step<48;step++){
            float angle=step*Mathf.PI/24;var q=p+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*radius;
            var world=transform.TransformPoint(q);
            if(Room(q)!=room||!Contains(world)||!ClearStandingPosition(world))continue;
            Debug.Log("HOME_FURNITURE_SAVE_RECOVERED: moved "+radius.ToString("F2")+"m within the saved room; investigation state retained.");
            return world;
        }
        return saved;
    }
}
