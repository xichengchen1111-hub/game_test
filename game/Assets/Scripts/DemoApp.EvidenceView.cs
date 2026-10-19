using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public partial class DemoApp {
    public Mesh intactTicketStock,punchedTicketStock;
    GameObject evidenceStage,evidenceObject;Camera evidenceCamera;RenderTexture evidenceRender;
    readonly HashSet<Material> evidenceOwnedMaterials=new HashSet<Material>();
    readonly HashSet<Mesh> evidenceOwnedMeshes=new HashSet<Mesh>();
    bool evidenceDetails,evidenceDragging;Vector2 evidenceScroll,evidenceLastMouse;float evidenceZoom=1,evidenceZoomTarget=1,evidenceEnterTime;
    Quaternion evidenceRestRotation,evidenceTargetRotation;
    string evidenceKindLabel="";Texture2D evidenceBackdrop;
    // Clickable areas in GUI units; a click there opens or closes the text instead of turning the model.
    Rect evidenceOpenRect,evidenceCloseRect,evidencePrevRect,evidenceNextRect;
    // Bound documents (EvidenceBook): the open page, and the leaf being turned.
    EvidenceBook evidenceBook;int evidencePage,evidenceFlipMode;Transform evidenceFlipHinge;float evidenceFlipStart,evidenceFlipFrom,evidenceFlipTo;GameObject evidenceFlipReveal;
    bool ModelEvidence=>inspected&&(!string.IsNullOrEmpty(inspected.evidenceId)||evidenceBook)&&!(inspected is HouseContainer)&&!(inspected is ClockTowerAdjustment)&&!DialogueOpen;
    void BeginEvidenceModel(Interactable item){
        EndEvidenceModel();evidenceDetails=false;evidenceScroll=Vector2.zero;evidenceZoom=evidenceZoomTarget=RestZoom;evidenceEnterTime=Time.unscaledTime;
        evidenceStage=new GameObject("Evidence observation stage");evidenceStage.transform.position=new Vector3(0,-500,0);
        var cam=new GameObject("Evidence observation camera");cam.transform.SetParent(evidenceStage.transform,false);cam.transform.localPosition=new Vector3(0,0,-3.1f);
        evidenceCamera=cam.AddComponent<Camera>();evidenceCamera.clearFlags=CameraClearFlags.SolidColor;evidenceCamera.backgroundColor=Color.clear;evidenceCamera.cullingMask=1<<31;
        evidenceCamera.orthographic=false;evidenceCamera.fieldOfView=30;evidenceCamera.nearClipPlane=.05f;evidenceCamera.farClipPlane=8;evidenceCamera.allowMSAA=true;evidenceCamera.allowHDR=false;
        var data=evidenceCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
        ResizeEvidenceRender();
        var payload=EvidencePayload(item,out bool paper,out bool ownMeshes);
        evidenceObject=StudyPivot(payload,evidenceStage.transform,paper,ownMeshes,evidenceOwnedMaterials,evidenceOwnedMeshes,"Examined evidence pivot");
        evidenceRestRotation=StudyRest(paper);evidenceTargetRotation=evidenceRestRotation;evidenceObject.transform.localRotation=evidenceRestRotation;
        foreach(var t in evidenceStage.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        focus=null;UpdateEvidencePose(true);
    }
    // The object as it is examined: the actual ticket stock, a solid model, a clone of the paper in the
    // world, or a paper made from its image. Shared by the inspection and the briefcase preview.
    GameObject EvidencePayload(Interactable item,out bool paper,out bool ownMeshes) {
        var physical=item.GetComponentInChildren<EvidencePhysical>();ownMeshes=!physical;GameObject payload;
        if(evidenceBook&&evidenceBook.Page(evidencePage)==item){
            // One bound book, open at the current page; every page keeps its own printed face.
            int n=evidenceBook.Count;var faces=new Texture2D[n];var backs=new Texture2D[n];float bw=0,bh=0;
            for(int i=0;i<n;i++){faces[i]=PaperFace(evidenceBook.Page(i),out float pw,out float ph,out backs[i]);if(bw<=0){bw=pw;bh=ph;}}
            if(evidenceBook.style==EvidenceBook.Style.Stack){
                // Loose sheets keep their own sizes; a sheet with no paper in the world takes the width of one that has.
                var sizes=new Vector2[n];var kinds=new PaperKind[n];float refW=0;
                for(int i=0;i<n;i++)if(evidenceBook.Page(i).GetComponentInChildren<EvidencePhysical>(true)){PaperFace(evidenceBook.Page(i),out refW,out _,out _);break;}
                if(refW<=0)refW=.21f;
                for(int i=0;i<n;i++){var p=evidenceBook.Page(i);PaperFace(p,out float pw,out float ph,out _);bool hasPaper=p.GetComponentInChildren<EvidencePhysical>(true);
                    sizes[i]=hasPaper?new Vector2(pw,ph):new Vector2(refW,refW*ph/Mathf.Max(.001f,pw));kinds[i]=EvidenceKinds.For(p.evidenceId,p.title);}
                payload=EvidenceKinds.BuildStack(faces,backs,sizes,kinds,evidencePage,evidenceFlipMode,evidenceBook.container);
            }else payload=EvidenceKinds.BuildBook(faces,backs,evidencePage,evidenceFlipMode,bw,bh);
            ownMeshes=true;paper=true;evidenceKindLabel=string.IsNullOrEmpty(evidenceBook.label)?"文件 · 簿册":evidenceBook.label;
            return payload;
        }
        bool ticket=(item.evidenceId=="E02"||item.evidenceId=="E03")&&intactTicketStock&&punchedTicketStock;
        bool solid=EvidenceSolidModel.Handles(item.evidenceId);
        if(ticket){
            payload=new GameObject("Actual ticket stock");payload.AddComponent<MeshFilter>().sharedMesh=item.evidenceId=="E03"?punchedTicketStock:intactTicketStock;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetTexture("_BaseMap",TicketImage(item.evidenceId));payload.AddComponent<MeshRenderer>().sharedMaterial=mat;
            payload.transform.localRotation=Quaternion.Euler(-90,0,0);
        }else if(solid){EvidenceSolidModel.Dial=dialTexture;payload=EvidenceSolidModel.Create(item.evidenceId,item.illustration,Minutes);}
        else if(physical&&!physical.paper){payload=Instantiate(physical.gameObject);foreach(var c in payload.GetComponentsInChildren<Collider>())Destroy(c);foreach(var c in payload.GetComponentsInChildren<MonoBehaviour>())Destroy(c);payload.transform.localRotation=Quaternion.identity;}
        else{
            // Paper is rebuilt as what it physically is (folded letter, torn note, stub, open register,
            // newspaper, photograph...), with the same printed face and the proportions of the sheet in the world.
            var kind=EvidenceKinds.For(item.evidenceId,item.title);
            var faceTexture=PaperFace(item,out float w,out float h,out var reverseTexture);
            payload=EvidenceKinds.Build(kind,"Observed "+kind,faceTexture,reverseTexture,w,h);ownMeshes=true;
            evidenceKindLabel=EvidenceKinds.Label(kind);
        }
        if(ticket)evidenceKindLabel="文件 · 车票";else if(solid||(physical&&!physical.paper))evidenceKindLabel="物品";
        paper=ticket||(physical&&physical.paper)||!solid;
        return payload;
    }
    // A sheet's printed face, its size in the world (or the image's proportions) and its printed reverse.
    Texture2D PaperFace(Interactable item,out float w,out float h,out Texture2D reverse) {
        var physical=item.GetComponentInChildren<EvidencePhysical>(true);
        var face=item.illustration?item.illustration:TicketImage(item.evidenceId);
        w=1;h=face?face.height/(float)face.width:.75f;
        var worldFace=physical?physical.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(f=>f.name=="Paper printed face"):null;
        if(worldFace&&worldFace.sharedMesh){var b=worldFace.sharedMesh.bounds;if(b.size.x>.01f&&b.size.y>.01f){w=b.size.x;h=b.size.y;}
            var worldMat=worldFace.GetComponent<MeshRenderer>().sharedMaterial;var worldTexture=worldMat&&worldMat.HasProperty("_BaseMap")?worldMat.GetTexture("_BaseMap") as Texture2D:null;if(worldTexture)face=worldTexture;}
        reverse=item.reverse;
        if(!reverse&&physical){var back=physical.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Paper reverse");if(back&&back.sharedMaterial&&back.sharedMaterial.HasProperty("_BaseMap"))reverse=back.sharedMaterial.GetTexture("_BaseMap") as Texture2D;}
        return face;
    }
    // Turn to the next (dir 1) or previous (-1) page of the open book: the leaf swings over the spine,
    // the view keeps its angle and zoom, and the new page is read (and recorded) like opening that sheet.
    bool TurnEvidencePage(int dir) {
        if(!evidenceBook||evidenceFlipHinge||!evidenceObject)return false;
        int next=evidencePage+dir;if(next<0||next>=evidenceBook.Count)return false;
        var target=evidenceTargetRotation;var current=evidenceObject.transform.localRotation;float zoom=evidenceZoomTarget;bool details=evidenceDetails;var scroll=evidenceScroll;
        evidencePage=next;var page=evidenceBook.Page(next);
        evidenceFlipMode=dir;EndEvidenceModel();inspected=page;BeginEvidenceModel(page);evidenceFlipMode=0;
        evidenceTargetRotation=target;evidenceObject.transform.localRotation=current;evidenceZoomTarget=evidenceZoom=zoom;evidenceDetails=details;evidenceScroll=Vector2.zero;evidenceEnterTime=-10;
        evidenceFlipHinge=evidenceObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Turning leaf hinge"||t.name=="Sliding sheet");
        // Sliding a sheet back onto the stack: the top copy appears only when it lands.
        evidenceFlipReveal=null;if(dir<0&&evidenceFlipHinge&&evidenceFlipHinge.GetComponent<EvidenceSlide>()){var top=evidenceObject.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Stack top sheet");if(top){evidenceFlipReveal=top.gameObject;top.gameObject.SetActive(false);}}
        evidenceFlipFrom=dir>0?0:180;evidenceFlipTo=dir>0?180:0;evidenceFlipStart=Time.unscaledTime;
        if(page is PoliceDocumentItem doc&&doc.paperAudio)doc.paperAudio.Play();
        RecordInspected(page);
        return true;
    }
    void DrawBookPager() {
        if(!evidenceBook){evidencePrevRect=evidenceNextRect=default;return;}
        float cx=evidenceDetails?W*.29f:W*.42f,y=H-124;
        var centre=new GUIStyle(small){fontSize=17,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(.82f,.8f,.74f,.9f)}};
        bool stack=evidenceBook.style==EvidenceBook.Style.Stack;
        Label(new Rect(cx-90,y,180,40),"第 "+(evidencePage+1)+" / "+evidenceBook.Count+(stack?" 张":" 页"),centre);
        evidencePrevRect=evidencePage>0?new Rect(cx-240,y,130,40):default;evidenceNextRect=evidencePage<evidenceBook.Count-1?new Rect(cx+110,y,130,40):default;
        if(evidencePrevRect.width>0&&Button(evidencePrevRect,stack?"‹  上一张":"‹  上一页"))TurnEvidencePage(-1);
        if(evidenceNextRect.width>0&&Button(evidenceNextRect,stack?"下一张  ›":"下一页  ›",true))TurnEvidencePage(1);
    }
    // Centres and scales the payload on a pivot under the stage and gives it the study materials.
    GameObject StudyPivot(GameObject payload,Transform stage,bool paper,bool ownMeshes,HashSet<Material> ownedMaterials,HashSet<Mesh> ownedMeshes,string name) {
        // Centre the complete model before rotating, including imported meshes with an offset origin.
        payload.transform.SetParent(stage,false);payload.transform.localPosition=Vector3.zero;
        var bounds=new Bounds();bool first=true;foreach(var r in payload.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);var centre=stage.InverseTransformPoint(bounds.center);
        var pivot=new GameObject(name);pivot.transform.SetParent(stage,false);payload.transform.SetParent(pivot.transform,true);payload.transform.localPosition-=centre;
        payload.transform.localScale*=1.12f/Mathf.Max(.01f,size);payload.transform.localPosition*=1.12f/Mathf.Max(.01f,size);
        var shader=Resources.Load<Shader>("EvidenceStudy");
        foreach(var r in payload.GetComponentsInChildren<Renderer>()){
            var old=r.sharedMaterials;var materials=new Material[old.Length];
            for(int i=0;i<old.Length;i++){
                var m=new Material(shader);m.SetColor("_BaseColor",old[i].HasProperty("_BaseColor")?old[i].GetColor("_BaseColor"):Color.white);
                if(old[i].HasProperty("_BaseMap"))m.SetTexture("_BaseMap",old[i].GetTexture("_BaseMap"));
                var surface=r.GetComponent<EvidenceSurface>();
                if(surface){
                    // Built parts carry their own stock: paper fibre, laid lines, cloth, leather, brass.
                    bool isPaper=surface.metal<=0&&(surface.fibre>0||surface.laid>0||surface.translucency>0||surface.newsprint>0||surface.gloss>0);
                    m.SetFloat("_Paper",isPaper?1:0);m.SetFloat("_Metallic",surface.metal);m.SetFloat("_Rough",surface.rough);m.SetFloat("_Fibre",surface.fibre);m.SetFloat("_Laid",surface.laid);
                    m.SetFloat("_Weave",surface.weave);m.SetFloat("_Stripes",surface.stripes);m.SetFloat("_Leather",surface.leather);m.SetFloat("_Brushed",surface.brushed);m.SetFloat("_Newsprint",surface.newsprint);
                    m.SetFloat("_Translucency",surface.translucency);m.SetFloat("_Gloss",surface.gloss);m.SetFloat("_Age",surface.age);
                    // Glass: premultiplied blend after the opaque parts, dust on its inner face.
                    if(surface.glass>0){m.SetFloat("_Glass",surface.glass);m.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.One);m.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWriteOn",0);m.renderQueue=3000;}
                    // An unprinted reverse shows the face's writing faintly through thin stock.
                    if(r.name=="Paper reverse"&&!(old[i].HasProperty("_BaseMap")&&old[i].GetTexture("_BaseMap"))&&surface.translucency>.2f){
                        var front=r.transform.parent?r.transform.parent.Find("Paper printed face"):null;var fm=front?front.GetComponent<Renderer>().sharedMaterial:null;
                        if(fm&&fm.HasProperty("_BaseMap")&&fm.GetTexture("_BaseMap")){m.SetTexture("_ShowMap",fm.GetTexture("_BaseMap"));m.SetFloat("_ShowThrough",Mathf.Clamp01(surface.translucency*.32f));}
                    }
                }else{
                    m.SetFloat("_Paper",paper?1:0);
                    string n=r.name.ToLowerInvariant();bool metal=!paper&&(n.Contains("plate")||n.Contains("bronze")||n.Contains("screw")||n.Contains("pendulum")||n.Contains("watch")||n.Contains("bezel")||n.Contains("crown")||n.Contains("pendant")||n.Contains("bow")||n.Contains("brass"));
                    m.SetFloat("_Metallic",metal?.85f:.0f);m.SetFloat("_Rough",paper?.7f:metal?.3f:.75f);m.SetFloat("_Brushed",metal?.6f:0);
                    // Ticket card and unlabelled solids still get a material, not flat paint.
                    m.SetFloat("_Fibre",paper?.8f:0);m.SetFloat("_Age",paper?.4f:0);m.SetFloat("_Leather",!paper&&(n.Contains("leather")||n.Contains("cover")||n.Contains("board"))?1:0);
                }
                materials[i]=m;ownedMaterials.Add(m);if(ownMeshes)ownedMaterials.Add(old[i]);
            }
            r.sharedMaterials=materials;
        }
        if(ownMeshes)foreach(var f in payload.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.sharedMesh!=intactTicketStock&&f.sharedMesh!=punchedTicketStock&&f.sharedMesh.name!="Cube"&&f.sharedMesh.name!="Sphere"&&f.sharedMesh.name!="Cylinder"&&f.sharedMesh.name!="Quad")ownedMeshes.Add(f.sharedMesh);
        return pivot;
    }
    static Quaternion StudyRest(bool paper)=>Quaternion.Euler(paper?14:12,paper?-14:-25,paper?-3:0);
    void ResizeEvidenceRender(){
        int w=Mathf.Min(1920,Screen.width),h=Mathf.Min(1080,Screen.height);
        if(evidenceRender&&evidenceRender.width==w&&evidenceRender.height==h)return;
        if(evidenceRender){evidenceCamera.targetTexture=null;evidenceRender.Release();Destroy(evidenceRender);}
        evidenceRender=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4,name="Evidence close-up"};evidenceCamera.targetTexture=evidenceRender;evidenceCamera.aspect=Screen.width/(float)Screen.height;
    }
    void EndEvidenceModel(){
        evidenceDragging=false;if(evidenceCamera)evidenceCamera.enabled=false;
        if(evidenceStage)Destroy(evidenceStage);foreach(var m in evidenceOwnedMaterials)if(m)Destroy(m);foreach(var m in evidenceOwnedMeshes)if(m)Destroy(m);evidenceOwnedMaterials.Clear();evidenceOwnedMeshes.Clear();
        if(evidenceRender){evidenceRender.Release();Destroy(evidenceRender);}evidenceStage=null;evidenceObject=null;evidenceCamera=null;
    }
    void ToggleEvidenceDetails(){evidenceDetails=!evidenceDetails;evidenceScroll=Vector2.zero;evidenceDragging=false;}
    void RotateEvidence(Vector2 degrees){evidenceTargetRotation=Quaternion.AngleAxis(degrees.x,Vector3.right)*Quaternion.AngleAxis(degrees.y,Vector3.up)*evidenceTargetRotation;}
    void ZoomEvidence(float steps){evidenceZoomTarget=Mathf.Clamp(evidenceZoomTarget+steps*.1f,.7f,1.5f);}
    // An open book is a two-page spread: it starts closer so a page reads at the size of a single sheet.
    float RestZoom=>evidenceBook&&evidenceBook.style==EvidenceBook.Style.Bound?1.3f:1;
    void ResetEvidencePose(){evidenceTargetRotation=evidenceRestRotation;evidenceZoomTarget=RestZoom;}
    bool EvidenceModelArea(Vector2 mouse){
        float scale=W>0?Screen.width/W:1;var gui=new Vector2(mouse.x/scale,(Screen.height-mouse.y)/scale);
        if(evidencePrevRect.Contains(gui)||evidenceNextRect.Contains(gui))return false;
        if(!evidenceDetails)return !evidenceOpenRect.Contains(gui);
        return mouse.x<Screen.width*.56f;
    }
    void TickEvidenceModel(){
        if(!ModelEvidence||!evidenceObject||houseLetterOpen)return;ResizeEvidenceRender();
        if(!Review){
            if(Input.GetKeyDown(KeyCode.E))ToggleEvidenceDetails();
            if(Input.GetKeyDown(KeyCode.R))ResetEvidencePose();
            if(evidenceBook&&evidenceGallery==null){if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.PageDown))TurnEvidencePage(1);if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.PageUp))TurnEvidencePage(-1);}
            Vector2 mouse=Input.mousePosition;
            if(Input.GetMouseButtonDown(0)){evidenceDragging=EvidenceModelArea(mouse);evidenceLastMouse=mouse;}
            if(Input.GetMouseButtonUp(0))evidenceDragging=false;
            if(evidenceDragging&&Input.GetMouseButton(0)){var delta=(mouse-evidenceLastMouse)*(180f/Screen.height);RotateEvidence(new Vector2(delta.y,-delta.x));}
            evidenceLastMouse=mouse;
            if(EvidenceModelArea(mouse))ZoomEvidence(Input.mouseScrollDelta.y);
        }
        UpdateEvidencePose(false);
    }
    void UpdateEvidencePose(bool instant){
        float blend=instant?1:1-Mathf.Exp(-24*Time.unscaledDeltaTime);
        evidenceZoom=Mathf.Lerp(evidenceZoom,evidenceZoomTarget,blend);evidenceCamera.transform.localPosition=new Vector3(0,0,-3.1f/evidenceZoom);
        float width=2*(3.1f/evidenceZoom)*Mathf.Tan(15*Mathf.Deg2Rad)*evidenceCamera.aspect;
        var target=new Vector3(evidenceDetails?-width*.23f:0,.06f,0);
        evidenceObject.transform.localPosition=Vector3.Lerp(evidenceObject.transform.localPosition,target,blend);evidenceObject.transform.localRotation=Quaternion.Slerp(evidenceObject.transform.localRotation,evidenceTargetRotation,blend);
        if(evidenceFlipHinge){
            float t=Mathf.Clamp01((Time.unscaledTime-evidenceFlipStart)/.62f),e=t*t*(3-2*t);
            var slide=evidenceFlipHinge.GetComponent<EvidenceSlide>();
            if(slide){evidenceFlipHinge.localPosition=Vector3.Lerp(slide.from,slide.to,e)+Vector3.forward*(-Mathf.Sin(e*Mathf.PI)*Mathf.Abs(slide.to.x-slide.from.x)*.06f);evidenceFlipHinge.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(slide.spinFrom,slide.spinTo,e));}
            else evidenceFlipHinge.localRotation=Quaternion.Euler(0,Mathf.Lerp(evidenceFlipFrom,evidenceFlipTo,e),0);
            if(t>=1){evidenceFlipHinge.gameObject.SetActive(false);evidenceFlipHinge=null;if(evidenceFlipReveal){evidenceFlipReveal.SetActive(true);evidenceFlipReveal=null;}}
        }
        float enter=Mathf.SmoothStep(.92f,1,Mathf.Clamp01((Time.unscaledTime-evidenceEnterTime)/.22f));evidenceObject.transform.localScale=Vector3.one*enter;
    }
    void DrawEvidenceModel(){
        // Dark charcoal with a soft pool of light behind the object, as in the reference inspection screen.
        if(!evidenceBackdrop){
            const int size=256;evidenceBackdrop=new Texture2D(size,size,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,name="Evidence backdrop"};
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){float dx=(x-size*.5f)/(size*.5f),dy=(y-size*.54f)/(size*.5f);float r=Mathf.Sqrt(dx*dx*.62f+dy*dy);
                float glow=Mathf.Clamp01(1-r);glow=glow*glow*(3-2*glow);float v=Mathf.Lerp(.014f,.085f,glow);
                float dither=((x*7+y*13)%5)/5f*.003f;evidenceBackdrop.SetPixel(x,y,new Color(v*.96f+dither,v*.97f+dither,v+dither,1));}
            evidenceBackdrop.Apply();
        }
        GUI.DrawTexture(new Rect(0,0,W,H),evidenceBackdrop,ScaleMode.StretchToFill,false);
        if(evidenceRender)GUI.DrawTexture(new Rect(0,0,W,H),evidenceRender,ScaleMode.StretchToFill,true);
        var hintStyle=new GUIStyle(small){fontSize=15,normal={textColor=new Color(.7f,.69f,.65f,.78f)}};
        if(!evidenceDetails){
            var nameStyle=new GUIStyle(text){fontSize=28,alignment=TextAnchor.MiddleLeft};var p=new Rect(W*.64f,H*.82f,W*.31f,56);
            if(!string.IsNullOrEmpty(evidenceKindLabel))Label(new Rect(p.x,p.y-28,p.width,26),evidenceKindLabel,new GUIStyle(small){fontSize=15,normal={textColor=new Color(.74f,.62f,.42f,.9f)}});
            // The whole name block is a button: click (or E) to read what is written on it.
            evidenceOpenRect=new Rect(p.x-18,p.y-38,p.width+30,p.height+86);
            bool hover=evidenceOpenRect.Contains(Event.current.mousePosition);
            Box(evidenceOpenRect,new Color(.75f,.62f,.4f,hover?.10f:.035f));Box(new Rect(evidenceOpenRect.x,evidenceOpenRect.y,2,evidenceOpenRect.height),new Color(.74f,.62f,.42f,hover?.9f:.45f));
            Label(p,inspected.title,nameStyle);Box(new Rect(p.x,p.yMax,p.width,1),new Color(.62f,.61f,.57f,.42f));
            Label(new Rect(p.x,p.yMax+10,p.width,26),"阅读内容  ›     点击或按 E",new GUIStyle(hintStyle){normal={textColor=hover?new Color(.95f,.86f,.66f):hintStyle.normal.textColor}});
            if(GUI.Button(evidenceOpenRect,GUIContent.none,GUIStyle.none))ToggleEvidenceDetails();
            Label(new Rect(42,H-52,W*.55f,30),"左键拖动 旋转    滚轮 缩放    R 复位    Esc 放回"+(evidenceBook?"    ← → 翻页":"")+(evidenceGallery!=null?"    ← → 切换":""),hintStyle);
            DrawBookPager();
            if(evidenceGallery!=null)Label(new Rect(42,34,400,30),"证物陈列 · "+(evidenceGalleryIndex+1)+" / "+evidenceGallery.Count,hintStyle);
            return;
        }
        Rect panel=new Rect(W*.59f,65,W*.36f,H-130);Box(panel,new Color(.018f,.02f,.023f,.78f));
        Label(new Rect(panel.x+26,panel.y+24,panel.width-52,76),inspected.title,new GUIStyle(title){fontSize=32});Label(new Rect(panel.x+26,panel.y+107,panel.width-52,64),inspected.source,small);
        Label(new Rect(panel.x+26,panel.y+152,panel.width-52,24),"纸面内容",new GUIStyle(small){fontSize=15,normal={textColor=new Color(.74f,.62f,.42f,.9f)}});Box(new Rect(panel.x+26,panel.y+176,panel.width-52,1),new Color(.62f,.61f,.57f,.3f));
        var area=new Rect(panel.x+26,panel.y+189,panel.width-52,panel.height-277);float height=text.CalcHeight(new GUIContent(inspected.body),area.width-24)+20;
        evidenceScroll=GUI.BeginScrollView(area,evidenceScroll,new Rect(0,0,area.width-24,Mathf.Max(area.height,height)));Label(new Rect(0,0,area.width-24,height),inspected.body,text);GUI.EndScrollView();
        evidenceCloseRect=new Rect(panel.xMax-170,panel.yMax-66,144,44);
        if(Button(evidenceCloseRect,"‹  收起",true))ToggleEvidenceDetails();
        DrawBookPager();
        Label(new Rect(panel.x+26,panel.yMax-63,panel.width-220,45),"E 收起    R 复位    Esc 放回",small);DrawHousePaperBack();
    }
}
