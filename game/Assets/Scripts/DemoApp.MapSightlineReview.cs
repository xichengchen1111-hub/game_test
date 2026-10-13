using System.Collections;
using System.IO;
using UnityEngine;
public partial class DemoApp {
    // Shared playable-house standing point used by the ordinary east-door review.
    // The old +9 m proxy now lies in the open door leaf's sweep after HOUSE-07.
    Vector3 HomeDeliveryStandingPoint()=>HouseInstance.Current.Point(10,.16f,0);
    IEnumerator ReviewMapSightlines() {
        yield return new WaitForSeconds(1);
        var home=HouseInstance.Current;ONeed(home,"shared house exists");
        var feet=HomeDeliveryStandingPoint();Physics.SyncTransforms();
        ONeed(home.ClearStandingPosition(feet),"delivery view is an actual clear standing point");
        var neighbor=new Vector3(-40.8f,1.6f,-57);var kitchen=new Vector3(-33,1.6f,-57);
        ONeed(!Physics.Linecast(neighbor,kitchen),"original neighbor-to-kitchen proxy remains clear");
        ONeed(Physics.Linecast(neighbor,feet+Vector3.up*1.66f),"house volume still hides the east-door return");
        player.Place(feet,0);yield return new WaitForSeconds(.5f);
        var dial=new Vector3(0,ClockTowerInteriorLayout.DialHeight,-ClockTowerInteriorLayout.DialPlaneDistance);player.Aim(dial);
        bool obstructed=Physics.Linecast(player.eye.transform.position,dial,out var hit)&&!hit.collider.GetComponentInParent<ClockTowerInstance>();
        ONeed(!obstructed,"actual player-height clock view remains clear: "+(hit.collider?hit.collider.name:"none"));
        var cycle=FindFirstObjectByType<DayNightCycle>();cycle.SetReviewWeather(TownWeather.Kind.Clear);DayReviewTime(1075);
        yield return Capture("home-delivery-clock-view");
        File.WriteAllText(Path.Combine(captures,"sightline.json"),"{\"passed\":true,\"standingLocal\":[10,0.16,0],\"actualPlayerEye\":true,\"historicalRouteProof\":false}");
        Debug.Log("MAP_SIGHTLINE_REVIEW_PASS");Application.Quit();
    }
}
