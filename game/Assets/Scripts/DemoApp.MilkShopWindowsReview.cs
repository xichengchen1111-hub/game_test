using System;
using System.Collections;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    // Actual motor movement and existing window collision; only the two exterior closeups use a placed camera.
    IEnumerator ReviewMilkShopWindows() {
        yield return new WaitForSeconds(1);
        var shop=GameObject.Find("MilkShop");
        if(!shop)throw new Exception("Milk shop missing");
        var placement=shop.GetComponent<MilkShopPlacement>();
        var windows=Array.Find(shop.GetComponentsInChildren<MeshRenderer>(),r=>r.name=="Windows");
        var glass=Array.Find(windows.sharedMaterials,m=>m.name=="OldWindow");
        if(!glass||glass.shader.name!="WrongDate/ClearGlass"||glass.renderQueue!=3000||glass.GetColor("_Tint").a>.06f)
            throw new Exception("Milk shop panes are not using transparent glass");
        Vector3 Point(float x,float y,float z)=>shop.transform.TransformPoint(new Vector3(x,y,z));
        yield return WalkTo(new Vector3(4.9f,.1f,-8));
        yield return WalkTo(placement.outside);
        yield return WalkTo(placement.inside);
        for(int side=-1;side<=1;side+=2) {
            string label=side<0?"north":"south";
            // Stay in front of the counter, then approach each real side window.
            yield return WalkTo(Point(side*2.65f,.20f,-1.10f));
            yield return WalkTo(Point(side*2.65f,.20f,-.29f));
            player.Aim(Point(side*3.615f,2.08f,-.29f));
            yield return Capture("window-"+label+"-inside");
            // Fixed orientation sideways movement changes the framing against the actual exterior.
            yield return WalkTo(Point(side*2.65f,.20f,.20f),.35f);
            yield return Capture("window-"+label+"-inside-shifted");
            yield return WalkTo(Point(side*2.65f,.20f,-.29f));
            player.Aim(Point(side*3.615f,2.08f,-.29f));
            player.scriptedDirection=shop.transform.TransformDirection(Vector3.right*side);
            yield return new WaitForSeconds(.9f);player.scriptedDirection=Vector3.zero;
            float reached=side*shop.transform.InverseTransformPoint(player.transform.position).x;
            if(reached<3.0f||reached>3.35f)throw new Exception("Window failed to contain the player: "+label+" "+reached);
            yield return Capture("window-"+label+"-near");
            yield return WalkTo(Point(side*2.65f,.20f,-.29f));
            yield return WalkTo(Point(side*2.65f,.20f,-1.10f));
            yield return WalkTo(placement.inside);
        }
        yield return WalkTo(placement.outside);
        // Check the same physical window from outside, with unchanged town lighting.
        player.enabled=false;reviewPortrait=true;
        for(int side=-1;side<=1;side+=2) {
            player.eye.transform.position=Point(side*4.15f,1.99f,-.35f);
            player.eye.transform.LookAt(Point(side*2.5f,1.8f,.15f));
            yield return Capture("window-"+(side<0?"north":"south")+"-outside");
        }
        reviewPortrait=false;player.enabled=true;
        File.WriteAllText(Path.Combine(captures,"milkshop-window-results.json"),"{\"passed\":true,\"transparentSharedMaterial\":true,\"bothWindowCollisions\":true,\"bothInteriorViews\":true,\"doorwayRoundTrip\":true,\"actualController\":true,\"verification\":\"Visible Mac player; actual controller walks to each window and presses against existing side-wall collision; exterior closeups use controlled camera, same geometry and town lighting.\"}");
        Debug.Log("MILKSHOP_WINDOWS_REVIEW_PASS: both clear windows, physical collision, actual doorway round trip");
        Application.Quit();
    }
}
