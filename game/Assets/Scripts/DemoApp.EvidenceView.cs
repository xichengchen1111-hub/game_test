using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public partial class DemoApp {
    public Mesh intactTicketStock,punchedTicketStock;
    GameObject evidenceStage,evidenceObject;Camera evidenceCamera;RenderTexture evidenceRender;
    readonly HashSet<Material> evidenceOwnedMaterials=new HashSet<Material>();
    readonly HashSet<Mesh> evidenceOwnedMeshes=new HashSet<Mesh>();
    bool evidenceDetails,evidenceDragging;Vector2 evidenceScroll,evidenceLastMouse;float evidenceZoom=1,evidenceZoomTarget=1,evidenceEnterTime;
    Quaternion evidenceRestRotation,evidenceTargetRotation;
    bool ModelEvidence=>inspected&&!string.IsNullOrEmpty(inspected.evidenceId)&&!(inspected is HouseContainer)&&!(inspected is ClockTowerAdjustment)&&!DialogueOpen;
    void BeginEvidenceModel(Interactable item){
        EndEvidenceModel();evidenceDetails=false;evidenceScroll=Vector2.zero;evidenceZoom=evidenceZoomTarget=1;evidenceEnterTime=Time.unscaledTime;
        evidenceStage=new GameObject("Evidence observation stage");evidenceStage.transform.position=new Vector3(0,-500,0);
        var cam=new GameObject("Evidence observation camera");cam.transform.SetParent(evidenceStage.transform,false);cam.transform.localPosition=new Vector3(0,0,-3.1f);
        evidenceCamera=cam.AddComponent<Camera>();evidenceCamera.clearFlags=CameraClearFlags.SolidColor;evidenceCamera.backgroundColor=Color.clear;evidenceCamera.cullingMask=1<<31;
        evidenceCamera.orthographic=false;evidenceCamera.fieldOfView=30;evidenceCamera.nearClipPlane=.05f;evidenceCamera.farClipPlane=8;evidenceCamera.allowMSAA=true;evidenceCamera.allowHDR=false;
        var data=evidenceCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
        ResizeEvidenceRender();
        var physical=item.GetComponentInChildren<EvidencePhysical>();bool ownMeshes=!physical;GameObject payload;
        bool ticket=(item.evidenceId=="E02"||item.evidenceId=="E03")&&intactTicketStock&&punchedTicketStock;
        if(ticket){
            payload=new GameObject("Actual ticket stock");payload.AddComponent<MeshFilter>().sharedMesh=item.evidenceId=="E03"?punchedTicketStock:intactTicketStock;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.SetTexture("_BaseMap",TicketImage(item.evidenceId));payload.AddComponent<MeshRenderer>().sharedMaterial=mat;
            payload.transform.localRotation=Quaternion.Euler(-90,0,0);
        }else if(item.evidenceId=="H17"||item.evidenceId=="H18"||item.evidenceId=="E06")payload=EvidenceSolidModel.Create(item.evidenceId,item.illustration);
        else if(physical){payload=Instantiate(physical.gameObject);foreach(var c in payload.GetComponentsInChildren<Collider>())Destroy(c);foreach(var c in payload.GetComponentsInChildren<MonoBehaviour>())Destroy(c);payload.transform.localRotation=Quaternion.identity;}
        else{var texture=item.illustration?item.illustration:TicketImage(item.evidenceId);float aspect=texture?texture.width/(float)texture.height:1.33f;payload=EvidenceModel.Paper("Observed evidence",texture,1,1/aspect,item.reverse);}
        // Centre the complete model before rotating, including imported meshes with an offset origin.
        payload.transform.SetParent(evidenceStage.transform,false);payload.transform.localPosition=Vector3.zero;
        var bounds=new Bounds();bool first=true;foreach(var r in payload.GetComponentsInChildren<Renderer>()){if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}
        float size=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);var centre=evidenceStage.transform.InverseTransformPoint(bounds.center);
        evidenceObject=new GameObject("Examined evidence pivot");evidenceObject.transform.SetParent(evidenceStage.transform,false);payload.transform.SetParent(evidenceObject.transform,true);payload.transform.localPosition-=centre;
        payload.transform.localScale*=1.12f/Mathf.Max(.01f,size);payload.transform.localPosition*=1.12f/Mathf.Max(.01f,size);
        bool paper=ticket||(physical&&physical.paper)||!(item.evidenceId=="H17"||item.evidenceId=="H18"||item.evidenceId=="E06");
        var shader=Resources.Load<Shader>("EvidenceStudy");
        foreach(var r in payload.GetComponentsInChildren<Renderer>()){
            var old=r.sharedMaterials;var materials=new Material[old.Length];
            for(int i=0;i<old.Length;i++){
                var m=new Material(shader);m.SetColor("_BaseColor",old[i].HasProperty("_BaseColor")?old[i].GetColor("_BaseColor"):Color.white);
                if(old[i].HasProperty("_BaseMap"))m.SetTexture("_BaseMap",old[i].GetTexture("_BaseMap"));m.SetFloat("_Paper",paper?1:0);
                string n=r.name.ToLowerInvariant();m.SetFloat("_Metallic",paper?0:n.Contains("plate")||n.Contains("bronze")||n.Contains("screw")||n.Contains("pendulum")?.65f:.08f);
                materials[i]=m;evidenceOwnedMaterials.Add(m);if(ownMeshes)evidenceOwnedMaterials.Add(old[i]);
            }
            r.sharedMaterials=materials;
        }
        if(ownMeshes)foreach(var f in payload.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh&&f.sharedMesh!=intactTicketStock&&f.sharedMesh!=punchedTicketStock&&f.sharedMesh.name!="Cube"&&f.sharedMesh.name!="Sphere"&&f.sharedMesh.name!="Cylinder"&&f.sharedMesh.name!="Quad")evidenceOwnedMeshes.Add(f.sharedMesh);
        evidenceRestRotation=Quaternion.Euler(paper?9:12,paper?-16:-25,paper?-3:0);evidenceTargetRotation=evidenceRestRotation;evidenceObject.transform.localRotation=evidenceRestRotation;
        foreach(var t in evidenceStage.GetComponentsInChildren<Transform>())t.gameObject.layer=31;
        focus=null;UpdateEvidencePose(true);
    }
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
    void ResetEvidencePose(){evidenceTargetRotation=evidenceRestRotation;evidenceZoomTarget=1;}
    bool EvidenceModelArea(Vector2 mouse)=>!evidenceDetails||mouse.x<Screen.width*.56f;
    void TickEvidenceModel(){
        if(!ModelEvidence||!evidenceObject||houseLetterOpen)return;ResizeEvidenceRender();
        if(!Review){
            if(Input.GetKeyDown(KeyCode.E))ToggleEvidenceDetails();
            if(Input.GetKeyDown(KeyCode.R))ResetEvidencePose();
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
        float enter=Mathf.SmoothStep(.92f,1,Mathf.Clamp01((Time.unscaledTime-evidenceEnterTime)/.22f));evidenceObject.transform.localScale=Vector3.one*enter;
    }
    void DrawEvidenceModel(){
        Box(new Rect(0,0,W,H),new Color(.012f,.011f,.013f,1));
        if(evidenceRender)GUI.DrawTexture(new Rect(0,0,W,H),evidenceRender,ScaleMode.StretchToFill,true);
        if(!evidenceDetails){
            var nameStyle=new GUIStyle(text){fontSize=26,alignment=TextAnchor.MiddleLeft};var p=new Rect(W*.64f,H*.84f,W*.29f,56);
            Label(p,inspected.title,nameStyle);Box(new Rect(p.x,p.yMax,p.width,1),new Color(.62f,.61f,.57f,.42f));return;
        }
        Rect panel=new Rect(W*.59f,65,W*.36f,H-130);Box(panel,new Color(.018f,.02f,.023f,.78f));
        Label(new Rect(panel.x+26,panel.y+24,panel.width-52,76),inspected.title,new GUIStyle(title){fontSize=32});Label(new Rect(panel.x+26,panel.y+107,panel.width-52,64),inspected.source,small);
        var area=new Rect(panel.x+26,panel.y+181,panel.width-52,panel.height-269);float height=text.CalcHeight(new GUIContent(inspected.body),area.width-24)+20;
        evidenceScroll=GUI.BeginScrollView(area,evidenceScroll,new Rect(0,0,area.width-24,Mathf.Max(area.height,height)));Label(new Rect(0,0,area.width-24,height),inspected.body,text);GUI.EndScrollView();
        Label(new Rect(panel.x+26,panel.yMax-63,panel.width-52,45),"E 收起详情    R 复位    Esc 放回",small);DrawHousePaperBack();
    }
}
