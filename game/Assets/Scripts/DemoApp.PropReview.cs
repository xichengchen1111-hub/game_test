using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    [Serializable] sealed class PropAudioSample {
        public string action,clip;public float length,peak,rms,volume;public int sampleRate;
    }
    [Serializable] sealed class PropReviewResult {
        public bool passed,mutualPutAway,readingDoesNotReplay,escClosesMap;
        public List<PropAudioSample> samples=new List<PropAudioSample>();
        public string verification;
    }
    IEnumerator CheckPropSound(string action,AudioSource source,string expected,PropReviewResult result) {
        NeedNavigation(source&&source.clip&&source.clip.name==expected&&source.isPlaying&&source.enabled&&source.volume>.1f&&!source.mute,
            "Held-item sound failed to play: "+action);
        NeedNavigation(AudioListener.volume>0&&!AudioListener.pause&&FindFirstObjectByType<AudioListener>().enabled,"Audio listener disabled");
        var buffer=new float[1024];float peak=0,rms=0;float until=Time.unscaledTime+source.clip.length+.08f;
        while(Time.unscaledTime<until) {
            yield return new WaitForEndOfFrame();source.GetOutputData(buffer,0);double energy=0;
            foreach(float value in buffer){peak=Mathf.Max(peak,Mathf.Abs(value));energy+=value*value;}
            rms=Mathf.Max(rms,(float)Math.Sqrt(energy/buffer.Length));
        }
        NeedNavigation(peak>.015f&&rms>.002f,"Native audio output was silent: "+action);
        result.samples.Add(new PropAudioSample{action=action,clip=source.clip.name,length=source.clip.length,peak=peak,rms=rms,volume=source.volume,sampleRate=source.clip.frequency});
        // Preserve the exact imported clip for audition; output energy above is
        // measured from the running native AudioSource, not this PCM export.
        var clip=source.clip;var samples=new float[clip.samples*clip.channels];
        NeedNavigation(clip.GetData(samples,0),"Cannot inspect imported PCM audio");
        using(var writer=new BinaryWriter(File.Create(Path.Combine(captures,expected+".wav")))) {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)clip.channels);
            writer.Write(clip.frequency);writer.Write(clip.frequency*clip.channels*2);writer.Write((short)(clip.channels*2));writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
            foreach(float value in samples)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value,-1,1)*32767));
        }
    }
    IEnumerator ReviewHeldProps() {
        yield return new WaitForSeconds(.5f);var sound=GetComponent<TownSound>();NeedNavigation(sound,"Town sound missing");
        var result=new PropReviewResult();
        ToggleWatch();NeedNavigation(watch&&!mapOpen,"Q did not take out watch");
        yield return CheckPropSound("watch-open",sound.HeldItemSource(false),"WatchTakeOut",result);yield return Capture("props-watch-open");
        ToggleWatch();NeedNavigation(!watch,"Q did not put away watch");
        yield return CheckPropSound("watch-close",sound.HeldItemSource(false),"WatchPutAway",result);
        MapReviewPitch(47);NeedNavigation(HandleMapKeys(true,false)&&mapOpen,"M did not take out paper");
        yield return CheckPropSound("map-open",sound.HeldItemSource(true),"MapUnfold",result);yield return Capture("props-map-no-bottom-hint");
        // Ordinary reading only hides the map temporarily; it must not restart foley.
        journal=true;CursorMode();yield return new WaitForSeconds(.25f);
        NeedNavigation(mapOpen&&!heldTownMap.ViewCamera.enabled&&!sound.HeldItemSource(true).isPlaying,"Reading restarted map audio");
        journal=false;CursorMode();yield return new WaitForSeconds(.3f);
        NeedNavigation(heldTownMap.Visible&&!sound.HeldItemSource(true).isPlaying,"Returning from reading restarted audio");result.readingDoesNotReplay=true;
        NeedNavigation(HandleMapKeys(false,true)&&!mapOpen&&!paused,"Esc did not put away map");
        yield return CheckPropSound("map-close",sound.HeldItemSource(true),"MapFold",result);result.escClosesMap=true;
        ToggleWatch();yield return new WaitForSeconds(.35f);HandleMapKeys(true,false);
        NeedNavigation(!watch&&mapOpen&&sound.HeldItemSource(false).clip.name=="WatchPutAway"&&sound.HeldItemSource(false).isPlaying,
            "Taking out map did not audibly put away watch");
        yield return new WaitForSeconds(.5f);ToggleWatch();
        NeedNavigation(watch&&!mapOpen&&sound.HeldItemSource(true).clip.name=="MapFold"&&sound.HeldItemSource(true).isPlaying,
            "Taking out watch did not audibly put away map");
        yield return new WaitForSeconds(.5f);SetWatchOpen(false);result.mutualPutAway=true;
        result.passed=true;result.verification="Visible native player; shared Q/M/Esc handlers and actual AudioSource output samples. No physical keyboard or speaker/headphone recording claim.";
        File.WriteAllText(Path.Combine(captures,"prop-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("HELD_PROP_REVIEW_PASS: four nonzero native audio outputs, Q/M/Esc, mutual put-away and temporary reading hide without replay");Application.Quit();
    }
}
