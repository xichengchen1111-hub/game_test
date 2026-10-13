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
    }
}
