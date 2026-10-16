using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

// Presentation run of the A gabled old inn (B08, HOTEL-01; user, 2026-10-16: record some gameplay
// videos): the gables from the street at dusk in the rain, in through the porch to the register
// hall, the register page (I01), the next page that reads blank until the desk lamp is pressed down
// and its raking light brings out the pressed writing (I02), the dining ledger (I03), then up the
// stair to room six. Same route and interaction points as ReviewHotel, walked at a pace a viewer can
// follow; the porter and the landlady are still placeholder markers, so the camera does not dwell on
// them. Recorded by ShowcaseRecorder with --review --fresh --showcase-hotel; its own save. Not a test.
public partial class DemoApp {
    Vector3 ShowInn(float x,float y,float z)=>HotelInn.Current.transform.TransformPoint(new Vector3(x,y,z));
    PoliceDocumentItem ShowInnPaper(string evidence,string document=null)=>HotelInn.Current.GetComponentsInChildren<PoliceDocumentItem>(true)
        .FirstOrDefault(d=>evidence!=null?d.evidenceId==evidence:d.documentId==document);
    IEnumerator ShowInnWalk(float speed,params Vector3[] points){foreach(var p in points)yield return ShowWalk(p,speed);}
    // Open a record in the viewer, tilt it a little (RotateEvidence adds degrees per call: about 15 degrees
    // of pitch and 24 of yaw in all, so a flat sheet never turns edge-on), square it up again, show its text,
    // put it back.
    IEnumerator ShowInnRead(Interactable item,Vector3 point,float hold) {
        yield return ShowUse(item,point,.8f);yield return ShowHold(1.2f);
        if(ModelEvidence){for(int i=0;i<30;i++){RotateEvidence(new Vector2(-.5f,-.8f));yield return null;}yield return ShowHold(.8f);ResetEvidencePose();yield return ShowHold(.7f);}
        ToggleEvidenceDetails();yield return ShowHold(hold);CloseInspect();yield return ShowHold(.8f);
    }
    IEnumerator ShowcaseHotel() {
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;dayNightReviewFrozen=false;
        state.openingRevision=System.Math.Max(1,state.openingRevision);state.openingStage=4;state.openingEscort=4;state.belongingsCollected=true;
        state.openingRideRevision=1;state.openingRideStage=5;
        // Dusk on the 20th's evening schedule: the shower before half past six, the lamps lit.
        StreetTime(18*60+5);
        yield return new WaitForSeconds(8);
        var inn=HotelInn.Current;if(!inn){ShowAbort("no inn in Town");yield break;}
        float G=inn.Layout.levels.ground,U=inn.Layout.levels.upper,DT=G+.76f;
        var register=ShowInnPaper("I01");var blank=ShowInnPaper(null,"register-next");var lamp=inn.RegisterLamp;
        var ledger=ShowInnPaper("I03");var stove=ShowInnPaper(null,"room6-stove");
        if(!register||!blank||!lamp||!ledger||!stove){ShowAbort("inn records missing");yield break;}
        Silent=false;AudioListener.volume=ShowcaseListenerVolume;
        showcase=player.eye.gameObject.AddComponent<ShowcaseRecorder>();
        showcase.Begin(Path.Combine(captures,"showcase-hotel"));
        // 1. The front from the street: two gables over the porch, stone below, half-timber above.
        showcaseCaption="傍晚 18:05 · B08 老客栈：两座临街山墙，首层毛石、二层半木构";ShowMark("street");
        player.Place(ShowInn(13.4f,.1f,44.6f),180);player.Aim(ShowInn(11.6f,2.2f,31f));
        yield return ShowHold(3);yield return ShowLookAt(ShowInn(11.6f,6.4f,31f),3.5f);yield return ShowHold(2.5f);yield return ShowLookAt(ShowInn(11.6f,2.0f,31f),2.5f);
        // 2. In through the gate and the porch to the register hall.
        showcaseCaption="穿过门廊，走进登记大堂";ShowMark("porch");
        yield return ShowInnWalk(.6f,ShowInn(11.6f,.1f,38.0f),ShowInn(11.6f,.1f,36.0f),ShowInn(11.6f,G,34.0f),ShowInn(11.6f,G,31.2f),ShowInn(11.6f,G,29.8f));
        // The porter's counter (a placeholder figure) is on the left of the way in; look the other way, to the reading desk.
        yield return ShowLookAt(ShowInn(9.5f,1.8f,24.0f),1.6f);yield return ShowHold(2);yield return ShowLookAt(ShowInn(7.0f,1.3f,25.0f),1.4f);yield return ShowHold(1);
        // 3. The register at the reading desk.
        showcaseCaption="查阅桌上的住客登记簿";ShowMark("register");
        yield return ShowInnWalk(.5f,ShowInn(9.0f,G,28.0f),ShowInn(8.55f,G,25.6f));
        yield return ShowLookAt(ShowInn(7.08f,DT,25.35f),1.2f);yield return ShowHold(1.5f);
        yield return ShowInnRead(register,ShowInn(7.08f,DT+.05f,25.15f),4.5f);
        // 4. The next page reads blank; the lamp pressed down rakes across it and the writing appears.
        showcaseCaption="下一页看上去是空白的";ShowMark("blank");
        yield return ShowUse(blank,ShowInn(7.08f,DT+.05f,25.55f),.6f);yield return ShowHold(2.6f);CloseInspect();yield return ShowHold(.6f);
        showcaseCaption="压低台灯：侧光掠过纸面，照出前一页写字留下的压痕";ShowMark("lamp");
        yield return ShowUse(lamp,ShowInn(6.86f,DT+.32f,24.82f),.7f);yield return ShowHold(1.6f);
        yield return ShowLookAt(ShowInn(7.08f,DT,25.4f),1.0f);yield return ShowHold(3);
        var pressed=ShowInnPaper("I02");if(!pressed){ShowAbort("the pressed page did not appear");yield break;}
        yield return ShowInnRead(pressed,ShowInn(7.08f,DT+.05f,25.55f),4.5f);
        // 5. The dining room: the ledger on the cash desk, the fireplace.
        showcaseCaption="餐厅：账桌上的账本";ShowMark("dining");
        yield return ShowInnWalk(.6f,ShowInn(10.0f,G,28.6f),ShowInn(13.0f,G,28.8f),ShowInn(16.0f,G,28.95f),ShowInn(17.6f,G,28.95f),ShowInn(17.6f,G,30.4f));
        // Toward the fireplace: the landlady's placeholder stands by the cash desk behind.
        yield return ShowLookAt(ShowInn(21.9f,1.4f,27.4f),1.6f);yield return ShowHold(2.5f);
        yield return ShowInnRead(ledger,ShowInn(17.25f,DT+.04f,31.35f),4.5f);
        // 6. Up the stair: the landing, the corridor, room six.
        showcaseCaption="上楼：楼梯平台和二层走廊";ShowMark("stair");
        // Back out to the dining aisle first: a straight line from the cash desk to the hall runs into the dining chairs.
        yield return ShowInnWalk(.6f,ShowInn(17.6f,G,28.95f),ShowInn(16.0f,G,28.95f),ShowInn(13.0f,G,28.0f),ShowInn(13.0f,G,23.8f),ShowInn(15.6f,G,23.5f),ShowInn(15.6f,G,22.05f));
        yield return ShowInnWalk(.5f,ShowInn(14.3f,G,22.05f),ShowInn(12.0f,G,22.05f),ShowInn(9.65f,U,22.05f),ShowInn(9.65f,U,24.4f));
        if(player.transform.position.y<U-.2f){ShowAbort("did not reach the upper floor");yield break;}
        yield return ShowLookAt(ShowInn(13.0f,1.0f,22.0f),1.4f);yield return ShowHold(1.5f);
        showcaseCaption="六号房";ShowMark("room6");
        yield return ShowInnWalk(.6f,ShowInn(9.65f,U,26.4f),ShowInn(20.6f,U,26.4f),ShowInn(20.6f,U,24.2f));
        yield return ShowLookAt(ShowInn(19.3f,U+.6f,22.4f),1.4f);yield return ShowHold(2);
        yield return ShowInnWalk(.5f,ShowInn(21.0f,U,23.0f));
        yield return ShowInnRead(stove,ShowInn(22.0f,U+.55f,21.72f),3.5f);
        yield return ShowLookAt(ShowInn(26.0f,1.0f,28.0f),1.6f);yield return ShowHold(3);
        showcaseCaption=null;ShowMark("end");yield return ShowHold(1.5f);
        showcase.End();AudioListener.volume=0;
        Debug.Log("SHOWCASE_HOTEL_DONE");Application.Quit();
    }
}
