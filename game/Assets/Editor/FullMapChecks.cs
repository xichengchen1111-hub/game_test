using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class FullMapChecks {
    public static void Validate(TownPlan plan,GameObject geometry) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Full map: "+why);}
        var markers=geometry.GetComponentsInChildren<TownMapBuilding>();
        Need(markers.Length==70&&markers.Select(m=>m.mapId).Distinct().Count()==70,"all 70 properties must appear once");
        Need(markers.Count(m=>m.finished)==3,"finished tower/shop/dairy only");
        foreach(var b in plan.buildings) {
            var marker=markers.Single(m=>m.mapId==b.id);
            Need(Vector3.Distance(marker.door,TownPlan.Point(b.door))<.001f,"door marker drift "+b.id);
            Need(marker.door.x>=plan.bounds[0]&&marker.door.x<=plan.bounds[1]&&marker.door.z>=plan.bounds[2]&&marker.door.z<=plan.bounds[3],"door outside map "+b.id);
            if(!marker.finished)foreach(var part in b.parts) {
                var group=marker.transform.Find(b.id+" "+part.role);var roof=group.Find("Pitched roof").GetComponent<MeshFilter>().sharedMesh.bounds;
                Need(Mathf.Abs(roof.size.x-part.rect[2])<.001f&&Mathf.Abs(roof.size.z-part.rect[3])<.001f,"Roof asset reused with another property's dimensions: "+b.id);
            }
        }
        var shop=geometry.GetComponentInChildren<BuildingInstance>();var layout=shop.layout;
        Need(Vector3.Distance(shop.transform.position,TownPlan.Point(plan.Building("B02").parts[0].rect,0))<.001f,"shop centre");
        Need(Quaternion.Angle(shop.transform.rotation,Quaternion.Euler(0,TownPlan.Yaw(plan.Building("B02").front),0))<.001f,"shop door orientation");
        Need(Vector3.Distance(layout.ToTown(layout.OpeningCentre(layout.Opening(layout.entranceId)))-Vector3.up*1.45f,TownPlan.Point(plan.Building("B02").door,0))<.16f,"shop physical entrance to plan");
        Need(geometry.GetComponentInChildren<ClockTowerInstance>().transform.position==Vector3.zero,"tower origin");
        Need(geometry.GetComponentsInChildren<StreetSurfaceLayout>().Length==11,"all street profiles");
        var shadowLights=geometry.GetComponentsInChildren<Light>().Where(l=>l.shadows!=LightShadows.None).ToArray();
        Need(shadowLights.Length==3,"preserve upstream's two shop and one low-resolution machinery shadow lamps: "+string.Join(", ",shadowLights.Select(l=>l.name+"/"+l.type)));
        Need(geometry.GetComponentsInChildren<WeatherShelter>().Length>70,"finished and placeholder roofs shelter rain");
        string directory="../output/demo/full-map";Directory.CreateDirectory(directory);
        File.WriteAllText(directory+"/authoring-checks.json",JsonUtility.ToJson(new Result{passed=true,mapVersion=plan.version,source=plan.sourceSha256,properties=markers.Length,finished=3,roads=11,lights=geometry.GetComponentsInChildren<Light>().Length,shadows=3},true));
        Debug.Log("FULL_MAP_AUTHORING_PASS: 70 property markers; finished proportions, origin/doors, roads and light budget");
    }
    [Serializable] class Result {public bool passed;public int mapVersion,properties,finished,roads,lights,shadows;public string source;}
}
