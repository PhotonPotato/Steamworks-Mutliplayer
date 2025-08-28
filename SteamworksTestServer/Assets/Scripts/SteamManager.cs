using System.Collections;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
    public uint appid = 480;

    private void Awake()
    {
        DontDestroyOnLoad(this);

        try
        {
            // Try to perform the initial handshake
            Steamworks.SteamClient.Init(appid, true);
            Log("Steam is up and running!");
        }
        catch (System.Exception e)
        {
            Log(e.Message);
        }
    }

    private void OnApplicationQuit()
    {
        try
        {
            Steamworks.SteamClient.Shutdown();
            Log("Shutdown!");
        }
        catch
        {
            Log("Failed to shutdown");
        }
    }

    void OnDestroy()
    {
        SteamClient.Shutdown();
    }

    // Debug.Log Wrapper (I feel like Ima be using htis a lot)
    public static void Log(string msg) { Debug.Log(msg); }
}
