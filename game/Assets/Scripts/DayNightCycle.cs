using UnityEngine;
using UnityEngine.Rendering;

// The watch's clock is authoritative. No second timer and no offline progression.
[DefaultExecutionOrder(100)]
public sealed class DayNightCycle : MonoBehaviour {
    public Light celestialLight;
    public Material skyTemplate;
    Material sky;
    Material originalSky;
    Texture2D cloudNoise;
    public float Daylight { get; private set; }
    public float SolarAltitude { get; private set; }
    public double AppliedMinutes { get; private set; }
    public bool MoonActive { get; private set; }
    public TownWeather.State Weather { get; private set; }
    public float NaturalIllumination => Daylight * Weather.naturalLight;
    public float CloudTransmission {get;private set;}
    public double AppliedElapsedMinutes { get; private set; }
    TownWeather.Kind? reviewWeather;
    public static DayNightCycle Instance { get; private set; }

    public static float Smooth(float lo, float hi, float value) {
        float t = Mathf.InverseLerp(lo, hi, value);
        return t * t * (3 - 2 * t);
    }
    public static Vector3 SunDirection(double minutes) {
        float angle = (float)((GameClock.WrapMinutes(minutes) - 360) / 1440 * System.Math.PI * 2);
        // Tilt the orbit to avoid an unstable LookRotation at the zenith.
        return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) * .88f, Mathf.Sin(angle) * .475f).normalized;
    }
    public static float NaturalDaylight(double minutes) => Smooth(-.10f, .32f, SunDirection(minutes).y);

    void Awake() {
        Instance = this;
        originalSky = RenderSettings.skybox;
        if (skyTemplate) sky = new Material(skyTemplate);
        else if (originalSky) sky = new Material(originalSky);
        if(sky)cloudNoise=sky.GetTexture("_Noise") as Texture2D;
        Apply(DemoApp.Instance ? DemoApp.Instance.Minutes : GameClock.InitialMinutes);
    }
    void LateUpdate() {
        var app=DemoApp.Instance;
        if (app && (AppliedElapsedMinutes!=app.ElapsedGameMinutes || AppliedMinutes!=app.Minutes || RenderSettings.skybox!=sky || RenderSettings.sun!=celestialLight)) Apply(app.Minutes);
    }
    public void SetReviewWeather(TownWeather.Kind? kind) {
        if (!DemoApp.Instance || !DemoApp.Instance.Review) throw new System.InvalidOperationException("Weather staging requires an isolated review");
        reviewWeather=kind;Apply(DemoApp.Instance.Minutes);
    }
    public void Apply(double minutes) {
        AppliedMinutes = GameClock.WrapMinutes(minutes);
        AppliedElapsedMinutes=DemoApp.Instance ? DemoApp.Instance.ElapsedGameMinutes : 0;
        Weather=reviewWeather.HasValue ? TownWeather.Preset(reviewWeather.Value) : TownWeather.Evaluate(AppliedElapsedMinutes);
        Vector3 sun = SunDirection(minutes);
        SolarAltitude = sun.y;
        Daylight = NaturalDaylight(minutes);
        float twilight = Smooth(-.22f, -.01f, sun.y) * (1 - Smooth(.02f, .30f, sun.y));
        float sunStrength = Smooth(0, .25f, sun.y);
        float moonStrength = Smooth(.02f, .25f, -sun.y);
        MoonActive = sun.y < 0;
        CloudTransmission=1-.84f*CloudOpacity(MoonActive ? -sun : sun);
        if (celestialLight) {
            RenderSettings.sun = celestialLight;
            celestialLight.transform.rotation = Quaternion.LookRotation(MoonActive ? sun : -sun, Vector3.up);
            celestialLight.color = MoonActive ? new Color(.65f, .74f, 1) : Color.Lerp(new Color(1, .63f, .34f), new Color(1, .96f, .86f), sunStrength);
            celestialLight.intensity = (MoonActive ? .20f * moonStrength : .9f * sunStrength) * Weather.directLight * CloudTransmission;
            celestialLight.shadowStrength=Mathf.Lerp(.15f,.85f,Weather.directLight*CloudTransmission);
            // The direction flips while intensity is zero at the horizon.
            celestialLight.enabled = celestialLight.intensity > .0001f;
        }
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(.32f, .39f, .55f), new Color(.70f, .78f, .88f), Daylight);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.23f, .27f, .37f), new Color(.58f, .60f, .61f), Daylight);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(.14f, .16f, .22f), new Color(.37f, .35f, .31f), Daylight);
        float cloudGrey=Daylight*Weather.darkness;
        RenderSettings.ambientSkyColor=Color.Lerp(RenderSettings.ambientSkyColor,new Color(.52f,.55f,.60f),cloudGrey*.55f);
        RenderSettings.ambientEquatorColor=Color.Lerp(RenderSettings.ambientEquatorColor,new Color(.48f,.49f,.50f),cloudGrey*.40f);
        float diffuse=Mathf.Lerp(1,.42f+Weather.naturalLight*.58f,Daylight);
        RenderSettings.ambientSkyColor*=diffuse;
        RenderSettings.ambientEquatorColor*=diffuse;
        RenderSettings.ambientGroundColor*=diffuse;
        RenderSettings.fogColor = Color.Lerp(new Color(.095f, .12f, .20f), new Color(.51f, .54f, .57f), Daylight);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, new Color(.42f, .35f, .40f), twilight * .55f);
        RenderSettings.fogDensity = Mathf.Lerp(.012f, .0085f, Daylight);
        RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,Color.Lerp(new Color(.13f,.16f,.23f),new Color(.47f,.49f,.51f),Daylight),Weather.darkness);
        RenderSettings.fogDensity+=Weather.mist*.057f;
        if (sky) {
            // Town is loaded additively before DemoApp makes it active. Restore the
            // owned sky after that active-scene change, including on the first frame.
            RenderSettings.skybox = sky;
            var top=Color.Lerp(new Color(.028f,.045f,.105f),new Color(.15f,.36f,.64f),Daylight);
            var middle=Color.Lerp(new Color(.075f,.105f,.18f),new Color(.48f,.65f,.80f),Daylight);
            var bottom=Color.Lerp(Color.Lerp(new Color(.13f,.15f,.23f),new Color(.74f,.79f,.84f),Daylight),new Color(.72f,.40f,.25f),twilight*.65f);
            var grey=Color.Lerp(new Color(.065f,.08f,.13f),new Color(.39f,.42f,.45f),Daylight);
            sky.SetColor("_Top",Color.Lerp(top,grey,Weather.darkness));
            sky.SetColor("_Middle",Color.Lerp(middle,grey,Weather.darkness*.80f));
            sky.SetColor("_Bottom",Color.Lerp(bottom,RenderSettings.fogColor,Weather.darkness*.70f));
            sky.SetColor("_CloudColor",Color.Lerp(new Color(.13f,.17f,.26f),Color.Lerp(new Color(.95f,.96f,.98f),new Color(.55f,.60f,.65f),Weather.darkness),Daylight));
            sky.SetColor("_CloudShade",Color.Lerp(new Color(.055f,.08f,.14f),Color.Lerp(new Color(.55f,.64f,.76f),new Color(.29f,.35f,.43f),Weather.darkness),Daylight));
            var low=TownWeather.CloudOffset(AppliedElapsedMinutes);
            var high=TownWeather.CloudOffset(AppliedElapsedMinutes,true);
            sky.SetVector("_CloudOffset",new Vector4(low.x,low.y,0,0));
            sky.SetVector("_HighCloudOffset",new Vector4(high.x,high.y,0,0));
            sky.SetFloat("_Coverage",Weather.coverage);
            sky.SetFloat("_Darkness",Weather.darkness);
            sky.SetFloat("_Haze",Weather.mist);
            sky.SetFloat("_Twilight",twilight);
            sky.SetVector("_SunDirection", sun);
            sky.SetVector("_MoonDirection", -sun);
            sky.SetFloat("_SunVisibility", Smooth(-.02f, .03f, sun.y)*(1-Weather.mist*.72f));
            sky.SetFloat("_MoonVisibility", moonStrength);
            sky.SetFloat("_Night", 1 - Smooth(-.18f, .02f, sun.y));
        }
        // Explicit fixture registry; no scene searches, raycasts, GI or reflection renders each frame.
        foreach (var lamp in DayNightLamp.Active) lamp.Apply(Daylight,NaturalIllumination);
    }
    float CloudOpacity(Vector3 direction) {
        if(!cloudNoise || !cloudNoise.isReadable)return Weather.coverage;
        // Same density/cover field as the sky, sampled only at the light direction.
        // This gives soft cloud-passage dimming without another light or shadow map.
        var p=new Vector2(direction.x,direction.z)/Mathf.Max(.25f,direction.y+.30f)*.38f+TownWeather.CloudOffset(AppliedElapsedMinutes);
        float warp=cloudNoise.GetPixelBilinear(p.x*.5f,p.y*.5f).g-.5f;
        p+=new Vector2(warp*.14f,warp*.08f);
        float n=cloudNoise.GetPixelBilinear(p.x,p.y).r+(cloudNoise.GetPixelBilinear(p.x*2+7,p.y*2+7).b-.5f)*.055f;
        float threshold=Mathf.Lerp(.71f,.33f,Weather.coverage);
        float alpha=Smooth(threshold-.045f,threshold+.055f,n);
        alpha=Mathf.Lerp(alpha,1,Smooth(.76f,1,Weather.coverage)*.98f);
        return alpha*Smooth(-.035f,.075f,direction.y);
    }
    void OnDestroy() {
        if (Instance == this) Instance = null;
        if (RenderSettings.skybox == sky) RenderSettings.skybox = originalSky;
        if (sky) Destroy(sky);
    }
}
