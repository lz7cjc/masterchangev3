using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class closeAllHuds : MonoBehaviour
{
    private string nextScene;
    //public GameObject level1OpenClose;
    public GameObject hud1PlusOff;
    public GameObject hud1PlusOn;
    public GameObject hud1Level2;
    public GameObject hud1Level3;

    private showHideHUD hudController;
    private showHideHUDcat hudCatController;
    private string behaviour;

    public void Start()
    {
        Debug.Log("Marker: closeAllHuds Start method called");

        hudController = FindFirstObjectByType<showHideHUD>();
        hudCatController = FindFirstObjectByType<showHideHUDcat>();

        if (hud1Level2 == null)
        {
            Debug.LogError("Marker: hud1Level2 is not assigned in Start method!");
        }
        if (hud1Level3 == null)
        {
            Debug.LogError("Marker: hud1Level3 is not assigned in Start method!");
        }
        if (hud1PlusOff == null)
        {
            Debug.LogError("Marker: hud1PlusOff is not assigned in Start method!");
        }
        if (hud1PlusOn == null)
        {
            Debug.LogError("Marker: hud1PlusOn is not assigned in Start method!");
        }

        CloseTheHuds();
    }

    public void CloseTheHuds()
    {
        Debug.Log("Marker: CloseTheHuds called");

        if (hud1Level2 == null)
        {
            Debug.LogError("Marker: hud1Level2 is not assigned in CloseTheHuds method!");
        }
        if (hud1Level3 == null)
        {
            Debug.LogError("Marker: hud1Level3 is not assigned in CloseTheHuds method!");
        }
        if (hud1PlusOff == null)
        {
            Debug.LogError("Marker: hud1PlusOff is not assigned in CloseTheHuds method!");
        }
        if (hud1PlusOn == null)
        {
            Debug.LogError("Marker: hud1PlusOn is not assigned in CloseTheHuds method!");
        }

        if (hudController != null)
        {
            Debug.Log("Marker: Resetting HUD state for showHideHUD");
            hudController.ResetHUDState();
        }
        else
        {
            // Fallback in case hudController is not found
            Debug.Log("Marker: Fallback - Deactivating HUD elements");
            if (hud1Level2 != null) hud1Level2.SetActive(false);
            if (hud1Level3 != null) hud1Level3.SetActive(false);
            if (hud1PlusOff != null) hud1PlusOff.SetActive(false);
            if (hud1PlusOn != null) hud1PlusOn.SetActive(true);
        }

        if (hudCatController != null)
        {
            Debug.Log("Marker: Resetting HUD state for showHideHUDcat");
            hudCatController.ResetHUDState();
        }
    }
}
