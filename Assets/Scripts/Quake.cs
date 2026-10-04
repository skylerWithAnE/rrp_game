using UnityEngine;

// The earthquake. Hidden from players: no meter, it just hits when too many loose cubes pile up.
// The host runs it as one event; everything it does to the ground is ordinary ground edits.
public class Quake : MonoBehaviour
{
    const float Duration = 3f;
    const float MergeAfter = 1f;   // the shaking starts first, then the cubes sink in

    public float shake;            // camera shake, on every machine
    public int count;              // quakes so far this job
    float shakeTime;
    float timer;                   // host
    bool merged, settling;
    float cooldown;

    public bool Active => timer > 0;

    void Update()
    {
        shakeTime -= Time.deltaTime;
        shake = shakeTime > 0 ? 0.25f * Mathf.Clamp01(shakeTime / 1.5f) : 0;

        var g = Game.I;
        if (!Net.IsHost || g.phase != Phase.Job) { timer = 0; settling = false; return; }

        if (timer > 0)
        {
            timer -= Time.deltaTime;
            if (!merged && timer < Duration - MergeAfter) Merge();
            return;
        }
        if (settling)
        {
            // walls keep slumping under the quake rules until the ground is still
            if (g.ground.Collapsing) return;
            g.ground.quakeMode = false;
            settling = false;
            cooldown = 3f;
        }
        cooldown -= Time.deltaTime;
        if (cooldown <= 0 && g.cubes.Loose > g.tuning.quakeThreshold) Trigger();
    }

    // host
    public void Trigger()
    {
        var g = Game.I;
        if (!Net.IsHost || g.phase != Phase.Job || Active || settling) return;
        timer = Duration;
        merged = false;

        // players are knocked over and drop what they carry
        foreach (var p in g.players)
        {
            if (p == null || p.load == 0) continue;
            int n = p.bits > 0 ? p.bits : 1;
            for (int k = 0; k < n; k++)
                g.cubes.Spawn((byte)p.load, p.bits > 0, p.ShovelPoint + Vector3.up * 0.2f * k, Quaternion.identity, Vector3.up);
            p.SetLoad(0, 0);
        }

        var m = Msg.New(Op.Quake, 12);
        m.F32(Duration);
        m.F32(g.tuning.quakeKnockdown);
        Net.ToClients(m, true);
        Begin(Duration, g.tuning.quakeKnockdown);
    }

    // everyone
    public void Begin(float duration, float knockdown)
    {
        shakeTime = duration;
        count++;
        foreach (var p in Game.I.players) if (p != null) p.knocked = knockdown;
    }

    // Every loose cube becomes a lump in the ground where it sits, then steep walls give way.
    void Merge()
    {
        var g = Game.I;
        merged = true;
        var cracked = new System.Collections.Generic.HashSet<int>();
        for (int i = 0; i < Cubes.Capacity; i++)
        {
            var c = g.cubes.all[i];
            if (c == null || !c.active) continue;
            // finished road near the mess cracks and drops a tier
            g.ground.Crack(c.go.transform.position, g.tuning.quakeCrackRadius, cracked);
            g.ground.Raise(g.ground.NearestPoint(c.go.transform.position), c.bit ? Ground.Cell / Cubes.BitsPerCube : Ground.Cell);
        }
        g.cubes.RemoveAll();
        g.ground.quakeMode = true;
        g.ground.DisturbAll();
        settling = true;
    }
}
