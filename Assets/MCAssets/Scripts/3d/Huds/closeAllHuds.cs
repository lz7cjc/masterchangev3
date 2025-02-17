using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class closeAllHuds : MonoBehaviour
{
    private string nextScene;
    public GameObject hud1primary;
    public GameObject hud1SecondaryNav;
    public GameObject hud1PlusOff;
    public GameObject hud1PlusOn;
    public GameObject hud1Zones;

    private showHideHUD hudController;
    private showHideHUDcat hudCatController;
    private string behaviour;

    public void Start()
    {
        hudController = FindFirstObjectByType<showHideHUD>();
        hudCatController = FindFirstObjectByType<showHideHUDcat>();
        CloseTheHuds();
    }

    public void CloseTheHuds()
    {
        Debug.Log("Marker: CloseTheHuds called");
        if (hudController != null)
        {
            Debug.Log("Marker: Resetting HUD state for showHideHUD");
            hudController.ResetHUDState();
        }
        else
        {
            // Fallback in case hudController is not found
            Debug.Log("Marker: Fallback - Deactivating HUD elements");
            hud1primary.SetActive(false);
            hud1SecondaryNav.SetActive(false);
            hud1PlusOff.SetActive(false);
            hud1PlusOn.SetActive(true);
            hud1Zones.SetActive(false);
        }

        if (hudCatController != null)
        {
            Debug.Log("Marker: Resetting HUD state for showHideHUDcat");
            hudCatController.ResetHUDState();
            Debug.Log($"Marker: CloseTheHuds - turnon: {hudCatController.turnon}, zones active: {hudCatController.zones.activeSelf}, secondaryNavs active: {hudCatController.secondaryNavs.activeSelf}");
        }
    }
}

