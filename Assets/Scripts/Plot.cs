using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A plot of ground that clicking can bring to a line. The line runs from stake to stake: two
// roped stakes make one section of road, as wide as the road with a shoulder outside each edge.
// A stake takes at most two ropes, so a road is a chain of sections, and where two meet at a
// stake they are cut along the line that halves the bend: a mitred corner, with no gap and no
// overlap. Along a section the line runs straight between the two stake heights; across the road
// it is level; over a shoulder it falls away from the road's edge. A click moves a patch of
// ground toward the line, never past it. Ground that no section covers cannot be changed.
//
// Each tool has one job. Stakes set the line. Grading brings ground to it. Gravel goes down on
// road that is on its line, a layer at a time, and once it is at full depth the same tool packs it.
//
//   0  station 2        stakes already set: a level section, a climb and a fall, on rough ground
//   1  station 3        a bare hillside where the players put the stakes down themselves
//   2  station 4        one section that is already level, for laying gravel on
//   3  station 5 good   a finished road for the trucks
//   4  station 5 bad    a rough one
//   5  wear             a finished road that the trucks wear out
//   6  hairpin          stakes laid round the tightest turn allowed, on rough ground
//   7  hairpin good     the same turn, finished
//   8  the map          one big piece of land with a town at each end: see "the map" below
//
// The first eight are the stations, and exist only while the host has the stations chosen. The
// ninth is the land of whichever map the host has chosen instead.
//
// The host decides every click and every stake and sends the results; clients never generate or
// change anything themselves.
public class Plot : MonoBehaviour
{
    public const float Cell = 0.25f;        // distance between ground points
    public const int PresetSections = 3;    // station 2: level, climbing, falling
    public const int Count = 9, Land = 8;
    public static readonly string[] Names = { "2", "3", "4", "5 good", "5 bad", "wear", "hairpin", "hairpin good", "map" };
    // what the host can choose. 0 is the stations; the rest are land between two towns.
    public static readonly string[] MapNames = { "Stations", "Short, 60 m", "Middle, a hill in the way", "Long, 300 m", "Climb, 16 m up" };
    const int ChunkCells = 32;              // cells along each side of one mesh
    const float Margin = 5f;                // ground round the edge of a plot, sloping down to the yard
    const float BaseHeight = 1.2f;          // the line of a level preset road, above the yard
    const float Level = 0.005f;             // closer to the line than this counts as level
    const int MaxStakes = 250;              // stakes and ropes are counted in a byte each
    const int Fine = 4;                     // the map: ground points to each metre of the heights the host sends
    const float TownEnd = 30f;              // the map: land behind each town
    const float PadRadius = 13f;            // the map: level ground round each town's stake
    const float EdgeMargin = 8f;            // the map: the land falls away to nothing over this, at its edges
    const int RowsPerMessage = 4000;        // ground points per message when the whole plot is sent

    public const int Grade = 0, Gravel = 1;     // these two are sent with a click
    public const int Stakes = 3;
    public static int Tool = Stakes;            // what the local player is holding

    public int id;
    public int w, d;                        // points per side
    public float[] h;
    public byte[] gravel, packed;           // per point: millimetres of gravel on it, and how far that is packed, 0 to 100
    byte[] health;                          // host: what the trucks have left of it, 0 to 100
    public uint clicks;                     // clicks the host has accepted since the ground was made
    public float roadShare, shoulderShare;  // how much of each is on its line, 0 to 1
    public float gravelShare, packedShare;  // how much of the road has its full gravel, and has it packed
    public readonly List<Yard.Label> labels = new List<Yard.Label>();
    public readonly List<Vector3> stakes = new List<Vector3>();    // y is the height of the line at the stake
    public readonly List<Vector2Int> links = new List<Vector2Int>();
    public int selected = -1;               // the local player's stake: the next one is roped to it

    public bool Wears => id == 5;           // trucks damage this road
    public bool IsLand => id == Land;
    public int fixedStakes;                 // the map: the first stakes are the towns', and stay where they are
    public bool joined;                     // the map: ropes run all the way from one town's stake to the other's
    public float roadLength;                // the map: metres of rope in the chain that starts at the first town
    ushort[] coarse;                        // the map: the land as the host made it, a height in millimetres every metre
    int cw, cd;
    byte[] edited;                          // host, the map: points a click has changed, which is all a joiner needs sending
    float remakeAt;                         // host, the map: make the land again once the sliders have stopped moving
    bool Finished => id == 3 || id == 5 || id == 7;

    Vector3 origin;                         // world position of point 0,0
    float lane, shoulderWidth, shoulderDrop;

    // A section, worked out from its two stakes and whatever it meets at each.
    struct Seg
    {
        public Vector3 a, d, r;             // first stake on the ground; along; to the right
        public float len, yA, yB;
        public float kA, kB;                // how far its two ends slant per metre to the right: the mitres
    }
    Seg[] segs = new Seg[0];

    // what the sections make of each point
    float[] target;                         // where the line puts it
    byte[] zone;                            // 0 not covered, 1 road, 2 shoulder
    short[] linkOf;                         // which section covers it
    float[] along, across;                  // where it is in that section: 0 to 1 from end to end, and metres right of the centre line
    string built = "";
    int expected;                           // client: ground points still to arrive

    class Chunk { public Mesh mesh; public MeshCollider collider; public Vector3[] verts; public Color32[] colors; public int col0, cols, row0, rows; public bool dirty; }
    Chunk[] chunks;
    int chunksX, chunksZ;
    bool tallyDirty;
    float nextTally;
    readonly List<int> chain = new List<int>();     // sections in the order a truck meets them: the index, or ~index if it is driven from its second stake to its first
    Transform stakeRoot;
    readonly Dictionary<int, int> stakeByCollider = new Dictionary<int, int>();
    LineRenderer cursor, preview, hotRing;
    Material wood, red, white;
    float nextClick;                                                    // local rate cap
    readonly float[] hostNextClick = new float[Session.MaxPlayers];     // host: rate cap per player
    readonly Msg edit = new Msg(8192);
    readonly List<int> changed = new List<int>();

    static readonly Color32 Outside = new Color32(104, 100, 92, 255);
    static readonly Color32 Grass = new Color32(112, 128, 88, 255);
    static readonly Color32 RoadRough = new Color32(146, 128, 100, 255);
    static readonly Color32 ShoulderRough = new Color32(128, 116, 96, 255);
    static readonly Color32 RoadDone = new Color32(206, 192, 150, 255);
    static readonly Color32 ShoulderDone = new Color32(178, 168, 136, 255);
    static readonly Color32 GravelLoose = new Color32(176, 176, 172, 255);
    static readonly Color32 GravelPacked = new Color32(92, 92, 98, 255);

    public bool Ready => h != null && chunks != null;
    float SizeX => (w - 1) * Cell;
    float SizeZ => (d - 1) * Cell;
    float Reach => lane + shoulderWidth;
    bool Inside(float x, float z, float inset) { return x >= origin.x + inset && x <= origin.x + SizeX - inset && z >= origin.z + inset && z <= origin.z + SizeZ - inset; }
    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }
    static int FullGravel => Mathf.Clamp(Mathf.RoundToInt(Game.I.tuning.gravelDepth * 1000f), 1, 255);

    // how far the player is from this plot, for choosing which one the readout is about
    public float Distance(Vector3 p)
    {
        if (!Ready) return float.MaxValue;
        float dx = Mathf.Max(origin.x - p.x, 0, p.x - origin.x - SizeX), dz = Mathf.Max(origin.z - p.z, 0, p.z - origin.z - SizeZ);
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        labels.Clear();
        stakes.Clear();
        links.Clear();
        stakeByCollider.Clear();
        segs = new Seg[0];
        h = null;
        chunks = null;
        coarse = null;
        edited = null;
        fixedStakes = 0;
        joined = false;
        roadLength = 0;
        remakeAt = 0;
        cursor = preview = hotRing = null;
        stakeRoot = null;
        built = "";
        clicks = 0;
        selected = -1;
        if (hotPlot == this) hotPlot = null;
        roadShare = shoulderShare = gravelShare = packedShare = 0;
    }

    string Signature(Tuning t)
    {
        if (IsLand) return Game.I.map + " " + Game.I.mapSeed + " " + t.laneWidth + " " + t.shoulderWidth + " " + t.shoulderDrop + " " + t.landRoughness + " " + (Game.I.map == 2 ? t.mapDistance : 0);
        return t.laneWidth + " " + t.sectionLength + " " + t.sectionRise + " " + t.shoulderWidth + " " + t.shoulderDrop + " " + t.hairpinAcross;
    }

    // host: the lines follow these sizes, so new ones mean new ground. The map is big, so it
    // waits until the slider has stopped moving.
    public void TuningChanged()
    {
        if (!Net.IsHost || !Ready || Signature(Game.I.tuning) == built) return;
        if (IsLand) remakeAt = Time.unscaledTime + 0.7f;
        else Generate();
    }

    // ---- host: make the ground

    public void Generate()
    {
        var t = Game.I.tuning;
        Clear();
        built = Signature(t);
        lane = t.laneWidth;
        shoulderWidth = t.shoulderWidth;
        shoulderDrop = t.shoulderDrop;
        float ox = Random.value * 100f, oz = Random.value * 100f;
        float length = t.sectionLength, rise = t.sectionRise, width = (Reach + Margin) * 2f;
        // the straight roads stand in a row to the left of the road strip, each further out
        float first = -lane - Margin - Reach - 4f;
        switch (id)
        {
            case 0: Straight(first, 0, length, BaseHeight, BaseHeight, BaseHeight + rise, BaseHeight); Lay(t, ox, oz, 1f); break;
            case 1: Hillside(t, ox, oz); break;
            case 2: Straight(first, -(length + Margin * 2f + 10f), length, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case 3: Straight(first - 25f, 0, length, BaseHeight, BaseHeight, BaseHeight + rise * 0.5f); Lay(t, ox, oz, 0); break;
            case 4: Straight(first - 50f, 0, length, BaseHeight, BaseHeight, BaseHeight + rise * 0.5f); Lay(t, ox, oz, 0.9f); break;
            case 5: Straight(first - 75f, 0, length, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case Land: MakeLand(t); break;
            default:
                {
                    // the hairpins sit behind the row, side by side
                    float radius = t.hairpinAcross * 0.5f + lane, half = radius + Reach + Margin;
                    float centre = first - width * 0.5f - half - 2f - (id - 6) * (half * 2f + 2f);
                    Hairpin(centre, -25f, radius);
                    Lay(t, ox, oz, id == 6 ? 0.8f : 0);
                    break;
                }
        }
        // whole millimetres, which is what clients are sent
        for (int i = 0; i < h.Length; i++) h[i] = Mathf.RoundToInt(h[i] * 1000f) / 1000f;
        gravel = new byte[h.Length];
        packed = new byte[h.Length];
        health = new byte[h.Length];
        if (IsLand) edited = new byte[h.Length];
        for (int i = 0; i < h.Length; i++) health[i] = 100;
        if (Finished)
        {
            // gravel at full depth, packed
            for (int i = 0; i < h.Length; i++)
            {
                if (!Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out _, out _, out float side) || Mathf.Abs(side) > lane) continue;
                gravel[i] = (byte)FullGravel;
                packed[i] = 100;
            }
        }
        Build();
        if (Net.IsHost) SendState(0, true);
    }

    static float Noise(float x, float z, float ox, float oz)
    {
        // humps and hollows a few metres across
        float n = Mathf.PerlinNoise(ox + x * 0.16f, oz + z * 0.16f) - 0.5f;
        n += (Mathf.PerlinNoise(oz + x * 0.45f, ox + z * 0.45f) - 0.5f) * 0.35f;
        return Mathf.Clamp(n * 2.6f, -1f, 1f);
    }

    // stakes in a straight row along z, a section apart, at these heights
    void Straight(float x, float z0, float length, params float[] height)
    {
        for (int i = 0; i < height.Length; i++) stakes.Add(new Vector3(x, height[i], z0 + i * length));
        for (int i = 0; i + 1 < height.Length; i++) links.Add(new Vector2Int(i, i + 1));
    }

    // Stakes round a U-turn whose centre line has this radius: down one straight leg, round half a
    // circle in five sections, and back up the other leg. The legs start at z0 and run toward -z.
    void Hairpin(float centreX, float z0, float radius)
    {
        const float Leg = 14f;
        const int Steps = 5;
        stakes.Add(new Vector3(centreX - radius, BaseHeight, z0));
        for (int k = 0; k <= Steps; k++)
        {
            float angle = Mathf.PI + Mathf.PI * k / Steps;
            stakes.Add(new Vector3(centreX + Mathf.Cos(angle) * radius, BaseHeight, z0 - Leg + Mathf.Sin(angle) * radius));
        }
        stakes.Add(new Vector3(centreX + radius, BaseHeight, z0));
        for (int i = 0; i + 1 < stakes.Count; i++) links.Add(new Vector2Int(i, i + 1));
    }

    // Ground for a road whose stakes are already in: the line plus humps and hollows on the road
    // and its shoulders (roughness 0 leaves it on the line), falling away to the flat yard outside.
    void Lay(Tuning t, float ox, float oz, float roughness)
    {
        Vector3 low = stakes[0], high = stakes[0];
        foreach (var s in stakes) { low = Vector3.Min(low, s); high = Vector3.Max(high, s); }
        float border = Reach + Margin;
        origin = new Vector3(low.x - border, 0, low.z - border);
        w = Mathf.CeilToInt((high.x - low.x + border * 2f) / Cell) + 1;
        d = Mathf.CeilToInt((high.z - low.z + border * 2f) / Cell) + 1;
        h = new float[w * d];
        Rebuild();
        for (int i = 0; i < h.Length; i++)
        {
            float x = origin.x + i % w * Cell, z = origin.z + i / w * Cell;
            float line, outside = 0;
            if (Section(x, z, out int link, out float along_, out float side)) line = TargetIn(link, along_, side);
            else
            {
                // the nearest section, carried on past its edge
                outside = float.MaxValue;
                line = 0;
                foreach (var seg in segs)
                {
                    float px = x - seg.a.x, pz = z - seg.a.z;
                    float s = px * seg.d.x + pz * seg.d.z, off = px * seg.d.z - pz * seg.d.x;
                    float beyond = Mathf.Max(Mathf.Abs(off) - Reach, Mathf.Max(-s, s - seg.len));
                    if (beyond >= outside) continue;
                    outside = beyond;
                    line = Mathf.Lerp(seg.yA, seg.yB, Mathf.Clamp01(s / seg.len)) - shoulderDrop;
                }
                outside = Mathf.Max(0, outside);
            }
            float rough = line + Noise(x, z, ox, oz) * t.roughHeight * roughness;
            h[i] = Mathf.Max(0, rough * Mathf.SmoothStep(1, 0, Mathf.Clamp01(outside / Margin)));
        }
    }

    // Station 3: a bare hillside with no line. It rises away from where players start and tilts
    // a little to one side.
    void Hillside(Tuning t, float ox, float oz)
    {
        const float Width = 44f, Depth = 54f;
        origin = new Vector3(t.laneWidth + 12f, 0, -8f);
        w = Mathf.CeilToInt(Width / Cell) + 1;
        d = Mathf.CeilToInt(Depth / Cell) + 1;
        h = new float[w * d];
        for (int i = 0; i < h.Length; i++)
        {
            float u = i % w * Cell, v = i / w * Cell;
            float hill = 0.6f + v * 0.11f + u * 0.03f + Noise(origin.x + u, origin.z + v, ox, oz) * t.roughHeight * 0.6f;
            float edge = Mathf.Min(Mathf.Min(u, Width - u), Mathf.Min(v, Depth - v));
            h[i] = Mathf.Max(0, hill * Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / Margin)));
        }
    }

    // ---- the map
    //
    // A map is too big to send a height every 0.25 m (300 m of it is over half a million points).
    // So the host makes the land as a height every metre, in whole millimetres, and sends that:
    // 30 to 70 kB. Every machine fills in the points between with the same whole-number sums, so
    // they all hold exactly the same ground without any of them having to trust another's
    // arithmetic. After that only the points a click has changed are sent.

    // host: the land of the chosen map. The towns stand at x 0, the first at z 0 and the second
    // `length` further on, each with a fixed stake on a level pad.
    void MakeLand(Tuning t)
    {
        int map = Game.I.map;
        float length = Mathf.Round(map == 1 ? 60f : map == 2 ? t.mapDistance : map == 3 ? 300f : 160f);
        float width = map == 1 ? 70f : map == 4 ? 130f : 100f;
        // the same seed makes the same land, so a map is the same every time until it is made again
        var random = new System.Random(Game.I.mapSeed);
        float ox = (float)random.NextDouble() * 100f, oz = (float)random.NextDouble() * 100f;
        origin = new Vector3(-width * 0.5f, 0, -TownEnd);
        cw = Mathf.RoundToInt(width) + 1;
        cd = Mathf.RoundToInt(length + TownEnd * 2f) + 1;
        coarse = new ushort[cw * cd];
        float padA = Shape(map, 0, 0, length, ox, oz), padB = Shape(map, 0, length, length, ox, oz);
        for (int i = 0; i < coarse.Length; i++)
        {
            float u = i % cw, v = i / cw, x = origin.x + u, z = origin.z + v;
            float y = Shape(map, x, z, length, ox, oz) + Noise(x, z, ox, oz) * t.landRoughness;
            // level round each town's stake
            y = Mathf.Lerp(padA, y, Mathf.SmoothStep(0, 1, (Mathf.Sqrt(x * x + z * z) - PadRadius) / 10f));
            y = Mathf.Lerp(padB, y, Mathf.SmoothStep(0, 1, (Mathf.Sqrt(x * x + (z - length) * (z - length)) - PadRadius) / 10f));
            float edge = Mathf.Min(Mathf.Min(u, cw - 1 - u), Mathf.Min(v, cd - 1 - v));
            y *= Mathf.SmoothStep(0, 1, edge / EdgeMargin);
            coarse[i] = (ushort)Mathf.Clamp(Mathf.RoundToInt(y * 1000f), 0, 65535);
        }
        Expand();
        int centre = cw / 2, rowA = Mathf.RoundToInt(TownEnd);
        stakes.Add(new Vector3(0, coarse[rowA * cw + centre] * 0.001f, 0));
        stakes.Add(new Vector3(0, coarse[(rowA + Mathf.RoundToInt(length)) * cw + centre] * 0.001f, length));
        fixedStakes = 2;
    }

    // The shape of each map's land, before its humps and hollows: metres up at x, z, with the
    // towns `length` apart.
    static float Shape(int map, float x, float z, float length, float ox, float oz)
    {
        // a swell 50 m or so from crest to crest, between -1 and 1
        float swell = (Mathf.PerlinNoise(ox + 31f + x * 0.021f, oz + 17f + z * 0.021f) - 0.5f) * 2f;
        float mid = length * 0.5f;
        switch (map)
        {
            case 1:
                // short: nearly flat
                return 3f + swell * 0.8f;
            case 2:
                {
                    // middle: a hill on the straight line between the towns, and a hollow on one
                    // side of it, so the easy way is round the other side
                    float sigma = Mathf.Max(12f, length * 0.1f);
                    float hill = 9f * Mathf.Exp(-(x * x + (z - mid) * (z - mid)) / (2f * sigma * sigma));
                    float hollow = -3f * Mathf.Exp(-((x - 30f) * (x - 30f) + (z - mid) * (z - mid)) / 200f);
                    return 4f + swell + hill + hollow;
                }
            case 3:
                {
                    // long: rolling land, a ridge right across it with one gap, and a hollow further on
                    float zr = length * 0.38f, zh = length * 0.7f;
                    float ridge = 6f * Mathf.Exp(-(z - zr) * (z - zr) / 162f) * (1f - 0.8f * Mathf.Exp(-(x - 28f) * (x - 28f) / 288f));
                    float hollow = -3.5f * Mathf.Exp(-((x + 5f) * (x + 5f) + (z - zh) * (z - zh)) / 512f);
                    return 4f + swell * 1.5f + ridge + hollow;
                }
            default:
                {
                    // climb: the second town is 16 m higher. The rise is spread over 150 m at the
                    // left edge and gets shorter and steeper to the right: about 31 degrees on the
                    // straight line between the towns, and a cliff at the right edge.
                    float across = Mathf.Max(16f, 150f * Mathf.Pow(40f / 150f, (x + 55f) / 55f));
                    return 3f + 16f * Mathf.SmoothStep(0, 1, (z - (mid - across * 0.5f)) / across) + swell * 0.6f;
                }
        }
    }

    // every machine: the ground points from the heights a metre apart, by whole-number sums
    void Expand()
    {
        w = (cw - 1) * Fine + 1;
        d = (cd - 1) * Fine + 1;
        h = new float[w * d];
        for (int iz = 0; iz < d; iz++)
        {
            int cz = Mathf.Min(iz / Fine, cd - 2), fz = iz - cz * Fine;
            for (int ix = 0; ix < w; ix++)
            {
                int cx = Mathf.Min(ix / Fine, cw - 2), fx = ix - cx * Fine, c = cz * cw + cx;
                int mm = (coarse[c] * (Fine - fx) * (Fine - fz) + coarse[c + 1] * fx * (Fine - fz) + coarse[c + cw] * (Fine - fx) * fz + coarse[c + cw + 1] * fx * fz + Fine * Fine / 2) / (Fine * Fine);
                h[iz * w + ix] = mm / 1000f;
            }
        }
    }

    // where players start on the map: beside the first town's stake, looking at the other town
    public Vector3 SpawnAt(int slot)
    {
        if (!Ready || stakes.Count == 0) return new Vector3(0, 5f, 0);
        Vector3 at = stakes[0] + new Vector3(3f + slot * 1.3f, 0, -3f);
        return new Vector3(at.x, HeightAt(at.x, at.z) + 0.2f, at.z);
    }

    // A town: a painted pad behind its stake, a few blocks round the back, and a tall pole to
    // find it by from the other end. `away` is +1 or -1: which way along z is away from the road.
    void Town(Transform root, Vector3 stake, float away, Color color)
    {
        var dark = Mats.Make(new Color(0.34f, 0.34f, 0.36f));
        var wall = Mats.Make(color);
        var roof = Mats.Make(color * 0.55f);
        Mats.Part(root, Mats.Cube, dark, stake + new Vector3(0, 0.02f, away * 7f), new Vector3(16f, 0.04f, 10f));
        Mats.Part(root, Mats.Cube, wall, stake + new Vector3(0, 13f, away * 12.5f), new Vector3(0.35f, 26f, 0.35f));
        // x, how far behind the stake, width, height, depth
        float[] houses = { -12f, 6f, 6f, 4f, 6f,   12f, 7f, 5f, 5.5f, 7f,   -9f, 17f, 7f, 3.5f, 5f,   9f, 18f, 6f, 6.5f, 6f,   0f, 21f, 5f, 4.5f, 4f };
        for (int k = 0; k < houses.Length; k += 5)
        {
            float x = stake.x + houses[k], z = stake.z + away * houses[k + 1], width = houses[k + 2], height = houses[k + 3], depth = houses[k + 4];
            float foot = HeightAt(x, z) - 0.6f;
            var box = Mats.Part(root, Mats.Cube, wall, new Vector3(x, foot + (height + 0.6f) * 0.5f, z), new Vector3(width, height + 0.6f, depth));
            box.gameObject.AddComponent<BoxCollider>();
            Mats.Part(root, Mats.Cube, roof, new Vector3(x, foot + height + 0.6f + 0.25f, z), new Vector3(width + 0.8f, 0.5f, depth + 0.8f));
        }
    }

    // whether the ropes join the towns, and how much road is staked out from the first
    void Survey()
    {
        joined = false;
        roadLength = 0;
        if (!IsLand || stakes.Count < 2) return;
        joined = Chain(0, 1);
        foreach (int c in chain) roadLength += segs[c < 0 ? ~c : c].len;
    }

    // a spot on some section of road, for the test tooling: a, b and c are each 0 to 1
    public Vector3 RoadSpot(float a, float b, float c)
    {
        if (!Ready || segs.Length == 0) return Spot(b, c);
        Vector3 p = World(Mathf.Min((int)(a * segs.Length), segs.Length - 1), b, (c * 2f - 1f) * Reach * 0.97f);
        return new Vector3(p.x, HeightAt(p.x, p.z), p.z);
    }

    // host: the map's heights a metre apart, then the points that clicks have changed since
    void SendLand(ulong client, bool everyone)
    {
        const int PerMessage = 8000;
        for (int start = 0; start < coarse.Length; start += PerMessage)
        {
            int n = Mathf.Min(PerMessage, coarse.Length - start);
            var m = Msg.New(Op.PlotCoarse, n * 2 + 16);
            m.U8((byte)id);
            m.U32((uint)start);
            m.U16((ushort)n);
            for (int i = start; i < start + n; i++) m.U16(coarse[i]);
            Out(client, everyone, m);
        }
        var points = new List<int>();
        for (int i = 0; i < edited.Length; i++) if (edited[i] != 0) points.Add(i);
        const int PointsPerMessage = 2000;
        for (int start = 0; start < points.Count; start += PointsPerMessage)
        {
            int n = Mathf.Min(PointsPerMessage, points.Count - start);
            var m = Msg.New(Op.PlotPoints, n * 10 + 16);
            m.U8((byte)id);
            m.U16((ushort)n);
            for (int k = start; k < start + n; k++)
            {
                int i = points[k];
                m.U32((uint)i);
                m.F32(h[i]);
                m.U8(gravel[i]);
                m.U8(packed[i]);
            }
            Out(client, everyone, m);
        }
    }

    public void OnCoarse(Msg m)
    {
        if (coarse == null) return;
        int start = (int)m.U32(), n = m.U16();
        for (int i = start; i < start + n; i++) coarse[i] = m.U16();
        expected -= n;
        if (expected <= 0) Expand();
    }

    // points that were changed before this machine joined
    public void OnPoints(Msg m)
    {
        if (h == null) return;
        int n = m.U16();
        for (int k = 0; k < n; k++)
        {
            int i = (int)m.U32();
            h[i] = m.F32();
            gravel[i] = m.U8();
            packed[i] = m.U8();
        }
    }

    // ---- sending the whole plot

    // host: to one client (someone joined) or to all. Heights go in millimetres, two bytes each,
    // a few thousand points to a message. The stakes go last, and the plot is built when they land.
    public void SendState(ulong client, bool everyone = false)
    {
        if (!Ready) return;
        var m = Msg.New(Op.PlotState, 96);
        m.U8((byte)id);
        m.U16((ushort)w); m.U16((ushort)d);
        m.V3(origin);
        m.F32(lane); m.F32(shoulderWidth); m.F32(shoulderDrop);
        m.U32(clicks);
        m.U8((byte)(IsLand ? 1 : 0));
        if (IsLand) { m.U16((ushort)cw); m.U16((ushort)cd); m.U8((byte)fixedStakes); }
        Out(client, everyone, m);
        if (IsLand) SendLand(client, everyone);
        else for (int start = 0; start < h.Length; start += RowsPerMessage)
        {
            int n = Mathf.Min(RowsPerMessage, h.Length - start);
            m = Msg.New(Op.PlotRows, n * 4 + 16);
            m.U8((byte)id);
            m.U32((uint)start);
            m.U16((ushort)n);
            for (int i = start; i < start + n; i++)
            {
                m.U16((ushort)Mathf.Clamp(Mathf.RoundToInt(h[i] * 1000f), 0, 65535));
                m.U8(gravel[i]);
                m.U8(packed[i]);
            }
            Out(client, everyone, m);
        }
        Out(client, everyone, StakesMsg(255, -1));
    }

    static void Out(ulong client, bool everyone, Msg m)
    {
        if (everyone) Net.ToClients(m, true); else Net.Send(client, m, true);
    }

    public void OnState(Msg m)
    {
        Clear();
        w = m.U16(); d = m.U16();
        origin = m.V3();
        lane = m.F32(); shoulderWidth = m.F32(); shoulderDrop = m.F32();
        clicks = m.U32();
        h = new float[w * d];
        gravel = new byte[h.Length];
        packed = new byte[h.Length];
        health = new byte[h.Length];
        expected = h.Length;
        built = "from host";
        if (m.U8() == 0) return;
        // the map: its heights come a metre apart, and the ground is filled in from them here
        cw = m.U16(); cd = m.U16();
        fixedStakes = m.U8();
        coarse = new ushort[cw * cd];
        expected = coarse.Length;
        h = null;
    }

    public void OnRows(Msg m)
    {
        if (h == null) return;
        int start = (int)m.U32(), n = m.U16();
        for (int i = start; i < start + n; i++) { h[i] = m.U16() / 1000f; gravel[i] = m.U8(); packed[i] = m.U8(); }
        expected -= n;
    }

    // ---- meshes

    void Build()
    {
        target = new float[w * d];
        zone = new byte[w * d];
        linkOf = new short[w * d];
        along = new float[w * d];
        across = new float[w * d];
        Rebuild();
        foreach (var seg in segs) Resolve(seg);

        // square meshes, so an edit rebuilds only a few metres of ground however big the plot is
        var material = Mats.Make(Color.white, true);
        chunksX = (w - 1 + ChunkCells - 1) / ChunkCells;
        chunksZ = (d - 1 + ChunkCells - 1) / ChunkCells;
        chunks = new Chunk[chunksX * chunksZ];
        for (int c = 0; c < chunks.Length; c++)
        {
            var chunk = chunks[c] = new Chunk { col0 = c % chunksX * ChunkCells, row0 = c / chunksX * ChunkCells };
            chunk.cols = Mathf.Min(ChunkCells, w - 1 - chunk.col0);
            chunk.rows = Mathf.Min(ChunkCells, d - 1 - chunk.row0);
            int stride = chunk.cols + 1;
            chunk.verts = new Vector3[stride * (chunk.rows + 1)];
            chunk.colors = new Color32[chunk.verts.Length];
            var tris = new int[chunk.cols * chunk.rows * 6];
            int k = 0;
            for (int z = 0; z < chunk.rows; z++)
                for (int x = 0; x < chunk.cols; x++)
                {
                    int i = z * stride + x;
                    tris[k++] = i; tris[k++] = i + stride; tris[k++] = i + stride + 1;
                    tris[k++] = i; tris[k++] = i + stride + 1; tris[k++] = i + 1;
                }
            var go = new GameObject("Ground");
            go.transform.SetParent(transform, false);
            chunk.mesh = new Mesh();
            chunk.mesh.MarkDynamic();
            Fill(chunk);
            chunk.mesh.vertices = chunk.verts;
            chunk.mesh.colors32 = chunk.colors;
            chunk.mesh.triangles = tris;
            chunk.mesh.RecalculateNormals();
            chunk.mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = chunk.mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            chunk.collider = go.AddComponent<MeshCollider>();
            chunk.collider.sharedMesh = chunk.mesh;
        }
        Tally();
        Survey();

        wood = Mats.Make(new Color(0.80f, 0.62f, 0.30f));
        red = Mats.Make(new Color(0.90f, 0.15f, 0.12f));
        white = Mats.Make(Color.white);
        stakeRoot = new GameObject("Stakes").transform;
        stakeRoot.SetParent(transform, false);
        cursor = Line("Cursor", true, 0.05f);
        preview = Line("Preview", false, 0.07f);
        hotRing = Line("Hot spot", true, 0.07f);
        hotRing.startColor = hotRing.endColor = new Color(1f, 0.85f, 0.1f);
        BuildStakes();

        string text;
        Vector3 at = stakes.Count > 0 ? stakes[0] + Vector3.up * 1.6f : Vector3.zero;
        switch (id)
        {
            case 0: text = "Station 2: a level section, a climb and a fall\n2 then hold left click on the ground to bring it to the string"; break;
            case 1:
                text = "Station 3: 1 then left click the ground to put a stake down; the next is roped to it\nleft click a stake to choose it; the wheel moves its rope, X pulls it out, right click lets go";
                at = new Vector3(origin.x + SizeX * 0.5f, HeightAt(origin.x + SizeX * 0.5f, origin.z + Margin) + 2f, origin.z + Margin);
                break;
            case 2: text = "Station 4: a section that is already level\n3 then hold left click to lay gravel, and keep going to pack it"; at = stakes[1] + Vector3.up * 1.6f; break;
            case 3: text = "Station 5: a finished road. Trucks drive it both ways."; break;
            case 4: text = "Station 5: a bad road. Trucks try it both ways."; break;
            case 5: text = "Wear: a finished road that the trucks wear out"; break;
            case 6: text = "Hairpin: stakes set round the tightest turn allowed\nlevel it and gravel it; trucks try it as it is"; break;
            case Land:
                {
                    text = "Town A. Press 1 and left click this stake, then click the ground toward the pole at the other town.\nEach stake is roped to the last. Rope the last one to town B's stake and the trucks set off.";
                    at = stakes[0] + Vector3.up * 2.2f;
                    labels.Add(new Yard.Label { at = stakes[1] + Vector3.up * 2.2f, text = "Town B. Rope the road to this stake." });
                    var towns = new GameObject("Towns").transform;
                    towns.SetParent(transform, false);
                    Town(towns, stakes[0], -1f, new Color(0.85f, 0.35f, 0.3f));
                    Town(towns, stakes[1], 1f, new Color(0.3f, 0.5f, 0.85f));
                    // the plain the land stands on, so nobody falls for ever off its edge
                    var plain = Mats.Part(towns, Mats.Cube, Mats.Make(new Color(0.33f, 0.37f, 0.31f)), new Vector3(origin.x + SizeX * 0.5f, -0.5f, origin.z + SizeZ * 0.5f), new Vector3(2000f, 1f, 2000f));
                    plain.gameObject.AddComponent<BoxCollider>();
                    Game.I.respawn = true;      // everyone starts again at the first town
                    break;
                }
            default: text = "Hairpin: the same turn, finished"; break;
        }
        labels.Add(new Yard.Label { at = at, text = text });
    }

    LineRenderer Line(string name, bool loop, float width)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = Mats.Unlit;
        line.widthMultiplier = width;
        line.loop = loop;
        line.useWorldSpace = true;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.enabled = false;
        return line;
    }

    // the top of whatever is at a point: the ground, plus its gravel, which packs down by a quarter
    float Surface(int i) { return h[i] + gravel[i] * 0.001f * (1f - 0.25f * packed[i] * 0.01f); }

    void Fill(Chunk chunk)
    {
        float full = FullGravel;
        int stride = chunk.cols + 1;
        for (int k = 0; k < chunk.verts.Length; k++)
        {
            int i = (chunk.row0 + k / stride) * w + chunk.col0 + k % stride;
            chunk.verts[k] = new Vector3(i % w * Cell, Surface(i), i / w * Cell) + origin;
            bool level = zone[i] != 0 && Mathf.Abs(h[i] - target[i]) < Level;
            Color32 color = zone[i] == 0 ? (IsLand ? Grass : Outside) : zone[i] == 1 ? (level ? RoadDone : RoadRough) : (level ? ShoulderDone : ShoulderRough);
            // gravel greys the ground as it deepens, and darkens as it is packed
            if (gravel[i] > 0) color = Color32.Lerp(color, Color32.Lerp(GravelLoose, GravelPacked, packed[i] * 0.01f), Mathf.Clamp01(gravel[i] / full));
            chunk.colors[k] = color;
        }
    }

    void Touch(int i)
    {
        int ix = i % w, iz = i / w;
        int cx = Mathf.Min(ix / ChunkCells, chunksX - 1), cz = Mathf.Min(iz / ChunkCells, chunksZ - 1);
        chunks[cz * chunksX + cx].dirty = true;
        // a point on the seam between meshes belongs to each of them
        bool left = ix % ChunkCells == 0 && cx > 0, below = iz % ChunkCells == 0 && cz > 0;
        if (left) chunks[cz * chunksX + cx - 1].dirty = true;
        if (below) chunks[(cz - 1) * chunksX + cx].dirty = true;
        if (left && below) chunks[(cz - 1) * chunksX + cx - 1].dirty = true;
    }

    // every mesh with a point in this box of points
    void TouchBox(int x0, int x1, int z0, int z1)
    {
        int cx0 = Mathf.Max(0, (x0 - 1) / ChunkCells), cx1 = Mathf.Min(chunksX - 1, x1 / ChunkCells);
        int cz0 = Mathf.Max(0, (z0 - 1) / ChunkCells), cz1 = Mathf.Min(chunksZ - 1, z1 / ChunkCells);
        for (int cz = cz0; cz <= cz1; cz++)
            for (int cx = cx0; cx <= cx1; cx++) chunks[cz * chunksX + cx].dirty = true;
    }

    void Upload(bool heights)
    {
        foreach (var chunk in chunks)
        {
            if (!chunk.dirty) continue;
            chunk.dirty = false;
            Fill(chunk);
            chunk.mesh.colors32 = chunk.colors;
            if (!heights) continue;
            chunk.mesh.vertices = chunk.verts;
            chunk.mesh.RecalculateNormals();
            chunk.mesh.RecalculateBounds();
            chunk.collider.sharedMesh = null;
            chunk.collider.sharedMesh = chunk.mesh;
        }
        tallyDirty = true;      // counted in Update, a few times a second at most
    }

    void Tally()
    {
        int road = 0, roadLevel = 0, shoulder = 0, shoulderLevel = 0, gravelled = 0, done = 0, full = FullGravel;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] == 0) continue;
            bool level = Mathf.Abs(h[i] - target[i]) < Level;
            if (zone[i] == 1 && gravel[i] >= full) { gravelled++; if (packed[i] >= 100) done++; }
            if (zone[i] == 1) { road++; if (level) roadLevel++; }
            else { shoulder++; if (level) shoulderLevel++; }
        }
        roadShare = road > 0 ? (float)roadLevel / road : 0;
        shoulderShare = shoulder > 0 ? (float)shoulderLevel / shoulder : 0;
        gravelShare = road > 0 ? (float)gravelled / road : 0;
        packedShare = road > 0 ? (float)done / road : 0;
    }

    public float HeightAt(float x, float z)
    {
        float fx = Mathf.Clamp((x - origin.x) / Cell, 0, w - 1.001f), fz = Mathf.Clamp((z - origin.z) / Cell, 0, d - 1.001f);
        int ix = (int)fx, iz = (int)fz;
        fx -= ix; fz -= iz;
        int i = iz * w + ix;
        return Mathf.Lerp(Mathf.Lerp(Surface(i), Surface(i + 1), fx), Mathf.Lerp(Surface(i + w), Surface(i + w + 1), fx), fz);
    }

    public uint Hash()
    {
        uint hash = 2166136261;
        if (!Ready) return hash;
        for (int i = 0; i < h.Length; i++) hash = (hash ^ (uint)(Mathf.RoundToInt(h[i] * 1000f) + gravel[i] * 100000 + packed[i] * 30000000)) * 16777619;
        foreach (var s in stakes) hash = (hash ^ (uint)Mathf.RoundToInt((s.x + s.y * 7f + s.z * 13f) * 1000f)) * 16777619;
        foreach (var l in links) hash = (hash ^ (uint)(l.x * 100 + l.y)) * 16777619;
        return hash;
    }

    // a spot on the plot, clear of its edge: u across, v along, both 0 to 1
    public Vector3 Spot(float u, float v)
    {
        float x = origin.x + Margin + u * (SizeX - Margin * 2f), z = origin.z + Margin + v * (SizeZ - Margin * 2f);
        return new Vector3(x, Ready ? HeightAt(x, z) : 0, z);
    }

    // ---- sections

    int LinksAt(int stake)
    {
        int n = 0;
        foreach (var l in links) if (l.x == stake || l.y == stake) n++;
        return n;
    }

    // the stake at the far end of another rope tied to `stake`, or -1
    int Beyond(int stake, int notLink)
    {
        for (int k = 0; k < links.Count; k++)
        {
            if (k == notLink) continue;
            if (links[k].x == stake) return links[k].y;
            if (links[k].y == stake) return links[k].x;
        }
        return -1;
    }

    // Work the sections out again from the stakes. Where a section meets another at a stake, both
    // are cut along the line that halves the bend between them.
    void Rebuild()
    {
        segs = new Seg[links.Count];
        for (int k = 0; k < links.Count; k++)
        {
            Vector3 a = stakes[links[k].x], b = stakes[links[k].y];
            var seg = new Seg { a = Flat(a), yA = a.y, yB = b.y };
            seg.d = Flat(b - a);
            seg.len = seg.d.magnitude;
            seg.d /= Mathf.Max(seg.len, 0.001f);
            seg.r = new Vector3(seg.d.z, 0, -seg.d.x);
            int before = Beyond(links[k].x, k), after = Beyond(links[k].y, k);
            if (before >= 0) seg.kA = Slant(seg, Flat(a - stakes[before]).normalized + seg.d);
            if (after >= 0) seg.kB = Slant(seg, seg.d + Flat(stakes[after] - b).normalized);
            segs[k] = seg;
        }
    }

    // the slant of a cut whose line is square to `through`
    static float Slant(Seg seg, Vector3 through)
    {
        float forward = Vector3.Dot(seg.d, through);
        return forward < 0.2f ? 0 : -Vector3.Dot(seg.r, through) / forward;     // a bend sharper than about 160 degrees is left square
    }

    // Which section covers a spot, and where in it: t from 0 at its first end to 1 at its second,
    // side in metres right of the centre line. Where two overlap, the nearer centre line wins.
    bool Section(float x, float z, out int link, out float t, out float side)
    {
        link = -1; t = side = 0;
        float best = float.MaxValue, reach = Reach;
        for (int k = 0; k < segs.Length; k++)
        {
            var seg = segs[k];
            float px = x - seg.a.x, pz = z - seg.a.z;
            float s = px * seg.d.x + pz * seg.d.z, off = px * seg.d.z - pz * seg.d.x;
            if (Mathf.Abs(off) > reach + 0.001f || Mathf.Abs(off) >= best) continue;
            float s0 = off * seg.kA, s1 = seg.len + off * seg.kB;
            if (s1 - s0 < 0.05f || s < s0 - 0.001f || s > s1 + 0.001f) continue;
            best = Mathf.Abs(off);
            link = k; side = off;
            t = Mathf.Clamp01((s - s0) / (s1 - s0));
        }
        return link >= 0;
    }

    // a place in a section, on the ground
    Vector3 World(int link, float t, float side)
    {
        var seg = segs[link];
        float s0 = side * seg.kA, s1 = seg.len + side * seg.kB;
        return seg.a + seg.d * (s0 + t * (s1 - s0)) + seg.r * side;
    }

    // level across the road; over a shoulder it falls away from the road's edge
    float TargetIn(int link, float t, float side)
    {
        float over = Mathf.Abs(side) - lane;
        return Mathf.Lerp(segs[link].yA, segs[link].yB, t) - (over > 0 && shoulderWidth > 0 ? shoulderDrop * over / shoulderWidth : 0);
    }

    // Work out again what the sections make of the points a section lies over, or lay over
    // before it changed. Only those points are visited, so the cost follows the section and not
    // the size of the plot.
    void Resolve(Seg seg)
    {
        // the box round its four corners, mitres and all
        float x0 = float.MaxValue, x1 = float.MinValue, z0 = float.MaxValue, z1 = float.MinValue;
        for (int corner = 0; corner < 4; corner++)
        {
            float edge = corner < 2 ? -Reach : Reach;
            Vector3 p = seg.a + seg.d * (corner % 2 == 0 ? edge * seg.kA : seg.len + edge * seg.kB) + seg.r * edge;
            x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); z0 = Mathf.Min(z0, p.z); z1 = Mathf.Max(z1, p.z);
        }
        int ix0 = Mathf.Max(0, Mathf.FloorToInt((x0 - origin.x) / Cell) - 2), ix1 = Mathf.Min(w - 1, Mathf.CeilToInt((x1 - origin.x) / Cell) + 2);
        int iz0 = Mathf.Max(0, Mathf.FloorToInt((z0 - origin.z) / Cell) - 2), iz1 = Mathf.Min(d - 1, Mathf.CeilToInt((z1 - origin.z) / Cell) + 2);
        for (int iz = iz0; iz <= iz1; iz++)
            for (int ix = ix0; ix <= ix1; ix++)
            {
                int i = iz * w + ix;
                zone[i] = 0;
                if (!Section(origin.x + ix * Cell, origin.z + iz * Cell, out int link, out float t, out float side)) continue;
                linkOf[i] = (short)link;
                along[i] = t;
                across[i] = side;
                zone[i] = (byte)(Mathf.Abs(side) <= lane ? 1 : 2);
                target[i] = TargetIn(link, t, side);
            }
        if (chunks != null && ix1 >= ix0 && iz1 >= iz0) TouchBox(ix0, ix1, iz0, iz1);
    }

    static bool Same(Seg a, Seg b)
    {
        return a.a == b.a && a.d == b.d && a.len == b.len && a.yA == b.yA && a.yB == b.yB && a.kA == b.kA && a.kB == b.kB;
    }

    // ---- stakes

    void BuildStakes()
    {
        for (int i = stakeRoot.childCount - 1; i >= 0; i--) Destroy(stakeRoot.GetChild(i).gameObject);
        stakeByCollider.Clear();
        for (int i = 0; i < stakes.Count; i++)
        {
            Vector3 s = stakes[i];
            // the string is tied 0.8 m below the top, at the height the ground is to reach
            // a town's stake is twice as thick
            float thick = i < fixedStakes ? 0.16f : 0.08f;
            var post = Mats.Part(stakeRoot, Mats.Cube, i == selected ? white : wood, s + Vector3.down * 0.6f, new Vector3(thick, 2.8f, thick));
            var grab = post.gameObject.AddComponent<BoxCollider>();
            grab.isTrigger = true;                      // walked through, but the crosshair finds it
            grab.size = new Vector3(0.4f / thick, 1f, 0.4f / thick);    // 0.4 m across
            stakeByCollider[grab.GetInstanceID()] = i;
        }
        foreach (var l in links)
        {
            Vector3 a = stakes[l.x], b = stakes[l.y];
            var line = Mats.Part(stakeRoot, Mats.Cube, red, (a + b) * 0.5f, new Vector3(0.03f, 0.03f, (b - a).magnitude));
            line.localRotation = Quaternion.LookRotation(b - a);
        }
    }

    bool Linked(int a, int b)
    {
        foreach (var l in links) if ((l.x == a && l.y == b) || (l.x == b && l.y == a)) return true;
        return false;
    }

    // The rules for a rope from stake `from` to a spot. Stakes stand a minimum distance apart and
    // a section is at most a section long. A stake takes two ropes. The road may not bend more
    // than the limit at a stake. And a section may not run over road that is already staked out.
    bool RopeAllowed(int from, Vector3 to, int toStake)
    {
        var t = Game.I.tuning;
        Vector3 a = Flat(stakes[from]), b = Flat(to);
        float distance = Vector3.Distance(a, b);
        if (distance < t.minStakeSpacing - 0.01f || distance > t.sectionLength + 0.01f) return false;
        if (LinksAt(from) >= 2 || (toStake >= 0 && LinksAt(toStake) >= 2)) return false;
        if (!BendAllowed(from, b) || (toStake >= 0 && !BendAllowed(toStake, a))) return false;
        return !Crosses(a, b);
    }

    // would a rope from this stake to there turn the road too sharply at the stake?
    bool BendAllowed(int stake, Vector3 to)
    {
        int before = Beyond(stake, -1);
        if (before < 0) return true;
        Vector3 here = Flat(stakes[stake]);
        return Vector3.Angle(here - Flat(stakes[before]), to - here) <= Game.I.tuning.maxBend + 0.01f;
    }

    // Would a section from a to b run over road that is already staked out? Its two ends are let
    // off, since sections that share a stake meet there.
    bool Crosses(Vector3 a, Vector3 b)
    {
        float length = Vector3.Distance(a, b), ends = Reach;
        for (float s = ends; s <= length - ends; s += 0.5f)
        {
            Vector3 p = Vector3.Lerp(a, b, s / length);
            if (Section(p.x, p.z, out _, out _, out _)) return true;
        }
        return false;
    }

    // May a new stake go in at x,z, roped to stake `from` if there is one?
    bool CanAdd(float x, float z, int from)
    {
        if (stakes.Count >= MaxStakes || !Inside(x, z, IsLand ? EdgeMargin : Margin * 0.5f)) return false;
        if (Section(x, z, out _, out _, out _)) return false;    // not on road that is already staked out
        var stake = new Vector3(x, 0, z);
        foreach (var other in stakes)
            if (Vector3.Distance(Flat(other), stake) < Game.I.tuning.minStakeSpacing) return false;
        return from < 0 || RopeAllowed(from, stake, -1);
    }

    // May stakes `from` and `to` be roped together?
    bool CanJoin(int from, int to)
    {
        return from >= 0 && to >= 0 && from != to && !Linked(from, to) && RopeAllowed(from, stakes[to], to);
    }

    // host: a stake goes in at x,z, standing on the ground as it is there, roped to stake `from`
    // if there is one. If `to` is a stake, no new stake: `from` and `to` are roped instead.
    public bool HostStake(int slot, float x, float z, int from, int to)
    {
        if (!Ready) return false;
        if (from >= stakes.Count || to >= stakes.Count) return false;
        int made;
        if (to >= 0)
        {
            if (!CanJoin(from, to)) return false;
            links.Add(new Vector2Int(from, to));
            made = to;
        }
        else
        {
            if (!CanAdd(x, z, from)) return false;
            stakes.Add(new Vector3(x, HeightAt(x, z), z));
            made = stakes.Count - 1;
            if (from >= 0) links.Add(new Vector2Int(from, made));
        }
        Net.ToClients(StakesMsg(slot, made), true);
        StakesChanged(slot, made);
        return true;
    }

    // host: the rope at a stake goes up or down by so many clicks' worth, or (steps 0) the stake
    // comes out, with every rope tied to it. The ground stays as it is.
    public bool HostStakeEdit(int stake, int steps)
    {
        if (!Ready || stake < fixedStakes || stake >= stakes.Count) return false;    // a town's stake stays as it is
        if (steps != 0)
        {
            Vector3 s = stakes[stake];
            s.y = Mathf.Clamp(s.y + steps * Game.I.tuning.heightPerClick, 0.1f, 60f);
            stakes[stake] = s;
        }
        else
        {
            stakes.RemoveAt(stake);
            for (int k = links.Count - 1; k >= 0; k--)
            {
                var l = links[k];
                if (l.x == stake || l.y == stake) { links.RemoveAt(k); continue; }
                links[k] = new Vector2Int(l.x > stake ? l.x - 1 : l.x, l.y > stake ? l.y - 1 : l.y);
            }
        }
        Net.ToClients(StakesMsg(255, -1), true);
        StakesChanged(255, -1);
        return true;
    }

    public void RequestStakeEdit(int stake, int steps)
    {
        if (Net.IsHost) { HostStakeEdit(stake, steps); return; }
        var m = Msg.New(Op.StakeEdit, 8);
        m.U8((byte)id);
        m.U8((byte)stake);
        m.U8((byte)(steps + 128));
        Net.ToHost(m, true);
    }

    Msg StakesMsg(int slot, int made)
    {
        var m = Msg.New(Op.PlotStakes, 32 + stakes.Count * 12 + links.Count * 2);
        m.U8((byte)id);
        m.U8((byte)slot);                   // whose stake this was (255: nobody's), and which
        m.U8((byte)(made < 0 ? 255 : made));
        m.U8((byte)stakes.Count);
        foreach (var s in stakes) m.V3(s);
        m.U8((byte)links.Count);
        foreach (var l in links) { m.U8((byte)l.x); m.U8((byte)l.y); }
        return m;
    }

    public void OnStakes(Msg m)
    {
        int slot = m.U8(), made = m.U8();
        int had = stakes.Count;
        stakes.Clear();
        links.Clear();
        int n = m.U8();
        if (n < had) selected = -1;     // one came out, so the numbers have moved
        for (int i = 0; i < n; i++) stakes.Add(m.V3());
        n = m.U8();
        for (int i = 0; i < n; i++) { int a = m.U8(), b = m.U8(); links.Add(new Vector2Int(a, b)); }
        if (Ready) StakesChanged(slot, made == 255 ? -1 : made);
        else if (h != null && expected <= 0) Build();
    }

    void StakesChanged(int slot, int made)
    {
        // whoever put the stake down carries on from it
        if (slot == Game.I.localSlot && made >= 0)
        {
            foreach (var plot in Game.I.plots) if (plot != this) plot.selected = -1;
            selected = made;
        }
        if (selected >= stakes.Count) selected = -1;
        if (hotPlot == this) hotPlot = null;
        // only the sections that changed, where they were and where they are now
        var old = segs;
        Rebuild();
        for (int k = 0; k < Mathf.Max(old.Length, segs.Length); k++)
        {
            if (k < old.Length && k < segs.Length && Same(old[k], segs[k])) continue;
            if (k < old.Length) Resolve(old[k]);
            if (k < segs.Length) Resolve(segs[k]);
        }
        Survey();
        Upload(false);
        BuildStakes();
    }

    public void RequestStake(float x, float z, int from, int to)
    {
        if (Net.IsHost) { HostStake(Game.I.localSlot, x, z, from, to); return; }
        var m = Msg.New(Op.Stake, 16);
        m.U8((byte)id);
        m.F32(x);
        m.F32(z);
        m.U8((byte)(from < 0 ? 255 : from));
        m.U8((byte)(to < 0 ? 255 : to));
        Net.ToHost(m, true);
    }

    // ---- the patch

    // The grid square a spot is in. A section's grid has a whole number of squares to a lane and
    // to its length, as near the patch width as that allows, so none is cut short; each shoulder
    // is one square wide. At a mitred end the squares are wedges. The square is given in the
    // section's own terms: t along it and metres across it.
    void Square(Tuning tuning, int link, ref float t, ref float side, out float halfT, out float halfSide)
    {
        float cellSide = lane / Mathf.Max(1, Mathf.RoundToInt(lane / tuning.patchWidth));
        int rows = Mathf.Max(1, Mathf.RoundToInt(segs[link].len / tuning.patchWidth));
        if (Mathf.Abs(side) > lane)
        {
            side = Mathf.Sign(side) * (lane + shoulderWidth * 0.5f);
            halfSide = shoulderWidth * 0.5f;
        }
        else
        {
            int columns = Mathf.RoundToInt(lane * 2f / cellSide);
            side = -lane + (Mathf.Clamp(Mathf.FloorToInt((side + lane) / cellSide), 0, columns - 1) + 0.5f) * cellSide;
            halfSide = cellSide * 0.5f;
        }
        t = (Mathf.Clamp(Mathf.FloorToInt(t * rows), 0, rows - 1) + 0.5f) / rows;
        halfT = 0.5f / rows;
    }

    // the ground points in a grid square: a box of points that is sure to hold it
    void Box(int link, float t, float side, float halfT, float halfSide, out int x0, out int x1, out int z0, out int z1)
    {
        Vector3 centre = World(link, t, side);
        float radius = Mathf.Sqrt(halfT * halfT * segs[link].len * segs[link].len * 3f + halfSide * halfSide) + 1f;
        Box(centre.x, centre.z, radius, out x0, out x1, out z0, out z1);
    }

    void Box(float x, float z, float radius, out int x0, out int x1, out int z0, out int z1)
    {
        x0 = Mathf.Max(0, Mathf.FloorToInt((x - radius - origin.x) / Cell)); x1 = Mathf.Min(w - 1, Mathf.CeilToInt((x + radius - origin.x) / Cell));
        z0 = Mathf.Max(0, Mathf.FloorToInt((z - radius - origin.z) / Cell)); z1 = Mathf.Min(d - 1, Mathf.CeilToInt((z + radius - origin.z) / Cell));
    }

    bool InSquare(int i, int link, float t, float side, float halfT, float halfSide)
    {
        return zone[i] != 0 && linkOf[i] == link && Mathf.Abs(along[i] - t) <= halfT * 1.02f && Mathf.Abs(across[i] - side) <= halfSide * 1.02f;
    }

    // is there anything left for this tool to do at a point?
    bool Wants(int i, int tool)
    {
        bool level = Mathf.Abs(h[i] - target[i]) < Level;
        if (tool == Grade) return !level;
        return zone[i] == 1 && level && (gravel[i] < FullGravel || packed[i] < 100);
    }

    // is there anything left for this tool to do in a grid square?
    bool SquareWants(int link, float t, float side, float halfT, float halfSide, int tool)
    {
        Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                if (InSquare(i, link, t, side, halfT, halfSide) && Wants(i, tool)) return true;
            }
        return false;
    }

    // host: one click. Returns false if it came too soon after that player's last one.
    // `hot` is the clicking player's own word that their crosshair was on the hot spot.
    public bool HostClick(int slot, float x, float z, bool hot = false, int tool = Grade)
    {
        var tuning = Game.I.tuning;
        if (!Ready || !Section(x, z, out int link, out float t, out float side)) return false;
        // a little slack, so a client clicking exactly at the cap is not punished for network jitter
        if (Time.time < hostNextClick[slot]) return false;
        hostNextClick[slot] = Time.time + 0.8f / tuning.clicksPerSecond;

        bool round = tuning.brushRound >= 0.5f;
        float halfT = 0, halfSide = 0, radius = tuning.patchWidth * 0.5f;
        int x0, x1, z0, z1;
        if (round) Box(x, z, radius, out x0, out x1, out z0, out z1);
        else
        {
            Square(tuning, link, ref t, ref side, out halfT, out halfSide);
            Box(link, t, side, halfT, halfSide, out x0, out x1, out z0, out z1);
        }
        changed.Clear();
        int full = FullGravel;
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                if (zone[i] == 0) continue;
                float distance;
                if (round)
                {
                    float dx = origin.x + ix * Cell - x, dz = origin.z + iz * Cell - z;
                    distance = Mathf.Sqrt(dx * dx + dz * dz) / radius;
                    if (distance > 1.02f) continue;
                }
                else
                {
                    if (!InSquare(i, link, t, side, halfT, halfSide)) continue;
                    distance = Mathf.Max(Mathf.Abs(along[i] - t) / halfT, Mathf.Abs(across[i] - side) / halfSide);
                }
                // a soft edge moves the rim of the patch less than the middle
                float weight = (hot ? tuning.hotSpotBonus : 1f) * Mathf.Lerp(1f, 1f - Mathf.SmoothStep(0, 1, distance), tuning.brushSoftEdge);
                if (tool == Grade)
                {
                    float next = Mathf.MoveTowards(h[i], target[i], tuning.heightPerClick * weight);
                    // short of the line, ground sits on whole millimetres, which is what a late joiner is sent
                    float whole = Mathf.Round(next * 1000f) / 1000f;
                    if (next != target[i] && whole != h[i]) next = whole;
                    if (next == h[i]) continue;
                    h[i] = next;
                }
                else
                {
                    // gravel: only on the road, only where it is on its line. It goes down to full
                    // depth, and after that the same clicks pack it.
                    if (zone[i] != 1 || Mathf.Abs(h[i] - target[i]) >= Level) continue;
                    if (gravel[i] < full) gravel[i] = (byte)Mathf.Min(full, gravel[i] + Mathf.Max(1, Mathf.RoundToInt(tuning.gravelPerClick * 1000f * weight)));
                    else if (packed[i] < 100) packed[i] = (byte)Mathf.Min(100, packed[i] + Mathf.Max(1, Mathf.RoundToInt(tuning.compactPerClick * 100f * weight)));
                    else continue;
                }
                health[i] = 100;    // worked ground is sound again
                changed.Add(i);
            }
        clicks++;
        Broadcast();
        return true;
    }

    // host: send the points in `changed` as they now are, and show them
    void Broadcast()
    {
        edit.Reset(Op.PlotEdit);
        edit.U8((byte)id);
        edit.U32(clicks);
        edit.U16((ushort)changed.Count);
        foreach (int i in changed)
        {
            edit.U32((uint)i);
            edit.F32(h[i]);
            edit.U8(gravel[i]);
            edit.U8(packed[i]);
            Touch(i);
            if (edited != null) edited[i] = 1;
        }
        Net.ToClients(edit, true);
        if (changed.Count > 0) Upload(true);
    }

    public void OnEdit(Msg m)
    {
        if (!Ready) return;
        clicks = m.U32();
        int n = m.U16();
        for (int k = 0; k < n; k++)
        {
            int i = (int)m.U32();
            h[i] = m.F32();
            gravel[i] = m.U8();
            packed[i] = m.U8();
            Touch(i);
        }
        if (n > 0) Upload(true);
    }

    // any player: ask for a click here
    public void RequestClick(float x, float z, bool hot = false, int tool = Grade)
    {
        if (Net.IsHost) { HostClick(Game.I.localSlot, x, z, hot, tool); return; }
        var m = Msg.New(Op.Click, 12);
        m.U8((byte)id);
        m.F32(x);
        m.F32(z);
        m.U8((byte)(hot ? 1 : 0));
        m.U8((byte)tool);
        Net.ToHost(m, true);
    }

    // ---- trucks

    // How well a truck's wheels bite here: 1 on packed gravel, less on loose gravel, least on bare road.
    public float Going(float x, float z)
    {
        if (!Ready || !Inside(x, z, 0.5f)) return 1f;
        int i = Mathf.RoundToInt((z - origin.z) / Cell) * w + Mathf.RoundToInt((x - origin.x) / Cell);
        if (zone[i] != 1) return 1f;    // the way up onto the road is not the road's fault
        if (gravel[i] < FullGravel) return 0.4f;
        return Mathf.Lerp(0.6f, 1f, packed[i] * 0.01f);
    }

    // Walk the ropes from one stake until they run out, or until they reach `end`. Players put
    // stakes down in any order, so the order is found by following the ropes. Fills `chain`.
    bool Chain(int start, int end)
    {
        chain.Clear();
        int at = start, came = -1;
        while (chain.Count <= links.Count)
        {
            int next = -1;
            for (int k = 0; k < links.Count && next < 0; k++)
            {
                if (k == came) continue;
                if (links[k].x == at) { chain.Add(k); at = links[k].y; next = k; }
                else if (links[k].y == at) { chain.Add(~k); at = links[k].x; next = k; }
            }
            if (next < 0 || at == start || at == end) break;
            came = next;
        }
        return chain.Count > 0 && (end < 0 || at == end);
    }

    // the stake a truck sets off from: the first one with a single rope
    int ChainStart()
    {
        for (int i = 0; i < stakes.Count; i++) if (LinksAt(i) == 1) return i;
        return -1;
    }

    // The truck's way along the road: up onto the first stake, along the right-hand lane of each
    // section in turn, and off past the last; or back the other way, in the other lane.
    public bool Route(List<Vector3> path, bool back)
    {
        path.Clear();
        // on the map the road runs from one town's stake to the other's, and there is no road
        // until the ropes join them
        int start = !Ready ? -1 : IsLand ? 0 : ChainStart();
        if (start < 0 || !Chain(start, IsLand ? 1 : -1)) return false;
        float RunUp = IsLand ? 9f : 14f;    // a town's pad is smaller than the yard
        float half = (back ? -1f : 1f) * lane * 0.5f;       // to the right of the way the chain runs
        int c0 = chain[0], c1 = chain[chain.Count - 1];
        Seg first = segs[c0 < 0 ? ~c0 : c0], last = segs[c1 < 0 ? ~c1 : c1];
        Vector3 from = c0 < 0 ? first.a + first.d * first.len : first.a, ahead = c0 < 0 ? -first.d : first.d;
        for (float s = RunUp; s > 0; s -= 1f) path.Add(from - ahead * s + new Vector3(ahead.z, 0, -ahead.x) * half);
        foreach (int c in chain)
        {
            int k = c < 0 ? ~c : c;
            int steps = Mathf.Max(1, Mathf.RoundToInt(segs[k].len));
            for (int j = 0; j < steps; j++)
            {
                float t = (float)j / steps;
                path.Add(c < 0 ? World(k, 1f - t, -half) : World(k, t, half));
            }
        }
        Vector3 to = c1 < 0 ? last.a : last.a + last.d * last.len;
        ahead = c1 < 0 ? -last.d : last.d;
        for (float s = 0; s <= RunUp; s += 1f) path.Add(to + ahead * s + new Vector3(ahead.z, 0, -ahead.x) * half);
        if (back) path.Reverse();    // the other way, which puts it in the other lane
        return path.Count > 8;
    }

    // host: a truck is on this road, its wheels touching the ground at these spots. It does a
    // random amount of damage to the grid square under it. Once a square is worn below the
    // threshold, the wheels start to cut into it: gravel is scattered first, then the ground ruts.
    public void Wear(Vector3 centre, List<Vector3> wheels)
    {
        var tuning = Game.I.tuning;
        if (!Ready || !Section(centre.x, centre.z, out int link, out float t, out float side)) return;
        Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
        Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
        int damage = Mathf.RoundToInt(Random.value * tuning.truckDamage), left = 100;
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                if (!InSquare(i, link, t, side, halfT, halfSide)) continue;
                health[i] = (byte)Mathf.Max(0, health[i] - damage);
                left = Mathf.Min(left, health[i]);
            }
        if (left >= tuning.damageThreshold) return;

        changed.Clear();
        foreach (var wheel in wheels)
        {
            Box(wheel.x, wheel.z, 0.45f, out x0, out x1, out z0, out z1);
            float depth = tuning.rutDepth * Random.Range(0.4f, 1f);
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                {
                    int i = iz * w + ix;
                    float dx = origin.x + ix * Cell - wheel.x, dz = origin.z + iz * Cell - wheel.z;
                    if (zone[i] == 0 || dx * dx + dz * dz > 0.45f * 0.45f || changed.Contains(i)) continue;
                    packed[i] = 0;
                    int scatter = Mathf.RoundToInt(depth * 1000f);
                    if (gravel[i] >= scatter) gravel[i] = (byte)(gravel[i] - scatter);
                    else
                    {
                        h[i] = Mathf.Max(0, Mathf.Round((h[i] - (depth - gravel[i] * 0.001f)) * 1000f) / 1000f);
                        gravel[i] = 0;
                    }
                    changed.Add(i);
                }
        }
        Broadcast();
    }

    // ---- the local player's crosshair

    void Update()
    {
        var g = Game.I;
        if (!Ready || cursor == null) return;
        if (tallyDirty && Time.unscaledTime >= nextTally)
        {
            tallyDirty = false;
            nextTally = Time.unscaledTime + 0.25f;
            Tally();
        }
        if (remakeAt > 0 && Time.unscaledTime >= remakeAt)
        {
            remakeAt = 0;
            if (Net.IsHost && Signature(g.tuning) != built) { g.lorries.Clear(); Generate(); return; }
        }
        var tuning = g.tuning;
        cursor.enabled = preview.enabled = hotRing.enabled = false;
        if (g.local == null || !Hud.Playing) return;
        var mouse = Mouse.current;
        if (mouse == null) return;
        var eye = g.cam.transform;

        if (Tool == Stakes)
        {
            StakeTool(tuning, mouse, eye);
            return;
        }
        if (hotPlot == this) DrawHot(tuning);
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.clickReach, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        Vector3 aim = hit.point;

        if (!Section(aim.x, aim.z, out int link, out float t, out float side)) return;
        Outline(tuning, aim, link, t, side);
        // holding the button keeps clicking, as fast as the cap allows
        if (!mouse.leftButton.isPressed || Time.time < nextClick) return;
        nextClick = Time.time + 1f / tuning.clicksPerSecond;
        RequestClick(aim.x, aim.z, HotClick(tuning, aim, link, t, side), Tool);
    }

    // The hot spot. It appears in a grid square with the first click there, and stays put. A
    // click with the crosshair on it counts for more, and sends it to a new place in the square.
    // Clicking into another square moves it there. It goes when the square has nothing left for
    // the tool in hand to do. It belongs to the local player alone.
    static Plot hotPlot;
    static int hotLink, hotTool;
    static float hotT, hotSide;             // the middle of its square, in the section's own terms
    static float hotHalfT, hotHalfSide;
    static float hotOffT, hotOffSide;       // where it is in the square

    void DrawHot(Tuning tuning)
    {
        if (hotLink >= segs.Length || hotTool != Tool || !SquareWants(hotLink, hotT, hotSide, hotHalfT, hotHalfSide, hotTool)) { hotPlot = null; return; }
        Vector3 centre = World(hotLink, hotT + hotOffT, hotSide + hotOffSide);
        const int Points = 20;
        hotRing.enabled = true;
        hotRing.positionCount = Points;
        for (int k = 0; k < Points; k++)
        {
            float angle = k * Mathf.PI * 2f / Points;
            Vector3 p = centre + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * tuning.hotSpotSize;
            hotRing.SetPosition(k, new Vector3(p.x, HeightAt(p.x, p.z) + 0.06f, p.z));
        }
    }

    // somewhere new in the square, clear of where it was
    void MoveHot(Tuning tuning)
    {
        float size = tuning.hotSpotSize, length = segs[hotLink].len;
        float roomT = Mathf.Max(0, hotHalfT - size / length), roomSide = Mathf.Max(0, hotHalfSide - size);
        for (int tries = 0; tries < 12; tries++)
        {
            float t = Random.Range(-roomT, roomT), side = Random.Range(-roomSide, roomSide);
            float ds = (t - hotOffT) * length, dside = side - hotOffSide;
            if (ds * ds + dside * dside <= size * size * 6f && tries < 11) continue;
            hotOffT = t;
            hotOffSide = side;
            return;
        }
    }

    // One click by the local player at aim, in this square. Says whether it was on the hot spot.
    bool HotClick(Tuning tuning, Vector3 aim, int link, float t, float side)
    {
        Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
        if (!SquareWants(link, t, side, halfT, halfSide, Tool))
        {
            if (hotPlot == this) hotPlot = null;
            return false;
        }
        bool same = hotPlot == this && hotLink == link && hotTool == Tool && Mathf.Abs(hotT - t) < 0.0001f && Mathf.Abs(hotSide - side) < 0.01f;
        if (!same)
        {
            hotPlot = this;
            hotLink = link;
            hotTool = Tool;
            hotT = t; hotSide = side;
            hotHalfT = halfT; hotHalfSide = halfSide;
            MoveHot(tuning);
            return false;
        }
        Vector3 centre = World(hotLink, hotT + hotOffT, hotSide + hotOffSide);
        float dx = aim.x - centre.x, dz = aim.z - centre.z;
        if (dx * dx + dz * dz > tuning.hotSpotSize * tuning.hotSpotSize) return false;
        MoveHot(tuning);
        return true;
    }

    // The stake tool. Everything it does starts from the chosen stake, shown white:
    //   left click the ground   put a stake down, roped to the chosen one; the new stake is now chosen
    //   left click a stake      rope the chosen stake to it, and choose it
    //   wheel                   raise or lower the rope at the chosen stake
    //   X                       pull the chosen stake out
    //   right click             let go of the chosen stake
    // The rope that would be made shows green if it is allowed and red if it is not.
    void StakeTool(Tuning tuning, Mouse mouse, Transform eye)
    {
        if (selected >= 0)
        {
            float wheel = mouse.scroll.ReadValue().y;
            var keys = Keyboard.current;
            if (keys != null && keys.xKey.wasPressedThisFrame)
            {
                RequestStakeEdit(selected, 0);
                Choose(-1);
                return;
            }
            if (Mathf.Abs(wheel) > 0.01f) RequestStakeEdit(selected, wheel > 0 ? 1 : -1);
            if (mouse.rightButton.wasPressedThisFrame) { Choose(-1); return; }
        }

        int stakeAimed = -1;
        if (Physics.Raycast(eye.position, eye.forward, out var first, tuning.clickReach, ~0, QueryTriggerInteraction.Collide)
            && first.collider.transform.parent == stakeRoot && stakeByCollider.TryGetValue(first.collider.GetInstanceID(), out int found)) stakeAimed = found;
        if (stakeAimed >= 0)
        {
            bool canJoin = CanJoin(selected, stakeAimed);
            if (selected >= 0 && selected != stakeAimed && !Linked(selected, stakeAimed)) Preview(stakes[selected], stakes[stakeAimed], canJoin);
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (canJoin) RequestStake(0, 0, selected, stakeAimed);
                Choose(stakeAimed);
            }
            return;
        }

        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.clickReach, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        Vector3 aim = hit.point;
        bool allowed = CanAdd(aim.x, aim.z, selected);
        var end = new Vector3(aim.x, HeightAt(aim.x, aim.z), aim.z);
        // the rope the new stake would pull from the chosen one, or just where the stake would stand
        Preview(selected >= 0 ? stakes[selected] : end + Vector3.up * 2f, end, allowed);
        if (allowed && mouse.leftButton.wasPressedThisFrame) RequestStake(aim.x, aim.z, selected, -1);
    }

    // one chosen stake at a time, across all the plots
    void Choose(int stake)
    {
        foreach (var plot in Game.I.plots)
            if (plot != this && plot.selected >= 0) { plot.selected = -1; if (plot.Ready) plot.BuildStakes(); }
        selected = stake;
        BuildStakes();
    }

    // a rope that is not there yet: green if it can be made, red if it breaks a rule
    void Preview(Vector3 a, Vector3 b, bool allowed)
    {
        preview.enabled = true;
        preview.positionCount = 2;
        preview.SetPosition(0, a);
        preview.SetPosition(1, b);
        preview.startColor = preview.endColor = allowed ? new Color(0.25f, 1f, 0.35f) : new Color(1f, 0.1f, 0.1f);
    }

    // the edge of the patch a click would move, draped over the ground
    void Outline(Tuning tuning, Vector3 aim, int link, float t, float side)
    {
        bool round = tuning.brushRound >= 0.5f;
        float halfT = 0, halfSide = 0;
        if (!round) Square(tuning, link, ref t, ref side, out halfT, out halfSide);
        const int Points = 48;
        cursor.enabled = true;
        cursor.startColor = cursor.endColor = Tool == Grade ? Color.white : (Color)GravelLoose;
        cursor.positionCount = Points;
        for (int k = 0; k < Points; k++)
        {
            Vector3 p;
            if (round)
            {
                float angle = k * Mathf.PI * 2f / Points;
                p = aim + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * tuning.patchWidth * 0.5f;
            }
            else
            {
                // round the four sides, twelve points to a side
                float e = k % 12 / 12f * 2f - 1f;
                int edge = k / 12;
                float u = edge == 0 ? e : edge == 1 ? 1 : edge == 2 ? -e : -1;
                float v = edge == 0 ? -1 : edge == 1 ? e : edge == 2 ? 1 : -e;
                p = World(link, t + v * halfT, side + u * halfSide);
            }
            cursor.SetPosition(k, new Vector3(p.x, HeightAt(p.x, p.z) + 0.04f, p.z));
        }
    }
}
