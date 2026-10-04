# rrp_game

Online co-op Unity prototype: a crew of solid-color blobs builds a road between two towns across a
hilly desert, and the only tool is the shovel. Dug cubes go onto the shovel and become loose physics
cubes when flung or set down; too many loose cubes brings a punishing earthquake. Scenarios are 20 to 30 minute jobs chased
for a high score. The first is the Hill.

## Start here

1. Read `docs/DESIGN.md` (what the game is), `docs/TECH_PLAN.md` (how to build it, and the
   milestone checklist) and `docs/SETUP.md` (tooling state and known problems).
2. Find the first unticked milestone in `docs/TECH_PLAN.md` and work on that. Tick it when the user
   has playtested and accepted it.

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
