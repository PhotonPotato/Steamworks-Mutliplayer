using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public PhysicsScene currentGameScene;

    public PlayerManager thisPlayerManager;

    public GameObject[] playerObjects { get; private set; }
    public GameObject dummyPlayer;

    public ulong steamId { get; private set; }
    public Dictionary<ulong, int> steamIdToIndex { get; private set; } = new Dictionary<ulong, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    private void Start()
    {
        // Get the list of connected steamIds and swap this steam id to the 0th index
        steamId = SteamManager.Instance.myPlayerInfo.steamId;
        for (int i = 0, j = 1; i < LobbyManager.Instance.playersInLobby.Length; i++)
        {
            if (LobbyManager.Instance.playersInLobby[i].steamId == steamId)
            {
                steamIdToIndex.Add(steamId, 0);
            }
            else
            {
                steamIdToIndex.Add(LobbyManager.Instance.playersInLobby[i].steamId, j);
                j++;
            }
        }

        Log("Spawning players and dummy.");
        playerObjects = PlayerSpawnManager.Instance.SpawnAllPlayers(LobbyManager.Instance.playersInLobby.Length, currentGameScene, steamIdToIndex.Keys.ToArray(), out dummyPlayer);

        // Try to get player manager
        thisPlayerManager = playerObjects[0].GetComponent<PlayerManager>();
        if (thisPlayerManager == null) Log("Missing player manager");

        thisPlayerManager.CharacterController.cam = dummyPlayer.GetComponentInChildren<Camera>();
        thisPlayerManager.CharacterController.SetFOVs(thisPlayerManager.CharacterController.cam.fieldOfView);

        // Dummy setup
        dummyPlayer.GetComponent<DummyPlayerBehavior>().InitDummy(thisPlayerManager.CharacterController.DummyCameraTransform, thisPlayerManager.transform);


        // Update the current client scene
        currentGameScene = SceneManager.GetSceneByName(GameFlowManager.Instance.GameScene).GetPhysicsScene();
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }


    public void RunPlayerFixedUpdate()
    {
        thisPlayerManager?.RunClientFixedUpdate();

        InputLossMonitor.Instance?.AddRunningTickClient(TimeKeeper.Instance.gameTick);

        ClientCorrectionManager.Instance?.RunFixedUpdate();
    }

    public void RunClientPhysics() => currentGameScene.Simulate(TimeKeeper.Instance.TickSpeed);
}
