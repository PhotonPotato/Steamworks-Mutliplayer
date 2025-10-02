using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

[RequireComponent(typeof(CharacterController))]
public class PlayerCharacterController : MonoBehaviour
{
    [NonSerialized] public InputSystemFirstPersonControls inputActions;
    //[NonSerialized] public WeaponController m_weaponController;

    public PhysicsScene ThisPhysicsScene;

    private CharacterController controller;

    [SerializeField] public Camera cam;
    [SerializeField] private float movementSpeed = 2.0f;
    [SerializeField] public float lookSensitivity = 1.0f;
    [SerializeField] public float adsSensitivityMultiplier = 1.0f;

    private PlayerInput m_PlayerInput;
    private PlayerInputHandler m_PlayerInputHandler;

    private PlayerInputSnapshot currentInputState;

    //private PlayerInventoryManager m_PlayerInventoryManager;

    public bool usingGamepad = false;
    private XInputController m_Gamepad;

    public float groundDrag = .8f;
    public float aerialDrag = .9f;

    private float xRotation = 0f;
    private Vector3 m_LatestImpactSpeed;

    [Header("Movement Vars")]
    public Vector3 CharacterVelocity;
    public float gravity = -9.81f;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;

    public float cayoteTime = .1f;
    public float cayoteTimer = 0;

    // Zoom Vars - Zoom code adapted from @torahhorse's First Person Drifter scripts.
    public float zoomFOV = 35.0f;
    public float zoomSpeed = 9f;
    [SerializeField] private float targetFOV;

    public bool isInMenu = false;

    [SerializeField] public bool isADS { get; private set; }

    [SerializeField] private float baseFOV;
    [SerializeField] private float sprintFov;
    public float additionalFOVFromSprinting = 9f;

    // Crouch Vars
    private float initHeight;
    [SerializeField] private float crouchHeight;
    private float m_LastTimeJumped;
    private bool jumpWasPressed = false;
    private bool HasPressedJumpThisFrame = false;

    public float MaxSpeedOnGround = 10f;
    public float MaxSpeedInAir = 10f;
    public float MaxSpeedCrouchedRatio = .5f;
    public float AccelerationSpeedInAir = 25f;
    private float m_FootstepDistanceCounter;

    public float MovementSharpnessOnGround = 15f;

    public float JumpForce = 9f;
    public float SprintSpeedModifier = 1.5f;

    private void Awake()
    {
        inputActions = new InputSystemFirstPersonControls();

        //m_PlayerInventoryManager = GetComponent<PlayerInventoryManager>();
        //m_weaponController = GetComponent<WeaponController>();

        m_PlayerInput = GetComponent<PlayerInput>();
        m_PlayerInputHandler = GetComponent<PlayerInputHandler>();

        //Check the input type
        if (m_PlayerInput.devices[0] is XInputController)
        {
            usingGamepad = true;
            m_Gamepad = m_PlayerInput.devices[0] as XInputController;

            //GetComponent<WeaponController>().m_Controller = m_Gamepad;
        }
    }

    private void Start()
    {
        Debug.Log("Startup");
        controller = GetComponent<CharacterController>();
        initHeight = controller.height;

        // Set the fov
        SetFOVs(cam.fieldOfView);
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void Update()
    {
        /*currentInputState = m_PlayerInputHandler.GeneratePlayerINputSnapshot();

        
        //DEBUG>> FOR PC SO YOU CAN SEE THE MOUSE
        if (false)//m_PlayerInventoryManager.inventoryPanelOpen || m_PlayerInventoryManager.sellPanelOpen || m_PlayerInventoryManager.buyPanelOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            isInMenu = true;
        }
        else //Let the player move then
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            DoLooking();
            DoZoom();
            DoCrouch();

            isInMenu = false;
        }
        */
        if (cayoteTimer >  0) cayoteTimer -= Time.deltaTime;
    }


    /// <summary>
    /// Runs a player update given a specific input snapshot
    /// </summary>
    /// <param name="snapshot">The given input snapshot to run player controls with</param>
    public void RunPlayerUpdateWithInput(PlayerInputSnapshot snapshot)
    {
        currentInputState = snapshot;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        DoMovement();
        DoLooking();
        DoZoom();
        DoCrouch();

        isInMenu = false;
    }

    private void DoLooking()
    {
        Vector2 looking = GetPlayerLookInput();
        float lookX = looking.x * lookSensitivity * Time.fixedDeltaTime;
        float lookY = looking.y * lookSensitivity * Time.fixedDeltaTime;

        if (isADS)
        {
            //lookX *= m_weaponController.currentWeaponBehavior.lookSensitivityMultiplierOnADS * adsSensitivityMultiplier;
            //lookY *= m_weaponController.currentWeaponBehavior.lookSensitivityMultiplierOnADS * adsSensitivityMultiplier;
        }

        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        transform.Rotate(Vector3.up * lookX);
    }

    private void DoMovement()
    {
        HasPressedJumpThisFrame = false;

        isGrounded = ThisPhysicsScene.Raycast(transform.position, Vector3.down, 1.1f, LayerMask.GetMask("Ground")) || controller.isGrounded;

        isSprinting = GetPlayerSprintInput();

        float speedModifier = isSprinting ? SprintSpeedModifier : 1f;

        // converts move input to a worldspace vector based on our character's transform orientation
        Vector3 worldspaceMoveInput = transform.TransformVector(GetPlayerMovement());

        // handle grounded movement
        if (isGrounded)
        {
            //Update the cayote time
            cayoteTimer = cayoteTime;

            // calculate the desired velocity from inputs, max speed, and current slope
            Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
            // reduce speed if crouching by crouch speed ratio
            if (isCrouching)
                targetVelocity *= MaxSpeedCrouchedRatio;
            //targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
            //                 targetVelocity.magnitude;
            

            // smoothly interpolate between our current velocity and the target velocity based on acceleration speed
            CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                MovementSharpnessOnGround * Time.fixedDeltaTime);

            /*
            // TODO: footsteps sound
            float chosenFootstepSfxFrequency =
                (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
            if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
            {
                m_FootstepDistanceCounter = 0f;
                AudioSource.PlayOneShot(FootstepSfx);
            }*/

            // keep track of distance traveled for footsteps sound
            m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.fixedDeltaTime;
        }
        // handle air movement
        else
        {
            // add air acceleration
            CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.fixedDeltaTime;

            // limit air speed to a maximum, but only horizontally
            float verticalVelocity = CharacterVelocity.y;
            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
            horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
            CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

            // apply the gravity to the velocity
            CharacterVelocity += Vector3.down * gravity * Time.fixedDeltaTime;
        }

        // jumping
        if (GetPlayerJumpInputDown() && (isGrounded || cayoteTimer > 0))
        {
            if (cayoteTimer > 0 && !isGrounded) Debug.Log("cayote");

            // force the crouch state to false
            if (SetCrouchingState(false, false))
            {
                // start by canceling out the vertical component of our velocity
                CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);

                // then, add the jumpSpeed value upwards
                CharacterVelocity += Vector3.up * JumpForce;

                // play sound
                //AudioSource.PlayOneShot(JumpSfx);

                // remember last time we jumped because we need to prevent snapping to ground for a short time
                m_LastTimeJumped = Time.time;
                HasPressedJumpThisFrame = true;

                // Force grounding to false
                isGrounded = false;
                cayoteTimer = 0;

                //m_GroundNormal = Vector3.up;
            }
        }

        // apply the final calculated velocity value as a character movement
        Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
        Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(controller.height);
        controller.Move(CharacterVelocity * Time.fixedDeltaTime);

        // detect obstructions to adjust velocity accordingly
        m_LatestImpactSpeed = Vector3.zero;
        if (ThisPhysicsScene.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, controller.radius,
            CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.fixedDeltaTime, -1,
            QueryTriggerInteraction.Ignore))
        {
            // We remember the last impact speed because the fall damage logic might need it
            m_LatestImpactSpeed = CharacterVelocity;

            CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
        }
    }

    private void DoZoom()
    {
        if (m_PlayerInput.actions["Zoom"].ReadValue<float>() > 0)
        {
            targetFOV = zoomFOV;
            isADS = true;
        }
        else
        {
            targetFOV = baseFOV;
            isADS = false;
        }

        if (isSprinting) targetFOV = sprintFov;

        UpdateZoom();
    }

    private void DoCrouch()
    {
        if (currentInputState.crouchInput)
        {
            controller.height = crouchHeight;
        }
        else
        {
            if (ThisPhysicsScene.Raycast(transform.position, transform.TransformDirection(Vector3.up), 2.0f, -1))
            {
                controller.height = crouchHeight;
            }
            else
            {
                controller.height = initHeight;
            }
        }
    }

    private void UpdateZoom()
    {
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, zoomSpeed * Time.fixedDeltaTime);
    }

    public void SetFOVs(float baseFOV, float overrideSprintFOV = -1)
    {
        SetBaseFOV(baseFOV);
        SetSprintFov(overrideSprintFOV);
    }

    public void SetBaseFOV(float fov)
    {
        baseFOV = fov;
    }

    public void SetSprintFov(float overrideFOV = -1)
    {
        sprintFov = baseFOV + (overrideFOV == -1 ? additionalFOVFromSprinting : overrideFOV);
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    public Vector3 GetPlayerMovement()
    {
        Vector2 rawInput = currentInputState.moveInput;

        //I think the laggy movement sometimes comes from input system sucking ass here
        //Debug.Log(rawInput.ToString()); 

        //if (isADS) rawInput *= m_weaponController.currentWeaponBehavior.movementSpeedMultiplierOnADS;

        return new Vector3(rawInput.x, 0f, rawInput.y);
    }

    public Vector2 GetPlayerLookInput()
    {
        return currentInputState.lookInput;
    }

    public bool GetPlayerSprintInput()
    {
        //Dont allow sprinting if the gun has a scope
        //if (isADS && m_weaponController.currentWeaponBehavior.type == WeaponType.Sniper) return false;

        return currentInputState.sprintInput;
    }

    public bool GetPlayerJumpInputDown()
    {
        //Don't let teh player jump if they are in a menu,
        //the same binding is used to interact with menus (the "a" button)
        if (isInMenu) return false;

        bool jumpRaw = currentInputState.jumpInput;
        bool output;

        if (jumpRaw && !jumpWasPressed)
        {
            output = true;
        }
        else
        {
            output = false;
        }

        jumpWasPressed = jumpRaw;

        return output;
    }

    // Gets the center point of the bottom hemisphere of the character controller capsule    
    Vector3 GetCapsuleBottomHemisphere()
    {
        return transform.position + (transform.up * controller.radius);
    }

    // Gets the center point of the top hemisphere of the character controller capsule    
    Vector3 GetCapsuleTopHemisphere(float atHeight)
    {
        return transform.position + (transform.up * (atHeight - controller.radius));
    }

    bool SetCrouchingState(bool crouched, bool ignoreObstructions)
    {
        // set appropriate heights
        if (crouched)
        {
            //m_TargetCharacterHeight = CapsuleHeightCrouching;
        }
        else
        {
            /*
            // Detect obstructions
            if (!ignoreObstructions)
            {
                Collider[] standingOverlaps = Physics.OverlapCapsule(
                    GetCapsuleBottomHemisphere(),
                    GetCapsuleTopHemisphere(CapsuleHeightStanding),
                    controller.radius,
                    -1,
                    QueryTriggerInteraction.Ignore);
                foreach (Collider c in standingOverlaps)
                {
                    if (c != controller)
                    {
                        return false;
                    }
                }
            }/*

            //m_TargetCharacterHeight = CapsuleHeightStanding;
        }

       /* if (OnStanceChanged != null)
        {
            OnStanceChanged.Invoke(crouched);
        }*/

            
        }
        isCrouching = crouched;
        return true;
    }
}
