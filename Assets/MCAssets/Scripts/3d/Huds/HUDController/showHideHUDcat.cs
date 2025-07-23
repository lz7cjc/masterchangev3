using UnityEngine;

/// <summary>
/// Enhanced showHideHUDcat - Fixed selective child activation to prevent movement conflicts
/// </summary>
public class showHideHUDcat : MonoBehaviour
{
    // Exact same public fields as original
    public bool mousehover = false;
    public float Counter = 0;
    public GameObject level3;
    public bool turnon = true;
    public float delay = 3;

    [SerializeField] private hudCountdown hudCountdown;
    [SerializeField] private ToggleActiveIcons ToggleActiveIcons;

    [Header("Selective Activation Settings")]
    [Tooltip("Tags or names of objects that should NOT be auto-activated")]
    public string[] excludedObjectNames = { "PlayerMovement", "Movement", "Player", "Camera" };
    [Tooltip("Only activate objects with these tags (leave empty to activate all except excluded)")]
    public string[] allowedTags = { };

    // Enhanced internal coordination
    private HUDSystemCoordinator hudCoordinator;
    private int categoryIndex = 0;

    public void Start()
    {
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();

        if (ToggleActiveIcons != null)
        {
            ToggleActiveIcons.DefaultIcon();
        }

        AutoDetectCategoryIndex();

        if (level3 == null)
        {
            AutoFindLevel3Container();
        }

        // FIXED: Ensure proper initial state regardless of scene setup
        EnsureProperInitialState();

        Debug.Log($"showHideHUDcat initialized - Category: {categoryIndex}, Level3: {(level3 != null ? level3.name : "NOT ASSIGNED")}");
    }

    // Public methods to control icon states
    public void SetIconToDefault()
    {
        if (ToggleActiveIcons != null)
        {
            ToggleActiveIcons.DefaultIcon();
        }
    }

    public void SetIconToHover()
    {
        if (ToggleActiveIcons != null)
        {
            ToggleActiveIcons.HoverIcon();
        }
    }

    public void SetIconToSelected()
    {
        if (ToggleActiveIcons != null)
        {
            ToggleActiveIcons.SelectIcon();
        }
    }

    void Update()
    {
        if (mousehover)
        {
            SetIconToHover();

            Counter += Time.deltaTime;
            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(delay, Counter);
            }

            if (Counter >= delay)
            {
                mousehover = false;
                Counter = 0;

                ExecuteMenuAction();

                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }
            }
        }
    }

    public void MouseHoverChangeScene()
    {
        // Close any other open Level 3 menus before starting hover
        if (hudCoordinator != null)
        {
            hudCoordinator.ForceCloseAllOtherLevel3Menus(categoryIndex);
        }
        else
        {
            // Fallback: close other menus manually
            CloseAllOtherLevel3MenusManually();
        }

        mousehover = true;
        Counter = 0;

        if (hudCoordinator != null)
        {
            hudCoordinator.OnLevel2CategoryHovered(categoryIndex);
        }

        Debug.Log($"MouseHoverChangeScene - Category: {categoryIndex}");
    }

    public void MouseExit()
    {
        // Only reset icon to default if menu is not open
        if (!IsCurrentMenuOpen())
        {
            SetIconToDefault();
        }

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
        mousehover = false;
        Counter = 0;
    }

    public void ResetHUDState()
    {
        turnon = true;
        if (level3 != null)
        {
            CloseLevel3MenuCompletely();
        }

        SetIconToDefault();
    }

    public void SetCategoryIndex(int index)
    {
        categoryIndex = index;
        Debug.Log($"Category index manually set to: {index}");
    }

    public int GetCategoryIndex()
    {
        return categoryIndex;
    }

    public void LinkToLevel3Container(GameObject container)
    {
        level3 = container;
        Debug.Log($"Manually linked to Level3 container: {container?.name}");
    }

    public void ForceCloseMyLevel3()
    {
        if (level3 != null && level3.activeInHierarchy)
        {
            CloseLevel3MenuCompletely();
            turnon = true;
            if (ToggleActiveIcons != null)
            {
                ToggleActiveIcons.DefaultIcon();
            }
            Debug.Log($"Force closed Level3: {level3.name}");
        }
    }

    private void EnsureProperInitialState()
    {
        // FIXED: Force proper initial state regardless of scene setup
        if (level3 != null)
        {
            CloseLevel3MenuCompletely();
        }

        turnon = true;
        SetIconToDefault();

        Debug.Log($"Ensured proper initial state for category {categoryIndex}");
    }

    private void AutoDetectCategoryIndex()
    {
        string objectName = gameObject.name.ToLower();

        // FIXED: Corrected mapping to match your HUDSystemCoordinator setup
        if (objectName.Contains("location") || objectName.Contains("zone") || objectName.Contains("travel"))
        {
            categoryIndex = 0; // Maps to Level3a (locations) - Element 0
        }
        else if (objectName.Contains("move") || objectName.Contains("walk") || objectName.Contains("speed"))
        {
            categoryIndex = 1; // Maps to Level3b (move) - Element 1
        }
        else if (objectName.Contains("home") || objectName.Contains("main"))
        {
            categoryIndex = 0; // Default to locations
        }
        else if (objectName.Contains("dashboard") || objectName.Contains("settings"))
        {
            categoryIndex = 2; // For future expansion
        }
        else
        {
            // Fallback: use position in parent
            Transform parent = transform.parent;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    if (parent.GetChild(i) == transform)
                    {
                        categoryIndex = i;
                        break;
                    }
                }
            }
        }

        Debug.Log($"Auto-detected category index: {categoryIndex} for {gameObject.name}");
    }

    private void AutoFindLevel3Container()
    {
        GameObject foundContainer = null;

        // FIXED: Corrected mapping to match your setup
        if (categoryIndex == 0) // locations
        {
            foundContainer = GameObject.Find("Level3a (locations)");
            if (foundContainer == null) foundContainer = GameObject.Find("Level3a");
        }
        else if (categoryIndex == 1) // move
        {
            foundContainer = GameObject.Find("Level3b (move)");
            if (foundContainer == null) foundContainer = GameObject.Find("Level3b");
        }

        if (foundContainer != null)
        {
            level3 = foundContainer;
            Debug.Log($"Auto-assigned Level3 container: {foundContainer.name}");
        }
        else
        {
            Debug.LogWarning($"Could not auto-find Level3 container for category: {categoryIndex}");
        }
    }

    private void ExecuteMenuAction()
    {
        Debug.Log($"ExecuteMenuAction - Category: {categoryIndex}, Level3: {(level3 != null ? level3.name : "NULL")}, TurnOn: {turnon}");

        if (hudCoordinator != null)
        {
            if (IsCurrentMenuOpen())
            {
                // Closing current menu
                CloseLevel3MenuCompletely();
                turnon = true;
                SetIconToDefault();
                Debug.Log($"Closed Level 3 menu - Category: {categoryIndex}");
            }
            else
            {
                // Opening this menu - coordinator will handle closing others
                Debug.Log($"Opening Level 3 menu for category: {categoryIndex}");
                hudCoordinator.OnLevel3MenuOpened(categoryIndex);
                turnon = false;
                SetIconToSelected();
                Debug.Log($"Opened Level 3 menu - Category: {categoryIndex}");
            }
        }
        else
        {
            Debug.Log("No coordinator found, using direct method");

            if (turnon)
            {
                // Close all other menus first
                CloseAllOtherLevel3MenusManually();

                OpenLevel3MenuCompletely();
                turnon = false;
                SetIconToSelected();
            }
            else
            {
                CloseLevel3MenuCompletely();
                turnon = true;
                SetIconToDefault();
            }
        }
    }

    private void CloseAllOtherLevel3MenusManually()
    {
        // Find all other showHideHUDcat scripts and close their menus
        showHideHUDcat[] allHudCats = FindObjectsOfType<showHideHUDcat>();
        foreach (showHideHUDcat hudCat in allHudCats)
        {
            if (hudCat != this && hudCat.level3 != null && hudCat.level3.activeInHierarchy)
            {
                Debug.Log($"Manually closing other Level3: {hudCat.level3.name}");
                hudCat.CloseLevel3MenuCompletely();
                hudCat.turnon = true;
                hudCat.SetIconToDefault();
            }
        }
    }

    private bool IsCurrentMenuOpen()
    {
        return level3 != null && level3.activeInHierarchy;
    }

    // FIXED: Safer Level3 menu opening with selective child activation
    private void OpenLevel3MenuCompletely()
    {
        if (level3 == null)
        {
            Debug.LogError($"Cannot open Level3 menu - level3 field is not assigned on {gameObject.name}!");
            return;
        }

        // First activate the main container
        level3.SetActive(true);

        // FIXED: Use selective activation instead of activating everything
        ActivateUIChildrenSelectively(level3.transform);

        // Notify PlayerMovement1 that movement menu opened (if this is movement category)
        if (categoryIndex == 1) // Movement category
        {
            NotifyMovementMenuOpened();
        }

        Debug.Log($"Opened Level 3 menu selectively: {level3.name}");
    }

    // FIXED: Proper Level3 menu closing
    private void CloseLevel3MenuCompletely()
    {
        if (level3 != null)
        {
            // Notify PlayerMovement1 that movement menu closed (if this is movement category)
            if (categoryIndex == 1) // Movement category
            {
                NotifyMovementMenuClosed();
            }

            level3.SetActive(false);
            Debug.Log($"Closed Level 3 menu: {level3.name}");
        }
    }

    // FIXED: Selective activation that won't interfere with movement systems
    private void ActivateUIChildrenSelectively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // Check if this object should be excluded
            if (ShouldExcludeFromActivation(child.gameObject))
            {
                Debug.Log($"Skipping activation of: {child.name} (excluded)");
                continue;
            }

            // Check if it has movement-related components that shouldn't be auto-activated
            if (HasMovementComponents(child.gameObject))
            {
                Debug.Log($"Skipping activation of: {child.name} (has movement components)");
                continue;
            }

            // Activate UI elements only
            if (IsUIElement(child.gameObject))
            {
                child.gameObject.SetActive(true);
                Debug.Log($"Activated UI element: {child.name}");

                // Recursively activate UI children
                ActivateUIChildrenSelectively(child);
            }
            else
            {
                // For non-UI elements, still check children but don't activate the parent
                ActivateUIChildrenSelectively(child);
            }
        }
    }

    private bool ShouldExcludeFromActivation(GameObject obj)
    {
        string objName = obj.name.ToLower();

        foreach (string excludedName in excludedObjectNames)
        {
            if (objName.Contains(excludedName.ToLower()))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasMovementComponents(GameObject obj)
    {
        // Check for movement-related components
        return obj.GetComponent<PlayerMovement1>() != null ||
               obj.GetComponent<floorceilingmove>() != null ||
               obj.GetComponent<Rigidbody>() != null ||
               obj.GetComponent<CharacterController>() != null;
    }

    private bool IsUIElement(GameObject obj)
    {
        // Check if it's a UI element (has Canvas, UI components, etc.)
        return obj.GetComponent<UnityEngine.UI.Image>() != null ||
               obj.GetComponent<UnityEngine.UI.Button>() != null ||
               obj.GetComponent<UnityEngine.UI.Text>() != null ||
               obj.GetComponent<TMPro.TextMeshProUGUI>() != null ||
               obj.GetComponent<UnityEngine.Canvas>() != null ||
               obj.GetComponent<UnityEngine.CanvasGroup>() != null ||
               (allowedTags.Length > 0 && System.Array.Exists(allowedTags, tag => obj.CompareTag(tag))) ||
               (allowedTags.Length == 0 && obj.name.ToLower().Contains("ui")) ||
               obj.name.ToLower().Contains("button") ||
               obj.name.ToLower().Contains("text") ||
               obj.name.ToLower().Contains("image") ||
               obj.name.ToLower().Contains("panel");
    }

    private void NotifyMovementMenuOpened()
    {
        // Find and notify PlayerMovement1 script that the menu opened
        PlayerMovement1 playerMovement = FindFirstObjectByType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuOpened();
            Debug.Log("Notified PlayerMovement1 that movement menu opened");
        }
    }

    private void NotifyMovementMenuClosed()
    {
        // Find and notify PlayerMovement1 script that the menu closed
        PlayerMovement1 playerMovement = FindFirstObjectByType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuClosed();
            Debug.Log("Notified PlayerMovement1 that movement menu closed");
        }
    }

    // DEPRECATED: Old method - kept for compatibility but not used
    private void ActivateAllChildrenRecursively(Transform parent)
    {
        // This method is now deprecated and replaced with ActivateUIChildrenSelectively
        Debug.LogWarning("ActivateAllChildrenRecursively is deprecated. Use ActivateUIChildrenSelectively instead.");
    }

    private void SetActiveRecursively(GameObject obj, bool state)
    {
        if (obj == null) return;

        obj.SetActive(state);
        foreach (Transform child in obj.transform)
        {
            SetActiveRecursively(child.gameObject, state);
        }
    }

    // Legacy methods for compatibility
    private void OpenLevel3Menu()
    {
        OpenLevel3MenuCompletely();
    }

    private void CloseLevel3Menu()
    {
        CloseLevel3MenuCompletely();
    }
}