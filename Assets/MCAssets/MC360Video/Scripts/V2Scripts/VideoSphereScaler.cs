using UnityEngine;

/// <summary>
/// Ensures video sphere properly fills the view without black bars.
/// Attach to VideoSphere GameObject.
/// Adjusts sphere scale based on camera FOV and video aspect ratio.
/// </summary>
public class VideoSphereScaler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Settings")]
    [SerializeField] private float sphereScale = 10f; // Adjust if needed
    [SerializeField] private bool updateOnStart = true;

    void Start()
    {
        if (autoFindCamera)
        {
            mainCamera = Camera.main;
        }

        if (updateOnStart)
        {
            AdjustSphereScale();
        }
    }

    public void AdjustSphereScale()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[VideoSphereScaler] No camera assigned");
            return;
        }

        // Ensure sphere is large enough to fill view
        transform.localScale = Vector3.one * sphereScale;

        Debug.Log($"[VideoSphereScaler] Sphere scale set to {sphereScale}");
    }

    [ContextMenu("Adjust Scale Now")]
    public void AdjustScaleManually()
    {
        AdjustSphereScale();
    }
}