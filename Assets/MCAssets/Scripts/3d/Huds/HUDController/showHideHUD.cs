using UnityEngine;
using TMPro;

/// <summary>
/// Debug enhanced showHideHUD - Added logging to diagnose timer issues
/// </summary>
public class showHideHUD : MonoBehaviour
{
    // Exact same public fields as original
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

        Debug.Log("Enhanced showHideHUD initialized");
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

            // Note: Static state management is handled by coordinator
            // Only set if coordinator is not available
            showing = false;
        }

        ResetHoverState();
    }

    void Update()
    {
        if (mousehover)
        {
            Counter += Time.deltaTime;

            // DEBUG: Log timer progress every 0.5 seconds
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

    public void MouseHoverChangeScene()
    {
        Debug.Log($"MouseHoverChangeScene called - Current mousehover: {mousehover}, Counter: {Counter}");

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

    public void MouseExit()
    {
        Debug.Log($"MouseExit called - Was hovering: {mousehover}, Counter was: {Counter}");

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