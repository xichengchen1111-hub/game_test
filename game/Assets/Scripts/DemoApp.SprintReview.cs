using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Isolated native controller review. Only starting locations/times are staged;
// every leg uses collision, gravity and the same normalized Shift movement path.
public partial class DemoApp {
    [Serializable] class SprintRouteInput { public SprintRoutePoints[] routes; }
    [Serializable] class SprintRoutePoints { public string id; public Vector3[] points; }
    [Serializable] class SprintLeg {
        public string name; public float speed,metres,plannedMetres,peakMetresPerSecond;
        public double realSeconds,gameMinutes,oldRateProjectedGameMinutes,arrivalWatchMinutes;
        public Vector3[] points;
    }
    [Serializable] class SprintReport {
        public bool passed,stationBefore1740,homeBefore1755,storyWindowFeasible,normalizedDiagonal,closedDoorBlocks,openDoorPasses,mapSprint,stairs;
        public float walkSpeed=PlayerMotor.WalkSpeed,sprintSpeed=PlayerMotor.SprintSpeed;
        public double gameMinutesPerRealSecond=GameClock.GameMinutesPerRealSecond,stationWaitRealSeconds,roundTripGameMinutes,homeArrivalWatchMinutes;
        public List<SprintLeg> legs=new List<SprintLeg>();
        public string scope="Native "+Application.platform+" renderer, real CharacterController, scripted normalized movement and Shift state; no physical keyboard or blind player claim. Endpoints are exterior door approaches, excluding station and home interior travel. Historical lines are candidate routes, not proof of NPC actions. Old clock figures are projections of measured wall time. Passed means controller/rate/persistence checks succeeded; story-window feasibility is reported separately.";
    }
    void NeedSprint(bool ok,string why) {if(ok)return;Debug.LogError("SPRINT_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);}
    static float SprintDistance(Vector3 a,Vector3 b)=>Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z));
    void StopSprint(){player.scriptedDirection=Vector3.zero;player.scriptedSprint=false;player.scriptedPlayerInput=false;player.reviewSprintSpeed=PlayerMotor.SprintSpeed;}
    IEnumerator SprintLegRun(Vector3[] route,float speed,string name,SprintReport report,bool record=false,bool stair=false) {
        NeedSprint(route.Length>=2,"Route has fewer than two points: "+name);
        NeedSprint(SprintDistance(player.transform.position,route[0])<.3f,"Leg start was not reached continuously: "+name);
        var leg=new SprintLeg{name=name,speed=speed,points=route};
        for(int i=1;i<route.Length;i++)leg.plannedMetres+=SprintDistance(route[i-1],route[i]);
        player.scriptedPlayerInput=true;player.scriptedSprint=true;player.reviewSprintSpeed=speed;
        WeatherClipRecorder recorder=null;
        if(record){recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,name));}
        double real=Time.realtimeSinceStartupAsDouble,game=ElapsedGameMinutes,deadline=real+leg.plannedMetres/speed+30,nextLog=real+15;
        for(int i=1;i<route.Length;i++) {
            while(SprintDistance(player.transform.position,route[i])>.10f) {
                var delta=route[i]-player.transform.position;delta.y=0;
                player.scriptedDirection=delta.normalized*Mathf.Min(1,delta.magnitude/(speed*Mathf.Max(Time.deltaTime,.001f)));
                player.Aim(player.eye.transform.position+delta.normalized*8);
                var previous=player.transform.position;
                yield return null;
                float step=SprintDistance(previous,player.transform.position);leg.metres+=step;
                leg.peakMetresPerSecond=Mathf.Max(leg.peakMetresPerSecond,step/Mathf.Max(Time.deltaTime,.001f));
                NeedSprint(step<=speed*Time.deltaTime+.025f,"Unexpected teleport/speed boost: "+name);
                NeedSprint(SafePosition(player.transform.position),"Left the playable world: "+name);
                if(!stair)NeedSprint(player.transform.position.y>-.15f&&player.transform.position.y<.45f,"Climbed/fell off the street: "+name);
                NeedSprint(Time.realtimeSinceStartupAsDouble<deadline,"Route blocked: "+name+" point="+i+" target="+route[i]+" actual="+player.transform.position);
                if(Time.realtimeSinceStartupAsDouble>=nextLog){Debug.Log("SPRINT_ROUTE_PROGRESS: "+name+" metres="+leg.metres.ToString("F1")+" point="+i+"/"+route.Length);nextLog+=15;}
            }
            if(stair)NeedSprint(Mathf.Abs(player.transform.position.y-route[i].y)<.32f,"Missed stair height: "+name+" point="+i+" actual="+player.transform.position+" expected="+route[i]);
        }
        StopSprint();leg.realSeconds=Time.realtimeSinceStartupAsDouble-real;leg.gameMinutes=ElapsedGameMinutes-game;
        leg.oldRateProjectedGameMinutes=leg.realSeconds*10/7;leg.arrivalWatchMinutes=Minutes;
        NeedSprint(Math.Abs(leg.gameMinutes-leg.realSeconds*GameClock.GameMinutesPerRealSecond)<.04,"Watch disagrees with measured wall time: "+name);
        if(recorder){recorder.End();Destroy(recorder);}
        report.legs.Add(leg);File.WriteAllText(Path.Combine(captures,"sprint-results.json"),JsonUtility.ToJson(report,true));
        Debug.Log("SPRINT_LEG_PASS: "+name+" m="+leg.metres.ToString("F2")+" seconds="+leg.realSeconds.ToString("F2")+" gameMinutes="+leg.gameMinutes.ToString("F3"));
    }
    IEnumerator SprintInputChecks(SprintReport report) {
        foreach(bool sprint in new[]{false,true})foreach(bool diagonal in new[]{false,true}) {
            player.Place(new Vector3(60,.1f,20),0);yield return new WaitForSeconds(.2f);
            var p=player.transform.position;double real=Time.timeAsDouble;
            player.scriptedPlayerInput=true;player.scriptedSprint=sprint;
            player.scriptedDirection=diagonal?new Vector3(1,0,1):Vector3.right;
            yield return new WaitForSeconds(1);
            float measured=SprintDistance(p,player.transform.position)/(float)(Time.timeAsDouble-real);
            StopSprint();float expected=sprint?PlayerMotor.SprintSpeed:PlayerMotor.WalkSpeed;
            NeedSprint(Mathf.Abs(measured-expected)<.15f,"Straight/diagonal input speed mismatch: "+measured+" expected "+expected);
            Debug.Log("SPRINT_INPUT_PASS: sprint="+sprint+" diagonal="+diagonal+" speed="+measured);
        }
        report.normalizedDiagonal=true;
        var p0=player.transform.position;journal=true;player.scriptedPlayerInput=true;player.scriptedSprint=true;player.scriptedDirection=Vector3.right;
        double before=ElapsedGameMinutes;yield return new WaitForSeconds(.4f);StopSprint();
        NeedSprint(SprintDistance(p0,player.transform.position)<.001f&&ElapsedGameMinutes>before,"Reading must block sprint while watch advances");journal=false;
        SetMapOpen(true);yield return new WaitForSeconds(.4f);
        var route=new[]{player.transform.position,player.transform.position+Vector3.right*12};
        yield return SprintLegRun(route,PlayerMotor.SprintSpeed,"held-map-sprint",report);
        NeedSprint(mapOpen&&!Blocked,"Map blocked sprint");SetMapOpen(false);report.mapSprint=true;
        var door=Mechanism("shop-front-door");var layout=seamlessShop.layout;
        Vector3 W(float x,float z)=>layout.ToTown(new Vector3(x,.1f,z));
        if(door.IsOpen){door.Use();yield return new WaitForSeconds(.85f);}
        player.Place(W(0,-5.4f),0);yield return new WaitForSeconds(.2f);
        player.scriptedPlayerInput=true;player.scriptedSprint=true;player.scriptedDirection=seamlessShop.transform.forward;
        yield return new WaitForSeconds(1.0f);StopSprint();
        NeedSprint(seamlessShop.transform.InverseTransformPoint(player.transform.position).z<-3.6f,"Full sprint crossed a closed shop door");
        report.closedDoorBlocks=true;
        player.Place(W(0,-5.4f),0);yield return UseDoorRay(door);
        yield return SprintLegRun(new[]{player.transform.position,W(0,-2.7f)},PlayerMotor.SprintSpeed,"open-door-sprint",report);
        NeedSprint(IsInsideShop,"Sprint failed to enter the open shop");report.openDoorPasses=true;
        yield return Capture("sprint-open-shop");
        // The real 101-point stair course also remains traversable at full Shift.
        var tower=FindFirstObjectByType<ClockTowerInteriorLayout>();var towerDoor=Mechanism("clocktower-door");
        player.Place(new Vector3(0,.1f,-3.2f),0);
        if(!towerDoor.IsOpen)yield return UseDoorRay(towerDoor);
        var path=tower.WorldAscendingPath();var ascent=(new[]{player.transform.position}).Concat(path).ToArray();
        yield return SprintLegRun(ascent,PlayerMotor.SprintSpeed,"tower-sprint-up",report,false,true);
        yield return Capture("sprint-tower-top");
        var descent=(new[]{player.transform.position}).Concat(path.Reverse()).Concat(new[]{new Vector3(0,.1f,-3.2f)}).ToArray();
        yield return SprintLegRun(descent,PlayerMotor.SprintSpeed,"tower-sprint-down",report,false,true);report.stairs=true;
    }
    IEnumerator ReviewSprintRoute() {
        yield return new WaitForSeconds(.6f);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--sprint-safety-only")>=0) {
            var safety=new SprintReport();yield return SprintInputChecks(safety);safety.passed=true;
            File.WriteAllText(Path.Combine(captures,"sprint-results.json"),JsonUtility.ToJson(safety,true));
            Debug.Log("SPRINT_SAFETY_PASS");Application.Quit();yield break;
        }
        var input=JsonUtility.FromJson<SprintRouteInput>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"SprintReviewRoutes.json")));
        var outward=input.routes.Single(x=>x.id=="h-out").points;var back=input.routes.Single(x=>x.id=="h-back").points;
        var report=new SprintReport();
        foreach(float speed in new[]{4.1f,PlayerMotor.SprintSpeed}) {
            bool current=speed==PlayerMotor.SprintSpeed;string label=current?"current":"previous-speed";
            DayReviewTime(1055);DayNightCycle.Instance.SetReviewWeather(null);player.Place(outward[0],0);
            player.Aim(outward[1]+Vector3.up*1.66f);watch=true;yield return Capture(label+"-home-departure");watch=false;
            dayNightReviewFrozen=false;double start=ElapsedGameMinutes;
            yield return SprintLegRun(outward,speed,label+"-home-station",report,current);
            if(current) {
                // Measure the unchanged story budget honestly at the user-selected rate.
                // A missed story window must not abort the remaining controller checks.
                report.stationBefore1740=Minutes<1060;
                watch=true;yield return Capture("current-station-arrival");watch=false;
                double wait=Time.realtimeSinceStartupAsDouble;
                while(Minutes<1060)yield return null;
                // Ten real seconds represents a brief test stop, not authored buying time.
                yield return new WaitForSecondsRealtime(10);
                report.stationWaitRealSeconds=Time.realtimeSinceStartupAsDouble-wait;
            }
            yield return SprintLegRun(back,speed,label+"-station-home",report,current);
            if(current) {
                report.roundTripGameMinutes=ElapsedGameMinutes-start;report.homeArrivalWatchMinutes=Minutes;
                report.homeBefore1755=Minutes<1075;
                report.storyWindowFeasible=report.stationBefore1740&&report.homeBefore1755;
                Debug.Log("SPRINT_STORY_WINDOW: stationBefore1740="+report.stationBefore1740+" homeBefore1755="+report.homeBefore1755+" feasible="+report.storyWindowFeasible);
                watch=true;yield return Capture("current-home-return");watch=false;
            }
        }
        // Also walk the player's shortest map route in both directions, with collision.
        player.Place(outward[0],0);NeedSprint(townNavigation.Route(player.transform.position,outward.Last()),"Map cannot find station");
        var shortest=townNavigation.Corners.ToArray();
        yield return SprintLegRun(shortest,PlayerMotor.SprintSpeed,"map-shortest-home-station",report);
        NeedSprint(townNavigation.Route(player.transform.position,back.Last()),"Map cannot find home");
        yield return SprintLegRun(townNavigation.Corners.ToArray(),PlayerMotor.SprintSpeed,"map-shortest-station-home",report);
        yield return SprintInputChecks(report);
        player.Place(back.Last(),0);state.entries.Add(new NotebookEntry{id="E07",title="Sprint review retained progress"});
        state.dialogueTopics.Add("sprint-review-topic");state.openFixtures.Add("milk-reading-lamp-off");
        dayNightReviewFrozen=true;Save();File.WriteAllText(savePath+".sprint-proof.json",JsonUtility.ToJson(state,true));
        report.passed=true;File.WriteAllText(Path.Combine(captures,"sprint-results.json"),JsonUtility.ToJson(report,true));
        Debug.Log("SPRINT_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifySprintSave() {
        dayNightReviewFrozen=true;
        var proof=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath+".sprint-proof.json"));
        NeedSprint(clockLoadedElapsedMinutes==proof.elapsedGameMinutes,"Changed clock rate reset the saved phase");
        NeedSprint(SprintDistance(player.transform.position,new Vector3(proof.x,proof.y,proof.z))<.15f&&state.entries.Any(x=>x.id=="E07")&&state.dialogueTopics.Contains("sprint-review-topic")&&state.openFixtures.Contains("milk-reading-lamp-off"),"Reload lost position/evidence/fixtures");
        yield return Capture("sprint-save-restored");dayNightReviewFrozen=false;
        double before=ElapsedGameMinutes,real=Time.unscaledTimeAsDouble;yield return new WaitForSeconds(1);
        NeedSprint(Math.Abs(ElapsedGameMinutes-before-(Time.unscaledTimeAsDouble-real)*GameClock.GameMinutesPerRealSecond)<.003,"Reload did not use calibrated clock rate");
        Debug.Log("SPRINT_SAVE_PASS");Application.Quit();
    }
}
