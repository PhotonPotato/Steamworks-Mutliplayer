using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class TimeKeeper : MonoBehaviour
{
    public static TimeKeeper Instance;

    public DateTime clientTime { get; private set; }
    public DateTime serverTime { get; private set; }
    private DateTime estServerTime;

    [Header("Settings")]
    public float TPS = .02f;
    
    [SerializeField] private float gameStartTimestamp;
    [SerializeField] private float elapsedGameTime;
    [SerializeField] private float gameTick;

    public float timeBetweenHeatbeats = 3;
    private DateTime clientTimeOfLastEst;
    private float timeOflastHeartbeat = float.NegativeInfinity;
    [SerializeField] private double serverToClientLatency;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    private void Update()
    {
        // Update the clocks
        clientTime.AddSeconds(Time.deltaTime);
        serverTime.AddSeconds(Time.deltaTime);

        // Get a heatbeat and rehone the server est every so often
        if (Time.time - timeOflastHeartbeat > timeBetweenHeatbeats)
        {
            RequestServerHeartbeat();
        }
    }
    public void SpecUpdate()
    {
        //UpdateServerTime();

        //elapsedGameTime = serverTime - gameStartTimestamp;
        gameTick = elapsedGameTime * TPS;
    }

    /// <summary>
    /// Begins a request to the server for a timestamp
    /// </summary>
    public void RequestServerHeartbeat()
    {
        SteamManager.Instance.RequestServerTimestampPackage();
    }

    public void OnReceiveServerTimestamp(ServerTimestampPackageMessage pckg)
    {
        clientTime = GetNetworkTime();
        //clientTimeOfLastEst = clientTime;
        timeOflastHeartbeat = Time.time;

        DateTime serverSendTime = DateTime.Parse(pckg.timeData, null, System.Globalization.DateTimeStyles.RoundtripKind);

        Console.Log(serverSendTime);
        serverToClientLatency = (clientTime - serverSendTime).TotalSeconds;

        serverTime = clientTime.AddSeconds(serverToClientLatency);
    }

    /// <summary>
    /// Get the UTC time using a NTP asset from the asset store
    /// </summary>
    public DateTime GetNetworkTime()
    {
        using (NtpClient client = new NtpClient("time.windows.com"))
        {
            return client.GetNetworkTime();
        }
    }
}
