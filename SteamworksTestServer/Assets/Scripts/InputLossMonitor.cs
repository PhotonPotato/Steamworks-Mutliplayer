using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InputLossMonitor : MonoBehaviour
{
    public static InputLossMonitor Instance;

    [SerializeField] private bool MONITORING_ACTIVE = false;
    [SerializeField] private bool TICK_MONITORING_ACTIVE = false;

    public ServerInputManager server;
    public PlayerManager client;

    Dictionary<uint, PlayerInputSnapshot> clientSends = new Dictionary<uint, PlayerInputSnapshot>();
    Dictionary<uint, PlayerInputSnapshot> serverReceives = new Dictionary<uint, PlayerInputSnapshot>();
    List<uint> clientRunTicks = new List<uint>();
    List<uint> serverRunTicks = new();

    private void Awake()
    {
        // Singleton setup
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);

        Console.ServerLog("Input Monitor on standby");
        Debug.Log("Input monitor active " + this.gameObject.name);
    }

    public void StartInputMonitoring()
    {
        MONITORING_ACTIVE = true;

        if (!SteamManager.Instance.activeServer)
        {
            Console.ServerLog("Not running server, cannot monitor inputs.");
            return;
        }

        Console.ServerLog("Monitoring Active");

        clientSends.Clear();
        serverReceives.Clear();
    }

    public void EndInputMonitoring()
    {
        MONITORING_ACTIVE = false;

        // perform comparison analysis
        Console.ServerLog("Comparing sent and received frames");

        int received = 0;
        int lost = 0;

        foreach (uint key in clientSends.Keys)
        {
            if (serverReceives.ContainsKey(key))
            {
                if (!serverReceives[key].Equals(clientSends[key]))
                    Console.ServerLog($"--Correct tick {key}, unmatching content");
                received++;
            }
            else
            {
                Console.ServerLog($"-Missed snapshot frame {key}");
                lost++;
            }
        }

        Console.ServerLog($"Lost {lost}, Received {received}, Total: {lost + received}.");
    }

    public void StartTickMonitoring()
    {
        TICK_MONITORING_ACTIVE = true;

        if (!SteamManager.Instance.activeServer)
        {
            Console.ServerLog("Not running server, cannot monitor inputs.");
            return;
        }

        Console.ServerLog("Tick Monitoring Active");

        clientRunTicks.Clear();
        serverRunTicks.Clear();
    }

    public void EndTickMonitoring()
    {
        TICK_MONITORING_ACTIVE = false;

        Console.ServerLog("Comparing run frames");

        int received = 0;
        int lost = 0;

        foreach (var tick in clientRunTicks)
        {
            if (serverRunTicks.Contains(tick))
            {
                received++;
            }
            else
            {
                Console.ServerLog($"-Missed tick {tick}" + (serverReceives.ContainsKey(tick) ? ", Found in server rec." : ", Not in server rec."));
                lost++;
            }
        }

        // The server is always 6 ticks behind so you will "lose" 6 extra ticks
        lost = Mathf.Max(lost - 6, 0);

        Console.ServerLog($"Lost {lost}, Run {received}, Total: {lost + received}.");
        Console.ServerLog($"Lost %: {(double)lost / (lost + received) * 100}.");

    }

    public void AddClientInputSnapshot(PlayerInputSnapshot snapshot)
    {
        if (MONITORING_ACTIVE)
            clientSends[snapshot.gameTick] = snapshot;
    }

    public void AddServerInputSnapshot(PlayerInputSnapshot snapshot)
    {
        if (MONITORING_ACTIVE)
            serverReceives[snapshot.gameTick] = snapshot;
    }

    public void AddRunningTickClient(uint tick)
    {
        if (TICK_MONITORING_ACTIVE)
            clientRunTicks.Add(tick);
    }

    public void AddRunningTickServer(uint tick)
    {
        if (TICK_MONITORING_ACTIVE)
            serverRunTicks.Add(tick);
    }
}
