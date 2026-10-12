using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public partial class DemoApp {
    void NeedMap(bool ok,string why) {
        if(ok)return;
        Debug.LogError("FULL_MAP_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);
    }
    IEnumerator MapWalk(Vector3 target) {
        float distance=Vector2.Distance(new Vector2(target.x,target.z),new Vector2(player.transform.position.x,player.transform.position.z));
        float deadline=Time.unscaledTime+distance/PlayerMotor.SprintSpeed+8;
        player.scriptedSprint=true;player.scriptedPlayerInput=true;
        while(Time.unscaledTime<deadline) {
            var delta=target-player.transform.position;delta.y=0;if(delta.magnitude<.1f)break;
            // Same Shift speed and normalization as player input; slow only at stops.
            player.scriptedDirection=delta.normalized*Mathf.Clamp(delta.magnitude*2,.08f,1);
            yield return null;
            NeedMap(player.transform.position.y>-.15f&&player.transform.position.y<.45f,"Street route fell or climbed a prop: "+player.transform.position);
        }
        player.scriptedDirection=Vector3.zero;player.scriptedSprint=false;player.scriptedPlayerInput=false;
        NeedMap(Time.unscaledTime<deadline,"Street route blocked: "+target+" from "+player.transform.position);
    }
    Vector3[] MapLeg(string from,string to) {
        Vector3 V(float x,float z)=>new Vector3(x,.1f,z);
        Vector3[] shopHome={V(-35,26.7f),V(-35,25),V(26,25),V(26,-70),V(-14.5f,-70),V(-14.5f,-68),V(-14.5f,-57),V(-15.5f,-57)};
        Vector3[] shopStation={V(-35,26.7f),V(-35,25),V(188,25),V(188,30),V(188,38.7f)};
        Vector3[] homeStation={V(-15.5f,-57),V(-14.5f,-57),V(-14.5f,-68),V(-14.5f,-70),V(26,-70),V(26,25),V(188,25),V(188,30),V(188,38.7f)};
        var route=from=="shop"&&to=="home"||from=="home"&&to=="shop"?shopHome:from=="shop"&&to=="station"||from=="station"&&to=="shop"?shopStation:homeStation;
        bool reverse=route==shopHome?from=="home":route==shopStation?from=="station":from=="station";
        return reverse?route.Reverse().ToArray():route;
    }
    void CheckMapMigrations() {
        NeedMap(JsonUtility.FromJson<SliceSave>("{\"scene\":\"Town\",\"x\":9.72,\"z\":1}").worldRevision==0,"Missing map revision must remain a legacy save");
        var original=state;bool loaded=clockSaveLoadedAtStartup;clockSaveLoadedAtStartup=true;
        var tests=new[]{new Vector3(0,15,10),new Vector3(9.72f,.1f,1),new Vector3(11.45f,.14f,-7.35f),new Vector3(0,.1f,-12)};
        var expected=new[]{new Vector3(0,15,0),seamlessShop.layout.ToTown(new Vector3(0,.1f,0)),milkShopRoot.TransformPoint(new Vector3(0,.2f,0)),TownPlan.Spawn};
        for(int i=0;i<tests.Length;i++) {
            state=new SliceSave{worldRevision=2,towerLayoutRevision=1,towerAdjustmentMinutes=60,elapsedGameMinutes=412.125,gameClockRevision=1,milkOrderDate="1931-01-19",remarkSeen=true,milkboyGreetingRevision=2};
            state.entries.Add(new NotebookEntry{id="E05",body="Migration fixture"});state.openFixtures.Add("milk-reading-lamp-off");
            var position=tests[i];MigrateTownPosition(ref position,false);
            NeedMap(Vector3.Distance(position,expected[i])<.001f&&state.worldRevision==3,"Old map position migration "+i);
            NeedMap(state.entries.Count==1&&state.elapsedGameMinutes==412.125&&state.towerAdjustmentMinutes==60&&state.openFixtures.Contains("milk-reading-lamp-off")&&state.milkOrderDate=="1931-01-19"&&state.remarkSeen&&state.milkboyGreetingRevision==2,"Migration lost progress");
        }
        state=original;clockSaveLoadedAtStartup=loaded;
    }
    IEnumerator MapOverview() {
        var overview=new GameObject("Review map overview").AddComponent<Camera>();overview.CopyFrom(player.eye);
        overview.farClipPlane=700;overview.fieldOfView=48;
        overview.transform.position=new Vector3(35,370,-180);overview.transform.LookAt(new Vector3(35,0,-16));
        overview.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        bool fog=RenderSettings.fog;RenderSettings.fog=false;player.eye.enabled=false;
        yield return Capture("full-town-overview");
        player.eye.enabled=true;RenderSettings.fog=fog;Destroy(overview.gameObject);
    }
    IEnumerator ReviewFullMap() {
        yield return new WaitForSeconds(.8f);
        var plan=TownPlan.Current;var markers=FindObjectsByType<TownMapBuilding>(FindObjectsSortMode.None);
        NeedMap(markers.Length==70&&markers.Select(m=>m.mapId).Distinct().Count()==70&&markers.Count(m=>m.finished)==5,"Property identity/count (tower, shop, dairy, station, police)");
        NeedMap(FindFirstObjectByType<ClockTowerInstance>().transform.position==Vector3.zero,"Clock tower map origin");
        NeedMap(FindObjectsByType<StreetSurfaceLayout>(FindObjectsSortMode.None).Length==11,"Street profiles missing");
        CheckMapMigrations();
        foreach(var road in plan.roads) {
            var a=new Vector3(road.points[0],0,road.points[1]);var b=new Vector3(road.points[2],0,road.points[3]);var direction=(b-a).normalized;
            var normal=new Vector3(-direction.z,0,direction.x);float offset=road.lane>0?road.lane/2+road.strip+road.walk/2:0;
            var start=a+direction*2+normal*offset;
            foreach(int side in road.lane>0?new[]{-1,1}:new[]{1})foreach(float edge in new[]{-.5f,0,.5f}) {
                float band=road.lane>0?offset+edge*(road.walk-.5f):edge*(road.walk-.5f);
                var sweepStart=a+direction*2+normal*side*band;var sweepEnd=b-direction*2+normal*side*band;
                bool blocked=Physics.CapsuleCast(sweepStart+Vector3.up*.4f,sweepStart+Vector3.up*1.55f,.24f,direction,out var obstruction,Vector3.Distance(sweepStart,sweepEnd));
                NeedMap(!blocked,"Clear footway swept width blocked on "+road.id+" side="+side+" offset="+band+" by "+(obstruction.collider?obstruction.collider.name:"none"));
            }
            player.Place(start+Vector3.up*.12f,0);yield return MapWalk(start+direction*4+Vector3.up*.1f);
        }
        foreach(var shelter in FindObjectsByType<WeatherShelter>(FindObjectsSortMode.None)) {
            foreach(var local in new[]{Vector3.zero,new Vector3(shelter.footprint.x*.5f,0,shelter.footprint.y*.5f),new Vector3(-shelter.footprint.x*.5f,0,-shelter.footprint.y*.5f)})
                NeedMap(WeatherShelter.VerifySpatialIndex(shelter.transform.TransformPoint(local)),"Spatial roof index disagrees with exhaustive roof test");
        }
        var cycle=DayNightCycle.Instance;cycle.SetReviewWeather(TownWeather.Kind.Clear);DayReviewTime(720);
        yield return MapOverview();
        var lights=FindObjectsByType<Light>(FindObjectsSortMode.None);
        NeedMap(lights.Count(l=>l.shadows!=LightShadows.None)==4,"Upstream shadow-light budget changed");
        NeedMap(DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Exterior&&l.source).All(l=>!l.source.enabled),"Daytime street lights remain on");
        // All six station/shop/home exploration orders, on physical clear footways.
        string[][] orders={new[]{"shop","home","station"},new[]{"shop","station","home"},new[]{"home","shop","station"},new[]{"home","station","shop"},new[]{"station","shop","home"},new[]{"station","home","shop"}};
        foreach(var order in orders) {
            var first=MapLeg(order[0],order[1]);player.Place(first[0],0);
            for(int i=0;i<2;i++)foreach(var waypoint in MapLeg(order[i],order[i+1]))yield return MapWalk(waypoint);
            NeedMap(state.entries.Count==0,"Street placeholders granted evidence");
            Debug.Log("FULL_MAP_ORDER_PASS: "+string.Join(" -> ",order));
        }
        player.Place(new Vector3(-35,.1f,25),0);DayReviewTime(1075);player.Aim(new Vector3(30,3,20));yield return Capture("station-road-dusk");
        DayReviewTime(0);yield return new WaitForSeconds(.35f);
        NeedMap(DayNightLamp.Active.Count(l=>l.role==DayNightLamp.Role.Exterior&&l.sharesStreetPoolBudget&&l.source&&l.source.enabled)<=TownLightBudget.MaximumPools,"Too many local street pools");
        NeedMap(DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Exterior).All(l=>l.Output>.9f),"Distant lamp bulbs extinguished at night");
        yield return Capture("station-road-night");
        // Milk station reached on the same public street, then through its real doorway.
        DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Rain);
        player.Place(new Vector3(-35,.1f,25),0);
        foreach(var pos in new[]{new Vector3(-143.5f,.1f,25),new Vector3(-143.5f,.1f,13)})yield return MapWalk(pos);
        var placement=milkShopRoot.GetComponent<MilkShopPlacement>();yield return MapWalk(placement.outside);
        player.Aim(milkShopRoot.position+Vector3.up*2);yield return Capture("dairy-yard-rain");
        yield return WalkTo(placement.inside);NeedMap(insideMilkShop,"Dairy real door blocked after relocation");
        player.Aim(milkShopRoot.TransformPoint(new Vector3(0,1.98f,1.75f)));yield return Capture("dairy-interior-rain");
        NeedMap(FindFirstObjectByType<WeatherEffects>().ListenerSheltered,"Moved dairy roof leaks rain");
        // Native 3D volume visibility, with ordinary collision. Houses remain placeholders.
        Vector3 neighbor=new Vector3(-40.8f,1.6f,-57),kitchen=new Vector3(-33,1.6f,-57),front=new Vector3(-15.5f,1.6f,-57);
        NeedMap(!Physics.Linecast(neighbor,kitchen),"Neighbor-to-kitchen proxy view blocked by an item");
        NeedMap(Physics.Linecast(neighbor,front),"Home volume fails to hide east-door return");
        var dial=new Vector3(0,ClockTowerInteriorLayout.DialHeight,-ClockTowerInteriorLayout.DialPlaneDistance);
        bool obstructed=Physics.Linecast(front,dial,out var hit)&&!hit.collider.GetComponentInParent<ClockTowerInstance>();
        NeedMap(!obstructed,"Delivery point clock corridor blocked: "+(hit.collider?hit.collider.name:"none"));
        // Readable ground-player view of the real clock from the proposed delivery position.
        player.Place(new Vector3(-15.5f,.1f,-57),0);player.Aim(dial);cycle.SetReviewWeather(TownWeather.Kind.Clear);DayReviewTime(1075);yield return Capture("home-delivery-clock-view");
        player.Place(placement.inside,0);dayNightReviewFrozen=false;cycle.SetReviewWeather(null);Save();
        File.WriteAllText(Path.Combine(captures,"full-map-results.json"),JsonUtility.ToJson(new MapResult{passed=true,properties=70,roads=11,orders=6,migrations=4,pointLightLimit=12,source=plan.sourceSha256,verification=Application.platform+" native renderer and real controller at Shift speed on clear footways. Five finished buildings (tower, shop, dairy, station, police) preserved; other rooms, historical NPC schedules and coal vehicle turning remain unimplemented."},true));
        Debug.Log("FULL_MAP_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyFullMapSave() {
        yield return new WaitForSeconds(.7f);
        NeedMap(state.worldRevision==3&&insideMilkShop,"Relocated indoor save failed in new process");
        NeedMap(state.gameClockRevision==1&&GameClock.ValidElapsed(state.elapsedGameMinutes),"Clock phase lost");
        var placement=milkShopRoot.GetComponent<MilkShopPlacement>();yield return WalkTo(placement.outside);
        NeedMap(!insideMilkShop,"Saved dairy cannot exit");
        yield return Capture("full-map-save-relaunch");Debug.Log("FULL_MAP_SAVE_PASS");Application.Quit();
    }
    [Serializable] class MapResult {public bool passed;public int properties,roads,orders,migrations,pointLightLimit;public string source,verification;}
}
