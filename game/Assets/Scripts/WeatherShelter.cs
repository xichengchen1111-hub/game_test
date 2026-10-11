using System.Collections.Generic;
using UnityEngine;

// Authored roof footprint; does not add collisions or change the building mesh.
public sealed class WeatherShelter : MonoBehaviour {
    public Vector2 footprint;
    public float eaves, rise;
    public Vector2 centre;
    public ShopMechanism[] exteriorDoors = new ShopMechanism[0];
    public HotelCar vehicle;
    public bool ridgeAlongX, barrel, elliptical;
    static readonly HashSet<WeatherShelter> active = new HashSet<WeatherShelter>();
    const float CellSize=16;
    static readonly Dictionary<long,List<WeatherShelter>> cells=new Dictionary<long,List<WeatherShelter>>();
    readonly List<long> occupied=new List<long>();
    static long Key(int x,int z)=>((long)x<<32)^(uint)z;
    static long Key(Vector3 p)=>Key(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.z/CellSize));
    Matrix4x4 toLocal, toWorld;
    void OnEnable() {
        toLocal=transform.worldToLocalMatrix;toWorld=transform.localToWorldMatrix;active.Add(this);
        var bounds=new Bounds(toWorld.MultiplyPoint3x4(new Vector3(centre.x,eaves,centre.y)),Vector3.zero);
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})bounds.Encapsulate(toWorld.MultiplyPoint3x4(new Vector3(centre.x+x*footprint.x/2,eaves,centre.y+z*footprint.y/2)));
        for(int x=Mathf.FloorToInt(bounds.min.x/CellSize);x<=Mathf.FloorToInt(bounds.max.x/CellSize);x++)
            for(int z=Mathf.FloorToInt(bounds.min.z/CellSize);z<=Mathf.FloorToInt(bounds.max.z/CellSize);z++) {
                long key=Key(x,z);if(!cells.TryGetValue(key,out var list)){list=new List<WeatherShelter>();cells.Add(key,list);}list.Add(this);occupied.Add(key);
            }
    }
    void OnDisable() { active.Remove(this);foreach(long key in occupied)if(cells.TryGetValue(key,out var list)){list.Remove(this);if(list.Count==0)cells.Remove(key);}occupied.Clear(); }
    public bool RoofAt(Vector3 point, out float height) {
        var p=toLocal.MultiplyPoint3x4(point);
        float x=Mathf.Abs(p.x-centre.x), z=Mathf.Abs(p.z-centre.y);
        height=0;
        if(x>footprint.x*.5f || z>footprint.y*.5f)return false;
        float across=ridgeAlongX?z:x,half=ridgeAlongX?footprint.y*.5f:footprint.x*.5f;
        float roofRise=rise*(1-across/half);
        if(elliptical)roofRise=rise*Mathf.Sqrt(Mathf.Max(0,1-across*across/(half*half)));
        else if(barrel&&rise>0) {
            float radius=(half*half+rise*rise)/(2*rise);
            roofRise=rise-radius+Mathf.Sqrt(Mathf.Max(0,radius*radius-across*across));
        }
        height=toWorld.MultiplyPoint3x4(new Vector3(p.x,eaves+roofRise,p.z)).y;
        return true;
    }
    public static float RoofHeight(Vector3 point) {
        float height=-.04f;
        if(cells.TryGetValue(Key(point),out var candidates))foreach(var shelter in candidates)if(shelter.RoofAt(point,out var roof))height=Mathf.Max(height,roof);
        return height;
    }
    public static bool VerifySpatialIndex(Vector3 point) {
        float exhaustive=-.04f;foreach(var shelter in active)if(shelter.RoofAt(point,out var roof))exhaustive=Mathf.Max(exhaustive,roof);
        return Mathf.Abs(exhaustive-RoofHeight(point))<.001f;
    }
    public static bool Covered(Vector3 eye) => RoofHeight(eye)>eye.y;
    // Roof coverage still clips drops with the door open. Only the acoustic enclosure changes.
    public static bool MufflesRain(Vector3 eye) {
        bool covered=false;
        if(cells.TryGetValue(Key(eye),out var candidates))foreach(var shelter in candidates) {
            if(!shelter.RoofAt(eye,out var height)||height<=eye.y)continue;
            covered=true;
            foreach(var door in shelter.exteriorDoors)if(door&&door.OpenFraction>.001f)return false;
            if(shelter.vehicle&&shelter.vehicle.CabinOpen)return false;
        }
        return covered;
    }
    public static int Count => active.Count;
}
