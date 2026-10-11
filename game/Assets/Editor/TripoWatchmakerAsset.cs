using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// The authored seated motions retain their exact hand/tool contact through Generic playback.
// Bone names follow the Tripo Unity humanoid preset, with the rest rig kept in the source.
public static class TripoWatchmakerAsset {
    const string Folder="Assets/Art/WatchmakerTripo/";
    const string Model=Folder+"Watchmaker.fbx";
    static readonly string[] Names={"RepairScrew","InspectMovement","AdjustParts"};
    public static GameObject Create(Vector3 position) {
        var importer=AssetImporter.GetAtPath(Model) as ModelImporter;
        if(!importer)throw new Exception("Missing authored Tripo watchmaker.");
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
        var clips=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<AnimationClip>().Where(a=>!a.name.StartsWith("__preview__")).ToArray();
        if(clips.Length!=3||Names.Any(n=>clips.Count(c=>c.name.Contains(n))!=1))throw new Exception("Watchmaker must have three repair clips.");
        var root=new GameObject("Clockmaker");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));visual.transform.SetParent(root.transform,false);
        var bones=visual.GetComponentsInChildren<Transform>();
        SaveHumanoidAvatar(visual,bones);
        var head=bones.Single(t=>t.name=="Head");var front=bones.Single(t=>t.name=="headfront");
        var direction=front.position-head.position;direction.y=0;
        if(direction.sqrMagnitude<.0001f)throw new Exception("Missing watchmaker facing marker.");
        visual.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*visual.transform.rotation;
        foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>()) {
            if(skin.sharedMesh.subMeshCount!=4)throw new Exception("Expected body, brass, steel and wooden tool materials.");
            skin.sharedMaterials=skin.sharedMaterials.Select((m,i)=>Material(new[]{"WatchmakerBody","RepairBrass","RepairSteel","RepairWood"}[i])).ToArray();skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;
        }
        var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=Controller(clips);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        clips.Single(c=>c.name.Contains(Names[0])).SampleAnimation(visual,0);
        var performance=visual.AddComponent<WatchmakerPerformance>();performance.animator=animator;
        var chairAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/WatchmakerChair/WatchmakerChair.fbx");
        var chair=(GameObject)PrefabUtility.InstantiatePrefab(chairAsset);chair.name="Watchmaker chair";chair.transform.SetParent(root.transform,false);
        var parts=chair.GetComponentsInChildren<Transform>();var back=parts.Single(t=>t.name=="Back top rail");var seat=parts.Single(t=>t.name=="ChairSeat");
        var backDirection=back.position-seat.position;backDirection.y=0;
        chair.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(backDirection,Vector3.forward,Vector3.up),Vector3.up);
        foreach(var renderer in chair.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>ChairMaterial(m.name)).ToArray();
        if(Mathf.Abs(seat.GetComponent<Renderer>().bounds.max.y-.40f)>.005f||Vector3.Dot(back.position-seat.position,Vector3.forward)<.2f)throw new Exception("Chair orientation/support changed.");
        var body=root.AddComponent<BoxCollider>();body.center=new Vector3(0,.87f,0);body.size=new Vector3(.58f,.94f,.5f);
        var solid=chair.AddComponent<BoxCollider>();solid.center=new Vector3(0,.2f,0);solid.size=new Vector3(.6f,.4f,.59f);
        root.transform.position=position;
        Debug.Log("WATCHMAKER_IMPORT_PASS: Tripo, three authored seated repair clips, Unity skeleton, root motion off");return root;
    }
    static void SaveHumanoidAvatar(GameObject visual,Transform[] bones) {
        var human=Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>().Where(b=>b!=HumanBodyBones.LastBone)
            .Select(b=>new {id=b,name=b.ToString().StartsWith("Left")?"Left_"+b.ToString().Substring(4):b.ToString().StartsWith("Right")?"Right_"+b.ToString().Substring(5):b.ToString()})
            .Where(b=>bones.Any(t=>t.name==b.name)).Select(b=>new HumanBone{boneName=b.name,humanName=HumanTrait.BoneName[(int)b.id],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
        var description=new HumanDescription{human=human,skeleton=bones.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
            armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,feetSpacing=0,hasTranslationDoF=false};
        var avatar=AvatarBuilder.BuildHumanAvatar(visual,description);avatar.name="Watchmaker Humanoid";
        if(!avatar.isHuman||!avatar.isValid)throw new Exception("Watchmaker skeleton must also produce a valid Humanoid Avatar.");
        const string path=Folder+"WatchmakerHumanoid.asset";var saved=AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if(saved){EditorUtility.CopySerialized(avatar,saved);UnityEngine.Object.DestroyImmediate(avatar);EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(avatar,path);
        Debug.Log("WATCHMAKER_HUMANOID_PASS: reusable valid Humanoid Avatar; authored contact clips play through Generic rig");
    }
    static AnimatorController Controller(AnimationClip[] clips) {
        const string path="Assets/Generated/WatchmakerTripo.controller";
        var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(!c)c=AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm=c.layers[0].stateMachine;
        foreach(var name in Names) {
            var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??sm.AddState(name);
            state.motion=clips.Single(a=>a.name.Contains(name));if(name==Names[0])sm.defaultState=state;
        }
        EditorUtility.SetDirty(c);return c;
    }
    static Texture2D Texture(string name,TextureImporterType type,bool srgb) {
        var path=Folder+name;var i=AssetImporter.GetAtPath(path) as TextureImporter;if(!i)throw new Exception("Missing watchmaker texture "+path);
        if(i.textureType!=type||i.sRGBTexture!=srgb||i.maxTextureSize!=2048||!i.mipmapEnabled) {i.textureType=type;i.sRGBTexture=srgb;i.maxTextureSize=2048;i.mipmapEnabled=true;i.SaveAndReimport();}
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name) {
        name=name.Split('.')[0];var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        if(name=="WatchmakerBody") {
            m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",Texture("BaseColor.png",TextureImporterType.Default,true));
            m.SetTexture("_BumpMap",Texture("Normal.png",TextureImporterType.NormalMap,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness.png",TextureImporterType.Default,false));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
        } else {
            ColorUtility.TryParseHtmlString(name=="RepairBrass"?"#AE8950":name=="RepairSteel"?"#939B9B":"#42372A",out var color);
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",name=="RepairWood"?0:.7f);m.SetFloat("_Smoothness",.38f);
        }
        m.SetFloat("_Cull",(float)CullMode.Off);EditorUtility.SetDirty(m);return m;
    }
    static Material ChairMaterial(string name) {
        var path="Assets/Art/WatchmakerChair/"+name.Split('.')[0]+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)throw new Exception("Missing existing chair material "+path);return m;
    }
}
