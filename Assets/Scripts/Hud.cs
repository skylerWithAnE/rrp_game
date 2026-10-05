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

    string joinText = "127.0.0.1";
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
            if (kb.escapeKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame) Playing = !Playing;
            if (kb.enterKey.wasPressedThisFrame && g.won) g.BackToLobby();
            // one tool in the hands at a time
            if (kb.digit1Key.wasPressedThisFrame) Plot.Tool = Plot.Stakes;
            if (kb.digit2Key.wasPressedThisFrame) Plot.Tool = Plot.Grade;
            if (kb.digit3Key.wasPressedThisFrame) Plot.Tool = Plot.Gravel;
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
            if (g.phase == Phase.Lobby) Lobby(g, width);
            if (Playing) GUI.Label(new Rect(width / 2 - 5, height / 2 - 11, 20, 20), "+", label);
            // under the crosshair: why the red rope cannot be made
            if (Playing && Plot.Tool == Plot.Stakes && Plot.WhyFrame >= Time.frameCount - 1 && Plot.Why.Length > 0)
                GUI.Label(new Rect(width / 2 - 250, height / 2 + 24, 500, 22), "<color=#ff9080>" + Plot.Why + "</color>", new GUIStyle(label) { alignment = TextAnchor.UpperCenter });
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
        GUILayout.BeginArea(new Rect(width / 2 - 220, 10, 440, Net.IsHost ? 132 : 60), box);
        GUILayout.Label("<b>" + Plot.MapNames[g.map] + "</b>   " + g.PlayerCount + " of " + Session.MaxPlayers + " players", label);
        if (Net.IsHost)
        {
            // the host picks the map; everyone starts again on it
            // the test grounds on one row, the maps on the other
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(row == 0 ? "Tests" : "Maps", label, GUILayout.Width(40));
                for (int i = 0; i < Plot.MapNames.Length; i++)
                {
                    if (Plot.LandMap(i) != (row == 1)) continue;
                    GUI.enabled = i != g.map;
                    if (GUILayout.Button(i == 2 ? "Middle " + Mathf.RoundToInt(g.tuning.mapDistance) : Plot.MapButtons[i])) g.SetMap(i);
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
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
        text.Append('\n');
        // the readout is about whichever plot the player is on or nearest
        Plot near = null;
        if (g.local != null)
            foreach (var candidate in g.plots)
                if (candidate.Ready && (near == null || candidate.Distance(g.local.transform.position) < near.Distance(g.local.transform.position))) near = candidate;
        foreach (var plot in g.plots)
            if (plot == near) text.Append("<size=12>").Append(Plot.Names[plot.id]).Append(": level ").Append(Mathf.FloorToInt(plot.roadShare * 100f)).Append("  shoulder ").Append(Mathf.FloorToInt(plot.shoulderShare * 100f))
                .Append("  gravel ").Append(Mathf.FloorToInt(plot.gravelShare * 100f)).Append("  packed ").Append(Mathf.FloorToInt(plot.packedShare * 100f)).Append(" %   clicks ").Append(plot.clicks).Append("</size>\n");
        if (near != null && near.IsLand) text.Append("<size=12>road staked from town A: ").Append(Mathf.RoundToInt(near.roadLength)).Append(" m   towns joined: ").Append(near.joined ? "<b>YES</b>" : "no").Append("</size>\n");
        if (near != null) g.lorries.Readout(text, near.id);
        text.Append("holding: <b>").Append(Plot.Tool == Plot.Stakes ? "1 stakes" : Plot.Tool == Plot.Grade ? "2 grade" : "3 gravel").Append("</b>   sprint ").Append(Player.Sprinting ? "ON" : "off").Append('\n');
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
        text.Append("<size=11>F1 tuning   F3 readout   Tab mouse\nWASD walk   Shift sprint on/off   Space hop   1 2 3 tools\nstakes: left click places or chooses, wheel moves\nthe chosen rope, X removes, right click lets go</size>");

        GUI.Box(new Rect(8, 8, 360, 246), GUIContent.none, box);
        GUI.Label(new Rect(16, 12, 350, 241), text.ToString(), label);
    }

    // What each thing in the yard is and how big, written over it.
    void Labels(Game g)
    {
        var cam = g.cam.GetComponent<Camera>();
        var style = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        var all = new System.Collections.Generic.List<Yard.Label>(g.yard.labels);
        foreach (var plot in g.plots) all.AddRange(plot.labels);
        foreach (var l in all)
        {
            Vector3 s = cam.WorldToScreenPoint(l.at);
            if (s.z < 0.5f || s.z > 60f) continue;
            GUI.Label(new Rect(s.x / scale - 220, (Screen.height - s.y) / scale - 30, 440, 60), l.text, style);
        }
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
