using System;

// One continuous clock for the pocket watch and every physical town dial.
// Store elapsed game minutes, never wall-clock timestamps or a wrapped time of day.
public sealed class GameClock {
    public const double InitialMinutes = 17 * 60 + 55;
    // A new game opens at 21:00 (user, 2026-10-14: the first act and the arrival at the hospital are late
    // at night). It starts with these minutes already elapsed, so saved games, whose elapsed minutes
    // count from 17:55, keep their time of day.
    public const double OpeningMinutes = 21 * 60;
    public const double OpeningElapsed = OpeningMinutes - InitialMinutes;
    // Twelve game seconds per real second: one complete 24-hour day takes
    // two real hours. Existing saves retain their cumulative game minutes.
    public const double GameMinutesPerRealSecond = 12.0 / 60.0;
    public const double MinutesPerDay = 24 * 60;
    public const double TowerOffsetMinutes = 30;

    public double ElapsedGameMinutes { get; private set; }
    public double MinutesOfDay => WrapMinutes(InitialMinutes + ElapsedGameMinutes);

    public GameClock(double elapsedGameMinutes = 0) {
        if (!ValidElapsed(elapsedGameMinutes)) throw new ArgumentOutOfRangeException(nameof(elapsedGameMinutes));
        ElapsedGameMinutes = elapsedGameMinutes;
    }

    public void Advance(double realSeconds, bool paused) {
        if (paused || realSeconds <= 0 || double.IsNaN(realSeconds) || double.IsInfinity(realSeconds)) return;
        double next = ElapsedGameMinutes + realSeconds * GameMinutesPerRealSecond;
        if (ValidElapsed(next)) ElapsedGameMinutes = next;
    }

    public static bool ValidElapsed(double value) => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
    public static double WrapMinutes(double minutes) => (minutes % MinutesPerDay + MinutesPerDay) % MinutesPerDay;
}
