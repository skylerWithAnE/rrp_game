using UnityEngine;

// What the shovel does. Runs on the host only; clients send a request with where they aimed.
//
//   Primary   empty shovel: scoop the loose cube at the target, or dig the ground; either way
//                           the cube ends up on the shovel
//             loaded:       fling the cube
//   Secondary loaded:       set the cube down
//             empty:        smack the loose cube at the target, or flatten bare ground
public static class Verbs
{
    public const byte Primary = 0, Secondary = 1;

    public static void Do(Player p, byte verb, Vector3 target, Vector3 aim)
    {
        var g = Game.I;
        var t = g.tuning;
        if (g.phase != Phase.Job || p.knocked > 0 || g.quake.Active) return;
        if (Time.time < p.hostNextVerb) return;
        p.hostNextVerb = Time.time + t.digInterval * 0.8f;

        // never trust the reach
        Vector3 flat = target - p.transform.position;
        flat.y = 0;
        if (flat.magnitude > t.reach + 0.75f) target = p.transform.position + flat.normalized * t.reach;
        target = g.ground.Clamp(target, Ground.Cell);
        target.y = g.ground.HeightAt(target.x, target.z);

        bool loaded = p.load != 0 && p.bits == 0;
        bool partial = p.bits > 0;
        int swing;
        if (verb == Primary)
        {
            if (loaded) { Fling(p, aim); swing = Blob.SwingFling; }
            else { Scoop(p, target, partial); swing = Blob.SwingScoop; }
        }
        else
        {
            if (loaded || partial) { SetDown(p, target); swing = Blob.SwingScoop; }
            else { Smack(p, target); swing = Blob.SwingSmack; }
        }

        var m = Msg.New(Op.VerbFx, 4);
        m.U8((byte)p.slot);
        m.U8((byte)swing);
        Net.ToClients(m, true);
        if (!p.isLocal) p.Swing(swing);
    }

    static void Scoop(Player p, Vector3 target, bool partial)
    {
        var g = Game.I;
        var cube = g.cubes.Nearest(target + Vector3.up * 0.25f, g.tuning.pickRadius, partial);
        if (cube != null)
        {
            if (cube.bit)
            {
                // quarter bits add up to a whole cube again
                int bits = p.bits + 1;
                p.SetLoad(cube.mat, bits >= Cubes.BitsPerCube ? 0 : bits);
            }
            else p.SetLoad(cube.mat, 0);
            g.cubes.Remove(cube);
            return;
        }
        if (partial) return;

        // dug ground goes straight onto the shovel; it only becomes a loose cube when flung or set down
        if (g.ground.Dig(g.ground.NearestPoint(target))) p.SetLoad(Cubes.Sand, 0);
    }

    static void Fling(Player p, Vector3 aim)
    {
        var g = Game.I;
        if (g.cubes.Full) return;
        aim.y = 0;
        aim = aim.sqrMagnitude > 0.001f ? aim.normalized : p.Forward;
        Vector3 velocity = aim * g.tuning.flingSpeed + Vector3.up * g.tuning.flingUp;
        g.cubes.Spawn((byte)p.load, false, p.ShovelPoint + Vector3.up * 0.3f, Random.rotation, velocity, p.slot);
        p.SetLoad(0, 0);
    }

    static void SetDown(Player p, Vector3 target)
    {
        var g = Game.I;
        if (g.cubes.Full) return;
        // on top of whatever is there: ground or another cube
        float top = target.y;
        if (Physics.Raycast(target + Vector3.up * 4f, Vector3.down, out var hit, 8f)) top = hit.point.y;
        if (p.bits > 0)
        {
            for (int k = 0; k < p.bits; k++)
                g.cubes.Spawn((byte)p.load, true, new Vector3(target.x, top + 0.08f + k * 0.14f, target.z), Quaternion.identity, Vector3.zero);
        }
        else g.cubes.Spawn((byte)p.load, false, new Vector3(target.x, top + Ground.Cell * 0.5f + 0.02f, target.z), Quaternion.Euler(0, p.yaw, 0), Vector3.zero);
        p.SetLoad(0, 0);
    }

    static void Smack(Player p, Vector3 target)
    {
        var g = Game.I;
        var cube = g.cubes.Nearest(target + Vector3.up * 0.25f, g.tuning.pickRadius);
        if (cube == null)
        {
            g.ground.Flatten(g.ground.NearestPoint(target), g.tuning.flattenStrength);
            return;
        }
        // only a cube that is resting on something can be smacked
        if (cube.rb.linearVelocity.sqrMagnitude > 1f) return;
        Vector3 at = cube.go.transform.position;
        if (cube.mat == Cubes.Sand)
        {
            // sand on bare ground packs in
            g.ground.Raise(g.ground.NearestPoint(at), Ground.Cell);
            g.cubes.Remove(cube);
        }
        else if (cube.mat == Cubes.Oil && !cube.bit)
        {
            // oil on bare ground fails: it spreads out into quarter-height bits
            g.cubes.Remove(cube);
            for (int k = 0; k < Cubes.BitsPerCube; k++)
            {
                Vector3 offset = k == 0 ? Vector3.zero : Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * 0.35f;
                g.cubes.Spawn(Cubes.Oil, true, at + offset + Vector3.up * 0.1f, Quaternion.identity, offset * 4f + Vector3.up * 1.5f);
            }
        }
    }
}
