using UnityEngine;
using UnityEngine.InputSystem;

// Third-person camera: orbits the local blob with the mouse. The crosshair picks the shovel target.
public class CameraRig : MonoBehaviour
{
    public float yaw, pitch = 28f, distance = 6.5f;

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
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 position = focus - rotation * Vector3.forward * distance;
        position.y = Mathf.Max(position.y, g.ground.HeightAt(position.x, position.z) + 0.4f);
        Quaternion look2 = Quaternion.LookRotation(focus - position);
        float shake = g.quake.shake;
        if (shake > 0) position += Random.insideUnitSphere * shake;
        transform.SetPositionAndRotation(position, look2);
    }

    // Where the middle of the screen meets the ground.
    public Vector3 AimPoint()
    {
        var ground = Game.I.ground;
        Vector3 origin = transform.position, direction = transform.forward;
        for (float t = 1f; t < 40f; t += 0.2f)
        {
            Vector3 p = origin + direction * t;
            if (p.y <= ground.HeightAt(p.x, p.z)) return p;
        }
        return origin + direction * 40f;
    }
}
