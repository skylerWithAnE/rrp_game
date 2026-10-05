# Plan for the prototype

Written 2026-10-05 by Claude, after the user accepted the five stations. **Everything here is
Claude's proposal until the user has agreed it.** Nothing in it is built.

## What the stations settled

The verbs work and the user likes them: stakes and rope, hold-to-click grading with a hot spot,
gravel that packs, trucks that judge the road by driving it, wreck on bad road and wear out good
road. What does not exist is a reason to do any of it: no job, no map, no start and end, no clock.

## What the prototype is

One job, on one map, for one to four players: **stake, grade and gravel a road from one place to
another across rough ground, good enough that trucks get through.** The trucks start running as
soon as the two ends are joined, on whatever is there. The crew's work is then keeping them
arriving: fixing where they wreck, and repairing what they wear out.

That sentence is the proposal. The questions below decide its size and its ending.

## Questions for the user

These cannot be settled by Claude, and steps 2 and 3 wait on them.

1. **How long should one job take, and for how many players?** By arithmetic, one 20 m section is
   about two minutes of clicking for one player, before walking and aiming. If that holds, four
   players for 30 minutes could build a few hundred metres. Nobody has timed a section by hand.
   Timing one would fix this number.
2. **What is at each end of the road?** The first prototype had towns. This design has nothing.
3. **What ends a job?** Trucks arriving at all, a number of them, a number in a row, a clock
   running out, or nothing: the road is simply kept open.
4. **Do trucks wear the road everywhere, or is wear still an experiment on one road?**
5. **What does a second player do that the first does not?** Still unanswered from the redesign.
   The stations give every player the same three tools.
6. **Curved sections or mitred corners?** The user wanted to see the mitre first and has now
   played it.

## Steps

Each step is built, checked across two instances, and played by the user before the next starts.

### Step 1: one map

- One piece of ground big enough for a real route, with a hill or a hollow in the way, in place of
  the eight separate plots. A marked start and a marked end.
- All three tools work anywhere on it.
- Trucks follow any chain of stakes from the start to the end, in whatever order the stakes were
  placed. This removes the limit that trucks cannot drive a road the players staked.
- The stations stay reachable until the user says they can go.
- **What follows from it:** the ground is the cost. At 0.25 m between points, a map 200 m square
  is 640,000 points: about 2.5 megabytes to a joining player, and the code that works out what
  each point belongs to would have to be made local, because it currently visits every point when
  a stake changes. A coarser ground (0.5 m) is a quarter of that and changes how grading looks.
  This is the first thing to settle by trying it.
- **Does not need the questions answered**, except for how far apart the two ends are. Claude
  would start at 150 m and make it a slider.

### Step 2: the job

- Trucks set off by themselves once the ends are joined, and keep coming.
- The readout counts arrived and wrecked, and whatever question 3 decides is shown and ends the job.
- The host can start a fresh job.
- **Waits on** questions 1, 2 and 3.

### Step 3: keeping the road open

- Wear on the whole road, tuned so that a crew can keep up but has to work.
- **Waits on** question 4, and on step 2 having been played.

### Not in this plan

Earth that has to come from somewhere, things in the ground, weather, oil and asphalt, junctions,
a second map, scores between sessions, and anything about the look. They are listed in `DESIGN.md`
under "Open" and stay there until the user asks for them.

## Housekeeping that could be done at any point

- Delete the first prototype's switched-off code (about 2,000 lines). The user said not to during
  the stations.
- Link a Unity Cloud project so join codes can be tried. Only the user can do this (`SETUP.md`).
