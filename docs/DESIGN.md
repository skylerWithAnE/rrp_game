# Game design

Rewritten 2026-10-05 as the game stands, and amended the same day by a design session: "What
the design session decided", "What has been played" and "Open" are new, and the version before
them is commit `2f3f9b9`. The page the rewrite replaced had grown by addition through two
days of building and is in git history (commit `abf2516`), as is the first prototype's design
(`4348b8e`). How the code works is `TECH_PLAN.md`. What is being left for the prototype after
this one is `NEXT_PROTOTYPE.md`.

**How to read it.** Something in **bold with "user" and a date** was decided by the user, and
where there are quotation marks the words are the user's. Everything else is how Claude built
what was asked. A number is Claude's unless it says otherwise, and every number is a slider on
the F1 panel. "Played" means the user has had their hands on it; a great deal here has not been.

## The game

An online co-op crew builds a road that trucks can drive, in first person. They stake out a line,
bring the ground to it, lay and pack gravel, and, on the test grounds so far, pave and paint it.
Trucks drive whatever is there and wreck where it is bad. Gravel comes from a quarry by truck.

It is a prototype for demonstrating and playing with mechanics. **There is no end, no score and
no clock; a second player has exactly the same controls as the first** (user,
2026-10-05). That is the prototype as built. Where the user means to take it is next, and the next build
turns wear on for dirt and gravel.

## Where it is going

The user's long-term goals, set out on 2026-10-05, in full:

> The shape of the final prototype is starting to look like a game that is on a large map with
> multiple cities that need to be connected. Players start with just their shovel, build a dirt
> road for low volume traffic, and start making money. Money is at first spent on buying gravel,
> asphalt, and paint.
>
> Gravel is meant to be the first real step in a fully functional road. We need to introduce a
> lot of hazards in each tier of road to further encourage roadwork. A paved road is almost
> entirely hazard-free. All roads take damage. We need to make the dirt road much more fragile.
> We need more dramatic actions occurring. Suspension leading to more vehicles flying around. We
> need vehicles running into the player, but we don't want to implement that at this point. Keep
> it in the design, but that will be its own system.
>
> Dump trucks, rollers, gravel trucks will be both automated and drivable. Players will
> eventually be able to purchase their own quarry.
>
> Initial painting is handled by hand. Let's make this system hand drawn with a roller brush.
> The paint truck should also work on a hand-drawn mechanic so that the last phase of completing
> a road is hard to nail a perfect "score" on.
>
> These are the current long-term goals of this project.
>
> Scoring systems aren't important. Mentions of score here are more about the value paid to the
> players per road traveler.

None of this is built. A design session later on 2026-10-05 narrowed it and planned the next
build. What it decided is the next section.

## What the design session decided

2026-10-05. Every decision here is the user's, and words in quotation marks are theirs. Anything
marked [Claude] is a proposal or a default the user has not confirmed. None of it is built. The
build that tries it is in `PLAN.md`.

### Changes to the goals

- **"Just the shovel is officially no longer a goal of the final prototype."** Players have their
  tools from the start.
- **Money is for the final prototype, not the next one.** "The players will build a dirt road to
  get initial money. Usage of the quarry will cost money. That is for the final prototype. This
  next prototype will not involve money."
- **Paved roads do not wear for now.** "Paved is so much more sturdy that for now, we will not
  even implement road wear on paved roads."
- **An automated truck and a driven one are different vehicles.** "The gravel truck dispatched
  from the quarry needs to be a separate entity, not something the player can take over. We'll
  experiment with that in the next stage of scenarios." [Claude: assumed to hold for the asphalt
  dump truck and the sent roller too.]
- **"Next build should focus again on small isolated slices."** The large map waits. A road too
  long for one player is built by "multiple players".
- **"Signage is not something we want to incorporate in the design right now."**
- **"Let's backburner hot asphalt until we develop more basic vehicle health systems."**
- **The mechanics played so far stand.** "I'm happy with those mechanics and now I want to add
  more complications to them."

### Another planet

**"I think I want to go to the moon. Not really the moon, but some other planet where we can
have an excuse to lower gravity and cause more problems with small potholes."** It is
"specifically a thought to make things more dramatic and harder for players to predict". On
theming: "It might help with theming. I'm not sure about that." Nothing is themed.

- **Low gravity applies to the blob too**: "Might want to reduce it later, but right now that
  sounds fun." The 0.6 m hop is 3.6 m at the Moon's gravity.
- What follows [Claude]: a truck at 6 m/s kicked up at 1 m/s rises 5 cm on Earth and 30 cm on
  the Moon, and lands 7 m on. Every number measured at Earth gravity has to be measured again:
  the climbs (25, 16 and 10 degrees), the 15 degree slope limit, the 20 m a truck coasts, the
  springs. The user's tightest turn may not be driveable at 6 m/s. Gravity is to be a slider.

### The tiers of road

| Tier | What goes wrong | How fast |
|---|---|---|
| Dirt | Holes and ruts. "Once a square passes a certain threshold, let's scale up damage dealt to it" | "By the fifth, a significant portion of at least one square has been destroyed." "By the fiftieth, the road is probably more of a rut. Untouched surrounding land is likely to be easier to travel at that point." "The fifth and the fiftieth might change to larger or smaller numbers" |
| Gravel, loose | "Maybe unpacked gravel needs to encourage vehicles to spin out. Realistically, vehicles would pack gravel" | Until packed |
| Gravel, packed | "gravel needs to develop potholes quickly, too" | "after 50, we are seeing significant damage to multiple squares. by 500, it is unusable. it would be full of holes, several being very deep, after 250" |
| Paved | Nothing | No wear for now |
| Painted | "Let's plan on driver recklessness as a future hazard that is dampened by painted roads. Not to be worked on this sprint, but something to keep in mind as we further develop traveler AI" | Later |

- **What dirt is for**: "It degrades too rapidly to be something the player should use regularly.
  They can get away with service roads with only on demand traffic as dirt roads." In the final
  prototype it is also where the first money comes from.
- **Wear is part of the next build**: "Let's make wear and tear a part of the next overnight
  build that we're planning right now." "We need some good deterioration systems. Weird, jank,
  emergent dumb game humor."
- **Trucks keep to the road, ruts and all**: "trucks stick to the roads. players can drive their
  pickup all over, though."
- **The slope limit stays at 15 degrees**, though a bare road wrecks trucks from 12: "I don't
  think this is an issue right now. It is worth considering adjusting this so that we can make
  it a 'rake in the yard' for the player to step on."
- [Claude] Trucks packing gravel stays, on its slider: packing in one pass would end the
  spin-out after the first truck. Whether the truck's physics has sideways grip to lose is
  unchecked.
- [Claude] Wear already strips packing before it cuts, so a worn gravel road goes loose again.
- [Claude] Repair is assumed to be the same grading and gravel clicks as building, as on the wear
  road today. The user was asked and answered with signage, since parked.
- [Claude] Damage could come from how hard a truck lands, not only from time on a square, so that
  one hole starts the next. Proposed as a slider beside the present rule.
- At today's traffic (a truck from each town every 12 seconds) the fifth truck is one minute and
  the fiftieth is ten. How much traffic there is matters as much as the damage.

### Gravel delivery

**"Let's stick with a system where the player starts with access to the quarry. They will create
a gravel drop off spot with a distinct survey tool. It will need to be connected to the quarry
via road. The road will not necessarily need to be a service road, the gravel dump truck will be
able to drive along standard zoned roads. When the truck arrives, have it back into the spot,
dump, then return to the quarry. Using a standard road for this jobsite dump can definitely be a
sort-of emergent hazard."**

- Then, replacing "dump": **"The player will unload the gravel after the truck arrives."** The
  unloading as built stays: place the heap, one click off the truck, one fling onto the heap.
- **"Let's also go very high with the ratio of how many clicks it takes to load/unload vs the
  number of tiles that can be covered with gravel. 1 load -> enough gravel to place on 10 tiles.
  Make that as another lever that we can adjust for balance."** [Claude reads "load" as one
  shovel: `shovelWorth` goes from 8 laying clicks to 30, and a truck of 12 covers 60 m of road.]
- **On a bad road the truck is lost**: "I will probably want it to be destroyed to punish players
  for low quality service roads." [Claude: an empty one appears at the quarry after a delay.]
- Travellers stuck behind a truck that is backing in blow up after 4 seconds under Claude's
  stuck rule. The user: "worth revisitng later; we'll see how it works/feels."
- What follows [Claude]: the truck has to reverse and turn round by itself, which no truck can
  today. Someone has to be at the quarry to load, which gives the pick-up and a second player a
  job. A dirt quarry road is ruined by its own truck, so the first gravel may go on the road
  that brings the gravel.

### Painting by hand

**"I would like an isolated scene of hand-painting the roads. We'll decide if it is good by
checking the paint near designated places of the road's square. Add points for paint where we
want it to be (edges and center) and deduce points for paint where we wouldn't expect it."**

- **Fixing a bad line**: "We'll use a tar-spray tool to cover paint. A less precise airbrush
  style of hand painting. Or, we can use a surface grinder. Or, repaving."
- **"I think the mechanic/scoring system will be invisible to the player."** [Claude: the number
  goes on the F3 readout only, so the judging can be checked.]
- What follows [Claude]: paint has to be remembered in cells of about 0.1 m. The width of the
  wanted band against the width of the roller is the main lever. A broken centre line is much
  harder than a solid one. Whether the place for a line is marked on the asphalt is open, and
  the scene is to have a strip of each. The paint truck is assumed to join the scene, spraying
  where it actually is. Repaving is left out of the next build.

### Where the build still disagrees with the goals

| Today | The goals, as they now stand | State |
|---|---|---|
| Separate test grounds, and five maps with two towns each | One large map, several cities | Waits. The next build is slices |
| No money | Money from each traveller; the quarry costs money | Waits for the final prototype |
| A truck needs nothing but graded ground. Wear on one test road | Dirt and gravel wear fast; paved does not | Next build |
| Earth gravity | Another planet | Next build |
| Gravel is free off the Quarry ground; its truck is sent by a click to a fixed drop | A truck that runs its own round to a drop-off the player surveys | Next build, on a test ground |
| Lines painted a square at a time, by rule | Drawn by hand and judged unseen | Next build, in its own scene |
| Vehicles pass through players | Vehicles run into the player | "That will be its own system" |
| Paint does nothing for traffic | Paint dampens driver recklessness | Later, with traveller AI |

## What has been played

As the user told it in the design session of 2026-10-05: "I have played with each mechanic in
their isolated settings". The table this replaces was Claude's guess and understated it. Played
is not the same as commented on: where there are no words, none were given.

| | Played by the user |
|---|---|
| Stakes and rope, grading, gravel, the hot spot, mitred corners, self-driving trucks, wear | yes: accepted, "they look good" |
| The Short map | yes: "The short playtest went great. The game is looking much more like what I envisioned." |
| The Middle map | yes: "I have connected two towns in middle" |
| The Long and Climb maps | no: "kinda too labor intensive for me, so, I would like to see demos of those completed" |
| The Switchback map | not said |
| Building junctions; trucks at junctions | yes: "looks good, and intuitive"; after the change, "I did observe lane-keeping being much more tight" |
| Zoning | not by hand: "I haven't thought about zoning since establishing it" |
| The quarry | yes: "loading, flinging, sending, I have played those mechanics" |
| Paving | yes: "I have tested an asphalt dump truck that placed asphalt on the ground that I leveled with the 'asphalt' tool" |
| Driving, the roller, the brush, the paint truck, the shovel in the hands | yes, in their isolated settings: "I'm happy with those mechanics and now I want to add more complications to them" |
| The rope's slope limit; the dev tool; flying | not said |

## How the user wants it built

- **Multiplayer is the point.** Every system is built and verified networked from the start
  (user, 2026-10-04).
- **Questions are answered by playing, not on paper** (user, 2026-10-04). **Build and confirm each
  mechanic on a station first**: "Using stations to build, test and confirm each mechanic seems
  like it worked out really well" (user, 2026-10-05). Where a question has more than one good
  answer: "create stations demonstrating each desirable answer" (user, 2026-10-05).
- **Look and fidelity are parked.** Grey boxes and flat colors (user, 2026-10-04). Set dressing
  comes later: "We'll work on the mechanics for now" (user, 2026-10-05).
- **"I'm enjoying the explosions and jank-ness right now"** (user, 2026-10-05). Keep the trucks'
  wrecks and rough physics.
- **What the references mean** (user, 2026-10-04): Valheim is the size of one terraforming action,
  not the size of the world. Lethal Company is first person, simplicity and low fidelity.
  Satisfactory is where the idea came from.
- **30 minutes is the time it takes to try every component**, not the length of a job (user,
  2026-10-04).

## Sizes

All in metres, against a 1.6 m blob. **The user walked the scale yard on 2026-10-04 and said the
truck and road sizes look good.**

| Thing | Size | Whose |
|---|---|---|
| Blob | 1.6 tall | user: blobs stay as placeholder characters |
| Eye height, field of view | 1.45; 90 degrees horizontal | user's starting point; not commented on since |
| Walk, sprint | 4.2 m/s, 8.4 m/s; sprint is a toggle | user, after walking the yard: a real 1.4 m/s was "3-6x" too slow |
| Hop | 0.6 high | user asked for the hop back; the height is Claude's |
| Truck | 2.45 wide, 6.8 long, 3.1 tall: a 15 ft U-Haul box truck | user: a real U-Haul, "the Satisfactory truck is much too large"; 2.45 "seems reasonable" |
| Truck wheel | 0.75 across | Claude: the user first said knee height (0.45), which does not fit the real truck |
| Road | two lanes of 3.5, 7 in all | user: the road has two lanes |
| Shoulder | 1.75 wide outside each edge, falling 0.3 | user: one extra square outside each edge, crowned off, "a bonus for making a better road, not a requirement". The numbers are Claude's |
| One section | up to 20 long | Claude |
| Tightest turn | 7.5 inside radius, 11 centre line, 14.5 outside | user, 2026-10-04, chosen from three hairpins painted in the yard |
| One click | 0.1 of height | user: ground 1.0 m off the line takes about 10 clicks |

The truck's length and height agree with a third-party page for the U-Haul; its width is an 8 ft
box body from Claude's memory. U-Haul does not publish them. Its turning circle is unchecked.

## Controls

| Key | What it does |
|---|---|
| W A S D, mouse | Walk and look. Shift toggles sprint. Space hops |
| 1 | Stakes. Left click the ground or a stake; wheel raises and lowers the chosen rope; X pulls the chosen stake out; right click lets go |
| 2 | Grade: hold left click on staked ground |
| 3 | Gravel: hold left click on road that is on its line. At the quarry, the shovel: see "The quarry" |
| 4 | Zoning: left click a section to change its role |
| 5 | Asphalt: hold left click to spread what the dump truck left |
| 6 | Paint lines: hold left click on rolled asphalt |
| 8 | Paint brush: left click white, right click yellow, anywhere on rolled asphalt |
| Right click on a waiting truck or roller | Send it to its other depot, with any tool in hand |
| E | Get into or out of a vehicle. W S drive, A D steer |
| 7, V | Dev: finish a section's next stage in one click; fly |
| F1, F3, Tab | Sliders (host), readout, free the mouse |

**First person. Several tools, each with one job. Throwing cubes is out** (user, 2026-10-04).

## Building a road

The loop is the user's (2026-10-04): set up a line; click the ground toward it, wildly if you
like, because the rate is capped; lay gravel on the levelled ground and compact it. "Oil,
steamrolling and asphalt are options for later steps."

### Stakes and rope

- **The survey equipment is two stakes with a rope between them** [Claude's proposal, played and
  accepted: "the stakes look good"]. Two roped stakes make one **section** of road: the line runs
  straight between the two stake heights, level across the road, with each shoulder's line
  falling away from the road's edge.
- **Everything starts from the chosen stake** (user, 2026-10-05: "select a stake, move away from
  it, and place the next one with left click"). Left click on the ground puts a stake down at the
  height of the ground, roped to the chosen stake, and the new stake becomes the chosen one. Left
  click on a stake ropes the chosen one to it.
- **The rope at a stake is raised and lowered with the mouse wheel** (user, 2026-10-04), 0.1 m a
  notch. X pulls the chosen stake out with its ropes; the ground stays as it is. [The X key is
  Claude's: the user asked how to remove a stake and did not answer the proposal.]
- **A rope that would break a rule shows red, and a rope that can be made shows green** (user,
  2026-10-05: "placing a stake must show when it breaks those limits"). A line under the crosshair
  names the rule.
- The rules for a rope:
  - Stakes stand at least 6.5 m apart, and a rope is at most 20 m (`minStakeSpacing`,
    `sectionLength`). **Stakes must not be able to overlap** (user, 2026-10-04).
  - **The road bends at most 36 degrees at a stake** (`maxBend`). At the closest spacing that is
    about the user's tightest turn.
  - **A rope runs at 15 degrees at most** (`maxSlope`). **The user asked for a limit "so that we
    do not allow undriveable roads", and for it to be found, not chosen** (2026-10-05). It was
    measured: see "What a truck can climb". 15 is what a truck climbs once gravel is laid, with a
    degree in hand. The other candidates were 10 (driveable bare, so gravel never matters) and 25
    (driveable only once packed).
  - A stake cannot go on road that is already staked, and a section cannot run over another.
  - Where a map has rocks, no stake may stand in one and no section may touch one.
  - Up to 250 stakes on a plot.
- **Corners are mitred.** Where two sections meet at a stake they are cut along the line that
  halves the bend, with no gap and no overlap. **The user wanted to see a road as a curve, "exactly
  like we saw in the hairpin examples", but chose to see the mitred corner first** (2026-10-05),
  has played it, and has not said which to keep.

### Junctions

**Asked for by the user 2026-10-05; building them "looks good, and intuitive".** Allowed on the
Junctions and Quarry test grounds only, until they are wanted on the maps.

- A stake takes up to four ropes (`ropesPerStake`: 2 is none, 3 a T or a fork, 4 a crossroads).
  To make one, choose a stake that already has two ropes and click the ground to one side.
- A branch must leave at least 60 degrees from every rope already at the stake (`junctionAngle`).
- At a junction each section runs on past the stake and stays level round it, so the junction is
  a flat pad. The far side of a T comes out square.

### Roles and zoning

**A service road is "a road that the crew uses that should not be used by the trucks traveling
between towns", and players get "zoning tools that will let them assign roles to road segments"**
(user, 2026-10-05).

- Every section is a road for everyone or a service road. A new rope is for everyone.
- Trucks driving end to end or town to town never use a service road, and towns are joined only
  when roads for everyone join them.
- The zoning tool outlines the section under the crosshair, red for everyone and blue for
  service, and a left click gives it the other role. It works everywhere.
- A service road is drawn in blue: its rope, its bare ground and its shoulders.

### Grading

- **Left click, held, on staked ground moves one patch 0.1 m toward the line and never past it**
  (user, 2026-10-04: "the clicking looks great. Much more satisfying than the old system";
  "holding the button repeats clicks"). At most 4 clicks a second for each player. Ground no
  section covers cannot be changed.
- **Earth is free**: a click below the line adds ground from nowhere and a click above removes it.
- A patch is one square of its section's grid, which is fitted so that none is cut short: at the
  2 m setting squares are 1.75 m across the road and 2 m along it, four to the road's width and one
  to each shoulder. A white outline shows it. Ground on its line turns lighter.

### The hot spot

**The user's** (2026-10-04, 2026-10-05): "The goal is to give value to individual clicks."

- **It appears after a first click in a grid square and stays still. A click on it doubles the
  action. It jumps to a new place only when it is clicked, or when the player clicks into a
  different square. It stops when the current stage is complete.**
- It is a yellow ring 0.35 m in radius, each player has their own, and it works for grading,
  gravel, spreading asphalt and painting lines.

### Gravel

- **Gravel and packing are one tool** (user, 2026-10-05: "switching tools for gravel feels bad").
- It goes only on road that is on its line: 0.05 m a click up to 0.15 m deep, then the same
  clicks pack it, four to finish. Packed gravel sits a quarter lower and darker. Shoulders take
  none.
- **Gravel is unlimited, but has to be sourced from quarries** (user, 2026-10-05). That rule is
  in force on the Quarry test ground only, so far. Everywhere else gravel is still free.

### Paving and painting

**The user asked for "a final process for paving and painting the roads", imagining "dump trucks,
shovels, steam rollers"** (2026-10-05), and then, after trying the first version: **"let's make
it automated. Have it drive up, dump asphalt that the player then needs to level with either
their shovel or a steamroller. We'll cut players dumping asphalt for now."** It is on the Paving
and Driving test grounds only. On a road that is gravelled and packed:

1. **A dump truck brings asphalt by itself.** Wherever packed gravel is bare down the middle of
   a lane, it raises its bed and creeps along, and asphalt runs out behind it in a ridge as wide
   as the truck. One load covers both lanes of the test road. Then it goes back to the yard,
   fills, and waits until there is more to do. No click does anything to it.
2. **Players level it with the shovel** (key 5): two clicks spread a square. A square can be
   spread only if there is asphalt somewhere in its row across the road, so the work goes outward
   from the ridge.
3. **Or a roller levels it.** Any grid square under the roller that has asphalt anywhere in it is
   pressed flat across the whole square and rolled. On the Paving ground the roller is sent with a
   right click and drives a lane at a time; on the Driving ground a player drives it. Rolled
   asphalt is blacker and lies lower.
4. **Lines are painted** three ways. The paint tool (key 6) paints a square's share of the lines
   with one click: a white line inside each edge and a broken yellow one down the middle. **The
   user asked for "an additional painting tool players can use by hand"**: the brush (key 8)
   paints wherever it points, white on left click and yellow on right, with no squares and no
   cap. **And for "a drivable painting truck"**: see "Driving".

What paving is for today: trucks drive half as fast again on rolled asphalt (`pavedSpeed`).
**"Paving will do more"** (user, 2026-10-05): a point for the next prototype.

### The shovel in the hands

**"Let's go ahead and introduce shovels into the players hands with procedural animations. No
physical hand or model necessary, just a floating shovel model. Implement it for gravel, quarry
truck loading, leveling"** (user, 2026-10-05). **All animation is procedural** (user).

- A shovel floats at the lower right of the view while grading, gravel or asphalt is in hand. It
  drops away with any other tool and while driving.
- It moves on springs with each thing done: grading stabs down and levers back; laying gravel or
  asphalt pushes forward and flicks; packing comes straight down; loading it dips and comes up
  with a load on the blade; flinging whips up and forward.
- Other players see the same on the blob's own shovel.

## Trucks

### The ones that drive themselves

- **"The trucks are perfect. I like that they run on the incomplete road"** (user, 2026-10-05).
  **They drive in both lanes, in opposite directions** (user, 2026-10-04).
- A truck is the real size, 3,600 kg, on four sprung rays, steering itself along its lane at
  6 m/s. Its wheels bite according to the surface: full push on packed gravel, 0.6 of it on loose
  gravel, 0.4 on bare ground.
- It drives from one end of the road to another. On a test ground with more than two road ends it
  picks its two at random. On a map there is no road until ropes join the two towns' stakes.
- On its side, or getting no further along its way for 4 seconds, it is thrown up and blows up.
  [The "no further" half is Claude's, added because trucks on an unfinished hill slid back and
  crept up for ever. It has no switch.]
- **At a junction it keeps to its right-hand lane** (user, 2026-10-05: "turning too tightly.
  Let's keep them in their right hand lanes") up to the level ground round the stake, and turns
  there on a curve into its new lane. Nothing gives way to anything.
- It does not say why it failed. The user has said that is fine for now.

### What a truck can climb

Measured 2026-10-05 on straight ramps 16 m high, eleven or twelve trucks to each:

| Surface | Every truck arrived | Every truck wrecked |
|---|---|---|
| Packed gravel | every slope tried, up to 25 degrees | none |
| Loose gravel | up to 16 degrees | 18 degrees and steeper |
| Bare ground on its line | up to 10 degrees | 12 degrees and steeper |

Downhill every truck arrived on everything. A truck also gets up 20 m of any slope on the speed
it arrives with. **So a truck needs nothing but graded ground on the flat**, which the user's
Short playtest showed too: 21 trucks arrived and none wrecked before any gravel was laid. What
gravel is for on the flat is open.

### Wear

**"Trucks damage the road, on one example station only for now, as an experiment"** (user,
2026-10-05), and **wear is off in the prototype** (user, 2026-10-05). On the wear road only: four
times a second a truck takes a random amount, up to 30 of 100, off the health of the square
under it; below 50 each wheel cuts in, losing the packing, then the gravel, then rutting the
ground. Working a point makes it sound again.

### The ones that are sent

**"Trucks need to be sent to destinations, so we need a system for that. Allow players to create
truck depot locations, and then when sending the truck, they pick the specified destination. For
the purposes of the Quarry Station testing scene, we will not implement the player controls yet.
Simply create a static drop off"** (user, 2026-10-05).

- A depot is a stake a plot names. A truck stands at one until a player right clicks it, with any
  tool in hand, and then drives to the other by any road, service roads included.
- The depots are fixed, two to a plot, so there is one place to send a truck. Players making
  depots and choosing among them is for the next prototype.
- There is nowhere to turn at the end of a road, so a truck that is sent is put at the start of
  its way, facing along it.
- The quarry's gravel truck and the Paving ground's roller are sent. The dump truck sends itself.

### The quarry

**"The quarry station will be where players load gravel into a truck. Trucks will then drive on
a service road to a location where it will wait for players to unload it"**, and then: **"a bit
bigger, and actually a dug out section of the world. I'd like trucks to need to take a winding
road in and out of the quarry"** (user, 2026-10-05).

- The pit is a cone dug 8 m into raised ground, 60 m across at the top and 28 m at its floor,
  with the rock in the middle of the floor. A finished service road leaves a junction with a
  road for everyone, runs along the rim and winds three quarters of the way round down to the
  floor, as a bench cut into the side.
- **"Click once to load your shovel, and then click again to fling the gravel to the truck"**
  (user). With the gravel tool: a click on the rock, within reach, loads the shovel; a click on
  the truck, from up to 16 m (`flingReach`), flings it in. The truck holds 12 (`haulLoad`).
- **"First click on the truck will initiate a pile placement mode. Player places the pile, then
  uses 1 click to unload from truck, 1 click to fling to the pile"** (user). At the drop a ring
  follows the crosshair, a left click puts the heap there and a right click cancels. The heap can
  be moved only when it is empty.
- **"Don't worry about any sort of animations or anything that visually communicates that the
  gravel is being flung through the air"** (user).
- A shovel on the heap is worth 8 clicks of laying (`shovelWorth`). On this ground gravel laid on
  a road comes off the heap and none can be laid when it is empty. `gravelFromStock` at 0 makes
  it free again.
- The work it is for is a 40 m road for everyone, on its line and bare, beside the drop.

## Driving

**"Let's test out player driving mechanics. Make a scene with a drivable pick-up truck, a steam
roller, and a front loader"**, and **"include a drivable painting truck"** (user, 2026-10-05). A
first proposal, on the Driving test ground only.

- E beside a vehicle gets in; E gets out, on its left. One player to a vehicle.
- W and S drive forward and back, A and D steer, and it steers only while it rolls. Letting go
  brakes it. The view is first person from the driver's seat and turns with the vehicle.
- **The pick-up** is fast (14 m/s) and does nothing else.
- **The roller** is slow (3 m/s). It packs loose gravel and levels and rolls asphalt under it.
- **The front loader**: R raises the bucket and F lowers it. Driven into the gravel pile with the
  bucket down it fills; a left click tips it. Tipped on road that is on its line the gravel is
  laid there; anywhere else it is left as a heap.
- **The paint truck**: a left click turns its sprayers on and off. With them on it paints the
  lines of the rolled lane it drives.
- The tools are put away while driving. A vehicle on its side for two seconds is set back on its
  wheels. A driver's own machine works out the vehicle's motion, so the wheel answers at once.

## Where it is played

The host picks, from two rows of buttons at the top of the screen. Everyone starts again there.

### Test grounds

**"Let's cut our current Stations map into several maps with independent focuses. Don't split per
station, group things together in a sensible way"** (user, 2026-10-05). Each is flat yard with a
few plots on it.

| Test ground | What is on it | What it is for |
|---|---|---|
| Yard | A parked truck, a road strip, three painted hairpins | Sizes against the blob |
| Building | Rough ground to click to a line: level, a climb, a fall. A bare hillside to stake. A level section to gravel. The tightest turn staked out on rough ground | The stake, grade and gravel tools |
| Trucks | A finished road and a bad one. The wear road. The tightest turn, finished. Four ramps: 14 degrees bare and gravelled, 20 degrees gravelled and packed | What a truck can and cannot drive |
| Junctions | A T, a crossroads and a fork at the 60 degree limit, finished, with trucks. A rough field with a road to rope a branch to | Junctions |
| Quarry | The pit, the winding service road, the drop, the road that needs gravel | Loading and hauling gravel, sending trucks, service roads |
| Paving | A packed gravel road, the dump truck and the roller; beside it the same road finished, with trucks | Paving and painting |
| Driving | The four vehicles; a road whose sections are bare, loose gravel, spread asphalt and rolled asphalt; a gravel pile; a rough field | How driving feels, and what each vehicle does |

### Maps

**"Several maps, to try different distances"; "150 m between the two ends is fine as the starting
distance, with the distance as a slider"; "the host picks the map"; towns are "a few blocks and a
pad", with little time spent on them** (user, 2026-10-05). **The three tools work anywhere on a
map, and trucks set off as soon as a chain of stakes joins the two towns, in both lanes, and keep
coming on whatever is there** (user).

| Map | Between the towns | The land | What it is for |
|---|---|---|---|
| Short | 60 m | Nearly flat | One player finishing a whole road in a sitting |
| Middle | 150 m, on the `mapDistance` slider (40 to 400) | A 9 m hill on the straight line, a hollow to its right, flat to its left | The starting distance. Round the hill is the easy way |
| Long | 300 m | Rolling, with a 6 m ridge across it that has one gap | Whether a long road is something a crew wants to build |
| Climb | 160 m, town B 16 m higher | Gentle on the left, about 31 degrees on the straight line, a cliff on the right | Land that makes the road wind |
| Switchback | 150 m, town B 12 m higher | Two 6 m banks across the map, each lined with rocks but for one ramp: on the left, then on the right | **The user's**: "static, non-destructible pieces ... to encourage/funnel players into needing to use turns to navigate up a hill" |

- A town is a thick stake that cannot be moved, on level ground, with a painted pad, five blocks
  and a 26 m pole in its color behind it. A is red and B is blue. Everyone starts at A.
- To build: choose town A's stake, walk toward B's pole putting stakes down, and rope the last
  one to B's stake. The readout says how much road is staked and whether the towns are joined.
- Once they are joined a truck leaves each town every 12 seconds (`truckEvery`), up to six on the
  way in each lane.
- Each map is the same land every time. "Make new land for this map" on the F1 panel makes
  another. The land's humps and hollows are 0.35 m (`landRoughness`).
- A new game starts on the Middle map.
- The rocks on Switchback are boulders 5 to 7 m across that nothing moves or grades. A route
  through it, staked by script within every rule, was 187 m in 15 ropes.
- Since the slope limit, a road staked straight over the Middle map's hill may be refused: a
  script that tried stopped after three ropes. Not confirmed by hand.

## Systems Claude added to make a long road buildable

The user gave latitude for these on 2026-10-05 and asked for each to be recorded, explained and
switchable. Both work on maps only and are sliders under "Claude's additions". The user played
the Short map with both on and did not comment on either by name.

| System | Slider | Why |
|---|---|---|
| Trucks pack the gravel they drive over: each wheel packs its square by a quarter, four times a second | `truckPacking`, 0.25; 0 is off | Packing was four of the seven clicks a square of gravel takes. In the user's Short playtest packing stayed within a few points of laying the whole way |
| A click on the hot spot also does one ordinary click on every other square across the road there | `hotSpotRow`, 1; 0 is off | The user's "give value to individual clicks", turned up |

A script clicking at the cap, half its clicks on the hot spot, built 153 m straight over the
Middle map's hill (before the slope limit), shoulders included:

| | Grade | Gravel | Total |
|---|---|---|---|
| Both off | 365 s | 408 s, laid and packed | 13 minutes |
| Both on | 172 s | 122 s, laid; the trucks packed all of it | 5 minutes |

The Long map's 306 m took 11 minutes with both on. These are clicks only; a person also walks.

## Dev tools

**"Give me a dev tool to instantly complete work. So I can go from unleveled to leveled in one
click, and from leveled to compacted gravel in 1 click"**, and **"some flying/noclip mode"**
(user, 2026-10-05). Neither is part of the game.

- Key 7: the section under the crosshair, up to 60 m away, is outlined in magenta and a left
  click does its next stage whole: onto its line; gravelled and packed; paved and rolled;
  painted. [The last two are Claude's addition.]
- V: flying. Nothing is solid. Space is up and Ctrl is down.

## Open

Answered in the design session of 2026-10-05, and written up under "What the design session
decided": what the hazards of dirt and gravel are, what paving is for, how one vehicle is both
automated and driven (it is not: they are different vehicles), how a hand-drawn line is judged,
whether trucks packing gravel is wanted, and where the first gravel comes from.

To be answered by playing the next build:

- **How low the gravity should be**, for trucks and for the blob.
- **Whether dirt and gravel fall apart at the pace the user described**, and whether damage by
  landing is better than damage by time.
- **Whether loose gravel is dangerous for long enough**, with trucks packing it.
- **Whether a gravel truck that runs its own round is a job worth having**, what should happen
  to travellers behind it, and whether losing it on a bad road is the right punishment.
- **Whether the place for a painted line should be marked**, and tar spray against the grinder.
- **Whether a long road is worth having**, from the finished Long and Climb maps.

Still for the user to answer:

- **Which test grounds are finished with.** Asked in the design session and not answered. Until
  it is, none is removed.
- **What a traveller pays, and what the quarry, asphalt and paint cost.** For the final
  prototype.
- **How big the map is, how many cities, and how much traffic is "low volume".**
- **How a ruined stretch is repaired.** Claude's default is the same clicks as building.
- Whether Claude's hot-spot row is wanted, and how strong.
- Whether Claude's stuck rule should stay.
- Curved sections in place of mitred corners.
- Whether the view when driving should be from the seat.
- Where the heap may go.
- A rope across a rise runs under the ground, where it cannot be seen.
- What a second player does that the first does not. Loading at the quarry while another lays
  gravel is the first candidate, and came out of the delivery rules, not from a decision.
- Everyone starts at town A, so working from both ends means a walk.
- A player whose game is killed, not closed, makes the host stop hearing everyone. See `NOTES.md`.
