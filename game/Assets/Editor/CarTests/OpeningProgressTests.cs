using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public class OpeningProgressTests {
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Migrate(int revision,int stage,int escort,bool belongings,bool existing) {
        var g=new GameObject("Opening migration test");g.SetActive(false);
        try {
            var app=g.AddComponent(T("DemoApp"));var state=Activator.CreateInstance(T("SliceSave"));
            T("SliceSave").GetField("openingRevision").SetValue(state,revision);T("SliceSave").GetField("openingStage").SetValue(state,stage);
            T("SliceSave").GetField("openingEscort").SetValue(state,escort);T("SliceSave").GetField("belongingsCollected").SetValue(state,belongings);
            T("DemoApp").GetField("state").SetValue(app,state);
            T("DemoApp").GetMethod("InitializeOpeningState",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,new object[]{new string[0],existing});
            return state;
        }finally{UnityEngine.Object.DestroyImmediate(g);}
    }
    static object Get(object state,string field)=>T("SliceSave").GetField(field).GetValue(state);
    [TestCase(false,0)] [TestCase(true,4)]
    public void ExistingInvestigationSkipsOpeningButFreshSaveStartsInCell(bool existing,int stage) {
        var state=Migrate(0,0,0,false,existing);
        Assert.AreEqual(stage,Get(state,"openingStage"));
        Assert.AreEqual(2,Get(state,"openingRevision"));
        // A fresh story wakes up with nothing on him; an old investigation keeps everything.
        Assert.AreEqual(existing?4:0,Get(state,"openingEscort"));
        Assert.AreEqual(existing,Get(state,"belongingsCollected"));
    }
    // Revision 1 had no property room: a save still in the cell plays the new wake-up, one past it keeps its things.
    [TestCase(0,0,false)] [TestCase(1,4,true)] [TestCase(3,4,true)] [TestCase(4,4,true)]
    public void FirstRevisionSavesMoveToThePropertyRoomRevision(int stage,int escort,bool belongings) {
        var state=Migrate(1,stage,0,false,true);
        Assert.AreEqual(2,Get(state,"openingRevision"));Assert.AreEqual(stage,Get(state,"openingStage"));
        Assert.AreEqual(escort,Get(state,"openingEscort"));Assert.AreEqual(belongings,Get(state,"belongingsCollected"));
    }
    // A save on the way to or in the property room resumes there, with or without the briefcase.
    [TestCase(1,false)] [TestCase(2,false)] [TestCase(3,true)]
    public void EscortInProgressKeepsItsStep(int escort,bool belongings) {
        var state=Migrate(2,0,escort,belongings,true);
        Assert.AreEqual(0,Get(state,"openingStage"));Assert.AreEqual(escort,Get(state,"openingEscort"));Assert.AreEqual(belongings,Get(state,"belongingsCollected"));
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
    // Every node the escort opens exists in the guard's script.
    [TestCase("follow")] [TestCase("handover")] [TestCase("take")] [TestCase("collected")] [TestCase("to-lawyer")] [TestCase("delivered")] [TestCase("directions")] [TestCase("idle")]
    public void GuardScriptHasEveryEscortLine(string node) {
        var script=T("DialogueScript").GetMethod("Load").Invoke(null,new object[]{"OpeningGuardDialogue"});
        Assert.IsNotNull(T("DialogueScript").GetMethod("Get").Invoke(script,new object[]{node}));
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
