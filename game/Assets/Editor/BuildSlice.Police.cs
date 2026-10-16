using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// D courtyard-loggia police station (B10), user-selected 2026-10-11, built 2026-10-12.
// One shared prefab is both the street exterior and the walkable interior: the Blender
// shell (tools/police/build_police_station.py) and this assembler read the same layout
// contract (Resources/PoliceLayout.json). Doors use ShopMechanism hinges with sweep
// protection; every room shares the town clock, weather, photocell lamps and saves.
public static partial class BuildSlice {
    // "Light Layer 4": police interior surfaces. Interior lamps light only this layer, so
    // unshadowed room lights never leak through walls onto the street or court.
    public const uint PoliceInteriorLayer=1u<<4;
    const string PoliceArt="Assets/Art/Police/";
    const string PolicePrefabPath="Assets/Generated/PoliceStation.prefab";
    const string PoliceMaterials="Assets/Generated/Materials/Police/";
    [Serializable] class PoliceMaterialInfo { public string name, hex; public float metallic, roughness, tile; }
    [Serializable] class PoliceMaterialList { public PoliceMaterialInfo[] materials; }

    static Texture2D PoliceTexture(string name,bool normal) {
        string path=PoliceArt+"Textures/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing police texture "+path+" (tools/police/make_police_assets.py)");
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        if(importer.textureType!=type||importer.sRGBTexture==normal||importer.wrapMode!=TextureWrapMode.Repeat||!importer.mipmapEnabled||importer.anisoLevel!=8||importer.maxTextureSize!=1024) {
            importer.textureType=type;importer.sRGBTexture=!normal;importer.wrapMode=TextureWrapMode.Repeat;importer.mipmapEnabled=true;
            importer.anisoLevel=8;importer.maxTextureSize=1024;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material PoliceMaterial(PoliceMaterialInfo info) {
        Directory.CreateDirectory(PoliceMaterials);
        string path=PoliceMaterials+info.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool glass=info.name=="P_Glass";
        var shader=Shader.Find(glass?"WrongDate/ClearGlass":"Universal Render Pipeline/Lit");
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=shader)m.shader=shader;
        if(glass){m.SetColor("_Tint",new Color(.55f,.68f,.78f,.06f));EditorUtility.SetDirty(m);return m;}
        // Textured families: the tint multiplies an already coloured texture except for plaster.
        string texture=info.name switch {
            "PExt_Stucco"=>"PolicePlaster","PInt_Plaster"=>"PolicePlaster","PInt_Ceiling"=>"PolicePlaster",
            "PExt_Ashlar"=>"PoliceAshlar","PInt_Stone"=>"PoliceAshlar","PExt_Slate"=>"PoliceSlate","PExt_Flags"=>"PoliceFlags",
            "PInt_Oak"=>"PoliceOak","PInt_FurnLight"=>"PoliceOak","PInt_Tiles"=>"PoliceTiles",
            "PExt_Timber"=>"PoliceDarkOak","PExt_DoorOak"=>"PoliceDarkOak","PInt_Panel"=>"PoliceDarkOak","PInt_Furniture"=>"PoliceDarkOak",
            _=>null};
        Color tint=C("#"+info.hex);
        if(texture!=null) {
            if(texture!="PolicePlaster")tint=info.name switch {"PExt_DoorOak"=>new Color(.72f,.70f,.68f),"PInt_Stone"=>new Color(.86f,.86f,.86f),"PInt_FurnLight"=>new Color(.92f,.9f,.86f),_=>Color.white};
            m.SetTexture("_BaseMap",PoliceTexture(texture+"_Base",false));
            m.SetTexture("_BumpMap",PoliceTexture(texture+"_Normal",true));m.SetFloat("_BumpScale",texture=="PolicePlaster"?.3f:.75f);m.EnableKeyword("_NORMALMAP");
        } else {m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",info.metallic);m.SetFloat("_Smoothness",Mathf.Clamp01(1-info.roughness)*.85f);
        m.SetTextureScale("_BaseMap",Vector2.one);
        bool glow=info.name=="P_Bulb"||info.name=="P_LampGlass";
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",(info.name=="P_Bulb"?new Color(1f,.86f,.62f):new Color(1f,.78f,.48f))*3.2f);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
        else {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }

    static GameObject PoliceVisual(Transform root,Dictionary<string,Material> materials) {
        const string path=PoliceArt+"PoliceStation.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("Generate the police station first: tools/police/build_police_station.py");
        if(importer.importAnimation||importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importLights||importer.importCameras) {
            importer.importAnimation=false;importer.isReadable=false;importer.importLights=false;importer.importCameras=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        visual.name="Police shell and furnishings";visual.transform.SetParent(root,false);
        var nodes=visual.GetComponentsInChildren<Transform>();
        var north=nodes.Single(t=>t.name=="NorthMarker");var east=nodes.Single(t=>t.name=="EastMarker");
        var direction=north.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.forward,Vector3.up),Vector3.up)*visual.transform.localRotation;
        if(Vector3.Distance(root.InverseTransformPoint(north.position),new Vector3(0,0,3))>.01f||Vector3.Distance(root.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Police FBX axis/scale mismatch: north="+root.InverseTransformPoint(north.position)+" east="+root.InverseTransformPoint(east.position));
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m?m.name.Split('.')[0]:"";
                if(!materials.TryGetValue(key,out var mat))throw new Exception("Unmapped police material "+key+" on "+renderer.name);
                return mat;}).ToArray();
            string n=renderer.name;
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass");
            renderer.shadowCastingMode=glass?ShadowCastingMode.Off:ShadowCastingMode.On;
            bool interior=n.StartsWith("Int_")||n.StartsWith("Furn_")||n.StartsWith("Drawer_")||n.StartsWith("Leaf_")||((n.StartsWith("Lamp_")||n.StartsWith("Bulb_"))&&!n.Contains("lantern"));
            if(interior)renderer.renderingLayerMask|=PoliceInteriorLayer;
            renderer.gameObject.isStatic=!(n.StartsWith("Leaf_")||n.StartsWith("Drawer_"));
        }
        return visual;
    }

    static GameObject PoliceBox(Transform parent,string name,Bounds b,int layer=0) {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=b.center;g.isStatic=true;g.layer=layer;
        var c=g.AddComponent<BoxCollider>();c.size=b.size;return g;
    }

    static void PoliceCollision(Transform root,PoliceLayout layout) {
        var group=Group("Police collision",Vector3.zero,root).transform;
        int walls=0;
        foreach(var w in layout.walls)
            foreach(var piece in PoliceLayout.WallPieces(w,layout.openings.Where(o=>o.wall==w.id))){PoliceBox(group,"Wall "+w.id,PoliceLayout.PieceBounds(w,piece));walls++;}
        foreach(var s in layout.slabs) {
            if(s.kind=="ceiling"||s.kind=="structure"||s.kind=="foundation")continue;
            float top=s.y1,bottom=s.kind=="finish"?top-.3f:s.y0;
            PoliceBox(group,"Floor "+s.id,new Bounds(new Vector3((s.x0+s.x1)/2,(top+bottom)/2,(s.z0+s.z1)/2),new Vector3(s.x1-s.x0,top-bottom,s.z1-s.z0)));
        }
        foreach(var s in layout.stairs)
            PoliceBox(group,"Stair "+s.id,new Bounds(new Vector3((s.x0+s.x1)/2,(layout.levels.ground-.02f+s.top)/2,(s.z0+s.z1)/2),new Vector3(s.x1-s.x0,s.top-layout.levels.ground+.02f,s.z1-s.z0)));
        foreach(var c in layout.columns)PoliceBox(group,"Loggia column "+c.id,new Bounds(new Vector3(c.x,(c.y0+c.y1)/2,c.z),new Vector3(.3f,c.y1-c.y0,.3f)));
        foreach(var f in layout.furniture.Where(f=>f.collide)) {
            var g=new GameObject("Furniture "+f.id);g.transform.SetParent(group,false);g.transform.localPosition=new Vector3(f.x,f.y,f.z);g.transform.localRotation=Quaternion.Euler(0,f.yaw,0);g.isStatic=true;
            // Seats block walking at seat height so their sit target stays reachable from above.
            float h=f.kind=="chair"||f.kind=="bench"?Mathf.Min(f.h,.45f):f.h;
            var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,h/2,0);c.size=new Vector3(f.w,h,f.d);
        }
        // Window apertures keep the player in without blocking inspection rays (Ignore Raycast).
        foreach(var o in layout.openings.Where(o=>o.kind=="window")) {
            var w=layout.Wall(o.wall);var b=PoliceLayout.PieceBounds(w,new Vector4(o.at-o.width/2,o.at+o.width/2,o.bottom,o.top));
            b.size=w.axis=="x"?new Vector3(b.size.x,b.size.y,.08f):new Vector3(.08f,b.size.y,b.size.z);
            PoliceBox(group,"Window safety "+o.id,b,2);
        }
        Debug.Log("POLICE_COLLISION_READY: "+walls+" wall pieces from the shared opening cut");
    }

    static AudioSource PoliceAudio(GameObject owner,string clip,float volume=.55f,float range=12) {
        var audio=owner.AddComponent<AudioSource>();audio.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(PoliceArt+"Sounds/"+clip+".wav");
        if(!audio.clip)throw new Exception("Missing police sound "+clip);
        audio.playOnAwake=false;audio.loop=false;audio.spatialBlend=1;audio.volume=volume;audio.minDistance=1;audio.maxDistance=range;audio.rolloffMode=AudioRolloffMode.Linear;
        return audio;
    }

    static void PoliceDoors(Transform root,GameObject visual,PoliceLayout layout,PoliceBuildManifest manifest) {
        var group=Group("Police doors and drawers",Vector3.zero,root).transform;
        var nodes=visual.GetComponentsInChildren<Transform>(true);
        foreach(var leaf in manifest.leaves) {
            var opening=layout.Opening(leaf.opening);
            var hinge=new Vector3(leaf.hinge[0],leaf.hinge[1],leaf.hinge[2]);
            var centre=new Vector3(leaf.leafCentre[0],leaf.leafCentre[1],leaf.leafCentre[2]);
            if(leaf.axis=="z")centre=new Vector3(leaf.plane,leaf.leafCentre[1],leaf.leafCentre[0]);
            string id=leaf.count==2?leaf.fixture+(leaf.index==0?"-left":"-right"):leaf.fixture;
            var pivot=Group("Door "+id,hinge,group);
            var mesh=nodes.Single(t=>t.name==leaf.@object);mesh.SetParent(pivot.transform,true);
            var local=centre-hinge;local.y=centre.y-hinge.y;
            var size=leaf.axis=="x"?new Vector3(leaf.width-.02f,leaf.height-.02f,.06f):new Vector3(.06f,leaf.height-.02f,leaf.width-.02f);
            var collision=Group("Leaf collision",local,pivot.transform);collision.AddComponent<BoxCollider>().size=size;
            var door=pivot.AddComponent<ShopMechanism>();door.fixtureId=id;door.movingPart=pivot.transform;door.protectPlayer=true;
            // Only the street entrance stands with one leaf open; the yard gate starts shut.
            door.startsOpen=leaf.count==2&&opening.style=="front"?leaf.index==0:opening.startsOpen;
            door.label=opening.label+(leaf.count==2?(leaf.index==0?"（左扇）":"（右扇）"):"");
            door.leafCentre=local;door.leafSize=size;
            // Swing toward the authored side: test the quarter turn instead of trusting axis signs.
            var turned=Quaternion.AngleAxis(90,Vector3.up)*local;
            float side=leaf.axis=="x"?turned.z:turned.x;
            door.openEuler=new Vector3(0,(side*opening.swing>0?1:-1)*88,0);
            bool iron=opening.style=="iron";
            door.sound=PoliceAudio(pivot,iron?"PoliceIronDoor":"PoliceDoorOpen");
            door.openClip=door.sound.clip;door.closeClip=iron?door.sound.clip:AssetDatabase.LoadAssetAtPath<AudioClip>(PoliceArt+"Sounds/PoliceDoorClose.wav");
        }
        var docs=PoliceDocumentSet.Load();
        foreach(var drawer in manifest.drawers) {
            var item=layout.items.Single(i=>i.kind=="drawer"&&i.target=="police-index-drawer-"+drawer.index);
            var front=new Vector3(drawer.front[0],drawer.front[1],drawer.front[2]);var axis=new Vector3(drawer.axis[0],drawer.axis[1],drawer.axis[2]).normalized;
            var pivot=Group("Drawer "+item.title,front,group);
            nodes.Single(t=>t.name==drawer.@object).SetParent(pivot.transform,true);
            var face=Group("Drawer front collision",Vector3.zero,pivot.transform);var box=face.AddComponent<BoxCollider>();
            box.size=Mathf.Abs(axis.x)>.5f?new Vector3(.05f,.68f,.70f):new Vector3(.70f,.68f,.05f);
            var mech=pivot.AddComponent<ShopMechanism>();mech.fixtureId=item.target;mech.movingPart=pivot.transform;mech.openOffset=axis*drawer.travel;
            mech.label=item.title+"索引抽屉";mech.sound=PoliceAudio(pivot,"PoliceDrawer",.45f,8);mech.openClip=mech.closeClip=mech.sound.clip;
            // The index card rides inside the drawer; closed, the front collider is hit first.
            // Readable target over the open tray, rising above the drawer front so a standing
            // player can look over it; enabled only while the drawer is (nearly) fully open.
            var card=Group("Index card · "+item.title,-axis*.23f+Vector3.up*.33f,pivot.transform);
            var trigger=card.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=Mathf.Abs(axis.x)>.5f?new Vector3(.38f,.8f,.62f):new Vector3(.62f,.8f,.38f);
            PoliceDocumentComponent(card,docs.Get(item.doc),"");
            card.AddComponent<OpenDrawerTarget>().drawer=mech;
        }
    }

    static PoliceDocumentItem PoliceDocumentComponent(GameObject target,PoliceDocument doc,string evidence) {
        var item=target.AddComponent<PoliceDocumentItem>();item.documentId=doc.id;item.title=doc.title;item.source=doc.source;item.body=doc.body;item.evidenceId=evidence;
        if(!string.IsNullOrEmpty(doc.image)) {
            string path="Assets/Art/"+doc.image;
            // Same image the station desk shows, loaded as-is (its importer belongs to the rail kit).
            if(doc.image.StartsWith("Rail/"))item.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(RailFolder+doc.image.Substring(5));
            else {
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(!importer)throw new Exception("Missing document image "+path);
                // Typeset papers (tools/police/write_police_texts.py) keep their exact size, uncompressed like the hospital
                // and house sheets: the close-up takes the sheet's proportions from the texture.
                bool paper=doc.image.StartsWith("Police/Papers/");
                if(importer.wrapMode!=TextureWrapMode.Clamp||importer.mipmapEnabled||paper&&(importer.npotScale!=TextureImporterNPOTScale.None||importer.textureCompression!=TextureImporterCompression.Uncompressed||importer.maxTextureSize!=2048)) {
                    importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=false;
                    if(paper){importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;}
                    importer.SaveAndReimport();
                }
                item.illustration=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
        }
        item.paperAudio=PoliceAudio(target,"PolicePaper",.4f,6);
        return item;
    }

    static void PoliceLights(Transform root,GameObject visual,PoliceLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Police lamps",Vector3.zero,root).transform;
        var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);
        foreach(var l in layout.lamps) {
            bool exterior=l.role=="Exterior";
            var light=Point("Police lamp "+l.id,Vector3.zero,exterior?C("#FFD7A0"):l.kind=="desk"?C("#FFD9A6"):C("#FFE2B8"),l.intensity,l.range);
            light.transform.SetParent(group,false);light.transform.localPosition=new Vector3(l.x,l.y-(l.kind=="pendant"?.08f:0),l.z);
            if(!exterior)LayerOnly(light,PoliceInteriorLayer);
            var bulb=renderers.Where(r=>r.name=="Bulb_"+l.id).ToArray();
            if(bulb.Length!=1)throw new Exception("Police lamp needs one emissive bulb: "+l.id);
            controllers[l.id]=DayNightLighting.Bind(light,exterior?DayNightLamp.Role.Exterior:DayNightLamp.Role.Interior,l.access,bulb);
        }
    }

    static void PoliceItems(Transform root,PoliceLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Police materials and seats",Vector3.zero,root).transform;var docs=PoliceDocumentSet.Load();
        foreach(var item in layout.items) {
            if(item.kind=="drawer"||item.kind=="npc")continue;
            if(item.kind=="seat") {
                float targetY=item.y-item.sitY;
                var facing=new Vector3(Mathf.Sin(item.yaw*Mathf.Deg2Rad),0,Mathf.Cos(item.yaw*Mathf.Deg2Rad));
                RestSeat(group,item.id,item.title,new Vector3(item.sitX,item.sitY,item.sitZ),facing,new Vector3(item.standX,item.sitY,item.standZ),item.eye,new Vector3(item.w,item.h,item.d),targetY);
                continue;
            }
            var g=Group("Item · "+item.id,new Vector3(item.x,item.y,item.z),group);g.transform.localRotation=Quaternion.Euler(0,item.yaw,0);
            var trigger=g.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(item.w,item.h,item.d);
            if(item.kind=="evidence"||item.kind=="document") {
                var doc=PoliceDocumentComponent(g,docs.Get(item.doc),item.evidence);
                if(item.Printed)PoliceDeskPaper(root,doc,item);
            }
            else if(item.kind=="report"){var desk=g.AddComponent<PoliceReportDesk>();desk.title=item.title;desk.paperAudio=PoliceAudio(g,"PolicePaper",.4f,6);}
            else if(item.kind=="lampswitch") {
                var lamp=layout.Lamp(item.target);var s=g.AddComponent<LampSwitch>();s.label=item.title;s.offId=lamp.switchId;s.lamps=new[]{controllers[item.target]};
                if(string.IsNullOrEmpty(s.offId))throw new Exception("Lamp switch without saved off ID: "+item.id);
            }
            else throw new Exception("Unknown police item kind "+item.kind);
        }
    }

    // A desk copy of E01 is the typeset sheet itself, as the garage records lie on their desk:
    // EvidenceModel.Paper with EvidencePhysical (the close-up and the briefcase examine this sheet),
    // resting on its support at the layout contract's pose; the shell leaves that support's blank
    // sheet out. Meshes and materials are saved as assets so the prefab keeps them.
    static void PoliceDeskPaper(Transform root,PoliceDocumentItem item,PoliceItem layoutItem) {
        var p=layoutItem.paper;
        var visual=EvidenceModel.Paper("Physical "+item.evidenceId,item.illustration,p.w,p.h);visual.AddComponent<EvidencePhysical>();
        visual.transform.SetParent(item.transform,false);visual.transform.SetPositionAndRotation(root.TransformPoint(p.Position),root.rotation*p.Rotation);
        const string folder="Assets/Generated/Police/";Directory.CreateDirectory(folder);int n=0;
        foreach(var f in visual.GetComponentsInChildren<MeshFilter>()) {
            string path=folder+"Paper_"+layoutItem.id+"_"+n+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh){EditorUtility.CopySerialized(f.sharedMesh,mesh);UnityEngine.Object.DestroyImmediate(f.sharedMesh);}else{mesh=f.sharedMesh;AssetDatabase.CreateAsset(mesh,path);}f.sharedMesh=mesh;EditorUtility.SetDirty(mesh);
            var renderer=f.GetComponent<MeshRenderer>();string mp=folder+"Paper_"+layoutItem.id+"_"+n+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);
            if(material){EditorUtility.CopySerialized(renderer.sharedMaterial,material);UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);}else{material=renderer.sharedMaterial;AssetDatabase.CreateAsset(material,mp);}
            renderer.sharedMaterial=material;renderer.renderingLayerMask|=PoliceInteriorLayer;EditorUtility.SetDirty(material);n++;
        }
    }

    // Police-only rebuild after a layout/shell change. The Town scene holds this prefab as an
    // instance with transform overrides only, so it follows without regenerating the town.
    public static void RebuildPolice() {
        AssetDatabase.Refresh();PolicePrefab();AssetDatabase.SaveAssets();
    }
    static void PolicePrefab() {
        var layoutAsset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/PoliceLayout.json");
        var layout=PoliceLayout.Parse(layoutAsset.text);
        var manifest=JsonUtility.FromJson<PoliceBuildManifest>(AssetDatabase.LoadAssetAtPath<TextAsset>(PoliceArt+"PoliceBuildManifest.json").text);
        var materialList=JsonUtility.FromJson<PoliceMaterialList>(AssetDatabase.LoadAssetAtPath<TextAsset>(PoliceArt+"PoliceBuildManifest.json").text);
        var materials=materialList.materials.ToDictionary(m=>m.name,PoliceMaterial);
        var root=new GameObject("B10 警署 · D内院回廊");
        root.AddComponent<PoliceStation>().layoutJson=layoutAsset;
        var visual=PoliceVisual(root.transform,materials);
        PoliceCollision(root.transform,layout);
        PoliceDoors(root.transform,visual,layout,manifest);
        var controllers=new Dictionary<string,DayNightLamp>();
        PoliceLights(root.transform,visual,layout,controllers);
        PoliceItems(root.transform,layout,controllers);
        PoliceCastAuthoring.AddToPolice(root);
        // Rain clipping and per-room acoustic enclosures; loggias are roofed but open.
        var doors=root.GetComponentsInChildren<ShopMechanism>().Where(d=>d.protectPlayer).ToArray();
        foreach(var s in layout.shelters) {
            var g=Group("Rain shelter "+s.id,Vector3.zero,root.transform);
            WeatherAuthoring.Shelter(g,new Vector2(s.x1-s.x0,s.z1-s.z0),s.eaves,s.rise,new Vector2((s.x0+s.x1)/2,(s.z0+s.z1)/2));
            var shelter=g.GetComponent<WeatherShelter>();shelter.ridgeAlongX=s.ridgeAlongX;shelter.acousticallyOpen=s.open;
            shelter.exteriorDoors=doors.Where(d=>(s.doors??new string[0]).Any(f=>d.fixtureId==f||d.fixtureId.StartsWith(f+"-"))).ToArray();
            if((s.doors??new string[0]).Length>0&&shelter.exteriorDoors.Length==0)throw new Exception("Shelter without its doors: "+s.id);
        }
        OpeningAuthoring.AddToPolice(root);
        PoliceChecks.Validate(root,layout,manifest);
        PrefabUtility.SaveAsPrefabAsset(root,PolicePrefabPath);UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("POLICE_PREFAB_PASS: "+PolicePrefabPath);
    }

    static GameObject PoliceTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PolicePrefabPath));
        var layout=root.GetComponent<PoliceStation>().Layout;
        root.transform.position=layout.Origin;MapMarker(root,b,true);
        var door=layout.Opening("front-door");
        RailBoard(root.transform,"青 川 警 署",new Vector3(door.at,3.95f,-.06f),new Vector2(2.4f,.56f),Quaternion.identity);
        // Court benches from the town kit, facing the court tree.
        int index=0;
        foreach(var (pos,dir) in new[]{(new Vector3(8.7f,layout.levels.court,18.4f),Vector3.right),(new Vector3(14.3f,layout.levels.court,18.4f),Vector3.left)}) {
            var bench=Model("Bench",root.transform.TransformPoint(pos));bench.transform.SetParent(root.transform,true);
            FaceBench(bench,dir);Collider(bench,new Vector3(0,.45f,0),new Vector3(2,.9f,.75f));
            var seat=bench.AddComponent<FurnitureSeat>();seat.seatId="police-court-bench-"+index++;seat.title="坐下休息 · 内院长椅";seat.eyeHeight=1.10f;
            var facing=bench.transform.Find("Seat facing");
            seat.sittingPoint=Group("Resting pose",new Vector3(0,.025f,0),bench.transform).transform;seat.sittingPoint.localRotation=facing.localRotation;
            seat.standingPoint=Group("Clear standing aisle",facing.localRotation*Vector3.forward*.95f+Vector3.up*.025f,bench.transform).transform;seat.standingPoint.localRotation=facing.localRotation;
        }
        return root;
    }
    static void FaceBench(GameObject bench,Vector3 toward) {
        float backDepth=0;int vertices=0;
        foreach(var filter in bench.GetComponentsInChildren<MeshFilter>())
            foreach(var vertex in filter.sharedMesh.vertices) {
                var local=bench.transform.InverseTransformPoint(filter.transform.TransformPoint(vertex));
                if(local.y<.55f)continue;backDepth+=local.z;vertices++;
            }
        if(vertices==0||Mathf.Abs(backDepth/vertices)<.05f)throw new Exception("Cannot identify the imported bench backrest");
        var facing=Group("Seat facing",Vector3.zero,bench.transform).transform;facing.localRotation=Quaternion.Euler(0,backDepth>0?180:0,0);
        bench.transform.rotation=Quaternion.FromToRotation(facing.forward,toward);
    }
    static void PoliceCourtTree() {
        var station=UnityEngine.Object.FindFirstObjectByType<PoliceStation>();if(!station)throw new Exception("Police station missing for its court tree");
        var bed=station.Layout.furniture.Single(f=>f.id=="court-planter");
        GreeneryAuthoring.ExtraTree(station.transform,"Tree3",new Vector3(bed.x,bed.y+.24f,bed.z),2.6f,31,"Police court tree");
    }
}
