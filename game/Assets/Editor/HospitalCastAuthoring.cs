using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// The hospital's people. A figure is used only once its own model has been looked at and
// approved (assets/source/hospital-cast/acceptance.json, bound to file hashes like the police
// cast). Until then the porter and the ward nurse borrow an approved police figure, named as
// a stand-in; the child's bed keeps its place in the story without showing a wrong child; and
// the other staff, patients and visitors are simply not there yet.
public static class HospitalCastAuthoring {
    public const string Folder="Assets/Art/HospitalCast/";
    public const string Acceptance="assets/source/hospital-cast/acceptance.json";
    const string Lines="Assets/Resources/HospitalPeople.json";
    const float BenchTop=.45f;
    [Serializable] sealed class PeopleFile {public PersonLines[] people;}
    [Serializable] sealed class PersonLines {public string id;public string[] remarks;}
    // Standing height and shoulder width each source model is prepared at (tools/cast/import_period_character.py --height).
    public static readonly Dictionary<string,(float height,float width)> Size=new Dictionary<string,(float,float)> {
        {"Porter",(1.70f,.50f)},{"WardSister",(1.63f,.50f)},{"Child",(1.22f,.36f)},{"ChildGown",(1.22f,.36f)},
        {"StaffNurse",(1.68f,.48f)},{"Probationer",(1.57f,.46f)},{"Matron",(1.66f,.52f)},{"HouseSurgeon",(1.78f,.50f)},{"VisitingDoctor",(1.72f,.54f)},
        {"PatientMan",(1.69f,.48f)},{"PatientWoman",(1.60f,.46f)},{"PatientBoy",(1.12f,.34f)},
        {"VisitorWoman",(1.62f,.48f)},{"VisitorMan",(1.74f,.52f)},{"VisitorGranny",(1.52f,.48f)}};
    public static bool Ready(string id)=>PeriodCharacterAsset.Approved(Acceptance,Folder,id);
    // Revision 2 of the child (hospital gown, weak and tired; user, 2026-10-14) replaces the first
    // only once the user has approved it in acceptance.json; until then the approved first one stays.
    static string ChildId=>PreviewChild??(Ready("ChildGown")?"ChildGown":"Child");
    // Only for the editor pose review (HospitalChildReview): shows a revision in the bed before approval.
    // Never set while building the game.
    public static string PreviewChild;
    public static void AddToHospital(GameObject root) {
        var previous=root.transform.Find("Hospital people");if(previous)UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var group=new GameObject("Hospital people");group.transform.SetParent(root.transform,false);
        var layout=PoliceLayout.Parse(root.GetComponent<HospitalBuilding>().layoutJson.text);
        Standing(group.transform,layout.Item("porter"),"porter","Porter","门房","DutyOfficer","青川医院问讯处","门房在问讯台后守着登记簿。");
        Standing(group.transform,layout.Item("ward-sister"),"sister","WardSister","护士","ReceptionOfficer","青川医院儿童病房","病房护士在值班桌旁整理记录。");
        Child(group.transform,layout.Item("child"));Covers(root,layout,layout.Item("child"),PreviewChild!=null||Ready(ChildId));
        var lines=JsonUtility.FromJson<PeopleFile>(AssetDatabase.LoadAssetAtPath<TextAsset>(Lines).text).people.ToDictionary(p=>p.id,p=>p.remarks);
        var pending=new List<string>();
        foreach(var spot in layout.items.Where(i=>i.kind=="person")) {
            if(!Size.ContainsKey(spot.doc))throw new Exception("Hospital person without a cast figure: "+spot.id);
            if(!lines.TryGetValue(spot.id,out var remarks)||remarks.Length==0)throw new Exception("Hospital person without lines: "+spot.id);
            bool ready=Ready(spot.doc),bed=spot.target=="propped"||spot.target=="lying";
            if(bed)Covers(root,layout,spot,ready);
            if(!ready){pending.Add(spot.doc);continue;}
            GameObject actor=spot.target switch {
                "stand"=>Figure(group.transform,spot,spot.doc),
                "propped"=>InBed(group.transform,spot,spot.doc,false),
                "lying"=>InBed(group.transform,spot,spot.doc,true),
                "sit"=>OnBench(group.transform,spot,spot.doc),
                _=>throw new Exception("Unknown hospital pose "+spot.target+": "+spot.id)};
            actor.GetComponentInChildren<PeriodCharacterPerformance>().speaker=spot.title;
            var person=actor.AddComponent<HospitalPerson>();person.personId=spot.id;person.title=spot.title;person.source="青川医院";person.remarks=remarks;person.quiet=spot.target=="lying";
        }
        if(pending.Count>0)Debug.LogWarning("HOSPITAL_CAST_PENDING: not approved yet and left out: "+string.Join(", ",pending));
        // Room lamps light only the interior layer; on any other layer the people stand unlit.
        foreach(var renderer in group.GetComponentsInChildren<Renderer>())renderer.renderingLayerMask|=BuildSlice.HospitalInteriorLayer;
    }
    static GameObject Figure(Transform parent,PoliceItem spot,string id) {
        var actor=PeriodCharacterAsset.Create(parent,new Vector3(spot.x,spot.y,spot.z),Folder,id,spot.title,Size[id].height,Size[id].width);
        // Period characters face their local -Z.
        actor.transform.localRotation=Quaternion.Euler(0,spot.yaw+180,0);
        return actor;
    }
    static void Standing(Transform parent,PoliceItem spot,string role,string id,string speaker,string standIn,string source,string body) {
        bool ready=Ready(id);
        var actor=ready?Figure(parent,spot,id):PeriodCharacterAsset.Create(parent,new Vector3(spot.x,spot.y,spot.z),standIn,spot.title);
        if(!ready) {
            actor.name+=" · 临时模型";actor.transform.localRotation=Quaternion.Euler(0,spot.yaw+180,0);
            Debug.LogWarning("HOSPITAL_CAST_PENDING: "+id+" is not approved yet; the police figure "+standIn+" stands in.");
        }
        var person=actor.AddComponent<HospitalActor>();person.role=role;person.title=spot.title;person.source=source;person.body=body;
        actor.GetComponentInChildren<PeriodCharacterPerformance>().speaker=speaker;
    }
    // The covers drawn over whoever is in a bed are a separate object of the shell (Furn_<bed>-occupied):
    // shown with the figure, hidden while the bed has nobody to show.
    static void Covers(GameObject root,PoliceLayout layout,PoliceItem spot,bool occupied) {
        var bed=layout.furniture.Where(f=>f.kind=="wardbed").OrderBy(f=>new Vector2(f.x-spot.x,f.z-spot.z).sqrMagnitude).First();
        if(new Vector2(bed.x-spot.x,bed.z-spot.z).magnitude>1.1f)throw new Exception("Hospital person is not in a bed: "+spot.id);
        var covers=root.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="Furn_"+bed.id+"-occupied");
        if(!covers)throw new Exception("Bed without covers for its occupant: "+bed.id+" ("+spot.id+")");
        covers.gameObject.SetActive(occupied);
    }
    static void Child(Transform parent,PoliceItem spot) {
        GameObject actor;
        if(PreviewChild!=null||Ready(ChildId)) {
            actor=InBed(parent,spot,ChildId,false,true);
            actor.GetComponentInChildren<PeriodCharacterPerformance>().speaker="孩子";
        } else {
            actor=new GameObject(spot.title+" · 人物待接入");actor.transform.SetParent(parent,false);actor.transform.localPosition=new Vector3(spot.x,spot.y,spot.z);
            actor.transform.localRotation=Quaternion.Euler(0,spot.yaw+180,0);
            // Reach of the E ray: where the sitting child will be.
            var box=actor.AddComponent<BoxCollider>();box.center=new Vector3(0,.34f,-.02f);box.size=new Vector3(.5f,.72f,.5f);
            Debug.LogWarning("HOSPITAL_CAST_PENDING: Child is not approved yet; the bed has no figure.");
        }
        var person=actor.AddComponent<HospitalActor>();person.role="child";person.title=spot.title;person.source="青川医院儿童病房";person.body="孩子靠着枕头坐在床上。";
    }
    static HospitalBedPose Pose(GameObject actor,float lean,HospitalBedPose.Mode mode,out Transform visual,out Transform hips) {
        visual=actor.transform.GetChild(0);var bones=visual.GetComponentsInChildren<Transform>(true);
        Transform Bone(string name)=>bones.Single(t=>t.name==name);
        hips=Bone("Hips");
        // The frame the pose works in: up runs from the hips to the head, the chest faces -Z.
        var frame=new GameObject("Body frame").transform;frame.SetParent(actor.transform,false);frame.localRotation=Quaternion.Euler(lean,0,0);
        var pose=actor.AddComponent<HospitalBedPose>();pose.mode=mode;pose.frame=frame;
        // Posed people speak without the talking gestures (see PeriodCharacterPerformance.gestures).
        pose.performance=actor.GetComponentInChildren<PeriodCharacterPerformance>();if(pose.performance)pose.performance.gestures=false;
        pose.leftLeg=Bone("Left_UpperLeg");pose.rightLeg=Bone("Right_UpperLeg");pose.leftShin=Bone("Left_LowerLeg");pose.rightShin=Bone("Right_LowerLeg");
        pose.leftUpperArm=Bone("Left_UpperArm");pose.rightUpperArm=Bone("Right_UpperArm");
        pose.leftLowerArm=Bone("Left_LowerArm");pose.rightLowerArm=Bone("Right_LowerArm");
        pose.neck=Bone("Neck");pose.head=Bone("Head");pose.hips=hips;pose.hipsRest=hips.localPosition;
        UnityEngine.Object.DestroyImmediate(actor.GetComponent<BoxCollider>());
        return pose;
    }
    // In bed the figure is the standing character with its real idle and talk clips; HospitalBedPose
    // hides the legs under the covers each frame. Sitting up: the hips rest on the mattress and the back
    // leans into the pillows. Lying: laid back until the head is a little above level, on the pillow.
    // The child (user, 2026-10-14): weak and tired, she half lies against the sloped pillows
    // (tools/hospital/build_hospital.py) and moves little. Her head turns slowly and not far, it rests
    // tilted on the pillow, her hands lie on the covers, the idle runs slower and her mouth opens less.
    const float WearyLean=30;
    static GameObject InBed(Transform parent,PoliceItem spot,string id,bool lying,bool weary=false) {
        var actor=Figure(parent,spot,id);float height=Size[id].height,lean=lying?84:weary?WearyLean:9;
        var pose=Pose(actor,lean,lying?HospitalBedPose.Mode.LyingInBed:HospitalBedPose.Mode.SittingUpInBed,out var visual,out var hips);
        if(lying){pose.maxYaw=42;pose.maxPitchUp=8;pose.maxPitchDown=4;pose.armsIn=16;}
        if(weary) {
            pose.maxYaw=30;pose.maxPitchUp=10;pose.maxPitchDown=8;pose.turnRate=1.3f;pose.headTilt=7;pose.talkNod=.3f;
            pose.headForward=22;pose.shoulderForward=3;pose.elbowBend=26;pose.armsIn=12;pose.shoulderIn=12;
            if(pose.performance)pose.performance.pace=.55f;
            var mouth=actor.GetComponentInChildren<PeriodCharacterMouthMotion>();if(mouth)mouth.maxDegrees*=.65f;
        }
        // Create() stands the soles on the origin.
        visual.RotateAround(hips.position,actor.transform.right,lean);
        visual.position+=actor.transform.position+Vector3.up*(lying?.11f:.09f)-hips.position;
        // Reach of the E ray: the upper body above the covers.
        var box=actor.AddComponent<BoxCollider>();
        if(lying){box.center=new Vector3(0,.14f,height*.24f);box.size=new Vector3(.56f,.34f,height*.54f);}
        else if(weary){box.center=new Vector3(0,height*.22f,.1f);box.size=new Vector3(.5f,height*.44f+.04f,.7f);}
        else {box.center=new Vector3(0,height*.28f,-.02f);box.size=new Vector3(.5f,height*.56f+.04f,.5f);}
        return actor;
    }
    static GameObject OnBench(Transform parent,PoliceItem spot,string id) {
        var actor=Figure(parent,spot,id);float height=Size[id].height;
        var pose=Pose(actor,0,HospitalBedPose.Mode.SittingOnSeat,out var visual,out var hips);pose.shoulderForward=12;pose.elbowBend=70;
        visual.position+=Vector3.up*(spot.y+BenchTop+.1f-parent.InverseTransformPoint(hips.position).y);
        var box=actor.AddComponent<BoxCollider>();box.center=new Vector3(0,BenchTop+height*.24f,-.12f);box.size=new Vector3(Size[id].width,height*.5f+.2f,.62f);
        return actor;
    }
}
