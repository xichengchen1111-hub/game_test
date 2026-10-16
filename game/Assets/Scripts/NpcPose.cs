using UnityEngine;

// Everyday postures layered over a standing character's own idle/walk/talk clips, at runtime:
// sitting on a chair or bench, reading a folder held in both hands, writing at a desk, and
// turning the head toward the player who stands near. Same body-frame conventions as
// HospitalBedPose (up runs from hips to head, the chest faces the root's -Z), so it does not
// depend on how a rig orients its bones. Each posture fades in and out, so sitting down and
// standing up are visible movements rather than a cut. Explicit pause freezes it.
public sealed class NpcPose : MonoBehaviour {
    public enum Activity { Stand, Sit, Read, SitRead, SitWrite }
    public Activity activity;
    // World height of the seat surface while sitting.
    public float seatHeight;
    public bool lookAtPlayer=true;
    public float watchDistance=4f,maxYaw=55,maxPitchUp=18,maxPitchDown=20;
    // Something the hands are busy with (a lock, a handle, the briefcase): the eyes go there instead.
    public bool hasFocus;public Vector3 focus;
    // 0..1 posture weights, eased toward the activity.
    public float Sit=>sit;
    public bool Settled=>Mathf.Abs(sit-(Seated(activity)?1:0))<.02f&&Mathf.Abs(read-(Reading(activity)?1:0))<.02f;
    Transform visual,hips,leftLeg,rightLeg,leftShin,rightShin,leftUpperArm,rightUpperArm,leftLowerArm,rightLowerArm,neck,head;
    Vector3 visualRest,hipsRest;float standingHips;
    float sit,read,write,yaw,pitch,clock,flip,nextFlip=3,glanceClock=3,glanceYaw,glancePitch,talk;
    PeriodCharacterPerformance performance;
    Transform folder,page;
    bool ready;
    static bool Seated(Activity a)=>a==Activity.Sit||a==Activity.SitRead||a==Activity.SitWrite;
    static bool Reading(Activity a)=>a==Activity.Read||a==Activity.SitRead;
    void Awake()=>Resolve();
    void Resolve() {
        if(ready)return;
        performance=GetComponentInChildren<PeriodCharacterPerformance>();
        if(transform.childCount==0)return;
        visual=performance?performance.transform:transform.GetChild(0);
        foreach(var t in visual.GetComponentsInChildren<Transform>(true)) {
            switch(t.name) {
                case "Hips":hips=t;break;case "Left_UpperLeg":leftLeg=t;break;case "Right_UpperLeg":rightLeg=t;break;
                case "Left_LowerLeg":leftShin=t;break;case "Right_LowerLeg":rightShin=t;break;
                case "Left_UpperArm":leftUpperArm=t;break;case "Right_UpperArm":rightUpperArm=t;break;
                case "Left_LowerArm":leftLowerArm=t;break;case "Right_LowerArm":rightLowerArm=t;break;
                case "Neck":neck=t;break;case "Head":head=t;break;
            }
        }
        if(!hips)return;
        visualRest=visual.localPosition;hipsRest=hips.localPosition;
        standingHips=transform.InverseTransformPoint(hips.position).y;
        ready=true;
    }
    // A plain manila folder held in both hands while reading; a page turns now and then.
    void EnsureFolder() {
        if(folder)return;
        float h=Mathf.Max(1.2f,standingHips/.53f);
        folder=new GameObject("Reading folder").transform;folder.SetParent(transform,false);
        var cover=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(cover.GetComponent<Collider>());
        cover.transform.SetParent(folder,false);cover.transform.localScale=new Vector3(.25f,.006f,.32f);
        var paper=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(paper.GetComponent<Collider>());
        page=new GameObject("Page hinge").transform;page.SetParent(folder,false);page.localPosition=new Vector3(-.12f,.006f,0);
        paper.transform.SetParent(page,false);paper.transform.localPosition=new Vector3(.115f,0,0);paper.transform.localScale=new Vector3(.23f,.003f,.3f);
        var shader=Shader.Find("Universal Render Pipeline/Lit");
        var coverMat=new Material(shader){color=new Color(.62f,.5f,.32f)};var paperMat=new Material(shader){color=new Color(.86f,.83f,.74f)};
        cover.GetComponent<Renderer>().sharedMaterial=coverMat;paper.GetComponent<Renderer>().sharedMaterial=paperMat;
        uint layers=0;foreach(var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))layers|=r.renderingLayerMask;
        foreach(var r in folder.GetComponentsInChildren<Renderer>()){r.renderingLayerMask|=layers==0?1u:layers;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        folderHeight=h;
    }
    float folderHeight;
    void LateUpdate() {
        Resolve();if(!ready)return;
        var app=DemoApp.Instance;bool frozen=app&&app.NPCAnimationPaused;
        float dt=frozen?0:Time.deltaTime;
        sit=Mathf.MoveTowards(sit,Seated(activity)?1:0,dt/.75f);
        read=Mathf.MoveTowards(read,Reading(activity)?1:0,dt/.6f);
        write=Mathf.MoveTowards(write,activity==Activity.SitWrite?1:0,dt/.6f);
        clock+=dt;
        float s=Mathf.SmoothStep(0,1,sit),r=Mathf.SmoothStep(0,1,read),w=Mathf.SmoothStep(0,1,write);
        // Lower the body onto the seat: the hips keep their authored place (the idle sways them).
        float drop=Mathf.Min(0,(seatHeight-transform.position.y)+.1f-standingHips);
        visual.localPosition=visualRest+Vector3.up*(drop*s);
        if(s>0)hips.localPosition=Vector3.Lerp(hips.localPosition,hipsRest,s);
        var side=transform.right;var fwd=transform.forward;var up=transform.up;
        if(s>0){Bend(leftLeg,side,88*s);Bend(rightLeg,side,88*s);Bend(leftShin,side,-86*s);Bend(rightShin,side,-86*s);}
        // Arms: resting on the lap when seated, raised to hold a folder, or forward onto a desk.
        float armIn=10*Mathf.Max(s,r),shoulder=Mathf.Lerp(12*s,38,r),elbow=Mathf.Lerp(70*s,78,r),inward=14*r;
        shoulder=Mathf.Lerp(shoulder,26,w);elbow=Mathf.Lerp(elbow,72,w);inward=Mathf.Lerp(inward,8,w);
        if(armIn>0){Bend(leftUpperArm,fwd,-armIn);Bend(rightUpperArm,fwd,armIn);}
        if(shoulder>0){Bend(leftUpperArm,side,shoulder);Bend(rightUpperArm,side,shoulder);}
        if(inward>0){Bend(leftUpperArm,up,inward);Bend(rightUpperArm,up,-inward);}
        if(elbow>0){Bend(leftLowerArm,side,elbow);Bend(rightLowerArm,side,elbow);}
        // Writing: the right hand moves along the line in short strokes.
        if(w>0)Bend(rightLowerArm,up,w*(5*Mathf.Sin(clock*6.3f)+2*Mathf.Sin(clock*15.1f)));
        float headDown=22*r+26*w;
        if(headDown>0){Bend(neck,side,-headDown*.4f);Bend(head,side,-headDown*.6f);}
        UpdateFolder(r,dt);
        // Turning the head toward a nearby visitor; less while absorbed in reading or writing.
        float targetYaw=0,targetPitch=0;bool watching=false;
        if(hasFocus&&head) {
            var to=transform.InverseTransformPoint(focus)-transform.InverseTransformPoint(head.position);
            watching=true;
            targetYaw=Mathf.Clamp(Mathf.Atan2(-to.x,-to.z)*Mathf.Rad2Deg,-maxYaw,maxYaw);
            targetPitch=Mathf.Clamp(Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg,-maxPitchDown-8,maxPitchUp);
        }
        else if(lookAtPlayer&&app&&app.player&&head) {
            var to=transform.InverseTransformPoint(app.player.eye.transform.position)-transform.InverseTransformPoint(head.position);
            if(to.magnitude<watchDistance&&to.z<.4f) {
                watching=true;
                targetYaw=Mathf.Clamp(Mathf.Atan2(-to.x,-to.z)*Mathf.Rad2Deg,-maxYaw,maxYaw);
                targetPitch=Mathf.Clamp(Mathf.Atan2(to.y,new Vector2(to.x,to.z).magnitude)*Mathf.Rad2Deg,-maxPitchDown,maxPitchUp);
                float absorbed=Mathf.Max(r,w)*(performance&&performance.IsSpeaking?0:.65f);
                targetYaw*=1-absorbed;targetPitch*=1-absorbed;
            }
        }
        // Left alone, people do not hold their heads still: now and then a glance round the room, or
        // up from the page, then back to what they were doing.
        glanceClock-=dt;
        if(glanceClock<=0) {
            bool looking=glanceYaw!=0||glancePitch!=0;
            if(looking){glanceYaw=0;glancePitch=0;glanceClock=Random.Range(4f,9f);}
            else {
                bool absorbed=Reading(activity)||activity==Activity.SitWrite;
                glanceYaw=Random.Range(-1f,1f)*(absorbed?18:40);glancePitch=absorbed?Random.Range(14f,24f):Random.Range(-6f,8f);
                glanceClock=Random.Range(1.1f,2.4f);
            }
        }
        if(!watching&&head){targetYaw=glanceYaw;targetPitch=glancePitch;}
        float k=1-Mathf.Exp(-dt*(hasFocus?4.5f:3f));yaw=Mathf.Lerp(yaw,targetYaw,k);pitch=Mathf.Lerp(pitch,targetPitch,k);
        // Speaking without the hands (seated, or past the line's gestures): small nods carry the words.
        talk=Mathf.MoveTowards(talk,performance&&performance.IsSpeaking?1:0,dt*2.5f);
        float nod=talk*(2.4f*Mathf.Sin(clock*2.9f)+1.1f*Mathf.Sin(clock*5.3f+1.3f));
        Turn(neck,yaw*.34f,pitch*.34f+nod*.4f);Turn(head,yaw*.66f,pitch*.66f+nod*.6f);
    }
    void UpdateFolder(float r,float dt) {
        if(r<=.01f){if(folder)folder.gameObject.SetActive(false);return;}
        EnsureFolder();folder.gameObject.SetActive(true);
        // Between the hands, in front of the chest, tilted toward the eyes; seated it follows the body down.
        float y=standingHips*1.42f+visual.localPosition.y-visualRest.y;
        folder.localPosition=new Vector3(0,y,-.38f-.04f*(1-r));
        folder.localRotation=Quaternion.Euler(38,0,0);
        folder.localScale=Vector3.one*Mathf.Lerp(.6f,1,r);
        nextFlip-=dt;
        if(nextFlip<=0){flip=1;nextFlip=Random.Range(3.5f,7f);}
        if(flip>0){flip=Mathf.Max(0,flip-dt/.7f);page.localRotation=Quaternion.Euler(0,0,Mathf.Sin((1-flip)*Mathf.PI)*-150);}
        else page.localRotation=Quaternion.identity;
    }
    static void Bend(Transform bone,Vector3 axis,float degrees){if(bone)bone.rotation=Quaternion.AngleAxis(degrees,axis)*bone.rotation;}
    void Turn(Transform bone,float y,float p){if(bone)bone.rotation=Quaternion.AngleAxis(y,transform.up)*Quaternion.AngleAxis(p,transform.right)*bone.rotation;}
}
