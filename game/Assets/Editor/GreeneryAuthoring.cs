using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Shared by Town and its historical window-view copy. The metre map is unchanged.
public static class GreeneryAuthoring {
    const string Art="Assets/Art/Greenery/", Generated="Assets/Generated/Greenery/";
    [Serializable] class Plan {public string source;public int modelCount;public Plant[] plants;public Surface[] surfaces;}
    [Serializable] class Plant {public string asset,owner;public float x,y,z,scale,yaw;}
    [Serializable] class Surface {public string name,kind;public float[] vertices;}
    static readonly Dictionary<string,GameObject> models=new Dictionary<string,GameObject>();
    static Material foliage,lawn,soil;
    static Material Material(string name,string texture,Color tint,float tile=1) {
        string path=Art+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",.18f);m.SetFloat("_Cull",0);
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+texture));m.SetTextureScale("_BaseMap",Vector2.one*tile);
        m.enableInstancing=true;m.doubleSidedGI=true;EditorUtility.SetDirty(m);return m;
    }
    static GameObject Model(string asset) {
        if(models.TryGetValue(asset,out var model))return model;
        string path=Art+asset+".fbx";var importer=AssetImporter.GetAtPath(path) as ModelImporter;
        if(!importer)throw new InvalidOperationException("Generate greenery kit first: "+path);
        if(!importer.isReadable||importer.importAnimation||importer.materialImportMode!=ModelImporterMaterialImportMode.None) {
            importer.isReadable=true;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
        }
        model=AssetDatabase.LoadAssetAtPath<GameObject>(path);models.Add(asset,model);return model;
    }
    // One extra authored tree outside the 217 canonical map trees (the D police court).
    // Same kit, material, LODs and trunk proxy; call after Build() so the material exists.
    public static GameObject ExtraTree(Transform parent,string asset,Vector3 local,float radius,float yaw,string name) {
        if(!foliage)throw new InvalidOperationException("Build town greenery before extra trees");
        var g=(GameObject)PrefabUtility.InstantiatePrefab(Model(asset));g.name=name;g.transform.SetParent(parent,false);
        var rs=g.GetComponentsInChildren<MeshRenderer>();
        foreach(var r in rs){r.sharedMaterial=foliage;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
        var near=rs.Single(r=>r.name.EndsWith("LOD0"));var bounds=near.bounds;float extent=Mathf.Max(bounds.extents.x,bounds.extents.z);
        g.transform.localScale=new Vector3(radius/extent,radius*2.7f/bounds.size.y,radius/extent);
        g.transform.localPosition=local;g.transform.localRotation=Quaternion.Euler(0,yaw,0);
        SetLOD(g,new[]{new Renderer[]{near},new Renderer[]{rs.Single(r=>r.name.EndsWith("LOD1"))},new Renderer[]{rs.Single(r=>r.name.EndsWith("LOD2"))}},new[]{.15f,.06f,.008f});
        var trunk=new GameObject(name+" trunk");trunk.transform.SetParent(parent,false);trunk.transform.localPosition=local;
        var c=trunk.AddComponent<CapsuleCollider>();c.radius=.13f;c.height=radius*2.7f*.6f;c.center=Vector3.up*c.height/2;
        return g;
    }
    static Mesh Save(Mesh mesh,string name) {
        mesh.name=name;string path=Generated+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static MeshRenderer MeshObject(string name,Mesh mesh,Material material,Transform parent,bool shadows) {
        var g=new GameObject(name);g.transform.SetParent(parent,false);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;
        r.shadowCastingMode=shadows?ShadowCastingMode.On:ShadowCastingMode.Off;r.receiveShadows=true;return r;
    }
    static void SetLOD(GameObject root,Renderer[][] levels,float[] thresholds) {
        var lod=root.GetComponent<LODGroup>();if(!lod)lod=root.AddComponent<LODGroup>();lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=false;
        var entries=levels.Select((r,i)=>new LOD(thresholds[i],r){fadeTransitionWidth=.18f}).ToArray();lod.SetLODs(entries);lod.RecalculateBounds();
    }
    public static GameObject Build(TownPlan town) {
        Directory.CreateDirectory(Generated);AssetDatabase.Refresh();models.Clear();
        foreach(var name in new[]{"GreeneryPalette.png","GreeneryNormal.png"}) {
            var ti=AssetImporter.GetAtPath(Art+name) as TextureImporter;
            if(!ti)throw new Exception("Missing botanical texture: "+name);
            bool normal=name.Contains("Normal");
            if(ti.maxTextureSize!=2048||ti.anisoLevel!=8||normal&&ti.textureType!=TextureImporterType.NormalMap) {
                ti.maxTextureSize=2048;ti.anisoLevel=8;ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Clamp;
                ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.SaveAndReimport();
            }
        }
        foliage=Material("GardenPalette","GreeneryPalette.png",Color.white);
        foliage.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"GreeneryNormal.png"));
        foliage.SetFloat("_BumpScale",.8f);foliage.EnableKeyword("_NORMALMAP");
        lawn=Material("Meadow","Meadow.png",Color.white);
        soil=Material("PlantingSoil","Meadow.png",new Color(.64f,.58f,.45f));
        var tex=AssetImporter.GetAtPath(Art+"Meadow.png") as TextureImporter;
        if(tex&&(tex.wrapMode!=TextureWrapMode.Repeat||tex.anisoLevel!=4)){tex.wrapMode=TextureWrapMode.Repeat;tex.anisoLevel=4;tex.SaveAndReimport();}
        var text=Resources.Load<TextAsset>("GreeneryPlan");if(!text)throw new Exception("Run tools/plan_greenery.py first.");
        var plan=JsonUtility.FromJson<Plan>(text.text);if(plan.source!=town.sourceSha256)throw new Exception("Greenery placement map hash is stale.");
        // Remove only obsolete surfaces owned by this generator, preserving unrelated assets.
        var liveSurfaces=new System.Collections.Generic.HashSet<string>(plan.surfaces.Select(s=>s.name+".asset"));
        foreach(var path in Directory.GetFiles(Generated,"Border-*.asset"))if(!liveSurfaces.Contains(Path.GetFileName(path)))AssetDatabase.DeleteAsset(path);
        var root=new GameObject("Town greenery");var trees=new GameObject("Street and garden trees");trees.transform.SetParent(root.transform,false);
        int index=0;
        foreach(var t in town.trees) {
            int variant=t.kind=="果树"?5:t.kind=="行道树"?(index/3+index%2)%4:t.kind=="公共绿地树"?new[]{4,6,1}[index%3]:index%6;string asset="Tree"+(variant+1);
            var g=(GameObject)PrefabUtility.InstantiatePrefab(Model(asset));g.name="Tree "+index+" "+t.owner;g.transform.SetParent(trees.transform,false);
            var rs=g.GetComponentsInChildren<MeshRenderer>();
            foreach(var r in rs){r.sharedMaterial=foliage;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
            var near=rs.Single(r=>r.name.EndsWith("LOD0"));var bounds=near.bounds;
            if(bounds.size.y<5||bounds.size.y>10)throw new Exception("Greenery FBX is not upright/metre scale: "+asset+" "+bounds);
            float radius=Mathf.Max(bounds.extents.x,bounds.extents.z);
            g.transform.localScale=new Vector3(t.r/radius,t.r*2.7f/bounds.size.y,t.r/radius);
            g.transform.localPosition=new Vector3(t.x,-.04f,t.z);g.transform.localRotation=Quaternion.Euler(0,index*137.508f,0);
            SetLOD(g,new[]{new Renderer[]{near},new Renderer[]{rs.Single(r=>r.name.EndsWith("LOD1"))},new Renderer[]{rs.Single(r=>r.name.EndsWith("LOD2"))}},new[]{.15f,.06f,.008f});
            // Exactly the pre-existing trunk collision, unaffected by visual crown scale.
            var trunk=new GameObject("Existing trunk collision");trunk.transform.SetParent(trees.transform,false);trunk.transform.localPosition=new Vector3(t.x,-.04f,t.z);
            var c=trunk.AddComponent<CapsuleCollider>();c.radius=.13f;c.height=t.r*2.7f*.6f;c.center=Vector3.up*c.height/2;
            index++;
        }
        foreach(var surface in plan.surfaces) {
            var mesh=new Mesh();var v=new List<Vector3>();var uv=new List<Vector2>();
            for(int i=0;i<surface.vertices.Length;i+=3){var q=new Vector3(surface.vertices[i],surface.vertices[i+1],surface.vertices[i+2]);v.Add(q);uv.Add(new Vector2(q.x,q.z)*.32f);}
            if(v.Count>65535)mesh.indexFormat=IndexFormat.UInt32;mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(Enumerable.Range(0,v.Count).ToArray(),0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var renderer=MeshObject(surface.name,Save(mesh,surface.name),surface.kind=="Lawn"?lawn:soil,root.transform,false);renderer.gameObject.isStatic=true;
        }
        // Small plants retain shared mesh assets; only placement matrices are saved.
        var groups=plan.plants.GroupBy(p=>Mathf.FloorToInt(p.x/16)+"_"+Mathf.FloorToInt(p.z/16)+"_"+p.asset);
        var batches=new List<GreeneryInstances.Batch>();
        foreach(var group in groups) {
            var plants=group.ToArray();var model=Model(plants[0].asset);
            var near=model.GetComponentsInChildren<MeshFilter>().Single(f=>f.name.EndsWith("LOD0"));
            var far=model.GetComponentsInChildren<MeshFilter>().Single(f=>f.name.EndsWith("LOD1"));
            if(near.transform.localToWorldMatrix!=far.transform.localToWorldMatrix)throw new Exception("Plant LOD transforms differ");
            var matrices=plants.Select(p=>Matrix4x4.TRS(new Vector3(p.x,p.y,p.z),Quaternion.Euler(0,p.yaw,0),Vector3.one*p.scale)*near.transform.localToWorldMatrix).ToArray();
            var bounds=new Bounds(new Vector3(plants[0].x,plants[0].y,plants[0].z),Vector3.zero);
            foreach(var p in plants){bounds.Encapsulate(new Vector3(p.x-1,p.y-.1f,p.z-1));bounds.Encapsulate(new Vector3(p.x+1,p.y+1.3f,p.z+1));}
            if(matrices.Length>1023)throw new Exception("Plant instance batch too large");
            batches.Add(new GreeneryInstances.Batch{nearMesh=near.sharedMesh,farMesh=far.sharedMesh,matrices=matrices,localBounds=bounds,grass=plants[0].asset.StartsWith("Grass")});
        }
        var instances=root.AddComponent<GreeneryInstances>();instances.material=foliage;instances.batches=batches.ToArray();int batchCount=batches.Count;
        var marker=root.AddComponent<TownGreenery>();marker.treeCount=town.trees.Length;marker.plantCount=plan.plants.Length;marker.modelCount=plan.modelCount;marker.batchCount=batchCount;marker.mapSource=plan.source;
        if(root.GetComponentsInChildren<Collider>().Length!=town.trees.Length)throw new Exception("Ground planting must not add movement blockers.");
        Debug.Log("GREENERY_AUTHORING_PASS: trees="+marker.treeCount+" plants="+marker.plantCount+" models="+marker.modelCount+" batches="+batchCount+" map="+marker.mapSource);
        return root;
    }
}
