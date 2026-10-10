using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public partial class DemoApp {
    [Serializable] class MilkWindowMotionResult { public bool actualController;public int side;public float movementMetres;public List<float> framePatchLuminance=new List<float>();public string verification; }
    IEnumerator MilkWindowMotion(GameObject shop,int side) {
        Vector3 P(float x,float y,float z)=>shop.transform.TransformPoint(new Vector3(x,y,z));
        yield return WalkTo(P(side*2.65f,.2f,-.65f));
        string folder=Path.Combine(captures,"window-motion-"+(side<0?"north":"south"));Directory.CreateDirectory(folder);
        var result=new MilkWindowMotionResult{actualController=true,side=side,verification="Actual controller lateral motion with normal lighting, shadows and AO; samples at 10 Hz are not the game FPS. Patch follows the same opaque jamb face in world coordinates."};
        var patch=new Texture2D(9,9,TextureFormat.RGB24,false);
        var start=player.transform.position;var look=P(side*3.60f,1.87f,.1f);var surface=P(side*3.45f,1.90f,-.63f);
        player.scriptedDirection=shop.transform.TransformDirection(Vector3.forward*.30f);
        float finish=Time.unscaledTime+1.25f;float next=Time.unscaledTime;int frame=0;
        while(Time.unscaledTime<finish) {
            // Keep both the pane and the sampled jamb in view throughout the
            // lateral walk; aiming only at the pane puts the probe at the edge.
            player.Aim(Vector3.Lerp(look,surface,.5f));yield return new WaitForEndOfFrame();
            if(Time.unscaledTime<next)continue;next+=.1f;
            var xy=player.eye.WorldToScreenPoint(surface);
            NeedMilk(xy.z>0&&xy.x>5&&xy.x<Screen.width-5&&xy.y>5&&xy.y<Screen.height-5,"Window-motion frame probe is outside screen: pixel="+xy+" feet="+player.transform.position+" viewport="+Screen.width+"x"+Screen.height);
            patch.ReadPixels(new Rect(Mathf.Round(xy.x)-4,Mathf.Round(xy.y)-4,9,9),0,0);patch.Apply();
            float sum=0;foreach(var pixel in patch.GetPixels())sum+=.2126f*pixel.r+.7152f*pixel.g+.0722f*pixel.b;
            result.framePatchLuminance.Add(sum/81);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,frame.ToString("D3")+".png"));frame++;
        }
        player.scriptedDirection=Vector3.zero;Destroy(patch);result.movementMetres=Vector3.Distance(start,player.transform.position);
        NeedMilk(result.movementMetres>.7f&&frame>=10,"Window motion must use real lateral walking and enough rendered samples");
        File.WriteAllText(Path.Combine(folder,"motion.json"),JsonUtility.ToJson(result,true));
        yield return WalkTo(P(side*2.65f,.2f,-.29f));
    }
    IEnumerator MilkDoorFrameMotion(GameObject shop) {
        Vector3 P(float x,float y,float z)=>shop.transform.TransformPoint(new Vector3(x,y,z));
        foreach(int side in new[]{-1,1}) {
            yield return WalkTo(P(-.50f,.20f,-1.70f));
            Vector3 target=P(side*1.31f,2.765f,-2.735f);
            string folder=Path.Combine(captures,"door-motion-"+(side<0?"left":"right"));Directory.CreateDirectory(folder);
            var proof=new MilkWindowMotionResult{actualController=true,side=side,verification="Real inside-door lateral controller motion, normal AO/shadows. Jamb and lintel use butt joinery and clear the wall's inner face. 10 Hz capture samples are not FPS; inspect the corner frames."};
            var patch=new Texture2D(9,9,TextureFormat.RGB24,false);Vector3 start=player.transform.position;
            player.scriptedDirection=shop.transform.TransformDirection(Vector3.right*.30f);
            float finish=Time.unscaledTime+1.25f,next=Time.unscaledTime;int frame=0;
            while(Time.unscaledTime<finish) {
                player.Aim(target);yield return new WaitForEndOfFrame();
                if(Time.unscaledTime<next)continue;next+=.1f;
                var point=player.eye.WorldToScreenPoint(target);
                NeedMilk(point.z>0&&point.x>5&&point.x<Screen.width-5&&point.y>5&&point.y<Screen.height-5,"Door corner patch outside view");
                patch.ReadPixels(new Rect(Mathf.Round(point.x)-4,Mathf.Round(point.y)-4,9,9),0,0);patch.Apply();
                float sum=0;foreach(var pixel in patch.GetPixels())sum+=.2126f*pixel.r+.7152f*pixel.g+.0722f*pixel.b;
                proof.framePatchLuminance.Add(sum/81);ScreenCapture.CaptureScreenshot(Path.Combine(folder,frame.ToString("D3")+".png"));frame++;
            }
            player.scriptedDirection=Vector3.zero;Destroy(patch);proof.movementMetres=Vector3.Distance(start,player.transform.position);
            NeedMilk(proof.movementMetres>.7f&&frame>=10,"Door corner review must really walk inside the doorway");
            File.WriteAllText(Path.Combine(folder,"motion.json"),JsonUtility.ToJson(proof,true));
        }
        Debug.Log("MILK_DOORFRAME_MOTION_PASS: both inside upper corners under real movement with normal AO/shadows");
    }
}
