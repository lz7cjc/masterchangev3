using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UPDATED GazeHoverTrigger v2.1 - Mode-aware interactions with marker teleport
/// VR Mode: Gaze-based hover with countdown
/// 360 Mode: Touch/tap to trigger instantly
/// NEW: Teleport to marker support for custom zone systems
/// 
/// Handles concave MeshColliders, fixes countdown
/// </summary>
[RequireComponent(typeof(Collider))]
public class GazeHoverTrigger_premigration : MonoBehaviour
{
    [Header("Action Settings")]
    public string actionName;
    public float hoverDelay = 3f;
    public bool isHUDElement = false;
    public bool continuousHover = false; // Keep hover active after completion (for rotation, etc.)

    [Header("Visual Feedback")]
    public bool showCountdown = true;

    [Header("Auto-Detection")]
    public bool autoDetectZoneManager = true;
    public bool autoDetectVideoPlayer = true;

    [Header("Manual References (Optional)")]
    public ZoneManager manualZoneManager;
    public EnhancedVideoPlayer manualVideoPlayer;

    [Header("NEW - Marker Teleport System")]
    [Tooltip("Enable teleport to a specific marker for precise camera positioning")]
    public bool useTeleportMarker = false;
    [Tooltip("Transform marker to teleport the player to")]
    public Transform teleportMarker;
    [Tooltip("Reference to the Player object to move (auto-detected if not set)")]
    public Transform playerObject;
    [Tooltip("If true, also match the marker's rotation. If false, only teleport position.")]
    public bool matchRotation = true;

    [Header("Custom Events")]
    public UnityEvent onHoverStart;
    public UnityEvent onHoverComplete;
    public UnityEvent onHoverCancel;

    [Header("NEW - 360 Mode Settings")]
    [Tooltip("In 360 mode, require touch drag instead of tap (prevents accidental triggers)")]
    public bool require360Drag = false;
    [Tooltip("In 360 mode, use countdown delay (otherwise instant trigger)")]
    public bool use360Countdown = false;

    [Header("Debug")]
    public bool debugMode = false;

    private bool isHovering = false;
    private float hoverTimer = 0f;

    private ZoneManager zoneManager;
    private EnhancedVideoPlayer videoPlayer;
    private hudCountdown hudCountdown;
    private Collider triggerCollider;

    private enum InteractionMode { None, ZoneTeleport, VideoPlayer, CustomEvent, MarkerTeleport }
    private InteractionMode currentMode = InteractionMode.None;

    void Awake()
    {
        // FIX: Handle MeshColliders properly
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            MeshCollider meshCol = triggerCollider as MeshCollider;
            if (meshCol != null)
            {
                if (!meshCol.convex)
                {
                    Debug.LogWarning($"[GazeHoverTrigger] {gameObject.name} has concave MeshCollider - converting to BoxCollider");

                    Bounds bounds = meshCol.bounds;
                    Vector3 center = meshCol.bounds.center - transform.position;

                    DestroyImmediate(meshCol);

                    BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                    boxCol.center = center;
                    boxCol.size = bounds.size;
                    boxCol.isTrigger = false; // Must be false for OnMouseDown to work

                    triggerCollider = boxCol;
                    if (debugMode) Debug.Log($"[GazeHoverTrigger] Replaced MeshCollider with BoxCollider");
                }
                else
                {
                    // For gaze interactions, keep as trigger
                    // OnMouseDown works with both trigger and non-trigger colliders
                    meshCol.isTrigger = false;
                }
            }
            else
            {
                // For other collider types, keep as non-trigger for OnMouseDown
                triggerCollider.isTrigger = false;
            }
        }

        DetectInteractionMode();

        // Auto-detect Player object if not set
        if (useTeleportMarker && playerObject == null)
        {
            // Try to find the Player object by name
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerObject = player.transform;
                if (debugMode) Debug.Log($"[GazeHoverTrigger] Auto-detected Player object: {playerObject.name}");
            }
            else
            {
                // Fallback: find Camera.main and get its root parent
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerObject = mainCam.transform.root;
                    if (debugMode) Debug.Log($"[GazeHoverTrigger] Auto-detected Player from camera root: {playerObject.name}");
                }
            }
        }

        // Find countdown using correct type
        if (showCountdown)
        {
            hudCountdown = FindObjectOfType<hudCountdown>();
            if (hudCountdown != null && debugMode)
            {
                Debug.Log($"[GazeHoverTrigger] Found hudCountdown");
            }
        }

        if (debugMode)
        {
            Debug.Log($"[GazeHoverTrigger] Init: {gameObject.name}, Mode: {currentMode}, Delay: {hoverDelay}s");
        }
    }

    void Update()
    {
        // NEW v2.0: Check for touch input in 360 mode
        CheckForTouchInput();

        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // Update countdown visual correctly
            if (showCountdown && hudCountdown != null)
            {
                float progress = Mathf.Clamp01(hoverTimer / hoverDelay);
                hudCountdown.SetCountdown(hoverDelay, hoverTimer);
            }

            if (hoverTimer >= hoverDelay)
            {
                CompleteHover();
            }
        }
    }

    // Double-click detection variables
    private float lastClickTime = 0f;
    private const float doubleClickThreshold = 0.3f; // 300ms for double-click

    /// <summary>
    /// NEW v2.0: Touch detection for 360 mode using raycast (works with trigger colliders)
    /// Checks every frame for touch input and raycasts from camera
    /// EDITOR: Double-click to trigger (easier testing without accidental single clicks)
    /// DEVICE: Single tap to trigger
    /// </summary>
    private void CheckForTouchInput()
    {
        // Get current camera mode
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        GazeReticlePointer pointer = mainCam.GetComponent<GazeReticlePointer>();

        // Only process touch in 360 mode
        if (pointer == null || pointer.currentMode != GazeReticlePointer.ViewMode.Mode360)
            return;

        // Check for touch/click input
        bool touchDown = false;
        Vector2 touchPosition = Vector2.zero;

        // Mobile: Check for touch (single tap)
        if (Application.isMobilePlatform && UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
            if (touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                touchDown = true;
                touchPosition = touchscreen.primaryTouch.position.ReadValue();
                if (debugMode) Debug.Log("[GazeHoverTrigger] Mobile tap detected");
            }
        }
        // Editor/Desktop: Check for DOUBLE-CLICK (easier testing)
        else if (UnityEngine.InputSystem.Mouse.current != null)
        {
            var mouse = UnityEngine.InputSystem.Mouse.current;

            // Double-click detection
            if (mouse.leftButton.wasPressedThisFrame)
            {
                float timeSinceLastClick = Time.time - lastClickTime;

                if (timeSinceLastClick < doubleClickThreshold)
                {
                    // Double-click detected!
                    touchDown = true;
                    touchPosition = mouse.position.ReadValue();
                    if (debugMode) Debug.Log("[GazeHoverTrigger] Double-click detected in editor");
                }

                lastClickTime = Time.time;
            }
        }

        if (!touchDown) return;

        // Raycast from camera to check if this object was tapped
        Ray ray = mainCam.ScreenPointToRay(touchPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (debugMode) Debug.Log($"[GazeHoverTrigger] 360 Mode touch detected on: {actionName}");

                // Option 1: Instant trigger (default for 360 mode - good UX)
                if (!use360Countdown)
                {
                    if (debugMode) Debug.Log("[GazeHoverTrigger] Instant trigger in 360 mode");
                    CompleteHover();
                }
                // Option 2: Start countdown on tap (if use360Countdown enabled)
                else
                {
                    if (!isHovering)
                    {
                        if (debugMode) Debug.Log("[GazeHoverTrigger] Starting countdown via touch");
                        OnGazeEnter();
                    }
                }
            }
        }
    }

    private void DetectInteractionMode()
    {
        // Check for marker teleport first (highest priority)
        if (useTeleportMarker && teleportMarker != null)
        {
            currentMode = InteractionMode.MarkerTeleport;
            if (debugMode) Debug.Log($"[GazeHoverTrigger] Mode: MarkerTeleport to {teleportMarker.name}");
            return;
        }

        if (autoDetectZoneManager)
        {
            zoneManager = manualZoneManager != null ? manualZoneManager : FindObjectOfType<ZoneManager>();
            if (zoneManager != null)
            {
                currentMode = InteractionMode.ZoneTeleport;
                if (debugMode) Debug.Log($"[GazeHoverTrigger] Mode: ZoneTeleport");
                return;
            }
        }

        if (autoDetectVideoPlayer)
        {
            videoPlayer = manualVideoPlayer != null ? manualVideoPlayer : GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                currentMode = InteractionMode.VideoPlayer;
                if (debugMode) Debug.Log($"[GazeHoverTrigger] Mode: VideoPlayer");
                return;
            }
        }

        if (onHoverComplete.GetPersistentEventCount() > 0)
        {
            currentMode = InteractionMode.CustomEvent;
            if (debugMode) Debug.Log($"[GazeHoverTrigger] Mode: CustomEvent");
            return;
        }

        currentMode = InteractionMode.None;
    }

    public void OnGazeEnter()
    {
        if (isHovering) return;

        isHovering = true;
        hoverTimer = 0f;

        if (debugMode) Debug.Log($"[GazeHoverTrigger] Gaze entered: {actionName}");

        // Start countdown correctly (initialize display to full wait value)
        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.SetCountdown(hoverDelay, 0f);
        }

        switch (currentMode)
        {
            case InteractionMode.ZoneTeleport:
                if (zoneManager != null)
                {
                    zoneManager.OnHoverEnter(gameObject.name);
                }
                break;

            case InteractionMode.MarkerTeleport:
                // No hover enter action needed for marker teleport
                break;

            case InteractionMode.CustomEvent:
                onHoverStart?.Invoke();
                break;
        }
    }

    public void OnGazeExit()
    {
        if (!isHovering) return;

        if (debugMode) Debug.Log($"[GazeHoverTrigger] Gaze exited: {actionName}");

        // Reset countdown correctly
        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }

        switch (currentMode)
        {
            case InteractionMode.ZoneTeleport:
                if (zoneManager != null)
                {
                    zoneManager.OnHoverExit(gameObject.name);
                }
                break;

            case InteractionMode.VideoPlayer:
                if (videoPlayer != null)
                {
                    videoPlayer.MouseExit();
                }
                break;

            case InteractionMode.MarkerTeleport:
                // No hover exit action needed for marker teleport
                break;

            case InteractionMode.CustomEvent:
                onHoverCancel?.Invoke();
                break;
        }

        isHovering = false;
        hoverTimer = 0f;
    }

    private void CompleteHover()
    {
        if (debugMode) Debug.Log($"[GazeHoverTrigger] Hover completed: {actionName}");

        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.SetCountdown(hoverDelay, hoverDelay);
            hudCountdown.resetCountdown();
        }

        switch (currentMode)
        {
            case InteractionMode.ZoneTeleport:
                // ZoneManager handles teleport in Update
                break;

            case InteractionMode.VideoPlayer:
                if (videoPlayer != null)
                {
                    videoPlayer.MouseHoverChangeScene();
                }
                isHovering = false;
                hoverTimer = 0f;
                break;

            case InteractionMode.MarkerTeleport:
                TeleportToMarker();
                isHovering = false;
                hoverTimer = 0f;
                break;

            case InteractionMode.CustomEvent:
                onHoverComplete?.Invoke();
                // Only keep hovering if continuousHover is enabled (for rotation/continuous actions)
                if (!continuousHover)
                {
                    isHovering = false;
                    hoverTimer = 0f;
                }
                break;

            case InteractionMode.None:
                Debug.LogWarning($"[GazeHoverTrigger] No action configured on {gameObject.name}");
                isHovering = false;
                hoverTimer = 0f;
                break;
        }
    }

    /// <summary>
    /// NEW v2.1: Teleport entire Player object to the specified marker
    /// Makes Player a child of the marker in hierarchy for easy real-time adjustment in Play mode
    /// </summary>
    private void TeleportToMarker()
    {
        if (teleportMarker == null)
        {
            Debug.LogError($"[GazeHoverTrigger] Teleport marker not assigned on {gameObject.name}");
            return;
        }

        if (playerObject == null)
        {
            Debug.LogError($"[GazeHoverTrigger] Player object not found for teleport on {gameObject.name}");
            return;
        }

        Vector3 startPos = playerObject.position;
        Quaternion startRot = playerObject.rotation;

        Debug.Log($"[GazeHoverTrigger] === TELEPORT START ===");
        Debug.Log($"[GazeHoverTrigger] Player: {playerObject.name}");
        Debug.Log($"[GazeHoverTrigger] FROM Position: {startPos}");
        Debug.Log($"[GazeHoverTrigger] TO Marker: {teleportMarker.name} at {teleportMarker.position}");

        // Check if Character Controller is blocking movement
        CharacterController controller = playerObject.GetComponent<CharacterController>();
        bool wasEnabled = false;
        if (controller != null)
        {
            wasEnabled = controller.enabled;
            if (wasEnabled)
            {
                controller.enabled = false; // Disable temporarily for reparenting
                Debug.Log($"[GazeHoverTrigger] Disabled CharacterController for teleport");
            }
        }

        // WORKFLOW: Make Player a child of the marker in hierarchy
        // This allows you to adjust the marker transform in Play mode and the Player moves with it
        Transform originalParent = playerObject.parent;

        // Set Player as child of marker, maintaining world position initially
        playerObject.SetParent(teleportMarker, true);

        Debug.Log($"[GazeHoverTrigger] Player is now child of {teleportMarker.name} in hierarchy");

        // Now reset local position/rotation so Player sits exactly at marker's transform
        playerObject.localPosition = Vector3.zero;

        if (matchRotation)
        {
            playerObject.localRotation = Quaternion.identity;
        }
        else
        {
            // Keep current world rotation
            playerObject.rotation = startRot;
        }

        // Re-enable Character Controller
        if (controller != null && wasEnabled)
        {
            controller.enabled = true;
            Debug.Log($"[GazeHoverTrigger] Re-enabled CharacterController");
        }

        Debug.Log($"[GazeHoverTrigger] AFTER Teleport Position: {playerObject.position}");
        Debug.Log($"[GazeHoverTrigger] Distance moved: {Vector3.Distance(startPos, playerObject.position):F2} units");
        Debug.Log($"[GazeHoverTrigger] Player is now at local position (0,0,0) relative to marker");
        Debug.Log($"[GazeHoverTrigger] === ADJUST MARKER NOW TO FINE-TUNE VIEW ===");
        Debug.Log($"[GazeHoverTrigger] 1. Select marker in hierarchy");
        Debug.Log($"[GazeHoverTrigger] 2. Adjust marker transform until view is perfect");
        Debug.Log($"[GazeHoverTrigger] 3. Copy marker transform values");
        Debug.Log($"[GazeHoverTrigger] 4. Exit Play mode and paste values to marker");
        Debug.Log($"[GazeHoverTrigger] === TELEPORT COMPLETE ===");

        // Visual confirmation in Scene view - select the MARKER so you can adjust it
#if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = teleportMarker.gameObject;
        UnityEditor.SceneView.lastActiveSceneView?.FrameSelected();
#endif

        // Invoke custom events after teleport (for any additional logic)
        onHoverComplete?.Invoke();
    }

    public void ResetHoverState()
    {
        if (isHovering)
        {
            OnGazeExit();
        }
    }

    public bool IsHovering => isHovering;
    public float HoverProgress => isHovering ? Mathf.Clamp01(hoverTimer / hoverDelay) : 0f;

    #region Visual Debugging

    /// <summary>
    /// Draw gizmos in Scene view to show teleport marker location
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!useTeleportMarker || teleportMarker == null) return;

        // Draw line from this trigger to the marker
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, teleportMarker.position);

        // Draw sphere at marker position
        Gizmos.color = new Color(0, 1, 1, 0.3f); // Transparent cyan
        Gizmos.DrawSphere(teleportMarker.position, 0.5f);

        // Draw forward direction arrow if matching rotation
        if (matchRotation)
        {
            Gizmos.color = Color.yellow;
            Vector3 forward = teleportMarker.forward * 2f;
            Gizmos.DrawRay(teleportMarker.position, forward);

            // Draw arrowhead
            Vector3 arrowTip = teleportMarker.position + forward;
            Vector3 right = teleportMarker.right * 0.5f;
            Vector3 up = teleportMarker.up * 0.5f;
            Gizmos.DrawLine(arrowTip, arrowTip - forward.normalized * 0.5f + right);
            Gizmos.DrawLine(arrowTip, arrowTip - forward.normalized * 0.5f - right);
        }
    }

    /// <summary>
    /// Draw selected gizmos with labels
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!useTeleportMarker || teleportMarker == null) return;

        // Draw wireframe sphere at marker
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(teleportMarker.position, 0.5f);

#if UNITY_EDITOR
        // Draw label
        UnityEditor.Handles.Label(
            teleportMarker.position + Vector3.up * 1f,
            $"Teleport Target: {teleportMarker.name}\n{teleportMarker.position}",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = Color.cyan },
                alignment = TextAnchor.MiddleCenter
            }
        );
#endif
    }

    #endregion
}