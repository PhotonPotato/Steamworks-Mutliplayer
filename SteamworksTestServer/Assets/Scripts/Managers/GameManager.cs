using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PhysicsScene currentGameScene;

    public PlayerManager thisPlayerManager;

    public GameObject[] playerObjects { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    private void Start()
    {
        Log("Spawning players.");
        playerObjects = PlayerSpawnManager.Instance.SpawnAllPlayers(LobbyManager.Instance.playersInLobby.Length, currentGameScene);

        // Try to get player manager
        thisPlayerManager = playerObjects[0].GetComponent<PlayerManager>();
        if (thisPlayerManager == null) Log("Missing player manager");


        // Update the current client scene
        currentGameScene = SceneManager.GetSceneByName(GameFlowManager.Instance.GameScene).GetPhysicsScene();
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }


    public void RunPlayerFixedUpdate() => thisPlayerManager?.RunClientFixedUpdate();

    public void RunClientPhysics() => currentGameScene.Simulate(TimeKeeper.Instance.TickSpeed);
}
