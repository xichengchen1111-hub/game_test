using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Records the real game window and the listener's mix for a presentation clip. Frames are
// read back on the main thread and compressed on worker threads; every frame keeps its
// wall-clock time, and named marks let an editor cut the footage without guessing.
public sealed class ShowcaseRecorder : MonoBehaviour {
    [Serializable] public class Cue {public string name;public int frame;public double time;}
    [Serializable] public class Manifest {
        public List<double> times=new List<double>();public List<Cue> marks=new List<Cue>();
        public double duration,audioStart;public int rate,channels,samples,width,height;public float listenerVolume;
        public string source="Native Unity window and AudioListener mix; wall-clock frame times; scripted real controller and interactions.";
    }
    struct Job {public int index,width,height;public GraphicsFormat format;public byte[] pixels;}
    readonly object audioGate=new object();
    readonly List<float[]> audio=new List<float[]>();
    BlockingCollection<Job> jobs;Thread[] workers;
    volatile bool running;int channels;long startTicks,firstAudioTicks;string folder;Manifest manifest;
    double Now=>(System.Diagnostics.Stopwatch.GetTimestamp()-startTicks)/(double)System.Diagnostics.Stopwatch.Frequency;
    public void Begin(string destination) {
        folder=destination;Directory.CreateDirectory(folder);
        manifest=new Manifest{rate=AudioSettings.outputSampleRate,listenerVolume=AudioListener.volume};
        jobs=new BlockingCollection<Job>(12);workers=new Thread[4];
        for(int i=0;i<workers.Length;i++){workers[i]=new Thread(Compress){IsBackground=true};workers[i].Start();}
        startTicks=System.Diagnostics.Stopwatch.GetTimestamp();firstAudioTicks=0;running=true;StartCoroutine(Frames());
    }
    public void Mark(string name){if(manifest!=null)manifest.marks.Add(new Cue{name=name,frame=manifest.times.Count,time=Now});Debug.Log("SHOWCASE_MARK: "+name);}
    void Compress() {
        foreach(var job in jobs.GetConsumingEnumerable()) {
            var jpg=ImageConversion.EncodeArrayToJPG(job.pixels,job.format,(uint)job.width,(uint)job.height,0,93);
            File.WriteAllBytes(Path.Combine(folder,"frame"+job.index.ToString("D05")+".jpg"),jpg);
        }
    }
    void OnAudioFilterRead(float[] data,int count) {
        lock(audioGate) {
            if(!running)return;
            if(firstAudioTicks==0)firstAudioTicks=System.Diagnostics.Stopwatch.GetTimestamp();
            channels=count;audio.Add((float[])data.Clone());
        }
    }
    IEnumerator Frames() {
        while(running) {
            yield return new WaitForEndOfFrame();if(!running)break;
            double time=Now;var texture=ScreenCapture.CaptureScreenshotAsTexture();
            jobs.Add(new Job{index=manifest.times.Count,width=texture.width,height=texture.height,format=texture.graphicsFormat,pixels=texture.GetRawTextureData()});
            manifest.width=texture.width;manifest.height=texture.height;Destroy(texture);manifest.times.Add(time);
        }
    }
    void OnDestroy(){if(running)End();}
    public void End() {
        lock(audioGate)running=false;
        manifest.duration=Now;jobs.CompleteAdding();foreach(var worker in workers)worker.Join();
        manifest.channels=Math.Max(1,channels);manifest.audioStart=firstAudioTicks==0?0:(firstAudioTicks-startTicks)/(double)System.Diagnostics.Stopwatch.Frequency;
        long total=0;foreach(var block in audio)total+=block.Length;manifest.samples=(int)total;
        using(var w=new BinaryWriter(File.Create(Path.Combine(folder,"game-audio.wav")))) {
            // 32-bit float keeps the mix exact even when it was recorded at a low listener volume.
            int bytes=manifest.samples*4;w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+bytes);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);w.Write((short)3);w.Write((short)manifest.channels);w.Write(manifest.rate);w.Write(manifest.rate*manifest.channels*4);w.Write((short)(manifest.channels*4));w.Write((short)32);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(bytes);
            foreach(var block in audio)foreach(var sample in block)w.Write(sample);
        }
        File.WriteAllText(Path.Combine(folder,"capture.json"),JsonUtility.ToJson(manifest,true));
        Debug.Log("SHOWCASE_RECORDING_SAVED: frames="+manifest.times.Count+" seconds="+manifest.duration.ToString("F1")+" audioSamples="+manifest.samples);
    }
}
