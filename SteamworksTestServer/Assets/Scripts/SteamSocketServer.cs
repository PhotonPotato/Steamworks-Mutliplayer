using UnityEngine;
using System.Collections;
using Steamworks;
using Steamworks.Data;
using System;

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
        Console.ServerLog($"{data.Identity} has joined the game");
    }

    public override void OnDisconnected(Connection connection, ConnectionInfo data)
    {
        base.OnDisconnected(connection, data);
        Console.ServerLog($"{data.Identity} is out of here");
    }

    public override void OnMessage(Connection connection, NetIdentity identity, IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        base.OnMessage(connection, identity, data, size, messageNum, recvTime, channel);
        Console.ServerLog($"We got a message from {identity}!");

        // Send it right back
        connection.SendMessage(data, size, SendType.Reliable);
    }
}

