using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientCorrectionManager : MonoBehaviour
{
    public static ClientCorrectionManager Instance;

    [Header("Refs")]
    public PlayerCharacterController controller;
    
    [Header("Settings")]
    public float maximumPosError = .1f;
    public float maximumLookError = .1f;

    [Header("Trackers")]
    public List<PlayerPhysicsStateMessage> previousPhysicsStatesLog = new List<PlayerPhysicsStateMessage>();

    public uint lastRecievedStateTick { get; private set; } = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }

    public void RunFixedUpdate()
    {
        // Throw a new fram of player position in the log
        previousPhysicsStatesLog.Add(new()
        {
            gameTick = TimeKeeper.Instance?.gameTick ?? 0,

            position = transform.position,
            velocity = controller.CharacterVelocity,
            look = transform.rotation
        });

        // Check if the log is too long
        if (previousPhysicsStatesLog.Count > GameManager.Instance?.thisPlayerManager.InputLogLength)
            previousPhysicsStatesLog.RemoveAt(0);
    }

    private void Start()
    {
        UpdatePlayerCharacterController();
    }

    private void UpdatePlayerCharacterController()
    {
        controller = GameManager.Instance?.thisPlayerManager.CharacterController;
    }

    /// <summary>
    /// We want this to:
    /// - compare the received state with the corresponding physics
    ///   state we have saved in the log.
    ///  - If the discrepancy is too high, reset it and
    ///    call for a physics rerun.
    /// </summary>
    /// <param name="state"> The new physics state from the server </param>
    public void OnReceivedNewPlayerStateFromServer(PlayerPhysicsStateMessage state)
    {
        lastRecievedStateTick = state.gameTick;

        if (controller == null) UpdatePlayerCharacterController();

        //Console.Log("Received new state from server");

        PlayerPhysicsStateMessage loggedState = previousPhysicsStatesLog.Find(f => f.gameTick == state.gameTick);

        // If it equals an empty player physics state
        if (loggedState.Equals(new PlayerPhysicsStateMessage()))
        {
            // Then ignore this frame
            Debug.Log("State tick not found");
            return;
        }
        else
        {
            // Run comparison
            float posErr = state.ComparePosAndVelTo(loggedState);
            float lookErr = state.CompareLookTo(loggedState);

            bool correctionMade = false;

            //Debug.Log("prediction error " + err);
            //Debug.Log($"Frame difference {TimeKeeper.Instance.gameTick - state.gameTick}. Server: {state.gameTick}. Cur frame: {TimeKeeper.Instance.gameTick}");

            if (posErr >= maximumPosError)
            {
                // Reset player state to how it should be
                SetPlayerPositionAndVelocity(state);

                Debug.Log(state.position);

                correctionMade = true;
            }

            if (lookErr >= maximumLookError)
            {
                SetPlayerRotation(loggedState);

                Debug.Log(state.look);

                correctionMade = true;
            }


            if (correctionMade)
            {
                Physics.SyncTransforms();

                // Rerun player physics from there on back to current
                GameManager.Instance?.thisPlayerManager.ForceRunFramesOfClientInputFromFrame(state.gameTick);
            }
        }
    }

    public void SetPlayerToState(PlayerPhysicsStateMessage state)
    {
        if (controller == null) UpdatePlayerCharacterController();

        //Console.Log("Updating state");
        controller.transform.position = state.position;
        controller.CharacterVelocity = state.velocity;
        controller.transform.rotation = state.look;
    }

    public void SetPlayerPositionAndVelocity(PlayerPhysicsStateMessage state)
    {
        if (controller == null) UpdatePlayerCharacterController();

        controller.transform.position = state.position;
        controller.CharacterVelocity = state.velocity;
    }

    public void SetPlayerRotation(PlayerPhysicsStateMessage state)
    {
        if (controller == null) UpdatePlayerCharacterController();

        controller.transform.rotation = state.look;
    }
}
