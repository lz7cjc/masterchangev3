using UnityEngine;

/// <summary>
/// IMPROVED VERSION: Handles camera parenting correctly
/// Works when sphere and camera are children of Player
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class VideoSphere360SetupImproved : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool setupOnStart = true;
    [SerializeField] private Camera targetCamera;

    [Header("Sphere Settings")]
    [SerializeField] private float sphereScale = 100f;

    [Header("Camera Settings")]
    [SerializeField] private float cameraFOV = 60f;
    [SerializeField] private float cameraNearClip = 0.1f;
    [SerializeField] private float cameraFarClip = 1000f;
    [SerializeField] private bool centerCameraInSphere = true;
    [SerializeField] private bool lockCameraEveryFrame = true; // NEW: Lock camera position

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private Vector3 sphereWorldPosition;

    void Start()
    {
        if (setupOnStart)
        {
            SetupVideoSphere();
        }
    }

    void LateUpdate()
    {
        // Keep camera locked at sphere center every frame
        if (lockCameraEveryFrame && targetCamera != null && centerCameraInSphere)
        {
            targetCamera.transform.position = transform.position;
        }
    }

    [ContextMenu("Setup Video Sphere")]
    public void SetupVideoSphere()
    {
        Debug.Log("[360-SETUP-V2] ========================================");
        Debug.Log("[360-SETUP-V2] Configuring 360 video sphere...");

        // 1. Setup sphere transform
        Debug.Log("[360-SETUP-V2] Step 1: Configuring sphere...");
        transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
        sphereWorldPosition = transform.position;
        Debug.Log($"[360-SETUP-V2] ✓ Sphere world position: {sphereWorldPosition}");
        Debug.Log($"[360-SETUP-V2] ✓ Sphere scale: {sphereScale}");

        // 2. Find camera if not assigned
        if (targetCamera == null)
        {
            // Try different methods to find camera
            targetCamera = Camera.main;

            if (targetCamera == null)
            {
                // Look for camera in parent hierarchy
                Transform parent = transform.parent;
                if (parent != null)
                {
                    targetCamera = parent.GetComponentInChildren<Camera>();
                }
            }

            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }

        // 3. Setup camera
        if (targetCamera != null)
        {
            Debug.Log("[360-SETUP-V2] Step 2: Configuring camera...");
            Debug.Log($"[360-SETUP-V2] Camera found: {targetCamera.name}");
            Debug.Log($"[360-SETUP-V2] Camera parent: {(targetCamera.transform.parent != null ? targetCamera.transform.parent.name : "None")}");

            if (centerCameraInSphere)
            {
                // Set camera to sphere's world position
                targetCamera.transform.position = sphereWorldPosition;
                Debug.Log($"[360-SETUP-V2] ✓ Camera positioned at: {sphereWorldPosition}");
                Debug.Log($"[360-SETUP-V2] ✓ Camera local position: {targetCamera.transform.localPosition}");
            }

            targetCamera.fieldOfView = cameraFOV;
            targetCamera.nearClipPlane = cameraNearClip;
            targetCamera.farClipPlane = cameraFarClip;

            Debug.Log($"[360-SETUP-V2] ✓ Camera FOV: {cameraFOV}°");

            if (lockCameraEveryFrame)
            {
                Debug.Log("[360-SETUP-V2] ✓ Camera will be locked at sphere center every frame");
            }
        }
        else
        {
            Debug.LogError("[360-SETUP-V2] ✗ No camera found!");
        }

        // 4. Validate material
        Debug.Log("[360-SETUP-V2] Step 3: Validating material...");
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
        {
            Material mat = renderer.material;
            Debug.Log($"[360-SETUP-V2] ✓ Material: {mat.name}");
            Debug.Log($"[360-SETUP-V2] ✓ Shader: {mat.shader.name}");

            if (mat.shader.name.ToLower().Contains("inside"))
            {
                Debug.Log("[360-SETUP-V2] ✓ Shader configured for inside rendering");
            }
            else
            {
                Debug.LogWarning("[360-SETUP-V2] ⚠ Shader might not support inside rendering");
            }
        }

        // 5. Check VideoPlayer
        Debug.Log("[360-SETUP-V2] Step 4: Checking VideoPlayer...");
        var videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer>();
        if (videoPlayer != null)
        {
            Debug.Log($"[360-SETUP-V2] ✓ VideoPlayer found");
            Debug.Log($"[360-SETUP-V2]   Render Mode: {videoPlayer.renderMode}");
        }

        Debug.Log("[360-SETUP-V2] ========================================");
        Debug.Log("[360-SETUP-V2] ✅ SETUP COMPLETE!");
        Debug.Log("[360-SETUP-V2] ========================================");
    }

    [ContextMenu("Test Camera Position")]
    public void TestCameraPosition()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            Debug.Log("[360-SETUP-V2] ========================================");
            Debug.Log("[360-SETUP-V2] Camera Position Test");
            Debug.Log($"[360-SETUP-V2] Sphere world position: {transform.position}");
            Debug.Log($"[360-SETUP-V2] Camera world position: {targetCamera.transform.position}");
            Debug.Log($"[360-SETUP-V2] Camera local position: {targetCamera.transform.localPosition}");

            float distance = Vector3.Distance(transform.position, targetCamera.transform.position);
            Debug.Log($"[360-SETUP-V2] Distance: {distance:F4} units");

            if (distance > 0.01f)
            {
                Debug.LogWarning("[360-SETUP-V2] ⚠ Camera is NOT at sphere center!");
                Debug.LogWarning("[360-SETUP-V2] ⚠ This will cause the 360 video to not display correctly");
            }
            else
            {
                Debug.Log("[360-SETUP-V2] ✓ Camera is correctly centered!");
            }
            Debug.Log("[360-SETUP-V2] ========================================");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Draw sphere
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sphereScale * 0.5f);

        // Draw camera if found
        if (targetCamera != null)
        {
            float distance = Vector3.Distance(transform.position, targetCamera.transform.position);

            if (distance < 0.1f)
            {
                // Camera is centered - green
                Gizmos.color = Color.green;
            }
            else
            {
                // Camera is NOT centered - red
                Gizmos.color = Color.red;
            }

            Gizmos.DrawSphere(targetCamera.transform.position, 1f);
            Gizmos.DrawLine(transform.position, targetCamera.transform.position);

            // Draw label
            UnityEditor.Handles.Label(targetCamera.transform.position + Vector3.up * 2,
                $"Distance: {distance:F2}");
        }
    }
}