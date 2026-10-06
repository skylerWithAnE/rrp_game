# Playtest notes

Notes Claude took while watching the user play, read from the running game every 10 seconds.
What the user said is quoted. Everything else is what the game's state showed.

## 2026-10-05, morning: the Short map, alone

**What the user said afterwards:** "The short playtest went great. The game is looking much more
like what I envisioned. Using stations to build, test and confirm each mechanic seems like it
worked out really well. The build session was a big success. I have not played the medium
playtest but it looks promising."

What the state showed, from 09:49 when watching began to 09:54:

- The user hosted alone, on the Short map, with every slider at its default, so both of Claude's
  systems (trucks packing gravel, the hot spot working its row) were on throughout.
- When watching began the road was already staked (6 stakes, 60 m), the towns joined, and the
  road 100 % level after about 450 clicks. The user was finishing the shoulders.
- **The whole road was finished**: shoulders to 100 %, then gravel from 0 to 99 % in about three
  and a half minutes, 1,128 clicks in all. The user graded the shoulders, which are optional.
- **Packing kept pace with laying.** Packed stayed two to five points behind gravel the whole
  way (21/19, 50/48, 90/85, 99/97). The trucks were packing each square within seconds of it
  being laid, so the user never had to pack by hand.
- **No truck wrecked.** 21 had arrived and none had wrecked when watching began, on bare graded
  ground with no gravel. On this flat map a graded road is enough for a truck.
- Sprint stayed on. The tuning panel was never opened.
- The user then chose the Middle map, walked up onto the hill without staking anything, and
  stopped.
- About 550 frames a second throughout, and nothing in the console.

What was not seen: the staking and the grading of the road itself, which were done before
watching began, and anything about how it felt beyond what the user said.

## 2026-10-05, midday: the test grounds

Claude was not watching. What the user said afterwards: "Trucks at junctions are turning too
tightly. Let's keep them in their right hand lanes." "Building junctions looks good, and
intuitive. I did not test zoning roads, but it clearly worked from your example. The 'things
that looked off' are fine." "The quarry needs to be a bit bigger, and actually a dug out
section of the world." The rest of that message was requests, and is in `DESIGN.md`.

## 2026-10-05, afternoon: the dump truck

Claude was not watching. The user: "The dump truck does not appear to work. And the bed rotates
in the wrong direction." Both were faults (see `NOTES.md`): the truck could not be clicked with
the stake, zoning or dev tool in hand, and the bed swung nose down. After the fix the user
changed the design instead: dumping is automated, and players level what is dumped.

## What Claude took from the first playtest

- Nothing wrecked on Short, so nothing there showed the truck as the judge of the road. Bare
  graded ground passes every truck on the flat; gravel only matters on a slope. See the concerns
  in the reply of that day, and "What a truck can climb" in `DESIGN.md`.
- The Middle, Long and Climb maps are still unplayed.
