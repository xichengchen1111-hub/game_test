using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    void NeedNavigation(bool ok,string why){if(ok)return;Debug.LogError("MAP_NAVIGATION_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);}
    void MapReviewPitch(float pitch){player.pitch=pitch;player.eye.transform.localRotation=Quaternion.Euler(pitch,0,0);}
    Vector2 HeldScreen(Vector3 world)=>heldTownMap.PaperToScreen(heldTownMap.WorldToPaper(world));
    IEnumerator ReviewNavigationMap(){
        yield return new WaitForSeconds(.5f);NeedNavigation(townNavigation.Ready,"Navigation asset missing");
        NeedNavigation(mapLandmarks.Count==16&&mapLandmarks.Any(x=>x.id=="H03"&&x.symbol==TownMapSymbol.Tailor),"Required landmark missing");
        player.Place(TownPlan.Spawn,0);MapReviewPitch(0);var position=player.transform.position;float pitch=player.pitch;
        NeedNavigation(HandleMapKeys(true,false)&&mapOpen&&!Blocked&&!GameTimePaused&&player.pitch==pitch,"Taking out map changed look or blocked walking/time");
        yield return new WaitForSeconds(.4f);NeedNavigation(heldTownMap.Visible,"Held sheet did not finish drawing");yield return Capture("paper-map-looking-ahead");
        float aheadTop=heldTownMap.PaperToScreen(new Vector2(0,.40f)).y;
        MapReviewPitch(47);yield return new WaitForSeconds(.25f);float downTop=heldTownMap.PaperToScreen(new Vector2(0,.40f)).y;
        NeedNavigation(downTop-aheadTop>Screen.height*.3f,"Looking down did not reveal the physical paper");
        NeedNavigation(Vector3.Distance(position,player.transform.position)<.1f,"Taking out or looking at map teleported player");yield return Capture("paper-map-looking-down");
        var time=ElapsedGameMinutes;player.scriptedDirection=Vector3.right;yield return new WaitForSeconds(.65f);player.scriptedDirection=Vector3.zero;
        NeedNavigation(Vector3.Distance(position,player.transform.position)>1&&ElapsedGameMinutes>time&&!Blocked,"Could not walk/look at map while time advanced");yield return Capture("paper-map-walking-down");
        MapReviewPitch(0);var before=player.transform.position;yield return MapWalk(before+Vector3.right*2);NeedNavigation(mapOpen&&!Blocked,"Looking ahead closed or blocked held map");yield return Capture("paper-map-walking-ahead");
        player.Place(TownPlan.Spawn,0);MapReviewPitch(47);SetMapEditing(true);yield return new WaitForSeconds(.25f);
        NeedNavigation(mapEditing&&!Blocked&&!GameTimePaused&&Cursor.visible,"Alt cursor edit unexpectedly paused walking/time");
        foreach(var world in new[]{new Vector3(-150,0,30),new Vector3(0,0,0),new Vector3(188,0,40)}){
            var screen=HeldScreen(world);NeedNavigation(heldTownMap.TryPaperLocal(screen,out var local),"Held paper projection missed at "+world);
            var round=heldTownMap.PaperToWorld(local);NeedNavigation(Vector2.Distance(new Vector2(world.x,world.z),new Vector2(round.x,round.z))<.1f,"Perspective mouse-to-map conversion wrong: expected="+world+" actual="+round);
        }
        foreach(var landmark in mapLandmarks){
            NeedNavigation(HandleHeldMapRightClick(HeldScreen(landmark.point)),"Paper icon click rejected: "+landmark.id);
            NeedNavigation(state.hasMapWaypoint&&mapRouteAvailable,"Entrance unreachable: "+landmark.id);
            var route=townNavigation.Corners;
            for(int i=1;i<route.Length;i++){
                bool obstruction=Physics.CapsuleCast(route[i-1]+Vector3.up*.42f,route[i-1]+Vector3.up*1.5f,.22f,(route[i]-route[i-1]).normalized,out var hit,Vector3.Distance(route[i-1],route[i]),~((1<<2)|(1<<HeldTownMap.ViewLayer)),QueryTriggerInteraction.Ignore);
                NeedNavigation(!obstruction||hit.collider.GetComponentInParent<ShopMechanism>(),"Route clips collider: "+landmark.id);
            }
            Debug.Log("MAP_LANDMARK_ROUTE_PASS: "+landmark.id+" distance="+townNavigation.Distance);
            var pinUpper=heldTownMap.WorldToPaper(mapWaypoint)+Vector2.up*.026f;
            NeedNavigation(HandleHeldMapRightClick(heldTownMap.PaperToScreen(pinUpper))&&!state.hasMapWaypoint,"Upper pin does not cancel");
        }
        NeedNavigation(!HandleHeldMapRightClick(new Vector2(0,Screen.height))&&!state.hasMapWaypoint,"Clicking the sky placed marker");
        var anchor=heldTownMap.PaperToScreen(new Vector2(0,0));heldTownMap.TryPaperLocal(anchor,out var anchorLocal);var anchorWorld=heldTownMap.PaperToWorld(anchorLocal);
        heldTownMap.ZoomAt(2,anchor);yield return null;NeedNavigation(Vector3.Distance(anchorWorld,heldTownMap.PaperToWorld(anchorLocal))<.1f,"Paper zoom lost mouse anchor");heldTownMap.Pan(new Vector2(.03f,0));NeedNavigation(heldTownMap.Zoom==2,"Paper pan changed zoom");heldTownMap.ResetView();
        SetMapEditing(false);NeedNavigation(!mapEditing&&mapOpen&&!Blocked,"Releasing Alt did not restore observation");
        var tower=FindFirstObjectByType<ClockTowerInteriorLayout>();
        var interiorPositions=new[]{seamlessShop.layout.ToTown(seamlessShop.layout.interiorSpawn),milkShopRoot.GetComponent<MilkShopPlacement>().inside,tower.transform.TransformPoint(tower.machineStandingPoint)};
        for(int i=0;i<interiorPositions.Length;i++){player.Place(interiorPositions[i],0);MapReviewPitch(47);SetMapWaypoint(TownPlan.Spawn,"广场");NeedNavigation(mapRouteAvailable,"Interior route missing: "+i);yield return Capture("paper-map-interior-"+i);ClearMapWaypoint();}
        player.Place(new Vector3(139,.12f,-8),0);MapReviewPitch(47);SetMapWaypoint(new Vector3(159,.1f,-8),"坐标标记");
        NeedNavigation(mapRouteAvailable&&townNavigation.Distance>37&&townNavigation.Corners.Length>=4,"Hotel detour crosses the building");float hotelDistance=townNavigation.Distance;var walking=townNavigation.Corners.ToArray();
        yield return Capture("paper-map-hotel-route");yield return VerifyRenderedHeldRoute();
        foreach(var corner in walking)yield return MapWalk(corner);RefreshMapRoute();NeedNavigation(townNavigation.Distance<.3f&&mapOpen&&!Blocked,"Held walking route did not update at arrival");
        SetMapEditing(true);NeedNavigation(HandleHeldMapRightClick(HeldScreen(mapWaypoint))&&!state.hasMapWaypoint,"Arrival pin cancellation failed");SetMapEditing(false);
        var police=TownPlan.Current.Building("B03").parts[0].rect; // B10 is now an enterable building; B03 stays a closed placeholderSetMapWaypoint(new Vector3(police[0],.1f,police[1]),"坐标标记");NeedNavigation(!mapRouteAvailable,"Unreachable point invented a route");ClearMapWaypoint();
        // Reading hides the prop and locks movement while the world clock runs.
        journal=true;CursorMode();var readingTime=ElapsedGameMinutes;yield return new WaitForSeconds(.4f);NeedNavigation(Blocked&&!GameTimePaused&&!heldTownMap.ViewCamera.enabled&&ElapsedGameMinutes>readingTime,"Journal must hide paper while time continues");journal=false;CursorMode();yield return new WaitForSeconds(.4f);NeedNavigation(heldTownMap.Visible&&!Blocked,"Paper did not return from journal");
        paused=true;var pausedTime=ElapsedGameMinutes;yield return new WaitForSeconds(.3f);NeedNavigation(GameTimePaused&&pausedTime==ElapsedGameMinutes&&!heldTownMap.ViewCamera.enabled,"Explicit pause did not freeze time/hide paper");paused=false;
        NeedNavigation(HandleMapKeys(false,true)&&!mapOpen&&!paused&&!Blocked,"Esc on held map opened pause menu");yield return new WaitForSeconds(.4f);NeedNavigation(!heldTownMap.ViewCamera.enabled,"Paper did not put away");
        HandleMapKeys(true,false);player.Place(TownPlan.Spawn,0);MapReviewPitch(47);SetMapWaypoint(TownPlan.Point(TownPlan.Current.Building("B12").door)+Vector3.right*.85f,"主角住宅");yield return new WaitForSeconds(.4f);yield return Capture("paper-map-home-route");
        state.entries.Add(new NotebookEntry{id="E07",title="Map review fixture",body="Preserve prior evidence"});state.dialogueTopics.Add("map-review-topic");state.openFixtures.Add("milk-reading-lamp-off");state.towerAdjustmentMinutes=30;state.milkOrderDate="1931-01-19";SetMapOpen(false);Save();
        File.WriteAllText(Path.Combine(captures,"held-map-review.json"),JsonUtility.ToJson(new HeldNavigationReport{passed=true,walkingWithMap=true,lookUpDown=true,timeContinues=true,perspectivePicking=true,markerCancel=true,readingTimeContinues=true,explicitPauseFreezes=true,renderedRoute=true,landmarks=16,width=Screen.width,height=Screen.height,hotelDetourMetres=hotelDistance},true));Debug.Log("MAP_NAVIGATION_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyRenderedHeldRoute(){
        yield return new WaitForEndOfFrame();var frame=ScreenCapture.CaptureScreenshotAsTexture();int checkedPoints=0;var corners=townNavigation.Corners;
        for(int i=1;i<corners.Length;i++)foreach(float fraction in new[]{.25f,.5f,.75f}){
            var point=Vector3.Lerp(corners[i-1],corners[i],fraction);var paper=heldTownMap.WorldToPaper(point);
            if(mapLandmarks.Any(l=>Vector2.Distance(paper,heldTownMap.WorldToPaper(l.point))<.020f)||Vector2.Distance(paper,heldTownMap.WorldToPaper(player.transform.position))<.016f||Vector2.Distance(paper,heldTownMap.WorldToPaper(mapWaypoint))<.038f)continue;
            var screen=heldTownMap.PaperToScreen(paper);int x=Mathf.RoundToInt(screen.x),y=Mathf.RoundToInt(screen.y);if(x<3||x>=frame.width-3||y<3||y>=frame.height-3)continue;bool ink=false;
            for(int dy=-3;dy<=3;dy++)for(int dx=-3;dx<=3;dx++){var c=frame.GetPixel(x+dx,y+dy);ink|=c.r-c.g>.13f&&c.r-c.b>.18f&&c.r<.95f;}
            NeedNavigation(ink,"Perspective route missing from native paper pixels at "+screen);checkedPoints++;
        }
        Destroy(frame);NeedNavigation(checkedPoints>=3,"Too few uncovered rendered route samples");Debug.Log("HELD_MAP_ROUTE_RENDER_PASS: native pixels="+checkedPoints);
    }
    [Serializable] sealed class HeldNavigationReport {public bool passed,walkingWithMap,lookUpDown,timeContinues,perspectivePicking,markerCancel,readingTimeContinues,explicitPauseFreezes,renderedRoute;public int landmarks,width,height;public float hotelDetourMetres;}
    IEnumerator VerifyNavigationMapSave(){
        yield return new WaitForSeconds(.5f);NeedNavigation(state.hasMapWaypoint&&Mathf.Abs(state.mapWaypointX+15.65f)<.3f&&Mathf.Abs(state.mapWaypointZ+57)<.3f,"Waypoint not restored");
        NeedNavigation(state.entries.Any(e=>e.id=="E07")&&state.dialogueTopics.Contains("map-review-topic")&&state.openFixtures.Contains("milk-reading-lamp-off")&&state.towerAdjustmentMinutes==30&&state.milkOrderDate=="1931-01-19","Progress changed after paper map update");
        NeedNavigation(Vector3.Distance(player.transform.position,TownPlan.Spawn)<.2f,"Saved world position changed");
        MapReviewPitch(47);HandleMapKeys(true,false);yield return new WaitForSeconds(.4f);NeedNavigation(mapOpen&&mapRouteAvailable&&!Blocked&&!GameTimePaused,"Restored paper marker blocked gameplay");yield return Capture("paper-map-marker-reloaded");
        SetMapEditing(true);NeedNavigation(HandleHeldMapRightClick(HeldScreen(mapWaypoint))&&!state.hasMapWaypoint,"Restored marker cannot be removed");SetMapOpen(false);NeedNavigation(!Blocked&&!mapEditing,"Putting away paper left stale editing mode");Debug.Log("MAP_NAVIGATION_SAVE_PASS");Application.Quit();
    }
}
