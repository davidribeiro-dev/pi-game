using UnityEngine;

public class ODMGear : MonoBehaviour
{
    [Header("References")]
    public Transform aimOrigin;
    public Transform leftHookOrigin;
    public Transform rightHookOrigin;

    [Header("Gear State")]
    public bool isActivated = true;

    [Header("Attachment")]
    public LayerMask attachableMask = ~0;
    public float maxAttachDistance = 45f;
    public float minAttachDistance = 1f;

    [Header("Hook Physics")]
    public float pullAcceleration = 26f;
    public float maxPullSpeed = 20f;
    public float ropeConstraintStrength = 24f;
    public float swingControlAcceleration = 8f;
    public float maxHookSpeed = 30f;

    [Header("Hook Release Boost")]
    public float hookReleaseBoost = 7f;
    public float backwardReleaseBoostMultiplier = 0f;

    [Header("Grapple Boost")]
    public float grappleBoostSpeed = 15f;
    public float grappleBoostDoubleTapWindow = 0.3f;
    public float grappleBoostCooldown = 0.25f;
    public float grappleBoostFuelCost = 20f;
    public float grappleBoostChargeBuffer = 0.5f;

    [Header("Fuel")]
    public float maxFuel = 100f;
    public float hookFuelDrainPerSecond = 10f;
    public float fuelRechargePerSecond = 18f;
    public float fuelRechargeGroundSpeedThreshold = 0.2f;

    [Header("Debug")]
    public bool logAttachEvents = false;
    public bool logFuelEvents = true;
    public bool logGrappleBoostEvents = true;
    public bool drawGizmos = true;

    [Header("Visuals")]
    public LineRenderer leftHookLine;
    public LineRenderer rightHookLine;

    public float CurrentFuel => currentFuel;
    public float FuelNormalized => maxFuel > 0f ? Mathf.Clamp01(currentFuel / maxFuel) : 0f;

    private struct HookState
    {
        public bool active;
        public Vector3 anchorPoint;
        public float ropeLength;
    }

    private HookState leftHook;
    private HookState rightHook;
    private CharacterController controller;
    private PlayerInputHandler localInputHandler;

    private float currentFuel;
    private float pendingForwardBoost;
    private bool pendingBoostOverrideVelocity;
    private float grappleBoostCooldownTimer;
    private float lastJumpPressTime = -999f;
    private float chargePrimedTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        localInputHandler = GetComponent<PlayerInputHandler>();
        currentFuel = Mathf.Max(0f, maxFuel);
    }

    private PlayerInputHandler GetInputHandler()
    {
        return localInputHandler;
    }

    void Update()
    {
        PlayerInputHandler input = GetInputHandler();

        if (grappleBoostCooldownTimer > 0f)
            grappleBoostCooldownTimer -= Time.deltaTime;

        if (chargePrimedTimer > 0f)
            chargePrimedTimer -= Time.deltaTime;

        UpdateFuel(Time.deltaTime);

        if (input == null)
        {
            UpdateHookVisuals();
            return;
        }

        if (!isActivated)
        {
            ForceDetachAll();
            UpdateHookVisuals();
            return;
        }

        HandleGrappleBoostInput(input);
        HandleAttachInputs(input);
        HandleDetachInputs(input);
        UpdateHookVisuals();
    }

    public void SetActivated(bool value)
    {
        isActivated = value;

        if (!isActivated)
            ForceDetachAll();
    }

    public bool HasActiveHook()
    {
        return leftHook.active || rightHook.active;
    }

    public void ForceDetachAll()
    {
        leftHook.active = false;
        rightHook.active = false;
        UpdateHookVisuals();
    }

    public void ApplyForces(ref Vector3 horizontalVelocity, ref float verticalVelocity, Transform orientation, Vector2 moveInput, float deltaTime)
    {
        if (!isActivated || deltaTime <= 0f)
            return;

        ApplyPendingBoost(ref horizontalVelocity, ref verticalVelocity, orientation);

        if (!leftHook.active && !rightHook.active)
            return;

        Vector3 playerPosition = transform.position;
        Vector3 totalVelocity = horizontalVelocity + Vector3.up * verticalVelocity;

        if (leftHook.active)
            ApplySingleHookPhysics(leftHook, playerPosition, ref totalVelocity, deltaTime);

        if (rightHook.active)
            ApplySingleHookPhysics(rightHook, playerPosition, ref totalVelocity, deltaTime);

        if (orientation != null && moveInput.sqrMagnitude > 0.0001f)
        {
            Vector3 forward = orientation.forward;
            Vector3 right = orientation.right;
            forward.y = 0f;
            right.y = 0f;

            if (forward.sqrMagnitude > 0.0001f)
                forward.Normalize();

            if (right.sqrMagnitude > 0.0001f)
                right.Normalize();

            Vector3 steerDirection = (forward * moveInput.y + right * moveInput.x).normalized;
            totalVelocity += steerDirection * swingControlAcceleration * deltaTime;
        }

        Vector3 horizontal = new Vector3(totalVelocity.x, 0f, totalVelocity.z);
        float horizontalSpeed = horizontal.magnitude;
        if (horizontalSpeed > maxHookSpeed)
            horizontal = horizontal.normalized * maxHookSpeed;

        horizontalVelocity = horizontal;
        verticalVelocity = totalVelocity.y;
    }

    private void HandleAttachInputs(PlayerInputHandler input)
    {
        if (currentFuel <= 0f)
            return;

        bool leftPressed = input.LeftHookPressedThisFrame;
        bool rightPressed = input.RightHookPressedThisFrame;

        if (leftPressed && rightPressed)
        {
            TryAttachSharedAnchor();
            return;
        }

        if (leftPressed && !leftHook.active)
            TryAttachSingle(ref leftHook, "Left");

        if (rightPressed && !rightHook.active)
            TryAttachSingle(ref rightHook, "Right");
    }

    private void HandleDetachInputs(PlayerInputHandler input)
    {
        int releasedHookCount = 0;

        bool leftReleasedThisFrame = input.LeftHookReleasedThisFrame;
        if (leftHook.active && (!input.LeftHookTriggered || leftReleasedThisFrame))
        {
            if (leftReleasedThisFrame)
                releasedHookCount++;

            leftHook.active = false;
        }

        bool rightReleasedThisFrame = input.RightHookReleasedThisFrame;
        if (rightHook.active && (!input.RightHookTriggered || rightReleasedThisFrame))
        {
            if (rightReleasedThisFrame)
                releasedHookCount++;

            rightHook.active = false;
        }

        if (releasedHookCount > 0)
        {
            float backwardMultiplier = input.MoveInput.y < -0.1f
                ? Mathf.Clamp01(backwardReleaseBoostMultiplier)
                : 1f;

            QueueForwardBoost(hookReleaseBoost * releasedHookCount * backwardMultiplier);
        }
    }

    private void TryAttachSharedAnchor()
    {
        if (currentFuel <= 0f)
            return;

        if (!TryRaycastAttachPoint(out Vector3 anchorPoint, out float distance))
            return;

        leftHook.active = true;
        leftHook.anchorPoint = anchorPoint;
        leftHook.ropeLength = distance;

        rightHook.active = true;
        rightHook.anchorPoint = anchorPoint;
        rightHook.ropeLength = distance;

        if (logAttachEvents)
            Debug.Log($"ODMGear: Shared attach at {anchorPoint}");
    }

    private void TryAttachSingle(ref HookState hook, string hookName)
    {
        if (currentFuel <= 0f)
            return;

        if (!TryRaycastAttachPoint(out Vector3 anchorPoint, out float distance))
            return;

        hook.active = true;
        hook.anchorPoint = anchorPoint;
        hook.ropeLength = distance;

        if (logAttachEvents)
            Debug.Log($"ODMGear: {hookName} hook attached at {anchorPoint}");
    }

    private bool TryRaycastAttachPoint(out Vector3 anchorPoint, out float distance)
    {
        anchorPoint = Vector3.zero;
        distance = 0f;

        if (!TryGetAimRay(out Ray aimRay))
            return false;

        if (!Physics.Raycast(aimRay, out RaycastHit hit, maxAttachDistance, attachableMask, QueryTriggerInteraction.Ignore))
            return false;

        if (hit.distance < minAttachDistance)
            return false;

        anchorPoint = hit.point;
        distance = Vector3.Distance(transform.position, anchorPoint);
        return true;
    }

    private bool TryGetAimRay(out Ray ray)
    {
        Transform rayTransform = aimOrigin;

        if (rayTransform == null && Camera.main != null)
            rayTransform = Camera.main.transform;

        if (rayTransform == null)
        {
            ray = default;
            return false;
        }

        ray = new Ray(rayTransform.position, rayTransform.forward);
        return true;
    }

    private void ApplySingleHookPhysics(HookState hook, Vector3 playerPosition, ref Vector3 totalVelocity, float deltaTime)
    {
        Vector3 toAnchor = hook.anchorPoint - playerPosition;
        float currentDistance = toAnchor.magnitude;
        if (currentDistance <= 0.0001f)
            return;

        Vector3 pullDirection = toAnchor / currentDistance;

        totalVelocity += pullDirection * pullAcceleration * deltaTime;

        float towardAnchorSpeed = Vector3.Dot(totalVelocity, pullDirection);
        if (towardAnchorSpeed > maxPullSpeed)
            totalVelocity += pullDirection * (maxPullSpeed - towardAnchorSpeed);

        bool ropeIsTaut = currentDistance > hook.ropeLength;
        if (!ropeIsTaut)
            return;

        float awayFromAnchorSpeed = Vector3.Dot(totalVelocity, -pullDirection);
        if (awayFromAnchorSpeed > 0f)
            totalVelocity += pullDirection * awayFromAnchorSpeed;

        float stretch = currentDistance - hook.ropeLength;
        totalVelocity += pullDirection * (stretch * ropeConstraintStrength * deltaTime);
    }

    private void UpdateHookVisuals()
    {
        UpdateSingleHookVisual(leftHookLine, leftHook, leftHookOrigin);
        UpdateSingleHookVisual(rightHookLine, rightHook, rightHookOrigin);
    }

    private void UpdateSingleHookVisual(LineRenderer line, HookState hook, Transform hookOrigin)
    {
        if (line == null)
            return;

        if (!hook.active)
        {
            line.enabled = false;
            return;
        }

        Vector3 origin = hookOrigin != null ? hookOrigin.position : transform.position;

        line.enabled = true;
        line.positionCount = 2;
        line.SetPosition(0, origin);
        line.SetPosition(1, hook.anchorPoint);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        if (leftHook.active)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(leftHook.anchorPoint, 0.2f);
        }

        if (rightHook.active)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(rightHook.anchorPoint, 0.2f);
        }
    }

    private void HandleGrappleBoostInput(PlayerInputHandler input)
    {
        if (input.ChargePressedThisFrame)
            chargePrimedTimer = Mathf.Max(chargePrimedTimer, grappleBoostChargeBuffer);

        if (!input.JumpPressedThisFrame)
            return;

        bool isDoubleTap = Time.time - lastJumpPressTime <= grappleBoostDoubleTapWindow;
        lastJumpPressTime = Time.time;

        if (!isDoubleTap)
            return;

        bool chargeReady = input.ChargeTriggered || chargePrimedTimer > 0f;
        if (!chargeReady)
            return;

        if (grappleBoostCooldownTimer > 0f)
            return;

        if (!TryConsumeFuel(grappleBoostFuelCost))
            return;

        QueueForwardBoost(grappleBoostSpeed, true);
        grappleBoostCooldownTimer = grappleBoostCooldown;
        chargePrimedTimer = 0f;

        if (logGrappleBoostEvents)
            Debug.Log($"ODMGear: Grapple boost triggered. Fuel: {currentFuel:F1}/{maxFuel:F1}");
    }

    private void ApplyPendingBoost(ref Vector3 horizontalVelocity, ref float verticalVelocity, Transform orientation)
    {
        if (pendingForwardBoost <= 0f)
            return;

        Vector3 boostDirection = ResolveHeadingDirection(orientation, horizontalVelocity);
        Vector3 boostVelocity = boostDirection * pendingForwardBoost;

        if (pendingBoostOverrideVelocity)
        {
            horizontalVelocity = new Vector3(boostVelocity.x, 0f, boostVelocity.z);
            verticalVelocity = boostVelocity.y;
        }
        else
        {
            horizontalVelocity += new Vector3(boostVelocity.x, 0f, boostVelocity.z);
            verticalVelocity += boostVelocity.y;
        }

        pendingForwardBoost = 0f;
        pendingBoostOverrideVelocity = false;
    }

    private Vector3 ResolveHeadingDirection(Transform orientation, Vector3 currentHorizontalVelocity)
    {
        Vector3 direction;

        if (aimOrigin != null)
        {
            direction = aimOrigin.forward;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        if (Camera.main != null)
        {
            direction = Camera.main.transform.forward;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        if (orientation != null)
        {
            direction = orientation.forward;
            if (direction.sqrMagnitude > 0.0001f)
                return direction.normalized;
        }

        direction = transform.forward;
        if (direction.sqrMagnitude > 0.0001f)
            return direction.normalized;

        return Vector3.forward;
    }

    private void QueueForwardBoost(float amount, bool overrideVelocity = false)
    {
        if (amount <= 0f)
            return;

        pendingForwardBoost += amount;
        pendingBoostOverrideVelocity |= overrideVelocity;
    }

    private void UpdateFuel(float deltaTime)
    {
        if (!isActivated || deltaTime <= 0f)
            return;

        if (HasActiveHook())
        {
            currentFuel = Mathf.Max(0f, currentFuel - hookFuelDrainPerSecond * deltaTime);
            if (currentFuel <= 0f)
            {
                if (logFuelEvents)
                    Debug.Log("ODMGear: Fuel depleted. Detaching hooks.");

                ForceDetachAll();
            }

            return;
        }

        if (controller == null || !controller.isGrounded)
            return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        if (horizontalVelocity.magnitude > fuelRechargeGroundSpeedThreshold)
            return;

        currentFuel = Mathf.MoveTowards(currentFuel, Mathf.Max(0f, maxFuel), fuelRechargePerSecond * deltaTime);
    }

    private bool TryConsumeFuel(float amount)
    {
        if (amount <= 0f)
            return true;

        if (currentFuel < amount)
            return false;

        currentFuel -= amount;
        return true;
    }
}
