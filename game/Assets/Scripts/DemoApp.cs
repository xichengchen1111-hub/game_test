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
    public SliceSave state = new SliceSave();
    public bool Review { get; private set; }
    public float Minutes => 17*60+55+Time.time/60f;
    public bool Blocked => opening || transitioning || inspected || journal || paused;
    public string CurrentScene { get; private set; } = "Town";
    public bool Silent;
    bool opening=true, transitioning, journal, paused, watch;
    bool quitting;
    bool sceneReady;
    float fade=1, locationAge, remarkUntil;
    string location="青川 · 小镇广场", remark="";
    Interactable inspected, focus;
    string savePath, captures;
    GUIStyle text, small, title, button, large, italic;
    Texture2D solid, circle;
    float W,H;
    int selectedEntry;
    bool recordWalk,recordingWalk;
    int motionFrames;

    void Awake() {
        if (Instance) { Destroy(gameObject); return; }
        Instance=this; DontDestroyOnLoad(gameObject);
        Application.wantsToQuit+=WantsToQuit;
        Application.targetFrameRate=60;
        var args=Environment.GetCommandLineArgs();
        Review=Array.IndexOf(args,"--review")>=0;
        recordWalk=Array.IndexOf(args,"--record")>=0;
        Silent=Review || Array.IndexOf(args,"--silent")>=0;
        savePath=Path.Combine(Application.persistentDataPath,Review?"review-save.json":"slice-save.json");
        int c=Array.IndexOf(args,"--capture-dir");
        captures=c>=0&&c+1<args.Length?args[c+1]:Path.Combine(Application.persistentDataPath,"Captures");
        Directory.CreateDirectory(captures);
        if (File.Exists(savePath) && Array.IndexOf(args,"--fresh")<0) {
            try { state=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath)); }
            catch (Exception e) { Debug.LogWarning("Save could not be read: "+e.Message); }
        }
        if (state==null || state.entries==null) state=new SliceSave();
        if (state.scene!="Town" && state.scene!="ClockShop") state.scene="Town";
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        StartCoroutine(InitialScene(args));
    }
    IEnumerator InitialScene(string[] args) {
        yield return SceneManager.LoadSceneAsync(state.scene,LoadSceneMode.Additive);
        CurrentScene=state.scene;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentScene));
        player.Place(new Vector3(state.x,state.y,state.z),state.yaw);
        player.Aim(CurrentScene=="Town"?new Vector3(0,3.1f,9):new Vector3(0,1.4f,1.5f));
        sceneReady=true;
        location=CurrentScene=="Town"?"青川 · 小镇广场":"青川 · 钟表铺";
        locationAge=Time.time;
        yield return FadeTo(0,0.9f);
        if (Review) {
            opening=false;
            if(Array.IndexOf(args,"--verify-save")>=0) StartCoroutine(VerifySaved());
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
    IEnumerator WalkTo(Vector3 target) {
        Debug.Log("Review walking: "+player.transform.position+" -> "+target+" blocked="+Blocked+" motor="+player.enabled);
        float until=Time.time+20;
        while(Vector3.Distance(new Vector3(player.transform.position.x,0,player.transform.position.z),new Vector3(target.x,0,target.z))>0.10f&&Time.time<until) {
            Vector3 d=target-player.transform.position;d.y=0;player.scriptedDirection=d.normalized;yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        if(Time.time>=until) throw new Exception("Review movement blocked before "+target+" at "+player.transform.position+" blocked="+Blocked+" direction="+player.scriptedDirection);
    }
    IEnumerator ReviewRun() {
        yield return new WaitForSeconds(2);
        yield return Capture("town");watch=true;yield return Capture("town-watch");watch=false;
        if(recordWalk){recordingWalk=true;StartCoroutine(RecordWalk());}
        yield return WalkTo(new Vector3(0,0,-5));
        yield return WalkTo(new Vector3(3.7f,0,1));
        recordingWalk=false;
        player.Aim(new Vector3(5.8f,1.2f,1));yield return null;
        if(!TryInteract()) throw new Exception("Shop door could not be reached by interaction ray");
        yield return new WaitForSeconds(1.8f);
        if(CurrentScene!="ClockShop") throw new Exception("Shop transition failed");
        yield return Capture("clock-shop");
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
    IEnumerator RecordWalk() {
        string folder=Path.Combine(captures,"motion");Directory.CreateDirectory(folder);
        while(recordingWalk) {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"frame"+(motionFrames++).ToString("D4")+".png"));
            yield return new WaitForSeconds(.2f);
        }
    }
    [Serializable] class ReviewResult {public bool evidenceRecorded,doorRoundTrip,characterControllerWalk,companionSilent; public int renderedFrames,motionFrames; public string verification;}
}
