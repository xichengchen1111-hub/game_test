using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

// Exercise the real generated controller with small diagnostic clips, not the
// unavailable new art. These checks cannot certify a character's gait or mesh.
public class PeriodCharacterAnimationTests {
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    string folder;
    RuntimeAnimatorController controller;
    readonly List<GameObject> objects=new List<GameObject>();
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,BindingFlags.Public|Private).Invoke(target,args);
    static void Field(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.Public|Private).SetValue(target,value);
    static bool Flag(object target,string name)=>(bool)target.GetType().GetProperty(name).GetValue(target);
    [SetUp] public void Setup() {
        Assert.IsNull(T("DemoApp").GetField("Instance").GetValue(null),"Tests must not replace a running game");
        string name="__PeriodCharacterAnimationTests-"+Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets",name);folder="Assets/"+name+"/";
        AnimationClip Clip(string label,float value) {
            var clip=new AnimationClip{name=label};
            clip.SetCurve("Probe",typeof(Transform),"m_LocalPosition.x",AnimationCurve.Linear(0,value,1,value+.1f));
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            AssetDatabase.CreateAsset(clip,folder+label+".anim");return clip;
        }
        var idle=Clip("01_Idle",0);var walk=Clip("02_Walk",1);var talk=Clip("03_Talk",2);
        controller=(RuntimeAnimatorController)T("PeriodCharacterAsset").GetMethod("Controller",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{folder,"Test",idle,walk,talk});
    }
    [TearDown] public void Cleanup() {
        T("PeriodCharacterPerformance").GetMethod("Silence").Invoke(null,null);
        T("DemoApp").GetField("Instance").SetValue(null,null);
        foreach(var item in objects)if(item)UnityEngine.Object.DestroyImmediate(item);objects.Clear();
        if(folder!=null)AssetDatabase.DeleteAsset(folder.TrimEnd('/'));
    }
    Component Subject(out Animator animator) {
        var root=new GameObject("Test character");objects.Add(root);
        var probe=new GameObject("Probe");probe.transform.SetParent(root.transform,false);
        animator=root.AddComponent<Animator>();animator.runtimeAnimatorController=controller;
        var performance=root.AddComponent(T("PeriodCharacterPerformance"));Field(performance,"animator",animator);Field(performance,"speaker","狱警");
        animator.Rebind();animator.Update(0);return performance;
    }
    static void Advance(Component performance,Animator animator,float seconds,bool frozen=false) {
        for(float remaining=seconds;remaining>0;remaining-=.05f){Call(performance,"Tick",.05f,frozen);animator.Update(.05f);}
    }
    static void State(Animator animator,string state)=>Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName(state),"Expected actual Animator state "+state);
    Component App() {
        var root=new GameObject("Inactive test app");root.SetActive(false);objects.Add(root);
        var app=root.AddComponent(T("DemoApp"));Field(app,"opening",false);Field(app,"sceneReady",true);
        T("DemoApp").GetField("Instance").SetValue(null,app);return app;
    }
    [Test] public void RealControllerPrioritizesWalkingAndReturnsThroughTalkToIdle() {
        var performance=Subject(out var animator);State(animator,"Idle");
        Call(performance,"Speak","测试这是一句会说完的话。");Advance(performance,animator,.4f);State(animator,"Talk");
        Call(performance,"SetWalking",true);Advance(performance,animator,.4f);State(animator,"Walk");
        Call(performance,"Speak","继续交谈也不能打断正在进行的步行。");Advance(performance,animator,.4f);State(animator,"Walk");
        Call(performance,"SetWalking",false);Advance(performance,animator,.4f);State(animator,"Talk");
        Advance(performance,animator,13);State(animator,"Idle");Assert.IsFalse(Flag(performance,"IsSpeaking"));
        Assert.IsFalse(animator.applyRootMotion);Assert.AreEqual(Vector3.zero,performance.transform.position,"Animation must not move the actor root");
    }
    [Test] public void PauseFreezesPoseAndSpeechDurationUntilResumed() {
        var performance=Subject(out var animator);Call(performance,"Speak","短句");Advance(performance,animator,.5f);
        var pose=performance.transform.Find("Probe").localPosition;float time=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        Advance(performance,animator,20,true);
        Assert.IsTrue(Flag(performance,"IsSpeaking"),"Paused wall time must not consume the line");Assert.AreEqual(0,animator.speed);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime,Is.EqualTo(time).Within(.0001f));Assert.AreEqual(pose,performance.transform.Find("Probe").localPosition);
        Advance(performance,animator,3);State(animator,"Idle");Assert.AreEqual(1,animator.speed);
    }
    [Test] public void MeasuredWalkRateScalesOnlyWalkAndUncalibratedSourceStaysAtOne() {
        var performance=Subject(out var animator);Call(performance,"SetLocomotion",2f);Advance(performance,animator,.4f);State(animator,"Walk");
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).speedMultiplier,Is.EqualTo(1).Within(.001f));
        Field(performance,"walkMetresPerSecond",1f);Call(performance,"SetLocomotion",2f);Advance(performance,animator,.4f);
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).speedMultiplier,Is.EqualTo(2).Within(.001f));Assert.AreEqual(1,animator.speed);
        Call(performance,"SetLocomotion",0f);Call(performance,"Speak","站定后说话。");Advance(performance,animator,.4f);State(animator,"Talk");
        Assert.That(animator.GetCurrentAnimatorStateInfo(0).speedMultiplier,Is.EqualTo(1).Within(.001f),"Walk rate must not speed up dialogue");
    }
    [Test] public void OnlySelectedSpeakerTalksAndHistoryStopsSpeech() {
        var first=Subject(out var firstAnimator);var second=Subject(out var secondAnimator);
        Call(first,"Speak","原来的发言。");Call(second,"Speak","当前发言。");Advance(first,firstAnimator,.4f);Advance(second,secondAnimator,.4f);
        State(firstAnimator,"Idle");State(secondAnimator,"Talk");
        var app=App();Call(app,"BindDialoguePerformance",second);
        var node=Activator.CreateInstance(T("DialogueNode"));Field(node,"speaker","你");Field(node,"text","玩家发言。");Field(app,"dialogueNode",node);
        Call(app,"UpdateMilkboySpeech");Advance(second,secondAnimator,.4f);State(secondAnimator,"Idle");
        Field(node,"speaker","狱警");Call(app,"UpdateMilkboySpeech");Advance(second,secondAnimator,.4f);State(secondAnimator,"Talk");
        Call(app,"ToggleDialogueHistory");Advance(second,secondAnimator,.4f);State(secondAnimator,"Idle");
    }
    [Test] public void NewOpeningActorUsesWalkClipWithoutManualBoneSwingAndPauseStopsRetreat() {
        var app=App();var performance=Subject(out var animator);var actor=performance.gameObject.AddComponent(T("OpeningActor"));Field(actor,"role","guard");
        // Invoke Awake explicitly for EditMode, where scene lifecycle is not driven.
        Call(actor,"Awake");var leg=new GameObject("Left_UpperLeg");leg.transform.SetParent(actor.transform,false);leg.transform.localRotation=Quaternion.Euler(12,3,7);var rotation=leg.transform.localRotation;
        actor.transform.position=new Vector3(16.95f,.15f,20.6f);
        Call(actor,"StepTowards",new Vector3(16.95f,.15f,21.8f),1f,.4f);Advance(performance,animator,.4f);State(animator,"Walk");
        Assert.That(actor.transform.position.z,Is.EqualTo(21).Within(.001f));Call(actor,"LateUpdate");Assert.AreEqual(rotation,leg.transform.localRotation,"New cast must not receive legacy procedural leg rotation");
        var state=T("DemoApp").GetField("state").GetValue(app);Field(state,"openingStage",1);Field(app,"paused",true);
        var position=actor.transform.position;Call(actor,"Update");Call(actor,"StepTowards",new Vector3(16.95f,.15f,21.8f),1f,1f);
        Assert.AreEqual(position,actor.transform.position,"Guard Update and shared path movement both obey pause");
        Field(app,"paused",false);Call(actor,"StopWalking");Advance(performance,animator,.4f);State(animator,"Idle");
    }
    [Test] public void TalkingToAnotherActorStopsLawyerAnimationWithoutDiscardingHisRoute() {
        var app=App();var performance=Subject(out var animator);var actor=performance.gameObject.AddComponent(T("OpeningActor"));Field(actor,"role","lawyer");Call(actor,"Awake");
        var target=new Vector3(0,0,5);Call(actor,"StepTowards",target,1f,.4f);Advance(performance,animator,.4f);State(animator,"Walk");
        var queue=(Queue<Vector3>)T("DemoApp").GetField("lawyerWalk",Private).GetValue(app);queue.Enqueue(target);
        var node=Activator.CreateInstance(T("DialogueNode"));Field(node,"speaker","值班警员");Field(app,"dialogueNode",node);
        var position=actor.transform.position;Call(app,"TickOpeningRide");Advance(performance,animator,.4f);
        State(animator,"Idle");Assert.AreEqual(position,actor.transform.position);Assert.AreEqual(1,queue.Count);Assert.AreEqual(target,queue.Peek());
        Field(app,"dialogueNode",null);Field(app,"openingRideActive",true);Call(app,"TickOpeningRide");
        Assert.AreEqual(1,queue.Count,"The unfinished route remains available after the conversation");
    }
}
