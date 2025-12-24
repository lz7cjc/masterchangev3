using UnityEngine;

/// <summary>
/// SpeedController - Controls player movement speed
/// Works with PlayerMovement1 script
/// Optimized for mobile VR performance
/// </summary>
public class SpeedController : MonoBehaviour
{
    [Header("Speed Settings")]
    [SerializeField] private float speedIncrement = 0.5f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float defaultSpeed = 3f;

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement1 playerMovement;

    [Header("Visual Feedback")]
    [SerializeField] private ToggleActiveIcons toggleActiveIcons;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private float currentSpeed;

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

        // Initialize speed
        currentSpeed = defaultSpeed;
        ApplySpeed();

        LogDebug($"SpeedController initialized - Speed: {currentSpeed}");
    }

    /// <summary>
    /// Increase movement speed - called by GazeHoverTrigger
    /// </summary>
    public void IncreaseSpeed()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncrement, maxSpeed);
        ApplySpeed();
        
        LogDebug($"Speed increased to: {currentSpeed}");

        // Visual feedback
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
            Invoke(nameof(ResetIcon), 0.2f);
        }
    }

    /// <summary>
    /// Decrease movement speed - called by GazeHoverTrigger
    /// </summary>
    public void DecreaseSpeed()
    {
        currentSpeed = Mathf.Max(currentSpeed - speedIncrement, minSpeed);
        ApplySpeed();
        
        LogDebug($"Speed decreased to: {currentSpeed}");

        // Visual feedback
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.SelectIcon();
            Invoke(nameof(ResetIcon), 0.2f);
        }
    }

    /// <summary>
    /// Set speed to specific value
    /// </summary>
    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        ApplySpeed();
        LogDebug($"Speed set to: {currentSpeed}");
    }

    /// <summary>
    /// Reset speed to default
    /// </summary>
    public void ResetSpeed()
    {
        currentSpeed = defaultSpeed;
        ApplySpeed();
        LogDebug($"Speed reset to default: {currentSpeed}");
    }

    private void ApplySpeed()
    {
        if (playerMovement != null)
        {
            playerMovement.SetMovementSpeed(currentSpeed);
        }
        else
        {
            Debug.LogWarning("[SpeedController] PlayerMovement1 not found - speed not applied");
        }
    }

    private void ResetIcon()
    {
        if (toggleActiveIcons != null)
        {
            toggleActiveIcons.DefaultIcon();
        }
    }

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SpeedController] {message}");
        }
    }

    #region Inspector Helpers

    [ContextMenu("Test Increase Speed")]
    private void TestIncreaseSpeed()
    {
        if (Application.isPlaying)
        {
            IncreaseSpeed();
        }
    }

    [ContextMenu("Test Decrease Speed")]
    private void TestDecreaseSpeed()
    {
        if (Application.isPlaying)
        {
            DecreaseSpeed();
        }
    }

    #endregion

    #region Public Properties

    public float CurrentSpeed => currentSpeed;
    public float SpeedPercentage => Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

    #endregion
}
