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
        // The guard waits out of sight along the cloister, north of the cell, until he knocks
        // (nobody stands at the bars on the title screen or while the player wakes).
        var guard=Actor(group.transform,"狱警","guard",new Vector3(16.95f,.15f,22.9f),0,true);
        var lawyer=Actor(group.transform,"律师","lawyer",new Vector3(16,.15f,1.3f),180,false);
        // The guard returns to his own round once the player has been delivered to the lawyer.
        PoliceCastAuthoring.AddRoutine(guard,"CustodyGuard");guard.GetComponent<PoliceStaffRoutine>().enabled=false;
        // The lawyer sits down at the meeting table and later gets into the car (NpcPose).
        var lawyerPose=lawyer.AddComponent<NpcPose>();lawyerPose.watchDistance=5;
        // Hands that knock, unlock, push doors and carry (user, 2026-10-15); a short gesture at the
        // start of each line, then the hands rest (the curt guard gestures least).
        guard.AddComponent<NpcHands>();lawyer.AddComponent<NpcHands>();
        foreach(var (actor,seconds) in new[]{(guard,1.1f),(lawyer,1.5f)}){var p=actor.GetComponentInChildren<PeriodCharacterPerformance>();if(p)p.gestureSeconds=seconds;}
        Briefcase(group.transform);PropertySign(group.transform);PropertyCage(group.transform,root);
        // The cell door closes with its own heavy clang instead of replaying the opening squeal.
        var ironClose=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Police/Sounds/PoliceIronDoorClose.wav");
        foreach(var door in root.GetComponentsInChildren<ShopMechanism>(true))
            if(ironClose&&door.sound&&door.openClip&&door.openClip.name=="PoliceIronDoor")door.closeClip=ironClose;
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
        foreach(var door in root.GetComponentsInChildren<ShopMechanism>()){if(door.fixtureId=="police-interview-door")door.label="律师会见室门";if(door.fixtureId=="police-evidence-door")door.label="保管室门";
            // The barred cell door is heavy: it swings slower than a panelled one, so the guard's shove carries it.
            if(door.fixtureId=="police-holding-door")door.swingSpeed=1.05f;}
        // A visitor room in the existing shared police building, rather than a modern interrogation set.
        var sign=new GameObject("律师会见室标牌");sign.transform.SetParent(group.transform,false);sign.transform.localPosition=new Vector3(12.912f,1.9f,3.5f);sign.transform.localRotation=Quaternion.Euler(0,90,0);
        var tm=sign.AddComponent<TextMesh>();tm.text="律师会见室";tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");tm.fontSize=64;tm.characterSize=.02f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.75f,.67f,.5f);
        // Painted beside the door on the hall face of the wall; depth-tested so it no longer shows through walls.
        const string signPath=Folder+"InterviewSignText.mat";var signMaterial=AssetDatabase.LoadAssetAtPath<Material>(signPath);
        if(!signMaterial){signMaterial=new Material(Shader.Find("WrongDate/RailWorldText"));AssetDatabase.CreateAsset(signMaterial,signPath);}
        tm.font.RequestCharactersInTexture(tm.text,64);signMaterial.mainTexture=tm.font.material.mainTexture;EditorUtility.SetDirty(signMaterial);
        var signRenderer=sign.GetComponent<MeshRenderer>();signRenderer.sharedMaterial=signMaterial;signRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var lettering=sign.AddComponent<WallLettering>();lettering.font=tm.font;lettering.material=signMaterial;
        // Checked like every generated prefab before it is saved (this group is added after the Town scene's
        // own pass). The cage is built without shared faces, so this should find nothing (ZFIGHT-01, OPENING-05).
        ZFightRepair.Repair(group.transform,"OpeningAct");
    }
    // The player's own leather briefcase, kept in the property room's locked cage until the guard hands it back.
    static void Briefcase(Transform parent) {
        // Standing on the stacked boxes inside the cage door, its flap toward the room.
        var root=new GameObject("公文包");root.transform.SetParent(parent,false);root.transform.localPosition=CaseHome+Vector3.up*.15f;root.transform.localRotation=Quaternion.Euler(0,180,0);
        var leather=Material("BriefcaseLeather",new Color(.27f,.15f,.08f),.42f,0);var dark=Material("BriefcaseSeam",new Color(.13f,.07f,.04f),.3f,0);var brass=Material("BriefcaseBrass",new Color(.72f,.56f,.26f),.62f,.85f);
        Part(root.transform,"Body",new Vector3(0,0,0),new Vector3(.42f,.30f,.11f),leather);
        Part(root.transform,"Top gusset",new Vector3(0,.152f,0),new Vector3(.40f,.008f,.09f),dark);
        // The front flap folds over the top and comes down a third of the face, held by two straps.
        Part(root.transform,"Flap",new Vector3(0,.098f,.0585f),new Vector3(.428f,.115f,.006f),leather);
        Part(root.transform,"Flap edge",new Vector3(0,.04f,.0615f),new Vector3(.428f,.006f,.003f),dark);
        foreach(float x in new[]{-.12f,.12f}) {
            Part(root.transform,x<0?"Strap left":"Strap right",new Vector3(x,.07f,.0625f),new Vector3(.028f,.15f,.003f),dark);
            Part(root.transform,x<0?"Buckle left":"Buckle right",new Vector3(x,.03f,.0645f),new Vector3(.036f,.026f,.004f),brass);
        }
        Part(root.transform,"Handle left",new Vector3(-.06f,.168f,0),new Vector3(.016f,.035f,.02f),dark);
        Part(root.transform,"Handle right",new Vector3(.06f,.168f,0),new Vector3(.016f,.035f,.02f),dark);
        Part(root.transform,"Handle grip",new Vector3(0,.193f,0),new Vector3(.15f,.02f,.026f),leather);
        var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,.02f,0);box.size=new Vector3(.46f,.36f,.14f);
        var item=root.AddComponent<OpeningBriefcase>();item.title="你的公文包";item.source="青川警署保管室";item.body="被带进来时登记保管的私人物品。";
    }
    // Building-local: the top of the upper box stack behind the cage door, where the case stands.
    static readonly Vector3 CaseHome=new Vector3(20.88f,1.21f,19.06f);
    // The property room's cage, rebuilt with a door that opens (the station's own cage mesh is one
    // closed piece): the same size, iron frame, wire front and stacked boxes as tools/police builds
    // (furniture ev-cage, 1.6 × 0.6 × 2.0 m against the north wall), with the right-hand part of the
    // front hung as a door on the corner post and a brass lock on its free edge. The station's
    // cage collider stays, so nothing inside can be taken through the wire.
    static void PropertyCage(Transform parent,GameObject station) {
        foreach(var r in station.GetComponentsInChildren<MeshRenderer>(true))if(r.name=="Furn_ev-cage")r.enabled=false;
        const string mats="Assets/Generated/Materials/Police/";
        var iron=AssetDatabase.LoadAssetAtPath<Material>(mats+"PInt_Iron.mat");var box=AssetDatabase.LoadAssetAtPath<Material>(mats+"PInt_Box.mat");var brass=AssetDatabase.LoadAssetAtPath<Material>(mats+"PInt_Brass.mat");
        if(!iron||!box||!brass)throw new Exception("Missing police cage materials");
        // Origin: the middle of the front at floor level; +Z runs into the cage.
        var cage=new GameObject("保管柜（可开门）");cage.transform.SetParent(parent,false);cage.transform.localPosition=new Vector3(20.4f,.15f,18.9f);
        void Bar(Transform to,string name,float x0,float y0,float z0,float x1,float y1,float z1,Material m)=>Part(to,name,new Vector3((x0+x1)/2,(y0+y1)/2,(z0+z1)/2),new Vector3(x1-x0,y1-y0,z1-z0),m);
        var t=cage.transform;
        // Parts that touch never share a face, which would flicker (z-fighting, AGENTS.md 共面闪烁): the
        // posts stand between the plates, the door's rails between its stiles, wires end inside the
        // members they meet, and the mesh is two layers deep, the uprights flush with the frame front and
        // the cross wires half a wire behind them, so crossing wires do not share a face either.
        const float up=0,cross=.006f,wire=.012f,back=cross+wire+.006f;
        Bar(t,"Base plate",-.8f,0,0,.8f,.06f,.6f,iron);Bar(t,"Top plate",-.8f,1.94f,0,.8f,2f,.6f,iron);
        foreach(float x in new[]{-.8f,.75f})foreach(float z in new[]{0f,.55f})Bar(t,"Corner post",x,.06f,z,x+.05f,1.94f,z+.05f,iron);
        // The fixed part of the wire front, left of the door, and its lock-side jamb.
        for(int i=8;i<=15;i++){float x=.8f-.1f*i;Bar(t,"Wire",x-.006f,.06f,up,x+.006f,1.94f,up+wire,iron);}
        Bar(t,"Door jamb",.088f,.06f,-.004f,.112f,1.94f,back,iron);
        for(int k=1;k<=5;k++){float y=2f*k/6;Bar(t,"Wire",-.775f,y-.006f,cross,.1f,y+.006f,cross+wire,iron);}
        Bar(t,"Hasp",.082f,1.0f,-.02f,.118f,1.08f,0,brass);
        // Stacked boxes of sealed property, three columns and two rows, as in the station's cage.
        foreach(var (x0,x1) in new[]{(.3f,.7f),(-.18f,.22f),(-.66f,-.26f)})foreach(float y in new[]{.06f,.76f})Bar(t,"Box",x0,y,.1f,x1,y+.3f,.54f,box);
        // The door: hung on the right-hand corner post, the leaf running left to its free edge.
        var door=new GameObject("Cage door");door.transform.SetParent(t,false);door.transform.localPosition=new Vector3(.75f,0,0);
        var d=door.transform;const float w=.63f;
        Bar(d,"Hinge stile",-.025f,.08f,-.002f,0,1.92f,back,iron);Bar(d,"Lock stile",-w,.08f,-.002f,-w+.025f,1.92f,back,iron);
        Bar(d,"Bottom rail",-w+.025f,.08f,-.002f,-.025f,.105f,back,iron);Bar(d,"Top rail",-w+.025f,1.895f,-.002f,-.025f,1.92f,back,iron);
        foreach(float x in new[]{-.05f,-.15f,-.25f,-.35f,-.45f,-.55f})Bar(d,"Wire",x-.006f,.105f,up,x+.006f,1.895f,up+wire,iron);
        for(int k=1;k<=5;k++){float y=2f*k/6;Bar(d,"Wire",-w+.0125f,y-.006f,cross,-.0125f,y+.006f,cross+wire,iron);}
        foreach(float y in new[]{.35f,1.6f})Bar(d,"Hinge",-.01f,y,-.02f,.02f,y+.09f,.02f,iron);
        Bar(d,"Lock",-w+.005f,.98f,-.04f,-w+.06f,1.1f,0,brass);
        var lockPoint=new GameObject("Lock point");lockPoint.transform.SetParent(d,false);lockPoint.transform.localPosition=new Vector3(-w+.035f,1.03f,-.045f);
        var home=new GameObject("Briefcase home");home.transform.SetParent(t,false);home.transform.localPosition=CaseHome-new Vector3(20.4f,.15f,18.9f);
        var item=cage.AddComponent<OpeningCage>();item.door=d;item.width=w;item.openDegrees=-100;item.lockPoint=lockPoint.transform;item.caseHome=home.transform;
        var audio=door.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=.6f;audio.minDistance=1;audio.maxDistance=10;audio.rolloffMode=AudioRolloffMode.Linear;
        item.sound=audio;
        item.unlockClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/OpeningSounds/CageUnlock.wav");
        item.openClip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/OpeningSounds/CageDoor.wav");
        if(!item.unlockClip||!item.openClip)throw new Exception("Missing cage sounds");
    }
    static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material) {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent,false);g.transform.localPosition=position;g.transform.localScale=size;
        var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
    }
    static Material Material(string name,Color color,float smoothness,float metallic) {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smoothness);m.SetFloat("_Metallic",metallic);EditorUtility.SetDirty(m);return m;
    }
    // Painted beside the property-room door on the cloister face of the wing wall, like the meeting-room sign.
    static void PropertySign(Transform parent) {
        var sign=new GameObject("保管室标牌");sign.transform.SetParent(parent,false);sign.transform.localPosition=new Vector3(17.492f,1.9f,18.15f);sign.transform.localRotation=Quaternion.Euler(0,90,0);
        var tm=sign.AddComponent<TextMesh>();tm.text="保管室";tm.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/TownSans.ttf");tm.fontSize=64;tm.characterSize=.02f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.75f,.67f,.5f);
        var signMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"InterviewSignText.mat");
        tm.font.RequestCharactersInTexture(tm.text,64);
        var r=sign.GetComponent<MeshRenderer>();r.sharedMaterial=signMaterial;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        var lettering=sign.AddComponent<WallLettering>();lettering.font=tm.font;lettering.material=signMaterial;
    }
    static GameObject Actor(Transform parent,string name,string role,Vector3 position,float yaw,bool police) {
        var g=PeriodCharacterAsset.AllPrepared?PeriodCharacterAsset.Create(parent,position,police?"CustodyGuard":"Lawyer",name):StationClerkAsset.Create(parent,position,police);
        g.name=name+(PeriodCharacterAsset.AllPrepared?"":" · 临时模型");g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        if(g.GetComponent<Interactable>())UnityEngine.Object.DestroyImmediate(g.GetComponent<Interactable>());
        var actor=g.AddComponent<OpeningActor>();actor.title=name;actor.role=role;
        var legacy=g.GetComponentInChildren<StationClerkPerformance>();if(legacy)legacy.speaker=name;
        var performance=g.GetComponentInChildren<PeriodCharacterPerformance>();if(performance)performance.speaker=name;
        foreach(var r in g.GetComponentsInChildren<Renderer>())r.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
        return g;
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
        StreetLifeAuthoring.Build();
        string path="Assets/Generated/PoliceStation.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{PoliceCastAuthoring.AddToPolice(root);AddToPolice(root);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        // The hospital's people follow their own approvals the same way: a newly approved figure replaces its stand-in here.
        const string hospitalPath="Assets/Generated/Hospital.prefab";var hospital=PrefabUtility.LoadPrefabContents(hospitalPath);
        try{HospitalCastAuthoring.AddToHospital(hospital);PrefabUtility.SaveAsPrefabAsset(hospital,hospitalPath);}finally{PrefabUtility.UnloadPrefabContents(hospital);}
        DialogueScript.Load("OpeningGuardDialogue").Validate();DialogueScript.Load("PoliceOfficerDialogue").Validate();DialogueScript.Load("OpeningLawyerDialogue").Validate();DialogueScript.Load("OpeningRideDialogue").Validate();
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
