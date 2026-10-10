using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class RainCheck {public string location;public float rms,highFrequencyRatio,sourceHighFrequencyRatio,cutoff,shelter;public int splashes;}
    [Serializable] class RainReviewResult {public bool passed,seamless,pauseResume,roofSafe;public List<RainCheck> acoustics=new List<RainCheck>();public float minimumBedRms,maximumBedRms;public int loopWraps;}
    IEnumerator RainSoundSample(string location,RainReviewResult report) {
        var effects=DayNightCycle.Instance.GetComponent<WeatherEffects>();var sound=effects.Ambience;
        var data=new float[2048];var sourceData=new float[2048];double energy=0,difference=0,sourceEnergy=0,sourceDifference=0;int count=0;
        float until=Time.unscaledTime+1;
        while(Time.unscaledTime<until) {
            yield return new WaitForEndOfFrame();AudioListener.GetOutputData(data,0);sound.Bed.GetOutputData(sourceData,0);
            for(int i=1;i<data.Length;i++){energy+=data[i]*data[i];difference+=(data[i]-data[i-1])*(data[i]-data[i-1]);sourceEnergy+=sourceData[i]*sourceData[i];sourceDifference+=(sourceData[i]-sourceData[i-1])*(sourceData[i]-sourceData[i-1]);count++;}
        }
        NeedWeather(energy>1e-6&&sound.Bed.isPlaying&&sound.Ground.isPlaying&&sound.Roof.isPlaying,"Rain layers produced no output");
        // Measure the final listener mix: source taps precede source effects on this player.
        report.acoustics.Add(new RainCheck{location=location,rms=(float)Math.Sqrt(energy/count),highFrequencyRatio=(float)(difference/energy),sourceHighFrequencyRatio=(float)(sourceDifference/sourceEnergy),cutoff=sound.Cutoff,shelter=sound.ShelterBlend,splashes=effects.Splashes.VisibleHits});
        File.WriteAllText(Path.Combine(captures,"rain-acoustic-samples.json"),JsonUtility.ToJson(report,true));
    }
    IEnumerator RainLookAt(Vector3 target) {
        var start=player.transform.rotation;float pitch=player.pitch;player.Aim(target);
        var end=player.transform.rotation;float endPitch=player.pitch;player.transform.rotation=start;player.pitch=pitch;
        float age=0;while(age<1.0f){age+=Time.unscaledDeltaTime;float t=Mathf.SmoothStep(0,1,age);player.transform.rotation=Quaternion.Slerp(start,end,t);player.pitch=Mathf.Lerp(pitch,endPitch,t);yield return null;}
    }
    IEnumerator ReviewRainPresentation() {
        yield return new WaitForSeconds(.5f);DayReviewTime(680);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);dayNightReviewFrozen=false;
        var effects=DayNightCycle.Instance.GetComponent<WeatherEffects>();var sound=effects.Ambience;var report=new RainReviewResult();
        var recorder=player.eye.GetComponent<WeatherClipRecorder>();if(!recorder)recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();
        player.Place(new Vector3(0,.12f,-9),0);player.Aim(new Vector3(0,.04f,-5));yield return new WaitForSeconds(1.5f);
        NeedWeather(effects.Splashes.VisibleHits>8,"Ground splashes are missing");
        recorder.Begin(Path.Combine(captures,"01-rain-and-splashes"));
        yield return new WaitForSeconds(3);yield return RainSoundSample("outdoors",report);
        // Cross all three real clip seams, checking the actual source signal.
        var block=new float[1024];float minimum=float.MaxValue,maximum=0;int previous=sound.Bed.timeSamples,wraps=0;
        float end=Time.unscaledTime+26;
        while(Time.unscaledTime<end) {
            yield return new WaitForEndOfFrame();sound.Bed.GetOutputData(block,0);double energy=0;foreach(float f in block)energy+=f*f;
            float rms=(float)Math.Sqrt(energy/block.Length);minimum=Mathf.Min(minimum,rms);maximum=Mathf.Max(maximum,rms);
            if(sound.Bed.timeSamples<previous)wraps++;previous=sound.Bed.timeSamples;
            NeedWeather(sound.Bed.isPlaying&&sound.Ground.isPlaying&&sound.Roof.isPlaying,"Rain unexpectedly stopped");
            NeedWeather(effects.Splashes.ContactQueries<=4,"Ground query budget exceeded");
            var positions=effects.Splashes.SurfaceMesh.vertices;var colors=effects.Splashes.SurfaceMesh.colors32;
            for(int i=0;i<positions.Length;i++)if(colors[i].a>0)NeedWeather(WeatherShelter.RoofHeight(positions[i])<=positions[i].y+.08f,"Splash appeared under a roof");
        }
        report.minimumBedRms=minimum;report.maximumBedRms=maximum;report.loopWraps=wraps;
        NeedWeather(wraps>=1&&minimum>maximum*.35f,"Rain has an audible gap at its loop seam");report.seamless=report.roofSafe=true;
        recorder.End();yield return Capture("rain-ground-splashes");
        paused=true;yield return new WaitForSeconds(.2f);NeedWeather(sound.Bed.volume==0&&!sound.Bed.isPlaying,"Pause did not stop rain");
        paused=false;yield return new WaitForSeconds(.4f);NeedWeather(sound.Bed.isPlaying&&sound.Bed.volume>0,"Rain failed to resume");report.pauseResume=true;
        DayReviewTime(680);dayNightReviewFrozen=false;
        var placement=milkShopRoot.GetComponent<MilkShopPlacement>();
        player.Place(placement.outside,0);player.Aim(placement.inside+Vector3.up*1.4f);yield return new WaitForSeconds(1.4f);
        recorder.Begin(Path.Combine(captures,"02-enter-room-rain"));yield return new WaitForSeconds(2);
        yield return WalkTo(placement.inside,.5f);player.Aim(milkShopRoot.TransformPoint(new Vector3(0,1.6f,1.75f)));
        yield return UseDoorRay(Mechanism("milk-left-door"));
        yield return new WaitForSeconds(3);yield return RainSoundSample("indoors",report);
        NeedWeather(sound.ShelterBlend>.95f&&sound.Cutoff<1200,"Room failed to muffle the rain");
        NeedWeather(report.acoustics[1].highFrequencyRatio<report.acoustics[0].highFrequencyRatio*.4f,"Room rain retained outdoor high frequencies");
        yield return RainLookAt(milkShopRoot.TransformPoint(new Vector3(-3.4f,1.85f,-.3f)));yield return new WaitForSeconds(3);yield return Capture("indoor-rain-window");
        yield return UseDoorRay(Mechanism("milk-left-door"));
        yield return RainLookAt(placement.outside+Vector3.up*.5f);yield return WalkTo(placement.outside,.5f);yield return new WaitForSeconds(3);
        recorder.End();NeedWeather(sound.ShelterBlend<.1f,"Rain muffling remained outdoors");
        DayReviewTime(785);DayNightCycle.Instance.SetReviewWeather(null);dayNightReviewFrozen=false;player.Place(new Vector3(0,.12f,-9),0);player.Aim(new Vector3(25,3,20));yield return new WaitForSeconds(.5f);
        recorder.Begin(Path.Combine(captures,"03-grey-day-fog"));yield return new WaitForSeconds(3);
        float until=Time.unscaledTime+5;while(Time.unscaledTime<until){player.transform.Rotate(0,Time.deltaTime*12,0);yield return null;}
        yield return new WaitForSeconds(2);recorder.End();yield return Capture("grey-fog-day");
        NeedWeather(RenderSettings.fogDensity>.045f,"Overcast visibility remains too clear");
        report.passed=true;File.WriteAllText(Path.Combine(captures,"rain-presentation-results.json"),JsonUtility.ToJson(report,true));
        Debug.Log("RAIN_PRESENTATION_REVIEW_PASS");Application.Quit();
    }
}
