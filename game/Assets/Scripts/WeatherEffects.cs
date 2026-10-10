using System;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(110)]
public sealed class WeatherEffects : MonoBehaviour {
    public const int DropBudget=256;
    public Material rainTemplate;
    public int VisibleStreaks {get;private set;}
    public bool ListenerSheltered {get;private set;}
    public float RainVolume => ambience ? ambience.Bed.volume : 0;
    public Mesh RainMesh => mesh;
    readonly Vector3[] vertices=new Vector3[DropBudget*4];
    readonly Color32[] colours=new Color32[DropBudget*4];
    readonly Vector3[] seeds=new Vector3[DropBudget];
    Mesh mesh;
    Material rainMaterial;
    MeshRenderer rainRenderer;
    RainAmbience ambience;
    RainSplashes splashes;
    public RainAmbience Ambience => ambience;
    public RainSplashes Splashes => splashes;
    double lastElapsed=double.NaN;
    Vector3 lastEye=Vector3.positiveInfinity;
    Quaternion lastRotation;
    float lastRain=-1;

    void Awake() {
        var random=new System.Random(1931);
        var uv=new Vector2[vertices.Length];var indices=new int[DropBudget*6];
        for(int d=0;d<DropBudget;d++) {
            float angle=(float)random.NextDouble()*Mathf.PI*2;
            float radius=Mathf.Sqrt((float)random.NextDouble())*(d<128?3.5f:14.5f);
            seeds[d]=new Vector3(Mathf.Cos(angle)*radius,(float)random.NextDouble(),Mathf.Sin(angle)*radius);
            int v=d*4,k=d*6;
            uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(0,1);uv[v+3]=new Vector2(1,1);
            indices[k]=v;indices[k+1]=v+2;indices[k+2]=v+1;indices[k+3]=v+1;indices[k+4]=v+2;indices[k+5]=v+3;
        }
        mesh=new Mesh{name="Bounded roof-clipped rain"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;mesh.colors32=colours;
        var rain=new GameObject("Local weather rain");rain.transform.SetParent(transform,false);
        // Vertices are world coordinates. The celestial parent rotates every frame.
        rain.transform.SetParent(null);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rain,gameObject.scene);
        rain.AddComponent<MeshFilter>().sharedMesh=mesh;
        rainRenderer=rain.AddComponent<MeshRenderer>();rainRenderer.shadowCastingMode=ShadowCastingMode.Off;rainRenderer.receiveShadows=false;
        rainRenderer.lightProbeUsage=LightProbeUsage.Off;rainRenderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
        rainMaterial=rainTemplate ? new Material(rainTemplate) : null;rainRenderer.sharedMaterial=rainMaterial;rainRenderer.enabled=false;
        ambience=gameObject.AddComponent<RainAmbience>();
        splashes=gameObject.AddComponent<RainSplashes>();splashes.Initialize(rainMaterial);

    }
    void LateUpdate() {
        var app=DemoApp.Instance;var cycle=DayNightCycle.Instance;
        if(!app||!app.player||!cycle||!rainMaterial)return;
        var eye=app.player.eye.transform;var weather=cycle.Weather;
        ListenerSheltered=WeatherShelter.Covered(eye.position);
        ambience.Apply(weather.rain,WeatherShelter.MufflesRain(eye.position),app.GameTimePaused);
        splashes.Apply(eye,weather,cycle.Daylight,app.ElapsedGameMinutes);
        rainRenderer.enabled=weather.rain>.001f;
        if(!rainRenderer.enabled){VisibleStreaks=0;return;}
        if(lastElapsed==app.ElapsedGameMinutes && lastEye==eye.position && lastRotation==eye.rotation && lastRain==weather.rain)return;
        lastElapsed=app.ElapsedGameMinutes;lastEye=eye.position;lastRotation=eye.rotation;lastRain=weather.rain;
        rainMaterial.SetColor("_Tint",Color.Lerp(new Color(.33f,.42f,.55f),new Color(.68f,.74f,.79f),cycle.Daylight));
        double seconds=app.ElapsedGameMinutes/GameClock.GameMinutesPerRealSecond;
        VisibleStreaks=0;
        for(int d=0;d<DropBudget;d++) {
            int v=d*4;var seed=seeds[d];
            float field=d<128?7:29;
            // Wrap stationary world columns only at the faded edge of the local field.
            // Rain must retain parallax rather than sliding along with the camera.
            var point=new Vector3(seed.x+Mathf.Floor((eye.position.x-seed.x)/field+.5f)*field,eye.position.y,
                seed.z+Mathf.Floor((eye.position.z-seed.z)/field+.5f)*field);
            var margin=eye.right*.036f;
            var tail=new Vector3(-weather.wind*.10f,.40f+seed.y*.34f,weather.wind*.045f);
            float roof=WeatherShelter.RoofHeight(point);
            roof=Mathf.Max(roof,Mathf.Max(WeatherShelter.RoofHeight(point-margin),WeatherShelter.RoofHeight(point+margin)));
            roof=Mathf.Max(roof,Mathf.Max(WeatherShelter.RoofHeight(point+tail-margin),WeatherShelter.RoofHeight(point+tail+margin)));
            float top=eye.position.y+8,bottom=Mathf.Max(eye.position.y-7,roof+.05f);
            float length=tail.y,span=top-bottom;
            float speed=11+seed.y*4;
            float fall=(float)((seed.y*16-(seconds%(16/speed))*speed+16)%16);
            point.y=fall+Mathf.Floor((top-fall)/16)*16;
            if(span<length || point.y<bottom || weather.rain<seed.y*.08f) {
                for(int j=0;j<4;j++){vertices[v+j]=eye.position;colours[v+j]=new Color32(255,255,255,0);}continue;
            }
            float fade=Mathf.Clamp01((point.y-bottom)/length)*Mathf.Clamp01((top-point.y)/length);
            fade*=1-DayNightCycle.Smooth(field*.35f,field*.5f,Mathf.Abs(point.x-eye.position.x));
            fade*=1-DayNightCycle.Smooth(field*.35f,field*.5f,Mathf.Abs(point.z-eye.position.z));
            var side=eye.right*(.007f+Vector3.Distance(eye.position,point)*.00055f);
            vertices[v]=point-side;vertices[v+1]=point+side;
            vertices[v+2]=point-side+tail;vertices[v+3]=point+side+tail;
            byte alpha=(byte)(Mathf.Clamp01(weather.rain*fade*.84f)*255);
            for(int j=0;j<4;j++)colours[v+j]=new Color32(255,255,255,alpha);
            VisibleStreaks++;
        }
        mesh.vertices=vertices;mesh.colors32=colours;
        mesh.bounds=new Bounds(eye.position,new Vector3(31,18,31));
    }
    void OnDestroy() {
        if(rainRenderer)Destroy(rainRenderer.gameObject);
        if(mesh)Destroy(mesh);if(rainMaterial)Destroy(rainMaterial);
    }
}
