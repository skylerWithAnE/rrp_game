# Notes for the next prototype

Started 2026-10-05 at the user's request ("Let's also start taking notes for the next
prototype"). This is a list of things deliberately left out of the current prototype, so they
are not lost. Nothing here is designed or decided unless it says the user said it.

## The shape of it

At the end of 2026-10-05 the user set out the long-term goals, which are quoted in full in
`DESIGN.md` under "Where it is going". In short, in the user's terms:

- **A large map with multiple cities that need to be connected.**
- **Players start with just their shovel, build a dirt road for low volume traffic, and start
  making money.** Money is first spent on gravel, asphalt and paint.
- **Gravel is the first real step in a fully functional road.** Each tier of road has a lot of
  hazards, to encourage roadwork. **A paved road is almost entirely hazard-free.**
- **All roads take damage. The dirt road is much more fragile.**
- **More dramatic actions.** Suspension leading to more vehicles flying around.
- **Vehicles running into the player**: in the design, not to be built yet. "That will be its own
  system."
- **Dump trucks, rollers and gravel trucks are both automated and drivable.**
- **Players will eventually be able to purchase their own quarry.**
- **Painting is hand drawn with a roller brush**, and the paint truck is hand drawn too, "so that
  the last phase of completing a road is hard to nail a perfect score on".
- **Scoring systems aren't important.** Score here means "the value paid to the players per road
  traveler".

The items below were said earlier the same day and still stand unless the goals above replace
them.

## Changed or parked in the design session of 2026-10-05

In the user's words. The next build, six slices, is in `PLAN.md`; these are what it leaves out.

- **"Just the shovel is officially no longer a goal of the final prototype."**
- **Money**: "The players will build a dirt road to get initial money. Usage of the quarry will
  cost money. That is for the final prototype. This next prototype will not involve money."
- **Signage**: "not something we want to incorporate in the design right now." The user had
  said a crew's first move at a ruined stretch is "a sign to deter/block traffic".
- **Hot asphalt**: "Let's backburner hot asphalt until we develop more basic vehicle health
  systems." Vehicle health is not designed.
- **Paint and recklessness**: "Let's plan on driver recklessness as a future hazard that is
  dampened by painted roads. Not to be worked on this sprint, but something to keep in mind as
  we further develop traveler AI."
- **Wear on paved roads**: "for now, we will not even implement road wear on paved roads."
- **Repaving** as a way to fix bad paint: named by the user beside tar spray and a grinder; left
  out of the next build by Claude, because the dump truck cannot lay over a finished road.
- **The large map**: waits. "Next build should focus again on small isolated slices."
- **Theming the planet**: "It might help with theming. I'm not sure about that."

## Said by the user

- **Small, finite sources of gravel** that players can use before they set up or find a quarry.
  In the current prototype gravel is unlimited but has to come from a quarry. (2026-10-05)
- **Players create truck depots**, and when sending a truck they pick its destination from
  them. The current prototype has fixed depots and one place to send a truck. (2026-10-05)
- **Set dressing for flinging gravel**: something that shows it going through the air.
  "We'll work on the mechanics for now and add set-dressing later." (2026-10-05)
- **Signage.** Blocking roads with signs. Stop signs. (2026-10-05)
- **Vehicles are damaged by "hot" (unspread) asphalt and by uncompacted gravel.** (2026-10-05)
- **Players dumping asphalt** is cut for now: the dump truck is automated. (2026-10-05)
- **Paving will do more** than it does now. Answered later the same day: a paved road is almost
  entirely hazard-free. Today its only effect is that trucks drive half as fast again.
  (2026-10-05)
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
- The dump truck decides for itself when to come, and covers the whole road. Ordering asphalt,
  or saying where it goes, is not in.
- The dump truck fills itself whenever it is back at the yard. Where asphalt comes from (a
  plant, oil, the quarry) is not in.
- The roller drives itself along its lane when sent. Whether a player should drive it.
- Asphalt is spread and paint is laid for free and without limit.
- Paint is the two edge lines and a broken centre line, all at once, per square. The goals
  replace this with lines drawn by hand. Colours, crossings and junction markings are not in.

Driving
- Vehicles are on the Driving test ground only. Whether they belong on the maps, and whether
  the trucks and roller that are sent today should be driven instead.
- Passengers, and carrying anything in the pick-up.
- The loader fills from one fixed pile and carries one bucket. Loading a truck with it, and
  digging with it, are not in.
- A driven vehicle cannot be wrecked: on its side, it is set back on its wheels.
- Nothing stops two players' vehicles, or a vehicle and a truck, from hitting each other.

Roads
- The hazards of each tier of road are not designed. Today a truck needs nothing but graded
  ground up to 10 degrees, so a dirt road has none.
- Junctions on the maps. They are allowed on the Junctions and Quarry test grounds only.
- A rope across a rise runs under the ground where it cannot be seen.
- Everyone starts at town A.

Tools
- The dev tool (key 7) and flying (V) are for testing and should not ship as they are.
- Seven tools on number keys (1 to 6 and 8, with the dev tool on 7 between them) is a lot. What
  is in the player's hands is shown only for the shovel.

Known faults
- Killing a client's process (not closing it) makes the host stop hearing everyone.
