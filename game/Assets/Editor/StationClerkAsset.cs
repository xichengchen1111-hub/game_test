using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

public static class StationClerkAsset {
    const string Folder="Assets/Art/StationClerkTripo/";
    const string Model=Folder+"StationClerk.fbx";
    // policeOfficer: the D police station's duty officer reuses this approved rig and clips with a
    // navy-recoloured uniform (tools/police/make_officer_texture.py) until a dedicated model exists.
    // mechanic: the E garage's 修车匠 (B07) uses the same placeholder approach with a faded brown
    // work-canvas recolour (tools/garage/make_mechanic_texture.py) until a dedicated model is made.
    public static GameObject Create(Transform parent,Vector3 position,bool policeOfficer=false,bool mechanic=false) {
        var importer=AssetImporter.GetAtPath(Model) as ModelImporter;
        if(!importer)throw new Exception("Import the approved Tripo station clerk before building.");
        if(!importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||
            importer.animationType!=ModelImporterAnimationType.Generic||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
            importer.animationType=ModelImporterAnimationType.Generic;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        var definitions=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        bool changed=false;
        foreach(var clip in definitions) {
            if(!clip.loopTime||clip.loopPose||!clip.lockRootPositionXZ||!clip.lockRootRotation||!clip.lockRootHeightY) {
                clip.loopTime=true;clip.loopPose=false;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;changed=true;
            }
        }
        if(changed){importer.clipAnimations=definitions;importer.SaveAndReimport();}
        var clips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        if(clips.Length!=2)throw new Exception("Station clerk requires exactly idle and talk clips.");
        var idle=clips.Single(c=>c.name.Contains("Idle"));var talk=clips.Single(c=>c.name.Contains("Talk"));
        var root=new GameObject(mechanic?"修车匠":policeOfficer?"值班警员":"车站售票员");root.transform.SetParent(parent,false);root.transform.localPosition=position;
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));visual.transform.SetParent(root.transform,false);
        var bones=visual.GetComponentsInChildren<Transform>();var head=bones.Single(t=>t.name=="Head");var front=bones.Single(t=>t.name=="headfront");
        SaveAvatar(visual,bones);
        var direction=front.position-head.position;direction.y=0;
        if(direction.sqrMagnitude<.0001f)throw new Exception("Station clerk face marker is missing.");
        visual.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*visual.transform.rotation;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            skin.sharedMaterials=skin.sharedMaterials.Select((m,i)=>Material(i==0?(mechanic?"MechanicBody":policeOfficer?"OfficerBody":"ClerkBody"):"MouthInterior")).ToArray();
            skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
        }
        var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=Controller(idle,talk);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var performance=visual.AddComponent<StationClerkPerformance>();performance.animator=animator;if(policeOfficer)performance.speaker="值班警员";if(mechanic)performance.speaker="修车匠";
        idle.SampleAnimation(visual,0);
        var solid=root.AddComponent<BoxCollider>();solid.center=new Vector3(0,.95f,0);solid.size=new Vector3(.46f,1.85f,.36f);
        if(mechanic){var mechanicTalk=root.AddComponent<Interactable>();mechanicTalk.title="修车匠";mechanicTalk.source="青川修车铺修理车间";mechanicTalk.body="修车匠站在货车旁，手上还有机油。";Debug.Log("GARAGE_MECHANIC_IMPORT_PASS: clerk rig placeholder, work-canvas texture, two in-place animations");return root;}
        if(policeOfficer){var officerTalk=root.AddComponent<Interactable>();officerTalk.title="值班警员";officerTalk.source="青川警署值班接待窗口";officerTalk.body="值班警员在窗口后整理文书。";Debug.Log("POLICE_OFFICER_IMPORT_PASS: clerk rig, navy uniform texture, two in-place animations");return root;}
        var conversation=root.AddComponent<Interactable>();conversation.title="售票员";conversation.source="青川车站售票处";
        conversation.body="检票的时候，会用检票钳在票上打孔。旁边废票桶里的同线路旧票，你可以拿起来看看。\n\n请从候车厅后门前往站台，车厢靠站台的一侧可以登车。坐稳后，列车便会出发。";
        Debug.Log("STATION_CLERK_IMPORT_PASS: Tripo model, two in-place animations, reusable Humanoid Avatar");return root;
    }
    static void SaveAvatar(GameObject visual,Transform[] bones) {
        var human=Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>().Where(b=>b!=HumanBodyBones.LastBone)
            .Select(b=>new{id=b,name=(b.ToString().StartsWith("Left")?"Left_"+b.ToString().Substring(4):b.ToString().StartsWith("Right")?"Right_"+b.ToString().Substring(5):b.ToString()).Replace("Little","Pinky")})
            .Where(b=>bones.Any(t=>t.name==b.name)).Select(b=>new HumanBone{boneName=b.name,humanName=HumanTrait.BoneName[(int)b.id],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
        var description=new HumanDescription{human=human,skeleton=bones.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
            armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,hasTranslationDoF=false};
        var avatar=AvatarBuilder.BuildHumanAvatar(visual,description);avatar.name="Station clerk Humanoid";
        if(!avatar.isHuman||!avatar.isValid)throw new Exception("Station clerk must produce a valid Humanoid Avatar.");
        string path=Folder+"ClerkHumanoid.asset";var saved=AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if(saved){EditorUtility.CopySerialized(avatar,saved);UnityEngine.Object.DestroyImmediate(avatar);EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(avatar,path);
    }
    static AnimatorController Controller(AnimationClip idle,AnimationClip talk) {
        const string path="Assets/Generated/StationClerkTripo.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!c)c=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(!c.parameters.Any(p=>p.name=="Speaking"))c.AddParameter("Speaking",AnimatorControllerParameterType.Bool);
        var machine=c.layers[0].stateMachine;
        AnimatorState State(string name,AnimationClip motion) {var st=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);st.motion=motion;return st;}
        var a=State("Idle",idle);var b=State("Talk",talk);machine.defaultState=a;
        void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode) {
            var t=from.transitions.FirstOrDefault(v=>v.destinationState==to)??from.AddTransition(to);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=.25f;
            foreach(var condition in t.conditions)t.RemoveCondition(condition);t.AddCondition(mode,0,"Speaking");
        }
        Transition(a,b,AnimatorConditionMode.If);Transition(b,a,AnimatorConditionMode.IfNot);EditorUtility.SetDirty(c);return c;
    }
    static Texture2D Texture(string name,TextureImporterType type,bool srgb) {
        var path=Folder+name;var i=AssetImporter.GetAtPath(path) as TextureImporter;if(!i)throw new Exception("Missing clerk texture "+path);
        if(i.textureType!=type||i.sRGBTexture!=srgb||i.maxTextureSize!=2048||!i.mipmapEnabled) {i.textureType=type;i.sRGBTexture=srgb;i.maxTextureSize=2048;i.mipmapEnabled=true;i.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D OfficerTexture() {
        const string path="Assets/Art/PoliceOfficer/BaseColor.png";var i=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!i)throw new Exception("Run tools/police/make_officer_texture.py first: "+path);
        if(i.maxTextureSize!=2048||!i.sRGBTexture||!i.mipmapEnabled){i.maxTextureSize=2048;i.sRGBTexture=true;i.mipmapEnabled=true;i.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D MechanicTexture() {
        const string path="Assets/Art/Garage/Mechanic/BaseColor.png";var i=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!i)throw new Exception("Run tools/garage/make_mechanic_texture.py first: "+path);
        if(i.maxTextureSize!=2048||!i.sRGBTexture||!i.mipmapEnabled){i.maxTextureSize=2048;i.sRGBTexture=true;i.mipmapEnabled=true;i.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name) {
        var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        if(name=="ClerkBody"||name=="OfficerBody"||name=="MechanicBody") {
            m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",name=="OfficerBody"?OfficerTexture():name=="MechanicBody"?MechanicTexture():Texture("BaseColor.png",TextureImporterType.Default,true));
            m.SetTexture("_BumpMap",Texture("Normal.png",TextureImporterType.NormalMap,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness.png",TextureImporterType.Default,false));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
        } else {m.SetColor("_BaseColor",new Color(.065f,.008f,.012f));m.SetFloat("_Smoothness",.1f);}
        m.SetFloat("_Cull",(float)CullMode.Off);EditorUtility.SetDirty(m);return m;
    }
}
