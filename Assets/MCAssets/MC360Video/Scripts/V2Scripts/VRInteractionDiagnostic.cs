using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// DIAGNOSTIC TOOL: Attach this to any button to diagnose why it's not being detected
/// This will tell you EXACTLY what's wrong with your button setup
/// 
/// ATTACH TO: OpenClose button (or any HUD button)
/// </summary>
public class VRInteractionDiagnostic : MonoBehaviour
{
    [Header("Diagnostic Settings")]
    [SerializeField] private bool runContinuousDiagnostics = true;
    [SerializeField] private bool showColliderInScene = true;
    [SerializeField] private Color gizmoColor = Color.red;

    private Collider buttonCollider;
    private Camera mainCamera;
    private int diagnosticFrameInterval = 60; // Run diagnostics every 60 frames
    private int frameCount = 0;

    void Start()
    {
        buttonCollider = GetComponent<Collider>();
        mainCamera = Camera.main;

        // Run initial diagnostic
        Debug.Log("=== VR INTERACTION DIAGNOSTIC START ===");
        RunFullDiagnostic();
    }

    void Update()
    {
        if (runContinuousDiagnostics)
        {
            frameCount++;
            if (frameCount >= diagnosticFrameInterval)
            {
                frameCount = 0;
                CheckRaycastHit();
            }
        }
    }

    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log($"\n<color=cyan>╔════════════════════════════════════════╗</color>");
        Debug.Log($"<color=cyan>║  VR INTERACTION DIAGNOSTIC: {gameObject.name}</color>");
        Debug.Log($"<color=cyan>╚════════════════════════════════════════╝</color>\n");

        CheckCollider();
        CheckLayer();
        CheckEventHandlers();
        CheckCameraDistance();
        CheckRaycastHit();
        CheckEventSystem();

        Debug.Log($"\n<color=cyan>═══ DIAGNOSTIC COMPLETE ═══</color>\n");
    }

    private void CheckCollider()
    {
        Debug.Log("▶ 1. COLLIDER CHECK:");

        if (buttonCollider == null)
        {
            Debug.LogError("  ❌ NO COLLIDER FOUND!");
            Debug.LogError("  → Solution: Add a Box Collider to this GameObject");
            return;
        }

        Debug.Log($"  ✓ Collider found: {buttonCollider.GetType().Name}");

        if (buttonCollider.isTrigger)
        {
            Debug.Log("  ✓ Is Trigger: TRUE (correct for VR buttons)");
        }
        else
        {
            Debug.LogWarning("  ⚠ Is Trigger: FALSE");
            Debug.LogWarning("  → Set 'Is Trigger' to TRUE in Inspector");
        }

        if (buttonCollider.enabled)
        {
            Debug.Log("  ✓ Collider Enabled: TRUE");
        }
        else
        {
            Debug.LogError("  ❌ Collider Enabled: FALSE");
            Debug.LogError("  → Enable the collider in Inspector");
        }

        // Check collider bounds
        Bounds bounds = buttonCollider.bounds;
        Debug.Log($"  ℹ Collider Bounds:");
        Debug.Log($"    Center: {bounds.center}");
        Debug.Log($"    Size: {bounds.size}");
        Debug.Log($"    Extents: {bounds.extents}");

        if (bounds.size.magnitude < 0.1f)
        {
            Debug.LogWarning("  ⚠ Collider is VERY SMALL (might be hard to hit)");
            Debug.LogWarning("  → Try making it larger for testing");
        }
    }

    private void CheckLayer()
    {
        Debug.Log("\n▶ 2. LAYER CHECK:");

        string layerName = LayerMask.LayerToName(gameObject.layer);
        Debug.Log($"  ℹ GameObject Layer: {gameObject.layer} ({layerName})");

        // Find VRReticlePointer
        VRReticlePointer reticle = FindObjectOfType<VRReticlePointer>();
        if (reticle != null)
        {
            // Use reflection to get the private interactableLayers field
            var field = typeof(VRReticlePointer).GetField("interactableLayers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                LayerMask interactableLayers = (LayerMask)field.GetValue(reticle);

                Debug.Log($"  ℹ VRReticlePointer Interactable Layers: {LayerMaskToString(interactableLayers)}");

                // Check if this layer is included
                if ((interactableLayers & (1 << gameObject.layer)) != 0)
                {
                    Debug.Log($"  ✓ Layer '{layerName}' IS included in Interactable Layers");
                }
                else
                {
                    Debug.LogError($"  ❌ Layer '{layerName}' is NOT in Interactable Layers!");
                    Debug.LogError($"  → Solution: In VRReticlePointer Inspector, add layer '{layerName}' to 'Interactable Layers'");
                    Debug.LogError($"  → OR: Change this button's layer to match Interactable Layers");
                }
            }
        }
        else
        {
            Debug.LogWarning("  ⚠ VRReticlePointer not found in scene");
        }
    }

    private void CheckEventHandlers()
    {
        Debug.Log("\n▶ 3. EVENT HANDLER CHECK:");

        var enterHandlers = GetComponents<IPointerEnterHandler>();
        var exitHandlers = GetComponents<IPointerExitHandler>();

        if (enterHandlers.Length > 0)
        {
            Debug.Log($"  ✓ Found {enterHandlers.Length} IPointerEnterHandler(s):");
            foreach (var handler in enterHandlers)
            {
                Debug.Log($"    - {handler.GetType().Name}");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠ No IPointerEnterHandler found");
            Debug.LogWarning("  → VRHUDToggleButton should implement IPointerEnterHandler");
        }

        if (exitHandlers.Length > 0)
        {
            Debug.Log($"  ✓ Found {exitHandlers.Length} IPointerExitHandler(s):");
            foreach (var handler in exitHandlers)
            {
                Debug.Log($"    - {handler.GetType().Name}");
            }
        }
        else
        {
            Debug.LogWarning("  ⚠ No IPointerExitHandler found");
            Debug.LogWarning("  → VRHUDToggleButton should implement IPointerExitHandler");
        }

        // Check for EventTrigger
        EventTrigger eventTrigger = GetComponent<EventTrigger>();
        if (eventTrigger != null)
        {
            Debug.Log($"  ℹ EventTrigger found with {eventTrigger.triggers.Count} trigger(s)");
        }
    }

    private void CheckCameraDistance()
    {
        Debug.Log("\n▶ 4. CAMERA DISTANCE CHECK:");

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            Debug.LogError("  ❌ No camera found!");
            return;
        }

        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
        Debug.Log($"  ℹ Distance from camera: {distance:F2}m");

        VRReticlePointer reticle = FindObjectOfType<VRReticlePointer>();
        if (reticle != null)
        {
            var field = typeof(VRReticlePointer).GetField("maxInteractionDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                float maxDistance = (float)field.GetValue(reticle);
                Debug.Log($"  ℹ Max Interaction Distance: {maxDistance:F2}m");

                if (distance <= maxDistance)
                {
                    Debug.Log("  ✓ Button is within interaction range");
                }
                else
                {
                    Debug.LogError($"  ❌ Button is TOO FAR ({distance:F2}m > {maxDistance:F2}m)");
                    Debug.LogError("  → Solution: Increase 'Max Interaction Distance' in VRReticlePointer");
                }
            }
        }
    }

    private void CheckRaycastHit()
    {
        Debug.Log("\n▶ 5. RAYCAST HIT CHECK:");

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null || buttonCollider == null)
        {
            Debug.LogWarning("  ⚠ Cannot perform raycast check (camera or collider missing)");
            return;
        }

        // Perform raycast from camera forward
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // Try with all layers first
        if (Physics.Raycast(ray, out hit, 100f, ~0))
        {
            Debug.Log($"  ℹ Raycast hit SOMETHING: {hit.collider.gameObject.name} at {hit.distance:F2}m");

            if (hit.collider == buttonCollider)
            {
                Debug.Log("  ✓ Raycast HITS THIS BUTTON!");
                Debug.Log($"    Hit Point: {hit.point}");
                Debug.Log($"    Hit Normal: {hit.normal}");
            }
            else
            {
                Debug.LogWarning($"  ⚠ Raycast hits '{hit.collider.gameObject.name}' instead of this button");

                // Check if hit object is between camera and button
                float buttonDist = Vector3.Distance(mainCamera.transform.position, transform.position);
                if (hit.distance < buttonDist)
                {
                    Debug.LogWarning($"  → '{hit.collider.gameObject.name}' is BLOCKING the button");
                    Debug.LogWarning($"  → Solution: Move blocker or adjust layers");
                }
            }
        }
        else
        {
            Debug.LogWarning("  ⚠ Raycast didn't hit anything");
            Debug.LogWarning("  → Button might be outside camera view or blocked");
        }

        // Try raycast with VRReticlePointer's layer mask
        VRReticlePointer reticle = FindObjectOfType<VRReticlePointer>();
        if (reticle != null)
        {
            var field = typeof(VRReticlePointer).GetField("interactableLayers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                LayerMask mask = (LayerMask)field.GetValue(reticle);

                if (Physics.Raycast(ray, out hit, 100f, mask))
                {
                    if (hit.collider == buttonCollider)
                    {
                        Debug.Log($"  ✓ Raycast with Interactable Layers HITS THIS BUTTON!");
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠ Raycast with Interactable Layers hits '{hit.collider.gameObject.name}' instead");
                    }
                }
                else
                {
                    Debug.LogError("  ❌ Raycast with Interactable Layers hits NOTHING");
                    Debug.LogError("  → This is the problem! Check layer settings.");
                }
            }
        }
    }

    private void CheckEventSystem()
    {
        Debug.Log("\n▶ 6. EVENT SYSTEM CHECK:");

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            Debug.Log($"  ✓ EventSystem found: {eventSystem.name}");
            Debug.Log($"    Current Selected: {(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "None")}");
        }
        else
        {
            Debug.LogWarning("  ⚠ No EventSystem in scene");
            Debug.LogWarning("  → EventSystem is recommended (though VRReticlePointer should work without it)");
        }
    }

    private string LayerMaskToString(LayerMask mask)
    {
        if (mask == -1) return "Everything";
        if (mask == 0) return "Nothing";

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(string.IsNullOrEmpty(layerName) ? $"Layer{i}" : layerName);
            }
        }
        return sb.Length > 0 ? sb.ToString() : "Nothing";
    }

    void OnDrawGizmos()
    {
        if (!showColliderInScene || buttonCollider == null) return;

        Gizmos.color = gizmoColor;
        Bounds bounds = buttonCollider.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw a sphere at center
        Gizmos.DrawSphere(bounds.center, 0.02f);
    }

    void OnDrawGizmosSelected()
    {
        if (buttonCollider == null) return;

        // Draw detailed collider visualization
        Gizmos.color = Color.yellow;
        Bounds bounds = buttonCollider.bounds;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // Draw ray from camera to button
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(mainCamera.transform.position, bounds.center);

            // Draw camera forward ray
            Gizmos.color = Color.red;
            Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * 10f);
        }
    }
}