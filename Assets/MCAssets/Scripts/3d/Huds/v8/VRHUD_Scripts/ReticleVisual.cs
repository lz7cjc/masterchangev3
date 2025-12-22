using UnityEngine;

/// <summary>
/// World-space reticle visual (no Canvas).
/// Attach to a small Sphere/Quad under VRReticlePointer.
/// - If ray hits something, reticle sits on the hit point.
/// - Otherwise it sits at a fixed distance in front of the active camera.
/// </summary>
public class ReticleVisual : MonoBehaviour
{
    [SerializeField] private ActiveCameraProvider cameraProvider;
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private float defaultDistance = 2.0f;
    [SerializeField] private LayerMask raycastLayers = ~0;
    [SerializeField] private float surfaceOffset = 0.01f;

    private void LateUpdate()
    {
        var camT = cameraProvider != null ? cameraProvider.ActiveTransform : null;
        if (camT == null) return;

        Ray ray = new Ray(camT.position, camT.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, raycastLayers, QueryTriggerInteraction.Ignore))
        {
            transform.position = hit.point + hit.normal * surfaceOffset;
            transform.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
        }
        else
        {
            transform.position = camT.position + camT.forward * defaultDistance;
            transform.rotation = camT.rotation;
        }
    }
}
