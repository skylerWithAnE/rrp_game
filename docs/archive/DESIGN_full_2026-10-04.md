# Game design

Last updated: 2026-10-04 (planning conversation, before Unity was installed). This file describes
the **current** design. How it got here is in the decision log (section 11) and history (section 12).

Labels: **[User]** = said or decided by the user. **[Claude]** = proposal or interpretation the user
has not confirmed.

## 1. The game in one paragraph

An online co-op game for 3 to 4 players (up to 8) where a crew of solid-color blobs builds a **road**
across a deformable desert using **shovels**. Players choose their own route between fixed
landmarks, cut and fill the terrain, and upgrade the road through tiers from dirt track to painted
asphalt. Digging produces physical cubes that the crew has to deal with. The game is a series of
replayable scenarios chased for high scores. It should be **weird**.

## 2. Decisions [User]

- **Multiplayer first.** Online, peer-to-peer, one player hosts. No dedicated server, "especially
  during this prototype phase."
- **Player count:** "design with 3-4 in mind, support for 8."
- **It is a road, not a railroad.** "I like the road idea more, then we can really focus on the shovels."
- **The shovel is the focus.** Players start with shovels. The user likes all five verbs: scoop,
  fling, smack, scrape, pry.
- **Free routing.** "Players are free to route." Landmarks are fixed; the path is the players' choice.
- **Not flat.** "I really don't want an easy flat world. I want it to force players to build uphill
  in some scenarios."
- **Jank means weird.**
- **Digging makes physics cubes.** Digging up ground should "create physics object cubes that
  players have to deal with."
- **Road variants A, B, C and D are all liked** ("a, b, c, d, I like those"): realistic road crew,
  blobs as the machinery, road tiers, chip seal. Section 5 combines them.
- **Variant E liked too** (trees: sap is tar, fruit are paint cans/cones) "but maybe we can dig more
  stuff up?" Ideas in section 6.
- **Scenarios and high scores.** The user sees the game as "a series of stages where the players
  have a job," as "unique individual replayable scenarios with a goal of chasing high scores."
  Examples given:
  1. connecting two towns
  2. building a route through a busy section of a town
  3. connecting a town to a bus stop
  4. connecting two towns to a bus stop
  5. then further adding on a train station
- **First target: the Hill.** "Players are connecting two towns, there's a large hill in the
  middle." Full description in section 7. This replaces the earlier "2 km slice with a start, middle
  and end."
- **Time beats distance.** "Let's take '20-30 minutes' more into consideration than map size or
  distance." The 2 km figure is no longer a requirement; the map is sized to fit the session.
- **The only tool is the shovel.** Carts are not ruled out entirely, but must be shovels "in a fun
  way": "shovels with wheels. Wheeled shovel."
- **Players do not drive the trucks.**
- **The goal is painted asphalt.**
- **Process [User, 2026-10-04]:** make a few more decisions, trim all the documents, then start a
  new conversation to do the work. The current conversation is "focused on shooting down
  overcomplicated ideas, simplifying things, and scope creep." A list of 18 scope-cutting questions
  was put to the user; answers pending. Much of this document is expected to be cut.
- **It is a crew game.** The user's picture: "one player digging, another catching dug up tiles and
  taking them away to compress them somewhere else in the world while others are digging up gravel
  for the next phase, repeat phases for oil and so on."
- **Deterministic physics is dropped** ("I will forget about the deterministic physics stuff for
  now"), "but let's keep the dumb physics objects from players digging."
- **Connecting:** join code now, Steam later. (Host shares a short code; keep the connection layer
  swappable.)
- **Camera:** "let's do all three" — third person, first person and top-down/isometric. [Claude's
  reading: all three available, switchable by the player.]
- **Session length:** a scenario should take 3 to 4 players **20 to 30 minutes**.
- **Loose cubes and earthquakes:** cubes stay in the world; recycling is tied to an **earthquake**
  ("we're leaning more into the earthquake and recycling mechanic now"). "If we do keep earthquakes,
  it should be sort of punishing." **This is an experiment**: "we need to experiment in game to
  decide good caps though, this might not really be a feasible mechanic." See section 4.
- **Tunnelling collapses.** "If they try to tunnel, we want it to cause a collapse."
- **Art:** prototype only. Solid-color shaders. Solid-color blobby characters. Procedural animation
  for everything.
- **Resources are placed in the world to collect for now.** "We'll figure out more weird gameplay to
  make it work later."

## 3. Terrain direction [User leaning, not final]

The user's reasoning, in order:

1. Got excited about going **full voxel**, with dug tiles becoming physics cubes.
2. Worried about networking those cubes; asked whether **deterministic physics** is a good solution
   if the world is entirely voxel based.
3. Realised an all-voxel world would struggle to make "a good looking believable uphill road."
   "I've already talked myself back into heightfields."
4. Concern: heightfields give up **tunnels**. "That might be OK in prototype phase but if we keep
   going forward, that might be an issue."
5. Idea, unsure: "**voxel buildings atop a heightmap world**, then if we ever do a tunnel we can use
   the voxel system. Not sure about that."

Claude's answers are in `TECH_PLAN.md` section 2. Short version: heightfield for the ground, physics
cubes for what you dig out of it, host-simulated (not deterministic) physics, and the terrain kept
behind an interface so voxel volumes can be added later for tunnels. **Status: heightfield is the
working assumption for the prototype; tunnels deferred; the hybrid is an unconfirmed idea.**

## 4. Shovel verbs [User approved the list; details are Claude's]

| Verb | What it does |
|---|---|
| **Scoop** | Take a bite out of whatever is under the blade. A bite of ground becomes a cube on the shovel. |
| **Fling** | Throw the load. Distance and spread depend on the swing. A teammate can catch it on their shovel. |
| **Smack** | Hit with the flat of the blade. Compacts ground, merges a loose cube into the ground, breaks rock into gravel, embeds chips in tar. |
| **Scrape** | Drag the blade along the surface. Shaves bumps, spreads loose material, smooths hot asphalt, draws paint. |
| **Pry** | Lever something out of the ground: trees, boulders, buried objects. |

Materials answer the same verbs differently: sand crumbles and pours, clay is sticky, rock is heavy
and must be smacked apart, gravel scatters, tar strings and drips, hot asphalt is sticky and sets as
it cools, paint leaves a trail.

**Cubes** [Claude]: a scooped bite pops out as a physical cube of that material. It can be carried,
flung, stacked, kicked around, or smacked flat to merge back into the ground where it sits. Material
is conserved, so a cut through a hill produces a pile of cubes that has to go somewhere, ideally
into the next low spot (real road builders call this balancing cut and fill). Cubes left lying
around are mess.

**Earthquakes** [User's idea, Claude's elaboration]: instead of cubes quietly disappearing when
there are too many, the ground rumbles as loose cubes pile up, then an earthquake shakes every
resting cube back into the ground where it sits. Messy crews get lumpy ground, including lumps on
their own road if they left cubes there. It makes the technical limit a piece of weird gameplay,
and it could also strike on a timer or as a scenario hazard.

[User] The earthquake "should be sort of punishing." [Claude] Ways to make it hurt: lumps wherever
cubes were left (worst on the road); steep cut walls collapse (see below); finished road near the
mess cracks and drops a tier; players get knocked over and drop their loads. A visible rumble meter
that climbs with the number of loose cubes gives the crew a chance to clean up first.

**Collapse** [User requirement, Claude's mechanism]: digging into the hill like a tunnel must cave
in. On a heightfield, burrowing sideways shows up as a deep cut with near-vertical walls, so the
rule is: walls that are too steep and too tall give way, sliding material and cubes down into the
cut (and onto whoever is in it). A safe cut needs its sides laid back to a gentle angle, which means
moving far more ground. Earthquakes trigger collapses on any risky wall.

## 5. How the crew builds a road [Claude's plan, combining variants A to D]

- **A (realistic order)** sets the sequence.
- **C (tiers)** turns the sequence into upgrade passes, each leaving a usable road.
- **D (chip seal)** is the cheap middle tier between gravel and full asphalt.
- **B (blobs are the machinery)** means every step is done with shovels and bodies, no machines.

### The tiers

| Tier | Road | Steps and verbs | What it unlocks |
|---|---|---|---|
| 0 | **Route** | **Pry** out trees and boulders in the way. Pick the line (fling paint blobs or plant stakes as markers). | A plan everyone can see. |
| 1 | **Dirt track** | **Scoop** the high ground into cubes. **Fling** or carry cubes into the low ground. **Smack** cubes to merge them into the fill. **Scrape** to shave it smooth. **Smack** along it to compact. | Walking is faster; a handcart can roll. Hauling gets easier. |
| 2 | **Gravel road** | **Pry** or **scoop** rock. **Smack** rock cubes until they shatter into gravel. **Scoop** and **fling** gravel onto the track. **Scrape** it into an even bed with a slight crown. **Smack** to compact. | A truck can drive it, slowly. |
| 3 | **Chip seal** | **Scoop** tar. **Fling** or drizzle it over the gravel (this is the prime coat). While it is still wet, **fling** fine chips over it. **Smack** to press them in. | Faster driving, no washboard. The cheap way to "finish" a long stretch. |
| 4 | **Asphalt** | Mix tar and gravel into hot asphalt at a mixing pit. **Scoop** it hot and relay it by **fling**-and-catch before it cools. Dump, **scrape** level while soft, **smack** flat before it sets. | Smooth, full-speed road. |
| 5 | **Painted** | **Scoop** paint. Walk the line letting it drip, or **scrape** to draw. Centre line and edges. | Finished road; score bonus. Wobbly lines are permanent. |

A scenario says what tier each connection needs. Example for the first slice: start → middle needs
gravel; middle → end needs asphalt and paint.

### How the crew moves

**Push the low tier through first, upgrade behind.** A dirt track all the way to the next landmark
comes first, because it makes every later haul faster. Then the crew comes back along it laying
gravel, and so on. Each pass is a trip along the road the last pass built.

**Like a real paving train, strung out along the road:**

- **Pioneers** at the front: pry obstacles, scoop the cut, choose the line.
- **Haulers** behind them: move cubes from cuts to fills, bring rock, tar and paint from wherever
  they were dug up.
- **Finishers** at the back: scrape, smack, seal, paint.

With 3 to 4 players nobody holds one job. Everyone leapfrogs: dig together through a hill, then
everyone hauls, then everyone finishes. With 5 to 8, split into a pioneer crew and a paving crew, or
two crews starting from opposite ends and meeting in the middle.

**Moments the plan is designed to create:**

- **The cut:** a hill in the way. Dig through it (lots of cubes to deal with), go around it (longer
  road), or build up and over it (steep, bad for the truck).
- **The fill:** a dip that swallows every cube you can throw at it.
- **The uphill:** a required climb. Too steep and the truck cannot make it, so the crew has to cut a
  gentler ramp or switchback.
- **The hot relay:** a bucket brigade of shovels flinging asphalt down the line before it sets.
- **The paint job:** one blob walking very carefully while others try not to bump them.

### Pacing note [Claude]

The user resolved the earlier tension (2 km was too far for 20 to 30 minutes): **the session length
wins and the map shrinks to fit.** Size the Hill scenario by playtesting, starting small (towns a
few hundred metres apart) and growing it until a 3 to 4 player crew takes 20 to 30 minutes. Other
levers: scoop size, how many tiers the scenario demands, and how much of the route is easy ground.

### The crew at work [User's picture, Claude's elaboration]

- **Digger** scoops the cut; each bite pops out a cube.
- **Catcher/hauler** catches cubes on their shovel and takes them to where fill is needed, then
  smacks them into the ground there.
- **Prospectors** are meanwhile digging up the next phase's material (rock for gravel, then oil for
  tar), so it is ready when the current tier is done.
- The same pattern repeats each phase. Jobs rotate; everyone has the same shovel.

### Payoff and scoring [Claude]

A truck drives the route at the end (and possibly at each tier). Bad road shakes cargo out. Possible
score ingredients: time, cargo delivered, ride smoothness, road length versus the shortest possible,
mess left behind (stray cubes, spilled tar), paint accuracy, things destroyed that should not have been.

## 6. Things to dig up [Claude's ideas, answering "maybe we can dig more stuff up?"]

**[User] Oil is in.** "Digging up oil seems dumb and funny." [Claude] It is also accurate: the
sticky binder in asphalt is made from crude oil. So oil can be the source of tar: dig into an oil
pocket, it gushes, the crew scoops it up (and slips in it), and it gets cooked down into tar for
chip seal and asphalt. Oil-soaked sand cubes could be a ready-made cheap paving material.

**Materials (the ground itself has layers):**

- **Sand** on top, **clay** below, **rock** deepest. Each makes a different cube. Rock is the source
  of gravel, so the hill you cut through pays for the road you lay.
- **Tar seeps** — dark patches; scoop out sticky tar blobs. Dig too greedily and it gushes.
- **Paint veins** — bright mineral streaks in the rock, one color per vein.
- **Old buried road** — chunks of ancient asphalt that can be reheated and reused.

**Things that grow (keeping the dumb trees):**

- **Sap trees** — pry one out, it bleeds tar.
- **Traffic cones grow like carrots** — a little orange tip sticks out of the sand; pry it up.
- **Road-sign saplings**, **paint-can fruit**, **guardrail vines**.

**Trouble:**

- **Water pipes** — hit one and get a geyser and a mud pit. In town scenarios, utilities you must
  not break.
- **Boulders** too big to scoop: pry them, roll them, or smack them to bits.
- **Buried dynamite** from an old mine — a big dig in one go, wanted or not.
- **Burrowing things** — something that eats gravel or digs holes in your finished road at night.
- **Sinkholes** — a thin crust over a void.

**Rewards:**

- **Fossils and treasure** — bonus score, or awkward large objects exactly where the road should go.
- **Better shovels** — wide blade, long handle, spring-loaded, one that is clearly a spoon.
- **Ruins** — score penalty for wrecking them, so the road has to bend.

## 7. Scenarios [User's vision, Claude's notes]

Each scenario is a self-contained, replayable job with a score. [Claude] A scenario would define:
the terrain, the landmarks to connect, the tier each connection must reach, what is buried where, any
special rules (traffic in a busy town, utilities not to break), and score targets. Building this as
data from the start keeps new scenarios cheap.

### Scenario 1: the Hill [User]

Two towns with a **large hill between them**. It is the prototype's first challenge and the testing
ground for hills, collapses, cubes and earthquakes. Goals the user set:

- The scenario should **heavily encourage going around** the hill.
- **Through and over must both be possible, but more difficult.**
- **Trying to tunnel causes a collapse.**
- Target 20 to 30 minutes for 3 to 4 players; map size follows from that.

[Claude] How the three routes differ:

| Route | Cost | Risk |
|---|---|---|
| **Around** | Longest road, so the most surface to lay, but gentle ground and little digging | Low. The intended answer. |
| **Over** | Short, but the climb is too steep as it stands; needs a ramp or switchbacks cut into the slope | A steep road hurts the score (or the truck cannot climb it) |
| **Through** | Shortest, but an enormous amount of ground to move, and a mountain of cubes to put somewhere | Collapse if the walls are steep; cube pile-up brings earthquakes, which bring more collapses |

The risk to design against: "around" being so obviously right that nobody ever tries the others.
Scoring should make a clean cut through the hill the high-score route for a skilled crew.

## 8. World [mix]

- [User] Desert. Not flat. Must force uphill building in places.
- [Claude] Wide enough that routing is a real decision, with obstacles between landmarks: a ridge, a
  mesa, a dry riverbed, dunes.
- [Claude] Landmarks are simple solid-color structures.

## 9. Players and art [User]

- Solid-color blobs, one distinct color each. All animation procedural (see `TECH_PLAN.md` section 5).
- Solid-color shaders, no textures.
- [Claude] Suggested palette: tan sand, red-brown clay, grey rock and gravel, black tar and asphalt,
  white and yellow paint, green trees, saturated player colors.

## 10. Open questions

Answered 2026-10-04: session length, connection method, camera (section 2).

Also answered: pacing (time wins, map shrinks), slumping (steep cut walls collapse), tunnels
(attempts collapse).

To settle by experiment in game, not by discussion [User]:

1. **Cube caps** — how many loose cubes the game can carry with up to 8 players, and whether the
   cube mechanic is feasible at all.
2. **Earthquake tuning** — threshold, warning, how punishing.

Still to ask:

3. **Does "the only tool is the shovel" rule out carts?** Earlier tiers mentioned a handcart for
   hauling. Is a truck driving the finished road at the end still wanted as the payoff?
4. **Does the earthquake also strike on its own**, or only when the crew is messy?
5. **Cube size** — big chunky cubes (about half a blob tall) or small ones?
6. **Which dig-up ideas** from section 6 are in, beyond oil and gravel rock?
7. **Scoring** — which ingredients matter most?
8. **Default camera** — which of the three does a new player start in?
9. **Which tiers does the Hill scenario require** — all the way to painted asphalt, or stop at gravel
   for the first playable?

## 11. Decision log

| Date | Decision | Source |
|---|---|---|
| 2026-10-04 | Co-op game about building a railroad, 2 km slice, deformable desert, shovels, solid-color blobs, procedural animation | User's first brief |
| 2026-10-04 | 3 to 4 players, support 8; free routing; online peer-to-peer with a host | User |
| 2026-10-04 | Road instead of railroad, to focus on shovels | User |
| 2026-10-04 | Likes variants A to D and E; all five shovel verbs approved | User |
| 2026-10-04 | Jank means weird; digging makes physics cubes; world must not be flat | User |
| 2026-10-04 | Leaning heightfield over voxels; tunnels a known sacrifice; hybrid idea floated | User, not final |
| 2026-10-04 | Game is a series of replayable, high-score scenarios | User |
| 2026-10-04 | Digging up oil is in ("dumb and funny") | User |
| 2026-10-04 | Join code now, Steam later; all three cameras; first scenario 20 to 30 minutes | User |
| 2026-10-04 | Earthquake as the cube-recycling mechanic | User idea ("maybe"), not final |
| 2026-10-04 | Leaning into earthquakes; they should be punishing; caps to be found by experiment; mechanic may prove infeasible | User |
| 2026-10-04 | Scenario 1 is the Hill: two towns, big hill between; around encouraged, over/through possible but harder; tunnelling collapses | User |
| 2026-10-04 | Session length (20 to 30 min) outranks map size; 2 km dropped as a requirement | User |
| 2026-10-04 | The only tool is the shovel; crew roles (digger, catcher/hauler, prospectors); deterministic physics dropped | User |

## 12. History: the railroad version

The game began as a railroad builder: level the ground, add gravel, lay track, with trees that turn
into railroad ties and bear rails as fruit ("It's dumb. Cool."). The user asked which goes first,
ties or rails. Answer: **ties first**. Real track is built bottom-up (levelled earth, gravel ballast,
wooden ties, then rails spiked on top), because the ties are what hold the rails at the right
spacing. The railroad was dropped for the road, but a train station appears in the user's later
scenario ideas, so this may come back.
