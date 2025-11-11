using UnityEngine;

/// <summary>
/// Keeps HUD in front of camera when player rotates beyond threshold.
/// HUD maintains fixed world position and only rotates when threshold exceeded.
/// Attach to HUDPivot (parent of HUDCanvas).
/// </summary>
public class VRHUDFollower : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform camera360;
    [SerializeField] private Transform cameraVR;

    [Header("Position Settings")]
    [SerializeField] private float distanceFromCamera = 1.5f;
    [SerializeField] private bool setPositionOnStart = true;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationThreshold = 15f; // Degrees before HUD rotates
    [SerializeField] private float followSpeed = 8f;
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
            // Set initial position ONCE - based on camera's starting position and orientation
            if (setPositionOnStart)
            {
                Vector3 targetPosition = activeCamera.position + activeCamera.forward * distanceFromCamera;
                transform.position = targetPosition;

                if (showDebug)
                {
                    Debug.Log($"HUD positioned at: {transform.position}");
                }
            }

            // Set initial rotation to match camera
            lastCameraYRotation = activeCamera.eulerAngles.y;
            Vector3 currentRot = transform.eulerAngles;
            currentRot.y = lastCameraYRotation;
            transform.eulerAngles = currentRot;
        }
    }

    void Update()
    {
        UpdateActiveCamera();

        if (activeCamera == null) return;

        // ONLY handle rotation threshold - do NOT update position
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
            Vector3 targetPosition = activeCamera.position + activeCamera.forward * distanceFromCamera;
            transform.position = targetPosition;

            Vector3 currentRot = transform.eulerAngles;
            currentRot.y = activeCamera.eulerAngles.y;
            transform.eulerAngles = currentRot;
            lastCameraYRotation = activeCamera.eulerAngles.y;

            Debug.Log("HUD snapped to camera direction");
        }
    }
}