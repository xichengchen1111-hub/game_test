using UnityEngine;

public partial class DemoApp {
    public Texture2D intactTicketImage,punchedTicketImage;
    Interactable carriedTicket;
    Texture2D TicketImage(string id)=>id=="E02"?intactTicketImage:id=="E03"?punchedTicketImage:null;
    void InspectCarriedTicket() {
        if(!carriedTicket) {
            var item=new GameObject("Carried original ticket");item.transform.SetParent(transform,false);
            carriedTicket=item.AddComponent<Interactable>();carriedTicket.evidenceId="E02";
            carriedTicket.title="完整车票 · 随身";carriedTicket.source="主角随身车票";
            carriedTicket.body="一九三一年一月二十日的车票。\n\n纸面完整，票边也完整，没有检票器打出的孔。";
            carriedTicket.illustration=intactTicketImage;
        }
        SetWatchOpen(false);Inspect(carriedTicket);
    }
    bool TickCarriedTicket() {
        if(!opening&&!paused&&!transitioning&&!inspected&&!DialogueOpen&&!MilkOrdersOpen&&Input.GetKeyDown(KeyCode.T)&&!HeldItemBlockedByDriving()) {
            journal=false;InspectCarriedTicket();return true;
        }
        return false;
    }
    void DrawTicketOnJournal(NotebookEntry entry,Rect panel) {
        var image=TicketImage(entry.id);if(!image)return;
        Box(new Rect(panel.x+400,panel.y+376,640,152),new Color(.12f,.17f,.16f,.95f));
        GUI.color=Color.white;GUI.DrawTexture(new Rect(panel.x+416,panel.y+382,608,140),image,ScaleMode.ScaleToFit,true);
    }
}
