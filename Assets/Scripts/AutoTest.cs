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
//   -rrpBotTime <s>     bots stand still once the job is this many seconds old
//   -rrpLog             print one RRPSTATE line every second
//
// RunStress (also a button on the tuning panel) runs the cube experiment's pile and avalanche
// tests on the host and prints one RRPSTRESS line per test.
//
// Multiplayer Play Mode clones do not get their own command line, so in the editor the same flags
// are also read from the file rrp_autotest.txt in the system temp folder, if it exists.
public class AutoTest : MonoBehaviour
{
    public static bool Bot, Log;
    public static AutoTest I;
    public bool stressRunning;

    // measured since the last State() call
    static float worstMs, peakOut, sampleTime;
    static int sampleFrames;
    string join;
    float botTime;
    bool host;
    int startAt;
    float retry, logTimer, botTimer;
    Vector2 wander;
    int botStep;

    void Start()
    {
        I = this;
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
            else if (args[i] == "-rrpBotTime" && i + 1 < args.Length) float.TryParse(args[++i], out botTime);
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

        float dt = Time.unscaledDeltaTime;
        worstMs = Mathf.Max(worstMs, dt * 1000f);
        sampleTime += dt;
        sampleFrames++;
        peakOut = Mathf.Max(peakOut, MaxSentRate());

        if (!Log) return;
        logTimer += Time.unscaledDeltaTime;
        if (logTimer < 1f) return;
        logTimer = 0;
        Debug.Log(State());
    }

    // Wander near the spawn side of the hill, turning now and then, digging and flinging.
    Controls BotControls()
    {
        var g = Game.I;
        var p = g.local;
        if (botTime > 0 && g.phase == Phase.Job && g.jobTime > botTime) return default;
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
        s.Append(" truck=").Append(g.truck.alive ? g.truck.Position.ToString("0.0") : "none").Append(" won=").Append(g.won);
        s.Append(" road=").Append(g.road.asphaltLinked ? 'A' : '-').Append(g.road.paintedLinked ? 'P' : '-');
        s.Append(" fps=").Append(Mathf.RoundToInt(g.fps)).Append(" hostfps=").Append(Mathf.RoundToInt(g.hostFps));
        float sent = 0, received = 0;
        foreach (var peer in Net.Peers.Values) { sent += peer.sentRate; received += peer.receivedRate; }
        s.Append(" outKB=").Append((sent / 1024f).ToString("0.0")).Append(" inKB=").Append((received / 1024f).ToString("0.0"));
        s.Append(" avgFps=").Append(sampleTime > 0 ? Mathf.RoundToInt(sampleFrames / sampleTime) : 0);
        s.Append(" worstMs=").Append(worstMs.ToString("0.0"));
        if (!Net.IsHost) s.Append(" lag=").Append(g.cubes.clientLag.ToString("0.00"));
        ResetSamples();
        return s.ToString();
    }

    static void ResetSamples() { worstMs = 0; peakOut = 0; sampleTime = 0; sampleFrames = 0; }

    // the busiest single client link, bytes per second
    static float MaxSentRate()
    {
        float max = 0;
        foreach (var peer in Net.Peers.Values) max = Mathf.Max(max, peer.sentRate);
        return max;
    }

    public void RunStress()
    {
        if (Net.IsHost && Game.I.phase == Phase.Job && !stressRunning) StartCoroutine(Stress());
    }

    // Tests 2 and 3 of the cube experiment: resting piles of 100 to 800, then an avalanche.
    // The earthquake is held off while it runs.
    System.Collections.IEnumerator Stress()
    {
        var g = Game.I;
        stressRunning = true;
        float savedThreshold = g.tuning.quakeThreshold, savedMax = g.tuning.maxLooseCubes;
        g.tuning.quakeThreshold = 100000;
        g.tuning.maxLooseCubes = Cubes.Capacity;
        g.TuningChanged();
        Vector3 spot = g.ground.Clamp(g.local.transform.position + g.local.Forward * 8f, 6f);

        foreach (int n in new[] { 100, 200, 400, 800 })
        {
            g.cubes.RemoveAll();
            yield return new WaitForSecondsRealtime(1f);
            g.cubes.SpawnPile(n, spot);
            yield return Measure("pile n=" + n);
        }
        g.cubes.Avalanche();
        yield return Measure("avalanche n=" + g.cubes.Loose);

        g.cubes.RemoveAll();
        g.tuning.quakeThreshold = savedThreshold;
        g.tuning.maxLooseCubes = savedMax;
        g.TuningChanged();
        stressRunning = false;
        Debug.Log("RRPSTRESS done");
    }

    // Watch until every cube is asleep (or 60 s), then a few seconds at rest.
    System.Collections.IEnumerator Measure(string label)
    {
        var g = Game.I;
        ResetSamples();
        float start = Time.realtimeSinceStartup, peakMoving = 0;
        yield return new WaitForSecondsRealtime(0.2f);
        while (g.cubes.Moving > 0 && Time.realtimeSinceStartup - start < 60f)
        {
            peakMoving = Mathf.Max(peakMoving, g.cubes.Moving);
            yield return null;
        }
        float settle = Time.realtimeSinceStartup - start;
        float movingFps = sampleFrames / Mathf.Max(sampleTime, 0.001f), movingWorst = worstMs, movingPeak = peakOut;
        int stillMoving = g.cubes.Moving;

        ResetSamples();
        yield return new WaitForSecondsRealtime(3f);
        float restFps = sampleFrames / Mathf.Max(sampleTime, 0.001f);
        Debug.Log("RRPSTRESS " + label + " clients=" + (g.PlayerCount - 1) + " settle=" + settle.ToString("0.0") + "s" + (stillMoving > 0 ? "(timeout, " + stillMoving + " still moving)" : "")
            + " peakMoving=" + peakMoving + " movingAvgFps=" + Mathf.RoundToInt(movingFps) + " movingWorstMs=" + movingWorst.ToString("0.0")
            + " peakOutPerClientKB=" + (movingPeak / 1024f).ToString("0.0") + " restFps=" + Mathf.RoundToInt(restFps) + " restWorstMs=" + worstMs.ToString("0.0"));
    }
}
