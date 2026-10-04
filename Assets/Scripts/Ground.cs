using System.Collections.Generic;
using UnityEngine;

// The heightfield. Heights live on grid points one cube-width apart, so lowering a single point by
// one cube height removes exactly one cube's volume. Only the host changes heights; it broadcasts
// the resulting values and every client applies the same change.
public class Ground : MonoBehaviour
{
    public const float Cell = 0.5f;   // one cube
    const int ChunkCells = 16;
    const int RebuildsPerFrame = 6;

    public int w, d;                  // points per side
    public float[] h;                 // current height at each point
    float[] h0;                       // height at generation, for tinting what was dug or packed

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
        w = d = hill ? 129 : 41;
        h = new float[w * d];
        h0 = new float[w * d];

        float baseHeight = 4f;
        var rng = new System.Random(seed);
        float ox = (float)rng.NextDouble() * 100f, oz = (float)rng.NextDouble() * 100f;
        Vector2 centre = new Vector2(SizeX, SizeZ) * 0.5f;
        for (int z = 0; z < d; z++)
        {
            for (int x = 0; x < w; x++)
            {
                float y = baseHeight;
                if (hill)
                {
                    Vector2 p = new Vector2(x, z) * Cell;
                    float r = (p - centre).magnitude;
                    y += 8f * Mathf.Exp(-r * r / (2f * 9f * 9f));
                    y += (Mathf.PerlinNoise(ox + p.x * 0.08f, oz + p.y * 0.08f) - 0.5f) * 1.2f;
                }
                h[Index(x, z)] = h0[Index(x, z)] = y;
            }
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

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        chunks = null;
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

    public uint Hash()
    {
        uint hash = 2166136261;
        for (int i = 0; i < h.Length; i++) hash = (hash ^ (uint)Mathf.RoundToInt(h[i] * 100f)) * 16777619;
        return hash;
    }

    // ---- host edits

    public void Set(int i, float v)
    {
        v = Mathf.Max(0, v);
        if (h[i] == v) return;
        h[i] = v;
        MarkDirty(i);
        if (pendingSet.Add(i)) pending.Add(i);
    }

    // Scoop: one cube's volume out of the point. False at bedrock.
    public bool Dig(int i)
    {
        if (h[i] < Cell * 0.5f) return false;
        Set(i, h[i] - Cell);
        Disturb(i);
        return true;
    }

    public void Raise(int i, float amount)
    {
        Set(i, h[i] + amount);
        Disturb(i);
    }

    // Smack on bare ground: pull a point and its neighbours toward their average. Keeps volume.
    public void Flatten(int i, float strength)
    {
        int x = i % w, z = i / w;
        float sum = 0;
        for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++) sum += h[Index(x + dx, z + dz)];
        float mean = sum / 9f;
        for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                int j = Index(x + dx, z + dz);
                if (!Edge(j)) Set(j, Mathf.Lerp(h[j], mean, strength));
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
                    float drop = h[i] - h[j];
                    if (drop <= slope + 0.01f) continue;
                    if (WallHeight(x, z, k, slope) < tall) continue;
                    float move = (drop - slope) * 0.35f + 0.005f;
                    Set(i, h[i] - move);
                    Set(j, h[j] + move);
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
        float top = h[Index(x, z)], bottom = top;
        int ux = x, uz = z;
        for (int s = 0; s < 8; s++)
        {
            int nx = ux - DirX[k], nz = uz - DirZ[k];
            if (nx < 0 || nz < 0 || nx >= w || nz >= d) break;
            float v = h[Index(nx, nz)];
            if (v - top <= slope * 0.75f) break;
            top = v; ux = nx; uz = nz;
        }
        int bx = x, bz = z;
        for (int s = 0; s < 8; s++)
        {
            int nx = bx + DirX[k], nz = bz + DirZ[k];
            if (nx < 0 || nz < 0 || nx >= w || nz >= d) break;
            float v = h[Index(nx, nz)];
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
        const int perMessage = 400;
        for (int start = 0; start < pending.Count; start += perMessage)
        {
            int count = Mathf.Min(perMessage, pending.Count - start);
            var m = Msg.New(Op.GroundEdit, 8 + count * 8);
            m.U16((ushort)count);
            for (int k = 0; k < count; k++)
            {
                int i = pending[start + k];
                m.I32(i);
                m.F32(h[i]);
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
            float v = m.F32();
            if (!Ready || i < 0 || i >= h.Length) continue;
            h[i] = v;
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

    static readonly Color Sand = new Color(0.87f, 0.74f, 0.50f);
    static readonly Color Dug = new Color(0.66f, 0.52f, 0.34f);
    static readonly Color Packed = new Color(0.95f, 0.86f, 0.64f);

    Color32 ColorAt(int i)
    {
        float delta = h[i] - h0[i];
        Color c = delta < 0 ? Color.Lerp(Sand, Dug, Mathf.Clamp01(-delta / 1.5f)) : Color.Lerp(Sand, Packed, Mathf.Clamp01(delta / 1f));
        // a little fixed speckle so flat sand is not one flat color
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
