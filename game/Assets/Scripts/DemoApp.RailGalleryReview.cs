using System.Collections;
using UnityEngine;

// Fixed comparison viewpoints for the station/train art pass (isolated review save, staged time).
public partial class DemoApp {
    // Diagnostic: lamp outputs and the emission actually applied to each bound glowing surface.
    void LogRailLamps() {
        var block=new MaterialPropertyBlock();
        foreach(var lamp in DayNightLamp.Active) {
            if(lamp.glowingSurfaces==null||lamp.glowingSurfaces.Length==0)continue;
            foreach(var r in lamp.glowingSurfaces) {
                if(!r)continue;r.GetPropertyBlock(block,0);
                Debug.Log("RAIL_LAMP_STATE: "+lamp.name+" role="+lamp.role+" output="+lamp.Output.ToString("0.00")+" source="+(lamp.source&&lamp.source.enabled)+" surface="+r.name+" emission="+block.GetColor("_EmissionColor")+" visible="+r.isVisible+" enabled="+r.enabled);
            }
        }
    }
    IEnumerator ReviewRailGallery() {
        const double Dusk=1065,Noon=720,Night=1260;
        var clear=TownWeather.Kind.Clear;var rain=TownWeather.Kind.Rain;
        var shots=new (string name,double minutes,TownWeather.Kind weather,Vector3 feet,Vector3 aim)[] {
            ("front-wide",Dusk,clear,new Vector3(170,.12f,31),new Vector3(188,4.5f,46)),
            ("front-entrance",Dusk,clear,new Vector3(188,.12f,33),new Vector3(188,4.6f,40)),
            ("front-oblique",Dusk,clear,new Vector3(166,.12f,35),new Vector3(181,3.6f,40)),
            ("window-detail",Dusk,clear,new Vector3(180,.12f,37),new Vector3(180,3.6f,40)),
            ("west-wing",Dusk,clear,new Vector3(160,.12f,38),new Vector3(169,3,46)),
            ("rear-way",Dusk,clear,new Vector3(181.5f,.12f,57.5f),new Vector3(188,3,52)),
            ("concourse-barrier",Dusk,clear,new Vector3(188,.12f,54.5f),new Vector3(188,2.6f,63.6f)),
            ("barrier-close",Dusk,clear,new Vector3(188,.12f,60.8f),new Vector3(188,2.95f,63.6f)),
            ("barrier-platform-side",Dusk,clear,new Vector3(188.5f,.42f,66.5f),new Vector3(188,2.8f,63.6f)),
            ("platform-east",Dusk,clear,new Vector3(179,.42f,66),new Vector3(205,2.2f,70)),
            ("platform-west-endscreen",Dusk,clear,new Vector3(198,.42f,68),new Vector3(154,6.5f,66)),
            ("shed-up",Dusk,clear,new Vector3(188,.42f,68),new Vector3(188,9,70.5f)),
            ("loco-three-quarter",Dusk,clear,new Vector3(211,.42f,69),new Vector3(201,1.8f,73)),
            ("coach-exterior",Dusk,clear,new Vector3(183,.42f,69.3f),new Vector3(183,2.0f,73)),
            ("wheels-on-rails",Dusk,clear,new Vector3(197.5f,.42f,70.6f),new Vector3(197.5f,.5f,72.1f)),
            ("hall-west",Dusk,clear,new Vector3(194,.12f,42.5f),new Vector3(176,3,48)),
            ("hall-east",Dusk,clear,new Vector3(182,.12f,42.5f),new Vector3(200,3,48)),
            ("hall-ceiling",Dusk,clear,new Vector3(188,.12f,41.5f),new Vector3(188,7,49)),
            ("hall-stove",Dusk,clear,new Vector3(177.5f,.12f,50.6f),new Vector3(174.9f,.9f,50.35f)),
            ("luggage-room",Dusk,clear,new Vector3(172.4f,.12f,45.4f),new Vector3(167,1.4f,48)),
            ("duty-room",Dusk,clear,new Vector3(203.6f,.12f,45.4f),new Vector3(209,1.2f,46.5f)),
            ("coach-aisle",Dusk,clear,new Vector3(185.6f,1.05f,73),new Vector3(176,2.25f,73)),
            ("coach-window",Dusk,clear,new Vector3(181.9f,1.05f,72.75f),new Vector3(181.9f,2.1f,71.5f)),
            ("coach-gangway",Dusk,clear,new Vector3(174.6f,1.05f,73),new Vector3(170,2.0f,73)),
            ("day-front",Noon,clear,new Vector3(170,.12f,31),new Vector3(188,4.5f,46)),
            ("day-shed",Noon,clear,new Vector3(179,.42f,66),new Vector3(205,5,70)),
            ("day-hall",Noon,clear,new Vector3(194,.12f,42.5f),new Vector3(176,3,48)),
            ("night-rain-front",Night,rain,new Vector3(176,.12f,31),new Vector3(188,4,46)),
            ("night-rain-platform",Night,rain,new Vector3(179,.42f,66),new Vector3(205,2.5f,70)),
            ("night-hall",Night,rain,new Vector3(194,.12f,42.5f),new Vector3(176,3,48)),
            ("night-coach",Night,rain,new Vector3(185.6f,1.05f,73),new Vector3(176,2.25f,73)),
        };
        yield return new WaitForSeconds(.75f);
        foreach(var shot in shots) {
            DayReviewTime(shot.minutes);DayNightCycle.Instance.SetReviewWeather(shot.weather);
            player.Place(shot.feet,0);player.Aim(shot.aim);
            yield return new WaitForSeconds(.6f);
            yield return Capture("gallery-"+shot.name);
            if(shot.name=="night-rain-platform")LogRailLamps();
        }
        Debug.Log("RAIL_GALLERY_PASS: "+shots.Length+" staged views");
        Application.Quit();
    }
}
