using UnityEngine;
public partial class DemoApp {
    bool creatorMenu,creatorReturnToPause;
    bool CreatorActionsAvailable=>sceneReady&&player&&!opening&&!transitioning&&!inspected&&!journal&&!backpack&&!DialogueOpen&&!MilkOrdersOpen&&!PoliceReportOpen&&!RailSeated&&!FurnitureSeated&&!CarSeated&&!OpeningRideActive;
    void OpenCreatorMenu() {
        if(creatorMenu||!CreatorActionsAvailable)return;
        creatorReturnToPause=paused;paused=true;creatorMenu=true;
        SetMapOpen(false);SetWatchOpen(false);CursorMode();
    }
    void CloseCreatorMenu() {
        if(!creatorMenu)return;
        creatorMenu=false;paused=creatorReturnToPause;creatorReturnToPause=false;CursorMode();
    }
    void ResumeAfterCreatorAction(){creatorMenu=false;creatorReturnToPause=false;paused=false;CursorMode();}
    bool UpdateCreatorInput() {
        // Consume submenu Escape here, before the ordinary pause toggle sees it.
        if(creatorMenu){if(Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.F2))CloseCreatorMenu();return true;}
        if(Input.GetKeyDown(KeyCode.F2)&&(!Blocked||paused)&&CreatorActionsAvailable){OpenCreatorMenu();return true;}
        if(!Blocked&&Input.GetKeyDown(KeyCode.F3))ToggleCreatorFlight();
        return false;
    }
    void ToggleCreatorFlight(){if(!CreatorActionsAvailable)return;player.SetCreatorFlying(!player.CreatorFlying);ResumeAfterCreatorAction();Save();}
    void CreatorTeleport(Vector3 point,float yaw=0) {
        if(!CreatorActionsAvailable)return;
        player.SetCreatorFlying(false);player.Place(point,yaw);ResumeAfterCreatorAction();Save();
    }
    void DrawCreator() {
        if(!opening&&!Blocked&&!OpeningRideActive&&player.CreatorFlying)Label(new Rect(36,H-85,950,32),"开发者飞行 · WASD / 视角移动 · 空格上升 · Ctrl 下降 · Shift 加速 · F3 返回起飞点",small);
        if(!creatorMenu)return;
        Box(new Rect(0,0,W,H),new Color(0,0,0,.42f));
        var p=new Rect(W/2-330,H/2-310,660,620);Panel(p);
        Label(new Rect(p.x+30,p.y+25,600,48),"开发者模式",title);
        Label(new Rect(p.x+30,p.y+82,600,58),"快速观察场景。关闭飞行会返回起飞点。\n飞行位置不会写入存档；传送只改变位置。",small);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&CreatorActionsAvailable;
        if(Button(new Rect(p.x+30,p.y+150,600,44),player.CreatorFlying?"关闭飞行并返回地面":"开启飞行（F3）"))ToggleCreatorFlight();
        if(CurrentScene=="Town"&&PoliceStation.Current) {
            if(Button(new Rect(p.x+30,p.y+207,285,44),"警署 · 牢房"))CreatorTeleport(OpeningPoint(20.1f,21.4f),-90);
            if(Button(new Rect(p.x+345,p.y+207,285,44),"警署 · 律师会见室"))CreatorTeleport(OpeningPoint(16,4.4f),180);
            if(Button(new Rect(p.x+30,p.y+261,285,44),"警署 · 门厅"))CreatorTeleport(OpeningPoint(11.5f,4.6f));
            if(Button(new Rect(p.x+345,p.y+261,285,44),"小镇广场"))CreatorTeleport(TownPlan.Spawn);
            if(seamlessShop&&Button(new Rect(p.x+30,p.y+315,285,44),"钟表铺门前"))CreatorTeleport(seamlessShop.layout.ToTown(new Vector3(0,.05f,-seamlessShop.layout.footprint.y/2-2)));
            if(milkShopRoot&&Button(new Rect(p.x+345,p.y+315,285,44),"牛奶店门前"))CreatorTeleport(milkShopRoot.TransformPoint(new Vector3(0,.1f,-4)));
            if(Button(new Rect(p.x+345,p.y+369,285,44),"车站门前"))CreatorTeleport(new Vector3(188,.12f,33));
            if(HouseInstance.Current&&Button(new Rect(p.x+30,p.y+369,285,44),"住宅门前"))CreatorTeleport(HouseInstance.Current.transform.TransformPoint(new Vector3(10,.16f,0)));
            if(HospitalBuilding.Current&&Button(new Rect(p.x+30,p.y+423,285,44),"医院门前"))CreatorTeleport(HospitalDropoff,0);
            if(HospitalBuilding.Current&&Button(new Rect(p.x+345,p.y+423,285,44),"医院 · 儿童病房"))CreatorTeleport(HospitalPoint(4,13.4f),0);
            if(RepairGarage.Current&&Button(new Rect(p.x+30,p.y+477,285,44),"修车铺门前"))CreatorTeleport(RepairGarage.Current.transform.TransformPoint(new Vector3(9.0f,.1f,-3.2f)),0);
            if(HotelInn.Current&&Button(new Rect(p.x+345,p.y+477,285,44),"旅馆门前"))CreatorTeleport(HotelInn.Current.Point(11.6f,.1f,37.5f),180);
        } else Label(new Rect(p.x+30,p.y+220,600,80),"当前场景可使用飞行观察。回到青川后可使用地点传送。",text);
        GUI.enabled=enabled;
        Label(new Rect(p.x+30,p.y+526,600,22),"第一幕需重新体验时，从独立“第一幕试玩”入口启动；现有调查存档保留。",small);
        if(Button(new Rect(p.x+30,p.y+548,600,44),creatorReturnToPause?"返回暂停菜单 · Esc":"返回游戏 · F2 / Esc"))CloseCreatorMenu();
    }
}
