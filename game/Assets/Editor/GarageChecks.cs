using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Pre-build consistency checks for the shared E garage prefab (B07). The street view and the
// interior are the same object, so these compare the built collision/visual against the layout
// contract: every opening is a real aperture, no extra or displaced holes anywhere along a wall,
// doors/drawer/lamps/records match the source, and the footprint/doors match the compiled town map.
public static class GarageChecks {
    [Serializable] class Result {
        public bool passed;
        public int walls, openings, apertureProbes, solidProbes, doors, drawers, lamps, interiorLamps, exteriorLamps, documents, seats, shelters, rooms;
        public string[] records;
        public string layoutRule, verification;
    }
    public static void Validate(GameObject root,GarageLayout layout,int manifestLeaves,int manifestDrawers) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Repair garage: "+why);}
        Physics.SyncTransforms();
        var walls=root.transform.Find("Garage collision").GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Wall ")).ToArray();
        bool Solid(Vector3 local){var p=root.transform.TransformPoint(local);return walls.Any(c=>c.bounds.Contains(p));}
        Vector3 OnWall(PoliceWall w,float along,float y)=>w.axis=="x"?new Vector3(along,y,w.c):new Vector3(w.c,y,along);
        int aperture=0,solid=0;
        foreach(var o in layout.openings) {
            var w=layout.Wall(o.wall);
            Need(!Solid(OnWall(w,o.at,(o.bottom+o.top)/2)),"opening is blocked or displaced: "+o.id);aperture++;
            Need(!Solid(OnWall(w,o.at-o.width/2+.05f,o.bottom+.05f))&&!Solid(OnWall(w,o.at+o.width/2-.05f,o.top-.05f)),"opening narrower than contract: "+o.id);
        }
        foreach(var w in layout.walls) {
            var own=layout.openings.Where(o=>o.wall==w.id).ToArray();
            for(float a=w.a0+.1f;a<=w.a1-.1f;a+=.25f)for(float y=w.y0+.1f;y<=w.y1-.1f;y+=.3f) {
                if(own.Any(o=>a>o.at-o.width/2-.04f&&a<o.at+o.width/2+.04f&&y>o.bottom-.04f&&y<o.top+.04f))continue;
                Need(Solid(OnWall(w,a,y)),"unexpected aperture in "+w.id+" at "+a.ToString("0.00")+","+y.ToString("0.00"));solid++;
            }
        }
        var mechanisms=root.GetComponentsInChildren<ShopMechanism>();
        int leaves=layout.openings.Sum(o=>o.leaves);
        Need(manifestLeaves==leaves&&mechanisms.Count(d=>d.protectPlayer)==leaves,"door leaves differ from openings");
        Need(mechanisms.Select(d=>d.fixtureId).Distinct().Count()==mechanisms.Length,"duplicate door/drawer fixture IDs");
        Need(mechanisms.Count(d=>!d.protectPlayer)==1&&manifestDrawers==1,"one parts drawer");
        Need(mechanisms.Where(d=>d.fixtureId.StartsWith("garage-carriage-door")).All(d=>d.startsOpen)&&mechanisms.Single(d=>d.fixtureId=="garage-office-door").startsOpen==false,"door initial states");
        var lights=root.GetComponentsInChildren<Light>();
        Need(lights.Length==layout.lamps.Length&&lights.All(l=>l.shadows==LightShadows.None),"lamp count or a new shadow light");
        var lamps=root.GetComponentsInChildren<DayNightLamp>();
        int interior=lamps.Count(l=>l.role==DayNightLamp.Role.Interior),exterior=lamps.Count(l=>l.role==DayNightLamp.Role.Exterior);
        Need(exterior==3&&lamps.Where(l=>l.role==DayNightLamp.Role.Interior).All(l=>l.source.renderingLayerMask==(int)BuildSlice.PoliceInteriorLayer),"interior lamp may leak onto the street");
        Need(lamps.All(l=>l.glowingSurfaces.Length==1&&l.glowingSurfaces[0]),"each lamp drives its own bulb");
        var docs=root.GetComponentsInChildren<PoliceDocumentItem>();
        var records=docs.Where(d=>!string.IsNullOrEmpty(d.evidenceId)).Select(d=>d.evidenceId).OrderBy(x=>x).ToArray();
        Need(records.SequenceEqual(new[]{"W01","W02"}),"exactly one W01 and one W02 record");
        Need(docs.Where(d=>!string.IsNullOrEmpty(d.evidenceId)).All(d=>d.GetComponentInChildren<EvidencePhysical>()&&d.illustration),"records are physical typeset sheets");
        Need(!docs.Any(d=>d.evidenceId!=null&&d.evidenceId.StartsWith("E")),"the garage grants no core E-series evidence");
        var seats=root.GetComponentsInChildren<FurnitureSeat>();
        Need(seats.Length==layout.items.Count(i=>i.kind=="seat")&&seats.Select(s=>s.seatId).Distinct().Count()==seats.Length,"seat IDs");
        Need(root.GetComponentsInChildren<LampSwitch>().Length==layout.items.Count(i=>i.kind=="lampswitch"),"lamp switches");
        var mechanic=root.GetComponentsInChildren<Interactable>().Where(i=>i.title=="修车匠").ToArray();
        Need(mechanic.Length==1&&mechanic[0].GetComponentInChildren<StationClerkPerformance>()?.speaker=="修车匠","mechanic actor");
        var shelters=root.GetComponentsInChildren<WeatherShelter>();
        Need(shelters.Length==layout.shelters.Length&&shelters.Count(s=>s.acousticallyOpen)==2,"rain shelters / open shed and awning");
        var b07=TownPlan.Current.Building("B07");
        Need(b07.parts.Length==2&&b07.storeys==1,"compiled TownPlan B07 must be the E office range and stone hall");
        var origin=layout.Origin;var front=layout.Opening("office-door");var rear=layout.Opening("rear-door");
        Need(Vector2.Distance(new Vector2(front.x+origin.x,layout.office.z0+origin.z),new Vector2(b07.door[0],b07.door[1]))<.01f&&
             Vector2.Distance(new Vector2(rear.x+origin.x,layout.hall.z1+origin.z),new Vector2(b07.rearDoor[0],b07.rearDoor[1]))<.01f,"doors differ from the town map");
        var result=new Result{passed=true,walls=walls.Length,openings=layout.openings.Length,apertureProbes=aperture,solidProbes=solid,doors=leaves,drawers=1,
            lamps=lights.Length,interiorLamps=interior,exteriorLamps=exterior,documents=docs.Length,seats=seats.Length,shelters=shelters.Length,rooms=layout.rooms.Length,records=records,
            layoutRule="One shared prefab for street exterior and interior; no second scene shell. Openings are cut from the layout contract that also generated the Blender shell.",
            verification="Editor authoring check only; native rendering, controller walking and saves are verified by --review-garage."};
        Directory.CreateDirectory("../output/demo/garage");
        File.WriteAllText("../output/demo/garage/authoring-checks.json",JsonUtility.ToJson(result,true));
        Debug.Log("GARAGE_AUTHORING_PASS: "+aperture+" apertures, "+solid+" solid wall probes, "+leaves+" doors, "+lights.Length+" lamps");
    }
}
