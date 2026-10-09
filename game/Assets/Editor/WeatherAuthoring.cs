using UnityEditor;
using UnityEngine;

public static class WeatherAuthoring {
    public static void Sky(GameObject celestial,Material sky) {
        const string texturePath="Assets/Art/Sky/WeatherNoise.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.sRGBTexture=false;importer.isReadable=true;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;
        importer.filterMode=FilterMode.Trilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=256;importer.SaveAndReimport();
        sky.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));EditorUtility.SetDirty(sky);
        const string path="Assets/Art/Sky/Rain.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("WrongDate/WeatherRain"));AssetDatabase.CreateAsset(material,path);}
        celestial.AddComponent<WeatherEffects>().rainTemplate=material;
    }
    public static void Shelter(GameObject root,Vector2 footprint,float eaves,float rise=0,Vector2 centre=default) {
        var roof=root.AddComponent<WeatherShelter>();roof.footprint=footprint;roof.eaves=eaves;roof.rise=rise;roof.centre=centre;
    }
}
