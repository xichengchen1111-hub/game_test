using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

// Explicit isolated review only: real rendered frames and the listener's game mix.
public sealed class WeatherClipRecorder : MonoBehaviour {
    [Serializable] public class Frame {public string file;public double time;}
    [Serializable] public class Manifest {public List<Frame> frames=new List<Frame>();public double duration,audioStart;public int rate,channels,samples;public string source="Native Unity screen and AudioListener mix; wall-clock frame timestamps, scripted real controller, no dubbed audio.";}
    volatile bool running;
    readonly object audioGate=new object();
    float[] samples;
    int sampleCount,channels;
    long startTicks,firstAudioTicks;
    string folder;
    Manifest manifest;
    double Now => (System.Diagnostics.Stopwatch.GetTimestamp()-startTicks)/(double)System.Diagnostics.Stopwatch.Frequency;
    public void Begin(string destination) {
        if(!DemoApp.Instance||!DemoApp.Instance.Review)throw new InvalidOperationException("Recording requires isolated review");
        folder=destination;Directory.CreateDirectory(folder);manifest=new Manifest{rate=AudioSettings.outputSampleRate};
        samples=new float[manifest.rate*2*100];sampleCount=0;channels=0;firstAudioTicks=0;
        startTicks=System.Diagnostics.Stopwatch.GetTimestamp();running=true;StartCoroutine(Frames());
    }
    void OnAudioFilterRead(float[] data,int count) {
        lock(audioGate) {
        if(!running||samples==null)return;
        if(firstAudioTicks==0)firstAudioTicks=System.Diagnostics.Stopwatch.GetTimestamp();
        channels=count;int offset=sampleCount,n=Math.Min(data.Length,samples.Length-offset);
        Array.Copy(data,0,samples,offset,n);Volatile.Write(ref sampleCount,offset+n);
        }
    }
    IEnumerator Frames() {
        double next=0;
        while(running) {
            yield return new WaitForEndOfFrame();if(!running)break;
            double time=Now;if(time<next)continue;next=time+1.0/24;
            var texture=ScreenCapture.CaptureScreenshotAsTexture();string name="frame"+manifest.frames.Count.ToString("D05")+".jpg";
            File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToJPG(88));Destroy(texture);
            manifest.frames.Add(new Frame{file=name,time=time});
        }
    }
    void OnDestroy(){if(running)End();}
    public void End() {
        lock(audioGate)running=false;manifest.duration=Now;manifest.channels=channels;manifest.samples=Volatile.Read(ref sampleCount);
        manifest.audioStart=(firstAudioTicks-startTicks)/(double)System.Diagnostics.Stopwatch.Frequency;
        using(var w=new BinaryWriter(File.Create(Path.Combine(folder,"game-audio.wav")))) {
            int bytes=manifest.samples*2;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);w.Write((short)1);w.Write((short)Math.Max(1,channels));w.Write(manifest.rate);w.Write(manifest.rate*Math.Max(1,channels)*2);w.Write((short)(Math.Max(1,channels)*2));w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);for(int i=0;i<manifest.samples;i++)w.Write((short)Mathf.Clamp(samples[i]*32767,-32768,32767));
        }
        File.WriteAllText(Path.Combine(folder,"capture.json"),JsonUtility.ToJson(manifest,true));samples=null;
    }
}
