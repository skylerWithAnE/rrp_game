# Game design

Final trim 2026-10-04. Everything here was decided by the user unless marked **[Claude]**. The user
wants scope creep resisted: do not add anything that is not on this page.

## The game

An online co-op crew game. Solid-color blobs build a road between two towns across a desert that is
not flat. **The only tool is the shovel.** Digging puts a cube on the digger's shovel; flung or set down, it
becomes a loose physics cube that the crew has to deal with. It should be weird. A scenario is a replayable 20 to 30 minute job, chased for a high score.

The crew the user pictures: one player digging, another catching the dug-up cubes and carrying them
off to pack into the ground elsewhere, others digging up material for the next phase.

## Prototype rules

- **Players:** build and test for 4. Do not block 8.
- **Joining:** everyone joins before the job starts; no joining mid-game. If needed, use a starting
  lobby, then move everyone to a fresh map together.
- **Camera:** third person.
- **Verbs:** scoop, fling (teammates can catch on their shovel), smack (also flattens bare ground),
  and setting a cube down.
- **Everything dug up is a cube.** No liquids, sprays or drips.
- **Everything the shovel touches is big** (decided 2026-10-04 after the second playtest: "everything
  needs to be bigger"). **[Claude]** read that as doubling the ground grid to 1 m, so a cube is now
  as tall as a blob, not half a blob as first written. Confirm or correct.
- **A dug cube goes straight onto the digger's shovel** (decided 2026-10-04 after the first
  playtest; it used to pop out loose). It becomes a loose cube when flung or set down.
- **Ground:** sand on top, rock underneath, with buried oil pockets and brightly colored paint pockets.
- **A road** is a connected strip at least **3 cubes wide**. Players route it wherever they like.
- **Goal:** painted asphalt connecting the two towns.
- **Score:** time to finish. Nothing else yet.
- **Session:** 20 to 30 minutes for a crew of 3 to 4. This outranks map size; size the map to fit.
- **Art:** solid-color shaders, no textures. Blobby solid-color characters. All animation procedural.

## Building the road

Smack a loose cube that is resting on a surface:

| Cube | Bare ground | Gravel | Asphalt | Painted asphalt |
|---|---|---|---|---|
| Sand | Packs in, raising the ground | Rough noise | Rough noise | Rough noise |
| Rock | Gravel if the ground is flat, otherwise thud | Thud | Thud | Thud |
| Oil | Squeak, splits (see below) | Asphalt | Squeak, splits | Squeak, splits |
| Paint | Paints it (cosmetic) | Paints it (cosmetic) | Painted road (counts) | Repaints it |

- A noise means the smack failed: nothing is built and the cube stays loose. Each material has its
  own fail noise.
- **Paint never fails**; it colors whatever it lands on. Only paint on asphalt counts toward the goal.
- **A failed oil cube spreads out into five quarter-height cubes.** All five must be scooped up to
  make one oil cube again. (To be tried in the cube experiment.)
- **Setting a rock cube down** makes it a block (see Reinforcement). Smacking only works on a loose
  rock cube; a placed block must be scooped up before it can be smashed into gravel. (Kept as is
  on 2026-10-04; the game now shows on screen what each button will do, and blocks and loose rock
  are different colors.)

## The truck

As soon as the towns are connected by asphalt (before paint), a truck starts driving back and forth
between them. Players do not drive it. It is a physics vehicle, so it bounces on bad road. If it
gets stuck or flips, it bounces away and blows up, and a new one sets off from a town.

## Earthquakes

Loose cubes stay in the world. When too many pile up, an earthquake hits. It is a **mostly hidden
mechanic with no meter**, and it is meant to be punishing:

- every loose cube merges into the ground as a lump where it sits,
- unreinforced steep cut walls collapse,
- finished road near the mess cracks and drops a tier,
- players are knocked over and drop their loads.

**Cubes and earthquakes are an experiment.** The limits must be found by playing, and the user
accepts the cube mechanic may prove unworkable. See the cube experiment in `TECH_PLAN.md`.

## Collapse, reinforcement and tunnels

- Cut walls that are too steep and too tall **collapse**. Cave-ins punish what was just dug, never
  what was reinforced.
- The ground is a heightfield. **Blocks** are a separate thing placed on the same grid as the ground
  cells. Only rock cubes become blocks. Blocks stack, a roof block can hang off the side of another
  block with nothing under it, and a block can be scooped back up. Blocks are permanent: earthquakes
  do not move them.
- A wall is **reinforced** when blocks cover its full height.
- **A tunnel is two block walls and a roof.** Players dig a trench from above, build a truck-height
  corridor at the bottom, and earth ends up on top of the roof again, either packed there by players
  or dropped there when the unreinforced sides cave in. The truck drives through it.
- **Towns** are block structures and are indestructible.
- Built **after** the cube experiment.

## Scenario 1: the Hill

Two towns with a large hill between them. Hard-coded; no scenario system until a second scenario exists.

- **Around** is heavily encouraged: longest, but gentle ground.
- **Over** is possible but harder: too steep without cutting a ramp or switchbacks.
- **Through** is possible but harder: a lot of ground to move, a lot of cubes, collapse and
  earthquake risk, and a tunnel to build.

## Later (wanted, not in the prototype)

Joining mid-game. First-person and top-down cameras. Scrape and pry verbs. Wheeled shovel (the only
acceptable cart). Tuning for 8 players. More scenarios (a route through a busy part of town; a town
to a bus stop; two towns to a bus stop; adding a train station). Payment for road usage. Steam
invites. Richer scoring. More things to dig up, trees included.

## Cut

Chip seal. Hot asphalt (mixing, cooling, relays). Clay layer. Rumble meter. Deterministic physics.
Diggable voxel terrain. The railroad (the original idea).
