using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    IEnumerator ReviewTicketOffice() {
        var station=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.evidenceId=="E03").transform.parent;
        TicketOfficeChecks.Validate(station.gameObject,Resources.Load<TextAsset>("TicketOfficeGeometry"));
        yield return RailWalk(new Vector3(188,.12f,45.5f));
        yield return RailWalk(new Vector3(180,.12f,45.5f));
        player.Aim(new Vector3(180,2.04f,47));yield return Capture("ticket-office-front");
        var clerk=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.title=="售票员");
        player.Aim(clerk.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head").position);yield return null;
        NeedRail(TryInteract()&&DialogueOpen&&dialogueNode.speaker=="售票员","ordinary 3m E ray could not open clerk dialogue");
        NeedRail(dialogueNode.text=="你好。","clerk greeting missing");AdvanceDialogue();
        NeedRail(dialogueChoices.Count==6,"clerk questions missing");SelectDialogue(dialogueChoices.FindIndex(c=>c.topic=="station-clerk.inspection"));AdvanceDialogue();
        NeedRail(dialogueNode.text=="检票的时候，会用检票钳在票上打孔。旁边废票桶里的同线路旧票，您可以拿起来看看。","clerk inspection rule changed");
        yield return Capture("ticket-office-clerk-dialogue");AdvanceDialogue();
        SelectDialogue(dialogueChoices.FindIndex(c=>c.topic=="station-clerk.boarding"));AdvanceDialogue();
        NeedRail(DialogueOpen&&dialogueNode.id=="boarding","clerk boarding instruction missing");CloseDialogue();
        NeedRail(!Blocked,"clerk dialogue did not restore movement");
        player.Aim(new Vector3(180,1.64f,47.9f));yield return Capture("ticket-office-wicket-clerk");
        yield return ReadWorldItem("手持检票钳",new Vector3(181.5f,.12f,45.5f));
        player.Aim(new Vector3(181.335f,1.19f,46.60f));yield return Capture("ticket-office-punch-close");
        yield return RailWalk(new Vector3(183.2f,.12f,45.5f));
        player.Aim(new Vector3(179.9f,1.75f,47.5f));yield return Capture("ticket-office-right");
        // Walk the real staff-side passage around the open right end, without teleporting.
        yield return RailWalk(new Vector3(183.2f,.12f,48));
        yield return RailWalk(new Vector3(181.25f,.12f,48));
        player.Aim(new Vector3(180,2.06f,48.72f));yield return Capture("ticket-office-stock-cabinet");
        player.Aim(new Vector3(180,.55f,48.458f));yield return Capture("ticket-office-storage-base");
        yield return RailWalk(new Vector3(183.2f,.12f,48));
        yield return RailWalk(new Vector3(183.2f,.12f,45.5f));
        yield return RailWalk(new Vector3(183.2f,.12f,45.8f));
        yield return RailWalk(new Vector3(175.9f,.12f,45.8f));
        player.Aim(new Vector3(180,1.85f,47));yield return Capture("ticket-office-left");
        var bin=FindObjectsByType<Interactable>(FindObjectsSortMode.None).Single(i=>i.evidenceId=="E03");
        var stock=bin.GetComponentsInChildren<RailTicketPiece>();
        NeedRail(stock.Length==8,"spent ticket stock was changed/duplicated");
        var pile=station.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="WastePaperPileSupport");
        var pileCollider=pile.GetComponent<MeshCollider>();
        foreach(var piece in stock) {
            var bounds=piece.GetComponent<MeshRenderer>().bounds;
            var origin=piece.transform.position+Vector3.up*.08f;
            NeedRail(pileCollider.Raycast(new Ray(origin,Vector3.down),out var support,.15f),"spent ticket has no physical paper support");
            NeedRail(Mathf.Abs(bounds.min.y-support.point.y)<.001f,"spent ticket floats above its folded-paper support");
        }
        yield return RailWalk(new Vector3(175.9f,.12f,44.7f));
        player.Aim(bin.transform.position+new Vector3(0,.715f,-.04f));yield return Capture("ticket-bin-filled-punched");
        player.Aim(bin.transform.position+new Vector3(0,.41f,-.257f));yield return null;
        NeedRail(TryInteract()&&inspected==bin,"bin label cannot trigger its normal E03 interaction");CloseInspect();
        yield return RailWalk(new Vector3(175.9f,.12f,45.8f));
        yield return RailWalk(new Vector3(183.2f,.12f,45.8f));
        DayReviewTime(1260);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);
        player.Aim(new Vector3(180,1.9f,47));yield return new WaitForSeconds(.35f);yield return Capture("ticket-office-night-rain");
        DayReviewTime(1065);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Clear);
        yield return RailWalk(new Vector3(175.9f,.12f,45.8f));
        yield return RailWalk(new Vector3(177.4f,.12f,45.8f));
        yield return RailWalk(new Vector3(188,.12f,45.5f));
        yield return RailWalk(new Vector3(188,.12f,44));
        Debug.Log("TICKET_OFFICE_REVIEW_PASS: imported support, player walking, clerk/punch/bin-label E rays and filled perforated stock");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--office-only")>=0) {
            File.WriteAllText(Path.Combine(captures,"ticket-office-results.json"),"{\"passed\":true,\"nativeUnity\":true,\"scriptedOrdinaryPlayerMotorAndRays\":true}");
            Application.Quit();
        }
    }
    IEnumerator ReviewRailDoor(RailTrain train) {
        var door=train.doors.Single(d=>d.name=="Door_C1_S_F").GetComponent<RailDoor>();
        NeedRail(door.ActionText=="关闭车门","open platform door gives a misleading open prompt");
        player.Aim(door.leafCollider.bounds.center);yield return null;
        NeedRail(TryInteract()&&!train.DoorOpen(door.transform),"ordinary E ray did not close the real door");
        yield return new WaitForSeconds(.8f);Physics.SyncTransforms();
        player.Aim(door.leafCollider.bounds.center);yield return Capture("train-door-closed");
        var ray=new Ray(train.transform.TransformPoint(new Vector3(5.9f,2.2f,-2)),train.transform.forward);
        NeedRail(door.leafCollider.Raycast(ray,out _,1),"closed doorway has no real collision");
        NeedRail(door.ActionText=="打开车门"&&TryInteract()&&train.DoorOpen(door.transform),"ordinary E ray did not reopen the door");
        yield return new WaitForSeconds(.8f);Physics.SyncTransforms();
        NeedRail(!door.leafCollider.Raycast(ray,out _,1),"open leaf still blocks the doorway");
        player.Aim(door.leafCollider.bounds.center);yield return Capture("train-door-reopened");
        Debug.Log("RAIL_DOOR_INTERACTION_PASS: platform-side E close/open, dynamic prompt and leaf collision");
    }
    IEnumerator ReviewRailDoorInside(RailTrain train) {
        var north=train.doors.Single(d=>d.name=="Door_C1_N_F").GetComponent<RailDoor>();
        player.Aim(north.leafCollider.bounds.center);yield return null;
        NeedRail(north.ActionText=="打开车门"&&TryInteract()&&train.DoorOpen(north.transform),"north-side door E open still has no effect");
        yield return new WaitForSeconds(.8f);Physics.SyncTransforms();
        var ray=new Ray(train.transform.TransformPoint(new Vector3(5.9f,2.2f,1)),train.transform.forward);
        NeedRail(!north.leafCollider.Raycast(ray,out _,1),"north door did not rotate clear of aperture");
        player.Aim(north.leafCollider.bounds.center);yield return null;
        NeedRail(TryInteract()&&!train.DoorOpen(north.transform),"north door cannot close from inside");
        yield return new WaitForSeconds(.8f);Physics.SyncTransforms();
        NeedRail(north.leafCollider.Raycast(ray,out _,1),"north door lost its closed physical leaf");
        var south=train.doors.Single(d=>d.name=="Door_C1_S_F").GetComponent<RailDoor>();
        yield return RailWalk(train.transform.TransformPoint(new Vector3(5.9f,1.05f,-1.45f)));
        player.Aim(south.leafCollider.bounds.center);yield return null;
        NeedRail(TryInteract()&&!train.DoorOpen(south.transform),"doorway safety case could not initiate close");
        yield return new WaitForSeconds(1);
        NeedRail(train.DoorOpen(south.transform),"closing door did not reopen when the passenger occupied its sweep");
        yield return Capture("train-door-occupied-safety");
        yield return RailWalk(train.transform.TransformPoint(new Vector3(5.9f,1.05f,0)));
        Debug.Log("RAIL_DOOR_INSIDE_AND_SAFETY_PASS: north-side E open/close and occupied south doorway reopens");
    }
}
