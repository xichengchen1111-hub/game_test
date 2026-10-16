using UnityEngine;

// The property room's wire-mesh cage, with a door the guard unlocks and swings open to take the
// player's briefcase out (user, 2026-10-15: the case used to slide out of the closed cage by itself).
// Built by OpeningAuthoring in place of the station's one-piece cage mesh.
public sealed class OpeningCage : MonoBehaviour {
    // Pivot on the hinge post; the leaf runs from it to the free edge along the pivot's local -X.
    public Transform door;
    public float width=.63f,openDegrees=-100;
    // On the free edge at the lock, and where the briefcase stands inside on the stacked boxes.
    public Transform lockPoint,caseHome;
    public AudioSource sound;
    public AudioClip unlockClip,openClip;
    float open;
    // 0 shut .. 1 standing open.
    public float Open {
        get=>open;
        set{open=Mathf.Clamp01(value);if(door)door.localRotation=Quaternion.Euler(0,openDegrees*open,0);}
    }
    // The free edge of the leaf at a height, wherever the leaf stands now.
    public Vector3 Edge(float worldHeight,float inset=.03f){var p=door.TransformPoint(new Vector3(-(width-inset),0,0));p.y=worldHeight;return p;}
    public Vector3 Lock=>lockPoint?lockPoint.position:Edge(transform.position.y+1);
    // Out of the cage, toward the room (the side the door swings to).
    public Vector3 Outward=>-transform.forward;
    public void Play(AudioClip clip){if(sound&&clip)sound.PlayOneShot(clip);}
}
