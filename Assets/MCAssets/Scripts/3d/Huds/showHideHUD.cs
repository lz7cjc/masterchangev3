using UnityEngine;
using TMPro;

public class showHideHUD : MonoBehaviour
{
    public bool mousehover = false;
    public float Counter = 0;
    public float waitFor;
    public static bool showing = false;
    public GameObject hudprimary;
    public GameObject hudPrimaryNav;
    public GameObject hudSecondaryNav;
    public GameObject turnHudOn;
    public GameObject turnHudOff;
    public GameObject hudZones;
    
  //  [SerializeField] private ToggleActiveIcons ToggleActiveIcons;


    private hudCountdown hudCountdown;

    public void Start()
    {
       // ToggleActiveIcons = FindFirstObjectByType<ToggleActiveIcons>();
        hudprimary.SetActive(false);
        hudPrimaryNav.SetActive(false);
        hudSecondaryNav.SetActive(false);
        hudZones.SetActive(false);
        Counter = 0;
        mousehover = false;
    }

    public void resetShow()
    {
        showing = true;
    }

    public void ResetHUDState()
    {
        //ToggleActiveIcons.DefaultIcon();
        hudprimary.SetActive(false);
        hudPrimaryNav.SetActive(false);
        hudSecondaryNav.SetActive(false);
        turnHudOff.SetActive(false);
        turnHudOn.SetActive(true);
        hudZones.SetActive(false);
        showing = false;
    }

    void Update()
    {
        if (mousehover)
        {
            Counter += Time.deltaTime;
            if (hudCountdown == null)
            {
                hudCountdown = FindFirstObjectByType<hudCountdown>();
            }
            hudCountdown.SetCountdown(waitFor, Counter);
          

            if (Counter >= waitFor)
            {
                mousehover = false;
                Counter = 0;
                hudCountdown.resetCountdown();
             //   ToggleActiveIcons.SelectIcon();
                directClick();
            }
        }
    }

    public void MouseHoverChangeScene()
    {
     //   ToggleActiveIcons.HoverIcon();
        mousehover = true;
    }

    public void MouseExit()
    {
        mousehover = false;
        Counter = 0;
        if (hudCountdown != null)
        {
       //     ToggleActiveIcons.DefaultIcon();
            hudCountdown.resetCountdown();
        }
    }

    public void directClick()
    {
        if (!showing)
        {
            hudprimary.SetActive(true);
            hudPrimaryNav.SetActive(true);
            turnHudOff.SetActive(true);
            turnHudOn.SetActive(false);
            hudSecondaryNav.SetActive(false);
            showing = true;
        }
        else if (showing)
        {
            ResetHUDState();
        }
    }
}