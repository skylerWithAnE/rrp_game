using System.Reflection;
using UnityEngine;

// Every gameplay number. Only float fields: they are synced host -> clients in declaration order
// and the debug panel builds one slider per field from its Range.
[CreateAssetMenu(menuName = "RRP/Tuning")]
public class Tuning : ScriptableObject
{
    [Header("Player")]
    [Range(1, 12)] public float moveSpeed = 6f;
    [Range(0, 12)] public float jumpSpeed = 7.5f;
    [Range(5, 40)] public float gravity = 20f;
    [Range(1, 6)] public float reach = 3.2f;

    [Header("Shovel")]
    [Tooltip("Seconds per scoop: the digging speed.")]
    [Range(0.05f, 1.5f)] public float digInterval = 0.35f;
    [Range(2, 20)] public float flingSpeed = 9f;
    [Range(0, 12)] public float flingUp = 6f;
    [Range(0.3f, 2.5f)] public float catchRadius = 1.4f;
    [Range(0.2f, 2.5f)] public float pickRadius = 1.1f;
    [Range(0.1f, 1)] public float flattenStrength = 0.5f;

    [Header("Cubes")]
    [Range(10, 2000)] public float maxLooseCubes = 600f;
    [Range(0.1f, 10)] public float cubeMass = 1f;
    [Range(0, 1)] public float cubeFriction = 0.6f;
    [Range(0, 1)] public float cubeBounce = 0.1f;
    [Tooltip("A cube slower than this (m/s) counts as resting.")]
    [Range(0.01f, 1)] public float sleepSpeed = 0.15f;
    [Tooltip("Seconds a cube must rest before it is put to sleep.")]
    [Range(0, 3)] public float sleepDelay = 0.5f;
    [Tooltip("Snapshots per second for moving cubes.")]
    [Range(2, 60)] public float syncRate = 20f;

    [Header("Earthquake")]
    [Tooltip("Loose cubes that trigger an earthquake.")]
    [Range(10, 2000)] public float quakeThreshold = 60f;
    [Range(0.5f, 8)] public float quakeKnockdown = 3f;
    [Tooltip("Stable drop between neighbouring ground points during a quake (m).")]
    [Range(0.1f, 2)] public float quakeSlope = 1f;
    [Tooltip("Walls shorter than this survive a quake (m).")]
    [Range(0.25f, 4)] public float quakeWallHeight = 1.5f;

    [Tooltip("Road within this distance of a loose cube cracks in a quake (m).")]
    [Range(0, 6)] public float quakeCrackRadius = 3f;

    [Header("Road")]
    [Tooltip("How far a smacked cube's surface spreads: 0 is one point, 1 is a 3 by 3 patch (a full road width).")]
    [Range(0, 2)] public float roadSpread = 1f;
    [Tooltip("How far a point may sit off the line between its neighbours and still take gravel (m).")]
    [Range(0.02f, 1f)] public float gravelFlatness = 0.25f;

    [Header("Truck")]
    [Range(1, 12)] public float truckSpeed = 6f;
    [Tooltip("Push available for climbing. 170 stalls on slopes steeper than about 20 degrees.")]
    [Range(50, 600)] public float truckPower = 190f;
    [Range(100, 900)] public float truckSpring = 320f;
    [Range(5, 120)] public float truckDamper = 40f;
    [Tooltip("Seconds without moving before the truck gives up and blows up.")]
    [Range(1, 15)] public float truckStuckSeconds = 4f;

    [Header("Collapse")]
    [Tooltip("Stable drop between neighbouring ground points (m). Points are 1 m apart.")]
    [Range(0.25f, 3)] public float collapseSlope = 1.5f;
    [Tooltip("Walls shorter than this never collapse (m).")]
    [Range(0.5f, 6)] public float collapseHeight = 2f;

    static FieldInfo[] fields;
    public static FieldInfo[] Fields
    {
        get
        {
            if (fields == null)
                fields = typeof(Tuning).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            return fields;
        }
    }

    public void Write(Msg m)
    {
        m.U8((byte)Fields.Length);
        foreach (var f in Fields) m.F32((float)f.GetValue(this));
    }

    public void Read(Msg m)
    {
        int n = m.U8();
        for (int i = 0; i < n; i++)
        {
            float v = m.F32();
            if (i < Fields.Length) Fields[i].SetValue(this, v);
        }
    }
}
