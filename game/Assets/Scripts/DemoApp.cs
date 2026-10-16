using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable] public class NotebookEntry { public string id,title,body,source; }
[Serializable] public class SliceSave {
    public string scene="Town";
    public float x=0,y=0.12f,z=-10,yaw=0;
    public float returnX=3.7f,returnY=0.05f,returnZ=1;
    public List<NotebookEntry> entries = new List<NotebookEntry>();
    public int openingRevision, openingStage;
    // Revision 2 opening: 0 waking in the cell (cinematic), 1 following the guard to the property room,
    // 2 belongings on the table, 3 collected and following the guard to the lawyer, 4 delivered.
    public int openingEscort;
    // Watch, map, ticket and notebook are returned with the briefcase.
    public bool belongingsCollected;
    public int openingRideRevision, openingRideStage;
    public int hospitalRevision, hospitalStage;
    public bool remarkSeen;
    public bool milkboyGreeted;
    public int milkboyGreetingRevision;
    public List<string> dialogueTopics = new List<string>();
    public List<string> openFixtures = new List<string>();
    public List<string> initializedFixtures = new List<string>();
    // Stage report (police desk / officer / journal): player-composed submissions and supported rule IDs.
    public List<string> reportSubmissions = new List<string>();
    public List<string> reportSupported = new List<string>();
    public bool reportReinvestigation;
    public int worldRevision;
    public int gameClockRevision;
    public int towerLayoutRevision;
    public int towerAdjustmentMinutes;
    public double elapsedGameMinutes;
    public string milkOrderDate;
    public bool hasMapWaypoint;
    public float mapWaypointX,mapWaypointZ;
    public bool railSeated;
    public string furnitureSeatId;
    public int railJourneys;
    public bool carSaved;
    public float carX,carY,carZ,carYaw;
    public string carSeatId;
    public double carWarmUntil;
}

public partial class DemoApp : MonoBehaviour {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [System.Runtime.InteropServices.DllImport("kernel32.dll",SetLastError=true)]
    static extern bool TerminateProcess(IntPtr process,uint exitCode);
#endif
    public static DemoApp Instance;
    public PlayerMotor player;
    public Font font;
    public Texture2D dialTexture;
    public UnityEngine.Rendering.Universal.ScriptableRendererFeature reviewAmbientOcclusion;
    public SliceSave state = new SliceSave();
    public bool Review { get; private set; }
    public float Minutes => (float)gameClock.MinutesOfDay;
    public bool NPCAnimationPaused => opening || transitioning || paused;
    public bool Blocked => creatorMenu || opening || transitioning || inspected || journal || backpack || paused || DialogueOpen || MilkOrdersOpen || PoliceReportOpen || ChildCinematicActive || cellCinematic;
    public string CurrentScene { get; private set; } = "Town";
    public bool IsInsideShop { get; private set; }
    BuildingInstance seamlessShop;
    Transform milkShopRoot;
    bool insideMilkShop;
    public bool Silent;
    bool opening=true, transitioning, journal, paused, watch;
    bool quitting,reviewPortrait;
    bool sceneReady;
    bool towerSaveMigrated;
    Vector3 towerMigratedPosition;
    float fade=1, locationAge, remarkUntil, remarkStart;
    string location="青川 · 小镇广场", remark="";
    Interactable inspected, focus;
    string savePath, captures;
    GUIStyle text, small, title, button, large, italic;
    Texture2D solid, circle;
    float W,H;
    float watchRenderedMinutes;
    int selectedEntry;
    bool recordWalk,recordingWalk,reviewWindow,recordingWindow,previewWindow,previewStreet,previewMilkboy,previewWatchmaker,reviewLighting,recordingLighting,lightingRight;
    int motionFrames;
    int windowFrames;
    int exteriorWindowCount;
    int lightingFrames;
    float lightingStart,lightingYaw,lightingPitch;
    List<LightingSample> lightingSamples=new List<LightingSample>();

    void Awake() {
        if (Instance) { Destroy(gameObject); return; }
        Instance=this; DontDestroyOnLoad(gameObject);
        Application.wantsToQuit+=WantsToQuit;
        Application.targetFrameRate=60;
        var args=Environment.GetCommandLineArgs();
        Review=Array.IndexOf(args,"--review")>=0;
        InitializeAudio();
        if(Review) {
            int widthIndex=Array.IndexOf(args,"-screen-width"),heightIndex=Array.IndexOf(args,"-screen-height");
            if(widthIndex>=0&&heightIndex>=0&&widthIndex+1<args.Length&&heightIndex+1<args.Length&&int.TryParse(args[widthIndex+1],out var width)&&int.TryParse(args[heightIndex+1],out var height))
                Screen.SetResolution(width,height,FullScreenMode.Windowed);
        }
        previewWindow=!Review&&Array.IndexOf(args,"--window-preview")>=0;
        previewStreet=!Review&&!previewWindow&&Array.IndexOf(args,"--street-preview")>=0;
        previewMilkboy=!Review&&!previewWindow&&!previewStreet&&Array.IndexOf(args,"--milkboy-preview")>=0;
        previewWatchmaker=!Review&&!previewWindow&&!previewStreet&&!previewMilkboy&&Array.IndexOf(args,"--watchmaker-preview")>=0;
        recordWalk=Array.IndexOf(args,"--record")>=0;
        reviewWindow=Array.IndexOf(args,"--review-window")>=0;
        reviewLighting=Review&&Array.IndexOf(args,"--review-lighting")>=0;
        lightingRight=reviewLighting&&Array.IndexOf(args,"--lighting-window-right")>=0;
        Silent=Review || Array.IndexOf(args,"--silent")>=0;
        // The officers' daily rounds run in play; isolated reviews keep their staged positions unless they look at the rounds.
        PoliceStaffRoutine.Enabled=!Review||Array.IndexOf(args,"--review-opening")>=0||Array.IndexOf(args,"--review-police-npc")>=0||Array.IndexOf(args,"--showcase-opening")>=0;
        // Passers-by and traffic: in play, and in the reviews and recordings that look at them; the other
        // reviews measure the empty streets (footway sweeps, light budgets, the story car's route).
        StreetLife.Enabled=!Review||Array.IndexOf(args,"--review-street-life")>=0||Array.IndexOf(args,"--review-street-traffic")>=0||Array.IndexOf(args,"--showcase-street")>=0;
        if(Review&&StreetLife.Enabled)StreetLife.Seed=1931;
        bool dialogueReview=Review&&(Array.IndexOf(args,"--review-dialogue")>=0||Array.IndexOf(args,"--verify-dialogue-save")>=0);
        bool migrationReview=Review&&Array.IndexOf(args,"--verify-walnut-migration")>=0;
        bool sprintReview=Review&&(Array.IndexOf(args,"--review-sprint-route")>=0||Array.IndexOf(args,"--verify-sprint-save")>=0);
        bool timeReview=Review&&(Array.IndexOf(args,"--review-time")>=0||Array.IndexOf(args,"--verify-time-save")>=0);
        bool towerMigrationReview=Review&&(Array.IndexOf(args,"--verify-clocktower-migration")>=0||Array.IndexOf(args,"--verify-clocktower-middle-save")>=0);
        bool towerReview=Review&&(Array.IndexOf(args,"--review-clocktower")>=0||Array.IndexOf(args,"--verify-clocktower-save")>=0);
        bool milkInteriorReview=Review&&(Array.IndexOf(args,"--review-milk-interior-a")>=0||Array.IndexOf(args,"--verify-milk-interior-save")>=0);
        bool dayNightReview=Review&&(Array.IndexOf(args,"--review-day-night")>=0||Array.IndexOf(args,"--verify-day-night-save")>=0);
        bool dairyDoorsReview=Review&&(Array.IndexOf(args,"--review-dairy-doors")>=0||Array.IndexOf(args,"--verify-dairy-doors-save")>=0);
        bool weatherReview=Review&&(Array.IndexOf(args,"--review-rain-presentation")>=0||Array.IndexOf(args,"--review-weather")>=0||Array.IndexOf(args,"--verify-weather-save")>=0);
        bool navigationReview=Review&&(Array.IndexOf(args,"--review-map")>=0||Array.IndexOf(args,"--verify-map-save")>=0||Array.IndexOf(args,"--review-props")>=0);
        bool fullMapReview=Review&&(Array.IndexOf(args,"--review-full-map")>=0||Array.IndexOf(args,"--verify-full-map-save")>=0);
        savePath=Path.Combine(Application.persistentDataPath,sprintReview?"sprint-review-save.json":dairyDoorsReview?"dairy-doors-review-save.json":navigationReview?"map-review-save.json":fullMapReview?"full-map-review-save.json":weatherReview?"weather-review-save.json":dayNightReview?"day-night-review-save.json":milkInteriorReview?"milk-interior-review-save.json":towerMigrationReview?"clocktower-migration-save.json":towerReview?"clocktower-review-save.json":timeReview?"time-review-save.json":migrationReview?"walnut-migration-save.json":dialogueReview?"dialogue-review-save.json":Review?"review-save.json":previewWindow?"window-preview-save.json":previewStreet?"street-preview-save.json":previewMilkboy?"milkboy-preview-save.json":previewWatchmaker?"watchmaker-preview-save.json":"slice-save.json");
        int c=Array.IndexOf(args,"--capture-dir");
        if(Review&&(Array.IndexOf(args,"--review-rail")>=0||Array.IndexOf(args,"--verify-rail-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"rail-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-milkboy-tripo")>=0)savePath=Path.Combine(Application.persistentDataPath,"milkboy-tripo-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-station-clerk")>=0||Array.IndexOf(args,"--verify-station-clerk-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"station-clerk-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-house")>=0||Array.IndexOf(args,"--verify-house-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"house-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-period-streets")>=0)savePath=Path.Combine(Application.persistentDataPath,"period-street-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-greenery")>=0)savePath=Path.Combine(Application.persistentDataPath,"greenery-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-watchmaker-tripo")>=0||Array.IndexOf(args,"--review-watchmaker")>=0))savePath=Path.Combine(Application.persistentDataPath,"watchmaker-tripo-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-items")>=0||Array.IndexOf(args,"--verify-items-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"items-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-car")>=0||Array.IndexOf(args,"--verify-car-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"car-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-tower-night")>=0||Array.IndexOf(args,"--review-day-look")>=0))savePath=Path.Combine(Application.persistentDataPath,"look-gallery-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-police")>=0||Array.IndexOf(args,"--verify-police-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"police-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-hotel")>=0||Array.IndexOf(args,"--verify-hotel-save")>=0||Array.IndexOf(args,"--review-hotel-day")>=0))savePath=Path.Combine(Application.persistentDataPath,"hotel-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-garage")>=0||Array.IndexOf(args,"--verify-garage-save")>=0||Array.IndexOf(args,"--review-garage-day")>=0))savePath=Path.Combine(Application.persistentDataPath,"garage-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-opening")>=0||Array.IndexOf(args,"--verify-opening-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"opening-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-police-npc")>=0)savePath=Path.Combine(Application.persistentDataPath,"police-npc-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-police-cast")>=0)savePath=Path.Combine(Application.persistentDataPath,"police-cast-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-opening-ride")>=0||Array.IndexOf(args,"--verify-opening-ride-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"opening-ride-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-opening-ride-interrupted")>=0||Array.IndexOf(args,"--verify-opening-ride-interrupted")>=0))savePath=Path.Combine(Application.persistentDataPath,"opening-ride-interrupted-review-save.json");
        if(!Review&&(Application.productName.EndsWith("第一幕试玩")||Array.IndexOf(args,"--opening-playtest")>=0))savePath=Path.Combine(Application.persistentDataPath,"opening-playtest-save.json");
        if(Review&&Array.IndexOf(args,"--review-map-sightlines")>=0)savePath=Path.Combine(Application.persistentDataPath,"map-sightline-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-yard-gate")>=0||Array.IndexOf(args,"--verify-yard-gate-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"yard-gate-review-save.json");
        if(Review&&(Array.IndexOf(args,"--review-hospital")>=0||Array.IndexOf(args,"--verify-hospital-save")>=0))savePath=Path.Combine(Application.persistentDataPath,"hospital-review-save.json");
        if(Review&&Array.IndexOf(args,"--showcase-opening")>=0)savePath=Path.Combine(Application.persistentDataPath,"showcase-opening-save.json");
        if(Review&&Array.IndexOf(args,"--review-street-life")>=0)savePath=Path.Combine(Application.persistentDataPath,"street-life-review-save.json");
        if(Review&&Array.IndexOf(args,"--review-street-traffic")>=0)savePath=Path.Combine(Application.persistentDataPath,"street-traffic-review-save.json");
        if(Review&&Array.IndexOf(args,"--showcase-street")>=0)savePath=Path.Combine(Application.persistentDataPath,"showcase-street-save.json");
        if(Review&&Array.IndexOf(args,"--showcase-hospital-cg")>=0)savePath=Path.Combine(Application.persistentDataPath,"showcase-hospital-cg-save.json");
        if(Array.IndexOf(args,"--audio-probe")>=0||Array.IndexOf(args,"--audio-probe-muted-start")>=0)savePath=Path.Combine(Application.persistentDataPath,"audio-probe-save.json");
        captures=c>=0&&c+1<args.Length?args[c+1]:Path.Combine(Application.persistentDataPath,"Captures");
        Directory.CreateDirectory(captures);
        Debug.Log("SAVE_LOCATION: "+savePath);
        if (File.Exists(savePath) && Array.IndexOf(args,"--fresh")<0) {
            try { state=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath)); }
            catch (Exception e) { Debug.LogWarning("Save could not be read: "+e.Message); }
        }
        if (state==null || state.entries==null) state=new SliceSave();
        if(state.dialogueTopics==null)state.dialogueTopics=new List<string>();
        if(state.openFixtures==null)state.openFixtures=new List<string>();
        if(state.initializedFixtures==null)state.initializedFixtures=new List<string>();
        if(state.reportSubmissions==null)state.reportSubmissions=new List<string>();
        if(state.reportSupported==null)state.reportSupported=new List<string>();
        InitializeOpeningState(args,File.Exists(savePath)&&Array.IndexOf(args,"--fresh")<0);
        InitializeHospitalState();
        InitializeGameClock(File.Exists(savePath)&&Array.IndexOf(args,"--fresh")<0);
        if (state.scene!="Town" && state.scene!="ClockShop" && state.scene!="OtherTown") state.scene="Town";
        if(previewWindow){state.scene="ClockShop";state.x=0;state.y=.03f;state.z=-.6f;opening=false;}
        if(previewStreet){state.scene="Town";state.x=0;state.y=.03f;state.z=1;opening=false;}
        if(previewMilkboy){state.scene="Town";state.x=3.8f;state.y=.03f;state.z=-4.1f;opening=false;}
        if(previewWatchmaker){state.scene="ClockShop";state.x=.2f;state.y=.03f;state.z=-.1f;opening=false;}
        if(reviewLighting){state.scene="ClockShop";state.x=lightingRight?.4f:-.4f;state.y=.03f;state.z=-.8f;}
        Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        StartCoroutine(InitialScene(args));
    }
    IEnumerator InitialScene(string[] args) {
        bool legacyRoom=state.scene=="ClockShop";
        string initial=state.scene=="OtherTown"?"OtherTown":"Town";
        yield return SceneManager.LoadSceneAsync(initial,LoadSceneMode.Additive);
        seamlessShop=FindFirstObjectByType<BuildingInstance>();
        milkShopRoot=GameObject.Find("MilkShop")?.transform;
        if(legacyRoom) {
            var mapped=seamlessShop.layout.ToTown(new Vector3(state.x,state.y,state.z));
            state.x=mapped.x;state.y=mapped.y;state.z=mapped.z;state.yaw+=seamlessShop.layout.townYaw;
            Debug.Log("SEAMLESS_SAVE_MIGRATED: interior-local position to Town world coordinates; evidence/dialogue preserved");
        }
        state.scene=initial;
        CurrentScene=initial;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(CurrentScene));
        var savedPosition=new Vector3(state.x,state.y,state.z);
        if(CurrentScene=="Town")MigrateTownPosition(ref savedPosition,legacyRoom);
        if(state.towerLayoutRevision<1&&!legacyRoom) {
            var tower=FindFirstObjectByType<ClockTowerInstance>();
            if(tower&&tower.TryMigrateLegacyPosition(savedPosition,out var migrated)) {
                savedPosition=migrated;towerSaveMigrated=true;towerMigratedPosition=migrated;
                state.x=migrated.x;state.y=migrated.y;state.z=migrated.z;
                Debug.Log("CLOCKTOWER_SAVE_MIGRATED: old tower position moved to corresponding safe stair/landing; progress retained");
            }
        }
        state.towerLayoutRevision=1;
        if(!SafePosition(savedPosition)) {
            Debug.LogWarning("Recovered invalid saved position: "+savedPosition);
            savedPosition=SafeSpawn();
        }
        if(CurrentScene=="Town"&&HouseInstance.Current)savedPosition=HouseInstance.Current.ResolveLoadedFurnitureOverlap(savedPosition);
        player.Place(savedPosition,float.IsNaN(state.yaw)||float.IsInfinity(state.yaw)?0:state.yaw);
        if(CurrentScene=="Town")player.Aim(previewWindow?seamlessShop.layout.ToTown(seamlessShop.layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f):new Vector3(0,ClockTowerInteriorLayout.DialHeight,-ClockTowerInteriorLayout.DialPlaneDistance));
        RestoreRailSeat();
        RestoreFurnitureSeat();
        RestoreCarSeat();
        if(previewStreet){var layout=FindFirstObjectByType<BuildingInstance>().layout;player.Aim(layout.ToTown(layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f));}
        if(previewMilkboy)player.Aim(GameObject.Find("Milkboy").transform.position+Vector3.up*.94f);
        if(previewWatchmaker)player.Aim(GameObject.Find("Clockmaker").transform.position+Vector3.up*1.1f);
        InitializeTownMap();
        InitializeOpeningScene();
        sceneReady=true;
        InitializeStreetLife();
        location=CurrentScene=="Town"?"青川 · 小镇广场":"邻镇 · 车站街区";
        locationAge=Time.time;
        yield return FadeTo(0,0.9f);
        if(previewWindow||previewStreet||previewMilkboy||previewWatchmaker)CursorMode();
        if (Review) {
            opening=false;
            if(Array.IndexOf(args,"--review-street-life")>=0)StartCoroutine(ReviewStreetLife());
            else if(Array.IndexOf(args,"--review-street-traffic")>=0)StartCoroutine(ReviewStreetTraffic());
            else if(Array.IndexOf(args,"--showcase-street")>=0)StartCoroutine(ShowcaseStreet());
            else if(Array.IndexOf(args,"--review-police-cast")>=0)StartCoroutine(ReviewPoliceCast());
            else if(Array.IndexOf(args,"--review-police-npc")>=0)StartCoroutine(ReviewPoliceNpc());
            else if(Array.IndexOf(args,"--review-opening-ride")>=0)StartCoroutine(ReviewOpeningRide());
            else if(Array.IndexOf(args,"--verify-opening-ride-save")>=0)StartCoroutine(VerifyOpeningRideSave());
            else if(Array.IndexOf(args,"--review-opening-ride-interrupted")>=0)StartCoroutine(ReviewOpeningRide(true));
            else if(Array.IndexOf(args,"--verify-opening-ride-interrupted")>=0)StartCoroutine(VerifyOpeningRideInterrupted());
            else if(Array.IndexOf(args,"--review-map-sightlines")>=0)StartCoroutine(ReviewMapSightlines());
            else if(Array.IndexOf(args,"--showcase-opening")>=0)StartCoroutine(ShowcaseOpening());
            else if(Array.IndexOf(args,"--showcase-hospital-cg")>=0)StartCoroutine(ShowcaseHospitalCinematic());
            else if(Array.IndexOf(args,"--review-hospital")>=0)StartCoroutine(ReviewHospital());
            else if(Array.IndexOf(args,"--verify-hospital-save")>=0)StartCoroutine(VerifyHospitalSave());
            else if(Array.IndexOf(args,"--review-yard-gate")>=0)StartCoroutine(ReviewYardGate());
            else if(Array.IndexOf(args,"--verify-yard-gate-save")>=0)StartCoroutine(VerifyYardGateSave());
            else if(Array.IndexOf(args,"--review-opening")>=0)StartCoroutine(ReviewOpening());
            else if(Array.IndexOf(args,"--verify-opening-save")>=0)StartCoroutine(VerifyOpeningSave());
            else if(Array.IndexOf(args,"--review-hotel-day")>=0)StartCoroutine(ReviewHotelGallery());
            else if(Array.IndexOf(args,"--review-hotel")>=0)StartCoroutine(ReviewHotel());
            else if(Array.IndexOf(args,"--verify-hotel-save")>=0)StartCoroutine(VerifyHotelSave());
            else if(Array.IndexOf(args,"--review-garage-day")>=0)StartCoroutine(ReviewGarageGallery());
            else if(Array.IndexOf(args,"--review-garage")>=0)StartCoroutine(ReviewGarage());
            else if(Array.IndexOf(args,"--verify-garage-save")>=0)StartCoroutine(VerifyGarageSave());
            else if(Array.IndexOf(args,"--review-police")>=0)StartCoroutine(ReviewPolice());
            else if(Array.IndexOf(args,"--verify-police-save")>=0)StartCoroutine(VerifyPoliceSave());
            else if(Array.IndexOf(args,"--review-house")>=0)StartCoroutine(ReviewHouse());
            else if(Array.IndexOf(args,"--verify-house-save")>=0)StartCoroutine(VerifyHouseSave());
            else if(Array.IndexOf(args,"--review-car")>=0)StartCoroutine(ReviewCar());
            else if(Array.IndexOf(args,"--verify-car-save")>=0)StartCoroutine(VerifyCarSave());
            else if(Array.IndexOf(args,"--review-sprint-route")>=0)StartCoroutine(ReviewSprintRoute());
            else if(Array.IndexOf(args,"--verify-sprint-save")>=0)StartCoroutine(VerifySprintSave());
            else if(Array.IndexOf(args,"--review-dairy-doors")>=0)StartCoroutine(ReviewDairyDoors());
            else if(Array.IndexOf(args,"--verify-dairy-doors-save")>=0)StartCoroutine(VerifyDairyDoorsSave());
            else if(Array.IndexOf(args,"--review-props")>=0)StartCoroutine(ReviewHeldProps());
            else if(Array.IndexOf(args,"--review-map")>=0)StartCoroutine(ReviewNavigationMap());
            else if(Array.IndexOf(args,"--verify-map-save")>=0)StartCoroutine(VerifyNavigationMapSave());
            else if(Array.IndexOf(args,"--review-items")>=0)StartCoroutine(ReviewFurniture());
            else if(Array.IndexOf(args,"--verify-items-save")>=0)StartCoroutine(VerifyFurnitureSave());
            else if(Array.IndexOf(args,"--review-rail")>=0)StartCoroutine(ReviewRail());
            else if(Array.IndexOf(args,"--verify-rail-save")>=0)StartCoroutine(VerifyRailSave());
            else if(Array.IndexOf(args,"--review-period-streets")>=0)StartCoroutine(ReviewPeriodStreets());
            else if(Array.IndexOf(args,"--review-greenery")>=0)StartCoroutine(ReviewGreenery());
            else if(Array.IndexOf(args,"--review-full-map")>=0)StartCoroutine(ReviewFullMap());
            else if(Array.IndexOf(args,"--verify-full-map-save")>=0)StartCoroutine(VerifyFullMapSave());
            else if(Array.IndexOf(args,"--review-rain-presentation")>=0)StartCoroutine(ReviewRainPresentation());
            else if(Array.IndexOf(args,"--review-weather")>=0)StartCoroutine(ReviewWeather());
            else if(Array.IndexOf(args,"--verify-weather-save")>=0)StartCoroutine(VerifyWeatherSave());
            else if(Array.IndexOf(args,"--review-day-night")>=0)StartCoroutine(ReviewDayNight());
            else if(Array.IndexOf(args,"--verify-day-night-save")>=0)StartCoroutine(VerifyDayNightSave());
            else if(Array.IndexOf(args,"--review-milk-interior-a")>=0)StartCoroutine(ReviewMilkInteriorA());
            else if(Array.IndexOf(args,"--verify-milk-interior-save")>=0)StartCoroutine(VerifyMilkInteriorSave());
            else if(Array.IndexOf(args,"--review-tower-night")>=0)StartCoroutine(ReviewTowerNightGallery());
            else if(Array.IndexOf(args,"--review-day-look")>=0)StartCoroutine(ReviewDayLookGallery());
            else if(Array.IndexOf(args,"--review-clocktower")>=0)StartCoroutine(ReviewClockTower());
            else if(Array.IndexOf(args,"--verify-clocktower-save")>=0)StartCoroutine(VerifyClockTowerSave());
            else if(Array.IndexOf(args,"--verify-clocktower-migration")>=0)StartCoroutine(VerifyClockTowerMigration());
            else if(Array.IndexOf(args,"--verify-clocktower-middle-save")>=0)StartCoroutine(VerifyClockTowerMiddleSave());
            else if(Array.IndexOf(args,"--review-time")>=0)StartCoroutine(ReviewTime());
            else if(Array.IndexOf(args,"--verify-time-save")>=0)StartCoroutine(VerifyTimeSave());
            else if(Array.IndexOf(args,"--review-walnut-shop")>=0)StartCoroutine(ReviewWalnutShop());
            else if(Array.IndexOf(args,"--verify-walnut-save")>=0)StartCoroutine(VerifyWalnutSave());
            else if(Array.IndexOf(args,"--verify-walnut-migration")>=0)StartCoroutine(VerifyWalnutMigration());
            else if(Array.IndexOf(args,"--review-milkboy-tripo")>=0)StartCoroutine(ReviewTripoMilkboy());
            else if(Array.IndexOf(args,"--review-station-clerk")>=0)StartCoroutine(ReviewStationClerk());
            else if(Array.IndexOf(args,"--verify-station-clerk-save")>=0)StartCoroutine(VerifyStationClerkSave());
            else if(Array.IndexOf(args,"--review-watchmaker-tripo")>=0||Array.IndexOf(args,"--review-watchmaker")>=0)StartCoroutine(ReviewTripoWatchmaker());
            else if(Array.IndexOf(args,"--review-dialogue")>=0)StartCoroutine(ReviewDialogue());
            else if(Array.IndexOf(args,"--verify-dialogue-save")>=0)StartCoroutine(VerifyDialogueSave());
            else if(Array.IndexOf(args,"--review-milkshop-windows")>=0)StartCoroutine(ReviewMilkShopWindows());
            else if(Array.IndexOf(args,"--review-milkshop")>=0)StartCoroutine(ReviewMilkShop());
            else if(Array.IndexOf(args,"--review-position")>=0)StartCoroutine(ReviewPosition());
            else if(Array.IndexOf(args,"--review-watch")>=0)StartCoroutine(ReviewWatch());
            else if(Array.IndexOf(args,"--review-milkboy")>=0)StartCoroutine(ReviewMilkboy());
            else if(reviewLighting)StartCoroutine(ReviewLighting(args));
            else if(Array.IndexOf(args,"--verify-save")>=0) StartCoroutine(VerifySaved());
            else StartCoroutine(ReviewWalnutShop());
        }
    }
    void Update() {
        UpdateMapLocation();
        UpdateGameClock();
        TickTowerAdjustment();
        TickHeldMap();
        TickEvidenceModel();
        TickBackpack();
        if(TickRailSeat())return;
        if(TickFurnitureSeat())return;
        TickCar();
        TickOpeningRide();
        TickOpeningEscort();
        if(sceneReady&&seamlessShop) {
            var p=seamlessShop.transform.InverseTransformPoint(player.transform.position);
            bool inside=Mathf.Abs(p.x)<seamlessShop.layout.footprint.x/2&&Mathf.Abs(p.z)<seamlessShop.layout.footprint.y/2;
            var milk=milkShopRoot?milkShopRoot.InverseTransformPoint(player.transform.position):new Vector3(99,99,99);
            bool inMilk=Mathf.Abs(milk.x)<3.6f&&Mathf.Abs(milk.z)<2.9f&&milk.y<3.18f;
            if(inside!=IsInsideShop||inMilk!=insideMilkShop){IsInsideShop=inside;insideMilkShop=inMilk;location=inside?"青川 · 钟表铺":inMilk?"青川 · 牛奶屋":"青川 · 小镇广场";locationAge=Time.time;Save();}
        }
        if (Input.GetKeyDown(KeyCode.F12)) StartCoroutine(Capture("manual-"+DateTime.Now.ToString("HHmmss")));
        // Review coroutines invoke the same handlers; desktop shortcuts must not race them.
        if(Review)return;
        if (transitioning) return;
        TickOpening();
        if(cellCinematic){if(Input.GetKeyDown(KeyCode.Escape))SkipCellCinematic();return;}
        if(openingRideActive){if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;CursorMode();}return;}
        if(UpdateCreatorInput())return;
        bool mapKey=Input.GetKeyDown(KeyCode.M);
        if(mapKey&&!mapOpen&&HeldItemBlockedByDriving())mapKey=false;
        if(!state.belongingsCollected&&(mapKey||Input.GetKeyDown(KeyCode.Q)||Input.GetKeyDown(KeyCode.T)||Input.GetKeyDown(KeyCode.B)||Input.GetKeyDown(KeyCode.Tab))&&!Blocked&&!opening){mapKey=false;OpeningReminder("你身上空空的。怀表、地图和随身的东西都被收走了。");}
        if(HandleMapKeys(mapKey,Input.GetKeyDown(KeyCode.Escape)))return;
        if(TickCarriedTicket())return;
        if(MilkOrdersOpen) { UpdateMilkOrderInput(); focus=null; return; }
        if(PoliceReportOpen) { UpdatePoliceReportInput(); focus=null; return; }
        if(DialogueOpen) { UpdateDialogueInput(); focus=null; return; }
        if(backpack) { UpdateBackpackInput(); focus=null; return; }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if(inspected) CloseInspect();
            else if(journal) journal=false;
            else if(!opening) paused=!paused;
            CursorMode();
        }
        if (!opening && !inspected && !paused) {
            if(Input.GetKeyDown(KeyCode.Tab) && (journal||state.belongingsCollected)) { if(journal){journal=false;CursorMode();} else OpenDossier(); }
            // B: the briefcase, from walking or from the dossier.
            if(Input.GetKeyDown(KeyCode.B) && state.belongingsCollected && (journal||!HeldItemBlockedByDriving())) { journal=false; OpenBackpack(); return; }
            if(Input.GetKeyDown(KeyCode.Q) && !journal && (watch||!HeldItemBlockedByDriving()) && (watch||state.belongingsCollected)) ToggleWatch();
        }
        if(inspected is ClockTowerAdjustment) { UpdateTowerAdjustmentInput(); focus=null; return; }
        focus=null;
        if(!Blocked && !RailSeated && !FurnitureSeated && Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)) focus=hit.collider.GetComponentInParent<Interactable>();
        // Inside the car, E on no particular control means getting out (when stopped).
        if(!Blocked && Input.GetKeyDown(KeyCode.E) && !TryInteract() && CarSeated) ExitCar();
    }
    void CursorMode() { bool free=(Blocked&&!railTravelling) || mapEditing || Review; Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=free; }
    void SetWatchOpen(bool value) {
        if(watch==value)return;
        if(value&&mapOpen)SetMapOpen(false);
        if(value&&!state.belongingsCollected)return;
        watch=value;GetComponent<TownSound>()?.PlayHeldItem(false,value);
    }
    void ToggleWatch() { SetWatchOpen(!watch); }
    public bool TryInteract() {
        if(Blocked||FurnitureSeated||RailSeated||OpeningRideActive) return false;
        if(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3f)) {
            var item=hit.collider.GetComponentInParent<Interactable>();
            if(Review)Debug.Log("INTERACTION_RAY: hit="+hit.collider.name+" distance="+hit.distance+" item="+(item?item.title:"none")+" eye="+player.eye.transform.position+" direction="+player.eye.transform.forward);
            if(item) {
                if(player.CreatorFlying&&(item is RailSeat||item is FurnitureSeat||item is ScenePortal||item is CarControl carControl&&carControl.kind==CarControlKind.Seat)){OpeningReminder("先按 F3 返回地面，再乘车、坐下或进入其他场景。");return true;}
                item.Use(); return true; }
        }
        else if(Review)Debug.Log("INTERACTION_RAY: no hit eye="+player.eye.transform.position+" direction="+player.eye.transform.forward);
        return false;
    }
    public void Inspect(Interactable item) {
        if(item.title=="住宅看守"){OpenHouseGuardDialogue(item);return;}
        if(item is HouseContainer){houseCode="";houseFeedback="";}
        if(item.evidenceId=="E04") { OpenMilkboyDialogue(); return; }
        if(item.title=="钟表匠") { OpenWatchmakerDialogue(); return; }
        if(item.title=="售票员") { OpenStationClerkDialogue(item); return; }
        if(item.title=="值班警员") { OpenPoliceOfficerDialogue(item); return; }
        if(item.title=="修车匠") { OpenMechanicDialogue(item); return; }
        EndEvidenceModel();inspected=item;if(ModelEvidence)BeginEvidenceModel(item); houseLetterOpen=false; towerAdjustmentFeedback=""; CursorMode();
        if(item.evidenceId=="E02"||item.evidenceId=="E03") {
            var existing=state.entries.Find(e=>e.id==item.evidenceId);
            if(existing!=null){existing.title=item.title;existing.body=item.body;existing.source=item.source;Save();}
        }
        // The player's own things in the briefcase (watch, map, notebook, wallet) are not evidence records.
        if(!string.IsNullOrEmpty(item.evidenceId) && !item.evidenceId.StartsWith("pack-") && !state.entries.Exists(e=>e.id==item.evidenceId)) {
            state.entries.Add(new NotebookEntry{id=item.evidenceId,title=item.title,body=item.body,source=item.source});
            Debug.Log("Recorded evidence: "+item.evidenceId);
            Save();
        }
    }
    void CloseInspect() {
        if(DialogueOpen) { CloseDialogue(); return; }
        if(inspected && inspected.evidenceId=="E07" && !state.remarkSeen && !Silent) {
            state.remarkSeen=true; remark="同行者  /  老街的事，店里总会留些纸。"; remarkStart=Time.time; remarkUntil=Time.time+5;
        }
        OpeningInspectionClosed();
        EndEvidenceModel();inspected=null; houseLetterOpen=false; houseReturnContainer=null; CursorMode(); Save();
        ReturnToBackpack();
    }
    public void Travel(ScenePortal portal) { if(!transitioning&&!CarSeated) StartCoroutine(Transition(portal)); }
    IEnumerator Transition(ScenePortal portal) {
        transitioning=true; CursorMode();
        string target=portal.destination;
        Vector3 spawn=portal.entryPosition;
        float yaw=portal.entryYaw;
        if(CurrentScene=="Town") {
            state.returnX=player.transform.position.x; state.returnY=player.transform.position.y; state.returnZ=player.transform.position.z;
        } else spawn=new Vector3(state.returnX,state.returnY,state.returnZ);
        Save(); yield return FadeTo(1,0.4f);
        string old=CurrentScene;
        yield return SceneManager.UnloadSceneAsync(old);
        yield return SceneManager.LoadSceneAsync(target,LoadSceneMode.Additive);
        CurrentScene=target;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(target));
        player.Place(spawn,yaw);
        RestoreOpeningRideScene();
        InitializeStreetLife();
        location=target=="Town"?"青川 · 小镇广场":"青川 · 钟表铺";
        locationAge=Time.time; Save();
        yield return FadeTo(0,0.6f);
        transitioning=false; CursorMode();
        Debug.Log("Door transition completed: "+old+" -> "+target);
    }
    IEnumerator FadeTo(float target,float duration) {
        float start=fade;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime) { fade=Mathf.Lerp(start,target,t/duration); yield return null; }
        fade=target;
    }
    public void Save() {
        if(!player||!sceneReady) return;
        if(railTravelling)return;
        StoreGameClock();
        state.towerLayoutRevision=1;
        state.scene=CurrentScene;
        var p=OpeningRideActive?OpeningRideSafePoint(state.openingRideStage):FurnitureSeated?restingExit:CarSeated?CarStandingPoint():player.transform.position;
        state.furnitureSeatId=FurnitureSeated?restingSeat.seatId:"";
        state.carSeatId=CarSeated?carSeat.seatId:"";
        if(CurrentScene=="Town"&&HotelCar.Current)HotelCar.Current.StoreState(state);
        if(player.CreatorFlying)p=player.CreatorReturnPoint;
        if(!SafePosition(p))p=SafeSpawn();
        state.x=p.x;state.y=p.y;state.z=p.z;state.yaw=player.transform.eulerAngles.y;
        string temporary=savePath+".tmp";
        File.WriteAllText(temporary,JsonUtility.ToJson(state,true));
        File.Copy(temporary,savePath,true); File.Delete(temporary);
    }
    void OnApplicationQuit() { Save(); townNavigation?.Dispose(); }
    Vector3 SafeSpawn() {
        if(CurrentScene=="OtherTown")return new Vector3(0,.1f,-38);
        if(CurrentScene=="ClockShop")return FindFirstObjectByType<BuildingInstance>().layout.interiorSpawn;
        return TownPlan.Spawn;
    }
    bool SafePosition(Vector3 p) {
        if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))return false;
        if(p.y < -1)return false;
        if(CurrentScene=="OtherTown")return p.y<4&&Mathf.Abs(p.x)<59&&p.z>-79&&p.z<29;
        if(p.y > 4) {
            var tower=CurrentScene=="Town"?FindFirstObjectByType<ClockTowerInstance>():null;
            if(!tower||!tower.ContainsUpperWalkPosition(p))return false;
        }
        if(CurrentScene=="Town"){var b=TownPlan.Current.bounds;return p.x>b[0]+.3f&&p.x<b[1]-.3f&&p.z>b[2]+.3f&&p.z<b[3]-.3f;}
        var layout=FindFirstObjectByType<BuildingInstance>().layout;
        return Mathf.Abs(p.x)<layout.footprint.x/2 && Mathf.Abs(p.z)<layout.footprint.y/2;
    }
    void LateUpdate() {
        ApplyCellCinematicEye();
        // The car recovers itself (and its occupant) if it leaves the valid town area.
        if(!sceneReady||transitioning||!player||player.CreatorFlying||CarSeated||OpeningRideActive||SafePosition(player.transform.position))return;
        Debug.LogWarning("Recovered out-of-bounds player: "+player.transform.position);
        player.Place(SafeSpawn(),0);
        Save();
    }
    void OnDestroy() { Application.wantsToQuit-=WantsToQuit; }
    bool WantsToQuit() {
        if(Review)Debug.Log("REVIEW_NATIVE_QUIT_REQUEST: "+Environment.StackTrace);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        RequestQuit();return false;
#else
        return true;
#endif
    }
    void RequestQuit() { if(!quitting){quitting=true;StartCoroutine(QuitAfterInput());} }
    IEnumerator QuitAfterInput() {
        paused=true;CursorMode();Save();
        // Let the native mouse-up/window message finish before closing the player.
        yield return new WaitForSecondsRealtime(.25f);
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        // This host's Unity 6.3 teardown crashes after relative mouse input.
        // Data is already flushed above. Exit only the standalone Windows player;
        // the OS releases its resources. Never terminate the Editor this way.
        // Mono Environment.Exit can wait indefinitely for Unity's worker threads.
        // Use the current-process pseudo-handle, never a PID or another process.
        if(!TerminateProcess(GetCurrentProcess(),0))throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
#else
        Application.Quit();
#endif
    }

    void Styles() {
        if(solid) return;
        solid=new Texture2D(1,1);solid.SetPixel(0,0,Color.white);solid.Apply();
        circle=new Texture2D(64,64,TextureFormat.RGBA32,false);
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float r=new Vector2(x-31.5f,y-31.5f).magnitude;circle.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(29.5f-r)));}circle.Apply();
        text=new GUIStyle{font=font,fontSize=22,wordWrap=true,normal={textColor=new Color(0.9f,0.88f,0.81f)}};
        small=new GUIStyle(text){fontSize=16};
        title=new GUIStyle(text){fontSize=40};
        large=new GUIStyle(text){fontSize=62};
        italic=new GUIStyle(small){normal={textColor=new Color(0.67f,0.73f,0.75f)}};
        button=new GUIStyle(text){alignment=TextAnchor.MiddleCenter,fontSize=20};
    }
    void Box(Rect r,Color color) { GUI.color=color;GUI.DrawTexture(r,solid);GUI.color=Color.white; }
    void Label(Rect r,string s,GUIStyle style=null) { GUI.Label(r,s,style??text); }
    bool Button(Rect r,string s,bool warm=false) {
        bool hover=r.Contains(Event.current.mousePosition);
        Box(r,hover?new Color(0.27f,0.35f,0.36f,0.98f):new Color(0.09f,0.16f,0.18f,0.96f));
        Box(new Rect(r.x,r.y,r.width,1),warm?new Color(0.68f,0.5f,0.3f):new Color(0.37f,0.52f,0.53f));
        return GUI.Button(r,s,button);
    }
    void Panel(Rect r) { Box(r,new Color(0.055f,0.10f,0.13f,0.97f)); Box(new Rect(r.x,r.y,r.width,2),new Color(0.68f,0.5f,0.3f)); }
    void OnGUI() {
        if(reviewPortrait)return;
        if(Review&&(Event.current.isKey||Event.current.isMouse))return;
        Styles(); float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        W=Screen.width/scale;H=Screen.height/scale;
        GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(scale,scale,1));
        if(ModelEvidence&&!houseLetterOpen){DrawEvidenceModel();GUI.matrix=Matrix4x4.identity;return;}
        if(backpack){DrawBackpack();GUI.matrix=Matrix4x4.identity;return;}
        if(opening && (!Review||showcaseTitle)) {
            Box(new Rect(0,0,610,H),new Color(0.035f,0.07f,0.10f,0.72f));
            Label(new Rect(75,100,400,35),"青川  /  一九三一",italic);
            Label(new Rect(70,160,480,90),"错日证词",large);
            Box(new Rect(75,270,70,2),new Color(0.77f,0.57f,0.35f));
            // The premise (user, 2026-10-15): he chose to come back, by Dr Fang Ming's shuttle device, for a murder never solved in his own time.
            Label(new Rect(75,315,430,120),CjkWrap(state.openingStage<4?"一桩到你那个年代都没破的命案。\n方明博士的装置，把你送回了一九三一年。":"调查还在继续。\n重新走访，核对你手中的材料。",text,430),text);
            if(Button(new Rect(75,H-230,360,60),state.openingStage<4?"在牢房醒来":"继续调查",true)) { opening=false; CursorMode(); Save(); }
            Label(new Rect(75,H-140,450,70),"自由走动，靠近物件按 E 查看。\nQ 怀表  ·  M 纸地图  ·  B 公文包  ·  Tab 档案  ·  Esc 暂停",small);
        } else {
            Label(new Rect(36,26,250,35),"错日证词",text);
            Label(new Rect(36,62,300,30),CurrentScene=="OtherTown"?"邻镇  /  一九三一":"青川  /  一九三一",italic);
            var right=new GUIStyle(small){alignment=TextAnchor.UpperRight};
            Label(new Rect(W-350,32,310,32),location,right);
            DrawOpeningObjective();
            if(!Blocked&&!OpeningRideActive) {
                if(!mapOpen||player.pitch<25){GUI.color=new Color(1,0.95f,0.85f,0.8f);GUI.DrawTexture(new Rect(W/2-3,H/2-3,6,6),circle);GUI.color=Color.white;}
                if(focus&&(!mapOpen||player.pitch<25)) {
                    Rect p=new Rect(W/2-170,H/2+38,340,43);Box(p,new Color(0.04f,0.08f,0.11f,0.87f));
                    Label(p,"E   "+(focus is ScenePortal||focus is ShopMechanism||focus is MilkShopFixture||focus is RailSeat?focus.title:focus.evidenceId=="E04"||focus.title=="钟表匠"||focus.title=="值班警员"?"交谈 · "+focus.title:focus.ActionText),button);
                }
                if(!mapOpen)Label(new Rect(36,H-50,W-72,30),!state.belongingsCollected?"WASD 行走    鼠标 观察    Shift 奔跑    E 互动    Esc 暂停":CarSeated?CarHelpLine():RailSeated?"鼠标 观察    E 起身下车    Q 怀表    T 车票    M 纸地图    B 公文包    Tab 档案    Esc 暂停":"WASD 行走    鼠标 观察    Shift 奔跑    E 互动    Q 怀表    T 车票    M 纸地图    B 公文包    Tab 档案    Esc 暂停",small);
            }
            if(watch&&!mapOpen&&!inspected&&!journal&&!paused&&!DialogueOpen&&!MilkOrdersOpen) DrawWatch();
            DrawOpeningRide();
            if(ChildCinematicActive) DrawChildCinematic(); else if(DialogueOpen) DrawDialogue();
            if(houseLetterOpen&&inspected) DrawHouseLetter();
            else if(inspected is ClockTowerAdjustment) DrawTowerAdjustment();
            else if(inspected is HouseContainer) DrawHouseContainer((HouseContainer)inspected);
            else if(inspected) DrawInspection(inspected.title,inspected.body,inspected.source,inspected.illustration);
            DrawHousePaperBack();
            if(MilkOrdersOpen) DrawMilkOrders();
            if(PoliceReportOpen) DrawPoliceReport();
            if(journal) DrawJournal();
            if(paused&&!creatorMenu) {
                Box(new Rect(0,0,W,H),new Color(0,0,0,0.42f));Panel(new Rect(W/2-230,H/2-260,460,540));
                Label(new Rect(W/2-180,H/2-222,360,50),"稍作停留",title);
                if(Button(new Rect(W/2-180,H/2-155,360,55),"继续游戏")){paused=false;CursorMode();}
                bool enabled=GUI.enabled;GUI.enabled=enabled&&CreatorActionsAvailable;
                if(Button(new Rect(W/2-180,H/2-85,360,55),CreatorActionsAvailable?"开发者模式":"开发者模式 · 乘坐中不可用"))OpenCreatorMenu();
                GUI.enabled=enabled;
                if(Button(new Rect(W/2-180,H/2-15,360,55),"保存并退出"))RequestQuit();
                DrawAudioSettings(W/2-180,H/2+84,360);
            }
            DrawCreator();
            if(!mapOpen&&!Silent&&Time.time<remarkUntil&&!inspected&&!journal&&!DialogueOpen&&!MilkOrdersOpen)DrawRemark();
        }
        if(mapOpen&&!Blocked) DrawHeldMapDestination();
        DrawRailStatus();
        DrawFurnitureStatus();
        DrawCarStatus();
        DrawShowcaseCaption();
        if(fade>0) Box(new Rect(0,0,W,H),new Color(0.018f,0.022f,0.03f,fade));
        DrawChildCinematicCard();
        DrawCellCinematic();
        GUI.matrix=Matrix4x4.identity;
    }
    void DrawWatch() {
        float size=230,x=42,y=H-330;
        Box(new Rect(x+size/2-11,y-14,22,26),new Color(.58f,.4f,.2f));
        Box(new Rect(x+size/2-20,y-22,40,12),new Color(.68f,.49f,.25f));
        GUI.color=new Color(0.12f,0.09f,0.07f,0.8f);GUI.DrawTexture(new Rect(x+10,y+15,size,size),circle);
        GUI.color=new Color(0.62f,0.44f,0.22f);GUI.DrawTexture(new Rect(x,y,size,size),circle);
        GUI.color=Color.white;if(dialTexture)GUI.DrawTexture(new Rect(x+15,y+15,size-30,size-30),dialTexture);
        float minutes=Minutes;
        if(Event.current.type==EventType.Repaint)watchRenderedMinutes=minutes;
        Vector2 center=new Vector2(x+size/2,y+size/2);DrawHand(center,minutes%720*0.5f,56,5);DrawHand(center,minutes%60*6f,80,3);
        GUI.color=new Color(0.2f,0.16f,0.13f);GUI.DrawTexture(new Rect(center.x-5,center.y-5,10,10),circle);GUI.color=Color.white;
        Label(new Rect(x+20,y+size+14,230,30),"怀表 · 你的随身物件",small);
    }
    void DrawHand(Vector2 center,float angle,float length,float width) {
        var previous=GUI.matrix;
        // Keep the pivot in logical watch coordinates, then apply the HUD scale once.
        GUI.matrix=previous*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.Euler(0,0,angle),Vector3.one);
        Box(new Rect(-width/2,-length,width,length),new Color(0.14f,0.12f,0.12f));GUI.matrix=previous;
    }
    void DrawInspection(string name,string body,string source,Texture2D image) {
        Box(new Rect(0,0,W,H),new Color(0.015f,0.03f,0.04f,0.62f));
        Rect p=new Rect(W/2-560,H/2-310,1120,620);Panel(p);
        Label(new Rect(p.x+42,p.y+32,850,55),name,title);
        Label(new Rect(p.x+42,p.y+97,950,40),"出处  /  "+source,italic);
        Box(new Rect(p.x+42,p.y+144,1036,1),new Color(0.29f,0.37f,0.37f));
        if(image) {Box(new Rect(p.x+42,p.y+172,460,340),new Color(.10f,.16f,.15f,.96f));GUI.DrawTexture(new Rect(p.x+54,p.y+184,436,316),image,ScaleMode.ScaleToFit,true);Label(new Rect(p.x+550,p.y+180,500,300),body,text);}
        else Label(new Rect(p.x+48,p.y+182,1010,310),body,text);
        Label(new Rect(p.x+42,p.y+550,550,32),inspected&&!string.IsNullOrEmpty(inspected.evidenceId)?"已记下，可在公文包里再次查看。":"可随时再次查看。",small);
        if(Button(new Rect(p.x+865,p.y+532,210,52),"收起  ·  Esc")) { CloseInspect(); }
    }
    // Tab: the dossier — what people said (user, 2026-10-15); physical evidence is in the briefcase (B).
    void DrawJournal() {
        Box(new Rect(0,0,W,H),new Color(0.015f,0.03f,0.04f,0.5f));
        Rect p=new Rect(W/2-570,H/2-310,1140,620);Panel(p);
        Label(new Rect(p.x+40,p.y+32,600,60),"档案",title);
        Label(new Rect(p.x+40,p.y+105,1000,40),"口供与问话  /  保留原话，慢慢核对。物证收在公文包里（B）。",italic);
        Box(new Rect(p.x+40,p.y+153,1060,1),new Color(0.29f,0.37f,0.37f));
        var people=Dossier();
        if(people.Count==0) Label(new Rect(p.x+45,p.y+205,990,180),"还没有问话记录。\n和镇上的人谈谈，他们说过的话会记在这里。",text);
        else {
            journalPage=Mathf.Clamp(journalPage,0,Mathf.Max(0,(people.Count-1)/4));
            selectedEntry=Mathf.Clamp(selectedEntry,0,people.Count-1);
            for(int i=journalPage*4;i<Mathf.Min(people.Count,journalPage*4+4);i++) if(Button(new Rect(p.x+40,p.y+180+(i%4)*76,310,60),people[i].name,i==selectedEntry)){selectedEntry=i;housePaperScroll=Vector2.zero;}
            if(journalPage>0&&Button(new Rect(p.x+40,p.y+505,100,45),"上一页")){journalPage--;selectedEntry=journalPage*4;housePaperScroll=Vector2.zero;}
            Label(new Rect(p.x+145,p.y+512,100,35),(journalPage+1)+" / "+Mathf.Max(1,(people.Count+3)/4),small);
            if((journalPage+1)*4<people.Count&&Button(new Rect(p.x+250,p.y+505,100,45),"下一页")){journalPage++;selectedEntry=journalPage*4;housePaperScroll=Vector2.zero;}
            var person=people[selectedEntry];
            Label(new Rect(p.x+400,p.y+183,640,55),person.name,title);
            Label(new Rect(p.x+400,p.y+250,640,40),person.place,italic);
            float height=Mathf.Max(200,text.CalcHeight(new GUIContent(person.body),615)+10);
            housePaperScroll=GUI.BeginScrollView(new Rect(p.x+400,p.y+300,650,222),housePaperScroll,new Rect(0,0,615,height));Label(new Rect(0,0,615,height),person.body,text);GUI.EndScrollView();
        }
        if(Button(new Rect(p.x+400,p.y+538,215,52),"打开公文包  ·  B")){journal=false;OpenBackpack();}
        if(Button(new Rect(p.x+630,p.y+538,225,52),"整理阶段报告")){journal=false;OpenPoliceReport();}
        if(Button(new Rect(p.x+870,p.y+538,225,52),"继续调查  ·  Tab")){journal=false;CursorMode();}
    }
    IEnumerator Capture(string name) {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(captures,name+".png"));
        yield return new WaitForSeconds(0.2f);
        Debug.Log("Captured runtime frame: "+name);
    }
    IEnumerator WalkTo(Vector3 target,float speedFactor=1) {
        Debug.Log("Review walking: "+player.transform.position+" -> "+target+" blocked="+Blocked+" motor="+player.enabled);
        float until=Time.time+20;
        while(Vector3.Distance(new Vector3(player.transform.position.x,0,player.transform.position.z),new Vector3(target.x,0,target.z))>0.10f&&Time.time<until) {
            Vector3 d=target-player.transform.position;d.y=0;player.scriptedDirection=d.normalized*speedFactor;yield return null;
        }
        player.scriptedDirection=Vector3.zero;
        if(Time.time>=until) throw new Exception("Review movement blocked before "+target+" at "+player.transform.position+" blocked="+Blocked+" direction="+player.scriptedDirection);
    }
    IEnumerator ReviewPosition() {
        yield return new WaitForSeconds(.5f);
        if(!SafePosition(player.transform.position))throw new Exception("Invalid save was not recovered");
        int entries=state.entries.Count;
        player.Place(new Vector3(-10.69f,-7210.03f,37.44f),0);
        Save();
        var saved=JsonUtility.FromJson<SliceSave>(File.ReadAllText(savePath));
        if(!SafePosition(new Vector3(saved.x,saved.y,saved.z))||saved.entries.Count!=entries)throw new Exception("Unsafe save or lost evidence");
        yield return null;yield return null;
        if(!SafePosition(player.transform.position))throw new Exception("Fall recovery failed");
        player.Place(new Vector3(8.5f,.1f,30),0);
        scriptedNorthStart=player.transform.position.z;
        player.scriptedDirection=Vector3.forward;
        yield return new WaitForSeconds(2);
        player.scriptedDirection=Vector3.zero;
        if(player.transform.position.z<scriptedNorthStart+.3f||player.transform.position.z>31.7f)throw new Exception("North boundary did not stop walking");
        yield return Capture("north-boundary");
        player.Place(SafeSpawn(),0);Save();
        yield return Capture("safe-spawn");
        Debug.Log("POSITION_REVIEW_PASS: invalid save, runtime fall, safe save, evidence retention and north collision");
        Application.Quit();
    }
    float scriptedNorthStart;
    IEnumerator ReviewWatch() {
        yield return new WaitForSeconds(1);
        if(!dialTexture)throw new Exception("Pocket-watch face texture is missing.");
        ToggleWatch();
        yield return Capture("watch-open");
        float first=watchRenderedMinutes;
        yield return new WaitForSeconds(1.1f);
        yield return Capture("watch-later");
        if(Mathf.Repeat(watchRenderedMinutes-first,1440f)<=0)throw new Exception("Pocket-watch time is not advancing.");
        yield return new WaitForEndOfFrame();
        foreach(var dial in FindObjectsByType<ClockDial>(FindObjectsSortMode.None)) {
            float expected=-((Minutes+(dial.streetClock?(float)GameClock.TowerOffsetMinutes:0))%60)*6;
            if(Mathf.Abs(Mathf.DeltaAngle(dial.minuteHand.localEulerAngles.z,expected))>.5f)
                throw new Exception("Scene clock no longer matches shared game time.");
        }
        float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
        var result=new WatchReviewResult{width=Screen.width,height=Screen.height,hudScale=scale,
            centerX=157*scale,centerY=Screen.height-215*scale,minutes=watchRenderedMinutes,
            timeAdvanced=true,sharedClockHandsAligned=true,
            verification="Native rendered HUD; same Q toggle handler, keyboard hardware bypassed. Hand visibility requires screenshot pixel checks."};
        ToggleWatch();
        if(watch)throw new Exception("Pocket-watch toggle did not close.");
        yield return Capture("watch-closed");
        result.toggleClosed=true;
        File.WriteAllText(Path.Combine(captures,"watch-results.json"),JsonUtility.ToJson(result,true));
        Debug.Log("WATCH_REVIEW_PASS: "+Screen.width+"x"+Screen.height+" toggle, advancing time and shared clocks; inspect rendered hands");
        Application.Quit();
    }
    [Serializable] class WatchReviewResult {
        public int width,height;
        public float hudScale,centerX,centerY,minutes;
        public bool timeAdvanced,sharedClockHandsAligned,toggleClosed;
        public string verification;
    }
    IEnumerator ReviewMilkShop() {
        yield return new WaitForSeconds(1);
        var shop=GameObject.Find("MilkShop");var boy=GameObject.Find("Milkboy");
        if(!shop||!boy)throw new Exception("Milk shop or milkboy missing");
        var placement=shop.GetComponent<MilkShopPlacement>();
        if(!placement||placement.minimumBuildingGap<.55f)throw new Exception("Milk shop spacing validation is missing");
        // Start in the square, reach the unchanged witness and use the actual interaction ray.
        yield return WalkTo(new Vector3(3.8f,.1f,-4.1f));
        player.Aim(boy.transform.position+Vector3.up*.94f);
        yield return Capture("milkshop-behind-boy");
        if(!TryInteract()||!DialogueOpen)throw new Exception("Milkboy dialogue interaction was obstructed by the milk shop");
        yield return Capture("milkboy-dialogue");CloseInspect();
        // Take the southern approach to avoid the boy and cart, then walk through the real doorway.
        yield return WalkTo(new Vector3(4.9f,.1f,-8.0f));
        yield return WalkTo(placement.outside);
        player.Aim(placement.inside+Vector3.up*1.0f);yield return Capture("milkshop-doorway");
        yield return WalkTo(placement.inside);
        player.Aim(shop.transform.TransformPoint(new Vector3(0,1.35f,1.05f)));yield return Capture("milkshop-inside");
        yield return WalkTo(placement.outside);
        // Walk the alley between the unchanged clock shop and the new milk shop.
        yield return WalkTo(new Vector3(7.3f,.1f,-5.0f));
        yield return WalkTo(placement.northPassageStart);
        yield return WalkTo(placement.northPassageEnd);
        player.Aim(new Vector3(11.5f,1.6f,-3.7f));yield return Capture("milkshop-building-gap");
        // Player-height street view plus controlled overview, using the actual town lighting.
        yield return WalkTo(placement.northPassageStart);
        yield return WalkTo(new Vector3(7.3f,.1f,-5.0f));
        yield return WalkTo(new Vector3(7.3f,.1f,-8.0f));
        yield return WalkTo(new Vector3(4.8f,.1f,-8.8f));
        player.Aim(shop.transform.position+Vector3.up*2.8f);yield return Capture("milkshop-street-view");
        player.enabled=false;reviewPortrait=true;
        var eye=player.eye;eye.transform.position=new Vector3(1.0f,8.8f,-13.3f);eye.transform.LookAt(new Vector3(10,2.4f,-4.5f));eye.fieldOfView=62;
        yield return Capture("milkshop-overview");
        reviewPortrait=false;player.enabled=true;
        File.WriteAllText(Path.Combine(captures,"milkshop-results.json"),"{\"passed\":true,\"E04Raycast\":true,\"doorwayRoundTrip\":true,\"northAlleyWalk\":true,\"scale\":0.9,\"minimumBuildingGap\":"+placement.minimumBuildingGap.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",\"verification\":\"actual controller and interaction ray; overview uses controlled camera; existing slice, not full fourth-map migration\"}");
        Debug.Log("MILKSHOP_REVIEW_PASS: E04, doorway round trip, alley walking and rendered placement");
        Application.Quit();
    }
    IEnumerator ReviewMilkboy() {
        yield return new WaitForSeconds(1);
        var boy=GameObject.Find("Milkboy");
        if(!boy)throw new Exception("Milkboy is missing from Town.");
        Vector3 origin=boy.transform.position,front=-boy.transform.forward,right=boy.transform.right;
        // Reach the NPC from the square with the real motor and interaction ray.
        yield return ApproachReviewMilkboy();
        player.Aim(origin+Vector3.up*.94f);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-player-view");
        if(!TryInteract()||!DialogueOpen)throw new Exception("Milkboy interaction ray failed.");
        yield return Capture("unity-dialogue");CloseInspect();
        // Controlled detail viewpoints; lighting and materials stay as in the playable town.
        player.enabled=false;reviewPortrait=true;
        var eye=player.eye;float originalFov=eye.fieldOfView;eye.nearClipPlane=.04f;
        void Frame(Vector3 position,Vector3 target,float fov) {
            eye.transform.position=position;eye.transform.LookAt(target);eye.fieldOfView=fov;
        }
        Frame(origin+front*2.45f+right*.60f+Vector3.up*1.05f,origin+Vector3.up*.70f,36);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-full");
        var animator=boy.GetComponentInChildren<Animator>();
        if(!animator||!animator.GetCurrentAnimatorStateInfo(0).loop||animator.applyRootMotion)
            throw new Exception("Milkboy must have a looping, in-place idle.");
        var head=boy.GetComponentInChildren<MilkboyPerformance>().head;
        if(!head)throw new Exception("Milkboy animated head bone is missing.");
        var headRotation=head.localRotation;float startTime=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        yield return new WaitForSeconds(1.2f);yield return Capture("unity-idle-later");
        float headMotion=Quaternion.Angle(headRotation,head.localRotation);
        float timeAdvance=animator.GetCurrentAnimatorStateInfo(0).normalizedTime-startTime;
        if(headMotion<.05f||timeAdvance<=0||Vector3.Distance(origin,boy.transform.position)>.001f)
            throw new Exception("Milkboy idle is static or moves the actor root.");
        Frame(origin+front*.94f+right*.19f+Vector3.up*1.22f,origin+Vector3.up*1.14f,33);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-face");
        var leftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand);
        var rightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        if(!leftHand||!rightHand)throw new Exception("Milkboy hand bones are missing.");
        var handCentre=(leftHand.position+rightHand.position)*.5f;
        Frame(handCentre+front*1.05f+right*.08f+Vector3.up*.04f,handCentre,44);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-hands");
        Frame(origin+front*.42f+right*.62f+Vector3.up*1.16f,origin+Vector3.up*1.02f,54);
        yield return new WaitForSeconds(.25f);yield return Capture("unity-side");
        eye.fieldOfView=originalFov;player.enabled=true;
        File.WriteAllText(Path.Combine(captures,"runtime-results.json"),"{\"passed\":true,\"npc\":\"E04\",\"source\":\"user-supplied Meshy messenger\",\"realMotorAndRaycast\":true,\"loopingIdle\":true,\"stationaryRoot\":true,\"headMotionDegrees\":"+headMotion.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+",\"detailViews\":\"controlled camera, unchanged town lighting\"}");
        Debug.Log("MILKBOY_REVIEW_PASS: real movement, E04 raycast, looping animated idle, full/face/hands/side frames");
        Application.Quit();
    }
    IEnumerator ReviewWatchmaker() {
        yield return WalkTo(new Vector3(.2f,0,-.1f));
        var owner=GameObject.Find("Clockmaker");
        if(!owner||owner.GetComponent<AmbientMotion>())throw new Exception("Seated watchmaker is missing or has whole-body bobbing.");
        var origin=owner.transform.position;
        var animator=owner.GetComponentInChildren<Animator>();
        var bones=owner.GetComponentsInChildren<Transform>();
        Transform Bone(string name)=>Array.Find(bones,t=>t.name.Replace(':','_')==name);
        var hips=Bone("mixamorig_Hips");var head=Bone("mixamorig_Head");
        var leftFoot=Bone("mixamorig_LeftFoot");var rightFoot=Bone("mixamorig_RightFoot");
        if(!animator||!hips||!head||!leftFoot||!rightFoot||animator.applyRootMotion||!animator.GetCurrentAnimatorStateInfo(0).loop)
            throw new Exception("Watchmaker requires a real looping seated skeleton with no root motion.");
        var playing=animator.GetCurrentAnimatorClipInfo(0);
        if(playing.Length!=1||!playing[0].clip.name.Contains("Sit_Cross_Legged"))throw new Exception("Watchmaker is playing a non-seated clip.");
        var chair=owner.transform.Find("Watchmaker chair");
        if(!chair||!chair.GetComponent<Collider>())throw new Exception("Watchmaker chair has no physical support/collision.");
        var seat=Array.Find(chair.GetComponentsInChildren<Transform>(),t=>t.name=="ChairSeat");
        if(!seat)throw new Exception("Chair seat mesh is missing.");
        float seatTop=seat.GetComponent<Renderer>().bounds.max.y;
        if(Mathf.Abs(seatTop-.40f)>.015f)throw new Exception("Chair import height is incorrect: "+seatTop);
        var back=Array.Find(chair.GetComponentsInChildren<Transform>(),t=>t.name=="Back top rail");
        if(!back||Vector3.Dot(back.position-seat.position,owner.transform.forward)<.2f)
            throw new Exception("Chair backrest must be behind the seated owner.");
        player.Aim(head.position+Vector3.up*.12f);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-player-view");
        if(!TryInteract()||inspected!=owner.GetComponent<Interactable>())throw new Exception("Watchmaker cannot be spoken to over the counter.");
        yield return Capture("unity-dialogue");CloseInspect();
        player.enabled=false;reviewPortrait=true;
        var eye=player.eye;float originalFov=eye.fieldOfView;
        void Frame(Vector3 position,Vector3 target,float fov) {
            eye.transform.position=position;eye.transform.LookAt(target);eye.fieldOfView=fov;
        }
        Frame(origin+new Vector3(1.25f,1.03f,-1.55f),origin+Vector3.up*.70f,50);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-seated-full");
        var minHip=hips.position;var maxHip=hips.position;
        var firstHead=head.localRotation;float maxHeadMotion=0,minFoot=float.PositiveInfinity,maxRootMotion=0;
        float start=Time.time,startNormalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        float duration=playing[0].clip.length+.35f;int samples=0,animationFrames=0;
        bool record=Array.IndexOf(Environment.GetCommandLineArgs(),"--record-watchmaker")>=0;
        string motionFolder=Path.Combine(captures,"seated-motion");
        if(record)Directory.CreateDirectory(motionFolder);
        float captureAt=Time.time;
        while(Time.time-start<duration) {
            minHip=Vector3.Min(minHip,hips.position);maxHip=Vector3.Max(maxHip,hips.position);
            maxHeadMotion=Mathf.Max(maxHeadMotion,Quaternion.Angle(firstHead,head.localRotation));
            minFoot=Mathf.Min(minFoot,leftFoot.position.y,rightFoot.position.y);
            maxRootMotion=Mathf.Max(maxRootMotion,Vector3.Distance(origin,owner.transform.position));
            if(hips.position.y>seatTop+.32f||hips.position.y<seatTop+.10f)throw new Exception("Watchmaker stood up or lost the seat.");
            if(record&&Time.time>=captureAt) {
                ScreenCapture.CaptureScreenshot(Path.Combine(motionFolder,"frame"+(animationFrames++).ToString("D4")+".png"));
                captureAt=Time.time+.1f;
            }
            samples++;yield return null;
        }
        float loops=animator.GetCurrentAnimatorStateInfo(0).normalizedTime-startNormalized;
        if(maxRootMotion>.001f||maxHeadMotion<.05f||maxHip.y-minHip.y>.035f||minFoot<.10f||loops<1)
            throw new Exception("Seated loop is static, drifting, standing or below the floor: hip="+(maxHip-minHip)+" foot="+minFoot+" loops="+loops);
        yield return Capture("unity-seated-later");
        Frame(origin+new Vector3(1.8f,1.02f,.15f),origin+Vector3.up*.70f,54);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-chair-side");
        Frame(origin+new Vector3(.7f,.95f,1.05f),origin+new Vector3(0,.61f,.08f),49);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-chair-back");
        Frame(head.position+new Vector3(.40f,.22f,-1.02f),head.position+Vector3.up*.11f,36);
        yield return new WaitForSeconds(.3f);yield return Capture("unity-face");
        eye.fieldOfView=originalFov;reviewPortrait=false;player.enabled=true;
        player.Place(new Vector3(.2f,.03f,-.1f),0);player.Aim(owner.transform.position+Vector3.up*1.1f);
        File.WriteAllText(Path.Combine(captures,"watchmaker-results.json"),JsonUtility.ToJson(new WatchmakerReviewResult {
            passed=true,realMotorAndDialogueRay=true,loopingSeatedAnimation=true,rootStationary=true,
            chairSeatTop=seatTop,minFootBoneHeight=minFoot,maxRootMotion=maxRootMotion,
            hipMotion=maxHip-minHip,headMotionDegrees=maxHeadMotion,observedLoops=loops,samples=samples,animationFrames=animationFrames,
            verification="Native Windows player, automatic controller/raycast interaction; full supplied sitting cycle sampled. Portraits use controlled cameras with unchanged shop lighting. Bone heights supplement visual seat/sole inspection."
        },true));
        Debug.Log("WATCHMAKER_REVIEW_PASS: dialogue ray, full seated loop, stationary root, chair and side/back/face captures");
    }
    [Serializable] class WatchmakerReviewResult {
        public bool passed,realMotorAndDialogueRay,loopingSeatedAnimation,rootStationary;
        public float chairSeatTop,minFootBoneHeight,maxRootMotion,headMotionDegrees,observedLoops;
        public Vector3 hipMotion;
        public int samples,animationFrames;
        public string verification;
    }
    IEnumerator ReviewRun() {
        yield return new WaitForSeconds(2);
        yield return Capture("town");watch=true;yield return Capture("town-watch");watch=false;
        if(recordWalk){recordingWalk=true;StartCoroutine(RecordWalk());}
        yield return WalkTo(new Vector3(0,0,-5));
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--review-roads")>=0)yield return ReviewRoads();
        if(reviewWindow) {
            yield return ReviewStreet();
            yield return WalkTo(new Vector3(0,0,1));
            var building=FindFirstObjectByType<BuildingInstance>();
            exteriorWindowCount=0;
            foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>()) {
                if(marker.kind==OpeningKind.Window)exteriorWindowCount++;
                var opening=building.layout.Opening(marker.openingId);
                if(Vector3.Distance(marker.transform.position,building.layout.ToTown(building.layout.OpeningCentre(opening)))>.001f)
                    throw new Exception("Exterior opening is not aligned with its shared layout: "+marker.openingId);
            }
            player.Aim(building.layout.ToTown(building.layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f));
            yield return new WaitForSeconds(.3f);yield return Capture("layout-exterior");
        }
        yield return WalkTo(new Vector3(3.7f,0,1));
        recordingWalk=false;
        player.Aim(new Vector3(5.8f,1.2f,1));yield return null;
        if(!TryInteract()) throw new Exception("Shop door could not be reached by interaction ray");
        yield return new WaitForSeconds(1.8f);
        if(CurrentScene!="ClockShop") throw new Exception("Shop transition failed");
        yield return Capture("clock-shop");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--review-watchmaker")>=0)yield return ReviewWatchmaker();
        if(reviewWindow)yield return ReviewWindow();
        yield return WalkTo(new Vector3(0,0,-0.1f));
        var notice=GameObject.Find("RepairNotice");player.Aim(notice.transform.position);yield return null;
        if(!TryInteract()||!inspected||inspected.evidenceId!="E07") throw new Exception("Notice cannot be inspected normally");
        var sharedClocks=FindObjectsByType<ClockDial>(FindObjectsSortMode.None);
        foreach(var dial in sharedClocks) {
            float expected=-((Minutes+(dial.streetClock?30:0))%60)*6;
            if(Mathf.Abs(Mathf.DeltaAngle(dial.minuteHand.localEulerAngles.z,expected))>.5f)throw new Exception("Clock hand does not match shared time");
            if(dial.minuteHand.GetComponentInChildren<Renderer>().gameObject.isStatic)throw new Exception("Moving clock hands cannot be static batched");
        }
        yield return Capture("evidence");CloseInspect();journal=true;yield return Capture("journal");journal=false;
        yield return WalkTo(new Vector3(0,0,-2.1f));
        player.Aim(new Vector3(0,1.1f,-3.85f));yield return null;
        if(!TryInteract()) throw new Exception("Return door interaction failed");
        yield return new WaitForSeconds(1.8f);
        if(CurrentScene!="Town"||!state.entries.Exists(e=>e.id=="E07")) throw new Exception("Round trip lost evidence");
        if(Vector3.Distance(player.transform.position,new Vector3(state.returnX,state.returnY,state.returnZ))>0.25f) throw new Exception("Wrong exterior return point");
        Save();yield return Capture("return-town");
        File.WriteAllText(Path.Combine(captures,"review-results.json"),JsonUtility.ToJson(new ReviewResult{
            evidenceRecorded=true,doorRoundTrip=true,characterControllerWalk=true,companionSilent=Silent,
            renderedFrames=6,motionFrames=motionFrames,verification="Scripted movement through the actual controller and raycast interaction; not a blind human playthrough"
        },true));
        Debug.Log("SLICE_REVIEW_PASS");Application.Quit();
    }
    IEnumerator VerifySaved() {
        yield return new WaitForSeconds(1);
        if(CurrentScene!="Town"||!state.entries.Exists(e=>e.id=="E07")) throw new Exception("Disk relaunch did not restore state");
        if(Mathf.Abs(player.transform.position.x-state.returnX)>0.25f) throw new Exception("Disk relaunch position mismatch");
        yield return Capture("reloaded-town");
        File.WriteAllText(Path.Combine(captures,"save-relaunch.txt"),"PASS: fresh process restored E07, Town scene and return position.\n");
        Debug.Log("SLICE_SAVE_RELAUNCH_PASS");Application.Quit();
    }
    IEnumerator ReviewStreet() {
        var town=GameObject.Find("Town geometry").transform;
        int porches=0,posts=0;
        foreach(var porch in town.GetComponentsInChildren<Transform>()) {
            if(porch.name!="Porch")continue;
            var canopy=porch.Find("Awning").GetComponent<Renderer>().bounds;
            var beam=porch.Find("Canopy support beam").GetComponent<Renderer>().bounds;
            if(beam.max.y<canopy.min.y-.001f)throw new Exception("Canopy beam is disconnected: "+porch.parent.name);
            foreach(Transform child in porch) {
                if(child.name!="Shop post")continue;
                var post=child.GetComponent<Renderer>().bounds;
                var joint=new Vector3(post.center.x,(post.max.y+beam.min.y)/2,post.center.z);
                if(post.max.y<beam.min.y-.001f||!beam.Contains(joint))throw new Exception("Canopy post is disconnected: "+porch.parent.name);
                posts++;
            }
            porches++;
        }
        if(porches!=3||posts!=6)throw new Exception("Unexpected number of street canopy supports");
        var bench=town.Find("Bench");
        var surfaces=town.Find("Street surfaces");
        var streetLayout=surfaces.GetComponent<StreetSurfaceLayout>();
        var sidewalk=surfaces.Find("West sidewalk").GetComponent<Renderer>().bounds;
        var renderers=bench.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        // The mesh widens south of the square. Measure the path at the bench,
        // rather than using the bounds of that wider, distant portion.
        float walkway=-streetLayout.FootwayInnerEdge(bench.position.z)-bench.GetComponent<BoxCollider>().bounds.max.x;
        if(walkway<.8f||Mathf.Abs(bounds.min.y-sidewalk.max.y)>.015f)throw new Exception("Bench blocks the footpath or does not rest on it");
        if(Vector3.Dot(bench.Find("Seat facing").forward,Vector3.right)<.99f)throw new Exception("Bench faces away from the square");
        yield return WalkTo(new Vector3(0,0,5.8f));
        yield return WalkTo(new Vector3(-5,0,7.5f));
        player.Aim(bench.position+Vector3.up*.5f);yield return new WaitForSeconds(.3f);yield return Capture("bench-placement");
        // Walk the remaining sidewalk beside the full length of the bench.
        yield return WalkTo(new Vector3(-6.65f,0,6.4f));
        yield return WalkTo(new Vector3(-6.65f,0,8.6f));
        yield return WalkTo(new Vector3(-5,0,8.6f));
        yield return WalkTo(new Vector3(0,0,1));
        player.Aim(new Vector3(5.72f,1.7f,1));yield return new WaitForSeconds(.3f);yield return Capture("canopy-front");
        yield return WalkTo(new Vector3(2.9f,0,4.4f));
        player.Aim(new Vector3(4.42f,2.88f,4.75f));yield return new WaitForSeconds(.3f);yield return Capture("canopy-joint");
        yield return WalkTo(new Vector3(0,0,1));
        File.WriteAllText(Path.Combine(captures,"street-results.json"),JsonUtility.ToJson(new StreetResult{
            connectedPorches=porches,connectedPosts=posts,sidewalkClearance=walkway,benchGrounded=true,benchFacesSquare=true,
            sidewalkWalkPassed=true,verification="Actual rendered player-height views, native renderer bounds and CharacterController walk along the bench-side footpath"
        },true));
        Debug.Log("STREET_REVIEW_PASS: connected posts="+posts+" sidewalk clearance="+walkway);
    }
    IEnumerator ReviewRoads() {
        var root=GameObject.Find("Street surfaces");
        if(!root)throw new Exception("Street surface kit missing.");
        var layout=root.GetComponent<StreetSurfaceLayout>();
        if(layout.carriageway!=6||layout.clearFootway!=2||layout.furnitureStrip!=1)
            throw new Exception("Street dimensions differ from R01 profile.");
        float Ground(float x,float z) {
            if(!Physics.Raycast(new Vector3(x,1,z),Vector3.down,out var hit,2))throw new Exception("Street has no ground: "+x+","+z);
            return hit.point.y;
        }
        float crown=Ground(0,-10),edge=Ground(2.70f,-10),footway=Ground(4.1f,-10);
        if(crown-edge<.03f||Mathf.Abs(footway-.1f)>.01f)throw new Exception("Road crown or raised footway is missing.");
        foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>()) {
            if(mesh.name.Contains("iron"))continue;
            if(!mesh.sharedMaterial.GetTexture("_BaseMap")||!mesh.sharedMaterial.GetTexture("_BumpMap")||!mesh.sharedMaterial.GetTexture("_MetallicGlossMap"))
                throw new Exception("Street surface material lacks authored maps: "+mesh.name);
        }
        player.Aim(new Vector3(0,.1f,8));yield return new WaitForSeconds(.3f);yield return Capture("road-approach");
        yield return WalkTo(new Vector3(-2.15f,0,-6));
        player.Aim(new Vector3(-3.05f,-.015f,-5.6f));yield return new WaitForSeconds(.3f);yield return Capture("kerb-and-drain");
        yield return WalkTo(new Vector3(-4.1f,0,-6));
        if(Mathf.Abs(player.transform.position.y-footway)>.09f)throw new Exception("Controller cannot step onto footway.");
        player.Aim(new Vector3(-4.1f,.5f,3));yield return new WaitForSeconds(.3f);yield return Capture("west-footway");
        yield return WalkTo(new Vector3(-4.1f,0,0));
        player.Aim(new Vector3(0,3.1f,10));yield return new WaitForSeconds(.3f);yield return Capture("street-lamps");
        yield return WalkTo(new Vector3(4.1f,0,0));
        yield return WalkTo(new Vector3(4.1f,0,-6));
        player.Aim(new Vector3(5.1f,.3f,-.5f));yield return new WaitForSeconds(.3f);yield return Capture("east-footway");
        yield return WalkTo(new Vector3(0,0,-5));
        File.WriteAllText(Path.Combine(captures,"road-results.json"),JsonUtility.ToJson(new RoadResult{
            carriageway=layout.carriageway,clearFootway=layout.clearFootway,furnitureStrip=layout.furnitureStrip,
            crownHeight=crown,edgeHeight=edge,footwayHeight=footway,actualControllerKerbCrossing=true,bothFootwaysWalked=true,
            verification="Native visible Windows player, original dusk lighting; actual CharacterController movement and ground raycasts, automatic input. R01 profile shown in legacy visual slice, not the full fourth-map town."
        },true));
        Debug.Log("ROAD_REVIEW_PASS: crowned road, raised footways, grounded surfaces and real controller crossings");
    }
    [Serializable] class RoadResult {
        public float carriageway,clearFootway,furnitureStrip,crownHeight,edgeHeight,footwayHeight;
        public bool actualControllerKerbCrossing,bothFootwaysWalked;
        public string verification;
    }
    IEnumerator ReviewWindow() {
        var street=GameObject.Find("Window street geometry");
        var building=FindFirstObjectByType<BuildingInstance>();
        var layout=building.layout;
        var frame=building.Opening("front-left").gameObject;
        var rightFrame=building.Opening("front-right");
        int interiorWindows=0;
        foreach(var marker in building.GetComponentsInChildren<BuildingOpeningMarker>())if(marker.kind==OpeningKind.Window)interiorWindows++;
        if(exteriorWindowCount!=2||interiorWindows!=exteriorWindowCount)throw new Exception("Interior/exterior window counts differ");
        if(!street||!frame||GameObject.Find("Dusk seen through window"))throw new Exception("Real window geometry missing or image still present");
        if(street.GetComponentsInChildren<Interactable>().Length!=0)throw new Exception("Exterior preview must not duplicate clue interactions");
        var tower=street.transform.Find("Town clock");
        var station=street.transform.Find("Station");
        if(!tower||!station||Vector3.Distance(frame.transform.position,station.position)<10)throw new Exception("Window street has no real depth");
        if(Vector3.Distance(tower.position,layout.ToInterior(new Vector3(0,0,10)))>.01f)throw new Exception("Window street is not aligned with the town");
        yield return WalkTo(new Vector3(0,0,-.6f));
        player.Aim(layout.WallCentre(BuildingWall.Front)+Vector3.up*1.7f);yield return new WaitForSeconds(.3f);yield return Capture("layout-interior");
        yield return WalkTo(new Vector3(-.4f,0,-.8f));
        player.Aim(new Vector3(-2,1.95f,-4.4f));yield return new WaitForSeconds(.3f);yield return Capture("window-room");
        yield return WalkTo(new Vector3(-1.35f,0,-3));
        player.Aim(new Vector3(-4.8f,2.25f,-11));yield return new WaitForSeconds(.3f);
        var nearPoint=frame.transform.position;
        var farPoint=tower.TransformPoint(new Vector3(0,6.35f,0));
        var nearFirst=player.eye.WorldToViewportPoint(nearPoint);
        var farFirst=player.eye.WorldToViewportPoint(farPoint);
        yield return Capture("window-right");
        recordingWindow=true;StartCoroutine(RecordWindow());
        yield return new WaitForSeconds(.45f);
        // Keep orientation fixed: differences in near/far displacement are camera parallax.
        yield return WalkTo(new Vector3(-2.25f,0,-3),.18f);
        yield return new WaitForSeconds(.45f);
        recordingWindow=false;yield return new WaitForSeconds(.3f);
        var nearSecond=player.eye.WorldToViewportPoint(nearPoint);
        var farSecond=player.eye.WorldToViewportPoint(farPoint);
        float parallax=Mathf.Abs((nearSecond.x-farSecond.x)-(nearFirst.x-farFirst.x));
        if(parallax<.08f)throw new Exception("Near window and distant clock tower lack expected parallax: "+parallax);
        yield return Capture("window-left");
        // The wall hole admits the view; the sill/glass still keeps the controller inside.
        player.scriptedDirection=Vector3.back;yield return new WaitForSeconds(.8f);player.scriptedDirection=Vector3.zero;
        if(player.transform.position.z < -3.76f||player.transform.position.z > -3.3f)throw new Exception("Window collision does not stop the player at the sill");
        var eye=player.eye.transform.position;
        if(Physics.Raycast(eye,(frame.transform.position-eye).normalized,Vector3.Distance(eye,frame.transform.position)))throw new Exception("Opaque wall still blocks the aperture");
        player.Aim(farPoint);yield return new WaitForSeconds(.3f);yield return Capture("window-near");
        yield return WalkTo(new Vector3(-2.25f,0,-3));
        yield return WalkTo(new Vector3(rightFrame.transform.position.x,0,-3));
        player.Aim(rightFrame.transform.position-rightFrame.transform.forward*.5f);
        yield return new WaitForSeconds(.3f);yield return Capture("window-second");
        player.scriptedDirection=Vector3.back;yield return new WaitForSeconds(.8f);player.scriptedDirection=Vector3.zero;
        if(player.transform.position.z < -3.76f||player.transform.position.z > -3.3f)throw new Exception("Second window collision does not contain the player");
        var rightEye=player.eye.transform.position;
        if(Physics.Raycast(rightEye,(rightFrame.transform.position-rightEye).normalized,Vector3.Distance(rightEye,rightFrame.transform.position)))throw new Exception("Opaque wall blocks the second aperture");
        yield return Capture("window-second-near");
        File.WriteAllText(Path.Combine(captures,"window-results.json"),JsonUtility.ToJson(new WindowResult{
            realTownMeshes=true,wallApertureClear=true,playerContained=true,duplicateCluesRemoved=true,
            exteriorWindows=exteriorWindowCount,interiorWindows=interiorWindows,secondApertureClear=true,secondWindowContained=true,
            relativeViewportParallax=parallax,motionFrames=windowFrames,
            verification="Player-height native rendering, fixed-orientation lateral CharacterController movement, real town alignment and physical window collision"
        },true));
        Debug.Log("WINDOW_REVIEW_PASS: relative viewport parallax="+parallax);
        yield return WalkTo(new Vector3(rightFrame.transform.position.x,0,-3));
    }
    IEnumerator RecordWindow() {
        string folder=Path.Combine(captures,"motion");Directory.CreateDirectory(folder);
        while(recordingWindow) {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"window"+(windowFrames++).ToString("D4")+".png"));
            yield return new WaitForSeconds(.05f);
        }
    }
    IEnumerator ReviewLighting(string[] args) {
        bool noMain=Array.IndexOf(args,"--lighting-no-main")>=0;
        bool noLocal=Array.IndexOf(args,"--lighting-no-local")>=0;
        bool noAO=Array.IndexOf(args,"--lighting-no-ao")>=0;
        foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(light.type==LightType.Directional?noMain:noLocal)light.shadows=LightShadows.None;
        if(noAO&&reviewAmbientOcclusion)reviewAmbientOcclusion.SetActive(false);
        float side=lightingRight?-1:1;
        player.Aim(new Vector3(-2*side,1.95f,-4.4f));yield return new WaitForSeconds(.5f);
        lightingYaw=player.transform.eulerAngles.y;lightingPitch=player.pitch;lightingStart=Time.time;
        recordingLighting=true;StartCoroutine(RecordLighting());
        yield return new WaitForSeconds(.45f);
        for(int i=0;i<2;i++) {
            yield return WalkTo(new Vector3(-1.2f*side,0,-1.4f),.35f);
            yield return WalkTo(new Vector3(-.4f*side,0,-.8f),.35f);
        }
        yield return new WaitForSeconds(.45f);recordingLighting=false;
        yield return new WaitForSeconds(.2f);
        File.WriteAllText(Path.Combine(captures,"lighting-results.json"),JsonUtility.ToJson(new LightingResult{
            mainShadowsDisabled=noMain,localShadowsDisabled=noLocal,ambientOcclusionDisabled=noAO,rightWindow=lightingRight,
            renderedFrames=lightingFrames,duration=Time.time-lightingStart,samples=lightingSamples,
            verification="Native player frames during actual CharacterController walking, head bob and camera rotation; projected matte surface colour samples"
        },true));
        Debug.Log("LIGHTING_REVIEW_PASS: frames="+lightingFrames);Application.Quit();
    }
    IEnumerator RecordLighting() {
        string folder=Path.Combine(captures,"frames");Directory.CreateDirectory(folder);
        // Sample the matte lower door panel on its actual face. A point in front
        // of the patterned glazing changes sampled UVs as the camera moves.
        var door=FindFirstObjectByType<BuildingInstance>().Opening("front-door");
        var doorSurface=door.transform.position+door.transform.forward*.082f;
        doorSurface.y=.8f;
        var points=new[]{new Vector3(-3.049f,1.85f,-3.98f),new Vector3(-.82f,1.8f,-3.89f),doorSurface,new Vector3(-2,3.06f,-3.89f)};
        if(lightingRight)for(int i=0;i<points.Length;i++)points[i].x=-points[i].x;
        var names=new[]{"window-return","front-plaster","door-panel","upper-plaster"};
        while(recordingLighting) {
            float age=Time.time-lightingStart;
            player.transform.rotation=Quaternion.Euler(0,lightingYaw+Mathf.Sin(age*6)*3,0);
            player.pitch=lightingPitch+Mathf.Sin(age*5)*1.5f;
            yield return new WaitForEndOfFrame();
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            for(int i=0;i<points.Length;i++) {
                var p=player.eye.WorldToViewportPoint(points[i]);
                if(p.z<=0||p.x<.02f||p.x>.98f||p.y<.02f||p.y>.98f)continue;
                Color colour=Color.black;int x=Mathf.RoundToInt(p.x*(frame.width-1)),y=Mathf.RoundToInt(p.y*(frame.height-1));
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)colour+=frame.GetPixel(x+dx,y+dy)/9;
                lightingSamples.Add(new LightingSample{frame=lightingFrames,time=age,surface=names[i],r=colour.r,g=colour.g,b=colour.b});
            }
            File.WriteAllBytes(Path.Combine(folder,"frame"+(lightingFrames++).ToString("D4")+".png"),frame.EncodeToPNG());
            Destroy(frame);yield return new WaitForSeconds(.04f);
        }
    }
    IEnumerator RecordWalk() {
        string folder=Path.Combine(captures,"motion");Directory.CreateDirectory(folder);
        while(recordingWalk) {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"frame"+(motionFrames++).ToString("D4")+".png"));
            yield return new WaitForSeconds(.2f);
        }
    }
    [Serializable] class ReviewResult {public bool evidenceRecorded,doorRoundTrip,characterControllerWalk,companionSilent; public int renderedFrames,motionFrames; public string verification;}
    [Serializable] class StreetResult {public int connectedPorches,connectedPosts;public float sidewalkClearance;public bool benchGrounded,benchFacesSquare,sidewalkWalkPassed;public string verification;}
    [Serializable] class WindowResult {public bool realTownMeshes,wallApertureClear,playerContained,duplicateCluesRemoved,secondApertureClear,secondWindowContained;public float relativeViewportParallax;public int motionFrames,exteriorWindows,interiorWindows;public string verification;}
    [Serializable] class LightingSample {public int frame;public float time;public string surface;public float r,g,b;}
    [Serializable] class LightingResult {public bool mainShadowsDisabled,localShadowsDisabled,ambientOcclusionDisabled,rightWindow;public int renderedFrames;public float duration;public List<LightingSample> samples;public string verification;}
}
