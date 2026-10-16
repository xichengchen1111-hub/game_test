using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Pre-build consistency checks for the shared A inn prefab (B08). The street view and the interior
// are the same object, so these compare the built collision/visual against the layout contract:
// every opening is a real aperture, no extra or displaced holes anywhere along a wall, doors, lamps,
// records and the tilting register lamp match the source, the hotel car's stand and its run to the
// street stay clear, and the footprint/doors match the compiled town map.
public static class HotelChecks {
    [Serializable] class Result {
        public bool passed;
        public int walls, openings, apertureProbes, solidProbes, doors, lamps, interiorLamps, exteriorLamps, documents, seats, shelters, rooms, placeholders;
        public string[] records;
        public string layoutRule, characters, verification;
    }
    public static void Validate(GameObject root,HotelLayout layout,int manifestLeaves) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Hotel: "+why);}
        Physics.SyncTransforms();
        var collision=root.transform.Find("Hotel collision").GetComponentsInChildren<BoxCollider>();
        var walls=collision.Where(c=>c.name.StartsWith("Wall ")).ToArray();
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
        Need(manifestLeaves==leaves&&mechanisms.Length==leaves&&mechanisms.All(d=>d.protectPlayer),"door leaves differ from openings");
        Need(mechanisms.Select(d=>d.fixtureId).Distinct().Count()==mechanisms.Length,"duplicate door fixture IDs");
        Need(mechanisms.Where(d=>d.fixtureId.StartsWith("hotel-front-door")).All(d=>d.startsOpen)&&!mechanisms.Single(d=>d.fixtureId=="hotel-back-door").startsOpen,"door initial states");
        var lights=root.GetComponentsInChildren<Light>(true);
        Need(lights.Length==layout.lamps.Length+1&&lights.All(l=>l.shadows==LightShadows.None),"lamp count (plus the raking light) or a new shadow light");
        var lamps=root.GetComponentsInChildren<DayNightLamp>();
        int interior=lamps.Count(l=>l.role==DayNightLamp.Role.Interior),exterior=lamps.Count(l=>l.role==DayNightLamp.Role.Exterior);
        Need(exterior==layout.lamps.Count(l=>l.role=="Exterior")&&lamps.Where(l=>l.role==DayNightLamp.Role.Interior).All(l=>l.source.renderingLayerMask==(int)BuildSlice.PoliceInteriorLayer),"interior lamp may leak onto the street");
        Need(lamps.All(l=>l.glowingSurfaces.Length==1&&l.glowingSurfaces[0]),"each lamp drives its own bulb");
        var docs=root.GetComponentsInChildren<PoliceDocumentItem>(true);
        var records=docs.Where(d=>!string.IsNullOrEmpty(d.evidenceId)).Select(d=>d.evidenceId).OrderBy(x=>x).ToArray();
        Need(records.SequenceEqual(new[]{"I01","I02","I03"}),"exactly one I01, I02 and I03 record (I04 comes from the porter)");
        Need(docs.Where(d=>!string.IsNullOrEmpty(d.evidenceId)).All(d=>d.GetComponentInChildren<EvidencePhysical>(true)&&d.illustration),"records are physical typeset sheets");
        Need(!docs.Any(d=>d.evidenceId!=null&&d.evidenceId.StartsWith("E")),"the inn grants no core E-series evidence");
        var raking=root.GetComponentsInChildren<RakingLamp>(true).Single();
        Need(raking.rakedPage&&!raking.rakedPage.activeSelf&&raking.rakedPage.GetComponent<PoliceDocumentItem>().evidenceId=="I02","I02 waits under the lamp, hidden until the shade is lowered");
        Need(raking.plainPage&&raking.plainPage.activeSelf&&string.IsNullOrEmpty(raking.plainPage.GetComponent<PoliceDocumentItem>().evidenceId),"the next page is plain while the shade is up");
        Need(raking.head&&raking.head.GetComponentInChildren<Light>()&&raking.head.GetComponentsInChildren<MeshRenderer>().Length>=2,"lamp head carries its shade, bulb and light");
        var seats=root.GetComponentsInChildren<FurnitureSeat>();
        Need(seats.Length==layout.items.Count(i=>i.kind=="seat")&&seats.Select(s=>s.seatId).Distinct().Count()==seats.Length,"seat IDs");
        Need(root.GetComponentsInChildren<LampSwitch>().Length==layout.items.Count(i=>i.kind=="lampswitch"),"lamp switches");
        var actors=root.GetComponentsInChildren<HotelActor>();
        Need(actors.Length==2&&actors.Any(a=>a.role=="porter")&&actors.Any(a=>a.role=="keeper"),"porter and landlady talk targets");
        Need(actors.All(a=>a.name.Contains("占位")&&!a.GetComponentInChildren<Animator>()),"characters stay placeholders (a teammate makes the people)");
        var shelters=root.GetComponentsInChildren<WeatherShelter>();
        Need(shelters.Length==layout.shelters.Length&&shelters.Count(s=>s.acousticallyOpen)==2,"rain shelters / open porch and carport");
        // The hotel car's stand (world 169.5, 2.0) and its lane to the street: no solid collider.
        var car=layout.car;
        var stand=new Bounds(new Vector3(car.x,1f,car.z+(car.front-car.back)/2),new Vector3(car.halfW*2+1.9f,1.6f,car.back+car.front));
        var lane=new Bounds(new Vector3(car.x,1f,(car.z+40)/2),new Vector3(2.6f,1.6f,40-car.z));
        foreach(var c in root.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger&&c.enabled)) {
            var b=c.bounds;b.center=root.transform.InverseTransformPoint(b.center);
            Need(!b.Intersects(stand)&&!b.Intersects(lane),"collider "+c.name+" intrudes on the hotel car's stand or its way out");
        }
        var b08=TownPlan.Current.Building("B08");
        Need(b08.parts.Length==2&&b08.storeys==2,"compiled TownPlan B08 must be the A front range and service wing");
        var origin=layout.Origin;var front=layout.Opening("front-door");var back=layout.Opening("back-door");
        Need(Vector2.Distance(new Vector2(front.x+origin.x,layout.front.z1+origin.z),new Vector2(b08.door[0],b08.door[1]))<.01f&&
             Vector2.Distance(new Vector2(back.x+origin.x,layout.service.z0+origin.z),new Vector2(b08.rearDoor[0],b08.rearDoor[1]))<.01f,"doors differ from the town map");
        var result=new Result{passed=true,walls=walls.Length,openings=layout.openings.Length,apertureProbes=aperture,solidProbes=solid,doors=leaves,
            lamps=lights.Length,interiorLamps=interior,exteriorLamps=exterior,documents=docs.Length,seats=seats.Length,shelters=shelters.Length,rooms=layout.rooms.Length,
            placeholders=actors.Length,records=records,
            layoutRule="One shared prefab for street exterior and interior; no second scene shell. Openings are cut from the layout contract that also generated the Blender shell.",
            characters="Porter and landlady are grey standing markers with their conversations; the figures come from a teammate (user, 2026-10-16).",
            verification="Editor authoring check only; native rendering, controller walking and saves are verified by --review-hotel."};
        Directory.CreateDirectory("../output/demo/hotel");
        File.WriteAllText("../output/demo/hotel/authoring-checks.json",JsonUtility.ToJson(result,true));
        Debug.Log("HOTEL_AUTHORING_PASS: "+aperture+" apertures, "+solid+" solid wall probes, "+leaves+" doors, "+lights.Length+" lamps");
    }
}
