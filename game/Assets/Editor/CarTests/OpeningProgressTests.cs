using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public class OpeningProgressTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    [TestCase(false,0)] [TestCase(true,4)]
    public void ExistingInvestigationSkipsOpeningButFreshSaveStartsInCell(bool existing,int stage) {
        var g=new GameObject("Opening migration test");g.SetActive(false);
        try {
            var app=g.AddComponent(T("DemoApp"));var state=Activator.CreateInstance(T("SliceSave"));
            T("DemoApp").GetField("state").SetValue(app,state);
            T("DemoApp").GetMethod("InitializeOpeningState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new string[0],existing});
            Assert.AreEqual(stage,T("SliceSave").GetField("openingStage").GetValue(state));
            Assert.AreEqual(1,T("SliceSave").GetField("openingRevision").GetValue(state));
        }finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void InterruptedOpeningKeepsItsStage(int stage) {
        var g=new GameObject("Opening resumed test");g.SetActive(false);
        try {
            var app=g.AddComponent(T("DemoApp"));var state=Activator.CreateInstance(T("SliceSave"));
            T("SliceSave").GetField("openingRevision").SetValue(state,1);T("SliceSave").GetField("openingStage").SetValue(state,stage);T("DemoApp").GetField("state").SetValue(app,state);
            T("DemoApp").GetMethod("InitializeOpeningState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new string[0],true});
            Assert.AreEqual(stage,T("SliceSave").GetField("openingStage").GetValue(state));
        }finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    [TestCase("OpeningGuardDialogue")] [TestCase("OpeningLawyerDialogue")] [TestCase("OpeningRideDialogue")]
    public void OpeningConversationDoesNotGrantDeductionCards(string resource) {
        var script=T("DialogueScript").GetMethod("Load").Invoke(null,new object[]{resource});
        foreach(var node in (Array)T("DialogueScript").GetField("nodes").GetValue(script)) {
            Assert.IsTrue(string.IsNullOrEmpty((string)T("DialogueNode").GetField("grantEvidence").GetValue(node)));
            Assert.IsFalse((bool)T("DialogueNode").GetField("greeted").GetValue(node),"Opening must not mark the milkboy greeted");
        }
    }
    [TestCase(0,false,0,0)] [TestCase(0,true,4,5)] [TestCase(1,true,4,1)]
    public void RideMigrationDistinguishesLegacyInvestigationAndCurrentOpening(int revision,bool existing,int openingStage,int expectedRide) {
        var g=new GameObject("Ride migration test");g.SetActive(false);
        try {
            var app=g.AddComponent(T("DemoApp"));var state=Activator.CreateInstance(T("SliceSave"));
            T("SliceSave").GetField("openingRevision").SetValue(state,revision);T("SliceSave").GetField("openingStage").SetValue(state,openingStage);T("DemoApp").GetField("state").SetValue(app,state);
            T("DemoApp").GetMethod("InitializeOpeningState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new string[0],existing});
            Assert.AreEqual(expectedRide,T("SliceSave").GetField("openingRideStage").GetValue(state));
        }finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    [TestCase(2,2)] [TestCase(3,2)] [TestCase(4,4)] [TestCase(5,5)]
    public void RideCheckpointsRecoverWithoutRepeatingCompletedTrip(int savedStage,int restoredStage) {
        var g=new GameObject("Ride resume test");g.SetActive(false);
        try {
            var app=g.AddComponent(T("DemoApp"));var state=Activator.CreateInstance(T("SliceSave"));
            T("SliceSave").GetField("openingRevision").SetValue(state,1);T("SliceSave").GetField("openingStage").SetValue(state,4);
            T("SliceSave").GetField("openingRideRevision").SetValue(state,1);T("SliceSave").GetField("openingRideStage").SetValue(state,savedStage);T("DemoApp").GetField("state").SetValue(app,state);
            T("DemoApp").GetMethod("InitializeOpeningState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new string[0],true});
            Assert.AreEqual(restoredStage,T("SliceSave").GetField("openingRideStage").GetValue(state));
            if(savedStage==3){Assert.AreEqual("Town",T("SliceSave").GetField("scene").GetValue(state));Assert.That((float)T("SliceSave").GetField("x").GetValue(state),Is.EqualTo(26.5f).Within(.01));}
        }finally{UnityEngine.Object.DestroyImmediate(g);}
    }
}
