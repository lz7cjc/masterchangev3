using UnityEngine;

/// <summary>
/// Detects which camera is currently active (VR vs 360) and exposes its Transform.
/// Works with any setup where cameras are enabled/disabled.
/// </summary>
public class ActiveCameraProvider : MonoBehaviour
{
    [Header("Assign both cameras (optional but recommended)")]
    [SerializeField] private Camera vrCamera;
    [SerializeField] private Camera camera360;

    public Camera ActiveCamera { get; private set; }
    public Transform ActiveTransform => ActiveCamera != null ? ActiveCamera.transform : null;

    private void LateUpdate()
    {
        ResolveActive();
    }

    private void ResolveActive()
    {
        Camera candidate = null;

        if (vrCamera != null && vrCamera.isActiveAndEnabled) candidate = vrCamera;
        else if (camera360 != null && camera360.isActiveAndEnabled) candidate = camera360;
        else if (Camera.main != null && Camera.main.isActiveAndEnabled) candidate = Camera.main;

        ActiveCamera = candidate;
    }
}
