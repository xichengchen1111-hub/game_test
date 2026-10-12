using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class BuildSlice {
    const string WalnutFolder="Assets/Art/WalnutShop/";
    static Material ShopMaterial(string name) {
        name=name.Split('.')[0];
        if(name=="ShopGlass") {
            string p=WalnutFolder+name+".mat";
            var glass=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(!glass){glass=new Material(Shader.Find("WrongDate/ClearGlass"));AssetDatabase.CreateAsset(glass,p);}
            glass.SetColor("_Tint",new Color(.65f,.75f,.80f,.035f));glass.renderQueue=3000;EditorUtility.SetDirty(glass);return glass;
        }
        string hex=name switch {
            "ShopWalnut"=>"#65452F","ShopGreen"=>"#334D43","ShopBrass"=>"#B89453",
            "ShopCopper"=>"#877265","ShopIvory"=>"#C9B99D","ShopFelt"=>"#354F40",
            "ShopGlow"=>"#FFC682","ShopPaper"=>"#D8C8A7","ShopOak"=>"#A67A50",
            "ShopIron"=>"#303538","ShopRug"=>"#70534A",_=>"#786252"};
        var m=Mat(name,hex,name=="ShopGlow"?1.5f:0,name=="ShopBrass"?.48f:.25f);
        m.SetFloat("_Metallic",name=="ShopBrass"||name=="ShopCopper"?.65f:0);
        string image=name=="ShopWalnut"||name=="ShopOak"||name=="ShopGreen"?"Wood.png":name=="ShopIvory"?"Plaster.png":name=="ShopRug"?"Rug.png":null;
        var texture=image==null?null:AssetDatabase.LoadAssetAtPath<Texture2D>(WalnutFolder+image);
        if(texture)m.SetTexture("_BaseMap",texture);
        return m;
    }
    static GameObject WalnutVisual(Transform parent) {
        string p=WalnutFolder+"WalnutShop.fbx";
        var importer=AssetImporter.GetAtPath(p) as ModelImporter;
        if(!importer)throw new Exception("Generate tools/build_walnut_shop.py first.");
        if(importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||!importer.isReadable) {
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.isReadable=true;
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
        visual.name="Walnut shop joinery";visual.transform.SetParent(parent,false);
        var markers=visual.GetComponentsInChildren<Transform>();
        var front=markers.Single(t=>t.name=="FrontMarker");
        var east=markers.Single(t=>t.name=="EastMarker");
        var direction=front.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up);
        if(Vector3.Distance(parent.InverseTransformPoint(front.position),new Vector3(0,0,-4))>.01f||
            Vector3.Distance(parent.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Walnut FBX axes/scale mismatch: front="+front.position+" east="+east.position);
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ShopMaterial(m.name)).ToArray();
            if(renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass"))renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        return visual;
    }
    static void Solid(string name,Transform parent,Vector3 position,Vector3 size) {
        var solid=Group(name,position,parent);Collider(solid,Vector3.zero,size);
    }
    static void HeritageDial(Transform parent,Vector3 position,float diameter) {
        var face=Group("Heritage clock face",position,parent).transform;
        var disc=Cylinder("Ivory circular dial",Vector3.zero,new Vector3(diameter,.006f,diameter),ShopMaterial("ShopPaper"),face);
        disc.transform.localRotation=Quaternion.Euler(90,0,0);
        for(int i=0;i<12;i++) {
            float a=i*30*Mathf.Deg2Rad;
            var tick=Box("Clock hour index",new Vector3(Mathf.Sin(a)*diameter*.42f,Mathf.Cos(a)*diameter*.42f,-.012f),new Vector3(diameter*.018f,diameter*.060f,.009f),ShopMaterial("ShopIron"),face,false);
            tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);
        }
        // Painted trade-sign hands are decorative, not another accurate time source.
        var minute=Group("Fixed sign minute",new Vector3(0,0,-.023f),face).transform;
        var hour=Group("Fixed sign hour",new Vector3(0,0,-.033f),face).transform;
        Box("Minute hand",new Vector3(0,diameter*.16f,0),new Vector3(diameter*.022f,diameter*.36f,.01f),ShopMaterial("ShopIron"),minute,false);
        Box("Hour hand",new Vector3(0,diameter*.10f,0),new Vector3(diameter*.035f,diameter*.25f,.012f),ShopMaterial("ShopIron"),hour,false);
        minute.localRotation=Quaternion.Euler(0,0,-60);hour.localRotation=Quaternion.Euler(0,0,-305);
    }
    static void ShopDoor(Transform frame,BuildingOpening opening) {
        float width=opening.size.x-.10f,height=opening.size.y-.04f;
        var pivot=Group("Door hinge",new Vector3(-width/2,-opening.size.y/2+.02f,0),frame).transform;
        var green=ShopMaterial("ShopGreen");var brass=ShopMaterial("ShopBrass");
        for(int side=0;side<=1;side++)Box("Door stile",new Vector3(side*(width-.12f)+.06f,height/2,0),new Vector3(.12f,height,.10f),green,pivot,false);
        foreach(float y in new[]{.06f,.89f,height-.06f})Box("Door rail",new Vector3(width/2,y,0),new Vector3(width-.20f,.12f,.10f),green,pivot,false);
        Box("Lower raised panel",new Vector3(width/2,.45f,0),new Vector3(width-.24f,.70f,.065f),green,pivot,false);
        Box("Panel inset",new Vector3(width/2,.45f,-.044f),new Vector3(width-.38f,.53f,.025f),ShopMaterial("ShopWalnut"),pivot,false);
        var pane=Quad("Transparent door glass",new Vector3(width/2,(height+.91f)/2,0),new Vector2(width-.24f,height-1.02f),ShopMaterial("ShopGlass"),pivot);
        pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        for(int side=-1;side<=1;side+=2) {
            Box("Brass escutcheon",new Vector3(width-.20f,1.05f,side*.065f),new Vector3(.07f,.23f,.025f),brass,pivot,false);
            Box("Brass pull",new Vector3(width-.20f,1.07f,side*.11f),new Vector3(.04f,.17f,.045f),brass,pivot,false);
            for(int j=0;j<3;j++)Cylinder("Hinge knuckle",new Vector3(0,.30f+j*(height-.60f)/2,side*.04f),new Vector3(.045f,.065f,.045f),brass,pivot);
        }
        var leaf=pivot.gameObject.AddComponent<BoxCollider>();leaf.center=new Vector3(width/2,height/2,0);leaf.size=new Vector3(width,height,.10f);
        var door=pivot.gameObject.AddComponent<ShopMechanism>();door.fixtureId="shop-"+opening.id;door.movingPart=pivot;
        door.openEuler=new Vector3(0,-102,0);door.protectPlayer=true;door.leafCentre=leaf.center;door.leafSize=leaf.size;door.title="打开木门";
        foreach(var t in pivot.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
        for(int side=-1;side<=1;side+=2)Box("Door jamb",new Vector3(side*opening.size.x/2,0,0),new Vector3(.08f,opening.size.y,.28f),green,frame,false);
        Box("Door lintel",new Vector3(0,opening.size.y/2,0),new Vector3(opening.size.x+.12f,.08f,.28f),green,frame,false);
    }
    static void Fixture(GameObject kit,string name,string id,Vector3 euler,Vector3 slide) {
        var pivot=kit.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
        var fixture=pivot.gameObject.AddComponent<ShopMechanism>();fixture.fixtureId=id;fixture.movingPart=pivot;fixture.openEuler=euler;
        fixture.openOffset=pivot.parent.InverseTransformVector(kit.transform.parent.TransformVector(Vector3.back*slide.z));
        fixture.title=id.Contains("drawer")?"打开零件抽屉":"打开展柜";
        foreach(var renderer in pivot.GetComponentsInChildren<MeshRenderer>()) {
            var box=renderer.gameObject.AddComponent<BoxCollider>();box.center=renderer.GetComponent<MeshFilter>().sharedMesh.bounds.center;
            box.size=renderer.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        }
        foreach(var t in pivot.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
    }
    static void WalnutShopPrefab() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=Group("ClockShop",Vector3.zero);
        var instance=root.AddComponent<BuildingInstance>();instance.layout=shopLayout;instance.interior=true;
        Box("Interior floor",new Vector3(0,-.17f,0),new Vector3(shopLayout.footprint.x,.3f,shopLayout.footprint.y),ShopMaterial("ShopWalnut"),root.transform);
        SharedShell(root.transform,ShopMaterial("ShopIvory"));
        Box("Ceiling",new Vector3(0,3.37f,0),new Vector3(7.4f,.14f,8.2f),ShopMaterial("ShopIvory"),root.transform);
        Roof(root.transform,7.65f,8.45f,3.45f,.18f,Mat("HeritageRoof","#3F494A"));
        WeatherAuthoring.Shelter(root,new Vector2(7.65f,8.45f),3.45f,.18f);
        WeatherAuthoring.Shelter(root,new Vector2(7.55f,.94f),3.20f,0,new Vector2(0,-4.50f));
        var kit=WalnutVisual(root.transform);
        foreach(var opening in shopLayout.openings) {
            var frame=Group("Opening "+opening.id,shopLayout.OpeningCentre(opening),root.transform).transform;
            frame.localRotation=shopLayout.WallRotation(opening.wall);
            var marker=frame.gameObject.AddComponent<BuildingOpeningMarker>();marker.openingId=opening.id;marker.kind=opening.kind;marker.apertureSize=opening.size;
            if(opening.kind==OpeningKind.Window)ClearWindow(frame,opening.size.x,opening.size.y);
            else ShopDoor(frame,opening);
        }
        for(int side=-1;side<=1;side+=2) {
            float end=side<0?2.2f:1.10f,length=end+2.7f,middle=(end-2.7f)/2;
            Solid("Display cabinet collision",root.transform,new Vector3(side*2.85f,.51f,middle),new Vector3(.78f,1.02f,length));
            Solid("Window display plinth collision",root.transform,new Vector3(side*2,.4f,-3.3f),new Vector3(1.9f,.8f,.65f));
        }
        Solid("Workbench top collision",root.transform,new Vector3(2,.76f,1.85f),new Vector3(2.4f,.09f,.8f));
        for(float x=.95f;x<3.1f;x+=2.1f)Solid("Workbench pedestal collision",root.transform,new Vector3(x,.37f,1.85f),new Vector3(.38f,.74f,.75f));
        Solid("Rear cabinet collision",root.transform,new Vector3(-2,.65f,3.5f),new Vector3(2.2f,1.3f,.55f));
        Fixture(kit,"VitrineLeftHinge","shop-vitrine-left",new Vector3(0,95,0),Vector3.zero);
        Fixture(kit,"VitrineRightHinge","shop-vitrine-right",new Vector3(0,-95,0),Vector3.zero);
        Fixture(kit,"PartsDrawer","shop-parts-drawer",Vector3.zero,new Vector3(0,0,.32f));
        var npc=WatchmakerAsset.Create(new Vector3(2,0,2.7f));npc.transform.SetParent(root.transform,false);
        var talk=npc.AddComponent<Interactable>();talk.title="钟表匠";talk.source="钟表铺，维修台交谈";talk.body="“钟表坏了就放在柜台。老物件，慢慢修。”\n\n“想看什么，你自己看看。”";
        var notice=Group("RepairNotice",new Vector3(1.1f,.865f,1.6f),root.transform);
        Collider(notice,Vector3.zero,new Vector3(.50f,.025f,.36f));
        var paper=Quad("Notice ink",Vector3.up*.016f,new Vector2(.50f,.36f),ImageMat("NoticePaper","Notice.png",true),notice.transform);
        paper.transform.localRotation=Quaternion.Euler(90,0,0);
        var clue=notice.AddComponent<Interactable>();clue.title="修灯通知";clue.evidenceId="E07";clue.source="镇务维修通知，钟表铺维修台";
        clue.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"Notice.png");
        clue.body="镇务维修通知\n\n1931 年 1 月 20 日 · 中午十二时\n\n南区街灯已修复，灯柱上的白布于修复后拆下。\n\n修灯工签记：已完工。";
        HeritageDial(root.transform,new Vector3(0,3.68f,-4.32f),.67f);
        for(int side=-1;side<=1;side+=2) {
            var sign=Group("Hanging sign dial",new Vector3(-3.65f+side*.10f,2.59f,-4.75f),root.transform).transform;
            sign.localRotation=Quaternion.Euler(0,side<0?90:-90,0);HeritageDial(sign,Vector3.zero,.50f);
        }
        for(int side=-1;side<=1;side+=2) {
            var pendant=Cylinder("Pendant shade",new Vector3(side*1.75f,2.9f,-.5f),new Vector3(.50f,.065f,.50f),ShopMaterial("ShopBrass"),root.transform);
            Box("Pendant cord",new Vector3(side*1.75f,3.15f,-.5f),new Vector3(.016f,.42f,.016f),ShopMaterial("ShopIron"),root.transform,false);
            var pool=Point("Warm pendant pool",new Vector3(side*1.75f,2.76f,-.5f),C("#FFC386"),8f,5.7f,true);pool.transform.SetParent(root.transform,true);
            DayNightLighting.Bind(pool,DayNightLamp.Role.Interior,.85f,side<0?DayNightLighting.ModuleGlow(root.transform,"Module_SoftFurnishings"):new Renderer[0]);
            var facade=Point("Facade sconce pool",new Vector3(side*2.6f,3.45f,-4.65f),C("#FFD4A0"),3,3.5f);facade.transform.SetParent(root.transform,true);
            DayNightLighting.Bind(facade,DayNightLamp.Role.Exterior,.85f,side<0?DayNightLighting.ModuleGlow(root.transform,"Module_Facade"):new Renderer[0]);
        }
        var task=Point("Repair task light",new Vector3(2.65f,1.35f,1.9f),C("#FFD296"),2.5f,2.3f);task.transform.SetParent(root.transform,true);
        DayNightLighting.Bind(task,DayNightLamp.Role.Interior,.30f,DayNightLighting.ModuleGlow(root.transform,"Module_RepairBay"));
        var rear=Point("Rear ambient pool",new Vector3(0,2.75f,2.6f),C("#FFD19B"),5.2f,4.4f);rear.transform.SetParent(root.transform,true);
        DayNightLighting.Bind(rear,DayNightLamp.Role.Interior,.45f);
        WeatherAuthoring.BindDoors(root);
        ZFightRepair.Repair(root.transform,"ClockShop");
        PrefabUtility.SaveAsPrefabAsset(root,ShopPrefabPath);
        Debug.Log("WALNUT_SHOP_ASSET_PASS: calibrated FBX, two transparent windows, two physical doors, owner and evidence");
    }
    static void ConfigureWalnutLayout() {
        var map=TownPlan.Current.Building("B02");
        shopLayout.footprint=new Vector2(7.4f,8.2f);
        shopLayout.townPosition=new Vector3(map.parts[0].rect[0],0,map.parts[0].rect[1]);
        shopLayout.townYaw=TownPlan.Yaw(map.front);
        shopLayout.wallHeight=3.3f;shopLayout.roofRise=.18f;
        shopLayout.openings=new[]{
            new BuildingOpening{id="front-left",kind=OpeningKind.Window,wall=BuildingWall.Front,offset=-2,centreHeight=1.85f,size=new Vector2(2.1f,2.2f)},
            new BuildingOpening{id="front-door",kind=OpeningKind.Door,wall=BuildingWall.Front,offset=0,centreHeight=1.45f,size=new Vector2(1.4f,2.9f)},
            new BuildingOpening{id="front-right",kind=OpeningKind.Window,wall=BuildingWall.Front,offset=2,centreHeight=1.85f,size=new Vector2(2.1f,2.2f)},
            new BuildingOpening{id="rear-door",kind=OpeningKind.Door,wall=BuildingWall.Back,offset=0,centreHeight=1.2f,size=new Vector2(1.1f,2.4f)}
        };
        EditorUtility.SetDirty(shopLayout);
        System.IO.Directory.CreateDirectory("../assets/source/watch-shop");
        System.IO.File.WriteAllText("../assets/source/watch-shop/layout.json",JsonUtility.ToJson(shopLayout,true));
    }
}
