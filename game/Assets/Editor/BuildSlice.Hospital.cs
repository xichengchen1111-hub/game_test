using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Qingchuan cottage hospital (B11), built 2026-10-14 for the second act. Like the police
// station, one shared prefab is both the street exterior and the walkable interior: the
// Blender shell (tools/hospital/build_hospital.py) and this assembler read the same layout
// contract (Resources/HospitalLayout.json, in the police contract's schema). Doors use
// ShopMechanism hinges with sweep protection; every room shares the town clock, weather,
// photocell lamps and saves.
public static partial class BuildSlice {
    // The hospital's room lamps share the police interior light layer. The two buildings stand
    // ninety metres apart, far outside any lamp's range, so no further light layer is spent.
    public const uint HospitalInteriorLayer=PoliceInteriorLayer;
    const string HospitalArt="Assets/Art/Hospital/";
    const string HospitalPrefabPath="Assets/Generated/Hospital.prefab";
    const string HospitalMaterials="Assets/Generated/Materials/Hospital/";

    static Material HospitalMaterial(PoliceMaterialInfo info) {
        Directory.CreateDirectory(HospitalMaterials);
        string path=HospitalMaterials+info.name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        bool glass=info.name=="H_Glass";
        var shader=Shader.Find(glass?"WrongDate/ClearGlass":"Universal Render Pipeline/Lit");
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=shader)m.shader=shader;
        if(glass){m.SetColor("_Tint",new Color(.55f,.68f,.78f,.06f));EditorUtility.SetDirty(m);return m;}
        // The original police textures carry these families; plaster takes its colour from the tint.
        string texture=info.name switch {
            "HExt_Render"=>"PolicePlaster","HInt_Wall"=>"PolicePlaster","HInt_Ceiling"=>"PolicePlaster",
            "HExt_Stone"=>"PoliceAshlar","HExt_Slate"=>"PoliceSlate","HInt_Wood"=>"PoliceOak","HInt_FurnLight"=>"PoliceOak","HInt_Tiles"=>"PoliceTiles",
            "HExt_Timber"=>"PoliceDarkOak","HInt_Joinery"=>"PoliceDarkOak","HInt_Furniture"=>"PoliceDarkOak",
            _=>null};
        Color tint=C("#"+info.hex);
        if(texture!=null) {
            if(texture!="PolicePlaster")tint=info.name switch {"HInt_FurnLight"=>new Color(.92f,.9f,.86f),"HInt_Tiles"=>new Color(.95f,.93f,.88f),_=>Color.white};
            m.SetTexture("_BaseMap",PoliceTexture(texture+"_Base",false));
            m.SetTexture("_BumpMap",PoliceTexture(texture+"_Normal",true));m.SetFloat("_BumpScale",texture=="PolicePlaster"?.3f:.75f);m.EnableKeyword("_NORMALMAP");
        } else {m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",info.metallic);m.SetFloat("_Smoothness",Mathf.Clamp01(1-info.roughness)*.85f);
        m.SetTextureScale("_BaseMap",Vector2.one);
        bool glow=info.name=="H_Bulb"||info.name=="H_LampGlass";
        if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",(info.name=="H_Bulb"?new Color(1f,.9f,.72f)*2.4f:new Color(1f,.78f,.48f)*3.2f));m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
        else {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
        m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }

    static GameObject HospitalVisual(Transform root,Dictionary<string,Material> materials) {
        const string path=HospitalArt+"Hospital.fbx";
        var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new Exception("Generate the hospital first: tools/hospital/build_hospital.py");
        if(importer.importAnimation||importer.isReadable||importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||importer.importLights||importer.importCameras) {
            importer.importAnimation=false;importer.isReadable=false;importer.importLights=false;importer.importCameras=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
        }
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.UnpackPrefabInstance(visual,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        visual.name="Hospital shell and furnishings";visual.transform.SetParent(root,false);
        var nodes=visual.GetComponentsInChildren<Transform>();
        var north=nodes.Single(t=>t.name=="NorthMarker");var east=nodes.Single(t=>t.name=="EastMarker");
        var direction=north.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.forward,Vector3.up),Vector3.up)*visual.transform.localRotation;
        if(Vector3.Distance(root.InverseTransformPoint(north.position),new Vector3(0,0,3))>.01f||Vector3.Distance(root.InverseTransformPoint(east.position),new Vector3(3,0,0))>.01f)
            throw new Exception("Hospital FBX axis/scale mismatch: north="+root.InverseTransformPoint(north.position)+" east="+root.InverseTransformPoint(east.position));
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>()) {
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                string key=m?m.name.Split('.')[0]:"";
                if(!materials.TryGetValue(key,out var mat))throw new Exception("Unmapped hospital material "+key+" on "+renderer.name);
                return mat;}).ToArray();
            string n=renderer.name;
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass");
            renderer.shadowCastingMode=glass?ShadowCastingMode.Off:ShadowCastingMode.On;
            bool interior=n.StartsWith("Int_")||n.StartsWith("Furn_")||n.StartsWith("Leaf_")||((n.StartsWith("Lamp_")||n.StartsWith("Bulb_"))&&!n.Contains("lantern"));
            if(interior)renderer.renderingLayerMask|=HospitalInteriorLayer;
            renderer.gameObject.isStatic=!n.StartsWith("Leaf_");
        }
        return visual;
    }

    static void HospitalCollision(Transform root,PoliceLayout layout) {
        var group=Group("Hospital collision",Vector3.zero,root).transform;
        int walls=0;
        foreach(var w in layout.walls)
            foreach(var piece in PoliceLayout.WallPieces(w,layout.openings.Where(o=>o.wall==w.id))){PoliceBox(group,"Wall "+w.id,PoliceLayout.PieceBounds(w,piece));walls++;}
        foreach(var s in layout.slabs) {
            if(s.kind=="ceiling"||s.kind=="foundation")continue;
            float top=s.y1,bottom=s.kind=="finish"?top-.3f:s.y0;
            PoliceBox(group,"Floor "+s.id,new Bounds(new Vector3((s.x0+s.x1)/2,(top+bottom)/2,(s.z0+s.z1)/2),new Vector3(s.x1-s.x0,top-bottom,s.z1-s.z0)));
        }
        // The floor runs through every doorway: room finishes stop at the wall faces.
        foreach(var o in layout.openings.Where(o=>o.kind=="door"||o.kind=="arch")) {
            var w=layout.Wall(o.wall);var b=PoliceLayout.PieceBounds(w,new Vector4(o.at-o.width/2,o.at+o.width/2,o.bottom-.3f,o.bottom));
            PoliceBox(group,"Sill "+o.id,b);
        }
        foreach(var c in layout.columns)PoliceBox(group,"Hood post "+c.id,new Bounds(new Vector3(c.x,(c.y0+c.y1)/2,c.z),new Vector3(c.w+.04f,c.y1-c.y0,c.w+.04f)));
        foreach(var f in layout.furniture.Where(f=>f.collide&&f.kind!="overbed")) {
            var g=new GameObject("Furniture "+f.id);g.transform.SetParent(group,false);g.transform.localPosition=new Vector3(f.x,f.y,f.z);g.transform.localRotation=Quaternion.Euler(0,f.yaw,0);g.isStatic=true;
            // Seats and beds block walking at their own height; a palm is its pot.
            float h=f.kind=="chair"||f.kind=="bench"||f.kind=="smallchair"?Mathf.Min(f.h,.45f):f.kind=="wardbed"?.64f:f.kind=="palm"?.4f:f.h;
            float w=f.kind=="palm"?.42f:f.w,d=f.kind=="palm"?.42f:f.d;
            var c=g.AddComponent<BoxCollider>();c.center=new Vector3(0,h/2,0);c.size=new Vector3(w,h,d);
        }
        // Window apertures keep the player in without blocking inspection rays (Ignore Raycast).
        foreach(var o in layout.openings.Where(o=>o.kind=="window"||o.kind=="hatch")) {
            var w=layout.Wall(o.wall);var b=PoliceLayout.PieceBounds(w,new Vector4(o.at-o.width/2,o.at+o.width/2,o.bottom,o.top));
            b.size=w.axis=="x"?new Vector3(b.size.x,b.size.y,.08f):new Vector3(.08f,b.size.y,b.size.z);
            PoliceBox(group,(o.kind=="window"?"Window safety ":"Hatch safety ")+o.id,b,2);
        }
        Debug.Log("HOSPITAL_COLLISION_READY: "+walls+" wall pieces from the shared opening cut");
    }

    static void HospitalDoors(Transform root,GameObject visual,PoliceLayout layout,PoliceBuildManifest manifest) {
        var group=Group("Hospital doors",Vector3.zero,root).transform;
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
            // The street entrance stands with one leaf open; ward doors stand open by day.
            door.startsOpen=leaf.count==2&&opening.style=="front"?leaf.index==0:opening.startsOpen;
            door.label=opening.label+(leaf.count==2?(leaf.index==0?"（左扇）":"（右扇）"):"");
            door.leafCentre=local;door.leafSize=size;
            // Swing toward the authored side: test the quarter turn instead of trusting axis signs.
            var turned=Quaternion.AngleAxis(90,Vector3.up)*local;
            float side=leaf.axis=="x"?turned.z:turned.x;
            door.openEuler=new Vector3(0,(side*opening.swing>0?1:-1)*88,0);
            door.sound=PoliceAudio(pivot,"PoliceDoorOpen");
            door.openClip=door.sound.clip;door.closeClip=AssetDatabase.LoadAssetAtPath<AudioClip>(PoliceArt+"Sounds/PoliceDoorClose.wav");
        }
    }

    static void HospitalLights(Transform root,GameObject visual,PoliceLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Hospital lamps",Vector3.zero,root).transform;
        var renderers=visual.GetComponentsInChildren<MeshRenderer>(true);
        foreach(var l in layout.lamps) {
            bool exterior=l.role=="Exterior";
            // Ward pendants are a warmer, lower-key opal glow than the offices (the visit is at night).
            bool ward=l.kind=="pendant"&&(l.room=="children"||l.room=="general");
            var light=Point("Hospital lamp "+l.id,Vector3.zero,exterior?C("#FFD7A0"):l.kind=="desk"?C("#FFD9A6"):l.kind=="oplamp"?C("#FFF4E2"):ward?C("#FFD8A8"):C("#FFE9C8"),l.intensity,l.range);
            light.transform.SetParent(group,false);light.transform.localPosition=new Vector3(l.x,l.y-(l.kind=="pendant"?.08f:0),l.z);
            if(!exterior)LayerOnly(light,HospitalInteriorLayer);
            var bulb=renderers.Where(r=>r.name=="Bulb_"+l.id).ToArray();
            if(bulb.Length!=1)throw new Exception("Hospital lamp needs one emissive bulb: "+l.id);
            controllers[l.id]=DayNightLighting.Bind(light,exterior?DayNightLamp.Role.Exterior:DayNightLamp.Role.Interior,l.access,bulb);
        }
    }

    // Papers of the hospital records (tools/hospital/evidence.py), imported uncompressed like the house sheets.
    static Texture2D HospitalPaper(string name) {
        string path=HospitalArt+"Papers/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)return null;
        if(importer.npotScale!=TextureImporterNPOTScale.None||importer.mipmapEnabled||importer.textureCompression!=TextureImporterCompression.Uncompressed){importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;importer.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void HospitalItems(Transform root,PoliceLayout layout,Dictionary<string,DayNightLamp> controllers) {
        var group=Group("Hospital seats and switches",Vector3.zero,root).transform;
        var records=JsonUtility.FromJson<HomeRecords>(File.ReadAllText("Assets/Resources/HospitalEvidence.json")).entries;
        foreach(var item in layout.items) {
            if(item.kind=="npc"||item.kind=="person")continue;       // placed by HospitalCastAuthoring
            if(item.kind=="seat") {
                float targetY=item.y-item.sitY;
                var facing=new Vector3(Mathf.Sin(item.yaw*Mathf.Deg2Rad),0,Mathf.Cos(item.yaw*Mathf.Deg2Rad));
                RestSeat(group,item.id,item.title,new Vector3(item.sitX,item.sitY,item.sitZ),facing,new Vector3(item.standX,item.sitY,item.standZ),item.eye,new Vector3(item.w,item.h,item.d),targetY);
                continue;
            }
            var g=Group("Item · "+item.id,new Vector3(item.x,item.y,item.z),group);g.transform.localRotation=Quaternion.Euler(0,item.yaw,0);
            var trigger=g.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(item.w,item.h,item.d);
            if(item.kind=="evidence") {
                // The visible paper is the shell's own prop (open register, stub book, drawing); this is its reading target.
                var record=records.Single(r=>r.id==item.doc);var paper=g.AddComponent<Interactable>();
                paper.evidenceId=item.evidence;paper.title=record.title;paper.source=record.source;paper.body=record.body;
                paper.illustration=HospitalPaper(record.id);paper.reverse=HospitalPaper(record.id+"-back");
                if(!paper.illustration||paper.evidenceId!=record.id)throw new Exception("Hospital record without its paper: "+item.id);
                continue;
            }
            if(item.kind=="lampswitch") {
                var lamp=layout.Lamp(item.target);var s=g.AddComponent<LampSwitch>();s.label=item.title;s.offId=lamp.switchId;s.lamps=new[]{controllers[item.target]};
                if(string.IsNullOrEmpty(s.offId))throw new Exception("Lamp switch without saved off ID: "+item.id);
            }
            else throw new Exception("Unknown hospital item kind "+item.kind);
        }
    }

    // Painted lettering on the enamel sign boards. Depth-tested world text, so it never shows through walls.
    static void HospitalLettering(Transform root,PoliceLayout layout) {
        var group=Group("Hospital lettering",Vector3.zero,root).transform;
        var letters=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");if(!letters)throw new Exception("Font not imported");
        const string path=HospitalArt+"SignText.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("WrongDate/RailWorldText"));AssetDatabase.CreateAsset(material,path);}
        foreach(var f in layout.furniture.Where(f=>f.kind=="signboard"&&!string.IsNullOrEmpty(f.extra))) {
            var facing=new Vector3(Mathf.Sin(f.yaw*Mathf.Deg2Rad),0,Mathf.Cos(f.yaw*Mathf.Deg2Rad));
            var sign=Group("Lettering · "+f.id,new Vector3(f.x,f.y+f.h/2,f.z)+facing*(f.d/2+.004f),group);
            sign.transform.localRotation=Quaternion.Euler(0,f.yaw+180,0);
            var tm=sign.AddComponent<TextMesh>();tm.text=f.extra;tm.font=letters;tm.fontSize=64;tm.characterSize=.02f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.93f,.9f,.8f);
            letters.RequestCharactersInTexture(f.extra,64);material.mainTexture=letters.material.mainTexture;
            var renderer=sign.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            renderer.renderingLayerMask|=HospitalInteriorLayer;
            var lettering=sign.AddComponent<WallLettering>();lettering.font=letters;lettering.material=material;
        }
        EditorUtility.SetDirty(material);
    }

    // Hospital-only rebuild after a layout/shell/cast change. The Town scene holds this prefab as an
    // instance with transform overrides only, so it follows without regenerating the town.
    public static void RebuildHospital() {
        AssetDatabase.Refresh();HospitalPrefab();AssetDatabase.SaveAssets();
    }
    // Whole-game regeneration into a side folder: the town scene is rebuilt with the hospital in it,
    // without touching the player the user has on the desktop.
    public static void BuildHospitalStaging() {
        string company=PlayerSettings.companyName,id=PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
        try {
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.qingchuanstudio.wrongdatetest.currentmac");
            BuildFor(BuildTarget.StandaloneOSX,"../builds/hospital/staging/医院验收.app",new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal},1,"Qingchuan Fresh 20261008");
        } finally {
            PlayerSettings.companyName=company;PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,id);AssetDatabase.SaveAssets();
        }
    }
    static void HospitalPrefab() {
        var layoutAsset=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/HospitalLayout.json");
        var layout=PoliceLayout.Parse(layoutAsset.text);
        var manifestAsset=AssetDatabase.LoadAssetAtPath<TextAsset>(HospitalArt+"HospitalBuildManifest.json");
        if(!manifestAsset)throw new Exception("Generate the hospital first: tools/hospital/build_hospital.py");
        var manifest=JsonUtility.FromJson<PoliceBuildManifest>(manifestAsset.text);
        var materials=JsonUtility.FromJson<PoliceMaterialList>(manifestAsset.text).materials.ToDictionary(m=>m.name,HospitalMaterial);
        var root=new GameObject("B11 青川医院");
        root.AddComponent<HospitalBuilding>().layoutJson=layoutAsset;
        var visual=HospitalVisual(root.transform,materials);
        HospitalCollision(root.transform,layout);
        HospitalDoors(root.transform,visual,layout,manifest);
        var controllers=new Dictionary<string,DayNightLamp>();
        HospitalLights(root.transform,visual,layout,controllers);
        HospitalItems(root.transform,layout,controllers);
        HospitalLettering(root.transform,layout);
        HospitalCastAuthoring.AddToHospital(root);
        // Rain clipping and acoustic enclosures: the front range and each ward keyed to its own outside doors.
        var doors=root.GetComponentsInChildren<ShopMechanism>().Where(d=>d.protectPlayer).ToArray();
        foreach(var s in layout.shelters) {
            var g=Group("Rain shelter "+s.id,Vector3.zero,root.transform);
            WeatherAuthoring.Shelter(g,new Vector2(s.x1-s.x0,s.z1-s.z0),s.eaves,s.rise,new Vector2((s.x0+s.x1)/2,(s.z0+s.z1)/2));
            var shelter=g.GetComponent<WeatherShelter>();shelter.ridgeAlongX=s.ridgeAlongX;shelter.acousticallyOpen=s.open;
            shelter.exteriorDoors=doors.Where(d=>(s.doors??new string[0]).Any(f=>d.fixtureId==f||d.fixtureId.StartsWith(f+"-"))).ToArray();
            if((s.doors??new string[0]).Length>0&&shelter.exteriorDoors.Length==0)throw new Exception("Shelter without its doors: "+s.id);
        }
        HospitalChecks.Validate(root,layout,manifest);
        ZFightRepair.Repair(root.transform,"Hospital");
        PrefabUtility.SaveAsPrefabAsset(root,HospitalPrefabPath);UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("HOSPITAL_PREFAB_PASS: "+HospitalPrefabPath);
    }

    static GameObject HospitalTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HospitalPrefabPath));
        var layout=root.GetComponent<HospitalBuilding>().Layout;
        root.transform.position=layout.Origin;MapMarker(root,b,true);
        var door=layout.Opening("front-door");
        // Name board on the entrance gable, between the door hood and the roof vent.
        RailBoard(root.transform,"青 川 医 院",new Vector3(door.at,4.56f,-.06f),new Vector2(3.0f,.52f),Quaternion.identity);
        return root;
    }
}
