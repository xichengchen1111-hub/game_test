using UnityEngine;

/// <summary>Small, decorative motion of the physical clock train. It never maintains a second clock.</summary>
public sealed class ClockTowerGearMotion : MonoBehaviour {
    public Transform[] gears;
    public float[] degreesPerSecond;
    public Transform pendulum;
    public float pendulumAngle = 3.5f;
    Quaternion[] initial;
    Quaternion pendulumInitial;
    double elapsed;
    double[] angles;
    public double PrimaryTravelDegrees { get; private set; }

    void Awake() {
        initial = new Quaternion[gears.Length]; angles = new double[gears.Length];
        for (int i = 0; i < gears.Length; i++) initial[i] = gears[i].localRotation;
        if (pendulum) pendulumInitial = pendulum.localRotation;
    }

    void Update() {
        if (DemoApp.Instance && DemoApp.Instance.TowerMechanismPaused) return;
        elapsed += Time.deltaTime;
        float factor=DemoApp.Instance ? DemoApp.Instance.TowerWindingSpeedFactor : 1;
        for (int i = 0; i < gears.Length; i++) {
            double advance=Time.unscaledDeltaTime*degreesPerSecond[i]*factor;
            angles[i]+=advance;
            if(i==0) PrimaryTravelDegrees+=System.Math.Abs(advance);
            if(gears[i]) gears[i].localRotation=initial[i]*Quaternion.AngleAxis((float)(angles[i]%360),Vector3.forward);
        }
        if (pendulum) pendulum.localRotation = pendulumInitial *
            Quaternion.AngleAxis(Mathf.Sin((float)(elapsed % 2.3) * Mathf.PI * 2 / 2.3f) * pendulumAngle,
                Vector3.forward);
    }
}
