using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Pre-build consistency checks for the shared D police-station prefab. The street view
// and the interior are the same object, so these compare the built collision/visual
// against the layout contract: every opening is a real aperture, no extra or displaced
// holes exist anywhere along a wall, doors/drawers/lamps/materials match the source.
public static class PoliceChecks {
    [Serializable] class Result {
        public bool passed;
        public int walls, openings, apertureProbes, solidProbes, doors, drawers, lamps, interiorLamps, exteriorLamps, items, seats, shelters, rooms;
        public int e01Copies, e08Copies;
        public string layoutRule, verification;
    }
    public static void Validate(GameObject root,PoliceLayout layout,PoliceBuildManifest manifest) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Police station: "+why);}
        Physics.SyncTransforms();
        var walls=root.transform.Find("Police collision").GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Wall ")).ToArray();
        bool Solid(Vector3 local){var p=root.transform.TransformPoint(local);return walls.Any(c=>c.bounds.Contains(p));}
        Vector3 OnWall(PoliceWall w,float along,float y)=>w.axis=="x"?new Vector3(along,y,w.c):new Vector3(w.c,y,along);
        int aperture=0,solid=0;
        foreach(var o in layout.openings) {
            var w=layout.Wall(o.wall);
            Need(!Solid(OnWall(w,o.at,(o.bottom+o.top)/2)),"opening is blocked or displaced: "+o.id);aperture++;
            Need(!Solid(OnWall(w,o.at-o.width/2+.05f,o.bottom+.05f))&&!Solid(OnWall(w,o.at+o.width/2-.05f,o.top-.05f)),"opening narrower than contract: "+o.id);
        }
        // Dense probe grid: anything outside an authored opening must be wall (rejects extra holes).
        foreach(var w in layout.walls) {
            var own=layout.openings.Where(o=>o.wall==w.id).ToArray();
            for(float a=w.a0+.1f;a<=w.a1-.1f;a+=.25f)for(float y=w.y0+.1f;y<=w.y1-.1f;y+=.3f) {
                if(own.Any(o=>a>o.at-o.width/2-.04f&&a<o.at+o.width/2+.04f&&y>o.bottom-.04f&&y<o.top+.04f))continue;
                Need(Solid(OnWall(w,a,y)),"unexpected aperture in "+w.id+" at "+a.ToString("0.00")+","+y.ToString("0.00"));solid++;
            }
        }
        var doors=root.GetComponentsInChildren<ShopMechanism>();
        int leaves=layout.openings.Sum(o=>o.leaves);
        Need(manifest.leaves.Length==leaves&&doors.Count(d=>d.protectPlayer)==leaves,"door leaves differ from openings");
        Need(doors.Select(d=>d.fixtureId).Distinct().Count()==doors.Length,"duplicate door/drawer fixture IDs");
        Need(doors.Count(d=>!d.protectPlayer)==3&&manifest.drawers.Length==3,"three index drawers");
        var lights=root.GetComponentsInChildren<Light>();
        Need(lights.Length==layout.lamps.Length&&lights.All(l=>l.shadows==LightShadows.None),"lamp count or a new shadow light");
        var lamps=root.GetComponentsInChildren<DayNightLamp>();
        int interior=lamps.Count(l=>l.role==DayNightLamp.Role.Interior),exterior=lamps.Count(l=>l.role==DayNightLamp.Role.Exterior);
        Need(lamps.Where(l=>l.role==DayNightLamp.Role.Interior).All(l=>l.source.renderingLayerMask==(int)BuildSlice.PoliceInteriorLayer),"interior lamp may leak onto the street");
        Need(lamps.All(l=>l.glowingSurfaces.Length==1&&l.glowingSurfaces[0]),"each lamp drives its own bulb");
        var items=root.GetComponentsInChildren<PoliceDocumentItem>();
        int e01=items.Count(i=>i.evidenceId=="E01"),e08=items.Count(i=>i.evidenceId=="E08");
        Need(e01==2&&e08==1&&items.Count(i=>!string.IsNullOrEmpty(i.evidenceId))==3,"only the approved E01/E08 copies carry evidence IDs");
        Need(root.GetComponentsInChildren<PoliceReportDesk>().Length==1,"one stage-report desk");
        var seats=root.GetComponentsInChildren<FurnitureSeat>();
        Need(seats.Length==layout.items.Count(i=>i.kind=="seat")&&seats.Select(s=>s.seatId).Distinct().Count()==seats.Length,"seat IDs");
        Need(root.GetComponentsInChildren<LampSwitch>().Length==layout.items.Count(i=>i.kind=="lampswitch"),"lamp switches");
        var officer=root.GetComponentsInChildren<Interactable>().Where(i=>i.title=="值班警员").ToArray();
        // The placeholder clerk rig and the accepted period cast use different performance components.
        Need(officer.Length==1&&(officer[0].GetComponentInChildren<StationClerkPerformance>()?.speaker=="值班警员"||officer[0].GetComponentInChildren<PeriodCharacterPerformance>()?.speaker=="值班警员"),"duty officer actor");
        var shelters=root.GetComponentsInChildren<WeatherShelter>();
        Need(shelters.Length==layout.shelters.Length&&shelters.Count(s=>s.acousticallyOpen)==2,"rain shelters / open loggias");
        // Agreement with the compiled town map: B10 footprint parts and both doors.
        var b10=TownPlan.Current.Building("B10");
        Need(b10.parts.Length==3&&b10.storeys==2,"compiled TownPlan B10 must be the D U-shape");
        var origin=layout.Origin;var front=layout.Opening("front-door");var court=layout.Opening("court-door");
        Need(Vector2.Distance(new Vector2(front.x+origin.x,0+origin.z),new Vector2(b10.door[0],b10.door[1]))<.01f&&Vector2.Distance(new Vector2(court.x+origin.x,12+origin.z),new Vector2(b10.rearDoor[0],b10.rearDoor[1]))<.01f,"doors differ from the town map");
        var result=new Result{passed=true,walls=walls.Length,openings=layout.openings.Length,apertureProbes=aperture,solidProbes=solid,doors=leaves,drawers=3,
            lamps=lights.Length,interiorLamps=interior,exteriorLamps=exterior,items=items.Length,seats=seats.Length,shelters=shelters.Length,rooms=layout.rooms.Length,e01Copies=e01,e08Copies=e08,
            layoutRule="One shared prefab for street exterior and interior; no second scene shell. Openings are cut from the layout contract that also generated the Blender shell.",
            verification="Editor authoring check only; native rendering, controller walking and saves are verified by --review-police."};
        Directory.CreateDirectory("../output/demo/police-station");
        File.WriteAllText("../output/demo/police-station/authoring-checks.json",JsonUtility.ToJson(result,true));
        Debug.Log("POLICE_AUTHORING_PASS: "+aperture+" apertures, "+solid+" solid wall probes, "+leaves+" doors, "+lights.Length+" lamps");
    }
}
