using UnityEngine;

// Manual lamp switch: off is saved as a stable fixture ID and wins over the
// daylight/weather schedule until the player switches the lamp on again.
public sealed class LampSwitch : Interactable {
    public string offId, label;
    public DayNightLamp[] lamps;
    public AudioSource clickAudio;
    public bool LampOn { get; private set; } = true;
    public override string ActionText => title;
    void Start() { Apply(!DemoApp.Instance || !DemoApp.Instance.state.openFixtures.Contains(offId)); }
    void Apply(bool on) {
        LampOn = on;
        foreach (var lamp in lamps) if (lamp) lamp.SetRequested(on);
        title = (on ? "关闭" : "打开") + label;
    }
    public override void Use() {
        var app = DemoApp.Instance;
        if (app && app.MovementLocked) return;
        Apply(!LampOn);
        if (clickAudio) clickAudio.Play();
        if (!app) return;
        app.state.openFixtures.RemoveAll(id => id == offId);
        if (!LampOn) app.state.openFixtures.Add(offId);
        app.Save();
    }
}
