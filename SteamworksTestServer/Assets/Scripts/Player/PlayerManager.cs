using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public bool IS_IN_TEST_SCENE = false;

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
        if (IS_IN_TEST_SCENE) RunClientFixedUpdate();
    }

    public void RunClientFixedUpdate()
    {
        // Get new input, save it
        cur = InputHandler.GeneratePlayerInputSnapshot();
        // Run CharacterController update
        CharacterController.RunPlayerUpdateWithInput(cur);
        // Send it up to the server
        SteamManager.Instance?.SendPlayerInputSnapshot(cur);
    }
}
