using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Fixed HUD System Coordinator - Ensures proper initial state and child activation
/// </summary>
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

    // State tracking
    private bool isLevel1Open = false;
    private int activeLevel3Index = -1;

    // Component references
    private showHideHUD mainHUDController;
    private ZoneManager zoneManager;
    private List<showHideHUDcat> level2Controllers = new List<showHideHUDcat>();

    private void Start()
    {
        mainHUDController = FindFirstObjectByType<showHideHUD>();
        zoneManager = FindFirstObjectByType<ZoneManager>();

        var hudCats = FindObjectsOfType<showHideHUDcat>();
        level2Controllers.AddRange(hudCats);

        // FIXED: Ensure proper initial state regardless of scene setup
        EnsureProperInitialState();

        Log("HUD System Coordinator initialized with proper initial state");
    }

    // FIXED: Force correct initial state
    private void EnsureProperInitialState()
    {
        Log("Ensuring proper initial HUD state...");

        // Level 1 should be active (visible open/close button)
        if (level1Container != null)
        {
            level1Container.SetActive(true);
            Log($"Set {level1Container.name} to ACTIVE");
        }

        // Level 2 should be inactive initially
        if (level2Container != null)
        {
            level2Container.SetActive(false);
            Log($"Set {level2Container.name} to INACTIVE");
        }

        // All Level 3 containers should be inactive initially
        if (level3Containers != null)
        {
            foreach (var container in level3Containers)
            {
                if (container != null)
                {
                    container.SetActive(false);
                    Log($"Set {container.name} to INACTIVE");
                }
            }
        }

        // Turn HUD On should be visible, Turn HUD Off should be hidden
        if (turnHudOn != null)
        {
            turnHudOn.SetActive(true);
            Log($"Set {turnHudOn.name} to ACTIVE (show open button)");
        }

        if (turnHudOff != null)
        {
            turnHudOff.SetActive(false);
            Log($"Set {turnHudOff.name} to INACTIVE (hide close button)");
        }

        // Reset static states
        showHideHUD.showing = false;
        isLevel1Open = false;
        activeLevel3Index = -1;

        Log("Initial HUD state properly configured");
    }

    public void OnMainHUDHoverStarted()
    {
        Log("Main HUD hover started");

        // Only reset OTHER hover states, not the main HUD
        ResetLevel2Hovers();
        ResetZoneHovers();
        ResetCountdownDisplay();

        Log("Reset other hover states (preserving main HUD timer)");
    }

    public void OnMainHUDHoverEnded()
    {
        Log("Main HUD hover ended");
    }

    public void OnMainHUDToggled(bool isOpen)
    {
        isLevel1Open = isOpen;

        if (isOpen)
        {
            Log("Main HUD opening - Level 2 shown");
            CloseAllLevel3Menus();
            OpenLevel2Only();
        }
        else
        {
            Log("Main HUD closing - All levels closed");
            CloseAllLevels();
        }

        Log($"Main HUD toggled: {(isOpen ? "Open" : "Closed")}");
    }

    public void OpenMainHUD()
    {
        if (level2Container != null)
        {
            level2Container.SetActive(true);
            Log($"Activated {level2Container.name}");
        }
        if (turnHudOn != null)
        {
            turnHudOn.SetActive(false);
            Log($"Deactivated {turnHudOn.name}");
        }
        if (turnHudOff != null)
        {
            turnHudOff.SetActive(true);
            Log($"Activated {turnHudOff.name}");
        }

        CloseAllLevel3Menus();

        Log("Main HUD opened - Level 2 shown, Level 3 closed");
    }

    public void OnZoneHoverStarted(string zoneName)
    {
        Log($"Zone hover started: {zoneName}");
        ResetMainHUDHover();
        ResetLevel2Hovers();
    }

    public void OnZoneHoverEnded(string zoneName)
    {
        Log($"Zone hover ended: {zoneName}");
    }

    public void OnZoneChanged(string zoneName)
    {
        Log($"Zone changed to: {zoneName}");
        CloseAllHUDs();
        ResetAllHoverStates();
        ResetAllStaticStates();
    }

    public void ForceCloseAllOtherLevel3Menus(int exceptCategoryIndex)
    {
        Log($"Force closing all Level 3 menus except category: {exceptCategoryIndex}");

        // Method 1: Close via coordinator array
        if (level3Containers != null)
        {
            for (int i = 0; i < level3Containers.Length; i++)
            {
                if (i != exceptCategoryIndex && level3Containers[i] != null)
                {
                    if (level3Containers[i].activeInHierarchy)
                    {
                        Log($"Coordinator closing: {level3Containers[i].name}");
                        level3Containers[i].SetActive(false);
                    }
                }
            }
        }

        // Method 2: Close via all showHideHUDcat scripts
        showHideHUDcat[] allHudCats = FindObjectsOfType<showHideHUDcat>();
        foreach (showHideHUDcat hudCat in allHudCats)
        {
            int catIndex = hudCat.GetCategoryIndex();
            if (catIndex != exceptCategoryIndex && hudCat.level3 != null)
            {
                if (hudCat.level3.activeInHierarchy)
                {
                    Log($"HUDcat closing: {hudCat.level3.name} (category {catIndex})");
                    hudCat.level3.SetActive(false);
                }

                hudCat.turnon = true;
                hudCat.SetIconToDefault();
            }
        }

        // Update active index
        if (activeLevel3Index != exceptCategoryIndex)
        {
            activeLevel3Index = -1;
        }

        Log("Completed force closing other Level 3 menus");
    }

    public void OnLevel2CategoryHovered(int categoryIndex)
    {
        Log($"Level 2 category hovered: {categoryIndex}");
        ResetMainHUDHover();
        ResetZoneHovers();
    }

    public void OnLevel3MenuOpened(int menuIndex)
    {
        Log($"Request to open Level 3 menu: {menuIndex}");
        Log($"Currently active Level 3 index: {activeLevel3Index}");

        // FORCE close ALL Level 3 menus using multiple methods
        ForceCloseAllLevel3MenusCompletely();

        // Wait a frame to ensure everything is closed, then open the new menu
        StartCoroutine(OpenMenuAfterClose(menuIndex));
    }

    private System.Collections.IEnumerator OpenMenuAfterClose(int menuIndex)
    {
        yield return null; // Wait one frame

        // Now open the requested menu
        if (menuIndex >= 0 && menuIndex < level3Containers.Length && level3Containers[menuIndex] != null)
        {
            // FIXED: Properly activate the Level3 menu and all its children
            ActivateLevel3MenuCompletely(level3Containers[menuIndex]);
            activeLevel3Index = menuIndex;
            Log($"Successfully opened Level 3 menu: {menuIndex} ({level3Containers[menuIndex].name})");
        }
        else
        {
            Log($"Cannot open Level 3 menu - Invalid index {menuIndex} or container not found");
        }

        UpdateAllLevel2States();
    }

    // FIXED: New method to properly activate Level3 menu and all children
    private void ActivateLevel3MenuCompletely(GameObject level3Container)
    {
        if (level3Container == null) return;

        // First activate the main container
        level3Container.SetActive(true);
        Log($"Activated main container: {level3Container.name}");

        // Then activate all child UI elements
        ActivateAllChildrenRecursively(level3Container.transform);
    }

    // FIXED: Recursively activate all children (UI elements like buttons, icons, etc.)
    private void ActivateAllChildrenRecursively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Activate the child GameObject
            child.gameObject.SetActive(true);

            // Recursively activate its children
            ActivateAllChildrenRecursively(child);

            Log($"Activated child: {child.name}");
        }
    }

    private void ForceCloseAllLevel3MenusCompletely()
    {
        Log("FORCE closing all Level 3 menus using multiple methods");

        // Method 1: Close containers in our array
        if (level3Containers != null)
        {
            foreach (var container in level3Containers)
            {
                if (container != null)
                {
                    if (container.activeInHierarchy)
                    {
                        Log($"Method 1 - Closing: {container.name}");
                    }
                    container.SetActive(false);
                }
            }
        }

        // Method 2: Find and close ANY GameObject with "Level3" in the name
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Level3") && obj.activeInHierarchy)
            {
                Log($"Method 2 - Force closing unmanaged Level3: {obj.name}");
                obj.SetActive(false);
            }
        }

        // Method 3: Use all showHideHUDcat scripts to close their Level3 containers
        showHideHUDcat[] allHudCats = FindObjectsOfType<showHideHUDcat>();
        foreach (showHideHUDcat hudCat in allHudCats)
        {
            GameObject level3Container = hudCat.level3;
            if (level3Container != null && level3Container.activeInHierarchy)
            {
                Log($"Method 3 - Closing via HUDcat: {level3Container.name}");
                level3Container.SetActive(false);
            }
        }

        activeLevel3Index = -1;
        Log("All Level 3 menus should now be closed");
    }

    public void OnLevel2ItemSelected(int categoryIndex)
    {
        Log($"Level 2 item selected: {categoryIndex}");
        ResetAllHoverStates();
    }

    public void OnLevel1ItemSelected()
    {
        Log("Level 1 item selected");
        CloseAllLevel3Menus();
        ResetAllHoverStates();
    }

    public void CloseAllHUDs()
    {
        CloseAllLevels();
    }

    public void ResetAllHUDStates()
    {
        CloseAllLevels();

        if (mainHUDController != null)
        {
            mainHUDController.mousehover = false;
            mainHUDController.Counter = 0;
        }

        ResetAllStaticStates();
        Log("All HUD states reset");
    }

    private void ResetAllStaticStates()
    {
        showHideHUD.showing = false;
        Log("Static states reset by coordinator");
    }

    public bool IsLevel1Open => isLevel1Open;
    public bool IsAnyLevel3Open => activeLevel3Index >= 0;
    public int ActiveLevel3Index => activeLevel3Index;

    public bool IsLocationMenuActive()
    {
        if (activeLevel3Index < 0 || activeLevel3Index >= level3Containers.Length)
            return false;

        string containerName = level3Containers[activeLevel3Index]?.name?.ToLower() ?? "";
        return containerName.Contains("location") || containerName.Contains("zone") ||
               containerName.Contains("level3a");
    }

    public bool IsMovementMenuActive()
    {
        return IsAnyLevel3Open && !IsLocationMenuActive();
    }

    private void OpenLevel2Only()
    {
        if (level2Container != null) level2Container.SetActive(true);
        CloseAllLevel3Menus();
    }

    private void CloseAllLevels()
    {
        CloseAllLevel3Menus();

        if (level2Container != null) level2Container.SetActive(false);
        if (turnHudOn != null) turnHudOn.SetActive(true);
        if (turnHudOff != null) turnHudOff.SetActive(false);

        isLevel1Open = false;
        activeLevel3Index = -1;
        ResetAllStaticStates();
    }

    private void CloseAllLevel3Menus()
    {
        Log("Closing all Level 3 menus (standard method)");

        if (level3Containers != null)
        {
            foreach (var container in level3Containers)
            {
                if (container != null)
                {
                    if (container.activeInHierarchy)
                    {
                        Log($"Standard close: {container.name}");
                    }
                    container.SetActive(false);
                }
            }
        }

        showHideHUDcat[] allHudCats = FindObjectsOfType<showHideHUDcat>();
        foreach (showHideHUDcat hudCat in allHudCats)
        {
            if (hudCat.level3 != null && hudCat.level3.activeInHierarchy)
            {
                Log($"HUDcat close: {hudCat.level3.name}");
                hudCat.level3.SetActive(false);
            }
            hudCat.SetIconToDefault();
            hudCat.turnon = true;
        }

        activeLevel3Index = -1;
    }

    private void UpdateAllLevel2States()
    {
        var allHudCats = FindObjectsOfType<showHideHUDcat>();

        foreach (var hudCat in allHudCats)
        {
            int catIndex = hudCat.GetCategoryIndex();
            bool shouldBeOn = (catIndex != activeLevel3Index);

            hudCat.turnon = shouldBeOn;
            Log($"Updated category {catIndex}: turnon = {shouldBeOn}");
        }
    }

    private void ResetAllHoverStates()
    {
        ResetMainHUDHover();
        ResetLevel2Hovers();
        ResetZoneHovers();
        ResetCountdownDisplay();
        Log("All hover states reset");
    }

    private void ResetMainHUDHover()
    {
        if (mainHUDController != null)
        {
            mainHUDController.mousehover = false;
            mainHUDController.Counter = 0;
        }
    }

    private void ResetLevel2Hovers()
    {
        foreach (var controller in level2Controllers)
        {
            if (controller != null)
            {
                controller.mousehover = false;
                controller.Counter = 0;
            }
        }
    }

    private void ResetZoneHovers()
    {
        if (zoneManager != null)
        {
            zoneManager.ResetAllHoverStates();
        }
    }

    private void ResetCountdownDisplay()
    {
        var hudCountdown = FindFirstObjectByType<hudCountdown>();
        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    private void Log(string message)
    {
        if (debugLogging)
        {
            Debug.Log($"[HUD Coordinator] {message}");
        }
    }

    [ContextMenu("Find HUD References")]
    private void FindHUDReferences()
    {
        if (level1Container == null)
            level1Container = GameObject.Find("Level1(openclose)");

        if (level2Container == null)
            level2Container = GameObject.Find("Level2");

        if (turnHudOn == null)
            turnHudOn = GameObject.Find("opendefault");

        if (turnHudOff == null)
            turnHudOff = GameObject.Find("closeHud");

        var level3List = new List<GameObject>();

        var level3a = GameObject.Find("Level3a (locations)") ?? GameObject.Find("Level3a");
        var level3b = GameObject.Find("Level3b (move)") ?? GameObject.Find("Level3b");

        if (level3a != null) level3List.Add(level3a);
        if (level3b != null) level3List.Add(level3b);

        level3Containers = level3List.ToArray();

        Log($"Auto-found HUD references: L1={level1Container?.name}, L2={level2Container?.name}, L3 Count={level3Containers?.Length}");

        for (int i = 0; i < level3Containers.Length; i++)
        {
            if (level3Containers[i] != null)
            {
                Log($"Level3[{i}]: {level3Containers[i].name}");
            }
        }
    }

    [ContextMenu("Show Current State")]
    private void ShowCurrentState()
    {
        Log($"Current State - L1: {isLevel1Open}, L3 Active: {activeLevel3Index}, Location Menu: {IsLocationMenuActive()}, Movement Menu: {IsMovementMenuActive()}");
    }

    [ContextMenu("Force Proper Initial State")]
    private void ForceProperInitialState()
    {
        EnsureProperInitialState();
    }
}