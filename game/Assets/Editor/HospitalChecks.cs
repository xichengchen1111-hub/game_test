using System;
using System.IO;
using System.Linq;
using UnityEngine;

// Pre-build consistency checks for the shared hospital prefab. The street view and the
// interior are the same object, so these compare the built collision against the layout
// contract: every opening is a real aperture, nothing else is, and doors, lamps, seats and
// the town map agree with the source.
public static class HospitalChecks {
    [Serializable] class Result {
        public bool passed;
        public int walls, openings, apertureProbes, solidProbes, doors, lamps, interiorLamps, exteriorLamps, seats, shelters, rooms, beds, people;
        public string layoutRule, verification;
    }
    public static void Validate(GameObject root,PoliceLayout layout,PoliceBuildManifest manifest) {
        void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Hospital: "+why);}
        Physics.SyncTransforms();
        var walls=root.transform.Find("Hospital collision").GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Wall ")).ToArray();
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
        Need(manifest.leaves.Length==leaves&&doors.Count(d=>d.protectPlayer)==leaves&&doors.Length==leaves,"door leaves differ from openings");
        Need(doors.Select(d=>d.fixtureId).Distinct().Count()==doors.Length&&doors.All(d=>d.fixtureId.StartsWith("hospital-")),"door fixture IDs");
        var lights=root.GetComponentsInChildren<Light>();
        Need(lights.Length==layout.lamps.Length&&lights.All(l=>l.shadows==LightShadows.None),"lamp count or a new shadow light");
        var lamps=root.GetComponentsInChildren<DayNightLamp>();
        int interior=lamps.Count(l=>l.role==DayNightLamp.Role.Interior),exterior=lamps.Count(l=>l.role==DayNightLamp.Role.Exterior);
        Need(lamps.Where(l=>l.role==DayNightLamp.Role.Interior).All(l=>l.source.renderingLayerMask==(int)BuildSlice.HospitalInteriorLayer),"interior lamp may leak onto the street");
        Need(lamps.All(l=>l.glowingSurfaces.Length==1&&l.glowingSurfaces[0]),"each lamp drives its own bulb");
        var seats=root.GetComponentsInChildren<FurnitureSeat>();
        Need(seats.Length==layout.items.Count(i=>i.kind=="seat")&&seats.Select(s=>s.seatId).Distinct().Count()==seats.Length,"seat IDs");
        Need(root.GetComponentsInChildren<LampSwitch>().Length==layout.items.Count(i=>i.kind=="lampswitch"),"lamp switches");
        // The story needs its three people where the contract puts them, each exactly once.
        var people=root.GetComponentsInChildren<HospitalActor>();
        foreach(string role in new[]{"porter","child","sister"})Need(people.Count(p=>p.role==role)==1,"one "+role);
        // Everyone else is there only with an approved figure of their own, each at most once, and nobody without lines.
        var others=root.GetComponentsInChildren<HospitalPerson>();var spots=layout.items.Where(i=>i.kind=="person").ToArray();
        Need(others.Select(p=>p.personId).Distinct().Count()==others.Length&&others.All(p=>spots.Any(s=>s.id==p.personId)&&p.remarks!=null&&p.remarks.Length>0),"hospital people differ from the contract");
        Need(others.Length==spots.Count(s=>HospitalCastAuthoring.Ready(s.doc)),"an approved hospital figure is missing, or an unapproved one is shown");
        // Covers shaped over a body are shown exactly where a figure lies or sits in the bed.
        var covers=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Furn_")&&t.name.EndsWith("-occupied")).ToArray();
        Need(covers.Length==1+spots.Count(s=>s.target=="propped"||s.target=="lying"),"occupied beds differ from the contract");
        Need(covers.Count(c=>c.gameObject.activeSelf)==(HospitalCastAuthoring.Ready("Child")?1:0)+others.Count(p=>p.GetComponent<HospitalBedPose>()&&p.GetComponent<HospitalBedPose>().mode!=HospitalBedPose.Mode.SittingOnSeat),"bed covers shown without their occupant");
        // Records: each placed once, with its paper, and only those the contract lists (hospital_1931 in docs/mvp-evidence.json).
        var records=root.GetComponentsInChildren<Interactable>().Where(i=>!string.IsNullOrEmpty(i.evidenceId)).ToArray();
        Need(records.Select(r=>r.evidenceId).OrderBy(x=>x).SequenceEqual(new[]{"Y01","Y02","Y03"})&&records.All(r=>r.illustration&&r.GetComponent<Collider>()),"hospital records Y01-Y03 placed once each with their papers");
        Need(records.Single(r=>r.evidenceId=="Y03").reverse,"the drawing has its back");
        var shelters=root.GetComponentsInChildren<WeatherShelter>();
        Need(shelters.Length==layout.shelters.Length&&shelters.Count(s=>s.acousticallyOpen)==1,"rain shelters / open door hood");
        int beds=layout.furniture.Count(f=>f.kind=="wardbed");
        Need(beds==22&&layout.furniture.Count(f=>f.kind=="wardbed"&&f.extra=="child")==1,"ward beds and the child's bed");
        // Agreement with the compiled town map: B11 footprint parts, storey count and both doors.
        var b11=TownPlan.Current.Building("B11");
        Need(b11.parts.Length==3&&b11.storeys==1,"compiled TownPlan B11 must be the single-storey hospital");
        var origin=layout.Origin;var front=layout.Opening("front-door");var court=layout.Opening("court-door");
        Need(Vector2.Distance(new Vector2(front.x+origin.x,0+origin.z),new Vector2(b11.door[0],b11.door[1]))<.01f&&Vector2.Distance(new Vector2(court.x+origin.x,12+origin.z),new Vector2(b11.rearDoor[0],b11.rearDoor[1]))<.01f,"doors differ from the town map");
        Need(b11.windows.Length==layout.openings.Count(o=>o.kind=="window"&&o.exterior),"outside windows differ from the town map");
        var result=new Result{passed=true,walls=walls.Length,openings=layout.openings.Length,apertureProbes=aperture,solidProbes=solid,doors=leaves,
            lamps=lights.Length,interiorLamps=interior,exteriorLamps=exterior,seats=seats.Length,shelters=shelters.Length,rooms=layout.rooms.Length,beds=beds,people=people.Length+others.Length,
            layoutRule="One shared prefab for street exterior and interior; no second scene shell. Openings are cut from the layout contract that also generated the Blender shell.",
            verification="Editor authoring check only; native rendering, controller walking and saves are verified by --review-hospital."};
        Directory.CreateDirectory("../output/demo/hospital");
        File.WriteAllText("../output/demo/hospital/authoring-checks.json",JsonUtility.ToJson(result,true));
        Debug.Log("HOSPITAL_AUTHORING_PASS: "+aperture+" apertures, "+solid+" solid wall probes, "+leaves+" doors, "+lights.Length+" lamps, "+beds+" beds");
    }
}
