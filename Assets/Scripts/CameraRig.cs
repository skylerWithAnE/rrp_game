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

// Third-person camera: orbits the local blob with the mouse. The crosshair picks the shovel target.
public class CameraRig : MonoBehaviour
{
    public float yaw, pitch = 28f, distance = 6.5f;
    static readonly RaycastHit[] hits = new RaycastHit[48];

    void Awake()
    {
        var c = gameObject.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = new Color(0.56f, 0.76f, 0.93f);
        c.nearClipPlane = 0.1f;
        c.farClipPlane = 300f;
        gameObject.AddComponent<AudioListener>();
        transform.position = new Vector3(0, 8, -10);
    }

    void LateUpdate()
    {
        var g = Game.I;
        var p = g.local;
        if (p == null || !g.ground.Ready) return;

        if (Hud.Playing && Mouse.current != null)
        {
            Vector2 look = Mouse.current.delta.ReadValue();
            yaw += look.x * 0.12f;
            pitch = Mathf.Clamp(pitch - look.y * 0.12f, -5f, 78f);
        }

        Vector3 focus = p.transform.position + Vector3.up * 1.3f;
        Vector3 back = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
        // come in closer rather than look through a hill, a house or a tunnel roof
        float reach = distance;
        if (Cast(focus, back, distance, out var blocked)) reach = Mathf.Max(0.6f, Vector3.Distance(focus, blocked.point) - 0.3f);
        Vector3 position = focus + back * reach;
        Quaternion look2 = Quaternion.LookRotation(focus - position);
        float shake = g.quake.shake;
        if (shake > 0) position += Random.insideUnitSphere * shake;
        transform.SetPositionAndRotation(position, look2);
    }

    // The nearest thing a shovel can act on along a ray. Players, cubes and the truck are ignored.
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
