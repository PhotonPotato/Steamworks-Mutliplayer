using System;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

public class SteamServerConnectionManager : ConnectionManager
{
    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        Debug.Log("ConnectionOnConnected");
    }

    public override void OnConnecting(ConnectionInfo info)
    {
        base.OnConnecting(info);
        Debug.Log("ConnectionOnConnecting");
    }

    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Debug.Log("ConnectionOnDisconnected");
    }

    public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        // Message received from socket server, delegate to method for processing
        SteamManager.Instance.ProcessMessageFromSocketServer(data, size);
        Console.Log($"Connection Got A Message #{messageNum}");
    }
}