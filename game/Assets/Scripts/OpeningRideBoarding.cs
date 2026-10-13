public sealed class OpeningRideBoarding : Interactable {
    public override string ActionText => DemoApp.Instance.state.openingRideStage==4?"和律师道别":"上车 · 去医院";
    public override void Use()=>DemoApp.Instance.UseOpeningRideCar();
}
