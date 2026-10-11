using UnityEngine;

// Rain darkens stone, brick, slate and timber and gives them a wet sheen; surfaces stay
// damp for about half an hour after a shower and then dry. Driven only by the shared
// weather schedule on the saved clock: no timer of its own, nothing saved.
// Shared materials are changed in memory and restored when this component goes away.
public sealed class TownWetness : MonoBehaviour {
    [System.Serializable] public struct Entry { public Material material; public float wetSmoothness; public float wetDarken; }
    public Entry[] entries = new Entry[0];
    Color[] baseColours; float[] baseSmoothness;
    float applied = -1, next;
    public float Wetness { get; private set; }
    public static TownWetness Instance { get; private set; }
    const int DryingSteps = 6; const double StepMinutes = 6;

    void Awake() {
        Instance = this;
        baseColours = new Color[entries.Length]; baseSmoothness = new float[entries.Length];
        for (int i = 0; i < entries.Length; i++) if (entries[i].material) {
            baseColours[i] = entries[i].material.GetColor("_BaseColor"); baseSmoothness[i] = entries[i].material.GetFloat("_Smoothness");
        }
    }
    public static float Evaluate(double elapsed) {
        float wet = 0;
        for (int k = 0; k <= DryingSteps; k++)
            wet = Mathf.Max(wet, TownWeather.Evaluate(elapsed - k * StepMinutes).rain * (1 - k / (DryingSteps + 1f)));
        return wet;
    }
    void Update() {
        var cycle = DayNightCycle.Instance;
        if (Time.unscaledTime < next || !cycle) return;
        next = Time.unscaledTime + .25f;
        Wetness = Mathf.Clamp01(cycle.WeatherStaged ? cycle.Weather.rain : Mathf.Max(cycle.Weather.rain, Evaluate(cycle.AppliedElapsedMinutes)));
        if (Mathf.Abs(Wetness - applied) < .01f) return;
        applied = Wetness;
        for (int i = 0; i < entries.Length; i++) {
            var e = entries[i]; if (!e.material) continue;
            e.material.SetFloat("_Smoothness", Mathf.Lerp(baseSmoothness[i], Mathf.Max(baseSmoothness[i], e.wetSmoothness), Wetness));
            e.material.SetColor("_BaseColor", baseColours[i] * Mathf.Lerp(1, e.wetDarken, Wetness));
        }
    }
    void OnDestroy() {
        if (Instance == this) Instance = null;
        if (baseColours == null) return;
        for (int i = 0; i < entries.Length; i++) if (entries[i].material) {
            entries[i].material.SetFloat("_Smoothness", baseSmoothness[i]); entries[i].material.SetColor("_BaseColor", baseColours[i]);
        }
    }
}
