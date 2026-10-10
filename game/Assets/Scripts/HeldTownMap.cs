using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public struct HeldMapLandmark {public string name;public Vector3 point;public TownMapSymbol symbol;}

// A real held sheet: its pose follows the body, while the eye can turn independently.
public sealed class HeldTownMap : MonoBehaviour {
    public const int ViewLayer=30;
    public static readonly Rect MapArea=new Rect(-.49f,-.335f,.98f,.662f);
    public Camera ViewCamera {get;private set;}
    public Transform Sheet {get;private set;}
    public float Zoom {get;private set;}=1;
    public bool Visible=>shown&&reveal>.9f;
    public Vector2 Centre {get;private set;}
    PlayerMotor player;
    CharacterController controller;
    Font mapFont;
    HeldMapLandmark[] landmarks;
    Material inkTemplate,textInk,routeInk;
    readonly List<Material> ownedMaterials=new List<Material>();
    readonly List<Mesh> ownedMeshes=new List<Mesh>();
    readonly List<Texture2D> ownedTextures=new List<Texture2D>();
    readonly Dictionary<TownMapSymbol,Material> symbolMaterials=new Dictionary<TownMapSymbol,Material>();
    readonly List<PrintedText> textItems=new List<PrintedText>();
    Transform[] landmarkIcons,landmarkLabels;
    Transform mapFace,playerArrow,pin;
    Mesh routeMesh,mapFaceMesh;
    Camera baseCamera;
    int previousMask;
    bool shown,fontDirty,viewDirty;
    float reveal;
    Vector3[] route=Array.Empty<Vector3>();
    Vector3 waypoint;
    bool hasWaypoint;
    sealed class PrintedText {public string text;public float height;public Transform root;public Mesh mesh;}

    public static HeldTownMap Create(PlayerMotor motor,Font font,HeldMapLandmark[] points){
        var go=new GameObject("Held paper map");go.transform.SetParent(motor.transform,false);
        var view=go.AddComponent<HeldTownMap>();view.Initialize(motor,font,points);return view;
    }
    void Initialize(PlayerMotor motor,Font font,HeldMapLandmark[] points){
        player=motor;controller=motor.GetComponent<CharacterController>();mapFont=font;landmarks=points;
        inkTemplate=Resources.Load<Material>("HeldMapInk");if(!inkTemplate)throw new InvalidOperationException("Held paper map material missing; rebuild with BuildSlice");
        baseCamera=player.eye;previousMask=baseCamera.cullingMask;baseCamera.cullingMask&=~(1<<ViewLayer);
        var cameraObject=new GameObject("Held item camera");cameraObject.transform.SetParent(baseCamera.transform,false);
        ViewCamera=cameraObject.AddComponent<Camera>();ViewCamera.CopyFrom(baseCamera);ViewCamera.cullingMask=1<<ViewLayer;ViewCamera.nearClipPlane=.01f;ViewCamera.farClipPlane=3;ViewCamera.enabled=false;
        var cameraData=ViewCamera.GetUniversalAdditionalCameraData();cameraData.renderType=CameraRenderType.Overlay;cameraData.renderPostProcessing=false;cameraData.renderShadows=false;cameraData.requiresDepthTexture=false;
        baseCamera.GetUniversalAdditionalCameraData().cameraStack.Add(ViewCamera);
        Sheet=Child("Folded sheet",transform);
        var paperTexture=TownMapInk.Paper();ownedTextures.Add(paperTexture);
        var paper=NewMaterial(new Color(1,1,1),paperTexture,false);
        var sheetMesh=BuildSheet();Renderer("Paper body",Sheet,sheetMesh,paper);
        var back=NewMaterial(new Color(.61f,.48f,.31f),null,false);var backMesh=BuildSheet();var backRoot=Renderer("Paper thickness",Sheet,backMesh,back);backRoot.localPosition=Vector3.forward*.0018f;backRoot.localScale=new Vector3(1.006f,1.006f,1);
        var planTexture=TownMapInk.Plan(TownPlan.Current);ownedTextures.Add(planTexture);
        mapFaceMesh=QuadMesh(MapArea.width,MapArea.height);mapFace=Renderer("Printed town plan",Sheet,mapFaceMesh,NewMaterial(Color.white,planTexture,false));mapFace.localPosition=new Vector3(MapArea.center.x,MapArea.center.y,-.001f);
        routeInk=NewMaterial(new Color(.49f,.16f,.105f),null,true);routeInk.renderQueue=3001;
        routeMesh=new Mesh{name="Pencil walking route"};ownedMeshes.Add(routeMesh);Renderer("Walking route",Sheet,routeMesh,routeInk);
        textInk=NewMaterial(new Color(.26f,.19f,.12f),null,true);textInk.renderQueue=3004;
        Print("青川镇街道图",new Vector2(0,.375f),.038f);
        Print("一九三一 · 钟楼为原点 · 东 X / 北 Z · 米",new Vector2(0,.34f),.014f);
        Print("青川镇绘图 · 北为上 · 坐标单位：米",new Vector2(0,-.397f),.016f);
        Print("北",new Vector2(.443f,.293f),.018f);var north=Symbol("North rose",TownMapSymbol.Player,.020f,new Color(.29f,.21f,.13f));north.localPosition=new Vector3(.443f,.266f,-.006f);
        landmarkIcons=new Transform[landmarks.Length];landmarkLabels=new Transform[landmarks.Length];
        for(int i=0;i<landmarks.Length;i++){
            var item=landmarks[i];landmarkIcons[i]=Symbol(item.name+" icon",item.symbol,item.symbol==TownMapSymbol.Clock?.039f:.031f,new Color(.28f,.22f,.145f));
            string label=item.name.Replace("警署资料室","警署").Replace("裁缝商住","裁缝").Replace("街角杂货铺","杂货铺").Replace("本镇教堂","教堂");
            landmarkLabels[i]=Print(label,Vector2.zero,.015f);
        }
        playerArrow=Symbol("Live player position",TownMapSymbol.Player,.026f,new Color(.13f,.34f,.37f));
        playerArrow.GetComponent<MeshRenderer>().sharedMaterial=NewMaterial(new Color(.13f,.34f,.37f),symbolMaterials[TownMapSymbol.Player].mainTexture,true);
        pin=Symbol("Coordinate pin",TownMapSymbol.Pin,.036f,new Color(.59f,.12f,.075f));pin.gameObject.SetActive(false);
        ResetView();Font.textureRebuilt+=FontRebuilt;fontDirty=true;
    }
    Transform Child(string name,Transform parent){var go=new GameObject(name){layer=ViewLayer};go.transform.SetParent(parent,false);return go.transform;}
    Material NewMaterial(Color tint,Texture texture,bool transparent,float shade=0){
        var m=new Material(inkTemplate){name="Held map material",renderQueue=transparent?3002:2000};m.SetColor("_Color",tint);m.SetFloat("_Shade",shade);m.SetFloat("_ZWrite",transparent?0:1);m.SetFloat("_SrcBlend",transparent?(float)BlendMode.SrcAlpha:1);m.SetFloat("_DstBlend",transparent?(float)BlendMode.OneMinusSrcAlpha:0);if(texture)m.SetTexture("_MainTex",texture);ownedMaterials.Add(m);return m;
    }
    Transform Renderer(string name,Transform parent,Mesh mesh,Material material){var t=Child(name,parent);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;return t;}
    Mesh QuadMesh(float width,float height){var mesh=new Mesh{name="Paper print quad"};mesh.vertices=new[]{new Vector3(-width/2,-height/2),new Vector3(width/2,-height/2),new Vector3(-width/2,height/2),new Vector3(width/2,height/2)};mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,2,3,1};mesh.RecalculateNormals();ownedMeshes.Add(mesh);return mesh;}
    Mesh BuildSheet(){
        const int columns=14,rows=12;var v=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
        for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++){
            float u=x/(float)columns,t=y/(float)rows,px=(u-.5f)*1.10f,py=(t-.5f)*.86f;
            float edge=Mathf.Clamp01((Mathf.Abs(px)-.485f)/.065f)+Mathf.Clamp01((Mathf.Abs(py)-.335f)/.095f);
            float curl=-.004f*edge*(1+Mathf.Sin(u*12+t*9));
            if(x==0||x==columns)px+=Mathf.Sin(y*3.8f)*.0018f;
            if(y==0||y==rows)py+=Mathf.Sin(x*4.1f)*.0016f;
            v.Add(new Vector3(px,py,curl));uv.Add(new Vector2(u,t));
            if(x<columns&&y<rows){int i=y*(columns+1)+x;indices.AddRange(new[]{i,i+columns+1,i+1,i+1,i+columns+1,i+columns+2});}
        }
        var mesh=new Mesh{name="Slightly curled paper sheet"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();ownedMeshes.Add(mesh);return mesh;
    }
    Transform Symbol(string name,TownMapSymbol symbol,float size,Color color){
        // Each symbol has one material/texture; tint is authored per symbol type.
        if(!symbolMaterials.TryGetValue(symbol,out var m)){var texture=TownMapInk.Symbol(symbol);ownedTextures.Add(texture);m=NewMaterial(color,texture,true);m.renderQueue=3003;symbolMaterials.Add(symbol,m);}
        return Renderer(name,Sheet,QuadMesh(size,size),m);
    }
    Transform Print(string value,Vector2 position,float height){var mesh=new Mesh{name="Printed "+value};ownedMeshes.Add(mesh);var root=Renderer(value,Sheet,mesh,textInk);root.localPosition=new Vector3(position.x,position.y,-.008f);textItems.Add(new PrintedText{text=value,height=height,root=root,mesh=mesh});return root;}
    void FontRebuilt(Font changed){if(changed==mapFont)fontDirty=true;}
    void UpdateType(){
        string characters="";foreach(var item in textItems)characters+=item.text;
        mapFont.RequestCharactersInTexture(characters,64,FontStyle.Normal);textInk.SetTexture("_MainTex",mapFont.material.mainTexture);
        foreach(var item in textItems){var v=new List<Vector3>();var uv=new List<Vector2>();var ids=new List<int>();float advance=0;
            foreach(char c in item.text)if(mapFont.GetCharacterInfo(c,out var glyph,64))advance+=glyph.advance;
            float scale=item.height/64,pen=-advance*.5f;
            foreach(char c in item.text){if(!mapFont.GetCharacterInfo(c,out var g,64))continue;int n=v.Count;float y=-22;
                v.AddRange(new[]{new Vector3((pen+g.minX)*scale,(g.minY+y)*scale),new Vector3((pen+g.maxX)*scale,(g.minY+y)*scale),new Vector3((pen+g.minX)*scale,(g.maxY+y)*scale),new Vector3((pen+g.maxX)*scale,(g.maxY+y)*scale)});
                uv.AddRange(new[]{g.uvBottomLeft,g.uvBottomRight,g.uvTopLeft,g.uvTopRight});ids.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});pen+=g.advance;
            }
            item.mesh.Clear();item.mesh.SetVertices(v);item.mesh.SetUVs(0,uv);item.mesh.SetTriangles(ids,0);item.mesh.RecalculateNormals();
        }
        fontDirty=false;
    }
    public void SetShown(bool value){shown=value;}
    void LateUpdate(){
        reveal=Mathf.MoveTowards(reveal,shown?1:0,Time.unscaledDeltaTime*4.5f);ViewCamera.enabled=reveal>.005f;
        if(!ViewCamera.enabled)return;
        float reading=Mathf.SmoothStep(0,1,Mathf.InverseLerp(12,48,player.pitch));
        float motion=controller.velocity.magnitude>.15f?1:0;
        float bob=Mathf.Sin(Time.time*6.8f)*.004f*motion;
        Sheet.localPosition=Vector3.Lerp(new Vector3(0,1.06f,.70f),new Vector3(0,1.13f,.60f),reading)+new Vector3(Mathf.Sin(Time.time*3.4f)*.003f*motion,bob-(1-reveal)*.66f,0);
        Sheet.localRotation=Quaternion.Euler(64+Mathf.Sin(Time.time*6.8f)*.4f*motion,0,Mathf.Sin(Time.time*3.4f)*.55f*motion);
        ViewCamera.fieldOfView=baseCamera.fieldOfView;
        if(fontDirty)UpdateType();if(viewDirty)UpdateView();
        var here=WorldToPaper(player.transform.position);playerArrow.gameObject.SetActive(MapArea.Contains(here));playerArrow.localPosition=new Vector3(here.x,here.y,-.012f);playerArrow.localRotation=Quaternion.Euler(0,0,-player.transform.eulerAngles.y);
    }
    public void ResetView(){var b=TownPlan.Current.bounds;Centre=new Vector2((b[0]+b[1])/2,(b[2]+b[3])/2);Zoom=1;viewDirty=true;}
    public Vector2 WorldToPaper(Vector3 world){var b=TownPlan.Current.bounds;return MapArea.center+new Vector2((world.x-Centre.x)/(b[1]-b[0])*MapArea.width,(world.z-Centre.y)/(b[3]-b[2])*MapArea.height)*Zoom;}
    public Vector3 PaperToWorld(Vector2 local){var b=TownPlan.Current.bounds;var p=(local-MapArea.center)/Zoom;return new Vector3(Centre.x+p.x/MapArea.width*(b[1]-b[0]),.1f,Centre.y+p.y/MapArea.height*(b[3]-b[2]));}
    public bool TryPaperLocal(Vector2 screen,out Vector2 local){
        local=Vector2.zero;if(!Visible||screen.x<0||screen.x>Screen.width||screen.y<0||screen.y>Screen.height)return false;
        var ray=ViewCamera.ScreenPointToRay(screen);var plane=new Plane(Sheet.forward,Sheet.TransformPoint(new Vector3(0,0,-.001f)));
        if(!plane.Raycast(ray,out float distance)||distance<0||distance>3)return false;
        var point=Sheet.InverseTransformPoint(ray.GetPoint(distance));local=new Vector2(point.x,point.y);
        return Mathf.Abs(local.x)<=.55f&&Mathf.Abs(local.y)<=.43f;
    }
    public Vector2 PaperToScreen(Vector2 local){var p=ViewCamera.WorldToScreenPoint(Sheet.TransformPoint(new Vector3(local.x,local.y,-.001f)));return new Vector2(p.x,p.y);}
    public bool PinContains(Vector2 local,Vector3 point){var p=WorldToPaper(point);return new Rect(p.x-.02f,p.y-.004f,.04f,.040f).Contains(local);}
    public void ZoomAt(float factor,Vector2 screen){if(!TryPaperLocal(screen,out var local)||!MapArea.Contains(local))return;var anchor=PaperToWorld(local);Zoom=Mathf.Clamp(Zoom*factor,1,4);var after=PaperToWorld(local);Centre+=new Vector2(anchor.x-after.x,anchor.z-after.z);ClampCentre();viewDirty=true;}
    public void Pan(Vector2 delta){var b=TownPlan.Current.bounds;Centre-=new Vector2(delta.x/MapArea.width*(b[1]-b[0]),delta.y/MapArea.height*(b[3]-b[2]))/Zoom;ClampCentre();viewDirty=true;}
    void ClampCentre(){var b=TownPlan.Current.bounds;float x=(b[1]-b[0])/(2*Zoom),z=(b[3]-b[2])/(2*Zoom);Centre=new Vector2(Mathf.Clamp(Centre.x,b[0]+x,b[1]-x),Mathf.Clamp(Centre.y,b[2]+z,b[3]-z));}
    public void SetNavigation(bool active,Vector3 target,Vector3[] points){hasWaypoint=active;waypoint=target;route=points;viewDirty=true;}
    void UpdateView(){
        var b=TownPlan.Current.bounds;float u=(Centre.x-b[0])/(b[1]-b[0]),v=(Centre.y-b[2])/(b[3]-b[2]),half=.5f/Zoom;
        mapFaceMesh.uv=new[]{new Vector2(u-half,v-half),new Vector2(u+half,v-half),new Vector2(u-half,v+half),new Vector2(u+half,v+half)};
        for(int i=0;i<landmarks.Length;i++){var p=WorldToPaper(landmarks[i].point);bool visible=MapArea.Contains(p);landmarkIcons[i].gameObject.SetActive(visible);landmarkIcons[i].localPosition=new Vector3(p.x,p.y,-.007f);float offset=landmarks[i].symbol==TownMapSymbol.Clock?.030f:landmarks[i].point.z>26&&landmarks[i].point.z<36?(i%2==0?.028f:.041f):landmarks[i].name.Contains("邻居")?-.046f:-.026f;landmarkLabels[i].localPosition=new Vector3(p.x,p.y+offset,-.008f);landmarkLabels[i].gameObject.SetActive(visible&&p.x>MapArea.xMin+.035f&&p.x<MapArea.xMax-.035f&&p.y+offset>MapArea.yMin+.012f&&p.y+offset<MapArea.yMax-.012f);}
        var target=WorldToPaper(waypoint);pin.gameObject.SetActive(hasWaypoint&&MapArea.Contains(target));pin.localPosition=new Vector3(target.x,target.y+.015f,-.013f);
        RebuildRoute();viewDirty=false;
    }
    void RebuildRoute(){var vertices=new List<Vector3>();var ids=new List<int>();for(int i=1;i<route.Length;i++){
        var a=WorldToPaper(route[i-1]);var b=WorldToPaper(route[i]);if(!Clip(ref a,ref b))continue;var d=b-a;if(d.sqrMagnitude<1e-10f)continue;var n=new Vector2(-d.y,d.x).normalized*.00115f;int start=vertices.Count;
        vertices.AddRange(new[]{new Vector3(a.x+n.x,a.y+n.y,-.004f),new Vector3(a.x-n.x,a.y-n.y,-.004f),new Vector3(b.x+n.x,b.y+n.y,-.004f),new Vector3(b.x-n.x,b.y-n.y,-.004f)});ids.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3});
    }routeMesh.Clear();routeMesh.SetVertices(vertices);routeMesh.SetTriangles(ids,0);routeMesh.RecalculateNormals();}
    static bool Clip(ref Vector2 a,ref Vector2 b){var d=b-a;float low=0,high=1;bool Edge(float p,float q){if(Mathf.Abs(p)<1e-7f)return q>=0;float r=q/p;if(p<0)low=Mathf.Max(low,r);else high=Mathf.Min(high,r);return low<=high;}if(!Edge(-d.x,a.x-MapArea.xMin)||!Edge(d.x,MapArea.xMax-a.x)||!Edge(-d.y,a.y-MapArea.yMin)||!Edge(d.y,MapArea.yMax-a.y))return false;b=a+d*high;a+=d*low;return true;}
    void OnDestroy(){Font.textureRebuilt-=FontRebuilt;if(baseCamera){baseCamera.cullingMask=previousMask;if(ViewCamera)baseCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(ViewCamera);}if(ViewCamera)Destroy(ViewCamera.gameObject);foreach(var m in ownedMaterials)Destroy(m);foreach(var m in ownedMeshes)Destroy(m);foreach(var t in ownedTextures)Destroy(t);}
}
