using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Native acceptance for the E stone-barn garage (isolated garage-review-save.json).
// Uses the real CharacterController, the ordinary E interaction ray (TryInteract), real
// door/drawer/lamp/seat components, the 3D evidence viewer and the same dialogue/report
// handlers. Programmatic inputs, not physical keyboard/mouse; not a blind playtest.
public partial class DemoApp {
    [Serializable] class GarageReviewResult {
        public bool passed, archStartsOpen, archClosedFromStreet, officeDoorBothSides, closedBuildingMuffles, registerRead, lampSwitchedOff,
            w01Recorded, w02Recorded, recordsArePhysical, recordsDeduplicated, seatedAndStood, mechanicTopics, mechanicSpeaks, copiesQuestionNeedsW01,
            truckRead, viseRead, tagBoardRead, storeEntered, loftReached, loftDescended, drawerCardRead, rearDoorKeepsRain, yardUncovered,
            shedCoveredButOpen, yardGateOpened, unsupportedWithoutE01, supportedD06, noReinvestigation, roomLabels;
        public float loftY, reviewSeconds, gameMinutesStart, gameMinutesEnd;
        public int entries, reportSubmissions, mechanicChoices;
        public List<string> labels = new List<string>(), captures = new List<string>(), log = new List<string>();
        public string verification;
    }
    GarageReviewResult garageResult;
    Vector3 GW(float x, float y, float z) => RepairGarage.Current.transform.TransformPoint(new Vector3(x, y, z));
    void GNeed(bool ok, string why) { if (!ok) throw new Exception("GARAGE_REVIEW_FAIL: " + why); }
    void GLog(string s) { garageResult.log.Add(s); Debug.Log("GARAGE_REVIEW: " + s); }
    IEnumerator GWalk(params Vector3[] points) { foreach (var p in points) yield return WalkTo(p); }
    IEnumerator GCapture(string name) { yield return Capture("garage-" + name); garageResult.captures.Add("garage-" + name + ".png"); }
    IEnumerator GUse(Vector3 world, Func<Interactable, bool> expect, string why) {
        // Let the view settle (frame-rate independent), then re-aim once if the first ray misses.
        Interactable hitItem = null;
        for (int attempt = 0; attempt < 2 && !(hitItem && expect(hitItem)); attempt++) {
            player.Aim(world); yield return new WaitForSeconds(.12f); player.Aim(world); yield return null;
            Physics.SyncTransforms(); hitItem = null;
            if (Physics.Raycast(player.eye.transform.position, player.eye.transform.forward, out var hit, 3f)) hitItem = hit.collider.GetComponentInParent<Interactable>();
        }
        if (!(hitItem && expect(hitItem))) GLog("aim miss: eye=" + player.eye.transform.position + " forward=" + player.eye.transform.forward + " target=" + world);
        GNeed(hitItem && expect(hitItem), why + " (ray hit " + (hitItem ? hitItem.title : "nothing") + ")");
        GNeed(TryInteract(), why + " (interaction refused)");
        yield return null;
    }
    IEnumerator GLabel(string expect) {
        yield return new WaitForSeconds(.5f);
        GNeed(location.Contains(expect), "location label should contain " + expect + ": " + location);
        garageResult.labels.Add(location);
    }
    ShopMechanism GDoor(string id) => RepairGarage.Current.Door(id);
    bool GMuffled => WeatherShelter.MufflesRain(player.eye.transform.position);
    bool GCovered => WeatherShelter.Covered(player.eye.transform.position);

    IEnumerator ReviewGarage() {
        garageResult = new GarageReviewResult();
        float started = Time.realtimeSinceStartup; garageResult.gameMinutesStart = Minutes;
        yield return new WaitForSeconds(1.2f);
        GNeed(RepairGarage.Current, "repair garage prefab missing from Town");
        var L = RepairGarage.Current.Layout; float G = L.levels.ground, Y = L.levels.yard;
        // 1. Street front: both arch leaves start open; the office door starts closed.
        player.Place(GW(9.0f, .1f, -4.6f), 0); player.Aim(GW(9.0f, 3.4f, 4f));
        yield return new WaitForSeconds(.8f); yield return GCapture("street-front");
        var archL = GDoor("garage-carriage-door-left"); var archR = GDoor("garage-carriage-door-right"); var office = GDoor("garage-office-door");
        GNeed(archL && archR && archL.IsOpen && archR.IsOpen && office && !office.IsOpen, "initial door states (arch open, office closed)");
        garageResult.archStartsOpen = true;
        player.Place(GW(11.3f, .1f, -4.2f), 0); player.Aim(GW(11.3f, 2.0f, 8f)); yield return new WaitForSeconds(.5f); yield return GCapture("arch-open");
        // 2. Close both arch leaves from the street (each opened leaf stands across the paving).
        yield return GWalk(GW(11.3f, .1f, -2.6f));
        yield return GUse(GW(9.6f, 1.6f, -1.0f), i => i == archL, "close the left arch leaf");
        yield return GUse(GW(13.0f, 1.6f, -1.0f), i => i == archR, "close the right arch leaf");
        yield return new WaitForSeconds(1.4f);
        GNeed(!archL.IsOpen && !archR.IsOpen && !archL.Moving && !archR.Moving, "both arch leaves closed"); garageResult.archClosedFromStreet = true;
        player.Aim(GW(11.3f, 2.6f, 0)); yield return GCapture("arch-closed");
        // 3. Office door from the street, step in, close it again from inside.
        yield return GWalk(GW(3.7f, .1f, -1.3f));
        yield return GUse(GW(3.55f, 1.3f, .5f), i => i == office, "open the office door from the street"); yield return new WaitForSeconds(1.3f);
        GNeed(office.IsOpen, "office door opened");
        yield return GWalk(GW(3.7f, G, .9f), GW(3.0f, G, 2.0f));
        yield return GLabel("修车铺 · 接待室");
        yield return GUse(GW(4.1f, 1.3f, 1.05f), i => i == office, "close the office door from inside"); yield return new WaitForSeconds(1.3f);
        GNeed(!office.IsOpen, "office door closed from inside"); garageResult.officeDoorBothSides = true;
        GNeed(GCovered && GMuffled, "with every outer door shut the office hears the muffled rain"); garageResult.closedBuildingMuffles = true;
        player.Aim(GW(1.2f, 1.1f, 4.5f)); yield return GCapture("office");
        // 4. Customer register on the counter (ordinary inspection, no record).
        yield return GWalk(GW(1.4f, G, 2.45f));
        yield return GUse(GW(1.2f, G + 1.04f, 3.38f), i => i is PoliceDocumentItem d && d.documentId == "register", "customer register");
        GNeed(inspected && string.IsNullOrEmpty(inspected.evidenceId) && inspected.body.Contains("凯恩车间"), "register text"); garageResult.registerRead = true;
        yield return GCapture("register"); CloseInspect();
        // 5. Behind the counter: the desk lamp's manual off is saved.
        yield return GWalk(GW(4.05f, G, 2.4f), GW(4.05f, G, 3.9f));
        var officeLamp = RepairGarage.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "garage-office-lamp-off");
        yield return GUse(GW(4.8f, G + .88f, 4.55f), i => i == officeLamp, "office desk lamp switch");
        GNeed(!officeLamp.LampOn && officeLamp.lamps.All(l => !l.RequestedOn) && state.openFixtures.Contains("garage-office-lamp-off"), "lamp switched off and saved");
        garageResult.lampSwitchedOff = true;
        player.Aim(GW(7.5f, 1.4f, 5.0f)); yield return new WaitForSeconds(.3f); yield return GCapture("office-viewing-window");
        // 6. Records room: the neighbouring-city envelope and the two copies (W01, W02) as real paper.
        yield return GWalk(GW(3.3f, G, 4.1f), GW(3.3f, G, 5.9f), GW(2.6f, G, 6.5f), GW(2.6f, G, 8.2f), GW(2.6f, G, 9.95f));
        yield return GLabel("修车铺 · 账房");
        yield return GCapture("records");
        yield return GUse(GW(2.1f, G + .8f, 11.32f), i => i is PoliceDocumentItem d && d.documentId == "kane-envelope", "Kane envelope");
        GNeed(inspected && inspected.illustration, "envelope illustration"); CloseInspect();
        GNeed(!state.entries.Exists(e => e.id == "W01"), "no record before reading the copy");
        yield return GUse(GW(2.68f, G + .78f, 11.3f), i => i.evidenceId == "W01", "work-order copy W01");
        GNeed(inspected && state.entries.Count(e => e.id == "W01") == 1, "W01 recorded once"); garageResult.w01Recorded = true;
        GNeed(ModelEvidence && evidenceObject && inspected.GetComponentInChildren<EvidencePhysical>(), "W01 opens as a 3D paper sheet");
        yield return new WaitForSeconds(.5f); yield return GCapture("w01-inspect");
        ToggleEvidenceDetails(); yield return new WaitForSeconds(.2f); yield return GCapture("w01-details"); ToggleEvidenceDetails();
        CloseInspect();
        yield return GUse(GW(3.05f, G + .78f, 11.12f), i => i.evidenceId == "W02", "testimony copy W02");
        GNeed(state.entries.Count(e => e.id == "W02") == 1, "W02 recorded once"); garageResult.w02Recorded = true;
        yield return new WaitForSeconds(.4f); yield return GCapture("w02-inspect"); CloseInspect();
        yield return GUse(GW(2.68f, G + .78f, 11.3f), i => i.evidenceId == "W01", "re-read W01"); CloseInspect();
        GNeed(state.entries.Count(e => e.id == "W01") == 1, "re-reading does not duplicate W01"); garageResult.recordsDeduplicated = true; garageResult.recordsArePhysical = true;
        // 7. Records chair: sit and stand up behind it.
        yield return GUse(GW(2.6f, G + .47f, 10.55f), i => i is FurnitureSeat, "records chair");
        GNeed(FurnitureSeated, "sat on the records chair"); yield return new WaitForSeconds(.4f); yield return GCapture("seated-records");
        GNeed(RiseFromFurnitureSeat(), "stand up behind the chair"); garageResult.seatedAndStood = true;
        // 8. Into the hall through the office side door (stands open).
        yield return GWalk(GW(2.6f, G, 8.2f), GW(2.6f, G, 6.5f), GW(3.3f, G, 5.9f), GW(3.3f, G, 4.1f), GW(4.05f, G, 3.9f), GW(4.05f, G, 2.0f), GW(4.7f, G, 2.0f), GW(6.4f, G, 2.0f), GW(8.0f, G, 2.6f));
        yield return GLabel("修车铺 · 修理车间");
        player.Aim(GW(11.0f, 2.6f, 14f)); yield return GCapture("hall");
        // 9. The mechanic beside his truck: six topics now that W01 is known; he speaks his lines.
        yield return GWalk(GW(11.0f, G, 4.2f), GW(15.4f, G, 4.2f), GW(15.4f, G, 5.9f));
        var mechanic = RepairGarage.Current.GetComponentsInChildren<Interactable>().Single(i => i.title == "修车匠");
        yield return GUse(mechanic.transform.position + Vector3.up * 1.4f, i => i == mechanic, "talk to the mechanic");
        GNeed(DialogueOpen && dialogueNode.id == "greeting", "mechanic dialogue opens"); yield return GCapture("mechanic-greeting");
        AdvanceDialogue(); garageResult.mechanicChoices = dialogueChoices.Count;
        GNeed(dialogueChoices.Count == 6 && dialogueChoices.Any(c => c.requiresEvidence == "W01"), "six topics including the W01 question");
        SelectDialogue(1); AdvanceDialogue();
        GNeed(dialogueNode.id == "where" && dialogueNode.text.Contains("凯恩车间"), "his alibi line");
        var performance = mechanic.GetComponentInChildren<StationClerkPerformance>();
        GNeed(performance && performance.IsSpeaking, "mechanic body animates while speaking"); garageResult.mechanicSpeaks = true;
        yield return new WaitForSeconds(.6f); yield return GCapture("mechanic-speaking");
        AdvanceDialogue(); GNeed(dialogueChoices.Count == 6, "topic returns to the menu");
        SelectDialogue(0); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(2); AdvanceDialogue(); GNeed(dialogueNode.text.Contains("牌子"), "tool tag answer"); AdvanceDialogue();
        SelectDialogue(3); AdvanceDialogue(); GNeed(dialogueNode.id == "copies", "copies answer"); AdvanceDialogue();
        SelectDialogue(4); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(5); AdvanceDialogue(); AdvanceDialogue(); if (DialogueOpen) CloseDialogue();
        GNeed(!DialogueOpen && !MovementLocked, "dialogue closes and restores movement");
        GNeed(new[] { "mechanic.where", "mechanic.leave", "mechanic.tools", "mechanic.copies", "mechanic.board" }.All(t => state.dialogueTopics.Contains(t)), "mechanic topics saved");
        garageResult.mechanicTopics = true;
        // The copies question is filtered by the notebook, not by visit order.
        var script = DialogueScript.Load("MechanicDialogue");
        var without = new SliceSave(); garageResult.copiesQuestionNeedsW01 = script.Choices(script.Get("topics"), without).Count == 5;
        GNeed(garageResult.copiesQuestionNeedsW01, "without W01 the copies question is hidden");
        // 10. Truck and vice.
        yield return GUse(GW(14.2f, G + 1.0f, 6.4f), i => i is PoliceDocumentItem d && d.documentId == "work-truck", "work truck");
        garageResult.truckRead = true; CloseInspect();
        yield return GWalk(GW(15.4f, G, 6.6f));
        yield return GUse(GW(16.55f, G + 1.0f, 8.05f), i => i is PoliceDocumentItem d && d.documentId == "bench-vise", "bench vice");
        garageResult.viseRead = true; yield return GCapture("vise"); CloseInspect();
        // 11. Back of the hall: tool-tag board, tool store, parts drawer.
        yield return GWalk(GW(15.4f, G, 4.2f), GW(11.0f, G, 4.2f), GW(10.9f, G, 11.0f), GW(9.4f, G, 13.3f));
        yield return GUse(GW(7.6f, G + 1.6f, 14.66f), i => i is PoliceDocumentItem d && d.documentId == "tag-board", "tool tag board");
        GNeed(inspected.body.Contains("空着"), "tag board text"); garageResult.tagBoardRead = true; yield return GCapture("tag-board"); CloseInspect();
        yield return GWalk(GW(9.4f, G, 14.2f), GW(9.4f, G, 15.7f));
        yield return GLabel("修车铺 · 工具库"); garageResult.storeEntered = true;
        player.Aim(GW(8.0f, 1.2f, 18.4f)); yield return GCapture("tool-store");
        yield return GWalk(GW(9.4f, G, 14.2f), GW(10.7f, G, 13.3f), GW(12.8f, G, 13.3f), GW(12.8f, G, 16.75f));
        yield return GLabel("修车铺 · 车间后场");
        var drawer = GDoor("garage-parts-drawer");
        yield return GUse(drawer.transform.position, i => i == drawer, "parts drawer"); yield return new WaitForSeconds(1.0f);
        GNeed(drawer.IsOpen && !drawer.Moving, "drawer opened");
        yield return GUse(drawer.transform.position + new Vector3(0, .12f, .16f), i => i is PoliceDocumentItem d && d.documentId == "tag-blanks", "drawer contents");
        garageResult.drawerCardRead = true; yield return GCapture("drawer"); CloseInspect();
        yield return GUse(drawer.transform.position + new Vector3(0, -.05f, 0), i => i == drawer, "close the drawer"); yield return new WaitForSeconds(1.0f);
        GNeed(!drawer.IsOpen, "drawer pushed back");
        // 12. Rear door: inside muffled while everything is shut, outdoor rain once it opens.
        yield return GWalk(GW(14.5f, G, 17.5f));
        var rear = GDoor("garage-rear-door"); GNeed(rear && !rear.IsOpen, "rear door starts closed");
        GNeed(GCovered && GMuffled, "closed hall muffles the rain");
        yield return GUse(GW(14.5f, 1.3f, 18.9f), i => i == rear, "open the rear door"); yield return new WaitForSeconds(1.4f);
        GNeed(rear.IsOpen && GCovered && !GMuffled, "open rear door keeps the outdoor rain"); garageResult.rearDoorKeepsRain = true;
        // 13. Rear yard, the lean-to shed (roofed, acoustically open) and the yard gate.
        yield return GWalk(GW(14.5f, Y, 19.6f), GW(14.5f, Y, 21.5f));
        yield return GLabel("修车铺 · 后院");
        GNeed(!GCovered, "yard is open to the sky"); garageResult.yardUncovered = true;
        player.Aim(GW(9.0f, 2.6f, 18.5f)); yield return GCapture("yard");
        yield return GWalk(GW(8.4f, Y, 21.4f));
        GNeed(GCovered && !GMuffled, "the shed is roofed but open"); garageResult.shedCoveredButOpen = true;
        yield return GUse(GW(6.3f, Y + .8f, 20.4f), i => i is PoliceDocumentItem d && d.documentId == "shed-rack", "shed rack"); CloseInspect();
        var gate = GDoor("garage-yard-gate"); GNeed(gate && !gate.IsOpen, "yard gate starts closed");
        yield return GWalk(GW(12.0f, Y, 25.0f), GW(15.6f, Y, 28.6f));
        yield return GUse(GW(14.5f, 1.0f, 29.93f), i => i == gate, "open the yard gate"); yield return new WaitForSeconds(1.4f);
        GNeed(gate.IsOpen, "yard gate opened");
        yield return GWalk(GW(14.6f, Y, 29.2f), GW(14.6f, .0f, 31.2f));
        GNeed(!location.Contains("修车铺 · "), "outside the fence: " + location); garageResult.yardGateOpened = true;
        player.Aim(GW(12.0f, 2.0f, 20.0f)); yield return GCapture("gate-from-lane");
        yield return GWalk(GW(14.6f, Y, 29.2f), GW(14.5f, Y, 21.5f), GW(14.5f, Y, 19.7f), GW(14.5f, G, 17.6f));
        // 14. Real stair up to the loft over the tool store, then back down and up again to save there.
        yield return GWalk(GW(12.8f, G, 15.5f), GW(12.8f, G, 13.3f), GW(10.9f, G, 11.0f), GW(6.0f, G, 10.6f));
        player.Aim(GW(6.0f, 2.2f, 14.5f)); yield return GCapture("stair-foot");
        yield return GWalk(GW(6.0f, G, 14.95f), GW(7.6f, G, 15.1f), GW(7.6f, G, 16.4f));
        GNeed(player.transform.position.y > L.levels.loft - .1f, "reached the loft, y=" + player.transform.position.y);
        garageResult.loftY = player.transform.position.y; garageResult.loftReached = true;
        yield return GLabel("修车铺 · 储物阁楼");
        player.Aim(GW(9.6f, 3.4f, 18.3f)); yield return GCapture("loft");
        yield return GUse(GW(7.0f, L.levels.loft + .45f, 17.7f), i => i is PoliceDocumentItem d && d.documentId == "loft-crates", "loft crates"); CloseInspect();
        player.Aim(GW(13.0f, 1.0f, 8.0f)); yield return GCapture("loft-over-hall");
        yield return GWalk(GW(7.6f, G, 15.1f), GW(6.0f, G, 14.95f), GW(6.0f, G, 10.6f));
        GNeed(player.transform.position.y < G + .2f, "back down on the hall floor"); garageResult.loftDescended = true;
        // 15. Stage report from the notebook: S6 "trip" with W01+W02 needs the case summary E01 as well.
        OpenPoliceReport(); GNeed(PoliceReportOpen, "report opens");
        SelectReportStatement(5); SelectReportKind(2); ToggleReportCard("W01"); ToggleReportCard("W02");
        GNeed(SubmitPoliceReport() == null, "S6 without the case summary is not supported"); garageResult.unsupportedWithoutE01 = true;
        ClosePoliceReport();
        // Review fixture: the E01 case summary normally comes from the police reading room.
        state.entries.Add(new NotebookEntry { id = "E01", title = "案卷摘要（副本）", source = "garage review fixture", body = "review fixture" }); Save();
        OpenPoliceReport(); SelectReportStatement(5); SelectReportKind(2); ToggleReportCard("E01"); ToggleReportCard("W01"); ToggleReportCard("W02");
        GNeed(SubmitPoliceReport() == "D06" && state.reportSupported.Contains("D06"), "S6 supported by E01+W01+W02"); garageResult.supportedD06 = true;
        GNeed(!state.reportReinvestigation, "a non-core rule never closes the case"); garageResult.noReinvestigation = true;
        yield return GCapture("report-d05"); ClosePoliceReport();
        // 16. East lane view of the cross gables, then finish in the loft for the relaunch check.
        player.Place(GW(21.5f, .1f, 2.0f), -40); player.Aim(GW(17.6f, 4.2f, 9.0f)); yield return new WaitForSeconds(.6f); yield return GCapture("lane-east");
        player.Place(GW(10.9f, G + .02f, 11.0f), 0); yield return new WaitForSeconds(.4f);
        yield return GWalk(GW(6.0f, G, 10.6f), GW(6.0f, G, 14.95f), GW(7.6f, G, 15.1f), GW(7.6f, G, 16.4f));
        GNeed(player.transform.position.y > L.levels.loft - .1f, "back in the loft for saving");
        garageResult.roomLabels = true;
        Save();
        garageResult.entries = state.entries.Count; garageResult.reportSubmissions = state.reportSubmissions.Count;
        garageResult.reviewSeconds = Time.realtimeSinceStartup - started; garageResult.gameMinutesEnd = Minutes;
        garageResult.passed = true;
        garageResult.verification = Application.platform + " " + SystemInfo.graphicsDeviceType + " " + Screen.width + "x" + Screen.height + ": real CharacterController and E-ray interactions; programmatic inputs, not physical keyboard/mouse or a blind playtest. E01 was injected as a labelled review fixture for the report rule.";
        File.WriteAllText(Path.Combine(captures, "garage-review-results.json"), JsonUtility.ToJson(garageResult, true));
        Debug.Log("GARAGE_REVIEW_PASS: doors, acoustics, records, mechanic, drawer, yard, loft stair and report rule");
        Application.Quit();
    }

    // Fixed daylight gallery of the garage (staged overcast noon and a clear afternoon), for visual review only.
    IEnumerator ReviewGarageGallery() {
        yield return new WaitForSeconds(1.2f);
        GNeed(RepairGarage.Current, "repair garage prefab missing from Town");
        var L = RepairGarage.Current.Layout; float G = L.levels.ground, Y = L.levels.yard, F = L.levels.loft;
        var views = new (string name, Vector3 feet, Vector3 aim)[] {
            ("street-wide", GW(3.0f, .1f, -8.0f), GW(9.5f, 4.2f, 4f)),
            ("arch", GW(11.3f, .1f, -5.2f), GW(11.3f, 2.6f, 5f)),
            ("office-front", GW(2.6f, .1f, -3.4f), GW(2.2f, 2.4f, 0f)),
            ("lane-south", GW(23.0f, .1f, -3.0f), GW(14.5f, 4.2f, 10f)),
            ("lane-mid", GW(21.5f, .1f, 9.0f), GW(17.6f, 4.8f, 8.6f)),
            ("yard", GW(14.0f, Y, 28.6f), GW(9.0f, 3.0f, 18.5f)),
            ("hall-from-arch", GW(11.3f, G, 1.2f), GW(11.0f, 2.6f, 14f)),
            ("hall-to-door", GW(9.0f, G, 13.2f), GW(12.0f, 2.2f, 1f)),
            ("office", GW(3.0f, G, 1.0f), GW(1.5f, 1.3f, 6f)),
            ("loft", GW(7.6f, F, 16.4f), GW(12.5f, 1.4f, 7f)),
        };
        foreach (var (weather, minutes, kind) in new[] { ("overcast", 785.0, TownWeather.Kind.Overcast), ("clear", 875.0, TownWeather.Kind.Clear) })
            foreach (var v in views) {
                if (weather == "clear" && v.name.StartsWith("hall-") || weather == "clear" && v.name == "office" || weather == "clear" && v.name == "loft") continue;
                DayReviewTime(minutes); DayNightCycle.Instance.SetReviewWeather(kind);
                player.Place(v.feet, 0); player.Aim(v.aim);
                yield return new WaitForSeconds(.9f);
                yield return Capture("garage-day-" + weather + "-" + v.name);
            }
        Debug.Log("GARAGE_GALLERY_PASS: staged daylight views");
        Application.Quit();
    }

    IEnumerator VerifyGarageSave() {
        garageResult = new GarageReviewResult();
        yield return new WaitForSeconds(1.5f);
        GNeed(RepairGarage.Current, "repair garage prefab missing");
        GNeed(state.entries.Count(e => e.id == "W01") == 1 && state.entries.Count(e => e.id == "W02") == 1, "W01/W02 persisted once");
        GNeed(state.reportSupported.Contains("D06") && !state.reportReinvestigation, "report progress persisted");
        GNeed(state.dialogueTopics.Contains("mechanic.where") && state.dialogueTopics.Contains("mechanic.copies"), "mechanic topics persisted");
        var lamp = RepairGarage.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "garage-office-lamp-off");
        GNeed(!lamp.LampOn && lamp.lamps.All(l => !l.RequestedOn), "manual lamp off survives relaunch");
        GNeed(!GDoor("garage-carriage-door-left").IsOpen && !GDoor("garage-carriage-door-right").IsOpen && !GDoor("garage-office-door").IsOpen, "closed arch and office door persisted");
        GNeed(GDoor("garage-rear-door").IsOpen && GDoor("garage-yard-gate").IsOpen && !GDoor("garage-parts-drawer").IsOpen, "rear door/gate/drawer states persisted");
        GNeed(player.transform.position.y > RepairGarage.Current.Layout.levels.loft - .2f, "relaunched in the loft: " + player.transform.position);
        yield return new WaitForSeconds(.6f);
        GNeed(location.Contains("储物阁楼"), "room label after relaunch: " + location);
        player.Aim(GW(9.6f, 3.4f, 18.3f)); yield return GCapture("reload-loft");
        garageResult.passed = true; garageResult.entries = state.entries.Count; garageResult.reportSubmissions = state.reportSubmissions.Count;
        garageResult.verification = "Separate process reading garage-review-save.json: records, report, topics, lamp, door/gate/drawer states and the loft position.";
        File.WriteAllText(Path.Combine(captures, "garage-save-results.json"), JsonUtility.ToJson(garageResult, true));
        Debug.Log("GARAGE_SAVE_PASS: records, report, topics, lamp, doors and loft position restored");
        Application.Quit();
    }
}
