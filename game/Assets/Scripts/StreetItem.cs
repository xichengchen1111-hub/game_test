using UnityEngine;

// A placed piece of 1931 street furniture (docs/maps/town-current/plan-data.json streetItems).
// Pure dressing: no interaction, evidence, clock, light source or saved state.
public sealed class StreetItem : MonoBehaviour {
    public string id, kind, facing;
    public Vector2 footprint;
    public float height;
}
