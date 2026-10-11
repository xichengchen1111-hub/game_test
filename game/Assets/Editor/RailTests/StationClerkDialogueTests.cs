using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public class StationClerkDialogueTests {
    static Type RuntimeType(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Field(object value,string name) => value.GetType().GetField(name).GetValue(value);
    static object Load() => RuntimeType("DialogueScript").GetMethod("Load",BindingFlags.Public|BindingFlags.Static).Invoke(null,new object[]{"StationClerkDialogue"});
    static object Node(object script,string id) => script.GetType().GetMethod("Get").Invoke(script,new object[]{id});
    [Test] public void ClerkDialogueCannotGrantEvidenceOrGateTheWasteTickets() {
        var script=Load();var evidence=Field(script,"evidence");
        Assert.IsTrue(evidence==null||string.IsNullOrEmpty((string)Field(evidence,"id")));
        foreach(var node in (Array)Field(script,"nodes")) {
            Assert.IsTrue(string.IsNullOrEmpty((string)Field(node,"grantEvidence")));
            Assert.IsFalse((bool)Field(node,"greeted"),"Clerk must not change milkboy greeting progress");
            var choices=Field(node,"choices") as Array;if(choices==null)continue;
            foreach(var choice in choices) {
                Assert.IsTrue(string.IsNullOrEmpty((string)Field(choice,"requiresEvidence")),"Station explanations must be freely accessible");
                Assert.IsTrue(string.IsNullOrEmpty((string)Field(choice,"requiresTopic")),"Station questions must not depend on visit order");
            }
        }
    }
    [Test] public void AskedQuestionsRemainAvailableAfterSaveRoundTrip() {
        var script=Load();var state=Activator.CreateInstance(RuntimeType("SliceSave"));
        var topics=(IList)Field(state,"dialogueTopics");var hub=Node(script,"topics");
        var method=script.GetType().GetMethod("Choices");var fresh=(IList)method.Invoke(script,new[]{hub,state});
        Assert.AreEqual(6,fresh.Count);
        foreach(var choice in fresh)topics.Add(Field(choice,"topic"));
        var json=UnityEngine.JsonUtility.ToJson(state);
        var loaded=UnityEngine.JsonUtility.FromJson(json,state.GetType());
        var repeated=(IList)method.Invoke(script,new[]{hub,loaded});
        Assert.AreEqual(6,repeated.Count);
        CollectionAssert.AreEqual(fresh.Cast<object>().Select(c=>Field(c,"text")).ToArray(),repeated.Cast<object>().Select(c=>Field(c,"text")).ToArray());
        foreach(var choice in repeated) {
            var question=Node(script,(string)Field(choice,"target"));Assert.AreEqual("你",Field(question,"speaker"));
            var answer=Node(script,(string)Field(question,"next"));Assert.AreEqual("售票员",Field(answer,"speaker"));
            if((string)Field(answer,"id")=="goodbye")Assert.IsTrue(string.IsNullOrEmpty((string)Field(answer,"next")));
            else Assert.AreEqual("topics",Field(answer,"next"),"Each question must return to the free topic hub");
        }
    }
    [Test] public void DateQuestionRetainsUncertainRecognitionWithoutInventingACertainDay() {
        var script=Load();Assert.AreEqual("你好。",Field(Node(script,(string)Field(script,"first")),"text"));
        Assert.AreEqual("你在20号见过我吗？",Field(Node(script,"ask-date"),"text"));
        StringAssert.Contains("前几天确实见过您",(string)Field(Node(script,"date"),"text"));
        StringAssert.Contains("哪一天，我记不清了",(string)Field(Node(script,"date"),"text"));
        StringAssert.Contains("不敢说",(string)Field(Node(script,"saw-boarding"),"text"));
        foreach(var node in (Array)Field(script,"nodes"))if((string)Field(node,"speaker")=="售票员") {
            var text=(string)Field(node,"text");StringAssert.DoesNotContain("19号",text);StringAssert.DoesNotContain("20号",text);
        }
    }
}
