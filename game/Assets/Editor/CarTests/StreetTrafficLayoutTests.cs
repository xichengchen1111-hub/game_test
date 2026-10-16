using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// STREET-03: the street traffic's layout keeps car bodies apart by construction (stop lines set back on
// the 4 m streets, conflicts measured with bodies), and the body measure the cars' guard and the review
// share is the review's old corner-based one.
public class StreetTrafficLayoutTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object net;
    static object Build() {
        var plan=T("TownPlan").GetProperty("Current").GetValue(null);var period=Resources.Load<TextAsset>("PeriodStreets");
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var built=T("StreetNet").GetMethod("Build").Invoke(null,new object[]{plan,period?period.text:null});
        Debug.Log("StreetNet.Build "+watch.ElapsedMilliseconds+" ms ("+built.GetType().GetProperty("BuildTimes").GetValue(built)+")");
        return built;
    }
    static object Net()=>net??(net=Build());
    // Lines set back and conflicts, as numbers that can be compared between two builds.
    static string Layout(object built) {
        var lanes=((IList)built.GetType().GetField("lanes").GetValue(built)).Cast<object>().ToList();
        var turns=((IList)built.GetType().GetField("turns").GetValue(built)).Cast<object>().ToList();
        var conflicts=T("StreetNet+Turn").GetField("conflicts");
        return string.Join(",",lanes.Select(l=>((float)l.GetType().GetField("setback").GetValue(l)).ToString("0.00")))+"|"+
            string.Join(";",turns.Select(t=>string.Join(",",((IEnumerable)conflicts.GetValue(t)).Cast<object>().Select(c=>turns.IndexOf(c)).OrderBy(i=>i))));
    }
    [Test] public void LoadingTheTownAgainReusesTheSameLayout() {
        var first=Layout(Net());var again=Build();
        StringAssert.StartsWith("reused",(string)again.GetType().GetProperty("BuildTimes").GetValue(again));
        Assert.AreEqual(first,Layout(again));
    }
    static float Const(string type,string name)=>(float)T(type).GetField(name).GetValue(null);
    static float Gap(Vector3 a,Vector3 fa,Vector3 b,Vector3 fb)=>(float)T("AmbientCar").GetMethod("BoxGap").Invoke(null,new object[]{a,fa,b,fb});
    // The review's measure before STREET-03: separating axes over the boxes' corners.
    static float CornerGap(Vector3 a,Vector3 fa,Vector3 b,Vector3 fb) {
        Vector2[] Corners(Vector3 o,Vector3 fw){var f=new Vector2(fw.x,fw.z).normalized;var r=new Vector2(f.y,-f.x);var m=new Vector2(o.x,o.z)-f*.17f;return new[]{m+f*2.15f+r*.89f,m+f*2.15f-r*.89f,m-f*2.15f-r*.89f,m-f*2.15f+r*.89f};}
        var A=Corners(a,fa);var B=Corners(b,fb);float best=float.MinValue;
        foreach(var poly in new[]{A,B})for(int i=0;i<4;i++) {
            var e=poly[(i+1)%4]-poly[i];var n=new Vector2(-e.y,e.x).normalized;
            float aMin=float.MaxValue,aMax=float.MinValue,bMin=float.MaxValue,bMax=float.MinValue;
            foreach(var v in A){float d=Vector2.Dot(v,n);aMin=Mathf.Min(aMin,d);aMax=Mathf.Max(aMax,d);}
            foreach(var v in B){float d=Vector2.Dot(v,n);bMin=Mathf.Min(bMin,d);bMax=Mathf.Max(bMax,d);}
            best=Mathf.Max(best,Mathf.Max(bMin-aMax,aMin-bMax));
        }
        return best;
    }
    [Test] public void BodyGapMatchesTheCornerMeasure() {
        var rng=new System.Random(31);float R()=>(float)rng.NextDouble();
        for(int i=0;i<2000;i++) {
            var a=new Vector3(R()*10-5,.025f,R()*10-5);var b=new Vector3(R()*10-5,.025f,R()*10-5);
            var fa=Quaternion.Euler(0,R()*360,0)*Vector3.forward;var fb=Quaternion.Euler(0,R()*360,0)*Vector3.forward;
            Assert.AreEqual(CornerGap(a,fa,b,fb),Gap(a,fa,b,fb),1e-4f);
        }
        // Side by side on a 4 m street, lanes 2 m apart: 0.22 m between them.
        Assert.AreEqual(.22f,Gap(Vector3.zero,Vector3.forward,new Vector3(2,0,0),Vector3.back),1e-4f);
    }
    [Test] public void MarginsStayBetweenTheGuardAndTwoCarsSideBySide() {
        float guard=Const("AmbientCar","Guard"),clear=Const("StreetNet","Clear"),plan=Const("StreetNet","ClearPlan");
        Assert.Less(guard,clear);Assert.Less(clear,plan);Assert.Less(plan,.22f);
    }
    [Test] public void OnlyTheNarrowStreetLinesAreSetBack() {
        var lanes=(IList)Net().GetType().GetField("lanes").GetValue(Net());int moved=0;
        foreach(var lane in lanes) {
            var t=lane.GetType();float setback=(float)t.GetField("setback").GetValue(lane);
            var carriage=t.GetField("carriage").GetValue(lane);bool eastWest=(bool)carriage.GetType().GetField("eastWest").GetValue(carriage);
            bool inbound=t.GetField("toJunction").GetValue(lane)!=null;
            if(setback>0){moved++;Assert.IsFalse(eastWest,lane+" set back");Assert.IsTrue(inbound);Assert.That(setback,Is.InRange(1f,3f),lane.ToString());}
            else Assert.IsTrue(eastWest||!inbound,lane+" was not set back");
        }
        Assert.AreEqual(18,moved);
    }
    [Test] public void WaitingCarsStayClearOfOtherWaysThrough() {
        var args=new object[]{null};float c=(float)Net().GetType().GetMethod("WaitingClearance").Invoke(Net(),args);
        Assert.GreaterOrEqual(c,Const("StreetNet","Clear"),(string)args[0]);
    }
    [Test] public void WaysThroughTakenAtOnceStayClear() {
        var args=new object[]{null};float c=(float)Net().GetType().GetMethod("OpenTurnClearance").Invoke(Net(),args);
        Assert.GreaterOrEqual(c,Const("StreetNet","ClearPlan"),(string)args[0]);
    }
    // The two-step search (every fifth pose, then round the near ones) finds every clearance under
    // ClearPlan exactly: checked against all the sweep's poses, for bodies beside every turn and for
    // every pair of turns at the signal and at a STOP corner.
    static Func<Vector3,Vector3,Vector3,Vector3,float> boxGap;
    static float Exact(Vector3[] sweep,Vector3[] forward,Vector3 at,Vector3 facing){float best=float.MaxValue;for(int i=0;i<sweep.Length;i++)best=Mathf.Min(best,boxGap(sweep[i],forward[i],at,facing));return best;}
    [Test] public void TwoStepClearanceFindsEveryNearPose() {
        boxGap=(Func<Vector3,Vector3,Vector3,Vector3,float>)Delegate.CreateDelegate(typeof(Func<Vector3,Vector3,Vector3,Vector3,float>),T("AmbientCar").GetMethod("BoxGap"));
        var turnType=T("StreetNet+Turn");const BindingFlags inside=BindingFlags.NonPublic|BindingFlags.Instance;
        var sweepField=turnType.GetField("sweep",inside);var forwardField=turnType.GetField("sweepForward",inside);
        var toPose=turnType.GetMethod("Clearance",new[]{typeof(Vector3),typeof(Vector3),typeof(float)});var toTurn=turnType.GetMethod("Clearance",new[]{turnType,typeof(float)});
        float plan=Const("StreetNet","ClearPlan");var rng=new System.Random(7);int near=0;
        var turns=((IList)Net().GetType().GetField("turns").GetValue(Net())).Cast<object>().ToList();
        foreach(var t in turns) {
            var sweep=(Vector3[])sweepField.GetValue(t);var forward=(Vector3[])forwardField.GetValue(t);
            for(int k=0;k<60;k++) {
                var at=sweep[rng.Next(sweep.Length)]+new Vector3((float)rng.NextDouble()*9-4.5f,0,(float)rng.NextDouble()*9-4.5f);
                var facing=Quaternion.Euler(0,(float)rng.NextDouble()*360,0)*Vector3.forward;
                float exact=Exact(sweep,forward,at,facing);
                foreach(float within in new[]{plan,.5f}) {
                    float fast=(float)toPose.Invoke(t,new object[]{at,facing,within});
                    if(exact<within){near++;Assert.AreEqual(exact,fast,1e-5f,t.ToString());}else Assert.GreaterOrEqual(fast,within,t.ToString());
                }
            }
        }
        Assert.Greater(near,500);
        var junction=turnType.GetField("junction");var from=turnType.GetField("from");int pairs=0;
        foreach(var centre in new[]{new Vector3(30,0,20),new Vector3(30,0,-75)}) {
            var here=turns.Where(t=>{var j=junction.GetValue(t);return (Vector3)j.GetType().GetField("centre").GetValue(j)==centre;}).ToList();
            for(int i=0;i<here.Count;i++)for(int k=i+1;k<here.Count;k++) {
                var a=here[i];var b=here[k];if(from.GetValue(a)==from.GetValue(b))continue;
                var sa=(Vector3[])sweepField.GetValue(a);var fa=(Vector3[])forwardField.GetValue(a);var sb=(Vector3[])sweepField.GetValue(b);var fb=(Vector3[])forwardField.GetValue(b);
                float exact=float.MaxValue;for(int p=0;p<sa.Length;p++)exact=Mathf.Min(exact,Exact(sb,fb,sa[p],fa[p]));
                pairs++;
                foreach(float within in new[]{plan,.5f}) {
                    float fast=(float)toTurn.Invoke(a,new object[]{b,within});
                    if(exact<within)Assert.AreEqual(exact,fast,1e-5f,a+" / "+b);else Assert.GreaterOrEqual(fast,within,a+" / "+b);
                }
            }
        }
        Assert.AreEqual(108,pairs);
    }
}
