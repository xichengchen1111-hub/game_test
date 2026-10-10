using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoApp {
    sealed class MapLandmark {public string id,name;public TownMapSymbol symbol;public Vector3 point,entrance;public bool finished;}
    bool mapOpen,mapEditing,mapRouteAvailable,mapDragging;
    TownNavigation townNavigation;
    HeldTownMap heldTownMap;
    readonly List<MapLandmark> mapLandmarks=new List<MapLandmark>();
    Vector3 mapWaypoint,mapRouteOrigin;
    Vector2 mapDragLocal;
    float nextMapRouteUpdate;
    string mapDestinationName="坐标标记";
    public bool MapEditing=>mapEditing;

    void InitializeTownMap(){
        townNavigation=new TownNavigation();var plan=TownPlan.Current;
        var ids=new[]{"B01","B04","H03","B02","B12","B05","B10","B03","B17","B11","B08","B07","B06","B09","B14","B15"};
        var symbols=new[]{TownMapSymbol.Clock,TownMapSymbol.Milk,TownMapSymbol.Tailor,TownMapSymbol.Watch,TownMapSymbol.Home,TownMapSymbol.Home,TownMapSymbol.Police,TownMapSymbol.Post,TownMapSymbol.Station,TownMapSymbol.Hospital,TownMapSymbol.Inn,TownMapSymbol.Repair,TownMapSymbol.Pub,TownMapSymbol.Shop,TownMapSymbol.Shop,TownMapSymbol.Church};
        var printed=new List<HeldMapLandmark>();
        for(int i=0;i<ids.Length;i++){
            var b=plan.Building(ids[i]);var r=b.parts[0].rect;
            var outside=TownPlan.Point(b.door)+Quaternion.Euler(0,TownPlan.Yaw(b.doorFacing),0)*Vector3.back*.85f;
            var point=new Vector3(r[0],.1f,r[1]);bool finished=ids[i]=="B01"||ids[i]=="B02"||ids[i]=="B04";
            if(ids[i]=="B04"&&milkShopRoot){point=milkShopRoot.position;outside=milkShopRoot.GetComponent<MilkShopPlacement>().outside;}
            string name=b.name.Replace("（背景）","");
            mapLandmarks.Add(new MapLandmark{id=ids[i],name=name,symbol=symbols[i],point=point,entrance=outside,finished=finished});
            printed.Add(new HeldMapLandmark{name=name,point=point,symbol=symbols[i]});
        }
        heldTownMap=HeldTownMap.Create(player,font,printed.ToArray());
        var bounds=plan.bounds;
        if(state.hasMapWaypoint&&FiniteMapNumber(state.mapWaypointX)&&FiniteMapNumber(state.mapWaypointZ)&&state.mapWaypointX>=bounds[0]&&state.mapWaypointX<=bounds[1]&&state.mapWaypointZ>=bounds[2]&&state.mapWaypointZ<=bounds[3]){
            mapWaypoint=new Vector3(state.mapWaypointX,.1f,state.mapWaypointZ);RefreshMapRoute();
        }else state.hasMapWaypoint=false;
    }
    static bool FiniteMapNumber(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    bool HandleMapKeys(bool m,bool escape){
        if(mapOpen&&!Blocked&&(m||escape)){SetMapOpen(false);return true;}
        if(m&&sceneReady&&!Blocked){SetMapOpen(true);return true;}
        return false;
    }
    void SetMapOpen(bool value){
        if(mapOpen==value)return;
        if(value&&(!sceneReady||Blocked))return;
        mapOpen=value;focus=null;mapDragging=false;SetMapEditing(false);
        if(value){SetWatchOpen(false);RefreshMapRoute();}else Save();
        GetComponent<TownSound>()?.PlayHeldItem(true,value);
        if(heldTownMap)heldTownMap.SetShown(value&&!Blocked);
        CursorMode();
    }
    void SetMapEditing(bool value){
        bool editing=value&&mapOpen&&!Blocked;
        if(mapEditing==editing)return;
        mapEditing=editing;mapDragging=false;CursorMode();
    }
    void TickHeldMap(){
        if(!heldTownMap)return;
        heldTownMap.SetShown(mapOpen&&!Blocked);
        if(!Review)SetMapEditing(Input.GetKey(KeyCode.LeftAlt)||Input.GetKey(KeyCode.RightAlt));
        if(Blocked&&mapEditing)SetMapEditing(false);
        if(!mapOpen||Blocked)return;
        if(state.hasMapWaypoint&&Time.unscaledTime>=nextMapRouteUpdate&&Vector3.Distance(player.transform.position,mapRouteOrigin)>.8f)RefreshMapRoute();
        if(mapEditing&&!Review){
            if(Input.GetMouseButtonDown(1))HandleHeldMapRightClick(Input.mousePosition);
            float scroll=Input.mouseScrollDelta.y;
            if(Mathf.Abs(scroll)>.01f)heldTownMap.ZoomAt(Mathf.Pow(1.15f,scroll),Input.mousePosition);
            if(Input.GetMouseButtonDown(0)){mapDragging=heldTownMap.TryPaperLocal(Input.mousePosition,out mapDragLocal);}
            if(!Input.GetMouseButton(0))mapDragging=false;
            if(mapDragging&&heldTownMap.TryPaperLocal(Input.mousePosition,out var local)){heldTownMap.Pan(local-mapDragLocal);mapDragLocal=local;}
        }
    }
    void RefreshMapRoute(){
        mapRouteAvailable=state.hasMapWaypoint&&townNavigation.Route(player.transform.position,mapWaypoint);
        mapRouteOrigin=player.transform.position;nextMapRouteUpdate=Time.unscaledTime+1;
        if(heldTownMap)heldTownMap.SetNavigation(state.hasMapWaypoint,mapWaypoint,mapRouteAvailable?townNavigation.Corners:Array.Empty<Vector3>());
    }
    void SetMapWaypoint(Vector3 requested,string name){
        var bounds=TownPlan.Current.bounds;
        if(!FiniteMapNumber(requested.x)||!FiniteMapNumber(requested.z)||requested.x<bounds[0]||requested.x>bounds[1]||requested.z<bounds[2]||requested.z>bounds[3])return;
        mapWaypoint=townNavigation.Snap(requested,4,out var nearest)?nearest:requested;
        state.hasMapWaypoint=true;state.mapWaypointX=mapWaypoint.x;state.mapWaypointZ=mapWaypoint.z;mapDestinationName=name;
        RefreshMapRoute();Save();
    }
    void ClearMapWaypoint(){state.hasMapWaypoint=false;mapRouteAvailable=false;mapDestinationName="坐标标记";RefreshMapRoute();Save();}
    bool HandleHeldMapRightClick(Vector2 screen){
        if(!mapOpen||!mapEditing||Blocked||!heldTownMap.TryPaperLocal(screen,out var local)||!HeldTownMap.MapArea.Contains(local))return false;
        if(state.hasMapWaypoint&&heldTownMap.PinContains(local,mapWaypoint)){ClearMapWaypoint();return true;}
        MapLandmark landmark=null;float best=.019f;
        foreach(var b in mapLandmarks){float distance=Vector2.Distance(local,heldTownMap.WorldToPaper(b.point));if(distance<best){best=distance;landmark=b;}}
        if(landmark!=null)SetMapWaypoint(landmark.entrance,landmark.name);
        else SetMapWaypoint(heldTownMap.PaperToWorld(local),"坐标标记");
        return true;
    }
    void DrawHeldMapDestination(){
        if(state.hasMapWaypoint){var right=new GUIStyle(small){alignment=TextAnchor.UpperRight};Label(new Rect(W-470,94,430,32),mapDestinationName+" · "+(mapRouteAvailable?townNavigation.Distance.ToString("0")+" 米":"暂无可通行路线"),right);}
    }
}
