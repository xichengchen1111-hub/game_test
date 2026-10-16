using UnityEngine;

// An organ grinder on the square (user, 2026-10-15: street performers too). Barrel organs were still
// heard in 1931 (New York banned them only in 1935–36). He stands behind the box on its pole, turns
// the crank with his right hand — the hand really follows the handle round (NpcHands) — and rests his
// left on the lid; an upturned cap lies in front. The tune is original, made for the game
// (tools/opening/make_street_sounds.py). E drops a coin in the cap. Nothing is kept.
public sealed class StreetOrgan : Interactable {
    StreetLife life;NpcHands hands;PeriodCharacterPerformance performance;
    Transform organ,crank;AudioSource music;float angle,thanks;
    const float AxleHeight=1.1f,Radius=.08f,Turns=1.05f;
    public void Setup(StreetLife owner) {
        life=owner;title="摇风琴的艺人";source="街头";body="";evidenceId="";
        performance=GetComponentInChildren<PeriodCharacterPerformance>();if(performance){performance.gestures=false;performance.speaker="艺人";}
        hands=GetComponent<NpcHands>();if(!hands)hands=gameObject.AddComponent<NpcHands>();
        // One figure on a pitch: it keeps animating out of view so the pose on top never builds up.
        foreach(var a in GetComponentsInChildren<Animator>(true))a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var c in GetComponentsInChildren<Collider>(true))Destroy(c);
        var box=gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.85f,-.15f);box.size=new Vector3(.7f,1.7f,.95f);
        organ=life.Prop("BarrelOrgan")?.transform;
        if(!organ)organ=BuildOrgan();
        organ.name="Barrel organ";organ.SetParent(transform,false);
        // In front of him: the model faces local -Z.
        organ.localPosition=new Vector3(0,0,-.25f);organ.localRotation=Quaternion.identity;
        crank=organ.Find("Crank");
        foreach(var t in organ.GetComponentsInChildren<Transform>(true))t.gameObject.layer=gameObject.layer;
        foreach(var r in organ.GetComponentsInChildren<Renderer>(true))r.renderingLayerMask=1;
        var cap=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(cap.GetComponent<Collider>());cap.name="Cap";cap.transform.SetParent(transform,false);
        cap.transform.localPosition=new Vector3(-.2f,.035f,-.75f);cap.transform.localScale=new Vector3(.24f,.035f,.24f);cap.GetComponent<Renderer>().sharedMaterial=life.Material(new Color(.18f,.16f,.14f));cap.layer=gameObject.layer;
        var clip=Resources.Load<AudioClip>("StreetSounds/BarrelOrgan");
        if(clip) {
            var g=new GameObject("Organ music");g.transform.SetParent(organ,false);g.transform.localPosition=new Vector3(0,AxleHeight,0);
            music=g.AddComponent<AudioSource>();music.clip=clip;music.loop=true;music.spatialBlend=1;music.rolloffMode=AudioRolloffMode.Linear;
            music.minDistance=3;music.maxDistance=36;music.volume=.6f;music.dopplerLevel=0;music.Play();
        }
    }
    // The box on its single leg, painted front, crank on the right side (the player's left when facing him).
    Transform BuildOrgan() {
        var root=new GameObject("Barrel organ").transform;
        void Part(string n,PrimitiveType shape,Vector3 pos,Vector3 size,Color c,Transform parent=null){var o=GameObject.CreatePrimitive(shape);o.name=n;Destroy(o.GetComponent<Collider>());o.transform.SetParent(parent?parent:root,false);o.transform.localPosition=pos;o.transform.localScale=size;o.GetComponent<Renderer>().sharedMaterial=life.Material(c);}
        Part("Pole",PrimitiveType.Cylinder,new Vector3(0,.45f,0),new Vector3(.05f,.45f,.05f),new Color(.3f,.2f,.12f));
        Part("Case",PrimitiveType.Cube,new Vector3(0,AxleHeight,0),new Vector3(.54f,.42f,.3f),new Color(.36f,.16f,.1f));
        Part("Front panel",PrimitiveType.Cube,new Vector3(0,AxleHeight,-.152f),new Vector3(.44f,.32f,.01f),new Color(.62f,.48f,.25f));
        Part("Lid",PrimitiveType.Cube,new Vector3(0,AxleHeight+.22f,0),new Vector3(.58f,.03f,.33f),new Color(.28f,.13f,.08f));
        var crankRoot=new GameObject("Crank").transform;crankRoot.SetParent(root,false);crankRoot.localPosition=new Vector3(-.3f,AxleHeight,0);
        Part("Crank arm",PrimitiveType.Cube,new Vector3(-.02f,Radius/2,0),new Vector3(.015f,Radius,.02f),new Color(.4f,.38f,.33f),crankRoot);
        Part("Crank handle",PrimitiveType.Cylinder,new Vector3(-.06f,Radius,0),new Vector3(.03f,.04f,.03f),new Color(.22f,.15f,.09f),crankRoot);
        crankRoot.Find("Crank handle").localRotation=Quaternion.Euler(0,0,90);
        return root;
    }
    public void Tick(float dt) {
        if(!organ)return;
        var app=DemoApp.Instance;bool frozen=app&&app.GameTimePaused;
        if(music){if(frozen&&music.isPlaying)music.Pause();else if(!frozen&&!music.isPlaying)music.UnPause();}
        if(frozen)return;
        angle=Mathf.Repeat(angle+dt*Turns*360,360);thanks-=dt;
        // The crank turns about the box's side axis; the handle is at its end.
        if(crank)crank.localRotation=Quaternion.AngleAxis(angle,Vector3.right);
        var handle=crank?crank.TransformPoint(new Vector3(-.06f,Radius,0)):organ.position+Vector3.up*AxleHeight;
        // The crank is on his right (the model faces -Z, so his right is local -X).
        hands.Reach(true,handle,.3f);
        hands.SetGrip(true,NpcHands.Grip.Hold);
        var lid=organ.TransformPoint(new Vector3(.14f,AxleHeight+.25f,.05f));
        hands.Reach(false,lid,.4f);hands.SetGrip(false,NpcHands.Grip.Open);
    }
    public override string ActionText=>"给钱 · "+title;
    public override void Use() {
        if(thanks>0)return;thanks=30;
        if(performance)performance.Speak("谢谢您，先生");
        DemoApp.Instance?.OpeningReminder("你往他的帽子里丢了一枚硬币。  艺人：谢谢您，先生！祝您今天顺顺当当。");
    }
}
