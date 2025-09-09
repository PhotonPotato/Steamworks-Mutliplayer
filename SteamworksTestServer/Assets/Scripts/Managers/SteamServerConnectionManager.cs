using System;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using System.Text;
using System.Buffers;
using System.Runtime.InteropServices;

public class SteamServerConnectionManager : ConnectionManager
{
    public override void OnConnecting(ConnectionInfo info)
    {
        base.OnConnecting(info);
        Debug.Log("ConnectionOnConnecting");
    }

    public override void OnConnected(ConnectionInfo info)
    {
        base.OnConnected(info);
        Debug.Log("ConnectionOnConnected");

        // Start a refresh of the UI player listings
        SteamManager.Instance.RequestLobbyInfoPackage();
    }
    
    public override void OnDisconnected(ConnectionInfo info)
    {
        base.OnDisconnected(info);
        Debug.Log("ConnectionOnDisconnected");

        LobbyManager.Instance.LeaveCurrentLobby();
    }

    public override void OnMessage(IntPtr data, int size, long messageNum, long recvTime, int channel)
    {
        // Message received from socket server, delegate to method for processing
        //SteamManager.Instance.ProcessMessageFromSocketServer(data, size);
        Console.Log($"Connection Got A Message #{messageNum}");

        // Process the message right here

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
                    ConsoleChatMessage consoleChatMessage = JsonUtility.FromJson<ConsoleChatMessage>(msg.body);

                    Console.Log($"<color=#eba134>{consoleChatMessage.authorInfo.name}</color>: {consoleChatMessage.chatMessage}");
                    break;

                case MessageType.LobbyInfoPackage:
                    LobbyInfoPackageMessage lobbyInfoPackageMessage = JsonUtility.FromJson<LobbyInfoPackageMessage>(msg.body);
                    
                    LobbyManager.Instance.OnReceiveLobbyInfoPackage(lobbyInfoPackageMessage);
                    break;

                case MessageType.ServerTimestampPackage:
                    // Fwd it to the time keeper if there is one
                    TimeKeeper.Instance?.OnReceiveServerTimestamp(JsonUtility.FromJson<ServerTimestampPackageMessage>(msg.body));
                    break;
            }
        }
        catch (Exception e)
        {
            Console.Log($"Exception when parsing message: {e}");
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
    public bool SendMessageToSocketServer(Message message, SendType sendType = SendType.Reliable, bool tryOnce = false, bool logSend = true)
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
            Result result = Connection.SendMessage(msgPtr, byteCount, sendType);
            if (result != Result.OK && !tryOnce)
                result = Connection.SendMessage(msgPtr, byteCount, sendType);
            
            if (logSend)
            {
                if (result == Result.OK)
                    Log($"Message send(s) success. Sent {byteCount} byte(s)!");
                else
                    Log($"Message send(s) failed. Send result: {result}");
            }

            return result == Result.OK;
        }
        finally
        {
            if (handle.IsAllocated) handle.Free();
            ArrayPool<byte>.Shared.Return(jsonMessageBuffer);
        }
    }

    public void Log(object o)
    {
        Console.Log(o);
    }
}