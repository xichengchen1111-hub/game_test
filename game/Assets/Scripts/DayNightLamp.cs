using System.Collections.Generic;
using UnityEngine;

// Authored aperture access approximates natural illumination, excluding this lamp's own light.
// This avoids on/off feedback and retains task lighting in deep rooms during the day.
public sealed class DayNightLamp : MonoBehaviour {
    public enum Role { Exterior, Interior, WindowDaylight }
    public Role role;
    public Light source;
    public float fullIntensity;
    [Range(0, 1)] public float daylightAccess = .85f;
    public Renderer[] glowingSurfaces = new Renderer[0];
    public bool darkenWindowWhenOff;
    public bool RequestedOn { get; private set; } = true;
    public float Output { get; private set; }
    public float NaturalIllumination { get; private set; }
    public static readonly HashSet<DayNightLamp> Active = new HashSet<DayNightLamp>();
    struct Surface { public Renderer renderer; public int slot; public Color emission, baseColor; }
    readonly List<Surface> surfaces = new List<Surface>();
    MaterialPropertyBlock properties;
    bool prepared;
    bool applied;
    float distanceVisibility=1;
    public void SetDistanceVisibility(float value) {
        distanceVisibility=Mathf.Clamp01(value);
        if(source){source.intensity=fullIntensity*Output*distanceVisibility;source.enabled=Output*distanceVisibility>.001f;}
    }

    void Awake() { Prepare(); }
    void OnEnable() {
        Prepare(); Active.Add(this);
        ApplyCurrent();
    }
    void OnDisable() { Active.Remove(this); }
    void Prepare() {
        if (prepared) return;
        prepared = true;
        if (source && fullIntensity <= 0) fullIntensity = source.intensity;
        properties = new MaterialPropertyBlock();
        foreach (var renderer in glowingSurfaces) {
            if (!renderer) continue;
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) {
                var material = materials[i];
                if (!material || !material.HasProperty("_EmissionColor") || !material.IsKeywordEnabled("_EMISSION")) continue;
                surfaces.Add(new Surface { renderer = renderer, slot = i, emission = material.GetColor("_EmissionColor"), baseColor = material.GetColor("_BaseColor") });
            }
        }
    }
    public void SetRequested(bool requested) {
        RequestedOn = requested;
        ApplyCurrent();
    }
    void ApplyCurrent() {
        var cycle=DayNightCycle.Instance;
        float daylight=cycle ? cycle.Daylight : DayNightCycle.NaturalDaylight(DemoApp.Instance ? DemoApp.Instance.Minutes : GameClock.InitialMinutes);
        Apply(daylight,cycle ? cycle.NaturalIllumination : daylight);
    }
    public static float AutomaticOutput(Role role, float daylight, float access) {
        if (role == Role.WindowDaylight) return .14f + .86f * daylight;
        // 42% or less: fully lit; 60% or more: off. The smooth band avoids abrupt switching.
        if (role == Role.Exterior) return 1 - DayNightCycle.Smooth(.42f, .60f, daylight);
        return 1 - DayNightCycle.Smooth(.35f, .60f, daylight * access);
    }
    public void Apply(float daylight) {
        Apply(daylight,daylight);
    }
    public void Apply(float daylight,float naturalLight) {
        Prepare();
        NaturalIllumination = naturalLight * (role==Role.Exterior ? 1 : daylightAccess);
        float next = RequestedOn ? AutomaticOutput(role, naturalLight, daylightAccess) : 0;
        // Update disabled lights and emission together. The smooth illumination curve
        // has no thresholds that can oscillate and freezes with the game clock.
        if (applied && Mathf.Abs(Output - next) < .00001f && (!source || source.enabled == (next*distanceVisibility > .001f))) return;
        applied = true;
        Output = next;
        if (source) { source.intensity = fullIntensity * next*distanceVisibility; source.enabled = next*distanceVisibility > .001f; }
        foreach (var surface in surfaces) {
            if (!surface.renderer) continue;
            surface.renderer.GetPropertyBlock(properties, surface.slot);
            properties.SetColor("_EmissionColor", surface.emission * next);
            if (darkenWindowWhenOff) properties.SetColor("_BaseColor", Color.Lerp(new Color(.31f, .38f, .45f), surface.baseColor, next));
            surface.renderer.SetPropertyBlock(properties, surface.slot);
            properties.Clear();
        }
    }
}
