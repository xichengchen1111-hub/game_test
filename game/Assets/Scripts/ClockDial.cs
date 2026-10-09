using UnityEngine;
public class ClockDial : MonoBehaviour {
    public Transform minuteHand, hourHand;
    public bool streetClock;
    void LateUpdate() {
        if (!DemoApp.Instance) return;
        float t = DemoApp.Instance.Minutes + (streetClock ? (float)GameClock.TowerOffsetMinutes : 0f);
        if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0,0,-(t%60f)*6f);
        if (hourHand) hourHand.localRotation = Quaternion.Euler(0,0,-(t%720f)*0.5f);
    }
}
