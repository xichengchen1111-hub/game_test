using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    void NeedRail(bool ok,string why){if(ok)return;Debug.LogError("RAIL_REVIEW_FAIL: "+why);File.WriteAllText(Path.Combine(captures,"rail-failure.txt"),why);Application.Quit(1);throw new InvalidOperationException(why);}
    IEnumerator RailWalk(Vector3 target) {
        float deadline=Time.unscaledTime+Vector3.Distance(player.transform.position,target)/2.5f+9;
        while(Time.unscaledTime<deadline) {
            var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.09f)break;
            player.scriptedDirection=delta.normalized*Mathf.Clamp(delta.magnitude*2,.1f,1);yield return null;
            NeedRail(player.transform.position.y>-.3f&&player.transform.position.y<1.3f,"walk fell or climbed beyond the authored floor: "+player.transform.position);
        }
        player.scriptedDirection=Vector3.zero;NeedRail(Time.unscaledTime<deadline,"continuous railway route blocked toward "+target+" at "+player.transform.position);
    }
    IEnumerator RailRead(string id) {
        var item=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(t=>t.evidenceId==id);
        player.Aim(item.transform.position+(id=="E03"?Vector3.up*.74f:Vector3.zero));yield return null;
        NeedRail(TryInteract()&&inspected==item,"ordinary 3m E ray could not read "+id);
        double clock=ElapsedGameMinutes;float readingStart=Time.unscaledTime;yield return new WaitForSecondsRealtime(.25f);
        double expected=(Time.unscaledTime-readingStart)*GameClock.GameMinutesPerRealSecond;
        NeedRail(MovementLocked&&!LookAllowed&&!GameTimePaused&&ElapsedGameMinutes>clock&&Math.Abs(ElapsedGameMinutes-clock-expected)<GameClock.GameMinutesPerRealSecond*.08,"inspection must lock player while the shared clock continues at its current rate");
        yield return Capture("station-"+id);CloseInspect();
    }
    IEnumerator RailBoard(RailTrain train) {
        var root=train.transform;
        yield return RailWalk(root.TransformPoint(new Vector3(5.9f,.42f,-3)));
        yield return ReviewRailDoor(train);
        player.Aim(root.TransformPoint(new Vector3(5.9f,2.5f,-1.4f)));yield return Capture("train-open-boarding");
        yield return RailWalk(root.TransformPoint(new Vector3(5.9f,1.05f,-.6f)));
        yield return RailWalk(root.TransformPoint(new Vector3(5.9f,1.05f,0)));
        yield return ReviewRailDoorInside(train);
        // Traverse the actual passenger bulkhead, aisle and coach connector.
        yield return RailWalk(root.TransformPoint(new Vector3(-16,1.05f,0)));
        player.Aim(root.TransformPoint(new Vector3(-22,2.6f,0)));yield return Capture("train-coach-02");
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);yield return new WaitForSeconds(.35f);
        NeedRail(FindFirstObjectByType<WeatherEffects>().ListenerSheltered,"coach interior is not sheltered from rain");
        player.Aim(root.TransformPoint(new Vector3(-15,2.5f,-8)));yield return Capture("train-window-rain");DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Clear);
        yield return RailWalk(train.StandPosition);
        if(CurrentScene=="Town") {
            SetMapWaypoint(TownPlan.Spawn,"广场");
            NeedRail(HandleMapKeys(true,false)&&mapOpen,"Town map could not open before boarding");
            SetMapEditing(true);
        }
        var seat=train.GetComponentInChildren<RailSeat>();player.Aim(seat.transform.position);yield return null;
        NeedRail(TryInteract()&&RailSeated&&railTravelling,"ordinary seat E interaction did not start journey");
        NeedRail(!mapOpen&&!mapEditing,"departure did not put away the town map/edit cursor");
    }
    IEnumerator ReviewRail() {
        yield return new WaitForSeconds(.75f);DayReviewTime(990);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Clear);
        player.Place(new Vector3(184,.12f,33),0);player.Aim(new Vector3(188,4.1f,44));yield return Capture("station-B-front-detail-day");
        DayReviewTime(1065);dayNightReviewFrozen=false;
        if(RecordRailSigns)yield return ReviewRailSigns("town");
        player.Place(new Vector3(170,.12f,31),0);player.Aim(new Vector3(188,4.5f,49));yield return Capture("station-B-wide-dusk");
        player.Place(new Vector3(188,.12f,33),0);player.Aim(new Vector3(188,5,46));yield return Capture("station-B-front-dusk");
        yield return RailWalk(new Vector3(188,.12f,44));player.Aim(new Vector3(178,3,47));yield return Capture("station-ticket-hall");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--record-ticket-office")>=0) {
            yield return ReviewTicketOffice();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--office-only")>=0)yield break;
        }
        RailTicketChecks.ValidateImages(intactTicketImage,punchedTicketImage);
        InspectCarriedTicket();yield return Capture("ticket-carried-intact");CloseInspect();
        yield return RailWalk(new Vector3(177.4f,.12f,43.5f));
        var bin=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(t=>t.evidenceId=="E03");
        RailTicketChecks.Validate(bin.gameObject);player.Aim(bin.transform.position+new Vector3(0,.78f,-.17f));yield return Capture("ticket-bin-punched-physical");
        yield return RailRead("E03");
        journal=true;selectedEntry=state.entries.FindIndex(e=>e.id=="E03");yield return Capture("ticket-punched-journal");journal=false;CursorMode();
        yield return RailWalk(new Vector3(188,.12f,45.5f));yield return RailWalk(new Vector3(197,.12f,45.5f));yield return RailRead("E08");
        yield return RailWalk(new Vector3(188,.12f,46));yield return RailWalk(new Vector3(188,.12f,59));yield return RailWalk(new Vector3(188,.42f,66));
        var train=FindFirstObjectByType<RailTrain>();player.Aim(train.transform.TransformPoint(new Vector3(19,2,0)));yield return Capture("train-steam-platform");
        NeedRail(state.entries.Count(e=>e.id=="E03")==1&&state.entries.Count(e=>e.id=="E08")==1,"station evidence IDs lost/duplicated");
        double clock=ElapsedGameMinutes;yield return RailBoard(train);double departedClock=ElapsedGameMinutes;
        yield return new WaitForSecondsRealtime(2.5f);yield return Capture("train-seated-departure");
        NeedRail(train.Travelled>.4f,"train failed to move");
        var checkpoint=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
        File.Copy(savePath,Path.Combine(captures,"departure-checkpoint.json"),true);
        NeedRail(checkpoint.scene=="Town"&&checkpoint.railSeated&&Math.Abs(checkpoint.x-184.8f)<.01f,"interrupted journey checkpoint is not parked source seat");
        Save();var after=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));NeedRail(after.x==checkpoint.x,"moving train overwrote safe checkpoint");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--interrupt-rail")>=0){Debug.Log("RAIL_INTERRUPT_CHECKPOINT_PASS");Application.Quit();yield break;}
        while(transitioning)yield return null;
        NeedRail(CurrentScene=="OtherTown"&&RailSeated&&state.railJourneys==1,"arrival did not load next town on same seat");
        NeedRail(state.hasMapWaypoint&&!mapOpen&&!HandleMapKeys(true,false),"Town waypoint must survive travel without showing a Town route in OtherTown");
        NeedRail(Math.Abs(ElapsedGameMinutes-departedClock)<.05,"transition advanced the shared clock");
        NeedRail(state.entries.Any(e=>e.id=="E02")&&state.entries.Any(e=>e.id=="E03")&&state.entries.Any(e=>e.id=="E08"),"scene unload lost carried ticket/clues");
        InspectCarriedTicket();yield return Capture("ticket-carried-after-travel");CloseInspect();
        player.Aim(seatedTrain.transform.TransformPoint(new Vector3(0,2.5f,-8)));yield return Capture("next-town-arrival-seat");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--stop-at-next-town")>=0){Save();Debug.Log("RAIL_NEXT_TOWN_CHECKPOINT_PASS");Application.Quit();yield break;}
        RiseFromRailSeat();if(RecordRailSigns)yield return ReviewRailSigns("other-town");
        yield return RailWalk(new Vector3(5.9f,1.05f,0));yield return RailWalk(new Vector3(5.9f,.42f,-3));
        yield return RailWalk(new Vector3(7,.42f,-8));yield return RailWalk(new Vector3(7,.12f,-17));yield return RailWalk(new Vector3(7,.12f,-29));yield return RailWalk(new Vector3(7,.12f,-37));
        player.Aim(new Vector3(-15,4,-52));yield return Capture("next-town-street");
        // Return journey uses the same authored floor and seat, no scene-specific shortcuts.
        yield return RailWalk(new Vector3(7,.12f,-17));yield return RailWalk(new Vector3(7,.42f,-8));train=FindFirstObjectByType<RailTrain>();yield return RailBoard(train);
        while(transitioning)yield return null;
        NeedRail(CurrentScene=="Town"&&state.railJourneys==2,"return journey failed");
        NeedRail(state.hasMapWaypoint&&HandleMapKeys(true,false)&&mapOpen,"Town map/waypoint unavailable after return");SetMapOpen(false);
        // Leave an actual arrival seat checkpoint for a separate process reload.
        Save();yield return Capture("return-town-arrival");
        File.WriteAllText(Path.Combine(captures,"rail-results.json"),JsonUtility.ToJson(new RailResult{passed=true,width=Screen.width,height=Screen.height,journeys=state.railJourneys,scene=CurrentScene,elapsed=ElapsedGameMinutes,verification="Visible native Unity renderer; scripted ordinary CharacterController walking, 3m E rays, both coaches, two-way scene travel, actual seat reload checkpoint. Not physical keyboard input or blind playtest."},true));
        Debug.Log("RAIL_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyRailSave() {
        yield return new WaitForSeconds(.5f);
        var args=Environment.GetCommandLineArgs();bool other=Array.IndexOf(args,"--load-next-town")>=0,interrupted=Array.IndexOf(args,"--load-interrupted")>=0;
        NeedRail(CurrentScene==(other?"OtherTown":"Town")&&RailSeated&&state.railSeated&&state.railJourneys==(other?1:interrupted?0:2),"arrival/source seat state failed separate-process reload");
        NeedRail(clockSaveLoadedAtStartup&&Math.Abs(ReadElapsedGameMinutes(state)-clockLoadedElapsedMinutes)<.0001,"reload changed saved shared clock phase");
        NeedRail(state.entries.Any(e=>e.id=="E02")&&state.entries.Any(e=>e.id=="E03")&&state.entries.Any(e=>e.id=="E08"),"reload lost carried ticket/station clues");
        RailTicketChecks.ValidateImages(intactTicketImage,punchedTicketImage);
        InspectCarriedTicket();yield return Capture("ticket-carried-reloaded");CloseInspect();
        yield return Capture("rail-save-reloaded-seat");RiseFromRailSeat();
        var train=FindFirstObjectByType<RailTrain>();yield return RailWalk(train.transform.TransformPoint(new Vector3(5.9f,1.05f,0)));yield return RailWalk(train.boardingPoint.position);
        NeedRail(!RailSeated&&player.transform.position.y<.6f,"loaded passenger could not disembark");
        if(!other) {
            NeedRail(state.hasMapWaypoint&&HandleMapKeys(true,false)&&mapOpen,"returned waypoint did not survive reload");
            RefreshMapRoute();NeedRail(mapRouteAvailable,"station platform could not navigate back to Town after reload");SetMapOpen(false);
        }
        File.WriteAllText(Path.Combine(captures,"rail-save-results.json"),"{\"passed\":true,\"separateProcess\":true,\"cluesPreserved\":true,\"canDisembark\":true}");Debug.Log("RAIL_SAVE_PASS");Application.Quit();
    }
    [Serializable] class RailResult {public bool passed;public int width,height,journeys;public string scene,verification;public double elapsed;}
}
