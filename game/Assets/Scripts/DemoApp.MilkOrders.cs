using System.Collections.Generic;
using UnityEngine;

public partial class DemoApp {
    // Paper treatment and bounded navigation adapted after review of the agy draft.
    MilkOrderBook milkOrderBook;
    int milkOrderPage;
    GUIStyle milkTitle,milkText,milkSmall,milkButtonStyle,milkDate;
    public bool MilkOrdersOpen => milkOrderBook;
    public MilkDeliveryPage CurrentMilkOrder => milkOrderBook ? milkOrderBook.Pages[milkOrderPage] : null;
    public void OpenMilkOrders(MilkOrderBook book) {
        if(Blocked||!book)return;
        milkOrderBook=book;milkOrderPage=book.Pages.Count-1;
        for(int i=0;i<book.Pages.Count;i++)if(book.Pages[i].DateId==state.milkOrderDate)milkOrderPage=i;
        player.scriptedDirection=Vector3.zero;SetWatchOpen(false);focus=null;CursorMode();
    }
    public void CloseMilkOrders() {
        if(!MilkOrdersOpen)return;
        state.milkOrderDate=CurrentMilkOrder.DateId;
        milkOrderBook=null;player.scriptedDirection=Vector3.zero;CursorMode();Save();
    }
    void SelectMilkOrder(int index) {
        milkOrderPage=Mathf.Clamp(index,0,milkOrderBook.Pages.Count-1);
        state.milkOrderDate=CurrentMilkOrder.DateId;
    }
    public void MoveMilkOrderDay(int direction) { if(MilkOrdersOpen)SelectMilkOrder(milkOrderPage+direction); }
    void UpdateMilkOrderInput() {
        if(Input.GetKeyDown(KeyCode.Escape)){CloseMilkOrders();return;}
        if(Input.GetKeyDown(KeyCode.LeftArrow))MoveMilkOrderDay(-1);
        if(Input.GetKeyDown(KeyCode.RightArrow))MoveMilkOrderDay(1);
    }
    void MilkStyles() {
        if(milkText!=null)return;
        var ink=new Color(.22f,.18f,.13f);
        milkText=new GUIStyle(text){fontSize=27,wordWrap=false,alignment=TextAnchor.MiddleLeft,normal={textColor=ink}};
        milkSmall=new GUIStyle(milkText){fontSize=21};
        milkButtonStyle=new GUIStyle(milkSmall){alignment=TextAnchor.MiddleCenter};
        milkTitle=new GUIStyle(milkText){fontSize=42};
        milkDate=new GUIStyle(milkText){fontSize=32,alignment=TextAnchor.MiddleRight};
    }
    bool MilkButton(Rect rect,string label,bool selected=false,bool enabled=true) {
        bool hover=enabled&&rect.Contains(Event.current.mousePosition);
        Box(rect,selected?new Color(.75f,.69f,.53f):hover?new Color(.87f,.82f,.70f):new Color(.91f,.86f,.75f));
        Box(new Rect(rect.x,rect.y+rect.height-2,rect.width,2),new Color(.62f,.54f,.39f));
        var previous=GUI.enabled;GUI.enabled=previous&&enabled;
        bool pressed=GUI.Button(rect,label,milkButtonStyle);GUI.enabled=previous;return pressed;
    }
    void DrawMilkOrders() {
        MilkStyles();
        Box(new Rect(0,0,W,H),new Color(.015f,.025f,.035f,.66f));
        var p=new Rect((W-1140)/2,(H-660)/2,1140,660);
        Box(new Rect(p.x+9,p.y+12,p.width,p.height),new Color(0,0,0,.4f));
        Box(p,new Color(.23f,.28f,.22f));
        Box(new Rect(p.x+30,p.y+8,p.width-38,p.height-16),new Color(.94f,.90f,.79f));
        for(int i=0;i<5;i++)Box(new Rect(p.x+7,p.y+65+i*120,16,3),new Color(.63f,.54f,.36f));
        float x=p.x+65,y=p.y+28,width=1020;
        Label(new Rect(x,y,450,55),"送奶记录",milkTitle);
        Label(new Rect(x,y+55,480,33),"牛奶屋柜台 · 按送达日期归档",milkSmall);
        var page=CurrentMilkOrder;
        Label(new Rect(x+530,y+10,490,55),$"{page.year}年{page.month}月{page.day}日",milkDate);
        var months=new HashSet<string>();int tab=0;
        for(int i=0;i<milkOrderBook.Pages.Count;i++) {
            var other=milkOrderBook.Pages[i];string key=other.year+"-"+other.month;
            if(!months.Add(key))continue;
            if(MilkButton(new Rect(x+tab*150,y+104,136,40),other.month+"月",other.year==page.year&&other.month==page.month))SelectMilkOrder(i);
            tab++;
        }
        int dayTab=0;
        for(int i=0;i<milkOrderBook.Pages.Count;i++) {
            var other=milkOrderBook.Pages[i];if(other.year!=page.year||other.month!=page.month)continue;
            if(MilkButton(new Rect(x+width-214+dayTab*110,y+104,102,40),other.day+"日",i==milkOrderPage))SelectMilkOrder(i);
            dayTab++;
        }
        float top=y+166;float[] columns={200,420,180,220};string[] headers={"日期","送达店家","数量","确认"};
        Box(new Rect(x,top,width,44),new Color(.82f,.77f,.64f));
        float at=x;
        for(int i=0;i<4;i++){Label(new Rect(at+14,top,columns[i]-28,44),headers[i],milkSmall);at+=columns[i];}
        for(int row=0;row<5;row++) {
            float ry=top+44+row*60;
            if(row%2==0)Box(new Rect(x,ry,width,60),new Color(.96f,.93f,.85f));
            Box(new Rect(x,ry+59,width,1),new Color(.74f,.68f,.55f));
            float grid=x;for(int col=0;col<4;col++){Box(new Rect(grid,top,1,344),new Color(.74f,.68f,.55f));grid+=columns[col];}
            if(row>=page.deliveries.Count)continue;
            var record=page.deliveries[row];
            Label(new Rect(x+14,ry+5,180,50),$"{page.month}月{page.day}日",milkText);
            Label(new Rect(x+214,ry+5,390,50),record.shop,milkText);
            Label(new Rect(x+634,ry+5,160,50),record.bottles+"瓶",milkText);
            Label(new Rect(x+814,ry+5,190,50),record.delivered?"已送":"待送",milkText);
        }
        float footer=p.y+p.height-75;
        if(MilkButton(new Rect(x,footer,150,44),"‹ 上一日",false,milkOrderPage>0))MoveMilkOrderDay(-1);
        if(MilkButton(new Rect(x+166,footer,150,44),"下一日 ›",false,milkOrderPage<milkOrderBook.Pages.Count-1))MoveMilkOrderDay(1);
        Label(new Rect(x+340,footer,410,44),"← → 翻日期",milkSmall);
        if(MilkButton(new Rect(x+800,footer,220,44),"合上 · Esc"))CloseMilkOrders();
    }
}
