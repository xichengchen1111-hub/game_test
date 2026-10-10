using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    void RequireDialogue(bool condition,string message) {
        if(condition)return;
        Debug.LogError("DIALOGUE_REVIEW_FAIL: "+message);
        Application.Quit(1);
        throw new InvalidOperationException(message);
    }
    void ChooseReviewTopic(string topic) {
        int index=dialogueChoices.FindIndex(c=>c.topic==topic);
        RequireDialogue(index>=0,"Missing topic: "+topic);
        SelectDialogue(index);
    }
    IEnumerator FinishDialogueBranch() {
        int steps=0;
        while(DialogueOpen&&dialogueChoices.Count==0) {
            RequireDialogue(++steps<20,"Dialogue branch never reaches choices");
            AdvanceDialogue();yield return null;
        }
    }
    IEnumerator ApproachReviewMilkboy() {
        var boy=GameObject.Find("Milkboy");
        RequireDialogue(boy,"Relocated milkboy must exist");
        // The imported head-front bone is normalized to root -Z by MilkboyAsset.
        var bones=boy.GetComponentsInChildren<Transform>();
        var head=Array.Find(bones,t=>t.name.Replace(':','_')=="mixamorig_Head");
        var headFront=Array.Find(bones,t=>t.name=="headfront");
        RequireDialogue(head&&headFront,"Milkboy facing bones must exist");
        Vector3 face=headFront.position-head.position;face.y=0;face.Normalize();
        RequireDialogue(Vector3.Dot(face,milkShopRoot.TransformDirection(Vector3.back))>.9f,"Milkboy must face the accessible customer floor");
        Vector3 front=boy.transform.position+face*1.35f;
        front.y=milkShopRoot.TransformPoint(new Vector3(0,.20f,0)).y;
        player.Place(front+face*.75f,0);
        yield return WalkTo(front);
        player.Aim(boy.transform.position+Vector3.up*.94f);yield return null;
    }
    IEnumerator ReviewDialogue() {
        yield return new WaitForSeconds(.5f);
        RequireDialogue(CurrentScene=="Town","Dialogue review must start in Town");
        RequireDialogue(!state.entries.Exists(e=>e.id=="E04"),"Use --fresh for dialogue review");
        // An old save can remember the earlier greeting but lack the new revision.
        state.milkboyGreeted=true;state.milkboyGreetingRevision=0;
        var retained=new NotebookEntry{id="E07",title="retained test entry"};
        state.entries.Add(retained);
        yield return ApproachReviewMilkboy();
        RequireDialogue(TryInteract()&&DialogueOpen,"Real interaction ray must open dialogue");
        RequireDialogue(dialogueNode.id=="greeting"&&!state.entries.Exists(e=>e.id=="E04"),"Greeting cannot grant testimony");
        RequireDialogue(dialogueNode.text=="您是住在［街名］［门牌号］的［姓氏］先生吧？"&&state.entries.Contains(retained),"Old greeting flag must not hide revised opening or remove evidence");
        yield return Capture("dialogue-greeting");
        var position=player.transform.position;
        var rotation=player.eye.transform.rotation;
        player.scriptedDirection=Vector3.forward;
        yield return new WaitForSeconds(.3f);
        RequireDialogue(Blocked&&Vector3.Distance(position,player.transform.position)<.001f&&Quaternion.Angle(rotation,player.eye.transform.rotation)<.01f,"Dialogue must suspend movement and look");
        player.scriptedDirection=Vector3.zero;
        CloseDialogue();
        RequireDialogue(!Blocked,"Closing greeting must restore movement");
        RequireDialogue(TryInteract()&&DialogueOpen,"Interrupted greeting must reopen");
        RequireDialogue(!AdvanceDialogueAt(new Vector2(W*.25f,H*.75f))&&!AdvanceDialogueAt(new Vector2(W*.75f,H*.25f)),"Only lower-right screen quadrant may advance");
        RequireDialogue(!AdvanceDialogueAt(DialogueHistoryButton.center)&&!AdvanceDialogueAt(DialogueExitButton.center),"Footer controls must not advance dialogue");
        RequireDialogue(AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"Click far from the arrow in lower-right quadrant must advance");
        RequireDialogue(dialogueNode.id=="condolence"&&dialogueChoices.Count==3,"First conversation must offer three choices");
        RequireDialogue(state.milkboyGreetingRevision==MilkboyGreetingRevision,"Revised greeting must be remembered after condolences");
        RequireDialogue(!AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"Broad click area must not choose an option");
        yield return Capture("dialogue-first-choices");
        SelectDialogue(2);
        RequireDialogue(dialogueNode.id=="goodbye","Polite exit must show farewell");
        AdvanceDialogue();
        RequireDialogue(!Blocked&&!state.entries.Exists(e=>e.id=="E04"),"Polite exit must not grant testimony");
        RequireDialogue(TryInteract()&&DialogueOpen,"Repeat interaction must work");
        RequireDialogue(dialogueNode.id=="topics","Repeat visit must remember greeting");
        ChooseReviewTopic("milkboy.daily");yield return FinishDialogueBranch();
        RequireDialogue(state.dialogueTopics.Contains("milkboy.daily")&&!state.entries.Exists(e=>e.id=="E04"),"Daily conversation must remain independent of testimony");
        RequireDialogue(dialogueHistory.Exists(s=>s.Contains("手套")),"Daily branch must include human detail");
        ChooseReviewTopic("milkboy.delivery");
        AdvanceDialogue();AdvanceDialogue();
        RequireDialogue(dialogueNode.id=="time"&&!state.entries.Exists(e=>e.id=="E04"),"Time alone must not record incomplete testimony");
        CloseDialogue();
        RequireDialogue(TryInteract()&&DialogueOpen,"Interrupted testimony must be repeatable");
        ChooseReviewTopic("milkboy.delivery");
        while(dialogueNode.id!="clock") { AdvanceDialogue();yield return null; }
        RequireDialogue(state.entries.FindAll(e=>e.id=="E04").Count==1,"Complete testimony must grant exactly one E04");
        yield return Capture("dialogue-testimony");
        AdvanceDialogue();
        RequireDialogue(dialogueChoices.Count==5,"Core testimony must expose follow-up questions");
        yield return Capture("dialogue-followups");
        ChooseReviewTopic("milkboy.gloves");yield return FinishDialogueBranch();
        ChooseReviewTopic("milkboy.route");yield return FinishDialogueBranch();
        ChooseReviewTopic("milkboy.delivery");yield return FinishDialogueBranch();
        RequireDialogue(state.entries.FindAll(e=>e.id=="E04").Count==1,"Repeating testimony must not duplicate evidence");
        ToggleDialogueHistory();
        string historyNode=dialogueNode.id;
        SelectDialogue(0);AdvanceDialogue();
        RequireDialogue(!AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"History must disable broad click advance");
        RequireDialogue(dialogueNode.id==historyNode,"History must block underlying dialogue actions");
        yield return Capture("dialogue-history");ToggleDialogueHistory();

        // E05 is not implemented in this slice. Supply it only in this isolated test
        // to check evidence-first availability, then remove the test entry.
        CloseDialogue();
        var testRecord=new NotebookEntry{id="E05",title="test-only"};state.entries.Add(testRecord);
        RequireDialogue(TryInteract()&&DialogueOpen,"Evidence-first revisit must open");
        RequireDialogue(dialogueChoices.Exists(c=>c.topic=="milkboy.signatures"),"E05 must unlock its question on every visit");
        yield return Capture("dialogue-evidence-question");
        ChooseReviewTopic("milkboy.signatures");yield return FinishDialogueBranch();
        state.entries.Remove(testRecord);CloseDialogue();
        RequireDialogue(TryInteract()&&DialogueOpen,"Revisit without E05 must open");
        RequireDialogue(!dialogueChoices.Exists(c=>c.topic=="milkboy.signatures"),"Questions must derive from current evidence");
        CloseDialogue();
        Vector3 beforeWalk=player.transform.position;
        yield return WalkTo(beforeWalk+new Vector3(-.6f,0,0));
        RequireDialogue(Vector3.Distance(beforeWalk,player.transform.position)>.35f,"Movement must resume after dialogue");
        journal=true;selectedEntry=state.entries.FindIndex(e=>e.id=="E04");
        yield return Capture("dialogue-notebook");journal=false;
        Save();
        File.WriteAllText(Path.Combine(captures,"dialogue-results.json"),JsonUtility.ToJson(new DialogueReviewResult {
            passed=true,width=Screen.width,height=Screen.height,
            verification=Application.platform+" rendered player; actual controller/raycast; conversation actions use shared handlers, not physical keyboard. Conditional E05 supplied only in isolated test. Not a blind playthrough."
        },true));
        Debug.Log("DIALOGUE_REVIEW_PASS: greeting, all branches, interruption, repeat, evidence timing, history, movement, current-evidence questions, save");
        Application.Quit();
    }
    IEnumerator VerifyDialogueSave() {
        yield return new WaitForSeconds(.5f);
        RequireDialogue(state.milkboyGreeted&&state.dialogueTopics.Contains("milkboy.daily")&&state.dialogueTopics.Contains("milkboy.delivery"),"Disk relaunch must preserve greeting and asked topics");
        RequireDialogue(state.milkboyGreetingRevision==MilkboyGreetingRevision,"Disk relaunch must preserve revised opening state");
        RequireDialogue(state.entries.FindAll(e=>e.id=="E04").Count==1,"Disk relaunch must preserve E04");
        yield return ApproachReviewMilkboy();
        RequireDialogue(TryInteract()&&DialogueOpen&&dialogueNode.id=="topics","Disk relaunch must reopen repeat dialogue");
        RequireDialogue(dialogueChoices.Exists(c=>c.topic=="milkboy.gloves"),"Disk relaunch must restore evidence follow-ups");
        yield return Capture("dialogue-reloaded");CloseDialogue();
        File.WriteAllText(Path.Combine(captures,"dialogue-relaunch.txt"),"PASS: separate process restored E04, greeting, asked topics and follow-up choices.\n");
        Debug.Log("DIALOGUE_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    [Serializable] class DialogueReviewResult {
        public bool passed;
        public int width,height;
        public string verification;
    }
}
