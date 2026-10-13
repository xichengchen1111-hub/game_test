using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
public partial class DemoApp {
    void NeedHome(bool ok,string why){if(!ok)throw new Exception("HOME_REVIEW_FAIL: "+why);}
    void AimVisibleHomePaper(Interactable paper){
        var box=paper.GetComponent<BoxCollider>();
        // Aim at a visible part of the actual sheet, not through the drawer front at an occluded centre.
        foreach(float y in new[]{.32f,0f,-.32f})foreach(float x in new[]{0f,-.3f,.3f}){
            player.Aim(box.transform.TransformPoint(box.center+new Vector3(x*box.size.x,y*box.size.y,0)));
            if(Physics.Raycast(player.eye.transform.position,player.eye.transform.forward,out var hit,3)&&hit.collider.GetComponentInParent<Interactable>()==paper)return;
        }
        NeedHome(false,"No unobstructed paper surface "+paper.evidenceId);
    }
    IEnumerator ReviewHouse(){
        yield return new WaitForSeconds(1);
        var home=HouseInstance.Current;NeedHome(home,"Missing house");Vector3 P(float x,float y,float z)=>home.Point(x,y,z);
        var containers=home.GetComponentsInChildren<HouseContainer>();NeedHome(containers.Length==20,"20 searchable furniture groups");
        var east=home.GetComponentsInChildren<ShopMechanism>().Single(d=>d.fixtureId=="home-door-east");var west=home.GetComponentsInChildren<ShopMechanism>().Single(d=>d.fixtureId=="home-door-west");
        DayReviewTime(720);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Overcast);
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--stairs-only")>=0){yield return ReviewHouseStairs();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--placement-only")>=0){yield return ReviewHousePlacement();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--evidence-only")>=0){yield return ReviewEvidencePresentation();yield break;}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--storage-only")>=0){containers.Single(c=>c.safe).TryCode("0614");yield return new WaitForSeconds(.7f);yield return ReviewHomeStorage();Debug.Log("HOME_STORAGE_REVIEW_PASS");Application.Quit();yield break;}
        player.Place(P(20,.16f,-17),0);player.Aim(P(0,3,0));yield return Capture("01-exterior-east");
        player.Place(P(10,.16f,0),0);yield return WalkTo(P(6.2f,.16f,0));NeedHome(home.Contains(player.transform.position),"actual east-door entry");
        player.Aim(P(-3,1.2f,-3));yield return Capture("02-entry-hall");
        yield return WalkTo(P(-2.5f,.16f,0));yield return WalkTo(P(-2.5f,.16f,-2.6f));yield return WalkTo(P(.3f,.16f,-2.6f));player.Aim(P(-5.6f,.7f,-3.3f));yield return Capture("03-parlour");
        player.Aim(P(.1f,.18f,-2.8f));yield return Capture("04-chalk-outline");
        yield return WalkTo(P(-2.5f,.16f,-2.6f));yield return WalkTo(P(-2.5f,.16f,0));yield return WalkTo(P(-.2f,.16f,0));yield return WalkTo(P(-.2f,.16f,2));player.Aim(P(.3f,1.2f,4.8f));yield return Capture("05-study");
        yield return WalkTo(P(2.25f,.16f,4.55f));var medical=containers.Single(c=>c.fixtureId=="home-cabinet-medical");player.Aim(medical.GetComponent<Collider>().bounds.center);yield return null;
        NeedHome(TryInteract()&&medical.IsOpen,"Medical drawer normal ray opens");yield return new WaitForSeconds(.8f);NeedHome(!inspected,"Opening furniture must not display a list");yield return Capture("06-medical-drawer");
        var hospital=medical.contents.Single(i=>i.evidenceId=="H09");AimVisibleHomePaper(hospital);yield return null;NeedHome(TryInteract()&&inspected==hospital,"Physical medical paper ray");yield return Capture("06b-paper-model");RotateEvidence(new Vector2(0,155));yield return new WaitForSeconds(.3f);yield return Capture("06c-paper-turned");ResetEvidencePose();ToggleEvidenceDetails();double clock=ElapsedGameMinutes;var feet=player.transform.position;player.scriptedDirection=Vector3.forward;yield return new WaitForSeconds(.5f);player.scriptedDirection=Vector3.zero;
        NeedHome(Blocked&&Vector3.Distance(feet,player.transform.position)<.01f&&ElapsedGameMinutes>clock,"Reading movement and time contract");yield return Capture("07-hospital-birthday");CloseInspect();NeedHome(!evidenceStage&&!Blocked,"Close restores exploration");
        var safe=containers.Single(c=>c.safe);yield return WalkTo(P(1.2f,.16f,2.8f));player.Aim(safe.GetComponent<Collider>().bounds.center);yield return null;NeedHome(TryInteract()&&inspected==safe,"Safe normal ray opens keypad");
        NeedHome(!safe.TryCode("1406")&&!safe.Unlocked,"Wrong birthday order must fail");yield return Capture("08-safe-locked");NeedHome(safe.TryCode("0614")&&safe.Unlocked&&safe.IsOpen,"Birthday opens safe without prior evidence gate");yield return new WaitForSeconds(.7f);CloseInspect();yield return Capture("09-safe-open");
        Inspect(safe.contents.Single(i=>i.evidenceId=="H12"));ToggleEvidenceDetails();yield return Capture("10-insurance-paper");CloseInspect();
        yield return WalkTo(P(-.2f,.16f,2));yield return WalkTo(P(-.2f,.16f,0));yield return WalkTo(P(-5.2f,.16f,0));yield return WalkTo(P(-5.2f,.16f,1.55f));player.Aim(P(-4.8f,1.2f,3.6f));yield return Capture("11-kitchen");
        yield return WalkTo(P(-5.2f,.16f,3.0f));yield return WalkTo(P(-6.1f,.16f,4.3f));yield return WalkTo(P(-5.6f,.16f,5.7f));yield return WalkTo(P(-5.6f,.16f,7));player.Aim(P(-4.8f,1.2f,11.5f));yield return Capture("12-utility");
        yield return WalkTo(P(-5.6f,.16f,5.6f));yield return WalkTo(P(-6.1f,.16f,4.3f));yield return WalkTo(P(-5.2f,.16f,3.0f));yield return WalkTo(P(-5.2f,.16f,0));yield return WalkTo(P(4,.16f,0));
        // Continuous ascent on actual stair colliders, no teleport between floors.
        yield return WalkTo(P(4,.16f,1.5f));yield return WalkTo(P(4,1.76f,4.8f),.6f);yield return WalkTo(P(6.4f,1.76f,4.8f),.6f);yield return WalkTo(P(6.4f,3.36f,1.2f),.6f);yield return WalkTo(P(6.4f,3.36f,0),.6f);
        NeedHome(player.transform.position.y>3.1f,"Continuous staircase did not reach upper floor");player.Aim(P(4.8f,4,4.7f));yield return Capture("13-upper-stairs");
        yield return WalkTo(P(-2.5f,3.36f,0));yield return WalkTo(P(-2.5f,3.36f,-2));player.Aim(P(-3,4,-4.7f));yield return Capture("14-bedroom");
        yield return WalkTo(P(-2.5f,3.36f,0));yield return WalkTo(P(-5.2f,3.36f,0));yield return WalkTo(P(-5.2f,3.36f,2));player.Aim(P(-5.4f,4.2f,4.7f));yield return Capture("15-child-room");
        yield return WalkTo(P(-5.2f,3.36f,0));yield return WalkTo(P(-.2f,3.36f,0));yield return WalkTo(P(-.2f,3.36f,2));player.Aim(P(.8f,4.2f,4.8f));yield return Capture("16-bathroom");
        yield return WalkTo(P(-.2f,3.36f,0));yield return WalkTo(P(4,3.36f,0));yield return WalkTo(P(4,3.36f,-2));player.Aim(P(4.3f,4.2f,-4.8f));yield return Capture("17-sewing-room");
        yield return ReviewHomeStorage();
        foreach(var record in home.GetComponentsInChildren<Interactable>().Where(i=>i.evidenceId=="E10"||i.evidenceId=="H25")){var spot=record.evidenceId=="H25"?P(-4.5f,3.36f,4.5f):P(-.5f,.16f,4.2f);player.Place(spot,0);player.Aim(record.transform.position);yield return null;NeedHome(TryInteract()&&inspected==record,"Loose evidence ray "+record.evidenceId);CloseInspect();}
        letterPos[0]=new Vector2(320,100);letterPos[1]=new Vector2(320,270);CheckHouseLetter();NeedHome(state.entries.Any(e=>e.id=="H27"),"Letter comparison saves");
        Inspect(home.GetComponentsInChildren<Interactable>().Single(i=>i.evidenceId=="E09"));houseLetterOpen=true;yield return Capture("18a-letter-comparison");CloseInspect();
        foreach(var id in new[]{"H04","H17","H18"}){var record=home.GetComponentsInChildren<Interactable>().Single(i=>i.evidenceId==id);player.Place(id=="H04"?P(6,.16f,-2.8f):id=="H17"?P(-6.5f,.16f,0):P(-4.9f,.16f,4.9f),0);player.Aim(record.transform.position);yield return null;NeedHome(TryInteract()&&inspected==record,"Loose evidence "+id);CloseInspect();}
        player.Place(P(1.2f,.16f,2.8f),0);player.Aim(safe.transform.position);yield return Capture("18b-safe-physical-open");
        journal=true;journalPage=(state.entries.Count-1)/4;selectedEntry=state.entries.Count-1;yield return Capture("18c-journal-last-page");journal=false;
        player.Place(P(4,3.36f,-2),0);player.Aim(P(4.3f,4.2f,-4.8f));
        InspectCarriedTicket();NeedHome(ModelEvidence&&!evidenceDetails,"Carried ticket model-first");yield return Capture("ticket-3d-intact");CloseInspect();
        var spent=FindObjectsByType<Interactable>(FindObjectsSortMode.None).First(i=>i.evidenceId=="E03");Inspect(spent);yield return Capture("ticket-3d-punched");CloseInspect();
        foreach(string id in new[]{"E06","E07","E08","H17","H18"}){var other=FindObjectsByType<Interactable>(FindObjectsSortMode.None).First(i=>i.evidenceId==id);Inspect(other);NeedHome(ModelEvidence&&evidenceObject&&!evidenceDetails,"Shared model viewer "+id);yield return Capture("evidence-model-"+id);CloseInspect();}
        DayReviewTime(1290);dayNightReviewFrozen=false;DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);yield return new WaitForSeconds(.8f);yield return Capture("18-night-room");
        // Close exterior doors in a safe staging spot; verify all parts share the same acoustic enclosure.
        if(east.IsOpen)east.Use();if(west.IsOpen)west.Use();yield return new WaitForSeconds(1);NeedHome(WeatherShelter.MufflesRain(player.eye.transform.position),"Closed home must muffle rain");
        east.Use();yield return new WaitForSeconds(1);NeedHome(!WeatherShelter.MufflesRain(player.eye.transform.position),"Open exterior door must retain outdoor rain sound");east.Use();yield return new WaitForSeconds(1);
        NeedHome(Silent,"Companion-independent review");Save();
        File.WriteAllText(Path.Combine(captures,"house-results.json"),"{\"passed\":true,\"rooms\":8,\"containers\":20,\"stairAscent\":true,\"safeWrongAndCorrect\":true,\"readingClock\":true,\"acoustics\":true,\"verification\":\"Scripted actual controller and interaction rays; every stored paper uses an actual visible interaction ray; detail toggle and code entry call shared input handlers. Not physical input or blind playtest.\"}");Debug.Log("HOME_REVIEW_PASS");Application.Quit();
    }
    IEnumerator ReviewHouseStairs(){
        var home=HouseInstance.Current;Vector3 P(float x,float y,float z)=>home.Point(x,y,z);
        var views=new (string name,Vector3 feet,Vector3 target)[]{
            ("01-lower-approach",P(4,.16f,.25f),P(3.6f,1.9f,4.4f)),
            ("02-lower-wall",P(4,.9f,2.4f),P(3.35f,2.2f,4.5f)),
            ("03-landing-left",P(5.2f,1.76f,4.95f),P(3.25f,2.25f,3.0f)),
            ("04-landing-return",P(5.2f,1.76f,4.95f),P(7.02f,2.65f,4.20f)),
            ("05-upper-climb",P(6.40f,1.96f,4.05f),P(6.2f,3.7f,1.05f)),
            ("06-upper-wall",P(6.70f,1.96f,4.28f),P(7.3f,3.0f,2.0f)),
            ("07-upper-exit",P(6.4f,3.36f,.25f),P(6.4f,3.3f,2.1f)),
            ("08-lower-descent",P(4,1.76f,4.75f),P(3.45f,1.8f,2.2f))
        };
        foreach(var v in views){player.Place(v.feet,0);yield return new WaitForSeconds(.4f);player.Aim(v.target);yield return Capture(v.name);}
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--stairs-baseline")<0){
            int traversal=0;
            foreach(float shift in new[]{0f,-.24f,.24f,0f}){
                player.scriptedSprint=traversal++==3;
                player.Place(P(4+shift,.16f,0),0);
                yield return WalkTo(P(4+shift,.16f,1.5f),1f);yield return WalkTo(P(4+shift,1.76f,4.9f),1f);
                yield return WalkTo(P(6.4f+shift,1.76f,4.9f),1f);yield return WalkTo(P(6.4f+shift,3.36f,.2f),1f);
                NeedHome(player.transform.position.y>3.2f,"Stair upper arrival "+shift);
                yield return WalkTo(P(6.4f+shift,1.76f,4.9f),1f);yield return WalkTo(P(4+shift,1.76f,4.9f),1f);
                yield return WalkTo(P(4+shift,.16f,.2f),1f);NeedHome(player.transform.position.y<.3f,"Stair lower arrival "+shift);
            }
        }
        player.scriptedSprint=false;
        DayReviewTime(1290);DayNightCycle.Instance.SetReviewWeather(TownWeather.Kind.Rain);
        foreach(int i in new[]{1,5,6}){var v=views[i];player.Place(v.feet,0);yield return new WaitForSeconds(.4f);player.Aim(v.target);yield return Capture("night-"+v.name);}
        Debug.Log("HOME_STAIRS_REVIEW_PASS");Application.Quit();
    }
    IEnumerator ReviewHousePlacement(){
        var home=HouseInstance.Current;Vector3 P(Vector3 p)=>home.Point(p.x,p.y,p.z);
        var old=P(new Vector3(-2.023315f,3.38f,-4.816635f));var recovered=home.ResolveLoadedFurnitureOverlap(old);
        NeedHome(home.ClearStandingPosition(recovered)&&Vector3.Distance(old,recovered)<1,"Old bedroom save resolves locally");
        player.Place(recovered,0);player.Aim(P(new Vector3(-2.9f,4.1f,-5.6f)));yield return Capture("00-recovered-bedside-position");
        // Additional eye-height views inspect backs, sides and support contact, not just doorway beauty shots.
        var views=new (string name,Vector3 feet,Vector3 target)[]{
            ("01-master-front",new(-4.8f,3.36f,-2.25f),new(-3,4,-5.2f)),
            ("02-master-head-side",new(-4.35f,3.36f,-4.3f),new(-3.6f,4.2f,-5.7f)),
            ("03-master-left-corner",new(-6.0f,3.36f,-3.5f),new(-7.5f,4.1f,-5.3f)),
            ("04-master-wardrobe",new(-1.4f,3.36f,-3.1f),new(.45f,4.35f,-2.5f)),
            ("05-master-right-side",new(-.5f,3.36f,-4.35f),new(-2.1f,4.0f,-5.5f)),
            ("06-child-room",new(-5.0f,3.36f,2.25f),new(-5.35f,4.0f,5.4f)),
            ("07-child-head-side",new(-5.7f,3.36f,4.15f),new(-6.8f,4.2f,5.7f)),
            ("08-child-desk",new(-4.7f,3.36f,4.05f),new(-3.6f,4.05f,5.45f)),
            ("09-child-toys",new(-4.3f,3.36f,2.65f),new(-3,3.9f,1.4f)),
            ("10-study-front",new(-.3f,.16f,2.5f),new(.2f,1,5.5f)),
            ("11-study-desk-side",new(1.35f,.16f,4.2f),new(-.1f,.85f,5.5f)),
            ("12-study-bookcase",new(-.4f,.16f,3.2f),new(-1.65f,1.35f,3.5f)),
            ("13-study-safe",new(.8f,.16f,3.1f),new(1.9f,.9f,1.5f)),
            ("14-kitchen-window",new(-5.7f,.16f,3.15f),new(-7.5f,1.1f,3.0f)),
            ("15-kitchen-cabinets",new(-5.35f,.16f,3.9f),new(-3.6f,1.05f,5.45f)),
            ("16-kitchen-cooker",new(-5.25f,.16f,2.05f),new(-3,.7f,1.47f)),
            ("17-kitchen-table",new(-5.55f,.16f,4.0f),new(-3.9f,.7f,3.0f)),
            ("18-parlour-seating",new(-3,.16f,-2.7f),new(-6.4f,.8f,-3.8f)),
            ("19-parlour-sofa-side",new(-5.5f,.16f,-5.4f),new(-7.3f,.7f,-3.4f)),
            ("20-parlour-sideboard",new(5.7f,.16f,-3.0f),new(7.48f,.9f,-3.7f)),
            ("21-parlour-fireplace",new(1.6f,.16f,-3.6f),new(0,.9f,-5.75f)),
            ("22-utility",new(-5.5f,.16f,10.1f),new(-5.1f,.9f,12.4f)),
            ("23-utility-cupboard",new(-6.25f,.16f,11.0f),new(-7.5f,.9f,11.85f)),
            ("24-bathroom",new(.2f,3.36f,3.1f),new(.9f,4.05f,5.5f)),
            ("25-basin-mirror",new(1.3f,3.36f,4.3f),new(2.1f,4.6f,5.7f)),
            ("26-bath-side",new(-1.35f,3.36f,4.4f),new(0,3.8f,5.4f)),
            ("27-sewing-room",new(4.65f,3.36f,-2.8f),new(4.8f,4.1f,-5.45f)),
            ("28-sewing-table",new(2.1f,3.36f,-4.3f),new(3.3f,4.1f,-5.46f)),
            ("29-storage-trunk",new(5.9f,3.36f,-3.8f),new(7.47f,3.85f,-2.65f)),
            ("30-hall-console",new(5.4f,.16f,.4f),new(6.3f,.75f,-.67f)),
            ("31-study-stationery",new(1.3f,.16f,3.4f),new(2.62f,.7f,3.65f))
        };
        foreach(var v in views){
            var foot=P(v.feet);Physics.SyncTransforms();
            var overlaps=Physics.OverlapCapsule(foot+Vector3.up*.3f,foot+Vector3.up*1.4f,.25f).Where(c=>!c.isTrigger&&c.transform.IsChildOf(home.transform)).ToArray();
            NeedHome(overlaps.Length==0,"Placement camera obstructed "+v.name+": "+string.Join(",",overlaps.Select(c=>c.name)));
            player.Place(foot,0);player.Aim(P(v.target));yield return new WaitForSeconds(.3f);yield return Capture(v.name);
        }
        File.WriteAllText(Path.Combine(captures,"placement-results.json"),"{\"passed\":true,\"eyeHeightViews\":31,\"unobstructedCameraCapsules\":true,\"oldBedroomPositionRecovered\":true,\"verification\":\"Native screenshots from staged eye-height positions; visual assessment separate. No physical-input or blind-playtest claim.\"}");
        Debug.Log("HOME_PLACEMENT_VIEWS_PASS: 31 eye-height views, unobstructed camera capsules; visual assessment required.");Application.Quit();
    }
    IEnumerator ReviewHomeStorage(){
        var home=HouseInstance.Current;Vector3 P(float x,float y,float z)=>home.Point(x,y,z);
        var containers=home.GetComponentsInChildren<HouseContainer>();var safe=containers.Single(c=>c.safe);
        // Each fixture is tested with the same three-metre interaction ray from its room-side approach.
        foreach(var c in containers){var spot=c.transform.position-c.transform.forward*(c.hinged||c.safe?1.10f:.65f);spot.y=home.transform.position.y+(c.transform.position.y>3.3f?3.36f:.16f);
            switch(c.fixtureId){case "home-cabinet-desk":spot=P(-.95f,.16f,4.02f);break;case "home-cabinet-stationery":spot=P(1.55f,.16f,3.65f);break;case "home-cabinet-medical":spot=P(2.25f,.16f,4.55f);break;case "home-cabinet-safe":spot=P(1.2f,.16f,2.8f);break;case "home-cabinet-kitchen-bills":spot=P(-4.16f,.16f,4.4f);break;case "home-cabinet-kitchen-crockery":spot=P(-6.4f,.16f,3.6f);break;case "home-cabinet-old-trunk":spot=P(6.25f,3.36f,-2.65f);break;}
            Physics.SyncTransforms();var overlap=Physics.OverlapCapsule(spot+Vector3.up*.3f,spot+Vector3.up*1.4f,.25f).Where(col=>!col.isTrigger&&col.transform.IsChildOf(home.transform)).ToArray();NeedHome(overlap.Length==0,"Blocked cabinet approach "+c.fixtureId+": "+string.Join(",",overlap.Select(col=>col.name)));
            player.Place(spot,0);player.Aim(c.GetComponent<Collider>().bounds.center);yield return null;
            if(c.IsOpen)c.SetOpen(false);yield return new WaitForSeconds(.6f);player.Aim(c.GetComponent<Collider>().bounds.center);yield return null;
            NeedHome(TryInteract()&&c.IsOpen,"Storage opens via ray "+c.fixtureId);yield return new WaitForSeconds(.65f);NeedHome(!inspected,"No automatic cabinet menu");
            player.Aim(c.transform.position+Vector3.up*.06f);yield return Capture("cabinet-"+c.fixtureId);
            foreach(var record in c.contents){AimVisibleHomePaper(record);yield return null;NeedHome(TryInteract()&&inspected==record,"Physical paper visible and ray-selectable "+record.evidenceId);NeedHome(ModelEvidence&&!evidenceDetails&&evidenceObject,"3D-only first phase "+record.evidenceId);ToggleEvidenceDetails();NeedHome(evidenceDetails,"E details "+record.evidenceId);CloseInspect();}
            c.SetOpen(false);yield return new WaitForSeconds(.6f);
        }
        safe.SetOpen(true);yield return new WaitForSeconds(.6f);
    }
    IEnumerator VerifyHouseSave(){
        yield return new WaitForSeconds(1);var home=HouseInstance.Current;Vector3 P(float x,float y,float z)=>home.Point(x,y,z);
        NeedHome(home&&home.Contains(player.transform.position)&&player.transform.position.y>3.1f,"Upper floor save restoration");var safe=home.GetComponentsInChildren<HouseContainer>().Single(c=>c.safe);
        NeedHome(safe.Unlocked&&safe.IsOpen&&state.entries.Any(e=>e.id=="H09")&&state.entries.Any(e=>e.id=="H12")&&state.entries.Any(e=>e.id=="H27"),"Safe and documents did not survive relaunch");yield return Capture("19-reloaded-upstairs");
        yield return WalkTo(P(4,3.36f,0));yield return WalkTo(P(6.4f,3.36f,0));yield return WalkTo(P(6.4f,3.36f,1.2f));yield return WalkTo(P(6.4f,1.76f,4.8f),.6f);yield return WalkTo(P(4,1.76f,4.8f),.6f);yield return WalkTo(P(4,.16f,1.3f),.6f);yield return WalkTo(P(4,.16f,0));NeedHome(player.transform.position.y<.4f,"Continuous descent");
        foreach(var side in new[]{"west","east"}){var door=home.GetComponentsInChildren<ShopMechanism>().Single(d=>d.fixtureId=="home-door-"+side);float sign=side=="west"?-1:1;
            yield return WalkTo(P(sign*6.3f,.16f,0));if(door.IsOpen){door.Use();yield return new WaitForSeconds(1);}NeedHome(!door.IsOpen,"Door closes "+side);
            player.scriptedDirection=Vector3.right*sign;yield return new WaitForSeconds(1);player.scriptedDirection=Vector3.zero;NeedHome(Mathf.Abs(home.transform.InverseTransformPoint(player.transform.position).x)<7.85f,"Closed leaf blocks "+side);
            yield return WalkTo(P(sign*6.3f,.16f,0));player.Aim(door.GetComponent<Collider>().bounds.center);yield return null;NeedHome(TryInteract()&&door.IsOpen,"Inside door ray "+side);yield return new WaitForSeconds(1);yield return WalkTo(P(sign*10,.16f,0));
            player.Aim(door.GetComponent<Collider>().bounds.center);yield return null;NeedHome(TryInteract()&&!door.IsOpen,"Outside close ray "+side);yield return new WaitForSeconds(1);player.Aim(door.GetComponent<Collider>().bounds.center);yield return null;NeedHome(TryInteract()&&door.IsOpen,"Outside open ray "+side);yield return new WaitForSeconds(1);yield return WalkTo(P(sign*6.3f,.16f,0));
        }
        player.Aim(home.GetComponentInChildren<HomeTicket>().transform.position);yield return null;NeedHome(TryInteract()&&inspected.evidenceId=="E02","Hall shows same carried ticket");CloseInspect();
        yield return WalkTo(P(10,.16f,0));yield return WalkTo(P(11.1f,.16f,1.8f));var guard=home.GetComponentsInChildren<Interactable>().Single(i=>i.title=="住宅看守");player.Aim(guard.GetComponent<Collider>().bounds.center);yield return null;NeedHome(TryInteract()&&DialogueOpen,"Guard normal dialogue ray");yield return Capture("20-guard-dialogue");AdvanceDialogue();NeedHome(dialogueChoices.Count==3,"Repeatable guard choices");CloseDialogue();
        NeedHome(HouseInstance.Current==home,"Continuous house identity");Save();File.WriteAllText(Path.Combine(captures,"house-save-results.json"),"{\"passed\":true,\"separateProcess\":true,\"upperFloor\":true,\"safe\":true,\"documents\":true,\"stairDescent\":true,\"bothDoorRoundTrips\":true,\"guard\":true}");Debug.Log("HOME_SAVE_PASS");Application.Quit();
    }
}
