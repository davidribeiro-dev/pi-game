using UnityEngine;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    private AdvancedMovement advancedMovement;
    private Booster booster;
    private ODMGear odmGear;

    [Header("Speeds")]
    public float walkSpeed = 4f;
    public float sprintSpeed = 7f;

    [Header("Acceleration")]
    public float groundAccel = 30f;
    public float airAccel = 8f;
    public float groundDrag = 6f;

    [Header("Wallrun (Commit 2)")]
    public float wallRunRedirectAccel = 30f;
    public float wallRunGravityScale = 0.5f;
    public float wallRunMaxDownSpeed = 8f;
    public float wallRunStartUpBoost = 2f;
    public float wallRunSpeedGainPerSecond = 2f;
    public float wallRunMaxSpeed = 9f;

    [Header("Jumping")]
    public float jumpHeight = 1.6f;
    public float gravity = 9.81f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.15f;

    [Header("Long Jump Tuning")]
    public float highLongJumpHeight = 2.2f;
    public float highLongJumpForwardSpeed = 9f;
    public float ledgeLongJumpHeight = 1.4f;
    public float ledgeLongJumpForwardSpeed = 13f;
    public float wallDirectionalLongJumpHeight = 1.8f;
    public float wallDirectionalLongJumpSpeed = 10f;
    public float wallStaticLongJumpHeight = 2.6f;
    public float wallStaticPushAwaySpeed = 0f;

    [Header("Input")]
    public float inputDeadZone = 0.01f;

    [Header("Debug")]
    public bool logJumpMessages = true;

    [Header("Slope")]
    public bool enableSlopeSlide = true;
    public float slideSpeed = 4f;
    public float slideAcceleration = 10f;
    public float slopeStickAngle = 2f;

    private CharacterController controller;
    private PlayerInputHandler localInputHandler;
    private Vector3 velocity;
    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity;
    private Vector3 groundNormal = Vector3.up;
    private bool sliding = false;
    private float currentSurfaceFriction = 1f;

    private Vector2 cachedMoveInput;
    private float cachedSprintValue;
    private bool cachedJumpPressed;
    private bool cachedGrounded;
    private Vector3 cachedInputDir;
    private float cachedTargetSpeed;
    private Vector3 cachedTargetHorizontalVel;
    private float cachedSlopeAngle;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool previousJumpPressed = false;
    private float wallRunCurrentSpeed = 0f;
    private bool wasWallRunningLastFrame = false;

    private bool longJumpAppliedThisFrame = false;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private Vector3 activeDashVelocity;
    private float speedBoostMultiplier = 1f;
    private float speedBoostTimer = 0f;

    public event Action OnDoubleJump;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        localInputHandler = GetComponent<PlayerInputHandler>();
        advancedMovement = GetComponent<AdvancedMovement>();
        booster = GetComponent<Booster>();
        odmGear = GetComponent<ODMGear>();
        ValidateReferences();
    }

    private PlayerInputHandler GetInputHandler()
    {
        return localInputHandler;
    }

    void ValidateReferences()
    {
        if (orientation == null)
            orientation = transform;

        if (GetInputHandler() == null)
            Debug.LogWarning("PlayerMovement: Local PlayerInputHandler not found. Movement will be disabled until initialized.");
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    public float GetVerticalVelocity()
    {
        return verticalVelocity;
    }

    void Update()
    {
        if (GetInputHandler() == null || controller == null)
            return;

        ReadInputs();
        UpdateGroundInfo();
        HandleJumping();
        HandleBoosterRequests();
        HandleMovement();
        HandleODMForces();
        ApplyMovement();

        longJumpAppliedThisFrame = false;
    }

    void ReadInputs()
    {
        PlayerInputHandler input = GetInputHandler();
        if (input == null)
            return;

        cachedMoveInput = input.MoveInput;
        cachedSprintValue = input.SprintValue;
        cachedJumpPressed = input.JumpTriggered;

        if (cachedMoveInput.magnitude < inputDeadZone)
            cachedMoveInput = Vector2.zero;

        cachedGrounded = controller.isGrounded;
    }

    void UpdateGroundInfo()
    {
        float rayDist = controller.height * 0.5f + 0.2f;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, controller.radius, Vector3.down, out hit, rayDist, ~0, QueryTriggerInteraction.Ignore))
        {
            groundNormal = hit.normal;

            var sf = hit.collider.GetComponent<SurfaceFriction>();
            currentSurfaceFriction = sf != null ? Mathf.Clamp(sf.friction, 0f, 5f) : 1f;
        }
        else
        {
            groundNormal = Vector3.up;
            currentSurfaceFriction = 1f;
        }

        if (cachedGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (cachedGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (cachedJumpPressed)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;
    }

    void HandleJumping()
    {
        if (advancedMovement != null && advancedMovement.ConsumeLongJumpRequest(out AdvancedMovement.LongJumpType ljType, out Vector3 ljDir))
        {
            jumpBufferTimer = -1f;
            coyoteTimer = 0f;
            ApplyLongJumpImpulse(ljType, ljDir);

            longJumpAppliedThisFrame = true;
            previousJumpPressed = true;
            return;
        }

        bool wallRunning = IsWallRunning();
        bool wallRunIntent = advancedMovement != null && advancedMovement.ShouldConsumeJumpForWallRun();

        if (wallRunning || wallRunIntent)
        {
            jumpBufferTimer = -1f;
            coyoteTimer = 0f;
            previousJumpPressed = true;
            return;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            LogJumpMessage("Normal Jump");
        }
        else if (!previousJumpPressed && cachedJumpPressed && !cachedGrounded && coyoteTimer <= 0f && !wallRunning && !wallRunIntent)
        {
            if (booster != null && booster.TryConsumeDoubleJump(out float boosterJumpHeight))
            {
                verticalVelocity = Mathf.Sqrt(boosterJumpHeight * 2f * gravity);
                OnDoubleJump?.Invoke();
                LogJumpMessage("Double Jump (Booster)");
            }
        }

        previousJumpPressed = cachedJumpPressed;
    }

    void HandleBoosterRequests()
    {
        if (booster != null && booster.ConsumeDashRequest(out Vector3 dashDirection, out float dashSpeed, out float dashDuration, out float momentumRetention))
        {
            isDashing = true;
            dashTimer = dashDuration;
            activeDashVelocity = dashDirection.normalized * dashSpeed;
            speedBoostMultiplier = dashSpeed;
            speedBoostTimer = momentumRetention;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                dashTimer = 0f;
                isDashing = false;
            }
        }
    }

    private void ApplyLongJumpImpulse(AdvancedMovement.LongJumpType jumpType, Vector3 requestedDirection)
    {
        Vector3 resolvedDirection = requestedDirection;
        resolvedDirection.y = 0f;

        if (resolvedDirection.sqrMagnitude < 0.0001f && orientation != null)
        {
            resolvedDirection = orientation.forward;
            resolvedDirection.y = 0f;
        }

        if (resolvedDirection.sqrMagnitude > 0.0001f)
            resolvedDirection.Normalize();

        switch (jumpType)
        {
            case AdvancedMovement.LongJumpType.HighGround:
                verticalVelocity = Mathf.Sqrt(highLongJumpHeight * 2f * gravity);
                currentHorizontalVelocity = resolvedDirection * highLongJumpForwardSpeed;
                LogJumpMessage("Long Jump: HighGround");
                break;

            case AdvancedMovement.LongJumpType.LedgeLong:
                verticalVelocity = Mathf.Sqrt(ledgeLongJumpHeight * 2f * gravity);
                currentHorizontalVelocity = resolvedDirection * ledgeLongJumpForwardSpeed;
                LogJumpMessage("Long Jump: LedgeLong");
                break;

            case AdvancedMovement.LongJumpType.WallDirectional:
                verticalVelocity = Mathf.Sqrt(wallDirectionalLongJumpHeight * 2f * gravity);
                currentHorizontalVelocity = resolvedDirection * wallDirectionalLongJumpSpeed;
                LogJumpMessage("Long Jump: WallDirectional");
                break;

            case AdvancedMovement.LongJumpType.WallStatic:
                verticalVelocity = Mathf.Sqrt(wallStaticLongJumpHeight * 2f * gravity);

                Vector3 pushAway = advancedMovement != null ? advancedMovement.WallNormal : Vector3.zero;
                pushAway.y = 0f;
                if (pushAway.sqrMagnitude > 0.0001f)
                    pushAway.Normalize();

                currentHorizontalVelocity = pushAway * wallStaticPushAwaySpeed;
                LogJumpMessage("Long Jump: WallStatic");
                break;

            default:
                verticalVelocity = Mathf.Sqrt(highLongJumpHeight * 2f * gravity);
                currentHorizontalVelocity = resolvedDirection * highLongJumpForwardSpeed;
                LogJumpMessage("Long Jump: DefaultFallback");
                break;
        }
    }

    private void LogJumpMessage(string jumpMessage)
    {
        if (!logJumpMessages)
            return;

        Debug.Log($"[PlayerMovement] {jumpMessage}");
    }

    void HandleMovement()
    {
        if (advancedMovement != null && advancedMovement.TryConsumeWallRunEndBoost(out float wallRunEndBoost))
        {
            verticalVelocity += wallRunEndBoost;
        }

        if (isDashing)
        {
            currentHorizontalVelocity = activeDashVelocity;
            wasWallRunningLastFrame = false;
            return;
        }

        if (!longJumpAppliedThisFrame)
        {
            cachedTargetSpeed = Mathf.Lerp(walkSpeed, sprintSpeed, Mathf.Clamp01(cachedSprintValue));

            Vector3 forward = orientation.forward;
            Vector3 right = orientation.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            cachedInputDir = (forward * cachedMoveInput.y + right * cachedMoveInput.x).normalized;

            cachedSlopeAngle = Vector3.Angle(groundNormal, Vector3.up);
            sliding = false;
            cachedTargetHorizontalVel = Vector3.zero;

            if (cachedGrounded && enableSlopeSlide)
            {
                if (cachedSlopeAngle > controller.slopeLimit + slopeStickAngle)
                {
                    sliding = true;
                    Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
                    currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, slideDir * slideSpeed, slideAcceleration * Time.deltaTime);
                }
                else
                {
                    Vector3 slopeMove = Vector3.ProjectOnPlane(cachedInputDir, groundNormal).normalized;
                    cachedTargetHorizontalVel = slopeMove * cachedTargetSpeed;

                    float accel = cachedGrounded ? groundAccel : airAccel;
                    currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, cachedTargetHorizontalVel, accel * Time.deltaTime);
                }
            }
            else
            {
                cachedTargetHorizontalVel = cachedInputDir * cachedTargetSpeed;
                float accel = cachedGrounded ? groundAccel : airAccel;
                currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, cachedTargetHorizontalVel, accel * Time.deltaTime);
            }

            float effectiveGroundDrag = groundDrag * currentSurfaceFriction;
            if (cachedGrounded && cachedInputDir == Vector3.zero && !sliding)
                currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, Vector3.zero, effectiveGroundDrag * Time.deltaTime);
        }

        bool wallRunning = IsWallRunning();

        if (wallRunning)
        {
            Vector3 wallForward = advancedMovement.GetWallForward();
            wallForward.y = 0f;
            wallForward.Normalize();

            if (!wasWallRunningLastFrame)
            {
                wallRunCurrentSpeed = Mathf.Max(currentHorizontalVelocity.magnitude, walkSpeed);
                verticalVelocity = Mathf.Max(verticalVelocity, wallRunStartUpBoost);
            }
            else
            {
                wallRunCurrentSpeed = Mathf.Max(wallRunCurrentSpeed, currentHorizontalVelocity.magnitude);
            }

            float maxWallSpeed = Mathf.Max(wallRunMaxSpeed, sprintSpeed);
            wallRunCurrentSpeed = Mathf.MoveTowards(wallRunCurrentSpeed, maxWallSpeed, wallRunSpeedGainPerSecond * Time.deltaTime);

            Vector3 targetVel = wallForward * wallRunCurrentSpeed;
            currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVel, wallRunRedirectAccel * Time.deltaTime);
        }
        else
        {
            wallRunCurrentSpeed = currentHorizontalVelocity.magnitude;
        }

        wasWallRunningLastFrame = wallRunning;
    }

    void ApplyMovement()
    {
        if (speedBoostTimer > 0f)
        {
            Vector3 forward = orientation.forward;
            Vector3 right = orientation.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 inputDir = (forward * cachedMoveInput.y + right * cachedMoveInput.x).normalized;
            if (inputDir.magnitude > 0.1f)
                controller.Move(inputDir * speedBoostMultiplier * Time.deltaTime);

            speedBoostTimer -= Time.deltaTime;
        }

        float g = gravity * (IsWallRunning() ? wallRunGravityScale : 1f);
        verticalVelocity -= g * Time.deltaTime;

        if (IsWallRunning() && verticalVelocity < -wallRunMaxDownSpeed)
            verticalVelocity = -wallRunMaxDownSpeed;

        velocity = currentHorizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleODMForces()
    {
        if (odmGear == null)
            return;

        if (isDashing)
            return;

        odmGear.ApplyForces(ref currentHorizontalVelocity, ref verticalVelocity, orientation, cachedMoveInput, Time.deltaTime);
    }

    private bool IsWallRunning()
    {
        return advancedMovement != null && advancedMovement.GetIsWallRunning();
    }
}