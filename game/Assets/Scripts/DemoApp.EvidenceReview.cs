using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
public partial class DemoApp {
    IEnumerator ReviewEvidencePresentation(){
        var home=HouseInstance.Current;var medical=home.GetComponentsInChildren<HouseContainer>().Single(c=>c.fixtureId=="home-cabinet-medical");
        player.Place(home.Point(2.25f,.16f,4.55f),0);if(!medical.IsOpen)medical.SetOpen(true);yield return new WaitForSeconds(1);
        var paper=medical.contents.Single(i=>i.evidenceId=="H09");var originalTransform=paper.transform.localToWorldMatrix;var originalMaterial=paper.GetComponentInChildren<Renderer>().sharedMaterial;
        AimVisibleHomePaper(paper);yield return null;NeedHome(TryInteract()&&inspected==paper,"Actual drawer paper opens studio view");
        NeedHome(!evidenceDetails&&!evidenceCamera.orthographic,"Perspective model first");yield return new WaitForSeconds(.3f);yield return Capture("01-paper-front");
        var start=evidenceObject.transform.localRotation;var recorder=player.eye.gameObject.AddComponent<WeatherClipRecorder>();recorder.Begin(Path.Combine(captures,"paper-rotation"));
        float began=Time.unscaledTime;while(Time.unscaledTime-began<4){float dt=Time.unscaledDeltaTime;RotateEvidence(new Vector2(0,90*dt));yield return null;}recorder.End();Destroy(recorder);
        RotateEvidence(new Vector2(0,155));yield return new WaitForSeconds(.35f);yield return Capture("02-paper-back");
        ZoomEvidence(100);yield return new WaitForSeconds(.3f);NeedHome(evidenceZoom<=1.501f,"Upper zoom bound");ZoomEvidence(-100);yield return new WaitForSeconds(.3f);NeedHome(evidenceZoom>=.699f,"Lower zoom bound");
        ResetEvidencePose();yield return new WaitForSeconds(.4f);NeedHome(Quaternion.Angle(evidenceObject.transform.localRotation,start)<.1f&&Mathf.Abs(evidenceZoom-1)<.001f,"R restores rotation and zoom");
        ToggleEvidenceDetails();double clock=ElapsedGameMinutes;var feet=player.transform.position;player.scriptedDirection=Vector3.forward;yield return new WaitForSeconds(.5f);player.scriptedDirection=Vector3.zero;
        NeedHome(Blocked&&Vector3.Distance(feet,player.transform.position)<.01f&&ElapsedGameMinutes>clock,"Reading blocks motion, time runs");NeedHome(!EvidenceModelArea(new Vector2(Screen.width*.9f,Screen.height*.5f)),"Reading area excludes model controls");yield return Capture("03-paper-details");
        ToggleEvidenceDetails();yield return new WaitForSeconds(.35f);NeedHome(!evidenceDetails,"E returns to name-only model");CloseInspect();yield return null;
        NeedHome(!Blocked&&!evidenceStage&&evidenceOwnedMaterials.Count==0&&evidenceOwnedMeshes.Count==0,"Close releases preview and movement");NeedHome(paper.transform.localToWorldMatrix==originalTransform&&paper.GetComponentInChildren<Renderer>().sharedMaterial==originalMaterial,"Original evidence stays unchanged");
        foreach(string id in new[]{"E09","E10","E02","E03","E06","E07","E08","H17","H18"}){
            if(id=="E02")InspectCarriedTicket();else Inspect(FindObjectsByType<Interactable>(FindObjectsSortMode.None).First(i=>i.evidenceId==id));
            NeedHome(ModelEvidence&&!evidenceDetails,"Model-first "+id);yield return new WaitForSeconds(.3f);yield return Capture("model-"+id);
            if(id=="H17"){RotateEvidence(new Vector2(15,45));yield return new WaitForSeconds(.4f);yield return Capture("solid-turned");}
            if(id=="E10"){ToggleEvidenceDetails();yield return new WaitForSeconds(.35f);yield return Capture("long-letter-details");}
            CloseInspect();yield return null;
        }
        File.WriteAllText(Path.Combine(captures,"evidence-results.json"),"{\"passed\":true,\"actualMedicalPaperRay\":true,\"perspectiveModels\":10,\"rotationZoomReset\":true,\"modelFirstAndDetails\":true,\"readingClock\":true,\"originalUnchanged\":true,\"resourceCleanup\":true,\"verification\":\"Native renderer; actual medical drawer ray; shared rotation/zoom/reset/detail input handlers. Other cases use Inspect directly. Not physical keyboard/mouse or blind testing.\"}");
        Debug.Log("EVIDENCE_REVIEW_PASS");Application.Quit();
    }
}
