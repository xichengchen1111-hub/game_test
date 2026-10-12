using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Meter-scaled, editable mesh generation. Top surfaces have world-distance UVs;
// splitting a footway around a building never stretches its paving texture.
public static partial class StreetSurfaces {
    const string TextureRoot="Assets/Art/Street/";
    const string MeshRoot="Assets/Generated/Street/";
    const float KerbWidth=.22f,GutterWidth=.28f,OuterEdge=7.975f;
    [Serializable] class Plan { public Road[] roads; }
    [Serializable] class Road { public string id; public float lane,walk,strip; }
    sealed class Builder {
        public readonly List<Vector3> vertices=new List<Vector3>();
        readonly List<Vector2> uv=new List<Vector2>();
        readonly List<int> indices=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 c) {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c});
            foreach(var p in new[]{a,b,c})uv.Add(new Vector2(p.x,p.y)/2);
            indices.AddRange(new[]{n,n+1,n+2});
        }
        public void SurfaceTriangle(Vector3 a,Vector3 b,Vector3 c) {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c});
            foreach(var p in new[]{a,b,c})uv.Add(new Vector2(p.x,p.z)/4);
            indices.AddRange(new[]{n,n+1,n+2});
        }
        public void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float tile=4) {
            int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
            var normal=Vector3.Cross(b-a,c-a).normalized;
            foreach(var p in new[]{a,b,c,d}) {
                if(Mathf.Abs(normal.y)>.6f)uv.Add(new Vector2(p.x,p.z)/tile);
                else if(Mathf.Abs(normal.x)>.6f)uv.Add(new Vector2(p.z,p.y)/tile);
                else uv.Add(new Vector2(p.x,p.y)/tile);
            }
            indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        public void Rect(float x0,float x1,float z0,float z1,float y,float tile=4) {
            Face(new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z1),new Vector3(x1,y,z0),tile);
        }
        public void Block(Vector3 min,Vector3 max,float tile=2) {
            Face(new Vector3(min.x,max.y,min.z),new Vector3(min.x,max.y,max.z),max,new Vector3(max.x,max.y,min.z),tile);
            Face(min,new Vector3(min.x,min.y,max.z),new Vector3(min.x,max.y,max.z),new Vector3(min.x,max.y,min.z),tile);
            Face(new Vector3(max.x,min.y,min.z),new Vector3(max.x,max.y,min.z),max,new Vector3(max.x,min.y,max.z),tile);
            Face(min,new Vector3(min.x,max.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(max.x,min.y,min.z),tile);
            Face(new Vector3(min.x,min.y,max.z),new Vector3(max.x,min.y,max.z),max,new Vector3(min.x,max.y,max.z),tile);
        }
        public GameObject Save(string name,Material material,Transform parent,bool collision=true) {
            if(vertices.Count==0)throw new Exception("Empty street mesh: "+name);
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            string path=MeshRoot+name.Replace(' ','-')+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
            else AssetDatabase.CreateAsset(mesh,path);
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.isStatic=true;
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=material;
            if(collision)g.AddComponent<MeshCollider>().sharedMesh=mesh;
            return g;
        }
    }
    static Material Stone(string name,string texture,Color tint,float smoothness=1,float bump=.4f) {
        foreach(var suffix in new[]{"Base","Normal","Mask"}) {
            string texturePath=TextureRoot+texture+"_"+suffix+".png";
            var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if(!importer)throw new Exception("Missing street texture: "+texturePath);
            importer.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=suffix=="Base";importer.wrapMode=TextureWrapMode.Repeat;
            importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
            importer.maxTextureSize=texture.StartsWith("Period")?2048:1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.SaveAndReimport();
        }
        string path="Assets/Generated/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",tint);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+texture+"_Base.png"));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+texture+"_Normal.png"));m.SetFloat("_BumpScale",bump);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+texture+"_Mask.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_SmoothnessTextureChannel",0);
        m.SetFloat("_EnvironmentReflections",1);m.SetFloat("_SpecularHighlights",1);EditorUtility.SetDirty(m);return m;
    }
    static Material Iron() {
        const string path="Assets/Generated/Materials/StreetIron.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",new Color(.17f,.145f,.125f));m.SetFloat("_Metallic",.55f);m.SetFloat("_Smoothness",.24f);EditorUtility.SetDirty(m);return m;
    }
    static void Clipped(Builder b,float x0,float x1,float z0,float z1,float y,Bounds hole) {
        if(x1<=x0||z1<=z0)return;
        if(x1<=hole.min.x||x0>=hole.max.x||z1<=hole.min.z||z0>=hole.max.z){b.Rect(x0,x1,z0,z1,y);return;}
        float a=Mathf.Max(x0,hole.min.x),c=Mathf.Min(x1,hole.max.x);
        Clipped(b,x0,a,z0,z1,y,hole);Clipped(b,c,x1,z0,z1,y,hole);
        if(hole.min.z>z0)b.Rect(a,c,z0,Mathf.Min(z1,hole.min.z),y);
        if(hole.max.z<z1)b.Rect(a,c,Mathf.Max(z0,hole.max.z),z1,y);
    }
    static void Kerb(Builder b,float x,float z0,float z1,float y) {
        float w=KerbWidth/2,bevel=.018f;
        var profile=new[]{new Vector2(x-w,-.036f),new Vector2(x-w,y-bevel),new Vector2(x-w+bevel,y),new Vector2(x+w-bevel,y),new Vector2(x+w,y-bevel),new Vector2(x+w,-.036f)};
        for(int i=0;i<profile.Length-1;i++) {
            var a=profile[i];var c=profile[i+1];
            b.Face(new Vector3(a.x,a.y,z0),new Vector3(a.x,a.y,z1),new Vector3(c.x,c.y,z1),new Vector3(c.x,c.y,z0),2);
        }
        // Close the ends so every joint has actual thickness, not a floating plane.
        for(int i=1;i<profile.Length-1;i++) {
            var a=profile[0];var c=profile[i];var d=profile[i+1];
            b.Triangle(new Vector3(a.x,a.y,z0),new Vector3(c.x,c.y,z0),new Vector3(d.x,d.y,z0));
            b.Triangle(new Vector3(a.x,a.y,z1),new Vector3(d.x,d.y,z1),new Vector3(c.x,c.y,z1));
        }
    }
    public static GameObject Build(Transform shop) {
        Directory.CreateDirectory(MeshRoot);AssetDatabase.Refresh();
        string planPath=Path.GetFullPath("../docs/maps/town-current/plan-data.json");
        var profile=JsonUtility.FromJson<Plan>(File.ReadAllText(planPath)).roads.First(r=>r.id=="R01");
        if(profile.lane!=6||profile.walk!=2||profile.strip!=1)throw new Exception("R01 road kit profile changed: review street dimensions before building");
        var root=new GameObject("Street surfaces");var layout=root.AddComponent<StreetSurfaceLayout>();
        layout.carriageway=profile.lane;layout.clearFootway=profile.walk;layout.furnitureStrip=profile.strip;
        var reflectionObject=new GameObject("Street reflection");reflectionObject.transform.SetParent(root.transform,false);
        reflectionObject.transform.localPosition=new Vector3(0,3,-3);
        var reflection=reflectionObject.AddComponent<ReflectionProbe>();reflection.mode=ReflectionProbeMode.Realtime;
        reflection.refreshMode=ReflectionProbeRefreshMode.OnAwake;reflection.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        reflection.resolution=128;reflection.size=new Vector3(28,12,56);reflection.boxProjection=true;
        reflection.nearClipPlane=.15f;reflection.farClipPlane=90;reflection.intensity=.7f;reflection.hdr=true;
        float half=profile.lane/2,stoneEdge=half-GutterWidth,kerbEdge=half+KerbWidth;
        var roadMat=Stone("StreetSetts","RoadSetts",Color.white,1,.32f);
        var flagMat=Stone("StreetFlags","FootwayFlags",Color.white,.85f,.32f);
        // The furniture band is a spatial allocation, not a painted dark stripe.
        // Identical material and world-distance UVs keep the flagstones continuous.
        var stripMat=flagMat;
        var kerbMat=Stone("StreetKerb","KerbStone",new Color(.85f,.86f,.87f),.8f,.35f);
        var gutterMat=Stone("StreetGutter","RoadSetts",new Color(.69f,.71f,.72f),1,.25f);
        var road=new Builder();
        float[] xs={-stoneEdge,-1.4f,0,1.4f,stoneEdge};
        float Crown(float x)=>.025f-.045f*Mathf.Abs(x)/stoneEdge;
        for(int i=0;i<xs.Length-1;i++)road.Face(new Vector3(xs[i],Crown(xs[i]),-20),new Vector3(xs[i],Crown(xs[i]),5),new Vector3(xs[i+1],Crown(xs[i+1]),5),new Vector3(xs[i+1],Crown(xs[i+1]),-20));
        road.Save("Crowned carriageway",roadMat,root.transform);
        var apron=new Builder();
        float[] rampXs={-half,-stoneEdge,-1.4f,0,1.4f,stoneEdge,half};
        for(int i=0;i<rampXs.Length-1;i++) {
            float a=rampXs[i],b=rampXs[i+1];
            apron.Face(new Vector3(a,Crown(Mathf.Clamp(a,-stoneEdge,stoneEdge)),5),new Vector3(a,.1f,layout.plazaStart),new Vector3(b,.1f,layout.plazaStart),new Vector3(b,Crown(Mathf.Clamp(b,-stoneEdge,stoneEdge)),5));
        }
        apron.Save("Square stone apron",roadMat,root.transform);
        var hole=shop.Find("Interior floor").GetComponent<Renderer>().bounds;
        var plaza=new Builder();Clipped(plaza,-layout.plazaHalfWidth,layout.plazaHalfWidth,layout.plazaStart,28,.1f,hole);
        plaza.Save("Clock square paving",flagMat,root.transform);
        GameObject west=null;
        for(int side=-1;side<=1;side+=2) {
            float Side(float x)=>side*x;
            void Cut(Builder b,float a,float c,float z0,float z1)=>Clipped(b,Mathf.Min(Side(a),Side(c)),Mathf.Max(Side(a),Side(c)),z0,z1,.1f,hole);
            var footway=new Builder();Cut(footway,4,OuterEdge,-20,layout.plazaStart);Cut(footway,layout.plazaHalfWidth,OuterEdge,layout.plazaStart,28);
            var sidewalk=footway.Save(side<0?"West sidewalk":"East sidewalk",flagMat,root.transform);
            if(side<0)west=sidewalk;
            var service=new Builder();Cut(service,kerbEdge,4,-20,layout.plazaStart);service.Save(side<0?"West furniture strip":"East furniture strip",stripMat,root.transform);
            var kerbs=new Builder();
            for(float z=-20;z<layout.plazaStart;z+=1)Kerb(kerbs,Side(half+KerbWidth/2),z+.003f,Mathf.Min(z+.997f,layout.plazaStart),.1f);
            kerbs.Save(side<0?"West dressed kerb":"East dressed kerb",kerbMat,root.transform);
            var channel=new Builder();var grates=new Builder();
            float left=Mathf.Min(Side(stoneEdge),Side(half)),right=Mathf.Max(Side(stoneEdge),Side(half));
            var drains=new[]{-14.75f,-5.75f,3.25f};
            for(float z=-20;z<5;z+=.5f) {
                bool drain=drains.Any(d=>Mathf.Abs(d-(z+.25f))<.1f);
                if(!drain)channel.Rect(left+.003f,right-.003f,z+.003f,z+.497f,-.019f);
            }
            channel.Save(side<0?"West drainage channel":"East drainage channel",gutterMat,root.transform);
            foreach(float z in drains) {
                grates.Block(new Vector3(left+.004f,-.035f,z-.247f),new Vector3(right-.004f,-.023f,z+.247f));
                for(int i=0;i<9;i++)grates.Block(new Vector3(left+.01f,-.022f,z-.237f+i*.055f),new Vector3(right-.01f,-.008f,z-.215f+i*.055f));
                grates.Block(new Vector3(left+.004f,-.022f,z-.247f),new Vector3(left+.025f,-.008f,z+.247f));
                grates.Block(new Vector3(right-.025f,-.022f,z-.247f),new Vector3(right-.004f,-.008f,z+.247f));
            }
            grates.Save(side<0?"West iron gully grates":"East iron gully grates",Iron(),root.transform);
        }
        Debug.Log("STREET_SURFACE_BUILD_PASS: R01 6m carriageway, 2m footway, 1m furniture strip; legacy slice frontage and square, shared window geometry");
        return west;
    }
}
