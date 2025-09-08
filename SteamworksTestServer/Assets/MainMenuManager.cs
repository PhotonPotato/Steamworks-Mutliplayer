using System.Collections;
using System.Collections.Generic;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance;

    private List<Lobby> activeLobbies;

    [Header("Main Menu Refs")]
    public GameObject UIInitialMenuParent;
    public GameObject UIHostGameMenuParent;
    public GameObject UIJoinGamePanel;

    [Header("Lobby Panel Refs")]
    public Transform UILobbyListContentParent;
    public GameObject UILobbyListingPrefab;

    public Transform UIPlayerListContentParent;
    public GameObject UIPlayerListingPrefab;


    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        activeLobbies = new List<Lobby>();

        // Hide anything that was left open and pop open the initial menu
        OnCloseAllPanels();
        OnInitialMenuOpened();
    }


    /// <summary>
    /// Handles getting an updated list of lobbies and updating lobby listings and their ui.
    /// </summary>
    public async void RefreshLobbiesPressedAsync()
    {
        if (await LobbyManager.Instance.GetRefreshedMultiplayerLobbyList() != null)
        {
            Console.Log($"Refreshing Lobby Listings. Active Lobby Listings: {activeLobbies.Count}");

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
                    Console.Log("Someone fucked with the lobby listings. Incorrect amt of text comps.");
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
                listing.GetComponentInChildren<Button>().onClick.AddListener(() => LobbyManager.Instance.JoinLobby(activeLobbies[tempI]));
            }
        }
    }

    /// <summary>
    /// Called when the initial menu  button is presseed.
    /// </summary>
    public void OnInitialMenuOpened()
    {
        UIInitialMenuParent.SetActive(true);

        // Close all
        UIHostGameMenuParent.SetActive(false);
        UIJoinGamePanel.SetActive(false);
    }

    /// <summary>
    /// Called when host game menu button pressed.
    /// </summary>
    public void OnHostGameMenuOpen()
    {
        // Open the host game panel
        UIHostGameMenuParent.SetActive(true);

        // Hide the initial menu
        UIInitialMenuParent.SetActive(false);
    }


    /// <summary>
    /// Called when join game button is pressed.
    /// </summary>
    public void OnJoinGameMenuOpened()
    {
        // Refresh the menu
        RefreshLobbiesPressedAsync();

        // Open join game panel
        UIJoinGamePanel.SetActive(true);

        // Hide the initial menu
        UIInitialMenuParent.SetActive(false);
    }


    /// <summary>
    /// Closes all the possible open panels
    /// </summary>
    public void OnCloseAllPanels()
    {
        UIInitialMenuParent.SetActive(false);
        UIJoinGamePanel.SetActive(false);
        UIHostGameMenuParent.SetActive(false);
    }


    public async void CreateLobbyPressedAsync()
    {
        if (await LobbyManager.Instance.CreateLobby(2))
        {

        }
    }
}
