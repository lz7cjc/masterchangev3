using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Updated showHideHUDcat - Compatible with VRReticlePointer Event Trigger system
/// NOW WORKS WITH: BaseEventData from Event Triggers
/// </summary>
public class showHideHUDcat : MonoBehaviour
{
    // Public fields - same as before
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

        EnsureProperInitialState();

        Debug.Log($"Updated showHideHUDcat initialized - Category: {categoryIndex}, Level3: {(level3 != null ? level3.name : "NOT ASSIGNED")}");
    }

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

    // ============================================
    // NEW: Event Trigger compatible methods
    // These accept BaseEventData from VRReticlePointer
    // ============================================

    /// <summary>
    /// Called by Event Trigger - Pointer Enter
    /// Accepts BaseEventData (compatible with VRReticlePointer)
    /// </summary>
    public void MouseHoverChangeScene(BaseEventData eventData)
    {
        // Close any other open Level 3 menus before starting hover
        if (hudCoordinator != null)
        {
            hudCoordinator.ForceCloseAllOtherLevel3Menus(categoryIndex);
        }
        else
        {
            CloseAllOtherLevel3MenusManually();
        }

        mousehover = true;
        Counter = 0;

        if (hudCoordinator != null)
        {
            hudCoordinator.OnLevel2CategoryHovered(categoryIndex);
        }

        Debug.Log($"MouseHoverChangeScene (EventData) - Category: {categoryIndex}");
    }

    /// <summary>
    /// Called by Event Trigger - Pointer Exit
    /// Accepts BaseEventData (compatible with VRReticlePointer)
    /// </summary>
    public void MouseExit(BaseEventData eventData)
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

    // ============================================
    // LEGACY: Keep old methods for backward compatibility
    // ============================================

    /// <summary>
    /// Legacy version - no parameters
    /// </summary>
    public void MouseHoverChangeScene()
    {
        MouseHoverChangeScene(null);
    }

    /// <summary>
    /// Legacy version - no parameters
    /// </summary>
    public void MouseExit()
    {
        MouseExit(null);
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

        if (objectName.Contains("location") || objectName.Contains("zone") || objectName.Contains("travel"))
        {
            categoryIndex = 0;
        }
        else if (objectName.Contains("move") || objectName.Contains("walk") || objectName.Contains("speed"))
        {
            categoryIndex = 1;
        }
        else if (objectName.Contains("home") || objectName.Contains("main"))
        {
            categoryIndex = 0;
        }
        else if (objectName.Contains("dashboard") || objectName.Contains("settings"))
        {
            categoryIndex = 2;
        }
        else
        {
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

        if (categoryIndex == 0)
        {
            foundContainer = GameObject.Find("Level3a (locations)");
            if (foundContainer == null) foundContainer = GameObject.Find("Level3a");
        }
        else if (categoryIndex == 1)
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
                CloseLevel3MenuCompletely();
                turnon = true;
                SetIconToDefault();
                Debug.Log($"Closed Level 3 menu - Category: {categoryIndex}");
            }
            else
            {
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

    private void OpenLevel3MenuCompletely()
    {
        if (level3 == null)
        {
            Debug.LogError($"Cannot open Level3 menu - level3 field is not assigned on {gameObject.name}!");
            return;
        }

        level3.SetActive(true);
        ActivateUIChildrenSelectively(level3.transform);

        if (categoryIndex == 1)
        {
            NotifyMovementMenuOpened();
        }

        Debug.Log($"Opened Level 3 menu selectively: {level3.name}");
    }

    private void CloseLevel3MenuCompletely()
    {
        if (level3 != null)
        {
            if (categoryIndex == 1)
            {
                NotifyMovementMenuClosed();
            }

            level3.SetActive(false);
            Debug.Log($"Closed Level 3 menu: {level3.name}");
        }
    }

    private void ActivateUIChildrenSelectively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (ShouldExcludeFromActivation(child.gameObject))
            {
                Debug.Log($"Skipping activation of: {child.name} (excluded)");
                continue;
            }

            if (HasMovementComponents(child.gameObject))
            {
                Debug.Log($"Skipping activation of: {child.name} (has movement components)");
                continue;
            }

            if (IsUIElement(child.gameObject))
            {
                child.gameObject.SetActive(true);
                Debug.Log($"Activated UI element: {child.name}");
                ActivateUIChildrenSelectively(child);
            }
            else
            {
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
        return obj.GetComponent<PlayerMovement1>() != null ||
               obj.GetComponent<floorceilingmove>() != null ||
               obj.GetComponent<Rigidbody>() != null ||
               obj.GetComponent<CharacterController>() != null;
    }

    private bool IsUIElement(GameObject obj)
    {
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
        PlayerMovement1 playerMovement = FindFirstObjectByType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuOpened();
            Debug.Log("Notified PlayerMovement1 that movement menu opened");
        }
    }

    private void NotifyMovementMenuClosed()
    {
        PlayerMovement1 playerMovement = FindFirstObjectByType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuClosed();
            Debug.Log("Notified PlayerMovement1 that movement menu closed");
        }
    }
}