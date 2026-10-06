# rrp_game

Online co-op Unity prototype, first person: a crew of solid-color blobs builds a road that a box
truck can drive. They set stakes, bring the ground to the rope between them, lay and pack gravel,
and trucks drive the result, wrecking on bad road and wearing out good road.

The five mechanics stations are built and were played and accepted by the user on 2026-10-05. The
prototype itself was built that night, unattended: maps, each with a town at either end, to
stake, grade and gravel a road across and watch trucks try it. The user played the Short map the
next morning and said it "went great" and "is looking much more like what I envisioned"; the
other maps are unplayed. The same day the user asked for, and got: a rope steepness limit, a map
with rocks that forces a winding road, the Stations map cut into focused test grounds, junctions,
service roads with a zoning tool, a quarry pit where gravel is shovelled into a truck that is
sent to a drop, a paving and painting process, vehicles a player drives (pick-up, roller, front
loader, paint truck), a shovel in the player's hands, a dev tool and flying. The user has tried the junctions and accepted
them; the rest is unplayed. `docs/NEXT_PROTOTYPE.md` collects what is
being left for the prototype after this one. There is no end, score or clock, by decision.

**Where it is going (user, 2026-10-05):** a large map with several cities to connect. Players
start with a shovel and a dirt road, are paid for each traveller, and spend the money on gravel,
asphalt and paint. Each tier of road has hazards that the next removes; all roads take damage
and dirt is fragile; vehicles are both automated and drivable; paint is drawn by hand. The full
text is in `docs/DESIGN.md`. None of it is built.

**The design session (2026-10-05)** narrowed the goals and planned the next build. It is set on
another planet, in low gravity, "to make things more dramatic and harder for players to
predict". Dirt and gravel wear out fast and paved road does not. "Just the shovel" is dropped,
and money waits for the final prototype.

**The six slices were built on 2026-10-06, unattended, and the user has not played them**:
gravity on a slider (it starts at Mars), wear on dirt and gravel, loose gravel that spins trucks
out, a gravel truck that runs its own round to a drop-off the player surveys, lines painted by
hand and judged unseen, and a button that stakes and finishes the road on any map. Each exists
to answer one question. `docs/SESSION_2026-10-06.md` opens with the play list and ends with
every choice that was Claude's, for a yes or no. After a first go the user set gravity to the
Moon (0.16) and asked for a third-person camera in vehicles, seats in the pick-up and the paint
truck, a second spin-out road, and the Wear ground raised to 10 m with four roads and counters;
those are built and unplayed too.

## Start here

1. `docs/DESIGN.md` is the game as it stands: what the user decided, in their words and dated,
   how each thing was built, what has been played and what has not, and what is open. Start here.
2. `docs/PLAN.md` is where the project is going and where it stands: the user plays the slices
   next, and nothing further is planned until then.
   `docs/SESSION_2026-10-06.md` is what the last build made, how it was checked and what went
   wrong in it; `docs/SESSION_2026-10-05.md` is the session before.
   `docs/NEXT_SESSION_PROMPT.md` is the prompt that build was run from. It is spent.
   `docs/PLAYTEST.md` is what was seen when the user played. `docs/NEXT_PROTOTYPE.md` is what is
   being left for the prototype after this one.
3. `docs/TECH_PLAN.md` describes the code as it stands.
4. `docs/NOTES.md` is what was learned building the stations: how the user works, and the
   technical traps. Read it before the first build of a session.
5. `docs/SETUP.md` is tooling and how to run two instances.

**When a document goes stale, rewrite it in place** (user, 2026-10-05: "I don't think we need to
use archive in general"). The old version is in git; say which commit in the new one's header.

**Do not read `docs/archive/` unless the user asks you to.** It holds superseded documents: the
first prototype's design, technical plan and post-mortem, and earlier planning full of ideas that
were cut or deferred. Nothing in it is current, and it must not be used as a source of features or
requirements.

The first prototype (third person, blobs building a road from physics cubes with a shovel) was
rejected on 2026-10-04. Its code is still in `Assets/Scripts` (`Cubes`, `Quake`, `Blocks`, `Road`,
`Truck`, `Verbs`, `Ground`, `Sfx`), switched off. Do not continue it and do not delete it without
asking.

## Ground rules from the user

- **Latitude for the prototype push (user, 2026-10-05).** The session building the maps worked
  while the user was asleep and was deliberately unrestricted: solve problems creatively, and add
  a system if it would streamline the gameplay, without asking first. Write down each addition as
  Claude's, with the reason, and make it something that can be switched off. **That push is
  over.** The user called it "a big success".
- **The overnight build of 2026-10-06 had the same latitude** (user, 2026-10-05: "Give it wide
  latitude. Same as before"), and it **ended with that build**.
- **So the older rule stands now**: do not add features, systems or options that are not in
  `DESIGN.md` or `PLAN.md`; if something seems missing, ask.
- **Build and confirm each mechanic on a station first** (user, 2026-10-05: "Using stations to
  build, test and confirm each mechanic seems like it worked out really well").
- No end, score or clock yet. Dirt and gravel wear (on the Wear ground and on maps; a switch
  turns it off on maps); paved road does not wear (user, 2026-10-05). A second player has exactly the same controls as the
  first. Do not spend long on the towns: the first prototype's were fine. (This is the prototype
  as built. The long-term goals bring money per traveller; do not build
  it until the user says so. "Scoring systems aren't important": score means what a
  traveller pays.)
- **Play before building more** (learned 2026-10-05): do not stack new systems on ones the user
  has not played. As of 2026-10-06 six slices are waiting to be played.
- **The user may be in the editor at any hour**, including during a build they called
  unattended (2026-10-06). Check before every compile; do not change a script under a running
  game. See `docs/NOTES.md`.
- It is a prototype: fast, simple and weird beats correct and polished. The user is enjoying the
  trucks' explosions and jank (2026-10-05): do not smooth them out.
- First person. Several tools, each with one job. Throwing cubes around is out.
- Solid-color shaders, no textures. Blobby solid-color characters, 1.6 m tall. Look and fidelity
  are parked.
- All animation is procedural. No animation clips, no Animator controllers.
- Multiplayer is the point. Build and verify every system networked from the start.
- Every size is chosen in metres against the 1.6 m blob and is not settled until the user has stood
  next to it. Make the numbers adjustable during play.
- Questions about feel are answered by playing, not on paper. Build the simplest version of a
  proposal, say plainly that it is a proposal, and let the user play it.

## Working conventions

- Code-first: generate the world from scripts at runtime rather than hand-authoring scenes and prefabs.
- Every gameplay number lives in the one tuning asset (`Tuning.cs`), which makes it a slider.
- The host decides everything and sends results, not operations.
- When the user decides something, update the docs. Keep the user's decisions distinct from
  Claude's proposals.
- Before committing to a rule, say what follows from it, especially for sizes. Ask open questions,
  not multiple-choice ones that steer toward small fixes.
- Before asking the user to play something new: check it across two instances, say how, say what
  was not checked, and send a screenshot from the blob's eyes.
- Commit when the user says a step is accepted. During the unattended prototype push, commit each
  step once it works on two instances. Never push to a remote unless asked.
