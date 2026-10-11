using UnityEngine;

// Original procedural sounds for the hotel car: no sampled recordings.
// Loops contain whole firing/compression cycles so they repeat without seams.
public sealed class CarSounds : MonoBehaviour {
    const int Rate = 22050;
    public const float EngineLoopRpm = 1000;
    AudioSource engine, starter, horn, road, wiper, oneShot;
    AudioLowPassFilter engineFilter, roadFilter;
    AudioClip doorOpen, doorClose, click, clunk, ratchet, crankTurn, latch;
    float engineLevel, starterLevel, hornLevel, roadLevel, wiperLevel;
    public bool EnginePlaying => engine && engine.isPlaying && engine.volume > .01f;
    public float EngineVolume => engine ? engine.volume : 0;
    public float EnginePitch => engine ? engine.pitch : 0;
    public float HornVolume => horn ? horn.volume : 0;
    public float StarterVolume => starter ? starter.volume : 0;

    public void Initialize(Vector3 enginePoint) {
        if (engine) return;
        engine = Source("Inline four engine", enginePoint, EngineLoop(), true, out engineFilter);
        starter = Source("Starter motor", enginePoint, StarterLoop(), true, out _);
        horn = Source("Electric horn", enginePoint + Vector3.forward * .3f, HornLoop(), true, out _);
        road = Source("Tyres on setts", new Vector3(0, .3f, 0), RoadLoop(), true, out roadFilter);
        wiper = Source("Vacuum wiper", new Vector3(-.3f, 1.6f, .3f), WiperLoop(), true, out _);
        oneShot = Source("Car mechanisms", new Vector3(0, .9f, 0), null, false, out _);
        oneShot.volume = .8f;
        doorOpen = Impulse("Door latch open", .16f, 1800, .5f, 260, .35f, 11);
        doorClose = Impulse("Door shut", .32f, 900, .25f, 85, .9f, 12);
        click = Impulse("Switch click", .05f, 3200, .8f, 0, 0, 13);
        clunk = Impulse("Gear clunk", .18f, 1300, .35f, 120, .6f, 14);
        latch = Impulse("Cap or strap", .12f, 2400, .6f, 180, .25f, 15);
        ratchet = Ratchet("Hand brake ratchet", 7, .035f, 16);
        crankTurn = Ratchet("Crank turning", 4, .09f, 17);
    }
    AudioSource Source(string label, Vector3 local, AudioClip clip, bool loop, out AudioLowPassFilter filter) {
        var child = new GameObject(label); child.transform.SetParent(transform, false); child.transform.localPosition = local;
        var source = child.AddComponent<AudioSource>();
        source.clip = clip; source.loop = loop; source.playOnAwake = false; source.volume = 0;
        source.spatialBlend = 1; source.rolloffMode = AudioRolloffMode.Linear; source.minDistance = 2.5f; source.maxDistance = 45;
        source.dopplerLevel = 0; source.priority = 90;
        filter = child.AddComponent<AudioLowPassFilter>(); filter.cutoffFrequency = 22000;
        if (loop && clip) source.Play();
        return source;
    }
    public void Tick(float rpm, float load, bool running, bool cranking, bool hornOn, float speed, bool wipers, bool listenerInsideClosed, bool frozen) {
        if (!engine) return;
        float dt = Time.unscaledDeltaTime;
        engineLevel = Mathf.MoveTowards(engineLevel, running ? .28f + .32f * load : 0, dt * 3);
        starterLevel = Mathf.MoveTowards(starterLevel, cranking ? .55f : 0, dt * 8);
        hornLevel = Mathf.MoveTowards(hornLevel, hornOn ? .85f : 0, dt * 14);
        roadLevel = Mathf.MoveTowards(roadLevel, Mathf.Clamp01(speed / 14f) * .45f, dt * 2);
        wiperLevel = Mathf.MoveTowards(wiperLevel, wipers ? .18f : 0, dt * 4);
        engine.pitch = Mathf.Clamp(rpm / EngineLoopRpm, .35f, 3f);
        road.pitch = .7f + Mathf.Clamp01(speed / 20f) * .6f;
        float cutoff = listenerInsideClosed ? 2400 : 22000;
        engineFilter.cutoffFrequency = Mathf.MoveTowards(engineFilter.cutoffFrequency, cutoff, dt * 40000);
        roadFilter.cutoffFrequency = engineFilter.cutoffFrequency;
        Level(engine, frozen ? 0 : engineLevel); Level(starter, frozen ? 0 : starterLevel); Level(horn, frozen ? 0 : hornLevel);
        Level(road, frozen ? 0 : roadLevel); Level(wiper, frozen ? 0 : wiperLevel);
    }
    static void Level(AudioSource source, float volume) {
        source.volume = volume;
        if (volume > .001f && !source.isPlaying) source.Play();
    }
    public void Door(bool opening) => Play(opening ? doorOpen : doorClose, opening ? .7f : 1f);
    public void Click() => Play(click, .55f);
    public void Clunk() => Play(clunk, .7f);
    public void Latch() => Play(latch, .7f);
    public void Ratchet() => Play(ratchet, .55f);
    public void Crank() => Play(crankTurn, .8f);
    void Play(AudioClip clip, float volume) { if (oneShot && clip) oneShot.PlayOneShot(clip, volume); }

    // 20 firing pulses of a four-cylinder four-stroke engine at 1000 rpm (33.3 Hz) = 0.6 s.
    static AudioClip EngineLoop() {
        int n = (int)(Rate * .6f); var data = new float[n]; var random = new System.Random(1931);
        float pulse = n / 20f;
        float[] cylinder = { 1f, .86f, .94f, .81f };
        for (int p = 0; p < 20; p++) {
            int start = (int)(p * pulse); float gain = cylinder[p % 4];
            for (int j = 0; j < pulse * 1.6f; j++) {
                float t = j / (float)Rate, env = Mathf.Exp(-t / .012f);
                float body = Mathf.Sin(2 * Mathf.PI * 62 * t) * .7f + Mathf.Sin(2 * Mathf.PI * 125 * t) * .25f;
                data[(start + j) % n] += gain * env * (body + ((float)random.NextDouble() * 2 - 1) * .35f) * .5f;
            }
            // Tappet tick between firings.
            int tick = (start + (int)(pulse * .5f)) % n;
            for (int j = 0; j < 90; j++) data[(tick + j) % n] += Mathf.Exp(-j / 18f) * ((float)random.NextDouble() * 2 - 1) * .08f;
        }
        float low = 0; for (int i = -n; i < n; i++) { int k = (i + n) % n; low = low * .9f + data[k] * .1f; if (i >= 0) data[k] = data[k] * .6f + low * .9f; }
        return Clip("Original inline four at 1000 rpm", data);
    }
    // Starter motor whine with four compression strokes per 0.5 s loop.
    static AudioClip StarterLoop() {
        int n = Rate / 2; var data = new float[n];
        for (int i = 0; i < n; i++) {
            float t = i / (float)Rate, phase = t / .5f;
            float compression = .65f + .35f * Mathf.Cos(2 * Mathf.PI * 4 * phase);
            float saw = 2 * (t * 140 % 1) - 1;
            data[i] = (saw * .35f + Mathf.Sin(2 * Mathf.PI * 70 * t) * .4f) * compression * .6f;
        }
        return Clip("Original starter motor", data);
    }
    // Buzzy electric horn, an exact number of periods for a seamless hold.
    static AudioClip HornLoop() {
        int n = Rate / 4; var data = new float[n];
        for (int i = 0; i < n; i++) {
            float t = i / (float)Rate;
            float a = Mathf.Sin(2 * Mathf.PI * 336 * t), b = Mathf.Sin(2 * Mathf.PI * 424 * t);
            data[i] = (Mathf.Clamp(a * 3, -1, 1) * .4f + Mathf.Clamp(b * 2.4f, -1, 1) * .3f) * .55f;
        }
        return Clip("Original electric horn", data);
    }
    static AudioClip RoadLoop() {
        int n = Rate * 2; var data = new float[n]; var random = new System.Random(1932); var white = new float[n];
        for (int i = 0; i < n; i++) white[i] = (float)random.NextDouble() * 2 - 1;
        float low = 0;
        for (int j = -2048; j < n; j++) { int i = (j + n) % n; low = low * .97f + white[i] * .03f; if (j >= 0) data[i] = low * 2.2f; }
        return Clip("Original tyre rumble", data);
    }
    static AudioClip WiperLoop() {
        int n = (int)(Rate * 1.4f); var data = new float[n]; var random = new System.Random(1933);
        for (int i = 0; i < n; i++) {
            float t = i / (float)Rate, sweep = Mathf.Abs(Mathf.Sin(Mathf.PI * t / .7f));
            data[i] = ((float)random.NextDouble() * 2 - 1) * .18f * sweep;
        }
        return Clip("Original wiper motor", data);
    }
    static AudioClip Impulse(string name, float seconds, float bright, float brightGain, float thump, float thumpGain, int seed) {
        int n = (int)(Rate * seconds); var data = new float[n]; var random = new System.Random(seed); float smooth = 0;
        for (int i = 0; i < n; i++) {
            float t = i / (float)Rate, env = Mathf.Exp(-t / (seconds * .22f)) * Mathf.Min(1, t * Rate / 30f);
            float noise = (float)random.NextDouble() * 2 - 1; smooth = smooth * .5f + noise * .5f;
            data[i] = env * (brightGain * (noise * .5f + Mathf.Sin(2 * Mathf.PI * bright * t) * .3f) + thumpGain * Mathf.Sin(2 * Mathf.PI * thump * t) * Mathf.Exp(-t / (seconds * .4f)));
        }
        return Clip(name, data);
    }
    static AudioClip Ratchet(string name, int clicks, float gap, int seed) {
        int n = (int)(Rate * (clicks * gap + .05f)); var data = new float[n]; var random = new System.Random(seed);
        for (int c = 0; c < clicks; c++) {
            int start = (int)(c * gap * Rate);
            for (int j = 0; j < 260 && start + j < n; j++) data[start + j] += Mathf.Exp(-j / 40f) * (((float)random.NextDouble() * 2 - 1) * .5f + Mathf.Sin(j * .9f) * .3f);
        }
        return Clip(name, data);
    }
    static AudioClip Clip(string name, float[] data) {
        var clip = AudioClip.Create(name, data.Length, 1, Rate, false); clip.SetData(data, 0); return clip;
    }
}
