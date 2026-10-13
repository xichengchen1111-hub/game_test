using System;
using System.Collections;
using System.Linq;
using UnityEngine;

// Diagnostic only. Start the player with --audio-probe (volume as saved) or
// --audio-probe-muted-start (starts at zero like a first launch, then raises the volume the way
// the pause menu does) to log what reaches the listener. It uses its own save, never writes the
// player's volume preference, and its one test sound plays at 3 % volume.
public sealed class AudioProbe : MonoBehaviour {
    static bool mutedStart;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin() {
        var args=Environment.GetCommandLineArgs();
        mutedStart=Array.IndexOf(args,"--audio-probe-muted-start")>=0;
        if(!mutedStart&&Array.IndexOf(args,"--audio-probe")<0)return;
        // Awake has run and Start has not: this is the state a launch with a saved volume of zero has.
        if(mutedStart)AudioListener.volume=0;
        var host=new GameObject("Audio probe");DontDestroyOnLoad(host);host.AddComponent<AudioProbe>();
    }
    IEnumerator Measure(string label,float seconds) {
        var data=new float[1024];double sum=0;long count=0;float peak=0,low=float.MaxValue,high=0;float until=Time.unscaledTime+seconds;
        AudioListener.GetOutputData(data,0);yield return null; // the first read only attaches the tap
        while(Time.unscaledTime<until) {
            AudioListener.GetOutputData(data,0);
            foreach(var v in data){sum+=v*v;count++;peak=Mathf.Max(peak,Mathf.Abs(v));}
            low=Mathf.Min(low,AudioListener.volume);high=Mathf.Max(high,AudioListener.volume);
            yield return null;
        }
        var playing=FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Where(s=>s.isPlaying&&s.volume>0).Select(s=>s.gameObject.name+(s.isVirtual?"(virtual)":"")).ToArray();
        Debug.Log("AUDIO_PROBE "+label+": listenerVolume="+low+".."+high+" rms="+Mathf.Sqrt((float)(sum/Math.Max(1,count))).ToString("E3")+" peak="+peak.ToString("E3")+
            " focused="+Application.isFocused+" runInBackground="+Application.runInBackground+" audible=["+string.Join(", ",playing)+"]");
    }
    IEnumerator Start() {
        yield return new WaitForSecondsRealtime(4);
        yield return Measure(mutedStart?"muted start, before raising":"as launched",2.5f);
        if(mutedStart) {
            AudioListener.volume=1;
            yield return Measure("after raising to 1",2.5f);
        }
        var test=gameObject.AddComponent<AudioSource>();test.spatialBlend=0;test.volume=.03f;test.clip=Resources.Load<AudioClip>("PropSounds/MapUnfold");
        if(test.clip){test.Play();yield return Measure("test sound at 3 % source volume",1.2f);}
        Debug.Log("AUDIO_PROBE_DONE");Application.Quit();
    }
}
