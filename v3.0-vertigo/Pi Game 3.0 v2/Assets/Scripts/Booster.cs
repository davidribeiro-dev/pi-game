using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Booster : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public AdvancedMovement advancedMovement;

    [Header("Gear State")]
    public bool isActivated = true;

    [Header("Dash Settings")]
    public float dashSpeed = 12f;
    public float minDashSpeed = 6f;
    public float dashDuration = 0.2f;
    public float groundDashCooldown = 0.3f;

    [Header("Momentum Settings")]
    public float speedBoostRetentionTime = 1.5f;

    [Header("Dash Charge")]
    public float maxChargeTime = 1.3f;

    [Header("Double Jump")]
    public bool allowDoubleJump = true;
    public float doubleJumpHeight = 1.2f;

    private CharacterController controller;
    private PlayerInputHandler localInputHandler;
    private float dashCooldownTimer;
    private float currentChargeTime;
    private bool isCharging;
    private bool hasPendingDash;
    private Vector3 pendingDashDirection;
    private float pendingDashSpeed;
    private float pendingDashDuration;
    private float pendingMomentumRetention;

    private bool canUseDoubleJump;

    public bool IsCharging => isCharging;
    public bool IsActivated => isActivated;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        localInputHandler = GetComponent<PlayerInputHandler>();

        if (orientation == null)
            orientation = transform;

        if (advancedMovement == null)
            advancedMovement = GetComponent<AdvancedMovement>();

        canUseDoubleJump = controller != null && controller.isGrounded && isActivated && allowDoubleJump;
    }

    private PlayerInputHandler GetInputHandler()
    {
        return localInputHandler;
    }

    private void Update()
    {
        if (controller == null || GetInputHandler() == null)
            return;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (controller.isGrounded)
            canUseDoubleJump = isActivated && allowDoubleJump;

        if (!isActivated)
        {
            isCharging = false;
            currentChargeTime = 0f;
            hasPendingDash = false;
            return;
        }

        HandleDashChargeAndRequest();
    }

    public void SetActivated(bool value)
    {
        isActivated = value;

        if (!isActivated)
        {
            isCharging = false;
            currentChargeTime = 0f;
            hasPendingDash = false;
            canUseDoubleJump = false;
            return;
        }

        canUseDoubleJump = controller != null && controller.isGrounded && allowDoubleJump;
    }

    public bool TryConsumeDoubleJump(out float jumpHeight)
    {
        jumpHeight = 0f;

        if (!isActivated || !allowDoubleJump || !canUseDoubleJump)
            return false;

        if (advancedMovement != null && advancedMovement.IsTouchingWallForAirJumpBlock())
            return false;

        canUseDoubleJump = false;
        jumpHeight = doubleJumpHeight;
        return true;
    }

    public bool ConsumeDashRequest(out Vector3 direction, out float speed, out float duration, out float momentumRetention)
    {
        if (hasPendingDash)
        {
            direction = pendingDashDirection;
            speed = pendingDashSpeed;
            duration = pendingDashDuration;
            momentumRetention = pendingMomentumRetention;

            hasPendingDash = false;
            return true;
        }

        direction = Vector3.zero;
        speed = 0f;
        duration = 0f;
        momentumRetention = 0f;
        return false;
    }

    private void HandleDashChargeAndRequest()
    {
        PlayerInputHandler input = GetInputHandler();
        if (input == null)
            return;

        if (isCharging)
        {
            currentChargeTime -= Time.deltaTime;
            if (currentChargeTime <= 0f)
            {
                currentChargeTime = 0f;
                isCharging = false;
            }
        }

        if (!isCharging && input.ChargePressedThisFrame && dashCooldownTimer <= 0f)
        {
            isCharging = true;
            currentChargeTime = maxChargeTime;
        }

        if (!isCharging || !input.DashPressedThisFrame)
            return;

        if (advancedMovement != null && advancedMovement.IsNearWallForDash())
            return;

        QueueDashRequest();
        isCharging = false;
    }

    private void QueueDashRequest()
    {
        float chargeRatio = maxChargeTime > 0f ? Mathf.Clamp01(currentChargeTime / maxChargeTime) : 1f;
        float finalDashSpeed = Mathf.Lerp(minDashSpeed, dashSpeed, chargeRatio);

        Vector3 forward = orientation != null ? orientation.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;
        forward.Normalize();

        hasPendingDash = true;
        pendingDashDirection = forward;
        pendingDashSpeed = finalDashSpeed;
        pendingDashDuration = dashDuration;
        pendingMomentumRetention = speedBoostRetentionTime;

        Debug.Log($"Dash performed with speed: {finalDashSpeed:F2}");

        dashCooldownTimer = groundDashCooldown;
    }
}