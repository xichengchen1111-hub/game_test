using System;
using System.Linq;
using UnityEngine;

// Check the realised FBX rather than trusting source coordinates or a rendering alone.
public static class TicketOfficeChecks {
    [Serializable] class Contact { public string item,support; }
    [Serializable] class Contract { public float floorTop;public Contact[] contacts;public string[] grounded,fixtures; }
    public static void Validate(GameObject root,TextAsset geometry) {
        if(!geometry)throw new InvalidOperationException("Ticket office contact contract missing");
        var data=JsonUtility.FromJson<Contract>(geometry.text);
        var meshes=root.GetComponentsInChildren<MeshRenderer>();
        Bounds Bounds(string name) {
            var found=meshes.Where(m=>m.name==name).ToArray();
            if(found.Length!=1)throw new InvalidOperationException("Ticket office fixture missing/duplicated: "+name);
            return found[0].bounds;
        }
        foreach(string name in data.fixtures)Bounds(name);
        foreach(var pair in data.contacts) {
            var item=Bounds(pair.item);var support=Bounds(pair.support);
            float gap=item.min.y-support.max.y;
            bool overlap=item.max.x>support.min.x&&item.min.x<support.max.x&&item.max.z>support.min.z&&item.min.z<support.max.z;
            if(gap>.0021f||gap<-.0111f||!overlap)
                throw new InvalidOperationException("Ticket office unsupported/intersecting: "+pair.item+" on "+pair.support+" gap="+gap);
        }
        float floor=root.transform.TransformPoint(new Vector3(0,data.floorTop,0)).y;
        foreach(string name in data.grounded)
            if(Mathf.Abs(Bounds(name).min.y-floor)>.0021f)throw new InvalidOperationException("Ticket office floating above floor: "+name);
        Debug.Log("TICKET_OFFICE_SUPPORT_PASS: "+data.contacts.Length+" imported contacts, "+data.grounded.Length+" grounded fixtures");
    }
}
