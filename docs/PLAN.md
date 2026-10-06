# Plan

Rewritten 2026-10-05, at the end of the design session that followed the build of the maps and
test grounds. The plan this replaces, which set that session's agenda, is in git history (commit
`2f3f9b9`). What the session decided is in `DESIGN.md` under "What the design session decided".

## Where the project is going

The user's long-term goals are quoted in full in `DESIGN.md` under "Where it is going", with the
design session's changes after them. In one line: **a crew on another planet, in low gravity,
connects cities with roads; dirt and gravel fall apart under traffic and paved road does not;
money from travellers comes later.**

## Where it stands

- Everything asked for before the design session is built and passes a two-instance check. The
  user has played every mechanic in its isolated setting, and the Short and Middle maps. The
  Long and Climb maps were too much work to build by hand.
- Nothing decided in the design session is built.

## Next: an overnight build of six slices

**"Next build should focus again on small isolated slices"** (user, 2026-10-05). It is to run
unattended, in a new chat, from a prompt the user will ask for. **Latitude: "Give it wide
latitude. Same as before"** (user, 2026-10-05): solve problems creatively and add a system if it
streamlines the play, without asking; write down each addition as Claude's, with the reason, and
make it something that can be switched off.

The slices and their order are Claude's proposal. The user has seen the list and has not
objected to it or confirmed it.

| # | Slice | What is built | The one question |
|---|---|---|---|
| 1 | Gravity | A slider for gravity, for trucks, vehicles and blobs, everywhere. The climbs, the coast and the tightest turn measured again by script at Earth, Mars (0.38) and the Moon (0.16). Starts at Mars | How low is fun? |
| 2 | Wear | A dirt road and a packed gravel road under steady traffic. Wear on bare ground. Damage scales up past the threshold. A slider between damage by time on a square and damage by how hard a truck lands. No wear on paved | Does a road fall apart at the pace the user described, and is it good to watch? |
| 3 | Spin-out | A loose gravel stretch with a bend. Sideways grip that depends on the surface. Truck packing on its slider | Is loose gravel dangerous for long enough to matter? |
| 4 | Gravel delivery | On the Quarry ground: a survey tool that places a drop-off; a truck that runs its own round, backs in, waits to be unloaded and goes home; a road for everyone that it shares with travellers; the truck destroyed on a bad road and replaced; one shovel covers ten tiles, on a slider | Is bringing gravel over a road you built a job worth doing? |
| 5 | Hand painting | Its own scene: a strip with the lines marked and one without; a roller brush that leaves a stripe; the paint truck spraying where it is; tar spray and a grinder to remove paint; the judging on the F3 readout only | Is drawing a line by hand hard in a good way? |
| 6 | Long and Climb finished | A button that stakes and finishes a map's road, so the user can watch trucks on it with gravity and wear running | Is a long road worth having? |

Why this order: every other slice behaves differently under low gravity, so it is first. Spin-out
and the delivery truck's punishment both depend on wear, so it is second. Paint depends on
nothing. The demos are last so that they show the rest.

The pace the user gave for wear, to check slice 2 against: dirt has a square destroyed by the
fifth truck and is a rut by the fiftieth; gravel has several squares damaged by 50, several very
deep holes by 250, and is unusable by 500.

### Risks to say out loud

- **Gravity cannot be played before the rest is built on it.** The build picks a starting value
  unplayed. Every slice must work across the slider's whole range, not only at the start value.
- **The tightest turn may not be driveable in low gravity.** If so, report it; do not quietly
  widen the turn the user chose.
- **The truck's physics may have no sideways grip to lose.** Slice 3 may need it added.
- **Reversing is new.** No truck can back up or turn round today.
- **Not in this build**: money, a large map, signage, hot asphalt, vehicle health, driver
  recklessness, vehicles hitting players, repaving over paint, wear on paved road, theming.

## After that

The user plays the slices. Nothing further is planned until then.

## Standing rules for building

- Build and confirm each mechanic on a station first. Verify on two instances. Commit when the
  user accepts a step, or when it passes two instances if the user is away. Never push.
- A scripted check of a rule is not a check of the control: stand the player there and read what
  the screen says. See `NOTES.md`.
- Play before building more.
- Compiling stops the user's game. Check the editor's state before every compile.
- What the user has played is what the user says, not what a note from a session guessed.
