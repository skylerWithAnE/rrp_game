using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Station 5, the truck as the judge. Nobody drives it. The host sets one off along a road and it
// follows the right-hand lane from the first stake to the last. It is a rigidbody the size of the
// real truck on four sprung rays, so rough road throws it about, and a slope too steep stops it.
// Its wheels bite best on packed gravel, less on loose gravel and least on bare ground, so a hump
// it would climb on a finished road stops it on a bad one. Stuck or on its side, it bounces away and blows up. Clients see a
// smoothed copy.
//
// Two trucks per road, one each way, each in its own right-hand lane. The example roads (station
// 5's two, the wear road and the hairpins) send them one after another by themselves; the host can
// send a pair down station 2 or 4 from the panel. On the wear road the trucks damage what they
// drive on: see Plot.Wear.
public class Lorries : MonoBehaviour
{
    const float Travel = 1.1f;              // length of a wheel's ray: spring travel plus the wheel's radius
    const float Spring = 11f;               // push per wheel at full squash, as acceleration (m/s2)
    const float Damper = 1.1f;
    const float Grip = 3f;                  // sideways grip per wheel
    const float Wait = 3f;                  // seconds before the next truck sets off

    class Lorry
    {
        public int plot;
        public bool back;                   // drives from the last stake to the first, in the other lane
        public bool wanted;                 // host: keep one on this road
        public bool once;                   // ...but only until it arrives or is wrecked
        public GameObject body;
        public Rigidbody rb;
        public Vector3[] wheels;
        public readonly List<Vector3> path = new List<Vector3>();
        public int index, trips, wrecks;
        public float stuck, flipped, launch = -1, wait, wear;
        public Vector3 netPos;
        public Quaternion netRot = Quaternion.identity;
        public bool Alive => body != null;
    }

    Lorry[] lorries;
    float sendTimer;
    readonly Msg snapshot = new Msg(512);
    readonly List<Vector3> touching = new List<Vector3>();

    void Ensure()
    {
        if (lorries != null) return;
        lorries = new Lorry[Game.I.plots.Length * 2];
        for (int i = 0; i < lorries.Length; i++) lorries[i] = new Lorry { plot = i / 2, back = i % 2 == 1 };
    }

    public void Clear()
    {
        Ensure();
        foreach (var l in lorries)
        {
            if (l.body != null) Destroy(l.body);
            l.body = null;
            l.trips = l.wrecks = 0;
            l.launch = -1;
            l.wait = Wait + (l.back ? 1.5f : 0);
            // the example roads always have trucks on the way
            l.wanted = l.plot >= 3;
            l.once = false;
        }
    }

    // host: a truck each way along this road
    public void Send(int plot)
    {
        Ensure();
        foreach (var l in lorries)
        {
            if (l.plot != plot || l.Alive) continue;
            l.wanted = l.once = true;
            l.wait = 0;
        }
    }

    public void Readout(StringBuilder text, int plot)
    {
        if (lorries == null) return;
        for (int i = plot * 2; i < plot * 2 + 2; i += 2)
        {
            Lorry a = lorries[i], b = lorries[i + 1];
            if (!a.wanted && !b.wanted && a.trips + a.wrecks + b.trips + b.wrecks == 0) continue;
            text.Append("<size=12>trucks ").Append(Plot.Names[a.plot]).Append(": on the road ").Append((a.Alive ? 1 : 0) + (b.Alive ? 1 : 0))
                .Append("   arrived ").Append(a.trips + b.trips).Append("   wrecked ").Append(a.wrecks + b.wrecks).Append("</size>\n");
        }
    }

    public string State()
    {
        var s = new StringBuilder();
        if (lorries == null) return "";
        foreach (var l in lorries)
            if (l.wanted || l.trips + l.wrecks > 0)
                s.Append(' ').Append(l.plot).Append(l.back ? "b:" : "a:").Append(l.Alive ? l.body.transform.position.ToString("0.0") : "none").Append('t').Append(l.trips).Append('w').Append(l.wrecks);
        return s.ToString();
    }

    void Build(Lorry l, Vector3 position, Quaternion rotation)
    {
        var g = Game.I;
        var t = g.tuning;
        l.body = new GameObject("Lorry");
        l.body.transform.SetParent(transform, false);
        l.body.transform.SetPositionAndRotation(position, rotation);
        l.netPos = position;
        l.netRot = rotation;
        g.yard.MakeTruck(l.body.transform, t, false);

        float w = t.truckWidth, length = t.truckLength, h = t.truckHeight, clearance = t.truckWheel * 0.6f;
        var box = l.body.AddComponent<BoxCollider>();
        box.size = new Vector3(w, h - clearance, length);
        box.center = new Vector3(0, (h + clearance) * 0.5f, 0);
        // the body slides when it touches down; only the wheels grip
        box.sharedMaterial = new PhysicsMaterial("Lorry") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
        l.rb = l.body.AddComponent<Rigidbody>();
        l.rb.mass = 3600f;
        l.rb.isKinematic = !Net.IsHost;
        l.rb.centerOfMass = new Vector3(0, 0.7f, 0);
        l.rb.angularDamping = 1.5f;

        // the same axles as the model: see Yard.MakeTruck
        float front = length * 0.5f - length * 0.13f, rear = front - length * 0.66f, track = w * 0.5f - 0.3f;
        float top = Travel - 0.25f;     // at rest the springs are squashed a little under a quarter
        l.wheels = new[] { new Vector3(-track, top, front), new Vector3(track, top, front), new Vector3(-track, top, rear), new Vector3(track, top, rear) };
        l.index = 0;
        l.stuck = l.flipped = 0;
        l.launch = -1;
    }

    void Remove(Lorry l)
    {
        if (l.body != null) Destroy(l.body);
        l.body = null;
        l.launch = -1;
        l.wait = Wait;
        if (l.once) l.wanted = l.once = false;
    }

    void Update()
    {
        var g = Game.I;
        if (lorries == null || g.phase != Phase.Lobby) return;
        if (!Net.IsHost)
        {
            float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
            foreach (var l in lorries)
                if (l.Alive) l.body.transform.SetPositionAndRotation(Vector3.Lerp(l.body.transform.position, l.netPos, k), Quaternion.Slerp(l.body.transform.rotation, l.netRot, k));
            return;
        }

        foreach (var l in lorries)
        {
            if (!l.Alive)
            {
                if (!l.wanted) continue;
                l.wait -= Time.deltaTime;
                if (l.wait > 0 || !g.plots[l.plot].Route(l.path, l.back)) continue;
                Build(l, l.path[0] + Vector3.up * 0.3f, Quaternion.LookRotation(l.path[4] - l.path[0]));
            }
            else if (l.launch >= 0)
            {
                l.launch -= Time.deltaTime;
                if (l.launch >= 0) continue;
                Vector3 at = l.body.transform.position + Vector3.up;
                var m = Msg.New(Op.Boom, 16);
                m.V3(at);
                Net.ToClients(m, true);
                Truck.Boom(at);
                l.wrecks++;
                Remove(l);
            }
        }

        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < 0.05f) return;
        sendTimer = 0;
        snapshot.Reset(Op.Lorry);
        foreach (var l in lorries)
        {
            snapshot.U8((byte)(l.Alive ? 1 : 0));
            snapshot.U8((byte)l.trips);
            snapshot.U8((byte)l.wrecks);
            if (!l.Alive) continue;
            snapshot.V3(l.body.transform.position);
            snapshot.Rot(l.body.transform.rotation);
        }
        Net.ToClients(snapshot, false);
    }

    public void OnState(Msg m)
    {
        Ensure();
        foreach (var l in lorries)
        {
            bool alive = m.U8() != 0;
            l.trips = m.U8();
            l.wrecks = m.U8();
            l.wanted = alive;
            if (!alive)
            {
                if (l.Alive) { Destroy(l.body); l.body = null; }
                continue;
            }
            Vector3 position = m.V3();
            Quaternion rotation = m.Rot();
            if (!l.Alive) Build(l, position, rotation);
            l.netPos = position;
            l.netRot = rotation;
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    void FixedUpdate()
    {
        var g = Game.I;
        if (lorries == null || !Net.IsHost || g.phase != Phase.Lobby) return;
        var t = g.tuning;
        foreach (var l in lorries)
        {
            if (!l.Alive) continue;
            var tr = l.body.transform;
            var rb = l.rb;
            Vector3 up = tr.up;
            float mass = rb.mass;

            // suspension and sideways grip at each wheel
            int grounded = 0;
            touching.Clear();
            foreach (var wheel in l.wheels)
            {
                Vector3 origin = tr.TransformPoint(wheel);
                if (!Physics.Raycast(origin, -up, out var hit, Travel, ~0, QueryTriggerInteraction.Ignore)) continue;
                grounded++;
                touching.Add(hit.point);
                Vector3 v = rb.GetPointVelocity(origin);
                float squash = 1f - hit.distance / Travel;
                float force = Spring * squash - Damper * Vector3.Dot(v, up);
                if (force > 0) rb.AddForceAtPosition(up * force * mass, origin);
                rb.AddForceAtPosition(-tr.right * Vector3.Dot(v, tr.right) * Grip * mass * 0.25f, origin);
            }
            if (l.launch >= 0) continue;

            // four times a second, a truck on the wear road damages the square it is on
            l.wear += Time.fixedDeltaTime;
            if (g.plots[l.plot].Wears && l.wear >= 0.25f && touching.Count > 0)
            {
                l.wear = 0;
                g.plots[l.plot].Wear(tr.position, touching);
            }

            Vector3 position = tr.position;
            var path = l.path;
            // where it is on its way: the closest point a little ahead of where it last was
            float best = float.MaxValue;
            for (int k = 0, j = l.index; k < 14 && j < path.Count; k++, j++)
            {
                float distance = Flat(path[j] - position).sqrMagnitude;
                if (distance < best) { best = distance; l.index = j; }
            }
            if (l.index >= path.Count - 2)
            {
                // it made it
                l.trips++;
                Remove(l);
                continue;
            }

            if (grounded >= 2)
            {
                // steer at a point just ahead; slow for bends and for poor going
                Vector3 forward = Flat(tr.forward);
                Vector3 near = path[Mathf.Min(l.index + 5, path.Count - 1)], far = path[Mathf.Min(l.index + 12, path.Count - 1)];
                float angle = Vector3.SignedAngle(forward, Flat(near - position), Vector3.up);
                float bend = Mathf.Max(Mathf.Abs(angle), Vector3.Angle(forward, Flat(far - position)));
                float turn = Mathf.Clamp(angle * 0.06f, -1.5f, 1.5f);
                rb.AddTorque(Vector3.up * (turn - rb.angularVelocity.y) * 6f, ForceMode.Acceleration);
                float wanted = t.lorrySpeed * Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(bend / 40f));
                float speed = Vector3.Dot(rb.linearVelocity, tr.forward);
                // the wheels only bite as well as the surface lets them
                float push = t.lorryPower * g.plots[l.plot].Going(position.x, position.z);
                rb.AddForce(tr.forward * Mathf.Clamp((wanted - speed) * 4f, -t.lorryPower, push) * mass);
            }

            // stuck, on its side or fallen off the world: it bounces away and blows up
            l.stuck = Flat(rb.linearVelocity).magnitude < 0.3f ? l.stuck + Time.fixedDeltaTime : 0;
            l.flipped = up.y < 0.45f ? l.flipped + Time.fixedDeltaTime : 0;
            if (l.stuck > t.lorryStuckSeconds || l.flipped > 1.5f || position.y < -5f)
            {
                l.launch = 1.4f;
                Vector3 away = Random.insideUnitSphere * 3f;
                away.y = 9f;
                rb.AddForce(away, ForceMode.VelocityChange);
                rb.AddTorque(Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
            }
        }
    }
}
