using UnityEngine;

// Second act: the hospital. The player may ask the hall porter for the ward, or simply walk
// in and find the bed; neither the porter nor the ward nurse is a gate. Nothing here grants
// evidence or settles the case: the visit gives the investigation its reason.
public partial class DemoApp {
    DialogueScript hospitalScript;
    HospitalActor hospitalSpeaker;
    // 0 arrived, 1 knows the ward, 2 has met the child, 3 has given the child a promise.
    public const int HospitalPromised=3;
    void InitializeHospitalState() {
        if(state.hospitalRevision==0){state.hospitalRevision=1;state.hospitalStage=0;}
    }
    public Vector3 HospitalPoint(float x,float z)=>HospitalBuilding.Current.Point(x,z);
    public void OpenHospitalActor(HospitalActor actor) {
        if(Blocked||OpeningRideActive)return;
        // The first meeting with the child is played as a cinematic (DemoApp.HospitalCinematic.cs).
        bool cinematic=actor.role=="child"&&state.hospitalStage<HospitalPromised&&actor.GetComponent<HospitalBedPose>();
        hospitalScript=DialogueScript.Load(actor.role=="porter"?"HospitalPorterDialogue":actor.role=="child"?"HospitalChildDialogue":"HospitalSisterDialogue");
        milkboyScript=hospitalScript;hospitalSpeaker=actor;BindDialoguePerformance(actor);
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;
        bool promised=state.hospitalStage>=HospitalPromised;
        string node=actor.role=="child"?(promised?"revisit":"greeting"):
            promised?"after":actor.role=="porter"?(state.hospitalStage>=1?"again":"greeting"):(state.dialogueTopics.Exists(t=>t.StartsWith("hospital.sister."))?"again":"greeting");
        if(cinematic)BeginChildCinematic(actor);
        // On later visits she is glad to see him.
        else if(actor.role=="child"){var expression=actor.GetComponentInChildren<PeriodCharacterExpression>();if(expression)expression.SetMood(promised?.5f:.3f,0);}
        VisitDialogue(node);CursorMode();Debug.Log("DIALOGUE_OPEN: hospital-"+actor.role+(cinematic?" (cinematic)":""));
    }
    // Progress follows what the player has actually heard, including a conversation closed early.
    void HospitalDialogueVisited() {
        if(hospitalScript==null||milkboyScript!=hospitalScript||!hospitalSpeaker)return;
        string id=dialogueNode.id;
        if(hospitalSpeaker.role=="porter"&&(id=="ward"||id=="again"))state.hospitalStage=Mathf.Max(1,state.hospitalStage);
        if(hospitalSpeaker.role=="child") {
            state.hospitalStage=Mathf.Max(2,state.hospitalStage);
            if(id=="promise"||id=="promise-truth")state.hospitalStage=HospitalPromised;
        }
    }
    void HospitalDialogueClosed() {
        if(hospitalScript==null||milkboyScript!=hospitalScript||!hospitalSpeaker)return;
        if(hospitalSpeaker.role=="child"&&state.hospitalStage>=HospitalPromised&&!state.dialogueTopics.Contains("hospital.promise.noted")) {
            state.dialogueTopics.Add("hospital.promise.noted");
            remark="你答应了孩子：查清是谁害了她的母亲。";remarkUntil=Time.time+9;
        }
        hospitalSpeaker=null;
    }
    bool NearHospital() {
        if(!HospitalBuilding.Current||CurrentScene!="Town")return false;
        var p=HospitalBuilding.Current.transform.InverseTransformPoint(player.transform.position);
        return p.x>-6&&p.x<48&&p.z>-9&&p.z<36;
    }
    // Shown only on the hospital grounds, so it never nags an investigation elsewhere in town.
    bool HospitalObjectiveShown=>state.hospitalStage<HospitalPromised&&state.openingRideStage>=5&&NearHospital();
    bool DrawHospitalObjective() {
        if(!HospitalObjectiveShown||ChildCinematicActive)return false;
        string[] goals={"探望孩子 · 走进医院，可以向门厅问讯处的门房打听孩子在哪间病房。","探望孩子 · 儿童病房在西翼：沿走廊向左走到头，靠院子一侧第三张床。","陪孩子说说话。靠近病床按 E 交谈。"};
        Label(new Rect(36,102,820,65),goals[Mathf.Clamp(state.hospitalStage,0,2)],small);
        return true;
    }
}
