using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public ulong attachedSteamId;

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

        if (SEND_INPUT_SNAPSHOTS_TO_MONITOR) InputLossMonitor.Instance?.AddClientInputSnapshot(currentInputSnapshot);

        // Update the input log and player position log
        previousInputsLog.Enqueue(currentInputSnapshot);

        while (previousInputsLog.Count > InputLogLength)
        {
            previousInputsLog.Dequeue();
        }

        InputLogLength = Mathf.Max(5, (int)(TimeKeeper.Instance.gameTick - ClientCorrectionManager.Instance.lastRecievedStateTick));

        Debug.Log($"Prev inputs length: {previousInputsLog.Count} Tick: {TimeKeeper.Instance.gameTick}");

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

    /// <summary>
    /// Runs all saved input frames AFTER starting frame as player input.
    /// </summary>
    /// <param name="startingFrame"></param>
    public void ForceRunFramesOfClientInputFromFrame(uint startingFrame)
    {
        Debug.Log("Running input runback from frame: " + startingFrame);
        Debug.Log("Start pos: " + transform.position);

        PlayerInputSnapshot[] snapshots = previousInputsLog.ToArray();

        ClientCorrectionManager.Instance.previousPhysicsStatesLog.Clear();

        bool frameFound = false;

        // Run a pre-movement physics sync
        Physics.SyncTransforms();

        // This should skip running the input from the actual starting frame
        for (int i = 0; i < snapshots.Length; i++)
        {
            if (frameFound)
            {
                Debug.Log("Running frame interation " + snapshots[i].gameTick);

                // Run CharacterController update
                CharacterController.RunPlayerUpdateWithInput(snapshots[i]);

                Physics.SyncTransforms();

                // Throw a new fram of player position in the log
                ClientCorrectionManager.Instance.previousPhysicsStatesLog.Add(new()
                {
                    gameTick = snapshots[i].gameTick,

                    position = transform.position,
                    velocity = CharacterController.CharacterVelocity,
                    look = transform.rotation
                });

                // Check if the log is too long
                while (ClientCorrectionManager.Instance.previousPhysicsStatesLog.Count > GameManager.Instance?.thisPlayerManager.InputLogLength)
                    ClientCorrectionManager.Instance.previousPhysicsStatesLog.RemoveAt(0);
            }
            else
            {
                // It has to first iterate through to find the first input tick snapshot before simulation
                if (snapshots[i].gameTick != startingFrame) continue;
                else frameFound = true;
            }
        }

        Debug.Log(transform.position);
    }
}
