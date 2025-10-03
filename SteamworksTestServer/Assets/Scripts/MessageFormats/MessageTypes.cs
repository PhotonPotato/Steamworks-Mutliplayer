using UnityEngine;
using System;
using Steamworks;
using System.Runtime.Serialization;

public enum MessageType
{
    ConsoleChat,

    LobbyInfoRequest,
    LobbyInfoPackage,

    ServerTimestampRequest,
    ServerTimestampPackage,
    GameStartTimestampRequest,
    GameStartTimestampPackage,

    InputSnapshot,
    InputSnapshotBundle,
    PlayerPhysicsState,
    entityWorldData
}

[Serializable]
public struct Message
{
    public uint id;
    public MessageType type;
    public string body;

    /// <summary>
    /// Used to generate a new message.
    /// </summary>
    /// <returns>The new message with wha tyou specified.</returns>
    public static Message CreateMessage(MessageType messageType, string body)
    {
        Message msg = new()
        {
            // id will just be what the 
            id = (uint)Time.frameCount % 10000,
            type = messageType,
            body = body
        };

        return msg;
    }
}

[Serializable]
public struct LobbyInfoRequestMessage
{
    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.LobbyInfoRequest, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct LobbyInfoPackageMessage
{
    public PlayerInfo[] players;
    public uint ownerIndex;

    /// <summary>
    /// Turns this LobbyInfoPackage into a Message format using json util.
    /// </summary>
    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.LobbyInfoPackage, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct ConsoleChatMessage
{
    public PlayerInfo authorInfo;
    public string chatMessage;

    /// <summary>
    /// Turns this ConsoleChatMessage into a Message format using json util.
    /// </summary>
    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.ConsoleChat, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct InputPackageMessage
{

    /// <summary>
    /// Turns this InputPackage into a Message format using json util.
    /// </summary>
    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.InputSnapshot, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct ServerTimestampRequestMessage
{
    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.ServerTimestampRequest, JsonUtility.ToJson(this));
    }

    public Message ToGameStartRequestMessage()
    {
        return Message.CreateMessage(MessageType.GameStartTimestampRequest, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct ServerTimestampPackageMessage
{
    public string timeData;

    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.ServerTimestampPackage, JsonUtility.ToJson(this));
    }

    public Message ToGameStartTimestampMessage()
    {
        return Message.CreateMessage(MessageType.GameStartTimestampPackage, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct PlayerInputSnapshot
{
    public uint gameTick;
    public Vector2 moveInput;
    public Vector2 lookInput;
    public bool sprintInput;
    public bool jumpInput;
    public bool crouchInput;

    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.InputSnapshot, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct PlayerInputSnapshotBundle
{
    public PlayerInputSnapshot[] snapshots;

    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.InputSnapshotBundle, JsonUtility.ToJson(this));
    }
}

[Serializable]
public struct PlayerPhysicsStateMessage
{
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion look;

    public Message ToMessage()
    {
        return Message.CreateMessage(MessageType.PlayerPhysicsState, JsonUtility.ToJson(this));
    }
}
