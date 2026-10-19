using System.Collections.Generic;
using UnityEngine;

// What a document physically is. Every paper keeps its typeset face and proportions; the kind decides
// the stock, the way it has been folded, torn or bound, and what lies with it (user, 2026-10-19:
// a hospital record, a newspaper and a note must each look like their own thing).
public enum PaperKind { Envelope, Sheet, Form, Dossier, Letter, Note, Receipt, Ledger, Newspaper, Photo, Drawing, Card, FoldedCard, Notice, Carbon, NotebookPage, Fragment }

// Surface response read by the study shader. Lives on each built part.
public sealed class EvidenceSurface : MonoBehaviour {
    public float rough=.85f, fibre=1, laid, weave, stripes, translucency=.25f, gloss, metal, leather, brushed, newsprint, age, glass;
}

public static class EvidenceKinds {
    // Stable evidence IDs first; anything else (new records, reading-only props) falls back to its title.
    static readonly Dictionary<string,PaperKind> byId=new Dictionary<string,PaperKind>{
        {"E01",PaperKind.Dossier},{"E05",PaperKind.Ledger},{"E07",PaperKind.Notice},{"E08",PaperKind.Carbon},{"E09",PaperKind.Fragment},{"E10",PaperKind.Letter},
        {"H01",PaperKind.Form},{"H03",PaperKind.Photo},{"H04",PaperKind.Form},{"H05",PaperKind.Form},{"H06",PaperKind.Photo},{"H09",PaperKind.Receipt},{"H10",PaperKind.Letter},
        {"H11",PaperKind.Receipt},{"H12",PaperKind.Form},{"H13",PaperKind.Receipt},{"H14",PaperKind.Note},{"H15",PaperKind.Receipt},{"H16",PaperKind.Note},{"H19",PaperKind.Receipt},
        {"H20",PaperKind.Letter},{"H21",PaperKind.Letter},{"H22",PaperKind.Note},{"H23",PaperKind.Receipt},{"H24",PaperKind.FoldedCard},{"H25",PaperKind.Photo},{"H26",PaperKind.NotebookPage},
        {"Y01",PaperKind.Ledger},{"Y02",PaperKind.Receipt},{"Y03",PaperKind.Drawing},
        {"I01",PaperKind.Ledger},{"I02",PaperKind.Ledger},{"I03",PaperKind.Ledger},{"W01",PaperKind.Carbon},{"W02",PaperKind.Carbon},
    };
    static readonly (string word,PaperKind kind)[] byTitle={
        ("报纸",PaperKind.Newspaper),("报",PaperKind.Newspaper),("照片",PaperKind.Photo),("画",PaperKind.Drawing),("生日卡",PaperKind.FoldedCard),("贺卡",PaperKind.FoldedCard),
        ("信封",PaperKind.Envelope),("残信",PaperKind.Fragment),("裁余",PaperKind.Fragment),("抄件",PaperKind.Carbon),("抄本",PaperKind.Carbon),("誊录",PaperKind.Carbon),("案卷",PaperKind.Dossier),
        ("便条",PaperKind.Note),("纸条",PaperKind.Note),("便笺",PaperKind.Note),("笔记",PaperKind.NotebookPage),("索引卡",PaperKind.Card),("卡",PaperKind.Card),
        ("来信",PaperKind.Letter),("回信",PaperKind.Letter),("家书",PaperKind.Letter),("来函",PaperKind.Letter),("函",PaperKind.Letter),("信",PaperKind.Letter),
        ("登记簿",PaperKind.Ledger),("登记本",PaperKind.Ledger),("账本",PaperKind.Ledger),("值班簿",PaperKind.Ledger),("派送簿",PaperKind.Ledger),("簿",PaperKind.Ledger),
        ("存根",PaperKind.Receipt),("收据",PaperKind.Receipt),("回执",PaperKind.Receipt),("凭据",PaperKind.Receipt),("账单",PaperKind.Receipt),("催账",PaperKind.Receipt),("点菜单",PaperKind.Receipt),
        ("通知",PaperKind.Notice),("告示",PaperKind.Notice),("须知",PaperKind.Notice),("规则",PaperKind.Notice),
        ("清点",PaperKind.Form),("申报",PaperKind.Form),("记录",PaperKind.Form),("保险单",PaperKind.Form),("工单",PaperKind.Form),("许可",PaperKind.Form),("目录",PaperKind.Form),("单",PaperKind.Form),
    };
    public static PaperKind For(string id,string title) {
        if(!string.IsNullOrEmpty(id)&&byId.TryGetValue(id.ToUpperInvariant(),out var k))return k;
        if(!string.IsNullOrEmpty(title))foreach(var (word,kind) in byTitle)if(title.Contains(word))return kind;
        return PaperKind.Sheet;
    }
    public static string Label(PaperKind kind)=>kind switch{
        PaperKind.Envelope=>"文件 · 信封",PaperKind.Letter=>"文件 · 信件",PaperKind.Note=>"文件 · 便条",PaperKind.Receipt=>"文件 · 票据存根",PaperKind.Ledger=>"文件 · 簿册",PaperKind.Newspaper=>"文件 · 报纸",
        PaperKind.Photo=>"文件 · 照片",PaperKind.Drawing=>"文件 · 画纸",PaperKind.Card=>"文件 · 卡片",PaperKind.FoldedCard=>"文件 · 贺卡",PaperKind.Notice=>"文件 · 告示",
        PaperKind.Carbon=>"文件 · 抄件",PaperKind.NotebookPage=>"文件 · 笔记页",PaperKind.Fragment=>"文件 · 残片",PaperKind.Dossier=>"文件 · 案卷",PaperKind.Form=>"文件 · 表单",_=>"文件"};

    // ---- stock -------------------------------------------------------------------------------------
    struct Stock { public float thick,rough,fibre,laid,translucency,gloss,newsprint,age; public Color face,back,edge; }
    static Stock StockOf(PaperKind kind) {
        var s=new Stock{thick=.0045f,rough=.86f,fibre=1,translucency=.28f,age=.6f,face=new Color(.95f,.92f,.85f),back=new Color(.80f,.75f,.62f),edge=new Color(.66f,.60f,.48f)};
        switch(kind){
            case PaperKind.Letter: s.thick=.003f;s.laid=1;s.translucency=.5f;s.back=new Color(.86f,.82f,.72f);break;
            case PaperKind.Note: s.thick=.0035f;s.fibre=1.4f;s.translucency=.42f;s.back=new Color(.83f,.79f,.68f);break;
            case PaperKind.Receipt: s.thick=.005f;s.fibre=1.2f;s.back=new Color(.78f,.74f,.63f);s.edge=new Color(.6f,.55f,.45f);break;
            case PaperKind.Ledger: s.thick=.004f;s.laid=.4f;s.back=new Color(.84f,.8f,.68f);break;
            case PaperKind.Newspaper: s.age=.85f; s.thick=.0025f;s.rough=.95f;s.fibre=1.8f;s.newsprint=1;s.translucency=.55f;s.face=new Color(.9f,.88f,.82f);s.back=new Color(.74f,.72f,.66f);s.edge=new Color(.62f,.6f,.55f);break;
            case PaperKind.Photo: s.age=.3f; s.thick=.008f;s.rough=.18f;s.fibre=.1f;s.gloss=1;s.translucency=0;s.face=Color.white;s.back=new Color(.86f,.84f,.78f);s.edge=new Color(.8f,.78f,.72f);break;
            case PaperKind.Drawing: s.age=.45f; s.thick=.007f;s.rough=.97f;s.fibre=2.2f;s.translucency=.1f;s.face=new Color(.97f,.95f,.9f);s.back=new Color(.88f,.85f,.78f);break;
            case PaperKind.Card: case PaperKind.FoldedCard: s.age=.5f; s.thick=.009f;s.rough=.7f;s.fibre=.8f;s.translucency=.08f;s.back=new Color(.86f,.82f,.72f);s.edge=new Color(.78f,.74f,.64f);break;
            case PaperKind.Notice: s.thick=.0035f;s.fibre=1.3f;s.translucency=.45f;break;
            case PaperKind.Carbon: s.age=.4f; s.thick=.0025f;s.rough=.6f;s.fibre=.7f;s.translucency=.7f;s.face=new Color(.93f,.93f,.95f);s.back=new Color(.82f,.83f,.88f);s.edge=new Color(.7f,.7f,.74f);break;
            case PaperKind.NotebookPage: s.thick=.0035f;s.translucency=.4f;s.back=new Color(.85f,.82f,.72f);break;
            case PaperKind.Envelope: s.thick=.007f;s.rough=.88f;s.fibre=1.5f;s.translucency=.12f;s.age=.65f;s.back=new Color(.76f,.66f,.48f);s.edge=new Color(.66f,.56f,.4f);break;
            case PaperKind.Fragment: s.thick=.003f;s.laid=1;s.translucency=.5f;break;
        }
        return s;
    }

    // ---- public builders ---------------------------------------------------------------------------
    // Close-up model: the sheet plus whatever it physically comes with (book, envelope, folder, clip).
    public static GameObject Build(PaperKind kind,string name,Texture2D face,Texture2D reverse,float width,float height) {
        var root=new GameObject(name);float a=Mathf.Min(width,height);var s=StockOf(kind);
        Sheet(root.transform,kind,face,reverse,width,height,false,s);
        Extras(root.transform,kind,width,height,a,s);
        return root;
    }
    // A bound book open at one page (EvidenceBook). The current page lies on the right; on the left is
    // the plain verso of the leaf before it (or the first endpaper). flip=1 adds the previous leaf hinged
    // at the spine lying over the right page (to be turned to the left); flip=-1 adds this page's leaf
    // lying on the left (to be turned back onto the right). The hinge is "Turning leaf hinge", turned
    // about the spine (local Y), 0 = on the right, 180 = on the left.
    public static GameObject BuildBook(Texture2D[] faces,Texture2D[] reverses,int page,int flip,float w,float h) {
        var root=new GameObject("Observed bound book");float a=Mathf.Min(w,h);var s=StockOf(PaperKind.Ledger);float under=s.thick*a;
        var right=new GameObject("Right page");right.transform.SetParent(root.transform,false);
        Sheet(right.transform,PaperKind.Ledger,faces[page],reverses[page],w,h,false,s);
        // Left: a plain sheet mirrored about the spine (scale -1 keeps its face up and its curve rising).
        var left=new GameObject(page==0?"Front endpaper":"Verso of the previous leaf");left.transform.SetParent(root.transform,false);
        left.transform.localPosition=new Vector3(-w,0,0);left.transform.localScale=new Vector3(-1,1,1);
        Sheet(left.transform,PaperKind.Ledger,page>0?reverses[page-1]:null,null,w,h,false,s);
        if(page==0)foreach(var r in left.GetComponentsInChildren<Renderer>())r.sharedMaterial.SetColor("_BaseColor",new Color(.8f,.79f,.71f));
        if(flip!=0){
            int leafPage=flip>0?page-1:page;
            var hinge=new GameObject("Turning leaf hinge");hinge.transform.SetParent(root.transform,false);hinge.transform.localPosition=new Vector3(-w/2,0,-.004f*a);
            var leaf=new GameObject("Turning leaf");leaf.transform.SetParent(hinge.transform,false);leaf.transform.localPosition=new Vector3(w/2,0,0);
            // Same curved page as the right-hand one, lifted just clear of it, so at 0 it lies exactly over it.
            Sheet(leaf.transform,PaperKind.Ledger,faces[leafPage],reverses[leafPage],w,h,false,s);
            hinge.transform.localRotation=Quaternion.Euler(0,flip>0?0:180,0);
        }
        // Binding across both halves: page block, cloth boards, rounded spine, ribbon marker.
        float block=.06f*a,cover=.012f*a;var cloth=new Color(.17f,.25f,.2f);
        Solid(root.transform,"Ledger page block",new Vector3(-w/2,0,under+block/2+.002f*a),new Vector3(w*1.97f,h*.98f,block),new Color(.86f,.82f,.7f),stripes:1);
        Solid(root.transform,"Ledger cloth cover",new Vector3(-w/2,0,under+block+cover/2+.002f*a),new Vector3(w*2.09f,h*1.04f,cover),cloth,weave:1,rough:.9f);
        var spine=Solid(root.transform,"Ledger spine",new Vector3(-w/2,0,under+block+cover),Vector3.one,cloth*.8f,weave:1,rough:.9f,shape:PrimitiveType.Cylinder);spine.transform.localScale=new Vector3(.08f*a,h*.52f,.05f*a);
        Solid(root.transform,"Ledger ribbon",new Vector3(-w/2+.03f*w,-h*.52f,under+.004f*a),new Vector3(.025f*a,.14f*h,.002f*a),new Color(.5f,.12f,.1f),weave:.4f,rough:.6f);
        return root;
    }
    // Loose sheets kept together. The current sheet lies on top ("Stack top sheet"); the others are
    // fanned underneath in reading order, on a file folder when there is one. flip=1 adds the previous
    // sheet above, ready to slide away; flip=-1 adds the current sheet off to the side, ready to slide
    // back on ("Sliding sheet" with an EvidenceSlide).
    public static GameObject BuildStack(Texture2D[] faces,Texture2D[] reverses,Vector2[] sizes,PaperKind[] kinds,int page,int flip,string container) {
        var root=new GameObject("Observed stack of papers");int n=faces.Length;float maxW=0,maxH=0;
        foreach(var v in sizes){maxW=Mathf.Max(maxW,v.x);maxH=Mathf.Max(maxH,v.y);}float a=Mathf.Min(maxW,maxH);
        GameObject SheetAt(string name,int k,bool full){
            var g=new GameObject(name);g.transform.SetParent(root.transform,false);var st=StockOf(kinds[k]);
            Sheet(g.transform,kinds[k],faces[k],reverses[k],sizes[k].x,sizes[k].y,!full,st);
            if(full&&(kinds[k]==PaperKind.Envelope||kinds[k]==PaperKind.Photo||kinds[k]==PaperKind.Notice))Extras(g.transform,kinds[k],sizes[k].x,sizes[k].y,Mathf.Min(sizes[k].x,sizes[k].y),st);
            return g;
        }
        for(int d=1;d<n;d++){int k=(page+d)%n;var g=SheetAt("Stacked sheet "+(k+1),k,false);float side=d%2==0?1:-1;
            g.transform.localPosition=new Vector3(side*d*.022f*a,-d*.026f*a,d*.022f*a);g.transform.localRotation=Quaternion.Euler(0,0,side*(1.2f+d*1.4f));}
        SheetAt("Stack top sheet",page,true);
        if(container=="folder"){
            var kraft=new Color(.72f,.56f,.34f);float z=n*.022f*a+.012f*a;
            Solid(root.transform,"File folder board",new Vector3(0,-.01f*maxH,z),new Vector3(maxW*1.16f,maxH*1.1f,.008f*a),kraft,rough:.9f,fibre:1.8f);
            Solid(root.transform,"File folder tab",new Vector3(-maxW*.3f,maxH*.57f,z),new Vector3(maxW*.3f,maxH*.06f,.008f*a),kraft*.95f,rough:.9f,fibre:1.8f);
            Solid(root.transform,"File folder label",new Vector3(-maxW*.3f,maxH*.575f,z-.0045f*a),new Vector3(maxW*.22f,maxH*.035f,.001f*a),new Color(.9f,.87f,.78f),rough:.9f,fibre:1);
        }
        if(flip!=0){
            int k=flip>0?page-1:page;var g=SheetAt("Sliding sheet",k,true);var slide=g.AddComponent<EvidenceSlide>();
            var top=new Vector3(0,0,-.012f*a);var away=new Vector3(maxW*1.18f,maxH*.06f,-.05f*a);
            slide.from=flip>0?top:away;slide.to=flip>0?away:top;slide.spinFrom=flip>0?0:-9;slide.spinTo=flip>0?-9:0;
            g.transform.localPosition=slide.from;g.transform.localRotation=Quaternion.Euler(0,0,slide.spinFrom);
        }
        return root;
    }
    // World copy: same deformation family at a gentler amplitude, no extra parts, same three child
    // names, so furniture placement, reviews and the close-up clone stay valid.
    public static void DressWorld(GameObject physical,PaperKind kind) {
        var faceR=Find(physical.transform,"Paper printed face");var backR=Find(physical.transform,"Paper reverse");var edgeR=Find(physical.transform,"Paper cut edges");
        if(!faceR||!backR||!edgeR)return;
        var mf=faceR.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)return;
        var b=mf.sharedMesh.bounds;float w=b.size.x,h=b.size.y;if(w<.01f||h<.01f)return;
        var s=StockOf(kind);s.thick=.0014f/Mathf.Max(.05f,Mathf.Min(w,h));
        var backMat=backR.GetComponent<MeshRenderer>().sharedMaterial;bool printedReverse=backMat&&backMat.HasProperty("_BaseMap")&&backMat.GetTexture("_BaseMap");
        var set=Meshes(kind,w,h,true,s,printedReverse);
        faceR.GetComponent<MeshFilter>().sharedMesh=set.face;backR.GetComponent<MeshFilter>().sharedMesh=set.back;edgeR.GetComponent<MeshFilter>().sharedMesh=set.edge;
    }
    static Transform Find(Transform root,string n){foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name==n)return t;return null;}

    // ---- the sheet ---------------------------------------------------------------------------------
    struct MeshSet { public Mesh face,back,edge; }
    static void Sheet(Transform parent,PaperKind kind,Texture2D face,Texture2D reverse,float w,float h,bool world,Stock s) {
        var set=Meshes(kind,w,h,world,s,reverse?true:false);
        Part(parent,"Paper printed face",set.face,face,face?s.face:s.back,s);
        Part(parent,"Paper reverse",set.back,reverse,reverse?Color.white:s.back,s);
        Part(parent,"Paper cut edges",set.edge,null,s.edge,s);
    }
    static GameObject Part(Transform parent,string name,Mesh mesh,Texture2D tex,Color color,Stock s) {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetColor("_BaseColor",color);if(tex)mat.SetTexture("_BaseMap",tex);
        go.AddComponent<MeshRenderer>().sharedMaterial=mat;
        var f=go.AddComponent<EvidenceSurface>();f.rough=s.rough;f.fibre=s.fibre;f.laid=s.laid;f.translucency=s.translucency;f.gloss=s.gloss;f.newsprint=s.newsprint;f.age=s.age;
        return go;
    }

    static readonly Dictionary<string,MeshSet> worldCache=new Dictionary<string,MeshSet>();
    static MeshSet Meshes(PaperKind kind,float w,float h,bool world,Stock s,bool mirroredReverse) {
        string key=kind+"|"+w.ToString("0.0000")+"|"+h.ToString("0.0000")+"|"+mirroredReverse;
        if(world&&worldCache.TryGetValue(key,out var cached)&&cached.face)return cached;
        int res=world?56:110;int nx=Mathf.Clamp(Mathf.RoundToInt(res*w/Mathf.Max(w,h)),14,res),ny=Mathf.Clamp(Mathf.RoundToInt(res*h/Mathf.Max(w,h)),14,res);
        if(kind==PaperKind.Letter||kind==PaperKind.Newspaper||kind==PaperKind.FoldedCard)ny=Mathf.Max(ny,30);
        if(kind==PaperKind.Newspaper||kind==PaperKind.FoldedCard||kind==PaperKind.Ledger)nx=Mathf.Max(nx,36);
        float a=Mathf.Min(w,h),amp=world?.35f:1f,thick=s.thick*a;
        var rnd=new System.Random(kind.GetHashCode()*7919+Mathf.RoundToInt(w*1000)*31+Mathf.RoundToInt(h*1000));
        float R()=> (float)rnd.NextDouble();
        // A few seeded bumps for crumpled / cockled stocks.
        var bumps=new List<Vector4>();for(int i=0;i<7;i++)bumps.Add(new Vector4(R(),R(),.12f+.2f*R(),R()*2-1));
        var tear=new float[129];for(int i=0;i<tear.Length;i++)tear[i]=R();
        float Tear(float t,float amount,float detail){ // jagged profile 0..amount, a few teeth per unit
            float x=t*detail;int i0=Mathf.FloorToInt(x)&127;float f=x-Mathf.Floor(x);float n=Mathf.Lerp(tear[i0],tear[(i0+1)&127],f);
            float fine=tear[(Mathf.FloorToInt(t*detail*4)+17)&127];return amount*(.55f*n+.45f*fine);}
        // Close-up only: paper is never dead flat. Gentle cockling from damp and handling, and one or two
        // old soft creases (random mountain or valley), on every stock that is not stiff card or photo.
        bool limp=!world&&kind!=PaperKind.Photo&&kind!=PaperKind.Card&&kind!=PaperKind.FoldedCard;
        var phase=new float[6];for(int i=0;i<phase.Length;i++)phase[i]=R()*6.283f;
        var creases=new List<Vector4>();int creaseCount=kind==PaperKind.Note||kind==PaperKind.Fragment?3:kind==PaperKind.Ledger||kind==PaperKind.Newspaper?1:2;
        for(int i=0;i<creaseCount;i++){float ang=R()*Mathf.PI;creases.Add(new Vector4(.2f+.6f*R(),.2f+.6f*R(),ang,R()<.5f?-1:1));}
        float Handling(float u,float v){
            if(!limp)return 0;
            float x=(u-.5f)*w,y=(v-.5f)*h;
            float cockle=Mathf.Sin(x/a*11+phase[0])*Mathf.Sin(y/a*9+phase[1])+.55f*Mathf.Sin(x/a*23+phase[2])*Mathf.Sin(y/a*19+phase[3])+.3f*Mathf.Sin((x+y)/a*41+phase[4]);
            float z=-.0032f*a*cockle;
            foreach(var c in creases){float cx=(c.x-.5f)*w,cy=(c.y-.5f)*h;float d=(x-cx)*Mathf.Sin(c.z)-(y-cy)*Mathf.Cos(c.z);
                float along=Mathf.Abs((x-cx)*Mathf.Cos(c.z)+(y-cy)*Mathf.Sin(c.z));float fade=Mathf.Clamp01(1.4f-along/(a*.55f));
                z+=c.w*.0065f*a*Mathf.Max(0,1-Mathf.Abs(d)/(.028f*a))*fade;}
            return z;
        }
        float Lift(float u,float v)=>LiftShape(u,v)+Handling(u,v);
        float LiftShape(float u,float v){
            float z=0;
            switch(kind){
                case PaperKind.Letter:case PaperKind.Fragment:{float tri=Mathf.Abs(Mathf.Repeat(1.5f*v+.5f,1)-.5f)*2;z=-.05f*a*(1-tri);z-=.006f*a*(1-Mathf.Abs(2*u-1));break;}
                case PaperKind.Note:{foreach(var b in bumps){float d=((new Vector2(u,v)-new Vector2(b.x,b.y)).magnitude)/b.z;z+=-.02f*a*b.w*Mathf.Exp(-d*d);}z-=.012f*a*Mathf.Max(0,(u-v));break;}
                case PaperKind.Receipt:z=-.03f*a*Mathf.Pow(Mathf.Abs(2*v-1),2)-.006f*a*Mathf.Sin(u*Mathf.PI);break;
                case PaperKind.Ledger:{float g=w/h>1.15f?Mathf.Abs(2*u-1):u;z=-.05f*a*Mathf.Sqrt(Mathf.Clamp01(g*3))*(1-.25f*g*g)-.004f*a;break;}
                case PaperKind.Newspaper:z=-.05f*a*(1-Mathf.Abs(2*u-1))-.006f*a*Mathf.Sin(v*9+u*4)*Mathf.Pow(Mathf.Abs(2*u-1),3);break;
                case PaperKind.Photo:z=-.018f*a*Mathf.Pow(2*v-1,2);break;
                case PaperKind.Drawing:foreach(var b in bumps){float d=((new Vector2(u,v)-new Vector2(b.x,b.y)).magnitude)/(b.z*.7f);z+=-.012f*a*Mathf.Abs(b.w)*Mathf.Exp(-d*d);}break;
                case PaperKind.FoldedCard:z=-.32f*a*(.5f-Mathf.Abs(u-.5f))*(world?.25f:1);break;
                case PaperKind.Notice:{float c=Mathf.Max(0,1-v*3);z=-.05f*a*c*c*Mathf.Pow(Mathf.Abs(2*u-1),2);break;}
                case PaperKind.Carbon:z=-.004f*a*Mathf.Sin(u*23+v*7)*Mathf.Sin(v*17)-.01f*a*Mathf.Pow(Mathf.Max(0,1-v*2.5f),2);break;
                case PaperKind.NotebookPage:z=-.012f*a*Mathf.Pow(1-v,2);break;
                case PaperKind.Card:z=-.004f*a*Mathf.Sin(u*Mathf.PI);break;
                default:{float corner=Mathf.Max(0,(u+(1-v))-1.45f)/.55f;z=-.04f*a*corner*corner-.006f*a*Mathf.Sin(u*Mathf.PI*2)*(1-v);break;}
            }
            return z*amp;
        }
        // Outline: torn, perforated or cut edges inset the boundary (0 = straight edge). UVs follow the
        // final position, so the print is cut by the edge rather than squashed.
        float Top(float u)=>kind==PaperKind.Note?Tear(u,.035f,9):kind==PaperKind.NotebookPage?Mathf.Pow(Mathf.Abs(Mathf.Sin(u*Mathf.PI*14)),6)*.02f+Tear(u,.012f,20):kind==PaperKind.Fragment?Tear(u,.10f,5):0;
        float Bottom(float u)=>kind==PaperKind.Fragment?Tear(u+.37f,.07f,6):0;
        float Left(float v)=>kind==PaperKind.Receipt?.012f+.012f*Mathf.Pow(Mathf.Abs(Mathf.Sin(v*Mathf.PI*22)),.5f):kind==PaperKind.Drawing?Tear(v+.61f,.02f,14):kind==PaperKind.Fragment?Tear(v+.13f,.08f,5):0;
        float Right(float v)=>kind==PaperKind.Fragment?.12f*v:0;
        Vector2 Shape(float u,float v)=>new Vector2(Mathf.Lerp(Left(v),1-Right(v),u),Mathf.Lerp(Bottom(u),1-Top(u),v));
        bool Rounded=kind==PaperKind.Photo||kind==PaperKind.Card||kind==PaperKind.FoldedCard;float radius=Rounded?.035f*a:0;
        var top=new Vector3[(nx+1)*(ny+1)];var uv=new Vector2[top.Length];
        for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++){
            float u=x/(float)nx,v=y/(float)ny;var q=Shape(u,v);
            var p=new Vector2((q.x-.5f)*w,(q.y-.5f)*h);
            if(Rounded){var c=new Vector2(Mathf.Clamp(p.x,-w/2+radius,w/2-radius),Mathf.Clamp(p.y,-h/2+radius,h/2-radius));var d=p-c;if(d.magnitude>radius)p=c+d.normalized*radius;}
            int i=y*(nx+1)+x;top[i]=new Vector3(p.x,p.y,Lift(p.x/w+.5f,p.y/h+.5f));uv[i]=new Vector2(p.x/w+.5f,p.y/h+.5f);
        }
        var tris=new int[nx*ny*6];int k2=0;
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=y*(nx+1)+x;tris[k2++]=i;tris[k2++]=i+nx+1;tris[k2++]=i+1;tris[k2++]=i+1;tris[k2++]=i+nx+1;tris[k2++]=i+nx+2;}
        var face=new Mesh{name="Evidence "+kind+" face"};if(top.Length>65000)face.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
        face.vertices=top;face.uv=uv;face.triangles=tris;face.RecalculateNormals();face.RecalculateBounds();
        var bv=new Vector3[top.Length];var buv=new Vector2[top.Length];for(int i=0;i<top.Length;i++){bv[i]=top[i]+Vector3.forward*thick;buv[i]=mirroredReverse?new Vector2(1-uv[i].x,uv[i].y):uv[i];}
        var btri=(int[])tris.Clone();System.Array.Reverse(btri);
        var back=new Mesh{name="Evidence "+kind+" reverse"};back.vertices=bv;back.uv=buv;back.triangles=btri;back.RecalculateNormals();back.RecalculateBounds();
        // Thin edge band around the outline loop.
        var loop=new List<int>();for(int x=0;x<=nx;x++)loop.Add(x);for(int y=1;y<=ny;y++)loop.Add(y*(nx+1)+nx);for(int x=nx-1;x>=0;x--)loop.Add(ny*(nx+1)+x);for(int y=ny-1;y>=1;y--)loop.Add(y*(nx+1));
        var ev=new List<Vector3>();var eu=new List<Vector2>();var et=new List<int>();float run=0;
        for(int i=0;i<loop.Count;i++){var p0=top[loop[i]];var p1=top[loop[(i+1)%loop.Count]];int b0=ev.Count;float len=(p1-p0).magnitude;
            ev.Add(p0);ev.Add(p1);ev.Add(p0+Vector3.forward*thick);ev.Add(p1+Vector3.forward*thick);eu.Add(new Vector2(run,0));eu.Add(new Vector2(run+len,0));eu.Add(new Vector2(run,1));eu.Add(new Vector2(run+len,1));run+=len;
            et.AddRange(new[]{b0,b0+2,b0+1,b0+1,b0+2,b0+3,b0+1,b0+2,b0,b0+3,b0+2,b0+1});}
        var edgeMesh=new Mesh{name="Evidence "+kind+" edge"};edgeMesh.SetVertices(ev);edgeMesh.SetUVs(0,eu);edgeMesh.SetTriangles(et,0);edgeMesh.RecalculateNormals();edgeMesh.RecalculateBounds();
        var set=new MeshSet{face=face,back=back,edge=edgeMesh};if(world)worldCache[key]=set;return set;
    }

    // ---- what comes with the paper ------------------------------------------------------------------
    static void Extras(Transform root,PaperKind kind,float w,float h,float a,Stock s) {
        float under=s.thick*a;   // +Z is behind the sheet
        switch(kind){
            case PaperKind.Ledger:{
                bool spread=w/h>1.15f;float block=.06f*a,cover=.012f*a;
                var cloth=new Color(.17f,.25f,.2f);
                Solid(root,"Ledger page block",new Vector3(0,0,under+block/2+.002f*a),new Vector3(w*.985f,h*.98f,block),new Color(.86f,.82f,.7f),stripes:1);
                Solid(root,"Ledger cloth cover",new Vector3(0,0,under+block+cover/2+.002f*a),new Vector3(w*1.05f,h*1.04f,cover),cloth,weave:1,rough:.9f);
                if(spread){var spine=Solid(root,"Ledger spine",new Vector3(0,0,under+block+cover),new Vector3(.08f*a,h*1.04f,.08f*a),cloth*.8f,weave:1,rough:.9f,shape:PrimitiveType.Cylinder);spine.transform.localRotation=Quaternion.identity;spine.transform.localScale=new Vector3(.08f*a,h*.52f,.05f*a);}
                else Solid(root,"Ledger spine",new Vector3(-w*.52f,0,under+block/2+.002f*a),new Vector3(.03f*a,h*1.04f,block+2*cover),cloth*.8f,weave:1,rough:.9f);
                // A ribbon marker hangs from the head of the book.
                Solid(root,"Ledger ribbon",new Vector3(spread?.04f*w:.3f*w,-h*.52f,under+.004f*a),new Vector3(.025f*a,.14f*h,.002f*a),new Color(.5f,.12f,.1f),weave:.4f,rough:.6f);
                break;}
            case PaperKind.Letter:{
                // The envelope it came in, lying under the letter, seen when it is turned over.
                var env=Solid(root,"Envelope",new Vector3(.12f*w,-.18f*h,under+.004f*a),new Vector3(w*.72f,h*.5f,.004f*a),new Color(.83f,.74f,.56f),rough:.9f,fibre:1.4f);
                env.transform.localRotation=Quaternion.Euler(0,0,-7);
                // Flap seam drawn as two thin folds meeting at the point, and the stamp in its corner.
                foreach(float side in new[]{-1f,1f}){var seam=Solid(env.transform,"Envelope flap fold",new Vector3(side*.25f,.3f,1.2f),new Vector3(.56f,.012f,.3f),new Color(.7f,.61f,.44f),rough:.9f);seam.transform.localRotation=Quaternion.Euler(0,0,side*-24);}
                Solid(env.transform,"Envelope stamp",new Vector3(-.36f,.3f,.8f),new Vector3(.12f,.22f,.3f),new Color(.62f,.24f,.2f),rough:.7f);
                break;}
            case PaperKind.Dossier:{
                var kraft=new Color(.72f,.56f,.34f);
                Solid(root,"Folder back board",new Vector3(.02f*w,-.01f*h,under+.006f*a),new Vector3(w*1.1f,h*1.06f,.008f*a),kraft,rough:.9f,fibre:1.8f);
                Solid(root,"Folder tie button",new Vector3(w*.535f,-.01f*h,under+.012f*a),new Vector3(.05f*a,.004f*a,.05f*a),new Color(.35f,.24f,.14f),rough:.6f,shape:PrimitiveType.Cylinder).transform.localRotation=Quaternion.Euler(90,0,0);
                Wire(root,"Folder string",new[]{new Vector3(w*.535f,-.01f*h,under+.01f*a),new Vector3(w*.56f,-.14f*h,under+.014f*a),new Vector3(w*.52f,-.32f*h,under+.012f*a),new Vector3(w*.58f,-.46f*h,under+.01f*a)},.006f*a,new Color(.8f,.74f,.6f),0);
                Clip(root,new Vector3(-w*.32f,h*.5f,-.002f*a),a);
                break;}
            case PaperKind.Form:
                Solid(root,"Second page",new Vector3(.012f*w,-.015f*h,under+.003f*a),new Vector3(w,h,.002f*a),new Color(.9f,.87f,.8f),fibre:1);
                Staple(root,new Vector3(-w*.42f,h*.44f,-.003f*a),a);
                break;
            case PaperKind.Carbon:
                // The flimsy under-copy, a shade bluer from the carbon, pinned with the top sheet.
                Solid(root,"Under copy sheet",new Vector3(-.012f*w,-.016f*h,under+.003f*a),new Vector3(w,h,.002f*a),new Color(.8f,.81f,.88f),rough:.6f,fibre:.6f);
                Clip(root,new Vector3(-w*.3f,h*.5f,-.002f*a),a);
                break;
            case PaperKind.Notice:
                foreach(float side in new[]{-1f,1f})Tack(root,new Vector3(side*w*.44f,h*.45f,-.004f*a),a);
                break;
            case PaperKind.Envelope:
                // Back of the envelope: the flap's V and the side seams, a little darker than the paper.
                foreach(float side in new[]{-1f,1f}){var seam=Solid(root,"Envelope flap seam",new Vector3(side*w*.24f,h*.18f,under+.0015f*a),new Vector3(w*.55f,.006f*a,.002f*a),new Color(.62f,.52f,.36f),rough:.9f);seam.transform.localRotation=Quaternion.Euler(0,0,side*-28);}
                Solid(root,"Envelope bottom seam",new Vector3(0,-h*.12f,under+.0015f*a),new Vector3(w*.96f,.005f*a,.002f*a),new Color(.62f,.52f,.36f),rough:.9f);
                break;
            case PaperKind.Newspaper:
                Solid(root,"Inner pages",new Vector3(.006f*w,-.008f*h,under+.004f*a),new Vector3(w*.995f,h*.99f,.006f*a),new Color(.78f,.76f,.7f),rough:.95f,fibre:1.8f);
                break;
            case PaperKind.Photo:
                // Corner mounts from the album it was taken out of.
                foreach(var c in new[]{new Vector2(-1,1),new Vector2(1,-1)}){var m=Solid(root,"Photo corner mount",new Vector3(c.x*w*.47f,c.y*h*.46f,-.002f*a),new Vector3(.07f*a,.07f*a,.002f*a),new Color(.12f,.1f,.09f),rough:.5f);m.transform.localRotation=Quaternion.Euler(0,0,45);}
                break;
            case PaperKind.NotebookPage:
                Solid(root,"Notebook under-page",new Vector3(.006f*w,-.01f*h,under+.003f*a),new Vector3(w*.99f,h*.97f,.002f*a),new Color(.86f,.84f,.76f),fibre:1);
                break;
            case PaperKind.Receipt:
                Solid(root,"Stub book card",new Vector3(-.03f*w,-.01f*h,under+.004f*a),new Vector3(w*1.02f,h*1.03f,.006f*a),new Color(.55f,.42f,.3f),rough:.85f,fibre:1.6f);
                break;
        }
    }

    // ---- small parts -------------------------------------------------------------------------------
    public static GameObject Solid(Transform parent,string name,Vector3 pos,Vector3 size,Color color,float rough=.85f,float fibre=0,float weave=0,float stripes=0,float metal=0,float leather=0,float brushed=0,PrimitiveType shape=PrimitiveType.Cube) {
        var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=size;
        Object.DestroyImmediate(o.GetComponent<Collider>());
        var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=color;o.GetComponent<Renderer>().sharedMaterial=mat;
        var s=o.AddComponent<EvidenceSurface>();s.rough=rough;s.fibre=fibre;s.weave=weave;s.stripes=stripes;s.metal=metal;s.leather=leather;s.brushed=brushed;s.translucency=0;
        return o;
    }
    static readonly Color Steel=new Color(.62f,.63f,.65f),Brass=new Color(.72f,.55f,.27f);
    public static void Wire(Transform parent,string name,Vector3[] points,float radius,Color color,float metal=1) {
        for(int i=0;i<points.Length-1;i++){var a=points[i];var b=points[i+1];var seg=Solid(parent,name,(a+b)/2,Vector3.one,color,metal>0?.25f:.9f,metal:metal,shape:PrimitiveType.Cylinder);
            seg.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);seg.transform.localScale=new Vector3(radius*2,(b-a).magnitude/2+radius*.6f,radius*2);}
    }
    static void Clip(Transform root,Vector3 at,float a) {
        // Gem paper clip: two nested rounded loops in the sheet plane, half of it behind the paper.
        float L=.16f*a,W=.045f*a,r=.0035f*a;var pts=new List<Vector3>();
        void Loop(float len,float wid,float z,int from,int to){for(int i=from;i<=to;i++){float t=i/24f*Mathf.PI*2;pts.Add(at+new Vector3(Mathf.Sin(t)*wid/2,-(len/2)+Mathf.Cos(t)*len/2,z));}}
        Loop(L,W,0,0,12);Loop(L*.75f,W*.6f,.004f*a,12,24);
        Wire(root,"Paper clip",pts.ToArray(),r,Steel);
    }
    static void Staple(Transform root,Vector3 at,float a) {
        var s=Solid(root,"Staple",at,new Vector3(.07f*a,.008f*a,.004f*a),Steel,.3f,metal:1);s.transform.localRotation=Quaternion.Euler(0,0,35);
    }
    static void Tack(Transform root,Vector3 at,float a) {
        var head=Solid(root,"Brass drawing pin",at,new Vector3(.05f*a,.006f*a,.05f*a),Brass,.3f,metal:1,shape:PrimitiveType.Cylinder);head.transform.localRotation=Quaternion.Euler(90,0,0);
        var dome=Solid(root,"Brass drawing pin dome",at+new Vector3(0,0,-.004f*a),new Vector3(.03f*a,.03f*a,.012f*a),Brass,.25f,metal:1,shape:PrimitiveType.Sphere);
    }
}

// World papers take the same shape as their close-up when a scene loads (the saved prefab sheets stay
// as built; only the scene instance's mesh is swapped, materials and placement untouched).
static class EvidenceWorldDressing {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=Dress;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=Dress;}
    static void Dress(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode){
        int dressed=0;
        foreach(var root in scene.GetRootGameObjects())
            foreach(var physical in root.GetComponentsInChildren<EvidencePhysical>(true)){
                if(!physical.paper)continue;var item=physical.GetComponentInParent<Interactable>(true);
                var kind=EvidenceKinds.For(item?item.evidenceId:null,item?item.title:physical.name);
                EvidenceKinds.DressWorld(physical.gameObject,kind);dressed++;
            }
        if(dressed>0)Debug.Log("EVIDENCE_WORLD_DRESSED: "+scene.name+" "+dressed);
        BindSets(scene);
    }
    // Sheets that are kept together are read as one set (user, 2026-10-19). Keys are document IDs or
    // evidence IDs; a set is bound only when every sheet is present and they lie together.
    static readonly (string[] keys,string container,string label)[] Sets={
        (new[]{"kane-envelope","w01","w02"},"","文件 · 信封与两份抄件"),
        (new[]{"H09","H10"},"folder","文件 · 医疗文件夹"),
        (new[]{"e01-copy","husband-statement","letter-photo"},"folder","文件 · 公开材料"),
    };
    static void BindSets(UnityEngine.SceneManagement.Scene scene){
        var all=new System.Collections.Generic.List<Interactable>();
        foreach(var root in scene.GetRootGameObjects())all.AddRange(root.GetComponentsInChildren<Interactable>(true));
        Interactable Find(string key){
            foreach(var i in all)if(i is PoliceDocumentItem d&&string.Equals(d.documentId,key,System.StringComparison.OrdinalIgnoreCase))return i;
            foreach(var i in all)if(string.Equals(i.evidenceId,key,System.StringComparison.OrdinalIgnoreCase))return i;
            return null;
        }
        foreach(var set in Sets){
            var items=new Interactable[set.keys.Length];bool ok=true;
            for(int k=0;k<items.Length;k++){items[k]=Find(set.keys[k]);if(!items[k]){ok=false;break;}}
            if(!ok)continue;
            float spread=0;foreach(var i in items)spread=Mathf.Max(spread,Vector3.Distance(i.transform.position,items[0].transform.position));
            if(spread>2.5f){Debug.LogWarning("EVIDENCE_SET_SKIPPED: "+string.Join("+",set.keys)+" lie "+spread.ToString("0.0")+" m apart");continue;}
            var pages=new Interactable[items.Length][];for(int k=0;k<items.Length;k++)pages[k]=new[]{items[k]};
            EvidenceBook.Bind(items[0],EvidenceBook.Style.Stack,set.container,set.label,pages);
            Debug.Log("EVIDENCE_SET_BOUND: "+string.Join("+",set.keys));
        }
    }
}
