using System.Collections.Generic;
using UnityEngine;

// Every loose cube. The host simulates them as rigidbodies; clients hold kinematic copies that
// ease toward the host's poses. Only awake cubes are sent, at the tunable sync rate; a cube that
// falls asleep gets one final reliable pose.
public class Cubes : MonoBehaviour
{
    public const byte Sand = 1, Oil = 2;
    public const int Capacity = 2048;
    public const int BitsPerCube = 5;       // a failed oil cube splits into this many quarter-height bits
    const float Size = Ground.Cell;
    const int PerSnapshot = 80;             // cubes per unreliable packet (12 bytes each)

    public class Cube
    {
        public int id;
        public GameObject go;
        public Rigidbody rb;
        public MeshRenderer renderer;
        public bool active;
        public byte mat;
        public bool bit;
        // host
        public bool awake;
        public float slowTime;
        public int flySource = -1;          // who flung it; they cannot catch it
        public float flyTime;               // catchable while above zero
        // client
        public Vector3 targetPos;
        public Quaternion targetRot;
        public uint tick;
    }

    public readonly Cube[] all = new Cube[Capacity];
    readonly Stack<int> free = new Stack<int>();
    readonly List<int> rested = new List<int>();
    readonly Msg snapshot = new Msg(Net.MaxUnreliable + 64);

    public int Loose, Moving;
    public int clientMoving;                // from the host's stats
    public float clientLag;                 // client: how far the worst cube is from where it should be
    uint tick;
    float syncTimer;
    Material sandMaterial, oilMaterial;
    PhysicsMaterial physicsMaterial;

    void Awake()
    {
        sandMaterial = Mats.Make(new Color(0.78f, 0.62f, 0.36f));
        oilMaterial = Mats.Make(new Color(0.13f, 0.10f, 0.16f));
        physicsMaterial = new PhysicsMaterial("Cube");
        for (int i = Capacity - 1; i >= 0; i--) free.Push(i);
    }

    Cube Take(int id, byte mat, bool bit, Vector3 pos, Quaternion rot)
    {
        var c = all[id];
        if (c == null)
        {
            c = all[id] = new Cube { id = id };
            c.go = new GameObject("Cube");
            c.go.transform.SetParent(transform, false);
            c.go.AddComponent<MeshFilter>().sharedMesh = Mats.Cube;
            c.renderer = c.go.AddComponent<MeshRenderer>();
            c.go.AddComponent<BoxCollider>().sharedMaterial = physicsMaterial;
            c.rb = c.go.AddComponent<Rigidbody>();
        }
        c.active = true;
        c.mat = mat;
        c.bit = bit;
        c.renderer.sharedMaterial = mat == Oil ? oilMaterial : sandMaterial;
        c.go.transform.localScale = bit ? new Vector3(Size * 0.8f, Size * 0.25f, Size * 0.8f) : Vector3.one * Size * 0.98f;
        c.go.transform.SetPositionAndRotation(pos, rot);
        c.targetPos = pos;
        c.targetRot = rot;
        c.slowTime = 0;
        c.flySource = -1;
        c.flyTime = 0;
        c.awake = true;
        c.go.SetActive(true);
        c.rb.isKinematic = !Net.IsHost;
        c.rb.mass = Game.I.tuning.cubeMass * (bit ? 0.25f : 1f);
        c.rb.interpolation = RigidbodyInterpolation.None;
        Loose++;
        return c;
    }

    void Release(Cube c)
    {
        if (!c.active) return;
        c.active = false;
        c.go.SetActive(false);
        Loose--;
    }

    public void Clear()
    {
        for (int i = 0; i < Capacity; i++) if (all[i] != null && all[i].active) Release(all[i]);
        free.Clear();
        for (int i = Capacity - 1; i >= 0; i--) free.Push(i);
        rested.Clear();
        Loose = Moving = 0;
    }

    // ---- host

    public bool Full => Loose >= Mathf.Min(Capacity, (int)Game.I.tuning.maxLooseCubes);

    public Cube Spawn(byte mat, bool bit, Vector3 pos, Quaternion rot, Vector3 velocity, int flySource = -1)
    {
        if (free.Count == 0) return null;
        var c = Take(free.Pop(), mat, bit, pos, rot);
        c.rb.linearVelocity = velocity;
        c.rb.angularVelocity = velocity.sqrMagnitude > 0.01f ? Random.insideUnitSphere * 3f : Vector3.zero;
        if (flySource >= 0) { c.flySource = flySource; c.flyTime = 3f; }
        var m = Msg.New(Op.CubeSpawn, 32);
        m.U16(1);
        WriteSpawn(m, c);
        Net.ToClients(m, true);
        return c;
    }

    void WriteSpawn(Msg m, Cube c)
    {
        m.U16((ushort)c.id);
        m.U8((byte)(c.mat | (c.bit ? 0x80 : 0)));
        m.U32(tick);
        m.V3(c.go.transform.position);
        m.Rot(c.go.transform.rotation);
    }

    public void Remove(Cube c)
    {
        if (!c.active) return;
        Release(c);
        free.Push(c.id);
        var m = Msg.New(Op.CubeRemove, 8);
        m.U16(1);
        m.U16((ushort)c.id);
        Net.ToClients(m, true);
    }

    public void RemoveAll()
    {
        var m = Msg.New(Op.CubeRemove, 8);
        m.U16(0xFFFF);
        Net.ToClients(m, true);
        Clear();
    }

    // Stress test: a block of cubes dropped above a spot. One spawn message per 150 cubes.
    public int SpawnPile(int count, Vector3 centre, byte mat = Sand)
    {
        int side = Mathf.CeilToInt(Mathf.Pow(count, 1f / 3f));
        float step = Size * 1.15f;
        Msg m = null;
        int inMessage = 0, made = 0;
        float ground = Game.I.ground.HeightAt(centre.x, centre.z);
        for (int i = 0; i < count && free.Count > 0; i++)
        {
            int x = i % side, z = i / side % side, y = i / (side * side);
            Vector3 pos = new Vector3(centre.x + (x - side * 0.5f) * step, ground + 1.5f + y * step, centre.z + (z - side * 0.5f) * step);
            var c = Take(free.Pop(), mat, false, pos, Quaternion.identity);
            c.rb.linearVelocity = Vector3.zero;
            c.rb.angularVelocity = Vector3.zero;
            if (m == null) { m = Msg.New(Op.CubeSpawn, 4096); m.U16(0); inMessage = 0; }
            WriteSpawn(m, c);
            inMessage++;
            made++;
            if (inMessage == 150) { SendCounted(m, inMessage); m = null; }
        }
        if (m != null) SendCounted(m, inMessage);
        return made;
    }

    static void SendCounted(Msg m, int count)
    {
        m.d[1] = (byte)count;
        m.d[2] = (byte)(count >> 8);
        Net.ToClients(m, true);
    }

    // Stress test: kick every cube at once.
    public void Avalanche()
    {
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active) continue;
            c.rb.WakeUp();
            c.slowTime = 0;
            c.rb.AddForce((Random.insideUnitSphere + Vector3.up) * 5f, ForceMode.VelocityChange);
        }
    }

    public Cube Nearest(Vector3 p, float radius, bool bitsOnly = false)
    {
        Cube best = null;
        float bestSqr = radius * radius;
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active || (bitsOnly && !c.bit)) continue;
            float sqr = (c.go.transform.position - p).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = c; }
        }
        return best;
    }

    // The ground changed under these cubes; sleeping bodies do not notice on their own.
    public void WakeNear(Vector3 min, Vector3 max)
    {
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active) continue;
            Vector3 p = c.go.transform.position;
            if (p.x < min.x - 1 || p.x > max.x + 1 || p.z < min.z - 1 || p.z > max.z + 1) continue;
            c.rb.WakeUp();
            c.slowTime = 0;
        }
    }

    public void ApplyTuning()
    {
        var t = Game.I.tuning;
        physicsMaterial.dynamicFriction = physicsMaterial.staticFriction = t.cubeFriction;
        physicsMaterial.bounciness = t.cubeBounce;
        if (!Net.IsHost) return;
        for (int i = 0; i < Capacity; i++)
            if (all[i] != null && all[i].active) all[i].rb.mass = t.cubeMass * (all[i].bit ? 0.25f : 1f);
    }

    void FixedUpdate()
    {
        if (!Net.IsHost) return;
        var g = Game.I;
        var t = g.tuning;
        float dt = Time.fixedDeltaTime;
        float slow = t.sleepSpeed * t.sleepSpeed;
        int moving = 0;
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active) continue;
            bool awake = !c.rb.IsSleeping();
            // it moved during the last step, so whatever pose clients were told is stale
            if (awake) c.awake = true;
            if (awake)
            {
                Vector3 p = c.rb.position;
                if (p.y < -5f || p.x < 0 || p.z < 0 || p.x > g.ground.SizeX || p.z > g.ground.SizeZ)
                {
                    p = g.ground.Clamp(p, 1f);
                    p.y = g.ground.HeightAt(p.x, p.z) + 1f;
                    c.rb.position = p;
                    c.rb.linearVelocity = Vector3.zero;
                }
                if (c.rb.linearVelocity.sqrMagnitude < slow && c.rb.angularVelocity.sqrMagnitude < slow * 16f)
                {
                    c.slowTime += dt;
                    if (c.slowTime >= t.sleepDelay) { c.rb.Sleep(); awake = false; }
                }
                else c.slowTime = 0;
            }
            if (c.awake && !awake) { rested.Add(c.id); c.flyTime = 0; }
            c.awake = awake;
            if (!awake) continue;
            moving++;

            if (c.flyTime > 0)
            {
                c.flyTime -= dt;
                TryCatch(c);
            }
        }
        Moving = moving;
    }

    // A flying cube that passes close to an empty shovel lands on it.
    void TryCatch(Cube c)
    {
        var g = Game.I;
        float radius = g.tuning.catchRadius;
        Vector3 p = c.rb.position;
        for (int s = 0; s < g.players.Length; s++)
        {
            var player = g.players[s];
            if (player == null || s == c.flySource || player.load != 0 || player.knocked > 0) continue;
            if ((player.ShovelPoint - p).sqrMagnitude > radius * radius) continue;
            if (c.bit) player.SetLoad(c.mat, 1); else player.SetLoad(c.mat, 0);
            Remove(c);
            return;
        }
    }

    void Update()
    {
        if (Net.IsHost) HostSend();
        else if (Net.Running) ClientEase();
    }

    void HostSend()
    {
        if (rested.Count > 0)
        {
            const int perMessage = 150;
            for (int start = 0; start < rested.Count; start += perMessage)
            {
                int count = Mathf.Min(perMessage, rested.Count - start);
                var m = Msg.New(Op.CubeRest, 8 + count * 18);
                m.U32(tick);
                m.U16((ushort)count);
                for (int k = 0; k < count; k++)
                {
                    var c = all[rested[start + k]];
                    m.U16((ushort)c.id);
                    m.V3(c.go.transform.position);
                    m.Rot(c.go.transform.rotation);
                }
                Net.ToClients(m, true);
            }
            rested.Clear();
        }

        syncTimer += Time.unscaledDeltaTime;
        float interval = 1f / Mathf.Max(1f, Game.I.tuning.syncRate);
        if (syncTimer < interval) return;
        syncTimer = Mathf.Min(syncTimer - interval, interval);
        tick++;

        int inPacket = 0;
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active || !c.awake) continue;
            if (inPacket == 0) { snapshot.Reset(Op.CubeSnap); snapshot.U32(tick); snapshot.U8(0); }
            snapshot.U16((ushort)c.id);
            snapshot.Pos16(c.go.transform.position);
            snapshot.Rot(c.go.transform.rotation);
            if (++inPacket == PerSnapshot) { snapshot.d[5] = (byte)inPacket; Net.ToClients(snapshot, false); inPacket = 0; }
        }
        if (inPacket > 0) { snapshot.d[5] = (byte)inPacket; Net.ToClients(snapshot, false); }
    }

    // ---- client

    public void OnSpawn(Msg m)
    {
        int count = m.U16();
        for (int k = 0; k < count; k++)
        {
            int id = m.U16();
            byte kind = m.U8();
            uint at = m.U32();
            Vector3 pos = m.V3();
            Quaternion rot = m.Rot();
            if (all[id] != null && all[id].active) Release(all[id]);
            var c = Take(id, (byte)(kind & 0x7F), (kind & 0x80) != 0, pos, rot);
            c.tick = at;
        }
    }

    public void OnRemove(Msg m)
    {
        int count = m.U16();
        if (count == 0xFFFF) { Clear(); return; }
        for (int k = 0; k < count; k++)
        {
            int id = m.U16();
            if (all[id] != null) Release(all[id]);
        }
    }

    public void OnRest(Msg m)
    {
        uint at = m.U32();
        int count = m.U16();
        for (int k = 0; k < count; k++)
        {
            int id = m.U16();
            Vector3 pos = m.V3();
            Quaternion rot = m.Rot();
            var c = all[id];
            if (c == null || !c.active) continue;
            // A rest pose beats any snapshot from the same tick or earlier.
            c.tick = at + 1;
            c.targetPos = pos;
            c.targetRot = rot;
        }
    }

    public void OnSnapshot(Msg m)
    {
        uint at = m.U32();
        int count = m.U8();
        for (int k = 0; k < count; k++)
        {
            int id = m.U16();
            Vector3 pos = m.Pos16();
            Quaternion rot = m.Rot();
            var c = all[id];
            if (c == null || !c.active || at < c.tick) continue;
            c.tick = at;
            c.targetPos = pos;
            c.targetRot = rot;
        }
    }

    void ClientEase()
    {
        float k = 1f - Mathf.Exp(-18f * Time.deltaTime);
        float worst = 0;
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active) continue;
            var tr = c.go.transform;
            Vector3 p = tr.position;
            float sqr = (p - c.targetPos).sqrMagnitude;
            if (sqr > worst) worst = sqr;
            if (sqr < 1e-8f) continue;
            tr.SetPositionAndRotation(Vector3.Lerp(p, c.targetPos, k), Quaternion.Slerp(tr.rotation, c.targetRot, k));
        }
        clientLag = Mathf.Max(Mathf.Sqrt(worst), clientLag * 0.98f);
    }

    // For comparing instances: where the resting cubes are, to the centimetre.
    public uint RestHash()
    {
        uint hash = 2166136261;
        for (int i = 0; i < Capacity; i++)
        {
            var c = all[i];
            if (c == null || !c.active) continue;
            Vector3 p = Net.IsHost ? c.go.transform.position : c.targetPos;
            hash = (hash ^ (uint)(i * 31 + Mathf.RoundToInt(p.x * 20f) * 7 + Mathf.RoundToInt(p.y * 20f) * 13 + Mathf.RoundToInt(p.z * 20f) * 17)) * 16777619;
        }
        return hash;
    }
}
