using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Eliminate only hidden caps fully covered by the real machine deck. Keep the
// authored FBX, wall side faces, materials, stair opening and all collision.
public static class ClockTowerSurfaceChecks {
    const float Epsilon=.0002f;
    const string DirectoryPath="Assets/Art/ClockTowerInterior/SurfaceMeshes";
    struct Face {public Vector2 a,b,c; public float area;}
    [Serializable] class Result {public bool passed,collisionUnchanged;public int removedTriangles,overlappingTriangles,remainingTriangles;public List<string> surfaces=new List<string>();}
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static Face Project(Transform root,MeshFilter filter,Vector3[] vertices,int a,int b,int c) {
        Vector3 P(int index)=>root.InverseTransformPoint(filter.transform.TransformPoint(vertices[index]));
        var p=P(a);var q=P(b);var r=P(c);
        if(Mathf.Abs(p.y-ClockTowerInteriorLayout.MachineFloor)>Epsilon||Mathf.Abs(q.y-p.y)>Epsilon||Mathf.Abs(r.y-p.y)>Epsilon)return default;
        // Downward feet/rail contact caps are backface-culled from above and
        // can protrude past a stair edge. Only upward visible caps compete.
        if(Vector3.Cross(q-p,r-p).y<=0)return default;
        var face=new Face{a=new Vector2(p.x,p.z),b=new Vector2(q.x,q.z),c=new Vector2(r.x,r.z)};
        float signed=Cross(face.b-face.a,face.c-face.a);
        if(signed<0){var swap=face.b;face.b=face.c;face.c=swap;}
        face.area=Mathf.Abs(signed)/2;return face;
    }
    static float Intersection(Face subject,Face clip) {
        var polygon=new List<Vector2>{subject.a,subject.b,subject.c};var edges=new[]{clip.a,clip.b,clip.c};
        for(int edge=0;edge<3&&polygon.Count>0;edge++) {
            var a=edges[edge];var b=edges[(edge+1)%3];var next=new List<Vector2>();var previous=polygon[polygon.Count-1];
            float before=Cross(b-a,previous-a);
            foreach(var point in polygon) {
                float after=Cross(b-a,point-a);
                if((before>=0)!=(after>=0))next.Add(Vector2.LerpUnclamped(previous,point,before/(before-after)));
                if(after>=0)next.Add(point);
                previous=point;before=after;
            }
            polygon=next;
        }
        float area=0;for(int i=0;i<polygon.Count;i++)area+=Cross(polygon[i],polygon[(i+1)%polygon.Count]);
        return Mathf.Abs(area)/2;
    }
    static MeshFilter Deck(Transform root)=>root.GetComponentsInChildren<MeshFilter>().Single(f=>f.name=="Machine room oak floor with real stair opening");
    static List<Face> DeckFaces(Transform root,MeshFilter deck) {
        var vertices=deck.sharedMesh.vertices;var indices=deck.sharedMesh.triangles;var faces=new List<Face>();
        for(int i=0;i<indices.Length;i+=3){var face=Project(root,deck,vertices,indices[i],indices[i+1],indices[i+2]);if(face.area>1e-8f)faces.Add(face);}
        if(faces.Count<100)throw new InvalidOperationException("Missing machine deck geometry for cap checks");
        return faces;
    }
    static float Covered(Face face,List<Face> deck)=>face.area>1e-8f?deck.Sum(d=>Intersection(face,d)):0;
    static Result Check(Transform root,bool repair) {
        var result=new Result();var deck=Deck(root);var faces=DeckFaces(root,deck);int filterIndex=0;
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>()) {
            filterIndex++;if(filter==deck||!filter.GetComponent<MeshRenderer>()||!filter.sharedMesh.isReadable)continue;
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var submeshes=new List<int[]>();int removed=0,overlaps=0;
            for(int sub=0;sub<mesh.subMeshCount;sub++) {
                var indices=mesh.GetTriangles(sub);var retained=new List<int>();
                for(int i=0;i<indices.Length;i+=3) {
                    var face=Project(root,filter,vertices,indices[i],indices[i+1],indices[i+2]);float covered=Covered(face,faces);
                    if(covered>1e-6f) {
                        result.overlappingTriangles++;overlaps++;
                        if(repair) {
                            if(covered<face.area*.999f)throw new InvalidOperationException("Partly exposed machine-floor cap requires source repair: "+filter.name);
                            removed++;continue;
                        }
                    }
                    retained.Add(indices[i]);retained.Add(indices[i+1]);retained.Add(indices[i+2]);
                }
                submeshes.Add(retained.ToArray());
            }
            if(overlaps>0)result.surfaces.Add(filter.name+": "+overlaps);
            if(removed==0)continue;
            var clean=UnityEngine.Object.Instantiate(mesh);clean.name=filter.name+" covered caps removed";
            for(int sub=0;sub<submeshes.Count;sub++)clean.SetTriangles(submeshes[sub],sub,false);
            string name=new string(filter.name.Select(c=>char.IsLetterOrDigit(c)||c==' '||c=='-'?c:'_').ToArray());
            string path=DirectoryPath+"/"+filterIndex.ToString("D3")+" "+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved){EditorUtility.CopySerialized(clean,saved);UnityEngine.Object.DestroyImmediate(clean);EditorUtility.SetDirty(saved);}
            else {AssetDatabase.CreateAsset(clean,path);saved=clean;}
            filter.sharedMesh=saved;result.removedTriangles+=removed;
        }
        result.passed=repair||result.overlappingTriangles==0;return result;
    }
    public static void Repair(Transform root) {
        Directory.CreateDirectory(DirectoryPath);AssetDatabase.Refresh();
        string Collisions()=>string.Join(";",root.GetComponentsInChildren<Collider>().Select(c=>c.GetInstanceID()+":"+(c is MeshCollider m?m.sharedMesh.GetInstanceID():0)));
        string before=Collisions();
        var result=Check(root,true);var after=Check(root,false);
        if(!after.passed)throw new InvalidOperationException("Machine-floor coplanar caps remain");
        result.remainingTriangles=after.overlappingTriangles;result.collisionUnchanged=before==Collisions();
        if(!result.collisionUnchanged)throw new InvalidOperationException("Visual cap removal changed collision");
        Directory.CreateDirectory("../output/demo/full-map");
        File.WriteAllText("../output/demo/full-map/tower-surface-separation.json",JsonUtility.ToJson(result,true));
        Debug.Log("CLOCKTOWER_SURFACE_SEPARATION_PASS: removed "+result.removedTriangles+" fully covered visual caps; collider and deck unchanged");
    }
    public static void Audit() {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/ClockTower.prefab");
        var result=Check(prefab.transform,false);
        Directory.CreateDirectory("../builds/review/full-map");
        File.WriteAllText("../builds/review/full-map/tower-cap-before.json",JsonUtility.ToJson(result,true));
        Debug.Log("TOWER_CAP_AUDIT: overlaps="+result.overlappingTriangles);
    }
}
