using UnityEngine;

// Decorative municipal signal, driven by the saved shared game clock. It adds
// no timer/save field, point lights, traffic AI, police penalties or story facts.
public sealed class PeriodTrafficSignal : MonoBehaviour {
    public Renderer[] lenses; // face order N,E,S,W, each red/amber/green
    public int Phase {get;private set;}
    public double CycleSeconds {get;private set;}
    MaterialPropertyBlock block;
    void Awake(){block=new MaterialPropertyBlock();}
    static readonly Color[] Colours={new Color(1,.075f,.025f),new Color(1,.49f,.025f),new Color(.04f,.78f,.32f)};
    public static int State(double seconds,int axis) {
        double t=(seconds%58+58)%58;
        // 24s green, 3s amber, 2s clearance; then the perpendicular approach.
        if(axis==0)return t<24?2:t<27?1:0;
        return t>=29&&t<53?2:t>=53&&t<56?1:0;
    }
    void LateUpdate() {
        var app=DemoApp.Instance;if(!app)return;
        CycleSeconds=app.ElapsedGameMinutes/GameClock.GameMinutesPerRealSecond;
        Phase=(int)(CycleSeconds%58);
        for(int face=0;face<4;face++)for(int colour=0;colour<3;colour++) {
            int index=face*3+colour;if(index>=lenses.Length||!lenses[index])continue;
            bool on=State(CycleSeconds,face%2)==colour;
            block.SetColor("_BaseColor",Colours[colour]*(on?.62f:.095f));
            block.SetColor("_EmissionColor",on?Colours[colour]*2.8f:Color.black);
            lenses[index].SetPropertyBlock(block);
        }
    }
}
