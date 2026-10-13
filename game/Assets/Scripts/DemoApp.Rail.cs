using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class DemoApp {
    RailTrain seatedTrain;
    bool railTravelling;
    string railStatus;
    bool RailSeated=>seatedTrain;
    public bool MovementLocked=>OpeningRideActive||RailSeated||FurnitureSeated||CarSeated||Blocked;
    public bool LookAllowed=>!OpeningRideCameraMoving&&!creatorMenu&&!opening&&(!transitioning||railTravelling)&&!inspected&&!journal&&!paused&&!DialogueOpen&&!MilkOrdersOpen&&!PoliceReportOpen;
    void RestoreRailSeat() {
        if(!state.railSeated)return;
        seatedTrain=FindFirstObjectByType<RailTrain>();
        if(!seatedTrain){state.railSeated=false;return;}
        PlaceRailSeat();railStatus="列车已到站  ·  E 起身下车";
    }
    void PlaceRailSeat() {
        player.Place(seatedTrain.SeatPosition,125);
        player.eye.transform.localPosition=new Vector3(0,1.10f,0);
    }
    public void SitAndDepart(RailTrain train) {
        if(Blocked||RailSeated||FurnitureSeated||CarSeated||railTravelling||!train)return;
        // Recheck a physical seat interaction rather than allowing a remote journey.
        if(Vector3.Distance(player.eye.transform.position,train.rideSeat.position+Vector3.up*.55f)>3.2f)return;
        SetMapOpen(false);SetWatchOpen(false);
        seatedTrain=train;state.railSeated=true;PlaceRailSeat();
        Save();StartCoroutine(RailJourney());
    }
    bool TickRailSeat() {
        if(!RailSeated||railTravelling)return false;
        if(!opening&&!paused&&!journal&&!inspected&&!DialogueOpen&&Input.GetKeyDown(KeyCode.E)) {
            RiseFromRailSeat();return true;
        }
        return false;
    }
    void RiseFromRailSeat() {
        if(!RailSeated||railTravelling)return;
        var stand=seatedTrain.StandPosition;
        state.railSeated=false;seatedTrain=null;
        player.Place(stand,180);player.eye.transform.localPosition=new Vector3(0,1.66f,0);railStatus=null;Save();CursorMode();
    }
    IEnumerator RailJourney() {
        railTravelling=true;transitioning=true;CursorMode();
        railStatus="汽笛响起，列车正在发车……";
        var sourceTrain=seatedTrain;string target=sourceTrain.destination;
        sourceTrain.BeginDeparture();
        float start=Time.unscaledTime;
        while(Time.unscaledTime-start<5) {
            sourceTrain.StepDeparture(Time.unscaledDeltaTime);
            var yaw=player.transform.eulerAngles.y;var pitch=player.pitch;
            player.Place(sourceTrain.SeatPosition,yaw);player.pitch=pitch;
            player.eye.transform.localPosition=new Vector3(0,1.10f,0);
            player.eye.transform.localRotation=Quaternion.Euler(pitch,0,0);
            yield return null;
        }
        railStatus="驶向下一站……";yield return FadeTo(1,1);
        // No transient world coordinates are saved. Until arrival commits, disk points to the parked source seat.
        string old=CurrentScene;seatedTrain=null;
        seamlessShop=null;milkShopRoot=null;IsInsideShop=false;insideMilkShop=false;
        yield return SceneManager.UnloadSceneAsync(old);
        yield return SceneManager.LoadSceneAsync(target,LoadSceneMode.Additive);
        CurrentScene=target;SceneManager.SetActiveScene(SceneManager.GetSceneByName(target));
        seamlessShop=FindFirstObjectByType<BuildingInstance>();milkShopRoot=GameObject.Find("MilkShop")?.transform;
        seatedTrain=FindFirstObjectByType<RailTrain>();
        if(!seatedTrain)throw new InvalidOperationException("Arrival station has no train");
        state.railSeated=true;state.railJourneys++;PlaceRailSeat();RestoreOpeningRideScene();
        location=target=="Town"?"青川 · 车站":"邻镇 · 车站街区";locationAge=Time.time;
        railTravelling=false;Save();
        railStatus="列车已到站  ·  E 起身下车";
        yield return FadeTo(0,1);
        transitioning=false;CursorMode();Debug.Log("RAIL_ARRIVAL: "+old+" -> "+target+"; journeys="+state.railJourneys);
    }
    void DrawRailStatus() {
        if(!RailSeated||paused||journal)return;
        var rect=new Rect(W/2-280,H-100,560,48);
        Box(rect,new Color(.04f,.08f,.11f,.9f));Label(rect,railStatus,button);
    }
}
