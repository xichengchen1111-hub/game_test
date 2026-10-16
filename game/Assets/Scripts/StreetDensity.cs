using UnityEngine;

// How busy the streets are hour by hour (user, 2026-10-15: many people and cars by day, few at
// night). A January 1931 small town, after the design brief (docs/research/street-life-1931.md):
// almost nobody before six; workers and schoolchildren in the morning; shoppers and the elderly
// through the late morning and afternoon; a second rush at five; the evening thins quickly after
// supper and the street is nearly empty late at night, apart from a few men going home (no open
// saloons: Prohibition). Rain sends people indoors far more than it stops cars.
// The hourly shares are design values built from traffic and pedestrian count curves, not 1931 data.
public static class StreetDensity {
    public enum Folk { Man, Woman, OldMan, OldWoman, Boy, Girl, Worker, DogWalker, Beggar, Musician }
    // Share of the busiest hour, 0..1, at each whole hour 0..24 (24 repeats 0); interpolated.
    static readonly float[] People={.02f,.01f,.01f,.01f,.02f,.08f,.3f,.8f,1f,.8f,.82f,.86f,1f,.78f,.72f,.9f,.97f,.92f,.78f,.45f,.28f,.16f,.08f,.05f,.02f};
    static readonly float[] Cars={.04f,.02f,.02f,.02f,.05f,.15f,.35f,.8f,.95f,.72f,.68f,.72f,.9f,.72f,.68f,.76f,.85f,1f,.9f,.55f,.35f,.22f,.12f,.07f,.04f};
    static float Curve(float[] table,float hour){hour=Mathf.Repeat(hour,24);int h=(int)hour;return Mathf.Lerp(table[h],table[h+1],hour-h);}
    public static float PeopleShare(float hour,float rain)=>Curve(People,hour)*(1-.45f*Mathf.Clamp01(rain));
    public static float CarShare(float hour,float rain)=>Curve(Cars,hour)*(1-.1f*Mathf.Clamp01(rain));
    // Two-hour-ish bands: 0–5, 5–7, 7–9, 9–12, 12–13, 13–15, 15–17, 17–19, 19–22, 22–24.
    static int Band(float hour){hour=Mathf.Repeat(hour,24);return hour<5?0:hour<7?1:hour<9?2:hour<12?3:hour<13?4:hour<15?5:hour<17?6:hour<19?7:hour<22?8:9;}
    static readonly float[] Workers={.02f,.35f,1,.2f,.8f,.2f,.3f,1,.25f,.05f};
    static readonly float[] Business={0,.05f,.7f,.5f,.9f,.5f,.5f,.9f,.2f,.03f};
    static readonly float[] NightFigures={.4f,.1f,0,0,0,0,0,.1f,.6f,1};
    static readonly float[] Shoppers={0,.02f,.3f,1,.4f,.8f,.7f,.3f,.05f,0};
    static readonly float[] Elderly={0,.02f,.2f,1,.4f,.7f,.4f,.1f,.02f,0};
    static readonly float[] Children={0,.02f,1,.05f,.8f,.1f,1,.4f,.05f,0};
    static readonly float[] Dogs={0,.3f,.9f,.4f,.3f,.4f,.7f,1,.4f,.1f};
    // Who is out at a given hour: share of the crowd × the hour's band × how much rain keeps them in.
    public static float Weight(Folk kind,float hour,float rain) {
        int b=Band(hour);rain=Mathf.Clamp01(rain);
        switch(kind) {
            case Folk.Worker:return .25f*Workers[b]*(1-.15f*rain);
            case Folk.Man:return (.08f*Business[b]+.02f*NightFigures[b])*(1-.15f*rain);
            case Folk.Woman:return .22f*Shoppers[b]*(1-.5f*rain);
            case Folk.OldMan:case Folk.OldWoman:return .045f*Elderly[b]*(1-.75f*rain);
            case Folk.Boy:case Folk.Girl:return .09f*Children[b]*(1-.2f*rain);
            case Folk.DogWalker:return .02f*Dogs[b]*(1-.7f*rain);
            default:return 0; // beggars and musicians keep their own pitches (StreetLife)
        }
    }
    // Walking speed (m/s) and forward lean of the trunk (degrees), from gait studies (Bohannon &
    // Andrews 2011; Winter et al. 1990): older people walk slower with a flexed trunk; children's
    // speed is close to adults' but more varied.
    public static void Gait(Folk kind,System.Random rng,out float speed,out float stoop) {
        float r=(float)rng.NextDouble();
        switch(kind) {
            case Folk.OldMan:case Folk.OldWoman:speed=.85f+.27f*r;stoop=8+8*(float)rng.NextDouble();break;
            case Folk.Boy:case Folk.Girl:speed=1f+.3f*r;stoop=0;break;
            case Folk.DogWalker:speed=1f+.2f*r;stoop=0;break;
            case Folk.Worker:speed=1.3f+.2f*r;stoop=2;break;
            case Folk.Woman:speed=1.2f+.18f*r;stoop=0;break;
            default:speed=1.25f+.2f*r;stoop=0;break;
        }
    }
    // Speed limits of the 1926 Uniform Vehicle Code: 15 mph in the business street, 20 mph in the
    // residential ones; the narrow north–south streets are taken slower still.
    public static float SpeedLimit(StreetNet.Carriage c) {
        if(c==null)return 6.7f;
        if(!c.eastWest)return 5.6f;
        return c.road!=null&&c.road.id=="R01"?6.7f:8f;
    }
}
