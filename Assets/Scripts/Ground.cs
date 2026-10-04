using System.Collections.Generic;
using UnityEngine;

// The heightfield. Heights live on grid points one cube-width apart (a cube is as tall as a blob), so lowering a single point by
// one cube height removes exactly one cube's volume. Each point also has a depth of sand over rock,
// maybe a buried pocket of oil or paint, a road surface and a coat of paint.
// Only the host changes any of it; it broadcasts the resulting values and every client applies them.
public class Ground : MonoBehaviour
{
    public const float Cell = 1f;     // one cube: the size of everything the shovel touches
    public const float TownInset = 12f; // how far each town sits from its end of the map
    public const byte Bare = 0, Gravel = 1, Asphalt = 2, Painted = 3;
    const int ChunkCells = 16;
    const int RebuildsPerFrame = 6;

    public int w, d;                  // points per side
    public float[] h;                 // current height at each point
    public float[] sand;              // sand lying on top of the rock at each point
    public byte[] surface;            // Bare, Gravel, Asphalt, Painted
    public byte[] paint;              // 0 none, else paint color + 1 (cosmetic unless the surface is Painted)
    public bool[] locked;             // town ground: nothing can change it
    // The one special case: where blocks stand above the ground, earth can lie on top of them
    // as a second layer. That is what buries a tunnel roof.
    public float[] blockTop;          // top of the highest block at this point, 0 if none
    public float[] upper;             // earth lying on top of those blocks
    readonly Dictionary<int, Transform> upperBoxes = new Dictionary<int, Transform>();
    readonly Dictionary<int, int> upperByCollider = new Dictionary<int, int>();
    Material earthMaterial;
    public Vector3 siteA, siteB;      // the middle of each town's pad
    float[] h0;                       // height at generation, for tinting what was dug or packed
    byte[] pocket;                    // 0 none, else the cube material buried in the rock here
    float[] pocketLow, pocketHigh;    // the heights that pocket spans

    Chunk[] chunks;
    int chunksX, chunksZ;
    Material material;

    readonly List<int> pending = new List<int>();     // host: points changed since the last flush
    readonly HashSet<int> pendingSet = new HashSet<int>();

    // host: collapse works inside this box of points until nothing moves
    bool collapsing;
    int cx0, cz0, cx1, cz1;
    float collapseTimer;
    public bool quakeMode;            // use the quake thresholds

    class Chunk
    {
        public Mesh mesh;
        public MeshCollider collider;
        public bool dirty;
        public Vector3[] verts;
        public Color32[] colors;
    }

    public float SizeX => (w - 1) * Cell;
    public float SizeZ => (d - 1) * Cell;
    public int Index(int x, int z) { return z * w + x; }
    public Vector3 PointPos(int i) { return new Vector3(i % w * Cell, h[i], i / w * Cell); }

    public void Generate(bool hill, int seed)
    {
        Clear();
        material = Mats.Make(Color.white, true);
        w = d = hill ? 65 : 21;
        int n = w * d;
        h = new float[n];
        h0 = new float[n];
        sand = new float[n];
        surface = new byte[n];
        paint = new byte[n];
        locked = new bool[n];
        blockTop = new float[n];
        upper = new float[n];
        earthMaterial = Mats.Make(PackedColor);
        pocket = new byte[n];
        pocketLow = new float[n];
        pocketHigh = new float[n];

        float baseHeight = 4f;
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 100f, oz = (float)rng.NextDouble() * 100f;
        Vector2 centre = new Vector2(SizeX, SizeZ) * 0.5f;
        for (int z = 0; z < d; z++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = Index(x, z);
                float y = baseHeight;
                sand[i] = 100f; // the lobby is all sand
                if (hill)
                {
                    Vector2 p = new Vector2(x, z) * Cell;
                    float r = (p - centre).magnitude;
                    y += 8f * Mathf.Exp(-r * r / (2f * 9f * 9f));
                    y += (Mathf.PerlinNoise(ox + p.x * 0.08f, oz + p.y * 0.08f) - 0.5f) * 1.2f;
                    sand[i] = 0.8f + Mathf.PerlinNoise(oz + p.x * 0.05f, ox + p.y * 0.05f) * 1.4f;
                }
                h[i] = h0[i] = y;
            }
        }
        if (hill)
        {
            siteA = Pad(TownInset);
            siteB = Pad(SizeZ - TownInset);
            for (int k = 0; k < 8; k++) Pocket(rng, Cubes.Oil);
            for (int k = 0; k < 7; k++) Pocket(rng, Cubes.PaintOf(k % Mats.PaintColors.Length));
        }

        chunksX = (w - 1 + ChunkCells - 1) / ChunkCells;
        chunksZ = (d - 1 + ChunkCells - 1) / ChunkCells;
        chunks = new Chunk[chunksX * chunksZ];
        for (int i = 0; i < chunks.Length; i++)
        {
            var go = new GameObject("Chunk");
            go.transform.SetParent(transform, false);
            var c = chunks[i] = new Chunk();
            c.mesh = new Mesh();
            c.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = c.mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            c.collider = go.AddComponent<MeshCollider>();
            Rebuild(i);
        }
    }

    // A town's pad: a level 3 by 3 patch of finished, painted road that can never be changed. The players'
    // road has to reach it. Returns the middle of the pad.
    Vector3 Pad(float z)
    {
        int cx = w / 2, cz = Mathf.RoundToInt(z / Cell);
        float level = h[Index(cx, cz)];
        for (int dz = -12; dz <= 12; dz++)
            for (int dx = -12; dx <= 12; dx++)
            {
                float distance = new Vector2(dx, dz).magnitude * Cell;
                if (distance > 5.5f) continue;
                int i = Index(cx + dx, cz + dz);
                h[i] = h0[i] = Mathf.Lerp(level, h[i], Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.3f, 5.5f, distance)));
                if (Mathf.Abs(dx) > 1 || Mathf.Abs(dz) > 1) continue;
                surface[i] = Painted;
                paint[i] = 2;
                locked[i] = true;
            }
        return new Vector3(cx * Cell, level, cz * Cell);
    }

    // A round pocket of one material, buried in the rock a little below the sand.
    void Pocket(System.Random rng, byte mat)
    {
        float px = 8f + (float)rng.NextDouble() * (SizeX - 16f), pz = 8f + (float)rng.NextDouble() * (SizeZ - 16f);
        float radius = 2f + (float)rng.NextDouble() * 1.5f;
        float below = (float)rng.NextDouble() * 0.6f, thickness = 1f + (float)rng.NextDouble() * 0.5f;
        int reach = Mathf.CeilToInt(radius / Cell);
        int cx = Mathf.RoundToInt(px / Cell), cz = Mathf.RoundToInt(pz / Cell);
        for (int z = cz - reach; z <= cz + reach; z++)
            for (int x = cx - reach; x <= cx + reach; x++)
            {
                if (x < 0 || z < 0 || x >= w || z >= d) continue;
                if (new Vector2(x - cx, z - cz).magnitude * Cell > radius) continue;
                int i = Index(x, z);
                if (pocket[i] != 0) continue;
                pocket[i] = mat;
                pocketHigh[i] = h0[i] - sand[i] - below;
                pocketLow[i] = pocketHigh[i] - thickness;
            }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        chunks = null;
        upperBoxes.Clear();
        upperByCollider.Clear();
        pending.Clear();
        pendingSet.Clear();
        collapsing = false;
        quakeMode = false;
    }

    // ---- queries

    public bool Ready => h != null && chunks != null;

    public int NearestPoint(Vector3 p)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(p.x / Cell), 1, w - 2);
        int z = Mathf.Clamp(Mathf.RoundToInt(p.z / Cell), 1, d - 2);
        return Index(x, z);
    }

    public float HeightAt(float px, float pz)
    {
        if (!Ready) return 0;
        float fx = Mathf.Clamp(px / Cell, 0, w - 1.001f), fz = Mathf.Clamp(pz / Cell, 0, d - 1.001f);
        int x = (int)fx, z = (int)fz;
        fx -= x; fz -= z;
        float h00 = h[Index(x, z)], h10 = h[Index(x + 1, z)], h01 = h[Index(x, z + 1)], h11 = h[Index(x + 1, z + 1)];
        // Matches the mesh: each cell is split along the 00-11 diagonal.
        return fx > fz ? h00 + fx * (h10 - h00) + fz * (h11 - h10) : h00 + fz * (h01 - h00) + fx * (h11 - h01);
    }

    public Vector3 Clamp(Vector3 p, float margin)
    {
        p.x = Mathf.Clamp(p.x, margin, SizeX - margin);
        p.z = Mathf.Clamp(p.z, margin, SizeZ - margin);
        return p;
    }

    // Blocks standing proud of the ground at this point?
    public bool Stacked(int i) { return blockTop[i] > h[i] + 0.01f; }

    // The top of whatever is at this point: the ground, or the blocks and any earth on them.
    public float Surface(int i) { return Stacked(i) ? blockTop[i] + upper[i] : h[i]; }

    public bool TryUpper(Collider collider, out int i) { return upperByCollider.TryGetValue(collider.GetInstanceID(), out i); }

    // What a scoop here would bring up: sand while there is any, then rock or whatever is buried.
    public byte MaterialAt(int i, float y)
    {
        if (sand[i] > 0.02f) return Cubes.Sand;
        if (pocket[i] != 0 && y <= pocketHigh[i] && y >= pocketLow[i]) return pocket[i];
        return Cubes.Rock;
    }

    // Level enough to take gravel: the point sits on the line between its neighbours, both ways.
    // A steady slope counts as flat; a bump or a dip does not.
    public bool IsFlat(int i)
    {
        float tolerance = Game.I.tuning.gravelFlatness;
        return Mathf.Abs(h[i] - (h[i - 1] + h[i + 1]) * 0.5f) <= tolerance
            && Mathf.Abs(h[i] - (h[i - w] + h[i + w]) * 0.5f) <= tolerance;
    }

    public uint Hash()
    {
        uint hash = 2166136261;
        for (int i = 0; i < h.Length; i++)
            hash = (hash ^ (uint)(Mathf.RoundToInt(h[i] * 100f) + Mathf.RoundToInt(upper[i] * 100f) * 31 + surface[i] * 7919 + paint[i] * 104729)) * 16777619;
        return hash;
    }

    // ---- host edits

    // Every height change goes through here. Added height is sand; removed height takes sand first.
    public void Set(int i, float v)
    {
        v = Mathf.Max(0, v);
        if (h[i] == v || locked[i]) return;
        sand[i] = Mathf.Max(0, sand[i] + v - h[i]);
        h[i] = v;
        Touch(i);
    }

    void Touch(int i)
    {
        RefreshUpper(i);
        MarkDirty(i);
        if (pendingSet.Add(i)) pending.Add(i);
    }

    // Scoop: one cube's volume out of the point, and any road on it. False at bedrock.
    public bool Dig(int i, out byte mat)
    {
        mat = 0;
        if (h[i] < Cell * 0.5f || locked[i]) return false;
        mat = MaterialAt(i, h[i] - Cell * 0.5f);
        Set(i, h[i] - Cell);
        if (surface[i] != Bare || paint[i] != 0) SetSurface(i, Bare, 0);
        Disturb(i);
        return true;
    }

    // Add earth at a point. Where blocks stand above the ground it lands on top of them, unless
    // it is known to be underneath (a cube lying on a tunnel floor).
    public void Raise(int i, float amount, bool underBlocks = false)
    {
        if (locked[i]) return;
        if (Stacked(i) && !underBlocks)
        {
            upper[i] += amount;
            Touch(i);
        }
        else Set(i, h[i] + amount);
        Disturb(i);
    }

    // Scoop from the earth lying on top of blocks.
    public bool DigUpper(int i)
    {
        if (!Stacked(i) || upper[i] < 0.05f) return false;
        upper[i] = Mathf.Max(0, upper[i] - Cell);
        Touch(i);
        Disturb(i);
        return true;
    }

    // Blocks were added or removed at this point.
    public void SetBlockTop(int i, float top)
    {
        blockTop[i] = top;
        if (Net.IsHost && upper[i] > 0 && !Stacked(i))
        {
            // nothing holds the earth up any more: it drops onto the ground
            float fallen = upper[i];
            upper[i] = 0;
            Set(i, h[i] + fallen);
            Touch(i);
        }
        if (Net.IsHost) Disturb(i);
        RefreshUpper(i);
    }

    // The earth on top of blocks is drawn as a plain box per point.
    void RefreshUpper(int i)
    {
        bool want = upper[i] > 0.01f && Stacked(i);
        upperBoxes.TryGetValue(i, out var box);
        if (!want)
        {
            if (box == null) return;
            upperByCollider.Remove(box.GetComponent<Collider>().GetInstanceID());
            Destroy(box.gameObject);
            upperBoxes.Remove(i);
            return;
        }
        if (box == null)
        {
            box = Mats.Part(transform, Mats.Cube, earthMaterial, Vector3.zero, Vector3.one);
            box.name = "Earth";
            upperByCollider[box.gameObject.AddComponent<BoxCollider>().GetInstanceID()] = i;
            upperBoxes[i] = box;
        }
        box.position = new Vector3(i % w * Cell, blockTop[i] + upper[i] * 0.5f, i / w * Cell);
        box.localScale = new Vector3(Cell, upper[i], Cell);
    }

    public void SetSurface(int i, byte newSurface, byte newPaint)
    {
        if (locked[i] || (surface[i] == newSurface && paint[i] == newPaint)) return;
        if (surface[i] != newSurface) Game.I.road.dirty = true;
        surface[i] = newSurface;
        paint[i] = newPaint;
        Touch(i);
    }

    // Earthquake: road within reach drops one tier. Each point only once per quake.
    public void Crack(Vector3 centre, float radius, HashSet<int> done)
    {
        int reach = Mathf.CeilToInt(radius / Cell);
        int cx = Mathf.RoundToInt(centre.x / Cell), cz = Mathf.RoundToInt(centre.z / Cell);
        for (int z = cz - reach; z <= cz + reach; z++)
            for (int x = cx - reach; x <= cx + reach; x++)
            {
                if (x < 1 || z < 1 || x > w - 2 || z > d - 2) continue;
                if (new Vector2(x - cx, z - cz).magnitude * Cell > radius) continue;
                int i = Index(x, z);
                if (surface[i] == Bare || !done.Add(i)) continue;
                SetSurface(i, (byte)(surface[i] - 1), 0);
            }
    }

    // Smack on bare ground: pull a point and its bare neighbours toward their average. Keeps volume
    // when all nine are bare.
    public void Flatten(int i, float strength)
    {
        if (surface[i] != Bare) return;
        int x = i % w, z = i / w;
        float sum = 0;
        for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) sum += h[Index(x + dx, z + dz)];
        float mean = sum / 9f;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int j = Index(x + dx, z + dz);
                if (!Edge(j) && surface[j] == Bare) Set(j, Mathf.Lerp(h[j], mean, strength));
            }
    }

    bool Edge(int i)
    {
        int x = i % w, z = i / w;
        return x == 0 || z == 0 || x == w - 1 || z == d - 1;
    }

    // Tell collapse to look around a changed point.
    void Disturb(int i)
    {
        int x = i % w, z = i / w;
        if (!collapsing) { cx0 = cx1 = x; cz0 = cz1 = z; collapsing = true; }
        Grow(x, z, 3);
    }

    void Grow(int x, int z, int by)
    {
        cx0 = Mathf.Max(1, Mathf.Min(cx0, x - by));
        cz0 = Mathf.Max(1, Mathf.Min(cz0, z - by));
        cx1 = Mathf.Min(w - 2, Mathf.Max(cx1, x + by));
        cz1 = Mathf.Min(d - 2, Mathf.Max(cz1, z + by));
    }

    public void DisturbAll()
    {
        collapsing = true;
        cx0 = cz0 = 1;
        cx1 = w - 2;
        cz1 = d - 2;
    }

    static readonly int[] DirX = { 1, -1, 0, 0 };
    static readonly int[] DirZ = { 0, 0, 1, -1 };

    // One pass: wherever a wall is both steeper than the stable slope and taller than the limit,
    // slide part of the excess one point downhill. Repeated passes look like a slump.
    bool CollapsePass()
    {
        var t = Game.I.tuning;
        float slope = quakeMode ? t.quakeSlope : t.collapseSlope;
        float tall = quakeMode ? t.quakeWallHeight : t.collapseHeight;
        bool moved = false;
        int x0 = cx0, x1 = cx1, z0 = cz0, z1 = cz1;
        for (int z = z0; z <= z1; z++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int i = Index(x, z);
                for (int k = 0; k < 4; k++)
                {
                    int jx = x + DirX[k], jz = z + DirZ[k];
                    if (jx < 1 || jz < 1 || jx > w - 2 || jz > d - 2) continue;
                    int j = Index(jx, jz);
                    if (locked[i] || locked[j]) continue;
                    // Blocks never move. A wall with blocks standing as high as it is has nothing to
                    // fall onto, so it is reinforced; only loose earth on top of blocks can slide.
                    float drop = Surface(i) - Surface(j);
                    if (drop <= slope + 0.01f) continue;
                    bool stacked = Stacked(i);
                    if (stacked && upper[i] < 0.001f) continue;
                    if (WallHeight(x, z, k, slope) < tall) continue;
                    float move = (drop - slope) * 0.35f + 0.005f;
                    if (stacked)
                    {
                        move = Mathf.Min(move, upper[i]);
                        upper[i] -= move;
                        Touch(i);
                    }
                    else Set(i, h[i] - move);
                    if (Stacked(j)) { upper[j] += move; Touch(j); }
                    else Set(j, h[j] + move);
                    Grow(jx, jz, 2);
                    moved = true;
                }
            }
        }
        return moved;
    }

    // Height of the steep run through (x,z) going in direction k: follow it up behind and down ahead.
    float WallHeight(int x, int z, int k, float slope)
    {
        float top = Surface(Index(x, z)), bottom = top;
        int ux = x, uz = z;
        for (int s = 0; s < 8; s++)
        {
            int nx = ux - DirX[k], nz = uz - DirZ[k];
            if (nx < 0 || nz < 0 || nx >= w || nz >= d) break;
            float v = Surface(Index(nx, nz));
            if (v - top <= slope * 0.75f) break;
            top = v; ux = nx; uz = nz;
        }
        int bx = x, bz = z;
        for (int s = 0; s < 8; s++)
        {
            int nx = bx + DirX[k], nz = bz + DirZ[k];
            if (nx < 0 || nz < 0 || nx >= w || nz >= d) break;
            float v = Surface(Index(nx, nz));
            if (bottom - v <= slope * 0.75f) break;
            bottom = v; bx = nx; bz = nz;
        }
        return top - bottom;
    }

    void Update()
    {
        if (!Ready) return;

        if (Net.IsHost)
        {
            if (collapsing)
            {
                collapseTimer -= Time.deltaTime;
                if (collapseTimer <= 0)
                {
                    collapseTimer = 0.08f;
                    if (!CollapsePass()) collapsing = false;
                }
            }
            Flush();
        }

        int budget = RebuildsPerFrame;
        for (int i = 0; i < chunks.Length && budget > 0; i++)
        {
            if (!chunks[i].dirty) continue;
            Rebuild(i);
            budget--;
        }
    }

    public bool Collapsing => collapsing;

    // host: send what changed, and wake any cube sitting on it
    void Flush()
    {
        if (pending.Count == 0) return;
        Vector3 min = new Vector3(float.MaxValue, 0, float.MaxValue), max = new Vector3(float.MinValue, 0, float.MinValue);
        const int perMessage = 220;
        for (int start = 0; start < pending.Count; start += perMessage)
        {
            int count = Mathf.Min(perMessage, pending.Count - start);
            var m = Msg.New(Op.GroundEdit, 8 + count * 18);
            m.U16((ushort)count);
            for (int k = 0; k < count; k++)
            {
                int i = pending[start + k];
                m.I32(i);
                m.F32(h[i]);
                m.F32(sand[i]);
                m.F32(upper[i]);
                m.U8(surface[i]);
                m.U8(paint[i]);
                Vector3 p = PointPos(i);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
            Net.ToClients(m, true);
        }
        pending.Clear();
        pendingSet.Clear();
        Game.I.cubes.WakeNear(min, max);
    }

    // client: apply the host's values
    public void ApplyEdits(Msg m)
    {
        int count = m.U16();
        for (int k = 0; k < count; k++)
        {
            int i = m.I32();
            float height = m.F32(), sandDepth = m.F32(), onBlocks = m.F32();
            byte newSurface = m.U8(), newPaint = m.U8();
            if (!Ready || i < 0 || i >= h.Length) continue;
            h[i] = height;
            sand[i] = sandDepth;
            upper[i] = onBlocks;
            RefreshUpper(i);
            surface[i] = newSurface;
            paint[i] = newPaint;
            MarkDirty(i);
        }
    }

    // ---- mesh

    void MarkDirty(int i)
    {
        int x = i % w, z = i / w;
        // A point on a chunk border belongs to both neighbours.
        for (int dz = -1; dz <= 0; dz++)
            for (int dx = -1; dx <= 0; dx++)
            {
                int qx = (x + dx) / ChunkCells, qz = (z + dz) / ChunkCells;
                if (x + dx < 0 || z + dz < 0 || qx >= chunksX || qz >= chunksZ) continue;
                chunks[qz * chunksX + qx].dirty = true;
            }
    }

    static readonly Color SandColor = new Color(0.87f, 0.74f, 0.50f);
    static readonly Color DugColor = new Color(0.66f, 0.52f, 0.34f);
    static readonly Color PackedColor = new Color(0.95f, 0.86f, 0.64f);
    static readonly Color RockColor = new Color(0.50f, 0.47f, 0.46f);
    static readonly Color GravelColor = new Color(0.62f, 0.61f, 0.60f);
    static readonly Color AsphaltColor = new Color(0.17f, 0.17f, 0.19f);

    Color32 ColorAt(int i)
    {
        Color c;
        if (surface[i] == Painted) c = Mats.PaintColors[paint[i] - 1];
        else if (surface[i] == Asphalt) c = AsphaltColor;
        else
        {
            if (surface[i] == Gravel) c = GravelColor;
            else
            {
                byte mat = MaterialAt(i, h[i] - 0.05f);
                if (mat == Cubes.Sand)
                {
                    float delta = h[i] - h0[i];
                    c = delta < 0 ? Color.Lerp(SandColor, DugColor, Mathf.Clamp01(-delta / 1.5f)) : Color.Lerp(SandColor, PackedColor, Mathf.Clamp01(delta / 1f));
                }
                else c = mat == Cubes.Rock ? RockColor : Cubes.ColorOf(mat);
            }
            // cosmetic paint: a wash over whatever is underneath
            if (paint[i] != 0) c = Color.Lerp(c, Mats.PaintColors[paint[i] - 1], 0.55f);
        }
        // a little fixed speckle so flat ground is not one flat color
        float speckle = (((uint)i * 2654435761u) >> 24) / 255f;
        return c * (0.96f + 0.08f * speckle);
    }

    void Rebuild(int ci)
    {
        var c = chunks[ci];
        int x0 = ci % chunksX * ChunkCells, z0 = ci / chunksX * ChunkCells;
        int nx = Mathf.Min(ChunkCells, w - 1 - x0) + 1, nz = Mathf.Min(ChunkCells, d - 1 - z0) + 1;

        bool first = c.verts == null;
        if (first)
        {
            c.verts = new Vector3[nx * nz];
            c.colors = new Color32[nx * nz];
        }
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int i = Index(x0 + x, z0 + z);
                c.verts[z * nx + x] = new Vector3((x0 + x) * Cell, h[i], (z0 + z) * Cell);
                c.colors[z * nx + x] = ColorAt(i);
            }
        c.mesh.vertices = c.verts;
        c.mesh.colors32 = c.colors;
        if (first)
        {
            var tris = new int[(nx - 1) * (nz - 1) * 6];
            int t = 0;
            for (int z = 0; z < nz - 1; z++)
                for (int x = 0; x < nx - 1; x++)
                {
                    int a = z * nx + x, b = a + 1, e = a + nx, f = e + 1;
                    tris[t++] = a; tris[t++] = e; tris[t++] = f;
                    tris[t++] = a; tris[t++] = f; tris[t++] = b;
                }
            c.mesh.triangles = tris;
        }
        c.mesh.RecalculateNormals();
        c.mesh.RecalculateBounds();
        c.collider.sharedMesh = null;
        c.collider.sharedMesh = c.mesh;
        c.dirty = false;
    }
}
