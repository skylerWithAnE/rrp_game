using System;
using System.IO;
using UnityEngine;

// Test tooling, not game: lets an instance host, join and play by itself so several can be checked
// against each other on one machine. Off unless asked for on the command line:
//
//   -rrpHost            host a direct session
//   -rrpJoin <address>  join a direct session (retries until the host is up)
//   -rrpStart <n>       host: start the job once n players are in the lobby
//   -rrpBot             the local blob wanders and uses the shovel by itself
//   -rrpLog             print one RRPSTATE line every two seconds
//
// Multiplayer Play Mode clones do not get their own command line, so in the editor the same flags
// are also read from the file rrp_autotest.txt in the system temp folder, if it exists.
public class AutoTest : MonoBehaviour
{
    public static bool Bot, Log;
    string join;
    bool host;
    int startAt;
    float retry, logTimer, botTimer;
    Vector2 wander;
    int botStep;

    void Start()
    {
        string[] args = Environment.GetCommandLineArgs();
#if UNITY_EDITOR
        string file = Path.Combine(Path.GetTempPath(), "rrp_autotest.txt");
        if (File.Exists(file)) args = File.ReadAllText(file).Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        // in the editor the main window is driven by hand (or by script); only clones follow the file
        if (Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor) args = new string[0];
#endif
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-rrpHost") host = true;
            else if (args[i] == "-rrpJoin" && i + 1 < args.Length) join = args[++i];
            else if (args[i] == "-rrpStart" && i + 1 < args.Length) int.TryParse(args[++i], out startAt);
            else if (args[i] == "-rrpBot") Bot = true;
            else if (args[i] == "-rrpLog") Log = true;
        }
        if (host) Game.I.Host(false);
    }

    void Update()
    {
        var g = Game.I;
        if (join != null && g.phase == Phase.Menu)
        {
            retry -= Time.unscaledDeltaTime;
            if (retry <= 0) { retry = 2f; g.Join(join); }
        }
        if (host && startAt > 0 && g.phase == Phase.Lobby && g.PlayerCount >= startAt) g.StartJob();

        if (g.local != null) g.local.bot = Bot ? (Func<Controls>)BotControls : null;

        if (!Log) return;
        logTimer += Time.unscaledDeltaTime;
        if (logTimer < 2f) return;
        logTimer = 0;
        Debug.Log(State());
    }

    // Wander near the spawn side of the hill, turning now and then, digging and flinging.
    Controls BotControls()
    {
        var g = Game.I;
        var p = g.local;
        botTimer -= Time.deltaTime;
        if (botTimer <= 0)
        {
            botTimer = UnityEngine.Random.Range(0.6f, 1.6f);
            botStep++;
            Vector3 home = new Vector3(g.ground.SizeX * 0.5f, 0, g.ground.SizeZ * 0.3f);
            Vector3 to = home - p.transform.position;
            to.y = 0;
            // head home when far away, otherwise pick a new heading
            if (to.magnitude > 8f) p.yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            else p.yaw += UnityEngine.Random.Range(-120f, 120f);
            wander = botStep % 3 == 0 ? Vector2.zero : new Vector2(0, 1);
        }
        var c = new Controls { move = wander };
        if (g.phase == Phase.Job)
        {
            // dig while standing; fling or set down whatever was picked up
            c.primary = wander == Vector2.zero || p.load != 0;
            c.secondary = botStep % 7 == 0 && !c.primary;
        }
        return c;
    }

    public static string State()
    {
        var g = Game.I;
        var s = new System.Text.StringBuilder("RRPSTATE ");
        s.Append(Net.IsHost ? "host" : "client").Append(" slot=").Append(g.localSlot).Append(" phase=").Append(g.phase);
        s.Append(" players=");
        foreach (var p in g.players)
            if (p != null) s.Append(p.slot).Append(':').Append(p.transform.position.ToString("0.0")).Append("load").Append(p.load).Append(' ');
        if (g.ground.Ready) s.Append(" ground=").Append(g.ground.Hash().ToString("x8"));
        s.Append(" cubes=").Append(g.cubes.Loose).Append(" moving=").Append(Net.IsHost ? g.cubes.Moving : g.cubes.clientMoving);
        s.Append(" rest=").Append(g.cubes.RestHash().ToString("x8"));
        s.Append(" quakes=").Append(g.quake.count);
        s.Append(" fps=").Append(Mathf.RoundToInt(g.fps)).Append(" hostfps=").Append(Mathf.RoundToInt(g.hostFps));
        float sent = 0, received = 0;
        foreach (var peer in Net.Peers.Values) { sent += peer.sentRate; received += peer.receivedRate; }
        s.Append(" outKB=").Append((sent / 1024f).ToString("0.0")).Append(" inKB=").Append((received / 1024f).ToString("0.0"));
        return s.ToString();
    }
}
