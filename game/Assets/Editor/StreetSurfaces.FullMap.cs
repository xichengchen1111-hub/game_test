using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static partial class StreetSurfaces {
    public static GameObject BuildFull(TownPlan plan) {
        Directory.CreateDirectory(MeshRoot);AssetDatabase.Refresh();
        var root=new GameObject("Street surfaces");
        var road=Stone("StreetSetts","PeriodAsphalt",Color.white,.7f,.42f);
        var walk=Stone("StreetFlags","PeriodConcrete",Color.white,.7f,.30f);
        var kerb=Stone("StreetKerb","KerbStone",new Color(.85f,.86f,.87f),.8f,.35f);
        var yard=Stone("StreetYard","FootwayFlags",new Color(.76f,.76f,.69f),.5f,.24f);
        foreach(var surface in plan.surfaces) {
            var mesh=new Builder();
            for(int i=0;i<surface.vertices.Length;i+=9) {
                var v=surface.vertices;
                // Face uses X/Z metre UVs; Triangle's legacy end-cap UV uses X/Y.
                var a=new Vector3(v[i],v[i+1],v[i+2]);var b=new Vector3(v[i+3],v[i+4],v[i+5]);var c=new Vector3(v[i+6],v[i+7],v[i+8]);
                mesh.SurfaceTriangle(a,b,c);
            }
            mesh.Save("Map-"+surface.name,surface.material=="road"?road:surface.material=="kerb"?kerb:surface.material=="yard"?yard:walk,root.transform);
        }
        foreach(var r in plan.roads) {
            var marker=new GameObject(r.id+" "+r.name);marker.transform.SetParent(root.transform,false);
            var profile=marker.AddComponent<StreetSurfaceLayout>();profile.profileId=r.id;profile.carriageway=r.lane;profile.clearFootway=r.walk;profile.furnitureStrip=r.strip;
        }
        BuildPeriod(root,plan);
        Debug.Log("FULL_MAP_STREETS_PASS: "+plan.roads.Length+" profiles, "+plan.surfaces.Length+" partitioned chunks; metres UV, junction and building cutouts");
        return root;
    }
}
