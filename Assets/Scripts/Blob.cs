using UnityEngine;

// A damped spring: the one building block of all animation here.
public struct Spring
{
    public float x, v;

    public void Step(float target, float frequency, float damping, float dt)
    {
        float omega = frequency * 2f * Mathf.PI;
        dt = Mathf.Min(dt, 0.033f);
        v += (-2f * damping * omega * v - omega * omega * (x - target)) * dt;
        x += v * dt;
    }
}

// The look and procedural animation of a player: a solid-color blob with eyes and a shovel.
// Local and remote players run exactly this from the same few inputs (velocity, grounded, events).
public class Blob : MonoBehaviour
{
    public const int SwingScoop = 0, SwingFling = 1, SwingSmack = 2;
    public const float Height = 1.6f;   // a blob is 1.6 m tall; the parts below are modelled 1 m tall and scaled

    Transform body, shovel, loadVisual;
    MeshRenderer loadRenderer;
    static Material eyeMaterial, handleMaterial, bladeMaterial;

    Spring squash, swing, leanX, leanZ, flop;
    float walkPhase;
    bool wasGrounded = true;
    float lastVerticalSpeed;

    public void Build(Color color)
    {
        if (eyeMaterial == null)
        {
            eyeMaterial = Mats.Make(new Color(0.08f, 0.08f, 0.1f));
            handleMaterial = Mats.Make(new Color(0.45f, 0.30f, 0.18f));
            bladeMaterial = Mats.Make(new Color(0.62f, 0.64f, 0.68f));
        }

        body = new GameObject("Body").transform;
        body.SetParent(transform, false);
        Mats.Part(body, Mats.Sphere, Mats.Make(color), new Vector3(0, 0.5f, 0), new Vector3(0.8f, 1f, 0.8f));
        Mats.Part(body, Mats.Sphere, eyeMaterial, new Vector3(-0.14f, 0.68f, 0.33f), Vector3.one * 0.11f);
        Mats.Part(body, Mats.Sphere, eyeMaterial, new Vector3(0.14f, 0.68f, 0.33f), Vector3.one * 0.11f);

        shovel = new GameObject("Shovel").transform;
        shovel.SetParent(body, false);
        shovel.localPosition = new Vector3(0.36f, 0.42f, 0.05f);
        Mats.Part(shovel, Mats.Cube, handleMaterial, new Vector3(0, 0, 0.35f), new Vector3(0.06f, 0.06f, 0.8f));
        Mats.Part(shovel, Mats.Cube, bladeMaterial, new Vector3(0, -0.01f, 0.88f), new Vector3(0.36f, 0.04f, 0.34f));
        loadVisual = Mats.Part(shovel, Mats.Cube, Cubes.MaterialFor(Cubes.Sand), new Vector3(0, 0.22f, 0.88f), Vector3.one * 0.4f);
        loadRenderer = loadVisual.GetComponent<MeshRenderer>();
        loadVisual.gameObject.SetActive(false);

        squash.x = 1;
        // every blob carries a shovel; its swings are the ones its player made (Game.SendSwing)
    }

    // First person: the local player does not see their own blob.
    public void Hide() { body.gameObject.SetActive(false); }
    public void Show(bool shown) { if (body.gameObject.activeSelf != shown) body.gameObject.SetActive(shown); }

    public void SetLoad(int load, int bits)
    {
        loadVisual.gameObject.SetActive(load != 0);
        if (load == 0) return;
        loadRenderer.sharedMaterial = Cubes.MaterialFor((byte)load);
        // a partial oil load shows as a stack of thin bits
        // the cube is far bigger than the shovel, and is carried that way
        float height = bits == 0 ? 0.45f : 0.09f * bits;
        loadVisual.localScale = new Vector3(0.45f, height, 0.45f);
        loadVisual.localPosition = new Vector3(0, 0.02f + height * 0.5f, 0.88f);
    }

    public void Swing(int kind)
    {
        if (kind == SwingScoop) swing.v += 520f;        // dip the blade into the ground
        else if (kind == SwingFling) swing.v -= 900f;   // whip it up
        else { swing.x = -75f; swing.v = 700f; }        // wind up overhead, then slam
        squash.v -= 1.2f;
    }

    public void Tick(Vector3 velocity, bool grounded, bool knocked, float dt)
    {
        Vector3 local = transform.InverseTransformDirection(velocity);
        float speed = new Vector2(velocity.x, velocity.z).magnitude;

        // squash and stretch: stretch in the air, splat on landing, bob while walking
        if (grounded && !wasGrounded) squash.v -= Mathf.Clamp(-lastVerticalSpeed, 0, 12f) * 0.45f;
        if (!grounded && wasGrounded && velocity.y > 1f) squash.v += 3f;
        wasGrounded = grounded;
        lastVerticalSpeed = velocity.y;
        float airStretch = grounded ? 1f : 1f + Mathf.Clamp(Mathf.Abs(velocity.y) * 0.02f, 0, 0.2f);
        squash.Step(airStretch, 3.2f, 0.35f, dt);

        walkPhase += speed * dt * 2.4f;
        float bob = grounded ? Mathf.Sin(walkPhase * Mathf.PI) * 0.07f * Mathf.Clamp01(speed / 3f) : 0;
        float sy = Mathf.Clamp(squash.x + bob, 0.45f, 1.7f);
        float sxz = 1f / Mathf.Sqrt(sy); // keeps the volume
        body.localScale = new Vector3(sxz, sy, sxz) * Height;

        // lean into movement; flop over when knocked down
        leanX.Step(Mathf.Clamp(local.z * 3.5f, -22f, 22f), 2.5f, 0.5f, dt);
        leanZ.Step(Mathf.Clamp(-local.x * 3.5f, -22f, 22f), 2.5f, 0.5f, dt);
        flop.Step(knocked ? 1f : 0f, 2.2f, 0.4f, dt);
        float wobble = knocked ? Mathf.Sin(Time.time * 9f) * 6f : 0;
        body.localRotation = Quaternion.Euler(leanX.x + flop.x * 82f, wobble, leanZ.x + wobble);

        swing.Step(0, 3.5f, 0.45f, dt);
        shovel.localRotation = Quaternion.Euler(Mathf.Clamp(swing.x, -110f, 70f), 0, 0);
    }
}
