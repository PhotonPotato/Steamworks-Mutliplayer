using System;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    [Serializable]
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

    [Header("Player Connection Info")]
    [SerializeField] private PlayerInfo myPlayerInfo;
    [SerializeField] public bool isHost { get; private set; } = false;
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
            myPlayerInfo = new PlayerInfo
            (
                SteamClient.Name,
                SteamClient.SteamId
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


    private void Update()
    {
        SteamClient.RunCallbacks();

        if (isHost)
        {
            // Server

            if (activeServer)
            {
                socketServer.Receive();
            }
        }
        else
        {
            // Client
            if (activeConnection)
            {
                connectionManager.Receive();
            }
        }
    }


    private void OnApplicationQuit()
    {
        try
        {
            LeaveOrShutdownSteamSocketServer();

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


    public void ProcessMessageFromSocketServer(IntPtr msgPtr, int size)
    {
        try
        {
            // Get the message and copy it from heap into stack
            byte[] message = new byte[size];

            System.Runtime.InteropServices.Marshal.Copy(msgPtr, message, 0, size);

            string msgString = System.Text.Encoding.UTF8.GetString(message);


            // For now just print the message to log
            Console.ServerLog(msgString + $" ({size} bytes)");
        }
        catch (Exception e)
        {
            Log($"Failed to process message from server: {e}");
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
        if (isHost) Log("Client is host, cannot join socket server.");

        Log($"Attemptint to join {serverOwner.Name}'s socket server...");

        connectionManager = SteamNetworkingSockets.ConnectRelay<SteamServerConnectionManager>(serverOwner.Id);
        connectionManager.Connection.ConnectionName = myPlayerInfo.name;
        activeConnection = true;
    }


    /// <summary>
    /// Attempt to leave the current server, shutting it down if we are the host.
    /// </summary>
    public void LeaveOrShutdownSteamSocketServer()
    {
        if (!activeConnection) return;

        // Try to boot everyone (Only runs if we are host)
        KickEveryoneFromSocketServer();

        // Close the connection and server
        connectionManager.Close();
        if(isHost) socketServer.Close();
    }


    /// <summary>
    /// Only works if we are the host and the server is active.
    /// </summary>
    public void KickEveryoneFromSocketServer()
    {
        if (isHost && activeServer)
        {
            foreach (var connection in socketServer.Connected)
            {
                connection.Close();
            }
        }
    }


    /// <summary>
    /// Attempts to send a message of bytes to the connected socket server.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="sendType"></param>
    /// <param name="tryOnce"></param>
    /// <param name="logSend"></param>
    /// <returns></returns>
    public bool SendMessageToSocketServer(byte[] msg, SendType sendType = SendType.Reliable, bool tryOnce = false, bool logSend = true)
    {
        try
        {
            // Copy message data to heap and use IntPtr to send it
            int size = msg.Length;
            IntPtr msgPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

            System.Runtime.InteropServices.Marshal.Copy(msg, 0, msgPtr, size);

            // Try send
            Result result = connectionManager.Connection.SendMessage(msgPtr, size, sendType);

            if (result == Result.OK)
            {
                // Success
                Log($"Message send success ({size} bytes).");

                System.Runtime.InteropServices.Marshal.FreeHGlobal(msgPtr);

                return true;
            }
            else if (!tryOnce)
            {
                // Try once more
                result = connectionManager.Connection.SendMessage(msgPtr, size, sendType);

                System.Runtime.InteropServices.Marshal.FreeHGlobal(msgPtr);

                return result == Result.OK;
            }

            // Send failed
            Log($"Message send(s) failed. Send result: {result}");

            return false;
        }
        catch (Exception e)
        {
            Log($"Failed to send message to server. Error: {e}");

            return false;
        }
    }

    public void RequestConnections()
    {
        if (!activeServer) return;

        Console.ServerLog("Players connected to socket server:");

        foreach (var connection in socketServer.Connected)
        {
            Console.ServerLog("- " + connection.Id);
        }
    }
}
