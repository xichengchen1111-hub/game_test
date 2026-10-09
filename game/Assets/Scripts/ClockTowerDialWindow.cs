using UnityEngine;
// Marker for the actual shared two-sided dial assembly, not an interior clone.
public sealed class ClockTowerDialWindow : MonoBehaviour {
    public MeshRenderer glazing;
    public BoxCollider safety;
    public ClockDial Dial => GetComponent<ClockDial>();
}
