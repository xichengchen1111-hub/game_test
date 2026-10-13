using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Exercise the shared submenu handlers and real flight controller without
// loading scenes or touching the player's save or audio preferences.
public class DeveloperMenuTests {
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    readonly List<GameObject> objects=new List<GameObject>();
    Component app,motor;
    string folder,save;
    CursorLockMode previousLock;
    bool previousCursor;
    static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);
    static object Call(object target,string method,params object[] args)=>target.GetType().GetMethod(method,Instance).Invoke(target,args);
    static void Field(object target,string name,object value)=>target.GetType().GetField(name,Instance).SetValue(target,value);
    static bool FieldFlag(object target,string name)=>(bool)target.GetType().GetField(name,Instance).GetValue(target);
    static bool Flag(object target,string name)=>(bool)target.GetType().GetProperty(name,Instance).GetValue(target);
    GameObject Object(string name){var value=new GameObject(name);value.SetActive(false);objects.Add(value);return value;}
    [SetUp] public void Setup() {
        Assert.IsNull(T("DemoApp").GetField("Instance").GetValue(null),"Tests must not replace a running game");
        previousLock=Cursor.lockState;previousCursor=Cursor.visible;
        folder=Path.Combine(Path.GetTempPath(),"wrong-date-developer-menu-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);save=Path.Combine(folder,"test-save.json");
        app=Object("Inactive developer-menu app").AddComponent(T("DemoApp"));
        var body=Object("Developer-menu player");body.AddComponent<CharacterController>();motor=body.AddComponent(T("PlayerMotor"));
        var eye=Object("Developer-menu eye").AddComponent<Camera>();eye.enabled=false;eye.transform.SetParent(body.transform,false);Field(motor,"eye",eye);Call(motor,"Awake");
        Field(app,"player",motor);Field(app,"opening",false);Field(app,"sceneReady",true);Field(app,"savePath",save);
        app.GetType().GetProperty("CurrentScene").GetSetMethod(true).Invoke(app,new object[]{"OtherTown"});
        Call(motor,"Place",new Vector3(0,.12f,-38),0f);
        var state=app.GetType().GetField("state").GetValue(app);Field(state,"openingStage",2);
    }
    [TearDown] public void Cleanup() {
        foreach(var item in objects)if(item)UnityEngine.Object.DestroyImmediate(item);objects.Clear();
        if(folder!=null&&Directory.Exists(folder))Directory.Delete(folder,true);
        Cursor.lockState=previousLock;Cursor.visible=previousCursor;
    }
    [Test] public void PauseSubmenuBackReturnsToPausedGameWithoutMovingOrSaving() {
        Field(app,"paused",true);var position=motor.transform.position;Call(app,"OpenCreatorMenu");
        Assert.IsTrue(FieldFlag(app,"creatorMenu"));Assert.IsTrue(Flag(app,"GameTimePaused"));Assert.IsTrue(Flag(app,"NPCAnimationPaused"));
        Assert.IsTrue(Flag(app,"MovementLocked"));Assert.IsFalse(Flag(app,"LookAllowed"));
        Call(app,"CloseCreatorMenu");Assert.IsFalse(FieldFlag(app,"creatorMenu"));Assert.IsTrue(FieldFlag(app,"paused"));
        Assert.AreEqual(position,motor.transform.position);Assert.IsFalse(File.Exists(save));
    }
    [Test] public void ShortcutSubmenuPausesAndBackResumesItsOriginalGameplayState() {
        Assert.IsFalse(Flag(app,"GameTimePaused"));Call(app,"OpenCreatorMenu");Assert.IsTrue(Flag(app,"GameTimePaused"));
        Call(app,"CloseCreatorMenu");Assert.IsFalse(FieldFlag(app,"paused"));Assert.IsFalse(Flag(app,"Blocked"));Assert.IsTrue(Flag(app,"LookAllowed"));
        Call(app,"CloseCreatorMenu");Assert.IsFalse(FieldFlag(app,"paused"),"A duplicate close must not toggle pause");
    }
    [Test] public void FlightAndTeleportResumeFromPauseAndKeepSafeSavePosition() {
        var launch=motor.transform.position;Field(app,"paused",true);Call(app,"OpenCreatorMenu");Call(app,"ToggleCreatorFlight");
        Assert.IsTrue(Flag(motor,"CreatorFlying"));Assert.IsFalse(FieldFlag(app,"paused"));Assert.IsFalse(FieldFlag(app,"creatorMenu"));
        Assert.IsFalse(motor.GetComponent<CharacterController>().enabled);Assert.IsTrue(File.Exists(save));
        motor.transform.position+=Vector3.up*9;Call(app,"Save");
        var state=app.GetType().GetField("state").GetValue(app);
        Assert.That((float)state.GetType().GetField("y").GetValue(state),Is.EqualTo(launch.y).Within(.001f));
        Field(app,"paused",true);Call(app,"OpenCreatorMenu");var target=new Vector3(2,.12f,-35);Call(app,"CreatorTeleport",target,90f);
        Assert.IsFalse(Flag(motor,"CreatorFlying"));Assert.IsTrue(motor.GetComponent<CharacterController>().enabled);Assert.AreEqual(target,motor.transform.position);
        Assert.IsFalse(Flag(app,"GameTimePaused"));Assert.IsFalse(FieldFlag(app,"creatorMenu"));Assert.AreEqual(2,state.GetType().GetField("openingStage").GetValue(state));
    }
    [TestCase("restingSeat","FurnitureSeat")]
    [TestCase("seatedTrain","RailTrain")]
    [TestCase("seatedCar","HotelCar")]
    [TestCase("openingRideActive",null)]
    [TestCase("transitioning",null)]
    public void SeatedOrTravellingStatesRejectMenuFlightAndTeleport(string field,string type) {
        Field(app,field,type==null?(object)true:Object("Occupied seat").AddComponent(T(type)));Field(app,"paused",true);
        var position=motor.transform.position;Call(app,"OpenCreatorMenu");Call(app,"ToggleCreatorFlight");Call(app,"CreatorTeleport",new Vector3(2,.12f,-35),90f);
        Assert.IsFalse(FieldFlag(app,"creatorMenu"));Assert.IsFalse(Flag(motor,"CreatorFlying"));Assert.AreEqual(position,motor.transform.position);
        Assert.IsTrue(FieldFlag(app,"paused"));Assert.IsFalse(File.Exists(save));
    }
}
