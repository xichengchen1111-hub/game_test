using UnityEngine;

// Environment lines use the same speaker protocol as the principal actors;
// existing placeholder prefabs remain compatible with the two-clip component.
public sealed class PoliceStaffPerformance : MonoBehaviour {
    public StationClerkPerformance speech;
    public PeriodCharacterPerformance character;
    public bool IsSpeaking=>character?character.IsSpeaking:speech&&speech.IsSpeaking;
    void Awake(){Resolve();}
    void Resolve(){if(!character)character=GetComponentInChildren<PeriodCharacterPerformance>();if(!speech)speech=GetComponentInChildren<StationClerkPerformance>();}
    public void Speak(string line) {
        Resolve();
        if(character)character.Speak(line);else if(speech)speech.Speak(line);
    }
    public void Listen(){if(character)character.Listen();if(speech)speech.Listen();}
    void OnDisable(){Listen();}
}
