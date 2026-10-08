using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Import the user-supplied Meshy messenger without changing the E04 actor contract.
public static class MilkboyAsset {
    const string Folder="Assets/Art/MilkboyMeshy/";
    const string ModelPath=Folder+"Messenger.fbx";
    const string IdleName="Short_Breathe_and_Look_Around";
    const float NormalizedHeight=1.505281f;
    public static GameObject Create(Vector3 position,float yaw) {
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if(importer==null)throw new Exception("Run tools/import_meshy_milkboy.py with the supplied archive first.");
        if(!importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||
            importer.animationType!=ModelImporterAnimationType.Generic||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=true;importer.animationType=ModelImporterAnimationType.Generic;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }
        var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        bool changed=false;
        foreach(var clip in clips.Where(c=>c.name.Contains(IdleName))) {
            if(!clip.loopTime||!clip.loopPose||!clip.lockRootPositionXZ||!clip.lockRootRotation) {
                clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;changed=true;
            }
        }
        if(changed){importer.clipAnimations=clips;importer.SaveAndReimport();}
        var animations=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var idle=animations.Single(c=>c.name.Contains(IdleName));
        if(!idle.isLooping)throw new Exception("Milkboy idle must loop.");
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var root=new GameObject("Milkboy");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(asset);
        visual.transform.SetParent(root.transform,false);
        // The source supplies a head-front bone, so facing is independent of FBX axis conventions.
        var bones=visual.GetComponentsInChildren<Transform>();
        var head=bones.Single(t=>t.name.Replace(':','_')=="mixamorig_Head");
        var headFront=bones.Single(t=>t.name=="headfront");
        Vector3 front=headFront.position-head.position;front.y=0;
        if(front.sqrMagnitude<.001f)throw new Exception("Milkboy facing cannot be determined.");
        visual.transform.rotation=Quaternion.FromToRotation(front.normalized,Vector3.back);
        var renderer=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
        renderer.sharedMaterials=renderer.sharedMaterials.Select(_=>PrepareMaterial()).ToArray();
        renderer.updateWhenOffscreen=true;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        var bounds=MeshBounds(renderer);
        if(bounds.size.y<.5f||bounds.size.y>3f)throw new Exception("Milkboy import scale: "+bounds.size);
        visual.transform.localScale*=NormalizedHeight/bounds.size.y;
        bounds=MeshBounds(renderer);
        visual.transform.position-=Vector3.up*bounds.min.y;
        var animator=visual.GetComponent<Animator>();
        if(!animator)animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=IdleController(idle);animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Debug.Log("MILKBOY_IMPORT_PASS: height="+bounds.size.y+" bones="+renderer.bones.Length+" clips="+animations.Length+" idle="+idle.name+" source=user-supplied Meshy messenger");
        return root;
    }
    static Bounds MeshBounds(SkinnedMeshRenderer renderer) {
        var points=renderer.sharedMesh.vertices;var bounds=new Bounds(renderer.transform.TransformPoint(points[0]),Vector3.zero);
        foreach(var p in points)bounds.Encapsulate(renderer.transform.TransformPoint(p));
        return bounds;
    }
    static Texture2D Texture(string name,TextureImporterType type,bool srgb) {
        string path=Folder+name;var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        if(importer==null)throw new Exception("Missing Milkboy texture: "+path);
        if(importer.textureType!=type||importer.sRGBTexture!=srgb||importer.maxTextureSize!=2048||!importer.mipmapEnabled) {
            importer.textureType=type;importer.sRGBTexture=srgb;importer.maxTextureSize=2048;
            importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material PrepareMaterial() {
        string path=Folder+"Messenger.mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",Texture("BaseColor.png",TextureImporterType.Default,true));
        m.SetTexture("_BumpMap",Texture("Normal.png",TextureImporterType.NormalMap,false));
        m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness.png",TextureImporterType.Default,false));
        m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Cull",(float)CullMode.Off);m.doubleSidedGI=true;
        EditorUtility.SetDirty(m);return m;
    }
    static AnimatorController IdleController(AnimationClip clip) {
        const string path="Assets/Generated/MilkboyIdle.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(controller.layers.Length==0)controller.AddLayer("Base Layer");
        var machine=controller.layers[0].stateMachine;
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Idle")??machine.AddState("Idle");
        state.motion=clip;machine.defaultState=state;EditorUtility.SetDirty(controller);return controller;
    }
}
