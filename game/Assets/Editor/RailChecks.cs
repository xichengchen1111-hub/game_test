using System;
using System.Linq;
using UnityEngine;

public static class RailChecks {
    static void Need(bool condition,string why){if(!condition)throw new InvalidOperationException("Rail geometry: "+why);}
    static bool Solid(GameObject root,Vector3 point,Vector3 direction,float distance) {
        point=root.transform.TransformPoint(point);direction=root.transform.TransformDirection(direction);
        return Physics.RaycastAll(point,direction,distance).Any(h=>h.collider.transform.IsChildOf(root.transform)&&!(h.collider.GetComponent<RailSeat>()));
    }
    public static void ValidateTrain(GameObject root) {
        Physics.SyncTransforms();var train=root.GetComponent<RailTrain>();
        Need(train&&train.rideSeat&&train.standPoint&&train.boardingPoint,"shared seat/standing/boarding markers missing");
        var east=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="EastMarker");
        Need(Vector3.Distance(root.transform.InverseTransformPoint(east.position),new Vector3(3,0,0))<.01f,"orientation/scale drift");
        Need(train.doors.Length==8,"two coaches require four physical vestibule doors each");
        Need(train.wheels.Length>=8,"independent steam/coach wheel pivots missing");
        foreach(float offset in new[]{0f,-16f})foreach(float x in new[]{-4.5f,-2.7f,-.9f,.9f,2.7f,4.5f})foreach(int side in new[]{-1,1}) {
            Need(!Solid(root,new Vector3(offset+x,2.7f,side*1.7f),new Vector3(0,0,-side),.4f),"opaque or missing aperture at coach "+offset+" window "+x+" side "+side);
        }
        foreach(float offset in new[]{0f,-16f})foreach(int side in new[]{-1,1}) {
            Need(Solid(root,new Vector3(offset+3.6f,2.7f,side*1.7f),new Vector3(0,0,-side),.4f),"unintended extra aperture / missing window post");
            Need(Solid(root,new Vector3(offset,1.5f,side*1.7f),new Vector3(0,0,-side),.4f),"lower carriage wall missing");
            Need(Solid(root,new Vector3(offset,1.35f,0),Vector3.down,.4f),"shared coach floor missing");
            Need(!Solid(root,new Vector3(offset,1.08f,0),Vector3.up,2.15f),"aisle/headroom blocked");
            Need(Solid(root,new Vector3(offset,3.25f,0),Vector3.up,.7f),"actual coach ceiling missing");
        }
        Need(root.GetComponentsInChildren<Light>().All(l=>l.shadows==UnityEngine.LightShadows.None),"train adds shadow lights");
        var renderers=root.GetComponentsInChildren<Renderer>();Need(renderers.Length<350,"excess renderer count");
        Need(renderers.SelectMany(r=>r.sharedMaterials).All(m=>m),"missing material");
        Debug.Log("RAIL_AUTHORING_PASS: original editable train; matched actual coach windows/floor/aisle, eight doors and independent wheels");
    }
    public static void ValidateStation(GameObject root) {
        Physics.SyncTransforms();
        foreach(float x in new[]{-12f,-8f,-4f,4f,8f,12f})foreach(int side in new[]{-1,1})
            Need(!Solid(root,new Vector3(x,2.7f,side*6.3f),new Vector3(0,0,-side),.6f),"station window aperture blocked "+x);
        foreach(int side in new[]{-1,1})
            Need(!Solid(root,new Vector3(0,1.7f,side*6.3f),new Vector3(0,0,-side),.6f),"station central entrance blocked");
        foreach(int side in new[]{-1,1})foreach(float x in new[]{-12f,-8f,-4f,4f,8f,12f}) {
            Need(!Solid(root,new Vector3(x,3.85f,side*6.3f),new Vector3(0,0,-side),.6f),"arch crown has a backing");
            Need(Solid(root,new Vector3(x+.98f,3.85f,side*6.3f),new Vector3(0,0,-side),.6f),"arched opening upper corner has no shared masonry");
        }
        Need(Solid(root,new Vector3(0,.3f,0),Vector3.down,.4f),"hall physical floor missing");
        Debug.Log("STATION_AUTHORING_PASS: actual front/rear openings and continuous floor");
    }
}
