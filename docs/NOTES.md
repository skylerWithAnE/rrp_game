# Notes from building the stations and the maps

Written by Claude on 2026-10-05, at the end of the session that built stations 1 to 5, and added
to that night after the unattended session that built the maps. These are things the next
session would otherwise learn again the slow way.

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
| A new stake on the map needed its height before the gravel arrays existed | `HeightAt` reads gravel. While making ground, read the heights directly |

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
- A truck climbs 25 degrees and more on packed gravel, 16 on loose gravel and 10 on bare ground
  that is on its line; it wrecks at 18 on loose and 12 on bare. Downhill it does not care.
- To time anything with trucks, `Time.timeScale = 4` works: the physics steps are the same
  length, there are just more of them a frame. Put it back to 1.
- The maps: Short is 135,000 points, Middle at 150 m 337,000, Climb 459,000, Long 578,000. The
  host makes the Long map in 0.3 s. A joiner is sent 30 to 70 kB plus ten bytes for each point a
  click has changed.
