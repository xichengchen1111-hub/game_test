using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Presentation recording of the first meeting with the child (the bedside cinematic), for the
// user to watch: --review --fresh --showcase-hospital-cg. The real controller walks down the
// ward, the ordinary E ray opens the talk, and the lines go on at a viewer's reading pace with
// the same answers a player might give. Recorded by ShowcaseRecorder into <captures>/showcase-
// hospital-cg; tools/hospital/encode_cinematic.py makes the video. Its own save; not a test.
public partial class DemoApp {
    IEnumerator ShowcaseHospitalCinematic() {
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;dayNightReviewFrozen=false;
        yield return new WaitForSeconds(1.5f);
        if(!HospitalBuilding.Current||state.hospitalStage!=0){ShowAbort("needs a fresh hospital visit");yield break;}
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);DayReviewTime(22*60+40);   // the visit is at night
        var child=HospitalBuilding.Current.GetComponentsInChildren<HospitalActor>().FirstOrDefault(a=>a.role=="child");
        if(!child||!child.GetComponent<HospitalBedPose>()){ShowAbort("no child in her bed");yield break;}
        var bed=HospitalBuilding.Current.Layout.furniture.Single(f=>f.kind=="wardbed"&&f.extra=="child");
        // In the ward aisle, two beds down from hers.
        player.Place(HospitalPoint(3.3f,bed.z+3.4f),0);player.Aim(child.transform.position+Vector3.up*.45f);
        yield return ShowHold(1f);
        // The recorder taps the listener, so the mix has to be above zero; it stays very quiet in the room.
        AudioListener.volume=ShowcaseListenerVolume;
        showcase=player.eye.gameObject.AddComponent<ShowcaseRecorder>();
        showcase.Begin(Path.Combine(captures,"showcase-hospital-cg"));showcase.Mark("ward");
        yield return ShowHold(1.2f);
        yield return ShowWalk(HospitalPoint(4.6f,bed.z+1.2f),.7f,6);
        yield return ShowWalk(HospitalPoint(5.35f,bed.z),.6f,10);
        yield return ShowUse(child,child.transform.position+Vector3.up*.42f,1.1f);
        if(!ChildCinematicActive){ShowAbort("the first meeting did not open as a cinematic");yield break;}
        showcase.Mark("cinematic");
        // The same answers as the hospital review: arrived late, the truth about her mother, an honest
        // promise to come, and the promise to find the person.
        int[] picks={0,0,1,0};int pick=0;string last=null;
        while(ChildCinematicActive) {
            if(!DialogueOpen||cinematicEnding){if(cinematicCard!=null&&last!="card"){last="card";showcase.Mark("card");}yield return null;continue;}
            string id=dialogueNode.id;
            // A viewer needs the line and the moving shot: at least four and a half seconds, longer for long lines.
            float read=Mathf.Max(id=="resolve"?5.2f:4.6f,1.6f+dialogueNode.text.Length*.11f);
            if(dialogueChoices.Count>0) {
                yield return ShowHold(read);if(!DialogueOpen||dialogueNode.id!=id)continue;
                yield return ShowHold(1.6f); // the choices are read, then one is taken
                if(pick>=picks.Length||picks[pick]>=dialogueChoices.Count){ShowAbort("unexpected choice at "+id);yield break;}
                SelectDialogue(picks[pick++]);
            } else {
                yield return ShowHold(read);
                if(ChildCinematicActive&&DialogueOpen&&!cinematicEnding&&dialogueNode.id==id)AdvanceDialogue();
            }
            yield return null;
        }
        // Back at the bedside: the everyday talk goes on in the ordinary dialogue view.
        showcase.Mark("bedside");
        yield return ShowHold(4.2f);
        CloseDialogue();yield return ShowHold(1.6f);
        showcase.Mark("end");yield return ShowHold(.8f);
        showcase.End();AudioListener.volume=0;
        Debug.Log("SHOWCASE_HOSPITAL_CG_DONE");Application.Quit();
    }
}
