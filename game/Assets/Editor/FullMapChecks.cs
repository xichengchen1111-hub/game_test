using System;
using System.IO;
using System.Linq;
using UnityEngine;

public static class FullMapChecks {
    public static void Validate(TownPlan plan,GameObject geometry) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Full map: "+why);}
        foreach(var surface in plan.surfaces)for(int i=0;i<surface.vertices.Length;i+=9) {
            var v=surface.vertices;var a=new Vector3(v[i],v[i+1],v[i+2]);var b=new Vector3(v[i+3],v[i+4],v[i+5]);var c=new Vector3(v[i+6],v[i+7],v[i+8]);
            Need(Vector3.Cross(b-a,c-a).y>0,"inverted road/footway triangle: "+surface.name);
        }
        var markers=geometry.GetComponentsInChildren<TownMapBuilding>();
        Need(markers.Length==70&&markers.Select(m=>m.mapId).Distinct().Count()==70,"all 70 properties must appear once");
        Need(markers.Count(m=>m.finished)==6,"finished tower/shop/dairy/station/police/home");
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
        // A finished prefab's own ground slab owns its footprint: no street triangle may lie
        // coplanar beneath it (2026-10-11, rear footpath z-fighting the station concourse).
        foreach(var slab in geometry.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="StationConcourseFlags"||r.name=="Ext_Court")) {
            var b=slab.bounds;int coplanar=0;
            foreach(var surface in plan.surfaces)for(int i=0;i+8<surface.vertices.Length;i+=9) {
                float x=(surface.vertices[i]+surface.vertices[i+3]+surface.vertices[i+6])/3,y=surface.vertices[i+1],z=(surface.vertices[i+2]+surface.vertices[i+5]+surface.vertices[i+8])/3;
                if(x>b.min.x&&x<b.max.x&&z>b.min.z&&z<b.max.z&&y>b.max.y-.06f)coplanar++;
            }
            Need(coplanar==0,"street surfaces lie coplanar under "+slab.name+": "+coplanar+" triangles");
        }
        Need(geometry.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="StationConcourseFlags"),"station concourse slab present for coplanarity check");
        Need(geometry.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="Ext_Court"),"police court slab present for coplanarity check");
        string directory="../output/demo/full-map";Directory.CreateDirectory(directory);
        File.WriteAllText(directory+"/authoring-checks.json",JsonUtility.ToJson(new Result{passed=true,mapVersion=plan.version,source=plan.sourceSha256,properties=markers.Length,finished=6,roads=11,lights=geometry.GetComponentsInChildren<Light>().Length,shadows=3},true));
        Debug.Log("FULL_MAP_AUTHORING_PASS: 70 property markers; finished proportions, origin/doors, roads and light budget");
    }
    [Serializable] class Result {public bool passed;public int mapVersion,properties,finished,roads,lights,shadows;public string source;}
}
