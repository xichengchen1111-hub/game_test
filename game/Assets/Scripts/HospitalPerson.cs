using UnityEngine;

// The rest of the hospital: nurses, doctors, patients and visitors who only pass the time of
// day. No evidence, permission or plot state; their lines come from Resources/HospitalPeople.json.
public sealed class HospitalPerson : Interactable {
    public string personId;
    [TextArea] public string[] remarks;
    // Asleep under the covers: words only, no talking gestures.
    public bool quiet;
    int nextRemark;
    public override string ActionText=>"交谈 · "+title;
    public override void Use() {
        var app=DemoApp.Instance;if(!app||app.Blocked||remarks==null||remarks.Length==0)return;
        string line=remarks[nextRemark%remarks.Length];nextRemark++;
        app.OpeningReminder(title+"："+line);
        if(!quiet)GetComponentInChildren<PeriodCharacterPerformance>()?.Speak(line);
    }
}
