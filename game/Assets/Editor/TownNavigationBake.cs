using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Bake the same collider geometry that the CharacterController walks through.
public static class TownNavigationBake {
    public static void Build(Transform root) {
        var marks=new List<NavMeshBuildMarkup>();
        foreach(var fixture in root.GetComponentsInChildren<ShopMechanism>()) {
            // Hinged entrance leaves are usable passages; furniture stays solid.
            if(fixture.protectPlayer&&fixture.movingPart)
                marks.Add(new NavMeshBuildMarkup{root=fixture.movingPart,ignoreFromBuild=true});
        }
        var sources=new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(root,~(1<<2),NavMeshCollectGeometry.PhysicsColliders,0,marks,sources);
        var settings=NavMesh.GetSettingsByIndex(0);
        settings.agentRadius=.30f;settings.agentHeight=1.78f;settings.agentClimb=.24f;settings.agentSlope=44;
        settings.overrideVoxelSize=true;settings.voxelSize=.075f;settings.minRegionArea=.5f;
        var b=TownPlan.Current.bounds;
        var bounds=new Bounds(new Vector3((b[0]+b[1])/2,12,(b[2]+b[3])/2),new Vector3(b[1]-b[0]+2,32,b[3]-b[2]+2));
        var data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
        if(!data)throw new Exception("Town navigation bake failed");
        const string path="Assets/Resources/TownNavigation.asset";
        var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if(old){EditorUtility.CopySerialized(data,old);UnityEngine.Object.DestroyImmediate(data);data=old;}
        else AssetDatabase.CreateAsset(data,path);
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        Debug.Log("TOWN_NAVIGATION_BAKE_PASS: sources="+sources.Count+" radius=.30 height=1.78 voxel=.075");
    }
}
