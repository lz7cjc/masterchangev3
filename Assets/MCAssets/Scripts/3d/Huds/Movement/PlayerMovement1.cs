using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// PlayerMovement1 - Simplified version that matches your existing Event Trigger pattern
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
    public Rigidbody playerRigidbody;

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
    [Tooltip("Main start/walk image GameObject")]
    public GameObject startImage;
    [Tooltip("Main stop image GameObject")]
    public GameObject stopImage;
    [Tooltip("Speed up image GameObject")]
    public GameObject speedUpImage;
    [Tooltip("Speed down image GameObject")]
    public GameObject speedDownImage;

    [Header("ToggleActiveIcons Controllers")]
    public ToggleActiveIcons startStopIconController;
    public ToggleActiveIcons speedUpIconController;
    public ToggleActiveIcons speedDownIconController;

    [Header("HUD Integration")]
    public HUDSystemCoordinator hudCoordinator;

    [Header("Debug Settings")]
    [Tooltip("Don't auto-setup Event Triggers (use manual setup)")]
    public bool useManualEventTriggers = true;

    // Private variables
    private float currentSpeed = 0f;
    private float hoverTimer = 0f;
    private bool isHovering = false;
    private bool isMoving = false;
    private bool isInitialized = false;

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

        // Update UI - IMPORTANT: This sets the initial start/stop button visibility
        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();

        isInitialized = true;
        DebugLog($"=== PlayerMovement1 initialization complete - Speed: {currentSpeed}, Moving: {isMoving} ===");
    }

    private void ValidateSetup()
    {
        if (playerRigidbody == null)
        {
            Debug.LogError("PlayerMovement1: Player Rigidbody is not assigned!");
        }

        // Validate Level 3 references
        if (startImage == null) Debug.LogWarning("PlayerMovement1: Start Image is not assigned!");
        if (stopImage == null) Debug.LogWarning("PlayerMovement1: Stop Image is not assigned!");
        if (speedUpImage == null) Debug.LogWarning("PlayerMovement1: Speed Up Image is not assigned!");
        if (speedDownImage == null) Debug.LogWarning("PlayerMovement1: Speed Down Image is not assigned!");

        DebugLog($"Obstacle Check Distance: {obstacleCheckDistance}, Ground Check Distance: {groundCheckDistance}");
        DebugLog($"Ignore Collision Layers: {ignoreCollisionLayers}");
    }

    void FixedUpdate()
    {
        if (!isInitialized) return;

        HandleHoverTimer();
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (!isMoving || currentSpeed <= 0f || playerRigidbody == null) return;

        // Get movement direction
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 horizontalDirection = new Vector3(cameraForward.x, 0f, cameraForward.z).normalized;

        Vector3 currentPosition = playerRigidbody.position;
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

        // Move the rigidbody
        playerRigidbody.MovePosition(finalPosition);

        if (debugMode && Time.frameCount % 60 == 0)
        {
            DebugLog($"Moving - Speed: {currentSpeed}, Position: {finalPosition}");
        }
    }

    private bool CheckForObstacles(Vector3 fromPosition, Vector3 direction)
    {
        // Multiple height checks to catch different obstacles
        float playerHeight = 2f; // Adjust based on your player size
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
                    continue; // Ignore this collision
                }

                // Check if it's the player itself
                if (hit.collider.transform == playerRigidbody.transform)
                {
                    continue; // Ignore self-collision
                }

                // Valid obstacle found
                if (debugMode && Time.frameCount % 30 == 0)
                {
                    DebugLog($"Obstacle hit: {hit.collider.name} at distance {hit.distance:F2}");
                }
                return true;
            }
        }

        return false; // No obstacles found
    }

    private Vector3 ApplyGroundFollowing(Vector3 targetPosition)
    {
        if (!useSimpleGroundDetection)
        {
            return targetPosition; // Use original Y position
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
                return playerRigidbody.position; // Don't move, keep current position
            }

            // Check slope angle
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                DebugLog($"Slope too steep: {slopeAngle:F1}° (max: {maxSlopeAngle}°)");
                return playerRigidbody.position; // Don't move on steep slopes
            }

            // Valid ground found - apply smooth following
            float targetY = hit.point.y;
            float currentY = playerRigidbody.position.y;
            float smoothedY = Mathf.Lerp(currentY, targetY, Time.deltaTime * terrainFollowSmoothness);

            Vector3 finalPosition = new Vector3(targetPosition.x, smoothedY, targetPosition.z);

            if (debugMode && Time.frameCount % 60 == 0)
            {
                DebugLog($"Ground follow - Hit: {hit.collider.name}, Slope: {slopeAngle:F1}°, Y: {currentY:F2}→{smoothedY:F2}");
            }

            return finalPosition;
        }
        else
        {
            // No ground found - don't move to prevent falling
            if (debugMode && Time.frameCount % 60 == 0)
            {
                DebugLog("No ground found - preventing movement");
            }
            return playerRigidbody.position;
        }
    }

    private bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void HandleHoverTimer()
    {
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // DEBUG: Log timer progress like showHideHUD does
            if (Mathf.FloorToInt(hoverTimer * 2) > Mathf.FloorToInt((hoverTimer - Time.deltaTime) * 2))
            {
                DebugLog($"Movement Timer: {hoverTimer:F1}s / {actionDelay}s");
            }

            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(actionDelay, hoverTimer);
            }

            if (hoverTimer >= actionDelay)
            {
                DebugLog("Movement Timer completed! Executing pending action");
                ExecutePendingAction();
                ResetHover();
            }
        }
    }

    private void InitializeMovementState()
    {
        if (resetSavedSpeedOnStart)
        {
            PlayerPrefs.DeleteKey("walkspeed");
            currentSpeed = 0f;
            isMoving = false;
        }
        else if (safeStart)
        {
            currentSpeed = 0f;
            isMoving = false;
        }
        else
        {
            currentSpeed = PlayerPrefs.GetFloat("walkspeed", 0f);
            isMoving = currentSpeed > 0f;
        }

        DebugLog($"Movement initialized - Speed: {currentSpeed}, Moving: {isMoving}");
    }

    private void ExecutePendingAction()
    {
        DebugLog($"=== EXECUTING PENDING ACTION: {pendingAction} ===");

        switch (pendingAction)
        {
            case ActionType.StartWalk:
                StartWalking();
                break;
            case ActionType.ChangeSpeed:
                ChangeSpeed(speedDelta);
                break;
            case ActionType.Stop:
                StopWalking();
                break;
        }

        UpdateSpeedDisplay();
        UpdateIcons();
        UpdateLevel3Controls();
        SaveSpeed();
    }

    private void StartWalking()
    {
        if (!isMoving)
        {
            currentSpeed = defaultSpeed;
            isMoving = true;
            DebugLog($"Started walking at speed: {currentSpeed}");
        }
    }

    private void ChangeSpeed(float delta)
    {
        if (isMoving)
        {
            currentSpeed = Mathf.Clamp(currentSpeed + delta, minSpeed, maxSpeed);
            if (currentSpeed <= 0f)
            {
                StopWalking();
            }
            DebugLog($"Speed changed to: {currentSpeed}");
        }
        else if (delta > 0f)
        {
            StartWalking();
        }
    }

    private void StopWalking()
    {
        currentSpeed = 0f;
        isMoving = false;
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

        // FIXED: Properly toggle start/stop button visibility
        if (startImage != null && stopImage != null)
        {
            if (isMoving)
            {
                // When moving: hide start, show stop
                startImage.SetActive(false);
                stopImage.SetActive(true);
                DebugLog("Updated UI: Start hidden, Stop visible (moving)");
            }
            else
            {
                // When stopped: show start, hide stop
                startImage.SetActive(true);
                stopImage.SetActive(false);
                DebugLog("Updated UI: Start visible, Stop hidden (stopped)");
            }
        }

        // Reset icon controllers when not hovering
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
               (stopImage != null && stopImage.activeInHierarchy) ||
               (speedUpImage != null && speedUpImage.activeInHierarchy) ||
               (speedDownImage != null && speedDownImage.activeInHierarchy);
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
        DebugLog("ResetHover called");
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

    #region Public Interface Methods - Called by Event Triggers

    public void OnMouseEnterStartWalk()
    {
        DebugLog($"OnMouseEnterStartWalk called - Current isHovering: {isHovering}, Timer: {hoverTimer}");

        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.StartWalk;
            hoverTimer = 0f;
            startStopIconController?.HoverIcon();
            DebugLog("Starting movement hover timer for StartWalk");
        }
        else
        {
            DebugLog("Already hovering or not initialized - not resetting timer");
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverEnded();
        }
    }

    public void OnMouseEnterStop()
    {
        DebugLog($"OnMouseEnterStop called - Current isHovering: {isHovering}, Timer: {hoverTimer}");

        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.Stop;
            hoverTimer = 0f;
            startStopIconController?.HoverIcon();
            DebugLog("Starting movement hover timer for Stop");
        }
        else
        {
            DebugLog("Already hovering or not initialized - not resetting timer");
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverEnded();
        }
    }

    public void OnMouseEnterSpeedUp()
    {
        DebugLog($"OnMouseEnterSpeedUp called - Current isHovering: {isHovering}, Timer: {hoverTimer}");

        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = speedIncrement;
            hoverTimer = 0f;
            speedUpIconController?.HoverIcon();
            DebugLog("Starting movement hover timer for SpeedUp");
        }
        else
        {
            DebugLog("Already hovering or not initialized - not resetting timer");
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverEnded();
        }
    }

    public void OnMouseEnterSlowDown()
    {
        DebugLog($"OnMouseEnterSlowDown called - Current isHovering: {isHovering}, Timer: {hoverTimer}");

        if (!isHovering && isInitialized)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = -speedIncrement;
            hoverTimer = 0f;
            speedDownIconController?.HoverIcon();
            DebugLog("Starting movement hover timer for SlowDown");
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverEnded();
        }
    }

    public void OnMouseExit()
    {
        DebugLog($"OnMouseExit called - Was hovering: {isHovering}, Timer was: {hoverTimer}");
        ResetHover();
    }

    public void OnMovementMenuOpened()
    {
        DebugLog("Movement menu opened - updating controls");
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
        DebugLog("Force stopped movement");
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
        DebugLog($"Force started movement at speed: {currentSpeed}");
    }

    #endregion

    #region Public Properties

    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    public bool IsHovering => isHovering;
    public bool CanIncreaseSpeed => currentSpeed < maxSpeed;
    public bool CanDecreaseSpeed => currentSpeed > minSpeed || isMoving;
    public bool IsInitialized => isInitialized;

    public float HoverProgress
    {
        get { return isHovering ? Mathf.Clamp01(hoverTimer / actionDelay) : 0f; }
    }

    #endregion

    #region Context Menu Debug Methods

    [ContextMenu("Force Start Walk")]
    private void ForceStartWalk()
    {
        if (Application.isPlaying)
        {
            DebugLog("Manual start walk triggered");
            OnMouseEnterStartWalk();
        }
    }

    [ContextMenu("Force Stop")]
    private void ForceStopWalk()
    {
        if (Application.isPlaying)
        {
            DebugLog("Manual stop triggered");
            OnMouseEnterStop();
        }
    }

    [ContextMenu("Force Speed Up")]
    private void ForceSpeedUp()
    {
        if (Application.isPlaying)
        {
            DebugLog("Manual speed up triggered");
            OnMouseEnterSpeedUp();
        }
    }

    [ContextMenu("Force Speed Down")]
    private void ForceSpeedDown()
    {
        if (Application.isPlaying)
        {
            DebugLog("Manual speed down triggered");
            OnMouseEnterSlowDown();
        }
    }

    [ContextMenu("Test Event Trigger")]
    private void TestEventTrigger()
    {
        if (Application.isPlaying)
        {
            DebugLog("Testing event trigger...");
            OnMouseEnterStartWalk();
        }
    }

    #endregion
}