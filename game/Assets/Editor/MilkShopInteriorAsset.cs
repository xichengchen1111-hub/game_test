using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Additive detail kit. The original shell, counter, apertures and placement stay authoritative.
public static class MilkShopInteriorAsset {
    public const uint InteriorFillLayer=1u<<3;
    const string Folder="Assets/Art/MilkShopInteriorA/";
    const string Model=Folder+"MilkShopInteriorA.fbx";
    public static void AddTo(GameObject shop) {
        var importer=AssetImporter.GetAtPath(Model) as ModelImporter;
        if(!importer)throw new Exception("Generate MilkShopInteriorA.fbx with tools/milkshop/build_interior_a.py");
        if(importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importAnimation||!importer.isReadable) {
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=false;
            importer.importCameras=false;importer.importLights=false;importer.isReadable=true;importer.SaveAndReimport();
        }
        var detail=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
        detail.name="Milk shop A interior";detail.transform.SetParent(shop.transform,false);
        var nodes=detail.GetComponentsInChildren<Transform>();
        var front=nodes.Single(t=>t.name=="FrontMarker");var right=nodes.Single(t=>t.name=="RightMarker");var up=nodes.Single(t=>t.name=="UpMarker");
        var direction=shop.transform.InverseTransformDirection(front.position-detail.transform.position);direction.y=0;
        detail.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*detail.transform.localRotation;
        if(Vector3.Distance(shop.transform.InverseTransformPoint(front.position),Vector3.back)>.005f||
           Vector3.Distance(shop.transform.InverseTransformPoint(right.position),Vector3.right)>.005f||
           Vector3.Distance(shop.transform.InverseTransformPoint(up.position),Vector3.up)>.005f)
            throw new Exception("Milk interior FBX metre scale or axes mismatch");
        foreach(var renderer in detail.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>MaterialFor(m.name)).ToArray();
        Transform Part(string name)=>nodes.Single(t=>t.name==name);
        for(int side=-1;side<=1;side+=2) {
            var pendant=LightAt("Enamel pendant pool",shop.transform,new Vector3(side*1.05f,2.47f,.83f),3.0f,4.2f);
            DayNightLighting.Bind(pendant,DayNightLamp.Role.Interior,.88f,Part(side<0?"Pendant_Left":"Pendant_Right").GetComponent<Renderer>());
        }
        var task=LightAt("Counter reading pool",shop.transform,new Vector3(.21f,1.675f,1.105f),.75f,2.2f);
        var schedule=DayNightLighting.Bind(task,DayNightLamp.Role.Interior,.80f,Part("ReadingBulb").GetComponent<Renderer>());
        var book=HitTarget("Counter delivery ledger",shop.transform,new Vector3(-.14f,1.39f,.75f),new Vector3(.68f,.25f,.46f));
        var orders=book.AddComponent<MilkOrderBook>();orders.title="送奶记录";orders.source="牛奶屋柜台";
        var lamp=HitTarget("Reading lamp switch",shop.transform,new Vector3(.39f,1.46f,1.20f),new Vector3(.20f,.39f,.21f));
        var switcher=lamp.AddComponent<MilkShopFixture>();switcher.action=MilkShopFixture.Action.ReadingLamp;
        switcher.readingLight=task;switcher.readingBulb=Part("ReadingBulb").GetComponent<Renderer>();
        switcher.scheduledLamp=schedule;
        // Broad indirect room fill lifts faces below the hat without blowing out the counter.
        // Restrict it to this room and its actor so unshadowed light cannot leak onto the street.
        foreach(var renderer in shop.GetComponentsInChildren<Renderer>()) {
            renderer.renderingLayerMask|=InteriorFillLayer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
        foreach(var point in new[]{new Vector3(0,1.50f,-1.60f),new Vector3(2.40f,1.80f,-.10f)}) {
            var fill=LightAt("Milk room indirect fill",shop.transform,point,point.x==0?2.2f:.7f,4.3f);
            fill.color=new Color(1,.90f,.78f);
            fill.GetUniversalAdditionalLightData().renderingLayers=InteriorFillLayer;
            fill.renderingLayerMask=(int)InteriorFillLayer;
            DayNightLighting.Bind(fill,DayNightLamp.Role.Interior,.48f);
        }
        var bell=HitTarget("Counter service bell",shop.transform,new Vector3(-.63f,1.34f,.68f),new Vector3(.17f,.17f,.17f));
        var ring=bell.AddComponent<MilkShopFixture>();ring.action=MilkShopFixture.Action.Bell;ring.title="按响柜台铃";
        var audio=bell.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"ServiceBell.wav");
        audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=1;audio.volume=.35f;audio.minDistance=.5f;audio.maxDistance=5;
        ring.bellAudio=audio;
        Inspectable("Bottle paper tag",shop.transform,new Vector3(-1.20f,1.47f,.88f),new Vector3(.12f,.17f,.035f),"鲜奶瓶签","鲜奶\n\n玻璃瓶装。送达后收回空瓶，洗净再用。","柜台上的奶瓶纸签");
        Inspectable("Return milk churn",shop.transform,new Vector3(-2.5f,.69f,-1.75f),new Vector3(.52f,1.0f,.52f),"回收奶桶","清洗过的空奶桶放在门边，等待下一次装奶。\n桶口旁留着提手和封盖。","牛奶屋门边");
        var before=shop.GetComponent<MilkShopPlacement>().visualBounds;
        var after=MilkShopAsset.BoundsOf(shop);
        if(Vector3.Distance(before.min,after.min)>.005f||Vector3.Distance(before.max,after.max)>.005f)
            throw new Exception("A additions changed the milk shop exterior bounds");
        int triangles=detail.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
        if(triangles>16000)throw new Exception("Milk shop detail budget exceeded: "+triangles);
        var lights=shop.GetComponentsInChildren<Light>();
        if(lights.Length!=5||lights.Any(l=>l.shadows!=LightShadows.None))throw new Exception("Milk interior requires three fixture lights and two unshadowed room fills");
        var snapshot=UnityEngine.Object.Instantiate(detail);snapshot.transform.SetPositionAndRotation(Vector3.zero,detail.transform.localRotation);snapshot.transform.localScale=detail.transform.localScale;
        PrefabUtility.SaveAsPrefabAsset(snapshot,Folder+"MilkShopInteriorA.prefab");UnityEngine.Object.DestroyImmediate(snapshot);
        Directory.CreateDirectory("../output/demo/milkshop-interior-a");
        File.WriteAllText("../output/demo/milkshop-interior-a/import.txt","PASS: metre markers; original exterior bounds unchanged; "+triangles+" triangles; 3 fixture lights + 2 room-only unshadowed fills; 5 interaction targets.\n");
        Debug.Log("MILK_INTERIOR_IMPORT_PASS: "+triangles+" triangles, original bounds, five unshadowed lights");
    }
    static GameObject HitTarget(string name,Transform parent,Vector3 centre,Vector3 size) {
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);obj.transform.localPosition=centre;
        var box=obj.AddComponent<BoxCollider>();box.size=size;return obj;
    }
    static void Inspectable(string name,Transform parent,Vector3 centre,Vector3 size,string title,string body,string source) {
        var item=HitTarget(name,parent,centre,size).AddComponent<Interactable>();item.title=title;item.body=body;item.source=source;
    }
    static Light LightAt(string name,Transform parent,Vector3 local,float intensity,float range) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=local;
        var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.77f,.49f);light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;
        return light;
    }
    static Material MaterialFor(string imported) {
        string name=imported.Split('.')[0];string path=Folder+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        string hex=name=="Milk_Enamel"?"#DED2B1":name=="Milk_Brass"?"#9C743F":name=="Milk_Iron"?"#252E28":name=="Milk_Linen"?"#35463A":name=="Milk_Leaf"?"#455C2F":name=="Milk_Bulb"?"#FFDC9C":"#FFFFFF";
        ColorUtility.TryParseHtmlString(hex,out var tint);mat.SetColor("_BaseColor",tint);
        mat.SetFloat("_Metallic",name=="Milk_Brass"?.65f:name=="Milk_Iron"?.35f:name=="Milk_Enamel"?.08f:0);
        mat.SetFloat("_Smoothness",name=="Milk_Enamel"?.48f:name=="Milk_Brass"?.40f:.15f);
        mat.SetTexture("_BaseMap",name=="Milk_Ink"?AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"InkAtlas.png"):name=="Milk_Walnut"?AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/MilkShop1931/timber-basecolor.png"):null);
        if(name=="Milk_Bulb"){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",new Color(1,.65f,.25f)*2);}
        EditorUtility.SetDirty(mat);return mat;
    }
}
