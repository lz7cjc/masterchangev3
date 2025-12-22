using UnityEngine;
using System.Collections;

/// <summary>
/// UPDATED: Coordinates VR mode switching with GazeReticlePointer
/// Works with VRLoadingManager and togglingXRFilm
/// </summary>
public class SetVrState : MonoBehaviour
{
    [Header("UI Sprites")]
    [SerializeField] private Sprite spritePickedVR;
    [SerializeField] private Sprite spritePickedNoVR;
    [SerializeField] public SpriteRenderer spriterendererVR;

    [Header("Cameras")]
    [SerializeField] private GameObject mainCamera2D;
    [SerializeField] private GameObject mainCameraVR;

    [Header("VR Components")]
    [SerializeField] private GameObject vrReticle;

    private showHideHUD hudController;
    private togglingXRFilm xrToggler;  // UPDATED: Changed from togglingXR
    private VRLoadingManager loadingManager;

    private void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();
        xrToggler = FindFirstObjectByType<togglingXRFilm>();  // UPDATED: Changed from togglingXR
        loadingManager = VRLoadingManager.Instance;

        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[SetVrState] Starting with VR mode: {headsetOr2D}");

        UpdateVRSprites(headsetOr2D == 1);

        // DON'T setup cameras here - let StartUp handle initial setup
        // This prevents conflicts during scene initialization
    }

    /// <summary>
    /// UPDATED: Set VR mode with loading screen coordination
    /// </summary>
    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"[SetVrState] SetVR called with state: {headsetOr2D}");

        // Save the preference
        PlayerPrefs.SetInt("toggleToVR", headsetOr2D);
        PlayerPrefs.Save();

        // Update sprites immediately
        UpdateVRSprites(headsetOr2D == 1);

        // Show loading screen immediately
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

        // Start the switch coroutine
        StartCoroutine(SwitchModeWithLoading(headsetOr2D));
    }

    /// <summary>
    /// NEW: Switch mode with proper loading coordination
    /// </summary>
    private IEnumerator SwitchModeWithLoading(int headsetOr2D)
    {
        bool isVRMode = headsetOr2D == 1;

        // Update progress
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.1f);
        }

        // Trigger XR toggling (this handles XR subsystems)
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

        // Update progress
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.7f);
        }

        // Wait a frame for XR to settle
        yield return new WaitForEndOfFrame();

        // Setup cameras (togglingXRFilm already did this, but ensure VR reticle)
        SetupVRReticle(isVRMode);

        // Update progress
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(0.9f);
        }

        // Brief delay for everything to settle
        yield return new WaitForSeconds(0.2f);

        // Update to 100% and hide
        if (loadingManager != null)
        {
            loadingManager.UpdateProgress(1f);
            yield return new WaitForSeconds(0.2f);
            loadingManager.HideLoading();
        }

        Debug.Log($"[SetVrState] Mode switch complete. VR Mode: {isVRMode}");
    }

    /// <summary>
    /// Setup VR reticle (cameras are handled by togglingXRFilm)
    /// </summary>
    private void SetupVRReticle(bool isVRMode)
    {
        if (vrReticle != null)
        {
            vrReticle.SetActive(isVRMode);
            Debug.Log($"[SetVrState] VR reticle: {(isVRMode ? "enabled" : "disabled")}");
        }
    }

    private void UpdateVRSprites(bool isVRMode)
    {
        if (spriterendererVR != null)
        {
            spriterendererVR.sprite = isVRMode ? spritePickedNoVR : spritePickedVR;
            Debug.Log($"[SetVrState] Updated sprite. Is VR Mode: {isVRMode}");
        }
    }

    /// <summary>
    /// Public method to toggle VR mode - can be called from UI buttons
    /// </summary>
    public void ToggleVRMode()
    {
        int currentMode = PlayerPrefs.GetInt("toggleToVR", 0);
        int newMode = currentMode == 1 ? 0 : 1;

        Debug.Log($"[SetVrState] Toggling VR from {currentMode} to {newMode}");
        SetVR(newMode);
    }
}