using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// Import the approved Tripo child while retaining the existing E04 interaction.
public static class MilkboyAsset {
    const string Folder="Assets/Art/MilkboyTripo/";
    const string ModelPath=Folder+"Milkboy.fbx";
    const float NormalizedHeight=1.505281f;
    public static GameObject Create(Vector3 position,float yaw) {
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        if(!importer)throw new Exception("Run tools/import_tripo_milkboy.py with the approved v4 source.");
        if(!importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||importer.animationType!=ModelImporterAnimationType.Human||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        var definitions=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        bool changed=false;
        foreach(var clip in definitions) {
            if(!clip.loopTime||!clip.loopPose||!clip.lockRootPositionXZ||!clip.lockRootRotation||!clip.lockRootHeightY) {
                clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;changed=true;
            }
        }
        if(changed){importer.clipAnimations=definitions;importer.SaveAndReimport();}
        var all=AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        var avatar=all.OfType<Avatar>().Single();
        if(!avatar.isHuman||!avatar.isValid)throw new Exception("Tripo child requires a valid Humanoid Avatar.");
        var clips=all.OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        if(clips.Length!=2)throw new Exception("Tripo child must contain exactly two animations.");
        var idle=clips.Single(c=>c.name.Contains("HoldShoulderStrap"));var talk=clips.Single(c=>c.name.Contains("Talk_WithMouth"));
        var root=new GameObject("Milkboy");
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));visual.transform.SetParent(root.transform,false);
        var renderer=visual.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
        if(renderer.sharedMesh.subMeshCount!=2)throw new Exception("Expected body and inner-mouth material slots.");
        var head=visual.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head");
        // Derive facing from the real mouth surface, independent of FBX axes.
        var vertices=renderer.sharedMesh.vertices;var mouthIndices=renderer.sharedMesh.GetTriangles(1).Distinct().ToArray();
        Vector3 mouth=Vector3.zero;foreach(int i in mouthIndices)mouth+=renderer.transform.TransformPoint(vertices[i]);mouth/=mouthIndices.Length;
        Vector3 front=mouth-head.position;front.y=0;
        if(front.sqrMagnitude<.0001f)throw new Exception("Cannot determine child face direction.");
        visual.transform.rotation=Quaternion.AngleAxis(Vector3.SignedAngle(front,Vector3.back,Vector3.up),Vector3.up)*visual.transform.rotation;
        renderer.sharedMaterials=new[]{PrepareMaterial(),MouthMaterial()};renderer.updateWhenOffscreen=true;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
        var bounds=MeshBounds(renderer);if(bounds.size.y<.5f||bounds.size.y>3f)throw new Exception("Child import scale: "+bounds.size);
        visual.transform.localScale*=NormalizedHeight/bounds.size.y;bounds=MeshBounds(renderer);visual.transform.position-=Vector3.up*bounds.min.y;
        var headFront=new GameObject("headfront").transform;headFront.SetParent(head,true);headFront.position=head.position+Vector3.back*.08f;
        var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
        animator.avatar=avatar;animator.runtimeAnimatorController=Controller(idle,talk);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var performance=visual.AddComponent<MilkboyPerformance>();performance.animator=animator;performance.head=head;performance.headFront=headFront;
        performance.headUpLocal=head.InverseTransformDirection(Vector3.up);
        idle.SampleAnimation(visual,0);
        root.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
        Debug.Log("MILKBOY_IMPORT_PASS: valid Humanoid; height="+bounds.size.y+" bones="+renderer.bones.Length+" clips=2 idle="+idle.name+" talk="+talk.name);
        return root;
    }
    static Bounds MeshBounds(SkinnedMeshRenderer r) {var v=r.sharedMesh.vertices;var b=new Bounds(r.transform.TransformPoint(v[0]),Vector3.zero);foreach(var p in v)b.Encapsulate(r.transform.TransformPoint(p));return b;}
    static Texture2D Texture(string name,TextureImporterType type,bool srgb) {
        string path=Folder+name;var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Missing child texture: "+path);
        if(importer.textureType!=type||importer.sRGBTexture!=srgb||importer.maxTextureSize!=2048||!importer.mipmapEnabled) {
            importer.textureType=type;importer.sRGBTexture=srgb;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material PrepareMaterial() {
        string path=Folder+"Milkboy.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",Color.white);m.SetTexture("_BaseMap",Texture("BaseColor.png",TextureImporterType.Default,true));
        m.SetTexture("_BumpMap",Texture("Normal.png",TextureImporterType.NormalMap,false));m.SetFloat("_BumpScale",1);m.EnableKeyword("_NORMALMAP");
        m.SetTexture("_MetallicGlossMap",Texture("MetallicSmoothness.png",TextureImporterType.Default,false));m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.SetFloat("_Cull",(float)CullMode.Off);m.doubleSidedGI=true;EditorUtility.SetDirty(m);return m;
    }
    static Material MouthMaterial() {
        string path=Folder+"MouthInterior.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",new Color(.065f,.008f,.012f));m.SetFloat("_Smoothness",.1f);m.SetFloat("_Cull",(float)CullMode.Off);EditorUtility.SetDirty(m);return m;
    }
    static AnimatorController Controller(AnimationClip idle,AnimationClip talk) {
        const string path="Assets/Generated/MilkboyTripo.controller";var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!c)c=AnimatorController.CreateAnimatorControllerAtPath(path);if(c.layers.Length==0)c.AddLayer("Base Layer");
        if(!c.parameters.Any(p=>p.name=="Speaking"))c.AddParameter("Speaking",AnimatorControllerParameterType.Bool);
        var machine=c.layers[0].stateMachine;
        AnimatorState State(string name,AnimationClip motion) {var st=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);st.motion=motion;return st;}
        var idleState=State("Idle",idle);var talkState=State("Talk",talk);machine.defaultState=idleState;
        void Transition(AnimatorState from,AnimatorState to,AnimatorConditionMode mode) {
            var t=from.transitions.FirstOrDefault(v=>v.destinationState==to)??from.AddTransition(to);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=.25f;
            foreach(var condition in t.conditions)t.RemoveCondition(condition);t.AddCondition(mode,0,"Speaking");
        }
        Transition(idleState,talkState,AnimatorConditionMode.If);Transition(talkState,idleState,AnimatorConditionMode.IfNot);EditorUtility.SetDirty(c);return c;
    }
}
