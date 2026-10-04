using System.Collections.Generic;
using UnityEngine;

// Blocks: static cubes on the same grid as the ground points, one cube high per level. A rock cube
// set down becomes a block; a block scooped up is a rock cube again. There are no structural
// rules: a block may hang off the side of another, blocks never fall, and earthquakes ignore them.
// The host decides every placement and removal and tells the clients. Town houses are blocks too,
// built the same way on every machine and marked permanent.
public class Blocks : MonoBehaviour
{
    public const int MaxLevel = 60;
    public const byte Rock = 0;

    class Block
    {
        public GameObject go;
        public byte style;
        public bool permanent;
    }

    readonly Dictionary<long, Block> map = new Dictionary<long, Block>();
    readonly Dictionary<int, Vector3Int> byCollider = new Dictionary<int, Vector3Int>();
    readonly Dictionary<byte, Material> materials = new Dictionary<byte, Material>();

    public int Count => map.Count;

    static long Key(int x, int y, int z) { return (long)y << 40 | (long)z << 20 | (uint)x; }
    public static Vector3 Centre(Vector3Int c) { return new Vector3(c.x * Ground.Cell, (c.y + 0.5f) * Ground.Cell, c.z * Ground.Cell); }

    public bool Has(int x, int y, int z) { return map.ContainsKey(Key(x, y, z)); }
    public bool Has(Vector3Int c) { return Has(c.x, c.y, c.z); }
    public bool Permanent(Vector3Int c) { return map.TryGetValue(Key(c.x, c.y, c.z), out var b) && b.permanent; }

    public bool TryCell(Collider collider, out Vector3Int cell)
    {
        return byCollider.TryGetValue(collider.GetInstanceID(), out cell);
    }

    public void Clear()
    {
        foreach (var b in map.Values) Destroy(b.go);
        map.Clear();
        byCollider.Clear();
    }

    Material MaterialFor(byte style)
    {
        if (!materials.TryGetValue(style, out var m) || m == null)
        {
            // style 0 is dressed rock; the others are town colors, with a darker shade for roofs
            Color c = new Color(0.30f, 0.38f, 0.52f);   // steel blue: nothing like a loose rock or oil cube
            if (style == 1) c = new Color(0.85f, 0.35f, 0.3f);
            else if (style == 2) c = new Color(0.3f, 0.5f, 0.85f);
            else if (style == 3) c = new Color(0.5f, 0.2f, 0.18f);
            else if (style == 4) c = new Color(0.18f, 0.3f, 0.5f);
            materials[style] = m = Mats.Make(c);
        }
        return m;
    }

    // May a player's block go here? It must be free, inside the map, off the town pads, and either
    // sit at the surface of its column or touch another block.
    public bool CanPlace(Vector3Int c)
    {
        var ground = Game.I.ground;
        if (c.x < 1 || c.z < 1 || c.x > ground.w - 2 || c.z > ground.d - 2 || c.y < 0 || c.y >= MaxLevel) return false;
        if (Has(c)) return false;
        int i = ground.Index(c.x, c.z);
        if (ground.locked[i]) return false;
        if (c.y == SurfaceLevel(i)) return true;
        return Has(c.x + 1, c.y, c.z) || Has(c.x - 1, c.y, c.z) || Has(c.x, c.y + 1, c.z)
            || Has(c.x, c.y - 1, c.z) || Has(c.x, c.y, c.z + 1) || Has(c.x, c.y, c.z - 1);
    }

    // The level a block takes when it is simply set down on a column.
    public static int SurfaceLevel(int i)
    {
        return Mathf.RoundToInt(Game.I.ground.Surface(i) / Ground.Cell);
    }

    public void Place(Vector3Int c, byte style, bool permanent, bool tellClients)
    {
        if (Has(c)) return;
        var go = new GameObject("Block");
        go.transform.SetParent(transform, false);
        go.transform.position = Centre(c);
        go.transform.localScale = Vector3.one * Ground.Cell;
        go.AddComponent<MeshFilter>().sharedMesh = Mats.Cube;
        go.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(style);
        var collider = go.AddComponent<BoxCollider>();
        map[Key(c.x, c.y, c.z)] = new Block { go = go, style = style, permanent = permanent };
        byCollider[collider.GetInstanceID()] = c;
        ColumnChanged(c.x, c.z);
        if (tellClients) Send(1, c, style);
    }

    public void Remove(Vector3Int c, bool tellClients)
    {
        long key = Key(c.x, c.y, c.z);
        if (!map.TryGetValue(key, out var b)) return;
        byCollider.Remove(b.go.GetComponent<Collider>().GetInstanceID());
        Destroy(b.go);
        map.Remove(key);
        ColumnChanged(c.x, c.z);
        if (tellClients) Send(0, c, 0);
    }

    static void Send(byte add, Vector3Int c, byte style)
    {
        var m = Msg.New(Op.Block, 12);
        m.U8(add);
        m.U16((ushort)c.x);
        m.U16((ushort)c.y);
        m.U16((ushort)c.z);
        m.U8(style);
        Net.ToClients(m, true);
    }

    // client
    public void OnMessage(Msg m)
    {
        bool add = m.U8() != 0;
        var c = new Vector3Int(m.U16(), m.U16(), m.U16());
        byte style = m.U8();
        if (add) Place(c, style, false, false);
        else Remove(c, false);
    }

    // The ground wants to know how high the blocks reach at each point.
    void ColumnChanged(int x, int z)
    {
        float top = 0;
        for (int y = MaxLevel - 1; y >= 0; y--)
            if (Has(x, y, z)) { top = (y + 1) * Ground.Cell; break; }
        Game.I.ground.SetBlockTop(Game.I.ground.Index(x, z), top);
    }
}
