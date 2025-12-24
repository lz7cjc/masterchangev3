using UnityEngine;
using System;


/// <summary>
/// StartStopController - Toggles player movement on/off
/// Works with PlayerMovement1 script
/// Optimized for mobile VR
/// </summary>
public class StartStopController : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerMovement1 playerMovement;

    [Header("Visual Feedback")]
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;
    
    [Header("Optional: Sprite Override")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite pauseSprite;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private bool isMovementEnabled = true;

    void Start()
    {
        // Auto-find player movement
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement1>();
        }

        // Auto-find icon controller
        if (toggleActiveIcons == null)
        {
            toggleActiveIcons = GetComponent<ToggleActiveIcons>();
        }

        // Auto-find sprite renderer
        if (iconRenderer == null)
        {
            iconRenderer = GetComponent<SpriteRenderer>();
        }

        // Initialize state
        UpdateVisuals();

        LogDebug($"StartStopController initialized - Movement enabled: {isMovementEnabled}");
    }

    /// <summary>
    /// Toggle movement on/off - called by GazeHoverTrigger
    /// </summary>
    public void ToggleMovement()
    {
        isMovementEnabled = !isMovementEnabled;
        
        if (playerMovement != null)
        {
            if (isMovementEnabled)
            {
                playerMovement.EnableMovement();
                LogDebug("Movement ENABLED");
            }
            else
            {
                playerMovement.DisableMovement();
                LogDebug("Movement DISABLED");
            }
        }
        else
        {
            Debug.LogWarning("[StartStopController] PlayerMovement1 not found!");
        }

        UpdateVisuals();
    }

    /// <summary>
    /// Enable movement explicitly
    /// </summary>
    public void EnableMovement()
    {
        if (!isMovementEnabled)
        {
            ToggleMovement();
        }
    }

    /// <summary>
    /// Disable movement explicitly
    /// </summary>
    public void DisableMovement()
    {
        if (isMovementEnabled)
        {
            ToggleMovement();
        }
    }

    private void UpdateVisuals()
    {
        // Update icon state
        if (toggleActiveIcons != null)
        {
            if (isMovementEnabled)
            {
                toggleActiveIcons.SelectIcon(); // Active state when moving
            }
            else
            {
                toggleActiveIcons.DefaultIcon(); // Paused state
            }
        }

        // Update sprite if play/pause sprites are assigned
        if (iconRenderer != null && playSprite != null && pauseSprite != null)
        {
            // When moving, show pause button; when stopped, show play button
            iconRenderer.sprite = isMovementEnabled ? pauseSprite : playSprite;
        }
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[StartStopController] {message}");
        }
    }

    #region Inspector Helpers

    [ContextMenu("Test Toggle Movement")]
    private void TestToggleMovement()
    {
        if (Application.isPlaying)
        {
            ToggleMovement();
        }
    }

    #endregion

    #region Public Properties

    public bool IsMovementEnabled => isMovementEnabled;

    #endregion
}
