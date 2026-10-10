using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FurnitureSeatSafetyTests {
    GameObject seat,floor,obstacle;
    Component component;
    MethodInfo find;
    [SetUp] public void Setup() {
        seat=new GameObject("Seat clearance test");var stand=new GameObject("Standing point");stand.transform.SetParent(seat.transform);
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("FurnitureSeat")).First(t=>t!=null);
        component=seat.AddComponent(type);type.GetField("standingPoint").SetValue(component,stand.transform);find=type.GetMethod("FindStandingPosition");
        floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.05f,0);floor.transform.localScale=new Vector3(5,.1f,5);
    }
    bool CanStand(){object[] args={Vector3.zero};return (bool)find.Invoke(component,args);}
    [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(seat);UnityEngine.Object.DestroyImmediate(floor);if(obstacle)UnityEngine.Object.DestroyImmediate(obstacle);}
    [Test] public void StandingRequiresARealFloor(){UnityEngine.Object.DestroyImmediate(floor);Assert.IsFalse(CanStand());}
    [Test] public void ClearFloorSupportsStanding(){Assert.IsTrue(CanStand());}
    [Test] public void OccupiedExitCannotPushPassengerThroughObjects(){obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=Vector3.up*.9f;obstacle.transform.localScale=new Vector3(2,1.8f,2);Assert.IsFalse(CanStand());}
    [Test] public void ItemTriggersDoNotObstructThePhysicalExit(){obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=Vector3.up*.9f;obstacle.transform.localScale=new Vector3(2,1.8f,2);obstacle.GetComponent<BoxCollider>().isTrigger=true;Assert.IsTrue(CanStand());}
}
