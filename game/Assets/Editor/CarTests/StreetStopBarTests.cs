using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// STREET-04: the stop bars and STOP posts in the street dressing (PeriodStreets, written by
// tools/build_period_streets.py) stand on the stop lines the traffic works out at load (StreetNet; the
// north–south ones set back for turning cars, STREET-03). The generator works the lines out again with
// its own copy of the rule, reading the car's size and margins from AmbientCar.cs and StreetNet.cs; if the
// rule, the size or the margins change and the dressing is not generated again, the paint drifts off the
// lines the cars stop at. Each bar's centre lies 0.10 m on the junction side of its lane's line (its near
// edge 2 cm short of it), and each STOP post stands on its bar's centre line. This reads the dressing, not
// the town scene built from it.
public class StreetStopBarTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    [Serializable] class Dressing{public Surface[] surfaces;public Post[] stops;}
    [Serializable] class Surface{public string name;public float[] vertices;}
    [Serializable] class Post{public float x,z;}
    sealed class Stop{public string lane;public bool sign;public Vector3 end,dir,centre;public Surface bar;}
    const float Inside=.1f,Within=.005f;
    static Dressing dressing;static List<Stop> stops;
    static bool IsBar(Surface s)=>s.name.StartsWith("stopline-")||s.name.StartsWith("signalbar-");
    // Every lane that stops at a STOP sign or at the signal, with the bar painted across it nearest its line.
    static List<Stop> Stops() {
        if(stops!=null)return stops;
        var period=Resources.Load<TextAsset>("PeriodStreets");Assert.IsNotNull(period,"Resources/PeriodStreets");
        dressing=JsonUtility.FromJson<Dressing>(period.text);
        var net=T("StreetNet").GetMethod("Build").Invoke(null,new object[]{T("TownPlan").GetProperty("Current").GetValue(null),period.text});
        var bars=dressing.surfaces.Where(IsBar).ToList();var found=new List<Stop>();
        foreach(var lane in (IEnumerable)net.GetType().GetField("lanes").GetValue(net)) {
            object F(string name)=>lane.GetType().GetField(name).GetValue(lane);
            bool sign=(bool)F("stopSign");if(!sign&&(int)F("signalAxis")<0)continue;
            var end=(Vector3)F("end");var dir=(Vector3)F("dir");bool ns=Mathf.Abs(dir.z)>.5f;
            var stop=new Stop{lane=lane.ToString(),sign=sign,end=new Vector3(end.x,0,end.z),dir=dir};float nearest=float.MaxValue;
            foreach(var bar in bars) {
                float x0=float.MaxValue,x1=float.MinValue,z0=float.MaxValue,z1=float.MinValue;var v=bar.vertices;
                for(int i=0;i+2<v.Length;i+=3){x0=Mathf.Min(x0,v[i]);x1=Mathf.Max(x1,v[i]);z0=Mathf.Min(z0,v[i+2]);z1=Mathf.Max(z1,v[i+2]);}
                // Painted across this lane: long across it and over its line.
                if(ns?!(x1-x0>z1-z0&&end.x>x0&&end.x<x1):!(z1-z0>x1-x0&&end.z>z0&&end.z<z1))continue;
                var centre=ns?new Vector3(end.x,0,(z0+z1)/2):new Vector3((x0+x1)/2,0,end.z);
                float along=Mathf.Abs(Vector3.Dot(centre-stop.end,dir));
                if(along<nearest){nearest=along;stop.bar=bar;stop.centre=centre;}
            }
            found.Add(stop);
        }
        return stops=found;
    }
    static string At(Vector3 p)=>"("+p.x.ToString("0.000")+", "+p.z.ToString("0.000")+")";
    [Test] public void EveryStopLineHasItsBarPaintedJustInside() {
        var all=Stops();var wrong=new List<string>();float worst=0;
        Assert.AreEqual(20,all.Count,"lanes stopping at a STOP sign or the signal");
        Assert.AreEqual(20,dressing.surfaces.Count(IsBar),"painted stop bars");
        foreach(var s in all) {
            if(s.bar==null){wrong.Add(s.lane+": no bar painted across it");continue;}
            float inside=Vector3.Dot(s.centre-s.end,s.dir);worst=Mathf.Max(worst,Mathf.Abs(inside-Inside));
            if(Mathf.Abs(inside-Inside)>Within)wrong.Add(s.lane+": "+s.bar.name+" centre "+inside.ToString("+0.000;-0.000")+" m on the junction side of its line at "+At(s.end)+", not +0.100");
        }
        foreach(var g in all.Where(s=>s.bar!=null).GroupBy(s=>s.bar).Where(g=>g.Count()>1))wrong.Add(g.Key.name+" is across "+string.Join(" and ",g.Select(s=>s.lane)));
        Assert.IsEmpty(wrong,string.Join("\n",wrong));
        Debug.Log("STOP_BARS_ON_LINES: "+all.Count+" bars, worst "+worst.ToString("0.0000")+" m from 0.10 m inside");
    }
    [Test] public void EveryStopPostStandsOnItsBar() {
        var signs=Stops().Where(s=>s.sign).ToList();var wrong=new List<string>();var used=new HashSet<Post>();float worst=0;
        Assert.AreEqual(16,signs.Count,"lanes stopping at a STOP sign");
        Assert.AreEqual(16,dressing.stops.Length,"STOP posts");
        foreach(var s in signs) {
            if(s.bar==null){wrong.Add(s.lane+": no bar for its post to stand on");continue;}
            var post=dressing.stops.OrderBy(p=>(new Vector3(p.x,0,p.z)-s.end).sqrMagnitude).First();var at=new Vector3(post.x,0,post.z);
            if(!used.Add(post))wrong.Add(s.lane+": the post at "+At(at)+" is the nearest to another line too");
            float off=Vector3.Dot(at-s.centre,s.dir);worst=Mathf.Max(worst,Mathf.Abs(off));
            if(Mathf.Abs(off)>Within)wrong.Add(s.lane+": the post at "+At(at)+" is "+off.ToString("+0.000;-0.000")+" m from the centre line of "+s.bar.name);
        }
        Assert.IsEmpty(wrong,string.Join("\n",wrong));
        Debug.Log("STOP_POSTS_ON_BARS: "+signs.Count+" posts, worst "+worst.ToString("0.0000")+" m off their bars");
    }
}
