using UnityEngine;

// The A inn (B08): the porter at the counter and the landlady at the dining cash desk. Lines come
// from Resources/HotelPorterDialogue.json and HotelKeeperDialogue.json. Asking the porter about the
// evening of the 20th records his testimony (I04); nothing else is granted by talk, and neither
// speaker says which clock was wrong, where the husband went or who killed anyone. The figures are
// placeholders until the teammate's characters arrive.
public partial class DemoApp {
    DialogueScript hotelScript;
    public void OpenHotelActor(HotelActor actor) {
        if(Blocked||OpeningRideActive)return;
        hotelScript=DialogueScript.Load(actor.role=="porter"?"HotelPorterDialogue":"HotelKeeperDialogue");
        milkboyScript=hotelScript;BindDialoguePerformance(actor);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        string prefix=actor.role=="porter"?"hotel.porter.":"hotel.keeper.";
        VisitDialogue(state.dialogueTopics.Exists(t=>t.StartsWith(prefix))?milkboyScript.revisit:milkboyScript.first);
        CursorMode();Debug.Log("DIALOGUE_OPEN: hotel-"+actor.role);
    }
}
