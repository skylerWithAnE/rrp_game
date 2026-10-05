# Game design

Written 2026-10-04 in the redesign session. This replaces the design of the first prototype, which
is in git history (commit `4348b8e`) and is summarised in `POSTMORTEM.md`.

Everything under "Decided by the user" was said by the user. Everything marked **[Claude]** is a
proposal that the user has seen and not objected to; it is not a decision until it has been played.
Do not add anything that is not on this page. If something seems missing, ask.

## The game

An online co-op crew builds a road that a box truck can drive. In first person, the crew sets a
survey line, brings the ground to that line, lays and compacts gravel, and a truck drives the
result.

This is a mechanics prototype. It has no job, map, towns, score or clock yet.

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
- **Gravel and compaction are two tools**, one job each. Their details are left until station 3
  has been played.
- **The truck drives itself** and fails for a visible reason. Carried over from the first
  prototype, where it was the part that worked.
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
