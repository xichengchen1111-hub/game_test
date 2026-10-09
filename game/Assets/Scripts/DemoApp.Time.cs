using UnityEngine;

public partial class DemoApp {
    const int ClockSaveRevision = 1;
    const double ClockAutosaveSeconds = 15;
    GameClock gameClock = new GameClock();
    double clockSecondsSinceSave;
    double clockLoadedElapsedMinutes;
    int clockLoadedRevision;
    bool clockSaveLoadedAtStartup;

    public double ElapsedGameMinutes => gameClock.ElapsedGameMinutes;
    public bool GameTimePaused => !sceneReady || Blocked || (Review && dayNightReviewFrozen);

    static double ReadElapsedGameMinutes(SliceSave saved) =>
        saved.gameClockRevision > 0 && GameClock.ValidElapsed(saved.elapsedGameMinutes) ? saved.elapsedGameMinutes : 0;

    void InitializeGameClock(bool loadedFromDisk) {
        clockSaveLoadedAtStartup = loadedFromDisk;
        clockLoadedRevision = state.gameClockRevision;
        clockLoadedElapsedMinutes = ReadElapsedGameMinutes(state);
        if (state.gameClockRevision > 0 && !GameClock.ValidElapsed(state.elapsedGameMinutes))
            Debug.LogWarning("Invalid saved clock repaired; position, evidence and dialogue retained.");
        gameClock = new GameClock(clockLoadedElapsedMinutes);
        // Missing fields in older saves begin at 17:55. No offline elapsed time is added.
        StoreGameClock();
    }

    void UpdateGameClock() {
        bool clockPaused = GameTimePaused;
        gameClock.Advance(Time.unscaledDeltaTime, clockPaused);
        if (clockPaused) return;
        clockSecondsSinceSave += Time.unscaledDeltaTime;
        if (clockSecondsSinceSave >= ClockAutosaveSeconds) Save();
    }

    void StoreGameClock() {
        state.gameClockRevision = ClockSaveRevision;
        state.elapsedGameMinutes = gameClock.ElapsedGameMinutes;
        clockSecondsSinceSave = 0;
    }
}
