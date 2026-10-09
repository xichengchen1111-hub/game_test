using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    bool dayNightReviewFrozen;
    [Serializable] class DayNightSample {
        public string view, phase;
        public double minutes;
        public float daylight, directionalIntensity;
        public int exteriorOn, interiorOn;
    }
    [Serializable] class DayNightProof {
        public double elapsedMinutes;
        public int processId;
        public string selectedDate;
    }
    [Serializable] class DayNightResult {
        public bool passed, clockPause, midnightContinuous, sunlightContinuous, roomAutoOff, deepRoomLit, manualOffPreserved, shadowBudget, emissionSynced;
        public List<DayNightSample> samples=new List<DayNightSample>();
        public string verification;
    }
    void NeedDay(bool ok,string why) {
        if(ok)return;
        Debug.LogError("DAY_NIGHT_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);
    }
    void DayReviewTime(double minutes) {
        NeedDay(Review,"Time staging is only available in the isolated review player");
        dayNightReviewFrozen=true;paused=false;
        gameClock=new GameClock(GameClock.WrapMinutes(minutes-GameClock.InitialMinutes));
        DayNightCycle.Instance.Apply(Minutes);
    }
    void CheckEmission(DayNightLamp lamp) {
        var properties=new MaterialPropertyBlock();
        foreach(var renderer in lamp.glowingSurfaces) {
            if(!renderer)continue;
            var materials=renderer.sharedMaterials;
            for(int slot=0;slot<materials.Length;slot++) {
                var material=materials[slot];
                if(!material||!material.IsKeywordEnabled("_EMISSION"))continue;
                renderer.GetPropertyBlock(properties,slot);
                var expected=material.GetColor("_EmissionColor")*lamp.Output;
                var actual=properties.GetColor("_EmissionColor");
                NeedDay(new Vector3(actual.r-expected.r,actual.g-expected.g,actual.b-expected.b).magnitude<.001f,"Emission differs: "+lamp.name+" / "+renderer.name);
                properties.Clear();
            }
        }
    }
    IEnumerator DayViews(string view,Vector3 look,DayNightResult result) {
        player.Aim(look);
        string[] labels={"dawn","noon","dusk","night"};
        double[] times={375,720,1075,0};
        var cycle=DayNightCycle.Instance;
        for(int i=0;i<times.Length;i++) {
            DayReviewTime(times[i]);yield return null;
            var lamps=DayNightLamp.Active.Where(l=>l.source).ToArray();
            foreach(var lamp in DayNightLamp.Active)CheckEmission(lamp);
            result.samples.Add(new DayNightSample{view=view,phase=labels[i],minutes=Minutes,daylight=cycle.Daylight,directionalIntensity=cycle.celestialLight.intensity,
                exteriorOn=lamps.Count(l=>l.role==DayNightLamp.Role.Exterior&&l.source.enabled),interiorOn=lamps.Count(l=>l.role==DayNightLamp.Role.Interior&&l.source.enabled)});
            NeedDay(lamps.Where(l=>l.role==DayNightLamp.Role.Exterior).All(l=>i==1?!l.source.enabled:l.Output>0),"Exterior lamps daylight rule failed");
            if(i==1) {
                NeedDay(lamps.Where(l=>l.role==DayNightLamp.Role.Interior&&l.daylightAccess>=.8f).All(l=>!l.source.enabled),"Bright room lamps failed to turn off");
                NeedDay(lamps.Where(l=>l.role==DayNightLamp.Role.Interior&&l.daylightAccess<.35f).All(l=>l.source.enabled),"Deep rooms lost necessary daytime light");
            }
            yield return Capture(view+"-"+labels[i]);
        }
        dayNightReviewFrozen=false;paused=false;CursorMode();
    }
    IEnumerator DayBoundary(double boundary,string label) {
        DayReviewTime(GameClock.WrapMinutes(boundary-.4));
        var cycle=DayNightCycle.Instance;
        float previous=cycle.Daylight;
        double elapsed=ElapsedGameMinutes;
        yield return Capture("boundary-"+label+"-before");
        dayNightReviewFrozen=false;
        float until=Time.unscaledTime+.65f;
        while(Time.unscaledTime<until) {
            yield return new WaitForEndOfFrame();
            NeedDay(Mathf.Abs(cycle.Daylight-previous)<.008f,"Rendered horizon brightness jumped: "+label);
            previous=cycle.Daylight;
        }
        dayNightReviewFrozen=true;
        NeedDay(ElapsedGameMinutes-elapsed>.7&&ElapsedGameMinutes-elapsed<1.2,"Boundary check did not use the real clock rate");
        NeedDay(Math.Abs(GameClock.WrapMinutes(Minutes-boundary))<1,"Real game clock failed to cross "+label);
        if(label!="midnight")NeedDay(cycle.celestialLight.intensity<.003f,"Celestial direction changed with a bright horizon light");
        yield return Capture("boundary-"+label+"-after");
        dayNightReviewFrozen=false;
    }
    IEnumerator ReviewDayNight() {
        yield return new WaitForSeconds(.6f);
        var cycle=FindFirstObjectByType<DayNightCycle>();
        NeedDay(cycle&&cycle.celestialLight,"Clock-driven cycle missing");
        cycle.SetReviewWeather(TownWeather.Kind.Clear);
        var lights=FindObjectsByType<Light>(FindObjectsSortMode.None);
        NeedDay(lights.Count(l=>l.type==LightType.Directional)==1&&lights.Count(l=>l.shadows!=LightShadows.None)==4,"Must retain upstream's three local shadow lamps and one celestial source");
        NeedDay(lights.Where(l=>l.type==LightType.Point).All(l=>l.GetComponent<DayNightLamp>()),"Unscheduled point light");
        var result=new DayNightResult{shadowBudget=true};
        float previous=DayNightCycle.NaturalDaylight(0);
        for(int minute=1;minute<=1440;minute++) {
            float value=DayNightCycle.NaturalDaylight(minute);
            NeedDay(Mathf.Abs(value-previous)<.015f,"Daylight jumps at "+minute);previous=value;
            foreach(var lamp in DayNightLamp.Active) {
                float a=DayNightLamp.AutomaticOutput(lamp.role,DayNightCycle.NaturalDaylight(minute-.05),lamp.daylightAccess);
                float b=DayNightLamp.AutomaticOutput(lamp.role,value,lamp.daylightAccess);
                // Nested smoothsteps have a maximum change below 0.5% for this
                // .05 game-minute interval (~35ms at the unchanged clock rate).
                NeedDay(Mathf.Abs(a-b)<.005f,"Fixture jumps at "+minute);
            }
        }
        NeedDay(Vector3.Angle(DayNightCycle.SunDirection(1439.99),DayNightCycle.SunDirection(.01))<.02f,"Orbit jumps at midnight");
        result.midnightContinuous=true;result.sunlightContinuous=true;
        NeedDay(RenderSettings.skybox&&RenderSettings.skybox.shader.name=="WrongDate/DuskSky"&&RenderSettings.skybox!=cycle.skyTemplate,"Additive scene did not use the owned dynamic sky");
        yield return WalkTo(new Vector3(0,.1f,-10));
        player.Aim(new Vector3(0,ClockTowerInteriorLayout.DialHeight,0));
        yield return DayBoundary(360,"sunrise");yield return DayBoundary(1080,"sunset");yield return DayBoundary(0,"midnight");
        yield return DayViews("town",new Vector3(0,ClockTowerInteriorLayout.DialHeight,0),result);
        var shop=GameObject.Find("MilkShop");var placement=shop.GetComponent<MilkShopPlacement>();
        player.Place(placement.outside,0);yield return WalkTo(placement.outside);yield return WalkTo(placement.inside);
        Vector3 P(float x,float y,float z)=>shop.transform.TransformPoint(new Vector3(x,y,z));
        yield return DayViews("milk-room",P(0,1.98f,1.75f),result);
        var reading=shop.GetComponentsInChildren<MilkShopFixture>().Single(f=>f.action==MilkShopFixture.Action.ReadingLamp);
        yield return WalkTo(P(.39f,.2f,.05f));player.Aim(reading.GetComponent<Collider>().bounds.center);yield return null;
        DayReviewTime(720);paused=false;
        NeedDay(TryInteract()&&!reading.LampOn&&!reading.readingLight.enabled,"Manual off failed in daytime");
        DayReviewTime(0);
        NeedDay(!reading.scheduledLamp.RequestedOn&&!reading.readingLight.enabled,"Nightfall undid manual off");CheckEmission(reading.scheduledLamp);
        paused=false;yield return null;
        NeedDay(TryInteract()&&reading.LampOn&&reading.readingLight.enabled,"Cannot resume night illumination");
        DayReviewTime(720);
        NeedDay(reading.LampOn&&!reading.readingLight.enabled,"Manual on bypassed daylight saving");
        result.manualOffPreserved=true;result.roomAutoOff=true;result.deepRoomLit=true;
        dayNightReviewFrozen=false;paused=false;var book=shop.GetComponentInChildren<MilkOrderBook>();
        yield return WalkTo(P(-.14f,.2f,-.02f));player.Aim(book.GetComponent<Collider>().bounds.center);yield return null;
        NeedDay(TryInteract()&&MilkOrdersOpen,"Ledger interaction failed");
        double before=ElapsedGameMinutes;float daylight=cycle.Daylight;Quaternion rotation=cycle.celestialLight.transform.rotation;
        yield return new WaitForSeconds(.5f);
        NeedDay(before==ElapsedGameMinutes&&Mathf.Abs(daylight-cycle.Daylight)<1e-6f&&Quaternion.Angle(rotation,cycle.celestialLight.transform.rotation)<.001f,"UI changed illumination while time paused");
        CloseMilkOrders();result.clockPause=true;
        foreach(int side in new[]{-1,1}) {
            yield return WalkTo(P(side*2.65f,.2f,-.65f));
            yield return DayViews(side<0?"milk-window-north":"milk-window-south",P(side*3.6f,1.87f,.1f),result);
            DayReviewTime(side<0?720:0);paused=false;
            yield return MilkWindowMotion(shop,side);
        }
        yield return WalkTo(placement.inside);yield return WalkTo(placement.outside);
        var door=seamlessShop.GetComponentsInChildren<ShopMechanism>().Single(f=>f.fixtureId=="shop-front-door");
        player.Place(seamlessShop.layout.ToTown(new Vector3(0,.1f,-6.5f)),0);
        yield return WalkTo(seamlessShop.layout.ToTown(new Vector3(0,.1f,-5)));
        player.Aim(door.movingPart.TransformPoint(door.leafCentre));yield return null;
        NeedDay(TryInteract(),"Clock shop door cannot be reached");yield return new WaitForSeconds(.7f);
        yield return WalkTo(seamlessShop.layout.ToTown(new Vector3(0,.1f,-2.8f)));
        yield return DayViews("clock-shop",seamlessShop.layout.ToTown(new Vector3(0,1.8f,2.6f)),result);
        yield return WalkTo(seamlessShop.layout.ToTown(new Vector3(0,.1f,-5)));
        player.Place(placement.outside,0);yield return WalkTo(placement.outside);yield return WalkTo(placement.inside);
        yield return WalkTo(P(.39f,.2f,.05f));player.Aim(reading.GetComponent<Collider>().bounds.center);yield return null;
        NeedDay(TryInteract()&&!reading.LampOn,"Final off switch failed");
        yield return WalkTo(P(-.66f,.2f,-1.1f));DayReviewTime(720);Save();
        File.WriteAllText(savePath+".day-proof.json",JsonUtility.ToJson(new DayNightProof{elapsedMinutes=ElapsedGameMinutes,processId=System.Diagnostics.Process.GetCurrentProcess().Id,selectedDate=state.milkOrderDate},true));
        result.passed=true;result.emissionSynced=true;
        result.verification="Rendered Windows player with normal AO/shadows. Scripted controller and ordinary interaction rays; time staging in isolated review. No physical keyboard or low-end hardware benchmark.";
        File.WriteAllText(Path.Combine(captures,"day-night-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("DAY_NIGHT_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyDayNightSave() {
        dayNightReviewFrozen=true;paused=false;yield return null;
        var proof=JsonUtility.FromJson<DayNightProof>(File.ReadAllText(savePath+".day-proof.json"));
        NeedDay(clockSaveLoadedAtStartup&&proof.processId!=System.Diagnostics.Process.GetCurrentProcess().Id&&proof.elapsedMinutes==clockLoadedElapsedMinutes,"Relaunch changed exact saved clock");
        var cycle=DayNightCycle.Instance;cycle.SetReviewWeather(TownWeather.Kind.Clear);cycle.Apply(Minutes);
        var shop=GameObject.Find("MilkShop");var reading=shop.GetComponentsInChildren<MilkShopFixture>().Single(f=>f.action==MilkShopFixture.Action.ReadingLamp);
        NeedDay(cycle.Daylight==1&&DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Exterior&&l.source).All(l=>!l.source.enabled),"Daylight was not restored from saved time");
        NeedDay(!reading.LampOn&&!reading.scheduledLamp.RequestedOn&&!reading.readingLight.enabled&&state.milkOrderDate==proof.selectedDate,"Relaunch lost lamp or ledger state");
        var p=shop.transform.InverseTransformPoint(player.transform.position);
        NeedDay(Math.Abs(p.x+.66f)<.15f&&Math.Abs(p.z+1.1f)<.15f,"Relaunch lost interior position");
        player.Aim(shop.transform.TransformPoint(new Vector3(0,1.98f,1.75f)));yield return Capture("day-night-save-relaunch");
        Debug.Log("DAY_NIGHT_SAVE_RELAUNCH_PASS");Application.Quit();
    }
}
