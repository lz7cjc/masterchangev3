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
    [SerializeField] private bool followCurveRotation = true; // Make icons follow curve direction

    [Header("Level 3 Specific Settings")]
    [SerializeField] private bool isLevel3Menu = false;
    [SerializeField] private float level3Radius = 6f; // Smaller radius for Level3 menus
    [SerializeField] private float level3ArcAngle = 120f; // Wider arc for more buttons
    [SerializeField] private bool level3UseTextMeshPro = true; // Level3 uses TextMeshPro buttons

    [Header("Integration")]
    [SerializeField] private MonoBehaviour hudCoordinator; // Changed to MonoBehaviour to avoid compilation issues
    [SerializeField] private bool isLevel2Menu = false;

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

        // Level3 specific inclusions
        if (isLevel3Menu && level3UseTextMeshPro)
        {
            // Include TextMeshPro components (Level3 category buttons)
            bool hasTextMeshPro = item.GetComponent<TMPro.TextMeshPro>() != null ||
                                 item.GetComponent<TMPro.TextMeshProUGUI>() != null;

            // Include objects with specific Level3 naming patterns
            bool hasLevel3Name = name.Contains("level3") ||
                                name.Contains("text") ||
                                name.Contains("travel") ||
                                name.Contains("sport") ||
                                name.Contains("beaches") ||
                                name.Contains("heights") ||
                                name.Contains("alcohol") ||
                                name.Contains("smoking") ||
                                name.Contains("mindfulness") ||
                                name.Contains("speedup") ||
                                name.Contains("slowdown") ||
                                name.Contains("startstop");

            // Include movement controls
            bool hasMovementControls = name.Contains("speed") ||
                                      name.Contains("start") ||
                                      name.Contains("stop") ||
                                      name.Contains("walk");

            if (hasTextMeshPro || hasLevel3Name || hasMovementControls)
            {
                Debug.Log($"Level3 menu item included: {item.name} (TextMeshPro={hasTextMeshPro}, Name={hasLevel3Name}, Movement={hasMovementControls})");
                return true;
            }
        }

        // Standard Level2 inclusions
        bool hasValidComponent = item.GetComponent<SpriteRenderer>() != null ||
               item.GetComponent<MeshRenderer>() != null ||
               item.GetComponent<TMPro.TextMeshPro>() != null ||
               item.GetComponent<showHideHUDcat>() != null ||
               item.GetComponent<ToggleActiveIcons>() != null ||
               item.GetComponent<MoveCamera>() != null ||
               item.GetComponent<Collider>() != null;

        // Include items with specific naming patterns
        bool hasValidName = name.Contains("icon") ||
               name.Contains("button") ||
               name.Contains("quad") ||
               name.Contains("panel") ||
               name.Contains("mesh") ||
               name.Contains("btn_") ||
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

        // Use Level3 specific settings if this is a Level3 menu
        float effectiveRadius = isLevel3Menu ? level3Radius : radius;
        float effectiveArcAngle = isLevel3Menu ? level3ArcAngle : arcAngle;

        // Calculate the angle between each item
        float angleStep = menuItems.Count > 1 ? effectiveArcAngle / (menuItems.Count - 1) : 0f;
        float startAngle = -effectiveArcAngle / 2f;

        Debug.Log($"VRCurvedMenuLayout: Arranging {menuItems.Count} items over {effectiveArcAngle}° arc (Level3={isLevel3Menu})");
        Debug.Log($"Angle step: {angleStep}°, Start angle: {startAngle}°, Radius: {effectiveRadius}");

        for (int i = 0; i < menuItems.Count; i++)
        {
            float currentAngle = startAngle + (angleStep * i);

            // Convert angle to radians
            float angleRad = currentAngle * Mathf.Deg2Rad;

            // Calculate position on arc
            Vector3 position = new Vector3(
                Mathf.Sin(angleRad) * effectiveRadius,
                heightOffset + (addDepthVariation ? Mathf.Sin(i * 0.5f) * depthVariation : 0f),
                Mathf.Cos(angleRad) * effectiveRadius
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
                        float effectiveArcAngle = isLevel3Menu ? level3ArcAngle : arcAngle;
                        float angleStep = menuItems.Count > 1 ? effectiveArcAngle / (menuItems.Count - 1) : 0f;
                        float startAngle = -effectiveArcAngle / 2f;
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
                    float effectiveArcAngle = isLevel3Menu ? level3ArcAngle : arcAngle;
                    float angleStep = menuItems.Count > 1 ? effectiveArcAngle / (menuItems.Count - 1) : 0f;
                    float startAngle = -effectiveArcAngle / 2f;
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
        if (animateOnEnable && menuItems.Count > 0)
        {
            StartCoroutine(AnimateToPositions());
        }
    }

    private void OnDisable()
    {
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

        int restoredCount = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                float spacing = 1.5f;
                float startOffset = -(transform.childCount - 1) * spacing * 0.5f;

                Vector3 newPosition = new Vector3(startOffset + (i * spacing), 0, 0);
                child.localPosition = newPosition;
                child.localScale = Vector3.one;
                child.localRotation = Quaternion.identity;
                child.gameObject.SetActive(true);

                Debug.Log($"Emergency restored: {child.name} to position {newPosition}");
                restoredCount++;
            }
        }
        Debug.Log($"=== EMERGENCY RESTORE COMPLETED - Restored {restoredCount} items ===");

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

            var components = child.GetComponents<Component>();
            Debug.Log($"Components found: {components.Length}");
            foreach (var comp in components)
            {
                Debug.Log($"  - {comp.GetType().Name}");
            }

            bool shouldInclude = ShouldIncludeInLayout(child);
            Debug.Log($"Should include in layout: {shouldInclude}");
        }
        Debug.Log("=== COMPONENT ANALYSIS COMPLETE ===");
    }

    #endregion
}