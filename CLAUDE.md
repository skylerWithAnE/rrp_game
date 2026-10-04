# rrp_game

Online co-op Unity prototype: a crew of solid-color blobs builds a road between two towns across a
hilly desert, and the only tool is the shovel. Dug cubes go onto the shovel and become loose physics
cubes when flung or set down; too many loose cubes brings a punishing earthquake. Scenarios are 20 to 30 minute jobs chased
for a high score. The first is the Hill.

## Start here

**The game is being redesigned (2026-10-04).** The first prototype was built through milestone 6
and the user does not want it as it stands. Do not continue the milestone list.

1. Read `docs/POSTMORTEM.md` first: what was built, why it missed, and the questions the redesign
   has to answer.
2. `docs/DESIGN.md` and `docs/TECH_PLAN.md` describe the prototype as built. Treat them as a record,
   not a plan. `docs/SETUP.md` is still current for tooling.
3. Decided by the user for the redesign: **the "only tool is the shovel" rule is gone; the game is
   first person; throwing cubes around is out of favour.** The description above and several ground
   rules below predate that and are up for review with the user.

**Do not read `docs/archive/` unless the user asks you to.** It holds superseded planning documents
full of ideas that were cut or deferred. Nothing in it is current, and it must not be used as a
source of features or requirements.

## Ground rules from the user

- **Fight scope creep.** The design was deliberately cut down. Do not add features, systems or
  options that are not in `DESIGN.md`. If something seems missing, ask.
- It is a prototype: fast, simple and weird beats correct and polished.
- Solid-color shaders, no textures. Blobby solid-color characters.
- All animation is procedural. No animation clips, no Animator controllers.
- Multiplayer is the point. Build every system networked from the start.
- Cubes and earthquakes are experiments to be judged by playing. Make their numbers adjustable
  during play and check in with the user after each milestone.

## Working conventions

- Code-first: generate the world from scripts at runtime rather than hand-authoring scenes and prefabs.
- Every gameplay number lives in one tuning asset.
- When the user decides something, update the docs. Keep the user's decisions distinct from
  Claude's proposals.
