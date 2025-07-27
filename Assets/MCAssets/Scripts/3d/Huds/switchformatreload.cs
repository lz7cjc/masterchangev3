using UnityEngine;

public class switchformatreload : MonoBehaviour
{
    [SerializeField] private float Counter;
    [SerializeField] private float delay;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private togglingXR togglingXR;
    [SerializeField] private ToggleActiveIconsVR toggleActiveIconsVR;
    [SerializeField] private SetVrState setVrState; // ADD THIS LINE

    private bool isProcessing = false;
    private bool isHovering = false;

    public void Start()
    {
        // AUTO-FIND SetVrState if not assigned
        if (setVrState == null)
        {
            setVrState = FindFirstObjectByType<SetVrState>();
        }

        if (PlayerPrefs.GetInt("toggleToVR") == 1)
        {
            toggleActiveIconsVR.SetHeadsetIcon(true);
        }
        else
        {
            toggleActiveIconsVR.SetHeadsetIcon(false);
        }
    }

    void Update()
    {
        if (isHovering && !isProcessing)
        {
            toggleActiveIconsVR.HoverIcon();

            Counter += Time.deltaTime;
            if (hudCountdown == null)
            {
                hudCountdown = FindFirstObjectByType<hudCountdown>();
            }

            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(delay, Counter);
            }

            if (Counter >= delay)
            {
                toggleActiveIconsVR.DefaultIcon();
                isProcessing = true;
                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }

                // Toggle state
                int currentState = PlayerPrefs.GetInt("toggleToVR");
                int newState = (currentState == 1) ? 0 : 1;
                Debug.Log($"Toggling VR state from {currentState} to {newState}");

                PlayerPrefs.SetInt("toggleToVR", newState);
                PlayerPrefs.Save();

                // Switch XR system using togglingXR
                if (togglingXR == null)
                {
                    togglingXR = FindFirstObjectByType<togglingXR>();
                }

                if (togglingXR != null)
                {
                    Debug.Log($"Calling SwitchingVR() with new state: {newState}");
                    togglingXR.SwitchingVR();
                }
                else
                {
                    Debug.LogWarning("togglingXR instance not found!");
                }

                // ADD THIS: Switch cameras and reticle mode using SetVrState
                if (setVrState != null)
                {
                    Debug.Log($"Calling SetVR() with new state: {newState}");
                    setVrState.SetVR(newState);
                }
                else
                {
                    Debug.LogWarning("SetVrState instance not found!");
                }

                // Wait a frame to ensure the VR state change has processed
                StartCoroutine(WaitAndUpdateIcon(newState));

                isHovering = false;
                Counter = 0;
                isProcessing = false;
            }
        }
    }

    private System.Collections.IEnumerator WaitAndUpdateIcon(int newState)
    {
        yield return new WaitForEndOfFrame();
        // Update icon based on new state
        toggleActiveIconsVR.SetHeadsetIcon(newState == 1);
    }

    public void MouseHoverChangeScene()
    {
        if (!isProcessing && !isHovering)
        {
            Debug.Log("Mouse hover started");
            isHovering = true;
            Counter = 0;
        }
    }

    public void MouseExit()
    {
        toggleActiveIconsVR.DefaultIcon();
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
        isHovering = false;
        Counter = 0;
    }

    // Add debug logs to track format switching
    public void ReloadFormat(bool isVRMode)
    {
        Debug.Log($"Reloading format for VR mode: {isVRMode}");
        // Existing logic for reloading format
    }
}