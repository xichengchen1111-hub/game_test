using System;
using UnityEngine;
using UnityEngine.Rendering;

// Shared plant meshes with spatial culling. Keeps thousands of placements small
// without thousands of behaviours/renderers or duplicated combined vertex data.
[ExecuteAlways]
public sealed class GreeneryInstances : MonoBehaviour {
    [Serializable] public class Batch {
        public Mesh nearMesh,farMesh;
        public Matrix4x4[] matrices;
        public Bounds localBounds;
        public bool grass;
        [NonSerialized] public Matrix4x4[] world;
        [NonSerialized] public Bounds worldBounds;
    }
    public Material material;
    public Batch[] batches=Array.Empty<Batch>();
    readonly Plane[] planes=new Plane[6];
    MaterialPropertyBlock lighting;
    SphericalHarmonicsL2[] ambient;
    double nextAmbient;
    Matrix4x4 cachedMatrix;
    int cachedCount=-1;
    void OnEnable(){cachedCount=-1;RenderPipelineManager.beginCameraRendering+=Draw;}
    void OnDisable(){RenderPipelineManager.beginCameraRendering-=Draw;}
    void Draw(ScriptableRenderContext context,Camera camera) {
        if(!material||!gameObject.activeInHierarchy||(camera.cullingMask&(1<<gameObject.layer))==0)return;
        var matrix=transform.localToWorldMatrix;
        if(cachedCount!=batches.Length||matrix!=cachedMatrix) {
            int largest=1;
            foreach(var b in batches) {
                largest=Mathf.Max(largest,b.matrices.Length);
                b.world=new Matrix4x4[b.matrices.Length];for(int i=0;i<b.world.Length;i++)b.world[i]=matrix*b.matrices[i];
                var e=b.localBounds.extents;var x=matrix.MultiplyVector(new Vector3(e.x,0,0));var y=matrix.MultiplyVector(new Vector3(0,e.y,0));var z=matrix.MultiplyVector(new Vector3(0,0,e.z));
                b.worldBounds=new Bounds(matrix.MultiplyPoint3x4(b.localBounds.center),new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z))*2);
            }
            cachedMatrix=matrix;cachedCount=batches.Length;
            ambient=new SphericalHarmonicsL2[largest];lighting=new MaterialPropertyBlock();
            var visibility=new Vector4[largest];for(int i=0;i<largest;i++)visibility[i]=Vector4.one;
            lighting.CopyProbeOcclusionArrayFrom(visibility);nextAmbient=0;
        }
        // Instanced draws need per-instance SH explicitly supplied to match ordinary
        // foliage renderers. These outdoor plants share the live sky/ground ambient.
        if(Time.realtimeSinceStartupAsDouble>=nextAmbient) {
            var probe=RenderSettings.ambientProbe;for(int i=0;i<ambient.Length;i++)ambient[i]=probe;
            lighting.CopySHCoefficientArraysFrom(ambient);nextAmbient=Time.realtimeSinceStartupAsDouble+.15;
        }
        GeometryUtility.CalculateFrustumPlanes(camera,planes);var eye=camera.transform.position;
        foreach(var b in batches) {
            float distance=Mathf.Sqrt(b.worldBounds.SqrDistance(eye));
            if(distance>(b.grass?65:125)||!GeometryUtility.TestPlanesAABB(planes,b.worldBounds))continue;
            var mesh=distance<(b.grass?25:45)?b.nearMesh:b.farMesh;
            Graphics.DrawMeshInstanced(mesh,0,material,b.world,b.world.Length,lighting,b.grass?ShadowCastingMode.Off:ShadowCastingMode.On,true,gameObject.layer,camera,LightProbeUsage.CustomProvided);
        }
    }
}
