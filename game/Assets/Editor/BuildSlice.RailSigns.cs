using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public static partial class BuildSlice {
    // Bind to the actual imported face, including its connected triangles and bevels.
    static (MeshCollider collider,Vector3 point,Quaternion rotation,Vector2 size) RailFace(Transform root,Vector3 target,Quaternion rotation,Vector2? region=null) {
        var inward=root.TransformDirection(rotation*Vector3.forward);
        var ray=new Ray(root.TransformPoint(target)-inward*.4f,inward);
        MeshCollider face=null;RaycastHit nearest=default;float distance=float.MaxValue;
        Physics.SyncTransforms();
        foreach(var collider in root.GetComponentsInChildren<MeshCollider>())
            if(collider.Raycast(ray,out var hit,.8f)&&hit.distance<distance){face=collider;nearest=hit;distance=hit.distance;}
        if(!face||Vector3.Dot(nearest.normal,-inward)<.999f)throw new Exception("Rail sign has no flat mounting face at "+target);
        var frame=Quaternion.LookRotation(-nearest.normal,root.TransformDirection(rotation*Vector3.up));
        var inverse=Quaternion.Inverse(frame);
        var vertices=face.sharedMesh.vertices.Select(v=>inverse*(face.transform.TransformPoint(v)-nearest.point)).ToArray();
        var triangles=face.sharedMesh.triangles;
        var adjacent=new Dictionary<Vector3Int,List<int>>();
        Vector3Int Key(Vector3 v)=>new Vector3Int(Mathf.RoundToInt(v.x*10000),Mathf.RoundToInt(v.y*10000),Mathf.RoundToInt(v.z*10000));
        for(int t=0;t<triangles.Length/3;t++) {
            if(Enumerable.Range(0,3).Any(i=>Mathf.Abs(vertices[triangles[t*3+i]].z)>.0003f))continue;
            for(int i=0;i<3;i++) {
                var key=Key(vertices[triangles[t*3+i]]);
                if(!adjacent.TryGetValue(key,out var list))adjacent[key]=list=new List<int>();
                list.Add(t);
            }
        }
        var pending=new Stack<int>();pending.Push(nearest.triangleIndex);var visited=new HashSet<int>();
        var minimum=Vector2.positiveInfinity;var maximum=Vector2.negativeInfinity;
        while(pending.Count>0) {
            int t=pending.Pop();if(!visited.Add(t))continue;
            for(int i=0;i<3;i++) {
                var point=vertices[triangles[t*3+i]];minimum=Vector2.Min(minimum,point);maximum=Vector2.Max(maximum,point);
                if(adjacent.TryGetValue(Key(point),out var neighbours))foreach(var next in neighbours)if(!visited.Contains(next))pending.Push(next);
            }
        }
        Vector2 centre=(minimum+maximum)/2,size=maximum-minimum;
        if(region.HasValue) {
            // A number or route uses a reserved patch of a larger painted wall/fascia.
            size=region.Value;centre=Vector2.zero;
            if(minimum.x>-size.x/2||maximum.x<size.x/2||minimum.y>-size.y/2||maximum.y<size.y/2)
                throw new Exception("Rail sign patch exceeds its actual mounting face at "+target);
        }
        return (face,nearest.point+frame*new Vector3(centre.x,centre.y,0),frame,size);
    }
    static RailWorldSign RailLettering(Transform parent,Collider face,Vector3 worldPoint,Quaternion worldRotation,Vector2 faceSize,string words,Color? tint=null,float inset=.055f) {
        var mount=Group("Mounted rail sign · "+words,Vector3.zero,parent);
        var inherited=parent.lossyScale;
        mount.transform.localScale=new Vector3(1/inherited.x,1/inherited.y,1/inherited.z);
        mount.transform.SetPositionAndRotation(worldPoint,worldRotation);
        var text=Group("Face lettering",Vector3.zero,mount.transform).AddComponent<TextMesh>();
        text.text=words;text.font=font;text.fontSize=64;text.characterSize=1;text.anchor=TextAnchor.MiddleCenter;
        text.color=tint??C("#E7D4AE");
        const string path=RailFolder+"WorldText.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("WrongDate/RailWorldText"));AssetDatabase.CreateAsset(material,path);}
        font.RequestCharactersInTexture(words,64);material.mainTexture=font.material.mainTexture;
        var renderer=text.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        var sign=mount.AddComponent<RailWorldSign>();sign.font=font;sign.material=material;sign.lettering=text;sign.surface=face;sign.faceSize=faceSize;sign.inset=inset;
        sign.Fit();return sign;
    }
    static void RailSurfaceText(Transform root,string words,Vector3 target,Quaternion rotation,Color? tint=null,Vector2? region=null,float inset=.055f) {
        var face=RailFace(root,target,rotation,region);
        RailLettering(face.collider.transform,face.collider,face.point,face.rotation,face.size,words,tint,inset);
    }
    static void RailBoard(Transform root,string words,Vector3 front,Vector2 size,Quaternion rotation,Color? tint=null,float inset=.055f) {
        var board=Group("Rail sign board · "+words,front,root);board.transform.localRotation=rotation;
        var backing=Box("Solid painted sign face",Vector3.forward*.03f,new Vector3(size.x,size.y,.06f),RailMaterial("RailGreen"),board.transform);
        const float rim=.025f;
        foreach(int side in new[]{-1,1}) {
            Box("Brass side border",new Vector3(side*(size.x-rim)/2,0,0),new Vector3(rim,size.y,.012f),RailMaterial("RailBrass"),board.transform,false);
            Box("Brass top bottom border",new Vector3(0,side*(size.y-rim)/2,0),new Vector3(size.x-2*rim,rim,.012f),RailMaterial("RailBrass"),board.transform,false);
        }
        RailLettering(board.transform,backing.GetComponent<Collider>(),board.transform.position,board.transform.rotation,size,words,tint,inset);
    }
    static void RailWallBoard(Transform root,string words,Vector3 target,Vector2 size,Quaternion rotation) {
        var inward=root.TransformDirection(rotation*Vector3.forward);
        var ray=new Ray(root.TransformPoint(target)-inward*.4f,inward);
        Collider face=null;RaycastHit nearest=default;float distance=float.MaxValue;
        Physics.SyncTransforms();
        foreach(var collider in root.GetComponentsInChildren<Collider>())
            if(!collider.isTrigger&&collider.Raycast(ray,out var hit,.8f)&&hit.distance<distance){face=collider;nearest=hit;distance=hit.distance;}
        if(!face||Vector3.Dot(nearest.normal,-inward)<.999f)throw new Exception("Wall sign has no mounting face at "+target);
        foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1}) {
            var corner=nearest.point+root.rotation*rotation*new Vector3(x*size.x/2,y*size.y/2,0);
            if(!face.Raycast(new Ray(corner-inward*.08f,inward),out var support,.16f)||Mathf.Abs(support.distance-.08f)>.001f)
                throw new Exception("Wall sign exceeds its masonry at "+target);
        }
        var front=root.InverseTransformPoint(nearest.point-inward*.06f);
        RailBoard(root,words,front,size,rotation);
    }
    static void RailValidateSigns(Transform root) {
        Physics.SyncTransforms();
        foreach(var sign in root.GetComponentsInChildren<RailWorldSign>())
            if(!sign.Validate(out var problem))throw new Exception("Rail sign "+sign.lettering.text+": "+problem);
        Debug.Log("RAIL_SIGN_FACES_PASS: "+root.GetComponentsInChildren<RailWorldSign>().Length+" fitted, centred and physically attached signs");
    }
}
