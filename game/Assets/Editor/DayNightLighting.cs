using System.Linq;
using UnityEngine;

// Bind in the shared generator, never maintain separate Town/interior lighting copies.
public static class DayNightLighting {
    public static DayNightLamp Bind(Light light, DayNightLamp.Role role, float access = .85f, params Renderer[] emission) {
        var controller = light.gameObject.AddComponent<DayNightLamp>();
        controller.source = light; controller.fullIntensity = light.intensity;
        controller.role = role; controller.daylightAccess = access; controller.glowingSurfaces = emission;
        return controller;
    }
    public static Renderer[] Glowing(Transform root) => root.GetComponentsInChildren<Renderer>().Where(r => r.sharedMaterials.Any(m => m && m.IsKeywordEnabled("_EMISSION"))).ToArray();
    public static Renderer[] ModuleGlow(Transform root, string module) {
        var part = root.GetComponentsInChildren<Transform>().Single(t => t.name == module);
        return Glowing(part);
    }
}
