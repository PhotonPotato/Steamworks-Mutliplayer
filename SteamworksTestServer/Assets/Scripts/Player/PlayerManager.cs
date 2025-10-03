using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Debug Flags")]
    public bool IS_IN_TEST_SCENE = false;
    public bool SEND_INPUT_SNAPSHOTS_TO_MONITOR = true;

    private PlayerInputHandler InputHandler;
    public PlayerCharacterController CharacterController { get; private set; }

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
        Cursor.lockState = (Console.Instance?.open?? false) ? CursorLockMode.Confined : CursorLockMode.Locked;
        Cursor.visible = Console.Instance?.open ?? false;

        // Get new input, save it
        currentInputSnapshot = InputHandler.GeneratePlayerInputSnapshot();

        if (SEND_INPUT_SNAPSHOTS_TO_MONITOR) InputLossMonnitor.Instance?.AddClientInputSnapshot(currentInputSnapshot);

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
