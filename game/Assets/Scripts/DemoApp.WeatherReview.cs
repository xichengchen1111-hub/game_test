using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class WeatherSample {
        public string view,weather;public float minutes,coverage,rain,fog,directLight,roomLight,cloudTransmission;
        public int streaks;public bool covered;
    }
    [Serializable] class WeatherProof {
        public double elapsed;public int processId;public TownWeather.State weather;
        public Vector2 cloudOffset;public Vector3 position;
    }
    [Serializable] class WeatherResult {
        public bool passed,pause,transition,cloudMotion,shelter,daytimeStreetOff,rainRoomLight,shadowBudget,worldRainParallax;
        public List<WeatherSample> samples=new List<WeatherSample>();public string verification;
    }
    void NeedWeather(bool ok,string why) {
        if(ok)return;Debug.LogError("WEATHER_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);
    }
    void RainRoofCheck(WeatherEffects effects) {
        NeedWeather(effects.RainMesh.vertexCount==WeatherEffects.DropBudget*4,"Rain exceeded its fixed mesh budget");
        var positions=effects.RainMesh.vertices;var colors=effects.RainMesh.colors32;
        for(int i=0;i<positions.Length;i++)if(colors[i].a>0)
            NeedWeather(positions[i].y+.01f>=WeatherShelter.RoofHeight(positions[i]),"Rain penetrates a covered roof at "+positions[i]);
    }
    IEnumerator WeatherView(string view,TownWeather.Kind kind,double minute,Vector3 look,WeatherResult result) {
        DayReviewTime(minute);var cycle=DayNightCycle.Instance;cycle.SetReviewWeather(kind);
        player.Aim(look);yield return null;yield return new WaitForEndOfFrame();
        var effects=cycle.GetComponent<WeatherEffects>();var state=cycle.Weather;
        var sunlight=DayNightCycle.SunDirection(Minutes);
        NeedWeather(Vector3.Angle(cycle.celestialLight.transform.forward,cycle.MoonActive?sunlight:-sunlight)<.01f,"Sky disc and directional light disagree");
        if(kind==TownWeather.Kind.Rain)NeedWeather(cycle.CloudTransmission<.2f,"Rain cloud blanket failed to dim the celestial light");
        RainRoofCheck(effects);
        foreach(var lamp in DayNightLamp.Active)CheckEmission(lamp);
        if(cycle.Daylight==1)NeedWeather(DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Exterior&&l.source).All(l=>!l.source.enabled),"Weather enabled noon street lamps");
        result.samples.Add(new WeatherSample{view=view,weather=kind.ToString(),minutes=Minutes,coverage=state.coverage,rain=state.rain,
            fog=RenderSettings.fogDensity,directLight=cycle.celestialLight.intensity,cloudTransmission=cycle.CloudTransmission,streaks=effects.VisibleStreaks,covered=effects.ListenerSheltered,
            roomLight=DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Interior&&l.daylightAccess>.8f).Average(l=>l.Output)});
        yield return Capture(view+"-"+kind.ToString().ToLowerInvariant());
    }
    IEnumerator ReviewWeather() {
        dayNightReviewFrozen=true;yield return new WaitForSeconds(.4f);
        var cycle=DayNightCycle.Instance;var effects=cycle.GetComponent<WeatherEffects>();
        NeedWeather(cycle&&effects&&WeatherShelter.Count>=10,"Sky, weather or authored shelters missing");
        NeedWeather(FindObjectsByType<Light>(FindObjectsSortMode.None).Count(l=>l.shadows!=UnityEngine.LightShadows.None)==4,"Shadow baseline must retain upstream tower lamp and original shop lamps");
        var result=new WeatherResult{shadowBudget=true};
        // Full timetable sweep includes hold/fade boundaries and cumulative days.
        var before=TownWeather.Evaluate(0);
        for(double t=.25;t<210*24;t+=.25) {
            var after=TownWeather.Evaluate(t);
            NeedWeather(Mathf.Abs(after.coverage-before.coverage)<.004f&&Mathf.Abs(after.rain-before.rain)<.008f,"Weather timetable contains a discontinuity");
            NeedWeather(after.coverage>=.65f||after.rain==0,"Showers started before sufficient cloud cover");
            before=after;
        }
        NeedWeather(TownWeather.Evaluate(double.NaN).coverage==TownWeather.Evaluate(0).coverage,"Invalid weather time not recovered");
        player.Place(new Vector3(0,.1f,-7),0);
        var townLook=new Vector3(0,12,10);
        foreach(TownWeather.Kind kind in Enum.GetValues(typeof(TownWeather.Kind))) {
            yield return WeatherView("town-noon",kind,720,townLook,result);
            if(kind!=TownWeather.Kind.Rain)continue;
            // Freeze rain time, move the real controller: world columns must stay put.
            var start=player.transform.position;var vertices=effects.RainMesh.vertices;var colors=effects.RainMesh.colors32;
            player.scriptedDirection=Vector3.right*.20f;yield return new WaitForSeconds(.25f);player.scriptedDirection=Vector3.zero;
            yield return new WaitForEndOfFrame();var moved=effects.RainMesh.vertices;var movedColors=effects.RainMesh.colors32;
            int stable=0,compared=0;
            for(int d=0;d<WeatherEffects.DropBudget;d++) {
                int v=d*4;if(colors[v].a<8||movedColors[v].a<8)continue;
                var a=(vertices[v]+vertices[v+1])*.5f;var b=(moved[v]+moved[v+1])*.5f;compared++;
                if(new Vector2(a.x-b.x,a.z-b.z).sqrMagnitude<.000004f)stable++;
            }
            NeedWeather(Vector3.Distance(start,player.transform.position)>.08f&&compared>20&&stable>compared*.90f,"Rain slid with the camera instead of retaining world parallax");
            result.worldRainParallax=true;RainRoofCheck(effects);yield return WalkTo(start);
        }
        yield return WeatherView("town-sunrise",TownWeather.Kind.Clear,378,new Vector3(30,3,10),result);
        yield return WeatherView("town-sunset",TownWeather.Kind.Scattered,1060,new Vector3(-25,6,10),result);
        yield return WeatherView("town-night",TownWeather.Kind.Clear,0,player.eye.transform.position-DayNightCycle.SunDirection(0)*70,result);
        yield return WeatherView("town-night",TownWeather.Kind.Rain,0,townLook,result);
        result.daytimeStreetOff=true;
        // Actual unpaused clock, using the ordinary weather evaluator during a fade.
        cycle.SetReviewWeather(null);gameClock=new GameClock(3*TownWeather.SegmentMinutes+185);cycle.Apply(Minutes);
        player.Aim(townLook);yield return Capture("automatic-transition-before");
        var old=RenderSettings.skybox.GetVector("_CloudOffset");before=cycle.Weather;
        dayNightReviewFrozen=false;float until=Time.unscaledTime+2.5f;
        while(Time.unscaledTime<until) {
            yield return new WaitForEndOfFrame();var next=cycle.Weather;
            NeedWeather(Mathf.Abs(next.rain-before.rain)<.02f,"Live rain transition jumps");before=next;RainRoofCheck(effects);
        }
        dayNightReviewFrozen=true;
        NeedWeather((RenderSettings.skybox.GetVector("_CloudOffset")-old).sqrMagnitude>.0001f,"Clouds did not move with game time");
        result.transition=result.cloudMotion=true;yield return Capture("automatic-transition-after");
        // Walk into the unchanged milk shop; outdoor drops remain visible beyond its panes.
        var placement=milkShopRoot.GetComponent<MilkShopPlacement>();
        player.Place(placement.outside,0);yield return WalkTo(placement.outside);yield return WalkTo(placement.inside);
        Vector3 P(float x,float y,float z)=>milkShopRoot.TransformPoint(new Vector3(x,y,z));
        yield return WeatherView("milk-room-noon",TownWeather.Kind.Clear,720,P(0,1.98f,1.75f),result);
        NeedWeather(DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Interior&&l.daylightAccess>=.8f).All(l=>!l.source.enabled),"Clear noon room stayed artificially lit");
        yield return WeatherView("milk-room-noon",TownWeather.Kind.Rain,720,P(0,1.98f,1.75f),result);
        NeedWeather(effects.ListenerSheltered&&DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Interior&&l.daylightAccess>=.8f).All(l=>l.Output>.5f),"Rain did not retain indoor shelter and supplementary lamps");
        result.rainRoomLight=true;
        for(int side=-1;side<=1;side+=2) {
            yield return WalkTo(P(side*2.65f,.2f,-1.10f));yield return WalkTo(P(side*2.65f,.2f,-.29f));
            yield return WeatherView("milk-window-"+(side<0?"north":"south"),TownWeather.Kind.Rain,720,P(side*3.60f,1.675f,-.29f),result);
            NeedWeather(effects.ListenerSheltered&&effects.VisibleStreaks>0,"Rain vanished outside an indoor window");
            dayNightReviewFrozen=false;yield return MilkWindowMotion(milkShopRoot.gameObject,side);dayNightReviewFrozen=true;
            yield return WalkTo(P(side*2.65f,.2f,-1.10f));yield return WalkTo(placement.inside);
        }
        result.shelter=true;
        yield return WalkTo(P(-.52f,.2f,-1.23f));
        var ledger=milkShopRoot.GetComponentInChildren<MilkOrderBook>();player.Aim(ledger.GetComponent<Collider>().bounds.center);yield return null;
        dayNightReviewFrozen=false;NeedWeather(TryInteract()&&MilkOrdersOpen,"Weather UI pause check did not open the normal ledger");yield return new WaitForEndOfFrame();
        double frozen=ElapsedGameMinutes;old=RenderSettings.skybox.GetVector("_CloudOffset");var drops=effects.RainMesh.vertices;
        yield return new WaitForSecondsRealtime(.45f);
        NeedWeather(frozen==ElapsedGameMinutes&&old==RenderSettings.skybox.GetVector("_CloudOffset")&&drops.SequenceEqual(effects.RainMesh.vertices)&&effects.RainVolume==0,"Inspection did not freeze weather and rain");
        result.pause=true;yield return Capture("rain-ledger-pause");CloseMilkOrders();dayNightReviewFrozen=true;
        // Restore ordinary deterministic rain, save its exact phase, relaunch separately.
        cycle.SetReviewWeather(null);gameClock=new GameClock(4*TownWeather.SegmentMinutes+40);cycle.Apply(Minutes);
        yield return WalkTo(placement.inside);yield return null;Save();
        File.WriteAllText(savePath+".weather-proof.json",JsonUtility.ToJson(new WeatherProof{elapsed=ElapsedGameMinutes,processId=System.Diagnostics.Process.GetCurrentProcess().Id,weather=cycle.Weather,cloudOffset=TownWeather.CloudOffset(ElapsedGameMinutes),position=player.transform.position},true));
        result.passed=true;result.verification="Actual Windows rendered player, normal AO/shadows, scripted CharacterController and normal interaction rays; weather/time staging isolated. No physical input or low-end FPS claim.";
        File.WriteAllText(Path.Combine(captures,"weather-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("WEATHER_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyWeatherSave() {
        dayNightReviewFrozen=true;yield return null;
        var proof=JsonUtility.FromJson<WeatherProof>(File.ReadAllText(savePath+".weather-proof.json"));var cycle=DayNightCycle.Instance;
        NeedWeather(clockSaveLoadedAtStartup&&proof.processId!=System.Diagnostics.Process.GetCurrentProcess().Id&&proof.elapsed==clockLoadedElapsedMinutes,"Weather reload changed the saved clock");
        // Compare load-time phase, before the short scene fade advances it.
        NeedWeather(JsonUtility.ToJson(TownWeather.Evaluate(clockLoadedElapsedMinutes))==JsonUtility.ToJson(proof.weather)&&TownWeather.CloudOffset(clockLoadedElapsedMinutes)==proof.cloudOffset,"Weather or cloud phase failed to restore");
        NeedWeather(Vector3.Distance(player.transform.position,proof.position)<.1f&&WeatherShelter.Covered(player.eye.transform.position),"Weather save lost indoor shelter/position");
        cycle.Apply(Minutes);NeedWeather(cycle.Weather.rain>.8f,"Restored ordinary rain changed unexpectedly");
        player.Aim(milkShopRoot.TransformPoint(new Vector3(0,1.98f,1.75f)));yield return Capture("weather-save-relaunch");
        Debug.Log("WEATHER_SAVE_RELAUNCH_PASS");Application.Quit();
    }
}
