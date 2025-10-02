using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public bool IS_IN_TEST_SCENE = false;

    private PlayerInputHandler InputHandler;
    private PlayerCharacterController CharacterController;

    private PlayerInputSnapshot currentInputSnapshot;

    public int InputLogLength = 10;
    Queue<PlayerInputSnapshot> previousInputsLog = new Queue<PlayerInputSnapshot>();

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
        currentInputSnapshot = InputHandler.GeneratePlayerInputSnapshot();

        // Update the input log
        previousInputsLog.Enqueue(currentInputSnapshot);

        if (previousInputsLog.Count > InputLogLength)
            previousInputsLog.Dequeue();

        // Run CharacterController update
        CharacterController.RunPlayerUpdateWithInput(currentInputSnapshot);

        // Send it up to the server
        SteamManager.Instance?.SendPlayerInputSnapshotBundle(new()
        {
            snapshots = previousInputsLog.ToArray()
        });
    }

    public void RunServerFixedUpdate(PlayerInputSnapshot inputSnapshot)
    {
        currentInputSnapshot = inputSnapshot;

        // Run CharacterController update
        CharacterController.RunPlayerUpdateWithInput(currentInputSnapshot);

        // Reset cur
        currentInputSnapshot = new PlayerInputSnapshot();
    }
}
