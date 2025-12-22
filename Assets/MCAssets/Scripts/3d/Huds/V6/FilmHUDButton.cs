using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Professional button component for Film HUD.
/// Handles reticle pointer interaction with smooth visual feedback.
/// 
/// ATTACH TO: Each button GameObject in Film HUD
/// 
/// FEATURES:
/// - Smooth hover animations
/// - Visual countdown feedback
/// - Multiple button types (instant, hold, toggle)
/// - Works with VRReticlePointerFixed
/// </summary>
[RequireComponent(typeof(Image))]
public class FilmHUDButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Button Type")]
    [SerializeField] private ButtonType buttonType = ButtonType.Hold;

    [Header("Action Settings")]
    [SerializeField] private string actionName = "Button";
    [SerializeField, Range(0f, 5f)] private float holdDuration = 2f;

    [Header("Visual Feedback - Sprites")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite activeSprite;

    [Header("Visual Feedback - Colors (if no hover sprite)")]
    [SerializeField] private bool useColorFeedback = true;
    [SerializeField] private Color defaultColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.8f, 1f, 1f);
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.4f, 1f);

    [Header("Scale Animation")]
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float activeScale = 0.95f;
    [SerializeField] private float scaleSpeed = 10f;

    [Header("Countdown Feedback")]
    [SerializeField] private Image fillImage; // Radial fill for countdown
    [SerializeField] private hudCountdown countdownDisplay;
    [SerializeField] private bool autoFindCountdown = true;

    [Header("Events")]
    public UnityEvent OnButtonTriggered;

    // State
    private bool isHovering = false;
    private float hoverTimer = 0f;
    private bool actionTriggered = false;
    private Vector3 originalScale;
    private Vector3 targetScale;
    private ButtonState currentState = ButtonState.Default;

    // For toggle buttons
    private bool isToggled = false;

    public enum ButtonType
    {
        Instant,    // Triggers immediately on hover
        Hold,       // Requires holding hover for duration
        Toggle      // Toggles between on/off states
    }

    public enum ButtonState
    {
        Default,
        Hover,
        Active
    }

    private void Start()
    {
        // Auto-assign button image if not set
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        // Store original scale
        originalScale = transform.localScale;
        targetScale = originalScale;

        // Auto-find countdown display
        if (autoFindCountdown && countdownDisplay == null)
        {
            countdownDisplay = FindObjectOfType<hudCountdown>();
        }

        // Set initial visual state
        SetVisualState(ButtonState.Default);

        if (FilmHUDManager.Instance != null && FilmHUDManager.Instance.ShowDebugInfo)
        {
            Debug.Log($"[FilmHUDButton] Initialized: {actionName} (Type: {buttonType})");
        }
    }

    private void Update()
    {
        // Handle hover timer for Hold buttons
        if (isHovering && buttonType == ButtonType.Hold && !actionTriggered)
        {
            hoverTimer += Time.deltaTime;

            // Update visual feedback
            UpdateHoverProgress();

            // Trigger when duration reached
            if (hoverTimer >= holdDuration)
            {
                TriggerAction();
            }
        }

        // Smooth scale animation
        if (useScaleAnimation)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverTimer = 0f;
        actionTriggered = false;

        SetVisualState(ButtonState.Hover);

        // Instant trigger for Instant type buttons
        if (buttonType == ButtonType.Instant)
        {
            TriggerAction();
        }

        if (FilmHUDManager.Instance != null && FilmHUDManager.Instance.ShowDebugInfo)
        {
            Debug.Log($"[FilmHUDButton] Hover enter: {actionName}");
        }
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

        // Reset fill image
        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
        }

        // Return to appropriate state
        if (buttonType == ButtonType.Toggle && isToggled)
        {
            SetVisualState(ButtonState.Active);
        }
        else
        {
            SetVisualState(ButtonState.Default);
        }

        if (FilmHUDManager.Instance != null && FilmHUDManager.Instance.ShowDebugInfo)
        {
            Debug.Log($"[FilmHUDButton] Hover exit: {actionName}");
        }
    }

    /// <summary>
    /// Update visual feedback during hover (for Hold type)
    /// </summary>
    private void UpdateHoverProgress()
    {
        float progress = Mathf.Clamp01(hoverTimer / holdDuration);

        // Update countdown display
        if (countdownDisplay != null)
        {
            countdownDisplay.SetCountdown(holdDuration, hoverTimer);
        }

        // Update fill image (radial countdown)
        if (fillImage != null)
        {
            fillImage.fillAmount = progress;
        }

        // Optional: Color lerp during countdown
        if (useColorFeedback && buttonImage != null)
        {
            buttonImage.color = Color.Lerp(hoverColor, activeColor, progress);
        }
    }

    /// <summary>
    /// Trigger the button action
    /// </summary>
    private void TriggerAction()
    {
        actionTriggered = true;

        // Handle toggle state
        if (buttonType == ButtonType.Toggle)
        {
            isToggled = !isToggled;
        }

        // Visual feedback
        SetVisualState(ButtonState.Active);

        // Invoke event
        OnButtonTriggered?.Invoke();

        if (FilmHUDManager.Instance != null && FilmHUDManager.Instance.ShowDebugInfo)
        {
            Debug.Log($"<color=lime>[FilmHUDButton] ✓ Triggered: {actionName}</color>");
        }

        // Reset after brief delay (except for toggle buttons)
        if (buttonType != ButtonType.Toggle)
        {
            Invoke(nameof(ResetButton), 0.3f);
        }
    }

    /// <summary>
    /// Reset button to default state
    /// </summary>
    private void ResetButton()
    {
        if (!isHovering)
        {
            SetVisualState(ButtonState.Default);
        }
        else
        {
            SetVisualState(ButtonState.Hover);
        }
    }

    /// <summary>
    /// Set visual state (sprite, color, scale)
    /// </summary>
    private void SetVisualState(ButtonState state)
    {
        currentState = state;

        if (buttonImage == null) return;

        // Apply sprite
        switch (state)
        {
            case ButtonState.Default:
                if (defaultSprite != null)
                    buttonImage.sprite = defaultSprite;
                if (useColorFeedback)
                    buttonImage.color = defaultColor;
                targetScale = originalScale;
                break;

            case ButtonState.Hover:
                if (hoverSprite != null)
                    buttonImage.sprite = hoverSprite;
                else if (defaultSprite != null)
                    buttonImage.sprite = defaultSprite;

                if (useColorFeedback)
                    buttonImage.color = hoverColor;
                targetScale = originalScale * hoverScale;
                break;

            case ButtonState.Active:
                if (activeSprite != null)
                    buttonImage.sprite = activeSprite;
                else if (hoverSprite != null)
                    buttonImage.sprite = hoverSprite;
                else if (defaultSprite != null)
                    buttonImage.sprite = defaultSprite;

                if (useColorFeedback)
                    buttonImage.color = activeColor;
                targetScale = originalScale * activeScale;
                break;
        }
    }

    /// <summary>
    /// Manually trigger this button (for testing)
    /// </summary>
    [ContextMenu("Trigger Button")]
    public void ManualTrigger()
    {
        OnButtonTriggered?.Invoke();

        if (FilmHUDManager.Instance != null && FilmHUDManager.Instance.ShowDebugInfo)
        {
            Debug.Log($"[FilmHUDButton] ✓ Manually triggered: {actionName}");
        }
    }

    /// <summary>
    /// Set button enabled/disabled state
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;

        if (!enabled)
        {
            SetVisualState(ButtonState.Default);
            buttonImage.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, defaultColor.a * 0.5f);
        }
    }

    /// <summary>
    /// For toggle buttons - set the toggled state
    /// </summary>
    public void SetToggleState(bool toggled)
    {
        if (buttonType != ButtonType.Toggle) return;

        isToggled = toggled;
        SetVisualState(toggled ? ButtonState.Active : ButtonState.Default);
    }

    /// <summary>
    /// Get current toggle state (for Toggle type buttons)
    /// </summary>
    public bool IsToggled => isToggled;

    #region Public Properties

    public ButtonState CurrentState => currentState;
    public bool IsHovering => isHovering;
    public float HoverProgress => hoverTimer / holdDuration;

    #endregion
}