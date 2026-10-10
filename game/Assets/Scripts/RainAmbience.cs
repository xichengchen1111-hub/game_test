using System;
using UnityEngine;

// Original continuous stereo rain plus independent ground/roof patter.
// Circular noise filtering and wrapped impact tails have no silent loop seams.
public sealed class RainAmbience : MonoBehaviour {
    public AudioSource Bed {get;private set;}
    public AudioSource Ground {get;private set;}
    public AudioSource Roof {get;private set;}
    public float ShelterBlend {get;private set;}
    public float Cutoff => bedFilter.cutoffFrequency;
    AudioLowPassFilter bedFilter,groundFilter;
    bool paused;
    AudioSource[] sources;
    const int Rate=22050;
    void Awake() {
        Bed=Source("Continuous stereo rainfall",MakeLoop(24,0,1931),out bedFilter);
        Ground=Source("Drops striking wet ground",MakeLoop(29,1,2193),out groundFilter);
        Roof=Source("Rain beyond the roof and walls",MakeLoop(31,2,3102),out var roofFilter);
        roofFilter.cutoffFrequency=950;
        sources=new[]{Bed,Ground,Roof};
    }
    AudioSource Source(string label,AudioClip clip,out AudioLowPassFilter filter) {
        var child=new GameObject(label);child.transform.SetParent(transform,false);
        var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.clip=clip;
        source.loop=true;source.spatialBlend=0;source.dopplerLevel=0;source.priority=80;source.volume=0;
        filter=child.AddComponent<AudioLowPassFilter>();filter.cutoffFrequency=16000;filter.lowpassResonanceQ=1;
        source.Play();return source;
    }
    static AudioClip MakeLoop(int seconds,int kind,int seed) {
        int n=seconds*Rate;var data=new float[n*2];var random=new System.Random(seed);
        for(int channel=0;channel<2;channel++) {
            var white=new float[n];for(int i=0;i<n;i++)white[i]=(float)random.NextDouble()*2-1;
            float low=0,body=0;
            // Warm the filters from the end of this same circular signal.
            for(int j=-2048;j<n;j++) {
                int i=(j+n)%n;low=low*.985f+white[i]*.015f;body=body*.64f+white[i]*.36f;
                if(j>=0)data[i*2+channel]=kind==0 ? white[i]*.16f+body*.24f+low*.30f : white[i]*.010f;
            }
            if(kind==0)continue;
            int count=seconds*(kind==1?105:65);
            for(int drop=0;drop<count;drop++) {
                int start=random.Next(n);float duration=kind==1?.018f+(float)random.NextDouble()*.045f:.035f+(float)random.NextDouble()*.065f;
                float gain=.07f+(float)random.NextDouble()*.16f;
                float frequency=kind==1?950+(float)random.NextDouble()*2600:220+(float)random.NextDouble()*550;
                float smooth=0;
                for(int j=0;j<(int)(duration*Rate);j++) {
                    float t=j/(float)Rate,env=Mathf.Min(1,t/.001f)*Mathf.Exp(-t/(duration*.18f));
                    float noise=(float)random.NextDouble()*2-1;smooth=smooth*.70f+noise*.30f;
                    data[((start+j)%n)*2+channel]+=gain*env*(kind==1?noise*.7f+Mathf.Sin(6.283185f*frequency*t)*.3f:smooth+Mathf.Sin(6.283185f*frequency*t)*.35f);
                }
            }
        }
        var clip=AudioClip.Create("Original seamless rain layer "+kind,n,2,Rate,false);clip.SetData(data,0);return clip;
    }
    public void Apply(float amount,bool sheltered,bool freeze) {
        if(!freeze)ShelterBlend=Mathf.MoveTowards(ShelterBlend,sheltered?1:0,Time.unscaledDeltaTime*1.6f);
        // A logarithmic cutoff sweep sounds like moving behind a wall, rather than switching clips.
        bedFilter.cutoffFrequency=Mathf.Exp(Mathf.Lerp(Mathf.Log(16000),Mathf.Log(1100),ShelterBlend));
        groundFilter.cutoffFrequency=Mathf.Exp(Mathf.Lerp(Mathf.Log(16000),Mathf.Log(750),ShelterBlend));
        Bed.volume=freeze?0:amount*Mathf.Lerp(.60f,.40f,ShelterBlend);
        Ground.volume=freeze?0:amount*Mathf.Lerp(.53f,.12f,ShelterBlend);
        Roof.volume=freeze?0:amount*.36f*ShelterBlend;
        if(freeze!=paused) {
            paused=freeze;
            foreach(var source in sources) {if(paused)source.Pause();else source.UnPause();}
        }
        // Recover an audio-device restart without restarting healthy loops every frame.
        if(!freeze)foreach(var source in sources)if(!source.isPlaying)source.Play();
    }
    void OnDestroy() { foreach(var source in sources)if(source&&source.clip)Destroy(source.clip); }
}
