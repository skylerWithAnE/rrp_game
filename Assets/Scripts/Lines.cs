using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Paint drawn by hand, on the Painting ground's two strips of rolled asphalt. The ground's own
// points are 0.25 m apart, which is too coarse for a line 0.15 m wide, so paint is remembered
// here in cells of 0.1 m: nothing, white, yellow, or tar (which covers paint and is not paint).
//
// Four things put paint down or take it off, and all of them are strokes: a line from one spot
// to another, so wide.
//   the roller brush   a stripe as wide as the roller, wherever it is dragged: white on the left
//                      button, yellow on the right
//   the paint truck    a stripe behind each of its two nozzles, wherever the truck actually is
//   the tar spray      blobs scattered round where it points: wide and imprecise. Tar covers paint
//   the grinder        a narrow, exact line that takes paint and tar off
//
// A player's machine works out the strokes and asks the host; the host applies each and sends it
// on to everyone, in order. A stroke is whole millimetres and is applied with whole-number sums,
// so every machine ends up with exactly the same cells.
//
// The judging. Each strip should have a white line inside each edge and a yellow one down the
// middle (broken or solid, on a slider). Round where each line should be is a band, as wide as
// the paintBand slider. A grid square scores for every 0.1 m of its length that has paint of the
// right color somewhere in the band, and loses for paint anywhere else. The number is shown on
// the F3 readout and nowhere else: the player is not meant to see it.
public class Lines : MonoBehaviour
{
    public const float Cell = 0.1f;
    public const int None = 0, White = 1, Yellow = 2, Tar = 3;
    public const int Grind = 0;             // a stroke's tool: one of the three above, or this
    const int ChunkCells = 32;
    const float EdgeInset = 0.375f;         // an edge line runs this far inside the road's edge, as the painted lines of the Paving ground do
    const float Dash = 3f;                  // a broken centre line: this much line, then this much gap

    Plot plot;
    public bool marked;                     // the place for each line is marked on the asphalt
    int cols, rows;
    byte[] paint;
    byte[] want;                            // per cell: 0 not road, 1 road, 2 a white line belongs here, 3 a yellow one
    byte[] mid;                             // per cell: 1 if it is on the middle of a band, where the mark goes
    int[] square;                           // per cell: which grid square it is in (see Key), or -1
    int[] slice;                            // per cell: which 0.1 m of its section's length it is in
    byte[] lineOf;                          // per cell: which of the three lines its band belongs to: 0 left edge, 1 centre, 2 right edge
    float[] height;                         // per cell: the top of the asphalt
    string wantFor = "";
    bool scoreDirty = true;
    float nextScore;

    class Chunk { public Mesh mesh; public bool dirty; public int col0, row0; }
    Chunk[] chunks;
    int chunksX, chunksZ;
    LineRenderer ring;

    // the score, worked out a couple of times a second
    public float total;                     // the whole strip, -100 to 100
    readonly Dictionary<int, Vector3Int> squares = new Dictionary<int, Vector3Int>();  // per grid square: lengths wanted, lengths covered, cells astray
    readonly Dictionary<long, bool> covered = new Dictionary<long, bool>();

    static readonly Color32 PaintWhite = new Color32(236, 236, 228, 255);
    static readonly Color32 PaintYellow = new Color32(236, 200, 40, 255);
    static readonly Color32 TarBlack = new Color32(14, 12, 12, 255);
    static readonly Color32 Mark = new Color32(84, 84, 92, 255);

    public void Build(Plot plot, bool marked)
    {
        this.plot = plot;
        this.marked = marked;
        cols = Mathf.CeilToInt(plot.Width / Cell);
        rows = Mathf.CeilToInt(plot.Depth / Cell);
        paint = new byte[cols * rows];
        want = new byte[paint.Length];
        mid = new byte[paint.Length];
        square = new int[paint.Length];
        slice = new int[paint.Length];
        lineOf = new byte[paint.Length];
        height = new float[paint.Length];
        chunksX = (cols + ChunkCells - 1) / ChunkCells;
        chunksZ = (rows + ChunkCells - 1) / ChunkCells;
        chunks = new Chunk[chunksX * chunksZ];
        var material = Mats.Make(Color.white);
        for (int c = 0; c < chunks.Length; c++)
        {
            var chunk = chunks[c] = new Chunk { col0 = c % chunksX * ChunkCells, row0 = c / chunksX * ChunkCells, dirty = true };
            var go = new GameObject("Paint");
            go.transform.SetParent(transform, false);
            chunk.mesh = new Mesh();
            chunk.mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = chunk.mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var ringGo = new GameObject("Paint cursor");
        ringGo.transform.SetParent(transform, false);
        ring = ringGo.AddComponent<LineRenderer>();
        ring.sharedMaterial = Mats.Unlit;
        ring.loop = true;
        ring.useWorldSpace = true;
        ring.widthMultiplier = 0.03f;
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring.enabled = false;
        Wanted();
    }

    Vector3 Centre(int i) { return plot.Origin + new Vector3((i % cols + 0.5f) * Cell, 0, (i / cols + 0.5f) * Cell); }

    // Where each line should be, from the sliders. Worked out again when one of them moves.
    void Wanted()
    {
        var t = Game.I.tuning;
        string signature = t.paintBand + " " + t.centreBroken + " " + t.patchWidth;
        if (signature == wantFor) return;
        wantFor = signature;
        float lane = plot.LaneWidth, half = t.paintBand * 0.5f, edge = lane - EdgeInset;
        bool broken = t.centreBroken >= 0.5f;
        for (int i = 0; i < paint.Length; i++)
        {
            Vector3 p = Centre(i);
            want[i] = mid[i] = 0;
            square[i] = -1;
            if (!plot.IsPaved(p.x, p.z) || !plot.Where(p.x, p.z, out int link, out float along, out float side) || Mathf.Abs(side) > lane) continue;
            height[i] = plot.HeightAt(p.x, p.z);
            float run = along * plot.Length(link);
            want[i] = 1;
            slice[i] = Mathf.FloorToInt(run / Cell);
            square[i] = plot.SquareKey(link, along, side);
            float fromEdge = Mathf.Abs(Mathf.Abs(side) - edge);
            if (fromEdge <= half) { want[i] = 2; lineOf[i] = (byte)(side < 0 ? 0 : 2); mid[i] = (byte)(fromEdge <= Cell * 0.5f ? 1 : 0); }
            else if (Mathf.Abs(side) <= half && (!broken || run % (Dash * 2f) < Dash)) { want[i] = 3; lineOf[i] = 1; mid[i] = (byte)(Mathf.Abs(side) <= Cell * 0.5f ? 1 : 0); }
        }
        foreach (var chunk in chunks) chunk.dirty = true;
        scoreDirty = true;
    }

    // ---- strokes

    // One stroke: from (ax, az) to (bx, bz), in millimetres from the plot's corner, `radius`
    // millimetres to each side. Every cell of rolled asphalt whose middle is within it takes
    // the tool. Whole-number sums only, so that every machine agrees.
    public void Apply(int tool, int ax, int az, int bx, int bz, int radius)
    {
        const int Mm = 100;     // a cell, in millimetres
        int x0 = Mathf.Max(0, (Mathf.Min(ax, bx) - radius) / Mm), x1 = Mathf.Min(cols - 1, (Mathf.Max(ax, bx) + radius) / Mm);
        int z0 = Mathf.Max(0, (Mathf.Min(az, bz) - radius) / Mm), z1 = Mathf.Min(rows - 1, (Mathf.Max(az, bz) + radius) / Mm);
        long dx = bx - ax, dz = bz - az, len2 = dx * dx + dz * dz, r2 = (long)radius * radius;
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * cols + ix;
                if (want[i] == 0 || paint[i] == tool) continue;
                long px = ix * Mm + Mm / 2 - ax, pz = iz * Mm + Mm / 2 - az;
                bool inside;
                if (len2 == 0) inside = px * px + pz * pz <= r2;
                else
                {
                    long along = px * dx + pz * dz;
                    if (along <= 0) inside = px * px + pz * pz <= r2;
                    else if (along >= len2) inside = (px - dx) * (px - dx) + (pz - dz) * (pz - dz) <= r2;
                    else
                    {
                        // the distance from the line, squared, times len2
                        long cross = px * dz - pz * dx;
                        inside = cross * cross <= r2 * len2;
                    }
                }
                if (!inside) continue;
                paint[i] = (byte)tool;
                chunks[iz / ChunkCells * chunksX + ix / ChunkCells].dirty = true;
                scoreDirty = true;
            }
    }

    readonly Msg strokes = new Msg(256);
    int pending;

    // any machine: a stroke to ask the host for, in metres. They go together, a few times a second.
    public void Stroke(int tool, Vector3 a, Vector3 b, float width)
    {
        if (pending == 0)
        {
            strokes.Reset(Op.Paint);
            strokes.U8((byte)plot.id);
            strokes.U8(0);
        }
        if (pending >= 40) return;
        pending++;
        strokes.U8((byte)tool);
        strokes.I32(Mathf.RoundToInt((a.x - plot.Origin.x) * 1000f)); strokes.I32(Mathf.RoundToInt((a.z - plot.Origin.z) * 1000f));
        strokes.I32(Mathf.RoundToInt((b.x - plot.Origin.x) * 1000f)); strokes.I32(Mathf.RoundToInt((b.z - plot.Origin.z) * 1000f));
        strokes.U16((ushort)Mathf.Clamp(Mathf.RoundToInt(width * 500f), 10, 2000));
    }

    // send what has been asked for since the last time
    public void Flush()
    {
        if (pending == 0) return;
        strokes.d[2] = (byte)pending;
        pending = 0;
        strokes.r = 2;
        if (Net.IsHost) OnStrokes(strokes, true);
        else Net.ToHost(strokes, true);
    }

    // host: strokes a player (or a paint truck) asked for: apply them and tell everyone. A
    // client: strokes the host has decided on.
    public void OnStrokes(Msg m, bool host)
    {
        int start = m.r, n = m.U8();
        for (int k = 0; k < n; k++)
        {
            int tool = m.U8(), ax = m.I32(), az = m.I32(), bx = m.I32(), bz = m.I32(), radius = m.U16();
            if (tool > Tar || radius > 2000) continue;
            Apply(tool, ax, az, bx, bz, radius);
        }
        if (!host) return;
        var copy = Msg.New(Op.Paint, m.n + 4);
        copy.U8((byte)plot.id);
        for (int k = start; k < m.r; k++) copy.U8(m.d[k]);
        Net.ToClients(copy, true);
    }

    // host: the whole strip, to someone who has just joined, as runs of the same cell
    public void SendState(ulong client)
    {
        const int RunsPerMessage = 6000;
        int i = 0;
        while (i < paint.Length)
        {
            var m = Msg.New(Op.PaintState, 1024);
            m.U8((byte)plot.id);
            m.U32((uint)i);
            int at = m.n, runs = 0;
            m.U16(0);
            while (i < paint.Length && runs < RunsPerMessage)
            {
                int run = 1;
                while (i + run < paint.Length && paint[i + run] == paint[i] && run < 65535) run++;
                m.U8(paint[i]);
                m.U16((ushort)run);
                i += run;
                runs++;
            }
            m.d[at] = (byte)runs;
            m.d[at + 1] = (byte)(runs >> 8);
            Net.Send(client, m, true);
        }
    }

    public void OnState(Msg m)
    {
        int i = (int)m.U32(), runs = m.U16();
        for (int k = 0; k < runs; k++)
        {
            byte value = m.U8();
            int run = m.U16();
            for (int j = 0; j < run && i < paint.Length; j++) paint[i++] = value;
        }
        foreach (var chunk in chunks) chunk.dirty = true;
        scoreDirty = true;
    }

    public uint Hash()
    {
        uint hash = 2166136261;
        if (paint == null) return hash;
        for (int i = 0; i < paint.Length; i++)
            if (paint[i] != 0) hash = (hash ^ (uint)(i * 4 + paint[i])) * 16777619;
        return hash;
    }

    // ---- the tools in the local player's hands

    Vector3 last;
    int lastTool = -1, heldFrame;
    float lastTime, nextSend;
    public static Vector3 Aim;      // where the local player's paint tool points, for the readout
    public static int AimFrame;

    // Called by the plot each frame the local player is holding the roller brush, the tar spray
    // or the grinder. Says what the crosshair is on.
    public void Hold(Tuning t, Mouse mouse, Transform eye)
    {
        ring.enabled = false;
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, t.clickReach, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != plot.transform) { lastTool = -1; return; }
        Vector3 aim = hit.point;
        Aim = aim;
        AimFrame = heldFrame = Time.frameCount;
        int cell = CellAt(aim.x, aim.z);
        bool asphalt = cell >= 0 && want[cell] != 0;
        int tool = -1;
        float width;
        if (Plot.Tool == Plot.Brush)
        {
            width = t.rollerWidth;
            if (mouse.leftButton.isPressed) tool = White; else if (mouse.rightButton.isPressed) tool = Yellow;
            Plot.Hint(asphalt ? "the roller brush: hold left click and drag for a white stripe, right click for yellow" : "the roller brush paints rolled asphalt");
        }
        else if (Plot.Tool == Plot.TarSpray)
        {
            width = t.tarWidth;
            if (mouse.leftButton.isPressed) tool = Tar;
            Plot.Hint("the tar spray: hold left click to cover paint. It is wide, and it wanders");
        }
        else
        {
            width = t.grinderWidth;
            if (mouse.leftButton.isPressed) tool = Grind;
            Plot.Hint("the grinder: hold left click and drag to take paint and tar off, exactly");
        }
        Ring(aim, width * 0.5f, Plot.Tool == Plot.Brush ? (Color)PaintWhite : Plot.Tool == Plot.TarSpray ? new Color(0.5f, 0.4f, 0.3f) : new Color(1f, 0.5f, 0.2f));
        if (tool < 0) { lastTool = -1; return; }
        if (Plot.Tool == Plot.TarSpray)
        {
            // a few blobs every so often, scattered over the width of the spray
            if (Time.time < nextSend) return;
            nextSend = Time.time + 0.05f;
            for (int k = 0; k < 3; k++)
            {
                Vector2 scatter = Random.insideUnitCircle * width * 0.5f;
                Vector3 blob = aim + new Vector3(scatter.x, 0, scatter.y);
                Stroke(Tar, blob, blob, Random.Range(0.25f, 0.5f) * width);
            }
            Flush();
            return;
        }
        // a stripe from where the tool was to where it is now, so that it is continuous however fast it is dragged
        bool carryOn = lastTool == tool && Time.time - lastTime < 0.25f && (aim - last).sqrMagnitude < 9f;
        if (carryOn && (aim - last).sqrMagnitude < 0.0004f) { lastTime = Time.time; return; }
        Stroke(tool, carryOn ? last : aim, aim, width);
        last = aim;
        lastTool = tool;
        lastTime = Time.time;
        if (Time.time >= nextSend) { nextSend = Time.time + 0.03f; Flush(); }
    }

    void Ring(Vector3 centre, float radius, Color color)
    {
        const int Points = 20;
        ring.enabled = true;
        ring.startColor = ring.endColor = color;
        ring.positionCount = Points;
        for (int k = 0; k < Points; k++)
        {
            float angle = k * Mathf.PI * 2f / Points;
            ring.SetPosition(k, centre + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius));
        }
    }

    int CellAt(float x, float z)
    {
        int ix = Mathf.FloorToInt((x - plot.Origin.x) / Cell), iz = Mathf.FloorToInt((z - plot.Origin.z) / Cell);
        return ix < 0 || iz < 0 || ix >= cols || iz >= rows ? -1 : iz * cols + ix;
    }

    public bool Covers(float x, float z) { int cell = CellAt(x, z); return cell >= 0 && want[cell] != 0; }

    // ---- judging

    // The score of the grid square at a spot, -100 to 100, and what it is made of: how much of
    // the length of line it should have has paint, and how much paint it has where none should be.
    public bool SquareScore(float x, float z, out float score, out float line, out float stray)
    {
        score = line = stray = 0;
        int cell = CellAt(x, z);
        if (cell < 0 || square[cell] < 0 || !squares.TryGetValue(square[cell], out var s)) return false;
        Score(s, out score, out line, out stray);
        return true;
    }

    // wanted and covered are lengths of line, in cells of length; astray is cells of paint out of place
    void Score(Vector3Int s, out float score, out float line, out float stray)
    {
        var t = Game.I.tuning;
        // paint out of place is counted in lines' worth: a stray stripe the length of the square costs as much as its own line earns
        float across = Mathf.Max(1f, t.rollerWidth / Cell), length = Mathf.Max(1f, t.patchWidth / Cell);
        line = s.x > 0 ? (float)s.y / s.x : 0;
        stray = s.z / (across * (s.x > 0 ? s.x : length));
        score = Mathf.Clamp(line - stray, -1f, 1f) * 100f;
        line *= 100f;
        stray *= 100f;
    }

    void Judge()
    {
        squares.Clear();
        covered.Clear();
        // which lengths of each line have paint of the right color somewhere across the band
        for (int i = 0; i < paint.Length; i++)
        {
            if (want[i] < 2) continue;
            long key = ((long)square[i] / 4 * 4 + lineOf[i]) * 100000L + slice[i];     // the row of squares, the line, and how far along
            bool right = paint[i] == (want[i] == 2 ? White : Yellow);
            if (!covered.TryGetValue(key, out bool had)) covered[key] = right;
            else if (right && !had) covered[key] = true;
        }
        // each square: the lengths it wants and has, and its paint out of place
        var counted = new HashSet<long>();
        for (int i = 0; i < paint.Length; i++)
        {
            if (square[i] < 0) continue;
            squares.TryGetValue(square[i], out var s);
            if (want[i] >= 2)
            {
                long key = ((long)square[i] / 4 * 4 + lineOf[i]) * 100000L + slice[i];
                // once for each length of line in this square
                if (counted.Add(key * 4 + square[i] % 4)) { s.x++; if (covered[key]) s.y++; }
                if (paint[i] == (want[i] == 2 ? Yellow : White)) s.z++;     // the wrong color
            }
            else if (paint[i] == White || paint[i] == Yellow) s.z++;
            squares[square[i]] = s;
        }
        var t = Game.I.tuning;
        float across = Mathf.Max(1f, t.rollerWidth / Cell);
        int wanted = 0, have = 0;
        float astray = 0;
        foreach (var s in squares.Values) { wanted += s.x; have += s.y; astray += s.z / across; }
        total = wanted > 0 ? Mathf.Clamp((have - astray) / wanted, -1f, 1f) * 100f : 0;
    }

    // ---- showing it

    readonly List<Vector3> verts = new List<Vector3>();
    readonly List<Color32> colors = new List<Color32>();
    readonly List<Vector3> normals = new List<Vector3>();
    readonly List<int> tris = new List<int>();

    void LateUpdate()
    {
        if (paint == null || plot == null || !plot.Ready) return;
        if (heldFrame < Time.frameCount - 1) ring.enabled = false;
        Wanted();
        if (scoreDirty && Time.unscaledTime >= nextScore)
        {
            scoreDirty = false;
            nextScore = Time.unscaledTime + 0.5f;
            Judge();
        }
        int built = 0;
        foreach (var chunk in chunks)
        {
            if (!chunk.dirty || built >= 6) continue;
            chunk.dirty = false;
            built++;
            verts.Clear(); colors.Clear(); normals.Clear(); tris.Clear();
            for (int iz = chunk.row0; iz < Mathf.Min(rows, chunk.row0 + ChunkCells); iz++)
                for (int ix = chunk.col0; ix < Mathf.Min(cols, chunk.col0 + ChunkCells); ix++)
                {
                    int i = iz * cols + ix;
                    Color32 color;
                    if (paint[i] == White) color = PaintWhite;
                    else if (paint[i] == Yellow) color = PaintYellow;
                    else if (paint[i] == Tar) color = TarBlack;
                    else if (marked && mid[i] != 0 && (ix + iz) % 3 != 0) color = Mark;     // a faint dotted line where each line should go
                    else continue;
                    float x = plot.Origin.x + ix * Cell, z = plot.Origin.z + iz * Cell, y = height[i] + (paint[i] == None ? 0.012f : 0.02f);
                    int v = verts.Count;
                    verts.Add(new Vector3(x, y, z)); verts.Add(new Vector3(x, y, z + Cell)); verts.Add(new Vector3(x + Cell, y, z + Cell)); verts.Add(new Vector3(x + Cell, y, z));
                    for (int k = 0; k < 4; k++) { colors.Add(color); normals.Add(Vector3.up); }
                    tris.Add(v); tris.Add(v + 1); tris.Add(v + 2); tris.Add(v); tris.Add(v + 2); tris.Add(v + 3);
                }
            chunk.mesh.Clear();
            chunk.mesh.SetVertices(verts);
            chunk.mesh.SetColors(colors);
            chunk.mesh.SetNormals(normals);
            chunk.mesh.SetTriangles(tris, 0);
        }
    }
}
