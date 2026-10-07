using UnityEngine;
public class PlayerMotor : MonoBehaviour {
    public Camera eye;
    public float pitch;
    public Vector3 scriptedDirection;
    CharacterController capsule;
    float falling, sway;
    void Awake() { capsule = GetComponent<CharacterController>(); }
    public void Place(Vector3 feet, float yaw) {
        capsule.enabled = false;
        transform.position = feet;
        transform.rotation = Quaternion.Euler(0,yaw,0);
        pitch = 0;
        eye.transform.localRotation = Quaternion.identity;
        capsule.enabled = true;
        falling = 0;
    }
    public void Aim(Vector3 point) {
        Vector3 d = point - eye.transform.position;
        transform.rotation = Quaternion.Euler(0,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);
        pitch = -Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
        eye.transform.localRotation = Quaternion.Euler(pitch,0,0);
    }
    void Update() {
        if (!DemoApp.Instance || DemoApp.Instance.Blocked) return;
        bool scripted = DemoApp.Instance.Review;
        if (!scripted && Cursor.lockState == CursorLockMode.Locked) {
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*1.9f,0);
            pitch = Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.9f,-72,72);
        }
        Vector3 movement = scripted ? scriptedDirection : transform.TransformDirection(new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"))).normalized;
        float speed = !scripted && Input.GetKey(KeyCode.LeftShift) ? 4.1f : 2.5f;
        falling = capsule.isGrounded ? -1f : falling-18f*Time.deltaTime;
        capsule.Move((movement*speed + Vector3.up*falling)*Time.deltaTime);
        sway += Time.deltaTime*(movement.sqrMagnitude>0.05f?7f:1f);
        float bob = movement.sqrMagnitude>0.05f ? Mathf.Sin(sway)*0.016f : 0;
        eye.transform.localPosition = new Vector3(0,1.66f+bob,0);
        eye.transform.localRotation = Quaternion.Euler(pitch,0,0);
    }
}
