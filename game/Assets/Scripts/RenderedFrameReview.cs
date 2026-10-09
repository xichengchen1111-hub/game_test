using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Review-only timing; includes screenshot stalls, so this is not a benchmark.
public sealed class RenderedFrameReview : MonoBehaviour {
    readonly List<float> samples=new List<float>();
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartReview() {
        var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--capture-dir");
        if(Array.IndexOf(args,"--review")<0||i<0||i+1>=args.Length)return;
        var go=new GameObject("Review frame sampling");DontDestroyOnLoad(go);go.AddComponent<RenderedFrameReview>().output=args[i+1];
    }
    void Update() {if(Time.realtimeSinceStartup>3)samples.Add(Time.unscaledDeltaTime*1000);}
    void OnApplicationQuit() {
        if(samples.Count==0)return;
        samples.Sort();double total=0;foreach(var t in samples)total+=t;
        var report=new Result{frames=samples.Count,medianMs=samples[samples.Count/2],p95Ms=samples[(int)((samples.Count-1)*.95)],meanMs=total/samples.Count};
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"frame-timing.json"),JsonUtility.ToJson(report,true));
    }
    [Serializable] class Result {public int frames;public double meanMs;public float medianMs,p95Ms;public string scope="Rendered review after three seconds, includes capture stalls and UI; local Mac only, not a GPU benchmark or long-duration performance acceptance.";}
}
