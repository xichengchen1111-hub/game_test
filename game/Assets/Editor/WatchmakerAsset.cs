using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// User-supplied watchmaker, seated for the entire slice. The shared shop prefab owns the chair too.
public static class WatchmakerAsset {
    const string Folder="Assets/Art/WatchmakerMeshy/";
    const string ModelPath=Folder+"Watchmaker.fbx";
    const string SitName="Sit_Cross_Legged";
    const float StandingHeight=1.72f;
    public const float SeatHeight=.40f;
    // Measured supplied seated mesh: pelvis bone to the supporting apron/buttock surface,
    // scaled from source rest height 1.70m to the existing adult height 1.72m.
    const float SeatedHipAboveSeat=.284f;
    public static GameObject Create(Vector3 position) {
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if(!importer)throw new Exception("Run tools/import_meshy_watchmaker.py with the supplied archive first.");
        if(!importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||
            importer.animationType!=ModelImporterAnimationType.Generic||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;
            importer.isReadable=true;importer.animationType=ModelImporterAnimationType.Generic;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        bool changed=false;
        foreach(var clip in clips.Where(c=>c.name.Contains(SitName))) {
            if(!clip.loopTime||!clip.loopPose||!clip.lockRootPositionXZ||!clip.lockRootRotation||!clip.lockRootHeightY) {
                clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=true;
                clip.lockRootRotation=true;clip.lockRootHeightY=true;changed=true;
            }
        }
        if(changed){importer.clipAnimations=clips;importer.SaveAndReimport();}
        var animations=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var sit=animations.Single(c=>c.name.Contains(SitName));
        if(!sit.isLooping)throw new Exception("Watchmaker sitting animation must loop.");
        var root=new GameObject("Clockmaker");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        visual.transform.SetParent(root.transform,false);
        var bones=visual.GetComponentsInChildren<Transform>();
        Transform Bone(string name)=>bones.Single(t=>t.name.Replace(':','_')==name);
        Vector3 front=Bone("headfront").position-Bone("mixamorig_Head").position;front.y=0;
        if(front.sqrMagnitude<.001f)throw new Exception("Watchmaker facing cannot be determined.");
        visual.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(front.normalized,Vector3.back,Vector3.up),Vector3.up);
        var skin=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
        skin.sharedMaterials=skin.sharedMaterials.Select(_=>PrepareMaterial()).ToArray();
        skin.updateWhenOffscreen=true;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
        var restBounds=RestBounds(skin);
        if(restBounds.size.y<.5f||restBounds.size.y>3f)throw new Exception("Watchmaker rest scale: "+restBounds.size);
        visual.transform.localScale*=StandingHeight/restBounds.size.y;
        // Sample the supplied seated pose before saving the prefab: no standing flash on scene load.
        sit.SampleAnimation(visual,0);
        var hips=Bone("mixamorig_Hips");
        visual.transform.position+=new Vector3(-hips.position.x,SeatHeight+SeatedHipAboveSeat-hips.position.y,-hips.position.z);
        var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
        animator.runtimeAnimatorController=Controller(sit);animator.applyRootMotion=false;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var chairAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/WatchmakerChair/WatchmakerChair.fbx");
        if(!chairAsset)throw new Exception("Run tools/build_watchmaker_chair.py first.");
        var chair=(GameObject)PrefabUtility.InstantiatePrefab(chairAsset);chair.name="Watchmaker chair";
        chair.transform.SetParent(root.transform,false);
        // Static FBX and animated FBX have different import axes. Determine the actual back,
        // rather than assigning an unverified yaw and putting the backrest in his lap.
        var chairParts=chair.GetComponentsInChildren<Transform>();
        var back=chairParts.Single(t=>t.name=="Back top rail");
        var seat=chairParts.Single(t=>t.name=="ChairSeat");
        var backDirection=back.position-seat.position;backDirection.y=0;
        if(backDirection.sqrMagnitude<.01f)throw new Exception("Chair back direction cannot be determined.");
        chair.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(backDirection.normalized,Vector3.forward,Vector3.up),Vector3.up);
        foreach(var r in chair.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>ChairMaterial(m.name)).ToArray();
        if(Mathf.Abs(seat.GetComponent<Renderer>().bounds.max.y-SeatHeight)>.005f||Vector3.Dot(back.position-seat.position,Vector3.forward)<.2f)
            throw new Exception("Chair must remain upright with its back behind the owner.");
        var body=root.AddComponent<BoxCollider>();body.center=new Vector3(0,.94f,0);body.size=new Vector3(.58f,.94f,.50f);
        var solid=chair.AddComponent<BoxCollider>();solid.center=new Vector3(0,.20f,0);solid.size=new Vector3(.60f,.40f,.59f);
        root.transform.position=position;
        Debug.Log("WATCHMAKER_IMPORT_PASS: restHeight="+StandingHeight+" bones="+skin.bones.Length+" clips="+animations.Length+" sitting="+sit.name+" length="+sit.length+" seatedHip="+hips.position+" seat="+SeatHeight);
        return root;
    }
    static Bounds RestBounds(SkinnedMeshRenderer skin) {
        var points=skin.sharedMesh.vertices;var bounds=new Bounds(skin.transform.TransformPoint(points[0]),Vector3.zero);
        foreach(var p in points)bounds.Encapsulate(skin.transform.TransformPoint(p));return bounds;
    }
    static Texture2D Texture(string name,TextureImporterType type,bool srgb) {
        string path=Folder+name;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing watchmaker texture: "+path);
        if(importer.textureType!=type||importer.sRGBTexture!=srgb||importer.maxTextureSize!=2048||!importer.mipmapEnabled) {
            importer.textureType=type;importer.sRGBTexture=srgb;importer.maxTextureSize=2048;
            importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material PrepareMaterial() {
        string path=Folder+"Watchmaker.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",Texture("BaseColor.png",TextureImporterType.Default,true));
        m.SetTexture("_BumpMap",Texture("Normal.png",TextureImporterType.NormalMap,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness.png",TextureImporterType.Default,false));
        m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Cull",(float)CullMode.Off);m.doubleSidedGI=true;EditorUtility.SetDirty(m);return m;
    }
    static Material ChairMaterial(string name) {
        string path="Assets/Art/WatchmakerChair/"+name.Split('.')[0]+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString(name.StartsWith("ChairLeather")?"#473C32":"#69503B",out var c);
        m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",.2f);EditorUtility.SetDirty(m);return m;
    }
    static AnimatorController Controller(AnimationClip clip) {
        const string path="Assets/Generated/WatchmakerSeated.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(controller.layers.Length==0)controller.AddLayer("Base Layer");
        var machine=controller.layers[0].stateMachine;
        foreach(var child in machine.states.Where(s=>s.state.name!="Seated").ToArray())machine.RemoveState(child.state);
        var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="Seated")??machine.AddState("Seated");
        state.motion=clip;machine.defaultState=state;EditorUtility.SetDirty(controller);return controller;
    }
}
