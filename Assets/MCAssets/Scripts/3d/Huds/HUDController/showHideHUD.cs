using UnityEngine;
using TMPro;

/// <summary>
/// CLEANED showHideHUD - Works with GazeHoverTrigger
/// Removed conflicting timer logic - GazeHoverTrigger handles all timing
/// </summary>
public class showHideHUD : MonoBehaviour
{
    [Header("HUD State")]
    public static bool showing = false;

    [Header("HUD Level References")]
    public GameObject openCloseLevel1;
    public GameObject mainIconsLevel2;
    public GameObject locationsLevel3;
    public GameObject turnHudOn;
    public GameObject turnHudOff;

    private HUDSystemCoordinator hudCoordinator;

    void Start()
    {
        hudCoordinator = FindObjectOfType<HUDSystemCoordinator>();

        // Initialize - all levels closed
        if (mainIconsLevel2 != null) mainIconsLevel2.SetActive(false);
        if (locationsLevel3 != null) locationsLevel3.SetActive(false);
        
        showing = false;

        Debug.Log("[showHideHUD] Initialized - Works with GazeHoverTrigger");
    }

    /// <summary>
    /// Toggle HUD open/closed
    /// Called by GazeHoverTrigger On Hover Complete event
    /// </summary>
    public void directClick()
    {
        Debug.Log($"[showHideHUD] directClick - Current state: {showing}");

        if (!showing)
        {
            // Open HUD
            OpenHUD();
        }
        else
        {
            // Close HUD
            CloseHUD();
        }

        Debug.Log($"[showHideHUD] New state: {showing}");
    }

    private void OpenHUD()
    {
        Debug.Log("[showHideHUD] Opening HUD");

        if (hudCoordinator != null)
        {
            hudCoordinator.OnLevel1ItemSelected();
            hudCoordinator.OpenMainHUD();
        }
        else
        {
            // Fallback if no coordinator
            if (mainIconsLevel2 != null)
            {
                mainIconsLevel2.SetActive(true);
                Debug.Log($"[showHideHUD] Activated {mainIconsLevel2.name}");
            }
            if (locationsLevel3 != null) locationsLevel3.SetActive(false);
            if (turnHudOff != null) turnHudOff.SetActive(true);
            if (turnHudOn != null) turnHudOn.SetActive(false);
        }

        showing = true;
    }

    private void CloseHUD()
    {
        Debug.Log("[showHideHUD] Closing HUD");

        if (hudCoordinator != null)
        {
            hudCoordinator.ResetAllHUDStates();
        }
        else
        {
            // Fallback if no coordinator
            if (mainIconsLevel2 != null) mainIconsLevel2.SetActive(false);
            if (locationsLevel3 != null) locationsLevel3.SetActive(false);
            if (turnHudOff != null) turnHudOff.SetActive(false);
            if (turnHudOn != null) turnHudOn.SetActive(true);
        }

        showing = false;
    }

    public void ResetHUDState()
    {
        Debug.Log("[showHideHUD] ResetHUDState called");
        CloseHUD();
    }

    public bool IsShowing
    {
        get { return showing; }
    }

    // DEBUG: Manual trigger for testing
    [ContextMenu("Force Direct Click")]
    private void ForceDirectClick()
    {
        if (Application.isPlaying)
        {
            Debug.Log("[showHideHUD] Manual direct click");
            directClick();
        }
    }
}
