using System.Collections.Generic;
using UnityEngine;

// The truck. Nobody drives it: once the towns are joined by asphalt the host sets one off, and it
// shuttles back and forth along the road the road check found. It is a rigidbody on four sprung
// rays, so bad road throws it about. Stuck or flipped, it bounces away and blows up, and a new one
// sets off from the first town. Clients see a smoothed copy.
public class Truck : MonoBehaviour
{
    const float RayLength = 0.55f;
    static readonly Vector3[] Wheels =
    {
        new Vector3(-0.38f, -0.1f, 0.6f), new Vector3(0.38f, -0.1f, 0.6f),
        new Vector3(-0.38f, -0.1f, -0.6f), new Vector3(0.38f, -0.1f, -0.6f),
    };

    public bool alive;
    public int trips;              // how many times it has reached a town
    public int wrecks;

    GameObject body;
    Rigidbody rb;
    int direction = 1;             // +1 toward the far town, -1 back again (it reverses, it does not turn round)
    int index;
    float stuckTime, flippedTime, launchTimer = -1, respawnTimer, sendTimer;
    Vector3 netPos;
    Quaternion netRot = Quaternion.identity;

    public Vector3 Position => body != null ? body.transform.position : Vector3.zero;

    public void Clear()
    {
        if (body != null) Destroy(body);
        body = null;
        alive = false;
        launchTimer = -1;
        respawnTimer = 2f;
        trips = wrecks = 0;
    }

    void Build(Vector3 position, Quaternion rotation)
    {
        body = new GameObject("Truck");
        body.transform.SetPositionAndRotation(position, rotation);
        netPos = position;
        netRot = rotation;
        var box = body.AddComponent<BoxCollider>();
        box.size = new Vector3(0.9f, 0.4f, 1.5f);
        box.center = new Vector3(0, 0.2f, 0);
        // the body slides when it touches down; only the wheels grip
        box.sharedMaterial = new PhysicsMaterial("Truck") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
        rb = body.AddComponent<Rigidbody>();
        rb.mass = 50f;
        rb.isKinematic = !Net.IsHost;
        rb.centerOfMass = new Vector3(0, -0.15f, 0);
        rb.angularDamping = 1.5f;

        var paint = Mats.Make(new Color(0.95f, 0.75f, 0.1f));
        var dark = Mats.Make(new Color(0.1f, 0.1f, 0.12f));
        var glass = Mats.Make(new Color(0.6f, 0.85f, 0.95f));
        Mats.Part(body.transform, Mats.Cube, paint, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.4f, 1.6f));
        Mats.Part(body.transform, Mats.Cube, paint, new Vector3(0, 0.5f, 0.45f), new Vector3(0.8f, 0.45f, 0.55f));
        Mats.Part(body.transform, Mats.Cube, glass, new Vector3(0, 0.55f, 0.74f), new Vector3(0.66f, 0.25f, 0.02f));
        foreach (var w in Wheels)
            Mats.Part(body.transform, Mats.Sphere, dark, new Vector3(w.x * 1.15f, -0.22f, w.z), new Vector3(0.2f, 0.4f, 0.4f));
        alive = true;
    }

    void Update()
    {
        var g = Game.I;
        if (!Net.IsHost)
        {
            if (body != null)
            {
                float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
                body.transform.SetPositionAndRotation(Vector3.Lerp(body.transform.position, netPos, k), Quaternion.Slerp(body.transform.rotation, netRot, k));
            }
            return;
        }
        if (g.phase != Phase.Job) return;

        var path = g.road.path;
        if (!alive)
        {
            if (!g.road.asphaltLinked || path.Count < 4) return;
            respawnTimer -= Time.deltaTime;
            if (respawnTimer > 0) return;
            Vector3 ahead = path[3] - path[0];
            ahead.y = 0;
            Build(path[0] + Vector3.up * 0.8f, Quaternion.LookRotation(ahead));
            direction = 1;
            index = 0;
            stuckTime = flippedTime = 0;
        }
        else if (launchTimer >= 0)
        {
            launchTimer -= Time.deltaTime;
            if (launchTimer < 0) Explode();
        }

        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < 0.05f) return;
        sendTimer = 0;
        var m = Msg.New(Op.Truck, 24);
        m.U8((byte)(alive ? 1 : 0));
        if (alive)
        {
            m.V3(body.transform.position);
            m.Rot(body.transform.rotation);
        }
        Net.ToClients(m, false);
    }

    void FixedUpdate()
    {
        var g = Game.I;
        if (!Net.IsHost || !alive || g.phase != Phase.Job) return;
        var t = g.tuning;
        var tr = body.transform;
        Vector3 up = tr.up;

        // suspension and sideways grip at each wheel
        int grounded = 0;
        foreach (var wheel in Wheels)
        {
            Vector3 origin = tr.TransformPoint(wheel);
            if (!Physics.Raycast(origin, -up, out var hit, RayLength)) continue;
            grounded++;
            Vector3 v = rb.GetPointVelocity(origin);
            float squash = 1f - hit.distance / RayLength;
            float force = t.truckSpring * squash - t.truckDamper * Vector3.Dot(v, up);
            if (force > 0) rb.AddForceAtPosition(up * force, origin);
            rb.AddForceAtPosition(-tr.right * Vector3.Dot(v, tr.right) * 60f, origin);
        }
        if (launchTimer >= 0) return;

        var path = g.road.path;
        Vector3 position = tr.position;
        if (path.Count >= 4)
        {
            // walk along the road; at either end, go back the other way
            // where it is on the road: the closest point a little way ahead of where it last was
            index = Mathf.Clamp(index, 0, path.Count - 1);
            float best = float.MaxValue;
            for (int k = 0, j = index; k < 14 && j >= 0 && j < path.Count; k++, j += direction)
            {
                float distance = Flat(path[j] - position).sqrMagnitude;
                if (distance < best) { best = distance; index = j; }
            }
            bool atEnd = index == (direction > 0 ? path.Count - 1 : 0);
            if (atEnd && Flat(path[index] - position).magnitude < 1.5f)
            {
                direction = -direction;
                trips++;
            }

            if (grounded >= 2)
            {
                // steer at a point just ahead; judge the speed by how much the road bends further on
                Vector3 forward = Flat(tr.forward) * direction;
                Vector3 near = path[Mathf.Clamp(index + 3 * direction, 0, path.Count - 1)];
                Vector3 far = path[Mathf.Clamp(index + 9 * direction, 0, path.Count - 1)];
                float angle = Vector3.SignedAngle(forward, Flat(near - position), Vector3.up);
                float bend = Mathf.Max(Mathf.Abs(angle), Vector3.Angle(forward, Flat(far - position)));
                float turn = Mathf.Clamp(angle * 0.09f, -3f, 3f);
                rb.AddTorque(Vector3.up * (turn - rb.angularVelocity.y) * 10f, ForceMode.Acceleration);
                float wanted = t.truckSpeed * Mathf.Lerp(1f, 0.3f, Mathf.Clamp01(bend / 40f));
                float speed = Vector3.Dot(rb.linearVelocity, tr.forward * direction);
                rb.AddForce(tr.forward * direction * Mathf.Clamp((wanted - speed) * 60f, -t.truckPower, t.truckPower));
            }
        }

        else if (grounded >= 2)
        {
            // the road has gone: brake, and soon give up
            rb.AddForce(-Flat(rb.linearVelocity) * 150f);
        }

        // stuck, flipped or fallen off the world: it bounces away and blows up
        stuckTime = Flat(rb.linearVelocity).magnitude < 0.4f ? stuckTime + Time.fixedDeltaTime : 0;
        flippedTime = up.y < 0.4f ? flippedTime + Time.fixedDeltaTime : 0;
        if (stuckTime > t.truckStuckSeconds || flippedTime > 1.5f || position.y < -5f)
        {
            launchTimer = 1.4f;
            Vector3 away = Random.insideUnitSphere * 3f;
            away.y = 9f;
            rb.AddForce(away, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

    void Explode()
    {
        Vector3 at = body.transform.position;
        var m = Msg.New(Op.Boom, 16);
        m.V3(at);
        Net.ToClients(m, true);
        Boom(at);
        int wrecked = wrecks + 1, done = trips;
        Clear();
        wrecks = wrecked;
        trips = done;
        respawnTimer = 3f;
    }

    // everyone: a flash and a bang
    public static void Boom(Vector3 at)
    {
        Sfx.Play(Sfx.Bang, at);
        var flash = Mats.Part(null, Mats.Sphere, Mats.Make(new Color(1f, 0.55f, 0.1f)), at, Vector3.one * 0.5f);
        flash.gameObject.AddComponent<Flash>();
    }

    // client
    public void OnState(Msg m)
    {
        bool nowAlive = m.U8() != 0;
        if (!nowAlive)
        {
            if (body != null) Clear();
            return;
        }
        Vector3 position = m.V3();
        Quaternion rotation = m.Rot();
        if (body == null) Build(position, rotation);
        netPos = position;
        netRot = rotation;
    }

    // A ball that swells and is gone.
    class Flash : MonoBehaviour
    {
        float age;

        void Update()
        {
            age += Time.deltaTime;
            transform.localScale = Vector3.one * (0.5f + age * 14f);
            if (age > 0.3f) Destroy(gameObject);
        }
    }
}
