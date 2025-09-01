using UnityEngine;
using System;
using Steamworks;

public enum MessageType
{
    ConsoleChat,
    LobbyInfoRequest,
    LobbyInfoPackage,
    InputPackage,
    Position
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
    public Friend[] players;
    public SteamId owner;

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
        return Message.CreateMessage(MessageType.InputPackage, JsonUtility.ToJson(this));
    }
}
