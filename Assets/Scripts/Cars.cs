using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Vehicles a player drives: a pick-up, a road roller, a front loader and a paint truck, on the
// Driving test ground. A proposal for how driving works, to be played.
//
// E beside one gets in, and E gets out. W and S drive and reverse, A and D steer. The view is
// from the driver's seat and turns with the vehicle. The loader's bucket goes up with R and down
// with F; driven into the gravel pile with the bucket down it fills, and a left click tips it.
// A left click in the paint truck turns its sprayers on and off.
//
// Whoever is driving a vehicle works out its motion on their own machine and sends where it is,
// the way players do for themselves, so the wheel answers at once. A vehicle nobody is driving
// is the host's. What a vehicle does to the ground is decided by the host from where it is: the
// roller packs gravel and rolls asphalt under it, the loader's gravel is laid where it is
// tipped on a road, or left in a heap anywhere else, and the paint truck paints the lines of
// the rolled asphalt it drives over.
public class Cars : MonoBehaviour
{
    public const int Pickup = 0, Roller = 1, Loader = 2, Painter = 3;
    static readonly string[] Names = { "the pick-up", "the roller", "the loader", "the paint truck" };
    const float Travel = 0.9f;      // length of a wheel's ray
    const float Spring = 12f, Damper = 1.4f, Grip = 5f;

    public class Car
    {
        public int kind, driver = -1, load;
        public GameObject body;
        public Rigidbody rb;
        public Vector3[] wheels;
        public Vector3 size, seat, home;        // width, height, length; where the driver's feet are; where it starts
        public Transform arm;                   // the loader's arms and bucket
        public float bucket, netBucket;         // 0 on the ground, 1 right up
        public Vector3 netPos;
        public Quaternion netRot = Quaternion.identity;
        public float flipped, lastYaw;
        public Vector3 lastWhite, lastYellow;   // host: where the paint truck's two nozzles last were
        public bool sprayed;
    }

    public Car[] cars = new Car[0];
    public static int Mine = -1;                // the vehicle the local player is driving
    public static float TestThrottle, TestSteer;    // test tooling: the controls held by a script
    public readonly List<Vector4> heaps = new List<Vector4>();     // gravel tipped off a road: where, and how many buckets
    readonly List<Transform> heapParts = new List<Transform>();
    public Vector3 pile;                        // the gravel pile the loader fills from
    const float PileRadius = 4.5f;
    Material heapMaterial;
    float sendTimer, workTimer;
    float throttle, steer;
    readonly Msg snapshot = new Msg(512);
    readonly List<Vector3> under = new List<Vector3>();

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        cars = new Car[0];
        heaps.Clear();
        heapParts.Clear();
        Mine = -1;
    }

    // where the paint truck's two nozzles are, in its own terms: the white one out to its right,
    // over the edge line when the truck is in the middle of its lane, and the yellow one out to
    // its left, over the centre line
    static readonly Vector3 WhiteNozzle = new Vector3(1.375f, 0, -2.5f), YellowNozzle = new Vector3(-1.75f, 0, -2.5f);
    bool painting;      // the Painting ground: one paint truck, which sprays where it actually is

    // Every machine builds the same vehicles in the same places, beside where players start:
    // the four of the Driving ground, or, on the Painting ground, a paint truck alone.
    public void Build(bool paintOnly)
    {
        Clear();
        painting = paintOnly;
        var t = Game.I.tuning;
        heapMaterial = Mats.Make(new Color(0.69f, 0.69f, 0.67f), true);
        cars = new Car[paintOnly ? 1 : 4];
        for (int i = 0; i < cars.Length; i++)
        {
            int kind = paintOnly ? Painter : i;
            var car = cars[i] = new Car { kind = kind };
            car.size = kind == Pickup || kind == Painter ? new Vector3(2.0f, 1.9f, 5.4f) : kind == Roller ? new Vector3(2.4f, 3.0f, 4.6f) : new Vector3(2.6f, 3.3f, 6.2f);
            car.seat = kind == Pickup || kind == Painter ? new Vector3(-0.45f, 0.15f, 0.5f) : kind == Roller ? new Vector3(0, 1.25f, -0.2f) : new Vector3(0, 1.5f, -0.6f);
            car.home = new Vector3(13f + kind * 6f, 0.5f, 8f);
            // on the Painting ground it stands on the near end of the marked strip, in its right-hand lane
            if (paintOnly) car.home = new Vector3(-t.laneWidth - 5f - (t.laneWidth + t.shoulderWidth) - 12f + t.laneWidth * 0.5f, 2f, 5f);
            car.body = new GameObject(Names[kind]);
            car.body.transform.SetParent(transform, false);
            car.body.transform.position = car.netPos = car.home;
            Shape(car, t);
            float clearance = 0.4f;
            var box = car.body.AddComponent<BoxCollider>();
            box.size = new Vector3(car.size.x, car.size.y - clearance, car.size.z);
            box.center = new Vector3(0, (car.size.y + clearance) * 0.5f, 0);
            box.sharedMaterial = new PhysicsMaterial("Car") { dynamicFriction = 0, staticFriction = 0, frictionCombine = PhysicsMaterialCombine.Minimum };
            car.rb = car.body.AddComponent<Rigidbody>();
            car.rb.mass = kind == Pickup ? 2000f : 6000f;
            car.rb.centerOfMass = new Vector3(0, 0.6f, 0);
            car.rb.angularDamping = 2f;
            float track = car.size.x * 0.5f - 0.25f, axle = car.size.z * 0.33f, top = Travel - 0.3f;
            car.wheels = new[] { new Vector3(-track, top, axle), new Vector3(track, top, axle), new Vector3(-track, top, -axle), new Vector3(track, top, -axle) };
        }
        if (paintOnly) return;
        // the gravel pile: no collider, so the loader can drive into it
        pile = new Vector3(34f, 0, 24f);
        for (int n = 0; n < 7; n++)
        {
            float radius = 1.6f + n % 3 * 0.7f;
            Mats.Part(transform, Mats.Sphere, heapMaterial, pile + new Vector3((n % 3 - 1) * 2.2f, radius * 0.25f, (n / 3 - 1) * 2.2f), new Vector3(radius * 2f, radius * 1.3f, radius * 2f));
        }
    }

    // Grey boxes, like the truck: just enough to tell the three apart and see which way they face.
    void Shape(Car car, Tuning t)
    {
        var root = car.body.transform;
        var tyre = Mats.Make(new Color(0.12f, 0.12f, 0.13f));
        var glass = Mats.Make(new Color(0.20f, 0.22f, 0.26f));
        float w = car.size.x, h = car.size.y, l = car.size.z;
        if (car.kind == Roller) { Game.I.yard.MakeRoller(root, t); return; }
        var paint = Mats.Make(car.kind == Pickup ? new Color(0.25f, 0.45f, 0.75f) : car.kind == Painter ? new Color(0.92f, 0.92f, 0.90f) : new Color(0.95f, 0.75f, 0.10f));
        if (car.kind == Pickup || car.kind == Painter)
        {
            // the paint truck is the pick-up in white with a yellow tank in its bed
            if (car.kind == Painter) Box(root, Mats.Make(new Color(0.93f, 0.78f, 0.15f)), w * 0.7f, 0.8f, l * 0.3f, new Vector3(0, 1.55f, -l * 0.3f));
            Box(root, paint, w, 0.7f, l, new Vector3(0, 0.75f, 0));                         // the body, bed and all
            Box(root, paint, w, 0.75f, l * 0.34f, new Vector3(0, 1.45f, l * 0.08f));        // the cab
            Box(root, glass, w + 0.02f, 0.4f, l * 0.3f, new Vector3(0, 1.52f, l * 0.08f));
            Box(root, tyre, w * 0.9f, 0.05f, l * 0.3f, new Vector3(0, 1.12f, -l * 0.3f));   // the bed's floor
            if (car.kind == Painter && painting)
            {
                // a boom across its tail with a nozzle at each end: white on the right, yellow on the left
                Box(root, tyre, WhiteNozzle.x - YellowNozzle.x, 0.08f, 0.08f, new Vector3((WhiteNozzle.x + YellowNozzle.x) * 0.5f, 0.6f, WhiteNozzle.z));
                Box(root, Mats.Make(new Color(0.93f, 0.93f, 0.9f)), 0.16f, 0.4f, 0.16f, new Vector3(WhiteNozzle.x, 0.4f, WhiteNozzle.z));
                Box(root, Mats.Make(new Color(0.93f, 0.78f, 0.15f)), 0.16f, 0.4f, 0.16f, new Vector3(YellowNozzle.x, 0.4f, YellowNozzle.z));
            }
        }
        else
        {
            Box(root, paint, w * 0.8f, 1.2f, l * 0.55f, new Vector3(0, 1.4f, -l * 0.15f));  // the body
            Box(root, glass, w * 0.6f, 1.1f, l * 0.22f, new Vector3(0, 2.5f, -l * 0.12f));  // the cab
            car.arm = new GameObject("Arm").transform;
            car.arm.SetParent(root, false);
            car.arm.localPosition = new Vector3(0, 1.6f, l * 0.1f);
            for (int side = -1; side <= 1; side += 2) Box(car.arm, paint, 0.2f, 0.2f, 2.6f, new Vector3(side * w * 0.38f, 0, 1.3f));
            Box(car.arm, tyre, w, 0.9f, 0.12f, new Vector3(0, -0.1f, 2.5f));                // the bucket: back, floor and lip
            Box(car.arm, tyre, w, 0.12f, 1.0f, new Vector3(0, -0.55f, 3.0f));
        }
        float wheel = car.kind == Pickup ? 0.75f : 1.5f, track = w * 0.5f - 0.2f, axle = l * 0.33f;
        for (int k = 0; k < 4; k++)
        {
            var part = Mats.Part(root, Mats.Cylinder, tyre, new Vector3(k % 2 == 0 ? -track : track, wheel * 0.5f, k < 2 ? axle : -axle), new Vector3(wheel, 0.2f, wheel));
            part.localRotation = Quaternion.Euler(0, 0, 90f);
        }
    }

    static void Box(Transform parent, Material material, float x, float y, float z, Vector3 centre)
    {
        Mats.Part(parent, Mats.Cube, material, centre, new Vector3(x, y, z));
    }

    float TopSpeed(Car car, Tuning t) { return car.kind == Pickup ? t.pickupSpeed : car.kind == Roller ? t.rollerDriveSpeed : car.kind == Painter ? t.painterSpeed : t.loaderSpeed; }

    // where someone getting out of a vehicle stands: clear of its left side
    public Vector3 ExitSpot(int car)
    {
        var tr = cars[car].body.transform;
        return tr.position - tr.right * (cars[car].size.x * 0.5f + 1.4f) + Vector3.up * 0.4f;
    }

    // where the driver's feet are, in the world
    public Vector3 Seat(int car) { return cars[car].body.transform.TransformPoint(cars[car].seat); }

    // ---- getting in and out, and the controls

    void Update()
    {
        var g = Game.I;
        if (cars.Length == 0 || g.phase != Phase.Lobby) return;
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        // which one is mine is whatever the host last said
        Mine = -1;
        for (int i = 0; i < cars.Length; i++) if (cars[i].driver == g.localSlot && g.localSlot >= 0) Mine = i;

        throttle = steer = 0;
        if (g.local != null && Hud.Playing && kb != null)
        {
            if (Mine < 0)
            {
                int near = -1;
                float best = 4.5f;
                for (int i = 0; i < cars.Length; i++)
                {
                    float distance = Vector3.Distance(g.local.transform.position, cars[i].body.transform.position);
                    if (cars[i].driver < 0 && distance < best) { best = distance; near = i; }
                }
                if (near >= 0)
                {
                    Plot.Hint("E: drive " + Names[cars[near].kind]);
                    if (kb.eKey.wasPressedThisFrame) Ask(near, 1);
                }
            }
            else
            {
                var car = cars[Mine];
                throttle = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
                steer = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
                string keys = "W S drive   A D steer   E get out";
                if (car.kind == Loader)
                {
                    car.bucket = Mathf.Clamp01(car.bucket + ((kb.rKey.isPressed ? 1 : 0) - (kb.fKey.isPressed ? 1 : 0)) * 0.7f * Time.deltaTime);
                    keys += "   R F bucket up, down   " + (car.load > 0 ? "left click tips the bucket" : "drive into the gravel pile with the bucket down to fill it");
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame && car.load > 0) Ask(Mine, 2);
                }
                else if (car.kind == Roller) keys += "   it packs gravel and rolls asphalt under it";
                else if (car.kind == Painter && painting)
                {
                    // it sprays where it is: a nozzle on each side, each on its own button
                    keys += "   left click: white nozzle (right side) " + ((car.load & 1) != 0 ? "ON" : "off") + "   right click: yellow nozzle (left side) " + ((car.load & 2) != 0 ? "ON" : "off");
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame) Ask(Mine, 3);
                    if (mouse != null && mouse.rightButton.wasPressedThisFrame) Ask(Mine, 4);
                }
                else if (car.kind == Painter)
                {
                    keys += "   left click: paint " + (car.load > 0 ? "ON, it paints the rolled asphalt under it" : "off");
                    if (mouse != null && mouse.leftButton.wasPressedThisFrame) Ask(Mine, 3);
                }
                Plot.Hint(keys);
                if (kb.eKey.wasPressedThisFrame) Ask(Mine, 0);
                // the view turns with the vehicle
                float yaw = car.body.transform.eulerAngles.y;
                g.cam.yaw += Mathf.DeltaAngle(car.lastYaw, yaw);
                car.lastYaw = yaw;
            }
        }

        // every vehicle: whose it is to move, the bucket, and those that are someone else's
        float k = 1f - Mathf.Exp(-14f * Time.deltaTime);
        for (int i = 0; i < cars.Length; i++)
        {
            var car = cars[i];
            bool mine = Simulated(car);
            car.rb.isKinematic = !mine;
            if (!mine) car.body.transform.SetPositionAndRotation(Vector3.Lerp(car.body.transform.position, car.netPos, k), Quaternion.Slerp(car.body.transform.rotation, car.netRot, k));
            if (i != Mine) car.bucket = Mathf.Lerp(car.bucket, car.netBucket, k);
            if (car.arm != null) car.arm.localRotation = Quaternion.Euler(-car.bucket * 55f + 22f, 0, 0);
            if (i != Mine) car.lastYaw = car.body.transform.eulerAngles.y;
        }

        if (Net.IsHost)
        {
            // a blob in a seat must not shove the vehicle it is sitting in
            foreach (var p in g.players)
            {
                if (p == null) continue;
                bool riding = false;
                foreach (var car in cars) if (car.driver == p.slot) riding = true;
                p.Riding(riding);
            }
            // a driver who has left the game leaves the vehicle too
            foreach (var car in cars) if (car.driver >= 0 && g.players[car.driver] == null) car.driver = -1;
            workTimer += Time.deltaTime;
            if (workTimer >= 0.25f) { workTimer = 0; Work(g); }
        }

        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < 0.05f) return;
        sendTimer = 0;
        if (Net.IsHost) { if (painting) Spray(g); SendAll(); }
        else if (Mine >= 0)
        {
            var m = Msg.New(Op.CarPose, 32);
            m.U8((byte)Mine);
            m.V3(cars[Mine].body.transform.position);
            m.Rot(cars[Mine].body.transform.rotation);
            m.U8((byte)Mathf.RoundToInt(cars[Mine].bucket * 255f));
            Net.ToHost(m, false);
        }
    }

    // host, on the Painting ground: each nozzle that is on leaves a stripe from where it last was to where it is now
    void Spray(Game g)
    {
        foreach (var car in cars)
        {
            if (car.kind != Painter) continue;
            var tr = car.body.transform;
            Vector3 white = tr.TransformPoint(WhiteNozzle), yellow = tr.TransformPoint(YellowNozzle);
            Lines lines = null;
            foreach (var plot in g.plots)
                if (plot.Ready && plot.lines != null && plot.Covers(tr.position.x, tr.position.z)) lines = plot.lines;
            if (lines != null && car.sprayed)
            {
                if ((car.load & 1) != 0 && (white - car.lastWhite).sqrMagnitude < 9f) lines.Stroke(Lines.White, car.lastWhite, white, g.tuning.sprayWidth);
                if ((car.load & 2) != 0 && (yellow - car.lastYellow).sqrMagnitude < 9f) lines.Stroke(Lines.Yellow, car.lastYellow, yellow, g.tuning.sprayWidth);
                lines.Flush();
            }
            car.lastWhite = white;
            car.lastYellow = yellow;
            car.sprayed = lines != null;
        }
    }

    // is this machine the one that works out this vehicle's motion?
    bool Simulated(Car car) { return car.driver >= 0 ? car.driver == Game.I.localSlot : Net.IsHost; }

    // ask the host: 1 get in, 0 get out, 2 tip the bucket
    public void Ask(int car, int what)
    {
        if (Net.IsHost) { HostAsk(Game.I.localSlot, car, what); return; }
        var m = Msg.New(Op.Car, 4);
        m.U8((byte)car);
        m.U8((byte)what);
        Net.ToHost(m, true);
    }

    public void HostAsk(int slot, int index, int what)
    {
        if (index < 0 || index >= cars.Length) return;
        var car = cars[index];
        if (what == 1)
        {
            foreach (var other in cars) if (other.driver == slot) return;    // one vehicle each
            if (car.driver < 0) car.driver = slot;
        }
        else if (car.driver != slot) return;
        else if (what == 0) car.driver = -1;
        else if (what == 3 && car.kind == Painter) car.load ^= 1;      // its sprayers, on and off
        else if (what == 4 && car.kind == Painter && painting) car.load ^= 2;     // on the Painting ground the yellow nozzle has a button of its own
        else if (what == 2 && car.kind == Loader && car.load > 0)
        {
            // gravel tipped on a road that is on its line is laid there; anywhere else it is a heap
            car.load = 0;
            Vector3 at = Tip(car);
            foreach (var plot in Game.I.plots)
                if (plot.Ready && plot.Covers(at.x, at.z) && plot.Tip(at, 2.2f)) return;
            for (int h = 0; h < heaps.Count; h++)
                if (Vector3.Distance(new Vector3(heaps[h].x, heaps[h].y, heaps[h].z), at) < 4f) { heaps[h] += new Vector4(0, 0, 0, 1); return; }
            if (heaps.Count < 12) heaps.Add(new Vector4(at.x, 0, at.z, 1));
        }
    }

    // where the loader's bucket is, on the ground
    static Vector3 Tip(Car car)
    {
        Vector3 at = car.body.transform.position + car.body.transform.forward * (car.size.z * 0.5f + 1.2f);
        at.y = 0;
        return at;
    }

    // host, four times a second: what each vehicle does to the world from where it is
    void Work(Game g)
    {
        foreach (var car in cars)
        {
            var tr = car.body.transform;
            if (car.kind == Loader && car.load == 0 && car.bucket < 0.2f && Vector3.Distance(Tip(car), pile) < PileRadius) car.load = 1;
            if (car.kind != Roller && !(car.kind == Painter && car.load > 0)) continue;
            under.Clear();
            foreach (var wheel in car.wheels) under.Add(tr.TransformPoint(new Vector3(wheel.x, 0, wheel.z)));
            foreach (var plot in g.plots)
            {
                if (!plot.Ready || !plot.Covers(tr.position.x, tr.position.z)) continue;
                if (car.kind == Painter) { if (plot.lines == null) plot.PaintUnder(under); continue; }
                plot.Pack(under);
                plot.Roll(under);
            }
        }
    }

    // ---- motion, on whichever machine a vehicle belongs to

    void FixedUpdate()
    {
        var g = Game.I;
        if (cars.Length == 0 || g.phase != Phase.Lobby) return;
        var t = g.tuning;
        for (int i = 0; i < cars.Length; i++)
        {
            var car = cars[i];
            if (!Simulated(car) || car.rb.isKinematic) continue;
            var tr = car.body.transform;
            var rb = car.rb;
            Vector3 up = tr.up;
            float mass = rb.mass;
            // on a lighter planet the wheels bite less and the springs are softer, as the trucks' are
            float bite = Game.Bite(t), springs = Game.Springs(t), damping = Mathf.Sqrt(springs);
            int grounded = 0;
            foreach (var wheel in car.wheels)
            {
                Vector3 origin = tr.TransformPoint(wheel);
                if (!Physics.Raycast(origin, -up, out var hit, Travel, ~0, QueryTriggerInteraction.Ignore) || hit.collider.gameObject == car.body) continue;
                grounded++;
                Vector3 v = rb.GetPointVelocity(origin);
                float force = Spring * springs * (1f - hit.distance / Travel) - Damper * damping * Vector3.Dot(v, up);
                if (force > 0) rb.AddForceAtPosition(up * force * mass, origin);
                rb.AddForceAtPosition(-tr.right * Vector3.Dot(v, tr.right) * Grip * bite * mass * 0.25f, origin);
            }
            bool driven = i == Mine;
            float go = driven ? throttle + TestThrottle : 0, turn = driven ? steer + TestSteer : 0;
            if (grounded >= 2)
            {
                float top = TopSpeed(car, t), speed = Vector3.Dot(rb.linearVelocity, tr.forward);
                float wanted = go > 0 ? top : go < 0 ? -top * 0.4f : 0;
                rb.AddForce(tr.forward * Mathf.Clamp((wanted - speed) * 4f, -t.drivePower * bite, t.drivePower * bite) * mass);
                // it steers only as it rolls, and the other way in reverse
                float rate = turn * t.driveTurn * Mathf.Clamp(speed / 3f, -1f, 1f);
                rb.AddTorque(Vector3.up * (rate - rb.angularVelocity.y) * 6f, ForceMode.Acceleration);
            }
            // on its side for two seconds, or off the edge of the world: set it back on its wheels
            car.flipped = up.y < 0.4f ? car.flipped + Time.fixedDeltaTime : 0;
            if (car.flipped > 2f || tr.position.y < -8f)
            {
                car.flipped = 0;
                Vector3 at = tr.position.y < -8f ? car.home : tr.position + Vector3.up * 1.5f;
                rb.position = at;
                rb.rotation = Quaternion.Euler(0, tr.eulerAngles.y, 0);
                rb.linearVelocity = rb.angularVelocity = Vector3.zero;
            }
        }
    }

    // ---- keeping every machine in step

    void SendAll()
    {
        snapshot.Reset(Op.Cars);
        snapshot.U8((byte)cars.Length);
        foreach (var car in cars)
        {
            snapshot.U8((byte)(car.driver + 1));
            snapshot.U8((byte)car.load);
            snapshot.U8((byte)Mathf.RoundToInt(car.bucket * 255f));
            snapshot.V3(car.body.transform.position);
            snapshot.Rot(car.body.transform.rotation);
        }
        snapshot.U8((byte)heaps.Count);
        foreach (var heap in heaps) { snapshot.F32(heap.x); snapshot.F32(heap.z); snapshot.U8((byte)Mathf.Min(255, heap.w)); }
        Net.ToClients(snapshot, false);
        ShowHeaps();
    }

    // client
    public void OnState(Msg m)
    {
        int n = m.U8();
        for (int i = 0; i < n; i++)
        {
            int driver = m.U8() - 1, load = m.U8();
            float bucket = m.U8() / 255f;
            Vector3 position = m.V3();
            Quaternion rotation = m.Rot();
            if (i >= cars.Length) continue;
            var car = cars[i];
            car.driver = driver;
            car.load = load;
            car.netPos = position;
            car.netRot = rotation;
            car.netBucket = bucket;
        }
        heaps.Clear();
        n = m.U8();
        for (int i = 0; i < n; i++) { float x = m.F32(), z = m.F32(); heaps.Add(new Vector4(x, 0, z, m.U8())); }
        ShowHeaps();
    }

    // host: where a client says the vehicle it is driving is
    public void OnPose(int slot, Msg m)
    {
        int index = m.U8();
        Vector3 position = m.V3();
        Quaternion rotation = m.Rot();
        float bucket = m.U8() / 255f;
        if (index >= cars.Length || cars[index].driver != slot) return;
        cars[index].netPos = position;
        cars[index].netRot = rotation;
        cars[index].netBucket = bucket;
    }

    void ShowHeaps()
    {
        while (heapParts.Count < heaps.Count) heapParts.Add(Mats.Part(transform, Mats.Sphere, heapMaterial, Vector3.zero, Vector3.one));
        for (int i = 0; i < heapParts.Count; i++)
        {
            heapParts[i].gameObject.SetActive(i < heaps.Count);
            if (i >= heaps.Count) continue;
            float size = Mathf.Pow(heaps[i].w, 1f / 3f) * 1.3f;
            heapParts[i].position = new Vector3(heaps[i].x, size * 0.2f, heaps[i].z);
            heapParts[i].localScale = new Vector3(size * 2f, size * 1.2f, size * 2f);
        }
    }

    public string State()
    {
        var s = new System.Text.StringBuilder();
        foreach (var car in cars) s.Append(' ').Append(car.kind).Append(':').Append(car.body.transform.position.ToString("0.0")).Append('d').Append(car.driver).Append('l').Append(car.load);
        s.Append(" heaps=").Append(heaps.Count);
        return s.ToString();
    }
}
