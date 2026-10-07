using System;
using UnityEngine;

public enum OpeningKind { Window, Door }
public enum BuildingWall { Front, Back, Left, Right }

[Serializable]
public class BuildingOpening {
    public string id;
    public OpeningKind kind;
    public BuildingWall wall;
    public float offset;
    public float centreHeight;
    public Vector2 size;
}

[CreateAssetMenu(menuName="Wrong Date/Building layout")]
public class BuildingLayout : ScriptableObject {
    public string buildingId="ClockShop";
    [Tooltip("Outside dimensions, including the wall thickness, in metres.")]
    public Vector2 footprint=new Vector2(7.4f,8.2f);
    public float wallHeight=3.3f;
    public float wallThickness=.2f;
    public float roofRise=1.6f;
    public Vector3 townPosition=new Vector3(9.72f,0,1);
    public float townYaw=90;
    public Vector3 interiorSpawn=new Vector3(0,.03f,-2.2f);
    public string entranceId="front-door";
    public BuildingOpening[] openings={
        new BuildingOpening{id="front-left",kind=OpeningKind.Window,wall=BuildingWall.Front,offset=-2,centreHeight=1.95f,size=new Vector2(2.1f,1.75f)},
        new BuildingOpening{id="front-door",kind=OpeningKind.Door,wall=BuildingWall.Front,offset=0,centreHeight=1.2f,size=new Vector2(1.4f,2.4f)},
        new BuildingOpening{id="front-right",kind=OpeningKind.Window,wall=BuildingWall.Front,offset=2,centreHeight=1.95f,size=new Vector2(2.1f,1.75f)}
    };

    public Quaternion TownRotation => Quaternion.Euler(0,townYaw,0);
    public float WallSpan(BuildingWall wall) => wall==BuildingWall.Front||wall==BuildingWall.Back?footprint.x:footprint.y-2*wallThickness;
    public Quaternion WallRotation(BuildingWall wall) => Quaternion.Euler(0,wall switch {BuildingWall.Back=>180,BuildingWall.Left=>90,BuildingWall.Right=>-90,_=>0},0);
    public Vector3 WallCentre(BuildingWall wall) => wall switch {
        BuildingWall.Back=>new Vector3(0,0,(footprint.y-wallThickness)/2),
        BuildingWall.Left=>new Vector3(-(footprint.x-wallThickness)/2,0,0),
        BuildingWall.Right=>new Vector3((footprint.x-wallThickness)/2,0,0),
        _=>new Vector3(0,0,-(footprint.y-wallThickness)/2)
    };
    public Vector3 OpeningCentre(BuildingOpening opening) => WallCentre(opening.wall)+WallRotation(opening.wall)*new Vector3(opening.offset,opening.centreHeight,.02f);
    public Vector3 ToTown(Vector3 roomPoint) => townPosition+TownRotation*roomPoint;
    public Vector3 ToInterior(Vector3 townPoint) => Quaternion.Inverse(TownRotation)*(townPoint-townPosition);
    public BuildingOpening Opening(string id) => Array.Find(openings,o=>o.id==id) ?? throw new InvalidOperationException("Unknown building opening: "+id);
}
