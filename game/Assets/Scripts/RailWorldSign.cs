using UnityEngine;
public sealed class RailWorldSign : MonoBehaviour {
    public Font font;
    public Material material;
    public TextMesh lettering;
    public Collider surface;
    public Vector2 faceSize;
    public float inset=.055f;
    public float faceGap=.002f;
    bool needsFit=true;
    void OnEnable(){Font.textureRebuilt+=AtlasChanged;AtlasChanged(font);}
    void OnDisable(){Font.textureRebuilt-=AtlasChanged;}
    void AtlasChanged(Font updated){if(updated==font&&material){material.mainTexture=font.material.mainTexture;needsFit=true;}}
    void LateUpdate(){if(needsFit)Fit();}
    public bool Fit() {
        if(!lettering)return false;
        var bounds=lettering.GetComponent<MeshRenderer>().localBounds;
        if(bounds.size.x<.0001f||bounds.size.y<.0001f)return false;
        float scale=Mathf.Min((faceSize.x-2*inset)/bounds.size.x,(faceSize.y-2*inset)/bounds.size.y);
        if(scale<=0)return false;
        lettering.transform.localScale=Vector3.one*scale;
        lettering.transform.localPosition=-bounds.center*scale-Vector3.forward*faceGap;
        needsFit=false;return true;
    }
    public bool Validate(out string problem) {
        problem="";
        if(!surface||!lettering||!Fit()){problem="missing physical face or lettering bounds";return false;}
        var bounds=lettering.GetComponent<MeshRenderer>().localBounds;
        foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y}) {
            var world=lettering.transform.TransformPoint(new Vector3(x,y,bounds.center.z));
            var local=transform.InverseTransformPoint(world);
            if(Mathf.Abs(local.x)>faceSize.x/2-inset+.001f||Mathf.Abs(local.y)>faceSize.y/2-inset+.001f) {
                problem="lettering exceeds the inset face";return false;
            }
            bool touched=surface.Raycast(new Ray(world,transform.forward),out var hit,.012f);
            if(!touched||Mathf.Abs(hit.distance-faceGap)>.001f) {
                problem="lettering is detached from its physical face; surface="+surface.name+" corner="+local+" world="+world+" hit="+touched+" gap="+hit.distance+" size="+faceSize+" scale="+transform.lossyScale;return false;
            }
        }
        return true;
    }
}
