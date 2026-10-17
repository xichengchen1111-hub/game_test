using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Street life in play (StreetLife), and its isolated review: --review --fresh --review-street-life
// (tools/opening/run_review.py street-life). Muted, own save; the normal save is untouched.
public partial class DemoApp {
    public Transform RideCarTransform=>rideCar?rideCar.transform:null;
    // Not during the first act: people from the hospital on, cars once the lawyer has driven off.
    public bool StreetLifeAllowed=>sceneReady&&CurrentScene=="Town"&&!opening&&!cellCinematic&&state.openingRideStage>=4;
    public bool StreetCarsAllowed=>StreetLifeAllowed&&!OpeningRideActive&&!rideCar&&state.openingRideStage>=5;
    void InitializeStreetLife() {
        if(!StreetLife.Enabled||CurrentScene!="Town"||StreetLife.Current||!player)return;
        StreetLife.Create(player.eye,player.transform);
    }

    [Serializable] class StreetLifeReviewResult {
        public bool passed;public int width,height;
        public int peopleMorning,carsMorning,peopleNightTarget,carsNightTarget,peopleNoonTarget,peopleRainTarget,carsEveningTarget;
        public int signalEntries,redRuns,stopSignEntries,stopViolations,crossingsStarted,kerbWaits,yieldsToPeople,honks;
        public int carOverlaps,carPersonContacts,personOverlaps,personPlayerOverlaps,roadWalking;
        public float minCarGap=float.MaxValue,minPersonGap=float.MaxValue,meanFps;
        public bool playerBlockStopped,playerBlockHonked,playerBlockResumed;
        public float elderlySpeed,elderlyStoop,caneTipHeight;public bool caneHeld;
        public float umbrellaShare;public bool organPlaying;public float crankHandError;public bool beggarSeated;
        public bool backpackCarried,backpackThumbnails,backpackTicketReturns,backpackPaperReturns,dossierTestimony;
        // The street cast (2026-10-16): the dog on its lead and the children.
        public List<string> figures=new List<string>();
        public float dogFurthest,dogFloat,leadLongest,boySpeed,girlSpeed;public int dogInWall,dogWalkingFrames,dogContacts,dogPlayerContacts;public bool leadInHand;
        // STREET-03: the stop lines set back for turning cars, the tightest clearances left by the
        // layout, and how often the last-resort guard had to hold a car (it should not have to).
        public string stopLines,waitingClearance,openTurnClearance;public int carGuardHolds;
        // STREET-05: in the minute and a half of traffic, walkers who went back to the kerb from a crossing
        // and cars that edged past somebody alongside; over the whole review, the longest a car stood
        // waiting for somebody on foot (not the player, who blocks a car on purpose here).
        public int turnBacks,edgedPast,madeWay;public float longestWaitForPerson,longestHeldByPerson;public string longestWaitForPersonCar,longestHeldByPersonCar;
        // The staged case (CarOnCrossing): a car let go soon after the walker is held up beside it, and one
        // held longer. Seconds from letting the car go until its body is off the walker's line and until the
        // walker is over; the car's top speed while within EdgeNear of the walker; how long the walker was
        // held before turning back.
        public float edgedCarOff=-1,edgedWalkerOver=-1,edgedTopSpeedNear=-1,heldTurnBackAfter=-1,heldCarOff=-1,heldWalkerOver=-1;
        public bool edgedCounted,edgedWalkerTurnedBack;
        public List<string> captures=new List<string>();
        public string verification;
    }
    StreetLifeReviewResult slResult;
    void SLNeed(bool ok,string why) {
        if(ok)return;
        Debug.LogError("STREET_LIFE_REVIEW_FAIL: "+why);
        if(slResult!=null)File.WriteAllText(Path.Combine(captures,"street-life-acceptance.json"),JsonUtility.ToJson(slResult,true));
        Application.Quit(1);throw new InvalidOperationException(why);
    }
    IEnumerator SLCapture(string name){slResult.captures.Add(name+".png");yield return Capture(name);}
    // The clock set without freezing it: traffic needs time to run.
    void StreetTime(double minutes) {
        dayNightReviewFrozen=false;paused=false;
        gameClock=new GameClock(GameClock.WrapMinutes(minutes-GameClock.InitialMinutes));
        DayNightCycle.Instance.Apply(Minutes);
    }
    IEnumerator ReviewStreetLife() {
        slResult=new StreetLifeReviewResult{width=Screen.width,height=Screen.height};
        // Well after the first act: belongings back, the lawyer gone.
        state.openingRevision=Math.Max(1,state.openingRevision);state.openingStage=4;state.openingEscort=4;state.belongingsCollected=true;
        state.openingRideRevision=1;state.openingRideStage=5;
        state.entries.Clear();
        foreach(var id in new[]{"E01","E03","E06","H17","H18","W02","Y03","H09","E04","Y04"})state.entries.Add(new NotebookEntry{id=id,title="Review "+id,body="Review body "+id,source="Review"});
        state.dialogueTopics.AddRange(new[]{"milkboy.delivery","milkboy.route","station-clerk.date","station-clerk.inspection","mechanic.where"});
        StreetTime(8*60+30);
        yield return new WaitForSeconds(1.5f);
        var life=StreetLife.Current;SLNeed(life,"street life was not created in Town");
        SLNeed(life.Net.lanes.Count>=20&&life.Net.junctions.Count>=9&&life.Net.turns.Count>40,"street network too small: lanes="+life.Net.lanes.Count+" junctions="+life.Net.junctions.Count);
        SLNeed(life.Net.turns.Any(t=>t.conflicts.Count>0),"no junction conflicts computed");
        // 1. The morning rush at Station Road × Middle Street, watched from the north-east corner.
        player.Place(new Vector3(36.4f,.12f,27.4f),225);player.Aim(new Vector3(28,1.2f,18));
        yield return new WaitForSeconds(12);
        slResult.peopleMorning=life.People;slResult.carsMorning=life.Cars;
        SLNeed(life.PeopleTarget>=24&&life.People>=life.PeopleTarget*.6f,"morning people "+life.People+"/"+life.PeopleTarget);
        SLNeed(life.CarTarget>=7&&life.Cars>=3,"morning cars "+life.Cars+"/"+life.CarTarget);
        yield return SLCapture("01-morning-signal-corner");
        // 2. A minute and a half of traffic: rules, gaps and contacts, every frame.
        life.Stats.Reset();
        float watch=Time.time+90,frames=0,seconds=0;
        var signalShots=new HashSet<string>();
        while(Time.time<watch) {
            frames++;seconds+=Time.unscaledDeltaTime;
            Audit(life);
            if(!signalShots.Contains("02")&&life.AllCars.Any(c=>c.Waiting=="rule"&&Vector3.Distance(c.transform.position,new Vector3(30,0,20))<14)){signalShots.Add("02");yield return SLCapture("02-cars-held-at-signal");}
            if(!signalShots.Contains("03")&&life.Walkers.Any(w=>w.Crossing&&Vector3.Distance(w.transform.position,new Vector3(30,0,20))<12)){signalShots.Add("03");yield return SLCapture("03-people-crossing-on-green");}
            yield return null;
        }
        slResult.meanFps=frames/Mathf.Max(.01f,seconds);
        var st=life.Stats;
        slResult.signalEntries=st.signalEntries;slResult.redRuns=st.redRuns;slResult.stopSignEntries=st.stopEntries;slResult.stopViolations=st.stopViolations;
        slResult.crossingsStarted=st.crossings;slResult.kerbWaits=st.kerbWaits;slResult.yieldsToPeople=st.yields;slResult.honks=st.honks;
        slResult.turnBacks=st.turnBacks;slResult.edgedPast=st.edgedPast;slResult.madeWay=st.madeWay;
        SLNeed(st.signalEntries>=3,"too few cars through the signal: "+st.signalEntries);
        SLNeed(st.redRuns==0,"cars ran the red: "+st.redRuns);
        SLNeed(st.stopViolations==0,"cars went past a STOP sign without stopping: "+st.stopViolations);
        SLNeed(slResult.carOverlaps==0&&slResult.carPersonContacts==0&&slResult.personOverlaps==0&&slResult.personPlayerOverlaps==0,"contacts: car-car "+slResult.carOverlaps+" car-person "+slResult.carPersonContacts+" person-person "+slResult.personOverlaps+" person-player "+slResult.personPlayerOverlaps);
        SLNeed(slResult.roadWalking==0,"people walking along the carriageway: "+slResult.roadWalking);
        SLNeed(st.crossings>=2,"too few people crossed: "+st.crossings);
        // 3. The player standing in the road: the car stops short, toots once, goes on when the way is clear.
        yield return PlayerBlocksCar(life);
        // 4. A STOP corner (South Street × Middle Street): a car coming down Middle Street stops fully with
        // its bumper at the line, waits at least a second, then goes on.
        var stopLane=life.Net.lanes.FirstOrDefault(l=>l.stopSign&&l.toJunction!=null&&Mathf.Abs(l.toJunction.centre.x-30)<.5f&&Mathf.Abs(l.toJunction.centre.z+122)<.5f&&l.dir.z<-.9f);
        SLNeed(stopLane!=null,"no STOP approach on Middle Street at South Street");
        // Beside its line, wherever the line stands (it was set back for turning cars, STREET-03).
        player.Place(stopLane.end+new Vector3(5.4f,.12f,4.05f),220);player.Aim(stopLane.end+new Vector3(1,1,-3.35f));
        life.Stats.Reset();var stopper=life.SpawnCarOn(stopLane,2,4.5f);SLNeed(stopper,"could not place the STOP-corner car");
        // Timed by the clock from the first stopped frame to the last (a screenshot pauses this loop).
        float stopStart=-1,stopEnd=-1,lineGap=99;bool entered=false,shot=false;
        for(float until=Time.time+40;Time.time<until&&!entered;) {
            Audit(life);
            if(stopper.path==stopLane){float toLine=stopLane.length-stopper.s-AmbientCar.Front;if(stopper.speed<.05f&&toLine<2.5f){if(stopStart<0)stopStart=Time.time;stopEnd=Time.time;lineGap=Mathf.Min(lineGap,toLine);}}
            else entered=true;
            if(stopStart>=0&&Time.time-stopStart>.4f&&!shot){shot=true;yield return SLCapture("04-stop-corner");}
            yield return null;
        }
        float stoppedAtLine=stopStart<0?0:stopEnd-stopStart;
        slResult.stopSignEntries=life.Stats.stopEntries;slResult.stopViolations=life.Stats.stopViolations;
        SLNeed(entered&&stoppedAtLine>=1&&lineGap>-.05f&&life.Stats.stopViolations==0,"STOP corner: entered="+entered+" stopped "+stoppedAtLine+" s, bumper "+lineGap+" m before the line, violations "+life.Stats.stopViolations);
        // 4b. A car on a crossing and a walker held up beside it: both on their way within seconds (STREET-05).
        yield return CarOnCrossing(life);
        // 5. The old gentleman: slower, bent, the stick planted on the ground.
        yield return Elderly(life);
        // 5b. The street cast: a woman walking her terrier on its lead, a boy and a girl.
        yield return StreetCast(life);
        // 6. Density by the hour and the rain (targets follow StreetDensity).
        StreetTime(3*60);yield return new WaitForSeconds(1.2f);slResult.peopleNightTarget=(int)life.PeopleTarget;slResult.carsNightTarget=(int)life.CarTarget;
        SLNeed(life.PeopleTarget<=1&&life.CarTarget<=1,"three in the morning should be empty: "+life.PeopleTarget+"/"+life.CarTarget);
        StreetTime(21*60+30);for(float until=Time.time+14;Time.time<until;){Audit(life);yield return null;}yield return SLCapture("05-night-street");
        StreetTime(12*60+20);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);yield return new WaitForSeconds(1.2f);slResult.peopleNoonTarget=(int)life.PeopleTarget;
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);yield return new WaitForSeconds(1.2f);slResult.peopleRainTarget=(int)life.PeopleTarget;
        SLNeed(slResult.peopleNoonTarget>=28&&slResult.peopleRainTarget<slResult.peopleNoonTarget*.75f,"noon/rain targets "+slResult.peopleNoonTarget+"/"+slResult.peopleRainTarget);
        // Umbrellas up in the rain.
        player.Place(new Vector3(-20,.12f,15.2f),90);player.Aim(new Vector3(0,1.4f,15.2f));
        for(float until=Time.time+12;Time.time<until;){Audit(life);yield return null;}
        var near=life.Walkers.Where(w=>Vector3.Distance(w.transform.position,player.transform.position)<60).ToList();
        slResult.umbrellaShare=near.Count==0?0:near.Count(w=>w.transform.Find("Umbrella")&&w.transform.Find("Umbrella").gameObject.activeSelf)/(float)near.Count;
        SLNeed(near.Count>=3&&slResult.umbrellaShare>=.5f,"umbrellas in the rain: "+slResult.umbrellaShare+" of "+near.Count);
        yield return SLCapture("06-rain-umbrellas");
        DayNightCycle.Instance.SetReviewWeather(null);
        StreetTime(17*60+35);yield return new WaitForSeconds(1.2f);slResult.carsEveningTarget=(int)life.CarTarget;
        SLNeed(life.CarTarget>=8,"evening rush cars target "+life.CarTarget);
        // 7. The pitches in the late morning: the organ grinder turning his crank, the beggar seated.
        // He sets up where nobody is watching: stand well away, looking the other way, first.
        StreetTime(10*60+15);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);
        player.Place(new Vector3(-75,.12f,15.2f),-90);yield return new WaitForSeconds(2);
        for(float until=Time.time+8;Time.time<until&&!life.Organ;)yield return null;
        SLNeed(life.Organ,"organ grinder not on his pitch at 10:15");
        var organ=life.Organ;player.Place(organ.transform.position-organ.transform.forward*3.2f+Vector3.up*.02f,0);player.Aim(organ.transform.position+Vector3.up*1.2f);
        yield return new WaitForSeconds(2.5f);
        var organHands=organ.GetComponent<NpcHands>();slResult.crankHandError=organHands?organHands.Error(true):99;
        var music=organ.GetComponentInChildren<AudioSource>();slResult.organPlaying=music&&music.isPlaying;
        SLNeed(slResult.organPlaying&&slResult.crankHandError<.09f,"organ: playing="+slResult.organPlaying+" crank hand error="+slResult.crankHandError);
        yield return SLCapture("07-organ-grinder");
        for(float until=Time.time+6;Time.time<until&&!life.Beggar;)yield return null;
        if(life.Beggar) {
            var beggar=life.Beggar;player.Place(beggar.transform.position+(-beggar.transform.forward)*2.4f+Vector3.up*.02f,0);player.Aim(beggar.transform.position+Vector3.up*.7f);
            yield return new WaitForSeconds(2);var pose=beggar.GetComponent<HospitalBedPose>();var hips=pose?pose.hips:null;
            slResult.beggarSeated=hips&&hips.position.y-beggar.transform.position.y<.62f;
            SLNeed(slResult.beggarSeated,"beggar not seated");yield return SLCapture("08-beggar");
        } else Debug.LogWarning("STREET_LIFE_REVIEW_NOTE: no beggar pitch was free");
        DayNightCycle.Instance.SetReviewWeather(null);
        SLNeed(slResult.carOverlaps==0&&slResult.carPersonContacts==0&&slResult.personOverlaps==0&&slResult.personPlayerOverlaps==0&&slResult.roadWalking==0,"later contacts: car-car "+slResult.carOverlaps+" car-person "+slResult.carPersonContacts+" person-person "+slResult.personOverlaps+" person-player "+slResult.personPlayerOverlaps+" road "+slResult.roadWalking);
        // 8. The briefcase and the dossier.
        yield return Briefcase();
        RecordStreetLayout(life,out slResult.stopLines,out slResult.waitingClearance,out slResult.openTurnClearance);
        slResult.carGuardHolds=life.GuardHolds;
        slResult.passed=true;
        slResult.verification="Isolated muted review player; program-driven player position, no physical keyboard/mouse. Townsfolk from the approved street cast (2026-10-16); the old woman is still the hospital's visitor. The terrier's gait is procedural (StreetDog).";
        File.WriteAllText(Path.Combine(captures,"street-life-acceptance.json"),JsonUtility.ToJson(slResult,true));
        Debug.Log("STREET_LIFE_REVIEW_PASS: rules, contacts, density, gait, street cast and dog, umbrellas, pitches, briefcase and dossier");
        Application.Quit();
    }
    // A longer look at the traffic alone (STREET-03; --review --fresh --review-street-traffic,
    // tools/opening/run_review.py street-traffic): every corner in turn at the morning rush, with every
    // car the town allows, time at double speed and a stalled frame every few seconds. No two cars may
    // touch and no car may stand two minutes on end waiting for cars or rules (a jam); the guard's holds
    // are counted (it should not have to hold anyone). A car held up by somebody on foot is measured
    // apart (AmbientCar.PersonHeldFor: only the time people are what it waits for, not the red or the
    // STOP sign before) and may not be held PersonWaitLimit: a walker held on a crossing by a car's body,
    // and the car waiting for that walker, used to stand until one of them was recycled out of sight,
    // and so did a queue of people backed up from a kerb into the road (STREET-05). Each hold over 10 s
    // is logged with where the walker stood and what held them. longestWaitForPerson keeps the earlier
    // measure (the whole standstill of a car last waiting for somebody on foot). Muted, own save; the
    // normal save is untouched.
    const float PersonWaitLimit=40;
    [Serializable] class StreetTrafficReviewResult {
        public bool passed;public int width,height,corners,frames,stalls,carsSeen;public float timeScale,simulatedSeconds,realSeconds;
        public int carOverlaps,carGuardHolds,signalEntries,redRuns,stopSignEntries,stopViolations;
        public float minCarGap,longestStop,longestWaitForPerson,longestHeldByPerson;public string longestStopCar,longestWaitForPersonCar,longestHeldByPersonCar,stopLines,waitingClearance,openTurnClearance;
        // STREET-05: walkers who went back to the kerb from a crossing, cars that edged past somebody
        // alongside, and holds by somebody on foot longer than 10 s.
        public int turnBacks,edgedPast,madeWay,personHoldsOver10s;
        // Not what this review is for (the street-life review checks them), but noted.
        public int carPersonContacts,personOverlaps,personPlayerOverlaps,roadWalking;
        public string verification;
    }
    StreetTrafficReviewResult stResult;
    void TrafficNeed(bool ok,string why) {
        if(ok)return;
        Time.timeScale=1;Debug.LogError("STREET_TRAFFIC_REVIEW_FAIL: "+why);
        if(stResult!=null)File.WriteAllText(Path.Combine(captures,"street-traffic-acceptance.json"),JsonUtility.ToJson(stResult,true));
        Application.Quit(1);throw new InvalidOperationException(why);
    }
    IEnumerator ReviewStreetTraffic() {
        slResult=new StreetLifeReviewResult{width=Screen.width,height=Screen.height};
        stResult=new StreetTrafficReviewResult{width=Screen.width,height=Screen.height,timeScale=2};
        state.openingRevision=Math.Max(1,state.openingRevision);state.openingStage=4;state.openingEscort=4;state.belongingsCollected=true;
        state.openingRideRevision=1;state.openingRideStage=5;
        StreetTime(8*60+20);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);
        yield return new WaitForSeconds(1.5f);
        var life=StreetLife.Current;TrafficNeed(life,"street life was not created in Town");
        RecordStreetLayout(life,out stResult.stopLines,out stResult.waitingClearance,out stResult.openTurnClearance);
        life.ForcedCars=StreetLife.CarCap;life.Stats.Reset();
        var seen=new HashSet<int>();float realStart=Time.realtimeSinceStartup,simulated=0;
        var longWaits=new Dictionary<AmbientCar,float>();
        Time.timeScale=stResult.timeScale;
        foreach(var j in life.Net.junctions.OrderBy(n=>n.centre.x).ThenBy(n=>n.centre.z)) {
            if(!StandAtCorner(life,j)){Debug.LogWarning("STREET_TRAFFIC_REVIEW_NOTE: no clear corner to stand at "+j.centre);continue;}
            stResult.corners++;
            for(float until=Time.time+80,stall=Time.time+7;Time.time<until;) {
                Audit(life);stResult.frames++;simulated+=Time.deltaTime;
                foreach(var c in life.AllCars) {
                    seen.Add(c.GetInstanceID());
                    if(c.Waiting=="person"||c.Waiting=="player") {
                        if(c.StoppedFor>stResult.longestWaitForPerson){stResult.longestWaitForPerson=c.StoppedFor;stResult.longestWaitForPersonCar=c.Describe();}
                    }
                    else if(c.StoppedFor>stResult.longestStop){stResult.longestStop=c.StoppedFor;stResult.longestStopCar=c.Describe();}
                    if(c.PersonHeldFor>stResult.longestHeldByPerson){stResult.longestHeldByPerson=c.PersonHeldFor;stResult.longestHeldByPersonCar=c.Describe();}
                    // Each hold over 10 s once (a new one starts when the car has moved since), and again at 30 s.
                    if(c.PersonHeldFor>10) {
                        bool fresh=!longWaits.TryGetValue(c,out float since)||since>c.PersonHeldFor;
                        if(fresh){stResult.personHoldsOver10s++;Debug.LogWarning("STREET_CAR_LONG_WAIT_FOR_PERSON: "+c.Describe());}
                        else if(since<=30&&c.PersonHeldFor>30)Debug.LogWarning("STREET_CAR_LONG_WAIT_FOR_PERSON (still): "+c.Describe());
                        longWaits[c]=c.PersonHeldFor;
                    }
                }
                // A long frame, as when the window is resized: the street takes it as one short step.
                if(Time.time>=stall){stall=Time.time+7;stResult.stalls++;System.Threading.Thread.Sleep(300);}
                yield return null;
            }
            Debug.Log("STREET_TRAFFIC_CORNER: "+j.centre+" overlaps "+slResult.carOverlaps+" guard "+life.GuardHolds+" signal "+life.Stats.signalEntries+" stop "+life.Stats.stopEntries+" longest stop "+stResult.longestStop.ToString("0.0")+" s");
        }
        Time.timeScale=1;life.ForcedCars=null;DayNightCycle.Instance.SetReviewWeather(null);
        var st=life.Stats;
        stResult.simulatedSeconds=simulated;stResult.realSeconds=Time.realtimeSinceStartup-realStart;stResult.carsSeen=seen.Count;
        stResult.carOverlaps=slResult.carOverlaps;stResult.minCarGap=slResult.minCarGap;stResult.carGuardHolds=life.GuardHolds;
        stResult.signalEntries=st.signalEntries;stResult.redRuns=st.redRuns;stResult.stopSignEntries=st.stopEntries;stResult.stopViolations=st.stopViolations;
        stResult.carPersonContacts=slResult.carPersonContacts;stResult.personOverlaps=slResult.personOverlaps;stResult.personPlayerOverlaps=slResult.personPlayerOverlaps;stResult.roadWalking=slResult.roadWalking;
        stResult.turnBacks=st.turnBacks;stResult.edgedPast=st.edgedPast;stResult.madeWay=st.madeWay;
        TrafficNeed(stResult.corners==life.Net.junctions.Count,"stood at "+stResult.corners+" of "+life.Net.junctions.Count+" corners");
        TrafficNeed(stResult.carOverlaps==0,"cars touched on "+stResult.carOverlaps+" frames (min gap "+stResult.minCarGap+")");
        TrafficNeed(stResult.longestStop<120,"a car stood still "+stResult.longestStop+" s waiting for cars or rules: "+stResult.longestStopCar);
        TrafficNeed(stResult.longestHeldByPerson<PersonWaitLimit,"a car was held "+stResult.longestHeldByPerson+" s by somebody on foot: "+stResult.longestHeldByPersonCar);
        TrafficNeed(st.redRuns==0&&st.stopViolations==0,"rules broken: red "+st.redRuns+" STOP "+st.stopViolations);
        TrafficNeed(st.signalEntries>=10&&st.stopEntries>=10,"too little traffic through the corners: signal "+st.signalEntries+" STOP "+st.stopEntries);
        stResult.passed=true;
        stResult.verification="Isolated muted review player; program-driven player position, time at double speed, a 0.3 s stall every 7 s of game time. Overlaps, standstills and rules are measured every frame; guard holds are counted from STREET_CAR_GUARD. carsSeen counts pooled car objects, not trips. Holds by people on foot (AmbientCar.PersonHeldFor) are judged against "+PersonWaitLimit+" s (STREET-05); walkers on the carriageway and car-person contacts are noted here, judged by the street-life review.";
        File.WriteAllText(Path.Combine(captures,"street-traffic-acceptance.json"),JsonUtility.ToJson(stResult,true));
        Debug.Log("STREET_TRAFFIC_REVIEW_PASS: "+stResult.corners+" corners, "+stResult.simulatedSeconds.ToString("0")+" s of traffic, 0 overlaps, guard holds "+stResult.carGuardHolds+", longest stop for cars or rules "+stResult.longestStop.ToString("0.0")+" s, longest held by someone on foot "+stResult.longestHeldByPerson.ToString("0.0")+" s ("+stResult.longestHeldByPersonCar+"), longest standstill last waiting for someone on foot "+stResult.longestWaitForPerson.ToString("0.0")+" s, holds over 10 s "+stResult.personHoldsOver10s+", turned back "+stResult.turnBacks+", edged past "+stResult.edgedPast+", made way "+stResult.madeWay);
        Application.Quit();
    }
    // Off the footway at one of the junction's four corners (the west ones first, if asked), looking at the middle of it.
    bool StandAtCorner(StreetLife life,StreetNet.Junction j,bool westFirst=false) {
        var corners=westFirst?new[]{(-1,1),(-1,-1),(1,1),(1,-1)}:new[]{(1,1),(-1,1),(1,-1),(-1,-1)};
        foreach(float extra in new[]{1.2f,2.4f,.4f})foreach(var (sx,sz) in corners) {
            var ns=j.ns.road;var ew=j.ew.road;
            var at=j.centre+new Vector3(sx*(j.ns.half+ns.strip+ns.walk+extra),.12f,sz*(j.ew.half+ew.strip+ew.walk+extra));
            if(!life.Free(at,null))continue;
            player.Place(at,0);player.Aim(j.centre+Vector3.up*1.2f);return true;
        }
        return false;
    }
    // Contacts and road-walking, sampled each frame.
    readonly Dictionary<long,float> carOverlapLogged=new Dictionary<long,float>();
    void Audit(StreetLife life) {
        var cars=life.AllCars;var people=life.Walkers;var p=player.transform.position;
        for(int i=0;i<cars.Count;i++)for(int k=i+1;k<cars.Count;k++) {
            float gap=CarGap(cars[i],cars[k]);slResult.minCarGap=Mathf.Min(slResult.minCarGap,gap);
            if(gap<-.02f) {
                slResult.carOverlaps++;
                // The first frame in full (where each was, on which way, how fast, waiting for what),
                // then only as the overlap changes.
                long a=cars[i].GetInstanceID(),b=cars[k].GetInstanceID();long key=a<b?(a<<32)^(b&0xffffffffL):(b<<32)^(a&0xffffffffL);
                if(!carOverlapLogged.TryGetValue(key,out float last))Debug.LogWarning("STREET_CAR_OVERLAP_START: gap="+gap+" | "+cars[i].Describe()+" | "+cars[k].Describe());
                else if(Mathf.Abs(gap-last)>=.005f)Debug.LogWarning("STREET_CAR_OVERLAP: "+cars[i].name+" "+cars[k].name+" gap="+gap);
                else continue;
                carOverlapLogged[key]=gap;
            }
        }
        foreach(var c in cars) {
            if(c.Waiting=="person"&&c.StoppedFor>slResult.longestWaitForPerson){slResult.longestWaitForPerson=c.StoppedFor;slResult.longestWaitForPersonCar=c.Describe();}
            if(c.PersonHeldFor>slResult.longestHeldByPerson){slResult.longestHeldByPerson=c.PersonHeldFor;slResult.longestHeldByPersonCar=c.Describe();}
        }
        foreach(var c in cars){foreach(var w in people){float d=PointToCar(c,w.transform.position)-.25f;slResult.minPersonGap=Mathf.Min(slResult.minPersonGap,d);if(d<-.02f){slResult.carPersonContacts++;if(roadLogged.Add(c.GetInstanceID()^w.GetInstanceID()))Debug.LogWarning("STREET_CAR_PERSON_CONTACT: "+c.name+" "+c.transform.position+" speed="+c.speed+" waiting="+c.Waiting+" path="+c.path.GetType().Name+" | "+w.name+" "+w.transform.position+" "+w.Activity+" d="+d);}}
            float dp=PointToCar(c,p)-.25f;if(dp<-.02f){slResult.carPersonContacts++;Debug.LogWarning("STREET_CAR_PLAYER_CONTACT: "+c.name+" d="+dp);}}
        for(int i=0;i<people.Count;i++) {
            var a=people[i].transform.position;
            for(int k=i+1;k<people.Count;k++){var d=a-people[k].transform.position;d.y=0;if(d.magnitude<.4f&&people[i].partner!=people[k]){slResult.personOverlaps++;Debug.LogWarning("STREET_PERSON_OVERLAP: "+people[i].name+" "+people[k].name);}}
            var dp=a-p;dp.y=0;if(dp.magnitude<.47f){slResult.personPlayerOverlaps++;Debug.LogWarning("STREET_PERSON_PLAYER_OVERLAP: "+people[i].name);}
            if(people[i].dog&&!people[i].Gone) {
                var g=people[i].dog.Position;var gp=g-p;gp.y=0;if(gp.magnitude<.3f){slResult.dogPlayerContacts++;Debug.LogWarning("STREET_DOG_PLAYER_CONTACT: "+gp.magnitude);}
                for(int k=0;k<people.Count;k++){if(k==i||people[k].Gone)continue;var gd=g-people[k].transform.position;gd.y=0;if(gd.magnitude<.28f){slResult.dogContacts++;Debug.LogWarning("STREET_DOG_PERSON_CONTACT: "+people[k].name+" "+gd.magnitude);}}
            }
            // Walking along a carriageway that carries cars, not crossing it (the garden lane has no traffic).
            if(!people[i].Crossing&&people[i].Velocity.sqrMagnitude>.04f&&OnTrafficRoad(life,a)){slResult.roadWalking++;if(roadLogged.Add(people[i].GetInstanceID()))Debug.LogWarning("STREET_PERSON_ON_ROAD: "+people[i].name+" "+a+" "+people[i].Activity);}
        }
    }
    readonly HashSet<int> roadLogged=new HashSet<int>();
    static bool OnTrafficRoad(StreetLife life,Vector3 p) {
        foreach(var c in life.Net.carriages) {
            bool over=c.eastWest?Mathf.Abs(p.z-c.centre)<c.half-.15f&&p.x>=c.from&&p.x<=c.to:Mathf.Abs(p.x-c.centre)<c.half-.15f&&p.z>=c.from&&p.z<=c.to;
            if(over)return true;
        }
        return false;
    }
    // Separating axes of the two body boxes (1.78 × 4.3, centre 0.17 m behind the origin), in plan: the
    // same measure the cars' own guard uses.
    static float CarGap(AmbientCar a,AmbientCar b)=>AmbientCar.BoxGap(a.transform.position,a.transform.forward,b.transform.position,b.transform.forward);
    // For the acceptance files: which stop lines were set back, and the tightest clearances the layout leaves.
    static void RecordStreetLayout(StreetLife life,out string lines,out string waiting,out string open) {
        var moved=life.Net.lanes.Where(l=>l.setback>0).ToList();var f=System.Globalization.CultureInfo.InvariantCulture;
        lines=moved.Count==0?"none":moved.Count+" lines set back "+moved.Min(l=>l.setback).ToString("0.00",f)+"–"+moved.Max(l=>l.setback).ToString("0.00",f)+" m: "+string.Join(", ",moved.Select(l=>l.ToString()));
        waiting=life.Net.WaitingClearance(out var w).ToString("0.000",f)+" m ("+w+")";
        open=life.Net.OpenTurnClearance(out var o).ToString("0.000",f)+" m ("+o+")";
        Debug.Log("STREET_LAYOUT: "+lines+" | waiting clearance "+waiting+" | open turns "+open);
    }
    static float PointToCar(AmbientCar c,Vector3 p) {
        var local=c.transform.InverseTransformPoint(p);local.z+=.17f;
        float dx=Mathf.Max(0,Mathf.Abs(local.x)-.89f),dz=Mathf.Max(0,Mathf.Abs(local.z)-2.15f);
        if(dx==0&&dz==0)return -Mathf.Min(.89f-Mathf.Abs(local.x),2.15f-Mathf.Abs(local.z));
        return Mathf.Sqrt(dx*dx+dz*dz);
    }
    IEnumerator PlayerBlocksCar(StreetLife life) {
        // Station Road westbound, mid-block between the square and Middle Street.
        AmbientCar car=null;
        for(float until=Time.time+40;Time.time<until&&!car;) {
            car=life.AllCars.FirstOrDefault(c=>c.path is StreetNet.Lane l&&l.carriage.eastWest&&l.carriage.road.id=="R01"&&l.length-c.s>30&&c.speed>3&&Vector3.Distance(c.transform.position,player.transform.position)<80);
            yield return null;
        }
        SLNeed(car,"no car on Station Road to stand in front of");
        var lane=(StreetNet.Lane)car.path;var spot=lane.Point(Mathf.Min(lane.length-2,car.s+22));spot.y=.06f;
        player.Place(spot,0);player.Aim(car.transform.position+Vector3.up*1.2f);
        float hold=Time.time+9;bool honked=false;int honks=life.Stats.honks;
        while(Time.time<hold){Audit(life);if(life.Stats.honks>honks)honked=true;yield return null;}
        float gap=PointToCar(car,player.transform.position)-.25f;
        slResult.playerBlockStopped=car.speed<.05f&&gap>.3f;slResult.playerBlockHonked=honked;
        SLNeed(slResult.playerBlockStopped,"car did not stop short of the player: speed="+car.speed+" gap="+gap);
        SLNeed(honked,"car did not sound its horn after waiting");
        yield return SLCapture("09-car-waits-for-player");
        player.Place(spot+Vector3.Cross(Vector3.up,lane.dir)*3.3f,0);
        float resume=Time.time+6;while(Time.time<resume&&car.speed<1){Audit(life);yield return null;}
        slResult.playerBlockResumed=car.speed>=1;SLNeed(slResult.playerBlockResumed,"car did not move on after the player stepped aside");
    }
    // 4b. STREET-05, staged at Middle Street × Residential Street: a car that has pulled onto the north
    // crossing in front of somebody already on it, kept there by the player standing ahead of it in the
    // junction. The walker comes up to its side and is held up there. Let go half a second later, the
    // car edges past at a walking pace and the walker goes on behind it; kept four seconds, the walker
    // goes back to the kerb, the car drives off and the walker crosses after it. Contacts are audited
    // every frame, as everywhere in the review. Fewer cars meanwhile, for a quiet moment to set it up.
    IEnumerator CarOnCrossing(StreetLife life) {
        var j=life.Net.junctions.FirstOrDefault(n=>Mathf.Abs(n.centre.x-30)<.5f&&Mathf.Abs(n.centre.z+75)<.5f);
        var turn=j==null?null:life.Net.turns.FirstOrDefault(t=>t.junction==j&&t.kind==0&&t.from.dir.z<-.9f);
        var crossing=j==null?null:life.Net.walkEdges.FirstOrDefault(e=>e.junction==j&&e.crosses==j.ns&&(e.a.point.z+e.b.point.z)/2>j.centre.z);
        SLNeed(turn!=null&&crossing!=null,"no way straight on southward, or no north crossing, at Middle Street × Residential Street");
        var west=crossing.a.point.x<crossing.b.point.x?crossing.a:crossing.b;var east=crossing.Other(west);
        life.ForcedCars=3;StandAtCorner(life,j);
        foreach(bool kept in new[]{false,true}) {
            StreetWalker walker=null;AmbientCar car=null;float line=0,sCar=0;
            for(int attempt=0;attempt<4&&!car;attempt++) {
                // Somebody setting out from the west kerb at a quiet moment...
                walker=null;
                for(float until=Time.time+60;Time.time<until&&!walker;){Audit(life);if(QuietCrossing(life,turn,west))walker=life.SpawnCrossing(StreetDensity.Folk.Man,crossing,west);yield return null;}
                SLNeed(walker,"no quiet moment to send somebody over the north crossing at (30,-75)");
                for(float until=Time.time+30;Time.time<until&&!(walker.Crossing&&walker.transform.position.x>west.point.x+.9f);){Audit(life);yield return null;}
                // Under way, a metre out (held up at the kerb or gone on too far: another one).
                if(!walker.Crossing||walker.transform.position.x<west.point.x+.9f||walker.transform.position.x>west.point.x+1.6f)continue;
                // ...and then a car standing over their way across, its front half across their line.
                line=walker.transform.position.z;sCar=AtZ(turn,line+.93f);
                var at=turn.Point(sCar);var facing=turn.Tangent(sCar);var hold=turn.Point(Mathf.Min(turn.length,sCar+AmbientCar.Front+4.5f));
                bool clear=life.AllCars.All(c=>AmbientCar.BoxGap(at,facing,c.transform.position,c.transform.forward)>1&&AmbientCar.PointGap(c.transform.position,c.transform.forward,hold)>1.2f)&&
                    life.Walkers.All(w=>AmbientCar.PointGap(at,facing,w.transform.position)>(w==walker?.5f:.8f)&&Vector3.Distance(w.transform.position,hold)>1.5f);
                if(!clear)continue;
                // The player in the junction ahead of it, where it waits for them, looking back at the crossing.
                player.Place(new Vector3(hold.x,.12f,hold.z),0);player.Aim(new Vector3(west.point.x+2,1.1f,line));
                car=life.SpawnCarOn(turn,sCar,0);
            }
            SLNeed(car,"could not stand a car on the north crossing at (30,-75)");
            int turnBacks=life.Stats.turnBacks,edged=life.Stats.edgedPast;
            // Held up beside it.
            float held=-1;
            for(float until=Time.time+8;Time.time<until&&held<0;){Audit(life);if(walker.Velocity.sqrMagnitude<.01f&&AmbientCar.PointGap(car.transform.position,car.transform.forward,walker.transform.position)<.45f)held=Time.time;yield return null;}
            SLNeed(held>=0,"the walker was not held up beside the car on the crossing: "+walker.Describe(car.transform));
            float turned=-1;bool backShot=false;
            for(float until=held+(kept?4:.5f);Time.time<until;) {
                Audit(life);if(turned<0&&walker.Activity=="turn-back")turned=Time.time;
                // Looking from where the player holds the car: the walker going back past its front.
                if(!backShot&&turned>=0&&Time.time>turned+.7f){backShot=true;player.Aim(Vector3.Lerp(walker.transform.position,car.transform.position+car.transform.forward*AmbientCar.Front,.35f)+Vector3.up*1.1f);yield return SLCapture("19-walker-turns-back");}
                yield return null;
            }
            // Let go: the player steps off the road (on the walker's side when it edges past, to see them).
            StandAtCorner(life,j,!kept);float go=Time.time,carOff=-1,over=-1,top=0;bool shot=false;
            for(float until=go+30;Time.time<until&&(carOff<0||over<0);) {
                Audit(life);
                var cp=car.transform.position;
                // Its speed while the walker stands within EdgeNear beside its front half.
                var beside=car.transform.InverseTransformPoint(walker.transform.position);
                if(beside.z+AmbientCar.CentreBack>0&&AmbientCar.PointGap(cp,car.transform.forward,walker.transform.position)<StreetLife.EdgeNear)top=Mathf.Max(top,car.speed);
                if(turned<0&&walker.Activity=="turn-back")turned=Time.time;
                if(carOff<0&&cp.z+AmbientCar.CentreBack+AmbientCar.HalfLength<line-.3f)carOff=Time.time-go;
                // Over: past the far side of the carriageway (the crossing ends on the footway there, STREET-05).
                if(over<0&&walker.transform.position.x>east.point.x-1.6f)over=Time.time-go;
                if(!shot&&!kept&&Time.time>go+1.4f){shot=true;player.Aim(walker.transform.position+Vector3.up*1.1f);yield return SLCapture("18-car-edges-past-walker");}
                yield return null;
            }
            // Whether the walker got over afterwards is noted, not judged: people bunched at the far corner can
            // hold them up there (STREET-05 limits), with no car waiting.
            if(over<0)Debug.LogWarning("STREET_CROSSING_STAGED_NOTE: the walker was not over 30 s after the car went: "+walker.Describe());
            Debug.Log("STREET_CROSSING_STAGED: "+(kept?"kept":"let go")+" held "+(turned<0?"no turn-back":"turned back after "+(turned-held).ToString("0.0")+" s")+", car off the line "+carOff.ToString("0.0")+" s and walker over "+over.ToString("0.0")+" s after letting go, top speed beside "+top.ToString("0.00")+" m/s, edged "+(life.Stats.edgedPast-edged)+", turned back "+(life.Stats.turnBacks-turnBacks));
            if(kept) {
                slResult.heldTurnBackAfter=turned<0?-1:turned-held;slResult.heldCarOff=carOff;slResult.heldWalkerOver=over;
                SLNeed(turned>=0&&turned-held<3.5f,"kept on the crossing, the walker did not go back to the kerb: "+walker.Describe(car.transform));
                SLNeed(carOff>=0&&carOff<8,"after the walker went back, the car did not drive off the crossing: "+car.Describe());
            } else {
                slResult.edgedCarOff=carOff;slResult.edgedWalkerOver=over;slResult.edgedTopSpeedNear=top;slResult.edgedCounted=life.Stats.edgedPast>edged;slResult.edgedWalkerTurnedBack=turned>=0;
                SLNeed(carOff>=0&&carOff<8&&slResult.edgedCounted&&top<1.3f,"let go, the car did not edge past the walker: off "+carOff+" s, top speed beside "+top+", edged "+slResult.edgedCounted+" | "+car.Describe());
            }
        }
        life.ForcedCars=null;
    }
    // No car on or close to the way through, nobody on the crossing, nobody standing right at the kerb (a
    // busy corner may have people about it: anyone the car would be set down beside is checked again after).
    bool QuietCrossing(StreetLife life,StreetNet.Turn turn,StreetNet.WalkNode kerb) {
        var mid=turn.Point(turn.length*.25f);
        if(life.AllCars.Any(c=>c.path==turn||Vector3.Distance(c.transform.position,mid)<10))return false;
        if(life.Walkers.Any(w=>w.Crossing&&Vector3.Distance(w.transform.position,mid)<6||Vector3.Distance(w.transform.position,kerb.point)<1.6f))return false;
        return life.Free(kerb.point,null);
    }
    // Where along a way going south it reaches z.
    static float AtZ(StreetNet.Path path,float z) {
        float best=0,bestD=float.MaxValue;
        for(float s=0;s<=path.length;s+=.01f){float d=Mathf.Abs(path.Point(s).z-z);if(d<bestD){bestD=d;best=s;}}
        return best;
    }
    IEnumerator Elderly(StreetLife life) {
        StreetTime(10*60);life.ForcedPeople=30;
        StreetWalker old=null;
        for(float until=Time.time+40;Time.time<until&&!old;) {
            old=life.Walkers.FirstOrDefault(w=>w.kind==StreetDensity.Folk.OldMan&&w.Velocity.magnitude>.3f&&w.transform.Find("Walking stick"));
            yield return null;
        }
        life.ForcedPeople=null;
        if(!old){Debug.LogWarning("STREET_LIFE_REVIEW_NOTE: no old gentleman walking within 40 s");yield break;}
        float speedSum=0;int n=0;
        for(float until=Time.time+4;Time.time<until;){speedSum+=old.Velocity.magnitude;n++;yield return null;}
        slResult.elderlySpeed=speedSum/Mathf.Max(1,n);slResult.elderlyStoop=old.stoop;
        var stick=old.transform.Find("Walking stick");var hands=old.GetComponent<NpcHands>();
        var tip=stick.position+stick.up*-.86f;slResult.caneTipHeight=tip.y-life.GroundAt(tip,old.transform.position.y);
        slResult.caneHeld=hands&&hands.Weight(true)>.5f&&hands.Error(true)<.1f;
        SLNeed(slResult.elderlySpeed<1.2f&&old.stoop>=8,"old gentleman too quick or upright: "+slResult.elderlySpeed+" m/s, stoop "+old.stoop);
        SLNeed(slResult.caneHeld&&Mathf.Abs(slResult.caneTipHeight)<.12f,"walking stick not held or not on the ground: held="+slResult.caneHeld+" tip="+slResult.caneTipHeight);
        player.Place(old.transform.position+old.Facing*3.4f+Vector3.Cross(Vector3.up,old.Facing)*1.2f,0);player.Aim(old.transform.position+Vector3.up*1.1f);
        yield return new WaitForSeconds(.2f);yield return SLCapture("10-old-gentleman");
    }
    // The street cast: which figures are on the streets, then a dog walker and her terrier followed
    // for twenty seconds (the dog stays within the lead's reach, out of walls, on the ground, the lead
    // in her left hand) and a boy and a girl at a child's pace.
    IEnumerator StreetCast(StreetLife life) {
        StreetTime(8*60+10);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);life.ForcedPeople=30;
        player.Place(new Vector3(-20,.12f,15.2f),90);player.Aim(new Vector3(0,1.4f,15.2f));
        for(float until=Time.time+3;Time.time<until;){Audit(life);yield return null;}
        var walker=life.SpawnKind(StreetDensity.Folk.DogWalker,player.transform.position,5,18);
        SLNeed(walker&&walker.dog,"could not place a dog walker with her dog");
        var dog=walker.dog;var hands=walker.GetComponent<NpcHands>();
        bool shotWide=false,shotClose=false;int handFrames=0,frames=0;
        for(float until=Time.time+20;Time.time<until&&!walker.Gone;) {
            Audit(life);frames++;
            var o=walker.transform.position;var d=dog.Position-o;d.y=0;slResult.dogFurthest=Mathf.Max(slResult.dogFurthest,d.magnitude);
            slResult.dogFloat=Mathf.Max(slResult.dogFloat,Mathf.Abs(dog.Position.y-life.GroundAt(dog.Position,dog.Position.y+.3f)));
            if(Physics.CheckCapsule(dog.Position+Vector3.up*.2f,dog.Position+Vector3.up*.42f,.1f,~((1<<StreetLife.Layer)|(1<<2)),QueryTriggerInteraction.Ignore)){slResult.dogInWall++;Debug.LogWarning("STREET_DOG_IN_WALL: "+dog.Position);}
            if(dog.Lead){var a=dog.Lead.GetPosition(0);var b=dog.Lead.GetPosition(dog.Lead.positionCount-1);slResult.leadLongest=Mathf.Max(slResult.leadLongest,Vector3.Distance(a,b));}
            if(dog.Speed>.3f)slResult.dogWalkingFrames++;
            if(hands&&hands.Weight(false)>.5f&&hands.Error(false)<.12f)handFrames++;
            // Keep them in view from the side, a few metres off.
            var side=Vector3.Cross(Vector3.up,walker.Facing);
            if(Time.frameCount%20==0){var at=o+side*-3.2f+walker.Facing*1.2f;at.y=o.y+.02f;if(life.Free(at,null))player.Place(at,0);player.Aim(Vector3.Lerp(o,dog.Position,.4f)+Vector3.up*.7f);}
            if(!shotWide&&dog.Speed>.4f&&Time.time>until-15){shotWide=true;yield return SLCapture("14-dog-walker");}
            if(!shotClose&&dog.Speed>.4f&&Time.time>until-8) {
                shotClose=true;var at=dog.Position+side*-1.9f+walker.Facing*.6f;at.y=o.y+.02f;
                if(life.Free(at,null))player.Place(at,0);player.Aim(dog.Position+Vector3.up*.25f);yield return null;yield return SLCapture("15-terrier");
            }
            yield return null;
        }
        slResult.leadInHand=handFrames>frames*.7f;
        SLNeed(slResult.dogFurthest<1.7f,"the dog strayed "+slResult.dogFurthest+" m from its owner");
        SLNeed(slResult.dogInWall==0,"the dog stood in a wall on "+slResult.dogInWall+" frames");
        SLNeed(slResult.dogFloat<.08f,"the dog floated or sank "+slResult.dogFloat+" m");
        SLNeed(slResult.leadLongest<1.56f,"the lead stretched to "+slResult.leadLongest+" m");
        SLNeed(slResult.dogWalkingFrames>frames*.3f,"the dog hardly walked: "+slResult.dogWalkingFrames+"/"+frames);
        SLNeed(slResult.leadInHand,"the lead was not held in her left hand: "+handFrames+"/"+frames);
        SLNeed(slResult.dogContacts==0&&slResult.dogPlayerContacts==0,"the dog touched people "+slResult.dogContacts+" / the player "+slResult.dogPlayerContacts);
        // Children: at a child's pace, never stopped for a chat.
        foreach(var kind in new[]{StreetDensity.Folk.Boy,StreetDensity.Folk.Girl}) {
            var child=life.SpawnKind(kind,player.transform.position,5,18);SLNeed(child,"could not place a "+kind);
            float sum=0;int n=0;
            for(float until=Time.time+6;Time.time<until&&!child.Gone;){Audit(life);if(child.Velocity.magnitude>.2f){sum+=child.Velocity.magnitude;n++;}
                if(Time.frameCount%20==0){var o=child.transform.position;var at=o+child.Facing*3f+Vector3.Cross(Vector3.up,child.Facing)*1.3f;at.y=o.y+.02f;if(life.Free(at,null))player.Place(at,0);player.Aim(o+Vector3.up*.9f);}
                yield return null;}
            float speed=sum/Mathf.Max(1,n);if(kind==StreetDensity.Folk.Boy)slResult.boySpeed=speed;else slResult.girlSpeed=speed;
            SLNeed(speed>.8f&&speed<1.45f&&child.Activity!="chat","a "+kind+" walked at "+speed+" m/s ("+child.Activity+")");
            yield return SLCapture(kind==StreetDensity.Folk.Boy?"16-boy":"17-girl");
        }
        // Which figures were out (prefab names: kind and figure).
        slResult.figures=life.Walkers.Select(w=>w.name).Distinct().OrderBy(n=>n).ToList();
        life.ForcedPeople=null;DayNightCycle.Instance.SetReviewWeather(null);
    }
    IEnumerator Briefcase() {
        player.Place(new Vector3(-20,.12f,15.2f),90);yield return new WaitForSeconds(.5f);
        OpenBackpack();SLNeed(backpack,"B did not open the briefcase");
        slResult.backpackCarried=packItems.Take(packCarried).Select(p=>p.id).SequenceEqual(new[]{"pack-watch","pack-map","E02","pack-notebook","pack-wallet"});
        SLNeed(slResult.backpackCarried,"carried items: "+string.Join(",",packItems.Take(packCarried).Select(p=>p.id)));
        SLNeed(!packItems.Any(p=>p.id=="E04"||p.id=="Y04"),"testimony in the briefcase");
        for(float until=Time.time+6;Time.time<until&&packThumbs.Count<packItems.Count(p=>p.item);)yield return null;
        slResult.backpackThumbnails=packThumbs.Count>=packItems.Count(p=>p.item);SLNeed(slResult.backpackThumbnails,"thumbnails "+packThumbs.Count+"/"+packItems.Count);
        yield return new WaitForSeconds(.4f);yield return SLCapture("11-briefcase");
        ActivatePackItem(packItems.FindIndex(p=>p.id=="E02"));yield return new WaitForSeconds(.5f);
        SLNeed(inspected==carriedTicket&&ModelEvidence,"ticket not examined from the briefcase");yield return SLCapture("12-briefcase-ticket");
        CloseInspect();yield return null;slResult.backpackTicketReturns=backpack&&!inspected;SLNeed(slResult.backpackTicketReturns,"closing the ticket did not return to the briefcase");
        int paper=packItems.FindIndex(p=>p.id=="W02"||p.id=="E01");
        if(paper>=0){ActivatePackItem(paper);yield return new WaitForSeconds(.5f);SLNeed(ModelEvidence,"paper evidence not examined");ToggleEvidenceDetails();yield return new WaitForSeconds(.3f);yield return SLCapture("13-briefcase-paper");CloseInspect();yield return null;slResult.backpackPaperReturns=backpack;SLNeed(backpack,"closing the paper did not return");}
        ActivatePackItem(packItems.FindIndex(p=>p.id=="pack-notebook"));yield return null;
        SLNeed(journal&&!backpack,"the notebook did not open the dossier");
        var people=Dossier();slResult.dossierTestimony=people.Any(p=>p.name=="送奶少年"&&p.body.Contains("问  "))&&people.Any(p=>p.name=="售票员");
        SLNeed(slResult.dossierTestimony,"dossier missing testimony: "+string.Join(",",people.Select(p=>p.name)));
        yield return new WaitForSeconds(.3f);yield return SLCapture("14-dossier");
        journal=false;CursorMode();
    }
}
