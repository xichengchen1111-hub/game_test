using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// 1931 street furniture (user-approved concept round docs/concepts/street-items/round-01, 2026-10-16):
// ten kinds, 41 placements. Positions, facings and footprints come only from the map
// (docs/maps/town-current/build_plan.py -> plan-data.json -> TownPlan.json streetItems); the meshes come
// from tools/street_items/build_street_items.py (Art/StreetItems/StreetItems.fbx, Item_<kind> with
// Front_/Top_ axis markers). Pure dressing: no interaction, evidence, clock, text clue or light source;
// the fire-alarm globe is an emissive surface on the shared photocell curve, outside the light-pool budget.
public static partial class BuildSlice {
    const string StreetItemArt="Assets/Art/StreetItems/";
    const string StreetItemMaterials="Assets/Generated/Materials/StreetItems/";
    [Serializable] class StreetKindInfo { public string kind, @object; public int triangles; public string[] materials; public float[] footprint; public float height; }
    [Serializable] class StreetMaterialInfo { public string name; public float[] rgb; }
    [Serializable] class StreetManifest { public int version, triangles; public StreetKindInfo[] kinds; public StreetMaterialInfo[] materials; }
    static readonly string[] StreetKinds={"hydrant","mailbox","alarm","trough","hitch","column","litter","sandbin","shoeshine","treeguard"};
    static readonly Dictionary<string,Material> streetMats=new Dictionary<string,Material>();

    static Texture2D StreetTexture(string file,bool normal) {
        string path=StreetItemArt+file;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing street item texture "+path+" (tools/street_items/street_item_art.py)");
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        if(importer.textureType!=type||importer.sRGBTexture==normal||!importer.mipmapEnabled||importer.anisoLevel!=8||importer.wrapMode!=TextureWrapMode.Repeat) {
            importer.textureType=type;importer.sRGBTexture=!normal;importer.mipmapEnabled=true;importer.anisoLevel=8;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material StreetMaterial(StreetMaterialInfo info) {
        if(streetMats.TryGetValue(info.name,out var cached))return cached;
        Directory.CreateDirectory(StreetItemMaterials);
        string path=StreetItemMaterials+info.name+".mat";var shader=Shader.Find("Universal Render Pipeline/Lit");
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else if(m.shader!=shader)m.shader=shader;
        string n=info.name;
        // Blender palette values are linear; Unity material colours are authored in sRGB.
        var linear=new Color(info.rgb[0],info.rgb[1],info.rgb[2]);var tint=linear.gamma;
        bool image=n=="SI_Posters"||n.StartsWith("SI_Plate");
        bool metal=n=="SI_Brass"||n=="SI_Bronze"||n=="SI_Galvanised"||n=="SI_GoldLeaf"||n=="SI_Tin";
        bool stone=n.StartsWith("SI_Granite")||n=="SI_Carved";
        bool wood=n=="SI_BinWood"||n=="SI_Walnut"||n=="SI_StandGreen"||n=="SI_Handle";
        string detail=image?null:stone?"SI_Granite":wood?"SI_Wood":n=="SI_Water"||n=="SI_Glass"||n=="SI_RedGlobe"||n=="SI_Dark"||n=="SI_Soil"?null:"SI_Paint";
        m.SetTexture("_BaseMap",image?StreetTexture(n+".png",false):detail!=null?StreetTexture(detail+"_Base.png",false):null);
        m.SetTexture("_BumpMap",detail!=null?StreetTexture(detail+"_Normal.png",true):null);
        if(detail!=null){m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",stone?.8f:.6f);}else m.DisableKeyword("_NORMALMAP");
        m.SetColor("_BaseColor",image?Color.white:tint);
        m.SetFloat("_Metallic",metal?1:n=="SI_HydrantSilver"||n=="SI_ZincCap"?.45f:0);
        m.SetFloat("_Smoothness",n=="SI_Water"?.94f:n=="SI_Glass"?.92f:n=="SI_RedGlobe"?.8f:metal?.55f:n=="SI_Leather"?.42f:image?.28f:stone?.22f:wood?.18f:n=="SI_Soil"?0:.36f);
        if(n=="SI_RedGlobe"){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1f,.28f,.16f)*2.2f);m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;}
        else {m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);}
        m.enableInstancing=true;EditorUtility.SetDirty(m);streetMats[n]=m;return m;
    }
    static float StreetGround(TownPlan plan,float x,float z) {
        // Highest authored walk/yard triangle under the footprint centre (all items stand on paving).
        float best=float.NegativeInfinity;
        foreach(var s in plan.surfaces){var v=s.vertices;for(int i=0;i+8<v.Length;i+=9) {
            Vector2 a=new Vector2(v[i],v[i+2]),b=new Vector2(v[i+3],v[i+5]),c=new Vector2(v[i+6],v[i+8]),p=new Vector2(x,z);
            float d1=(p.x-b.x)*(a.y-b.y)-(a.x-b.x)*(p.y-b.y),d2=(p.x-c.x)*(b.y-c.y)-(b.x-c.x)*(p.y-c.y),d3=(p.x-a.x)*(c.y-a.y)-(c.x-a.x)*(p.y-a.y);
            bool neg=d1<0||d2<0||d3<0,pos=d1>0||d2>0||d3>0;
            if(!(neg&&pos))best=Mathf.Max(best,(v[i+1]+v[i+4]+v[i+7])/3);
        }}
        if(float.IsNegativeInfinity(best))throw new Exception("Street item stands on no authored paving at "+x+","+z);
        return best;
    }
    static void StreetColliders(GameObject g,string kind) {
        void Cap(Vector3 c,float r,float h){var k=g.AddComponent<CapsuleCollider>();k.center=c;k.radius=r;k.height=h;}
        void Box(Vector3 c,Vector3 s){var k=g.AddComponent<BoxCollider>();k.center=c;k.size=s;}
        switch(kind) {
            case "hydrant":Cap(new Vector3(0,.375f,0),.19f,.75f);break;
            case "mailbox":Box(new Vector3(0,.65f,0),new Vector3(.48f,1.3f,.44f));break;
            case "alarm":Cap(new Vector3(0,1.1f,0),.2f,2.2f);break;
            case "trough":Box(new Vector3(0,.34f,-.03f),new Vector3(1.8f,.68f,.8f));Box(new Vector3(0,.95f,.33f),new Vector3(.76f,.9f,.2f));Cap(new Vector3(-.96f,.5f,.1f),.12f,1f);break;
            case "hitch":Cap(new Vector3(-.6f,.57f,0),.09f,1.14f);Box(new Vector3(.3f,.2f,.02f),new Vector3(.75f,.4f,.42f));break;
            case "column":Cap(new Vector3(0,1.7f,0),.62f,3.4f);break;
            case "litter":Cap(new Vector3(0,.42f,0),.26f,.85f);break;
            case "sandbin":Box(new Vector3(0,.4f,0),new Vector3(1.04f,.8f,.66f));break;
            case "shoeshine":Box(new Vector3(0,.25f,.13f),new Vector3(.8f,.5f,.95f));Box(new Vector3(0,.1f,-.49f),new Vector3(.8f,.2f,.32f));Box(new Vector3(0,.95f,.23f),new Vector3(.66f,.9f,.5f));break;
            case "treeguard":Cap(new Vector3(0,.78f,0),.31f,1.55f);break;
            default:throw new Exception("Unknown street item kind "+kind);
        }
    }
    static void StreetItemsTown(TownPlan plan) {
        var manifest=JsonUtility.FromJson<StreetManifest>(Resources.Load<TextAsset>("StreetItemsManifest").text);
        var kinds=manifest.kinds.ToDictionary(k=>k.kind);
        if(!StreetKinds.All(kinds.ContainsKey)||kinds.Count!=10)throw new Exception("Street item kit kinds differ from the approved ten (tools/street_items/build_street_items.py)");
        var materials=manifest.materials.ToDictionary(m=>m.name,StreetMaterial);
        const string fbx=StreetItemArt+"StreetItems.fbx";
        var importer=AssetImporter.GetAtPath(fbx) as ModelImporter;
        if(!importer)throw new Exception("Generate the street item kit first: tools/BuildStreetItems.ps1");
        if(importer.importAnimation||importer.isReadable||importer.importLights||importer.importCameras) {
            importer.importAnimation=false;importer.isReadable=false;importer.importLights=false;importer.importCameras=false;importer.SaveAndReimport();
        }
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        var root=Group("1931 street furniture",Vector3.zero);
        foreach(var it in plan.streetItems) {
            var info=kinds[it.kind];
            var place=Group(it.id+" "+it.kind,new Vector3(it.point[0],StreetGround(plan,it.point[0],it.point[1]),it.point[1]),root.transform);
            place.transform.rotation=Quaternion.Euler(0,TownPlan.Yaw(it.facing),0);place.isStatic=true;
            var source=model.transform.Find(info.@object);if(!source)throw new Exception("FBX lacks "+info.@object);
            var item=UnityEngine.Object.Instantiate(source.gameObject,place.transform,false);item.name=info.@object;
            item.transform.localPosition=Vector3.zero;item.transform.localScale=source.localScale;item.transform.localRotation=source.localRotation;
            // Axis markers: the street side must become local -Z (facing yaw 0 = south), up +Y, 1 m apart.
            var front=item.transform.Find("Front_"+it.kind);var top=item.transform.Find("Top_"+it.kind);
            Vector3 F(Transform t)=>place.transform.InverseTransformPoint(t.position);
            Vector3 f=F(front),u=F(top);
            var correction=Quaternion.LookRotation(Vector3.back,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(f.normalized,u.normalized));
            item.transform.localRotation=correction*item.transform.localRotation;
            f=F(front);u=F(top);
            if(Vector3.Distance(f,Vector3.back)>.01f||Vector3.Distance(u,Vector3.up)>.01f)throw new Exception("Street item FBX axis/scale mismatch "+it.kind+": front="+f+" top="+u);
            UnityEngine.Object.DestroyImmediate(front.gameObject);UnityEngine.Object.DestroyImmediate(top.gameObject);
            foreach(var r in item.GetComponentsInChildren<MeshRenderer>()) {
                r.sharedMaterials=r.sharedMaterials.Select(m=>{string key=m?m.name.Split('.')[0].Replace(" (Instance)",""):"";
                    if(!materials.TryGetValue(key,out var mat))throw new Exception("Unmapped street item material "+key+" on "+it.kind);return mat;}).ToArray();
                r.shadowCastingMode=ShadowCastingMode.On;r.gameObject.isStatic=true;
                if(it.kind=="alarm") {
                    // Night beacon only: same photocell curve as the street lamps, no Light, not a pooled lamp.
                    var lamp=r.gameObject.AddComponent<DayNightLamp>();lamp.role=DayNightLamp.Role.Exterior;lamp.source=null;
                    lamp.glowingSurfaces=new Renderer[]{r};lamp.sharesStreetPoolBudget=false;lamp.daylightAccess=1;
                }
            }
            StreetColliders(place,it.kind);
            var marker=place.AddComponent<StreetItem>();marker.id=it.id;marker.kind=it.kind;marker.facing=it.facing;marker.footprint=new Vector2(it.width,it.depth);marker.height=it.height;
        }
        StreetItemChecks(plan,root,manifest);
    }
    static void StreetItemChecks(TownPlan plan,GameObject root,StreetManifest manifest) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Street items: "+why);}
        var placed=root.GetComponentsInChildren<StreetItem>();
        Need(placed.Length==41&&placed.Select(p=>p.id).Distinct().Count()==41,"41 unique placements");
        Need(StreetKinds.All(k=>placed.Any(p=>p.kind==k)),"all ten kinds placed");
        Need(!root.GetComponentsInChildren<Light>().Any(),"street furniture must not add a light source");
        Need(!root.GetComponentsInChildren<Interactable>().Any(),"street furniture must not carry interaction or evidence");
        Need(root.GetComponentsInChildren<DayNightLamp>().Length==placed.Count(p=>p.kind=="alarm"),"one photocell globe per fire alarm");
        int triangles=0;
        foreach(var p in placed) {
            var it=plan.streetItems.Single(i=>i.id==p.id);
            Need(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(it.point[0],it.point[1]))<.001f,"position drift "+p.id);
            var renderers=p.GetComponentsInChildren<MeshRenderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            triangles+=p.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);
            // Every kit material survives the join as a non-empty submesh (2026-10-16: a slot merge once collapsed all faces to slot 0).
            var info=manifest.kinds.Single(k=>k.kind==p.kind);var renderer=renderers.Single();var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            Need(renderer.sharedMaterials.Select(m=>m.name).OrderBy(n=>n).SequenceEqual(info.materials.OrderBy(n=>n))&&mesh.subMeshCount==info.materials.Length,"materials "+p.id+": "+string.Join(",",renderer.sharedMaterials.Select(m=>m.name)));
            for(int sub=0;sub<mesh.subMeshCount;sub++)Need(mesh.GetSubMesh(sub).indexCount>0,"empty submesh "+sub+" on "+p.id);
            if(p.kind=="alarm")Need(renderer.sharedMaterials.Any(m=>m.name=="SI_RedGlobe"&&m.IsKeywordEnabled("_EMISSION")),"alarm globe lacks its emissive glass "+p.id);
            // World footprint follows the facing (rotated w/d); the tree guard's flat grate may exceed it.
            bool ns=it.facing=="N"||it.facing=="S";float wx=ns?it.width:it.depth,wz=ns?it.depth:it.width,tol=.07f;
            if(p.kind!="treeguard")Need(b.size.x<=wx+2*tol&&b.size.z<=wz+2*tol,"footprint/orientation "+p.id+" "+p.kind+" bounds="+b.size+" plan="+wx+"x"+wz);
            Need(Mathf.Abs(b.size.y-it.height)<.09f,"height "+p.id+" "+b.size.y);
            Need(Mathf.Abs(b.min.y-p.transform.position.y)<.01f,"not standing on the paving "+p.id);
            // Front faces the declared direction (the street side of the footprint).
            var forward=p.transform.rotation*Vector3.back;var want=it.facing=="N"?Vector3.forward:it.facing=="S"?Vector3.back:it.facing=="E"?Vector3.right:Vector3.left;
            Need(Vector3.Dot(forward,want)>.999f,"facing "+p.id);
        }
        // No street item collider overlaps another prop, lamp, sign, building or bench (AABB, 2 cm slack).
        Physics.SyncTransforms();
        var own=new HashSet<Collider>(root.GetComponentsInChildren<Collider>());
        var others=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Where(c=>!own.Contains(c)&&!(c is MeshCollider)&&!c.isTrigger).ToArray();
        foreach(var p in placed)foreach(var c in p.GetComponents<Collider>()) {
            var mine=c.bounds;mine.Expand(-.04f);
            foreach(var o in others) {
                if(!o.bounds.Intersects(mine))continue;
                if(p.kind=="treeguard"&&o.name=="Existing trunk collision")continue;
                if(o.bounds.size.x>300||o.bounds.size.z>300)continue; // town ground slab
                Need(false,p.id+" "+p.kind+" collider overlaps "+o.name+" at "+o.bounds.center);
            }
        }
        foreach(var guard in placed.Where(p=>p.kind=="treeguard"))
            Need(others.Any(o=>o.name=="Existing trunk collision"&&Vector2.Distance(new Vector2(o.transform.position.x,o.transform.position.z),new Vector2(guard.transform.position.x,guard.transform.position.z))<.01f),"tree guard without its tree "+guard.id);
        string directory="../output/demo/street-items";Directory.CreateDirectory(directory);
        File.WriteAllText(directory+"/authoring-checks.json",JsonUtility.ToJson(new StreetResult{passed=true,source=plan.sourceSha256,placements=placed.Length,kinds=10,triangles=triangles,kitTriangles=manifest.triangles,colliders=root.GetComponentsInChildren<Collider>().Length,lights=0,photocellGlobes=root.GetComponentsInChildren<DayNightLamp>().Length},true));
        Debug.Log("STREET_ITEMS_AUTHORING_PASS: 41 placements, 10 kinds, "+triangles+" triangles, footprints/facing/ground/overlap verified");
    }
    [Serializable] class StreetResult {public bool passed;public string source;public int placements,kinds,triangles,kitTriangles,colliders,lights,photocellGlobes;}
}
