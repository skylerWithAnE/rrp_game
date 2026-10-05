# rrp_game

Online co-op Unity prototype, first person: a crew of solid-color blobs builds a road that a box
truck can drive. They set a survey line, bring the ground to it, lay and compact gravel, and a truck
drives the result. It is a mechanics prototype made of five stations that together take about 30
minutes to try. There is no job, map, score or clock yet.

## Start here

**The game was redesigned on 2026-10-04.** A first prototype (blobs building a road out of physics
cubes with a shovel, third person) was built through milestone 6 and the user did not want it. Its
code is still in the repository. Do not continue its milestone list.

1. `docs/DESIGN.md` is the plan: the user's decisions, sizes in metres, the five stations and the
   first build step. Build one station at a time and have the user play it by hand before starting
   the next.
2. `docs/POSTMORTEM.md` says what the first prototype was, why it missed, and what is reusable.
   Read it before reusing or deleting old code.
3. `docs/TECH_PLAN.md` describes the first prototype as built. Treat it as a record, not a plan.
   `docs/SETUP.md` is still current for tooling.

**Do not read `docs/archive/` unless the user asks you to.** It holds superseded planning documents
full of ideas that were cut or deferred. Nothing in it is current, and it must not be used as a
source of features or requirements.

## Ground rules from the user

- **Fight scope creep.** Do not add features, systems or options that are not in `DESIGN.md`. If
  something seems missing, ask. Do not add a feature to solve a design problem.
- It is a prototype: fast, simple and weird beats correct and polished.
- First person. Several tools, each with one job. Throwing cubes around is out.
- Solid-color shaders, no textures. Blobby solid-color characters, 1.6 m tall. Look and fidelity
  are parked.
- All animation is procedural. No animation clips, no Animator controllers.
- Multiplayer is the point. Build and verify every system networked from the start.
- Every size is chosen in metres against the 1.6 m blob and is not settled until the user has stood
  next to it. Make the numbers adjustable during play and check in with the user after each station.

## Working conventions

- Code-first: generate the world from scripts at runtime rather than hand-authoring scenes and prefabs.
- Every gameplay number lives in one tuning asset.
- When the user decides something, update the docs. Keep the user's decisions distinct from
  Claude's proposals.
- Before committing to a rule, say what follows from it, especially for sizes. Ask open questions,
  not multiple-choice ones that steer toward small fixes.
