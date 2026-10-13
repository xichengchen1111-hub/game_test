using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class GreeneryResult {
        public bool passed,originalTrunks,roadsClear,entryClear,sightlineClear,milkCartClear;
        public int trees,plants,models,batches,lodGroups,views;
        public float meanFrameMs,p95FrameMs;public int sampledFrames;
        public string mapSource,verification;
    }
    IEnumerator ReviewGreenery() {
        yield return new WaitForSeconds(.8f);
        var green=FindFirstObjectByType<TownGreenery>();var plan=TownPlan.Current;
        NeedMap(green&&green.treeCount==217&&green.modelCount==22&&green.plantCount>10000,"Greenery kit/counts missing");
        NeedMap(green.mapSource==plan.sourceSha256&&!GameObject.Find("Map tree placeholders"),"Old tree cards or stale planting map");
        var colliders=green.GetComponentsInChildren<Collider>();
        NeedMap(colliders.Length==217,"Vegetation added new ground obstructions");
        foreach(var t in plan.trees) {
            var collider=colliders.Single(c=>Vector2.Distance(new Vector2(c.transform.position.x,c.transform.position.z),new Vector2(t.x,t.z))<.001f) as CapsuleCollider;
            NeedMap(collider&&Mathf.Abs(collider.radius-.13f)<.001f&&Mathf.Abs(collider.height-t.r*2.7f*.6f)<.001f,"Existing trunk collision drift");
        }
        foreach(var road in plan.roads) {
            var a=new Vector3(road.points[0],0,road.points[1]);var b=new Vector3(road.points[2],0,road.points[3]);var dir=(b-a).normalized;var normal=new Vector3(-dir.z,0,dir.x);
            float offset=road.lane>0?road.lane/2+road.strip+road.walk/2:0;
            foreach(int side in road.lane>0?new[]{-1,1}:new[]{1})foreach(float edge in new[]{-.5f,0,.5f}) {
                float band=road.lane>0?offset+edge*(road.walk-.5f):edge*(road.walk-.5f);var start=a+dir*2+normal*side*band;var end=b-dir*2+normal*side*band;
                NeedMap(!Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.55f,.24f,dir,out var hit,Vector3.Distance(start,end)),"Planting blocks footway "+road.id+" "+(hit.collider?hit.collider.name:"none"));
            }
        }
        var dairy=plan.buildings.Single(b=>b.id=="B04");var cart=TownPlan.Point(dairy.gate)+new Vector3(-2,0,-.9f);
        var instances=green.GetComponent<GreeneryInstances>();
        foreach(var batch in instances.batches)foreach(var matrix in batch.matrices) {
            var position=matrix.MultiplyPoint3x4(Vector3.zero);
            NeedMap(Mathf.Abs(position.x-cart.x)>=1.60f||Mathf.Abs(position.z-cart.z)>=1.30f,"Flowers overlap the milk cart reserve");
        }
        var cycle=DayNightCycle.Instance;DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();
        var points=new[]{new Vector3(-35,.1f,25),new Vector3(18,.1f,-70),new Vector3(192,.1f,-19),new Vector3(-151,.1f,88),new Vector3(-143.5f,.1f,14),new Vector3(0,.1f,-11)};
        var looks=new[]{new Vector3(-55,3,20),new Vector3(-4,3,-75),new Vector3(203,2,-12),new Vector3(-145,3,106),new Vector3(-147,1,10),new Vector3(-7.4f,.35f,-10.7f)};
        string[] names={"main-street","residential-street","station-garden","north-orchard","dairy-approach","plaza-flowers"};
        for(int i=0;i<points.Length;i++) {
            player.Place(points[i],0);player.Aim(looks[i]);yield return new WaitForSeconds(.7f);
            recorder.Begin(Path.Combine(captures,names[i]));yield return new WaitForSeconds(1.5f);
            var right=player.eye.transform.right;right.y=0;right.Normalize();
            player.scriptedDirection=right*.24f;yield return new WaitForSeconds(2.5f);player.scriptedDirection=Vector3.zero;
            yield return new WaitForSeconds(.7f);yield return Capture(names[i]);recorder.End();
        }
        // Near flowers at ordinary standing height, plus a clear-noon palette view.
        player.Place(new Vector3(-7.4f,.12f,-12.2f),0);player.Aim(new Vector3(-7.4f,.25f,-10.7f));yield return Capture("flower-close");
        DayReviewTime(565);cycle.SetReviewWeather(TownWeather.Kind.Clear);player.Place(new Vector3(-35,.1f,25),0);player.Aim(new Vector3(-55,3,20));yield return new WaitForSeconds(.5f);yield return Capture("main-street-clear");
        DayReviewTime(0);cycle.SetReviewWeather(TownWeather.Kind.Rain);yield return new WaitForSeconds(.7f);yield return Capture("main-street-night-rain");
        DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        var placement=milkShopRoot.GetComponent<MilkShopPlacement>();player.Place(placement.outside,0);yield return WalkTo(placement.inside);NeedMap(insideMilkShop,"Greenery blocks dairy entrance");yield return WalkTo(placement.outside);
        var sightA=new Vector3(-40.8f,1.6f,-57);var sightB=new Vector3(-33,1.6f,-57);
        NeedMap(!Physics.Linecast(sightA,sightB),"Kitchen sightline blocked");
        var front=HomeDeliveryStandingPoint()+Vector3.up*1.66f;var dial=new Vector3(0,ClockTowerInteriorLayout.DialHeight,-ClockTowerInteriorLayout.DialPlaneDistance);
        NeedMap(!Physics.Linecast(front,dial,out var block)||block.collider.GetComponentInParent<ClockTowerInstance>(),"Clock sightline blocked");
        player.Place(HomeDeliveryStandingPoint(),0);player.Aim(dial);yield return Capture("clock-sightline");
        // Detailed near-player bed and bark views, without changing live town lighting.
        player.Place(new Vector3(12,.1f,-70),0);player.Aim(new Vector3(8,.7f,-71.8f));yield return Capture("layered-roadside-border");
        player.Place(new Vector3(-30.8f,.1f,25.0f),0);player.Aim(new Vector3(-30,1.2f,23.5f));yield return new WaitForSeconds(.5f);yield return Capture("tree-bark-and-flowers");
        // Walk parallel to a layered outer border on the real residential footway.
        player.Place(new Vector3(18,.1f,-70),0);player.Aim(new Vector3(5,.65f,-67.0f));yield return new WaitForSeconds(.7f);
        recorder.Begin(Path.Combine(captures,"roadside-flower-walk"));
        player.scriptedDirection=Vector3.left*.32f;yield return new WaitForSeconds(8);player.scriptedDirection=Vector3.zero;recorder.End();
        yield return Capture("layered-border-overcast");
        DayReviewTime(565);cycle.SetReviewWeather(TownWeather.Kind.Clear);yield return new WaitForSeconds(.7f);yield return Capture("layered-border-clear");
        // Short local timing sample WITHOUT image/audio recording, not a hardware benchmark.
        var timing=new System.Collections.Generic.List<float>();float sampleEnd=Time.realtimeSinceStartup+5;
        while(Time.realtimeSinceStartup<sampleEnd){yield return null;timing.Add(Time.unscaledDeltaTime*1000);}
        timing.Sort();float mean=timing.Average(),p95=timing[Mathf.Min(timing.Count-1,Mathf.FloorToInt(timing.Count*.95f))];
        DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        yield return MapOverview();
        var result=new GreeneryResult{passed=true,originalTrunks=true,roadsClear=true,entryClear=true,sightlineClear=true,milkCartClear=true,trees=green.treeCount,plants=green.plantCount,models=green.modelCount,batches=green.batchCount,lodGroups=green.GetComponentsInChildren<LODGroup>().Length,views=15,meanFrameMs=mean,p95FrameMs=p95,sampledFrames=timing.Count,mapSource=green.mapSource,
            verification=Application.platform+" native rendered player; authored collider comparison, road capsule sweeps, actual dairy entry/exit. Staged camera positions between areas; real small lateral controller movement. Not physical input, blind playtest, full-town walking or low-end FPS proof."};
        File.WriteAllText(Path.Combine(captures,"greenery-results.json"),JsonUtility.ToJson(result,true));Debug.Log("GREENERY_REVIEW_PASS");Application.Quit();
    }
}
