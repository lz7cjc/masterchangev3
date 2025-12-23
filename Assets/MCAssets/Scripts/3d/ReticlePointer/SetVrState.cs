using UnityEngine;
using System.Collections;

/// <summary>
/// CLEANED: VR mode toggle controller
/// Handles UI button for switching between VR and 360 modes
/// Works with togglingXRFilm to do the actual switching
/// </summary>
public class SetVrState : MonoBehaviour
{
    [Header("UI Sprites")]
    [Tooltip("Sprite shown when VR mode is available")]
    [SerializeField] private Sprite spritePickedVR;
    
    [Tooltip("Sprite shown when in VR mode (to switch back to 360)")]
    [SerializeField] private Sprite spritePickedNoVR;
    
    [Tooltip("The sprite renderer that displays the toggle icon")]
    public SpriteRenderer spriterendererVR;

    // Internal references
    private togglingXR xrToggler;
    private VRLoadingManager loadingManager;

    private void Start()
    {
        // Find references
        xrToggler = FindObjectOfType<togglingXR>();
        
        // Try to find loading manager (may not exist)
        loadingManager = FindObjectOfType<VRLoadingManager>();

        // Load saved VR preference
        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[SetVrState] Starting with VR mode: {headsetOr2D}");

        // Update sprite to show current mode
        UpdateVRSprites(headsetOr2D == 1);
    }

    /// <summary>
    /// Set VR mode: 0 = 360 mode, 1 = VR mode
    /// Called by UI button or other scripts
    /// </summary>
    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"[SetVrState] SetVR called with state: {headsetOr2D}");

        // Save preference
        PlayerPrefs.SetInt("toggleToVR", headsetOr2D);
        PlayerPrefs.Save();

        // Update sprite immediately
        UpdateVRSprites(headsetOr2D == 1);

        // Show loading screen if available
        if (loadingManager != null)
        {
            if (headsetOr2D == 1)
            {
                loadingManager.ShowSwitchToVR();
            }
            else
            {
                loadingManager.ShowSwitchTo360();
            }
        }

        // Start mode switch
        StartCoroutine(SwitchModeWithLoading(headsetOr2D));
    }

    /// <summary>
    /// Switch mode with loading coordination
    /// </summary>
    private IEnumerator SwitchModeWithLoading(int headsetOr2D)
    {
        bool isVRMode = headsetOr2D == 1;

        // Update progress if loading manager exists
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.1f);
        }

        // Switch XR mode via togglingXRFilm
        if (xrToggler != null)
        {
            if (isVRMode)
            {
                yield return xrToggler.StartXR();
            }
            else
            {
                xrToggler.StopXR();
                yield return new WaitForEndOfFrame();
            }
        }
        else
        {
            Debug.LogError("[SetVrState] togglingXRFilm not found! Cannot switch modes.");
            yield break;
        }

        // Update progress
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.7f);
        }

        // Wait for settling
        yield return new WaitForEndOfFrame();

        // Update progress
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.9f);
        }

        // Brief delay
        yield return new WaitForSeconds(0.2f);

        // Complete and hide loading
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            yield return new WaitForSeconds(0.2f);
            loadingManager.HideLoading();
        }

        Debug.Log($"[SetVrState] Mode switch complete. VR Mode: {isVRMode}");
    }

    /// <summary>
    /// Update sprite to show current mode
    /// </summary>
    private void UpdateVRSprites(bool isVRMode)
    {
        if (spriterendererVR != null)
        {
            spriterendererVR.sprite = isVRMode ? spritePickedNoVR : spritePickedVR;
            Debug.Log($"[SetVrState] Updated sprite. Is VR Mode: {isVRMode}");
        }
    }

    /// <summary>
    /// Toggle VR mode - can be called from UI button
    /// </summary>
    public void ToggleVRMode()
    {
        int currentMode = PlayerPrefs.GetInt("toggleToVR", 0);
        int newMode = currentMode == 1 ? 0 : 1;

        Debug.Log($"[SetVrState] Toggling VR from {currentMode} to {newMode}");
        SetVR(newMode);
    }
}
