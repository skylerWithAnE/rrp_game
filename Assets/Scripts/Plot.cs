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
//   8  ramp, bare       a 14 degree climb on bare ground that is on its line: trucks cannot
//   9  ramp, gravel     the same climb under loose gravel: they can
//  10  steep, gravel    a 20 degree climb under loose gravel: they cannot
//  11  steep, packed    the same climb, packed: they can
//  12  T                a finished junction of three ropes at one stake
//  13  crossroads       a finished junction of four
//  14  Y                a finished junction with two branches 60 degrees apart
//  15  junction         rough ground with a road across it, to rope a branch to
//  16  quarry           a quarry, a service road from it to a drop, and a road that needs gravel
//  17  paving           a finished gravel road to pave: a dump truck, a roller, and paint
//  18  paved            the same road paved and painted, with trucks on it
//  19  driving road     a road in four states, for the vehicles players drive: bare, gravelled, spread asphalt, finished
//  20  driving field    rough ground to drive over
//  21  wear, dirt       a long bare road on its line, under steady traffic, that wears out
//  22  wear, gravel     the same road gravelled and packed
//  23  spin-out         a road with a bend in it, under loose gravel but for its two ends
//  24  paint, marked    a strip of rolled asphalt to paint by hand, with the place for each line marked
//  25  paint, plain     the same strip with nothing marked
//  26  the map          one big piece of land with a town at each end: see "the map" below
//
// Each belongs to one of the host's choices (see MapOf) and exists only while that is chosen:
// the test grounds for building, for trucks, for junctions and for the quarry, or the land of
// a map.
//
// A rope has a role. A road for everyone carries the trucks that drive from end to end. A
// service road is the crew's: only the gravel truck uses it. The zoning tool changes a
// section's role.
//
// The host decides every click and every stake and sends the results; clients never generate or
// change anything themselves.
public class Plot : MonoBehaviour
{
    public const float Cell = 0.25f;        // distance between ground points
    public const int PresetSections = 3;    // station 2: level, climbing, falling
    public const int Count = 27, Quarry = 16, Paving = 17, Paved = 18, DriveRoad = 19, DriveField = 20, WearDirt = 21, WearGravel = 22, SpinOut = 23, PaintMarked = 24, PaintPlain = 25, Land = 26;
    public static readonly string[] Names = { "clicking", "hillside", "gravel", "good road", "bad road", "wear", "hairpin", "hairpin good",
        "ramp bare", "ramp gravel", "steep gravel", "steep packed", "T", "crossroads", "Y", "junction", "quarry", "paving", "paved", "driving road", "driving field",
        "dirt road", "gravel road", "loose gravel", "marked strip", "unmarked strip", "map" };
    // What the host can choose. 1 to 5 are land between two towns. The others are test grounds,
    // each about one thing: the sizes, building a road, what trucks can drive, and junctions.
    public static readonly string[] MapNames = { "Scale yard", "Short, 60 m", "Middle, a hill in the way", "Long, 300 m", "Climb, 16 m up", "Switchback, rocks",
        "Building roads", "Trucks: road types and turns", "Junctions", "Quarry and service roads", "Paving and painting", "Driving",
        "Wear: a dirt road and a gravel road under traffic", "Spin-out: loose gravel round a bend", "Painting lines by hand" };
    public static readonly string[] MapButtons = { "Yard", "Short", "Middle", "Long", "Climb", "Switchback", "Building", "Trucks", "Junctions", "Quarry", "Paving", "Driving", "Wear", "Spin-out", "Painting" };
    public const int YardMap = 0, BuildingMap = 6, TrucksMap = 7, JunctionsMap = 8, QuarryMap = 9, PavingMap = 10, DrivingMap = 11, WearMap = 12, SpinMap = 13, PaintMap = 14;
    public const int FirstNewMap = 12;      // the grounds from here on were built for the slices of 2026-10-06, and have a row of buttons to themselves
    public static bool LandMap(int map) { return map >= 1 && map <= 5; }

    // roads with steady traffic: a truck sets off down each lane every few seconds
    public static bool Steady(int id) { return id == Land || id == WearDirt || id == WearGravel || id == SpinOut; }
    // how many trucks a plot's road can have on it at once, both lanes together
    public static int Traffic(int id) { return id == WearDirt || id == WearGravel || id == SpinOut ? 12 : 2; }

    // which of the host's choices a plot belongs to
    public static int MapOf(int id)
    {
        if (id == Land) return -1;
        if (id == WearDirt || id == WearGravel) return WearMap;
        if (id == SpinOut) return SpinMap;
        if (id == PaintMarked || id == PaintPlain) return PaintMap;
        if (id <= 2 || id == 6) return BuildingMap;     // clicking, the hillside, gravel, and the hairpin to level
        if (id == Quarry) return QuarryMap;
        if (id == Paving || id == Paved) return PavingMap;
        if (id == DriveRoad || id == DriveField) return DrivingMap;
        return id <= 11 ? TrucksMap : JunctionsMap;
    }
    public bool ShownOn(int map) { return IsLand ? LandMap(map) : MapOf(id) == map; }
    const int Switchback = 5;
    const float TierRise = 6f;              // the switchback map: each of its two banks is this high
    const float RampDegrees = 12f;          // ...and the one way up each is this steep: more than a truck climbs on bare ground, less than it climbs on gravel
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
    public const int Zone = 4;                  // the zoning tool: it changes a section's role
    public const int Pave = 5, Paint = 6;       // spreading asphalt, and painting lines: sent with a click like the first two
    public const int Dev = 7;                   // the dev tool: it finishes a section's next stage in one click
    public const int Brush = 8, BrushYellow = 9;    // the paint brush: paint wherever it points, white or yellow
    public const int TarSpray = 10, Grinder = 11;   // the two ways to take hand-drawn paint off: see Lines
    public const int DropTool = 12;                 // the survey tool that places the gravel drop-off, at the quarry
    public static int Tool = Stakes;            // what the local player is holding

    public int id;
    public int w, d;                        // points per side
    public float[] h;
    public byte[] gravel, packed;           // per point: millimetres of gravel on it, and how far that is packed, 0 to 100
    // Per point, what is on top of the packed gravel: 0 nothing; under 100 asphalt lying where
    // the dump truck left it; 100 spread out; up to 200 as it is rolled; 255 rolled and painted.
    public byte[] top;
    public const int Heaped = 60, Spread = 100, Rolled = 200, Painted = 255;
    public const int DaubYellow = 253, DaubWhite = 254;     // rolled, and painted over by hand with the brush
    byte[] health;                          // host: what the trucks have left of it, 0 to 100
    public uint clicks;                     // clicks the host has accepted since the ground was made
    public float roadShare, shoulderShare;  // how much of each is on its line, 0 to 1
    public float gravelShare, packedShare;  // how much of the road has its full gravel, and has it packed
    public readonly List<Yard.Label> labels = new List<Yard.Label>();
    public readonly List<Vector3> stakes = new List<Vector3>();    // y is the height of the line at the stake
    public readonly List<Vector3Int> links = new List<Vector3Int>();    // two stakes, and the rope's role: 0 a road for everyone, 1 a service road
    public int selected = -1;               // the local player's stake: the next one is roped to it

    // trucks damage this road: the first wear road, the two of the Wear ground, and a map while its switch is on
    public bool Wears => id == 5 || id == WearDirt || id == WearGravel || (IsLand && Game.I.tuning.mapWear >= 0.5f) || (IsQuarry && Game.I.tuning.quarryWear >= 0.5f);
    public bool TrucksPack => IsLand || id == SpinOut;      // trucks pack the gravel they drive over
    public bool IsLand => id == Land;
    public float rutShare, deepest;         // how much of the road's ground has been cut below its line, 0 to 1, and the deepest cut (m)
    public int fixedStakes;                 // the map: the first stakes are the towns', and stay where they are
    public bool joined;                     // the map: ropes run all the way from one town's stake to the other's
    public float roadLength;                // the map: metres of rope in the chain that starts at the first town
    ushort[] coarse;                        // the map: the land as the host made it, a height in millimetres every metre
    int cw, cd;
    byte[] edited;                          // host, the map: points a click has changed, which is all a joiner needs sending
    float remakeAt;                         // host, the map: make the land again once the sliders have stopped moving
    readonly List<Vector3> rocks = new List<Vector3>();     // the map: rocks nothing can move, as x, radius, z. No stake or road may touch one.

    // why the rope or stake under the crosshair cannot be made, for the screen
    public static string Why = "";
    public static int WhyFrame;
    public static bool WhyBad;      // it is a refusal, not a hint
    static bool No(string why) { Why = why; return false; }
    static void Say(string what, bool bad) { Why = what; WhyBad = bad; WhyFrame = Time.frameCount; }
    public static void Hint(string what) { Say(what, false); }
    // how a plot's road starts: 2 gravelled and packed, 1 gravelled, 0 bare
    int StartsAs => TestSurface != null && id >= 8 && id <= 11 ? TestSurface[id - 8]
        : id == 3 || id == 5 || id == 7 || (id >= 11 && id <= 14) || id == Paving || id == Paved || id == WearGravel || id == PaintMarked || id == PaintPlain ? 2 : id == 9 || id == 10 || id == SpinOut ? 1 : 0;
    // Test tooling: the four ramps of the Trucks ground (plots 8 to 11) made to order, for the
    // scripts that measure what a truck climbs. Degrees and surface (0 bare, 1 loose gravel,
    // 2 packed) for each, and how high they go. Null: the ramps as designed.
    public static float[] TestDegrees;
    public static int[] TestSurface;
    public static float TestHeight = 16f;
    // a stake here takes more than two ropes. Only on the junction test ground until it has been played.
    bool Junctions => id >= 12 && id <= 16;
    public bool IsQuarry => id == Quarry;
    public int stock;                       // the quarry: gravel on the heap and not yet laid, in clicks' worth
    public int carrying;                    // the quarry: which players' shovels are loaded, a bit each
    public bool heapPlaced;                 // the quarry: a player has said where the heap goes
    public Vector3 heapAt;
    bool placing;                           // the local player is choosing where
    public readonly List<int> depots = new List<int>();     // stakes where a truck stands until it is sent on
    public Lines lines;                     // the Painting ground's strips: paint drawn by hand, in cells finer than the ground's points
    public Vector3 Origin => origin;
    public float Width => SizeX;
    public float Depth => SizeZ;
    public float LaneWidth => lane;
    public bool Where(float x, float z, out int link, out float t, out float side) { return Section(x, z, out link, out t, out side); }
    public float Length(int link) { return segs[link].len; }

    // a number for the grid square a spot of a section is in: the same for the four squares of a row across the road but for its last two bits
    public int SquareKey(int link, float t, float side)
    {
        Square(Game.I.tuning, link, ref t, ref side, out float halfT, out float halfSide);
        int row = Mathf.FloorToInt(t / (halfT * 2f)), column = Mathf.Clamp(Mathf.FloorToInt((side + lane) / (halfSide * 2f)), 0, 3);
        return (link * 1000 + row) * 4 + column;
    }
    public int drop = -1;                   // the quarry: the stake that is the gravel drop-off, which a player puts down with the survey tool
    const float PitDepth = 8f, PitRim = 30f, PitFloor = 14f;    // the quarry's pit: how deep, and its radius at the top and at the floor
    Vector3 PitCentre => origin + new Vector3(110f, 0, 55f);
    bool canLay, laid;                      // host, during a click: may gravel go down, and did any
    Transform quarryRoot, pile;             // the quarry: its rock, and the heap of gravel at the drop

    Vector3 origin;                         // world position of point 0,0
    float lane, shoulderWidth, shoulderDrop;

    // A section, worked out from its two stakes and whatever it meets at each.
    struct Seg
    {
        public Vector3 a, d, r;             // first stake on the ground; along; to the right
        public float len, yA, yB;
        public float kA, kB;                // how far its two ends slant per metre to the right: the mitres
        public float eA, eB;                // at a junction: how far it runs on past the stake, so the sections there overlap
        public float pA, pB;                // at a junction: how far from the stake it stays level, at the stake's height
        public int role;                    // 0 a road for everyone, 1 a service road
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
    Material wood, red, white, blue, orange;
    float nextClick;                                                    // local rate cap
    readonly float[] hostNextClick = new float[Session.MaxPlayers];     // host: rate cap per player
    readonly Msg edit = new Msg(8192);
    readonly List<int> changed = new List<int>();

    static readonly Color32 Outside = new Color32(104, 100, 92, 255);
    static readonly Color32 Grass = new Color32(112, 128, 88, 255);
    static readonly Color32 AsphaltLoose = new Color32(64, 54, 48, 255);
    static readonly Color32 AsphaltRolled = new Color32(28, 28, 32, 255);
    static readonly Color32 LineYellow = new Color32(236, 200, 40, 255);
    static readonly Color32 LineWhite = new Color32(236, 236, 228, 255);
    // a service road is the same road in blue
    static readonly Color32 ServiceRough = new Color32(112, 122, 146, 255);
    static readonly Color32 ServiceShoulderRough = new Color32(100, 110, 134, 255);
    static readonly Color32 ServiceDone = new Color32(160, 184, 222, 255);
    static readonly Color32 ServiceShoulderDone = new Color32(128, 158, 208, 255);
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
        rocks.Clear();
        stock = carrying = 0;
        heapPlaced = placing = false;
        depots.Clear();
        drop = -1;
        lines = null;
        quarryRoot = pile = null;
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
            case 8: case 9: case 10: case 11:
                {
                    // The ramps carry the row on to the left: level, up for two sections, level,
                    // and down again, so a truck from either end has the same climb. One section
                    // is not enough: a truck gets up 20 m of anything on the speed it arrives with.
                    float up = length * Mathf.Tan((TestDegrees != null ? TestDegrees[id - 8] : id <= 9 ? 14f : 20f) * Mathf.Deg2Rad);
                    if (TestDegrees != null)
                    {
                        // made to order for a measuring script: as many sections up as it takes to reach the height asked for
                        int n = Mathf.Max(1, Mathf.CeilToInt(TestHeight / up));
                        var heights = new List<float> { BaseHeight };
                        for (int k = 0; k <= n; k++) heights.Add(BaseHeight + up * k);
                        for (int k = n; k >= 0; k--) heights.Add(BaseHeight + up * k);
                        heights.Add(BaseHeight);
                        Straight(first - 100f - (id - 8) * 25f, 0, length, heights.ToArray());
                    }
                    else Straight(first - 100f - (id - 8) * 25f, 0, length, BaseHeight, BaseHeight, BaseHeight + up, BaseHeight + up * 2f, BaseHeight + up * 2f, BaseHeight + up, BaseHeight, BaseHeight);
                    Lay(t, ox, oz, 0);
                    break;
                }
            // the wear roads: five sections each, side by side, on their lines
            case WearDirt: case WearGravel: Straight(first - 8f - (id - WearDirt) * 26f, 0, length, BaseHeight, BaseHeight, BaseHeight, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            // the two strips to paint by hand: three sections each, side by side
            case PaintMarked: case PaintPlain: Straight(first - 8f - (id - PaintMarked) * 26f, 0, length, BaseHeight, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case SpinOut:
                {
                    // Two sections straight ahead, a bend to the left of 90 degrees in three
                    // shorter ones, and two more straight. All of it is under loose gravel but
                    // the first section and the last, which are packed: see below.
                    Vector3 at = new Vector3(first - 8f, BaseHeight, 0);
                    float[] turn = { 0, 0, -30f, -30f, -30f, 0, 0 };
                    float heading = 0;
                    stakes.Add(at);
                    for (int k = 0; k < turn.Length; k++)
                    {
                        heading += turn[k];
                        at += Quaternion.Euler(0, heading, 0) * Vector3.forward * (turn[k] != 0 ? 16f : length);
                        stakes.Add(at);
                        links.Add(new Vector3Int(k, k + 1, 0));
                    }
                    Lay(t, ox, oz, 0);
                    break;
                }
            case Paving: Straight(first - 25f, 0, length, BaseHeight, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case DriveRoad: Straight(first - 25f, 0, length, BaseHeight, BaseHeight, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case DriveField: Field(new Vector3(46f, 0, -8f), 50f, 60f, t.roughHeight * 0.6f, ox, oz); break;
            case Paved: Straight(first - 55f, 0, length, BaseHeight, BaseHeight, BaseHeight, BaseHeight); Lay(t, ox, oz, 0); break;
            case 12: Spokes(first - 25f, 20f, length, 0, 180f, 90f); Lay(t, ox, oz, 0); break;
            case 13: Spokes(first - 85f, 20f, length, 0, 180f, 90f, 270f); Lay(t, ox, oz, 0); break;
            case 14: Spokes(first - 25f, -50f, length, 180f, 30f, -30f); Lay(t, ox, oz, 0); break;
            case Quarry:
                {
                    // High ground with a pit dug into it. The pit is a cone 8 m deep; a
                    // service road comes along its rim and winds down three quarters of the
                    // way round to the floor, where the rock is. The cone falls exactly as
                    // fast as the road does, so the road is a bench cut into its side.
                    // Stake 0 is the quarry's loading bay, on the floor, and stake 1 the drop,
                    // up on top near a road for everyone that is bare and needs the gravel.
                    // Those two are depots and cannot be pulled out.
                    float top = BaseHeight + PitDepth;
                    Field(new Vector3(lane + 12f, 0, -8f), 170f, 110f, t.roughHeight * 0.3f, ox, oz, top, 18f);
                    Vector3 pit = PitCentre;
                    for (int i = 0; i < h.Length; i++)
                    {
                        float u = i % w * Cell - (pit.x - origin.x), v = i / w * Cell - (pit.z - origin.z);
                        float into = Mathf.Clamp01((PitRim - Mathf.Sqrt(u * u + v * v)) / (PitRim - PitFloor));
                        // the pit's sides and floor are smooth: only the high ground is rough
                        if (into > 0) h[i] = Mathf.Lerp(h[i], top, Mathf.Clamp01(into * 4f)) - PitDepth * into;
                    }
                    float[] at = { 0, 0,  38, 55,  20, 55,  20, 35,  20, 75,  51.9f, 63,  61, 76.1f,  73.2f, 83.1f,  92, 85 };
                    for (int i = 0; i < at.Length; i += 2) stakes.Add(new Vector3(origin.x + at[i], top, origin.z + at[i + 1]));
                    // the way down: ten stakes 30 degrees apart, the last of them the loading bay
                    for (int i = 0; i <= 9; i++)
                    {
                        float angle = (90f - 30f * i) * Mathf.Deg2Rad, radius = PitRim - (PitRim - PitFloor) * i / 9f;
                        var stake = new Vector3(pit.x + Mathf.Cos(angle) * radius, top - PitDepth * i / 9f, pit.z + Mathf.Sin(angle) * radius);
                        if (i == 9) stakes[0] = stake; else stakes.Add(stake);
                    }
                    int[] service = { 2, 1,  1, 5,  5, 6,  6, 7,  7, 8,  8, 9,  9, 10,  10, 11,  11, 12,  12, 13,  13, 14,  14, 15,  15, 16,  16, 17,  17, 0 };
                    for (int i = 0; i < service.Length; i += 2) links.Add(new Vector3Int(service[i], service[i + 1], 1));
                    links.Add(new Vector3Int(3, 2, 0));
                    links.Add(new Vector3Int(2, 4, 0));
                    fixedStakes = 2;
                    Rebuild();
                    for (int i = 0; i < h.Length; i++)
                        if (Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out int link, out float along_, out float side)) h[i] = TargetIn(link, along_, side);
                    break;
                }
            case 15:
                {
                    // a rough field beside where players start, with a road staked straight across it
                    Field(new Vector3(lane + 12f, 0, -8f), 54f, 60f, t.roughHeight * 0.5f, ox, oz);
                    for (int i = 0; i < 3; i++) stakes.Add(new Vector3(origin.x + 16f, BaseHeight, origin.z + 10f + i * length));
                    links.Add(new Vector3Int(0, 1, 0));
                    links.Add(new Vector3Int(1, 2, 0));
                    break;
                }
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
        top = new byte[h.Length];
        packed = new byte[h.Length];
        health = new byte[h.Length];
        if (IsLand) edited = new byte[h.Length];
        for (int i = 0; i < h.Length; i++) health[i] = 100;
        if (StartsAs > 0)
        {
            // gravel at full depth, and packed if the road starts finished
            if (id == 15) Rebuild();
            for (int i = 0; i < h.Length; i++)
            {
                if (!Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out _, out _, out float side) || Mathf.Abs(side) > lane) continue;
                gravel[i] = (byte)FullGravel;
                packed[i] = (byte)(StartsAs == 2 ? 100 : 0);
            }
        }
        if (id == DriveRoad)
        {
            // one section in each state: bare, gravelled, spread asphalt, finished
            for (int i = 0; i < h.Length; i++)
            {
                if (!Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out int link, out _, out float side) || Mathf.Abs(side) > lane || link == 0) continue;
                gravel[i] = (byte)FullGravel;
                packed[i] = (byte)(link >= 2 ? 100 : 0);
                top[i] = (byte)(link == 2 ? Spread : link == 3 ? Rolled : 0);
            }
        }
        if (id == SpinOut)
        {
            // the two ends are packed, so a truck is up to speed and straight when it meets the loose gravel
            for (int i = 0; i < h.Length; i++)
                if (gravel[i] > 0 && Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out int link, out _, out _) && (link == 0 || link == links.Count - 1)) packed[i] = 100;
        }
        if (id == PaintMarked || id == PaintPlain)
        {
            // paved and rolled, and no lines on it
            for (int i = 0; i < h.Length; i++)
                if (gravel[i] > 0) top[i] = Rolled;
        }
        if (id == Paved)
        {
            // the example: paved, rolled and painted from end to end
            for (int i = 0; i < h.Length; i++)
                if (gravel[i] > 0) top[i] = Painted;
        }
        if (IsQuarry)
        {
            // the service road starts finished, so the gravel truck can drive it
            for (int i = 0; i < h.Length; i++)
            {
                if (!Section(origin.x + i % w * Cell, origin.z + i / w * Cell, out int link, out _, out float side) || Mathf.Abs(side) > lane || links[link].z != 1) continue;
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
        for (int i = 0; i + 1 < height.Length; i++) links.Add(new Vector3Int(i, i + 1, 0));
    }

    // A junction: a stake at x, z with a rope out to a stake at each of these bearings (degrees
    // from +z, clockwise), all level.
    void Spokes(float x, float z, float length, params float[] bearing)
    {
        stakes.Add(new Vector3(x, BaseHeight, z));
        for (int i = 0; i < bearing.Length; i++)
        {
            float radians = bearing[i] * Mathf.Deg2Rad;
            stakes.Add(new Vector3(x + Mathf.Sin(radians) * length, BaseHeight, z + Mathf.Cos(radians) * length));
            links.Add(new Vector3Int(0, i + 1, 0));
        }
    }

    // A rough, roughly level field with no line, falling away to the yard at its edges.
    void Field(Vector3 corner, float width, float depth, float rough, float ox, float oz, float height = BaseHeight, float fall = Margin)
    {
        origin = corner;
        w = Mathf.CeilToInt(width / Cell) + 1;
        d = Mathf.CeilToInt(depth / Cell) + 1;
        h = new float[w * d];
        for (int i = 0; i < h.Length; i++)
        {
            float u = i % w * Cell, v = i / w * Cell;
            float edge = Mathf.Min(Mathf.Min(u, width - u), Mathf.Min(v, depth - v));
            h[i] = Mathf.Max(0, (height + Noise(origin.x + u, origin.z + v, ox, oz) * rough) * Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / fall)));
        }
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
        for (int i = 0; i + 1 < stakes.Count; i++) links.Add(new Vector3Int(i, i + 1, 0));
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
        float length = Mathf.Round(map == 1 ? 60f : map == 2 ? t.mapDistance : map == 3 ? 300f : map == Switchback ? 150f : 160f);
        float width = map == 1 ? 70f : map == 4 ? 130f : map == Switchback ? 110f : 100f;
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
            case Switchback:
                {
                    // switchback: two banks across the whole map, each 6 m high and too steep
                    // to rope, with one gentle way up apiece: on the left for the first and on
                    // the right for the second. Rocks stand along the rest of each bank (see
                    // PlaceRocks), so the road has to swing from one side to the other.
                    float y = 3f + swell * 0.4f;
                    for (int tier = 0; tier < 2; tier++)
                    {
                        float z0 = BankStart(tier, length), run = RampRun, middle = z0 + run * 0.5f;
                        float gentle = Mathf.Clamp01((z - z0) / run), steep = Mathf.SmoothStep(0, 1, (z - (middle - 5f)) / 10f);
                        float way = 1f - Mathf.SmoothStep(0, 1, (Mathf.Abs(x - WayUp(tier)) - 8f) / 7f);
                        y += TierRise * Mathf.Lerp(steep, gentle, way);
                    }
                    return y;
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

    // the switchback map: where each bank's way up begins, how long it is, and which side it is on
    static float BankStart(int tier, float length) { return length * (tier == 0 ? 0.2f : 0.63f); }
    static float RampRun => TierRise / Mathf.Tan(RampDegrees * Mathf.Deg2Rad);
    static float WayUp(int tier) { return tier == 0 ? -22f : 22f; }

    // The switchback map's rocks: a row along each bank, from edge to edge, leaving only the
    // way up. They are worked out from the map alone, the same on every machine, and nothing
    // moves them.
    void PlaceRocks(Transform root, float length)
    {
        var stone = Mats.Make(new Color(0.36f, 0.35f, 0.38f), true);
        for (int tier = 0; tier < 2; tier++)
        {
            float middle = BankStart(tier, length) + RampRun * 0.5f;
            int n = 0;
            for (float x = -48f; x <= 48f; x += 6f, n++)
            {
                if (Mathf.Abs(x - WayUp(tier)) < 14f) continue;
                float radius = 2.4f + (n * 5 + tier) % 4 * 0.4f, z = middle + ((n * 7 + tier * 3) % 5 - 2) * 1.2f;
                rocks.Add(new Vector3(x, radius, z));
                var rock = Mats.Part(root, Mats.Sphere, stone, new Vector3(x, HeightAt(x, z) + radius * 0.3f, z), new Vector3(radius * 2f, radius * 1.7f, radius * 2f));
                rock.localRotation = Quaternion.Euler(n * 37f, n * 71f, n * 13f);
                rock.gameObject.AddComponent<SphereCollider>();
            }
        }
    }

    // is there a rock within `clear` metres of the line from a to b?
    bool RockNear(Vector3 a, Vector3 b, float clear)
    {
        foreach (var rock in rocks)
        {
            Vector3 p = new Vector3(rock.x, 0, rock.z), ab = b - a;
            float t = ab.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
            if (Vector3.Distance(p, a + ab * t) < rock.y + clear) return true;
        }
        return false;
    }

    // Test tooling, host only: one click here with this tool, with no cap on how fast.
    public void TestClick(float x, float z, int tool)
    {
        for (int slot = 0; slot < hostNextClick.Length; slot++) hostNextClick[slot] = 0;
        HostClick(0, x, z, false, tool);
    }

    // Test tooling, host only: a road through these points from the first town to the second,
    // whatever the rules for ropes say.
    public void TestRoad(List<Vector3> between)
    {
        if (!Ready || !IsLand) return;
        stakes.RemoveRange(fixedStakes, stakes.Count - fixedStakes);
        links.Clear();
        int last = 0;
        foreach (var p in between)
        {
            stakes.Add(p);
            links.Add(new Vector3Int(last, stakes.Count - 1, 0));
            last = stakes.Count - 1;
        }
        links.Add(new Vector3Int(last, 1, 0));
        Net.ToClients(StakesMsg(255, -1), true);
        StakesChanged(255, -1);
    }

    // Test tooling, host only, and clients are not told: every staked point on its line, and
    // the road bare (0), under loose gravel (1) or packed (2).
    public void TestFinish(int surface)
    {
        if (!Ready) return;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] == 0) continue;
            h[i] = target[i];
            gravel[i] = (byte)(zone[i] == 1 && surface >= 1 ? FullGravel : 0);
            packed[i] = (byte)(zone[i] == 1 && surface == 2 ? 100 : 0);
        }
        foreach (var chunk in chunks) chunk.dirty = true;
        Upload(true);
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

    // where players start at the quarry: up on top, by the road that needs the gravel
    public Vector3 QuarrySpawn(int slot)
    {
        float x = origin.x + 30f + slot * 1.3f, z = origin.z + 47f;
        return new Vector3(x, Ready ? HeightAt(x, z) + 0.2f : 12f, z);
    }

    // where players start on the map: beside the first town's stake, looking at the other town
    public Vector3 SpawnAt(int slot)
    {
        if (!Ready || stakes.Count == 0) return new Vector3(0, 5f, 0);
        Vector3 at = stakes[0] + new Vector3(-2f - slot * 1.3f, 0, -6f);    // the stake and its label are ahead and a little to the right, clear of the readout
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
        joined = PathBetween(0, 1, true);
        // every rope that can be reached from the first town, branches and all
        var reached = new bool[stakes.Count];
        reached[0] = true;
        for (bool more = true; more;)
        {
            more = false;
            foreach (var l in links)
                if (reached[l.x] != reached[l.y]) { reached[l.x] = reached[l.y] = true; more = true; }
        }
        for (int k = 0; k < links.Count; k++) if (reached[links[k].x]) roadLength += segs[k].len;
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
            var m = Msg.New(Op.PlotPoints, n * 11 + 16);
            m.U8((byte)id);
            m.U16((ushort)n);
            for (int k = start; k < start + n; k++)
            {
                int i = points[k];
                m.U32((uint)i);
                m.F32(h[i]);
                m.U8(gravel[i]);
                m.U8(packed[i]);
                m.U8(top[i]);
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
            top[i] = m.U8();
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
            m = Msg.New(Op.PlotRows, n * 5 + 16);
            m.U8((byte)id);
            m.U32((uint)start);
            m.U16((ushort)n);
            for (int i = start; i < start + n; i++)
            {
                m.U16((ushort)Mathf.Clamp(Mathf.RoundToInt(h[i] * 1000f), 0, 65535));
                m.U8(gravel[i]);
                m.U8(packed[i]);
                m.U8(top[i]);
            }
            Out(client, everyone, m);
        }
        Out(client, everyone, StakesMsg(255, -1));
        if (lines != null && !everyone) lines.SendState(client);
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
        top = new byte[h.Length];
        packed = new byte[h.Length];
        health = new byte[h.Length];
        expected = h.Length;
        built = "from host";
        if (IsQuarry) fixedStakes = 2;
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
        for (int i = start; i < start + n; i++) { h[i] = m.U16() / 1000f; gravel[i] = m.U8(); packed[i] = m.U8(); top[i] = m.U8(); }
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
        blue = Mats.Make(new Color(0.20f, 0.45f, 0.95f));
        red = Mats.Make(new Color(0.90f, 0.15f, 0.12f));
        white = Mats.Make(Color.white);
        orange = Mats.Make(new Color(0.95f, 0.5f, 0.1f));
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
            case SpinOut: text = "Loose gravel round a bend, packed at each end.\nTrucks slide on it, and pack it as they go (truckPacking on F1).\nThe host's button above makes it loose again."; at = stakes[1] + Vector3.up * 2.2f; break;
            case PaintMarked: text = "Rolled asphalt, with the place for each line marked.\n8: the roller brush. Hold left click and drag for white, right click for yellow.\nT: tar spray, to cover paint. G: the grinder, to take it off.\nE by the paint truck drives it: left click its white nozzle, right click its yellow."; at = stakes[0] + new Vector3(0, 2.4f, 6f); break;
            case PaintPlain: text = "The same strip with nothing marked.\nA white line inside each edge, and a yellow one down the middle."; at = stakes[0] + new Vector3(0, 2.4f, 6f); break;
            case WearDirt: text = "A dirt road: bare ground on its line, under steady traffic.\nThe readout (F3) counts the trucks. Grade it (2) to mend it."; at = stakes[0] + new Vector3(0, 2.2f, 4f); break;
            case WearGravel: text = "A gravel road, packed, under the same traffic.\nGrade (2) and gravel (3) mend it."; at = stakes[0] + new Vector3(0, 2.2f, 4f); break;
            case 6: text = "Hairpin: stakes set round the tightest turn allowed\nlevel it and gravel it; trucks try it as it is"; break;
            case 8: text = "A 14 degree climb, bare ground on its line.\nToo steep for a truck without gravel."; break;
            case 9: text = "The same 14 degree climb under loose gravel."; break;
            case 10: text = "A 20 degree climb under loose gravel.\nToo steep until the gravel is packed."; break;
            case 11: text = "The same 20 degree climb, packed."; break;
            case 12: text = "A junction: three ropes at one stake.\nTrucks drive from any end to any other."; at = stakes[0] + Vector3.up * 2.2f; break;
            case 13: text = "A crossroads: four ropes at one stake."; at = stakes[0] + Vector3.up * 2.2f; break;
            case 14: text = "A fork: two branches 60 degrees apart, the closest allowed."; at = stakes[0] + Vector3.up * 2.2f; break;
            case Quarry:
                {
                    text = "The quarry. Press 3. Click the rock to load your shovel,\nthen click the truck to fling it in. It sets off for the drop-off when it is full.";
                    at = new Vector3(PitCentre.x, stakes[0].y + 7f, PitCentre.z);
                    labels.Add(new Yard.Label());       // the drop-off, the heap, and the truck: written each frame
                    labels.Add(new Yard.Label());
                    labels.Add(new Yard.Label());
                    depots.Add(0);
                    quarryRoot = new GameObject("Quarry").transform;
                    quarryRoot.SetParent(transform, false);
                    var stone = Mats.Make(new Color(0.36f, 0.35f, 0.38f), true);
                    for (int n = 0; n < 9; n++)
                    {
                        float radius = 2.2f + n % 3 * 0.8f;
                        Vector3 centre = new Vector3(PitCentre.x + 1f + n / 3 * 3f, stakes[0].y + radius * 0.4f + n % 2 * 1.2f - 0.6f, PitCentre.z + (n % 3 - 1) * 4.5f);
                        var rock = Mats.Part(quarryRoot, Mats.Sphere, stone, centre, new Vector3(radius * 2f, radius * 1.8f, radius * 2f));
                        rock.localRotation = Quaternion.Euler(n * 37f, n * 71f, n * 13f);
                        rock.gameObject.AddComponent<SphereCollider>();
                    }
                    pile = Mats.Part(quarryRoot, Mats.Sphere, Mats.Make(GravelLoose, true), Vector3.zero, Vector3.one);
                    pile.gameObject.AddComponent<SphereCollider>();
                    pile.gameObject.SetActive(false);
                    break;
                }
            case Paving:
                text = "A road to pave. The dump truck comes by itself and tips asphalt down each lane.\nLevel it: press 5 and hold left click with the shovel, or right click the roller to send it over.\nThe roller also rolls it. Then press 6 and hold left click to paint the lines.";
                at = stakes[1] + Vector3.up * 3f;
                depots.Add(0);
                depots.Add(3);
                labels.Add(new Yard.Label());       // the dump truck and the roller: written each frame
                labels.Add(new Yard.Label());
                break;
            case Paved: text = "The same road paved, rolled and painted.\nTrucks drive faster on it."; break;
            case DriveRoad: text = "A road in four states, from this end: bare, loose gravel, spread asphalt, rolled asphalt.\nThe roller packs gravel and rolls asphalt. The loader's gravel can be tipped on the bare part.\nThe paint truck, the paint tool (6) and the brush (8) paint rolled asphalt."; break;
            case DriveField: text = "Rough ground to drive over."; at = new Vector3(origin.x + 6f, 3f, origin.z + 8f); break;
            case 15: text = "A junction to build. Press 1, click the middle stake,\nthen click the ground to one side for a branch.\nA stake here takes up to four ropes."; at = stakes[1] + Vector3.up * 2.2f; break;
            case Land:
                {
                    text = "Town A. Press 1, click this stake, then click the ground\ntoward the pole at town B to put stakes down.\nRope the last one to town B's stake and trucks set off.";
                    at = stakes[0] + Vector3.up * 2.2f;
                    labels.Add(new Yard.Label { at = stakes[1] + Vector3.up * 2.2f, text = "Town B. Rope the road to this stake." });
                    var towns = new GameObject("Towns").transform;
                    towns.SetParent(transform, false);
                    Town(towns, stakes[0], -1f, new Color(0.85f, 0.35f, 0.3f));
                    Town(towns, stakes[1], 1f, new Color(0.3f, 0.5f, 0.85f));
                    if (Game.I.map == Switchback) PlaceRocks(towns, stakes[1].z);
                    // the plain the land stands on, so nobody falls for ever off its edge
                    var plain = Mats.Part(towns, Mats.Cube, Mats.Make(new Color(0.33f, 0.37f, 0.31f)), new Vector3(origin.x + SizeX * 0.5f, -0.5f, origin.z + SizeZ * 0.5f), new Vector3(2000f, 1f, 2000f));
                    plain.gameObject.AddComponent<BoxCollider>();
                    Game.I.respawn = true;      // everyone starts again at the first town
                    break;
                }
            default: text = "Hairpin: the same turn, finished"; break;
        }
        labels.Add(new Yard.Label { at = at, text = text });
        if (id == PaintMarked || id == PaintPlain)
        {
            lines = new GameObject("Lines").AddComponent<Lines>();
            lines.transform.SetParent(transform, false);
            lines.Build(this, id == PaintMarked);
        }
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
    float Surface(int i) { return h[i] + gravel[i] * 0.001f * (1f - 0.25f * packed[i] * 0.01f) + Asphalt(top[i]); }

    // how thick the asphalt lies: in a ridge where it was dumped, 6 cm spread, 4 cm rolled
    static float Asphalt(int on) { return on == 0 ? 0 : on < Spread ? 0.22f : 0.06f - 0.02f * Mathf.Clamp01((on - Spread) / 100f); }

    void Fill(Chunk chunk)
    {
        float full = FullGravel;
        int stride = chunk.cols + 1;
        for (int k = 0; k < chunk.verts.Length; k++)
        {
            int i = (chunk.row0 + k / stride) * w + chunk.col0 + k % stride;
            chunk.verts[k] = new Vector3(i % w * Cell, Surface(i), i / w * Cell) + origin;
            bool level = zone[i] != 0 && Mathf.Abs(h[i] - target[i]) < Level;
            bool service = zone[i] != 0 && linkOf[i] < segs.Length && segs[linkOf[i]].role == 1;
            Color32 color = zone[i] == 0 ? (IsLand ? Grass : Outside)
                : zone[i] == 1 ? (level ? (service ? ServiceDone : RoadDone) : (service ? ServiceRough : RoadRough))
                : (level ? (service ? ServiceShoulderDone : ShoulderDone) : (service ? ServiceShoulderRough : ShoulderRough));
            // gravel greys the ground as it deepens, and darkens as it is packed
            if (gravel[i] > 0) color = Color32.Lerp(color, Color32.Lerp(GravelLoose, GravelPacked, packed[i] * 0.01f), Mathf.Clamp01(gravel[i] / full));
            // asphalt is brown-black as it is dumped and spread, and blacker as it is rolled
            if (top[i] > 0) color = Color32.Lerp(AsphaltLoose, AsphaltRolled, Mathf.Clamp01((top[i] - Spread) / 100f));
            if (top[i] == DaubWhite) color = LineWhite;
            else if (top[i] == DaubYellow) color = LineYellow;
            if (top[i] == Painted && linkOf[i] < segs.Length)
            {
                // the lines: a broken yellow one down the middle, a white one inside each edge
                float off = Mathf.Abs(across[i]), run = along[i] * segs[linkOf[i]].len;
                if (off <= 0.13f && run % 6f < 3f) color = LineYellow;
                else if (off >= lane - 0.55f && off <= lane - 0.2f) color = LineWhite;
            }
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
        int road = 0, roadLevel = 0, shoulder = 0, shoulderLevel = 0, gravelled = 0, done = 0, full = FullGravel, cut = 0;
        deepest = 0;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] == 0) continue;
            bool level = Mathf.Abs(h[i] - target[i]) < Level;
            // ground more than 2 cm under its line is a rut or a hole (or was never brought up to it)
            if (zone[i] == 1 && target[i] - h[i] > 0.02f) { cut++; deepest = Mathf.Max(deepest, target[i] - h[i]); }
            if (zone[i] == 1 && gravel[i] >= full) { gravelled++; if (packed[i] >= 100) done++; }
            if (zone[i] == 1) { road++; if (level) roadLevel++; }
            else { shoulder++; if (level) shoulderLevel++; }
        }
        roadShare = road > 0 ? (float)roadLevel / road : 0;
        shoulderShare = shoulder > 0 ? (float)shoulderLevel / shoulder : 0;
        gravelShare = road > 0 ? (float)gravelled / road : 0;
        packedShare = road > 0 ? (float)done / road : 0;
        rutShare = road > 0 ? (float)cut / road : 0;
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
        for (int i = 0; i < h.Length; i++)
        {
            hash = (hash ^ (uint)(Mathf.RoundToInt(h[i] * 1000f) + gravel[i] * 100000 + packed[i] * 30000000)) * 16777619;
            if (top[i] != 0) hash = (hash ^ top[i]) * 16777619;
        }
        foreach (var s in stakes) hash = (hash ^ (uint)Mathf.RoundToInt((s.x + s.y * 7f + s.z * 13f) * 1000f)) * 16777619;
        foreach (var l in links) hash = (hash ^ (uint)(l.x * 100 + l.y + l.z * 100000)) * 16777619;
        if (lines != null) hash = (hash ^ lines.Hash()) * 16777619;
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
            var seg = new Seg { a = Flat(a), yA = a.y, yB = b.y, role = links[k].z };
            seg.d = Flat(b - a);
            seg.len = seg.d.magnitude;
            seg.d /= Mathf.Max(seg.len, 0.001f);
            seg.r = new Vector3(seg.d.z, 0, -seg.d.x);
            // Where three or more ropes meet there is no one bend to halve. Each section runs
            // on past the stake instead, so that between them they cover the whole junction (a
            // point belongs to the nearest centre line), and each stays level at the stake's
            // height until it is clear of the others.
            int before = Beyond(links[k].x, k), after = Beyond(links[k].y, k);
            if (LinksAt(links[k].x) >= 3) { seg.eA = Reach; seg.pA = Mathf.Min(Reach, seg.len * 0.4f); }
            else if (before >= 0) seg.kA = Slant(seg, Flat(a - stakes[before]).normalized + seg.d);
            if (LinksAt(links[k].y) >= 3) { seg.eB = Reach; seg.pB = Mathf.Min(Reach, seg.len * 0.4f); }
            else if (after >= 0) seg.kB = Slant(seg, seg.d + Flat(stakes[after] - b).normalized);
            segs[k] = seg;
        }
    }

    // the slant of a cut whose line is square to `through`
    static float Slant(Seg seg, Vector3 through)
    {
        float forward = Vector3.Dot(seg.d, through);
        return forward < 0.2f ? 0 : -Vector3.Dot(seg.r, through) / forward;     // a bend sharper than about 160 degrees is left square
    }

    // Where a section begins and ends, in metres along it from its first stake, at `off` metres
    // right of its centre line: s0 to s1 is the ground it covers, and t0 to t1 is where its line
    // runs from one height to the other. They differ only at a junction.
    static void Span(Seg seg, float off, out float s0, out float s1, out float t0, out float t1)
    {
        s0 = seg.eA > 0 ? -seg.eA : off * seg.kA;
        s1 = seg.eB > 0 ? seg.len + seg.eB : seg.len + off * seg.kB;
        t0 = seg.eA > 0 ? seg.pA : s0;
        t1 = seg.eB > 0 ? seg.len - seg.pB : s1;
    }

    // Which section covers a spot, and where in it: t from 0 at its first end to 1 at its second,
    // side in metres right of the centre line. Where two overlap, the nearer centre line wins.
    // Sections tied to the stakes `notA` and `notB` are left out.
    bool Section(float x, float z, out int link, out float t, out float side, int notA = -1, int notB = -1)
    {
        link = -1; t = side = 0;
        float best = float.MaxValue, reach = Reach;
        for (int k = 0; k < segs.Length; k++)
        {
            if (notA >= 0 && (links[k].x == notA || links[k].y == notA || links[k].x == notB || links[k].y == notB)) continue;
            var seg = segs[k];
            float px = x - seg.a.x, pz = z - seg.a.z;
            float s = px * seg.d.x + pz * seg.d.z, off = px * seg.d.z - pz * seg.d.x;
            if (Mathf.Abs(off) > reach + 0.001f || Mathf.Abs(off) >= best) continue;
            Span(seg, off, out float s0, out float s1, out float t0, out float t1);
            if (s1 - s0 < 0.05f || s < s0 - 0.001f || s > s1 + 0.001f) continue;
            best = Mathf.Abs(off);
            link = k; side = off;
            t = Mathf.Clamp01((s - t0) / Mathf.Max(0.01f, t1 - t0));
        }
        return link >= 0;
    }

    // a place in a section, on the ground
    Vector3 World(int link, float t, float side)
    {
        var seg = segs[link];
        Span(seg, side, out _, out _, out float t0, out float t1);
        return seg.a + seg.d * (t0 + t * (t1 - t0)) + seg.r * side;
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
            Span(seg, edge, out float s0, out float s1, out _, out _);
            Vector3 p = seg.a + seg.d * (corner % 2 == 0 ? s0 : s1) + seg.r * edge;
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
        return a.a == b.a && a.d == b.d && a.len == b.len && a.yA == b.yA && a.yB == b.yB && a.kA == b.kA && a.kB == b.kB
            && a.eA == b.eA && a.eB == b.eB && a.pA == b.pA && a.pB == b.pB && a.role == b.role;
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
            float thick = i < fixedStakes || i == drop ? 0.16f : 0.08f;
            var post = Mats.Part(stakeRoot, Mats.Cube, i == selected ? white : i == drop ? orange : wood, s + Vector3.down * 0.6f, new Vector3(thick, 2.8f, thick));
            // the drop-off's stake carries a board, to find it by
            if (i == drop) Mats.Part(stakeRoot, Mats.Cube, orange, s + Vector3.up * 1.1f, new Vector3(1.2f, 0.7f, 0.06f));
            var grab = post.gameObject.AddComponent<BoxCollider>();
            grab.isTrigger = true;                      // walked through, but the crosshair finds it
            grab.size = new Vector3(0.4f / thick, 1f, 0.4f / thick);    // 0.4 m across
            stakeByCollider[grab.GetInstanceID()] = i;
        }
        foreach (var l in links)
        {
            Vector3 a = stakes[l.x], b = stakes[l.y];
            var line = Mats.Part(stakeRoot, Mats.Cube, l.z == 1 ? blue : red, (a + b) * 0.5f, new Vector3(0.03f, 0.03f, (b - a).magnitude));
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
        if (distance < t.minStakeSpacing - 0.01f) return No("too close to the last stake");
        if (distance > t.sectionLength + 0.01f) return No("too far: a rope is " + t.sectionLength.ToString("0") + " m at most");
        int most = Junctions ? Mathf.RoundToInt(t.ropesPerStake) : 2;
        if (LinksAt(from) >= most || (toStake >= 0 && LinksAt(toStake) >= most)) return No("a stake takes " + most + " ropes, and that one has them");
        if (!BendAllowed(from, b) || (toStake >= 0 && !BendAllowed(toStake, a))) return false;
        if (TooSteep(stakes[from].y, to.y, distance)) return No("too steep for a truck: " + t.maxSlope.ToString("0") + " degrees at most");
        if (RockNear(a, b, Reach)) return No("a rock is in the way");
        return Crosses(a, b, from, toStake) ? No("it would run over road already staked") : true;
    }

    // would a rope between these two heights, this far apart on the flat, be too steep to drive?
    static bool TooSteep(float yA, float yB, float distance)
    {
        return Mathf.Abs(yA - yB) > Mathf.Tan(Game.I.tuning.maxSlope * Mathf.Deg2Rad) * distance + 0.005f;
    }

    // would a rope from this stake to there turn the road too sharply at the stake?
    // A stake with one rope: the second carries the road on, and may not bend it too sharply.
    // A stake with two or more: another rope is a branch, and must leave well clear of each.
    bool BendAllowed(int stake, Vector3 to)
    {
        var t = Game.I.tuning;
        int ropes = LinksAt(stake);
        if (ropes == 0) return true;
        Vector3 here = Flat(stakes[stake]);
        if (ropes == 1)
            return Vector3.Angle(here - Flat(stakes[Beyond(stake, -1)]), to - here) <= t.maxBend + 0.01f ? true : No("too sharp a bend: " + t.maxBend.ToString("0") + " degrees at most");
        foreach (var l in links)
        {
            if (l.x != stake && l.y != stake) continue;
            if (Vector3.Angle(Flat(stakes[l.x == stake ? l.y : l.x]) - here, to - here) < t.junctionAngle - 0.01f)
                return No("a branch must leave " + t.junctionAngle.ToString("0") + " degrees or more from the other ropes");
        }
        return true;
    }

    // Would a section from a to b run over road that is already staked out? The sections tied
    // to its own two stakes do not count: it meets those there, at a bend or a junction.
    bool Crosses(Vector3 a, Vector3 b, int from, int toStake)
    {
        float length = Vector3.Distance(a, b);
        for (float s = 1f; s <= length - 1f; s += 0.5f)
        {
            Vector3 p = Vector3.Lerp(a, b, s / length);
            if (Section(p.x, p.z, out _, out _, out _, from, toStake)) return true;
        }
        return false;
    }

    // May a new stake go in at x,z, roped to stake `from` if there is one?
    bool CanAdd(float x, float z, int from)
    {
        if (stakes.Count >= MaxStakes) return No("no stakes left");
        if (!Inside(x, z, IsLand ? EdgeMargin : Margin * 0.5f)) return No("too near the edge");
        if (Section(x, z, out _, out _, out _)) return No("road is already staked here");
        var stake = new Vector3(x, 0, z);
        if (RockNear(stake, stake, 0.5f)) return No("a rock is in the way");
        foreach (var other in stakes)
            if (Vector3.Distance(Flat(other), stake) < Game.I.tuning.minStakeSpacing) return No("too close to another stake");
        stake.y = HeightAt(x, z);       // where it would stand, for the rope's slope
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
            links.Add(new Vector3Int(from, to, 0));
            made = to;
        }
        else
        {
            if (!CanAdd(x, z, from)) return false;
            stakes.Add(new Vector3(x, HeightAt(x, z), z));
            made = stakes.Count - 1;
            if (from >= 0) links.Add(new Vector3Int(from, made, 0));
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
            float was = s.y;
            s.y = Mathf.Clamp(s.y + steps * Game.I.tuning.heightPerClick, 0.1f, 60f);
            // not if it would make a rope too steep, or one that already is steeper still
            foreach (var l in links)
            {
                if (l.x != stake && l.y != stake) continue;
                Vector3 other = stakes[l.x == stake ? l.y : l.x];
                if (TooSteep(s.y, other.y, Vector3.Distance(Flat(s), Flat(other))) && Mathf.Abs(s.y - other.y) > Mathf.Abs(was - other.y)) return false;
            }
            stakes[stake] = s;
        }
        else PullOut(stake);
        Net.ToClients(StakesMsg(255, -1), true);
        StakesChanged(255, -1);
        return true;
    }

    // host: a stake comes out, with every rope tied to it, and the stakes after it are renumbered
    void PullOut(int stake)
    {
        stakes.RemoveAt(stake);
        for (int k = links.Count - 1; k >= 0; k--)
        {
            var l = links[k];
            if (l.x == stake || l.y == stake) { links.RemoveAt(k); continue; }
            links[k] = new Vector3Int(l.x > stake ? l.x - 1 : l.x, l.y > stake ? l.y - 1 : l.y, l.z);
        }
        if (drop == stake) drop = -1;
        else if (drop > stake) drop--;
    }

    // ---- the gravel drop-off, and the gravel truck's round
    //
    // The drop-off is a stake a player puts down with the survey tool, on a short spur roped
    // to a stake that is already in the middle of a road. The gravel truck drives from the
    // quarry to that stake by any road, on past it, and backs into the spur; it stands there
    // until it has been unloaded, and drives home.

    // May the drop-off go at x, z, roped to stake `from`?
    bool DropAllowed(float x, float z, int from)
    {
        if (!IsQuarry) return No("the drop-off tool works on the Quarry ground");
        if (Game.I.lorries.StandingAt(Lorries.GravelTruck) != 0) return No("the gravel truck is out: wait until it is back at the quarry");
        if (from < 0 || from >= stakes.Count) return No("first left click a stake in the middle of a road");
        if (LinksAt(from) < 2) return No("start from a stake in the middle of a road: the truck drives on past it and backs in");
        return CanAdd(x, z, from);
    }

    // host: the drop-off goes in at x, z, on a service road roped to stake `from`. There is one
    // drop-off: the old one's stake comes out, unless something else is tied to it.
    public bool HostDrop(int slot, float x, float z, int from)
    {
        if (!Ready || !DropAllowed(x, z, from)) return false;
        if (drop >= 0)
        {
            int old = drop;
            drop = -1;
            if (old >= fixedStakes && old != from && LinksAt(old) <= 1) { PullOut(old); if (from > old) from--; }
        }
        stakes.Add(new Vector3(x, HeightAt(x, z), z));
        drop = stakes.Count - 1;
        links.Add(new Vector3Int(from, drop, 1));
        Net.ToClients(StakesMsg(slot, drop), true);
        StakesChanged(slot, drop);
        return true;
    }

    public void RequestDrop(float x, float z, int from)
    {
        if (Net.IsHost) { HostDrop(Game.I.localSlot, x, z, from); return; }
        var m = Msg.New(Op.Drop, 16);
        m.U8((byte)id);
        m.F32(x);
        m.F32(z);
        m.U8((byte)(from < 0 ? 255 : from));
        Net.ToHost(m, true);
    }

    // The survey tool for the drop-off. Left click a stake in the middle of a road to start
    // from it, then left click the ground beside the road: the drop-off goes there.
    void DropSurvey(Tuning tuning, Mouse mouse, Transform eye)
    {
        if (!IsQuarry) return;
        if (selected >= 0 && mouse.rightButton.wasPressedThisFrame) { Choose(-1); return; }
        if (Physics.Raycast(eye.position, eye.forward, out var first, tuning.clickReach, ~0, QueryTriggerInteraction.Collide)
            && first.collider.transform.parent == stakeRoot && stakeByCollider.TryGetValue(first.collider.GetInstanceID(), out int found))
        {
            Say(LinksAt(found) >= 2 ? "left click: the drop-off will branch from this stake" : "this stake is at the end of a road: pick one in the middle, so the truck can drive past and back in", LinksAt(found) < 2);
            if (mouse.leftButton.wasPressedThisFrame && LinksAt(found) >= 2) Choose(found);
            return;
        }
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.clickReach, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        if (selected < 0) { Say("the drop-off survey: first left click a stake in the middle of a road", false); return; }
        Vector3 aim = hit.point;
        bool allowed = DropAllowed(aim.x, aim.z, selected);
        var end = new Vector3(aim.x, HeightAt(aim.x, aim.z), aim.z);
        Preview(stakes[selected], end, allowed);
        if (allowed) Say("left click puts the gravel drop-off here. The truck will back in from the road", false);
        if (allowed && mouse.leftButton.wasPressedThisFrame) RequestDrop(aim.x, aim.z, selected);
    }

    int SpurOf(int stake)
    {
        for (int k = 0; k < links.Count; k++) if (links[k].x == stake || links[k].y == stake) return k;
        return -1;
    }

    // The gravel truck's way to the drop-off, worked out in `chain`: from the quarry to the
    // stake the spur branches from (the junction), and on along whichever other road leaves it
    // most nearly straight ahead. Gives the junction, and the spur as driven from the drop-off.
    bool HaulChain(out int junction, out int fromDrop)
    {
        junction = fromDrop = -1;
        if (!Ready || !IsQuarry || drop < 0 || drop >= stakes.Count || depots.Count == 0) return false;
        int spur = SpurOf(drop);
        if (spur < 0) return false;
        junction = links[spur].x == drop ? links[spur].y : links[spur].x;
        fromDrop = links[spur].x == drop ? spur : ~spur;
        if (!PathBetween(depots[0], junction)) return false;
        int last = chain[chain.Count - 1], came = last < 0 ? ~last : last;
        Vector3 arriving = last < 0 ? -segs[came].d : segs[came].d;
        int on = 0;
        float best = -2f;
        for (int k = 0; k < links.Count; k++)
        {
            if (k == spur || k == came || (links[k].x != junction && links[k].y != junction)) continue;
            float ahead = Vector3.Dot(links[k].x == junction ? segs[k].d : -segs[k].d, arriving);
            if (ahead <= best) continue;
            best = ahead;
            on = links[k].x == junction ? k : ~k;
        }
        if (best < -1.5f) return false;     // no road on past the junction
        chain.Add(on);
        return true;
    }

    // drop the end of a way that runs on further than the truck needs to go past the junction
    void HaulTrim(List<Vector3> path, int junction)
    {
        Vector3 at = Flat(stakes[junction]);
        float pass = Game.I.tuning.haulPass;
        while (path.Count > 8 && Vector3.Distance(Flat(path[path.Count - 1]), at) > pass) path.RemoveAt(path.Count - 1);
    }

    // the way out: from the quarry to a little past the junction
    public bool HaulOut(List<Vector3> path)
    {
        path.Clear();
        if (!HaulChain(out int junction, out _)) return false;
        Lane(path, false, 0, 0);
        HaulTrim(path, junction);
        return path.Count > 6;
    }

    // The way in: the points a truck leaving the drop-off for that same stretch of road would
    // drive, which the gravel truck follows backwards, tail first, from their end to their start.
    public bool HaulIn(List<Vector3> path)
    {
        path.Clear();
        if (!HaulChain(out int junction, out int fromDrop)) return false;
        int on = chain[chain.Count - 1];
        chain.Clear();
        chain.Add(fromDrop);
        chain.Add(on);
        Lane(path, false, 0, 0);
        HaulTrim(path, junction);
        return path.Count > 6;
    }

    // the way home: from the drop-off to the quarry, by any road
    public bool HaulHome(List<Vector3> path)
    {
        path.Clear();
        if (!Ready || !IsQuarry || drop < 0 || drop >= stakes.Count || !PathBetween(drop, depots[0])) return false;
        Lane(path, false, 0, 4f);
        return path.Count > 6;
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
        var m = Msg.New(Op.PlotStakes, 32 + stakes.Count * 12 + links.Count * 3);
        m.U8((byte)id);
        m.U8((byte)slot);                   // whose stake this was (255: nobody's), and which
        m.U8((byte)(made < 0 ? 255 : made));
        m.U8((byte)stakes.Count);
        foreach (var s in stakes) m.V3(s);
        m.U8((byte)links.Count);
        foreach (var l in links) { m.U8((byte)l.x); m.U8((byte)l.y); m.U8((byte)l.z); }
        m.U8((byte)(drop < 0 ? 255 : drop));
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
        for (int i = 0; i < n; i++) { int a = m.U8(), b = m.U8(), role = m.U8(); links.Add(new Vector3Int(a, b, role)); }
        drop = m.U8();
        if (drop == 255) drop = -1;
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
        int rows = Mathf.Max(1, Mathf.RoundToInt((segs[link].len - segs[link].pA - segs[link].pB) / tuning.patchWidth));
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
        // at a junction the end squares take in the level ground round the stake as well
        float radius = Mathf.Sqrt(halfT * halfT * segs[link].len * segs[link].len * 3f + halfSide * halfSide) + 1f
            + Mathf.Max(segs[link].eA + segs[link].pA, segs[link].eB + segs[link].pB);
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
        if (tool == Pave) return zone[i] == 1 && level && gravel[i] >= FullGravel && packed[i] >= 100 && top[i] < Spread;
        if (tool == Paint) return top[i] >= Rolled && top[i] < Painted;
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
        if (tool >= Brush)
        {
            // The brush is not work, it is drawing: no squares and no cap. Rolled asphalt
            // within a hand's width of where it points turns white, or yellow.
            changed.Clear();
            Box(x, z, 0.4f, out int bx0, out int bx1, out int bz0, out int bz1);
            byte daub = (byte)(tool == BrushYellow ? DaubYellow : DaubWhite);
            for (int iz = bz0; iz <= bz1; iz++)
                for (int ix = bx0; ix <= bx1; ix++)
                {
                    int i = iz * w + ix;
                    float dx = origin.x + ix * Cell - x, dz = origin.z + iz * Cell - z;
                    if (dx * dx + dz * dz > 0.1f || top[i] < Rolled || top[i] == daub) continue;
                    top[i] = daub;
                    changed.Add(i);
                }
            if (changed.Count > 0) Broadcast();
            return true;
        }
        // a little slack, so a client clicking exactly at the cap is not punished for network jitter
        if (Time.time < hostNextClick[slot]) return false;
        hostNextClick[slot] = Time.time + 0.8f / tuning.clicksPerSecond;

        bool round = tuning.brushRound >= 0.5f;
        changed.Clear();
        // at the quarry, gravel that goes down comes off the heap at the drop
        bool fromStock = IsQuarry && tool == Gravel && tuning.gravelFromStock >= 0.5f;
        canLay = !fromStock || stock > 0;
        laid = false;
        float mine = side;
        if (!round) Square(tuning, link, ref t, ref mine, out _, out _);
        Patch(tuning, link, t, side, x, z, round, hot ? tuning.hotSpotBonus : 1f, tool);
        // [Claude, maps only] a click on the hot spot also does one ordinary click on every
        // other square of its row, from shoulder to shoulder
        if (hot && IsLand && !round && tuning.hotSpotRow >= 0.5f)
        {
            float cellSide = lane / Mathf.Max(1, Mathf.RoundToInt(lane / tuning.patchWidth));
            int columns = Mathf.RoundToInt(lane * 2f / cellSide);
            for (int c = -1; c <= columns; c++)
            {
                // -1 and `columns` are the two shoulders
                if ((c < 0 || c == columns) && shoulderWidth <= 0) continue;
                float other = c < 0 ? -lane - shoulderWidth * 0.5f : c == columns ? lane + shoulderWidth * 0.5f : -lane + (c + 0.5f) * cellSide;
                if (Mathf.Abs(other - mine) > 0.01f) Patch(tuning, link, t, other, x, z, false, 1f, tool);
            }
        }
        if (fromStock && laid) stock--;
        clicks++;
        Broadcast();
        return true;
    }

    // host: one click's worth of work on the grid square at t, side of a section (or, for the
    // round brush, on a disc at x, z). Points it changes are added to `changed`.
    void Patch(Tuning tuning, int link, float t, float side, float x, float z, bool round, float strength, int tool)
    {
        float halfT = 0, halfSide = 0, radius = tuning.patchWidth * 0.5f;
        int x0, x1, z0, z1;
        if (round) Box(x, z, radius, out x0, out x1, out z0, out z1);
        else
        {
            Square(tuning, link, ref t, ref side, out halfT, out halfSide);
            Box(link, t, side, halfT, halfSide, out x0, out x1, out z0, out z1);
        }
        int full = FullGravel;
        // Asphalt is spread from where the dump truck left it: a square can be spread only if
        // there is asphalt somewhere in its row across the road, dumped or already spread.
        bool reachable = tool != Pave || round;
        if (!reachable)
        {
            Box(link, t, 0, halfT, Reach, out int r0, out int r1, out int q0, out int q1);
            for (int iz = q0; iz <= q1 && !reachable; iz++)
                for (int ix = r0; ix <= r1; ix++)
                {
                    int i = iz * w + ix;
                    if (top[i] > 0 && zone[i] == 1 && linkOf[i] == link && Mathf.Abs(along[i] - t) <= halfT * 1.02f) { reachable = true; break; }
                }
            if (!reachable) return;
        }
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
                float weight = strength * Mathf.Lerp(1f, 1f - Mathf.SmoothStep(0, 1, distance), tuning.brushSoftEdge);
                if (tool == Grade)
                {
                    float next = Mathf.MoveTowards(h[i], target[i], tuning.heightPerClick * weight);
                    // short of the line, ground sits on whole millimetres, which is what a late joiner is sent
                    float whole = Mathf.Round(next * 1000f) / 1000f;
                    if (next != target[i] && whole != h[i]) next = whole;
                    if (next == h[i]) continue;
                    h[i] = next;
                }
                else if (tool == Pave)
                {
                    // asphalt: only on packed gravel. The ridge the truck left is raked out flat.
                    if (!Wants(i, Pave)) continue;
                    top[i] = (byte)Mathf.Min(Spread, top[i] + Mathf.Max(1, Mathf.RoundToInt(tuning.spreadPerClick * 100f * weight)));
                }
                else if (tool == Paint)
                {
                    if (!Wants(i, Paint)) continue;
                    top[i] = Painted;
                }
                else
                {
                    // gravel: only on the road, only where it is on its line. It goes down to full
                    // depth, and after that the same clicks pack it.
                    if (zone[i] != 1 || Mathf.Abs(h[i] - target[i]) >= Level) continue;
                    if (gravel[i] < full)
                    {
                        if (!canLay) continue;
                        gravel[i] = (byte)Mathf.Min(full, gravel[i] + Mathf.Max(1, Mathf.RoundToInt(tuning.gravelPerClick * 1000f * weight)));
                        laid = true;
                    }
                    else if (packed[i] < 100) packed[i] = (byte)Mathf.Min(100, packed[i] + Mathf.Max(1, Mathf.RoundToInt(tuning.compactPerClick * 100f * weight)));
                    else continue;
                }
                health[i] = 100;    // worked ground is sound again
                changed.Add(i);
            }
    }

    // host: the dump truck tips asphalt where it is, in a ridge down its lane, on packed gravel
    // that has none yet. Says whether any went down.
    public bool Dump(Vector3 at, Vector3 right, float half)
    {
        if (!Ready) return false;
        changed.Clear();
        Box(at.x, at.z, half + 0.6f, out int x0, out int x1, out int z0, out int z1);
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                float dx = origin.x + ix * Cell - at.x, dz = origin.z + iz * Cell - at.z;
                // a strip as wide as the truck and half a metre along
                float across_ = dx * right.x + dz * right.z, along_ = dx * right.z - dz * right.x;
                if (Mathf.Abs(across_) > half || Mathf.Abs(along_) > 0.5f || top[i] != 0 || !Wants(i, Pave)) continue;
                top[i] = Heaped;
                changed.Add(i);
            }
        if (changed.Count > 0) Broadcast();
        return changed.Count > 0;
    }

    public bool Covers(float x, float z) { return Ready && Inside(x, z, 0); }

    // host: a loader's bucket of gravel tipped here. Road that is on its line and short of
    // gravel within `radius` gets it, loose. Says whether any did.
    public bool Tip(Vector3 at, float radius)
    {
        if (!Ready) return false;
        changed.Clear();
        Box(at.x, at.z, radius, out int x0, out int x1, out int z0, out int z1);
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                float dx = origin.x + ix * Cell - at.x, dz = origin.z + iz * Cell - at.z;
                if (dx * dx + dz * dz > radius * radius || zone[i] != 1 || Mathf.Abs(h[i] - target[i]) >= Level || gravel[i] >= FullGravel) continue;
                gravel[i] = (byte)FullGravel;
                changed.Add(i);
            }
        if (changed.Count > 0) Broadcast();
        return changed.Count > 0;
    }

    // Is there packed gravel with no asphalt on it where the dump truck would tip: down the
    // middle of a lane, the width of the truck? The strips at a lane's edges are for the
    // shovel and the roller, and the truck does not come back for them.
    public bool NeedsAsphalt()
    {
        if (!Ready) return false;
        // The truck does not follow its lane to the centimetre, so slivers are always left;
        // it is worth a trip only when a tenth of what it could cover is still bare.
        float half = Game.I.tuning.truckWidth * 0.5f - 0.15f;
        int could = 0, bare = 0;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] != 1 || Mathf.Abs(Mathf.Abs(across[i]) - lane * 0.5f) > half) continue;
            could++;
            if (top[i] == 0 && Wants(i, Pave)) bare++;
        }
        return bare * 10 > could;
    }

    // host: the roller levels and rolls the asphalt in the grid squares under it. A square with
    // any asphalt in it, even just where the truck left it, is pressed out flat across the
    // whole square and rolled.
    public void Roll(List<Vector3> wheels)
    {
        var tuning = Game.I.tuning;
        if (!Ready) return;
        changed.Clear();
        int add = Mathf.Max(1, Mathf.RoundToInt(tuning.rollPerPass * 100f));
        foreach (var wheel in wheels)
        {
            if (!Section(wheel.x, wheel.z, out int link, out float t, out float side) || Mathf.Abs(side) > lane) continue;
            Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
            Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
            bool any = false;
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                {
                    int i = iz * w + ix;
                    if (top[i] > 0 && InSquare(i, link, t, side, halfT, halfSide)) any = true;
                }
            if (!any) continue;
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                {
                    int i = iz * w + ix;
                    if (top[i] >= Rolled || !InSquare(i, link, t, side, halfT, halfSide) || changed.Contains(i)) continue;
                    if (top[i] == 0 && !Wants(i, Pave)) continue;
                    top[i] = (byte)Mathf.Min(Rolled, Mathf.Max(top[i], Spread) + add);
                    changed.Add(i);
                }
        }
        if (changed.Count > 0) Broadcast();
    }

    // host: the paint truck paints the lines of the rolled squares under it
    public void PaintUnder(List<Vector3> wheels)
    {
        var tuning = Game.I.tuning;
        if (!Ready) return;
        changed.Clear();
        foreach (var wheel in wheels)
        {
            if (!Section(wheel.x, wheel.z, out int link, out float t, out float side) || Mathf.Abs(side) > lane) continue;
            Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
            Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                {
                    int i = iz * w + ix;
                    if (top[i] < Rolled || top[i] == Painted || !InSquare(i, link, t, side, halfT, halfSide)) continue;
                    top[i] = Painted;
                    changed.Add(i);
                }
        }
        if (changed.Count > 0) Broadcast();
    }

    // is the road here paved and rolled? Trucks go faster on it.
    public bool IsPaved(float x, float z)
    {
        if (!Ready || !Inside(x, z, 0.5f)) return false;
        return top[Mathf.RoundToInt((z - origin.z) / Cell) * w + Mathf.RoundToInt((x - origin.x) / Cell)] >= Rolled;
    }

    // [Claude, maps only] host: a truck packs the gravel under its wheels. Each wheel packs
    // the grid square it is on, where the gravel is at full depth, by tuning.truckPacking.
    public void Pack(List<Vector3> wheels)
    {
        var tuning = Game.I.tuning;
        if (!Ready || tuning.truckPacking <= 0) return;
        changed.Clear();
        int full = FullGravel, add = Mathf.Max(1, Mathf.RoundToInt(tuning.truckPacking * 100f));
        foreach (var wheel in wheels)
        {
            if (!Section(wheel.x, wheel.z, out int link, out float t, out float side) || Mathf.Abs(side) > lane) continue;
            Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
            Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
            for (int iz = z0; iz <= z1; iz++)
                for (int ix = x0; ix <= x1; ix++)
                {
                    int i = iz * w + ix;
                    if (zone[i] != 1 || gravel[i] < full || packed[i] >= 100 || !InSquare(i, link, t, side, halfT, halfSide) || changed.Contains(i)) continue;
                    packed[i] = (byte)Mathf.Min(100, packed[i] + add);
                    changed.Add(i);
                }
        }
        if (changed.Count > 0) Broadcast();
    }

    // For the test tooling: a spot where this tool still has work, looking on from the last one
    // found. With `layOnly`, gravel that is down but not packed does not count.
    int workAt;
    public bool NextWork(int tool, bool layOnly, out Vector3 spot)
    {
        spot = default;
        if (!Ready) return false;
        for (int n = 0; n < h.Length; n++)
        {
            int i = (workAt + n) % h.Length;
            if (zone[i] == 0 || (tool == Gravel && zone[i] != 1)) continue;
            if (tool == Gravel && layOnly ? Mathf.Abs(h[i] - target[i]) >= Level || gravel[i] >= FullGravel : !Wants(i, tool)) continue;
            workAt = i;
            spot = new Vector3(origin.x + i % w * Cell, 0, origin.z + i / w * Cell);
            return true;
        }
        return false;
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
            edit.U8(top[i]);
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
            top[i] = m.U8();
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

    // How loose the road is here, 0 to 1: gravel that has not been packed, and all the looser
    // the deeper it lies. Wheels slide sideways on it.
    public float Loose(float x, float z)
    {
        if (!Ready || !Inside(x, z, 0.5f)) return 0;
        int i = Mathf.RoundToInt((z - origin.z) / Cell) * w + Mathf.RoundToInt((x - origin.x) / Cell);
        if (zone[i] != 1 || gravel[i] == 0 || top[i] > 0) return 0;
        return (1f - packed[i] * 0.01f) * Mathf.Clamp01(gravel[i] / (float)FullGravel);
    }

    // host, the Spin-out ground: the gravel between the two packed ends is loose again, and at full depth
    public void HostLoosen()
    {
        if (!Ready || id != SpinOut) return;
        changed.Clear();
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] != 1 || linkOf[i] == 0 || linkOf[i] >= segs.Length - 1 || (packed[i] == 0 && gravel[i] == FullGravel)) continue;
            packed[i] = 0;
            gravel[i] = (byte)FullGravel;
            changed.Add(i);
            if (changed.Count >= 5000) { Broadcast(); changed.Clear(); }
        }
        if (changed.Count > 0) Broadcast();
    }

    // How well a truck's wheels bite here: 1 on packed gravel, less on loose gravel, least on bare road.
    public float Going(float x, float z)
    {
        if (!Ready || !Inside(x, z, 0.5f)) return 1f;
        int i = Mathf.RoundToInt((z - origin.z) / Cell) * w + Mathf.RoundToInt((x - origin.x) / Cell);
        if (zone[i] != 1) return 1f;    // the way up onto the road is not the road's fault
        if (gravel[i] < FullGravel) return 0.4f;
        return Mathf.Lerp(0.6f, 1f, packed[i] * 0.01f);
    }

    // The shortest way along the ropes from one stake to another, as the sections in the order
    // a truck meets them. Players put stakes down in any order and roads can branch, so the way
    // is searched for, not assumed. Fills `chain`.
    // With `townOnly`, service roads are not driven.
    bool PathBetween(int start, int end, bool townOnly = false)
    {
        chain.Clear();
        if (start < 0 || end < 0 || start >= stakes.Count || end >= stakes.Count || start == end) return false;
        const int Unseen = int.MinValue;
        var by = new int[stakes.Count];     // the section each stake was reached by
        for (int i = 0; i < by.Length; i++) by[i] = Unseen;
        var queue = new Queue<int>();
        queue.Enqueue(start);
        by[start] = int.MaxValue;
        while (queue.Count > 0 && by[end] == Unseen)
        {
            int at = queue.Dequeue();
            for (int k = 0; k < links.Count; k++)
            {
                int other, c;
                if (townOnly && links[k].z == 1) continue;
                if (links[k].x == at) { other = links[k].y; c = k; }
                else if (links[k].y == at) { other = links[k].x; c = ~k; }
                else continue;
                if (by[other] != Unseen) continue;
                by[other] = c;
                queue.Enqueue(other);
            }
        }
        if (by[end] == Unseen) return false;
        for (int at = end; at != start;)
        {
            int c = by[at];
            chain.Add(c);
            at = c < 0 ? links[~c].y : links[c].x;
        }
        chain.Reverse();
        return true;
    }

    // The truck's way along the road: up onto the first stake, along the right-hand lane of each
    // section in turn, and off past the last; or back the other way, in the other lane.
    public bool Route(List<Vector3> path, bool back)
    {
        path.Clear();
        if (!Ready) return false;
        // On the map the road runs from one town's stake to the other's, and there is no road
        // until the ropes join them. Anywhere else a truck drives from one end of the road (a
        // stake with a single rope) to another; where a junction gives it more than two ends to
        // choose from, it picks its two at random.
        int start = 0, end = 1;
        if (!IsLand)
        {
            var ends = TownEnds();
            if (ends.Count < 2) return false;
            start = ends[0];
            end = ends[1];
            if (ends.Count > 2)
            {
                start = ends[Random.Range(0, ends.Count)];
                do end = ends[Random.Range(0, ends.Count)]; while (end == start);
                back = false;
            }
        }
        if (!PathBetween(start, end, true)) return false;
        // a town's pad is smaller than the yard, and at the quarry a road may end near the edge of the high ground
        float beyond = IsLand ? 9f : IsQuarry ? 4f : 14f;
        Lane(path, back, beyond, beyond);
        return path.Count > 8;
    }

    // the ends of the roads for everyone: stakes with just one such rope
    List<int> TownEnds()
    {
        var ends = new List<int>();
        for (int i = 0; i < stakes.Count; i++)
        {
            int ropes = 0;
            foreach (var l in links) if (l.z == 0 && (l.x == i || l.y == i)) ropes++;
            if (ropes == 1) ends.Add(i);
        }
        return ends;
    }

    // A depot is a stake where a truck stands until it is sent somewhere. This is the way from
    // one depot to another, by any road, service roads included.
    public bool DepotRoute(List<Vector3> path, int from, int to)
    {
        path.Clear();
        if (!Ready || from < 0 || to < 0 || from >= depots.Count || to >= depots.Count || !PathBetween(depots[from], depots[to])) return false;
        Lane(path, false, 0, 4f);       // a little past the stake, so the whole road is driven
        return path.Count > 6;
    }

    // where a truck stands at a depot, and which way it faces: as if it had just driven in
    // from the next depot along
    // `aside` metres to its right, off the road.
    public bool DepotStand(int depot, out Vector3 at, out Quaternion facing, float aside = 0)
    {
        at = Vector3.zero;
        facing = Quaternion.identity;
        var path = new List<Vector3>();
        if (IsQuarry)
        {
            // at the loading bay, facing the way out of the pit
            int bay = depots.Count > 0 ? SpurOf(depots[0]) : -1;
            if (bay < 0) return false;
            chain.Clear();
            chain.Add(links[bay].x == depots[0] ? bay : ~bay);
            Lane(path, false, 0, 0);
            if (path.Count < 6) return false;
            facing = Quaternion.LookRotation(Flat(path[5] - path[1]));
            at = path[1];
            at.y = HeightAt(at.x, at.z) + 0.3f;
            return true;
        }
        if (!DepotRoute(path, (depot + 1) % Mathf.Max(1, depots.Count), depot)) return false;
        facing = Quaternion.LookRotation(Flat(path[path.Count - 1] - path[path.Count - 5]));
        at = path[path.Count - 3] + facing * Vector3.right * aside;
        at.y = HeightAt(at.x, at.z) + 0.3f;
        return true;
    }

    public string DepotName(int depot) { return IsQuarry ? (depot == 0 ? "the quarry" : "the drop-off") : depot == 0 ? "the yard" : "the far end"; }

    // The points of a truck's way along the sections in `chain`: a run-up, the right-hand lane
    // of each section, and a run-off; or, with `back`, the same road the other way in the other lane.
    void Lane(List<Vector3> path, bool back, float RunUp, float runOff)
    {
        float half = (back ? -1f : 1f) * lane * 0.5f;       // to the right of the way the chain runs
        int c0 = chain[0], c1 = chain[chain.Count - 1];
        Seg first = segs[c0 < 0 ? ~c0 : c0], last = segs[c1 < 0 ? ~c1 : c1];
        Vector3 from = c0 < 0 ? first.a + first.d * first.len : first.a, ahead = c0 < 0 ? -first.d : first.d;
        for (float s = RunUp; s > 0; s -= 1f) path.Add(from - ahead * s + new Vector3(ahead.z, 0, -ahead.x) * half);
        for (int n = 0; n < chain.Count; n++)
        {
            int c = chain[n], k = c < 0 ? ~c : c;
            int steps = Mathf.Max(1, Mathf.RoundToInt(segs[k].len));
            for (int j = 0; j < steps; j++)
            {
                float t = (float)j / steps;
                path.Add(c < 0 ? World(k, 1f - t, -half) : World(k, t, half));
            }
            // At a junction the truck stays in its lane right up to the level ground round the
            // stake, and turns there, on a curve from the end of its lane to the start of the
            // next one. Turning right it swings a little wide, as a truck does.
            if (n + 1 >= chain.Count || (c < 0 ? segs[k].eA : segs[k].eB) <= 0) continue;
            int c2 = chain[n + 1], k2 = c2 < 0 ? ~c2 : c2;
            Vector3 p1 = c < 0 ? World(k, 0, -half) : World(k, 1f, half), p2 = c2 < 0 ? World(k2, 1f, -half) : World(k2, 0, half);
            Vector3 d1 = c < 0 ? -segs[k].d : segs[k].d, d2 = c2 < 0 ? -segs[k2].d : segs[k2].d;
            float cross = d1.x * d2.z - d1.z * d2.x;
            path.Add(p1);
            if (Mathf.Abs(cross) < 0.2f) continue;      // straight on
            Vector3 corner = p1 + d1 * (((p2.x - p1.x) * d2.z - (p2.z - p1.z) * d2.x) / cross);
            Vector3 stake = Flat(stakes[c < 0 ? links[k].x : links[k].y]);
            if ((cross < 0) != back) corner += (stake - corner).normalized * Mathf.Min(2f, Vector3.Distance(stake, corner));
            int points = Mathf.Max(4, Mathf.RoundToInt((Vector3.Distance(p1, corner) + Vector3.Distance(corner, p2)) / 0.7f));
            for (int j = 1; j < points; j++)
            {
                float u = (float)j / points;
                path.Add(Vector3.Lerp(Vector3.Lerp(p1, corner, u), Vector3.Lerp(corner, p2, u), u));
            }
        }
        Vector3 to = c1 < 0 ? last.a : last.a + last.d * last.len;
        ahead = c1 < 0 ? -last.d : last.d;
        for (float s = 0; s <= runOff; s += 1f) path.Add(to + ahead * s + new Vector3(ahead.z, 0, -ahead.x) * half);
        if (back) path.Reverse();    // the other way, which puts it in the other lane
    }

    // ---- roles, and the quarry

    // host: a section becomes a road for everyone (0) or a service road (1)
    public bool HostZone(int link, int role)
    {
        if (!Ready || link < 0 || link >= links.Count || role < 0 || role > 1 || links[link].z == role) return false;
        links[link] = new Vector3Int(links[link].x, links[link].y, role);
        Net.ToClients(StakesMsg(255, -1), true);
        StakesChanged(255, -1);
        return true;
    }

    public void RequestZone(int link, int role)
    {
        if (Net.IsHost) { HostZone(link, role); return; }
        var m = Msg.New(Op.Zone, 8);
        m.U8((byte)id);
        m.U8((byte)link);
        m.U8((byte)role);
        Net.ToHost(m, true);
    }

    public const int Scoop = 0, FlingIn = 1, TakeOff = 2, FlingToHeap = 3, PlaceHeap = 4, SendRig = 5;

    // host: one thing a player does with a shovel or a truck here.
    //   Scoop        load the shovel at the rock
    //   FlingIn      fling it into the gravel truck, which must be standing at the quarry
    //   TakeOff      load the shovel from the truck, which must be standing somewhere else
    //   FlingToHeap  fling it onto the heap
    //   PlaceHeap    say where the heap is to be (a, b are x and z)
    //   SendRig      send truck number a to depot number b
    public bool HostShovel(int slot, int what, float a, float b)
    {
        if (!Ready) return false;
        var t = Game.I.tuning;
        var rigs = Game.I.lorries;
        if (what == SendRig) return rigs.Send(Mathf.RoundToInt(a), Mathf.RoundToInt(b));
        if (!IsQuarry) return false;
        if (what == PlaceHeap)
        {
            // only while there is nothing on it to move
            if ((heapPlaced && stock > 0) || !Inside(a, b, 1f)) return false;
            heapPlaced = true;
            heapAt = new Vector3(a, 0, b);
            return true;
        }
        if (Time.time < hostNextClick[slot]) return false;
        hostNextClick[slot] = Time.time + 0.8f / t.clicksPerSecond;
        bool full = (carrying & 1 << slot) != 0;
        int at = rigs.StandingAt(Lorries.GravelTruck);
        if (what == Scoop && !full) carrying |= 1 << slot;
        else if (what == FlingIn && full && at == 0 && rigs.rigs[Lorries.GravelTruck].load < Mathf.RoundToInt(t.haulLoad)) { rigs.rigs[Lorries.GravelTruck].load++; carrying &= ~(1 << slot); }
        else if (what == TakeOff && !full && at > 0 && heapPlaced && rigs.rigs[Lorries.GravelTruck].load > 0) { rigs.rigs[Lorries.GravelTruck].load--; carrying |= 1 << slot; }
        else if (what == FlingToHeap && full && heapPlaced) { stock += Mathf.RoundToInt(t.shovelWorth); carrying &= ~(1 << slot); }
        else return false;
        return true;
    }

    public void RequestShovel(int what, float a = 0, float b = 0)
    {
        if (Net.IsHost) { HostShovel(Game.I.localSlot, what, a, b); return; }
        var m = Msg.New(Op.Shovel, 16);
        m.U8((byte)id);
        m.U8((byte)what);
        m.F32(a);
        m.F32(b);
        Net.ToHost(m, true);
    }

    // The zoning tool: the section under the crosshair is outlined in its role's color, and a
    // left click gives it the other role.
    void ZoneTool(Tuning tuning, Mouse mouse, Transform eye)
    {
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.clickReach * 2f, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        if (!Section(hit.point.x, hit.point.z, out int link, out _, out _)) return;
        bool service = links[link].z == 1;
        OutlineSection(link, service ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.35f, 0.3f));
        Say(service ? "a service road: the crew's trucks only. Left click to open it to everyone" : "a road for everyone. Left click to make it a service road", false);
        if (mouse.leftButton.wasPressedThisFrame) RequestZone(link, service ? 0 : 1);
    }

    void OutlineSection(int link, Color color)
    {
        const int Points = 48;
        cursor.enabled = true;
        cursor.startColor = cursor.endColor = color;
        cursor.positionCount = Points;
        for (int k = 0; k < Points; k++)
        {
            float e = k % 12 / 12f * 2f - 1f;
            int edge = k / 12;
            float u = edge == 0 ? e : edge == 1 ? 1 : edge == 2 ? -e : -1, v = edge == 0 ? -1 : edge == 1 ? e : edge == 2 ? 1 : -e;
            Vector3 p = World(link, 0.5f + v * 0.5f, u * Reach);
            cursor.SetPosition(k, new Vector3(p.x, HeightAt(p.x, p.z) + 0.06f, p.z));
        }
    }

    // ---- the dev tool
    //
    // Not part of the game: it does a whole section's work in one click, so that what comes
    // after the work can be tried without doing the work. One click puts every point of the
    // section on its line; the next lays its gravel and packs it.

    // which stage a section's next click would do: 0 level it, 1 gravel and pack it, 2 pave and
    // roll it, 3 paint it, -1 nothing left
    int NextStage(int link)
    {
        int next = -1;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] == 0 || linkOf[i] != link) continue;
            if (Mathf.Abs(h[i] - target[i]) >= Level) return 0;
            if (zone[i] != 1) continue;
            int stage = gravel[i] < FullGravel || packed[i] < 100 ? 1 : top[i] < Rolled ? 2 : top[i] < Painted ? 3 : -1;
            if (stage >= 0 && (next < 0 || stage < next)) next = stage;
        }
        return next;
    }

    // host: do the next stage of a whole section at once
    public bool HostFinish(int link)
    {
        if (!Ready || link < 0 || link >= segs.Length) return false;
        int stage = NextStage(link);
        if (stage < 0) return false;
        changed.Clear();
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] == 0 || linkOf[i] != link) continue;
            if (stage == 0) h[i] = target[i];
            else if (zone[i] != 1) continue;
            else if (stage == 1) { gravel[i] = (byte)FullGravel; packed[i] = 100; }
            else top[i] = (byte)(stage == 2 ? Rolled : Painted);
            health[i] = 100;
            changed.Add(i);
            // a section is a few thousand points; send them a few thousand at a time
            if (changed.Count >= 5000) { Broadcast(); changed.Clear(); }
        }
        Broadcast();
        return true;
    }

    public void RequestFinish(int link)
    {
        if (Net.IsHost) { HostFinish(link); return; }
        var m = Msg.New(Op.Finish, 8);
        m.U8((byte)id);
        m.U8((byte)link);
        Net.ToHost(m, true);
    }

    void DevTool(Tuning tuning, Mouse mouse, Transform eye)
    {
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        if (!Section(hit.point.x, hit.point.z, out int link, out _, out _)) return;
        int stage = NextStage(link);
        OutlineSection(link, new Color(1f, 0.3f, 1f));
        Say("dev tool: " + (stage == 0 ? "left click puts this whole section on its line" : stage == 1 ? "left click gravels and packs this whole section" : stage == 2 ? "left click paves and rolls this whole section" : stage == 3 ? "left click paints this whole section" : "this section is finished"), false);
        if (stage >= 0 && mouse.leftButton.wasPressedThisFrame) RequestFinish(link);
    }

    // A truck that stands at depots, under the crosshair with any tool in hand: say where it
    // can be sent, and send it on a right click. With more than two depots the wheel would pick.
    bool RigTool(Game g, Tuning tuning, Mouse mouse, RaycastHit hit)
    {
        int rig = g.lorries.RigOf(hit.collider);
        if (rig < 0 || g.lorries.rigs[rig].plot != id) return false;
        int at = g.lorries.rigs[rig].at;
        if (g.lorries.rigs[rig].kind == Lorries.DumpTruck)
        {
            // it comes and goes by itself
            Say("Dump truck: it brings asphalt by itself. Level what it leaves with the asphalt tool (5) or the roller", false);
            return true;
        }
        if (at < 0) { Say(g.lorries.rigs[rig].leg == 2 ? "backing into the drop-off" : "on its way to " + DepotName(g.lorries.rigs[rig].to), false); return true; }
        if (g.lorries.rigs[rig].kind == Lorries.GravelTruck)
        {
            // it runs its own round: off when it is full, home when it is empty. A right click sends it early.
            int aboard = g.lorries.rigs[rig].load;
            bool can = at == 1 || (drop >= 0 && aboard > 0);
            rigLine = at == 1 ? "it goes home when it is empty; right click sends it now"
                : drop < 0 ? "there is no drop-off yet: press 9 and put one down beside a road"
                : aboard == 0 ? "it sets off for the drop-off when it is full" : "it sets off when it is full; right click sends it now";
            if (can && mouse.rightButton.wasPressedThisFrame) RequestShovel(SendRig, rig, at == 0 ? 1 : 0);
            return false;
        }
        int to = (at + 1) % Mathf.Max(1, depots.Count);
        rigLine = g.lorries.RigName(rig) + ": right click sends it to " + DepotName(to);
        if (mouse.rightButton.wasPressedThisFrame) RequestShovel(SendRig, rig, to);
        return false;   // the caller may have more to say about it
    }
    string rigLine = "";

    // The quarry with the gravel tool in hand. A shovel holds one load: load it at the rock and
    // fling it into the truck, or load it from the truck and fling it onto the heap. The heap
    // has to be put somewhere first: the first click on a loaded truck at the drop starts that.
    bool QuarryTool(Game g, Tuning tuning, Mouse mouse, Transform eye)
    {
        bool full = (carrying & 1 << g.localSlot) != 0;
        bool click = mouse.leftButton.isPressed && Time.time >= nextClick;
        string hands = full ? "Your shovel is loaded. " : "";
        if (placing)
        {
            // choosing where the heap goes
            if (mouse.rightButton.wasPressedThisFrame || heapPlaced && stock > 0) { placing = false; return true; }
            if (!Physics.Raycast(eye.position, eye.forward, out var ground, tuning.flingReach, ~0, QueryTriggerInteraction.Ignore) || ground.collider.transform.parent != transform) { Say("point at the ground where the heap should go", false); return true; }
            Ring(ground.point, 1.6f, GravelLoose);
            Say("left click puts the heap here. Right click cancels", false);
            if (mouse.leftButton.wasPressedThisFrame) { RequestShovel(PlaceHeap, ground.point.x, ground.point.z); placing = false; nextClick = Time.time + 0.3f; }
            return true;
        }
        rigLine = "";
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.flingReach, ~0, QueryTriggerInteraction.Ignore)) return false;
        bool near = hit.distance <= tuning.clickReach;
        if (RigTool(g, tuning, mouse, hit)) return true;
        int what = -1;
        if (hit.collider.transform == pile)
        {
            if (full) { what = FlingToHeap; Say("left click flings it onto the heap", false); }
            else Say("the heap: " + stock + " clicks' worth of gravel", false);
        }
        else if (hit.collider.transform.parent == quarryRoot)
        {
            if (full) Say(hands + "Fling it into the truck", false);
            else if (!near) Say("go closer to the rock to load your shovel", false);
            else { what = Scoop; Say("left click loads your shovel", false); }
        }
        else if (g.lorries.RigOf(hit.collider) == Lorries.GravelTruck)
        {
            var rig = g.lorries.rigs[Lorries.GravelTruck];
            int most = Mathf.RoundToInt(tuning.haulLoad);
            if (rig.at == 0)
            {
                if (full && rig.load < most) { what = FlingIn; Say("left click flings it into the truck (" + rig.load + " of " + most + ").   " + rigLine, false); }
                else Say((rig.load >= most ? "The truck is full.   " : "Load your shovel at the rock.   ") + rigLine, false);
            }
            else if (rig.load == 0) Say("The truck is empty.   " + rigLine, false);
            else if (full) Say(hands + (heapPlaced ? "Fling it onto the heap" : ""), false);
            else if (!heapPlaced)
            {
                Say("left click, then say where the heap of gravel should go", false);
                if (mouse.leftButton.wasPressedThisFrame) placing = true;
            }
            else if (!near) Say("go closer to the truck to load your shovel from it", false);
            else { what = TakeOff; Say("left click loads your shovel from the truck (" + rig.load + " left)", false); }
        }
        else return false;
        if (what >= 0 && click)
        {
            nextClick = Time.time + 1f / tuning.clicksPerSecond;
            Shovel.Swing(what == Scoop || what == TakeOff ? Shovel.Scoop : Shovel.Fling);
            RequestShovel(what);
        }
        return true;
    }

    // a ring on the ground
    void Ring(Vector3 centre, float radius, Color color)
    {
        const int Points = 24;
        cursor.enabled = true;
        cursor.startColor = cursor.endColor = color;
        cursor.positionCount = Points;
        for (int k = 0; k < Points; k++)
        {
            float angle = k * Mathf.PI * 2f / Points;
            Vector3 p = centre + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            cursor.SetPosition(k, new Vector3(p.x, HeightAt(p.x, p.z) + 0.06f, p.z));
        }
    }

    void QuarryLabels(Game g)
    {
        float size = Mathf.Pow(Mathf.Max(stock, 1f), 1f / 3f) * 0.6f + 0.4f;
        pile.gameObject.SetActive(heapPlaced);
        if (heapPlaced)
        {
            pile.position = new Vector3(heapAt.x, HeightAt(heapAt.x, heapAt.z) + size * 0.2f, heapAt.z);
            pile.localScale = new Vector3(size * 2f, size * 1.2f, size * 2f);
        }
        labels[1] = new Yard.Label { at = pile.position + Vector3.up * (size * 0.6f + 1.4f), text = !heapPlaced ? "" : "The heap: " + stock + " clicks' worth of gravel." + (stock == 0 ? "\nNone can be laid here until some is unloaded." : "") };
        labels[2] = new Yard.Label { at = g.lorries.RigAt(Lorries.GravelTruck) + Vector3.up * 4f, text = g.lorries.RigSays(Lorries.GravelTruck) };
        labels[0] = drop >= 0 && drop < stakes.Count
            ? new Yard.Label { at = stakes[drop] + Vector3.up * 2.6f, text = "Gravel drop-off. The truck backs in here\nand waits to be unloaded." }
            : new Yard.Label { at = (stakes.Count > 2 ? stakes[2] : stakes[0]) + Vector3.up * 3f, text = "No gravel drop-off yet. Press 9, left click a stake in the\nmiddle of a road, then left click the ground beside the road." };
    }

    // host: a truck is on this road, its wheels touching the ground at these spots. It does a
    // random amount of damage to the grid square under it. Once a square is worn below the
    // threshold, the wheels start to cut into it: gravel is scattered first, then the ground ruts.
    //
    // That is the rule the first wear road (on the Trucks ground) was played with, and it keeps
    // it. Every other road that wears follows the rule of 2026-10-06:
    //   - Bare ground wears as well as gravel, each at its own rate. Paved road does not wear.
    //   - Damage comes two ways, with a slider between them: by time, to the square under the
    //     truck, as before; and by landing, to the square under each wheel, in proportion to how
    //     fast that wheel came down on it. So one hole starts the next.
    //   - Once a square is worn below the threshold, damage to it is scaled up, and the wheels
    //     cut deeper the further below it is.
    public void Wear(Vector3 centre, List<Vector3> wheels, float[] hit, Vector3[] hitAt)
    {
        var tuning = Game.I.tuning;
        if (!Ready) return;
        if (id != 5)
        {
            float byLanding = Mathf.Clamp01(tuning.wearByLanding);
            if (byLanding < 1f && Section(centre.x, centre.z, out int under, out float along_, out float off))
                Damage(tuning, under, along_, off, Random.value * (1f - byLanding));
            if (byLanding > 0)
                for (int k = 0; k < hit.Length; k++)
                {
                    // a wheel that is only rolling comes down at a few centimetres a second, and that is free
                    float hard = (hit[k] - 0.15f) / Mathf.Max(0.05f, tuning.wearLandSpeed);
                    if (hard > 0 && Section(hitAt[k].x, hitAt[k].z, out under, out along_, out off)) Damage(tuning, under, along_, off, Mathf.Min(2f, hard) * byLanding);
                }
            changed.Clear();
            foreach (var wheel in wheels)
            {
                if (!Inside(wheel.x, wheel.z, 0.5f)) continue;
                int at = Mathf.RoundToInt((wheel.z - origin.z) / Cell) * w + Mathf.RoundToInt((wheel.x - origin.x) / Cell);
                if (zone[at] == 0 || top[at] > 0 || health[at] >= tuning.damageThreshold) continue;
                // the further below the threshold, the deeper the cut
                float worse = 1f - health[at] / Mathf.Max(1f, tuning.damageThreshold);
                Cut(wheel, tuning.wearCut * (gravel[at] == 0 ? tuning.wearDirtCut : 1f) * Random.Range(0.4f, 1f) * Mathf.Lerp(1f, tuning.wearScaleUp, worse));
            }
            if (changed.Count > 0) Broadcast();
            return;
        }
        if (!Section(centre.x, centre.z, out int link, out float t, out float side)) return;
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

    // Test tooling, host: the state of a road that wears, in a line. Shares are of the road's
    // points: worn below the threshold; its packing gone; cut more than 2 cm and more than 15 cm
    // below the line; and the deepest cut.
    public string WearSays()
    {
        if (!Ready) return "";
        int road = 0, worn = 0, loose = 0, cut = 0, deep = 0;
        float most = 0;
        for (int i = 0; i < h.Length; i++)
        {
            if (zone[i] != 1) continue;
            road++;
            if (health[i] < Game.I.tuning.damageThreshold) worn++;
            if (StartsAs == 2 && packed[i] < 100) loose++;
            float below = target[i] - h[i];
            if (below > 0.02f) cut++;
            if (below > 0.15f) deep++;
            most = Mathf.Max(most, below);
        }
        float share = 100f / Mathf.Max(1, road);
        return "worn=" + (worn * share).ToString("0.0") + "% unpacked=" + (loose * share).ToString("0.0") + "% cut=" + (cut * share).ToString("0.0") + "% deep=" + (deep * share).ToString("0.0") + "% deepest=" + (most * 100f).ToString("0") + "cm";
    }

    // host: a truck damages the grid square at t, side of a section, by `amount` of what its
    // surface takes in a quarter second: bare ground and gravel each have their own rate, and
    // asphalt takes none. A square already below the threshold takes more.
    void Damage(Tuning tuning, int link, float t, float side, float amount)
    {
        Square(tuning, link, ref t, ref side, out float halfT, out float halfSide);
        Box(link, t, side, halfT, halfSide, out int x0, out int x1, out int z0, out int z1);
        int left = 100, gravelled = 0, bare = 0;
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                if (!InSquare(i, link, t, side, halfT, halfSide)) continue;
                if (top[i] > 0) return;     // paved: no wear
                left = Mathf.Min(left, health[i]);
                if (gravel[i] > 0) gravelled++; else bare++;
            }
        float damage = amount * (gravelled > bare ? tuning.wearGravel : tuning.wearDirt);
        if (left < tuning.damageThreshold) damage *= tuning.wearScaleUp;
        // a rate of less than one a time still wears, one time in so many
        int whole = Mathf.FloorToInt(damage) + (Random.value < damage - Mathf.Floor(damage) ? 1 : 0);
        if (whole <= 0) return;
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                if (InSquare(i, link, t, side, halfT, halfSide)) health[i] = (byte)Mathf.Max(0, health[i] - whole);
            }
    }

    // host: a wheel cuts into the road under it, this deep: the packing goes, then the gravel is
    // scattered, then the ground ruts. Asphalt is not cut. Points it changes are added to `changed`.
    void Cut(Vector3 wheel, float depth)
    {
        Box(wheel.x, wheel.z, 0.45f, out int x0, out int x1, out int z0, out int z1);
        for (int iz = z0; iz <= z1; iz++)
            for (int ix = x0; ix <= x1; ix++)
            {
                int i = iz * w + ix;
                float dx = origin.x + ix * Cell - wheel.x, dz = origin.z + iz * Cell - wheel.z;
                if (zone[i] == 0 || top[i] > 0 || dx * dx + dz * dz > 0.45f * 0.45f || changed.Contains(i)) continue;
                packed[i] = 0;
                int scatter = Mathf.RoundToInt(depth * 1000f);
                if (gravel[i] >= scatter) gravel[i] = (byte)(gravel[i] - scatter);
                else
                {
                    // no hole goes deeper below the line than the slider allows
                    // (and not every point as deep as that, or a road worn right out would be smooth again)
                    // (and not as deep as that everywhere: the bottom is in patches a metre across, or a road worn right out would be smooth again)
                    int patch = (i / w / 4) * 977 + i % w / 4;
                    float floor = Mathf.Min(h[i], target[i] - Game.I.tuning.wearDeepest * (0.4f + 0.6f * ((patch * 7919 ^ patch >> 3) & 15) / 15f));
                    h[i] = Mathf.Max(Mathf.Max(0, floor), Mathf.Round((h[i] - (depth - gravel[i] * 0.001f)) * 1000f) / 1000f);
                    gravel[i] = 0;
                }
                changed.Add(i);
            }
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
        if (IsQuarry && pile != null) QuarryLabels(g);
        if (Cars.Mine >= 0) return;     // both hands are on the wheel
        if (id == Paving && labels.Count >= 3)
            for (int r = 1; r <= 2; r++) labels[r] = new Yard.Label { at = g.lorries.RigAt(r) + Vector3.up * 4f, text = g.lorries.RigSays(r) };
        if (g.local == null || !Hud.Playing) return;
        var mouse = Mouse.current;
        if (mouse == null) return;
        var eye = g.cam.transform;

        // A truck that waits to be sent comes before whatever tool is in hand: the crosshair on
        // it says what a click would do, a right click sends it, and no tool acts through it.
        // (At the quarry with the gravel tool, the shovel has more to say: see QuarryTool.)
        if ((depots.Count > 1 || IsQuarry) && !(IsQuarry && Tool == Gravel) && Physics.Raycast(eye.position, eye.forward, out var truck, tuning.flingReach, ~0, QueryTriggerInteraction.Ignore))
        {
            rigLine = "";
            if (RigTool(g, tuning, mouse, truck)) return;
            if (rigLine.Length > 0) { Say(rigLine, false); return; }
        }

        // paint by hand, on the strips that take it: the roller brush, the tar spray and the grinder
        if (Tool == TarSpray || Tool == Grinder || (Tool == Brush && lines != null))
        {
            if (lines != null) lines.Hold(tuning, mouse, eye);
            return;
        }
        if (Tool == Stakes)
        {
            StakeTool(tuning, mouse, eye);
            return;
        }
        if (Tool == Zone)
        {
            ZoneTool(tuning, mouse, eye);
            return;
        }
        if (Tool == DropTool)
        {
            DropSurvey(tuning, mouse, eye);
            return;
        }
        if (Tool == Dev)
        {
            DevTool(tuning, mouse, eye);
            return;
        }
        if (IsQuarry && Tool == Gravel && QuarryTool(g, tuning, mouse, eye)) return;
        if (hotPlot == this) DrawHot(tuning);
        if (!Physics.Raycast(eye.position, eye.forward, out var hit, tuning.clickReach, ~0, QueryTriggerInteraction.Ignore) || hit.collider.transform.parent != transform) return;
        Vector3 aim = hit.point;

        if (!Section(aim.x, aim.z, out int link, out float t, out float side)) return;
        if (Tool == Brush)
        {
            // the brush: left paints white and right paints yellow, as fast as the hand moves
            Ring(aim, 0.3f, LineWhite);
            Say("the brush: left click paints white, right click yellow, on rolled asphalt", false);
            bool yellow = mouse.rightButton.isPressed;
            if ((mouse.leftButton.isPressed || yellow) && Time.time >= nextClick)
            {
                nextClick = Time.time + 0.03f;
                RequestClick(aim.x, aim.z, false, yellow ? BrushYellow : Brush);
            }
            return;
        }
        Outline(tuning, aim, link, t, side);
        // holding the button keeps clicking, as fast as the cap allows
        if (!mouse.leftButton.isPressed || Time.time < nextClick) return;
        nextClick = Time.time + 1f / tuning.clicksPerSecond;
        // the shovel in the player's hands: it digs, spreads or tamps with each click
        if (Tool == Grade) Shovel.Swing(Shovel.Dig);
        else if (Tool == Pave) Shovel.Swing(Shovel.Spread);
        else if (Tool == Gravel)
        {
            int at = Mathf.Clamp(Mathf.RoundToInt((aim.z - origin.z) / Cell), 0, d - 1) * w + Mathf.Clamp(Mathf.RoundToInt((aim.x - origin.x) / Cell), 0, w - 1);
            Shovel.Swing(gravel[at] < FullGravel ? Shovel.Spread : Shovel.Tamp);
        }
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
        if (!allowed) Say(Why, true);   // the screen says which rule it breaks
    }

    // the edge of the patch a click would move, draped over the ground
    void Outline(Tuning tuning, Vector3 aim, int link, float t, float side)
    {
        bool round = tuning.brushRound >= 0.5f;
        float halfT = 0, halfSide = 0;
        if (!round) Square(tuning, link, ref t, ref side, out halfT, out halfSide);
        const int Points = 48;
        cursor.enabled = true;
        cursor.startColor = cursor.endColor = Tool == Grade ? Color.white : Tool == Pave ? new Color(0.9f, 0.5f, 0.2f) : Tool == Paint ? (Color)LineYellow : (Color)GravelLoose;
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
