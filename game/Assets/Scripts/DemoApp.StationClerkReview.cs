using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class StationClerkResult {
        public bool passed,idleLoops,inPlace,dialogueRay,talkLoops,mouthMoves,historyIdle,closeIdle,pauseFreezes,clockContinues,legacyHidden,sixChoices,playerIdle,repeatableTopics,historyBlocks,layoutFits,topicsPersisted;
        public float idleLoopsObserved,jawMaxDegrees,rootDrift,feetDrift;public string verification;
    }
    IEnumerator ReviewStationClerk() {
        yield return new WaitForSeconds(.8f);
        var item=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title=="售票员");
        var actor=item.GetComponentInChildren<StationClerkPerformance>();NeedRail(actor&&actor.animator,"Tripo clerk missing");
        var animator=actor.animator;var bones=actor.GetComponentsInChildren<Transform>();var head=bones.Single(t=>t.name=="Head");var jaw=bones.Single(t=>t.name=="Jaw");
        var feet=bones.Where(t=>t.name=="Left_Foot"||t.name=="Right_Foot").ToArray();NeedRail(feet.Length==2,"clerk foot bones missing");
        var footOrigins=feet.Select(t=>t.position).ToArray();
        var result=new StationClerkResult();var origin=item.transform.position;
        NeedRail(!animator.applyRootMotion&&animator.runtimeAnimatorController.animationClips.Length==2,"clerk must have two in-place clips");
        var module=item.transform.parent.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Module_TicketClerk");
        NeedRail(!module.gameObject.activeSelf,"legacy block clerk is still visible");result.legacyHidden=true;
        player.Place(new Vector3(188,.12f,40),0);player.Aim(new Vector3(188,1.6f,46));
        yield return RailWalk(new Vector3(188,.12f,45.5f));yield return RailWalk(new Vector3(180,.12f,45.5f));
        player.Aim(head.position);yield return new WaitForSeconds(.5f);yield return Capture("clerk-idle-player-view");
        var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,"01-idle-dialogue"));
        float start=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;float until=Time.time+16f;
        while(Time.time<until) {
            result.rootDrift=Mathf.Max(result.rootDrift,Vector3.Distance(origin,item.transform.position));
            for(int i=0;i<feet.Length;i++)result.feetDrift=Mathf.Max(result.feetDrift,Vector3.Distance(footOrigins[i],feet[i].position));
            yield return null;
        }
        result.idleLoopsObserved=animator.GetCurrentAnimatorStateInfo(0).normalizedTime-start;
        NeedRail(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")&&animator.GetCurrentAnimatorStateInfo(0).loop&&result.idleLoopsObserved>1,"ordinary idle did not loop");result.idleLoops=true;
        var evidenceBefore=state.entries.Select(e=>e.id).OrderBy(id=>id).ToArray();
        NeedRail(TryInteract()&&DialogueOpen&&dialogueNode.speaker=="售票员","clerk ordinary E ray failed");result.dialogueRay=true;
        NeedRail(dialogueNode.id=="greeting"&&dialogueNode.text=="你好。","clerk must begin with the requested hello");
        yield return Capture("clerk-greeting");
        NeedRail(AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"lower-right quadrant did not advance greeting");
        NeedRail(dialogueNode.id=="topics"&&dialogueChoices.Count==6,"clerk must offer six free questions");result.sixChoices=true;
        var originalChoices=dialogueChoices.Select(c=>c.text).ToArray();
        AdvanceDialogue();NeedRail(dialogueNode.id=="topics"&&!AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"choices must not auto-select or advance");
        var layout=MeasureDialogue();
        for(int i=0;i<6;i++) {
            var rect=layout.Choice(i);
            NeedRail(rect.x>=DialogueMargin&&rect.xMax<=W-DialogueMargin+.01f&&rect.yMax<=H-44+.01f,"choice outside compact footer layout");
            NeedRail(dialogueOption.CalcHeight(new GUIContent(dialogueChoices[i].text),rect.width-38)<=rect.height,"Chinese choice clipped");
            if(i>0)NeedRail(!rect.Overlaps(layout.Choice(i-1)),"choice rows overlap");
        }
        NeedRail(H-(layout.top-28)<H*.25f,"choice UI exceeds the compact bottom quarter");result.layoutFits=true;
        yield return Capture("clerk-six-options");
        int greetingRevision=state.milkboyGreetingRevision;bool greetedBefore=state.milkboyGreeted;
        SelectDialogue(0);yield return new WaitForSeconds(.4f);
        NeedRail(dialogueNode.id=="ask-date"&&dialogueNode.speaker=="你"&&!actor.IsSpeaking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"player question must use clerk listening idle");result.playerIdle=true;
        NeedRail(dialogueHistory.Count(line=>line=="你：你在20号见过我吗？")==1,"chosen player question duplicated in history");
        yield return Capture("clerk-player-date-question");AdvanceDialogue();
        NeedRail(dialogueNode.id=="date"&&dialogueNode.text.Contains("哪一天，我记不清了"),"date answer lost uncertainty");
        yield return new WaitForSeconds(.5f);NeedRail(actor.IsSpeaking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Talk"),"clerk speech did not switch to talk");
        var jawStart=jaw.localRotation;double clock=ElapsedGameMinutes;until=Time.time+5.3f;
        while(Time.time<until) {
            result.jawMaxDegrees=Mathf.Max(result.jawMaxDegrees,Quaternion.Angle(jawStart,jaw.localRotation));result.rootDrift=Mathf.Max(result.rootDrift,Vector3.Distance(origin,item.transform.position));
            for(int i=0;i<feet.Length;i++)result.feetDrift=Mathf.Max(result.feetDrift,Vector3.Distance(footOrigins[i],feet[i].position));
            yield return null;
        }
        NeedRail(result.jawMaxDegrees>1,"clerk talk has no visible jaw animation");result.mouthMoves=true;
        NeedRail(MovementLocked&&!LookAllowed&&!GameTimePaused&&ElapsedGameMinutes>clock,"clerk dialogue did not keep world clock running");result.clockContinues=true;
        NeedRail(animator.GetCurrentAnimatorStateInfo(0).loop&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>1,"date answer did not retain looping talk");result.talkLoops=true;
        yield return Capture("clerk-date-uncertain-answer");ToggleDialogueHistory();yield return new WaitForSeconds(.4f);
        NeedRail(!actor.IsSpeaking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"history must restore idle");result.historyIdle=true;
        var historyNode=dialogueNode.id;SelectDialogue(0);AdvanceDialogue();
        NeedRail(dialogueNode.id==historyNode&&!AdvanceDialogueAt(new Vector2(W*.75f,H*.60f)),"history allowed click-through");result.historyBlocks=true;
        yield return Capture("clerk-history-idle");ToggleDialogueHistory();AdvanceDialogue();yield return null;
        NeedRail(originalChoices.SequenceEqual(dialogueChoices.Select(c=>c.text)),"asked topics changed visible choices");
        SelectDialogue(0);AdvanceDialogue();AdvanceDialogue();
        NeedRail(originalChoices.SequenceEqual(dialogueChoices.Select(c=>c.text)),"repeat question removed or changed options");result.repeatableTopics=true;
        yield return Capture("clerk-options-after-repeat");
        for(int i=1;i<5;i++) {
            SelectDialogue(i);yield return new WaitForSeconds(.3f);NeedRail(!actor.IsSpeaking,"clerk must listen to every player question");
            AdvanceDialogue();yield return new WaitForSeconds(.4f);NeedRail(actor.IsSpeaking,"clerk branch answer did not talk");
            yield return Capture("clerk-answer-"+dialogueNode.id);AdvanceDialogue();
            NeedRail(originalChoices.SequenceEqual(dialogueChoices.Select(c=>c.text)),"branch must return to unchanged questions");
        }
        SelectDialogue(5);AdvanceDialogue();NeedRail(dialogueNode.id=="goodbye","farewell branch missing");
        yield return Capture("clerk-farewell");AdvanceDialogue();yield return new WaitForSeconds(.5f);
        NeedRail(state.milkboyGreetingRevision==greetingRevision&&state.milkboyGreeted==greetedBefore,"clerk changed milkboy greeting progress");
        NeedRail(state.dialogueTopics.Count(t=>t.StartsWith("station-clerk."))==6,"clerk topics were lost or duplicated");result.topicsPersisted=true;
        NeedRail(!Blocked&&!actor.IsSpeaking&&animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"closing clerk dialogue did not restore idle and controls");result.closeIdle=true;
        NeedRail(evidenceBefore.SequenceEqual(state.entries.Select(e=>e.id).OrderBy(id=>id)),"clerk dialogue granted unexpected evidence");
        paused=true;yield return null;float frozen=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;yield return new WaitForSecondsRealtime(.35f);
        NeedRail(animator.speed==0&&Mathf.Abs(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-frozen)<.002f,"explicit pause did not freeze clerk");result.pauseFreezes=true;
        paused=false;yield return new WaitForSeconds(.4f);recorder.End();NeedRail(result.rootDrift<.001f&&result.feetDrift<.001f,"clerk root or feet moved");result.inPlace=true;
        // Use the existing walkable staff passage before taking unobstructed full-body views.
        yield return RailWalk(new Vector3(183.2f,.12f,45.5f));yield return RailWalk(new Vector3(183.2f,.12f,48));yield return RailWalk(new Vector3(181.25f,.12f,48));
        player.Aim(head.position);yield return Capture("clerk-staff-side");
        player.enabled=false;reviewPortrait=true;var eye=player.eye;float fov=eye.fieldOfView;
        eye.transform.position=origin+new Vector3(1.70f,1.05f,.02f);eye.transform.LookAt(origin+Vector3.up*.96f);eye.fieldOfView=75;
        yield return Capture("clerk-side-full-body");
        eye.transform.position=origin+new Vector3(.50f,1.52f,-.48f);eye.transform.LookAt(head.position);eye.fieldOfView=48;
        recorder.Begin(Path.Combine(captures,"02-face-talk-detail"));actor.Speak("检票的时候，会用检票钳在票上打孔。旁边废票桶里的同线路旧票，你可以拿起来看看。");
        yield return new WaitForSeconds(.7f);yield return Capture("clerk-mouth-detail");yield return new WaitForSeconds(3.3f);actor.Listen();yield return new WaitForSeconds(.6f);recorder.End();
        foreach(var phase in new[]{"clear-noon","rain-noon","night"}) {
            DayReviewTime(phase=="night"?0:720);DayNightCycle.Instance.SetReviewWeather(phase=="rain-noon"?TownWeather.Kind.Rain:TownWeather.Kind.Clear);
            yield return new WaitForSeconds(.8f);yield return Capture("clerk-face-"+phase);
        }
        reviewPortrait=false;player.enabled=true;eye.fieldOfView=fov;player.Place(new Vector3(181.25f,.12f,48),0);player.Aim(head.position);
        state.entries.Add(new NotebookEntry{id="station-clerk-review-proof",title="review only",body="isolated review save"});Save();
        result.passed=true;result.verification=Application.platform+" native rendered player; scripted real controller/rays/shared dialogue handlers, controlled detail cameras. Generic mouth motion without phoneme or audio synchronization. Not physical input or blind playtest.";
        File.WriteAllText(Path.Combine(captures,"station-clerk-results.json"),JsonUtility.ToJson(result,true));Debug.Log("STATION_CLERK_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyStationClerkSave() {
        yield return new WaitForSeconds(.8f);
        NeedRail(state.dialogueTopics.Count(t=>t.StartsWith("station-clerk."))==6,"independent reload lost clerk topics");
        NeedRail(state.entries.Any(e=>e.id=="station-clerk-review-proof"),"independent reload lost review progress");
        var item=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title=="售票员");var actor=item.GetComponentInChildren<StationClerkPerformance>();
        NeedRail(actor&&!actor.IsSpeaking&&actor.animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"),"reloaded clerk did not start idle");
        yield return RailWalk(new Vector3(183.2f,.12f,48));yield return RailWalk(new Vector3(183.2f,.12f,45.5f));yield return RailWalk(new Vector3(180,.12f,45.5f));
        player.Aim(actor.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head").position);yield return null;
        NeedRail(TryInteract()&&DialogueOpen,"reloaded clerk cannot be approached normally");yield return new WaitForSeconds(.5f);NeedRail(actor.IsSpeaking,"reloaded clerk did not talk");
        NeedRail(dialogueNode.text=="你好。","revisit must retain hello");AdvanceDialogue();
        NeedRail(dialogueChoices.Count==6&&dialogueChoices[0].text=="你在20号见过我吗？","reload must keep all asked choices available");
        yield return Capture("clerk-reloaded-options");SelectDialogue(0);AdvanceDialogue();
        NeedRail(dialogueNode.id=="date","reloaded date question cannot be repeated");
        CloseDialogue();yield return new WaitForSeconds(.4f);NeedRail(!Blocked&&!actor.IsSpeaking,"reload conversation did not close");
        yield return Capture("clerk-reloaded-idle");Debug.Log("STATION_CLERK_SAVE_PASS");Application.Quit();
    }
}
