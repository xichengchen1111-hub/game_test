using System.Linq;
using UnityEngine;

// Shared by a full police rebuild and the opening-only player build.
// Actors join BuildSlice.PoliceInteriorLayer, the only layer the station's room lamps light;
// on any other layer they stand unlit in a lit room.
public static class PoliceCastAuthoring {
    public static void AddToPolice(GameObject root) {
        if(!PeriodCharacterAsset.AllPrepared) {
            if(!root.GetComponentsInChildren<Interactable>(true).Any(i=>i.title=="值班警员")) {
                var oldLayout=PoliceLayout.Parse(root.GetComponent<PoliceStation>().layoutJson.text);
                var oldSpot=oldLayout.items.Single(i=>i.kind=="npc");
                var placeholder=StationClerkAsset.Create(root.transform,new Vector3(oldSpot.x,oldSpot.y,oldSpot.z),true);
                foreach(var renderer in placeholder.GetComponentsInChildren<Renderer>())renderer.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
            }
            Debug.LogWarning("POLICE_CAST_PENDING: all five characters require explicit acceptance, matching source hashes and complete imports; retaining the existing cast.");
            return;
        }
        var previous=root.transform.Find("Police staff");
        if(previous)Object.DestroyImmediate(previous.gameObject);
        // Replace only the old police desk actor; station and house clerks keep their own assets.
        foreach(var old in root.GetComponentsInChildren<Interactable>(true).Where(i=>i.title=="值班警员").ToArray())
            Object.DestroyImmediate(old.gameObject);
        var group=new GameObject("Police staff");group.transform.SetParent(root.transform,false);
        var layout=PoliceLayout.Parse(root.GetComponent<PoliceStation>().layoutJson.text);
        var spot=layout.items.Single(i=>i.kind=="npc");
        var duty=PeriodCharacterAsset.Create(group.transform,new Vector3(spot.x,spot.y,spot.z),"DutyOfficer","值班警员");
        var talk=duty.AddComponent<Interactable>();talk.title="值班警员";talk.source="青川警署值班接待窗口";talk.body="值班警员在窗口后整理文书。";
        AddRoutine(duty,"DutyOfficer");
        Staff(group.transform,"ReceptionOfficer","接待女警",new Vector3(21,.15f,2.9f),new Vector3(19.9f,.15f,6),
            "这边有座位。需要找人，可以问横厅的值班窗口。","来访的人多，过道得留出来。坐着等会儿吧。");
        Staff(group.transform,"PatrolOfficer","巡警",new Vector3(19.6f,.15f,14),new Vector3(17.7f,.15f,13),
            "雨披湿了总不容易干。好在这里还有个炉子。","有事可以去前楼值班窗口，我这边还在整理巡勤用品。");
        foreach(var renderer in group.GetComponentsInChildren<Renderer>())renderer.renderingLayerMask|=BuildSlice.PoliceInteriorLayer;
    }
    static void Staff(Transform parent,string id,string title,Vector3 position,Vector3 face,params string[] remarks) {
        var actor=PeriodCharacterAsset.Create(parent,position,id,title);
        actor.transform.localRotation=Quaternion.LookRotation(position-face);
        var staff=actor.AddComponent<PoliceStaff>();staff.title=title;staff.staffId=id;staff.remarks=remarks;
        actor.AddComponent<PoliceStaffPerformance>();
        AddRoutine(actor,id);
    }
    // Each officer's daily round (user, 2026-10-15: officers walk about, read files and do desk work).
    // Building-local metres from PoliceLayout furniture; seats: surface .61, sitting faces the desk,
    // approached from the side so nobody walks through a chair.
    const float Seat=.61f;
    static PoliceStaffRoutine.Spot S(string name,float x,float z,float fx,float fy,float fz,NpcPose.Activity activity,float min,float max,float weight,float ax=0,float az=0,bool post=false,float y=.15f)=>
        new PoliceStaffRoutine.Spot{name=name,position=new Vector3(x,y,z),face=new Vector3(fx,fy,fz),approach=ax==0&&az==0?Vector3.zero:new Vector3(ax,y,az),activity=activity,minSeconds=min,maxSeconds=max,weight=weight,seatHeight=Seat,post=post};
    public static PoliceStaffRoutine.Spot[] Spots(string id)=>id switch {
        "DutyOfficer"=>new[]{
            S("值班窗口",11.9f,9.72f,11.9f,1.5f,8.9f,NpcPose.Activity.Stand,18,35,2.5f,post:true),
            S("办公桌写报告",15,10.45f,15,1,11.25f,NpcPose.Activity.SitWrite,20,40,2,15.62f,10.45f),
            S("文件柜翻卷宗",17.85f,9.75f,18.6f,1.1f,9.75f,NpcPose.Activity.Read,10,18,1.5f),
            S("另一张桌前看文件",17.35f,10.5f,17.35f,.9f,11.3f,NpcPose.Activity.Read,10,16,1),
            S("炉边",13.4f,10.45f,13.4f,.7f,11.2f,NpcPose.Activity.Stand,8,14,1)},
        "ReceptionOfficer"=>new[]{
            S("候问侧席",21,2.9f,19.9f,1.4f,6,NpcPose.Activity.Stand,15,30,2),
            S("炉边",21.45f,1.55f,22.2f,.8f,1.05f,NpcPose.Activity.Stand,8,14,1),
            S("整理雨披",21.7f,4.3f,22.3f,1.2f,4.3f,NpcPose.Activity.Stand,6,10,.8f),
            S("长凳上翻登记簿",21.8f,5.74f,21.8f,1,4.5f,NpcPose.Activity.SitRead,15,25,1.2f,21.8f,5.12f),
            S("横厅电话",15.2f,8.25f,15.2f,1.6f,8.85f,NpcPose.Activity.Stand,8,12,.8f),
            S("门厅告示",10.75f,5.4f,10.1f,1.7f,5.4f,NpcPose.Activity.Read,6,10,.6f)},
        "PatrolOfficer"=>new[]{
            S("值班室",19.6f,14,17.7f,1.4f,13,NpcPose.Activity.Stand,12,25,1.5f),
            S("值班桌写巡勤记录",21.5f,14,22.25f,.9f,14,NpcPose.Activity.SitWrite,20,35,2,21.5f,13.38f),
            S("雨披架",21.65f,12.45f,22.3f,1.2f,12.45f,NpcPose.Activity.Stand,6,10,.8f),
            S("炉边",19.15f,15.1f,18.45f,.6f,15.35f,NpcPose.Activity.Stand,8,12,1),
            S("长凳上看巡勤簿",20.6f,15.74f,20.6f,1,14.5f,NpcPose.Activity.SitRead,14,22,1.2f,20.6f,15.12f)},
        "CustodyGuard"=>new[]{
            // North of the cell door, clear of the doorway (a body there would block the way in and out).
            S("羁押室门外",16.95f,21.85f,17.7f,1.5f,20.6f,NpcPose.Activity.Stand,18,35,2),
            S("回廊南头",16.95f,13.6f,12,1.5f,13.6f,NpcPose.Activity.Stand,8,14,1),
            S("内院",13.2f,19.5f,11.5f,.5f,18.4f,NpcPose.Activity.Stand,6,10,.7f,y:.06f),
            S("保管室门外",16.95f,17.6f,17.7f,1.5f,17,NpcPose.Activity.Stand,5,8,.6f)},
        _=>new PoliceStaffRoutine.Spot[0]};
    public static void AddRoutine(GameObject actor,string id) {
        if(!actor.GetComponent<NpcPose>())actor.AddComponent<NpcPose>();
        var routine=actor.GetComponent<PoliceStaffRoutine>();if(!routine)routine=actor.AddComponent<PoliceStaffRoutine>();
        routine.spots=Spots(id);
        if(id=="DutyOfficer"){routine.postWatch=new Vector3(11.9f,.15f,8.0f);routine.postRadius=2.6f;}
    }
}
