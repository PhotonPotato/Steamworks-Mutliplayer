using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OtherPlayersManager : MonoBehaviour
{
    public static OtherPlayersManager Instance;

    private GameObject[] players => GameManager.Instance.playerObjects;
    private Dictionary<ulong, int> steamIdToIndex => GameManager.Instance.steamIdToIndex;

    public PlayerPhysicsStateBundle allStates;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this.gameObject);
    }

    public void PostPhysicsUpdate()
    {
        for (int i = 0; i < allStates.ids.Length; i++)
        {
            UpdatePlayerState(allStates.ids[i], allStates.states[i]);
        }
    }

    public void UpdatePlayerState(ulong id, PlayerPhysicsStateMessage state)
    {
        if (steamIdToIndex[id] == 0) return;

        PlayerManager man = players[steamIdToIndex[id]].GetComponent<PlayerManager>();

        man.CharacterController.CharacterVelocity = state.velocity;

        man.transform.position = state.position;
        man.transform.rotation = state.look;
    }

    public void OnNewPlayerState(PlayerPhysicsStateBundle states) => allStates = states;
}
