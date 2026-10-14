using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

// Speaking mouths for period characters whose Tripo export has no jaw, mouth cavity or shape
// keys. At build time, from the approved mesh (the FBX on disk is never changed):
//   - the face frame comes from the eye and headfront markers; the lip line is the thin dark crease
//     painted in the base colour, found near the hollow of the front profile and tracked to the corners;
//   - the lower lip and chin are skinned to a new MouthJaw bone hinged near the ear (the mesh is not
//     cut, so no seam or stray triangle can show);
//   - a dark strip lies on the lip line, its upper edge on the head and its lower edge on the jaw:
//     shut it has no height, speaking it opens into the dark of the mouth.
// PeriodCharacterMouthMotion turns the jaw while the character speaks a line.
public static class PeriodCharacterMouth {
    public sealed class Report { public bool rigged; public float seamFromEyes, eyeToChin, halfWidth; public int jawVertices, mouthSubmesh=-1, tearSubmesh=-1; public string why; }
    // Characters that also get blinking lids, a smile and tears (the child; user, 2026-10-14). The
    // detection reads the painted eyes, so a new character is added here only after its renders are checked.
    public static readonly HashSet<string> Expressive=new HashSet<string>{"ChildGown"};
    // The painted eye opening: for columns across the eye, the top (upper lashes) and bottom edge, in the face frame.
    sealed class EyeOpening {
        public float x0,step;public float[] top,bot;public bool fromTexture;
        public float x1=>x0+step*(top.Length-1);
        public float Top(float x)=>At(top,x);public float Bot(float x)=>At(bot,x);
        float At(float[] a,float x){float i=Mathf.Clamp((x-x0)/step,0,a.Length-1);int lo=Mathf.FloorToInt(i),hi=Mathf.Min(lo+1,a.Length-1);return Mathf.Lerp(a[lo],a[hi],i-lo);}
    }

    public static Report Add(GameObject visual,string folder,string id) {
        var report=new Report();
        var renderer=visual.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r=>r.sharedMesh?r.sharedMesh.vertexCount:0).FirstOrDefault();
        if(!renderer||!renderer.sharedMesh){report.why="no skinned mesh";return report;}
        var source=renderer.sharedMesh;var bones=renderer.bones;var bind=source.bindposes;
        int head=Array.FindIndex(bones,b=>b&&b.name=="Head"),neck=Array.FindIndex(bones,b=>b&&b.name=="Neck");
        var all=visual.GetComponentsInChildren<Transform>(true);
        Transform Find(string name)=>all.FirstOrDefault(t=>t.name==name);
        Transform eyeL=Find("Left_Eye"),eyeR=Find("Right_Eye"),front=Find("headfront");
        if(head<0||neck<0||!eyeL||!eyeR||!front){report.why="missing Head/Neck/eye/headfront";return report;}
        var H=bones[head];var toMesh=bind[head].inverse;
        // A point rigid with the head, from the current pose into bind-pose mesh space.
        Vector3 M(Transform t)=>toMesh.MultiplyPoint3x4(H.InverseTransformPoint(t.position));
        Vector3 eL=M(eyeL),eR=M(eyeR),eyes=(eL+eR)/2,headFront=M(front),neckM=bind[neck].inverse.MultiplyPoint3x4(Vector3.zero),headM=toMesh.MultiplyPoint3x4(Vector3.zero);
        float meshIpd=Vector3.Distance(eL,eR),k=Vector3.Distance(eyeL.position,eyeR.position)/Mathf.Max(meshIpd,1e-6f);
        if(meshIpd<1e-6f||k<=0){report.why="eyes coincide";return report;}
        Vector3 R=(eL-eR).normalized;Vector3 F=Vector3.ProjectOnPlane(headFront-headM,R).normalized;Vector3 U=Vector3.Cross(F,R).normalized;
        if(Vector3.Dot(U,headM-neckM)<0)U=-U;
        F=Vector3.Cross(R,U).normalized;if(Vector3.Dot(F,headFront-headM)<0)F=-F;
        // Face frame in metres: x toward the character's left, y up, z out of the face, origin between the eyes.
        Vector3 Face(Vector3 v){var d=v-eyes;return new Vector3(Vector3.Dot(d,R),Vector3.Dot(d,U),Vector3.Dot(d,F))*k;}
        Vector3 FromFace(Vector3 f)=>eyes+(R*f.x+U*f.y+F*f.z)/k;
        float ipd=meshIpd*k;

        var vertices=source.vertices;int n=vertices.Length;
        var perVertex=source.GetBonesPerVertex().ToArray();var weights=source.GetAllBoneWeights().ToArray();
        var start=new int[n];for(int i=1;i<n;i++)start[i]=start[i-1]+perVertex[i-1];
        float HeadWeight(int v){float w=0;for(int j=0;j<perVertex[v];j++)if(weights[start[v]+j].boneIndex==head)w+=weights[start[v]+j].weight;return w;}
        var face=new Vector3[n];var headW=new float[n];
        for(int i=0;i<n;i++){face[i]=Face(vertices[i]);headW[i]=HeadWeight(i);}
        // Chin: the lowest front point of the head on the centre line.
        var centre=Enumerable.Range(0,n).Where(i=>headW[i]>.5f&&Mathf.Abs(face[i].x)<ipd*.25f&&face[i].z>-ipd*.2f).ToArray();
        if(centre.Length<20){report.why="too few face vertices";return report;}
        float chin=centre.Min(i=>face[i].y),E=-chin;report.eyeToChin=E;
        // Lip seam: the deepest hollow of the front profile near 62 % of eyes-to-chin.
        float guess=-.62f*E,step=.0015f;var profile=new List<(float y,float z)>();
        for(float y=guess-.16f*E;y<=guess+.16f*E;y+=step) {
            var near=centre.Where(i=>Mathf.Abs(face[i].y-y)<step*.75f&&Mathf.Abs(face[i].x)<ipd*.12f).ToArray();
            if(near.Length>0)profile.Add((y,near.Max(i=>face[i].z)));
        }
        float seam=guess;
        if(profile.Count>5) {
            float best=float.MaxValue;
            for(int a=1;a<profile.Count-1;a++) {
                float above=profile.Skip(a+1).Take(6).Max(p=>p.z),below=profile.Take(a).Skip(Math.Max(0,a-6)).DefaultIfEmpty((0,float.MinValue)).Max(p=>p.z);
                float depth=Mathf.Min(above,below)-profile[a].z;
                // Prefer a real hollow close to the proportion guess.
                float score=-depth+Mathf.Abs(profile[a].y-guess)*.15f;
                if(depth>.0008f&&score<best){best=score;seam=profile[a].y;}
            }
        }
        // The lip line as painted: a thin dark crease in the base colour, darker than the skin both
        // above and below it (so the edge of a moustache does not count), tracked from the middle of
        // the mouth out to its corners. The hollow found above is only a prior; the texture decides.
        var uv0=new List<Vector2>();source.GetUVs(0,uv0);
        var faceTris=new List<int>();
        bool Near(int v)=>headW[v]>.2f&&Mathf.Abs(face[v].x)<ipd*1.1f&&face[v].y<seam+.04f&&face[v].y>seam-.05f&&face[v].z>-ipd*.5f;
        for(int s=0;s<source.subMeshCount;s++){var tris=source.GetTriangles(s);for(int t=0;t<tris.Length;t+=3)if(Near(tris[t])&&Near(tris[t+1])&&Near(tris[t+2]))faceTris.AddRange(new[]{tris[t],tris[t+1],tris[t+2]});}
        // The front-most skin at (x, y) of the face frame: its depth and texture coordinate.
        bool Surface(float x,float y,out float z,out Vector2 uv) {
            z=float.MinValue;uv=default;
            for(int t=0;t<faceTris.Count;t+=3) {
                Vector3 a=face[faceTris[t]],b=face[faceTris[t+1]],c=face[faceTris[t+2]];
                float d=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(d)<1e-12f)continue;
                float wa=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/d,wb=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/d,wc=1-wa-wb;
                if(wa<-1e-4f||wb<-1e-4f||wc<-1e-4f)continue;
                float zz=wa*a.z+wb*b.z+wc*c.z;if(zz<=z)continue;
                z=zz;uv=wa*uv0[faceTris[t]]+wb*uv0[faceTris[t+1]]+wc*uv0[faceTris[t+2]];
            }
            return z>float.MinValue;
        }
        if(!Surface(0,seam,out float seamZ,out _)){report.why="no lip surface at the seam";return report;}
        var texture=new Texture2D(2,2);bool painted=System.IO.File.Exists(folder+"BaseColor.png")&&texture.LoadImage(System.IO.File.ReadAllBytes(folder+"BaseColor.png"));
        float Lum(float x,float y){if(!painted||!Surface(x,y,out _,out var uv))return float.NaN;var c=texture.GetPixelBilinear(uv.x,uv.y);return .3f*c.r+.59f*c.g+.11f*c.b;}
        const float dy=.0005f,dx=.001f;float y0=seam-.02f;int rows=(int)(.04f/dy)+1;
        float[] Column(float x){var col=new float[rows];for(int r=0;r<rows;r++){float sum=0;int m=0;foreach(float ox in new[]{-dx,0,dx}){float l=Lum(x+ox,y0+r*dy);if(!float.IsNaN(l)){sum+=l;m++;}}col[r]=m>0?sum/m:float.NaN;}return col;}
        float Ridge(float[] col,int r){float best=0;foreach(int o in new[]{3,4,5}){if(r-o<0||r+o>=rows||float.IsNaN(col[r])||float.IsNaN(col[r-o])||float.IsNaN(col[r+o]))continue;best=Mathf.Max(best,Mathf.Min(col[r-o],col[r+o])-col[r]);}return best;}
        var middle=new[]{-.004f,0,.004f}.Select(Column).ToArray();
        int centreRow=-1;float centreRidge=0,bestScore=float.MinValue;
        var centreRidges=Enumerable.Range(0,rows).Select(r=>middle.Sum(c=>Ridge(c,r))/3).ToArray();
        // Strength minus 4 per metre from the profile hollow: a clear crease a centimetre away still wins,
        // but a faint chin fold does not beat the dark line under a moustache.
        for(int r=0;r<rows;r++){float score=centreRidges[r]-Mathf.Abs(y0+r*dy-seam)*4;if(score>bestScore){bestScore=score;centreRidge=centreRidges[r];centreRow=r;}}
        Debug.Log("PERIOD_CHARACTER_MOUTH_CREASES: "+id+" profile hollow "+(seam*100).ToString("F1")+" cm; "+string.Join(", ",Enumerable.Range(1,rows-2).Where(r=>centreRidges[r]>.01f&&centreRidges[r]>=centreRidges[r-1]&&centreRidges[r]>=centreRidges[r+1]).OrderByDescending(r=>centreRidges[r]).Take(4).Select(r=>((y0+r*dy)*100).ToString("F1")+" cm "+centreRidges[r].ToString("F3"))));
        float halfW=ipd*.47f;int span=Mathf.RoundToInt(ipd*.62f/dx);
        var lineAt=new float[2*span+1];for(int i=0;i<lineAt.Length;i++)lineAt[i]=seam;
        if(painted&&centreRidge>.015f) {
            lineAt[span]=y0+centreRow*dy;float corner=0;
            foreach(int sign in new[]{-1,1}) {
                int prev=centreRow,weak=0;float reach=ipd*.62f;
                for(int i=1;i<=span;i++) {
                    var col=Column(sign*i*dx);int best=prev;float strongest=-1;
                    for(int r=Math.Max(0,prev-2);r<=Math.Min(rows-1,prev+2);r++){float score=Ridge(col,r)-Mathf.Abs(r-prev)*.004f;if(score>strongest){strongest=score;best=r;}}
                    prev=best;lineAt[span+sign*i]=y0+best*dy;
                    // The corner: where the crease fades to a third of its strength in the middle.
                    if(Ridge(col,best)<centreRidge*.33f){if(++weak>=2){reach=(i-1)*dx;for(int j=i-1;j<=span;j++)lineAt[span+sign*j]=lineAt[span+sign*(i-2)];break;}}else weak=0;
                }
                corner+=reach/2;
            }
            halfW=Mathf.Clamp(corner,ipd*.36f,ipd*.55f);
            // Smoothing against texture noise: a lip line is a gentle curve.
            for(int pass=0;pass<3;pass++){var smooth=(float[])lineAt.Clone();for(int i=0;i<lineAt.Length;i++){float sum=0;int m=0;for(int j=Math.Max(0,i-4);j<=Math.Min(lineAt.Length-1,i+4);j++){sum+=lineAt[j];m++;}smooth[i]=sum/m;}lineAt=smooth;}
            seam=lineAt[span];
        }
        float Line(float x){float i=Mathf.Clamp(x/dx+span,0,lineAt.Length-1);int a=Mathf.FloorToInt(i),b=Mathf.Min(a+1,lineAt.Length-1);return Mathf.Lerp(lineAt[a],lineAt[b],i-a);}
        report.seamFromEyes=seam;report.halfWidth=halfW;
        Surface(0,seam,out seamZ,out _);

        // Jaw weight: below the lip line the lower lip follows the jaw, fully in the middle and less
        // toward the corners; a little lower the whole chin does; it fades up toward the ear and back
        // toward the throat. Nothing above the line moves, so the upper lip and the face stay put.
        float Taper(float x){float t=x/halfW;return Mathf.Pow(Mathf.Clamp01(1-t*t),.6f);}
        float Jaw(Vector3 f){
            float ax=Mathf.Abs(f.x);
            float depth=Line(f.x)+Mathf.Max(0,ax-halfW)*.55f-f.y;          // below the line; it rises toward the ear
            float below=Mathf.SmoothStep(0,1,Mathf.Clamp01(depth/.0015f));
            float lip=Mathf.Lerp(Taper(f.x),1,Mathf.SmoothStep(0,1,Mathf.Clamp01((depth-.002f)/.012f)));
            float side=1-Mathf.SmoothStep(0,1,Mathf.Clamp01((ax-halfW*1.6f)/(ipd*.55f)));
            float throat=Mathf.SmoothStep(0,1,Mathf.Clamp01((f.z-(seamZ-ipd*1.15f))/(ipd*.45f)));
            float underChin=Mathf.SmoothStep(0,1,Mathf.Clamp01((f.y-(chin-.03f))/.02f));
            return below*lip*side*throat*underChin;
        }

        // Assemble the new mesh: the original vertices with the jaw added, then the mouth opening.
        int jawBone=bones.Length;
        var outVerts=new List<Vector3>(vertices);var normals=new List<Vector3>(source.normals);var tangents=new List<Vector4>(source.tangents);
        var uv=new List<Vector2>(uv0);
        var colors=source.colors;bool hasColors=colors!=null&&colors.Length==n;var outColors=hasColors?new List<Color>(colors):null;
        var outWeights=new List<BoneWeight1>();var finalPer=new List<byte>();
        int jawVertices=0;
        for(int i=0;i<n;i++) {
            float share=Jaw(face[i]);var list=new List<BoneWeight1>();float moved=0;
            for(int j=0;j<perVertex[i];j++) {
                var w=weights[start[i]+j];
                if(w.boneIndex==head&&share>0){moved=w.weight*share;w.weight-=moved;}
                if(w.weight>1e-5f)list.Add(w);
            }
            if(moved>1e-5f){list.Add(new BoneWeight1{boneIndex=jawBone,weight=moved});jawVertices++;}
            list=list.OrderByDescending(w=>w.weight).ToList();float sum=list.Sum(w=>w.weight);
            for(int j=0;j<list.Count;j++){var w=list[j];w.weight/=sum;list[j]=w;}
            outWeights.AddRange(list);finalPer.Add((byte)list.Count);
        }
        report.jawVertices=jawVertices;
        // The opening: a dark strip lying on the lips along the painted line, just proud of the skin.
        // Its upper edge stays with the head and its lower edge goes with the jaw (as much as the lower
        // lip under it), so at rest it has no height and while speaking it opens into a lens.
        var opening=new List<int>();int cols=28,baseIndex=outVerts.Count;var probe=new List<Vector3>();
        for(int c=0;c<=cols;c++) {
            float x=Mathf.Lerp(-halfW,halfW,c/(float)cols),y=Line(x);
            // In front of the skin within 2 mm, so a lip bulging next to the crease never covers it.
            float z=float.MinValue;foreach(float oy in new[]{-.002f,-.001f,0,.001f,.002f})foreach(float ox in new[]{-.001f,0,.001f})if(Surface(x+ox,y+oy,out float zz,out _))z=Mathf.Max(z,zz);
            if(z==float.MinValue)z=seamZ-Mathf.Abs(x)*.4f;
            var at=FromFace(new Vector3(x,y,z+.001f));
            probe.Add(at);probe.Add(FromFace(new Vector3(x,y-.002f,z+.001f)));      // as if open, to choose the winding
            foreach(float share in new[]{0,Taper(x)}) {
                outVerts.Add(at);normals.Add(F);tangents.Add(new Vector4(R.x,R.y,R.z,1));uv.Add(Vector2.zero);if(hasColors)outColors.Add(Color.black);
                var list=new List<BoneWeight1>();if(share>1e-4f)list.Add(new BoneWeight1{boneIndex=jawBone,weight=share});if(share<1-1e-4f)list.Add(new BoneWeight1{boneIndex=head,weight=1-share});
                list=list.OrderByDescending(w=>w.weight).ToList();outWeights.AddRange(list);finalPer.Add((byte)list.Count);
            }
        }
        for(int c=0;c<cols;c++){int a=baseIndex+2*c,b=a+1,d=a+2,e=a+3;opening.AddRange(new[]{a,d,b,b,d,e});}
        // Face the strip out of the face.
        for(int t=0;t<opening.Count;t+=3) {
            Vector3 P(int v)=>probe[v-baseIndex];
            var normal=Vector3.Cross(P(opening[t+1])-P(opening[t]),P(opening[t+2])-P(opening[t]));
            if(Vector3.Dot(normal,F)<0){int s=opening[t+1];opening[t+1]=opening[t+2];opening[t+2]=s;}
        }
        var subs=Enumerable.Range(0,source.subMeshCount).Select(s=>source.GetTriangles(s)).ToArray();

        // ----- The expressive face (Expressive): blinking upper lids, a smile, tears. -----------------
        // Everything is extra geometry or blend shapes on this same mesh: the FBX stays untouched.
        bool expressive=Expressive.Contains(id)&&painted;
        var blinkDelta=new Dictionary<int,Vector3>();var smileDelta=new Dictionary<int,Vector3>();var filmDelta=new Dictionary<int,Vector3>();
        var flowDelta=new List<Dictionary<int,Vector3>>();var tearTris=new List<int>();var lidTris=new List<int>();string faceNote="";
        if(expressive) {
            var eyeTris=new List<int>();
            bool NearEye(int v)=>headW[v]>.2f&&Mathf.Abs(face[v].x)<ipd*1.4f&&face[v].y<.04f&&face[v].y>-.05f&&face[v].z>-ipd*.5f;
            for(int sm=0;sm<source.subMeshCount;sm++){var tl=source.GetTriangles(sm);for(int q=0;q<tl.Length;q+=3)if(NearEye(tl[q])&&NearEye(tl[q+1])&&NearEye(tl[q+2]))eyeTris.AddRange(new[]{tl[q],tl[q+1],tl[q+2]});}
            bool Hit(float x,float y,out float z,out Vector2 at) {
                z=float.MinValue;at=default;
                for(int q=0;q<eyeTris.Count;q+=3) {
                    Vector3 a=face[eyeTris[q]],b=face[eyeTris[q+1]],c=face[eyeTris[q+2]];
                    float d=(b.y-c.y)*(a.x-c.x)+(c.x-b.x)*(a.y-c.y);if(Mathf.Abs(d)<1e-12f)continue;
                    float wa=((b.y-c.y)*(x-c.x)+(c.x-b.x)*(y-c.y))/d,wb=((c.y-a.y)*(x-c.x)+(a.x-c.x)*(y-c.y))/d,wc=1-wa-wb;
                    if(wa<-1e-4f||wb<-1e-4f||wc<-1e-4f)continue;
                    float zz=wa*a.z+wb*b.z+wc*c.z;if(zz<=z)continue;
                    z=zz;at=wa*uv0[eyeTris[q]]+wb*uv0[eyeTris[q+1]]+wc*uv0[eyeTris[q+2]];
                }
                return z>float.MinValue;
            }
            // The front-most skin within r of a point: extra geometry sits just in front of it.
            float Front(float x,float y,float r){float best=float.MinValue;foreach(float ox in new[]{-r,0,r})foreach(float oy in new[]{-r,0,r})if(Hit(x+ox,y+oy,out float zz,out _))best=Mathf.Max(best,zz);return best;}
            Color Colour(float x,float y)=>Hit(x,y,out _,out var at)?texture.GetPixelBilinear(at.x,at.y):Color.clear;
            Vector2 TexAt(float x,float y)=>Hit(x,y,out _,out var at)?at:Vector2.zero;
            float Lu(Color col)=>.3f*col.r+.59f*col.g+.11f*col.b;
            float Ch(Color col)=>Mathf.Max(col.r,Mathf.Max(col.g,col.b))-Mathf.Min(col.r,Mathf.Min(col.g,col.b));
            float Median(List<float> v){if(v.Count==0)return .5f;v.Sort();return v[v.Count/2];}
            // The opening near each eye marker (the marker of these rigs sits about a centimetre high): skin is
            // warm (red well above blue), the white of the eye and the iris are grey or green, the pupil and
            // lashes are dark. The cool or dark region around the coolest point near the marker is the eye.
            EyeOpening FindEye(int side) {
                float cx=side*ipd*.5f;
                var skinL=new List<float>();var skinW=new List<float>();
                for(float sx=-.004f;sx<=.0041f;sx+=.002f)for(float sy=-.032f;sy>=-.0381f;sy-=.002f){var col=Colour(cx+sx,sy);if(col.a>0){skinL.Add(Lu(col));skinW.Add(col.r-col.b);}}
                float ls=Median(skinL),ws=Median(skinW);
                const float g=.0005f;int nx=121,ny=81;float gx0=cx-.03f,gy0=-.03f;
                var marked=new bool[nx,ny];var warmth=new float[nx,ny];
                for(int ix=0;ix<nx;ix++)for(int iy=0;iy<ny;iy++){var col=Colour(gx0+ix*g,gy0+iy*g);warmth[ix,iy]=float.MaxValue;if(col.a<=0)continue;warmth[ix,iy]=col.r-col.b;marked[ix,iy]=col.r-col.b<ws*.45f||Lu(col)<ls*.5f||Lu(col)>ls+.06f;}
                int seedX=-1,seedY=-1;float seedWarm=float.MaxValue;
                for(int ix=0;ix<nx;ix++)for(int iy=0;iy<ny;iy++)if(marked[ix,iy]&&new Vector2(gx0+ix*g-cx,gy0+iy*g+.009f).magnitude<.016f&&warmth[ix,iy]<seedWarm){seedWarm=warmth[ix,iy];seedX=ix;seedY=iy;}
                EyeOpening Prior(string why){faceNote+=" "+(side>0?"left":"right")+" eye from proportions ("+why+");";int m2=41;var e2=new EyeOpening{x0=cx-ipd*.25f,step=ipd*.5f/(m2-1),top=new float[m2],bot=new float[m2]};for(int q=0;q<m2;q++){float u=(q/(float)(m2-1))*2-1;float h2=ipd*.1f*Mathf.Sqrt(Mathf.Max(0,1-u*u));e2.top[q]=h2;e2.bot[q]=-h2;}return e2;}
                if(seedX<0)return Prior("no eye colour near the marker");
                var inEye=new bool[nx,ny];var queue=new Queue<(int,int)>();queue.Enqueue((seedX,seedY));inEye[seedX,seedY]=true;
                while(queue.Count>0){var (qx,qy)=queue.Dequeue();foreach(var (ox,oy) in new[]{(1,0),(-1,0),(0,1),(0,-1)}){int ax=qx+ox,ay=qy+oy;if(ax<0||ay<0||ax>=nx||ay>=ny||inEye[ax,ay]||!marked[ax,ay])continue;inEye[ax,ay]=true;queue.Enqueue((ax,ay));}}
                int left=nx,right=-1;for(int ix=0;ix<nx;ix++)for(int iy=0;iy<ny;iy++)if(inEye[ix,iy]){left=Mathf.Min(left,ix);right=Mathf.Max(right,ix);}
                // What the search saw, for checking: the sampled colours, the eye region tinted red, other marks blue.
                {var dbg=new Texture2D(nx,ny,TextureFormat.RGB24,false);for(int ix=0;ix<nx;ix++)for(int iy=0;iy<ny;iy++){var col=Colour(gx0+ix*g,gy0+iy*g);if(inEye[ix,iy])col=Color.Lerp(col,Color.red,.45f);else if(marked[ix,iy])col=Color.Lerp(col,Color.blue,.45f);if(ix==Mathf.RoundToInt((cx-gx0)/g)&&iy==Mathf.RoundToInt(-gy0/g))col=Color.green;dbg.SetPixel(ix,iy,col);}
                 var dbgDir=System.IO.Path.GetFullPath("../builds/cast-review/face-debug");System.IO.Directory.CreateDirectory(dbgDir);System.IO.File.WriteAllBytes(System.IO.Path.Combine(dbgDir,id+"-"+(side>0?"left":"right")+".png"),dbg.EncodeToPNG());UnityEngine.Object.DestroyImmediate(dbg);}
                var e=new EyeOpening{x0=gx0+left*g,step=g,top=new float[right-left+1],bot=new float[right-left+1],fromTexture=true};
                for(int ix=left;ix<=right;ix++){float hi=float.MinValue,lo=float.MaxValue;for(int iy=0;iy<ny;iy++)if(inEye[ix,iy]){hi=Mathf.Max(hi,gy0+iy*g);lo=Mathf.Min(lo,gy0+iy*g);}
                    e.top[ix-left]=hi==float.MinValue?float.NaN:hi;e.bot[ix-left]=lo==float.MaxValue?float.NaN:lo;}
                for(int q=0;q<e.top.Length;q++)if(float.IsNaN(e.top[q])){int r2=q;while(r2<e.top.Length&&float.IsNaN(e.top[r2]))r2++;int l2=q-1;float tt=l2>=0?e.top[l2]:(r2<e.top.Length?e.top[r2]:0),bb=l2>=0?e.bot[l2]:(r2<e.top.Length?e.bot[r2]:0);e.top[q]=tt;e.bot[q]=bb;}
                float width=e.x1-e.x0,height=e.top.Max()-e.bot.Min();
                if(width<ipd*.3f||width>ipd*.85f||height<width*.12f||height>width*.65f)return Prior("found "+(width*100).ToString("F1")+" x "+(height*100).ToString("F1")+" cm, out of range");
                for(int pass=0;pass<2;pass++){var tt=(float[])e.top.Clone();var bb=(float[])e.bot.Clone();for(int q=0;q<tt.Length;q++){float st=0,sb=0;int m2=0;for(int j2=Math.Max(0,q-3);j2<=Math.Min(tt.Length-1,q+3);j2++){st+=e.top[j2];sb+=e.bot[j2];m2++;}tt[q]=st/m2;bb[q]=sb/m2;}e.top=tt;e.bot=bb;}
                faceNote+=" "+(side>0?"left":"right")+" eye "+(width*100).ToString("F1")+" x "+(height*100).ToString("F1")+" cm from the texture;";
                return e;
            }
            int firstExtra=outVerts.Count;
            Vector3 Normal(float x,float y){int nearest=-1;float best=float.MaxValue;for(int v=0;v<n;v++){if(!NearEye(v))continue;float dd=new Vector2(face[v].x-x,face[v].y-y).sqrMagnitude;if(dd<best){best=dd;nearest=v;}}return nearest>=0?source.normals[nearest]:F;}
            int AddVertex(Vector3 facePoint,Vector2 tex,Vector3 normal){outVerts.Add(FromFace(facePoint));normals.Add(normal);tangents.Add(new Vector4(R.x,R.y,R.z,1));uv.Add(tex);if(hasColors)outColors.Add(Color.white);outWeights.Add(new BoneWeight1{boneIndex=head,weight=1});finalPer.Add(1);return outVerts.Count-1;}
            // Two triangles of a quad, wound to face out of the face at the given (shut or open) corners.
            void Quad(List<int> into,int a,int b,int c,int d,Vector3 pa,Vector3 pb,Vector3 pc,Vector3 pd){
                if(Vector3.Dot(Vector3.Cross(FromFace(pb)-FromFace(pa),FromFace(pc)-FromFace(pa)),F)>=0)into.AddRange(new[]{a,b,c,a,c,d});else into.AddRange(new[]{a,c,b,a,d,c});
            }
            foreach(int side in new[]{1,-1}) {
                var eye=FindEye(side);
                // Upper lid: shut, it covers the opening from just above the lashes to just below the lower
                // edge, curving over the eye; open, all of it lies along the upper edge with no height. It is
                // one patch of eyelid skin (a single texel just above the middle of the lashes: sampling the
                // texture point by point crossed atlas seams and showed the gown), and its lowest row takes the
                // darkest texel straight below that patch, the lash line, so the shut lid has a dark edge.
                float mid=(eye.x0+eye.x1)/2,skinY=eye.Top(mid)+.0035f;
                Vector2 skinTex=TexAt(mid,skinY),lashTex=skinTex;float darkest=float.MaxValue;
                for(float ly=skinY;ly>=eye.Top(mid)+.0001f;ly-=.0002f){var col=Colour(mid,ly);if(col.a>0&&Lu(col)<darkest){darkest=Lu(col);lashTex=TexAt(mid,ly);}}
                const int lidCols=12,lidRows=5;var ids=new int[lidCols,lidRows];var shut=new Vector3[lidCols,lidRows];
                for(int a=0;a<lidCols;a++) {
                    float x=Mathf.Lerp(eye.x0-.0006f,eye.x1+.0006f,a/(float)(lidCols-1));
                    float top=eye.Top(x)+.001f,bottom=Mathf.Min(eye.Bot(x)-.0007f,top-.0005f);
                    float zTop=Front(x,top,.0012f),zBottom=Front(x,bottom,.0012f);
                    if(zTop==float.MinValue)zTop=0;if(zBottom==float.MinValue)zBottom=zTop;
                    var rest=new Vector3(x,top,zTop+.0008f);
                    for(int b=0;b<lidRows;b++) {
                        float f=b/(float)(lidRows-1),y=Mathf.Lerp(top,bottom,f);
                        float z=Mathf.Max(Front(x,y,.001f),Mathf.Lerp(zTop,zBottom,f)+.0012f*Mathf.Sin(f*Mathf.PI))+.0008f;
                        shut[a,b]=new Vector3(x,y,z);
                        // A lid curves over the eye: facing up at its fold, forward in the middle, a little down at the lashes.
                        var lidNormal=(F+U*(.6f-1.1f*f)+R*((x-mid)/(eye.x1-eye.x0+1e-4f))*.5f).normalized;
                        ids[a,b]=AddVertex(rest,b==lidRows-1?lashTex:skinTex,lidNormal);blinkDelta[ids[a,b]]=FromFace(shut[a,b])-FromFace(rest);
                    }
                }
                for(int a=0;a<lidCols-1;a++)for(int b=0;b<lidRows-1;b++)Quad(lidTris,ids[a,b],ids[a+1,b],ids[a+1,b+1],ids[a,b+1],shut[a,b],shut[a+1,b],shut[a+1,b+1],shut[a,b+1]);
                // Tear film: a thin glossy strip along the lower lid from the inner corner, closed at rest.
                float inner=side>0?eye.x0:eye.x1,outer=side>0?eye.x1:eye.x0;
                const int filmCols=9;int[] upperIds=new int[filmCols],lowerIds=new int[filmCols];var up=new Vector3[filmCols];var down=new Vector3[filmCols];
                for(int a=0;a<filmCols;a++) {
                    float x=Mathf.Lerp(inner,Mathf.Lerp(inner,outer,.7f),a/(float)(filmCols-1)),yb=eye.Bot(x);
                    float z=Front(x,yb,.0012f);if(z==float.MinValue)z=0;z+=.0006f;
                    var rest=new Vector3(x,yb+.0001f,z);up[a]=new Vector3(x,yb+.0007f,z);down[a]=new Vector3(x,yb-.0011f,z);
                    upperIds[a]=AddVertex(rest,Vector2.zero,Normal(x,yb));lowerIds[a]=AddVertex(rest,Vector2.zero,Normal(x,yb));
                    filmDelta[upperIds[a]]=FromFace(up[a])-FromFace(rest);filmDelta[lowerIds[a]]=FromFace(down[a])-FromFace(rest);
                }
                for(int a=0;a<filmCols-1;a++)Quad(tearTris,upperIds[a],upperIds[a+1],lowerIds[a+1],lowerIds[a],up[a],up[a+1],down[a+1],down[a]);
                // One tear, from the inner corner of the eye on the camera's side (her left), running down
                // beside the nose: shut at rest; frames of the TearFlow shape reveal it along the cheek.
                if(side>0) {
                    const int points=9;var trail=new Vector3[points];var halfWidth=new float[points];
                    float sx=inner+.0008f,sy=eye.Bot(sx)-.0004f;
                    for(int a=0;a<points;a++){float x=sx+a*.0005f,y=sy-a*.0027f,z=Front(x,y,.0012f);if(z==float.MinValue)z=a>0?trail[a-1].z:0;trail[a]=new Vector3(x,y,z+.0007f);halfWidth[a]=Mathf.Lerp(.0003f,.0006f,a/(float)(points-1));}
                    var leftIds=new int[points];var rightIds=new int[points];
                    for(int a=0;a<points;a++){leftIds[a]=AddVertex(trail[0],Vector2.zero,Normal(trail[a].x,trail[a].y));rightIds[a]=AddVertex(trail[0],Vector2.zero,Normal(trail[a].x,trail[a].y));}
                    for(int frame=1;frame<points;frame++) {
                        var deltas=new Dictionary<int,Vector3>();
                        for(int a=0;a<points;a++){int at=Mathf.Min(a,frame);float w=halfWidth[at]*(a>=frame?1.5f:1);   // the leading drop is a little rounder
                            deltas[leftIds[a]]=FromFace(trail[at]-new Vector3(w,0,0))-FromFace(trail[0]);deltas[rightIds[a]]=FromFace(trail[at]+new Vector3(w,0,0))-FromFace(trail[0]);}
                        flowDelta.Add(deltas);
                    }
                    for(int a=0;a<points-1;a++)Quad(tearTris,leftIds[a],rightIds[a],rightIds[a+1],leftIds[a+1],trail[a]-new Vector3(.001f,0,0),trail[a]+new Vector3(.001f,0,0),trail[a+1]+new Vector3(.001f,0,0),trail[a+1]-new Vector3(.001f,0,0));
                }
            }
            // Smile: the mouth corners lift and draw out a little (and the opening's ends with them).
            float sigma=halfW*.45f;
            Vector3 Smile(Vector3 f){
                if(f.z<-ipd*.5f||f.y>-.018f)return Vector3.zero;
                Vector3 d=Vector3.zero;
                foreach(int side in new[]{-1,1}){float cx=side*halfW,cy=Line(cx);float w=Mathf.Exp(-((f.x-cx)*(f.x-cx)+(f.y-cy)*(f.y-cy)*1.4f)/(sigma*sigma));d+=new Vector3(side*.001f,.0026f,.0004f)*w;}
                return d;
            }
            for(int v=0;v<n;v++){if(headW[v]<.2f)continue;var d=Smile(face[v]);if(d.sqrMagnitude>1e-10f)smileDelta[v]=(R*d.x+U*d.y+F*d.z)/k;}
            for(int v=baseIndex;v<firstExtra;v++){var d=Smile(Face(outVerts[v]));if(d.sqrMagnitude>1e-10f)smileDelta[v]=(R*d.x+U*d.y+F*d.z)/k;}
        }
        UnityEngine.Object.DestroyImmediate(texture);

        var mesh=new Mesh{name=source.name+" (speaking mouth)",indexFormat=outVerts.Count>65000?UnityEngine.Rendering.IndexFormat.UInt32:source.indexFormat};
        mesh.SetVertices(outVerts);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.SetUVs(0,uv);if(hasColors)mesh.SetColors(outColors);
        bool tears=tearTris.Count>0;
        mesh.subMeshCount=subs.Length+1+(tears?1:0);
        // The lids are skin: they join the first (body) submesh.
        for(int s=0;s<subs.Length;s++)mesh.SetTriangles(s==0?subs[s].Concat(lidTris).ToArray():subs[s],s);
        mesh.SetTriangles(opening,subs.Length);report.mouthSubmesh=subs.Length;
        if(tears){mesh.SetTriangles(tearTris,subs.Length+1);report.tearSubmesh=subs.Length+1;}
        using(var per=new NativeArray<byte>(finalPer.ToArray(),Allocator.Temp))using(var bw=new NativeArray<BoneWeight1>(outWeights.ToArray(),Allocator.Temp))mesh.SetBoneWeights(per,bw);
        // The jaw hinges near the ear, level with the bottom of the nose and well behind the face.
        var pivotFace=new Vector3(0,-.33f*E,seamZ-ipd*1.25f);
        var jaw=new GameObject("MouthJaw").transform;jaw.SetParent(H,false);
        var pivotHead=bind[head].MultiplyPoint3x4(FromFace(pivotFace));jaw.localPosition=pivotHead;jaw.localRotation=Quaternion.identity;jaw.localScale=Vector3.one;
        var jawBind=Matrix4x4.TRS(pivotHead,Quaternion.identity,Vector3.one).inverse*bind[head];
        mesh.bindposes=bind.Concat(new[]{jawBind}).ToArray();
        int total=outVerts.Count;
        Vector3[] Deltas(Dictionary<int,Vector3> d){var a=new Vector3[total];foreach(var kv in d)a[kv.Key]=kv.Value;return a;}
        if(blinkDelta.Count>0)mesh.AddBlendShapeFrame("Blink",100,Deltas(blinkDelta),null,null);
        if(smileDelta.Count>0)mesh.AddBlendShapeFrame("Smile",100,Deltas(smileDelta),null,null);
        if(filmDelta.Count>0)mesh.AddBlendShapeFrame("TearFilm",100,Deltas(filmDelta),null,null);
        for(int fr=0;fr<flowDelta.Count;fr++)mesh.AddBlendShapeFrame("TearFlow",100f*(fr+1)/flowDelta.Count,Deltas(flowDelta[fr]),null,null);
        mesh.RecalculateBounds();
        string path=folder+"SpeakingMouth.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(mesh,path);
        renderer.sharedMesh=mesh;renderer.bones=bones.Concat(new[]{jaw}).ToArray();
        var motion=visual.AddComponent<PeriodCharacterMouthMotion>();motion.jaw=jaw;
        // Opening turns the chin down: about the face's left-right axis, expressed in the head's frame.
        motion.axis=bind[head].MultiplyVector(R).normalized;
        // A positive turn must lower the chin.
        var arm=FromFace(new Vector3(0,chin,seamZ))-FromFace(pivotFace);
        if(Vector3.Dot(Quaternion.AngleAxis(10,R)*arm-arm,U)>0)motion.axis=-motion.axis;
        // Six to eight millimetres between the lips at the widest syllable; ordinary speech opens less.
        motion.maxDegrees=E<.11f?4.5f:5f;
        if(expressive) {
            var expression=visual.AddComponent<PeriodCharacterExpression>();expression.face=renderer;
            expression.blink=mesh.GetBlendShapeIndex("Blink");expression.smile=mesh.GetBlendShapeIndex("Smile");
            expression.tearFilm=mesh.GetBlendShapeIndex("TearFilm");expression.tearFlow=mesh.GetBlendShapeIndex("TearFlow");
            Debug.Log("PERIOD_CHARACTER_FACE_PASS: "+id+faceNote+" "+lidTris.Count/3+" lid triangles, "+smileDelta.Count+" smile vertices, "+tearTris.Count/3+" tear triangles");
        }
        report.rigged=true;
        Debug.Log("PERIOD_CHARACTER_MOUTH_PASS: "+id+" seam "+(seam*100).ToString("F1")+" cm below the eyes (eyes to chin "+(E*100).ToString("F1")+" cm), mouth "+(halfW*200).ToString("F1")+" cm wide, "+jawVertices+" on the jaw"+(centreRidge>.015f?", lip line from the texture":", lip line from the profile"));
        return report;
    }
}

// Close-ups of every approved period character with the mouth shut and open, rendered in an
// empty scene (Editor batch mode): builds/cast-review/mouth/<id>-closed|open.png.
public static class PeriodCharacterMouthReview {
    public static void Render() {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        var light=new GameObject("Key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(20,160,0);
        var fill=new GameObject("Fill").AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.transform.rotation=Quaternion.Euler(10,220,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.45f,.48f);
        var cam=new GameObject("Camera").AddComponent<Camera>();cam.fieldOfView=22;cam.nearClipPlane=.02f;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.55f,.55f,.56f);
        var rt=new RenderTexture(640,640,24);cam.targetTexture=rt;
        var dir=System.IO.Path.GetFullPath("../builds/cast-review/mouth");System.IO.Directory.CreateDirectory(dir);
        var cast=PeriodCharacterAsset.Ids.Select(i=>("Assets/Art/PoliceCast/",i)).Concat(new[]{"Child","ChildGown","Porter","WardSister","StaffNurse","Probationer","Matron","HouseSurgeon","VisitingDoctor","PatientMan","PatientWoman","PatientBoy","VisitorWoman","VisitorMan","VisitorGranny"}.Select(i=>(HospitalCastAuthoring.Folder,i)));
        var root=new GameObject("Cast").transform;int done=0;
        foreach(var (folder,id) in cast) {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(folder+id+"/Character.fbx"))continue;
            var actor=folder==HospitalCastAuthoring.Folder?PeriodCharacterAsset.Create(root,Vector3.zero,folder,id,id,HospitalCastAuthoring.Size[id].height,HospitalCastAuthoring.Size[id].width):PeriodCharacterAsset.Create(root,Vector3.zero,id,id);
            var motion=actor.GetComponentInChildren<PeriodCharacterMouthMotion>();
            if(!motion){Debug.LogWarning("PERIOD_CHARACTER_MOUTH_MISSING: "+id);}
            else {
                var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(r=>r.sharedMesh.vertexCount).First();
                var a0=new Mesh();motion.jaw.localRotation=Quaternion.identity;skin.BakeMesh(a0,true);
                var a1=new Mesh();motion.jaw.localRotation=Quaternion.AngleAxis(motion.maxDegrees,motion.axis);skin.BakeMesh(a1,true);
                var v0=a0.vertices;var v1=a1.vertices;float most=0;for(int v=0;v<v0.Length;v++)most=Mathf.Max(most,(skin.transform.TransformVector(v1[v]-v0[v])).magnitude);
                int jawIndex=skin.bones.Length-1;int weighted=skin.sharedMesh.GetAllBoneWeights().Count(w=>w.boneIndex==jawIndex&&w.weight>.01f);
                Debug.Log("PERIOD_CHARACTER_MOUTH_MEASURE: "+id+" jaw bone "+skin.bones[jawIndex].name+", "+weighted+" weights on it, chin moves "+(most*1000).ToString("F1")+" mm at "+motion.maxDegrees+" degrees");
                UnityEngine.Object.DestroyImmediate(a0);UnityEngine.Object.DestroyImmediate(a1);
            }
            var bones=actor.GetComponentsInChildren<Transform>(true);var head=bones.First(t=>t.name=="Head");var front=bones.First(t=>t.name=="headfront");
            var eyes=(bones.First(t=>t.name=="Left_Eye").position+bones.First(t=>t.name=="Right_Eye").position)/2;
            var forward=(front.position-head.position);forward.y=0;forward.Normalize();
            foreach(var (name,yaw) in new[]{("front",0f),("three-quarter",35f)}) {
                var look=Quaternion.AngleAxis(yaw,Vector3.up)*forward;
                cam.transform.position=eyes-Vector3.up*.06f+look*.42f;cam.transform.LookAt(eyes-Vector3.up*.075f);
                foreach(var (state,deg) in new[]{("closed",0f),("open",motion?motion.maxDegrees:0)}) {
                    if(motion)motion.jaw.localRotation=Quaternion.AngleAxis(deg,motion.axis);
                    var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=new List<GameObject>();
                    foreach(var s in skins){var m=new Mesh();s.BakeMesh(m,true);var g=new GameObject("Baked");g.transform.SetPositionAndRotation(s.transform.position,s.transform.rotation);g.transform.localScale=s.transform.lossyScale;g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;baked.Add(g);}
                    cam.Render();
                    foreach(var g in baked){UnityEngine.Object.DestroyImmediate(g.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(g);}foreach(var s in skins)s.enabled=true;RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,id+"-"+name+"-"+state+".png"),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
                }
                // The expressive face: a blink, a tired rest, the smile and the tears.
                var expression=actor.GetComponentInChildren<PeriodCharacterExpression>();
                if(expression){cam.transform.position=eyes-Vector3.up*.03f+look*.34f;cam.transform.LookAt(eyes-Vector3.up*.03f);}
                if(expression)foreach(var (state,smile,tears,run,lid) in new[]{("rest",0f,0f,0f,expression.restLid),("blink",0f,0f,0f,1f),("half-blink",0f,0f,0f,.55f),("smile",1f,0f,0f,expression.restLid),("tears",.3f,1f,1f,expression.restLid)}) {
                    if(motion)motion.jaw.localRotation=Quaternion.identity;
                    expression.SetMoodNow(smile,tears,run);expression.Shut(lid);
                    var skins=actor.GetComponentsInChildren<SkinnedMeshRenderer>();var baked=new List<GameObject>();
                    foreach(var s in skins){var m=new Mesh();s.BakeMesh(m,true);var g=new GameObject("Baked");g.transform.SetPositionAndRotation(s.transform.position,s.transform.rotation);g.transform.localScale=s.transform.lossyScale;g.AddComponent<MeshFilter>().sharedMesh=m;g.AddComponent<MeshRenderer>().sharedMaterials=s.sharedMaterials;s.enabled=false;baked.Add(g);}
                    cam.Render();
                    foreach(var g in baked){UnityEngine.Object.DestroyImmediate(g.GetComponent<MeshFilter>().sharedMesh);UnityEngine.Object.DestroyImmediate(g);}foreach(var s in skins)s.enabled=true;RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir,id+"-"+name+"-face-"+state+".png"),tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);
                }
            }
            UnityEngine.Object.DestroyImmediate(actor);done++;
        }
        RenderTexture.active=null;AssetDatabase.SaveAssets();
        Debug.Log("PERIOD_CHARACTER_MOUTH_REVIEW: "+done+" characters rendered to "+dir);
    }
}
