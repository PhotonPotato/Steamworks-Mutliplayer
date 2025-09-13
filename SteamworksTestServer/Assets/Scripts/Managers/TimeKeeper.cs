using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using TMPro;

public class TimeKeeper : MonoBehaviour
{
    public static TimeKeeper Instance;

    public DateTime clientTime { get; private set; }
    public DateTime serverTime { get; private set; }
    private DateTime gameStartTime;

    [Header("Settings")]
    public float TPS = .02f;
    
    [SerializeField] private float gameStartTimestamp;
    [SerializeField] private float elapsedGameTime;
    [SerializeField] private float gameTick;

    public float timeBetweenHeatbeats = 3;
    private DateTime clientTimeOfLastEst;
    private float timeOflastHeartbeat = float.NegativeInfinity;
    [SerializeField] private double serverToClientLatency;

    [Header("Refs")]
    public TMP_Text timeClockText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    private void Start()
    {
        // if we are the host, and in game, lets request for a game start
        if (SteamManager.Instance.isHost)
        {
            // Req game start by requesting a server timestamp, but with a GameStart
            // message type.
            SteamManager.Instance.RequestGameStartTimestampPackage();
        }
    }

    private void Update()
    {
        // Update the clocks
        clientTime.AddSeconds(Time.deltaTime);
        //serverTime.AddSeconds(Time.deltaTime);
        elapsedGameTime += Time.deltaTime;

        // Try to rehone the clients time every so often
        if (Time.time - timeOflastHeartbeat > timeBetweenHeatbeats)
        {
            clientTime = GetNetworkTime();

            // Rehone elapsed time while we're at it
            elapsedGameTime = (float) (clientTime - gameStartTime).TotalSeconds;

            timeOflastHeartbeat = Time.time;
        }

        // Update the time clock
        TimeSpan time = TimeSpan.FromSeconds(elapsedGameTime);
        timeClockText.text = time.ToString(@"mm\:ss\:ff");
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }


    /// <summary>
    /// Begins a request to the server for a timestamp
    /// </summary>
    public void RequestServerHeartbeat()
    {
        SteamManager.Instance.RequestServerTimestampPackage();
    }

    /// <summary>
    /// Updates the estimated server time and recalculates server-client latency
    /// </summary>
    /// <param name="pckg"></param>
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

    public void OnReceiveGameStartTimestamp(ServerTimestampPackageMessage pckg)
    {
        gameStartTime = DateTime.Parse(pckg.timeData, null, System.Globalization.DateTimeStyles.RoundtripKind);

        Log("Game start time updated  to " + gameStartTime);
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
