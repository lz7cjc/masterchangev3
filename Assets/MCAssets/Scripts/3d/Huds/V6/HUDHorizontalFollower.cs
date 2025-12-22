using UnityEngine;

/// <summary>
/// HUDHorizontalFollower - Makes HUD follow camera HORIZONTALLY ONLY
/// Maintains constant downward tilt for comfortable viewing
/// NO vertical rotation - guaranteed!
/// </summary>
public class HUDHorizontalFollower : MonoBehaviour
{
    [Header("Camera References")]
    [Tooltip("Assign Main CameraVR here")]
    [SerializeField] private Transform vrCamera;

    [Tooltip("Assign Main Camera360 here")]
    [SerializeField] private Transform camera360;

    [Header("HUD Angle")]
    [Tooltip("Constant downward tilt (negative = tilts down towards user)")]
    [SerializeField] private float downwardTilt = -10f;

    [Header("Rotation Behavior")]
    [Tooltip("How smoothly HUD rotates (higher = faster)")]
    [SerializeField] private float rotationSpeed = 3f;

    [Tooltip("Camera must rotate this many degrees before HUD follows")]
    [SerializeField] private float followThreshold = 120f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Internal state
    private Transform activeCamera;
    private float lastCameraYRotation;
    private float targetYRotation;

    void Start()
    {
        // Find which camera is active
        FindActiveCamera();

        if (activeCamera == null)
        {
            Debug.LogError("HUDHorizontalFollower: No active camera found!");
            enabled = false;
            return;
        }

        // Store initial camera Y rotation
        lastCameraYRotation = activeCamera.eulerAngles.y;
        targetYRotation = lastCameraYRotation;

        // Set HUD to initial position: downward tilt + camera's horizontal rotation
        transform.rotation = Quaternion.Euler(downwardTilt, lastCameraYRotation, 0);

        if (showDebugInfo)
        {
            Debug.Log($"HUDHorizontalFollower initialized - Tilt: {downwardTilt}°, Initial Y: {lastCameraYRotation:F1}°");
        }
    }

    void Update()
    {
        // Check if camera switched
        CheckCameraSwitch();

        if (activeCamera == null) return;

        // Get current camera's horizontal rotation (Y-axis only)
        float currentCameraY = activeCamera.eulerAngles.y;

        // Calculate how much camera has rotated
        float rotationDelta = Mathf.DeltaAngle(lastCameraYRotation, currentCameraY);

        // Only update if camera rotated beyond threshold
        if (Mathf.Abs(rotationDelta) > followThreshold)
        {
            // Update target to camera's current Y rotation
            targetYRotation = currentCameraY;
            lastCameraYRotation = currentCameraY;

            if (showDebugInfo)
            {
                Debug.Log($"HUD following camera - Delta: {rotationDelta:F1}°, New target Y: {targetYRotation:F1}°");
            }
        }

        // Smoothly rotate HUD towards target (Y-axis only)
        float currentY = transform.eulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetYRotation, rotationSpeed * Time.deltaTime);

        // Apply rotation: FIXED tilt (X), smooth horizontal (Y), NO roll (Z=0)
        transform.rotation = Quaternion.Euler(downwardTilt, newY, 0);

        // Debug display
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Vector3 rot = transform.eulerAngles;
            Debug.Log($"HUD Rotation - X: {rot.x:F1}° (fixed), Y: {rot.y:F1}° (following), Z: {rot.z:F1}° (should be 0)");
        }
    }

    /// <summary>
    /// Find which camera is currently active
    /// </summary>
    private void FindActiveCamera()
    {
        // Check VR camera first
        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            Camera cam = vrCamera.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                activeCamera = vrCamera;
                if (showDebugInfo) Debug.Log("Using VR Camera");
                return;
            }
        }

        // Check 360 camera
        if (camera360 != null && camera360.gameObject.activeInHierarchy)
        {
            Camera cam = camera360.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                activeCamera = camera360;
                if (showDebugInfo) Debug.Log("Using 360 Camera");
                return;
            }
        }

        // Fallback to Camera.main
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            activeCamera = mainCam.transform;
            if (showDebugInfo) Debug.Log("Using Main Camera (fallback)");
        }
    }

    /// <summary>
    /// Check if active camera changed (VR mode switch)
    /// </summary>
    private void CheckCameraSwitch()
    {
        Transform previousCamera = activeCamera;
        FindActiveCamera();

        // If camera changed, reset tracking
        if (activeCamera != previousCamera && activeCamera != null)
        {
            lastCameraYRotation = activeCamera.eulerAngles.y;
            targetYRotation = lastCameraYRotation;

            if (showDebugInfo)
            {
                Debug.Log($"Camera switched to {activeCamera.name}");
            }
        }
    }

    /// <summary>
    /// Public method to change the tilt angle at runtime
    /// </summary>
    public void SetTilt(float tiltAngle)
    {
        downwardTilt = Mathf.Clamp(tiltAngle, -45f, 45f);
        Debug.Log($"HUD tilt changed to {downwardTilt}°");
    }

    /// <summary>
    /// Force HUD to snap to camera's current rotation immediately
    /// </summary>
    public void SnapToCamera()
    {
        if (activeCamera == null) return;

        float cameraY = activeCamera.eulerAngles.y;
        transform.rotation = Quaternion.Euler(downwardTilt, cameraY, 0);
        lastCameraYRotation = cameraY;
        targetYRotation = cameraY;

        Debug.Log("HUD snapped to camera position");
    }

    // Visual debugging in Scene view
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || activeCamera == null) return;

        // Draw camera's horizontal forward direction (yellow)
        Vector3 cameraForward = activeCamera.forward;
        cameraForward.y = 0; // Flatten to horizontal
        cameraForward.Normalize();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(activeCamera.position, activeCamera.position + cameraForward * 3f);

        // Draw HUD's forward direction (green)
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        // Draw sphere at HUD center
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }

    // Validate settings in Inspector
    void OnValidate()
    {
        downwardTilt = Mathf.Clamp(downwardTilt, -45f, 45f);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        followThreshold = Mathf.Clamp(followThreshold, 0f, 180f);
    }
}