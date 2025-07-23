using UnityEngine;

/// <summary>
/// Enhanced ToggleActiveIcons - Optimized with reduced redundancy
/// </summary>
public class ToggleActiveIcons : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite;

    // Enhanced internal state tracking
    private IconState currentState = IconState.Default;
    private HUDSystemCoordinator hudCoordinator;

    public enum IconState
    {
        Default,
        Hover,
        Selected
    }

    public void Start()
    {
        Debug.Log("Enhanced ToggleActiveIcons initialized");

        // Try to find coordinator (optional)
        hudCoordinator = FindFirstObjectByType<HUDSystemCoordinator>();

        // Auto-assign spriteRenderer if not set
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Set initial state
        DefaultIcon();
    }

    // Exact same public methods as original
    public void SelectIcon()
    {
        Debug.Log("in SelectIcon of enhanced toggle active icons");
        SetIconState(IconState.Selected);
    }

    public void HoverIcon()
    {
        Debug.Log("in HoverIcon of enhanced toggle active icons");
        SetIconState(IconState.Hover);
    }

    public void DefaultIcon()
    {
        Debug.Log("in DefaultIcon of enhanced toggle active icons");
        SetIconState(IconState.Default);
    }

    #region Enhanced Internal Implementation

    private void SetIconState(IconState newState)
    {
        if (currentState == newState) return; // Avoid unnecessary changes

        currentState = newState;

        Sprite targetSprite = GetSpriteForState(newState);

        if (spriteRenderer != null && targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
        }
        else if (targetSprite == null)
        {
            Debug.LogWarning($"Missing sprite for state {newState} on {gameObject.name}");
        }
    }

    private Sprite GetSpriteForState(IconState state)
    {
        switch (state)
        {
            case IconState.Default:
                return defaultSprite;
            case IconState.Hover:
                return hoverSprite;
            case IconState.Selected:
                return selectedSprite;
            default:
                return defaultSprite;
        }
    }

    #endregion

    #region Enhanced Public Properties

    /// <summary>
    /// Get current icon state
    /// </summary>
    public IconState CurrentState => currentState;

    /// <summary>
    /// Check if icon is in specific state
    /// </summary>
    public bool IsInState(IconState state) => currentState == state;

    /// <summary>
    /// Set sprites programmatically
    /// </summary>
    public void SetSprites(Sprite defaultSpr, Sprite hoverSpr, Sprite selectedSpr)
    {
        defaultSprite = defaultSpr;
        hoverSprite = hoverSpr;
        selectedSprite = selectedSpr;

        // Refresh current state
        var currentStateTemp = currentState;
        currentState = IconState.Default; // Force refresh
        SetIconState(currentStateTemp);
    }

    /// <summary>
    /// Get sprite for a specific state
    /// </summary>
    public Sprite GetSprite(IconState state)
    {
        return GetSpriteForState(state);
    }

    #endregion

    #region Inspector Helpers

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void OnValidate()
    {
        // Auto-assign spriteRenderer if not set
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    [ContextMenu("Test Icon States")]
    private void TestIconStates()
    {
        if (Application.isPlaying)
        {
            Debug.Log("Testing icon states...");
            DefaultIcon();
            Invoke(nameof(TestHover), 1f);
            Invoke(nameof(TestSelected), 2f);
            Invoke(nameof(TestDefault), 3f);
        }
    }

    private void TestHover() => HoverIcon();
    private void TestSelected() => SelectIcon();
    private void TestDefault() => DefaultIcon();

    [ContextMenu("Validate Sprite Assignments")]
    private void ValidateSprites()
    {
        string status = "Sprite Status:\n";
        status += $"- Default: {(defaultSprite != null ? "✓" : "✗")}\n";
        status += $"- Hover: {(hoverSprite != null ? "✓" : "✗")}\n";
        status += $"- Selected: {(selectedSprite != null ? "✓" : "✗")}\n";
        status += $"- Renderer: {(spriteRenderer != null ? "✓" : "✗")}";

        Debug.Log(status);
    }

    #endregion
}