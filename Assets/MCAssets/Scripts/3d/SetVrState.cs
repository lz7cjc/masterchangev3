using UnityEngine;

public class SetVrState : MonoBehaviour
{
    [SerializeField] private Sprite spritePickedVR;
    [SerializeField] private Sprite spritePickedNoVR;
    [SerializeField] public SpriteRenderer spriterendererVR;
    [SerializeField] private GameObject mainCamera2D;
    [SerializeField] private GameObject mainCameraVR;
    [SerializeField] private VRReticlePointer vrReticlePointer; // ADD THIS LINE
    private showHideHUD hudController;

    private void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();

        // AUTO-FIND VRReticlePointer if not assigned
        if (vrReticlePointer == null)
        {
            vrReticlePointer = FindFirstObjectByType<VRReticlePointer>();
        }

        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR");
        UpdateVRSprites(headsetOr2D == 1);
        SetReticleMode(headsetOr2D == 1); // ADD THIS LINE
    }

    public void SetVR(int headsetOr2D)
    {
        Debug.Log($"SetVR called with state: {headsetOr2D}");

        // Store the state in PlayerPrefs first
        PlayerPrefs.SetInt("toggleToVR", headsetOr2D);
        PlayerPrefs.Save();

        // Immediate camera switch without delay
        ActivateCamera();

        UpdateVRSprites(headsetOr2D == 1);
        SetReticleMode(headsetOr2D == 1);
    }

    private void ActivateCamera()
    {
        int headsetOr2D = PlayerPrefs.GetInt("toggleToVR");
        Debug.Log($"ActivateCamera: toggleToVR = {headsetOr2D}");

        if (headsetOr2D == 1)
        {
            Debug.Log("Switching to VR camera");
            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(false);
                Debug.Log("2D camera deactivated");
            }
            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(true);
                Debug.Log("VR camera activated");
            }

            // Handle AudioListener switching
            SetAudioListener(mainCamera2D, false);
            SetAudioListener(mainCameraVR, true);
        }
        else
        {
            Debug.Log("Switching to 2D camera");
            if (mainCameraVR != null)
            {
                mainCameraVR.SetActive(false);
                Debug.Log("VR camera deactivated");
            }
            if (mainCamera2D != null)
            {
                mainCamera2D.SetActive(true);
                Debug.Log("2D camera activated");
            }

            // Handle AudioListener switching
            SetAudioListener(mainCameraVR, false);
            SetAudioListener(mainCamera2D, true);
        }

        // Verify the switch worked
        Debug.Log($"Final state - 2D active: {(mainCamera2D != null ? mainCamera2D.activeInHierarchy.ToString() : "null")}, VR active: {(mainCameraVR != null ? mainCameraVR.activeInHierarchy.ToString() : "null")}");
    }

    private void UpdateVRSprites(bool isVRMode)
    {
        if (spriterendererVR != null)
        {
            spriterendererVR.sprite = isVRMode ? spritePickedNoVR : spritePickedVR;
        }
    }

    // SET RETICLE MODE METHOD
    private void SetReticleMode(bool isVRMode)
    {
        if (vrReticlePointer != null)
        {
            if (isVRMode)
            {
                // In editor, use Mode360 for testing with mouse controls
                // On device, use ModeVR for actual VR head tracking
#if UNITY_EDITOR
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.Mode360);
                Debug.Log("Set reticle to 360 mode (Editor VR Testing)");
#else
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.ModeVR);
                Debug.Log("Set reticle to VR mode");
#endif
            }
            else
            {
                vrReticlePointer.SetMode(VRReticlePointer.ViewMode.Mode360);
                Debug.Log("Set reticle to 360 mode");
            }
        }
        else
        {
            Debug.LogWarning("VRReticlePointer reference not found!");
        }
    }

    private void SetAudioListener(GameObject cameraObject, bool enabled)
    {
        if (cameraObject != null)
        {
            AudioListener audioListener = cameraObject.GetComponent<AudioListener>();
            if (audioListener == null && enabled)
            {
                // Add AudioListener if it doesn't exist and we need it enabled
                audioListener = cameraObject.AddComponent<AudioListener>();
                Debug.Log($"Added AudioListener to {cameraObject.name}");
            }
            if (audioListener != null)
            {
                audioListener.enabled = enabled;
                Debug.Log($"AudioListener on {cameraObject.name} set to {enabled}");
            }
        }
    }

    // DEBUG METHOD - Add this to help troubleshoot
    [ContextMenu("Debug Camera References")]
    private void DebugCameraReferences()
    {
        Debug.Log("=== Camera Reference Debug ===");
        Debug.Log($"mainCamera2D: {(mainCamera2D != null ? mainCamera2D.name : "NULL")}");
        Debug.Log($"mainCameraVR: {(mainCameraVR != null ? mainCameraVR.name : "NULL")}");
        Debug.Log($"vrReticlePointer: {(vrReticlePointer != null ? vrReticlePointer.name : "NULL")}");
        Debug.Log($"Current toggleToVR: {PlayerPrefs.GetInt("toggleToVR")}");

        if (mainCamera2D != null)
        {
            Debug.Log($"2D Camera active: {mainCamera2D.activeInHierarchy}");
            Debug.Log($"2D Camera has Camera component: {mainCamera2D.GetComponent<Camera>() != null}");
        }

        if (mainCameraVR != null)
        {
            Debug.Log($"VR Camera active: {mainCameraVR.activeInHierarchy}");
            Debug.Log($"VR Camera has Camera component: {mainCameraVR.GetComponent<Camera>() != null}");
        }
    }
}