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
under "The maps" below. Nothing in that section has been played by the user yet: every choice in
it is Claude's, made under the latitude the user gave for that push.

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

## The maps

Built 2026-10-05, unattended. **The user's decisions** are the ones listed in `PLAN.md`: several
maps to try distances, 150 m to start with the distance on a slider, the host picks, the stations
stay as one choice, the three tools work anywhere, trucks set off once a chain of stakes joins
the towns and keep coming in both lanes, towns are a few blocks and a pad, no end, wear off, a
second player the same as the first. **Everything else below is [Claude]** and has not been
played by the user.

What a player does: choose a map, press 1, click the thick stake in town A, walk toward the pole
at town B clicking the ground to put stakes down, and rope the last one to town B's stake. Trucks
start at once. Then grade and gravel what they are wrecking on.

| Map | Between the towns | The land | What it is for |
|---|---|---|---|
| Stations | | The five stations, as accepted | The reference for how each mechanic felt |
| Short | 60 m | Nearly flat, 70 m wide | One player finishing a whole road in a sitting |
| Middle | 150 m, on the `mapDistance` slider (40 to 400) | A 9 m hill on the straight line between the towns, a hollow to its right, flat ground to its left; 100 m wide | The starting distance. Over the hill is short and a lot of clicking; round it is longer and easy |
| Long | 300 m | Rolling, with a 6 m ridge right across it that has one gap, to the right of the straight line, and a hollow further on; 100 m wide | Whether a long road is something a crew wants to build |
| Climb | 160 m, and town B is 16 m higher | The rise is gentle on the left (spread over 150 m) and steepens to the right: about 31 degrees on the straight line, a cliff at the right edge; 130 m wide | Land that makes the road wind. Straight is too steep for a truck even on packed gravel |

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
- Nothing limits how steep a rope may be, so a road can be staked that no truck can climb.
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

- Whether a rope should have a steepest allowed slope. Nothing limits it.
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
