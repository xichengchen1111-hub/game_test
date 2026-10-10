using System.Collections.Generic;
using UnityEngine;

// The same coach definition and prefab serve departure, arrival and reload.
public sealed class RailTrain : MonoBehaviour {
    public string destination="OtherTown";
    public Transform rideSeat, standPoint, boardingPoint;
    public Transform[] wheels, rods, doors;
    public ParticleSystem chimney, steam;
    public AudioSource sound;
    public AudioClip whistle, running;
    public bool Departing { get; private set; }
    public float Travelled { get; private set; }
    readonly Dictionary<Transform,Quaternion> doorClosed=new Dictionary<Transform,Quaternion>();
    readonly Dictionary<Transform,float> doorFractions=new Dictionary<Transform,float>();
    readonly Dictionary<Transform,bool> doorTargets=new Dictionary<Transform,bool>();
    Vector3 parked;
    float speed, doorFraction;
    void Awake() {
        parked=transform.position;
        foreach(var door in doors)if(door) {
            doorClosed[door]=door.localRotation;
            bool platform=door.name.Contains("_S_");
            doorTargets[door]=platform;doorFractions[door]=platform?1:0;
        }
        ApplyDoors();
    }
    public Vector3 SeatPosition=>rideSeat.position;
    public Vector3 StandPosition=>standPoint.position;
    public void SetDoors(bool open) {
        foreach(var door in doorClosed.Keys){doorTargets[door]=open;doorFractions[door]=open?1:0;}
        ApplyDoors();
    }
    public bool DoorOpen(Transform door)=>doorTargets.TryGetValue(door,out bool open)&&open;
    public bool ToggleDoor(Transform door) {
        if(Departing||!doorTargets.ContainsKey(door))return false;
        doorTargets[door]=!doorTargets[door];return true;
    }
    void Update() {
        if(Departing||(DemoApp.Instance&&DemoApp.Instance.GameTimePaused))return;
        foreach(var door in doorClosed.Keys) {
            float next=Mathf.MoveTowards(doorFractions[door],doorTargets[door]?1:0,Time.deltaTime*1.7f);
            if(Mathf.Abs(next-doorFractions[door])<.00001f)continue;
            var old=door.rotation;ApplyDoor(door,next);
            var handle=door.GetComponent<RailDoor>();
            var app=DemoApp.Instance;
            // Both opening and closing stop if the leaf would overlap the passenger.
            if(app&&app.player) {
                var body=app.player.GetComponent<CharacterController>();
                if(handle.OccupiedBy(body)) {
                    door.rotation=old;doorTargets[door]=true;continue;
                }
            }
            doorFractions[door]=next;
        }
    }
    void ApplyDoor(Transform door,float fraction) {
        float angle=(door.name.Contains("_S_")?100:-100)*fraction;
        door.rotation=Quaternion.AngleAxis(angle,transform.up)*door.parent.rotation*doorClosed[door];
    }
    void ApplyDoors() {
        foreach(var pair in doorClosed)ApplyDoor(pair.Key,doorFractions[pair.Key]);
    }
    public void BeginDeparture() {
        Departing=true; Travelled=0; speed=0;
        foreach(var door in doorClosed.Keys)doorTargets[door]=false;
        if(sound&&whistle)sound.PlayOneShot(whistle,.65f);
        if(steam)steam.Play();
    }
    public void StepDeparture(float delta) {
        if(!Departing)return;
        doorFraction=0;
        foreach(var door in doorClosed.Keys) {
            doorFractions[door]=Mathf.MoveTowards(doorFractions[door],0,delta*1.6f);
            doorFraction=Mathf.Max(doorFraction,doorFractions[door]);
        }
        ApplyDoors();
        if(doorFraction>0)return;
        speed=Mathf.MoveTowards(speed,3.2f,delta*.85f);
        float distance=speed*delta; Travelled+=distance;
        transform.position=parked+Vector3.right*Travelled;
        foreach(var wheel in wheels)if(wheel) {
            var specification=wheel.GetComponent<RailWheel>();float radius=specification?specification.radius:.73f;
            wheel.Rotate(transform.forward,-distance/radius*Mathf.Rad2Deg,Space.World);
        }
        float phase=Travelled/.73f;
        foreach(var rod in rods)if(rod) {
            var marker=rod.GetComponent<RailRod>();if(marker)rod.position=rod.parent.TransformPoint(marker.rest)+transform.TransformVector(new Vector3(Mathf.Cos(phase)*.14f,Mathf.Sin(phase)*.14f,0));
        }
        if(sound&&running&&!sound.isPlaying){sound.clip=running;sound.loop=true;sound.volume=.24f;sound.Play();}
        if(sound)sound.pitch=Mathf.Lerp(.65f,1.15f,speed/3.2f);
    }
}
