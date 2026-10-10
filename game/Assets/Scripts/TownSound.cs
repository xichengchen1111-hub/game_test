using UnityEngine;

// Original procedural ambience and authored held-item foley; no sampled recordings.
public class TownSound : MonoBehaviour {
    AudioSource ambience,details;
    AudioSource mapAudio,watchAudio;
    readonly AudioClip[] propClips=new AudioClip[4];
    AudioClip step,tick;
    float nextStep,nextTick;

    void Start() {
        const int rate=22050;
        var random=new System.Random(1931);
        var wind=new float[rate*12];float filtered=0;
        for(int i=0;i<wind.Length;i++) {
            filtered=filtered*.995f+((float)random.NextDouble()*2-1)*.005f;
            float envelope=Mathf.Sin(Mathf.PI*i/(wind.Length-1));
            wind[i]=filtered*envelope*(.6f+.4f*Mathf.Sin(i/(float)rate*.45f));
        }
        ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.volume=.32f;
        ambience.clip=AudioClip.Create("Quiet dusk breeze",wind.Length,1,rate,false);ambience.clip.SetData(wind,0);ambience.Play();
        details=gameObject.AddComponent<AudioSource>();details.volume=.15f;
        step=Impulse("Soft footstep",.15f,125f,random,rate);tick=Impulse("Clock tick",.055f,1900f,random,rate);
        mapAudio=HeldSource("Paper handling",.52f);watchAudio=HeldSource("Pocket watch handling",.48f);
        string[] names={"MapUnfold","MapFold","WatchTakeOut","WatchPutAway"};
        for(int i=0;i<names.Length;i++)propClips[i]=Resources.Load<AudioClip>("PropSounds/"+names[i]);
    }
    AudioSource HeldSource(string name,float volume) {
        var child=new GameObject(name);child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;
        source.spatialBlend=0;source.dopplerLevel=0;source.volume=volume;source.priority=64;return source;
    }
    public AudioSource HeldItemSource(bool map)=>map?mapAudio:watchAudio;
    public void PlayHeldItem(bool map,bool opening) {
        var source=HeldItemSource(map);var clip=propClips[(map?0:2)+(opening?0:1)];
        if(!source||!clip)return;
        source.clip=clip;source.Play();
    }
    AudioClip Impulse(string name,float seconds,float frequency,System.Random random,int rate) {
        var samples=new float[(int)(rate*seconds)];
        for(int i=0;i<samples.Length;i++) {
            float t=i/(float)rate,envelope=Mathf.Exp(-t/(seconds*.16f))*Mathf.Min(t*rate/40f,1);
            samples[i]=(Mathf.Sin(2*Mathf.PI*frequency*t)*.45f+((float)random.NextDouble()*2-1)*.2f)*envelope;
        }
        var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
    }
    void Update() {
        var app=DemoApp.Instance;if(!app||!details)return;
        ambience.volume=app.IsInsideShop?.06f:.32f;
        if(app.IsInsideShop&&Time.time>=nextTick){nextTick=Time.time+.85f;details.PlayOneShot(tick,.4f);}
        if(!app.Blocked&&app.player.GetComponent<CharacterController>().velocity.sqrMagnitude>.1f&&Time.time>=nextStep) {
            nextStep=Time.time+.44f;details.PlayOneShot(step,.6f);
        }
    }
}
