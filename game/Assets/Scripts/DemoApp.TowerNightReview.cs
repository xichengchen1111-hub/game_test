using System.Collections;
using UnityEngine;
using T = ClockTowerInteriorLayout;

// Fixed comparison viewpoints for the night clock-tower art pass (isolated review save, staged time).
// Staged views only: the stair route itself is exercised by --review-clocktower.
public partial class DemoApp {
    IEnumerator ReviewTowerNightGallery() {
        const double Dusk=1095,Evening=1150,Night=1290,Midnight=30,Noon=720;
        var clear=TownWeather.Kind.Clear;var overcast=TownWeather.Kind.Overcast;var rain=TownWeather.Kind.Rain;
        Vector3 mid=new Vector3(0,T.ExteriorHeight*.47f,0),dial=new Vector3(0,T.DialHeight,-T.DialPlaneDistance);
        // Tower root is the town origin; layout points are the review route's own standing places.
        var layout=FindFirstObjectByType<ClockTowerInstance>().GetComponent<ClockTowerInteriorLayout>();
        Vector3 middle=layout.middleStandingPoint,machine=layout.machineStandingPoint;
        var shots=new (string name,double minutes,TownWeather.Kind weather,Vector3 feet,Vector3 aim)[] {
            ("front",Evening,overcast,new Vector3(0,.1f,-19),mid),
            ("corner",Evening,overcast,new Vector3(5,.1f,-17),mid),
            ("door",Evening,overcast,new Vector3(1.1f,.1f,-5.2f),new Vector3(0,1.9f,-1.8f)),
            ("look-up",Evening,overcast,new Vector3(2.6f,.1f,-4.2f),dial),
            ("alley-south",Evening,overcast,new Vector3(0,.1f,-28),new Vector3(0,9,0)),
            ("street-north",Evening,overcast,new Vector3(26,.1f,20),new Vector3(0,10,0)),
            ("street-far",Evening,overcast,new Vector3(-44,.1f,20),new Vector3(0,11,0)),
            ("rain-front",Night,rain,new Vector3(0,.1f,-19),mid),
            ("rain-corner",Night,rain,new Vector3(-6,.1f,-14),mid),
            ("midnight-front",Midnight,overcast,new Vector3(3,.1f,-21),mid),
            ("dusk-front",Dusk,rain,new Vector3(0,.1f,-19),mid),
            ("noon-front",Noon,clear,new Vector3(0,.1f,-19),mid),
            ("inside-ground",Evening,overcast,new Vector3(0,T.GroundFloor,-1.40f),new Vector3(.3f,3.2f,.9f)),
            ("inside-middle",Evening,overcast,middle,new Vector3(.9f,T.MiddleFloor+1.1f,-.3f)),
            ("machine-room",Evening,overcast,machine,layout.mechanismLookPoint),
            ("machine-dial",Evening,overcast,machine+new Vector3(.5f,0,.2f),new Vector3(0,T.DialHeight,-T.ChamberInteriorWidth/2)),
            // Clear of the first tread (which starts at -90 degrees): stand on the open floor by the door.
            ("threshold-inside",Evening,overcast,new Vector3(-.05f,T.GroundFloor,-1.02f),new Vector3(0,T.GroundFloor,-1.75f)),
            ("threshold-grazing",Evening,overcast,new Vector3(-.55f,T.GroundFloor,-.95f),new Vector3(.25f,T.GroundFloor,-1.55f)),
            ("threshold-noon",Noon,clear,new Vector3(-.30f,T.GroundFloor,-1.15f),new Vector3(.20f,T.GroundFloor,-1.70f)),
            ("station-exit",Evening,overcast,new Vector3(188,.12f,50.9f),new Vector3(188,.1f,57.5f)),
            ("station-exit-rain",Night,rain,new Vector3(187.3f,.12f,51.2f),new Vector3(188.6f,.1f,60.5f)),
        };
        yield return new WaitForSeconds(.75f);
        foreach(var shot in shots) {
            DayReviewTime(shot.minutes);DayNightCycle.Instance.SetReviewWeather(shot.weather);
            player.Place(shot.feet,0);player.Aim(shot.aim);
            yield return new WaitForSeconds(.9f);
            yield return Capture("tower-night-"+shot.name);
        }
        Debug.Log("TOWER_NIGHT_GALLERY_PASS: "+shots.Length+" staged views");
        Application.Quit();
    }
    // Daytime look comparison across the town's finished buildings and typical streets.
    IEnumerator ReviewDayLookGallery() {
        const double Overcast=785,Clear=875,Shower=690,Morning=450;
        var overcast=TownWeather.Kind.Overcast;var clear=TownWeather.Kind.Clear;var rain=TownWeather.Kind.Rain;
        var views=new (string name,Vector3 feet,Vector3 aim)[] {
            ("plaza-wide",new Vector3(-9,.1f,-11.5f),new Vector3(3,5,5)),
            ("tower-close",new Vector3(4,.1f,-7),new Vector3(0,7,0)),
            ("tower-base",new Vector3(1.6f,.1f,-4.6f),new Vector3(0,1.3f,-1.8f)),
            ("main-street",new Vector3(-60,.1f,15),new Vector3(-25,4,24)),
            ("clock-shop",new Vector3(-33,.1f,21.5f),new Vector3(-35,2.6f,27.8f)),
            ("dairy",new Vector3(-140,.1f,15),new Vector3(-144,2.5f,9)),
            ("station-front",new Vector3(176,.12f,31),new Vector3(188,4,46)),
            ("south-houses",new Vector3(-40,.1f,-68.5f),new Vector3(-47.5f,3,-60)),
        };
        var shots=new System.Collections.Generic.List<(string name,double minutes,TownWeather.Kind weather,Vector3 feet,Vector3 aim)>();
        foreach(var v in views)shots.Add(("overcast-"+v.name,Overcast,overcast,v.feet,v.aim));
        foreach(var v in new[]{views[0],views[1],views[3]}) {
            shots.Add(("clear-"+v.name,Clear,clear,v.feet,v.aim));
            shots.Add(("rain-"+v.name,Shower,rain,v.feet,v.aim));
            shots.Add(("morning-"+v.name,Morning,overcast,v.feet,v.aim));
        }
        // Looking toward the sun itself (aim derived from the same clock-driven direction).
        var plaza=new Vector3(-9,.1f,-11.5f);
        shots.Add(("sun-overcast",Overcast,overcast,plaza,Vector3.zero));
        shots.Add(("sun-morning",Morning,overcast,plaza,Vector3.zero));
        shots.Add(("sun-clear",Clear,clear,plaza,Vector3.zero));
        shots.Add(("sun-rain",Shower,rain,plaza,Vector3.zero));
        yield return new WaitForSeconds(.75f);
        foreach(var shot in shots) {
            DayReviewTime(shot.minutes);DayNightCycle.Instance.SetReviewWeather(shot.weather);
            player.Place(shot.feet,0);
            var aim=shot.name.StartsWith("sun-")?player.eye.transform.position+DayNightCycle.SunDirection(Minutes)*40:shot.aim;
            player.Aim(aim);
            yield return new WaitForSeconds(.9f);
            yield return Capture("day-"+shot.name);
        }
        Debug.Log("DAY_LOOK_GALLERY_PASS: "+shots.Count+" staged views");
        Application.Quit();
    }
}
