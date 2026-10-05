using System.Collections.Generic;
using UnityEngine;

// Station 1, the scale yard: flat grey ground, a grey-box truck standing in one lane of a marked
// two-lane strip, and three hairpins. Nothing here moves or can be changed by a player. Every size
// comes from Tuning, and the yard rebuilds itself when one of them changes.
public class Yard : MonoBehaviour
{
    public struct Label { public Vector3 at; public string text; }
    public readonly List<Label> labels = new List<Label>();

    const float Line = 0.15f;          // width of a painted line
    const float HairpinLeg = 10f;      // straight road leading into and out of each hairpin
    const float HairpinGap = 6f;       // bare ground between hairpins
    const float HairpinStart = 12f;    // how far past the end of the strip the hairpins begin

    Material groundMaterial, roadMaterial, lineMaterial, bodyMaterial, glassMaterial, tyreMaterial, yellowMaterial, orangeMaterial;
    string built = "";

    public bool Ready => built.Length > 0;

    static string Signature(Tuning t)
    {
        return Game.I.map + " " + t.truckWidth + " " + t.truckLength + " " + t.truckHeight + " " + t.truckWheel + " "
            + t.laneWidth + " " + t.sectionLength + " " + t.hairpinAcross;
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
        labels.Clear();
        built = "";
    }

    // Build, or rebuild if a size has changed since the last build.
    public void Refresh()
    {
        var t = Game.I.tuning;
        string signature = Signature(t);
        if (signature == built) return;
        Clear();
        built = signature;
        Materials();

        // the flat ground is under every test ground; the truck, the strip and the painted
        // hairpins are the scale yard's alone
        var ground = Mats.Part(transform, Mats.Cube, groundMaterial, new Vector3(0, -0.5f, 40f), new Vector3(700f, 1f, 700f));
        ground.name = "Ground";
        ground.gameObject.AddComponent<BoxCollider>();
        if (Game.I.map != Plot.YardMap) return;

        float road = t.laneWidth * 2f;
        Strip(t, road);
        Truck(t, TruckPosition(t));

        // three readings of "a hairpin N metres across", side by side
        float half = t.hairpinAcross * 0.5f;
        float[] centreRadius = { half - t.laneWidth, half, half + t.laneWidth };
        string[] names = { "the outside edge", "the centre line", "the inside edge" };
        float total = -HairpinGap;
        foreach (float r in centreRadius) total += (r + t.laneWidth) * 2f + HairpinGap;
        // past the far end of station 2's plot, which is three sections long
        float x = -total * 0.5f, z = t.sectionLength * Plot.PresetSections + HairpinStart;
        for (int k = 0; k < 3; k++)
        {
            float outer = centreRadius[k] + t.laneWidth;
            Hairpin(t, new Vector3(x + outer, 0, z), Mathf.Max(centreRadius[k], t.laneWidth),
                "Hairpin " + t.hairpinAcross.ToString("0.#") + " m across " + names[k]);
            x += outer * 2f + HairpinGap;
        }
    }

    // the trucks on a map use these too, and there the yard is never built
    void Materials()
    {
        if (groundMaterial != null) return;
        groundMaterial = Mats.Make(new Color(0.50f, 0.50f, 0.50f));
        roadMaterial = Mats.Make(new Color(0.34f, 0.34f, 0.36f));
        lineMaterial = Mats.Make(new Color(0.93f, 0.93f, 0.90f));
        bodyMaterial = Mats.Make(new Color(0.74f, 0.74f, 0.76f));
        glassMaterial = Mats.Make(new Color(0.20f, 0.22f, 0.26f));
        tyreMaterial = Mats.Make(new Color(0.12f, 0.12f, 0.13f));
        yellowMaterial = Mats.Make(new Color(0.95f, 0.75f, 0.10f));
        orangeMaterial = Mats.Make(new Color(0.90f, 0.42f, 0.10f));
    }

    // A dump truck: the truck's cab and wheels, and in place of the box an open orange bed that
    // tips up about a hinge at the back. Returns the bed, for whoever tips it.
    public Transform MakeDumpTruck(Transform parent, Tuning t)
    {
        Materials();
        var root = new GameObject("Dump truck").transform;
        root.SetParent(parent, false);
        float w = t.truckWidth, l = t.truckLength, wheel = t.truckWheel;
        float back = -l * 0.5f, front = l * 0.5f, deck = wheel + 0.25f, floor = wheel * 0.6f;
        float cargoFront = back + l * 0.62f, cabWidth = Mathf.Min(w, 2.0f);
        Box(root, tyreMaterial, -w * 0.35f, w * 0.35f, floor, deck, back + 0.2f, cargoFront, false);                 // the chassis
        Box(root, bodyMaterial, -cabWidth * 0.5f, cabWidth * 0.5f, floor, 2.15f, cargoFront + 0.1f, front - l * 0.1f, false);  // the cab
        Box(root, bodyMaterial, -cabWidth * 0.5f, cabWidth * 0.5f, floor, 1.35f, front - l * 0.1f, front, false);
        Box(root, glassMaterial, -cabWidth * 0.5f - 0.01f, cabWidth * 0.5f + 0.01f, 1.45f, 2.03f, front - l * 0.1f - 0.9f, front - l * 0.1f + 0.01f, false);
        float frontAxle = front - l * 0.13f, rearAxle = frontAxle - l * 0.66f;
        for (int side = -1; side <= 1; side += 2)
        {
            Wheel(root, new Vector3(side * (cabWidth * 0.5f - 0.09f), wheel * 0.5f, frontAxle), wheel, 0.26f);
            Wheel(root, new Vector3(side * (w * 0.5f - 0.26f), wheel * 0.5f, rearAxle), wheel, 0.5f);
        }
        // the bed: a floor, two sides and a front wall, hinged at the back of the chassis
        var bed = new GameObject("Bed").transform;
        bed.SetParent(root, false);
        bed.localPosition = new Vector3(0, deck, back + 0.3f);
        float length = cargoFront - back - 0.3f;
        Box(bed, orangeMaterial, -w * 0.5f, w * 0.5f, 0, 0.12f, 0, length, false);
        Box(bed, orangeMaterial, -w * 0.5f, -w * 0.5f + 0.12f, 0, 1.1f, 0, length, false);
        Box(bed, orangeMaterial, w * 0.5f - 0.12f, w * 0.5f, 0, 1.1f, 0, length, false);
        Box(bed, orangeMaterial, -w * 0.5f, w * 0.5f, 0, 1.3f, length - 0.12f, length, false);
        return bed;
    }

    // A road roller: a yellow box on two drums as wide as a lane's wheel tracks.
    public Transform MakeRoller(Transform parent, Tuning t)
    {
        Materials();
        var root = new GameObject("Roller").transform;
        root.SetParent(parent, false);
        float w = t.truckWidth, l = t.truckLength;
        Box(root, yellowMaterial, -w * 0.4f, w * 0.4f, 1.1f, 2.3f, -l * 0.2f, l * 0.25f, false);
        Box(root, glassMaterial, -w * 0.3f, w * 0.3f, 2.3f, 3.0f, -l * 0.15f, l * 0.1f, false);
        for (int end = -1; end <= 1; end += 2)
        {
            var drum = Mats.Part(root, Mats.Cylinder, tyreMaterial, new Vector3(0, 0.7f, end * l * 0.32f), new Vector3(1.4f, w * 0.5f, 1.4f));
            drum.localRotation = Quaternion.Euler(0, 0, 90f);
        }
        return root;
    }

    Vector3 TruckPosition(Tuning t) { return new Vector3(t.laneWidth * 0.5f, 0, t.truckLength * 0.5f + 1f); }

    // Players start in a row on the bare ground beside the truck, looking at it.
    public Vector3 Spawn(int slot)
    {
        var t = Game.I.tuning;
        // on the trucks' test ground: at the near end of the row of roads, between the good and the bad
        if (Game.I.map == Plot.TrucksMap) return new Vector3(-t.laneWidth - 51.5f - slot * 1.2f, 0.1f, -6f);
        Vector3 truck = TruckPosition(t);
        return new Vector3(truck.x + t.truckWidth * 0.5f + 3f, 0.1f, 1f + slot * 1.2f);
    }

    public const float SpawnYaw = -70f;

    // ---- road markings

    Transform Flat(Transform parent, Material material, Vector3 centre, float width, float length, float lift)
    {
        return Mats.Part(parent, Mats.Cube, material, new Vector3(centre.x, lift * 0.5f, centre.z), new Vector3(width, lift, length));
    }

    // a straight piece of road along z: surface, two edge lines and a centre line
    void Straight(Transform parent, float lane, Vector3 centre, float length)
    {
        Flat(parent, roadMaterial, centre, lane * 2f, length, 0.01f);
        for (int k = -1; k <= 1; k++)
            Flat(parent, lineMaterial, centre + Vector3.right * (k * (lane - (k == 0 ? 0 : Line * 0.5f))), Line, length, 0.02f);
    }

    void Strip(Tuning t, float road)
    {
        var root = new GameObject("Strip").transform;
        root.SetParent(transform, false);
        Straight(root, t.laneWidth, new Vector3(0, 0, t.sectionLength * 0.5f), t.sectionLength);
        labels.Add(new Label
        {
            at = new Vector3(0, 0.3f, t.sectionLength * 0.5f),
            text = "Road: two " + t.laneWidth.ToString("0.#") + " m lanes, " + road.ToString("0.#") + " m wide, " + t.sectionLength.ToString("0.#") + " m long",
        });
    }

    // A U-turn: in along one leg, half a circle, out along the other. centre is the middle of the
    // open end; radius is that of the centre line.
    void Hairpin(Tuning t, Vector3 centre, float radius, string text)
    {
        var root = new GameObject("Hairpin").transform;
        root.SetParent(transform, false);
        root.localPosition = centre;
        float lane = t.laneWidth, inner = radius - lane, outer = radius + lane;
        Straight(root, lane, new Vector3(-radius, 0, HairpinLeg * 0.5f), HairpinLeg);
        Straight(root, lane, new Vector3(radius, 0, HairpinLeg * 0.5f), HairpinLeg);
        Vector3 turn = new Vector3(0, 0, HairpinLeg);
        HalfRing(root, roadMaterial, turn, inner, outer, 0.01f);
        HalfRing(root, lineMaterial, turn, radius - Line * 0.5f, radius + Line * 0.5f, 0.02f);
        HalfRing(root, lineMaterial, turn, outer - Line, outer, 0.02f);
        if (inner > Line) HalfRing(root, lineMaterial, turn, inner, inner + Line, 0.02f);
        labels.Add(new Label
        {
            at = centre + new Vector3(0, 0.3f, HairpinLeg * 0.5f),
            text = text + "\ninside edge radius " + inner.ToString("0.#") + " m, outside " + outer.ToString("0.#") + " m",
        });
    }

    // half an annulus lying on the ground, curving round the far (+z) side of centre
    void HalfRing(Transform parent, Material material, Vector3 centre, float inner, float outer, float lift)
    {
        const int Segments = 48;
        var verts = new Vector3[(Segments + 1) * 2];
        var normals = new Vector3[verts.Length];
        var tris = new int[Segments * 6];
        for (int i = 0; i <= Segments; i++)
        {
            float a = Mathf.PI * i / Segments;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            verts[i * 2] = dir * inner;
            verts[i * 2 + 1] = dir * outer;
            normals[i * 2] = normals[i * 2 + 1] = Vector3.up;
            if (i == Segments) break;
            int v = i * 2, k = i * 6;
            tris[k] = v; tris[k + 1] = v + 3; tris[k + 2] = v + 1;
            tris[k + 3] = v; tris[k + 4] = v + 2; tris[k + 5] = v + 3;
        }
        var mesh = new Mesh { vertices = verts, normals = normals, triangles = tris };
        mesh.RecalculateBounds();
        Mats.Part(parent, mesh, material, centre + Vector3.up * lift, Vector3.one).gameObject
            .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ---- the truck

    // A grey box in the proportions of a 15 ft U-Haul: cargo box over the rear, a cab and a short
    // hood in front. Only the overall width, length, height and wheel size are tuned; the rest
    // follows from them.
    void Truck(Tuning t, Vector3 position)
    {
        MakeTruck(transform, t, true).localPosition = position;
        labels.Add(new Label
        {
            at = position + Vector3.up * (t.truckHeight + 0.4f),
            text = "Truck: " + t.truckWidth.ToString("0.##") + " wide, " + t.truckLength.ToString("0.##") + " long, " + t.truckHeight.ToString("0.##") + " tall, wheels " + t.truckWheel.ToString("0.##"),
        });
    }

    // The truck's shape, standing on the ground at its parent's origin. solid: it can be walked into.
    public Transform MakeTruck(Transform parent, Tuning t, bool solid)
    {
        Materials();
        var root = new GameObject("Truck").transform;
        root.SetParent(parent, false);
        float w = t.truckWidth, l = t.truckLength, h = t.truckHeight, wheel = t.truckWheel;
        float back = -l * 0.5f, front = l * 0.5f;
        float deck = Mathf.Min(wheel + 0.1f, h * 0.4f);     // cargo floor: U-Haul gives 33 in (0.84 m)
        float floor = wheel * 0.6f;                         // underside of the cab
        float cargo = l * 0.66f;                            // 4.5 m of a 6.8 m truck
        float hood = l * 0.13f;
        float cabWidth = Mathf.Min(w, 2.0f);
        float cabTop = Mathf.Min(h, 2.15f);
        float cargoFront = back + cargo, cabFront = front - hood;

        Box(root, bodyMaterial, -w * 0.5f, w * 0.5f, deck, h, back, cargoFront, solid);
        Box(root, tyreMaterial, -w * 0.35f, w * 0.35f, floor, deck, back + 0.2f, cargoFront, false);
        Box(root, bodyMaterial, -cabWidth * 0.5f, cabWidth * 0.5f, floor, cabTop, cargoFront, cabFront, solid);
        Box(root, bodyMaterial, -cabWidth * 0.5f, cabWidth * 0.5f, floor, cabTop * 0.62f, cabFront, front, solid);
        // the windows: a dark band round the front of the cab, its bottom edge at 1.45 m
        float sill = cabTop * 0.675f;
        Box(root, glassMaterial, -cabWidth * 0.5f - 0.01f, cabWidth * 0.5f + 0.01f, sill, cabTop - 0.12f, cabFront - 0.9f, cabFront + 0.01f, false);

        float frontAxle = front - l * 0.13f, rearAxle = frontAxle - l * 0.66f;
        for (int side = -1; side <= 1; side += 2)
        {
            Wheel(root, new Vector3(side * (cabWidth * 0.5f - 0.09f), wheel * 0.5f, frontAxle), wheel, 0.26f);
            Wheel(root, new Vector3(side * (w * 0.5f - 0.26f), wheel * 0.5f, rearAxle), wheel, 0.5f);   // twin rear tyres
        }

        return root;
    }

    void Box(Transform parent, Material material, float x0, float x1, float y0, float y1, float z0, float z1, bool solid)
    {
        var part = Mats.Part(parent, Mats.Cube, material, new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, y1 - y0, z1 - z0));
        if (solid) part.gameObject.AddComponent<BoxCollider>();
    }

    void Wheel(Transform parent, Vector3 position, float diameter, float width)
    {
        // Unity's cylinder is 2 tall along y and 1 across
        var part = Mats.Part(parent, Mats.Cylinder, tyreMaterial, position, new Vector3(diameter, width * 0.5f, diameter));
        part.localRotation = Quaternion.Euler(0, 0, 90f);
    }
}
