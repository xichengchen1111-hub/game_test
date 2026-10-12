using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Daylight material and light quality. The town is mostly under thick cloud (AGENTS
// weather table), where there is no hard sun: form comes from ambient occlusion in
// corners and under eaves, a top-lit sky, and restrained colour separation.
public static partial class BuildSlice {
    static void ConfigureAmbientOcclusion() {
        var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
        var feature=renderer.rendererFeatures.Find(f=>f is ScreenSpaceAmbientOcclusion);
        if(!feature)throw new InvalidOperationException("SSAO renderer feature missing");
        var settings=new SerializedObject(feature);
        // Wider and stronger contact shadowing; still full resolution and the same sample count.
        settings.FindProperty("m_Settings.Intensity").floatValue=.95f;
        settings.FindProperty("m_Settings.Radius").floatValue=.55f;
        settings.FindProperty("m_Settings.DirectLightingStrength").floatValue=.30f;
        settings.FindProperty("m_Settings.Falloff").floatValue=90f;
        settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(feature);
    }

    static void GradeTownVolume(VolumeProfile profile,bool inside) {
        if(inside)return;
        // Cool slate shadows and faintly warm highlights: planes separate while the whole
        // stays grey, cold and low in saturation. Lamps at night stay the warm accent.
        if(!profile.TryGet<SplitToning>(out var split))split=profile.Add<SplitToning>();
        split.shadows.Override(new Color(.495f,.505f,.53f));split.highlights.Override(new Color(.54f,.515f,.49f));split.balance.Override(-8f);
        if(!profile.TryGet<ShadowsMidtonesHighlights>(out var tones))tones=profile.Add<ShadowsMidtonesHighlights>();
        tones.shadows.Override(new Vector4(.99f,1f,1.015f,-.025f));tones.midtones.Override(new Vector4(1,1,1,0));tones.highlights.Override(new Vector4(1.01f,1f,.985f,.03f));
        if(profile.TryGet<ColorAdjustments>(out var colour)){colour.contrast.Override(13);colour.postExposure.Override(.28f);}
    }

    // Placeholder buildings: one shared weathered facade atlas (ground + upper storey rows,
    // normal-mapped) in four broad stucco tints, and normal-mapped slate roofs. Still opaque
    // panels: painted windows never imply an interior that does not exist.
    static readonly (string name,string hex,string plaster)[] FacadeTints={
        ("Cream","#E6DCC8","#C6BBA6"),("Ochre","#DEC6A0","#BFA683"),("Ash","#CBCAC1","#AAA9A0"),("Rose","#D8C0AF","#B9A090")};
    const string PlaceholderArt="Assets/Art/TownPlaceholders/";
    static int FacadeVariant(string id){int h=17;foreach(char c in id)h=h*31+c;return Mathf.Abs(h)%FacadeTints.Length;}
    static void ImportAs(string path,bool normal) {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Missing placeholder texture "+path);
        var type=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        if(importer.textureType!=type||importer.anisoLevel!=8||importer.sRGBTexture==normal) {
            importer.textureType=type;importer.sRGBTexture=!normal;importer.mipmapEnabled=true;importer.anisoLevel=8;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();
        }
    }
    static Material TexturedMap(string name,string albedo,string normal,Color tint,float smoothness,float bump) {
        ImportAs(PlaceholderArt+albedo,false);ImportAs(PlaceholderArt+normal,true);
        var m=Mat(name,"#FFFFFF",0,smoothness);
        m.SetColor("_BaseColor",tint);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderArt+albedo));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(PlaceholderArt+normal));m.SetFloat("_BumpScale",bump);m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_Smoothness",smoothness);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Material PlaceholderFacade(int variant)=>TexturedMap("MapFacade"+FacadeTints[variant].name,"FacadeAtlas.png","FacadeAtlas-Normal.png",C(FacadeTints[variant].hex),.12f,.9f);
    static Material PlaceholderPlaster(int variant)=>Mat("MapPlaster"+FacadeTints[variant].name,FacadeTints[variant].plaster,0,.10f);
    static Material PlaceholderSlate()=>TexturedMap("MapSlateRoof","Slate.png","Slate-Normal.png",Color.white,.24f,.85f);

    // Rain wetness for the shared town surfaces, the placeholder kit and the clock tower.
    static void TownWetnessDriver() {
        var entries=new System.Collections.Generic.List<TownWetness.Entry>();
        void Add(Material m,float smoothness,float darken){if(!m)throw new InvalidOperationException("Wetness material missing");entries.Add(new TownWetness.Entry{material=m,wetSmoothness=smoothness,wetDarken=darken});}
        Material Load(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
        // Street materials keep their mask-map smoothness; _Smoothness scales it.
        Add(Load("Assets/Generated/Materials/StreetSetts.mat"),.80f,.76f);
        Add(Load("Assets/Generated/Materials/StreetFlags.mat"),.78f,.80f);
        Add(Load("Assets/Generated/Materials/StreetKerb.mat"),.74f,.82f);
        Add(Load("Assets/Generated/Materials/StreetYard.mat"),.62f,.82f);
        Add(Load("Assets/Generated/Materials/PeriodGutter.mat"),.78f,.78f);
        Add(Load("Assets/Generated/Materials/PeriodRoadPaint.mat"),.55f,.86f);
        for(int i=0;i<FacadeTints.Length;i++){Add(PlaceholderFacade(i),.40f,.84f);Add(PlaceholderPlaster(i),.38f,.85f);}
        Add(PlaceholderSlate(),.62f,.86f);
        foreach(var (name,smoothness,darken) in new[]{("PExt_Stucco",.38f,.86f),("PExt_Ashlar",.50f,.82f),("PExt_Slate",.62f,.86f),("PExt_Flags",.72f,.80f)})
            Add(Load(PoliceMaterials+name+".mat"),smoothness,darken);
        foreach(var (name,smoothness,darken) in new[]{("IvoryBrick",.48f,.82f),("CreamStone",.50f,.82f),("Slate",.62f,.86f),("Lead",.52f,.92f),("DarkOak",.40f,.86f),("Recess",.30f,.90f),("Bronze",.46f,.92f)})
            Add(Load("Assets/Art/ClockTower1931/Tower_"+name+".mat"),smoothness,darken);
        var driver=new GameObject("Town rain wetness").AddComponent<TownWetness>();driver.entries=entries.ToArray();
        Debug.Log("TOWN_WETNESS_READY: "+entries.Count+" shared materials darken and gloss in rain, drying over ~36 game minutes");
    }
}
