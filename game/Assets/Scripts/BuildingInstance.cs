using UnityEngine;

public class BuildingInstance : MonoBehaviour {
    public BuildingLayout layout;
    public bool interior;
    public BuildingOpeningMarker Opening(string id) {
        foreach(var marker in GetComponentsInChildren<BuildingOpeningMarker>())
            if(marker.openingId==id)return marker;
        throw new System.InvalidOperationException("Missing built opening: "+id);
    }
}
