# Technical plan

Last updated: 2026-10-04. Written before the Unity project existed.

**Status: Claude's proposals unless marked [User].** Re-check package names and versions against
the installed Unity editor before relying on them.

## 1. Engine and project

- Unity 6 LTS (exact version to be recorded in `docs/SETUP.md`), URP, 3D, new Input System.
- Code-first: one bootstrap scene, world generated at runtime from scripts.

## 2. Terrain

### The user's questions and Claude's answers (2026-10-04)

**"Deterministic physics? Is that a good solution if the world is entirely voxel based?"**
No, not for this project.

- Unity's built-in physics (PhysX) does not produce identical results on different machines, so
  "everyone simulates the same cubes and stays in sync" does not hold.
- A voxel world does not help. The grid is easy to keep identical; tumbling rigid bodies are the hard part.
- Engines built for deterministic physics exist (Unity's DOTS physics on matching hardware, Photon
  Quantum), but they mean a different architecture and networking model for the whole game. Too much
  for a prototype, and awkward with players joining mid-game.
- **What to do instead:** the host simulates the cubes and sends their positions; clients smooth
  them. That is the normal approach and matches "one player hosts." The real constraint is the
  number of moving cubes at once, handled by the budget rules below.

**"Are we going to struggle making a good looking believable uphill road in a voxel world?"**
With blocky voxels, yes: a slope becomes a staircase. Smooth voxels (the Astroneer / Deep Rock
style) can do slopes, but they are poor at **thin layers**. A road is thin layers: a few centimetres
of gravel, asphalt, then paint. A heightfield stores those exactly and gives a smooth surface for a
truck to drive on. So the user's instinct to go back to heightfields is right for a road game.

**"We are going to give up tunnels with heightfields?"**
True tunnels, yes; a heightfield has one height per spot. Deep open cuts (a notch through a hill)
work fine. For later, there are two known ways to add tunnels without redoing the ground:

1. **Voxel volumes on top of the heightfield** — the user's own idea. Specific features (a mesa, a
   mountain, a building) are separate diggable voxel bodies sitting on the heightfield. Tunnels go
   through those. This is workable and is the path Claude would take.
2. **Holes plus tunnel pieces** — punch a hole in the heightfield and place a tunnel structure in it.
   Less free-form, simpler.

Neither needs deciding now. What matters now is that the rest of the game talks to terrain through a
small interface (remove a bite here, add material there, what is at this spot), so a second kind of
terrain can be added behind it later.

### Proposal

- **Layered heightfield**, chunked (about 32 m chunks, about 0.5 m cells, both tunable).
- Per cell: the height of each **ground layer** (rock, clay, sand) and thin **road layers** on top
  (gravel, tar/chip seal, asphalt, paint), plus a compaction value.
- **Dig = cube.** A scoop removes roughly one cube's volume from the top layer and spawns a physics
  cube of that material. Smacking a loose cube merges its volume back into the heightfield at that
  spot. Material is conserved.
- **Verbs as terrain operations:** scoop (remove volume), merge (add volume), scrape (smooth/level
  within a radius, moving material rather than deleting it), smack (compact; also triggers merges and
  rock breaking).
- Collision: one MeshCollider per chunk, rebuilt (throttled) when dirty.
- **Collapse [User requirement]:** tunnelling attempts must cave in. A stability check runs on
  recently dug areas and during earthquakes: where a wall is steeper than a material's safe angle
  and taller than a threshold, the host moves material downhill (a landslide as a batch of terrain
  operations) and throws out some cubes. Sand gives way easily, clay less, rock least.
- **Deterministic physics is dropped** [User]. Cubes are host-simulated.

## 3. Multiplayer

- **[User] Online, peer-to-peer, one player hosts. No dedicated server. Design for 3 to 4, support 8.**
- **[User] Multiplayer is the focus.** Claude reads this as: networking goes in at the first
  milestone and every system is built networked.
- **Netcode for GameObjects (NGO)** in host mode with Unity Transport. Works with **Multiplayer Play
  Mode** for testing several players in one editor.
- **[User] Join code now, Steam later.** Use **Unity Relay** (host gets a join code; needs a free
  Unity Cloud project linked to the Unity project). Keep session setup behind one small class so
  Steam networking can replace it later.
- **[User] Three cameras:** third person, first person, top-down/isometric, switchable. Build the
  camera as swappable modes over one rig; the blob's own body is hidden in first person.
- No host migration: if the host leaves, the session ends.
- Alternatives if NGO gets in the way: FishNet, Mirror, Photon Fusion.

### Who decides what

- **Player movement:** each player's own machine (responsive; co-op, so cheating is not a concern).
- **Terrain:** the host. Clients request an operation; the host applies it and broadcasts it;
  everyone applies the same operation. Late joiners receive the changed chunks.
- **Cubes and other loose objects:** simulated by the host. A cube on a shovel is not simulated at
  all; it is attached to the player. Catching and flinging hand it back to the host's physics.

### Cube budget (the thing to be careful about)

**[User] This is an experiment.** "We need to experiment in game to decide good caps though, this
might not really be a feasible mechanic." So the cube system ships early with a debug overlay (live
cubes, moving cubes, bytes per second, host frame time), a cube spawner for stress tests, and every
limit adjustable while playing. Test with the full 8 players before building more on top of it. If
it fails, the fallback is that a scoop goes straight onto the shovel as a load with no loose object.

- Only **moving** cubes cost network traffic; resting cubes send nothing.
- A hard cap on live cubes (start around 150 to 200; measure with 8 players).
- A way for cubes to stop being objects: smacked into the ground by players, plus the **earthquake**
  [User's idea]: when the live count nears the cap, the host triggers a quake and every resting cube
  merges into the heightfield where it sits. That is one network event followed by ordinary terrain
  operations, which are exact on every machine.
- **[User asked] Could cubes be simulated locally on each machine with deterministic physics to cut
  traffic?** Not reliably. The physics will not match across machines, and cubes are gameplay
  objects (picked up, smacked into the ground), so two players disagreeing about where a cube is
  would break things. It is also premature, as the user suspected: resting cubes send nothing, and
  a few dozen moving cubes is a small amount of data. If traffic ever becomes a problem, the fallback
  is to make only cosmetic debris (crumbs, dust) local and keep real cubes host-simulated.
- Pooled objects, compact position/rotation encoding, lower send rate for distant cubes.
- Bulk materials that are not worth a rigid body (flung gravel spray, dripping tar, paint) are
  visual effects plus a terrain operation where they land, not physics objects.

## 4. Game systems

- `Terrain` interface + `LayeredHeightfield` implementation (chunks, layers, operations, meshing).
- `Shovel` — the five verbs, load on the blade, catch.
- `Cube` — material, physics, merge, break (rock → gravel).
- `Materials` — per-material behavior table (weight, stickiness, cooling, how it reacts to each verb).
- `Diggables` — trees, boulders, buried objects.
- `RoadSurvey` — reads the heightfield to work out where road exists, its tier, and whether
  landmarks are connected (free routing means the game must discover the road, not prescribe it).
- `Scenario` — data definition: terrain, landmarks, required tiers, buried content, rules, scoring.
- `Truck` — drives the surveyed route; ride quality feeds the score.
- `Tuning` — one place for every number.

`RoadSurvey` is the least obvious piece: with free routing, "are the towns connected by a gravel
road?" is a path search over terrain cells that meet a tier's requirements (width, smoothness,
slope, layer depth).

## 5. Procedural animation ("for everything") [User requirement]

No animation clips, no Animator. Built from a few reusable pieces: a damped spring, sine/noise
oscillators, and volume-preserving squash and stretch.

- **Blobs:** squash on landing, stretch on jumps and acceleration, lean into movement, bob with
  speed, jiggle when stopping. Eyes look where they are going. Body sags under a heavy load.
- **Shovel:** each verb is a spring-driven pose sequence (wind-up, strike, recoil) with body squash
  on impact.
- **Cubes:** pop out with overshoot, wobble when carried, splat when smacked into the ground.
- **Trees and diggables:** sway, shake harder with each pry, topple with overshoot.
- **Materials:** tar strings and drips, asphalt steams and stiffens, paint dribbles.
- Remote players run the same animation code from replicated state, so animation costs no bandwidth.

## 6. Rendering

- One solid-color URP material setup; color per object or per vertex (terrain layers are vertex
  colors). Simple lighting and shadows so shapes read. No textures.
- Meshes are Unity primitives or generated in code.

## 7. Milestones

1. **Online blobs** — host/join, synced blob players with procedural animation, camera, flat test ground.
2. **Diggable hill** — layered heightfield with a test hill, scoop and smack, cubes, fling and
   catch, all networked. Debug overlay and stress spawner.
3. **Cube experiment** — find workable caps with up to 8 players; earthquake and collapse. Decide
   with the user whether the cube mechanic stays.
4. **Remaining verbs and tiers 1 and 2** — scrape, pry; dirt and gravel, rock breaking,
   `RoadSurvey` connection check.
5. **The Hill, first playable** — two towns, the hill, around/over/through all possible, score,
   sized to 20 to 30 minutes.
6. **Road tiers 3 to 5** — oil to tar, chip seal, hot asphalt with cooling, paint.
7. **Tuning** — session length, earthquake punishment, route balance.
