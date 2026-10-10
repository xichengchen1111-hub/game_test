using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class BuildSlice {
    const string RailFolder="Assets/Art/Rail/";
    static Texture2D RailTexture(string name,bool normal=false,bool linear=false,bool paper=false) {
        string path=RailFolder+name;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing authored rail texture "+path);
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        var wrap=paper?TextureWrapMode.Clamp:TextureWrapMode.Repeat;
        if(importer.textureType!=type||importer.sRGBTexture==linear||importer.wrapMode!=wrap||importer.isReadable!=paper||importer.mipmapEnabled==paper) {
            importer.textureType=type;importer.sRGBTexture=!linear;importer.wrapMode=wrap;importer.isReadable=paper;
            importer.mipmapEnabled=!paper;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=paper;importer.maxTextureSize=paper?2048:512;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material RailMaterial(string name) {
        name=name.Split('.')[0];
        if(name=="RailGlass") {
            string path=RailFolder+"RailGlass.mat";
            var glass=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!glass){glass=new Material(Shader.Find("WrongDate/ClearGlass"));AssetDatabase.CreateAsset(glass,path);}
            glass.SetColor("_Tint",new Color(.65f,.76f,.80f,.045f));glass.renderQueue=3000;EditorUtility.SetDirty(glass);return glass;
        }
        string hex=name switch {
            "RailIron"=>"#44514E","RailGreen"=>"#345B47","RailIvory"=>"#E2D6BE","RailBrass"=>"#B59755",
            "RailWalnut"=>"#A37850","RailFelt"=>"#4B6651","RailGlow"=>"#FFD292","RailStone"=>"#D4C8AD",
            "RailFloor"=>"#DDD8C3","RailLeather"=>"#8C5C3D",
            "RailSlate"=>"#35414E","RailRed"=>"#773C36","RailCoal"=>"#1C2224","RailSkin"=>"#B99075",_=>"#A49170"};
        var material=Mat(name,hex,name=="RailGlow"?1.8f:0,name=="RailBrass"?.45f:.22f);
        material.SetFloat("_Metallic",name=="RailBrass"?.65f:name=="RailIron"?.22f:0);
        string surface=name switch {"RailStone"=>"Limestone","RailFloor"=>"Floor","RailWalnut"=>"Walnut","RailFelt"=>"Moquette","RailBrass"=>"Brass","RailIron"=>"Iron","RailSlate"=>"Slate","RailLeather"=>"Leather","RailGreen" or "RailIvory" or "RailRed"=>"Paint",_=>null};
        if(surface!=null) {
            material.SetTexture("_BaseMap",RailTexture(surface+"Base.png"));
            material.SetTexture("_BumpMap",RailTexture(surface+"Normal.png",true,true));
            material.SetTexture("_MetallicGlossMap",RailTexture(surface+"Mask.png",false,true));
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_BumpScale",name=="RailFelt"?.3f:.22f);
            float tiling=name=="RailStone"?.4f:name=="RailFloor"?.36f:name=="RailFelt"?3f:1f;
            material.SetTextureScale("_BaseMap",Vector2.one*tiling);EditorUtility.SetDirty(material);
        }
        return material;
    }
    static GameObject RailVisual(string name,Transform parent,bool train) {
        string path=RailFolder+name+".fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("Missing editable railway FBX "+path);
        if(!importer.isReadable||importer.importAnimation||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard) {
            importer.isReadable=true;importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        visual.transform.SetParent(parent,false);
        var markers=visual.GetComponentsInChildren<Transform>();
        var front=markers.Single(t=>t.name=="FrontMarker");var east=markers.Single(t=>t.name=="EastMarker");
        var direction=front.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up);
        if(Vector3.Distance(parent.InverseTransformPoint(front.position),new Vector3(0,0,-3))>.01f||Vector3.Distance(parent.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Rail import axis/scale mismatch "+name);
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>RailMaterial(m.name)).ToArray();
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass");
            if(glass)renderer.shadowCastingMode=ShadowCastingMode.Off;
            bool movable=renderer.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Wheel_")||t.name.StartsWith("Rod_"));
            if(!glass&&!movable)renderer.gameObject.AddComponent<MeshCollider>().sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            renderer.gameObject.isStatic=!train;
        }
        return visual;
    }
    static Transform RailMarker(Transform root,string name,Vector3 position) => Group(name,position,root).transform;
    static void RailRamp(string name,Transform parent,float x,float z0,float z1,float y0,float y1,float width) {
        var root=Group(name,Vector3.zero,parent);
        var mesh=new Mesh{name=name};
        mesh.vertices=new[]{new Vector3(x-width/2,y0,z0),new Vector3(x+width/2,y0,z0),new Vector3(x+width/2,y1,z1),new Vector3(x-width/2,y1,z1)};
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path="Assets/Generated/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=RailMaterial("RailStone");root.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void RailwayGround(Transform parent,float centreX,float trainZ,float span=88) {
        var iron=RailMaterial("RailIron");var ballast=Mat("RailBallast","#636469");
        Box("Ballast bed",new Vector3(centreX,.02f,trainZ),new Vector3(span,.06f,4),ballast,parent);
        for(float x=centreX-span/2+1;x<centreX+span/2;x+=.8f)Box("Railway sleeper",new Vector3(x,.08f,trainZ),new Vector3(.22f,.13f,2.5f),RailMaterial("RailWalnut"),parent,false);
        foreach(float z in new[]{trainZ-.7175f,trainZ+.7175f})Box("Steel rail",new Vector3(centreX,.16f,z),new Vector3(span,.16f,.09f),iron,parent,false);
        Box("Passenger platform",new Vector3(centreX,.21f,trainZ-5.5f),new Vector3(68,.42f,7.9f),RailMaterial("RailStone"),parent);
        Box("Platform safety edge",new Vector3(centreX,.425f,trainZ-1.64f),new Vector3(68,.01f,.25f),RailMaterial("RailIvory"),parent,false);
        RailRamp("StationPlatformRamp-"+centreX+"-"+trainZ,parent,centreX,trainZ-14,trainZ-9.4f,.1f,.42f,3.4f);
        var platformSign=new Vector3(centreX+15,2.4f,trainZ-6);
        foreach(float side in new[]{-1f,1f}) {
            Box("Platform sign iron post",platformSign+new Vector3(side*1.08f,-.875f,.095f),new Vector3(.07f,2.21f,.07f),iron,parent);
            Box("Platform sign foot",platformSign+new Vector3(side*1.08f,-1.94f,.095f),new Vector3(.24f,.08f,.24f),iron,parent);
        }
        RailBoard(parent,"一号站台",platformSign,new Vector2(2.6f,.48f),Quaternion.identity);
        RailValidateSigns(parent);
    }
    static void TrainPrefab() {
        var root=Group("墨绿与黄铜 · 可乘列车",Vector3.zero);
        var visual=RailVisual("TrainSet",root.transform,true);var train=root.AddComponent<RailTrain>();
        train.rideSeat=RailMarker(root.transform,"RideSeat",new Vector3(3.8f,1.05f,-.92f));
        train.standPoint=RailMarker(root.transform,"StandPoint",new Vector3(3.8f,1.05f,0));
        train.boardingPoint=RailMarker(root.transform,"BoardingPoint",new Vector3(5.9f,.42f,-3));
        var transforms=visual.GetComponentsInChildren<Transform>();
        train.wheels=transforms.Where(t=>t.name.StartsWith("Wheel_")&&t.GetComponent<MeshFilter>()==null).ToArray();
        foreach(var wheel in train.wheels)wheel.gameObject.AddComponent<RailWheel>().radius=wheel.GetComponentInChildren<Renderer>().bounds.size.y*.5f;
        train.rods=transforms.Where(t=>t.name.StartsWith("Rod_")&&t.GetComponent<MeshFilter>()==null).ToArray();
        foreach(var rod in train.rods)rod.gameObject.AddComponent<RailRod>().rest=rod.localPosition;
        train.doors=transforms.Where(t=>t.name.StartsWith("Door_")&&t.GetComponent<MeshFilter>()==null).ToArray();
        foreach(var door in train.doors){var handle=door.gameObject.AddComponent<RailDoor>();handle.train=train;handle.title="打开车门";}
        foreach(float offset in new[]{0f,-16f})foreach(float end in new[]{-5.9f,5.9f}) {
            // Three 21cm rises from the 42cm platform to the 105cm coach floor.
            for(int step=0;step<3;step++) {
                float y=.42f+(step+1)*.21f;
                var stair=Box("Boarding step",new Vector3(offset+end,(y+.42f)/2,-2.18f+step*.27f),new Vector3(1.15f,y-.42f,.29f),RailMaterial("RailIron"),root.transform);
                stair.isStatic=false;
            }
            // Inside a vestibule the entry is level with the same shared floor.
        }
        Box("Coach gangway floor",new Vector3(-8,.985f,0),new Vector3(1.2f,.13f,.8f),RailMaterial("RailWalnut"),root.transform).isStatic=false;
        var seat=Group("Departure seat interaction",new Vector3(3.8f,1.7f,-.92f),root.transform);
        Collider(seat,Vector3.zero,new Vector3(.85f,.48f,.48f));var interact=seat.AddComponent<RailSeat>();interact.train=train;interact.title="坐下并出发";
        foreach(float offset in new[]{0f,-16f})foreach(float x in new[]{-4f,-1.5f,1.5f,4f}) {
            var light=Point("Coach warm pendant",root.transform.TransformPoint(new Vector3(offset+x,3.16f,0)),C("#FFDFAD"),1.8f,4.8f);
            light.transform.SetParent(root.transform,true);DayNightLighting.Bind(light,DayNightLamp.Role.Interior,.65f,x==-4?DayNightLighting.ModuleGlow(visual.transform,offset==0?"Module_Coach1_Static":"Module_Coach2_Static"):new Renderer[0]);
        }
        train.sound=root.AddComponent<AudioSource>();train.sound.spatialBlend=.4f;train.sound.playOnAwake=false;train.sound.maxDistance=30;
        foreach(float offset in new[]{0f,-16f}) {
            RailSurfaceText(root.transform,offset==0?"01":"02",new Vector3(offset+6.9f,2.75f,-1.515f),Quaternion.identity,region:new Vector2(.26f,.24f),inset:.025f);
            RailSurfaceText(root.transform,"青川  ·  邻镇",new Vector3(offset,3.375f,-1.515f),Quaternion.identity,C("#314C3C"),new Vector2(3.4f,.30f),.035f);
        }
        var headlamp=Point("Locomotive headlamp",new Vector3(25,2.5f,0),C("#FFD39A"),1,7);headlamp.transform.SetParent(root.transform,true);DayNightLighting.Bind(headlamp,DayNightLamp.Role.Exterior,.85f,DayNightLighting.ModuleGlow(visual.transform,"Module_Loco_Static").Concat(DayNightLighting.ModuleGlow(visual.transform,"Module_Tender_Detail")).ToArray());
        train.whistle=AssetDatabase.LoadAssetAtPath<AudioClip>(RailFolder+"whistle.wav");train.running=AssetDatabase.LoadAssetAtPath<AudioClip>(RailFolder+"running.wav");
        train.chimney=RailSteam(root.transform,"Chimney smoke",new Vector3(23,3.6f,0),false);
        train.steam=RailSteam(root.transform,"Cylinder steam",new Vector3(20,.8f,-1.1f),true);
        // Re-register train shelter footprints as the vehicle moves; static roofs retain the spatial hash.
        foreach(float offset in new[]{0f,-16f}) {
            var shelter=Group("Coach rain shelter",new Vector3(offset,0,0),root.transform);WeatherAuthoring.Shelter(shelter,new Vector2(15,2.96f),3.46f,.34f);
            var profile=shelter.GetComponent<WeatherShelter>();profile.ridgeAlongX=true;profile.elliptical=true;
            shelter.AddComponent<MovingWeatherShelter>();
        }
        TrainItems(root.transform,visual.transform);
        RailValidateSigns(root.transform);
        RailChecks.ValidateTrain(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Generated/RailTrain.prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static ParticleSystem RailSteam(Transform parent,string name,Vector3 point,bool burst) {
        var system=Group(name,point,parent).AddComponent<ParticleSystem>();
        var main=system.main;main.startLifetime=burst?2.5f:4;main.startSpeed=burst?.7f:.45f;main.startSize=burst?.3f:.45f;
        main.maxParticles=burst?32:40;main.startColor=new Color(.78f,.81f,.82f,burst?.5f:.33f);main.simulationSpace=ParticleSystemSimulationSpace.World;main.playOnAwake=!burst;
        var emission=system.emission;emission.rateOverTime=burst?8:6;
        var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.10f;
        system.transform.localRotation=Quaternion.Euler(-90,0,0);
        var size=system.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,2));
        var color=system.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.gray,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.6f,0),new GradientAlphaKey(0,1)});color.color=gradient;
        string path=RailFolder+"Steam.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);material.SetFloat("_ZWrite",0);material.renderQueue=3000;material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");AssetDatabase.CreateAsset(material,path);}
        system.GetComponent<ParticleSystemRenderer>().sharedMaterial=material;
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(RailFolder+"Steam.png"));EditorUtility.SetDirty(material);
        if(burst)system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return system;
    }
    static void StationPrefab() {
        var root=Group("B 方案 · 铁与玻璃车站",Vector3.zero);var visual=RailVisual("StationB",root.transform,false);
        WeatherAuthoring.Shelter(root,new Vector2(30,12),6,1.6f);root.GetComponent<WeatherShelter>().ridgeAlongX=true;
        WeatherAuthoring.Shelter(root,new Vector2(68,22),5.4f,4,new Vector2(0,20));
        var canopy=root.GetComponents<WeatherShelter>().Last();canopy.ridgeAlongX=true;canopy.barrel=true;
        foreach(float x in new[]{-19f,19f}) {
            var annex=Group("Annex roof",new Vector3(x,0,0),root.transform);WeatherAuthoring.Shelter(annex,new Vector2(8,8),4,1.2f);annex.GetComponent<WeatherShelter>().ridgeAlongX=true;
        }
        foreach(float x in new[]{-8f,-3f,3f,8f}) {
            var lamp=Point("Station hall lantern",new Vector3(x,Mathf.Abs(x)==8?4.4f:4.7f,0),C("#FFE0B1"),6.5f,10);lamp.transform.SetParent(root.transform,false);DayNightLighting.Bind(lamp,DayNightLamp.Role.Interior,.65f,x==-8?DayNightLighting.ModuleGlow(visual.transform,"Module_Hall_Lamps"):new Renderer[0]);
        }
        foreach(float x in new[]{-25f,-15f,-5f,5f,15f,25f}) {
            var lamp=Point("Platform lantern",new Vector3(x,5.7f,20),C("#FFDEA8"),3.5f,11);lamp.transform.SetParent(root.transform,false);DayNightLighting.Bind(lamp,DayNightLamp.Role.Exterior,.85f,x==-25?DayNightLighting.ModuleGlow(visual.transform,"Module_Platform_Lamps"):new Renderer[0]);
        }
        foreach(float x in new[]{-10f,-6f,6f,10f}) {
            var lamp=Point("Facade wall lantern",new Vector3(x,3.25f,-6.76f),C("#FFDAA5"),3.5f,8);lamp.transform.SetParent(root.transform,false);
            DayNightLighting.Bind(lamp,DayNightLamp.Role.Exterior,.85f,x==-10?DayNightLighting.ModuleGlow(visual.transform,"Module_Facade_Lamps"):new Renderer[0]);
        }
        WeatherAuthoring.Shelter(root,new Vector2(3.94f,1.12f),3.76f,0,new Vector2(0,-6.67f));
        var clock=Group("Shared station clock",new Vector3(0,7,-6.28f),root.transform).AddComponent<ClockDial>();
        clock.minuteHand=Group("Station minute hand",Vector3.zero,clock.transform).transform;
        clock.hourHand=Group("Station hour hand",new Vector3(0,0,-.008f),clock.transform).transform;
        Box("Minute needle",new Vector3(0,.22f,0),new Vector3(.025f,.44f,.015f),RailMaterial("RailIron"),clock.minuteHand,false);
        Box("Hour needle",new Vector3(0,.14f,0),new Vector3(.04f,.28f,.015f),RailMaterial("RailIron"),clock.hourHand,false);
        for(int i=0;i<12;i++) {
            float angle=i*Mathf.PI/6;
            var tick=Box("Station clock index",new Vector3(Mathf.Sin(angle)*.46f,Mathf.Cos(angle)*.46f,-.004f),new Vector3(.018f,i%3==0?.075f:.045f,.008f),RailMaterial("RailIron"),clock.transform,false);
            tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);
        }
        var porchFace=RailFace(root.transform,new Vector3(0,3.72f,-7.26f),Quaternion.identity);
        RailBoard(root.transform,"青 川 车 站",root.transform.InverseTransformPoint(porchFace.point-Vector3.forward*.06f),new Vector2(3.7f,.48f),Quaternion.identity);
        RailSurfaceText(root.transform,"QINGCHUAN",new Vector3(0,6.25f,-6.29f),Quaternion.identity);
        RailSurfaceText(root.transform,"售 票 处",new Vector3(-8,2.7f,.92f),Quaternion.identity);
        RailSurfaceText(root.transform,"一 号 站 台",new Vector3(0,4.95f,17.90f),Quaternion.identity);
        RailWallBoard(root.transform,"候车厅  →  站台",new Vector3(0,3.8f,5.88f),new Vector2(3.1f,.42f),Quaternion.identity);
        RailSurfaceText(root.transform,"旅客须知\n检票时请交验车票\n大件行李请妥善放置",new Vector3(10,2.70f,5.80f),Quaternion.identity,C("#453C30"));
        RailSurfaceText(root.transform,"售票与候车\n请从后门前往站台\n请勿在轨道上停留",new Vector3(-14.81f,2.60f,-3.5f),Quaternion.Euler(0,-90,0),C("#453C30"));
        RailSurfaceText(root.transform,"行李须知\n大件行李请寄存\n请勿阻塞过道",new Vector3(14.81f,2.60f,-3.5f),Quaternion.Euler(0,90,0),C("#453C30"));
        StationItems(root.transform);
        RailValidateSigns(root.transform);
        RailChecks.ValidateStation(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Generated/StationB.prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static GameObject StationTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/StationB.prefab"));root.transform.position=new Vector3(188,0,46);MapMarker(root,b,true);
        RailwayGround(root.transform,0,27);
        var train=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/RailTrain.prefab"));train.transform.position=new Vector3(181,0,73);train.GetComponent<RailTrain>().destination="OtherTown";
        // Parent beneath the station so full-map geometry retains a single authored hierarchy.
        train.transform.SetParent(root.transform,true);
        foreach(float x in new[]{184f,192f}) {
            var lantern=Model("Lantern",new Vector3(x,0,38.5f));lantern.transform.SetParent(root.transform,true);
            var pool=Point("Station entrance lantern",new Vector3(x,3.1f,38.5f),C("#FFD49A"),3.5f,10);pool.transform.SetParent(lantern.transform,true);DayNightLighting.Bind(pool,DayNightLamp.Role.Exterior,.85f,DayNightLighting.Glowing(lantern.transform));
        }
        RailWasteTickets(root.transform);
        RailBoard(root.transform,"废票",new Vector3(-11.3f,.40f,-1.057f),new Vector2(.24f,.14f),Quaternion.identity,inset:.03f);
        var punch=Group("手持检票钳",new Vector3(-6.68f,1.235f,.64f),root.transform);Collider(punch,Vector3.zero,new Vector3(.26f,.065f,.07f));
        var tool=punch.AddComponent<Interactable>();tool.title="手持检票钳";tool.source="青川车站售票柜台";tool.body="金属检票钳。按下把手，钳口便在票上打出一个小孔。";
        var clerk=Group("车站售票员",new Vector3(-8,1.64f,1.9f),root.transform);
        Collider(clerk,Vector3.zero,new Vector3(.35f,.30f,.28f));var conversation=clerk.AddComponent<Interactable>();conversation.title="售票员";conversation.source="青川车站售票处";conversation.body="检票的时候，会用检票钳在票上打孔。旁边废票桶里的同线路旧票，你可以拿起来看看。\n\n请从候车厅后门前往站台，车厢靠站台的一侧可以登车。坐稳后，列车便会出发。";
        RailEvidence(root.transform,"E08","巡警原簿副本","本镇车站资料桌 · 获准查阅的另一镇巡警原簿副本","一九三一年一月十九日\n下午五时二十分\n\n南区巡查记录：有人询问东街。此处没有该街。\n\n本页为获准查阅的原簿副本。",new Vector3(9,1.06f,1),"E08.png");
        RailValidateSigns(root.transform);
        return root;
    }
    static void RailEvidence(Transform parent,string id,string title,string source,string body,Vector3 position,string image) {
        var paper=Box(title,position,new Vector3(.55f,.025f,.4f),Mat("RailPaper","#E4D5B8"),parent);
        var item=paper.AddComponent<Interactable>();item.title=title;item.source=source;item.evidenceId=id;item.body=body;
        item.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(RailFolder+image);
    }
    static void OtherTown() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(false);
        var root=Group("邻镇 · 到达车站街区",Vector3.zero);
        Box("Arrival district ground",new Vector3(0,-.22f,-25),new Vector3(120,.4f,110),Mat("Ground","#4D525D"),root.transform);
        RailwayGround(root.transform,7,0,104);
        // A small original depot opposite the platform, with an open passage and actual window apertures.
        var depot=Group("Neighbour town depot",new Vector3(7,0,-21),root.transform);
        Box("Depot floor",new Vector3(0,.05f,0),new Vector3(24,.1f,10),RailMaterial("RailStone"),depot.transform);
        foreach(float side in new[]{-1f,1f}) {
            float z=side*5;
            foreach(float x in new[]{-7.4f,7.4f})Box("Depot wall",new Vector3(x,2,z),new Vector3(9.2f,4,.25f),RailMaterial("RailStone"),depot.transform);
            Box("Entrance lintel",new Vector3(0,3.5f,z),new Vector3(5.6f,1,.25f),RailMaterial("RailStone"),depot.transform);
        }
        foreach(float x in new[]{-12f,12f})Box("Depot end wall",new Vector3(x,2,0),new Vector3(.25f,4,10),RailMaterial("RailStone"),depot.transform);
        Roof(depot.transform,24,10,4,1.8f,RailMaterial("RailSlate"));
        WeatherAuthoring.Shelter(depot,new Vector2(25,11),4,1.8f);
        foreach(float x in new[]{-6f,6f}) {
            var bulb=Cylinder("Arrival depot diffuser",new Vector3(x,3.3f,0),new Vector3(.42f,.07f,.42f),RailMaterial("RailGlow"),depot.transform);
            var lamp=Point("Arrival depot lantern",depot.transform.TransformPoint(new Vector3(x,3.2f,0)),C("#FFD49A"),4,10);lamp.transform.SetParent(depot.transform,true);DayNightLighting.Bind(lamp,DayNightLamp.Role.Interior,.45f,bulb.GetComponent<Renderer>());
        }
        RailWallBoard(depot.transform,"邻 镇 车 站",new Vector3(0,3.5f,-5.125f),new Vector2(5f,.60f),Quaternion.identity);
        RailWallBoard(depot.transform,"出站  ·  车站街区",new Vector3(0,3.5f,-4.875f),new Vector2(5f,.60f),Quaternion.Euler(0,180,0));
        RailValidateSigns(depot.transform);
        foreach(float x in new[]{-7f,7f})Model("Bench",new Vector3(7+x,.1f,-22));
        Box("Arrival town street",new Vector3(0,.025f,-37),new Vector3(110,.06f,8),Mat("ArrivalRoad","#65636A"),root.transform);
        foreach(float z in new[]{-31f,-43f})Box("Arrival pavement",new Vector3(0,.065f,z),new Vector3(110,.10f,4),RailMaterial("RailStone"),root.transform);
        // Environment only: no invented witnesses, timetable or case-critical locations.
        foreach(float x in new[]{-42f,-27f,-12f,27f,42f}) {
            var house=Group("Station street frontage",new Vector3(x,0,-52),root.transform);
            Box("Brick frontage",new Vector3(0,3.4f,0),new Vector3(11,6.8f,9),Mat("ArrivalBrick","#8A7062"),house.transform);
            house.name+=" "+x;Roof(house.transform,11,9,6.8f,1.9f,RailMaterial("RailSlate"));
            foreach(float wx in new[]{-3f,0f,3f})foreach(float y in new[]{2f,5f})Quad("Opaque environment window",new Vector3(wx,y,4.52f),new Vector2(1.6f,1.8f),RailMaterial("RailIron"),house.transform).transform.localRotation=Quaternion.Euler(0,180,0);
            WeatherAuthoring.Shelter(house,new Vector2(11,9),6.8f,1.9f);
        }
        foreach(float x in new[]{-40f,-20f,0f,20f,40f}) {
            var lamp=Model("Lantern",new Vector3(x,0,-31));var pool=Point("Arrival street pool",new Vector3(x,3,-31),C("#FFD39A"),1.5f,11);pool.transform.SetParent(lamp.transform,true);DayNightLighting.Bind(pool,DayNightLamp.Role.Exterior,.85f,DayNightLighting.Glowing(lamp.transform));
        }
        var train=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/RailTrain.prefab"));train.transform.position=Vector3.zero;train.GetComponent<RailTrain>().destination="Town";
        Group("Arrival lamp budget",Vector3.zero).AddComponent<TownLightBudget>();
        foreach(float x in new[]{-60f,60f})Box("Arrival district edge",new Vector3(x,2,-25),new Vector3(.2f,4,110),RailMaterial("RailIron"),root.transform);
        foreach(float z in new[]{-80f,30f})Box("Arrival district edge",new Vector3(0,2,z),new Vector3(120,4,.2f),RailMaterial("RailIron"),root.transform);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/OtherTown.unity");
    }
}
