using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
// Native review of the hospital's main line: arrive at the door, walk in, ask the porter,
// walk the corridor to the children's ward, speak with the ward nurse and the child, then
// reload in a second process. Real controller, colliders, door leaves and the ordinary E ray;
// isolated save, always muted. Scripted inputs are not physical keyboard or blind play.
public partial class DemoApp {
    void HNeed(bool ok,string why){if(!ok)throw new Exception("HOSPITAL_REVIEW_FAIL: "+why);Debug.Log("HOSPITAL_CHECK: "+why);}
    Vector3 HP(float x,float z)=>HospitalBuilding.Current.Point(x,z);
    Vector3 HW(float x,float y,float z)=>HospitalBuilding.Current.transform.TransformPoint(new Vector3(x,y,z));
    string HRoom=>HospitalBuilding.Current.RoomAt(player.transform.position,out var room)?room.id:"outside";
    HospitalActor HPerson(string role)=>HospitalBuilding.Current.GetComponentsInChildren<HospitalActor>().Single(a=>a.role==role);
    IEnumerator HUse(HospitalActor actor,Vector3 aim) {
        player.Aim(aim);yield return null;yield return null;Physics.SyncTransforms();
        HNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)&&hit.collider.GetComponentInParent<Interactable>()==actor,"the ordinary E ray reaches "+actor.title+" (hit "+(hit.collider?hit.collider.name:"nothing")+")");
        HNeed(TryInteract()&&DialogueOpen,"E opens the conversation with "+actor.title);yield return null;
    }
    // Reads a hospital record with the ordinary E ray; it must enter the notebook.
    IEnumerator HRead(string id,Vector3 aim,string capture,bool turnOver=false) {
        player.Aim(aim);yield return null;yield return null;Physics.SyncTransforms();
        HNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)&&hit.collider.GetComponentInParent<Interactable>() is Interactable item&&item.evidenceId==id,"the E ray reaches record "+id+" (hit "+(hit.collider?hit.collider.name:"nothing")+")");
        HNeed(TryInteract()&&inspected&&inspected.evidenceId==id,"E opens record "+id);
        yield return new WaitForSeconds(.9f);yield return Capture(capture);
        if(turnOver){evidenceTargetRotation=evidenceRestRotation*Quaternion.Euler(0,180,0);yield return new WaitForSeconds(1.2f);yield return Capture(capture+"-back");}
        CloseInspect();yield return null;
        HNeed(state.entries.Any(e=>e.id==id),"record "+id+" is in the notebook");
    }
    // Follows a conversation by the option texts a player would pick; fails on an unexpected node.
    IEnumerator HTalk(string closingNode,params int[] picks) {
        int pick=0,guard=0;
        while(DialogueOpen&&guard++<80) {
            yield return new WaitForSeconds(.25f);
            string id=dialogueNode.id;
            if(dialogueChoices.Count>0) {
                HNeed(pick<picks.Length,"a planned choice remains for the options at "+id);
                HNeed(picks[pick]<dialogueChoices.Count,"choice "+picks[pick]+" exists at "+id);
                SelectDialogue(picks[pick++]);
            } else {
                AdvanceDialogue();
                if(!DialogueOpen)HNeed(id==closingNode,"conversation closes on "+closingNode+" (was "+id+")");
            }
        }
        HNeed(!DialogueOpen&&pick==picks.Length,"conversation finished with every planned choice used");
    }
    IEnumerator ReviewHospital() {
        yield return new WaitForSeconds(1.2f);
        HNeed(HospitalBuilding.Current,"hospital present in Town");
        HNeed(AudioListener.volume==0,"review runs muted");
        HNeed(state.hospitalStage==0&&state.openingRideStage==5,"fresh review starts after the lift, before the visit");
        var L=HospitalBuilding.Current.Layout;
        // The visit happens late at night (a new game opens at 21:00; user, 2026-10-14).
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);DayReviewTime(22*60+40);
        // Where the lawyer sets the player down.
        player.Place(HospitalDropoff,0);player.Aim(HW(21,3.2f,0));
        yield return new WaitForSeconds(.8f);yield return Capture("hospital-01-arrival");
        HNeed(HospitalObjectiveShown,"the drop-off lies on the hospital grounds, so the visit hint shows");
        player.Place(new Vector3(HospitalDropoff.x-14,.12f,HospitalDropoff.z-5),0);player.Aim(HW(24,3f,0));
        yield return new WaitForSeconds(.5f);yield return Capture("hospital-02-front");
        DayReviewTime(21*60);yield return new WaitForSeconds(.8f);yield return Capture("hospital-03-front-night");
        DayReviewTime(22*60+40);
        // Walk in through the open leaf of the street door.
        player.Place(HospitalDropoff,0);yield return null;
        var left=HospitalBuilding.Current.Door("hospital-front-door-left");var right=HospitalBuilding.Current.Door("hospital-front-door-right");
        HNeed(left&&right&&left.IsOpen&&!right.IsOpen,"street door stands with its left leaf open");
        yield return WalkTo(HP(20.62f,-1.6f));yield return WalkTo(HP(20.62f,1.7f));yield return WalkTo(HP(21.2f,3.2f));
        HNeed(HRoom=="hall","walked into the entrance hall (in "+HRoom+")");
        player.Aim(HW(23.6f,1.3f,5.2f));yield return new WaitForSeconds(.5f);yield return Capture("hospital-04-hall");
        // Ask at the enquiry desk.
        var porter=HPerson("porter");
        yield return WalkTo(HP(22.2f,4.2f));
        yield return HUse(porter,porter.transform.position+Vector3.up*1.45f);
        HNeed(dialogueNode.id=="greeting","the porter greets a first-time visitor");
        yield return new WaitForSeconds(.4f);yield return Capture("hospital-05-porter");
        // ward, then whether the child knows, then visiting hours, then leave
        yield return HTalk("bye",0,1,2,3);
        HNeed(state.hospitalStage==1,"the porter's answer is remembered as knowing the ward");
        HNeed(state.dialogueTopics.Contains("hospital.porter.ward")&&state.dialogueTopics.Contains("hospital.porter.knows"),"asked topics recorded");
        // The visiting register lies open on the enquiry desk.
        float g=L.levels.ground;
        yield return HRead("Y01",HW(23.35f,g+1.06f,3.5f),"hospital-05b-register");
        // Down the corridor to the west end and through the ward doors.
        yield return WalkTo(HP(21.2f,6.6f));yield return WalkTo(HP(21f,10.1f));
        HNeed(HRoom=="corridor","reached the corridor through the hall arch (in "+HRoom+")");
        player.Aim(HW(2f,1.5f,10.1f));yield return new WaitForSeconds(.5f);yield return Capture("hospital-06-corridor");
        yield return WalkTo(HP(4f,10.1f));
        var wardLeft=HospitalBuilding.Current.Door("hospital-children-door-left");var wardRight=HospitalBuilding.Current.Door("hospital-children-door-right");
        HNeed(wardLeft&&wardRight&&wardLeft.IsOpen&&wardRight.IsOpen,"ward doors stand open by day");
        yield return WalkTo(HP(4f,13.3f));
        HNeed(HRoom=="children","entered the children's ward (in "+HRoom+")");
        player.Aim(HW(4.4f,1.2f,24f));yield return new WaitForSeconds(.5f);yield return Capture("hospital-07-ward");
        // The ward nurse is optional company, not a gate: speak to her on the way.
        var sister=HPerson("sister");
        yield return WalkTo(HP(2.9f,15.2f));yield return WalkTo(HP(2.9f,20.6f));yield return WalkTo(HP(4f,21.2f));
        yield return HUse(sister,sister.transform.position+Vector3.up*1.4f);
        yield return new WaitForSeconds(.4f);yield return Capture("hospital-08-sister");
        // the accident; when the mother last came (her testimony is recorded); how to speak to the child; leave
        yield return HTalk("bye",1,3,4,5);
        HNeed(state.hospitalStage==1,"the nurse changes no progress");
        HNeed(state.entries.Any(e=>e.id=="Y04"),"asking when the mother last came records the nurse's testimony");
        int evidence=state.entries.Count;
        // The child: third bed on the court side.
        var child=HPerson("child");var bed=L.furniture.Single(f=>f.kind=="wardbed"&&f.extra=="child");
        yield return WalkTo(HP(4.6f,20.4f));yield return WalkTo(HP(5.35f,bed.z));
        player.Aim(child.transform.position+Vector3.up*.45f);yield return new WaitForSeconds(.6f);yield return Capture("hospital-09-child-from-foot");
        yield return HUse(child,child.transform.position+Vector3.up*.42f);
        HNeed(dialogueNode.id=="greeting"&&dialogueNode.speaker=="孩子","the child speaks first");
        HNeed(ChildCinematicActive&&!player.enabled,"the first meeting plays as a cinematic");
        yield return new WaitForSeconds(2.4f);yield return Capture("hospital-10-child-dialogue");
        // late; someone harmed her; an honest answer about coming back; the promise; the drawing; goodbye
        int promiseSeen=0;
        int[] picks={0,0,1,0,1,4};int pick=0,guard=0;bool card=false,talked=false;
        var shots=new System.Collections.Generic.HashSet<string>();
        // In bed she speaks without the talking gestures, so her hands stay on the covers.
        var childBones=child.GetComponentsInChildren<Transform>(true);var childAnimator=child.GetComponentInChildren<Animator>();
        Transform handL=childBones.FirstOrDefault(t=>t.name=="Left_Hand"),handR=childBones.FirstOrDefault(t=>t.name=="Right_Hand"),headBone=childBones.FirstOrDefault(t=>t.name=="Head");
        HNeed(handL&&handR&&headBone&&childAnimator,"the child's hands, head and animator are found");
        float nearestHand=float.MaxValue;bool gestured=false,choicesClear=true;int choiceChecks=0;
        void WatchChild(){
            if(!ChildCinematicActive)return;
            if(childAnimator.GetCurrentAnimatorStateInfo(0).IsName("Talk"))gestured=true;
            nearestHand=Mathf.Min(nearestHand,Mathf.Min(Vector3.Distance(handL.position,headBone.position),Vector3.Distance(handR.position,headBone.position)));
            if(DialogueOpen&&dialogueChoices.Count>0&&ChildCinematicChoicesShown){choiceChecks++;for(int i=0;i<dialogueChoices.Count;i++)if(ChildCinematicChoiceRect(i,dialogueChoices.Count).yMin<ChildCinematicPictureBottom-.5f)choicesClear=false;}
        }
        while((DialogueOpen||ChildCinematicActive)&&guard++<160) {
            yield return new WaitForSeconds(.25f);
            WatchChild();
            if(!DialogueOpen){if(cinematicCard!=null&&cinematicCardAlpha>.9f&&!card){card=true;yield return Capture("hospital-11c-cinematic-card");}continue;}
            string id=dialogueNode.id;
            if(ChildCinematicActive&&ChildCinematicShot!=null)shots.Add(ChildCinematicShot);
            if(ChildCinematicActive&&!talked&&id=="waited"){talked=true;var mouth=child.GetComponentInChildren<PeriodCharacterMouthMotion>();float widest=0;
                for(int f=0;f<45;f++){if(mouth)widest=Mathf.Max(widest,Quaternion.Angle(mouth.jaw.localRotation,Quaternion.identity));WatchChild();yield return null;}
                HNeed(mouth&&widest>1.2f,"the child's mouth opens while she speaks (widest "+widest.ToString("F1")+" degrees; hers opens less, she is weak)");yield return Capture("hospital-10a-cinematic-father");}
            if(id=="mother-apples"){yield return new WaitForSeconds(2f);yield return Capture("hospital-10b-cinematic-apples");}
            if(id=="ask-find"&&promiseSeen==0){promiseSeen=1;yield return new WaitForSeconds(3.5f);yield return Capture("hospital-11-child-asks");}
            if(id=="resolve"){yield return new WaitForSeconds(4.2f);yield return Capture("hospital-11b-cinematic-resolve");}
            if(dialogueChoices.Count>0){HNeed(pick<picks.Length&&picks[pick]<dialogueChoices.Count,"planned choice exists at "+id);SelectDialogue(picks[pick++]);}
            else {AdvanceDialogue();if(!DialogueOpen)HNeed(id=="leave-apple","the visit ends on the child's last line (was "+id+")");}
        }
        HNeed(!DialogueOpen&&pick==picks.Length,"the visit ran to its end");
        HNeed(card&&!ChildCinematicActive&&player.enabled&&shots.SetEquals(new[]{"wide","father","close","apples","push","window"}),"the cinematic ran its shots ("+string.Join(",",shots)+"), showed its card and gave the view back");
        HNeed(!gestured,"in the cinematic the child speaks without the talking gestures");
        HNeed(nearestHand>.2f,"the child's hands stay away from her face (nearest "+(nearestHand*100).ToString("F0")+" cm from the head)");
        HNeed(choiceChecks>0&&choicesClear,"the cinematic choices sit in the bottom bar, below the picture ("+choiceChecks+" checks)");
        HNeed(state.hospitalStage==HospitalPromised,"the promise is recorded");
        HNeed(state.dialogueTopics.Contains("hospital.promise.noted")&&remark.Contains("答应")&&Time.time<remarkUntil,"the promise is shown to the player after the visit");
        HNeed(state.entries.Count==evidence,"the visit grants no evidence");
        yield return new WaitForSeconds(.5f);yield return Capture("hospital-12-after-promise");
        // Beside the bed: the child turns her head to a visitor at her side.
        yield return WalkTo(HP(5.35f,bed.z+1.05f));yield return WalkTo(HP(6.95f,bed.z+.95f));
        player.Aim(child.transform.position+Vector3.up*.5f);yield return new WaitForSeconds(1.6f);yield return Capture("hospital-13-child-bedside");
        // Her drawing on the overbed table: turned over, the date is on the back.
        yield return HRead("Y03",HW(6.38f,g+.99f,bed.z+.08f),"hospital-13b-drawing",true);
        // A second visit opens on the promise.
        yield return HUse(child,child.transform.position+Vector3.up*.42f);
        HNeed(dialogueNode.id=="revisit","a second visit asks after the promise");
        yield return HTalk("leave-apple",0,4);
        // Evening lamps in the ward and the general ward for the record.
        DayReviewTime(20*60+30);yield return WalkTo(HP(4f,bed.z+.95f));player.Aim(HW(4.4f,1.3f,13f));
        yield return new WaitForSeconds(.8f);yield return Capture("hospital-14-ward-evening");
        DayReviewTime(22*60+40);
        // The rest of the hospital's people, each seen from a little in front of where they face.
        var people=HospitalBuilding.Current.GetComponentsInChildren<HospitalPerson>();
        HNeed(people.Length==L.items.Count(i=>i.kind=="person"&&people.Any(p=>p.personId==i.id)),"every placed hospital person has a contract spot ("+people.Length+")");
        int shot=20;
        foreach(var person in people.OrderBy(p=>p.personId)) {
            var spot=L.Item(person.personId);var face=new Vector3(Mathf.Sin(spot.yaw*Mathf.Deg2Rad),0,Mathf.Cos(spot.yaw*Mathf.Deg2Rad));
            var at=new Vector3(spot.x,0,spot.z)+face*2.2f;player.Place(HP(at.x,at.z),spot.yaw+180);
            float eye=spot.target=="stand"?1.4f:spot.target=="sit"?.95f:spot.target=="lying"?.2f:.45f;
            player.Aim(person.transform.position+Vector3.up*eye);yield return new WaitForSeconds(.8f);
            yield return Capture("hospital-"+shot+++"-"+person.personId);
        }
        // Passing the time of day uses the ordinary E ray and changes nothing.
        var talker=people.FirstOrDefault(p=>p.personId=="visitor-man");
        if(talker) {
            int stage=state.hospitalStage,entries=state.entries.Count;
            player.Place(HP(20.4f,5.9f),90);player.Aim(talker.transform.position+Vector3.up*1.4f);yield return null;yield return null;Physics.SyncTransforms();
            HNeed(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)&&hit.collider.GetComponentInParent<Interactable>()==talker,"the E ray reaches "+talker.title);
            HNeed(TryInteract()&&!DialogueOpen&&state.hospitalStage==stage&&state.entries.Count==entries,"small talk shows a line and changes no progress");
            yield return new WaitForSeconds(.4f);yield return Capture("hospital-"+shot+"-small-talk");
        }
        // The receipt stub book inside the pay hatch, reached through the office door off the corridor.
        player.Place(HP(27.5f,10.2f),180);yield return null;
        var office=HospitalBuilding.Current.Door("hospital-office-door");HNeed(office&&!office.IsOpen,"the office door starts closed");
        player.Aim(HW(27.5f,g+1.2f,8.5f));yield return null;Physics.SyncTransforms();HNeed(TryInteract()&&office.IsOpen,"the office door opens with E");
        yield return new WaitForSeconds(1.2f);yield return WalkTo(HP(27.5f,7.4f));yield return WalkTo(HP(26.1f,4.55f));
        HNeed(HRoom=="office","walked into the office (in "+HRoom+")");
        yield return HRead("Y02",HW(25.3f,g+1.05f,4.55f),"hospital-16-receipts");
        // Back at the bedside before saving: the second process expects the children's ward.
        player.Place(HP(4f,bed.z+.95f),90);yield return null;
        HNeed(AudioListener.volume==0,"review still muted");
        Save();
        File.WriteAllText(Path.Combine(captures,"acceptance.json"),"{\"passed\":true,\"nativeControllerAndRays\":true,\"physicalInputs\":false,\"muted\":true,\"hospitalStage\":"+state.hospitalStage+",\"childModel\":"+(child.GetComponentInChildren<SkinnedMeshRenderer>()?"true":"false")+",\"porterStandIn\":"+(porter.name.Contains("临时")?"true":"false")+",\"sisterStandIn\":"+(sister.name.Contains("临时")?"true":"false")+",\"otherPeople\":"+people.Length+"}");
        Debug.Log("HOSPITAL_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifyHospitalSave() {
        yield return new WaitForSeconds(1.2f);
        HNeed(HospitalBuilding.Current&&AudioListener.volume==0,"hospital present, review muted");
        HNeed(state.hospitalStage==HospitalPromised,"the promise persists in a second process");
        HNeed(HRoom=="children","position restored inside the children's ward (in "+HRoom+")");
        HNeed(state.dialogueTopics.Contains("hospital.child.draw")&&state.dialogueTopics.Contains("hospital.porter.ward"),"asked topics persist");
        opening=false;var child=HPerson("child");
        player.Aim(child.transform.position+Vector3.up*.45f);yield return new WaitForSeconds(.6f);
        HNeed(!HospitalObjectiveShown,"no visit hint once the promise is given");
        yield return Capture("hospital-15-reload");
        var porter=HPerson("porter");
        player.Place(HP(22.2f,4.2f),90);yield return null;
        yield return HUse(porter,porter.transform.position+Vector3.up*1.45f);
        HNeed(dialogueNode.id=="after","the porter speaks of the visit afterwards");
        yield return HTalk("after-child");
        Debug.Log("HOSPITAL_SAVE_PASS");Application.Quit();
    }
}
