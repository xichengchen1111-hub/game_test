using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
public static class HousePlacementChecks {
    public static void Check(GameObject house){
        var all=house.GetComponentsInChildren<BoxCollider>();var failures=new List<string>();int comparisons=0;
        foreach(var c in house.GetComponentsInChildren<HouseContainer>()){
            foreach(var d in c.drawers){var start=d.localPosition;var rotation=d.localRotation;
                foreach(float t in new[]{0f,.25f,.5f,.75f,1f}){
                    int index=Array.IndexOf(c.drawers,d);if(c.safe||c.hinged)d.localRotation=Quaternion.AngleAxis((c.hinged&&index==1?-85:85)*t,Vector3.up)*rotation;else d.localPosition=start+c.offset*t;
                    Physics.SyncTransforms();
                    foreach(var moving in d.GetComponentsInChildren<BoxCollider>().Where(b=>!b.isTrigger))foreach(var other in all){
                        if(other.isTrigger||other==moving||c.drawers.Any(part=>other.transform.IsChildOf(part)))continue;
                        if(!moving.bounds.Intersects(other.bounds))continue;comparisons++;
                        if(Physics.ComputePenetration(moving,moving.transform.position,moving.transform.rotation,other,other.transform.position,other.transform.rotation,out var direction,out float depth)&&depth>.018f)
                            failures.Add(c.fixtureId+" opening="+t+" hits "+other.name+" depth="+depth.ToString("F3"));
                    }
                }
                d.localPosition=start;d.localRotation=rotation;
            }
            foreach(var paper in c.contents){if(!paper.GetComponent<Collider>()||paper.GetComponentsInChildren<MeshFilter>().Any(f=>!f.sharedMesh))failures.Add("Missing physical paper "+paper.evidenceId);}
        }
        Physics.SyncTransforms();
        Directory.CreateDirectory("../builds/house-review");File.WriteAllText("../builds/house-review/storage-sweep.txt","Comparisons="+comparisons+"\n"+string.Join("\n",failures.Distinct()));
        if(failures.Count>0)throw new Exception("HOUSE_STORAGE_SWEEP: "+string.Join("; ",failures.Distinct().Take(15)));
        Debug.Log("HOUSE_STORAGE_SWEEP_PASS: "+comparisons+" intersecting broad-phase pairs checked, 5 poses per moving part.");
    }
}
