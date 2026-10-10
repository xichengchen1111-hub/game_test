using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RailDoorInteractionTests {
    GameObject train;Component controller;Type type;
    [SetUp] public void Load() {
        train=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/RailTrain.prefab"));
        type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RailTrain")).First(t=>t!=null);
        controller=train.GetComponent(type);type.GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(controller,null);
    }
    [TearDown] public void Clean(){if(train)UnityEngine.Object.DestroyImmediate(train);}
    object Call(string method,params object[] args)=>type.GetMethod(method).Invoke(controller,args);
    [TestCase("Door_C1_S_F")][TestCase("Door_C1_S_R")]
    [TestCase("Door_C1_N_F")][TestCase("Door_C1_N_R")]
    [TestCase("Door_C2_S_F")][TestCase("Door_C2_S_R")]
    [TestCase("Door_C2_N_F")][TestCase("Door_C2_N_R")]
    public void BothSidesHaveRealOpenClosedLeaves(string name) {
        var door=train.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
        var leaf=door.GetComponentInChildren<BoxCollider>();Assert.IsNotNull(leaf);
        Call("SetDoors",false);Physics.SyncTransforms();
        var ray=new Ray(leaf.bounds.center-Vector3.forward*.4f,Vector3.forward);
        Assert.IsTrue(leaf.Raycast(ray,out _,.8f),"closed leaf collision missing");
        Call("SetDoors",true);Physics.SyncTransforms();
        Assert.IsFalse(leaf.Raycast(ray,out _,.8f),"opening must rotate the leaf clear, including north doors");
        Assert.IsTrue((bool)Call("ToggleDoor",door));Assert.IsFalse((bool)Call("DoorOpen",door));
        Assert.IsTrue((bool)Call("ToggleDoor",door));Assert.IsTrue((bool)Call("DoorOpen",door));
    }
    [TestCase("Door_C1_S_F")][TestCase("Door_C1_N_F")]
    public void CapsuleInTheLeafIsDetectedAndClearPassengerIsAllowed(string name) {
        Call("SetDoors",false);
        var door=train.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
        var handle=door.GetComponents<Component>().Single(c=>c&&c.GetType().Name=="RailDoor");
        var leaf=door.GetComponentInChildren<BoxCollider>();
        var passenger=new GameObject("Door safety capsule");
        try {
            var body=passenger.AddComponent<CharacterController>();body.height=1.78f;body.radius=.25f;body.center=Vector3.up*.9f;body.skinWidth=.03f;
            passenger.transform.position=leaf.bounds.center-body.center;
            var occupied=handle.GetType().GetMethod("OccupiedBy");
            Assert.IsTrue((bool)occupied.Invoke(handle,new object[]{body}));
            passenger.transform.position+=Vector3.back;
            Assert.IsFalse((bool)occupied.Invoke(handle,new object[]{body}));
        } finally {UnityEngine.Object.DestroyImmediate(passenger);}
    }
}
