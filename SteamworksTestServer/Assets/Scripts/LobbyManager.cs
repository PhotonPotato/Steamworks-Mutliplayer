using System;
using System.Collections;
using System.Collections.Generic;
using Steamworks;
using Steamworks.Data;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    [Header("Lobby Vars")]
    [SerializeField] private Lobby? currentLobby = null;
    [SerializeField] private Lobby hostedLobby;
    [SerializeField] public List<Lobby> activeLobbies;

    public PlayerInfo[] playersInLobby;
    public uint ownerIndex;

    [Header("Refs")]
    public Transform UILobbyListContentParent;
    public GameObject UILobbyListingPrefab;

    public Transform UIPlayerListContentParent;
    public GameObject UIPlayerListingPrefab;

    private void Awake()
    {
        // Update singleton
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        activeLobbies = new List<Lobby>();

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

    public async void CreateLobbyPressedAsync()
    {
        if (await CreateLobby(2))
        {

        }
    }


    /// <summary>
    /// Gathers lobby list from steam matchmaking.
    /// </summary>
    /// <returns>True if successfull in gathering list, false if not.</returns>
    public async Task<bool> RefreshMultiplayerLobbies()
    {
        try
        {
            Lobby[] lobbies = await SteamMatchmaking.LobbyList.WithMaxResults(20).RequestAsync();

            if (lobbies != null)
            {
                // NOTE: Can't use "toList" for some reason

                activeLobbies.Clear();
                foreach (Lobby lobby in lobbies) activeLobbies.Add(lobby);
            }
            else
            {
                Log("Refreshing lobbies. No lobbies");
            }

            return true;
        }
        catch (Exception e)
        {
            Log($"Caught exception refreshing lobbies: {e}.");
            return false;
        }
    }

    /// <summary>
    /// Handles getting an updated list of lobbies and updating lobby listings and their ui.
    /// </summary>
    public async void RefreshLobbiesPressedAsync()
    {
        if (await RefreshMultiplayerLobbies())
        {
            Log($"Refreshing Lobby Listings. Active Lobby Listings: {activeLobbies.Count}");

            // Check for discrepancy between ui and lobby list length
            int uiDiscrepancyAmt = activeLobbies.Count - UILobbyListContentParent.childCount;
            if (uiDiscrepancyAmt > 0)
            {
                // We need to add the diff
                for (int i = 0; i < uiDiscrepancyAmt; i++) Instantiate(UILobbyListingPrefab, UILobbyListContentParent);
            }
            else if (uiDiscrepancyAmt < 0)
            {
                // We need to destroy the diff
                for (int i = 0; i > uiDiscrepancyAmt; i--) DestroyImmediate(UILobbyListContentParent.GetChild(0).gameObject);
            }

            
            // Go through the correct size list and update the elements
            for (int i = 0; i < activeLobbies.Count; i++)
            {
                Transform listing = UILobbyListContentParent.GetChild(i);

                TMP_Text[] textComponents = listing.GetComponentsInChildren<TMP_Text>();
                if (textComponents.Length != 4)
                {
                    Log("Someone fucked with the lobby listings. Incorrect amt of text comps.");
                    continue;
                }

                textComponents[0].text = "Own: " + activeLobbies[i].Owner.Name;
                textComponents[1].text = "Id: " + activeLobbies[i].Id.ToString();
                textComponents[2].text = $"{activeLobbies[i].MemberCount}/{activeLobbies[i].MaxMembers}";

                // This is where you would put the PFP of the player update
                var ownerAvatar = await activeLobbies[i].Owner.GetSmallAvatarAsync();
                
                if (ownerAvatar.HasValue)
                {
                    Texture2D avatar = ownerAvatar?.Covert();
                    
                    listing.GetComponentInChildren<RawImage>().texture = avatar;
                }


                // Set the join button on click to call the JoinPressed function using a handy lambda

                // K This is crazy but i actually gets changed by the time the button is pressed (bc its in a for loop) and
                // ig this int is pass by ref here. (Check out my git commit with "Bug Joining Lobbies")
                // Like if there is 1 lobby, JoinLobbyPressed will get a 1 when i == 0. I think its bc i would go to one
                // in the for loop.
                // Kinda a hackie fix but I need some way to pass the value of i so I'm making a temp variable and it seems
                // to work fine.
                int tempI = i;
                listing.GetComponentInChildren<Button>().onClick.AddListener(() => JoinLobby(activeLobbies[tempI]));
            }
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

    public async void RefreshPlayerList()
    {
        Log($"Refreshing Player Listings. Players in lobby: {playersInLobby.Length}");

        // Check for discrepancy between ui and lobby list length
        int uiDiscrepancyAmt = playersInLobby.Length - UIPlayerListContentParent.childCount;
        if (uiDiscrepancyAmt > 0)
        {
            // We need to add the diff
            for (int i = 0; i < uiDiscrepancyAmt; i++) Instantiate(UIPlayerListingPrefab, UIPlayerListContentParent);
        }
        else if (uiDiscrepancyAmt < 0)
        {
            // We need to destroy the diff
            for (int i = 0; i > uiDiscrepancyAmt; i--) DestroyImmediate(UIPlayerListContentParent.GetChild(0).gameObject);
        }

        
        // Go through the correct size list and update the elements
        for (int i = 0; i < playersInLobby.Length; i++)
        {
            Transform listing = UIPlayerListContentParent.GetChild(i);

            TMP_Text[] textComponents = listing.GetComponentsInChildren<TMP_Text>();

            textComponents[0].text = (i == ownerIndex ? "(Host) " : "") + playersInLobby[i].name;

            // This is where you would put the PFP of the player update
            var playerAvatar = await new Friend(new SteamId { Value = playersInLobby[i].steamId }).GetSmallAvatarAsync();

            if (playerAvatar.HasValue)
            {
                Log($"Gathered {playersInLobby[i].name}'s avatar image.");
                Texture2D avatar = playerAvatar?.Covert();

                listing.GetComponentInChildren<RawImage>().texture = avatar;
            }


            // Set the join button on click to call the JoinPressed function using a handy lambda

            // K This is crazy but i actually gets changed by the time the button is pressed (bc its in a for loop) and
            // ig this int is pass by ref here. (Check out my git commit with "Bug Joining Lobbies")
            // Like if there is 1 lobby, JoinLobbyPressed will get a 1 when i == 0. I think its bc i would go to one
            // in the for loop.
            // Kinda a hackie fix but I need some way to pass the value of i so I'm making a temp variable and it seems
            // to work fine.
            //int tempI = i;
            //listing.GetComponentInChildren<Button>().onClick.AddListener(() => JoinLobby(activeLobbies[tempI]));
        }
    }


    public void RequestLobbyInfoPackage() => SteamManager.Instance.RequestLobbyInfoPackage();

    public void OnReceiveLobbyInfoPackage(LobbyInfoPackageMessage info)
    {
        playersInLobby = info.players;
        
        RefreshPlayerList();
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
