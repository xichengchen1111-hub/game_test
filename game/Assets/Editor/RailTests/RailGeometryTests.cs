using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Reflection keeps the existing predefined runtime/editor assemblies and asset GUIDs intact.
public class RailGeometryTests {
    GameObject train;
    MethodInfo validation;
    [SetUp] public void OpenSharedPrefab() {
        train=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/RailTrain.prefab"));
        validation=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RailChecks")).First(t=>t!=null).GetMethod("ValidateTrain");
    }
    [TearDown] public void Clean(){if(train)UnityEngine.Object.DestroyImmediate(train);}
    void Check()=>validation.Invoke(null,new object[]{train});
    [Test] public void SharedPhysicalCoachIsValid(){Assert.DoesNotThrow(Check);}
    [Test] public void MovedVisualCannotDriftFromBoardingLayout() {
        train.transform.Find("TrainSet").localPosition=Vector3.right*.4f;
        Assert.Throws<TargetInvocationException>(Check);
    }
    [Test] public void AnOpaqueWindowBackingIsRejected() {
        var backing=GameObject.CreatePrimitive(PrimitiveType.Cube);backing.transform.SetParent(train.transform,false);
        backing.transform.localPosition=new Vector3(2.7f,2.7f,-1.45f);backing.transform.localScale=new Vector3(1,1,.1f);
        Assert.Throws<TargetInvocationException>(Check);
    }
    [Test] public void AnExtraApertureCannotReplaceAWindowPost() {
        Physics.SyncTransforms();var hit=Physics.RaycastAll(new Vector3(3.6f,2.7f,-1.7f),Vector3.forward,.4f).First(h=>h.collider.transform.IsChildOf(train.transform));
        hit.collider.enabled=false;Assert.Throws<TargetInvocationException>(Check);
    }
    [TestCase(false,false,6f,1.6f,0f,11f,6f)]
    [TestCase(true,false,5.4f,4f,0f,0f,9.4f)]
    [TestCase(true,false,5.4f,4f,0f,11f,5.4f)]
    [TestCase(false,true,3.46f,.34f,0f,0f,3.8f)]
    public void RailwayRainProfilesMatchTheirActualRoof(bool barrel,bool ellipse,float eaves,float rise,float x,float z,float expected) {
        var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("WeatherShelter")).First(t=>t!=null);
        var root=new GameObject("Roof profile test");root.transform.position=new Vector3(2000,0,2000);
        var shelter=root.AddComponent(type);
        try {
            type.GetField("footprint").SetValue(shelter,new Vector2(68,22));type.GetField("eaves").SetValue(shelter,eaves);type.GetField("rise").SetValue(shelter,rise);
            type.GetField("ridgeAlongX").SetValue(shelter,true);type.GetField("barrel").SetValue(shelter,barrel);type.GetField("elliptical").SetValue(shelter,ellipse);
            type.GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shelter,null);
            object[] query={root.transform.TransformPoint(new Vector3(x,0,z)),0f};
            Assert.IsTrue((bool)type.GetMethod("RoofAt").Invoke(shelter,query));Assert.AreEqual(expected,(float)query[1],.001f);
        } finally {type.GetMethod("OnDisable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shelter,null);UnityEngine.Object.DestroyImmediate(root);}
    }
}
