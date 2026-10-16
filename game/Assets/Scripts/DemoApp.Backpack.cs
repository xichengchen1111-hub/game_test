using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// The briefcase (user, 2026-10-15): once the guard hands it back it stays with the player, and B opens
// it like the inventory in most games. A grid of small 3D pictures holds what he carries — the watch,
// the map, the ticket, the notebook and the wallet — and every piece of physical evidence he has
// examined; the selected one turns on the right, and E takes it out or examines it the usual way
// (the RE3-style view, returning here afterwards). What people said is not in here: testimony goes
// to the dossier (Tab, DrawJournal). The originals stay where they were found (only the ticket E02 is
// carried, as before); the briefcase lists what he has looked at, as the notebook did.
public partial class DemoApp {
    // Spoken testimony belongs to the dossier, not the briefcase.
    static readonly HashSet<string> TestimonyIds=new HashSet<string>{"E04","Y04"};
    sealed class PackItem { public string id,title,source,hint; public Interactable item; public bool carried; }
    bool backpack,packReturn;
    public bool BackpackOpen=>backpack;
    readonly List<PackItem> packItems=new List<PackItem>();
    int packSelected,packCarried;
    Vector2 packScroll;bool packFollow;
    readonly Dictionary<string,Interactable> packProxies=new Dictionary<string,Interactable>();
    // The selected item turns on its own stage; thumbnails are drawn one per frame on another.
    GameObject packStage,packObject,packThumbStage,packThumbObject;
    Camera packCamera,packThumbCamera;RenderTexture packRender;
    readonly Dictionary<string,RenderTexture> packThumbs=new Dictionary<string,RenderTexture>();
    readonly HashSet<Material> packMaterials=new HashSet<Material>(),packThumbMaterials=new HashSet<Material>();
    readonly HashSet<Mesh> packMeshes=new HashSet<Mesh>(),packThumbMeshes=new HashSet<Mesh>();
    string packShown,packThumbPending;int packThumbFrame=-1;
    float packSpin,packTilt;bool packDragging;Quaternion packRest=Quaternion.identity;
    Texture2D packMapTexture;GUIStyle packSlotName,packTitle;
    const int PackColumns=6;
    const float PackSlot=112,PackGap=14;

    void OpenBackpack() {
        if(backpack||!state.belongingsCollected||Blocked)return;
        backpack=true;packReturn=false;focus=null;player.scriptedDirection=Vector3.zero;
        BuildPackItems();packSelected=Mathf.Clamp(packSelected,0,Mathf.Max(0,packItems.Count-1));packFollow=true;
        PlayBriefcase(true);
        CursorMode();
        Debug.Log("BACKPACK_OPEN: items="+packItems.Count+" carried="+packCarried);
    }
    void CloseBackpack() {
        if(!backpack)return;
        backpack=false;EndPackStages(true);PlayBriefcase(false);
        CursorMode();
        Debug.Log("BACKPACK_CLOSED");
    }
    // Catches and lid (tools/opening/make_door_sounds.py), through the held-item source.
    void PlayBriefcase(bool open) {
        var source=GetComponent<TownSound>()?.HeldItemSource(false);var clip=Resources.Load<AudioClip>(open?"OpeningSounds/BriefcaseOpen":"OpeningSounds/BriefcaseClose");
        if(!source||!clip)return;source.clip=clip;source.Play();
    }
    Interactable PackProxy(string id,string title,string source,string body,Texture2D image) {
        if(!packProxies.TryGetValue(id,out var proxy)||!proxy) {
            var go=new GameObject("Briefcase item "+id);go.transform.SetParent(transform,false);
            proxy=go.AddComponent<Interactable>();packProxies[id]=proxy;
        }
        proxy.evidenceId=id;proxy.title=title;proxy.source=source;proxy.body=body;proxy.illustration=image;
        return proxy;
    }
    Texture2D PackMapTexture() {
        if(heldTownMap&&heldTownMap.PlanTexture)return heldTownMap.PlanTexture;
        if(!packMapTexture)packMapTexture=TownMapInk.Plan(TownPlan.Current);
        return packMapTexture;
    }
    void BuildPackItems() {
        packItems.Clear();
        void Carry(string id,string title,string hint,Interactable item)=>packItems.Add(new PackItem{id=id,title=title,source="随身物品",hint=hint,item=item,carried=true});
        Carry("pack-watch","怀表","E 取出怀表（Q）",PackProxy("pack-watch","怀表","随身物品","随身的黄铜怀表。",dialTexture));
        Carry("pack-map","纸地图","E 展开地图（M）",PackProxy("pack-map","纸地图","随身物品","青川镇街道图。",PackMapTexture()));
        if(!carriedTicket)InspectCarriedTicket(false);
        Carry("E02","车票","E 查看（T）",carriedTicket);
        Carry("pack-notebook","调查簿","E 翻开档案（Tab）",PackProxy("pack-notebook","调查簿","随身物品","你的调查簿，问来的话都记在里面。",null));
        Carry("pack-wallet","钱夹","E 查看",PackProxy("pack-wallet","钱夹","随身物品","皮钱夹。几张纸币，一些零钱。",null));
        packCarried=packItems.Count;
        // The world object each record came from, so the picture is the object itself.
        var world=new Dictionary<string,Interactable>();
        foreach(var it in FindObjectsByType<Interactable>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
            if(string.IsNullOrEmpty(it.evidenceId)||it is HouseContainer||it.transform.IsChildOf(transform))continue;
            if(!world.TryGetValue(it.evidenceId,out var had)||!had.gameObject.activeInHierarchy&&it.gameObject.activeInHierarchy||!had.GetComponentInChildren<EvidencePhysical>(true)&&it.GetComponentInChildren<EvidencePhysical>(true))world[it.evidenceId]=it;
        }
        foreach(var e in state.entries) {
            if(string.IsNullOrEmpty(e.id)||TestimonyIds.Contains(e.id)||e.id=="E02"||e.id.StartsWith("pack-"))continue;
            // Not in this scene, or a record with no object of its own (the reassembled letter): a sheet with its text.
            var item=world.TryGetValue(e.id,out var w)?w:PackProxy(e.id,e.title,e.source,e.body,null);
            packItems.Add(new PackItem{id=e.id,title=e.title,source=e.source,hint="E 查看",item=item});
        }
    }
    void ActivatePackItem(int index) {
        if(index<0||index>=packItems.Count)return;
        var p=packItems[index];packSelected=index;
        Debug.Log("BACKPACK_ACTIVATE: "+p.id);
        switch(p.id) {
            case "pack-watch":CloseBackpack();SetWatchOpen(true);return;
            case "pack-map":
                if(CurrentScene!="Town"){OpeningReminder("这是青川镇的地图，在这里用不上。");return;}
                CloseBackpack();SetMapOpen(true);return;
            case "pack-notebook":CloseBackpack();OpenDossier();return;
        }
        // Examined the usual way; Esc puts it back in the briefcase.
        backpack=false;EndPackStages(false);packReturn=true;
        if(p.id=="E02")InspectCarriedTicket();else Inspect(p.item);
    }
    // After an examination started here, the briefcase opens again on the same item.
    void ReturnToBackpack() {
        if(!packReturn)return;
        packReturn=false;backpack=true;packShown=null;packFollow=true;CursorMode();
    }
    void OpenDossier(){journal=true;selectedEntry=0;journalPage=0;housePaperScroll=Vector2.zero;CursorMode();}
    void UpdateBackpackInput() {
        if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.B)){CloseBackpack();return;}
        if(Input.GetKeyDown(KeyCode.Tab)){CloseBackpack();OpenDossier();return;}
        int dx=(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D)?1:0)-(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A)?1:0);
        int dy=(Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.S)?1:0)-(Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.W)?1:0);
        if(dx!=0||dy!=0)MovePackSelection(dx,dy);
        if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter))ActivatePackItem(packSelected);
    }
    void PackCell(int index,out int row,out int column) {
        if(index<packCarried){row=0;column=index;return;}
        int k=index-packCarried;row=1+k/PackColumns;column=k%PackColumns;
    }
    void MovePackSelection(int dx,int dy) {
        if(packItems.Count==0)return;
        if(dx!=0)packSelected=Mathf.Clamp(packSelected+dx,0,packItems.Count-1);
        if(dy!=0) {
            PackCell(packSelected,out int row,out int column);int best=-1,bestGap=int.MaxValue;
            for(int i=0;i<packItems.Count;i++){PackCell(i,out int r,out int c);if(r==row+dy&&Mathf.Abs(c-column)<bestGap){best=i;bestGap=Mathf.Abs(c-column);}}
            if(best>=0)packSelected=best;
        }
        packFollow=true;
    }

    // ---- Stages: preview and thumbnails ----
    Camera PackCamera(Transform stage,string name) {
        var go=new GameObject(name);go.transform.SetParent(stage,false);go.transform.localPosition=new Vector3(0,0,-3.1f);
        var cam=go.AddComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.clear;cam.cullingMask=1<<31;
        cam.orthographic=false;cam.fieldOfView=30;cam.nearClipPlane=.05f;cam.farClipPlane=8;cam.allowMSAA=true;cam.allowHDR=false;cam.aspect=1;
        var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
        return cam;
    }
    void EnsurePackStages() {
        // Far below the town (and the inspection stage at -500), each out of the other's 8 m clip range.
        if(!packStage) {
            packStage=new GameObject("Briefcase preview stage");packStage.transform.position=new Vector3(0,-640,0);
            packCamera=PackCamera(packStage.transform,"Briefcase preview camera");
            packRender=new RenderTexture(900,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4,name="Briefcase preview"};
            packCamera.targetTexture=packRender;
        }
        if(!packThumbStage) {
            packThumbStage=new GameObject("Briefcase thumbnail stage");packThumbStage.transform.position=new Vector3(0,-700,0);
            packThumbCamera=PackCamera(packThumbStage.transform,"Briefcase thumbnail camera");packThumbCamera.enabled=false;
        }
    }
    static void ReleaseOwned(HashSet<Material> materials,HashSet<Mesh> meshes) {
        foreach(var m in materials)if(m)Destroy(m);foreach(var m in meshes)if(m)Destroy(m);materials.Clear();meshes.Clear();
    }
    GameObject PackModel(PackItem p,Transform stage,HashSet<Material> materials,HashSet<Mesh> meshes,string name,out bool paper) {
        var payload=EvidencePayload(p.item,out paper,out bool own);payload.SetActive(true);
        var pivot=StudyPivot(payload,stage,paper,own,materials,meshes,name);
        foreach(var t in pivot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
        return pivot;
    }
    void ShowPackPreview(PackItem p) {
        if(packObject){Destroy(packObject);packObject=null;}ReleaseOwned(packMaterials,packMeshes);
        if(p==null||!p.item)return;
        packObject=PackModel(p,packStage.transform,packMaterials,packMeshes,"Briefcase preview pivot",out bool paper);
        packRest=StudyRest(paper);packSpin=0;packTilt=0;packObject.transform.localRotation=packRest;
    }
    void RenderNextPackThumb() {
        if(packThumbObject){Destroy(packThumbObject);packThumbObject=null;}ReleaseOwned(packThumbMaterials,packThumbMeshes);packThumbPending=null;
        PackItem next=null;foreach(var p in packItems)if(p.item&&!packThumbs.ContainsKey(p.id)){next=p;break;}
        if(next==null){packThumbCamera.enabled=false;return;}
        var rt=new RenderTexture(192,192,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=4,name="Briefcase thumbnail "+next.id};
        packThumbs[next.id]=rt;packThumbPending=next.id;packThumbFrame=Time.frameCount;
        packThumbObject=PackModel(next,packThumbStage.transform,packThumbMaterials,packThumbMeshes,"Briefcase thumbnail pivot",out bool paper);
        packThumbObject.transform.localRotation=StudyRest(paper);
        packThumbCamera.targetTexture=rt;packThumbCamera.enabled=true;
    }
    void EndPackStages(bool releaseThumbs) {
        // A thumbnail set up this frame has not been drawn yet: drop it so it is drawn again next time.
        if(packThumbPending!=null&&packThumbFrame==Time.frameCount&&packThumbs.TryGetValue(packThumbPending,out var pending)){if(pending){pending.Release();Destroy(pending);}packThumbs.Remove(packThumbPending);}
        packThumbPending=null;
        if(packObject)Destroy(packObject);if(packThumbObject)Destroy(packThumbObject);packObject=packThumbObject=null;
        ReleaseOwned(packMaterials,packMeshes);ReleaseOwned(packThumbMaterials,packThumbMeshes);
        if(packCamera)packCamera.targetTexture=null;if(packThumbCamera)packThumbCamera.targetTexture=null;
        if(packStage)Destroy(packStage);if(packThumbStage)Destroy(packThumbStage);packStage=packThumbStage=null;packCamera=packThumbCamera=null;
        if(packRender){packRender.Release();Destroy(packRender);packRender=null;}
        if(releaseThumbs){foreach(var rt in packThumbs.Values)if(rt){rt.Release();Destroy(rt);}packThumbs.Clear();}
        packShown=null;packDragging=false;
    }
    void TickBackpack() {
        if(!backpack)return;
        EnsurePackStages();
        var p=packItems.Count>0?packItems[Mathf.Clamp(packSelected,0,packItems.Count-1)]:null;
        if(p?.id!=packShown){ShowPackPreview(p);packShown=p?.id;}
        if(packObject) {
            if(!packDragging)packSpin+=Time.unscaledDeltaTime*22;
            packObject.transform.localRotation=Quaternion.AngleAxis(packSpin,Vector3.up)*Quaternion.AngleAxis(packTilt,Vector3.right)*packRest;
        }
        RenderNextPackThumb();
    }

    // ---- Drawing ----
    void DrawBackpack() {
        Box(new Rect(0,0,W,H),new Color(.012f,.011f,.013f,.96f));
        Label(new Rect(60,34,500,56),"公文包",title);
        Label(new Rect(62,94,760,32),"随身物品，和你查看过的物证。原件都留在原处。",italic);
        float x0=60,gridWidth=PackColumns*(PackSlot+PackGap)-PackGap,y=146;
        Label(new Rect(x0,y,300,28),"随身",small);y+=32;
        for(int i=0;i<packCarried;i++)DrawPackSlot(i,new Rect(x0+i*(PackSlot+PackGap),y,PackSlot,PackSlot));
        y+=PackSlot+26;
        int evidence=packItems.Count-packCarried;
        Label(new Rect(x0,y,500,28),"物证  ·  "+evidence+" 件",small);y+=32;
        var view=new Rect(x0,y,gridWidth+20,H-y-84);
        int rows=Mathf.CeilToInt(evidence/(float)PackColumns);
        var content=new Rect(0,0,gridWidth,Mathf.Max(view.height,rows*(PackSlot+PackGap)));
        if(packFollow&&packSelected>=packCarried&&Event.current.type==EventType.Layout) {
            float top=(packSelected-packCarried)/PackColumns*(PackSlot+PackGap);
            if(top<packScroll.y)packScroll.y=top;else if(top+PackSlot>packScroll.y+view.height)packScroll.y=top+PackSlot-view.height;
            packFollow=false;
        }
        packScroll=GUI.BeginScrollView(view,packScroll,content);
        for(int k=0;k<evidence;k++)DrawPackSlot(packCarried+k,new Rect(k%PackColumns*(PackSlot+PackGap),k/PackColumns*(PackSlot+PackGap),PackSlot,PackSlot));
        if(evidence==0)Label(new Rect(0,6,gridWidth,60),"还没有查看过物证。",text);
        GUI.EndScrollView();
        // The selected item, turning; drag to turn it by hand.
        var panel=new Rect(W*.585f,118,W*.37f,H-208);Box(panel,new Color(.03f,.03f,.034f,.7f));
        var selected=packItems.Count>0?packItems[Mathf.Clamp(packSelected,0,packItems.Count-1)]:null;
        if(selected!=null) {
            float side=Mathf.Min(panel.width-40,panel.height-190);var stage=new Rect(panel.center.x-side/2,panel.y+18,side,side);
            if(packRender)GUI.DrawTexture(stage,packRender,ScaleMode.ScaleToFit,true);
            var e=Event.current;
            if(e.type==EventType.MouseDown&&stage.Contains(e.mousePosition)){packDragging=true;e.Use();}
            else if(e.type==EventType.MouseDrag&&packDragging){packSpin+=e.delta.x*.6f;packTilt=Mathf.Clamp(packTilt+e.delta.y*.4f,-60,60);e.Use();}
            else if(e.rawType==EventType.MouseUp)packDragging=false;
            if(packTitle==null)packTitle=new GUIStyle(title){fontSize=28};
            Label(new Rect(panel.x+28,stage.yMax+12,panel.width-56,46),selected.title,packTitle);
            Label(new Rect(panel.x+28,stage.yMax+60,panel.width-56,56),selected.source,italic);
            Label(new Rect(panel.x+28,panel.yMax-46,panel.width-56,32),selected.hint,small);
        }
        Label(new Rect(60,H-52,W-120,30),"方向键 / WASD 选择    E 查看    双击 查看    Tab 档案    B / Esc 合上",small);
    }
    void DrawPackSlot(int index,Rect r) {
        bool selected=index==packSelected;var item=packItems[index];
        Box(r,selected?new Color(.17f,.155f,.13f,.96f):new Color(.075f,.075f,.08f,.94f));
        float t=selected?2:1;var edge=selected?new Color(.82f,.64f,.38f):new Color(.32f,.32f,.33f,.55f);
        Box(new Rect(r.x,r.y,r.width,t),edge);Box(new Rect(r.x,r.yMax-t,r.width,t),edge);Box(new Rect(r.x,r.y,t,r.height),edge);Box(new Rect(r.xMax-t,r.y,t,r.height),edge);
        if(packThumbs.TryGetValue(item.id,out var thumb)&&thumb&&item.id!=packThumbPending)GUI.DrawTexture(new Rect(r.x+6,r.y+4,r.width-12,r.height-30),thumb,ScaleMode.ScaleToFit,true);
        if(packSlotName==null)packSlotName=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=14,wordWrap=false,clipping=TextClipping.Clip};
        Label(new Rect(r.x+4,r.yMax-26,r.width-8,22),item.title.Length>8?item.title.Substring(0,7)+"…":item.title,packSlotName);
        var e=Event.current;
        if(e.type==EventType.MouseDown&&e.button==0&&r.Contains(e.mousePosition)) {
            if(e.clickCount>=2&&packSelected==index)ActivatePackItem(index);else packSelected=index;
            e.Use();
        }
    }

    // ---- The dossier (Tab): testimony and what each person was asked ----
    sealed class DossierPerson { public string name,place,body; }
    static readonly (string resource,string name,string place,string prefix,string statement)[] DossierPeople={
        ("MilkboyDialogue","送奶少年","牛奶店柜台前","milkboy.","E04"),
        ("HospitalSisterDialogue","病房护士","医院 · 儿童病房","hospital.sister.","Y04"),
        ("StationClerkDialogue","售票员","车站售票处","station-clerk.",null),
        ("MechanicDialogue","修车匠","修车铺车间","mechanic.",null),
        ("PoliceOfficerDialogue","值班警员","警署","police-officer.",null),
        ("HospitalPorterDialogue","医院门房","医院 · 门厅问讯处","hospital.porter.",null),
        ("HouseGuardDialogue","住宅看守","住宅门外","house-guard.",null),
    };
    // Questions that are not about anything: leaving, or asking to file the report.
    static readonly HashSet<string> DossierSkipped=new HashSet<string>{"police-officer.report"};
    List<DossierPerson> dossier;string dossierSignature;
    List<DossierPerson> Dossier() {
        string signature=state.entries.Count+"/"+state.dialogueTopics.Count;
        if(dossier!=null&&signature==dossierSignature)return dossier;
        dossierSignature=signature;dossier=new List<DossierPerson>();
        foreach(var person in DossierPeople) {
            var statement=person.statement!=null?state.entries.Find(e=>e.id==person.statement):null;
            var topics=state.dialogueTopics.FindAll(t=>t.StartsWith(person.prefix)&&!t.EndsWith(".goodbye")&&!DossierSkipped.Contains(t));
            if(statement==null&&topics.Count==0)continue;
            DialogueScript script=null;
            try{script=DialogueScript.Load(person.resource);}catch(System.Exception e){Debug.LogWarning("Dossier could not read "+person.resource+": "+e.Message);}
            var body=new StringBuilder();
            if(statement!=null)body.Append("证言  /  ").Append(statement.title).Append('\n').Append(statement.body).Append("\n\n");
            bool header=false;
            if(script!=null)foreach(var topic in topics) {
                if(!DossierAnswer(script,topic,out var question,out var answer))continue;
                if(!header){body.Append("问话记录\n\n");header=true;}
                body.Append("问  ").Append(question).Append('\n').Append(answer).Append("\n\n");
            }
            dossier.Add(new DossierPerson{name=person.name,place=person.place,body=body.ToString().TrimEnd()});
        }
        return dossier;
    }
    // The question as asked and the answer as given, up to the point where the person waits for the next question.
    static bool DossierAnswer(DialogueScript script,string topic,out string question,out string answer) {
        question=answer=null;DialogueChoice choice=null;
        foreach(var n in script.nodes){if(n.choices!=null)foreach(var c in n.choices)if(c.topic==topic){choice=c;break;}if(choice!=null)break;}
        if(choice==null)return false;
        question=choice.text;var lines=new List<string>();
        var node=script.Get(choice.target);
        for(int guard=0;node!=null&&guard<12;guard++) {
            if(node.choices!=null&&node.choices.Length>0)break;
            if(!(node.speaker=="你"&&node.text==choice.text))lines.Add(node.speaker+"："+node.text);
            node=string.IsNullOrEmpty(node.next)?null:script.Get(node.next);
        }
        if(lines.Count==0)return false;
        answer=string.Join("\n",lines);return true;
    }
}
