using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Native acceptance for the A gabled old inn (isolated hotel-review-save.json). Uses the real
// CharacterController, the ordinary E interaction ray (TryInteract), real door/lamp/seat
// components, the tilting register lamp, the 3D evidence viewer and the same dialogue/report
// handlers. Programmatic inputs, not physical keyboard/mouse; not a blind playtest. The porter
// and the landlady are placeholders (a teammate makes the people).
public partial class DemoApp {
    [Serializable] class HotelReviewResult {
        public bool passed, frontStartsOpen, frontClosedFromInside, closedInnMuffles, registerReadI01, nextPagePlainWithoutLamp, lampLowered,
            rakingRevealsI02, recordsArePhysical, recordsDeduplicated, seatedAtRegister, porterTestimonyI04, porterTopics, lodgeRead,
            officeDoorOpened, officeLampOff, diningLedgerI03, keeperTopics, fireplaceRead, kitchenReached, backDoorKeepsRain, yardUncovered,
            carportCoveredButOpen, carAtStand, stairClimbed, roomSixRead, roomOneOpened, unsupportedWithoutI02, supportedD07, noReinvestigation, roomLabels;
        public float upperY, reviewSeconds, gameMinutesStart, gameMinutesEnd, carOffset;
        public int entries, reportSubmissions, porterChoices, keeperChoices;
        public List<string> labels = new List<string>(), captures = new List<string>(), log = new List<string>();
        public string verification;
    }
    HotelReviewResult hotelResult;
    Vector3 HtW(float x, float y, float z) => HotelInn.Current.transform.TransformPoint(new Vector3(x, y, z));
    void HtNeed(bool ok, string why) { if (!ok) throw new Exception("HOTEL_REVIEW_FAIL: " + why); }
    void HtLog(string s) { hotelResult.log.Add(s); Debug.Log("HOTEL_REVIEW: " + s); }
    IEnumerator HtWalk(params Vector3[] points) { foreach (var p in points) yield return WalkTo(p); }
    IEnumerator HtCapture(string name) { yield return Capture("hotel-" + name); hotelResult.captures.Add("hotel-" + name + ".png"); }
    IEnumerator HtUse(Vector3 world, Func<Interactable, bool> expect, string why) {
        Interactable hitItem = null;
        for (int attempt = 0; attempt < 2 && !(hitItem && expect(hitItem)); attempt++) {
            player.Aim(world); yield return new WaitForSeconds(.12f); player.Aim(world); yield return null;
            Physics.SyncTransforms(); hitItem = null;
            if (Physics.Raycast(player.eye.transform.position, player.eye.transform.forward, out var hit, 3f)) hitItem = hit.collider.GetComponentInParent<Interactable>();
        }
        if (!(hitItem && expect(hitItem))) HtLog("aim miss: eye=" + player.eye.transform.position + " forward=" + player.eye.transform.forward + " target=" + world);
        HtNeed(hitItem && expect(hitItem), why + " (ray hit " + (hitItem ? hitItem.title : "nothing") + ")");
        HtNeed(TryInteract(), why + " (interaction refused)");
        yield return null;
    }
    IEnumerator HtLabel(string expect) {
        yield return new WaitForSeconds(.5f);
        HtNeed(location.Contains(expect), "location label should contain " + expect + ": " + location);
        hotelResult.labels.Add(location);
    }
    ShopMechanism HtDoor(string id) => HotelInn.Current.Door(id);
    bool HtMuffled => WeatherShelter.MufflesRain(player.eye.transform.position);
    bool HtCovered => WeatherShelter.Covered(player.eye.transform.position);
    HotelActor HtActor(string role) => HotelInn.Current.GetComponentsInChildren<HotelActor>().Single(a => a.role == role);
    static bool HtDoc(Interactable i, string id) => i is PoliceDocumentItem d && d.documentId == id;

    IEnumerator ReviewHotel() {
        hotelResult = new HotelReviewResult();
        float started = Time.realtimeSinceStartup; hotelResult.gameMinutesStart = Minutes;
        yield return new WaitForSeconds(1.2f);
        HtNeed(HotelInn.Current, "hotel prefab missing from Town");
        var L = HotelInn.Current.Layout; float G = L.levels.ground, U = L.levels.upper, Y = L.levels.yard, DT = G + .76f;
        // 1. Street: the front door pair starts open, the kitchen back door closed.
        player.Place(HtW(11.6f, .1f, 41.6f), 180); player.Aim(HtW(11.6f, 4.0f, 30f));
        yield return new WaitForSeconds(.8f); yield return HtCapture("street-front");
        var frontL = HtDoor("hotel-front-door-left"); var frontR = HtDoor("hotel-front-door-right"); var back = HtDoor("hotel-back-door");
        HtNeed(frontL && frontR && frontL.IsOpen && frontR.IsOpen && back && !back.IsOpen, "initial door states (front open, back closed)");
        hotelResult.frontStartsOpen = true;
        // 2. Through the walk gate and the porch into the hall.
        yield return HtWalk(HtW(11.6f, .1f, 38.0f), HtW(11.6f, .1f, 36.0f), HtW(11.6f, G, 34.0f));
        player.Aim(HtW(11.6f, 2.2f, 32f)); yield return HtCapture("porch");
        yield return HtWalk(HtW(11.6f, G, 31.2f), HtW(11.6f, G, 29.8f));
        yield return HtLabel("旅馆 · 登记大堂");
        HtNeed(HtCovered && !HtMuffled, "with the front door open the hall hears the outdoor rain");
        player.Aim(HtW(14.0f, 1.6f, 23.0f)); yield return HtCapture("hall");
        // 3. Close both front leaves from inside: now every outer door is shut.
        yield return HtWalk(HtW(11.6f, G, 30.9f));
        yield return HtUse(HtW(10.86f, 1.3f, 31.95f), i => i == frontL, "close the left front leaf");
        yield return HtUse(HtW(12.34f, 1.3f, 31.95f), i => i == frontR, "close the right front leaf");
        yield return new WaitForSeconds(1.4f);
        HtNeed(!frontL.IsOpen && !frontR.IsOpen && !frontL.Moving && !frontR.Moving, "front leaves closed"); hotelResult.frontClosedFromInside = true;
        HtNeed(HtCovered && HtMuffled, "with every outer door shut the hall hears muffled rain"); hotelResult.closedInnMuffles = true;
        // 4. The register desk: I01 on the left page; the next page reads blank until the lamp is lowered.
        yield return HtWalk(HtW(9.0f, G, 28.0f), HtW(8.55f, G, 25.6f));
        var lamp = HotelInn.Current.RegisterLamp;
        HtNeed(lamp && !lamp.Lowered && lamp.plainPage.activeInHierarchy && !lamp.rakedPage.activeInHierarchy, "lamp up, plain next page showing");
        player.Aim(HtW(7.08f, DT, 25.35f)); yield return new WaitForSeconds(.3f); yield return HtCapture("register-desk");
        HtNeed(!state.entries.Exists(e => e.id == "I01"), "no record before reading the register");
        yield return HtUse(HtW(7.08f, DT + .05f, 25.15f), i => i.evidenceId == "I01", "register page I01");
        HtNeed(inspected && state.entries.Count(e => e.id == "I01") == 1 && inspected.body.Contains("八时十五分"), "I01 recorded once with its legible time");
        HtNeed(ModelEvidence && evidenceObject && inspected.GetComponentInChildren<EvidencePhysical>(), "I01 opens as a 3D paper sheet");
        yield return new WaitForSeconds(.5f); yield return HtCapture("i01-inspect");
        ToggleEvidenceDetails(); yield return new WaitForSeconds(.2f); yield return HtCapture("i01-details"); ToggleEvidenceDetails();
        CloseInspect(); hotelResult.registerReadI01 = true;
        yield return HtUse(HtW(7.08f, DT + .05f, 25.55f), i => HtDoc(i, "register-next"), "the next page under ordinary light");
        HtNeed(string.IsNullOrEmpty(inspected.evidenceId) && !state.entries.Exists(e => e.id == "I02"), "plain next page records nothing");
        yield return HtCapture("next-page-plain"); CloseInspect(); hotelResult.nextPagePlainWithoutLamp = true;
        // Lower the shade: the light grazes the paper and the pressed writing appears.
        yield return HtUse(HtW(6.86f, DT + .32f, 24.82f), i => i == lamp, "press the desk lamp's shade down");
        yield return new WaitForSeconds(1.2f);
        HtNeed(lamp.Lowered && lamp.Fraction > .99f && lamp.rakedPage.activeInHierarchy && !lamp.plainPage.activeInHierarchy && lamp.raking.enabled, "lamp lowered, raked page showing");
        HtNeed(state.openFixtures.Contains("hotel-register-lamp"), "lamp position saved"); hotelResult.lampLowered = true;
        player.Aim(HtW(7.08f, DT, 25.4f)); yield return new WaitForSeconds(.3f); yield return HtCapture("register-raking");
        yield return HtUse(HtW(7.08f, DT + .05f, 25.55f), i => i.evidenceId == "I02", "pressed writing I02");
        HtNeed(state.entries.Count(e => e.id == "I02") == 1 && inspected.body.Contains("六时三十五分"), "I02 recorded with the pressed time");
        yield return new WaitForSeconds(.5f); yield return HtCapture("i02-inspect"); CloseInspect(); hotelResult.rakingRevealsI02 = true;
        yield return HtUse(HtW(7.08f, DT + .05f, 25.15f), i => i.evidenceId == "I01", "re-read I01"); CloseInspect();
        HtNeed(state.entries.Count(e => e.id == "I01") == 1, "re-reading does not duplicate I01"); hotelResult.recordsDeduplicated = true;
        // Sit at the desk and stand up again.
        yield return HtUse(HtW(7.78f, G + .47f, 25.3f), i => i is FurnitureSeat, "register desk chair");
        HtNeed(FurnitureSeated, "sat at the register desk"); yield return new WaitForSeconds(.4f); yield return HtCapture("seated-register");
        HtNeed(RiseFromFurnitureSeat(), "stand up behind the chair"); hotelResult.seatedAtRegister = true;
        // 5. The porter behind the counter (placeholder figure): asking about the 20th records I04.
        yield return HtWalk(HtW(11.5f, G, 26.0f), HtW(13.3f, G, 25.6f));
        var porter = HtActor("porter");
        HtNeed(porter.name.Contains("占位"), "porter is a placeholder marker");
        yield return HtUse(porter.transform.position + Vector3.up * 1.4f, i => i == porter, "talk to the porter");
        HtNeed(DialogueOpen && dialogueNode.id == "greeting", "porter dialogue opens"); yield return HtCapture("porter-greeting");
        AdvanceDialogue(); hotelResult.porterChoices = dialogueChoices.Count; HtNeed(dialogueChoices.Count == 6, "six porter topics");
        HtNeed(!state.entries.Exists(e => e.id == "I04"), "no testimony before asking");
        SelectDialogue(0); AdvanceDialogue();
        HtNeed(dialogueNode.id == "seen" && state.entries.Count(e => e.id == "I04") == 1, "asking about the 20th records I04"); hotelResult.porterTestimonyI04 = true;
        yield return HtCapture("porter-testimony");
        AdvanceDialogue(); AdvanceDialogue(); AdvanceDialogue(); HtNeed(dialogueChoices.Count == 6, "back at the porter's topics");
        SelectDialogue(1); AdvanceDialogue(); HtNeed(dialogueNode.text.Contains("帽子"), "how he knows the face"); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(2); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(3); AdvanceDialogue(); HtNeed(dialogueNode.text.Contains("进站"), "what was asked"); AdvanceDialogue();
        SelectDialogue(4); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(5); AdvanceDialogue(); AdvanceDialogue(); if (DialogueOpen) CloseDialogue();
        HtNeed(!DialogueOpen && !MovementLocked, "dialogue closes and restores movement");
        HtNeed(new[] { "hotel.porter.seen", "hotel.porter.sure", "hotel.porter.time", "hotel.porter.asked", "hotel.porter.watch" }.All(t => state.dialogueTopics.Contains(t)), "porter topics saved");
        hotelResult.porterTopics = true;
        // 6. Porter's lodge (door stands open).
        yield return HtWalk(HtW(9.2f, G, 30.4f), HtW(7.3f, G, 30.6f), HtW(6.0f, G, 31.55f), HtW(4.6f, G, 30.9f));
        yield return HtLabel("旅馆 · 门房");
        yield return HtUse(HtW(6.0f, DT + .03f, 29.85f), i => HtDoc(i, "lodge-log"), "porter's log");
        HtNeed(inspected.body.Contains("六号房"), "lodge log text"); CloseInspect(); hotelResult.lodgeRead = true;
        player.Aim(HtW(3.0f, 1.2f, 31.9f)); yield return HtCapture("lodge");
        // 7. Landlady's office: open its door from the hall, read the police letter, switch the desk lamp off.
        yield return HtWalk(HtW(6.0f, G, 31.55f), HtW(7.3f, G, 30.6f), HtW(9.3f, G, 29.0f), HtW(9.3f, G, 23.3f), HtW(7.6f, G, 22.9f));
        var office = HtDoor("hotel-office-door"); HtNeed(office && !office.IsOpen, "office door starts closed");
        yield return HtUse(HtW(6.48f, 1.3f, 22.6f), i => i == office, "open the office door"); yield return new WaitForSeconds(1.3f);
        HtNeed(office.IsOpen, "office door opened"); hotelResult.officeDoorOpened = true;
        yield return HtWalk(HtW(5.6f, G, 22.6f), HtW(3.4f, G, 24.6f));
        yield return HtLabel("旅馆 · 账房");
        yield return HtUse(HtW(4.0f, DT + .02f, 26.55f), i => HtDoc(i, "police-letter"), "police letter");
        HtNeed(inspected.body.Contains("折角"), "letter text"); CloseInspect();
        var officeLamp = HotelInn.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "hotel-office-lamp-off");
        yield return HtUse(HtW(5.0f, DT + .12f, 26.75f), i => i == officeLamp, "office desk lamp switch");
        HtNeed(!officeLamp.LampOn && state.openFixtures.Contains("hotel-office-lamp-off"), "office lamp off and saved"); hotelResult.officeLampOff = true;
        player.Aim(HtW(4.4f, 1.2f, 28.0f)); yield return HtCapture("office");
        // 8. Dining room: the dining ledger (I03), the landlady (placeholder) and the fireplace.
        yield return HtWalk(HtW(5.6f, G, 22.6f), HtW(7.6f, G, 23.3f), HtW(10.0f, G, 28.6f), HtW(13.0f, G, 28.8f), HtW(16.0f, G, 28.95f), HtW(17.6f, G, 28.95f), HtW(17.6f, G, 30.4f));
        yield return HtLabel("旅馆 · 餐厅");
        player.Aim(HtW(21.0f, 1.2f, 24.0f)); yield return HtCapture("dining");
        yield return HtUse(HtW(17.25f, DT + .04f, 31.35f), i => i.evidenceId == "I03", "dining ledger I03");
        HtNeed(state.entries.Count(e => e.id == "I03") == 1 && inspected.body.Contains("晚餐：不用"), "I03 recorded");
        yield return new WaitForSeconds(.4f); yield return HtCapture("i03-inspect"); CloseInspect(); hotelResult.diningLedgerI03 = true;
        hotelResult.recordsArePhysical = new[] { "I01", "I02", "I03" }.All(id => HotelInn.Current.GetComponentsInChildren<PoliceDocumentItem>(true).Single(d => d.evidenceId == id).GetComponentInChildren<EvidencePhysical>(true));
        HtNeed(hotelResult.recordsArePhysical, "records are physical sheets");
        yield return HtWalk(HtW(18.7f, G, 29.9f));
        var keeper = HtActor("keeper");
        yield return HtUse(keeper.transform.position + Vector3.up * 1.4f, i => i == keeper, "talk to the landlady");
        HtNeed(DialogueOpen && dialogueNode.id == "greeting", "landlady dialogue opens");
        AdvanceDialogue(); hotelResult.keeperChoices = dialogueChoices.Count; HtNeed(dialogueChoices.Count == 6, "six landlady topics");
        SelectDialogue(0); AdvanceDialogue(); HtNeed(dialogueNode.text.Contains("台灯"), "where the page lies"); AdvanceDialogue();
        SelectDialogue(1); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(2); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(3); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(4); AdvanceDialogue(); AdvanceDialogue();
        SelectDialogue(5); AdvanceDialogue(); AdvanceDialogue(); if (DialogueOpen) CloseDialogue();
        HtNeed(!state.entries.Exists(e => e.id != "I01" && e.id != "I02" && e.id != "I03" && e.id != "I04"), "the landlady grants nothing");
        HtNeed(new[] { "hotel.keeper.page", "hotel.keeper.six", "hotel.keeper.clock" }.All(t => state.dialogueTopics.Contains(t)), "landlady topics saved"); hotelResult.keeperTopics = true;
        yield return HtWalk(HtW(19.2f, G, 28.4f), HtW(21.2f, G, 28.4f));
        yield return HtUse(HtW(21.92f, G + .7f, 27.4f), i => HtDoc(i, "fireplace"), "dining fireplace"); CloseInspect(); hotelResult.fireplaceRead = true;
        // 9. Kitchen through the dining room's back door, then out of the back door into the yard.
        yield return HtWalk(HtW(19.25f, G, 28.4f), HtW(19.25f, G, 25.5f), HtW(19.4f, G, 22.4f), HtW(19.4f, G, 20.4f), HtW(19.4f, G, 19.0f));
        yield return HtLabel("旅馆 · 厨房"); hotelResult.kitchenReached = true;
        player.Aim(HtW(16.9f, 1.2f, 19.3f)); yield return HtCapture("kitchen");
        yield return HtWalk(HtW(21.4f, G, 19.6f));
        yield return HtUse(HtW(22.6f, G + 1.05f, 20.3f), i => HtDoc(i, "order-spike"), "order spike"); CloseInspect();
        yield return HtWalk(HtW(21.3f, G, 18.5f), HtW(21.3f, G, 15.6f), HtW(20.0f, G, 15.3f));
        HtNeed(HtCovered && HtMuffled, "closed inn muffles the rain in the kitchen");
        yield return HtUse(HtW(20.5f, 1.3f, 13.25f), i => i == back, "open the back door"); yield return new WaitForSeconds(1.4f);
        HtNeed(back.IsOpen && HtCovered && !HtMuffled, "open back door keeps the outdoor rain"); hotelResult.backDoorKeepsRain = true;
        yield return HtWalk(HtW(20.2f, G, 14.6f), HtW(20.2f, .06f, 12.8f), HtW(20.2f, Y, 11.2f));
        yield return HtLabel("旅馆 · 后院");
        HtNeed(!HtCovered, "yard is open to the sky"); hotelResult.yardUncovered = true;
        player.Aim(HtW(18.0f, 3.5f, 18.0f)); yield return HtCapture("rear-yard");
        // 10. Round the service wing to the carport: roofed but open, the hotel car on its stand.
        yield return HtWalk(HtW(31.6f, Y, 11.2f), HtW(31.6f, Y, 23.0f), HtW(29.3f, Y, 25.8f), HtW(27.9f, Y, 25.8f));
        yield return HtLabel("旅馆 · 车棚");
        HtNeed(HtCovered && !HtMuffled, "carport is roofed but open"); hotelResult.carportCoveredButOpen = true;
        var car = FindFirstObjectByType<HotelCar>();
        HtNeed(car, "hotel car present");
        var carLocal = HotelInn.Current.transform.InverseTransformPoint(car.transform.position);
        hotelResult.carOffset = new Vector2(carLocal.x - L.car.x, carLocal.z - L.car.z).magnitude;
        HtNeed(hotelResult.carOffset < .5f, "hotel car stands under the carport: offset " + hotelResult.carOffset); hotelResult.carAtStand = true;
        player.Aim(HtW(25.5f, 1.0f, 29.0f)); yield return HtCapture("carport-car");
        // 11. Back inside and up the stair to the landing, the corridor and room six.
        yield return HtWalk(HtW(29.3f, Y, 25.8f), HtW(31.6f, Y, 23.0f), HtW(31.6f, Y, 11.2f), HtW(20.2f, Y, 11.2f), HtW(20.2f, .06f, 12.8f), HtW(20.2f, G, 14.6f), HtW(21.3f, G, 15.6f),
            HtW(21.3f, G, 18.5f), HtW(19.4f, G, 20.4f), HtW(19.4f, G, 22.4f), HtW(19.25f, G, 25.5f), HtW(19.2f, G, 28.95f), HtW(16.0f, G, 28.95f), HtW(13.0f, G, 28.0f),
            HtW(13.0f, G, 23.8f), HtW(15.6f, G, 23.5f), HtW(15.6f, G, 22.05f));
        player.Aim(HtW(10.0f, 2.6f, 22.05f)); yield return HtCapture("stair-foot");
        yield return HtWalk(HtW(14.3f, G, 22.05f), HtW(12.0f, G, 22.05f), HtW(9.65f, U, 22.05f), HtW(9.65f, U, 24.4f));
        HtNeed(player.transform.position.y > U - .1f, "reached the upper floor, y=" + player.transform.position.y);
        hotelResult.upperY = player.transform.position.y; hotelResult.stairClimbed = true;
        yield return HtLabel("旅馆 · 楼梯平台");
        player.Aim(HtW(13.0f, 1.0f, 22.0f)); yield return HtCapture("landing-well");
        yield return HtWalk(HtW(9.65f, U, 26.4f), HtW(20.6f, U, 26.4f), HtW(20.6f, U, 24.2f));
        yield return HtLabel("旅馆 · 六号房");
        player.Aim(HtW(19.3f, U + .6f, 22.4f)); yield return HtCapture("room6");
        yield return HtWalk(HtW(21.0f, U, 23.0f));
        yield return HtUse(HtW(19.28f, U + .62f, 22.6f), i => HtDoc(i, "room6-bed"), "room six bed"); CloseInspect();
        yield return HtUse(HtW(22.0f, U + .55f, 21.72f), i => HtDoc(i, "room6-stove"), "room six stove");
        HtNeed(inspected.body.Contains("没烧透"), "stove text"); CloseInspect(); hotelResult.roomSixRead = true;
        player.Aim(HtW(26.0f, 1.0f, 28.0f)); yield return HtCapture("room6-window");
        // Room one, closed: open it from the corridor and look in.
        yield return HtWalk(HtW(20.6f, U, 24.2f), HtW(20.6f, U, 26.4f), HtW(5.0f, U, 26.4f));
        var room1 = HtDoor("hotel-room-1-door"); HtNeed(room1 && !room1.IsOpen, "room one starts closed");
        yield return HtUse(HtW(5.0f, U + 1.2f, 27.28f), i => i == room1, "open room one"); yield return new WaitForSeconds(1.3f);
        HtNeed(room1.IsOpen, "room one opened");
        yield return HtWalk(HtW(5.4f, U, 28.6f));
        yield return HtLabel("旅馆 · 一号房"); hotelResult.roomOneOpened = true;
        player.Aim(HtW(6.4f, U + .5f, 30.6f)); yield return HtCapture("room1");
        // 12. Stage report: S2 "trip" needs E01 + I01 + I02 + I04 (I03 supports); without I02 it fails.
        OpenPoliceReport(); HtNeed(PoliceReportOpen, "report opens");
        state.entries.Add(new NotebookEntry { id = "E01", title = "案卷摘要（副本）", source = "hotel review fixture", body = "review fixture" }); Save();
        SelectReportStatement(1); SelectReportKind(2); ToggleReportCard("E01"); ToggleReportCard("I01"); ToggleReportCard("I04");
        HtNeed(SubmitPoliceReport() == null, "S2 trip without the pressed page is not supported"); hotelResult.unsupportedWithoutI02 = true;
        ClosePoliceReport();
        OpenPoliceReport(); SelectReportStatement(1); SelectReportKind(2); ToggleReportCard("E01"); ToggleReportCard("I01"); ToggleReportCard("I02"); ToggleReportCard("I04"); ToggleReportCard("I03");
        HtNeed(SubmitPoliceReport() == "D07" && state.reportSupported.Contains("D07"), "S2 trip supported by E01+I01+I02+I04"); hotelResult.supportedD07 = true;
        HtNeed(!state.reportReinvestigation, "a non-core rule never closes the case"); hotelResult.noReinvestigation = true;
        yield return HtCapture("report-d07"); ClosePoliceReport();
        // 13. Finish in room six for the relaunch check.
        yield return HtWalk(HtW(5.4f, U, 26.4f), HtW(20.6f, U, 26.4f), HtW(20.6f, U, 24.2f));
        hotelResult.roomLabels = true;
        Save();
        hotelResult.entries = state.entries.Count; hotelResult.reportSubmissions = state.reportSubmissions.Count;
        hotelResult.reviewSeconds = Time.realtimeSinceStartup - started; hotelResult.gameMinutesEnd = Minutes;
        hotelResult.passed = true;
        hotelResult.verification = Application.platform + " " + SystemInfo.graphicsDeviceType + " " + Screen.width + "x" + Screen.height + ": real CharacterController and E-ray interactions; programmatic inputs, not physical keyboard/mouse or a blind playtest. E01 was injected as a labelled review fixture for the report rule. Porter and landlady are placeholder markers.";
        File.WriteAllText(Path.Combine(captures, "hotel-review-results.json"), JsonUtility.ToJson(hotelResult, true));
        Debug.Log("HOTEL_REVIEW_PASS: doors, acoustics, register and raking lamp, records, porter and landlady, kitchen, yard, carport, stair and report rule");
        Application.Quit();
    }

    // Fixed daylight gallery of the inn (staged overcast noon and a clear afternoon), for visual review only.
    IEnumerator ReviewHotelGallery() {
        yield return new WaitForSeconds(1.2f);
        HtNeed(HotelInn.Current, "hotel prefab missing from Town");
        var L = HotelInn.Current.Layout; float G = L.levels.ground, U = L.levels.upper, Y = L.levels.yard;
        var views = new (string name, Vector3 feet, Vector3 aim)[] {
            ("street-wide", HtW(3.0f, .1f, 47.0f), HtW(13.0f, 5.0f, 28f)),
            ("street-east", HtW(36.0f, .1f, 44.0f), HtW(17.0f, 4.5f, 27f)),
            ("porch", HtW(11.6f, .1f, 38.5f), HtW(11.6f, 3.0f, 33f)),
            ("carport", HtW(33.0f, .1f, 36.0f), HtW(25.5f, 1.8f, 28f)),
            ("rear", HtW(26.0f, Y, 4.0f), HtW(18.0f, 4.0f, 18f)),
            ("hall", HtW(15.6f, G, 31.6f), HtW(8.0f, 1.2f, 24f)),
            ("hall-stair", HtW(9.0f, G, 30.0f), HtW(13.0f, 2.2f, 22f)),
            ("register", HtW(8.6f, G, 25.8f), HtW(7.0f, .9f, 25.3f)),
            ("dining", HtW(17.4f, G, 22.6f), HtW(21.0f, 1.2f, 31f)),
            ("kitchen", HtW(22.2f, G, 14.2f), HtW(17.0f, 1.2f, 19.5f)),
            ("corridor", HtW(3.2f, U, 26.4f), HtW(22.0f, U + 1.2f, 26.4f)),
            ("room6", HtW(21.8f, U, 25.0f), HtW(19.0f, U + .6f, 21.6f)),
        };
        foreach (var (weather, minutes, kind) in new[] { ("overcast", 785.0, TownWeather.Kind.Overcast), ("clear", 875.0, TownWeather.Kind.Clear) })
            foreach (var v in views) {
                if (weather == "clear" && !v.name.StartsWith("street") && v.name != "porch" && v.name != "carport" && v.name != "rear") continue;
                DayReviewTime(minutes); DayNightCycle.Instance.SetReviewWeather(kind);
                player.Place(v.feet, 0); player.Aim(v.aim);
                yield return new WaitForSeconds(.9f);
                yield return Capture("hotel-day-" + weather + "-" + v.name);
            }
        Debug.Log("HOTEL_GALLERY_PASS: staged daylight views");
        Application.Quit();
    }

    IEnumerator VerifyHotelSave() {
        hotelResult = new HotelReviewResult();
        yield return new WaitForSeconds(1.5f);
        HtNeed(HotelInn.Current, "hotel prefab missing");
        HtNeed(new[] { "I01", "I02", "I03", "I04" }.All(id => state.entries.Count(e => e.id == id) == 1), "I01-I04 persisted once");
        HtNeed(state.reportSupported.Contains("D07") && !state.reportReinvestigation, "report progress persisted");
        HtNeed(state.dialogueTopics.Contains("hotel.porter.seen") && state.dialogueTopics.Contains("hotel.keeper.page"), "conversation topics persisted");
        var lamp = HotelInn.Current.RegisterLamp;
        HtNeed(lamp.Lowered && lamp.rakedPage.activeInHierarchy && !lamp.plainPage.activeInHierarchy, "lowered register lamp survives relaunch");
        var officeLamp = HotelInn.Current.GetComponentsInChildren<LampSwitch>().Single(s => s.offId == "hotel-office-lamp-off");
        HtNeed(!officeLamp.LampOn && officeLamp.lamps.All(l => !l.RequestedOn), "manual lamp off survives relaunch");
        HtNeed(!HtDoor("hotel-front-door-left").IsOpen && !HtDoor("hotel-front-door-right").IsOpen && HtDoor("hotel-back-door").IsOpen && HtDoor("hotel-office-door").IsOpen && HtDoor("hotel-room-1-door").IsOpen, "door states persisted");
        HtNeed(player.transform.position.y > HotelInn.Current.Layout.levels.upper - .2f, "relaunched upstairs: " + player.transform.position);
        yield return new WaitForSeconds(.6f);
        HtNeed(location.Contains("六号房"), "room label after relaunch: " + location);
        player.Aim(HtW(19.3f, HotelInn.Current.Layout.levels.upper + .6f, 22.4f)); yield return HtCapture("reload-room6");
        // Porter re-visit opens on the revisit line.
        yield return HtWalk(HtW(20.6f, HotelInn.Current.Layout.levels.upper, 26.4f));
        hotelResult.passed = true; hotelResult.entries = state.entries.Count; hotelResult.reportSubmissions = state.reportSubmissions.Count;
        hotelResult.verification = "Separate process reading hotel-review-save.json: records, report, topics, register lamp, office lamp, door states and the room-six position.";
        File.WriteAllText(Path.Combine(captures, "hotel-save-results.json"), JsonUtility.ToJson(hotelResult, true));
        Debug.Log("HOTEL_SAVE_PASS: records, report, topics, lamps, doors and room-six position restored");
        Application.Quit();
    }
}
