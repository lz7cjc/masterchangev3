using UnityEngine;
using TMPro;

public class PlayerMovement1 : MonoBehaviour
{
    [Header("Movement Settings")]
    public float defaultSpeed = 5f;
    public float speedIncrement = 2f;
    public float maxSpeed = 20f;
    public float minSpeed = 0f;

    [Header("Timing")]
    public float actionDelay = 1f;
    public float stopDelay = 0.5f;

    [Header("References")]
    public Rigidbody playerRigidbody;

    [Header("UI Elements")]
    public TMP_Text[] speedDisplays;
    public GameObject[] walkStopIcons;
    public GameObject[] walkStartIcons;

    // Private variables
    private float currentSpeed = 0f;
    private float hoverTimer = 0f;
    private bool isHovering = false;
    private bool isMoving = false;

    // Action types
    private enum ActionType { None, StartWalk, ChangeSpeed, Stop }
    private ActionType pendingAction = ActionType.None;
    private float speedDelta = 0f;

    // Cached components
    private hudCountdown hudCountdown;

    void Start()
    {
        // Cache components
        hudCountdown = FindFirstObjectByType<hudCountdown>();

        // Initialize speed from PlayerPrefs or default
        currentSpeed = PlayerPrefs.GetFloat("walkspeed", 0f);

        // Update UI
        UpdateSpeedDisplay();
        UpdateIcons();
    }

    void FixedUpdate()
    {
        HandleHoverTimer();
        HandleMovement();
    }

    private void HandleHoverTimer()
    {
        if (isHovering)
        {
            hoverTimer += Time.deltaTime;

            // Update countdown UI
            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(actionDelay, hoverTimer);
            }

            // Check if action should trigger
            if (hoverTimer >= actionDelay)
            {
                ExecutePendingAction();
                ResetHover();
            }
        }
    }

    private void HandleMovement()
    {
        if (isMoving && currentSpeed > 0f)
        {
            Vector3 movement = Camera.main.transform.forward * currentSpeed * Time.deltaTime;
            playerRigidbody.MovePosition(transform.position + movement);
        }
    }

    private void ExecutePendingAction()
    {
        switch (pendingAction)
        {
            case ActionType.StartWalk:
                StartWalking();
                break;
            case ActionType.ChangeSpeed:
                ChangeSpeed(speedDelta);
                break;
            case ActionType.Stop:
                StopWalking();
                break;
        }

        UpdateSpeedDisplay();
        UpdateIcons();
        SaveSpeed();
    }

    private void StartWalking()
    {
        if (!isMoving)
        {
            currentSpeed = defaultSpeed;
            isMoving = true;
        }
    }

    private void ChangeSpeed(float delta)
    {
        if (isMoving)
        {
            currentSpeed = Mathf.Clamp(currentSpeed + delta, minSpeed, maxSpeed);
            if (currentSpeed <= 0f)
            {
                StopWalking();
            }
        }
        else if (delta > 0f)
        {
            // Start walking if trying to increase speed while stopped
            StartWalking();
        }
    }

    private void StopWalking()
    {
        currentSpeed = 0f;
        isMoving = false;
    }

    private void UpdateSpeedDisplay()
    {
        string speedText = currentSpeed.ToString("F1");
        foreach (var display in speedDisplays)
        {
            if (display != null)
                display.text = speedText;
        }
    }

    private void UpdateIcons()
    {
        // Show appropriate icons based on movement state
        SetIconsActive(walkStartIcons, !isMoving);
        SetIconsActive(walkStopIcons, isMoving);
    }

    private void SetIconsActive(GameObject[] icons, bool active)
    {
        foreach (var icon in icons)
        {
            if (icon != null)
                icon.SetActive(active);
        }
    }

    private void ResetHover()
    {
        isHovering = false;
        hoverTimer = 0f;
        pendingAction = ActionType.None;
        speedDelta = 0f;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    private void SaveSpeed()
    {
        PlayerPrefs.SetFloat("walkspeed", currentSpeed);
    }

    // Public methods for UI interaction
    public void OnMouseEnterStartWalk()
    {
        if (!isHovering)
        {
            isHovering = true;
            pendingAction = ActionType.StartWalk;
            hoverTimer = 0f;
        }
    }

    public void OnMouseEnterSpeedUp()
    {
        if (!isHovering)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = speedIncrement;
            hoverTimer = 0f;
        }
    }

    public void OnMouseEnterSlowDown()
    {
        if (!isHovering)
        {
            isHovering = true;
            pendingAction = ActionType.ChangeSpeed;
            speedDelta = -speedIncrement;
            hoverTimer = 0f;
        }
    }

    public void OnMouseEnterStop()
    {
        if (!isHovering)
        {
            isHovering = true;
            pendingAction = ActionType.Stop;
            hoverTimer = 0f;
        }
    }

    public void OnMouseExit()
    {
        ResetHover();
    }

    // Public getters for debugging/external access
    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    public bool IsHovering => isHovering;
}