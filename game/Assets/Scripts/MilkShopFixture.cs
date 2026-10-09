using UnityEngine;

// Compact actions on the existing counter; no dialogue, quest, or evidence unlock.
public class MilkShopFixture : Interactable {
    public enum Action { ReadingLamp, Bell }
    public Action action;
    public Light readingLight;
    public Renderer readingBulb;
    public AudioSource bellAudio;
    public DayNightLamp scheduledLamp;
    public bool LampOn { get; private set; } = true;
    public int BellRings { get; private set; }
    const string OffId="milk-reading-lamp-off";
    MaterialPropertyBlock bulbProperties;
    void Start() {
        if(action==Action.ReadingLamp)SetLamp(!DemoApp.Instance||!DemoApp.Instance.state.openFixtures.Contains(OffId));
    }
    void SetLamp(bool on) {
        LampOn=on;
        if(scheduledLamp)scheduledLamp.SetRequested(on);
        else if(readingLight)readingLight.enabled=on;
        if(readingBulb&&!scheduledLamp) {
            if(bulbProperties==null)bulbProperties=new MaterialPropertyBlock();
            readingBulb.GetPropertyBlock(bulbProperties);
            bulbProperties.SetColor("_EmissionColor",on?new Color(1,.65f,.25f)*2:Color.black);
            readingBulb.SetPropertyBlock(bulbProperties);
        }
        title=on?"关闭阅读灯":"打开阅读灯";
    }
    public override void Use() {
        if(action==Action.Bell) {
            BellRings++;
            if(bellAudio)bellAudio.Play();
            return;
        }
        SetLamp(!LampOn);
        var app=DemoApp.Instance;
        if(!app)return;
        app.state.openFixtures.RemoveAll(id=>id==OffId);
        if(!LampOn)app.state.openFixtures.Add(OffId);
        app.Save();
    }
}
