using UnityEngine;
using System.Collections;

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

    [Header("Mode-Aware Behavior")]
    [SerializeField] private bool enable360ContinuousFollow = true;
    [Tooltip("In 360 mode, HUD rotates continuously with camera (no delay)")]

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Transform currentCamera;
    private BoxCollider boundsCollider;
    private Vector3 lastCameraYaw;
    private float outsideTimer = 0f;
    private bool isRepositioning = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private int lastKnownToggleValue = -1;

    private void Start()
    {
        // Start initialization after a delay to let cameras activate
        StartCoroutine(DelayedInitialization());
    }

    private IEnumerator DelayedInitialization()
    {
        // Wait for cameras to be activated by StartUp.cs
        yield return new WaitForSeconds(0.5f);

        boundsCollider = GetComponent<BoxCollider>();
        boundsCollider.isTrigger = true;

        FindActiveCamera();

        if (currentCamera == null)
        {
            Debug.LogError("[HUD] No camera found after delay!");
            enabled = false;
            yield break;
        }

        InitializePosition();
        lastCameraYaw = GetHorizontalForward(currentCamera.forward);

        // Detect current mode from PlayerPrefs and cache it
        int toggleValue = PlayerPrefs.GetInt("toggleToVR", 0);
        lastKnownToggleValue = toggleValue;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Debug.Log("╔═══════════════════════════════╗");
        Debug.Log("║  HUD ORBIT CONTROLLER ACTIVE  ║");
        Debug.Log("╚═══════════════════════════════╝");
        Debug.Log($"[HUD] PlayerPrefs toggleToVR = {toggleValue}");
        Debug.Log($"[HUD] Detected Mode: {(toggleValue == 0 ? "360" : "VR")}");
        Debug.Log($"[HUD] Active Camera: {currentCamera.name}");
        Debug.Log($"[HUD] VR Camera Ref: {(vrCamera != null ? vrCamera.name : "NULL")}");
        Debug.Log($"[HUD] 360 Camera Ref: {(camera360 != null ? camera360.name : "NULL")}");
        Debug.Log($"[HUD] Continuous Follow: {enable360ContinuousFollow}");
        Debug.Log($"[HUD] Distance: {hudDistance}m");
        Debug.Log($"[HUD] Runtime mode switching: ENABLED");
    }

    private void Update()
    {
        // Safety: Re-find camera if reference lost
        if (currentCamera == null)
        {
            FindActiveCamera();
            if (currentCamera == null) return;
        }

        // Detect mode and handle changes
        bool is360Mode = DetectCurrentMode();

        // 360 MODE: Continuous follow (no delay, no angle check)
        if (is360Mode && enable360ContinuousFollow)
        {
            ContinuousFollow360();
            lastCameraYaw = GetHorizontalForward(currentCamera.forward);
            return;
        }

        // VR MODE: Original behavior (delay + angle-based repositioning)
        float angleToHUD = GetHorizontalAngleToHUD();
        bool lookingAway = angleToHUD > triggerAngle;
        float headSpeed = GetHeadMovementSpeed();
        bool headStill = headSpeed < movementThreshold;

        if (debugLogs && Time.frameCount % 60 == 0)
        {
            string mode = is360Mode ? "360" : "VR";
            string status = lookingAway ? "🔴 OUT" : "🟢 IN";
            Debug.Log($"[HUD] Mode:{mode} | {angleToHUD:F0}°/{triggerAngle:F0}° {status} | Timer: {outsideTimer:F1}s");
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

    private bool DetectCurrentMode()
    {
        int currentToggleValue = PlayerPrefs.GetInt("toggleToVR", -1);

        // Detect mode change during gameplay
        if (currentToggleValue != lastKnownToggleValue && lastKnownToggleValue != -1)
        {
            Debug.Log($"[HUD] ⚡ MODE CHANGE! {lastKnownToggleValue} → {currentToggleValue}");
            OnModeChanged(currentToggleValue == 0);
        }

        lastKnownToggleValue = currentToggleValue;

        // Return mode: 0 = 360, 1 = VR
        if (currentToggleValue != -1)
        {
            return (currentToggleValue == 0);
        }

        // Fallback: check active camera
        if (camera360 != null && currentCamera == camera360) return true;
        if (vrCamera != null && currentCamera == vrCamera) return false;

        return false; // Default to VR
    }

    private void OnModeChanged(bool newIs360Mode)
    {
        Debug.Log($"[HUD] Switching to {(newIs360Mode ? "360" : "VR")} mode");

        isRepositioning = false;
        outsideTimer = 0f;
        FindActiveCamera();

        targetPosition = transform.position;
        targetRotation = transform.rotation;
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
        // Preserve editor position, only adjust rotation
        Vector3 forward = GetHorizontalForward(currentCamera.forward);
        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(fixedXTilt, yRot, 0);

        targetPosition = transform.position;
        targetRotation = transform.rotation;

        Debug.Log($"[HUD] ✓ Init at position: {transform.position}");
    }

    private void ContinuousFollow360()
    {
        Vector3 forward = GetHorizontalForward(currentCamera.forward);
        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        Quaternion targetRot = Quaternion.Euler(fixedXTilt, yRot, 0);

        Vector3 targetPos = currentCamera.position + forward * hudDistance;
        targetPos.y = transform.position.y; // Maintain height

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, moveSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    private void StartRepositioning()
    {
        Debug.Log("╔════════════════════════════════╗");
        Debug.Log("║   🔄 REPOSITIONING HUD! 🔄    ║");
        Debug.Log("╚════════════════════════════════╝");

        isRepositioning = true;

        Vector3 forward = GetHorizontalForward(currentCamera.forward);
        targetPosition = currentCamera.position + forward * hudDistance;
        targetPosition.y = transform.position.y; // FIXED: Maintain original height

        float yRot = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(fixedXTilt, yRot, 0);

        if (debugLogs)
        {
            Debug.Log($"[HUD] Current: {transform.position}");
            Debug.Log($"[HUD] Target: {targetPosition}");
            Debug.Log($"[HUD] Distance: {Vector3.Distance(transform.position, targetPosition):F2}m");
        }
    }

    private void ApplyRepositioning()
    {
        if (smoothMovement)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                isRepositioning = false;
                outsideTimer = 0f;
                Debug.Log("[HUD] ✅ Reposition complete!");
            }
        }
        else
        {
            transform.position = targetPosition;
            transform.rotation = targetRotation;
            isRepositioning = false;
            outsideTimer = 0f;
            Debug.Log("[HUD] ✅ Reposition complete!");
        }
    }
}