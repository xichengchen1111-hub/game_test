using System;
using UnityEngine;

// Weather follows the saved, cumulative watch clock, including its pause rules.
public static class TownWeather {
    public enum Kind { Clear, Scattered, Overcast, Rain, Mist }
    public const double SegmentMinutes = 210;
    static readonly Kind[] sequence = { Kind.Scattered, Kind.Clear, Kind.Scattered, Kind.Overcast, Kind.Rain, Kind.Overcast, Kind.Mist, Kind.Clear, Kind.Scattered, Kind.Rain, Kind.Mist, Kind.Clear };
    [Serializable] public struct State {
        public float coverage, darkness, naturalLight, directLight, rain, mist, wind;
        public static State Blend(State a, State b, float t) => new State {
            coverage=Mathf.Lerp(a.coverage,b.coverage,t), darkness=Mathf.Lerp(a.darkness,b.darkness,t),
            naturalLight=Mathf.Lerp(a.naturalLight,b.naturalLight,t), directLight=Mathf.Lerp(a.directLight,b.directLight,t),
            rain=Mathf.Lerp(a.rain,b.rain,t), mist=Mathf.Lerp(a.mist,b.mist,t), wind=Mathf.Lerp(a.wind,b.wind,t)
        };
    }
    public static State Preset(Kind kind) {
        switch(kind) {
            case Kind.Clear: return new State {coverage=.12f,darkness=.03f,naturalLight=1,directLight=1,wind=.18f};
            case Kind.Scattered: return new State {coverage=.46f,darkness=.12f,naturalLight=.92f,directLight=.83f,mist=.03f,wind=.32f};
            case Kind.Overcast: return new State {coverage=.94f,darkness=.40f,naturalLight=.70f,directLight=.24f,mist=.18f,wind=.46f};
            case Kind.Rain: return new State {coverage=1,darkness=.64f,naturalLight=.43f,directLight=.07f,rain=.85f,mist=.35f,wind=.70f};
            default: return new State {coverage=.74f,darkness=.28f,naturalLight=.72f,directLight=.40f,mist=1,wind=.10f};
        }
    }
    public static State Evaluate(double elapsed) {
        elapsed=GameClock.ValidElapsed(elapsed)?elapsed:0;
        double segment=Math.Floor(elapsed/SegmentMinutes);
        int index=(int)(segment%sequence.Length);
        float blend=DayNightCycle.Smooth(.55f,1,(float)(elapsed/SegmentMinutes-segment));
        var state=State.Blend(Preset(sequence[index]),Preset(sequence[(index+1)%sequence.Length]),blend);
        // Cloud cover builds before showers; rain fades as the sky clears.
        state.rain*=DayNightCycle.Smooth(.65f,1,state.coverage);
        return state;
    }
    public static Vector2 CloudOffset(double elapsed, bool high=false) => new Vector2(
        (float)((elapsed*(high?.004:.009))%64), (float)((elapsed*(high?-.0012:.0035))%64));
}
