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
        player.Aim(tower.LocalToWorld(new Vector3(1.15f,17.2f,1.2f)));
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
        yield return new WaitForSeconds(.7f);
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<ClockTowerInteriorLayout>();
        NeedTower(tower&&layout&&layout.staircaseCollider&&layout.platformCollider,"Missing shared tower/physical stairs");
        NeedTower(layout.middlePlatformCollider,"Middle storey needs a real floor");
        var atmosphere=CheckTowerSurfaceGradient(tower);
        int scenes=SceneManager.sceneCount,instanceId=tower.GetInstanceID();
        NeedTower(FindObjectsByType<ClockDial>(FindObjectsSortMode.None).All(d=>d.streetClock),"Another accurate world clock bypasses Q pocket watch");
        NeedTower(tower.GetComponentsInChildren<ClockDial>().Length==4,"Tower needs four synchronized clock faces");
        yield return WalkTo(new Vector3(0,.1f,-9f));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.ExteriorHeight*.47f,0)));yield return Capture("tower-exterior-front");
        yield return WalkTo(new Vector3(5,.1f,-7));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.ExteriorHeight*.47f,0)));yield return Capture("tower-exterior-corner");
        yield return WalkTo(new Vector3(0,.1f,-1.5f));
        player.Aim(tower.LocalToWorld(new Vector3(0,T.DialHeight,-1.86f)));
        ToggleWatch();yield return new WaitForSeconds(.4f);yield return Capture("tower-watch-comparison");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        var dials=tower.GetComponentsInChildren<ClockDial>();var handsBefore=dials.Select(d=>d.minuteHand.localRotation).ToArray();
        float before=Minutes;yield return new WaitForSeconds(7);yield return Capture("tower-watch-comparison-later");
        yield return new WaitForEndOfFrame();RequireSharedDialHands();
        for(int i=0;i<dials.Length;i++)NeedTower(Quaternion.Angle(handsBefore[i],dials[i].minuteHand.localRotation)>55,"A physical tower minute hand stopped moving");
        NeedTower(Mathf.Abs(Mathf.Repeat(Minutes-before,1440)-10)<.8f,"Q view does not continue advancing at the configured ratio");
        ToggleWatch();
        yield return WalkTo(new Vector3(0,.1f,6.8f));
        var plaque=tower.GetComponentsInChildren<Interactable>().Single(i=>i.evidenceId=="E06");
        player.Aim(plaque.transform.position);yield return null;
        NeedTower(TryInteract()&&inspected&&inspected.evidenceId=="E06","Wall plaque cannot be read with ordinary interaction ray");
        yield return Capture("tower-wall-history");CloseInspect();
        var door=Mechanism("clocktower-door");
        if(door.IsOpen){door.Use();yield return new WaitForSeconds(.9f);}
        player.scriptedDirection=Vector3.forward;yield return new WaitForSeconds(.75f);player.scriptedDirection=Vector3.zero;
        NeedTower(player.transform.position.z<8.0f,"Closed tower door does not block player");
        // Back away from the outward-swinging leaf before opening it.
        yield return WalkTo(new Vector3(0,.1f,6.8f));player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
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
        Save();NeedTower(state.y>T.MachineFloor-.2f&&state.openFixtures.Contains("clocktower-door"),"Upper-floor save/door state was not preserved");
        File.WriteAllText(Path.Combine(captures,"clocktower-results.json"),JsonUtility.ToJson(new TowerResult{passed=true,referenceExterior=tower.referenceExteriorImported,
            towerHeight=T.ExteriorHeight,middleFloor=T.MiddleFloor,machineFloor=T.MachineFloor,realStairWaypoints=path.Length,footprints=layout.footprintCount,continuousTown=true,closedDoorBlocks=true,fourMovingDials=true,upperFloorSave=true,
            atmosphere=atmosphere,
            verification="Actual CharacterController walked from town through the door up every spiral-stair waypoint. Lower/middle/upper close-ups retain the same stair azimuth and ordinary eye height; material-gradient checks supplement visual inspection. Live dust is sampled twice without forcing emission or simulation. Controlled review, not physical keyboard input or blind playtest. Exterior is locally reconstructed from the approved illustration, following the milk-shop workflow."},true));
        Debug.Log("CLOCKTOWER_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyClockTowerSave() {
        yield return new WaitForSeconds(.8f);
        var tower=FindFirstObjectByType<ClockTowerInstance>();var layout=tower.GetComponent<ClockTowerInteriorLayout>();var door=Mechanism("clocktower-door");
        NeedTower(CurrentScene=="Town"&&player.transform.position.y>T.MachineFloor-.2f&&SafePosition(player.transform.position),"Independent process failed to restore machine-floor save");
        NeedTower(state.entries.Any(e=>e.id=="E06")&&door.IsOpen,"Tower evidence/door state not restored");
        player.Aim(tower.LocalToWorld(layout.mechanismLookPoint));yield return Capture("tower-upper-floor-reloaded");
        var path=layout.WorldAscendingPath();
        for(int i=path.Length-1;i>=0;i--)yield return TowerWalk(path[i]);
        yield return WalkTo(new Vector3(0,.1f,6.8f));
        player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
        NeedTower(TryInteract()&&!door.IsOpen,"Door cannot close after descending and leaving tower");yield return new WaitForSeconds(.9f);
        NeedTower(player.transform.position.y<.3f,"Actual descending route failed");
        NeedTower(!SafePosition(new Vector3(5,T.MachineFloor,10))&&!SafePosition(new Vector3(0,T.ExteriorHeight+1,10)),"Upper-floor exception weakened town fall/height recovery");
        yield return Capture("tower-exit-after-descent");
        File.WriteAllText(Path.Combine(captures,"clocktower-save-relaunch.txt"),"PASS: separate process restored upper-floor position/E06/door; actual controller descended every stair and left the building; door closed from outside.\n");
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
        public float towerHeight,middleFloor,machineFloor;
        public TowerAtmosphereResult atmosphere;
        public string verification;
    }
    [Serializable] class TowerAtmosphereResult {
        public bool gradedDustSurfaces,upperCobwebs,liveDriftingDust;
        public int dustBands,particlesBefore,particlesAfter,movingParticles;
        public float lowerPrintContrast,upperPrintContrast,lowerPrintCutoff,upperPrintCutoff;
    }
}
