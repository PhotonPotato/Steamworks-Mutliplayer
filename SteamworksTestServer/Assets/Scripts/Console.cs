using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public sealed class Console : MonoBehaviour
{
    public static Console Instance = null;

    public TMP_Text ConsoleText;
    public TMP_InputField ConsoleInput;

    public bool trySendingInputToServerAsClient = true;

    public void Awake()
    {
        if (Instance == null) Instance = this;

        if (Instance == this)
        {
            // Clean the console off rip
            ClearConsole();
        }

        // Hook up the input submitted to out listening function
        ConsoleInput.onSubmit.AddListener(OnConsoleInputSubmitted);
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

        Instance.ConsoleText.text += $"\n<color=#459c2d>{Mathf.Round(Time.time * 100) / 100}</color>:\t{message}";
    }

    /// <summary>
    /// Same as log but adds a server tag
    /// </summary>
    /// <param name="message"></param>
    public static void ServerLog(object message)
    {
        if (Instance == null) return;

        Instance.ConsoleText.text += $"\n<color=#459c2d>{Mathf.Round(Time.time * 100) / 100}</color>:\t<b><color=#7851a9>[SERVER]</color></b> {message}";
    }


    /// <summary>
    /// Runs when the ucer submits console input and tries to send said input to the server as the client.
    /// </summary>
    /// <param name="input"></param>
    public void OnConsoleInputSubmitted(string input)
    {
        if (SteamManager.Instance != null)
        {
            // HERE IS WHERE TO PARSE CONSOLE INPUTS INTO COMMANDS OR WHATEVER
            // YOU WOULD LIKE TO DO

            if (trySendingInputToServerAsClient)
            {
                SteamManager.Instance.SendConsoleMessageToSocketServer(input);
            }

            ConsoleInput.text = "";
        }
    }
}
