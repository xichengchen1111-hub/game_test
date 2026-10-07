using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class BuildSlice {
    static Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static Font font;
    static string art="Assets/Art/Textures/";
    static Color C(string hex){ColorUtility.TryParseHtmlString(hex,out var c);return c;}
    static Material Mat(string name,string hex,float emission=0,float smoothness=0.15f) {
        if(mats.TryGetValue(name,out var old))return old;
        string path="Assets/Generated/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",C(hex));m.SetFloat("_Smoothness",smoothness);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",C(hex)*emission);}
        EditorUtility.SetDirty(m);mats[name]=m;return m;
    }
    static Material ImageMat(string name,string file,bool lit=false) {
        string path="Assets/Generated/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(lit?"Universal Render Pipeline/Lit":"Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(m,path);}
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(art+file));m.SetColor("_BaseColor",Color.white);
        EditorUtility.SetDirty(m);return m;
    }
    static GameObject Box(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null,bool collide=true) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;
        if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collide)UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.isStatic=true;return g;
    }
    static GameObject Cylinder(string name,Vector3 pos,Vector3 scale,Material mat,Transform parent=null) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;
        if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.isStatic=true;return g;
    }
    static GameObject Group(string name,Vector3 pos,Transform parent=null) {
        var g=new GameObject(name);if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=pos;return g;
    }
    static GameObject Quad(string name,Vector3 pos,Vector2 size,Material mat,Transform parent=null) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=name;
        if(parent)g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=new Vector3(size.x,size.y,1);
        g.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static GameObject Model(string name,Vector3 pos,float yaw=0) {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/"+name+".fbx");
        if(!asset)throw new Exception("Missing editable Blender asset: "+name);
        var obj=Group(name,pos);obj.transform.rotation=Quaternion.Euler(0,yaw,0);
        var imported=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        imported.transform.SetParent(obj.transform,false);
        var renderers=obj.GetComponentsInChildren<Renderer>();
        var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
        float expectedHeight=name switch {"Lantern"=>3.5f,"Bench"=>.86f,"MilkCart"=>1f,"Clockmaker"=>1.72f,"Desk"=>.86f,"Plant"=>.7f,_=>0};
        if(Mathf.Abs(bounds.size.y-expectedHeight)>.05f) throw new Exception("Imported model height mismatch: "+name+" "+bounds.size);
        Debug.Log("MODEL_IMPORT_PASS: "+name+" bounds="+bounds.size);
        foreach(var r in obj.GetComponentsInChildren<Renderer>()) {
            var list=r.sharedMaterials;
            for(int i=0;i<list.Length;i++){
                string key=list[i]?list[i].name.Split('.')[0].ToLowerInvariant():"wood";
                string hex=key switch {"wood"=>"#79543C","darkwood"=>"#3B3031","brass"=>"#B38A49","iron"=>"#252B38","glass_glow"=>"#FFB969","skin"=>"#C49B7A","coat"=>"#4E535A","trousers"=>"#30333F","cream"=>"#C5B69A","jade"=>"#52645A",_=>"#82644E"};
                list[i]=Mat("kit-"+key,hex,key=="glass_glow"?2f:0);
            }
            r.sharedMaterials=list;
        }
        return obj;
    }
    static void Collider(GameObject obj,Vector3 center,Vector3 size) {var c=obj.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static Light Point(string name,Vector3 position,Color color,float intensity,float range,bool shadow=false) {
        var g=new GameObject(name);g.transform.position=position;
        var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;
        l.shadows=shadow?LightShadows.Soft:LightShadows.None;return l;
    }
    static void SetupLight(bool inside) {
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=C(inside?"#9A8A7B":"#8B99BC");
        RenderSettings.ambientEquatorColor=C(inside?"#6A6570":"#74778F");
        RenderSettings.ambientGroundColor=C(inside?"#4B3730":"#4C4555");
        RenderSettings.ambientIntensity=1;
        RenderSettings.fog=!inside;RenderSettings.fogMode=FogMode.Exponential;RenderSettings.fogDensity=.012f;RenderSettings.fogColor=C("#646B8B");
        var sun=new GameObject("Dusk light").AddComponent<Light>();sun.type=LightType.Directional;
        sun.transform.rotation=Quaternion.Euler(35,-32,0);sun.color=C("#B4BBDD");sun.intensity=inside?.25f:1.15f;sun.shadows=LightShadows.Soft;
        RenderSettings.sun=sun;
        var sky=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Materials/DuskSky.mat");
        if(!sky){sky=new Material(Shader.Find("WrongDate/DuskSky"));AssetDatabase.CreateAsset(sky,"Assets/Generated/Materials/DuskSky.mat");}
        sky.SetColor("_Top",new Color(.24f,.26f,.37f));sky.SetColor("_Middle",new Color(.42f,.4f,.48f));sky.SetColor("_Bottom",new Color(.7f,.5f,.38f));EditorUtility.SetDirty(sky);
        RenderSettings.skybox=sky;
        var volume=new GameObject("Atmosphere").AddComponent<Volume>();volume.isGlobal=true;
        string path="Assets/Generated/"+(inside?"Interior":"Town")+"Volume.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        volume.sharedProfile=profile;
        if(!profile.TryGet<Tonemapping>(out var tonemap))tonemap=profile.Add<Tonemapping>();tonemap.mode.Override(TonemappingMode.ACES);
        if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>();bloom.intensity.Override(.25f);bloom.threshold.Override(1f);bloom.scatter.Override(.55f);
        if(!profile.TryGet<ColorAdjustments>(out var color))color=profile.Add<ColorAdjustments>();color.postExposure.Override(inside?.45f:.2f);color.saturation.Override(-8);
        if(!profile.TryGet<Vignette>(out var vignette))vignette=profile.Add<Vignette>();vignette.intensity.Override(.18f);vignette.smoothness.Override(.65f);
        foreach(var component in profile.components) if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);
    }
    static void Roof(Transform parent,float width,float depth,float bottom,float rise,Material material) {
        var g=new GameObject("Pitched roof");g.transform.SetParent(parent,false);
        var mesh=new Mesh();
        mesh.vertices=new[]{new Vector3(-width/2,bottom,-depth/2),new Vector3(width/2,bottom,-depth/2),new Vector3(-width/2,bottom,depth/2),new Vector3(width/2,bottom,depth/2),new Vector3(0,bottom+rise,-depth/2),new Vector3(0,bottom+rise,depth/2)};
        mesh.triangles=new[]{0,4,2,2,4,5,1,3,4,3,5,4,0,1,4,2,5,3,0,2,1,1,2,3};mesh.RecalculateNormals();
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
        string p="Assets/Generated/"+parent.name+"-roof.asset";
        if(AssetDatabase.LoadAssetAtPath<Mesh>(p))AssetDatabase.DeleteAsset(p);AssetDatabase.CreateAsset(mesh,p);
    }
    static void Window(Transform p,float x,float y,float z,float width=1.1f,float height=1.55f,bool glow=true) {
        var trim=Mat("WindowTrim","#37323A");var glass=Mat(glow?"WarmWindow":"DarkWindow",glow?"#D4A878":"#3B4962",glow?.6f:0);
        Box("Window frame",new Vector3(x,y,z),new Vector3(width+.16f,height+.16f,.12f),trim,p,false);
        Box("Glass",new Vector3(x,y,z-.07f),new Vector3(width,height,.045f),glass,p,false);
        Box("Mullion",new Vector3(x,y,z-.105f),new Vector3(.055f,height,.035f),trim,p,false);
        Box("Crossbar",new Vector3(x,y,z-.105f),new Vector3(width,.055f,.035f),trim,p,false);
        Box("Stone sill",new Vector3(x,y-height/2-.13f,z-.08f),new Vector3(width+.28f,.14f,.24f),Mat("Sill","#8A7F77"),p,false);
    }
    static Transform Building(string name,Vector3 pos,float yaw,float width,float height,string color,string sign=null) {
        var root=Group(name,pos).transform;root.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Plaster",new Vector3(0,height/2,0),new Vector3(width,height,6.4f),Mat(name+"-plaster",color),root);
        Box("Foundation",new Vector3(0,.22f,-3.25f),new Vector3(width+.3f,.44f,.3f),Mat("Foundation","#5D5A64"),root);
        Box("Cornice",new Vector3(0,height-.1f,-3.22f),new Vector3(width+.4f,.25f,.5f),Mat("Cornice","#736D72"),root,false);
        for(int floor=0;floor<2;floor++)for(int x=0;x<3;x++)Window(root,(x-1)*width/3.6f,2.0f+floor*2.8f,-3.23f,1.25f,floor==0?1.7f:1.5f,(x+floor)%3!=0);
        Roof(root,width+.45f,7,height,1.6f,Mat("Roof","#3B4255"));
        Box("Chimney",new Vector3(width/3,height+1.0f,1.5f),new Vector3(.65f,1.7f,.7f),Mat("Brick","#67565A"),root,false);
        if(sign!=null) {
            Box("Shop sign",new Vector3(0,3.7f,-3.34f),new Vector3(width-.6f,.9f,.16f),Mat("SignWood","#283F40"),root,false);
            Quad("Lettered sign",new Vector3(0,3.7f,-3.435f),new Vector2(width-.7f,.85f),ImageMat("sign-"+sign,sign),root);
            Box("Awning",new Vector3(0,3.05f,-3.95f),new Vector3(width+.4f,.15f,1.55f),Mat("Awning","#445A54"),root,false);
            for(int j=-1;j<=1;j+=2)Box("Shop post",new Vector3(j*(width/2+.05f),1.4f,-4.5f),new Vector3(.17f,2.8f,.17f),Mat("DarkTimber","#3B3031"),root);
        }
        return root;
    }
    static void Dial(Transform parent,Vector3 pos,float size,bool street) {
        var root=Group("Clock dial",pos,parent).transform;
        Quad("Clock face",Vector3.zero,new Vector2(size,size),ImageMat("ClockFace","ClockFace.png"),root);
        var controller=root.gameObject.AddComponent<ClockDial>();controller.streetClock=street;
        controller.minuteHand=Group("Minute",new Vector3(0,0,-.035f),root).transform;
        Box("Minute hand",new Vector3(0,size*.16f,0),new Vector3(size*.024f,size*.36f,.025f),Mat("ClockInk","#302B31"),controller.minuteHand,false).isStatic=false;
        controller.hourHand=Group("Hour",new Vector3(0,0,-.055f),root).transform;
        Box("Hour hand",new Vector3(0,size*.10f,0),new Vector3(size*.037f,size*.25f,.028f),Mat("ClockInk","#302B31"),controller.hourHand,false).isStatic=false;
        Box("Spindle",new Vector3(0,0,-.07f),Vector3.one*size*.035f,Mat("Brass","#B68C4B"),root,false);
    }
    static void Lantern(Vector3 pos) {
        var lamp=Model("Lantern",pos);Collider(lamp,new Vector3(0,1.4f,0),new Vector3(.24f,2.8f,.24f));
        Point("Lantern pool",pos+Vector3.up*2.95f,C("#FFBD78"),3.1f,6.3f);
    }
    static void Town() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(false);
        Box("Town ground",new Vector3(0,-.24f,4),new Vector3(55,.4f,65),Mat("Ground","#60637B",0,.38f));
        UnityEngine.Random.InitState(1931);
        for(int z=-20;z<29;z++)for(int x=-6;x<=6;x++) {
            string col=new[]{"#66697F","#717185","#747382","#666B7D","#777482"}[UnityEngine.Random.Range(0,5)];
            Box("Paving stone",new Vector3(x*1.02f+(z%2)*.51f,-.025f,z*1.0f),new Vector3(.99f,.08f,.965f),Mat("Paver"+col.Substring(1),col,0,.45f),null,false);
        }
        Box("West sidewalk",new Vector3(-7.1f,.03f,2),new Vector3(1.75f,.14f,42),Mat("Sidewalk","#827C85"));
        Box("East sidewalk",new Vector3(7.1f,.03f,2),new Vector3(1.75f,.14f,42),Mat("Sidewalk","#827C85"));
        var station=Building("Station",new Vector3(-9.2f,0,1),-90,8,6.5f,"#B3A89A","SignStation.png");
        var shop=Building("ClockShop",new Vector3(9,0,1),90,7,6.6f,"#9C8580","SignClock.png");
        var door=Box("Shop entrance",new Vector3(0,1.25f,-3.28f),new Vector3(1.4f,2.5f,.16f),Mat("Door","#413832"),shop);
        Quad("Shop door glazing",new Vector3(0,1.65f,-3.38f),new Vector2(1.08f,1.15f),ImageMat("DoorWindow","DoorWindow.png"),shop);
        Box("Door handle",new Vector3(.48f,1.04f,-3.42f),new Vector3(.065f,.35f,.07f),Mat("Brass","#B68C4B"),shop,false);
        var portal=door.AddComponent<ScenePortal>();portal.title="进入钟表铺";portal.destination="ClockShop";portal.entryPosition=new Vector3(0,.03f,-2.2f);portal.entryYaw=0;
        Building("GuestHouse",new Vector3(-10,0,14),-90,8,7.5f,"#929396","SignInn.png");
        Building("Home",new Vector3(10.2f,0,15),90,7.4f,6.3f,"#B1A398");
        Building("BackRow",new Vector3(0,0,28),0,12,7,"#84899B");
        Building("EastBackground",new Vector3(17,0,31),0,10,9,"#787D96");
        Building("WestBackground",new Vector3(-17,0,30),0,10,8,"#8A8B9A");
        var tower=Group("Town clock",new Vector3(0,0,10)).transform;
        Box("Tower base",new Vector3(0,.22f,0),new Vector3(3.9f,.44f,3.9f),Mat("TowerBase","#787783"),tower);
        Box("Tower shaft",new Vector3(0,3.55f,0),new Vector3(2.4f,6.6f,2.4f),Mat("TowerStone","#A19A96"),tower);
        Box("Clock chamber",new Vector3(0,6.3f,0),new Vector3(3f,2.05f,3f),Mat("TowerStone","#A19A96"),tower);
        Box("Clock cornice",new Vector3(0,7.42f,0),new Vector3(3.25f,.22f,3.25f),Mat("Cornice","#736D72"),tower,false);
        Roof(tower,3.45f,3.45f,7.55f,2.15f,Mat("Roof","#3B4255"));
        Dial(tower,new Vector3(0,6.35f,-1.52f),1.85f,true);
        var side=Group("East dial",new Vector3(1.52f,6.35f,0),tower).transform;side.localRotation=Quaternion.Euler(0,-90,0);Dial(side,Vector3.zero,1.85f,true);
        Point("Tower dial warm light",new Vector3(0,6.3f,8.2f),C("#E5BA7C"),1.2f,3);
        var plaque=Box("Clock observation",new Vector3(0,1.3f,8.77f),new Vector3(.9f,.44f,.05f),Mat("SignWood","#283F40"));
        var clock=plaque.AddComponent<Interactable>();clock.title="街钟";clock.evidenceId="E06";clock.source="广场的公共街钟";clock.body="广场上的钟，钟表铺的匠人负责维护。\n\n钟面可以直接观察，怀表在你身上。";
        foreach(float x in new[]{-5.7f,5.7f})foreach(float z in new[]{-8f,6f,19f})Lantern(new Vector3(x,0,z));
        var bench=Model("Bench",new Vector3(-2.8f,.03f,8));bench.transform.rotation=Quaternion.Euler(0,15,0);Collider(bench,new Vector3(0,.45f,0),new Vector3(2,.9f,.75f));
        var cart=Model("MilkCart",new Vector3(6.35f,.10f,-5.4f),-90);Collider(cart,new Vector3(0,.6f,0),new Vector3(1.6f,1.15f,.9f));
        var boy=Model("Clockmaker",new Vector3(6.15f,.1f,-4.2f),-85);boy.transform.localScale=Vector3.one*.88f;
        Collider(boy,new Vector3(0,.8f,0),new Vector3(.65f,1.65f,.55f));boy.AddComponent<AmbientMotion>();
        var milk=boy.AddComponent<Interactable>();milk.title="送奶少年";milk.evidenceId="E04";milk.source="送奶少年，广场初访";milk.body="“我没戴表。那天抬头看了一眼街钟，像是六点二十五。”\n\n“奶送到她手里，我才走。”";
        for(int i=0;i<4;i++)Model("Plant",new Vector3(i%2==0?-6.5f:6.5f,.12f,i<2?-1.8f:11.5f));
        for(int i=0;i<9;i++) {
            var shrub=GameObject.CreatePrimitive(PrimitiveType.Sphere);shrub.name="Clipped shrub";shrub.transform.position=new Vector3(Mathf.Cos(i*.7f)*2.3f,.48f,10+Mathf.Sin(i*.7f)*2.3f);
            shrub.transform.localScale=new Vector3(1.05f,.9f,1.05f);shrub.GetComponent<Renderer>().sharedMaterial=Mat("Foliage","#58685F");UnityEngine.Object.DestroyImmediate(shrub.GetComponent<Collider>());
        }
        Box("South town boundary",new Vector3(0,1,-23),new Vector3(55,3,.6f),Mat("Boundary","#64677B"));
        Box("West town boundary",new Vector3(-17,1,2),new Vector3(.6f,3,60),Mat("Boundary","#64677B"));
        Box("East town boundary",new Vector3(17,1,2),new Vector3(.6f,3,60),Mat("Boundary","#64677B"));
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Town.unity");
    }
    static void Shop() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(true);
        var timber=Mat("ShopWood","#7F573C");var wall=Mat("InteriorPlaster","#A09992");var panel=Mat("Wainscot","#5D4A42");
        Box("Interior floor",new Vector3(0,-.17f,0),new Vector3(7.2f,.3f,8.2f),timber);
        for(int i=0;i<18;i++)for(int j=0;j<4;j++)Box("Floor board",new Vector3(-3.35f+i*.39f,-.006f,-3+j*2.03f),new Vector3(.37f,.025f,2f),Mat("Floor"+(i+j)%3,new[]{"#845D45","#90684B","#78563F"}[(i+j)%3]),null,false);
        Box("Back wall",new Vector3(0,1.65f,4),new Vector3(7.2f,3.3f,.2f),wall);
        Box("Left wall",new Vector3(-3.6f,1.65f,0),new Vector3(.2f,3.3f,8.2f),wall);
        Box("Right wall",new Vector3(3.6f,1.65f,0),new Vector3(.2f,3.3f,8.2f),wall);
        Box("Front wall",new Vector3(0,1.65f,-4),new Vector3(7.2f,3.3f,.2f),wall);
        Box("Ceiling",new Vector3(0,3.35f,0),new Vector3(7.2f,.15f,8.2f),Mat("Ceiling","#776A63"));
        Box("Back panel",new Vector3(0,.58f,3.87f),new Vector3(7.1f,1.15f,.06f),panel,null,false);
        Box("Left panel",new Vector3(-3.47f,.58f,0),new Vector3(.06f,1.15f,8f),panel,null,false);
        Box("Right panel",new Vector3(3.47f,.58f,0),new Vector3(.06f,1.15f,8f),panel,null,false);
        var entry=Box("Return door",new Vector3(0,1.2f,-3.86f),new Vector3(1.4f,2.4f,.08f),Mat("Door","#413832"));
        Box("Return handle",new Vector3(.5f,1.02f,-3.78f),new Vector3(.065f,.35f,.07f),Mat("Brass","#B68C4B"),null,false);
        var exit=entry.AddComponent<ScenePortal>();exit.title="回到小镇";exit.destination="Town";exit.entryYaw=-90;
        var window=Group("Window recess",new Vector3(-3.47f,1.95f,-.5f)).transform;window.localRotation=Quaternion.Euler(0,-90,0);
        Window(window,0,0,0,2.5f,1.75f,true);
        Quad("Dusk seen through window",new Vector3(0,0,-.13f),new Vector2(2.35f,1.60f),ImageMat("WindowTown","WindowTown.png"),window);
        Box("Window upright",new Vector3(0,0,-.17f),new Vector3(.07f,1.73f,.035f),panel,window,false);
        Box("Window crosspiece",new Vector3(0,0,-.17f),new Vector3(2.45f,.07f,.035f),panel,window,false);
        Point("Cool window light",new Vector3(-2.8f,2.2f,-.6f),C("#8DAAD5"),1.4f,5.5f);
        var desk=Model("Desk",new Vector3(0,0,1.35f));Collider(desk,new Vector3(0,.44f,0),new Vector3(1.85f,.88f,.85f));
        var npc=Model("Clockmaker",new Vector3(.65f,0,2.65f),180);Collider(npc,new Vector3(0,.85f,0),new Vector3(.64f,1.7f,.55f));npc.AddComponent<AmbientMotion>();
        var talk=npc.AddComponent<Interactable>();talk.title="钟表匠";talk.source="钟表铺，柜台交谈";talk.body="“钟表坏了就放在柜台。老物件，慢慢修。”\n\n“想看什么，你自己看看。”";
        var notice=Box("RepairNotice",new Vector3(.45f,.91f,1.23f),new Vector3(.53f,.018f,.38f),Mat("Paper","#D5C19C"));
        var page=Quad("Notice ink",Vector3.zero,new Vector2(.52f,.37f),ImageMat("NoticePaper","Notice.png",true),notice.transform);
        page.transform.localScale=new Vector3(.52f/.53f,.37f/.38f,1);page.transform.localPosition=new Vector3(0,.55f,0);page.transform.localRotation=Quaternion.Euler(90,0,0);
        var clue=notice.AddComponent<Interactable>();clue.title="修灯通知";clue.evidenceId="E07";clue.source="镇务维修通知，钟表铺柜台";clue.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"Notice.png");
        clue.body="镇务维修通知\n\n1931 年 1 月 20 日 · 中午十二时\n\n南区街灯已修复，灯柱上的白布于修复后拆下。\n\n修灯工签记：已完工。";
        for(int i=0;i<4;i++) {
            float x=-2.4f+i*1.25f;
            Box("Wall clock case",new Vector3(x,2.13f,3.80f),new Vector3(.94f,1.35f,.18f),timber,null,false);
            var dial=Group("Wall clock "+i,new Vector3(x,2.35f,3.68f)).transform;Dial(dial,Vector3.zero,.7f,false);
            Box("Pendulum window",new Vector3(x,1.68f,3.68f),new Vector3(.35f,.42f,.05f),Mat("ClockInk","#302B31"),null,false);
            Cylinder("Pendulum bob",new Vector3(x,1.60f,3.60f),new Vector3(.13f,.025f,.13f),Mat("Brass","#B68C4B")).transform.rotation=Quaternion.Euler(90,0,0);
        }
        for(int i=0;i<3;i++) {
            Box("Display shelf",new Vector3(3.28f,.8f+i*.6f,1.15f),new Vector3(.45f,.09f,3.2f),timber);
            for(int j=0;j<5;j++)Box("Clock case or book",new Vector3(3.28f,1.01f+i*.6f,-.05f+j*.55f),new Vector3(.22f,.32f,.30f),Mat("Shelf"+(i+j)%3,new[]{"#53645C","#9B8060","#473F3F"}[(i+j)%3]),null,false);
        }
        Model("Plant",new Vector3(-2.9f,0,-2.8f));
        Box("Left worktable",new Vector3(-2.05f,.73f,2.4f),new Vector3(1.7f,.13f,.8f),timber);
        for(int i=-1;i<=1;i+=2)Box("Worktable leg",new Vector3(-2.05f+i*.65f,.34f,2.4f),new Vector3(.12f,.7f,.55f),panel);
        for(int i=0;i<5;i++)Cylinder("Brass wheel",new Vector3(-2.5f+i*.23f,.85f,2.35f),new Vector3(.18f,.018f,.18f),Mat("Brass","#B68C4B"));
        Cylinder("Lamp foot",new Vector3(-.55f,.94f,1.43f),new Vector3(.25f,.025f,.25f),Mat("Brass","#B68C4B"));
        Box("Desk lamp stem",new Vector3(-.55f,1.19f,1.43f),new Vector3(.035f,.5f,.035f),Mat("Brass","#B68C4B"),null,false);
        Box("Desk lamp shade",new Vector3(-.55f,1.46f,1.43f),new Vector3(.43f,.15f,.3f),Mat("LampJade","#53665B"),null,false);
        Box("Desk lamp glass",new Vector3(-.55f,1.38f,1.43f),new Vector3(.35f,.018f,.23f),Mat("LampGlow","#FFBB73",2),null,false);
        Point("Desk task light",new Vector3(-.55f,1.26f,1.43f),C("#FFBB77"),2.8f,3.4f,true);
        Cylinder("Ceiling lamp",new Vector3(0,2.8f,-.8f),new Vector3(.52f,.08f,.52f),Mat("LampJade","#53665B"));
        Point("Warm ceiling pool",new Vector3(0,2.65f,-.8f),C("#FFC58C"),3.4f,7,true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/ClockShop.unity");
    }
    static void Systems() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var app=new GameObject("Persistent systems").AddComponent<DemoApp>();app.font=font;app.dialTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"WatchFace.png");
        app.gameObject.AddComponent<TownSound>();
        var player=new GameObject("First person");player.transform.SetParent(app.transform);
        var cc=player.AddComponent<CharacterController>();cc.height=1.78f;cc.radius=.25f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.24f;cc.skinWidth=.03f;
        var eye=new GameObject("Player eye");eye.transform.SetParent(player.transform,false);eye.transform.localPosition=new Vector3(0,1.66f,0);eye.tag="MainCamera";
        var camera=eye.AddComponent<Camera>();camera.nearClipPlane=.08f;camera.farClipPlane=160;camera.fieldOfView=66;camera.allowHDR=true;
        var extra=camera.GetUniversalAdditionalCameraData();extra.renderPostProcessing=true;extra.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        eye.AddComponent<AudioListener>();
        var motor=player.AddComponent<PlayerMotor>();motor.eye=camera;app.player=motor;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Systems.unity");
    }
    public static void Build() {
        Directory.CreateDirectory("Assets/Generated/Materials");Directory.CreateDirectory("Assets/Scenes");AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");if(!font)throw new Exception("Font not imported");
        var ids=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if(ids.Length==0)throw new Exception("Official URP template pipeline not present");
        var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if(!pipe) throw new Exception("Desktop URP pipeline not present");
        GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;pipe.supportsHDR=true;pipe.msaaSampleCount=4;pipe.shadowDistance=55;
        pipe.mainLightShadowmapResolution=2048;EditorUtility.SetDirty(pipe);
        PlayerSettings.companyName="Qingchuan Studio";PlayerSettings.productName="错日证词 · 暮色小镇";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SplashScreen.show=false;PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        Town();Shop();Systems();AssetDatabase.SaveAssets();
        string[] scenes={"Assets/Scenes/Systems.unity","Assets/Scenes/Town.unity","Assets/Scenes/ClockShop.unity"};
        EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));
        string output=Path.GetFullPath("../builds/windows/TownDemo.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(scenes,output,BuildTarget.StandaloneWindows64,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Player build failed: "+report.summary.result);
        Debug.Log("SLICE_BUILD_PASS: "+output+" bytes="+report.summary.totalSize);
    }
}
