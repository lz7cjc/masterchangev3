using UnityEngine;

/// <summary>
/// HUDFollowerWithHoverFreeze - Final solution
/// - Forces HUD to follow camera horizontally ONLY
/// - Freezes rotation when reticle hovers over HUD
/// - Uses LateUpdate to override any other scripts
/// </summary>
public class HUDFollowerWithHoverFreeze : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Transform camera360;

    [Header("HUD Angle")]
    [SerializeField] private float fixedXTilt = -10f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float followThreshold = 120f;

    [Header("Hover Freeze Settings")]
    [Tooltip("HUD won't rotate when reticle is hovering over it")]
    [SerializeField] private bool freezeOnHover = true;

    [Tooltip("Assign the HUDCurved GameObject here")]
    [SerializeField] private GameObject hudCurvedObject;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Internal state
    private Transform activeCamera;
    private float lastCameraYRotation;
    private float targetYRotation;
    private bool isHovering = false;
    private BoxCollider hoverDetectionCollider;

    void Start()
    {
        FindActiveCamera();

        if (activeCamera == null)
        {
            Debug.LogError("HUDFollowerWithHoverFreeze: No active camera found!");
            enabled = false;
            return;
        }

        // Initialize rotation to match camera
        lastCameraYRotation = activeCamera.eulerAngles.y;
        targetYRotation = lastCameraYRotation;
        transform.rotation = Quaternion.Euler(fixedXTilt, lastCameraYRotation, 0);

        // Setup hover detection
        if (freezeOnHover)
        {
            SetupHoverDetection();
        }

        if (showDebugInfo)
        {
            Debug.Log($"HUDFollowerWithHoverFreeze initialized - Tilt: {fixedXTilt}°, Y: {lastCameraYRotation:F1}°");
        }
    }

    void SetupHoverDetection()
    {
        // Add collider to HUDCurved for hover detection
        if (hudCurvedObject == null)
        {
            hudCurvedObject = transform.Find("HUDCurved")?.gameObject;
        }

        if (hudCurvedObject != null)
        {
            // Check if collider already exists
            hoverDetectionCollider = hudCurvedObject.GetComponent<BoxCollider>();

            if (hoverDetectionCollider == null)
            {
                // Add new collider
                hoverDetectionCollider = hudCurvedObject.AddComponent<BoxCollider>();
                hoverDetectionCollider.isTrigger = true;

                // Size it to cover the HUD area (adjust as needed)
                hoverDetectionCollider.center = new Vector3(0, 0, 5);
                hoverDetectionCollider.size = new Vector3(20, 5, 1);

                if (showDebugInfo)
                {
                    Debug.Log("Added hover detection collider to HUDCurved");
                }
            }

            // Add hover detector script
            HUDHoverDetector detector = hudCurvedObject.GetComponent<HUDHoverDetector>();
            if (detector == null)
            {
                detector = hudCurvedObject.AddComponent<HUDHoverDetector>();
            }
            detector.Initialize(this);
        }
        else
        {
            Debug.LogWarning("HUDCurved object not found - hover freeze disabled");
        }
    }

    void Update()
    {
        if (activeCamera == null) return;

        CheckCameraSwitch();

        // Only update rotation if NOT hovering
        if (!isHovering || !freezeOnHover)
        {
            UpdateHUDRotation();
        }
    }

    // Use LateUpdate to FORCE rotation after everything else
    void LateUpdate()
    {
        if (activeCamera == null) return;

        // FORCE the rotation - nothing can override this
        Vector3 currentEuler = transform.eulerAngles;

        // Only update Y if not hovering
        float finalY = isHovering && freezeOnHover ? currentEuler.y : targetYRotation;

        // FORCE X and Z to correct values
        transform.rotation = Quaternion.Euler(fixedXTilt, finalY, 0);

        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"HUD Forced: X={fixedXTilt}°, Y={finalY:F1}°, Z=0° | Hovering={isHovering}");
        }
    }

    void FindActiveCamera()
    {
        // Check VR camera
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

        // Fallback
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            activeCamera = mainCam.transform;
        }
    }

    void CheckCameraSwitch()
    {
        Transform previousCamera = activeCamera;
        FindActiveCamera();

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

    void UpdateHUDRotation()
    {
        float currentCameraY = activeCamera.eulerAngles.y;
        float rotationDelta = Mathf.DeltaAngle(lastCameraYRotation, currentCameraY);

        // Only follow if rotation exceeds threshold
        if (Mathf.Abs(rotationDelta) > followThreshold)
        {
            targetYRotation = currentCameraY;
            lastCameraYRotation = currentCameraY;

            if (showDebugInfo)
            {
                Debug.Log($"HUD following - Delta: {rotationDelta:F1}°, Target Y: {targetYRotation:F1}°");
            }
        }

        // Smooth rotation towards target
        float currentY = transform.eulerAngles.y;
        targetYRotation = Mathf.LerpAngle(currentY, targetYRotation, rotationSpeed * Time.deltaTime);
    }

    // Called by HUDHoverDetector
    public void SetHovering(bool hovering)
    {
        if (isHovering != hovering)
        {
            isHovering = hovering;

            if (showDebugInfo)
            {
                Debug.Log($"HUD hover: {(hovering ? "STARTED - Rotation FROZEN" : "ENDED - Rotation ACTIVE")}");
            }
        }
    }

    public void SetTilt(float tiltAngle)
    {
        fixedXTilt = Mathf.Clamp(tiltAngle, -45f, 45f);
    }

    void OnValidate()
    {
        fixedXTilt = Mathf.Clamp(fixedXTilt, -45f, 45f);
        rotationSpeed = Mathf.Max(0.1f, rotationSpeed);
        followThreshold = Mathf.Clamp(followThreshold, 0f, 180f);
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || activeCamera == null) return;

        // Draw HUD forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);

        // Draw camera direction
        Gizmos.color = Color.yellow;
        Vector3 cameraForward = activeCamera.forward;
        cameraForward.y = 0;
        cameraForward.Normalize();
        Gizmos.DrawLine(activeCamera.position, activeCamera.position + cameraForward * 3f);

        // Draw hover state
        if (isHovering)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}

/// <summary>
/// Helper component to detect when reticle hovers over HUD
/// </summary>
public class HUDHoverDetector : MonoBehaviour
{
    private HUDFollowerWithHoverFreeze hudFollower;
    private int hoverCount = 0;

    public void Initialize(HUDFollowerWithHoverFreeze follower)
    {
        hudFollower = follower;
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if it's the reticle/camera entering
        if (other.CompareTag("MainCamera") || other.name.Contains("Camera"))
        {
            hoverCount++;
            if (hoverCount == 1 && hudFollower != null)
            {
                hudFollower.SetHovering(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MainCamera") || other.name.Contains("Camera"))
        {
            hoverCount--;
            if (hoverCount <= 0 && hudFollower != null)
            {
                hoverCount = 0;
                hudFollower.SetHovering(false);
            }
        }
    }
}