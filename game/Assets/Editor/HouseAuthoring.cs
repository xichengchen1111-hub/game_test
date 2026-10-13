using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
[Serializable] public class HomeBox {public string name;public float[] centre,size;public float yaw;}
[Serializable] public class HomeDoor {public string id,node;public float[] pivot;public float yaw,width,height;public bool inside;}
[Serializable] public class HomeCabinet {public string id,title;public float[] position,size,offset;public float yaw;public string[] nodes,items;public HomePaper[] evidence;public float height;public bool safe,hinged;}
[Serializable] public class HomePaper {public string id,node;public float[] position;public float width,depth;public bool upright;}
[Serializable] public class HomeLoose {public string id;public float[] position;}
[Serializable] public class HomePlan {public float[] origin;public HomeBox[] colliders,openings;public HomeDoor[] doors;public HomeCabinet[] cabinets;public HomeLoose[] loose;public float[][] lights;public int rooms,storeys,objects,triangles;}
[Serializable] public class HomeRecords {public NotebookEntry[] entries;}
public static class HouseAuthoring {
    public static void CheckOnly(){Create();}
    const string Dir="Assets/Art/House1931/";
    const uint HouseLayer=1u<<4;
    static Vector3 V(float[] p)=>new Vector3(p[0],p[1],p[2]);
    static GameObject Node(string name,Transform parent,Vector3 position){var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=position;return g;}
    public static GameObject Create(){
        var plan=JsonUtility.FromJson<HomePlan>(File.ReadAllText(Dir+"Layout.json"));
        var imp=(ModelImporter)AssetImporter.GetAtPath(Dir+"House1931.fbx");
        if(imp.importAnimation||!imp.isReadable||imp.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard){imp.importAnimation=false;imp.isReadable=true;imp.importCameras=false;imp.importLights=false;imp.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;imp.SaveAndReimport();}
        var root=new GameObject("主角住宅");root.AddComponent<HouseInstance>();
        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Dir+"House1931.fbx"));PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);model.transform.SetParent(root.transform,false);
        var nodes=model.GetComponentsInChildren<Transform>();var front=nodes.Single(t=>t.name=="FrontMarker");var right=nodes.Single(t=>t.name=="RightMarker");var up=nodes.Single(t=>t.name=="UpMarker");
        var dir=front.position-model.transform.position;dir.y=0;model.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(dir,Vector3.back,Vector3.up),Vector3.up)*model.transform.localRotation;
        model.transform.localScale=new Vector3(-1,1,1);
        if(Vector3.Distance(right.position,Vector3.right)>.005f||Vector3.Distance(up.position,Vector3.up)>.005f)throw new Exception("Home FBX metre/axis mismatch: "+right.position+" "+up.position);
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterials=r.sharedMaterials.Select(m=>Material(m.name.Split('.')[0].Replace("House_",""))).ToArray();r.renderingLayerMask|=HouseLayer;r.gameObject.isStatic=!r.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Drawer_")||t.name.StartsWith("Door_"));}
        foreach(var b in plan.colliders){var g=Node(b.name+" collision",root.transform,V(b.centre));g.transform.localRotation=Quaternion.Euler(0,b.yaw,0);g.AddComponent<BoxCollider>().size=V(b.size);}
        // Give tall/open furniture and the stair barrier their actual physical obstruction.
        foreach(var name in new[]{"Bookcase","Pedestal basin"}){var group=nodes.Single(t=>t.name==name);var rs=group.GetComponentsInChildren<MeshRenderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);var go=Node(name+" collision",root.transform,b.center);go.AddComponent<BoxCollider>().size=b.size;}
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Fire surround")||r.name=="Mantel"||r.name=="Hearth")){var go=Node(r.name+" collision",root.transform,r.bounds.center);go.AddComponent<BoxCollider>().size=r.bounds.size;}
        foreach(var r in model.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Stair closed string")||r.name.StartsWith("Stair seated baluster")||r.name.StartsWith("Stair handrail")||r.name.StartsWith("Landing baluster"))){var c=r.gameObject.AddComponent<MeshCollider>();c.sharedMesh=r.GetComponent<MeshFilter>().sharedMesh;}
        foreach(var d in plan.doors){
            var frame=Node("Mechanism "+d.id,root.transform,V(d.pivot));frame.transform.localRotation=Quaternion.Euler(0,d.yaw,0);
            var visual=nodes.Single(t=>t.name==d.node);visual.SetParent(frame.transform,true);
            var col=frame.AddComponent<BoxCollider>();col.center=new Vector3(d.width/2,d.height/2,0);col.size=new Vector3(d.width-.12f,d.height-.07f,.085f);
            var mech=frame.AddComponent<ShopMechanism>();mech.fixtureId="home-door-"+d.id;mech.movingPart=frame.transform;mech.openEuler=new Vector3(0,d.id=="west"?85:-85,0);mech.protectPlayer=true;mech.startsOpen=d.inside||d.id=="east";mech.leafCentre=col.center;mech.leafSize=col.size;
        }
        foreach(var window in nodes.Where(t=>plan.openings.Any(w=>w.name==t.name))){
            var renderers=window.GetComponentsInChildren<MeshRenderer>();var rail=renderers.First(r=>r.name.Contains("curtain rail"));var glass=renderers.First(r=>r.name.Contains("glazing"));
            foreach(var fold in renderers.Where(r=>r.name.Contains("curtain fold"))){var b=fold.bounds;float top=rail.bounds.center.y-.012f;var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Attached curtain heading";g.transform.SetParent(root.transform,false);g.transform.position=new Vector3(b.center.x,(b.max.y+top)/2,b.center.z);g.transform.localScale=new Vector3(b.size.x,top-b.max.y+.005f,b.size.z);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var rr=g.GetComponent<MeshRenderer>();rr.sharedMaterial=Material("Fabric");rr.renderingLayerMask|=HouseLayer;}
            Vector3 along=rail.bounds.size.x>rail.bounds.size.z?Vector3.right:Vector3.forward;float width=Mathf.Max(rail.bounds.size.x,rail.bounds.size.z);Vector3 intoWall=glass.bounds.center-rail.bounds.center;intoWall.y=0;
            foreach(float sign in new[]{-1f,1f}){var tip=rail.bounds.center+along*(width*.43f)*sign;SupportRod("Curtain wall bracket",root.transform,tip,tip+intoWall*.60f,.018f,"Brass");}
        }
        var records=JsonUtility.FromJson<HomeRecords>(File.ReadAllText("Assets/Resources/HouseEvidence.json")).entries;
        Interactable Paper(string id,Transform parent){var e=records.Single(e=>e.id==id);var o=Node("Record "+id,parent,Vector3.zero);var i=o.AddComponent<Interactable>();i.evidenceId=e.id;i.title=e.title;i.source=e.source;i.body=e.body;
            // Reading the ticket still uses the existing portable original and illustration.
            string paperPath=Dir+id+".png";var paperImport=AssetImporter.GetAtPath(paperPath) as TextureImporter;
            if(paperImport&&(paperImport.npotScale!=TextureImporterNPOTScale.None||paperImport.mipmapEnabled||paperImport.textureCompression!=TextureImporterCompression.Uncompressed)){paperImport.npotScale=TextureImporterNPOTScale.None;paperImport.mipmapEnabled=false;paperImport.textureCompression=TextureImporterCompression.Uncompressed;paperImport.maxTextureSize=2048;paperImport.SaveAndReimport();}
            i.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(paperPath);return i;}
        foreach(var c in plan.cabinets){
            var g=Node("Search "+c.id,root.transform,V(c.position));g.transform.localRotation=Quaternion.Euler(0,c.yaw,0);var col=g.AddComponent<BoxCollider>();col.size=new Vector3(c.size[0],.08f,c.size[2]);col.isTrigger=true;
            var item=g.AddComponent<HouseContainer>();item.fixtureId="home-cabinet-"+c.id;item.title=c.safe?"旧铁保险柜":CabinetTitle(c.id);item.source="主角住宅 · "+item.title;item.safe=c.safe;item.hinged=c.hinged;
            item.body=Everyday(c.id);
            if(c.safe)foreach(var r in nodes.Single(t=>t.name=="Cabinet_"+c.id).GetComponentsInChildren<MeshRenderer>().Where(r=>(r.name.StartsWith("Stored ")||r.name.StartsWith("Parcel cotton"))&&r.bounds.center.y<.9f))r.transform.position+=Vector3.forward*.33f;
            // World-aligned pivots isolate moving leaves from the imported mirrored hierarchy.
            item.drawers=c.nodes.Select(n=>{var old=nodes.Single(t=>t.name==n);var pivot=Node("Storage pivot "+n,root.transform,old.position);old.SetParent(pivot.transform,true);
                foreach(var r in old.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Cupboard framed leaf")||r.name.StartsWith("Drawer front")||r.name.StartsWith("Drawer side")||r.name.StartsWith("Drawer bottom")||r.name.StartsWith("Drawer rear"))){var block=Node("Moving storage collision",pivot.transform,pivot.transform.InverseTransformPoint(r.bounds.center));block.AddComponent<BoxCollider>().size=r.bounds.size;}
                return pivot.transform;}).ToArray();item.closedPositions=item.drawers.Select(t=>t.localPosition).ToArray();item.offset=V(c.offset);
            if(c.items.Length==0&&!c.hinged&&!c.safe){var top=nodes.Single(t=>t.name=="Cabinet_"+c.id).GetComponentsInChildren<MeshRenderer>().First(r=>r.name.StartsWith("Cabinet top")).bounds;float depth=(Mathf.Abs(Mathf.Cos(c.yaw*Mathf.Deg2Rad))>.5f?top.size.z:top.size.x)-.08f;
                var towardsOpening=Quaternion.Euler(0,c.yaw,0)*Vector3.back*depth*.47f;
                foreach(var drawer in item.drawers)foreach(var r in drawer.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Stored ")||r.name.StartsWith("Parcel cotton")))r.transform.position+=towardsOpening;
            }
            foreach(var drawer in item.drawers)foreach(var pull in drawer.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("Attached drawer pull")).ToArray()){
                var along=Quaternion.Euler(0,c.yaw,0)*Vector3.right;var back=Quaternion.Euler(0,c.yaw,0)*Vector3.forward;
                foreach(float sign in new[]{-1f,1f})SupportRod("Handle mounting stem",drawer,pull.bounds.center+along*.055f*sign,pull.bounds.center+along*.055f*sign+back*.029f,.009f,"Brass");
            }
            item.contents=c.evidence.Select(e=>{var i=Paper(e.id,g.transform);i.transform.position=V(e.position);i.transform.rotation=Quaternion.Euler(e.upright?0:90,c.yaw,0);
                var visual=EvidenceModel.Paper("Physical "+e.id,i.illustration,e.width,e.depth);visual.transform.SetParent(i.transform,false);visual.AddComponent<EvidencePhysical>();StorePaper(visual,i.evidenceId);
                if(!c.hinged&&!c.safe){float lift=(c.height-.20f)/3*.70f;i.transform.position+=Vector3.up*lift;
                    LocalBox("Document storage box",i.transform,new Vector3(0,0,lift/2+.006f),new Vector3(e.width+.018f,e.depth+.012f,lift),"Paper");
                    LocalBox("Box lid rim",i.transform,new Vector3(0,0,.010f),new Vector3(e.width+.022f,e.depth+.016f,.008f),"Cream");
                }
                if(e.upright){LocalBox("Standing folder backing",i.transform,new Vector3(0,0,.01f),new Vector3(e.width+.012f,e.depth,.016f),"Walnut");LocalBox("File rack foot",i.transform,new Vector3(0,-e.depth/2,.02f),new Vector3(e.width+.024f,.010f,.08f),"Walnut");LocalBox("Document clip",i.transform,new Vector3(0,e.depth/2-.01f,-.003f),new Vector3(.025f,.018f,.012f),"Brass");}
                var target=i.gameObject.AddComponent<BoxCollider>();target.size=new Vector3(e.width,e.depth,.014f);target.isTrigger=true;
                if(!string.IsNullOrEmpty(e.node)){int index=System.Array.IndexOf(c.nodes,e.node);i.transform.SetParent(item.drawers[index],true);}
                return i;}).ToArray();
        }
        foreach(var p in plan.loose){var i=Paper(p.id,root.transform);i.transform.localPosition=V(p.position);
            if(p.id!="H17"&&p.id!="H18") {i.transform.localRotation=Quaternion.Euler(90,0,0);var visual=EvidenceModel.Paper("Physical "+p.id,i.illustration,.36f,.25f);visual.transform.SetParent(i.transform,false);visual.AddComponent<EvidencePhysical>();StorePaper(visual,i.evidenceId);var box=i.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(.36f,.25f,.015f);box.isTrigger=true;}
            else {var box=i.gameObject.AddComponent<BoxCollider>();box.size=p.id=="H17"?new Vector3(.17f,.6f,.3f):new Vector3(.44f,.66f,.25f);box.isTrigger=true;}
        }
        // Hall table inspects the same portable ticket, never a second collectible original.
        var ticket=Node("Ticket comparison table",root.transform,new Vector3(5.8f,1.06f,-.65f)).AddComponent<HomeTicket>();ticket.title="摊开随身车票";ticket.gameObject.AddComponent<BoxCollider>().size=new Vector3(.45f,.18f,.4f);
        // Manifest uses nested numeric arrays, which JsonUtility cannot deserialize. Author fixture positions explicitly from original named mesh parents.
        foreach(var t in nodes.Where(t=>t.name.StartsWith("Pendant_"))){var lamp=Node("Home warm fixture",root.transform,root.transform.InverseTransformPoint(t.GetComponentsInChildren<MeshRenderer>().First(v=>v.name.StartsWith("Warm bulb")).bounds.center)).AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.85f,.64f);lamp.intensity=2.0f;lamp.range=5;lamp.shadows=LightShadows.None;lamp.GetUniversalAdditionalLightData().renderingLayers=HouseLayer;lamp.renderingLayerMask=(int)HouseLayer;DayNightLighting.Bind(lamp,DayNightLamp.Role.Interior,.45f);}
        // Local room fills stay inside the house rendering layer, with no extra shadow lights.
        foreach(var p in new[]{new Vector3(-4,1.65f,-3),new Vector3(4,1.65f,-3),new Vector3(0,1.8f,3),new Vector3(-5,1.8f,3),new Vector3(-4,4.9f,-3),new Vector3(4,4.9f,-3),new Vector3(-5,4.9f,3),new Vector3(0,4.9f,3),new Vector3(-5,1.8f,10)}){
            var light=Node("Home indirect room fill",root.transform,p).AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.90f,.88f,.8f);light.intensity=1.3f;light.range=4.6f;light.shadows=LightShadows.None;light.GetUniversalAdditionalLightData().renderingLayers=HouseLayer;light.renderingLayerMask=(int)HouseLayer;DayNightLighting.Bind(light,DayNightLamp.Role.Interior,.4f);
        }
        WeatherAuthoring.Shelter(root,new Vector2(16.3f,12.3f),6.65f,1.8f);WeatherAuthoring.Shelter(root,new Vector2(5.5f,7.3f),3.24f,.55f,new Vector2(-5.36f,9.5f));WeatherAuthoring.Shelter(root,new Vector2(1.9f,2.8f),2.88f,0,new Vector2(8.65f,0));
        foreach(var roof in root.GetComponents<WeatherShelter>())roof.exteriorDoors=root.GetComponentsInChildren<ShopMechanism>().Where(d=>d.fixtureId=="home-door-east"||d.fixtureId=="home-door-west").ToArray();
        AddGuard(root.transform);GateLamp(root.transform);
        BuildingLayoutChecks.ValidateHouse(root,plan);
        HousePlacementChecks.Check(root);
        PrefabUtility.SaveAsPrefabAsset(root,Dir+"House1931.prefab");root.transform.position=V(plan.origin);return root;
    }
    static void LocalBox(string name,Transform parent,Vector3 point,Vector3 size,string material){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=point;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=Material(material);r.renderingLayerMask|=HouseLayer;}
    static void SupportRod(string name,Transform parent,Vector3 a,Vector3 b,float radius,string material){var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,true);g.transform.position=(a+b)/2;g.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);g.transform.localScale=new Vector3(radius*2,(b-a).magnitude/2,radius*2);UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=Material(material);r.renderingLayerMask|=HouseLayer;}
    static void StorePaper(GameObject root,string id){
        int n=0;foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
            string path=Dir+"Paper_"+id+"_"+n+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh){EditorUtility.CopySerialized(f.sharedMesh,mesh);UnityEngine.Object.DestroyImmediate(f.sharedMesh);}else{mesh=f.sharedMesh;AssetDatabase.CreateAsset(mesh,path);}f.sharedMesh=mesh;EditorUtility.SetDirty(mesh);
            var renderer=f.GetComponent<MeshRenderer>();string mp=Dir+"Paper_"+id+"_"+n+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(material){EditorUtility.CopySerialized(renderer.sharedMaterial,material);UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);}else{material=renderer.sharedMaterial;AssetDatabase.CreateAsset(material,mp);}renderer.sharedMaterial=material;EditorUtility.SetDirty(material);n++;
        }
    }
    static string CabinetTitle(string id){switch(id){case "medical":return "医疗文件柜";case "desk":return "书桌抽屉";case "dressing":return "梳妆台信匣";case "sideboard":return "会客室侧柜";case "hall":return "玄关窄柜";case "toys":return "玩具柜";case "wardrobe":return "卧室衣柜";case "bedside-a":return "床头小抽屉";case "child-bedside":return "孩子的床头柜";case "kitchen-bills":return "厨房账单柜";case "utility":return "洗衣杂物柜";case "bath-mirror":return "浴室镜柜";case "linen":return "床单柜";case "old-trunk":return "旧衣箱";default:return "木柜抽屉";}}
    static string Everyday(string id)=>id=="toys"?"积木、旧布球与一只修补过的小马。":id=="bath-mirror"?"剃须刷、肥皂、梳子和普通药瓶，整齐地放在搪瓷托盘上。":id=="linen"?"叠好的床单和枕套。边缘有细密的补线。":"日常使用的物件、折好的布料与纸包装。木头上留着长年开合磨出的亮痕。";
    static Material Material(string name){
        string path=Dir+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool glass=name=="Glass";if(!m){m=new Material(Shader.Find(glass?"WrongDate/ClearGlass":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        if(glass){m.SetColor("_Tint",new Color(.65f,.74f,.73f,.035f));m.renderQueue=3000;return m;}
        string hex=name switch {"Cream"=>"#C5BBA0","Green"=>"#46564A","Iron"=>"#252A28","Brass"=>"#9C7A40","Enamel"=>"#C6C8B5","Paper"=>"#D1C5A5","Chalk"=>"#D0CEC1","Blood"=>"#95302C","Bulb"=>"#FFE0A0","Mirror"=>"#9EA6A4",_=>"#FFFFFF"};ColorUtility.TryParseHtmlString(hex,out var c);m.SetColor("_BaseColor",c);if(name=="Blood"){c.a=.78f;m.SetColor("_BaseColor",c);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);}
        m.SetFloat("_Smoothness",name=="Mirror"?.9f:name=="Brass"?.48f:name=="Enamel"?.44f:name=="Walnut"?.28f:.18f);m.SetFloat("_Metallic",name=="Mirror"?.9f:name=="Brass"?.68f:name=="Iron"?.38f:0);
        foreach(var normal in new[]{false,true}){var file=Dir+name+(normal?"Normal":"")+".png";var importer=AssetImporter.GetAtPath(file) as TextureImporter;if(!importer)continue;var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            if(importer.textureType!=type||importer.anisoLevel!=8||!importer.mipmapEnabled){importer.textureType=type;importer.anisoLevel=8;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();}
            m.SetTexture(normal?"_BumpMap":"_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(file));if(normal){m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.4f);}}
        if(name=="Bulb"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1,.68f,.3f)*1.5f);}EditorUtility.SetDirty(m);return m;
    }
    static void AddGuard(Transform root){
        var guard=StationClerkAsset.Create(root,new Vector3(9.1f,.16f,1.8f));guard.name="住宅看守";guard.GetComponentInChildren<StationClerkPerformance>().speaker="住宅看守";guard.transform.localRotation=Quaternion.Euler(0,270,0);
        var item=guard.GetComponent<Interactable>();item.title="住宅看守";item.evidenceId="";item.source="住宅门外";item.body="许可已经核过。可以进去查阅。";
        // Reuse the existing rig and motions; give this role its own material, never recolor the actual clerk.
        foreach(var skin in guard.GetComponentsInChildren<SkinnedMeshRenderer>()){
            var materials=skin.sharedMaterials;for(int i=0;i<materials.Length;i++){var path=Dir+"Guard_"+i+".mat";var copy=AssetDatabase.LoadAssetAtPath<Material>(path);if(!copy){copy=new Material(materials[i]);AssetDatabase.CreateAsset(copy,path);}copy.SetColor("_BaseColor",i==0?new Color(.64f,.7f,.78f):Color.white);materials[i]=copy;}skin.sharedMaterials=materials;skin.renderingLayerMask|=HouseLayer;
        }
    }
    static void GateLamp(Transform root){
        Renderer Part(string name,PrimitiveType type,Vector3 position,Vector3 scale,string material){var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=Material(material);r.renderingLayerMask|=HouseLayer;return r;}
        Part("Gate lamp post",PrimitiveType.Cylinder,new Vector3(10,.99f,2.6f),new Vector3(.065f,.99f,.065f),"Iron");
        var glow=Part("Gate lamp glass",PrimitiveType.Sphere,new Vector3(10,2.15f,2.6f),new Vector3(.21f,.27f,.21f),"Bulb");
        Part("Gate lamp cap",PrimitiveType.Cube,new Vector3(10,2.31f,2.6f),new Vector3(.3f,.06f,.3f),"Iron");
        var lamp=Node("Gate lamp light",root,new Vector3(10,2.15f,2.6f)).AddComponent<Light>();lamp.type=LightType.Point;lamp.intensity=2.4f;lamp.range=3.5f;lamp.color=new Color(1,.85f,.65f);lamp.shadows=LightShadows.None;lamp.renderingLayerMask=(int)HouseLayer;lamp.GetUniversalAdditionalLightData().renderingLayers=HouseLayer;DayNightLighting.Bind(lamp,DayNightLamp.Role.Exterior,.85f,glow);
    }
    public static void Validate(GameObject root,HomePlan p){
        void Need(bool v,string s){if(!v)throw new Exception("HOME_LAYOUT: "+s);}
        Need(p.storeys==2&&p.rooms==8,"two storeys/eight rooms");Need(root.GetComponentsInChildren<HouseContainer>().Length==p.cabinets.Length,"all storage targets");Need(root.GetComponentsInChildren<ShopMechanism>().Length==p.doors.Length,"all doors");
        var nodes=root.GetComponentsInChildren<Transform>();
        Need(root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.name.Contains(" glazing"))==p.openings.Length,"missing or extra window glazing");
        foreach(var w in p.openings){var node=nodes.Single(t=>t.name==w.name);var glass=node.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name.StartsWith(w.name+" glazing"));var centre=glass.bounds.center;var size=Mathf.Abs(w.yaw)>45?new Vector3(.014f,w.size[1]-.07f,w.size[0]-.07f):new Vector3(w.size[0]-.07f,w.size[1]-.07f,.014f);Need(Vector3.Distance(glass.bounds.size,size)<.02f,"window size or orientation "+w.name);Need(Vector3.Distance(root.transform.InverseTransformPoint(centre),V(w.centre))<.015f,"window centre "+w.name);}
        foreach(var d in p.doors){var door=root.GetComponentsInChildren<ShopMechanism>().Single(m=>m.fixtureId=="home-door-"+d.id);Need(Vector3.Distance(root.transform.InverseTransformPoint(door.transform.position),V(d.pivot))<.005f,"displaced door "+d.id);var leaf=door.GetComponentsInChildren<MeshRenderer>().SingleOrDefault(r=>r.name.StartsWith("Panelled door"));Need(leaf,"missing leaf "+d.id+" names="+string.Join(";",door.GetComponentsInChildren<MeshRenderer>().Select(r=>r.name)));Need(Vector3.Distance(leaf.bounds.center,door.transform.TransformPoint(new Vector3(d.width/2,d.height/2,0)))<.02f,"door leaf/collider alignment "+d.id);}
        Need(root.GetComponentsInChildren<HouseContainer>().Single(c=>c.safe).contents.Length==4,"safe contents");
        Need(root.GetComponentsInChildren<Interactable>().Any(e=>e.evidenceId=="E09")&&root.GetComponentsInChildren<Interactable>().Any(e=>e.evidenceId=="E10"),"stable residential evidence");
        Need(root.GetComponentsInChildren<Light>().All(l=>l.shadows==LightShadows.None),"no added shadow lamps");
        Directory.CreateDirectory("../output/demo/house");File.WriteAllText("../output/demo/house/import.json","{\"passed\":true,\"rooms\":8,\"storeys\":2,\"windows\":"+p.openings.Length+",\"containers\":"+p.cabinets.Length+",\"singleBuilding\":true}");Debug.Log("HOME_LAYOUT_PASS");
    }
}
