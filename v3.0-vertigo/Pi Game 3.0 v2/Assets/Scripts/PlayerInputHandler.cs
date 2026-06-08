using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class PlayerInputHandler : MonoBehaviour
{
    [UnitHeaderInspectable("Input Action Asset")]
    public InputActionAsset playerControls;

    [Header("Input Action Maps")]
    public string actionMapName = "Player";

    [Header("Action Names")]
    public string move = "Move";
    public string look = "Look";
    public string jump = "Jump";
    public string sprint = "Sprint";
    public string dash = "Dash";
    public string charge = "Charge";
    public string wallAction = "WallAction";
    public string vault = "Vault";
    public string slide = "Slide";
    public string leftHook = "Left Hook";
    public string rightHook = "Right Hook";

    private InputActionAsset runtimePlayerControls;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction dashAction;
    private InputAction chargeAction;
    private InputAction vaultAction;
    private InputAction slideAction;
    private InputAction leftHookAction;
    private InputAction rightHookAction;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool JumpPressedThisFrame { get; private set; }
    public float SprintValue { get; private set; }
    public bool DashTriggered { get; private set; }
    public bool DashPressedThisFrame { get; private set; }
    public bool ChargeTriggered { get; private set; }
    public bool ChargePressedThisFrame { get; private set; }
    public bool VaultTriggered { get; private set; }
    public bool SlideTriggered { get; private set; }
    public bool LeftHookTriggered { get; private set; }
    public bool LeftHookPressedThisFrame { get; private set; }
    public bool LeftHookReleasedThisFrame { get; private set; }
    public bool RightHookTriggered { get; private set; }
    public bool RightHookPressedThisFrame { get; private set; }
    public bool RightHookReleasedThisFrame { get; private set; }

    private void Awake()
    {
        if (playerControls == null)
        {
            Debug.LogError("PlayerInputHandler: playerControls is not assigned.", gameObject);
            return;
        }

        runtimePlayerControls = Instantiate(playerControls);

        InputActionMap playerMap = runtimePlayerControls.FindActionMap(actionMapName, false);
        if (playerMap == null)
        {
            Debug.LogError($"PlayerInputHandler: action map '{actionMapName}' not found.", gameObject);
            return;
        }

        moveAction = playerMap.FindAction(move, false);
        lookAction = playerMap.FindAction(look, false);
        jumpAction = playerMap.FindAction(jump, false);
        sprintAction = playerMap.FindAction(sprint, false);
        dashAction = playerMap.FindAction(dash, false);
        chargeAction = playerMap.FindAction(charge, false);
        vaultAction = playerMap.FindAction(vault, false);
        slideAction = playerMap.FindAction(slide, false);
        leftHookAction = playerMap.FindAction(leftHook, false);
        rightHookAction = playerMap.FindAction(rightHook, false);

        if (moveAction == null || lookAction == null || jumpAction == null || sprintAction == null ||
            dashAction == null || chargeAction == null || vaultAction == null || slideAction == null ||
            leftHookAction == null || rightHookAction == null)
        {
            Debug.LogError("PlayerInputHandler: one or more required input actions were not found.", gameObject);
            return;
        }

        RegisterInputActions();
    }

    void RegisterInputActions()
    {
        moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
        moveAction.canceled += ctx => MoveInput = Vector2.zero;

        lookAction.performed += ctx => LookInput = ctx.ReadValue<Vector2>();
        lookAction.canceled += ctx => LookInput = Vector2.zero;

        jumpAction.performed += ctx => JumpTriggered = true;
        jumpAction.canceled += ctx => JumpTriggered = false;
        jumpAction.performed += ctx => JumpPressedThisFrame = true;

        sprintAction.performed += ctx => SprintValue = ctx.ReadValue<float>();
        sprintAction.canceled += ctx => SprintValue = 0f;

        dashAction.performed += ctx => DashTriggered = true;
        dashAction.performed += ctx => DashPressedThisFrame = true;
        dashAction.canceled += ctx => DashTriggered = false;

        chargeAction.performed += ctx => ChargeTriggered = true;
        chargeAction.performed += ctx => ChargePressedThisFrame = true;
        chargeAction.canceled += ctx => ChargeTriggered = false;

        vaultAction.performed += ctx => VaultTriggered = true;
        vaultAction.canceled += ctx => VaultTriggered = false;

        slideAction.performed += ctx => SlideTriggered = true;
        slideAction.canceled += ctx => SlideTriggered = false;

        leftHookAction.performed += ctx => LeftHookTriggered = true;
        leftHookAction.performed += ctx => LeftHookPressedThisFrame = true;
        leftHookAction.canceled += ctx => LeftHookTriggered = false;
        leftHookAction.canceled += ctx => LeftHookReleasedThisFrame = true;

        rightHookAction.performed += ctx => RightHookTriggered = true;
        rightHookAction.performed += ctx => RightHookPressedThisFrame = true;
        rightHookAction.canceled += ctx => RightHookTriggered = false;
        rightHookAction.canceled += ctx => RightHookReleasedThisFrame = true;
    }


    private void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();    
        jumpAction?.Enable();
        sprintAction?.Enable();
        dashAction?.Enable();
        chargeAction?.Enable();
        vaultAction?.Enable();
        slideAction?.Enable();
        leftHookAction?.Enable();
        rightHookAction?.Enable();
    }
    private void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        dashAction?.Disable();
        chargeAction?.Disable();
        vaultAction?.Disable();
        slideAction?.Disable();
        leftHookAction?.Disable();
        rightHookAction?.Disable();
    }

    private void OnDestroy()
    {
        if (runtimePlayerControls != null)
            Destroy(runtimePlayerControls);
    }

    void Start()
    {
        
    }
    void LateUpdate()
    {
        JumpPressedThisFrame = false;
        DashPressedThisFrame = false;
        ChargePressedThisFrame = false;
        LeftHookPressedThisFrame = false;
        LeftHookReleasedThisFrame = false;
        RightHookPressedThisFrame = false;
        RightHookReleasedThisFrame = false;
    }
}
