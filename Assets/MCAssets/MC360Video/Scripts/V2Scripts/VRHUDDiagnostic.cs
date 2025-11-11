using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// DIAGNOSTIC TOOL: Verifies HUD interaction setup and identifies problems.
/// 
/// ATTACH TO: Any GameObject in the scene (or HUDCanvas)
/// 
/// USAGE:
/// 1. Add this script to any GameObject
/// 2. Right-click the script in Inspector
/// 3. Select "Run Full Diagnostic"
/// 4. Check Console for detailed report
/// </summary>
public class VRHUDDiagnostic : MonoBehaviour
{
    [Header("References to Check")]
    [SerializeField] private Canvas hudCanvas;
    [SerializeField] private GameObject[] buttonsToCheck;
    [SerializeField] private bool autoFindComponents = true;

    [Header("Diagnostic Options")]
    [SerializeField] private bool showDetailedLogs = true;
    [SerializeField] private bool checkEveryFrame = false;

    private EventSystem eventSystem;
    private int framesSinceLastCheck = 0;

    void Start()
    {
        if (autoFindComponents)
        {
            FindComponents();
        }

        // Run diagnostic at start
        Invoke(nameof(RunFullDiagnostic), 1f);
    }

    void Update()
    {
        if (checkEveryFrame)
        {
            framesSinceLastCheck++;
            if (framesSinceLastCheck > 60) // Check every 60 frames
            {
                QuickCheck();
                framesSinceLastCheck = 0;
            }
        }
    }

    private void FindComponents()
    {
        // Find Canvas
        if (hudCanvas == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas c in canvases)
            {
                if (c.name.Contains("HUD") || c.renderMode == RenderMode.WorldSpace)
                {
                    hudCanvas = c;
                    Debug.Log($"[VRHUDDiagnostic] Auto-found Canvas: {hudCanvas.name}");
                    break;
                }
            }
        }

        // Find EventSystem
        eventSystem = EventSystem.current;

        // Find buttons
        if (buttonsToCheck == null || buttonsToCheck.Length == 0)
        {
            List<GameObject> buttons = new List<GameObject>();

            // Find all objects with VRHUDButton or VRHUDToggleButton components
            VRHUDButton[] hudButtons = FindObjectsOfType<VRHUDButton>();
            foreach (VRHUDButton btn in hudButtons)
            {
                buttons.Add(btn.gameObject);
            }

            VRHUDToggleButton[] toggleButtons = FindObjectsOfType<VRHUDToggleButton>();
            foreach (VRHUDToggleButton btn in toggleButtons)
            {
                buttons.Add(btn.gameObject);
            }

            buttonsToCheck = buttons.ToArray();
            Debug.Log($"[VRHUDDiagnostic] Auto-found {buttonsToCheck.Length} buttons");
        }
    }

    /// <summary>
    /// Run complete diagnostic check
    /// </summary>
    [ContextMenu("Run Full Diagnostic")]
    public void RunFullDiagnostic()
    {
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("<color=cyan><b>VR HUD INTERACTION DIAGNOSTIC REPORT</b></color>");
        Debug.Log("═══════════════════════════════════════════════════");

        FindComponents();

        bool allChecksPassed = true;

        // Check 1: EventSystem
        allChecksPassed &= CheckEventSystem();

        // Check 2: Canvas Setup
        allChecksPassed &= CheckCanvas();

        // Check 3: Cameras
        allChecksPassed &= CheckCameras();

        // Check 4: Button Components
        allChecksPassed &= CheckButtons();

        // Check 5: Raycasting
        allChecksPassed &= CheckRaycasting();

        // Check 6: Input System
        allChecksPassed &= CheckInputSystem();

        Debug.Log("═══════════════════════════════════════════════════");
        if (allChecksPassed)
        {
            Debug.Log("<color=green><b>✓ ALL CHECKS PASSED!</b></color>");
            Debug.Log("If buttons still don't work, try:");
            Debug.Log("  1. Ensure HUD panels are ACTIVE in hierarchy");
            Debug.Log("  2. Check that buttons are in front of camera");
            Debug.Log("  3. Verify reticle is hitting buttons (enable debug ray)");
        }
        else
        {
            Debug.Log("<color=red><b>✗ ISSUES FOUND - See above for details</b></color>");
        }
        Debug.Log("═══════════════════════════════════════════════════");
    }

    /// <summary>
    /// Quick check for continuous monitoring
    /// </summary>
    private void QuickCheck()
    {
        if (hudCanvas == null || eventSystem == null) return;

        Camera eventCam = hudCanvas.worldCamera;
        if (eventCam == null || !eventCam.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[VRHUDDiagnostic] Event Camera is null or inactive!");
        }
    }

    private bool CheckEventSystem()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 1: EventSystem ━━━</color>");

        if (eventSystem == null)
        {
            Debug.LogError("✗ No EventSystem found in scene!");
            Debug.LogError("  FIX: Add EventSystem to scene (GameObject > UI > Event System)");
            return false;
        }

        Debug.Log($"✓ EventSystem found: {eventSystem.name}");

        // Check for InputSystemUIInputModule
        var inputModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule != null)
        {
            Debug.Log("✓ InputSystemUIInputModule is present");
        }
        else
        {
            Debug.LogWarning("⚠ No InputSystemUIInputModule found - using legacy input");
        }

        return true;
    }

    private bool CheckCanvas()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 2: Canvas Setup ━━━</color>");

        if (hudCanvas == null)
        {
            Debug.LogError("✗ No Canvas assigned or found!");
            Debug.LogError("  FIX: Assign HUD Canvas in Inspector");
            return false;
        }

        Debug.Log($"✓ Canvas found: {hudCanvas.name}");

        // Check render mode
        if (hudCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError($"✗ Canvas Render Mode is {hudCanvas.renderMode} - should be WorldSpace!");
            Debug.LogError("  FIX: Set Canvas Render Mode to 'World Space'");
            return false;
        }
        Debug.Log("✓ Canvas Render Mode: World Space");

        // Check Event Camera
        Camera eventCam = hudCanvas.worldCamera;
        if (eventCam == null)
        {
            Debug.LogError("✗ Canvas Event Camera is NOT SET!");
            Debug.LogError("  FIX: Add VRCanvasEventCameraSwitcher script to Canvas");
            return false;
        }

        Debug.Log($"✓ Event Camera set: {eventCam.name}");

        if (!eventCam.gameObject.activeInHierarchy)
        {
            Debug.LogError($"✗ Event Camera '{eventCam.name}' is INACTIVE!");
            Debug.LogError("  FIX: Ensure the active camera is enabled");
            return false;
        }
        Debug.Log("✓ Event Camera is active");

        // Check for Graphic Raycaster
        GraphicRaycaster raycaster = hudCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            Debug.LogError("✗ No GraphicRaycaster on Canvas!");
            Debug.LogError("  FIX: Add GraphicRaycaster component to Canvas");
            return false;
        }
        Debug.Log("✓ GraphicRaycaster present");

        // Check Canvas Scaler
        CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            Debug.Log($"  Canvas scale: {hudCanvas.transform.localScale}");
            if (hudCanvas.transform.localScale.x < 0.01f)
            {
                Debug.LogWarning("⚠ Canvas scale is very small - might cause precision issues");
            }
        }

        // Check VRCanvasEventCameraSwitcher
        var switcher = hudCanvas.GetComponent<VRCanvasEventCameraSwitcher>();
        if (switcher == null)
        {
            Debug.LogWarning("⚠ VRCanvasEventCameraSwitcher NOT FOUND!");
            Debug.LogWarning("  RECOMMENDED: Add VRCanvasEventCameraSwitcher to automatically switch cameras");
        }
        else
        {
            Debug.Log("✓ VRCanvasEventCameraSwitcher present");
        }

        return true;
    }

    private bool CheckCameras()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 3: Cameras ━━━</color>");

        Camera[] allCameras = FindObjectsOfType<Camera>();
        int activeCameras = 0;
        Camera activeCamera = null;

        foreach (Camera cam in allCameras)
        {
            if (cam.gameObject.activeInHierarchy && cam.enabled)
            {
                activeCameras++;
                activeCamera = cam;
                Debug.Log($"✓ Active camera: {cam.name}");
            }
        }

        if (activeCameras == 0)
        {
            Debug.LogError("✗ No active cameras found!");
            return false;
        }

        if (activeCameras > 1)
        {
            Debug.LogWarning($"⚠ Multiple active cameras found ({activeCameras})");
            Debug.LogWarning("  This might cause issues - ensure only one camera is active per mode");
        }

        // Check if Canvas Event Camera matches active camera
        if (hudCanvas != null && activeCamera != null)
        {
            if (hudCanvas.worldCamera != activeCamera)
            {
                Debug.LogError($"✗ Canvas Event Camera mismatch!");
                Debug.LogError($"  Canvas uses: {hudCanvas.worldCamera?.name ?? "NULL"}");
                Debug.LogError($"  Active camera: {activeCamera.name}");
                Debug.LogError("  FIX: Ensure VRCanvasEventCameraSwitcher is working");
                return false;
            }
            else
            {
                Debug.Log("✓ Canvas Event Camera matches active camera");
            }
        }

        return true;
    }

    private bool CheckButtons()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 4: Button Components ━━━</color>");

        if (buttonsToCheck == null || buttonsToCheck.Length == 0)
        {
            Debug.LogWarning("⚠ No buttons to check");
            return true;
        }

        bool allButtonsValid = true;

        foreach (GameObject buttonObj in buttonsToCheck)
        {
            if (buttonObj == null)
            {
                Debug.LogWarning("⚠ Button reference is null");
                continue;
            }

            Debug.Log($"\nChecking button: {buttonObj.name}");

            // Check if button is active
            if (!buttonObj.activeInHierarchy)
            {
                Debug.LogError($"  ✗ Button '{buttonObj.name}' is INACTIVE!");
                allButtonsValid = false;
                continue;
            }

            // Check for interaction components
            bool hasInteractionComponent = false;

            if (buttonObj.GetComponent<VRHUDButton>() != null)
            {
                Debug.Log("  ✓ Has VRHUDButton");
                hasInteractionComponent = true;
            }

            if (buttonObj.GetComponent<VRHUDToggleButton>() != null)
            {
                Debug.Log("  ✓ Has VRHUDToggleButton");
                hasInteractionComponent = true;
            }

            if (buttonObj.GetComponent<Button>() != null)
            {
                Debug.Log("  ✓ Has UI Button");
                hasInteractionComponent = true;
            }

            if (!hasInteractionComponent)
            {
                Debug.LogWarning($"  ⚠ No interaction component found on '{buttonObj.name}'");
            }

            // Check for Box Collider (if using 3D interaction)
            BoxCollider boxCollider = buttonObj.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                if (boxCollider.isTrigger)
                {
                    Debug.Log("  ✓ Has Box Collider (trigger)");
                }
                else
                {
                    Debug.LogWarning("  ⚠ Box Collider is not set as trigger");
                }
            }

            // Check for Graphic component (for UI raycasting)
            Graphic graphic = buttonObj.GetComponent<Graphic>();
            if (graphic != null)
            {
                if (graphic.raycastTarget)
                {
                    Debug.Log("  ✓ Has raycastable Graphic");
                }
                else
                {
                    Debug.LogWarning("  ⚠ Graphic raycastTarget is disabled");
                }
            }
        }

        return allButtonsValid;
    }

    private bool CheckRaycasting()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 5: Raycasting ━━━</color>");

        if (eventSystem == null || hudCanvas == null)
        {
            Debug.LogWarning("⚠ Cannot test raycasting - missing EventSystem or Canvas");
            return false;
        }

        // Test raycast from screen center
        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.LogWarning("⚠ No UI elements hit by center raycast");
            Debug.LogWarning("  This might be normal if HUD is not in center of view");
            Debug.LogWarning("  Try looking directly at a button and re-run diagnostic");
            return true; // Not a failure, just informational
        }

        Debug.Log($"✓ Raycast hit {results.Count} UI element(s) at screen center:");
        foreach (RaycastResult result in results)
        {
            Debug.Log($"  - {result.gameObject.name} (Distance: {result.distance:F2})");
        }

        return true;
    }

    private bool CheckInputSystem()
    {
        Debug.Log("\n<color=yellow>━━━ CHECK 6: Input System ━━━</color>");

        // Check for VRReticlePointer
        VRReticlePointer[] reticlePointers = FindObjectsOfType<VRReticlePointer>();
        if (reticlePointers.Length == 0)
        {
            Debug.LogWarning("⚠ No VRReticlePointer found in scene");
            Debug.LogWarning("  FIX: Add VRReticlePointer to camera or Player");
            return false;
        }

        Debug.Log($"✓ Found {reticlePointers.Length} VRReticlePointer(s)");

        foreach (VRReticlePointer pointer in reticlePointers)
        {
            if (pointer.gameObject.activeInHierarchy)
            {
                Debug.Log($"  ✓ Active: {pointer.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"  ⚠ Inactive: {pointer.gameObject.name}");
            }
        }

        return true;
    }

    /// <summary>
    /// Test if a specific button can receive pointer events
    /// </summary>
    [ContextMenu("Test Button Under Mouse")]
    public void TestButtonUnderMouse()
    {
        if (eventSystem == null)
        {
            Debug.LogError("No EventSystem found!");
            return;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem);
        pointerData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log("No UI elements under mouse");
            return;
        }

        Debug.Log($"<color=cyan>UI elements under mouse:</color>");
        foreach (RaycastResult result in results)
        {
            Debug.Log($"  {result.gameObject.name}");
        }
    }
}