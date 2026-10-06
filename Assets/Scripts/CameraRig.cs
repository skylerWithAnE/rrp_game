using UnityEngine;
using UnityEngine.InputSystem;

// What the crosshair is on: the ground, a block, or earth lying on top of blocks.
public struct Aim
{
    public const byte Ground = 0, Block = 1, Earth = 2;
    public Vector3 point;
    public byte kind;
    public Vector3Int cell;     // the block that was hit
    public Vector3Int normal;   // the face of it
}

// First-person camera: sits at the local blob's eye and turns with the mouse.
public class CameraRig : MonoBehaviour
{
    public float yaw, pitch;
    Camera cam;
    static readonly RaycastHit[] hits = new RaycastHit[48];

    void Awake()
    {
        cam = gameObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.56f, 0.76f, 0.93f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        gameObject.AddComponent<AudioListener>();
        transform.position = new Vector3(0, 8, -10);
    }

    void LateUpdate()
    {
        var g = Game.I;
        var p = g.local;
        if (p == null) return;

        if (Hud.Playing && Mouse.current != null)
        {
            Vector2 look = Mouse.current.delta.ReadValue();
            yaw += look.x * 0.12f;
            pitch = Mathf.Clamp(pitch - look.y * 0.12f, -85f, 85f);
        }

        // the tuned field of view is horizontal; Unity's camera takes a vertical one
        cam.fieldOfView = Camera.HorizontalToVerticalFieldOfView(g.tuning.fieldOfView, cam.aspect);
        var facing = Quaternion.Euler(pitch, yaw, 0);
        if (Cars.Outside && Cars.Mine < g.cars.cars.Length)
        {
            // In a vehicle: from behind and above it, turning with the mouse round a point over
            // its roof, and no further back than the ground or anything else solid allows.
            var car = g.cars.cars[Cars.Mine];
            Vector3 pivot = car.body.transform.position + Vector3.up * (car.size.y + 0.6f);
            float back = g.tuning.driveCamDistance * Mathf.Max(1f, car.size.z / 5.4f);
            if (Physics.Raycast(pivot, facing * Vector3.back, out var hit, back + 0.4f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.gameObject != car.body) back = Mathf.Max(1f, hit.distance - 0.4f);
            transform.SetPositionAndRotation(pivot + facing * Vector3.back * back, facing);
            return;
        }
        transform.SetPositionAndRotation(p.transform.position + Vector3.up * g.tuning.eyeHeight, facing);
    }

    // First prototype: the nearest thing a shovel can act on along a ray. Unused in station 1.
    public static bool Cast(Vector3 origin, Vector3 direction, float max, out Aim aim)
    {
        var g = Game.I;
        aim = default;
        float best = float.MaxValue;
        int n = Physics.RaycastNonAlloc(origin, direction, hits, max);
        for (int k = 0; k < n; k++)
        {
            var collider = hits[k].collider;
            byte kind;
            Vector3Int cell = default;
            if (g.blocks.TryCell(collider, out cell)) kind = Aim.Block;
            else if (g.ground.TryUpper(collider, out _)) kind = Aim.Earth;
            else if (collider is MeshCollider && collider.transform.parent == g.ground.transform) kind = Aim.Ground;
            else continue;
            if (hits[k].distance >= best) continue;
            best = hits[k].distance;
            aim.point = hits[k].point;
            aim.kind = kind;
            aim.cell = cell;
            aim.normal = Vector3Int.RoundToInt(hits[k].normal);
        }
        return best < float.MaxValue;
    }

    // What the middle of the screen is on.
    public Aim AimAt()
    {
        if (Cast(transform.position, transform.forward, 60f, out var aim)) return aim;
        aim.point = transform.position + transform.forward * 60f;
        return aim;
    }
}
