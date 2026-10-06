# Plan

Rewritten 2026-10-06, at the end of the overnight build of the six slices. The plan this
replaces, which set that build, is in git history (commit `71fcc8b`). What the build made is in
`DESIGN.md` under "What the build of 2026-10-06 made of it", and how it went is in
`SESSION_2026-10-06.md`.

## Where the project is going

The user's long-term goals are quoted in full in `DESIGN.md` under "Where it is going", with the
design session's changes after them. In one line: **a crew on another planet, in low gravity,
connects cities with roads; dirt and gravel fall apart under traffic and paved road does not;
money from travellers comes later.**

## Where it stands

- **The six slices are built, each checked on two instances and committed.** Gravity, wear,
  spin-out, gravel delivery, painting by hand, and a road at a button on every map.
- **The user has not played them**, bar about five minutes of gravity and wear during the build.
- Each slice exists to answer one question, and the questions are open. They are at the top of
  `SESSION_2026-10-06.md` as a play list, and in `DESIGN.md` under "Open".
- **The latitude given for that build is over** (user, 2026-10-05: it "ends with that build").
  The older rule stands again: nothing that is not in `DESIGN.md` or here, without asking.

## Next

**The user plays the slices.** Nothing further is planned until then, by the standing rule: play
before building more.

What the user will be asked to decide, once they have played:

| | The decision |
|---|---|
| Gravity | How low. Whether wheels should bite by the gravity and springs soften with it (Claude's two switches, both on). What to do about trucks leaving their lane in the tightest turn |
| Wear | Whether the pace is right, by time or by landing, and whether a rut should wreck trucks |
| Spin-out | Whether loose gravel is dangerous for long enough, and whether trucks taking 50 passes to pack gravel is right on a map |
| Delivery | Whether the round is a job worth having; what should happen to travellers behind the truck; whether the drop-off as a spur from a road is the right shape; whether wear belongs on the Quarry ground |
| Painting | Marked or unmarked; tar or grinder; whether the judging is fair; how the paint truck's nozzles should work |
| Long roads | Whether a long road is worth having, which decides the size of the map |
| Still open from before | Which test grounds are finished with; curved sections; the stuck rule; the hot-spot row |

## Risks to say out loud

- **There is now a lot the user has not played**: six slices on top of each other's sliders.
  Gravity changes every other slice. If the user's answer to "how low is fun" is far from Mars,
  the pace of wear and the spin-out rates in `DESIGN.md` were measured at the wrong gravity.
- **Three things changed that the user had played**, each for a slice and each with a way back:
  loose gravel now spins trucks everywhere (the two gravel ramps on the Trucks ground, any map);
  trucks pack gravel at 0.02 a time, not 0.25; and the quarry's truck is no longer sent to a
  fixed drop but runs its round to a drop-off the player places.
- **Not in any build yet**: money, a large map, signage, hot asphalt, vehicle health, driver
  recklessness, vehicles hitting players, repaving over paint, wear on paved road, theming.

## Standing rules for building

- Build and confirm each mechanic on a station first. Verify on two instances. Commit when the
  user accepts a step, or when it passes two instances if the user is away. Never push.
- A scripted check of a rule is not a check of the control: stand the player there and read what
  the screen says. Mouse clicks can be scripted: see `NOTES.md`.
- Play before building more.
- Compiling stops the user's game. Check the editor's state before every compile, and do not
  change a script while someone is in play mode, even in a build that is meant to be unattended.
- What the user has played is what the user says, not what a note from a session guessed.
