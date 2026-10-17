using System;
using UnityEngine;

// Generated from the authoritative map by tools/build_full_town.py.
[Serializable] public class TownPlan {
    public int version;
    public string sourceSha256;
    public float[] bounds,milkRect;
    public TownPlanBuilding[] buildings;
    public TownPlanRoad[] roads;
    public TownPlanTree[] trees;
    public TownPlanSurface[] surfaces;
    public TownPlanLamp[] lamps;
    public TownPlanLand[] land,details;
    public TownPlanFacility[] facilities;
    public TownPlanStreetItem[] streetItems;
    public TownPlanBuilding Building(string id)=>Array.Find(buildings,b=>b.id==id) ?? throw new Exception("Map building missing: "+id);
    static TownPlan cached;
    public static TownPlan Current => cached ?? (cached=JsonUtility.FromJson<TownPlan>(Resources.Load<TextAsset>("TownPlan").text));
    public static Vector3 Point(float[] p,float y=.1f)=>new Vector3(p[0],y,p[1]);
    public static float Yaw(string facing)=>facing=="N"?180:facing=="E"?-90:facing=="W"?90:0;
    public static readonly Vector3 Spawn=new Vector3(0,.12f,-10);
}
[Serializable] public class TownPlanBuilding { public string id,name,kind,front,doorFacing,rearFacing;public int storeys;public float[] door,rearDoor,gate,plot;public TownPlanPart[] parts;public TownPlanWindow[] windows; }
[Serializable] public class TownPlanPart {public string role;public float[] rect;}
[Serializable] public class TownPlanWindow {public float x,z,width;public string side;}
[Serializable] public class TownPlanRoad {public string id,name;public float[] points;public float lane,walk,strip,width;}
[Serializable] public class TownPlanTree {public float x,z,r;public string kind,owner;}
[Serializable] public class TownPlanSurface {public string name,material;public float[] vertices;}
[Serializable] public class TownPlanLamp {public float x,z;public string road;}
[Serializable] public class TownPlanLand {public string name,kind,owner;public float[] rect;}
[Serializable] public class TownPlanFacility {public string id,name,status;public float[] point;public float width,depth;}
[Serializable] public class TownPlanStreetItem {public string id,kind,name,facing,zone,@ref;public float[] point;public float width,depth,height;}
