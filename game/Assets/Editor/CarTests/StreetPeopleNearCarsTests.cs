using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// STREET-05: the measures cars and walkers use close to each other, how far a walker is from a car's
// body and how far a way across the street passes from it, against the car's own frame and against
// sampling the way across.
public class StreetPeopleNearCarsTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static float PointGap(Vector3 o,Vector3 f,Vector3 p)=>(float)T("AmbientCar").GetMethod("PointGap").Invoke(null,new object[]{o,f,p});
    static float SegmentGap(Vector3 a,Vector3 b,Vector3 o,Vector3 f)=>(float)T("AmbientCar").GetMethod("SegmentGap").Invoke(null,new object[]{a,b,o,f});
    // The walkers' room from a car before STREET-05 (StreetLife.Free), worked out in the car's own frame.
    static float FrameGap(Vector3 o,Vector3 f,Vector3 p) {
        var local=Quaternion.Inverse(Quaternion.LookRotation(f))*(p-o);local.z+=.17f;
        float dx=Mathf.Max(0,Mathf.Abs(local.x)-.89f),dz=Mathf.Max(0,Mathf.Abs(local.z)-2.15f);
        return Mathf.Sqrt(dx*dx+dz*dz);
    }
    [Test] public void PointGapMatchesTheCarsOwnFrame() {
        var rng=new System.Random(41);float R()=>(float)rng.NextDouble();
        for(int i=0;i<4000;i++) {
            var o=new Vector3(R()*10-5,.025f,R()*10-5);var f=Quaternion.Euler(0,R()*360,0)*Vector3.forward;
            var p=o+new Vector3(R()*8-4,R()*.2f,R()*10-5);
            Assert.AreEqual(FrameGap(o,f,p),PointGap(o,f,p),1e-4f);
        }
        // 0.33 m off the side of a car facing north, and a metre ahead of its front bumper.
        Assert.AreEqual(.33f,PointGap(Vector3.zero,Vector3.forward,new Vector3(.89f+.33f,0,0)),1e-5f);
        Assert.AreEqual(1f,PointGap(Vector3.zero,Vector3.forward,new Vector3(0,0,1.98f+1)),1e-5f);
    }
    [Test] public void SegmentGapMatchesSamplingTheWayAcross() {
        var rng=new System.Random(43);float R()=>(float)rng.NextDouble();int through=0;
        for(int i=0;i<1500;i++) {
            var o=new Vector3(R()*6-3,.025f,R()*6-3);var f=Quaternion.Euler(0,R()*360,0)*Vector3.forward;
            var a=new Vector3(R()*14-7,0,R()*14-7);var b=new Vector3(R()*14-7,0,R()*14-7);
            float sampled=float.MaxValue;for(int k=0;k<=2000;k++)sampled=Mathf.Min(sampled,FrameGap(o,f,Vector3.Lerp(a,b,k/2000f)));
            float exact=SegmentGap(a,b,o,f);if(exact==0)through++;
            // Steps of at most a centimetre find the least distance to within half of one, never below it.
            Assert.LessOrEqual(exact,sampled+1e-4f);Assert.GreaterOrEqual(exact,sampled-.006f);
        }
        Assert.Greater(through,50,"some of the ways across should pass through the body");
        // A crossing line 0.66 m in front of a car standing at its line (Station Road at the signal).
        Assert.AreEqual(.66f,SegmentGap(new Vector3(-4,0,1.98f+.66f),new Vector3(4,0,1.98f+.66f),Vector3.zero,Vector3.forward),1e-5f);
    }
}
