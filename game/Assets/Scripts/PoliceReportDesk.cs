using UnityEngine;

// The small desk inside the entrance: opens the player-composed stage report.
public sealed class PoliceReportDesk : Interactable {
    public AudioSource paperAudio;
    public override string ActionText => title;
    public override void Use() {
        if (paperAudio) paperAudio.Play();
        DemoApp.Instance.OpenPoliceReport();
    }
}
