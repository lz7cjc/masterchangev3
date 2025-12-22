using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Universal hover interaction component for VR gaze-based selection
/// Works with EnhancedVideoPlayer, HUD buttons, and any interactive object
/// Attach to any GameObject with a collider that should be interactable
/// Compatible with both VR and 360 modes
/// </summary>
[RequireComponent(typeof(Collider))]
public class GazeHoverTrigger : MonoBehaviour
{
    [Header("Action Settings")]
    [Tooltip("Name for debugging")]
    [SerializeField] private string actionName;
    
    [Tooltip("Time in seconds to trigger action")]
    [SerializeField] private float hoverDelay = 3f;
    
    [Tooltip("Is this a HUD element? (affects HUD rotation freezing)")]
    [SerializeField] private bool isHUDElement = true;

    [Header("Visual Feedback")]
    [Tooltip("Show countdown on HUD")]
    [SerializeField] private bool showCountdown = true;

    [Header("EnhancedVideoPlayer Integration")]
    [Tooltip("Forward hover events to EnhancedVideoPlayer (if present)")]
    [SerializeField] private bool forwardToVideoPlayer = true;
    private EnhancedVideoPlayer videoPlayer;

    [Header("Scene Teleport Settings (Optional)")]
    [Tooltip("Enable teleportation on hover complete")]
    [SerializeField] private bool enableTeleport = false;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject targetLocation;

    [Header("Events")]
    public UnityEvent OnHoverComplete;
    public UnityEvent OnHoverStart;
    public UnityEvent OnHoverCancel;

    // State
    private bool isHovering = false;
    private float hoverCounter = 0f;
    private GazeHUDCountdown hudCountdown;
    private bool hasTriggered = false;

    void Start()
    {
        // Ensure collider is trigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[GazeHoverTrigger] Collider on {gameObject.name} not set as trigger! Auto-fixing...");
            col.isTrigger = true;
        }

        // Find HUD countdown if needed
        if (showCountdown)
        {
            hudCountdown = FindFirstObjectByType<GazeHUDCountdown>();
            if (hudCountdown == null)
            {
                Debug.LogWarning($"[GazeHoverTrigger] showCountdown enabled but no GazeHUDCountdown found");
            }
        }

        // Check for EnhancedVideoPlayer
        if (forwardToVideoPlayer)
        {
            videoPlayer = GetComponent<EnhancedVideoPlayer>();
            if (videoPlayer != null)
            {
                Debug.Log($"[GazeHoverTrigger] EnhancedVideoPlayer found on {gameObject.name}");
            }
        }

        // Validate teleport settings
        if (enableTeleport && (player == null || targetLocation == null))
        {
            Debug.LogWarning($"[GazeHoverTrigger] Teleport enabled but player/target not assigned on {gameObject.name}");
        }

        // Auto-assign action name if empty
        if (string.IsNullOrEmpty(actionName))
        {
            actionName = gameObject.name;
        }
    }

    void Update()
    {
        if (isHovering && !hasTriggered)
        {
            UpdateHoverProgress();
        }
    }

    /// <summary>
    /// Called by GazeReticlePointer when reticle enters
    /// </summary>
    public void OnReticleEnter()
    {
        if (hasTriggered) return;

        isHovering = true;
        hoverCounter = 0f;

        // Start countdown visual
        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.StartCountdown(hoverDelay);
        }

        // Forward to EnhancedVideoPlayer
        if (videoPlayer != null)
        {
            videoPlayer.MouseHoverChangeScene();
        }

        // Invoke event
        OnHoverStart?.Invoke();

        Debug.Log($"[GazeHoverTrigger] Started hovering on {actionName}");
    }

    /// <summary>
    /// Called by GazeReticlePointer when reticle exits
    /// </summary>
    public void OnReticleExit()
    {
        if (!isHovering) return;

        isHovering = false;
        hoverCounter = 0f;
        hasTriggered = false;

        // Reset countdown visual
        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.ResetCountdown();
        }

        // Forward to EnhancedVideoPlayer
        if (videoPlayer != null)
        {
            videoPlayer.MouseExit();
        }

        // Invoke event
        OnHoverCancel?.Invoke();
    }

    /// <summary>
    /// Update hover progress and trigger action when complete
    /// </summary>
    private void UpdateHoverProgress()
    {
        hoverCounter += Time.deltaTime;

        // Update countdown visual
        if (showCountdown && hudCountdown != null)
        {
            float progress = Mathf.Clamp01(hoverCounter / hoverDelay);
            hudCountdown.UpdateCountdown(progress);
        }

        // Check if hover complete
        if (hoverCounter >= hoverDelay)
        {
            TriggerHoverComplete();
        }
    }

    /// <summary>
    /// Execute action when hover delay is complete
    /// </summary>
    private void TriggerHoverComplete()
    {
        hasTriggered = true;
        isHovering = false;
        hoverCounter = 0f;

        // Reset countdown visual
        if (showCountdown && hudCountdown != null)
        {
            hudCountdown.ResetCountdown();
        }

        // Execute teleport if enabled
        if (enableTeleport && player != null && targetLocation != null)
        {
            TeleportPlayer();
        }

        // Forward to EnhancedVideoPlayer
        // NOTE: EnhancedVideoPlayer handles its own timer, so we don't call SetVideoUrl
        // The video player's Update loop will trigger when its timer completes

        // Invoke event
        OnHoverComplete?.Invoke();

        Debug.Log($"[GazeHoverTrigger] ✓ Hover complete on {actionName}");

        // Reset after delay
        Invoke(nameof(ResetTrigger), 0.5f);
    }

    /// <summary>
    /// Teleport player to target location
    /// </summary>
    private void TeleportPlayer()
    {
        player.transform.position = targetLocation.transform.position;
        player.transform.SetParent(targetLocation.transform);
        player.transform.rotation = Quaternion.identity;

        Debug.Log($"[GazeHoverTrigger] Teleported player to {targetLocation.name}");
    }

    /// <summary>
    /// Reset trigger state
    /// </summary>
    private void ResetTrigger()
    {
        hasTriggered = false;
    }

    /// <summary>
    /// Check if this is a HUD element
    /// </summary>
    public bool IsHUDElement()
    {
        return isHUDElement;
    }

    /// <summary>
    /// Get hover delay
    /// </summary>
    public float GetHoverDelay()
    {
        return hoverDelay;
    }

    /// <summary>
    /// Get current hover progress (0-1)
    /// </summary>
    public float GetHoverProgress()
    {
        return Mathf.Clamp01(hoverCounter / hoverDelay);
    }

    /// <summary>
    /// Manually trigger action (for testing)
    /// </summary>
    [ContextMenu("Manual Trigger")]
    public void ManualTrigger()
    {
        TriggerHoverComplete();
    }

    /// <summary>
    /// Reset hover state
    /// </summary>
    public void ResetHover()
    {
        OnReticleExit();
    }

    /// <summary>
    /// Set hover delay at runtime
    /// </summary>
    public void SetHoverDelay(float delay)
    {
        hoverDelay = Mathf.Max(0.1f, delay);
    }

    // Visualization in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = isHUDElement ? Color.cyan : Color.yellow;
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.1f);
        }

        // Draw line to teleport target
        if (enableTeleport && targetLocation != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetLocation.transform.position);
            Gizmos.DrawWireSphere(targetLocation.transform.position, 0.2f);
        }
    }
}
