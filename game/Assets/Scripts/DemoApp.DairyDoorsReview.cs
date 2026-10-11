using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class DairyDoorsResult {
        public bool passed,counterReachable,bothDoorsInteract,closedDoorBlocks,obstructedCloseRejected,openDoorOutdoorSound,closedDoorMuffled,otherHouseIndependent,roofStillDry,weatherLamps,savePreserved;
        public Vector3 milkboyLocal;public float rainLight,clearLight;public string verification;
    }
    // Pick a visible point on the real leaf, then use the ordinary E interaction ray.
    IEnumerator UseDoorRay(ShopMechanism door) {
        var leaf=door.GetComponent<BoxCollider>();bool found=false;
        foreach(float x in new[]{0f,-.35f,.35f,-.46f,.46f}) {
            foreach(float y in new[]{0f,-.2f,.2f}) {
                foreach(float z in new[]{0f,-.4f,.4f}) {
                    var target=leaf.transform.TransformPoint(leaf.center+Vector3.Scale(leaf.size,new Vector3(x,y,z)));
                    var ray=target-player.eye.transform.position;
                    if(Physics.Raycast(player.eye.transform.position,ray.normalized,out var hit,2.65f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<ShopMechanism>()==door) {
                        player.Aim(target);found=true;break;
                    }
                }
                if(found)break;
            }
            if(found)break;
        }
        NeedWeather(found,"No visible/reachable leaf for "+door.fixtureId+" at "+player.transform.position+" leaf="+leaf.bounds.center);
        yield return null;bool before=door.IsOpen;
        NeedWeather(TryInteract()&&before!=door.IsOpen&&!door.LastUseBlocked,"Normal door interaction failed: "+door.fixtureId);
        yield return new WaitForSeconds(.85f);
        NeedWeather(!door.Moving,"Door sweep did not finish: "+door.fixtureId);
    }
    IEnumerator ReviewDairyDoors() {
        yield return new WaitForSeconds(.6f);DayReviewTime(690);var cycle=DayNightCycle.Instance;cycle.SetReviewWeather(TownWeather.Kind.Rain);dayNightReviewFrozen=false;
        var effects=cycle.GetComponent<WeatherEffects>();var sound=effects.Ambience;var result=new DairyDoorsResult();var acoustics=new RainReviewResult();
        var left=Mechanism("milk-left-door");var right=Mechanism("milk-right-door");var marker=milkShopRoot.GetComponent<MilkShopPlacement>();
        Vector3 P(float x,float z)=>milkShopRoot.TransformPoint(new Vector3(x,.20f,z));
        var boy=GameObject.Find("Milkboy");result.milkboyLocal=milkShopRoot.InverseTransformPoint(boy.transform.position);
        NeedWeather(Vector3.Distance(result.milkboyLocal,new Vector3(.15f,.18f,.15f))<.01f,"Boy is not in front of the counter");
        NeedWeather(left.IsOpen&&!right.IsOpen&&milkShopRoot.GetComponent<WeatherShelter>().exteriorDoors.Length==2,"Milk door migration/default/binding failed");
        player.Place(marker.outside,0);yield return WalkTo(marker.inside);yield return ApproachReviewMilkboy();
        player.Aim(boy.transform.position+Vector3.up*.94f);yield return null;
        NeedWeather(TryInteract()&&DialogueOpen,"Counter-side milkboy is not reachable by normal interaction");yield return Capture("milkboy-at-counter");CloseDialogue();result.counterReachable=true;
        yield return WalkTo(P(2.35f,-1.1f));yield return WalkTo(marker.inside);
        var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,"01-milk-doors-rain"));
        player.Aim(boy.transform.position+Vector3.up*.9f);yield return new WaitForSeconds(2);yield return RainSoundSample("milk-open-left",acoustics);
        NeedWeather(effects.ListenerSheltered&&sound.ShelterBlend<.02f&&sound.Cutoff>10000,"Open entrance did not retain outdoor rain under the dry roof");
        RainRoofCheck(effects);result.openDoorOutdoorSound=result.roofStillDry=true;
        yield return UseDoorRay(left);yield return new WaitForSeconds(1.2f);yield return RainSoundSample("milk-both-closed",acoustics);
        NeedWeather(sound.ShelterBlend>.98f&&sound.Cutoff<1200&&acoustics.acoustics[1].highFrequencyRatio<acoustics.acoustics[0].highFrequencyRatio*.4f,"Closed shop did not muffle final listener rain");
        result.closedDoorMuffled=true;yield return Capture("milk-doors-closed");
        // A door in a different parcel cannot change this room's acoustics.
        var watch=Mechanism("shop-front-door");watch.Use();yield return new WaitForSeconds(1.1f);
        NeedWeather(watch.IsOpen&&sound.ShelterBlend>.98f,"Unrelated house door changed milk acoustics");watch.Use();yield return new WaitForSeconds(.85f);result.otherHouseIndependent=true;
        player.scriptedDirection=milkShopRoot.TransformDirection(Vector3.back);yield return new WaitForSeconds(.85f);player.scriptedDirection=Vector3.zero;
        NeedWeather(milkShopRoot.InverseTransformPoint(player.transform.position).z>-2.85f,"Closed milk door failed to block controller");result.closedDoorBlocks=true;
        yield return WalkTo(marker.inside);yield return WalkTo(P(.66f,-1.1f));
        yield return UseDoorRay(right);yield return new WaitForSeconds(1.2f);yield return RainSoundSample("milk-open-right",acoustics);
        NeedWeather(sound.ShelterBlend<.02f,"Right door did not restore outdoor sound");yield return Capture("milk-right-open");
        // Walk onto the actual closed-leaf plane, beyond WalkTo's 10cm stopping tolerance.
        yield return WalkTo(P(.66f,-3.08f));right.Use();NeedWeather(right.IsOpen&&right.LastUseBlocked,"Milk leaf clips player in doorway");result.obstructedCloseRejected=true;
        yield return WalkTo(P(.66f,-4.6f));player.Aim(milkShopRoot.TransformPoint(new Vector3(0,1.5f,-2.9f)));yield return new WaitForSeconds(2);
        yield return UseDoorRay(right);yield return UseDoorRay(right);yield return WalkTo(P(.66f,-1.1f));yield return UseDoorRay(right);yield return WalkTo(marker.inside);
        yield return UseDoorRay(left);yield return new WaitForSeconds(1.2f);yield return WalkTo(marker.outside);yield return UseDoorRay(left);yield return UseDoorRay(left);yield return WalkTo(marker.inside);yield return UseDoorRay(left);
        result.bothDoorsInteract=true;yield return new WaitForSeconds(1.2f);recorder.End();
        // Closed initialization must survive reload; preserve unrelated evidence and manual lamps.
        state.entries.Add(new NotebookEntry{id="E07",title="Retained door review evidence"});state.openFixtures.Add("milk-reading-lamp-off");Save();
        NeedWeather(state.initializedFixtures.Contains("milk-left-door")&&!state.openFixtures.Contains("milk-left-door")&&!state.openFixtures.Contains("milk-right-door"),"Closed door state not stored");
        result.savePreserved=true;
        // Check bindings on both other real houses with the ordinary moving leaf.
        var layout=seamlessShop.layout;player.Place(layout.ToTown(new Vector3(0,.1f,-2.1f)),0);yield return new WaitForSeconds(1.2f);
        NeedWeather(sound.ShelterBlend>.98f,"Closed watch shop not enclosed");yield return UseDoorRay(watch);yield return new WaitForSeconds(1.2f);NeedWeather(sound.ShelterBlend<.02f,"Open watch shop stayed muffled");yield return UseDoorRay(watch);
        var rear=Mechanism("shop-rear-door");player.Place(layout.ToTown(new Vector3(0,.1f,2.6f)),0);yield return UseDoorRay(rear);yield return new WaitForSeconds(1.2f);NeedWeather(sound.ShelterBlend<.02f,"Open rear exit stayed muffled");yield return UseDoorRay(rear);
        var tower=Mechanism("clocktower-door");var roof=tower.GetComponentInParent<WeatherShelter>();
        NeedWeather(roof&&roof.exteriorDoors.Contains(tower),"Tower exterior door not bound to shelter");
        player.Place(roof.transform.TransformPoint(new Vector3(0,.14f,0)),0);yield return new WaitForSeconds(1.2f);NeedWeather(sound.ShelterBlend>.98f,"Closed tower not enclosed");
        yield return UseDoorRay(tower);yield return new WaitForSeconds(1.2f);NeedWeather(sound.ShelterBlend<.02f,"Open tower stayed muffled");yield return UseDoorRay(tower);
        player.Place(new Vector3(0,.12f,-9),0);player.Aim(new Vector3(20,3,18));DayReviewTime(720);cycle.SetReviewWeather(TownWeather.Kind.Rain);dayNightReviewFrozen=false;yield return new WaitForSeconds(1);
        var lamps=DayNightLamp.Active.Where(l=>l.role==DayNightLamp.Role.Exterior).ToArray();
        NeedWeather(lamps.Length>0&&lamps.All(l=>l.Output>.99f)&&lamps.Any(l=>l.source&&l.source.enabled),"Rainy noon failed to light street lamps");
        result.rainLight=cycle.NaturalIllumination;foreach(var lamp in lamps)CheckEmission(lamp);
        recorder.Begin(Path.Combine(captures,"02-weather-street-lamps"));yield return new WaitForSeconds(3);yield return Capture("rain-street-lamps");
        // Exercise the actual Apply path through the complete sensor band, independent of lamps' emitted light.
        var sample=lamps[0];float last=1;
        for(int i=0;i<=100;i++) {float light=.35f+i*.003f;sample.Apply(1,light);NeedWeather(sample.Output<=last+.00001f&&Mathf.Abs(sample.Output-last)<.03f,"Photocell band jumps or brightens in rising natural light");last=sample.Output;}
        cycle.SetReviewWeather(TownWeather.Kind.Clear);yield return new WaitForSeconds(1);
        NeedWeather(lamps.All(l=>l.Output==0&&!l.source.enabled),"Clear noon did not switch street lamps off");result.clearLight=cycle.NaturalIllumination;
        foreach(var lamp in lamps)CheckEmission(lamp);yield return new WaitForSeconds(3);yield return Capture("clear-street-lamps");recorder.End();result.weatherLamps=true;
        player.Place(marker.inside,0);Save();result.passed=true;result.verification="Native macOS Metal player; scripted real controller, ray interactions and listener mix; isolated test saves. No physical keyboard input or blind test.";
        File.WriteAllText(Path.Combine(captures,"dairy-doors-results.json"),JsonUtility.ToJson(result,true));Debug.Log("DAIRY_DOORS_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyDairyDoorsSave() {
        yield return new WaitForSeconds(.6f);var left=Mechanism("milk-left-door");var right=Mechanism("milk-right-door");
        NeedWeather(state.initializedFixtures.Contains(left.fixtureId)&&!left.IsOpen&&!right.IsOpen&&left.OpenFraction==0&&right.OpenFraction==0,"Reload reopened a saved closed entrance");
        NeedWeather(state.entries.Any(e=>e.id=="E07")&&state.openFixtures.Contains("milk-reading-lamp-off"),"Door reload lost existing progress/manual lighting");
        NeedWeather(WeatherShelter.MufflesRain(player.eye.transform.position),"Reload lost closed-room enclosure");
        yield return UseDoorRay(left);yield return new WaitForSeconds(.3f);NeedWeather(!WeatherShelter.MufflesRain(player.eye.transform.position),"Reloaded door no longer opens");
        yield return Capture("dairy-door-save-relaunch");Debug.Log("DAIRY_DOORS_SAVE_RELAUNCH_PASS");Application.Quit();
    }
}
