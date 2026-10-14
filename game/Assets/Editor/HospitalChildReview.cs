using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Stills of the child in her bed, straight from the saved hospital prefab, for checking the pose
// without a player build (Editor batch mode). The idle is sampled at a few moments and
// HospitalBedPose is applied on top, as it is every frame in the game. Skinned meshes are baked
// before each render because batch mode does not refresh skinning between renders.
//   Unity -batchmode -quit -projectPath game -executeMethod HospitalChildReview.Render
// Writes builds/hospital/child-pose/<view>-<time>.png. With -childPreview <id>, that revision is put
// in the bed first (for review before the user approves it; the saved prefab is not changed).
public static class HospitalChildReview {
    public static void Render() {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/Hospital.prefab"));
        var args=System.Environment.GetCommandLineArgs();int at=System.Array.IndexOf(args,"-childPreview");string preview=at>=0&&at+1<args.Length?args[at+1]:null;
        if(preview!=null){PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);HospitalCastAuthoring.PreviewChild=preview;try{HospitalCastAuthoring.AddToHospital(root);}finally{HospitalCastAuthoring.PreviewChild=null;}}
        var child=root.GetComponentsInChildren<HospitalActor>(true).Single(a=>a.role=="child");
        var pose=child.GetComponent<HospitalBedPose>();var animator=child.GetComponentInChildren<Animator>();
        var idle=animator.runtimeAnimatorController.animationClips.First(c=>c.name.ToLowerInvariant().Contains("idle"));
        var key=new GameObject("Key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.1f;key.transform.rotation=Quaternion.Euler(35,200,0);
        var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.45f;fill.transform.rotation=Quaternion.Euler(20,60,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.42f,.43f,.46f);
        var cam=new GameObject("Camera").AddComponent<Camera>();cam.nearClipPlane=.03f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.3f,.32f,.34f);
        var rt=new RenderTexture(960,720,24);cam.targetTexture=rt;
        var dir=Path.GetFullPath("../builds/hospital/child-pose"+(preview!=null?"-"+preview:""));Directory.CreateDirectory(dir);
        var t=child.transform;var facing=-t.forward;facing.y=0;facing.Normalize();var left=-Vector3.Cross(Vector3.up,facing);
        var layout=root.GetComponent<HospitalBuilding>().Layout;var chair=layout.furniture.First(f=>f.id=="cw-chair");
        var chairEye=root.transform.TransformPoint(new Vector3(chair.x,layout.levels.ground+1.15f,chair.z));
        var head=child.GetComponentsInChildren<Transform>(true).First(b=>b.name=="Head");
        foreach(float time in new[]{0f,.37f,.71f}) {
            idle.SampleAnimation(animator.gameObject,time*idle.length);pose.lookTarget=null;pose.Apply();
            var face=head.position+Vector3.up*.05f;
            var views=new (string name,Vector3 from,Vector3 at,float fov)[] {
                ("side",t.position+left*1.7f+Vector3.up*.55f+facing*.1f,t.position+Vector3.up*.3f,40),
                ("foot",t.position+facing*2.2f+Vector3.up*.8f,t.position+Vector3.up*.3f,40),
                ("father",chairEye,face,34),
            };
            foreach(var v in views) {
                var skins=child.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=skins.Select(s=>{var m=new Mesh();s.BakeMesh(m,true);var g=new GameObject("Baked");g.transform.SetPositionAndRotation(s.transform.position,s.transform.rotation);g.transform.localScale=s.transform.lossyScale;g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;return g;}).ToList();
                cam.fieldOfView=v.fov;cam.transform.position=v.from;cam.transform.LookAt(v.at);cam.Render();
                foreach(var g in baked){Object.DestroyImmediate(g.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(g);}foreach(var s in skins)s.enabled=true;
                RenderTexture.active=rt;var tex=new Texture2D(960,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,960,720),0,0);tex.Apply();
                File.WriteAllBytes(Path.Combine(dir,v.name+"-"+Mathf.RoundToInt(time*100)+".png"),tex.EncodeToPNG());Object.DestroyImmediate(tex);
            }
        }
        RenderTexture.active=null;
        Debug.Log("HOSPITAL_CHILD_POSE_RENDERED: "+dir);
    }
}
