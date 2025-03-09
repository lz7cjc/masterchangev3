using UnityEngine;
using TMPro;

public class showHideHUD : MonoBehaviour
{
    public bool mousehover = false;
    public float Counter = 0;
    public float waitFor;
    public static bool showing = false;
    public GameObject openCloseLevel1;
     public GameObject mainIconsLevel2;
    public GameObject locationsLevel3;
    public GameObject turnHudOn;
    public GameObject turnHudOff;
    
    
  //  [SerializeField] private ToggleActiveIcons ToggleActiveIcons;


    private hudCountdown hudCountdown;

    public void Start()
    {
        // ToggleActiveIcons = FindFirstObjectByType<ToggleActiveIcons>();
       
        mainIconsLevel2.SetActive(false);
        locationsLevel3.SetActive(false);
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
        mainIconsLevel2.SetActive(false);
        locationsLevel3.SetActive(false);
        turnHudOff.SetActive(false);
        turnHudOn.SetActive(true);
        
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
            mainIconsLevel2.SetActive(true);
            locationsLevel3.SetActive(false);
            turnHudOff.SetActive(true);
            turnHudOn.SetActive(false);
           
            showing = true;
        }
        else if (showing)
        {
            ResetHUDState();
        }
    }
}