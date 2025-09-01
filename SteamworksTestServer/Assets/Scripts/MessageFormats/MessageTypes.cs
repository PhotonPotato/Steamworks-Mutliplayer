using UnityEngine;
using System;

public enum MessageType
{
    ConsoleChat,
    Status,
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
