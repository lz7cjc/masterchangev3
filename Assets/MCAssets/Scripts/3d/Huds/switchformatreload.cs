using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR/360 mode switching with hover countdown and icon effects
/// Uses scene reload method (the working approach)
/// </summary>
public class switchformatreload : MonoBehaviour
{
    [Header("Hover Countdown Settings")]
    [SerializeField] private float delay = 3f;
    [SerializeField] private hudCountdown hudCountdown;

    [Header("Icon Toggle Reference")]
    [SerializeField] private ToggleActiveIconsVR toggleActiveIconsVR;

    [Header("Scene Management")]
    // Scene will be reloaded using SceneManager.LoadScene

    [Header("Loading Screen")]
    [SerializeField] private VRLoadingManager loadingManager;

    private bool mousehover;
    private float Counter;
    private int formatVR;

    void Start()
    {
        Debug.Log("[VRLOAD] switchformatreload initialized (scene reload version)");

        // Find dependencies if not assigned
        if (hudCountdown == null)
        {
            hudCountdown = FindFirstObjectByType<hudCountdown>();
            if (hudCountdown == null)
            {
                Debug.LogWarning("[VRLOAD] hudCountdown not found - countdown display will not work");
            }
        }

        if (toggleActiveIconsVR == null)
        {
            toggleActiveIconsVR = GetComponent<ToggleActiveIconsVR>();
            if (toggleActiveIconsVR == null)
            {
                Debug.LogWarning("[VRLOAD] ToggleActiveIconsVR not found - icon hover effects will not work");
            }
        }

        if (loadingManager == null)
        {
            loadingManager = VRLoadingManager.Instance;
            if (loadingManager == null)
            {
                Debug.LogWarning("[VRLOAD] VRLoadingManager not found - no loading screen will show");
            }
        }
    }

    void Update()
    {
        if (mousehover)
        {
            Counter += Time.deltaTime;

            // Update countdown display
            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(delay, Counter);
            }

            // Trigger scene reload when delay is reached
            if (Counter >= delay)
            {
                Counter = 0;
                mousehover = false;

                // Reset countdown display
                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }

                // Toggle PlayerPrefs and reload scene
                TriggerModeSwitch();
            }
        }
    }

    /// <summary>
    /// Called by Event Trigger: Pointer Enter
    /// </summary>
    public void MouseHoverChangeScene()
    {
        Debug.Log("[VRLOAD] Mouse hover started");
        mousehover = true;
        formatVR = PlayerPrefs.GetInt("toggleToVR");

        // Trigger hover icon effect
        if (toggleActiveIconsVR != null)
        {
            toggleActiveIconsVR.HoverIcon();
        }
    }

    /// <summary>
    /// Called by Event Trigger: Pointer Exit
    /// </summary>
    public void MouseExit()
    {
        Debug.Log("[VRLOAD] Mouse hover ended");

        // Reset countdown
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }

        mousehover = false;
        Counter = 0;

        // Reset to default icon
        if (toggleActiveIconsVR != null)
        {
            toggleActiveIconsVR.DefaultIcon();
        }
    }

    /// <summary>
    /// Toggle PlayerPrefs and reload scene (the working method)
    /// </summary>
    private void TriggerModeSwitch()
    {
        bool isCurrentlyVR = (formatVR == 1);

        Debug.Log($"[VRLOAD] ========================================");
        Debug.Log($"[VRLOAD] MODE SWITCH TRIGGERED");
        Debug.Log($"[VRLOAD] Current mode: {(isCurrentlyVR ? "VR" : "360")}");

        // Toggle PlayerPrefs
        if (formatVR == 1)
        {
            Debug.Log("[VRLOAD] Switching: VR → 360");
            PlayerPrefs.SetInt("toggleToVR", 0);

            // Show loading screen for 360 mode
            if (loadingManager != null)
            {
                loadingManager.ShowSwitchTo360();
            }
        }
        else
        {
            Debug.Log("[VRLOAD] Switching: 360 → VR");
            PlayerPrefs.SetInt("toggleToVR", 1);

            // Show loading screen for VR mode
            if (loadingManager != null)
            {
                loadingManager.ShowSwitchToVR();
            }
        }

        PlayerPrefs.Save();
        Debug.Log($"[VRLOAD] ✓ PlayerPrefs updated: toggleToVR = {PlayerPrefs.GetInt("toggleToVR")}");

        // Brief delay to ensure loading screen is visible before reload
        StartCoroutine(ReloadSceneAfterDelay());
    }

    /// <summary>
    /// Reload scene after brief delay to ensure loading screen shows
    /// </summary>
    private IEnumerator ReloadSceneAfterDelay()
    {
        Debug.Log("[VRLOAD] Waiting for loading screen to display...");
        yield return new WaitForSeconds(0.2f);

        Debug.Log("[VRLOAD] Reloading scene...");

        // Get current scene name and reload it
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        Debug.Log("[VRLOAD] ========================================");
    }
}