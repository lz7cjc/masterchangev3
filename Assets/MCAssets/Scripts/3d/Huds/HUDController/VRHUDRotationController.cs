using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class VRHUDRotationController : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Transform vrCamera;
    [SerializeField] private Transform camera360;

    [Header("Detection Settings")]
    [SerializeField] private float triggerAngle = 40f;

    [Header("Rotation Trigger")]
    [SerializeField] private float stillnessDelay = 2f;
    [SerializeField] private float movementThreshold = 1000f;

    [Header("HUD Positioning")]
    [SerializeField] private float hudDistance = 3f;
    [Tooltip("Fixed downward tilt")]
    [SerializeField] private float fixedXTilt = -10f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private bool smoothMovement = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Transform currentCamera;
    private BoxCollider boundsCollider;
    private Vector3 lastCameraYaw;
    private float outsideTimer = 0f;
    private bool isRepositioning = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private void Start()
    {
        boundsCollider = GetComponent<BoxCollider>();
        boundsCollider.isTrigger = true;

        FindActiveCamera();

        if (currentCamera == null)
        {
            Debug.LogError("[HUD] No camera found!");
            enabled = false;
            return;
        }

        InitializePosition();
        lastCameraYaw = GetHorizontalForward(currentCamera.forward);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log("╔══════════════════════════════════╗");
        Debug.Log("║  HUD ORBIT CONTROLLER ACTIVE  ║");
        Debug.Log("╚══════════════════════════════════╝");
        Debug.Log($"[HUD] Camera: {currentCamera.name}");
        Debug.Log($"[HUD] Distance: {hudDistance}m");
        Debug.Log($"[HUD] Trigger: {triggerAngle}°");
        Debug.Log($"[HUD] Delay: {stillnessDelay}s");
    }

    private void Update()
    {
        if (currentCamera == null) return;

        float angleToHUD = GetHorizontalAngleToHUD();
        bool lookingAway = angleToHUD > triggerAngle;
        float headSpeed = GetHeadMovementSpeed();
        bool headStill = headSpeed < movementThreshold;

        if (debugLogs && Time.frameCount % 60 == 0)
        {
            string status = lookingAway ? "🔴 OUT" : "🟢 IN";
            Debug.Log($"[HUD] {angleToHUD:F0}°/{triggerAngle:F0}° {status} | Timer: {outsideTimer:F1}s | Moving: {isRepositioning}");
        }

        if (lookingAway && headStill)
        {
            outsideTimer += Time.deltaTime;

            if (outsideTimer >= stillnessDelay && !isRepositioning)
            {
                StartRepositioning();
            }
        }
        else
        {
            if (outsideTimer > 0.5f && debugLogs)
            {
                Debug.Log($"[HUD] Timer reset (was {outsideTimer:F1}s)");
            }
            outsideTimer = 0f;
        }

        if (isRepositioning)
        {
            ApplyRepositioning();
        }

        lastCameraYaw = GetHorizontalForward(currentCamera.forward);
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

    private Vector3 GetHorizontalForward(Vector3 direction)
    {
        Vector3 flat = new Vector3(direction.x, 0, direction.z);
        return flat.magnitude > 0.001f ? flat.normalized : Vector3.forward;
    }

    private float GetHorizontalAngleToHUD()
    {
        Vector3 toHUD = transform.position - currentCamera.position;
        Vector3 toHUDFlat = GetHorizontalForward(toHUD);
        Vector3 cameraFlat = GetHorizontalForward(currentCamera.forward);
        return Vector3.Angle(cameraFlat, toHUDFlat);
    }

    private float GetHeadMovementSpeed()
    {
        Vector3 currentYaw = GetHorizontalForward(currentCamera.forward);
        float angleDelta = Vector3.Angle(lastCameraYaw, currentYaw);
        return angleDelta / Time.deltaTime;
    }

    private void InitializePosition()
    {
        // Position HUD in front of camera at specified distance
        Vector3 forward = GetHorizontalForward(currentCamera.forward);
        Vector3 newPos = currentCamera.position + forward * hudDistance;
        newPos.y = currentCamera.position.y; // Keep same height as camera
        transform.position = newPos;

        // Rotate to face camera
        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(fixedXTilt, yRot, 0);

        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void StartRepositioning()
    {
        Debug.Log("╔════════════════════════════════╗");
        Debug.Log("║   🔄 REPOSITIONING HUD! 🔄    ║");
        Debug.Log("╚════════════════════════════════╝");

        isRepositioning = true;

        // Calculate new position in front of camera
        Vector3 forward = GetHorizontalForward(currentCamera.forward);
        targetPosition = currentCamera.position + forward * hudDistance;
        targetPosition.y = currentCamera.position.y; // Keep same height

        // Calculate rotation to face camera
        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(fixedXTilt, yRot, 0);

        if (debugLogs)
        {
            Debug.Log($"[HUD] Current Pos: {transform.position}");
            Debug.Log($"[HUD] Target Pos: {targetPosition}");
            Debug.Log($"[HUD] Distance to move: {Vector3.Distance(transform.position, targetPosition):F2}m");
        }
    }

    private void ApplyRepositioning()
    {
        if (smoothMovement)
        {
            // Smoothly move position
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Smoothly rotate
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime);

            // Check if close enough to finish
            if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isRepositioning = false;
                outsideTimer = 0f;
                Debug.Log("[HUD] ✅ Repositioning complete!");
            }
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            isRepositioning = false;
            outsideTimer = 0f;
            Debug.Log("[HUD] ✅ Repositioning complete!");
        }
    }
}