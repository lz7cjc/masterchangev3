using UnityEngine;

/// <summary>
/// Step 1: Make HUD stay in front of the active camera with a constant downward tilt.
/// No interaction yet.
/// Attach to HUDPivot. Assign ActiveCameraProvider + HUDTilt.
/// </summary>
public class HUDStep1_KeepInFront : MonoBehaviour
{
    [SerializeField] private ActiveCameraProvider cameraProvider;
    [SerializeField] private Transform hudTilt;

    [Header("Placement")]
    [SerializeField] private float distance = 2.0f;
    [SerializeField] private float heightOffset = -0.10f;

    [Header("Tilt")]
    [SerializeField] private float fixedDownTiltDegrees = -10f;

    private void LateUpdate()
    {
        var camT = cameraProvider != null ? cameraProvider.ActiveTransform : null;
        if (camT == null) return;

        // Flatten forward so HUD does not follow pitch/roll
        Vector3 forwardFlat = camT.forward;
        forwardFlat.y = 0f;

        if (forwardFlat.sqrMagnitude < 0.0001f)
            forwardFlat = Vector3.forward;

        forwardFlat.Normalize();

        transform.position = camT.position + forwardFlat * distance + Vector3.up * heightOffset;

        // Yaw only
        float camYaw = camT.eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, camYaw, 0f);

        // Constant downward tilt
        if (hudTilt != null)
            hudTilt.localRotation = Quaternion.Euler(fixedDownTiltDegrees, 0f, 0f);
    }
}
