# Setup

Machine: Windows 11. Project: `C:\Users\skyle\source\repos\rrp_game`. State as of 2026-10-05.

## Done

- Git repo on `main` with Unity `.gitignore` and `.gitattributes`.
- Unity Hub installed. Editors present: `6000.3.25f1` and `6000.6.4f1`. **The project uses
  `6000.3.25f1`.**
- Packages (in `Packages/manifest.json`): Universal RP 17.3.0, Input System 1.20.0, Netcode for
  GameObjects 2.13.3, Unity Transport 2.7.4, Multiplayer Play Mode 2.0.2, Multiplayer Services
  2.3.3 (this is the package that contains Relay), and MCP for Unity (`com.coplaydev.unity-mcp`,
  from the CoplayDev git URL).
- URP is on: `Assets/Settings/URP_Asset.asset` is the render pipeline for every quality level.
- New Input System is the only active input backend. Run In Background is on, and builds start
  windowed at 1280x720, so several instances can run side by side.
- `Assets/Scenes/Bootstrap.unity` is the one scene, and it is empty on purpose: the game builds
  itself at runtime.
- All of the above is done by `Assets/Editor/ProjectSetup.cs` (menu *RRP > Setup Project*). It only
  creates what is missing, so it is safe to run again.
- `uv` 0.12.23 installed (the MCP server needs it). Python 3.14 via `py`. Node.js not installed and
  not needed.

## Unity MCP

The server runs on `http://127.0.0.1:8080/mcp` and Claude Code has it registered as `UnityMCP` for
this folder. Unity must be open with the project loaded.

- The MCP for Unity editor preference "auto start on load" is now on, so the bridge reconnects by
  itself after an editor restart.
- Claude Code only loads the server's tools if the server is already up **when the Claude Code
  session starts**. If `/mcp` shows `UnityMCP` as failed, start Unity first, check *Window > MCP for
  Unity* says the server is running, then restart Claude Code (or reconnect from `/mcp`).
- If the tools did not load, the server can still be driven over plain HTTP with `tools/umcp.py`
  (`py tools/umcp.py list`). That is how the 2026-10-04 session worked.
- Port 8080 was once held by **Wwise** (Audiokinetic). If the server will not start, close Wwise or
  change the port in *Window > MCP for Unity* and click Configure for Claude Code again.

## Relay join codes: what the user has to do

The Relay path is written (`Session.cs`) and switched on by the "Host (join code through Relay)"
button. It needs this project linked to a Unity Cloud project, which only the signed-in user can do:

1. In the editor: *Edit > Project Settings > Services*. Sign in, pick your organization, and
   create a new cloud project (or link an existing one). This writes a project id into
   `ProjectSettings/ProjectSettings.asset`.
2. In the Unity Cloud dashboard (cloud.unity.com), open that project, find **Relay** under
   Multiplayer, and enable it.
3. Commit the `ProjectSettings` change.
4. Test: press Play, click "Host (join code through Relay)". The lobby shows a join code. In a
   second instance, type the code in the box and click Join.

Until step 1 is done the button reports "this project is not linked to a Unity Cloud project yet".
Sign-in is anonymous, with a separate profile per process, so several instances on one machine
count as different players. **This path has never connected**, so expect to fix something.

## Testing with several players on one machine

- **The way the stations were played and checked**: press Play in the editor, click in the Game
  view, click "Host (direct)". Run `Builds\rrp_game\rrp_game.exe` and click Join (the box already
  says 127.0.0.1). Anyone can join at any time.
- **Builds**: *RRP > Build Windows Player* writes `Builds/rrp_game/rrp_game.exe`. This is also the
  build to send to other players. It has to be rebuilt after every code change, or host and client
  will not agree.
- **Multiplayer Play Mode** (clones inside the editor) was not used for the stations. It should
  still work: *Window > Multiplayer > Multiplayer Play Mode*, tick Players 2 to 4.
- **Test flags**: see the top of `Assets/Scripts/AutoTest.cs`. A client that joins, puts three
  stakes on the hillside, clicks 150 times across the plots and logs its state every second:
  `rrp_game.exe -rrpJoin 127.0.0.1 -rrpLog -rrpStakes -rrpClicks 150 -logFile client.log`.
  Clones in the editor read the same flags from `rrp_autotest.txt` in the system temp folder; delete
  that file when done or the clones will keep joining by themselves.
- **Test flags on a map**: `-rrpMap 3` makes a host choose map 3. On a map `-rrpStakes` stakes a
  road from town A to town B, `-rrpClicks 300` clicks at random along it, and `-rrpWork 50`
  grades and gravels all of it at the click cap with half the clicks on the hot spot, printing an
  `RRPWORK` line with the time each took.
- **Controls**: WASD, mouse, Shift toggles sprint, Space hops, Tab frees the mouse, F1 tuning
  (host), F3 readout. Keys 1, 2 and 3 are the stake, grade and gravel tools. The host picks the
  map with the buttons at the top of the screen.
- **Driving the editor from a script**: play mode does not start ticking until the editor window
  has had focus once. After that it keeps running in the background.
- With clones attached, each clone also registers with the Unity MCP server, so every MCP call has
  to name an instance.
