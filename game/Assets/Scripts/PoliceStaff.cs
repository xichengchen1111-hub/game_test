using UnityEngine;

// Optional environmental conversation: no evidence, permission or plot state.
public sealed class PoliceStaff : Interactable {
    public string staffId;
    [TextArea] public string[] remarks;
    int nextRemark;
    public override string ActionText=>"交谈 · "+title;
    public override void Use() {
        var app=DemoApp.Instance;if(!app||app.Blocked)return;
        string line;
        if(remarks!=null&&remarks.Length>0){line=remarks[nextRemark%remarks.Length];nextRemark++;}
        else line=staffId=="ReceptionOfficer"?"会见室在门厅东侧。来访的事情，可以先到值班窗口问问。":
            staffId=="PatrolOfficer"?"请给过道留些空当。有事可以到门厅找值班警员。":"有事请到门厅的值班窗口询问。";
        if(string.IsNullOrWhiteSpace(line))return;
        app.OpeningReminder(title+"："+line);
        var performance=GetComponent<PoliceStaffPerformance>();
        if(performance)performance.Speak(line);
        else {
            var character=GetComponentInChildren<PeriodCharacterPerformance>();
            if(character)character.Speak(line);else GetComponentInChildren<StationClerkPerformance>()?.Speak(line);
        }
    }
}
