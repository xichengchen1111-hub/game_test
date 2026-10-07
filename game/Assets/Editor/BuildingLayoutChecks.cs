using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildingLayoutChecks {
    static void Require(bool condition,string message) {
        if(!condition)throw new InvalidOperationException("Building consistency: "+message);
    }
    public static void ValidateDefinition(BuildingLayout layout) {
        Require(layout.footprint.x>1&&layout.footprint.y>1&&layout.wallHeight>2,"invalid room dimensions");
        Require(layout.wallThickness>.01f&&layout.wallThickness<Mathf.Min(layout.footprint.x,layout.footprint.y)/4,"invalid wall thickness");
        Require(layout.openings!=null&&layout.openings.Length>0,"no openings defined");
        Require(layout.openings.All(o=>o!=null&&!string.IsNullOrWhiteSpace(o.id))&&layout.openings.Select(o=>o.id).Distinct().Count()==layout.openings.Length,"opening IDs must be unique");
        Require(layout.openings.Count(o=>o.id==layout.entranceId&&o.kind==OpeningKind.Door)==1,"entrance must name one physical door");
        foreach(BuildingWall wall in Enum.GetValues(typeof(BuildingWall))) {
            float edge=-layout.WallSpan(wall)/2;
            foreach(var opening in layout.openings.Where(o=>o.wall==wall).OrderBy(o=>o.offset)) {
                float left=opening.offset-opening.size.x/2,right=opening.offset+opening.size.x/2;
                float bottom=opening.centreHeight-opening.size.y/2,top=opening.centreHeight+opening.size.y/2;
                Require(opening.size.x>.2f&&opening.size.y>.2f&&left>=edge-.001f&&right<=layout.WallSpan(wall)/2,"overlapping or out-of-bounds opening: "+opening.id);
                Require(bottom>=-.001f&&top<=layout.wallHeight,"opening height outside wall: "+opening.id);
                if(opening.kind==OpeningKind.Door)Require(Mathf.Abs(bottom)<.001f,"door must reach the floor: "+opening.id);
                edge=right;
            }
        }
    }
    static void CheckInstance(BuildingInstance instance,BuildingLayout layout) {
        Require(instance.layout==layout,"scene uses a different layout asset");
        var markers=instance.GetComponentsInChildren<BuildingOpeningMarker>();
        Require(markers.Length==layout.openings.Length,"missing or additional opening");
        foreach(var opening in layout.openings) {
            var matches=markers.Where(m=>m.openingId==opening.id).ToArray();
            Require(matches.Length==1,"missing or duplicated opening ID: "+opening.id);
            var actual=matches[0];
            Require(actual.kind==opening.kind&&Vector2.Distance(actual.apertureSize,opening.size)<.001f,"opening type/size differs: "+opening.id);
            Require(Vector3.Distance(instance.transform.InverseTransformPoint(actual.transform.position),layout.OpeningCentre(opening))<.001f,"opening position differs: "+opening.id);
            Require(Quaternion.Angle(Quaternion.Inverse(instance.transform.rotation)*actual.transform.rotation,layout.WallRotation(opening.wall))<.01f,"opening orientation differs: "+opening.id);
        }
        var floor=instance.transform.Find("Interior floor");
        Require(floor&&Vector3.Distance(floor.localScale,new Vector3(layout.footprint.x,.3f,layout.footprint.y))<.001f,"floor footprint differs from layout");
    }
    static string Path(Transform child,Transform root) {
        var parts=new List<string>();
        while(child!=root){parts.Add(child.name+"["+child.GetSiblingIndex()+"]");child=child.parent;}
        parts.Reverse();return string.Join("/",parts);
    }
    static void CompareGeometry(BuildingInstance town,BuildingInstance room) {
        var exterior=town.GetComponentsInChildren<MeshRenderer>().ToDictionary(r=>Path(r.transform,town.transform));
        var interior=room.GetComponentsInChildren<MeshRenderer>().ToDictionary(r=>Path(r.transform,room.transform));
        Require(exterior.Count==interior.Count,"renderer counts differ");
        foreach(var pair in exterior) {
            Require(interior.TryGetValue(pair.Key,out var counterpart),"missing interior geometry: "+pair.Key);
            var a=pair.Value;var b=counterpart;
            Require(a.GetComponent<MeshFilter>().sharedMesh==b.GetComponent<MeshFilter>().sharedMesh&&a.sharedMaterials.SequenceEqual(b.sharedMaterials),"different mesh/material: "+pair.Key);
            var ma=town.transform.worldToLocalMatrix*a.transform.localToWorldMatrix;
            var mb=room.transform.worldToLocalMatrix*b.transform.localToWorldMatrix;
            for(int i=0;i<16;i++)Require(Mathf.Abs(ma[i]-mb[i])<.001f,"different physical geometry: "+pair.Key);
        }
    }
    static void CheckPair(BuildingInstance town,BuildingInstance room,BuildingLayout layout,GameObject prefab) {
        CheckInstance(town,layout);CheckInstance(room,layout);
        Require(PrefabUtility.GetCorrespondingObjectFromSource(town.gameObject)==prefab&&PrefabUtility.GetCorrespondingObjectFromSource(room.gameObject)==prefab,"both scenes must instantiate the same building prefab");
        Require(!town.interior&&room.interior,"incorrect building scene roles");
        Require(Vector3.Distance(town.transform.position,layout.townPosition)<.001f&&Quaternion.Angle(town.transform.rotation,layout.TownRotation)<.01f,"town building transform differs from layout");
        Require(Vector3.Distance(room.transform.position,Vector3.zero)<.001f&&Quaternion.Angle(room.transform.rotation,Quaternion.identity)<.01f,"interior building must retain local coordinates");
        CompareGeometry(town,room);
        var entry=town.Opening(layout.entranceId).GetComponentInChildren<ScenePortal>();
        var exit=room.Opening(layout.entranceId).GetComponentInChildren<ScenePortal>();
        Require(entry&&exit&&entry.destination=="ClockShop"&&exit.destination=="Town"&&Vector3.Distance(entry.entryPosition,layout.interiorSpawn)<.001f,"door transition/spawn differs from physical layout");
        Require(town.GetComponentsInChildren<Interactable>().Length==1,"exterior room preview exposes duplicate clue interactions");
    }
    static void RejectMutation(Action mutate,Action restore,Action validate) {
        bool rejected=false;
        try{mutate();try{validate();}catch(InvalidOperationException){rejected=true;}}
        finally{restore();}
        Require(rejected,"validation accepted deliberately inconsistent geometry");
    }
    public static void ValidateScenes(BuildingLayout layout,string prefabPath) {
        var roomScene=SceneManager.GetActiveScene();
        var room=roomScene.GetRootGameObjects().Select(g=>g.GetComponent<BuildingInstance>()).Single(i=>i);
        var townScene=EditorSceneManager.OpenScene("Assets/Scenes/Town.unity",OpenSceneMode.Additive);
        try {
            var town=townScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BuildingInstance>()).Single();
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Action check=()=>CheckPair(town,room,layout,prefab);
            check();
            var window=room.GetComponentsInChildren<BuildingOpeningMarker>().First(m=>m.kind==OpeningKind.Window);
            var position=window.transform.localPosition;
            RejectMutation(()=>window.transform.localPosition+=Vector3.right*.15f,()=>window.transform.localPosition=position,check);
            RejectMutation(()=>window.gameObject.SetActive(false),()=>window.gameObject.SetActive(true),check);
            GameObject extra=null;
            RejectMutation(()=>extra=UnityEngine.Object.Instantiate(window.gameObject,window.transform.parent),()=>UnityEngine.Object.DestroyImmediate(extra),check);
            check();
            var street=roomScene.GetRootGameObjects().Single(g=>g.name=="Window street geometry");
            Require(street.GetComponentsInChildren<BuildingInstance>().Length==0,"duplicate shop surfaces in the window street");
            Require(street.GetComponentsInChildren<Interactable>().Length==0&&street.GetComponentsInChildren<Collider>().Length==0,"window street duplicates interactions/collisions");
            string report=System.IO.Path.GetFullPath("../output/demo/layout/build-layout-results.json");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(report));
            File.WriteAllText(report,JsonUtility.ToJson(new Result {
                building=layout.buildingId,sharedPrefab=prefabPath,footprint=layout.footprint,wallHeight=layout.wallHeight,
                exteriorWindows=town.GetComponentsInChildren<BuildingOpeningMarker>().Count(m=>m.kind==OpeningKind.Window),
                interiorWindows=room.GetComponentsInChildren<BuildingOpeningMarker>().Count(m=>m.kind==OpeningKind.Window),
                openings=layout.openings,geometryIdentical=true,doorAndSpawnAligned=true,
                rejectsMovedWindow=true,rejectsMissingWindow=true,rejectsExtraWindow=true,
                verification="Checks loaded scene instances, every mesh/material/transform and named physical openings; three deliberate mismatches are rejected before player build."
            },true));
            Debug.Log("BUILDING_LAYOUT_PASS: shared prefab, identical geometry and openings; rejected moved/missing/extra windows");
        } finally {EditorSceneManager.CloseScene(townScene,true);SceneManager.SetActiveScene(roomScene);}
    }
    [Serializable] class Result {
        public string building,sharedPrefab,verification;
        public Vector2 footprint;
        public float wallHeight;
        public int exteriorWindows,interiorWindows;
        public BuildingOpening[] openings;
        public bool geometryIdentical,doorAndSpawnAligned,rejectsMovedWindow,rejectsMissingWindow,rejectsExtraWindow;
    }
}
