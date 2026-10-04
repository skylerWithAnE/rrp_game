# Technical plan

Final trim 2026-10-04. Claude's proposals for building what `DESIGN.md` describes, except where
marked **[User]**. Build only what the current milestone needs.

## Stack

- Unity `6000.3.25f1` [User], URP, new Input System.
- Netcode for GameObjects in **host mode** with Unity Transport [User: online, peer-to-peer, one
  player hosts, no dedicated server].
- **Unity Relay join codes** [User: join code now, Steam later]. Needs a Unity Cloud project linked
  to this project. Keep session setup in one small class so it can be swapped.
- Multiplayer Play Mode for testing several players in one editor.
- Code-first: one bootstrap scene, world generated at runtime, materials and meshes made in code,
  every tunable number in one `Tuning` asset.

## Terrain

- **Heightfield** [User], split into chunks. One cell is one cube wide.
- Per cell: ground height, depth of sand over rock, any pocket (oil, paint), road surface (none,
  gravel, asphalt, painted), and a cosmetic paint color.
- **Scoop** removes one cube's volume and puts a cube of that material on the digger's shovel
  [User, 2026-10-04]. **Smack** applies the
  interaction grid in `DESIGN.md`; on bare ground with no cube it flattens.
- **Collapse:** after digging and during earthquakes, the host finds walls steeper and taller than a
  threshold and slides that material downhill. Reinforced walls are skipped.
- One mesh and MeshCollider per chunk, rebuilt (throttled) when changed. Vertex colors for materials.

## Cubes

- Simulated by the host; clients see smoothed copies.
- A cube on a shovel is attached to the player, not simulated.
- Only moving cubes cost network traffic. Pool them.
- **Earthquake:** triggered by the host when loose cubes pass a hidden threshold. One event; the
  results are ordinary terrain changes.
- Fallback if cubes prove unworkable: a scoop goes straight onto the shovel with no loose object.

### The cube experiment (milestone 3) [User: cubes are an experiment]

Purpose: learn whether loose physics cubes work online, and their limits, before more is built on them.

- **Setup:** the test hill over a real internet connection through Relay, with 4 players and then
  as close to 8 as can be gathered.
- **Adjustable during play:** maximum loose cubes, earthquake threshold, cube weight / friction /
  bounce, how quickly a resting cube sleeps, sync rate for moving cubes, digging speed.
- **On-screen readout:** loose cubes, moving cubes, data per second per player, host frame rate.
- **Tests:**
  1. *Normal play.* Four players dig a cut for ten minutes. How many cubes pile up?
  2. *Resting pile.* Spawn 100, 200, 400, 800 cubes. Where does the host slow down?
  3. *Avalanche.* Disturb the whole pile at once. Where does it fall apart for clients?
  4. *Fling and catch.* Does catching a teammate's cube feel fair with real lag?
  5. *Earthquake.* Try different thresholds. Funny and punishing, or just annoying?
  6. *Oil split.* A failed oil cube becomes five quarter-height cubes.
- **Outcome:** keep as designed, keep with fewer and bigger cubes, or fall back. The user decides
  after playing. The earthquake threshold sits between what normal play produces and what the game
  can carry.

## Blocks, tunnels and towns (milestone 6)

- A sparse grid of static blocks aligned to the ground cells, synced as place/remove events.
- A rock cube set down becomes a block; a block scooped up becomes a rock cube again.
- No structural rules: blocks never fall and ignore earthquakes.
- **Ground above a roof:** a cell with a roof block can hold a second layer of earth on top of the
  roof (packed sand, or collapse debris). This is the one special case in the heightfield, and it is
  what makes a tunnel look like a tunnel.
- Towns are prebuilt, indestructible arrangements of blocks.

## Networking rules

- Players move themselves; everything else is decided by the host.
- Terrain: client requests, host applies and broadcasts, everyone applies the same change.
- No mid-game joining [User]. Lobby, then the host starts the job and everyone loads a fresh map
  together; after that the host refuses new connections. Keep terrain changes as a replayable list
  so late joining can be added later.

## Road check and truck

- A grid path search over road cells requiring a strip 3 cells wide [User], town to town, rerun when
  road cells change.
- Asphalt path found: spawn the truck. Painted path found: win, stop the timer.
- The truck is a host-simulated physics vehicle steering along the found path. Stuck or flipped: it
  is launched away, explodes, and a new one spawns at a town.

## Procedural animation [User: for everything]

No clips, no Animator. A damped spring, sine/noise wobble, and volume-preserving squash and stretch,
reused for blobs, shovel swings, cubes, the truck and earthquakes. Remote players run the same code
from synced state.

## Milestones

Tick these off as they are finished. Check in with the user for a playtest after each one.

- [ ] **0. Setup.** Finish `SETUP.md`: Unity MCP connected, packages added, URP on, first commit.
  - *Built 2026-10-04, awaiting the user's check.* Everything except linking a Unity Cloud project,
    which only the user can do (steps in `SETUP.md`).
- [ ] **1. Online blobs.** Lobby, host and join by code, third-person camera, animated blobs on test ground.
  - *Built 2026-10-04, awaiting the user's playtest.* Direct host/join verified with 4 editor
    instances (Multiplayer Play Mode) and 8 standalone instances. The Relay join-code path is
    written and fails cleanly without a cloud project, but has never connected: untested.
- [ ] **2. Diggable hill.** Heightfield with a hill, scoop to cube, fling and catch, set down, smack
  to pack. Debug readout.
  - *Built 2026-10-04, awaiting the user's playtest.* Ground, cubes, players and shovel loads were
    compared across instances and matched exactly. How it feels in the hand is untested: only bots
    and scripts have played it.
- [ ] **3. Cube experiment.** As described above, including earthquake and collapse. User decides
  whether cubes stay.
  - *Tooling built 2026-10-04, awaiting the user's tests.* Readout, live tuning, spawner, avalanche,
    earthquake, collapse and oil split all work and sync. Single-machine numbers are below. The
    tests that need real people on a real connection (1, 4, 5 and the feel of 6) are not done.
- [ ] **4. Road recipe.** Rock, oil and paint in the ground; the interaction grid with its fail
  noises; gravel, asphalt and paint surfaces; road check.
- [ ] **5. The Hill.** Two towns, the truck, win on paint, timer. Size the map to 20 to 30 minutes.
- [ ] **6. Reinforcement.** Blocks, reinforced walls, tunnels with ground above the roof.
- [ ] **7. Tuning.**

## How it was built (milestones 0 to 3) [Claude]

- **No networked prefabs.** Netcode for GameObjects runs in host mode over Unity Transport, but
  the game sends everything through one named message (`Net.cs`) instead of NetworkObjects and
  RPCs. That keeps the world code-first (no prefab assets), lets cubes be sent as compact batches,
  and means every byte is counted for the readout.
- **Session setup** is `Session.cs`: direct connection (port 7777) and Relay join codes. The menu's
  one text box takes either an address or a join code.
- **Ground** stores a height per grid point, 0.5 m apart. Lowering one point by 0.5 m removes
  exactly one cube's volume. The host sends the resulting heights, not the operation.
- **Cubes**: awake cubes go out in unreliable snapshots (12 bytes per cube) at the tunable sync
  rate; a cube that falls asleep gets one reliable final pose. Clients ease toward the latest pose.
- **Lobby**: a small flat map where blobs can run about. The shovel does nothing until the host
  starts the job.
- **Tuning**: `Assets/Resources/Tuning.asset`. F1 in play shows a slider for every number; the host's
  changes reach everyone live. "Save these numbers" (editor only) writes them back to the asset.
- **Test tooling**: `AutoTest.cs` (command-line flags for self-hosting, self-joining bots and state
  logging) and the "Run stress series" button. `tools/umcp.py` drives the editor over HTTP.

## Cube experiment: single-machine results (2026-10-04) [Claude]

One PC, loopback network, so there is no lag or packet loss in any of this. Default tuning.

**Stress series**: editor as host with 3 editor clients (Multiplayer Play Mode), uncapped frame rate.

| Test | Settle time | Host fps while moving | Host fps at rest | Peak data per client |
|---|---|---|---|---|
| Pile of 100 | 4.4 s | 499 | 555 | 27 kB/s |
| Pile of 200 | 5.3 s | 435 | 522 | 48 kB/s |
| Pile of 400 | 6.7 s | 449 | 520 | 96 kB/s |
| Pile of 800 | 8.2 s | 408 | 455 | 190 kB/s |
| Avalanche of 800 | one cube never slept in 60 s | 454 | 455 | 192 kB/s |

- **The host does not slow down** in this range. Worst single frames were 22 to 27 ms in every
  test including the smallest, so that is four editors sharing one machine, not the cubes.
- **Data is the limit, not physics.** It is about 0.24 kB/s per moving cube per client at 20
  snapshots a second. 800 moving cubes is 190 kB/s to each client, so about 570 kB/s of upload for
  a crew of 4 and 1.3 MB/s for 8. Resting cubes cost nothing. Halving the sync rate halves it.
- **Clients kept up**: after every test all cubes were at exactly the host's positions. While
  falling, the worst cube on a client trailed its true position by 0.25 to 0.7 m (the smoothing).
- Not checked: whether Relay limits throughput per connection. Check before trusting an
  800-cube avalanche over Relay.

**Eight players**: 8 standalone builds, all bots, 90 s of digging, flinging and smacking.

- All 8 ended with the same ground, the same 22 cubes in the same places, and the same loads.
- Cubes piled up at about 3.8 a second, so the default threshold of 150 brought an earthquake
  every 40 s (two in 90 s). Host held 60 fps (capped); peak 116 kB/s out in total to 7 clients.
- A human digging non-stop at the default 0.35 s per scoop makes 171 cubes a minute. One digger
  alone reaches 150 loose cubes in under a minute if nobody packs them. **The default threshold
  and digging speed are placeholders**; finding the real ones is the point of tests 1 and 5.

**Also verified across instances**: collapse after digging, pack, set down, oil split and
re-collecting five bits, the earthquake (merge, slump, knock-down, dropped loads), and a late
joiner being refused once the job has started.

## Decisions awaiting the user [Claude]

Choices the docs did not cover. Each is the simplest option found; confirm or change.

1. **Controls.** Left mouse: scoop with an empty shovel, fling with a loaded one. Right mouse:
   smack with an empty shovel, set down with a loaded one. WASD to move, mouse to look.
2. **A hop** (Space). Not in the verb list. Added because a blob has no other way out of a
   steep-sided hole or onto a cube.
3. ~~Digging pops the cube out loose.~~ **Decided by the user 2026-10-04: a dug cube goes straight
   onto the shovel.** Still open: nobody can catch a cube they flung themselves.
4. **Catching is automatic**: a flying cube that passes within the catch radius of an empty
   shovel lands on it. No button.
5. **At the maximum loose cubes, fling and set down do nothing** (the cube stays on the shovel).
   Only reachable if the earthquake threshold is set above the maximum.
6. **Quarter-height oil bits** are flat slabs, five to a cube. A partly filled shovel (1 to 4 bits)
   can only scoop more bits, or set them all back down.
7. **Smacking a sand cube packs it in even when it is resting on another cube**; it goes into the
   ground underneath.
8. **Earthquake sequence**: 3 s of shaking; players go down at once and drop their loads; after 1 s
   the cubes sink in as lumps; walls then slump under stricter slope limits until still. The shovel
   does nothing during the 3 s.
9. **A wall is "steep" and "tall" by two tunable numbers** (`collapseSlope`, `collapseHeight`), with
   a stricter pair for earthquakes. Walls shorter than the limit never collapse.
10. **The lobby is a small flat map** and the shovel is off there.
11. **Blobs bump into each other on the host only.** Clients walk through other blobs.
12. **Roads and blocks later** (milestones 4 and 6) will sit on ground *points*, since that is where
    heights live. Flag now if cells must be squares instead.

Not built, because nothing exists yet for it to act on: "finished road near the mess cracks and
drops a tier" (needs milestone 4), and reinforced walls being skipped by collapse (milestone 6).
