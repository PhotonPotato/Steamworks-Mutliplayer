using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class Console : MonoBehaviour
{
    public static Console Instance = null;

    public TMP_Text ConsoleText;

    public void Awake()
    {
        if (Instance == null) Instance = this;

        if (Instance == this)
        {
            // Clean the console off rip
            ClearConsole();
        }
    }

    public static void ClearConsole()
    {
        Instance.ConsoleText.text = "Console:";
    }


    /// <summary>
    /// Logs an object message to the in-canvas console.
    /// </summary>
    /// <param name="message">Object containing message.</param>
    public static void Log(object message)
    {
        if (Instance == null) return;

        Instance.ConsoleText.text += $"\n{Mathf.Round(Time.time * 100) / 100}:\t {message}";
    }

    /// <summary>
    /// Same as log but adds a server tag
    /// </summary>
    /// <param name="message"></param>
    public static void ServerLog(object message)
    {
        if (Instance == null) return;

        Instance.ConsoleText.text += $"\n[SERVER] {Mathf.Round(Time.time * 100) / 100}:\t {message}";
    }
}
