using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class BuildSlice {
    static Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static Font font;
    static BuildingLayout shopLayout;
    const string ShopPrefabPath="Assets/Generated/ClockShop.prefab";
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
    static GameObject Milkboy(Vector3 pos,float yaw=0) => MilkboyAsset.Create(pos,yaw);
    static void Collider(GameObject obj,Vector3 center,Vector3 size) {var c=obj.AddComponent<BoxCollider>();c.center=center;c.size=size;}
    static Light Point(string name,Vector3 position,Color color,float intensity,float range,bool shadow=false) {
        var g=new GameObject(name);g.transform.position=position;
        var l=g.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;
        l.shadows=shadow?LightShadows.Soft:LightShadows.None;
        if(shadow) {
            // Two point lights need twelve cube faces. Request 512px per face
            // explicitly so they fit the 2048px atlas without automatic resizing.
            var settings=new SerializedObject(l.GetUniversalAdditionalLightData());
            settings.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        return l;
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
        string p="Assets/Generated/"+parent.name+"-roof.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
        else AssetDatabase.CreateAsset(mesh,p);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    static void Window(Transform p,float x,float y,float z,float width=1.1f,float height=1.55f,bool glow=true) {
        var trim=Mat("WindowTrim","#37323A");var glass=Mat(glow?"WarmWindow":"DarkWindow",glow?"#D4A878":"#3B4962",glow?.6f:0);
        Box("Window frame",new Vector3(x,y,z),new Vector3(width+.16f,height+.16f,.12f),trim,p,false);
        Box("Glass",new Vector3(x,y,z-.07f),new Vector3(width,height,.045f),glass,p,false);
        Box("Mullion",new Vector3(x,y,z-.105f),new Vector3(.055f,height,.035f),trim,p,false);
        Box("Crossbar",new Vector3(x,y,z-.105f),new Vector3(width,.055f,.035f),trim,p,false);
        Box("Stone sill",new Vector3(x,y-height/2-.13f,z-.08f),new Vector3(width+.28f,.14f,.24f),Mat("Sill","#8A7F77"),p,false);
    }
    static Transform Building(string name,Vector3 pos,float yaw,float width,float height,string color,string sign=null,bool groundWindows=true) {
        var root=Group(name,pos).transform;root.localRotation=Quaternion.Euler(0,yaw,0);
        Box("Plaster",new Vector3(0,height/2,0),new Vector3(width,height,6.4f),Mat(name+"-plaster",color),root);
        Box("Foundation",new Vector3(0,.22f,-3.25f),new Vector3(width+.3f,.44f,.3f),Mat("Foundation","#5D5A64"),root);
        Box("Cornice",new Vector3(0,height-.1f,-3.22f),new Vector3(width+.4f,.25f,.5f),Mat("Cornice","#736D72"),root,false);
        for(int floor=0;floor<2;floor++)for(int x=0;x<3;x++)if(floor>0||groundWindows)Window(root,(x-1)*width/3.6f,2.0f+floor*2.8f,-3.23f,1.25f,floor==0?1.7f:1.5f,(x+floor)%3!=0);
        Roof(root,width+.45f,7,height,1.6f,Mat("Roof","#3B4255"));
        Box("Chimney",new Vector3(width/3,height+1.0f,1.5f),new Vector3(.65f,1.7f,.7f),Mat("Brick","#67565A"),root,false);
        if(sign!=null) {
            Box("Shop sign",new Vector3(0,3.7f,-3.34f),new Vector3(width-.6f,.9f,.16f),Mat("SignWood","#283F40"),root,false);
            Quad("Lettered sign",new Vector3(0,3.7f,-3.435f),new Vector2(width-.7f,.85f),ImageMat("sign-"+sign,sign),root);
            Porch(root,width,-3.2f);
        }
        return root;
    }
    static void Porch(Transform building,float width,float frontWall) {
        var root=Group("Porch",Vector3.zero,building).transform;
        const float canopyY=3.05f,canopyThickness=.15f,beamHeight=.14f,jointOverlap=.01f;
        float underside=canopyY-canopyThickness/2;
        float beamTop=underside+jointOverlap;
        float beamBottom=beamTop-beamHeight;
        float postHeight=beamBottom+jointOverlap;
        float postZ=frontWall-1.3f;
        var timber=Mat("DarkTimber","#3B3031");
        Box("Awning",new Vector3(0,canopyY,frontWall-.875f),new Vector3(width+.4f,canopyThickness,1.3f),Mat("Awning","#445A54"),root,false);
        Box("Canopy support beam",new Vector3(0,(beamTop+beamBottom)/2,postZ),new Vector3(width+.30f,beamHeight,.20f),timber,root,false);
        for(int side=-1;side<=1;side+=2)
            Box("Shop post",new Vector3(side*(width/2+.05f),postHeight/2,postZ),new Vector3(.17f,postHeight,.17f),timber,root);
    }
    static void FaceBenchTowardSquare(GameObject bench) {
        // Infer the backrest from the imported geometry rather than assuming an
        // FBX forward axis. Above seat height, the backrest dominates the depth.
        float backDepth=0;int vertices=0;
        foreach(var filter in bench.GetComponentsInChildren<MeshFilter>())
            foreach(var vertex in filter.sharedMesh.vertices) {
                var local=bench.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if(local.y<.55f)continue;
                backDepth+=local.z;vertices++;
            }
        if(vertices==0||Mathf.Abs(backDepth/vertices)<.05f)throw new Exception("Cannot identify the imported bench backrest");
        var facing=Group("Seat facing",Vector3.zero,bench.transform).transform;
        facing.localRotation=Quaternion.Euler(0,backDepth>0?180:0,0);
        bench.transform.rotation=Quaternion.FromToRotation(facing.forward,Vector3.right);
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
        Point("Lantern pool",pos+Vector3.up*2.95f,C("#FFBD78"),14f,7.2f);
    }
    static void Town() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(false);
        var atmosphere=new HashSet<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
        Box("Town ground",new Vector3(0,-.24f,4),new Vector3(55,.4f,65),Mat("Ground","#60637B",0,.38f));
        var station=Building("Station",new Vector3(-9.2f,0,1),-90,8,6.5f,"#B3A89A","SignStation.png");
        var shop=InstantiateShop(false);
        var westSidewalk=StreetSurfaces.Build(shop.transform);
        Building("GuestHouse",new Vector3(-10,0,14),-90,8,7.5f,"#929396","SignInn.png");
        Building("Home",new Vector3(10.2f,0,15),90,7.4f,6.3f,"#B1A398");
        Building("BackRow",new Vector3(0,0,28),0,12,7,"#84899B");
        Building("EastBackground",new Vector3(17,0,31),0,10,9,"#787D96");
        Building("WestBackground",new Vector3(-17,0,30),0,10,8,"#8A8B9A");
        InstantiateClockTower();
        foreach(int lanternSide in new[]{-1,1})foreach(float z in new[]{-8f,6f,19f})
            Lantern(new Vector3(lanternSide*(z<6.5f?3.62f:5.7f),.1f,z));
        var bench=Model("Bench",new Vector3(-7.55f,westSidewalk.GetComponent<Renderer>().bounds.max.y,7.5f));
        FaceBenchTowardSquare(bench);Collider(bench,new Vector3(0,.45f,0),new Vector3(2,.9f,.75f));
        var cart=Model("MilkCart",new Vector3(6.35f,.10f,-5.4f),-90);Collider(cart,new Vector3(0,.6f,0),new Vector3(1.6f,1.15f,.9f));
        var boy=Milkboy(new Vector3(6.48f,.1f,-4.2f),95);boy.transform.localScale=Vector3.one*.88f;
        Collider(boy,new Vector3(0,.8f,0),new Vector3(.65f,1.65f,.55f));
        var testimony=DialogueScript.Load("MilkboyDialogue").evidence;
        var milk=boy.AddComponent<Interactable>();milk.title="送奶少年";milk.evidenceId=testimony.id;milk.source=testimony.source;milk.body=testimony.body;
        MilkShopAsset.Create(boy);
        for(int i=0;i<4;i++)Model("Plant",new Vector3(i%2==0?-6.5f:6.5f,.12f,i<2?-1.8f:11.5f));
        for(int i=0;i<7;i++) {
            var shrub=GameObject.CreatePrimitive(PrimitiveType.Sphere);shrub.name="Clipped shrub";shrub.transform.position=new Vector3(Mathf.Cos(i*Mathf.PI/6)*2.8f,.48f,10+Mathf.Sin(i*Mathf.PI/6)*2.8f);
            shrub.transform.localScale=new Vector3(1.05f,.9f,1.05f);shrub.GetComponent<Renderer>().sharedMaterial=Mat("Foliage","#58685F");UnityEngine.Object.DestroyImmediate(shrub.GetComponent<Collider>());
        }
        Box("South town boundary",new Vector3(0,1,-23),new Vector3(55,3,.6f),Mat("Boundary","#64677B"));
        Box("North town boundary",new Vector3(0,1,32),new Vector3(55,3,.6f),Mat("Boundary","#64677B"));
        Box("West town boundary",new Vector3(-17,1,2),new Vector3(.6f,3,60),Mat("Boundary","#64677B"));
        Box("East town boundary",new Vector3(17,1,2),new Vector3(.6f,3,60),Mat("Boundary","#64677B"));
        var geometry=Group("Town geometry",Vector3.zero);
        foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if(root!=geometry&&!atmosphere.Contains(root))root.transform.SetParent(geometry.transform,true);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Town.unity");
    }
    static void SharedShell(Transform building,Material wallMaterial) {
        foreach(BuildingWall side in Enum.GetValues(typeof(BuildingWall))) {
            var wall=Group("Wall "+side,shopLayout.WallCentre(side),building).transform;
            wall.localRotation=shopLayout.WallRotation(side);
            float cursor=-shopLayout.WallSpan(side)/2,end=-cursor;
            foreach(var opening in shopLayout.openings.Where(o=>o.wall==side).OrderBy(o=>o.offset)) {
                float left=opening.offset-opening.size.x/2,right=opening.offset+opening.size.x/2;
                float bottom=opening.centreHeight-opening.size.y/2,top=opening.centreHeight+opening.size.y/2;
                WallPiece(wall,cursor,left,0,shopLayout.wallHeight,wallMaterial,"Pier");
                WallPiece(wall,left,right,0,bottom,wallMaterial,"Below "+opening.id);
                WallPiece(wall,left,right,top,shopLayout.wallHeight,wallMaterial,"Above "+opening.id);
                cursor=right;
            }
            WallPiece(wall,cursor,end,0,shopLayout.wallHeight,wallMaterial,"Pier");
        }
    }
    static void WallPiece(Transform wall,float left,float right,float bottom,float top,Material material,string name) {
        if(right-left<.001f||top-bottom<.001f)return;
        Box(name,new Vector3((left+right)/2,(bottom+top)/2,0),new Vector3(right-left,top-bottom,shopLayout.wallThickness),material,wall);
    }
    static void TrimStreetFloor(Transform shop) {
        // Carve the actual shop footprint out of paving and the sidewalk in the
        // source town, so both scenes show the same street outside the windows.
        var floor=shop.Find("Interior floor").GetComponent<Renderer>().bounds;
        foreach(var tile in SceneManager.GetActiveScene().GetRootGameObjects()) {
            if(tile.name!="Paving stone"&&tile.name!="East sidewalk")continue;
            var b=tile.GetComponent<Renderer>().bounds;
            if(b.max.x<=floor.min.x||b.min.x>=floor.max.x||b.max.z<=floor.min.z||b.min.z>=floor.max.z)continue;
            float middleLeft=Mathf.Max(b.min.x,floor.min.x),middleRight=Mathf.Min(b.max.x,floor.max.x);
            FloorPiece(tile,b.min.x,Mathf.Min(b.max.x,floor.min.x),b.min.z,b.max.z);
            FloorPiece(tile,Mathf.Max(b.min.x,floor.max.x),b.max.x,b.min.z,b.max.z);
            FloorPiece(tile,middleLeft,middleRight,b.min.z,Mathf.Min(b.max.z,floor.min.z));
            FloorPiece(tile,middleLeft,middleRight,Mathf.Max(b.min.z,floor.max.z),b.max.z);
            UnityEngine.Object.DestroyImmediate(tile);
        }
    }
    static void FloorPiece(GameObject source,float x0,float x1,float z0,float z1) {
        if(x1-x0<.001f||z1-z0<.001f)return;
        var tile=UnityEngine.Object.Instantiate(source);tile.name=source.name;
        tile.transform.position=new Vector3((x0+x1)/2,source.transform.position.y,(z0+z1)/2);
        tile.transform.localScale=new Vector3(x1-x0,source.transform.localScale.y,z1-z0);
    }
    static void WindowStreet() {
        // Reuse the saved town meshes, rather than maintaining a second invented street.
        // The building layout defines the complete town/interior coordinate transform.
        var interior=SceneManager.GetActiveScene();
        var town=EditorSceneManager.OpenScene("Assets/Scenes/Town.unity",OpenSceneMode.Additive);
        var source=Array.Find(town.GetRootGameObjects(),g=>g.name=="Town geometry");
        if(!source)throw new Exception("Town geometry missing for real window view");
        SceneManager.SetActiveScene(interior);
        var street=UnityEngine.Object.Instantiate(source);street.name="Window street geometry";
        SceneManager.MoveGameObjectToScene(street,interior);
        EditorSceneManager.CloseScene(town,true);
        var ownShop=street.transform.Find("ClockShop");
        if(!ownShop)throw new Exception("Shared shop missing from town");
        // The interior already contains the entire same prefab, including its roof
        // and canopy. Remove the copy completely, avoiding duplicate surfaces.
        UnityEngine.Object.DestroyImmediate(ownShop.gameObject);
        foreach(var component in street.GetComponentsInChildren<Interactable>())UnityEngine.Object.DestroyImmediate(component);
        foreach(var component in street.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(component);
        var rotation=Quaternion.Inverse(shopLayout.TownRotation);
        street.transform.SetPositionAndRotation(rotation*-shopLayout.townPosition,rotation);
    }
    static void ClearWindow(Transform parent,float width,float height) {
        var wood=ShopMaterial("ShopGreen");
        // Let the timber overlap the aperture by 5 cm. Its inner faces must not
        // coincide with the plaster returns: those coplanar faces flicker on movement.
        float innerWidth=width-.10f,innerHeight=height-.10f;
        // Protrude beyond both wall faces, including the exterior face now visible
        // on the shared prefab; the previous 24 cm frame was coplanar outside.
        float frameDepth=shopLayout.wallThickness+.10f;
        for(int side=-1;side<=1;side+=2) {
            Box("Window jamb",new Vector3(side*width/2,0,0),new Vector3(.10f,height+.10f,frameDepth),wood,parent,false);
            Box("Window rail",new Vector3(0,side*height/2,0),new Vector3(innerWidth,.10f,frameDepth),wood,parent,false);
        }
        Box("Window transom",new Vector3(0,height/2-.34f,0),new Vector3(innerWidth,.045f,.12f),wood,parent,false);
        Box("Stone sill",new Vector3(0,-height/2-.07f,.07f),new Vector3(width+.32f,.12f,.42f),Mat("Sill","#8A7F77"),parent,false);
        string path="Assets/Generated/Materials/ClearWindowGlass.mat";
        var glass=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!glass){glass=new Material(Shader.Find("WrongDate/ClearGlass"));AssetDatabase.CreateAsset(glass,path);}
        glass.SetColor("_Tint",new Color(.55f,.68f,.78f,.055f));EditorUtility.SetDirty(glass);
        Quad("Transparent window glass",new Vector3(0,0,-.025f),new Vector2(width,height),glass,parent);
        // Keep the player inside without intercepting ordinary inspection rays.
        var blocker=Group("Window safety collision",Vector3.zero,parent);blocker.layer=2;
        Collider(blocker,Vector3.zero,new Vector3(width,height,.08f));
    }
    static BuildingInstance InstantiateShop(bool inside) {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(ShopPrefabPath);
        var instance=((GameObject)PrefabUtility.InstantiatePrefab(prefab)).GetComponent<BuildingInstance>();
        instance.interior=inside;
        if(!inside) {
            instance.transform.SetPositionAndRotation(shopLayout.townPosition,shopLayout.TownRotation);
        }
        return instance;
    }
    static void Shop() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SetupLight(true);
        InstantiateShop(true);WindowStreet();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/ClockShop.unity");
    }
    static void Systems() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var app=new GameObject("Persistent systems").AddComponent<DemoApp>();app.font=font;app.dialTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"WatchFace.png");
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        app.reviewAmbientOcclusion=renderer.rendererFeatures.Find(f=>f is ScreenSpaceAmbientOcclusion);
        app.gameObject.AddComponent<TownSound>();
        var player=new GameObject("First person");player.transform.SetParent(app.transform);
        // Looking down at a nearby handle must not hit our own shoulder capsule.
        // Ignore Raycast still participates in physical collision / door sweep checks.
        player.layer=2;
        var cc=player.AddComponent<CharacterController>();cc.height=1.78f;cc.radius=.25f;cc.center=new Vector3(0,.9f,0);cc.stepOffset=.24f;cc.skinWidth=.03f;
        var eye=new GameObject("Player eye");eye.transform.SetParent(player.transform,false);eye.transform.localPosition=new Vector3(0,1.66f,0);eye.tag="MainCamera";
        var camera=eye.AddComponent<Camera>();camera.nearClipPlane=.08f;camera.farClipPlane=160;camera.fieldOfView=66;camera.allowHDR=true;
        var extra=camera.GetUniversalAdditionalCameraData();extra.renderPostProcessing=true;extra.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
        eye.AddComponent<AudioListener>();
        var motor=player.AddComponent<PlayerMotor>();motor.eye=camera;app.player=motor;
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),"Assets/Scenes/Systems.unity");
    }
    public static void Build() {
        BuildFor(BuildTarget.StandaloneWindows64,"../builds/windows/TownDemo.exe",new[]{GraphicsDeviceType.Direct3D11},null);
    }
    public static void BuildMac() {
        BuildFor(BuildTarget.StandaloneOSX,"../builds/macos-arm64/TownDemo.app",new[]{GraphicsDeviceType.Metal},1);
    }
    public static void BuildFreshMac() {
        string originalCompany=PlayerSettings.companyName;
        string originalIdentifier=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        try {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.qingchuanstudio.wrongdatetest.currentmac");
            BuildFor(BuildTarget.StandaloneOSX,"../builds/macos-current/错日证词-新版.app",new[]{GraphicsDeviceType.Metal},1,"Qingchuan Fresh 20261008");
        } finally {
            PlayerSettings.companyName=originalCompany;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,originalIdentifier);
            AssetDatabase.SaveAssets();
        }
    }
    public static void BuildMilkboyMacCopy() {
        string originalCompany=PlayerSettings.companyName;
        try {
            BuildFor(BuildTarget.StandaloneOSX,"../builds/macos-arm64/TownDemo-Milkboy-Rebuilt.app",new[]{GraphicsDeviceType.Metal},1,"Qingchuan Milkboy Rebuilt");
        } finally {
            PlayerSettings.companyName=originalCompany;
        }
    }
    static void BuildFor(BuildTarget target,string relativeOutput,GraphicsDeviceType[] graphicsApis,int? architecture,string companyOverride=null) {
        Directory.CreateDirectory("Assets/Generated/Materials");Directory.CreateDirectory("Assets/Scenes");AssetDatabase.Refresh();
        font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");if(!font)throw new Exception("Font not imported");
        var ids=AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        if(ids.Length==0)throw new Exception("Official URP template pipeline not present");
        var pipe=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if(!pipe) throw new Exception("Desktop URP pipeline not present");
        GraphicsSettings.defaultRenderPipeline=pipe;QualitySettings.renderPipeline=pipe;pipe.supportsHDR=true;pipe.msaaSampleCount=4;pipe.shadowDistance=55;
        pipe.mainLightShadowmapResolution=2048;EditorUtility.SetDirty(pipe);
        PlayerSettings.companyName=String.IsNullOrEmpty(companyOverride)?"Qingchuan Studio":companyOverride;PlayerSettings.productName="错日证词 · 暮色小镇";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SplashScreen.show=false;PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        if(architecture.HasValue)PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,architecture.Value);
        PlayerSettings.SetGraphicsAPIs(target,graphicsApis);
        string layoutPath="Assets/Generated/ClockShopLayout.asset";
        shopLayout=AssetDatabase.LoadAssetAtPath<BuildingLayout>(layoutPath);
        if(!shopLayout){shopLayout=ScriptableObject.CreateInstance<BuildingLayout>();AssetDatabase.CreateAsset(shopLayout,layoutPath);}
        ConfigureWalnutLayout();
        BuildingLayoutChecks.ValidateDefinition(shopLayout);
        WalnutShopPrefab();ClockTowerPrefab();Town();Shop();BuildingLayoutChecks.ValidateScenes(shopLayout,ShopPrefabPath);Systems();AssetDatabase.SaveAssets();
        string[] scenes={"Assets/Scenes/Systems.unity","Assets/Scenes/Town.unity","Assets/Scenes/ClockShop.unity"};
        EditorBuildSettings.scenes=Array.ConvertAll(scenes,p=>new EditorBuildSettingsScene(p,true));
        string output=Path.GetFullPath(relativeOutput);Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report=BuildPipeline.BuildPlayer(scenes,output,target,BuildOptions.None);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Player build failed: "+report.summary.result);
        Debug.Log("SLICE_BUILD_PASS: "+output+" bytes="+report.summary.totalSize);
    }
}
