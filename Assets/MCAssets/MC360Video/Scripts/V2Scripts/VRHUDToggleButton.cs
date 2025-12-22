using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Toggle button for HUD open/close.
/// Shows + when closed, X when open.
/// Attach to the open/close button.
/// </summary>
public class VRHUDToggleButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Toggle Settings")]
    [SerializeField] private string buttonName = "OpenClose";
    [SerializeField] private float hoverDelaySeconds = 2f;
    [SerializeField] private bool startOpen = false; // Is HUD open by default?

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Sprites - Closed State")]
    [SerializeField] private Sprite closedDefaultSprite; // + icon
    [SerializeField] private Sprite closedHoverSprite;   // + icon highlighted

    [Header("Sprites - Open State")]
    [SerializeField] private Sprite openDefaultSprite;   // X icon
    [SerializeField] private Sprite openHoverSprite;     // X icon highlighted

    [Header("Target Panels")]
    [SerializeField] private GameObject[] panelsToToggle; // Level2Panel, etc.

    [Header("Countdown Display")]
    [SerializeField] private hudCountdown countdownDisplay;

    [Header("Events")]
    public UnityEvent OnOpen;
    public UnityEvent OnClose;

    // State
    private bool isOpen;
    private bool isHovering = false;
    private float hoverTimer = 0f;
    private bool actionTriggered = false;

    void Start()
    {
        isOpen = startOpen;
        UpdateVisuals();
        UpdatePanelStates();
    }

    void Update()
    {
        if (isHovering && !actionTriggered)  // Add this check
        {
            hoverTimer += Time.deltaTime;

            // Update countdown display
            if (countdownDisplay != null)
            {
                countdownDisplay.SetCountdown(hoverDelaySeconds, hoverTimer);
            }

            // Trigger toggle when timer exceeds delay
            if (hoverTimer >= hoverDelaySeconds)
            {
                ToggleState();
            }
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverTimer = 0f;
        actionTriggered = false;

        // Change to hover sprite based on current state
        if (iconRenderer != null)
        {
            if (isOpen)
            {
                if (openHoverSprite != null)
                    iconRenderer.sprite = openHoverSprite;
            }
            else
            {
                if (closedHoverSprite != null)
                    iconRenderer.sprite = closedHoverSprite;
            }
        }

        Debug.Log($"VRHUDToggleButton: Hover enter - {buttonName}");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        hoverTimer = 0f;
        actionTriggered = false;

        // Reset countdown display
        if (countdownDisplay != null)
        {
            countdownDisplay.resetCountdown();
        }

        // Change back to default sprite based on current state
        UpdateVisuals();

        Debug.Log($"VRHUDToggleButton: Hover exit - {buttonName}");
    }

    private void ToggleState()
    {
        actionTriggered = true;
        isOpen = !isOpen;

        Debug.Log($"VRHUDToggleButton: Toggled to {(isOpen ? "OPEN" : "CLOSED")}");

        // Update visuals and panels
        UpdateVisuals();
        UpdatePanelStates();

        // Fire events
        if (isOpen)
        {
            OnOpen?.Invoke();
        }
        else
        {
            OnClose?.Invoke();
        }
    }

    /// <summary>
    /// Update button sprite based on current state
    /// </summary>
    private void UpdateVisuals()
    {
        if (iconRenderer == null) return;

        if (isOpen)
        {
            // Show X (open state)
            if (isHovering && openHoverSprite != null)
            {
                iconRenderer.sprite = openHoverSprite;
            }
            else if (openDefaultSprite != null)
            {
                iconRenderer.sprite = openDefaultSprite;
            }
        }
        else
        {
            // Show + (closed state)
            if (isHovering && closedHoverSprite != null)
            {
                iconRenderer.sprite = closedHoverSprite;
            }
            else if (closedDefaultSprite != null)
            {
                iconRenderer.sprite = closedDefaultSprite;
            }
        }
    }

    /// <summary>
    /// Show/hide panels based on open state
    /// </summary>
    private void UpdatePanelStates()
    {
        if (panelsToToggle == null) return;

        foreach (GameObject panel in panelsToToggle)
        {
            if (panel != null)
            {
                panel.SetActive(isOpen);
            }
        }

        Debug.Log($"VRHUDToggleButton: Panels set to {(isOpen ? "active" : "inactive")}");
    }

    /// <summary>
    /// Manually set open/closed state
    /// </summary>
    public void SetOpen(bool open)
    {
        isOpen = open;
        UpdateVisuals();
        UpdatePanelStates();

        Debug.Log($"VRHUDToggleButton: Manually set to {(isOpen ? "OPEN" : "CLOSED")}");
    }

    /// <summary>
    /// Check current state
    /// </summary>
    public bool IsOpen() => isOpen;

    /// <summary>
    /// Manual toggle (for testing)
    /// </summary>
    [ContextMenu("Toggle State")]
    public void ManualToggle()
    {
        isOpen = !isOpen;
        UpdateVisuals();
        UpdatePanelStates();

        if (isOpen)
        {
            OnOpen?.Invoke();
        }
        else
        {
            OnClose?.Invoke();
        }
    }
}