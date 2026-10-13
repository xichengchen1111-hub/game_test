using UnityEngine;
public static class EvidenceSolidModel {
    public static GameObject Create(string kind,Texture2D image=null){
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
        }else{
            Part("Clock timber case",PrimitiveType.Cube,Vector3.zero,new Vector3(.52f,.72f,.16f),new Color(.28f,.16f,.075f),Vector3.zero);
            Part("Clock dial",PrimitiveType.Cylinder,new Vector3(0,.1f,-.094f),new Vector3(.42f,.018f,.42f),cream,new Vector3(90,0,0));
            for(int i=0;i<12;i++){float a=i*Mathf.PI/6;Part("Hour marker",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.17f,.1f+Mathf.Cos(a)*.17f,-.117f),new Vector3(.016f,.033f,.006f),iron,new Vector3(0,0,-i*30));}
            Part("Hour hand",PrimitiveType.Cube,new Vector3(.04f,.14f,-.13f),new Vector3(.014f,.12f,.007f),iron,new Vector3(0,0,-45));Part("Minute hand",PrimitiveType.Cube,new Vector3(-.065f,.08f,-.137f),new Vector3(.15f,.012f,.007f),iron,new Vector3(0,0,18));
            Part("Pendulum",PrimitiveType.Cylinder,new Vector3(0,-.21f,-.1f),new Vector3(.10f,.012f,.10f),brass,new Vector3(90,0,0));
        }
        return root;
    }
}
