using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoApp {
    const int MilkboyGreetingRevision=2;
    DialogueScript milkboyScript;
    MilkboyPerformance milkboyPerformance;
    StationClerkPerformance stationClerkPerformance;
    PeriodCharacterPerformance periodCharacterPerformance;
    OpeningActor dialogueOpeningActor;
    DialogueNode dialogueNode;
    List<DialogueChoice> dialogueChoices=new List<DialogueChoice>();
    readonly List<string> dialogueHistory=new List<string>();
    bool dialogueHistoryOpen;
    Vector2 dialogueHistoryScroll;
    Texture2D dialogueGradient;
    GUIStyle dialogueSpeech,dialogueName,dialogueOption,dialogueMeta,dialogueLog,dialogueShadow;
    bool DialogueOpen => dialogueNode!=null;
    public bool IsSpeakingWith(OpeningActor actor)=>DialogueOpen&&dialogueOpeningActor==actor;
    public bool IsInDialogueWith(Component actor)=>DialogueOpen&&actor&&periodCharacterPerformance&&periodCharacterPerformance.transform.IsChildOf(actor.transform);

    void BindDialoguePerformance(Component actor) {
        if(stationClerkPerformance)stationClerkPerformance.Listen();
        if(milkboyPerformance)milkboyPerformance.Listen();
        PeriodCharacterPerformance.Silence();
        stationClerkPerformance=actor?actor.GetComponentInChildren<StationClerkPerformance>():null;
        periodCharacterPerformance=actor?actor.GetComponentInChildren<PeriodCharacterPerformance>():null;
        // Conversations stop queued locomotion until the existing route resumes.
        dialogueOpeningActor=actor?actor.GetComponent<OpeningActor>():null;if(dialogueOpeningActor)dialogueOpeningActor.StopWalking();
    }

    void OpenMilkboyDialogue() {
        BindDialoguePerformance(null);
        milkboyScript=DialogueScript.Load("MilkboyDialogue");
        dialogueHistory.Clear(); dialogueHistoryOpen=false;
        player.scriptedDirection=Vector3.zero;
        VisitDialogue(state.milkboyGreetingRevision>=MilkboyGreetingRevision?milkboyScript.revisit:milkboyScript.first);
        CursorMode();
        Debug.Log("DIALOGUE_OPEN: milkboy");
    }
    void OpenWatchmakerDialogue() {
        BindDialoguePerformance(null);
        milkboyScript=DialogueScript.Load("WatchmakerDialogue");
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        VisitDialogue(milkboyScript.first);CursorMode();Debug.Log("DIALOGUE_OPEN: watchmaker");
    }
    void OpenStationClerkDialogue(Interactable item) {
        milkboyScript=DialogueScript.Load("StationClerkDialogue");
        BindDialoguePerformance(item);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        VisitDialogue(milkboyScript.first);CursorMode();Debug.Log("DIALOGUE_OPEN: station-clerk");
    }
    void VisitDialogue(string id) {
        dialogueNode=milkboyScript.Get(id);
        dialogueChoices=milkboyScript.Choices(dialogueNode,state);
        UpdateMilkboySpeech();
        dialogueHistory.Add(dialogueNode.speaker+"："+dialogueNode.text);
        if(dialogueHistory.Count>100)dialogueHistory.RemoveAt(0);
        if(dialogueNode.greeted) {
            state.milkboyGreeted=true;
            state.milkboyGreetingRevision=MilkboyGreetingRevision;
        }
        if(!string.IsNullOrEmpty(dialogueNode.grantEvidence)) {
            var entry=milkboyScript.evidence;
            var previous=state.entries.Find(e=>e.id==entry.id);
            if(previous==null)state.entries.Add(new NotebookEntry{id=entry.id,title=entry.title,body=entry.body,source=entry.source});
            else { previous.title=entry.title;previous.body=entry.body;previous.source=entry.source; }
            Debug.Log("Recorded evidence: "+entry.id+" after testimony");
        }
        HospitalDialogueVisited();
        Save();
        Debug.Log("DIALOGUE_NODE: "+id);
    }
    void UpdateMilkboySpeech() {
        if(periodCharacterPerformance) {
            if(dialogueNode.speaker==periodCharacterPerformance.speaker)periodCharacterPerformance.Speak(dialogueNode.text);
            else periodCharacterPerformance.Listen();
        }
        if(stationClerkPerformance) {
            if(dialogueNode.speaker==stationClerkPerformance.speaker)stationClerkPerformance.Speak(dialogueNode.text);
            else stationClerkPerformance.Listen();
        }
        if(!milkboyPerformance) {
            var boy=GameObject.Find("Milkboy");if(boy)milkboyPerformance=boy.GetComponentInChildren<MilkboyPerformance>();
        }
        if(!milkboyPerformance)return;
        if(milkboyScript.evidence!=null&&milkboyScript.evidence.id=="E04"&&dialogueNode.speaker=="送奶少年")milkboyPerformance.Speak(dialogueNode.text);
        else milkboyPerformance.Listen();
    }
    void SelectDialogue(int index) {
        if(!DialogueOpen||dialogueHistoryOpen||index<0||index>=dialogueChoices.Count)return;
        var choice=dialogueChoices[index];
        // Player nodes already retain the chosen question in history.
        var target=milkboyScript.Get(choice.target);
        if(target.speaker!="你"||target.text!=choice.text)dialogueHistory.Add("你："+choice.text);
        if(!string.IsNullOrEmpty(choice.topic)&&!state.dialogueTopics.Contains(choice.topic))state.dialogueTopics.Add(choice.topic);
        VisitDialogue(choice.target);
    }
    void AdvanceDialogue() {
        if(!DialogueOpen||dialogueHistoryOpen||dialogueChoices.Count>0)return;
        if(TryOpeningRideDialogueAction())return;
        if(TryOpeningDialogueAction())return;
        if(TryPoliceDialogueAction())return;
        if(string.IsNullOrEmpty(dialogueNode.next))CloseDialogue();
        else VisitDialogue(dialogueNode.next);
    }
    void CloseDialogue() {
        OpeningRideDialogueClosed();HospitalDialogueClosed();
        dialogueNode=null;dialogueChoices.Clear();dialogueHistoryOpen=false;
        if(milkboyPerformance)milkboyPerformance.Listen();
        if(stationClerkPerformance)stationClerkPerformance.Listen();
        if(periodCharacterPerformance)periodCharacterPerformance.Listen();
        player.scriptedDirection=Vector3.zero;
        CursorMode();Save();
        Debug.Log("DIALOGUE_CLOSED");
    }
    void ToggleDialogueHistory() {
        dialogueHistoryOpen=!dialogueHistoryOpen;
        if(milkboyPerformance)milkboyPerformance.Listen();
        if(stationClerkPerformance)stationClerkPerformance.Listen();
        if(periodCharacterPerformance)periodCharacterPerformance.Listen();
        dialogueHistoryScroll=new Vector2(0,float.MaxValue);
    }
    void UpdateDialogueInput() {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            if(dialogueHistoryOpen)dialogueHistoryOpen=false;
            else CloseDialogue();
            return;
        }
        if(Input.GetKeyDown(KeyCode.H)) { ToggleDialogueHistory(); return; }
        if(dialogueHistoryOpen) {
            if(Input.GetKeyDown(KeyCode.UpArrow))dialogueHistoryScroll.y=Mathf.Max(0,dialogueHistoryScroll.y-70);
            if(Input.GetKeyDown(KeyCode.DownArrow))dialogueHistoryScroll.y+=70;
            return;
        }
        if(dialogueChoices.Count>0) {
            for(int i=0;i<Mathf.Min(9,dialogueChoices.Count);i++) {
                if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i))||Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1+i))) { SelectDialogue(i); return; }
            }
        } else if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter)||Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.E))AdvanceDialogue();
    }
    void DialogueStyles() {
        if(dialogueGradient)return;
        dialogueGradient=new Texture2D(1,128,TextureFormat.RGBA32,false);
        for(int y=0;y<128;y++) {
            float t=1-y/127f;
            dialogueGradient.SetPixel(0,y,new Color(.012f,.016f,.022f,Mathf.SmoothStep(0,.58f,Mathf.Clamp01(t*3f))));
        }
        dialogueGradient.Apply();dialogueGradient.wrapMode=TextureWrapMode.Clamp;
        dialogueSpeech=new GUIStyle(text){fontSize=26,normal={textColor=new Color(.96f,.94f,.87f)}};
        dialogueName=new GUIStyle(small){fontSize=17,normal={textColor=new Color(.91f,.78f,.55f)}};
        dialogueOption=new GUIStyle(text){fontSize=22,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(8,8,4,4),normal={textColor=new Color(.96f,.94f,.87f)}};
        dialogueMeta=new GUIStyle(small){fontSize=15,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.78f,.8f,.79f,.85f)}};
        dialogueLog=new GUIStyle(text){fontSize=23};
        dialogueShadow=new GUIStyle();
    }
    void DialogueLabel(Rect rect,string value,GUIStyle style) {
        dialogueShadow.font=style.font;dialogueShadow.fontSize=style.fontSize;
        dialogueShadow.alignment=style.alignment;dialogueShadow.wordWrap=style.wordWrap;
        dialogueShadow.padding=style.padding;dialogueShadow.normal.textColor=new Color(0,0,0,.9f);
        var shadowRect=rect;shadowRect.position+=new Vector2(1,2);
        GUI.Label(shadowRect,value,dialogueShadow);GUI.Label(rect,value,style);
    }
    float DialogueMargin => Mathf.Max(46,(W-1508)/2);
    Rect DialogueHistoryButton => new Rect(W-DialogueMargin-270,H-35,125,30);
    Rect DialogueExitButton => new Rect(W-DialogueMargin-132,H-35,132,30);
    bool AdvanceDialogueAt(Vector2 point) {
        if(!DialogueOpen||dialogueHistoryOpen||dialogueChoices.Count>0)return false;
        if(!new Rect(W/2,H/2,W/2,H/2).Contains(point))return false;
        if(DialogueHistoryButton.Contains(point)||DialogueExitButton.Contains(point))return false;
        AdvanceDialogue();return true;
    }
    void DialogueArrow(Rect rect) {
        Color color=new Color(.93f,.86f,.69f);
        var previous=GUI.matrix;
        // Draw a small chevron without depending on a font's symbol coverage.
        for(int side=0;side<2;side++) {
            Vector3 origin=new Vector3(rect.center.x+(side==0?-10:10),rect.center.y-5,0);
            GUI.matrix=previous*Matrix4x4.TRS(origin,Quaternion.Euler(0,0,side==0?45:135),Vector3.one);
            Box(new Rect(0,-1.5f,14.2f,3),color);
        }
        GUI.matrix=previous;
    }
    struct DialogueLayout {
        public float speechWidth,speechHeight,rightX,columnGap,columnWidth,row,top;
        public string speech;
        public Rect Choice(int index) => new Rect(rightX+(index%2)*(columnWidth+columnGap),top+(index/2)*row,columnWidth,row-4);
    }
    DialogueLayout MeasureDialogue() {
        float margin=DialogueMargin;bool choosing=dialogueChoices.Count>0;
        // A comfortable reading measure rather than the whole width of a wide screen.
        float speechWidth=choosing?(W-2*margin)*.32f:Mathf.Min(W-2*margin-65,1180);
        float rightX=margin+(choosing?speechWidth:(W-2*margin)*.32f)+38,columnGap=24;
        float columnWidth=(W-margin-rightX-columnGap)/2;
        float row=48;
        foreach(var choice in dialogueChoices)row=Mathf.Max(row,dialogueOption.CalcHeight(new GUIContent(CjkWrap(choice.text,dialogueOption,columnWidth-38)),columnWidth-38)+8);
        string speech=CjkWrap(dialogueNode.text,dialogueSpeech,speechWidth);
        float speechHeight=dialogueSpeech.CalcHeight(new GUIContent(speech),speechWidth);
        float contentHeight=Mathf.Max(100,speechHeight+55,Mathf.Ceil(dialogueChoices.Count/2f)*row);
        float top=H-44-contentHeight;
        return new DialogueLayout{speechWidth=speechWidth,speechHeight=speechHeight,rightX=rightX,columnGap=columnGap,columnWidth=columnWidth,row=row,top=top,speech=speech};
    }
    // ---- Chinese line breaking (user, 2026-10-15: a full stop was left alone at the start of a line) ----
    // IMGUI breaks anywhere. Here no line may begin with closing punctuation or end with an opening
    // bracket or quote: the character before is carried down with it, as Chinese typesetting does.
    const string NoLineStart="，。、；：？！…—）」』”’》〉·,.;:?!)";
    const string NoLineEnd="（「『“‘《〈(";
    readonly Dictionary<(Font,int,FontStyle,char),float> glyphAdvance=new Dictionary<(Font,int,FontStyle,char),float>();
    float GlyphAdvance(GUIStyle style,char c) {
        var key=(style.font,style.fontSize,style.fontStyle,c);
        if(!glyphAdvance.TryGetValue(key,out var width)){var plain=new GUIStyle(style){padding=new RectOffset(),wordWrap=false};width=plain.CalcSize(new GUIContent(c.ToString())).x;glyphAdvance[key]=width;}
        return width;
    }
    public string CjkWrap(string value,GUIStyle style,float width) {
        if(string.IsNullOrEmpty(value)||style==null)return value;
        width-=style.padding.horizontal+6;
        var result=new System.Text.StringBuilder();var paragraphs=value.Split('\n');
        for(int p=0;p<paragraphs.Length;p++) {
            if(p>0)result.Append('\n');
            var line=new System.Text.StringBuilder();float used=0;
            foreach(char c in paragraphs[p]) {
                float advance=GlyphAdvance(style,c);
                if(used+advance>width&&line.Length>0) {
                    int carry=0;
                    if(NoLineStart.IndexOf(c)>=0){carry=1;if(line.Length>2&&NoLineStart.IndexOf(line[line.Length-1])>=0)carry=2;}
                    else if(NoLineEnd.IndexOf(line[line.Length-1])>=0)carry=1;
                    if(carry>=line.Length)carry=0;
                    string down=line.ToString(line.Length-carry,carry);line.Length-=carry;
                    result.Append(line).Append('\n');line.Clear().Append(down);used=0;foreach(char d in down)used+=GlyphAdvance(style,d);
                }
                line.Append(c);used+=advance;
            }
            result.Append(line);
        }
        return result.ToString();
    }
    // A line someone says in passing (the lawyer greeting the player at the door, the guard telling
    // him to keep up) looks like a line of the conversation, without stopping anything (user,
    // 2026-10-15: the lawyer's words came up in a small notice box). Other notices keep a box.
    static bool SpeechRemark(string line,out string speaker,out string words) {
        speaker=words=null;if(string.IsNullOrEmpty(line))return false;
        int colon=line.IndexOf('：');if(colon<1||colon>4)return false;
        var who=line.Substring(0,colon);if(who.IndexOf(' ')>=0||who.IndexOf('·')>=0||who.IndexOf('/')>=0)return false;
        speaker=who;words=line.Substring(colon+1);return true;
    }
    void DrawRemark() {
        DialogueStyles();
        float left=remarkUntil-Time.time,alpha=Mathf.Clamp01(left/.45f)*Mathf.Clamp01((Time.time-remarkStart)/.2f);
        if(SpeechRemark(remark,out var who,out var words)) {
            float margin=DialogueMargin,width=Mathf.Min(W-2*margin-65,1180);
            string wrapped=CjkWrap(words,dialogueSpeech,width);float height=dialogueSpeech.CalcHeight(new GUIContent(wrapped),width);
            float top=H-44-Mathf.Max(78,height+45);
            GUI.color=new Color(1,1,1,alpha);GUI.DrawTexture(new Rect(0,top-28,W,H-top+28),dialogueGradient);
            Box(new Rect(margin,top+5,2,16),new Color(.86f,.7f,.44f,.8f*alpha));GUI.color=new Color(1,1,1,alpha);
            DialogueLabel(new Rect(margin+12,top+3,width-12,25),who,dialogueName);
            DialogueLabel(new Rect(margin,top+33,width,height+4),wrapped,dialogueSpeech);
            GUI.color=Color.white;return;
        }
        string body=CjkWrap(remark,small,548);float h=Mathf.Max(57,small.CalcHeight(new GUIContent(body),548)+28);
        Rect r=new Rect(36,H-78-h,580,h);Box(r,new Color(.025f,.035f,.045f,.84f*alpha));Box(new Rect(r.x,r.y,2,r.height),new Color(.86f,.7f,.44f,.75f*alpha));
        GUI.color=new Color(1,1,1,alpha);Label(new Rect(r.x+16,r.y+14,r.width-32,h-20),body,small);GUI.color=Color.white;
    }
    void DrawDialogue() {
        DialogueStyles();
        GUIUtility.keyboardControl=0;
        float margin=DialogueMargin;
        if(dialogueHistoryOpen) { DrawDialogueHistory(margin); return; }
        bool choosing=dialogueChoices.Count>0;
        var layout=MeasureDialogue();
        float speechWidth=layout.speechWidth,speechHeight=layout.speechHeight,top=layout.top;
        GUI.DrawTexture(new Rect(0,top-28,W,H-top+28),dialogueGradient);
        Box(new Rect(margin,top+5,2,16),new Color(.86f,.7f,.44f,.8f));
        DialogueLabel(new Rect(margin+12,top+3,speechWidth-12,25),dialogueNode.speaker,dialogueName);
        DialogueLabel(new Rect(margin,top+33,speechWidth,speechHeight+4),layout.speech,dialogueSpeech);
        if(!string.IsNullOrEmpty(dialogueNode.grantEvidence))DialogueLabel(new Rect(margin,top+39+speechHeight,speechWidth,23),"证言已记入档案",dialogueMeta);
        if(choosing) {
            // Render against a snapshot: a click changes the active node immediately.
            var choices=dialogueChoices;
            for(int i=0;i<choices.Count;i++) {
                var choice=choices[i];
                Rect r=layout.Choice(i);
                if(r.Contains(Event.current.mousePosition))Box(r,new Color(.55f,.43f,.27f,.20f));
                if(GUI.Button(r,GUIContent.none,GUIStyle.none)) { SelectDialogue(i); break; }
                DialogueLabel(new Rect(r.x+6,r.y+(r.height-25)/2,30,25),(i+1).ToString(),dialogueName);
                DialogueLabel(new Rect(r.x+36,r.y,r.width-38,r.height),CjkWrap(choice.text,dialogueOption,r.width-38),dialogueOption);
            }
        } else DialogueArrow(new Rect(W-margin-38,H-82,38,32));
        if(!DialogueOpen)return;
        if(choosing)DialogueLabel(new Rect(margin,H-35,700,30),"数字键 1–"+dialogueChoices.Count+" / 鼠标选择",dialogueMeta);
        if(GUI.Button(DialogueHistoryButton,GUIContent.none,GUIStyle.none))ToggleDialogueHistory();
        DialogueLabel(DialogueHistoryButton,"H  对话回看",dialogueMeta);
        if(GUI.Button(DialogueExitButton,GUIContent.none,GUIStyle.none))CloseDialogue();
        DialogueLabel(DialogueExitButton,"Esc  结束交谈",dialogueMeta);
        // GUI controls consume their click first; the broad advance area never
        // steals choices, history or exit, nor advances twice on a choice click.
        if(!choosing&&Event.current.type==EventType.MouseUp&&Event.current.button==0&&AdvanceDialogueAt(Event.current.mousePosition))Event.current.Use();
    }
    void DrawDialogueHistory(float margin) {
        // The regular dialogue controls are not drawn while this modal is open.
        Rect panel=new Rect(margin,H*.22f,W-2*margin,H*.70f);
        Box(panel,new Color(.025f,.038f,.05f,.99f));
        Label(new Rect(panel.x+30,panel.y+20,panel.width-250,40),"对话回看",dialogueName);
        Rect view=new Rect(panel.x+30,panel.y+76,panel.width-60,panel.height-100);
        float contentWidth=view.width-30,total=12;
        foreach(var line in dialogueHistory)total+=dialogueLog.CalcHeight(new GUIContent(line),contentWidth)+24;
        dialogueHistoryScroll=GUI.BeginScrollView(view,dialogueHistoryScroll,new Rect(0,0,contentWidth,total));
        float y=6;
        foreach(var line in dialogueHistory) {
            float height=dialogueLog.CalcHeight(new GUIContent(line),contentWidth);
            Label(new Rect(0,y,contentWidth,height),line,dialogueLog);y+=height+24;
        }
        GUI.EndScrollView();
        if(GUI.Button(new Rect(panel.xMax-280,panel.y+14,250,45),"返回交谈  ·  Esc",dialogueOption))dialogueHistoryOpen=false;
    }
}
