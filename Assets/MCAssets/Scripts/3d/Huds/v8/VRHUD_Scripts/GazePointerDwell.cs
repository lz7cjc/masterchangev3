using UnityEngine;

/// <summary>
/// Physics raycast pointer with dwell-to-click.
/// - Raycast originates from the active camera
/// - After dwellSeconds on a collider, calls IGazeClickable (if present)
/// - Also reports whether the reticle is on the HUD (for freezing HUD yaw)
/// Attach to a manager object (e.g., Cameras/VRReticlePointer).
/// </summary>
public class GazePointerDwell : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ActiveCameraProvider cameraProvider;

    [Header("Raycast")]
    [SerializeField] private float maxDistance = 25f;
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("Dwell")]
    [SerializeField] private float dwellSeconds = 3f;

    [Header("HUD Detection (pick one)")]
    [Tooltip("Recommended: set to the HUD layer index so the system can detect hovering HUD reliably.")]
    [SerializeField] private int hudLayer = -1;

    [Tooltip("Optional: assign HUDBounds collider. If set, we treat hits under HUDBounds hierarchy as HUD hits.")]
    [SerializeField] private Collider hudBoundsTrigger;

    [Header("HUD Freeze Sink")]
    [SerializeField] private MonoBehaviour hudHoverSinkBehaviour; // assign HUDAnchorController here

    private IHUDHoverStateSink hudHoverSink;

    private Collider current;
    private float dwellTimer;

    private void Awake()
    {
        hudHoverSink = hudHoverSinkBehaviour as IHUDHoverStateSink;
    }

    private void Update()
    {
        var camT = cameraProvider != null ? cameraProvider.ActiveTransform : null;
        if (camT == null) return;

        Ray ray = new Ray(camT.position, camT.forward);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance, raycastLayers, QueryTriggerInteraction.Ignore);

        if (!hit)
        {
            SetHudHover(false);
            current = null;
            dwellTimer = 0f;
            return;
        }

        // HUD hover detection
        bool isHudHit = false;

        if (hudLayer >= 0 && hitInfo.collider.gameObject.layer == hudLayer)
            isHudHit = true;

        if (!isHudHit && hudBoundsTrigger != null)
        {
            // If the hit object is within HUDBounds hierarchy, treat as HUD
            isHudHit = hitInfo.collider.transform.IsChildOf(hudBoundsTrigger.transform) || hitInfo.collider == hudBoundsTrigger;
        }

        SetHudHover(isHudHit);

        Collider hitCol = hitInfo.collider;

        if (hitCol != current)
        {
            current = hitCol;
            dwellTimer = 0f;
        }

        dwellTimer += Time.deltaTime;

        if (dwellTimer >= dwellSeconds)
        {
            dwellTimer = 0f;

            // Prefer component on parent (so collider can be on a child)
            var clickable = current.GetComponentInParent<IGazeClickable>();
            if (clickable != null) clickable.OnGazeClick();
            else Debug.Log($"DWELL CLICK (no handler): {current.name}");
        }
    }

    private void SetHudHover(bool inside)
    {
        hudHoverSink?.SetReticleInsideHud(inside);
    }
}
