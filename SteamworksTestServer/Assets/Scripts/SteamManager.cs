using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public string name { get; private set; }
        public SteamId steamId { get; private set; }

        public PlayerInfo(string name, SteamId id)
        {
            this.name = name;
            steamId = id;
        }
    }
    public static SteamManager Instance = null;

    public uint appid = 480;

    [SerializeField] private PlayerInfo playerInfo;

    private void Awake()
    {
        Console.Log("Awake");

        if (Instance == null) Instance = this;

        // Validate that this is the singleton
        if (Instance == this)
        {
            DontDestroyOnLoad(this);

            // Try to perform the initial handshake
            try
            {
                SteamClient.Init(appid, true);
                Log("Steam is up and running!");
            }
            catch (System.Exception e)
            {
                Log(e.Message);
            }

            // Gather this player info
            playerInfo = new PlayerInfo
            (
                SteamClient.Name,
                SteamClient.SteamId
            );

            Log($"Logged in as {playerInfo.steamId} : {playerInfo.name}");


        }
        else
        {
            // We only want one SteamManager
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        #region Callbacks
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallBack;
        #endregion
    }


    private void Update()
    {
        SteamClient.RunCallbacks();
    }


    private void OnApplicationQuit()
    {
        try
        {
            SteamClient.Shutdown();
            Log("Shutdown!");
        }
        catch
        {
            Log("Failed to shutdown");
        }
    }


    void OnDestroy()
    {
        SteamClient.Shutdown();
    }


    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }


    /// <summary>
    /// Subscribes to steam lobby creation callback.
    /// </summary>
    /// <param name="result"></param>
    /// <param name="lobby"></param>
    void OnLobbyCreatedCallBack(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Log($"Lobby creation result {result}, not OK");
        }
    }
}
