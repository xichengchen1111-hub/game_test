using UnityEngine;
public partial class DemoApp {
 bool houseLetterOpen;
 Vector2[] letterPos={new Vector2(65,80),new Vector2(630,340)};
 int letterDragging=-1;
 Vector2 letterDragOffset;
 void DrawHouseLetter(){
  Box(new Rect(0,0,W,H),new Color(.015f,.025f,.03f,.65f));var p=new Rect(W/2-550,H/2-310,1100,620);Panel(p);
  Label(new Rect(p.x+36,p.y+22,900,45),"信纸对照",title);Label(new Rect(p.x+36,p.y+80,950,45),"将上下两片裁余纸移到照片旁，核对裁边与文字。",small);
  var start=new Vector2(p.x,p.y+100);Rect targetTop=new Rect(start.x+320,start.y+100,450,85),mid=new Rect(start.x+320,start.y+185,450,85),targetBottom=new Rect(start.x+320,start.y+270,450,100);
  PaperFragment(mid,"……再这样拖下去，我不会放过你……",false);
  string[] content={"修车铺那笔钱，\n你答应过要还。","……欠下的账。我会去找你把话说清楚。\n二十一日早班车的事，另信再谈。"};
  bool complete=state.openFixtures.Contains("house-letter-reassembled");
  if(complete){letterPos[0]=new Vector2(320,100);letterPos[1]=new Vector2(320,270);}
  for(int i=0;i<2;i++){
   Rect r=new Rect(start+letterPos[i],new Vector2(450,i==0?85:100));PaperFragment(r,content[i],true);
   if(!complete&&Event.current.type==EventType.MouseDown&&r.Contains(Event.current.mousePosition)){letterDragging=i;letterDragOffset=Event.current.mousePosition-(start+letterPos[i]);Event.current.Use();}
  }
  if(letterDragging>=0&&Event.current.type==EventType.MouseDrag){letterPos[letterDragging]=Event.current.mousePosition-start-letterDragOffset;letterPos[letterDragging]=new Vector2(Mathf.Clamp(letterPos[letterDragging].x,20,630),Mathf.Clamp(letterPos[letterDragging].y,0,380));Event.current.Use();}
  if(Event.current.type==EventType.MouseUp){letterDragging=-1;CheckHouseLetter();}
  if(complete)Label(new Rect(p.x+35,p.y+480,1010,45),"裁边与文字接续吻合。完整文字已记入调查簿。",small);
  if(Button(new Rect(p.x+820,p.y+544,240,48),"返回材料"))houseLetterOpen=false;
 }
 void PaperFragment(Rect r,string value,bool moving){Box(r,new Color(.78f,.73f,.60f));var ink=new GUIStyle(small){normal={textColor=new Color(.18f,.19f,.15f)}};GUI.Label(new Rect(r.x+14,r.y+14,r.width-28,r.height-20),value,ink);}
 void CheckHouseLetter(){
  if(Vector2.Distance(letterPos[0],new Vector2(320,100))>25||Vector2.Distance(letterPos[1],new Vector2(320,270))>25)return;
  if(!state.openFixtures.Contains("house-letter-reassembled"))state.openFixtures.Add("house-letter-reassembled");
  if(!state.entries.Exists(e=>e.id=="H27"))state.entries.Add(new NotebookEntry{id="H27",title="拼合后的信文",source="住宅裁余片与现场残信照片的对照",body="修车铺那笔钱，你答应过要还。再这样拖下去，我不会放过你欠下的账。我会去找你把话说清楚。二十一日早班车的事，另信再谈。\n\n上下两片裁余纸与照片中的纸片接续。"});Save();
 }
}
