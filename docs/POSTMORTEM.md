# Post-mortem: the first prototype

Written 2026-10-04 by Claude at the user's request, at the end of the session that built
milestones 0 to 6. Its job is to give the next design session a true picture: what was built, why
it missed, what is worth keeping, and what has to be decided before anything else is built.

**Read this before `DESIGN.md` and `TECH_PLAN.md`.** Those two describe the prototype as built. They
are accurate as a record and wrong as a plan.

## Where we are

A working, networked Unity prototype exists and the user does not want this game. After three
playtests the verdict was, in order: "a good start", "the world feels so much smaller than I want,
this pacing is tedious", and "the scale of everything is way off".

The user's own summary of the miss:

- The game was hampered by the camera angle. Listing first, third and top-down cameras may have
  confused the vision.
- The picture in the user's head looks more like **Lethal Company**.
- The heightmap is incredibly simple and low resolution. The scale wanted is like **Valheim**, with
  terrain mechanics like Valheim's.
- The whole idea began while trying to build roads in **Satisfactory**.

Decided by the user on 2026-10-04, for the redesign:

1. **"The shovel is the only tool" goes.**
2. **First person.**
3. **Throwing cubes around is out of favour.** It was not as funny as hoped and is expected to become
   a problem later.

**The redesign is about mechanics** (user, 2026-10-04). Do not let the look of other games steer
the mechanics prototype.

## Lethal Company as a reference: fidelity, scale, simple mechanics

The user named Lethal Company as an example of three things. It is not a reference for theme,
horror or setting.

Facts from its Steam page (read 2026-10-04): first person, online co-op for up to 4, one developer
(Zeekerss), early access since October 2023. Players collect scrap from abandoned moons and "carry
all valuables to the ship" to meet a quota. Its tools are "lights, shovels, walkie talkies, stun
grenades, or boomboxes". The rest of this section is Claude's recollection and reading. Claude has
not played it; check anything that matters.

**Fidelity: low, and that is enough.**

- It renders at a low resolution with simple models and flat materials, and it sold enormously
  anyway. One person made it.
- The lesson for this project is permission, not a style to copy: nothing has to look good for the
  mechanics to be judged. Grey boxes and flat colors are fine.
- The prototype's looks were not its problem. Its solid colors would have been acceptable at the
  right scale and camera.

**Scale: everything is sized from a standing person's eyes.**

- The camera is at eye height. A doorway, a ladder, a ship and a crewmate all read at their real
  size because the player's body is the ruler.
- The world is small in area. A level is a ship, a walk of a minute or two, and a building. It
  feels big because crossing it on foot takes time and matters, and because what is carried slows
  the player down.
- The prototype did the opposite: a camera 6 to 8 m up and back, looking down at a 1 m character,
  on ground whose pieces were smaller than the character. The same 64 m map would feel several
  times larger walked at eye height.
- So "bigger" is first a camera and proportion problem, and only second a map-size problem. Sizes
  should be chosen in metres from eye height, then checked by walking them.

**Simple mechanics: few verbs, each one obvious.**

- The player walks, sprints, jumps, crouches, picks things up, carries them, and drops them. Tools
  are separate objects, and each does one thing: a light lights, a shovel hits.
- An object is held in the hands and seen there. Big things take both hands. Weight slows the
  carrier. Nothing needs a tutorial or on-screen text to explain what a button will do.
- The fun comes from the crew and the situation, not from the depth of any one mechanic: who
  carries what, who goes back, what gets left behind.
- The prototype had one tool whose two buttons meant eight different things depending on what was
  carried and what was under the crosshair, plus a 14-cell table of cube-on-surface outcomes. It
  needed a hint line to be usable. That is the clearest measure of how far it was from this
  reference.
- A working test for any new mechanic: can a player predict what the button will do before
  pressing it, with no text on screen?

What this suggests for the redesign, as Claude's proposals to discuss, not decisions:

- Separate tools with one job each, now that the shovel is no longer the only tool.
- Material that is carried in the hands or by a vehicle rather than thrown.
- A short list of verbs, settled before any system is built.
- First-person grey-box at human scale first, with fidelity left alone.

## Where the idea came from: the Satisfactory road mod

Source: `C:\Users\skyle\source\repos\satisfactory-map-mod` (its `CLAUDE.md` and commit history; the
brief file there is empty). This is Claude's reading of it, not something the user dictated.

The mod tried to lay two-lane truck paths along Satisfactory's natural dirt roads, so trucks could
drive the map by themselves. What happened:

- **The ground could not be changed, so the road had to be bent around it.** The road data was a
  rough line, often several metres to one side of the real dirt road. Lanes landed on grass and
  rock and the game refused them: "Vehicle collides with terrain!"
- **The game is the judge, per vehicle.** Each path piece passes or fails for each vehicle, for a
  stated reason: static obstacle (rocks, trees), turn too sharp, terrain too steep, no floor, under
  water. In the last recorded run 84 of 96 pieces were valid.
- **The fix available was nudging.** Failing pieces were pushed sideways up to 2.25 m to get round a
  rock. An obstacle on a join, or wider than the nudge, could not be fixed at all. The user wanted to
  drag control points on the line by hand.
- **Junctions were the hard part.** Merging lanes at a point was rejected in game. Keeping separate
  lanes with connector curves needed 998 connectors.
- **The truck was the interesting vehicle** because it shrugs off small rocks the others stop at,
  while failing more often on turns.
- **No truck ever drove a finished loop.** The payoff was never reached.

What that implies for this game, as Claude reads it:

- **This game is the wish the mod could not grant: change the ground until the truck gets through.**
  Remove the rock, fill the dip, ease the slope, widen the corner.
- **The truck succeeding is the reward**, and its failures are the feedback. A stuck truck that says
  why (too steep here, blocked here, too tight here) is the Satisfactory validator turned into play.
- **The scale is vehicle scale.** The mod's numbers: truck 6.5 m wide and 10.5 m long, lanes about
  9.75 m apart, an 18 m turning width, slopes up to about 15 degrees accepted, 85 km of road. The
  prototype's truck was 0.9 m wide on a 64 m map.
- **Roads follow the land.** Route choice, corners and junctions were the substance of that project.
  The prototype reduced a road to painting grid squares.

None of this was in the documents Claude was given. The Satisfactory origin first came up in the
message that asked for this post-mortem.

## What was built

Eleven commits on `main` before this document, nothing pushed. About 3,900 lines of C# in
`Assets/Scripts`.

| Milestone | What exists |
|---|---|
| 0 Setup | Unity 6000.3, URP, Input System, Netcode for GameObjects, Unity Transport, Multiplayer Play Mode, Multiplayer Services. Setup and build are menu items. |
| 1 Online blobs | Menu, lobby, host and join, third-person camera, spring-animated blobs. Late joiners are refused. Relay join codes are written but have never connected (no cloud project linked). |
| 2 Diggable hill | Chunked heightfield, dig, pack, flatten, fling and catch, set down. |
| 3 Cube experiment | Earthquake, collapse, oil split, live tuning panel, readout, stress series. |
| 4 Road recipe | Sand over rock, buried oil and paint pockets, the smack table, fail noises, gravel/asphalt/paint, 3-wide road check. |
| 5 The Hill | Two towns, a physics truck that follows the road and blows up when stuck, win on paint, clock, back to lobby. |
| 6 Reinforcement | Blocks from rock cubes, reinforced walls, earth on tunnel roofs, towns as blocks. |
| 7 Tuning | Not done. It needed players. |

Changes made in response to playtests: a dug cube goes straight onto the shovel; hold to dig but
click to let go; the grid doubled to 1 m; one smacked cube surfaces a 3x3 patch; an on-screen hint
says what each mouse button will do.

What was verified, and how: almost everything by script, bots and state comparison. Up to 8
instances ended with identical ground, cubes and blocks. The real keyboard and mouse path was
exercised with simulated input events. **No human built a road, placed a block or completed a job.**

## What Claude was building, against what the user wanted

| | Built | Wanted (as now stated) |
|---|---|---|
| View | Third person, orbit camera 6 to 8 m back, crosshair aiming past the character | First person |
| Look | Flat solid colors, round blobs, bright sky | Low fidelity is fine (see Lethal Company); not the priority |
| World | 64 m square, one 8 m hill, towns 40 m apart | Valheim scale |
| Terrain | A grid of points 0.5 m (later 1 m) apart, moved one whole cube at a time | Valheim-like terraforming |
| Tools | One shovel, two mouse buttons, meaning changes with what is carried | More than one tool |
| Material | Everything is a physics cube, flung, caught and smacked | Not thrown cubes |
| Road | Recolored grid squares, 3 squares wide | A road a truck drives, at truck scale |
| Truck | 0.9 m wide, later 1.4 m | A real vehicle |

Claude's reading: the miss is not in any one system. The prototype answered "can loose physics
cubes work online" and "can a crew build a road out of them". The user wanted to know what it feels
like to stand on a hillside and reshape it so a truck can get through. Those are different games
that share the words "co-op", "road" and "shovel".

## How it went wrong

### The scale was fixed by rules, not chosen

Every size in the prototype follows from four written rules:

- a cube is half a blob tall (`DESIGN.md`)
- one ground cell is one cube wide (`TECH_PLAN.md`)
- a road is at least 3 cubes wide (`DESIGN.md`)
- a session is 20 to 30 minutes, and that "outranks map size; size the map to fit" (`DESIGN.md`)

A blob is about a metre, so a cube is 0.5 m, a road is 1.5 m, a truck that fits is under a metre
wide, and a map a crew can finish in half an hour is tens of metres across. Claude followed that
chain faithfully and never showed it to the user as a consequence. No document gave a size in
metres, a reference game, or the word "Valheim".

### The camera made the small scale worse

From 6 to 8 m behind a 1 m blob, a 0.5 m ground face is a few pixels. First person at eye height
would have made the same ground feel several times larger. `DESIGN.md` said "Camera: third person"
with first person listed under "Later", so third person was built without question.

### The terrain is coarse in the wrong way

The final grid, 1 m between points, is roughly the resolution Claude believes Valheim uses (unchecked).
The difference is how edits behave. Here one action moves one point by one whole cube, so every dig
is a hard pyramid. Valheim-style tools act on a radius with a soft edge, level toward a height, and
change the ground by small amounts. "Low resolution" is as much about the brush as the grid.

That brush could not exist under the rule that everything dug up is a cube, because a cube has to
be one fixed amount of ground.

### One tool carried too many meanings

With only the shovel, two mouse buttons had to cover dig, scoop, fling, set down, smack, flatten,
place a block and pick up a block, chosen by what was carried and what was under the crosshair. The
user's confusion over grey cubes ("sometimes they make a tile, sometimes they place a block, then
right click does nothing") is that overload. The hint text added afterwards explained the rule; it
did not make it good.

### Seven milestones stood on an unaccepted base

Milestones 4, 5 and 6 were built overnight on milestones 1 to 3, which had been played once, for a
few minutes, with keyboard and mouse working for the first time. The first real feedback on scale
came after all of it was built.

## In Claude's defence

The user asked for this section. These are the rigid or under-communicated parts of the brief.

- **"Fight scope creep. Do not add anything that is not on this page."** This was the loudest
  instruction in the project, repeated in `CLAUDE.md`, `DESIGN.md` and the first prompt. It worked.
  It also told Claude not to question the page.
- **"The only tool is the shovel"** was in bold in the first paragraph. It is now the first rule to
  go.
- **The cube rules were specific and the scale was not.** Half a blob tall, 3 cubes wide, five
  quarter-height bits, a full smack table: the brief was exact about cubes and silent about how big
  the world is, how tall a person is, or what it should look like.
- **No reference games were named** until the final message. Lethal Company, Valheim and
  Satisfactory each would have changed the first day's work.
- **"Blobby solid-color characters", "it should be weird", "fast, simple and weird beats correct
  and polished"** all point away from a grounded first-person game.
- **The camera was stated as a decision**, not a question.
- **The archive was off limits.** `docs/archive/` holds the fuller planning documents and Claude was
  told not to read them. If the Satisfactory origin or the Valheim scale is in there, it was cut
  from what Claude was allowed to see. Claude still has not read it.
- **The work was to proceed without checking in.** Both long sessions were explicitly "don't stop
  to check in with me" and "run as long as you can until you run out of milestones".
- **Cubes were framed as the experiment.** The plan's stated purpose for milestone 3 was to learn
  whether loose cubes work online. Claude spent a great deal of effort answering that precisely. It
  was the wrong question to spend it on.

## Claude's own mistakes

- **Never showed the scale.** One screenshot of a blob beside a cube, a road and a truck, sent
  before milestone 2, would have exposed the problem on day one. Claude derived the sizes and built
  on them.
- **Did not say the base was unproven before building on it.** After "a good start", Claude asked
  whether to start milestone 4 and took "yeah, go ahead" as enough. It should have said plainly that
  nobody had yet played a full loop and that three more milestones were a gamble.
- **No feel check alongside the sync checks.** Every system was verified across up to eight
  instances. The user has since said that is not a mistake: good networking at every step is
  wanted and should continue. What was missing was a person playing each step by hand as well.
- **Sized the job by a guess** (8 seconds per cube per player) and built a road recipe needing about
  900 cubes. The user found it too tedious to attempt.
- **Misread "everything needs to be bigger".** Claude doubled the grid. The user meant something
  closer to an order of magnitude and a different camera.
- **Added `roadSpread` without being asked**, in the same change, to rescue the pacing. It was
  flagged, but it was a patch on a design that needed questioning instead.
- **Asked questions with ready-made options.** The three multiple-choice questions after the second
  playtest steered toward small fixes. An open "what did you picture?" would have surfaced Valheim a
  session earlier.

## Challenges and how they were worked around

Useful to the next session whatever the design becomes.

| Challenge | What worked |
|---|---|
| The Unity MCP tools did not load in Claude Code (server was down when the session started) | Drive the server over plain HTTP. `tools/umcp.py` does this. |
| Play mode would not tick with the editor in the background | It needs focus once after entering play mode, then runs unfocused. |
| Switching to the new Input System needs a real editor restart | `EditorApplication.OpenProject` on the same project does not restart the process. Quit and reopen. |
| Testing several players on one machine | Multiplayer Play Mode clones can be switched on from script (internal `UnityPlayer.Activate`), and each clone registers with the MCP server. Headless standalone builds with command-line bots scale to 8. |
| Proving instances agree | A hash of the ground and of resting cube positions, logged once a second by every instance. |
| Frame-time numbers polluted by the test harness | Put the stress run inside the game as a coroutine that logs its own results. |
| Networked prefabs fight a code-first project | Skip NetworkObjects. One named message over NGO carries everything, and every byte gets counted. |
| A cube forced to sleep kept a stale position on clients | Send a final pose whenever a body was awake at the start of a step and asleep at the end. |
| A sleeping cube floats when the ground under it changes | Wake cubes near every ground edit. |
| Recompiling with clones attached reports "not ready" for minutes | Trust "is playing and not compiling" instead. |

## Observations worth carrying forward

- **The truck was the best thing in the prototype.** Watching it stall on the hill, bounce away and
  blow up was the one moment that worked as intended first time. It is also the closest thing to
  the Satisfactory origin.
- **Loose cubes cost bandwidth, not frame rate.** About 0.24 kB/s per moving cube per client at 20
  updates a second. 800 moving cubes is 190 kB/s to each client. Resting cubes are free. If loose
  material survives the redesign, this is its budget.
- **Results, not operations, made terrain sync trivial.** The host sends the new heights. No
  determinism was needed and no instance ever disagreed.
- **Cave-ins never showed up in play.** The user could not trigger one by digging naturally. A rule
  that only fires when you dig one spot four times is not a mechanic.
- **The earthquake was never felt by a person.** It was tested only by script.
- **Hidden pockets with no hint** were never found by hand. All oil and paint in testing came from
  debug buttons.
- **Dig-then-pick-up was one step too many.** The user's first request was for dug material to go
  straight onto the shovel.
- **Every rule that needed on-screen text to explain it was a rule in trouble.**

## What is reusable

Likely to survive a redesign:

- Project setup, build menu item, `tools/umcp.py`, the multi-instance test method.
- `Net.cs` and `Session.cs`: the message layer, byte accounting, host and join, lobby, late-join
  refusal. Relay is still untested.
- The host-decides, broadcast-results pattern for terrain.
- The truck's sprung-ray physics and road following, as a starting point for a real vehicle.
- The live tuning panel built from one asset.
- The solid-color shader, if the look stays flat.

Likely to be thrown away:

- The third-person camera and crosshair aiming.
- The verb model (`Verbs.cs`) and the smack table.
- Cubes as the unit of everything; blocks as cubes on the ground grid.
- Road as recolored grid points.
- The 64 m map, the hill, the towns.
- Blobs.
- The IMGUI interface.

Uncertain: the heightfield itself. A chunked heightfield with host-broadcast edits is a sound base
for Valheim-like terraforming. What has to go is one-point, one-cube editing.

## Questions for the redesign session

Claude suggests settling these, with numbers and pictures, before any system is built.

1. **A reference for scale and terrain.** One or two Valheim screenshots that show the scale and
   the terraforming wanted. The look is parked: this is a mechanics prototype.
2. **Three sizes in metres**: eye height, road width, truck size. The Satisfactory truck is 6.5 by
   10.5 m.
3. **How big is the job?** Road length in metres, and whether 20 to 30 minutes still outranks size.
4. **What are the tools**, now that there is more than one? What does each do to the ground?
5. **What replaces the cube?** Where does dug earth go, and how is road material carried, if not
   thrown?
6. **What judges a road?** Claude's suggestion from the Satisfactory story: the truck does, by
   driving it and failing for a visible reason.
7. **What does the land fight back with?** Rocks, trees, steepness, water and junctions were the
   Satisfactory obstacles. Earthquakes and cave-ins were this prototype's.
8. ~~Is multiplayer still the point from day one?~~ **Answered by the user 2026-10-04: yes.** Keep
   building and verifying every system networked from the start.
9. **Characters**: blobs, or people?
10. **Should Claude read `docs/archive/`?** It may already answer some of these.

And one process suggestion: start with a walkable grey-box at the right scale, in first person,
with one terraforming tool and a truck, and get it accepted by hand before adding anything.

## State of the repository

- `main`, clean, nothing pushed. Latest work commit: "Everything bigger: 1 m grid, road patches, and
  a hint for each button".
- To run: open the project in Unity 6000.3.25f1, press Play, "Host (direct)", Enter.
- `Builds/rrp_game/rrp_game.exe` matches the latest code (not in git).
- `DESIGN.md` and `TECH_PLAN.md` describe the prototype as built, including 39 numbered decisions
  that were waiting for confirmation. Most are now moot.
- Nothing is ticked in `TECH_PLAN.md`. No milestone was formally accepted.
