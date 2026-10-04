using UnityEngine;

// The fail noises, one per material, made in code: no audio files.
public static class Sfx
{
    public const byte Rough = 0, Thud = 1, Squeak = 2;
    const int Rate = 44100;

    static AudioClip[] clips;

    // host: everyone hears it
    public static void Broadcast(byte kind, Vector3 position)
    {
        var m = Msg.New(Op.Sound, 16);
        m.U8(kind);
        m.V3(position);
        Net.ToClients(m, true);
        Play(kind, position);
    }

    public static void Play(byte kind, Vector3 position)
    {
        if (Application.isBatchMode) return;
        if (clips == null) clips = new[] { MakeRough(), MakeThud(), MakeSqueak() };
        var go = new GameObject("Sfx");
        go.transform.position = position;
        var source = go.AddComponent<AudioSource>();
        source.clip = clips[kind];
        source.spatialBlend = 1f;
        source.minDistance = 6f;
        source.maxDistance = 60f;
        source.Play();
        Object.Destroy(go, clips[kind].length + 0.1f);
    }

    static AudioClip Make(string name, float seconds, System.Func<float, float> wave)
    {
        int n = (int)(seconds * Rate);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // sand on road: a gritty scrape
    static AudioClip MakeRough()
    {
        var rng = new System.Random(1);
        float low = 0;
        return Make("Rough", 0.28f, t =>
        {
            low = Mathf.Lerp(low, (float)rng.NextDouble() * 2f - 1f, 0.35f);
            return low * Mathf.Exp(-t * 9f) * 0.9f;
        });
    }

    // rock that will not break: a low knock
    static AudioClip MakeThud()
    {
        return Make("Thud", 0.3f, t => Mathf.Sin(2f * Mathf.PI * (95f - 120f * t) * t) * Mathf.Exp(-t * 16f));
    }

    // oil with nothing to soak into: a rising squeak
    static AudioClip MakeSqueak()
    {
        return Make("Squeak", 0.22f, t =>
        {
            float pitch = 900f + 2600f * t + Mathf.Sin(t * 90f) * 60f;
            return Mathf.Sin(2f * Mathf.PI * pitch * t) * Mathf.Sin(Mathf.PI * t / 0.22f) * 0.5f;
        });
    }
}
