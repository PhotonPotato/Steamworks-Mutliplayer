using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager Instance;
        
    [Header("Refs")]
    public GameObject PlayerPrefab;
    public GameObject PlayerDummyPrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    public GameObject[] SpawnAllPlayers(int count, PhysicsScene physicsScene, out GameObject SpawnedDummy)
    {
        GameObject[] players = new GameObject[count];

        for (int i = 0; i < count; i++)
        {
            players[i] = Instantiate(PlayerPrefab);

            // Turn off all client components, effectively making it a zombie
            if (i != 0)
            {
                players[i].GetComponent<PlayerManager>().enabled = false;
                players[i].GetComponent<PlayerCharacterController>().enabled = false;
                players[i].GetComponent<PlayerCharacterController>().ThisPhysicsScene = physicsScene;
                players[i].GetComponent<CharacterController>().enabled = false;
                players[i].GetComponent<PlayerInput>().enabled = false;
                players[i].GetComponent<PlayerInputHandler>().enabled = false;
            }
        }

        Log($"Spawned {count} player(s).");

        // Spawn the dummy
        SpawnedDummy = Instantiate(PlayerDummyPrefab);

        return players;
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
