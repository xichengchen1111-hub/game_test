using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class DemoApp {
    void NeedMilk(bool condition,string why) {
        if(condition)return;
        Debug.LogError("MILK_INTERIOR_REVIEW_FAIL: "+why);Application.Quit(1);throw new InvalidOperationException(why);
    }
    IEnumerator ReviewMilkInteriorA() {
        yield return new WaitForSeconds(.8f);
        var shop=GameObject.Find("MilkShop");var placement=shop.GetComponent<MilkShopPlacement>();
        var book=shop.GetComponentInChildren<MilkOrderBook>();
        var lamp=shop.GetComponentsInChildren<MilkShopFixture>().Single(f=>f.action==MilkShopFixture.Action.ReadingLamp);
        var bell=shop.GetComponentsInChildren<MilkShopFixture>().Single(f=>f.action==MilkShopFixture.Action.Bell);
        Vector3 P(float x,float y,float z)=>shop.transform.TransformPoint(new Vector3(x,y,z));
        int identity=shop.GetInstanceID(),scenes=SceneManager.sceneCount,entries=state.entries.Count;
        NeedMilk(book.Pages.Count==2&&book.Pages[0].DateId=="1931-01-19"&&book.Pages[1].DateId=="1931-01-20","Approved historical dates differ");
        NeedMilk(lamp.LampOn&&shop.GetComponentsInChildren<Light>().Length==5,"Three fixture lights and two room fills required");
        NeedMilk(shop.GetComponentsInChildren<Light>().All(l=>l.shadows==LightShadows.None),"Added realtime shadow budget");
        var windows=shop.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Windows");
        var glass=windows.sharedMaterials.Single(m=>m.name=="OldWindow");
        NeedMilk(glass.shader.name=="WrongDate/ClearGlass"&&glass.renderQueue==3000&&glass.GetColor("_Tint").a<=.06f,"Existing clear window material changed");
        player.Place(placement.outside,0);yield return WalkTo(placement.outside);yield return WalkTo(placement.inside);
        player.Aim(P(0,1.98f,1.75f));yield return Capture("milk-a-interior");
        NeedMilk(insideMilkShop&&location=="青川 · 牛奶屋","Milk-shop location label failed");
        yield return WalkTo(P(-.85f,.20f,-.02f));
        player.Aim(book.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&MilkOrdersOpen,"Counter ledger is not reachable by the normal three-metre ray");
        NeedMilk(CurrentMilkOrder.DateId=="1931-01-20"&&CurrentMilkOrder.deliveries[0].shop=="旅馆"&&CurrentMilkOrder.deliveries[0].bottles==3&&CurrentMilkOrder.deliveries[0].delivered,"20 January record incorrect");
        var position=player.transform.position;var rotation=player.transform.rotation;double clock=ElapsedGameMinutes;
        player.scriptedDirection=Vector3.forward;yield return new WaitForSeconds(.5f);player.scriptedDirection=Vector3.zero;
        NeedMilk(Blocked&&!GameTimePaused&&Vector3.Distance(position,player.transform.position)<.001f&&Quaternion.Angle(rotation,player.transform.rotation)<.001f&&ElapsedGameMinutes-clock>.5*GameClock.GameMinutesPerRealSecond*.9,"Ledger must block movement/look while time continues");
        NeedMilk(Cursor.visible&&Cursor.lockState==CursorLockMode.None,"Reading cursor isn't free");
        yield return Capture("milk-a-orders-jan20");
        MoveMilkOrderDay(-1);MoveMilkOrderDay(-1);
        NeedMilk(CurrentMilkOrder.DateId=="1931-01-19"&&CurrentMilkOrder.deliveries[0].shop=="钟表铺"&&CurrentMilkOrder.deliveries[0].bottles==2,"19 January record or navigation boundary incorrect");
        yield return Capture("milk-a-orders-jan19");MoveMilkOrderDay(1);MoveMilkOrderDay(1);
        NeedMilk(CurrentMilkOrder.DateId=="1931-01-20","Next-day boundary overflow");MoveMilkOrderDay(-1);CloseMilkOrders();
        NeedMilk(!Blocked&&state.entries.Count==entries&&!state.entries.Any(e=>e.id=="E05"),"Ordinary ledger granted evidence or failed to close");
        // Walk around the child through the customer aisle, preserving his collision.
        yield return WalkTo(P(-.85f,.20f,-1.10f));yield return WalkTo(P(.90f,.20f,-1.10f));
        yield return WalkTo(P(.90f,.20f,.05f));player.Aim(lamp.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&!lamp.LampOn&&!lamp.readingLight.enabled,"Lamp cannot switch off through actual ray");
        yield return Capture("milk-a-reading-lamp-off");
        NeedMilk(TryInteract()&&lamp.LampOn&&lamp.readingLight.enabled,"Lamp cannot switch on");yield return Capture("milk-a-counter");
        NeedMilk(TryInteract()&&!lamp.LampOn,"Lamp off-state cannot be saved");
        yield return WalkTo(P(.90f,.20f,-1.10f));yield return WalkTo(P(-.65f,.20f,-1.10f));
        yield return WalkTo(P(-.65f,.20f,.02f));player.Aim(bell.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&bell.BellRings==1&&bell.bellAudio.isPlaying,"Bell isn't reachable or doesn't play audio");
        var tag=shop.GetComponentsInChildren<Interactable>().Single(i=>i.title=="鲜奶瓶签");
        player.Aim(tag.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&inspected==tag,"Bottle tag is unreachable");yield return Capture("milk-a-bottle-tag");CloseInspect();
        yield return WalkTo(P(-.66f,.20f,-1.10f));yield return WalkTo(P(-1.60f,.20f,-1.75f));
        var churn=shop.GetComponentsInChildren<Interactable>().Single(i=>i.title=="回收奶桶");
        player.Aim(churn.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&inspected==churn,"Original empty churn is unreachable");yield return Capture("milk-a-return-churn");CloseInspect();
        NeedMilk(state.entries.Count==entries,"Daily props granted investigation cards");
        yield return WalkTo(placement.inside);yield return WalkTo(placement.outside);yield return WalkTo(placement.inside);
        NeedMilk(CurrentScene=="Town"&&fade==0&&shop.GetInstanceID()==identity&&SceneManager.sceneCount==scenes,"Milk-shop walk used transition or replaced geometry");
        // Same real side-window round trip as the existing clear-glass review.
        for(int side=-1;side<=1;side+=2) {
            yield return WalkTo(P(side*2.65f,.20f,-1.10f));yield return WalkTo(P(side*2.65f,.20f,-.29f));
            player.Aim(P(side*3.615f,2.08f,-.29f));yield return Capture("milk-a-window-"+(side<0?"north":"south"));
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--record-milk-windows")>=0)yield return MilkWindowMotion(shop,side);
            player.scriptedDirection=shop.transform.TransformDirection(Vector3.right*side);
            yield return new WaitForSeconds(.9f);player.scriptedDirection=Vector3.zero;
            float reached=side*shop.transform.InverseTransformPoint(player.transform.position).x;
            NeedMilk(reached>3&&reached<3.35f,"Side window no longer blocks crossing");
            yield return WalkTo(P(side*2.65f,.20f,-1.10f));yield return WalkTo(placement.inside);
        }
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--record-milk-doorframe")>=0) {
            yield return MilkDoorFrameMotion(shop);
            yield return WalkTo(placement.inside);
        }
        // Historical pages must also remain fixed after a current-clock midnight wrap.
        double savedTime=ElapsedGameMinutes;gameClock=new GameClock(365.5);player.Aim(book.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&CurrentMilkOrder.DateId=="1931-01-19","Reopening or midnight rewrote historical paper dates");CloseMilkOrders();gameClock=new GameClock(savedTime);
        player.Aim(P(0,1.9f,1.60f));Save();
        NeedMilk(state.openFixtures.Contains("milk-reading-lamp-off")&&state.milkOrderDate=="1931-01-19","New fixture/selected page state not saved");
        // Controlled exterior close-ups complement the actual interior/window walking above.
        var eyePosition=player.eye.transform.localPosition;var eyeRotation=player.eye.transform.localRotation;
        player.enabled=false;reviewPortrait=true;
        for(int side=-1;side<=1;side+=2) {
            player.eye.transform.position=P(side*4.15f,1.99f,-.35f);
            player.eye.transform.LookAt(P(side*2.5f,1.8f,.15f));
            yield return Capture("milk-a-window-"+(side<0?"north":"south")+"-outside");
        }
        player.eye.transform.localPosition=eyePosition;player.eye.transform.localRotation=eyeRotation;reviewPortrait=false;player.enabled=true;
        File.WriteAllText(Path.Combine(captures,"milk-a-results.json"),JsonUtility.ToJson(new MilkReviewResult{passed=true,width=Screen.width,height=Screen.height,continuousEntry=true,actualRayAllFiveObjects=true,orderDatesAndQuantities=true,movementAndLookBlocked=true,timeContinues=true,noEvidenceGranted=true,bothWindowCollisions=true,lampPersistence=true,originalShellUnchanged=true,verification="Native rendered player; scripted CharacterController and normal TryInteract rays. Page navigation invokes the same UI handlers. Not physical keyboard input or blind playtest."},true));
        Debug.Log("MILK_INTERIOR_A_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyMilkInteriorSave() {
        yield return new WaitForSeconds(.7f);
        var shop=GameObject.Find("MilkShop");var lamp=shop.GetComponentsInChildren<MilkShopFixture>().Single(f=>f.action==MilkShopFixture.Action.ReadingLamp);
        var book=shop.GetComponentInChildren<MilkOrderBook>();var p=shop.transform.InverseTransformPoint(player.transform.position);
        NeedMilk(clockSaveLoadedAtStartup&&CurrentScene=="Town"&&Math.Abs(p.x+.66f)<.15f&&Math.Abs(p.z+1.1f)<.15f,"Separate process did not restore interior position");
        NeedMilk(!lamp.LampOn&&!lamp.readingLight.enabled&&state.milkOrderDate=="1931-01-19","Separate process lost reading-lamp/ledger state");
        int count=state.entries.Count;player.Aim(book.GetComponent<Collider>().bounds.center);yield return null;
        NeedMilk(TryInteract()&&MilkOrdersOpen&&CurrentMilkOrder.DateId=="1931-01-19","Reloaded ledger cannot reopen with selected date");
        yield return Capture("milk-a-orders-reloaded");CloseMilkOrders();
        NeedMilk(state.entries.Count==count&&!Blocked,"Reload changed evidence or left movement blocked");
        yield return WalkTo(shop.GetComponent<MilkShopPlacement>().outside);
        yield return Capture("milk-a-exit-reloaded");
        File.WriteAllText(Path.Combine(captures,"milk-a-save-relaunch.txt"),"PASS: separate process restored interior position, elapsed clock, lamp off and selected Jan19 page; normal ray reopened ledger; controller walked out.\n");
        Debug.Log("MILK_INTERIOR_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    [Serializable] class MilkReviewResult {
        public bool passed,continuousEntry,actualRayAllFiveObjects,orderDatesAndQuantities,movementAndLookBlocked,timeContinues,noEvidenceGranted,bothWindowCollisions,lampPersistence,originalShellUnchanged;
        public int width,height;public string verification;
    }
}
