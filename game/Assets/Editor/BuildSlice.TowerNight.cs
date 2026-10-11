using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using T = ClockTowerInteriorLayout;

// Night dress for the town clock, generated with the shared ClockTower prefab:
// opal dials lit from lamps behind the glass, their spill on the timber surround,
// mist halos and two carriage lanterns at the door (rain sheen: TownWetness).
// The plaza floodlights live on the square's four existing lamp standards.
// No shadows are added; landmark lights stay out of the twelve street pools.
public static partial class BuildSlice {
    // "Light Layer 1": the tower's exterior shell only. Floodlights and dial spill
    // use it so they never leak through the walls onto the stair or machine room.
    public const uint TowerFacadeLayer=1u<<1;
    // "Light Layer 2": the door lanterns. Received by the outer shell, door, plaque and
    // the paving around the entrance, never by the stair interior behind the wall.
    public const uint TowerLanternLayer=1u<<2;
    static readonly Color DialLamp=new Color(1f,.77f,.50f);
    const string NightArt="Assets/Art/ClockTowerInterior/Night";

    static void TowerNightDress(Transform root,GameObject exterior) {
        System.IO.Directory.CreateDirectory(NightArt);AssetDatabase.Refresh();
        int facade=0;
        if(exterior)foreach(var renderer in exterior.GetComponentsInChildren<MeshRenderer>()){renderer.renderingLayerMask|=TowerFacadeLayer|TowerLanternLayer;facade++;}
        foreach(var part in new[]{"Clock tower door hinge","Clock observation","Legible tower history","Tower entrance threshold"})
            foreach(var renderer in root.Find(part).GetComponentsInChildren<Renderer>())renderer.renderingLayerMask|=TowerLanternLayer;
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ClockTowerInterior/Glazing/Frosted opal clock glass.mat");
        var glow=DialLamp*1.75f;
        glass.SetColor("_EmissionColor",glow);glass.SetFloat("_GlowFull",glow.maxColorComponent);
        glass.SetFloat("_DialRadius",T.DialDiameter/2);glass.EnableKeyword("_EMISSION");EditorUtility.SetDirty(glass);
        var halo=HaloMaterial("Tower dial mist halo",DialLamp*.34f,new Vector2(6,17),1.4f,.9f);
        var bulb=Mat("TowerBacklightBulb","#FFE2B0",2.4f,.4f);
        var brass=Mat("TowerBacklightBrass","#8A6F45",0,.42f);
        for(int face=0;face<4;face++) {
            var mount=root.Find("Tower dial "+face);
            var pane=mount.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Live frosted clock glazing");
            // The profile is read in object space; keep the four panes out of static batching.
            GameObjectUtility.SetStaticEditorFlags(pane.gameObject,GameObjectUtility.GetStaticEditorFlags(pane.gameObject)&~StaticEditorFlags.BatchingStatic);
            var spill=Point("Clock dial spill light",Vector3.zero,DialLamp,9.5f,5.4f);
            // Out in front of the face so the glow lands on the oak posts, beams and gable boards
            // instead of only grazing them from the dial axis.
            spill.transform.SetParent(mount,false);spill.transform.localPosition=new Vector3(0,-.15f,-(T.DialPlaneDistance+1.35f));
            FacadeOnly(spill);
            var haze=HaloQuad("Clock dial mist halo",mount,new Vector3(0,0,-(T.DialPlaneDistance+.40f)),7.2f,halo);
            // Two brass reflector lamps hang from the chamber ceiling behind each dial.
            var bulbs=new List<Renderer>{pane,haze};
            foreach(float x in new[]{-.62f,.62f}) {
                var lamp=Group("Dial backlight lamp",new Vector3(x,T.InnerWallHeight-T.DialHeight-.24f,-(T.ChamberInteriorWidth/2-.42f)),mount).transform;
                Cylinder("Ceiling drop rod",new Vector3(0,.12f,0),new Vector3(.018f,.12f,.018f),brass,lamp);
                var shade=Cylinder("Brass reflector shade",new Vector3(0,-.02f,-.03f),new Vector3(.26f,.045f,.26f),brass,lamp);shade.transform.localRotation=Quaternion.Euler(-58,0,0);
                var globe=Cylinder("Backlight lamp bulb",new Vector3(0,-.07f,-.08f),new Vector3(.09f,.05f,.09f),bulb,lamp);globe.transform.localRotation=Quaternion.Euler(-58,0,0);
                bulbs.Add(globe.GetComponent<Renderer>());
            }
            var controller=DayNightLighting.Bind(spill,DayNightLamp.Role.Exterior,.85f,bulbs.ToArray());controller.sharesStreetPoolBudget=false;
        }
        TowerDoorLanterns(root);
        Debug.Log("CLOCKTOWER_NIGHT_DRESS_READY: 4 backlit opal dials, 4 facade-layer spill lights, 8 backlight fittings, 2 door lanterns, "+facade+" facade renderers on the light layers; no shadows");
    }

    static void TowerDoorLanterns(Transform root) {
        var iron=Mat("TowerLanternIron","#24282B",0,.38f);
        var glassGlow=Mat("TowerLanternGlass","#FFD39C",2.2f,.6f);
        var halo=HaloMaterial("Tower lantern mist halo",new Color(1,.78f,.52f)*.42f,new Vector2(1.6f,6),.35f,.25f);
        float face=-ClockTowerInstance.OuterWidth/2-.014f; // limewashed pier surface
        foreach(int side in new[]{-1,1}) {
            // Clear of the open door leaf (sweeps to x=-.79) and above the bronze plaque.
            var lantern=Group("Tower door carriage lantern",new Vector3(side*1.18f,2.36f,face),root).transform;
            Box("Lantern wall plate",new Vector3(0,.20f,-.012f),new Vector3(.11f,.30f,.024f),iron,lantern,false);
            Box("Lantern bracket arm",new Vector3(0,.30f,-.15f),new Vector3(.03f,.03f,.28f),iron,lantern,false);
            var stay=Box("Lantern bracket stay",new Vector3(0,.17f,-.10f),new Vector3(.022f,.022f,.25f),iron,lantern,false);stay.transform.localRotation=Quaternion.Euler(-34,0,0);
            Box("Lantern hook",new Vector3(0,.25f,-.28f),new Vector3(.018f,.10f,.018f),iron,lantern,false);
            Box("Lantern roof",new Vector3(0,.175f,-.28f),new Vector3(.20f,.04f,.20f),iron,lantern,false);
            var cap=Box("Lantern cap",new Vector3(0,.21f,-.28f),new Vector3(.12f,.05f,.12f),iron,lantern,false);cap.transform.localRotation=Quaternion.Euler(0,45,0);
            var pane=Box("Lantern glowing glass",new Vector3(0,.03f,-.28f),new Vector3(.15f,.25f,.15f),glassGlow,lantern,false);
            foreach(var corner in new[]{new Vector2(-1,-1),new Vector2(1,-1),new Vector2(-1,1),new Vector2(1,1)})
                Box("Lantern corner post",new Vector3(corner.x*.077f,.03f,-.28f+corner.y*.077f),new Vector3(.016f,.27f,.016f),iron,lantern,false);
            Box("Lantern base",new Vector3(0,-.115f,-.28f),new Vector3(.17f,.04f,.17f),iron,lantern,false);
            Box("Lantern finial",new Vector3(0,-.16f,-.28f),new Vector3(.03f,.05f,.03f),iron,lantern,false);
            var light=Point("Tower door lantern light",Vector3.zero,new Color(1,.78f,.52f),1.5f,4.6f);
            light.transform.SetParent(lantern,false);light.transform.localPosition=new Vector3(0,.02f,-.36f);
            var haze=HaloQuad("Lantern mist halo",lantern,new Vector3(0,.03f,-.30f),1.25f,halo);
            LayerOnly(light,TowerLanternLayer);
            DayNightLighting.Bind(light,DayNightLamp.Role.Exterior,.85f,pane.GetComponent<Renderer>(),haze);
            foreach(var t in lantern.GetComponentsInChildren<Transform>())t.gameObject.isStatic=t.GetComponent<Light>()==null&&t.name!="Lantern mist halo";
        }
    }

    // Small cast-iron projectors on the four square lamp standards, aimed at the tower.
    // From ~15 m they wash both nearest faces evenly; only the facade layer receives them.
    static void TowerFloodlights(Vector3[] standards) {
        var iron=Mat("TowerFloodIron","#202427",0,.40f);
        var lens=Mat("TowerFloodLens","#FFE6BF",2.6f,.7f);
        var target=new Vector3(0,9.0f,0);
        foreach(var standard in standards) {
            var flat=-standard;flat.y=0;var yaw=Quaternion.LookRotation(flat.normalized,Vector3.up);
            var mount=Group("Tower floodlight projector",standard+Vector3.up*2.52f,null).transform;mount.rotation=yaw;
            Box("Floodlight post collar",new Vector3(0,0,0),new Vector3(.16f,.10f,.16f),iron,mount,false);
            Box("Floodlight bracket",new Vector3(0,0,.22f),new Vector3(.04f,.04f,.34f),iron,mount,false);
            Box("Floodlight yoke",new Vector3(0,.06f,.40f),new Vector3(.30f,.03f,.04f),iron,mount,false);
            foreach(int s in new[]{-1,1})Box("Floodlight yoke arm",new Vector3(s*.14f,.11f,.40f),new Vector3(.025f,.13f,.04f),iron,mount,false);
            var head=Group("Floodlight head",new Vector3(0,.16f,.40f),mount).transform;
            var origin=head.position;head.rotation=Quaternion.LookRotation((target-origin).normalized,Vector3.up);
            var body=Cylinder("Floodlight housing",new Vector3(0,0,-.04f),new Vector3(.24f,.15f,.24f),iron,head);body.transform.localRotation=Quaternion.Euler(90,0,0);
            var rim=Cylinder("Floodlight rim",new Vector3(0,0,.118f),new Vector3(.27f,.02f,.27f),iron,head);rim.transform.localRotation=Quaternion.Euler(90,0,0);
            var glass=Cylinder("Floodlight lens",new Vector3(0,0,.140f),new Vector3(.21f,.012f,.21f),lens,head);glass.transform.localRotation=Quaternion.Euler(90,0,0);
            Box("Floodlight cooling fin",new Vector3(0,.13f,-.05f),new Vector3(.03f,.04f,.22f),iron,head,false);
            var spot=new GameObject("Tower floodlight").AddComponent<Light>();spot.transform.SetParent(head,false);spot.transform.localPosition=new Vector3(0,0,.16f);
            spot.type=LightType.Spot;spot.color=new Color(1,.86f,.68f);spot.intensity=72;spot.range=32;spot.spotAngle=62;spot.innerSpotAngle=30;spot.shadows=LightShadows.None;
            FacadeOnly(spot);
            var controller=DayNightLighting.Bind(spot,DayNightLamp.Role.Exterior,.85f,glass.GetComponent<Renderer>());controller.sharesStreetPoolBudget=false;
            foreach(var t in mount.GetComponentsInChildren<Transform>())t.gameObject.isStatic=t.GetComponent<Light>()==null;
        }
        // The paving chunks around the entrance also take the door lanterns' pools.
        var entrance=new Bounds(new Vector3(0,0,-3),new Vector3(12,2,10));int paving=0;
        foreach(var renderer in GameObject.Find("Street surfaces").GetComponentsInChildren<MeshRenderer>())
            if(renderer.bounds.Intersects(entrance)){renderer.renderingLayerMask|=TowerLanternLayer;paving++;}
        if(paving==0)throw new InvalidOperationException("No street paving receives the tower door lanterns");
        Debug.Log("CLOCKTOWER_FLOODLIGHTS_READY: "+paving+" lantern-lit paving chunks; "+standards.Length+" unshadowed facade-layer projectors on existing square lamp standards");
    }

    static void FacadeOnly(Light light)=>LayerOnly(light,TowerFacadeLayer);
    static void LayerOnly(Light light,uint layers) {
        var data=light.GetUniversalAdditionalLightData();data.renderingLayers=layers;
        light.renderingLayerMask=(int)layers;
        EditorUtility.SetDirty(data);
    }

    static Material HaloMaterial(string name,Color colour,Vector2 nearFade,float softness,float pull) {
        string path=NightArt+"/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader=Shader.Find("WrongDate/LampHalo");
        if(!shader)throw new InvalidOperationException("WrongDate/LampHalo shader missing");
        if(!material){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;material.SetColor("_EmissionColor",colour);material.EnableKeyword("_EMISSION");
        material.SetVector("_NearFade",new Vector4(nearFade.x,nearFade.y,0,0));material.SetFloat("_Softness",softness);material.SetFloat("_Pull",pull);
        EditorUtility.SetDirty(material);return material;
    }

    static Renderer HaloQuad(string name,Transform parent,Vector3 position,float size,Material material) {
        // A billboard rebuilt in the vertex shader: give it cube bounds for culling,
        // and keep it dynamic so static batching never bakes away its centre.
        string path=NightArt+"/Halo quad.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh) {
            mesh=new Mesh{name="Halo quad"};
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
            mesh.triangles=new[]{0,2,1,0,3,2};mesh.bounds=new Bounds(Vector3.zero,Vector3.one*1.2f);
            AssetDatabase.CreateAsset(mesh,path);
        }
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*size;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;go.isStatic=false;
        return renderer;
    }
}
