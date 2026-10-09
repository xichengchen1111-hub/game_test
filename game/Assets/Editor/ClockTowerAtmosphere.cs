using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using L = ClockTowerInteriorLayout;

/// <summary>Small anchored cobwebs and sparse upper-room dust; no gameplay colliders.</summary>
public static class ClockTowerAtmosphere {
    const string Art = "Assets/Art/ClockTowerInterior/Atmosphere";
    // One millimetre into the 6mm-offset wall lining avoids floating thread ends.
    const float WallAnchor = L.InteriorWidth / 2 - .005f;

    public static void Build(Transform kit) {
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        var silk = TransparentMaterial("Old grey cobweb silk", "Universal Render Pipeline/Unlit", new Color(.76f,.77f,.73f,.40f));
        var webs = Child("Upper tower cobwebs", kit);
        // Attach each outer edge to masonry or a real baluster, away from the walking line.
        MakeWeb("North east upper wall corner", webs, silk,
            new Vector3(WallAnchor,17.65f,WallAnchor), new Vector3(.63f,17.61f,WallAnchor), new Vector3(WallAnchor,16.77f,.75f), 1931, 1);
        MakeWeb("North west upper wall corner", webs, silk,
            new Vector3(-WallAnchor,17.92f,WallAnchor), new Vector3(-.80f,17.75f,WallAnchor), new Vector3(-WallAnchor,17.0f,.50f), 1816, .9f);
        MakeWeb("South west upper wall corner", webs, silk,
            new Vector3(-WallAnchor,17.60f,-WallAnchor), new Vector3(-.72f,17.51f,-WallAnchor), new Vector3(-WallAnchor,16.92f,-.80f), 422, .85f);
        foreach (int step in new[] { 76, 84, 90 }) {
            float angle = L.StartAngle + (step + .5f) * L.StepAngle;
            float top = L.StepTop(step);
            Vector3 rail = L.Polar(angle,1.35f,top+.93f);
            Vector3 lower = L.Polar(angle,1.35f,top+.35f);
            Vector3 direction = L.Polar(angle,1,0);
            float wallRadius = WallAnchor / Mathf.Max(Mathf.Abs(direction.x),Mathf.Abs(direction.z));
            Vector3 wall = L.Polar(angle,wallRadius,top+.88f);
            MakeWeb("Upper baluster outside web " + step,webs,silk,rail,wall,lower,step*271,.55f);
        }
        MakeDust(kit);
        Debug.Log("CLOCKTOWER_ATMOSPHERE_READY: six anchored upper webs; sparse local-space dust capped at 32; no decoration colliders");
    }

    static Transform Child(string name, Transform parent) {
        var go = new GameObject(name); go.transform.SetParent(parent,false); return go.transform;
    }

    static Material TransparentMaterial(string name, string shaderName, Color color) {
        string path = Art + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        var shader = Shader.Find(shaderName);
        if (!shader) throw new System.InvalidOperationException("Required tower atmosphere shader missing: " + shaderName);
        if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat,path); }
        mat.shader=shader; mat.SetColor("_BaseColor",color);
        mat.SetFloat("_Surface",1); mat.SetFloat("_Blend",0);
        mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite",0); mat.SetFloat("_Cull",(float)CullMode.Off);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHATEST_ON"); mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.SetOverrideTag("RenderType","Transparent"); mat.renderQueue=(int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat); return mat;
    }

    static void MakeWeb(string name, Transform parent, Material silk, Vector3 a, Vector3 b, Vector3 c, int seed, float density) {
        var rng = new System.Random(seed);
        var vertices = new List<Vector3>(); var triangles = new List<int>();
        Vector3 normal=Vector3.Cross(b-a,c-a).normalized;
        float Random() => (float)rng.NextDouble();
        Vector3 Edge(Vector3 x, Vector3 y) => Vector3.Lerp(x,y,.06f+Random()*.88f);
        // Irregular slack cross-strands resemble old house cobwebs rather than an orb-web symbol.
        void Strand(Vector3 start, Vector3 end, float width, float sag) {
            Vector3 previous=start;
            for(int j=1;j<=5;j++) {
                float t=j/5f;
                Vector3 next=Vector3.Lerp(start,end,t)+Vector3.down*(Mathf.Sin(t*Mathf.PI)*sag)
                    +normal*((Random()-.5f)*.013f*Mathf.Sin(t*Mathf.PI));
                Vector3 side=Vector3.Cross(next-previous,normal).normalized*width*.5f;
                int n=vertices.Count;
                vertices.Add(previous-side);vertices.Add(previous+side);vertices.Add(next+side);vertices.Add(next-side);
                triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
                previous=next;
            }
        }
        Strand(a,b,.0020f,.013f); Strand(a,c,.0017f,.017f); Strand(b,c,.0017f,.035f);
        int strands=Mathf.RoundToInt(62*density);
        for(int i=0;i<strands;i++) {
            Vector3 start,end;
            if(i%3==0) {start=Edge(a,b);end=Edge(a,c);}
            else if(i%3==1) {start=Edge(a,b);end=Edge(b,c);}
            else {start=Edge(a,c);end=Edge(b,c);}
            // Sparse missing links prevent a dense triangular veil across the view.
            if(i%8==3) end=Vector3.Lerp(start,end,.45f+Random()*.30f);
            Strand(start,end,.0007f+Random()*.0012f,.007f+Random()*.025f);
        }
        var mesh=new Mesh { name=name };
        mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Art+"/"+name+".asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved) {EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
        else {saved=mesh;AssetDatabase.CreateAsset(saved,path);}
        var obj=Child(name,parent).gameObject;
        obj.AddComponent<MeshFilter>().sharedMesh=saved;
        var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=silk;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        obj.isStatic=true;
    }

    static void MakeDust(Transform parent) {
        const int size=64;
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false);
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
            float r=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1).magnitude;
            float alpha=Mathf.Pow(Mathf.Clamp01(1-r),2.5f);
            pixels[y*size+x]=new Color(1,1,1,alpha);
        }
        tex.SetPixels(pixels);tex.Apply();string texturePath=Art+"/SoftDustMote.png";
        File.WriteAllBytes(texturePath,tex.EncodeToPNG());Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(texturePath,ImportAssetOptions.ForceUpdate);
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
        importer.wrapMode=TextureWrapMode.Clamp;importer.mipmapEnabled=true;importer.SaveAndReimport();
        var mat=TransparentMaterial("Soft upper room dust", "Universal Render Pipeline/Particles/Unlit",Color.white);
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        mat.SetFloat("_ColorMode",0);EditorUtility.SetDirty(mat);
        var obj=Child("Upper room drifting dust",parent);obj.localPosition=new Vector3(0,15.75f,0);
        var ps=obj.gameObject.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main;main.duration=24;main.loop=true;main.prewarm=true;main.playOnAwake=true;
        main.startLifetime=new ParticleSystem.MinMaxCurve(12,22);main.startSpeed=0;
        main.startSize=new ParticleSystem.MinMaxCurve(.009f,.019f);
        main.startColor=new Color(.90f,.91f,.87f,.50f);main.maxParticles=32;
        main.simulationSpace=ParticleSystemSimulationSpace.Local;main.cullingMode=ParticleSystemCullingMode.PauseAndCatchup;
        ps.useAutoRandomSeed=false;ps.randomSeed=19311816;
        var emission=ps.emission;emission.rateOverTime=1.25f;
        var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(2.4f,2.5f,2.4f);
        // Keep the volume upright; a slight air current stays within the walls.
        shape.rotation=Vector3.zero;
        var velocity=ps.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
        velocity.x=new ParticleSystem.MinMaxCurve(-.006f,.006f);
        velocity.y=new ParticleSystem.MinMaxCurve(-.005f,.013f);
        velocity.z=new ParticleSystem.MinMaxCurve(-.006f,.006f);
        var color=ps.colorOverLifetime;color.enabled=true;
        var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.18f),new GradientAlphaKey(.65f,.78f),new GradientAlphaKey(0,1)});
        color.color=gradient;
        var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=mat;
        renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
        renderer.receiveShadows=false;renderer.minParticleSize=0;renderer.maxParticleSize=.012f;
        ps.Play();
    }
}
