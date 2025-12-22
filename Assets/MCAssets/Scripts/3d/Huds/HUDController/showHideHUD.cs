using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Updated showHideHUD - Compatible with VRReticlePointer Event Trigger system
/// NOW WORKS WITH: BaseEventData from Event Triggers
/// </summary>
public class showHideHUD : MonoBehaviour
{
    // Public fields - same as before
    public bool mousehover = false;
    public float Counter = 0;
    public float waitFor = 3f;
    public static bool showing = false;
    public GameObject openCloseLevel1;
    public GameObject mainIconsLevel2;
    public GameObject locationsLevel3;
    public GameObject turnHudOn;
    public GameObject turnHudOff;

    private hudCountdown hudCountdown;
    private HUDSystemCoordinator hudCoordinator;

    public void Start()
    {
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();

        if (hudCountdown == null)
        {
            hudCountdown = FindFirstObjectByType<hudCountdown>();
        }

        if (mainIconsLevel2 != null) mainIconsLevel2.SetActive(false);
        if (locationsLevel3 != null) locationsLevel3.SetActive(false);
        Counter = 0;
        mousehover = false;

        Debug.Log("Updated showHideHUD initialized (VRReticlePointer compatible)");
    }

    public void resetShow()
    {
        showing = true;
    }

    public void ResetHUDState()
    {
        if (hudCoordinator != null)
        {
            hudCoordinator.ResetAllHUDStates();
        }
        else
        {
            if (mainIconsLevel2 != null) mainIconsLevel2.SetActive(false);
            if (locationsLevel3 != null) locationsLevel3.SetActive(false);
            if (turnHudOff != null) turnHudOff.SetActive(false);
            if (turnHudOn != null) turnHudOn.SetActive(true);

            showing = false;
        }

        ResetHoverState();
    }

    void Update()
    {
        if (mousehover)
        {
            Counter += Time.deltaTime;

            // Log timer progress every 0.5 seconds
            if (Mathf.FloorToInt(Counter * 2) > Mathf.FloorToInt((Counter - Time.deltaTime) * 2))
            {
                Debug.Log($"HUD Timer: {Counter:F1}s / {waitFor}s");
            }

            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(waitFor, Counter);
            }

            if (Counter >= waitFor)
            {
                Debug.Log("HUD Timer completed! Triggering directClick()");
                mousehover = false;
                Counter = 0;
                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }
                directClick();
            }
        }
    }

    // ============================================
    // NEW: Event Trigger compatible methods
    // These accept BaseEventData from VRReticlePointer
    // ============================================

    /// <summary>
    /// Called by Event Trigger - Pointer Enter
    /// Accepts BaseEventData (compatible with VRReticlePointer)
    /// </summary>
    public void MouseHoverChangeScene(BaseEventData eventData)
    {
        Debug.Log($"MouseHoverChangeScene (EventData) - Current mousehover: {mousehover}, Counter: {Counter}");

        // Only reset if not already hovering to prevent timer resets
        if (!mousehover)
        {
            mousehover = true;
            Counter = 0;
            Debug.Log("Starting HUD hover timer");
        }
        else
        {
            Debug.Log("Already hovering - not resetting timer");
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverStarted();
        }
    }

    /// <summary>
    /// Called by Event Trigger - Pointer Exit
    /// Accepts BaseEventData (compatible with VRReticlePointer)
    /// </summary>
    public void MouseExit(BaseEventData eventData)
    {
        Debug.Log($"MouseExit (EventData) - Was hovering: {mousehover}, Counter was: {Counter}");

        mousehover = false;
        Counter = 0;
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDHoverEnded();
        }
    }

    // ============================================
    // LEGACY: Keep old methods for backward compatibility
    // These are the no-parameter versions
    // ============================================

    /// <summary>
    /// Legacy version - no parameters
    /// Kept for backward compatibility
    /// </summary>
    public void MouseHoverChangeScene()
    {
        MouseHoverChangeScene(null);
    }

    /// <summary>
    /// Legacy version - no parameters
    /// Kept for backward compatibility
    /// </summary>
    public void MouseExit()
    {
        MouseExit(null);
    }

    public void directClick()
    {
        Debug.Log($"directClick called - Current showing state: {showing}");

        if (!showing)
        {
            Debug.Log("Opening HUD (showing = false, opening Level2)");
            if (hudCoordinator != null)
            {
                hudCoordinator.OnLevel1ItemSelected();
                hudCoordinator.OpenMainHUD();
            }
            else
            {
                Debug.Log("Using fallback method to open HUD");
                if (mainIconsLevel2 != null)
                {
                    mainIconsLevel2.SetActive(true);
                    Debug.Log($"Set {mainIconsLevel2.name} to active");
                }
                if (locationsLevel3 != null) locationsLevel3.SetActive(false);
                if (turnHudOff != null) turnHudOff.SetActive(true);
                if (turnHudOn != null) turnHudOn.SetActive(false);
            }
            showing = true;
            Debug.Log("Level 1 opened: Level 2 shown, Level 3 closed");
        }
        else
        {
            Debug.Log("Closing HUD (showing = true, closing all levels)");
            ResetHUDState();
            Debug.Log("Level 1 closed: All levels closed");
        }

        ResetHoverState();

        if (hudCoordinator != null)
        {
            hudCoordinator.OnMainHUDToggled(showing);
        }

        Debug.Log($"directClick completed - New showing state: {showing}");
    }

    private void ResetHoverState()
    {
        Debug.Log("ResetHoverState called");
        mousehover = false;
        Counter = 0;
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    public float HoverProgress
    {
        get { return mousehover ? Mathf.Clamp01(Counter / waitFor) : 0f; }
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
            Debug.Log("Manual direct click triggered");
            directClick();
        }
    }
}