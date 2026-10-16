using UnityEngine;
using UnityEngine.Rendering;

// One bounded mesh. Ground contacts are cached per world cell; at most four
// contact queries a frame when the local field moves, never physics particles.
public sealed class RainSplashes : MonoBehaviour {
    public const int Budget=96;
    const int RingSegments=10, VerticesPerHit=56;
    readonly Vector3[] vertices=new Vector3[Budget*VerticesPerHit];
    readonly Color32[] colors=new Color32[Budget*VerticesPerHit];
    readonly Vector3[] contacts=new Vector3[Budget];
    readonly Vector2[] offsets=new Vector2[Budget];
    readonly Vector2Int[] cells=new Vector2Int[Budget];
    readonly bool[] known=new bool[Budget],valid=new bool[Budget];
    readonly RaycastHit[] hits=new RaycastHit[16];
    Mesh mesh;MeshRenderer visual;Material material;
    int cursor;
    public int VisibleHits {get;private set;}
    public int ContactQueries {get;private set;}
    public Mesh SurfaceMesh => mesh;
    public void Initialize(Material template) {
        var random=new System.Random(9013);var uv=new Vector2[vertices.Length];var indices=new int[vertices.Length/4*6];
        for(int i=0;i<Budget;i++)offsets[i]=new Vector2((float)random.NextDouble(),(float)random.NextDouble());
        for(int i=0;i<vertices.Length/4;i++) {
            int v=i*4,k=i*6;uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(0,1);uv[v+3]=new Vector2(1,1);
            indices[k]=v;indices[k+1]=v+2;indices[k+2]=v+1;indices[k+3]=v+1;indices[k+4]=v+2;indices[k+5]=v+3;
        }
        mesh=new Mesh{name="Ground rain crowns and spreading rings"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.colors32=colors;mesh.uv=uv;mesh.triangles=indices;
        var child=new GameObject("Rain landing splashes");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(child,gameObject.scene);
        child.AddComponent<MeshFilter>().sharedMesh=mesh;visual=child.AddComponent<MeshRenderer>();
        visual.shadowCastingMode=ShadowCastingMode.Off;visual.receiveShadows=false;visual.lightProbeUsage=LightProbeUsage.Off;visual.reflectionProbeUsage=ReflectionProbeUsage.Off;
        material=template?new Material(template):null;visual.sharedMaterial=material;visual.enabled=false;
    }
    void RefreshContacts(Vector3 eye) {
        ContactQueries=0;int cx=Mathf.FloorToInt(eye.x/1.3f),cz=Mathf.FloorToInt(eye.z/1.3f);
        for(int n=0;n<Budget&&ContactQueries<4;n++) {
            int i=cursor++%Budget;int x=cx-6+i%12,z=cz-4+i/12;var cell=new Vector2Int(x,z);
            if(known[i]&&cells[i]==cell)continue;
            known[i]=true;cells[i]=cell;valid[i]=false;ContactQueries++;
            var origin=new Vector3((x+offsets[i].x)*1.3f,40,(z+offsets[i].y)*1.3f);
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,hits,55,~((1<<30)|(1<<StreetLife.Layer)),QueryTriggerInteraction.Ignore);
            float nearest=float.PositiveInfinity;RaycastHit surface=default;
            for(int h=0;h<count;h++)if(!hits[h].collider.GetComponentInParent<PlayerMotor>()&&hits[h].distance<nearest){surface=hits[h];nearest=surface.distance;}
            if(float.IsInfinity(nearest)||surface.normal.y<.92f||surface.point.y>eye.y-.3f)continue;
            // Covered floors receive no impact crowns, including inside window views.
            if(WeatherShelter.RoofHeight(surface.point)>surface.point.y+.08f)continue;
            contacts[i]=surface.point+Vector3.up*.008f;valid[i]=true;
        }
    }
    public void Apply(Transform eye,TownWeather.State weather,float daylight,double elapsed) {
        if(!visual)return;visual.enabled=weather.rain>.001f;VisibleHits=0;if(!visual.enabled)return;
        RefreshContacts(eye.position);
        material.SetColor("_Tint",Color.Lerp(new Color(.27f,.34f,.43f),new Color(.60f,.64f,.67f),daylight));
        double seconds=elapsed/GameClock.GameMinutesPerRealSecond;
        for(int i=0;i<Budget;i++) {
            int start=i*VerticesPerHit;for(int v=start;v<start+VerticesPerHit;v++){vertices[v]=eye.position;colors[v]=new Color32(255,255,255,0);}
            if(!valid[i])continue;
            float period=.58f+offsets[i].x*.41f;float age=(float)((seconds+offsets[i].y*13)%period);
            if(age>.36f||weather.rain<offsets[i].y*.6f)continue;
            var p=contacts[i];float distance=Vector3.Distance(eye.position,p);
            float fade=(1-DayNightCycle.Smooth(4.5f,7.5f,distance))*weather.rain;
            float t=age/.36f,radius=.025f+t*(.095f+offsets[i].x*.055f);
            float alpha=fade*(1-t)*.72f;
            // Small expanding broken ring, then four short ballistic beads.
            for(int k=0;k<RingSegments;k++) {
                float a=k*Mathf.PI*2/RingSegments,b=(k+.86f)*Mathf.PI*2/RingSegments;
                var u=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var w=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                Quad(start+k*4,p+u*(radius-.008f),p+u*(radius+.008f),p+w*(radius-.008f),p+w*(radius+.008f),alpha);
            }
            for(int k=0;k<4;k++) {
                float a=k*Mathf.PI*.5f+offsets[i].x*6;
                float y=age*.95f-4.5f*age*age; if(y<=0)continue;
                var q=p+new Vector3(Mathf.Cos(a)*age*.38f,y,Mathf.Sin(a)*age*.38f);
                var side=eye.right*.009f;var up=Vector3.up*(.018f+.025f*(1-t));
                Quad(start+40+k*4,q-side,q+side,q-side+up,q+side+up,fade*(1-t)*.9f);
            }
            if(fade>.01f)VisibleHits++;
        }
        mesh.vertices=vertices;mesh.colors32=colors;mesh.bounds=new Bounds(eye.position,new Vector3(20,50,20));
    }
    void Quad(int v,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float alpha) {
        vertices[v]=a;vertices[v+1]=b;vertices[v+2]=c;vertices[v+3]=d;
        byte opacity=(byte)(Mathf.Clamp01(alpha)*255);for(int j=0;j<4;j++)colors[v+j]=new Color32(255,255,255,opacity);
    }
    void OnDestroy(){if(visual)Destroy(visual.gameObject);if(mesh)Destroy(mesh);if(material)Destroy(material);}
}
