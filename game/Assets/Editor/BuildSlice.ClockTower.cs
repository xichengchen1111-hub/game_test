using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using T = ClockTowerInteriorLayout;

public static partial class BuildSlice {
    const string TowerPrefabPath="Assets/Generated/ClockTower.prefab";
    static bool towerInteriorDevelopmentPreview;

    // Separate identifier for development collision/art review builds.
    public static void BuildClockTowerPreviewMac() {
        string previousId=PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        string previousCompany=PlayerSettings.companyName;
        towerInteriorDevelopmentPreview=true;
        try {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,"com.qingchuanstudio.wrongdatetest.towerreview");
            BuildFor(BuildTarget.StandaloneOSX,"../builds/clocktower-review/钟塔-内部验证.app",new[]{GraphicsDeviceType.Metal},1,"Qingchuan Tower Review");
        } finally {
            towerInteriorDevelopmentPreview=false;
            PlayerSettings.companyName=previousCompany;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone,previousId);
            AssetDatabase.SaveAssets();
        }
    }

    static void ClockTowerPrefab() {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ClockTowerAsset.ModelPath);
        if(!model&&!towerInteriorDevelopmentPreview)
            throw new InvalidOperationException("The approved local reference exterior must be built before delivery.");
        var root=Group("Town clock",Vector3.zero);
        WeatherAuthoring.Shelter(root,new Vector2(6,6),ClockTowerInstance.Height);
        var instance=root.AddComponent<ClockTowerInstance>();instance.referenceExteriorImported=model;
        if(model)ClockTowerAsset.Create(root.transform);
        TowerPhysicalShell(root.transform,!model);
        ClockTowerInterior.Build(root.transform);
        ClockTowerSurfaceChecks.Repair(root.transform);
        TowerDoor(root.transform);
        TowerWallPlaque(root.transform);
        for(int face=0;face<4;face++) {
            var mount=Group("Tower dial "+face,new Vector3(0,T.DialHeight,0),root.transform).transform;
            mount.localRotation=Quaternion.Euler(0,face*90,0);
            TowerDial(mount,new Vector3(0,0,-T.DialPlaneDistance),T.DialDiameter);
        }
        PrefabUtility.SaveAsPrefabAsset(root,TowerPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("CLOCKTOWER_PREFAB_READY: localReferenceExterior="+(model!=null)+" shared exterior/interior; four moving faces; continuous doorway");
    }

    static void TowerPhysicalShell(Transform root,bool preview) {
        float wallHeight=T.MachineFloor;
        float half=ClockTowerInstance.OuterWidth/2,thickness=(ClockTowerInstance.OuterWidth-ClockTowerInstance.InnerWidth)/2;
        var wall=Mat("TowerInnerLimewash","#C8C1AF",0,.12f);
        // Interior lining doubles as declared collision. No separate room scene.
        Box("Tower west inner wall",new Vector3(-half+thickness/2,wallHeight/2,0),new Vector3(thickness,wallHeight,3.6f),wall,root);
        Box("Tower east inner wall",new Vector3(half-thickness/2,wallHeight/2,0),new Vector3(thickness,wallHeight,3.6f),wall,root);
        Box("Tower north inner wall",new Vector3(0,wallHeight/2,half-thickness/2),new Vector3(3.6f,wallHeight,thickness),wall,root);
        float pier=(3.6f-ClockTowerInstance.DoorWidth)/2;
        foreach(int s in new[]{-1,1})
            Box("Tower entrance pier",new Vector3(s*(ClockTowerInstance.DoorWidth/2+pier/2),wallHeight/2,-half+thickness/2),new Vector3(pier,wallHeight,thickness),wall,root);
        float lintelBottom=ClockTowerInstance.DoorFloor+ClockTowerInstance.DoorHeight;
        Box("Tower entrance lintel",new Vector3(0,(wallHeight+lintelBottom)/2,-half+thickness/2),new Vector3(ClockTowerInstance.DoorWidth,wallHeight-lintelBottom,thickness),wall,root);
        ClockTowerGlazing.BuildShell(root,wall);
        Box("Tower roof underside",new Vector3(0,T.InnerWallHeight,0),new Vector3(T.ChamberInteriorWidth,.12f,T.ChamberInteriorWidth),wall,root);
        Box("Tower entrance threshold",new Vector3(0,.06f,-1.72f),new Vector3(1.10f,.12f,.70f),Mat("TowerThreshold","#AEA99A"),root);
        if(preview) {
            Roof(root,3.8f,3.8f,wallHeight,T.ExteriorHeight-wallHeight,Mat("TowerPreviewRoof","#59606B"));
            Debug.LogWarning("CLOCKTOWER_DEVELOPMENT_SHELL: placeholder for collision review; finished reference exterior is absent.");
        }
    }

    static void TowerDial(Transform parent,Vector3 position,float diameter) {
        var face=Group("Clock dial",position,parent).transform;
        var dial=face.gameObject.AddComponent<ClockDial>();dial.streetClock=true;
        dial.minuteHand=Group("Minute",new Vector3(0,0,-.028f),face).transform;
        dial.hourHand=Group("Hour",new Vector3(0,0,-.040f),face).transform;
        var ink=Mat("TowerDialInk","#303534",0,.28f);
        ClockTowerGlazing.BuildDial(face,diameter,ink);
        Box("Long minute hand",new Vector3(0,diameter*.17f,0),new Vector3(diameter*.023f,diameter*.40f,.025f),ink,dial.minuteHand,false).isStatic=false;
        Box("Short hour hand",new Vector3(0,diameter*.11f,0),new Vector3(diameter*.034f,diameter*.28f,.027f),ink,dial.hourHand,false).isStatic=false;
        var hub=Cylinder("Clock hand spindle",new Vector3(0,0,-.058f),new Vector3(.070f,.013f,.070f),ink,face);
        hub.transform.localRotation=Quaternion.Euler(90,0,0);
    }

    static void TowerDoor(Transform root) {
        float width=ClockTowerInstance.DoorWidth-.06f,height=ClockTowerInstance.DoorHeight-.04f;
        var hinge=Group("Clock tower door hinge",new Vector3(-width/2,ClockTowerInstance.DoorFloor,-1.80f),root).transform;
        var wood=Mat("TowerOakDoor","#63503E",0,.22f);var iron=Mat("TowerDoorIron","#424139",0,.4f);
        Box("Tower door oak leaf",new Vector3(width/2,height/2,0),new Vector3(width,height,.09f),wood,hinge,false);
        for(int i=0;i<6;i++)Box("Door plank seam",new Vector3(.09f+i*(width-.18f)/5,height/2,-.048f),new Vector3(.009f,height-.12f,.005f),iron,hinge,false);
        foreach(float h in new[]{.32f,height-.36f})Box("Forged strap hinge",new Vector3(width*.37f,h,-.059f),new Vector3(width*.68f,.045f,.019f),iron,hinge,false);
        foreach(int s in new[]{-1,1}) {
            Box("Brass pull plate",new Vector3(width-.13f,1.02f,s*.056f),new Vector3(.065f,.18f,.018f),Mat("TowerPlaqueBronze","#8D774D",0,.38f),hinge,false);
            Box("Door pull",new Vector3(width-.13f,1.04f,s*.091f),new Vector3(.025f,.115f,.038f),iron,hinge,false);
        }
        var leaf=hinge.gameObject.AddComponent<BoxCollider>();leaf.center=new Vector3(width/2,height/2,0);leaf.size=new Vector3(width,height,.09f);
        var door=hinge.gameObject.AddComponent<ShopMechanism>();door.fixtureId="clocktower-door";door.movingPart=hinge;door.openEuler=new Vector3(0,105,0);
        door.protectPlayer=true;door.leafCentre=leaf.center;door.leafSize=leaf.size;door.title="打开木门";
        foreach(var t in hinge.GetComponentsInChildren<Transform>())t.gameObject.isStatic=false;
    }

    static void TowerWallPlaque(Transform root) {
        var plaque=Box("Clock observation",new Vector3(1.06f,1.52f,-1.845f),new Vector3(.67f,.72f,.025f),Mat("TowerPlaqueBronze","#8D774D",0,.35f),root);
        Quad("Legible tower history",new Vector3(1.06f,1.52f,-1.862f),new Vector2(.63f,.68f),ImageMat("TowerHistory","TowerHistory.png",true),root);
        var clock=plaque.AddComponent<Interactable>();clock.title="古钟塔铭牌";clock.evidenceId="E06";clock.source="广场古钟塔，外墙铭牌与钟面";
        clock.body="青川古钟塔\n\n建成于 1816 年，由钟表大师埃利亚斯·霍夫曼主持建造。\n百余年钟声，守候小镇晨昏。\n\n钟楼由钟表铺的匠人维护。抬头可以看见钟面。";
    }

    static GameObject InstantiateClockTower() {
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(TowerPrefabPath));
        root.name="Town clock";root.transform.position=Vector3.zero;return root;
    }
}
