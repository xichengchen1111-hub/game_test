using UnityEngine;

public partial class DemoApp {
    float nextMapLocation;
    void UpdateMapLocation() {
        if(!sceneReady||CurrentScene!="Town"||Time.unscaledTime<nextMapLocation)return;
        nextMapLocation=Time.unscaledTime+.35f;
        var p=player.transform.position;string name;
        if(IsInsideShop)name="钟表铺";
        else if(PoliceStation.Current&&PoliceStation.Current.RoomAt(p,out var policeRoom))name="警署 · "+(policeRoom.id=="interview"?"律师会见室":policeRoom.name);
        else if(HospitalBuilding.Current&&HospitalBuilding.Current.RoomAt(p,out var hospitalRoom))name="医院 · "+hospitalRoom.name;
        else if(RepairGarage.Current&&RepairGarage.Current.RoomAt(p,out var garageRoom))name="修车铺 · "+garageRoom.name;
        else if(HotelInn.Current&&HotelInn.Current.RoomAt(p,out var hotelRoom))name="旅馆 · "+hotelRoom.name;
        else if(insideMilkShop)name="牛奶屋";
        else if(Mathf.Abs(p.x)<13&&p.z>-13.5f&&p.z<14)name="小镇广场";
        else {
            float nearest=float.MaxValue;name="小镇";
            foreach(var road in TownPlan.Current.roads) {
                var a=new Vector2(road.points[0],road.points[1]);var b=new Vector2(road.points[2],road.points[3]);var point=new Vector2(p.x,p.z);
                var delta=b-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);float distance=(point-a-delta*t).sqrMagnitude;
                if(distance<nearest){nearest=distance;name=road.name;}
            }
            foreach(var building in TownPlan.Current.buildings)if(building.id.StartsWith("B")) {
                var r=building.plot;if(Mathf.Abs(p.x-r[0])<r[2]/2&&Mathf.Abs(p.z-r[1])<r[3]/2){name=building.name;break;}
            }
        }
        string next="青川 · "+name;if(next!=location){location=next;locationAge=Time.time;}
    }
}
