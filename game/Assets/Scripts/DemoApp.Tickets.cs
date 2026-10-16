using UnityEngine;

public partial class DemoApp {
    public Texture2D intactTicketImage,punchedTicketImage;
    Interactable carriedTicket;
    Texture2D TicketImage(string id)=>id=="E02"?intactTicketImage:id=="E03"?punchedTicketImage:null;
    // open=false only makes sure the carried ticket exists (the briefcase shows it).
    void InspectCarriedTicket(bool open=true) {
        if(!carriedTicket) {
            var item=new GameObject("Carried original ticket");item.transform.SetParent(transform,false);
            carriedTicket=item.AddComponent<Interactable>();carriedTicket.evidenceId="E02";
            carriedTicket.title="完整车票 · 随身";carriedTicket.source="主角随身车票";
            carriedTicket.body="一九三一年一月二十日的车票。\n\n纸面完整，票边也完整，没有检票器打出的孔。";
            carriedTicket.illustration=intactTicketImage;
        }
        if(!open)return;
        SetWatchOpen(false);Inspect(carriedTicket);
    }
    bool TickCarriedTicket() {
        if(!opening&&!paused&&!transitioning&&!inspected&&!backpack&&!DialogueOpen&&!MilkOrdersOpen&&Input.GetKeyDown(KeyCode.T)&&!HeldItemBlockedByDriving()&&state.belongingsCollected) {
            journal=false;InspectCarriedTicket();return true;
        }
        return false;
    }
}
