using UnityEngine;

/// <summary>
/// Makes HUD always face the camera without tilting
/// Replaces the complex VRHUDRotationSystem
/// </summary>
public class SimpleHUDFaceCamera : MonoBehaviour
{
    [Header("Camera Reference")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool lockVerticalRotation = true;
    [SerializeField] private float yRotationOffset = 180f; // Flip to curve inward

    void Start()
    {
        if (autoFindCamera && targetCamera == null)
        {
            // Try to find Main CameraVR
            GameObject camVR = GameObject.Find("Main CameraVR");
            if (camVR != null)
            {
                targetCamera = camVR.GetComponent<Camera>();
            }

            // Fallback to Camera.main
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        if (targetCamera == null)
        {
            Debug.LogError("[SimpleHUDFaceCamera] No camera found!");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;

        // HUD STAYS IN PLACE - only rotates to face camera
        Vector3 directionToCamera = targetCamera.transform.position - transform.position;

        if (lockVerticalRotation)
        {
            // Keep HUD level (no tilt), only rotate on Y axis
            directionToCamera.y = 0;
        }

        if (directionToCamera != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);

            // Apply Y rotation offset (180 to flip inward)
            targetRotation *= Quaternion.Euler(0, yRotationOffset, 0);

            transform.rotation = targetRotation;
        }
    }

    // Draw gizmo to show HUD position
    void OnDrawGizmos()
    {
        if (targetCamera == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(targetCamera.transform.position, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}