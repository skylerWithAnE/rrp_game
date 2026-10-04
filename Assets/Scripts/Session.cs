using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

// The only place that knows how players find each other. Two ways in: a direct connection (same
// machine or LAN) and a Unity Relay join code. Swap this class to change either (Steam later).
public static class Session
{
    public const ushort Port = 7777;
    public const int MaxPlayers = 8;

    public static string JoinCode = "";  // set while hosting through Relay
    public static string Address = "";   // set while hosting directly
    public static string Error = "";
    public static bool Busy;

    static NetworkManager Nm => Game.I.nm;
    static UnityTransport Utp => Game.I.utp;

    public static bool HostDirect()
    {
        Clear();
        Utp.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
        Address = "127.0.0.1";
        return Check(Nm.StartHost(), "Could not host on port " + Port + ".");
    }

    public static bool JoinDirect(string address)
    {
        Clear();
        Utp.SetConnectionData(address, Port);
        return Check(Nm.StartClient(), "Could not start a client.");
    }

    public static async Task<bool> HostRelay()
    {
        Clear();
        Busy = true;
        try
        {
            await SignIn();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Utp.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            return Check(Nm.StartHost(), "Could not host through Relay.");
        }
        catch (Exception e)
        {
            Error = "Relay: " + e.Message;
            return false;
        }
        finally { Busy = false; }
    }

    public static async Task<bool> JoinRelay(string code)
    {
        Clear();
        Busy = true;
        try
        {
            await SignIn();
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(code.Trim().ToUpperInvariant());
            Utp.SetRelayServerData(allocation.ToRelayServerData("dtls"));
            return Check(Nm.StartClient(), "Could not join through Relay.");
        }
        catch (Exception e)
        {
            Error = "Relay: " + e.Message;
            return false;
        }
        finally { Busy = false; }
    }

    // A join code is short letters and digits; anything with a dot or colon is an address.
    public static bool LooksLikeAddress(string text)
    {
        return text.Contains(".") || text.Contains(":") || text == "localhost";
    }

    static async Task SignIn()
    {
        if (string.IsNullOrEmpty(UnityEngine.Application.cloudProjectId))
            throw new Exception("this project is not linked to a Unity Cloud project yet (see docs/SETUP.md).");
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            // A profile per process, so several instances on one machine are different players.
            var options = new InitializationOptions();
            options.SetProfile("p" + System.Diagnostics.Process.GetCurrentProcess().Id);
            await UnityServices.InitializeAsync(options);
        }
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    static void Clear()
    {
        JoinCode = "";
        Address = "";
        Error = "";
    }

    static bool Check(bool ok, string error)
    {
        if (!ok) Error = error;
        return ok;
    }
}
