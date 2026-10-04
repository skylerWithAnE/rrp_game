# Plan: first playable of the road-building co-op prototype (rrp_game)

## Context

The user wants to build a Unity prototype with Claude: an online co-op game (3 to 4 players, up to 8,
one player hosts) where blobby solid-color characters build a road across a non-flat, deformable
desert using shovels. Digging pops out physics cubes the crew must deal with. The game is a series of
replayable high-score scenarios; the first is a 2 km "connect two towns" slice.

Design and technical direction were worked out in conversation on 2026-10-04 and are documented in
the repo: `rrp_game/CLAUDE.md`, `docs/DESIGN.md`, `docs/TECH_PLAN.md`, `docs/SETUP.md`. This plan
covers what happens once Unity finishes installing: finish tooling, then build toward the first
playable. There is no code yet; the repo holds only docs, `.gitignore` and `.gitattributes`.

Settled: road (not railroad), shovel verbs (scoop, fling, smack, scrape, pry), free routing, layered
heightfield terrain with dig-to-cube (tunnels deferred), host-simulated physics (not deterministic),
networking from the first milestone, procedural animation only, solid-color art.

Answered 2026-10-04: join code (Unity Relay) now with Steam later; all three cameras (third person,
first person, top-down), switchable; first scenario targets 20 to 30 minutes; loose cubes stay until
smacked, with a cap and an earthquake that merges resting cubes into the ground (user's idea, not
final). Milestone 1 includes the three camera modes and Relay join codes; milestone 2 includes the
earthquake.

## Step 0: finish setup (needs Unity installed)

1. Record the installed Unity 6 LTS version in `docs/SETUP.md`.
2. Create the Unity project inside `C:\Users\skyle\source\repos\rrp_game` from the command line
   (`Unity.exe -batchmode -quit -createProject`), since Hub may refuse a non-empty folder. Set up URP.
3. Add MCP for Unity via Package Manager git URL
   (`https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity`), configure it for Claude Code
   from its Unity window, restart Claude Code inside `rrp_game`, confirm with `/mcp`.
4. Add packages: Netcode for GameObjects, Unity Transport, Multiplayer Play Mode, Input System, and
   the relay/Steam package matching the user's connection choice.
5. First commit (docs + empty project).

## Milestones

Each milestone is networked from the start and ends in something two editor instances can play.

1. **Online blobs.** Bootstrap scene; host/join UI; blob player (sphere body, eyes) with
   spring-based squash/stretch/lean; camera; flat test ground; one solid-color material setup.
   - `Assets/Scripts/Core/` (Bootstrap, Tuning), `Net/` (session host/join), `Player/` (BlobController,
     BlobAnimator), `Anim/` (Spring, squash-stretch helpers).
2. **Diggable ground.** `ITerrain` interface + `LayeredHeightfield` (chunks, sand/clay/rock layers,
   vertex-color meshing, per-chunk colliders). Scoop removes a cube's volume and spawns a networked
   physics cube; smack merges a cube back. Host applies and broadcasts terrain operations; late-join sync.
   - `Terrain/`, `Shovel/`, `Cubes/` (pooled, host-simulated, live-cube cap).
3. **All five verbs.** Fling and catch, scrape (level/spread), pry (trees, boulders); per-material
   behavior table.
4. **Road tiers 1 to 2.** Dirt and gravel layers, compaction, rock → gravel; `RoadSurvey` path
   search that detects whether landmarks are connected at a given tier.
5. **Road tiers 3 to 5.** Oil → tar, chip seal, hot asphalt with cooling, paint.
6. **Scenario 1.** `Scenario` data definition; 2 km hilly desert with two towns and a midpoint,
   buried content (oil, rock, cones, etc.), truck run and score.
7. **Stress and tuning.** 8 players, cube budget, session length.

Stop for a playtest check-in with the user after milestones 1, 2 and 4.

**Superseded later on 2026-10-04:** the user made the Hill the first scenario, put session length
(20 to 30 minutes) ahead of map size, and made the cube mechanic an in-game experiment. The current
milestone order is in `rrp_game/docs/TECH_PLAN.md` section 7; use that, not the list above.

## Conventions

- Code-first: world built at runtime from scripts; minimal scene/prefab authoring.
- All numbers in one `Tuning` asset.
- Keep `docs/DESIGN.md` and `docs/TECH_PLAN.md` current as decisions change; keep [User] and
  [Claude] labels.

## Verification

- Unity compiles with no console errors (read via the Unity MCP console tool after each change).
- Multiplayer Play Mode with 2 to 4 virtual players: all see each other move and animate; a dig on
  one client changes terrain on all; cubes match across clients; a late joiner sees prior digs.
- Milestone 4+: build a short road between two test landmarks and confirm `RoadSurvey` reports
  connected only when the tier requirements are met.
- Before milestone 7: a real two-machine online session through the chosen connection method.
