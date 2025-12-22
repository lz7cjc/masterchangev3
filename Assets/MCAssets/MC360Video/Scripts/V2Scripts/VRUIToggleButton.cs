using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Generic toggle script for UI buttons that switch between two states.
/// Works with VRHUDButtonUI or standalone.
/// Use for: Open/Close, Play/Pause, Mute/Unmute, etc.
/// </summary>
public class VRUIToggleButton : MonoBehaviour
{
    [Header("Button Reference")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private VRHUDButtonUI hudButtonUI;
    [SerializeField] private bool autoFindComponents = true;

    [Header("State A (Default)")]
    [SerializeField] private Sprite stateASprite;
    [SerializeField] private Sprite stateAHoverSprite;
    [SerializeField] private string stateAName = "Close"; // For OpenClose button
    [SerializeField] private UnityEvent onStateAActivated;

    [Header("State B (Toggled)")]
    [SerializeField] private Sprite stateBSprite;
    [SerializeField] private Sprite stateBHoverSprite;
    [SerializeField] private string stateBName = "Open"; // For OpenClose button
    [SerializeField] private UnityEvent onStateBActivated;

    [Header("Current State")]
    [SerializeField] private bool isStateB = false; // false = State A, true = State B

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private void Start()
    {
        // Auto-find components
        if (autoFindComponents)
        {
            if (buttonImage == null)
                buttonImage = GetComponent<Image>();

            if (hudButtonUI == null)
                hudButtonUI = GetComponent<VRHUDButtonUI>();
        }

        // Subscribe to VRHUDButtonUI trigger event if present
        if (hudButtonUI != null)
        {
            hudButtonUI.OnButtonTriggered.AddListener(Toggle);
        }

        // Set initial state
        UpdateVisuals();

        if (showDebug)
            Debug.Log($"[VRUIToggleButton] Initialized - Current state: {CurrentStateName}");
    }

    /// <summary>
    /// Toggle between State A and State B
    /// </summary>
    public void Toggle()
    {
        isStateB = !isStateB;
        UpdateVisuals();
        TriggerStateEvent();

        if (showDebug)
            Debug.Log($"[VRUIToggleButton] Toggled to: {CurrentStateName}");
    }

    /// <summary>
    /// Set to State A (default)
    /// </summary>
    public void SetStateA()
    {
        if (isStateB)
        {
            isStateB = false;
            UpdateVisuals();
            TriggerStateEvent();

            if (showDebug)
                Debug.Log($"[VRUIToggleButton] Set to State A: {stateAName}");
        }
    }

    /// <summary>
    /// Set to State B (toggled)
    /// </summary>
    public void SetStateB()
    {
        if (!isStateB)
        {
            isStateB = true;
            UpdateVisuals();
            TriggerStateEvent();

            if (showDebug)
                Debug.Log($"[VRUIToggleButton] Set to State B: {stateBName}");
        }
    }

    /// <summary>
    /// Update button visuals based on current state
    /// </summary>
    private void UpdateVisuals()
    {
        if (buttonImage == null) return;

        // Update main sprite
        if (isStateB)
        {
            if (stateBSprite != null)
                buttonImage.sprite = stateBSprite;
        }
        else
        {
            if (stateASprite != null)
                buttonImage.sprite = stateASprite;
        }

        // Update VRHUDButtonUI sprites if present
        if (hudButtonUI != null)
        {
            // Use reflection to update private fields
            // This is a bit hacky but works
            var buttonImageField = hudButtonUI.GetType().GetField("buttonImage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var defaultSpriteField = hudButtonUI.GetType().GetField("defaultSprite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hoverSpriteField = hudButtonUI.GetType().GetField("hoverSprite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (buttonImageField != null)
                buttonImageField.SetValue(hudButtonUI, buttonImage);

            if (isStateB)
            {
                if (defaultSpriteField != null && stateBSprite != null)
                    defaultSpriteField.SetValue(hudButtonUI, stateBSprite);
                if (hoverSpriteField != null && stateBHoverSprite != null)
                    hoverSpriteField.SetValue(hudButtonUI, stateBHoverSprite);
            }
            else
            {
                if (defaultSpriteField != null && stateASprite != null)
                    defaultSpriteField.SetValue(hudButtonUI, stateASprite);
                if (hoverSpriteField != null && stateAHoverSprite != null)
                    hoverSpriteField.SetValue(hudButtonUI, stateAHoverSprite);
            }
        }
    }

    /// <summary>
    /// Trigger the appropriate state event
    /// </summary>
    private void TriggerStateEvent()
    {
        if (isStateB)
        {
            onStateBActivated?.Invoke();
        }
        else
        {
            onStateAActivated?.Invoke();
        }
    }

    /// <summary>
    /// Get current state name for debugging
    /// </summary>
    public string CurrentStateName => isStateB ? stateBName : stateAName;

    /// <summary>
    /// Check if currently in State B
    /// </summary>
    public bool IsStateB => isStateB;

    /// <summary>
    /// Check if currently in State A
    /// </summary>
    public bool IsStateA => !isStateB;

    [ContextMenu("Toggle State")]
    private void ContextToggle()
    {
        Toggle();
    }

    [ContextMenu("Force State A")]
    private void ContextForceStateA()
    {
        SetStateA();
    }

    [ContextMenu("Force State B")]
    private void ContextForceStateB()
    {
        SetStateB();
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (hudButtonUI != null)
        {
            hudButtonUI.OnButtonTriggered.RemoveListener(Toggle);
        }
    }
}