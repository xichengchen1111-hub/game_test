using System;
using UnityEngine;
public partial class DemoApp {
    DialogueScript openingScript;
    bool openingNoticePlayed;
    void InitializeOpeningState(string[] args,bool existing) {
        bool actReview=Review&&(Array.IndexOf(args,"--review-opening")>=0||Array.IndexOf(args,"--verify-opening-save")>=0||Array.IndexOf(args,"--review-police-cast")>=0);
        bool legacyInvestigation=state.openingRevision==0&&(existing||Review&&!actReview||previewWindow||previewStreet||previewMilkboy||previewWatchmaker);
        if(state.openingRevision==0){state.openingRevision=1;state.openingStage=existing||Review&&!actReview||previewWindow||previewStreet||previewMilkboy||previewWatchmaker?4:0;}
        InitializeOpeningRideState(legacyInvestigation);
    }
    Vector3 OpeningPoint(float x,float z) => PoliceStation.Current.transform.TransformPoint(new Vector3(x,PoliceStation.Current.Layout.levels.ground+.025f,z));
    void InitializeOpeningScene() {
        if(CurrentScene!="Town"||!PoliceStation.Current)return;
        if(state.openingStage==0) {player.Place(OpeningPoint(19.2f,20.6f),-90);player.Aim(OpeningPoint(17,20.6f)+Vector3.up*1.5f);}
        RestoreOpeningRideScene();
        // Door state is restored by ShopMechanism, including a player closing it later.
        location="青川 · "+(state.openingStage<1?"羁押室":state.openingStage<4?"律师来访":"警署");
    }
    void TickOpening() {
        if(state.openingStage==0&&!openingNoticePlayed&&!Blocked&&CurrentScene=="Town") {
            openingNoticePlayed=true;
            var actor=FindOpeningActor("guard");if(actor)OpenOpeningActor(actor);
        }
    }
    OpeningActor FindOpeningActor(string role) {
        foreach(var actor in FindObjectsByType<OpeningActor>(FindObjectsSortMode.None))if(actor.role==role)return actor;
        return null;
    }
    public void CallOpeningGuard(){var guard=FindOpeningActor("guard");if(guard)OpenOpeningActor(guard);}
    public void OpeningReminder(string line){remark=line;remarkUntil=Time.time+5;}
    public void OpenOpeningActor(OpeningActor actor) {
        if(Blocked||OpeningRideActive)return;
        if(actor.role=="lawyer"&&state.openingStage>=4&&state.openingRideStage<3){if(state.openingRideStage==1&&lawyerWalk.Count>0){OpeningReminder("律师：我们到门外再说。");return;}OpenRideDialogue(state.openingRideStage==2?"ready":"offer");return;}
        openingScript=DialogueScript.Load(actor.role=="guard"?"OpeningGuardDialogue":"OpeningLawyerDialogue");
        milkboyScript=openingScript;BindDialoguePerformance(actor);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        string node=actor.role=="guard"?(state.openingStage==0?"arrival":"directions"):
            state.openingStage==0?"wait":state.openingStage==1?"greeting":state.openingStage==2?"read":state.openingStage==3?"after":"revisit";
        VisitDialogue(node);CursorMode();
    }
    bool TryOpeningDialogueAction() {
        if(openingScript==null||milkboyScript!=openingScript)return false;
        string id=dialogueNode.id;
        if(id=="open-cell") {state.openingStage=Mathf.Max(1,state.openingStage);EnsureOpeningDoor();CloseDialogue();return true;}
        if(id=="paper") {state.openingStage=Mathf.Max(2,state.openingStage);CloseDialogue();return true;}
        if(id=="release") {state.openingStage=4;state.openingRideStage=1;CloseDialogue();StartLawyerExit();return true;}
        return false;
    }
    void EnsureOpeningDoor() {
        var door=PoliceStation.Current.Door("police-holding-door");
        if(door&&!door.IsOpen&&!door.Moving)door.Use();
    }
    void OpeningInspectionClosed() {
        if(inspected is OpeningNewspaper&&evidenceDetails&&state.openingStage==2)state.openingStage=3;
    }
    void DrawOpeningObjective() {
        if(opening||creatorMenu||OpeningRideActive)return;
        if(state.openingStage>=4){if(state.openingRideStage==1||state.openingRideStage==2)Label(new Rect(36,102,720,65),state.openingRideStage==1?"可选 · 律师在警署门外等你，靠近按 E 交谈。":"去看孩子 · 车停在警署东侧中街，靠近右侧前门按 E 上车。",small);return;}
        string[] goals={"门外的狱警有话要说。靠近按 E 交谈。","会见律师：出牢门穿过内院，进入正楼横厅，再到门厅东侧会见室。","查看会见桌上的报纸：E 拿起，再按 E 阅读正文。","放回报纸，和律师继续谈话。"};
        Label(new Rect(36,102,720,65),goals[Mathf.Clamp(state.openingStage,0,3)],small);
    }
}
