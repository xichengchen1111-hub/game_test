using System;
using System.Collections;
using System.IO;
using UnityEngine;
// Focused native review of the court wall, its barred yard gate and the pause menu that now
// holds the volume and developer entries. Real controller, colliders and the ordinary E ray;
// isolated save, always muted. Scripted inputs are not physical keyboard or blind play.
public partial class DemoApp {
    void YNeed(bool ok,string why){if(!ok)throw new Exception("YARD_GATE_REVIEW_FAIL: "+why);Debug.Log("YARD_GATE_CHECK: "+why);}
    Vector3 YLocal=>PoliceStation.Current.transform.InverseTransformPoint(player.transform.position);
    IEnumerator YPush(Vector3 direction,float seconds) {
        player.scriptedDirection=direction;yield return new WaitForSeconds(seconds);player.scriptedDirection=Vector3.zero;yield return null;
    }
    IEnumerator YUse(ShopMechanism leaf,bool open,string why) {
        player.Aim(leaf.GetComponentInChildren<BoxCollider>().bounds.center);yield return null;yield return null;Physics.SyncTransforms();
        Interactable hitItem=null;
        if(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f))hitItem=hit.collider.GetComponentInParent<Interactable>();
        YNeed(hitItem==leaf,why+": the E ray reaches the leaf (hit "+(hitItem?hitItem.title:"nothing")+")");
        YNeed(TryInteract()&&!leaf.LastUseBlocked,why+": interaction accepted");
        yield return new WaitForSeconds(1.3f);
        YNeed(leaf.IsOpen==open&&!leaf.Moving,why+": leaf finished "+(open?"open":"closed"));
    }
    IEnumerator ReviewYardGate() {
        yield return new WaitForSeconds(1.2f);
        YNeed(PoliceStation.Current,"police station present in Town");
        YNeed(AudioListener.volume==0,"review runs muted");
        var L=PoliceStation.Current.Layout;float court=L.levels.court+.03f;
        var wall=L.Wall("C_N");float inner=wall.c-wall.t/2,outer=wall.c+wall.t/2;
        var left=PDoor("police-yard-gate-left");var right=PDoor("police-yard-gate-right");
        YNeed(left&&right&&!left.IsOpen&&!right.IsOpen,"both gate leaves start closed");
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Clear);DayReviewTime(12*60);
        // The view the user reported: from the court toward the north end.
        player.Place(PW(9.6f,court,14.6f),0);player.Aim(PW(11.5f,1.7f,23.8f));
        yield return new WaitForSeconds(.6f);yield return Capture("yard-01-court-closed-noon");
        DayReviewTime(21*60);yield return new WaitForSeconds(.6f);yield return Capture("yard-02-court-closed-night");
        DayReviewTime(12*60);
        // The whole 12 m between the wings is closed at eye height, from both sides. Court-side rays
        // start north of the benches, the planter and the guard's post in the east loggia.
        Physics.SyncTransforms();
        foreach(float x in new[]{5.8f,6.4f,7.6f,9f,10.3f,12.7f,14f,15.4f,16.6f,17.2f}) {
            YNeed(Physics.Raycast(PW(x,1.66f,22.4f),Vector3.forward,out var north,6,~0,QueryTriggerInteraction.Ignore)&&north.collider.name=="Wall C_N"&&Mathf.Abs(PoliceStation.Current.transform.InverseTransformPoint(north.point).z-inner)<.01f,"court side closed at x "+x);
            YNeed(Physics.Raycast(PW(x,1.66f,26.5f),Vector3.back,out var south,6,~0,QueryTriggerInteraction.Ignore)&&south.collider.name=="Wall C_N"&&Mathf.Abs(PoliceStation.Current.transform.InverseTransformPoint(south.point).z-outer)<.01f,"yard side closed at x "+x);
        }
        foreach(float x in new[]{11.07f,11.93f})
            YNeed(Physics.Raycast(PW(x,1.2f,22.4f),Vector3.forward,out var bars,6,~0,QueryTriggerInteraction.Ignore)&&bars.collider.GetComponentInParent<ShopMechanism>(),"closed gate leaf fills the opening at x "+x);
        // Controller cannot leave through the wall or the closed gate.
        player.Place(PW(9f,court,22.6f),0);yield return YPush(Vector3.forward,2.2f);
        YNeed(YLocal.z<inner,"wall stops the walking controller at "+YLocal);
        player.Place(PW(11.5f,court,22.6f),0);yield return YPush(Vector3.forward,2.2f);
        YNeed(YLocal.z<inner+.1f,"closed gate stops the walking controller at "+YLocal);
        // Open the left leaf from the court and walk out through it.
        player.Place(PW(11.3f,court,22.3f),0);yield return null;
        yield return YUse(left,true,"open left leaf from the court");
        yield return Capture("yard-03-gate-left-open");
        yield return WalkTo(PW(11.08f,court,22.6f));yield return WalkTo(PW(11.08f,court,25.3f));
        YNeed(YLocal.z>outer+1,"walked out through the open leaf to "+YLocal);
        // Camera-only placement for the outside elevation; the walk continues from where it left.
        var outside=player.transform.position;player.Place(PW(11.5f,.3f,31f),180);player.Aim(PW(11.5f,1.6f,23.8f));
        yield return new WaitForSeconds(.5f);yield return Capture("yard-04-wall-from-yard");
        player.Place(outside,180);yield return null;
        // From outside: open the right leaf, close the left one, and come back in.
        yield return WalkTo(PW(11.9f,court,25.3f));
        yield return YUse(right,true,"open right leaf from the yard");
        yield return WalkTo(PW(11.3f,court,25.0f));
        yield return YUse(left,false,"close left leaf from the yard");
        player.Place(PW(11.07f,court,25.2f),180);yield return YPush(Vector3.back,2.2f);
        YNeed(YLocal.z>outer-.1f,"closed left leaf blocks re-entry at "+YLocal);
        yield return WalkTo(PW(11.93f,court,25.2f));yield return WalkTo(PW(11.93f,court,22.2f));
        YNeed(YLocal.z<inner-1&&PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="court","returned to the court through the right leaf");
        // Pause menu: volume and developer entry live here; the submenu returns to it.
        player.Aim(PW(11.5f,1.7f,23.8f));yield return null;
        paused=true;CursorMode();YNeed(Blocked&&GameTimePaused&&!creatorMenu,"Esc pause state");
        yield return Capture("yard-05-pause-menu");
        OpenCreatorMenu();YNeed(creatorMenu&&paused&&MovementLocked&&!LookAllowed,"developer mode opens from the pause menu");
        yield return Capture("yard-06-developer-menu");
        CloseCreatorMenu();YNeed(!creatorMenu&&paused,"developer mode returns to the pause menu");
        paused=false;CursorMode();YNeed(!Blocked,"pause closes back to play");
        YNeed(AudioListener.volume==0,"review still muted");
        Save();
        File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"nativeControllerAndRays\":true,\"physicalInputs\":false,\"muted\":true,\"leftLeafOpen\":false,\"rightLeafOpen\":true}");
        Debug.Log("YARD_GATE_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyYardGateSave() {
        yield return new WaitForSeconds(1.2f);
        var left=PDoor("police-yard-gate-left");var right=PDoor("police-yard-gate-right");
        YNeed(left&&right&&!left.IsOpen&&right.IsOpen,"gate leaf states persist in a second process");
        YNeed(PoliceStation.Current.RoomAt(player.transform.position,out var room)&&room.id=="court","court position restored");
        YNeed(AudioListener.volume==0,"review runs muted");
        opening=false;player.Aim(PW(11.5f,1.7f,23.8f));yield return new WaitForSeconds(.4f);
        yield return Capture("yard-07-reload");Debug.Log("YARD_GATE_SAVE_PASS");Application.Quit();
    }
}
