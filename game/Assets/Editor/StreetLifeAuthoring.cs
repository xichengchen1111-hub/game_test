using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Street life resources (user, 2026-10-15), all generated here so nothing is hand-edited:
//  - townsfolk: the street cast made in Tripo and approved on 2026-10-16 (StreetCastAuthoring): a
//    businessman, a young office clerk and a housewife, a delivery workman, an old gentleman with his
//    stick, a boy and a girl, a woman walking her terrier, and a man down on his luck who begs by the
//    station; the delivery workman also turns the barrel organ. A kind whose street figure is not (or
//    no longer) approved falls back to the hospital stand-in named 临时模型 in docs, or is left out
//    (the children and the dog walker have none). The old woman is still the hospital's visitor.
//    Each keeps its idle/walk/talk clips; unlike story characters they stop animating out of view and
//    skip off-screen bounds.
//  - cars: the hotel car's meshes and pivots without its controls or physics, a driver from the street
//    cast posed at the wheel with three left-arm poses for the hand signals, the driver's window wound
//    down, paint changed at runtime.
//  - props: umbrella, walking stick, crate, cap and the barrel organ, with saved materials.
public static class StreetLifeAuthoring {
    const string Root="Assets/Resources/StreetLife/";
    const string Generated="Assets/Generated/StreetLife/";
    // Each kind's street figures in order of preference, and the hospital stand-in if none is approved.
    static readonly (StreetDensity.Folk kind,string title,string[] street,string standIn)[] Folk={
        (StreetDensity.Folk.Man,"路人",new[]{"Businessman"},"VisitorMan"),
        (StreetDensity.Folk.Woman,"路人",new[]{"YoungWoman","Housewife"},"VisitorWoman"),
        (StreetDensity.Folk.OldWoman,"老妇人",new string[0],"VisitorGranny"),
        (StreetDensity.Folk.OldMan,"老先生",new[]{"OldGentleman"},"VisitingDoctor"),
        (StreetDensity.Folk.Worker,"路人",new[]{"Labourer"},"Porter"),
        (StreetDensity.Folk.Boy,"男孩",new[]{"StreetBoy"},null),
        (StreetDensity.Folk.Girl,"女孩",new[]{"StreetGirl"},null),
        (StreetDensity.Folk.DogWalker,"遛狗的妇人",new[]{"DogWalker"},null),
        (StreetDensity.Folk.Beggar,"乞讨的人",new[]{"Transient"},"VisitorMan"),
        (StreetDensity.Folk.Musician,"摇风琴的艺人",new[]{"Labourer"},"Porter")};
    static readonly (string street,string standIn)[] Drivers={("Businessman","VisitorMan"),("OldGentleman","VisitingDoctor"),("Labourer","Porter")};
    // A figure of either cast: folder and measurements.
    static (string folder,float height,float width)? Figure(string street,string standIn) {
        if(street!=null&&StreetCastAuthoring.Ready(street)){var size=StreetCastAuthoring.Size[street];return (StreetCastAuthoring.Folder,size.height,size.width);}
        if(standIn!=null&&HospitalCastAuthoring.Ready(standIn)){var size=HospitalCastAuthoring.Size[standIn];return (HospitalCastAuthoring.Folder,size.height,size.width);}
        return null;
    }
    public static void Build() {
        foreach(var dir in new[]{Root+"Folk",Root+"Cars",Generated})Directory.CreateDirectory(dir);
        foreach(var old in Directory.GetFiles(Root+"Folk","*.prefab").Concat(Directory.GetFiles(Root+"Cars","*.prefab")))AssetDatabase.DeleteAsset(old.Replace('\\','/'));
        int folk=0,cars=0;bool dogReady=StreetCastAuthoring.DogReady();
        foreach(var (kind,title,street,standIn) in Folk) {
            var ids=street.Where(StreetCastAuthoring.Ready).ToList();bool borrowed=false;
            if(ids.Count==0&&standIn!=null&&HospitalCastAuthoring.Ready(standIn)){ids.Add(standIn);borrowed=true;}
            if(kind==StreetDensity.Folk.DogWalker&&!dogReady){Debug.LogWarning("STREET_LIFE_FOLK_PENDING: the terrier is not approved; nobody walks a dog.");continue;}
            if(ids.Count==0){Debug.LogWarning("STREET_LIFE_FOLK_PENDING: no approved figure for "+kind+"; it is left out.");continue;}
            foreach(var id in ids) {
                var holder=new GameObject("Street folk holder");
                try {
                    var figure=borrowed?(HospitalCastAuthoring.Folder,HospitalCastAuthoring.Size[id].height,HospitalCastAuthoring.Size[id].width):(StreetCastAuthoring.Folder,StreetCastAuthoring.Size[id].height,StreetCastAuthoring.Size[id].width);
                    var actor=PeriodCharacterAsset.Create(holder.transform,Vector3.zero,figure.Item1,id,title,figure.Item2,figure.Item3);
                    actor.transform.SetParent(null,false);actor.name=kind+" · "+id+(borrowed?" · 临时模型":"");
                    Streetwise(actor,kind,figure.Item2);
                    PrefabUtility.SaveAsPrefabAsset(actor,Root+"Folk/"+kind+"__"+id+".prefab");folk++;
                    UnityEngine.Object.DestroyImmediate(actor);
                } finally {UnityEngine.Object.DestroyImmediate(holder);}
            }
        }
        for(int i=0;i<Drivers.Length;i++) {
            var (street,standIn)=Drivers[i];var figure=Figure(street,standIn);if(figure==null)continue;
            BuildCar(i,StreetCastAuthoring.Ready(street)?street:standIn,figure.Value.folder,figure.Value.height,figure.Value.width);cars++;
        }
        BuildProps();
        AssetDatabase.SaveAssets();
        Debug.Log("STREET_LIFE_AUTHORING_PASS: folk="+folk+" cars="+cars+" dog="+dogReady);
    }
    static void Streetwise(GameObject actor,StreetDensity.Folk kind,float height) {
        foreach(var c in actor.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true)){skin.updateWhenOffscreen=false;skin.renderingLayerMask=1;}
        foreach(var r in actor.GetComponentsInChildren<Renderer>(true))r.renderingLayerMask=1;
        foreach(var animator in actor.GetComponentsInChildren<Animator>(true))animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
        var performance=actor.GetComponentInChildren<PeriodCharacterPerformance>();
        if(performance){performance.speaker=kind==StreetDensity.Folk.Beggar?"乞讨的人":kind==StreetDensity.Folk.Musician?"艺人":"路人";performance.gestureSeconds=1.2f;}
        var body=actor.AddComponent<CapsuleCollider>();body.radius=height<1.4f?.2f:.25f;body.height=height;body.center=new Vector3(0,height*.5f,0);
        var rb=actor.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var hands=actor.AddComponent<NpcHands>();
        if(kind==StreetDensity.Folk.Beggar||kind==StreetDensity.Folk.Musician)return;
        var walker=actor.AddComponent<StreetWalker>();walker.kind=kind;walker.performance=performance;walker.hands=hands;
        var bones=actor.GetComponentsInChildren<Transform>(true);
        walker.spine=bones.FirstOrDefault(t=>t.name=="Spine");walker.neck=bones.FirstOrDefault(t=>t.name=="Neck");
        if(kind==StreetDensity.Folk.DogWalker){var dog=StreetCastAuthoring.AddDog(actor);dog.owner=walker;walker.dog=dog;}
    }

    // ---- Cars ----
    static void BuildCar(int index,string driverId,string folder,float height,float width) {
        var root=PrefabUtility.LoadPrefabContents(BuildSlice.CarPrefabPath);
        try {
            root.name="Street car "+index;
            root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var original=root.GetComponent<HotelCar>();
            if(!original)throw new Exception("Street cars require the hotel car prefab.");
            var car=root.AddComponent<AmbientCar>();
            car.parts=original.parts.Where(p=>p.partId.StartsWith("Wheel_")||p.partId=="SteeringWheel").ToArray();
            car.lampRenderers=original.lampRenderers;car.lampSlots=original.lampSlots;car.lampKinds=original.lampKinds;
            // The driver's window wound down for the hand signals.
            var glass=original.parts.FirstOrDefault(p=>p.partId=="Glass_FL");if(glass)glass.Snap(1);
            foreach(var part in original.parts){part.fixtureId="";part.leaves=new BoxCollider[0];}
            if(original.windshieldRain)UnityEngine.Object.DestroyImmediate(original.windshieldRain.gameObject);
            if(original.shelter)UnityEngine.Object.DestroyImmediate(original.shelter.gameObject);
            foreach(var light in root.GetComponentsInChildren<Light>(true))UnityEngine.Object.DestroyImmediate(light.gameObject);
            foreach(var wheel in root.GetComponentsInChildren<WheelCollider>(true))UnityEngine.Object.DestroyImmediate(wheel.gameObject);
            foreach(var interaction in root.GetComponentsInChildren<Interactable>(true))UnityEngine.Object.DestroyImmediate(interaction);
            foreach(var collider in root.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
            foreach(var seat in root.GetComponentsInChildren<CarSeatPoint>(true))UnityEngine.Object.DestroyImmediate(seat);
            foreach(var sounds in root.GetComponentsInChildren<CarSounds>(true))UnityEngine.Object.DestroyImmediate(sounds);
            UnityEngine.Object.DestroyImmediate(original);
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Rigidbody>());
            var solid=root.AddComponent<BoxCollider>();solid.center=new Vector3(0,.88f,-.17f);solid.size=new Vector3(1.78f,1.76f,4.3f);
            var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
            Driver(root.transform,car,driverId,folder,height,width);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.renderingLayerMask=1;
            PrefabUtility.SaveAsPrefabAsset(root,Root+"Cars/StreetCar"+index+"__"+driverId+".prefab");
        } finally {PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Driver(Transform parent,AmbientCar car,string id,string folder,float height,float width) {
        var actor=PeriodCharacterAsset.Create(parent,Vector3.zero,folder,id,"司机",height,width);
        actor.name="司机 · "+id+(folder==HospitalCastAuthoring.Folder?" · 临时模型":"");
        foreach(var c in actor.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
        foreach(var p in actor.GetComponentsInChildren<PeriodCharacterPerformance>(true)){p.gestures=false;p.speaker="司机";}
        foreach(var a in actor.GetComponentsInChildren<Animator>(true))a.enabled=false;
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.updateWhenOffscreen=false;
        actor.transform.localRotation=Quaternion.Euler(0,180,0);
        var bones=actor.GetComponentsInChildren<Transform>(true);
        Transform Bone(string name)=>bones.Single(t=>t.name==name);
        Vector3 Point(float x,float y,float z)=>parent.TransformPoint(new Vector3(x,y,z));
        var hips=Bone("Hips");
        actor.transform.position+=Point(-.30f,.82f,-.52f)-hips.position;
        // The same seated pose as the lawyer at the wheel (OpeningRideCarAuthoring.AddDriver).
        PoseLimb(Bone("Left_UpperLeg"),Bone("Left_LowerLeg"),Bone("Left_Foot"),Point(-.43f,.47f,.32f),Point(-.43f,.78f,.12f));
        PoseLimb(Bone("Right_UpperLeg"),Bone("Right_LowerLeg"),Bone("Right_Foot"),Point(-.18f,.47f,.32f),Point(-.18f,.78f,.12f));
        PoseLimb(Bone("Right_UpperArm"),Bone("Right_LowerArm"),Bone("Right_Hand"),Point(-.12f,1.10f,-.085f),Point(-.04f,1.06f,-.43f));
        var arm=new[]{Bone("Left_UpperArm"),Bone("Left_LowerArm"),Bone("Left_Hand")};
        var rest=arm.Select(b=>b.localRotation).ToArray();
        var poses=new List<Quaternion>();
        // 0 on the wheel; 1 left turn, the arm straight out of the window; 2 right turn, the forearm up;
        // 3 slowing or stopping, the hand down (the 1926 code's three signals).
        (Vector3 hand,Vector3 elbow)[] targets={
            (Point(-.48f,1.10f,-.085f),Point(-.58f,1.06f,-.43f)),
            (Point(-1.2f,1.22f,-.52f),Point(-.8f,1.32f,-.62f)),
            (Point(-.86f,1.62f,-.5f),Point(-.95f,1.22f,-.55f)),
            (Point(-.9f,.86f,-.5f),Point(-.86f,1.3f,-.6f))};
        foreach(var (hand,elbow) in targets) {
            for(int i=0;i<arm.Length;i++)arm[i].localRotation=rest[i];
            PoseLimb(arm[0],arm[1],arm[2],hand,elbow);
            poses.AddRange(arm.Select(b=>b.localRotation));
        }
        for(int i=0;i<arm.Length;i++)arm[i].localRotation=poses[i];
        car.armBones=arm;car.armPoses=poses.ToArray();
    }
    static void PoseLimb(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint) {
        float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,end.position);
        var forward=(target-upper.position).normalized;
        float distance=Mathf.Clamp(Vector3.Distance(upper.position,target),Mathf.Abs(a-b)+.001f,a+b-.001f);
        var bend=Vector3.ProjectOnPlane(hint-upper.position,forward).normalized;
        if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.up,forward).normalized;
        float along=(a*a+distance*distance-b*b)/(2*distance);
        var joint=upper.position+forward*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,joint-upper.position)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(end.position-lower.position,target-lower.position)*lower.rotation;
    }

    // ---- Props ----
    static Material Mat(string name,Color colour,float smooth=.25f,float metal=0) {
        string path=Generated+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",colour);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);EditorUtility.SetDirty(m);return m;
    }
    static GameObject Part(Transform parent,string name,PrimitiveType shape,Vector3 pos,Vector3 size,Material m,Vector3 euler=default) {
        var o=GameObject.CreatePrimitive(shape);o.name=name;UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        o.transform.SetParent(parent,false);o.transform.localPosition=pos;o.transform.localScale=size;o.transform.localEulerAngles=euler;
        var r=o.GetComponent<MeshRenderer>();r.sharedMaterial=m;r.renderingLayerMask=1;return o;
    }
    static void BuildProps() {
        var root=new GameObject("StreetProps");
        try {
            var black=Mat("UmbrellaCloth",new Color(.05f,.05f,.055f),.35f);var wood=Mat("StickWood",new Color(.22f,.14f,.08f),.4f);
            var brass=Mat("Ferrule",new Color(.55f,.42f,.22f),.55f,.6f);
            // Umbrella: a shallow eight-panel canopy over a stick, the handle at the origin.
            var umbrella=new GameObject("Umbrella").transform;umbrella.SetParent(root.transform,false);
            // The origin is where the hand grips the shaft (at the chest): the hook below, the crown 0.72 m above.
            var canopy=new GameObject("Canopy");canopy.transform.SetParent(umbrella,false);canopy.transform.localPosition=new Vector3(0,.72f,0);
            canopy.AddComponent<MeshFilter>().sharedMesh=SaveMesh(Canopy(),"UmbrellaCanopy");var cr=canopy.AddComponent<MeshRenderer>();cr.sharedMaterial=black;cr.renderingLayerMask=1;
            Part(umbrella,"Stick",PrimitiveType.Cylinder,new Vector3(0,.235f,0),new Vector3(.018f,.49f,.018f),wood);
            Part(umbrella,"Tip",PrimitiveType.Cylinder,new Vector3(0,.76f,0),new Vector3(.012f,.04f,.012f),brass);
            Part(umbrella,"Handle hook",PrimitiveType.Cylinder,new Vector3(0,-.27f,.03f),new Vector3(.022f,.035f,.022f),wood,new Vector3(90,0,0));
            // Crate and cap (the beggar's), sized by StreetBeggar.
            Part(root.transform,"Crate",PrimitiveType.Cube,Vector3.zero,new Vector3(.5f,.38f,.42f),Mat("CrateWood",new Color(.36f,.27f,.17f),.15f));
            Part(root.transform,"Cap",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.24f,.035f,.24f),Mat("CapCloth",new Color(.18f,.17f,.16f),.1f));
            // The barrel organ on its pole; the crank turns about the box's side axis (StreetOrgan).
            var organ=new GameObject("BarrelOrgan").transform;organ.SetParent(root.transform,false);
            var caseWood=Mat("OrganCase",new Color(.36f,.16f,.1f),.45f);var panel=Mat("OrganPanel",new Color(.62f,.48f,.25f),.4f);
            Part(organ,"Pole",PrimitiveType.Cylinder,new Vector3(0,.45f,0),new Vector3(.05f,.45f,.05f),wood);
            Part(organ,"Case",PrimitiveType.Cube,new Vector3(0,1.1f,0),new Vector3(.54f,.42f,.3f),caseWood);
            Part(organ,"Front panel",PrimitiveType.Cube,new Vector3(0,1.1f,-.152f),new Vector3(.44f,.32f,.01f),panel);
            for(int i=0;i<7;i++)Part(organ,"Pipe "+i,PrimitiveType.Cylinder,new Vector3(-.15f+i*.05f,1.1f-.02f*Mathf.Abs(i-3),-.16f),new Vector3(.025f,.1f+.012f*(3-Mathf.Abs(i-3)),.025f),brass);
            Part(organ,"Lid",PrimitiveType.Cube,new Vector3(0,1.32f,0),new Vector3(.58f,.03f,.33f),Mat("OrganLid",new Color(.28f,.13f,.08f),.45f));
            var crank=new GameObject("Crank").transform;crank.SetParent(organ,false);crank.localPosition=new Vector3(-.3f,1.1f,0);
            var iron=Mat("CrankIron",new Color(.4f,.38f,.33f),.5f,.7f);
            Part(crank,"Crank arm",PrimitiveType.Cube,new Vector3(-.02f,.04f,0),new Vector3(.015f,.08f,.02f),iron);
            Part(crank,"Crank handle",PrimitiveType.Cylinder,new Vector3(-.06f,.08f,0),new Vector3(.03f,.04f,.03f),wood,new Vector3(0,0,90));
            PrefabUtility.SaveAsPrefabAsset(root,Root+"StreetProps.prefab");
        } finally {UnityEngine.Object.DestroyImmediate(root);}
    }
    static Mesh SaveMesh(Mesh mesh,string name) {
        string path=Generated+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){old.Clear();EditorUtility.CopySerialized(mesh,old);return old;}
        mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    // Eight panels, each sagging a little between its ribs; open below (double-sided by duplicate faces).
    static Mesh Canopy() {
        const int ribs=8,rings=4;const float radius=.52f,height=.2f;
        var v=new List<Vector3>{Vector3.zero};var t=new List<int>();
        for(int ring=1;ring<=rings;ring++)for(int k=0;k<ribs*2;k++) {
            // Ribs at even k; between them the cloth droops a little lower and inward.
            float u=ring/(float)rings,a=k*Mathf.PI/ribs;
            float r=radius*u*(k%2==1?.97f:1),y=-height*u*u*(k%2==1?1.08f:1);
            v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));
        }
        int n=ribs*2;
        for(int k=0;k<n;k++){t.Add(0);t.Add(1+(k+1)%n);t.Add(1+k);}
        for(int ring=1;ring<rings;ring++)for(int k=0;k<n;k++) {
            int a=1+(ring-1)*n+k,b=1+(ring-1)*n+(k+1)%n,c=1+ring*n+k,d=1+ring*n+(k+1)%n;
            t.Add(a);t.Add(b);t.Add(c);t.Add(b);t.Add(d);t.Add(c);
        }
        int faces=t.Count;for(int i=0;i<faces;i+=3){t.Add(t[i]);t.Add(t[i+2]);t.Add(t[i+1]);}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
}
