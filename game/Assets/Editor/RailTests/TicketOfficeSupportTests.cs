using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TicketOfficeSupportTests {
    GameObject station;MethodInfo validation;TextAsset geometry;
    [SetUp] public void Load() {
        station=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Generated/StationB.prefab"));
        geometry=AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/TicketOfficeGeometry.json");
        validation=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("TicketOfficeChecks")).First(t=>t!=null).GetMethod("Validate");
    }
    [TearDown] public void Clean(){if(station)UnityEngine.Object.DestroyImmediate(station);}
    void Check()=>validation.Invoke(null,new object[]{station,geometry});
    Transform Mesh(string name)=>station.GetComponentsInChildren<MeshRenderer>().Single(m=>m.name==name).transform;
    [Test] public void ImportedOfficeFixturesAreSupported(){Assert.DoesNotThrow(Check);}
    [TestCase("PigeonDocBundle_0_0")]
    [TestCase("PunchFixedHandle")]
    [TestCase("SignBacking_Tickets")]
    [TestCase("OfficePost_3")]
    public void FloatingFixturesAreRejected(string name) {
        Mesh(name).position+=Vector3.up*.035f;Assert.Throws<TargetInvocationException>(Check);
    }
    [Test] public void MissingWastePaperFillIsRejected() {
        UnityEngine.Object.DestroyImmediate(Mesh("DiscardedPaperCore").gameObject);Assert.Throws<TargetInvocationException>(Check);
    }
}
