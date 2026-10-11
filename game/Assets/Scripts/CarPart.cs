using UnityEngine;

// One authored pivot of the hotel car. Axes are car-local; every pivot is built
// with the car's orientation, so its closed local rotation is the identity.
public sealed class CarPart : MonoBehaviour {
    public enum Kind { Hinge, Slide, Screw, Spin, Steps, Gear, Gauge, Sweep, Wheel, Static }
    public string partId, fixtureId;
    public Kind kind;
    public Vector3 axis = Vector3.up;
    public float angle;
    public Vector3 offset;
    public float speed = 1.6f;
    public BoxCollider[] leaves = new BoxCollider[0];
    Quaternion closedRotation;
    Vector3 closedPosition;
    bool prepared;
    public float Fraction { get; private set; }
    public float Target { get; set; }
    public bool Open => Target > .5f;
    public bool Moving => Mathf.Abs(Fraction - Target) > .0005f;
    void Prepare() {
        if (prepared) return;
        prepared = true; closedRotation = transform.localRotation; closedPosition = transform.localPosition;
    }
    public void Snap(float target) { Prepare(); Target = target; Fraction = target; Apply(target); }
    public void Tick(float delta) {
        Prepare();
        if (!Moving) return;
        Fraction = Mathf.MoveTowards(Fraction, Target, delta * speed);
        Apply(Fraction);
    }
    float Ease(float t) => kind == Kind.Hinge || kind == Kind.Slide || kind == Kind.Screw ? Mathf.SmoothStep(0, 1, t) : t;
    public void Apply(float t) { float e = Ease(t); Pose(angle * e, offset * e); }
    public void Pose(float degrees, Vector3 translation) {
        Prepare();
        transform.localPosition = closedPosition + translation;
        transform.localRotation = closedRotation * Quaternion.AngleAxis(degrees, axis);
    }
    public void PoseEuler(Vector3 localEuler) {
        Prepare();
        transform.localPosition = closedPosition;
        transform.localRotation = closedRotation * Quaternion.Euler(localEuler);
    }
    // World pose of a leaf collider if this part stood at fraction t (parent unchanged).
    public void LeafPose(BoxCollider leaf, float t, out Vector3 centre, out Quaternion rotation, out Vector3 half) {
        Prepare();
        float e = Ease(t);
        var parent = transform.parent;
        var localRotation = closedRotation * Quaternion.AngleAxis(angle * e, axis);
        var localPosition = closedPosition + offset * e;
        var leafLocal = transform.InverseTransformPoint(leaf.transform.TransformPoint(leaf.center));
        var leafRotation = Quaternion.Inverse(transform.rotation) * leaf.transform.rotation;
        centre = parent.TransformPoint(localPosition + localRotation * leafLocal);
        rotation = parent.rotation * localRotation * leafRotation;
        var scale = leaf.transform.lossyScale;
        half = Vector3.Scale(leaf.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * .5f;
    }
    public void SetVisible(bool visible) {
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = visible;
    }
}
