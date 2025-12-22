using UnityEngine;

/// <summary>
/// FIXED VERSION: HUD follows camera with manual position override support.
/// 
/// KEY FIX:
/// - When "Update Position Continuously" is UNCHECKED, the script uses your editor-set position
/// - When CHECKED, it calculates position based on Distance + Vertical Angle
/// - Reticle-based following works in both modes
/// 
/// POSITIONING MODES:
/// 1. MANUAL MODE (Update Position Continuously = OFF):
///    - HUD starts at the exact position you set in the editor
///    - HUD follows rotation when you look away from canvas
///    - Perfect for when you want precise control over HUD placement
/// 
/// 2. AUTO MODE (Update Position Continuously = ON):
///    - HUD calculates position based on Distance + Vertical Angle
///    - HUD continuously updates position (useful for moving player)
///    - HUD follows rotation when you look away from canvas
/// 
/// ATTACH TO: HUDPivot GameObject (parent of HUDCanvas)
/// </summary>
public class VRHUDFollowerWithReticleTrigger : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform camera360;
    [SerializeField] private Transform cameraVR;
    [SerializeField] private bool autoFindCameras = true;

    [Header("Position Settings")]
    [SerializeField] private float distanceFromCamera = 0.8f;
    [SerializeField, Range(-90f, 90f)] private float verticalAngle = 45f;
    [SerializeField] private bool updatePositionContinuously = false;
    [Tooltip("When Update Position Continuously is OFF, use editor position as starting point")]
    [SerializeField] private bool useEditorPosition = true;

    [Header("Rotation Settings")]
    [SerializeField] private bool autoCalculateTilt = true;
    [SerializeField, Range(-90f, 90f)] private float manualTiltAngle = 45f;
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool smoothFollow = true;

    [Header("Canvas Reference")]
    [SerializeField] private RectTransform hudCanvas;
    [SerializeField] private bool autoFindCanvas = true;

    [Header("Reticle Trigger Settings")]
    [SerializeField, Range(0f, 100f)] private float canvasBoundsThreshold = 10f;
    [SerializeField] private bool requireReticleOutside = true;
    [SerializeField] private float reticleCheckDistance = 2f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showReticleBounds = true;

    private Transform activeCamera;
    private VRReticlePointerFixed reticlePointer;
    private RectTransform canvasRectTransform;
    private Canvas canvas;

    private float targetYRotation;
    private bool isInitialized = false;
    private bool isFollowingReticle = false;

    // NEW: Store initial editor position
    private Vector3 editorPosition;
    private Quaternion editorRotation;
    private bool editorPositionStored = false;

    // Cache for canvas bounds in world space
    private Vector3[] canvasWorldCorners = new Vector3[4];

    void Awake()
    {
        // CRITICAL FIX: Store editor position BEFORE any other initialization
        if (useEditorPosition && !updatePositionContinuously)
        {
            editorPosition = transform.position;
            editorRotation = transform.rotation;
            editorPositionStored = true;

            if (showDebug)
            {
                Debug.Log($"<color=cyan>[VRHUDFollower] Stored editor position: {editorPosition}</color>");
                Debug.Log($"<color=cyan>[VRHUDFollower] Stored editor rotation: {editorRotation.eulerAngles}</color>");
            }
        }
    }

    void Start()
    {
        InitializeHUD();
    }

    void Update()
    {
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            if (showDebug && Time.frameCount % 60 == 0)
                Debug.LogWarning("[VRHUDFollower] No active camera found!");
            return;
        }

        // Find reticle pointer if not found yet
        if (reticlePointer == null)
        {
            reticlePointer = activeCamera.GetComponent<VRReticlePointerFixed>();
            if (reticlePointer != null && showDebug)
            {
                Debug.Log("<color=cyan>[VRHUDFollower] Found VRReticlePointerFixed</color>");
            }
        }

        // Find canvas if not found yet
        if (canvasRectTransform == null)
        {
            if (hudCanvas != null)
            {
                canvasRectTransform = hudCanvas;
                canvas = hudCanvas.GetComponent<Canvas>();
            }
            else if (autoFindCanvas)
            {
                Transform canvasTransform = transform.Find("HUDCanvas");
                if (canvasTransform != null)
                {
                    canvasRectTransform = canvasTransform.GetComponent<RectTransform>();
                    canvas = canvasTransform.GetComponent<Canvas>();
                }
            }
        }

        // FIXED: Only update position if continuous mode is enabled
        if (updatePositionContinuously)
        {
            UpdateHUDPositionAndRotation();
        }
        else
        {
            // In manual mode, only update rotation when needed
            if (requireReticleOutside)
            {
                HandleReticleBasedFollow();
            }
            else
            {
                HandleContinuousFollow();
            }
        }
    }

    /// <summary>
    /// FIXED: Set initial HUD position - respects editor position or calculates
    /// </summary>
    private void InitializeHUD()
    {
        UpdateActiveCamera();

        if (activeCamera == null)
        {
            Debug.LogError("[VRHUDFollower] Cannot initialize - no active camera found!");
            return;
        }

        // Find canvas
        if (hudCanvas != null)
        {
            canvasRectTransform = hudCanvas;
            canvas = hudCanvas.GetComponent<Canvas>();
        }
        else if (autoFindCanvas)
        {
            Transform canvasTransform = transform.Find("HUDCanvas");
            if (canvasTransform != null)
            {
                canvasRectTransform = canvasTransform.GetComponent<RectTransform>();
                canvas = canvasTransform.GetComponent<Canvas>();
            }
        }

        if (canvasRectTransform == null)
        {
            Debug.LogWarning("[VRHUDFollower] Canvas not assigned and auto-find failed! Please assign HUDCanvas in inspector.");
        }

        // CRITICAL FIX: Only calculate position if continuous update is enabled
        // OR if editor position mode is disabled
        if (updatePositionContinuously || !useEditorPosition)
        {
            targetYRotation = activeCamera.eulerAngles.y;
            UpdateHUDPositionAndRotation();

            if (showDebug)
            {
                Debug.Log($"<color=cyan>[VRHUDFollower] ✓ Initialized with CALCULATED position</color>");
                Debug.Log($"  Position: {transform.position}");
                Debug.Log($"  Rotation: {transform.eulerAngles}");
            }
        }
        else if (editorPositionStored)
        {
            // Use stored editor position
            transform.position = editorPosition;
            transform.rotation = editorRotation;
            targetYRotation = editorRotation.eulerAngles.y;

            if (showDebug)
            {
                Debug.Log($"<color=green>[VRHUDFollower] ✓ Initialized with EDITOR position</color>");
                Debug.Log($"  Position: {transform.position}");
                Debug.Log($"  Rotation: {transform.eulerAngles}");
            }
        }
        else
        {
            // Fallback: keep current position
            targetYRotation = transform.eulerAngles.y;

            if (showDebug)
            {
                Debug.Log($"<color=yellow>[VRHUDFollower] ✓ Initialized with CURRENT position</color>");
                Debug.Log($"  Position: {transform.position}");
            }
        }

        isInitialized = true;
    }

    /// <summary>
    /// MODIFIED: Calculate and apply HUD position based on camera
    /// Only called when updatePositionContinuously is true
    /// </summary>
    private void UpdateHUDPositionAndRotation()
    {
        if (activeCamera == null) return;

        // Calculate position in orbital fashion
        Vector3 cameraForward = activeCamera.forward;
        Vector3 cameraRight = activeCamera.right;

        // Apply vertical angle
        float angleInRadians = verticalAngle * Mathf.Deg2Rad;
        Vector3 offset = Quaternion.AngleAxis(verticalAngle, cameraRight) * cameraForward;
        offset = offset.normalized * distanceFromCamera;

        // Set position (orbit around camera)
        transform.position = activeCamera.position + offset;

        // Calculate rotation
        Vector3 directionToCamera = (activeCamera.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);

        // Apply tilt
        float tiltAngle = autoCalculateTilt ? -verticalAngle : manualTiltAngle;
        Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        Quaternion finalRotation = lookRotation * tiltRotation;

        // Apply Y-axis rotation separately
        finalRotation = Quaternion.Euler(finalRotation.eulerAngles.x, targetYRotation, finalRotation.eulerAngles.z);

        if (smoothFollow)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * followSpeed);
        }
        else
        {
            transform.rotation = finalRotation;
        }
    }

    /// <summary>
    /// FIXED: Handle rotation following based on reticle position
    /// Only rotates HUD, doesn't change position in manual mode
    /// </summary>
    private void HandleReticleBasedFollow()
    {
        if (reticlePointer == null || canvasRectTransform == null || activeCamera == null)
        {
            return;
        }

        // Get reticle position in world space
        Ray reticleRay = new Ray(activeCamera.position, activeCamera.forward);
        Vector3 reticleWorldPos = reticleRay.GetPoint(reticleCheckDistance);

        // Check if reticle is outside canvas bounds
        float percentageOutside;
        bool isOutside = IsReticleOutsideCanvasBounds(reticleWorldPos, out percentageOutside);

        if (isOutside)
        {
            if (!isFollowingReticle && showDebug)
            {
                Debug.Log($"<color=yellow>[VRHUDFollower] Reticle left canvas - following ({percentageOutside:F1}% outside)</color>");
            }

            isFollowingReticle = true;

            // Update target rotation to face camera
            targetYRotation = Mathf.LerpAngle(targetYRotation, activeCamera.eulerAngles.y, Time.deltaTime * followSpeed);

            // FIXED: In manual position mode, only update rotation
            if (!updatePositionContinuously)
            {
                // Keep position fixed, only rotate to face camera
                Vector3 directionToCamera = (activeCamera.position - transform.position).normalized;
                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);

                // Apply tilt based on current angle to camera
                Vector3 toCamera = activeCamera.position - transform.position;
                float currentVerticalAngle = Mathf.Asin(toCamera.normalized.y) * Mathf.Rad2Deg;
                float tiltAngle = autoCalculateTilt ? -currentVerticalAngle : manualTiltAngle;
                Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
                Quaternion finalRotation = lookRotation * tiltRotation;

                if (smoothFollow)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * followSpeed);
                }
                else
                {
                    transform.rotation = finalRotation;
                }
            }
            else
            {
                // In continuous mode, update both position and rotation
                UpdateHUDPositionAndRotation();
            }
        }
        else
        {
            if (isFollowingReticle && showDebug)
            {
                Debug.Log("<color=green>[VRHUDFollower] Reticle back in canvas - stopped following</color>");
            }

            isFollowingReticle = false;
        }
    }

    /// <summary>
    /// Continuous follow mode - always faces camera
    /// </summary>
    private void HandleContinuousFollow()
    {
        if (activeCamera == null) return;

        targetYRotation = Mathf.LerpAngle(targetYRotation, activeCamera.eulerAngles.y, Time.deltaTime * followSpeed);

        if (!updatePositionContinuously)
        {
            // Manual position mode - only rotate
            Vector3 directionToCamera = (activeCamera.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);

            Vector3 toCamera = activeCamera.position - transform.position;
            float currentVerticalAngle = Mathf.Asin(toCamera.normalized.y) * Mathf.Rad2Deg;
            float tiltAngle = autoCalculateTilt ? -currentVerticalAngle : manualTiltAngle;
            Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0f, 0f);
            Quaternion finalRotation = lookRotation * tiltRotation;

            if (smoothFollow)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * followSpeed);
            }
            else
            {
                transform.rotation = finalRotation;
            }
        }
        else
        {
            // Continuous mode - update both
            UpdateHUDPositionAndRotation();
        }
    }

    /// <summary>
    /// Check if reticle is outside canvas bounds (with threshold)
    /// </summary>
    private bool IsReticleOutsideCanvasBounds(Vector3 reticleWorldPos, out float percentageOutside)
    {
        percentageOutside = 0f;

        if (canvasRectTransform == null) return false;

        // Get canvas corners in world space
        canvasRectTransform.GetWorldCorners(canvasWorldCorners);

        // Project reticle onto canvas plane
        Vector3 canvasNormal = canvasRectTransform.forward;
        Vector3 canvasCenter = canvasRectTransform.position;

        Vector3 toReticle = reticleWorldPos - canvasCenter;
        float distanceToPlane = Vector3.Dot(toReticle, canvasNormal);
        Vector3 projectedReticle = reticleWorldPos - (canvasNormal * distanceToPlane);

        // Get canvas local bounds
        Vector3 localReticle = canvasRectTransform.InverseTransformPoint(projectedReticle);
        Rect canvasRect = canvasRectTransform.rect;

        // Calculate threshold margins
        float thresholdX = canvasRect.width * (canvasBoundsThreshold / 100f);
        float thresholdY = canvasRect.height * (canvasBoundsThreshold / 100f);

        // Active bounds with threshold (INWARD)
        Rect activeRect = new Rect(
            canvasRect.x + thresholdX,
            canvasRect.y + thresholdY,
            canvasRect.width - (thresholdX * 2),
            canvasRect.height - (thresholdY * 2)
        );

        // Check if outside active bounds
        bool isOutside = !activeRect.Contains(new Vector2(localReticle.x, localReticle.y));

        if (showDebug && Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=cyan>[VRHUDFollower Bounds Check]</color>");
            Debug.Log($"  Local Reticle: ({localReticle.x:F2}, {localReticle.y:F2})");
            Debug.Log($"  Active Rect: {activeRect}");
            Debug.Log($"  Is Outside: {isOutside}");
        }

        if (isOutside)
        {
            float xDist = 0f;
            float yDist = 0f;

            if (localReticle.x < activeRect.xMin)
                xDist = (activeRect.xMin - localReticle.x) / canvasRect.width * 100f;
            else if (localReticle.x > activeRect.xMax)
                xDist = (localReticle.x - activeRect.xMax) / canvasRect.width * 100f;

            if (localReticle.y < activeRect.yMin)
                yDist = (activeRect.yMin - localReticle.y) / canvasRect.height * 100f;
            else if (localReticle.y > activeRect.yMax)
                yDist = (localReticle.y - activeRect.yMax) / canvasRect.height * 100f;

            percentageOutside = Mathf.Max(xDist, yDist);
        }

        return isOutside;
    }

    /// <summary>
    /// Determines which camera is currently active
    /// </summary>
    private void UpdateActiveCamera()
    {
        if (autoFindCameras)
        {
            if (camera360 == null)
            {
                GameObject cam360 = GameObject.Find("Main Camera360");
                if (cam360 != null) camera360 = cam360.transform;
            }
            if (cameraVR == null)
            {
                GameObject camVR = GameObject.Find("Main CameraVR");
                if (camVR != null) cameraVR = camVR.transform;
            }
        }

        Transform previousCamera = activeCamera;

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
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                activeCamera = mainCam.transform;
            }
        }

        if (previousCamera != activeCamera && activeCamera != null && isInitialized)
        {
            if (showDebug)
                Debug.Log($"<color=cyan>[VRHUDFollower] Camera switched to: {activeCamera.name}</color>");

            targetYRotation = activeCamera.eulerAngles.y;

            // Only update position if in continuous mode
            if (updatePositionContinuously)
            {
                UpdateHUDPositionAndRotation();
            }
        }
    }

    [ContextMenu("Snap to Camera")]
    public void SnapToCamera()
    {
        // Force re-initialization with calculated position
        bool wasManual = useEditorPosition;
        useEditorPosition = false;
        InitializeHUD();
        useEditorPosition = wasManual;
        Debug.Log("<color=green>[VRHUDFollower] Snapped to camera</color>");
    }

    [ContextMenu("Store Current Position as Editor Position")]
    public void StoreCurrentAsEditorPosition()
    {
        editorPosition = transform.position;
        editorRotation = transform.rotation;
        editorPositionStored = true;
        Debug.Log($"<color=green>[VRHUDFollower] Stored current position: {editorPosition}</color>");
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || activeCamera == null) return;

        // Draw line from camera to HUD
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(activeCamera.position, transform.position);

        // Draw HUD forward direction
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 0.5f);

        // Draw reticle position and canvas bounds
        if (showReticleBounds && canvasRectTransform != null)
        {
            Ray reticleRay = new Ray(activeCamera.position, activeCamera.forward);
            Vector3 reticleWorldPos = reticleRay.GetPoint(reticleCheckDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(reticleWorldPos, 0.02f);

            canvasRectTransform.GetWorldCorners(canvasWorldCorners);
            Gizmos.color = Color.white;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(canvasWorldCorners[i], canvasWorldCorners[(i + 1) % 4]);
            }

            // Draw safe zone
            float thresholdX = canvasRectTransform.rect.width * (canvasBoundsThreshold / 100f);
            float thresholdY = canvasRectTransform.rect.height * (canvasBoundsThreshold / 100f);

            Vector3 right = canvasRectTransform.right;
            Vector3 up = canvasRectTransform.up;

            Vector3[] activeCorners = new Vector3[4];
            activeCorners[0] = canvasWorldCorners[0] + right * thresholdX + up * thresholdY;
            activeCorners[1] = canvasWorldCorners[1] + right * thresholdX - up * thresholdY;
            activeCorners[2] = canvasWorldCorners[2] - right * thresholdX - up * thresholdY;
            activeCorners[3] = canvasWorldCorners[3] - right * thresholdX + up * thresholdY;

            Gizmos.color = isFollowingReticle ? Color.red : Color.green;
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(activeCorners[i], activeCorners[(i + 1) % 4]);
            }
        }
    }
}