using UnityEngine;

/// <summary>
/// VRHUDBoundingBoxRotation V3 - Local-space oriented bounds for angled HUDs
/// Uses oriented bounding box that follows HUD's rotation
/// </summary>
public class VRHUDBoundingBoxRotation : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private bool autoFindActiveCamera = true;
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Transform camera2D;

    [Header("Local Bounds Settings (Relative to HUD)")]
    [Tooltip("Bounds size in HUD's local space")]
    [SerializeField] private Vector3 boundsSize = new Vector3(2f, 1.5f, 0.5f);
    
    [Tooltip("Bounds center offset in HUD's local space")]
    [SerializeField] private Vector3 boundsCenter = new Vector3(0f, 0f, 0f);
    
    [Tooltip("Extra padding beyond bounds (units)")]
    [SerializeField] private Vector3 boundsPadding = new Vector3(0.3f, 0.3f, 0.3f);

    [Header("Rotation Trigger Settings")]
    [Tooltip("How long user must look away before rotation triggers (seconds)")]
    [SerializeField] [Range(0.5f, 5f)] private float stillnessDelay = 2f;
    
    [Tooltip("Minimum head movement to reset stillness timer (degrees/second)")]
    [SerializeField] [Range(0.1f, 5f)] private float movementThreshold = 1f;

    [Header("Rotation Behavior")]
    [Tooltip("Constant downward tilt angle")]
    [SerializeField] private float fixedXTilt = -10f;
    
    [Tooltip("How fast HUD rotates to new position")]
    [SerializeField] [Range(0.5f, 10f)] private float rotationSpeed = 2f;
    
    [Tooltip("Use smooth rotation vs instant snap")]
    [SerializeField] private bool smoothRotation = true;

    [Header("Visual Debug")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool showBoundsGizmo = true;
    [SerializeField] private Color innerBoundsColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color outerBoundsColor = new Color(0f, 1f, 0f, 0.3f);

    // Private state
    private Transform currentActiveCamera;
    private Vector3 lastCameraForward;
    private float timeOutsideBounds = 0f;
    private bool isRotating = false;
    private Quaternion targetRotation;
    private bool isInitialized = false;
    private Bounds innerBounds;
    private Bounds outerBounds;

    private void Start()
    {
        FindActiveCamera();
        
        if (currentActiveCamera == null)
        {
            Debug.LogError("[VRHUDBoundingBoxRotation] No camera found!");
            enabled = false;
            return;
        }

        // Prevent HUD from falling if it has a Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            if (debugMode) Debug.Log("[VRHUDBoundingBoxRotation] Set Rigidbody to kinematic");
        }

        // Calculate bounds
        RecalculateBounds();

        // Initialize rotation to face camera
        InitializeHUDRotation();

        lastCameraForward = currentActiveCamera.forward;
        isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"[VRHUDBoundingBoxRotation] Initialized - Camera: {currentActiveCamera.name}");
        }
    }

    private void Update()
    {
        if (!isInitialized || currentActiveCamera == null) return;

        // Check for camera switches
        CheckForCameraSwitch();

        // Recalculate bounds in case HUD moved/rotated
        RecalculateBounds();

        // Check if camera is looking outside bounds
        bool isLookingOutsideBounds = IsLookingOutsideBounds();

        // Check if head is moving
        bool isHeadMoving = IsHeadMoving();

        if (isLookingOutsideBounds)
        {
            if (!isHeadMoving)
            {
                // User is still and looking away - increment timer
                timeOutsideBounds += Time.deltaTime;

                if (debugMode && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"[VRHUDBoundingBoxRotation] Outside bounds: {timeOutsideBounds:F1}s / {stillnessDelay:F1}s");
                }

                if (timeOutsideBounds >= stillnessDelay && !isRotating)
                {
                    // Trigger rotation
                    StartRotation();
                }
            }
            else
            {
                // Head is moving - reset timer
                timeOutsideBounds = 0f;
            }
        }
        else
        {
            // Looking at HUD - reset timer
            if (timeOutsideBounds > 0f && debugMode && Time.frameCount % 60 == 0)
            {
                Debug.Log("[VRHUDBoundingBoxRotation] Looking at HUD - timer reset");
            }
            timeOutsideBounds = 0f;
            isRotating = false;
        }

        // Apply rotation if needed
        if (isRotating)
        {
            ApplyRotation();
        }

        // Update last camera forward for next frame
        lastCameraForward = currentActiveCamera.forward;
    }

    private void RecalculateBounds()
    {
        // Inner bounds (interactive area)
        innerBounds = new Bounds(
            transform.TransformPoint(boundsCenter),
            boundsSize
        );

        // Outer bounds (with padding)
        Vector3 paddedSize = boundsSize + boundsPadding * 2f;
        outerBounds = new Bounds(
            transform.TransformPoint(boundsCenter),
            paddedSize
        );
    }

    private void InitializeHUDRotation()
    {
        // Set initial rotation to face camera with fixed tilt
        Vector3 cameraHorizontal = currentActiveCamera.forward;
        cameraHorizontal.y = 0;
        cameraHorizontal.Normalize();

        float yRotation = Mathf.Atan2(cameraHorizontal.x, cameraHorizontal.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(fixedXTilt, yRotation, 0);
        targetRotation = transform.rotation;

        if (debugMode)
        {
            Debug.Log($"[VRHUDBoundingBoxRotation] Initial rotation - Y: {yRotation:F1}°, X: {fixedXTilt}°");
        }
    }

    private void FindActiveCamera()
    {
        if (!autoFindActiveCamera && playerCamera != null)
        {
            currentActiveCamera = playerCamera;
            return;
        }

        // Check VR camera first
        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            Camera vrCam = vrCamera.GetComponent<Camera>();
            if (vrCam != null && vrCam.enabled)
            {
                currentActiveCamera = vrCamera;
                playerCamera = vrCamera;
                if (debugMode) Debug.Log("[VRHUDBoundingBoxRotation] Using VR Camera");
                return;
            }
        }

        // Check 360 camera
        if (camera2D != null && camera2D.gameObject.activeInHierarchy)
        {
            Camera cam2d = camera2D.GetComponent<Camera>();
            if (cam2d != null && cam2d.enabled)
            {
                currentActiveCamera = camera2D;
                playerCamera = camera2D;
                if (debugMode) Debug.Log("[VRHUDBoundingBoxRotation] Using 360 Camera");
                return;
            }
        }

        // Fallback to main camera
        Camera activeCamera = Camera.main;
        if (activeCamera != null)
        {
            currentActiveCamera = activeCamera.transform;
            playerCamera = activeCamera.transform;
            if (debugMode) Debug.Log("[VRHUDBoundingBoxRotation] Using Main Camera");
        }
    }

    private void CheckForCameraSwitch()
    {
        Transform newActiveCamera = null;

        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            Camera vrCam = vrCamera.GetComponent<Camera>();
            if (vrCam != null && vrCam.enabled)
            {
                newActiveCamera = vrCamera;
            }
        }

        if (newActiveCamera == null && camera2D != null && camera2D.gameObject.activeInHierarchy)
        {
            Camera cam2d = camera2D.GetComponent<Camera>();
            if (cam2d != null && cam2d.enabled)
            {
                newActiveCamera = camera2D;
            }
        }

        if (newActiveCamera != null && newActiveCamera != currentActiveCamera)
        {
            if (debugMode)
            {
                Debug.Log($"[VRHUDBoundingBoxRotation] Camera switched: {currentActiveCamera.name} -> {newActiveCamera.name}");
            }
            
            currentActiveCamera = newActiveCamera;
            playerCamera = newActiveCamera;
            lastCameraForward = currentActiveCamera.forward;
            timeOutsideBounds = 0f;
            
            // Re-initialize rotation for new camera
            InitializeHUDRotation();
        }
    }

    private bool IsLookingOutsideBounds()
    {
        // Cast ray from camera
        Ray cameraRay = new Ray(currentActiveCamera.position, currentActiveCamera.forward);
        
        // Check if ray intersects outer bounds (with padding)
        // Using IntersectRay on oriented bounding box
        bool intersectsOuter = IntersectsOrientedBox(cameraRay, outerBounds);
        
        return !intersectsOuter;
    }

    private bool IntersectsOrientedBox(Ray ray, Bounds bounds)
    {
        // Transform ray to HUD's local space for accurate oriented bounds check
        Vector3 localRayOrigin = transform.InverseTransformPoint(ray.origin);
        Vector3 localRayDir = transform.InverseTransformDirection(ray.direction);
        
        // Create local-space bounds
        Bounds localBounds = new Bounds(boundsCenter, bounds.size);
        
        // Check intersection in local space
        return localBounds.IntersectRay(new Ray(localRayOrigin, localRayDir));
    }

    private bool IsHeadMoving()
    {
        // Calculate angular velocity (degrees per second)
        float angle = Vector3.Angle(lastCameraForward, currentActiveCamera.forward);
        float angularVelocity = angle / Time.deltaTime;
        
        return angularVelocity > movementThreshold;
    }

    private void StartRotation()
    {
        isRotating = true;

        // Calculate target rotation (horizontal only)
        Vector3 cameraHorizontal = currentActiveCamera.forward;
        cameraHorizontal.y = 0;
        cameraHorizontal.Normalize();

        float yRotation = Mathf.Atan2(cameraHorizontal.x, cameraHorizontal.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(fixedXTilt, yRotation, 0);

        if (debugMode)
        {
            Debug.Log($"[VRHUDBoundingBoxRotation] Rotation triggered - Current Y: {transform.eulerAngles.y:F1}° → Target Y: {yRotation:F1}°");
        }
    }

    private void ApplyRotation()
    {
        if (smoothRotation)
        {
            // Smooth rotation using Euler angles to prevent roll
            Vector3 currentEuler = transform.eulerAngles;
            Vector3 targetEuler = targetRotation.eulerAngles;

            float newX = Mathf.LerpAngle(currentEuler.x, targetEuler.x, rotationSpeed * Time.deltaTime);
            float newY = Mathf.LerpAngle(currentEuler.y, targetEuler.y, rotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(newX, newY, 0);

            // Stop rotating when close enough
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
                timeOutsideBounds = 0f;
                
                if (debugMode)
                {
                    Debug.Log("[VRHUDBoundingBoxRotation] Rotation complete");
                }
            }
        }
        else
        {
            // Instant rotation
            transform.rotation = targetRotation;
            isRotating = false;
            timeOutsideBounds = 0f;
        }
    }

    private void OnValidate()
    {
        // Clamp values
        fixedXTilt = Mathf.Clamp(fixedXTilt, -45f, 45f);
        stillnessDelay = Mathf.Clamp(stillnessDelay, 0.5f, 5f);
        movementThreshold = Mathf.Clamp(movementThreshold, 0.1f, 5f);
        rotationSpeed = Mathf.Clamp(rotationSpeed, 0.5f, 10f);
        
        // Ensure bounds sizes are positive
        boundsSize.x = Mathf.Max(0.1f, boundsSize.x);
        boundsSize.y = Mathf.Max(0.1f, boundsSize.y);
        boundsSize.z = Mathf.Max(0.1f, boundsSize.z);
        
        boundsPadding.x = Mathf.Max(0f, boundsPadding.x);
        boundsPadding.y = Mathf.Max(0f, boundsPadding.y);
        boundsPadding.z = Mathf.Max(0f, boundsPadding.z);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showBoundsGizmo) return;

        // Draw oriented bounding boxes
        DrawOrientedBox(boundsCenter, boundsSize, innerBoundsColor, "INNER\n(Interactive)");
        
        Vector3 paddedSize = boundsSize + boundsPadding * 2f;
        DrawOrientedBox(boundsCenter, paddedSize, outerBoundsColor, "OUTER\n(Trigger)");

        // Draw camera ray if playing
        if (Application.isPlaying && currentActiveCamera != null)
        {
            bool isOutside = IsLookingOutsideBounds();
            Gizmos.color = isOutside ? Color.red : Color.cyan;
            Gizmos.DrawLine(currentActiveCamera.position, 
                           currentActiveCamera.position + currentActiveCamera.forward * 5f);
            
            // Draw line from camera to bounds center
            Gizmos.color = Color.white;
            Vector3 worldCenter = transform.TransformPoint(boundsCenter);
            Gizmos.DrawLine(currentActiveCamera.position, worldCenter);
        }
    }

    private void DrawOrientedBox(Vector3 localCenter, Vector3 size, Color color, string label)
    {
        // Get world position of center
        Vector3 worldCenter = transform.TransformPoint(localCenter);
        
        // Get the 8 corners of the box in world space
        Vector3[] corners = new Vector3[8];
        Vector3 halfSize = size * 0.5f;
        
        // Local space corners
        Vector3[] localCorners = new Vector3[]
        {
            localCenter + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
            localCenter + new Vector3(halfSize.x, -halfSize.y, -halfSize.z),
            localCenter + new Vector3(halfSize.x, -halfSize.y, halfSize.z),
            localCenter + new Vector3(-halfSize.x, -halfSize.y, halfSize.z),
            localCenter + new Vector3(-halfSize.x, halfSize.y, -halfSize.z),
            localCenter + new Vector3(halfSize.x, halfSize.y, -halfSize.z),
            localCenter + new Vector3(halfSize.x, halfSize.y, halfSize.z),
            localCenter + new Vector3(-halfSize.x, halfSize.y, halfSize.z)
        };
        
        // Transform to world space
        for (int i = 0; i < 8; i++)
        {
            corners[i] = transform.TransformPoint(localCorners[i]);
        }
        
        // Draw wireframe box
        Gizmos.color = color;
        
        // Bottom face
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
        
        // Top face
        Gizmos.DrawLine(corners[4], corners[5]);
        Gizmos.DrawLine(corners[5], corners[6]);
        Gizmos.DrawLine(corners[6], corners[7]);
        Gizmos.DrawLine(corners[7], corners[4]);
        
        // Vertical edges
        Gizmos.DrawLine(corners[0], corners[4]);
        Gizmos.DrawLine(corners[1], corners[5]);
        Gizmos.DrawLine(corners[2], corners[6]);
        Gizmos.DrawLine(corners[3], corners[7]);
        
        // Draw semi-transparent filled box
        Color fillColor = new Color(color.r, color.g, color.b, 0.1f);
        Gizmos.color = fillColor;
        
        // Draw 6 faces as quads (approximated with lines)
        DrawQuad(corners[0], corners[1], corners[2], corners[3]); // Bottom
        DrawQuad(corners[4], corners[5], corners[6], corners[7]); // Top
        DrawQuad(corners[0], corners[1], corners[5], corners[4]); // Front
        DrawQuad(corners[2], corners[3], corners[7], corners[6]); // Back
        DrawQuad(corners[0], corners[3], corners[7], corners[4]); // Left
        DrawQuad(corners[1], corners[2], corners[6], corners[5]); // Right
        
        // Draw label
        #if UNITY_EDITOR
        Vector3 labelPos = worldCenter + transform.up * (size.y * 0.5f + 0.1f);
        string labelText = $"{label}\nSize: {size.x:F2} x {size.y:F2} x {size.z:F2}\nCenter: {localCenter}";
        UnityEditor.Handles.Label(labelPos, labelText);
        #endif
    }

    private void DrawQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        // Draw filled quad using mesh (for better visualization)
        // This is a simple approximation - Unity doesn't have Gizmos.DrawQuad
        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }

    // Public methods
    public void SetBoundsSize(Vector3 size)
    {
        boundsSize = size;
        RecalculateBounds();
    }

    public void SetBoundsCenter(Vector3 center)
    {
        boundsCenter = center;
        RecalculateBounds();
    }

    public void SetBoundsPadding(Vector3 padding)
    {
        boundsPadding = padding;
        RecalculateBounds();
    }

    public bool IsUserLookingAtHUD()
    {
        return !IsLookingOutsideBounds();
    }

    public float GetTimeOutsideBounds()
    {
        return timeOutsideBounds;
    }
}
