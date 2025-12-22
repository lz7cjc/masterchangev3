using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// UI Canvas version of VRHUDButton - handles HUD button interactions for Unity UI.
/// Works with Image components (not SpriteRenderer).
/// Attach to each UI Button GameObject in your HUD Canvas.
/// Works with VRReticlePointer for both 360 and VR modes.
/// </summary>
public class VRHUDButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Action Settings")]
    [SerializeField] private string actionName; // For debugging and identification
    [SerializeField] private float hoverDelaySeconds = 2f; // Hold time before trigger
    [SerializeField] private bool requireHoldToTrigger = true;

    [Header("Visual Feedback - UI Image")]
    [SerializeField] private Image buttonImage; // The UI Image component (NOT SpriteRenderer!)
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite; // Optional - leave empty if not needed

    [Header("Color Feedback (Alternative to Sprites)")]
    [SerializeField] private bool useColorInsteadOfSprites = false;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color hoverColor = Color.cyan;
    [SerializeField] private Color selectedColor = Color.green;

    [Header("Scale Feedback")]
    [SerializeField] private bool useScaleFeedback = false;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float selectedScale = 0.95f;
    private Vector3 originalScale;

    [Header("Countdown Display")]
    [SerializeField] private hudCountdown countdownDisplay; // Reference to countdown UI
    [SerializeField] private bool autoFindCountdown = true; // Try to find CountdownUI automatically

    [Header("Events")]
    public UnityEvent OnButtonTriggered;

    // State
    private bool isHovering = false;
    private float hoverTimer = 0f;
    private bool actionTriggered = false;

    private void Start()
    {
        // Auto-assign button image if not set
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
            if (buttonImage == null)
            {
                // Try to find in children
                buttonImage = GetComponentInChildren<Image>();
            }
        }

        // Store original scale
        originalScale = transform.localScale;

        // Auto-find countdown display if enabled
        if (autoFindCountdown && countdownDisplay == null)
        {
            countdownDisplay = FindObjectOfType<hudCountdown>();
            if (countdownDisplay != null)
            {
                Debug.Log($"[VRHUDButtonUI] Auto-found countdown display for {actionName}");
            }
        }

        // Set initial visual state
        SetVisualState(ButtonState.Default);

        Debug.Log($"[VRHUDButtonUI] Initialized: {actionName}");
    }

    void Update()
    {
        if (isHovering && requireHoldToTrigger && !actionTriggered)
        {
            hoverTimer += Time.deltaTime;

            // Update countdown display
            if (countdownDisplay != null)
            {
                countdownDisplay.SetCountdown(hoverDelaySeconds, hoverTimer);
            }

            // Trigger action when timer exceeds delay
            if (hoverTimer >= hoverDelaySeconds)
            {
                TriggerAction();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverTimer = 0f;
        actionTriggered = false;

        SetVisualState(ButtonState.Hover);

        Debug.Log($"[VRHUDButtonUI] Hover enter - {actionName}");
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

        SetVisualState(ButtonState.Default);

        Debug.Log($"[VRHUDButtonUI] Hover exit - {actionName}");
    }

    private void TriggerAction()
    {
        actionTriggered = true;

        SetVisualState(ButtonState.Selected);

        // Invoke the event
        OnButtonTriggered?.Invoke();

        Debug.Log($"[VRHUDButtonUI] ✓ Action triggered - {actionName}");

        // Reset after short delay
        Invoke(nameof(ResetButton), 0.3f);
    }

    private void ResetButton()
    {
        if (!isHovering)
        {
            SetVisualState(ButtonState.Default);
        }
    }

    private void SetVisualState(ButtonState state)
    {
        if (buttonImage == null) return;

        // Apply sprite change (if using sprites)
        if (!useColorInsteadOfSprites)
        {
            switch (state)
            {
                case ButtonState.Default:
                    if (defaultSprite != null)
                        buttonImage.sprite = defaultSprite;
                    break;
                case ButtonState.Hover:
                    if (hoverSprite != null)
                        buttonImage.sprite = hoverSprite;
                    break;
                case ButtonState.Selected:
                    if (selectedSprite != null)
                        buttonImage.sprite = selectedSprite;
                    break;
            }
        }

        // Apply color change (if using colors)
        if (useColorInsteadOfSprites)
        {
            switch (state)
            {
                case ButtonState.Default:
                    buttonImage.color = defaultColor;
                    break;
                case ButtonState.Hover:
                    buttonImage.color = hoverColor;
                    break;
                case ButtonState.Selected:
                    buttonImage.color = selectedColor;
                    break;
            }
        }

        // Apply scale feedback
        if (useScaleFeedback)
        {
            switch (state)
            {
                case ButtonState.Default:
                    transform.localScale = originalScale;
                    break;
                case ButtonState.Hover:
                    transform.localScale = originalScale * hoverScale;
                    break;
                case ButtonState.Selected:
                    transform.localScale = originalScale * selectedScale;
                    break;
            }
        }
    }

    /// <summary>
    /// Manually trigger this button (for testing or instant actions)
    /// </summary>
    [ContextMenu("Trigger Button")]
    public void ManualTrigger()
    {
        OnButtonTriggered?.Invoke();
        Debug.Log($"[VRHUDButtonUI] ✓ Manually triggered - {actionName}");
    }

    /// <summary>
    /// Enable instant trigger (no hold required)
    /// </summary>
    [ContextMenu("Set Instant Trigger")]
    public void SetInstantTrigger()
    {
        requireHoldToTrigger = false;
        hoverDelaySeconds = 0f;
        Debug.Log($"[VRHUDButtonUI] {actionName} set to instant trigger");
    }

    /// <summary>
    /// Set hover delay time
    /// </summary>
    public void SetHoverDelay(float seconds)
    {
        hoverDelaySeconds = Mathf.Max(0f, seconds);
        Debug.Log($"[VRHUDButtonUI] {actionName} hover delay set to {hoverDelaySeconds}s");
    }

    private enum ButtonState
    {
        Default,
        Hover,
        Selected
    }
}