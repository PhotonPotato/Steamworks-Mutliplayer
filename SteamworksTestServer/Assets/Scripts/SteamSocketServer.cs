using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Buffers;

public class SteamSocketServer : SocketManager
{
    List<Friend> connectedPlayers = new List<Friend>();

    // Another way to access connections for specific players thats O(1) without doing a search
    Dictionary<ulong, Connection> connectionList = new Dictionary<ulong, Connection>();

    // Links a steam id to an index in teh connected players list
    Dictionary<ulong, int> steamIDToIndex = new Dictionary<ulong, int>();

    uint ownerIndex = 0; // Usually gonna be the first person to enter the server

    DateTime lastServerTime;
    DateTime gameStartTime = DateTime.MinValue;

    public int playerCount => connectedPlayers.Count;

    public override void OnConnecting(Connection connection, ConnectionInfo data)
    {
        base.OnConnecting(connection, data);
        connection.Accept();
        Console.ServerLog($"{data.Identity} is connecting");
    }

    public override void OnConnected(Connection connection, ConnectionInfo data)
    {
        base.OnConnected(connection, data);

        // Try to convert the new connection to a name
        Friend friend = new Friend(data.Identity);
        // Get it if it isn't already cached
        if (!friend.IsOnline)
        {
            SteamFriends.RequestUserInformation(data.Identity, true);
        }

        connection.ConnectionName = friend.Name;

        Console.ServerLog($"{friend.Name} has joined the game");

        connection.UserData = (long) friend.Id.Value;

        // Update the player list, connection list, and steamID -> index mapping
        connectedPlayers.Add(friend);
        connectionList[friend.Id.Value] = connection;
        steamIDToIndex[friend.Id.Value] = connectedPlayers.Count - 1;
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo data)
    {
        base.OnDisconnected(connection, data);
        Console.ServerLog($"{data.Identity} is out of here");

        ulong steamID = (ulong) connection.UserData; //ulong.Parse(connection.ConnectionName);
        Debug.Log("sds" + steamID);
        connectedPlayers.RemoveAt(steamIDToIndex[steamID]);//connectedPlayers.Find(f => (long)f.Id.Value == connection.UserData));
        connectionList.Remove(steamID);
        steamIDToIndex.Remove(steamID);
    }

    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);

        // Check what kind of msg it is
        byte[] rented = ArrayPool<byte>.Shared.Rent(size);

        try
        {
            if (size == 0) return;

            // Convert it from ptr striaght to string
            // NOTE: Only goes up to the first null char
            Marshal.Copy(data, rented, 0, size);
            string msgString = Encoding.UTF8.GetString(rented, 0, size);

            Message msg = JsonUtility.FromJson<Message>(msgString);

            // Here's the meat and potatoes
            switch (msg.type)
            {
                case MessageType.ConsoleChat:
                    RelaySocketMessageToConnections(rented, size, messageNum, connection.Id);
                    break;

                case MessageType.LobbyInfoRequest:
                    Console.ServerLog($"Recieved a lobby info request from {connection.ConnectionName}");

                    SendMessageToClient(connection, BuildLobbyInfopackage().ToMessage(), SendType.Reliable);
                    break;

                case MessageType.ServerTimestampRequest:
                    Console.ServerLog($"Received a serverTimestampRequest from {connection.ConnectionName}");

                    SendMessageToClient(connection, BuildServerTimestampPackage().ToMessage(), SendType.Reliable);
                    break;

                //TODO: reqork the following 2 message types and how they are used, now there are 2 "gameStart" messages and types
                case MessageType.GameStartMessage:
                    // Pretty much only gonna get this from host, just
                    // echo this message back to everyone in the server.
                    Console.ServerLog($"Received a game start message from {connection.ConnectionName}. Echoing to everyone...");

                    AnnounceGameStart();
                    break;

                case MessageType.GameStartTimestampRequest:
                    Console.ServerLog($"Received a gameStartTimestampRequest from {connection.ConnectionName}");

                    // Send a timestamp with a "GameStart" type
                    if (gameStartTime == DateTime.MinValue)
                    {
                        // Then we are host trying to start game
                        SendMessageToClient(connection, BuildServerTimestampPackage().ToGameStartTimestampMessage(), SendType.Reliable);
                        gameStartTime = lastServerTime;
                    }
                    else
                    {
                        // Then we arent host and need to just get the time
                        SendMessageToClient(connection, BuildGameStartTimestampMessage().ToGameStartTimestampMessage(), SendType.Reliable);
                    }
                    break;

                case MessageType.InputSnapshot:
                    // Send to input buffer in the physics sim world
                    PlayerInputSnapshot snapshot = JsonUtility.FromJson<PlayerInputSnapshot>(msg.body);

                    Console.ServerLog($"Received Input Package from {connection.ConnectionName}. Input tick: {snapshot.gameTick}");

                    ServerInputManager.Instance.AddInputFrame(0, snapshot);
                    break;

                case MessageType.InputSnapshotBundle:
                    // Send to input buffer in the physics sim world
                    PlayerInputSnapshotBundle bundle = JsonUtility.FromJson<PlayerInputSnapshotBundle>(msg.body);

                    //Console.ServerLog($"Received Input Package from {connection.ConnectionName}.");

                    ServerInputManager.Instance.ProcessInputSnapshotBundle(0, bundle);
                    break;

                default:
                    Console.ServerLog($"We got a message from {connection.ConnectionName}!");
                    break;
            }
        }
        catch (Exception e)
        {
            Console.ServerLog($"Exception when parsing message: {e}");
        }
    }

    public void RelaySocketMessageToConnections(byte[] data, int size, long messageNum, uint authorConnectionId, bool echoBackToSender = true)
    {
        try
        {
            // Loop through connections to relay the message
            foreach (Connection connection in Connected)
            {
                // Skip echoing back
                if (!echoBackToSender && authorConnectionId == connection.Id) continue;
                
                connection.SendMessage(data,0, size, SendType.Reliable);
            }
        }
        catch (Exception e)
        {
            Console.ServerLog($"Exception while relaying message: {e}");
        }
    }

    /// <summary>
    /// Attempts to send a message of bytes to a client.
    /// </summary>
    /// <returns></returns>
    public bool SendMessageToClient(Connection connection, Message message, SendType sendType = SendType.Reliable, bool tryOnce = false, bool logSend = true)
    {
        string jsonMessage = JsonUtility.ToJson(message);

        // Get the worst case byte count
        int maxBytes = Encoding.UTF8.GetMaxByteCount(jsonMessage.Length);
        // Using the arrayppool is supposed to help performance and replace places where
        // arrays are being created and destroyed frequently (like making the buf every send)
        byte[] jsonMessageBuffer = ArrayPool<byte>.Shared.Rent(maxBytes);
        int byteCount = Encoding.UTF8.GetBytes(jsonMessage, 0, jsonMessage.Length, jsonMessageBuffer, 0);

        // Garb Collector stuff incoming
        // Keep in mind that the garb collector moves shit around so we have to pin it.
        GCHandle handle = default;

        try
        {
            // Pin handle (mem) and send without copying
            handle = GCHandle.Alloc(jsonMessageBuffer, GCHandleType.Pinned);
            IntPtr msgPtr = byteCount == 0 ? IntPtr.Zero : handle.AddrOfPinnedObject();

            // Try send
            Result result = connection.SendMessage(msgPtr, byteCount, sendType);
            if (result != Result.OK && !tryOnce)
                result = connection.SendMessage(msgPtr, byteCount, sendType);

            if (logSend)
            {
                /*
                if (result == Result.OK)
                    //Console.ServerLog($"Message send(s) success. Sent {byteCount} byte(s)!");
                else
                    //Console.ServerLog($"Message send(s) failed. Send result: {result}");
                */
            }

            return result == Result.OK;
        }
        finally
        {
            if (handle.IsAllocated) handle.Free();
            ArrayPool<byte>.Shared.Return(jsonMessageBuffer);
        }
    }

    /// <summary>
    /// Converts the List<Friend> into an array of PlayerInfo to send to client.
    /// </summary>
    /// <returns></returns>
    public LobbyInfoPackageMessage BuildLobbyInfopackage()
    {
        // K just learned that JSONUtility is useless and can only send basic
        // structs without properits({get; set;}) only public fields. Soooo..
        // no SteamId, no Friend bc those are Facepunches and are too complex
        PlayerInfo[] players = new PlayerInfo[connectedPlayers.Count];

        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            players[i] = new PlayerInfo(connectedPlayers[i].Name, connectedPlayers[i].Id.Value);
        }

        return new LobbyInfoPackageMessage()
        {
            players = players,
            ownerIndex = this.ownerIndex
        };
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

    public ServerTimestampPackageMessage BuildServerTimestampPackage()
    {
        // TODO: keep track of the server time using time since instead
        //       constant GetNetworkTime() calls
        lastServerTime = GetNetworkTime();
        Console.ServerLog(lastServerTime);
        return new ServerTimestampPackageMessage
        {
            // Apparently "o" is the flag for a round trip format, not sure y thats important
            timeData = lastServerTime.ToString("o")
        };
    }

    public ServerTimestampPackageMessage BuildGameStartTimestampMessage()
    {
        return new ServerTimestampPackageMessage
        {
            // Apparently "o" is the flag for a round trip format, not sure y thats important
            timeData = gameStartTime.ToString("o")
        };
    }

    // HIGH CODING HEADS UP
    public void SendPlayerPhysicsState(ulong playerId, PlayerPhysicsStateMessage state)
    {
        state.ConvertToClientWorldSpace();
        SendMessageToClient(connectionList[playerId], state.ToMessage(), SendType.Unreliable);
    }

    // Lightly backed, please put comments and summaries when sober
    public void SendPlayerPhysicsStateBundle(ulong playerID, PlayerPhysicsStateBundle bundle)
    {
        SendMessageToClient(connectionList[playerID], bundle.ToMessage(), SendType.Unreliable);
    }

    public void SendPlayerPhysicsStateBundle(Connection connection, PlayerPhysicsStateBundle bundle)
    {
        SendMessageToClient(connection, bundle.ToMessage(), SendType.Unreliable);
    }

    /// <summary>
    /// Returns an array of connected user steamIds
    /// </summary>
    public ulong[] GetConnectedIds() => steamIDToIndex.Keys.ToArray();

    public void AnnounceGameStart()
    {
        foreach (Connection connection in Connected)
        {
            if (steamIDToIndex[(ulong) connection.UserData] == ownerIndex) continue;

            SendMessageToClient(connection, new GameStartMessage().ToMessage(), SendType.Reliable);
        }
    }
}

