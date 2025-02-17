using UnityEngine;
using TMPro;

public class FPVMovement : MonoBehaviour
{
    [Header("Timer Settings")]
    public float Delay = 3f;
    public float DelayStop = 2f;

    [Header("References")]
    [SerializeField] private Rigidbody player;
    [SerializeField] private TMP_Text speedvalue;
    [SerializeField] private GameObject walkStopIcon1;
    [SerializeField] private GameObject walkStartIcon1;
    private hudCountdown countdownTimer;

    private float Counter1 = 0f;
    private float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float startSpeed = 10f;
    private bool mouseHover = false;
    private bool isWalking = false;
    private bool isSpeedChangeRequested = false;
    private Camera mainCam;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (player == null)
        {
            Debug.LogError("Player Rigidbody not assigned!");
            return;
        }

        player.useGravity = false;
        player.collisionDetectionMode = CollisionDetectionMode.Continuous;
        player.interpolation = RigidbodyInterpolation.Interpolate;
        player.constraints = RigidbodyConstraints.None;

        Debug.Log($"Initial Rigidbody setup - Constraints: {player.constraints}, Position: {player.position}");

        mainCam = Camera.main;
        if (!mainCam)
        {
            Debug.LogError("Main Camera not found! Ensure there's a camera tagged as MainCamera");
            return;
        }
        Debug.Log($"Found main camera: {mainCam.name}, Position: {mainCam.transform.position}");

        countdownTimer = GetComponent<hudCountdown>();
        if (countdownTimer == null)
        {
            Debug.LogError("hudCountdown component not found on this GameObject!");
            return;
        }

        currentSpeed = 0f;
        targetSpeed = 0f;
        if (speedvalue)
        {
            speedvalue.text = "0.0";
        }
        UpdateMovementIcons(false);

        isInitialized = true;
        isWalking = false;
        Debug.Log($"FPVMovement initialized - isInitialized: {isInitialized}, isWalking: {isWalking}");
    }

    private void Update()
    {
        if (!isInitialized)
        {
            Debug.LogWarning($"FPVMovement not initialized, skipping Update. isInitialized: {isInitialized}");
            return;
        }

        if (mouseHover)
        {
            HandleMouseHover();
        }

        if (speedvalue)
        {
            speedvalue.text = currentSpeed.ToString("F1");
        }
    }

    private void FixedUpdate()
    {
        Debug.Log($"FixedUpdate - isInitialized: {isInitialized}, isWalking: {isWalking}");

        if (!isInitialized || !isWalking)
        {
            return;
        }

        UpdateMovement();
    }

    private void HandleMouseHover()
    {
        Counter1 += Time.deltaTime;

        if (countdownTimer)
        {
            countdownTimer.SetCountdown(Delay, Counter1);
        }

        if (Counter1 >= Delay)
        {
            ProcessHoverComplete();
        }
    }

    private void ProcessHoverComplete()
    {
        if (countdownTimer)
        {
            countdownTimer.resetCountdown();
        }

        mouseHover = false;
        Counter1 = 0f;

        if (!isWalking && !isSpeedChangeRequested)
        {
            Debug.Log($"Starting walk sequence - Setting initial speed to {startSpeed}");
            targetSpeed = startSpeed;
            currentSpeed = startSpeed;
            isWalking = true;
            UpdateMovementIcons(true);
            Debug.Log($"Walk state updated - isWalking: {isWalking}, currentSpeed: {currentSpeed}");
        }
        else if (isSpeedChangeRequested)
        {
            currentSpeed = targetSpeed;
            isSpeedChangeRequested = false;
        }

        Debug.Log($"Process Complete - Walking: {isWalking}, Speed: {currentSpeed}, Target: {targetSpeed}");
    }

    private void UpdateMovement()
    {
        if (!player || !mainCam)
        {
            Debug.LogError($"Missing references - Player: {player != null}, MainCam: {mainCam != null}");
            return;
        }

        Debug.Log($"Starting UpdateMovement - Position: {player.position}, Speed: {currentSpeed}");

        if (player.constraints != RigidbodyConstraints.None)
        {
            Debug.LogWarning($"Constraints detected: {player.constraints} - removing constraints");
            player.constraints = RigidbodyConstraints.None;
        }

        // Get and normalize camera direction
        Vector3 moveDirection = mainCam.transform.forward;
        moveDirection.y = 0f;
        moveDirection.Normalize();

        Debug.Log($"Camera info - Position: {mainCam.transform.position}, Forward: {mainCam.transform.forward}");
        Debug.Log($"Movement info - Direction: {moveDirection}, Speed: {currentSpeed}");

        if (moveDirection != Vector3.zero && currentSpeed > 0.01f)
        {
            Vector3 targetVelocity = moveDirection * currentSpeed;
            player.linearVelocity = targetVelocity;
            Debug.Log($"Applied velocity - Target: {targetVelocity}, Actual: {player.linearVelocity}");
        }
        else
        {
            Debug.Log($"No movement - Direction zero: {moveDirection == Vector3.zero}, Speed too low: {currentSpeed <= 0.01f}");
        }
    }

    private void UpdateMovementIcons(bool isMoving)
    {
        if (walkStartIcon1) walkStartIcon1.SetActive(!isMoving);
        if (walkStopIcon1) walkStopIcon1.SetActive(isMoving);
        Debug.Log($"Icons updated - Moving: {isMoving}");
    }

    public void OnMouseEnterStartWalk(float initialSpeed = 10f)
    {
        Debug.Log($"Walk button activated - Speed: {initialSpeed}, isInitialized: {isInitialized}, isWalking: {isWalking}");
        if (isInitialized && !isWalking)
        {
            startSpeed = initialSpeed;
            mouseHover = true;
            Counter1 = 0f;
        }
    }

    public void OnMouseEnterChangeSpeed(float direction)
    {
        const float speedChange = 2f;
        float deltaSpeed = direction > 0 ? speedChange : -speedChange;

        Debug.Log($"Speed change requested: {deltaSpeed}");
        if (!isWalking) return;

        mouseHover = true;
        isSpeedChangeRequested = true;
        targetSpeed = Mathf.Max(0, currentSpeed + deltaSpeed);
        Counter1 = 0f;

        Debug.Log($"Speed update - Current: {currentSpeed}, Target: {targetSpeed}");
    }

    public void OnMouseEnterStop()
    {
        Debug.Log($"Stop requested - Current walking state: {isWalking}");
        if (!isWalking) return;

        mouseHover = true;
        if (countdownTimer)
        {
            countdownTimer.resetCountdown();
        }

        StopMovement();
    }

    private void StopMovement()
    {
        isWalking = false;
        targetSpeed = 0f;
        currentSpeed = 0f;

        if (player)
        {
            player.linearVelocity = Vector3.zero;
        }

        UpdateMovementIcons(false);
        Debug.Log($"Movement stopped - Position: {player.position}, isWalking: {isWalking}");
    }

    public void OnMouseExit()
    {
        Debug.Log($"Mouse Exit - Walking: {isWalking}, Speed: {currentSpeed}");
        mouseHover = false;
        Counter1 = 0f;

        if (countdownTimer)
        {
            countdownTimer.resetCountdown();
        }
    }

    void OnEnable()
    {
        if (player)
        {
            player.constraints = RigidbodyConstraints.None;
            Debug.Log("OnEnable - Reset constraints to None");
        }
    }

    void OnDisable()
    {
        if (player)
        {
            player.linearVelocity = Vector3.zero;
        }
    }
}