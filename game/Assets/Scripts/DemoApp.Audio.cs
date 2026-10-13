using UnityEngine;

// One user preference for the whole game; review runs never change it.
public partial class DemoApp {
    const string MasterVolumeKey="audio.masterVolume";
    float masterVolume;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void SilenceBeforeSceneAudio(){AudioListener.volume=0;}
    void InitializeAudio() {
        masterVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey,0));
        ApplyMasterVolume();
    }
    void ApplyMasterVolume(){AudioListener.volume=Review?0:masterVolume;}
    void SetMasterVolume(float volume) {
        masterVolume=Mathf.Clamp01(volume);ApplyMasterVolume();
        if(!Review){PlayerPrefs.SetFloat(MasterVolumeKey,masterVolume);PlayerPrefs.Save();}
    }
    void DrawAudioSettings(float x,float y,float width) {
        Label(new Rect(x,y,width,30),"声音设置",text);
        Label(new Rect(x,y+34,width-105,30),masterVolume<.001f?"总音量 · 已静音":"总音量 · "+Mathf.RoundToInt(masterVolume*100)+"%",small);
        if(Button(new Rect(x+width-100,y+28,100,36),masterVolume<.001f?"开启声音":"静音"))SetMasterVolume(masterVolume<.001f?1:0);
        float volume=GUI.HorizontalSlider(new Rect(x,y+75,width,25),masterVolume,0,1);
        if(Mathf.Abs(volume-masterVolume)>.005f)SetMasterVolume(volume);
    }
}
