using System;
using System.Buffers;
using System.Text;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

[Serializable]
public class PlayerInfo
{
    public string name;
    public ulong steamId;

    public PlayerInfo(string name, SteamId id)
    {
        this.name = name;
        steamId = id.Value;
    }
}

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance = null;

    public uint appid = 480;

    //[Header("Player Connection Info")]
    [SerializeField] public PlayerInfo myPlayerInfo { get; private set; }
    [SerializeField] public bool isHost { get; set; } = false;
    [SerializeField] public bool activeServer { get; private set; } = false;
    [SerializeField] public bool activeConnection { get; private set; } = false;

    [Tooltip("Server Manager when hosting")]
    public SteamSocketServer socketServer { get; private set; }

    [Tooltip("Client-to-server connection manager when a client (and hosting)")]
    public SteamServerConnectionManager connectionManager { get; private set; }

    private void Awake()
    {
        Console.Log("Awake");

        if (Instance == null) Instance = this;

        // Validate that this is the singleton
        if (Instance == this)
        {
            //DontDestroyOnLoad(this);

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
            myPlayerInfo = new PlayerInfo
            (
                SteamClient.Name,
                SteamClient.SteamId.Value
            );

            Log($"Logged in as {myPlayerInfo.steamId} : {myPlayerInfo.name}");


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


    public void PostPhysUpdate()
    {
        SteamClient.RunCallbacks();

        if (isHost)
        {
            // Server

            if (activeServer) socketServer.Receive();
        }
       
        // Client
        if (activeConnection) connectionManager.Receive();
    }


    private void OnApplicationQuit()
    {
        if (Instance != this) return;

        try
        {
            LeaveOrShutdown();

            if (SteamClient.IsValid) SteamClient.Shutdown();
            else Log("Invalid Client on shutdown.");

            Log("Shutdown!");
        }
        catch
        {
            Log("Failed to shutdown");
        }
    }


    void OnDestroy()
    {
        OnApplicationQuit();
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


    /// <summary>
    /// Creates a steam socket server and connects the host to it.
    /// </summary>
    public void CreateSteamSocketServer()
    {
        socketServer = SteamNetworkingSockets.CreateRelaySocket<SteamSocketServer>();
        activeServer = true;

        Console.ServerLog("Created socket server.");

        // Apparently the host has to connect to its own server to send and recieve messages
        connectionManager = SteamNetworkingSockets.ConnectRelay<SteamServerConnectionManager>(myPlayerInfo.steamId);
        activeConnection = true;
    }


    /// <summary>
    /// Used to connect to someone ELSE'S socket server.
    /// </summary>
    public void JoinSteamSocketServer(Friend serverOwner)
    {
        if (isHost)
        {
            Log("Client is host, cannot join socket server.");
            return;
        }

        Log($"Attemptint to join {serverOwner.Name}'s socket server...");

        connectionManager = SteamNetworkingSockets.ConnectRelay<SteamServerConnectionManager>(serverOwner.Id);
        connectionManager.Connection.ConnectionName = myPlayerInfo.name;
        connectionManager.Connection.UserData = (long) myPlayerInfo.steamId;
        activeConnection = true;
    }


    /// <summary>
    /// Attempt to leave the current server, shutting it down if we are the host.
    /// </summary>
    public async Task LeaveOrShutdownSteamSocketServerAsync()
    {
        LobbyManager.Instance.LeaveCurrentLobby();


        // TODO : not super readable that the function does a check
        // Try to boot everyone (Only runs if we are host)
        KickEveryoneFromSocketServer();

        
        // Close our client connection (host and guests both have this)
        if (connectionManager != null)
        {
            connectionManager.Close();
            connectionManager = null;
        }

        // Close the server if we were hosting
        if (isHost && socketServer != null)
        {
            socketServer.Close();
            socketServer = null;
        }

        // Reset flags deterministically
        activeConnection = false;
        activeServer = false;
        isHost = false;

        // Give Steam callbacks a tick to settle
        await Task.Yield();
        await Task.Yield();
    }
    public void LeaveOrShutdown() => LeaveOrShutdownSteamSocketServerAsync().ContinueWith(t =>
    {
        if (t.IsFaulted) Debug.LogException(t.Exception);
    });


    /// <summary>
    /// Only works if we are the host and the server is active.
    /// </summary>
    public void KickEveryoneFromSocketServer()
    {
        if (isHost && activeServer && socketServer != null)
        {
            foreach (var connection in socketServer.Connected)
            {
                connection.Close();
            }
        }
    }


    /// <summary>
    /// Sends a string message to the socket server to be relayed to everyone on the server.
    /// </summary>
    /// <param name="message"></param>
    public void SendConsoleMessageToSocketServer(string message)
    {
        ConsoleChatMessage chatMsg = new()
        {
            authorInfo = myPlayerInfo,
            chatMessage = message
        };
        
        Message msg = Message.CreateMessage(MessageType.ConsoleChat, JsonUtility.ToJson(chatMsg));
        
        connectionManager.SendMessageToSocketServer(msg, SendType.Reliable);
    }


    /// <summary>
    /// Gets a LobbyInfoPackage containing all of the players and the owner info
    /// </summary>
    public void RequestLobbyInfoPackage() => connectionManager.SendMessageToSocketServer(new LobbyInfoRequestMessage().ToMessage(), SendType.Reliable);

    /// <summary>
    /// Gets a ServerTimestampPackage containing all of the players and the owner info
    /// </summary>
    public void RequestServerTimestampPackage() => connectionManager.SendMessageToSocketServer(new ServerTimestampRequestMessage().ToMessage(), SendType.Reliable);

    /// <summary>
    /// Sends a modified server timestamp request message just with a "GameStart" message type
    /// </summary>
    public void RequestGameStartTimestampPackage() => connectionManager.SendMessageToSocketServer(new ServerTimestampRequestMessage().ToGameStartRequestMessage(), SendType.Reliable);

    public void SendPlayerInputSnapshot(PlayerInputSnapshot snapshot) => connectionManager.SendMessageToSocketServer(snapshot.ToMessage(), SendType.Unreliable);

    public void SendPlayerInputSnapshotBundle(PlayerInputSnapshotBundle snapshotBundle) => connectionManager.SendMessageToSocketServer(snapshotBundle.ToMessage(), SendType.Unreliable);


    #region testing

    public void RequestConnections()
    {
        if (!activeServer) return;

        Console.ServerLog("Players connected to socket server:");

        foreach (var connection in socketServer.Connected)
        {
            SteamId id = new()
            {
                Value = (ulong) connection.UserData
            };
            Console.ServerLog("- " + connection.ConnectionName + " state: " + connection.DetailedStatus());
        }
    }

    public void SendConsoleMsgFromInputField(TMP_InputField field)
    {
        Log("Sending: " + field.text);
        SendConsoleMessageToSocketServer(field.text);
        field.SetTextWithoutNotify("");
    }


    #endregion
}
