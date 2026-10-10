using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class BuildSlice {
    [Serializable] class TicketMeshData { public float[] vertices,uv;public int[] triangles; }
    [Serializable] class TicketStockData { public TicketMeshData intact,punched; }
    static Mesh TicketMesh(bool punched) {
        var data=JsonUtility.FromJson<TicketStockData>(AssetDatabase.LoadAssetAtPath<TextAsset>(RailFolder+"TicketGeometry.json").text);
        var d=punched?data.punched:data.intact;
        string path="Assets/Generated/Ticket"+(punched?"Punched":"Intact")+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.name=punched?"Actually punched ticket stock":"Intact ticket stock";
        mesh.vertices=Enumerable.Range(0,d.vertices.Length/3).Select(i=>new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2])).ToArray();
        mesh.uv=Enumerable.Range(0,d.uv.Length/2).Select(i=>new Vector2(d.uv[i*2],d.uv[i*2+1])).ToArray();
        mesh.triangles=d.triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }
    static Material TicketPaper(bool punched) {
        string path=RailFolder+(punched?"TicketPunched.mat":"TicketIntact.mat");var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",Color.white);mat.SetTexture("_BaseMap",RailTexture(punched?"E03.png":"E02.png",false,false,true));
        mat.SetFloat("_Smoothness",.12f);mat.SetFloat("_Cull",(float)CullMode.Off);mat.SetFloat("_AlphaClip",1);mat.SetFloat("_Cutoff",.5f);
        mat.EnableKeyword("_ALPHATEST_ON");mat.renderQueue=2450;EditorUtility.SetDirty(mat);return mat;
    }
    static void RailWasteTickets(Transform station) {
        var bin=Group("废票桶 · 已验车票",new Vector3(-11.3f,0,-.8f),station);
        Collider(bin,new Vector3(0,.73f,0),new Vector3(.54f,.25f,.50f));
        var clue=bin.AddComponent<Interactable>();clue.title="废票桶里的已验车票";clue.evidenceId="E03";
        clue.source="青川车站废票桶 · 售票员允许查看的同线路旧票";
        clue.body="同线路的旧车票。\n\n票上留有三个清楚的穿透孔，纸面被检票钳打穿。\n\n售票员说明：检票时会在车票上打孔。";
        clue.illustration=RailTexture("E03.png",false,false,true);
        var mesh=TicketMesh(true);var paper=TicketPaper(true);
        for(int i=0;i<8;i++) {
            var p=Group("Punched discarded ticket "+(i+1),i==0?new Vector3(0,.78f,-.17f):new Vector3((i%3-1)*.09f,.56f+i*.019f,(i%2-.5f)*.12f),bin.transform);
            p.transform.localRotation=i==0?Quaternion.Euler(-50,4,0):Quaternion.Euler(-15+i*3,i*39,3-i);
            p.transform.localScale=Vector3.one*1.4f;p.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=p.AddComponent<MeshRenderer>();renderer.sharedMaterial=paper;renderer.shadowCastingMode=ShadowCastingMode.Off;
            p.AddComponent<MeshCollider>().sharedMesh=mesh;p.AddComponent<RailTicketPiece>().punched=true;
        }
        // Intact stock is never placed in the waste bin.
        TicketMesh(false);TicketPaper(false);RailTicketChecks.Validate(bin);
    }
}
