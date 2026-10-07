using UnityEngine;

// Original procedural sounds: no third-party recordings or asset dependencies.
public class TownSound : MonoBehaviour {
    AudioSource ambience,details;
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
        ambience.volume=app.CurrentScene=="Town"?.32f:.06f;
        if(app.CurrentScene=="ClockShop"&&Time.time>=nextTick){nextTick=Time.time+.85f;details.PlayOneShot(tick,.4f);}
        if(!app.Blocked&&app.player.GetComponent<CharacterController>().velocity.sqrMagnitude>.1f&&Time.time>=nextStep) {
            nextStep=Time.time+.44f;details.PlayOneShot(step,.6f);
        }
    }
}
