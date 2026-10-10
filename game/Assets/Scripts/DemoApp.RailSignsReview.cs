using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public partial class DemoApp {
    readonly List<RailSignRecord> railSignRecords=new List<RailSignRecord>();
    bool RecordRailSigns=>Array.IndexOf(Environment.GetCommandLineArgs(),"--record-rail-signs")>=0;
    IEnumerator ReviewRailSigns(string scene) {
        var start=player.transform.position;float yaw=player.transform.eulerAngles.y;
        var signs=FindObjectsByType<RailWorldSign>(FindObjectsSortMode.None).OrderBy(s=>s.lettering.text).ThenBy(s=>s.transform.position.x).ToArray();
        NeedRail(signs.Length==(scene=="town"?14:7),"unexpected rail sign count in "+scene);
        int index=0;
        foreach(var sign in signs) {
            Physics.SyncTransforms();NeedRail(sign.Validate(out var issue),"sign "+sign.lettering.text+": "+issue);
            var feet=sign.transform.position-sign.transform.forward*Mathf.Max(1.4f,sign.faceSize.x*.9f);
            NeedRail(Physics.Raycast(new Vector3(feet.x,1.5f,feet.z),Vector3.down,out var floor,3,~(1<<2),QueryTriggerInteraction.Ignore),"no viewing floor for "+sign.lettering.text);
            feet.y=floor.point.y+.025f;player.Place(feet,0);player.Aim(sign.transform.position);
            string capture=scene+"-sign-"+(++index).ToString("00");yield return Capture(capture);
            // Use actual changing font geometry, rather than relying on an assumed character width.
            int originalSize=sign.lettering.fontSize;sign.lettering.fontSize=80;
            sign.font.RequestCharactersInTexture(sign.lettering.text,80);yield return null;
            NeedRail(sign.Validate(out issue),"larger glyphs escaped "+sign.lettering.text+": "+issue);
            sign.lettering.fontSize=originalSize;sign.font.RequestCharactersInTexture(sign.lettering.text,originalSize);yield return null;
            NeedRail(sign.Validate(out issue),"restored glyphs escaped "+sign.lettering.text+": "+issue);
            railSignRecords.Add(new RailSignRecord{scene=scene,text=sign.lettering.text,surface=sign.surface.name,faceSize=sign.faceSize,faceGap=sign.faceGap,position=sign.transform.position,capture=capture+".png",fontResizePassed=true});
            if(sign.lettering.text.Contains("出站")||sign.lettering.text.Contains("候车厅")) {
                player.Place(feet+sign.transform.right*.7f,0);player.Aim(sign.transform.position);yield return Capture(capture+"-oblique");
            }
        }
        player.Place(start,yaw);
        File.WriteAllText(Path.Combine(captures,"sign-results.json"),JsonUtility.ToJson(new RailSignsResult{passed=true,width=Screen.width,height=Screen.height,signs=railSignRecords.ToArray(),verification="Native Unity frames at staged player viewpoints; actual renderer bounds and rays into each physical face, including a larger-font rebuild. The accompanying rail route uses the real CharacterController and E rays. Not physical input or blind playtest."},true));
        Debug.Log("RAIL_SIGN_REVIEW_PASS: "+scene+" "+signs.Length+" signs");
    }
    [Serializable] class RailSignRecord {public string scene,text,surface,capture;public Vector2 faceSize;public Vector3 position;public float faceGap;public bool fontResizePassed;}
    [Serializable] class RailSignsResult {public bool passed;public int width,height;public string verification;public RailSignRecord[] signs;}
}
