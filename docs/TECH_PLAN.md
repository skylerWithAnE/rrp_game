# Technical description

Written 2026-10-05, after the five stations, and brought up to date through that day as the maps
and the test grounds were built, and on 2026-10-06 for the six slices (the version before that
is commit `71fcc8b`). This says how the code works now. It says nothing about the
long-term goals in `DESIGN.md`, none of which is built. The plan is `PLAN.md`. The first prototype's
technical plan is in `archive/TECH_PLAN_badscale.md`.

## Stack

- Unity `6000.3.25f1`, URP, the new Input System.
- Netcode for GameObjects in host mode over Unity Transport. One player hosts; there is no
  dedicated server. Direct connection on port 7777 works. Relay join codes are written and have
  never connected (no Unity Cloud project is linked).
- One empty scene. Everything is made at runtime from scripts. Meshes and materials are made in
  code. One shader, `Resources/SolidColor.shader`: a solid color times vertex color, one sun,
  shadows, optional flat shading, optional unlit.

## Files in use

| File | What it does |
|---|---|
| `Game.cs` | Boot, the session flow (menu, connecting, playing), which map is chosen, the roster, and the one message handler |
| `Session.cs` | Host and join. The only place that knows how players find each other |
| `Net.cs` | The message buffer (`Msg`), the list of messages (`Op`), and sending with byte counts |
| `Tuning.cs` | Every gameplay number. Float fields only; each is synced and is a slider |
| `Player.cs`, `Blob.cs` | The blob: walk, sprint toggle, hop; pose sync; procedural animation |
| `CameraRig.cs` | First-person camera at eye height, horizontal field of view |
| `Yard.cs` | Station 1: flat ground, the parked truck, the road strip, three painted hairpins. Also the truck's shape |
| `Plot.cs` | A piece of ground with its stakes, sections and tools. Twenty-six small ones are shared out among the test grounds; one more is the land of a map, with its towns |
| `Lines.cs` | Paint drawn by hand on the Painting ground's two strips: cells of 0.1 m, strokes, the tools that make them, and the judging |
| `Lorries.cs` | The trucks that drive themselves, on the test grounds' roads and on a map's, and the ones that wait to be sent |
| `Cars.cs` | The vehicles players drive |
| `Shovel.cs` | The shovel in the local player's view, and its motion |
| `Hud.cs` | Menu, readout (F3), tuning panel (F1), labels over things, tool keys |
| `AutoTest.cs` | Test tooling: command-line flags for self-hosting, self-joining, clicking and logging |
| `Mats.cs` | Materials and primitive meshes |

Switched off, from the first prototype: `Cubes`, `Quake`, `Blocks`, `Road`, `Truck`, `Verbs`,
`Ground`, `Sfx`. `Game` still creates them and disables them, and `Phase.Job` and its code paths
are never entered. `Truck.Boom` and `Sfx` are still used for the trucks' explosions.

## Networking

- NGO is used only for connections and one named message. There are no NetworkObjects, no RPCs
  and no networked prefabs. Every message starts with an `Op` byte.
- Players move themselves and send their pose 20 times a second. The host relays all poses.
- Everything else is decided by the host. A client asks (`Click`, `Stake`, `StakeEdit`); the host
  checks, applies, and sends the result (`PlotEdit`, `PlotStakes`). Clients never generate or
  change ground themselves.
- The host chooses the map (`Game.SetMap`, sent as `Map`). Everyone throws their ground away; the
  host makes the new and sends it. Choices 1 to 5 are maps: one plot of land (`Plot.Land`).
  Choices 0 and 6 to 9 are test grounds: the flat yard and the plots `Plot.MapOf` gives them.
- Anyone can join at any time. A joiner is sent the map number and the tuning, then every plot
  that exists. A station plot goes whole: a header (`PlotState`), the ground in messages of 4,000
  points (`PlotRows`: height in millimetres as two bytes, gravel and packing as one byte each),
  then the stakes (`PlotStakes`). A map's land goes as described under "The map's ground". Either
  way the plot is built when the stakes land.
- A local player does not move until there is ground (`Game.WorldReady`), and is put at the start
  once there is (`Game.respawn`).
- Trucks are simulated on the host and sent 20 times a second, unreliably (`Lorry`).
- The hot spot is local to each player and is not sent: a click carries the player's own word that
  it was on the hot spot.

## The plot

`Plot` is one rectangle of ground with its own stakes. There are twenty-seven, made by `Generate`
from their index (the list is at the top of `Plot.cs`): twenty-six small ones shared out among
the test grounds, and one for a map's land. The land is always the last (`Plot.Land`), so a new
plot goes in before it and the land's number moves up.

- **Ground:** a height per point, points 0.25 m apart, plus a byte of gravel (millimetres), a
  byte of packing (0 to 100) and a byte for what is on top (`top`: 0 nothing, under 100 asphalt
  as dumped, 100 spread, up to 200 as it is rolled, 255 painted). All three bytes travel with
  every edit. The surface drawn and walked on is ground plus gravel plus asphalt.
  Meshes are squares of 32 by 32 cells (8 m), each with its own collider; an edit rebuilds only
  the squares it touches, so a click costs the same on a 300 m map as on a station.
- **Stakes and ropes:** a list of stakes (position, with y as the height of the line there) and a
  list of ropes between them. A stake takes two ropes.
- **Sections:** `Rebuild` turns each rope into a `Seg`: start, direction, length, the two heights,
  and the slant of each end. Where two sections meet, both ends are cut along the line that halves
  the bend. `Section(x, z)` says which section covers a spot and where in it: `t` from 0 to 1
  between the two cuts, and `side` in metres right of the centre line. `World` goes the other way.
- **What a point is:** `Resolve` fills, for each point a section lies over, its zone (none, road,
  shoulder), its section, its place in it, and its target height. When stakes change, only the
  sections that changed are resolved again, over the box they covered before and the box they
  cover now. Pulling out an early stake renumbers every later section, so that one is slower.
- **The level and gravel counts** (`Tally`) visit every point, so they run at most four times a
  second, not on every click.
- **The grid:** a section has a whole number of squares to a lane and to its length, as near the
  patch width as possible; each shoulder is one square. `Square` snaps a spot to its square.
- **A click** (`HostClick`) is capped per player, then applied to every point in the square (or
  in a disc, for the round brush): grading moves the height toward the target, gravel adds depth
  and then packing. Changed points go out in one `PlotEdit`.
- **Junctions.** A stake with three or more ropes has no mitre. `Rebuild` gives each of its
  sections an extension past the stake (`eA`, `eB`) and a level stretch (`pA`, `pB`), and `Span`
  turns those into where the section's ground and its line begin and end. Everything that
  places a point in a section goes through `Span`.
- **Roles.** A rope is three numbers: its two stakes and its role (0 for everyone, 1 service),
  sent with the stakes. `HostZone` changes one. `PathBetween` can be told to leave service roads
  out, and is for every truck that is not the gravel truck.
- **Wear** (`Wear`) is host only. Health is a byte per point and is not sent; ruts are ordinary
  edits. `Plot.Wears` says which roads have it: the first wear road (plot 5, which keeps its
  old rule inside the same function), the Wear ground's two, a map while `mapWear` is on, and
  the quarry while `quarryWear` is. Four times a second each truck calls it with where its
  wheels are and how fast each has come down on the ground since the last call
  (`Lorry.hit`, the wheel's closing speed along its ray). `Damage` takes health off a grid
  square, by the rate of what the square is made of, more once it is under the threshold; `Cut`
  lowers a disc under a wheel whose square is under the threshold: packing, then gravel, then
  ground, and never deeper than `wearDeepest` below the line. Asphalt is skipped by both.
- **Loose gravel** is `Plot.Loose(x, z)`: 0 to 1, from the packing and the depth of gravel
  under a wheel.
- **The drop-off** (the quarry) is a stake's number, `Plot.drop`, sent on the end of every
  stakes message and renumbered when a stake comes out (`PullOut`). `HostDrop` puts it down
  under the stake rules plus two of its own.

## The map's ground

This was the risk in `PLAN.md`: a 300 m map at 0.25 m is 578,000 points. What was tried and kept:

- **The points stay 0.25 m apart everywhere.** Coarser ground was not needed. Making the 300 m
  map takes 0.3 seconds on the host, and both instances ran at their usual frame rates on it.
- **The host makes the land as a height every metre, in whole millimetres** (`MakeLand`, `Shape`),
  and sends only that (`PlotCoarse`): 30 to 70 kB for a map, against over 2 MB for every point.
- **Every machine fills in the points between with whole-number sums** (`Expand`). No machine
  has to reproduce another's floating-point arithmetic, so the ground cannot differ.
- **After that only changes are sent.** Clicks go out as they always did (`PlotEdit`). The host
  marks each point a click has changed, and a late joiner is sent those points alone
  (`PlotPoints`, ten bytes each, with the exact height).
- The other options in `PLAN.md` were not built: land made from a seed on every machine (needs
  every machine to agree on noise arithmetic), and a map as a strip of plots (sections would
  have to cross plot edges).
- A map's land is the same every time (the seed is fixed per map) until the host asks for new
  land. The towns are stakes 0 and 1: fixed, and not removable.
- `Survey` walks the ropes from stake 0 after every stake change and sets `joined` when they reach
  stake 1.
- **Rocks** (the Switchback map) are a list of x, radius, z worked out in `PlaceRocks` from the
  map's number and length alone, so they are never sent. `RockNear` is the rule: `CanAdd` and
  `RopeAllowed` use it. The ground under a rock is ordinary ground.
- **Why a rope is refused**: each rule in `CanAdd` and `RopeAllowed` leaves a line in `Plot.Why`,
  and `Hud` draws it under the crosshair while the rope shows red.
- `TestRoad` and `TestFinish` are test tooling on the host: a road through given points whatever
  the rules say, and every staked point put on its line with the road bare, gravelled or packed.
  Clients are not told, so they are for one instance only. The slope figures in `DESIGN.md` came
  from them.

## The trucks

`Lorries` holds two trucks per station plot, one each way, and twelve for a map. Each is a
rigidbody the size of the real truck on four rays with springs, steering toward a point a few
metres ahead on a path that `Plot.Route` builds along the right-hand lane. Its push is scaled by
`Plot.Going` (the surface under it). On its side, or getting no further along its path for
`lorryStuckSeconds`, it is thrown up and blows up.

`Route` searches the ropes for the shortest way (`PathBetween`), so stakes can be put down in any
order and roads can branch. On a test ground it runs between two ends of the roads for
everyone, picked at random if there are more than two. On a map it runs from stake 0 to stake 1
and there is no route until they are joined; then a truck leaves each town every `truckEvery`
seconds. `Lane` turns the sections found into the points a truck steers at. A truck keeps the
path it set off with, even if the stakes change under it.

**Gravity** is one number, `tuning.planetGravity`. `Game.Update` sets `Physics.gravity` from it
on every machine; the blob multiplies its own fall by it; and `Game.Bite` and `Game.Springs`
turn it into how hard wheels push and hold and how stiff springs are, for trucks and driven
vehicles alike, according to the two switches beside it. At 1 every one of those is exactly
what it was.

**Steady traffic.** `Plot.Steady` roads (a map, the Wear ground's two, the Spin-out road) have a
truck set off down each lane every few seconds; `Plot.Traffic` says how many a road can hold.
The snapshot starts with the map's number and carries only that map's trucks.

**Spin-out** is in the truck's drive: per wheel, sideways grip falls with `Plot.Loose`; the
steering torque weakens and a wandering torque is added in proportion to it; and past
`spinAngle` between heading and motion the truck is in a spin (`Lorry.spin`) for `spinSeconds`,
with no steering and its wheels locked.

**The gravel truck's round** (`Rig.leg`: 0 standing, 1 out, 2 backing in, 3 home). `SendHaul`
starts a leg, from `Lorries.Update` when the truck is full or empty, or from a right click.
`Plot.HaulOut` is the way from the quarry to `haulPass` metres past the junction the drop-off's
spur leaves; `Plot.HaulIn` is the way a truck leaving the drop-off for that same stretch would
drive, and the truck follows it backwards (`Lorry.reverse`: the index runs down, and its tail
is steered at a point behind); `Plot.HaulHome` is the way back. `HaulArrived` moves from one leg
to the next. The stuck rule reads "no further" in whichever direction the truck is following.

**Paint by hand** (`Lines`, one for each of the Painting ground's strips, made by `Plot.Build`).
A byte per cell of 0.1 m. Everything that changes it is a stroke: tool, two ends and a radius,
in whole millimetres from the plot's corner. `Lines.Apply` tests each cell's middle against the
stroke with whole-number sums, so every machine gets the same cells. A player's machine makes
strokes (`Hold`: the roller brush joins where it was to where it is; the tar spray scatters
points; the grinder is a thin brush that writes "nothing") and sends them a few times a second
(`Op.Paint`); the host applies them and passes them on to everyone; the host makes the paint
truck's own strokes from where its nozzles were and are (`Cars.Spray`, 20 times a second). A
joiner is sent the cells as runs (`Op.PaintState`), after the plot's stakes. `Wanted` works out
from the sliders, per cell, which line's band it is in, its grid square and how far along it
is; `Judge` turns that and the paint into each square's score, twice a second at most. The paint
is drawn as quads with vertex colors, in meshes of 32 by 32 cells, rebuilt when they change.
`Plot.Hash` takes the paint in, so the two-instance comparison covers it.

**A road at a button** (`Plot.HostAutoRoad`, host only): a list of places per map, a smooth
line through them, stakes at even steps, heights eased under the slope limit, then every staked
point put on its line and sent as ordinary edits. It says what it staked against the rope rules.

**Rigs** are trucks that wait to be sent: the quarry's gravel truck, and the paving ground's dump
truck and roller. Each is one more lorry at the end of the list, with a `Rig` beside it: the
depot it stands at, the one it is going to, and its load. A plot names some of its stakes as
depots; `Plot.DepotRoute` is the way from one to another by any road, and `Lorries.Send` puts
the truck at the start of that way and lets it go. A standing rig is held still and is never
stuck. As it drives, a dump truck calls `Plot.Dump` and a roller `Plot.Roll` four times a second.

Everything a player does with a shovel or a rig is one `Shovel` message (`Plot.HostShovel`):
load the shovel, fling it into the truck, take from the truck, fling to the heap, place the
heap, send a rig. The host decides each. The rigs' states and loads, whose shovels are loaded,
and the heap's place and size go to clients on the end of every truck snapshot.

**Seats.** A vehicle has a driver and, for the pick-up and the paint truck, riders
(`Car.riders`, with `riderSeats` for where their feet are). `Cars.Mine` is the vehicle the local
player is in and `Cars.MySeat` which seat. Only the driver's machine moves it; with no driver
it is the host's and stands still. Getting in, out and from seat to seat, and the nozzles, are
all `Cars.HostAsk`. The camera (`CameraRig`) sits behind the vehicle while `Cars.Outside`.

**The Wear ground** is four plots, one to a road, each a strip of the same high ground
(`Plot.WearGround`): the strips are exactly 24 m wide and share their edge points, so the
ground is continuous. `Plot.WearRoad(id)` says which road a plot is.

**Driven vehicles** (`Cars`) are built in the same places on every machine when the Driving
ground or the Painting ground is chosen; nothing creates them over the network. Whoever drives one simulates it (the
same four sprung rays as a truck) and, if not the host, sends its pose 20 times a second
(`CarPose`); a vehicle with no driver is the host's. Getting in, getting out, tipping the
bucket and switching the paint on are asked of the host (`Car`). The host sends every
vehicle's driver, load, bucket and pose to all clients (`Cars`), and from the poses decides what
each does to the ground, four times a second (`Cars.Work`): `Plot.Pack` and `Plot.Roll` under
the roller, `Plot.PaintUnder` under the paint truck, `Plot.Tip` for the loader's bucket. A
driving player's blob sits at the seat, and on the host its pusher is switched off so it does
not shove the vehicle.

**The shovel** (`Shovel`) is a child of the camera, moved by four springs. `Shovel.Swing` is
called where the local player's click is made (`Plot.Update`, `Plot.QuarryTool`), kicks the
springs, and tells the others through `Game.SendSwing`: a client sends `Swing` to the host,
which plays it on that player's blob (`Blob.Swing`) and passes it on as `VerbFx`.

The dump truck on the Paving ground sends itself (in `Lorries.Update`): it asks
`Plot.NeedsAsphalt` and calls `Send` with nobody's hand on it.

The dev tool is a `Finish` message: `Plot.HostFinish` does a section's next stage and sends the
points like any other edit.

## Verifying

- What a truck climbs, the pace of wear and the spin-out rates in `DESIGN.md` were measured by
  scripts driving the editor: `Plot.TestDegrees` and its neighbours make ramps to order,
  `Lorries.log` records how each trip ended, `AutoTest.WearLog` records a wearing road's state
  by truck count, and `tuning.fastForward` runs it all at ten times the speed.
- `AutoTest` prints one `RRPSTATE` line a second with `-rrpLog`: player positions, the tuning, and
  for each plot a hash of its ground, gravel, packing and stakes, its level and gravel shares, its
  clicks and its stake count.
- The method: host in the editor, a standalone build as the client, drive both, stop the action,
  and compare the two lines. See `SETUP.md` for the flags and `NOTES.md` for the traps.
- A script driving the editor must hand anything that sends a message to `AutoTest.Next`, which
  runs it inside the game's frame. See `NOTES.md`.
