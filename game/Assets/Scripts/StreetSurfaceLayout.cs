using UnityEngine;

// Dimensions of the road kit shown in the legacy visual slice. The complete
// town's placement remains in docs/maps/town-current/plan-data.json.
public class StreetSurfaceLayout : MonoBehaviour {
    public string profileId="R01";
    public float carriageway=6,clearFootway=2,furnitureStrip=1;
    public float footwayHeight=.1f,plazaStart=6.5f,plazaHalfWidth=6.225f;
    public float FootwayInnerEdge(float z) => z>=plazaStart?plazaHalfWidth:carriageway/2+.22f;
}
