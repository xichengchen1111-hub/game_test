using System.Collections.Generic;
using UnityEngine;

// Keep distant lamp bulbs visible, but limit expensive point-light pools around the player.
public sealed class TownLightBudget : MonoBehaviour {
    public const int MaximumPools=12;
    public const float FadeStart=25,FadeEnd=35;
    // Car headlamps and dome lamp borrow pools from the same budget while they are lit.
    public static int Reserved;
    readonly List<DayNightLamp> lamps=new List<DayNightLamp>();
    float next;
    void Start() { foreach(var lamp in DayNightLamp.Active)if(lamp.source&&lamp.role==DayNightLamp.Role.Exterior)lamps.Add(lamp); }
    void Update() {
        if(Time.unscaledTime<next||!DemoApp.Instance)return;
        next=Time.unscaledTime+.25f;
        var p=DemoApp.Instance.player.transform.position;
        lamps.Sort((a,b)=>(a.transform.position-p).sqrMagnitude.CompareTo((b.transform.position-p).sqrMagnitude));
        for(int i=0;i<lamps.Count;i++)if(lamps[i])lamps[i].SetDistanceVisibility(i<MaximumPools-Reserved?1-DayNightCycle.Smooth(FadeStart,FadeEnd,Vector3.Distance(p,lamps[i].transform.position)):0);
    }
}
