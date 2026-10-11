using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Finished dairy house placed inside the current map production parcel.
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
        root.transform.SetPositionAndRotation(new Vector3(TownPlan.Current.milkRect[0],-.04f,TownPlan.Current.milkRect[1]),Quaternion.Euler(0,180,0));
        root.transform.localScale=Vector3.one*.90f;
        WeatherAuthoring.Shelter(root,new Vector2(7.9f,6.56f),3.075f,1.92f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
        var marker=root.AddComponent<MilkShopPlacement>();
        marker.visualBounds=BoundsOf(root);
        marker.entrance=root.transform.TransformPoint(new Vector3(-.66f,.22f,-3.1f));
        marker.inside=root.transform.TransformPoint(new Vector3(-.66f,.20f,-1.1f));
        marker.outside=root.transform.TransformPoint(new Vector3(-.66f,.20f,-4.6f));
        marker.northPassageStart=root.transform.TransformPoint(new Vector3(-4.2f,.16f,-4.4f));
        marker.northPassageEnd=root.transform.TransformPoint(new Vector3(4.2f,.16f,-4.4f));
        float minimum=float.MaxValue;
        foreach(var neighbour in UnityEngine.Object.FindObjectsByType<TownMapBuilding>(FindObjectsSortMode.None)) {
            if(neighbour.mapId=="B04")continue;
            var b=BoundsOf(neighbour.gameObject);float gap=PlanGap(marker.visualBounds,b);minimum=Mathf.Min(minimum,gap);
            if(gap<.55f)throw new Exception("Milk shop too close to "+neighbour.mapId+": "+gap);
        }
        marker.minimumBuildingGap=minimum;
        if(root.GetComponentsInChildren<Collider>().Length!=12)throw new Exception("Milk shop colliders missing");
        var windows=root.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Windows");
        if(windows.sharedMaterials.Count(m=>m==glass)!=1||glass.shader.name!="WrongDate/ClearGlass"||glass.renderQueue!=3000)
            throw new Exception("Milk shop windows must share the transparent glass asset");
        if(Mathf.Abs(marker.visualBounds.min.y+.04f)>.015f)throw new Exception("Milk shop foundation isn't on the ground");
        MilkShopInteriorAsset.AddTo(root);
        ConfigureDoors(root);
        // In front of the counter, visible from the entrance; keep window aisles and ledger access.
        milkboy.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(.15f,.18f,.15f)),root.transform.rotation);
        foreach(var renderer in milkboy.GetComponentsInChildren<Renderer>())renderer.renderingLayerMask|=MilkShopInteriorAsset.InteriorFillLayer;
        marker.milkboyPosition=milkboy.transform.position;
        WeatherAuthoring.BindDoors(root);
        Directory.CreateDirectory("../output/demo/milkshop");
        File.WriteAllText("../output/demo/milkshop/placement-results.json",JsonUtility.ToJson(marker,true));
        Debug.Log("MILKSHOP_PLACEMENT_PASS: scale=.90 center="+root.transform.position+" minimum building gap="+minimum+" milkboy inside="+marker.milkboyPosition);
        return root;
    }
    static void ConfigureDoors(GameObject root) {
        var leaves=root.GetComponentsInChildren<Transform>();
        var left=leaves.Single(t=>t.name=="Door_Left");
        float openingAngle=Vector3.SignedAngle(left.parent.TransformDirection(Vector3.right),left.parent.TransformDirection(left.localRotation*Vector3.right),Vector3.up);
        if(Mathf.Abs(openingAngle)<90)throw new Exception("Milk door must retain its outward opening hinge");
        // Stop just short of square so the opened face remains reachable from inside the jamb.
        openingAngle=Mathf.Sign(openingAngle)*85;
        foreach(bool isLeft in new[]{true,false}) {
            var leaf=isLeft?left:leaves.Single(t=>t.name=="Door_Right");
            var collider=leaf.GetComponent<BoxCollider>();
            if(!collider)throw new Exception("Milk door has no physical leaf collider");
            leaf.localRotation=Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(leaf);
            var door=leaf.gameObject.AddComponent<ShopMechanism>();
            door.fixtureId=isLeft?"milk-left-door":"milk-right-door";door.movingPart=leaf;
            door.openEuler=new Vector3(0,isLeft?openingAngle:-openingAngle,0);
            door.protectPlayer=true;door.startsOpen=isLeft;door.leafCentre=collider.center;door.leafSize=collider.size;
            foreach(var part in leaf.GetComponentsInChildren<Transform>())part.gameObject.isStatic=false;
        }
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
