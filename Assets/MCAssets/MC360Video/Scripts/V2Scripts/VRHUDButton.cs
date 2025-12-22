using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Handles HUD button interactions: hover detection, countdown, and action triggering.
/// Attach to each HUD button GameObject.
/// Works with VRReticlePointer for both 360 and VR modes.
/// </summary>
public class VRHUDButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Action Settings")]
    [SerializeField] private string actionName; // For debugging
    [SerializeField] private float hoverDelaySeconds = 2f; // Hold time before trigger
    [SerializeField] private bool requireHoldToTrigger = true;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite selectedSprite; // Optional - leave empty if not needed

    [Header("Countdown Display")]
    [SerializeField] private hudCountdown countdownDisplay; // Reference to countdown UI

    [Header("Events")]
    public UnityEvent OnButtonTriggered;

    // State
    private bool isHovering = false;
    private float hoverTimer = 0f;
    private bool actionTriggered = false;

    void Update()
    {
        if (isHovering && requireHoldToTrigger && !actionTriggered)  // Add !actionTriggered
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

        // Change to hover sprite
        if (iconRenderer != null && hoverSprite != null)
        {
            iconRenderer.sprite = hoverSprite;
        }

        Debug.Log($"VRHUDButton: Hover enter - {actionName}");
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

        // Change back to default sprite
        if (iconRenderer != null && defaultSprite != null)
        {
            iconRenderer.sprite = defaultSprite;
        }

        Debug.Log($"VRHUDButton: Hover exit - {actionName}");
    }

    private void TriggerAction()
    {
        actionTriggered = true;

        // Change to selected sprite (if assigned)
        if (iconRenderer != null && selectedSprite != null)
        {
            iconRenderer.sprite = selectedSprite;
        }

        // Invoke the event
        OnButtonTriggered?.Invoke();

        Debug.Log($"VRHUDButton: Action triggered - {actionName}");

        // Reset after short delay
        Invoke(nameof(ResetButton), 0.3f);
    }

    private void ResetButton()
    {
        if (!isHovering)
        {
            if (iconRenderer != null && defaultSprite != null)
            {
                iconRenderer.sprite = defaultSprite;
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
        Debug.Log($"VRHUDButton: Manually triggered - {actionName}");
    }


}