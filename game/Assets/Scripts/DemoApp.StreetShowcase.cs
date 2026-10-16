using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Presentation run of the streets and the briefcase (user, 2026-10-15): the morning traffic at the
// signal, people waiting and crossing, children on their way to school and a woman walking her
// terrier (the street cast, 2026-10-16), the organ grinder and the beggar, rain and umbrellas, a STOP
// corner, a car waiting for the player, the empty night street, then B and Tab. Recorded by
// ShowcaseRecorder with --review --fresh --showcase-street; its own save. Nothing here is a test.
public partial class DemoApp {
    string showcaseCaption;
    void DrawShowcaseCaption() {
        if(string.IsNullOrEmpty(showcaseCaption))return;
        var style=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=20};
        var r=new Rect(W/2-420,22,840,40);Box(r,new Color(.02f,.03f,.04f,.62f));Label(r,showcaseCaption,style);
    }
    IEnumerator ShowCaption(string caption,double minutes) {
        StreetTime(minutes);showcaseCaption=caption;ShowMark(caption);
        yield return ShowHold(.2f);
    }
    // Walking among people: the player cannot pass through anyone, so step round whoever is close ahead,
    // and sidestep when held up, as a player steering with the mouse would.
    IEnumerator ShowStroll(Vector3 target,float speedFactor=.55f,float pitch=3) {
        float until=Time.time+60,still=0;var last=player.transform.position;
        while(Time.time<until) {
            Vector3 d=target-player.transform.position;d.y=0;if(d.magnitude<.3f)break;
            var dir=d.normalized;var right=Vector3.Cross(Vector3.up,dir);
            var life=StreetLife.Current;
            if(life)foreach(var w in life.Walkers) {
                var rel=w.transform.position-player.transform.position;rel.y=0;float ahead=Vector3.Dot(rel,dir),side=Vector3.Dot(rel,right);
                if(ahead>0&&ahead<1.8f&&Mathf.Abs(side)<.85f)dir=(dir+right*(side>0?-1:1)*.9f).normalized;
            }
            if(still>1)dir=(dir+right*(Mathf.Repeat(Time.time,4)<2?1.3f:-1.3f)).normalized;
            player.scriptedDirection=dir*speedFactor;
            float yaw=Mathf.MoveTowardsAngle(player.transform.localEulerAngles.y,Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,Time.deltaTime*120);
            ShowLook(yaw,Mathf.MoveTowards(player.pitch,pitch,Time.deltaTime*40));
            var moved=player.transform.position-last;moved.y=0;still=moved.magnitude<Time.deltaTime*.15f?still+Time.deltaTime:0;last=player.transform.position;
            yield return null;
        }
        player.scriptedDirection=Vector3.zero;
    }
    // ---- The street cast: each is sent along a straight run of footway, so the shot stays on one pavement ----
    // The straighter way along the stretch someone stands on (no crossing, no doorway, under ~30° at a node,
    // at least 12 m), the way nearest `prefer` (or the way they face): they are turned that way if need be
    // and sent along it. Returns the run's nodes, or null when neither way is long enough.
    List<StreetNet.WalkNode> ShowSendAlong(StreetLife life,StreetWalker who,Vector3 prefer,float metres) {
        var at=who.transform.position;StreetNet.WalkEdge edge=null;float best=1.2f;
        foreach(var e in life.Net.walkEdges) {
            if(e.crosses!=null||e.a.door!=null||e.b.door!=null)continue;
            var ab=Flat(e.b.point-e.a.point);float len=ab.magnitude;if(len<.5f||Mathf.Abs(Vector3.Dot(ab/len,who.Facing))<.9f)continue;
            float t=Mathf.Clamp01(Vector3.Dot(Flat(at-e.a.point),ab)/(len*len));float d=Flat(e.a.point+ab*t-at).magnitude;
            if(d<best){best=d;edge=e;}
        }
        if(edge==null)return null;
        List<StreetNet.WalkNode> Run(StreetNet.WalkNode node,Vector3 dir) {
            var nodes=new List<StreetNet.WalkNode>{node};float length=Flat(node.point-at).magnitude;
            while(length<metres) {
                StreetNet.WalkEdge next=null;float straightest=.86f;
                foreach(var e in node.edges) {
                    var o=e.Other(node);var step=Flat(o.point-node.point);
                    if(e.crosses!=null||o.door!=null||nodes.Contains(o)||step.sqrMagnitude<.01f)continue;
                    float c=Vector3.Dot(step.normalized,dir);if(c>straightest){straightest=c;next=e;}
                }
                if(next==null)break;
                node=next.Other(node);var s=Flat(node.point-nodes[nodes.Count-1].point);length+=s.magnitude;dir=s.normalized;nodes.Add(node);
            }
            return length>=12?nodes:null;
        }
        var along=Flat(edge.b.point-edge.a.point).normalized;
        var ways=new[]{(dir:along,nodes:Run(edge.b,along)),(dir:-along,nodes:Run(edge.a,-along))}.Where(w=>w.nodes!=null).ToList();
        if(ways.Count==0)return null;
        var way=ways.OrderByDescending(w=>Vector3.Dot(w.dir,prefer.sqrMagnitude>0?prefer:who.Facing)).First();
        if(Vector3.Dot(way.dir,who.Facing)<0)who.Begin(life,new System.Random(1931),at,way.dir);
        ShowFollowRun(who,way.nodes);
        return way.nodes;
    }
    // From where they stand onto the run's first node ahead of them, as StreetLife joins a fresh walker to its route.
    static void ShowFollowRun(StreetWalker who,List<StreetNet.WalkNode> nodes) {
        var at=who.transform.position;var ahead=nodes.SkipWhile(n=>Vector3.Dot(Flat(n.point-at),who.Facing)<.3f).ToList();
        if(ahead.Count==0)return;
        var start=new StreetNet.WalkNode{point=at};
        start.edges.Add(new StreetNet.WalkEdge{a=start,b=ahead[0],length=Vector3.Distance(at,ahead[0].point),width=1.4f});
        var route=new List<StreetNet.WalkNode>{start};route.AddRange(ahead);who.Follow(route,ahead[ahead.Count-1]);
    }
    readonly Collider[] showOverlap=new Collider[8];
    // Room for a standing player at p: the static world only (people and cars are left to the controller).
    bool ShowClear(Vector3 p) {
        int n=Physics.OverlapCapsuleNonAlloc(p+Vector3.up*.45f,p+Vector3.up*1.6f,.3f,showOverlap,~((1<<StreetLife.Layer)|(1<<2)),QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)if(!showOverlap[i].transform.IsChildOf(player.transform))return false;
        return true;
    }
    // How far out beside someone (up to `most`, at least 1.3 m) the player can walk: off the carriageway, clear of
    // walls, posts and benches, and with nothing solid (a bench back) between the eye and `subject`.
    float ShowRoom(StreetLife life,Vector3 o,Vector3 forward,Vector3 outward,float most,float ahead,Vector3 subject) {
        for(float d=most;d>=1.3f;d-=.2f){var p=o+outward*d+forward*ahead;if(!life.OnCarriage(p)&&ShowClear(p)&&ShowInSight(p+Vector3.up*1.6f,subject))return d;}
        return 0;
    }
    readonly RaycastHit[] showSight=new RaycastHit[8];
    bool ShowInSight(Vector3 eye,Vector3 subject) {
        var d=subject-eye;int n=Physics.RaycastNonAlloc(eye,d.normalized,showSight,d.magnitude,~((1<<StreetLife.Layer)|(1<<2)),QueryTriggerInteraction.Ignore);
        for(int i=0;i<n;i++)if(!showSight[i].collider.transform.IsChildOf(player.transform))return false;
        return true;
    }
    // At the cut to a new shot, nobody else stands right beside the people filmed (someone at their elbow
    // holds them up until they turn back another way); hidden at once, so nobody vanishes on screen.
    static void ShowClearAround(StreetLife life,float radius,IEnumerable<Vector3> around,params StreetWalker[] keep) {
        var points=around.ToList();
        foreach(var w in life.Walkers.ToList()) {
            if(w.Gone||keep.Contains(w))continue;
            if(points.Any(p=>Flat(w.transform.position-p).magnitude<radius)){w.Despawn();w.gameObject.SetActive(false);}
        }
    }
    // Children on their way to school: the boy and, a step behind him, the girl, coming along the footway
    // towards the player, who stands aside some eight metres up their way and watches them go by.
    IEnumerator ShowChildren(StreetLife life) {
        player.Place(new Vector3(-20,.12f,15.2f),90);
        var boy=life.SpawnKind(StreetDensity.Folk.Boy,player.transform.position,5,18);
        var run=boy?ShowSendAlong(life,boy,Vector3.zero,30):null;
        if(run==null){Debug.LogWarning("SHOWCASE_CAST: no straight footway for the children");if(boy)boy.Despawn();yield return ShowHold(2);yield break;}
        boy.speed=1.05f;
        var girl=life.SpawnKind(StreetDensity.Folk.Girl,boy.transform.position,1,18);bool paired=false;
        if(girl)foreach(float back in new[]{1.3f,1.6f,1.9f,-1.3f}) {
            var at=boy.transform.position-boy.Facing*back;at.y=life.GroundAt(at,boy.transform.position.y+.2f);
            if(!life.Free(at,girl))continue;
            girl.Begin(life,new System.Random(1932),at,boy.Facing);girl.speed=1.12f;ShowFollowRun(girl,run);paired=true;break;
        }
        if(girl&&!paired){girl.Despawn();girl=null;}
        // Their way as a line, and the spot aside on it some eight metres ahead of the boy.
        var line=new List<Vector3>{boy.transform.position};line.AddRange(run.Select(n=>n.point));
        Vector3 spot=boy.transform.position+boy.Facing*3+Vector3.Cross(Vector3.up,boy.Facing)*-1.3f;float want=8.5f;
        for(int i=0;i+1<line.Count;i++) {
            var seg=Flat(line[i+1]-line[i]);float len=seg.magnitude;if(len<.01f)continue;
            if(want>len){want-=len;continue;}
            var q=line[i]+seg*(want/len);var left=-Vector3.Cross(Vector3.up,seg/len);
            foreach(float side in new[]{1.05f,1.3f,.85f,1.6f}){var p=q+left*side;p.y=life.GroundAt(p,boy.transform.position.y+.2f);if(!life.OnCarriage(p)&&ShowClear(p)&&life.Free(p,null)){spot=p;break;}}
            break;
        }
        Vector3 Kids()=>(girl&&!girl.Gone?(boy.transform.position+girl.transform.position)/2:boy.transform.position)+Vector3.up*.85f;
        ShowClearAround(life,4.5f,new[]{boy.transform.position,girl?girl.transform.position:boy.transform.position,spot},boy,girl);
        player.Place(spot,0);var aim=Kids();player.Aim(aim);
        Debug.Log("SHOWCASE_CAST: children boy="+boy.transform.position+" paired="+paired+" run="+run.Count+" camera="+spot);
        for(float t=0;t<9.5f&&!boy.Gone;t+=Time.deltaTime){aim=Vector3.Lerp(aim,Kids(),1-Mathf.Exp(-Time.deltaTime*4));player.Aim(aim);yield return null;}
    }
    // A woman walking her terrier: the player walks along beside her on the dog's side (her left), a few
    // steps out and a little ahead, looking at the two of them and then down at the dog trotting on its lead.
    IEnumerator ShowDogWalker(StreetLife life) {
        // The square's footpath west of the tower (x = -9): she is sent north along it, the tower behind her
        // seen from the dog's side. She starts near z = -6 so the ten seconds end short of the bench at (-10, 8),
        // which otherwise slides into the foreground.
        var lady=life.SpawnKind(StreetDensity.Folk.DogWalker,new Vector3(-14.5f,.12f,-6),5,18);
        if(!lady||!lady.dog){Debug.LogWarning("SHOWCASE_CAST: no dog walker");yield return ShowHold(2);yield break;}
        var run=ShowSendAlong(life,lady,Vector3.forward,30);
        var dog=lady.dog;var o=lady.transform.position;var f=lady.Facing;var outward=-Vector3.Cross(Vector3.up,f);
        float most=2.6f,ahead=1.1f;
        float leftRoom=ShowRoom(life,o,f,outward,most,ahead,dog.Position+Vector3.up*.3f),rightRoom=ShowRoom(life,o,f,-outward,most,ahead,dog.Position+Vector3.up*.3f);
        float sign=leftRoom>=1.9f||leftRoom>=rightRoom?1:-1;float outBy=Mathf.Max(1.3f,sign>0?leftRoom:rightRoom);
        // The camera keeps to the same side of her even if she turns back (the side is followed in the world, not in her frame).
        outward*=sign;var at=o+outward*outBy+f*ahead;at.y=o.y;
        ShowClearAround(life,7,new[]{o,at},lady);
        player.Place(at,0);var aim=Vector3.Lerp(o,dog.Position,.4f)+Vector3.up*.8f;player.Aim(aim);
        Debug.Log("SHOWCASE_CAST: dog walker at="+o+" facing="+f+" run="+(run!=null?run.Count:0)+" side="+(sign>0?"left":"right")+" room="+leftRoom+"/"+rightRoom);
        for(float t=0;t<10&&!lady.Gone;t+=Time.deltaTime) {
            // From the two of them to the dog over the last few seconds, coming in a little closer.
            float k=Mathf.SmoothStep(0,1,(t-5.5f)/3.5f);
            o=lady.transform.position;f=lady.Facing;
            var across=-Vector3.Cross(Vector3.up,f);outward=Vector3.Dot(across,outward)>=0?across:-across;
            float room=ShowRoom(life,o,f,outward,Mathf.Lerp(most,2.1f,k),Mathf.Lerp(ahead,.6f,k),dog.Position+Vector3.up*.3f);
            if(room>0)outBy=Mathf.MoveTowards(outBy,room,Time.deltaTime*1.2f);
            var target=o+outward*outBy+f*Mathf.Lerp(ahead,.6f,k);
            var v=Flat(lady.Velocity+(target-player.transform.position)*1.6f);
            player.scriptedDirection=Vector3.ClampMagnitude(v,2.3f)/PlayerMotor.WalkSpeed;
            var look=Vector3.Lerp(Vector3.Lerp(o,dog.Position,.4f)+Vector3.up*.8f,dog.Position+Vector3.up*.32f,k);
            aim=Vector3.Lerp(aim,look,1-Mathf.Exp(-Time.deltaTime*4));player.Aim(aim);
            yield return null;
        }
        player.scriptedDirection=Vector3.zero;
    }
    IEnumerator ShowcaseStreet() {
        Application.targetFrameRate=30;QualitySettings.vSyncCount=0;dayNightReviewFrozen=false;
        state.openingRevision=System.Math.Max(1,state.openingRevision);state.openingStage=4;state.openingEscort=4;state.belongingsCollected=true;
        state.openingRideRevision=1;state.openingRideStage=5;
        foreach(var id in new[]{"E01","E03","E06","H17","W02","Y03"})if(!state.entries.Exists(e=>e.id==id))state.entries.Add(new NotebookEntry{id=id,title=id,body="",source=""});
        foreach(var t in new[]{"milkboy.delivery","milkboy.route","station-clerk.date","station-clerk.inspection","mechanic.where","mechanic.copies"})if(!state.dialogueTopics.Contains(t))state.dialogueTopics.Add(t);
        // Real titles and texts of the records, as the player would have them.
        foreach(var item in FindObjectsByType<Interactable>(FindObjectsInactive.Include,FindObjectsSortMode.None)){var e=state.entries.Find(x=>x.id==item.evidenceId);if(e!=null&&string.IsNullOrEmpty(e.body)&&!string.IsNullOrEmpty(item.body)){e.title=item.title;e.body=item.body;e.source=item.source;}}
        var milk=DialogueScript.Load("MilkboyDialogue").evidence;if(!state.entries.Exists(e=>e.id==milk.id))state.entries.Add(new NotebookEntry{id=milk.id,title=milk.title,body=milk.body,source=milk.source});
        StreetTime(8*60+25);
        yield return new WaitForSeconds(14);
        var life=StreetLife.Current;if(!life){ShowAbort("no street life");yield break;}
        Silent=false;AudioListener.volume=ShowcaseListenerVolume;
        showcase=player.eye.gameObject.AddComponent<ShowcaseRecorder>();
        showcase.Begin(Path.Combine(captures,"showcase-street"));
        // 1. Morning at the signal.
        player.Place(new Vector3(36.4f,.12f,27.4f),225);player.Aim(new Vector3(28,1.2f,18));
        yield return ShowCaption("早上 8:30 · 车站路与中街路口：车辆按信号灯停走，行人等绿灯过街",8*60+30);
        yield return ShowHold(9);yield return ShowLookAt(new Vector3(22,1.3f,22),3);yield return ShowHold(7);yield return ShowLookAt(new Vector3(30,1.2f,12),3);yield return ShowHold(6);
        // 2. Along Station Road among the morning crowd.
        showcaseCaption="沿车站路北侧人行道往西走：路人靠右走、互相让路";
        player.Place(new Vector3(24.4f,.12f,25.3f),-90);yield return ShowHold(.6f);
        yield return ShowStroll(new Vector3(-4,.12f,25.3f),.55f);
        // 2b. Children on their way to school.
        yield return ShowCaption("早上 8:40 · 上学路上的孩子",8*60+40);
        yield return ShowChildren(life);
        // 2c. A woman walking her terrier on the square.
        // 梗犬, not 㹴犬: the UI font and its system fallback have no glyph for 㹴 (it showed as a box).
        yield return ShowCaption("上午 9:30 · 遛狗的妇人和她的梗犬",9*60+30);
        yield return ShowDogWalker(life);
        // 3. The organ grinder on the square.
        yield return ShowCaption("上午 10:15 · 钟楼广场：摇手摇风琴的街头艺人",10*60+15);
        life.PlaceOrganNow();
        if(life.Organ) {
            var organ=life.Organ;var front=organ.transform.position-organ.transform.forward*4.2f;front.y=.12f;
            player.Place(front+Vector3.Cross(Vector3.up,-organ.transform.forward)*1.5f,0);yield return ShowLookAt(organ.transform.position+Vector3.up*1.15f,.5f);
            yield return ShowHold(6);yield return ShowStroll(organ.transform.position-organ.transform.forward*1.7f+Vector3.up*.12f,.4f,8);
            yield return ShowUse(organ,organ.transform.position+Vector3.up*1.2f);yield return ShowHold(4);
        }
        // 4. The beggar by the station.
        for(float until=Time.time+6;Time.time<until&&!life.Beggar;)yield return null;
        if(life.Beggar) {
            var beggar=life.Beggar;showcaseCaption="车站旁：坐在木箱上乞讨的人，有人走近就伸手";
            var start=beggar.transform.position-beggar.transform.forward*6;start.y=.12f;player.Place(start,0);
            yield return ShowLookAt(beggar.transform.position+Vector3.up*.7f,.4f);yield return ShowHold(1.5f);
            yield return ShowStroll(beggar.transform.position-beggar.transform.forward*2.2f+Vector3.up*.12f,.35f,14);yield return ShowHold(3.5f);
            yield return ShowUse(beggar,beggar.transform.position+Vector3.up*.75f);yield return ShowHold(4);
        }
        // 5. Rain: umbrellas up, the lamps of cars lit in the gloom.
        DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);
        yield return ShowCaption("11:20 下雨 · 行人撑伞、雨天人少一些",11*60+20);
        player.Place(new Vector3(-24,.12f,15.2f),90);yield return ShowHold(2.5f);
        // Meet someone under an umbrella: stand a few steps ahead of them on their way and watch them pass.
        StreetWalker under=null;
        for(float until=Time.time+6;Time.time<until&&!under;) {
            under=life.Walkers.Where(w=>w.transform.Find("Umbrella")&&w.transform.Find("Umbrella").gameObject.activeSelf&&w.Velocity.magnitude>.4f).OrderBy(w=>Vector3.Distance(w.transform.position,player.transform.position)).FirstOrDefault();
            yield return null;
        }
        if(under) {
            var meet=under.transform.position+under.Facing*6f+Vector3.Cross(Vector3.up,under.Facing)*-1.2f;meet.y=under.transform.position.y+.05f;
            player.Place(meet,0);
            for(float t=0;t<7&&under;t+=Time.deltaTime){player.Aim(under.transform.position+Vector3.up*1.4f);yield return null;}
        } else {yield return ShowLookAt(new Vector3(0,1.4f,15.6f),.3f);yield return ShowHold(5);}
        DayNightCycle.Instance.SetReviewWeather(null);
        // 6. A STOP corner: a full stop, a look, then on.
        yield return ShowCaption("下午 1:00 · 南街与中街的停车让行路口：先停稳，等主路有空当再走",13*60);
        var stopLane=life.Net.lanes.FirstOrDefault(l=>l.stopSign&&l.toJunction!=null&&Mathf.Abs(l.toJunction.centre.x-30)<.5f&&Mathf.Abs(l.toJunction.centre.z+122)<.5f&&l.dir.z<-.9f);
        // Beside its line, wherever the line stands (it was set back for turning cars, STREET-03).
        var line=stopLane!=null?stopLane.end:new Vector3(29,0,-116.65f);var look=line+new Vector3(1,1,-3.35f);
        player.Place(line+new Vector3(5.4f,.12f,4.05f),220);player.Aim(look);
        var stopper=stopLane!=null?life.SpawnCarOn(stopLane,2,4.5f):null;
        for(float t=0;t<16&&stopper&&stopper.path==stopLane;t+=Time.deltaTime){player.Aim(Vector3.Lerp(look,stopper.transform.position+Vector3.up*1.1f,.5f));yield return null;}
        yield return ShowHold(3);
        // 7. In the road: the car stops short and toots once, then goes on.
        yield return ShowCaption("站在车道上：汽车在前面停下、轻按一下喇叭，等人让开再走",13*60+5);
        AmbientCar car=null;
        for(float until=Time.time+30;Time.time<until&&!car;){car=life.AllCars.FirstOrDefault(c=>c.path is StreetNet.Lane l&&l.carriage.eastWest&&l.length-c.s>30&&c.speed>3&&Vector3.Distance(c.transform.position,player.transform.position)<120);yield return null;}
        if(car) {
            var lane=(StreetNet.Lane)car.path;var spot=lane.Point(Mathf.Min(lane.length-2,car.s+26));spot.y=.06f;
            player.Place(spot,0);player.Aim(car.transform.position+Vector3.up*1.2f);
            for(float t=0;t<9;t+=Time.deltaTime){player.Aim(car.transform.position+Vector3.up*1.2f);yield return null;}
            yield return ShowStroll(spot+Vector3.Cross(Vector3.up,lane.dir)*3.3f,.5f);
            for(float t=0;t<4;t+=Time.deltaTime){if(car)player.Aim(car.transform.position+Vector3.up*1.1f);yield return null;}
        }
        // 8. Night: the streets nearly empty.
        yield return ShowCaption("夜里 21:30 · 街上只剩零星的人和车",21*60+30);
        player.Place(new Vector3(36.4f,.12f,27.4f),225);player.Aim(new Vector3(20,1.2f,20));yield return ShowHold(10);
        // 9. The briefcase and the dossier.
        StreetTime(10*60);showcaseCaption=null;
        player.Place(new Vector3(-20,.12f,15.2f),90);yield return ShowHold(1);
        showcase.Mark("briefcase");OpenBackpack();yield return ShowHold(3);
        foreach(var step in new[]{1,1,1,1}){MovePackSelection(step,0);yield return ShowHold(.9f);}
        MovePackSelection(0,1);yield return ShowHold(1.2f);MovePackSelection(1,0);yield return ShowHold(1.2f);
        ActivatePackItem(packItems.FindIndex(p=>p.id=="E02"));yield return ShowHold(1.2f);
        for(int i=0;i<40;i++){RotateEvidence(new Vector2(0,-3));yield return null;}yield return ShowHold(1);
        ToggleEvidenceDetails();yield return ShowHold(3);CloseInspect();yield return ShowHold(1.5f);
        int paper=packItems.FindIndex(p=>p.id=="W02"||p.id=="E01");
        if(paper>=0){packSelected=paper;packFollow=true;yield return ShowHold(1.5f);ActivatePackItem(paper);yield return ShowHold(1.5f);ToggleEvidenceDetails();yield return ShowHold(4);CloseInspect();yield return ShowHold(1);}
        showcase.Mark("dossier");CloseBackpack();OpenDossier();yield return ShowHold(4);
        selectedEntry=1;housePaperScroll=Vector2.zero;yield return ShowHold(3.5f);journal=false;CursorMode();yield return ShowHold(1);
        showcase.Mark("end");yield return ShowHold(1.5f);
        showcase.End();AudioListener.volume=0;
        Debug.Log("SHOWCASE_STREET_DONE");Application.Quit();
    }
}
