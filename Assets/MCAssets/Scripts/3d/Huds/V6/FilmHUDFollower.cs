using UnityEngine;

/// <summary>
/// Clean, professional HUD follower for Film Controls.
/// Simple threshold-based following - no conflicts, no complexity.
/// 
/// ATTACH TO: HUDPivot GameObject
/// 
/// HOW IT WORKS:
/// - HUD stays at fixed position in front of camera
/// - Only rotates when camera turns beyond threshold (default 30°)
/// - Smooth following, no jarring movements
/// - Works in both VR and 360 modes
/// </summary>
public class FilmHUDFollower : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform cameraVR;
    [SerializeField] private Transform camera360;
    [SerializeField] private bool autoFindCameras = true;

    [Header("Position Settings")]
    [SerializeField] private float distanceFromCamera = 0.8f;
    [Tooltip("Angle above/below camera center (0 = straight ahead, positive = up)")]
    [SerializeField, Range(-45f, 45f)] private float verticalAngle = 0f;

    [Header("Follow Settings")]
    [SerializeField] private FollowMode followMode = FollowMode.CanvasBounds;

    [Header("Canvas Bounds Mode")]
    [Tooltip("HUD follows when reticle exits canvas bounds + buffer")]
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private bool autoFindCanvas = true;
    [SerializeField, Range(0f, 100f)] private float boundsBufferPercent = 10f;

    [Header("Rotation Angle Mode")]
    [Tooltip("Degrees camera must rotate before HUD follows")]
    [SerializeField, Range(10f, 60f)] private float followThreshold = 30f;

    [Header("General Settings")]
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool smoothFollow = true;
    [Tooltip("Only follow horizontal rotation (recommended for film controls)")]
    [SerializeField] private bool horizontalFollowOnly = true;

    public enum FollowMode
    {
        CanvasBounds,    // Follow when reticle outside canvas (Nick's idea - BETTER!)
        RotationAngle    // Follow when camera rotates X degrees
    }

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showDebugGizmos = true;

    private Transform activeCamera;
    private Camera activeCameraComponent; // For WorldToScreenPoint
    private float lastCameraYRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        // Find active camera
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            if (showDebugInfo && Time.frameCount % 60 == 0)
                Debug.LogWarning("[FilmHUDFollower] No active camera found!");
            return;
        }

        // Auto-find canvas if needed
        if (followMode == FollowMode.CanvasBounds && canvasRect == null && autoFindCanvas)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
                if (showDebugInfo)
                    Debug.Log($"[FilmHUDFollower] Auto-found canvas: {canvas.name}");
            }
        }

        // Handle follow behavior based on mode
        if (followMode == FollowMode.CanvasBounds)
        {
            UpdateHUDFollowByCanvasBounds();
        }
        else
        {
            UpdateHUDFollow(); // Original rotation angle method
        }
    }

    /// <summary>
    /// Initialize HUD position and rotation
    /// </summary>
    private void Initialize()
    {
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            Debug.LogError("[FilmHUDFollower] Cannot initialize - no camera found!");
            return;
        }

        // Calculate initial position
        CalculateTargetPosition();
        transform.position = targetPosition;

        // Set initial rotation
        lastCameraYRotation = activeCamera.eulerAngles.y;
        CalculateTargetRotation();
        transform.rotation = targetRotation;

        isInitialized = true;

        if (showDebugInfo)
        {
            Debug.Log($"<color=cyan>[FilmHUDFollower] ✓ Initialized</color>");
            Debug.Log($"  Position: {transform.position}");
            Debug.Log($"  Distance: {distanceFromCamera}m");
            Debug.Log($"  Threshold: {followThreshold}°");
        }
    }

    /// <summary>
    /// Update HUD follow based on canvas bounds (Nick's better idea!)
    /// </summary>
    private void UpdateHUDFollowByCanvasBounds()
    {
        // Always keep HUD at correct distance from camera
        CalculateTargetPosition();
        transform.position = targetPosition;

        if (canvasRect == null)
        {
            // Fallback to rotation angle mode if no canvas
            UpdateHUDFollow();
            return;
        }

        // Check if reticle is outside canvas bounds + buffer
        bool reticleOutsideBounds = IsReticleOutsideCanvasBounds();

        if (reticleOutsideBounds)
        {
            // Reticle is outside - follow camera rotation
            if (smoothFollow)
            {
                CalculateTargetRotation();
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);
            }
            else
            {
                CalculateTargetRotation();
                transform.rotation = targetRotation;
            }

            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[FilmHUDFollower] Reticle outside bounds - following");
            }
        }
        // If reticle is inside bounds, HUD stays put (doesn't rotate)
    }

    /// <summary>
    /// Check if reticle (screen center) is outside canvas bounds + buffer
    /// </summary>
    private bool IsReticleOutsideCanvasBounds()
    {
        if (canvasRect == null) return false;

        // Get canvas bounds in screen space
        Vector3[] canvasCorners = new Vector3[4];
        canvasRect.GetWorldCorners(canvasCorners);

        // Convert world corners to screen space
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = activeCameraComponent.WorldToScreenPoint(canvasCorners[i]);
            min = Vector2.Min(min, screenPoint);
            max = Vector2.Max(max, screenPoint);
        }

        // Add buffer (percentage of canvas size)
        Vector2 canvasSize = max - min;
        float bufferX = canvasSize.x * (boundsBufferPercent / 100f);
        float bufferY = canvasSize.y * (boundsBufferPercent / 100f);

        min -= new Vector2(bufferX, bufferY);
        max += new Vector2(bufferX, bufferY);

        // Check if screen center (reticle position) is outside bounds
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        bool outside = screenCenter.x < min.x || screenCenter.x > max.x ||
                      screenCenter.y < min.y || screenCenter.y > max.y;

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[FilmHUDFollower] Bounds: ({min.x:F0},{min.y:F0}) to ({max.x:F0},{max.y:F0}), " +
                     $"Reticle: ({screenCenter.x:F0},{screenCenter.y:F0}), Outside: {outside}");
        }

        return outside;
    }

    /// <summary>
    /// Update HUD follow behavior based on camera rotation (original method)
    /// </summary>
    private void UpdateHUDFollow()
    {
        // Always keep HUD at correct distance from camera
        CalculateTargetPosition();
        transform.position = targetPosition;

        // Check rotation difference
        float currentCameraY = activeCamera.eulerAngles.y;
        float angleDiff = Mathf.DeltaAngle(lastCameraYRotation, currentCameraY);

        // Only follow if threshold exceeded
        if (Mathf.Abs(angleDiff) > followThreshold)
        {
            if (smoothFollow)
            {
                // Smooth rotation
                CalculateTargetRotation();
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, followSpeed * Time.deltaTime);

                // Update last rotation when close to target
                if (Quaternion.Angle(transform.rotation, targetRotation) < 2f)
                {
                    lastCameraYRotation = currentCameraY;
                }
            }
            else
            {
                // Instant rotation
                CalculateTargetRotation();
                transform.rotation = targetRotation;
                lastCameraYRotation = currentCameraY;
            }

            if (showDebugInfo && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[FilmHUDFollower] Following - Angle diff: {angleDiff:F1}°");
            }
        }
    }

    /// <summary>
    /// Calculate target position in front of camera
    /// </summary>
    private void CalculateTargetPosition()
    {
        // Start from camera position
        Vector3 forward = activeCamera.forward;

        // Apply vertical angle offset
        if (verticalAngle != 0)
        {
            // Rotate forward vector around camera's right axis
            Quaternion verticalRotation = Quaternion.AngleAxis(verticalAngle, activeCamera.right);
            forward = verticalRotation * forward;
        }

        // Position at distance in front of camera
        targetPosition = activeCamera.position + (forward * distanceFromCamera);
    }

    /// <summary>
    /// Calculate target rotation to face camera
    /// </summary>
    private void CalculateTargetRotation()
    {
        if (horizontalFollowOnly)
        {
            // Only rotate around Y axis (horizontal)
            float yRotation = activeCamera.eulerAngles.y;
            targetRotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        else
        {
            // Full rotation to face camera
            Vector3 directionToCamera = activeCamera.position - transform.position;
            targetRotation = Quaternion.LookRotation(directionToCamera);
        }
    }

    /// <summary>
    /// Find which camera is currently active
    /// </summary>
    private void UpdateActiveCamera()
    {
        // Auto-find cameras if enabled
        if (autoFindCameras)
        {
            if (cameraVR == null)
            {
                GameObject camVR = GameObject.Find("Main CameraVR");
                if (camVR != null) cameraVR = camVR.transform;
            }
            if (camera360 == null)
            {
                GameObject cam360 = GameObject.Find("Main Camera360");
                if (cam360 != null) camera360 = cam360.transform;
            }
        }

        // Determine which camera is active
        Transform newActiveCamera = null;

        if (cameraVR != null && cameraVR.gameObject.activeInHierarchy && cameraVR.GetComponent<Camera>().enabled)
        {
            newActiveCamera = cameraVR;
        }
        else if (camera360 != null && camera360.gameObject.activeInHierarchy && camera360.GetComponent<Camera>().enabled)
        {
            newActiveCamera = camera360;
        }
        else
        {
            // Fallback to Camera.main
            Camera mainCam = Camera.main;
            if (mainCam != null)
                newActiveCamera = mainCam.transform;
        }

        // If camera switched, re-initialize
        if (newActiveCamera != activeCamera && newActiveCamera != null)
        {
            activeCamera = newActiveCamera;
            activeCameraComponent = activeCamera.GetComponent<Camera>();

            if (showDebugInfo)
                Debug.Log($"<color=cyan>[FilmHUDFollower] Switched to: {activeCamera.name}</color>");

            if (isInitialized)
                Initialize(); // Re-initialize with new camera
        }
    }

    /// <summary>
    /// Snap HUD to camera immediately (useful when toggling HUD on)
    /// </summary>
    public void SnapToCamera()
    {
        if (activeCamera == null)
        {
            UpdateActiveCamera();
        }

        if (activeCamera != null)
        {
            CalculateTargetPosition();
            transform.position = targetPosition;

            lastCameraYRotation = activeCamera.eulerAngles.y;
            CalculateTargetRotation();
            transform.rotation = targetRotation;

            if (showDebugInfo)
                Debug.Log("<color=green>[FilmHUDFollower] ✓ Snapped to camera</color>");
        }
    }

    /// <summary>
    /// Force HUD to follow camera rotation immediately
    /// </summary>
    [ContextMenu("Force Follow Now")]
    public void ForceFollowNow()
    {
        if (activeCamera != null)
        {
            lastCameraYRotation = activeCamera.eulerAngles.y;
            CalculateTargetRotation();
            transform.rotation = targetRotation;

            if (showDebugInfo)
                Debug.Log("<color=green>[FilmHUDFollower] ✓ Forced rotation update</color>");
        }
    }

    /// <summary>
    /// Set follow threshold at runtime
    /// </summary>
    public void SetFollowThreshold(float threshold)
    {
        followThreshold = Mathf.Clamp(threshold, 10f, 60f);

        if (showDebugInfo)
            Debug.Log($"[FilmHUDFollower] Follow threshold set to: {followThreshold}°");
    }

    /// <summary>
    /// Set distance from camera at runtime
    /// </summary>
    public void SetDistanceFromCamera(float distance)
    {
        distanceFromCamera = Mathf.Max(0.1f, distance);

        if (showDebugInfo)
            Debug.Log($"[FilmHUDFollower] Distance set to: {distanceFromCamera}m");
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || activeCamera == null) return;

        // Draw line from camera to HUD
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(activeCamera.position, transform.position);

        // Draw HUD forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);

        // Draw camera forward direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(activeCamera.position, activeCamera.forward * distanceFromCamera);

        // Draw follow threshold visualization
        Gizmos.color = Color.red;
        Vector3 thresholdLeft = Quaternion.Euler(0, -followThreshold, 0) * transform.forward;
        Vector3 thresholdRight = Quaternion.Euler(0, followThreshold, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, thresholdLeft * 0.5f);
        Gizmos.DrawRay(transform.position, thresholdRight * 0.5f);
    }

    #region Inspector Helpers

    [ContextMenu("Snap to Camera")]
    private void ContextSnapToCamera()
    {
        SnapToCamera();
    }

    [ContextMenu("Show Current Settings")]
    private void ShowCurrentSettings()
    {
        Debug.Log($"[FilmHUDFollower] Current Settings:\n" +
                  $"  Active Camera: {(activeCamera != null ? activeCamera.name : "None")}\n" +
                  $"  Distance: {distanceFromCamera}m\n" +
                  $"  Vertical Angle: {verticalAngle}°\n" +
                  $"  Follow Threshold: {followThreshold}°\n" +
                  $"  Follow Speed: {followSpeed}\n" +
                  $"  Smooth Follow: {smoothFollow}\n" +
                  $"  Horizontal Only: {horizontalFollowOnly}");
    }

    #endregion
}