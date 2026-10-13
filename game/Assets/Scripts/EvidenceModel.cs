using UnityEngine;
// Shared physical paper geometry. Same object is used in furniture and the close-up view.
public static class EvidenceModel {
    public static GameObject Paper(string name,Texture2D texture,float width,float height){
        var root=new GameObject(name);int nx=20,ny=12;var vertices=new Vector3[(nx+1)*(ny+1)];var uv=new Vector2[vertices.Length];var triangles=new int[nx*ny*6];int k=0;
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){int i=y*(nx+1)+x;float u=x/(float)nx,v=y/(float)ny;vertices[i]=new Vector3((u-.5f)*width,(v-.5f)*height,.007f*width*Mathf.Sin(u*Mathf.PI*2)+.012f*width*Mathf.Pow(Mathf.Abs(u-.5f)*2,6));uv[i]=new Vector2(u,v);}
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=y*(nx+1)+x;triangles[k++]=i;triangles[k++]=i+nx+1;triangles[k++]=i+1;triangles[k++]=i+1;triangles[k++]=i+nx+1;triangles[k++]=i+nx+2;}
        void Side(string label,bool back){var go=new GameObject(label);go.transform.SetParent(root.transform,false);var mesh=new Mesh{name=label};var v=(Vector3[])vertices.Clone();if(back)for(int i=0;i<v.Length;i++)v[i].z+=.0014f;var tri=(int[])triangles.Clone();if(back)System.Array.Reverse(tri);mesh.vertices=v;mesh.uv=uv;mesh.triangles=tri;mesh.RecalculateNormals();mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=mesh;var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",back?new Color(.69f,.63f,.49f):Color.white);if(!back&&texture)mat.SetTexture("_BaseMap",texture);else if(!back)mat.SetColor("_BaseColor",new Color(.82f,.77f,.64f));go.AddComponent<MeshRenderer>().sharedMaterial=mat;}
        Side("Paper printed face",false);Side("Paper reverse",true);
        // Real thin edge, visible when rotating the evidence.
        var edge=new GameObject("Paper cut edges");edge.transform.SetParent(root.transform,false);var ev=new System.Collections.Generic.List<Vector3>();var et=new System.Collections.Generic.List<int>();
        void Line(int a,int b){int i=ev.Count;ev.Add(vertices[a]);ev.Add(vertices[b]);ev.Add(vertices[a]+Vector3.forward*.0014f);ev.Add(vertices[b]+Vector3.forward*.0014f);et.AddRange(new[]{i,i+2,i+1,i+1,i+2,i+3,i+1,i+2,i,i+3,i+2,i+1});}
        for(int x=0;x<nx;x++){Line(x,x+1);Line(ny*(nx+1)+x,ny*(nx+1)+x+1);}for(int y=0;y<ny;y++){Line(y*(nx+1),(y+1)*(nx+1));Line(y*(nx+1)+nx,(y+1)*(nx+1)+nx);}
        var em=new Mesh();em.SetVertices(ev);em.SetTriangles(et,0);em.RecalculateNormals();edge.AddComponent<MeshFilter>().sharedMesh=em;var edgeMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));edgeMat.color=new Color(.48f,.42f,.31f);edge.AddComponent<MeshRenderer>().sharedMaterial=edgeMat;return root;
    }
}
