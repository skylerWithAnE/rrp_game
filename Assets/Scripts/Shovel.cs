using UnityEngine;

// The shovel in the local player's hands: a shovel floating at the lower right of the view, with
// no hand or arm. It is there while a tool that is a shovel is held (grading, gravel, asphalt),
// and drops out of sight otherwise and while driving. All of its motion is springs: each thing
// the player does with it gives the springs a kick, and they settle back by themselves.
//
//   Dig     grading: the blade stabs down and levers back
//   Spread  laying gravel or asphalt: a push forward and a flick
//   Tamp    packing: it comes straight down
//   Scoop   loading the shovel, at the quarry's rock or from the truck: a dip, and up with a load
//   Fling   flinging the load, into the truck or onto the heap: a whip up and forward
//
// Other players see the same thing on the blob's own shovel: see Game.SendSwing.
public class Shovel : MonoBehaviour
{
    public const int Dig = 0, Spread = 1, Tamp = 2, Scoop = 3, Fling = 4;
    static Shovel I;

    Transform root, load;
    Spring pitch, push, drop, shown;
    float walk;

    // where it rests, in the camera's own terms: low, to the right, blade forward and a little down
    static readonly Vector3 Rest = new Vector3(0.36f, -0.34f, 0.58f);
    static readonly Vector3 RestTurn = new Vector3(14f, -10f, 6f);

    void Awake()
    {
        I = this;
        root = new GameObject("Shovel").transform;
        root.SetParent(transform, false);
        var handle = Mats.Make(new Color(0.45f, 0.30f, 0.18f));
        var blade = Mats.Make(new Color(0.62f, 0.64f, 0.68f));
        Part(Mats.Cube, handle, new Vector3(0, 0, 0.05f), new Vector3(0.045f, 0.045f, 1.0f));
        Part(Mats.Cube, handle, new Vector3(0, 0, -0.45f), new Vector3(0.16f, 0.045f, 0.05f));      // the grip
        Part(Mats.Cube, blade, new Vector3(0, -0.01f, 0.70f), new Vector3(0.26f, 0.03f, 0.32f));
        load = Part(Mats.Sphere, Mats.Make(new Color(0.69f, 0.69f, 0.67f), true), new Vector3(0, 0.07f, 0.70f), new Vector3(0.26f, 0.16f, 0.30f));
        load.gameObject.SetActive(false);
    }

    Transform Part(Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var part = Mats.Part(root, mesh, material, position, scale);
        part.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return part;
    }

    // The local player did something with the shovel. Others are told, so the blob's shovel moves too.
    public static void Swing(int kind)
    {
        if (I == null) return;
        if (kind == Dig) { I.pitch.v += 420f; I.drop.v += 1.6f; I.push.v += 1.2f; }
        else if (kind == Spread) { I.push.v += 3.2f; I.pitch.v -= 260f; }
        else if (kind == Tamp) { I.drop.v += 4.2f; I.pitch.v += 120f; }
        else if (kind == Scoop) { I.pitch.v += 520f; I.push.v += 2.2f; I.drop.v += 1.2f; }
        else { I.pitch.v -= 820f; I.push.v += 2.6f; }
        Game.I.SendSwing(kind == Dig || kind == Scoop ? Blob.SwingScoop : kind == Tamp ? Blob.SwingSmack : Blob.SwingFling);
    }

    void LateUpdate()
    {
        var g = Game.I;
        float dt = Time.deltaTime;
        bool held = g.local != null && (g.phase == Phase.Lobby) && Cars.Mine < 0 && (Plot.Tool == Plot.Grade || Plot.Tool == Plot.Gravel || Plot.Tool == Plot.Pave);
        shown.Step(held ? 1f : 0f, 2.6f, 0.8f, dt);
        pitch.Step(0, 3.2f, 0.5f, dt);
        push.Step(0, 3.6f, 0.55f, dt);
        drop.Step(0, 3.6f, 0.55f, dt);
        root.gameObject.SetActive(shown.x > 0.02f);
        if (!root.gameObject.activeSelf) return;

        // it sways a little with each step
        float speed = g.local != null ? new Vector2(g.local.velocity.x, g.local.velocity.z).magnitude : 0;
        walk += speed * dt * 2.2f;
        float sway = Mathf.Sin(walk * Mathf.PI) * 0.012f * Mathf.Clamp01(speed / 3f);
        root.localPosition = Rest + new Vector3(sway, -Mathf.Clamp(drop.x, -0.2f, 0.35f) - (1f - shown.x) * 0.7f + Mathf.Abs(sway) * 0.6f, Mathf.Clamp(push.x, -0.2f, 0.45f));
        root.localRotation = Quaternion.Euler(RestTurn.x + Mathf.Clamp(pitch.x, -75f, 55f), RestTurn.y, RestTurn.z);
        // a loaded shovel shows its load
        load.gameObject.SetActive(g.localSlot >= 0 && (g.plots[Plot.Quarry].carrying & 1 << g.localSlot) != 0);
    }
}
