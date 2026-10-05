# Notes for the next prototype

Started 2026-10-05 at the user's request ("Let's also start taking notes for the next
prototype"). This is a list of things deliberately left out of the current prototype, so they
are not lost. Nothing here is designed or decided unless it says the user said it.

## Said by the user

- **Small, finite sources of gravel** that players can use before they set up or find a quarry.
  In the current prototype gravel is unlimited but has to come from a quarry. (2026-10-05)
- **Players create truck depots**, and when sending a truck they pick its destination from
  them. The current prototype has fixed depots and one place to send a truck. (2026-10-05)
- **Set dressing for flinging gravel**: something that shows it going through the air.
  "We'll work on the mechanics for now and add set-dressing later." (2026-10-05)
- From earlier, still parked: dirt flinging as a mechanic; where earth comes from and goes
  once it stops being free; what ends a job, scores, a clock; wear as part of the game; the
  look.

## Left out by Claude while building, for the user to decide

Trucks and depots
- A truck sent back along a road is put at the start of its way facing along it. There is no
  turning round at the end of a road: no loops, no reversing.
- With more than two depots, how a destination is picked. Today a right click sends a truck to
  the one other depot.
- Whether a truck should leave by itself when full, or only when sent. Today: only when sent.
- Nothing gives way at a junction, and trucks do not avoid a standing truck.
- Trucks cut straight across a junction's level ground when going straight on, and turn on a
  curve of about 4 m radius when turning right. A real truck needs more room.

Quarries and gravel
- A quarry is fixed. Finding one, or opening one, is not in.
- Gravel laid on a road comes off one heap wherever on the plot it is laid. Whether gravel
  should have to be near the road it is laid on is not decided.
- The heap can be put anywhere on the plot the truck can be seen from, and only moved when
  empty.
- Whether gravel should be limited on the maps, and how a map gets a quarry.

Paving
- The dump truck fills itself whenever it is back at the yard. Where asphalt comes from (a
  plant, oil, the quarry) is not in.
- The roller drives itself along its lane when sent. Whether a player should drive it.
- Asphalt is spread and paint is laid for free and without limit.
- What paving is for. Today: trucks drive half as fast again on rolled asphalt. Wear is the
  other obvious answer (paved road does not wear), and wear is off.
- Paint is the two edge lines and a broken centre line, all at once, per square. Colours,
  crossings and junction markings are not in.

Roads
- What gravel is for on the flat: a truck needs nothing but graded ground up to 10 degrees.
- Junctions on the maps. They are allowed on the Junctions and Quarry test grounds only.
- A rope across a rise runs under the ground where it cannot be seen.
- Everyone starts at town A.

Tools
- The dev tool (key 7) and flying (V) are for testing and should not ship as they are.
- Six tools on number keys is a lot. What is in the player's hands is still not shown.

Known faults
- Killing a client's process (not closing it) makes the host stop hearing everyone.
