using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class Console : MonoBehaviour
{
    public static Console Instance = null;

    [Header("Refs")]
    public Button OpenConsoleButton = null;
    public TMP_Text ConsoleText;
    public TMP_InputField ConsoleInput;

    [Header("Settings")]
    public bool trySendingInputToServerAsClient = true;
    public bool hideOnStart = false;

    public bool open { get; private set; } = false;

    public void Awake()
    {
        Instance = this;

        // Hook up the input submitted to out listening function
        ConsoleInput.onSubmit.AddListener(OnConsoleInputSubmitted);
    }

    private void Start()
    {
        if (hideOnStart) OnCloseConsole();
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

        if (Instance.ConsoleText.text.Length > 2048)
        {
            Instance.ConsoleText.text = Instance.ConsoleText.text.Substring(Instance.ConsoleText.text.Length - 2048);
        }
    }

    /// <summary>
    /// Same as log but adds a server tag
    /// </summary>
    /// <param name="message"></param>
    public static void ServerLog(object message)
    {
        if (Instance == null) return;

        Instance.ConsoleText.text += $"\n<color=#459c2d>{Mathf.Round(Time.time * 100) / 100}</color>:\t<b><color=#7851a9>[SERVER]</color></b> {message}";

        if (Instance.ConsoleText.text.Length > 10000)
        {
            Instance.ConsoleText.text = Instance.ConsoleText.text.Substring(Instance.ConsoleText.text.Length - 2048);
        }
    }


    /// <summary>
    /// Runs when the userr submits console input and tries to send said input to the server as the client.
    /// </summary>
    /// <param name="input"></param>
    public void OnConsoleInputSubmitted(string input)
    {
        if (SteamManager.Instance != null)
        {
            // HERE IS WHERE TO PARSE CONSOLE INPUTS INTO COMMANDS OR WHATEVER
            // YOU WOULD LIKE TO DO
            switch (input)
            {
                case "beginILM":
                    Log("beginning Input Loss Monitoring");
                    InputLossMonitor.Instance?.StartInputMonitoring();
                    break;

                case "endILM":
                    Log("ending Input Loss Monitoring");
                    InputLossMonitor.Instance?.EndInputMonitoring();
                    break;

                case "beginSTM":
                    InputLossMonitor.Instance?.StartTickMonitoring();
                    break;

                case "endSTM":
                    InputLossMonitor.Instance?.EndTickMonitoring();
                    break;

                default:
                    // Default to sending it as a console chat
                    if (trySendingInputToServerAsClient)
                    {
                        SteamManager.Instance.SendConsoleMessageToSocketServer(input);
                    }
                    break;
            }

            ConsoleInput.text = "";
        }
    }

    /// <summary>
    /// Hides the console and tries to show the open button.
    /// </summary>
    public void OnCloseConsole()
    {
        gameObject.SetActive(false);
        OpenConsoleButton?.gameObject.SetActive(true);

        open = false;
    }

    /// <summary>
    /// Shows the console and tries to hide the open button.
    /// </summary>
    public void OnOpenConsole()
    {
        gameObject.SetActive(true);
        OpenConsoleButton?.gameObject.SetActive(false);

        open = true;
    }
}
