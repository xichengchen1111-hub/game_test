using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Evidence showcase (user, 2026-10-19: "把所有的证据建模都优化一下然后给我看").
// --evidence-gallery opens every loaded evidence record in the normal inspection, one after another
// (← → or A/D), on its own save so the player's progress is untouched; Esc leaves the app.
// --review --fresh --review-evidence-gallery captures each one front-on and turned over, then quits.
public partial class DemoApp {
    List<Interactable> evidenceGallery;int evidenceGalleryIndex;
    [Serializable] class GalleryEntry { public string id,title,kind; public bool physical; }
    [Serializable] class GalleryResult { public bool passed; public int count; public List<GalleryEntry> items=new List<GalleryEntry>(); public string verification; }

    List<Interactable> GalleryItems() {
        bool Shown(Interactable i){
            // A set is listed once, by its first sheet (which may be an envelope with no evidence ID);
            // its later sheets are read by turning pages, not listed on their own.
            var pageOf=i.GetComponent<EvidencePageOf>();if(pageOf&&pageOf.book&&pageOf.book.Count>1)return pageOf.index==0;
            if(string.IsNullOrEmpty(i.evidenceId)||i.evidenceId.StartsWith("pack-")||i.evidenceId=="E04")return false;
            if(i is HouseContainer||i is ClockTowerAdjustment)return false;
            // Testimony is heard, not held: only things with a printed face, a physical model or a solid model.
            return i.illustration||i.GetComponentInChildren<EvidencePhysical>()||EvidenceSolidModel.Handles(i.evidenceId)||TicketImage(i.evidenceId);
        }
        return FindObjectsByType<Interactable>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(Shown)
            .GroupBy(GalleryKey).Select(g=>g.OrderByDescending(i=>i.GetComponent<EvidenceBook>()?2:i.GetComponentInChildren<EvidencePhysical>()?1:0).First())
            .OrderBy(i=>GalleryKey(i)[0]).ThenBy(GalleryKey,StringComparer.Ordinal).ToList();
    }
    static string GalleryKey(Interactable i){
        // A set keyed by its first evidence-bearing sheet, so the garage envelope sorts with W01.
        var book=i.GetComponent<EvidenceBook>();
        if(book)for(int k=0;k<book.Count;k++){var p=book.Page(k);if(!string.IsNullOrEmpty(p.evidenceId))return p.evidenceId.ToUpperInvariant();}
        return string.IsNullOrEmpty(i.evidenceId)?"DOC-"+i.name:i.evidenceId.ToUpperInvariant();
    }
    void ShowGalleryItem(int index) {
        if(evidenceGallery==null||evidenceGallery.Count==0)return;
        evidenceGalleryIndex=(index%evidenceGallery.Count+evidenceGallery.Count)%evidenceGallery.Count;
        Inspect(evidenceGallery[evidenceGalleryIndex]);
    }
    IEnumerator EvidenceGalleryRun(bool review) {
        // Open the first record in the same frame the title card is dismissed, so no opening cutscene starts underneath.
        evidenceGallery=GalleryItems();
        Debug.Log("EVIDENCE_GALLERY_ITEMS: "+string.Join(",",evidenceGallery.Select(i=>i.evidenceId)));
        if(evidenceGallery.Count==0){Debug.LogError("EVIDENCE_GALLERY_FAIL: no evidence loaded");Application.Quit(1);yield break;}
        opening=false;ShowGalleryItem(0);CursorMode();
        if(!review) {
            while(true) {
                if(!inspected){Save();Application.Quit();yield break;}
                // A book turns its pages first, then moves on to the next record.
                if(Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.D)){if(!TurnEvidencePage(1)&&!evidenceFlipHinge)ShowGalleryItem(evidenceGalleryIndex+1);}
                if(Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.A)){if(!TurnEvidencePage(-1)&&!evidenceFlipHinge)ShowGalleryItem(evidenceGalleryIndex-1);}
                yield return null;
            }
        }
        var result=new GalleryResult();
        for(int i=0;i<evidenceGallery.Count;i++) {
            var item=evidenceGallery[i];
            ShowGalleryItem(i);
            if(!ModelEvidence||!evidenceObject){Debug.LogError("EVIDENCE_GALLERY_FAIL: no model for "+item.evidenceId);Application.Quit(1);yield break;}
            yield return new WaitForSecondsRealtime(.45f);
            string id=GalleryKey(item);
            yield return Capture(id+"-a-front");
            RotateEvidence(new Vector2(-22,-150));yield return new WaitForSecondsRealtime(.5f);yield return Capture(id+"-b-turned");
            ResetEvidencePose();RotateEvidence(new Vector2(52,28));yield return new WaitForSecondsRealtime(.5f);yield return Capture(id+"-c-edge");
            ResetEvidencePose();
            if(evidenceBook&&evidenceBook.style==EvidenceBook.Style.Stack) {
                // Every sheet of the set in turn, then back to the first.
                for(int k=1;k<evidenceBook.Count;k++){TurnEvidencePage(1);yield return new WaitForSecondsRealtime(.27f);yield return Capture(id+"-d"+k+"-turning");
                    yield return new WaitForSecondsRealtime(.6f);yield return Capture(id+"-e"+k+"-sheet"+(k+1));
                    var sheet=evidenceBook.Page(k);if(!string.IsNullOrEmpty(sheet.evidenceId)&&!state.entries.Exists(e=>e.id==sheet.evidenceId)){Debug.LogError("EVIDENCE_GALLERY_FAIL: turning to "+sheet.evidenceId+" did not record it");Application.Quit(1);yield break;}}
                ToggleEvidenceDetails();yield return new WaitForSecondsRealtime(.4f);yield return Capture(id+"-f-last-text");ToggleEvidenceDetails();
                TurnEvidencePage(-1);yield return new WaitForSecondsRealtime(.27f);yield return Capture(id+"-g-back-turning");yield return new WaitForSecondsRealtime(.6f);
                Debug.Log("EVIDENCE_SET_PAGES: "+id+" "+evidenceBook.Count);
            }
            else if(evidenceBook) {
                // Turn to the next page with the lamp up (plain), then lowered (pressed writing).
                yield return new WaitForSecondsRealtime(.3f);
                TurnEvidencePage(1);yield return new WaitForSecondsRealtime(.27f);yield return Capture(id+"-d-turning");
                yield return new WaitForSecondsRealtime(.6f);yield return Capture(id+"-e-page2-plain");
                var lamp=HotelInn.Current?HotelInn.Current.RegisterLamp:null;
                if(lamp){if(!lamp.Lowered)lamp.Use();yield return new WaitForSecondsRealtime(1.2f);
                    TurnEvidencePage(-1);yield return new WaitForSecondsRealtime(.9f);yield return Capture(id+"-f-back-page1");
                    TurnEvidencePage(1);yield return new WaitForSecondsRealtime(.9f);yield return Capture(id+"-g-page2-raked");
                    ToggleEvidenceDetails();yield return new WaitForSecondsRealtime(.4f);yield return Capture(id+"-h-page2-text");ToggleEvidenceDetails();
                    if(!state.entries.Exists(e=>e.id=="I02")){Debug.LogError("EVIDENCE_GALLERY_FAIL: turning to the pressed page did not record I02");Application.Quit(1);yield break;}
                    Debug.Log("EVIDENCE_BOOK_PAGES: "+evidenceBook.Count+" recorded I02 by turning the page");}
            }
            result.items.Add(new GalleryEntry{id=id,title=item.title,kind=evidenceKindLabel,physical=item.GetComponentInChildren<EvidencePhysical>()});
            CloseInspect();yield return null;
            if(evidenceStage){Debug.LogError("EVIDENCE_GALLERY_FAIL: stage not released after "+id);Application.Quit(1);yield break;}
        }
        // One detail page, to check the reading layout still sits beside the rebuilt models.
        foreach(var id in new[]{"E10","Y01","H14","OPENING-NEWSPAPER"}){int k=evidenceGallery.FindIndex(e=>e.evidenceId.ToUpperInvariant()==id);if(k<0)continue;
            ShowGalleryItem(k);ToggleEvidenceDetails();yield return new WaitForSecondsRealtime(.45f);yield return Capture("details-"+id);CloseInspect();yield return null;}
        result.passed=true;result.count=result.items.Count;
        result.verification="Program-driven: shared Inspect/rotate/reset/detail handlers on every loaded evidence record; not physical input or a blind playtest.";
        File.WriteAllText(Path.Combine(captures,"evidence-gallery.json"),JsonUtility.ToJson(result,true));
        Debug.Log("EVIDENCE_GALLERY_PASS: "+result.count);Application.Quit();
    }
}
