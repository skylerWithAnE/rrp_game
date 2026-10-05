# Technical description

Written 2026-10-05, after the five stations. This says how the code works now. The plan for what
comes next is `PLAN.md`. The first prototype's technical plan is in `archive/TECH_PLAN_badscale.md`.

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
| `Game.cs` | Boot, the session flow (menu, connecting, yard), the roster, and the one message handler |
| `Session.cs` | Host and join. The only place that knows how players find each other |
| `Net.cs` | The message buffer (`Msg`), the list of messages (`Op`), and sending with byte counts |
| `Tuning.cs` | Every gameplay number. Float fields only; each is synced and is a slider |
| `Player.cs`, `Blob.cs` | The blob: walk, sprint toggle, hop; pose sync; procedural animation |
| `CameraRig.cs` | First-person camera at eye height, horizontal field of view |
| `Yard.cs` | Station 1: flat ground, the parked truck, the road strip, three painted hairpins. Also the truck's shape |
| `Plot.cs` | Stations 2 to 4 and the example roads: ground, stakes, sections, the tools, wear |
| `Lorries.cs` | Station 5: the trucks |
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
- Anyone can join at any time. A joiner is sent the tuning, then every plot whole: a header
  (`PlotState`), the ground in messages of 4,000 points (`PlotRows`: height in millimetres as two
  bytes, gravel and packing as one byte each), then the stakes (`PlotStakes`). The plot is built
  when the stakes land.
- Trucks are simulated on the host and sent 20 times a second, unreliably (`Lorry`).
- The hot spot is local to each player and is not sent: a click carries the player's own word that
  it was on the hot spot.

## The plot

`Plot` is one rectangle of ground with its own stakes. There are eight, made by `Generate` from
their index (the list is at the top of `Plot.cs`).

- **Ground:** a height per point, points 0.25 m apart, plus a byte of gravel (millimetres) and a
  byte of packing (0 to 100) per point. The surface drawn and walked on is ground plus gravel.
  Meshes are strips of 32 rows, each with its own collider; an edit rebuilds only the strips it
  touches.
- **Stakes and ropes:** a list of stakes (position, with y as the height of the line there) and a
  list of ropes between them. A stake takes two ropes.
- **Sections:** `Rebuild` turns each rope into a `Seg`: start, direction, length, the two heights,
  and the slant of each end. Where two sections meet, both ends are cut along the line that halves
  the bend. `Section(x, z)` says which section covers a spot and where in it: `t` from 0 to 1
  between the two cuts, and `side` in metres right of the centre line. `World` goes the other way.
- **What a point is:** `Resolve` fills, for every point, its zone (none, road, shoulder), its
  section, its place in it, and its target height. It runs again whenever stakes change.
- **The grid:** a section has a whole number of squares to a lane and to its length, as near the
  patch width as possible; each shoulder is one square. `Square` snaps a spot to its square.
- **A click** (`HostClick`) is capped per player, then applied to every point in the square (or
  in a disc, for the round brush): grading moves the height toward the target, gravel adds depth
  and then packing. Changed points go out in one `PlotEdit`.
- **Wear** (`Wear`) is host only. Health is a byte per point and is not sent; ruts are ordinary
  edits.

## The trucks

`Lorries` holds two trucks per plot, one each way. Each is a rigidbody the size of the real truck
on four rays with springs, steering toward a point a few metres ahead on a path that `Plot.Route`
builds along the right-hand lane. Its push is scaled by `Plot.Going` (the surface under it). Still
for too long or on its side, it is thrown up and blows up. `Route` assumes the stakes are in order
along the road, which is true of the roads the game lays out and not of ones players stake.

## Verifying

- `AutoTest` prints one `RRPSTATE` line a second with `-rrpLog`: player positions, the tuning, and
  for each plot a hash of its ground, gravel, packing and stakes, its level and gravel shares, its
  clicks and its stake count.
- The method: host in the editor, a standalone build as the client, drive both, stop the action,
  and compare the two lines. See `SETUP.md` for the flags and `NOTES.md` for the traps.
