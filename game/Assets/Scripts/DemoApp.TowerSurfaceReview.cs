using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class TowerSurfaceMotion {public bool actualController;public float metres;public int frames;public List<float> luminance=new List<float>();public string verification;}
    IEnumerator ReviewTowerSurfaceMotion(ClockTowerInstance tower) {
        foreach(string surface in new[]{"floor","wall"}) {
            yield return TowerWalk(tower.LocalToWorld(new Vector3(-1.05f,ClockTowerInteriorLayout.MachineFloor,.60f)));
            Vector3 target=tower.LocalToWorld(surface=="floor"?new Vector3(1.74f,15.001f,1.82f):new Vector3(2.119f,17.50f,1.78f));
            string folder=Path.Combine(captures,"tower-motion-"+surface);Directory.CreateDirectory(folder);
            var proof=new TowerSurfaceMotion{actualController=true,verification="Normal AO/shadows; real upper-platform lateral CharacterController motion. 10 Hz samples are not game FPS. Moving lighting can change luminance; inspect frames, do not infer no flicker from a scalar threshold."};
            var patch=new Texture2D(9,9,TextureFormat.RGB24,false);Vector3 start=player.transform.position;
            player.scriptedDirection=tower.transform.TransformDirection(Vector3.back*.30f);
            float finish=Time.unscaledTime+1.25f,next=Time.unscaledTime;int frame=0;
            while(Time.unscaledTime<finish) {
                player.Aim(target);yield return new WaitForEndOfFrame();
                if(Time.unscaledTime<next)continue;next+=.1f;
                var point=player.eye.WorldToScreenPoint(target);
                NeedTower(point.z>0&&point.x>5&&point.x<Screen.width-5&&point.y>5&&point.y<Screen.height-5,"Tower surface patch outside view");
                patch.ReadPixels(new Rect(Mathf.Round(point.x)-4,Mathf.Round(point.y)-4,9,9),0,0);patch.Apply();
                float sum=0;foreach(var pixel in patch.GetPixels())sum+=.2126f*pixel.r+.7152f*pixel.g+.0722f*pixel.b;
                proof.luminance.Add(sum/81);ScreenCapture.CaptureScreenshot(Path.Combine(folder,frame.ToString("D3")+".png"));frame++;
            }
            player.scriptedDirection=Vector3.zero;Destroy(patch);proof.metres=Vector3.Distance(start,player.transform.position);proof.frames=frame;
            NeedTower(proof.metres>.7f&&frame>=10,"Top surface review must physically move on the platform");
            File.WriteAllText(Path.Combine(folder,"motion.json"),JsonUtility.ToJson(proof,true));
        }
        yield return TowerWalk(tower.LocalToWorld(new Vector3(-1.05f,15,.60f)));
        yield return TowerWalk(tower.LocalToWorld(tower.GetComponent<ClockTowerInteriorLayout>().machineStandingPoint));
        Debug.Log("CLOCKTOWER_SURFACE_MOTION_PASS: floor and wall captured under real platform motion with normal AO/shadows");
    }
}
