using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public class NotebookEntry { public string id,title,body,source; }
[Serializable] public class SliceSave {
    public string scene="Town";
    public float x=0,y=0.05f,z=-12,yaw=0;
    public float returnX=3.7f,returnY=0.05f,returnZ=1;
    public List<NotebookEntry> entries = new List<NotebookEntry>();
    public bool remarkSeen;
}

public class DemoApp : MonoBehaviour {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)]
    static extern bool TerminateProcess(IntPtr process,uint exitCode);
#endif
    public static DemoApp Instance;
    public PlayerMotor player;
    public Font font;
    public Texture2D dialTexture;
    public UnityEngine.Rendering.Universal.ScriptableRendererFeature reviewAmbientOcclusion;
    public SliceSave state = new SliceSave();
    public bool Review { get; private set; }
    public float Minutes => 17*60+55+Time.time/60f;
    public bool Blocked => opening || transitioning || inspected || journal || paused;
    public string CurrentScene { get; private set; } = "Town";
    public bool Silent;
    bool opening=true, transitioning, journal, paused, watch;
    bool quitting,reviewPortrait;
    bool sceneReady;
    float fade=1, locationAge, remarkUntil;
    string location="青川 · 小镇广场", remark="";
    Interactable inspected, focus;
    string savePath, captures;
    GUIStyle text, small, title, button, large, italic;
    Texture2D solid, circle;
    float W,H;
    int selectedEntry;
    bool recordWalk,recordingWalk,reviewWindow,recordingWindow,previewWindow,previewStreet,reviewLighting,recordingLighting,lightingRight;
    int motionFrames;
    int windowFrames;
    int exteriorWindowCount;
    int lightingFrames;
    float lightingStart,lightingYaw,lightingPitch;
    List<LightingSample> lightingSamples=new List<LightingSample>();

    void Awake() {
        if (Instance) { Destroy(gameObject); return; }
        Instance=this; DontDestroyOnLoad(gameObject);
        Application.wantsToQuit+=WantsToQuit;
        Application.targetFrameRate=60;
        var args=Environment.GetCommandLineArgs();
        Review=Array.IndexOf(args,"--review")>=0;
        previewWindow=!Review&&Array.IndexOf(args,"--window-preview")>=0;
        previewStreet=!Review&&!previewWindow&&Array.IndexOf(args,"--street-preview")>=0;
        recordWalk=Array.IndexOf(args,"--record")>=0;
        reviewWindow=Array.IndexOf(args,"--review-window")>=0;
        reviewLighting=Review&&Array.IndexOf(args,"--review-lighting")>=0;
        lightingRight=reviewLighting&&Array.IndexOf(args,"--lighting-window-right")>=0;
        Silent=Review || Array.IndexOf(args,"--silent")>=0;
        savePath=Path.Combine(Application.persistentDataPath,Review?"review-save.json":previewWindow?"window-preview-save.json":previewStreet?"street-preview-save.json":"slice-save.json");
        int c=Array.IndexOf(args,"--capture-dir");
        captures=c>=0&&c+1<args.Length?args[c+1]:Path.Combine(Application.persistentDataPath,"Captures");
        Directory.CreateDirectory(captures);
        if (File.Exists(savePath) && Array.IndexOf(args,"--fresh")<0) {
            try { state=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath)); }
            catch (Exception e) { Debug.LogWarning("Save could not be read: "+e.Message); }
        }
        if (state==null || state.entries==null) state=new SliceSave();
        if (state.scene!="Town" && state.scene!="ClockShop") state.scene="Town";
        if(previewWindow){state.scene="ClockShop";state.x=0;state.y=.03f;state.z=-.6f;opening=false;}
        if(previewStreet){state.scene="Town";state.x=0;state.y=.03f;state.z=1;opening=false;}
        if(reviewLighting){state.scene="ClockShop";state.x=lightingRight?.4f:-.4f;state.y=.03f;state.z=-.8f;}
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        StartCoroutine(InitialScene(args));
    }
    IEnumerator InitialScene(string[] args) {
        yield return SceneManager.LoadSceneAsync(state.scene,LoadSceneMode.Additive);
        CurrentScene=state.scene;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentScene));
        player.Place(new Vector3(state.x,state.y,state.z),state.yaw);
        player.Aim(previewWindow?FindFirstObjectByType<BuildingInstance>().layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f:CurrentScene=="Town"?new Vector3(0,3.1f,9):new Vector3(0,1.4f,1.5f));
        if(previewStreet){var layout=FindFirstObjectByType<BuildingInstance>().layout;player.Aim(layout.ToTown(layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f));}
        sceneReady=true;
        location=CurrentScene=="Town"?"青川 · 小镇广场":"青川 · 钟表铺";
        locationAge=Time.time;
        yield return FadeTo(0,0.9f);
        if(previewWindow||previewStreet)CursorMode();
        if (Review) {
            opening=false;
            if(Array.IndexOf(args,"--review-milkboy")>=0)StartCoroutine(ReviewMilkboy());
            else if(reviewLighting)StartCoroutine(ReviewLighting(args));
            else if(Array.IndexOf(args,"--verify-save")>=0) StartCoroutine(VerifySaved());
            else StartCoroutine(ReviewRun());
        }
    }
    void Update() {
        if (Input.GetKeyDown(KeyCode.F12)) StartCoroutine(Capture("manual-"+DateTime.Now.ToString("HHmmss")));
        if (transitioning) return;
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if(inspected) CloseInspect();
            else if(journal) journal=false;
            else if(!opening) paused=!paused;
            CursorMode();
        }
        if (!opening && !inspected && !paused) {
            if(Input.GetKeyDown(KeyCode.Tab)) { journal=!journal; selectedEntry=0; CursorMode(); }
            if(Input.GetKeyDown(KeyCode.Q) && !journal) watch=!watch;
        }
        focus=null;
        if(!Blocked && Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)) focus=hit.collider.GetComponentInParent<Interactable>();
        if(!Blocked && Input.GetKeyDown(KeyCode.E)) TryInteract();
    }
    void CursorMode() { bool free=Blocked || Review; Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=free; }
    public bool TryInteract() {
        if(Blocked) return false;
        if(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)) {
            var item=hit.collider.GetComponentInParent<Interactable>();
            if(item) { item.Use(); return true; }
        }
        return false;
    }
    public void Inspect(Interactable item) {
        inspected=item; CursorMode();
        if(!string.IsNullOrEmpty(item.evidenceId) && !state.entries.Exists(e=>e.id==item.evidenceId)) {
            state.entries.Add(new NotebookEntry{id=item.evidenceId,title=item.title,body=item.body,source=item.source});
            Debug.Log("Recorded evidence: "+item.evidenceId);
            Save();
        }
    }
    void CloseInspect() {
        if(inspected && inspected.evidenceId=="E07" && !state.remarkSeen && !Silent) {
            state.remarkSeen=true; remark="同行者  /  老街的事，店里总会留些纸。"; remarkUntil=Time.time+5;
        }
        inspected=null; CursorMode(); Save();
    }
    public void Travel(ScenePortal portal) { if(!transitioning) StartCoroutine(Transition(portal)); }
    IEnumerator Transition(ScenePortal portal) {
        transitioning=true; CursorMode();
        string target=portal.destination;
        Vector3 spawn=portal.entryPosition;
        float yaw=portal.entryYaw;
        if(CurrentScene=="Town") {
            state.returnX=player.transform.position.x; state.returnY=player.transform.position.y; state.returnZ=player.transform.position.z;
        } else spawn=new Vector3(state.returnX,state.returnY,state.returnZ);
        Save(); yield return FadeTo(1,0.4f);
        string old=CurrentScene;
        yield return SceneManager.UnloadSceneAsync(old);
        yield return SceneManager.LoadSceneAsync(target,LoadSceneMode.Additive);
        CurrentScene=target;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(target));
        player.Place(spawn,yaw);
        location=target=="Town"?"青川 · 小镇广场":"青川 · 钟表铺";
        locationAge=Time.time; Save();
        yield return FadeTo(0,0.6f);
        transitioning=false; CursorMode();
        Debug.Log("Door transition completed: "+old+" -> "+target);
    }
    IEnumerator FadeTo(float target,float duration) {
        float start=fade;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime) { fade=Mathf.Lerp(start,target,t/duration); yield return null; }
        fade=target;
    }
    public void Save() {
        if(!player||!sceneReady) return;
        state.scene=CurrentScene;
        var p=player.transform.position; state.x=p.x;state.y=p.y;state.z=p.z;state.yaw=player.transform.eulerAngles.y;
        string temporary=savePath+".tmp";
        File.WriteAllText(temporary,JsonUtility.ToJson(state,true));
        File.Copy(temporary,savePath,true); File.Delete(temporary);
    }
    void OnApplicationQuit() { Save(); }
    void OnDestroy() { Application.wantsToQuit-=WantsToQuit; }
    bool WantsToQuit() {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        RequestQuit();return false;
#else
        return true;
#endif
    }
    void RequestQuit() { if(!quitting){quitting=true;StartCoroutine(QuitAfterInput());} }
    IEnumerator QuitAfterInput() {
        paused=true;CursorMode();Save();
        // Let the native mouse-up/window message finish before closing the player.
        yield return new WaitForSecondsRealtime(.25f);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // This host's Unity 6.3 teardown crashes after relative mouse input.
        // Data is already flushed above. Exit only the standalone Windows player;
        // the OS releases its resources. Never terminate the Editor this way.
        // Mono Environment.Exit can wait indefinitely for Unity's worker threads.
        // Use the current-process pseudo-handle, never a PID or another process.
        if(!TerminateProcess(GetCurrentProcess(),0))throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
#else
        Application.Quit();
#endif
    }

    void Styles() {
        if(solid) return;
        solid=new Texture2D(1,1);solid.SetPixel(0,0,Color.white);solid.Apply();
        circle=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2(x-31.5f,y-31.5f).magnitude;circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(29.5f-r)));}circle.Apply();
        text=new GUIStyle{font=font,fontSize=22,wordWrap=true,normal={textColor=new Color(0.9f,0.88f,0.81f)}};
        small=new GUIStyle(text){fontSize=16};
        title=new GUIStyle(text){fontSize=40};
        large=new GUIStyle(text){fontSize=62};
        italic=new GUIStyle(small){normal={textColor=new Color(0.67f,0.73f,0.75f)}};
        button=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontSize=20};
    }
    void Box(Rect r,Color color) { GUI.color=color;GUI.DrawTexture(r,solid);GUI.color=Color.white; }
    void Label(Rect r,string s,GUIStyle style=null) { GUI.Label(r,s,style??text); }
    bool Button(Rect r,string s,bool warm=false) {
        bool hover=r.Contains(Event.current.mousePosition);
        Box(r,hover?new Color(0.27f,0.35f,0.36f,0.98f):new Color(0.09f,0.16f,0.18f,0.96f));
        Box(new Rect(r.x,r.y,r.width,1),warm?new Color(0.68f,0.5f,0.3f):new Color(0.37f,0.52f,0.53f));
        return GUI.Button(r,s,button);
    }
    void Panel(Rect r) { Box(r,new Color(0.055f,0.10f,0.13f,0.97f)); Box(new Rect(r.x,r.y,r.width,2),new Color(0.68f,0.5f,0.3f)); }
    void OnGUI() {
        if(reviewPortrait)return;
        Styles(); float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        W=Screen.width/scale;H=Screen.height/scale;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
        if(opening && !Review) {
            Box(new Rect(0,0,610,H),new Color(0.035f,0.07f,0.10f,0.72f));
            Label(new Rect(75,100,400,35),"青川  /  一九三一",italic);
            Label(new Rect(70,160,480,90),"错日证词",large);
            Box(new Rect(75,270,70,2),new Color(0.77f,0.57f,0.35f));
            Label(new Rect(75,315,430,90),"暮色落在旧街上。\n钟声、灯光，还有没有说完的话。",text);
            if(Button(new Rect(75,H-230,360,60),"开始漫步",true)) { opening=false; CursorMode(); }
            Label(new Rect(75,H-140,450,70),"自由走动，靠近物件按 E 查看。\nQ 怀表  ·  Tab 调查簿  ·  Esc 暂停",small);
        } else {
            Label(new Rect(36,26,250,35),"错日证词",text);
            Label(new Rect(36,62,300,30),"青川  /  一九三一",italic);
            var right=new GUIStyle(small){alignment=TextAnchor.UpperRight};
            Label(new Rect(W-350,32,310,32),location,right);
            if(!Blocked) {
                GUI.color=new Color(1,0.95f,0.85f,0.8f);GUI.DrawTexture(new Rect(W/2-3,H/2-3,6,6),circle);GUI.color=Color.white;
                if(focus) {
                    Rect p=new Rect(W/2-170,H/2+38,340,43);Box(p,new Color(0.04f,0.08f,0.11f,0.87f));
                    Label(p,"E   "+(focus is ScenePortal?focus.title:"查看 · "+focus.title),button);
                }
                Label(new Rect(36,H-50,W-72,30),"WASD 行走    鼠标 观察    Shift 快走    E 互动    Q 怀表    Tab 调查簿    Esc 暂停",small);
            }
            if(watch&&!inspected&&!journal&&!paused) DrawWatch();
            if(inspected) DrawInspection(inspected.title,inspected.body,inspected.source,inspected.illustration);
            if(journal) DrawJournal();
            if(paused) {
                Box(new Rect(0,0,W,H),new Color(0,0,0,0.42f));Panel(new Rect(W/2-230,H/2-180,460,360));
                Label(new Rect(W/2-180,H/2-135,360,50),"稍作停留",title);
                if(Button(new Rect(W/2-180,H/2-50,360,55),"继续漫步")){paused=false;CursorMode();}
                if(Button(new Rect(W/2-180,H/2+30,360,55),"保存并退出"))RequestQuit();
            }
            if(!Silent&&Time.time<remarkUntil&&!inspected&&!journal) {
                Rect r=new Rect(36,H-135,580,57);Box(r,new Color(0.05f,0.10f,0.13f,0.9f));Label(new Rect(r.x+16,r.y+14,r.width-32,40),remark,small);
            }
        }
        if(fade>0) Box(new Rect(0,0,W,H),new Color(0.018f,0.022f,0.03f,fade));
        GUI.matrix=Matrix4x4.identity;
    }
    void DrawWatch() {
        float size=230,x=42,y=H-330;
        Box(new Rect(x+size/2-11,y-14,22,26),new Color(.58f,.4f,.2f));
        Box(new Rect(x+size/2-20,y-22,40,12),new Color(.68f,.49f,.25f));
        GUI.color=new Color(0.12f,0.09f,0.07f,0.8f);GUI.DrawTexture(new Rect(x+10,y+15,size,size),circle);
        GUI.color=new Color(0.62f,0.44f,0.22f);GUI.DrawTexture(new Rect(x,y,size,size),circle);
        GUI.color=Color.white;if(dialTexture)GUI.DrawTexture(new Rect(x+15,y+15,size-30,size-30),dialTexture);
        Vector2 center=new Vector2(x+size/2,y+size/2);DrawHand(center,Minutes%720*0.5f,56,5);DrawHand(center,Minutes%60*6f,80,3);
        GUI.color=new Color(0.2f,0.16f,0.13f);GUI.DrawTexture(new Rect(center.x-5,center.y-5,10,10),circle);GUI.color=Color.white;
        Label(new Rect(x+20,y+size+14,230,30),"怀表 · 你的随身物件",small);
    }
    void DrawHand(Vector2 center,float angle,float length,float width) {
        var previous=GUI.matrix;GUIUtility.RotateAroundPivot(angle,center);
        Box(new Rect(center.x-width/2,center.y-length,width,length),new Color(0.14f,0.12f,0.12f));GUI.matrix=previous;
    }
    void DrawInspection(string name,string body,string source,Texture2D image) {
        Box(new Rect(0,0,W,H),new Color(0.015f,0.03f,0.04f,0.62f));
        Rect p=new Rect(W/2-560,H/2-310,1120,620);Panel(p);
        Label(new Rect(p.x+42,p.y+32,850,55),name,title);
        Label(new Rect(p.x+42,p.y+97,950,40),"出处  /  "+source,italic);
        Box(new Rect(p.x+42,p.y+144,1036,1),new Color(0.29f,0.37f,0.37f));
        if(image) {GUI.DrawTexture(new Rect(p.x+42,p.y+172,460,340),image,ScaleMode.ScaleToFit);Label(new Rect(p.x+550,p.y+180,500,300),body,text);}
        else Label(new Rect(p.x+48,p.y+182,1010,310),body,text);
        Label(new Rect(p.x+42,p.y+550,550,32),inspected&&!string.IsNullOrEmpty(inspected.evidenceId)?"已记入调查簿，可随时再次查看。":"可随时再次交谈。",small);
        if(Button(new Rect(p.x+865,p.y+532,210,52),"收起  ·  Esc")) { CloseInspect(); }
    }
    void DrawJournal() {
        Box(new Rect(0,0,W,H),new Color(0.015f,0.03f,0.04f,0.5f));
        Rect p=new Rect(W/2-570,H/2-310,1140,620);Panel(p);
        Label(new Rect(p.x+40,p.y+32,600,60),"调查簿",title);
        Label(new Rect(p.x+40,p.y+105,700,40),"材料与见闻  /  保留原话，慢慢核对。",italic);
        Box(new Rect(p.x+40,p.y+153,1060,1),new Color(0.29f,0.37f,0.37f));
        if(state.entries.Count==0) Label(new Rect(p.x+45,p.y+205,990,180),"还没有记录。\n你可以从任意地方开始，看看街钟，或走进钟表铺。",text);
        else {
            for(int i=0;i<state.entries.Count;i++) if(Button(new Rect(p.x+40,p.y+180+i*76,310,60),state.entries[i].title,i==selectedEntry)) selectedEntry=i;
            var e=state.entries[Mathf.Clamp(selectedEntry,0,state.entries.Count-1)];
            Label(new Rect(p.x+400,p.y+183,640,55),e.title,title);
            Label(new Rect(p.x+400,p.y+255,640,55),"出处  /  "+e.source,italic);
            Label(new Rect(p.x+400,p.y+330,640,180),e.body,text);
        }
        if(Button(new Rect(p.x+870,p.y+538,225,52),"继续调查  ·  Tab")){journal=false;CursorMode();}
    }
    IEnumerator Capture(string name) {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(captures,name+".png"));
        yield return new WaitForSeconds(0.2f);
        Debug.Log("Captured runtime frame: "+name);
    }
    IEnumerator WalkTo(Vector3 target,float speedFactor=1) {
        Debug.Log("Review walking: "+player.transform.position+" -> "+target+" blocked="+Blocked+" motor="+player.enabled);
        float until=Time.time+20;
        while(Vector3.Distance(new Vector3(player.transform.position.x,0,player.transform.position.z),new Vector3(target.x,0,target.z))>0.10f&&Time.time<until) {
            Vector3 d=target-player.transform.position;d.y=0;player.scriptedDirection=d.normalized*speedFactor;yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        if(Time.time>=until) throw new Exception("Review movement blocked before "+target+" at "+player.transform.position+" blocked="+Blocked+" direction="+player.scriptedDirection);
    }
    IEnumerator ReviewMilkboy() {
        yield return new WaitForSeconds(1);
        var boy=GameObject.Find("Milkboy");
        if(!boy)throw new Exception("Milkboy is missing from Town.");
        Vector3 origin=boy.transform.position,front=-boy.transform.forward,right=boy.transform.right;
        // Reach the NPC from the square with the real motor and interaction ray.
        yield return WalkTo(new Vector3(3.8f,0,-4.1f));
        player.Aim(origin+Vector3.up*.94f);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-player-view");
        if(!TryInteract()||!inspected||inspected.evidenceId!="E04")throw new Exception("Milkboy interaction ray failed.");
        yield return Capture("unity-dialogue");CloseInspect();
        // Controlled detail viewpoints; lighting and materials stay as in the playable town.
        player.enabled=false;reviewPortrait=true;
        var eye=player.eye;float originalFov=eye.fieldOfView;eye.nearClipPlane=.04f;
        void Frame(Vector3 position,Vector3 target,float fov) {
            eye.transform.position=position;eye.transform.LookAt(target);eye.fieldOfView=fov;
        }
        Frame(origin+front*2.45f+right*.60f+Vector3.up*1.05f,origin+Vector3.up*.70f,36);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-full");
        Frame(origin+front*.94f+right*.19f+Vector3.up*1.22f,origin+Vector3.up*1.14f,33);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-face");
        Frame(origin+front*.77f+right*.10f+Vector3.up*.94f,origin+front*.10f+Vector3.up*.84f,29);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-hands");
        Frame(origin+front*.42f+right*.62f+Vector3.up*1.16f,origin+Vector3.up*1.02f,54);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-side");
        eye.fieldOfView=originalFov;player.enabled=true;
        File.WriteAllText(Path.Combine(captures,"runtime-results.json"),"{\"passed\":true,\"npc\":\"E04\",\"realMotorAndRaycast\":true,\"detailViews\":\"controlled camera, unchanged town lighting\"}");
        Debug.Log("MILKBOY_REVIEW_PASS: real movement, E04 raycast, full/face/hands/side frames");
        Application.Quit();
    }
    IEnumerator ReviewRun() {
        yield return new WaitForSeconds(2);
        yield return Capture("town");watch=true;yield return Capture("town-watch");watch=false;
        if(recordWalk){recordingWalk=true;StartCoroutine(RecordWalk());}
        yield return WalkTo(new Vector3(0,0,-5));
        if(reviewWindow) {
            yield return ReviewStreet();
            yield return WalkTo(new Vector3(0,0,1));
            var building=FindFirstObjectByType<BuildingInstance>();
            exteriorWindowCount=0;
            foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>()) {
                if(marker.kind==OpeningKind.Window)exteriorWindowCount++;
                var opening=building.layout.Opening(marker.openingId);
                if(Vector3.Distance(marker.transform.position,building.layout.ToTown(building.layout.OpeningCentre(opening)))>.001f)
                    throw new Exception("Exterior opening is not aligned with its shared layout: "+marker.openingId);
            }
            player.Aim(building.layout.ToTown(building.layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f));
            yield return new WaitForSeconds(.3f);yield return Capture("layout-exterior");
        }
        yield return WalkTo(new Vector3(3.7f,0,1));
        recordingWalk=false;
        player.Aim(new Vector3(5.8f,1.2f,1));yield return null;
        if(!TryInteract()) throw new Exception("Shop door could not be reached by interaction ray");
        yield return new WaitForSeconds(1.8f);
        if(CurrentScene!="ClockShop") throw new Exception("Shop transition failed");
        yield return Capture("clock-shop");
        if(reviewWindow)yield return ReviewWindow();
        yield return WalkTo(new Vector3(0,0,-0.1f));
        var notice=GameObject.Find("RepairNotice");player.Aim(notice.transform.position);yield return null;
        if(!TryInteract()||!inspected||inspected.evidenceId!="E07") throw new Exception("Notice cannot be inspected normally");
        var sharedClocks=FindObjectsByType<ClockDial>(FindObjectsSortMode.None);
        foreach(var dial in sharedClocks) {
            float expected=-((Minutes+(dial.streetClock?30:0))%60)*6;
            if(Mathf.Abs(Mathf.DeltaAngle(dial.minuteHand.localEulerAngles.z,expected))>.5f)throw new Exception("Clock hand does not match shared time");
            if(dial.minuteHand.GetComponentInChildren<Renderer>().gameObject.isStatic)throw new Exception("Moving clock hands cannot be static batched");
        }
        yield return Capture("evidence");CloseInspect();journal=true;yield return Capture("journal");journal=false;
        yield return WalkTo(new Vector3(0,0,-2.1f));
        player.Aim(new Vector3(0,1.1f,-3.85f));yield return null;
        if(!TryInteract()) throw new Exception("Return door interaction failed");
        yield return new WaitForSeconds(1.8f);
        if(CurrentScene!="Town"||!state.entries.Exists(e=>e.id=="E07")) throw new Exception("Round trip lost evidence");
        if(Vector3.Distance(player.transform.position,new Vector3(state.returnX,state.returnY,state.returnZ))>0.25f) throw new Exception("Wrong exterior return point");
        Save();yield return Capture("return-town");
        File.WriteAllText(Path.Combine(captures,"review-results.json"),JsonUtility.ToJson(new ReviewResult{
            evidenceRecorded=true,doorRoundTrip=true,characterControllerWalk=true,companionSilent=Silent,
            renderedFrames=6,motionFrames=motionFrames,verification="Scripted movement through the actual controller and raycast interaction; not a blind human playthrough"
        },true));
        Debug.Log("SLICE_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifySaved() {
        yield return new WaitForSeconds(1);
        if(CurrentScene!="Town"||!state.entries.Exists(e=>e.id=="E07")) throw new Exception("Disk relaunch did not restore state");
        if(Mathf.Abs(player.transform.position.x-state.returnX)>0.25f) throw new Exception("Disk relaunch position mismatch");
        yield return Capture("reloaded-town");
        File.WriteAllText(Path.Combine(captures,"save-relaunch.txt"),"PASS: fresh process restored E07, Town scene and return position.\n");
        Debug.Log("SLICE_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    IEnumerator ReviewStreet() {
        var town=GameObject.Find("Town geometry").transform;
        int porches=0,posts=0;
        foreach(var porch in town.GetComponentsInChildren<Transform>()) {
            if(porch.name!="Porch")continue;
            var canopy=porch.Find("Awning").GetComponent<Renderer>().bounds;
            var beam=porch.Find("Canopy support beam").GetComponent<Renderer>().bounds;
            if(beam.max.y<canopy.min.y-.001f)throw new Exception("Canopy beam is disconnected: "+porch.parent.name);
            foreach(Transform child in porch) {
                if(child.name!="Shop post")continue;
                var post=child.GetComponent<Renderer>().bounds;
                var joint=new Vector3(post.center.x,(post.max.y+beam.min.y)/2,post.center.z);
                if(post.max.y<beam.min.y-.001f||!beam.Contains(joint))throw new Exception("Canopy post is disconnected: "+porch.parent.name);
                posts++;
            }
            porches++;
        }
        if(porches!=3||posts!=6)throw new Exception("Unexpected number of street canopy supports");
        var bench=town.Find("Bench");
        var sidewalk=town.Find("West sidewalk").GetComponent<Renderer>().bounds;
        var renderers=bench.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        float walkway=sidewalk.max.x-bench.GetComponent<BoxCollider>().bounds.max.x;
        if(walkway<.8f||Mathf.Abs(bounds.min.y-sidewalk.max.y)>.015f)throw new Exception("Bench blocks the footpath or does not rest on it");
        if(Vector3.Dot(bench.Find("Seat facing").forward,Vector3.right)<.99f)throw new Exception("Bench faces away from the square");
        yield return WalkTo(new Vector3(0,0,5.8f));
        yield return WalkTo(new Vector3(-5,0,7.5f));
        player.Aim(bench.position+Vector3.up*.5f);yield return new WaitForSeconds(.3f);yield return Capture("bench-placement");
        // Walk the remaining sidewalk beside the full length of the bench.
        yield return WalkTo(new Vector3(-6.65f,0,6.4f));
        yield return WalkTo(new Vector3(-6.65f,0,8.6f));
        yield return WalkTo(new Vector3(-5,0,8.6f));
        yield return WalkTo(new Vector3(0,0,1));
        player.Aim(new Vector3(5.72f,1.7f,1));yield return new WaitForSeconds(.3f);yield return Capture("canopy-front");
        yield return WalkTo(new Vector3(2.9f,0,4.4f));
        player.Aim(new Vector3(4.42f,2.88f,4.75f));yield return new WaitForSeconds(.3f);yield return Capture("canopy-joint");
        yield return WalkTo(new Vector3(0,0,1));
        File.WriteAllText(Path.Combine(captures,"street-results.json"),JsonUtility.ToJson(new StreetResult{
            connectedPorches=porches,connectedPosts=posts,sidewalkClearance=walkway,benchGrounded=true,benchFacesSquare=true,
            sidewalkWalkPassed=true,verification="Actual rendered player-height views, native renderer bounds and CharacterController walk along the bench-side footpath"
        },true));
        Debug.Log("STREET_REVIEW_PASS: connected posts="+posts+" sidewalk clearance="+walkway);
    }
    IEnumerator ReviewWindow() {
        var street=GameObject.Find("Window street geometry");
        var building=FindFirstObjectByType<BuildingInstance>();
        var layout=building.layout;
        var frame=building.Opening("front-left").gameObject;
        var rightFrame=building.Opening("front-right");
        int interiorWindows=0;
        foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>())if(marker.kind==OpeningKind.Window)interiorWindows++;
        if(exteriorWindowCount!=2||interiorWindows!=exteriorWindowCount)throw new Exception("Interior/exterior window counts differ");
        if(!street||!frame||GameObject.Find("Dusk seen through window"))throw new Exception("Real window geometry missing or image still present");
        if(street.GetComponentsInChildren<Interactable>().Length!=0)throw new Exception("Exterior preview must not duplicate clue interactions");
        var tower=street.transform.Find("Town clock");
        var station=street.transform.Find("Station");
        if(!tower||!station||Vector3.Distance(frame.transform.position,station.position)<10)throw new Exception("Window street has no real depth");
        if(Vector3.Distance(tower.position,layout.ToInterior(new Vector3(0,0,10)))>.01f)throw new Exception("Window street is not aligned with the town");
        yield return WalkTo(new Vector3(0,0,-.6f));
        player.Aim(layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f);yield return new WaitForSeconds(.3f);yield return Capture("layout-interior");
        yield return WalkTo(new Vector3(-.4f,0,-.8f));
        player.Aim(new Vector3(-2,1.95f,-4.4f));yield return new WaitForSeconds(.3f);yield return Capture("window-room");
        yield return WalkTo(new Vector3(-1.35f,0,-3));
        player.Aim(new Vector3(-4.8f,2.25f,-11));yield return new WaitForSeconds(.3f);
        var nearPoint=frame.transform.position;
        var farPoint=tower.TransformPoint(new Vector3(0,6.35f,0));
        var nearFirst=player.eye.WorldToViewportPoint(nearPoint);
        var farFirst=player.eye.WorldToViewportPoint(farPoint);
        yield return Capture("window-right");
        recordingWindow=true;StartCoroutine(RecordWindow());
        yield return new WaitForSeconds(.45f);
        // Keep orientation fixed: differences in near/far displacement are camera parallax.
        yield return WalkTo(new Vector3(-2.25f,0,-3),.18f);
        yield return new WaitForSeconds(.45f);
        recordingWindow=false;yield return new WaitForSeconds(.3f);
        var nearSecond=player.eye.WorldToViewportPoint(nearPoint);
        var farSecond=player.eye.WorldToViewportPoint(farPoint);
        float parallax=Mathf.Abs((nearSecond.x-farSecond.x)-(nearFirst.x-farFirst.x));
        if(parallax<.08f)throw new Exception("Near window and distant clock tower lack expected parallax: "+parallax);
        yield return Capture("window-left");
        // The wall hole admits the view; the sill/glass still keeps the controller inside.
        player.scriptedDirection=Vector3.back;yield return new WaitForSeconds(.8f);player.scriptedDirection=Vector3.zero;
        if(player.transform.position.z < -3.76f||player.transform.position.z > -3.3f)throw new Exception("Window collision does not stop the player at the sill");
        var eye=player.eye.transform.position;
        if(Physics.Raycast(eye,(frame.transform.position-eye).normalized,Vector3.Distance(eye,frame.transform.position)))throw new Exception("Opaque wall still blocks the aperture");
        player.Aim(farPoint);yield return new WaitForSeconds(.3f);yield return Capture("window-near");
        yield return WalkTo(new Vector3(-2.25f,0,-3));
        yield return WalkTo(new Vector3(rightFrame.transform.position.x,0,-3));
        player.Aim(rightFrame.transform.position-rightFrame.transform.forward*.5f);
        yield return new WaitForSeconds(.3f);yield return Capture("window-second");
        player.scriptedDirection=Vector3.back;yield return new WaitForSeconds(.8f);player.scriptedDirection=Vector3.zero;
        if(player.transform.position.z < -3.76f||player.transform.position.z > -3.3f)throw new Exception("Second window collision does not contain the player");
        var rightEye=player.eye.transform.position;
        if(Physics.Raycast(rightEye,(rightFrame.transform.position-rightEye).normalized,Vector3.Distance(rightEye,rightFrame.transform.position)))throw new Exception("Opaque wall blocks the second aperture");
        yield return Capture("window-second-near");
        File.WriteAllText(Path.Combine(captures,"window-results.json"),JsonUtility.ToJson(new WindowResult{
            realTownMeshes=true,wallApertureClear=true,playerContained=true,duplicateCluesRemoved=true,
            exteriorWindows=exteriorWindowCount,interiorWindows=interiorWindows,secondApertureClear=true,secondWindowContained=true,
            relativeViewportParallax=parallax,motionFrames=windowFrames,
            verification="Player-height native rendering, fixed-orientation lateral CharacterController movement, real town alignment and physical window collision"
        },true));
        Debug.Log("WINDOW_REVIEW_PASS: relative viewport parallax="+parallax);
        yield return WalkTo(new Vector3(rightFrame.transform.position.x,0,-3));
    }
    IEnumerator RecordWindow() {
        string folder=Path.Combine(captures,"motion");Directory.CreateDirectory(folder);
        while(recordingWindow) {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"window"+(windowFrames++).ToString("D4")+".png"));
            yield return new WaitForSeconds(.05f);
        }
    }
    IEnumerator ReviewLighting(string[] args) {
        bool noMain=Array.IndexOf(args,"--lighting-no-main")>=0;
        bool noLocal=Array.IndexOf(args,"--lighting-no-local")>=0;
        bool noAO=Array.IndexOf(args,"--lighting-no-ao")>=0;
        foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(light.type==LightType.Directional?noMain:noLocal)light.shadows=LightShadows.None;
        if(noAO&&reviewAmbientOcclusion)reviewAmbientOcclusion.SetActive(false);
        float side=lightingRight?-1:1;
        player.Aim(new Vector3(-2*side,1.95f,-4.4f));yield return new WaitForSeconds(.5f);
        lightingYaw=player.transform.eulerAngles.y;lightingPitch=player.pitch;lightingStart=Time.time;
        recordingLighting=true;StartCoroutine(RecordLighting());
        yield return new WaitForSeconds(.45f);
        for(int i=0;i<2;i++) {
            yield return WalkTo(new Vector3(-1.2f*side,0,-1.4f),.35f);
            yield return WalkTo(new Vector3(-.4f*side,0,-.8f),.35f);
        }
        yield return new WaitForSeconds(.45f);recordingLighting=false;
        yield return new WaitForSeconds(.2f);
        File.WriteAllText(Path.Combine(captures,"lighting-results.json"),JsonUtility.ToJson(new LightingResult{
            mainShadowsDisabled=noMain,localShadowsDisabled=noLocal,ambientOcclusionDisabled=noAO,rightWindow=lightingRight,
            renderedFrames=lightingFrames,duration=Time.time-lightingStart,samples=lightingSamples,
            verification="Native player frames during actual CharacterController walking, head bob and camera rotation; projected matte surface colour samples"
        },true));
        Debug.Log("LIGHTING_REVIEW_PASS: frames="+lightingFrames);Application.Quit();
    }
    IEnumerator RecordLighting() {
        string folder=Path.Combine(captures,"frames");Directory.CreateDirectory(folder);
        // Sample the matte lower door panel on its actual face. A point in front
        // of the patterned glazing changes sampled UVs as the camera moves.
        var door=FindFirstObjectByType<BuildingInstance>().Opening("front-door");
        var doorSurface=door.transform.position+door.transform.forward*.082f;
        doorSurface.y=.8f;
        var points=new[]{new Vector3(-3.049f,1.85f,-3.98f),new Vector3(-.82f,1.8f,-3.89f),doorSurface,new Vector3(-2,3.06f,-3.89f)};
        if(lightingRight)for(int i=0;i<points.Length;i++)points[i].x=-points[i].x;
        var names=new[]{"window-return","front-plaster","door-panel","upper-plaster"};
        while(recordingLighting) {
            float age=Time.time-lightingStart;
            player.transform.rotation=Quaternion.Euler(0,lightingYaw+Mathf.Sin(age*6)*3,0);
            player.pitch=lightingPitch+Mathf.Sin(age*5)*1.5f;
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            for(int i=0;i<points.Length;i++) {
                var p=player.eye.WorldToViewportPoint(points[i]);
                if(p.z<=0||p.x<.02f||p.x>.98f||p.y<.02f||p.y>.98f)continue;
                Color colour=Color.black;int x=Mathf.RoundToInt(p.x*(frame.width-1)),y=Mathf.RoundToInt(p.y*(frame.height-1));
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)colour+=frame.GetPixel(x+dx,y+dy)/9;
                lightingSamples.Add(new LightingSample{frame=lightingFrames,time=age,surface=names[i],r=colour.r,g=colour.g,b=colour.b});
            }
            File.WriteAllBytes(Path.Combine(folder,"frame"+(lightingFrames++).ToString("D4")+".png"),frame.EncodeToPNG());
            Destroy(frame);yield return new WaitForSeconds(.04f);
        }
    }
    IEnumerator RecordWalk() {
        string folder=Path.Combine(captures,"motion");Directory.CreateDirectory(folder);
        while(recordingWalk) {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"frame"+(motionFrames++).ToString("D4")+".png"));
            yield return new WaitForSeconds(.2f);
        }
    }
    [Serializable] class ReviewResult {public bool evidenceRecorded,doorRoundTrip,characterControllerWalk,companionSilent; public int renderedFrames,motionFrames; public string verification;}
    [Serializable] class StreetResult {public int connectedPorches,connectedPosts;public float sidewalkClearance;public bool benchGrounded,benchFacesSquare,sidewalkWalkPassed;public string verification;}
    [Serializable] class WindowResult {public bool realTownMeshes,wallApertureClear,playerContained,duplicateCluesRemoved,secondApertureClear,secondWindowContained;public float relativeViewportParallax;public int motionFrames,exteriorWindows,interiorWindows;public string verification;}
    [Serializable] class LightingSample {public int frame;public float time;public string surface;public float r,g,b;}
    [Serializable] class LightingResult {public bool mainShadowsDisabled,localShadowsDisabled,ambientOcclusionDisabled,rightWindow;public int renderedFrames;public float duration;public List<LightingSample> samples;public string verification;}
}
