using UnityEngine;

/// <summary>
/// CRITICAL FIX: Dynamically switches Canvas Event Camera based on active mode.
/// This is ESSENTIAL for UI interaction to work in both 360 and VR modes.
/// 
/// ATTACH TO: HUDCanvas GameObject
/// 
/// SETUP:
/// 1. Assign both Camera360 and CameraVR in Inspector
/// 2. Canvas will automatically update its Event Camera based on which camera is active
/// </summary>
[RequireComponent(typeof(Canvas))]
public class VRCanvasEventCameraSwitcher : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera camera360;
    [SerializeField] private Camera cameraVR;

    [Header("Auto-Find Cameras")]
    [SerializeField] private bool autoFindCameras = true;
    [SerializeField] private string camera360Tag = "MainCamera"; // Change if needed
    [SerializeField] private string cameraVRTag = "MainCamera";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Canvas canvas;
    private Camera lastActiveCamera;

    void Awake()
    {
        canvas = GetComponent<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("[VRCanvasEventCameraSwitcher] No Canvas component found!");
            enabled = false;
            return;
        }

        // Auto-find cameras if not assigned
        if (autoFindCameras)
        {
            FindCameras();
        }

        if (camera360 == null && cameraVR == null)
        {
            Debug.LogError("[VRCanvasEventCameraSwitcher] No cameras assigned! Please assign Camera360 and CameraVR in Inspector.");
            enabled = false;
            return;
        }

        // Set initial camera
        UpdateEventCamera();
    }

    void Update()
    {
        // Continuously check if active camera changed
        UpdateEventCamera();
    }

    private void FindCameras()
    {
        // Try to find cameras by name if not assigned
        if (camera360 == null)
        {
            GameObject cam360Obj = GameObject.Find("Main Camera360");
            if (cam360Obj != null)
            {
                camera360 = cam360Obj.GetComponent<Camera>();
                if (showDebugLogs)
                    Debug.Log($"[VRCanvasEventCameraSwitcher] Auto-found Camera360: {camera360.name}");
            }
        }

        if (cameraVR == null)
        {
            GameObject camVRObj = GameObject.Find("Main CameraVR");
            if (camVRObj != null)
            {
                cameraVR = camVRObj.GetComponent<Camera>();
                if (showDebugLogs)
                    Debug.Log($"[VRCanvasEventCameraSwitcher] Auto-found CameraVR: {cameraVR.name}");
            }
        }

        // Fallback: Find by tag
        if (camera360 == null || cameraVR == null)
        {
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam.name.Contains("360") && camera360 == null)
                {
                    camera360 = cam;
                    if (showDebugLogs)
                        Debug.Log($"[VRCanvasEventCameraSwitcher] Auto-found Camera360 by name: {camera360.name}");
                }
                else if (cam.name.Contains("VR") && cameraVR == null)
                {
                    cameraVR = cam;
                    if (showDebugLogs)
                        Debug.Log($"[VRCanvasEventCameraSwitcher] Auto-found CameraVR by name: {cameraVR.name}");
                }
            }
        }
    }

    private void UpdateEventCamera()
    {
        Camera activeCamera = GetActiveCamera();

        if (activeCamera == null)
        {
            if (showDebugLogs)
                Debug.LogWarning("[VRCanvasEventCameraSwitcher] No active camera found!");
            return;
        }

        // Only update if camera changed
        if (activeCamera != lastActiveCamera)
        {
            canvas.worldCamera = activeCamera;
            lastActiveCamera = activeCamera;

            if (showDebugLogs)
                Debug.Log($"<color=green>[VRCanvasEventCameraSwitcher] ✓ Event Camera switched to: {activeCamera.name}</color>");
        }
    }

    private Camera GetActiveCamera()
    {
        // Check which camera is active and enabled
        if (camera360 != null && camera360.gameObject.activeInHierarchy && camera360.enabled)
        {
            return camera360;
        }
        else if (cameraVR != null && cameraVR.gameObject.activeInHierarchy && cameraVR.enabled)
        {
            return cameraVR;
        }

        // Fallback to Camera.main
        return Camera.main;
    }

    /// <summary>
    /// Manually set the event camera (for testing)
    /// </summary>
    public void SetEventCamera(Camera camera)
    {
        if (camera != null)
        {
            canvas.worldCamera = camera;
            lastActiveCamera = camera;

            if (showDebugLogs)
                Debug.Log($"[VRCanvasEventCameraSwitcher] Manually set Event Camera to: {camera.name}");
        }
    }

    /// <summary>
    /// Check current event camera
    /// </summary>
    [ContextMenu("Show Current Event Camera")]
    public void ShowCurrentEventCamera()
    {
        if (canvas.worldCamera != null)
        {
            Debug.Log($"Current Event Camera: {canvas.worldCamera.name} (Active: {canvas.worldCamera.gameObject.activeInHierarchy})");
        }
        else
        {
            Debug.LogWarning("No Event Camera assigned to Canvas!");
        }
    }
}