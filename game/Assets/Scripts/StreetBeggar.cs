using UnityEngine;

// A man begging (user, 2026-10-15: not everyone on the street is walking). January 1931: one man in
// six out of work, transients off the freight trains; he sits on a crate by the station with his cap
// upturned in front of him, watches people pass and holds out a hand to whoever comes close. E puts a
// few coins in the cap. Nothing is kept: no money, no saved state, no testimony.
public sealed class StreetBeggar : Interactable {
    StreetLife life;NpcHands hands;PeriodCharacterPerformance performance;
    float cooldown,handOut;Transform crate,cap;
    public void Setup(StreetLife owner) {
        life=owner;title="乞讨的人";source="街头";body="";evidenceId="";
        performance=GetComponentInChildren<PeriodCharacterPerformance>();if(performance){performance.gestures=false;performance.speaker="乞讨的人";}
        hands=GetComponent<NpcHands>();if(!hands)hands=gameObject.AddComponent<NpcHands>();
        // One figure on a pitch: it keeps animating out of view so the pose on top never builds up.
        foreach(var a in GetComponentsInChildren<Animator>(true))a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        // Seated as the hospital's people on a bench are (HospitalBedPose), on a low crate.
        var animator=GetComponentInChildren<Animator>();var visual=animator?animator.transform:transform.GetChild(0);var bones=visual.GetComponentsInChildren<Transform>(true);
        Transform Bone(string n){foreach(var b in bones)if(b.name==n)return b;return null;}
        var frame=new GameObject("Body frame").transform;frame.SetParent(transform,false);
        var pose=gameObject.AddComponent<HospitalBedPose>();pose.mode=HospitalBedPose.Mode.SittingOnSeat;pose.frame=frame;pose.performance=performance;
        pose.leftLeg=Bone("Left_UpperLeg");pose.rightLeg=Bone("Right_UpperLeg");pose.leftShin=Bone("Left_LowerLeg");pose.rightShin=Bone("Right_LowerLeg");
        pose.leftUpperArm=Bone("Left_UpperArm");pose.rightUpperArm=Bone("Right_UpperArm");pose.leftLowerArm=Bone("Left_LowerArm");pose.rightLowerArm=Bone("Right_LowerArm");
        pose.neck=Bone("Neck");pose.head=Bone("Head");pose.hips=Bone("Hips");
        pose.shoulderForward=18;pose.elbowBend=80;pose.watchDistance=7;pose.maxYaw=55;pose.turnRate=2.2f;
        const float seat=.38f;
        if(pose.hips){visual.position+=Vector3.up*(transform.position.y+seat+.1f-pose.hips.position.y);pose.hipsRest=pose.hips.localPosition;}
        foreach(var c in GetComponentsInChildren<Collider>(true))Destroy(c);
        var box=gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.62f,-.15f);box.size=new Vector3(.62f,1.25f,.85f);
        // The crate he sits on, behind his knees, and the cap on the ground before him.
        crate=Place("Crate",new Vector3(0,0,.08f),new Vector3(.5f,seat,.42f),new Color(.36f,.27f,.17f));
        cap=Place("Cap",new Vector3(.05f,0,-.62f),new Vector3(.24f,.07f,.24f),new Color(.18f,.17f,.16f));
    }
    Transform Place(string name,Vector3 local,Vector3 size,Color colour) {
        var prop=life.Prop(name);
        if(!prop){prop=GameObject.CreatePrimitive(name=="Cap"?PrimitiveType.Cylinder:PrimitiveType.Cube);Destroy(prop.GetComponent<Collider>());prop.GetComponent<Renderer>().sharedMaterial=life.Material(colour);prop.transform.localScale=name=="Cap"?new Vector3(size.x,size.y*.5f,size.z):size;}
        prop.name=name;prop.transform.SetParent(transform,false);prop.transform.localPosition=local+Vector3.up*(name=="Cap"?size.y*.5f:size.y*.5f);prop.transform.localRotation=Quaternion.identity;
        foreach(var t in prop.GetComponentsInChildren<Transform>(true))t.gameObject.layer=gameObject.layer;
        foreach(var r in prop.GetComponentsInChildren<Renderer>(true))r.renderingLayerMask=1;
        return prop.transform;
    }
    public void Tick(float dt,Vector3 player) {
        cooldown-=dt;handOut-=dt;
        var to=player-transform.position;to.y=0;var facing=-transform.forward;facing.y=0;
        if(cooldown<=0&&to.magnitude<3.2f&&Vector3.Dot(to.normalized,facing.normalized)>.2f) {
            cooldown=75;handOut=3.5f;
            var palm=transform.position+facing.normalized*.42f+Vector3.Cross(Vector3.up,facing.normalized)*.12f+Vector3.up*.78f;
            hands.Reach(true,palm,.7f);hands.Orient(true,facing.normalized,Vector3.down);hands.SetGrip(true,NpcHands.Grip.Open);
            if(performance)performance.Speak("行行好吧，先生");
            DemoApp.Instance?.OpeningReminder("乞讨的人：行行好吧，先生……");
        }
        if(handOut<=0&&hands.Weight(true)>0)hands.Release(true,.8f);
    }
    public override string ActionText=>"施舍 · "+title;
    public override void Use() {
        if(performance)performance.Speak("谢谢您，先生");
        DemoApp.Instance?.OpeningReminder("你在他的帽子里放了几枚硬币。  乞讨的人：谢谢您，先生，好人有好报。");
        cooldown=120;
    }
}
