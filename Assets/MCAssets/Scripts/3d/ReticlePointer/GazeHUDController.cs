using UnityEngine;

/// <summary>
/// Smart HUD positioning controller for VR
/// - Manual position override for fine-tuning (set once in editor)
/// - Automatic Y-axis only rotation following (no tilt/roll)
/// - Threshold-based camera following
/// - Freezes rotation when reticle hovers over HUD elements
/// - Independent GameObject (not parented to camera or player)
/// </summary>
public class GazeHUDController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("Active camera (auto-detects if not set)")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Position Mode")]
    [Tooltip("Use manual position set in editor (for fine-tuning)")]
    [SerializeField] private bool useManualPosition = true;
    
    [Tooltip("Distance from camera (only used if not using manual position)")]
    [SerializeField] private float distanceFromCamera = 2.5f;
    
    [Tooltip("Height offset from camera (only used if not using manual position)")]
    [SerializeField] private float heightOffset = -0.3f;

    [Header("Rotation Settings")]
    [Tooltip("Degrees camera must rotate before HUD follows")]
    [SerializeField] private float rotationThreshold = 120f;
    
    [Tooltip("How quickly HUD catches up to camera")]
    [SerializeField] private float followSpeed = 3f;
    
    [Tooltip("Minimum angle difference to start rotating")]
    [SerializeField] private float minAngleDifference = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool showGizmos = true;

    // Private state
    private float lastCameraYRotation;
    private float accumulatedRotation;
    private bool isReticleHoveringHUD = false;
    private float targetYRotation;
    private bool isInitialized = false;

    // Manual position storage
    private Vector3 manualPosition;
    private bool manualPositionStored = false;

    void Awake()
    {
        // Store manual position from editor
        if (useManualPosition)
        {
            manualPosition = transform.position;
            manualPositionStored = true;

            if (showDebugInfo)
            {
                Debug.Log($"[GazeHUDController] Manual position stored: {manualPosition}");
            }
        }
    }

    void Start()
    {
        InitializeHUD();
    }

    void LateUpdate()
    {
        if (!isInitialized) return;

        UpdateCameraReference();
        UpdateHUDPosition();
        UpdateHUDRotation();
    }

    /// <summary>
    /// Initialize HUD system
    /// </summary>
    private void InitializeHUD()
    {
        if (autoFindCamera && cameraTransform == null)
        {
            FindActiveCamera();
        }

        if (cameraTransform != null)
        {
            lastCameraYRotation = cameraTransform.eulerAngles.y;
            targetYRotation = lastCameraYRotation;

            // Set initial position
            if (!useManualPosition || !manualPositionStored)
            {
                PositionHUDInFrontOfCamera();
            }
            // If using manual position, keep editor position

            isInitialized = true;

            if (showDebugInfo)
            {
                Debug.Log($"[GazeHUDController] Initialized at position: {transform.position}");
            }
        }
        else
        {
            Debug.LogError("[GazeHUDController] No camera found! Assign camera manually.");
        }
    }

    /// <summary>
    /// Find and set active camera
    /// </summary>
    private void FindActiveCamera()
    {
        // Try to find camera with GazeReticlePointer
        GazeReticlePointer[] reticlePointers = FindObjectsByType<GazeReticlePointer>(FindObjectsSortMode.None);
        
        foreach (GazeReticlePointer pointer in reticlePointers)
        {
            Camera cam = pointer.GetComponent<Camera>();
            if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
            {
                cameraTransform = cam.transform;
                Debug.Log($"[GazeHUDController] Auto-detected camera: {cam.gameObject.name}");
                return;
            }
        }

        // Fallback to main camera
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            Debug.Log("[GazeHUDController] Using Main Camera");
        }
    }

    /// <summary>
    /// Update camera reference (in case of camera switching)
    /// </summary>
    private void UpdateCameraReference()
    {
        if (cameraTransform == null || !cameraTransform.gameObject.activeInHierarchy)
        {
            FindActiveCamera();
        }
    }

    /// <summary>
    /// Position HUD in front of camera
    /// </summary>
    private void PositionHUDInFrontOfCamera()
    {
        if (cameraTransform == null) return;

        // Calculate position in front of camera
        Vector3 forward = cameraTransform.forward;
        forward.y = 0; // Keep horizontal
        forward.Normalize();

        Vector3 targetPosition = cameraTransform.position + 
                                forward * distanceFromCamera + 
                                Vector3.up * heightOffset;

        transform.position = targetPosition;
    }

    /// <summary>
    /// Update HUD position to follow camera
    /// </summary>
    private void UpdateHUDPosition()
    {
        if (cameraTransform == null) return;

        // If using manual position, maintain that distance and offset from camera
        if (useManualPosition && manualPositionStored)
        {
            // Keep HUD at the same relative position to camera
            // Only update Y rotation, maintain manual offset
            Vector3 forward = Quaternion.Euler(0, targetYRotation, 0) * Vector3.forward;
            
            // Calculate offset from camera to manual position
            Vector3 offset = manualPosition - cameraTransform.position;
            float distanceToKeep = offset.magnitude;
            float heightToKeep = offset.y;

            Vector3 targetPosition = cameraTransform.position + 
                                    forward * distanceToKeep + 
                                    Vector3.up * heightToKeep;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }
        else
        {
            // Automatic positioning
            Vector3 forward = Quaternion.Euler(0, targetYRotation, 0) * Vector3.forward;
            Vector3 targetPosition = cameraTransform.position + 
                                    forward * distanceFromCamera + 
                                    Vector3.up * heightOffset;

            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
        }
    }

    /// <summary>
    /// Update HUD rotation with threshold-based following
    /// </summary>
    private void UpdateHUDRotation()
    {
        if (cameraTransform == null) return;

        float currentCameraYRotation = cameraTransform.eulerAngles.y;
        float rotationDelta = Mathf.DeltaAngle(lastCameraYRotation, currentCameraYRotation);

        // Only process if not frozen by reticle hover
        if (!isReticleHoveringHUD)
        {
            // Accumulate rotation
            accumulatedRotation += Mathf.Abs(rotationDelta);

            // Check if threshold exceeded
            if (accumulatedRotation >= rotationThreshold)
            {
                // Catch up to camera
                targetYRotation = currentCameraYRotation;
                accumulatedRotation = 0f;

                if (showDebugInfo)
                {
                    Debug.Log($"[GazeHUDController] Threshold exceeded, catching up");
                }
            }
            else
            {
                // Smooth follow if angle difference significant
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(targetYRotation, currentCameraYRotation));
                
                if (angleDifference > minAngleDifference)
                {
                    targetYRotation = Mathf.LerpAngle(targetYRotation, currentCameraYRotation, 
                                                     Time.deltaTime * followSpeed * 0.5f);
                }
            }
        }

        // Apply rotation (Y-axis only)
        Quaternion targetRotation = Quaternion.Euler(0, targetYRotation, 0);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                                             Time.deltaTime * followSpeed);

        lastCameraYRotation = currentCameraYRotation;
    }

    /// <summary>
    /// Called by GazeReticlePointer when reticle enters HUD
    /// </summary>
    public void OnReticleEnterHUD()
    {
        if (!isReticleHoveringHUD)
        {
            isReticleHoveringHUD = true;

            if (showDebugInfo)
            {
                Debug.Log("[GazeHUDController] Rotation FROZEN (reticle hovering)");
            }
        }
    }

    /// <summary>
    /// Called by GazeReticlePointer when reticle exits HUD
    /// </summary>
    public void OnReticleExitHUD()
    {
        if (isReticleHoveringHUD)
        {
            isReticleHoveringHUD = false;

            if (showDebugInfo)
            {
                Debug.Log("[GazeHUDController] Rotation UNFROZEN");
            }
        }
    }

    /// <summary>
    /// Manually snap HUD to face camera
    /// </summary>
    [ContextMenu("Snap to Camera")]
    public void SnapToCamera()
    {
        if (cameraTransform == null) return;

        targetYRotation = cameraTransform.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        accumulatedRotation = 0f;

        if (!useManualPosition)
        {
            PositionHUDInFrontOfCamera();
        }

        Debug.Log("[GazeHUDController] Snapped to camera");
    }

    /// <summary>
    /// Store current position as manual position
    /// </summary>
    [ContextMenu("Store Current Position")]
    public void StoreCurrentPosition()
    {
        manualPosition = transform.position;
        manualPositionStored = true;
        useManualPosition = true;

        Debug.Log($"[GazeHUDController] Stored manual position: {manualPosition}");
    }

    /// <summary>
    /// Set camera reference manually
    /// </summary>
    public void SetCamera(Transform newCamera)
    {
        cameraTransform = newCamera;
        lastCameraYRotation = cameraTransform.eulerAngles.y;
        targetYRotation = lastCameraYRotation;

        if (!useManualPosition)
        {
            PositionHUDInFrontOfCamera();
        }
    }

    /// <summary>
    /// Check if rotation is frozen
    /// </summary>
    public bool IsRotationFrozen()
    {
        return isReticleHoveringHUD;
    }

    /// <summary>
    /// Get accumulated rotation towards threshold
    /// </summary>
    public float GetAccumulatedRotation()
    {
        return accumulatedRotation;
    }

    /// <summary>
    /// Get threshold progress (0-1)
    /// </summary>
    public float GetThresholdProgress()
    {
        return Mathf.Clamp01(accumulatedRotation / rotationThreshold);
    }

    // Gizmo visualization
    void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Draw HUD position
        Gizmos.color = isReticleHoveringHUD ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw line to camera if available
        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, cameraTransform.position);
        }

        // Draw forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);
    }

    void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;

        // Draw rotation threshold arc
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        
        Vector3 forward = Quaternion.Euler(0, targetYRotation, 0) * Vector3.forward;
        float radius = useManualPosition && manualPositionStored ? 
                      (manualPosition - cameraTransform.position).magnitude : 
                      distanceFromCamera;

        // Draw arc showing threshold
        int segments = 20;
        float angleStep = rotationThreshold / segments;
        Vector3 prevPoint = cameraTransform.position;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = targetYRotation - (rotationThreshold / 2) + (i * angleStep);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 point = cameraTransform.position + dir * radius;
            
            if (i > 0)
            {
                Gizmos.DrawLine(prevPoint, point);
            }
            prevPoint = point;
        }
    }
}
