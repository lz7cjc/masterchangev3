using UnityEngine;

/// <summary>
/// IMPROVED VERSION: Manages HUD positioning and rotation with proper camera-facing tilt.
/// 
/// KEY FEATURES:
/// - Keeps HUD at fixed distance from camera
/// - Maintains 15° downward tilt so HUD faces camera when looking up
/// - Only rotates HUD when camera turns beyond threshold (keeps HUD stable)
/// - Works in both 360 and VR modes
/// 
/// ATTACH TO: HUDPivot GameObject (parent of HUDCanvas)
/// 
/// HIERARCHY:
/// HUDPivot (this script)
///   └── HUDCanvas (World Space Canvas with VRCanvasEventCameraSwitcher)
///       └── [UI Elements]
/// </summary>
public class VRHUDFollowerImproved : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform camera360;
    [SerializeField] private Transform cameraVR;
    [SerializeField] private bool autoFindCameras = true;

    [Header("Position Settings")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private float heightOffset = 0.2f; // Slightly above camera center
    [SerializeField] private bool updatePositionContinuously = false; // Set true if player moves in world space

    [Header("Rotation Settings")]
    [SerializeField] private float tiltAngle = 15f; // HUD tilts DOWN to face camera when looking up
    [SerializeField] private float rotationThreshold = 20f; // Degrees before HUD rotates
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool smoothFollow = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;

    private Transform activeCamera;
    private float lastCameraYRotation;
    private bool isInitialized = false;

    void Start()
    {
        InitializeHUD();
    }

    void Update()
    {
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.LogWarning("[VRHUDFollower] No active camera found!");
            return;
        }

        // Update position if needed (for moving player)
        if (updatePositionContinuously)
        {
            UpdateHUDPosition();
        }

        // Handle rotation threshold
        HandleRotationFollow();
    }

    /// <summary>
    /// Set initial HUD position and rotation
    /// </summary>
    private void InitializeHUD()
    {
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            Debug.LogError("[VRHUDFollower] Cannot initialize - no active camera found!");
            return;
        }

        // Set initial position
        UpdateHUDPosition();

        // Set initial rotation
        lastCameraYRotation = activeCamera.eulerAngles.y;
        UpdateHUDRotation(lastCameraYRotation);

        isInitialized = true;

        if (showDebug)
        {
            Debug.Log($"<color=cyan>[VRHUDFollower] ✓ Initialized</color>");
            Debug.Log($"  Position: {transform.position}");
            Debug.Log($"  Rotation: {transform.eulerAngles}");
            Debug.Log($"  Distance: {distanceFromCamera}m");
            Debug.Log($"  Tilt: {tiltAngle}°");
        }
    }

    /// <summary>
    /// Update HUD position to stay in front of camera
    /// </summary>
    private void UpdateHUDPosition()
    {
        if (activeCamera == null) return;

        // Calculate position: in front of camera at specified distance
        Vector3 forward = activeCamera.forward;
        Vector3 targetPosition = activeCamera.position + (forward * distanceFromCamera);

        // Add height offset (slightly above camera center)
        targetPosition += Vector3.up * heightOffset;

        transform.position = targetPosition;

        if (showDebug && !isInitialized)
        {
            Debug.Log($"[VRHUDFollower] Position updated: {transform.position}");
        }
    }

    /// <summary>
    /// Handle rotation following with threshold
    /// </summary>
    private void HandleRotationFollow()
    {
        float currentCameraY = activeCamera.eulerAngles.y;
        float angleDiff = Mathf.DeltaAngle(lastCameraYRotation, currentCameraY);

        // Check if rotation threshold exceeded
        if (Mathf.Abs(angleDiff) > rotationThreshold)
        {
            // Camera has rotated beyond threshold - follow it
            if (smoothFollow)
            {
                // Smooth rotation
                float targetY = currentCameraY;
                float currentY = transform.eulerAngles.y;
                float newY = Mathf.LerpAngle(currentY, targetY, followSpeed * Time.deltaTime);

                UpdateHUDRotation(newY);

                // Update last rotation when we're close to target
                if (Mathf.Abs(Mathf.DeltaAngle(newY, targetY)) < 2f)
                {
                    lastCameraYRotation = currentCameraY;
                }
            }
            else
            {
                // Instant rotation
                UpdateHUDRotation(currentCameraY);
                lastCameraYRotation = currentCameraY;
            }

            if (showDebug && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[VRHUDFollower] Following camera rotation - Angle diff: {angleDiff:F1}°");
            }
        }
    }

    /// <summary>
    /// Update HUD rotation with proper tilt
    /// </summary>
    private void UpdateHUDRotation(float yRotation)
    {
        // Apply Y rotation (follow camera) with downward tilt (so HUD faces camera)
        // Negative tilt tilts the top TOWARD the camera (faces camera when looking up)
        Vector3 newRotation = new Vector3(-tiltAngle, yRotation, 0f);
        transform.eulerAngles = newRotation;
    }

    /// <summary>
    /// Determines which camera is currently active
    /// </summary>
    private void UpdateActiveCamera()
    {
        if (autoFindCameras)
        {
            if (camera360 == null)
            {
                GameObject cam360 = GameObject.Find("Main Camera360");
                if (cam360 != null) camera360 = cam360.transform;
            }
            if (cameraVR == null)
            {
                GameObject camVR = GameObject.Find("Main CameraVR");
                if (camVR != null) cameraVR = camVR.transform;
            }
        }

        // Determine active camera
        if (camera360 != null && camera360.gameObject.activeInHierarchy)
        {
            if (activeCamera != camera360)
            {
                activeCamera = camera360;
                if (showDebug)
                    Debug.Log($"<color=cyan>[VRHUDFollower] Switched to Camera360</color>");

                if (isInitialized)
                    InitializeHUD(); // Re-initialize when switching cameras
            }
        }
        else if (cameraVR != null && cameraVR.gameObject.activeInHierarchy)
        {
            if (activeCamera != cameraVR)
            {
                activeCamera = cameraVR;
                if (showDebug)
                    Debug.Log($"<color=cyan>[VRHUDFollower] Switched to CameraVR</color>");

                if (isInitialized)
                    InitializeHUD(); // Re-initialize when switching cameras
            }
        }
        else
        {
            // Fallback to Camera.main
            Camera mainCam = Camera.main;
            if (mainCam != null && activeCamera != mainCam.transform)
            {
                activeCamera = mainCam.transform;
                if (showDebug)
                    Debug.Log($"<color=yellow>[VRHUDFollower] Using Camera.main as fallback</color>");
            }
        }
    }

    /// <summary>
    /// Snap HUD to camera immediately (useful when mode changes)
    /// </summary>
    [ContextMenu("Snap to Camera")]
    public void SnapToCamera()
    {
        InitializeHUD();
        Debug.Log("<color=green>[VRHUDFollower] Snapped to camera</color>");
    }

    /// <summary>
    /// Force HUD to follow camera rotation immediately
    /// </summary>
    [ContextMenu("Force Follow Now")]
    public void ForceFollowNow()
    {
        UpdateActiveCamera();
        if (activeCamera != null)
        {
            UpdateHUDRotation(activeCamera.eulerAngles.y);
            lastCameraYRotation = activeCamera.eulerAngles.y;
            Debug.Log("<color=green>[VRHUDFollower] Forced rotation update</color>");
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || activeCamera == null) return;

        // Draw line from camera to HUD
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(activeCamera.position, transform.position);

        // Draw HUD forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);

        // Draw camera forward direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(activeCamera.position, activeCamera.forward * distanceFromCamera);
    }
}