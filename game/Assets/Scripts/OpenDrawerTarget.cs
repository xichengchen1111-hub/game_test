using UnityEngine;

// Keeps a drawer's contents unreadable (no collider) until the drawer is pulled open.
public sealed class OpenDrawerTarget : MonoBehaviour {
    public ShopMechanism drawer;
    Collider target;
    void Awake() { target = GetComponent<Collider>(); }
    void Update() { bool open = drawer && drawer.OpenFraction > .8f; if (target.enabled != open) { target.enabled = open; Physics.SyncTransforms(); } }
}
