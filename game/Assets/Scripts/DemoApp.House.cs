using System.Linq;
using UnityEngine;
public partial class DemoApp {
    public void OpenHomeTicket()=>InspectCarriedTicket();
    string houseCode="",houseFeedback="";
    Vector2 housePaperScroll;
    Interactable houseReturnContainer;
    int journalPage;
    void OpenHouseGuardDialogue(Interactable item){
        milkboyScript=DialogueScript.Load("HouseGuardDialogue");stationClerkPerformance=item.GetComponentInChildren<StationClerkPerformance>();
        dialogueHistory.Clear();dialogueHistoryOpen=false;player.scriptedDirection=Vector3.zero;VisitDialogue(milkboyScript.first);CursorMode();
    }
    void DrawHouseContainer(HouseContainer c){
        Box(new Rect(0,0,W,H),new Color(.015f,.03f,.04f,.6f));var p=new Rect(W/2-490,H/2-300,980,600);Panel(p);
        Label(new Rect(p.x+38,p.y+24,800,50),c.title,title);Label(new Rect(p.x+38,p.y+83,820,42),c.source,small);
        if(c.safe&&!c.Unlocked){
            Label(new Rect(p.x+40,p.y+152,870,70),"四位机械数字盘。转动数字后，试着拉动把手。",text);
            houseCode=GUI.TextField(new Rect(p.x+315,p.y+242,350,58),houseCode,4,title);houseCode=new string(houseCode.Where(char.IsDigit).ToArray());
            if(Button(new Rect(p.x+315,p.y+330,350,55),"拉动把手"))houseFeedback=c.TryCode(houseCode)?"锁舌松开了。":"把手没有转动。";
            Label(new Rect(p.x+40,p.y+425,870,42),houseFeedback,text);if(c.Unlocked)CloseInspect();
        }else CloseInspect();
        if(Button(new Rect(p.x+675,p.y+520,255,50),"离开 · Esc")){houseReturnContainer=null;CloseInspect();}
    }
    void DrawHousePaperBack(){
        if(inspected&&inspected.evidenceId=="E09"&&!houseLetterOpen&&(state.entries.Any(e=>e.id=="H06")||state.entries.Any(e=>e.id=="E01")))
            if(Button(new Rect(W*.58f+28,H-165,W*.38f-56,46),"比对残信照片"))houseLetterOpen=true;
    }
}
