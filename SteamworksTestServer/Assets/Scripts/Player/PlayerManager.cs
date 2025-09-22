using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private PlayerInputHandler InputHandler;
    private PlayerCharacterController CharacterController;

    private PlayerInputSnapshot cur;

    private void Awake()
    {
        InputHandler = GetComponent<PlayerInputHandler>();
        CharacterController = GetComponent<PlayerCharacterController>();
    }

    public void FixedUpdate()
    {
        // Get new input, save it
        cur = InputHandler.GeneratePlayerInputSnapshot();
        // Send it up to the server
        SteamManager.Instance?.SendPlayerInputSnapshot(cur);
        // Run CharacterController update
        CharacterController.RunPlayerUpdateWithInput(cur);
    }
}
