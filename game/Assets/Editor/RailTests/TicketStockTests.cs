using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class TicketStockTests {
    GameObject stock;
    MethodInfo validation;
    [SetUp] public void Prepare() {
        stock=new GameObject("Ticket stock test");stock.AddComponent<MeshCollider>();
        validation=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("RailTicketChecks")).First(t=>t!=null).GetMethod("ValidateStock");
    }
    [TearDown] public void Clean(){UnityEngine.Object.DestroyImmediate(stock);}
    void Check(bool punched)=>validation.Invoke(null,new object[]{stock,punched});
    void Mesh(string name){stock.GetComponent<MeshCollider>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/Ticket"+name+".asset");}
    [Test] public void WasteStockHasPhysicalPunchVoids(){Mesh("Punched");Assert.DoesNotThrow(()=>Check(true));}
    [Test] public void CarriedStockPreservesThePaperAtAllPunchPositions(){Mesh("Intact");Assert.DoesNotThrow(()=>Check(false));}
    [Test] public void PaintedDotsCannotReplacePhysicalPunchVoids(){Mesh("Intact");Assert.Throws<TargetInvocationException>(()=>Check(true));}
    [Test] public void PunchedStockCannotBeUsedForTheCarriedOriginal(){Mesh("Punched");Assert.Throws<TargetInvocationException>(()=>Check(false));}
}
