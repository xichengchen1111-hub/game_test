using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static partial class StreetSurfaces {
    [Serializable] class PeriodPlan {public string sourceSha256;public TownPlanSurface[] surfaces;public PeriodRiser[] risers;public PeriodPoint[] stops,drains;}
    [Serializable] class PeriodPoint {public float x,z,yaw;}
    [Serializable] class PeriodRiser {public float[] a;}
    static Material PeriodMaterial(string name,Color colour,float metal=0,float smooth=.25f) {
        string path="Assets/Generated/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",colour);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Material PeriodTexture(string name,string file,bool cutout=false) {
        var importer=(TextureImporter)AssetImporter.GetAtPath(TextureRoot+file);
        importer.anisoLevel=8;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.maxTextureSize=2048;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.SaveAndReimport();
        var m=PeriodMaterial(name,Color.white,0,.26f);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TextureRoot+file));
        if(cutout){m.EnableKeyword("_ALPHATEST_ON");m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.renderQueue=2450;}
        return m;
    }
    static GameObject PGroup(string name,Transform parent,Vector3 pos) {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.localPosition=pos;return g;
    }
    static GameObject PPrimitive(string name,PrimitiveType type,Transform parent,Vector3 pos,Vector3 scale,Material m) {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;g.isStatic=true;return g;
    }
    static GameObject PMesh(string name,Transform parent,List<Vector3> v,List<int> t,List<Vector2> uv,Material m) {
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        string path=MeshRoot+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(mesh,path);
        var g=PGroup(name,parent,Vector3.zero);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;g.isStatic=true;return g;
    }
    static void Octagon(string name,Transform parent,float size,float depth,Material m,bool faceOnly=false) {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();float r=size*.5f/Mathf.Cos(Mathf.PI/8);
        for(int side=0;side<(faceOnly?1:2);side++)for(int k=0;k<8;k++) {
            float angle=Mathf.PI/8+k*Mathf.PI/4;float x=Mathf.Cos(angle)*r,y=Mathf.Sin(angle)*r;
            v.Add(new Vector3(x,y,side==0?-depth/2:depth/2));uv.Add(new Vector2(x/size+.5f,y/size+.5f));
        }
        for(int k=1;k<7;k++){t.AddRange(new[]{0,k+1,k});if(!faceOnly)t.AddRange(new[]{8,8+k,8+k+1});}
        if(!faceOnly)for(int k=0;k<8;k++){int next=(k+1)%8;t.AddRange(new[]{k,next,k+8,next,next+8,k+8});}
        PMesh(name,parent,v,t,uv,m);
    }
    static void Cable(string name,Transform parent,Vector3 a,Vector3 b,Material iron,float width=.014f) {
        var g=PPrimitive(name,PrimitiveType.Cylinder,parent,(a+b)/2,new Vector3(width,Vector3.Distance(a,b)/2,width),iron);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
    }
    static void SignalHood(Transform parent,Material iron,int index) {
        var v=new List<Vector3>();var uv=new List<Vector2>();var t=new List<int>();
        // Half-cylinder visor above the lens, with an outer and inner surface.
        for(int side=0;side<2;side++)for(int i=0;i<=16;i++)for(int end=0;end<2;end++) {
            float a=Mathf.PI*i/16,r=side==0?.126f:.117f;
            v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,end==0?-.035f:-.19f));uv.Add(new Vector2(i/16f,end));
        }
        for(int s=0;s<2;s++)for(int i=0;i<16;i++){int a=s*34+i*2;if(s==0)t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});else t.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
        PMesh("Period-visor-"+index,parent,v,t,uv,iron);
    }
    static void BuildPeriod(GameObject root,TownPlan plan) {
        var data=JsonUtility.FromJson<PeriodPlan>(Resources.Load<TextAsset>("PeriodStreets").text);
        if(data.sourceSha256!=plan.sourceSha256)throw new Exception("Period street dressing uses an obsolete map");
        var group=PGroup("1930 municipal street details",root.transform,Vector3.zero);
        var stone=Stone("PeriodGutter","RoadSetts",new Color(.79f,.78f,.74f),.7f,.3f);
        var paint=PeriodTexture("PeriodRoadPaint","PeriodPaint.png",true);
        var kerb=AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/Materials/StreetKerb.mat");
        var iron=PeriodMaterial("PeriodCastIron",new Color(.17f,.19f,.175f),.48f,.3f);
        var dark=PeriodMaterial("PeriodDrainRecess",new Color(.07f,.075f,.07f),.3f,.22f);
        var bronze=PeriodMaterial("PeriodBronze",new Color(.40f,.31f,.16f),.62f,.32f);
        foreach(var surface in data.surfaces) {
            var builder=new Builder();var v=surface.vertices;
            for(int i=0;i<v.Length;i+=9)builder.SurfaceTriangle(new Vector3(v[i],v[i+1],v[i+2]),new Vector3(v[i+3],v[i+4],v[i+5]),new Vector3(v[i+6],v[i+7],v[i+8]));
            builder.Save("Period-"+surface.name,surface.material=="paint"?paint:stone,group.transform,false);
        }
        // Both faces are explicit, collision continues to use the original walk/road meshes.
        var riser=new Builder();
        foreach(var r in data.risers){var q=r.a;var a=new Vector3(q[0],.0255f,q[1]);var b=new Vector3(q[2],.0255f,q[3]);var c=b+Vector3.up*.0735f;var d=a+Vector3.up*.0735f;riser.Face(a,b,c,d,2);riser.Face(d,c,b,a,2);}
        riser.Save("Period-kerb-risers",kerb,group.transform,false);
        // Merge all flat gratings into two meshes, keeping the street detail inexpensive.
        var grates=new Builder();var recess=new Builder();
        foreach(var drain in data.drains) {
            var center=new Vector3(drain.x,.032f,drain.z);var rotation=Quaternion.Euler(0,drain.yaw,0);
            void Rect(Builder builder,float x0,float x1,float z0,float z1,float y){Vector3 V(float x,float z)=>center+rotation*new Vector3(x,y,z);builder.Face(V(x0,z0),V(x0,z1),V(x1,z1),V(x1,z0));}
            Rect(recess,-.115f,.115f,-.35f,.35f,0);
            Rect(grates,-.125f,-.102f,-.37f,.37f,.006f);Rect(grates,.102f,.125f,-.37f,.37f,.006f);
            Rect(grates,-.125f,.125f,-.37f,-.34f,.006f);Rect(grates,-.125f,.125f,.34f,.37f,.006f);
            for(int j=0;j<11;j++)Rect(grates,-.102f,.102f,-.32f+j*.063f,-.298f+j*.063f,.007f);
        }
        recess.Save("Period-drain-recesses",dark,group.transform,false);grates.Save("Period-drain-iron-bars",iron,group.transform,false);
        // 24-inch urban octagon, bead edge, enamel panel, mounting straps and bolts.
        var sign=PeriodTexture("PeriodStopEnamel","PeriodStop.png");int signIndex=0;
        var bulbMat=PeriodMaterial("PeriodSignBulb",new Color(.85f,.77f,.57f));bulbMat.EnableKeyword("_EMISSION");bulbMat.SetColor("_EmissionColor",new Color(1,.78f,.48f)*1.5f);
        foreach(var s in data.stops) {
            var mount=PGroup("Period STOP "+signIndex++,group.transform,new Vector3(s.x,.1f,s.z));mount.transform.localRotation=Quaternion.Euler(0,s.yaw,0);
            var pole=PPrimitive("Cast iron sign post",PrimitiveType.Cylinder,mount.transform,new Vector3(0,.84f,.055f),new Vector3(.055f,.84f,.055f),iron);
            var col=mount.AddComponent<BoxCollider>();col.center=new Vector3(0,.85f,.055f);col.size=new Vector3(.07f,1.7f,.07f);
            PPrimitive("Foot socket",PrimitiveType.Cylinder,mount.transform,new Vector3(0,.045f,.055f),new Vector3(.14f,.045f,.14f),iron);
            var panel=PGroup("Embossed 24 inch octagon",mount.transform,new Vector3(0,1.65f,0));
            Octagon("Period-stop-shell",panel.transform,.619f,.022f,iron);
            var front=PGroup("Yellow enamel and red legend",panel.transform,new Vector3(0,0,-.013f));Octagon("Period-stop-lettering",front.transform,.6096f,.002f,sign,true);
            // Small shielded incandescent sign lamp: real surface lighting, pooled with
            // the existing street lanterns, no extra shadows or unlit/emissive sign cheat.
            Cable("Sign lamp bracket",mount.transform,new Vector3(0,1.98f,.055f),new Vector3(0,2.08f,-.29f),iron,.022f);
            var hood=PPrimitive("Sign lamp hood",PrimitiveType.Cylinder,mount.transform,new Vector3(0,2.075f,-.30f),new Vector3(.145f,.045f,.145f),iron);
            var bulb=PPrimitive("Sign lamp glass",PrimitiveType.Sphere,mount.transform,new Vector3(0,2.025f,-.30f),new Vector3(.075f,.027f,.075f),bulbMat);
            var light=PGroup("Shielded sign light",mount.transform,new Vector3(0,2.014f,-.30f)).AddComponent<Light>();
            light.type=LightType.Spot;light.range=1.25f;light.spotAngle=104;light.innerSpotAngle=65;light.intensity=1.55f;light.color=new Color(1,.84f,.63f);light.shadows=LightShadows.None;
            light.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-.39f,.29f));
            DayNightLighting.Bind(light,DayNightLamp.Role.Exterior,1,bulb.GetComponent<Renderer>());
            foreach(float y in new[]{-.22f,.22f}) {
                PPrimitive("Back clamp",PrimitiveType.Cube,panel.transform,new Vector3(0,y,.042f),new Vector3(.23f,.037f,.04f),iron);
                PPrimitive("Enamel rivet",PrimitiveType.Sphere,panel.transform,new Vector3(0,y,-.017f),new Vector3(.016f,.016f,.008f),bronze);
            }
        }
        var signal=PGroup("Period four-way traffic signal",group.transform,new Vector3(30,0,20));
        // Poles sit in the intersecting furniture bands, outside both clear footways.
        foreach(var pos in new[]{new Vector3(-2.55f,0,3.5f),new Vector3(2.55f,0,-3.5f)}) {
            PPrimitive("Signal cable post",PrimitiveType.Cylinder,signal.transform,pos+Vector3.up*3.175f,new Vector3(.095f,3.075f,.095f),iron);
            PPrimitive("Post base",PrimitiveType.Cylinder,signal.transform,pos+Vector3.up*.22f,new Vector3(.20f,.12f,.20f),iron);
            var collision=PGroup("Signal post collision",signal.transform,pos).AddComponent<BoxCollider>();collision.center=new Vector3(0,2.7f,0);collision.size=new Vector3(.11f,5.4f,.11f);
            Cable("Suspension wire",signal.transform,pos+Vector3.up*6.20f,new Vector3(0,5.85f,0),iron);
        }
        Cable("Suspension stem",signal.transform,new Vector3(0,5.86f,0),new Vector3(0,5.64f,0),iron,.028f);
        PPrimitive("Four-way cast housing",PrimitiveType.Cube,signal.transform,new Vector3(0,5.09f,0),new Vector3(.39f,1.06f,.39f),iron);
        PPrimitive("Cornice cap",PrimitiveType.Cube,signal.transform,new Vector3(0,5.655f,0),new Vector3(.49f,.07f,.49f),bronze);
        PPrimitive("Bottom cap",PrimitiveType.Cube,signal.transform,new Vector3(0,4.52f,0),new Vector3(.46f,.07f,.46f),iron);
        var lensMat=PeriodMaterial("PeriodSignalGlass",new Color(.13f,.12f,.07f),.08f,.7f);lensMat.EnableKeyword("_EMISSION");lensMat.SetColor("_EmissionColor",Color.white);
        var lenses=new List<Renderer>();
        for(int face=0;face<4;face++) {
            var mount=PGroup("Signal face "+face,signal.transform,new Vector3(0,5.09f,0));mount.transform.localRotation=Quaternion.Euler(0,face*90,0);
            for(int lamp=0;lamp<3;lamp++) {
                var socket=PGroup("Lens socket "+lamp,mount.transform,new Vector3(0,.335f-lamp*.335f,-.205f));
                var rim=PPrimitive("Cast lens bezel",PrimitiveType.Cylinder,socket.transform,Vector3.zero,new Vector3(.265f,.021f,.265f),bronze);rim.transform.localRotation=Quaternion.Euler(90,0,0);
                var glass=PPrimitive("Ribbed glass lens",PrimitiveType.Sphere,socket.transform,new Vector3(0,0,-.027f),new Vector3(.216f,.216f,.034f),lensMat);glass.isStatic=false;lenses.Add(glass.GetComponent<Renderer>());
                SignalHood(socket.transform,iron,face*3+lamp);
            }
        }
        var control=signal.AddComponent<PeriodTrafficSignal>();control.lenses=lenses.ToArray();
        // No modern reflective triangle, ladder crossing, pedestrian countdown or turn arrow.
        if(data.stops.Length!=16||control.lenses.Length!=12)throw new Exception("Period street fixture count mismatch");
        Debug.Log("PERIOD_STREET_AUTHORING_PASS: 16 yellow/red STOPs, one 4-way signal, "+data.drains.Length+" grates; original road collision/topology retained");
    }
}
