using UnityEngine;
using System.Collections;
using Steamworks;
using Steamworks.Data;
using System;
using System.Runtime.InteropServices;

public class SteamSocketServer : SocketManager
{
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
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo data)
    {
        base.OnDisconnected(connection, data);
        Console.ServerLog($"{data.Identity} is out of here");
    }

    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
        Console.ServerLog($"We got a message from {connection.ConnectionName}!");

        // Check what kind of msg it is
        byte[] rented = System.Buffers.ArrayPool<byte>.Shared.Rent(size);

        try
        {
            if (size == 0) return;

            // Convert it from ptr striaght to string
            // NOTE: Only goes up to the first null char
            Marshal.Copy(data, rented, 0, size);
            string msgString = System.Text.Encoding.UTF8.GetString(rented, 0, size);

            Message msg = JsonUtility.FromJson<Message>(msgString);

            // Here's the meat and potatoes
            switch (msg.type)
            {
                case MessageType.ConsoleChat:
                    RelaySocketMessageToConnections(data, size, messageNum, connection.Id);
                    break;
            }
        }
        catch (Exception e)
        {
            Console.ServerLog($"Exception when parsing message: {e}");
        }
    }

    public void RelaySocketMessageToConnections(IntPtr data, int size, long messageNum, uint authorConnectionId, bool relayBackToSender = true)
    {
        try
        {
            // Loop through connections to relay the message
            foreach (Connection connection in Connected)
            {
                // Skip echoing back
                if (!relayBackToSender && authorConnectionId == connection.Id) continue;

                connection.SendMessage(data, size, SendType.Reliable);
            }
        }
        catch (Exception e)
        {
            Console.ServerLog($"Exception while relaying message: {e}");
        }
    }
}

