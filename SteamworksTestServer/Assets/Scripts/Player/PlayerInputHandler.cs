using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput m_PlayerInput;
    private InputAction m_MoveInput;
    private InputAction m_LookInput;
    private InputAction m_SprintInput;
    private InputAction m_JumpInput;
    private InputAction m_CrouchInput;

    //private PlayerInputSnapshot[] 

    private void Awake()
    {
        // Setup all of the input actions
        m_PlayerInput = GetComponent<PlayerInput>();

        m_MoveInput = m_PlayerInput.actions["Move"];
        m_LookInput = m_PlayerInput.actions["Look"];
        m_SprintInput = m_PlayerInput.actions["Sprint"];
        m_JumpInput = m_PlayerInput.actions["Jump"];
        m_CrouchInput = m_PlayerInput.actions["Crouch"];
    }

    public PlayerInputSnapshot GeneratePlayerInputSnapshot()
    {
        return new()
        {
            gameTick = TimeKeeper.Instance?.GetGameTick() ?? 0,
            moveInput = m_MoveInput.ReadValue<Vector2>(),
            lookInput = m_LookInput.ReadValue<Vector2>(),
            sprintInput = m_SprintInput.ReadValue<float>() == 1,
            jumpInput = m_JumpInput.ReadValue<float>() > 0,
            crouchInput = m_CrouchInput.ReadValue<float>() > 0
        };
    }
}