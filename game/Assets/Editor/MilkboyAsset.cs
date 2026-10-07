using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Asset preparation for the authored CC-BY child. Keep the clockmaker independent.
public static class MilkboyAsset {
    const string Folder="Assets/Art/Milkboy/";
    public static GameObject Create(Vector3 position,float yaw) {
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"Milkboy.fbx");
        if(importer==null)throw new Exception("Generate Milkboy.fbx with tools/build_milkboy.py first.");
        if(importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable) {
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=true;importer.animationType=ModelImporterAnimationType.Generic;
            importer.SaveAndReimport();
        }
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"Milkboy.fbx");
        var root=new GameObject("Milkboy");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        visual.transform.SetParent(root.transform,false);
        // Read the authored visor instead of guessing FBX forward-axis conventions.
        var visor=visual.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Milkboy_CurvedVisor");
        var head=visual.GetComponentsInChildren<Renderer>().Single(r=>r.name=="Milkboy_Head");
        Vector3 front=visor.bounds.center-head.bounds.center;front.y=0;
        if(front.sqrMagnitude<.001f)throw new Exception("Milkboy facing cannot be determined.");
        visual.transform.rotation=Quaternion.FromToRotation(front.normalized,Vector3.back);
        foreach(var r in visual.GetComponentsInChildren<Renderer>()) {
            r.sharedMaterials=r.sharedMaterials.Select(m=>MaterialFor(m.name)).ToArray();
            if(r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
        }
        var all=visual.GetComponentsInChildren<Renderer>();var bounds=MeshBounds(all[0]);
        foreach(var r in all)bounds.Encapsulate(MeshBounds(r));
        if(bounds.size.y<1.40f||bounds.size.y>1.70f)throw new Exception("Milkboy import scale: "+bounds.size);
        visual.transform.position-=Vector3.up*bounds.min.y;
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Debug.Log("MILKBOY_IMPORT_PASS: child height="+bounds.size.y+" renderers="+all.Length+" forward=local-Z source=silentsheri/ToonBoy");
        return root;
    }
    static Bounds MeshBounds(Renderer renderer) {
        Mesh mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
        var points=mesh.vertices;var bounds=new Bounds(renderer.transform.TransformPoint(points[0]),Vector3.zero);
        foreach(var p in points)bounds.Encapsulate(renderer.transform.TransformPoint(p));
        return bounds;
    }
    static Material MaterialFor(string source) {
        string key=source.Split('.')[0];
        string path=Folder+key+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        Color linear=key switch {
            "Milkboy_Cotton"=>new Color(.80f,.77f,.68f),"Milkboy_Collar"=>new Color(.91f,.89f,.80f),
            "Milkboy_Blond"=>new Color(.48f,.26f,.066f),"Milkboy_Brows"=>new Color(.25f,.13f,.042f),
            "Milkboy_Leather"=>new Color(.125f,.052f,.022f),"Milkboy_LeatherEdge"=>new Color(.24f,.13f,.057f),
            "Milkboy_Navy"=>new Color(.018f,.023f,.031f),"Milkboy_Shoes"=>new Color(.022f,.019f,.016f),
            "Milkboy_Stitch"=>new Color(.48f,.40f,.27f),"Milkboy_Brass"=>new Color(.47f,.29f,.085f),
            "Milkboy_Sclera"=>new Color(.82f,.85f,.83f),"Milkboy_Iris"=>new Color(.035f,.26f,.46f),
            "Milkboy_IrisRim"=>new Color(.01f,.049f,.085f),"Milkboy_Pupil"=>new Color(.003f,.006f,.01f),
            "Teeth_shader"=>new Color(.75f,.70f,.61f),"Gum_shader"=>new Color(.28f,.075f,.07f),
            "Tongue_shader"=>new Color(.36f,.12f,.12f),_=>Color.white};
        m.SetColor("_BaseColor",linear.gamma);m.SetFloat("_Smoothness",.22f);m.SetFloat("_Metallic",0);
        m.SetTexture("_BaseMap",null);m.SetTexture("_BumpMap",null);m.DisableKeyword("_NORMALMAP");
        if(key=="Milkboy_Face"||key=="Milkboy_Hands") {
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+(key=="Milkboy_Face"?"Face.jpeg":"Hands.jpeg")));
            m.SetColor("_BaseColor",Color.white);m.SetFloat("_Smoothness",.25f);
        }
        if(key=="Milkboy_Cotton"||key=="Milkboy_Collar") {
            FabricTextures();
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"CottonWeave.png"));
            m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"CottonNormal.png"));
            m.EnableKeyword("_NORMALMAP");m.SetFloat("_BumpScale",.36f);
            m.SetTextureScale("_BaseMap",Vector2.one*14);m.SetTextureScale("_BumpMap",Vector2.one*14);
            m.SetFloat("_Smoothness",.08f);
        }
        if(key=="Milkboy_Brass"){m.SetFloat("_Metallic",.75f);m.SetFloat("_Smoothness",.55f);}
        if(key=="Milkboy_Sclera"||key=="Milkboy_Iris"||key=="Milkboy_Pupil")m.SetFloat("_Smoothness",.48f);
        EditorUtility.SetDirty(m);return m;
    }
    static void FabricTextures() {
        if(File.Exists(Folder+"CottonNormal.png"))return;
        const int size=256;var color=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var normal=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
        float H(int x,int y)=>.5f+.20f*Mathf.Sin(x*Mathf.PI/4)+.20f*Mathf.Sin(y*Mathf.PI/4)+.035f*Mathf.Sin((x+y)*2.3f);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float h=H(x,y),t=.93f+.06f*h;color.SetPixel(x,y,new Color(t,t,t));
            var n=new Vector3((H(x-1,y)-H(x+1,y))*.8f,(H(x,y-1)-H(x,y+1))*.8f,1).normalized;
            normal.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f));
        }
        color.Apply();normal.Apply();File.WriteAllBytes(Folder+"CottonWeave.png",color.EncodeToPNG());File.WriteAllBytes(Folder+"CottonNormal.png",normal.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(color);UnityEngine.Object.DestroyImmediate(normal);
        AssetDatabase.ImportAsset(Folder+"CottonWeave.png");AssetDatabase.ImportAsset(Folder+"CottonNormal.png");
        foreach(string file in new[]{"CottonWeave.png","CottonNormal.png"}) {
            var i=(TextureImporter)AssetImporter.GetAtPath(Folder+file);
            if(file.Contains("Normal"))i.textureType=TextureImporterType.NormalMap;
            i.wrapMode=TextureWrapMode.Repeat;i.anisoLevel=4;i.SaveAndReimport();
        }
    }
}
