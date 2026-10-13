using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public class HouseGeometryTests {
 GameObject house; object plan; MethodInfo validate;
 [SetUp] public void Load(){house=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/House1931/House1931.prefab"));var asm=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="Assembly-CSharp-Editor");plan=JsonUtility.FromJson(File.ReadAllText("Assets/Art/House1931/Layout.json"),asm.GetType("HomePlan"));validate=asm.GetType("BuildingLayoutChecks").GetMethod("ValidateHouse");}
 [TearDown]public void Clean(){if(house)UnityEngine.Object.DestroyImmediate(house);}
 void Validate()=>validate.Invoke(null,new[]{house,plan});
 MeshRenderer Glass=>house.GetComponentsInChildren<MeshRenderer>().First(m=>m.name.Contains(" glazing"));
 [Test]public void SharedHouseAccepted()=>Assert.DoesNotThrow(Validate);
 [Test]public void MissingWindowRejected(){UnityEngine.Object.DestroyImmediate(Glass.gameObject);Assert.Throws<TargetInvocationException>(Validate);}
 [Test]public void ExtraWindowRejected(){UnityEngine.Object.Instantiate(Glass.gameObject,house.transform);Assert.Throws<TargetInvocationException>(Validate);}
 [Test]public void DisplacedWindowRejected(){Glass.transform.position+=Vector3.right*.2f;Assert.Throws<TargetInvocationException>(Validate);}
 [Test]public void ResizedWindowRejected(){Glass.transform.localScale=Vector3.one*1.1f;Assert.Throws<TargetInvocationException>(Validate);}
 [Test]public void DisplacedDoorRejected(){house.GetComponentsInChildren<Transform>().Single(t=>t.name=="Mechanism east").position+=Vector3.right*.3f;Assert.Throws<TargetInvocationException>(Validate);}
 [Test]public void DoorKnobsKeepHumanScale(){foreach(var r in house.GetComponentsInChildren<MeshRenderer>().Where(m=>m.name.StartsWith("Brass door knob")))Assert.Less(r.bounds.size.magnitude,.18f);}
 Vector3 Recover(Vector3 p){var h=house.GetComponent("HouseInstance");return (Vector3)h.GetType().GetMethod("ResolveLoadedFurnitureOverlap").Invoke(h,new object[]{p});}
 bool Clear(Vector3 p){var h=house.GetComponent("HouseInstance");return (bool)h.GetType().GetMethod("ClearStandingPosition").Invoke(h,new object[]{p});}
 [Test]public void LoadedOldBedPositionRecoversInSameBedroom(){
  var old=house.transform.TransformPoint(new Vector3(-2.023315f,3.38f,-4.816635f));Physics.SyncTransforms();
  Assert.IsFalse(Clear(old));var recovered=Recover(old);
  Assert.IsTrue(Clear(recovered));Assert.Less(Vector3.Distance(old,recovered),1);Assert.AreEqual(old.y,recovered.y);
  var p=house.transform.InverseTransformPoint(recovered);Assert.Less(p.x,1);Assert.Less(p.z,-1);Assert.Greater(p.z,-6);
 }
 [Test]public void UnobstructedHouseAndOutdoorSavesRemainExact(){
  foreach(var p in new[]{new Vector3(-5,3.38f,-2),new Vector3(12,.18f,0),new Vector3(4,1.76f,4.8f)}){var world=house.transform.TransformPoint(p);Assert.AreEqual(world,Recover(world));}
 }
 [Test]public void PorchRoofStaysOutsideStairwell(){
  var r=house.GetComponentsInChildren<MeshRenderer>().Single(m=>m.name=="East porch canopy");
  Assert.GreaterOrEqual(house.transform.InverseTransformPoint(r.bounds.min).x,8.11f);
 }
 [Test]public void StairOutletHasContinuousPhysicalFloor(){
  Physics.SyncTransforms();foreach(float z in new[]{.94f,1.02f,1.09f,1.16f,1.26f}){
   var p=house.transform.TransformPoint(new Vector3(6.4f,3.55f,z));
   Assert.IsTrue(Physics.Raycast(p,Vector3.down,out var hit,.3f),"Open slit at stair outlet "+z);
   Assert.AreEqual(3.36f,house.transform.InverseTransformPoint(hit.point).y,.008f);
  }
 }

}
