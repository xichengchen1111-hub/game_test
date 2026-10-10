using System;
using UnityEngine;

public static class RailTicketChecks {
    static void Need(bool ok,string why){if(!ok)throw new InvalidOperationException("Ticket stock: "+why);}
    public static void ValidateStock(GameObject piece,bool punched) {
        var collider=piece.GetComponent<MeshCollider>();Need(collider&&collider.sharedMesh,"physical paper mesh missing");
        Physics.SyncTransforms();
        foreach(float y in new[]{.016f,.0325f,.049f}) {
            var centre=new Vector3(.049f,.04f,.0325f-y);
            var ray=new Ray(piece.transform.TransformPoint(centre),-piece.transform.up);
            bool hit=collider.Raycast(ray,out _,.12f*piece.transform.lossyScale.y);
            Need(hit!=punched,punched?"a punch hole has an opaque backing":"carried original has a punch hole");
        }
        Need(collider.Raycast(new Ray(piece.transform.TransformPoint(new Vector3(0,.04f,0)),-piece.transform.up),out _,.12f*piece.transform.lossyScale.y),"paper body absent");
    }
    public static void Validate(GameObject bin) {
        var pieces=bin.GetComponentsInChildren<RailTicketPiece>();Need(pieces.Length>=5,"waste bin has no inspectable stock");
        foreach(var piece in pieces){Need(piece.punched,"unpunched ticket in waste bin");ValidateStock(piece.gameObject,true);}
        Debug.Log("TICKET_PERFORATION_PASS: all waste tickets have three physical voids");
    }
    public static void ValidateImages(Texture2D intact,Texture2D punched) {
        Need(intact&&punched,"viewer images absent");
        foreach(float y in new[]{16f,32.5f,49f}) {
            Need(intact.GetPixelBilinear(104f/110,1-y/65).a>.99f,"carried ticket art is perforated");
            Need(punched.GetPixelBilinear(104f/110,1-y/65).a<.01f,"sample viewer shows painted dots instead of holes");
        }
    }
}
