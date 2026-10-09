using UnityEngine;

public partial class DemoApp {
    const int FullMapRevision=3;
    void MigrateTownPosition(ref Vector3 position,bool legacyRoom) {
        if(state.worldRevision>=FullMapRevision||!clockSaveLoadedAtStartup||legacyRoom){state.worldRevision=FullMapRevision;return;}
        var old=position;
        var shopLocal=Quaternion.Inverse(Quaternion.Euler(0,90,0))*(old-new Vector3(9.72f,0,1));
        var milkLocal=Quaternion.Inverse(Quaternion.Euler(0,90,0))*(old-new Vector3(11.45f,-.04f,-7.35f))/.9f;
        bool oldTower=Mathf.Abs(old.x)<3&&Mathf.Abs(old.z-10)<3&&old.y>=-.3f;
        if(oldTower)position=old-new Vector3(0,0,10);
        else if(Mathf.Abs(shopLocal.x)<3.7f&&Mathf.Abs(shopLocal.z)<4.1f){position=seamlessShop.layout.ToTown(shopLocal);state.yaw+=seamlessShop.layout.townYaw-90;}
        else if(Mathf.Abs(milkLocal.x)<3.6f&&Mathf.Abs(milkLocal.z)<2.9f){position=milkShopRoot.TransformPoint(milkLocal);state.yaw+=90;}
        else position=TownPlan.Spawn;
        state.worldRevision=FullMapRevision;
        state.returnX=TownPlan.Spawn.x;state.returnY=TownPlan.Spawn.y;state.returnZ=TownPlan.Spawn.z;
        state.x=position.x;state.y=position.y;state.z=position.z;
        Debug.Log("FULL_MAP_SAVE_MIGRATED: "+old+" -> "+position+"; evidence, fixtures, dialogue and elapsed clock preserved");
    }
}
