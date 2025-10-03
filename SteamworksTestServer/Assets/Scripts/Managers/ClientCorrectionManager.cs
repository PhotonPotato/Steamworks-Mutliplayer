using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientCorrectionManager : MonoBehaviour
{
    public static ClientCorrectionManager Instance;

    [Header("Refs")]
    public PlayerCharacterController controller;

    [Header("Settings")]
    public float maximumError = .1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }

    private void Start()
    {
        UpdatePlayerCharacterController();
    }

    private void UpdatePlayerCharacterController()
    {
        controller = GameManager.Instance?.thisPlayerManager.CharacterController;
    }

    public void SetPlayerToState(PlayerPhysicsStateMessage state)
    {
        if (controller == null) UpdatePlayerCharacterController();

        Console.Log("Updating state");
        controller.transform.position = state.position;
        controller.CharacterVelocity = state.velocity;
        controller.transform.rotation = state.look;
    }
}
