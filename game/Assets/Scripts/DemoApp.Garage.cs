using UnityEngine;

// The E garage's mechanic (修车匠) conversation. Lines come from Resources/MechanicDialogue.json
// (the revised script's section 7 lines verbatim, plus two shop-life questions); the question about
// the neighbouring-city copies appears only once W01 is in the notebook. The conversation grants no
// evidence and never states who staged the scene.
public partial class DemoApp {
    DialogueScript mechanicScript;
    void OpenMechanicDialogue(Interactable item) {
        mechanicScript ??= DialogueScript.Load("MechanicDialogue");
        milkboyScript = mechanicScript;
        BindDialoguePerformance(item);
        dialogueHistory.Clear(); dialogueHistoryOpen = false; player.scriptedDirection = Vector3.zero;
        VisitDialogue(milkboyScript.first); CursorMode(); Debug.Log("DIALOGUE_OPEN: mechanic");
    }
}
