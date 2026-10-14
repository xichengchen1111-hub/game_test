using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// E stone-barn repair garage (B07, 旧石仓修车院), user-selected 2026-10-14. One shared prefab is
// both the street exterior and the walkable interior: the Blender shell (tools/garage/build_garage.py)
// and this assembler read the same layout contract (Resources/GarageLayout.json). Doors use
// ShopMechanism hinges with sweep protection; rooms share the town clock, weather, photocell lamps
// and saves. Interior surfaces and lamps reuse the police interior light layer (Light Layer 4):
// the two buildings are 140 m apart, far beyond any interior lamp range.
public static partial class BuildSlice {
    const string GarageArt="Assets/Art/Garage/";
    const string GaragePrefabPath="Assets/Generated/RepairGarage.prefab";
    const string GarageMaterials="Assets/Generated/Materials/Garage/";
    [Serializable] class GarageLeafInfo { public string @object, opening, fixture, axis; public int index, count; public float[] hinge, leafCentre; public bool lowEnd; public float plane, width, height; }
    [Serializable] class GarageDrawerInfo { public string @object, label; public int index; public float[] front, axis; public float travel; }
    [Serializable] class GarageMaterialInfo { public string name, hex; public float metallic, roughness, tile; }
    [Serializable] class GarageManifest { public int version, layoutVersion, triangles; public GarageLeafInfo[] leaves; public GarageDrawerInfo[] drawers; public GarageMaterialInfo[] materials; public string blender; }

    static Texture2D GarageTexture(string path,bool normal,bool repeat=true) {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing garage texture "+path+" (tools/garage/make_garage_assets.py)");
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;var wrap=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
        if(importer.textureType!=type||importer.sRGBTexture==normal||importer.wrapMode!=wrap||!importer.mipmapEnabled||importer.anisoLevel!=8||importer.maxTextureSize!=1024) {
            importer.textureType=type;importer.sRGBTexture=!normal;importer.wrapMode=wrap;importer.mipmapEnabled=true;
            importer.anisoLevel=8;importer.maxTextureSize=1024;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material GarageMaterial(GarageMaterialInfo info) {
        // The police kit's furniture/joinery families keep their shared police materials.
        if(info.name.StartsWith("P"))return PoliceMaterial(new PoliceMaterialInfo{name=info.name,hex=info.hex,metallic=info.metallic,roughness=info.roughness,tile=info.tile});
        Directory.CreateDirectory(GarageMaterials);
        string path=GarageMaterials+info.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool glass=info.name=="G_Glass";
        var shader=Shader.Find(glass?"WrongDate/ClearGlass":"Universal Render Pipeline/Lit");
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=shader)m.shader=shader;
        if(glass){m.SetColor("_Tint",new Color(.55f,.68f,.78f,.06f));EditorUtility.SetDirty(m);return m;}
        // name -> (texture family, tint). Families under Garage/ are this kit's; Police* are shared.
        (string tex,Color tint) map=info.name switch {
            "GExt_Rubble"=>("Garage/GarageRubble",Color.white),
            "GExt_Ashlar"=>("Police/PoliceAshlar",new Color(.96f,.93f,.86f)),
            "GExt_Cream"=>("Police/PolicePlaster",C("#"+info.hex)),
            "GExt_Slate"=>("Police/PoliceSlate",Color.white),
            "GExt_Slate2"=>("Police/PoliceSlate",new Color(.78f,.8f,.82f)),
            "GExt_Tin"=>("Garage/GarageTin",Color.white),
            "GExt_Boards"=>("Garage/GarageBoards",Color.white),
            "GExt_Timber"=>("Police/PoliceDarkOak",new Color(.82f,.77f,.72f)),
            "GExt_Walnut"=>("Police/PoliceDarkOak",new Color(.66f,.62f,.58f)),
            "GExt_Cobble"=>("Garage/GarageCobble",Color.white),
            "GExt_Flags"=>("Police/PoliceFlags",new Color(.92f,.9f,.86f)),
            "GInt_Lime"=>("Garage/GarageLime",Color.white),
            "GInt_Plaster"=>("Police/PolicePlaster",C("#"+info.hex)),
            "GInt_Ceiling"=>("Police/PolicePlaster",C("#"+info.hex)),
            "GInt_Cement"=>("Garage/GarageCement",Color.white),
            "GInt_Plank"=>("Police/PoliceOak",new Color(.82f,.76f,.7f)),
            _=>(null,C("#"+info.hex))};
        if(info.name=="GExt_Sign") {
            m.SetTexture("_BaseMap",GarageTexture(GarageArt+"Textures/GarageSign.png",false,false));m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");map.tint=Color.white;
        } else if(map.tex!=null) {
            string folder=map.tex.StartsWith("Garage/")?GarageArt+"Textures/":PoliceArt+"Textures/";string file=map.tex.Substring(map.tex.IndexOf('/')+1);
            m.SetTexture("_BaseMap",map.tex.StartsWith("Garage/")?GarageTexture(folder+file+"_Base.png",false):PoliceTexture(file+"_Base",false));
            m.SetTexture("_BumpMap",map.tex.StartsWith("Garage/")?GarageTexture(folder+file+"_Normal.png",true):PoliceTexture(file+"_Normal",true));
            m.SetFloat("_BumpScale",file.Contains("Plaster")||file.Contains("Lime")?.35f:.8f);m.EnableKeyword("_NORMALMAP");
        } else {m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
        m.SetColor("_BaseColor",map.tint);m.SetFloat("_Metallic",info.metallic);m.SetFloat("_Smoothness",Mathf.Clamp01(1-info.roughness)*.85f);
        m.SetTextureScale("_BaseMap",Vector2.one);
        bool glow=info.name=="G_Bulb"||info.name=="G_LampGlass";
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",(info.name=="G_Bulb"?new Color(1f,.86f,.62f):new Color(1f,.78f,.48f))*3.2f);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
        else {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }

    static GameObject GarageVisual(Transform root,GarageLayout layout,Dictionary<string,Material> materials) {
        const string path=GarageArt+"RepairGarage.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("Generate the garage first: tools/BuildGarage.ps1");
        if(importer.importAnimation||importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importLights||importer.importCameras) {
            importer.importAnimation=false;importer.isReadable=false;importer.importLights=false;importer.importCameras=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        visual.name="Garage shell and furnishings";visual.transform.SetParent(root,false);
        var nodes=visual.GetComponentsInChildren<Transform>();
        var north=nodes.Single(t=>t.name=="NorthMarker");var east=nodes.Single(t=>t.name=="EastMarker");
        var direction=north.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.forward,Vector3.up),Vector3.up)*visual.transform.localRotation;
        if(Vector3.Distance(root.InverseTransformPoint(north.position),new Vector3(0,0,3))>.01f||Vector3.Distance(root.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Garage FBX axis/scale mismatch: north="+root.InverseTransformPoint(north.position)+" east="+root.InverseTransformPoint(east.position));
        var exteriorLeaves=new HashSet<string>(layout.openings.Where(o=>o.exterior).Select(o=>o.id));
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m?m.name.Split('.')[0]:"";
                if(!materials.TryGetValue(key,out var mat))throw new Exception("Unmapped garage material "+key+" on "+renderer.name);
                return mat;}).ToArray();
            string n=renderer.name;
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass");
            renderer.shadowCastingMode=glass?ShadowCastingMode.Off:ShadowCastingMode.On;
            bool exteriorLeaf=n.StartsWith("Leaf_")&&exteriorLeaves.Any(id=>n.StartsWith("Leaf_"+id+"_"));
            bool interior=n.StartsWith("Int_")||n.StartsWith("Furn_")||n.StartsWith("Drawer_")||(n.StartsWith("Leaf_")&&!exteriorLeaf)||((n.StartsWith("Lamp_")||n.StartsWith("Bulb_"))&&!n.Contains("lantern"));
            if(interior)renderer.renderingLayerMask|=PoliceInteriorLayer;
            renderer.gameObject.isStatic=!(n.StartsWith("Leaf_")||n.StartsWith("Drawer_"));
        }
        return visual;
    }

    static void GarageCollision(Transform root,GarageLayout layout) {
        var group=Group("Garage collision",Vector3.zero,root).transform;
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
        foreach(var p in layout.posts)PoliceBox(group,"Shed post "+p.id,new Bounds(new Vector3(p.x,(p.y0+p.y1)/2,p.z),new Vector3(p.w+.04f,p.y1-p.y0,p.w+.04f)));
        foreach(var f in layout.furniture.Where(f=>f.collide)) {
            var g=new GameObject("Furniture "+f.id);g.transform.SetParent(group,false);g.transform.localPosition=new Vector3(f.x,f.y,f.z);g.transform.localRotation=Quaternion.Euler(0,f.yaw,0);g.isStatic=true;
            float h=f.kind=="chair"||f.kind=="bench"?Mathf.Min(f.h,.45f):f.h;
            var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,h/2,0);c.size=new Vector3(f.w,h,f.d);
        }
        // Window apertures keep the player in without blocking inspection rays (Ignore Raycast).
        foreach(var o in layout.openings.Where(o=>o.kind=="window")) {
            var w=layout.Wall(o.wall);var b=PoliceLayout.PieceBounds(w,new Vector4(o.at-o.width/2,o.at+o.width/2,o.bottom,o.top));
            b.size=w.axis=="x"?new Vector3(b.size.x,b.size.y,.08f):new Vector3(.08f,b.size.y,b.size.z);
            PoliceBox(group,"Window safety "+o.id,b,2);
        }
        Debug.Log("GARAGE_COLLISION_READY: "+walls+" wall pieces from the shared opening cut");
    }

    static void GarageDoors(Transform root,GameObject visual,GarageLayout layout,GarageManifest manifest,PoliceDocumentSet docs) {
        var group=Group("Garage doors and drawer",Vector3.zero,root).transform;
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
            door.sound=PoliceAudio(pivot,opening.style=="carriage"?"PoliceIronDoor":"PoliceDoorOpen");
            door.openClip=door.sound.clip;door.closeClip=opening.style=="carriage"?door.sound.clip:AssetDatabase.LoadAssetAtPath<AudioClip>(PoliceArt+"Sounds/PoliceDoorClose.wav");
        }
        foreach(var drawer in manifest.drawers) {
            var item=layout.items.Single(i=>i.kind=="drawer"&&i.target=="garage-parts-drawer");
            var front=new Vector3(drawer.front[0],drawer.front[1],drawer.front[2]);var axis=new Vector3(drawer.axis[0],drawer.axis[1],drawer.axis[2]).normalized;
            var pivot=Group("Drawer "+item.title,front,group);
            nodes.Single(t=>t.name==drawer.@object).SetParent(pivot.transform,true);
            var face=Group("Drawer front collision",Vector3.zero,pivot.transform);var box=face.AddComponent<BoxCollider>();
            box.size=Mathf.Abs(axis.x)>.5f?new Vector3(.04f,.2f,.27f):new Vector3(.27f,.2f,.04f);
            var mech=pivot.AddComponent<ShopMechanism>();mech.fixtureId=item.target;mech.movingPart=pivot.transform;mech.openOffset=axis*drawer.travel;
            mech.label=item.title;mech.sound=PoliceAudio(pivot,"PoliceDrawer",.45f,8);mech.openClip=mech.closeClip=mech.sound.clip;
            // The contents target sits behind the front panel, so the front stays clickable to push it back.
            var card=Group("Drawer contents · "+item.title,-axis*.16f+Vector3.up*.13f,pivot.transform);
            var trigger=card.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=Mathf.Abs(axis.x)>.5f?new Vector3(.2f,.22f,.26f):new Vector3(.26f,.22f,.2f);
            PoliceDocumentComponent(card,docs.Get(item.doc),"");
            card.AddComponent<OpenDrawerTarget>().drawer=mech;
        }
    }

    static void GarageLights(Transform root,GameObject visual,GarageLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Garage lamps",Vector3.zero,root).transform;
        var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);
        foreach(var l in layout.lamps) {
            bool exterior=l.role=="Exterior";
            var light=Point("Garage lamp "+l.id,Vector3.zero,exterior?C("#FFD7A0"):l.kind=="desk"?C("#FFD9A6"):C("#FFDDB0"),l.intensity,l.range);
            light.transform.SetParent(group,false);light.transform.localPosition=new Vector3(l.x,l.y-(l.kind=="pendant"?.08f:0),l.z);
            if(!exterior)LayerOnly(light,PoliceInteriorLayer);
            var bulb=renderers.Where(r=>r.name=="Bulb_"+l.id).ToArray();
            if(bulb.Length!=1)throw new Exception("Garage lamp needs one emissive bulb: "+l.id);
            controllers[l.id]=DayNightLighting.Bind(light,exterior?DayNightLamp.Role.Exterior:DayNightLamp.Role.Interior,l.access,bulb);
        }
    }

    // Notebook records lie on the records-room desk as real sheets of paper (3D inspection contract).
    static void GaragePaper(PoliceDocumentItem item,string evidenceId,float width,float height) {
        var visual=EvidenceModel.Paper("Physical "+evidenceId,item.illustration,width,height);visual.transform.SetParent(item.transform,false);visual.AddComponent<EvidencePhysical>();
        const string folder="Assets/Generated/Garage/";Directory.CreateDirectory(folder);int n=0;
        foreach(var f in visual.GetComponentsInChildren<MeshFilter>()) {
            string path=folder+"Paper_"+evidenceId+"_"+n+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh){EditorUtility.CopySerialized(f.sharedMesh,mesh);UnityEngine.Object.DestroyImmediate(f.sharedMesh);}else{mesh=f.sharedMesh;AssetDatabase.CreateAsset(mesh,path);}f.sharedMesh=mesh;EditorUtility.SetDirty(mesh);
            var renderer=f.GetComponent<MeshRenderer>();string mp=folder+"Paper_"+evidenceId+"_"+n+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(material){EditorUtility.CopySerialized(renderer.sharedMaterial,material);UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);}else{material=renderer.sharedMaterial;AssetDatabase.CreateAsset(material,mp);}
            renderer.sharedMaterial=material;renderer.renderingLayerMask|=PoliceInteriorLayer;EditorUtility.SetDirty(material);n++;
        }
    }

    static void GarageItems(Transform root,GarageLayout layout,Dictionary<string,DayNightLamp> controllers,PoliceDocumentSet docs) {
        var group=Group("Garage materials and seats",Vector3.zero,root).transform;
        foreach(var item in layout.items) {
            if(item.kind=="drawer")continue;
            if(item.kind=="seat") {
                float targetY=item.y-item.sitY;
                var facing=new Vector3(Mathf.Sin(item.yaw*Mathf.Deg2Rad),0,Mathf.Cos(item.yaw*Mathf.Deg2Rad));
                RestSeat(group,item.id,item.title,new Vector3(item.sitX,item.sitY,item.sitZ),facing,new Vector3(item.standX,item.sitY,item.standZ),item.eye,new Vector3(item.w,item.h,item.d),targetY);
                continue;
            }
            if(item.kind=="npc") {
                var actor=StationClerkAsset.Create(group,new Vector3(item.x,item.y,item.z),false,true);
                // The clerk rig faces its root's -Z; turn it to the authored facing (sin yaw, cos yaw).
                actor.transform.localRotation=Quaternion.LookRotation(-new Vector3(Mathf.Sin(item.yaw*Mathf.Deg2Rad),0,Mathf.Cos(item.yaw*Mathf.Deg2Rad)),Vector3.up);
                foreach(var r in actor.GetComponentsInChildren<Renderer>())r.renderingLayerMask|=PoliceInteriorLayer;
                continue;
            }
            if(item.kind=="evidence") {
                var paperRoot=Group("Item · "+item.id,new Vector3(item.x,item.y,item.z),group);paperRoot.transform.localRotation=Quaternion.Euler(90,item.yaw,0);
                var doc=PoliceDocumentComponent(paperRoot,docs.Get(item.doc),item.evidence);
                GaragePaper(doc,item.evidence,.25f,.322f);
                var box=paperRoot.AddComponent<BoxCollider>();box.size=new Vector3(.25f,.322f,.016f);box.isTrigger=true;
                continue;
            }
            var g=Group("Item · "+item.id,new Vector3(item.x,item.y,item.z),group);g.transform.localRotation=Quaternion.Euler(0,item.yaw,0);
            var trigger=g.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(item.w,item.h,item.d);
            if(item.kind=="document")PoliceDocumentComponent(g,docs.Get(item.doc),"");
            else if(item.kind=="lampswitch") {
                var lamp=layout.Lamp(item.target);var s=g.AddComponent<LampSwitch>();s.label=item.title;s.offId=lamp.switchId;s.lamps=new[]{controllers[item.target]};
                if(string.IsNullOrEmpty(s.offId))throw new Exception("Lamp switch without saved off ID: "+item.id);
            }
            else throw new Exception("Unknown garage item kind "+item.kind);
        }
    }

    static PoliceDocumentSet GarageDocuments() {
        var asset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/GarageDocuments.json");
        if(!asset)throw new Exception("Missing Resources/GarageDocuments.json (tools/garage/write_garage_texts.py)");
        var set=JsonUtility.FromJson<PoliceDocumentSet>(asset.text);
        if(set.documents==null||set.documents.Select(d=>d.id).Distinct().Count()!=set.documents.Length)throw new Exception("Garage documents need unique IDs");
        return set;
    }

    // Garage-only rebuild after a layout/shell change. The Town scene holds this prefab as an
    // instance with transform overrides only, so it follows without regenerating the town.
    public static void RebuildGarage() {
        AssetDatabase.Refresh();GaragePrefab();AssetDatabase.SaveAssets();
    }
    static void GaragePrefab() {
        var layoutAsset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/GarageLayout.json");
        var layout=GarageLayout.Parse(layoutAsset.text);
        var manifest=JsonUtility.FromJson<GarageManifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(GarageArt+"GarageBuildManifest.json").text);
        var materials=manifest.materials.ToDictionary(m=>m.name,GarageMaterial);
        var docs=GarageDocuments();
        var root=new GameObject("B07 修车铺 · E旧石仓修车院");
        root.AddComponent<RepairGarage>().layoutJson=layoutAsset;
        var visual=GarageVisual(root.transform,layout,materials);
        GarageCollision(root.transform,layout);
        GarageDoors(root.transform,visual,layout,manifest,docs);
        var controllers=new Dictionary<string,DayNightLamp>();
        GarageLights(root.transform,visual,layout,controllers);
        GarageItems(root.transform,layout,controllers,docs);
        // Rain clipping; the hall and office are one acoustic enclosure (any open outer door keeps
        // the outdoor rain); the shed and the porch awning are roofed but open.
        var doors=root.GetComponentsInChildren<ShopMechanism>().Where(d=>d.protectPlayer).ToArray();
        foreach(var s in layout.shelters) {
            var g=Group("Rain shelter "+s.id,Vector3.zero,root.transform);
            WeatherAuthoring.Shelter(g,new Vector2(s.x1-s.x0,s.z1-s.z0),s.eaves,s.rise,new Vector2((s.x0+s.x1)/2,(s.z0+s.z1)/2));
            var shelter=g.GetComponent<WeatherShelter>();shelter.ridgeAlongX=s.ridgeAlongX;shelter.acousticallyOpen=s.open;
            shelter.exteriorDoors=doors.Where(d=>(s.doors??new string[0]).Any(f=>d.fixtureId==f||d.fixtureId.StartsWith(f+"-"))).ToArray();
            if((s.doors??new string[0]).Length>0&&shelter.exteriorDoors.Length!=4)throw new Exception("Garage shelter must bind the arch pair, office and rear doors: "+s.id);
        }
        var signFront=new Vector3((layout.office.x0+layout.office.x1)/2,3.74f,-.06f);
        RailBoard(root.transform,"修 车 铺",signFront,new Vector2(2.6f,.44f),Quaternion.identity);
        GarageChecks.Validate(root,layout,manifest.leaves.Length,manifest.drawers.Length);
        ZFightRepair.Repair(root.transform,"RepairGarage");
        PrefabUtility.SaveAsPrefabAsset(root,GaragePrefabPath);UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("GARAGE_PREFAB_PASS: "+GaragePrefabPath);
    }

    static GameObject GarageTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(GaragePrefabPath));
        var layout=root.GetComponent<RepairGarage>().Layout;
        root.transform.position=layout.Origin;MapMarker(root,b,true);
        return root;
    }
    static void GarageYardTree() {
        var garage=UnityEngine.Object.FindFirstObjectByType<RepairGarage>();if(!garage)throw new Exception("Repair garage missing for its yard tree");
        // A bare-crowned yard tree in the north-west corner, as in the concept, clear of the gate path.
        GreeneryAuthoring.ExtraTree(garage.transform,"Tree5",new Vector3(2.4f,garage.Layout.levels.yard,28.0f),2.0f,77,"Garage yard tree");
    }
}
