using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// A gabled old inn (B08, 山墙老客栈), user-selected 2026-10-16. One shared prefab is both the street
// exterior and the walkable interior: the Blender shell (tools/hotel/build_hotel.py) and this
// assembler read the same layout contract (Resources/HotelLayout.json). Doors use ShopMechanism
// hinges with sweep protection; the register desk's lamp tilts (RakingLamp) to rake light across
// the register. Interior surfaces and lamps reuse the police interior light layer (Light Layer 4):
// the nearest other building on that layer (the hospital) is 50 m away, beyond every lamp range.
// The porter and the landlady are placeholders (user: a teammate makes the people).
public static partial class BuildSlice {
    const string HotelArt="Assets/Art/Hotel/";
    const string HotelPrefabPath="Assets/Generated/Hotel.prefab";
    const string HotelMaterials="Assets/Generated/Materials/Hotel/";
    [Serializable] class HotelLeafInfo { public string @object, opening, fixture, axis; public int index, count; public float[] hinge, leafCentre; public bool lowEnd; public float plane, width, height; }
    [Serializable] class HotelLampHeadInfo { public string @object, bulb, lamp; public float[] joint, shade; }
    [Serializable] class HotelMaterialInfo { public string name, hex; public float metallic, roughness, tile; }
    [Serializable] class HotelManifest { public int version, layoutVersion, triangles; public HotelLeafInfo[] leaves; public HotelLampHeadInfo[] lampHeads; public HotelMaterialInfo[] materials; public string blender; }

    static Texture2D HotelTexture(string path,bool normal,bool repeat=true) {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing hotel texture "+path+" (tools/hotel/make_hotel_assets.py)");
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;var wrap=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        if(importer.textureType!=type||importer.sRGBTexture==normal||importer.wrapMode!=wrap||!importer.mipmapEnabled||importer.anisoLevel!=8||importer.maxTextureSize!=1024) {
            importer.textureType=type;importer.sRGBTexture=!normal;importer.wrapMode=wrap;importer.mipmapEnabled=true;
            importer.anisoLevel=8;importer.maxTextureSize=1024;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material HotelMaterial(HotelMaterialInfo info) {
        // The police and garage kits' furniture/joinery families keep their shared materials.
        if(info.name.StartsWith("P"))return PoliceMaterial(new PoliceMaterialInfo{name=info.name,hex=info.hex,metallic=info.metallic,roughness=info.roughness,tile=info.tile});
        if(info.name.StartsWith("G"))return GarageMaterial(new GarageMaterialInfo{name=info.name,hex=info.hex,metallic=info.metallic,roughness=info.roughness,tile=info.tile});
        Directory.CreateDirectory(HotelMaterials);
        string path=HotelMaterials+info.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=shader)m.shader=shader;
        // name -> (texture family, tint, clamp). Hotel/ families are this kit's; Police/ and Garage/ are shared.
        (string tex,Color tint,bool clamp) map=info.name switch {
            "HExt_Rubble"=>("Garage/GarageRubble",new Color(1f,.97f,.92f),false),
            "HExt_Chimney"=>("Garage/GarageRubble",new Color(.86f,.76f,.68f),false),
            "HExt_Ashlar"=>("Police/PoliceAshlar",new Color(.96f,.93f,.86f),false),
            "HExt_Plaster"=>("Police/PolicePlaster",C("#"+info.hex),false),
            "HExt_Timber"=>("Police/PoliceDarkOak",new Color(.52f,.46f,.42f),false),
            "HExt_Shutter"=>("Police/PoliceDarkOak",new Color(.46f,.48f,.4f),false),
            "HExt_Slate"=>("Police/PoliceSlate",Color.white,false),
            "HExt_Ridge"=>("Police/PoliceSlate",new Color(.78f,.8f,.84f),false),
            "HInt_PaperA"=>("Hotel/HotelPaperA",Color.white,false),
            "HInt_PaperB"=>("Hotel/HotelPaperB",Color.white,false),
            "HInt_Lime"=>("Garage/GarageLime",Color.white,false),
            "HInt_Ceiling"=>("Police/PolicePlaster",C("#"+info.hex),false),
            "HInt_Beam"=>("Police/PoliceDarkOak",new Color(.6f,.54f,.48f),false),
            "HInt_Panel"=>("Police/PoliceDarkOak",new Color(.68f,.6f,.52f),false),
            "HInt_Oak"=>("Police/PoliceOak",new Color(.78f,.7f,.62f),false),
            "HInt_Flags"=>("Police/PoliceFlags",new Color(.9f,.88f,.84f),false),
            "HInt_Tiles"=>("Police/PoliceTiles",Color.white,false),
            "HInt_Rug"=>("Hotel/HotelRug",Color.white,true),
            "HInt_Runner"=>("Hotel/HotelRunner",Color.white,false),
            _=>(null,C("#"+info.hex),false)};
        if(map.tex!=null) {
            bool own=map.tex.StartsWith("Hotel/");string file=map.tex.Substring(map.tex.IndexOf('/')+1);
            string folder=own?HotelArt+"Textures/":map.tex.StartsWith("Garage/")?GarageArt+"Textures/":null;
            m.SetTexture("_BaseMap",folder!=null?HotelTexture(folder+file+"_Base.png",false,!map.clamp):PoliceTexture(file+"_Base",false));
            string normalPath=folder!=null?folder+file+"_Normal.png":null;
            if(folder==null||File.Exists(normalPath)) {
                m.SetTexture("_BumpMap",folder!=null?HotelTexture(normalPath,true):PoliceTexture(file+"_Normal",true));
                m.SetFloat("_BumpScale",file.Contains("Plaster")||file.Contains("Lime")||file.Contains("Paper")?.35f:.8f);m.EnableKeyword("_NORMALMAP");
            } else {m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
        } else {m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
        m.SetColor("_BaseColor",map.tint);m.SetFloat("_Metallic",info.metallic);m.SetFloat("_Smoothness",Mathf.Clamp01(1-info.roughness)*.85f);
        m.SetTextureScale("_BaseMap",Vector2.one);
        m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }

    static GameObject HotelVisual(Transform root,HotelLayout layout,Dictionary<string,Material> materials) {
        const string path=HotelArt+"Hotel.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("Generate the hotel first: tools/BuildHotel.ps1");
        if(importer.importAnimation||importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importLights||importer.importCameras) {
            importer.importAnimation=false;importer.isReadable=false;importer.importLights=false;importer.importCameras=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        visual.name="Hotel shell and furnishings";visual.transform.SetParent(root,false);
        var nodes=visual.GetComponentsInChildren<Transform>();
        var north=nodes.Single(t=>t.name=="NorthMarker");var east=nodes.Single(t=>t.name=="EastMarker");
        var direction=north.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.forward,Vector3.up),Vector3.up)*visual.transform.localRotation;
        if(Vector3.Distance(root.InverseTransformPoint(north.position),new Vector3(0,0,3))>.01f||Vector3.Distance(root.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Hotel FBX axis/scale mismatch: north="+root.InverseTransformPoint(north.position)+" east="+root.InverseTransformPoint(east.position));
        var exteriorLeaves=new HashSet<string>(layout.openings.Where(o=>o.exterior).Select(o=>o.id));
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m?m.name.Split('.')[0]:"";
                if(!materials.TryGetValue(key,out var mat))throw new Exception("Unmapped hotel material "+key+" on "+renderer.name);
                return mat;}).ToArray();
            string n=renderer.name;
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass");
            renderer.shadowCastingMode=glass||n.StartsWith("Int_Runner")?ShadowCastingMode.Off:ShadowCastingMode.On;
            bool exteriorLeaf=n.StartsWith("Leaf_")&&exteriorLeaves.Any(id=>n.StartsWith("Leaf_"+id+"_"));
            bool interior=n.StartsWith("Int_")||n.StartsWith("Furn_")||n.StartsWith("LampHead_")||(n.StartsWith("Leaf_")&&!exteriorLeaf)||((n.StartsWith("Lamp_")||n.StartsWith("Bulb_"))&&!n.Contains("lantern"));
            if(interior)renderer.renderingLayerMask|=PoliceInteriorLayer;
            renderer.gameObject.isStatic=!(n.StartsWith("Leaf_")||n.StartsWith("LampHead_")||n=="Bulb_lamp-register");
        }
        return visual;
    }

    static void HotelCollision(Transform root,HotelLayout layout) {
        var group=Group("Hotel collision",Vector3.zero,root).transform;
        int walls=0;
        foreach(var w in layout.walls)
            foreach(var piece in PoliceLayout.WallPieces(w,layout.openings.Where(o=>o.wall==w.id))){PoliceBox(group,"Wall "+w.id,PoliceLayout.PieceBounds(w,piece));walls++;}
        foreach(var s in layout.slabs) {
            if(s.kind=="ceiling")continue;
            float top=s.y1,bottom=s.kind=="finish"?top-.3f:s.y0;
            PoliceBox(group,"Floor "+s.id,new Bounds(new Vector3((s.x0+s.x1)/2,(top+bottom)/2,(s.z0+s.z1)/2),new Vector3(s.x1-s.x0,top-bottom,s.z1-s.z0)));
        }
        foreach(var s in layout.stairs)
            PoliceBox(group,"Stair "+s.id,new Bounds(new Vector3((s.x0+s.x1)/2,(layout.levels.ground-.02f+s.top)/2,(s.z0+s.z1)/2),new Vector3(s.x1-s.x0,s.top-layout.levels.ground+.02f,s.z1-s.z0)));
        foreach(var b in layout.blocks)
            PoliceBox(group,"Block "+b.id,new Bounds(new Vector3((b.x0+b.x1)/2,(b.y0+b.y1)/2,(b.z0+b.z1)/2),new Vector3(b.x1-b.x0,b.y1-b.y0,b.z1-b.z0)));
        foreach(var p in layout.posts)PoliceBox(group,"Post "+p.id,new Bounds(new Vector3(p.x,(p.y0+p.y1)/2,p.z),new Vector3(p.w+.04f,p.y1-p.y0,p.w+.04f)));
        foreach(var f in layout.furniture.Where(f=>f.collide)) {
            var g=new GameObject("Furniture "+f.id);g.transform.SetParent(group,false);g.transform.localPosition=new Vector3(f.x,f.y,f.z);g.transform.localRotation=Quaternion.Euler(0,f.yaw,0);g.isStatic=true;
            float h=f.kind=="chair"||f.kind=="bench"||f.kind=="armchair"?Mathf.Min(f.h,.45f):f.kind=="bed"?Mathf.Min(f.h,.55f):f.h;
            var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,h/2,0);c.size=new Vector3(f.w,h,f.d);
        }
        // Window apertures keep the player in without blocking inspection rays (Ignore Raycast).
        foreach(var o in layout.openings.Where(o=>o.kind=="window")) {
            var w=layout.Wall(o.wall);var b=PoliceLayout.PieceBounds(w,new Vector4(o.at-o.width/2,o.at+o.width/2,o.bottom,o.top));
            b.size=w.axis=="x"?new Vector3(b.size.x,b.size.y,.08f):new Vector3(.08f,b.size.y,b.size.z);
            PoliceBox(group,"Window safety "+o.id,b,2);
        }
        Debug.Log("HOTEL_COLLISION_READY: "+walls+" wall pieces from the shared opening cut");
    }

    static void HotelDoors(Transform root,GameObject visual,HotelLayout layout,HotelManifest manifest) {
        var group=Group("Hotel doors",Vector3.zero,root).transform;
        var nodes=visual.GetComponentsInChildren<Transform>(true);
        foreach(var leaf in manifest.leaves) {
            var opening=layout.Opening(leaf.opening);
            var hinge=new Vector3(leaf.hinge[0],leaf.hinge[1],leaf.hinge[2]);
            var centre=new Vector3(leaf.leafCentre[0],leaf.leafCentre[1],leaf.leafCentre[2]);
            if(leaf.axis=="z")centre=new Vector3(leaf.plane,leaf.leafCentre[1],leaf.leafCentre[0]);
            string id=leaf.count==2?leaf.fixture+(leaf.index==0?"-left":"-right"):leaf.fixture;
            var pivot=Group("Door "+id,hinge,group);
            var mesh=nodes.Single(t=>t.name==leaf.@object);mesh.SetParent(pivot.transform,true);
            var local=centre-hinge;
            var size=leaf.axis=="x"?new Vector3(leaf.width-.02f,leaf.height-.02f,.07f):new Vector3(.07f,leaf.height-.02f,leaf.width-.02f);
            var collision=Group("Leaf collision",local,pivot.transform);collision.AddComponent<BoxCollider>().size=size;
            var door=pivot.AddComponent<ShopMechanism>();door.fixtureId=id;door.movingPart=pivot.transform;door.protectPlayer=true;
            door.startsOpen=opening.startsOpen;
            door.label=opening.label+(leaf.count==2?(leaf.index==0?"（左扇）":"（右扇）"):"");
            door.leafCentre=local;door.leafSize=size;
            var turned=Quaternion.AngleAxis(90,Vector3.up)*local;
            float side=leaf.axis=="x"?turned.z:turned.x;
            door.openEuler=new Vector3(0,(side*opening.swing>0?1:-1)*88,0);
            door.sound=PoliceAudio(pivot,"PoliceDoorOpen");
            door.openClip=door.sound.clip;door.closeClip=AssetDatabase.LoadAssetAtPath<AudioClip>(PoliceArt+"Sounds/PoliceDoorClose.wav");
        }
    }

    static void HotelLights(Transform root,GameObject visual,HotelLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Hotel lamps",Vector3.zero,root).transform;
        var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);
        foreach(var l in layout.lamps) {
            bool exterior=l.role=="Exterior";
            var light=Point("Hotel lamp "+l.id,Vector3.zero,exterior?C("#FFD7A0"):l.kind=="desk"||l.kind=="angle"?C("#FFD9A6"):C("#FFDDB0"),l.intensity,l.range);
            light.transform.SetParent(group,false);light.transform.localPosition=new Vector3(l.x,l.y-(l.kind=="pendant"?.08f:0),l.z);
            if(!exterior)LayerOnly(light,PoliceInteriorLayer);
            var bulb=renderers.Where(r=>r.name=="Bulb_"+l.id).ToArray();
            if(bulb.Length!=1)throw new Exception("Hotel lamp needs one emissive bulb: "+l.id);
            controllers[l.id]=DayNightLighting.Bind(light,exterior?DayNightLamp.Role.Exterior:DayNightLamp.Role.Interior,l.access,bulb);
        }
    }

    // Records lie on the register and the cash desk as real sheets of paper (3D inspection contract).
    static void HotelPaper(PoliceDocumentItem item,string id,float width,float height) {
        var visual=EvidenceModel.Paper("Physical "+id,item.illustration,width,height);visual.transform.SetParent(item.transform,false);visual.AddComponent<EvidencePhysical>();
        const string folder="Assets/Generated/Hotel/";Directory.CreateDirectory(folder);int n=0;
        foreach(var f in visual.GetComponentsInChildren<MeshFilter>()) {
            string path=folder+"Paper_"+id+"_"+n+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh){EditorUtility.CopySerialized(f.sharedMesh,mesh);UnityEngine.Object.DestroyImmediate(f.sharedMesh);}else{mesh=f.sharedMesh;AssetDatabase.CreateAsset(mesh,path);}f.sharedMesh=mesh;EditorUtility.SetDirty(mesh);
            var renderer=f.GetComponent<MeshRenderer>();string mp=folder+"Paper_"+id+"_"+n+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(material){EditorUtility.CopySerialized(renderer.sharedMaterial,material);UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);}else{material=renderer.sharedMaterial;AssetDatabase.CreateAsset(material,mp);}
            renderer.sharedMaterial=material;renderer.renderingLayerMask|=PoliceInteriorLayer;EditorUtility.SetDirty(material);n++;
        }
    }

    static GameObject HotelSheet(Transform group,PoliceItem item,PoliceDocumentSet docs,string evidence,float lift=0) {
        var paperRoot=Group("Item · "+item.id,new Vector3(item.x,item.y+lift,item.z),group);paperRoot.transform.localRotation=Quaternion.Euler(90,item.yaw,0);
        var doc=PoliceDocumentComponent(paperRoot,docs.Get(item.doc),evidence);
        // Register pages are 0.31 x 0.41 m; the dining ledger's page 0.28 x 0.37 m.
        bool register=item.id.StartsWith("register");float w=register?.31f:.28f,h=register?.41f:.37f;
        HotelPaper(doc,item.id,w,h);
        var box=paperRoot.AddComponent<BoxCollider>();box.size=new Vector3(w,h,.016f);box.isTrigger=true;
        return paperRoot;
    }

    // A neutral standing marker where a character will stand: it carries the conversation until the
    // teammate's figure replaces it. Not a character model.
    static void HotelPlaceholder(Transform group,PoliceItem item) {
        var root=Group("人物占位 · "+item.title+"（待队友制作）",new Vector3(item.x,item.y,item.z),group);
        root.transform.localRotation=Quaternion.Euler(0,item.yaw,0);
        var marker=GameObject.CreatePrimitive(PrimitiveType.Capsule);marker.name="Placeholder marker";
        marker.transform.SetParent(root.transform,false);marker.transform.localPosition=new Vector3(0,.85f,0);marker.transform.localScale=new Vector3(.45f,.85f,.45f);
        UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
        var renderer=marker.GetComponent<MeshRenderer>();renderer.sharedMaterial=Mat("HotelPlaceholderGrey","#8E8B85");renderer.renderingLayerMask|=PoliceInteriorLayer;
        var face=GameObject.CreatePrimitive(PrimitiveType.Cube);face.name="Facing tab";face.transform.SetParent(root.transform,false);
        face.transform.localPosition=new Vector3(0,1.45f,.22f);face.transform.localScale=new Vector3(.16f,.05f,.06f);
        UnityEngine.Object.DestroyImmediate(face.GetComponent<Collider>());face.GetComponent<MeshRenderer>().sharedMaterial=Mat("HotelPlaceholderDark","#4C4A46");
        face.GetComponent<MeshRenderer>().renderingLayerMask|=PoliceInteriorLayer;
        var body=root.AddComponent<CapsuleCollider>();body.center=new Vector3(0,.85f,0);body.height=1.7f;body.radius=.24f;
        var actor=root.AddComponent<HotelActor>();actor.role=item.target;actor.title=item.title;actor.source="青川旅馆";
    }

    static void HotelItems(Transform root,HotelLayout layout,HotelManifest manifest,GameObject visual,Dictionary<string,DayNightLamp> controllers,PoliceDocumentSet docs) {
        var group=Group("Hotel materials and seats",Vector3.zero,root).transform;
        GameObject plainPage=null,rakedPage=null;
        foreach(var item in layout.items) {
            if(item.kind=="seat") {
                float targetY=item.y-item.sitY;
                var facing=new Vector3(Mathf.Sin(item.yaw*Mathf.Deg2Rad),0,Mathf.Cos(item.yaw*Mathf.Deg2Rad));
                RestSeat(group,item.id,item.title,new Vector3(item.sitX,item.sitY,item.sitZ),facing,new Vector3(item.standX,item.sitY,item.standZ),item.eye,new Vector3(item.w,item.h,item.d),targetY);
                continue;
            }
            if(item.kind=="npc"){HotelPlaceholder(group,item);continue;}
            if(item.kind=="evidence") {
                var sheet=HotelSheet(group,item,docs,item.evidence,item.id=="register-raking"?.002f:0);
                if(item.id=="register-raking")rakedPage=sheet;
                continue;
            }
            if(item.kind=="page"){plainPage=HotelSheet(group,item,docs,"");continue;}
            if(item.kind=="rakinglamp")continue;
            var g=Group("Item · "+item.id,new Vector3(item.x,item.y,item.z),group);g.transform.localRotation=Quaternion.Euler(0,item.yaw,0);
            var trigger=g.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(item.w,item.h,item.d);
            if(item.kind=="document")PoliceDocumentComponent(g,docs.Get(item.doc),"");
            else if(item.kind=="lampswitch") {
                var lamp=layout.Lamp(item.target);var s=g.AddComponent<LampSwitch>();s.label=item.title;s.offId=lamp.switchId;s.lamps=new[]{controllers[item.target]};
                if(string.IsNullOrEmpty(s.offId))throw new Exception("Lamp switch without saved off ID: "+item.id);
            }
            else throw new Exception("Unknown hotel item kind "+item.kind);
        }
        // The register desk's tilting lamp: the head (and its bulb and light) turn about the joint.
        var info=manifest.lampHeads.Single();var spot=layout.items.Single(i=>i.kind=="rakinglamp");
        var nodes=visual.GetComponentsInChildren<Transform>(true);
        var joint=new Vector3(info.joint[0],info.joint[1],info.joint[2]);
        var pivot=Group("Register lamp head",joint,root.transform);
        nodes.Single(t=>t.name==info.@object).SetParent(pivot.transform,true);
        nodes.Single(t=>t.name==info.bulb).SetParent(pivot.transform,true);
        root.GetComponentsInChildren<Light>().Single(l=>l.name=="Hotel lamp "+info.lamp).transform.SetParent(pivot.transform,true);
        var target=Group("Item · "+spot.id,new Vector3(spot.x,spot.y,spot.z),group);
        var box=target.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(spot.w,spot.h,spot.d);
        var raking=target.AddComponent<RakingLamp>();raking.fixtureId=spot.target;raking.head=pivot.transform;
        // Swing the shade down toward the desk: about the horizontal axis across the arm.
        var shade=new Vector3(info.shade[0],info.shade[1],info.shade[2]);var arm=shade-joint;arm.y=0;
        raking.axis=Vector3.Cross(Vector3.up,arm.normalized).normalized;raking.angle=52;
        var low=Point("Hotel raking light",Vector3.zero,C("#FFE2B8"),0,.75f);low.transform.SetParent(group,false);
        var right=layout.Item("register-raking");
        low.transform.localPosition=new Vector3(right.x,right.y+.07f,right.z-.24f);LayerOnly(low,PoliceInteriorLayer);low.enabled=false;
        raking.raking=low;raking.rakingIntensity=.55f;raking.plainPage=plainPage;raking.rakedPage=rakedPage;
        raking.sound=PoliceAudio(target,"PoliceDrawer",.35f,6);
        if(!plainPage||!rakedPage)throw new Exception("Hotel register pages missing");
        rakedPage.SetActive(false);
    }

    static PoliceDocumentSet HotelDocuments() {
        var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/HotelDocuments.json");
        if(!asset)throw new Exception("Missing Resources/HotelDocuments.json (tools/hotel/write_hotel_texts.py)");
        var set=JsonUtility.FromJson<PoliceDocumentSet>(asset.text);
        if(set.documents==null||set.documents.Select(d=>d.id).Distinct().Count()!=set.documents.Length)throw new Exception("Hotel documents need unique IDs");
        return set;
    }

    // Hotel-only rebuild after a layout/shell change. The Town scene holds this prefab as an
    // instance with transform overrides only, so it follows without regenerating the town.
    public static void RebuildHotel() {
        AssetDatabase.Refresh();HotelPrefab();AssetDatabase.SaveAssets();
    }
    static void HotelPrefab() {
        var layoutAsset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/HotelLayout.json");
        var layout=HotelLayout.Parse(layoutAsset.text);
        var manifest=JsonUtility.FromJson<HotelManifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(HotelArt+"HotelBuildManifest.json").text);
        var materials=manifest.materials.ToDictionary(m=>m.name,HotelMaterial);
        var docs=HotelDocuments();
        var root=new GameObject("B08 旅馆 · A山墙老客栈");
        root.AddComponent<HotelInn>().layoutJson=layoutAsset;
        var visual=HotelVisual(root.transform,layout,materials);
        HotelCollision(root.transform,layout);
        HotelDoors(root.transform,visual,layout,manifest);
        var controllers=new Dictionary<string,DayNightLamp>();
        HotelLights(root.transform,visual,layout,controllers);
        HotelItems(root.transform,layout,manifest,visual,controllers,docs);
        // Rain clipping; the front range and the service wing are one acoustic enclosure (the front
        // door pair or the kitchen back door open keeps the outdoor rain); porch and carport are open.
        var doors=root.GetComponentsInChildren<ShopMechanism>().Where(d=>d.protectPlayer).ToArray();
        foreach(var s in layout.shelters) {
            var g=Group("Rain shelter "+s.id,Vector3.zero,root.transform);
            WeatherAuthoring.Shelter(g,new Vector2(s.x1-s.x0,s.z1-s.z0),s.eaves,s.rise,new Vector2((s.x0+s.x1)/2,(s.z0+s.z1)/2));
            var shelter=g.GetComponent<WeatherShelter>();shelter.ridgeAlongX=s.ridgeAlongX;shelter.acousticallyOpen=s.open;
            shelter.exteriorDoors=doors.Where(d=>(s.doors??new string[0]).Any(f=>d.fixtureId==f||d.fixtureId.StartsWith(f+"-"))).ToArray();
            if((s.doors??new string[0]).Length>0&&shelter.exteriorDoors.Length!=3)throw new Exception("Hotel shelter must bind the front pair and the back door: "+s.id);
        }
        RailBoard(root.transform,"旅  馆",new Vector3(11.6f,3.15f,35.41f),new Vector2(1.2f,.36f),Quaternion.Euler(0,180,0));
        HotelChecks.Validate(root,layout,manifest.leaves.Length);
        ZFightRepair.Repair(root.transform,"Hotel");
        PrefabUtility.SaveAsPrefabAsset(root,HotelPrefabPath);UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("HOTEL_PREFAB_PASS: "+HotelPrefabPath);
    }

    static GameObject HotelTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HotelPrefabPath));
        var layout=root.GetComponent<HotelInn>().Layout;
        root.transform.position=layout.Origin;MapMarker(root,b,true);
        return root;
    }
}
