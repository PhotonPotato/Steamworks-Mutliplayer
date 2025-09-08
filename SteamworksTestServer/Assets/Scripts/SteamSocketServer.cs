using UnityEngine;
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
    uint ownerIndex = 0; // Usually gonna be the first person to enter the server

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

        // Update the player list
        connectedPlayers.Add(friend);
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo data)
    {
        base.OnDisconnected(connection, data);
        Console.ServerLog($"{data.Identity} is out of here");

        connectedPlayers.Remove(connectedPlayers.Find(f => (long)f.Id.Value == connection.UserData));
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
                if (result == Result.OK)
                    Console.ServerLog($"Message send(s) success. Sent {byteCount} byte(s)!");
                else
                    Console.ServerLog($"Message send(s) failed. Send result: {result}");
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
}

