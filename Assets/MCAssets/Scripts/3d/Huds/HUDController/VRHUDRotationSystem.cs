using UnityEngine;

/// <summary>
/// Rotates the entire HUD system with the player's view direction
/// Keeps all menus (Level1, Level2, Level3a, Level3b) accessible at all times
/// </summary>
public class VRHUDRotationSystem : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform playerBody; // If you have a separate player body transform
    [SerializeField] private bool autoFindActiveCamera = true; // NEW: Auto-detect active camera

    [Header("Camera Switching Support")]
    [SerializeField] private Transform vrCamera; // Main CameraVR
    [SerializeField] private Transform camera2D; // Main Camera2d  
    [SerializeField] private bool supportCameraSwitching = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool followCameraRotation = true;
    [SerializeField] private bool followPlayerBodyRotation = false; // Alternative if you track body rotation
    [SerializeField] private float rotationSpeed = 3f; // How fast HUD rotates to follow
    [SerializeField] private bool smoothRotation = true;

    [Header("Rotation Constraints")]
    [SerializeField] private bool lockVerticalRotation = true; // Keep HUD level (recommended for VR)
    [SerializeField] private bool onlyRotateOnYAxis = true; // Only horizontal rotation
    [SerializeField] private float rotationThreshold = 15f; // Degrees before HUD starts rotating

    [Header("Positioning")]
    [SerializeField] private bool maintainDistanceFromPlayer = false;
    [SerializeField] private bool preserveEditorPosition = true;
    [SerializeField] private bool onlyRotateYAxis = true; // NEW: Only rotate, don't move position
    [SerializeField] private float hudDistance = 3f;
    [SerializeField] private float hudHeight = 0f;

    [Header("Advanced Options")]
    [SerializeField] private bool instantRotationOnLargeAngles = true;
    [SerializeField] private float instantRotationThreshold = 120f; // Snap if player turns too far
    [SerializeField] private bool debugMode = false;

    private Vector3 lastForwardDirection;
    private Quaternion targetRotation;
    private Vector3 relativeHUDPosition; // NEW: Position relative to camera from editor
    private Transform currentActiveCamera; // NEW: Track which camera is active
    private bool isInitialized = false;

    private void Start()
    {
        InitializeHUDRotationSystem();
    }

    private void InitializeHUDRotationSystem()
    {
        // Find or set the active camera
        FindActiveCamera();

        if (currentActiveCamera == null)
        {
            Debug.LogError("VRHUDRotationSystem: No active camera found! Please assign cameras or enable autoFindActiveCamera.");
            return;
        }

        // Calculate the relative position from camera to HUD (from editor setup)
        relativeHUDPosition = transform.position - currentActiveCamera.position;

        lastForwardDirection = GetTrackingDirection();
        targetRotation = transform.rotation;

        isInitialized = true;

        Debug.Log($"VR HUD Rotation System initialized - HUD relative position to camera: {relativeHUDPosition}");
        Debug.Log($"Active camera: {currentActiveCamera.name}");

        if (debugMode)
        {
            Debug.Log($"HUD world position: {transform.position}");
            Debug.Log($"Camera world position: {currentActiveCamera.position}");
            Debug.Log($"Relative position: {relativeHUDPosition}");
        }
    }

    private void FindActiveCamera()
    {
        if (supportCameraSwitching)
        {
            // Auto-detect which camera is active
            if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
            {
                currentActiveCamera = vrCamera;
                playerCamera = vrCamera;
            }
            else if (camera2D != null && camera2D.gameObject.activeInHierarchy)
            {
                currentActiveCamera = camera2D;
                playerCamera = camera2D;
            }
            else if (autoFindActiveCamera)
            {
                // Fallback to any active camera
                Camera activeCamera = Camera.main ?? FindObjectOfType<Camera>();
                if (activeCamera != null)
                {
                    currentActiveCamera = activeCamera.transform;
                    playerCamera = currentActiveCamera;
                }
            }
        }
        else
        {
            // Use manually assigned camera
            currentActiveCamera = playerCamera;
        }

        Debug.Log($"Active camera detected: {(currentActiveCamera != null ? currentActiveCamera.name : "None")}");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Check if active camera changed (for camera switching)
        if (supportCameraSwitching)
        {
            CheckForCameraSwitch();
        }

        if (currentActiveCamera == null) return;

        // Update HUD position to maintain relative offset from camera
        UpdateHUDRelativePosition();

        // Update rotation to follow camera direction
        UpdateHUDRotation();
    }

    private void UpdateHUDRelativePosition()
    {
        if (onlyRotateYAxis)
        {
            // Maintain relative position but only rotate it around Y axis with camera
            Vector3 rotatedRelativePosition = Quaternion.AngleAxis(currentActiveCamera.eulerAngles.y, Vector3.up) * relativeHUDPosition;
            Vector3 targetPosition = currentActiveCamera.position + rotatedRelativePosition;

            if (smoothRotation)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }

            if (debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log($"HUD relative position updated - Camera Y rotation: {currentActiveCamera.eulerAngles.y:F1}°, HUD position: {transform.position}");
            }
        }
        else
        {
            // Simple relative positioning without rotation compensation
            Vector3 targetPosition = currentActiveCamera.position + relativeHUDPosition;

            if (smoothRotation)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }
        }
    }

    private void CheckForCameraSwitch()
    {
        Transform newActiveCamera = null;

        // Check which camera is currently active
        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            newActiveCamera = vrCamera;
        }
        else if (camera2D != null && camera2D.gameObject.activeInHierarchy)
        {
            newActiveCamera = camera2D;
        }

        // If camera switched, update reference
        if (newActiveCamera != currentActiveCamera && newActiveCamera != null)
        {
            Debug.Log($"Camera switched from {(currentActiveCamera ? currentActiveCamera.name : "None")} to {newActiveCamera.name}");
            currentActiveCamera = newActiveCamera;
            playerCamera = newActiveCamera;
            lastForwardDirection = GetTrackingDirection();
        }
    }

    private void UpdateHUDRotation()
    {
        Vector3 currentDirection = GetTrackingDirection();
        float angleDifference = Vector3.Angle(lastForwardDirection, currentDirection);

        if (angleDifference < rotationThreshold) return; // Don't rotate for small movements

        // Calculate target rotation
        CalculateTargetRotation(currentDirection);

        // Apply rotation (smooth or instant based on settings)
        if (instantRotationOnLargeAngles && angleDifference > instantRotationThreshold)
        {
            // Instant rotation for large angle changes (player did a quick 180°)
            transform.rotation = targetRotation;
            lastForwardDirection = currentDirection;

            if (debugMode)
            {
                Debug.Log($"Instant HUD rotation applied - angle difference: {angleDifference:F1}°");
            }
        }
        else if (smoothRotation)
        {
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Update last direction when we're close to target
            if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
            {
                lastForwardDirection = currentDirection;
            }
        }
        else
        {
            // Direct rotation
            transform.rotation = targetRotation;
            lastForwardDirection = currentDirection;
        }
    }

    private void UpdateHUDPosition()
    {
        // Keep HUD at consistent distance from player
        Vector3 directionFromPlayer = transform.position - currentActiveCamera.position;
        directionFromPlayer.y = 0; // Keep at same height
        directionFromPlayer.Normalize();

        Vector3 targetPosition = currentActiveCamera.position +
                               (directionFromPlayer * hudDistance) +
                               (Vector3.up * hudHeight);

        if (smoothRotation)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, rotationSpeed * Time.deltaTime);
        }
        else
        {
            transform.position = targetPosition;
        }
    }

    private Vector3 GetTrackingDirection()
    {
        Vector3 direction;

        if (followPlayerBodyRotation && playerBody != null)
        {
            direction = playerBody.forward;
        }
        else if (currentActiveCamera != null)
        {
            direction = currentActiveCamera.forward;
        }
        else
        {
            direction = Vector3.forward; // Fallback
        }

        // Lock vertical rotation if enabled
        if (lockVerticalRotation)
        {
            direction.y = 0;
            direction.Normalize();
        }

        return direction;
    }

    private void CalculateTargetRotation(Vector3 direction)
    {
        if (onlyRotateOnYAxis)
        {
            // Only rotate around Y axis (horizontal rotation)
            float yRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            targetRotation = Quaternion.Euler(0, yRotation, 0);
        }
        else
        {
            // Full rotation to match direction
            targetRotation = Quaternion.LookRotation(direction);
        }
    }

    #region Public Methods

    [ContextMenu("Snap HUD to Player View")]
    public void SnapHUDToPlayerView()
    {
        if (currentActiveCamera == null) return;

        Vector3 currentDirection = GetTrackingDirection();
        CalculateTargetRotation(currentDirection);

        transform.rotation = targetRotation;

        // Update position to maintain relative offset
        UpdateHUDRelativePosition();

        lastForwardDirection = currentDirection;

        Debug.Log("HUD snapped to current player view direction maintaining relative position");
    }

    [ContextMenu("Reset HUD to Original Position")]
    public void ResetHUDToOriginalPosition()
    {
        if (currentActiveCamera != null)
        {
            transform.position = currentActiveCamera.position + relativeHUDPosition;
        }
        transform.rotation = Quaternion.identity;

        if (currentActiveCamera != null)
        {
            lastForwardDirection = GetTrackingDirection();
        }

        Debug.Log("HUD reset to relative position from camera");
    }

    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = Mathf.Clamp(newSpeed, 0.1f, 10f);
        Debug.Log($"HUD rotation speed set to: {rotationSpeed}");
    }

    public void EnableSmoothRotation(bool enable)
    {
        smoothRotation = enable;
        Debug.Log($"HUD smooth rotation: {(enable ? "Enabled" : "Disabled")}");
    }

    #endregion

    #region Debug Helpers

    private void OnDrawGizmosSelected()
    {
        if (!debugMode || currentActiveCamera == null) return;

        // Draw debug info in scene view
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, currentActiveCamera.position);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * 2f);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(currentActiveCamera.position, GetTrackingDirection() * 2f);

        // Draw fixed position marker
        Gizmos.color = Color.green;
        Vector3 relativeWorldPos = currentActiveCamera.position + relativeHUDPosition;
        Gizmos.DrawWireSphere(relativeWorldPos, 0.1f);
    }

    [ContextMenu("Toggle Debug Mode")]
    private void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"VR HUD Rotation debug mode: {(debugMode ? "ON" : "OFF")}");
    }

    #endregion
}