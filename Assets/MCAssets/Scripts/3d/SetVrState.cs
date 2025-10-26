using UnityEngine;

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
    [SerializeField] private GameObject vrReticle; // VR reticle pointer

    private showHideHUD hudController;
    private togglingXR xrToggler;

    private void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();
        xrToggler = FindFirstObjectByType<togglingXR>();

        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[SetVrState] Starting with VR mode: {headsetOr2D}");

        UpdateVRSprites(headsetOr2D == 1);

        // Initial camera setup (but let togglingXR handle XR initialization)
        SetupCameras(headsetOr2D == 1);
    }

    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"[SetVrState] SetVR called with state: {headsetOr2D}");

        // Save the preference
        PlayerPrefs.SetInt("toggleToVR", headsetOr2D);
        PlayerPrefs.Save();

        // Update sprites
        UpdateVRSprites(headsetOr2D == 1);

        // Setup cameras with a small delay
        Invoke(nameof(DelayedCameraSetup), 0.1f);

        // Trigger XR toggling
        if (xrToggler != null)
        {
            xrToggler.SwitchingVR();
        }
        else
        {
            Debug.LogWarning("[SetVrState] togglingXR component not found!");
        }
    }

    private void DelayedCameraSetup()
    {
        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR", 0);
        SetupCameras(headsetOr2D == 1);
    }

    private void SetupCameras(bool isVRMode)
    {
        Debug.Log($"[SetVrState] Setting up cameras. VR Mode: {isVRMode}");

        // First disable both cameras
        if (mainCamera2D != null)
        {
            mainCamera2D.SetActive(false);
        }

        if (mainCameraVR != null)
        {
            mainCameraVR.SetActive(false);
        }

        // Wait one frame for deactivation
        StartCoroutine(ActivateCameraAfterFrame(isVRMode));
    }

    private System.Collections.IEnumerator ActivateCameraAfterFrame(bool isVRMode)
    {
        yield return null; // Wait one frame

        if (isVRMode)
        {
            Debug.Log("[SetVrState] Activating VR camera");

            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(true);

                // Enable VR reticle
                if (vrReticle != null)
                {
                    vrReticle.SetActive(true);
                    Debug.Log("[SetVrState] VR reticle enabled");
                }
                else
                {
                    Debug.LogWarning("[SetVrState] VR reticle not assigned! Please assign it in the Inspector.");
                }
            }
            else
            {
                Debug.LogError("[SetVrState] VR camera GameObject is not assigned!");
            }

            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(false);
            }
        }
        else
        {
            Debug.Log("[SetVrState] Activating 2D camera");

            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(true);
            }
            else
            {
                Debug.LogError("[SetVrState] 2D camera GameObject is not assigned!");
            }

            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(false);
            }

            // Disable VR reticle
            if (vrReticle != null)
            {
                vrReticle.SetActive(false);
            }
        }
    }

    private void UpdateVRSprites(bool isVRMode)
    {
        if (spriterendererVR != null)
        {
            // When in VR mode, show the "No VR" button (to switch back)
            // When in 2D mode, show the "VR" button (to switch to VR)
            spriterendererVR.sprite = isVRMode ? spritePickedNoVR : spritePickedVR;
            Debug.Log($"[SetVrState] Updated sprite. Is VR Mode: {isVRMode}");
        }
        else
        {
            Debug.LogWarning("[SetVrState] Sprite renderer not assigned!");
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