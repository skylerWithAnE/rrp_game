# Game design

Written 2026-10-04 in the redesign session and added to as each station was built. This replaces
the design of the first prototype, which is in git history (commit `4348b8e`).

**All five stations were built, played and accepted by the user by 2026-10-05.** What comes next
is in `PLAN.md`. How the code works is in `TECH_PLAN.md`.

Decided by the user on 2026-10-05 for the prototype that follows the stations: several maps to
try different distances; no end, score or clock; wear off; a second player identical to the
first; towns as in the first prototype, with little time spent on them; and the session that
builds it may add systems that streamline the gameplay. The detail is in `PLAN.md`.

**The maps were built on the night of 2026-10-05 while the user was asleep.** They are described
under "The maps" below. Every choice in that section is Claude's, made under the latitude the
user gave for that push, unless it says otherwise.

**The user played the Short map the next morning** and said: "The short playtest went great. The
game is looking much more like what I envisioned." That covers the Short map as it stood, with
both of Claude's added systems on at their defaults; the user did not comment on either by name.
The Middle, Long, Climb and Switchback maps have not been played. What was seen is in
`PLAYTEST.md`.

Decided by the user on 2026-10-05, after that playtest:

- **A new map for the winding road**, with **static pieces that cannot be destroyed**, "to
  encourage/funnel players into needing to use turns to navigate up a hill".
- **A limit on how steep a rope may be**, "so that we do not allow undriveable roads". The user
  asked for the limit to be found, not chosen.
- **Building and confirming each mechanic on a station first worked well** and is how to go on.

Decided by the user later on 2026-10-05:

- **The Stations map is cut into several, each with its own focus**, grouped sensibly and not one
  per station: "a test for trucks with road types and turns, a test for building roads, and so on".
- **A system for making junctions**, on a new test ground of its own. How is left to Claude:
  "If questions come up, create stations demonstrating each desirable answer."
- **A quarry station**, "where players load gravel into a truck. Trucks will then drive on a
  service road to a location where it will wait for players to unload it."
- **A service road** is "a road that the crew uses that should not be used by the trucks
  traveling between towns".
- **Zoning tools** "that will let them assign roles to road segments".

How each of these was done is Claude's and is described under "Test grounds, junctions, roles and
the quarry" below.

Decided by the user on 2026-10-05, after trying the test grounds:

- **Building junctions "looks good, and intuitive".** Zoning was not tried by hand but "clearly
  worked" in the example. The things Claude reported as looking off "are fine".
- **Trucks at junctions turn too tightly: keep them in their right-hand lanes.**
- **A dev tool to complete work instantly**: unlevelled to levelled in one click, and levelled
  to compacted gravel in one click. **And a flying, no-clip mode** for the debug controller.
- **The quarry is bigger, and an actual dug-out part of the world**, and trucks take **a winding
  road in and out of it**.
- **In the quarry, click once to load your shovel, then click again to fling the gravel to the
  truck.** Unloading: the first click on the truck starts **placing the pile**; the player
  places it, then uses one click to unload from the truck and one to fling to the pile.
  Nothing has to show the gravel in the air yet.
- **Trucks are sent to destinations.** Players will create truck depots and pick one when
  sending a truck. On the Quarry test ground the player controls for that are not built: there
  is one static drop-off, the only place to send the truck.
- **Gravel is unlimited, but has to be sourced from quarries.** Smaller finite sources for
  before a crew has a quarry are for the next prototype.
- **A final process for paving and painting the roads, on a new test ground**, at Claude's
  discretion: "I'm imagining the use of dump trucks, shovels, steam rollers, but would like to
  see where your initial design decisions lead us".
- **Notes are kept for the next prototype**: `NEXT_PROTOTYPE.md`.

Decided by the user later on 2026-10-05:

- **Test player driving**: a test ground with a drivable pick-up truck, a steam roller and a
  front loader.
- **A drivable painting truck, and an additional painting tool players can use by hand.**
- **The asphalt dump truck works similarly to how one would work in reality.**
- **Paving will do more** than it does now: a point to discuss for the next prototype.
- For the next prototype: signage, blocking roads with signs, stop signs, and vehicles damaged
  by "hot" (unspread) asphalt and uncompacted gravel. These are in `NEXT_PROTOTYPE.md`.

Decided by the user on 2026-10-05, after trying the dump truck:

- **Dumping asphalt is automated.** The truck drives up and dumps asphalt, "that the player then
  needs to level with either their shovel or a steamroller". **Players dumping asphalt is cut
  for now.**
- **Shovels are in the players' hands, with procedural animations.** "No physical hand or model
  necessary, just a floating shovel model." For gravel, loading the quarry truck, and levelling.

Everything under "Decided by the user" was said by the user. Everything marked **[Claude]** is a
proposal that the user has seen and not objected to; it is not a decision until it has been played.
Do not add anything that is not on this page. If something seems missing, ask.

## The game

An online co-op crew builds a road that a box truck can drive. In first person, the crew sets a
survey line, brings the ground to that line, lays and compacts gravel, and a truck drives the
result.

This is a mechanics prototype. It has maps with a town at each end, and no job, score or clock.

## Decided by the user

- **First person.** Eye height 1.45 m as a starting point. Field of view 90 degrees horizontal.
- **The blob walks at 4.2 m/s and sprints at twice that while Shift is held** (2026-10-04, after
  walking station 1). A real walking pace of 1.4 m/s was far too slow: "3-6x faster".
- **The truck and road sizes look good** (2026-10-04, after walking station 1).
- **The tightest turn players may build, for now, is the hairpin 15 m across its inside edge:**
  inside edge radius 7.5 m, centre line 11 m, outside edge 14.5 m (2026-10-04, after walking
  station 1).
- **Blobs stay** as placeholder characters, 1.6 m tall.
- **Several tools**, each with one job. The shovel is no longer the only tool.
- **Throwing cubes is out.** Dirt flinging may come back later; it is on the back burner.
- **Multiplayer is the point.** Every system is built and verified networked from the start.
- **Look and fidelity are parked.** Grey boxes and flat colors.
- **The truck is a real-world U-Haul box truck.** The Satisfactory truck is much too large.
- **The road has two lanes.**
- **The sizes table below is the anchor for the models.**
- **30 minutes is the time it takes to try every component of the prototype.** It is not the length
  of a job.
- **The questions are answered by playing stations**, not on paper. The station table below is
  complete for this prototype.
- **The road-building loop:**
  1. Set up surveying equipment to establish a line.
  2. Click around the area the equipment covers. The ground moves toward the line. Wild clicking
     is safe because the rate is capped.
  3. Ground 1.0 m off the line takes about 10 clicks to bring level.
  4. Lay gravel on the levelled ground, then compact it.
  5. Oil, steamrolling and asphalt are options for later steps, at Claude's discretion.
- **What the references mean:** Valheim is the size of one terraforming action, not the size of the
  world. Lethal Company is first person, simplicity and low fidelity. Satisfactory is where the
  idea came from.

## Sizes

All in metres, against a 1.6 m blob. The user walked station 1 on 2026-10-04 and said the truck and
road sizes look good. Eye height and field of view were not commented on.

| Thing | Size | Against the blob |
|---|---|---|
| Eye height | 1.45 | Level with the bottom of the cab window |
| Truck (15 ft U-Haul) | 2.45 wide, 6.8 long, 3.1 tall | One and a half blobs wide, four long, two tall |
| Truck wheel **[Claude]** | 0.75 across | Mid-thigh. The user first said knee height (0.45), which does not fit a real box truck |
| Walk, sprint | 4.2 m/s, 8.4 m/s | Decided by the user after walking station 1 |
| One lane | 3.5 | Under a second to walk across |
| Road, two lanes | 7 | Under two seconds to walk across |
| Tightest corner | 15 across the inside edge: radii 7.5 inside, 11 centre line, 14.5 outside; 29 across the outside | Decided by the user after walking station 1. Seven seconds to walk across the outside |
| One section of road | 20 long (three truck lengths), 140 m² | Five seconds to walk along, two and a half to sprint |
| One click | 0.1 of height | Ankle height; ten clicks is knee to mid-thigh |
| Hop | 0.6 high | Feet to mid-shin. [Claude] |
| Shoulder | 1.75 wide, falling 0.3 | One more cell outside each edge of the road. [Claude's numbers] |

Truck figures were checked against U-Haul's published figures on 2026-10-04:

- U-Haul publishes the inside of the box (15 ft by 7 ft 8 in by 7 ft 2 in), the deck height (33 in,
  0.84 m) and a clearance height (11 ft, 3.35 m). It does not publish the outside length or width,
  the wheelbase, the tyre size or a turning circle.
- **Width is 2.45 (user, 2026-10-04: "that seems reasonable").** 2.3 was narrower than the 2.34 m
  inside of the box. 2.45 is an 8 ft box body, from Claude's memory.
- Length 6.8 and height 3.1 agree with a third-party page (22 ft 6 in, 9 ft 11 in). The 11 ft
  clearance figure is a rounded-up warning, not the height of the truck.
- The 0.75 wheel is the tyre of the Ford E-450 the truck is built on, from Claude's memory.
- The real truck's turning circle is still unchecked. U-Haul's sales site puts its 14 ft box on a
  158 in wheelbase and its 17 ft on 176 in; Claude's memory is that such a chassis turns in a circle
  15 to 17 m across, kerb to kerb. The corner the user chose is 29 m across its outside edge, so it
  is well inside what the real truck can do.
- **The yard shows three hairpins (user, 2026-10-04: "build all 3 into the scene. The scene is
  about visualizing decisions").** Each is 15 m across, measured at the outside edge, the centre
  line and the inside edge of a 7 m road. Their inside edges have radii of 0.5, 4 and 7.5 m. The
  user walked them and chose the third.

## Test grounds, junctions, roles and the quarry

Built 2026-10-05 at the user's request (see the decisions above). **Everything in this section
is [Claude]'s way of doing what was asked.** The user has tried the junction ground and said
building junctions is good; the reworked quarry, the dev tool, flying and the paving ground
have not been played.

### The test grounds

The host's choices are now two rows of buttons: seven test grounds and five maps. A test ground
is flat yard with a few plots on it, each plot one thing to try.

| Test ground | What is on it | What it is for |
|---|---|---|
| Yard | The parked truck, the road strip, the three painted hairpins (station 1) | Sizes against the blob |
| Building | Clicking to a line: level, climb and fall (station 2). The bare hillside to stake (station 3). The level section to gravel (station 4). The hairpin's stakes on rough ground | The three tools |
| Trucks | The finished road and the bad road (station 5). The wear road. The finished hairpin. Four ramps | What a truck can and cannot drive |
| Junctions | A T, a crossroads and a fork, finished, with trucks. A rough field with a road across it to rope a branch to | Junctions |
| Quarry | A quarry pit, a winding service road, a drop, and a road that needs gravel | Loading and hauling gravel, sending trucks, service roads, zoning |
| Paving | A gravel road to pave with a dump truck and a roller, and the same road finished | Paving and painting |
| Driving | A pick-up, a roller, a front loader and a paint truck to drive; a road in four states; a gravel pile; a rough field | How driving feels, and what each vehicle does |

- **The four ramps** each climb for two sections, level off and come down, so trucks from both
  ends meet the same climb. 14 degrees bare: every truck wrecks. 14 degrees under loose gravel:
  every truck arrives. 20 degrees under loose gravel: every truck wrecks. 20 degrees packed:
  every truck arrives. They are steeper than `maxSlope` lets a player rope, because they are
  there to show the limits. A ramp one section long showed nothing: a truck gets up 20 m of
  anything on the speed it arrives with.
- Players start beside the parked truck's place on every test ground but Trucks, where they
  start at the near end of the row of roads.
- Trucks can now be sent down the hillside's road from the F1 panel, as well as the clicking
  and gravel plots'.

### Junctions

- **A stake takes up to four ropes** (`ropesPerStake`; 2 switches junctions off, 3 allows a T or
  a fork but no crossroads). **On the Junctions and Quarry test grounds only**, until the user
  has played it: everywhere else a stake still takes two.
- **A branch must leave at least 60 degrees from every rope already at the stake**
  (`junctionAngle`). The bend limit is for a road carrying on through a stake; this is for a
  road leaving one.
- To make one: with the stake tool, choose a stake that already has two ropes and click the
  ground to one side, or click another stake.
- **The ground at a junction.** There is no one bend to mitre, so each section runs on 5.25 m
  past the stake and they overlap; a point belongs to the nearest centre line, as it always
  has. Each section stays level at the stake's height for its first 5.25 m, so the junction is
  flat. The far side of a T comes out square, a road's width, not shouldered.
- **Trucks at a junction.** A truck drives from one end of the road to another, and where there
  are more than two ends it picks its two at random each time. **It keeps to its right-hand
  lane up to the level ground round the stake and turns there**, on a curve from the end of its
  lane to the start of the next, swinging a little wide on a right turn (user, 2026-10-05; it
  used to cut straight across). Nothing gives way to anything.
- **The questions this raised, and the stations that answer them:** which junctions to allow
  (the T, the crossroads and the fork stand side by side, each with trucks); how close two
  branches may be (the fork is at the 60 degree limit); whether a junction can be built by
  hand (the field). In a check all three finished junctions passed every truck.
- Not built: curved or widened junctions, rules for who goes first, and junctions on the maps.

### Roles, and the zoning tool

- **Every section of road has a role**: a road for everyone, or a **service road**. A new rope
  is a road for everyone.
- **Trucks driving from end to end, or from town to town, never use a service road.** It is not
  part of their road at all: the ends they drive between are the ends of the roads for everyone,
  and on a map the towns are joined only when roads for everyone join them.
- **The zoning tool is key 4.** The section under the crosshair is outlined, red for everyone or
  blue for service, a line under the crosshair says which it is, and a left click gives it the
  other role. It works on every plot and map.
- A service road is drawn in blue where a road for everyone is tan: its rope, its bare ground
  and its shoulders. Gravel looks the same on both.

### The quarry

Rebuilt the same day to the user's second description. What the user decided is in the list at
the top; the sizes and the details here are Claude's.

- **The pit.** The quarry's ground is 8 m higher than the yard, and the quarry is a cone dug
  down into it: 60 m across at the top, 28 m across at its floor, 8 m deep. The rock is in the
  middle of the floor.
- **The winding road.** A service road, finished, leaves a junction with the road for everyone,
  swings north in an S, runs along the pit's rim and winds down inside it three quarters of
  the way round to the floor: about 185 m in all, at 4 degrees in the pit. The cone falls
  exactly as fast as the road does, so the road is a bench cut into its side.
- **Loading.** With the gravel tool: a click on the rock, from within reach, loads your shovel.
  A click on the truck, from up to 16 m (`flingReach`), flings it in. The truck holds 12
  shovels (`haulLoad`). Holding the button and swinging between the two works.
- **Sending.** A truck stands at a depot until it is sent. Right click it, with any tool in
  hand, and it drives to the other depot by any road. The line under the crosshair says where
  it would go. The quarry's floor and the drop are the two depots here, both fixed. The truck
  can be sent with any load, including none.
- **Unloading.** At the drop, the first click on a truck with gravel aboard asks where the heap
  should go: a ring follows the crosshair on the ground, a left click puts the heap there, a
  right click cancels. After that a click on the truck, from within reach, loads your shovel,
  and a click on the heap flings it on. The heap can be moved only when it is empty.
- **Gravel.** The rock never runs out. A shovel on the heap is worth 8 clicks of laying
  (`shovelWorth`). On this ground gravel laid on a road comes off the heap, and with the heap
  empty none can be laid; `gravelFromStock` at 0 makes it free again, to compare.
- **Turning round.** There is nowhere to turn at the end of a road, so a truck that is sent is
  put at the start of its way, facing along it.
- Players start on top, beside the road that needs the gravel: a 40 m road for everyone, on
  its line and bare.

### The dev tool, and flying

Asked for by the user. Neither is part of the game.

- **Key 7 is the dev tool.** The section under the crosshair is outlined in magenta, from up to
  60 m away, and a left click does its next stage whole: every point onto its line; then gravel
  at full depth, packed; then (Claude's addition, for the paving ground) asphalt, rolled; then
  paint. The line under the crosshair says which is next.
- **V toggles flying.** No gravity and nothing is solid. The way the camera looks is forward,
  Space is up and Ctrl is down, at three times walking speed, six with sprint on.

### The shovel in the player's hands

Asked for by the user on 2026-10-05. The motions are Claude's and have not been seen by the
user.

- **A shovel floats at the lower right of the view** while a tool that is a shovel is held:
  grading, gravel or asphalt. It has no hand or arm. With any other tool, and while driving, it
  drops out of sight.
- **Each thing done with it moves it**, on springs, so it settles back by itself:
  grading stabs the blade down and levers back; laying gravel or asphalt pushes forward and
  flicks; packing gravel comes straight down; loading the shovel at the quarry's rock or from
  the truck dips and comes up; flinging whips up and forward.
- **A shovel loaded at the quarry shows its load** on the blade until it is flung.
- It sways a little with each step.
- **Other players see it**: every blob now carries its shovel, and it scoops, flings or slams
  when its player does. (This is the first prototype's blob shovel, which was already there.)
- Not done: the shovel is the same whatever the tool; nothing flies through the air; paint,
  stakes and zoning have nothing in the hands.

### Driving

Asked for by the user on 2026-10-05, to test driving. **A first proposal, not played.** It is
on the Driving test ground only.

- **Getting in and out:** E beside a vehicle gets in, E gets out, on its left side. One player
  to a vehicle and one vehicle to a player.
- **Controls:** W and S drive forward and back, A and D steer. A vehicle steers only while it
  rolls. Letting go of W brakes it. The view is from the driver's seat, first person, and
  turns with the vehicle; the mouse still looks around.
- **The pick-up** is fast (14 m/s, `pickupSpeed`) and does nothing else.
- **The roller** is slow (3 m/s). It packs loose gravel and rolls spread asphalt under it, the
  same as the roller that is sent on the Paving ground, but where the driver takes it.
- **The front loader**: R raises the bucket and F lowers it. Driven into the gravel pile with
  the bucket down, the bucket fills. A left click tips it. Tipped on road that is on its line,
  the gravel is laid there, loose, 2.2 m round; anywhere else it is left as a heap.
- **The paint truck** (asked for by the user): a left click turns its sprayers on and off.
  With them on it paints the lines of the rolled asphalt it drives over, a lane at a time.
- **The ground:** a road whose four sections are bare, loose gravel, spread asphalt and rolled
  asphalt, so each vehicle has something to do; a pile of gravel; a rough field to drive over.
- A vehicle on its side for two seconds is set back on its wheels where it is.
- While driving, the tools are put away.
- **Who works out the motion:** the driver's own machine, which sends where the vehicle is, as
  players do for themselves. So the wheel answers at once for a client as well as for the host.
- **In a check by script:** the pick-up reached about 11 m/s in four seconds and turned; the
  roller packed and rolled its lane; the paint truck painted its lane; the loader filled at the
  pile and laid gravel on the bare section. With a client driving the pick-up in circles and
  the host driving the roller, each saw the other's vehicle where it was, and the road's hash
  agreed afterwards.
- **Open, for the user:** how each vehicle should feel (the numbers are sliders: speeds,
  `drivePower`, `driveTurn`); whether the view should be from the seat; whether passengers
  ride; whether vehicles belong on the maps; whether the sent trucks and roller should be
  driven instead.

### Paving and painting

Asked for by the user, with the process left to Claude: "would like to see where your initial
design decisions lead us". **This is a first proposal and has not been played.**

The process, on a road that is already gravelled and packed:

1. **A dump truck brings the asphalt by itself** (user, 2026-10-05: "let's make it automated";
   the bed that a player raised and the truck that a player sent are cut). A few seconds after
   it arrives anywhere with a load, if packed gravel is still bare where it would tip, it raises
   its bed and drives to the other end. Where there is bare packed gravel behind it, it creeps
   and asphalt runs out of the back in a ridge as wide as the truck; elsewhere it drives at
   speed. One load covers both lanes of the test road. Empty, it goes back to the yard and
   fills. It stops when a tenth or less of what it could cover is bare, and no click does
   anything to it.
2. **Players spread it with the asphalt tool, key 5.** Hold left click on the road, as with
   gravel: two clicks spread a square (`spreadPerClick`). A square can be spread only if there
   is asphalt somewhere in its row across the road, so the work goes outward from the ridge,
   and a lane the truck has not driven cannot be started.
3. **Or a roller levels it, and rolls it** (user: "level with either their shovel or a
   steamroller"). It stands off the road at the far end. Right click it and it drives the road
   in its lane, slowly (`rollerSpeed`). Any grid square under it that has asphalt anywhere in
   it is pressed out flat across the whole square and rolled, so the roller alone finishes a
   lane the truck has tipped on. Send it back for the other lane. Asphalt it has rolled is
   blacker and lies lower. The roller on the Driving ground does the same where it is driven.
4. **Players paint the lines with the paint tool, key 6.** Hold left click on rolled asphalt:
   one click paints a square's share of the lines, a white line inside each edge and a broken
   yellow one down the middle.
5. **Or by hand with the brush, key 8** (asked for by the user as an additional hand tool).
   It paints wherever it points on rolled asphalt, a hand's width at a time, as fast as the
   hand moves: left click white, right click yellow. It is for drawing, not for work: no
   squares, no cap, no hot spot. The paint tool's lines go over it.
6. **Or with the paint truck**, which a player drives: see "Driving" below.

- **What it is for:** trucks drive half as fast again on rolled asphalt (`pavedSpeed`). That is
  the only effect; the lines are for looks.
- The second road on the ground is the same road finished, with trucks on it, to see where it
  is going.
- The hot spot works for spreading and painting as it does for grading and gravel.
- In a check with nobody touching anything but the roller, the truck tipped both lanes and
  went home, and two passes of the roller left the whole 60 m road level and rolled.
- **Why these choices:** each machine does the part a machine does (bring the stuff, press it
  flat) and is sent, not driven, which is the system trucks already have; each hand job is the
  hold-to-click on squares that grading and gravel already are. The row rule is there so that
  where the truck drove matters.
- **Open, for the user:** whether the roller should be driven by a player; whether the truck
  should tip one heap to be shovelled from, as gravel is, and not a ridge; whether asphalt
  needs gravel to be packed first or just laid; what else paving should do. More are in
  `NEXT_PROTOTYPE.md`.

## The maps

Built 2026-10-05, unattended. **The user's decisions** are the ones listed in `PLAN.md`: several
maps to try distances, 150 m to start with the distance on a slider, the host picks, the stations
stay as one choice, the three tools work anywhere, trucks set off once a chain of stakes joins
the towns and keep coming in both lanes, towns are a few blocks and a pad, no end, wear off, a
second player the same as the first. **Everything else below is [Claude]**. Only the Short map
has been played by the user.

What a player does: choose a map, press 1, click the thick stake in town A, walk toward the pole
at town B clicking the ground to put stakes down, and rope the last one to town B's stake. Trucks
start at once. Then grade and gravel what they are wrecking on.

| Map | Between the towns | The land | What it is for |
|---|---|---|---|
| (the test grounds) | | The five stations as accepted, in four groups: see above | The reference for how each mechanic felt |
| Short | 60 m | Nearly flat, 70 m wide | One player finishing a whole road in a sitting |
| Middle | 150 m, on the `mapDistance` slider (40 to 400) | A 9 m hill on the straight line between the towns, a hollow to its right, flat ground to its left; 100 m wide | The starting distance. Over the hill is short and a lot of clicking; round it is longer and easy |
| Long | 300 m | Rolling, with a 6 m ridge right across it that has one gap, to the right of the straight line, and a hollow further on; 100 m wide | Whether a long road is something a crew wants to build |
| Climb | 160 m, and town B is 16 m higher | The rise is gentle on the left (spread over 150 m) and steepens to the right: about 31 degrees on the straight line, a cliff at the right edge; 130 m wide | Land that makes the road wind. Straight is too steep for a truck even on packed gravel |
| Switchback | 150 m, and town B is 12 m higher | Two banks right across the map, each 6 m high, each with a row of rocks along it and one way up: on the left for the first, on the right for the second; 110 m wide | The user's winding road: the road has to swing from one side of the map to the other. See below |

- **The host picks the map** from buttons at the top of the screen. Everyone starts again at town
  A. A new game starts on the Middle map; the other option was to start on the stations.
- **Each map is the same every time**, so two sessions can be compared. "Make new land for this
  map" on the F1 panel makes a different one; choosing the map again brings the first back.
- **The land's humps and hollows are 0.35 m** (`landRoughness`), against 1 m on the stations. The
  stations' roughness was chosen to show the clicking; on a long road it is most of the work.
- **A town** is a fixed stake, twice as thick as the players', on level ground 13 m in radius;
  behind it a painted pad, five blocks and a 26 m pole in the town's color, to walk toward. Town
  A is red and town B is blue. A town's stake cannot be pulled out or raised.
- **The road is the chain of ropes from town A's stake to town B's.** Stakes can go down in any
  order; all the stations' rules for ropes still hold. Up to 250 stakes.
- **Trucks:** once the chain is whole, one leaves each town every 12 seconds (`truckEvery`), up to
  six on the way in each lane, whatever the road is like. Break the chain and no more set off;
  the ones on the road carry on along the road as it was.
- **A truck is stuck when it gets no further along the road for 4 seconds** (`lorryStuckSeconds`),
  as well as when it stands still. Without this, trucks that could not climb an unfinished hill
  slid back and forth for ever, and queued. This applies on the stations too.
- **The readout** says how many metres of road are staked out from town A and whether the towns
  are joined.
- The land has an edge. Beyond it is a flat plain to stand on; the land slopes down to it over
  8 m, and stakes cannot go on that slope.

### The rope's steepest slope, and the Switchback map

Asked for by the user on 2026-10-05. How each was done is **[Claude]**, and neither has been
played.

**How steep a truck can climb** was measured, not chosen: a straight ramp of each slope, 16 m
high, finished three ways, with eleven or twelve trucks sent up it.

| Surface | Every truck arrived | Every truck wrecked |
|---|---|---|
| Packed gravel | every slope tried, up to 25 degrees | none |
| Loose gravel | up to 16 degrees | 18 degrees and steeper |
| Bare ground on its line | up to 10 degrees | 12 degrees and steeper |

Going down, every truck arrived on every surface at every slope. The figures agree with the
truck's push: 4.5 m/s2 on packed gravel, 0.6 of it on loose and 0.4 on bare.

- **A rope may run at 15 degrees at most** (`maxSlope`, a slider, with the other stake rules).
  That is the steepest a truck climbs once gravel is laid, with a degree in hand. A road at the
  limit stops trucks while it is bare, lets them up once gravelled, and they pack it themselves.
  The other choices were 10 degrees (driveable bare, so gravel never matters) and 25 (driveable
  only once packed, which trucks could never reach to do).
- The limit applies on every plot. A stake cannot be put down where the rope to it would be too
  steep, and the wheel will not raise or lower a rope past it.
- **The red rope now says why**: a line under the crosshair names the rule it breaks (too steep,
  too sharp a bend, too far, too close, a rock in the way, and so on). The user asked on
  2026-10-05 for placing a stake to show when it breaks the limits; the color did, the reason
  did not.

**The Switchback map.** The static pieces are **rocks**: dark boulders 5 to 7 m across that
nothing moves, digs or grades. No stake may stand in one and no section may come within a
road's half-width of one. They are the same on every machine and every time.

- The land is two flat terraces and a top, separated by two banks. A bank is too steep to rope
  (6 m up in about 10 m) and has rocks along it from edge to edge, except at its one way up: a
  ramp 16 m wide at 12 degrees. The first bank's ramp is 22 m left of the straight line and the
  second's is 22 m right.
- So the road leaves town A to the left, climbs, crosses the terrace to the right, climbs again,
  and comes back to town B. The route Claude staked to check it is 187 m in 15 ropes, with bends
  of up to 32 degrees.
- **What was checked:** that route can be staked within every rule; a stake in a rock and a rope
  straight up a bank are both refused, with the reason; host and client hold the same ground and
  see the same trucks. On the staked but ungraded road, trucks going up wrecked and trucks
  coming down arrived. With every point put on its line by a test command, every truck arrived
  both ways, even on bare ground, because ropes from stake to stake cut the ramps' corners and
  come out under 12 degrees. So on this map, as on Short, a graded road is enough and gravel is
  not needed. That is a finding, not a decision.
- Not checked: a person finding the route, and whether the turns are fun.

### Systems Claude added, and how to switch each off

The user asked for these to be recorded, explained and switchable. Both work on maps only, so
the stations still play exactly as accepted. Both are sliders at the bottom of the F1 panel.

1. **Trucks pack the gravel they drive over** (`truckPacking`, 0.25; 0 switches it off). Four
   times a second, each wheel packs the grid square it is on by a quarter, where the gravel is at
   full depth. *Why:* packing was four of the seven clicks every square of gravel takes, the
   largest single part of the work, and the trucks are already driving over it. It also gives a
   reason to join the towns early: lay the gravel and the traffic finishes it.
2. **A click on the hot spot also works the rest of its row** (`hotSpotRow`, 1; 0 switches it
   off). The hot click still doubles its own square, and does one ordinary click on every other
   square across the road at that point, shoulders included. *Why:* it is the user's own idea
   ("give value to individual clicks") turned up: aiming well does six squares, so a careful
   player grades several times faster than one holding the button down.

What they do to the time a road takes is in `MORNING.md`, measured with a script that clicks at
the cap.

Not built, and why: helpers that click for the players (it stops being the players' road),
bigger patches on maps (`patchWidth` already does it), a faster click cap (`clicksPerSecond`
already does it).

### Open after building the maps

- Whether any of the four maps is the right distance. That is what they are for.
- Whether the two added systems are wanted, and at what strength.
- A rope laid across a rise runs under the ground between its stakes, where it cannot be seen.
  The ground color still shows where the section is.
- **What gravel is for.** Trucks need nothing but graded ground on the flat and up to 10
  degrees. Gravel only matters between 10 and 15, and nothing on Short or Switchback is that
  steep once graded. See the concerns Claude raised on 2026-10-05.
- Everyone starts at town A. A crew that wants to work from both ends has to walk.

## Stations

One generated map **[Claude: areas of one map, not separate Unity scenes]**. Each station is built,
verified networked, and played by the user before the next one is started.

| Station | What the player does | Question it answers | Minutes |
|---|---|---|---|
| 1. Scale yard | Walks round a grey-box truck, a 7 m road strip and a hairpin | Eye height, field of view, truck and road sizes | 3 |
| 2. Ground clicking | Clicks rough ground to a line that is already set, with sliders for patch width and height per click | Patch size, square cell or round brush, click rate | 7 |
| 3. Setting the line | Puts the equipment down on a slope and levels to it | How the line is set and seen, flat or sloped | 7 |
| 4. Gravel | Lays gravel on a levelled section and compacts it | Whether surfacing is fun or a second chore | 5 |
| 5. Truck | Watches a truck drive a finished section and a bad one | Whether the truck works as the judge | 5 |

## Claude's proposals

Needed before the stations can be built. The user said to use discretion; change any of them.

- **The survey equipment is two stakes.** The line runs straight between their tops and is shown as
  a string. Stakes at the same height give a flat line; at different heights, a slope.
- **One setup covers a strip 7 m wide between the stakes, up to 20 m long.** Clicking outside a
  strip does nothing.
- **Earth is free.** A click below the line adds ground from nowhere; a click above removes it.
  There is no hauling until flinging comes back.
- **The ground never passes the line.** Clicks per second is a slider.
- **Gravel and compaction are two tools**, one job each. Built on 2026-10-05; see stations 2, 3
  and 4 below.
- **The truck drives itself** and fails for a visible reason. Carried over from the first
  prototype, where it was the part that worked. Built on 2026-10-05; see stations 2 to 5 below.
- **Build and verify for 4 players.** Carried over from the first prototype.
- Every number on this page lives in the one tuning asset and is adjustable during play.

## Open

- Patch width per click. Station 2 exists to find it. At 1 m across, one 20 m section is about 700
  clicks; at 4 m across it is about 56.
- What a second player does that the first does not. Asked, not answered.
- Road length, map size, session length and score. Deliberately left until the user has timed how
  long one section takes by hand.
- Where earth comes from and goes, once it stops being free.
- What the land fights back with (rocks, trees, steepness, water). Not asked for; not in this
  prototype.
- Oil, steamrolling, asphalt.

## Stations 2 to 5: ground clicking, setting the line, gravel, the truck

Station 2 was played by the user on 2026-10-04 in its first form (one level strip, one press per
click) and station 3 in its first form on 2026-10-05, and kept playing as stations 4 and 5, the
wear road and the hairpins were added. On 2026-10-05 the user tested the last unverified parts
(mitred corners, the green and red rope, the one gravel tool, the hot spot stopping) and said
**"they look good"**. The user also said: **"I'm enjoying the explosions and
jank-ness right now."** Keep the trucks' wrecks and rough physics; do not smooth them out.

Decided by the user after playing station 2:

- **The clicking "looks great. Much more satisfying than the old system."**
- **Holding the button repeats clicks.**
- **A shoulder: one extra square outside each edge of the road, where the road is crowned off.**
  It is a bonus for making a better road, not a requirement.
- **The stakes look good.**
- **Each stake must be able to be linked to another road section.**
- **Inclines and declines must be tested with this system.**
- **The hop is back** (Space).
- **The rope at a stake is raised and lowered with the mouse wheel, at the stake.**
- **Stakes must not be able to overlap** (said of two stakes 2 m apart with their sections lying
  over each other).
- **A hot spot:** a click on it doubles the action. Wanted for levelling "and probably future
  steps". It appears after a first click in a grid square and stays still. It jumps to a new place
  only when it is clicked, or when the player clicks into a different grid square. "The goal is
  to give value to individual clicks."
- **Trucks drive in both lanes, in opposite directions.**
- **"The trucks are perfect. I like that they run on the incomplete road."** The two limits (they
  cannot drive a road staked out on station 3, and do not say why they failed) are fine for now.
- **Joining two sections at a stake was not working.** The user wants to see a road as a curve,
  "exactly like we saw in the hairpin examples", but chose to **see the mitred corner first**
  before committing to curved sections, with the bend restricted by angle if needed.
- **Placing a stake must show when it breaks those limits.**
- **A station with stakes laid out round the hairpin, to level in game**, and hairpin roads for
  the trucks.
- **The hot spot stops when the current stage is complete.**
- **Switching tools for gravel felt bad: gravel and packing are one tool.** Textures and particle
  effects will make it more convincing later.
- **Trucks damage the road, on one example station only for now, as an experiment.** A truck
  deals a random amount of damage to a square; if that takes the square's health below a
  threshold, the terrain changes where the wheels touch. The damage factor is to be turned up
  high to see results, then down for balance.
- **Sprint is a toggle.**
- **Stake work depends on the selected stake:** select a stake, move away from it, and place the
  next one with left click. After the two are linked, further changes are made from there,
  including the height of the line.
- The user asked how to remove a stake and did not answer the proposal of the X key; it was built
  that way. **[Claude]**
- The user asked why one lane had a half-sized tile. A 2 m cell does not divide a 7 m road. Cells
  now come in whole numbers to a lane and to a section, as near the patch width as that allows:
  at the 2 m setting they are 1.75 m across the road and 2 m along it.

As built **[Claude]**. The user has played all of it and accepted it; the particular numbers
are still Claude's and are sliders:

- **A section is two linked stakes.** The line runs straight between the two stake heights. The
  road is level across; each shoulder is 1.75 m wide and its line falls 0.3 m from the road's edge
  to its outside. Two stakes can be linked from 7 m up to 20 m apart.
- **Corners are mitred.** Where two sections meet at a stake, both are cut along the line that
  halves the bend, so they meet edge to edge with no gap and no overlap. The grid squares at the
  cut are wedges, and the line's height is the stake's height all along the cut.
- **The limits on a rope:** stakes stand at least 6.5 m apart and at most one section (20 m); a
  stake takes two ropes, so a road is a chain with no junctions; the road bends at most 36 degrees
  at a stake; a stake cannot go on ground a section already covers, and a section cannot run over
  another. 36 degrees at 6.5 m is about the user's tightest turn. The spacing and the bend are
  sliders. The rope that would be made shows green if it is allowed and red if it is not.
- **A stake can be linked to any number of others**, so sections share stakes and a road is a
  chain of them. Where two sections overlap, each point belongs to the one whose centre line is
  nearer.
- **Station 2** is rough ground with four stakes already set: a level section, one climbing 2 m in
  20 m, and one falling 2 m.
- **Station 3** is a bare hillside, 44 m by 54 m, beside where players start.
- **Stakes are their own tool**, and everything it does starts from the chosen stake, shown
  white. Left click on the ground puts a stake down at the height of the ground there, roped to
  the chosen stake, and the new stake becomes the chosen one. Left click on a stake ropes the
  chosen stake to it and chooses it. The wheel moves the chosen stake's rope 0.1 m a notch and X
  pulls the chosen stake out with every rope tied to it, wherever the player is looking; the
  ground stays as it is. Right click lets go of the chosen stake. A white string shows a rope
  before it is made, and a dark one means it cannot be.
- The host's "Make the ground again" button starts stations 2, 3 and 4 over.
- **Three tools, one in the hands at a time,** on keys 1 to 3: stakes, grading and gravel. The
  last two use the same held left click, patch and hot spot.
- **Gravel** goes only on road that is on its line, 0.05 m a click up to 0.15 m deep. It is free,
  like earth. Shoulders take none. Once a point's gravel is at full depth the same clicks pack
  it, four to finish, and packed gravel sits a quarter lower and darker.
- **Station 4** is one 20 m section that is already level, behind and to the left of where
  players start, so gravel can be tried without grading first.
- **The hot spot** is a yellow ring 0.35 m in radius. A click with the crosshair on it counts
  double. It goes when its square has nothing left for the tool in hand to do. Each player has
  their own, and it is not sent to anyone else: the host takes the clicking player's word for it.
- **The wear road** is a finished 40 m road, the furthest left. Four times a second each truck on
  it takes a random amount, up to 30 out of 100, off the health of the square under it. Below 50,
  each wheel cuts up to 0.05 m where it touches: packing is lost, gravel is scattered, then the
  ground ruts. Working a point with a tool makes it sound again. In a check it went from fully
  packed to half packed in about a minute. All three numbers are sliders.
- **The hairpins** are two more roads behind the row: eight stakes round the tightest turn the
  user allows (7.5 m inside radius), in five mitred sections of 36 degrees. One is on rough ground
  for the players to level and gravel; the other is finished. Trucks run on both.
- **Station 5** is two roads, 40 m each with a gentle climb, further left than station 2: one
  finished (level, gravelled, packed) and one bad (rough bare ground). Two trucks run on each, and
  on the wear road and the hairpins, one each way in its own right-hand lane, and a new one sets
  off 3 seconds after the last arrived or was wrecked. The host can also send a pair down station
  2 or station 4 from the panel.
- **The truck** is the real size, 3,600 kg, on four sprung rays, and steers itself along its lane
  at 6 m/s. **Its wheels bite according to the surface:** full push on packed gravel, 0.6 of it on
  loose gravel, 0.4 on bare road. So a hump it climbs on a finished road stops it on a bad one.
  Still for 4 seconds, or on its side, it bounces away and blows up.
- In the checks the finished road passed every truck, the bad road wrecked every truck, and rough
  station 2 wrecked one of two.
- **Left click, held, on ground inside a section** moves one patch 0.1 m toward the line and never
  past it, at most 4 times a second per player. Ground outside every section cannot be changed.
- A square patch is one cell of its section's grid; each shoulder is one cell wide. A round patch
  sits wherever the click lands. A white outline shows the patch before the click.
- Ground on its line turns a lighter color. The readout shows, for each station, how much of the
  road and of the shoulders is on the line, and the clicks taken.
- Sliders: patch width, height per click, clicks per second, square or round, soft edge, reach,
  starting roughness, the climb per section, shoulder width and shoulder drop, hop speed, gravel
  depth, gravel per click, packing per click, the hot spot's bonus and size, the truck's speed,
  push and patience, stake spacing, the bend limit, and the three truck-damage numbers.
- Ground points are 0.25 m apart. The host decides every click and every stake and sends the
  results; a player who joins later is sent both plots as they stand.
- Not asked for, added by Claude to make the stations usable: the patch outline, the link preview,
  the lighter color for finished ground, the level and click counts, the soft-edge slider, the
  reach limit.

Open after building:

- ~~Whether a rope should have a steepest allowed slope.~~ Decided by the user 2026-10-05: yes.
  It is 15 degrees; see "The rope's steepest slope".
- Whether gravel and packing are fun or a second chore: station 4's question, not yet played.
- Whether the truck works as the judge: station 5's question. It does not say why it failed; the
  player has to see it. It cannot yet drive a road the players staked out on station 3.
- What a player sees of the tool in their hands. Only the readout and the outline's color say.
- Curved sections in place of mitred corners. The user has now played the mitre and has not
  said which to keep.
- Junctions. A stake takes two ropes, so roads cannot branch or cross.
- Whether stakes should be movable sideways as well as up and down.

## First build step: station 1, the scale yard

**[Claude]** proposal for the next session. Nothing else is built until the user has walked it.

- A flat grey ground. A first-person blob: eye at 1.45 m, 90 degrees horizontal, walk and look only.
- A static grey-box truck at 2.3 by 6.8 by 3.1 m on 0.75 m wheels.
- A 7 m wide, 20 m long road strip marked on the ground with a centre line, and a hairpin 15 m
  across marked the same way.
- A second player can join and both see each other, so a 1.6 m blob can be judged beside the truck.
- Sliders for eye height, field of view and walk speed.
- **Accepted by the user 2026-10-04.** All three hairpins stay in the yard.
- **Built and walked by the user 2026-10-04.** As built: the truck stands in one lane of the
  strip; the truck, lane, section and hairpin sizes are sliders too; each thing has its size
  written over it. The hop, the shovel and the first prototype's sliders are switched off. After
  the walk the user raised walk speed from 1.4 to 4.2 m/s and asked for a sprint on Shift.
- No terrain editing, no tools, no moving truck.
- Reuse the session, message layer, blobs and tuning panel from the first prototype. Leave the
  cube, shovel, earthquake, block and town code switched off rather than deleting it in this step.
- **Accepted when** the user has walked it with a second instance connected and either says the
  sizes are right or gives the changed numbers.
