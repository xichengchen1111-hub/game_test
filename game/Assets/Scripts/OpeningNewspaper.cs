using UnityEngine;
public sealed class OpeningNewspaper : Interactable {
    public override void Use() {
        if(DemoApp.Instance.state.openingStage<2){DemoApp.Instance.OpeningReminder("先听律师把话说完。");return;}
        base.Use();
    }
}
