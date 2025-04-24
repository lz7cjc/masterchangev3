using UnityEngine;
using TMPro;

/// <summary>
/// Controls player movement in the scene using reticle pointer interaction.
/// Supports both VR and non-VR modes with terrain following and collision detection.
/// </summary>
public class Motion : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool loop = false;
    public bool mouseHover = false;
    private bool move = false;
    private bool toggler = false;
    private float Counter1 = 0;
    private float speed;
    private float speedSet;
    private string status;
    public float Delay;
    public float DelayStop;
    public Rigidbody player;
    private float deltaSpeed;
    private float displaySpeed;

    [Tooltip("How quickly the player follows terrain")]
    public float terrainFollowSpeed = 5.0f;
    [Tooltip("Distance to check for ground below player")]
    public float groundCheckDistance = 2.0f;
    [Tooltip("Minimum allowed movement speed")]
    public float minSpeed = 0.5f;
    [Tooltip("Maximum allowed movement speed")]
    public float maxSpeed = 10.0f;
    [Tooltip("Distance to check ahead for obstacles")]
    public float obstacleCheckDistance = 0.5f;

    private hudCountdown hudCountdown;

    [Header("UI Elements")]
    public TMP_Text speedvalue;
    public TMP_Text speedvalue1;
    public TMP_Text speedvalue2;
    public TMP_Text speedvalue3;

    public GameObject walkStopIcon1;
    public GameObject walkStartIcon1;
    public GameObject walkStopIcon2;
    public GameObject walkStartIcon2;
    public GameObject walkStopIcon3;
    public GameObject walkStartIcon3;
    public GameObject walkStopIcon4;
    public GameObject walkStartIcon4;

    private bool changeSpeed;
    private bool startWalking;
    private bool stopNowTrigger;

    private showHideHUDMove showHideHUDMove;

    private void Start()
    {
        // Initialize references
        hudCountdown = FindFirstObjectByType<hudCountdown>();
        showHideHUDMove = FindFirstObjectByType<showHideHUDMove>();

        // Load saved speed if available
        if (PlayerPrefs.HasKey("walkspeed"))
        {
            speedSet = PlayerPrefs.GetInt("walkspeed");
            if (speedSet > 0)
            {
                move = true;
            }
        }
    }

    public void FixedUpdate()
    {
        // Update UI with current speed
        UpdateSpeedUI();

        // Handle hover interaction
        HandleHoverInteraction();

        // Apply movement if needed
        ApplyMovement();
    }

    private void UpdateSpeedUI()
    {
        // Update all speed text displays
        if (speedvalue != null) speedvalue.text = speedSet.ToString();
        if (speedvalue1 != null) speedvalue1.text = speedSet.ToString();
        if (speedvalue2 != null) speedvalue2.text = speedSet.ToString();
        if (speedvalue3 != null) speedvalue3.text = speedSet.ToString();
    }

    private void HandleHoverInteraction()
    {
        if (mouseHover)
        {
            Counter1 += Time.deltaTime;

            // Update countdown UI
            if (hudCountdown != null)
            {
                hudCountdown.SetCountdown(Delay, Counter1);
            }

            // Process button action when delay is reached
            if (Counter1 >= Delay)
            {
                // Reset countdown UI
                if (hudCountdown != null)
                {
                    hudCountdown.resetCountdown();
                }

                mouseHover = false;
                Counter1 = 0;

                // Handle different button actions
                if (stopNowTrigger)
                {
                    toggler = !toggler;
                    stopTheCamera();
                }
                else if (!toggler && changeSpeed)
                {
                    toggler = !toggler;
                    move = !move;
                    // Apply speed change but clamp within min-max range
                    speedSet = Mathf.Clamp(speedSet + deltaSpeed, minSpeed, maxSpeed);
                }
                else if (!toggler && startWalking)
                {
                    toggler = !toggler;
                    move = !move;
                    speedSet = speed;
                }
                else if (move && !changeSpeed)
                {
                    // This branch is kept for compatibility
                }
                else if (!toggler && !changeSpeed)
                {
                    toggler = !toggler;
                    move = !move;
                    speedSet = 0;
                    PlayerPrefs.SetInt("walkspeed", (int)speedSet);
                }
            }
        }
    }

    private void ApplyMovement()
    {
        if (speedSet > 0)
        {
            LetsGo();
        }
        else if (speedSet == 0)
        {
            stopTheCamera();
        }
    }

    #region Button Interaction Handlers

    public void OnMouseEnterStartWalk(float speed1)
    {
        mouseHover = true;
        changeSpeed = false;
        startWalking = true;
        stopNowTrigger = false;
        speed = speed1;
    }

    public void OnMouseEnterChangeSpeed(float deltaSpeed1)
    {
        mouseHover = true;
        changeSpeed = true;
        startWalking = false;
        stopNowTrigger = false;
        deltaSpeed = deltaSpeed1;
    }

    public void OnMouseEnterStop()
    {
        mouseHover = true;
        stopNowTrigger = true;
        changeSpeed = false;
        startWalking = false;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    public void OnMouseExit()
    {
        mouseHover = false;
        toggler = false;
        Counter1 = 0;
        stopNowTrigger = false;
        changeSpeed = false;
        startWalking = false;

        if (hudCountdown != null)
        {
            hudCountdown.resetCountdown();
        }
    }

    #endregion

    #region Movement Methods

    public void LetsGo()
    {
        // Get movement direction from camera
        Vector3 moveDirection = Camera.main.transform.forward;

        // Keep movement horizontal (no flying)
        moveDirection.y = 0;
        moveDirection.Normalize();

        // Check for obstacles
        if (!IsObstacleAhead(moveDirection))
        {
            // Calculate next position with terrain following
            Vector3 nextPosition = CalculateNextPosition(moveDirection);

            // Apply movement
            player.MovePosition(nextPosition);
        }
        else
        {
            // Slow down or stop when approaching obstacles
            // This makes movement more natural when approaching walls
            speedSet = Mathf.Max(speedSet * 0.8f, 0);
            if (speedSet < minSpeed)
            {
                speedSet = 0;
            }
        }

        // Update UI icons
        UpdateMovementIcons(true);
    }

    public void stopTheCamera()
    {
        speedSet = 0;
        player.MovePosition(transform.position + Camera.main.transform.forward * 0 * Time.deltaTime);
        toggler = false;

        // Update UI icons
        UpdateMovementIcons(false);
    }

    private void UpdateMovementIcons(bool isMoving)
    {
        // Update all walk/stop icons
        if (walkStartIcon1 != null) walkStartIcon1.SetActive(!isMoving);
        if (walkStopIcon1 != null) walkStopIcon1.SetActive(isMoving);

        if (walkStartIcon2 != null) walkStartIcon2.SetActive(!isMoving);
        if (walkStopIcon2 != null) walkStopIcon2.SetActive(isMoving);

        if (walkStartIcon3 != null) walkStartIcon3.SetActive(!isMoving);
        if (walkStopIcon3 != null) walkStopIcon3.SetActive(isMoving);

        if (walkStartIcon4 != null) walkStartIcon4.SetActive(!isMoving);
        if (walkStopIcon4 != null) walkStopIcon4.SetActive(isMoving);
    }

    #endregion

    #region Terrain Following and Collision

    private Vector3 CalculateNextPosition(Vector3 moveDirection)
    {
        // Calculate base horizontal movement
        Vector3 horizontalMovement = moveDirection * speedSet * Time.deltaTime;
        Vector3 nextPos = player.position + horizontalMovement;

        // Apply terrain following using regular raycasting without layer mask
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(nextPos.x, nextPos.y + 0.5f, nextPos.z), Vector3.down, out hit, groundCheckDistance))
        {
            // Adjust height based on terrain
            float targetY = hit.point.y + 1.0f; // Add offset for player height
            nextPos.y = Mathf.Lerp(player.position.y, targetY, Time.deltaTime * terrainFollowSpeed);
        }

        return nextPos;
    }

    private bool IsObstacleAhead(Vector3 moveDirection)
    {
        // Calculate check distance based on current speed
        float checkDistance = speedSet * Time.deltaTime + obstacleCheckDistance;

        // Cast ray from player position
        RaycastHit hit;
        if (Physics.Raycast(player.position + Vector3.up * 0.5f, moveDirection, out hit, checkDistance))
        {
            // Ignore collisions with trigger colliders
            if (!hit.collider.isTrigger)
            {
                return true; // Solid obstacle detected
            }
        }

        return false; // No obstacle
    }

    #endregion
}