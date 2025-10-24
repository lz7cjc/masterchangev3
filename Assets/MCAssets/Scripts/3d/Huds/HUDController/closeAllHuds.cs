using UnityEngine;

/// <summary>
/// Enhanced closeAllHuds - Fixed version with no duplicate validation
/// </summary>
public class closeAllHuds : MonoBehaviour
{
    [Header("HUD References")]
    public GameObject hud1PlusOff;
    public GameObject hud1PlusOn;
    public GameObject hud1Level2;
    public GameObject hud1Level3a;
    public GameObject hud1Level3b;

    private showHideHUD hudController;
    private showHideHUDcat hudCatController;
    private HUDSystemCoordinator hudCoordinator;
    private bool hasValidated = false;

    public void Start()
    {
        Debug.Log("[closeAllHuds] Start method called");

        // Try to find enhanced coordinator first
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();

        if (hudCoordinator == null)
        {
            // Fall back to finding individual controllers
            hudController = FindFirstObjectByType<showHideHUD>();
            hudCatController = FindFirstObjectByType<showHideHUDcat>();
        }

        // Validate assignments ONCE
        if (!hasValidated)
        {
            ValidateAssignments();
            hasValidated = true;
        }

        CloseTheHuds();
    }

    /// <summary>
    /// Close all HUDs
    /// </summary>
    public void CloseTheHuds()
    {
        Debug.Log("[closeAllHuds] CloseTheHuds called");

        // If we have a coordinator, use it
        if (hudCoordinator != null)
        {
            Debug.Log("[closeAllHuds] Using HUDSystemCoordinator to close all HUDs");
            hudCoordinator.CloseAllHUDs();
            return;
        }

        // Otherwise use individual controllers
        if (hudController != null)
        {
            Debug.Log("[closeAllHuds] Using showHideHUD to reset HUD state");
            hudController.ResetHUDState();
        }
        else
        {
            // Fallback - directly manage HUD GameObjects
            Debug.Log("[closeAllHuds] Fallback - Directly deactivating HUD elements");
            SafeSetActive(hud1Level2, false);
            SafeSetActive(hud1Level3a, false);
            SafeSetActive(hud1Level3b, false);
            SafeSetActive(hud1PlusOff, false);
            SafeSetActive(hud1PlusOn, true);
        }

        if (hudCatController != null)
        {
            Debug.Log("[closeAllHuds] Using showHideHUDcat to reset HUD state");
            hudCatController.ResetHUDState();
        }
    }

    /// <summary>
    /// Close HUDs with specific reason (for debugging)
    /// </summary>
    public void CloseTheHuds(string reason)
    {
        Debug.Log($"[closeAllHuds] Closing HUDs - Reason: {reason}");
        CloseTheHuds();
    }

    /// <summary>
    /// Force immediate close without validation
    /// </summary>
    public void ForceCloseAllHUDs()
    {
        if (hudCoordinator != null)
        {
            hudCoordinator.CloseAllHUDs();
        }
        else
        {
            // Force close by name search
            GameObject[] allObjects = FindObjectsOfType<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj.name.Contains("Level2") || obj.name.Contains("mainIcons"))
                {
                    obj.SetActive(false);
                }
                else if (obj.name.Contains("Level3"))
                {
                    obj.SetActive(false);
                }
            }
        }

        Debug.Log("[closeAllHuds] Force closed all HUDs");
    }

    /// <summary>
    /// Get status of HUD assignments
    /// </summary>
    public string GetAssignmentStatus()
    {
        return $"HUD Assignment Status:\n" +
               $"- Level2: {(hud1Level2 != null ? "✓" : "✗")}\n" +
               $"- Level3a: {(hud1Level3a != null ? "✓" : "✗")}\n" +
               $"- Level3b: {(hud1Level3b != null ? "✓" : "✗")}\n" +
               $"- Plus Off: {(hud1PlusOff != null ? "✗" : "✗")}\n" +
               $"- Plus On: {(hud1PlusOn != null ? "✓" : "✗")}\n" +
               $"- Coordinator: {(hudCoordinator != null ? "✓" : "✗")}";
    }

    #region Private Methods

    private void ValidateAssignments()
    {
        bool allAssigned = true;

        if (hud1Level2 == null)
        {
            Debug.LogWarning("[closeAllHuds] hud1Level2 is not assigned! Try assigning 'Level2' GameObject.");
            allAssigned = false;
        }
        if (hud1Level3a == null)
        {
            Debug.LogWarning("[closeAllHuds] hud1Level3a is not assigned! Try assigning 'Level3a' GameObject.");
            allAssigned = false;
        }
        if (hud1Level3b == null)
        {
            Debug.LogWarning("[closeAllHuds] hud1Level3b is not assigned! Try assigning 'Level3b' GameObject.");
            allAssigned = false;
        }
        if (hud1PlusOff == null)
        {
            Debug.LogWarning("[closeAllHuds] hud1PlusOff is not assigned! Try assigning 'closeHud' GameObject.");
            allAssigned = false;
        }
        if (hud1PlusOn == null)
        {
            Debug.LogWarning("[closeAllHuds] hud1PlusOn is not assigned! Try assigning 'opendefault' GameObject.");
            allAssigned = false;
        }

        if (allAssigned)
        {
            Debug.Log("[closeAllHuds] All HUD references are assigned correctly ✓");
        }
        else
        {
            Debug.Log("[closeAllHuds] Some HUD references are missing - fallback behavior will be used");
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

    #region Inspector Context Menu Helpers

    [ContextMenu("Auto-Find HUD References")]
    private void AutoFindHUDReferences()
    {
        if (hud1Level2 == null)
            hud1Level2 = GameObject.Find("Level2");

        if (hud1Level3a == null)
            hud1Level3a = GameObject.Find("Level3a");

        if (hud1Level3b == null)
            hud1Level3b = GameObject.Find("Level3b");

        if (hud1PlusOff == null)
            hud1PlusOff = GameObject.Find("closeHud");

        if (hud1PlusOn == null)
            hud1PlusOn = GameObject.Find("opendefault");

        Debug.Log($"[closeAllHuds] Auto-found references:\n{GetAssignmentStatus()}");
    }

    [ContextMenu("Test Close HUDs")]
    private void TestCloseHUDs()
    {
        if (Application.isPlaying)
        {
            CloseTheHuds("Manual Test from Context Menu");
        }
        else
        {
            Debug.LogWarning("[closeAllHuds] Test Close HUDs can only be used in Play Mode");
        }
    }

    [ContextMenu("Show Assignment Status")]
    private void ShowAssignmentStatus()
    {
        Debug.Log($"[closeAllHuds]\n{GetAssignmentStatus()}");
    }

    #endregion
}