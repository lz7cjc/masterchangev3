using UnityEngine;

/// <summary>
/// Fixes HUD being too close to camera near clip plane
/// Ensures HUD is always at a safe distance
/// Attach to HUD GameObject
/// </summary>
public class HUDNearClipFix : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera camera360;
    [SerializeField] private Camera cameraVR;
    [SerializeField] private bool autoFindCameras = true;

    [Header("Distance Settings")]
    [SerializeField] private float minDistanceFromCamera = 1.5f;
    [SerializeField] private float preferredDistance = 2f;
    [SerializeField] private bool maintainRelativePosition = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private Camera currentCamera;
    private Vector3 originalLocalPosition;
    private bool hasFixedPosition = false;

    void Start()
    {
        originalLocalPosition = transform.localPosition;

        if (autoFindCameras)
        {
            if (camera360 == null)
            {
                GameObject cam360 = GameObject.Find("Main Camera360");
                if (cam360 != null) camera360 = cam360.GetComponent<Camera>();
            }

            if (cameraVR == null)
            {
                GameObject camVR = GameObject.Find("Main CameraVR");
                if (camVR != null) cameraVR = camVR.GetComponent<Camera>();
            }
        }

        UpdateActiveCamera();
        CheckAndFixHUDPosition();

        Log("=== HUDNearClipFix Initialized ===");
        if (currentCamera != null)
        {
            Log($"Active camera: {currentCamera.name}");
            Log($"Near clip plane: {currentCamera.nearClipPlane}");
            Log($"Minimum safe distance: {minDistanceFromCamera}");
        }
    }

    void Update()
    {
        UpdateActiveCamera();

        if (!hasFixedPosition)
        {
            CheckAndFixHUDPosition();
        }
    }

    private void UpdateActiveCamera()
    {
        Camera newCamera = null;

        if (camera360 != null && camera360.gameObject.activeInHierarchy && camera360.enabled)
        {
            newCamera = camera360;
        }
        else if (cameraVR != null && cameraVR.gameObject.activeInHierarchy && cameraVR.enabled)
        {
            newCamera = cameraVR;
        }

        if (newCamera != currentCamera && newCamera != null)
        {
            currentCamera = newCamera;
            hasFixedPosition = false;
            Log($"Camera switched to: {currentCamera.name}");
        }
    }

    [ContextMenu("Check HUD Position")]
    public void CheckAndFixHUDPosition()
    {
        if (currentCamera == null)
        {
            LogWarning("No active camera found!");
            return;
        }

        float nearClip = currentCamera.nearClipPlane;
        float currentDistance = Vector3.Distance(transform.position, currentCamera.transform.position);

        Log($"Current HUD distance: {currentDistance:F3} units");
        Log($"Camera near clip: {nearClip:F3} units");

        if (currentDistance < minDistanceFromCamera)
        {
            LogWarning($"⚠️ HUD TOO CLOSE! {currentDistance:F3} < {minDistanceFromCamera:F3}");
            LogWarning("HUD is being clipped by camera!");
            FixHUDPosition();
        }
        else if (currentDistance < nearClip)
        {
            LogWarning($"⚠️ HUD INSIDE NEAR CLIP! {currentDistance:F3} < {nearClip:F3}");
            LogWarning("HUD is invisible!");
            FixHUDPosition();
        }
        else
        {
            Log($"✓ HUD position OK ({currentDistance:F3} units)");
            hasFixedPosition = true;
        }
    }

    private void FixHUDPosition()
    {
        if (currentCamera == null) return;

        Log("Fixing HUD position...");

        if (maintainRelativePosition)
        {
            Vector3 directionFromCamera = (transform.position - currentCamera.transform.position).normalized;

            if (directionFromCamera == Vector3.zero)
            {
                directionFromCamera = currentCamera.transform.forward;
            }

            Vector3 newPosition = currentCamera.transform.position + (directionFromCamera * preferredDistance);
            transform.position = newPosition;

            Log($"✓ Moved HUD to {preferredDistance:F2} units from camera");
        }
        else
        {
            Vector3 newPosition = currentCamera.transform.position + (currentCamera.transform.forward * preferredDistance);
            transform.position = newPosition;

            Log($"✓ Positioned HUD {preferredDistance:F2} units in front of camera");
        }

        float newDistance = Vector3.Distance(transform.position, currentCamera.transform.position);
        Log($"  New distance: {newDistance:F3} units");

        hasFixedPosition = true;
    }

    [ContextMenu("Force Fix Now")]
    public void ForceFixNow()
    {
        hasFixedPosition = false;
        UpdateActiveCamera();
        CheckAndFixHUDPosition();
    }

    [ContextMenu("Reset to Original Position")]
    public void ResetToOriginalPosition()
    {
        transform.localPosition = originalLocalPosition;
        hasFixedPosition = false;
        Log("Reset to original local position");
        CheckAndFixHUDPosition();
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[HUDNearClipFix] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[HUDNearClipFix] {message}");
    }

    void OnDrawGizmos()
    {
        if (currentCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentCamera.transform.position, transform.position);

        float distance = Vector3.Distance(transform.position, currentCamera.transform.position);
        Gizmos.color = distance >= minDistanceFromCamera ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentCamera.transform.position, currentCamera.nearClipPlane);
    }
}