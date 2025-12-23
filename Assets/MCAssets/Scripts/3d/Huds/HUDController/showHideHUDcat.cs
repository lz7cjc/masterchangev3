using UnityEngine;

public class showHideHUDcat : MonoBehaviour
{
    [Header("Level 3 Container")]
    public GameObject level3;

    [Header("Icon Controller")]
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    [Header("Category Settings")]
    public int categoryIndex = 0;

    [Header("Selective Activation")]
    public string[] excludedObjectNames = { "PlayerMovement", "Movement", "Player", "Camera" };

    private HUDSystemCoordinator hudCoordinator;
    private bool isOpen = false;

    void Start()
    {
        hudCoordinator = FindObjectOfType<HUDSystemCoordinator>();

        if (categoryIndex == 0 && gameObject.name.Contains("walk", System.StringComparison.OrdinalIgnoreCase))
        {
            categoryIndex = 1;
        }

        if (level3 == null)
        {
            AutoFindLevel3Container();
        }

        if (level3 != null)
        {
            level3.SetActive(false);
        }
        isOpen = false;

        SetIconToDefault();

        Debug.Log($"[showHideHUDcat] Init - Category: {categoryIndex}, Level3: {level3?.name ?? "NOT ASSIGNED"}");
    }

    public void ToggleLevel3Menu()
    {
        if (isOpen)
        {
            CloseLevel3Menu();
        }
        else
        {
            OpenLevel3Menu();
        }
    }

    private void OpenLevel3Menu()
    {
        if (hudCoordinator != null)
        {
            hudCoordinator.ForceCloseAllOtherLevel3Menus(categoryIndex);
        }
        else
        {
            CloseAllOtherLevel3MenusManually();
        }

        if (level3 != null)
        {
            level3.SetActive(true);
            ActivateUIChildrenSelectively(level3.transform);
            isOpen = true;
            SetIconToSelected();

            if (categoryIndex == 1)
            {
                NotifyMovementMenuOpened();
            }

            if (hudCoordinator != null)
            {
                hudCoordinator.OnLevel3MenuOpened(categoryIndex);
            }

            Debug.Log($"[showHideHUDcat] Opened: {level3.name}");
        }
    }

    private void CloseLevel3Menu()
    {
        if (level3 != null)
        {
            if (categoryIndex == 1)
            {
                NotifyMovementMenuClosed();
            }

            level3.SetActive(false);
            isOpen = false;
            SetIconToDefault();

            Debug.Log($"[showHideHUDcat] Closed: {level3.name}");
        }
    }

    public void ForceCloseMyLevel3()
    {
        if (isOpen)
        {
            CloseLevel3Menu();
        }
    }

    public void ResetHUDState()
    {
        if (level3 != null)
        {
            level3.SetActive(false);
        }
        isOpen = false;
        SetIconToDefault();
    }

    public void SetIconToDefault()
    {
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.DefaultIcon();
        }
    }

    private void SetIconToHover()
    {
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.HoverIcon();
        }
    }

    private void SetIconToSelected()
    {
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
        }
    }

    public void OnHoverStart()
    {
        SetIconToHover();
    }

    public void OnHoverCancel()
    {
        if (!isOpen)
        {
            SetIconToDefault();
        }
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
            foundContainer = GameObject.Find("Level3b (walk)");
            if (foundContainer == null) foundContainer = GameObject.Find("Level3b");
            if (foundContainer == null) foundContainer = GameObject.Find("Level3b (move)");
        }

        if (foundContainer != null)
        {
            level3 = foundContainer;
        }
    }

    private void CloseAllOtherLevel3MenusManually()
    {
        showHideHUDcat[] allHudCats = FindObjectsOfType<showHideHUDcat>();
        foreach (showHideHUDcat hudCat in allHudCats)
        {
            if (hudCat != this && hudCat.isOpen)
            {
                hudCat.ForceCloseMyLevel3();
            }
        }
    }

    private void ActivateUIChildrenSelectively(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (ShouldExcludeFromActivation(child.gameObject))
            {
                continue;
            }

            if (HasMovementComponents(child.gameObject))
            {
                continue;
            }

            if (IsUIElement(child.gameObject))
            {
                child.gameObject.SetActive(true);
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
               obj.name.ToLower().Contains("ui") ||
               obj.name.ToLower().Contains("button") ||
               obj.name.ToLower().Contains("text") ||
               obj.name.ToLower().Contains("image") ||
               obj.name.ToLower().Contains("panel");
    }

    private void NotifyMovementMenuOpened()
    {
        PlayerMovement1 playerMovement = FindObjectOfType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuOpened();
        }
    }

    private void NotifyMovementMenuClosed()
    {
        PlayerMovement1 playerMovement = FindObjectOfType<PlayerMovement1>();
        if (playerMovement != null)
        {
            playerMovement.OnMovementMenuClosed();
        }
    }

    public int GetCategoryIndex() => categoryIndex;
    public bool IsOpen => isOpen;
    public GameObject GetLevel3Container() => level3;
}
