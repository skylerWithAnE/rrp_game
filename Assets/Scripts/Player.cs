using UnityEngine;
using UnityEngine.InputSystem;

public struct Controls
{
    public Vector2 move;
    public bool jump, sprint, primary, secondary; // held
    public bool primaryDown, secondaryDown;     // pressed this frame
}

// One blob. The owner moves it and sends its pose; everyone else eases toward the last pose.
// What is on the shovel is decided by the host.
public class Player : MonoBehaviour
{
    public int slot;
    public bool isLocal;
    public int load;            // 0 empty, else a cube material (see Cubes)
    public int bits;            // 1-4 while collecting oil bits, 0 for a whole cube
    public float knocked;       // seconds left lying down
    public bool grounded = true;
    public float yaw;
    public Vector3 velocity;
    public Vector3 target;      // where the shovel will act
    public Aim aim;             // and what is there
    public float hostNextVerb;  // host: rate limit
    public System.Func<Controls> bot; // test driver; replaces keyboard and mouse

    public static bool Sprinting;   // the local player's sprint toggle
    public Vector3 netPos;
    public float netYaw;

    CharacterController controller;
    Blob blob;
    Rigidbody pusher;
    Transform marker;
    float verticalSpeed;
    float cooldown;
    float primaryPress, secondaryPress;   // a fresh click is remembered briefly, through the cooldown
    Vector3 lastPos;

    public Vector3 Forward => Quaternion.Euler(0, yaw, 0) * Vector3.forward;
    public Vector3 ShovelPoint => transform.position + Forward * 1.5f + Vector3.up * 1f;

    public void Init(int slot, bool isLocal, Vector3 position)
    {
        this.slot = slot;
        this.isLocal = isLocal;
        transform.position = netPos = lastPos = position;
        blob = gameObject.AddComponent<Blob>();
        blob.Build(Mats.PlayerColors[slot % Mats.PlayerColors.Length]);

        if (isLocal)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = Blob.Height;
            controller.radius = 0.45f;
            // a controller rests its skin width above the ground; raise the capsule by that much so
            // the feet are on the ground and eye height is true
            controller.center = new Vector3(0, Blob.Height * 0.5f + controller.skinWidth, 0);
            controller.slopeLimit = 50f;
            controller.stepOffset = 0.45f;
            marker = Mats.Part(null, Mats.Cube, Mats.Make(new Color(1f, 1f, 1f)), Vector3.zero, new Vector3(0.6f, 0.04f, 0.6f));
            marker.name = "Target";
            marker.gameObject.SetActive(false);
            blob.Hide();
        }

        if (Net.IsHost)
        {
            // Cubes are pushed by this, not by the character controller. It starts above the feet
            // so a blob can stand on a cube without shoving it.
            var go = new GameObject("Pusher");
            go.transform.position = position;
            pusher = go.AddComponent<Rigidbody>();
            pusher.isKinematic = true;
            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.radius = 0.6f;
            capsule.height = 1.4f;
            capsule.center = new Vector3(0, 1.1f, 0);
            if (controller != null) Physics.IgnoreCollision(controller, capsule);
        }
    }

    void OnDestroy()
    {
        if (pusher != null) Destroy(pusher.gameObject);
        if (marker != null) Destroy(marker.gameObject);
    }

    public void Teleport(Vector3 position)
    {
        if (controller != null) controller.enabled = false;
        transform.position = netPos = lastPos = position;
        if (controller != null) controller.enabled = true;
        verticalSpeed = 0;
    }

    public void SetLoad(int load, int bits)
    {
        this.load = load;
        this.bits = bits;
        blob.SetLoad(load, bits);
        if (!Net.IsHost) return;
        var m = Msg.New(Op.Load, 8);
        m.U8((byte)slot);
        m.U8((byte)load);
        m.U8((byte)bits);
        Net.ToClients(m, true);
    }

    public void Swing(int kind) { blob.Swing(kind); }

    void FixedUpdate()
    {
        if (pusher != null) pusher.MovePosition(transform.position);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (knocked > 0) knocked -= dt;
        if (isLocal) LocalUpdate(dt);
        else
        {
            float k = 1f - Mathf.Exp(-14f * dt);
            transform.position = Vector3.Lerp(transform.position, netPos, k);
            yaw = Mathf.LerpAngle(yaw, netYaw, k);
        }
        transform.rotation = Quaternion.Euler(0, yaw, 0);

        if (dt > 0) velocity = Vector3.Lerp(velocity, (transform.position - lastPos) / dt, 0.5f);
        lastPos = transform.position;
        blob.Tick(velocity, grounded, knocked > 0, dt);
    }

    void LocalUpdate(float dt)
    {
        var g = Game.I;
        var t = g.tuning;
        bool playing = Hud.Playing || bot != null;
        Controls c = bot != null ? bot() : playing ? ReadInput() : default;
        if (knocked > 0) c = default;
        if (bot == null) yaw = Mathf.LerpAngle(yaw, g.cam.yaw, 1f - Mathf.Exp(-16f * dt));

        Vector3 move = Quaternion.Euler(0, bot != null ? yaw : g.cam.yaw, 0) * new Vector3(c.move.x, 0, c.move.y);
        if (move.sqrMagnitude > 1) move.Normalize();
        if (controller.isGrounded)
        {
            verticalSpeed = -2f;
            if (c.jump) verticalSpeed = t.hopSpeed;
        }
        verticalSpeed -= t.gravity * dt;
        float speed = t.walkSpeed * (c.sprint ? t.sprintMultiplier : 1f);
        controller.Move((move * speed + Vector3.up * verticalSpeed) * dt);
        grounded = controller.isGrounded;
        if (transform.position.y < -10f) Teleport(g.yard.Spawn(slot));   // walked off the edge of the yard

        // Everything below is the first prototype's shovel. It only runs in a job, and station 1
        // never starts one.
        if (g.phase != Phase.Job) return;

        // where the shovel acts: under the crosshair, kept within reach
        aim = default;
        if (bot != null) aim.point = transform.position + Forward * Ground.Cell * 1.6f;
        else aim = g.cam.AimAt();
        Vector3 flat = aim.point - transform.position;
        flat.y = 0;
        float distance = Mathf.Clamp(flat.magnitude, Ground.Cell * 0.9f, t.reach);
        // a block or roof earth only counts if the crosshair is on it within reach
        if (aim.kind != Aim.Ground && (flat.magnitude > t.reach + 0.5f || Mathf.Abs(aim.point.y - transform.position.y) > t.reach + 1f)) aim.kind = Aim.Ground;
        flat = flat.sqrMagnitude > 0.0001f ? flat.normalized : Forward;
        target = transform.position + flat * distance;
        target.y = aim.kind != Aim.Ground ? aim.point.y : g.ground.HeightAt(target.x, target.z);
        marker.gameObject.SetActive(true);
        marker.position = target + Vector3.up * 0.03f;

        // Digging and smacking repeat while the button is held. Letting go of a cube (fling, set
        // down) takes a fresh click, so holding the button to dig does not throw each cube away.
        cooldown -= dt;
        primaryPress = c.primaryDown ? 0.3f : primaryPress - dt;
        secondaryPress = c.secondaryDown ? 0.3f : secondaryPress - dt;
        bool carrying = load != 0;
        bool wantPrimary = carrying ? primaryPress > 0 : c.primary;
        bool wantSecondary = carrying ? secondaryPress > 0 : c.secondary;
        if (g.phase != Phase.Job || cooldown > 0 || !(wantPrimary || wantSecondary)) return;
        cooldown = t.digInterval;
        primaryPress = secondaryPress = 0;
        byte verb = wantPrimary ? Verbs.Primary : Verbs.Secondary;
        // play the swing now rather than waiting for the host
        bool loaded = load != 0 && bits == 0;
        Swing(verb == Verbs.Primary ? (loaded ? Blob.SwingFling : Blob.SwingScoop) : (loaded ? Blob.SwingScoop : Blob.SwingSmack));
        Vector3 aimDirection = bot != null ? Forward : g.cam.transform.forward;
        if (Net.IsHost) Verbs.Do(this, verb, target, aimDirection, aim.kind, aim.cell, aim.normal);
        else
        {
            var m = Msg.New(Op.Verb, 32);
            m.U8(verb);
            m.V3(target);
            m.V3(aimDirection);
            m.U8(aim.kind);
            m.U16((ushort)aim.cell.x);
            m.U16((ushort)aim.cell.y);
            m.U16((ushort)aim.cell.z);
            m.U8((byte)(aim.normal.x + 1 + (aim.normal.y + 1) * 3 + (aim.normal.z + 1) * 9));
            Net.ToHost(m, true);
        }
    }

    static Controls ReadInput()
    {
        var c = new Controls();
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null)
        {
            c.move = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            c.jump = kb.spaceKey.isPressed;
            // Shift switches sprinting on and off
            if (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame) Sprinting = !Sprinting;
            c.sprint = Sprinting;
        }
        if (mouse != null)
        {
            c.primary = mouse.leftButton.isPressed;
            c.secondary = mouse.rightButton.isPressed;
            c.primaryDown = mouse.leftButton.wasPressedThisFrame;
            c.secondaryDown = mouse.rightButton.wasPressedThisFrame;
        }
        return c;
    }
}
