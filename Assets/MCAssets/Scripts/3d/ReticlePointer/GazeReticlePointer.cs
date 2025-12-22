using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Master gaze-based interaction system for Google Cardboard VR
/// Handles reticle visuals, raycasting, and interaction detection
/// Works in both VR mode (gyroscope) and 360 mode (touch camera rotation)
/// Integrates with Unity's new Input System
/// </summary>
public class GazeReticlePointer : MonoBehaviour
{
    public enum ViewMode { Mode360, ModeVR }

    [Header("Mode Settings")]
    [SerializeField] private ViewMode currentMode = ViewMode.Mode360;
    [SerializeField] private bool showDebugRay = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera attachedCamera;
    [SerializeField] private bool autoFindCamera = true;

    [Header("Reticle Visual Settings")]
    [SerializeField] private GameObject reticleDot;
    [SerializeField] private GameObject reticleRing;
    [SerializeField] private float reticleDistance = 2f;
    [SerializeField] private float dotScale = 0.008f;
    [SerializeField] private float ringIdleScale = 0.015f;
    [SerializeField] private float ringHoverScale = 0.03f;
    [SerializeField] private float ringScaleSpeed = 8f;

    [Header("Reticle Colors")]
    [SerializeField] private Color dotIdleColor = Color.white;
    [SerializeField] private Color dotHoverColor = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private Color ringIdleColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color ringHoverColor = new Color(0f, 1f, 1f, 0.7f); // Cyan

    [Header("Raycast Settings")]
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private LayerMask interactableLayers = ~0; // Everything by default

    [Header("360 Mode Camera Rotation")]
    [SerializeField, Range(0.1f, 20f)] private float horizontalSensitivity = 0.5f;
    [SerializeField, Range(0.1f, 20f)] private float verticalSensitivity = 0.5f;
    [SerializeField] private float dampingStrength = 5f;
    [SerializeField] private bool useSmoothing = true;

    [Header("Rotation Limits")]
    [SerializeField] private bool limitVerticalRotation = true;
    [SerializeField] private float minVerticalAngle = -80f;
    [SerializeField] private float maxVerticalAngle = 80f;

    // Input System
    private PlayerInput playerInput;
    private InputAction lookAction;
    private InputAction touchPressAction;

    // State
    private GazeHoverTrigger currentHoverTarget;
    private GazeHUDController hudController;
    private float currentRingScale;
    private float currentDotScale;
    private bool isHoveringInteractable = false;

    // Materials for color changes
    private Material dotMaterial;
    private Material ringMaterial;

    // Camera rotation state (360 mode)
    private bool isRotating = false;
    private Vector2 previousLookInput;
    private Vector3 currentRotation;
    private Vector3 targetRotation;

    void Awake()
    {
        // Find camera if not assigned
        if (autoFindCamera && attachedCamera == null)
        {
            attachedCamera = GetComponent<Camera>();
            if (attachedCamera == null)
            {
                attachedCamera = GetComponentInChildren<Camera>();
            }
        }

        if (attachedCamera == null)
        {
            Debug.LogError("[GazeReticlePointer] No camera found! Assign camera in inspector.");
            enabled = false;
            return;
        }

        // Get PlayerInput
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];
            touchPressAction = playerInput.actions["TouchPress"];
        }
        else
        {
            Debug.LogWarning("[GazeReticlePointer] No PlayerInput found - 360 camera rotation disabled");
        }

        // Initialize rotation
        currentRotation = transform.eulerAngles;
        targetRotation = currentRotation;

        InitializeReticle();
        FindHUDController();

        currentRingScale = ringIdleScale;
        currentDotScale = dotScale;
    }

    void OnEnable()
    {
        ConfigureInputActions();
    }

    void OnDisable()
    {
        CleanupInputActions();
        ExitCurrentTarget();
    }

    void LateUpdate()
    {
        // Handle camera rotation in 360 mode
        if (currentMode == ViewMode.Mode360 && isRotating)
        {
            HandleCameraRotation();
        }

        // Always perform raycasting and update reticle
        PerformRaycast();
        UpdateReticleVisuals();
    }

    /// <summary>
    /// Initialize reticle visual elements
    /// </summary>
    private void InitializeReticle()
    {
        // Create reticle dot if not assigned
        if (reticleDot == null)
        {
            reticleDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleDot.name = "ReticleDot";
            Destroy(reticleDot.GetComponent<Collider>());
            reticleDot.transform.SetParent(transform);
            reticleDot.transform.localScale = Vector3.one * dotScale;

            dotMaterial = new Material(Shader.Find("Unlit/Color"));
            dotMaterial.color = dotIdleColor;
            reticleDot.GetComponent<Renderer>().material = dotMaterial;
        }
        else
        {
            dotMaterial = reticleDot.GetComponent<Renderer>().material;
        }

        // Create reticle ring if not assigned
        if (reticleRing == null)
        {
            reticleRing = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticleRing.name = "ReticleRing";
            Destroy(reticleRing.GetComponent<Collider>());
            reticleRing.transform.SetParent(transform);
            reticleRing.transform.localScale = Vector3.one * ringIdleScale;

            ringMaterial = new Material(Shader.Find("Unlit/Color"));
            ringMaterial.color = ringIdleColor;
            reticleRing.GetComponent<Renderer>().material = ringMaterial;
        }
        else
        {
            ringMaterial = reticleRing.GetComponent<Renderer>().material;
        }

        Debug.Log("[GazeReticlePointer] Reticle initialized");
    }

    /// <summary>
    /// Find HUD controller in scene
    /// </summary>
    private void FindHUDController()
    {
        hudController = FindFirstObjectByType<GazeHUDController>();
        if (hudController == null)
        {
            Debug.LogWarning("[GazeReticlePointer] No GazeHUDController found in scene");
        }
    }

    /// <summary>
    /// Configure input actions based on mode
    /// </summary>
    private void ConfigureInputActions()
    {
        if (touchPressAction == null) return;

        // Clean up old bindings
        touchPressAction.performed -= OnTouchPress;
        touchPressAction.canceled -= OnTouchRelease;

        // Setup 360 mode bindings
        if (currentMode == ViewMode.Mode360)
        {
            touchPressAction.performed += OnTouchPress;
            touchPressAction.canceled += OnTouchRelease;
        }
    }

    /// <summary>
    /// Cleanup input actions
    /// </summary>
    private void CleanupInputActions()
    {
        if (touchPressAction != null)
        {
            touchPressAction.performed -= OnTouchPress;
            touchPressAction.canceled -= OnTouchRelease;
        }
    }

    /// <summary>
    /// Handle touch press start (360 mode)
    /// </summary>
    private void OnTouchPress(InputAction.CallbackContext context)
    {
        if (currentMode != ViewMode.Mode360) return;

        isRotating = true;
        previousLookInput = lookAction.ReadValue<Vector2>();
        
        // Normalize current rotation
        currentRotation = new Vector3(
            Mathf.Clamp(transform.eulerAngles.x > 180 ? transform.eulerAngles.x - 360 : transform.eulerAngles.x, 
                       minVerticalAngle, maxVerticalAngle),
            transform.eulerAngles.y,
            0
        );
        targetRotation = currentRotation;
    }

    /// <summary>
    /// Handle touch release (360 mode)
    /// </summary>
    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        isRotating = false;
    }

    /// <summary>
    /// Handle camera rotation in 360 mode
    /// </summary>
    private void HandleCameraRotation()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        Vector2 lookDelta = lookInput - previousLookInput;

        // Full 360 horizontal rotation
        targetRotation.y += lookDelta.x * horizontalSensitivity;

        // Limited vertical rotation
        if (limitVerticalRotation)
        {
            targetRotation.x = Mathf.Clamp(
                targetRotation.x - lookDelta.y * verticalSensitivity,
                minVerticalAngle,
                maxVerticalAngle
            );
        }
        else
        {
            targetRotation.x -= lookDelta.y * verticalSensitivity;
        }

        // Apply rotation
        if (useSmoothing)
        {
            currentRotation = Vector3.Lerp(currentRotation, targetRotation, Time.deltaTime * dampingStrength);
            transform.rotation = Quaternion.Euler(currentRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(targetRotation);
        }

        previousLookInput = lookInput;
    }

    /// <summary>
    /// Perform raycast and handle hit detection
    /// </summary>
    private void PerformRaycast()
    {
        Ray ray = new Ray(attachedCamera.transform.position, attachedCamera.transform.forward);
        RaycastHit hit;

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.green);
        }

        bool hitSomething = Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayers);

        if (hitSomething)
        {
            HandleRaycastHit(hit);
        }
        else
        {
            HandleRaycastMiss();
        }
    }

    /// <summary>
    /// Handle successful raycast hit
    /// </summary>
    private void HandleRaycastHit(RaycastHit hit)
    {
        // Position reticle at hit point
        float hitDistance = Mathf.Min(hit.distance, reticleDistance);
        PositionReticle(hitDistance);

        // Check for GazeHoverTrigger
        GazeHoverTrigger hoverTrigger = hit.collider.GetComponent<GazeHoverTrigger>();

        if (hoverTrigger != null)
        {
            // New hover target
            if (currentHoverTarget != hoverTrigger)
            {
                ExitCurrentTarget();
                currentHoverTarget = hoverTrigger;
                currentHoverTarget.OnReticleEnter();
            }

            isHoveringInteractable = true;

            // Notify HUD if hovering over HUD element
            if (hudController != null && hoverTrigger.IsHUDElement())
            {
                hudController.OnReticleEnterHUD();
            }
        }
        else
        {
            // Hit something but no trigger
            ExitCurrentTarget();
            isHoveringInteractable = false;
        }
    }

    /// <summary>
    /// Handle raycast miss
    /// </summary>
    private void HandleRaycastMiss()
    {
        PositionReticle(reticleDistance);
        ExitCurrentTarget();
        isHoveringInteractable = false;

        if (hudController != null)
        {
            hudController.OnReticleExitHUD();
        }
    }

    /// <summary>
    /// Exit current hover target
    /// </summary>
    private void ExitCurrentTarget()
    {
        if (currentHoverTarget != null)
        {
            currentHoverTarget.OnReticleExit();

            if (hudController != null && currentHoverTarget.IsHUDElement())
            {
                hudController.OnReticleExitHUD();
            }

            currentHoverTarget = null;
        }
    }

    /// <summary>
    /// Position reticle at distance from camera
    /// </summary>
    private void PositionReticle(float distance)
    {
        Vector3 targetPosition = attachedCamera.transform.position + 
                                attachedCamera.transform.forward * distance;

        if (reticleDot != null)
        {
            reticleDot.transform.position = targetPosition;
            reticleDot.transform.rotation = attachedCamera.transform.rotation;
        }

        if (reticleRing != null)
        {
            reticleRing.transform.position = targetPosition;
            reticleRing.transform.rotation = attachedCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Update reticle visual feedback
    /// </summary>
    private void UpdateReticleVisuals()
    {
        // Target scales
        float targetRingScale = isHoveringInteractable ? ringHoverScale : ringIdleScale;
        float targetDotScale = isHoveringInteractable ? dotScale * 1.2f : dotScale;

        // Smooth transitions
        currentRingScale = Mathf.Lerp(currentRingScale, targetRingScale, Time.deltaTime * ringScaleSpeed);
        currentDotScale = Mathf.Lerp(currentDotScale, targetDotScale, Time.deltaTime * ringScaleSpeed);

        // Apply scales
        if (reticleRing != null)
        {
            reticleRing.transform.localScale = Vector3.one * currentRingScale;
        }

        if (reticleDot != null)
        {
            reticleDot.transform.localScale = Vector3.one * currentDotScale;
        }

        // Target colors
        Color targetDotColor = isHoveringInteractable ? dotHoverColor : dotIdleColor;
        Color targetRingColor = isHoveringInteractable ? ringHoverColor : ringIdleColor;

        // Apply colors
        if (dotMaterial != null)
        {
            dotMaterial.color = Color.Lerp(dotMaterial.color, targetDotColor, Time.deltaTime * ringScaleSpeed);
        }

        if (ringMaterial != null)
        {
            ringMaterial.color = Color.Lerp(ringMaterial.color, targetRingColor, Time.deltaTime * ringScaleSpeed);
        }
    }

    /// <summary>
    /// Set view mode (called by mode switching system)
    /// </summary>
    public void SetMode(ViewMode newMode)
    {
        currentMode = newMode;
        ConfigureInputActions();
        
        Debug.Log($"[GazeReticlePointer] Mode set to: {newMode}");
    }

    /// <summary>
    /// Check if currently hovering over HUD
    /// </summary>
    public bool IsHoveringHUD()
    {
        return currentHoverTarget != null && currentHoverTarget.IsHUDElement();
    }

    /// <summary>
    /// Get current hover target
    /// </summary>
    public GazeHoverTrigger GetCurrentHoverTarget()
    {
        return currentHoverTarget;
    }

    /// <summary>
    /// Get current view mode
    /// </summary>
    public ViewMode GetCurrentMode()
    {
        return currentMode;
    }

    void OnDrawGizmos()
    {
        if (!showDebugRay || attachedCamera == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(attachedCamera.transform.position, attachedCamera.transform.forward * maxRaycastDistance);
    }
}
