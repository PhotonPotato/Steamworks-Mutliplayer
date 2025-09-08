using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("Lobby Vars")]
    [SerializeField] private Lobby? currentLobby = null;
    [SerializeField] private Lobby hostedLobby;

    public PlayerInfo[] playersInLobby;
    public uint ownerIndex;

    private void Awake()
    {
        // Update singleton
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        #region Callbacks
        SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
        #endregion
    }

    /// <summary>
    /// Attempts to create+join a lobby (as host) and update current lobby.
    /// </summary>
    /// <param name="maxMembers">Maximum amount of player that can join this lobby</param>
    /// <param name="publicLobby">Whether this lobby is public or private</param>
    /// <returns>True if successful, false if not</returns>
    public async Task<bool> CreateLobby(int maxMembers = 100, bool publicLobby = true)
    {
        try
        {
            // Attempt to create a lobby
            var createLobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxMembers);

            if (createLobbyResult.HasValue)
            {
                hostedLobby = createLobbyResult.Value;
                if (publicLobby) hostedLobby.SetPublic();
                hostedLobby.SetJoinable(true);

                Log($"Lobby created successfully, hosting lobby {hostedLobby.Owner.Name} : {hostedLobby.Id}");
            }
            else
            {
                // Invalid lobby
                throw new System.Exception("Steam failed to create lobby");
            }

            // Update the current lobby
            currentLobby = hostedLobby;

            // My dumdass, i didnt set this and the whole steammanager was confused asf
            SteamManager.Instance.isHost = true;
            return true;
        }
        catch (System.Exception e)
        {
            Log($"Exception creating lobby: {e}.");
            return false;
        }
    }



    /// <summary>
    /// Gets lobby list from steam matchmaking.
    /// </summary>
    /// <returns>Updated active lobby list if successfull in gathering list, null if not.</returns>
    public async Task<List<Lobby>> GetRefreshedMultiplayerLobbyList()
    {
        try
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(20).RequestAsync();
            List<Lobby> activeLobbies = new List<Lobby>();

            if (lobbies != null)
            {
                // NOTE: Can't use "toList" for some reason

                foreach (Lobby lobby in lobbies) activeLobbies.Add(lobby);
            }
            else
            {
                Log("Refreshing lobbies. No lobbies");
            }

            return activeLobbies;
        }
        catch (Exception e)
        {
            Log($"Caught exception refreshing lobbies: {e}.");
            return null;
        }
    }


    /// <summary>
    /// Attempts to join a lobby.
    /// </summary>
    /// <param name="lobby"></param>
    public async void JoinLobby(Lobby lobby)
    {
        RoomEnter joinResult = await lobby.Join();

        if (joinResult == RoomEnter.Success)
        {
            Log($"Join lobby SUCCESS id: {lobby.Id}.");

            currentLobby = lobby;
        }
        else
        {
            Log($"Join lobby FAILED id: {lobby.Id}. Result: {joinResult}.");
        }
    }


    /// <summary>
    /// In charge of spooling up a server if host and showing proper UI
    /// </summary>
    public void OnLobbyEntered(Lobby lobby)
    {
        Log("Lobby entered");

        // Check ownership
        if (lobby.Owner.Id == SteamClient.SteamId)
        {
            // Owner

            if (SteamManager.Instance.activeServer) Log("You are already in and hosting this server.");
            else
            {
                Log("Calling server boot...");
                // Spin up the socket server
                SteamManager.Instance.CreateSteamSocketServer();
            }
        }
        else
        {
            // Guest


            // Join the server
            if (SteamManager.Instance.activeConnection) Log("You have already joined this server.");
            else SteamManager.Instance.JoinSteamSocketServer(lobby.Owner);
        }

        //TODO: THIS SHOULD JUST SEND YOU TO THE INBETWEEN SCENE

        // Now we show the right lobby UI
        //MainMenuManager.Instance.OnCloseAllPanels(); // Change this shit
        //MainMenuManager.Instance.UIInLobbyPanel.SetActive(true);

        GameFlowManager.Instance.LoadBetweenGamesScene();

        return;
    }

    public void OnLobbyMemberJoined(Lobby lobby, Friend friend)
    {
        Log($"{friend.Name} joined lobby.");

        // Update the lobby info
        RequestLobbyInfoPackage();
    }

    public void OnLobbyMemberLeave(Lobby lobby, Friend friend)
    {
        Log($"{friend.Name} left lobby.");

        RequestLobbyInfoPackage();
    }

    public void OnLobbyMemberKicked(Lobby lobby, Friend friend)
    {
        Log($"{friend.Name} was kicked from lobby.");

        RequestLobbyInfoPackage();
    }


    public void RequestLobbyInfoPackage() => SteamManager.Instance.RequestLobbyInfoPackage();

    public void OnReceiveLobbyInfoPackage(LobbyInfoPackageMessage info)
    {
        playersInLobby = info.players;


        // TODO: Choose what to do based on the 
        BetweenGamesManager.Instance?.RefreshPlayerList(newPlayersInfo: info.players);
    }


    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
