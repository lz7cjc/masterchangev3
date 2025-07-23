using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Professional VR Curved Menu Layout - Arranges UI elements in natural arcs
/// Integrates with your existing HUD system
/// </summary>
public class VRCurvedMenuLayout : MonoBehaviour
{
    [Header("Curve Settings")]
    [SerializeField] private float radius = 3f;
    [SerializeField] private float arcAngle = 60f; // Total arc in degrees
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private bool faceCameraAlways = true;

    [Header("Animation")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool animateOnEnable = true;

    [Header("Professional Styling")]
    [SerializeField] private float iconScale = 1f;
    [SerializeField] private float iconSpacing = 1.2f; // Multiplier for natural spacing
    [SerializeField] private bool addDepthVariation = true;
    [SerializeField] private float depthVariation = 0.2f;
    [SerializeField] private bool followCurveRotation = true; // NEW: Make icons follow curve direction

    [Header("Integration")]
    [SerializeField] private MonoBehaviour hudCoordinator; // Changed to MonoBehaviour to avoid compilation issues
    [SerializeField] private bool isLevel2Menu = false;
    [SerializeField] private bool isLevel3Menu = false;

    private List<Transform> menuItems = new List<Transform>();
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Vector3> targetPositions = new List<Vector3>();
    private Transform playerCamera;
    private bool isAnimating = false;

    private void Start()
    {
        // Find player camera
        playerCamera = Camera.main?.transform ?? FindObjectOfType<Camera>()?.transform;

        // Find HUD coordinator if not assigned (safely)
        if (hudCoordinator == null)
        {
            hudCoordinator = GameObject.FindObjectOfType<MonoBehaviour>();
        }

        // Collect all child menu items
        CollectMenuItems();

        // Calculate curved positions
        CalculateCurvedPositions();

        Debug.Log($"VR Curved Layout initialized with {menuItems.Count} items");
    }



    private void CollectMenuItems()
    {
        menuItems.Clear();
        originalPositions.Clear();

        // Collect direct children that should be arranged
        foreach (Transform child in transform)
        {
            Debug.Log($"Found child: {child.name}, active: {child.gameObject.activeInHierarchy}");

            // Skip inactive objects and specific exclusions
            if (child.gameObject.activeInHierarchy && ShouldIncludeInLayout(child))
            {
                menuItems.Add(child);
                originalPositions.Add(child.localPosition);
                Debug.Log($"Added to layout: {child.name} at position {child.localPosition}");
            }
        }

        Debug.Log($"VRCurvedMenuLayout: Collected {menuItems.Count} menu items for curved layout");

        // List all collected items
        for (int i = 0; i < menuItems.Count; i++)
        {
            Debug.Log($"  [{i}] {menuItems[i].name}");
        }
    }

    private bool ShouldIncludeInLayout(Transform item)
    {
        string name = item.name.ToLower();

        // Exclude certain items from layout
        if (name.Contains("background") ||
            name.Contains("countdown") ||
            (name.Contains("close") && !name.Contains("icon")))
        {
            Debug.Log($"Excluding from layout: {item.name} (exclusion rule)");
            return false;
        }

        // Include items with these components (your actual setup)
        bool hasValidComponent = item.GetComponent<SpriteRenderer>() != null ||
               item.GetComponent<MeshRenderer>() != null ||
               item.GetComponent<TMPro.TextMeshPro>() != null ||
               item.GetComponent<showHideHUDcat>() != null ||
               item.GetComponent<ToggleActiveIcons>() != null ||
               item.GetComponent<MoveCamera>() != null || // YOUR SCRIPT
               item.GetComponent<Collider>() != null;

        // Include items with specific naming patterns
        bool hasValidName = name.Contains("icon") ||
               name.Contains("button") ||
               name.Contains("quad") ||
               name.Contains("panel") ||
               name.Contains("mesh") ||
               name.Contains("btn_") || // YOUR NAMING PATTERN
               name.Contains("hud_");

        bool shouldInclude = hasValidComponent || hasValidName;

        Debug.Log($"Checking {item.name}: Component={hasValidComponent}, Name={hasValidName}, Include={shouldInclude}");

        return shouldInclude;
    }

    private void CalculateCurvedPositions()
    {
        targetPositions.Clear();

        if (menuItems.Count == 0)
        {
            Debug.LogWarning("VRCurvedMenuLayout: No menu items to arrange!");
            return;
        }

        // Calculate the angle between each item
        float angleStep = menuItems.Count > 1 ? arcAngle / (menuItems.Count - 1) : 0f;
        float startAngle = -arcAngle / 2f;

        Debug.Log($"VRCurvedMenuLayout: Arranging {menuItems.Count} items over {arcAngle}° arc");
        Debug.Log($"Angle step: {angleStep}°, Start angle: {startAngle}°, Radius: {radius}");

        for (int i = 0; i < menuItems.Count; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // Convert angle to radians
            float angleRad = currentAngle * Mathf.Deg2Rad;

            // Calculate position on arc
            Vector3 position = new Vector3(
                Mathf.Sin(angleRad) * radius,
                heightOffset + (addDepthVariation ? Mathf.Sin(i * 0.5f) * depthVariation : 0f),
                Mathf.Cos(angleRad) * radius
            );

            targetPositions.Add(position);
            Debug.Log($"  Item {i} ({menuItems[i].name}): angle={currentAngle:F1}°, position={position}");
        }
    }

    private System.Collections.IEnumerator AnimateToPositions()
    {
        if (isAnimating || menuItems.Count == 0) yield break;

        isAnimating = true;
        float elapsedTime = 0f;

        // Store starting positions
        List<Vector3> startPositions = new List<Vector3>();
        for (int i = 0; i < menuItems.Count; i++)
        {
            startPositions.Add(menuItems[i].localPosition);
        }

        while (elapsedTime < animationDuration)
        {
            float progress = elapsedTime / animationDuration;
            float curvedProgress = animationCurve.Evaluate(progress);

            // Animate each item
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i] != null)
                {
                    Vector3 currentPos = Vector3.Lerp(startPositions[i], targetPositions[i], curvedProgress);
                    menuItems[i].localPosition = currentPos;

                    // Apply scaling animation
                    float scale = Mathf.Lerp(0.8f, iconScale, curvedProgress);
                    menuItems[i].localScale = Vector3.one * scale;

                    // Handle rotation based on settings
                    if (followCurveRotation)
                    {
                        // Calculate the angle for this position in the curve
                        float angleStep = menuItems.Count > 1 ? arcAngle / (menuItems.Count - 1) : 0f;
                        float startAngle = -arcAngle / 2f;
                        float currentAngle = startAngle + (angleStep * i);

                        // Set rotation to follow the curve (face outward from center)
                        Quaternion targetRotation = Quaternion.Euler(0, currentAngle, 0);
                        menuItems[i].localRotation = Quaternion.Lerp(Quaternion.identity, targetRotation, curvedProgress);
                    }
                    else if (faceCameraAlways && playerCamera != null)
                    {
                        // Face camera if enabled and not following curve
                        Vector3 directionToCamera = (playerCamera.position - menuItems[i].position).normalized;
                        menuItems[i].rotation = Quaternion.LookRotation(-directionToCamera);
                    }
                }
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final positions are exact
        for (int i = 0; i < menuItems.Count; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].localPosition = targetPositions[i];
                menuItems[i].localScale = Vector3.one * iconScale;

                // Set final rotation
                if (followCurveRotation)
                {
                    float angleStep = menuItems.Count > 1 ? arcAngle / (menuItems.Count - 1) : 0f;
                    float startAngle = -arcAngle / 2f;
                    float currentAngle = startAngle + (angleStep * i);
                    menuItems[i].localRotation = Quaternion.Euler(0, currentAngle, 0);
                }
            }
        }

        isAnimating = false;
        Debug.Log("Curved menu animation completed");
    }

    private void Update()
    {
        // Keep items facing camera during runtime
        if (faceCameraAlways && playerCamera != null && !isAnimating)
        {
            UpdateCameraFacing();
        }
    }

    private void UpdateCameraFacing()
    {
        if (!followCurveRotation) // Only do camera facing if not following curve
        {
            foreach (Transform item in menuItems)
            {
                if (item != null)
                {
                    Vector3 directionToCamera = (playerCamera.position - item.position).normalized;
                    item.rotation = Quaternion.Slerp(
                        item.rotation,
                        Quaternion.LookRotation(-directionToCamera),
                        Time.deltaTime * 5f
                    );
                }
            }
        }
    }

    #region Public Interface for HUD Integration

    public void RefreshLayout()
    {
        CollectMenuItems();
        CalculateCurvedPositions();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimateToPositions());
        }
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        CalculateCurvedPositions();
    }

    public void SetArcAngle(float newAngle)
    {
        arcAngle = newAngle;
        CalculateCurvedPositions();
    }

    public void InstantLayout()
    {
        CalculateCurvedPositions();

        for (int i = 0; i < menuItems.Count && i < targetPositions.Count; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].localPosition = targetPositions[i];
                menuItems[i].localScale = Vector3.one * iconScale;
            }
        }
    }

    public void ResetToOriginalPositions()
    {
        for (int i = 0; i < menuItems.Count && i < originalPositions.Count; i++)
        {
            if (menuItems[i] != null)
            {
                menuItems[i].localPosition = originalPositions[i];
                menuItems[i].localScale = Vector3.one;
            }
        }
    }

    #endregion

    #region Integration with Existing HUD System

    private void OnEnable()
    {
        // Subscribe to HUD events if coordinator is available
        // Coordinator integration temporarily disabled to avoid compilation issues

        if (animateOnEnable && menuItems.Count > 0)
        {
            StartCoroutine(AnimateToPositions());
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        StopAllCoroutines();
        isAnimating = false;
    }

    #endregion

    #region Editor Helpers

    [ContextMenu("Preview Curved Layout")]
    private void PreviewLayout()
    {
        CollectMenuItems();
        CalculateCurvedPositions();
        InstantLayout();
    }

    [ContextMenu("Reset to Original")]
    private void ResetLayout()
    {
        ResetToOriginalPositions();
    }

    [ContextMenu("Emergency Restore Icons")]
    private void EmergencyRestore()
    {
        Debug.Log("=== EMERGENCY RESTORE STARTING ===");

        // Find all children and reset them to reasonable positions
        int restoredCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                // Reset to a simple horizontal line spreading outward from center
                float spacing = 1.5f; // Adjust this spacing as needed
                float startOffset = -(transform.childCount - 1) * spacing * 0.5f;

                Vector3 newPosition = new Vector3(startOffset + (i * spacing), 0, 0);
                child.localPosition = newPosition;
                child.localScale = Vector3.one;
                child.localRotation = Quaternion.identity;

                // Make sure it's active
                child.gameObject.SetActive(true);

                Debug.Log($"Emergency restored: {child.name} to position {newPosition}, active: {child.gameObject.activeInHierarchy}");
                restoredCount++;
            }
        }
        Debug.Log($"=== EMERGENCY RESTORE COMPLETED - Restored {restoredCount} items ===");

        // Force refresh the menu items collection with detailed logging
        Debug.Log("=== COLLECTING MENU ITEMS AFTER RESTORE ===");
        CollectMenuItems();
    }

    [ContextMenu("Debug Component Check")]
    private void DebugComponentCheck()
    {
        Debug.Log("=== DETAILED COMPONENT ANALYSIS ===");
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log($"\n--- Analyzing: {child.name} ---");
            Debug.Log($"Active: {child.gameObject.activeInHierarchy}");
            Debug.Log($"Position: {child.localPosition}");
            Debug.Log($"Scale: {child.localScale}");

            // Check all components
            var components = child.GetComponents<Component>();
            Debug.Log($"Components found: {components.Length}");
            foreach (var comp in components)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }

            // Test the inclusion logic
            bool shouldInclude = ShouldIncludeInLayout(child);
            Debug.Log($"Should include in layout: {shouldInclude}");
        }
        Debug.Log("=== COMPONENT ANALYSIS COMPLETE ===");
    }

    private void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        // Only draw gizmos in editor when this GameObject is selected
        if (UnityEditor.Selection.activeGameObject == gameObject && targetPositions.Count > 0)
        {
            Gizmos.color = Color.cyan;

            // Draw arc line (simplified)
            for (int i = 0; i < targetPositions.Count - 1; i++)
            {
                Vector3 worldPos1 = transform.TransformPoint(targetPositions[i]);
                Vector3 worldPos2 = transform.TransformPoint(targetPositions[i + 1]);
                Gizmos.DrawLine(worldPos1, worldPos2);
            }

            // Draw radius circle (less intrusive)
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Semi-transparent red
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }

    #endregion
}