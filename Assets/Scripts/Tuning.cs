using System.Reflection;
using UnityEngine;

// Every gameplay number. Only float fields: they are synced host -> clients in declaration order
// and the debug panel builds one slider per field from its Range.
// Groups whose header starts with "Old" belong to the first prototype: they are still synced, but
// the panel does not show them.
[CreateAssetMenu(menuName = "RRP/Tuning")]
public class Tuning : ScriptableObject
{
    [Header("Station 1: the blob")]
    [Tooltip("Camera height above the feet (m). The blob is 1.6 m tall.")]
    [Range(0.8f, 1.6f)] public float eyeHeight = 1.45f;
    [Tooltip("Horizontal field of view (degrees).")]
    [Range(60, 120)] public float fieldOfView = 90f;
    [Tooltip("m/s. 1.4 is a real walking pace; the user asked for three times that.")]
    [Range(0.5f, 12)] public float walkSpeed = 4.2f;
    [Tooltip("Speed while Shift is held, as a multiple of walkSpeed.")]
    [Range(1, 4)] public float sprintMultiplier = 2f;
    [Tooltip("Upward speed of the hop on Space (m/s). 5 lifts the feet about 0.6 m.")]
    [Range(0, 10)] public float hopSpeed = 5f;

    [Header("Station 1: the truck")]
    [Range(1.5f, 3.5f)] public float truckWidth = 2.45f;
    [Range(4, 11)] public float truckLength = 6.8f;
    [Range(2, 4.5f)] public float truckHeight = 3.1f;
    [Tooltip("Wheel diameter (m).")]
    [Range(0.3f, 1.5f)] public float truckWheel = 0.75f;

    [Header("Station 1: the road")]
    [Range(2, 6)] public float laneWidth = 3.5f;
    [Tooltip("Length of one section of road (m). Two stakes can be linked up to this far apart.")]
    [Range(5, 40)] public float sectionLength = 20f;
    [Tooltip("How far across each of the three hairpins is (m): measured at the outside edge, the centre line and the inside edge.")]
    [Range(8, 30)] public float hairpinAcross = 15f;

    [Header("Stations 2 and 3: ground clicking")]
    [Tooltip("How far across the patch of ground one click moves (m).")]
    [Range(0.5f, 8)] public float patchWidth = 2f;
    [Tooltip("How far one click moves the ground toward the line (m).")]
    [Range(0.02f, 0.5f)] public float heightPerClick = 0.1f;
    [Tooltip("The most clicks a player gets per second. Faster clicks are ignored.")]
    [Range(1, 20)] public float clicksPerSecond = 4f;
    [Tooltip("0: a square patch that snaps to cells one patch wide. 1: a round patch wherever the click lands.")]
    [Range(0, 1)] public float brushRound = 0f;
    [Tooltip("0: the whole patch moves the same amount. 1: the rim barely moves.")]
    [Range(0, 1)] public float brushSoftEdge = 0f;
    [Tooltip("How far away the ground can be clicked (m).")]
    [Range(2, 20)] public float clickReach = 6f;
    [Tooltip("How far above and below the line the rough ground starts (m). Used when the ground is made again.")]
    [Range(0.2f, 1.2f)] public float roughHeight = 1f;
    [Tooltip("How far the climbing section rises, and the falling one drops, over one section (m).")]
    [Range(0, 5)] public float sectionRise = 2f;
    [Tooltip("Width of the shoulder outside each edge of the road (m). It is one cell.")]
    [Range(0, 4)] public float shoulderWidth = 1.75f;
    [Tooltip("How far the shoulder's line falls from the road's edge to its outside (m).")]
    [Range(0, 1)] public float shoulderDrop = 0.3f;

    [Header("Stakes")]
    [Tooltip("The closest two stakes may stand (m).")]
    [Range(3, 10)] public float minStakeSpacing = 6.5f;
    [Tooltip("The most the road may bend at one stake (degrees). 36 at the closest spacing is about the tightest turn allowed.")]
    [Range(5, 90)] public float maxBend = 36f;

    [Header("Truck damage (the wear road only)")]
    [Tooltip("The most damage a truck does to a grid square each quarter second it is on it, out of 100. The amount is random up to this.")]
    [Range(0, 100)] public float truckDamage = 30f;
    [Tooltip("A square worn below this starts to rut under the wheels.")]
    [Range(0, 100)] public float damageThreshold = 50f;
    [Tooltip("The deepest a wheel cuts each time (m).")]
    [Range(0.01f, 0.3f)] public float rutDepth = 0.05f;

    [Header("Station 4: gravel")]
    [Tooltip("Full depth of gravel on the road (m).")]
    [Range(0.05f, 0.25f)] public float gravelDepth = 0.15f;
    [Tooltip("Depth one click of gravel adds (m).")]
    [Range(0.01f, 0.25f)] public float gravelPerClick = 0.05f;
    [Tooltip("How much of the packing one click does, once the gravel is at full depth. 0.25 packs it in four clicks.")]
    [Range(0.05f, 1)] public float compactPerClick = 0.25f;

    [Header("Hot spot")]
    [Tooltip("A click with the crosshair on the hot spot moves the ground this many times as far.")]
    [Range(1, 4)] public float hotSpotBonus = 2f;
    [Tooltip("Radius of the hot spot (m).")]
    [Range(0.1f, 1.5f)] public float hotSpotSize = 0.35f;

    [Header("Station 5: the truck")]
    [Tooltip("The speed it tries to hold (m/s).")]
    [Range(2, 15)] public float lorrySpeed = 6f;
    [Tooltip("The hardest it can push on packed gravel (m/s2): 4.5 climbs about 27 degrees. Loose gravel gives 0.6 of it and bare ground 0.4.")]
    [Range(1, 9)] public float lorryPower = 4.5f;
    [Tooltip("Seconds without moving before it gives up and blows up.")]
    [Range(1, 15)] public float lorryStuckSeconds = 4f;

    [Header("Old: Player")]
    [Range(1, 12)] public float moveSpeed = 6f;
    [Range(0, 12)] public float jumpSpeed = 7.5f;
    [Range(5, 40)] public float gravity = 20f;
    [Range(1, 6)] public float reach = 3.2f;

    [Header("Old: Shovel")]
    [Tooltip("Seconds per scoop: the digging speed.")]
    [Range(0.05f, 1.5f)] public float digInterval = 0.35f;
    [Range(2, 20)] public float flingSpeed = 9f;
    [Range(0, 12)] public float flingUp = 6f;
    [Range(0.3f, 2.5f)] public float catchRadius = 1.4f;
    [Range(0.2f, 2.5f)] public float pickRadius = 1.1f;
    [Range(0.1f, 1)] public float flattenStrength = 0.5f;

    [Header("Old: Cubes")]
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

    [Header("Old: Earthquake")]
    [Tooltip("Loose cubes that trigger an earthquake.")]
    [Range(10, 2000)] public float quakeThreshold = 60f;
    [Range(0.5f, 8)] public float quakeKnockdown = 3f;
    [Tooltip("Stable drop between neighbouring ground points during a quake (m).")]
    [Range(0.1f, 2)] public float quakeSlope = 1f;
    [Tooltip("Walls shorter than this survive a quake (m).")]
    [Range(0.25f, 4)] public float quakeWallHeight = 1.5f;

    [Tooltip("Road within this distance of a loose cube cracks in a quake (m).")]
    [Range(0, 6)] public float quakeCrackRadius = 3f;

    [Header("Old: Road")]
    [Tooltip("How far a smacked cube's surface spreads: 0 is one point, 1 is a 3 by 3 patch (a full road width).")]
    [Range(0, 2)] public float roadSpread = 1f;
    [Tooltip("How far a point may sit off the line between its neighbours and still take gravel (m).")]
    [Range(0.02f, 1f)] public float gravelFlatness = 0.25f;

    [Header("Old: Truck")]
    [Range(1, 12)] public float truckSpeed = 6f;
    [Tooltip("Push available for climbing. 170 stalls on slopes steeper than about 20 degrees.")]
    [Range(50, 600)] public float truckPower = 190f;
    [Range(100, 900)] public float truckSpring = 320f;
    [Range(5, 120)] public float truckDamper = 40f;
    [Tooltip("Seconds without moving before the truck gives up and blows up.")]
    [Range(1, 15)] public float truckStuckSeconds = 4f;

    [Header("Old: Collapse")]
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
