using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;
        
    [Header("Refs")]
    public GameObject PlayerPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    public GameObject[] SpawnAllPlayers(int count)
    {
        GameObject[] players = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            players[i] = Instantiate(PlayerPrefab);
        }

        Log($"Spawned {count} player(s).");

        return players;
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
