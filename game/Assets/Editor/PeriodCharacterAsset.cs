using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;

// The source pipeline owns proportions and metre-scale geometry. Each cast
// member keeps an independent material, controller and reusable avatar asset.
public static class PeriodCharacterAsset {
    const string CastFolder="Assets/Art/PoliceCast/";
    public static readonly string[] Ids={"Lawyer","CustodyGuard","DutyOfficer","PatrolOfficer","ReceptionOfficer"};
    [Serializable] sealed class CastAcceptance {public int schemaVersion;public CharacterAcceptance[] characters;}
    [Serializable] sealed class CharacterAcceptance {
        public string role;
        public bool approved;
        public string characterSha256,baseColorSha256,normalSha256,metallicSmoothnessSha256;
    }
    [Serializable] sealed class MotionCalibration {public float walkMetresPerSecond;public bool calibrationRequired=true;}
    // Download/import success is not appearance acceptance. Approval is explicit
    // and bound to the reviewed files; a new export or texture revokes eligibility
    // until its hashes are reviewed again. Never generate approvals here.
    public static bool AllPrepared {
        get {
            try {
                string repository=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
                string manifest=Path.Combine(repository,"assets/source/police-cast/acceptance.json");
                if(!File.Exists(manifest))return false;
                var acceptance=JsonUtility.FromJson<CastAcceptance>(File.ReadAllText(manifest));
                if(acceptance==null||acceptance.schemaVersion!=1||acceptance.characters==null||acceptance.characters.Length!=Ids.Length)return false;
                string[] files={"Character.fbx","BaseColor.png","Normal.png","MetallicSmoothness.png"};
                foreach(string id in Ids) {
                    var matches=acceptance.characters.Where(c=>c!=null&&c.role==id).ToArray();
                    if(matches.Length!=1||!matches[0].approved)return false;
                    var character=matches[0];
                    string[] hashes={character.characterSha256,character.baseColorSha256,character.normalSha256,character.metallicSmoothnessSha256};
                    for(int i=0;i<files.Length;i++) {
                        string path=Path.Combine(repository,"game",CastFolder,id,files[i]);
                        if(!ApprovedContent(path,hashes[i]))return false;
                    }
                    if(!AssetDatabase.LoadAssetAtPath<GameObject>(CastFolder+id+"/Character.fbx"))return false;
                    var clips=AssetDatabase.LoadAllAssetsAtPath(CastFolder+id+"/Character.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                    if(!HasRequiredClips(clips))return false;
                    foreach(string texture in files.Skip(1))if(!AssetDatabase.LoadAssetAtPath<Texture2D>(CastFolder+id+"/"+texture))return false;
                }
                return true;
            } catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is CryptographicException) {
                // Missing, unreadable or malformed acceptance never changes a cast.
                return false;
            }
        }
    }
    static bool ApprovedContent(string path,string expected) {
        if(expected==null||expected.Length!=64||!expected.All(Uri.IsHexDigit)||!File.Exists(path))return false;
        using(var stream=File.OpenRead(path))using(var algorithm=SHA256.Create()) {
            string actual=BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-","");
            return string.Equals(actual,expected,StringComparison.OrdinalIgnoreCase);
        }
    }
    public static float Height(string id) => id switch {
        "Lawyer"=>1.80f,"CustodyGuard"=>1.83f,"DutyOfficer"=>1.65f,"PatrolOfficer"=>1.76f,"ReceptionOfficer"=>1.60f,
        _=>throw new ArgumentException("Unknown period character: "+id)
    };
    public static float Width(string id) => id switch {
        "Lawyer"=>.46f,"CustodyGuard"=>.62f,"DutyOfficer"=>.68f,"PatrolOfficer"=>.46f,"ReceptionOfficer"=>.54f,
        _=>throw new ArgumentException("Unknown period character: "+id)
    };
    public static GameObject Create(Transform parent,Vector3 position,string id,string title) {
        float height=Height(id),width=Width(id);
        string folder=CastFolder+id+"/",model=folder+"Character.fbx";
        ImportModel(model);
        var clips=AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        if(!HasRequiredClips(clips))throw new Exception(id+" requires distinct non-empty Idle, Walk and Talk clips from the source export.");
        var idle=RequireClip(clips,"Idle",id);var walk=RequireClip(clips,"Walk",id);var talk=RequireClip(clips,"Talk",id);
        var root=new GameObject(title);root.transform.SetParent(parent,false);root.transform.localPosition=position;
        try {
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
            visual.transform.SetParent(root.transform,false);visual.name=id+" visual";
            var bones=visual.GetComponentsInChildren<Transform>(true);
            var head=RequireBone(bones,"Head",id);var front=RequireBone(bones,"headfront",id);
            SaveAvatar(visual,bones,folder,id);
            var direction=root.transform.InverseTransformVector(front.position-head.position);direction.y=0;
            if(direction.sqrMagnitude<.0001f)throw new Exception(id+" has no usable face direction marker.");
            visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*visual.transform.localRotation;
            var body=CharacterMaterial(folder,false);var mouth=CharacterMaterial(folder,true);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true)) {
                renderer.sharedMaterials=renderer.sharedMaterials.Select((_,i)=>i==0?body:mouth).ToArray();
                renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
                if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=true;
            }
            var animator=visual.GetComponent<Animator>();if(!animator)animator=visual.AddComponent<Animator>();
            animator.runtimeAnimatorController=Controller(folder,id,idle,walk,talk);
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var performance=visual.AddComponent<PeriodCharacterPerformance>();performance.animator=animator;performance.speaker=title;
            var motion=AssetDatabase.LoadAssetAtPath<TextAsset>(folder+"Motion.json");
            var calibration=motion?JsonUtility.FromJson<MotionCalibration>(motion.text):null;
            if(calibration!=null&&!calibration.calibrationRequired&&calibration.walkMetresPerSecond>.05f&&!float.IsInfinity(calibration.walkMetresPerSecond))performance.walkMetresPerSecond=calibration.walkMetresPerSecond;
            else Debug.LogWarning(id+" Walk has no measured speed calibration; retaining source 1x playback pending gait review.");
            idle.SampleAnimation(visual,0);
            var bounds=PoseBounds(root.transform,visual);
            if(Mathf.Abs(bounds.size.y-height)>.12f)
                throw new Exception(id+" source height must be "+height.ToString("F2")+"m; imported idle mesh is "+bounds.size.y.ToString("F3")+"m. Correct the source export rather than stretching the actor.");
            visual.transform.localPosition-=Vector3.up*bounds.min.y;
            var collider=root.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,height*.5f,0);collider.size=new Vector3(width,height,Mathf.Max(.36f,width*.72f));
            Debug.Log("PERIOD_CHARACTER_IMPORT_PASS: "+id+" height="+bounds.size.y.ToString("F3")+" collider="+collider.size+" independent material/controller, Generic idle/walk/talk, local front -Z");
            return root;
        } catch {UnityEngine.Object.DestroyImmediate(root);throw;}
    }
    static void ImportModel(string model) {
        var importer=AssetImporter.GetAtPath(model) as ModelImporter;
        if(!importer)throw new Exception("Missing prepared period-character FBX: "+model);
        if(!importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||importer.optimizeGameObjects||
            importer.animationType!=ModelImporterAnimationType.Generic||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=true;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
            importer.optimizeGameObjects=false;importer.animationType=ModelImporterAnimationType.Generic;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        var definitions=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
        bool changed=false;
        foreach(var clip in definitions) {
            if(!clip.loopTime||clip.loopPose||!clip.lockRootPositionXZ||!clip.lockRootRotation||!clip.lockRootHeightY) {
                clip.loopTime=true;clip.loopPose=false;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;clip.lockRootHeightY=true;changed=true;
            }
        }
        if(changed){importer.clipAnimations=definitions;importer.SaveAndReimport();}
    }
    static AnimationClip RequireClip(AnimationClip[] clips,string token,string id) {
        var found=clips.Where(c=>c.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
        if(found.Length!=1||found[0].empty||found[0].length<=0)throw new Exception(id+" requires one non-empty "+token+" clip; imported clips: "+string.Join(", ",clips.Select(c=>c.name)));
        return found[0];
    }
    static bool HasRequiredClips(AnimationClip[] clips) {
        var selected=new System.Collections.Generic.HashSet<AnimationClip>();
        foreach(string token in new[]{"Idle","Walk","Talk"}) {
            var found=clips.Where(c=>c.name.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            if(found.Length!=1||found[0].empty||found[0].length<=0||!selected.Add(found[0]))return false;
        }
        return true;
    }
    static Transform RequireBone(Transform[] bones,string name,string id) {
        var found=bones.Where(t=>t.name==name).ToArray();
        if(found.Length!=1)throw new Exception(id+" requires one bone named "+name+"; found "+found.Length);
        return found[0];
    }
    static Bounds PoseBounds(Transform root,GameObject visual) {
        bool any=false;var bounds=new Bounds();
        foreach(var renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
            // Bake with the renderer's own scale: Tripo rigs carry a scaled armature, and the
            // matrix below already applies that scale once.
            var mesh=new Mesh();renderer.BakeMesh(mesh,true);
            var matrix=root.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
            foreach(var vertex in mesh.vertices) {
                var point=matrix.MultiplyPoint3x4(vertex);
                if(any)bounds.Encapsulate(point);else {bounds=new Bounds(point,Vector3.zero);any=true;}
            }
            UnityEngine.Object.DestroyImmediate(mesh);
        }
        if(!any)throw new Exception(visual.name+" has no skinned mesh vertices.");
        return bounds;
    }
    static void SaveAvatar(GameObject visual,Transform[] bones,string folder,string id) {
        var human=Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>().Where(b=>b!=HumanBodyBones.LastBone)
            .Select(b=>new{id=b,name=(b.ToString().StartsWith("Left")?"Left_"+b.ToString().Substring(4):b.ToString().StartsWith("Right")?"Right_"+b.ToString().Substring(5):b.ToString()).Replace("Little","Pinky")})
            .Where(b=>bones.Any(t=>t.name==b.name)).Select(b=>new HumanBone{boneName=b.name,humanName=HumanTrait.BoneName[(int)b.id],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
        var description=new HumanDescription{human=human,skeleton=bones.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),
            armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,hasTranslationDoF=false};
        var avatar=AvatarBuilder.BuildHumanAvatar(visual,description);avatar.name=id+" Humanoid";
        if(!avatar.isHuman||!avatar.isValid){UnityEngine.Object.DestroyImmediate(avatar);throw new Exception(id+" must produce a valid reusable Humanoid Avatar.");}
        string path=folder+id+"Humanoid.asset";var saved=AssetDatabase.LoadAssetAtPath<Avatar>(path);
        if(saved){EditorUtility.CopySerialized(avatar,saved);UnityEngine.Object.DestroyImmediate(avatar);EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(avatar,path);
    }
    static AnimatorController Controller(string folder,string id,AnimationClip idle,AnimationClip walk,AnimationClip talk) {
        var path=folder+id+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(path);
        if(!controller.parameters.Any(p=>p.name=="Speaking"))controller.AddParameter("Speaking",AnimatorControllerParameterType.Bool);
        if(!controller.parameters.Any(p=>p.name=="Walking"))controller.AddParameter("Walking",AnimatorControllerParameterType.Bool);
        if(!controller.parameters.Any(p=>p.name=="WalkRate"))controller.AddParameter(new AnimatorControllerParameter{name="WalkRate",type=AnimatorControllerParameterType.Float,defaultFloat=1});
        // An interrupted earlier build can leave a controller asset without its base layer.
        if(controller.layers.Length==0)controller.AddLayer("Base Layer");
        var machine=controller.layers[0].stateMachine;
        AnimatorState State(string name,AnimationClip clip) {
            var state=machine.states.Select(s=>s.state).FirstOrDefault(s=>s.name==name)??machine.AddState(name);state.motion=clip;return state;
        }
        var a=State("Idle",idle);var b=State("Walk",walk);var c=State("Talk",talk);machine.defaultState=a;
        b.speedParameter="WalkRate";b.speedParameterActive=true;
        void Transition(AnimatorState from,AnimatorState to,bool walking,bool? speaking=null) {
            var transition=from.transitions.FirstOrDefault(t=>t.destinationState==to)??from.AddTransition(to);
            transition.hasExitTime=false;transition.hasFixedDuration=true;transition.duration=.25f;
            // Closing a conversation starts Talk->Idle one frame before the actor steps off; let the
            // destination's own transition take over so Walk begins at once instead of after two blends.
            transition.interruptionSource=TransitionInterruptionSource.Destination;transition.orderedInterruption=true;
            foreach(var condition in transition.conditions)transition.RemoveCondition(condition);
            transition.AddCondition(walking?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Walking");
            if(speaking.HasValue)transition.AddCondition(speaking.Value?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"Speaking");
        }
        Transition(a,b,true);Transition(c,b,true);
        Transition(a,c,false,true);Transition(b,c,false,true);
        Transition(c,a,false,false);Transition(b,a,false,false);
        EditorUtility.SetDirty(controller);return controller;
    }
    static Texture2D Texture(string folder,string filename,TextureImporterType type,bool srgb) {
        var path=folder+filename;var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new Exception("Missing prepared period-character texture: "+path);
        if(importer.textureType!=type||importer.sRGBTexture!=srgb||importer.maxTextureSize!=2048||!importer.mipmapEnabled) {
            importer.textureType=type;importer.sRGBTexture=srgb;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material CharacterMaterial(string folder,bool mouth) {
        var path=folder+(mouth?"MouthInterior.mat":"Body.mat");var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new Exception("URP Lit shader is required.");material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        if(mouth){material.SetColor("_BaseColor",new Color(.065f,.008f,.012f));material.SetFloat("_Smoothness",.1f);}
        else {
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",Texture(folder,"BaseColor.png",TextureImporterType.Default,true));
            material.SetTexture("_BumpMap",Texture(folder,"Normal.png",TextureImporterType.NormalMap,false));material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");
            material.SetTexture("_MetallicGlossMap",Texture(folder,"MetallicSmoothness.png",TextureImporterType.Default,false));material.SetFloat("_Metallic",1);material.SetFloat("_Smoothness",1);material.EnableKeyword("_METALLICSPECGLOSSMAP");
        }
        material.SetFloat("_Cull",(float)CullMode.Off);EditorUtility.SetDirty(material);return material;
    }
}
