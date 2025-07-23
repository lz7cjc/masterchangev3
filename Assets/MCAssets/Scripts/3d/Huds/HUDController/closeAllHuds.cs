using UnityEngine;

/// <summary>
/// Enhanced closeAllHuds - Cleaned up version with reduced redundancy
/// </summary>
public class closeAllHuds : MonoBehaviour
{
    // Exact same public fields as original
    public GameObject hud1PlusOff;
    public GameObject hud1PlusOn;
    public GameObject hud1Level2;
    public GameObject hud1Level3;

    private showHideHUD hudController;
    private showHideHUDcat hudCatController;
    private HUDSystemCoordinator hudCoordinator;

    public void Start()
    {
        Debug.Log("Marker: Enhanced closeAllHuds Start method called");

        // Try to find enhanced coordinator first
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();

        if (hudCoordinator == null)
        {
            // Fall back to finding individual controllers
            hudController = FindFirstObjectByType<showHideHUD>();
            hudCatController = FindFirstObjectByType<showHideHUDcat>();
        }

        // Validate assignments with enhanced error reporting
        ValidateAssignments();

        CloseTheHuds();
    }

    // Exact same public method as original
    public void CloseTheHuds()
    {
        Debug.Log("Marker: Enhanced CloseTheHuds called");

        if (hudCoordinator != null)
        {
            Debug.Log("Marker: Using HUDSystemCoordinator to close all HUDs");
            hudCoordinator.CloseAllHUDs();
            return;
        }

        // Enhanced validation with better error reporting
        ValidateAssignments();

        // Enhanced closing logic with better coordination
        if (hudController != null)
        {
            Debug.Log("Marker: Resetting HUD state for enhanced showHideHUD");
            hudController.ResetHUDState();
        }
        else
        {
            // Enhanced fallback logic
            Debug.Log("Marker: Enhanced fallback - Deactivating HUD elements");
            SafeSetActive(hud1Level2, false);
            SafeSetActive(hud1Level3, false);
            SafeSetActive(hud1PlusOff, false);
            SafeSetActive(hud1PlusOn, true);
        }

        if (hudCatController != null)
        {
            Debug.Log("Marker: Resetting HUD state for enhanced showHideHUDcat");
            hudCatController.ResetHUDState();
        }

        // Let coordinator handle static state management
        // Removed duplicate static state reset to avoid conflicts
    }

    #region Enhanced Internal Implementation

    private void ValidateAssignments()
    {
        // Enhanced validation with suggestions
        if (hud1Level2 == null)
        {
            Debug.LogError("Marker: hud1Level2 is not assigned! Try assigning 'Level2' GameObject.");
        }
        if (hud1Level3 == null)
        {
            Debug.LogError("Marker: hud1Level3 is not assigned! Try assigning main Level3 container.");
        }
        if (hud1PlusOff == null)
        {
            Debug.LogError("Marker: hud1PlusOff is not assigned! Try assigning 'closeHud' GameObject.");
        }
        if (hud1PlusOn == null)
        {
            Debug.LogError("Marker: hud1PlusOn is not assigned! Try assigning 'opendefault' GameObject.");
        }
    }

    private void SafeSetActive(GameObject obj, bool state)
    {
        if (obj != null)
        {
            obj.SetActive(state);
        }
    }

    #endregion

    #region Enhanced Public Methods (New functionality)

    /// <summary>
    /// Close HUDs with specific reason (new functionality)
    /// </summary>
    public void CloseTheHuds(string reason)
    {
        Debug.Log($"Marker: Closing HUDs - Reason: {reason}");
        CloseTheHuds();
    }

    /// <summary>
    /// Force immediate close without validation (new functionality)
    /// </summary>
    public void ForceCloseAllHUDs()
    {
        if (hudCoordinator != null)
        {
            hudCoordinator.CloseAllHUDs();
        }
        else
        {
            // Force close everything we can find
            var allLevel2 = FindObjectsOfType<GameObject>();
            var allLevel3 = FindObjectsOfType<GameObject>();

            foreach (var obj in allLevel2)
            {
                if (obj.name.Contains("Level2") || obj.name.Contains("mainIcons"))
                {
                    obj.SetActive(false);
                }
            }

            foreach (var obj in allLevel3)
            {
                if (obj.name.Contains("Level3"))
                {
                    obj.SetActive(false);
                }
            }
        }

        Debug.Log("Marker: Force closed all HUDs");
    }

    /// <summary>
    /// Get status of HUD assignments (new functionality)
    /// </summary>
    public string GetAssignmentStatus()
    {
        return $"HUD Assignment Status:\n" +
               $"- Level2: {(hud1Level2 != null ? "✓" : "✗")}\n" +
               $"- Level3: {(hud1Level3 != null ? "✓" : "✗")}\n" +
               $"- Plus Off: {(hud1PlusOff != null ? "✓" : "✗")}\n" +
               $"- Plus On: {(hud1PlusOn != null ? "✓" : "✗")}\n" +
               $"- Coordinator: {(hudCoordinator != null ? "✓" : "✗")}";
    }

    #endregion

    #region Inspector Helpers

    [ContextMenu("Auto-Find HUD References")]
    private void AutoFindHUDReferences()
    {
        if (hud1Level2 == null)
            hud1Level2 = GameObject.Find("Level2");

        if (hud1Level3 == null)
            hud1Level3 = GameObject.Find("Level3a"); // or whatever your main Level3 is called

        if (hud1PlusOff == null)
            hud1PlusOff = GameObject.Find("closeHud");

        if (hud1PlusOn == null)
            hud1PlusOn = GameObject.Find("opendefault");

        Debug.Log($"Auto-found references: {GetAssignmentStatus()}");
    }

    [ContextMenu("Test Close HUDs")]
    private void TestCloseHUDs()
    {
        if (Application.isPlaying)
        {
            CloseTheHuds("Manual Test");
        }
    }

    #endregion
}