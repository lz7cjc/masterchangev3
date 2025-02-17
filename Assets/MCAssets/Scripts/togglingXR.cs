using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public class togglingXR : MonoBehaviour
{
    [SerializeField] private SetVrState SetVrState;
    private bool isProcessingSwitch = false;
    private XRManagerSettings managerSettings;
    private const float SAFETY_DELAY = 0.5f;
    private bool isQuitting = false;

    private void Start()
    {
        Debug.Log("www togglingXR start");
        int currentState = PlayerPrefs.GetInt("toggletovr");
        Debug.Log("www togglingXR loaded persistent state: " + currentState);

        Initialize();

        // Set initial state without going through the full switch process
        if (SetVrState != null)
        {
            SetVrState.SetVR(currentState);
        }
    }

    private void Initialize()
    {
        XRGeneralSettings generalSettings = XRGeneralSettings.Instance;
        if (generalSettings != null)
        {
            managerSettings = generalSettings.Manager;
        }

        SetVrState = FindFirstObjectByType<SetVrState>();
    }

    public void SwitchingVR()
    {
        if (isProcessingSwitch)
        {
            Debug.Log("Already processing a VR switch, please wait");
            return;
        }

        StartCoroutine(HandleVRSwitch());
    }

    private IEnumerator HandleVRSwitch()
    {
        isProcessingSwitch = true;
        int currentState = PlayerPrefs.GetInt("toggletovr");
        Debug.Log($"Starting VR mode switch. Current state: {currentState}");

        // First ensure XR is stopped
        yield return StopXRIfActive();

        // Update camera state before XR changes
        if (SetVrState != null)
        {
            SetVrState.SetVR(currentState);
        }

        yield return new WaitForSeconds(SAFETY_DELAY);

        // Handle XR state change
        if (currentState == 1)
        {
            yield return StartXRIfNeeded();
        }

        // Reset cursor state
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        isProcessingSwitch = false;
        Debug.Log("VR switch completed");
    }

    private IEnumerator StartXRIfNeeded()
    {
        if (managerSettings == null || managerSettings.activeLoader != null)
        {
            yield break;
        }

        Debug.Log("Starting XR...");
        yield return managerSettings.InitializeLoader();

        if (managerSettings.activeLoader == null)
        {
            Debug.LogWarning("Failed to initialize XR loader");
            yield break;
        }

        managerSettings.StartSubsystems();
        yield return new WaitForSeconds(SAFETY_DELAY);
    }

    private IEnumerator StopXRIfActive()
    {
        if (managerSettings?.activeLoader == null)
        {
            yield break;
        }

        Debug.Log("Stopping XR...");
        managerSettings.StopSubsystems();
        yield return new WaitForSeconds(SAFETY_DELAY);

        managerSettings.DeinitializeLoader();
        yield return new WaitForSeconds(SAFETY_DELAY);
    }

    private void OnDisable()
    {
        // Only try to stop XR if we're not already quitting
        if (!isQuitting && gameObject.activeInHierarchy && managerSettings?.activeLoader != null)
        {
            StopXRImmediate();
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
        StopXRImmediate();
    }

    private void StopXRImmediate()
    {
        if (managerSettings?.activeLoader != null)
        {
            managerSettings.StopSubsystems();
            managerSettings.DeinitializeLoader();
        }
    }
}