using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum Op : byte
{
    // client -> host
    PlayerState, Verb,
    // host -> clients
    Welcome, Roster, Players, StartJob, GroundEdit, CubeSpawn, CubeRemove, CubeRest, CubeSnap,
    Load, VerbFx, Tuning, Stats, Quake, Sound, Truck, Boom, Win, ToLobby, Block,
    // stations 2 and 3
    Click, Stake, StakeEdit,    // client -> host
    PlotState, PlotRows, PlotEdit, PlotStakes,
    Lorry,      // station 5
    Map, PlotCoarse, PlotPoints,    // the maps: which one, its land a height every metre, and the points changed since
    Zone, Shovel,                   // client -> host: give a section a role; a shovel of gravel on or off the truck
    Finish,                         // client -> host, the dev tool: do a whole section's next stage
    Car, CarPose,                   // client -> host: get in or out of a vehicle or tip its bucket; where the one I drive is
    Cars,                           // host -> clients: every driven vehicle
}

// A byte buffer with the few field types the game sends.
public class Msg
{
    public byte[] d;
    public int n; // bytes written
    public int r; // read position

    public Msg(int capacity = 256) { d = new byte[capacity]; }

    public static Msg New(Op op, int capacity = 256)
    {
        var m = new Msg(capacity);
        m.U8((byte)op);
        return m;
    }

    public Msg Reset(Op op) { n = 0; r = 0; U8((byte)op); return this; }

    void Need(int k) { if (n + k > d.Length) Array.Resize(ref d, Math.Max(d.Length * 2, n + k)); }

    public void U8(byte v) { Need(1); d[n++] = v; }
    public void U16(ushort v) { Need(2); d[n++] = (byte)v; d[n++] = (byte)(v >> 8); }
    public void U32(uint v) { Need(4); d[n++] = (byte)v; d[n++] = (byte)(v >> 8); d[n++] = (byte)(v >> 16); d[n++] = (byte)(v >> 24); }
    public void I32(int v) { U32((uint)v); }
    public void F32(float v) { U32((uint)BitConverter.SingleToInt32Bits(v)); }
    public void V3(Vector3 v) { F32(v.x); F32(v.y); F32(v.z); }
    public void Rot(Quaternion q) { U32(PackRot(q)); }
    // Position in 2 mm steps over the range every map fits in.
    public void Pos16(Vector3 v) { U16(Q16(v.x)); U16(Q16(v.y)); U16(Q16(v.z)); }

    public byte U8() { return d[r++]; }
    public ushort U16() { ushort v = (ushort)(d[r] | d[r + 1] << 8); r += 2; return v; }
    public uint U32() { uint v = (uint)(d[r] | d[r + 1] << 8 | d[r + 2] << 16 | d[r + 3] << 24); r += 4; return v; }
    public int I32() { return (int)U32(); }
    public float F32() { return BitConverter.Int32BitsToSingle((int)U32()); }
    public Vector3 V3() { float x = F32(), y = F32(), z = F32(); return new Vector3(x, y, z); }
    public Quaternion Rot() { return UnpackRot(U32()); }
    public Vector3 Pos16() { float x = D16(U16()), y = D16(U16()), z = D16(U16()); return new Vector3(x, y, z); }

    const float PosMin = -16f, PosRange = 128f;
    static ushort Q16(float v) { return (ushort)Mathf.Clamp(Mathf.RoundToInt((v - PosMin) / PosRange * 65535f), 0, 65535); }
    static float D16(ushort v) { return v / 65535f * PosRange + PosMin; }

    // Smallest-three: drop the largest component, 10 bits for each of the others.
    static uint PackRot(Quaternion q)
    {
        int big = 0;
        float max = Mathf.Abs(q[0]);
        for (int i = 1; i < 4; i++) { float a = Mathf.Abs(q[i]); if (a > max) { max = a; big = i; } }
        float sign = q[big] < 0 ? -1f : 1f;
        uint packed = (uint)big;
        for (int i = 0; i < 4; i++)
        {
            if (i == big) continue;
            float v = q[i] * sign; // in [-0.7071, 0.7071]
            uint bits = (uint)Mathf.Clamp(Mathf.RoundToInt((v * 0.7071068f + 0.5f) * 1023f), 0, 1023);
            packed = packed << 10 | bits;
        }
        return packed;
    }

    static Quaternion UnpackRot(uint packed)
    {
        int big = (int)(packed >> 30);
        var q = new Quaternion();
        float sum = 0;
        for (int i = 3; i >= 0; i--)
        {
            if (i == big) continue;
            float v = ((packed & 1023) / 1023f - 0.5f) / 0.7071068f;
            packed >>= 10;
            q[i] = v;
            sum += v * v;
        }
        q[big] = Mathf.Sqrt(Mathf.Max(0, 1 - sum));
        return q;
    }
}

// The whole game talks through one named message. Everything sent is counted, so the readout can
// show data per second per player.
public static class Net
{
    const string Name = "rrp";
    const int Overhead = 12; // rough per-message header cost, added to the counted bytes
    public const int MaxUnreliable = 1100;

    public class Peer
    {
        public int sent, received;
        public float sentRate, receivedRate; // bytes per second
    }

    public static readonly Dictionary<ulong, Peer> Peers = new Dictionary<ulong, Peer>();
    public static Action<ulong, Msg> Handler;

    static readonly Msg rx = new Msg(8192);
    static float rateTimer;

    static NetworkManager Nm => Game.I.nm;
    public static bool Running => Game.I != null && Nm != null && Nm.IsListening;
    public static bool IsHost => Running && Nm.IsHost;

    public static void Register()
    {
        Peers.Clear();
        Nm.CustomMessagingManager.RegisterNamedMessageHandler(Name, OnReceive);
    }

    static Peer PeerOf(ulong id)
    {
        if (!Peers.TryGetValue(id, out var p)) Peers[id] = p = new Peer();
        return p;
    }

    static void OnReceive(ulong sender, FastBufferReader reader)
    {
        int len = reader.Length - reader.Position;
        if (len <= 0) return;
        if (rx.d.Length < len) rx.d = new byte[len];
        reader.ReadBytesSafe(ref rx.d, len);
        rx.n = len;
        rx.r = 0;
        PeerOf(sender).received += len + Overhead;
        Handler?.Invoke(sender, rx);
    }

    public static void Send(ulong client, Msg m, bool reliable)
    {
        if (!Running) return;
        using (var w = new FastBufferWriter(m.n, Allocator.Temp))
        {
            w.WriteBytesSafe(m.d, m.n);
            Nm.CustomMessagingManager.SendNamedMessage(Name, client, w,
                reliable ? NetworkDelivery.ReliableFragmentedSequenced : NetworkDelivery.Unreliable);
        }
        PeerOf(client).sent += m.n + Overhead;
    }

    public static void ToClients(Msg m, bool reliable)
    {
        if (!IsHost) return;
        var ids = Nm.ConnectedClientsIds;
        for (int i = 0; i < ids.Count; i++)
            if (ids[i] != NetworkManager.ServerClientId) Send(ids[i], m, reliable);
    }

    public static void ToHost(Msg m, bool reliable) { Send(NetworkManager.ServerClientId, m, reliable); }

    public static void Forget(ulong client) { Peers.Remove(client); }

    public static void Tick(float dt)
    {
        rateTimer += dt;
        if (rateTimer < 1f) return;
        foreach (var p in Peers.Values)
        {
            p.sentRate = p.sent / rateTimer;
            p.receivedRate = p.received / rateTimer;
            p.sent = p.received = 0;
        }
        rateTimer = 0;
    }
}
