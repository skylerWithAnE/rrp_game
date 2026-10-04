using UnityEngine;

// What the shovel does. Runs on the host only; clients send a request with where they aimed.
//
//   Primary   empty shovel: scoop the block or loose cube at the target, or dig; either way
//                           the cube ends up on the shovel
//             loaded:       fling the cube
//   Secondary loaded:       set the cube down (a rock cube becomes a block)
//             empty:        smack the loose cube at the target, or flatten bare ground
public static class Verbs
{
    public const byte Primary = 0, Secondary = 1;

    public static void Do(Player p, byte verb, Vector3 target, Vector3 aim, byte kind = Aim.Ground, Vector3Int cell = default, Vector3Int normal = default)
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
        if (kind == Aim.Ground) target.y = g.ground.HeightAt(target.x, target.z);
        // a block has to be one that exists and is close by
        if (kind == Aim.Block && (!g.blocks.Has(cell) || (Blocks.Centre(cell) - p.transform.position).magnitude > t.reach + 2f)) kind = Aim.Ground;

        bool loaded = p.load != 0 && p.bits == 0;
        bool partial = p.bits > 0;
        int swing;
        if (verb == Primary)
        {
            if (loaded) { Fling(p, aim); swing = Blob.SwingFling; }
            else { Scoop(p, target, partial, kind, cell); swing = Blob.SwingScoop; }
        }
        else
        {
            if (loaded || partial) { SetDown(p, target, kind, cell, normal); swing = Blob.SwingScoop; }
            else { Smack(p, target, kind); swing = Blob.SwingSmack; }
        }

        var m = Msg.New(Op.VerbFx, 4);
        m.U8((byte)p.slot);
        m.U8((byte)swing);
        Net.ToClients(m, true);
        if (!p.isLocal) p.Swing(swing);
    }

    static void Scoop(Player p, Vector3 target, bool partial, byte kind, Vector3Int cell)
    {
        var g = Game.I;
        // a block comes back up as a rock cube; town blocks stay where they are
        if (kind == Aim.Block && !partial)
        {
            if (g.blocks.Permanent(cell)) return;
            g.blocks.Remove(cell, true);
            p.SetLoad(Cubes.Rock, 0);
            return;
        }
        var cube = g.cubes.Nearest(target + Vector3.up * Ground.Cell * 0.5f, g.tuning.pickRadius, partial);
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

        // earth lying on a roof
        if (kind == Aim.Earth && g.ground.DigUpper(g.ground.NearestPoint(target)))
        {
            p.SetLoad(Cubes.Sand, 0);
            return;
        }

        // dug ground goes straight onto the shovel; it only becomes a loose cube when flung or set down
        if (g.ground.Dig(g.ground.NearestPoint(target), out byte mat)) p.SetLoad(mat, 0);
    }

    static void Fling(Player p, Vector3 aim)
    {
        var g = Game.I;
        if (g.cubes.Full) return;
        aim.y = 0;
        aim = aim.sqrMagnitude > 0.001f ? aim.normalized : p.Forward;
        Vector3 velocity = aim * g.tuning.flingSpeed + Vector3.up * g.tuning.flingUp;
        g.cubes.Spawn((byte)p.load, false, p.ShovelPoint + Vector3.up * 0.6f, Random.rotation, velocity, p.slot);
        p.SetLoad(0, 0);
    }

    // A rock cube set down becomes a block: against the face of the block under the crosshair, or
    // else on top of whatever is at the target. Anything else is set down as a loose cube.
    static void SetDown(Player p, Vector3 target, byte kind, Vector3Int cell, Vector3Int normal)
    {
        var g = Game.I;
        if (p.load == Cubes.Rock && p.bits == 0)
        {
            Vector3Int place;
            if (kind == Aim.Block) place = cell + normal;
            else
            {
                int i = g.ground.NearestPoint(target);
                place = new Vector3Int(i % g.ground.w, Blocks.SurfaceLevel(i), i / g.ground.w);
            }
            if (!g.blocks.CanPlace(place)) return;
            g.blocks.Place(place, Blocks.Rock, false, true);
            p.SetLoad(0, 0);
            return;
        }
        if (g.cubes.Full) return;
        // on top of whatever is there: ground or another cube
        float top = target.y;
        if (Physics.Raycast(target + Vector3.up * 4f, Vector3.down, out var hit, 8f)) top = hit.point.y;
        if (p.bits > 0)
        {
            for (int k = 0; k < p.bits; k++)
                g.cubes.Spawn((byte)p.load, true, new Vector3(target.x, top + Ground.Cell * (0.15f + k * 0.27f), target.z), Quaternion.identity, Vector3.zero);
        }
        else g.cubes.Spawn((byte)p.load, false, new Vector3(target.x, top + Ground.Cell * 0.5f + 0.02f, target.z), Quaternion.Euler(0, p.yaw, 0), Vector3.zero);
        p.SetLoad(0, 0);
    }

    static readonly System.Collections.Generic.List<int> patch = new System.Collections.Generic.List<int>();

    // The points one smacked cube surfaces: the one it sits on, and its neighbours out to
    // `roadSpread`. Whether the smack works at all is decided by the middle point alone.
    static System.Collections.Generic.List<int> Patch(int i)
    {
        var ground = Game.I.ground;
        int spread = Mathf.RoundToInt(Game.I.tuning.roadSpread);
        int cx = i % ground.w, cz = i / ground.w;
        patch.Clear();
        for (int z = cz - spread; z <= cz + spread; z++)
            for (int x = cx - spread; x <= cx + spread; x++)
                if (x >= 1 && z >= 1 && x <= ground.w - 2 && z <= ground.d - 2) patch.Add(ground.Index(x, z));
        return patch;
    }

    // Smack a loose cube that is resting on a surface. What happens depends on the cube and on
    // what it rests on; see the table in DESIGN.md. A noise means it failed and the cube stays.
    static void Smack(Player p, Vector3 target, byte aimKind)
    {
        var g = Game.I;
        var ground = g.ground;
        var cube = g.cubes.Nearest(target + Vector3.up * Ground.Cell * 0.5f, g.tuning.pickRadius);
        if (cube == null)
        {
            if (aimKind == Aim.Ground) ground.Flatten(ground.NearestPoint(target), g.tuning.flattenStrength);
            return;
        }
        if (cube.rb.linearVelocity.sqrMagnitude > 1f) return;

        Vector3 at = cube.go.transform.position;
        int i = ground.NearestPoint(at);
        byte under = ground.surface[i];
        int kind = Cubes.Kind(cube.mat);

        // resting on blocks: sand packs down as earth on top of them, nothing else takes
        if (ground.Stacked(i) && at.y > ground.blockTop[i])
        {
            if (kind == Cubes.Sand)
            {
                ground.Raise(i, Ground.Cell);
                g.cubes.Remove(cube);
            }
            else if (kind == Cubes.Rock) Sfx.Broadcast(Sfx.Thud, at);
            else if (kind == Cubes.Oil) Sfx.Broadcast(Sfx.Squeak, at);
            return;
        }

        if (kind == Cubes.Paint)
        {
            // paint never fails: on asphalt it makes painted road, anywhere else it is cosmetic
            byte color = (byte)(Cubes.Tint(cube.mat) + 1);
            foreach (int j in Patch(i)) ground.SetSurface(j, ground.surface[j] >= Ground.Asphalt ? Ground.Painted : ground.surface[j], color);
            g.cubes.Remove(cube);
        }
        else if (kind == Cubes.Sand)
        {
            if (under != Ground.Bare) { Sfx.Broadcast(Sfx.Rough, at); return; }
            ground.SetSurface(i, Ground.Bare, 0);
            ground.Raise(i, Ground.Cell, true);
            g.cubes.Remove(cube);
        }
        else if (kind == Cubes.Rock)
        {
            if (under != Ground.Bare || !ground.IsFlat(i)) { Sfx.Broadcast(Sfx.Thud, at); return; }
            foreach (int j in Patch(i)) if (ground.surface[j] == Ground.Bare) ground.SetSurface(j, Ground.Gravel, ground.paint[j]);
            g.cubes.Remove(cube);
        }
        else if (kind == Cubes.Oil)
        {
            if (under == Ground.Gravel && !cube.bit)
            {
                foreach (int j in Patch(i)) if (ground.surface[j] == Ground.Gravel) ground.SetSurface(j, Ground.Asphalt, 0);
                g.cubes.Remove(cube);
                return;
            }
            Sfx.Broadcast(Sfx.Squeak, at);
            if (cube.bit) return;
            // a failed oil cube spreads out into quarter-height bits
            g.cubes.Remove(cube);
            for (int k = 0; k < Cubes.BitsPerCube; k++)
            {
                Vector3 offset = k == 0 ? Vector3.zero : Quaternion.Euler(0, k * 90f, 0) * Vector3.forward * Ground.Cell * 0.7f;
                g.cubes.Spawn(Cubes.Oil, true, at + offset + Vector3.up * 0.1f, Quaternion.identity, offset * 2f + Vector3.up * 1.5f);
            }
        }
    }
}
