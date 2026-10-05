# Plan for the prototype

Rewritten 2026-10-05 after the user answered the first version's questions. The section "Decided
by the user" is the user's. Everything else is Claude's proposal, and the session that builds it
is free to change it.

## Decided by the user (2026-10-05)

- **The next push is built while the user is asleep.** The building session has wide latitude: it
  may solve problems creatively, and **if it sees a system that would streamline the gameplay, it
  may add it.** It does not need to ask first.
- **Several maps, to try different distances** between the two ends of the road, and to get a feel
  for the mechanics on real ground.
- **150 m between the two ends is fine as the starting distance**, with the distance as a slider.
  The other maps try other distances around it.
- **There is no end.** No win, no score, no clock. The prototype is for demonstrating and playing
  with the mechanics.
- **Do not spend much time on the places at each end.** The towns in the first prototype were
  fine as they were.
- **Wear is off.** Trucks do not damage the road in the prototype.
- **A second player is the same as the first** in every control. There are no roles.

## What this push builds

One sentence: **pick a map, stake, grade and gravel a road between its two towns, and watch the
trucks try it.**

1. **Maps.** Each map is one piece of ground with a town at each end and rough land between. The
   host picks the map. A set that tries different distances and different land, for example:
   - short, about 60 m, gently rough: one player can finish it in a sitting
   - medium, about 150 m, with a hill or a hollow in the way
   - long, about 300 m
   - a climb: the ends far enough apart in height that a straight road is too steep for the
     truck, so the road has to wind
   The distances are proposals; the point is to have several to compare. The stations stay
   available as one more choice, since they are the reference for how each mechanic felt.
2. **All three tools work anywhere on a map.**
3. **Trucks drive what the players staked.** As soon as a chain of stakes joins the two towns,
   trucks set off from each town and keep coming, in both lanes, on whatever is there. They wreck
   where the road is bad. That is the feedback; nothing else judges the road.
4. **Towns** are a few solid-color blocks and a pad at each end, as in the first prototype. An
   hour at most.

## What follows from it, and where the risk is

- **The ground is the cost.** The stations' plots hold a height every 0.25 m. A 300 m map at that
  spacing is over a million points: megabytes to a joining player, and `Plot.Resolve` visits every
  point each time a stake changes. Something has to give. Options, none tried: coarser ground away
  from the road; ground made from a seed on every machine with only the edits sent; working out
  sections only near the stake that changed; a map as a strip of plots. Pick by trying. The result
  must still be the same on every machine.
- **`Plot.Route` assumes the stakes are in order.** Players place them in any order and can leave
  gaps, so the trucks need the chain walked from one town to the other.
- **A road a crew cannot finish is no demonstration.** By arithmetic one 20 m section is about two
  minutes of clicking for one player. A 300 m road is then half an hour of clicking alone. This is
  exactly where a streamlining system may earn its place. The user has said such systems are
  allowed; record any that is added as Claude's, with the reason, and make it a slider or a switch
  so it can be turned off and compared.
- **Nobody will play it before the next step is built.** That removes the check that caught every
  wrong turn in the stations. In its place: verify each step across two instances, look at it from
  the blob's eyes in screenshots, and keep each step small enough to describe in a few lines.

## How to work while the user is asleep

- Read `CLAUDE.md`, this file, `DESIGN.md`, `TECH_PLAN.md`, `NOTES.md` and `SETUP.md` first.
- Unity has to be open with the project loaded and not in play mode. If it is not reachable, say
  so and stop; there is nothing useful to do without it.
- Build in steps. After each step that works on two instances, commit it, so the morning starts
  from something that runs whatever happened later. Do not push to a remote.
- Keep `DESIGN.md`, `TECH_PLAN.md` and this file true as things change. Keep the user's decisions
  apart from Claude's choices.
- Do not stop to ask. Where a decision is needed, make it, write down what was chosen and what the
  other option was, and carry on.
- Leave the first prototype's switched-off code alone unless it is in the way.
- End with a report the user can read over coffee: what to run, what each map is for, what to
  try first, every system that was added and why, what was checked and how, what was not checked,
  and what looked wrong.

## Still open, and not for this push

- Curved sections or mitred corners. The user has played the mitre and not said.
- What ends a job, scores, a clock.
- Wear, as part of the game.
- Earth that has to come from somewhere, things in the ground, weather, oil and asphalt, junctions.
- Anything about the look.
