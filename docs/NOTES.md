# Notes from building the stations and the maps

Written by Claude on 2026-10-05, at the end of the session that built stations 1 to 5, and added
to that night after the unattended session that built the maps, and again on 2026-10-06 after
the build of the six slices. These are things the next session would otherwise learn again the
slow way.

## How the user works

- The user plays in the editor while Claude builds, and sends requests one after another without
  waiting for the last to finish. Several arrive in the middle of a build. Fold them in.
- **Compiling stops their game.** The editor has to leave play mode to compile, and any stakes or
  levelling they had done is lost. Say so when it happens.
- Feel questions get answered in minutes when there is something to play, and not at all on paper.
  What worked: build the simplest version of a proposal, mark it as a proposal in `DESIGN.md`, and
  list in the hand-over exactly which choices were Claude's.
- When the user asks "how do I do X?", it usually means X is missing or cannot be found.
- When a request is ambiguous, look first. A snapshot of their Game view and a read of the stakes
  they had placed showed what "connecting two grids isn't working" meant.
- The user said what they did not want fixed. Cosmetic problems Claude spotted (dark outlines, a
  crowded readout) were "not concerns right now". Report them once and move on.
- Explosions and jank are liked. Hold-to-click, the hot spot and trucks running on unfinished
  road were all liked.

- **Do not build far ahead of what has been played.** On 2026-10-05 about ten systems were built
  on three the user had played, and the user's one hands-on try of the newer ones found a fault
  in the first minute. Say so when a request would add to an unplayed stack.
- **The user's goals arrive as a direction, not an order.** The long-term goals of 2026-10-05
  came with "update all docs", not "build". Write them down in the user's words, say what
  follows from each, and wait.

- **"I am asleep" does not last the night.** On 2026-10-06 the user was in the editor, playing
  the first slices, about two hours into an unattended build, without saying so. Before every
  compile, ask the editor whether it is in play mode and whether anyone has moved the player. If
  someone is playing, leave `Assets` alone until they stop: the editor recompiles a changed
  script under a running game. Docs can be written meanwhile. And never leave a compile error on
  disk for longer than it takes to fix it: the editor will not enter play mode with one.

## What went wrong, and the fix

| Problem | What works |
|---|---|
| The blob's feet floated 0.08 m, so eye height was wrong | A CharacterController rests its skin width above the ground. Raise the capsule's centre by the skin width |
| Screenshots from the editor's Game view came out very wide | The Game view's aspect is whatever the panel is. Render the game camera to a 1280 by 720 texture from script and save that |
| A joining client threw an exception building a plot | The plot was built when the ground arrived, before its stakes. Send the stakes last and build then |
| Whole-plot state would not fit comfortably in one message | Send it in pieces of a few thousand points |
| Host and a late joiner disagreed on a worn road | The joiner is sent whole millimetres. Keep the host's ground on whole millimetres too, except where it lands exactly on its line |
| Outlines drew as dark slate, not white | LineRenderers were lit by the sun. They use an unlit material now |
| Sections overlapped and left gaps at a bend | Mitre the ends. Measure position along a section between its two cuts, from 0 to 1, so heights agree along a cut |
| The truck slowed on bare ground and so never failed | Let the surface limit how hard it can push, not how fast it goes |
| One lane had a half-width square | A fixed square size will not divide the road. Fit a whole number of squares to the lane and to the section |
| A test "froze" wear by setting damage to 0, and the road kept rutting | Squares already below the threshold keep rutting. Set the threshold to 0 to stop it |
| A script-driven player would not move | `AutoTest` resets the player's driver every frame. Disable `AutoTest` first |
| A two-instance comparison disagreed | The client's last log line was a second older than the host's reading. Stop the action, wait, then compare |
| The client lost its connection the moment a script changed the map on the host | A message sent from `umcp.py code` goes out from outside the game's frame and is never delivered. Hand the work to `AutoTest.Next.Enqueue(() => ...)`, which runs it in `Update` |
| Trucks on a map were magenta | Their materials were made when the scale yard was built, and a map has no yard. `Yard.MakeTruck` makes them itself now |
| Trucks queued on a hill for ever instead of wrecking | They slid back and crept up, never still for 4 seconds. Stuck now also means no further along the path |
| The editor was not in play mode after a build | A build stops play mode, and a play request sent as the build ends can be lost. Check the state and ask again |
| The host went deaf: clients dropped and nobody could join, with nothing in the log | A client process had been killed, not closed. About 30 s later the host hears no one until it is restarted. Cause not found; it is not the map code. In tests, close clients with `CloseMainWindow()`, never `Stop-Process` |
| The user found the dump truck could not be sent, after Claude had checked that it could | The check called the host's function directly. The player's way in was behind the tool in hand: with stakes, zoning or the dev tool held, the truck was never looked at, and stakes is what a player starts with. A scripted check of a rule is not a check of the control. Stand the player there with each tool and read what the screen says (`Plot.Why`) |
| The dump truck's bed tipped nose down | In Unity a positive turn about x takes the front down. Look at a screenshot of anything that swings before calling it done |
| A new stake on the map needed its height before the gravel arrays existed | `HeightAt` reads gravel. While making ground, read the heights directly |
| Ramps made to order for measuring were longer than the yard, and trucks from the far end fell off the world and were counted as wrecks | The yard is 700 m square, from z -310 to 390. A measuring road has to fit on it. A count that is all wrecks at "0 m" is the harness, not the truck |
| A measuring batch ended before the slow trucks had finished | It waited for a number of trips on each road, and the trucks that fell off supplied them. Count arrivals and wrecks apart |
| The first wear settings dug every road down to the yard inside 100 trucks | Damage that scales up and damage by landing feed each other. Holes need a deepest (`wearDeepest`), and the bottom has to be uneven or a worn-out road is a smooth trench |
| Screenshots from the blob's eyes looked as if the eye were at knee height | The camera's field of view follows the Game view's shape, which is whatever the panel is. Set `cam.aspect` to 16:9 and the field of view again before rendering to the texture, and `ResetAspect` after |
| A helper script was refused by the shell tool | It held `Remove-Item` beside a regex. Empty a file with `Set-Content`, and do not name a function `Compare`: it is an alias |
| PowerShell wrote a byte-order mark into a script it patched | `Set-Content -Encoding utf8` does that in PowerShell 5. Write with `[IO.File]::WriteAllText` and a `UTF8Encoding($false)` |
| Loose gravel spun trucks at Mars and never on Earth | Sideways grip was scaled by gravity twice over. Loose gravel's grip is now a share of Earth's on every planet |

## Driving Unity from Claude Code

- `py tools/umcp.py` talks to the editor. `code <file>` runs a C# method body in the editor, which
  is how positions, stakes and trucks were read and driven during play.
- A compile is: `refresh_unity`, wait about 20 seconds, then read the console for errors. A build
  is the menu item `RRP/Build Windows Player`, then wait for the console line `RRP build:`. A build
  with compile errors says `RRP build: Unknown`.
- Check whether the editor is in play mode before changing a script, and stop it if so.
- Shell heredocs mangle backslashes in C# and Python. Write scripts to a file and run the file.
- **Play mode does keep ticking while the editor is in the background** (about 600 frames a
  second on this machine), so a whole night can be driven without anyone touching the window.
- `umcp.py code` compiles with an old C# compiler: no `out var`, no string interpolation.
- JSON arguments with a space in them do not survive PowerShell. Put them in a file and pass
  `@file`.
- Screenshots taken through the MCP camera tool land in `Assets/Screenshots`. Delete them after.
- **Mouse clicks can be scripted**, which is how a control is checked and not just its rule: set
  `Hud.Playing = true`, point the camera (`Game.I.cam.yaw`, `pitch`), then
  `InputSystem.QueueStateEvent(Mouse.current, new MouseState().WithButton(MouseButton.Left, true))`,
  wait a fifth of a second, and queue an empty `MouseState` to let go. Read `Plot.Why` for what
  the screen says under the crosshair.
- **To watch hundreds of trucks**, set `tuning.fastForward` (1 to 10) and call `TuningChanged`.
  `Time.timeScale` set by hand is overwritten every frame now.
- `Lorries.log` records how every truck's trip ended (where, how far along, how far off its
  lane). `AutoTest.WearLog`, set to a list, gets a line each time a wearing road's truck count
  passes 5, 10, 25, 50, 100, 150, 250, 350, 500. `Plot.TestDegrees`, `TestSurface` and
  `TestHeight` make the Trucks ground's four ramps to order.

## Numbers worth knowing

- One 20 m section is 40 road squares and 20 shoulder squares at the 2 m setting.
- Levelling rough ground takes about 4 clicks a square on average, and gravel takes 7 (3 to lay,
  4 to pack). At the cap of 4 clicks a second that is roughly a minute to level a section's road
  and a little over a minute to gravel it, before any walking or aiming. This is arithmetic, not a
  measurement: nobody has timed a section by hand.
- A finished 40 m road passes a truck in about 15 seconds. The wear road went from fully packed to
  half packed in about a minute at the default damage.
- Eight plots are about 250,000 ground points. A joiner receives about a megabyte.
- A script clicking at the cap, half its clicks on the hot spot, built the Middle map's 153 m
  (straight over the hill, shoulders included) in 13 minutes with Claude's two systems off and 5
  with them on, and the Long map's 306 m in 11 with them on.
- On Earth a truck climbs 27 degrees on packed gravel, 16 on loose gravel and 10 on bare ground
  that is on its line; it wrecks at 30 on packed, 18 on loose and 12 on bare. Downhill it does
  not care. At Mars and the Moon the figures are in `DESIGN.md`, under "Gravity".
- On Earth a truck gets 10 m up a 20 degree slope it cannot climb, on the speed it arrives
  with; at Mars 27 m and at the Moon 33. A ramp that is meant to stop one has to be two
  sections long on Earth and six at Mars.
- To time anything with trucks, `Time.timeScale = 4` works: the physics steps are the same
  length, there are just more of them a frame. Put it back to 1.
- The maps: Short is 135,000 points, Middle at 150 m 337,000, Climb 459,000, Long 578,000. The
  host makes the Long map in 0.3 s. A joiner is sent 30 to 70 kB plus ten bytes for each point a
  click has changed.
