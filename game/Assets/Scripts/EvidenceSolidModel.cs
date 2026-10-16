using UnityEngine;
public static class EvidenceSolidModel {
    // Solid objects (not paper): the tower plaque, the lock, the kitchen clock, and the player's own
    // pocket watch, notebook and wallet as they lie in the briefcase (user, 2026-10-15).
    public static bool Handles(string kind)=>kind=="H17"||kind=="H18"||kind=="E06"||kind=="pack-watch"||kind=="pack-notebook"||kind=="pack-wallet";
    public static GameObject Create(string kind,Texture2D image=null,float minutes=0){
        var root=new GameObject("Observed "+kind);
        void Part(string name,PrimitiveType shape,Vector3 pos,Vector3 size,Color color,Vector3 angles){var o=GameObject.CreatePrimitive(shape);o.name=name;o.transform.SetParent(root.transform,false);o.transform.localPosition=pos;o.transform.localScale=size;o.transform.localEulerAngles=angles;Object.DestroyImmediate(o.GetComponent<Collider>());var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=color;o.GetComponent<Renderer>().sharedMaterial=mat;}
        var brass=new Color(.52f,.37f,.17f);var iron=new Color(.19f,.2f,.18f);var cream=new Color(.78f,.74f,.62f);
        if(kind=="E06"){
            Part("Original bronze plaque",PrimitiveType.Cube,Vector3.zero,new Vector3(.67f,.72f,.025f),brass,Vector3.zero);
            var face=GameObject.CreatePrimitive(PrimitiveType.Quad);face.name="Original tower inscription";face.transform.SetParent(root.transform,false);face.transform.localPosition=new Vector3(0,0,-.016f);face.transform.localScale=new Vector3(.63f,.68f,1);Object.DestroyImmediate(face.GetComponent<Collider>());var ink=new Material(Shader.Find("Universal Render Pipeline/Unlit"));ink.SetTexture("_BaseMap",image);face.GetComponent<Renderer>().sharedMaterial=ink;
        }else if(kind=="H17"){
            Part("Lock case",PrimitiveType.Cube,Vector3.zero,new Vector3(.4f,.55f,.10f),iron,Vector3.zero);
            Part("Lock plate",PrimitiveType.Cube,new Vector3(0,0,-.056f),new Vector3(.32f,.47f,.015f),brass,Vector3.zero);
            Part("Key aperture",PrimitiveType.Sphere,new Vector3(0,.02f,-.07f),new Vector3(.08f,.08f,.01f),iron,Vector3.zero);
            Part("Key slot",PrimitiveType.Cube,new Vector3(0,-.04f,-.07f),new Vector3(.04f,.09f,.01f),iron,Vector3.zero);
            foreach(float y in new[]{-.18f,.18f})Part("Fixing screw",PrimitiveType.Sphere,new Vector3(0,y,-.069f),new Vector3(.029f,.029f,.014f),cream,Vector3.zero);
        }else if(kind=="pack-watch"){
            // Brass hunter-less open-face watch: case, bezel, the dial the player reads with Q, crown and bow.
            // Case and bezel behind the face, each a step forward, so nothing shares the dial's plane.
            Part("Watch case",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.5f,.035f,.5f),brass,new Vector3(90,0,0));
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
            Part("Clock timber case",PrimitiveType.Cube,Vector3.zero,new Vector3(.52f,.72f,.16f),new Color(.28f,.16f,.075f),Vector3.zero);
            Part("Clock dial",PrimitiveType.Cylinder,new Vector3(0,.1f,-.094f),new Vector3(.42f,.018f,.42f),cream,new Vector3(90,0,0));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Part("Hour marker",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.17f,.1f+Mathf.Cos(a)*.17f,-.117f),new Vector3(.016f,.033f,.006f),iron,new Vector3(0,0,-i*30));}
            Part("Hour hand",PrimitiveType.Cube,new Vector3(.04f,.14f,-.13f),new Vector3(.014f,.12f,.007f),iron,new Vector3(0,0,-45));Part("Minute hand",PrimitiveType.Cube,new Vector3(-.065f,.08f,-.137f),new Vector3(.15f,.012f,.007f),iron,new Vector3(0,0,18));
            // Same build as the kitchen clock: a framed lower door with the stopped pendulum resting to one side inside the case.
            var recess=new Color(.16f,.09f,.04f);
            Part("Pendulum recess",PrimitiveType.Cube,new Vector3(0,-.238f,-.074f),new Vector3(.42f,.2f,.01f),recess,Vector3.zero);
            foreach(float y in new[]{-.13f,-.346f})Part("Glazed door rail",PrimitiveType.Cube,new Vector3(0,y,-.088f),new Vector3(.44f,.024f,.016f),new Color(.32f,.19f,.09f),Vector3.zero);
            foreach(float x in new[]{-.21f,.21f})Part("Glazed door stile",PrimitiveType.Cube,new Vector3(x,-.238f,-.088f),new Vector3(.024f,.24f,.016f),new Color(.32f,.19f,.09f),Vector3.zero);
            Part("Pendulum rod",PrimitiveType.Cube,new Vector3(.014f,-.2f,-.08f),new Vector3(.01f,.15f,.006f),brass,new Vector3(0,0,10));
            Part("Pendulum bob",PrimitiveType.Cylinder,new Vector3(.028f,-.285f,-.08f),new Vector3(.08f,.008f,.08f),brass,new Vector3(90,0,0));
        }
        return root;
    }
}
