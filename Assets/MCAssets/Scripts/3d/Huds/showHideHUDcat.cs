using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Net;
using Unity;

public class showHideHUDcat : MonoBehaviour
{
    /// <summary>
    /// //display the zones sub menu on the mainvr hud////////
    /// </summary>
    //public GameObject secondaryNavs;
    public bool mousehover = false;
    public float Counter = 0;
    public GameObject level3; 
    public bool turnon = true; // Changed to public

    public float delay = 3;
    [SerializeField] private hudCountdown hudCountdown;

    [SerializeField] private ToggleActiveIcons ToggleActiveIcons;

    public void Start()
    {
        ToggleActiveIcons.DefaultIcon();
        ResetHUDState(); // Ensure initial state is set correctly
    }

    void Update()
    {
        if (mousehover)
        {
            ToggleActiveIcons.HoverIcon();

            Counter += Time.deltaTime;

            hudCountdown.SetCountdown(delay, Counter);
            if (Counter >= delay)
            {
                mousehover = false;
                Counter = 0;
                ToggleActiveIcons.SelectIcon();

                if (turnon)
                {
                   // Debug.Log("Marker: Activating zones and secondaryNavs");
                    level3.SetActive(true);
                    turnon = false;
                }
                else
                {
                   // Debug.Log("Marker: Deactivating zones and secondaryNavs");
                    level3.SetActive(false);
                    turnon = true;
                }

                hudCountdown.resetCountdown();
               // Debug.Log($"Marker: Update - turnon: {turnon}, secondaryNavs active: {level3.activeSelf}");
            }
        }
    }

    // mouse Enter event
    public void MouseHoverChangeScene()
    {
       // Debug.Log("Marker: MouseHoverChangeScene called");
        mousehover = true;
        Counter = 0;
    }

    // mouse Exit Event
    public void MouseExit()
    {
     //   Debug.Log("Marker: MouseExit called");
        ToggleActiveIcons.DefaultIcon();
        hudCountdown.resetCountdown();
        mousehover = false;
        Counter = 0;
    }

    // Method to reset the state when the HUD is closed
    public void ResetHUDState()
    {
     //   Debug.Log("Marker: ResetHUDState called");
        turnon = true;
        level3.SetActive(false);
       
    //    Debug.Log($"Marker: ResetHUDState - turnon: {turnon}, zones active: {level3.activeSelf}");
    }

    // Method to open the HUD
    public void OpenHUD()
    {
      //  Debug.Log("Marker: OpenHUD called");
     //   Debug.Log($"Marker: OpenHUD - zones active: {level3.activeSelf}");
        // Add any additional logic for opening the HUD here
    }

    // Method to close the HUD
    public void CloseHUD()
    {
      //  Debug.Log("Marker: CloseHUD called");
     //   Debug.Log($"Marker: CloseHUD - zones active: {level3.activeSelf}");
        ResetHUDState();
        // Add any additional logic for closing the HUD here
    }
}
