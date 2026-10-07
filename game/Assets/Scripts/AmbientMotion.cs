using UnityEngine;
public class AmbientMotion : MonoBehaviour {
    public float amplitude = 0.008f, speed = 1f;
    Vector3 origin;
    void Start() { origin = transform.localPosition; }
    void Update() { transform.localPosition = origin + Vector3.up * Mathf.Sin(Time.time*speed)*amplitude; }
}
