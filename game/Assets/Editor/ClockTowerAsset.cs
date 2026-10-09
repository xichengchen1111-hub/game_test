using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Local reconstruction from the approved illustration, using the milk-shop asset workflow.
public static class ClockTowerAsset {
    const string Folder="Assets/Art/ClockTower1931/";
    public const string ModelPath=Folder+"ClockTower1931.fbx";

    public static GameObject Create(Transform parent) {
        var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if(!importer)throw new InvalidOperationException("Build the local reference model with tools/clocktower/build_exterior.py first.");
        if(importer.materialImportMode!=ModelImporterMaterialImportMode.ImportStandard||!importer.isReadable||importer.importAnimation) {
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.isReadable=true;
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.SaveAndReimport();
        }
        // Prepare textures before instantiation: first-time normal-map imports can
        // refresh prefab instances and discard an already-applied axis correction.
        string[] materialNames={"Tower_IvoryBrick","Tower_CreamStone","Tower_Slate","Tower_Lead","Tower_DarkOak","Tower_Recess","Tower_Bronze"};
        var prepared=materialNames.ToDictionary(n=>n,n=>MaterialFor(n));
        var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
        visual.name="Reference-model ivory clock tower";visual.transform.SetParent(parent,false);
        var markers=visual.GetComponentsInChildren<Transform>();
        var front=markers.Single(t=>t.name=="FrontMarker");var east=markers.Single(t=>t.name=="EastMarker");
        Debug.Log("TOWER_IMPORT_AXES: front="+front.position.ToString("F4")+" east="+east.position.ToString("F4")+" rootrotation="+visual.transform.localEulerAngles+" scale="+visual.transform.localScale);
        var direction=front.position-visual.transform.position;direction.y=0;
        visual.transform.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(direction,Vector3.back,Vector3.up),Vector3.up)*visual.transform.localRotation;
        if(Vector3.Distance(parent.InverseTransformPoint(front.position),new Vector3(0,.12f,-1.8f))>.01f||
           Vector3.Distance(parent.InverseTransformPoint(east.position),new Vector3(1.8f,.12f,0))>.01f)
            throw new InvalidOperationException("Clock tower FBX axes or metre scale mismatch: front="+front.position.ToString("F4")+" east="+east.position.ToString("F4"));
        foreach(var renderer in visual.GetComponentsInChildren<MeshRenderer>())
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>prepared[m.name.Split('.')[0]]).ToArray();
        foreach(var collider in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);
        var bounds=MilkShopAsset.BoundsOf(visual);
        if(Mathf.Abs(bounds.size.y-ClockTowerInstance.Height)>.10f||Mathf.Abs(bounds.min.y)>.02f)
            throw new InvalidOperationException("Clock tower geometry must rise from ground to "+ClockTowerInstance.Height+" metres: "+bounds);
        foreach(var entry in new[]{("GearFloorMarker",ClockTowerInteriorLayout.MachineFloor),("MiddleFloorMarker",ClockTowerInteriorLayout.MiddleFloor),("ClockCentreFront",ClockTowerInteriorLayout.DialHeight),("RoofPeakMarker",ClockTowerInteriorLayout.ExteriorHeight)}) {
            var marker=markers.Single(t=>t.name==entry.Item1);
            if(Mathf.Abs(parent.InverseTransformPoint(marker.position).y-entry.Item2)>.015f)
                throw new InvalidOperationException("Tower exterior/interior height mismatch: "+entry.Item1);
        }
        var dial=parent.InverseTransformPoint(markers.Single(t=>t.name=="ClockCentreFront").position);
        var chamber=parent.InverseTransformPoint(markers.Single(t=>t.name=="ChamberEastMarker").position);
        if(Mathf.Abs(dial.z+ClockTowerInteriorLayout.DialPlaneDistance)>.01f ||
            Mathf.Abs(chamber.x-ClockTowerInteriorLayout.ChamberExteriorWidth/2)>.01f)
            throw new InvalidOperationException("Projecting chamber or dial depth differs from shared physical layout: dial="+dial.ToString("F5")+" chamber="+chamber.ToString("F5"));
        CheckDoorway(visual,parent);
        Debug.Log("CLOCKTOWER_EXTERIOR_PASS: local reference reconstruction; metre scale; front/east markers; actual empty doorway; "+visual.GetComponentsInChildren<MeshRenderer>().Length+" material groups");
        return visual;
    }

    static Material MaterialFor(string importedName) {
        string name=importedName.Split('.')[0];
        string[] names={"Tower_IvoryBrick","Tower_CreamStone","Tower_Slate","Tower_Lead","Tower_DarkOak","Tower_Recess","Tower_Bronze"};
        if(!names.Contains(name))throw new InvalidOperationException("Unexpected tower material: "+importedName);
        string path=Folder+name+".mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
        mat.shader=Shader.Find("Universal Render Pipeline/Lit");
        string textureName=name.Replace("Tower_","");
        var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+textureName+"-BaseColor.png");
        string hex=name=="Tower_Lead"?"#5F686A":name=="Tower_DarkOak"?"#534434":name=="Tower_Recess"?"#333B38":name=="Tower_Bronze"?"#88704C":"#FFFFFF";
        ColorUtility.TryParseHtmlString(hex,out Color tint);mat.SetColor("_BaseColor",color?Color.white:tint);
        mat.SetTexture("_BaseMap",color);
        bool metal=name=="Tower_Lead"||name=="Tower_Bronze";
        mat.SetFloat("_Metallic",metal?.6f:0);mat.SetFloat("_Smoothness",metal?.30f:.18f);
        string normalPath=Folder+textureName+"-Normal.png";
        var normalImporter=AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if(normalImporter) {
            if(normalImporter.textureType!=TextureImporterType.NormalMap) {
                normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.SetFloat("_BumpScale",.95f);mat.EnableKeyword("_NORMALMAP");
        } else mat.DisableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(mat);return mat;
    }

    static void CheckDoorway(GameObject visual,Transform parent) {
        // Check the visible imported meshes too: collision-only holes cannot hide a solid facade.
        var temporary=visual.GetComponentsInChildren<MeshFilter>().Select(filter=>{
            var collider=filter.gameObject.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;return collider;
        }).ToArray();
        Physics.SyncTransforms();
        try {
            for(int side=0;side<4;side++)foreach(float x in new[]{-.8f,0,.8f})foreach(float y in new[]{-.8f,0,.8f}) {
                var q=Quaternion.Euler(0,side*90,0);
                var start=new Vector3(0,ClockTowerInteriorLayout.DialHeight,0)+q*new Vector3(x,y,-2.0f);
                var ray=new Ray(parent.TransformPoint(start),parent.TransformDirection(q*Vector3.back));
                foreach(var collider in temporary)if(collider.Raycast(ray,out RaycastHit hit,.60f))
                    throw new InvalidOperationException("Visible exterior blocks clock glazing aperture: side="+side+" sample="+new Vector2(x,y)+" mesh="+hit.collider.name);
            }
            foreach(float x in new[]{-.48f,0,.48f})foreach(float y in new[]{.15f,1.1f,2.23f}) {
                var ray=new Ray(parent.TransformPoint(new Vector3(x,y,-2.3f)),parent.TransformDirection(Vector3.forward));
                foreach(var collider in temporary)if(collider.Raycast(ray,out RaycastHit hit,.82f))
                    throw new InvalidOperationException("Imported tower geometry blocks the real entrance at "+new Vector2(x,y)+": "+hit.collider.name);
            }
        } finally {foreach(var collider in temporary)UnityEngine.Object.DestroyImmediate(collider);}
    }
}
