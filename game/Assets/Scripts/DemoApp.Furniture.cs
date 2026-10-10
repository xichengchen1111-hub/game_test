using System;
using UnityEngine;

public partial class DemoApp {
    FurnitureSeat restingSeat;
    Vector3 restingExit;
    string restingFeedback;
    public bool FurnitureSeated=>restingSeat;
    void PlaceFurnitureSeat() {
        player.Place(restingSeat.sittingPoint.position,restingSeat.sittingPoint.eulerAngles.y);
        player.eye.transform.localPosition=new Vector3(0,restingSeat.eyeHeight,0);
    }
    public void SitOnFurniture(FurnitureSeat seat) {
        if(!seat||Blocked||RailSeated||FurnitureSeated||railTravelling)return;
        if(Vector3.Distance(player.eye.transform.position,seat.GetComponent<Collider>().bounds.center)>3.2f)return;
        if(!seat.FindStandingPosition(out var exit)||!SafePosition(exit))return;
        restingSeat=seat;restingExit=exit;restingFeedback=null;
        PlaceFurnitureSeat();Save();CursorMode();
    }
    void RestoreFurnitureSeat() {
        if(string.IsNullOrEmpty(state.furnitureSeatId))return;
        if(RailSeated){state.furnitureSeatId="";return;}
        foreach(var seat in FindObjectsByType<FurnitureSeat>(FindObjectsSortMode.None)) {
            if(seat.seatId!=state.furnitureSeatId)continue;
            if(!seat.FindStandingPosition(out restingExit)||!SafePosition(restingExit))break;
            restingSeat=seat;PlaceFurnitureSeat();return;
        }
        // Saved feet point to the last clear aisle, so a removed/blocked seat restores safely standing.
        state.furnitureSeatId="";
    }
    public bool RiseFromFurnitureSeat() {
        if(!FurnitureSeated||Blocked||railTravelling)return false;
        if(!restingSeat.FindStandingPosition(out var exit)) {
            restingFeedback="前方有人或物件，请等通道空出来再起身。";return false;
        }
        float yaw=player.transform.eulerAngles.y;restingSeat=null;restingFeedback=null;
        player.Place(exit,yaw);player.eye.transform.localPosition=new Vector3(0,1.66f,0);
        state.furnitureSeatId="";Save();CursorMode();return true;
    }
    bool TickFurnitureSeat() {
        if(!FurnitureSeated)return false;
        if(!Blocked&&Input.GetKeyDown(KeyCode.E)){RiseFromFurnitureSeat();return true;}
        return false;
    }
    void DrawFurnitureStatus() {
        if(!FurnitureSeated||Blocked)return;
        var rect=new Rect(W/2-340,H-100,680,48);
        Box(rect,new Color(.04f,.08f,.11f,.9f));
        Label(rect,restingFeedback??"坐着休息  ·  E 起身  ·  Q 怀表  ·  T 车票",button);
    }
}
