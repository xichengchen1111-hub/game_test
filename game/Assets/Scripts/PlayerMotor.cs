using UnityEngine;
public class PlayerMotor : MonoBehaviour {
    public const float WalkSpeed = 2.5f;
    public const float SprintSpeed = 6f;
    public Camera eye;
    public float pitch;
    public Vector3 scriptedDirection;
    // Feed the same normalized input/Shift path in focused native reviews.
    // Older scenic reviews retain analogue speed for stairs and close-ups.
    internal bool scriptedSprint, scriptedPlayerInput;
    internal float reviewSprintSpeed = SprintSpeed;
    public bool CreatorFlying { get; private set; }
    public Vector3 CreatorReturnPoint { get; private set; }
    float creatorReturnYaw;
    public void SetCreatorFlying(bool value) {
        if(value==CreatorFlying)return;
        if(value){CreatorReturnPoint=transform.position;creatorReturnYaw=transform.eulerAngles.y;CreatorFlying=true;capsule.enabled=false;}
        else {CreatorFlying=false;Place(CreatorReturnPoint,creatorReturnYaw);}
    }
    CharacterController capsule;
    float falling, sway;
    void Awake() { capsule = GetComponent<CharacterController>(); }
    public void Place(Vector3 feet, float yaw) {
        capsule.enabled = false;
        transform.position = feet;
        transform.rotation = Quaternion.Euler(0,yaw,0);
        pitch = 0;
        eye.transform.localRotation = Quaternion.identity;
        capsule.enabled = !CreatorFlying;
        falling = 0;
    }
    // Seat the player in a moving vehicle: the capsule is disabled and look stays relative to the seat.
    public void Mount(Transform seat) {
        capsule.enabled = false;
        transform.SetParent(seat, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        pitch = 0; falling = 0;
        eye.transform.localRotation = Quaternion.identity;
    }
    public void Dismount(Transform home, Vector3 feet, float yaw) {
        transform.SetParent(home, true);
        Place(feet, yaw);
        eye.transform.localPosition = new Vector3(0,1.66f,0);
    }
    public void Aim(Vector3 point) {
        // Solve yaw around the body pivot, in the parent's (possibly tilted seat)
        // frame. Turning moves an offset eye; aiming from its old position misses
        // small nearby controls. Ordinary mouse look already uses this local frame.
        Vector3 target = transform.parent ? transform.parent.InverseTransformPoint(point) - transform.localPosition : point - transform.position;
        Vector3 offset = Vector3.Scale(eye.transform.localPosition, transform.localScale);
        float radius = new Vector2(target.x, target.z).magnitude;
        float yaw = Mathf.Atan2(target.x, target.z) - Mathf.Asin(Mathf.Clamp(offset.x / Mathf.Max(radius, .0001f), -1, 1));
        transform.localRotation = Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
        Vector3 d = transform.InverseTransformDirection(point - eye.transform.position);
        pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
        eye.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
    }
    void Update() {
        if (!DemoApp.Instance) return;
        bool scripted = DemoApp.Instance.Review;
        if (DemoApp.Instance.LookAllowed && !scripted && Cursor.lockState == CursorLockMode.Locked) {
            transform.Rotate(0,Input.GetAxisRaw("Mouse X")*1.9f,0);
            pitch = Mathf.Clamp(pitch-Input.GetAxisRaw("Mouse Y")*1.9f,-72,72);
        }
        if(DemoApp.Instance.MovementLocked) {
            eye.transform.localRotation=Quaternion.Euler(pitch,0,0);return;
        }
        bool sprint = scripted ? scriptedSprint : Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Vector3 movement = scripted ? scriptedDirection : transform.TransformDirection(new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")));
        if (!scripted || scriptedPlayerInput) movement = Vector3.ClampMagnitude(movement,1);
        float speed = sprint ? (scripted ? reviewSprintSpeed : SprintSpeed) : WalkSpeed;
        if(CreatorFlying){
            Vector3 direction=scripted?scriptedDirection:eye.transform.TransformDirection(new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical")));
            if(!scripted){if(Input.GetKey(KeyCode.Space))direction+=Vector3.up;if(Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl))direction+=Vector3.down;}
            transform.position+=Vector3.ClampMagnitude(direction,1)*(sprint?30f:10f)*Time.deltaTime;
            eye.transform.localPosition=new Vector3(0,1.66f,0);eye.transform.localRotation=Quaternion.Euler(pitch,0,0);return;
        }
        // Keep descending feet on slopes/steps at sprint speed. A fixed -1m/s
        // loses contact when the stairs drop faster than that beneath the player.
        falling = capsule.isGrounded ? -Mathf.Max(2f,speed) : falling-18f*Time.deltaTime;
        capsule.Move((movement*speed + Vector3.up*falling)*Time.deltaTime);
        sway += Time.deltaTime*(movement.sqrMagnitude>0.05f?7f:1f);
        float bob = movement.sqrMagnitude>0.05f ? Mathf.Sin(sway)*0.016f : 0;
        eye.transform.localPosition = new Vector3(0,1.66f+bob,0);
        eye.transform.localRotation = Quaternion.Euler(pitch,0,0);
    }
}
