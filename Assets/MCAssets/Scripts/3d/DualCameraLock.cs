using UnityEngine;

/// <summary>
/// Locks BOTH 360 and VR cameras at sphere center
/// Automatically detects which camera is active based on toggleToVR PlayerPrefs
/// Attach this to your Sphere GameObject
/// </summary>
public class DualCameraLock : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera camera360;  // Main Camera360
    [SerializeField] private Camera cameraVR;   // Main CameraVR

    [Header("Auto-Find Cameras")]
    [Tooltip("If enabled, will search for cameras by name if not assigned")]
    [SerializeField] private bool autoFindCameras = true;
    [SerializeField] private string camera360Name = "Main Camera360";
    [SerializeField] private string cameraVRName = "Main CameraVR";

    [Header("Settings")]
    [Tooltip("Lock camera position every frame")]
    [SerializeField] private bool lockEveryFrame = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Transform sphereTransform;
    private int lastVRMode = -1;

    void Start()
    {
        sphereTransform = transform;

        // Auto-find cameras if not assigned
        if (autoFindCameras)
        {
            if (camera360 == null)
            {
                GameObject cam360Obj = GameObject.Find(camera360Name);
                if (cam360Obj != null)
                {
                    camera360 = cam360Obj.GetComponent<Camera>();
                    if (showDebugLogs)
                        Debug.Log($"[DualCameraLock] Found {camera360Name}");
                }
            }

            if (cameraVR == null)
            {
                GameObject camVRObj = GameObject.Find(cameraVRName);
                if (camVRObj != null)
                {
                    cameraVR = camVRObj.GetComponent<Camera>();
                    if (showDebugLogs)
                        Debug.Log($"[DualCameraLock] Found {cameraVRName}");
                }
            }
        }

        // Validate
        if (camera360 == null)
        {
            Debug.LogError($"[DualCameraLock] {camera360Name} not found! Assign it in Inspector.");
        }

        if (cameraVR == null)
        {
            Debug.LogError($"[DualCameraLock] {cameraVRName} not found! Assign it in Inspector.");
        }

        // Initial lock
        LockBothCameras();

        if (showDebugLogs)
        {
            Debug.Log("[DualCameraLock] ========================================");
            Debug.Log("[DualCameraLock] Dual Camera Lock initialized");
            Debug.Log($"[DualCameraLock] Sphere position: {sphereTransform.position}");
            Debug.Log($"[DualCameraLock] 360 Camera: {(camera360 != null ? "✓" : "✗")}");
            Debug.Log($"[DualCameraLock] VR Camera: {(cameraVR != null ? "✓" : "✗")}");
            Debug.Log($"[DualCameraLock] Current VR Mode: {PlayerPrefs.GetInt("toggleToVR", 0)}");
            Debug.Log("[DualCameraLock] ========================================");
        }
    }

    void Update()
    {
        if (lockEveryFrame)
        {
            LockBothCameras();
        }

        // Detect VR mode changes
        int currentVRMode = PlayerPrefs.GetInt("toggleToVR", 0);
        if (currentVRMode != lastVRMode && lastVRMode != -1)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[DualCameraLock] VR mode changed: {lastVRMode} → {currentVRMode}");
                Debug.Log($"[DualCameraLock] {(currentVRMode == 1 ? "VR" : "360")} mode active");
            }
        }
        lastVRMode = currentVRMode;
    }

    void LateUpdate()
    {
        // Lock again in LateUpdate to ensure nothing moves cameras after other scripts
        if (lockEveryFrame)
        {
            LockBothCameras();
        }
    }

    /// <summary>
    /// Lock both cameras at sphere center
    /// </summary>
    private void LockBothCameras()
    {
        Vector3 spherePosition = sphereTransform.position;

        // Lock 360 camera
        if (camera360 != null && camera360.gameObject.activeInHierarchy)
        {
            if (camera360.transform.position != spherePosition)
            {
                camera360.transform.position = spherePosition;
            }
        }

        // Lock VR camera
        if (cameraVR != null && cameraVR.gameObject.activeInHierarchy)
        {
            if (cameraVR.transform.position != spherePosition)
            {
                cameraVR.transform.position = spherePosition;
            }
        }
    }

    /// <summary>
    /// Manual lock - can be called from other scripts or Inspector
    /// </summary>
    [ContextMenu("Lock Cameras Now")]
    public void LockCamerasManually()
    {
        LockBothCameras();

        if (showDebugLogs)
        {
            Debug.Log("[DualCameraLock] Cameras locked manually");
            PrintCameraPositions();
        }
    }

    /// <summary>
    /// Check camera positions and print status
    /// </summary>
    [ContextMenu("Check Camera Positions")]
    public void PrintCameraPositions()
    {
        Debug.Log("[DualCameraLock] ========================================");
        Debug.Log("[DualCameraLock] Camera Position Status");
        Debug.Log($"[DualCameraLock] Sphere position: {sphereTransform.position}");

        if (camera360 != null)
        {
            float distance360 = Vector3.Distance(sphereTransform.position, camera360.transform.position);
            Debug.Log($"[DualCameraLock] 360 Camera:");
            Debug.Log($"[DualCameraLock]   Position: {camera360.transform.position}");
            Debug.Log($"[DualCameraLock]   Distance: {distance360:F4} units");
            Debug.Log($"[DualCameraLock]   Active: {camera360.gameObject.activeInHierarchy}");
            Debug.Log($"[DualCameraLock]   Status: {(distance360 < 0.01f ? "✓ CENTERED" : "✗ NOT CENTERED")}");
        }
        else
        {
            Debug.LogWarning("[DualCameraLock] 360 Camera not assigned!");
        }

        if (cameraVR != null)
        {
            float distanceVR = Vector3.Distance(sphereTransform.position, cameraVR.transform.position);
            Debug.Log($"[DualCameraLock] VR Camera:");
            Debug.Log($"[DualCameraLock]   Position: {cameraVR.transform.position}");
            Debug.Log($"[DualCameraLock]   Distance: {distanceVR:F4} units");
            Debug.Log($"[DualCameraLock]   Active: {cameraVR.gameObject.activeInHierarchy}");
            Debug.Log($"[DualCameraLock]   Status: {(distanceVR < 0.01f ? "✓ CENTERED" : "✗ NOT CENTERED")}");
        }
        else
        {
            Debug.LogWarning("[DualCameraLock] VR Camera not assigned!");
        }

        int vrMode = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[DualCameraLock] Current mode: {(vrMode == 1 ? "VR" : "360")}");
        Debug.Log("[DualCameraLock] ========================================");
    }

    /// <summary>
    /// Visualize camera positions in Scene view
    /// </summary>
    private void OnDrawGizmos()
    {
        if (sphereTransform == null)
            sphereTransform = transform;

        // Draw sphere center
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sphereTransform.position, 1f);

        // Draw 360 camera
        if (camera360 != null)
        {
            float distance = Vector3.Distance(sphereTransform.position, camera360.transform.position);
            Gizmos.color = distance < 0.1f ? Color.green : Color.red;
            Gizmos.DrawSphere(camera360.transform.position, 0.5f);
            Gizmos.DrawLine(sphereTransform.position, camera360.transform.position);
        }

        // Draw VR camera
        if (cameraVR != null)
        {
            float distance = Vector3.Distance(sphereTransform.position, cameraVR.transform.position);
            Gizmos.color = distance < 0.1f ? Color.cyan : Color.magenta;
            Gizmos.DrawSphere(cameraVR.transform.position, 0.5f);
            Gizmos.DrawLine(sphereTransform.position, cameraVR.transform.position);
        }
    }
}