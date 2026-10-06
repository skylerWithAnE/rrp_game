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
    [Tooltip("The steepest a rope may run (degrees), so that no road is staked that a truck cannot climb once it is finished.")]
    [Range(3, 40)] public float maxSlope = 15f;

    [Header("Junctions (the Junctions test ground only)")]
    [Tooltip("The most ropes one stake takes. 2: no junctions. 3: a T or a fork. 4: a crossroads too.")]
    [Range(2, 4)] public float ropesPerStake = 4f;
    [Tooltip("The least angle between a new branch and each rope already at the stake (degrees).")]
    [Range(30, 90)] public float junctionAngle = 60f;

    [Header("Quarry (the Quarry test ground only)")]
    [Tooltip("Shovels of gravel the truck carries.")]
    [Range(2, 100)] public float haulLoad = 12f;
    [Tooltip("How many clicks of laying one shovel is worth once it is on the heap. 3 lays one square to full depth, so 30 covers ten squares.")]
    [Range(1, 60)] public float shovelWorth = 30f;
    [Tooltip("How far past the stake the drop-off branches from the gravel truck drives before it backs in (m).")]
    [Range(6, 20)] public float haulPass = 12f;
    [Tooltip("How fast the gravel truck backs in (m/s).")]
    [Range(0.5f, 5)] public float haulBackSpeed = 2f;
    [Tooltip("Seconds after the gravel truck is wrecked before an empty one stands at the quarry.")]
    [Range(1, 120)] public float haulRespawn = 20f;
    [Tooltip("[Claude] 1: trucks wear the roads of the Quarry ground, as they do on the Wear ground. 0: they do not.")]
    [Range(0, 1)] public float quarryWear = 0f;
    [Tooltip("How far a loaded shovel can be flung, into the truck or onto the heap (m).")]
    [Range(3, 30)] public float flingReach = 16f;
    [Tooltip("1: gravel laid at the quarry comes off the heap at the drop, and none can be laid when it is empty. 0: gravel is free, as everywhere else.")]
    [Range(0, 1)] public float gravelFromStock = 1f;

    [Header("Paving (the Paving test ground only)")]
    [Tooltip("How far the dump truck's load of asphalt goes: quarter-seconds of tipping at its creep. 250 is about 110 m of road, both lanes of the test road.")]
    [Range(20, 250)] public float dumpLoad = 250f;
    [Tooltip("How fast the dump truck creeps while it tips, as a share of a truck's speed.")]
    [Range(0.1f, 1)] public float tipSpeed = 0.3f;
    [Tooltip("How much of spreading a square one click does. 0.5 spreads it in two clicks.")]
    [Range(0.1f, 1)] public float spreadPerClick = 0.5f;
    [Tooltip("How much of the rolling the roller does to a square each quarter second it is on it.")]
    [Range(0.05f, 1)] public float rollPerPass = 0.5f;
    [Tooltip("How fast the roller drives, as a share of a truck's speed.")]
    [Range(0.1f, 1)] public float rollerSpeed = 0.4f;
    [Tooltip("How much faster a truck drives on road that is paved and rolled.")]
    [Range(1, 3)] public float pavedSpeed = 1.5f;

    [Header("Painting by hand (the Painting ground only)")]
    [Tooltip("How wide the band is round where each line should be (m). Paint in the band counts; paint outside it counts against.")]
    [Range(0.1f, 1.5f)] public float paintBand = 0.4f;
    [Tooltip("How wide a stripe the roller brush leaves (m).")]
    [Range(0.05f, 0.6f)] public float rollerWidth = 0.15f;
    [Tooltip("1: the line down the middle should be broken, 3 m on and 3 m off. 0: solid.")]
    [Range(0, 1)] public float centreBroken = 1f;
    [Tooltip("How wide a stripe each nozzle of the paint truck leaves (m).")]
    [Range(0.05f, 0.6f)] public float sprayWidth = 0.15f;
    [Tooltip("How wide the tar spray scatters (m).")]
    [Range(0.2f, 3)] public float tarWidth = 0.9f;
    [Tooltip("How wide a line the grinder takes off (m).")]
    [Range(0.05f, 0.5f)] public float grinderWidth = 0.1f;

    [Header("Driving (the Driving test ground only)")]
    [Tooltip("Top speed of the pick-up (m/s).")]
    [Range(3, 30)] public float pickupSpeed = 14f;
    [Tooltip("Top speed of the roller when a player drives it (m/s).")]
    [Range(1, 10)] public float rollerDriveSpeed = 3f;
    [Tooltip("Top speed of the front loader (m/s).")]
    [Range(2, 15)] public float loaderSpeed = 7f;
    [Tooltip("Top speed of the paint truck (m/s).")]
    [Range(1, 15)] public float painterSpeed = 5f;
    [Tooltip("How far behind a vehicle the camera sits for whoever is in it (m). 0: the view from the seat. C switches between the two.")]
    [Range(0, 20)] public float driveCamDistance = 9f;
    [Tooltip("The hardest a driven vehicle pushes or brakes (m/s2).")]
    [Range(1, 12)] public float drivePower = 5f;
    [Tooltip("How fast a driven vehicle turns at speed (radians a second).")]
    [Range(0.3f, 3)] public float driveTurn = 1.1f;

    [Header("Truck damage (the wear road only)")]
    [Tooltip("The most damage a truck does to a grid square each quarter second it is on it, out of 100. The amount is random up to this.")]
    [Range(0, 100)] public float truckDamage = 30f;
    [Tooltip("A square worn below this starts to rut under the wheels.")]
    [Range(0, 100)] public float damageThreshold = 50f;
    [Tooltip("The deepest a wheel cuts each time (m).")]
    [Range(0.01f, 0.3f)] public float rutDepth = 0.05f;

    [Header("Wear (the Wear ground, and maps). damageThreshold above is shared")]
    [Tooltip("The most damage a truck does to a square of bare ground each quarter second it is on it, out of 100.")]
    [Range(0, 100)] public float wearDirt = 80f;
    [Tooltip("The same for a square of gravel. Paved road takes none.")]
    [Range(0, 100)] public float wearGravel = 3f;
    [Tooltip("Once a square is below damageThreshold, damage to it is multiplied by this, and the wheels cut up to this many times as deep as it falls further.")]
    [Range(1, 6)] public float wearScaleUp = 1.5f;
    [Tooltip("How deep a wheel cuts each time into a square that is below the threshold (m), before the scaling up.")]
    [Range(0.002f, 0.1f)] public float wearCut = 0.006f;
    [Tooltip("Bare ground is cut this many times as deep as gravel each time.")]
    [Range(1, 10)] public float wearDirtCut = 6f;
    [Tooltip("The deepest a hole gets, below the road's line (m).")]
    [Range(0.05f, 8f)] public float wearDeepest = 0.8f;
    [Tooltip("How high the Wear ground stands above the yard (m). Used when the ground is made again.")]
    [Range(2, 20)] public float wearHeight = 10f;
    [Tooltip("0: all damage comes from time spent on a square. 1: all of it comes from how hard each wheel lands. Between: a share of each.")]
    [Range(0, 1)] public float wearByLanding = 0.5f;
    [Tooltip("A wheel coming down this fast (m/s) does a full quarter second's damage to the square it lands on; twice as fast does twice, and that is the most.")]
    [Range(0.2f, 5)] public float wearLandSpeed = 2f;
    [Tooltip("Seconds between trucks setting off from each end of the Wear ground's roads.")]
    [Range(1, 30)] public float wearTruckEvery = 4f;
    [Tooltip("1: trucks wear the road on a map. 0: they do not.")]
    [Range(0, 1)] public float mapWear = 1f;

    [Header("Loose gravel (everywhere there is any)")]
    [Tooltip("How well wheels hold sideways on gravel that is not packed, as a share of what they hold on packed gravel on Earth, and the same on every planet. 1: as well as on packed.")]
    [Range(0.02f, 1)] public float looseGrip = 0.1f;
    [Tooltip("How hard loose gravel throws a truck's tail about, and how much less the wheel answers. 0: not at all.")]
    [Range(0, 3)] public float looseFishtail = 2f;
    [Tooltip("A truck sliding more than this far sideways on loose gravel (degrees between where it points and where it is going) has spun out.")]
    [Range(10, 80)] public float spinAngle = 22f;
    [Tooltip("Seconds a truck that has spun out slides with its wheels locked before its driver has it back.")]
    [Range(0, 8)] public float spinSeconds = 2.5f;

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

    [Header("Maps")]
    [Tooltip("How far apart the two towns are on the middle map (m). The land is made again when this changes.")]
    [Range(40, 400)] public float mapDistance = 150f;
    [Tooltip("How far the humps and hollows on a map rise and fall (m). The stations use roughHeight instead.")]
    [Range(0, 1)] public float landRoughness = 0.35f;
    [Tooltip("Seconds between trucks setting off from each town, once the towns are joined.")]
    [Range(3, 60)] public float truckEvery = 12f;

    [Header("Planet (everywhere)")]
    [Tooltip("Gravity as a share of Earth's, for trucks, driven vehicles and blobs. 1 is Earth, 0.38 Mars, 0.16 the Moon.")]
    [Range(0.16f, 1)] public float planetGravity = 0.16f;
    [Tooltip("[Claude] 1: wheels push, brake and hold sideways in proportion to gravity, as tyres do, so a truck climbs about the same slopes on any planet. 0: wheels bite as hard as on Earth whatever the gravity, and low gravity makes every climb easy.")]
    [Range(0, 1)] public float gripFollowsGravity = 1f;
    [Tooltip("[Claude] 1: springs are as soft as the gravity, so a vehicle rides at the same height on any planet and bounces slowly. 0: Earth's springs everywhere: it rides high and is thrown about more.")]
    [Range(0, 1)] public float springsFollowGravity = 1f;
    [Tooltip("[Claude] Everything runs this many times as fast, for watching hundreds of trucks go by. 1 is normal. Players walk faster too.")]
    [Range(1, 10)] public float fastForward = 1f;

    [Header("Claude's additions (maps, and the Spin-out ground): 0 switches each off")]
    [Tooltip("Trucks pack the gravel they drive over: how much of the packing each truck does to the squares under its wheels, four times a second. 0.25 is one click's worth. 0: only clicks pack.")]
    [Range(0, 1)] public float truckPacking = 0.02f;
    [Tooltip("1: a click on the hot spot also does one ordinary click on every other square in the same row across the road. 0: the hot spot only doubles its own square.")]
    [Range(0, 1)] public float hotSpotRow = 1f;

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
