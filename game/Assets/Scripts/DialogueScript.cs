using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class DialogueChoice {
    public string text, target, topic, requiresEvidence, requiresTopic;
}
[Serializable] public class DialogueNode {
    public string id, speaker, text, next, grantEvidence;
    public bool greeted;
    public DialogueChoice[] choices;
}
[Serializable] public class DialogueScript {
    public string first, revisit;
    public NotebookEntry evidence;
    public DialogueNode[] nodes;
    Dictionary<string,DialogueNode> index;

    public static DialogueScript Load(string resource) {
        var asset=Resources.Load<TextAsset>(resource);
        if(!asset)throw new InvalidOperationException("Missing dialogue: "+resource);
        var script=JsonUtility.FromJson<DialogueScript>(asset.text);
        script.Validate();
        return script;
    }
    public void Validate() {
        index=new Dictionary<string,DialogueNode>();
        if(nodes==null)throw new InvalidOperationException("Missing dialogue nodes");
        foreach(var node in nodes) {
            if(string.IsNullOrEmpty(node.id)||index.ContainsKey(node.id)||string.IsNullOrEmpty(node.text))
                throw new InvalidOperationException("Missing or duplicate dialogue node: "+node.id);
            index.Add(node.id,node);
        }
        Get(first); Get(revisit);
        foreach(var node in nodes) {
            if(!string.IsNullOrEmpty(node.next))Get(node.next);
            if(!string.IsNullOrEmpty(node.grantEvidence)&&(evidence==null||node.grantEvidence!=evidence.id))
                throw new InvalidOperationException("Unknown dialogue evidence: "+node.grantEvidence);
            if(node.choices==null)continue;
            foreach(var choice in node.choices) {
                Get(choice.target);
                if(string.IsNullOrEmpty(choice.text))throw new InvalidOperationException("Empty dialogue choice");
            }
        }
    }
    public DialogueNode Get(string id) {
        if(string.IsNullOrEmpty(id)||!index.TryGetValue(id,out var node))throw new InvalidOperationException("Unknown dialogue node: "+id);
        return node;
    }
    public List<DialogueChoice> Choices(DialogueNode node,SliceSave state) {
        var result=new List<DialogueChoice>();
        if(node.choices==null)return result;
        foreach(var choice in node.choices) {
            if(!string.IsNullOrEmpty(choice.requiresEvidence)&&!state.entries.Exists(e=>e.id==choice.requiresEvidence))continue;
            if(!string.IsNullOrEmpty(choice.requiresTopic)&&!state.dialogueTopics.Contains(choice.requiresTopic))continue;
            result.Add(choice);
        }
        return result;
    }
}
