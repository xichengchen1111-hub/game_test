using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    void NeedItem(bool ok,string why){if(!ok)throw new InvalidOperationException("ITEM_REVIEW_FAIL: "+why);}
    IEnumerator ReadWorldItem(string title,Vector3 feet) {
        yield return RailWalk(feet);
        var item=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title==title);
        player.Aim(item.GetComponent<Collider>().bounds.center);yield return null;
        int count=state.entries.Count;
        NeedItem(TryInteract()&&inspected==item,"ordinary E ray failed "+title);
        double clock=ElapsedGameMinutes;float readingStart=Time.unscaledTime;
        yield return new WaitForSecondsRealtime(.15f);
        double expected=(Time.unscaledTime-readingStart)*GameClock.GameMinutesPerRealSecond;
        NeedItem(count==state.entries.Count&&MovementLocked&&!LookAllowed&&!GameTimePaused&&ElapsedGameMinutes>clock&&Math.Abs(ElapsedGameMinutes-clock-expected)<GameClock.GameMinutesPerRealSecond*.08,"ambient item must keep clues unchanged and lock the player while time continues");
        if(title=="查看旅客须知")yield return Capture("station-notice-interaction");
        CloseInspect();
    }
    IEnumerator ReviewFurniture() {
        yield return new WaitForSeconds(.6f);DayReviewTime(1065);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Clear);
        var seats=FindObjectsByType<FurnitureSeat>(FindObjectsSortMode.None).OrderBy(s=>s.seatId).ToArray();
        NeedItem(seats.Length==59&&seats.Select(s=>s.seatId).Distinct().Count()==59,"expected 8 street + 8 hall + 43 coach unique rest seats");
        var hall=seats.Single(s=>s.seatId=="hall-bench-0");
        player.Place(new Vector3(188,.12f,42),0);
        NeedItem(hall.FindStandingPosition(out var hallExit),"hall exit blocked");yield return RailWalk(hallExit);
        player.Aim(hall.GetComponent<Collider>().bounds.center);yield return null;
        NeedItem(TryInteract()&&FurnitureSeated&&!RailSeated&&MovementLocked&&LookAllowed&&!GameTimePaused,"ordinary hall seat E failed");
        double sittingClock=ElapsedGameMinutes;yield return new WaitForSecondsRealtime(.25f);
        NeedItem(ElapsedGameMinutes>sittingClock,"resting wrongly pauses clock");
        player.Aim(new Vector3(188,2.0f,47));yield return Capture("station-bench-seated");
        ToggleWatch();NeedItem(watch&&LookAllowed&&!GameTimePaused&&MovementLocked,"seated watch failed");yield return Capture("station-bench-watch");ToggleWatch();
        InspectCarriedTicket();NeedItem(inspected&&!GameTimePaused&&!LookAllowed&&FurnitureSeated,"seated carried-ticket UI failed");
        NeedItem(!RiseFromFurnitureSeat(),"seated UI allowed rising");CloseInspect();
        Save();File.Copy(savePath,Path.Combine(captures,"bench-checkpoint.json"),true);
        // Obstruct every authored escape candidate to test actual capsule-based safe standing.
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="Temporary standing clearance probe";
        blocker.transform.position=hallExit+Vector3.up*.9f;blocker.transform.localScale=new Vector3(2.1f,1.8f,1.5f);
        NeedItem(!RiseFromFurnitureSeat()&&FurnitureSeated,"blocked bench exit pushed player into an object");
        Destroy(blocker);yield return null;NeedItem(RiseFromFurnitureSeat(),"bench could not rise after obstruction removed");
        int checkedSeats=0;
        // Stage at each clear aisle, then use the actual normal E ray. Not a full town walking order.
        foreach(var seat in seats) {
            NeedItem(seat.FindStandingPosition(out var exit),"seat aisle blocked "+seat.seatId);
            player.Place(exit,0);player.Aim(seat.GetComponent<Collider>().bounds.center);yield return null;
            NeedItem(TryInteract()&&FurnitureSeated&&restingSeat==seat&&!RailSeated,"seat not reachable "+seat.seatId);
            NeedItem(Mathf.Abs(player.eye.transform.localPosition.y-seat.eyeHeight)<.001f,"seated eye mismatch "+seat.seatId);
            NeedItem(RiseFromFurnitureSeat(),"seat cannot rise "+seat.seatId);checkedSeats++;
        }
        player.Place(new Vector3(188,.12f,47),0);
        yield return ReadWorldItem("查看旅客须知",new Vector3(198,.12f,49.3f));
        yield return RailWalk(new Vector3(201,.12f,48.5f));
        yield return RailWalk(new Vector3(201,.12f,42.9f));
        yield return ReadWorldItem("查看行李秤",new Vector3(199.5f,.12f,42.9f));
        yield return ReadWorldItem("查看行李须知",new Vector3(201.4f,.12f,42.5f));
        player.Place(new Vector3(174.6f,.12f,42.5f),0);yield return ReadWorldItem("查看候车告示",player.transform.position);
        player.Place(new Vector3(164,.42f,64.8f),0);yield return ReadWorldItem("查看行李箱",new Vector3(165.2f,.42f,64.8f));
        var train=FindFirstObjectByType<RailTrain>();
        player.Place(train.transform.TransformPoint(new Vector3(5.9f,.42f,-3)),0);
        yield return RailWalk(train.transform.TransformPoint(new Vector3(5.9f,1.05f,0)));
        var lamps=FindObjectsByType<CoachLampSwitch>(FindObjectsSortMode.None).OrderBy(s=>s.coachName).ToArray();
        foreach(var control in lamps) {
            float x=control.coachName=="01"?6.55f:-9.45f;
            yield return RailWalk(train.transform.TransformPoint(new Vector3(x,1.05f,0)));
            player.Aim(control.transform.position);yield return null;
            if(control.coachName=="01")yield return Capture("coach-lamp-switch");
            NeedItem(control.LampOn&&TryInteract()&&!control.LampOn,"lamp switch E did not turn off");
            NeedItem(control.lamps.All(l=>!l.RequestedOn&&!l.source.enabled),"coach switch left a light on");
            if(control.coachName=="01"){player.Aim(train.transform.TransformPoint(new Vector3(-3,2.5f,0)));yield return Capture("coach-lights-off");player.Aim(control.transform.position);}
            yield return null;NeedItem(TryInteract()&&control.LampOn,"lamp switch E did not turn on");
            NeedItem(control.lamps.All(l=>l.RequestedOn),"coach switch failed to restore requested lights");
            if(control.coachName=="01"){player.Aim(train.transform.TransformPoint(new Vector3(-3,2.5f,0)));yield return Capture("coach-lights-on");}
        }
        yield return RailWalk(train.transform.TransformPoint(new Vector3(-2.7f,1.05f,0)));
        foreach(string label in new[]{"查看旅行卷包","查看皮革小箱","查看红色小箱"}) {
            var item=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title==label);
            var local=train.transform.InverseTransformPoint(item.transform.position);yield return RailWalk(train.transform.TransformPoint(new Vector3(local.x,1.05f,0)));
            player.Aim(item.transform.position);yield return null;int count=state.entries.Count;
            NeedItem(TryInteract()&&inspected==item&&state.entries.Count==count,"luggage E ray failed "+label);CloseInspect();
        }
        var coachSeat=seats.Single(s=>s.seatId=="coach-02-end-1");
        NeedItem(coachSeat.FindStandingPosition(out var coachExit),"coach rest exit failed");yield return RailWalk(coachExit);
        player.Aim(coachSeat.GetComponent<Collider>().bounds.center);yield return null;NeedItem(TryInteract()&&FurnitureSeated,"coach sitting failed");
        player.Aim(train.transform.TransformPoint(new Vector3(-16,2.5f,-4)));yield return Capture("coach-seat-resting");NeedItem(RiseFromFurnitureSeat(),"coach rising failed");
        // Leave a genuine first-coach seat + lamp-off checkpoint for a separate player process.
        yield return RailWalk(train.transform.TransformPoint(new Vector3(6.55f,1.05f,0)));
        player.Aim(lamps[0].transform.position);yield return null;NeedItem(TryInteract()&&!lamps[0].LampOn,"checkpoint lamp did not turn off");
        var checkpointSeat=seats.Single(s=>s.seatId=="coach-01-end-1");
        NeedItem(checkpointSeat.FindStandingPosition(out var checkpointExit),"checkpoint aisle blocked");yield return RailWalk(checkpointExit);
        player.Aim(checkpointSeat.GetComponent<Collider>().bounds.center);yield return null;NeedItem(TryInteract()&&FurnitureSeated,"checkpoint seat E failed");
        Save();File.Copy(savePath,Path.Combine(captures,"coach-checkpoint.json"),true);
        File.WriteAllText(Path.Combine(captures,"items-results.json"),JsonUtility.ToJson(new ItemResult{passed=true,width=Screen.width,height=Screen.height,restSeats=checkedSeats,ambientInspections=8,coachSwitches=2,safeRising=true,clockContinuesWhileResting=true,verification="Native rendered player, staged aisle positions plus ordinary E ray on every seat, real controller walking for item routes, shared UI/rise handlers. Not physical input, blind playtest or full town walk."},true));
        Debug.Log("ITEM_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyFurnitureSave() {
        yield return new WaitForSeconds(.7f);
        var args=Environment.GetCommandLineArgs();bool bench=Array.IndexOf(args,"--bench-checkpoint")>=0;
        bool other=Array.IndexOf(args,"--next-town-checkpoint")>=0,travel=Array.IndexOf(args,"--travel-after-rest")>=0;
        NeedItem(CurrentScene==(other?"OtherTown":"Town"),"rest seat reloaded in wrong scene");
        NeedItem(FurnitureSeated&&!RailSeated&&restingSeat.seatId==(bench?"hall-bench-0":"coach-01-end-1"),"rest seat failed separate-process reload");
        NeedItem(clockSaveLoadedAtStartup&&Math.Abs(ReadElapsedGameMinutes(state)-clockLoadedElapsedMinutes)<.001,"rest reload changed saved clock phase");
        if(!bench)NeedItem(FindObjectsByType<CoachLampSwitch>(FindObjectsSortMode.None).Single(s=>s.coachName=="01").lamps.All(l=>!l.RequestedOn&&!l.source.enabled),"lamp-off state failed reload");
        InspectCarriedTicket();NeedItem(FurnitureSeated&&!GameTimePaused&&!LookAllowed,"loaded resting ticket failed");CloseInspect();
        yield return Capture(bench?"hall-seat-reloaded":"coach-seat-reloaded");
        NeedItem(RiseFromFurnitureSeat()&&!MovementLocked,"rest reload could not rise");
        player.scriptedDirection=player.transform.right;var old=player.transform.position;yield return new WaitForSeconds(.25f);player.scriptedDirection=Vector3.zero;
        NeedItem(Vector3.Distance(old,player.transform.position)>.2f,"movement failed after rising from saved seat");
        if(travel) {
            var train=FindFirstObjectByType<RailTrain>();yield return RailWalk(train.StandPosition);
            var departure=train.GetComponentInChildren<RailSeat>();player.Aim(departure.transform.position);yield return null;
            NeedItem(TryInteract()&&railTravelling&&!FurnitureSeated&&string.IsNullOrEmpty(state.furnitureSeatId),"rising from rest did not permit original departure");
            while(transitioning)yield return null;
            NeedItem(CurrentScene=="OtherTown"&&RailSeated,"rest-to-travel failed scene arrival");
            var control=FindObjectsByType<CoachLampSwitch>(FindObjectsSortMode.None).Single(s=>s.coachName=="01");
            NeedItem(!control.LampOn&&control.lamps.All(l=>!l.RequestedOn&&!l.source.enabled),"manual lamp-off lost during scene travel");
            RiseFromRailSeat();train=FindFirstObjectByType<RailTrain>();
            var rest=FindObjectsByType<FurnitureSeat>(FindObjectsSortMode.None).Single(s=>s.seatId=="coach-01-end-1");
            NeedItem(rest.FindStandingPosition(out var exit),"next-town rest aisle blocked");yield return RailWalk(exit);
            player.Aim(rest.GetComponent<Collider>().bounds.center);yield return null;NeedItem(TryInteract()&&FurnitureSeated&&!RailSeated,"next-town regular rest seat failed");
            Save();File.Copy(savePath,Path.Combine(captures,"next-town-rest-checkpoint.json"),true);yield return Capture("next-town-resting");
        }
        File.WriteAllText(Path.Combine(captures,"items-save-results.json"),"{\"passed\":true,\"separateProcess\":true,\"seatRestored\":true,\"canRiseAndWalk\":true}");
        Debug.Log("ITEM_SAVE_PASS");Application.Quit();
    }
    [Serializable] class ItemResult {public bool passed,safeRising,clockContinuesWhileResting;public int width,height,restSeats,ambientInspections,coachSwitches;public string verification;}
}
