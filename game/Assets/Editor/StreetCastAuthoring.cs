using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The street cast (user, 2026-10-15: a boy, a girl, a woman walking her dog and the dog, and adults
// dressed differently), made in the user's Tripo account and approved on 2026-10-16 from the review
// sheets in output/demo/street-life/cast/. A figure is used only while its files still match the
// approval (assets/source/street-cast/acceptance.json, bound to file hashes like the other casts);
// otherwise StreetLifeAuthoring falls back to the hospital stand-ins or leaves the kind out.
// The terrier has no clips: Tripo's motions are humanoid only, and StreetDog moves it from its bones.
public static class StreetCastAuthoring {
    public const string Folder="Assets/Art/StreetCast/";
    public const string Acceptance="assets/source/street-cast/acceptance.json";
    // Standing height each figure was prepared at (tools/street/cast_prompts.py) and shoulder width.
    public static readonly Dictionary<string,(float height,float width)> Size=new Dictionary<string,(float,float)> {
        {"StreetBoy",(1.30f,.36f)},{"StreetGirl",(1.24f,.36f)},{"DogWalker",(1.63f,.50f)},{"Businessman",(1.78f,.54f)},
        {"YoungWoman",(1.65f,.46f)},{"Labourer",(1.75f,.52f)},{"OldGentleman",(1.70f,.50f)},{"Housewife",(1.60f,.50f)},{"Transient",(1.73f,.52f)}};
    public static bool Ready(string id)=>Size.ContainsKey(id)&&PeriodCharacterAsset.Approved(Acceptance,Folder,id);

    [Serializable] sealed class AcceptanceFile {public int schemaVersion;public Entry[] characters;}
    [Serializable] sealed class Entry {public string role;public bool approved;public string characterSha256,baseColorSha256,normalSha256,metallicSmoothnessSha256;}
    [Serializable] sealed class RigFile {public RigBones bones;public float withersMetres,lengthMetres;}
    [Serializable] sealed class RigBones {
        public string hip,pelvis,waist,spine1,spine2,neck1,neck2,head;
        public string foreLeftUpper,foreLeftLower,foreLeftPaw,foreRightUpper,foreRightLower,foreRightPaw;
        public string hindLeftUpper,hindLeftLower,hindLeftPaw,hindRightUpper,hindRightLower,hindRightPaw;
    }
    const string DogFolder=Folder+"Terrier/";
    // The dog's model, textures and rig file still match the approval.
    public static bool DogReady() {
        try {
            string repository=Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
            string manifest=Path.Combine(repository,Acceptance);if(!File.Exists(manifest))return false;
            var file=JsonUtility.FromJson<AcceptanceFile>(File.ReadAllText(manifest));
            var entry=file?.characters?.SingleOrDefault(c=>c!=null&&c.role=="Terrier");
            if(file==null||file.schemaVersion!=1||entry==null||!entry.approved)return false;
            string[] files={"Dog.fbx","BaseColor.png","Normal.png","MetallicSmoothness.png"};
            string[] hashes={entry.characterSha256,entry.baseColorSha256,entry.normalSha256,entry.metallicSmoothnessSha256};
            for(int i=0;i<files.Length;i++)if(!PeriodCharacterAsset.ApprovedContent(Path.Combine(repository,"game",DogFolder,files[i]),hashes[i]))return false;
            return AssetDatabase.LoadAssetAtPath<GameObject>(DogFolder+"Dog.fbx")&&AssetDatabase.LoadAssetAtPath<TextAsset>(DogFolder+"DogRig.json");
        } catch(Exception error) when(error is IOException||error is UnauthorizedAccessException||error is ArgumentException||error is InvalidOperationException) {
            return false;
        }
    }
    // The terrier at its owner's side: the approved model standing on the root's origin, facing the
    // root's -Z like the period characters, with StreetDog and a small body the player cannot walk through.
    public static StreetDog AddDog(GameObject owner) {
        string model=DogFolder+"Dog.fbx";
        var importer=AssetImporter.GetAtPath(model) as ModelImporter;
        if(!importer)throw new Exception("Missing prepared terrier: "+model);
        if(importer.importAnimation||importer.importCameras||importer.importLights||!importer.isReadable||importer.optimizeGameObjects||
            importer.animationType!=ModelImporterAnimationType.Generic||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
            importer.optimizeGameObjects=false;importer.animationType=ModelImporterAnimationType.Generic;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        var rig=JsonUtility.FromJson<RigFile>(AssetDatabase.LoadAssetAtPath<TextAsset>(DogFolder+"DogRig.json").text);
        var root=new GameObject("Terrier");root.transform.SetParent(owner.transform,false);
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(model));
        visual.transform.SetParent(root.transform,false);visual.name="Terrier visual";
        var bones=visual.GetComponentsInChildren<Transform>(true);
        Transform Bone(string name){var found=bones.Where(t=>t.name==name).ToArray();if(found.Length!=1)throw new Exception("The terrier needs one bone named "+name+"; found "+found.Length);return found[0];}
        var b=rig.bones;
        var direction=root.transform.InverseTransformVector(Bone(b.head).position-Bone(b.pelvis).position);direction.y=0;
        if(direction.sqrMagnitude<.0001f)throw new Exception("Cannot tell the terrier's head end.");
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*visual.transform.localRotation;
        var body=PeriodCharacterAsset.CharacterMaterial(DogFolder,false);
        float lowest=float.MaxValue;
        foreach(var renderer in visual.GetComponentsInChildren<Renderer>(true)) {
            int slots=renderer is SkinnedMeshRenderer skinned&&skinned.sharedMesh?skinned.sharedMesh.subMeshCount:renderer.sharedMaterials.Length;
            renderer.sharedMaterials=Enumerable.Repeat(body,Mathf.Max(1,slots)).ToArray();
            renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;renderer.renderingLayerMask=1;
            if(renderer is SkinnedMeshRenderer skin) {
                skin.updateWhenOffscreen=false;
                var mesh=new Mesh();skin.BakeMesh(mesh,true);var matrix=root.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                foreach(var v in mesh.vertices)lowest=Mathf.Min(lowest,matrix.MultiplyPoint3x4(v).y);
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        if(lowest==float.MaxValue)throw new Exception("The terrier has no skinned mesh.");
        visual.transform.localPosition-=Vector3.up*lowest;
        var dog=root.AddComponent<StreetDog>();dog.visual=visual.transform;
        dog.hip=Bone(b.hip);dog.spine1=Bone(b.spine1);dog.spine2=Bone(b.spine2);dog.neck1=Bone(b.neck1);dog.neck2=Bone(b.neck2);dog.head=Bone(b.head);
        dog.legs=new[]{Bone(b.foreLeftUpper),Bone(b.foreLeftLower),Bone(b.foreLeftPaw),Bone(b.foreRightUpper),Bone(b.foreRightLower),Bone(b.foreRightPaw),
                       Bone(b.hindLeftUpper),Bone(b.hindLeftLower),Bone(b.hindLeftPaw),Bone(b.hindRightUpper),Bone(b.hindRightLower),Bone(b.hindRightPaw)};
        dog.length=rig.lengthMetres;
        // About a trotting terrier's stride for its leg length (0.27 m at the elbow).
        dog.stride=.62f;
        var capsule=root.AddComponent<CapsuleCollider>();capsule.direction=2;capsule.radius=.12f;capsule.height=Mathf.Max(.3f,rig.lengthMetres*.86f);capsule.center=new Vector3(0,.25f,0);
        return dog;
    }
}
