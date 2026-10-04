using UnityEngine;

// The road check. A road is a connected strip at least 3 points wide, so the search walks over
// points whose whole 3x3 neighbourhood is road. The host reruns it when road surfaces change.
// Until the towns exist (milestone 5) the two ends are marked by posts.
public class Road : MonoBehaviour
{
    const float SiteRadius = 3f;

    public bool asphaltLinked, paintedLinked;
    public bool dirty;
    public Vector3 siteA, siteB;

    float timer;
    bool[] wide;
    bool[] seen;
    int[] queue;

    public void Setup(bool hill)
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        asphaltLinked = paintedLinked = dirty = false;
        if (!hill) return;

        var ground = Game.I.ground;
        siteA = Site(ground, 5f);
        siteB = Site(ground, ground.SizeZ - 5f);
        var material = Mats.Make(new Color(0.9f, 0.25f, 0.2f));
        Mats.Part(transform, Mats.Cube, material, siteA + Vector3.up * 2.5f, new Vector3(0.3f, 5f, 0.3f));
        Mats.Part(transform, Mats.Cube, material, siteB + Vector3.up * 2.5f, new Vector3(0.3f, 5f, 0.3f));
        wide = new bool[ground.h.Length];
        seen = new bool[ground.h.Length];
        queue = new int[ground.h.Length];
    }

    static Vector3 Site(Ground ground, float z)
    {
        float x = ground.SizeX * 0.5f;
        return new Vector3(x, ground.HeightAt(x, z), z);
    }

    void Update()
    {
        if (!Net.IsHost || Game.I.phase != Phase.Job || !dirty) return;
        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = 0.5f;
        dirty = false;
        asphaltLinked = Linked(Ground.Asphalt);
        paintedLinked = Linked(Ground.Painted);
    }

    // Is there a 3-wide strip of at least this surface from one site to the other?
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
                seen[i] = false;
            }

        int head = 0, tail = 0;
        for (int z = 1; z < d - 1; z++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = z * w + x;
                if (wide[i] && Near(x, z, siteA)) { seen[i] = true; queue[tail++] = i; }
            }
        while (head < tail)
        {
            int i = queue[head++];
            if (Near(i % w, i / w, siteB)) return true;
            Visit(i - 1, ref tail);
            Visit(i + 1, ref tail);
            Visit(i - w, ref tail);
            Visit(i + w, ref tail);
        }
        return false;
    }

    void Visit(int i, ref int tail)
    {
        if (!wide[i] || seen[i]) return;
        seen[i] = true;
        queue[tail++] = i;
    }

    static bool Near(int x, int z, Vector3 site)
    {
        float dx = x * Ground.Cell - site.x, dz = z * Ground.Cell - site.z;
        return dx * dx + dz * dz <= SiteRadius * SiteRadius;
    }
}
