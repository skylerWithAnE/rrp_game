# Plan

Rewritten 2026-10-05, at the end of the session that built the maps and the test grounds. The
plan this replaces, for the unattended push, is in git history (commit `d7cf145`). What was done
in that session is `SESSION_2026-10-05.md`.

## Where the project is going

The user set out the long-term goals on 2026-10-05. They are quoted in full in `DESIGN.md` under
"Where it is going". In one line: **a large map with several cities to connect; players start
with a shovel and a dirt road, earn money from each traveller, spend it on gravel, asphalt and
paint, and every tier of road has hazards that the next tier removes.**

These are goals, not the next thing to build. The user has not said what to build next.

## Where it stands

- Everything asked for so far is built and passes a two-instance check. Most of it has not been
  played: the table "What has been played" in `DESIGN.md` is the measure.
- The mechanics exist as separate test grounds and five small maps. None of the later mechanics
  (junctions, gravel from a quarry, paving, vehicles) is on a map.
- Several things as built now disagree with the goals. They are listed in `DESIGN.md` under "What
  the goals change", and are the natural agenda for the next session.

## Next: a design session

The user asked for a prompt to start one; it is `NEXT_SESSION_PROMPT.md`. It is for deciding, not
for building. What it should settle, in the order that unblocks the most:

1. **The tiers of road and their hazards.** Dirt, gravel, asphalt, painted: what goes wrong on
   each, what a traveller is worth on each, and how fragile each is. This answers the two
   questions that have been open longest: what gravel is for, and what paving is for.
2. **Money.** What a traveller pays, when, and what gravel, asphalt and paint cost. Whether the
   first road can pay for the second.
3. **The map.** How many cities, how far apart, what "low volume traffic" is, and whether the
   ground that was built for 300 m maps will do.
4. **Automated and driven.** How one vehicle is both, and who decides which it is at a moment.
5. **Hand-drawn paint.** What the roller brush and the paint truck draw, and how a drawn line is
   judged against where a line should be.
6. **Which of today's test grounds graduate**, and which mechanics are redone to fit the goals.

How to run it is the user's standing rule: questions are answered by playing, so each answer
worth testing becomes a station, and where there are two good answers, one station each.

## After that

Not planned. It depends on what the design session decides. The likeliest first build is a
station for the road tiers and their hazards, because everything else (money, the reason to
upgrade, the reason to pave) hangs on it. That is Claude's guess, not a decision.

## Standing rules for building

- Build and confirm each mechanic on a station first. Verify on two instances. Commit when the
  user accepts a step, or when it passes two instances if the user is away. Never push.
- A scripted check of a rule is not a check of the control: stand the player there and read what
  the screen says. See `NOTES.md`.
- Play before building more. On 2026-10-05 about ten systems were stacked on three that had been
  played, and the one fault the user found came from that.
- Compiling stops the user's game. Check the editor's state before every compile.
