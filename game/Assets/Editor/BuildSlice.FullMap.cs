using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class BuildSlice {
    static void MapMarker(GameObject root,TownPlanBuilding b,bool finished) {
        var marker=root.AddComponent<TownMapBuilding>();marker.mapId=b.id;marker.finished=finished;
        marker.door=TownPlan.Point(b.door);marker.gate=TownPlan.Point(b.gate);
    }
    static Material MapTexture(string name,string file,bool cutout=false) {
        var m=Mat(name,"#FFFFFF");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/TownPlaceholders/"+file);
        m.SetTexture("_BaseMap",texture);m.enableInstancing=true;
        if(cutout){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.45f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;}
        return m;
    }
    static void PlaceholderBuilding(TownPlanBuilding b) {
        var root=Group(b.id+" "+b.name,Vector3.zero);if(b.id!="B04")MapMarker(root,b,false);
        int variant=FacadeVariant(b.id);var wall=PlaceholderFacade(variant);var roof=PlaceholderSlate();var gable=PlaceholderPlaster(variant);
        foreach(var part in b.parts) {
            // The dairy's main footprint is a production-yard placeholder around the existing house.
            if(b.id=="B04"&&part==b.parts[0])continue;
            var r=part.rect;float height=b.storeys==1?3.2f:6.1f;
            var block=Group(b.id+" "+part.role,new Vector3(r[0],0,r[1]),root.transform).transform;
            var solid=Box("Placeholder volume",new Vector3(0,height/2,0),new Vector3(r[2],height,r[3]),gable,block);
            // Opaque repeated facade panels, not glass with an invented room behind it.
            for(int face=0;face<4;face++) {
                var mount=Group("Placeholder facade",Vector3.zero,block).transform;mount.localRotation=Quaternion.Euler(0,face*90,0);
                float width=face%2==0?r[2]:r[3],depth=face%2==0?r[3]:r[2];
                var pane=Quad("Facade texture",new Vector3(0,height/2,-depth/2-.012f),new Vector2(width,height),wall,mount);
                pane.isStatic=true;
                var mesh=UnityEngine.Object.Instantiate(pane.GetComponent<MeshFilter>().sharedMesh);mesh.name="Facade "+b.id+" "+part.role+" "+face;
                // Atlas rows: bottom half ground storey (plinth, damp), top half upper storey (cornice).
                var uv=mesh.uv;for(int j=0;j<uv.Length;j++)uv[j]=new Vector2(uv[j].x*width/4.5f,uv[j].y*(b.storeys>=2?1f:.5f));mesh.uv=uv;mesh.RecalculateTangents();
                string path="Assets/Generated/Street/"+mesh.name.Replace('/','-')+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
                pane.GetComponent<MeshFilter>().sharedMesh=mesh;
            }
            Roof(block,r[2],r[3],height,1.15f,roof,gable);
            WeatherAuthoring.Shelter(block.gameObject,new Vector2(r[2],r[3]),height,1.15f);
        }
        if(b.id=="B04")return;
        foreach(bool rear in new[]{false,true}) {
            var entry=Group(rear?"Placeholder rear entrance":"Placeholder entrance",TownPlan.Point(rear?b.rearDoor:b.door,0),root.transform).transform;
            entry.localRotation=Quaternion.Euler(0,TownPlan.Yaw(rear?b.rearFacing:b.doorFacing),0);
            Box("Opaque door placeholder",new Vector3(0,1.05f,-.025f),new Vector3(1.05f,2.1f,.035f),Mat("MapDoor","#45574D"),entry,false);
            var item=entry.gameObject.AddComponent<Interactable>();item.title=b.name;item.source="青川镇";
            item.body="这里的建筑和室内尚未制作，当前保留位置与外观占位。";
            Collider(entry.gameObject,new Vector3(0,1.05f,-.07f),new Vector3(1.05f,2.1f,.12f));
        }
        foreach(var window in b.windows) {
            var opening=Group("Mapped opaque window",new Vector3(window.x,1.65f,window.z),root.transform).transform;
            opening.localRotation=Quaternion.Euler(0,TownPlan.Yaw(window.side),0);
            Box("Window placeholder",new Vector3(0,0,-.026f),new Vector3(window.width,1.5f,.026f),Mat("MapWindow","#4D6263"),opening,false);
        }
        if(b.id=="B17"||b.id=="B08") {
            var sign=Group("Existing lettered sign",TownPlan.Point(b.door,2.85f),root.transform).transform;
            sign.localRotation=Quaternion.Euler(0,TownPlan.Yaw(b.doorFacing),0);
            Box("Sign backing",new Vector3(0,0,-.07f),new Vector3(2.8f,.72f,.1f),Mat("SignWood","#283F40"),sign,false);
            Quad("Lettered sign",new Vector3(0,0,-.128f),new Vector2(2.65f,.65f),ImageMat(b.id=="B17"?"sign-SignStation.png":"sign-SignInn.png",b.id=="B17"?"SignStation.png":"SignInn.png"),sign);
        }
    }
    // Scene-level z-fight repair may edit scene objects and FBX model instances, never the
    // generated building prefabs (shared with other scenes and layout checks).
    static bool SceneEditable(MeshRenderer r) {
        if(!PrefabUtility.IsPartOfPrefabInstance(r))return true;
        // Objects added in the scene under a prefab instance (e.g. the station's waste tickets) are scene geometry.
        for(var t=r.transform;t;t=t.parent)if(PrefabUtility.IsAddedGameObjectOverride(t.gameObject))return true;
        var outer=PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
        return !outer||!PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outer).StartsWith("Assets/Generated/");
    }
    static void FullMapTown() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(false);
        var atmosphere=new System.Collections.Generic.HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
        var plan=TownPlan.Current;var bounds=plan.bounds;
        Box("Town ground",new Vector3((bounds[0]+bounds[1])/2,-.24f,(bounds[2]+bounds[3])/2),new Vector3(bounds[1]-bounds[0],.4f,bounds[3]-bounds[2]),Mat("Ground","#737663",0,.15f));
        var shop=InstantiateShop(false);MapMarker(shop.gameObject,plan.Building("B02"),true);
        var tower=InstantiateClockTower();MapMarker(tower,plan.Building("B01"),true);
        StreetSurfaces.BuildFull(plan);
        foreach(var b in plan.buildings)if(b.id=="B12"){var home=HouseAuthoring.Create();MapMarker(home,b,true);}else if(b.id=="B17")StationTown(b);else if(b.id=="B10")PoliceTown(b);else if(b.id=="B11")HospitalTown(b);else if(b.id=="B07")GarageTown(b);else if(b.id=="B08")HotelTown(b);else if(b.id!="B01"&&b.id!="B02")PlaceholderBuilding(b);
        var dairy=plan.Building("B04");var boy=Milkboy(TownPlan.Point(dairy.gate)+new Vector3(2,0,-.8f),180);boy.transform.localScale=Vector3.one*.88f;
        Collider(boy,new Vector3(0,.8f,0),new Vector3(.65f,1.65f,.55f));
        var testimony=DialogueScript.Load("MilkboyDialogue").evidence;var milk=boy.AddComponent<Interactable>();milk.title="送奶少年";milk.evidenceId=testimony.id;milk.source=testimony.source;milk.body=testimony.body;
        var milkHouse=MilkShopAsset.Create(boy);MapMarker(milkHouse,dairy,true);
        var cart=Model("MilkCart",TownPlan.Point(dairy.gate)+new Vector3(-2,.0f,-.9f),0);Collider(cart,new Vector3(0,.6f,0),new Vector3(1.6f,1.15f,.9f));
        // Existing props are distributed across the square, station forecourt and south garden.
        int streetBenchIndex=0;
        foreach(var pos in new[]{new Vector3(-10,.1f,8),new Vector3(10,.1f,8),new Vector3(-10,.1f,-8),new Vector3(10,.1f,-8),new Vector3(194,.1f,33),new Vector3(205,.1f,33),new Vector3(42,-.03f,-108),new Vector3(74,-.03f,-107)}) {
            var bench=Model("Bench",pos);FaceBenchTowardSquare(bench);Collider(bench,new Vector3(0,.45f,0),new Vector3(2,.9f,.75f));
            StreetBenchSeat(bench,streetBenchIndex++);
        }
        foreach(var b in plan.buildings.Where(b=>b.id.StartsWith("B")&&b.id!="B01"&&b.id!="B04")) {
            var offset=b.front=="E"?Vector3.right*1.8f:Quaternion.Euler(0,TownPlan.Yaw(b.front),0)*Vector3.right*2.1f;Model("Plant",TownPlan.Point(b.gate)+offset);
        }
        foreach(var lamp in plan.lamps)Lantern(new Vector3(lamp.x,.1f,lamp.z));
        var squareStandards=new[]{new Vector3(-11,.1f,-10),new Vector3(11,.1f,-10),new Vector3(-11,.1f,10),new Vector3(11,.1f,10)};
        foreach(var pos in squareStandards)Lantern(pos);
        TowerFloodlights(squareStandards);
        GreeneryAuthoring.Build(plan);
        PoliceCourtTree();
        GarageYardTree();
        // Actual silhouettes for the two-home sightline, with the specified lower fence.
        Box("Two homes low fence",new Vector3(-37,.425f,-57),new Vector3(.12f,.85f,11),Mat("MapFence","#766E5C"));
        foreach(var detail in plan.details.Where(d=>d.kind=="bed"||d.kind=="shed")) {
            var r=detail.rect;bool shed=detail.kind=="shed";float h=shed?1.6f:.04f;
            Box(detail.kind+" "+detail.owner,new Vector3(r[0],-.04f+h/2,r[1]),new Vector3(r[2],h,r[3]),Mat(shed?"MapShed":"MapSoil",shed?"#7E7460":"#5E6045"),null,shed);
        }
        foreach(var f in plan.facilities) {
            float h=f.id=="F01"?2.4f:f.id=="F05"?.85f:.03f;
            Box(f.name+" placeholder",TownPlan.Point(f.point,-.04f+h/2),new Vector3(f.width,h,f.depth),Mat("MapFacility"+f.id,f.id=="F01"?"#485C54":"#807862"),null,f.id=="F01");
        }
        foreach(var side in new[]{-1,1}) {
            Box("East west town limit",new Vector3(side<0?bounds[0]:bounds[1],.5f,(bounds[2]+bounds[3])/2),new Vector3(.2f,2,bounds[3]-bounds[2]),Mat("Boundary","#737663"));
            Box("North south town limit",new Vector3((bounds[0]+bounds[1])/2,.5f,side<0?bounds[2]:bounds[3]),new Vector3(bounds[1]-bounds[0],2,.2f),Mat("Boundary","#737663"));
        }
        var geometry=Group("Town geometry",Vector3.zero);
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if(root!=geometry&&!atmosphere.Contains(root)&&!root.GetComponent<DayNightCycle>()&&!root.GetComponent<UnityEngine.Rendering.Volume>())root.transform.SetParent(geometry.transform,true);
        new GameObject("Street light budget").AddComponent<TownLightBudget>();
        TownWetnessDriver();
        PlaceHotelCar();
        FullMapChecks.Validate(plan,geometry);
        // Scene pass after the layout checks: generated building prefabs were repaired when saved and stay fixed here.
        ZFightRepair.Repair(geometry.transform,"Town",SceneEditable);
        TownNavigationBake.Build(geometry.transform);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Town.unity");
    }
}
