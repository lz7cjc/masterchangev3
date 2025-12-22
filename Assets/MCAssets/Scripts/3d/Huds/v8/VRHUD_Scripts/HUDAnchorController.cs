using UnityEngine;

/// <summary>
/// Final HUD motion behavior:
/// - Constant downward tilt
/// - Yaw-only follow (no pitch/roll coupling)
/// - Threshold follow (e.g., only follow after 120° yaw difference)
/// - Freeze yaw while reticle is inside HUD bounds
/// Attach to HUDPivot. Assign ActiveCameraProvider + HUDTilt.
/// </summary>
public class HUDAnchorController : MonoBehaviour, IHUDHoverStateSink
{
    [Header("Dependencies")]
    [SerializeField] private ActiveCameraProvider cameraProvider;
    [SerializeField] private Transform hudTilt;

    [Header("Tilt")]
    [SerializeField] private float fixedDownTiltDegrees = -10f;

    [Header("Follow / Freeze")]
    [SerializeField] private float yawFollowThresholdDegrees = 120f;
    [SerializeField] private float yawSlewSpeedDegreesPerSecond = 180f;

    [Header("Placement")]
    [SerializeField] private float distanceInFront = 2.0f;
    [SerializeField] private float heightOffset = -0.10f;

    private bool reticleInsideHud;
    private float hudYaw;

    private void Start()
    {
        var camT = cameraProvider != null ? cameraProvider.ActiveTransform : null;
        hudYaw = camT != null ? camT.eulerAngles.y : transform.eulerAngles.y;

        if (hudTilt != null)
            hudTilt.localRotation = Quaternion.Euler(fixedDownTiltDegrees, 0f, 0f);
    }

    private void LateUpdate()
    {
        var camT = cameraProvider != null ? cameraProvider.ActiveTransform : null;
        if (camT == null) return;

        // Keep in front (no parenting)
        Vector3 forwardFlat = camT.forward;
        forwardFlat.y = 0f;
        if (forwardFlat.sqrMagnitude < 0.0001f) forwardFlat = Vector3.forward;
        forwardFlat.Normalize();

        transform.position = camT.position + forwardFlat * distanceInFront + Vector3.up * heightOffset;

        // Constant tilt
        if (hudTilt != null)
            hudTilt.localRotation = Quaternion.Euler(fixedDownTiltDegrees, 0f, 0f);

        // Freeze yaw while hovering HUD
        if (reticleInsideHud)
        {
            transform.rotation = Quaternion.Euler(0f, hudYaw, 0f);
            return;
        }

        float camYaw = camT.eulerAngles.y;
        float delta = Mathf.DeltaAngle(hudYaw, camYaw);
        float absDelta = Mathf.Abs(delta);

        if (absDelta > yawFollowThresholdDegrees)
        {
            float step = yawSlewSpeedDegreesPerSecond * Time.deltaTime;
            hudYaw = Mathf.MoveTowardsAngle(hudYaw, camYaw, step);
        }

        transform.rotation = Quaternion.Euler(0f, hudYaw, 0f);
    }

    public void SetReticleInsideHud(bool inside)
    {
        if (inside && !reticleInsideHud)
            hudYaw = transform.eulerAngles.y;

        reticleInsideHud = inside;
    }
}
