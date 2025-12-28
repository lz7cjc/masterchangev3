using UnityEngine;

/// <summary>
/// VRHUDRotationController - Ultimate simple version
/// Just adjust BoxCollider size. Inside = control HUD, Outside = HUD rotates.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class VRHUDRotationController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Transform camera360;

    [Header("Rotation Trigger")]
    [Tooltip("Seconds looking outside BoxCollider before rotating")]
    [SerializeField] private float stillnessDelay = 2f;
    
    [Tooltip("Min head movement (deg/sec) to reset timer")]
    [SerializeField] private float movementThreshold = 1f;

    [Header("Rotation Behavior")]
    [Tooltip("Fixed downward tilt")]
    [SerializeField] private float fixedXTilt = -10f;
    
    [Tooltip("Rotation speed")]
    [SerializeField] private float rotationSpeed = 2f;
    
    [SerializeField] private bool smoothRotation = true;

    [Header("Visualization")]
    [SerializeField] private bool showBoundsInScene = true;
    [SerializeField] private Color boundsColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // State
    private Transform currentCamera;
    private BoxCollider boundsCollider;
    private Vector3 lastCameraForward;
    private float timeOutsideBounds = 0f;
    private bool isRotating = false;
    private Quaternion targetRotation;

    private void Start()
    {
        boundsCollider = GetComponent<BoxCollider>();
        if (boundsCollider == null)
        {
            Debug.LogError("[HUD] ERROR: No BoxCollider found!");
            enabled = false;
            return;
        }
        boundsCollider.isTrigger = true;

        FindActiveCamera();
        
        if (currentCamera == null)
        {
            Debug.LogError("[HUD] ERROR: No camera found!");
            enabled = false;
            return;
        }
        
        InitializeRotation();
        lastCameraForward = currentCamera.forward;

        // Prevent falling
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log($"[HUD] ===== INITIALIZED =====");
        Debug.Log($"[HUD] Camera: {currentCamera.name}");
        Debug.Log($"[HUD] Collider size: {boundsCollider.size}");
        Debug.Log($"[HUD] Collider center (local): {boundsCollider.center}");
        Debug.Log($"[HUD] Bounds center (world): {boundsCollider.bounds.center}");
        Debug.Log($"[HUD] Bounds size (world): {boundsCollider.bounds.size}");
        Debug.Log($"[HUD] HUD position: {transform.position}");
        Debug.Log($"[HUD] Camera position: {currentCamera.position}");
        Debug.Log($"[HUD] Stillness delay: {stillnessDelay}s");
        Debug.Log($"[HUD] Movement threshold: {movementThreshold} deg/s");
        Debug.Log($"[HUD] Debug mode: {debugMode}");
        Debug.Log($"[HUD] ========================");
    }

    private void Update()
    {
        if (currentCamera == null) return;

        CheckCameraSwitch();

        bool outsideBounds = IsLookingOutsideBounds();
        bool headMoving = IsHeadMoving();

        if (debugMode && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"[HUD] Status Check:");
            Debug.Log($"  - Looking outside bounds: {outsideBounds}");
            Debug.Log($"  - Head moving: {headMoving}");
            Debug.Log($"  - Time outside: {timeOutsideBounds:F2}s / {stillnessDelay:F1}s");
            Debug.Log($"  - Is rotating: {isRotating}");
        }

        if (outsideBounds && !headMoving)
        {
            timeOutsideBounds += Time.deltaTime;

            if (debugMode && Time.frameCount % 30 == 0)
            {
                Debug.Log($"[HUD] Outside bounds: {timeOutsideBounds:F1}s / {stillnessDelay:F1}s");
            }

            if (timeOutsideBounds >= stillnessDelay && !isRotating)
            {
                StartRotation();
            }
        }
        else
        {
            if (timeOutsideBounds > 0f && debugMode)
            {
                Debug.Log($"[HUD] Timer reset - was at {timeOutsideBounds:F2}s");
            }
            timeOutsideBounds = 0f;
            isRotating = false;
        }

        if (isRotating)
        {
            ApplyRotation();
        }

        lastCameraForward = currentCamera.forward;
    }

    private void FindActiveCamera()
    {
        if (vrCamera != null && vrCamera.gameObject.activeInHierarchy)
        {
            Camera cam = vrCamera.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                currentCamera = vrCamera;
                return;
            }
        }

        if (camera360 != null && camera360.gameObject.activeInHierarchy)
        {
            Camera cam = camera360.GetComponent<Camera>();
            if (cam != null && cam.enabled)
            {
                currentCamera = camera360;
                return;
            }
        }

        currentCamera = Camera.main?.transform;
    }

    private void CheckCameraSwitch()
    {
        Transform newCam = null;

        if (vrCamera != null && vrCamera.GetComponent<Camera>()?.enabled == true)
            newCam = vrCamera;
        else if (camera360 != null && camera360.GetComponent<Camera>()?.enabled == true)
            newCam = camera360;

        if (newCam != null && newCam != currentCamera)
        {
            currentCamera = newCam;
            InitializeRotation();
            timeOutsideBounds = 0f;
            if (debugMode) Debug.Log($"[HUD] Camera switched to {newCam.name}");
        }
    }

    private void InitializeRotation()
    {
        Vector3 forward = currentCamera.forward;
        forward.y = 0;
        forward.Normalize();

        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(fixedXTilt, yRot, 0);
        targetRotation = transform.rotation;
    }

    private bool IsLookingOutsideBounds()
    {
        Ray ray = new Ray(currentCamera.position, currentCamera.forward);
        bool intersects = boundsCollider.bounds.IntersectRay(ray);
        
        if (debugMode && Time.frameCount % 120 == 0) // Every 2 seconds
        {
            Debug.Log($"[HUD] Bounds Check:");
            Debug.Log($"  - Camera: {currentCamera.name}");
            Debug.Log($"  - Ray origin: {ray.origin}");
            Debug.Log($"  - Ray direction: {ray.direction}");
            Debug.Log($"  - Bounds center: {boundsCollider.bounds.center}");
            Debug.Log($"  - Bounds size: {boundsCollider.bounds.size}");
            Debug.Log($"  - Ray intersects bounds: {intersects}");
            Debug.Log($"  - Looking OUTSIDE: {!intersects}");
        }
        
        return !intersects;
    }

    private bool IsHeadMoving()
    {
        float angle = Vector3.Angle(lastCameraForward, currentCamera.forward);
        return (angle / Time.deltaTime) > movementThreshold;
    }

    private void StartRotation()
    {
        isRotating = true;
        Vector3 forward = currentCamera.forward;
        forward.y = 0;
        forward.Normalize();

        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(fixedXTilt, yRot, 0);

        if (debugMode)
        {
            Debug.Log($"[HUD] ROTATION TRIGGERED!");
            Debug.Log($"[HUD] Current rotation: {transform.eulerAngles.y:F1}°");
            Debug.Log($"[HUD] Target rotation: {yRot:F1}°");
            Debug.Log($"[HUD] Rotation difference: {Mathf.DeltaAngle(transform.eulerAngles.y, yRot):F1}°");
        }
    }

    private void ApplyRotation()
    {
        if (smoothRotation)
        {
            Vector3 current = transform.eulerAngles;
            Vector3 target = targetRotation.eulerAngles;

            float x = Mathf.LerpAngle(current.x, target.x, rotationSpeed * Time.deltaTime);
            float y = Mathf.LerpAngle(current.y, target.y, rotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(x, y, 0);

            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
                timeOutsideBounds = 0f;
                if (debugMode) Debug.Log("[HUD] Rotation complete");
            }
        }
        else
        {
            transform.rotation = targetRotation;
            isRotating = false;
            timeOutsideBounds = 0f;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showBoundsInScene) return;
        if (boundsCollider == null) boundsCollider = GetComponent<BoxCollider>();
        if (boundsCollider == null) return;

        // Draw shaded box that works at any angle
        Gizmos.matrix = transform.localToWorldMatrix;
        
        // Draw wireframe
        Gizmos.color = new Color(boundsColor.r, boundsColor.g, boundsColor.b, 1f);
        Gizmos.DrawWireCube(boundsCollider.center, boundsCollider.size);
        
        // Draw semi-transparent filled box
        Gizmos.color = boundsColor;
        Gizmos.DrawCube(boundsCollider.center, boundsCollider.size);
        
        Gizmos.matrix = Matrix4x4.identity;

        // Draw camera ray during play
        if (Application.isPlaying && currentCamera != null)
        {
            bool outside = IsLookingOutsideBounds();
            Gizmos.color = outside ? Color.red : Color.cyan;
            Gizmos.DrawLine(currentCamera.position, currentCamera.position + currentCamera.forward * 5f);
            
            // Draw rotation indicator
            if (isRotating)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
                
                // Draw current forward direction
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1f);
                
                // Draw target direction
                Gizmos.color = Color.white;
                Vector3 targetForward = targetRotation * Vector3.forward;
                Gizmos.DrawLine(transform.position, transform.position + targetForward * 1f);
            }
        }
    }
}
