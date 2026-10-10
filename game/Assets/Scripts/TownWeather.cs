using System;
using UnityEngine;

// Weather follows the saved, cumulative watch clock, including its pause rules.
public static class TownWeather {
    public enum Kind { Clear, Scattered, Overcast, Rain, Mist }
    public const float TransitionMinutes = 5;
    // Watch-time starts, repeated every day. Each new period eases in over five
    // game minutes; cloud motion retains its cumulative phase across midnight.
    static readonly int[] starts = { 0, 330, 380, 550, 580, 670, 710, 860, 890, 1000, 1050, 1070, 1100, 1270, 1310 };
    static readonly Kind[] sequence = {
        Kind.Overcast, Kind.Rain, Kind.Overcast, Kind.Clear, Kind.Overcast,
        Kind.Rain, Kind.Overcast, Kind.Clear, Kind.Overcast, Kind.Rain,
        Kind.Overcast, Kind.Rain, Kind.Overcast, Kind.Rain, Kind.Overcast
    };
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
            case Kind.Clear: return new State {coverage=.28f,darkness=.06f,naturalLight=1,directLight=.66f,mist=.07f,wind=.18f};
            case Kind.Scattered: return new State {coverage=.66f,darkness=.24f,naturalLight=.85f,directLight=.70f,mist=.06f,wind=.32f};
            case Kind.Overcast: return new State {coverage=.99f,darkness=.62f,naturalLight=.57f,directLight=.13f,mist=.74f,wind=.46f};
            case Kind.Rain: return new State {coverage=1,darkness=.78f,naturalLight=.38f,directLight=.04f,rain=1,mist=.90f,wind=.70f};
            default: return new State {coverage=.74f,darkness=.28f,naturalLight=.72f,directLight=.40f,mist=1,wind=.10f};
        }
    }
    public static State Evaluate(double elapsed) {
        elapsed=GameClock.ValidElapsed(elapsed)?elapsed:0;
        double minute=GameClock.WrapMinutes(GameClock.InitialMinutes+elapsed);
        int index=starts.Length-1;
        while(index>0 && minute<starts[index])index--;
        int previous=(index+sequence.Length-1)%sequence.Length;
        float blend=DayNightCycle.Smooth(0,TransitionMinutes,(float)(minute-starts[index]));
        var state=State.Blend(Preset(sequence[previous]),Preset(sequence[index]),blend);
        // Cloud cover builds before showers; rain fades as the sky clears.
        state.rain*=DayNightCycle.Smooth(.65f,1,state.coverage);
        return state;
    }
    public static Vector2 CloudOffset(double elapsed, bool high=false) => new Vector2(
        (float)((elapsed*(high?.004:.009))%64), (float)((elapsed*(high?-.0012:.0035))%64));
}
