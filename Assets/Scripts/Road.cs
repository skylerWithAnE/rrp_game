using System.Collections.Generic;
using UnityEngine;

// The road check, and the towns it runs between. A road is a connected strip at least 3 points
// wide, so the search walks over points whose whole 3x3 neighbourhood is road. The host reruns it
// when road surfaces change, and keeps the asphalt route for the truck to follow.
public class Road : MonoBehaviour
{
    const float SiteRadius = 1.6f;

    public bool asphaltLinked, paintedLinked;
    public bool dirty;
    public readonly List<Vector3> path = new List<Vector3>(); // first town to second, along the asphalt

    float timer;
    bool[] wide;
    int[] from;      // 0 unseen, else the point it was reached from + 1
    int[] queue;

    public void Setup(bool hill)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        asphaltLinked = paintedLinked = dirty = false;
        path.Clear();
        if (!hill) return;

        var ground = Game.I.ground;
        Town(ground.siteA, -1, new Color(0.85f, 0.35f, 0.3f));
        Town(ground.siteB, 1, new Color(0.3f, 0.5f, 0.85f));
        wide = new bool[ground.h.Length];
        from = new int[ground.h.Length];
        queue = new int[ground.h.Length];
        dirty = true;
    }

    // A handful of solid-color houses behind the town's pad, away from the hill.
    void Town(Vector3 site, int away, Color color)
    {
        var wall = Mats.Make(color);
        var roof = Mats.Make(color * 0.6f);
        var rng = new System.Random((int)(site.z * 31));
        for (int k = 0; k < 5; k++)
        {
            float x = site.x + (k - 2) * 2.3f;
            float z = site.z + away * (3.4f + (k % 2) * 1.3f);
            float y = Game.I.ground.HeightAt(x, z);
            float width = 1.5f + (float)rng.NextDouble() * 0.5f, height = 1.6f + (float)rng.NextDouble() * 1.6f;
            var house = Mats.Part(transform, Mats.Cube, wall, new Vector3(x, y + height * 0.5f - 0.3f, z), new Vector3(width, height + 0.6f, width));
            house.gameObject.AddComponent<BoxCollider>();
            Mats.Part(transform, Mats.Cube, roof, new Vector3(x, y + height + 0.1f, z), new Vector3(width + 0.3f, 0.25f, width + 0.3f));
        }
    }

    void Update()
    {
        if (!Net.IsHost || Game.I.phase != Phase.Job || !dirty) return;
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = 0.5f;
        dirty = false;
        paintedLinked = Linked(Ground.Painted);
        asphaltLinked = Linked(Ground.Asphalt);   // last, so `from` holds the asphalt search
    }

    // Is there a 3-wide strip of at least this surface from one town to the other?
    bool Linked(byte tier)
    {
        var ground = Game.I.ground;
        int w = ground.w, d = ground.d;
        byte[] surface = ground.surface;
        for (int z = 1; z < d - 1; z++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = z * w + x;
                bool ok = true;
                for (int dz = -1; dz <= 1 && ok; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (surface[i + dz * w + dx] < tier) { ok = false; break; }
                wide[i] = ok;
                from[i] = 0;
            }

        int head = 0, tail = 0;
        int start = ground.NearestPoint(ground.siteA);
        if (!wide[start]) return false;
        from[start] = start + 1;
        queue[tail++] = start;
        while (head < tail)
        {
            int i = queue[head++];
            if (Near(i % w, i / w, ground.siteB))
            {
                if (tier == Ground.Asphalt) TracePath(i);
                return true;
            }
            Visit(i, i - 1, ref tail);
            Visit(i, i + 1, ref tail);
            Visit(i, i - w, ref tail);
            Visit(i, i + w, ref tail);
        }
        if (tier == Ground.Asphalt) path.Clear();
        return false;
    }

    void Visit(int parent, int i, ref int tail)
    {
        if (!wide[i] || from[i] != 0) return;
        from[i] = parent + 1;
        queue[tail++] = i;
    }

    void TracePath(int end)
    {
        var ground = Game.I.ground;
        path.Clear();
        for (int i = end; ; i = from[i] - 1)
        {
            path.Add(ground.PointPos(i));
            if (from[i] - 1 == i) break;
        }
        path.Reverse();
    }

    static bool Near(int x, int z, Vector3 site)
    {
        float dx = x * Ground.Cell - site.x, dz = z * Ground.Cell - site.z;
        return dx * dx + dz * dz <= SiteRadius * SiteRadius;
    }
}
