using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public enum Phase { Menu, Connecting, Lobby, Job }

// Bootstrap and the top-level flow: menu -> lobby -> job. The scene is empty; everything is made
// here at runtime. There are no networked prefabs: the host and clients talk through Net.
public class Game : MonoBehaviour
{
    public static Game I;

    public NetworkManager nm;
    public UnityTransport utp;
    public Tuning tuning;
    public Ground ground;
    public Cubes cubes;
    public Quake quake;
    public Road road;
    public CameraRig cam;

    public Phase phase = Phase.Menu;
    public string status = "";
    public readonly Player[] players = new Player[Session.MaxPlayers];
    public Player local;
    public int localSlot = -1;
    public float jobTime;

    // what the host last reported, for the readout on clients
    public float hostFps;
    public float fps;

    readonly Dictionary<ulong, int> slotOf = new Dictionary<ulong, int>(); // host
    public readonly ulong[] clientOf = new ulong[Session.MaxPlayers];      // host
    float playerTimer, statsTimer, tuningTimer;
    bool tuningDirty;
    readonly Msg scratch = new Msg(512);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (I == null) new GameObject("Game").AddComponent<Game>();
    }

    void Awake()
    {
        I = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        // headless test instances must not spin a core each
        Application.targetFrameRate = Application.isBatchMode ? 60 : -1;

        var asset = Resources.Load<Tuning>("Tuning");
        tuning = asset != null ? Instantiate(asset) : ScriptableObject.CreateInstance<Tuning>();

        var netGo = new GameObject("Network");
        utp = netGo.AddComponent<UnityTransport>();
        utp.MaxPacketQueueSize = 512; // a hitch on the host must not drop a burst of packets
        nm = netGo.AddComponent<NetworkManager>();
        nm.NetworkConfig = new NetworkConfig();
        nm.NetworkConfig.NetworkTransport = utp;
        nm.NetworkConfig.EnableSceneManagement = false;
        nm.NetworkConfig.ConnectionApproval = true;
        nm.ConnectionApprovalCallback = Approve;
        nm.OnClientConnectedCallback += OnClientConnected;
        nm.OnClientDisconnectCallback += OnClientDisconnected;
        Net.Handler = OnMessage;

        var lightGo = new GameObject("Sun");
        lightGo.transform.SetParent(transform);
        lightGo.transform.rotation = Quaternion.Euler(48f, -35f, 0);
        var sun = lightGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.color = new Color(1f, 0.96f, 0.88f);

        ground = Child<Ground>("Ground");
        cubes = Child<Cubes>("Cubes");
        quake = Child<Quake>("Quake");
        road = Child<Road>("Road");
        cam = Child<CameraRig>("Camera");
        Child<Hud>("Hud");
        Child<AutoTest>("AutoTest");
        cubes.ApplyTuning();
    }

    T Child<T>(string name) where T : Component
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.AddComponent<T>();
    }

    // ---- session flow

    public void Host(bool relay)
    {
        if (phase != Phase.Menu) return;
        if (relay) HostRelay();
        else if (Session.HostDirect()) OnHosting();
    }

    async void HostRelay()
    {
        phase = Phase.Connecting;
        status = "Asking Relay for a join code...";
        if (await Session.HostRelay()) OnHosting();
        else { phase = Phase.Menu; status = ""; }
    }

    void OnHosting()
    {
        Net.Register();
        slotOf.Clear();
        slotOf[NetworkManager.ServerClientId] = 0;
        clientOf[0] = NetworkManager.ServerClientId;
        localSlot = 0;
        EnterLobby();
    }

    public void Join(string text)
    {
        if (phase != Phase.Menu) return;
        text = text.Trim();
        if (text.Length == 0) text = "127.0.0.1";
        if (Session.LooksLikeAddress(text))
        {
            if (Session.JoinDirect(text)) OnJoining();
        }
        else JoinRelay(text);
    }

    async void JoinRelay(string code)
    {
        phase = Phase.Connecting;
        status = "Looking up join code...";
        if (await Session.JoinRelay(code)) OnJoining();
        else { phase = Phase.Menu; status = ""; }
    }

    void OnJoining()
    {
        Net.Register();
        phase = Phase.Connecting;
        status = "Connecting...";
    }

    public void Leave(string why = "")
    {
        if (nm.IsListening) nm.Shutdown();
        for (int s = 0; s < players.Length; s++) RemovePlayer(s);
        cubes.Clear();
        ground.Clear();
        ground.h = null;
        road.Setup(false);
        local = null;
        localSlot = -1;
        phase = Phase.Menu;
        status = why;
    }

    void EnterLobby()
    {
        phase = Phase.Lobby;
        status = "";
        cubes.Clear();
        ground.Generate(false, 0);
        road.Setup(false);
        AddPlayer(localSlot, true);
    }

    // No joining mid-game: once the job starts the host turns everyone away.
    void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;
        if (request.ClientNetworkId == NetworkManager.ServerClientId) { response.Approved = true; return; }
        if (phase != Phase.Lobby) { response.Approved = false; response.Reason = "The job has already started."; return; }
        if (FreeSlot() < 0) { response.Approved = false; response.Reason = "The crew is full."; return; }
        response.Approved = true;
    }

    int FreeSlot()
    {
        for (int s = 0; s < players.Length; s++)
            if (players[s] == null && !slotOf.ContainsValue(s)) return s;
        return -1;
    }

    void OnClientConnected(ulong id)
    {
        if (!nm.IsHost || id == NetworkManager.ServerClientId) return;
        int slot = FreeSlot();
        if (slot < 0 || phase != Phase.Lobby) { nm.DisconnectClient(id); return; }
        slotOf[id] = slot;
        clientOf[slot] = id;
        AddPlayer(slot, false);

        var m = Msg.New(Op.Welcome, 256);
        m.U8((byte)slot);
        tuning.Write(m);
        Net.Send(id, m, true);
        SendRoster();
    }

    void OnClientDisconnected(ulong id)
    {
        if (nm.IsHost)
        {
            if (id == NetworkManager.ServerClientId || !slotOf.TryGetValue(id, out int slot)) return;
            var p = players[slot];
            // whatever they carried falls where they stood
            if (p != null && p.load != 0 && phase == Phase.Job)
                cubes.Spawn((byte)p.load, p.bits > 0, p.ShovelPoint, Quaternion.identity, Vector3.zero);
            slotOf.Remove(id);
            RemovePlayer(slot);
            Net.Forget(id);
            SendRoster();
        }
        else
        {
            string reason = nm.DisconnectReason;
            Leave(string.IsNullOrEmpty(reason) ? "Disconnected from the host." : reason);
        }
    }

    void SendRoster()
    {
        byte mask = 0;
        for (int s = 0; s < players.Length; s++) if (players[s] != null) mask |= (byte)(1 << s);
        var m = Msg.New(Op.Roster, 4);
        m.U8(mask);
        Net.ToClients(m, true);
    }

    public int PlayerCount
    {
        get { int n = 0; foreach (var p in players) if (p != null) n++; return n; }
    }

    void AddPlayer(int slot, bool isLocal)
    {
        if (players[slot] != null) return;
        var go = new GameObject("Player " + (slot + 1));
        var p = go.AddComponent<Player>();
        p.Init(slot, isLocal, SpawnPoint(slot));
        players[slot] = p;
        if (isLocal) local = p;
    }

    void RemovePlayer(int slot)
    {
        if (players[slot] == null) return;
        Destroy(players[slot].gameObject);
        players[slot] = null;
    }

    Vector3 SpawnPoint(int slot)
    {
        // a row facing the hill (or the middle of the lobby)
        float x = ground.SizeX * 0.5f + (slot - 3.5f) * 1.5f;
        float z = phase == Phase.Job ? 8f : ground.SizeZ * 0.5f;
        return new Vector3(x, ground.HeightAt(x, z) + 0.1f, z);
    }

    // host: everyone loads a fresh map together
    public void StartJob()
    {
        if (!Net.IsHost || phase != Phase.Lobby) return;
        int seed = Random.Range(1, int.MaxValue);
        var m = Msg.New(Op.StartJob, 8);
        m.I32(seed);
        Net.ToClients(m, true);
        LoadJob(seed);
    }

    void LoadJob(int seed)
    {
        phase = Phase.Job;
        jobTime = 0;
        cubes.Clear();
        ground.Generate(true, seed);
        road.Setup(true);
        quake.count = 0;
        foreach (var p in players)
        {
            if (p == null) continue;
            p.load = p.bits = 0;
            p.SetLoad(0, 0);
            p.knocked = 0;
            p.Teleport(SpawnPoint(p.slot));
        }
    }

    public void TuningChanged() { tuningDirty = true; cubes.ApplyTuning(); }

    // ---- messages

    void OnMessage(ulong sender, Msg m)
    {
        var op = (Op)m.U8();
        if (nm.IsHost)
        {
            if (!slotOf.TryGetValue(sender, out int slot) || players[slot] == null) return;
            var p = players[slot];
            if (op == Op.PlayerState) ReadPose(p, m);
            else if (op == Op.Verb)
            {
                byte verb = m.U8();
                Vector3 target = m.V3();
                Vector3 aim = m.V3();
                Verbs.Do(p, verb, target, aim);
            }
            return;
        }

        switch (op)
        {
            case Op.Welcome:
                localSlot = m.U8();
                tuning.Read(m);
                cubes.ApplyTuning();
                EnterLobby();
                break;
            case Op.Roster:
                byte mask = m.U8();
                for (int s = 0; s < players.Length; s++)
                {
                    bool present = (mask & 1 << s) != 0;
                    if (present && players[s] == null && s != localSlot && localSlot >= 0) AddPlayer(s, false);
                    if (!present && players[s] != null && s != localSlot) RemovePlayer(s);
                }
                break;
            case Op.Players:
                int count = m.U8();
                for (int k = 0; k < count; k++)
                {
                    int s = m.U8();
                    var p = players[s];
                    if (p == null || p.isLocal) { m.r += 14; continue; }
                    ReadPose(p, m);
                }
                break;
            case Op.StartJob:
                LoadJob(m.I32());
                break;
            case Op.GroundEdit: ground.ApplyEdits(m); break;
            case Op.CubeSpawn: cubes.OnSpawn(m); break;
            case Op.CubeRemove: cubes.OnRemove(m); break;
            case Op.CubeRest: cubes.OnRest(m); break;
            case Op.CubeSnap: cubes.OnSnapshot(m); break;
            case Op.Load:
                {
                    int s = m.U8(), load = m.U8(), bits = m.U8();
                    if (players[s] != null) players[s].SetLoad(load, bits);
                    break;
                }
            case Op.VerbFx:
                {
                    int s = m.U8(), swing = m.U8();
                    if (players[s] != null && !players[s].isLocal) players[s].Swing(swing);
                    break;
                }
            case Op.Tuning:
                tuning.Read(m);
                cubes.ApplyTuning();
                break;
            case Op.Stats:
                hostFps = m.U16();
                cubes.clientMoving = m.U16();
                jobTime = m.F32();
                int links = m.U8();
                road.asphaltLinked = (links & 1) != 0;
                road.paintedLinked = (links & 2) != 0;
                break;
            case Op.Sound:
                {
                    byte kind = m.U8();
                    Sfx.Play(kind, m.V3());
                    break;
                }
            case Op.Quake:
                quake.Begin(m.F32(), m.F32());
                break;
        }
    }

    // pose: position (12), yaw (1), flags (1)
    static void WritePose(Msg m, Player p)
    {
        m.V3(p.transform.position);
        m.U8((byte)Mathf.RoundToInt(Mathf.Repeat(p.yaw, 360f) / 360f * 255f));
        m.U8((byte)(p.grounded ? 1 : 0));
    }

    static void ReadPose(Player p, Msg m)
    {
        p.netPos = m.V3();
        p.netYaw = m.U8() / 255f * 360f;
        p.grounded = (m.U8() & 1) != 0;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(dt, 0.0001f), 0.05f);
        if (!Net.Running) return;
        Net.Tick(dt);
        if (phase != Phase.Lobby && phase != Phase.Job) return;
        if (phase == Phase.Job && nm.IsHost) jobTime += Time.deltaTime;

        playerTimer += dt;
        if (playerTimer >= 0.05f)
        {
            playerTimer = 0;
            if (nm.IsHost)
            {
                scratch.Reset(Op.Players);
                scratch.U8((byte)PlayerCount);
                foreach (var p in players)
                {
                    if (p == null) continue;
                    scratch.U8((byte)p.slot);
                    WritePose(scratch, p);
                }
                Net.ToClients(scratch, false);
            }
            else if (local != null)
            {
                scratch.Reset(Op.PlayerState);
                WritePose(scratch, local);
                Net.ToHost(scratch, false);
            }
        }

        if (!nm.IsHost) return;
        hostFps = fps;
        statsTimer += dt;
        if (statsTimer >= 0.5f)
        {
            statsTimer = 0;
            scratch.Reset(Op.Stats);
            scratch.U16((ushort)Mathf.Clamp(fps, 0, 65535));
            scratch.U16((ushort)cubes.Moving);
            scratch.F32(jobTime);
            scratch.U8((byte)((road.asphaltLinked ? 1 : 0) | (road.paintedLinked ? 2 : 0)));
            Net.ToClients(scratch, false);
        }

        tuningTimer += dt;
        if (tuningDirty && tuningTimer > 0.2f)
        {
            tuningDirty = false;
            tuningTimer = 0;
            var m = Msg.New(Op.Tuning, 256);
            tuning.Write(m);
            Net.ToClients(m, true);
        }
    }
}
