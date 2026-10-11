using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using T = ClockTowerInteriorLayout;

public partial class DemoApp {
    void NeedTower(bool condition,string message) {
        if(condition)return;
        Debug.LogError("CLOCKTOWER_REVIEW_FAIL: "+message);Application.Quit(1);throw new InvalidOperationException(message);
    }
    IEnumerator TowerWalk(Vector3 target) {
        float until=Time.time+6;
        while(Time.time<until) {
            Vector3 delta=target-player.transform.position;delta.y=0;
            if(delta.magnitude<.045f)break;
            player.scriptedDirection=delta.normalized*Mathf.Clamp(delta.magnitude*2,.12f,.42f);
            yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        NeedTower(Time.time<until,"Controller blocked on stair at "+player.transform.position+" target "+target);
        yield return new WaitForSeconds(.07f);
        NeedTower(Mathf.Abs(player.transform.position.y-target.y)<.25f,"Stair height mismatch/fall: "+player.transform.position+" target "+target);
    }
    IEnumerator CaptureTowerDustSection(ClockTowerInstance tower,int segment,string level) {
        // These three stops share the same azimuth, forty real stair segments
        // apart. Keep the ordinary eye height and only turn the player's view.
        var expected=T.Polar(T.StartAngle+(segment+.5f)*T.StepAngle,T.WalkingRadius,T.StepTop(segment));
        NeedTower(Vector3.Distance(player.transform.position,tower.LocalToWorld(expected))<.25f,
            "Dust close-up was not reached by the real stair route: "+level);
        var target=T.Polar(T.StartAngle+(segment+2.5f)*T.StepAngle,1.0f,T.StepTop(segment+2)+.03f);
        player.Aim(tower.LocalToWorld(target));
        yield return Capture("tower-dust-gradient-"+level);
    }
    TowerAtmosphereResult CheckTowerSurfaceGradient(ClockTowerInstance tower) {
        var renderers=tower.GetComponentsInChildren<MeshRenderer>();
        var dust=renderers.Where(r=>r.name.StartsWith("Stair dust band ",StringComparison.Ordinal)).OrderBy(r=>r.name).ToArray();
        var prints=renderers.Where(r=>r.name.StartsWith("Shoe impressions band ",StringComparison.Ordinal)).OrderBy(r=>r.name).ToArray();
        NeedTower(dust.Length>=3&&dust.Length==prints.Length,"Missing lower/middle/upper dust and shoe-impression surfaces");
        float previousContrast=-1,previousCutoff=1,firstContrast=0,firstCutoff=0;
        for(int i=0;i<dust.Length;i++) {
            var floor=dust[i].sharedMaterial;var print=prints[i].sharedMaterial;
            NeedTower(floor&&print&&floor.HasProperty("_BaseColor")&&print.HasProperty("_BaseColor")&&print.HasProperty("_Cutoff"),
                "Dust trail needs actual graded surface and alpha-cutout materials");
            float contrast=floor.GetColor("_BaseColor").grayscale-print.GetColor("_BaseColor").grayscale;
            float cutoff=print.GetFloat("_Cutoff");
            NeedTower(contrast>=previousContrast-.01f&&cutoff<=previousCutoff+.001f,"Dust trail clarity reverses before reaching the upper floor");
            if(i==0){firstContrast=contrast;firstCutoff=cutoff;}
            previousContrast=contrast;previousCutoff=cutoff;
        }
        NeedTower(previousContrast>firstContrast+.08f&&previousCutoff<firstCutoff-.1f,
            "Upper footprints are not clearer than the worn lower impressions");
        NeedTower(renderers.Any(r=>r.sharedMaterials.Any(m=>m&&m.name.StartsWith("Tower inner limestone bricks",StringComparison.Ordinal)&&m.GetTexture("_BaseMap"))),
            "Interior walls have no textured limestone-brick material");
        return new TowerAtmosphereResult {gradedDustSurfaces=true,dustBands=dust.Length,
            lowerPrintContrast=firstContrast,upperPrintContrast=previousContrast,
            lowerPrintCutoff=firstCutoff,upperPrintCutoff=previousCutoff};
    }
    int CheckTowerDustParticles(ClockTowerInstance tower,ParticleSystem dust,ParticleSystem.Particle[] particles) {
        int count=dust.GetParticles(particles);
        NeedTower(count>0&&count<=40,"Upper room needs a small number of live dust particles");
        for(int i=0;i<count;i++) {
            var local=tower.transform.InverseTransformPoint(dust.transform.TransformPoint(particles[i].position));
            NeedTower(local.y>T.MiddleFloor+1&&local.y<T.InnerWallHeight+.2f,
                "Drifting dust escaped the upper tower volume");
        }
        return count;
    }
    IEnumerator ReviewTowerAtmosphere(ClockTowerInstance tower,TowerAtmosphereResult result) {
        var transforms=tower.GetComponentsInChildren<Transform>();
        var webs=transforms.SingleOrDefault(t=>t.name=="Upper tower cobwebs");
        NeedTower(webs&&webs.GetComponentsInChildren<Renderer>().Length>0,"Upper corners have no visible cobweb geometry");
        NeedTower(webs.GetComponentsInChildren<Collider>().Length==0,"Decorative cobwebs must not obstruct the walking route");
        foreach(var web in webs.GetComponentsInChildren<Renderer>())
            NeedTower(tower.transform.InverseTransformPoint(web.bounds.min).y>T.MiddleFloor+1,"Cobwebs spread into the lower entrance");
        var dust=tower.GetComponentsInChildren<ParticleSystem>().SingleOrDefault(p=>p.name=="Upper room drifting dust");
        NeedTower(dust,"Missing upper-room drifting dust system");
        NeedTower(dust.main.maxParticles>0&&dust.main.maxParticles<=40&&dust.main.simulationSpace==ParticleSystemSimulationSpace.Local,
            "Atmospheric dust must stay sparse and local to the tower");
        NeedTower(dust.GetComponent<ParticleSystemRenderer>()&&dust.GetComponent<ParticleSystemRenderer>().enabled,"Upper dust renderer is disabled");
        // A genuine view from the reached machine floor, not a detached camera.
        player.Aim(tower.LocalToWorld(new Vector3(T.ChamberInteriorWidth/2-.2f,17.2f,T.ChamberInteriorWidth/2-.25f)));
        yield return new WaitForSeconds(.4f);
        yield return Capture("tower-upper-corner-cobwebs");
        var before=new ParticleSystem.Particle[dust.main.maxParticles];
        var after=new ParticleSystem.Particle[dust.main.maxParticles];
        int firstCount=CheckTowerDustParticles(tower,dust,before);
        yield return new WaitForSeconds(1);
        int secondCount=CheckTowerDustParticles(tower,dust,after),moved=0;
        for(int i=0;i<firstCount;i++)for(int j=0;j<secondCount;j++)
            if(before[i].randomSeed==after[j].randomSeed&&after[j].remainingLifetime<before[i].remainingLifetime&&
                (after[j].position-before[i].position).sqrMagnitude>.00000001f){moved++;break;}
        NeedTower(moved>0,"Live dust is not actually drifting over time");
        yield return Capture("tower-upper-corner-dust-later");
        result.upperCobwebs=true;result.liveDriftingDust=true;result.particlesBefore=firstCount;result.particlesAfter=secondCount;result.movingParticles=moved;
    }
    IEnumerator ReviewClockTower() {
        if(Review&&Array.IndexOf(Environment.GetCommandLineArgs(),"--night-illumination")>=0) {
            gameClock=new GameClock(GameClock.WrapMinutes(-GameClock.InitialMinutes));
            DayNightCycle.Instance.Apply(Minutes);
        }
        yield return new WaitForSeconds(.7f);
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<ClockTowerInteriorLayout>();
        NeedTower(tower&&layout&&layout.staircaseCollider&&layout.platformCollider,"Missing shared tower/physical stairs");
        NeedTower(layout.middlePlatformCollider,"Middle storey needs a real floor");
        var atmosphere=CheckTowerSurfaceGradient(tower);
        int scenes=SceneManager.sceneCount,instanceId=tower.GetInstanceID();
        // The station's barrier and facade clocks (RAIL-01/06) deliberately read the accurate
        // shared clock; any other accurate dial in town would bypass the Q pocket watch.
        static bool InStation(Transform t){var b=t.GetComponentInParent<TownMapBuilding>();return b&&b.mapId=="B17";}
        NeedTower(FindObjectsByType<ClockDial>(FindObjectsSortMode.None).All(d=>d.streetClock||InStation(d.transform)),"Another accurate world clock bypasses Q pocket watch");
        NeedTower(tower.GetComponentsInChildren<ClockDial>().Length==4,"Tower needs four synchronized clock faces");
        yield return WalkTo(new Vector3(0,.1f,-19f));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.ExteriorHeight*.47f,0)));yield return Capture("tower-exterior-front");
        yield return WalkTo(new Vector3(5,.1f,-17));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.ExteriorHeight*.47f,0)));yield return Capture("tower-exterior-corner");
        yield return WalkTo(new Vector3(0,.1f,-11.5f));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.DialHeight,-T.DialPlaneDistance)));
        ToggleWatch();yield return new WaitForSeconds(.4f);yield return Capture("tower-watch-comparison");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        var dials=tower.GetComponentsInChildren<ClockDial>();var handsBefore=dials.Select(d=>d.minuteHand.localRotation).ToArray();
        float before=Minutes;double compareReal=Time.unscaledTimeAsDouble;yield return new WaitForSeconds(7);yield return Capture("tower-watch-comparison-later");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        for(int i=0;i<dials.Length;i++)NeedTower(Quaternion.Angle(handsBefore[i],dials[i].minuteHand.localRotation )>2.5f,"A physical tower minute hand stopped moving");
        NeedTower(Math.Abs(Mathf.Repeat(Minutes-before,1440)-(Time.unscaledTimeAsDouble-compareReal)*GameClock.GameMinutesPerRealSecond)<.01,"Q view does not continue advancing at the configured ratio");
        ToggleWatch();
        yield return WalkTo(new Vector3(0,.1f,-3.2f));
        var plaque=tower.GetComponentsInChildren<Interactable>().Single(i=>i.evidenceId=="E06");
        player.Aim(plaque.transform.position);yield return null;
        NeedTower(TryInteract()&&inspected&&inspected.evidenceId=="E06","Wall plaque cannot be read with ordinary interaction ray");
        yield return Capture("tower-wall-history");CloseInspect();
        var door=Mechanism("clocktower-door");
        if(door.IsOpen){door.Use();yield return new WaitForSeconds(.9f);}
        player.scriptedDirection=Vector3.forward;yield return new WaitForSeconds(.75f);player.scriptedDirection=Vector3.zero;
        NeedTower(tower.transform.InverseTransformPoint(player.transform.position).z<-2.0f,"Closed tower door does not block player");
        // Back away from the outward-swinging leaf before opening it.
        yield return WalkTo(new Vector3(0,.1f,-3.2f));player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
        NeedTower(TryInteract()&&door.IsOpen&&!door.LastUseBlocked,"Tower door ray or safe outward swing failed");
        yield return new WaitForSeconds(.9f);yield return Capture("tower-door-open");
        var path=layout.WorldAscendingPath();
        NeedTower(path.Length==101,"Tower review must retain the full 101-point walking route");
        for(int i=0;i<path.Length;i++) {
            yield return TowerWalk(path[i]);
            if(i==0) {
                door.Use();NeedTower(door.IsOpen&&door.LastUseBlocked,"Door close must be rejected while player occupies the swing");
            }
            if(i==1) {
                player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
                NeedTower(TryInteract()&&!door.IsOpen,"Tower door cannot close from inside");yield return new WaitForSeconds(.9f);
                player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
                NeedTower(TryInteract()&&door.IsOpen,"Tower door cannot reopen from inside");yield return new WaitForSeconds(.9f);
                player.Aim(tower.LocalToWorld(new Vector3(1.02f,T.GroundFloor+.12f,-.56f)));
                yield return Capture("tower-ground-stone-and-wall");
            }
            if(i==layout.middleLandingPathIndex) {
                NeedTower(Mathf.Abs(player.transform.position.y-T.MiddleFloor)<.15f,"Middle landing has wrong height");
                player.Aim(tower.LocalToWorld(layout.middleLookPoint));yield return Capture("tower-middle-storey");
                player.Aim(tower.LocalToWorld(new Vector3(-.90f,T.MiddleFloor+.03f,1.08f)));yield return Capture("tower-middle-stone-floor");
                yield return new WaitForSeconds(.5f);
                NeedTower(Mathf.Abs(player.transform.position.y-T.MiddleFloor)<.15f,"Player cannot stand on middle floor");
            }
            if(i==2){player.Aim(tower.LocalToWorld(layout.footprintLookPoint));yield return Capture("tower-dust-and-footprints");}
            if(i==12||i==24||i==80) {
                var local=tower.transform.InverseTransformPoint(player.transform.position);float heightBefore=player.transform.position.y;
                var outward=tower.transform.TransformDirection(new Vector3(local.x,0,local.z).normalized);
                player.scriptedDirection=outward*.5f;yield return new WaitForSeconds(.7f);player.scriptedDirection=Vector3.zero;
                var stopped=tower.transform.InverseTransformPoint(player.transform.position);
                NeedTower(new Vector2(stopped.x,stopped.z).magnitude<1.19f&&Mathf.Abs(player.transform.position.y-heightBefore)<.25f,"Player crossed outer stair guard or fell at corner");
                yield return TowerWalk(path[i]);
            }
            if(i==12||i==24||i==80){player.Aim(path[Mathf.Min(i+3,path.Length-1)]+Vector3.up*.6f);yield return Capture("tower-stair-climb-"+i);}
            // Below the middle detour, path index = segment + 3; above it,
            // the two actual landing waypoints also count toward the route.
            if(i==10)yield return CaptureTowerDustSection(tower,7,"lower");
            if(i==50)yield return CaptureTowerDustSection(tower,47,"middle");
            if(i==92)yield return CaptureTowerDustSection(tower,87,"upper");
        }
        NeedTower(player.transform.position.y>T.MachineFloor-.2f&&SafePosition(player.transform.position),"Upper machine floor is rejected as an invalid save position");
        NeedTower(instanceId==tower.GetInstanceID()&&SceneManager.sceneCount==scenes&&fade==0&&CurrentScene=="Town","Tower entry used a fade, teleport or scene replacement");
        player.Aim(tower.LocalToWorld(layout.mechanismLookPoint));yield return Capture("tower-machine-room");
        NeedTower(layout.footprintCount==87,"The existing 87-footprint trail must retain its count while clarity changes");
        var moving=layout.machinery.GetComponentsInChildren<ClockTowerGearMotion>();
        NeedTower(moving.Length>0,"Mechanism has no moving gear shafts");
        var rotation=moving[0].gears[0].localRotation;yield return new WaitForSeconds(1);yield return Capture("tower-gears-later");
        NeedTower(Quaternion.Angle(rotation,moving[0].gears[0].localRotation)>.1f,"Gear mechanism does not visibly turn");
        player.Aim(tower.LocalToWorld(new Vector3(-.73f,T.MachineFloor+.03f,.68f)));yield return Capture("tower-upper-stone-floor");
        yield return ReviewTowerAtmosphere(tower,atmosphere);
        yield return ReviewClockGlazing(tower);
        yield return ReviewTowerAdjustments(tower,layout);
        // Walk into the projecting part of the new physical chamber and back.
        foreach(var point in new[]{new Vector3(-1.05f,T.MachineFloor,.60f),new Vector3(-1.20f,T.MachineFloor,1.12f),new Vector3(-1.70f,T.MachineFloor,1.12f)})
            yield return TowerWalk(tower.LocalToWorld(point));
        NeedTower(SafePosition(player.transform.position),"Wider upper floor is not valid for saves");
        player.Aim(tower.LocalToWorld(layout.mechanismLookPoint));yield return Capture("tower-expanded-chamber");
        player.Aim(tower.LocalToWorld(new Vector3(-T.DialPlaneDistance,16.1f,0)));yield return Capture("tower-glass-oblique");
        yield return TowerWalk(tower.LocalToWorld(new Vector3(-1.20f,T.MachineFloor,1.12f)));
        yield return TowerWalk(tower.LocalToWorld(new Vector3(-1.05f,T.MachineFloor,.60f)));
        yield return TowerWalk(tower.LocalToWorld(layout.machineStandingPoint));
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--record-tower-surfaces")>=0)yield return ReviewTowerSurfaceMotion(tower);
        Save();NeedTower(state.y>T.MachineFloor-.2f&&state.openFixtures.Contains("clocktower-door"),"Upper-floor save/door state was not preserved");
        File.WriteAllText(Path.Combine(captures,"clocktower-results.json"),JsonUtility.ToJson(new TowerResult{passed=true,referenceExterior=tower.referenceExteriorImported,
            towerHeight=T.ExteriorHeight,middleFloor=T.MiddleFloor,machineFloor=T.MachineFloor,realStairWaypoints=path.Length,footprints=layout.footprintCount,continuousTown=true,closedDoorBlocks=true,fourMovingDials=true,upperFloorSave=true,
            atmosphere=atmosphere,liveFrostedApertures=4,windingLocksControls=true,sharedInteriorExteriorHands=true,adjustmentControls=5,adjustedOffsetSaved=TowerClockOffsetMinutes,dialDiameter=T.DialDiameter,chamberInteriorWidth=T.ChamberInteriorWidth,
            verification="Actual CharacterController walked from town through the door up every spiral-stair waypoint. Lower/middle/upper close-ups retain the same stair azimuth and ordinary eye height; material-gradient checks supplement visual inspection. Live dust is sampled twice without forcing emission or simulation. Controlled review, not physical keyboard input or blind playtest. Exterior is locally reconstructed from the approved illustration, following the milk-shop workflow."},true));
        Debug.Log("CLOCKTOWER_REVIEW_PASS");Application.Quit();
    }
    IEnumerator ReviewWinding(ClockTowerInstance tower,bool extended=false) {
        NeedTower(TowerAdjustmentBusy,"Adjustment did not start winding");
        var motion=tower.GetComponentInChildren<ClockTowerGearMotion>();
        double travel=motion.PrimaryTravelDegrees,target=state.towerAdjustmentMinutes;
        for(int key=0;key<5;key++)ApplyTowerAdjustment(key);
        NeedTower(state.towerAdjustmentMinutes==target,"Another control bypassed winding lock");
        if(extended) {
            CloseInspect();player.Aim(tower.LocalToWorld(tower.GetComponent<ClockTowerInteriorLayout>().mechanismLookPoint));
            NeedTower(TryInteract()&&TowerAdjustmentOpen,"Cannot reopen winding panel");
            ApplyTowerAdjustment(4);NeedTower(state.towerAdjustmentMinutes==target,"Reopening bypassed winding lock");
            paused=true;float frozen=towerWindingAge;double angle=motion.PrimaryTravelDegrees;
            yield return new WaitForSecondsRealtime(.2f);
            NeedTower(towerWindingAge==frozen&&motion.PrimaryTravelDegrees==angle,"Pause did not freeze winding");paused=false;
        }
        float deadline=Time.realtimeSinceStartup+5;bool captured=false;double peak=0;
        while(TowerAdjustmentBusy&&Time.realtimeSinceStartup<deadline) {
            peak=Math.Max(peak,Math.Abs(TowerWindingSpeedFactor));
            yield return new WaitForEndOfFrame();RequireSharedDialHands();
            if(extended&&!captured&&towerWindingAge>.6f){captured=true;yield return Capture("tower-adjustment-winding");}
        }
        NeedTower(!TowerAdjustmentBusy&&TowerWindingSpeedFactor==1,"Winding never returned to normal speed");
        NeedTower(peak>100&&motion.PrimaryTravelDegrees-travel>720,"Gears did not accelerate through several turns");
        NeedTower(TowerClockOffsetMinutes==GameClock.TowerOffsetMinutes+state.towerAdjustmentMinutes,"Hands did not reach the requested time");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
    }
    IEnumerator ReviewClockGlazing(ClockTowerInstance tower) {
        var windows=tower.GetComponentsInChildren<ClockTowerDialWindow>();
        NeedTower(windows.Length==4,"Four physical glazed clock openings required");
        foreach(var window in windows) {
            NeedTower(window.Dial&&window.Dial.hourHand&&window.Dial.minuteHand&&window.safety,
                "Interior and exterior must share physical clock hands and safe glazing");
            NeedTower(window.glazing.sharedMaterial.shader.name=="WrongDate/FrostedClockGlass"&&window.glazing.sharedMaterial.shader.isSupported,
                "Live scene-sampling glass shader is unavailable");
        }
        player.Aim(tower.LocalToWorld(new Vector3(-T.DialPlaneDistance,T.DialHeight,0)));
        yield return Capture("tower-interior-frosted-clock");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        Debug.Log("CLOCKTOWER_GLAZING_PASS: four real openings, live scene glass, shared physical hands");
    }
    IEnumerator ReviewTowerAdjustments(ClockTowerInstance tower,T layout) {
        player.Aim(tower.LocalToWorld(layout.mechanismLookPoint));yield return null;
        int evidenceCount=state.entries.Count;
        NeedTower(TryInteract()&&inspected is ClockTowerAdjustment,"Ordinary E ray cannot reach the mechanical adjustment");
        NeedTower(Blocked&&!GameTimePaused,"Adjustment UI must suspend movement/look while watch time continues");
        yield return Capture("tower-adjustment-controls");
        double watchBefore=ElapsedGameMinutes;
        int[] expected={-60,-90,-60,0,0};
        for(int i=0;i<5;i++) {
            ApplyTowerAdjustment(i);yield return ReviewWinding(tower,i==0);
            NeedTower(state.towerAdjustmentMinutes==expected[i],"Wrong relative adjustment for control "+i);
            NeedTower(ElapsedGameMinutes>watchBefore&&state.entries.Count==evidenceCount,"Watch must continue naturally without adjustment granting evidence");
        }
        // Boundary fixtures exercise real controls on either side of a day wrap.
        state.towerAdjustmentMinutes=1410;ApplyTowerAdjustment(3);yield return ReviewWinding(tower);
        NeedTower(state.towerAdjustmentMinutes==30,"Positive day wrap failed");
        state.towerAdjustmentMinutes=-1410;ApplyTowerAdjustment(0);yield return ReviewWinding(tower);
        NeedTower(state.towerAdjustmentMinutes==-30,"Negative day wrap failed");
        ApplyTowerAdjustment(4);yield return ReviewWinding(tower);
        NeedTower(TowerClockOffsetMinutes==30,"Reset failed after positive/negative wraps");
        ApplyTowerAdjustment(3);yield return ReviewWinding(tower);
        NeedTower(TowerClockOffsetMinutes==90,"One +1-hour turn must add to the original offset");
        var disk=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
        NeedTower(disk.towerAdjustmentMinutes==60&&disk.elapsedGameMinutes>watchBefore&&disk.elapsedGameMinutes<=ElapsedGameMinutes,"Adjustment did not persist independently of the advancing watch: offset="+disk.towerAdjustmentMinutes+" elapsed="+disk.elapsedGameMinutes.ToString("R"));
        yield return Capture("tower-adjustment-applied");CloseInspect();
        NeedTower(!Blocked,"Closing adjustment UI failed to restore walking");
        Debug.Log("CLOCKTOWER_ADJUSTMENT_PASS: five controls, three-second multi-turn acceleration/deceleration, locks, pause/reopen, day wrap, advancing watch/no new evidence, shared hands and disk write");
    }
    IEnumerator VerifyClockTowerSave() {
        yield return new WaitForSeconds(.8f);
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<ClockTowerInteriorLayout>();var door=Mechanism("clocktower-door");
        NeedTower(CurrentScene=="Town"&&player.transform.position.y>T.MachineFloor-.2f&&SafePosition(player.transform.position),"Independent process failed to restore machine-floor save");
        NeedTower(state.entries.Any(e=>e.id=="E06")&&door.IsOpen,"Tower evidence/door state not restored");
        NeedTower(state.towerAdjustmentMinutes==60&&TowerClockOffsetMinutes==90,"Adjusted tower offset did not persist across process relaunch");
        player.Aim(tower.LocalToWorld(layout.mechanismLookPoint));yield return Capture("tower-upper-floor-reloaded");
        NeedTower(TryInteract()&&inspected is ClockTowerAdjustment,"Reloaded machine cannot be interacted with");
        double watchBeforeReset=ElapsedGameMinutes,realBeforeReset=Time.unscaledTimeAsDouble;
        ApplyTowerAdjustment(4);yield return ReviewWinding(tower);
        double watchAdvance=ElapsedGameMinutes-watchBeforeReset;
        NeedTower(TowerClockOffsetMinutes==30&&watchAdvance>3*GameClock.GameMinutesPerRealSecond*.9&&Math.Abs(watchAdvance-(Time.unscaledTimeAsDouble-realBeforeReset)*GameClock.GameMinutesPerRealSecond)<.1,
            "Reset must restore watch +30 while the watch advances at its ordinary rate");
        yield return Capture("tower-reset-after-reload");CloseInspect();
        var path=layout.WorldAscendingPath();
        for(int i=path.Length-1;i>=0;i--)yield return TowerWalk(path[i]);
        yield return WalkTo(new Vector3(0,.1f,-3.2f));
        player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
        NeedTower(TryInteract()&&!door.IsOpen,"Door cannot close after descending and leaving tower");yield return new WaitForSeconds(.9f);
        NeedTower(player.transform.position.y<.3f,"Actual descending route failed");
        NeedTower(!SafePosition(new Vector3(5,T.MachineFloor,10))&&!SafePosition(new Vector3(0,T.ExteriorHeight+1,10)),"Upper-floor exception weakened town fall/height recovery");
        yield return Capture("tower-exit-after-descent");
        yield return WalkTo(new Vector3(0,.1f,-11.5f));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.DialHeight,-T.DialPlaneDistance)));
        ToggleWatch();yield return Capture("tower-reset-ground-comparison");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        NeedTower(TowerClockOffsetMinutes==30,"Reset clock drifted after descent");
        File.WriteAllText(Path.Combine(captures,"clocktower-save-relaunch.txt"),"PASS: separate process restored upper-floor position/E06/door/adjusted +90-minute offset; ordinary E interaction reset all four dials to watch +30 while the watch advanced at its ordinary rate; actual controller descended every stair and left the building; door closed from outside.\n");
        Debug.Log("CLOCKTOWER_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    IEnumerator VerifyClockTowerMigration() {
        NeedTower(towerSaveMigrated&&state.towerLayoutRevision==1,"Old tower save was not migrated");
        NeedTower(state.entries.Any(e=>e.id=="E06")&&state.openFixtures.Contains("clocktower-door")&&state.milkboyGreeted,"Tower migration lost evidence, door or dialogue progress");
        NeedTower(Math.Abs(clockLoadedElapsedMinutes-123.5)<1e-9,"Migration lost saved clock");
        yield return new WaitForSeconds(.6f);
        NeedTower(Vector3.Distance(player.transform.position,towerMigratedPosition)<.15f,"Migrated player fell or spawned inside obstruction");
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<T>();
        if(player.transform.position.y>T.MachineFloor-.2f) {
            yield return TowerWalk(tower.LocalToWorld(new Vector3(-.65f,T.MachineFloor,.25f)));
            yield return TowerWalk(tower.LocalToWorld(layout.machineStandingPoint));
        } else {
            var path=layout.WorldAscendingPath();
            int index=Array.FindIndex(path,p=>Vector3.Distance(p,towerMigratedPosition)<.02f);
            NeedTower(index>=0&&index+1<path.Length,"Migrated stair is not on the shared walking route");
            yield return TowerWalk(path[index+1]);
        }
        Save();NeedTower(state.towerLayoutRevision==1,"Migrated revision was not saved");
        player.Aim(tower.LocalToWorld(player.transform.position.y>T.MachineFloor-.2f?layout.mechanismLookPoint:layout.footprintLookPoint));
        yield return Capture("tower-legacy-position-migrated");
        File.WriteAllText(Path.Combine(captures,"tower-migration.txt"),"PASS: legacy stair/machine-floor position mapped to verified new landing, real controller stood and moved; clock/evidence/door/dialogue preserved.\n");
        Debug.Log("CLOCKTOWER_MIGRATION_PASS");Application.Quit();
    }
    IEnumerator VerifyClockTowerMiddleSave() {
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<T>();
        NeedTower(!towerSaveMigrated&&state.towerLayoutRevision==1,"Current-layout save must not migrate again");
        yield return new WaitForSeconds(.6f);
        NeedTower(Vector3.Distance(player.transform.position,tower.LocalToWorld(layout.middleStandingPoint))<.15f,"Current middle-floor save did not stay on its floor");
        NeedTower(state.entries.Any(e=>e.id=="E06")&&Math.Abs(clockLoadedElapsedMinutes-123.5)<1e-9,"Middle-floor save lost progress or time");
        var path=layout.WorldAscendingPath();
        yield return TowerWalk(path[layout.middleLandingPathIndex+1]);
        yield return TowerWalk(path[layout.middleLandingPathIndex+2]);
        player.Aim(tower.LocalToWorld(layout.middleLookPoint));yield return Capture("tower-middle-floor-reloaded");
        Debug.Log("CLOCKTOWER_MIDDLE_SAVE_PASS");Application.Quit();
    }
    [Serializable] class TowerResult {
        public bool passed,referenceExterior,continuousTown,closedDoorBlocks,fourMovingDials,upperFloorSave;
        public int realStairWaypoints,footprints;
        public float towerHeight,middleFloor,machineFloor,dialDiameter,chamberInteriorWidth;
        public int adjustmentControls,liveFrostedApertures;
        public bool windingLocksControls,sharedInteriorExteriorHands;
        public double adjustedOffsetSaved;
        public TowerAtmosphereResult atmosphere;
        public string verification;
    }
    [Serializable] class TowerAtmosphereResult {
        public bool gradedDustSurfaces,upperCobwebs,liveDriftingDust;
        public int dustBands,particlesBefore,particlesAfter,movingParticles;
        public float lowerPrintContrast,upperPrintContrast,lowerPrintCutoff,upperPrintCutoff;
    }
}
