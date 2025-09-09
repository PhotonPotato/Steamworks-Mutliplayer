using System.Collections;
using System.Collections.Generic;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BetweenGamesManager : MonoBehaviour
{
    public static BetweenGamesManager Instance;

    [Header("Trackers")]
    public PlayerInfo[] playersInLobby;
    public uint ownerIndex = 0;

    [Header("Refs")]
    public Transform UIPlayerListContentParent;
    public GameObject UIPlayerListingPrefab;
    public GameObject UIStartGameButton;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(gameObject);
    }

    private void Start()
    {
        // Get the latest player list
        LobbyManager.Instance.RequestLobbyInfoPackage();

        UIStartGameButton.SetActive(SteamManager.Instance.isHost);
    }


    /// <summary>
    /// Called to begin a player list refresh
    /// </summary>
    public void OnRefreshPlayerListPressed()
    {
        LobbyManager.Instance.RequestLobbyInfoPackage();
    }


    /// <summary>
    /// Updates the player listing UI and updates the current player list
    /// </summary>
    /// <param name="newPlayersInfo"></param>
    public async void RefreshPlayerList(PlayerInfo[] newPlayersInfo)
    {
        playersInLobby = newPlayersInfo;

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


            // USE THIS TO SET A BUTTON EVENT FOR THE PLAYER LISTING

            //int tempI = i;
            //listing.GetComponentInChildren<Button>().onClick.AddListener(() => JoinLobby(activeLobbies[tempI]));
        }
    }


    public async void OnExitLobbyPressed()
    {
        // TODO: Some sort of are you sure system
        await SteamManager.Instance.LeaveOrShutdownSteamSocketServerAsync();

        GameFlowManager.Instance.LoadMainMenuScene();
    }


    public void OnStartGamePressed()
    {
        // Check perms
        if (SteamManager.Instance.isHost)
        {
            LobbyManager.Instance.CloseLobbyFromPublic();

            GameFlowManager.Instance.LoadGameScene();
        }
        else
        {
            Log("Cannot start game, not host.");
        }
    }

    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
