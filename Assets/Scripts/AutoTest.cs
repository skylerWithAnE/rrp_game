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
//   -rrpClicks <n>      ask for n clicks on random spots of the station 2 and 3 plots, twenty a second
//   -rrpStakes          first put three linked stakes down on the station 3 hillside; on a map,
//                       stake a road out from the first town to the second
//   -rrpMap <n>         host: choose map n (0 is the stations)
//   -rrpDrive <n>       on the Driving test ground: get into vehicle n and drive it in a circle
//   -rrpWork <percent>  on a map, after -rrpStakes: click wherever there is work, at the cap, until
//                       the road is graded and gravelled, with that share of clicks on the hot
//                       spot; prints RRPWORK lines with the time each took
//
// RunStress (also a button on the tuning panel) runs the cube experiment's pile and avalanche
// tests on the host and prints one RRPSTRESS line per test.
//
// Multiplayer Play Mode clones do not get their own command line, so in the editor the same flags
// are also read from the file rrp_autotest.txt in the system temp folder, if it exists.
public class AutoTest : MonoBehaviour
{
    public static bool Bot, Log;
    int clicksLeft, clicksTotal, stakesLeft;
    int mapWanted = -1;
    int driveWanted = -1;
    int workHot = -1, workPhase, workClicks;
    float workStart;
    float stakeTimer;
    float clickTimer;
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
            else if (args[i] == "-rrpClicks" && i + 1 < args.Length) { int.TryParse(args[++i], out clicksLeft); clicksTotal = clicksLeft; }
            else if (args[i] == "-rrpStakes") stakesLeft = 3;
            else if (args[i] == "-rrpMap" && i + 1 < args.Length) int.TryParse(args[++i], out mapWanted);
            else if (args[i] == "-rrpDrive" && i + 1 < args.Length) int.TryParse(args[++i], out driveWanted);
            else if (args[i] == "-rrpWork" && i + 1 < args.Length) int.TryParse(args[++i], out workHot);
        }
        if (host) Game.I.Host(false);
    }

    // Things for the game to do on its next frame. A script driving the editor from outside
    // must hand its work over this way: a message sent from outside the game's own frame is
    // never delivered and costs the client its connection.
    public static readonly System.Collections.Generic.Queue<Action> Next = new System.Collections.Generic.Queue<Action>();

    void Update()
    {
        var g = Game.I;
        while (Next.Count > 0) Next.Dequeue()();
        if (join != null && g.phase == Phase.Menu)
        {
            retry -= Time.unscaledDeltaTime;
            if (retry <= 0) { retry = 2f; g.Join(join); }
        }
        if (host && startAt > 0 && g.phase == Phase.Lobby && g.PlayerCount >= startAt) g.StartJob();

        if (g.local != null) g.local.bot = Bot ? (Func<Controls>)BotControls : null;
        if (mapWanted >= 0 && Net.IsHost && g.phase == Phase.Lobby)
        {
            if (g.map != mapWanted) g.SetMap(mapWanted);
            mapWanted = -1;
        }
        clickTimer += Time.unscaledDeltaTime;
        stakeTimer += Time.unscaledDeltaTime;
        if (driveWanted >= 0 && g.phase == Phase.Lobby && g.map == Plot.DrivingMap && driveWanted < g.cars.cars.Length && g.local != null && !g.respawn)
        {
            Cars.TestThrottle = 1f;
            Cars.TestSteer = 0.35f;
            if (Cars.Mine < 0 && stakeTimer > 2f) { stakeTimer = 0; g.cars.Ask(driveWanted, 1); }
        }
        if (Plot.LandMap(g.map))
        {
            if (g.phase == Phase.Lobby && g.plots[Plot.Land].Ready && !g.respawn) OnLand(g.plots[Plot.Land]);
        }
        else if (g.phase != Phase.Lobby) { }
        else if (stakesLeft > 0 && PlotsReady(g) && stakeTimer > 1f)
        {
            // each links to the one before, which the host's answer has made this player's chosen stake
            stakeTimer = 0;
            stakesLeft--;
            var hill = g.plots[1];
            Vector3 spot = hill.Spot(0.3f + 0.15f * stakesLeft, 0.7f - 0.25f * stakesLeft);
            hill.RequestStake(spot.x, spot.z, hill.selected, -1);
        }
        else if (stakesLeft == 0 && clicksLeft > 0 && PlotsReady(g) && stakeTimer > 1f && clickTimer > 0.05f)
        {
            clickTimer = 0;
            clicksLeft--;
            // grade stations 2 and 3; lay and pack gravel on station 4
            var plot = g.plots[clicksLeft % 3];
            int tool = plot.id != 2 ? Plot.Grade : Plot.Gravel;
            Vector3 spot = plot.Spot(UnityEngine.Random.value, UnityEngine.Random.value);
            plot.RequestClick(spot.x, spot.z, clicksLeft % 5 == 0, tool);
        }

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

    static bool PlotsReady(Game g) { return g.plots[0].Ready && g.plots[1].Ready && g.plots[2].Ready; }

    // On a map: stake a road out from the first town to the second, a stake a second, swinging a
    // little from side to side; then click along it, grading two clicks in three and gravelling
    // the third.
    void OnLand(Plot land)
    {
        if (stakesLeft > 0 && stakeTimer > 1f)
        {
            stakeTimer = 0;
            if (land.joined || land.stakes.Count > 60) { stakesLeft = 0; return; }
            int from = land.selected < 0 ? 0 : land.selected;
            Vector3 here = land.stakes[from], to = land.stakes[1] - here;
            to.y = 0;
            if (to.magnitude <= 19f) { land.RequestStake(0, 0, from, 1); return; }
            Vector3 step = to.normalized * 17f + new Vector3(land.stakes.Count % 2 == 0 ? 3f : -3f, 0, 0);
            land.RequestStake(here.x + step.x, here.z + step.z, from, -1);
        }
        else if (stakesLeft == 0 && workHot >= 0 && workPhase < 2 && stakeTimer > 1f && clickTimer > 1.02f / Game.I.tuning.clicksPerSecond)
        {
            // phase 0 grades, phase 1 gravels. If trucks pack, the gravel only has to be laid.
            if (workClicks == 0 && workPhase == 0) workStart = Time.unscaledTime;
            bool layOnly = Game.I.tuning.truckPacking > 0;
            if (!land.NextWork(workPhase == 0 ? Plot.Grade : Plot.Gravel, layOnly, out Vector3 work))
            {
                Debug.Log("RRPWORK " + (workPhase == 0 ? "graded" : layOnly ? "gravel laid" : "gravelled and packed") + " " + Mathf.RoundToInt(land.roadLength) + " m in " + (Time.unscaledTime - workStart).ToString("0") + " s, " + workClicks + " clicks, hot " + workHot + " %, row " + Game.I.tuning.hotSpotRow + ", truck packing " + Game.I.tuning.truckPacking);
                workPhase++;
                workClicks = 0;
                workStart = Time.unscaledTime;
                return;
            }
            clickTimer = 0;
            workClicks++;
            land.RequestClick(work.x, work.z, workClicks * workHot % 100 < workHot, workPhase == 0 ? Plot.Grade : Plot.Gravel);
        }
        else if (stakesLeft == 0 && clicksLeft > 0 && stakeTimer > 1f && clickTimer > 0.05f)
        {
            clickTimer = 0;
            clicksLeft--;
            Vector3 spot = land.RoadSpot(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            land.RequestClick(spot.x, spot.z, clicksLeft % 5 == 0, clicksLeft % 3 == 0 ? Plot.Gravel : Plot.Grade);
        }
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
            c.primaryDown = c.primary;
            c.secondaryDown = c.secondary;
        }
        return c;
    }

    public static string State()
    {
        var g = Game.I;
        var s = new System.Text.StringBuilder("RRPSTATE ");
        s.Append(Net.IsHost ? "host" : "client").Append(" slot=").Append(g.localSlot).Append(" phase=").Append(g.phase);
        s.Append(" map=").Append(g.map).Append(" joined=").Append(g.plots[Plot.Land].joined).Append(" road=").Append(g.plots[Plot.Land].roadLength.ToString("0.0"));
        s.Append(" players=");
        foreach (var p in g.players)
            if (p != null) s.Append(p.slot).Append(':').Append(p.transform.position.ToString("0.0")).Append("load").Append(p.load).Append(' ');
        if (g.ground.Ready) s.Append(" ground=").Append(g.ground.Hash().ToString("x8"));
        var t = g.tuning;
        s.Append(" eye=").Append(t.eyeHeight).Append(" fov=").Append(t.fieldOfView).Append(" walk=").Append(t.walkSpeed).Append(" sprint=").Append(t.sprintMultiplier);
        s.Append(" truck=").Append(t.truckWidth).Append('x').Append(t.truckLength).Append('x').Append(t.truckHeight).Append(" yardParts=").Append(g.yard.GetComponentsInChildren<MeshRenderer>().Length);
        foreach (var plot in g.plots)
            s.Append(" plot").Append(plot.id).Append('=').Append(plot.Hash().ToString("x8")).Append(" level=").Append((plot.roadShare * 100f).ToString("0.0")).Append('/').Append((plot.shoulderShare * 100f).ToString("0.0"))
                .Append('/').Append((plot.gravelShare * 100f).ToString("0.0")).Append('/').Append((plot.packedShare * 100f).ToString("0.0")).Append(" clicks=").Append(plot.clicks).Append(" stakes=").Append(plot.stakes.Count).Append('+').Append(plot.links.Count);
        s.Append(" lorries=").Append(g.lorries.State());
        s.Append(" cars=").Append(g.cars.State());
        s.Append(" rigs=").Append(g.lorries.RigState()).Append(" stock=").Append(g.plots[Plot.Quarry].stock).Append(" carrying=").Append(g.plots[Plot.Quarry].carrying)
            .Append(" heap=").Append(g.plots[Plot.Quarry].heapPlaced ? g.plots[Plot.Quarry].heapAt.ToString("0.0") : "none");
        s.Append(" blocks=").Append(g.blocks.Count);
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
