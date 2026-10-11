using System;
using System.Collections.Generic;
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
        if(name=="RailShedGlass") {
            // Train-shed and covered-way glazing: lit, lightly frosted and sooty so the panes read.
            string path=RailFolder+"RailShedGlass.mat";
            var shader=Shader.Find("WrongDate/ShedGlass");
            var glass=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!glass){glass=new Material(shader);AssetDatabase.CreateAsset(glass,path);}
            glass.shader=shader;glass.SetColor("_Tint",new Color(.62f,.71f,.70f,.20f));glass.SetFloat("_Grime",.4f);glass.renderQueue=3000;EditorUtility.SetDirty(glass);return glass;
        }
        string hex=name switch {
            "RailIron"=>"#44514E","RailGreen"=>"#345B47","RailIvory"=>"#E2D6BE","RailBrass"=>"#B59755",
            "RailOfficeWalnut"=>"#805B42","RailWalnut"=>"#A37850","RailFelt"=>"#4B6651","RailGlow"=>"#FFD292","RailStone"=>"#D4C8AD",
            "RailFloor"=>"#DDD8C3","RailLeather"=>"#8C5C3D",
            "RailSlate"=>"#35414E","RailRed"=>"#773C36","RailCoal"=>"#1C2224","RailSkin"=>"#B99075",
            "RailGranite"=>"#86858A","RailBrick"=>"#9C5E4A","RailClay"=>"#A86644","RailCeiling"=>"#E6DAC0","RailZinc"=>"#6C7880",
            "RailEmber"=>"#FF8A3D","RailParcel"=>"#B79668","RailCarpet"=>"#7E3431","RailCurtain"=>"#73302D","RailRubber"=>"#2C2E31",
            "RailCopper"=>"#B8744C","RailFlagstone"=>"#B6B1A7","RailCoping"=>"#CBC5B7","RailBallast"=>"#66676B","RailSleeper"=>"#4A3B2E",
            "RailRailHead"=>"#9A9EA2",_=>"#A49170"};
        float emission=name=="RailGlow"?1.8f:name=="RailEmber"?2.4f:0;
        var material=Mat(name,hex,emission,RailSmoothness(name));
        if(emission>0) {
            // RailGlow had been saved without its _EMISSION keyword, so every railway lamp glass
            // stayed dark and DayNightLamp skipped it. Re-assert the keyword and realtime GI flag.
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",C(hex)*emission);
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;EditorUtility.SetDirty(material);
        }
        material.SetFloat("_Metallic",name=="RailBrass"?.65f:name=="RailIron"?.22f:name=="RailCopper"?.8f:0);
        string surface=name switch {"RailStone"=>"Limestone","RailFloor"=>"Floor","RailOfficeWalnut" or "RailWalnut" or "RailSleeper"=>"Walnut","RailFelt"=>"Moquette",
            "RailBrass"=>"Brass","RailIron" or "RailRailHead"=>"Iron","RailSlate"=>"Slate","RailLeather"=>"Leather","RailGreen" or "RailIvory" or "RailRed"=>"Paint",
            "RailGranite" or "RailBallast"=>"Granite","RailBrick"=>"Brick","RailCeiling"=>"Boards","RailZinc"=>"Zinc","RailCarpet"=>"Carpet","RailCurtain"=>"Velvet",
            "RailRubber"=>"Bellows","RailParcel"=>"Paper","RailFlagstone"=>"Flagstone","RailCoping"=>"Coping",_=>null};
        if(surface!=null) {
            material.SetTexture("_BaseMap",RailTexture(surface+"Base.png"));
            material.SetTexture("_BumpMap",RailTexture(surface+"Normal.png",true,true));
            material.SetTexture("_MetallicGlossMap",RailTexture(surface+"Mask.png",false,true));
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.SetFloat("_BumpScale",name switch {"RailFelt"=>.3f,"RailStone" or "RailGranite" or "RailBrick" or "RailFlagstone" or "RailCoping"=>.45f,"RailCurtain" or "RailRubber"=>.6f,_=>.25f});
            // Texture metres per tile, given the shared 1.4 UV-per-metre box projection.
            float tiling=name switch {"RailStone"=>.4f,"RailFloor"=>.36f,"RailFelt"=>3f,"RailGranite"=>.4f,"RailBallast"=>2.2f,"RailBrick"=>.42f,"RailCeiling"=>.81f,
                "RailZinc"=>.36f,"RailCarpet"=>.47f,"RailCurtain"=>1.43f,"RailRubber"=>.74f,"RailFlagstone"=>.30f,"RailCoping"=>.397f,_=>1f};
            material.SetTextureScale("_BaseMap",Vector2.one*tiling);EditorUtility.SetDirty(material);
        }
        return material;
    }
    // URP Lit multiplies this by the authored mask alpha; satin paint, varnished walnut, polished brass.
    static float RailSmoothness(string name)=>name switch {
        "RailBrass"=>.80f,"RailCopper"=>.80f,"RailIron"=>.55f,"RailRailHead"=>.85f,"RailGreen"=>.78f,"RailIvory"=>.70f,"RailRed"=>.70f,
        "RailWalnut" or "RailOfficeWalnut"=>.62f,"RailFloor"=>.62f,"RailZinc"=>.60f,"RailLeather"=>.50f,"RailSlate"=>.45f,"RailGranite"=>.45f,
        "RailCurtain"=>.45f,"RailCoping"=>.45f,"RailStone"=>.40f,"RailFlagstone"=>.40f,"RailCeiling"=>.35f,"RailClay"=>.35f,"RailBrick"=>.30f,
        "RailFelt"=>.25f,"RailRubber"=>.25f,"RailSleeper"=>.25f,"RailParcel"=>.20f,"RailCarpet"=>.15f,"RailBallast"=>.10f,_=>.22f};
    /// <summary>Accumulates axis-aligned/rotated boxes into one static mesh with metric box-projected UVs.</summary>
    sealed class RailMeshKit {
        readonly List<Vector3> vertices=new List<Vector3>();readonly List<Vector3> normals=new List<Vector3>();
        readonly List<Vector2> uvs=new List<Vector2>();readonly List<int> triangles=new List<int>();
        static readonly Vector3[] Faces={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        public void Box(Vector3 centre,Vector3 size,Quaternion? rotation=null) {
            var r=rotation??Quaternion.identity;var h=size*.5f;
            foreach(var normal in Faces) {
                var u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;var w=Vector3.Cross(normal,u);
                var face=Vector3.Scale(normal,h);var du=Vector3.Scale(u,h);var dw=Vector3.Scale(w,h);
                int start=vertices.Count;var n=r*normal;
                foreach(var p in new[]{face-du-dw,face+du-dw,face+du+dw,face-du+dw}) {
                    var world=centre+r*p;vertices.Add(world);normals.Add(n);
                    var a=new Vector3(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z));
                    uvs.Add((a.x>=a.y&&a.x>=a.z?new Vector2(world.z,world.y):a.y>=a.z?new Vector2(world.x,world.z):new Vector2(world.x,world.y))*1.4f);
                }
                bool front=Vector3.Dot(Vector3.Cross(vertices[start+1]-vertices[start],vertices[start+2]-vertices[start]),n)>0;
                triangles.AddRange(front?new[]{start,start+1,start+2,start,start+2,start+3}:new[]{start,start+2,start+1,start,start+3,start+2});
            }
        }
        public GameObject Save(Transform parent,string name,Material material,bool collide) {
            var mesh=new Mesh{name=name};
            if(vertices.Count>65000)mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);
            mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path="Assets/Generated/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;EditorUtility.SetDirty(mesh);}else AssetDatabase.CreateAsset(mesh,path);
            var go=Group(name,Vector3.zero,parent);go.isStatic=true;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collide)go.AddComponent<MeshCollider>().sharedMesh=mesh;
            return go;
        }
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
            bool glass=renderer.sharedMaterials.All(m=>m.shader.name=="WrongDate/ClearGlass"||m.shader.name=="WrongDate/ShedGlass");
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
        // Metric UVs so the flagstone courses keep their size on the slope.
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x,v.z)*1.4f).ToArray();
        mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path="Assets/Generated/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=RailMaterial("RailFlagstone");root.AddComponent<MeshCollider>().sharedMesh=mesh;
        // Brick cheek walls close the open triangles under the slope; their tops follow it as low parapets.
        var cheeks=new RailMeshKit();float run=z1-z0,rise=y1-y0,angle=Mathf.Atan2(rise,run)*Mathf.Rad2Deg;
        foreach(int side in new[]{-1,1})
            cheeks.Box(new Vector3(x+side*(width/2+.11f),(y0+y1)/2+.05f,(z0+z1)/2),new Vector3(.22f,.70f,Mathf.Sqrt(run*run+rise*rise)),Quaternion.Euler(-angle,0,0));
        cheeks.Save(parent,name+"-cheeks",RailMaterial("RailBrick"),true);
    }
    static void RailClockFace(Transform parent,string name,Vector3 centre,Quaternion rotation,float radius) {
        // Reads the same shared clock as the facade clock and the watch (no tower offset).
        var clock=Group(name,centre,parent);clock.transform.localRotation=rotation;var dial=clock.AddComponent<ClockDial>();
        var face=Cylinder("Clock dial",new Vector3(0,0,.004f),new Vector3(radius*2,.004f,radius*2),RailMaterial("RailIvory"),clock.transform);face.transform.localRotation=Quaternion.Euler(90,0,0);
        dial.minuteHand=Group("Minute hand",new Vector3(0,0,-.006f),clock.transform).transform;
        dial.hourHand=Group("Hour hand",new Vector3(0,0,-.012f),clock.transform).transform;
        Box("Minute needle",new Vector3(0,radius*.40f,0),new Vector3(.018f,radius*.80f,.008f),RailMaterial("RailIron"),dial.minuteHand,false);
        Box("Hour needle",new Vector3(0,radius*.26f,0),new Vector3(.028f,radius*.52f,.008f),RailMaterial("RailIron"),dial.hourHand,false);
        for(int i=0;i<12;i++) {
            float a=i*Mathf.PI/6;
            var tick=Box("Clock index",new Vector3(Mathf.Sin(a)*radius*.82f,Mathf.Cos(a)*radius*.82f,-.002f),new Vector3(.014f,i%3==0?radius*.20f:radius*.11f,.004f),RailMaterial("RailIron"),clock.transform,false);
            tick.transform.localRotation=Quaternion.Euler(0,0,-i*30);
        }
    }
    static void RailwayGround(Transform parent,float centreX,float trainZ,float span=88,string stationName="青 川") {
        string key=centreX.ToString("0.#")+"-"+trainZ.ToString("0.#");
        var iron=RailMaterial("RailIron");var green=RailMaterial("RailGreen");
        // Track laid to the authored 1.9m wheel gauge: heads at y .095 directly under the treads.
        var bed=new RailMeshKit();bed.Box(new Vector3(centreX,0,trainZ),new Vector3(span,.08f,4.2f));
        foreach(int s in new[]{-1,1})bed.Box(new Vector3(centreX,-.02f,trainZ+s*2.3f),new Vector3(span,.04f,.4f));
        bed.Save(parent,"RailBallast-"+key,RailMaterial("RailBallast"),true);
        var sleepers=new RailMeshKit();var steel=new RailMeshKit();var heads=new RailMeshKit();
        for(float x=centreX-span/2+.6f;x<centreX+span/2-.3f;x+=.7f) {
            sleepers.Box(new Vector3(x,.01f,trainZ),new Vector3(.24f,.09f,2.6f));
            foreach(int s in new[]{-1,1})steel.Box(new Vector3(x,.062f,trainZ+s*.95f),new Vector3(.16f,.02f,.24f));
        }
        foreach(int s in new[]{-1,1}) {
            float z=trainZ+s*.95f;
            steel.Box(new Vector3(centreX,.062f,z),new Vector3(span,.014f,.12f));
            steel.Box(new Vector3(centreX,.075f,z),new Vector3(span,.02f,.022f));
            heads.Box(new Vector3(centreX,.09f,z),new Vector3(span,.01f,.065f));
        }
        sleepers.Save(parent,"RailSleepers-"+key,RailMaterial("RailSleeper"),false);
        steel.Save(parent,"RailSteel-"+key,iron,false);heads.Save(parent,"RailHeads-"+key,RailMaterial("RailRailHead"),false);
        // Brick platform wall, flagstone paving and an overhanging coping with a whitened edge.
        float back=trainZ-9.45f,edge=trainZ-1.55f;
        var wall=new RailMeshKit();wall.Box(new Vector3(centreX,.185f,(back+edge-.1f)/2),new Vector3(68,.37f,edge-.1f-back));
        wall.Save(parent,"PlatformWall-"+key,RailMaterial("RailBrick"),true);
        var flags=new RailMeshKit();flags.Box(new Vector3(centreX,.395f,(back+edge-.6f)/2),new Vector3(68,.05f,edge-.6f-back));
        flags.Save(parent,"PlatformFlags-"+key,RailMaterial("RailFlagstone"),true);
        var coping=new RailMeshKit();coping.Box(new Vector3(centreX,.39f,edge-.3f),new Vector3(68,.06f,.6f));
        coping.Save(parent,"PlatformCoping-"+key,RailMaterial("RailCoping"),true);
        var whitened=new RailMeshKit();whitened.Box(new Vector3(centreX,.4215f,edge-.08f),new Vector3(68,.003f,.10f));
        whitened.Save(parent,"PlatformEdgeLine-"+key,RailMaterial("RailIvory"),false);
        RailRamp("StationPlatformRamp-"+centreX+"-"+trainZ,parent,centreX,trainZ-14,back,.1f,.42f,3.4f);
        // Back railing: every passenger reaches the platform through the ticket barrier at the ramp head.
        var railing=new RailMeshKit();float rz=back+.06f;
        foreach(var (x0,x1) in new[]{(centreX-33.9f,centreX-1.95f),(centreX+1.95f,centreX+33.9f)}) {
            float len=x1-x0;int posts=Mathf.CeilToInt(len/2.4f);
            for(int i=0;i<=posts;i++) {
                float x=x0+len*i/posts;
                railing.Box(new Vector3(x,.97f,rz),new Vector3(.06f,1.10f,.06f));railing.Box(new Vector3(x,1.535f,rz),new Vector3(.09f,.03f,.09f));
            }
            railing.Box(new Vector3((x0+x1)/2,1.49f,rz),new Vector3(len,.05f,.05f));
            railing.Box(new Vector3((x0+x1)/2,.56f,rz),new Vector3(len,.035f,.035f));
            for(float x=x0+.13f;x<x1-.05f;x+=.13f)railing.Box(new Vector3(x,1.02f,rz),new Vector3(.016f,.92f,.016f));
        }
        // 检票口: gate posts, open leaves turned back onto the platform, a lintel and an iron arch.
        foreach(int s in new[]{-1,1}) {
            float x=centreX+s*1.85f;
            railing.Box(new Vector3(x,.47f,rz),new Vector3(.22f,.10f,.22f));
            railing.Box(new Vector3(x,1.71f,rz),new Vector3(.14f,2.48f,.14f));
            railing.Box(new Vector3(x,2.98f,rz),new Vector3(.20f,.06f,.20f));
            float lx=x-s*.10f;
            foreach(float y in new[]{.60f,1.40f})railing.Box(new Vector3(lx,y,rz+.92f),new Vector3(.04f,.04f,1.62f));
            for(float z=rz+.16f;z<rz+1.70f;z+=.13f)railing.Box(new Vector3(lx,1.0f,z),new Vector3(.016f,.80f,.016f));
            railing.Box(new Vector3(lx,1.0f,rz+1.72f),new Vector3(.05f,.86f,.05f));
        }
        railing.Box(new Vector3(centreX,2.95f,rz),new Vector3(3.84f,.06f,.08f));
        for(int i=0;i<10;i++) {
            float a0=Mathf.PI*i/10,a1=Mathf.PI*(i+1)/10;
            var p0=new Vector3(centreX+Mathf.Cos(a0)*1.85f,2.98f+Mathf.Sin(a0)*.62f,rz);var p1=new Vector3(centreX+Mathf.Cos(a1)*1.85f,2.98f+Mathf.Sin(a1)*.62f,rz);
            railing.Box((p0+p1)/2,new Vector3(Vector3.Distance(p0,p1)+.02f,.05f,.06f),Quaternion.FromToRotation(Vector3.right,p1-p0));
        }
        railing.Save(parent,"PlatformRailing-"+key,green,true);
        // Ticket inspector's lectern beside the barrier (no punch here; the clerk carries the counter punch).
        var lectern=new RailMeshKit();float deskX=centreX+2.55f,deskZ=rz+.90f;
        lectern.Box(new Vector3(deskX,.47f,deskZ),new Vector3(.46f,.10f,.40f));lectern.Box(new Vector3(deskX,.93f,deskZ),new Vector3(.30f,.82f,.26f));
        lectern.Box(new Vector3(deskX,1.36f,deskZ),new Vector3(.40f,.06f,.34f));
        lectern.Box(new Vector3(deskX,1.42f,deskZ-.01f),new Vector3(.54f,.04f,.44f),Quaternion.Euler(-14,0,0));
        lectern.Box(new Vector3(deskX,1.475f,deskZ+.215f),new Vector3(.54f,.05f,.04f),Quaternion.Euler(-14,0,0));
        lectern.Save(parent,"BarrierLectern-"+key,RailMaterial("RailWalnut"),true);
        var lecternBrass=new RailMeshKit();lecternBrass.Box(new Vector3(deskX,.95f,deskZ-.135f),new Vector3(.20f,.26f,.012f));
        lecternBrass.Box(new Vector3(deskX,1.40f,deskZ-.21f),new Vector3(.52f,.012f,.012f));
        lecternBrass.Save(parent,"BarrierLecternBrass-"+key,RailMaterial("RailBrass"),false);
        // Double-faced barrier clock in the arch, reading the shared clock.
        var drum=Cylinder("Barrier clock case",new Vector3(centreX,3.24f,rz),new Vector3(.62f,.08f,.62f),iron,parent);drum.transform.localRotation=Quaternion.Euler(90,0,0);
        RailClockFace(parent,"Barrier clock south",new Vector3(centreX,3.24f,rz-.085f),Quaternion.identity,.27f);
        RailClockFace(parent,"Barrier clock north",new Vector3(centreX,3.24f,rz+.085f),Quaternion.Euler(0,180,0),.27f);
        RailBoard(parent,"检 票 口",new Vector3(centreX,2.71f,rz-.075f),new Vector2(2.1f,.40f),Quaternion.identity);
        RailBoard(parent,"检 票 口",new Vector3(centreX,2.71f,rz+.075f),new Vector2(2.1f,.40f),Quaternion.Euler(0,180,0));
        // Station nameboards read from the train windows.
        foreach(float x in new[]{centreX-27,centreX+27}) {
            foreach(int s in new[]{-1,1})Box("Nameboard post",new Vector3(x+s*1.55f,1.48f,back+1.0f),new Vector3(.07f,2.12f,.07f),iron,parent);
            RailBoard(parent,stationName,new Vector3(x,2.25f,back+1.07f),new Vector2(3.0f,.56f),Quaternion.Euler(0,180,0));
        }
        // Fire buckets on the railing and parcels awaiting the van.
        Box("Fire bucket board",new Vector3(centreX-8,1.12f,rz+.06f),new Vector3(.95f,.42f,.03f),RailMaterial("RailRed"),parent,false);
        for(int i=0;i<3;i++) {
            float x=centreX-8.3f+i*.3f;
            Cylinder("Fire bucket",new Vector3(x,.88f,rz+.17f),new Vector3(.20f,.11f,.20f),RailMaterial("RailRed"),parent);
            Box("Fire bucket hook",new Vector3(x,1.02f,rz+.10f),new Vector3(.02f,.08f,.08f),iron,parent,false);
        }
        Box("Parcel crate",new Vector3(centreX+9,.70f,back+.75f),new Vector3(.62f,.56f,.62f),RailMaterial("RailWalnut"),parent);
        Box("Parcel crate",new Vector3(centreX+9.7f,.62f,back+.70f),new Vector3(.52f,.40f,.52f),RailMaterial("RailWalnut"),parent);
        Box("Brown paper parcel",new Vector3(centreX+9.05f,1.08f,back+.75f),new Vector3(.40f,.20f,.30f),RailMaterial("RailParcel"),parent);
        // Water crane near the departure end and a home semaphore beyond the platform.
        var crane=new RailMeshKit();float cx=centreX+30,cz=edge-1.2f;
        crane.Box(new Vector3(cx,.57f,cz),new Vector3(.60f,.30f,.60f));crane.Box(new Vector3(cx,2.05f,cz),new Vector3(.24f,2.70f,.24f));
        crane.Box(new Vector3(cx,3.45f,cz),new Vector3(.34f,.12f,.34f));crane.Box(new Vector3(cx-1.05f,3.50f,cz),new Vector3(2.30f,.12f,.12f));
        crane.Box(new Vector3(cx+.55f,3.50f,cz),new Vector3(.40f,.22f,.20f));crane.Box(new Vector3(cx-2.15f,3.0f,cz),new Vector3(.12f,.95f,.12f));
        crane.Save(parent,"WaterCrane-"+key,iron,true);
        var signal=new RailMeshKit();float sx=centreX+38,sz=trainZ+2.7f;
        signal.Box(new Vector3(sx,.15f,sz),new Vector3(.50f,.30f,.50f));signal.Box(new Vector3(sx,3.15f,sz),new Vector3(.16f,6.0f,.16f));
        signal.Box(new Vector3(sx,6.20f,sz),new Vector3(.26f,.10f,.26f));
        for(int i=0;i<16;i++)signal.Box(new Vector3(sx+.17f,.45f+i*.33f,sz),new Vector3(.03f,.03f,.30f));
        foreach(int s in new[]{-1,1})signal.Box(new Vector3(sx+.17f,2.95f,sz+s*.15f),new Vector3(.03f,5.1f,.03f));
        signal.Save(parent,"HomeSignalPost-"+key,iron,true);
        // Upper-quadrant arm raised 40 degrees about its pivot on the post (static, no timetable implied).
        var arm=Box("Semaphore arm",new Vector3(sx-.08f-.55f*Mathf.Cos(40*Mathf.Deg2Rad),5.55f+.55f*Mathf.Sin(40*Mathf.Deg2Rad),sz-.10f),new Vector3(1.10f,.18f,.04f),RailMaterial("RailRed"),parent,false);arm.transform.localRotation=Quaternion.Euler(0,0,-40);
        var stripe=Box("Semaphore stripe",new Vector3(.25f,0,-.6f),new Vector3(.10f,1.02f,.4f),RailMaterial("RailIvory"),arm.transform,false);
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
        foreach(var door in train.doors) {
            var handle=door.gameObject.AddComponent<RailDoor>();handle.train=train;handle.title="车门";
            // One simple physical leaf moves with the authored hinge, instead of many bevel colliders.
            var renderers=door.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            foreach(var collider in door.GetComponentsInChildren<MeshCollider>())UnityEngine.Object.DestroyImmediate(collider);
            var leaf=Group("Door leaf collision",Vector3.zero,door);
            leaf.transform.position=bounds.center;leaf.transform.rotation=root.transform.rotation;
            var scale=door.lossyScale;leaf.transform.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            handle.leafCollider=leaf.AddComponent<BoxCollider>();handle.leafCollider.size=new Vector3(1.05f,2.30f,.085f);
        }
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
            // Numbers sit on the ivory window band, so they are painted in the livery green.
            RailSurfaceText(root.transform,offset==0?"01":"02",new Vector3(offset+6.9f,2.75f,-1.515f),Quaternion.identity,C("#2F4A3A"),new Vector2(.26f,.24f),.025f);
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
    [Serializable] class OfficeLampData {public string bulb;public float[] position;}
    [Serializable] class OfficeLightingData {public OfficeLampData[] lamps;}
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
        var office=JsonUtility.FromJson<OfficeLightingData>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TicketOfficeGeometry.json").text);
        foreach(var data in office.lamps) {
            var p=data.position;var lamp=Point("Ticket office shaded work lamp",new Vector3(p[0],p[1],p[2]),C("#FFD59E"),.35f,2.1f);
            lamp.transform.SetParent(root.transform,false);
            var glow=visual.GetComponentsInChildren<Renderer>().Single(r=>r.name==data.bulb);
            DayNightLighting.Bind(lamp,DayNightLamp.Role.Interior,.5f,new[]{glow});
        }
        foreach(float x in new[]{-25f,-15f,-5f,5f,15f,25f}) {
            var lamp=Point("Platform lantern",new Vector3(x,5.7f,20),C("#FFDEA8"),4.5f,12);lamp.transform.SetParent(root.transform,false);DayNightLighting.Bind(lamp,DayNightLamp.Role.Exterior,.85f,x==-25?DayNightLighting.ModuleGlow(visual.transform,"Module_Platform_Lamps"):new Renderer[0]);
        }
        foreach(float x in new[]{-10f,-6f,6f,10f}) {
            // Just in front of each bracket lantern's glass (rail_station_beautify wall_lantern).
            var lamp=Point("Facade wall lantern",new Vector3(x,3.0f,-6.66f),C("#FFDAA5"),3.5f,8);lamp.transform.SetParent(root.transform,false);
            DayNightLighting.Bind(lamp,DayNightLamp.Role.Exterior,.85f,x==-10?DayNightLighting.ModuleGlow(visual.transform,"Module_Facade_Lamps"):new Renderer[0]);
        }
        WeatherAuthoring.Shelter(root,new Vector2(3.94f,1.12f),3.76f,0,new Vector2(0,-6.67f));
        // Glazed covered way from the rear hall door to the train shed.
        WeatherAuthoring.Shelter(root,new Vector2(4.08f,2.85f),3.78f,0,new Vector2(0,7.52f));
        foreach(var (x,module) in new[]{(-19f,"Module_Annex_Lamps_W"),(19f,"Module_Annex_Lamps_E")}) {
            var lamp=Point("Annex room lantern",new Vector3(x,3.0f,0),C("#FFDDAA"),2.2f,7);lamp.transform.SetParent(root.transform,false);
            DayNightLighting.Bind(lamp,DayNightLamp.Role.Interior,.5f,DayNightLighting.ModuleGlow(visual.transform,module));
        }
        // Map roles of the two wings (plan-data B17 parts) on their south headers.
        RailWallBoard(root.transform,"行 李 房",new Vector3(-19,3.6f,-4.12f),new Vector2(1.3f,.36f),Quaternion.identity);
        RailWallBoard(root.transform,"值 班 室",new Vector3(19,3.6f,-4.12f),new Vector2(1.3f,.36f),Quaternion.identity);
        // Flagstone concourse between the rear facade and the platform ramp, under the shed.
        var concourse=new RailMeshKit();concourse.Box(new Vector3(0,.05f,11.775f),new Vector3(68,.10f,11.55f));
        concourse.Save(root.transform,"StationConcourseFlags",RailMaterial("RailFlagstone"),true);
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
        RailSurfaceText(root.transform,"售票 · 检票",new Vector3(-8,2.7f,.92f),Quaternion.identity);
        RailSurfaceText(root.transform,"一 号 站 台",new Vector3(0,4.95f,17.90f),Quaternion.identity);
        RailWallBoard(root.transform,"候车厅  →  站台",new Vector3(0,3.8f,5.88f),new Vector2(3.1f,.42f),Quaternion.identity);
        RailSurfaceText(root.transform,"旅客须知\n检票时请交验车票\n大件行李请妥善放置",new Vector3(10,2.70f,5.80f),Quaternion.identity,C("#453C30"));
        RailSurfaceText(root.transform,"售票与候车\n请从后门前往站台\n请勿在轨道上停留",new Vector3(-14.81f,2.60f,-3.5f),Quaternion.Euler(0,-90,0),C("#453C30"));
        RailSurfaceText(root.transform,"行李须知\n大件行李请寄存\n请勿阻塞过道",new Vector3(14.81f,2.60f,-3.5f),Quaternion.Euler(0,90,0),C("#453C30"));
        StationItems(root.transform);
        RailValidateSigns(root.transform);
        TicketOfficeChecks.Validate(root,AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TicketOfficeGeometry.json"));
        RailChecks.ValidateStation(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Generated/StationB.prefab");UnityEngine.Object.DestroyImmediate(root);
    }
    static GameObject StationTown(TownPlanBuilding b) {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/StationB.prefab"));root.transform.position=new Vector3(188,0,46);MapMarker(root,b,true);
        RailwayGround(root.transform,0,27,88,"青 川");
        var train=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/RailTrain.prefab"));train.transform.position=new Vector3(181,0,73);train.GetComponent<RailTrain>().destination="OtherTown";
        // Parent beneath the station so full-map geometry retains a single authored hierarchy.
        train.transform.SetParent(root.transform,true);
        foreach(float x in new[]{184f,192f}) {
            var lantern=Model("Lantern",new Vector3(x,0,38.5f));lantern.transform.SetParent(root.transform,true);
            var pool=Point("Station entrance lantern",new Vector3(x,3.1f,38.5f),C("#FFD49A"),3.5f,10);pool.transform.SetParent(lantern.transform,true);DayNightLighting.Bind(pool,DayNightLamp.Role.Exterior,.85f,DayNightLighting.Glowing(lantern.transform));
        }
        RailWasteTickets(root.transform);
        RailBoard(root.transform,"废票",new Vector3(-11.3f,.40f,-1.057f),new Vector2(.24f,.14f),Quaternion.identity,inset:.03f);
        var punch=Group("手持检票钳",new Vector3(-6.665f,1.189f,.60f),root.transform);var punchTarget=punch.AddComponent<BoxCollider>();punchTarget.size=new Vector3(.31f,.078f,.11f);punchTarget.isTrigger=true;
        var tool=punch.AddComponent<Interactable>();tool.title="手持检票钳";tool.source="青川车站售票柜台";tool.body="金属检票钳。按下把手，钳口便在票上打出一个小孔。";
        var legacyClerk=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Module_TicketClerk");
        legacyClerk.gameObject.SetActive(false);
        StationClerkAsset.Create(root.transform,new Vector3(-8,.12f,1.9f));
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
        RailwayGround(root.transform,7,0,104,"邻 镇");
        // The open neighbour-town platform gets two lamp standards of the existing street kit.
        foreach(float x in new[]{-10f,24f}) {
            var standard=Model("Lantern",new Vector3(x,.42f,-6.2f));standard.transform.SetParent(root.transform,true);
            var pool=Point("Arrival platform lamp",new Vector3(x,3.5f,-6.2f),C("#FFD49A"),2.5f,10);pool.transform.SetParent(standard.transform,true);
            DayNightLighting.Bind(pool,DayNightLamp.Role.Exterior,.85f,DayNightLighting.Glowing(standard.transform));
        }
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
