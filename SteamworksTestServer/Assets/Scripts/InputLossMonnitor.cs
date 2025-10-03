using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputLossMonnitor : MonoBehaviour
{
    public static InputLossMonnitor Instance;

    [SerializeField] private bool MONITORING_ACTIVE = false;

    public ServerInputManager server;
    public PlayerManager client;

    Dictionary<uint, PlayerInputSnapshot> clientSends = new Dictionary<uint, PlayerInputSnapshot>();
    Dictionary<uint, PlayerInputSnapshot> serverReceives = new Dictionary<uint, PlayerInputSnapshot>();


    private void Start()
    {
        // Singleton setup
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    public void StartMonitoring()
    {
        MONITORING_ACTIVE = true;

        clientSends.Clear();
        serverReceives.Clear();
    }

    public void EndMonitoring()
    {
        MONITORING_ACTIVE = false;

        // perform comparison analysis
        Console.ServerLog("Comparing sent and received frames");

        int received = 0;
        int missed = 0;

        foreach (uint key in clientSends.Keys)
        {
            if (serverReceives.ContainsKey(key))
            {
                received++;
            }
            else
            {
                Console.ServerLog($"-Missed snapshot frame {key}");
                missed++;
            }
        }

        Console.ServerLog($"Lost {missed}, Received {received}, Total: {missed + received}.");
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
}
