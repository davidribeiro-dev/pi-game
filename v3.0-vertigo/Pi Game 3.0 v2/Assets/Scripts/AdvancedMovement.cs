using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class AdvancedMovement : MonoBehaviour
{
    [Header("References")]
    public Transform orientation; // Player root for movement direction

    [Header("Charge Settings")]
    public float maxChargeTime = 1.3f; 
    private float currentChargeTime = 0f;
    private bool isCharging = false;

    [Header("Long Jump Conditions")]
    public float longJumpForwardInputThreshold = 0.1f;
    public float longJumpGroundCoyoteTime = 0.12f;
    public float ledgeForwardCheckDistance = 0.9f;
    public float ledgeCheckHeightOffset = 0.2f;
    public float ledgeDownCheckDistance = 2.2f;
    public LayerMask groundMask;
    public float wallLongJumpMaxFallSpeed = 7f;
    public float wallDirectionalInputThreshold = 0.15f;

    private CharacterController controller;
    private PlayerInputHandler localInputHandler;

    // --- Wallrun (Commit 1) ---
    [Header("Wallrun Settings")]
    public LayerMask wallMask;
    public float wallCheckDistance = 0.7f;
    public float wallCheckHeight = 1.0f;
    public float forwardInputThreshold = 0.1f;
    public float maxWallUpNormal = 0.2f;
    public float wallRunMaxDuration = 5f;
    public float wallRunEndVerticalBoost = 0.35f;
    public float airborneStartBuffer = 0.08f;
    public float groundedStableResetTime = 0.05f;

    [Header("Wallrun Debug")]
    public bool logWallRunDebug = true;

    public bool IsWallRunning { get; private set; }
    public Vector3 WallForward { get; private set; }
    public Vector3 WallNormal { get; private set; }

    public bool GetIsWallRunning()
    {
        return IsWallRunning;
    }

    public Vector3 GetWallForward()
    {
        return WallForward;
    }

    public string GetWallRunSide()
    {
        return activeWallSide.ToString();
    }

    private bool wallLeft, wallRight;
    private RaycastHit leftHit, rightHit;
    private float wallRunTimer;
    private float nextWallRunDebugLogTime;
    private float timeSinceGrounded;
    private float groundedStableTimer;
    private bool previousJumpHeld;
    private bool usedLeftWallRun;
    private bool usedRightWallRun;
    private bool pendingWallRunEndBoost;

    private enum WallSide
    {
        None,
        Left,
        Right
    }

    private WallSide activeWallSide = WallSide.None;

    [Header("State")]
    public MovementState currentState = MovementState.grounded;
    public enum MovementState{
        grounded, 
        air, 
        charging,
        wallrun, 
    }

    public enum LongJumpType{
        HighGround,
        LedgeLong,
        WallDirectional,
        WallStatic
    }

    private bool hasPendingLongJump;
    private LongJumpType pendingLongJumpType;
    private Vector3 pendingLongJumpDirection;
    private bool jumpHandledForCurrentPress;
    private float longJumpGroundCoyoteTimer;
    public bool ConsumeLongJumpRequest(out LongJumpType type, out Vector3 direction)
    {
        if (hasPendingLongJump)
        {
            type = pendingLongJumpType;
            direction = pendingLongJumpDirection;

            hasPendingLongJump = false;
            return true;
        }

        type = default;
        direction = Vector3.zero;
        return false;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        localInputHandler = GetComponent<PlayerInputHandler>();

        if (orientation == null)
            orientation = transform;
    }

    private PlayerInputHandler GetInputHandler()
    {
        return localInputHandler;
    }

    private void Update()
    {
        if (controller == null || GetInputHandler() == null)
            return;

        if (controller.isGrounded)
        {
            longJumpGroundCoyoteTimer = longJumpGroundCoyoteTime;
            timeSinceGrounded = 0f;
            groundedStableTimer += Time.deltaTime;
        }
        else
        {
            longJumpGroundCoyoteTimer -= Time.deltaTime;
            timeSinceGrounded += Time.deltaTime;
            groundedStableTimer = 0f;
        }
        
        HandleWallRunState();
        UpdateState();
        HandleCharging();
    }

    private void LateUpdate()
    {
        
    }

    #region State Machine
    private void UpdateState()
    {
        // Don't change state while in special states
        if (currentState == MovementState.charging || IsWallRunning)
        {
            return;
        }
        if (controller.isGrounded)
        {
            currentState = MovementState.grounded;
        }
        else
        {
            currentState = MovementState.air;
        }
    }
    #endregion

    #region Charging
    private void HandleCharging(){
        PlayerInputHandler input = GetInputHandler();
        if (controller == null || input == null)
            return;

        bool jumpHeld = input.JumpTriggered;
        if (!jumpHeld)
            jumpHandledForCurrentPress = false;

        // Decrement charge timer while charging
        if (isCharging){
            currentChargeTime -= Time.deltaTime;
            if (currentChargeTime <= 0f){
                currentChargeTime = 0f;
                ExitChargingState();
            }
        }

        // Start charging from dedicated charge input (C)
        if (!isCharging && input.ChargePressedThisFrame)
        {
            isCharging = true;
            currentChargeTime = maxChargeTime;
            currentState = MovementState.charging;
        }

        bool shouldProcessJump = jumpHeld && !jumpHandledForCurrentPress && isCharging;
        if (shouldProcessJump)
        {
            jumpHandledForCurrentPress = true;
            TryQueueLongJumpFromCharge();
        }
    }

    private void ExitChargingState()
    {
        isCharging = false;
        currentState = controller.isGrounded ? MovementState.grounded : MovementState.air;
    }

    private bool TryQueueLongJumpFromCharge()
    {
        if (!TryBuildLongJumpRequest(out LongJumpType requestType, out Vector3 requestDirection))
            return false;

        hasPendingLongJump = true;
        pendingLongJumpType = requestType;
        pendingLongJumpDirection = requestDirection;
        ExitChargingState();
        return true;
    }
    #endregion

    #region Long Jump

    private bool TryBuildLongJumpRequest(out LongJumpType jumpType, out Vector3 jumpDirection)
    {
        jumpType = LongJumpType.HighGround;
        jumpDirection = Vector3.zero;

        if (orientation == null)
            return false;

        PlayerInputHandler input = GetInputHandler();
        if (input == null)
            return false;

        Vector2 moveInput = input.MoveInput;
        bool hasForwardIntent = HasForwardLongJumpIntent(moveInput);

        if (IsTouchingWallForLongJump(out Vector3 wallContactNormal))
        {
            if (!CanWallLongJump(wallContactNormal))
                return false;

            Vector3 forward = orientation.forward;
            Vector3 right = orientation.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 inputDirection = (forward * moveInput.y + right * moveInput.x);
            inputDirection.y = 0f;
            bool directionalInputHeld = Mathf.Abs(moveInput.x) > wallDirectionalInputThreshold || Mathf.Abs(moveInput.y) > wallDirectionalInputThreshold;

            if (directionalInputHeld && inputDirection.sqrMagnitude > 0.0001f)
            {
                jumpType = LongJumpType.WallDirectional;
                jumpDirection = inputDirection.normalized;
            }
            else
            {
                jumpType = LongJumpType.WallStatic;
                jumpDirection = wallContactNormal;
            }

            return true;
        }

        bool groundedOrCoyote = controller.isGrounded || longJumpGroundCoyoteTimer > 0f;
        if (!groundedOrCoyote || !hasForwardIntent)
            return false;

        Vector3 groundForward = orientation.forward;
        groundForward.y = 0f;

        if (groundForward.sqrMagnitude < 0.0001f)
            groundForward = transform.forward;

        jumpDirection = groundForward.normalized;
        jumpType = IsNearLedgeGround() ? LongJumpType.LedgeLong : LongJumpType.HighGround;
        return true;
    }

    private bool HasForwardLongJumpIntent(Vector2 moveInput)
    {
        if (orientation == null || controller == null)
            return moveInput.y > longJumpForwardInputThreshold;

        bool forwardInput = moveInput.y > longJumpForwardInputThreshold;

        Vector3 forward = orientation.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f)
            forward.Normalize();

        Vector3 flatVelocity = controller.velocity;
        flatVelocity.y = 0f;

        bool movingForward = forward.sqrMagnitude > 0.0001f && Vector3.Dot(flatVelocity, forward) > 0.2f;
        return forwardInput || movingForward;
    }

    private bool IsNearLedgeGround()
    {
        if (orientation == null || controller == null)
            return false;

        Vector3 horizontalForward = orientation.forward;
        horizontalForward.y = 0f;
        if (horizontalForward.sqrMagnitude < 0.0001f)
            return false;

        horizontalForward.Normalize();

        Vector3 worldCenter = transform.TransformPoint(controller.center);
        float feetOffset = Mathf.Max(0f, (controller.height * 0.5f) - controller.radius);
        Vector3 feetPosition = worldCenter - Vector3.up * feetOffset;

        Vector3 checkOrigin = feetPosition
                            + Vector3.up * (ledgeCheckHeightOffset + 0.05f)
                            + horizontalForward * ledgeForwardCheckDistance;

        int mask = groundMask.value == 0 ? ~0 : groundMask.value;
        float probeRadius = Mathf.Max(0.05f, controller.radius * 0.35f);
        float downDistance = Mathf.Max(ledgeDownCheckDistance, controller.stepOffset + controller.radius + ledgeCheckHeightOffset + 0.6f);

        bool hasGroundAhead = Physics.SphereCast(
            checkOrigin,
            probeRadius,
            Vector3.down,
            out _,
            downDistance,
            mask,
            QueryTriggerInteraction.Ignore
        );

        Debug.DrawRay(checkOrigin, Vector3.down * downDistance, hasGroundAhead ? Color.green : Color.yellow);
        return !hasGroundAhead;
    }

    private bool CanWallLongJump(Vector3 wallNormal)
    {
        if (controller == null)
            return false;

        return controller.velocity.y >= -wallLongJumpMaxFallSpeed;
    }

    private bool IsTouchingWallForLongJump(out Vector3 hitNormal)
    {
        hitNormal = Vector3.zero;

        if (controller == null || orientation == null)
            return false;

        if (controller.isGrounded)
            return false;

        CheckWalls();

        bool hasWall = wallLeft || wallRight;
        if (!hasWall)
            return false;

        if (wallLeft && wallRight)
            hitNormal = leftHit.distance <= rightHit.distance ? leftHit.normal : rightHit.normal;
        else
            hitNormal = wallRight ? rightHit.normal : leftHit.normal;

        bool wallIsSteepEnough = Mathf.Abs(hitNormal.y) <= maxWallUpNormal;
        return wallIsSteepEnough;
    }

    public bool IsNearWallForDash()
    {
        if (controller == null || orientation == null)
            return false;

        CheckWalls();
        return wallLeft || wallRight;
    }
    #endregion


    #region Wallrun 
    private bool EvaluateWallRunEligibility(out Vector3 hitNormal, out Vector3 wallForward, out WallSide wallSide, bool requireJumpPressedThisFrame)
    {
        hitNormal = Vector3.zero;
        wallForward = Vector3.zero;
        wallSide = WallSide.None;

        PlayerInputHandler input = GetInputHandler();
        if (controller == null || orientation == null || input == null)
            return false;

        CheckWalls();

        bool grounded = controller.isGrounded;
        Vector2 move = input.MoveInput;
        bool jumpHeld = input.JumpTriggered;
        bool jumpPressedThisFrame = input.JumpPressedThisFrame;
        bool wantsWallRun = requireJumpPressedThisFrame ? jumpPressedThisFrame : jumpHeld;

        bool hasWall = wallLeft || wallRight;
        if (!hasWall)
            return false;

        bool forwardOrBackwardHeld = Mathf.Abs(move.y) > forwardInputThreshold;

        if (!TryGetBestWallContact(out hitNormal, out wallSide))
            return false;

        if (!CanUseWallSide(wallSide))
            return false;

        bool wallIsSteepEnough = Mathf.Abs(hitNormal.y) <= maxWallUpNormal;

        wallForward = Vector3.Cross(hitNormal, Vector3.up);
        wallForward.y = 0f;
        if (wallForward.sqrMagnitude < 0.0001f)
            return false;
        wallForward.Normalize();

        Vector3 desiredTravelDirection = move.y >= 0f ? orientation.forward : -orientation.forward;
        desiredTravelDirection.y = 0f;
        if (desiredTravelDirection.sqrMagnitude > 0.0001f)
            desiredTravelDirection.Normalize();

        if (Vector3.Dot(wallForward, desiredTravelDirection) < 0f)
            wallForward = -wallForward;

        bool passedAirborneBuffer = timeSinceGrounded >= airborneStartBuffer;
        bool canStartFromAir = !requireJumpPressedThisFrame || passedAirborneBuffer;

        return !grounded && canStartFromAir && jumpHeld && wantsWallRun && forwardOrBackwardHeld && wallIsSteepEnough;
    }

    public bool ShouldConsumeJumpForWallRun()
    {
        if (controller == null || controller.isGrounded || timeSinceGrounded < airborneStartBuffer)
            return false;

        return EvaluateWallRunEligibility(out _, out _, out _, true);
    }

    public bool IsTouchingWallForAirJumpBlock()
    {
        return IsTouchingWallForLongJump(out _);
    }

    private void CheckWalls()
    {
    Vector3 origin = transform.position + Vector3.up * wallCheckHeight;
    int mask = wallMask.value == 0 ? ~0 : wallMask.value;

    wallLeft  = Physics.Raycast(origin, -orientation.right, out leftHit,  wallCheckDistance, mask, QueryTriggerInteraction.Ignore);
    wallRight = Physics.Raycast(origin,  orientation.right, out rightHit, wallCheckDistance, mask, QueryTriggerInteraction.Ignore);

    // Optional: debug rays
    Debug.DrawRay(origin, -orientation.right * wallCheckDistance, wallLeft ? Color.green : Color.red);
    Debug.DrawRay(origin,  orientation.right * wallCheckDistance, wallRight ? Color.green : Color.red);
    }

    private void HandleWallRunState()
    {
        PlayerInputHandler input = GetInputHandler();
        bool jumpHeld = input != null && input.JumpTriggered;
        bool jumpReleasedThisFrame = previousJumpHeld && !jumpHeld;
        previousJumpHeld = jumpHeld;

        bool grounded = controller.isGrounded;
        if (grounded)
        {
            EndWallRun(true, true);

            if (groundedStableTimer >= groundedStableResetTime)
            {
                usedLeftWallRun = false;
                usedRightWallRun = false;
            }

            return;
        }

         // Hard blocks
        if (currentState == MovementState.charging)
        {
            EndWallRun(grounded, true);
            return;
        }

        if (IsWallRunning && jumpReleasedThisFrame)
        {
            EndWallRun(grounded, true);
            return;
        }

        if (IsWallRunning && !jumpHeld)
        {
            EndWallRun(grounded, true);
            return;
        }

        if (IsWallRunning)
        {
            wallRunTimer -= Time.deltaTime;

            if (logWallRunDebug && Time.time >= nextWallRunDebugLogTime)
            {
                Debug.Log($"[AdvancedMovement] Wallrunning | Side: {activeWallSide} | TimeLeft: {Mathf.Max(0f, wallRunTimer):0.00}s");
                nextWallRunDebugLogTime = Time.time + 0.25f;
            }

            if (wallRunTimer <= 0f)
            {
                EndWallRun(grounded, true);
                return;
            }
        }

        bool eligible = EvaluateWallRunEligibility(out Vector3 hitNormal, out Vector3 wallForward, out WallSide wallSide, !IsWallRunning);

        if (!eligible)
        {
            EndWallRun(grounded, true);
            return;
        }

        bool startingNewWallRun = !IsWallRunning || activeWallSide != wallSide;
        if (startingNewWallRun)
        {
            MarkWallSideUsed(wallSide);
            wallRunTimer = wallRunMaxDuration;

            if (logWallRunDebug)
                Debug.Log($"[AdvancedMovement] Wallrun started | Side: {wallSide} | Duration: {wallRunMaxDuration:0.00}s");
        }

        WallNormal = hitNormal;
        WallForward = wallForward;
        IsWallRunning = true;
        activeWallSide = wallSide;

        currentState = MovementState.wallrun;
    }

    public bool TryConsumeWallRunEndBoost(out float boostAmount)
    {
        if (pendingWallRunEndBoost)
        {
            pendingWallRunEndBoost = false;
            boostAmount = wallRunEndVerticalBoost;
            return true;
        }

        boostAmount = 0f;
        return false;
    }

    private bool TryGetBestWallContact(out Vector3 hitNormal, out WallSide wallSide)
    {
        hitNormal = Vector3.zero;
        wallSide = WallSide.None;

        if (wallLeft && wallRight)
        {
            if (!usedLeftWallRun && usedRightWallRun)
            {
                hitNormal = leftHit.normal;
                wallSide = WallSide.Left;
                return true;
            }

            if (usedLeftWallRun && !usedRightWallRun)
            {
                hitNormal = rightHit.normal;
                wallSide = WallSide.Right;
                return true;
            }

            bool leftIsCloser = leftHit.distance <= rightHit.distance;
            hitNormal = leftIsCloser ? leftHit.normal : rightHit.normal;
            wallSide = leftIsCloser ? WallSide.Left : WallSide.Right;
            return true;
        }

        if (wallLeft)
        {
            hitNormal = leftHit.normal;
            wallSide = WallSide.Left;
            return true;
        }

        if (wallRight)
        {
            hitNormal = rightHit.normal;
            wallSide = WallSide.Right;
            return true;
        }

        return false;
    }

    private bool CanUseWallSide(WallSide wallSide)
    {
        if (IsWallRunning && activeWallSide == wallSide)
            return true;

        switch (wallSide)
        {
            case WallSide.Left:
                return !usedLeftWallRun;
            case WallSide.Right:
                return !usedRightWallRun;
            default:
                return false;
        }
    }

    private void MarkWallSideUsed(WallSide wallSide)
    {
        switch (wallSide)
        {
            case WallSide.Left:
                usedLeftWallRun = true;
                break;
            case WallSide.Right:
                usedRightWallRun = true;
                break;
        }
    }

    private void EndWallRun(bool grounded, bool grantEndBoost)
    {
        bool wasWallRunning = IsWallRunning;
        WallSide endedSide = activeWallSide;

        IsWallRunning = false;
        activeWallSide = WallSide.None;

        if (currentState == MovementState.wallrun)
            currentState = grounded ? MovementState.grounded : MovementState.air;

        if (grantEndBoost && wasWallRunning)
            pendingWallRunEndBoost = true;

        if (logWallRunDebug && wasWallRunning)
            Debug.Log($"[AdvancedMovement] Wallrun ended | Side: {endedSide} | Grounded: {grounded}");
    }
    #endregion

}