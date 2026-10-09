using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class DemoApp {
    void NeedShop(bool condition,string message) {
        if(condition)return;
        Debug.LogError("WALNUT_REVIEW_FAIL: "+message);Application.Quit(1);throw new InvalidOperationException(message);
    }
    ShopMechanism Mechanism(string id)=>FindObjectsByType<ShopMechanism>(FindObjectsSortMode.None).Single(m=>m.fixtureId==id);
    IEnumerator ReviewWalnutShop() {
        yield return new WaitForSeconds(.7f);
        var building=seamlessShop;var layout=building.layout;int buildingId=building.GetInstanceID();
        int sceneCount=SceneManager.sceneCount;
        Vector3 W(float x,float y,float z)=>layout.ToTown(new Vector3(x,y,z));
        NeedShop(CurrentScene=="Town"&&FindObjectsByType<ScenePortal>(FindObjectsSortMode.None).Length==0,"Shop still contains a loading portal");
        NeedShop(FindObjectsByType<Interactable>(FindObjectsSortMode.None).Count(i=>i.evidenceId=="E07")==1,"E07 duplicated or missing");
        yield return WalkTo(new Vector3(0,0,1));
        player.Aim(W(0,1.85f,-4));yield return Capture("unity-exterior");
        yield return WalkTo(W(0,0,-5.45f));
        var door=Mechanism("shop-front-door");
        if(door.IsOpen){door.Use();yield return new WaitForSeconds(1);}
        player.scriptedDirection=layout.TownRotation*Vector3.forward;yield return new WaitForSeconds(.75f);player.scriptedDirection=Vector3.zero;
        NeedShop(layout.ToInterior(player.transform.position).z<-4.20f,"Closed door does not block the actual controller");
        player.Aim(W(0,1.5f,-4));yield return null;
        bool used=TryInteract();
        Debug.Log("DOOR_OPEN_RAY: used="+used+" open="+door.IsOpen+" sweepBlocked="+door.LastUseBlocked+" position="+layout.ToInterior(player.transform.position));
        NeedShop(used&&door.IsOpen&&!Blocked,"Normal door ray failed to open physical door");
        yield return Capture("unity-door-opening");yield return new WaitForSeconds(.9f);
        NeedShop(!door.Moving&&fade==0,"Door is stuck or a black fade was used");
        yield return WalkTo(W(0,0,-2.15f));
        NeedShop(IsInsideShop&&building.GetInstanceID()==buildingId&&SceneManager.sceneCount==sceneCount,"Entry swapped scenes/building or failed containment");
        player.Aim(W(.3f,1.45f,2.7f));yield return Capture("unity-interior");
        int windowChecks=0;
        foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>().Where(m=>m.kind==OpeningKind.Window)) {
            float side=layout.Opening(marker.openingId).offset;
            yield return WalkTo(W(side*.72f,0,-2.15f));
            player.Aim(marker.transform.position+Vector3.up*.05f);yield return Capture("unity-"+marker.openingId+"-inside");
            var eye=player.eye.transform.position;var aperture=marker.transform.position;
            NeedShop(!Physics.Raycast(eye,(aperture-eye).normalized,Vector3.Distance(eye,aperture),Physics.DefaultRaycastLayers),"Opaque collider blocks front window: "+marker.openingId);
            var rotation=player.transform.rotation;float pitchBefore=player.pitch;
            var near=player.eye.WorldToViewportPoint(marker.transform.position);
            var far=player.eye.WorldToViewportPoint(GameObject.Find("Town clock").transform.position+Vector3.up*3);
            var parallaxStart=player.eye.transform.position;
            var lateral=player.eye.transform.right;lateral.y=0;lateral.Normalize();
            yield return WalkTo(player.transform.position+lateral*.40f,.45f);
            // Use a true lateral baseline from the reached position, avoiding
            // the old target tolerance introducing motion toward the window.
            yield return new WaitForEndOfFrame();
            NeedShop(Vector3.Distance(parallaxStart,player.eye.transform.position)>.20f,"Window check did not physically move sideways");
            var nearAfter=player.eye.WorldToViewportPoint(marker.transform.position);
            var farAfter=player.eye.WorldToViewportPoint(GameObject.Find("Town clock").transform.position+Vector3.up*3);
            Debug.Log("WINDOW_PARALLAX: "+marker.openingId+" travel="+Vector3.Distance(parallaxStart,player.eye.transform.position)+" delta="+Mathf.Abs((nearAfter.x-near.x)-(farAfter.x-far.x)));
            NeedShop(Mathf.Abs((nearAfter.x-near.x)-(farAfter.x-far.x))>.001f,"Window has no real near/far parallax");
            yield return Capture("unity-"+marker.openingId+"-shifted");
            NeedShop(Quaternion.Angle(rotation,player.transform.rotation)<.01f&&Mathf.Abs(pitchBefore-player.pitch)<.01f,"Parallax check changed view direction");
            windowChecks++;
        }
        yield return WalkTo(W(0,0,-1));yield return WalkTo(W(.65f,0,.8f));
        var owner=GameObject.Find("Clockmaker");var animator=owner.GetComponentInChildren<Animator>();
        player.Aim(owner.transform.position+Vector3.up*1.1f);yield return null;
        NeedShop(TryInteract()&&DialogueOpen,"Owner is not reachable through the new counter layout");
        yield return Capture("unity-owner-dialogue");CloseDialogue();
        NeedShop(animator.GetCurrentAnimatorClipInfo(0).Single().clip.name.Contains("Sit_Cross_Legged")&&!animator.applyRootMotion,"Locked owner animation changed");
        var originalRoot=owner.transform.position;float startLoop=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        player.Aim(owner.transform.position+Vector3.up*.85f);yield return Capture("unity-repair-area");
        yield return new WaitForSeconds(animator.GetCurrentAnimatorClipInfo(0)[0].clip.length+.2f);
        NeedShop(animator.GetCurrentAnimatorStateInfo(0).normalizedTime-startLoop>=1&&Vector3.Distance(originalRoot,owner.transform.position)<.001f,"Owner seated cycle not stable");
        var notice=GameObject.Find("RepairNotice");player.Aim(notice.transform.position);yield return null;
        NeedShop(TryInteract()&&inspected&&inspected.evidenceId=="E07","New clipboard cannot be read with ordinary ray");
        yield return Capture("unity-evidence");CloseInspect();
        journal=true;yield return Capture("unity-journal");journal=false;
        var drawer=Mechanism("shop-parts-drawer");
        if(drawer.IsOpen){drawer.Use();yield return new WaitForSeconds(.8f);}
        yield return WalkTo(W(.97f,0,.9f));
        player.Aim(W(.97f,.61f,1.39f));yield return null;
        NeedShop(TryInteract()&&drawer.IsOpen,"Parts drawer ray misses");yield return new WaitForSeconds(.8f);yield return Capture("unity-open-drawer");
        drawer.Use();yield return new WaitForSeconds(.8f);NeedShop(!drawer.IsOpen&&!drawer.Moving,"Drawer does not close");
        yield return WalkTo(W(0,0,.5f));yield return WalkTo(W(0,0,-2.3f));
        var vitrine=Mechanism("shop-vitrine-left");
        player.Aim(vitrine.GetComponentsInChildren<Renderer>().First().bounds.center);yield return null;
        NeedShop(TryInteract()&&vitrine.IsOpen,"Cabinet access door cannot be opened by ray");yield return new WaitForSeconds(.8f);yield return Capture("unity-open-vitrine");
        vitrine.Use();yield return new WaitForSeconds(.8f);
        yield return WalkTo(W(0,0,-4.02f));
        door.Use();NeedShop(door.IsOpen&&door.LastUseBlocked,"Closing door intersects a player in the doorway");
        yield return WalkTo(W(0,0,-5.5f));
        player.Aim(door.GetComponent<BoxCollider>().bounds.center);yield return null;
        NeedShop(TryInteract()&&!door.IsOpen,"Door cannot close from street side");yield return new WaitForSeconds(.9f);yield return Capture("unity-door-closed");
        player.Aim(W(0,1.5f,-4));yield return null;NeedShop(TryInteract()&&door.IsOpen,"Door cannot reopen");yield return new WaitForSeconds(.9f);
        yield return WalkTo(W(0,0,-2.15f));
        // Close from the room side using the swung leaf's real collider, then reopen.
        var leaf=door.GetComponent<BoxCollider>();player.Aim(leaf.bounds.center);yield return null;
        NeedShop(TryInteract()&&!door.IsOpen,"Door cannot close from the room side");yield return new WaitForSeconds(.9f);
        player.Aim(W(0,1.45f,-4));yield return null;NeedShop(TryInteract()&&door.IsOpen,"Door cannot reopen from room side");yield return new WaitForSeconds(.9f);
        yield return WalkTo(W(0,0,0));yield return WalkTo(W(0,0,2.8f));
        var rear=Mechanism("shop-rear-door");player.Aim(W(0,1.3f,4));yield return null;
        NeedShop(TryInteract()&&rear.IsOpen,"Service door cannot open");yield return new WaitForSeconds(.9f);
        yield return WalkTo(W(0,0,5.1f));NeedShop(!IsInsideShop,"Rear exit did not leave room");yield return Capture("unity-rear-exit");
        yield return WalkTo(W(0,0,2.8f));rear.Use();yield return new WaitForSeconds(.9f);
        yield return WalkTo(W(0,0,-2.4f));yield return WalkTo(W(0,0,-5.5f));
        foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>().Where(m=>m.kind==OpeningKind.Window)) {
            var opening=layout.Opening(marker.openingId);
            yield return WalkTo(W(opening.offset,0,-5.4f));player.Aim(W(opening.offset,1.5f,-2.5f));
            yield return Capture("unity-"+opening.id+"-outside");
        }
        yield return WalkTo(W(0,0,-5.5f));yield return WalkTo(W(0,0,-2.4f));
        drawer.Use();yield return new WaitForSeconds(.8f);
        NeedShop(SceneManager.sceneCount==sceneCount&&fade==0&&CurrentScene=="Town","Traversal caused a hidden scene/fade change");
        Save();yield return Capture("unity-saved-inside");
        File.WriteAllText(Path.Combine(captures,"walnut-results.json"),JsonUtility.ToJson(new WalnutResult{passed=true,continuousTown=true,doorBlocksClosed=true,
            doorBothSides=true,obstructedCloseRejected=true,frontAndRearWalked=true,transparentWindows=windowChecks,ownerSeatedLoop=true,
            evidenceRecorded=state.entries.Exists(e=>e.id=="E07"),drawerAndVitrine=true,saveScene=state.scene,sceneCount=sceneCount,
            verification="Visible native player, actual CharacterController and interaction rays. Controlled review movement/captures, not physical keyboard or blind playtest. No doorway fade/load/teleport."},true));
        Debug.Log("WALNUT_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyWalnutSave() {
        yield return new WaitForSeconds(1);
        NeedShop(CurrentScene=="Town"&&IsInsideShop&&state.entries.Exists(e=>e.id=="E07"),"Interior world position/evidence not restored in new process");
        NeedShop(Mechanism("shop-front-door").IsOpen&&Mechanism("shop-parts-drawer").IsOpen,"Fixture state not restored");
        NeedShop(!Mechanism("shop-parts-drawer").Moving,"Restored drawer should already be at saved position");
        yield return Capture("unity-reloaded-inside");
        File.WriteAllText(Path.Combine(captures,"walnut-save-relaunch.txt"),"PASS: independent process restored Town world-space interior position, E07, open door and open drawer.\n");
        Debug.Log("WALNUT_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    IEnumerator VerifyWalnutMigration() {
        yield return new WaitForSeconds(1);
        var expected=seamlessShop.layout.ToTown(new Vector3(-.4f,.03f,.3f));
        var position=player.transform.position;position.y=expected.y;
        NeedShop(CurrentScene=="Town"&&IsInsideShop&&Vector3.Distance(position,expected)<.02f,"Legacy local position was not mapped to world position");
        NeedShop(state.worldRevision==2&&state.entries.Any(e=>e.id=="E04")&&state.entries.Any(e=>e.id=="E07")&&state.entries.Count==2,"Legacy evidence was changed");
        NeedShop(state.milkboyGreetingRevision==2&&state.dialogueTopics.Contains("milkboy.delivery")&&state.remarkSeen,"Legacy dialogue/remark progress lost");
        Save();yield return Capture("unity-legacy-save-migrated");
        File.WriteAllText(Path.Combine(captures,"walnut-legacy-migration.txt"),"PASS: actual legacy ClockShop-local disk save loaded, mapped to Town and resaved; E04/E07, dialogue topic/revision and remark preserved.\n");
        Debug.Log("WALNUT_LEGACY_MIGRATION_PASS");Application.Quit();
    }
    [Serializable] class WalnutResult {
        public bool passed,continuousTown,doorBlocksClosed,doorBothSides,obstructedCloseRejected,frontAndRearWalked,ownerSeatedLoop,evidenceRecorded,drawerAndVitrine;
        public int transparentWindows,sceneCount;
        public string saveScene,verification;
    }
}
