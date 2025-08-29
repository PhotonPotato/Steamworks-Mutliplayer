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
    [SerializeField] private Lobby hostedLobby;
    [SerializeField] public List<Lobby> activeLobbies;

    [Header("Refs")]
    public Transform UILobbyListContentParent;
    public GameObject UILobbyListingPrefab;

    private void Start()
    {
        activeLobbies = new List<Lobby>();
    }

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

    public async void RefreshLobbiesPressedAsync()
    {
        if (await RefreshMultiplayerLobbies())
        {
            Log($"Active lobby count: {activeLobbies.Count}");
            Log($"Listed lobby count: {UILobbyListContentParent.childCount}");

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
            }
        }
    }


    /// <summary>
    /// Debug.Log Wrapper (I feel like Ima be using htis a lot)
    /// </summary>
    /// <param name="msg">Message to log.</param>
    public static void Log(object msg) { Debug.Log(msg); Console.Log(msg); }
}
