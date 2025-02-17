using UnityEngine;

public class switchformatreload : MonoBehaviour
{
    [SerializeField] private float Counter;
    [SerializeField] private float delay;
    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private togglingXR togglingXR;
    [SerializeField] private ToggleActiveIconsVR toggleActiveIconsVR;

    private bool isProcessing = false;
    private bool isHovering = false;

    public void Start()
    {
        if (PlayerPrefs.GetInt("toggletovr") == 1)
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
                int currentState = PlayerPrefs.GetInt("toggletovr");
                int newState = (currentState == 1) ? 0 : 1;
                Debug.Log($"Toggling VR state from {currentState} to {newState}");

                PlayerPrefs.SetInt("toggletovr", newState);
                PlayerPrefs.Save();

                // Switch VR state
                if (togglingXR == null)
                {
                    togglingXR = FindFirstObjectByType<togglingXR>();
                }

                if (togglingXR != null)
                {
                    togglingXR.SwitchingVR();
                }

                // Update icon based on new state
                toggleActiveIconsVR.SetHeadsetIcon(newState == 1);

                isHovering = false;
                Counter = 0;
                isProcessing = false;
            }
        }
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
}
