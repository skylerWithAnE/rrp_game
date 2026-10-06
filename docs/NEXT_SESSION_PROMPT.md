# Prompt for the next session

Written by Claude on 2026-10-05, at the end of the design session, for the user to paste and to
change first if it is wrong. It starts the overnight build. The prompt this replaces started the
design session and is in git history (commit `2f3f9b9`).

---

This is an overnight build session for rrp_game. I am asleep and will not answer. Work through
the whole list without stopping to ask me anything, and leave me a report to read in the morning.

Start by reading CLAUDE.md, then docs/DESIGN.md in full, then docs/PLAN.md, docs/NOTES.md in
full, docs/TECH_PLAN.md and docs/SETUP.md. Don't read docs/archive/.

What we decided is in DESIGN.md under "What the design session decided". Anything there marked
[Claude] is a default I have not confirmed: build it, put it on a slider or a switch, and list
it in the report so I can say yes or no.

You have wide latitude, the same as the last overnight push. Solve problems creatively, and add
a system if it would make the play better, without asking. Write down each addition as yours,
with the reason, and make it something I can switch off. The latitude ends with this build.

Build these six slices, in this order. Each is small and isolated, and each exists to answer one
question when I play it.

1. Gravity. One slider for gravity that applies to trucks, driven vehicles and blobs, on every
   ground and map. Start it at Mars (0.38 of Earth), running from Earth down to the Moon (0.16).
   Then measure again by script, at Earth, Mars and the Moon: what a truck climbs on packed
   gravel, loose gravel and bare ground; how far it coasts up a slope; and whether it gets round
   my tightest turn. Put the numbers in DESIGN.md. If the tightest turn or the 15 degree slope
   limit no longer works, tell me; do not change either yourself.
   Question: how low is fun?

2. Wear. A ground with a dirt road and a packed gravel road under steady traffic. Wear works on
   bare ground as well as gravel. Once a square passes the threshold, damage to it scales up.
   Add a slider between damage by time on a square and damage by how hard a truck lands. Paved
   road does not wear. Tune toward the pace I gave: dirt has a square destroyed by the fifth
   truck and is a rut by the fiftieth; gravel has several squares damaged by 50 trucks, several
   very deep holes by 250, and is unusable by 500. Count the trucks on the readout. Tell me what
   the pace actually came out as.
   Question: does a road fall apart at that pace, and is it good to watch?

3. Spin-out. A loose gravel stretch with a bend in it. Loose gravel should make trucks spin out.
   If the truck has no sideways grip to lose, add it. Trucks still pack the gravel they drive
   over; keep that on its slider and find a setting where loose gravel stays dangerous for more
   than one truck.
   Question: is loose gravel dangerous for long enough to matter?

4. Gravel delivery, on the Quarry ground. A new survey tool places a gravel drop-off, which has
   to be joined to the quarry by road; a road for everyone will do, it need not be a service
   road. The gravel truck is its own vehicle and I cannot get in it. I load it at the quarry as
   now. It drives to the drop-off, backs into the spot, waits while I unload it as now, and
   drives itself home. Put travellers on the same road. If the truck is wrecked on the way it is
   destroyed with its load, and an empty one appears at the quarry after a delay. One shovel of
   gravel covers ten tiles, on a slider. Leave the stuck rule alone: I want to see what happens
   to travellers behind a truck that is backing in.
   Question: is bringing gravel over a road I built a job worth doing?

5. Hand painting, in its own scene. Rolled asphalt with two strips: one with the place for each
   line marked, one with nothing marked. A roller brush that leaves a continuous stripe where I
   drag it, white and yellow. The paint truck, spraying where it actually is. Two ways to remove
   paint: a tar spray that is wide and imprecise, and a grinder that is narrow and exact. Judge
   each square: points for paint where a line should be (the two edges and the centre), points
   off for paint anywhere else. Show that number on the F3 readout and nowhere else; the player
   never sees it. Put the width of the wanted band, the width of the roller, and broken or solid
   centre line on sliders. Leave the square paint tool on the Paving ground as it is.
   Question: is drawing a line by hand hard in a good way?

6. The Long and Climb maps, finished. A button for the host that stakes and finishes the road on
   a map, so I can watch trucks drive it with gravity and wear running. Wear on a map is a
   switch.
   Question: is a long road worth having?

Rules for the night:

- Do the slices in order. If one is blocked, write down exactly where and why, leave what works
  switched on, and go on to the next. Do not spend the night on one.
- Every slice has to work across the whole gravity slider, not only at Mars.
- With gravity at Earth and wear off, everything I have already played should behave as it did.
- Build and check every slice networked, on two instances. A scripted check of a rule is not a
  check of the control: stand a player there with each tool in hand and read what the screen
  says. Look at a screenshot of anything that moves or swings before calling it done.
- Close test clients with CloseMainWindow, never by killing the process.
- Every number goes in Tuning.cs so it is a slider.
- Keep the explosions and the jank. Do not smooth anything out.
- Commit each slice once it works on two instances. Never push.
- Not tonight: money, a large map, signage, hot asphalt, vehicle health, driver recklessness,
  vehicles hitting players, repaving over paint, wear on paved road, theming the planet, NPC
  workers.

Before you stop:

- Update DESIGN.md, PLAN.md, TECH_PLAN.md, NOTES.md and CLAUDE.md in place, keeping my
  decisions apart from what you chose.
- Write docs/SESSION_<date>.md: what was built, what was checked and how, what was not checked,
  what went wrong, and every choice that was yours.
- At the top of it, give me a play list: the slices in the order I should play them, where to
  find each, which keys, the one question each answers, and a screenshot of each from the blob's
  eyes.
- Leave the editor out of play mode with the Windows player built.

Then stop.
