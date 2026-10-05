# Technical description

Written 2026-10-05, after the five stations, and brought up to date the same night after the
maps were built. This says how the code works now. The plan is `PLAN.md`. The first prototype's
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
| `Plot.cs` | A piece of ground with its stakes, sections and tools. Eight small ones are the stations; a ninth is the land of a map, with its towns |
| `Lorries.cs` | The trucks, on the stations' roads and on a map's |
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
  host makes the new and sends it. Map 0 is the stations (plots 0 to 7 and the yard); any other is
  one plot of land (plot 8, `Plot.Land`).
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

`Plot` is one rectangle of ground with its own stakes. There are nine, made by `Generate` from
their index (the list is at the top of `Plot.cs`): eight for the stations and one for a map.

- **Ground:** a height per point, points 0.25 m apart, plus a byte of gravel (millimetres) and a
  byte of packing (0 to 100) per point. The surface drawn and walked on is ground plus gravel.
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
- **Wear** (`Wear`) is host only. Health is a byte per point and is not sent; ruts are ordinary
  edits. Only the wear road has it.

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

`Route` follows the ropes (`Chain`), so stakes can be put down in any order. On a station it
starts from the first stake with one rope. On a map it runs from stake 0 to stake 1 and there is
no route until they are joined; then a truck leaves each town every `truckEvery` seconds. A truck
keeps the path it set off with, even if the stakes change under it.

## Verifying

- `AutoTest` prints one `RRPSTATE` line a second with `-rrpLog`: player positions, the tuning, and
  for each plot a hash of its ground, gravel, packing and stakes, its level and gravel shares, its
  clicks and its stake count.
- The method: host in the editor, a standalone build as the client, drive both, stop the action,
  and compare the two lines. See `SETUP.md` for the flags and `NOTES.md` for the traps.
- A script driving the editor must hand anything that sends a message to `AutoTest.Next`, which
  runs it inside the game's frame. See `NOTES.md`.
