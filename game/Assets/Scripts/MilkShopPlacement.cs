using UnityEngine;

// Shared physical placement markers for the exterior and copied window street.
public class MilkShopPlacement : MonoBehaviour {
    public Bounds visualBounds;
    public Vector3 entrance,inside,outside,northPassageStart,northPassageEnd;
    public float minimumBuildingGap;
}
