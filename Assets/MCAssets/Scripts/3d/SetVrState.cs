using UnityEngine;
using System.Collections;

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
    [SerializeField] private GameObject vrReticle; // Your VR reticle GameObject - assign in Inspector

    [Header("UI Canvas (Optional)")]
    [SerializeField] private Canvas mainCanvas; // Will auto-find if not assigned

    private showHideHUD hudController;
    private togglingXR xrToggler;

    private void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();
        xrToggler = FindFirstObjectByType<togglingXR>();

        // Auto-find canvas if not assigned
        if (mainCanvas == null)
        {
            mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas != null)
            {
                Debug.Log($"[SetVrState] Auto-found Canvas: {mainCanvas.gameObject.name}");
            }
        }

        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR", 0);
        Debug.Log($"[SetVrState] Starting with VR mode: {headsetOr2D}");

        UpdateVRSprites(headsetOr2D == 1);

        // Initial camera setup
        SetupCamerasImmediate(headsetOr2D == 1);
    }

    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"[SetVrState] SetVR called with state: {headsetOr2D}");

        // Save the preference
        PlayerPrefs.SetInt("toggleToVR", headsetOr2D);
        PlayerPrefs.Save();

        // Update sprites
        UpdateVRSprites(headsetOr2D == 1);

        // Trigger XR toggling first
        if (xrToggler != null)
        {
            xrToggler.SwitchingVR();
        }
        else
        {
            Debug.LogWarning("[SetVrState] togglingXR component not found!");
        }

        // Setup cameras with a delay to let XR initialize
        StartCoroutine(DelayedCameraSetup(headsetOr2D == 1, 0.2f));
    }

    private void SetupCamerasImmediate(bool isVRMode)
    {
        Debug.Log($"[SetVrState] Setting up cameras immediately. VR Mode: {isVRMode}");

        // Disable both cameras first
        if (mainCamera2D != null) mainCamera2D.SetActive(false);
        if (mainCameraVR != null) mainCameraVR.SetActive(false);

        // Enable the correct camera
        if (isVRMode)
        {
            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(true);
                Debug.Log("[SetVrState] VR camera activated");

                // Update Canvas event camera to VR camera
                UpdateCanvasEventCamera(mainCameraVR);
            }
            else
            {
                Debug.LogError("[SetVrState] VR camera GameObject is not assigned in Inspector!");
            }

            // Enable VR reticle
            if (vrReticle != null)
            {
                vrReticle.SetActive(true);
                Debug.Log("[SetVrState] VR reticle enabled");
            }
            else
            {
                Debug.LogWarning("[SetVrState] VR reticle not assigned in Inspector!");
            }
        }
        else
        {
            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(true);
                Debug.Log("[SetVrState] 2D camera activated");

                // Update Canvas event camera to 2D camera
                UpdateCanvasEventCamera(mainCamera2D);
            }
            else
            {
                Debug.LogError("[SetVrState] 2D camera GameObject is not assigned in Inspector!");
            }

            // Disable VR reticle in non-VR modes
            if (vrReticle != null)
            {
                vrReticle.SetActive(false);
            }
        }
    }

    private IEnumerator DelayedCameraSetup(bool isVRMode, float delay)
    {
        Debug.Log($"[SetVrState] Waiting {delay}s before camera setup...");
        yield return new WaitForSeconds(delay);

        // Disable both cameras
        if (mainCamera2D != null) mainCamera2D.SetActive(false);
        if (mainCameraVR != null) mainCameraVR.SetActive(false);

        // Wait one more frame
        yield return null;

        // Enable the correct camera
        if (isVRMode)
        {
            Debug.Log("[SetVrState] Activating VR camera");

            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(true);
                UpdateCanvasEventCamera(mainCameraVR);
            }
            else
            {
                Debug.LogError("[SetVrState] VR camera GameObject is not assigned in Inspector!");
            }

            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(false);
            }

            // Enable VR reticle
            if (vrReticle != null)
            {
                vrReticle.SetActive(true);
                Debug.Log("[SetVrState] VR reticle enabled");
            }
            else
            {
                Debug.LogWarning("[SetVrState] VR reticle not assigned! Assign it in the Inspector for VR interaction.");
            }
        }
        else
        {
            Debug.Log("[SetVrState] Activating 2D camera");

            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(true);
                UpdateCanvasEventCamera(mainCamera2D);
            }
            else
            {
                Debug.LogError("[SetVrState] 2D camera GameObject is not assigned in Inspector!");
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

    private void UpdateCanvasEventCamera(GameObject cameraObject)
    {
        if (cameraObject == null) return;

        Camera cam = cameraObject.GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogWarning($"[SetVrState] No Camera component found on {cameraObject.name}");
            return;
        }

        // Update main canvas if assigned
        if (mainCanvas != null)
        {
            mainCanvas.worldCamera = cam;
            Debug.Log($"[SetVrState] Canvas event camera updated to: {cameraObject.name}");
        }

        // Also update any other World Space canvases in the scene
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = cam;
                Debug.Log($"[SetVrState] Updated world space canvas '{canvas.gameObject.name}' event camera to: {cameraObject.name}");
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
            Debug.LogWarning("[SetVrState] Sprite renderer not assigned in Inspector!");
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