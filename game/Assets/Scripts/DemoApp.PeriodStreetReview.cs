using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    IEnumerator ReviewPeriodStreets() {
        yield return new WaitForSeconds(.8f);
        var root=GameObject.Find("1930 municipal street details");var signal=FindFirstObjectByType<PeriodTrafficSignal>();
        NeedMap(root&&signal&&signal.lenses.Length==12,"Period street kit missing");
        NeedMap(root.GetComponentsInChildren<Collider>().Length==18,"Only 16 sign stems and 2 signal poles should collide");
        NeedMap(root.GetComponentsInChildren<Light>().Length==16&&signal.GetComponentsInChildren<Light>().Length==0,"Unexpected street light count");
        // At every instant no perpendicular greens can overlap, including both wrap boundaries.
        for(double seconds=-1;seconds<117;seconds+=.025)NeedMap(!(PeriodTrafficSignal.State(seconds,0)==2&&PeriodTrafficSignal.State(seconds,1)!=0)&&!(PeriodTrafficSignal.State(seconds,1)==2&&PeriodTrafficSignal.State(seconds,0)!=0),"Conflicting signal right of way");
        foreach(var road in TownPlan.Current.roads) {
            var a=new Vector3(road.points[0],0,road.points[1]);var b=new Vector3(road.points[2],0,road.points[3]);var dir=(b-a).normalized;var normal=new Vector3(-dir.z,0,dir.x);
            foreach(int side in road.lane>0?new[]{-1,1}:new[]{1})foreach(float edge in new[]{-.5f,0,.5f}) {
                float band=road.lane>0?road.lane/2+road.strip+road.walk/2+edge*(road.walk-.5f):edge*(road.walk-.5f);
                var start=a+dir*2+normal*side*band;var end=b-dir*2+normal*side*band;
                NeedMap(!Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.55f,.24f,dir,out var hit,Vector3.Distance(start,end)),"Street furniture blocks "+road.id+" by "+(hit.collider?hit.collider.name:"none"));
            }
        }
        var cycle=DayNightCycle.Instance;DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        var views=new (string name,Vector3 feet,Vector3 aim)[]{
            ("signal-junction",new Vector3(22,.1f,15),new Vector3(30,3.8f,20)),
            ("stop-approach",new Vector3(-117.5f,.1f,8.8f),new Vector3(-117.5f,1.65f,13.3f)),
            ("crossing-and-kerb",new Vector3(24,.1f,23),new Vector3(29,.08f,23.8f)),
            ("shop-footway",new Vector3(-40,.1f,25),new Vector3(-32,.1f,23.3f)),
            ("gutter-grate",new Vector3(-150,.1f,24),new Vector3(-151,.03f,22.84f)),
            ("residential-street",new Vector3(20,.1f,-71),new Vector3(31,.7f,-76)),
            ("main-street",new Vector3(-60,.1f,15),new Vector3(-25,4,24)),
        };
        foreach(var view in views){player.Place(view.feet,0);player.Aim(view.aim);yield return new WaitForSeconds(.5f);yield return Capture(view.name);}
        player.Place(views[0].feet,0);player.Aim(views[0].aim);
        // Reading-like world clock, followed by an explicit pause: signal follows only this clock.
        dayNightReviewFrozen=false;double first=signal.CycleSeconds;yield return new WaitForSeconds(1.2f);NeedMap(signal.CycleSeconds>first+.9,"Signal clock not advancing");
        paused=true;yield return null;double frozen=signal.CycleSeconds;yield return new WaitForSeconds(.4f);NeedMap(Math.Abs(signal.CycleSeconds-frozen)<.001,"Signal advances while paused");paused=false;
        // Render both directions and amber/clearance from exactly the same shared clock input.
        foreach(int sec in new[]{10,25,28,40,54,57}) {
            gameClock=new GameClock(sec*GameClock.GameMinutesPerRealSecond);dayNightReviewFrozen=true;cycle.Apply(Minutes);
            yield return new WaitForSeconds(.25f);
            for(int face=0;face<4;face++)for(int colour=0;colour<3;colour++) {
                var block=new MaterialPropertyBlock();signal.lenses[face*3+colour].GetPropertyBlock(block);
                bool lit=block.GetColor("_EmissionColor").maxColorComponent>1;
                NeedMap(lit==(PeriodTrafficSignal.State(sec,face%2)==colour),"Actual lens emission disagrees with shared clock");
            }
            yield return Capture("signal-phase-"+sec);
        }
        DayReviewTime(690);cycle.SetReviewWeather(TownWeather.Kind.Rain);yield return new WaitForSeconds(.6f);yield return Capture("junction-rain");
        DayReviewTime(5);cycle.SetReviewWeather(TownWeather.Kind.Overcast);yield return new WaitForSeconds(.6f);yield return Capture("junction-night");
        player.Place(views[1].feet,0);player.Aim(views[1].aim);
        // The nearest-light pool refreshes every .25s. Let a staged camera jump settle.
        yield return new WaitForSeconds(.7f);
        var signLamp=root.GetComponentsInChildren<Light>().OrderBy(l=>Vector3.Distance(l.transform.position,player.transform.position)).First();
        NeedMap(signLamp.enabled&&signLamp.intensity>1,"Nearby night STOP lamp not in active pool");
        Debug.Log("PERIOD_STOP_NIGHT_LIGHT: "+signLamp.intensity+" at "+signLamp.transform.position);
        yield return Capture("stop-night");
        DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();player.Place(new Vector3(22,.1f,25),0);player.Aim(new Vector3(35,1.4f,19));
        recorder.Begin(Path.Combine(captures,"junction-walk"));yield return WalkTo(new Vector3(38,.1f,25));recorder.End();
        // Physical road crossing, kerb ascent and descent, along an actual pedestrian route.
        player.Place(new Vector3(26,.1f,14),0);player.Aim(new Vector3(26,.2f,26));yield return WalkTo(new Vector3(26,.1f,25.3f));
        NeedMap(player.transform.position.y>.05f&&player.transform.position.y<.3f,"Footway landing height "+player.transform.position);
        File.WriteAllText(Path.Combine(captures,"street-results.json"),"{\"passed\":true,\"stopSigns\":16,\"signalJunctions\":1,\"colliders\":18,\"roadSweeps\":true,\"controllerCrossing\":true,\"clockAdvanceAndPause\":true,\"exclusiveSignalPhases\":true,\"verification\":\"Native Mac/Metal rendered views; staged camera/clock; actual CharacterController walking. No physical-keyboard or blind test.\"}");
        Debug.Log("PERIOD_STREET_REVIEW_PASS");Application.Quit();
    }
}
