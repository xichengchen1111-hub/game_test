using UnityEngine;
public static class EvidenceSolidModel {
    // Clock face for the kitchen clock (the same printed dial texture the pocket watch uses).
    public static Texture2D Dial;
    // Solid objects (not paper): the tower plaque, the lock, the kitchen clock, and the player's own
    // pocket watch, notebook and wallet as they lie in the briefcase (user, 2026-10-15).
    public static bool Handles(string kind)=>kind=="H17"||kind=="H18"||kind=="E06"||kind=="pack-watch"||kind=="pack-notebook"||kind=="pack-wallet";
    public static GameObject Create(string kind,Texture2D image=null,float minutes=0){
        var root=new GameObject("Observed "+kind);
        void Part(string name,PrimitiveType shape,Vector3 pos,Vector3 size,Color color,Vector3 angles){var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(root.transform,false);o.transform.localPosition=pos;o.transform.localScale=size;o.transform.localEulerAngles=angles;Object.DestroyImmediate(o.GetComponent<Collider>());var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=color;o.GetComponent<Renderer>().sharedMaterial=mat;Finish(o,name);}
        GameObject Q(string name,PrimitiveType shape,Vector3 pos,Vector3 size,Color color,Vector3 angles,float metal=-1,float rough=-1,float brushed=-1,float age=-1,float gloss=-1,float glass=0){
            Part(name,shape,pos,size,color,angles);var o=root.transform.GetChild(root.transform.childCount-1).gameObject;var f=o.GetComponent<EvidenceSurface>();
            if(metal>=0)f.metal=metal;if(rough>=0)f.rough=rough;if(brushed>=0)f.brushed=brushed;if(age>=0)f.age=age;if(gloss>=0)f.gloss=gloss;f.glass=glass;return o;}
        var brass=new Color(.52f,.37f,.17f);var iron=new Color(.19f,.2f,.18f);var cream=new Color(.78f,.74f,.62f);
        if(kind=="E06"){
            Part("Original bronze plaque",PrimitiveType.Cube,Vector3.zero,new Vector3(.67f,.72f,.025f),brass,Vector3.zero);
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Original tower inscription";face.transform.SetParent(root.transform,false);face.transform.localPosition=new Vector3(0,0,-.016f);face.transform.localScale=new Vector3(.63f,.68f,1);Object.DestroyImmediate(face.GetComponent<Collider>());var ink=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ink.SetTexture("_BaseMap",image);face.GetComponent<Renderer>().sharedMaterial=ink;
        }else if(kind=="H17"){
            // West kitchen door: a 1930s iron rim lock on the painted door, its brass bolt thrown across the
            // gap into the keeper on the jamb. Bolt and keeper are whole (no pry damage); two old scratches of
            // different depth sit by the keyhole on the inside face (H17 record).
            var paint=new Color(.5f,.56f,.47f);var ironDark=new Color(.12f,.115f,.11f);var black=new Color(.03f,.03f,.03f);
            Q("Door stile painted",PrimitiveType.Cube,new Vector3(-.07f,0,.078f),new Vector3(.62f,.86f,.05f),paint,Vector3.zero,metal:0,rough:.72f,brushed:.6f,age:.75f);
            Q("Door jamb painted",PrimitiveType.Cube,new Vector3(.335f,0,.07f),new Vector3(.13f,.86f,.07f),paint*.9f,Vector3.zero,metal:0,rough:.72f,brushed:.6f,age:.8f);
            Q("Lock case",PrimitiveType.Cube,new Vector3(-.05f,0,0),new Vector3(.40f,.30f,.10f),ironDark,Vector3.zero,metal:.55f,rough:.55f,age:.55f);
            Q("Lock case front plate",PrimitiveType.Cube,new Vector3(-.05f,0,-.054f),new Vector3(.37f,.27f,.008f),ironDark*1.25f,Vector3.zero,metal:.6f,rough:.42f,brushed:.5f,age:.5f);
            Q("Lock flange",PrimitiveType.Cube,new Vector3(.155f,0,.012f),new Vector3(.014f,.34f,.13f),ironDark,Vector3.zero,metal:.6f,rough:.5f,age:.5f);
            Q("Lock bolt brass",PrimitiveType.Cube,new Vector3(.225f,-.03f,-.012f),new Vector3(.1f,.034f,.03f),brass,Vector3.zero);
            Q("Keeper box",PrimitiveType.Cube,new Vector3(.32f,-.02f,-.02f),new Vector3(.085f,.15f,.075f),ironDark,Vector3.zero,metal:.55f,rough:.55f,age:.55f);
            Q("Keeper mouth",PrimitiveType.Cube,new Vector3(.277f,-.03f,-.012f),new Vector3(.004f,.046f,.04f),black,Vector3.zero,metal:0,rough:.9f);
            // Brass knob on its rose, and the keyhole escutcheon.
            var rose=Lathe(root.transform,"Brass knob rose",new[]{new Vector2(0,0),new Vector2(.05f,0),new Vector2(.05f,-.005f),new Vector2(.042f,-.01f),new Vector2(.013f,-.012f),new Vector2(0,-.012f)},brass);rose.transform.localPosition=new Vector3(-.13f,.045f,-.058f);
            var knob=Lathe(root.transform,"Brass knob",new[]{new Vector2(.011f,0),new Vector2(.011f,-.028f),new Vector2(.027f,-.038f),new Vector2(.034f,-.052f),new Vector2(.03f,-.066f),new Vector2(.016f,-.072f),new Vector2(0,-.073f)},brass);knob.transform.localPosition=new Vector3(-.13f,.045f,-.068f);
            var esc=Lathe(root.transform,"Brass escutcheon",new[]{new Vector2(.032f,0),new Vector2(.032f,-.003f),new Vector2(.026f,-.006f),new Vector2(0,-.006f)},brass);esc.transform.localPosition=new Vector3(.03f,-.055f,-.058f);
            Q("Keyhole",PrimitiveType.Cylinder,new Vector3(.03f,-.049f,-.0645f),new Vector3(.019f,.0012f,.019f),black,new Vector3(90,0,0),metal:0,rough:.9f);
            Q("Keyhole slot",PrimitiveType.Cube,new Vector3(.03f,-.064f,-.0645f),new Vector3(.009f,.024f,.0024f),black,Vector3.zero,metal:0,rough:.9f);
            // Domed slotted screws at the four corners of the case.
            foreach(var c in new[]{new Vector2(-.215f,.115f),new Vector2(.115f,.115f),new Vector2(-.215f,-.115f),new Vector2(.115f,-.115f)}){
                var screw=Lathe(root.transform,"Screw head brass",new[]{new Vector2(.014f,0),new Vector2(.014f,-.002f),new Vector2(.011f,-.006f),new Vector2(.006f,-.0085f),new Vector2(0,-.009f)},brass*.85f,24);screw.transform.localPosition=new Vector3(c.x,c.y,-.058f);
                Q("Screw slot",PrimitiveType.Cube,new Vector3(c.x,c.y,-.0665f),new Vector3(.024f,.003f,.002f),black,new Vector3(0,0,c.x*300+c.y*170),metal:0,rough:.9f);
            }
            // Two old scratches by the keyhole: one deep (dark groove with a bright bared edge), one faint.
            Q("Deep scratch groove",PrimitiveType.Cube,new Vector3(.07f,-.03f,-.0585f),new Vector3(.13f,.0045f,.0012f),black,new Vector3(0,0,17),metal:0,rough:.9f);
            Q("Deep scratch bright edge",PrimitiveType.Cube,new Vector3(.0695f,-.026f,-.0586f),new Vector3(.125f,.0016f,.0012f),new Color(.7f,.68f,.62f),new Vector3(0,0,17),metal:1,rough:.25f);
            Q("Faint scratch",PrimitiveType.Cube,new Vector3(.055f,-.09f,-.0584f),new Vector3(.09f,.0018f,.001f),new Color(.36f,.35f,.33f),new Vector3(0,0,-8),metal:.8f,rough:.35f);
        }else if(kind=="pack-watch"){
            // Brass hunter-less open-face watch: case, bezel, the dial the player reads with Q, crown and bow.
            // Case and bezel behind the face, each a step forward, so nothing shares the dial's plane.
            // Turned case: domed back, a rounded band and the bezel lip, so the silhouette is a real watch.
            Lathe(root.transform,"Watch case",new[]{new Vector2(0,.07f),new Vector2(.12f,.066f),new Vector2(.2f,.052f),new Vector2(.238f,.03f),new Vector2(.25f,.008f),new Vector2(.252f,-.012f),new Vector2(.246f,-.03f),new Vector2(.236f,-.036f),new Vector2(.2f,-.036f)},brass);
            // Curb chain from the bow, falling away to one side.
            for(int c=0;c<14;c++){float t=c/13f;var at=new Vector3(.05f+t*.42f,.44f-t*t*.62f,.02f+t*.05f);
                var link=new GameObject("Watch chain link");link.transform.SetParent(root.transform,false);link.transform.localPosition=at;link.transform.localRotation=Quaternion.Euler(c%2==0?0:90,0,-60+t*40);
                var pts=new Vector3[9];for(int k=0;k<9;k++){float a=k/8f*Mathf.PI*2;pts[k]=new Vector3(Mathf.Cos(a)*.022f,Mathf.Sin(a)*.036f,0);}
                EvidenceKinds.Wire(link.transform,"Watch chain link",pts,.0055f,brass);}
            Part("Bezel",PrimitiveType.Cylinder,new Vector3(0,0,-.034f),new Vector3(.47f,.004f,.47f),new Color(.66f,.5f,.25f),new Vector3(90,0,0));
            // The printed face is round with transparent corners; the study shader clips them.
            var dial=GameObject.CreatePrimitive(PrimitiveType.Quad);dial.name="Watch dial";dial.transform.SetParent(root.transform,false);dial.transform.localPosition=new Vector3(0,0,-.042f);dial.transform.localScale=new Vector3(.45f,.45f,1);Object.DestroyImmediate(dial.GetComponent<Collider>());
            var dialMat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));if(image)dialMat.SetTexture("_BaseMap",image);dialMat.color=image?Color.white:cream;dial.GetComponent<Renderer>().sharedMaterial=dialMat;
            Part("Pendant",PrimitiveType.Cylinder,new Vector3(0,.27f,0),new Vector3(.06f,.035f,.06f),brass,Vector3.zero);
            Part("Crown",PrimitiveType.Cylinder,new Vector3(0,.315f,0),new Vector3(.085f,.022f,.085f),new Color(.66f,.5f,.25f),Vector3.zero);
            // The bow: a ring of short rods in the face's plane.
            for(int k=0;k<12;k++){float a0=k*30*Mathf.Deg2Rad;Part("Bow",PrimitiveType.Cylinder,new Vector3(Mathf.Cos(a0)*.065f,.395f+Mathf.Sin(a0)*.065f,0),new Vector3(.014f,.018f,.014f),brass,new Vector3(0,0,k*30));}
            // Hands at the time it is taken out, the same clock the Q watch shows.
            float hours=minutes%720/60f,mins=minutes%60;
            void Hand(string name,float degrees,float length,float width,float z){var h=GameObject.CreatePrimitive(PrimitiveType.Cube);h.name=name;h.transform.SetParent(root.transform,false);Object.DestroyImmediate(h.GetComponent<Collider>());var turn=Quaternion.Euler(0,0,-degrees);h.transform.localRotation=turn;h.transform.localPosition=new Vector3(0,0,z)+turn*new Vector3(0,length*.38f,0);h.transform.localScale=new Vector3(width,length,.003f);var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.color=new Color(.12f,.1f,.09f);h.GetComponent<Renderer>().sharedMaterial=m;}
            Hand("Watch hour hand",hours*30,.12f,.014f,-.046f);Hand("Watch minute hand",mins*6,.17f,.009f,-.05f);
            Part("Watch centre",PrimitiveType.Cylinder,new Vector3(0,0,-.053f),new Vector3(.022f,.003f,.022f),new Color(.16f,.13f,.11f),new Vector3(90,0,0));
        }else if(kind=="pack-notebook"){
            // A pocket notebook in worn leather, the page block showing at the edge, an elastic band.
            Part("Leather cover",PrimitiveType.Cube,Vector3.zero,new Vector3(.42f,.6f,.07f),new Color(.2f,.12f,.07f),Vector3.zero);
            Part("Page block",PrimitiveType.Cube,new Vector3(.012f,0,0),new Vector3(.4f,.575f,.055f),new Color(.86f,.82f,.71f),Vector3.zero);
            Part("Front board",PrimitiveType.Cube,new Vector3(0,0,-.03f),new Vector3(.425f,.605f,.012f),new Color(.22f,.13f,.075f),Vector3.zero);
            Part("Elastic band",PrimitiveType.Cube,new Vector3(.13f,0,-.037f),new Vector3(.022f,.61f,.006f),new Color(.07f,.06f,.05f),Vector3.zero);
        }else if(kind=="pack-wallet"){
            // A folded leather wallet with stitched edges and the end of a banknote showing.
            Part("Folded leather",PrimitiveType.Cube,Vector3.zero,new Vector3(.62f,.36f,.055f),new Color(.24f,.14f,.07f),Vector3.zero);
            Part("Stitched edge",PrimitiveType.Cube,new Vector3(0,.165f,-.029f),new Vector3(.58f,.008f,.004f),new Color(.55f,.43f,.28f),Vector3.zero);
            Part("Stitched edge",PrimitiveType.Cube,new Vector3(0,-.165f,-.029f),new Vector3(.58f,.008f,.004f),new Color(.55f,.43f,.28f),Vector3.zero);
            Part("Banknote edge",PrimitiveType.Cube,new Vector3(.02f,.19f,.004f),new Vector3(.5f,.04f,.03f),new Color(.6f,.66f,.55f),Vector3.zero);
        }else{
            // Kitchen wall clock: walnut case with cornice, arched crest and pilasters; brass bezel over the
            // dial under dusty glass; a glazed lower door with the pendulum stopped to one side (H18).
            // Hand angles are unchanged from the earlier model.
            var walnut=new Color(.3f,.17f,.08f);var walnutDark=new Color(.21f,.12f,.055f);var black=new Color(.03f,.03f,.03f);var dust=new Color(.82f,.8f,.74f);
            Q("Clock timber case",PrimitiveType.Cube,Vector3.zero,new Vector3(.52f,.72f,.16f),walnut,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            Q("Clock cornice timber",PrimitiveType.Cube,new Vector3(0,.37f,-.006f),new Vector3(.58f,.035f,.19f),walnutDark,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            Q("Clock cornice timber upper",PrimitiveType.Cube,new Vector3(0,.396f,0),new Vector3(.54f,.02f,.17f),walnut,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            Q("Clock crest timber",PrimitiveType.Cylinder,new Vector3(0,.405f,.005f),new Vector3(.3f,.06f,.3f),walnut,new Vector3(90,0,0),metal:0,rough:.42f,brushed:.9f,age:.3f);
            foreach(float x in new[]{-.255f,.255f}){var fin=Lathe(root.transform,"Brass finial",new[]{new Vector2(0,.03f),new Vector2(.016f,.026f),new Vector2(.02f,.012f),new Vector2(.009f,0),new Vector2(.012f,-.012f),new Vector2(0,-.016f)},brass,20);fin.transform.localPosition=new Vector3(x,.43f,0);fin.transform.localRotation=Quaternion.Euler(-90,0,0);}
            Q("Clock base timber",PrimitiveType.Cube,new Vector3(0,-.37f,-.006f),new Vector3(.58f,.035f,.19f),walnutDark,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            Q("Clock base drop timber",PrimitiveType.Cube,new Vector3(0,-.405f,0),new Vector3(.3f,.04f,.14f),walnut,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            foreach(float x in new[]{-.245f,.245f})Q("Clock pilaster timber",PrimitiveType.Cylinder,new Vector3(x,0,-.08f),new Vector3(.03f,.34f,.03f),walnutDark,Vector3.zero,metal:0,rough:.4f,brushed:.9f,age:.3f);
            var bezel=Lathe(root.transform,"Clock bezel brass",new[]{new Vector2(.215f,0),new Vector2(.215f,-.02f),new Vector2(.207f,-.03f),new Vector2(.193f,-.033f),new Vector2(.182f,-.027f),new Vector2(.18f,-.016f)},brass);bezel.transform.localPosition=new Vector3(0,.1f,-.08f);
            Q("Clock dial",PrimitiveType.Cylinder,new Vector3(0,.1f,-.094f),new Vector3(.36f,.012f,.36f),cream,new Vector3(90,0,0),metal:0,rough:.35f,gloss:.2f,age:.45f);
            if(Dial){var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Clock dial face";face.transform.SetParent(root.transform,false);face.transform.localPosition=new Vector3(0,.1f,-.1072f);face.transform.localScale=new Vector3(.345f,.345f,1);Object.DestroyImmediate(face.GetComponent<Collider>());
                var fm=new Material(Shader.Find("Universal Render Pipeline/Unlit"));fm.SetTexture("_BaseMap",Dial);face.GetComponent<Renderer>().sharedMaterial=fm;Finish(face,"Clock dial face");face.GetComponent<EvidenceSurface>().age=.45f;}
            else for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Part("Hour marker",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.15f,.1f+Mathf.Cos(a)*.15f,-.108f),new Vector3(.014f,.03f,.004f),iron,new Vector3(0,0,-i*30));}
            foreach(float x in new[]{-.055f,.055f})Q("Winding arbor hole",PrimitiveType.Cylinder,new Vector3(x,.045f,-.108f),new Vector3(.016f,.001f,.016f),black,new Vector3(90,0,0),metal:0,rough:.9f);
            Part("Hour hand",PrimitiveType.Cube,new Vector3(.04f,.14f,-.13f),new Vector3(.014f,.12f,.007f),iron,new Vector3(0,0,-45));Part("Minute hand",PrimitiveType.Cube,new Vector3(-.065f,.08f,-.137f),new Vector3(.15f,.012f,.007f),iron,new Vector3(0,0,18));
            Q("Hand centre cap brass",PrimitiveType.Cylinder,new Vector3(0,.1f,-.141f),new Vector3(.02f,.003f,.02f),brass,new Vector3(90,0,0));
            Q("Clock dial glass",PrimitiveType.Cylinder,new Vector3(0,.1f,-.15f),new Vector3(.37f,.0015f,.37f),dust,new Vector3(90,0,0),metal:0,rough:.1f,glass:1);
            var recess=new Color(.1f,.06f,.03f);
            Q("Pendulum recess",PrimitiveType.Cube,new Vector3(0,-.238f,-.074f),new Vector3(.42f,.2f,.01f),recess,Vector3.zero,metal:0,rough:.8f,brushed:.6f);
            foreach(float y in new[]{-.13f,-.346f})Q("Glazed door rail timber",PrimitiveType.Cube,new Vector3(0,y,-.088f),new Vector3(.44f,.024f,.016f),walnutDark,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            foreach(float x in new[]{-.21f,.21f})Q("Glazed door stile timber",PrimitiveType.Cube,new Vector3(x,-.238f,-.088f),new Vector3(.024f,.24f,.016f),walnutDark,Vector3.zero,metal:0,rough:.42f,brushed:.9f,age:.3f);
            Part("Pendulum rod",PrimitiveType.Cube,new Vector3(.014f,-.2f,-.08f),new Vector3(.01f,.15f,.006f),brass,new Vector3(0,0,10));
            var bob=Lathe(root.transform,"Pendulum bob brass",new[]{new Vector2(0,.005f),new Vector2(.036f,.004f),new Vector2(.042f,0),new Vector2(.036f,-.004f),new Vector2(0,-.005f)},brass);bob.transform.localPosition=new Vector3(.028f,-.285f,-.08f);
            Q("Clock door glass",PrimitiveType.Cube,new Vector3(0,-.238f,-.091f),new Vector3(.4f,.2f,.002f),dust,Vector3.zero,metal:0,rough:.1f,glass:1);
            Q("Door keyhole",PrimitiveType.Cylinder,new Vector3(.19f,-.238f,-.0975f),new Vector3(.01f,.001f,.01f),black,new Vector3(90,0,0),metal:0,rough:.9f);
        }
        return root;
    }

    // Material response by part name: brass and iron read as metal, leather and cloth have grain,
    // timber has a run of grain, enamel dials a soft glaze.
    static void Finish(GameObject o,string name){
        var s=o.GetComponent<EvidenceSurface>()?o.GetComponent<EvidenceSurface>():o.AddComponent<EvidenceSurface>();s.translucency=0;s.fibre=0;
        string n=name.ToLowerInvariant();
        if(n.Contains("brass")||n.Contains("bronze")||n.Contains("plate")||n.Contains("bezel")||n.Contains("crown")||n.Contains("pendant")||n.Contains("bow")||n.Contains("watch case")||n.Contains("pendulum")||n.Contains("screw")||n.Contains("chain")||n.Contains("hinge")){s.metal=1;s.rough=.32f;s.brushed=.7f;}
        else if(n.Contains("lock case")||n.Contains("aperture")||n.Contains("slot")||n.Contains("hand")||n.Contains("marker")){s.metal=.6f;s.rough=.45f;}
        else if(n.Contains("leather")||n.Contains("board")||n.Contains("folded")){s.leather=1.4f;s.rough=.62f;s.age=.35f;}
        else if(n.Contains("timber")||n.Contains("rail")||n.Contains("stile")||n.Contains("recess")){s.brushed=.9f;s.rough=.55f;}
        else if(n.Contains("dial")||n.Contains("inscription")){s.fibre=.3f;s.gloss=.25f;s.rough=.35f;}
        else if(n.Contains("page")||n.Contains("banknote")){s.fibre=1;s.stripes=n.Contains("page")?1:0;s.rough=.9f;}
        else if(n.Contains("stitch")||n.Contains("elastic")){s.weave=1;s.rough=.9f;}
    }
    // A turned (lathe) solid from a radius/height profile, axis along local Z.
    static GameObject Lathe(Transform parent,string name,Vector2[] profile,Color color,int sides=48){
        var o=new GameObject(name);o.transform.SetParent(parent,false);
        var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var t=new System.Collections.Generic.List<int>();
        for(int i=0;i<profile.Length;i++)for(int k=0;k<=sides;k++){float a=k/(float)sides*Mathf.PI*2;v.Add(new Vector3(Mathf.Cos(a)*profile[i].x,Mathf.Sin(a)*profile[i].x,profile[i].y));uv.Add(new Vector2(k/(float)sides,i/(float)(profile.Length-1)));}
        for(int i=0;i<profile.Length-1;i++)for(int k=0;k<sides;k++){int a=i*(sides+1)+k,b=a+sides+1;t.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        var m=new Mesh{name=name};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();
        o.AddComponent<MeshFilter>().sharedMesh=m;var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=color;o.AddComponent<MeshRenderer>().sharedMaterial=mat;Finish(o,name);return o;
    }
}
