using UnityEngine;
using UnityEngine.XR;

public class DualCameraManager : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private Camera camera2D;
    [SerializeField] private Camera cameraVR;

    [Header("Audio Listeners")]
    [SerializeField] private AudioListener audioListener2D;
    [SerializeField] private AudioListener audioListenerVR;

    private void Awake()
    {
        // Auto-find cameras if not assigned
        if (camera2D == null)
            camera2D = transform.Find("Main Camera2d")?.GetComponent<Camera>();
        if (cameraVR == null)
            cameraVR = transform.Find("Main CameraVR")?.GetComponent<Camera>();

        // Auto-find audio listeners
        if (audioListener2D == null && camera2D != null)
            audioListener2D = camera2D.GetComponent<AudioListener>();
        if (audioListenerVR == null && cameraVR != null)
            audioListenerVR = cameraVR.GetComponent<AudioListener>();
    }

    public void SwitchToVRMode()
    {
        Debug.Log("[DualCameraManager] Switching to VR mode");

        // Enable VR camera and components
        if (cameraVR != null)
        {
            cameraVR.gameObject.SetActive(true);
            cameraVR.enabled = true;
        }

        if (audioListenerVR != null)
            audioListenerVR.enabled = true;

        // Disable 2D camera and components
        if (camera2D != null)
        {
            camera2D.gameObject.SetActive(false);
            camera2D.enabled = false;
        }

        if (audioListener2D != null)
            audioListener2D.enabled = false;
    }

    public void SwitchTo2DMode()
    {
        Debug.Log("[DualCameraManager] Switching to 2D/360 mode");

        // Enable 2D camera and components
        if (camera2D != null)
        {
            camera2D.gameObject.SetActive(true);
            camera2D.enabled = true;
        }

        if (audioListener2D != null)
            audioListener2D.enabled = true;

        // Disable VR camera and components
        if (cameraVR != null)
        {
            cameraVR.gameObject.SetActive(false);
            cameraVR.enabled = false;
        }

        if (audioListenerVR != null)
            audioListenerVR.enabled = false;
    }

    public void SwitchCamera(bool enableVR)
    {
        if (enableVR)
        {
            SwitchToVRMode();
        }
        else
        {
            SwitchTo2DMode();
        }
    }

    // Get the currently active camera
    public Camera GetActiveCamera()
    {
        int vrState = PlayerPrefs.GetInt("toggleToVR", 0);
        if (vrState == 1 && XRSettings.enabled)
        {
            return cameraVR;
        }
        else
        {
            return camera2D;
        }
    }

    // Check which mode is currently active
    public bool IsVRModeActive()
    {
        return cameraVR != null && cameraVR.gameObject.activeInHierarchy;
    }

    private void OnValidate()
    {
        // Auto-assign in editor
        if (camera2D == null)
            camera2D = transform.Find("Main Camera2d")?.GetComponent<Camera>();
        if (cameraVR == null)
            cameraVR = transform.Find("Main CameraVR")?.GetComponent<Camera>();
    }
}