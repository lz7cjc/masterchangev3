using UnityEngine;

public class CleanIconRestorer : MonoBehaviour
{
    [Header("Icon Settings")]
    [SerializeField] private float iconSize = 0.75f;
    [SerializeField] private float iconSpacing = 1.8f;
    [SerializeField] private float iconHeight = 0f;

    [ContextMenu("List All Children")]
    private void ListAllChildren()
    {
        Debug.Log("=== LISTING ALL CHILDREN (INCLUDING FOLDERS) ===");
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Debug.Log($"Child {i}: {child.name}, Position: {child.localPosition}, Scale: {child.localScale}, Active: {child.gameObject.activeInHierarchy}");

            // Show what's inside folders
            if (child.childCount > 0)
            {
                Debug.Log($"  └─ Folder contents:");
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform subChild = child.GetChild(j);
                    Debug.Log($"     ├─ {subChild.name}, Active: {subChild.gameObject.activeInHierarchy}");
                }
            }
        }
        Debug.Log($"=== TOTAL: {transform.childCount} TOP-LEVEL ITEMS ===");
    }

    [ContextMenu("Perfect VR HUD Setup")]
    private void PerfectVRHUDSetup()
    {
        Debug.Log("=== CREATING PERFECT VR HUD ===");

        Vector3 idealSize = Vector3.one * iconSize;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                // Calculate position for horizontal line
                float totalWidth = (transform.childCount - 1) * iconSpacing;
                float startX = -totalWidth * 0.5f;
                Vector3 newPosition = new Vector3(startX + (i * iconSpacing), iconHeight, 0);

                // Apply settings
                child.localPosition = newPosition;
                child.localScale = idealSize; // This overwrites your manual scaling
                child.localRotation = Quaternion.identity;
                child.gameObject.SetActive(true);

                Debug.Log($"Setup: {child.name} at {newPosition} with scale {idealSize}");
            }
        }

        Debug.Log("=== PERFECT VR HUD COMPLETE ===");
    }

    [ContextMenu("Position Only (Keep Current Sizes)")]
    private void PositionOnlyKeepSizes()
    {
        Debug.Log("=== POSITIONING ICONS (KEEPING SIZES) ===");

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                // Calculate position for horizontal line
                float totalWidth = (transform.childCount - 1) * iconSpacing;
                float startX = -totalWidth * 0.5f;
                Vector3 newPosition = new Vector3(startX + (i * iconSpacing), iconHeight, 0);

                // Apply ONLY position and rotation, keep existing scale
                child.localPosition = newPosition;
                child.localRotation = Quaternion.identity;
                child.gameObject.SetActive(true);

                Debug.Log($"Positioned: {child.name} at {newPosition} keeping scale {child.localScale}");

                // Special handling for folders with toggle functionality
                if (child.name == "VR" && child.childCount > 0)
                {
                    Debug.Log($"  └─ VR folder positioned - contains {child.childCount} toggle states");
                }
                else if (child.name == "Spin" && child.childCount > 0)
                {
                    Debug.Log($"  └─ Spin folder positioned - contains {child.childCount} tracking icons");
                }
            }
        }

        Debug.Log("=== POSITIONING COMPLETE (SIZES PRESERVED) ===");
    }

    [ContextMenu("Position 7 Individual Icons")]
    private void Position7IndividualIcons()
    {
        Debug.Log("=== POSITIONING 7 INDIVIDUAL ICONS IN CORRECT ORDER ===");

        // Collect the 7 icons in your preferred order
        Transform[] iconsToArrange = new Transform[7];

        // Your preferred order:
        // 0: Spin Left, 1: Home, 2: Location, 3: Move, 4: Headset, 5: Dashboard, 6: Spin Right

        // From Spin folder
        Transform spinFolder = transform.Find("Spin");
        if (spinFolder != null)
        {
            iconsToArrange[0] = spinFolder.Find("trackingtargetleft");  // Spin Left
            iconsToArrange[6] = spinFolder.Find("trackingtargetright"); // Spin Right
        }

        // Direct children
        iconsToArrange[1] = transform.Find("btn_HUD_home");        // Home
        iconsToArrange[2] = transform.Find("btn_HUD_location");    // Location
        iconsToArrange[3] = transform.Find("Move");                // Move
        iconsToArrange[5] = transform.Find("gearsdashboarddewfault"); // Dashboard

        // From VR folder - find the active headset icon
        Transform vrFolder = transform.Find("VR");
        if (vrFolder != null)
        {
            // Get the headset icon (assuming headsetdefault is the main one)
            iconsToArrange[4] = vrFolder.Find("headsetdefault");   // Headset
            if (iconsToArrange[4] == null)
            {
                iconsToArrange[4] = vrFolder.Find("noheadsetdefault");
            }
        }

        // Verify all icons found
        string[] iconNames = { "Spin Left", "Home", "Location", "Move", "Headset", "Dashboard", "Spin Right" };
        int validIcons = 0;
        for (int i = 0; i < iconsToArrange.Length; i++)
        {
            if (iconsToArrange[i] != null)
            {
                validIcons++;
                Debug.Log($"✓ Position {i}: {iconNames[i]} -> {iconsToArrange[i].name}");
            }
            else
            {
                Debug.LogWarning($"✗ Position {i}: {iconNames[i]} -> NOT FOUND!");
            }
        }

        Debug.Log($"Found {validIcons} out of 7 expected icons");

        // Calculate positions for 7 icons
        float totalWidth = 6 * iconSpacing; // 6 gaps between 7 icons
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < iconsToArrange.Length; i++)
        {
            if (iconsToArrange[i] != null)
            {
                Vector3 newPosition = new Vector3(startX + (i * iconSpacing), iconHeight, 0);

                // Apply position (in world space since some are nested)
                iconsToArrange[i].position = transform.TransformPoint(newPosition);
                iconsToArrange[i].rotation = transform.rotation;
                iconsToArrange[i].gameObject.SetActive(true);

                Debug.Log($"Positioned {iconNames[i]}: {iconsToArrange[i].name} at world pos {iconsToArrange[i].position}");
            }
        }

        Debug.Log("=== 7 INDIVIDUAL ICONS POSITIONED IN YOUR PREFERRED ORDER ===");
    }

    [ContextMenu("List 7 Target Icons")]
    private void List7TargetIcons()
    {
        Debug.Log("=== FINDING 7 TARGET ICONS IN YOUR PREFERRED ORDER ===");

        string[] targetNames = {
            "Spin Left (trackingtargetleft)",
            "Home (btn_HUD_home)",
            "Location (btn_HUD_location)",
            "Move (Move)",
            "Headset (headsetdefault or noheadsetdefault)",
            "Dashboard (gearsdashboarddewfault)",
            "Spin Right (trackingtargetright)"
        };

        Transform[] targets = {
            transform.Find("Spin")?.Find("trackingtargetleft"),      // Spin Left
            transform.Find("btn_HUD_home"),                          // Home
            transform.Find("btn_HUD_location"),                      // Location
            transform.Find("Move"),                                  // Move
            transform.Find("VR")?.Find("headsetdefault") ?? transform.Find("VR")?.Find("noheadsetdefault"), // Headset
            transform.Find("gearsdashboarddewfault"),               // Dashboard
            transform.Find("Spin")?.Find("trackingtargetright")     // Spin Right
        };

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                Debug.Log($"✓ Position {i + 1}: {targetNames[i]} -> Found: {targets[i].name}");
            }
            else
            {
                Debug.LogError($"✗ Position {i + 1}: {targetNames[i]} -> NOT FOUND!");
            }
        }

        Debug.Log("=== TARGET ICON SEARCH COMPLETE ===");
    }

    [ContextMenu("Test Small Size (0.5)")]
    private void TestSmallSize()
    {
        SetSizeAndApply(0.5f);
    }

    [ContextMenu("Test Medium Size (0.75)")]
    private void TestMediumSize()
    {
        SetSizeAndApply(0.75f);
    }

    [ContextMenu("Test Large Size (1.0)")]
    private void TestLargeSize()
    {
        SetSizeAndApply(1.0f);
    }

    private void SetSizeAndApply(float newSize)
    {
        iconSize = newSize;
        PerfectVRHUDSetup();
    }
}