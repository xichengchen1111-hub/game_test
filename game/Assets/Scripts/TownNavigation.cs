using System;
using UnityEngine;
using UnityEngine.AI;

public sealed class TownNavigation : IDisposable {
    NavMeshDataInstance instance;
    readonly NavMeshPath path=new NavMeshPath();
    public bool Ready {get;private set;}
    public Vector3[] Corners {get;private set;}=Array.Empty<Vector3>();
    public float Distance {get;private set;}
    public Vector3 Destination {get;private set;}
    public TownNavigation() {
        var data=Resources.Load<NavMeshData>("TownNavigation");
        if(data){instance=NavMesh.AddNavMeshData(data);Ready=instance.valid;}
        else Debug.LogError("Town navigation asset missing; rebuild with BuildSlice");
    }
    public bool Snap(Vector3 requested,float radius,out Vector3 point) {
        point=requested;
        if(!Ready||!NavMesh.SamplePosition(requested,out var hit,radius,NavMesh.AllAreas))return false;
        // A street click cannot accidentally target a disconnected roof above it.
        if(Mathf.Abs(hit.position.y-requested.y)>1.5f)return false;
        point=hit.position;return true;
    }
    public bool Route(Vector3 from,Vector3 to) {
        Corners=Array.Empty<Vector3>();Distance=0;
        if(!Snap(from,2,out var start)||!Snap(to,1.5f,out var end))return false;
        if(!NavMesh.CalculatePath(start,end,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return false;
        Corners=path.corners;Destination=end;
        for(int i=1;i<Corners.Length;i++)Distance+=Vector3.Distance(Corners[i-1],Corners[i]);
        return true;
    }
    public void Dispose(){if(instance.valid)instance.Remove();Ready=false;}
}
