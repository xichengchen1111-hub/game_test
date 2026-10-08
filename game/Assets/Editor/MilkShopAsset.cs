using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Placement for the currently playable slice, not a migration of the fourth town plan.
public static class MilkShopAsset {
    const string PrefabPath="Assets/Art/MilkShop1931/MilkShop1931.prefab";
    public static GameObject Create(GameObject milkboy) {
        // Update the shared asset so the town, its interior and copied street use the same panes.
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/MilkShop1931/OldWindow.mat");
        var shader=Shader.Find("WrongDate/ClearGlass");
        if(!glass||!shader)throw new Exception("Milk shop clear-glass material/shader missing");
        glass.shader=shader;
        glass.SetColor("_Tint",new Color(.55f,.68f,.78f,.055f));
        glass.SetOverrideTag("RenderType","Transparent");
        glass.renderQueue=(int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(glass);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if(!prefab)throw new Exception("Milk shop prefab is missing: "+PrefabPath);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        root.name="MilkShop";
        root.transform.SetPositionAndRotation(new Vector3(11.45f,-.04f,-7.35f),Quaternion.Euler(0,90,0));
        root.transform.localScale=Vector3.one*.90f;
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
        var marker=root.AddComponent<MilkShopPlacement>();
        marker.visualBounds=BoundsOf(root);
        marker.entrance=root.transform.TransformPoint(new Vector3(-.66f,.22f,-3.1f));
        marker.inside=root.transform.TransformPoint(new Vector3(-.66f,.20f,-1.1f));
        marker.outside=root.transform.TransformPoint(new Vector3(-.66f,.20f,-4.6f));
        marker.northPassageStart=new Vector3(7.8f,.1f,-3.14f);
        marker.northPassageEnd=new Vector3(14.4f,.1f,-3.14f);
        string[] neighbours={"Station","ClockShop","GuestHouse","Home","BackRow","EastBackground","WestBackground"};
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        float minimum=float.MaxValue;
        foreach(string name in neighbours) {
            var other=roots.SingleOrDefault(g=>g.name==name);
            if(!other)throw new Exception("Expected neighbour missing: "+name);
            var b=BoundsOf(other);
            float gap=PlanGap(marker.visualBounds,b);minimum=Mathf.Min(minimum,gap);
            if(gap<.55f)throw new Exception("Milk shop is too close to "+name+": "+gap+"m");
        }
        marker.minimumBuildingGap=minimum;
        float behind=Vector3.Dot(root.transform.position-milkboy.transform.position,milkboy.transform.forward);
        if(behind<3||marker.visualBounds.min.x<7.6f||marker.visualBounds.max.x>16.4f)
            throw new Exception("Milk shop obstructs the footway, boundary, or isn't behind the boy");
        if(root.GetComponentsInChildren<Collider>().Length!=12)throw new Exception("Milk shop colliders missing");
        var windows=root.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Windows");
        if(windows.sharedMaterials.Count(m=>m==glass)!=1||glass.shader.name!="WrongDate/ClearGlass"||glass.renderQueue!=3000)
            throw new Exception("Milk shop windows must share the transparent glass asset");
        if(Mathf.Abs(marker.visualBounds.min.y+.04f)>.015f)throw new Exception("Milk shop foundation isn't on the ground");
        Directory.CreateDirectory("../output/demo/milkshop");
        File.WriteAllText("../output/demo/milkshop/placement-results.json",JsonUtility.ToJson(marker,true));
        Debug.Log("MILKSHOP_PLACEMENT_PASS: scale=.90 center="+root.transform.position+" minimum building gap="+minimum+" behind boy="+behind);
        return root;
    }
    public static Bounds BoundsOf(GameObject obj) {
        var renderers=obj.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        if(renderers.Length==0)throw new Exception("No visible geometry: "+obj.name);
        var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static float PlanGap(Bounds a,Bounds b) {
        float dx=Mathf.Max(0,Mathf.Max(a.min.x-b.max.x,b.min.x-a.max.x));
        float dz=Mathf.Max(0,Mathf.Max(a.min.z-b.max.z,b.min.z-a.max.z));
        return Mathf.Sqrt(dx*dx+dz*dz);
    }
}
