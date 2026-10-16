using UnityEngine;

// The register desk's lamp in the inn's hall. Pressing its shade down swings the head about the
// joint so the light grazes the open register: the pressed writing on the next page (I02) then
// stands out and can be read; with the shade up that page shows only blank ruling. The state is
// saved like a door (openFixtures) and never changes by itself.
public sealed class RakingLamp : Interactable {
    public string fixtureId;
    public Transform head;
    public Vector3 axis = Vector3.right;
    public float angle = 55;
    public Light raking;
    public float rakingIntensity = .9f;
    public GameObject plainPage, rakedPage;
    public AudioSource sound;
    public bool Lowered { get; private set; }
    public float Fraction => Mathf.SmoothStep(0, 1, progress);
    float progress;
    Quaternion up;
    public override string ActionText => title;
    void Start() {
        up = head.localRotation;
        Lowered = DemoApp.Instance && DemoApp.Instance.state.openFixtures.Contains(fixtureId);
        progress = Lowered ? 1 : 0; Apply(); SetTitle();
    }
    void SetTitle() { title = Lowered ? "抬起台灯灯罩" : "压低台灯，斜照登记簿"; }
    public override void Use() {
        Lowered = !Lowered; SetTitle();
        var opened = DemoApp.Instance.state.openFixtures;
        if (Lowered) { if (!opened.Contains(fixtureId)) opened.Add(fixtureId); } else opened.Remove(fixtureId);
        if (sound) sound.Play();
        DemoApp.Instance.Save();
    }
    void Apply() {
        float t = Fraction;
        head.localRotation = Quaternion.AngleAxis(angle * t, axis) * up;
        if (raking) { raking.enabled = t > .01f; raking.intensity = rakingIntensity * t; }
        bool raked = t > .5f;
        if (rakedPage && rakedPage.activeSelf != raked) { rakedPage.SetActive(raked); Physics.SyncTransforms(); }
        if (plainPage && plainPage.activeSelf == raked) { plainPage.SetActive(!raked); Physics.SyncTransforms(); }
    }
    void Update() {
        float target = Lowered ? 1 : 0;
        if (Mathf.Abs(target - progress) < .001f) return;
        progress = Mathf.MoveTowards(progress, target, Time.deltaTime * 1.8f); Apply();
    }
}
