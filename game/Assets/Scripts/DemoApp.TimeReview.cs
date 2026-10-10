using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    void RequireTime(bool condition, string message) {
        if (condition) return;
        Debug.LogError("TIME_REVIEW_FAIL: " + message);
        Application.Quit(1);
        throw new InvalidOperationException(message);
    }

    void ReviewClockBoundaries() {
        var clock = new GameClock();
        clock.Advance(50, false);
        RequireTime(Math.Abs(clock.ElapsedGameMinutes - 10) < 1e-9, "50 real seconds must advance ten game minutes");
        var fullDay = new GameClock(125.75);
        double savedTimeOfDay = fullDay.MinutesOfDay;
        RequireTime(fullDay.ElapsedGameMinutes == 125.75, "Changing rate must retain existing saved cumulative minutes");
        fullDay.Advance(7200, false);
        RequireTime(Math.Abs(fullDay.ElapsedGameMinutes - 1565.75) < 1e-9 &&
            Math.Abs(fullDay.MinutesOfDay - savedTimeOfDay) < 1e-9,
            "Two real hours must advance exactly one full day and preserve the dial phase");
        fullDay.Advance(7200, true);
        RequireTime(Math.Abs(fullDay.ElapsedGameMinutes - 1565.75) < 1e-9, "A paused full day must not advance the clock");
        clock.Advance(80, true);
        clock.Advance(-1, false);
        clock.Advance(double.NaN, false);
        clock.Advance(double.PositiveInfinity, false);
        RequireTime(Math.Abs(clock.ElapsedGameMinutes - 10) < 1e-9, "Pause/invalid delta must not move time");

        var hour = new GameClock(4.75);
        hour.Advance(.5 / GameClock.GameMinutesPerRealSecond, false);
        RequireTime(Math.Abs(hour.MinutesOfDay - 1080.25) < 1e-9, "Continuous hands must cross 18:00 without jumping back");
        var midnight = new GameClock(364.75);
        midnight.Advance(.5 / GameClock.GameMinutesPerRealSecond, false);
        RequireTime(Math.Abs(midnight.MinutesOfDay - .25) < 1e-9 && midnight.ElapsedGameMinutes > 365,
            "Midnight must wrap the dial while cumulative saved time continues");
        var days = new GameClock(midnight.ElapsedGameMinutes + 3 * GameClock.MinutesPerDay);
        RequireTime(Math.Abs(days.MinutesOfDay - .25) < 1e-9, "Elapsed days must survive instead of resetting the saved clock");
        foreach (double minute in new[] { 1075.0, 1420.0, 1439.75, .25 }) {
            double tower = GameClock.WrapMinutes(minute + GameClock.TowerOffsetMinutes);
            RequireTime(Math.Abs(GameClock.WrapMinutes(tower - minute) - 30) < 1e-9,
                "Tower must stay thirty minutes ahead across midnight");
        }

        var legacy = JsonUtility.FromJson<SliceSave>("{\"scene\":\"Town\",\"entries\":[{\"id\":\"E07\"}],\"milkboyGreeted\":true}");
        RequireTime(ReadElapsedGameMinutes(legacy) == 0 && legacy.entries[0].id == "E07" && legacy.milkboyGreeted,
            "Old missing clock fields must begin at 17:55 without removing progress");
        legacy.gameClockRevision = 1; legacy.elapsedGameMinutes = -3;
        RequireTime(ReadElapsedGameMinutes(legacy) == 0, "Invalid saved time must be repaired");
    }

    IEnumerator RequireTimeFrozen(string phase, List<string> verified) {
        double before = ElapsedGameMinutes;
        RequireTime(GameTimePaused, phase + " must block the clock");
        yield return new WaitForSecondsRealtime(.3f);
        RequireTime(Math.Abs(ElapsedGameMinutes - before) < 1e-9, phase + " allowed time to advance");
        verified.Add(phase);
    }

    IEnumerator RequireReadingTime(string phase, List<string> verified) {
        double before=ElapsedGameMinutes, real=Time.unscaledTimeAsDouble;
        var position=player.transform.position;var rotation=player.eye.transform.rotation;
        RequireTime(Blocked&&!GameTimePaused,phase+" must lock movement without pausing time");
        player.scriptedDirection=Vector3.forward;
        yield return new WaitForSecondsRealtime(.4f);
        yield return new WaitForEndOfFrame();player.scriptedDirection=Vector3.zero;
        double advance=ElapsedGameMinutes-before;
        RequireTime(advance>.4 * GameClock.GameMinutesPerRealSecond * .9 && Math.Abs(advance-(Time.unscaledTimeAsDouble-real)*GameClock.GameMinutesPerRealSecond)<.003,
            phase+" must keep the ordinary watch rate");
        RequireTime(Vector3.Distance(position,player.transform.position)<.001f&&Quaternion.Angle(rotation,player.eye.transform.rotation)<.01f,
            phase+" released movement or look");
        RequireTime(DayNightCycle.Instance.AppliedElapsedMinutes==ElapsedGameMinutes,phase+" left lighting on a different time");
        RequireSharedDialHands();verified.Add(phase);
    }

    void RequireSharedDialHands() {
        var dials = FindObjectsByType<ClockDial>(FindObjectsSortMode.None);
        RequireTime(dials.Length > 0, "The scene must contain a physical clock dial");
        int towerDials = 0;
        foreach (var dial in dials) {
            RequireTime(dial.minuteHand && dial.hourHand, "Both physical clock hands must exist");
            float displayed = Minutes + (dial.streetClock ? (float)TowerClockOffsetMinutes : 0);
            RequireTime(Mathf.Abs(Mathf.DeltaAngle(dial.minuteHand.localEulerAngles.z, -(displayed % 60) * 6)) < .03f,
                "Physical minute hand does not match the shared game clock");
            RequireTime(Mathf.Abs(Mathf.DeltaAngle(dial.hourHand.localEulerAngles.z, -(displayed % 720) * .5f)) < .03f,
                "Physical hour hand does not move continuously with the shared game clock");
            if (dial.streetClock) towerDials++;
        }
        RequireTime(towerDials > 0, "At least one tower dial must use the thirty-minute offset");
    }

    IEnumerator ReviewTime() {
        paused = true;
        RequireTime(!clockSaveLoadedAtStartup && clockLoadedElapsedMinutes == 0, "Start time review with --fresh");
        RequireTime(Minutes == 1075 && !watch, "New game must begin at 17:55 with the pocket watch put away");
        ReviewClockBoundaries();
        var result = new TimeReviewResult();
        yield return RequireTimeFrozen("pause menu", result.pausedStates);
        paused = false; opening = true;
        yield return RequireTimeFrozen("opening", result.pausedStates);
        opening = false; journal = true;
        yield return RequireReadingTime("journal", result.runningReadingStates);
        journal = false; transitioning = true;
        yield return RequireTimeFrozen("transition", result.pausedStates);
        transitioning = false;
        var notice = Array.Find(FindObjectsByType<Interactable>(FindObjectsSortMode.None), i => i.evidenceId == "E07");
        RequireTime(notice, "Existing inspectable E07 notice is required for the pause check");
        Inspect(notice);
        yield return RequireReadingTime("inspection", result.runningReadingStates);
        yield return Capture("time-inspection-running");
        CloseInspect();
        OpenMilkboyDialogue();
        yield return RequireReadingTime("dialogue", result.runningReadingStates);
        ToggleDialogueHistory();
        yield return RequireReadingTime("dialogue history", result.runningReadingStates);
        yield return Capture("time-dialogue-running");
        CloseDialogue();

        RequireTime(dialTexture, "The pocket-watch face must be available");
        ToggleWatch();
        RequireTime(watch && !GameTimePaused, "The Q watch toggle must keep time running");
        yield return Capture("time-watch-before");
        double startReal = Time.unscaledTimeAsDouble, startGame = ElapsedGameMinutes;
        float firstWatchReading = watchRenderedMinutes;
        yield return new WaitForSecondsRealtime(7);
        result.realSeconds = Time.unscaledTimeAsDouble - startReal;
        result.advancedGameMinutes = ElapsedGameMinutes - startGame;
        RequireTime(Math.Abs(result.advancedGameMinutes - result.realSeconds * GameClock.GameMinutesPerRealSecond) < .08,
            "Rendered runtime did not advance at twelve game seconds per real second");
        yield return new WaitForEndOfFrame();
        RequireSharedDialHands();
        RequireTime(Mathf.Repeat(watchRenderedMinutes - firstWatchReading, 1440) > .4,
            "The visible watch hands must advance during the real-time check");
        float previousReading = watchRenderedMinutes;
        yield return new WaitForEndOfFrame();
        float frameAdvance = Mathf.Repeat(watchRenderedMinutes - previousReading, 1440);
        RequireTime(frameAdvance > 0 && frameAdvance < 1, "Watch hands must move each rendered frame, without ten-minute ticks");
        yield return Capture("time-watch-after");
        ToggleWatch();
        RequireTime(!watch && !GameTimePaused, "Putting the watch away must leave time running");
        yield return Capture("time-watch-put-away");

        // Wait for the normal fifteen-active-second autosave, with no test-only shortcut.
        double diskBefore = JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath)).elapsedGameMinutes;
        double until = Time.realtimeSinceStartupAsDouble + ClockAutosaveSeconds + 2;
        bool autosaved = false;
        while (!autosaved && Time.realtimeSinceStartupAsDouble < until) {
            yield return new WaitForSecondsRealtime(.25f);
            var disk = JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
            autosaved = disk.gameClockRevision == ClockSaveRevision && disk.elapsedGameMinutes > diskBefore + .1;
        }
        RequireTime(autosaved, "Normal play must autosave advancing time");
        paused = true;
        Save();
        var persisted = JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
        RequireTime(Math.Abs(persisted.elapsedGameMinutes - ElapsedGameMinutes) < 1e-9, "Disk save must preserve cumulative game minutes");
        var proof = new TimeSaveProof {
            elapsedGameMinutes = ElapsedGameMinutes,
            processId = System.Diagnostics.Process.GetCurrentProcess().Id,
            entries = state.entries.ConvertAll(e => e.id),
            milkboyGreetingRevision = state.milkboyGreetingRevision
        };
        File.WriteAllText(savePath + ".time-proof.json", JsonUtility.ToJson(proof, true));
        result.elapsedGameMinutes = ElapsedGameMinutes;
        result.ratePassed = result.smoothHandsPassed = result.sharedDialsPassed = result.boundariesPassed = result.autosavePassed = true;
        result.verification = "Rendered player, shared Q handler, real active-time autosave; UI handlers/flags are driven by review, physical keyboard bypassed. Relaunch separately with --verify-time-save.";
        File.WriteAllText(Path.Combine(captures, "time-results.json"), JsonUtility.ToJson(result, true));
        Debug.Log("TIME_REVIEW_PASS: continuous 12x clock, two-real-hour full day, explicit pause freezes; journal/inspection/dialogue/history and Q keep running; shared dials, boundaries, legacy fields and disk autosave; independent relaunch still required");
        Application.Quit();
    }

    IEnumerator VerifyTimeSave() {
        paused = true;
        RequireTime(clockSaveLoadedAtStartup && clockLoadedRevision == ClockSaveRevision, "Load the previous time-review save without --fresh");
        RequireTime(File.Exists(savePath + ".time-proof.json"), "Missing independent-process time checkpoint");
        var proof = JsonUtility.FromJson<TimeSaveProof>(File.ReadAllText(savePath + ".time-proof.json"));
        RequireTime(proof.processId != System.Diagnostics.Process.GetCurrentProcess().Id, "Persistence verification requires a new player process");
        RequireTime(Math.Abs(clockLoadedElapsedMinutes - proof.elapsedGameMinutes) < 1e-9,
            "Relaunch changed the saved time or added offline elapsed time");
        RequireTime(proof.entries.TrueForAll(id => state.entries.Exists(e => e.id == id)) && state.milkboyGreetingRevision == proof.milkboyGreetingRevision,
            "Relaunch must preserve evidence and dialogue together with time");
        double before = ElapsedGameMinutes;
        yield return new WaitForSecondsRealtime(.4f);
        RequireTime(ElapsedGameMinutes == before, "Reloaded paused clock must remain fixed");
        paused = false; ToggleWatch();
        yield return new WaitForSecondsRealtime(.7f);
        RequireTime(ElapsedGameMinutes > before && ElapsedGameMinutes - before < 1.5,
            "Reloaded clock must resume forwards at its original rate");
        yield return new WaitForEndOfFrame();
        RequireSharedDialHands();
        yield return Capture("time-save-relaunch");
        paused = true; Save();
        File.WriteAllText(Path.Combine(captures, "time-save-relaunch.json"), JsonUtility.ToJson(new TimeRelaunchResult {
            loadedElapsedGameMinutes = clockLoadedElapsedMinutes,
            resumedElapsedGameMinutes = ElapsedGameMinutes,
            differentProcess = true, exactSavedTime = true, noOfflineAdvance = true, progressPreserved = true
        }, true));
        Debug.Log("TIME_SAVE_RELAUNCH_PASS: exact cumulative clock loaded in a new player process, no offline advance, evidence/dialogue retained, clock resumed forwards");
        Application.Quit();
    }

    [Serializable] class TimeReviewResult {
        public double gameMinutesPerRealSecond = GameClock.GameMinutesPerRealSecond;
        public double fullDayRealSeconds = GameClock.MinutesPerDay / GameClock.GameMinutesPerRealSecond;
        public List<string> pausedStates = new List<string>();
        public List<string> runningReadingStates = new List<string>();
        public double realSeconds, advancedGameMinutes, elapsedGameMinutes;
        public bool ratePassed, smoothHandsPassed, sharedDialsPassed, boundariesPassed, autosavePassed;
        public string verification;
    }
    [Serializable] class TimeSaveProof {
        public double elapsedGameMinutes;
        public int processId, milkboyGreetingRevision;
        public List<string> entries;
    }
    [Serializable] class TimeRelaunchResult {
        public double loadedElapsedGameMinutes, resumedElapsedGameMinutes;
        public bool differentProcess, exactSavedTime, noOfflineAdvance, progressPreserved;
    }
}
