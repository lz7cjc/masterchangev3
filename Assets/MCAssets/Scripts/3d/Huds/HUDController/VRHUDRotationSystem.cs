using UnityEngine;

/// <summary>
/// SIMPLIFIED VRHUDRotationSystem - Horizontal rotation ONLY, no vertical tilt
/// Keeps HUD at constant -10° downward angle for optimal viewing
/// </summary>
public class VRHUDRotationSystem : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform playerBody;
    [SerializeField] private bool autoFindActiveCamera = true;

    [Header("Camera Switching Support")]
    [SerializeField] private Transform vrCamera; // Main CameraVR
    [SerializeField] private Transform camera2D; // Main Camera360  
    [SerializeField] private bool supportCameraSwitching = true;

    [Header("Rotation Settings")]
    [Tooltip("Constant downward tilt angle for comfortable viewing (negative = down)")]
    [SerializeField] private float fixedXTilt = -10f; // HUD tilts down for better visibility

    [Tooltip("How fast HUD rotates horizontally to follow camera")]
    [SerializeField] private float rotationSpeed = 2f;

    [SerializeField] private bool smoothRotation = true;

    [Tooltip("Minimum horizontal rotation (degrees) before HUD starts following")]
    [SerializeField] private float rotationThreshold = 120f;

    [Header("Advanced Options")]
    [SerializeField] private bool instantRotationOnLargeAngles = true;
    [SerializeField] private float instantRotationThreshold = 120f;
    [SerializeField] private bool debugMode = false;

    // Private state
    private Vector3 lastForwardDirection;
    private Quaternion targetRotation;
    private Transform currentActiveCamera;
    private bool isInitialized = false;

    private void Start()
    {
        InitializeHUDRotationSystem();
    }

    private void InitializeHUDRotationSystem()
    {
        if (autoFindActiveCamera)
        {
            FindActiveCamera();
        }
        else if (playerCamera == null)
        {
            Debug.LogWarning("VRHUDRotationSystem: No camera assigned and auto-find disabled!");
            return;
        }

        currentActiveCamera = playerCamera;

        // Initialize with current horizontal direction only
        lastForwardDirection = GetHorizontalDirection();

        // Set initial rotation with fixed tilt
        float initialY = transform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(fixedXTilt, initialY, 0);

        isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"VRHUDRotationSystem initialized - Fixed tilt: {fixedXTilt}°, Initial Y rotation: {initialY:F1}°");
        }
    }

    private void FindActiveCamera()
    {
        if (supportCameraSwitching)
        {
            // Check VR camera first
            if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
            {
                Camera vrCam = vrCamera.GetComponent<Camera>();
                if (vrCam != null && vrCam.enabled)
                {
                    playerCamera = vrCamera;
                    if (debugMode) Debug.Log("VRHUDRotationSystem: Using VR Camera");
                    return;
                }
            }

            // Check 2D/360 camera
            if (camera2D != null && camera2D.gameObject.activeInHierarchy)
            {
                Camera cam2d = camera2D.GetComponent<Camera>();
                if (cam2d != null && cam2d.enabled)
                {
                    playerCamera = camera2D;
                    if (debugMode) Debug.Log("VRHUDRotationSystem: Using 360 Camera");
                    return;
                }
            }
        }

        // Fallback to finding any active camera
        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            playerCamera = activeCamera.transform;
            if (debugMode) Debug.Log("VRHUDRotationSystem: Using Main Camera as fallback");
        }
        else
        {
            Debug.LogError("VRHUDRotationSystem: No active camera found!");
        }
    }

    private void Update()
    {
        if (!isInitialized || currentActiveCamera == null) return;

        // Check for camera switches
        if (supportCameraSwitching)
        {
            CheckForCameraSwitch();
        }

        // Update HUD rotation (horizontal only)
        UpdateHUDRotation();
    }

    private void CheckForCameraSwitch()
    {
        Transform newActiveCamera = null;

        // Check VR camera
        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            Camera vrCam = vrCamera.GetComponent<Camera>();
            if (vrCam != null && vrCam.enabled)
            {
                newActiveCamera = vrCamera;
            }
        }

        // Check 360 camera if VR not active
        if (newActiveCamera == null && camera2D != null && camera2D.gameObject.activeInHierarchy)
        {
            Camera cam2d = camera2D.GetComponent<Camera>();
            if (cam2d != null && cam2d.enabled)
            {
                newActiveCamera = camera2D;
            }
        }

        // Switch camera if changed
        if (newActiveCamera != null && newActiveCamera != currentActiveCamera)
        {
            if (debugMode)
            {
                Debug.Log($"VRHUDRotationSystem: Camera switched from {currentActiveCamera.name} to {newActiveCamera.name}");
            }

            currentActiveCamera = newActiveCamera;
            playerCamera = newActiveCamera;
            lastForwardDirection = GetHorizontalDirection();
        }
    }

    private void UpdateHUDRotation()
    {
        Vector3 currentDirection = GetHorizontalDirection();
        float angleDifference = Vector3.Angle(lastForwardDirection, currentDirection);

        // Don't rotate for small movements
        if (angleDifference < rotationThreshold) return;

        // Calculate target rotation (horizontal only with fixed tilt)
        CalculateTargetRotation(currentDirection);

        // Apply rotation (smooth or instant based on settings)
        if (instantRotationOnLargeAngles && angleDifference > instantRotationThreshold)
        {
            // Instant rotation for large angle changes
            transform.rotation = targetRotation;
            lastForwardDirection = currentDirection;

            if (debugMode)
            {
                Debug.Log($"Instant HUD rotation - Angle: {angleDifference:F1}°, New Y rotation: {transform.eulerAngles.y:F1}°");
            }
        }
        else if (smoothRotation)
        {
            // Smooth rotation using Euler angles to prevent unwanted roll (Z-axis rotation)
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;

            // Interpolate each axis independently
            float newX = Mathf.LerpAngle(currentEuler.x, targetEuler.x, rotationSpeed * Time.deltaTime);
            float newY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotationSpeed * Time.deltaTime);
            float newZ = 0; // ALWAYS zero - no roll allowed!

            transform.rotation = Quaternion.Euler(newX, newY, newZ);

            // Update last direction when close to target
            if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
            {
                lastForwardDirection = currentDirection;
            }

            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"Smooth HUD rotation - X: {newX:F1}°, Y: {newY:F1}°, Z: {newZ}° (forced)");
            }
        }
        else
        {
            // Instant rotation
            transform.rotation = targetRotation;
            lastForwardDirection = currentDirection;
        }
    }

    /// <summary>
    /// Gets camera's forward direction projected onto horizontal plane (Y=0)
    /// This ensures we ONLY track horizontal rotation
    /// </summary>
    private Vector3 GetHorizontalDirection()
    {
        Vector3 direction;

        if (playerBody != null)
        {
            direction = playerBody.forward;
        }
        else if (currentActiveCamera != null)
        {
            direction = currentActiveCamera.forward;
        }
        else
        {
            direction = Vector3.forward;
        }

        // CRITICAL: Force Y to zero to get horizontal direction only
        direction.y = 0;
        direction.Normalize();

        return direction;
    }

    /// <summary>
    /// Calculates target rotation: fixed X tilt + horizontal Y rotation only
    /// Z rotation is always 0 (no roll)
    /// </summary>
    private void CalculateTargetRotation(Vector3 horizontalDirection)
    {
        // Calculate Y rotation from horizontal direction
        float yRotation = Mathf.Atan2(horizontalDirection.x, horizontalDirection.z) * Mathf.Rad2Deg;

        // FIXED ROTATION: X = constant tilt, Y = follow camera horizontally, Z = always 0
        targetRotation = Quaternion.Euler(fixedXTilt, yRotation, 0);

        if (debugMode && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Target rotation calculated - X: {fixedXTilt}° (fixed), Y: {yRotation:F1}° (horizontal), Z: 0°");
        }
    }

    /// <summary>
    /// Public method to manually adjust the fixed tilt angle
    /// </summary>
    public void SetFixedTilt(float tiltAngle)
    {
        fixedXTilt = tiltAngle;

        // Recalculate target rotation with new tilt
        Vector3 currentDirection = GetHorizontalDirection();
        CalculateTargetRotation(currentDirection);

        // Apply immediately
        transform.rotation = targetRotation;

        Debug.Log($"HUD tilt updated to {fixedXTilt}°");
    }

    /// <summary>
    /// Get current horizontal rotation angle
    /// </summary>
    public float GetCurrentYRotation()
    {
        return transform.eulerAngles.y;
    }

    private void OnValidate()
    {
        // Clamp fixed tilt to reasonable range
        fixedXTilt = Mathf.Clamp(fixedXTilt, -45f, 45f);

        // Ensure thresholds are positive
        rotationThreshold = Mathf.Max(0, rotationThreshold);
        instantRotationThreshold = Mathf.Max(rotationThreshold, instantRotationThreshold);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || currentActiveCamera == null) return;

        // Draw line showing HUD forward direction
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        // Draw line showing camera horizontal direction
        Vector3 cameraHorizontal = GetHorizontalDirection();
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentActiveCamera.position, currentActiveCamera.position + cameraHorizontal * 2f);

        // Draw sphere at HUD position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);
    }
}