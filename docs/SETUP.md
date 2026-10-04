# Setup

Machine: Windows 11. Project: `C:\Users\skyle\source\repos\rrp_game`. State as of 2026-10-04.

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
- If the tools did not load, the server can still be driven over plain HTTP (JSON-RPC `initialize`,
  then `tools/call`). That is how the 2026-10-04 session worked.
- Port 8080 was once held by **Wwise** (Audiokinetic). If the server will not start, close Wwise or
  change the port in *Window > MCP for Unity* and click Configure for Claude Code again.

## To do (user)

1. Create and link a Unity Cloud project for Relay join codes. See "Relay" in `TECH_PLAN.md` for
   the exact steps.
