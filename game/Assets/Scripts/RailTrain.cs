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
    Vector3 parked;
    float speed, doorFraction;
    void Awake() {
        parked=transform.position;
        foreach(var door in doors)if(door)doorClosed[door]=door.localRotation;
        SetDoors(true);
    }
    public Vector3 SeatPosition=>rideSeat.position;
    public Vector3 StandPosition=>standPoint.position;
    public void SetDoors(bool open) {
        doorFraction=open?1:0;
        ApplyDoors();
    }
    void ApplyDoors() {
        foreach(var pair in doorClosed) {
            float angle=pair.Key.name.Contains("_S_")?-100*doorFraction:0;
            pair.Key.rotation=Quaternion.AngleAxis(angle,transform.up)*pair.Key.parent.rotation*pair.Value;
        }
    }
    public void BeginDeparture() {
        Departing=true; Travelled=0; speed=0;doorFraction=1;
        if(sound&&whistle)sound.PlayOneShot(whistle,.65f);
        if(steam)steam.Play();
    }
    public void StepDeparture(float delta) {
        if(!Departing)return;
        doorFraction=Mathf.MoveTowards(doorFraction,0,delta*1.6f);
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
