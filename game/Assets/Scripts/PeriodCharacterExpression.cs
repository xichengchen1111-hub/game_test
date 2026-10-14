using UnityEngine;

// The child's face between the lines (user, 2026-10-14): she blinks, her upper lids rest a little low
// because she is tired, the corners of her mouth lift when she is glad, and her eyes fill and a tear
// runs when the talk turns to her mother. All of it drives blend shapes that PeriodCharacterMouth
// adds at build time; DemoApp sets the mood (SetMood) as the scene goes on. Pausing freezes it.
public sealed class PeriodCharacterExpression : MonoBehaviour {
    public SkinnedMeshRenderer face;
    public int blink=-1,smile=-1,tearFilm=-1,tearFlow=-1;
    // How far the upper lids rest lowered (0 open, 1 shut) and how long one blink takes.
    public float restLid=.16f,blinkSeconds=.2f;
    float smileTarget,tearTarget,smileNow,tearNow,flow,nextBlink=2,blinkClock=-1;bool second;
    public float Smile=>smileNow;public float Tears=>tearNow;public float Flow=>flow;public float Lid{get;private set;}
    public void SetMood(float smileAmount,float tears){smileTarget=Mathf.Clamp01(smileAmount);tearTarget=Mathf.Clamp01(tears);}
    // For stills: the mood at once, no easing.
    public void SetMoodNow(float smileAmount,float tears,float tearRun){SetMood(smileAmount,tears);smileNow=smileTarget;tearNow=tearTarget;flow=Mathf.Clamp01(tearRun);Apply(restLid);}
    public void Shut(float amount){Apply(Mathf.Clamp01(amount));}
    void LateUpdate() {
        var app=DemoApp.Instance;if(!face||app&&app.NPCAnimationPaused)return;
        float dt=Time.deltaTime;
        // A smile comes and goes slowly; tears gather more slowly still and do not vanish at once.
        smileNow=Mathf.MoveTowards(smileNow,smileTarget,dt*.5f);
        tearNow=Mathf.MoveTowards(tearNow,tearTarget,dt*(tearTarget>tearNow?.22f:.08f));
        // One tear runs once her eyes are full, over about seven seconds, and stays until they dry.
        if(tearNow>.7f)flow=Mathf.MoveTowards(flow,1,dt/7f);else if(tearNow<.08f)flow=Mathf.MoveTowards(flow,0,dt*.5f);
        // Blinks every two and a half to six seconds, now and then twice; a tired child blinks slowly.
        nextBlink-=dt;
        if(nextBlink<=0&&blinkClock<0){blinkClock=0;second=Random.value<.2f;nextBlink=Random.Range(2.5f,6f);}
        float lid=restLid;
        if(blinkClock>=0) {
            blinkClock+=dt;float p=blinkClock/blinkSeconds;
            if(p>=1){if(second){second=false;blinkClock=0;}else blinkClock=-1;}
            else{float closing=p<.4f?p/.4f:1-(p-.4f)/.6f;lid=Mathf.Lerp(restLid,1,Mathf.SmoothStep(0,1,closing));}
        }
        Apply(lid);
    }
    void Apply(float lid) {
        Lid=lid;
        if(blink>=0)face.SetBlendShapeWeight(blink,lid*100);
        if(smile>=0)face.SetBlendShapeWeight(smile,smileNow*100);
        if(tearFilm>=0)face.SetBlendShapeWeight(tearFilm,Mathf.Clamp01(tearNow*1.4f)*100);
        if(tearFlow>=0)face.SetBlendShapeWeight(tearFlow,flow*100);
    }
}
