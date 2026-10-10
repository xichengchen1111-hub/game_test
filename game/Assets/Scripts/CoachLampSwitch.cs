using UnityEngine;

public sealed class CoachLampSwitch : Interactable {
    public string offId,coachName;
    public Transform knob;
    public DayNightLamp[] lamps;
    public bool LampOn{get;private set;}=true;
    Quaternion knobRest;
    public override string ActionText=>title;
    void Awake(){if(knob)knobRest=knob.localRotation;}
    void Start(){Apply(!DemoApp.Instance||!DemoApp.Instance.state.openFixtures.Contains(offId));}
    void Apply(bool on) {
        LampOn=on;
        foreach(var lamp in lamps)if(lamp)lamp.SetRequested(on);
        if(knob)knob.rotation=Quaternion.AngleAxis(on?0:65,-transform.forward)*knob.parent.rotation*knobRest;
        title=(on?"关闭车厢灯":"打开车厢灯")+" · "+coachName;
    }
    public override void Use() {
        if(DemoApp.Instance.MovementLocked)return;
        Apply(!LampOn);
        var app=DemoApp.Instance;
        app.state.openFixtures.RemoveAll(id=>id==offId);
        if(!LampOn)app.state.openFixtures.Add(offId);
        app.Save();
    }
}
