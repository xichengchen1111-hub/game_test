using UnityEngine;

// Opens and closes a period character's MouthJaw (added at build time by PeriodCharacterMouth)
// while the character speaks a line: roughly one syllable per written character at the same
// pace as PeriodCharacterPerformance's speaking time, closed at punctuation, never driven by
// the idle or walk. Pausing freezes it with the rest of the cast.
public sealed class PeriodCharacterMouthMotion : MonoBehaviour {
    public Transform jaw;
    public Vector3 axis=Vector3.right;
    public float maxDegrees=8;
    public const float SyllablesPerSecond=5.2f;
    PeriodCharacterPerformance performance;
    float angle,clock;
    string line;int count=-1;
    void Awake(){performance=GetComponent<PeriodCharacterPerformance>();}
    static float Length(char c)=>c=='，'||c==','||c=='、'||c=='；'?.22f:c=='。'||c=='？'||c=='！'||c=='…'||c=='?'||c=='!'||c=='.'?.34f:char.IsWhiteSpace(c)||c=='“'||c=='”'||c=='「'||c=='」'||c=='—'?.06f:1/SyllablesPerSecond;
    static bool Spoken(char c)=>Length(c)==1/SyllablesPerSecond;
    // Opening (0..1) for a line at a time since it began. Within a phrase the mouth does not shut
    // between syllables (the first version did, and flapped like a puppet; user, 2026-10-14): it
    // dips to about a third and opens again, and closes only at punctuation and at the end.
    public static float Opening(string text,float t) {
        if(string.IsNullOrEmpty(text)||t<0)return 0;
        float at=0;
        for(int i=0;i<text.Length;i++) {
            char c=text[i];float length=Length(c);
            if(t<at+length) {
                if(!Spoken(c))return 0;
                float phase=(t-at)/length;
                // Each syllable opens a little differently; the shape is a soft rise and fall.
                float strength=.5f+.5f*Mathf.Abs(Mathf.Sin(c*12.9898f));
                float floor=Mathf.Lerp(i>0&&Spoken(text[i-1])?.32f:0,i+1<text.Length&&Spoken(text[i+1])?.32f:0,phase);
                return Mathf.Lerp(floor,strength,Mathf.Pow(Mathf.Sin(phase*Mathf.PI),.8f));
            }
            at+=length;
        }
        return 0;
    }
    void LateUpdate() {
        if(!jaw)return;
        var app=DemoApp.Instance;bool frozen=app&&app.NPCAnimationPaused;
        bool speaking=performance&&performance.IsSpeaking;
        if(speaking&&performance.SpokenCount!=count){line=performance.SpokenLine;count=performance.SpokenCount;clock=0;}
        if(!speaking){line=null;count=-1;}
        if(!frozen)clock+=Time.deltaTime;
        float target=speaking?Opening(line,clock)*maxDegrees:0;
        if(!frozen)angle=Mathf.Lerp(angle,target,1-Mathf.Exp(-Time.deltaTime*15));
        jaw.localRotation=Quaternion.AngleAxis(angle,axis);
    }
}
