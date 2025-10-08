using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ServerWorldManager : MonoBehaviour
{
    public static ServerWorldManager Instance;

    [Header("Refs")]
    public Transform PlayersParent;
    public GameObject PlayerPrefab;

    [Header("Trackers")]
    public PhysicsScene curPhysScene;
    private SteamSocketServer socketServer;
    public PlayerManager[] playerManagers;

    // Running the server 30 ticks behind
    public uint gameTick { get; private set; } = (TimeKeeper.Instance?.gameTick ?? ServerTickDelay) - ServerTickDelay;

    [Header("Settings")]
    public const int ServerTickDelay = 6;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }

    private void Start()
    {
        curPhysScene = SceneManager.GetSceneByName(GameFlowManager.Instance.ServerScene).GetPhysicsScene();

        socketServer = SteamManager.Instance.socketServer;

        Debug.Log("Player count " + socketServer.playerCount);

        ServerInputManager.Instance.CreatePlayerBuffers(socketServer.playerCount);

        SpawnInPlayerZombies();
    }

    public void RunServerPrePhysicsTick()
    {
        gameTick = (TimeKeeper.Instance?.gameTick ?? ServerTickDelay) - ServerTickDelay;

        InputLossMonitor.Instance.AddRunningTickServer(gameTick);
        //Console.ServerLog("Running next input frame. Sim tick num " + gameTick);
        ServerInputManager.Instance.RunNextInputFrame();

        if (gameTick % 60 == 0)
            ServerInputManager.Instance.CleanInputBuffers();
    }

    /// <summary>
    /// Runs a simulation step on the server world physics
    /// </summary>
    public void RunServerWorldPhysicsTick()
    {
        if (TimeKeeper.Instance != null) curPhysScene.Simulate(TimeKeeper.Instance.TickSpeed);
    }

    public void RunServerPostPhysicsTick()
    {
        SendBackPlayerPhysicsStates();
    }

    /// <summary>
    /// Spawns in the player physics zombies
    /// </summary>
    public void SpawnInPlayerZombies()
    {
        playerManagers = new PlayerManager[socketServer.playerCount];

        for (int i = 0; i < socketServer.playerCount; i++)
        {
            playerManagers[i] = Instantiate(PlayerPrefab, PlayersParent).GetComponent<PlayerManager>();

            // Immediately turn off the players' cameras
            playerManagers[i].GetComponentInChildren<Camera>().enabled = false;
            playerManagers[i].GetComponentInChildren<AudioListener>().enabled = false;

            playerManagers[i].GetComponent<PlayerInput>().enabled = false;
            playerManagers[i].GetComponent<PlayerCharacterController>().enabled = true;
            playerManagers[i].GetComponent<PlayerCharacterController>().ThisPhysicsScene = curPhysScene;

            playerManagers[i].gameObject.layer = 7;
        }
    }

    public void SendBackPlayerPhysicsStates()
    {
        for (int i = 0; i < playerManagers.Length; i++)
        {
            socketServer.SendPlayerPhysicsState(0, new()
            {
                gameTick = gameTick,

                position = playerManagers[i].transform.position,
                velocity = playerManagers[i].CharacterController.CharacterVelocity,
                look = playerManagers[i].transform.rotation
            });
        }
    }
}
