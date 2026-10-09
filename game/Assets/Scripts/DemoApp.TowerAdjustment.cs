using UnityEngine;

public partial class DemoApp {
    const float TowerWindingSeconds = 3f;
    string towerAdjustmentFeedback = "";
    bool towerAdjustmentBusy;
    float towerWindingAge;
    double towerWindingStart, towerWindingDelta;
    public bool TowerAdjustmentBusy => towerAdjustmentBusy;
    public bool TowerAdjustmentOpen => inspected is ClockTowerAdjustment;
    public bool TowerMechanismPaused => opening || paused || transitioning || journal || DialogueOpen || (inspected && !TowerAdjustmentOpen);
    public float TowerWindingSpeedFactor {
        get {
            if (!towerAdjustmentBusy) return 1;
            float u=towerWindingAge/TowerWindingSeconds;
            float envelope=u<.12f ? Mathf.SmoothStep(0,1,u/.12f) : u>.55f ? 1-Mathf.SmoothStep(0,1,(u-.55f)/.45f) : 1;
            return 1+240*envelope*(towerWindingDelta<0?-1:1);
        }
    }
    // Integrate the acceleration/cruise/deceleration envelope. Dial adjustment
    // follows the same drive cycle rather than jumping when the button is pressed.
    static double WindingFraction(float u) {
        u=Mathf.Clamp01(u);double v,area;
        if(u<.12f) {v=u/.12;area=.12*(v*v*v-.5*v*v*v*v);}
        else if(u<.55f) area=.06+(u-.12);
        else {v=(u-.55)/.45;area=.49+.45*(v-v*v*v+.5*v*v*v*v);}
        return System.Math.Min(1,System.Math.Max(0,area/.715));
    }
    public double TowerClockOffsetMinutes => GameClock.TowerOffsetMinutes +
        (towerAdjustmentBusy ? towerWindingStart+towerWindingDelta*WindingFraction(towerWindingAge/TowerWindingSeconds) : state.towerAdjustmentMinutes);
    static readonly int[] TowerAdjustments = { -60, -30, 30, 60, 0 };
    static readonly string[] TowerAdjustmentLabels = { "1  ·  -1 小时", "2  ·  -30 分钟", "3  ·  +30 分钟", "4  ·  +1 小时", "5  ·  Reset / 复位" };

    void TickTowerAdjustment() {
        if(!towerAdjustmentBusy || TowerMechanismPaused) return;
        towerWindingAge=Mathf.Min(TowerWindingSeconds,towerWindingAge+Time.unscaledDeltaTime);
        if(towerWindingAge<TowerWindingSeconds) return;
        towerAdjustmentBusy=false;towerAdjustmentFeedback="调校完成。";
    }
    void UpdateTowerAdjustmentInput() {
        for(int i=0;i<5;i++)
            if(Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)) || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1+i))) {
                ApplyTowerAdjustment(i);break;
            }
    }
    void ApplyTowerAdjustment(int choice) {
        // Guard the action itself: disabled buttons, hotkeys, reopening and rapid
        // repeated calls must all obey the same lock, with no queued actions.
        if(!TowerAdjustmentOpen || towerAdjustmentBusy || choice<0 || choice>=TowerAdjustments.Length) return;
        towerWindingStart=state.towerAdjustmentMinutes;
        towerWindingDelta=choice==4 ? -towerWindingStart : TowerAdjustments[choice];
        state.towerAdjustmentMinutes=choice==4 ? 0 : (int)(((long)state.towerAdjustmentMinutes+TowerAdjustments[choice])%GameClock.MinutesPerDay);
        towerWindingAge=0;towerAdjustmentBusy=true;towerAdjustmentFeedback="机芯正在调校，等待齿轮慢下来……";
        // Commit the chosen final setting atomically. Quitting mid-animation
        // restores that completed setting, never an unsaved fractional dial.
        Save();
    }
    bool TowerAdjustmentButton(Rect rect,string label) {
        bool hover=GUI.enabled&&rect.Contains(Event.current.mousePosition);
        Box(rect,new Color(.035f,.075f,.085f,GUI.enabled?(hover?.50f:.27f):.14f));
        Box(new Rect(rect.x,rect.y,rect.width,1),new Color(.65f,.69f,.64f,GUI.enabled?.42f:.16f));
        return GUI.Button(rect,label,button);
    }
    void TowerAdjustmentLabel(Rect rect,string label,GUIStyle style) {
        var shadow=new GUIStyle(style);shadow.normal.textColor=new Color(0,0,0,.7f);
        Label(new Rect(rect.x+1,rect.y+1,rect.width,rect.height),label,shadow);Label(rect,label,style);
    }
    void DrawTowerAdjustment() {
        Rect p=new Rect(W/2-500,H-255,1000,218);
        Box(p,new Color(.035f,.065f,.085f,.38f));
        Box(new Rect(p.x,p.y,p.width,1),new Color(.68f,.5f,.3f,.65f));
        TowerAdjustmentLabel(new Rect(p.x+28,p.y+18,600,46),"钟塔机芯 · 调时",title);
        TowerAdjustmentLabel(new Rect(p.x+30,p.y+65,870,30),towerAdjustmentBusy?"机芯正在调校，等待齿轮慢下来……":towerAdjustmentFeedback.Length>0?towerAdjustmentFeedback:"转动调时轮，带动外面的钟针。",small);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!towerAdjustmentBusy;
        for(int i=0;i<5;i++)
            if(TowerAdjustmentButton(new Rect(p.x+28+i*190,p.y+109,184,52),TowerAdjustmentLabels[i])) ApplyTowerAdjustment(i);
        GUI.enabled=enabled;
        TowerAdjustmentLabel(new Rect(p.x+30,p.y+175,610,27),towerAdjustmentBusy?"调校中，请稍候":"数字键 1–5 或点击操作",small);
        if(TowerAdjustmentButton(new Rect(p.x+748,p.y+169,225,33),"收起 · Esc")) CloseInspect();
    }
}
