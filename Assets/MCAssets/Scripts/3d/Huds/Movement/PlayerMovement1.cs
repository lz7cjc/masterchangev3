using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// PlayerMovement1 - Simple collision detection with CharacterController
/// UPDATED: Converted from Rigidbody to CharacterController for better VR teleportation
/// UPDATED: Added support for SpeedController and StartStopController (Level 3b HUD controls)
/// </summary>
public class PlayerMovement1 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float defaultSpeed = 5f;
    public float speedIncrement = 2f;
    public float maxSpeed = 20f;
    public float minSpeed = 0f;

    [Header("Timing")]
    public float actionDelay = 1f;
    public float stopDelay = 0.5f;

    [Header("References")]
    public CharacterController playerController; // CHANGED: From Rigidbody to CharacterController

    [Header("Simple Collision Detection")]
    [Tooltip("How far ahead to check for any obstacles")]
    public float obstacleCheckDistance = 1.5f;
    [Tooltip("How far to raycast down to find ground")]
    public float groundCheckDistance = 10f;
    [Tooltip("Ignore these layers for collision (typically UI layers)")]
    public LayerMask ignoreCollisionLayers = 0;
    [Tooltip("How smooth the terrain following is")]
    public float terrainFollowSmoothness = 10f;
    [Tooltip("Maximum slope angle player can walk on")]
    public float maxSlopeAngle = 45f;
    [Tooltip("Use simple ground detection (works with any mesh)")]
    public bool useSimpleGroundDetection = true;

    [Header("Gravity Settings")]
    [Tooltip("Gravity applied to character controller")]
    public float gravity = -9.81f;
    private float verticalVelocity = 0f;

    [Header("Safety Settings")]
    [Tooltip("Prevent movement on start")]
    public bool safeStart = true;
    [Tooltip("Reset saved speed on start")]
    public bool resetSavedSpeedOnStart = false;
    [Tooltip("Enable debug logging")]
    public bool debugMode = true;

    [Header("UI Elements")]
    public TMP_Text[] speedDisplays;
    public GameObject[] walkStopIcons;
    public GameObject[] walkStartIcons;

    [Header("Enhanced Level 3 Movement Controls")]
    public GameObject startImage;
    public GameObject stopImage;
    public GameObject speedUpImage;
    public GameObject speedDownImage;

    [Header("ToggleActiveIcons Controllers")]
    public ToggleActiveIcons startStopIconController;
    public ToggleActiveIcons speedUpIconController;
    public ToggleActiveIcons speedDownIconController;

    [Header("HUD Integration")]
    public HUDSystemCoordinator hudCoordinator;

    // Private variables
    private float currentSpeed = 0f;
    private float hoverTimer = 0f;
    private bool isHovering = false;
    private bool isMoving = false;
    private bool isInitialized = false;
    private bool movementEnabled = true; // NEW: For SpeedController/StartStopController

    // Action types
    private enum ActionType { None, StartWalk, ChangeSpeed, Stop }
    private ActionType pendingAction = ActionType.None;
    private float speedDelta = 0f;

    // Cached components
    private hudCountdown hudCountdown;

    void Start()
    {
        DebugLog("=== PlayerMovement1 Start() called ===");

        // Cache components
        hudCountdown = FindFirstObjectByType<hudCountdown>();

        if (hudCoordinator == null)
        {
            hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();
        }

        // Validate setup
        ValidateSetup();

        // Initialize movement state
        InitializeMovementState();

        // Update UI
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();

        // Setup Level 3 event triggers
        SetupLevel3EventTriggers();

        isInitialized = true;
        DebugLog($"=== PlayerMovement1 initialization complete - Speed: {currentSpeed}, Moving: {isMoving} ===");
    }

    private void ValidateSetup()
    {
        if (playerController == null)
        {
            Debug.LogError("PlayerMovement1: Player CharacterController is not assigned!");
        }

        DebugLog($"Obstacle Check Distance: {obstacleCheckDistance}, Ground Check Distance: {groundCheckDistance}");
        DebugLog($"Ignore Collision Layers: {ignoreCollisionLayers}");
    }

    private void InitializeMovementState()
    {
        if (safeStart)
        {
            currentSpeed = 0f;
            isMoving = false;
            DebugLog("Safe start enabled - player starts stationary");
        }
        else if (!resetSavedSpeedOnStart && PlayerPrefs.HasKey("walkspeed"))
        {
            currentSpeed = PlayerPrefs.GetFloat("walkspeed");
            isMoving = currentSpeed > 0f;
            DebugLog($"Loaded saved speed: {currentSpeed}");
        }
        else
        {
            currentSpeed = 0f;
            isMoving = false;
            DebugLog("Starting with default state (stationary)");
        }
    }

    void FixedUpdate()
    {
        if (!isInitialized) return;

        HandleHoverTimer();
        HandleMovement();
        ApplyGravity(); // NEW: Apply gravity for CharacterController
    }

    private void ApplyGravity()
    {
        // CharacterController needs manual gravity
        if (playerController == null) return;

        if (playerController.isGrounded)
        {
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Apply vertical movement
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0) * Time.deltaTime;
        playerController.Move(verticalMovement);
    }

    private void HandleMovement()
    {
        // NEW: Check movementEnabled flag (for StartStopController)
        if (!movementEnabled || !isMoving || currentSpeed <= 0f || playerController == null) 
            return;

        // Get movement direction
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 horizontalDirection = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        Vector3 currentPosition = playerController.transform.position;
        Vector3 moveVector = horizontalDirection * currentSpeed * Time.deltaTime;
        Vector3 targetPosition = currentPosition + moveVector;

        // Check for obstacles ahead
        if (CheckForObstacles(currentPosition, horizontalDirection))
        {
            DebugLog("Obstacle detected - stopping movement");
            return; // Don't move if obstacle detected
        }

        // Apply ground following
        Vector3 finalPosition = ApplyGroundFollowing(targetPosition);

        // Use CharacterController.Move
        Vector3 movement = finalPosition - playerController.transform.position;
        playerController.Move(movement);

        if (debugMode && Time.frameCount % 60 == 0)
        {
            DebugLog($"Moving - Speed: {currentSpeed}, Position: {finalPosition}");
        }
    }

    private bool CheckForObstacles(Vector3 fromPosition, Vector3 direction)
    {
        // Multiple height checks to catch different obstacles
        float playerHeight = 2f;
        Vector3[] checkPoints = {
            fromPosition + Vector3.up * 0.2f,  // Foot level
            fromPosition + Vector3.up * 1f,    // Center level  
            fromPosition + Vector3.up * 1.8f   // Head level
        };

        foreach (Vector3 checkPoint in checkPoints)
        {
            RaycastHit hit;
            if (Physics.Raycast(checkPoint, direction, out hit, obstacleCheckDistance))
            {
                // Check if we should ignore this collision
                int hitLayer = hit.collider.gameObject.layer;
                if (IsLayerInMask(hitLayer, ignoreCollisionLayers))
                {
                    continue;
                }

                // Check if it's the player itself
                if (hit.collider.transform == playerController.transform)
                {
                    continue;
                }

                // Valid obstacle found
                if (debugMode && Time.frameCount % 30 == 0)
                {
                    DebugLog($"Obstacle hit: {hit.collider.name} at distance {hit.distance:F2}");
                }
                return true;
            }
        }

        return false;
    }

    private Vector3 ApplyGroundFollowing(Vector3 targetPosition)
    {
        if (!useSimpleGroundDetection)
        {
            return targetPosition;
        }

        // Raycast down from above target position to find ground
        Vector3 rayStart = targetPosition + Vector3.up * groundCheckDistance;
        RaycastHit hit;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, groundCheckDistance * 2f))
        {
            // Check if we should ignore this ground
            int hitLayer = hit.collider.gameObject.layer;
            if (IsLayerInMask(hitLayer, ignoreCollisionLayers))
            {
                return targetPosition;
            }

            // Check slope angle
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                DebugLog($"Slope too steep: {slopeAngle:F1}° (max: {maxSlopeAngle}°)");
                return targetPosition;
            }

            // Smoothly adjust Y position
            float targetY = hit.point.y;
            float smoothedY = Mathf.Lerp(targetPosition.y, targetY, Time.deltaTime * terrainFollowSmoothness);

            return new Vector3(targetPosition.x, smoothedY, targetPosition.z);
        }

        return targetPosition;
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void HandleHoverTimer()
    {
        if (!isHovering) return;

        hoverTimer += Time.deltaTime;

        if (hudCountdown != null)
        {
            hudCountdown.SetCountdown((int)actionDelay, hoverTimer);
        }

        if (hoverTimer >= actionDelay)
        {
            ExecutePendingAction();
            ResetHover();
        }
    }

    private void ExecutePendingAction()
    {
        switch (pendingAction)
        {
            case ActionType.StartWalk:
                if (!isMoving)
                {
                    StartWalking();
                }
                break;

            case ActionType.Stop:
                if (isMoving)
                {
                    StopWalking();
                }
                break;

            case ActionType.ChangeSpeed:
                if (isMoving)
                {
                    ChangeSpeed(speedDelta);
                }
                break;
        }
    }

    private void StartWalking()
    {
        currentSpeed = defaultSpeed;
        isMoving = true;
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();
        SaveSpeed();
        DebugLog($"Started walking at speed {currentSpeed}");
    }

    private void ChangeSpeed(float delta)
    {
        float oldSpeed = currentSpeed;
        currentSpeed = Mathf.Clamp(currentSpeed + delta, minSpeed, maxSpeed);

        if (Mathf.Abs(currentSpeed - oldSpeed) < 0.01f)
        {
            return;
        }

        UpdateSpeedDisplay();
        SaveSpeed();
        DebugLog($"Changed speed from {oldSpeed} to {currentSpeed}");
    }

    private void StopWalking()
    {
        currentSpeed = 0f;
        isMoving = false;
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();
        SaveSpeed();
        DebugLog("Stopped walking");
    }

    private void UpdateSpeedDisplay()
    {
        string speedText = currentSpeed.ToString("F0");
        foreach (var display in speedDisplays)
        {
            if (display != null)
                display.text = speedText;
        }
    }

    private void UpdateIcons()
    {
        SetIconsActive(walkStartIcons, !isMoving);
        SetIconsActive(walkStopIcons, isMoving);
    }

    private void UpdateLevel3Controls()
    {
        if (!IsMovementMenuActive()) return;

        if (startImage != null && stopImage != null)
        {
            if (isMoving)
            {
                startImage.SetActive(false);
                stopImage.SetActive(true);
            }
            else
            {
                startImage.SetActive(true);
                stopImage.SetActive(false);
            }
        }

        if (!isHovering)
        {
            startStopIconController?.DefaultIcon();
            speedUpIconController?.DefaultIcon();
            speedDownIconController?.DefaultIcon();
        }
    }

    private bool IsMovementMenuActive()
    {
        if (hudCoordinator != null)
        {
            return hudCoordinator.IsMovementMenuActive();
        }

        return (startImage != null && startImage.activeInHierarchy) ||
               (stopImage != null && stopImage.activeInHierarchy);
    }

    private void SetupLevel3EventTriggers()
    {
        SetupImageEventTrigger(startImage, OnLevel3StartHover, OnLevel3HoverExit);
        SetupImageEventTrigger(stopImage, OnLevel3StopHover, OnLevel3HoverExit);
        SetupImageEventTrigger(speedUpImage, OnLevel3SpeedUpHover, OnLevel3HoverExit);
        SetupImageEventTrigger(speedDownImage, OnLevel3SpeedDownHover, OnLevel3HoverExit);
    }

    private void SetupImageEventTrigger(GameObject imageObject, System.Action onHover, System.Action onExit)
    {
        if (imageObject == null) return;

        EventTrigger trigger = imageObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imageObject.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => onHover());
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => onExit());
        trigger.triggers.Add(exitEntry);
    }

    private void SetIconsActive(GameObject[] icons, bool active)
    {
        foreach (var icon in icons)
        {
            if (icon != null)
                icon.SetActive(active);
        }
    }

    private void ResetHover()
    {
        isHovering = false;
        hoverTimer = 0f;
        pendingAction = ActionType.None;
        speedDelta = 0f;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }

        UpdateLevel3Controls();
    }

    private void SaveSpeed()
    {
        PlayerPrefs.SetFloat("walkspeed", currentSpeed);
    }

    private void DebugLog(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[PlayerMovement1] {message}");
        }
    }

    #region Public Interface Methods

    public void OnMouseEnterStartWalk()
    {
        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.StartWalk;
            hoverTimer = 0f;
            startStopIconController?.HoverIcon();
        }
    }

    public void OnMouseEnterSpeedUp()
    {
        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = speedIncrement;
            hoverTimer = 0f;
            speedUpIconController?.HoverIcon();
        }
    }

    public void OnMouseEnterSlowDown()
    {
        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = -speedIncrement;
            hoverTimer = 0f;
            speedDownIconController?.HoverIcon();
        }
    }

    public void OnMouseEnterStop()
    {
        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.Stop;
            hoverTimer = 0f;
            startStopIconController?.HoverIcon();
        }
    }

    public void OnMouseExit()
    {
        ResetHover();
    }

    public void OnMovementMenuOpened()
    {
        UpdateLevel3Controls();
    }

    public void OnMovementMenuClosed()
    {
        OnMouseExit();
    }

    public void ForceStop()
    {
        StopWalking();
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();
        SaveSpeed();
    }

    public void ForceStart(float speed = -1f)
    {
        if (speed > 0f)
        {
            currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        }
        else
        {
            currentSpeed = defaultSpeed;
        }

        isMoving = true;
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();
        SaveSpeed();
    }

    #endregion

    #region Level 3 Event Handlers

    private void OnLevel3StartHover()
    {
        hudCoordinator?.OnMainHUDHoverEnded();
        startStopIconController?.HoverIcon();
        OnMouseEnterStartWalk();
    }

    private void OnLevel3StopHover()
    {
        hudCoordinator?.OnMainHUDHoverEnded();
        startStopIconController?.HoverIcon();
        OnMouseEnterStop();
    }

    private void OnLevel3SpeedUpHover()
    {
        hudCoordinator?.OnMainHUDHoverEnded();
        speedUpIconController?.HoverIcon();
        OnMouseEnterSpeedUp();
    }

    private void OnLevel3SpeedDownHover()
    {
        hudCoordinator?.OnMainHUDHoverEnded();
        speedDownIconController?.HoverIcon();
        OnMouseEnterSlowDown();
    }

    private void OnLevel3HoverExit()
    {
        OnMouseExit();
    }

    #endregion

    #region NEW: Methods Required by SpeedController and StartStopController

    /// <summary>
    /// Set movement speed - called by SpeedController
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        UpdateSpeedDisplay();
        SaveSpeed();
        DebugLog($"[SpeedController] Movement speed set to: {currentSpeed}");
    }

    /// <summary>
    /// Enable player movement - called by StartStopController
    /// </summary>
    public void EnableMovement()
    {
        movementEnabled = true;
        DebugLog("[StartStopController] Movement ENABLED");
    }

    /// <summary>
    /// Disable player movement - called by StartStopController
    /// </summary>
    public void DisableMovement()
    {
        movementEnabled = false;
        DebugLog("[StartStopController] Movement DISABLED");
    }

    /// <summary>
    /// Check if movement is currently enabled
    /// </summary>
    public bool IsMovementEnabled => movementEnabled;

    #endregion

    #region Public Properties

    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    public bool IsHovering => isHovering;
    public bool CanIncreaseSpeed => currentSpeed < maxSpeed;
    public bool CanDecreaseSpeed => currentSpeed > minSpeed || isMoving;
    public bool IsInitialized => isInitialized;

    #endregion
}
