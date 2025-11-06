using UnityEngine;

/// <summary>
/// Keeps HUD in front of camera when player rotates beyond threshold.
/// Attach to HUDPivot (parent of HUDCanvas).
/// SIMPLE: Just tracks camera Y-axis rotation, no complex relative positioning.
/// </summary>
public class VRHUDFollower : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform camera360;
    [SerializeField] private Transform cameraVR;

    [Header("Follow Settings")]
    [SerializeField] private float rotationThreshold = 15f; // Degrees before HUD moves (lowered from 30)
    [SerializeField] private float followSpeed = 8f; // Increased from 5
    [SerializeField] private bool smoothFollow = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;

    private Transform activeCamera;
    private float lastCameraYRotation;

    void Start()
    {
        UpdateActiveCamera();

        if (activeCamera != null)
        {
            lastCameraYRotation = activeCamera.eulerAngles.y;

            // Set initial HUD rotation to match camera
            Vector3 currentRot = transform.eulerAngles;
            currentRot.y = lastCameraYRotation;
            transform.eulerAngles = currentRot;
        }
    }

    void Update()
    {
        UpdateActiveCamera();

        if (activeCamera == null) return;

        float currentCameraY = activeCamera.eulerAngles.y;
        float angleDiff = Mathf.DeltaAngle(lastCameraYRotation, currentCameraY);

        // Check if rotation threshold exceeded
        if (Mathf.Abs(angleDiff) > rotationThreshold)
        {
            // Camera has rotated beyond threshold - follow it
            Quaternion targetRotation = Quaternion.Euler(0, currentCameraY, 0);

            if (smoothFollow)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    followSpeed * Time.deltaTime
                );

                // Update last rotation when we're close to target
                if (Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, currentCameraY)) < 2f)
                {
                    lastCameraYRotation = currentCameraY;
                }
            }
            else
            {
                transform.rotation = targetRotation;
                lastCameraYRotation = currentCameraY;
            }

            if (showDebug && Time.frameCount % 30 == 0)
            {
                Debug.Log($"HUD following camera - Angle diff: {angleDiff:F1}°");
            }
        }
    }

    /// <summary>
    /// Determines which camera is currently active
    /// </summary>
    private void UpdateActiveCamera()
    {
        if (camera360 != null && camera360.gameObject.activeInHierarchy)
        {
            activeCamera = camera360;
        }
        else if (cameraVR != null && cameraVR.gameObject.activeInHierarchy)
        {
            activeCamera = cameraVR;
        }
        else
        {
            // Fallback to Camera.main
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                activeCamera = mainCam.transform;
            }
        }
    }

    /// <summary>
    /// Snap HUD to current camera direction immediately
    /// </summary>
    [ContextMenu("Snap to Camera")]
    public void SnapToCamera()
    {
        UpdateActiveCamera();
        if (activeCamera != null)
        {
            Vector3 currentRot = transform.eulerAngles;
            currentRot.y = activeCamera.eulerAngles.y;
            transform.eulerAngles = currentRot;
            lastCameraYRotation = activeCamera.eulerAngles.y;
            Debug.Log("HUD snapped to camera direction");
        }
    }
}