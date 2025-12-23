using UnityEngine;
using System.Collections.Generic;

public class HUDSystemCoordinator : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private GameObject level1Container;
    [SerializeField] private GameObject level2Container;
    [SerializeField] private GameObject[] level3Containers;
    [SerializeField] private GameObject turnHudOn;
    [SerializeField] private GameObject turnHudOff;

    [Header("Settings")]
    [SerializeField] private bool debugLogging = true;

    private bool isLevel1Open = false;
    private int activeLevel3Index = -1;
    private bool isMovementMenuActive = false;

    private showHideHUD mainHUDController;
    private ZoneManager zoneManager;
    private List<showHideHUDcat> level2Controllers = new List<showHideHUDcat>();

    private void Start()
    {
        mainHUDController = FindObjectOfType<showHideHUD>();
        zoneManager = FindObjectOfType<ZoneManager>();

        var hudCats = FindObjectsOfType<showHideHUDcat>();
        level2Controllers.AddRange(hudCats);

        EnsureProperInitialState();
        Log("HUD System Coordinator initialized");
    }

    private void EnsureProperInitialState()
    {
        if (level1Container != null) level1Container.SetActive(true);
        if (level2Container != null) level2Container.SetActive(false);

        if (level3Containers != null)
        {
            foreach (var container in level3Containers)
            {
                if (container != null) container.SetActive(false);
            }
        }

        if (turnHudOn != null) turnHudOn.SetActive(true);
        if (turnHudOff != null) turnHudOff.SetActive(false);

        showHideHUD.showing = false;
        isLevel1Open = false;
        activeLevel3Index = -1;
        isMovementMenuActive = false;
    }

    public void OnMainHUDHoverStarted() { }
    public void OnMainHUDHoverEnded() { }

    public void OnLevel1ItemSelected()
    {
        isLevel1Open = true;
    }

    public void OpenMainHUD()
    {
        if (level2Container != null) level2Container.SetActive(true);
        if (turnHudOff != null) turnHudOff.SetActive(true);
        if (turnHudOn != null) turnHudOn.SetActive(false);
        isLevel1Open = true;
    }

    public void OnMainHUDToggled(bool isOpen)
    {
        isLevel1Open = isOpen;
    }

    public void ResetAllHUDStates()
    {
        if (level2Container != null) level2Container.SetActive(false);

        if (level3Containers != null)
        {
            foreach (var container in level3Containers)
            {
                if (container != null) container.SetActive(false);
            }
        }

        if (turnHudOff != null) turnHudOff.SetActive(false);
        if (turnHudOn != null) turnHudOn.SetActive(true);

        foreach (var hudCat in level2Controllers)
        {
            if (hudCat != null) hudCat.ResetHUDState();
        }

        showHideHUD.showing = false;
        isLevel1Open = false;
        activeLevel3Index = -1;
        isMovementMenuActive = false;
    }

    public void CloseAllHUDs()
    {
        ResetAllHUDStates();
    }

    public void OnLevel2CategoryHovered(int categoryIndex) { }

    public void OnLevel3MenuOpened(int categoryIndex)
    {
        activeLevel3Index = categoryIndex;
        if (categoryIndex == 1) isMovementMenuActive = true;
    }

    public void ForceCloseAllOtherLevel3Menus(int exceptCategoryIndex)
    {
        foreach (var hudCat in level2Controllers)
        {
            if (hudCat != null && hudCat.GetCategoryIndex() != exceptCategoryIndex)
            {
                if (hudCat.IsOpen) hudCat.ForceCloseMyLevel3();
            }
        }

        if (activeLevel3Index != exceptCategoryIndex)
        {
            activeLevel3Index = -1;
            isMovementMenuActive = false;
        }
    }

    public void OnZoneHoverStarted(string zoneName) { }
    public void OnZoneHoverEnded(string zoneName) { }
    public void OnZoneChanged(string zoneName) { }

    public bool IsMovementMenuActive() => isMovementMenuActive;

    private void Log(string message)
    {
        if (debugLogging) Debug.Log($"[HUDSystemCoordinator] {message}");
    }
}
