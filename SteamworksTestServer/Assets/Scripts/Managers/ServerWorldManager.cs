using Steamworks.Data;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class ServerWorldManager : MonoBehaviour
{
    /// <summary>
    /// Datatype for a collection that uses steamId for indexing
    /// </summary>
    public class PlayerManagersBySteamID : KeyedCollection<ulong, PlayerManager>
    {
        protected override ulong GetKeyForItem(PlayerManager item) => item.attachedSteamId;
    }

    public static ServerWorldManager Instance;

    [Header("Refs")]
    public Transform PlayersParent;
    public GameObject PlayerPrefab;

    public Transform[] PlayerSpawnPoints;

    [Header("Trackers")]
    public PhysicsScene curPhysScene;
    private SteamSocketServer socketServer;
    public List<PlayerManager> playerManagers = new List<PlayerManager>();
    public Dictionary<ulong, int> steamIdToIndex = new Dictionary<ulong, int>();

    private PlayerPhysicsStateBundle allPlayerStates = new PlayerPhysicsStateBundle();


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

        allPlayerStates.ids = new ulong[playerManagers.Count];
        allPlayerStates.states = new PlayerPhysicsStateMessage[playerManagers.Count];
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
        // Send clients player state
        SendBackPlayerPhysicsStates();

        // Send dummy states 
        SendBackAllPlayerPhysicsStates();
    }

    /// <summary>
    /// Spawns in the player physics zombies
    /// </summary>
    public void SpawnInPlayerZombies()
    {
        ulong[] connectedSteamIDs = socketServer.GetConnectedIds();

        for (int i = 0; i < socketServer.playerCount; i++)
        {
            playerManagers.Add(Instantiate(PlayerPrefab, PlayerSpawnPoints[i].position, PlayerSpawnPoints[i].rotation).GetComponent<PlayerManager>());
            playerManagers[i].transform.SetParent(PlayersParent);

            playerManagers[i].GetComponent<PlayerInput>().enabled = false;

            PlayerCharacterController charController = playerManagers[i].GetComponent<PlayerCharacterController>();

            charController.enabled = true;
            charController.ThisPhysicsScene = curPhysScene;
            charController.Start();

            playerManagers[i].attachedSteamId = connectedSteamIDs[i];
            playerManagers[i].gameObject.layer = 7;

            Debug.Log("Adding steam id to index: " + connectedSteamIDs[i]);
            steamIdToIndex[connectedSteamIDs[i]] = i;
        }
    }

    public void SendBackPlayerPhysicsStates()
    {
        foreach (Connection connection in socketServer.Connected)
        {
            ulong connectionId = (ulong)connection.UserData;
            Debug.Log(connectionId);
            int index = steamIdToIndex[connectionId];

            // Update our current cache of player states
            allPlayerStates.ids[index] = connectionId;
            allPlayerStates.states[index] = new()
            {
                gameTick = gameTick,

                position = playerManagers[index].transform.position,
                velocity = playerManagers[index].CharacterController.CharacterVelocity,
                look = playerManagers[index].transform.rotation
            };

            socketServer.SendPlayerPhysicsState(connectionId, allPlayerStates.states[index]);
        }
    }

    // TODo: THIS IS REDUNDANT, IT SENDS THE CLIENT PLAYERS STATE THEN SEND ALL AT ONCE
    //       fix for performance
    //       COULD ALSO cache states from the prevosu function and then have this pull
    //       recent tick states to send back the everyone... [did it btw, was easier]
    public void SendBackAllPlayerPhysicsStates()
    {
        foreach (Connection connection in socketServer.Connected)
        {
            socketServer.SendPlayerPhysicsStateBundle(connection, allPlayerStates);
        }
    }
}
