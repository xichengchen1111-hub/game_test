using UnityEngine;

public enum TownMapSymbol { Clock, Watch, Milk, Tailor, Home, Police, Post, Station, Church, Repair, Inn, Hospital, Shop, Pub, Chess, Pin, Player }

// Small authored geometric symbols and a cached plan image, no new art dependency.
public sealed class TownMapInk {
    readonly int width,height;
    readonly Color32[] pixels;
    public TownMapInk(int w,int h,Color background){width=w;height=h;pixels=new Color32[w*h];for(int i=0;i<pixels.Length;i++)pixels[i]=background;}
    public void Pixel(int x,int y,Color c){if(x>=0&&x<width&&y>=0&&y<height)pixels[(height-1-y)*width+x]=c;}
    public void Rect(float x,float y,float w,float h,Color c){for(int j=Mathf.Max(0,Mathf.FloorToInt(y));j<Mathf.Min(height,Mathf.CeilToInt(y+h));j++)for(int i=Mathf.Max(0,Mathf.FloorToInt(x));i<Mathf.Min(width,Mathf.CeilToInt(x+w));i++)Pixel(i,j,c);}
    public void Disc(float x,float y,float r,Color c){for(int j=Mathf.Max(0,Mathf.FloorToInt(y-r));j<Mathf.Min(height,Mathf.CeilToInt(y+r));j++)for(int i=Mathf.Max(0,Mathf.FloorToInt(x-r));i<Mathf.Min(width,Mathf.CeilToInt(x+r));i++)if((i-x)*(i-x)+(j-y)*(j-y)<=r*r)Pixel(i,j,c);}
    public void Line(Vector2 a,Vector2 b,float thickness,Color c){float length=Vector2.Distance(a,b);for(float t=0;t<=length;t+=.6f) {var p=Vector2.Lerp(a,b,length<.01f?0:t/length);Disc(p.x,p.y,thickness/2,c);}}
    public void Line(float x,float y,float x2,float y2,float t,Color c)=>Line(new Vector2(x,y),new Vector2(x2,y2),t,c);
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    public void Triangle(Vector2 a,Vector2 b,Vector2 c,Color color){float area=Cross(b-a,c-a);if(Mathf.Abs(area)<.001f)return;int x0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,c.x)))),x1=Mathf.Min(width-1,Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,c.x))));int y0=Mathf.Max(0,Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,c.y)))),y1=Mathf.Min(height-1,Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,c.y))));for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){var p=new Vector2(x+.5f,y+.5f);float u=Cross(b-p,c-p)/area,v=Cross(c-p,a-p)/area;if(u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f)Pixel(x,y,color);}}
    public Texture2D Texture(string name){var t=new Texture2D(width,height,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};t.SetPixels32(pixels);t.Apply(false,true);return t;}
    public static Texture2D Symbol(TownMapSymbol symbol){var p=new TownMapInk(96,96,Color.clear);Color ink=Color.white;void L(float x,float y,float a,float b,float t=6)=>p.Line(x,y,a,b,t,ink);void R(float x,float y,float w,float h)=>p.Rect(x,y,w,h,ink);void Ring(float x,float y,float r){p.Disc(x,y,r,ink);p.Disc(x,y,r-6,Color.clear);}void T(float x,float y,float a,float b,float c,float d)=>p.Triangle(new Vector2(x,y),new Vector2(a,b),new Vector2(c,d),ink);
        switch(symbol){
            case TownMapSymbol.Clock: R(29,33,38,49);T(21,34,48,9,75,34);p.Disc(48,45,16,Color.clear);Ring(48,45,14);L(48,45,48,36,4);L(48,45,56,49,4);R(42,65,12,17);break;
            case TownMapSymbol.Watch:Ring(48,52,29);Ring(48,15,8);R(39,20,18,7);L(48,52,48,35);L(48,52,62,60);break;
            case TownMapSymbol.Milk:R(37,12,22,9);L(39,23,39,34);L(57,23,57,34);L(39,34,29,44);L(57,34,67,44);L(29,44,29,81);L(67,44,67,81);L(29,81,67,81);R(32,55,32,16);break;
            case TownMapSymbol.Tailor:Ring(25,69,13);Ring(69,69,13);L(34,58,69,17);L(61,58,26,17);p.Disc(48,43,5,ink);break;
            case TownMapSymbol.Home:L(15,45,48,16);L(48,16,81,45);L(25,38,25,80);L(71,38,71,80);L(25,80,71,80);R(41,57,14,23);R(59,17,9,20);break;
            case TownMapSymbol.Police:T(18,22,48,12,78,22);T(18,22,78,22,48,86);T(18,22,48,86,20,60);T(78,22,76,60,48,86);p.Disc(48,43,17,Color.clear);L(48,27,48,58,5);L(32,43,64,43,5);break;
            case TownMapSymbol.Post:L(14,25,82,25);L(82,25,82,72);L(82,72,14,72);L(14,72,14,25);L(14,25,48,51);L(48,51,82,25);break;
            case TownMapSymbol.Station:Ring(48,37,29);R(22,39,52,27);p.Rect(29,32,38,16,Color.clear);p.Disc(33,57,4,Color.clear);p.Disc(63,57,4,Color.clear);L(31,65,22,83);L(65,65,74,83);L(27,76,69,76);break;
            case TownMapSymbol.Church:L(48,10,48,35);L(36,22,60,22);T(15,52,48,31,81,52);R(23,52,50,30);p.Rect(41,60,14,22,Color.clear);break;
            case TownMapSymbol.Repair:Ring(59,30,19);p.Rect(56,4,40,22,Color.clear);L(47,44,21,76,14);p.Disc(21,76,10,ink);p.Disc(21,76,4,Color.clear);break;
            case TownMapSymbol.Inn:L(17,35,17,79);L(79,46,79,79);L(17,68,79,68);R(21,48,58,16);p.Disc(31,41,9,ink);break;
            case TownMapSymbol.Hospital:R(37,15,22,66);R(15,37,66,22);break;
            case TownMapSymbol.Shop:R(22,39,52,41);T(15,36,29,15,81,36);R(29,15,37,21);p.Rect(40,56,16,24,Color.clear);break;
            case TownMapSymbol.Pub:L(20,19,61,19);L(20,19,20,70);L(20,70,61,70);L(61,70,61,19);L(62,32,79,32);L(79,32,79,58);L(79,58,62,58);L(34,30,34,58,4);L(47,30,47,58,4);break;
            case TownMapSymbol.Chess:p.Disc(48,20,11,ink);T(48,25,29,65,67,65);R(24,71,48,10);break;
            case TownMapSymbol.Pin: p.Disc(48,33,26,ink);T(25,43,71,43,48,88);p.Disc(48,33,10,Color.clear);break;
            case TownMapSymbol.Player:T(48,12,24,81,48,65);T(48,12,48,65,72,81);break;
        }
        return p.Texture("Map "+symbol);
    }
    public static Texture2D PlayerHeading(float degrees){var p=new TownMapInk(96,96,Color.clear);float angle=degrees*Mathf.Deg2Rad;Vector2 V(float x,float y){var d=new Vector2(x-48,y-48);return new Vector2(48+Mathf.Cos(angle)*d.x-Mathf.Sin(angle)*d.y,48+Mathf.Sin(angle)*d.x+Mathf.Cos(angle)*d.y);}p.Triangle(V(48,12),V(24,81),V(48,65),Color.white);p.Triangle(V(48,12),V(48,65),V(72,81),Color.white);return p.Texture("Map player heading");}
    public static Texture2D Route(TownPlan plan,Vector3[] corners,float worldWidth){const int width=2048;var b=plan.bounds;float scale=width/(b[1]-b[0]);var p=new TownMapInk(width,Mathf.RoundToInt((b[3]-b[2])*scale),Color.clear);Vector2 V(Vector3 w)=>new Vector2((w.x-b[0])*scale,(b[3]-w.z)*scale);foreach(bool outline in new[]{true,false})for(int i=1;i<corners.Length;i++)p.Line(V(corners[i-1]),V(corners[i]),worldWidth*scale*(outline?2:1),outline?new Color(.055f,.12f,.14f):new Color(.98f,.75f,.35f));return p.Texture("Map walking route");}
    void PaperGrain(int seed,bool folds){
        var random=new System.Random(seed);
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){
            int index=(height-1-y)*width+x;Color c=pixels[index];if(c.a<.1f)continue;
            float fibre=(float)random.NextDouble()*.035f-.018f;
            float edge=Mathf.Pow(Mathf.Abs(x-width*.5f)/(width*.5f),6)+Mathf.Pow(Mathf.Abs(y-height*.5f)/(height*.5f),6);
            float shade=1+fibre-edge*.065f;
            if(folds){float fold=Mathf.Min(Mathf.Abs(x-width/3f),Mathf.Abs(x-width*2/3f));shade-=Mathf.Exp(-fold/3)*.10f;shade+=Mathf.Exp(-Mathf.Abs(fold-5)/2)*.03f;shade-=Mathf.Exp(-Mathf.Abs(y-height*.5f)/3)*.08f;}
            c.r*=shade;c.g*=shade;c.b*=shade;pixels[index]=c;
        }
    }
    public static Texture2D Paper(){var p=new TownMapInk(1536,1200,new Color(.89f,.83f,.70f));var ink=new Color(.55f,.42f,.26f);p.Line(50,50,1486,50,2,ink);p.Line(50,1150,1486,1150,2,ink);p.Line(50,50,50,1150,2,ink);p.Line(1486,50,1486,1150,2,ink);p.PaperGrain(1931,true);return p.Texture("Creased linen paper");}
    public static Texture2D Plan(TownPlan plan){
        const int width=2048;float[] b=plan.bounds;float scale=width/(b[1]-b[0]);int height=Mathf.RoundToInt((b[3]-b[2])*scale);var p=new TownMapInk(width,height,new Color(.88f,.82f,.695f));
        Vector2 V(float x,float z)=>new Vector2((x-b[0])*scale,(b[3]-z)*scale);
        void Rectangle(float[] r,Color c){var v=V(r[0]-r[2]/2,r[1]+r[3]/2);p.Rect(v.x,v.y,r[2]*scale,r[3]*scale,c);}
        foreach(var land in plan.land)Rectangle(land.rect,new Color(.80f,.78f,.61f));
        foreach(var surface in plan.surfaces){var v=surface.vertices;Color c=surface.material=="road"?new Color(.77f,.71f,.595f):surface.material=="walk"?new Color(.93f,.875f,.745f):new Color(.865f,.80f,.675f);for(int i=0;i+8<v.Length;i+=9)p.Triangle(V(v[i],v[i+2]),V(v[i+3],v[i+5]),V(v[i+6],v[i+8]),c);}
        var ink=new Color(.39f,.31f,.215f);
        foreach(var road in plan.roads){var a=V(road.points[0],road.points[1]);var c=V(road.points[road.points.Length-2],road.points[road.points.Length-1]);var direction=(c-a).normalized;var normal=new Vector2(-direction.y,direction.x)*road.lane*scale*.5f;p.Line(a+normal,c+normal,1.3f,ink);p.Line(a-normal,c-normal,1.3f,ink);}
        for(float x=Mathf.Ceil(b[0]/50)*50;x<=b[1];x+=50)p.Line(V(x,b[2]),V(x,b[3]),1,new Color(.75f,.69f,.56f));
        for(float z=Mathf.Ceil(b[2]/50)*50;z<=b[3];z+=50)p.Line(V(b[0],z),V(b[1],z),1,new Color(.75f,.69f,.56f));
        foreach(var tree in plan.trees){var v=V(tree.x,tree.z);p.Disc(v.x,v.y,tree.r*scale,new Color(.51f,.56f,.35f));p.Disc(v.x,v.y,Mathf.Max(0,tree.r*scale-1.6f),new Color(.76f,.755f,.55f));}
        void House(float[] r){Rectangle(r,new Color(.76f,.68f,.54f));var a=V(r[0]-r[2]/2,r[1]+r[3]/2);float w=r[2]*scale,h=r[3]*scale;p.Line(a,a+Vector2.right*w,1.8f,ink);p.Line(a,a+Vector2.up*h,1.8f,ink);p.Line(a+new Vector2(w,h),a+new Vector2(w,0),1.8f,ink);p.Line(a+new Vector2(w,h),a+new Vector2(0,h),1.8f,ink);for(float x=3;x<w;x+=13)p.Line(a+new Vector2(x,2),a+new Vector2(Mathf.Min(w-2,x+h-4),Mathf.Min(h-2,w-x)),1,new Color(.56f,.45f,.315f));}
        foreach(var building in plan.buildings)foreach(var part in building.parts)if(!(building.id=="B04"&&part==building.parts[0]))House(part.rect);
        House(plan.milkRect);p.PaperGrain(1931,true);return p.Texture("Ink and hatching town plan");
    }
}
