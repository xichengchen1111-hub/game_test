using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

// WALK-01: the footway graph people walk (StreetNet.BuildWalks) keeps off the carriageways except where
// it crosses them, from footway to footway. The back alleys A01 and A02 are drawn on into Station West
// Road; their last stretch used to end in the road, a dead end StreetLife sent people to as a way out of
// town, and they walked the carriageway to get there.
public class StreetWalkLayoutTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object net;
    static object Net() {
        if(net!=null)return net;
        var plan=T("TownPlan").GetProperty("Current").GetValue(null);var period=Resources.Load<TextAsset>("PeriodStreets");
        return net=T("StreetNet").GetMethod("Build").Invoke(null,new object[]{plan,period?period.text:null});
    }
    static object F(object o,string name)=>o.GetType().GetField(name).GetValue(o);
    static IEnumerable Field(string name)=>(IEnumerable)F(Net(),name);
    // A walker keeps up to 0.6 m right of the line and steps up to 0.55 m aside for people coming the
    // other way; with its 0.25 m body that is 1.4 m. The footways here are 1.75 m or more from the road.
    const float Keep=1.5f;
    // How far a point is from the nearest carriageway that carries cars (0 on it), and which road that is.
    static float FromRoad(Vector3 p,out string road) {
        float best=float.MaxValue;road="";
        foreach(var c in Field("carriages")) {
            bool ew=(bool)F(c,"eastWest");float centre=(float)F(c,"centre"),from=(float)F(c,"from"),to=(float)F(c,"to"),half=(float)F(c,"half");
            float along=ew?p.x:p.z,across=ew?p.z:p.x;
            float d=new Vector2(Mathf.Max(0,Mathf.Max(from-along,along-to)),Mathf.Max(0,Mathf.Abs(across-centre)-half)).magnitude;
            if(d<best){best=d;road=(string)F(F(c,"road"),"id");}
        }
        return best;
    }
    static Vector3 P(object node)=>(Vector3)F(node,"point");
    static string At(Vector3 p)=>"("+p.x.ToString("0.##")+", "+p.z.ToString("0.##")+")";
    [Test] public void FootwaysKeepClearOfTheCarriageways() {
        int edges=0;
        foreach(var e in Field("walkEdges")) {
            if(F(e,"crosses")!=null)continue;
            var a=P(F(e,"a"));var b=P(F(e,"b"));int n=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.25f));edges++;
            for(int i=0;i<=n;i++){var p=Vector3.Lerp(a,b,i/(float)n);float d=FromRoad(p,out var road);Assert.GreaterOrEqual(d,Keep,"walk edge "+At(a)+"→"+At(b)+" comes "+d.ToString("0.00")+" m from "+road+" at "+At(p));}
        }
        Assert.Greater(edges,150);
    }
    [Test] public void CrossingsRunFromFootwayToFootway() {
        int crossings=0;
        foreach(var e in Field("walkEdges")) {
            var over=F(e,"crosses");if(over==null)continue;crossings++;
            var a=P(F(e,"a"));var b=P(F(e,"b"));
            foreach(var p in new[]{a,b})Assert.GreaterOrEqual(FromRoad(p,out var road),Keep,"crossing "+At(a)+"→"+At(b)+" ends in "+road);
            Assert.AreEqual(0,FromRoad((a+b)/2,out var mid),"crossing "+At(a)+"→"+At(b)+" misses the road it crosses");
            Assert.AreEqual((string)F(F(over,"road"),"id"),mid);
        }
        Assert.AreEqual(43,crossings);
    }
    // A dead end is where StreetLife sends people out of town, and where they despawn.
    [Test] public void NoWayEndsInTheRoad() {
        int ends=0;
        foreach(var n in Field("walkNodes")) {
            float d=FromRoad(P(n),out var road);Assert.GreaterOrEqual(d,Keep,"walk node at "+At(P(n))+" is "+d.ToString("0.00")+" m from "+road);
            if(F(n,"door")==null&&((IList)F(n,"edges")).Count==1)ends++;
        }
        Assert.Greater(ends,20);
    }
    [Test] public void BackAlleysJoinStationWestRoadsFootway() {
        foreach(var z in new[]{69f,-30f}) {
            var mouth=Field("walkNodes").Cast<object>().FirstOrDefault(n=>(P(n)-new Vector3(131,P(n).y,z)).sqrMagnitude<.01f);
            Assert.IsNotNull(mouth,"no walk node where the alley at z "+z+" meets the footway");
            Assert.GreaterOrEqual(((IList)F(mouth,"edges")).Count,3,"the alley at z "+z+" does not join the footway both ways");
            Assert.IsFalse(Field("walkNodes").Cast<object>().Any(n=>Mathf.Abs(P(n).z-z)<.5f&&P(n).x>131.5f&&P(n).x<138.5f),"a walk node is left in Station West Road at z "+z);
        }
    }
}
