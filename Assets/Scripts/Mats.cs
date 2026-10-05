using UnityEngine;

// Materials and meshes made in code. One shader, solid colors.
public static class Mats
{
    static Shader shader;
    static Mesh cube, sphere, cylinder;

    public static readonly Color[] PlayerColors =
    {
        new Color(0.95f, 0.30f, 0.25f), new Color(0.20f, 0.55f, 0.95f), new Color(0.35f, 0.80f, 0.35f),
        new Color(0.98f, 0.80f, 0.15f), new Color(0.70f, 0.40f, 0.90f), new Color(0.98f, 0.55f, 0.15f),
        new Color(0.25f, 0.85f, 0.80f), new Color(0.95f, 0.45f, 0.75f),
    };

    // the paint that comes out of the ground: bright, and nothing like sand
    public static readonly Color[] PaintColors =
    {
        new Color(1.00f, 0.90f, 0.10f), new Color(0.98f, 0.98f, 0.98f), new Color(0.10f, 0.85f, 0.95f),
        new Color(1.00f, 0.25f, 0.60f), new Color(0.45f, 0.95f, 0.20f), new Color(1.00f, 0.50f, 0.05f),
    };

    public static Material Make(Color color, bool flat = false)
    {
        if (shader == null) shader = Resources.Load<Shader>("SolidColor");
        var m = new Material(shader);
        m.SetColor("_Color", color);
        m.SetFloat("_Flat", flat ? 1 : 0);
        return m;
    }

    // white, and not shaded by the sun: for lines whose color is set per line
    static Material unlit;
    public static Material Unlit
    {
        get
        {
            if (unlit == null) { unlit = Make(Color.white); unlit.SetFloat("_Unlit", 1); }
            return unlit;
        }
    }

    public static Mesh Cube => cube != null ? cube : cube = Primitive(PrimitiveType.Cube);
    public static Mesh Cylinder => cylinder != null ? cylinder : cylinder = Primitive(PrimitiveType.Cylinder);
    public static Mesh Sphere => sphere != null ? sphere : sphere = Primitive(PrimitiveType.Sphere);

    static Mesh Primitive(PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        var mesh = go.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(go);
        return mesh;
    }

    // A mesh-only child: no collider.
    public static Transform Part(Transform parent, Mesh mesh, Material material, Vector3 position, Vector3 scale)
    {
        var go = new GameObject("Part");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go.transform;
    }
}
