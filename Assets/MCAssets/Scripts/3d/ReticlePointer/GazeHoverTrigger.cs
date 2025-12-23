using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// FIXED GazeHoverTrigger - Handles concave MeshColliders, fixes countdown
/// </summary>
[RequireComponent(typeof(Collider))]
public class GazeHoverTrigger : MonoBehaviour
{
    [Header("Action Settings")]
    public string actionName;
    public float hoverDelay = 3f;
    public bool isHUDElement = false;

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
                    boxCol.isTrigger = true;
                    
                    triggerCollider = boxCol;
                    if (debugMode) Debug.Log($"[GazeHoverTrigger] Replaced MeshCollider with BoxCollider");
                }
                else
                {
                    meshCol.isTrigger = true;
                }
            }
            else
            {
                triggerCollider.isTrigger = true;
            }
        }

        DetectInteractionMode();

        // FIX: Find countdown using correct type
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
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // FIX: Update countdown visual correctly
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

        // FIX: Start countdown correctly (hudCountdown doesn't expose StartCountdown)
        if (showCountdown && hudCountdown != null)
        {
            // initialize display to full wait value (counter = 0)
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

        // FIX: Reset countdown correctly (hudCountdown's method is resetCountdown)
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
                isHovering = false;
                hoverTimer = 0f;
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
