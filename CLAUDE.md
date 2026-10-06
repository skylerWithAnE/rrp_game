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
text is in `docs/DESIGN.md`. None of it is built, and the next session is for designing it.

## Start here

1. `docs/DESIGN.md` is the game as it stands: what the user decided, in their words and dated,
   how each thing was built, what has been played and what has not, and what is open. Start here.
2. `docs/PLAN.md` is where the project is going, where it stands, and the agenda for the next
   session. `docs/NEXT_SESSION_PROMPT.md` is the prompt the user means to start it with.
   `docs/SESSION_2026-10-05.md` is what the last session built and what went wrong in it.
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
- Outside that push, the older rule stands: do not add features, systems or options that are not
  in `DESIGN.md` or `PLAN.md`; if something seems missing, ask.
- **Build and confirm each mechanic on a station first** (user, 2026-10-05: "Using stations to
  build, test and confirm each mechanic seems like it worked out really well").
- No end, score or clock yet. Wear is off. A second player has exactly the same controls as the
  first. Do not spend long on the towns: the first prototype's were fine. (This is the prototype
  as built. The long-term goals bring money per traveller and wear on every road; do not build
  either until the user says so. "Scoring systems aren't important": score means what a
  traveller pays.)
- **Play before building more** (learned 2026-10-05): do not stack new systems on ones the user
  has not played.
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
