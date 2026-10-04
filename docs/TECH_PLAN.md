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
- **Scoop** removes one cube's volume and spawns a cube of that material. **Smack** applies the
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
- [ ] **1. Online blobs.** Lobby, host and join by code, third-person camera, animated blobs on test ground.
- [ ] **2. Diggable hill.** Heightfield with a hill, scoop to cube, fling and catch, set down, smack
  to pack. Debug readout.
- [ ] **3. Cube experiment.** As described above, including earthquake and collapse. User decides
  whether cubes stay.
- [ ] **4. Road recipe.** Rock, oil and paint in the ground; the interaction grid with its fail
  noises; gravel, asphalt and paint surfaces; road check.
- [ ] **5. The Hill.** Two towns, the truck, win on paint, timer. Size the map to 20 to 30 minutes.
- [ ] **6. Reinforcement.** Blocks, reinforced walls, tunnels with ground above the roof.
- [ ] **7. Tuning.**
