using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class OpeningAuthoring {
    const string Folder="Assets/Art/Opening/";
    public static void AddToPolice(GameObject root) {
        var previous=root.transform.Find("Opening act");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var group=new GameObject("Opening act");group.transform.SetParent(root.transform,false);
        Actor(group.transform,"狱警","guard",new Vector3(16.95f,.15f,20.6f),-90,true);
        Actor(group.transform,"律师","lawyer",new Vector3(16,.15f,1.3f),180,false);
        var paper=EvidenceModel.Paper("律师带来的报纸",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Newspaper.png"),.42f,.54f);
        paper.transform.SetParent(group.transform,false);paper.transform.localPosition=new Vector3(16.38f,.925f,2.9f);paper.transform.localRotation=Quaternion.Euler(90,0,0);
        paper.AddComponent<EvidencePhysical>();var c=paper.AddComponent<BoxCollider>();c.size=new Vector3(.42f,.54f,.016f);
        var item=paper.AddComponent<OpeningNewspaper>();item.title="青川日报 · 妻子遇害报道";item.evidenceId="opening-newspaper";
        item.source="律师带来的青川日报（案件报道；具体刊日未标注）";
        item.body="本镇一名妇人于一月二十日夜间被发现死于自家会客室。发现者为其丈夫，邻居夫妇亦在场。警方已问讯丈夫，并核查其当晚行程。\n\n住宅内有局部翻乱痕迹，前后门未发现明显强行进入痕迹。死亡时刻尚未确定，警方仍在收集材料。\n\n镇上有人谈及死者的家庭纷争，亦有人将此案与一名修车匠联系起来。这些传闻尚不能说明谁实施了袭击。\n\n本报提醒：案情仍在调查，传言与已核实的事实应加以区别。";
        foreach(var mf in paper.GetComponentsInChildren<MeshFilter>()) {
            string path=Folder+mf.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mf.sharedMesh,old);UnityEngine.Object.DestroyImmediate(mf.sharedMesh);mf.sharedMesh=old;}else AssetDatabase.CreateAsset(mf.sharedMesh,path);
        }
        foreach(var mr in paper.GetComponentsInChildren<MeshRenderer>()) {
            string path=Folder+mr.name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(old){EditorUtility.CopySerialized(mr.sharedMaterial,old);UnityEngine.Object.DestroyImmediate(mr.sharedMaterial);mr.sharedMaterial=old;}else AssetDatabase.CreateAsset(mr.sharedMaterial,path);
            mr.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
        }
        foreach(var door in root.GetComponentsInChildren<ShopMechanism>())if(door.fixtureId=="police-interview-door")door.label="律师会见室门";
        // A visitor room in the existing shared police building, rather than a modern interrogation set.
        var sign=new GameObject("律师会见室标牌");sign.transform.SetParent(group.transform,false);sign.transform.localPosition=new Vector3(12.912f,1.9f,3.5f);sign.transform.localRotation=Quaternion.Euler(0,90,0);
        var tm=sign.AddComponent<TextMesh>();tm.text="律师会见室";tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");tm.fontSize=64;tm.characterSize=.02f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.75f,.67f,.5f);
        // Painted beside the door on the hall face of the wall; depth-tested so it no longer shows through walls.
        const string signPath=Folder+"InterviewSignText.mat";var signMaterial=AssetDatabase.LoadAssetAtPath<Material>(signPath);
        if(!signMaterial){signMaterial=new Material(Shader.Find("WrongDate/RailWorldText"));AssetDatabase.CreateAsset(signMaterial,signPath);}
        tm.font.RequestCharactersInTexture(tm.text,64);signMaterial.mainTexture=tm.font.material.mainTexture;EditorUtility.SetDirty(signMaterial);
        var signRenderer=sign.GetComponent<MeshRenderer>();signRenderer.sharedMaterial=signMaterial;signRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var lettering=sign.AddComponent<WallLettering>();lettering.font=tm.font;lettering.material=signMaterial;
    }
    static void Actor(Transform parent,string name,string role,Vector3 position,float yaw,bool police) {
        var g=PeriodCharacterAsset.AllPrepared?PeriodCharacterAsset.Create(parent,position,police?"CustodyGuard":"Lawyer",name):StationClerkAsset.Create(parent,position,police);
        g.name=name+(PeriodCharacterAsset.AllPrepared?"":" · 临时模型");g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        if(g.GetComponent<Interactable>())UnityEngine.Object.DestroyImmediate(g.GetComponent<Interactable>());
        var actor=g.AddComponent<OpeningActor>();actor.title=name;actor.role=role;
        var legacy=g.GetComponentInChildren<StationClerkPerformance>();if(legacy)legacy.speaker=name;
        var performance=g.GetComponentInChildren<PeriodCharacterPerformance>();if(performance)performance.speaker=name;
        foreach(var r in g.GetComponentsInChildren<Renderer>())r.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
    }
    public static void BuildMac()=>BuildTo("../builds/opening/错日证词-第一幕.app");
    public static void BuildMacRide()=>BuildTo("../builds/opening/ride/错日证词-第一幕.app");
    public static void BuildMacFinal()=>BuildTo("../builds/opening/final/错日证词-第一幕.app");
    // Stage the complete existing game before promoting it to the desktop entry.
    // Its ordinary product name preserves the original investigation save slot;
    // --opening-playtest opens the prison introduction in its separate slot.
    public static void BuildMacIntegrated()=>BuildTo("../builds/opening/integrated/错日证词-新版.app","错日证词 · 暮色小镇");
    static void BuildTo(string output,string buildProduct="错日证词 · 第一幕试玩") {
        AssetDatabase.Refresh();
        OpeningRideCarAuthoring.BuildPrefab();
        string path="Assets/Generated/PoliceStation.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{PoliceCastAuthoring.AddToPolice(root);AddToPolice(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        // The hospital's people follow their own approvals the same way: a newly approved figure replaces its stand-in here.
        const string hospitalPath="Assets/Generated/Hospital.prefab";var hospital=PrefabUtility.LoadPrefabContents(hospitalPath);
        try{HospitalCastAuthoring.AddToHospital(hospital);PrefabUtility.SaveAsPrefabAsset(hospital,hospitalPath);}finally{PrefabUtility.UnloadPrefabContents(hospital);}
        DialogueScript.Load("OpeningGuardDialogue").Validate();DialogueScript.Load("OpeningLawyerDialogue").Validate();DialogueScript.Load("OpeningRideDialogue").Validate();
        foreach(string script in new[]{"HospitalPorterDialogue","HospitalSisterDialogue","HospitalChildDialogue"})DialogueScript.Load(script).Validate();
        AssetDatabase.SaveAssets();
        string product=PlayerSettings.productName;PlayerSettings.productName=buildProduct;
        string identifier=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.qingchuanstudio.wrongdatetest.currentmac");
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,1);
        try {
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Systems.unity","Assets/Scenes/Town.unity","Assets/Scenes/ClockShop.unity","Assets/Scenes/OtherTown.unity"},output,BuildTarget.StandaloneOSX,BuildOptions.None);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Opening player build failed");
        } finally {
            PlayerSettings.productName=product;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,identifier);
        }
        Debug.Log("OPENING_BUILD_PASS");
    }
}
