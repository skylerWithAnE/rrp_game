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
            if (kb.enterKey.wasPressedThisFrame && g.phase == Phase.Lobby) g.StartJob();
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
            if (ShowPanel) Panel(g, panel);
            if (ShowReadout) Readout(g);
            if (g.phase == Phase.Lobby) Lobby(g, width);
            if (Playing) GUI.Label(new Rect(width / 2 - 5, height / 2 - 11, 20, 20), "+", label);
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

    void Lobby(Game g, float width)
    {
        GUILayout.BeginArea(new Rect(width / 2 - 170, 10, 340, 150), box);
        GUILayout.Label("Lobby: " + g.PlayerCount + " of " + Session.MaxPlayers + " players", label);
        if (Net.IsHost)
        {
            if (Session.JoinCode.Length > 0) GUILayout.Label("Join code: <b>" + Session.JoinCode + "</b>", label);
            else GUILayout.Label("Direct host on port " + Session.Port + " (same machine: 127.0.0.1)", label);
            if (GUILayout.Button("Start the job (Enter)", GUILayout.Height(28))) g.StartJob();
        }
        else GUILayout.Label("Waiting for the host to start the job.", label);
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
        text.Append("loose cubes ").Append(g.cubes.Loose).Append(" / ").Append((int)g.tuning.maxLooseCubes);
        text.Append("   moving ").Append(Net.IsHost ? g.cubes.Moving : g.cubes.clientMoving).Append('\n');
        text.Append("quake at ").Append((int)g.tuning.quakeThreshold).Append("   quakes ").Append(g.quake.count).Append('\n');
        if (g.phase == Phase.Job) text.Append("towns joined by asphalt: ").Append(g.road.asphaltLinked ? "YES" : "no").Append("   by paint: ").Append(g.road.paintedLinked ? "YES" : "no").Append('\n');
        if (g.phase == Phase.Job) text.Append("job time ").Append((int)(g.jobTime / 60)).Append(':').Append(((int)g.jobTime % 60).ToString("00")).Append('\n');

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
        text.Append("<size=11>F1 tuning   F3 readout   Tab mouse\nLMB scoop / fling   RMB smack / set down   Space hop</size>");

        GUI.Box(new Rect(8, 8, 330, 210), GUIContent.none, box);
        GUI.Label(new Rect(16, 12, 320, 205), text.ToString(), label);
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
            if (GUILayout.Button(AutoTest.I.stressRunning ? "Running..." : "Run stress series")) AutoTest.I.RunStress();
        }

        GUI.enabled = host;
        bool changed = false;
        foreach (FieldInfo f in Tuning.Fields)
        {
            var header = f.GetCustomAttribute<HeaderAttribute>();
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
