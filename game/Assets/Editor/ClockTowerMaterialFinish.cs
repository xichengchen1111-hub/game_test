using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Authored surface response; no changes to the physical kit or clue geometry.
public static class ClockTowerMaterialFinish {
    const string Root="Assets/Art/ClockTowerInterior/Materials/";
    const string Textures="Assets/Art/ClockTowerInterior/SurfaceTextures/";
    static Texture2D Write(string name,Color[] pixels,int size,bool normal=false,bool linear=false) {
        string path=Textures+name+".png";
        var t=new Texture2D(size,size,TextureFormat.RGBA32,false,normal||linear);t.SetPixels(pixels);t.Apply();
        File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.sRGBTexture=!(normal||linear);importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=16;importer.maxTextureSize=size;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Finish(string name,bool wood) {
        const int size=1024;var color=new Color[size*size];var mask=new Color[color.Length];var height=new float[color.Length];var normals=new Color[color.Length];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float u=(float)x/size,v=(float)y/size;
            float cloud=Mathf.PerlinNoise(u*12+13,v*12+37),grain=Mathf.PerlinNoise(u*170+Mathf.Sin(v*18)*2,v*4+5);
            float pore=Mathf.PerlinNoise(u*330+47,v*330+3),stain=Mathf.SmoothStep(.25f,.78f,cloud);
            float value=wood?.70f+.23f*grain+.07f*pore:.72f+.23f*stain+.05f*pore;
            color[y*size+x]=wood?new Color(value,value*.96f,value*.88f,1):new Color(value,value,Mathf.Lerp(value*.96f,value,stain),1);
            mask[y*size+x]=new Color(wood?0:Mathf.Lerp(.55f,1,stain),1,0,wood?.16f+.16f*grain:.28f+.44f*stain);
            height[y*size+x]=wood?grain*.003f+pore*.0004f:pore*.00022f+cloud*.0001f;
        }
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float dx=(height[y*size+(x+1)%size]-height[y*size+(x+size-1)%size])*size/2;
            float dy=(height[((y+1)%size)*size+x]-height[((y+size-1)%size)*size+x])*size/2;
            var n=new Vector3(-dx,-dy,1).normalized;normals[y*size+x]=new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1);
        }
        var albedo=Write(name+"-Detail",color,size);var normal=Write(name+"-DetailNormal",normals,size,true);var response=Write(name+"-Response",mask,size,false,true);
        foreach(string matName in wood?new[]{"Aged oak sides"}:new[]{"Patinated brass","Oiled cast iron"}) {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Root+matName+".mat");mat.SetTexture("_BaseMap",albedo);mat.SetTexture("_BumpMap",normal);mat.SetFloat("_BumpScale",wood?.75f:.55f);mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_MetallicGlossMap",response);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.SetFloat("_Smoothness",wood?.8f:1);
            if(!wood){ColorUtility.TryParseHtmlString(matName=="Patinated brass"?"#C4A267":"#59616A",out var tint);mat.SetColor("_BaseColor",tint);}
            EditorUtility.SetDirty(mat);
        }
        Texture2D dustGrain=null;
        if(wood) {
            for(int i=0;i<color.Length;i++) {float v=.91f+.09f*Mathf.InverseLerp(.70f,1,color[i].r);color[i]=new Color(v,v,v,1);}
            dustGrain=Write("Oak-DustGrain",color,size);
        }
        if(wood)foreach(var tread in ClockTowerInteriorSurfaces.Treads) {
            // Dust still controls the authored height gradient; grain relief fades upward.
            int band=System.Array.IndexOf(ClockTowerInteriorSurfaces.Treads,tread);
            tread.SetTexture("_BaseMap",dustGrain);tread.SetTexture("_BumpMap",normal);tread.EnableKeyword("_NORMALMAP");
            tread.SetFloat("_BumpScale",Mathf.Lerp(.60f,.18f,ClockTowerInteriorSurfaces.DustAmounts[band]));EditorUtility.SetDirty(tread);
        }
    }
    public static void Apply(Transform kit) {
        Finish("Oak",true);Finish("WorkedMetal",false);
        foreach(var renderer in kit.GetComponentsInChildren<MeshRenderer>()) {
            var mat=renderer.sharedMaterial;
            if(mat.name!="Patinated brass"&&mat.name!="Oiled cast iron"&&mat.name!="Aged oak sides")continue;
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
            for(int i=0;i<vertices.Length;i++) {var p=vertices[i];var n=normals[i];uv[i]=Mathf.Abs(n.z)>.5f?new Vector2(p.x,p.y):Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y):new Vector2(p.x,p.z);}
            mesh.uv=uv;mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
        }
        var lamp=kit.Find("Warm machine room service lamp").GetComponent<Light>();lamp.shadows=LightShadows.Soft;lamp.shadowStrength=.7f;lamp.shadowBias=.04f;lamp.shadowNormalBias=.12f;lamp.shadowResolution=LightShadowResolution.Low;
        var lightSettings=new SerializedObject(lamp.GetUniversalAdditionalLightData());lightSettings.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow;lightSettings.ApplyModifiedPropertiesWithoutUndo();
        var go=new GameObject("Clock room local reflection");go.transform.SetParent(kit,false);go.transform.localPosition=new Vector3(0,16.6f,0);
        var probe=go.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.resolution=256;probe.size=new Vector3(4.4f,3.8f,4.4f);
        probe.boxProjection=true;probe.importance=2;probe.intensity=1;probe.blendDistance=.45f;probe.nearClipPlane=.1f;probe.farClipPlane=30;probe.hdr=true;
        Debug.Log("CLOCKTOWER_MATERIAL_FINISH: 1024px oak and worked-metal response, metre UVs, local reflection and machinery shadows");
    }
}
