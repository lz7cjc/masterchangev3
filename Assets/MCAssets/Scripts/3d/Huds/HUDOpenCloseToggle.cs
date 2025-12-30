using UnityEngine;

/// <summary>
/// HUD Open/Close Toggle - Single icon that changes sprites based on HUD state
/// Uses 4 sprites: openDefault, openHover, closeDefault, closeHover
/// Works exactly like VR toggle pattern
/// </summary>
public class HUDOpenCloseToggle : MonoBehaviour
{
    [Header("HUD Levels")]
    [SerializeField] private GameObject level2MainMenu;
    [SerializeField] private GameObject level3aLocations;
    [SerializeField] private GameObject level3bMove;

    [Header("Sprites - Open (HUD Closed)")]
    [SerializeField] private Sprite openDefaultSprite;
    [SerializeField] private Sprite openHoverSprite;

    [Header("Sprites - Close (HUD Open)")]
    [SerializeField] private Sprite closeDefaultSprite;
    [SerializeField] private Sprite closeHoverSprite;

    [Header("Sprite Renderer")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isHUDOpen = false;

    private void Start()
    {
        // Auto-find sprite renderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Initialize - everything closed
        if (level2MainMenu != null) level2MainMenu.SetActive(false);
        if (level3aLocations != null) level3aLocations.SetActive(false);
        if (level3bMove != null) level3bMove.SetActive(false);

        // Show open icon (HUD is closed)
        UpdateSprite(false);

        isHUDOpen = false;

        Debug.Log("[HUDOpenCloseToggle] Initialized - HUD closed, showing open icon");
    }

    /// <summary>
    /// Toggle HUD open/closed
    /// Called by GazeHoverTrigger On Hover Complete
    /// </summary>
    public void ToggleHUD()
    {
        if (!isHUDOpen)
        {
            OpenHUD();
        }
        else
        {
            CloseHUD();
        }
    }

    /// <summary>
    /// Show hover sprite (called by GazeHoverTrigger On Hover Start)
    /// </summary>
    public void ShowHoverIcon()
    {
        if (spriteRenderer == null) return;

        // Show appropriate hover sprite based on current state
        spriteRenderer.sprite = isHUDOpen ? closeHoverSprite : openHoverSprite;
        
        Debug.Log($"[HUDOpenCloseToggle] Hover - showing {(isHUDOpen ? "close" : "open")} hover sprite");
    }

    /// <summary>
    /// Show default sprite (called by GazeHoverTrigger On Hover Cancel)
    /// </summary>
    public void ShowDefaultIcon()
    {
        UpdateSprite(isHUDOpen);
        Debug.Log($"[HUDOpenCloseToggle] Hover canceled - back to default");
    }

    private void OpenHUD()
    {
        Debug.Log("[HUDOpenCloseToggle] Opening HUD");

        // Show Level2
        if (level2MainMenu != null)
        {
            level2MainMenu.SetActive(true);
            Debug.Log($"[HUDOpenCloseToggle] Activated {level2MainMenu.name}");
        }

        // Hide Level3 menus
        if (level3aLocations != null) level3aLocations.SetActive(false);
        if (level3bMove != null) level3bMove.SetActive(false);

        // Update state and sprite
        isHUDOpen = true;
        UpdateSprite(true); // Now show close icon

        Debug.Log("[HUDOpenCloseToggle] HUD OPEN - now showing close icon");
    }

    private void CloseHUD()
    {
        Debug.Log("[HUDOpenCloseToggle] Closing HUD");

        // Hide all levels
        if (level2MainMenu != null) level2MainMenu.SetActive(false);
        if (level3aLocations != null) level3aLocations.SetActive(false);
        if (level3bMove != null) level3bMove.SetActive(false);

        // Update state and sprite
        isHUDOpen = false;
        UpdateSprite(false); // Now show open icon

        Debug.Log("[HUDOpenCloseToggle] HUD CLOSED - now showing open icon");
    }

    /// <summary>
    /// Update sprite based on HUD state
    /// </summary>
    /// <param name="hudIsOpen">True = show close icon, False = show open icon</param>
    private void UpdateSprite(bool hudIsOpen)
    {
        if (spriteRenderer == null) return;

        spriteRenderer.sprite = hudIsOpen ? closeDefaultSprite : openDefaultSprite;
    }

    /// <summary>
    /// Public method to check if HUD is open
    /// </summary>
    public bool IsOpen()
    {
        return isHUDOpen;
    }

    /// <summary>
    /// Public method to force close HUD
    /// </summary>
    public void ForceClose()
    {
        if (isHUDOpen)
        {
            CloseHUD();
        }
    }

    // DEBUG: Manual toggle for testing
    [ContextMenu("Manual Toggle HUD")]
    private void ManualToggle()
    {
        if (Application.isPlaying)
        {
            ToggleHUD();
        }
    }

    [ContextMenu("Test Hover Icon")]
    private void TestHover()
    {
        if (Application.isPlaying)
        {
            ShowHoverIcon();
        }
    }

    [ContextMenu("Test Default Icon")]
    private void TestDefault()
    {
        if (Application.isPlaying)
        {
            ShowDefaultIcon();
        }
    }
}
