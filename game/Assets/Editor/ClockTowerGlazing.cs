using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using T=ClockTowerInteriorLayout;

// One physical opening and one set of three-dimensional clock parts, seen from
// either side. The glass samples the current camera's scene, never a backdrop.
public static class ClockTowerGlazing {
    const string Art="Assets/Art/ClockTowerInterior/Glazing";
    sealed class Shape {
        public List<Vector3> v=new List<Vector3>();public List<int> t=new List<int>();
        public void Tri(Vector3 a,Vector3 b,Vector3 c) {
            if(Vector3.Cross(b-a,c-a).sqrMagnitude<1e-14f)return;
            int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(a,b,c);Tri(a,c,d);}
        public void Bar(Vector3 a,Vector3 b,float width,float depth) {
            Vector3 y=(b-a).normalized,x=new Vector3(y.y,-y.x,0)*width/2,z=Vector3.forward*depth/2;
            Vector3[] p={a-x-z,a+x-z,b+x-z,b-x-z,a-x+z,a+x+z,b+x+z,b-x+z};
            Quad(p[0],p[3],p[2],p[1]);Quad(p[4],p[5],p[6],p[7]);
            Quad(p[0],p[1],p[5],p[4]);Quad(p[3],p[7],p[6],p[2]);Quad(p[0],p[4],p[7],p[3]);Quad(p[1],p[2],p[6],p[5]);
        }
        public void Ring(float r,float width,float z,float depth) {
            for(int i=0;i<128;i++) {
                float a=i*Mathf.PI/64,b=(i+1)*Mathf.PI/64;
                Vector3 P(float angle,float radius,float dz)=>new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,z+dz);
                Quad(P(a,r,depth/2),P(a,r+width,depth/2),P(b,r+width,depth/2),P(b,r,depth/2));
                Quad(P(a,r,-depth/2),P(b,r,-depth/2),P(b,r+width,-depth/2),P(a,r+width,-depth/2));
                Quad(P(a,r,-depth/2),P(a,r,depth/2),P(b,r,depth/2),P(b,r,-depth/2));
                Quad(P(a,r+width,-depth/2),P(b,r+width,-depth/2),P(b,r+width,depth/2),P(a,r+width,depth/2));
            }
        }
    }
    static GameObject MeshObject(string name,Shape shape,Material material,Transform parent,bool collision=false) {
        System.IO.Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        var mesh=new Mesh{name=name};mesh.SetVertices(shape.v);mesh.SetTriangles(shape.t,0);
        var uv=new List<Vector2>();foreach(var v in shape.v)uv.Add(new Vector2(v.x/1.52f,(v.y+T.DialHeight)/.72f));
        mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=Art+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(mesh);}
        else AssetDatabase.CreateAsset(mesh,path);
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
        if(collision)go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;return go;
    }
    static List<float> Angles(params float[] halves) {
        var result=new List<float>();for(int i=0;i<128;i++)result.Add(i*Mathf.PI/64);
        foreach(float h in halves)foreach(float x in new[]{-h,h})foreach(float y in new[]{T.MachineFloor-T.DialHeight,T.InnerWallHeight-T.DialHeight})
            result.Add(Mathf.Repeat(Mathf.Atan2(y,x),2*Mathf.PI));
        result.Sort();return result;
    }
    static Vector3 Circle(float angle,float depth)=>new Vector3(Mathf.Cos(angle)*T.DialDiameter/2,Mathf.Sin(angle)*T.DialDiameter/2,-depth);
    static Vector3 Edge(float angle,float half,float depth) {
        float x=Mathf.Cos(angle),y=Mathf.Sin(angle);
        float radius=Mathf.Min(half/Mathf.Max(.000001f,Mathf.Abs(x)),
            (y>=0?T.InnerWallHeight-T.DialHeight:T.DialHeight-T.MachineFloor)/Mathf.Max(.000001f,Mathf.Abs(y)));
        return new Vector3(x*radius,y*radius,-depth);
    }
    public static void BuildShell(Transform root,Material material) {
        var s=new Shape();float inner=T.ChamberInteriorWidth/2,outer=T.ChamberExteriorWidth/2;
        var angles=Angles(inner,outer);
        for(int i=0;i<angles.Count;i++) {
            float a=angles[i],b=angles[(i+1)%angles.Count];
            var ai=Circle(a,inner);var bi=Circle(b,inner);var ao=Circle(a,outer);var bo=Circle(b,outer);
            s.Quad(ao,bo,Edge(b,outer,outer),Edge(a,outer,outer));
            s.Quad(ai,Edge(a,inner,inner),Edge(b,inner,inner),bi);
            s.Quad(ai,bi,bo,ao);
            s.Quad(Edge(a,inner,inner),Edge(a,outer,outer),Edge(b,outer,outer),Edge(b,inner,inner));
        }
        for(int side=0;side<4;side++) {
            var go=MeshObject("Clock chamber apertured physical wall",s,material,root,true);
            go.transform.localPosition=Vector3.up*T.DialHeight;go.transform.localRotation=Quaternion.Euler(0,side*90,0);
        }
    }
    public static void BuildLinings(Transform root,Material material) {
        float half=T.ChamberInteriorWidth/2-.006f;var s=new Shape();var angles=Angles(half);
        for(int i=0;i<angles.Count;i++) {float a=angles[i],b=angles[(i+1)%angles.Count];s.Quad(Circle(a,half),Edge(a,half,half),Edge(b,half,half),Circle(b,half));}
        for(int side=0;side<4;side++) {
            var go=MeshObject("Clock chamber apertured limestone lining",s,material,root);
            go.transform.localPosition=Vector3.up*T.DialHeight;go.transform.localRotation=Quaternion.Euler(0,side*90,0);
        }
    }
    public static void BuildDial(Transform face,float diameter,Material iron) {
        System.IO.Directory.CreateDirectory(Art);AssetDatabase.Refresh();
        string path=Art+"/Frosted opal clock glass.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("WrongDate/FrostedClockGlass"));AssetDatabase.CreateAsset(material,path);}
        material.shader=Shader.Find("WrongDate/FrostedClockGlass");material.SetColor("_MilkColor",new Color(.94f,.93f,.87f,1));
        material.SetFloat("_Milkiness",.64f);material.SetFloat("_OutsideMilkiness",.9f);material.SetFloat("_BlurPixels",6);EditorUtility.SetDirty(material);
        var disc=new Shape();float radius=diameter/2;
        for(int i=0;i<128;i++){float a=i*Mathf.PI/64,b=(i+1)*Mathf.PI/64;disc.Tri(Vector3.zero,new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0));}
        var glass=MeshObject("Live frosted clock glazing",disc,material,face);
        glass.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        var safety=glass.AddComponent<BoxCollider>();float reveal=T.DialPlaneDistance-T.ChamberInteriorWidth/2;
        safety.center=Vector3.forward*reveal/2;safety.size=new Vector3(diameter,diameter,reveal+.035f);
        var parts=new Shape();parts.Ring(radius-.055f,.045f,0,.055f);parts.Ring(radius*.64f,.024f,.027f,.045f);
        string[] numerals={"XII","I","II","III","IV","V","VI","VII","VIII","IX","X","XI"};
        for(int n=0;n<12;n++) {
            var rotation=Quaternion.Euler(0,0,-n*30);
            Vector3 centre=rotation*new Vector3(0,radius*.79f,0);
            string label=numerals[n];float advance=diameter*.046f,height=diameter*.095f;
            for(int j=0;j<label.Length;j++) {
                float x=(j-(label.Length-1)*.5f)*advance;
                Vector3 P(float u,float v)=>centre+rotation*new Vector3(x+u*advance*.72f,v*height,0);
                void Stroke(float ax,float ay,float bx,float by)=>parts.Bar(P(ax,ay),P(bx,by),diameter*.009f,.065f);
                if(label[j]=='I')Stroke(0,-.5f,0,.5f);
                else if(label[j]=='V'){Stroke(-.5f,.5f,0,-.5f);Stroke(0,-.5f,.5f,.5f);}
                else {Stroke(-.5f,-.5f,.5f,.5f);Stroke(-.5f,.5f,.5f,-.5f);}
            }
        }
        for(int i=0;i<60;i++) {
            var q=Quaternion.Euler(0,0,-i*6);
            parts.Bar(q*new Vector3(0,radius*.92f,0),q*new Vector3(0,radius*(i%5==0?.956f:.941f),0),diameter*(i%5==0?.007f:.0035f),.045f);
        }
        for(int i=0;i<4;i++) {
            var q=Quaternion.Euler(0,0,45+i*90);
            parts.Bar(q*new Vector3(0,.10f,.065f),q*new Vector3(0,radius-.065f,.065f),.021f,.035f);
        }
        MeshObject("Solid Roman numerals rings and radial braces",parts,iron,face);
        var window=face.gameObject.AddComponent<ClockTowerDialWindow>();window.glazing=glass.GetComponent<MeshRenderer>();window.safety=safety;
    }
}
