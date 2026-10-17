using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Native review of the 1931 street furniture (--review --fresh --review-street-items), isolated
// street-items-review-save.json. Checks the map placements, clear 2 m footways, solid collision,
// walking past the items, the alarm globe's photocell behaviour and the unchanged light-pool budget,
// and captures every kind in daylight plus night and rain views. Program-driven, muted.
public partial class DemoApp {
    [Serializable] class StreetItemsReviewResult { public bool passed; public int placements, kinds, colliders, footwaySweeps, solidPushes, walks; public float globeNight, globeNoon; public string[] captures; public string verification; }
    void NeedStreet(bool ok,string why) {
        if(ok)return;
        Debug.LogError("STREET_ITEMS_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);
    }
    IEnumerator ReviewStreetItems() {
        yield return new WaitForSeconds(.8f);
        var plan=TownPlan.Current;var placed=FindObjectsByType<StreetItem>(FindObjectsSortMode.None);
        NeedStreet(plan.streetItems.Length==41&&placed.Length==41,"41 map placements present: "+placed.Length);
        NeedStreet(placed.Select(p=>p.kind).Distinct().Count()==10,"ten kinds");
        foreach(var it in plan.streetItems) {
            var p=placed.SingleOrDefault(q=>q.id==it.id);NeedStreet(p,"missing "+it.id);
            NeedStreet(Vector2.Distance(new Vector2(p.transform.position.x,p.transform.position.z),new Vector2(it.point[0],it.point[1]))<.001f,"placement drift "+it.id);
            NeedStreet(p.GetComponents<Collider>().Length>0&&!p.GetComponentInChildren<Interactable>()&&!p.GetComponentInChildren<Light>(),"collision only, no interaction or light: "+it.id);
        }
        int colliders=placed.Sum(p=>p.GetComponents<Collider>().Length);
        // Clear footway width on every road, both sides, three lanes of the 2 m walk (same sweep as the full-map review).
        int sweeps=0;
        foreach(var road in plan.roads) {
            var a=new Vector3(road.points[0],0,road.points[1]);var b=new Vector3(road.points[2],0,road.points[3]);var dir=(b-a).normalized;var normal=new Vector3(-dir.z,0,dir.x);
            foreach(int side in road.lane>0?new[]{-1,1}:new[]{1})foreach(float edge in new[]{-.5f,0,.5f}) {
                float band=road.lane>0?road.lane/2+road.strip+road.walk/2+edge*(road.walk-.5f):edge*(road.walk-.5f);
                var start=a+dir*2+normal*side*band;var end=b-dir*2+normal*side*band;
                bool hit=Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.55f,.24f,dir,out var obstruction,Vector3.Distance(start,end));
                NeedStreet(!hit||!obstruction.collider.GetComponentInParent<StreetItem>(),"street item blocks the clear footway of "+road.id+": "+(hit?obstruction.collider.name:""));
                sweeps++;
            }
        }
        var cycle=DayNightCycle.Instance;DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Overcast);
        yield return new WaitForSeconds(.6f);
        // Solid: walking straight at an item stops at its surface instead of passing through.
        int pushes=0;
        foreach(var kind in new[]{"hydrant","mailbox","alarm","trough","column","sandbin","litter","treeguard","hitch","shoeshine"}) {
            var p=placed.First(q=>q.kind==kind);var toward=p.transform.rotation*Vector3.forward; // from behind (footway side) towards the front
            var centre=p.transform.position;var startPos=centre-toward*(Mathf.Max(p.footprint.x,p.footprint.y)/2+1.2f);
            player.Place(new Vector3(startPos.x,centre.y+.05f,startPos.z),0);yield return null;
            float until=Time.time+2.2f;while(Time.time<until){player.scriptedDirection=toward;yield return null;}
            player.scriptedDirection=Vector3.zero;
            var feet=player.transform.position;var local=p.transform.InverseTransformPoint(feet);
            // The shoeshine stand's 0.2 m customer step is meant to be stepped on; its 0.5 m platform is not.
            if(kind=="shoeshine")NeedStreet(local.z<-.34f,"player walked into the shoeshine platform "+p.id+" local="+local);
            else NeedStreet(Mathf.Abs(local.x)>p.footprint.x/2-.05f||Mathf.Abs(local.z)>p.footprint.y/2-.05f,"player walked into "+kind+" "+p.id+" local="+local);
            NeedStreet(feet.y<centre.y+.3f,"player climbed onto "+kind+" "+p.id);
            pushes++;
        }
        // Real walks past the furniture along the footways (station road shop side, residential street west).
        int walks=0;
        foreach(var leg in new[]{new[]{new Vector3(-114,.1f,25),new Vector3(-60,.1f,25),new Vector3(-2,.1f,25)},new[]{new Vector3(30,.1f,25),new Vector3(130,.1f,25)},new[]{new Vector3(-108,.1f,-70),new Vector3(-80,.1f,-70)},new[]{new Vector3(-124,.1f,-25),new Vector3(-124,.1f,-5)}}) {
            player.Place(leg[0],0);yield return null;
            for(int i=1;i<leg.Length;i++)yield return MapWalk(leg[i]);
            walks++;
        }
        // Photocell globe on the fire alarms: off in overcast daylight, full at night, never a pooled light.
        var globes=placed.Where(p=>p.kind=="alarm").Select(p=>p.GetComponentInChildren<DayNightLamp>()).ToArray();
        NeedStreet(globes.Length==2&&globes.All(g=>g&&!g.source&&!g.sharesStreetPoolBudget),"alarm globes are emissive-only photocell surfaces");
        NeedStreet(globes.All(g=>g.glowingSurfaces.Length==1&&g.glowingSurfaces[0].sharedMaterials.Any(m=>m.name=="SI_RedGlobe"&&m.IsKeywordEnabled("_EMISSION"))),"alarm globe has no emissive glass");
        // Same photocell curve as the street lanterns (partly up under thick overcast), fully off in clear daylight.
        var lantern=DayNightLamp.Active.First(l=>l.role==DayNightLamp.Role.Exterior&&l.source&&l.sharesStreetPoolBudget);
        NeedStreet(globes.All(g=>Mathf.Abs(g.Output-lantern.Output)<.01f),"alarm globe disagrees with the street lantern curve: "+globes[0].Output+" vs "+lantern.Output);
        cycle.SetReviewWeather(TownWeather.Kind.Clear);DayReviewTime(720);yield return new WaitForSeconds(.3f);
        float noon=globes.Max(g=>g.Output);NeedStreet(noon<.01f,"alarm globe lit in clear daylight: "+noon);
        cycle.SetReviewWeather(TownWeather.Kind.Overcast);DayReviewTime(720);yield return new WaitForSeconds(.3f);
        var shots=new System.Collections.Generic.List<string>();
        IEnumerator View(string name,StreetItem p,float distance,float side,float eye,float aimHeight) {
            var front=p.transform.rotation*Vector3.back;var right=p.transform.rotation*Vector3.right;
            var at=p.transform.position+front*distance+right*side;
            player.Place(new Vector3(at.x,.1f,at.z),0);player.Aim(p.transform.position+Vector3.up*aimHeight);
            yield return new WaitForSeconds(.6f);yield return Capture(name);shots.Add(name);
        }
        foreach(var kind in new[]{"hydrant","mailbox","alarm","trough","hitch","column","litter","sandbin","shoeshine","treeguard"}) {
            var p=placed.First(q=>q.kind==kind);
            yield return View("item-"+kind,p,1.6f+p.height*.6f,.9f,1.6f,p.height*.45f);
        }
        player.Place(new Vector3(-70,.1f,25.6f),0);player.Aim(new Vector3(-30,1.2f,23.6f));yield return new WaitForSeconds(.6f);yield return Capture("street-shopfronts");shots.Add("street-shopfronts");
        player.Place(new Vector3(-6,.1f,9),0);player.Aim(new Vector3(-12.5f,1.6f,4));yield return new WaitForSeconds(.6f);yield return Capture("square-column");shots.Add("square-column");
        cycle.SetReviewWeather(TownWeather.Kind.Rain);DayReviewTime(690);yield return new WaitForSeconds(.8f);
        yield return View("rain-trough",placed.First(q=>q.kind=="trough"),2.4f,1.2f,1.6f,.6f);
        cycle.SetReviewWeather(TownWeather.Kind.Overcast);DayReviewTime(22*60);yield return new WaitForSeconds(.8f);
        float night=globes.Min(g=>g.Output);NeedStreet(night>.9f,"alarm globe dark at night: "+night);
        foreach(var g in globes)CheckEmission(g);
        NeedStreet(DayNightLamp.Active.Count(l=>l.role==DayNightLamp.Role.Exterior&&l.sharesStreetPoolBudget&&l.source&&l.source.enabled)<=TownLightBudget.MaximumPools,"street light pool budget exceeded");
        yield return View("night-alarm",placed.First(q=>q.kind=="alarm"),3.2f,1.4f,1.6f,1.6f);
        NeedStreet(state.entries.Count==0,"street furniture granted evidence");
        var result=new StreetItemsReviewResult{passed=true,placements=placed.Length,kinds=10,colliders=colliders,footwaySweeps=sweeps,solidPushes=pushes,walks=walks,globeNight=night,globeNoon=noon,captures=shots.ToArray(),
            verification="Native player, staged clock/weather and camera; real CharacterController pushes and sprint walks; muted. Not physical keyboard/mouse or a blind test."};
        File.WriteAllText(Path.Combine(captures,"street-items-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("STREET_ITEMS_REVIEW_PASS");Application.Quit();
    }
}
