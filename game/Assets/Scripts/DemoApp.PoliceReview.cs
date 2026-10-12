using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Native acceptance for the D police station (isolated police-review-save.json).
// Uses the real CharacterController, the ordinary E interaction ray (TryInteract),
// real door/drawer/lamp/seat components and the same report/dialogue handlers.
// Programmatic inputs, not physical keyboard/mouse; not a blind playtest.
public partial class DemoApp {
    [Serializable] class PoliceReviewResult {
        public bool passed, frontDoorsOpened, e01Recorded, e01Deduplicated, e08Recorded, drawerCardRead, lampSwitchedOff,
            seatedAndStood, officerDialogue, unsupportedReportKept, supportedDateReport, courtDoorBothSides, openDoorKeepsRain,
            courtUncovered, loggiaCoveredButOpen, holdingDoorOpened, upperFloorReached, permitRead, roomLabel;
        public float upperFloorY, reviewSeconds, gameMinutesStart, gameMinutesEnd;
        public int entries, reportSubmissions;
        public List<string> captures = new List<string>(), log = new List<string>();
        public string verification;
    }
    PoliceReviewResult policeResult;
    Vector3 PW(float x, float y, float z) => PoliceStation.Current.transform.TransformPoint(new Vector3(x, y, z));
    void PNeed(bool ok, string why) { if (!ok) throw new Exception("POLICE_REVIEW_FAIL: " + why); }
    void PLog(string s) { policeResult.log.Add(s); Debug.Log("POLICE_REVIEW: " + s); }
    IEnumerator PWalk(params Vector3[] points) { foreach (var p in points) yield return WalkTo(p); }
    IEnumerator PCapture(string name) { yield return Capture("police-" + name); policeResult.captures.Add("police-" + name + ".png"); }
    IEnumerator PUse(Vector3 world, Func<Interactable, bool> expect, string why) {
        player.Aim(world); yield return null; yield return null;
        Physics.SyncTransforms();
        Interactable hitItem = null;
        if (Physics.Raycast(player.eye.transform.position, player.eye.transform.forward, out var hit, 3f)) hitItem = hit.collider.GetComponentInParent<Interactable>();
        PNeed(hitItem && expect(hitItem), why + " (ray hit " + (hitItem ? hitItem.title : "nothing") + ")");
        PNeed(TryInteract(), why + " (interaction refused)");
        yield return null;
    }
    ShopMechanism PDoor(string id) => PoliceStation.Current.Door(id);

    IEnumerator ReviewPolice() {
        policeResult = new PoliceReviewResult();
        float started = Time.realtimeSinceStartup; policeResult.gameMinutesStart = Minutes;
        yield return new WaitForSeconds(1.2f);
        PNeed(PoliceStation.Current, "police station prefab missing from Town");
        var L = PoliceStation.Current.Layout; float G = L.levels.ground, U = L.levels.upper;
        // 1. Street front and both leaves of the public door (left starts open, right closed).
        player.Place(PW(11.5f, .1f, -3.2f), 0); player.Aim(PW(11.5f, 3.2f, 0));
        yield return new WaitForSeconds(.6f); yield return PCapture("street-front");
        var left = PDoor("police-front-door-left"); var right = PDoor("police-front-door-right");
        PNeed(left && right && left.IsOpen && !right.IsOpen, "front door initial state (left open, right closed)");
        yield return PWalk(PW(11.5f, .1f, -1.1f));
        yield return PUse(PW(11.86f, 1.3f, .35f), i => i == right, "open the right front leaf");
        yield return new WaitForSeconds(1.3f);
        PNeed(left.IsOpen && right.IsOpen, "both front leaves open"); policeResult.frontDoorsOpened = true;
        yield return PCapture("front-doors-open");
        // 2. Hall, stage report from the entrance desk: an unsupported submission keeps exploration.
        yield return PWalk(PW(11.5f, G, 1.6f), PW(11.4f, G, 2.3f));
        yield return new WaitForSeconds(.5f);
        PNeed(location.Contains("警署"), "location label inside the station: " + location); policeResult.roomLabel = true;
        yield return PCapture("hall");
        yield return PUse(PW(10.38f, G + .9f, 2.3f), i => i is PoliceReportDesk, "report desk");
        PNeed(PoliceReportOpen && MovementLocked, "report opens and locks movement");
        SubmitPoliceReport(); PNeed(LastReportRule == null && reportFeedback.Contains("先选"), "report requires statement and judgment");
        SelectReportStatement(1); SelectReportKind(1);
        PNeed(SubmitPoliceReport() == null && !state.reportReinvestigation, "unsupported date report");
        yield return PCapture("report-unsupported");
        ClosePoliceReport(); PNeed(!PoliceReportOpen && !MovementLocked, "report closes and restores movement");
        policeResult.unsupportedReportKept = true;
        // 3. Reading room: E01 copies deduplicate, E08 copy from the comparison desk.
        yield return PWalk(PW(11.5f, G, 4.0f), PW(8.6f, G, 4.0f), PW(2.6f, G, 3.95f));
        yield return PCapture("reading-room");
        yield return PUse(PW(1.75f, G + .79f, 2.25f), i => i.evidenceId == "E01", "public desk E01 copy");
        PNeed(inspected && state.entries.Count(e => e.id == "E01") == 1, "E01 recorded once"); policeResult.e01Recorded = true;
        yield return PCapture("e01-copy"); CloseInspect();
        yield return PUse(PW(3.15f, G + .79f, 2.28f), i => i is PoliceDocumentItem d && d.documentId == "letter-photo", "letter photograph");
        PNeed(inspected && inspected.illustration, "letter photograph illustration"); yield return PCapture("letter-photo"); CloseInspect();
        yield return PWalk(PW(7.2f, G, 3.95f));
        yield return PUse(PW(6.75f, G + .96f, 2.2f), i => i.evidenceId == "E01", "comparison E01 copy");
        CloseInspect(); PNeed(state.entries.Count(e => e.id == "E01") == 1, "E01 copy deduplicated"); policeResult.e01Deduplicated = true;
        yield return PUse(PW(7.65f, G + .96f, 2.2f), i => i.evidenceId == "E08", "comparison E08 copy");
        PNeed(state.entries.Count(e => e.id == "E08") == 1, "E08 recorded once"); policeResult.e08Recorded = true;
        yield return PCapture("e08-copy"); CloseInspect();
        // 4. Reading lamp manual off (saved), sitting at the public table.
        yield return PWalk(PW(4.4f, G, 3.75f));
        var lampSwitch = PoliceStation.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "police-read-lamp-1-off");
        yield return PUse(PW(3.9f, G + .9f, 2.55f), i => i == lampSwitch, "reading lamp switch");
        PNeed(!lampSwitch.LampOn && lampSwitch.lamps.All(l => !l.RequestedOn) && state.openFixtures.Contains("police-read-lamp-1-off"), "lamp switched off and saved");
        policeResult.lampSwitchedOff = true;
        yield return PWalk(PW(3.4f, G, 4.1f));
        yield return PUse(PW(3.4f, G + .47f, 3.15f), i => i is FurnitureSeat, "reading chair");
        PNeed(FurnitureSeated, "sat on the reading chair"); yield return new WaitForSeconds(.4f); yield return PCapture("seated-reading");
        PNeed(RiseFromFurnitureSeat(), "stand up behind the chair"); policeResult.seatedAndStood = true;
        // 5. Classification drawer: pull it open, read the card, push it back.
        yield return PWalk(PW(2.0f, G, 4.9f));
        var drawer = PoliceStation.Current.Door("police-index-drawer-1");
        yield return PUse(PW(.92f, G + .66f, 4.9f), i => i == drawer, "date index drawer");
        yield return new WaitForSeconds(1.0f); PNeed(drawer.IsOpen && !drawer.Moving, "drawer opened");
        var front = drawer.transform.position;
        yield return PUse(front + new Vector3(-.2f, .55f, 0), i => i is PoliceDocumentItem d && d.documentId == "index-date", "date index card");
        PNeed(inspected, "index card readable"); policeResult.drawerCardRead = true; yield return PCapture("index-card"); CloseInspect();
        yield return PUse(drawer.transform.position + new Vector3(0, -.05f, 0), i => i == drawer, "close drawer"); yield return new WaitForSeconds(1.0f);
        PNeed(!drawer.IsOpen, "drawer pushed back");
        // 6. Duty officer through the hatch; dialogue hands over the report; supported date report.
        yield return PWalk(PW(4.5f, G, 4.2f), PW(4.5f, G, 8.0f), PW(11.9f, G, 8.0f), PW(11.9f, G, 8.15f));
        yield return PUse(PW(11.9f, G + 1.35f, 9.72f), i => i.title == "值班警员", "duty officer");
        PNeed(DialogueOpen, "officer dialogue opens"); yield return PCapture("officer-dialogue");
        AdvanceDialogue(); PNeed(dialogueChoices.Count == 6, "six officer topics");
        SelectDialogue(0); AdvanceDialogue(); AdvanceDialogue(); PNeed(dialogueChoices.Count == 6, "topic returns to the menu");
        SelectDialogue(4); AdvanceDialogue(); PNeed(DialogueOpen && dialogueNode.id == "report", "officer report line");
        AdvanceDialogue(); PNeed(!DialogueOpen && PoliceReportOpen, "officer hands over the report"); policeResult.officerDialogue = true;
        SelectReportStatement(1); SelectReportKind(1); ToggleReportCard("E01"); ToggleReportCard("E08");
        PNeed(SubmitPoliceReport() == "D03" && state.reportSupported.Contains("D03") && !state.reportReinvestigation, "supported date report without automatic closure");
        policeResult.supportedDateReport = true; yield return PCapture("report-supported"); ClosePoliceReport();
        // 7. Court door from inside; any open door of the front range keeps outdoor rain sound.
        yield return PWalk(PW(7.36f, G, 8.0f), PW(7.6f, G, 10.4f));
        var courtDoor = PDoor("police-court-door"); PNeed(courtDoor && !courtDoor.IsOpen, "court door starts closed");
        PNeed(WeatherShelter.Covered(player.eye.transform.position) && !WeatherShelter.MufflesRain(player.eye.transform.position), "open front door keeps outdoor sound in the front range");
        yield return PUse(PW(7.36f, 1.3f, 11.65f), i => i == courtDoor, "open court door from inside"); yield return new WaitForSeconds(1.3f);
        PNeed(courtDoor.IsOpen, "court door opened"); policeResult.openDoorKeepsRain = !WeatherShelter.MufflesRain(player.eye.transform.position);
        yield return PWalk(PW(7.36f, G, 11.0f), PW(7.36f, G, 12.9f), PW(10.5f, L.levels.court, 15.5f));
        PNeed(!WeatherShelter.Covered(player.eye.transform.position), "court is open to the sky"); policeResult.courtUncovered = true;
        player.Aim(PW(11.5f, 2.0f, 24f)); yield return new WaitForSeconds(.4f); yield return PCapture("court");
        yield return PWalk(PW(6.3f, G, 15.2f));
        PNeed(WeatherShelter.Covered(player.eye.transform.position) && !WeatherShelter.MufflesRain(player.eye.transform.position), "loggia is roofed but acoustically open");
        policeResult.loggiaCoveredButOpen = true; player.Aim(PW(6.3f, 1.6f, 22f)); yield return PCapture("west-loggia");
        // 8. Old holding cell: push the unlocked iron door from the east loggia, look inside.
        yield return PWalk(PW(7.6f, L.levels.court, 15.2f), PW(13.5f, L.levels.court, 14.5f), PW(15.2f, L.levels.court, 16.0f), PW(15.2f, L.levels.court, 20.0f), PW(16.75f, G, 20.0f), PW(16.75f, G, 20.6f));
        var holding = PDoor("police-holding-door"); PNeed(holding && !holding.IsOpen, "holding door closed");
        yield return PUse(PW(17.85f, 1.2f, 20.6f), i => i == holding, "holding cell iron door"); yield return new WaitForSeconds(1.3f);
        PNeed(holding.IsOpen, "holding door opened"); policeResult.holdingDoorOpened = true;
        yield return PWalk(PW(18.9f, G, 20.6f), PW(19.6f, G, 21.4f));
        yield return new WaitForSeconds(.5f); PNeed(location.Contains("旧羁押室"), "holding cell label: " + location);
        player.Aim(PW(22.2f, .6f, 21.7f)); yield return PCapture("holding-cell");
        yield return PUse(PW(22.1f, G + .8f, 21.7f), i => i is PoliceDocumentItem, "holding bed"); CloseInspect();
        // 9. Court door from outside: close, reopen, walk in and close it again from inside.
        yield return PWalk(PW(18.9f, G, 20.6f), PW(16.75f, G, 20.6f), PW(16.75f, G, 20.0f), PW(15.2f, L.levels.court, 20.0f), PW(15.2f, L.levels.court, 16.0f), PW(13.5f, L.levels.court, 14.5f), PW(7.36f, L.levels.court, 13.1f));
        yield return PUse(PW(6.92f, 1.3f, 11.2f), i => i == courtDoor, "close court door from the court"); yield return new WaitForSeconds(1.3f);
        PNeed(!courtDoor.IsOpen, "court door closed from outside");
        yield return PUse(PW(7.36f, 1.3f, 11.65f), i => i == courtDoor, "reopen court door from the court"); yield return new WaitForSeconds(1.3f);
        PNeed(courtDoor.IsOpen, "court door reopened from outside");
        yield return PWalk(PW(7.36f, G, 12.6f), PW(7.36f, G, 11.0f), PW(7.6f, G, 10.4f));
        yield return PUse(PW(6.92f, 1.3f, 11.2f), i => i == courtDoor, "close court door from inside"); yield return new WaitForSeconds(1.3f);
        PNeed(!courtDoor.IsOpen, "court door closed from inside"); policeResult.courtDoorBothSides = true;
        // 10. Stairs: real controller up both flights to the first floor and the chief's office.
        yield return PWalk(PW(7.36f, G, 8.0f), PW(16.0f, G, 8.0f), PW(18.0f, G, 7.6f), PW(18.6f, G, 6.6f), PW(21.7f, G, 6.6f));
        player.Aim(PW(21.7f, 1.6f, 10f)); yield return PCapture("stair-foot");
        yield return PWalk(PW(21.7f, G, 10.5f), PW(19.9f, G, 10.5f), PW(19.9f, G, 6.5f));
        PNeed(player.transform.position.y > U - .1f, "reached the upper landing, y=" + player.transform.position.y);
        policeResult.upperFloorY = player.transform.position.y; policeResult.upperFloorReached = true;
        yield return PWalk(PW(18.5f, U, 6.5f), PW(17.5f, U, 7.6f), PW(6.6f, U, 7.6f), PW(6.6f, U, 5.0f), PW(4.2f, U, 3.4f));
        yield return new WaitForSeconds(.5f); PNeed(location.Contains("负责人办公室"), "upper room label: " + location);
        player.Aim(PW(3.8f, U + .8f, 1.0f)); yield return PCapture("chief-office");
        yield return PUse(PW(3.6f, U + .79f, 1.5f), i => i is PoliceDocumentItem d && d.documentId == "permit-stub", "permit stub");
        policeResult.permitRead = true; CloseInspect();
        Save();
        policeResult.entries = state.entries.Count; policeResult.reportSubmissions = state.reportSubmissions.Count;
        policeResult.reviewSeconds = Time.realtimeSinceStartup - started; policeResult.gameMinutesEnd = Minutes;
        policeResult.passed = true;
        policeResult.verification = Application.platform + " " + SystemInfo.graphicsDeviceType + " " + Screen.width + "x" + Screen.height + ": real CharacterController and E-ray interactions; programmatic inputs, not physical keyboard/mouse or a blind playtest.";
        File.WriteAllText(Path.Combine(captures, "police-review-results.json"), JsonUtility.ToJson(policeResult, true));
        Debug.Log("POLICE_REVIEW_PASS: doors, documents, drawer, lamp, seat, officer, report, court acoustics, holding cell and both floors");
        Application.Quit();
    }

    IEnumerator VerifyPoliceSave() {
        policeResult = new PoliceReviewResult();
        yield return new WaitForSeconds(1.5f);
        PNeed(PoliceStation.Current, "police station prefab missing");
        PNeed(state.entries.Count(e => e.id == "E01") == 1 && state.entries.Count(e => e.id == "E08") == 1, "E01/E08 persisted once");
        PNeed(state.reportSupported.Contains("D03") && !state.reportReinvestigation && state.reportSubmissions.Count == 2, "report progress persisted (unsupported + supported)");
        var lampSwitch = PoliceStation.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "police-read-lamp-1-off");
        PNeed(!lampSwitch.LampOn && lampSwitch.lamps.All(l => !l.RequestedOn), "manual lamp off survives relaunch");
        PNeed(PDoor("police-front-door-left").IsOpen && PDoor("police-front-door-right").IsOpen, "front leaves stay open");
        PNeed(!PDoor("police-court-door").IsOpen && PDoor("police-holding-door").IsOpen && !PDoor("police-index-drawer-1").IsOpen, "court/holding/drawer states persisted");
        PNeed(player.transform.position.y > PoliceStation.Current.Layout.levels.upper - .2f, "relaunched on the upper floor: " + player.transform.position);
        yield return new WaitForSeconds(.6f);
        PNeed(location.Contains("负责人办公室"), "room label after relaunch: " + location);
        player.Aim(PW(3.8f, PoliceStation.Current.Layout.levels.upper + .8f, 1.0f)); yield return PCapture("reload-chief-office");
        policeResult.passed = true; policeResult.entries = state.entries.Count; policeResult.reportSubmissions = state.reportSubmissions.Count;
        policeResult.verification = "Separate process reading police-review-save.json: evidence, report, lamp, door/drawer states and indoor upper-floor position.";
        File.WriteAllText(Path.Combine(captures, "police-save-results.json"), JsonUtility.ToJson(policeResult, true));
        Debug.Log("POLICE_SAVE_PASS: evidence, report, lamp, doors and upper-floor position restored");
        Application.Quit();
    }
}
