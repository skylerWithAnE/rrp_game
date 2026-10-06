# Game design

Rewritten 2026-10-05 as the game stands, and amended the same day by a design session: "What
the design session decided", "What has been played" and "Open" are new, and the version before
them is commit `2f3f9b9`. Amended again on 2026-10-06 by the overnight build of the six slices:
"What the build of 2026-10-06 made of it" is new, and the controls, the grounds and "Open" follow
it; the version before is commit `71fcc8b`. The page the rewrite replaced had grown by addition through two
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
2026-10-05). That is the prototype as built. Where the user means to take it is next. Since the
build of 2026-10-06 it is set in low gravity, dirt and gravel wear out, and loose gravel spins
trucks round: see "What the build of 2026-10-06 made of it".

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
marked [Claude] is a proposal or a default the user has not confirmed. It was built on
2026-10-06, as the next section says; this section is left as it was decided.

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

## What the build of 2026-10-06 made of it

Built unattended, overnight, as six slices. **None of it has been played by the user yet, bar a
few minutes of gravity on the Trucks ground during the build.** Every number here is Claude's and
is a slider; every choice of Claude's is listed in `SESSION_2026-10-06.md` for a yes or no.

### What the user asked for after first playing them

2026-10-06, after a first go at the slices. The words in quotation marks are the user's. All of
it is built and checked on two instances; none of it has been played.

- **"Let's move forward with moon 0.16 as default gravity."** It starts at the Moon now.
- **"Let's get a third person camera for driving."** In a vehicle the camera sits behind and
  above it and turns with the mouse. C switches to the view from the seat and back
  [Claude: the key; `driveCamDistance`, 9 m, and 0 puts it in the seat for good].
- **"Pickup truck also needs to be 4 passenger vehicle with seat swapping mechanics."** E gets
  into the driver's seat if it is free, and otherwise the next free seat: beside the driver, or
  one of two in the bed. Aboard, the number keys 1 to 4 move you to that seat if it is empty.
  With nobody in the driver's seat it does not move. [Claude: the keys, and the bed as seats.]
- **"Painting truck needs to be multiple players. So one can drive, one or two could operate
  the paint nozzles."** It has two seats in the bed. Whoever is in the right-hand one has the
  white nozzle on left click; the left-hand one has the yellow. One person alone in the bed has
  both: their own on left click and the other on right click. While anyone is in the bed the
  driver has no nozzles; a driver alone has both, as before.
- **"Another spin out scene where the trucks enter the road on a more level surface, and get
  more time to speed up. Enter on less of a ramp, drive 3 finished paved road tiles, then enter
  loose gravel. Add a bend after two segments of loose gravel."** And then, after the first
  attempt was looked for in the wrong place and was the wrong length: **"a tile is a grid
  square, a segment is a full 20m segment of road"**, and **"40m of paved road into 40m of
  loose gravel, then the bend."** Road 2 on the Spin-out ground, to the right of where you
  start: 0.45 m above the yard, not 1.2; 40 m paved; 40 m of loose gravel; a 90 degree bend;
  and the same again in reverse [Claude: the far half, so that trucks from the other end meet
  the same thing]. Trucks reach the gravel at 9 m/s, not 6. By script at the Moon: 69 spins
  and 18 wrecks in the first 37 trucks. The first road is still there, as Road 1.
- **The words**: a **tile** is a grid square, about 2 m. A **segment** is 20 m of road between
  two stakes, which these pages have been calling a section.
- **"Let's move the wear scene on to a much larger height of the height map ... on top of some
  terrain that's 10m above sea level. And include a test for dirt, gravel, dirt with a paved
  road leading into it, and gravel with a paved on ramp, too."** The Wear ground is now one piece
  of high ground 10 m above the yard (`wearHeight`) with four roads on it, from the left: dirt,
  gravel, dirt with two paved sections leading into each end, gravel with the same. The stretch
  that wears is 100 m on all four. You start on top, between the dirt and the gravel.
  **"I really wanted to see the trucks digging deeper into the ground in the wear test. The
  higher elevation heightmap was to let me see if they could dig in further."** So
  `wearDeepest` is 9 m now, not 0.8, and its slider goes to 20: a hole can go nearly the
  height of the ground. Not measured at that depth.
- **"Let's add a counter for the trucks passing through roads in the wear scene."** Each road
  has signs over it, at both ends of the stretch that wears and its middle: trucks so far, how
  many got through, how many were wrecked, and how much of it is rutted and how deep.
- **"After about 4 lines, text cuts off."** A sign is now as tall as its text needs, and so are
  the line under the crosshair and the F3 readout.
- **"Spend some more time cleaning up signage and instructions in all of the demo scenes."**
  The instructions for each ground are now in a **guide box at the bottom left** (H hides it):
  what the ground is for, what is where, and which keys work it. Signs out in the world only
  name things, in a line or three, on a dark board. The nearest sign wins: one that would lie
  over another, or over the host's bar, the readout or the guide, is left out, and a sign more
  than 30 m off shows only its name. The counts over the wear roads are read in full from any
  distance. Each vehicle has a sign. Every sign's text was rewritten; "Station 2" and the like
  are gone. A road's sign stands 10 m in from its first stake, where it can be seen from the end.
- **"Glad to see the speed multiplier in the panel."**
- **"No review on quarry/dump truck right now."**
- **"Are we implementing that in a way that affects the engine appropriately or are we taking
  some other more narrow measure?"** Both. The slider sets the physics engine's own gravity,
  so everything with a rigid body falls by it: trucks, driven vehicles, wrecks. The blob is not
  a rigid body and has its own fall, which is multiplied by the same number. On top of that are
  the two switches below, which are narrower and are Claude's: they change how hard wheels
  bite and how stiff springs are, because the trucks' wheels and springs are written by hand
  and would otherwise take no notice of gravity.

### Gravity

`planetGravity` on the F1 panel, and three buttons on the host's bar: Earth 1.00, Mars 0.38,
Moon 0.16. It started at Mars and, since the user's word on 2026-10-06, starts at the Moon. It
acts on trucks, driven vehicles and blobs, everywhere.

Two switches of Claude's sit beside it, both on:

| Switch | On (as built) | Off |
|---|---|---|
| `gripFollowsGravity` | Wheels push, brake and hold sideways in proportion to gravity, as tyres do. A truck climbs about the same slopes on any planet, picks up speed slowly, and slides wide in bends | Wheels bite as on Earth. At Mars a truck then climbs 25 degrees of bare ground and 30 most of the time |
| `springsFollowGravity` | Springs are as soft as the gravity. A vehicle rides at the same height everywhere and bounces slowly | Earth's springs. It rides 15 cm higher at Mars and 20 at the Moon. It also climbs better: loose gravel to 20 degrees at Mars |

Measured by script on the Trucks ground, both switches on, ten or more trucks to each figure,
ramps 16 m high as before. "All" is every truck over the top; otherwise how many of how many.

| Surface | Slope | Earth | Mars | Moon |
|---|---|---|---|---|
| Packed gravel | 27 | all | all | all |
| | 28 | 4 of 10 | 7 of 11 | |
| | 30 | none | 17 of 27 | 4 of 13 |
| | 35 | none | none | |
| Loose gravel | 15 | all | all | all |
| | 16 | all | all | 11 of 13 |
| | 18 | none | 4 of 19 | 4 of 17 |
| | 20 | none | none | 3 of 18 |
| Bare ground on its line | 10 | all | all | all |
| | 12 | none | all | all |
| | 14 | none | none | 8 of 13 |
| | 15 | none | none | 12 of 17 |

- Earth is as it was measured on 2026-10-05, which is the check that nothing played has changed.
- **The 15 degree slope limit still does its job**: 15 degrees of loose gravel was climbed by
  every truck at all three (80 trucks). What has changed is the other side of it. At the Moon
  most trucks also get up 15 degrees of bare ground, on speed alone, if the climb is only 16 m.
- **Why low gravity climbs more**: the wheels push less, but the hill pulls less too, and the
  speed a truck arrives with carries it much further. On bare ground too steep to climb, the
  best truck got this far up the slope before it stopped: at 20 degrees 10 m on Earth, 27 at
  Mars, 33 at the Moon; at 30 degrees 5, 11 and 13. A ramp that is meant to stop a truck has to
  be about three times as long at Mars as on Earth. On a ramp 36 m high the Moon's trucks failed
  12 degrees of bare ground, having got 170 m up it.
- **The tightest turn: every truck got round it, at every gravity** (over 80 trucks each), on
  the finished hairpin. **But they do not stay in lane.** The furthest a truck strayed from its
  lane's line was 0.8 m on Earth, 2.9 m at Mars and 7.0 m at the Moon. At Mars that is a truck
  half off the outside edge of the road, on the shoulder. At the Moon it leaves the road and the
  shoulder altogether and drives back on. Neither the turn nor the slope limit was changed.
- The blob's hop is about 0.6 m on Earth, 1.6 m at Mars and 3.9 m at the Moon.
- The stuck rule, the 9 m/s kick when a truck wrecks and the trucks' speed are as they were. A
  wrecked truck is thrown 26 m up at the Moon.

### Wear

**New > Wear**: as first built, a dirt road (bare ground on its line) and a packed gravel road,
100 m each, side by side, with a truck setting off from each end every 4 seconds
(`wearTruckEvery`). It is four roads on high ground now: see the section above. The F3 readout
counts the trucks that have set off down the road you are nearest, and says how much of it is
rutted and how deep; so do the signs over each road. The pace below was measured on the first
two roads, at Mars.

The rule, for every road that wears but the first wear road on the Trucks ground, which keeps
the rule it was played with:

- Bare ground and gravel each take damage at their own rate (`wearDirt`, `wearGravel`). **Paved
  road takes none.**
- **`wearByLanding` is the slider between damage by time and damage by landing.** At 0 a truck
  damages the square it is on, a random amount each quarter second, as before. At 1 each wheel
  damages the square it comes down on, in proportion to how fast it came down, so a hole makes
  the next one. It starts at 0.5. At 1 a road that is perfectly smooth never starts to wear.
- **Once a square is below the threshold, damage to it is multiplied** (`wearScaleUp`, 1.5), and
  the wheels cut deeper the further below it is.
- A hole goes no deeper than `wearDeepest` (0.8 m) below the line. [Claude: without a limit the
  first settings dug the road down to the yard, 1.2 m, inside 100 trucks. The bottom is uneven
  in patches a metre across, or a road worn right out is a smooth trench that trucks like.]
- Grading and gravel mend it, with the same clicks as building.
- **Wear on a map is `mapWear`**, on. [Claude: `quarryWear` does the same for the Quarry ground
  and is off, so that slice asks one question.]

**The pace it came out at**, at Mars, counted in trucks that had set off (both lanes together):

| Trucks | Dirt | Gravel |
|---|---|---|
| 5 | 1 % of the road cut, the deepest 8 cm | nothing |
| 10 | 6 % cut, 22 cm | nothing |
| 25 | 48 % cut, a quarter of it deeper than 15 cm | 0.3 % worn |
| 50 | 74 % cut, half of it deeper than 15 cm, 80 cm at the deepest: a rut | 4 % of squares worn through and 2 % loose; no holes yet |
| 100 | 96 % cut. 8 trucks wrecked | 28 % worn, 15 % loose, the first holes, 70 cm deep |
| 250 | all of it | 55 % cut, 45 % deeper than 15 cm, 80 cm at the deepest. 16 wrecked so far |
| 350 | | 84 % cut. 93 wrecked so far |
| 500 | 22 wrecked in all | 95 % cut. 206 wrecked in all: three trucks in four since the 350th |

The rows to 250 were measured again after loose gravel's spin-out went in and came out within a
few trucks of these, with more wrecks on the dirt (39 by the 250th); the 350 and 500 rows were
not. At Earth and at the Moon the pace is much the same. With the slider at "all by time" gravel
goes faster (80 % of squares worn by the 100th truck); at "all by landing" both roads go slower
and from their ends inward (dirt 59 % cut by the 100th).

Against what the user asked for: dirt has a square's worth destroyed by the fifth truck and is a
rut by the fiftieth. Gravel at 50 has damage to several squares but it is loose patches, not
holes; it is full of holes with several very deep at 250; and it is unusable by 500. **Two things
are off.** Gravel's damage at 50 is modest. And a dirt road worn right out is still driveable:
only 22 of 500 trucks wrecked on it, because a rut is a road, a little lower.

### Loose gravel

**New > Spin-out**: a road with a 90 degree bend, under loose gravel but for a packed section at
each end, with the same steady traffic. The host's bar has "Loosen the gravel again".

- **The truck did have sideways grip, but it was the same on every surface** and never ran out.
  Now a wheel on loose gravel holds sideways a tenth as well as on packed gravel on Earth
  (`looseGrip`), whatever the gravity.
- Loose gravel also throws the truck's tail about, more the faster it goes and the harder it
  turns, and the wheel answers less (`looseFishtail`). **A truck more than 22 degrees sideways
  to its own motion has spun out** (`spinAngle`): it goes round with its wheels locked and its
  driver has it back 2.5 seconds later (`spinSeconds`), wherever it has ended up. The stuck
  rule, other trucks and the edge of the road do the rest. The readout counts spins.
- This is everywhere there is loose gravel, so it also changes the two gravel ramps on the
  Trucks ground and any gravel laid on a map. `looseGrip` at 1 and `looseFishtail` at 0 put
  it back.
- **Trucks still pack what they drive over, at `truckPacking` 0.02, down from 0.25.** At 0.25 a
  stretch was packed by its second or third truck. That slider is the same one the maps use, so
  on a map trucks now take about 50 passes to pack a road, not four.

Measured, 60 or more trucks at each:

| | Spins | Wrecks | Safe again after |
|---|---|---|---|
| Earth | 27 | 2 | about 30 trucks |
| Mars | 50 | 12 | about 50 |
| Moon | 93 | 24 | still spinning at 65, with the gravel 92 % packed |
| Mars, packing off | 219 in 72 trucks | 34 | never |

### Gravel delivery

On the **Quarry** ground.

- **The drop-off is put down with the survey tool, key 9.** Left click a stake that is in the
  middle of a road, then left click the ground beside the road. The drop-off is a stake with a
  board on it, on a short spur roped to the stake you chose. The spur is a service road [Claude:
  so that travellers do not drive into it; the zoning tool can open it]. There is one drop-off;
  putting another down moves it. It cannot be put down while the truck is out.
- **Why a stake in the middle of a road** [Claude]: the truck drives on past that stake and backs
  into the spur, so there has to be road beyond it. As the ground starts, the only such stake
  on the road for everyone is the junction itself, and the one free side of that is the slope.
  So the first job is to stake some road: carry the bare road on from its end, and branch the
  drop-off from the stake that used to be the end.
- **The truck runs its own round.** It is loaded at the quarry as before. A moment after it is
  full it sets off; a right click sends it sooner with whatever is aboard. It drives to the
  junction by any road, 12 m past it (`haulPass`), stops, and backs into the spur at 2 m/s
  (`haulBackSpeed`). It stands there while it is unloaded as before: place the heap, one click
  off the truck, one fling onto the heap. A moment after it is empty it drives home; a right
  click sends it sooner.
- **Wrecked on the way, it is gone with its load**, and an empty one stands at the quarry 20
  seconds later (`haulRespawn`).
- **One shovel is worth 30 clicks of laying, which is ten squares** (`shovelWorth`, was 8).
- Travellers drive the road for everyone as they did, between its ends, and so share it with the
  truck once the drop-off is on a road they use. The stuck rule is untouched.
- Still a cheat: at the quarry the truck is turned round on the spot before it sets off, because
  there is nowhere in the pit to turn. At the drop-off it really does back in.

### Painting by hand

**New > Painting**: two strips of rolled asphalt, 60 m each. The near one has the place for each
line marked with a faint dotted line; the far one has nothing. A paint truck stands on the near
one. The square paint tool (6) and the Paving ground are as they were.

- **Paint is kept in cells of 0.1 m**, a layer of its own over the asphalt, white, yellow or tar.
- **The roller brush, key 8**: hold a button and drag, and it leaves a continuous stripe
  `rollerWidth` wide (0.15 m), however fast it is dragged. Left is white, right is yellow.
- **The paint truck sprays where it actually is.** It has a nozzle out on each side: white on
  the right, over the edge line when the truck is in the middle of its lane, and yellow on the
  left, over the centre line. Left click turns the white one on and off and right click the
  yellow, so a broken centre line is made by clicking as you drive. [Claude: the two buttons,
  and where the nozzles are.]
- **The tar spray, key T**: blobs scattered over 0.9 m (`tarWidth`) round where it points. Tar
  covers paint, shows as a black patch, and can be painted over.
- **The grinder, key G**: a line 0.1 m wide (`grinderWidth`) that takes paint and tar off.
- **The judging**, which the player does not see. A white line belongs 0.375 m inside each edge
  and a yellow one down the middle, broken 3 m on and 3 m off (`centreBroken`; 0 is solid).
  Round each is a band `paintBand` wide (0.4 m). A grid square earns for every 0.1 m of its
  length that has paint of the right color somewhere across the band, and loses for paint
  anywhere else, or of the wrong color, counted so that one stray stripe the length of the
  square costs what its own line earns. A square scores from -100 to 100, and so does the strip.
  [Claude: judging by length covered and not by area. Filling a 0.4 m band with a 0.15 m roller
  is not what a line is.]
- **The number is on the F3 readout and nowhere else**: the strip's, and the square's under the
  paint tool or under your feet.
- By script: a paint truck driven dead straight up its lane with both nozzles on scored 100 on
  the squares it passed where the centre line is wanted. A stray yellow stripe in the middle of
  a lane took its square to -70, and tar over it brought it back to 0.

### The Long and Climb maps, finished at a button

**On any map the host's bar has "Stake and finish this road".** It stakes a road from town to
town where a crew would take it, about 15 m to a rope and within the rules for ropes, puts every
staked point on its line, and gravels and packs the road. Trucks set off at once. Beside it are
the switch for wear on maps and buttons for speed (x1, x4, x10) [Claude].

| Map | The road | By script, at Mars |
|---|---|---|
| Short | 60 m, straight | no wrecks in 50 trucks, wear off |
| Middle | 172 m, round the left of the hill; bends to 33 degrees | no wrecks in 50, wear off |
| Long | 307 m, through the gap in the ridge; nearly level | wear on: no wrecks in the first 150 trucks; 58 by the 250th; 155 by the 350th |
| Climb | 197 m, out to the gentle left side and back; 10 degrees at the steepest | wear off: no wrecks in 52. Wear on (an earlier route): holes of 30 cm by the 100th truck |
| Switchback | 181 m, up both ramps; a bend of 36 degrees, which is the limit | no wrecks in 50, wear off |

- With wear on, a finished gravel road on a map lasts about 150 trucks. At a truck from each
  town every 12 seconds that is fifteen minutes.
- [Claude] Players on a map now start 3.5 m further to the left of town A's stake. Where they
  started was in the lane that trucks from town B leave the road by, and a blob standing there
  stopped every one of them: they blew up at its feet. Trucks do not pass through a blob.
- A road that comes into a town at an angle sends its trucks into the houses. The button's roads
  come in straight; a player's may not.

## What has been played

As the user told it in the design session of 2026-10-05: "I have played with each mechanic in
their isolated settings". The table this replaces was Claude's guess and understated it. Played
is not the same as commented on: where there are no words, none were given.

| | Played by the user |
|---|---|
| Stakes and rope, grading, gravel, the hot spot, mitred corners, self-driving trucks, wear | yes: accepted, "they look good" |
| The Short map | yes: "The short playtest went great. The game is looking much more like what I envisioned." |
| The Middle map | yes: "I have connected two towns in middle" |
| The Long and Climb maps | no: "kinda too labor intensive for me, so, I would like to see demos of those completed". A button finishes them now |
| The Switchback map | not said |
| Building junctions; trucks at junctions | yes: "looks good, and intuitive"; after the change, "I did observe lane-keeping being much more tight" |
| Zoning | not by hand: "I haven't thought about zoning since establishing it" |
| The quarry | yes: "loading, flinging, sending, I have played those mechanics" |
| Paving | yes: "I have tested an asphalt dump truck that placed asphalt on the ground that I leveled with the 'asphalt' tool" |
| Driving, the roller, the brush, the paint truck, the shovel in the hands | yes, in their isolated settings: "I'm happy with those mechanics and now I want to add more complications to them" |
| The rope's slope limit; the dev tool; flying | not said |
| The six slices of 2026-10-06 | not yet. The user was in the editor for about five minutes at 01:05 on 2026-10-06, during the build: the Trucks ground and then the Wear ground, with gravity at the Moon, flying. A screenshot of the two wear roads from above is in `docs/answers/image.png`. No words came with it |

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
| 8 | Paint brush: left click white, right click yellow, anywhere on rolled asphalt. On the Painting ground it is the roller brush: hold and drag for a stripe |
| 9 | Drop-off survey, at the quarry: left click a stake in the middle of a road, then the ground beside it |
| T, G | On the Painting ground: tar spray, and the grinder. Hold left click |
| Right click on a waiting truck or roller | Send it to its other depot, with any tool in hand |
| E | Get into or out of a vehicle: the driver's seat if it is free, else the next. W S drive, A D steer. In a pick-up or a paint truck, 1 to 4 change seat. In the Painting ground's paint truck the nozzles are on the mouse buttons, for whoever is in the bed, or for a driver alone |
| C | In a vehicle: the camera behind it, or the view from the seat |
| 7, V | Dev: finish a section's next stage in one click; fly |
| F1, F3, Tab | Sliders (host), readout, free the mouse |
| H | The guide: what this ground is for and how to work it |

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

The host picks, from three rows of buttons at the top of the screen: Tests, New (the grounds
built on 2026-10-06) and Maps. Under them are three buttons for gravity. Everyone starts again
on the ground that is picked.

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
| Wear (New) | High ground, 10 m up, with four roads: dirt, gravel, and each again with a paved way in. A truck from each end every 4 seconds, and a count over each road | Whether a road falls apart at the right pace |
| Spin-out (New) | Two roads with a 90 degree bend under loose gravel and the same traffic: one packed at each end, and one on low ground with three paved sections to get up to speed on | Whether loose gravel is dangerous for long enough |
| Painting (New) | Two strips of rolled asphalt, one with the lines' places marked and one without; a paint truck | Whether drawing a line by hand is hard in a good way |

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

To be answered by playing the build of 2026-10-06, a question to a slice:

- **How low is fun?** Gravity, for trucks and for the blob.
- **Does a road fall apart at that pace, and is it good to watch?** And is damage by landing
  better than damage by time.
- **Is loose gravel dangerous for long enough to matter?**
- **Is bringing gravel over a road you built a job worth doing?** And what should happen to
  travellers behind the truck, and is losing it on a bad road the right punishment.
- **Is drawing a line by hand hard in a good way?** Marked or unmarked, tar or the grinder.
- **Is a long road worth having?**

What the build found, for the user to decide:

- **Trucks get round the tightest turn in low gravity but not in their lane**: 2.9 m out at
  Mars, 7 m at the Moon. Is that the fun, or should the turn, the speed or the grip change?
- **The slope limit of 15 degrees holds, but at the Moon most trucks climb 15 degrees of bare
  ground too.** A bare road was meant to fail from 12.
- **A dirt road worn right out is still driveable.** Should ruts wreck trucks?
- **Gravel's damage at 50 trucks is modest**: loose patches, no holes.
- **Trucks now take about 50 passes to pack gravel**, not four, on maps as well.
- Every choice of Claude's in `SESSION_2026-10-06.md`.

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
