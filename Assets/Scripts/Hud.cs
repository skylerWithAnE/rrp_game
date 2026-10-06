using System.Reflection;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// All on-screen text and buttons, drawn with IMGUI: the menu, the lobby, the debug readout (F3)
// and the tuning panel with the stress-test buttons (F1).
public class Hud : MonoBehaviour
{
    public static bool Playing;      // the mouse drives the camera and the shovel
    public static bool ShowReadout = true;
    public static bool ShowPanel;
    public static bool ShowGuide = true;    // the box that says what this ground is for and which keys work it; H hides it

    string joinText = "127.0.0.1";
    string roadSays = "";       // what the last "stake and finish" staked
    Vector2 scroll;
    readonly StringBuilder text = new StringBuilder();
    GUIStyle label, title, box;
    float scale = 1;

    void Update()
    {
        var g = Game.I;
        var kb = Keyboard.current;
        bool inGame = g.phase == Phase.Lobby || g.phase == Phase.Job;
        if (kb != null && inGame)
        {
            if (kb.f1Key.wasPressedThisFrame) { ShowPanel = !ShowPanel; if (ShowPanel) Playing = false; }
            if (kb.f3Key.wasPressedThisFrame) ShowReadout = !ShowReadout;
            if (kb.hKey.wasPressedThisFrame) ShowGuide = !ShowGuide;
            if (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) Playing = !Playing;
            if (kb.enterKey.wasPressedThisFrame && g.won) g.BackToLobby();
            // one tool in the hands at a time (in a vehicle the number keys change seats instead: see below)
            int held = Plot.Tool;
            if (kb.digit1Key.wasPressedThisFrame) Plot.Tool = Plot.Stakes;
            if (kb.digit2Key.wasPressedThisFrame) Plot.Tool = Plot.Grade;
            if (kb.digit3Key.wasPressedThisFrame) Plot.Tool = Plot.Gravel;
            if (kb.digit4Key.wasPressedThisFrame) Plot.Tool = Plot.Zone;
            if (kb.digit5Key.wasPressedThisFrame) Plot.Tool = Plot.Pave;
            if (kb.digit6Key.wasPressedThisFrame) Plot.Tool = Plot.Paint;
            if (kb.digit7Key.wasPressedThisFrame) Plot.Tool = Plot.Dev;
            if (kb.digit8Key.wasPressedThisFrame) Plot.Tool = Plot.Brush;
            if (kb.digit9Key.wasPressedThisFrame) Plot.Tool = Plot.DropTool;
            if (kb.tKey.wasPressedThisFrame) Plot.Tool = Plot.TarSpray;
            if (kb.gKey.wasPressedThisFrame) Plot.Tool = Plot.Grinder;
            if (Cars.Mine >= 0) Plot.Tool = held;
        }
        if (!inGame) { Playing = false; ShowPanel = false; }
        Cursor.lockState = Playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !Playing;
    }

    void OnGUI()
    {
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            label.normal.textColor = Color.white;
            title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold };
            box = new GUIStyle(GUI.skin.box);
        }
        scale = Mathf.Max(1f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
        float width = Screen.width / scale, height = Screen.height / scale;

        var g = Game.I;
        if (g.phase == Phase.Menu || g.phase == Phase.Connecting) Menu(g, width, height);
        else
        {
            Rect panel = new Rect(width - 370, 10, 360, height - 20);
            Labels(g);
            if (ShowPanel) Panel(g, panel);
            if (ShowReadout) Readout(g);
            if (ShowGuide) Guide(g, height);
            if (g.phase == Phase.Lobby) Lobby(g, width);
            if (Playing) GUI.Label(new Rect(width / 2 - 5, height / 2 - 11, 20, 20), "+", label);
            // under the crosshair: why the red rope cannot be made, or what a click here would do
            if (Playing && Plot.WhyFrame >= Time.frameCount - 1 && Plot.Why.Length > 0)
                GUI.Label(new Rect(width / 2 - 380, height / 2 + 24, 760, 110), (Plot.WhyBad ? "<color=#ff9080>" : "<color=#ffffff>") + Plot.Why + "</color>", new GUIStyle(label) { alignment = TextAnchor.UpperCenter, wordWrap = true });
            if (g.phase == Phase.Job) Clock(g, width, height);
            if (g.phase == Phase.Job && g.local != null) Hint(g, width, height);
            else GUI.Label(new Rect(width / 2 - 150, height - 30, 300, 22), "Click to play.  Tab frees the mouse.", label);

            // a click on the world (not on a button or the panel) grabs the mouse
            var e = Event.current;
            if (e.type == EventType.MouseDown && !Playing && !(ShowPanel && panel.Contains(e.mousePosition)))
            {
                Playing = true;
                e.Use();
            }
        }
    }

    void Menu(Game g, float width, float height)
    {
        GUILayout.BeginArea(new Rect(width / 2 - 170, height / 2 - 150, 340, 320), box);
        GUILayout.Label("rrp_game", title);
        GUILayout.Space(8);
        GUI.enabled = g.phase == Phase.Menu && !Session.Busy;
        if (GUILayout.Button("Host (join code through Relay)", GUILayout.Height(30))) g.Host(true);
        if (GUILayout.Button("Host (direct: this machine or LAN)", GUILayout.Height(30))) g.Host(false);
        GUILayout.Space(12);
        GUILayout.Label("Join code, or the host's address:", label);
        joinText = GUILayout.TextField(joinText, 40);
        if (GUILayout.Button("Join", GUILayout.Height(30))) g.Join(joinText);
        GUI.enabled = true;
        GUILayout.Space(8);
        if (g.phase == Phase.Connecting)
        {
            GUILayout.Label(g.status, label);
            if (GUILayout.Button("Cancel")) g.Leave();
        }
        else
        {
            if (Session.Error.Length > 0) GUILayout.Label("<color=#ff8080>" + Session.Error + "</color>", label);
            if (g.status.Length > 0) GUILayout.Label(g.status, label);
        }
        GUILayout.EndArea();
    }

    static string Name(byte mat)
    {
        switch (Cubes.Kind(mat))
        {
            case Cubes.Rock: return "rock";
            case Cubes.Oil: return "oil";
            case Cubes.Paint: return "paint";
            default: return "sand";
        }
    }

    // What a smack on this cube would do, by the same rules the host uses.
    static string SmackResult(Game g, Cubes.Cube cube)
    {
        var ground = g.ground;
        Vector3 at = cube.go.transform.position;
        int i = ground.NearestPoint(at);
        int kind = Cubes.Kind(cube.mat);
        byte under = ground.surface[i];
        if (ground.Stacked(i) && at.y > ground.blockTop[i]) return kind == Cubes.Sand ? "packs down on top of the blocks" : "FAILS: only sand packs on blocks";
        if (kind == Cubes.Paint) return under >= Ground.Asphalt ? "paints the road (this is what wins)" : "paints the ground (looks only)";
        if (kind == Cubes.Sand) return under == Ground.Bare ? "packs into the ground" : "FAILS: there is road here";
        if (kind == Cubes.Rock)
        {
            if (under != Ground.Bare) return "FAILS: already surfaced";
            return ground.IsFlat(i) ? "becomes GRAVEL" : "FAILS: ground is not flat (smack bare ground to flatten)";
        }
        if (cube.bit) return "FAILS: scoop up all five bits first";
        return under == Ground.Gravel ? "becomes ASPHALT" : "FAILS and splits: oil needs gravel under it";
    }

    // Two lines under the crosshair: what each mouse button will do right now.
    void Hint(Game g, float width, float height)
    {
        var p = g.local;
        string left, right;
        if (p.load == 0)
        {
            var cube = g.cubes.Nearest(p.target + Vector3.up * Ground.Cell * 0.5f, g.tuning.pickRadius);
            if (p.aim.kind == Aim.Block) left = g.blocks.Permanent(p.aim.cell) ? "town block, cannot be moved" : "pick the block up (it becomes a rock cube)";
            else if (cube != null) left = "scoop up the " + Name(cube.mat) + " cube";
            else left = p.aim.kind == Aim.Earth ? "dig earth off the blocks" : "dig";
            if (cube != null) right = "smack the " + Name(cube.mat) + " cube: " + SmackResult(g, cube);
            else right = p.aim.kind == Aim.Ground ? "flatten the ground" : p.aim.kind == Aim.Block ? "nothing (a block cannot be smacked; pick it up and fling it)" : "nothing";
        }
        else if (p.bits > 0)
        {
            left = "scoop another oil bit (" + p.bits + " of 5)";
            right = "set the bits down";
        }
        else
        {
            bool rock = Cubes.Kind((byte)p.load) == Cubes.Rock;
            left = "FLING the " + Name((byte)p.load) + " cube" + (rock ? " (loose rock can be smacked into gravel)" : "");
            right = rock ? "set it down as a fixed BLOCK (for walls and roofs)" : "set the cube down";
        }
        var style = new GUIStyle(label) { alignment = TextAnchor.UpperCenter };
        GUI.Label(new Rect(width / 2 - 350, height * 0.76f, 700, 44), "<b>Left</b>: " + left + "\n<b>Right</b>: " + right, style);
    }

    static string Time_(float seconds) { return (int)(seconds / 60) + ":" + ((int)seconds % 60).ToString("00"); }

    // The score is the time to finish, so the clock is always on screen during a job.
    void Clock(Game g, float width, float height)
    {
        var centred = new GUIStyle(title) { alignment = TextAnchor.UpperCenter };
        centred.normal.textColor = g.won ? new Color(1f, 0.9f, 0.2f) : Color.white;
        GUI.Label(new Rect(width / 2 - 200, 6, 400, 40), Time_(g.jobTime), centred);
        if (!g.won) return;
        GUI.Label(new Rect(width / 2 - 300, height * 0.3f, 600, 44), "THE ROAD IS OPEN", centred);
        var small = new GUIStyle(label) { alignment = TextAnchor.UpperCenter, fontSize = 18 };
        string line = "Finished in " + Time_(g.jobTime) + ".   Best on this machine: " + Time_(g.bestTime) + ".";
        if (Net.IsHost) line += "\nPress Enter to take everyone back to the lobby.";
        GUI.Label(new Rect(width / 2 - 300, height * 0.3f + 46, 600, 60), line, small);
    }

    void Lobby(Game g, float width)
    {
        GUILayout.BeginArea(new Rect(width / 2 - 260, 10, 560, Net.IsHost ? 216 : 60), box);
        GUILayout.Label("<b>" + Plot.MapNames[g.map] + "</b>   " + g.PlayerCount + " of " + Session.MaxPlayers + " players", label);
        if (Net.IsHost)
        {
            // the host picks the map; everyone starts again on it
            // the test grounds on one row, the maps on the other
            // and the grounds built for the slices of 2026-10-06 on a row of their own
            for (int row = 0; row < 3; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(row == 0 ? "Tests" : row == 1 ? "New" : "Maps", label, GUILayout.Width(40));
                for (int i = 0; i < Plot.MapNames.Length; i++)
                {
                    if ((Plot.LandMap(i) ? 2 : i >= Plot.FirstNewMap ? 1 : 0) != row) continue;
                    GUI.enabled = i != g.map;
                    if (GUILayout.Button(i == 2 ? "Middle " + Mathf.RoundToInt(g.tuning.mapDistance) : Plot.MapButtons[i])) g.SetMap(i);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            // [Claude] the planet's gravity at a click; the slider on the F1 panel is the same number
            GUILayout.BeginHorizontal();
            GUILayout.Label("Gravity", label, GUILayout.Width(52));
            for (int k = 0; k < 3; k++)
            {
                float gravity = k == 0 ? 1f : k == 1 ? 0.38f : 0.16f;
                GUI.enabled = Mathf.Abs(g.tuning.planetGravity - gravity) > 0.005f;
                if (GUILayout.Button(k == 0 ? "Earth 1.00" : k == 1 ? "Mars 0.38" : "Moon 0.16")) { g.tuning.planetGravity = gravity; g.TuningChanged(); }
            }
            GUI.enabled = true;
            GUILayout.Label("now " + g.tuning.planetGravity.ToString("0.00"), label, GUILayout.Width(70));
            if (g.map == Plot.SpinMap && GUILayout.Button("Loosen the gravel again")) { g.plots[Plot.SpinOut].HostLoosen(); g.plots[Plot.SpinOut2].HostLoosen(); }
            GUILayout.EndHorizontal();
            // on a map: a road at a button, to watch trucks on, and the switch for wear
            GUILayout.BeginHorizontal();
            if (Plot.LandMap(g.map))
            {
                if (GUILayout.Button("Stake and finish this road")) roadSays = g.plots[Plot.Land].HostAutoRoad();
                bool wears = g.tuning.mapWear >= 0.5f;
                if (GUILayout.Button(wears ? "Wear on maps: ON" : "Wear on maps: off", GUILayout.Width(140))) { g.tuning.mapWear = wears ? 0 : 1; g.TuningChanged(); }
            }
            // [Claude] fast forward, for watching a road wear out
            GUILayout.Label("Speed", label, GUILayout.Width(52));
            foreach (int pace in new[] { 1, 4, 10 })
            {
                GUI.enabled = Mathf.Abs(g.tuning.fastForward - pace) > 0.01f;
                if (GUILayout.Button("x" + pace, GUILayout.Width(40))) { g.tuning.fastForward = pace; g.TuningChanged(); }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }
        if (Net.IsHost)
        {
            if (Session.JoinCode.Length > 0) GUILayout.Label("Join code: <b>" + Session.JoinCode + "</b>", label);
            else GUILayout.Label("Direct host on port " + Session.Port + " (same machine: 127.0.0.1)", label);
        }
        if (GUILayout.Button("Leave")) g.Leave();
        GUILayout.EndArea();
    }

    void Readout(Game g)
    {
        var nm = g.nm;
        text.Clear();
        text.Append("<b>").Append(Net.IsHost ? "HOST" : "CLIENT").Append("</b>  player ").Append(g.localSlot + 1).Append('\n');
        text.Append("host fps ").Append(Mathf.RoundToInt(g.hostFps));
        if (!Net.IsHost) text.Append("   my fps ").Append(Mathf.RoundToInt(g.fps));
        text.Append("   gravity ").Append(g.tuning.planetGravity.ToString("0.00")).Append(" (").Append(Game.PlanetName(g.tuning.planetGravity)).Append(")\n");
        // the readout is about whichever plot the player is on or nearest
        Plot near = null;
        if (g.local != null)
            foreach (var candidate in g.plots)
                if (candidate.Ready && (near == null || candidate.Distance(g.local.transform.position) < near.Distance(g.local.transform.position))) near = candidate;
        foreach (var plot in g.plots)
            if (plot == near) text.Append("<size=12>").Append(Plot.Names[plot.id]).Append(": level ").Append(Mathf.FloorToInt(plot.roadShare * 100f)).Append("  shoulder ").Append(Mathf.FloorToInt(plot.shoulderShare * 100f))
                .Append("  gravel ").Append(Mathf.FloorToInt(plot.gravelShare * 100f)).Append("  packed ").Append(Mathf.FloorToInt(plot.packedShare * 100f)).Append(" %   clicks ").Append(plot.clicks).Append("</size>\n");
        if (near != null && near.IsLand && roadSays.Length > 0 && Net.IsHost) text.Append("<size=11>staked for you: ").Append(roadSays).Append("</size>\n");
        if (near != null && near.IsLand) text.Append("<size=12>road staked from town A: ").Append(Mathf.RoundToInt(near.roadLength)).Append(" m   towns joined: ").Append(near.joined ? "<b>YES</b>" : "no").Append("</size>\n");
        if (near != null && near.IsQuarry) text.Append("<size=12>gravel on the heap: ").Append(near.stock).Append(" clicks' worth").Append((near.carrying & 1 << g.localSlot) != 0 ? "   <b>shovel loaded</b>" : "").Append("</size>\n");
        if (near != null && near.Wears) text.Append("<size=12>wear: ruts and holes in ").Append((near.rutShare * 100f).ToString("0.0")).Append(" % of the road, the deepest ").Append(Mathf.RoundToInt(near.deepest * 100f)).Append(" cm</size>\n");
        if (near != null && near.lines != null)
        {
            // The judging of paint drawn by hand. It is shown here and nowhere else: the player is not meant to see it.
            Vector3 spot = Lines.AimFrame >= Time.frameCount - 2 ? Lines.Aim : g.local.transform.position;
            text.Append("<size=12>paint, ").Append(near.lines.marked ? "marked" : "unmarked").Append(" strip: <b>").Append(Mathf.RoundToInt(near.lines.total)).Append("</b> of 100");
            if (near.lines.SquareScore(spot.x, spot.z, out float score, out float line, out float stray))
                text.Append("\nthis square: <b>").Append(Mathf.RoundToInt(score)).Append("</b>   line ").Append(Mathf.RoundToInt(line)).Append(" %   astray ").Append(Mathf.RoundToInt(stray)).Append(" %");
            text.Append("</size>\n");
        }
        if (near != null) g.lorries.Readout(text, near.id);
        text.Append("holding: <b>").Append(Plot.Tool == Plot.Stakes ? "1 stakes" : Plot.Tool == Plot.Grade ? "2 grade" : Plot.Tool == Plot.Zone ? "4 zoning" : Plot.Tool == Plot.Dev ? "7 DEV: finish a section" : Plot.Tool == Plot.Pave ? "5 asphalt" : Plot.Tool == Plot.Paint ? "6 paint lines" : Plot.Tool == Plot.Brush ? "8 paint brush" : Plot.Tool == Plot.DropTool ? "9 drop-off survey" : Plot.Tool == Plot.TarSpray ? "T tar spray" : Plot.Tool == Plot.Grinder ? "G grinder" : "3 gravel").Append("</b>   sprint ").Append(Player.Sprinting ? "ON" : "off").Append(Player.Flying ? "   <b>FLYING</b>" : "").Append('\n');
        if (g.phase == Phase.Job) text.Append("towns joined by asphalt: ").Append(g.road.asphaltLinked ? "YES" : "no").Append("   by paint: ").Append(g.road.paintedLinked ? "YES" : "no").Append('\n');
        if (g.phase == Phase.Job) text.Append("truck: ").Append(g.truck.alive ? "driving" : "none").Append('\n');

        if (Net.IsHost)
        {
            float total = 0;
            for (int s = 1; s < g.players.Length; s++)
            {
                if (g.players[s] == null) continue;
                ulong id = g.clientOf[s];
                if (!Net.Peers.TryGetValue(id, out var peer)) continue;
                total += peer.sentRate;
                text.Append("P").Append(s + 1).Append("  out ").Append(Kb(peer.sentRate)).Append("  in ").Append(Kb(peer.receivedRate));
                text.Append("  rtt ").Append(g.utp.GetCurrentRtt(id)).Append(" ms\n");
            }
            text.Append("total out ").Append(Kb(total)).Append('\n');
        }
        else if (Net.Peers.TryGetValue(NetworkManager.ServerClientId, out var host))
        {
            text.Append("from host ").Append(Kb(host.receivedRate)).Append("  to host ").Append(Kb(host.sentRate));
            text.Append("  rtt ").Append(g.utp.GetCurrentRtt(NetworkManager.ServerClientId)).Append(" ms\n");
        }
        text.Append("<size=11>WASD walk   Shift sprint on/off   Space hop   H the guide\n1 stakes  2 grade  3 gravel  4 zoning  5 asphalt  6 paint\n8 brush  9 drop-off  T tar  G grinder\nE vehicle, C its camera   right click sends a waiting truck</size>");

        // as tall as what it says
        var wrapped = new GUIStyle(label) { wordWrap = true };
        float tall = wrapped.CalcHeight(new GUIContent(text.ToString()), 350);
        readoutRect = new Rect(8, 8, 360, tall + 10);
        GUI.Box(readoutRect, GUIContent.none, box);
        GUI.Label(new Rect(16, 12, 350, tall + 4), text.ToString(), wrapped);
    }

    // What this ground is for and how to work it, in a box at the bottom left. This is where
    // the instructions are; the signs out in the world only name things.
    static string GuideFor(Game g)
    {
        string host = Net.IsHost ? "" : " (the host's)";
        switch (g.map)
        {
            case Plot.YardMap: return "<b>Scale yard</b>\nSizes against the 1.6 m blob: the truck, a strip of road, and three hairpins. Nothing here can be changed.";
            case Plot.BuildingMap: return "<b>Building roads</b>\nFour pieces of ground, one for each tool.\n<b>1 Stakes</b>: left click the ground to put a stake down, roped to the last. Wheel moves the chosen rope, X pulls the stake out, right click lets go.\n<b>2 Grade</b>: hold left click on staked ground to bring it to the rope.\n<b>3 Gravel</b>: hold left click on graded road to lay gravel; keep going to pack it.\nA click on the yellow ring counts double.";
            case Plot.TrucksMap: return "<b>Trucks</b>\nWhat a truck can and cannot drive. Nothing to do but watch.\nOn your right, a finished road. On your left, a bad one; beyond it the first wear road, and then four ramps: 14 and 20 degrees, bare, loose and packed.\nBehind you: the tightest turn, rough and finished.\nTry the Gravity buttons above" + host + ".";
            case Plot.JunctionsMap: return "<b>Junctions</b>\nA T, a crossroads and a fork, finished, with trucks on them. And a field with a road across it, to build one:\n<b>1 Stakes</b>: click a stake in the middle of the road, then the ground to one side. A branch must leave 60 degrees or more from the other ropes.";
            case Plot.QuarryMap: return "<b>Quarry and gravel delivery</b>\n1. <b>1 Stakes</b>: carry the bare road on from its end by two stakes.\n2. <b>9 Drop-off</b>: left click the stake that was the end, then the ground beside the road.\n3. Down in the pit, <b>3</b>: click the rock, then the truck. At 12 shovels it leaves by itself; right click sends it sooner.\n4. It backs into the drop-off. <b>3</b>: click it and say where the heap goes, then click the truck and click the heap.\n5. Empty, it drives home. Gravel laid here comes off the heap.\n<b>4</b> zoning: left click a section to make it a service road or open it.";
            case Plot.PavingMap: return "<b>Paving and painting</b>\nThe dump truck tips asphalt down each lane by itself.\n<b>5 Asphalt</b>: hold left click to spread what it left. Or right click the roller to send it over.\n<b>6 Paint</b>: hold left click on rolled asphalt to paint a square's lines.\n<b>8 Brush</b>: left click white, right click yellow.\nBeside it is the same road finished, with trucks.";
            case Plot.DrivingMap: return "<b>Driving</b>\nFour vehicles, to your right, and a road in four states ahead.\n<b>E</b> beside a vehicle gets in, <b>E</b> again gets out.\n<b>W S</b> drive, <b>A D</b> steer. <b>C</b> switches the camera between behind the vehicle and the seat.\nThe pick-up seats four: <b>1 to 4</b> change seat.\nThe roller packs gravel and rolls asphalt. The loader: <b>R F</b> bucket, fill at the gravel pile, left click tips. The paint truck: left click paints.";
            case Plot.WearMap: return "<b>Wear</b>\nFour roads on high ground, a truck from each end every few seconds: dirt, gravel, and each again with a paved way in. Paved road does not wear.\nThe signs over each road count its trucks.\n<b>Speed</b> above" + host + " runs it faster. <b>V</b> flies.\n<b>2</b> and <b>3</b> mend a road.\nSliders (F1, under Wear): wearByLanding, wearDirt, wearGravel, wearCut, wearDeepest.";
            case Plot.SpinMap: return "<b>Spin-out</b>\nLoose gravel makes trucks slide and spin. They pack it as they drive, and it gets safer.\n<b>Road 1</b>, to your left: packed, then loose gravel and a bend.\n<b>Road 2</b>, to your right: 40 m paved, 40 m of loose gravel, then the bend, so trucks arrive at speed.\n\"Loosen the gravel again\" above" + host + " starts both over.\nSliders (F1): looseGrip, looseFishtail, spinAngle, truckPacking.";
            case Plot.PaintMap: return "<b>Painting lines by hand</b>\nA white line inside each edge, a yellow one down the middle. The near strip marks where; the far one does not.\n<b>8 Roller brush</b>: hold left click and drag for white, right click for yellow.\n<b>T Tar spray</b>: hold left click to cover paint. Wide.\n<b>G Grinder</b>: hold left click to take paint off. Narrow.\n<b>Paint truck</b>: E gets in. One drives; whoever is in the bed has the nozzles on the mouse buttons. A driver alone has both.";
            default: return "<b>" + Plot.MapNames[g.map] + "</b>\nJoin town A (red) to town B (blue) with a road.\n<b>1 Stakes</b>: click town A's stake, then the ground toward B's pole, and rope the last stake to B's.\n<b>2 Grade</b>: hold left click on staked ground.\n<b>3 Gravel</b>: hold left click on graded road; trucks pack it.\nTrucks set off as soon as the towns are joined.\nOr \"Stake and finish this road\" above" + host + " builds it, to watch. \"Wear on maps\" is beside it.";
        }
    }

    void Guide(Game g, float height)
    {
        var style = new GUIStyle(label) { wordWrap = true, fontSize = 13 };
        string says = GuideFor(g) + "\n<size=11>H hides this.   F1 sliders, F3 readout, Tab frees the mouse.   7 finishes a section, V flies.</size>";
        float tall = style.CalcHeight(new GUIContent(says), 400);
        guideRect = new Rect(8, height - tall - 22, 416, tall + 14);
        GUI.Box(guideRect, GUIContent.none, box);
        GUI.Label(new Rect(16, height - tall - 16, 400, tall + 4), says, style);
    }

    // Signs: what each thing is, written over it. The nearest are drawn first and a sign that
    // would lie over one already drawn is left out, so they never pile up; a sign more than 30 m
    // away shows only its first line. Each has a dark board behind it.
    Rect readoutRect, guideRect;
    readonly System.Collections.Generic.List<Rect> drawn = new System.Collections.Generic.List<Rect>();
    readonly System.Collections.Generic.List<Vector4> signs = new System.Collections.Generic.List<Vector4>();
    readonly System.Collections.Generic.List<string> signText = new System.Collections.Generic.List<string>();
    void Labels(Game g)
    {
        if (Event.current.type != EventType.Repaint) return;
        var cam = g.cam.GetComponent<Camera>();
        var style = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = 13 };
        signs.Clear();
        signText.Clear();
        foreach (var l in g.yard.labels) Sign(cam, l.at, l.text);
        foreach (var plot in g.plots)
            foreach (var l in plot.labels) Sign(cam, l.at, l.text);
        for (int i = 0; i < g.cars.cars.Length; i++)
            if (i != Cars.Mine) Sign(cam, g.cars.cars[i].body.transform.position + Vector3.up * (g.cars.cars[i].size.y + 1f), g.cars.Sign(i));
        // nearest first
        var order = new int[signs.Count];
        for (int i = 0; i < order.Length; i++) order[i] = i;
        System.Array.Sort(order, (a, b) => signs[a].z.CompareTo(signs[b].z));
        // no sign goes over the host's bar, the readout or the guide
        drawn.Clear();
        if (g.phase == Phase.Lobby) drawn.Add(new Rect(Screen.width / scale / 2 - 260, 10, 560, Net.IsHost ? 216 : 60));
        if (ShowReadout) drawn.Add(readoutRect);
        if (ShowGuide) drawn.Add(guideRect);
        foreach (int i in order)
        {
            string text = signText[i];
            // a sign whose text starts with ! is read in full from any distance: the counts of trucks
            bool always = text.StartsWith("!");
            if (always) text = text.Substring(1);
            int line = text.IndexOf('\n');
            // the first line is the sign's name
            string says = line < 0 ? "<b>" + text + "</b>" : signs[i].z > 30f && !always ? "<b>" + text.Substring(0, line) + "</b>" : "<b>" + text.Substring(0, line) + "</b>" + text.Substring(line);
            var content = new GUIContent(says);
            float wide = Mathf.Min(460f, style.CalcSize(content).x + 4f), tall = style.CalcHeight(content, wide);
            var rect = new Rect(signs[i].x / scale - wide * 0.5f - 8f, (Screen.height - signs[i].y) / scale - tall * 0.5f - 5f, wide + 16f, tall + 10f);
            // a sign for something near the edge of the view is kept whole, on the screen
            rect.x = Mathf.Clamp(rect.x, 4f, Mathf.Max(4f, Screen.width / scale - rect.width - 4f));
            rect.y = Mathf.Clamp(rect.y, 4f, Mathf.Max(4f, Screen.height / scale - rect.height - 4f));
            bool covered = false;
            foreach (var other in drawn) covered |= other.Overlaps(rect);
            if (covered) continue;
            drawn.Add(rect);
            GUI.color = new Color(0, 0, 0, 0.55f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 8f, rect.y + 5f, wide, tall), says, style);
        }
    }

    void Sign(Camera cam, Vector3 at, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Vector3 s = cam.WorldToScreenPoint(at);
        if (s.z < 0.5f || s.z > 70f) return;
        signs.Add(new Vector4(s.x, s.y, s.z, 0));
        signText.Add(text);
    }

    static string Kb(float bytesPerSecond) { return (bytesPerSecond / 1024f).ToString("0.0") + " kB/s"; }

    // One slider per Tuning field. Only the host can change them; clients see the host's values.
    void Panel(Game g, Rect rect)
    {
        GUILayout.BeginArea(rect, box);
        bool host = Net.IsHost;
        GUILayout.Label(host ? "<b>Tuning</b> (applies to everyone, live)" : "<b>Tuning</b> (set by the host)", label);
        scroll = GUILayout.BeginScrollView(scroll);

        if (host && g.phase == Phase.Job)
        {
            GUILayout.Label("<b>Cube tests</b>", label);
            GUILayout.BeginHorizontal();
            foreach (int n in new[] { 100, 200, 400, 800 })
                if (GUILayout.Button("+" + n)) g.cubes.SpawnPile(n, PileSpot(g));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Avalanche")) g.cubes.Avalanche();
            if (GUILayout.Button("Earthquake")) g.quake.Trigger();
            if (GUILayout.Button("Clear cubes")) g.cubes.RemoveAll();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rock")) Give(g, Cubes.Rock);
            if (GUILayout.Button("Oil")) Give(g, Cubes.Oil);
            if (GUILayout.Button("Paint")) Give(g, Cubes.PaintOf(Time.frameCount % Mats.PaintColors.Length));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(AutoTest.I.stressRunning ? "Running..." : "Run stress series")) AutoTest.I.RunStress();
            if (GUILayout.Button("Back to lobby")) g.BackToLobby();
            GUILayout.EndHorizontal();
        }

        // on a map this makes new land from a new seed; choosing the map again brings the first back
        if (host && GUILayout.Button(Plot.LandMap(g.map) ? "Make new land for this map" : "Make all the ground again")) g.SetMap(g.map, true);
        if (host && g.map == Plot.BuildingMap)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Send trucks: clicking")) g.lorries.Send(0);
            if (GUILayout.Button("hillside")) g.lorries.Send(1);
            if (GUILayout.Button("gravel")) g.lorries.Send(2);
            GUILayout.EndHorizontal();
        }

        GUI.enabled = host;
        bool changed = false, old = false;
        foreach (FieldInfo f in Tuning.Fields)
        {
            var header = f.GetCustomAttribute<HeaderAttribute>();
            if (header != null) old = header.header.StartsWith("Old");
            if (old) continue;  // the first prototype's numbers: switched off
            if (header != null) GUILayout.Label("<b>" + header.header + "</b>", label);
            var range = f.GetCustomAttribute<RangeAttribute>();
            float value = (float)f.GetValue(g.tuning);
            GUILayout.BeginHorizontal();
            GUILayout.Label(f.Name, label, GUILayout.Width(120));
            float next = GUILayout.HorizontalSlider(value, range != null ? range.min : 0, range != null ? range.max : 10, GUILayout.Width(150));
            GUILayout.Label(next.ToString(next >= 10 ? "0" : "0.00"), label, GUILayout.Width(50));
            GUILayout.EndHorizontal();
            if (next != value)
            {
                f.SetValue(g.tuning, next);
                changed = true;
            }
        }
        GUI.enabled = true;
        if (changed) g.TuningChanged();

#if UNITY_EDITOR
        GUILayout.Space(6);
        if (host && GUILayout.Button("Save these numbers to the Tuning asset"))
        {
            var asset = Resources.Load<Tuning>("Tuning");
            if (asset != null)
            {
                UnityEditor.EditorUtility.CopySerialized(g.tuning, asset);
                UnityEditor.EditorUtility.SetDirty(asset);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // a test cube dropped at the crosshair
    static void Give(Game g, byte mat)
    {
        g.cubes.Spawn(mat, false, g.local.target + Vector3.up, Quaternion.identity, Vector3.zero);
    }

    // test piles land a little ahead of the host's blob
    static Vector3 PileSpot(Game g)
    {
        return g.ground.Clamp(g.local.transform.position + g.local.Forward * 6f, 4f);
    }
}
