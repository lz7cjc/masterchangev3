using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UPDATED GazeHoverTrigger v2.0 - Mode-aware interactions
/// VR Mode: Gaze-based hover with countdown
/// 360 Mode: Touch/tap to trigger instantly
/// 
/// Handles concave MeshColliders, fixes countdown
/// </summary>
[RequireComponent(typeof(Collider))]
public class GazeHoverTrigger : MonoBehaviour
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

    private enum InteractionMode { None, ZoneTeleport, VideoPlayer, CustomEvent }
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

    public void ResetHoverState()
    {
        if (isHovering)
        {
            OnGazeExit();
        }
    }

    public bool IsHovering => isHovering;
    public float HoverProgress => isHovering ? Mathf.Clamp01(hoverTimer / hoverDelay) : 0f;
}
