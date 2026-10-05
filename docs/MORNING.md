# Morning report

Written by Claude at the end of the unattended push, night of 2026-10-05. Nothing here has been
played by a person. Everything was driven by script on two instances and looked at in screenshots.

**It is built.** Four maps with a town at each end, chosen by the host, plus the stations as a
fifth choice. The three tools work anywhere on a map. Trucks set off from both towns as soon as
the ropes join them. The ground problem is solved without making the ground coarser. I added two
systems of my own and one rule change; each is listed below with its switch.

## What to run

1. Unity is open, out of play mode, and `Builds/rrp_game/rrp_game.exe` matches the code.
2. Press Play in the editor, click in the Game view, click **Host (direct)**.
3. Run `Builds\rrp_game\rrp_game.exe` and click **Join**.
4. You start in town A on the Middle map. The map buttons are at the top of the host's screen.

Controls are the same as the stations: 1 stakes, 2 grade, 3 gravel, Shift sprint, F1 sliders
(host), F3 readout.

To build a road: with the stake tool, click the thick stake in town A, walk toward the tall pole
at town B, and click the ground every 20 m or less. Click town B's thick stake to rope the last
one to it. The readout says "towns joined: YES" and trucks start.

## The maps

| Map | Distance | What it is for |
|---|---|---|
| Short | 60 m, nearly flat | One player finishing a whole road in a sitting |
| Middle | 150 m, on the `mapDistance` slider (40 to 400) | Your starting distance. A 9 m hill sits on the straight line, with a hollow to its right. Over the top is short and a lot of clicking; round the left is longer and easy |
| Long | 300 m, rolling | Whether a long road is something a crew wants to build. A ridge crosses the whole map with one gap, to the right of the straight line |
| Climb | 160 m, town B 16 m higher | Land that makes the road wind. Straight is about 31 degrees, too steep for a truck even on packed gravel. The slope is gentle at the far left and a cliff at the far right |
| Stations | | The five stations as you accepted them |

Each map is the same land every time. "Make new land for this map" on the F1 panel makes a
different one. Changing map puts everyone back at town A and throws the road away.

Screenshots from the blob's eyes are in `docs/morning/`.

## What to try first

1. **Middle map, straight over the hill.** Stake it in a minute, join the towns, and watch the
   trucks wreck on the hill while you grade under them. This is the whole loop in ten minutes.
2. **Then the same map round the left of the hill**, to feel the difference in clicking.
3. **Switch my two systems off** (bottom of the F1 panel, set both to 0) on the Short map and
   build it again. That is the comparison you asked for.
4. **Climb**, to see whether winding a road is fun or a fight with the stake rules.
5. **Long**, with a second player if you have one.

## Systems I added, and how to switch each off

Both work on maps only, so the stations play exactly as you accepted them. Both are sliders at
the bottom of the F1 panel, under "Claude's additions".

| System | Slider | Why |
|---|---|---|
| Trucks pack the gravel they drive over. Each wheel packs the square it is on by a quarter, four times a second | `truckPacking`, 0.25. 0 is off | Packing was four of the seven clicks every gravel square takes, the biggest single part of the work, and the trucks are driving over it anyway. It also rewards joining the towns early |
| A click on the hot spot also does one ordinary click on every other square across the road at that point, shoulders included | `hotSpotRow`, 1. 0 is off | Your idea of giving value to individual clicks, turned up: a well-aimed click works six squares, so aiming beats holding the button |

**What they do to the time.** A script clicked at the cap (4 a second) with half its clicks on
the hot spot, on the Middle map straight over the hill, shoulders included:

| | Grade 153 m | Gravel 153 m | Total |
|---|---|---|---|
| Both systems off | 365 s | 408 s (lay and pack) | 13 minutes |
| Both systems on | 172 s | 122 s (lay; trucks packed it all) | 5 minutes |

With both on, the Long map's 306 m took 417 s to grade and 246 s to gravel: 11 minutes. These
are clicks only. A person also walks and aims, so expect longer.

**One rule I changed, on the stations too:** a truck is now stuck when it gets no further along
the road for 4 seconds, as well as when it stands still. On an unfinished hill trucks slid back
and crept up for ever and queued twelve deep instead of wrecking. There is no switch for this
one; `lorryStuckSeconds` is its slider. Tell me if you want the old rule back.

**Smaller choices that are mine:**

- A new game starts on the Middle map, not the stations.
- Land on maps is rough by 0.35 m (`landRoughness`), not the stations' 1 m.
- A truck leaves each town every 12 seconds (`truckEvery`), up to six on the way per lane.
- Town stakes cannot be pulled out or moved. A town has a painted pad, five blocks and a 26 m
  pole in its color.
- Everyone starts at town A.
- Up to 250 stakes on a map.

## How the ground was solved

The points are still 0.25 m apart everywhere, so clicking feels as it did. The host makes the
land as a height every metre, in whole millimetres, and sends only that: 30 to 70 kB. Every
machine fills in the points between with the same whole-number sums, so they cannot disagree.
After that only clicked points are sent. A stake change works out only the sections that
changed. The Long map is 578,000 points and takes the host 0.3 seconds to make.

## What I checked, and how

All on two instances: the editor hosting, the standalone build joined as a client, comparing a
hash of every plot's ground, gravel, packing and stakes.

- Every map and the stations: hashes agree after each of ten map changes with a client connected.
- The client staking a road and clicking along it: hashes agree during and after.
- A late joiner on a finished 150 m road: agrees with the host.
- Pulling a stake out of the middle of a road, raising a rope, a town stake refusing to come
  out, clicking again afterwards: agree.
- Changing the distance slider, and making new land: agree.
- Trucks on Middle, Long and Climb: same positions and the same arrived and wrecked counts on
  both. On Long, twelve trucks at once.
- A whole road built by script on Middle and on Long: 100 % level, gravelled and packed on both,
  trucks arriving.
- Frame rate with a finished 300 m road and twelve trucks: host about 550, client at its cap of 120.
- The stations after all of it: every plot agrees, trucks run on all five example roads.

## What I did not check

- **Nobody has played it.** No person has placed a stake on a map with the mouse, aimed at a hot
  spot, or clicked a map button. The scripts call the same code those do.
- A winding road on Climb. The script staked straight up the face, and trucks wrecked there as
  they should. Bends on a map use the same code as the hairpin stations.
- More than one client at once, and a second player doing real work.
- A weaker machine. This one has an RTX 4070 Ti Super.
- `mapDistance` at its 400 m end. 90, 150 and 300 m (the Long map) were run.
- Relay join codes, which have still never connected.

## What looked wrong

- **Killing a client's process makes the host stop hearing everyone.** About 30 seconds after I
  killed a client with Task Manager's equivalent, the other client was dropped and nobody could
  join until the host restarted. Closing the client's window normally is fine. I traced it as
  far as: it is not the map code (it happens with no map change), and both were on one machine.
  I did not find the cause. If a friend's game crashes mid-session, expect to re-host.
- **A rope across a rise runs under the ground**, so over the Middle map's hill you cannot see
  the string between two stakes. The ground color still shows the section.
- **Nothing stops a rope being too steep to drive.** On Climb you can stake a road no truck will
  ever finish.
- On the stations' bad road one truck of two arrived in my last check. Before tonight it wrecked
  every truck in the one check that was written down. Small sample, and the stuck rule changed,
  so I cannot say which it is.
- The wear road's hash differed between host and client once, mid-wear, and matched in two
  other checks. That is the known one-second lag in the comparison, not proven to be more.
- The readout and the map buttons overlap if the window is narrower than about 1000 pixels.

## Commits

Five, none pushed: the ground rework, the maps, trucks on maps, the two systems, and the docs
with this report. `DESIGN.md` has a new section "The maps" with every choice marked as mine;
`TECH_PLAN.md` has "The map's ground"; `NOTES.md` has the new traps.
